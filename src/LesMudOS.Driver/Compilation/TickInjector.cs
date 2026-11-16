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
    private static readonly StatementSyntax Tick =
        ParseStatement("global::LesMudOS.Api.Runtime.Tick();");

    private static BlockSyntax WithTick(BlockSyntax block)
        => block.WithStatements(block.Statements.Insert(0, Tick));

    private static StatementSyntax LoopBody(StatementSyntax body)
        => body is BlockSyntax block ? WithTick(block) : Block(Tick, body);

    /// <summary>Turn "=> expr;" into "{ Tick(); return expr; }", keeping all trivia (and so all line breaks).</summary>
    private static BlockSyntax ToBlock(ArrowExpressionClauseSyntax arrow, SyntaxToken semicolon, bool returnsValue)
    {
        StatementSyntax statement = returnsValue
            ? ReturnStatement(arrow.Expression)
            : ExpressionStatement(arrow.Expression);
        return Block(
            Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(arrow.ArrowToken.LeadingTrivia).WithTrailingTrivia(arrow.ArrowToken.TrailingTrivia),
            List(new[] { Tick, statement }),
            Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(semicolon.LeadingTrivia).WithTrailingTrivia(semicolon.TrailingTrivia));
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
        return node.Body is null ? node : node.WithBody(WithTick(node.Body));
    }

    public override SyntaxNode? VisitOperatorDeclaration(OperatorDeclarationSyntax node)
    {
        node = (OperatorDeclarationSyntax)base.VisitOperatorDeclaration(node)!;
        return node.Body is null ? node : node.WithBody(WithTick(node.Body));
    }

    public override SyntaxNode? VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
    {
        node = (ConversionOperatorDeclarationSyntax)base.VisitConversionOperatorDeclaration(node)!;
        return node.Body is null ? node : node.WithBody(WithTick(node.Body));
    }

    public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
    {
        node = (ParenthesizedLambdaExpressionSyntax)base.VisitParenthesizedLambdaExpression(node)!;
        return node.Block is null ? node : node.WithBlock(WithTick(node.Block));
    }

    public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
    {
        node = (SimpleLambdaExpressionSyntax)base.VisitSimpleLambdaExpression(node)!;
        return node.Block is null ? node : node.WithBlock(WithTick(node.Block));
    }

    public override SyntaxNode? VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
    {
        node = (AnonymousMethodExpressionSyntax)base.VisitAnonymousMethodExpression(node)!;
        return node.WithBlock(WithTick(node.Block));
    }
}
