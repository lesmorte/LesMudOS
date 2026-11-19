using System.Text;
using System.Text.Json;

namespace LesMudOS.Web.Data;

/// <summary>
/// The secrets filter: decides which Chronicle entries a visitor may see. The rules live in one file (publicfilter.json).
///
/// An entry is public only if ALL of these hold:
///   1. its event id does not start with any "hiddenEventPrefixes" entry (deny always wins);
///   2. its event id matches an entry of "publicEvents" (exact, or a prefix when the entry ends in "*"):
///      an event type that is not listed is hidden, so a secret type added to the game later stays hidden until someone lists it;
///   3. none of the "forbiddenTerms" appears in the entry's headline, text, significance, place, region, tags, subject or event id
///      (compared case-insensitively with punctuation ignored, so "great_stump" and "Great Stump" are the same);
///   4. nobody it names is a wizard, and no wizard's name appears in its text.
/// If the file is missing or unreadable nothing is public (the filter fails closed).
/// </summary>
public sealed class PublicFilter
{
    public IReadOnlyList<string> HiddenEventPrefixes { get; }
    public IReadOnlyList<string> PublicEvents { get; }
    public IReadOnlyList<string> ForbiddenTerms { get; }
    public IReadOnlyList<string> PublicPlaces { get; }
    public bool Loaded { get; }
    private readonly string[] _terms;

    public PublicFilter(IEnumerable<string> hiddenPrefixes, IEnumerable<string> publicEvents, IEnumerable<string> forbiddenTerms, IEnumerable<string> publicPlaces, bool loaded = true)
    {
        HiddenEventPrefixes = hiddenPrefixes.Select(s => s.Trim().ToLowerInvariant()).Where(s => s != "").ToList();
        PublicEvents = publicEvents.Select(s => s.Trim().ToLowerInvariant()).Where(s => s != "").ToList();
        ForbiddenTerms = forbiddenTerms.Select(s => s.Trim()).Where(s => s != "").ToList();
        PublicPlaces = publicPlaces.Select(s => s.Trim()).Where(s => s != "").ToList();
        _terms = ForbiddenTerms.Select(Normalize).Where(s => s != "").ToArray();
        Loaded = loaded;
    }

    /// <summary>A filter that lets nothing through.</summary>
    public static PublicFilter ClosedFilter { get; } = new(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), loaded: false);

    public static PublicFilter Load(string? path, out string? problem)
    {
        problem = null;
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) { problem = "filter file not found: " + path; return ClosedFilter; }
            using var doc = JsonDocument.Parse(File.ReadAllText(path), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
            var root = doc.RootElement;
            List<string> List(string name) => root.TryGetProperty(name, out var e) && e.ValueKind == JsonValueKind.Array
                ? e.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList() : new List<string>();
            return new PublicFilter(List("hiddenEventPrefixes"), List("publicEvents"), List("forbiddenTerms"), List("publicPlaces"));
        }
        catch (Exception ex)
        {
            problem = "filter file unreadable: " + ex.Message;
            return ClosedFilter;
        }
    }

    /// <summary>Lower-case, every run of non-letters and non-digits becomes one space.</summary>
    public static string Normalize(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        var space = true;
        foreach (var c in s)
        {
            if (char.IsLetterOrDigit(c)) { sb.Append(char.ToLowerInvariant(c)); space = false; }
            else if (!space) { sb.Append(' '); space = true; }
        }
        return sb.ToString().TrimEnd();
    }

    public bool IsHiddenEventType(string? eventId)
    {
        var id = (eventId ?? "").Trim().ToLowerInvariant();
        return HiddenEventPrefixes.Any(p => id.StartsWith(p, StringComparison.Ordinal));
    }

    public bool IsPublicEventType(string? eventId)
    {
        var id = (eventId ?? "").Trim().ToLowerInvariant();
        if (id == "" || IsHiddenEventType(id)) return false;
        foreach (var p in PublicEvents)
        {
            if (p.EndsWith('*') ? id.StartsWith(p[..^1], StringComparison.Ordinal) : id == p) return true;
        }
        return false;
    }

    public bool ContainsForbiddenTerm(params string?[] texts)
    {
        foreach (var t in texts)
        {
            var n = Normalize(t);
            if (n == "") continue;
            foreach (var term in _terms) if (n.Contains(term, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    public bool IsPublicPlace(string? place)
        => !string.IsNullOrWhiteSpace(place) && PublicPlaces.Any(p => p.Equals(place.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>True if the raw Chronicle entry (the game's dictionary of strings) may be shown.</summary>
    public bool Allows(IReadOnlyDictionary<string, string> entry, IReadOnlySet<string> wizards)
    {
        string F(string k) => entry.TryGetValue(k, out var v) ? v ?? "" : "";
        var ev = F("event");
        if (!IsPublicEventType(ev)) return false;
        if (ContainsForbiddenTerm(F("headline"), F("text"), F("significance"), F("area"), F("region"), F("tags"), F("subject"), ev, F("participants"), F("actor"))) return false;
        foreach (var n in F("participants").Split('|', StringSplitOptions.RemoveEmptyEntries).Append(F("actor")))
            if (n.Trim() != "" && wizards.Contains(n.Trim().ToLowerInvariant())) return false;
        if (wizards.Count > 0)
        {
            var words = " " + Normalize(F("headline") + " " + F("text") + " " + F("significance")) + " ";
            foreach (var w in wizards) if (words.Contains(" " + Normalize(w) + " ", StringComparison.Ordinal)) return false;
        }
        return true;
    }
}
