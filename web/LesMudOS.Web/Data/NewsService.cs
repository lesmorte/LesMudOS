using System.Globalization;
using System.Text.RegularExpressions;
using Markdig;
using Microsoft.Extensions.Options;

namespace LesMudOS.Web.Data;

public sealed record NewsPost(string Slug, string Title, DateOnly Date, string Summary, string Html);

/// <summary>
/// News is a folder of Markdown files (content/news/*.md). Posting news is adding a file. Each file starts with a small header:
///
///   ---
///   title: A headline
///   date: 2026-11-18          (a post dated in the future stays hidden until that day)
///   summary: One sentence for the list and the link previews.
///   slug: optional-url-name   (default: the file name without .md and without a leading date)
///   draft: true               (optional: hides the post)
///   ---
///
/// followed by the post in Markdown. Raw HTML in a post is not allowed. A file that is malformed is skipped.
/// </summary>
public sealed partial class NewsService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().DisableHtml().UseEmphasisExtras().Build();

    private readonly string? _dir;
    private readonly TimeProvider _time;
    private readonly int _cacheSeconds;
    private readonly object _gate = new();
    private IReadOnlyList<NewsPost> _posts = Array.Empty<NewsPost>();
    private DateTimeOffset _expires = DateTimeOffset.MinValue;

    public NewsService(string? directory, TimeProvider time, int cacheSeconds)
    {
        _dir = directory; _time = time; _cacheSeconds = cacheSeconds;
    }

    public IReadOnlyList<NewsPost> Posts
    {
        get
        {
            lock (_gate)
            {
                var now = _time.GetUtcNow();
                if (now < _expires) return _posts;
                try { _posts = Load(DateOnly.FromDateTime(now.UtcDateTime)); } catch (Exception) { /* keep the last good list */ }
                _expires = now.AddSeconds(Math.Max(0, _cacheSeconds));
                return _posts;
            }
        }
    }

    public NewsPost? Find(string slug) => Posts.FirstOrDefault(p => p.Slug == slug);

    [GeneratedRegex("^[a-z0-9][a-z0-9-]{0,80}$")]
    private static partial Regex SlugPattern();

    private IReadOnlyList<NewsPost> Load(DateOnly today)
    {
        var list = new List<NewsPost>();
        if (_dir == null || !Directory.Exists(_dir)) return list;
        foreach (var file in Directory.EnumerateFiles(_dir, "*.md"))
        {
            try
            {
                var post = Parse(file, today);
                if (post != null && list.All(p => p.Slug != post.Slug)) list.Add(post);
            }
            catch (Exception) { /* a bad file is skipped */ }
        }
        return list.OrderByDescending(p => p.Date).ThenBy(p => p.Slug, StringComparer.Ordinal).ToList();
    }

    private static NewsPost? Parse(string file, DateOnly today)
    {
        var lines = File.ReadAllText(file).Replace("\r\n", "\n").Split('\n');
        if (lines.Length < 3 || lines[0].Trim() != "---") return null;
        var end = Array.FindIndex(lines, 1, l => l.Trim() == "---");
        if (end < 0) return null;
        var head = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 1; i < end; i++)
        {
            var colon = lines[i].IndexOf(':');
            if (colon > 0) head[lines[i][..colon].Trim()] = lines[i][(colon + 1)..].Trim();
        }
        if (!head.TryGetValue("title", out var title) || title == "") return null;
        if (!head.TryGetValue("date", out var dateText) || !DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return null;
        if (head.TryGetValue("draft", out var draft) && draft.Equals("true", StringComparison.OrdinalIgnoreCase)) return null;
        if (date > today) return null;
        var stem = Path.GetFileNameWithoutExtension(file);
        var slug = head.TryGetValue("slug", out var s) && s != "" ? s : Regex.Replace(stem, @"^\d{4}-\d{2}-\d{2}-", "");
        slug = slug.ToLowerInvariant();
        if (!SlugPattern().IsMatch(slug)) return null;
        var body = string.Join("\n", lines.Skip(end + 1)).Trim();
        var html = Markdown.ToHtml(body, Pipeline);
        return new NewsPost(slug, title, date, head.GetValueOrDefault("summary") ?? "", html);
    }
}
