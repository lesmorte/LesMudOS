using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
