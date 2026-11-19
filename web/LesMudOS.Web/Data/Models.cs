namespace LesMudOS.Web.Data;

/// <summary>A date split for display in the margin of the Chronicle: "3rd", "Deepfrost", "Year 1".</summary>
public sealed record HollowmereDate(string Full, string Day, string Month, string Year);

/// <summary>A public Chronicle entry, already filtered and cleaned. Nothing here is hardcoded: it all comes from the game's file.</summary>
public sealed record ChronicleEntry(
    int Id, string Title, string Summary, string Significance, int Score, HollowmereDate Date, long GameDay,
    IReadOnlyList<string> Names, string Category, string ClassName, bool Featured, string? Where);

public sealed record Hero(string Name, int Renown, string Tier, string Title, string? Calling, string? ClassName);

/// <summary>What the website knows of the world right now. Every number is nullable: if it cannot be computed it is not shown.</summary>
public sealed record WorldSnapshot(
    IReadOnlyList<ChronicleEntry> Entries,   // public entries, newest first
    IReadOnlyList<Hero> Heroes,              // top ten, highest Renown first
    int? Characters,                         // characters created (wizards excluded); null if there is no roster
    string? Today)                           // today's date in Hollowmere; null if the Chronicle has no start date
{
    public static readonly WorldSnapshot Empty = new(Array.Empty<ChronicleEntry>(), Array.Empty<Hero>(), null, null);

    /// <summary>The five highest-scoring public entries, highest first (ties: newest first).</summary>
    public IReadOnlyList<ChronicleEntry> Top(int count)
        => Entries.OrderByDescending(e => e.Score).ThenByDescending(e => e.Id).Take(count).ToList();
}
