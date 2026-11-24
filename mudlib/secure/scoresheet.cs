/// <summary>
/// The look of `score` (docs/specs/score.md): the box with the name, the rule under each heading, the one sentence for a Renown rank, the
/// Chronicle Standing section from the character's own records, and the wrapping of long lines to 78 columns. It holds no state and
/// changes nothing; the numbers come from the character's own body, which asks for the pieces it cannot make itself.
/// </summary>
public class ScoreSheet : MudObject
{
    private const int BoxInside = 46;   // the box is 48 columns wide
    private const int MaxColumns = 78;  // nothing wider than 80
    private const string Ledger = "/secure/renown";
    private const string Chronicle = "/secure/chronicle";

    /// <summary>One sentence for each Renown rank (the order of the ledger's tiers).</summary>
    private static readonly string[] RankSentences =
    {
        "A stranger to the Vale.",
        "Known by sight to the folk of Vincent's Hollow.",
        "Recognized throughout Vincent's Hollow.",
        "Acclaimed across the length of Greenvale.",
        "Renowned on every road of Greenvale.",
        "Famous in every tavern from the Hollow to Oakenfell.",
        "A hero of Hollowmere, and the songs say so.",
        "A living legend of Hollowmere.",
        "A name all Hollowmere tells its children.",
    };

    private static string Esc(string text) => text.Replace("{", "{{");

    private static string Center(string text, int width)
    {
        if (text.Length > width) text = text.Substring(0, width - 3) + "...";
        var left = (width - text.Length) / 2;
        return new string(' ', left) + text + new string(' ', width - text.Length - left);
    }

    /// <summary>The box: the name (with its title words) centred, and, if there is one, the chosen Renown title in quotes. Both are plain text here.</summary>
    public string Box(string name, string title)
    {
        var rows = new List<string> { "{title}" + Esc(Center(name, BoxInside)) + "{/}" };
        if (title != "") rows.Add(Esc(Center("\"" + title + "\"", BoxInside)));
        var text = "╔" + new string('═', BoxInside) + "╗\n";
        foreach (var row in rows) text += "║" + row + "║\n";
        return text + "╚" + new string('═', BoxInside) + "╝\n";
    }

    /// <summary>A heading with its rule under it.</summary>
    public string Heading(string heading) => "{b}" + heading + "{/}\n" + new string('─', 25) + "\n";

    /// <summary>The sentence for this character's Renown rank.</summary>
    public string RankLine(string name)
    {
        var ledger = LoadObject(Ledger);
        var tier = ledger.Call<int>("TierIndex", ledger.Call<int>("QueryRenown", name));
        return RankSentences[Math.Max(0, Math.Min(tier, RankSentences.Length - 1))] + "\n";
    }

    /// <summary>
    /// Chronicle Standing: the rank, the notable deeds (their entries that are featured or a first), the number of their entries, and the
    /// latest three headlines, newest first. A wizard makes no history: one line and the wizard level instead.
    /// </summary>
    public string Standing(string name, int wizardLevel)
    {
        var text = Heading("Chronicle Standing");
        if (wizardLevel > 0) return text + $"Wizards make no history in the Chronicle. Wizard level {wizardLevel}.\n";
        var ledger = LoadObject(Ledger);
        var chronicle = LoadObject(Chronicle);
        var amount = ledger.Call<int>("QueryRenown", name);
        text += "Renown Rank: " + ledger.Call<string>("TierFor", amount) + "\n";
        var infamy = ledger.Call<int>("QueryInfamy", name);
        if (infamy > 0) text += $"Infamy: {infamy}\n";
        var ids = chronicle.Call<List<int>>("QueryEntriesFor", name) ?? new List<int>();
        var notable = ids.Count(id => chronicle.Call<string>("QueryEntry", id, "featured") == "1" || chronicle.Call<string>("QueryEntry", id, "first") == "1");
        text += $"Notable Deeds: {notable}\nChronicle Entries: {ids.Count}\n\n{{b}}Latest Deeds{{/}}\n";
        if (ids.Count == 0) return text + " None yet.\n";
        for (var i = ids.Count - 1; i >= 0 && i >= ids.Count - 3; i--)
            text += " • " + Esc(chronicle.Call<string>("QueryEntry", ids[i], "headline") ?? "") + "\n";
        return text;
    }

    private static int Visible(string line)
    {
        var count = 0;
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '{')
            {
                var end = line.IndexOf('}', i);
                if (end > i && end - i <= 8 && line.Substring(i + 1, end - i - 1).All(c => char.IsLetter(c) || c == '/')) { i = end; continue; }
            }
            count++;
        }
        return count;
    }

    /// <summary>Wrap every line of the text that is wider than 78 columns at a space (markup tags do not count towards the width).</summary>
    public string Wrap(string text)
    {
        var result = new System.Text.StringBuilder();
        foreach (var line in text.TrimEnd('\n').Split('\n'))
        {
            var current = "";
            foreach (var word in line.Split(' '))
            {
                if (current != "" && Visible(current) + 1 + Visible(word) > MaxColumns) { result.Append(current).Append('\n'); current = word; }
                else current = current == "" ? word : current + " " + word;
            }
            result.Append(current).Append('\n');
        }
        return result.ToString();
    }

    /// <summary>Items joined by three spaces, as many to a line as fit in 78 columns.</summary>
    public string Columns(List<string> items)
    {
        var text = "";
        var line = "";
        foreach (var item in items)
        {
            if (line != "" && Visible(line) + 3 + Visible(item) > MaxColumns) { text += line + "\n"; line = item; }
            else line = line == "" ? item : line + "   " + item;
        }
        return line == "" ? text : text + line + "\n";
    }
}
