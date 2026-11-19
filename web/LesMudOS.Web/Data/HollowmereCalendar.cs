using System.Text.RegularExpressions;

namespace LesMudOS.Web.Data;

/// <summary>The game's calendar (mudlib/secure/chronicle.cs, DateFor): one real day is one day in Hollowmere, twelve months of
/// thirty days and then the Turning of five days (six when a real 29 February falls in the year). Ported so that the site can
/// say what today's date is without asking the game.</summary>
public static partial class HollowmereCalendar
{
    public static readonly string[] Months =
    {
        "Deepfrost", "Thawmoon", "Sowing", "Blossom", "Greentide", "Highsun",
        "Goldfields", "Harvest", "Leaf-fall", "Mistwane", "Firstfrost", "Longnight"
    };

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

    public static int YearLength(long startDay)
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
        if (tens is >= 11 and <= 13) return n + "th";
        return (n % 10) switch { 1 => n + "st", 2 => n + "nd", 3 => n + "rd", _ => n + "th" };
    }

    /// <summary>The date of a Unix day, for a Year 1 that began on <paramref name="epochDay"/>: "the 17th Day of Harvest, Year 3".</summary>
    public static HollowmereDate DateFor(long epochDay, long unixDay)
    {
        if (unixDay < epochDay) unixDay = epochDay;
        var start = epochDay;
        var year = 1;
        while (true)
        {
            var length = YearLength(start);
            if (unixDay < start + length) break;
            start += length;
            year++;
            if (year > 100000) break; // a corrupt epoch must not spin forever
        }
        var offset = (int)(unixDay - start);
        if (offset >= 360)
        {
            var day = Ordinal(offset - 359);
            return new HollowmereDate($"the {day} Day of the Turning, Year {year}", day, "the Turning", "Year " + year);
        }
        var d = Ordinal(offset % 30 + 1);
        var month = Months[offset / 30];
        return new HollowmereDate($"the {d} Day of {month}, Year {year}", d, month, "Year " + year);
    }

    [GeneratedRegex(@"^\s*the (\d+(?:st|nd|rd|th)) Day of (.+?), Year (\d+)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DateText();

    /// <summary>Splits a date string the game stored in an entry. Anything unrecognised is kept whole.</summary>
    public static HollowmereDate Parse(string text)
    {
        var m = DateText().Match(text ?? "");
        return m.Success
            ? new HollowmereDate(text!.Trim(), m.Groups[1].Value, m.Groups[2].Value, "Year " + m.Groups[3].Value)
            : new HollowmereDate((text ?? "").Trim(), "", "", "");
    }
}
