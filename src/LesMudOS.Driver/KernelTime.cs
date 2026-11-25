using System.Globalization;
using LesMudOS.Api;

namespace LesMudOS.Driver;

// Time zone efuns (docs/specs/play-feel.md, the `time` command): the mudlib may not use TimeZoneInfo (it is not on the sandbox allow-list), so the
// driver answers two questions for it. A zone name is only ever matched against the driver's own list (the system's zone database, or a fixed
// "UTC+5:30" offset): nothing a player typed reaches the file system, and nothing here logs a name.
internal sealed partial class Kernel
{
    private static readonly object _zoneLock = new();
    private static string[]? _zoneNames;
    private static Dictionary<string, TimeZoneInfo>? _zonesByName;

    private static void LoadZones()
    {
        lock (_zoneLock)
        {
            if (_zonesByName != null) return;
            var byName = new Dictionary<string, TimeZoneInfo>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var zone in TimeZoneInfo.GetSystemTimeZones())
                {
                    var id = zone.Id;
                    if (id.Length == 0 || id.Length > 60 || id.StartsWith("Etc/", StringComparison.Ordinal) || id.StartsWith("posix", StringComparison.Ordinal)
                        || id.StartsWith("right", StringComparison.Ordinal) || id.StartsWith("SystemV", StringComparison.Ordinal) || id.Contains("..") || id.Contains('\\')) continue;
                    if (id != "UTC" && !id.Contains('/')) continue;
                    foreach (var ch in id) if (!(char.IsAsciiLetterOrDigit(ch) || ch is '/' or '_' or '-' or '+')) goto skip;
                    byName[id] = zone;
                skip:;
                }
            }
            catch (Exception)
            {
                byName.Clear(); // no zone database on this machine: only fixed offsets and UTC remain
            }
            _zonesByName = byName;
            _zoneNames = byName.Keys.OrderBy(n => n, StringComparer.Ordinal).ToArray();
        }
    }

    /// <summary>The IANA names of the zones the system knows (sorted; empty when it has no zone database). "UTC" is always answered by LocalTime.</summary>
    public string[] TimeZoneNames()
    {
        LoadZones();
        return (string[])_zoneNames!.Clone();
    }

    /// <summary>
    /// The wall clock now in a zone, as "year|month|day|hour|minute|second|weekday|offsetMinutes|daylight|name" (weekday 0 = Sunday, daylight 0 or 1,
    /// name the zone's own spelling). The zone is one of TimeZoneNames, "UTC", or a fixed offset "UTC+5", "UTC-3:30". "" if it is none of those.
    /// </summary>
    public string LocalTime(string zoneId)
    {
        if (zoneId == null || zoneId.Length > 60) return "";
        LoadZones();
        var now = DateTimeOffset.UtcNow;
        TimeSpan offset;
        var daylight = false;
        string name;
        if (zoneId.Equals("UTC", StringComparison.OrdinalIgnoreCase)) { offset = TimeSpan.Zero; name = "UTC"; }
        else if (_zonesByName!.TryGetValue(zoneId, out var zone))
        {
            offset = zone.GetUtcOffset(now);
            daylight = zone.IsDaylightSavingTime(now);
            name = zone.Id;
        }
        else if (TryFixedOffset(zoneId, out offset)) name = FixedName(offset);
        else return "";
        var local = now.ToOffset(offset);
        return string.Join("|", local.Year, local.Month, local.Day, local.Hour, local.Minute, local.Second, (int)local.DayOfWeek,
            (int)offset.TotalMinutes, daylight ? 1 : 0, name);
    }

    private static string FixedName(TimeSpan offset)
    {
        var minutes = (int)offset.TotalMinutes;
        if (minutes == 0) return "UTC";
        var abs = Math.Abs(minutes);
        return "UTC" + (minutes < 0 ? "-" : "+") + (abs / 60).ToString(CultureInfo.InvariantCulture) + (abs % 60 == 0 ? "" : ":" + (abs % 60).ToString("00", CultureInfo.InvariantCulture));
    }

    private static bool TryFixedOffset(string text, out TimeSpan offset)
    {
        offset = TimeSpan.Zero;
        if (!text.StartsWith("UTC", StringComparison.OrdinalIgnoreCase) || text.Length < 5) return false;
        var sign = text[3];
        if (sign != '+' && sign != '-') return false;
        var parts = text.Substring(4).Split(':');
        if (parts.Length > 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours) || hours > 14) return false;
        var minutes = 0;
        if (parts.Length == 2 && (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minutes) || minutes > 59 || parts[1].Length != 2)) return false;
        var total = hours * 60 + minutes;
        if (total > 14 * 60) return false;
        offset = TimeSpan.FromMinutes(sign == '-' ? -total : total);
        return true;
    }
}
