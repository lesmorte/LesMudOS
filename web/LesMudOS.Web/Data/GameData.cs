using System.Globalization;
using Microsoft.Extensions.Options;

namespace LesMudOS.Web.Data;

/// <summary>
/// The website's one view of the game: reads the Chronicle, Renown ledger, wizard list and characters from the save directory,
/// applies the secrets filter, caches the result for a short time and never throws. A file that cannot be read this time falls
/// back to the last good copy of that file; with none, the part that depends on it is simply empty.
/// </summary>
public sealed class GameData
{
    private static readonly string[] TierNames = { "Unknown", "Local", "Recognized", "Acclaimed", "Renowned", "Famous", "Heroic", "Legendary", "Mythic" };
    private static readonly int[] TierFloor = { 0, 1, 100, 500, 1000, 2500, 5000, 10000, 25000 };

    private readonly SiteOptions _options;
    private readonly PublicFilter _filter;
    private readonly TimeProvider _time;
    private readonly ILogger<GameData> _log;
    private readonly object _gate = new();
    private WorldSnapshot _snapshot = WorldSnapshot.Empty;
    private DateTimeOffset _expires = DateTimeOffset.MinValue;
    private ChronicleFile? _chronicle;
    private RenownFile? _renown;
    private HashSet<string>? _wizards;
    private Dictionary<string, Character>? _characters;

    public GameData(IOptions<SiteOptions> options, PublicFilter filter, TimeProvider time, ILogger<GameData> log)
    {
        _options = options.Value;
        _filter = filter;
        _time = time;
        _log = log;
    }

    public PublicFilter Filter => _filter;

    public WorldSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                var now = _time.GetUtcNow();
                if (now < _expires) return _snapshot;
                try { _snapshot = Build(now); }
                catch (Exception ex) { _log.LogWarning(ex, "Could not read the game's data; keeping the last good view."); }
                _expires = now.AddSeconds(Math.Max(0, _options.CacheSeconds));
                return _snapshot;
            }
        }
    }

    public ChronicleEntry? Entry(int id) => Snapshot.Entries.FirstOrDefault(e => e.Id == id);

    public static string TierOf(int renown)
    {
        var tier = 0;
        for (var i = 0; i < TierFloor.Length; i++) if (renown >= TierFloor[i]) tier = i;
        return TierNames[tier];
    }

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private WorldSnapshot Build(DateTimeOffset now)
    {
        var dir = _options.SaveDirectory;
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return WorldSnapshot.Empty;

        _chronicle = SaveReader.ReadChronicle(dir) ?? _chronicle;
        _renown = SaveReader.ReadRenown(dir) ?? _renown;
        _wizards = SaveReader.ReadWizards(dir) ?? _wizards;
        _characters = SaveReader.ReadCharacters(dir) ?? _characters;

        // If the wizard list cannot be read at all, nothing that names a person can be shown safely: show no entries and no heroes.
        if (_wizards == null) { _log.LogWarning("Wizard list unavailable; showing no named entries or heroes."); }
        var wizards = (IReadOnlySet<string>?)_wizards;

        var entries = new List<ChronicleEntry>();
        if (_chronicle != null && wizards != null)
        {
            var index = 0;
            foreach (var raw in _chronicle.Entries)
            {
                index++;
                if (!_filter.Allows(raw, wizards)) continue;
                string F(string k) => raw.TryGetValue(k, out var v) ? v : "";
                var title = F("headline").Trim();
                var summary = F("text").Trim();
                if (title == "" || summary == "") continue;
                var id = int.TryParse(F("id"), NumberStyles.None, CultureInfo.InvariantCulture, out var i) ? i : index;
                var score = int.TryParse(F("ess"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s : 0;
                var day = long.TryParse(F("day"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var d) ? d : 0;
                var names = F("participants").Split('|', StringSplitOptions.RemoveEmptyEntries).Select(n => Cap(n.Trim())).Where(n => n != "").Distinct().ToList();
                var area = F("area").Trim();
                entries.Add(new ChronicleEntry(id, title, summary, F("significance").Trim(), score, HollowmereCalendar.Parse(F("date")), day, names,
                    F("category"), F("classname"), F("featured") == "1", _filter.IsPublicPlace(area) ? area : null));
            }
        }
        entries = entries.GroupBy(e => e.Id).Select(g => g.First()).OrderByDescending(e => e.Id).ToList();

        var heroes = new List<Hero>();
        if (_renown != null && wizards != null)
        {
            var roster = _characters;
            var chosen = _renown.Renown
                .Where(r => r.Value > 0 && !wizards.Contains(r.Key) && (roster == null || roster.ContainsKey(r.Key)))
                .OrderByDescending(r => r.Value).ThenBy(r => r.Key, StringComparer.Ordinal).Take(10);
            foreach (var (key, value) in chosen)
            {
                string title = _renown.ActiveTitle.TryGetValue(key, out var active) && active != "" ? active
                    : _renown.Titles.TryGetValue(key, out var list) && list.Count > 0 ? list[^1] : "";
                Character? ch = roster != null && roster.TryGetValue(key, out var found) ? found : null;
                var calling = ch is { Calling: not "" } ? Cap(ch.Calling) : null;
                var cls = ch is { AdvancedClass: not "" } ? Cap(ch.AdvancedClass) : null;
                heroes.Add(new Hero(Cap(key), value, TierOf(value), title, calling, cls));
            }
        }

        int? characters = _characters != null && wizards != null ? _characters.Keys.Count(k => !wizards.Contains(k)) : null;
        string? today = null;
        if (_chronicle is { EpochDay: > 0 })
            today = HollowmereCalendar.DateFor(_chronicle.EpochDay, now.ToUnixTimeSeconds() / 86400).Full;

        return new WorldSnapshot(entries, heroes, characters, today);
    }
}
