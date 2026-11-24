using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace LesMudOS.Driver.Compilation;

/// <summary>
/// Rewrites mudlib source so that every loop iteration and every method entry
/// calls Runtime.Tick(). That gives the driver an LPC-style evaluation cost
/// limit and a recursion guard without any support from the CLR.
///
/// The rewrite never adds or removes a line break, so line numbers in stack
/// traces still match the file the author wrote.
/// </summary>
internal sealed class TickInjector : CSharpSyntaxRewriter
{
    /// <summary>Marks a rewritten expression-bodied lambda with the position of the original, for the second pass (see Compile).</summary>
    public const string LambdaAnnotationKind = "lp-lambda";

    private readonly StatementSyntax Tick;
    private readonly ExpressionSyntax? _filterCheck;
    private readonly ExpressionSyntax _pass; // Runtime.Pass(...): the tick as an expression, true or an exception
    private readonly IReadOnlySet<int> _voidLambdas;

    /// <param name="domain">The sandbox domain the program belongs to, or null for real code. Sandbox code's ticks also check that a
    /// frame of that domain is on top of the call stack (Runtime.Tick(string)).</param>
    /// <param name="voidLambdas">Positions (in the original source) of expression-bodied lambdas that a first compile found to be
    /// converted to void-returning delegates: they become "{ Tick(); expr; }" instead of "{ Tick(); return expr; }".</param>
    public TickInjector(string? domain = null, IReadOnlySet<int>? voidLambdas = null)
    {
        _voidLambdas = voidLambdas ?? new HashSet<int>();
        if (string.IsNullOrEmpty(domain))
        {
            Tick = ParseStatement("global::LesMudOS.Api.Runtime.Tick();");
            _filterCheck = null;
            _pass = ParseExpression("global::LesMudOS.Api.Runtime.Pass()");
        }
        else
        {
            var literal = SymbolDisplay.FormatLiteral(domain, quote: true);
            Tick = ParseStatement($"global::LesMudOS.Api.Runtime.Tick({literal});");
            _filterCheck = ParseExpression($"global::LesMudOS.Api.Runtime.Filter({literal})");
            _pass = ParseExpression($"global::LesMudOS.Api.Runtime.Pass({literal})");
        }
    }

    private BlockSyntax WithTick(BlockSyntax block)
        => block.WithStatements(block.Statements.Insert(0, Tick));

    private StatementSyntax LoopBody(StatementSyntax body)
        => body is BlockSyntax block ? WithTick(block) : Block(Tick, body);

    /// <summary>Turn "=> expr;" into "{ Tick(); return expr; }", keeping all trivia (and so all line breaks).</summary>
    private BlockSyntax ToBlock(ArrowExpressionClauseSyntax arrow, SyntaxToken semicolon, bool returnsValue)
    {
        StatementSyntax statement = arrow.Expression is ThrowExpressionSyntax thrown
            ? ThrowStatement(thrown.Expression).WithThrowKeyword(thrown.ThrowKeyword) // "=> throw e;" (no "return throw")
            : returnsValue
                ? ReturnStatement(arrow.Expression)
                : ExpressionStatement(arrow.Expression);
        return Block(
            Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(arrow.ArrowToken.LeadingTrivia).WithTrailingTrivia(arrow.ArrowToken.TrailingTrivia),
            List(new[] { Tick, statement }),
            Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(semicolon.LeadingTrivia).WithTrailingTrivia(semicolon.TrailingTrivia));
    }

    /// <summary>
    /// The body of an expression-bodied lambda as a block: "{ Tick(); return expr; }", or "{ Tick(); expr; }" for a lambda that is
    /// a void delegate, or "{ Tick(); throw e; }". The expression keeps its own trivia, so no line break is added or lost.
    /// </summary>
    private BlockSyntax LambdaBlock(ExpressionSyntax body, int originalPosition)
    {
        StatementSyntax statement = body is ThrowExpressionSyntax thrown
            ? ThrowStatement(thrown.Expression).WithThrowKeyword(thrown.ThrowKeyword)
            : _voidLambdas.Contains(originalPosition) ? ExpressionStatement(body) : ReturnStatement(body);
        return Block(Token(SyntaxKind.OpenBraceToken), List(new[] { Tick, statement }), Token(SyntaxKind.CloseBraceToken));
    }

    private static bool IsVoid(TypeSyntax type)
        => type is PredefinedTypeSyntax p && p.Keyword.IsKind(SyntaxKind.VoidKeyword);

    // ---- loops ----

    public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
    {
        node = (WhileStatementSyntax)base.VisitWhileStatement(node)!;
        return node.WithStatement(LoopBody(node.Statement));
    }

    public override SyntaxNode? VisitDoStatement(DoStatementSyntax node)
    {
        node = (DoStatementSyntax)base.VisitDoStatement(node)!;
        return node.WithStatement(LoopBody(node.Statement));
    }

    public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
    {
        node = (ForStatementSyntax)base.VisitForStatement(node)!;
        return node.WithStatement(LoopBody(node.Statement));
    }

    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
    {
        node = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;
        return node.WithStatement(LoopBody(node.Statement));
    }

    public override SyntaxNode? VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
    {
        node = (ForEachVariableStatementSyntax)base.VisitForEachVariableStatement(node)!;
        return node.WithStatement(LoopBody(node.Statement));
    }

    // ---- callable bodies ----

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(node)!;
        if (node.Body is not null) return node.WithBody(WithTick(node.Body));
        if (node.ExpressionBody is null) return node; // abstract / extern / partial
        return node
            .WithBody(ToBlock(node.ExpressionBody, node.SemicolonToken, !IsVoid(node.ReturnType)))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
    {
        node = (LocalFunctionStatementSyntax)base.VisitLocalFunctionStatement(node)!;
        if (node.Body is not null) return node.WithBody(WithTick(node.Body));
        if (node.ExpressionBody is null) return node;
        return node
            .WithBody(ToBlock(node.ExpressionBody, node.SemicolonToken, !IsVoid(node.ReturnType)))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        node = (PropertyDeclarationSyntax)base.VisitPropertyDeclaration(node)!;
        if (node.Initializer is not null) node = node.WithInitializer(GuardedInitializer(node.Initializer, node.Type));
        if (node.ExpressionBody is null) return node;
        // "int X => expr;"  ->  "int X { get { Tick(); return expr; } }"
        var getter = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithBody(ToBlock(node.ExpressionBody, node.SemicolonToken, returnsValue: true));
        return node
            .WithAccessorList(AccessorList(SingletonList(getter)))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitAccessorDeclaration(AccessorDeclarationSyntax node)
    {
        node = (AccessorDeclarationSyntax)base.VisitAccessorDeclaration(node)!;
        if (node.Body is not null) return node.WithBody(WithTick(node.Body));
        if (node.ExpressionBody is null) return node; // auto-property accessor
        return node
            .WithBody(ToBlock(node.ExpressionBody, node.SemicolonToken, node.IsKind(SyntaxKind.GetAccessorDeclaration)))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        node = (ConstructorDeclarationSyntax)base.VisitConstructorDeclaration(node)!;
        if (node.Body is not null) return node.WithBody(WithTick(node.Body));
        if (node.ExpressionBody is null) return node; // extern
        return node
            .WithBody(ToBlock(node.ExpressionBody, node.SemicolonToken, returnsValue: false))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    // ---- code that runs outside any body (the second review's T1) ----
    // Field and property initialisers run before a constructor's body, and so do the arguments of ": base(...)", ": this(...)" and
    // of a primary constructor's base type: "R n = new R();" recursed with no tick at all. Each such expression becomes
    // "Runtime.Pass() ? (expr) : default!", which ticks (and checks the stack and, for sandbox code, its frame) before the
    // expression is evaluated, and has the same type (a target-typed conditional where the expression has none of its own).

    private ExpressionSyntax Guarded(ExpressionSyntax value)
        => ConditionalExpression(_pass, ParenthesizedExpression(value.WithoutTrivia()),
                PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, LiteralExpression(SyntaxKind.DefaultLiteralExpression)))
            .WithTriviaFrom(value);

    private EqualsValueClauseSyntax GuardedInitializer(EqualsValueClauseSyntax clause, TypeSyntax? type)
    {
        var value = clause.Value;
        // An array initialiser ("= { a, b }") is not an expression: it is given its type first.
        if (value is InitializerExpressionSyntax list && type is ArrayTypeSyntax arrayType)
            value = ArrayCreationExpression(arrayType.WithoutTrivia(), list.WithoutTrivia()).WithTriviaFrom(list);
        else if (value is InitializerExpressionSyntax) return clause;
        return clause.WithValue(Guarded(value));
    }

    public override SyntaxNode? VisitFieldDeclaration(FieldDeclarationSyntax node)
    {
        node = (FieldDeclarationSyntax)base.VisitFieldDeclaration(node)!;
        if (node.Modifiers.Any(SyntaxKind.ConstKeyword)) return node; // a constant: evaluated by the compiler
        var type = node.Declaration.Type;
        return node.WithDeclaration(node.Declaration.WithVariables(SeparatedList(node.Declaration.Variables.Select(v =>
            v.Initializer is null ? v : v.WithInitializer(GuardedInitializer(v.Initializer, type))), node.Declaration.Variables.GetSeparators())));
    }

    public override SyntaxNode? VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
    {
        node = (EventFieldDeclarationSyntax)base.VisitEventFieldDeclaration(node)!;
        var type = node.Declaration.Type;
        return node.WithDeclaration(node.Declaration.WithVariables(SeparatedList(node.Declaration.Variables.Select(v =>
            v.Initializer is null ? v : v.WithInitializer(GuardedInitializer(v.Initializer, type))), node.Declaration.Variables.GetSeparators())));
    }

    private ArgumentListSyntax GuardedArguments(ArgumentListSyntax list)
    {
        // The first argument that is passed by value is evaluated first of those that can run code: guarding it ticks before any.
        for (var i = 0; i < list.Arguments.Count; i++)
        {
            var argument = list.Arguments[i];
            if (argument.RefKindKeyword.IsKind(SyntaxKind.None) && argument.NameColon is null)
                return list.WithArguments(list.Arguments.Replace(argument, argument.WithExpression(Guarded(argument.Expression))));
        }
        return list;
    }

    public override SyntaxNode? VisitConstructorInitializer(ConstructorInitializerSyntax node)
    {
        node = (ConstructorInitializerSyntax)base.VisitConstructorInitializer(node)!;
        return node.ArgumentList.Arguments.Count == 0 ? node : node.WithArgumentList(GuardedArguments(node.ArgumentList));
    }

    public override SyntaxNode? VisitPrimaryConstructorBaseType(PrimaryConstructorBaseTypeSyntax node)
    {
        node = (PrimaryConstructorBaseTypeSyntax)base.VisitPrimaryConstructorBaseType(node)!;
        return node.ArgumentList.Arguments.Count == 0 ? node : node.WithArgumentList(GuardedArguments(node.ArgumentList));
    }

    public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node)
    {
        node = (OperatorDeclarationSyntax)base.VisitOperatorDeclaration(node)!;
        if (node.Body is not null) return node.WithBody(WithTick(node.Body));
        if (node.ExpressionBody is null) return node;
        return node
            .WithBody(ToBlock(node.ExpressionBody, node.SemicolonToken, returnsValue: true))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
    {
        node = (ConversionOperatorDeclarationSyntax)base.VisitConversionOperatorDeclaration(node)!;
        if (node.Body is not null) return node.WithBody(WithTick(node.Body));
        if (node.ExpressionBody is null) return node;
        return node
            .WithBody(ToBlock(node.ExpressionBody, node.SemicolonToken, returnsValue: true))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    public override SyntaxNode? VisitIndexerDeclaration(IndexerDeclarationSyntax node)
    {
        node = (IndexerDeclarationSyntax)base.VisitIndexerDeclaration(node)!;
        if (node.ExpressionBody is null) return node; // accessors with bodies were ticked by VisitAccessorDeclaration
        var getter = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithBody(ToBlock(node.ExpressionBody, node.SemicolonToken, returnsValue: true));
        return node
            .WithAccessorList(AccessorList(SingletonList(getter)))
            .WithExpressionBody(null)
            .WithSemicolonToken(default);
    }

    // An expression-bodied lambda gets a block too (F1a of the November 2026 review): otherwise "f = x => f(x + 1)" recursed with
    // neither a tick nor a stack check, and overflowed the game thread's stack.

    public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
    {
        var position = node.SpanStart;
        node = (ParenthesizedLambdaExpressionSyntax)base.VisitParenthesizedLambdaExpression(node)!;
        if (node.Block is not null) return node.WithBlock(WithTick(node.Block));
        if (node.ExpressionBody is null) return node;
        return node.WithExpressionBody(null).WithBlock(LambdaBlock(node.ExpressionBody, position))
            .WithAdditionalAnnotations(new SyntaxAnnotation(LambdaAnnotationKind, position.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
    {
        var position = node.SpanStart;
        node = (SimpleLambdaExpressionSyntax)base.VisitSimpleLambdaExpression(node)!;
        if (node.Block is not null) return node.WithBlock(WithTick(node.Block));
        if (node.ExpressionBody is null) return node;
        return node.WithExpressionBody(null).WithBlock(LambdaBlock(node.ExpressionBody, position))
            .WithAdditionalAnnotations(new SyntaxAnnotation(LambdaAnnotationKind, position.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    // A catch filter of sandbox code runs during the search for a handler, while the frames the exception came out of (another
    // object's, or the master's) are still on the stack: it is checked like a body.
    public override SyntaxNode? VisitCatchFilterClause(CatchFilterClauseSyntax node)
    {
        node = (CatchFilterClauseSyntax)base.VisitCatchFilterClause(node)!;
        if (_filterCheck is null) return node;
        return node.WithFilterExpression(BinaryExpression(SyntaxKind.LogicalAndExpression, _filterCheck,
            ParenthesizedExpression(node.FilterExpression.WithoutTrivia())).WithTriviaFrom(node.FilterExpression));
    }

    public override SyntaxNode? VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
    {
        node = (AnonymousMethodExpressionSyntax)base.VisitAnonymousMethodExpression(node)!;
        return node.WithBlock(WithTick(node.Block));
    }
}
