using System.Text.RegularExpressions;
using LesMudOS.Web.Data;
using Xunit;

namespace LesMudOS.Web.Tests;

public class SiteTests
{
    private static readonly string[] Fixed = { "/", "/chronicle", "/heroes", "/world", "/classes", "/factions", "/news", "/play", "/rules", "/support" };
    private static readonly string[] VisibleMixedIds = { "1", "4", "6", "7", "8", "13" };

    public static IEnumerable<object[]> Sources()
    {
        yield return new object[] { "mixed" };
        yield return new object[] { "real" };
        yield return new object[] { "empty" };
        yield return new object[] { "missing" };
    }

    private static Site Open(string kind, Dictionary<string, string?>? extra = null)
    {
        string? dir = kind switch
        {
            "mixed" => Paths.Fixture("mixed"),
            "real" => Paths.Fixture("real"),
            "empty" => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "hollowmere-empty-" + Guid.NewGuid().ToString("N"))).FullName,
            _ => Path.Combine(Path.GetTempPath(), "hollowmere-does-not-exist-" + Guid.NewGuid().ToString("N")),
        };
        return new Site(dir, extra);
    }

    private static async Task<Dictionary<string, string>> AllPages(Site site)
    {
        var pages = new Dictionary<string, string>();
        foreach (var p in await site.PublicPaths())
        {
            var (status, body, _) = await site.Get(p);
            Assert.True(status == 200, $"{p} returned {status}");
            pages[p] = body;
        }
        return pages;
    }

    [Theory, MemberData(nameof(Sources))]
    public async Task Every_page_returns_200(string kind)
    {
        using var site = Open(kind);
        var paths = await site.PublicPaths();
        foreach (var p in Fixed) Assert.Contains(p, paths);
        Assert.Contains("/news/the-gates-are-open", paths);
        foreach (var p in paths)
        {
            var (status, _, _) = await site.Get(p);
            Assert.True(status == 200, $"{kind}: {p} returned {status}");
        }
        foreach (var extra in new[] { "/healthz", "/robots.txt", "/sitemap.xml" })
            Assert.Equal(200, (await site.Get(extra)).Status);
    }

    [Theory, MemberData(nameof(Sources))]
    public async Task Pages_have_one_h1_a_title_a_description_and_a_canonical_link(string kind)
    {
        using var site = Open(kind);
        foreach (var (path, html) in await AllPages(site))
        {
            Assert.True(Html.Count(html, "<h1[ >]") == 1, $"{path}: h1 count");
            Assert.Matches(@"<title>[^<]+</title>", html);
            Assert.Matches("<meta name=\"description\" content=\"[^\"]{20,}\"", html);
            Assert.Matches($"<link rel=\"canonical\" href=\"https://example.test{Regex.Escape(path == "/" ? "/" : path)}\"", html);
            Assert.Contains("<html lang=\"en\">", html);
            Assert.Contains("class=\"skip\"", html);
            Assert.Contains("<main id=\"main\"", html);
        }
    }

    [Fact]
    public async Task Top_five_on_the_home_page_are_the_five_best_public_entries_in_descending_order()
    {
        using var site = Open("mixed");
        var (_, home, _) = await site.Get("/");
        var section = Regex.Match(home, "<section class=\"band alt\" aria-labelledby=\"chronicle\">.*?</section>", RegexOptions.Singleline).Value;
        var titles = Regex.Matches(section, "<h3><a href=\"/chronicle/\\d+\">([^<]+)</a></h3>").Select(m => System.Net.WebUtility.HtmlDecode(m.Groups[1].Value)).ToList();
        Assert.Equal(new[] { "The First to Reach Level 10", "The First Knight", "The Fall of the Gaunt Stag", "The Goblin Purge of Greenvale", "Mira Is Renowned" }, titles);
        var scores = Regex.Matches(section, "ESS (\\d+)").Select(m => int.Parse(m.Groups[1].Value)).ToList();
        Assert.Equal(new[] { 4200, 4020, 2750, 1125, 300 }, scores);
    }

    [Fact]
    public async Task A_hidden_discovery_with_the_highest_score_appears_on_no_page_and_not_in_the_sitemap()
    {
        using var site = Open("mixed");
        var pages = await AllPages(site);
        foreach (var (path, html) in pages)
        {
            var text = Html.Text(html);
            foreach (var bad in new[] { "Root Warrens", "Orders from the Dark", "Hidden Grove", "99999", "50000", "Blood Ink", "A Quiet Marvel", "No Event Recorded", "Quieting of the Cellars", "Task at the Stump" })
                Assert.DoesNotContain(bad, text);
        }
        var entryPaths = (await site.PublicPaths()).Where(p => p.StartsWith("/chronicle/")).Select(p => p[("/chronicle/".Length)..]).OrderBy(x => x).ToArray();
        Assert.Equal(VisibleMixedIds.OrderBy(x => x).ToArray(), entryPaths);
        foreach (var hidden in new[] { 2, 3, 5, 9, 10, 11, 12, 14, 15, 16 })
            Assert.Equal(404, (await site.Get("/chronicle/" + hidden)).Status);
    }

    [Fact]
    public async Task Real_game_output_shows_no_hidden_discovery()
    {
        using var site = Open("real");
        var pages = await AllPages(site);
        Assert.DoesNotContain("Great Stump", string.Join(" ", pages.Values));
        Assert.Equal(404, (await site.Get("/chronicle/1")).Status);
        Assert.Contains("first page is still blank", pages["/chronicle"]);
        var heroes = pages["/heroes"];
        Assert.Contains("Bryn", heroes); Assert.Contains("Cade", heroes); Assert.DoesNotContain("Alice", heroes);
        Assert.Contains("Warrior", heroes); Assert.Contains("Scout", heroes);
    }

    [Fact]
    public async Task A_wizard_with_the_highest_renown_is_not_among_the_heroes()
    {
        using var site = Open("mixed");
        var (_, html, _) = await site.Get("/heroes");
        var names = Regex.Matches(html, "<span class=\"name\">([^<]+)</span>").Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(new[] { "Mira", "Dara", "Bryn", "Cade", "Eda", "Zed" }, names);   // not Alice (99999) or Wilder (7000); "gone" has no character
        foreach (var (path, page) in await AllPages(site))
        {
            Assert.DoesNotContain("Alice", Html.Text(page));
            Assert.DoesNotContain("Wilder", Html.Text(page));
        }
        Assert.Contains("Knight", html);
    }

    [Fact]
    public async Task The_sanitized_directory_written_by_the_deployment_sync_job_works_the_same()
    {
        // fixtures/synced is what deploy/setup-website.sh's lesmudos-web-sync wrote from fixtures/mixed: flat files and a roster without hashes
        Assert.DoesNotContain("passwordHash", string.Concat(Directory.GetFiles(Paths.Fixture("synced")).Select(File.ReadAllText)));
        using var site = new Site(Paths.Fixture("synced"));
        var (_, html, _) = await site.Get("/heroes");
        var names = Regex.Matches(html, "<span class=\"name\">([^<]+)</span>").Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(new[] { "Mira", "Dara", "Bryn", "Cade", "Eda", "Zed" }, names);
        Assert.Contains("Knight", html);
        Assert.Equal(VisibleMixedIds.Length + Fixed.Length + 1, (await site.PublicPaths()).Count);
    }

    [Fact]
    public async Task The_home_page_strip_shows_only_real_numbers()
    {
        using var site = Open("mixed");
        var (_, home, _) = await site.Get("/");
        Assert.Matches(@"Characters created</dt><dd>6</dd>", home);   // eight files, two wizards
        Assert.Matches(@"Chronicle entries</dt><dd>6</dd>", home);
        Assert.Matches(@"Today in Hollowmere</dt><dd>the \d+(st|nd|rd|th) Day of", home);
        using var empty = Open("empty");
        var (_, home2, _) = await empty.Get("/");
        Assert.DoesNotContain("Characters created", home2);
        Assert.DoesNotContain("Chronicle entries", home2);
        Assert.DoesNotContain("Today in Hollowmere", home2);
        Assert.Contains("first page is still blank", home2);
        Assert.Contains("roll of honour is blank", home2);
    }

    [Fact]
    public async Task A_truncated_json_file_does_not_break_a_page()
    {
        var dir = Paths.CopyToTemp(Paths.Fixture("mixed"));
        foreach (var name in new[] { "chronicle", "renown", "wizlist" })
        {
            var f = Path.Combine(dir, "secure", name + ".json");
            var text = File.ReadAllText(f);
            File.WriteAllText(f, text[..(text.Length / 2)]);
        }
        File.WriteAllText(Path.Combine(dir, "players", "mira.json"), "{ \"archetype\": \"war");
        using var site = new Site(dir);
        foreach (var p in new[] { "/", "/chronicle", "/heroes", "/world", "/chronicle?view=top&page=3" })
            Assert.Equal(200, (await site.Get(p)).Status);
        var (_, heroes, _) = await site.Get("/heroes");
        Assert.Contains("roll of honour is blank", heroes);
    }

    [Fact]
    public async Task A_file_that_breaks_after_a_good_read_keeps_the_last_good_copy()
    {
        var dir = Paths.CopyToTemp(Paths.Fixture("mixed"));
        using var site = new Site(dir);
        Assert.Contains("Mira", (await site.Get("/heroes")).Body);
        File.WriteAllText(Path.Combine(dir, "secure", "renown.json"), "{\"renown\": {\"mir");
        Assert.Contains("Mira", (await site.Get("/heroes")).Body);
    }

    [Fact]
    public async Task News_posts_come_from_the_content_directory()
    {
        var content = Path.Combine(Path.GetTempPath(), "hollowmere-content-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(content, "news"));
        File.Copy(Path.Combine(Paths.WebDir, "content", "world.json"), Path.Combine(content, "world.json"));
        File.WriteAllText(Path.Combine(content, "news", "2026-01-02-a-test-post.md"), "---\ntitle: A test post\ndate: 2026-01-02\nsummary: Testing the crier.\n---\nHello **world** <script>alert(1)</script>\n");
        File.WriteAllText(Path.Combine(content, "news", "2099-01-01-from-the-future.md"), "---\ntitle: From the future\ndate: 2099-01-01\n---\nNot yet.\n");
        File.WriteAllText(Path.Combine(content, "news", "2026-01-03-draft.md"), "---\ntitle: A draft\ndate: 2026-01-03\ndraft: true\n---\nHidden.\n");
        File.WriteAllText(Path.Combine(content, "news", "broken.md"), "no header at all");
        using var site = Open("empty", new() { ["Site:ContentDirectory"] = content });
        var (status, post, _) = await site.Get("/news/a-test-post");
        Assert.Equal(200, status);
        Assert.Contains("<strong>world</strong>", post);
        Assert.DoesNotContain("<script>alert", post);
        var (_, list, _) = await site.Get("/news");
        Assert.Contains("A test post", list);
        Assert.DoesNotContain("From the future", list);
        Assert.DoesNotContain("A draft", list);
        Assert.DoesNotContain("the-gates-are-open", list);   // only what the directory holds
        Assert.Equal(404, (await site.Get("/news/from-the-future")).Status);
        Assert.Contains("/news/a-test-post", await site.PublicPaths());
    }

    [Fact]
    public async Task The_real_news_post_announces_the_site_and_is_dated_today_or_earlier()
    {
        using var site = Open("empty");
        var (status, html, _) = await site.Get("/news/the-gates-are-open");
        Assert.Equal(200, status);
        Assert.Contains("2026-11-18", html);
        Assert.Single(Directory.GetFiles(Path.Combine(Paths.WebDir, "content", "news"), "*.md"));
    }

    [Fact]
    public async Task The_sitemap_lists_every_public_page()
    {
        using var site = Open("mixed");
        var paths = await site.PublicPaths();
        foreach (var p in Fixed) Assert.Contains(p, paths);
        Assert.Equal(Fixed.Length + 1 + VisibleMixedIds.Length, paths.Count);
        var (_, robots, _) = await site.Get("/robots.txt");
        Assert.Contains("Sitemap: https://example.test/sitemap.xml", robots);
    }

    [Fact]
    public async Task No_page_loads_anything_from_another_host_and_sets_no_cookie()
    {
        using var site = Open("mixed");
        var offsite = new Regex(@"(?:src|href|data|poster|action)\s*=\s*""(?:https?:)?//(?!example\.test)[^""]*""|url\(\s*['""]?(?:https?:)?//|@import\s+(?:url\()?['""]?(?:https?:)?//|xlink:href\s*=\s*""https?://", RegexOptions.IgnoreCase);
        foreach (var (path, html) in await AllPages(site))
        {
            // an ordinary link (a) to another site would be allowed; a resource would not. There are no such links without configuration.
            Assert.False(offsite.IsMatch(html), $"{path} references another host");
            Assert.DoesNotContain("<iframe", html);
        }
        foreach (var asset in new[] { "/css/site.css", "/js/site.js", "/favicon.svg" })
        {
            var (status, body, response) = await site.Get(asset);
            Assert.Equal(200, status);
            Assert.False(offsite.IsMatch(body), asset);
            Assert.False(response.Headers.Contains("Set-Cookie"), asset);
        }
        var (_, _, r) = await site.Get("/");
        Assert.False(r.Headers.Contains("Set-Cookie"));
        Assert.Contains("default-src 'none'", string.Join(";", r.Headers.GetValues("Content-Security-Policy")));
    }

    [Fact]
    public async Task The_discord_link_appears_only_when_it_is_configured()
    {
        using var without = Open("empty");
        Assert.DoesNotContain("Discord", (await without.Get("/")).Body);
        using var with = Open("empty", new() { ["Site:DiscordUrl"] = "https://discord.example/invite/abc" });
        var (_, html, _) = await with.Get("/");
        Assert.Contains("href=\"https://discord.example/invite/abc\"", html);
    }

    [Fact]
    public async Task The_play_page_points_to_the_mudlet_guide_and_links_it_only_when_configured()
    {
        using var without = Open("empty");
        var (_, plain, _) = await without.Get("/play");
        Assert.Contains("docs/MUDLET.md", plain);
        Assert.DoesNotContain("how to install the Mudlet map", plain);
        using var with = Open("empty", new() { ["Site:MudletDocsUrl"] = "https://docs.example/MUDLET.md" });
        var (_, linked, _) = await with.Get("/play");
        Assert.Contains("href=\"https://docs.example/MUDLET.md\"", linked);
        Assert.Contains("how to install the Mudlet map", linked);
    }

    [Fact]
    public async Task Play_page_uses_the_configured_address_and_port()
    {
        using var site = Open("empty", new() { ["Site:GameHost"] = "play.example", ["Site:GamePort"] = "4242" });
        var (_, html, _) = await site.Get("/play");
        Assert.Contains("telnet play.example 4242", html);
        Assert.Contains("not encrypted", html);
        Assert.Contains("Download Client", html);
        Assert.DoesNotMatch(@"<a[^>]*>\s*Download Client", html);
    }

    [Fact]
    public async Task Unknown_pages_get_a_404_in_the_worlds_voice()
    {
        using var site = Open("empty");
        var (status, html, _) = await site.Get("/no/such/road");
        Assert.Equal(404, status);
        Assert.Contains("wandered off the road", html);
        Assert.Equal(1, Html.Count(html, "<h1[ >]"));
    }

    [Fact]
    public async Task A_missing_filter_file_hides_every_entry()
    {
        using var site = Open("mixed", new() { ["Site:FilterFile"] = "no-such-filter.json" });
        var (_, html, _) = await site.Get("/chronicle");
        Assert.Contains("first page is still blank", html);
    }

    [Fact]
    public async Task Rendered_site_and_sources_never_contain_forbidden_terms()
    {
        var filter = PublicFilter.Load(Path.Combine(Paths.ProjectDir, "publicfilter.json"), out _);
        var terms = filter.ForbiddenTerms.Concat(new[] { "Circle of Ash", "Ash Circle", "Dryad's Spring", "Thornwick" }).Distinct().ToList();
        Assert.Contains("Circle of Ash", terms);
        Assert.Contains("Great Stump", terms);
        var texts = new Dictionary<string, string>();
        foreach (var kind in new[] { "mixed", "real", "empty", "missing" })
        {
            using var site = Open(kind);
            foreach (var (p, html) in await AllPages(site)) texts[$"{kind}:{p}"] = html;
            foreach (var p in new[] { "/robots.txt", "/sitemap.xml", "/missing-page" }) texts[$"{kind}:{p}"] = (await site.Get(p)).Body;
            foreach (var asset in new[] { "/css/site.css", "/js/site.js", "/favicon.svg" }) texts[$"{kind}:{asset}"] = (await site.Get(asset)).Body;
        }
        // static files and sources: images, styles, scripts, pages, partials, copy and news (the filter's own configuration must name the terms, so it is the one file left out)
        foreach (var root in new[] { Path.Combine(Paths.ProjectDir, "wwwroot"), Path.Combine(Paths.ProjectDir, "Pages"), Path.Combine(Paths.WebDir, "content") })
            foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                if (new[] { ".css", ".js", ".svg", ".cshtml", ".md", ".json", ".txt", ".html" }.Contains(Path.GetExtension(f))) texts[f] = File.ReadAllText(f);
        foreach (var f in new[] { "appsettings.json", "appsettings.Development.json", "Program.cs" }) texts[f] = File.ReadAllText(Path.Combine(Paths.ProjectDir, f));
        Assert.True(texts.Count > 60);
        foreach (var (where, text) in texts)
        {
            var n = PublicFilter.Normalize(text);
            foreach (var term in terms)
                Assert.False(n.Contains(PublicFilter.Normalize(term)), $"'{term}' found in {where}");
        }
        // the og image is binary: it carries no text
        Assert.True(File.Exists(Path.Combine(Paths.ProjectDir, "wwwroot", "img", "og.png")));
    }

    [Fact]
    public async Task Chronicle_pages_and_views_work()
    {
        using var site = Open("mixed", new() { ["Site:ChroniclePageSize"] = "3" });
        var (_, newest, _) = await site.Get("/chronicle");
        Assert.True(newest.IndexOf("The Fall of the Gaunt Stag") < newest.IndexOf("The First Knight") || !newest.Contains("The First Knight"));
        var firstId = Regex.Match(newest, "href=\"/chronicle/(\\d+)\"").Groups[1].Value;
        Assert.Equal("13", firstId);      // newest first
        var (_, top, _) = await site.Get("/chronicle?view=top");
        Assert.Equal("1", Regex.Match(top, "href=\"/chronicle/(\\d+)\"").Groups[1].Value);   // highest score first
        var (s2, page2, _) = await site.Get("/chronicle?page=2");
        Assert.Equal(200, s2);
        Assert.Contains("Page 2 of 2", page2);
        var (_, silly, _) = await site.Get("/chronicle?page=999");
        Assert.Contains("Page 2 of 2", silly);
        var (_, entry, _) = await site.Get("/chronicle/1");
        Assert.Contains("The 3rd Day of Deepfrost, Year 1", entry);
        Assert.Contains("ESS 4200", entry);
        Assert.Contains("Dara", entry);
        Assert.Contains("dropcap", entry);
    }
}

public class UnitTests
{
    [Fact]
    public void Calendar_matches_the_game()
    {
        // mudlib/secure/chronicle.cs: twelve months of thirty days, then the Turning; Year 1 starts on the epoch day.
        Assert.Equal("the 1st Day of Deepfrost, Year 1", HollowmereCalendar.DateFor(20776, 20776).Full);
        Assert.Equal("the 30th Day of Deepfrost, Year 1", HollowmereCalendar.DateFor(20776, 20776 + 29).Full);
        Assert.Equal("the 1st Day of Thawmoon, Year 1", HollowmereCalendar.DateFor(20776, 20776 + 30).Full);
        Assert.Equal("the 17th Day of Harvest, Year 1", HollowmereCalendar.DateFor(20776, 20776 + 7 * 30 + 16).Full);
        Assert.Equal("the 1st Day of the Turning, Year 1", HollowmereCalendar.DateFor(20776, 20776 + 360).Full);
        Assert.Equal("the 5th Day of the Turning, Year 1", HollowmereCalendar.DateFor(20776, 20776 + 364).Full);
        Assert.Equal("the 1st Day of Deepfrost, Year 2", HollowmereCalendar.DateFor(20776, 20776 + 365).Full);
        var parsed = HollowmereCalendar.Parse("the 17th Day of Harvest, Year 3");
        Assert.Equal(("17th", "Harvest", "Year 3"), (parsed.Day, parsed.Month, parsed.Year));
    }

    private static PublicFilter Default() => PublicFilter.Load(Path.Combine(Paths.ProjectDir, "publicfilter.json"), out _);

    [Theory]
    [InlineData("discover.hidden", false)]
    [InlineData("discover.hidden_anything", false)]
    [InlineData("discover.secret_dungeon", false)]
    [InlineData("discover.great_stump", false)]
    [InlineData("craft.discover_blood_ink", false)]
    [InlineData("craft.discover_", false)]
    [InlineData("some.future_type", false)]
    [InlineData("", false)]
    [InlineData("discover.area", false)]
    [InlineData("level.milestone", true)]
    [InlineData("renown.mythic", true)]
    [InlineData("RENOWN.FAMOUS", true)]
    public void Event_types_follow_the_allow_and_deny_rules(string id, bool expected) => Assert.Equal(expected, Default().IsPublicEventType(id));

    [Fact]
    public void Deny_beats_allow_and_terms_ignore_punctuation()
    {
        var f = new PublicFilter(new[] { "boss." }, new[] { "boss.*", "level.*" }, new[] { "Great Stump" }, new[] { "Greenvale" });
        Assert.False(f.IsPublicEventType("boss.dungeon"));
        Assert.True(f.IsPublicEventType("level.milestone"));
        Assert.True(f.ContainsForbiddenTerm("the great_stump, found"));
        Assert.True(f.ContainsForbiddenTerm("GREAT-STUMPS"));
        Assert.False(f.ContainsForbiddenTerm("a great tree stump"));
        Assert.True(f.IsPublicPlace("greenvale"));
        Assert.False(f.IsPublicPlace("The Forgotten Cellars"));
    }

    [Fact]
    public void Entries_naming_a_wizard_are_refused()
    {
        var f = Default();
        var wizards = new HashSet<string> { "alice" };
        IReadOnlyDictionary<string, string> E(string who, string text) => new Dictionary<string, string> { ["event"] = "level.milestone", ["headline"] = "H", ["text"] = text, ["participants"] = who, ["actor"] = who };
        Assert.True(f.Allows(E("Bryn", "Bryn did a thing."), wizards));
        Assert.False(f.Allows(E("Alice", "A thing."), wizards));
        Assert.False(f.Allows(E("Bryn|Alice", "A thing."), wizards));
        Assert.False(f.Allows(E("Bryn", "Bryn met Alice."), wizards));
    }
}
