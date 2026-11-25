using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace LesMudOS.Web.Tests;

public static class Paths
{
    /// <summary>The web/ directory, found by walking up from the test binaries.</summary>
    public static string WebDir { get; } = Find();
    public static string ProjectDir => Path.Combine(WebDir, "LesMudOS.Web");
    public static string Fixture(string name) => Path.Combine(WebDir, "fixtures", name);

    private static string Find()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !File.Exists(Path.Combine(d.FullName, "Web.sln"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("web/Web.sln not found above " + AppContext.BaseDirectory);
    }

    public static string CopyToTemp(string source)
    {
        var target = Path.Combine(Path.GetTempPath(), "hollowmere-web-test-" + Guid.NewGuid().ToString("N"));
        CopyDir(source, target);
        return target;
    }

    private static void CopyDir(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)));
        foreach (var d in Directory.GetDirectories(from)) CopyDir(d, Path.Combine(to, Path.GetFileName(d)));
    }
}

/// <summary>The site hosted in memory, pointed at a save directory (or none).</summary>
public sealed class Site : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _settings;
    public Site(string? saveDirectory, Dictionary<string, string?>? extra = null)
    {
        _settings = new Dictionary<string, string?> { ["Site:SaveDirectory"] = saveDirectory ?? "", ["Site:CacheSeconds"] = "0", ["Site:BaseUrl"] = "https://example.test" };
        if (extra != null) foreach (var kv in extra) _settings[kv.Key] = kv.Value;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(Paths.ProjectDir);
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(_settings));
        // The in-memory server has no TCP peer: say the requests come from the loopback, as they do behind Caddy.
        builder.ConfigureServices(s => s.AddTransient<IStartupFilter>(_ => new LoopbackPeer()));
        if (Logs != null) builder.ConfigureLogging(l => l.AddProvider(Logs));
        if (Time != null) builder.ConfigureTestServices(s => s.AddSingleton<TimeProvider>(Time));
    }

    public CaptureLogs? Logs { get; set; }
    public TimeProvider? Time { get; set; }

    private sealed class LoopbackPeer : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((ctx, n) => { ctx.Connection.RemoteIpAddress ??= IPAddress.Loopback; return n(ctx); });
            next(app);
        };
    }

    public HttpClient NoRedirectClient() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    public async Task<(int Status, string Body, HttpResponseMessage Response)> Get(string path)
    {
        var r = await CreateClient().GetAsync(path);
        return ((int)r.StatusCode, await r.Content.ReadAsStringAsync(), r);
    }

    /// <summary>The fixed pages, the news posts and the Chronicle entries the sitemap lists.</summary>
    public async Task<List<string>> PublicPaths()
    {
        var (_, xml, _) = await Get("/sitemap.xml");
        return Regex.Matches(xml, "<loc>https://example.test([^<]*)</loc>").Select(m => m.Groups[1].Value).ToList();
    }
}

public static class Html
{
    public static int Count(string html, string pattern) => Regex.Matches(html, pattern, RegexOptions.IgnoreCase).Count;
    public static string Text(string html) => System.Net.WebUtility.HtmlDecode(Regex.Replace(Regex.Replace(html, "<(script|style)[^>]*>.*?</\\1>", " ", RegexOptions.Singleline), "<[^>]+>", " "));
}

/// <summary>Every log line the site wrote, as rendered text, so tests can check what is (and is not) logged.</summary>
public sealed class CaptureLogs : ILoggerProvider
{
    private readonly object _lock = new();
    private readonly List<string> _lines = new();
    public List<string> Lines { get { lock (_lock) return _lines.ToList(); } }
    public ILogger CreateLogger(string categoryName) => new L(this, categoryName);
    public void Dispose() { }
    private sealed class L(CaptureLogs owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (owner._lock) owner._lines.Add(category + ": " + formatter(state, exception) + (exception == null ? "" : " [" + exception.GetType().Name + ": " + exception.Message + "]"));
        }
    }
}
