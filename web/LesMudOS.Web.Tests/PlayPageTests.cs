using System.Text.RegularExpressions;
using Xunit;

namespace LesMudOS.Web.Tests;

/// <summary>The client page, the site's links to it, its headers, and static checks of its script.</summary>
public class PlayPageTests
{
    private static Site Open(bool enabled = true, Dictionary<string, string?>? extra = null)
    {
        var s = new Dictionary<string, string?> { ["Site:Play:Enabled"] = enabled ? "true" : "false" };
        if (extra != null) foreach (var kv in extra) s[kv.Key] = kv.Value;
        return new Site(null, s);
    }

    private static string Csp(HttpResponseMessage r) => string.Join(";", r.Headers.GetValues("Content-Security-Policy"));

    [Fact]
    public async Task The_client_page_has_the_per_path_policy_and_nothing_inline()
    {
        using var site = Open();
        var (status, html, r) = await site.Get("/play");
        Assert.Equal(200, status);
        var csp = Csp(r);
        Assert.Contains("connect-src 'self' wss://example.test", csp);
        Assert.Contains("manifest-src 'self'", csp);
        Assert.Contains("worker-src 'self'", csp);
        Assert.Contains("script-src 'self'", csp);
        Assert.DoesNotContain("unsafe", csp);
        Assert.Contains("default-src 'none'", csp);
        // no inline script, no inline style, no handlers, nothing from another site
        foreach (Match m in Regex.Matches(html, "<script\\b[^>]*>", RegexOptions.IgnoreCase)) Assert.Contains("src=", m.Value);
        Assert.DoesNotContain("<style", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"\sstyle\s*=", html);
        Assert.DoesNotMatch(@"\son[a-z]+\s*=", html);
        Assert.DoesNotMatch(@"(src|href|action)\s*=\s*""(https?:)?//", html);
        Assert.Contains("href=\"/css/play.css", html);
        Assert.Contains("src=\"/js/play.js", html);
        Assert.Contains("viewport-fit=cover", html);
        Assert.Contains("<meta name=\"robots\" content=\"noindex\">", html);
        Assert.Contains("role=\"log\"", html);
        Assert.Contains("aria-live=\"polite\"", html);
        Assert.Contains("<label for=\"play-input\"", html);
        Assert.Equal(1, Html.Count(html, "<h1[ >]"));
    }

    [Fact]
    public async Task Other_pages_keep_the_site_wide_policy()
    {
        using var site = Open();
        var (_, _, r) = await site.Get("/how-to-play");
        var csp = Csp(r);
        Assert.DoesNotContain("connect-src", csp);
        Assert.DoesNotContain("manifest-src", csp);
        Assert.Contains("default-src 'none'", csp);
        var (_, _, home) = await site.Get("/");
        Assert.DoesNotContain("connect-src", Csp(home));
    }

    [Fact]
    public async Task The_client_page_is_not_indexed_and_the_instructions_are()
    {
        using var site = Open();
        var paths = await site.PublicPaths();
        Assert.DoesNotContain("/play", paths);
        Assert.Contains("/how-to-play", paths);
        var (_, robots, _) = await site.Get("/robots.txt");
        Assert.Contains("Disallow: /play/ws", robots);
        Assert.Contains("Disallow: /play/status", robots);
        var (_, how, _) = await site.Get("/how-to-play");
        Assert.DoesNotContain("noindex", how);
    }

    [Fact]
    public async Task With_the_client_on_the_site_leads_to_it_and_the_instructions_offer_it()
    {
        using var site = Open(true);
        var (_, home, _) = await site.Get("/");
        Assert.Contains("<a class=\"btn\" href=\"/play\">Play Now</a>", home);
        Assert.Contains("href=\"/play\">Play in your browser</a>", home);
        Assert.Contains("href=\"/how-to-play\">Other ways to connect</a>", home);
        Assert.DoesNotContain("Download Client", home);
        Assert.Contains("href=\"/how-to-play#first-login\">Create Account", home);
        Assert.Contains("<li class=\"play\"><a href=\"/play\"", home);
        Assert.Contains("<a href=\"/how-to-play\">How to Play</a>", home);
        var (_, how, _) = await site.Get("/how-to-play");
        Assert.Contains("Play in your browser", how);
        Assert.Contains("href=\"/play\">Play now</a>", how);
        Assert.Contains("telnet play.legendsofhollowmere.com 4000", how);
        Assert.Contains("not encrypted", how);
        var (_, world, _) = await site.Get("/world");
        Assert.Contains("href=\"/play\">Walk the road", world);
    }

    [Fact]
    public async Task With_the_client_off_nothing_changes_for_visitors()
    {
        using var site = Open(false);
        var (_, home, _) = await site.Get("/");
        Assert.DoesNotContain("Play in your browser", home);
        Assert.Contains("<a class=\"btn\" href=\"/how-to-play\">Play Now</a>", home);
        Assert.Contains("<li class=\"play\"><a href=\"/how-to-play\"", home);
        Assert.Contains("Download Client", home);
        var (_, how, _) = await site.Get("/how-to-play");
        Assert.DoesNotContain("Play in your browser", how);
        Assert.Contains("Download Client", how);
        var (_, world, _) = await site.Get("/world");
        Assert.Contains("href=\"/how-to-play\">Walk the road", world);
    }

    [Fact]
    public async Task The_defaults_of_a_site_with_no_play_settings_at_all_have_the_client_off()
    {
        using var site = new Site(null);
        Assert.Contains("\"enabled\":false", (await site.Get("/play/status")).Body);
        Assert.Contains("switched off", (await site.Get("/play")).Body);
    }

    [Fact]
    public async Task The_status_is_small_and_not_cached()
    {
        using var site = Open();
        var (status, body, r) = await site.Get("/play/status");
        Assert.Equal(200, status);
        Assert.Equal("{\"v\":1,\"enabled\":true,\"full\":false}", body);
        Assert.Equal("no-store", r.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task The_news_post_and_the_support_page_point_to_the_instructions()
    {
        using var site = Open();
        Assert.Contains("(/how-to-play)", File.ReadAllText(Path.Combine(Paths.WebDir, "content", "news", "2026-11-18-the-gates-are-open.md")));
        var (_, support, _) = await site.Get("/support");
        Assert.Contains("<a href=\"/how-to-play\">How to play</a>", support);
        var (_, post, _) = await site.Get("/news/the-gates-are-open");
        Assert.Contains("href=\"/how-to-play\"", post);
    }

    // ---- static checks of the script ----

    private static string Js => File.ReadAllText(Path.Combine(Paths.ProjectDir, "wwwroot", "js", "play.js"));

    [Fact]
    public void The_script_never_builds_markup_from_text()
    {
        var js = Js;
        foreach (var banned in new[] { "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write", "eval(", "new Function", "srcdoc", "javascript:", "DOMParser", "createContextualFragment", "setAttribute('style'", "setAttribute(\"style\"", "cssText" })
            Assert.False(js.Contains(banned, StringComparison.Ordinal), $"play.js must not contain {banned}");
        Assert.DoesNotMatch(@"set(Timeout|Interval)\s*\(\s*['""`]", js);
        Assert.DoesNotMatch(@"\.(href|src)\s*=", js);
        Assert.DoesNotMatch(@"setAttribute\(\s*['""](href|src|style|on[a-z]+)['""]", js);
    }

    [Fact]
    public void The_script_makes_no_request_but_the_status_and_opens_only_a_same_origin_socket()
    {
        var js = Js;
        Assert.DoesNotContain("XMLHttpRequest", js);
        Assert.DoesNotContain("sendBeacon", js);
        Assert.DoesNotContain("importScripts", js);
        Assert.DoesNotMatch(@"https?://", js.Replace("'https:'", ""));
        Assert.Single(Regex.Matches(js, @"\bfetch\("));
        Assert.Contains("win.fetch(statusPath", js);
        Assert.Contains("data-status", js);
        Assert.Single(Regex.Matches(js, @"new win\.WebSocket\("));
        Assert.Contains("new win.WebSocket(wsUrl(), protocol)", js);
        Assert.Contains("(win.location.protocol === 'https:' ? 'wss:' : 'ws:') + '//' + win.location.host + wsPath", js);
        Assert.DoesNotContain("document.cookie", js);
        Assert.DoesNotContain("sessionStorage", js);
        // storage: only the one settings key, never anything typed
        Assert.Single(Regex.Matches(js, @"'hm\.[a-z.0-9]+'"));
    }

    [Fact]
    public void The_stylesheet_and_page_load_nothing_from_another_site()
    {
        var css = File.ReadAllText(Path.Combine(Paths.ProjectDir, "wwwroot", "css", "play.css"));
        Assert.DoesNotMatch(@"url\(\s*['""]?(https?:)?//", css);
        Assert.DoesNotContain("@import", css);
        Assert.DoesNotContain("@font-face", css);
    }

    [Fact]
    public void The_static_file_provider_knows_the_webmanifest_type_for_stage_3()
    {
        // Settles one of the spec's unverified points: no explicit mapping in Program.cs is needed when this holds.
        Assert.True(new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider().TryGetContentType("manifest.webmanifest", out var type));
        Assert.Equal("application/manifest+json", type);
    }
}
