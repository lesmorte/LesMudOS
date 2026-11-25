using Microsoft.AspNetCore.DataProtection;
using System.Security;
using System.Text;
using LesMudOS.Web;
using LesMudOS.Web.Data;
using Microsoft.Extensions.Options;
using LesMudOS.Web.Play;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(k => k.AddServerHeader = false);
// Web players hold upgraded (WebSocket) connections; leave room for the limit the bridge itself enforces.
builder.Services.AddOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>()
    .Configure<IOptions<SiteOptions>>((k, site) => k.Limits.MaxConcurrentUpgradedConnections = site.Value.Play.MaxConnections + 20);
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.Section));
builder.Services.AddSingleton(TimeProvider.System);

string? Find(string relative, params string[] bases)
{
    if (string.IsNullOrWhiteSpace(relative)) return null;
    if (Path.IsPathRooted(relative)) return relative;
    foreach (var b in bases)
    {
        var p = Path.GetFullPath(Path.Combine(b, relative));
        if (File.Exists(p) || Directory.Exists(p)) return p;
    }
    return null;
}

builder.Services.AddSingleton(sp =>
{
    var o = sp.GetRequiredService<IOptions<SiteOptions>>().Value;
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var path = Find(o.FilterFile, env.ContentRootPath, AppContext.BaseDirectory);
    var filter = PublicFilter.Load(path, out var problem);
    if (problem != null) sp.GetRequiredService<ILoggerFactory>().CreateLogger("Filter").LogError("{Problem}: no Chronicle entry will be shown.", problem);
    return filter;
});
builder.Services.AddSingleton(sp =>
{
    var o = sp.GetRequiredService<IOptions<SiteOptions>>().Value;
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var dir = Find(o.ContentDirectory, env.ContentRootPath, Path.Combine(env.ContentRootPath, "..", "content"), AppContext.BaseDirectory);
    var world = WorldContent.Load(dir == null ? null : Path.Combine(dir, "world.json"), out var problem);
    if (problem != null) sp.GetRequiredService<ILoggerFactory>().CreateLogger("Content").LogError("{Problem}", problem);
    return world;
});
builder.Services.AddSingleton(sp =>
{
    var o = sp.GetRequiredService<IOptions<SiteOptions>>().Value;
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var dir = Find(o.ContentDirectory, env.ContentRootPath, Path.Combine(env.ContentRootPath, "..", "content"), AppContext.BaseDirectory);
    return new NewsService(dir == null ? null : Path.Combine(dir, "news"), sp.GetRequiredService<TimeProvider>(), o.CacheSeconds);
});
builder.Services.AddSingleton<GameData>();
builder.Services.AddSingleton<PlayRuntime>();
// No cookies and no forms: nothing is ever protected, so no key files are written to disk.
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddRazorPages();
builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

var app = builder.Build();

app.UseResponseCompression();
{
    var play = app.Services.GetRequiredService<IOptions<SiteOptions>>().Value.Play;
    play.Validate(); // refuses to start on an unsafe bridge host
    app.UseWebSockets(new WebSocketOptions
    {
        KeepAliveInterval = TimeSpan.FromSeconds(play.PingSeconds),
        KeepAliveTimeout = TimeSpan.FromSeconds(play.ClientSilenceSeconds),
    });
}
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/error");
app.UseStatusCodePagesWithReExecute("/not-found");

// Plain headers: no cookies, no trackers, nothing loaded from anywhere but this site.
const string siteCsp = "default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; base-uri 'self'; form-action 'none'; frame-ancestors 'none'";
// The client page also opens its own WebSocket and installs a manifest and a worker; nothing else is added (docs/specs/web-client.md 3.6).
var playCsp = "default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; "
    + $"connect-src 'self' {WsOrigin(app.Services.GetRequiredService<IOptions<SiteOptions>>().Value.BaseUrl)}; manifest-src 'self'; worker-src 'self'; "
    + "base-uri 'self'; form-action 'none'; frame-ancestors 'none'";
app.Use(async (ctx, next) =>
{
    var isPlay = ctx.Request.Path.StartsWithSegments("/play");
    ctx.Response.OnStarting(() =>
    {
        if (ctx.Request.Path.StartsWithSegments("/play/sw.js"))
        {
            ctx.Response.Headers.CacheControl = "no-cache";
            ctx.Response.Headers["Service-Worker-Allowed"] = "/play";
        }
        var h = ctx.Response.Headers;
        h["Content-Security-Policy"] = isPlay ? playCsp : siteCsp;
        h["X-Content-Type-Options"] = "nosniff";
        h["Referrer-Policy"] = "strict-origin-when-cross-origin";
        h["X-Frame-Options"] = "DENY";
        h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), interest-cohort=()";
        if (ctx.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true && !h.ContainsKey("Cache-Control"))
            h["Cache-Control"] = "public, max-age=60";
        return Task.CompletedTask;
    });
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    // Files requested with ?v=hash (asp-append-version) never change under that address, so they are cached for a year.
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = ctx.Context.Request.Query.ContainsKey("v")
        ? "public, max-age=31536000, immutable" : "public, max-age=3600"
});
app.MapRazorPages();
app.MapPlay();

app.MapGet("/healthz", () => Results.Text("ok", "text/plain"));

app.MapGet("/robots.txt", (IOptions<SiteOptions> o) =>
    Results.Text($"User-agent: *\nAllow: /\nDisallow: /play/ws\nDisallow: /play/status\n\nSitemap: {o.Value.BaseUrl.TrimEnd('/')}/sitemap.xml\n", "text/plain"));

app.MapGet("/sitemap.xml", (IOptions<SiteOptions> o, GameData data, NewsService news) =>
{
    var b = o.Value.BaseUrl.TrimEnd('/');
    var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
    void Add(string path, string? lastmod = null)
        => sb.Append("  <url><loc>").Append(SecurityElement.Escape(b + path)).Append("</loc>").Append(lastmod == null ? "" : "<lastmod>" + lastmod + "</lastmod>").Append("</url>\n");
    foreach (var p in SitePages.Static) Add(p);
    foreach (var post in news.Posts) Add("/news/" + post.Slug, post.Date.ToString("yyyy-MM-dd"));
    foreach (var e in data.Snapshot.Entries.OrderBy(e => e.Id)) Add("/chronicle/" + e.Id);
    sb.Append("</urlset>\n");
    return Results.Text(sb.ToString(), "application/xml");
});

app.Run();

static string WsOrigin(string baseUrl)
{
    // https://example.org -> wss://example.org (older Safari does not count 'self' as covering WebSocket)
    var b = baseUrl.TrimEnd('/');
    if (b.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return "wss://" + b[8..];
    if (b.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return "ws://" + b[7..];
    return "";
}

/// <summary>Lets the integration tests start the site.</summary>
public partial class Program { }
