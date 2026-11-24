/// <summary>
/// The clock behind `time` and `date` (docs/specs/play-feel.md): the time in a player's own zone, the lists of zones and the reading of what
/// a player types for a zone. It holds no state; the zone is a setting of the player's body, which asks this object and keeps the answer. The
/// zone data comes from the driver (TimeZoneNames, LocalTime): a name is only ever matched against that list, nothing typed reaches a file, and
/// nothing typed is logged.
/// </summary>
public class Clock : MudObject
{
    private static readonly string[] Days = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
    private static readonly string[] Months = { "", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

    /// <summary>The common zones, by region, for the short list (only those the server knows are shown).</summary>
    private static readonly string[][] Common =
    {
        new[] { "UTC", "UTC" },
        new[] { "North America", "America/New_York", "America/Chicago", "America/Denver", "America/Phoenix", "America/Los_Angeles", "America/Anchorage", "Pacific/Honolulu", "America/Halifax", "America/St_Johns", "America/Toronto", "America/Vancouver", "America/Mexico_City" },
        new[] { "Europe", "Europe/London", "Europe/Dublin", "Europe/Paris", "Europe/Berlin", "Europe/Madrid", "Europe/Rome", "Europe/Athens", "Europe/Istanbul", "Europe/Moscow" },
        new[] { "Asia", "Asia/Dubai", "Asia/Karachi", "Asia/Kolkata", "Asia/Dhaka", "Asia/Bangkok", "Asia/Shanghai", "Asia/Singapore", "Asia/Tokyo", "Asia/Seoul", "Asia/Jerusalem" },
        new[] { "Oceania", "Australia/Perth", "Australia/Adelaide", "Australia/Sydney", "Pacific/Auckland", "Pacific/Fiji" },
        new[] { "South America", "America/Sao_Paulo", "America/Argentina/Buenos_Aires", "America/Bogota", "America/Lima", "America/Santiago" },
        new[] { "Africa", "Africa/Cairo", "Africa/Lagos", "Africa/Nairobi", "Africa/Johannesburg", "Africa/Casablanca" },
    };

    /// <summary>What a short or common name means, so that daylight saving is right (a bare offset has none).</summary>
    private static readonly string[][] Aliases =
    {
        new[] { "America/New_York", "eastern", "est", "edt", "et", "eastern time" },
        new[] { "America/Chicago", "central", "cst", "cdt", "ct", "central time" },
        new[] { "America/Denver", "mountain", "mst", "mdt", "mt", "mountain time" },
        new[] { "America/Phoenix", "arizona" },
        new[] { "America/Los_Angeles", "pacific", "pst", "pdt", "pt", "pacific time" },
        new[] { "America/Anchorage", "alaska", "akst", "akdt", "alaska time" },
        new[] { "Pacific/Honolulu", "hawaii", "hst", "hawaii time" },
        new[] { "America/Halifax", "atlantic", "ast", "adt", "atlantic time" },
        new[] { "Europe/London", "bst", "british", "uk" },
        new[] { "Europe/Paris", "cet", "cest" },
        new[] { "Europe/Athens", "eet", "eest" },
        new[] { "Europe/Moscow", "msk" },
        new[] { "Asia/Kolkata", "ist", "india" },
        new[] { "Asia/Tokyo", "jst", "japan" },
        new[] { "Australia/Sydney", "aest", "aedt" },
        new[] { "Pacific/Auckland", "nzst", "nzdt" },
    };

    /// <summary>The name shown before the zone, for zones that have a common one.</summary>
    private static readonly string[][] Friendly =
    {
        new[] { "America/New_York", "Eastern time" }, new[] { "America/Chicago", "Central time" }, new[] { "America/Denver", "Mountain time" },
        new[] { "America/Phoenix", "Arizona time" }, new[] { "America/Los_Angeles", "Pacific time" }, new[] { "America/Anchorage", "Alaska time" },
        new[] { "Pacific/Honolulu", "Hawaii time" }, new[] { "America/Halifax", "Atlantic time" }, new[] { "Europe/London", "UK time" },
        new[] { "UTC", "Coordinated Universal Time" },
    };

    private static readonly string[] RegionWords = { "america", "europe", "asia", "africa", "australia", "antarctica", "arctic", "indian" };
    private const int MaxMatches = 12;

    private static string Esc(string text) => text.Replace("{", "{{");

    private static string Offset(int minutes)
    {
        if (minutes == 0) return "UTC+0";
        var abs = Math.Abs(minutes);
        return "UTC" + (minutes < 0 ? "-" : "+") + (abs / 60) + (abs % 60 == 0 ? "" : ":" + (abs % 60).ToString("00"));
    }

    private static string Clean(string input)
    {
        var text = input.Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder();
        foreach (var c in text) if (c >= ' ' && c < '\u007f') sb.Append(c);
        return sb.ToString();
    }

    private bool HaveZones() => TimeZoneNames().Length > 0;

    private string Friendly1(string zone)
    {
        foreach (var f in Friendly) if (f[0] == zone) return f[1];
        return "";
    }

    /// <summary>"It is 9:40 am on Tuesday 24 November 2026, Eastern time (America/New_York, UTC-5)." and the rest of what `time` says.</summary>
    public string Show(string zone, string inWorld)
    {
        var unset = zone == "";
        var fields = LocalTime(unset ? "UTC" : zone).Split('|');
        if (fields.Length < 10) { fields = LocalTime("UTC").Split('|'); unset = true; zone = ""; }
        var hour = int.Parse(fields[3]);
        var minute = int.Parse(fields[4]);
        var offset = int.Parse(fields[7]);
        var name = fields[9];
        var clock = $"{(hour % 12 == 0 ? 12 : hour % 12)}:{minute:00} {(hour < 12 ? "am" : "pm")} on {Days[int.Parse(fields[6]) % 7]} {int.Parse(fields[2])} {Months[int.Parse(fields[1])]} {fields[0]}";
        string where;
        if (unset) where = "UTC, the server's time";
        else
        {
            var friendly = Friendly1(name);
            var detail = name == "UTC" || name.StartsWith("UTC") ? Offset(offset) : name + ", " + Offset(offset) + (fields[8] == "1" ? ", daylight saving" : "");
            where = friendly == "" ? (name.StartsWith("UTC") ? Offset(offset) : name + " (" + Offset(offset) + (fields[8] == "1" ? ", daylight saving" : "") + ")") : friendly + " (" + detail + ")";
        }
        var text = $"It is {clock}, {where}.\n";
        if (inWorld != "") text += inWorld;
        if (unset) text += "Set your own with {cmd}time -timezone ZONE{/}.\n";
        return text;
    }

    // ---- The lists ----

    private string Entry(string zone) => zone + " " + Offset(int.Parse(LocalTime(zone).Split('|')[7]));

    private static string Columns(List<string> items)
    {
        var text = new System.Text.StringBuilder();
        const int width = 39;
        for (var i = 0; i < items.Count; i += 2)
        {
            text.Append("  ").Append(i + 1 < items.Count ? items[i].PadRight(width) + items[i + 1] : items[i]).Append('\n');
        }
        return text.ToString();
    }

    /// <summary>The short list: about forty common zones by region, each with its offset now.</summary>
    public string Zones()
    {
        var known = new HashSet<string>(TimeZoneNames());
        var text = new System.Text.StringBuilder();
        if (known.Count == 0)
        {
            text.Append("This server has no time zone database, so only UTC and fixed offsets can be used (daylight saving is not followed).\n");
            text.Append("Fixed offsets: {cmd}time -timezone utc-5{/}, {cmd}time -timezone +1{/}, {cmd}time -timezone utc+5:30{/} (from -12 to +14).\n");
            return text.ToString();
        }
        text.Append("Time zones (the offset from UTC now; daylight saving is followed):\n");
        foreach (var group in Common)
        {
            var list = new List<string>();
            for (var i = 1; i < group.Length; i++) if (group[i] == "UTC" || known.Contains(group[i])) list.Add(Entry(group[i]));
            if (list.Count == 0) continue;
            text.Append(group[0]).Append(":\n").Append(Columns(list));
        }
        text.Append("Any standard zone name works: {cmd}time -timezone America/New_York{/}, or just the city ({cmd}time -timezone london{/}), or a common name ({cmd}eastern{/}, {cmd}central{/}, {cmd}mountain{/}, {cmd}pacific{/}, {cmd}utc{/}), or an offset ({cmd}utc-5{/}).\n");
        text.Append("{cmd}time -timezone REGION{/} lists every zone of a region: America, Europe, Asia, Africa, Australia, Pacific/ (with the slash), Atlantic/, Indian, Antarctica, Arctic.\n");
        text.Append("{cmd}time -timezone none{/} clears your zone (the time is then shown in UTC).\n");
        return text.ToString();
    }

    /// <summary>Every zone of one region ("america", "pacific/"), or "" if the word names no region.</summary>
    public string Region(string input)
    {
        var word = Clean(input).TrimEnd('/');
        var slash = Clean(input).EndsWith("/");
        if (!(Array.IndexOf(RegionWords, word) >= 0 || (slash && (word == "pacific" || word == "atlantic" || word == "indian")))) return "";
        var items = new List<string>();
        foreach (var name in TimeZoneNames())
            if (name.StartsWith(word + "/", StringComparison.OrdinalIgnoreCase)) items.Add(Entry(name));
        if (items.Count == 0) return "";
        return $"Zones of {items[0].Substring(0, word.Length)} ({items.Count}; the offset from UTC now):\n" + Columns(items) + "Set one with {cmd}time -timezone NAME{/}; the city alone is enough when it is unambiguous.\n";
    }

    // ---- Reading what a player typed ----

    /// <summary>
    /// What to do with "time -timezone WORDS": "ZONE:name" (set it), "NONE:" (clear it) or "MSG:text" (say it; nothing changes). A name is
    /// matched only against the driver's list.
    /// </summary>
    public string Resolve(string input)
    {
        var word = Clean(input);
        if (word is "none" or "clear" or "off" or "unset" or "reset" or "server") return "NONE:";
        if (word is "utc" or "gmt" or "z" or "zulu" or "utc+0" or "utc-0" or "gmt+0" or "gmt-0" or "+0" or "-0") return "ZONE:UTC";
        var zones = TimeZoneNames();
        // A plain offset: utc-5, gmt+1, +5:30, -3.
        var offset = ReadOffset(word);
        if (offset != "") return offset == "bad" ? "MSG:A fixed offset runs from -12 to +14 hours: for example {cmd}time -timezone utc-5{/}.\n" : "ZONE:" + offset;
        var known = new Dictionary<string, string>();
        foreach (var name in zones) known[name.ToLowerInvariant()] = name;
        var key = word.Replace(' ', '_');
        // The common short names, mapped to the proper regional zone so that daylight saving is followed.
        foreach (var alias in Aliases)
            for (var i = 1; i < alias.Length; i++)
                if (word == alias[i] && known.ContainsKey(alias[0].ToLowerInvariant())) return "ZONE:" + alias[0];
        if (known.TryGetValue(key, out var exact)) return "ZONE:" + exact;
        if (zones.Length == 0)
            return "MSG:This server has no time zone database, so only UTC and fixed offsets (utc-5, +1) can be used.\n";
        var matches = new List<string>();
        foreach (var name in zones)
        {
            var city = name.Substring(name.LastIndexOf('/') + 1).ToLowerInvariant();
            if (city == key) matches.Add(name);
        }
        if (matches.Count == 0 && key.Length >= 3)
            foreach (var name in zones)
                if (name.ToLowerInvariant().Replace('/', '_').Contains(key)) matches.Add(name);
        if (matches.Count == 1) return "ZONE:" + matches[0];
        if (matches.Count > 1)
        {
            var shown = matches.Count > MaxMatches ? matches.GetRange(0, MaxMatches) : matches;
            return $"MSG:'{Esc(Shorten(input))}' could be several zones: {string.Join(", ", shown)}{(matches.Count > MaxMatches ? $" and {matches.Count - MaxMatches} more" : "")}. Say which, for example {{cmd}}time -timezone {shown[0]}{{/}}.\n";
        }
        return $"MSG:There is no time zone called '{Esc(Shorten(input))}'. {{cmd}}time -timezone{{/}} lists the common ones, and {{cmd}}time -timezone America{{/}} (or Europe, Asia ...) every zone of a region.\n";
    }

    private static string Shorten(string text)
    {
        var plain = Clean(text);
        return plain.Length > 30 ? plain.Substring(0, 30) + "..." : plain;
    }

    /// <summary>"UTC-5" style name for an offset typed as "utc-5", "gmt+1", "+5:30"; "" if the text is not an offset; "bad" if it is one that is out of range.</summary>
    private static string ReadOffset(string word)
    {
        var rest = word;
        if (rest.StartsWith("utc")) rest = rest.Substring(3);
        else if (rest.StartsWith("gmt")) rest = rest.Substring(3);
        rest = rest.Trim();
        if (rest.Length < 2 || (rest[0] != '+' && rest[0] != '-')) return "";
        var sign = rest[0];
        var digits = rest.Substring(1).Replace(" ", "");
        int hours, minutes = 0;
        var colon = digits.IndexOf(':');
        if (colon < 0 && digits.Length == 4 && int.TryParse(digits, out var packed)) { hours = packed / 100; minutes = packed % 100; }
        else
        {
            var h = colon < 0 ? digits : digits.Substring(0, colon);
            if (!int.TryParse(h, out hours)) return "";
            if (colon >= 0 && !int.TryParse(digits.Substring(colon + 1), out minutes)) return "";
        }
        if (hours < 0 || minutes < 0 || minutes > 59) return "bad";
        var total = hours * 60 + minutes;
        if (total > (sign == '-' ? 12 * 60 : 14 * 60)) return "bad";
        if (total == 0) return "UTC";
        return "UTC" + sign + hours + (minutes == 0 ? "" : ":" + minutes.ToString("00"));
    }
}
