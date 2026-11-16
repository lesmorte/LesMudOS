using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LesMudOS.Driver.Compilation;

/// <summary>
/// Allow-list check over a compiled mudlib file. Every type and member the
/// source touches, and the type of every expression, must come from the
/// mudlib itself or from an approved namespace.
///
/// This is a guard rail for trusted builders, not a security boundary: .NET
/// has no in-process sandbox. See docs/DESIGN.md, "Sandboxing".
/// </summary>
internal static class SandboxAnalyzer
{
    private static readonly HashSet<string> AllowedNamespaces = new(StringComparer.Ordinal)
    {
        "System",
        "System.Collections",
        "System.Collections.Generic",
        "System.Linq",
        "System.Text",
        "LesMudOS.Api",
    };

    /// <summary>Types inside an allowed namespace that are still off limits.</summary>
    private static readonly HashSet<string> DeniedTypes = new(StringComparer.Ordinal)
    {
        "System.Activator", "System.AppContext", "System.AppDomain", "System.Console",
        "System.Environment", "System.GC", "System.Type", "System.RuntimeTypeHandle",
        "System.RuntimeMethodHandle", "System.RuntimeFieldHandle", "System.WeakReference",
        "System.IntPtr", "System.UIntPtr", "System.Delegate", "System.MulticastDelegate",
        "System.TimeZoneInfo", "System.GCMemoryInfo", "System.LoaderOptimization",
    };

    private static readonly HashSet<string> DeniedMembers = new(StringComparer.Ordinal)
    {
        "System.Object.GetType",
        "System.Exception.TargetSite",
    };

    public static List<string> Analyze(CSharpCompilation compilation, SyntaxTree tree, string mudPath)
    {
        var errors = new List<string>();
        var seen = new HashSet<string>();
        var model = compilation.GetSemanticModel(tree);

        void Report(SyntaxNode node, string what)
        {
            var line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var message = $"{mudPath}.cs({line}): sandbox: {what}";
            if (seen.Add(message)) errors.Add(message);
        }

        void CheckType(SyntaxNode node, ITypeSymbol? type)
        {
            switch (type)
            {
                case null or ITypeParameterSymbol or IErrorTypeSymbol or IDynamicTypeSymbol:
                    return;
                case IArrayTypeSymbol array:
                    CheckType(node, array.ElementType);
                    return;
                case IPointerTypeSymbol or IFunctionPointerTypeSymbol:
                    Report(node, "pointer types are not allowed");
                    return;
                case INamedTypeSymbol named:
                    foreach (var arg in named.TypeArguments) CheckType(node, arg);
                    if (named.IsAnonymousType || IsMudlibAssembly(compilation, named.ContainingAssembly)) return;
                    var definition = named.OriginalDefinition;
                    var full = FullName(definition);
                    var ns = definition.ContainingNamespace?.ToDisplayString() ?? "";
                    if (!AllowedNamespaces.Contains(ns) || DeniedTypes.Contains(full))
                        Report(node, $"type '{full}' is not available to mudlib code");
                    return;
            }
        }

        void CheckSymbol(SyntaxNode node, ISymbol? symbol)
        {
            switch (symbol)
            {
                case null or INamespaceSymbol or ILocalSymbol or IParameterSymbol
                    or ILabelSymbol or IRangeVariableSymbol or ITypeParameterSymbol:
                    return;
                case ITypeSymbol type:
                    CheckType(node, type);
                    return;
            }
            var original = symbol.OriginalDefinition;
            if (original.ContainingType is { } owner)
            {
                CheckType(node, owner);
                if (DeniedMembers.Contains($"{FullName(owner.OriginalDefinition)}.{original.Name}"))
                    Report(node, $"'{original.Name}' is not available to mudlib code");
            }
        }

        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            switch (node)
            {
                case GotoStatementSyntax:
                    Report(node, "goto is not allowed (it can loop without a tick)");
                    continue;
                case TypeOfExpressionSyntax:
                    Report(node, "typeof is not allowed (reflection)");
                    continue;
                case DestructorDeclarationSyntax:
                    Report(node, "finalizers are not allowed (they run outside the game loop)");
                    continue;
                case UnsafeStatementSyntax or FixedStatementSyntax or StackAllocArrayCreationExpressionSyntax:
                    Report(node, "unsafe code is not allowed");
                    continue;
                case AwaitExpressionSyntax:
                    Report(node, "async/await is not supported in this version of the driver");
                    continue;
                case LockStatementSyntax:
                    Report(node, "lock is not allowed (mudlib code is single-threaded)");
                    continue;
            }

            if (node is SimpleNameSyntax or BaseObjectCreationExpressionSyntax or AttributeSyntax
                or ElementAccessExpressionSyntax or BinaryExpressionSyntax or PrefixUnaryExpressionSyntax
                or PostfixUnaryExpressionSyntax or CastExpressionSyntax or AssignmentExpressionSyntax)
            {
                CheckSymbol(node, model.GetSymbolInfo(node).Symbol);
            }

            if (node is ExpressionSyntax expression)
            {
                var info = model.GetTypeInfo(expression);
                CheckType(node, info.Type);
                CheckType(node, info.ConvertedType);
            }
            else if (node is ForEachStatementSyntax forEach)
            {
                var info = model.GetForEachStatementInfo(forEach);
                CheckSymbol(node, info.GetEnumeratorMethod);
                CheckType(node, info.ElementType);
            }
            else if (node is VariableDeclaratorSyntax or ParameterSyntax)
            {
                CheckSymbol(node, model.GetDeclaredSymbol(node) switch
                {
                    ILocalSymbol local => local.Type,
                    IFieldSymbol field => field.Type,
                    IParameterSymbol parameter => parameter.Type,
                    _ => null,
                });
            }
        }
        return errors;
    }

    private static bool IsMudlibAssembly(CSharpCompilation compilation, IAssemblySymbol? assembly)
        => assembly is not null
           && (SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly)
               || assembly.Name.StartsWith(ProgramCompiler.AssemblyPrefix, StringComparison.Ordinal));

    private static string FullName(INamedTypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .WithGenericsOptions(SymbolDisplayGenericsOptions.None)
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.None));
}
