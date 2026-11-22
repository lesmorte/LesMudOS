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

            // 2. Parse and inject evaluation-cost ticks.
            var parsed = CSharpSyntaxTree.ParseText(source, ParseOptions, file, Encoding.UTF8);
            var rewritten = new TickInjector().Visit(parsed.GetRoot());
            var tree = CSharpSyntaxTree.Create((CSharpSyntaxNode)rewritten, ParseOptions, file, Encoding.UTF8);

            // 3. Compile.
            var version = _versions[path] = _versions.GetValueOrDefault(path) + 1;
            var assemblyName = $"{AssemblyPrefix}{path.Trim('/').Replace('/', '.')}.v{version}";
            var compilation = CSharpCompilation.Create(
                assemblyName,
                [_globalUsingsTree, tree],
                _platformReferences.Concat(dependencies.Select(d => d.Reference)),
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    allowUnsafe: false,
                    nullableContextOptions: NullableContextOptions.Annotations));

            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => FormatDiagnostic(d, path))
                .ToList();
            if (errors.Count > 0) throw new CompileException(path, errors);

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
