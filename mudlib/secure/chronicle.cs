/// <summary>
/// The Chronicle: the permanent, append-only memory of Hollowmere, and the
/// game calendar. It stores the entries, the first occurrences (who did each
/// thing first), each character's lifetime ESS contribution and the date on
/// which Year 1 began. Everything that changes it comes from the event service
/// (/secure/events); everything else here is a query that returns plain data or
/// the text to show a player.
///
/// Blueprint only: a clone has no authority and refuses every write.
/// Design: docs/CHRONICLE.md, implementation notes: docs/CHRONICLE_IMPL.md.
/// </summary>
public class ChronicleDaemon : MudObject
{
    private const string SaveFile = "/secure/chronicle";
    private const string Service = "/secure/events";
    private const string Ledger = "/secure/renown";

    public static readonly string[] Months =
    {
        "Deepfrost", "Thawmoon", "Sowing", "Blossom", "Greentide", "Highsun",
        "Goldfields", "Harvest", "Leaf-fall", "Mistwane", "Firstfrost", "Longnight"
    };

    /// <summary>The Unix day (seconds / 86400, UTC) on which Year 1 began. Never changes once set.</summary>
    [Saved] private long epochDay;

    /// <summary>Every entry, oldest first. Ids are the position plus one. Values are plain strings.</summary>
    [Saved] private List<Dictionary<string, string>> entries = new();

    /// <summary>"event id|subject" to the record of the first time it happened.</summary>
    [Saved] private Dictionary<string, Dictionary<string, string>> occurred = new();

    /// <summary>Lower-cased name to lifetime ESS contribution.</summary>
    [Saved] private Dictionary<string, long> contribution = new();

    /// <summary>Lower-cased name to the total ESS of the Chronicle entries that name them.</summary>
    [Saved] private Dictionary<string, long> entryTotal = new();

    private bool persistent = true;

    public override void Create()
    {
        if (Self.IsClone) return;
        var restored = false;
        try { restored = RestoreObject(SaveFile); }
        catch (MudRuntimeException) { persistent = false; }
        if (!restored || epochDay == 0)
        {
            epochDay = Time() / 86400;
            Save();
        }
    }

    private void Save()
    {
        if (!persistent) return;
        try { SaveObject(SaveFile); }
        catch (MudRuntimeException) { persistent = false; }
    }

    /// <summary>False if the master refused the save file: nothing survives a restart.</summary>
    public bool QueryPersistent() => persistent;

    private bool FromService()
    {
        var caller = PreviousObject();
        return !Self.IsClone && caller != null && !caller.IsClone && caller.ProgramPath == Service;
    }

    // ------------------------------------------------------------------
    // The calendar
    // ------------------------------------------------------------------

    /// <summary>The Unix day on which Year 1 began.</summary>
    public long QueryEpochDay() => epochDay;

    /// <summary>Days since 1970-01-01 of a proleptic Gregorian date.</summary>
    private static long DaysFromCivil(long y, int m, int d)
    {
        y -= m <= 2 ? 1 : 0;
        var era = (y >= 0 ? y : y - 399) / 400;
        var yoe = y - era * 400;
        var doy = (153 * (m + (m > 2 ? -3 : 9)) + 2) / 5 + d - 1;
        var doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;
        return era * 146097 + doe - 719468;
    }

    private static long YearOfDay(long days)
    {
        var z = days + 719468;
        var era = (z >= 0 ? z : z - 146096) / 146097;
        var doe = z - era * 146097;
        var yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365;
        var y = yoe + era * 400;
        var doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
        var mp = (5 * doy + 2) / 153;
        var m = mp < 10 ? mp + 3 : mp - 9;
        return y + (m <= 2 ? 1 : 0);
    }

    private static bool IsLeap(long y) => y % 4 == 0 && (y % 100 != 0 || y % 400 == 0);

    /// <summary>
    /// Length in days of the Hollowmere year that starts on the given Unix day:
    /// 360 days in twelve months, then the Turning of five days, or six if a
    /// real 29 February falls in the first 365 days of the year.
    /// </summary>
    public int YearLength(long startDay)
    {
        for (var y = YearOfDay(startDay); y <= YearOfDay(startDay + 364); y++)
        {
            if (!IsLeap(y)) continue;
            var feb29 = DaysFromCivil(y, 2, 29);
            if (feb29 >= startDay && feb29 < startDay + 365) return 366;
        }
        return 365;
    }

    private static string Ordinal(int n)
    {
        var tens = n % 100;
        if (tens >= 11 && tens <= 13) return n + "th";
        return (n % 10) switch { 1 => n + "st", 2 => n + "nd", 3 => n + "rd", _ => n + "th" };
    }

    /// <summary>
    /// The in-world date of a Unix day, for a Year 1 that began on epoch:
    /// "the 17th Day of Harvest, Year 3" or "the 2nd Day of the Turning, Year 1".
    /// Pure arithmetic, so it can be checked with any pair of numbers.
    /// </summary>
    public string DateFor(long epoch, long day)
    {
        if (day < epoch) day = epoch;
        var start = epoch;
        var year = 1;
        while (true)
        {
            var length = YearLength(start);
            if (day < start + length) break;
            start += length;
            year++;
        }
        var offset = (int)(day - start);
        if (offset >= 360) return $"the {Ordinal(offset - 359)} Day of the Turning, Year {year}";
        return $"the {Ordinal(offset % 30 + 1)} Day of {Months[offset / 30]}, Year {year}";
    }

    /// <summary>Today's date in Hollowmere.</summary>
    public string DateText() => DateFor(epochDay, Time() / 86400);

    /// <summary>The date of a real timestamp (Unix seconds).</summary>
    public string DateOfTimestamp(long seconds) => DateFor(epochDay, seconds / 86400);

    /// <summary>What the `date` command shows.</summary>
    public string DateCommand() => "Today is " + DateText() + ".\n";

    // ------------------------------------------------------------------
    // Writes (event service only)
    // ------------------------------------------------------------------

    /// <summary>Append an entry. Returns its id, or 0 if refused.</summary>
    public int AddEntry(Dictionary<string, string> fields)
    {
        if (!FromService()) return 0;
        var entry = new Dictionary<string, string>(fields);
        entry["id"] = (entries.Count + 1).ToString();
        entry["ts"] = Time().ToString();
        entry["day"] = (Time() / 86400 - epochDay).ToString();
        entry["date"] = DateText();
        entries.Add(entry);
        Save();
        return entries.Count;
    }

    /// <summary>
    /// Record that something has happened for the first time. True if this
    /// call recorded it, false if it had happened before (the first stays with
    /// whoever got it) or the caller is refused.
    /// </summary>
    public bool MarkOccurred(string key, Dictionary<string, string> info)
    {
        if (!FromService() || occurred.ContainsKey(key)) return false;
        var record = new Dictionary<string, string>(info);
        record["date"] = DateText();
        record["ts"] = Time().ToString();
        occurred[key] = record;
        Save();
        return true;
    }

    /// <summary>Add to lifetime contribution, and to the entry total if an entry was made.</summary>
    public void AddContribution(string name, int ess, bool inEntry)
    {
        if (!FromService() || name == "") return;
        name = name.ToLowerInvariant();
        contribution[name] = contribution.GetValueOrDefault(name) + ess;
        if (inEntry) entryTotal[name] = entryTotal.GetValueOrDefault(name) + ess;
        Save();
    }

    // ------------------------------------------------------------------
    // Plain-data queries
    // ------------------------------------------------------------------

    public int QueryEntryCount() => entries.Count;

    public bool HasOccurred(string key) => occurred.ContainsKey(key);

    /// <summary>One field of the record of a first occurrence ("holders", "ess", "entry", "listed", "milestone", "date", "event", "subject"), or "".</summary>
    public string QueryOccurred(string key, string field)
        => occurred.TryGetValue(key, out var record) ? record.GetValueOrDefault(field) ?? "" : "";

    /// <summary>One field of an entry, or "". Fields: id ts day date ess class category subcategory area region participants actor factions headline text significance tags refs event subject first featured.</summary>
    public string QueryEntry(int id, string field)
        => id >= 1 && id <= entries.Count ? entries[id - 1].GetValueOrDefault(field) ?? "" : "";

    public long QueryContribution(string name) => contribution.GetValueOrDefault(name.ToLowerInvariant());

    public long QueryEntryTotal(string name) => entryTotal.GetValueOrDefault(name.ToLowerInvariant());

    /// <summary>Ids of the entries that name a character (as participant or primary actor), oldest first.</summary>
    public List<int> QueryEntriesFor(string name)
    {
        name = name.ToLowerInvariant();
        var ids = new List<int>();
        foreach (var e in entries)
            if (Names(e).Contains(name)) ids.Add(int.Parse(e["id"]));
        return ids;
    }

    /// <summary>The newest entries, newest first.</summary>
    public List<int> QueryRecent(int count)
    {
        var ids = new List<int>();
        for (var i = entries.Count; i >= 1 && ids.Count < count; i--) ids.Add(i);
        return ids;
    }

    /// <summary>Keys of the first occurrences that are listed as server firsts ("firsts"), oldest first.</summary>
    public List<string> QueryFirstKeys(string list)
        => occurred.Where(o => o.Value.GetValueOrDefault("listed") == list)
                   .OrderBy(o => long.Parse(o.Value.GetValueOrDefault("ts") ?? "0")).Select(o => o.Key).ToList();

    private static HashSet<string> Names(Dictionary<string, string> entry)
    {
        var names = new HashSet<string>();
        foreach (var n in entry.GetValueOrDefault("participants", "").Split('|', StringSplitOptions.RemoveEmptyEntries))
            names.Add(n.ToLowerInvariant());
        var actor = entry.GetValueOrDefault("actor", "");
        if (actor != "") names.Add(actor.ToLowerInvariant());
        return names;
    }

    public static string Cap(string name) => name == "" ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);

    /// <summary>Hall of Heroes: lifetime ESS contribution of 5,000 or an entry total of 10,000.</summary>
    public List<string> QueryHeroes()
    {
        var names = new HashSet<string>(contribution.Where(c => c.Value >= 5000).Select(c => c.Key));
        foreach (var e in entryTotal.Where(t => t.Value >= 10000)) names.Add(e.Key);
        return names.OrderByDescending(n => contribution.GetValueOrDefault(n)).ThenBy(n => n).ToList();
    }

    /// <summary>Hall of Legends: took part in a World-Defining event, holds a milestone server first, or has Mythic Renown.</summary>
    public List<string> QueryLegends()
    {
        var names = new HashSet<string>();
        foreach (var e in entries)
            if (int.TryParse(e.GetValueOrDefault("ess"), out var ess) && ess >= 5000) names.UnionWith(Names(e));
        foreach (var o in occurred.Values)
            if (o.GetValueOrDefault("milestone") == "1")
                foreach (var h in o.GetValueOrDefault("holders", "").Split('|', StringSplitOptions.RemoveEmptyEntries)) names.Add(h.ToLowerInvariant());
        var mythic = LoadObject(Ledger).Call<List<string>>("NamesAtOrAbove", 25000);
        if (mythic != null) names.UnionWith(mythic);
        return names.OrderBy(n => n).ToList();
    }

    // ------------------------------------------------------------------
    // Text for players
    // ------------------------------------------------------------------

    private string Line(Dictionary<string, string> e)
    {
        var mark = e.GetValueOrDefault("featured") == "1" ? "*" : " ";
        return $"{mark}#{e["id"]} {e["date"]}: {e.GetValueOrDefault("headline")}";
    }

    private string Listing(IEnumerable<int> ids)
    {
        var sb = new StringBuilder();
        foreach (var id in ids) sb.Append(Line(entries[id - 1])).Append('\n');
        return sb.ToString();
    }

    /// <summary>The most recent entries, featured ones marked with a star.</summary>
    public string RecentText(int count)
    {
        if (entries.Count == 0) return "The Chronicle of Hollowmere is still unwritten. Its first page waits for the first deed worth remembering.\n";
        return "The Chronicle of Hollowmere, most recent entries (a star marks a featured entry):\n" + Listing(QueryRecent(count))
               + "Type 'chronicle <number>' to read one in full.\n";
    }

    /// <summary>One entry in full.</summary>
    public string EntryText(int id)
    {
        if (id < 1 || id > entries.Count) return "There is no entry numbered " + id + " in the Chronicle.\n";
        var e = entries[id - 1];
        var sb = new StringBuilder();
        var ess = int.Parse(e.GetValueOrDefault("ess", "0"));
        if (ess >= 1000) sb.Append("=== A LEGENDARY ENTRY ===\n");
        sb.Append(e.GetValueOrDefault("headline")).Append("  (entry #").Append(e["id"]).Append(")\n");
        sb.Append(Cap(e["date"])).Append(".\n");
        sb.Append(e.GetValueOrDefault("text")).Append('\n');
        var significance = e.GetValueOrDefault("significance", "");
        if (significance != "") sb.Append(significance).Append('\n');
        var who = e.GetValueOrDefault("participants", "").Split('|', StringSplitOptions.RemoveEmptyEntries).Select(Cap).ToList();
        if (who.Count > 0) sb.Append("Named: ").Append(string.Join(", ", who)).Append('\n');
        sb.Append("Where: ").Append(e.GetValueOrDefault("area")).Append(", ").Append(e.GetValueOrDefault("region")).Append('\n');
        sb.Append("Recorded as: ").Append(e.GetValueOrDefault("category")).Append(", ESS ").Append(ess)
          .Append(" (").Append(e.GetValueOrDefault("classname")).Append(")\n");
        var refs = e.GetValueOrDefault("refs", "");
        if (refs != "") sb.Append("See also entries: ").Append(refs).Append('\n');
        return sb.ToString();
    }

    /// <summary>Entries naming a character, place or creature, or containing the word.</summary>
    public string SearchText(string term)
    {
        term = term.Trim().ToLowerInvariant();
        if (term == "") return "Search the Chronicle for what? Try a name, a place or a creature.\n";
        var ids = new List<int>();
        foreach (var e in entries)
        {
            var hay = string.Join(" ", e.GetValueOrDefault("participants", "").Replace('|', ' '), e.GetValueOrDefault("actor", ""),
                e.GetValueOrDefault("area", ""), e.GetValueOrDefault("region", ""), e.GetValueOrDefault("headline", ""),
                e.GetValueOrDefault("text", ""), e.GetValueOrDefault("tags", "")).ToLowerInvariant();
            if (hay.Contains(term)) ids.Add(int.Parse(e["id"]));
        }
        if (ids.Count == 0) return "The Chronicle holds nothing about '" + term + "'.\n";
        ids.Reverse();
        return $"{ids.Count} entr{(ids.Count == 1 ? "y" : "ies")} about '{term}', newest first:\n" + Listing(ids.Take(20));
    }

    /// <summary>One character's own entries.</summary>
    public string EntriesForText(string name)
    {
        var ids = QueryEntriesFor(name);
        if (ids.Count == 0) return Cap(name) + " does not yet appear in the Chronicle.\n";
        ids.Reverse();
        return $"{Cap(name)} appears in {ids.Count} entr{(ids.Count == 1 ? "y" : "ies")}, newest first:\n" + Listing(ids.Take(20));
    }

    private string HoldersText(Dictionary<string, string> o)
        => string.Join(", ", o.GetValueOrDefault("holders", "").Split('|', StringSplitOptions.RemoveEmptyEntries).Select(Cap));

    public string FirstsText()
    {
        var keys = QueryFirstKeys("firsts");
        if (keys.Count == 0) return "No server firsts have been recorded yet.\n";
        var sb = new StringBuilder("Server firsts of Hollowmere:\n");
        foreach (var key in keys)
        {
            var o = occurred[key];
            sb.Append("  ").Append(o.GetValueOrDefault("title")).Append(": ").Append(HoldersText(o)).Append(", ").Append(o["date"]).Append('\n');
        }
        return sb.ToString();
    }

    public string HeroesText()
    {
        var names = QueryHeroes();
        if (names.Count == 0) return "The Hall of Heroes is empty. No one has yet earned a place in it.\n";
        return "The Hall of Heroes:\n" + string.Concat(names.Select(n => $"  {Cap(n)} (lifetime ESS {QueryContribution(n)})\n"));
    }

    public string LegendsText()
    {
        var names = QueryLegends();
        if (names.Count == 0) return "The Hall of Legends is empty. Many years may pass before a name is carved into it.\n";
        return "The Hall of Legends:\n" + string.Concat(names.Select(n => $"  {Cap(n)}\n"));
    }

    public string RecordsText()
    {
        var sb = new StringBuilder("World records of Hollowmere:\n");
        sb.Append("Server firsts:\n");
        var firsts = QueryFirstKeys("firsts");
        if (firsts.Count == 0) sb.Append("  none yet\n");
        foreach (var key in firsts) sb.Append("  ").Append(occurred[key].GetValueOrDefault("title")).Append(": ").Append(HoldersText(occurred[key])).Append('\n');
        sb.Append("Most renowned:\n");
        var top = LoadObject(Ledger).Call<List<string>>("TopRenown", 5) ?? new List<string>();
        if (top.Count == 0) sb.Append("  none yet\n");
        foreach (var row in top) { var p = row.Split('|'); sb.Append("  ").Append(Cap(p[0])).Append(" (").Append(p[1]).Append(")\n"); }
        sb.Append("First discoverers:\n");
        var places = QueryFirstKeys("records");
        if (places.Count == 0) sb.Append("  none yet\n");
        foreach (var key in places) sb.Append("  ").Append(occurred[key].GetValueOrDefault("title")).Append(": ").Append(HoldersText(occurred[key])).Append('\n');
        return sb.ToString();
    }

    /// <summary>
    /// The `chronicle` command: no argument for the recent entries, a number
    /// for one entry, or search / firsts / heroes / legends / records.
    /// </summary>
    public string ChronicleCommand(string args)
    {
        args = (args ?? "").Trim();
        if (args == "") return RecentText(10);
        if (int.TryParse(args, out var id)) return EntryText(id);
        var words = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var rest = words.Length > 1 ? words[1] : "";
        switch (words[0].ToLowerInvariant())
        {
            case "search": return SearchText(rest);
            case "firsts": return FirstsText();
            case "heroes": return HeroesText();
            case "legends": return LegendsText();
            case "records": return RecordsText();
            case "about": return EntriesForText(rest.Trim());
            default: return "Try: chronicle, chronicle <number>, chronicle search <word>, chronicle firsts, chronicle heroes, chronicle legends, chronicle records.\n";
        }
    }
}
