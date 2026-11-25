using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using LesMudOS.Api;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace LesMudOS.Driver.Compilation;

/// <summary>One compiled version of one mudlib source file.</summary>
internal sealed class MudProgram
{
    public required string Path { get; init; }
    public required int Version { get; init; }
    public required string AssemblyName { get; init; }
    public required Assembly Assembly { get; init; }
    public required MetadataReference Reference { get; init; }
    /// <summary>The MudObject subclass the file defines; null if the file only defines an abstract class.</summary>
    public required Type? ObjectType { get; init; }
    /// <summary>The source text it was compiled from.</summary>
    public string Source { get; init; } = "";
    /// <summary>Transitive closure of the programs this one inherits or uses.</summary>
    public required IReadOnlyList<MudProgram> Dependencies { get; init; }
}

internal sealed class CompileException : MudRuntimeException
{
    public CompileException(string path, IEnumerable<string> errors)
        : base($"Failed to compile {path}:\n  " + string.Join("\n  ", errors)) { }
}

/// <summary>
/// Compiles mudlib .cs files on demand, one assembly per file, each in its own
/// collectible AssemblyLoadContext. Tracks the dependency graph so that
/// updating a base program invalidates everything built on it.
/// </summary>
internal sealed partial class ProgramCompiler
{
    public const string AssemblyPrefix = "lp.";

    private const string GlobalUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Text;
        global using LesMudOS.Api;
        """;

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    private readonly string _mudlibRoot;
    private readonly List<MetadataReference> _platformReferences;
    private readonly SyntaxTree _globalUsingsTree;
    private readonly Dictionary<string, MudProgram> _current = new();
    private readonly Dictionary<string, int> _versions = new();
    private readonly HashSet<string> _compiling = new();
    private readonly List<(string Name, WeakReference Context)> _everLoaded = new();

    /// <summary>
    /// The sandbox domain of a program path ("" for real code), from the master's ObjectDomain. Sandbox code is compiled with ticks
    /// that check its domain (TickInjector), and may build only on real programs and on programs of its own domain.
    /// </summary>
    public Func<string, string> DomainOf { get; set; } = _ => "";

    /// <summary>Told before and after each compile, with the program's domain: the driver's stall watchdog counts a sandbox
    /// program's compile against its domain and pauses for real code's.</summary>
    public Action<string>? CompileStarted { get; set; }
    public Action? CompileEnded { get; set; }

    public ProgramCompiler(string mudlibRoot)
    {
        _mudlibRoot = System.IO.Path.GetFullPath(mudlibRoot);
        _globalUsingsTree = CSharpSyntaxTree.ParseText(GlobalUsings, ParseOptions, "<global usings>", Encoding.UTF8);

        // Mudlib code compiles against the minimum set of framework assemblies
        // plus the driver API. Anything not referenced here cannot even be named.
        string[] wanted = ["System.Private.CoreLib", "System.Runtime", "System.Collections", "System.Linq"];
        var trusted = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(System.IO.Path.PathSeparator);
        _platformReferences = trusted
            .Where(p => wanted.Contains(System.IO.Path.GetFileNameWithoutExtension(p)))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList();
        _platformReferences.Add(MetadataReference.CreateFromFile(typeof(MudObject).Assembly.Location));
    }

    /// <summary>"/std/room.cs" or "std/room" -> "/std/room". Rejects anything that could leave the mudlib.</summary>
    public static string NormalizePath(string path)
    {
        path = path.Trim().Replace('\\', '/');
        if (path.EndsWith(".cs", StringComparison.Ordinal)) path = path[..^3];
        if (!path.StartsWith('/')) path = "/" + path;
        // One spelling per file: no "." or empty segments ("/./wiz/x" or "//wiz/x" would load /wiz/x under another program path).
        if (!PathPattern().IsMatch(path) || path.Contains("..", StringComparison.Ordinal)
            || path.Length < 2 || path.Substring(1).Split('/').Any(segment => segment.Length == 0 || segment == "."))
            throw new MudRuntimeException($"Illegal path '{path}'");
        return path;
    }

    public string SourceFile(string mudPath) => _mudlibRoot + mudPath + ".cs";

    /// <summary>The current program for a path, compiling it (and its dependencies) if needed.</summary>
    public MudProgram Get(string path)
    {
        path = NormalizePath(path);
        return _current.TryGetValue(path, out var program) ? program : Compile(path);
    }

    /// <summary>Is there a current program for the path, compiled from the file as it is now?</summary>
    public bool IsCurrent(string path)
    {
        path = NormalizePath(path);
        if (!_current.TryGetValue(path, out var program)) return false;
        var file = SourceFile(path);
        try { return File.Exists(file) && File.ReadAllText(file) == program.Source; }
        catch (IOException) { return false; }
    }

    /// <summary>Drop the program for a path and every program that depends on it. Returns what was dropped.</summary>
    public IReadOnlyList<string> Invalidate(string path)
    {
        path = NormalizePath(path);
        var stale = _current.Values
            .Where(p => p.Path == path || p.Dependencies.Any(d => d.Path == path))
            .Select(p => p.Path)
            .ToList();
        foreach (var p in stale) _current.Remove(p);
        return stale;
    }

    public (int Current, int StaleAlive, int Unloaded) Statistics()
    {
        var current = _current.Values.Select(p => p.AssemblyName).ToHashSet();
        int staleAlive = 0, unloaded = 0;
        foreach (var (name, context) in _everLoaded)
        {
            if (current.Contains(name)) continue;
            if (context.IsAlive) staleAlive++; else unloaded++;
        }
        return (current.Count, staleAlive, unloaded);
    }

    private MudProgram Compile(string path)
    {
        CompileStarted?.Invoke(DomainOf(path));
        try { return CompileCore(path); }
        finally { CompileEnded?.Invoke(); }
    }

    private MudProgram CompileCore(string path)
    {
        var file = SourceFile(path);
        if (!File.Exists(file)) throw new MudRuntimeException($"No such file: {path}.cs");
        if (!_compiling.Add(path)) throw new CompileException(path, [$"circular @inherit/@use involving {path}"]);
        try
        {
            var source = File.ReadAllText(file);

            // 1. Dependencies, declared with "// @inherit /std/room" or "// @use /std/item".
            var dependencies = new List<MudProgram>();
            foreach (Match match in DirectivePattern().Matches(source))
            {
                var dependency = Get(match.Groups[2].Value);
                foreach (var d in dependency.Dependencies.Append(dependency))
                    if (!dependencies.Contains(d)) dependencies.Add(d);
            }

            // Sandbox code builds only on real programs and on its own domain's: another home's code, or a file directly in /wiz,
            // would run in this program's objects and fail its own domain check at its first statement. Real code never builds on
            // sandbox code.
            var domain = DomainOf(path);
            foreach (var d in dependencies)
            {
                var other = DomainOf(d.Path);
                if (other.Length > 0 && other != domain)
                    throw new CompileException(path, [$"{path}.cs: cannot inherit or use {d.Path}: " +
                        (domain.Length == 0 ? "real code cannot build on a wizard's sandbox" : "it belongs to another wizard's sandbox")]);
            }

            // 2. Parse and inject evaluation-cost ticks. 3. Compile. An expression-bodied lambda becomes a block with a return; one
            // that turns out to be a void delegate (the compiler says so, CS8030) is rewritten without the return and compiled again.
            var parsed = CSharpSyntaxTree.ParseText(source, ParseOptions, file, Encoding.UTF8);
            var version = _versions[path] = _versions.GetValueOrDefault(path) + 1;
            var assemblyName = $"{AssemblyPrefix}{path.Trim('/').Replace('/', '.')}.v{version}";
            var voidLambdas = new HashSet<int>();
            SyntaxTree tree;
            CSharpCompilation compilation;
            List<Diagnostic> failed;
            var pass = 0;
            while (true)
            {
                var rewritten = new TickInjector(domain, voidLambdas).Visit(parsed.GetRoot());
                tree = CSharpSyntaxTree.Create((CSharpSyntaxNode)rewritten, ParseOptions, file, Encoding.UTF8);
                compilation = CSharpCompilation.Create(
                    assemblyName,
                    [_globalUsingsTree, tree],
                    _platformReferences.Concat(dependencies.Select(d => d.Reference)),
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary,
                        optimizationLevel: OptimizationLevel.Debug,
                        allowUnsafe: false,
                        nullableContextOptions: NullableContextOptions.Annotations));
                failed = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                if (failed.Count == 0 || ++pass > 4) break;
                var more = false;
                foreach (var diagnostic in failed.Where(d => d.Id == "CS8030" && d.Location.SourceTree == tree))
                {
                    var node = tree.GetRoot().FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                    var lambda = node.AncestorsAndSelf().FirstOrDefault(n => n.HasAnnotations(TickInjector.LambdaAnnotationKind));
                    var mark = lambda?.GetAnnotations(TickInjector.LambdaAnnotationKind).First().Data;
                    if (mark != null && voidLambdas.Add(int.Parse(mark, System.Globalization.CultureInfo.InvariantCulture))) more = true;
                }
                if (!more) break;
            }
            if (failed.Count > 0) throw new CompileException(path, failed.Select(d => FormatDiagnostic(d, path)).ToList());

            // 4. Sandbox allow-list.
            var violations = SandboxAnalyzer.Analyze(compilation, tree, path);
            if (violations.Count > 0) throw new CompileException(path, violations);

            // 5. Emit to memory and load into a collectible context of its own.
            using var image = new MemoryStream();
            using var symbols = new MemoryStream();
            var emitted = compilation.Emit(image, symbols, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
            if (!emitted.Success)
                throw new CompileException(path, emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => FormatDiagnostic(d, path)));

            var context = new ProgramLoadContext(assemblyName, dependencies);
            image.Position = 0;
            symbols.Position = 0;
            var assembly = context.LoadFromStream(image, symbols);

            // 6. Find the object class.
            var objectTypes = assembly.GetTypes().Where(t => t.IsPublic && typeof(MudObject).IsAssignableFrom(t)).ToList();
            if (objectTypes.Count != 1)
                throw new CompileException(path, [$"{path}.cs must define exactly one public class deriving from MudObject (found {objectTypes.Count})"]);

            var program = new MudProgram
            {
                Path = path,
                Version = version,
                AssemblyName = assemblyName,
                Assembly = assembly,
                Reference = MetadataReference.CreateFromImage(image.ToArray()),
                ObjectType = objectTypes[0].IsAbstract ? null : objectTypes[0],
                Dependencies = dependencies,
                Source = source,
            };
            _current[path] = program;
            _everLoaded.Add((assemblyName, new WeakReference(context)));
            Log.Info($"compiled {path} (v{version})");
            return program;
        }
        finally
        {
            _compiling.Remove(path);
        }
    }

    private static string FormatDiagnostic(Diagnostic diagnostic, string path)
    {
        var position = diagnostic.Location.GetLineSpan().StartLinePosition;
        return $"{path}.cs({position.Line + 1},{position.Character + 1}): {diagnostic.Id}: {diagnostic.GetMessage()}";
    }

    [GeneratedRegex(@"^/[A-Za-z0-9_\-./]+$")]
    private static partial Regex PathPattern();

    [GeneratedRegex(@"^\s*//\s*@(inherit|use)\s+""?([^""\s]+)""?\s*$", RegexOptions.Multiline)]
    private static partial Regex DirectivePattern();

    /// <summary>
    /// Collectible context holding one program version. Sibling mudlib
    /// assemblies resolve to the exact versions this program was compiled
    /// against; everything else falls through to the driver's own context.
    /// </summary>
    private sealed class ProgramLoadContext : AssemblyLoadContext
    {
        private readonly Dictionary<string, Assembly> _dependencies;

        public ProgramLoadContext(string name, IEnumerable<MudProgram> dependencies) : base(name, isCollectible: true)
        {
            _dependencies = dependencies.ToDictionary(d => d.AssemblyName, d => d.Assembly);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
            => assemblyName.Name is { } name && _dependencies.TryGetValue(name, out var assembly) ? assembly : null;
    }
}
