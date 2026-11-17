/// <summary>Base for anything with a name and a description: items, creatures, players.</summary>
public abstract class Thing : MudObject
{
    protected string Name = "thing";
    protected string? ShortDesc;
    protected string? LongDesc;
    protected List<string> Ids = new();

    public string QueryName() => Name;

    public override bool Id(string id) => id == Name.ToLower() || Ids.Contains(id);

    public override string? Short() => ShortDesc ?? Name;

    public override string Long() => (LongDesc ?? $"You see nothing special about {Short()}.") + "\n";

    /// <summary>May a player pick this up?</summary>
    public virtual bool CanGet() => false;

    /// <summary>The numbers a builder wants to see: shown by the wizard command "stat".</summary>
    public virtual string Stat() => $"{Self.Name}: {Name}\n";

    private const string AnsiReset = "\u001b[0m";

    /// <summary>The SGR parameters for a style tag, or null if it is not one.</summary>
    private static string? StyleCode(string tag) => tag switch
    {
        "cmd" => "1;96",
        "title" => "1",
        "b" => "1",
        "u" => "4",
        "red" => "31", "green" => "32", "yellow" => "33", "blue" => "34",
        "magenta" => "35", "cyan" => "36", "white" => "37",
        "bred" => "91", "bgreen" => "92", "byellow" => "93", "bblue" => "94",
        "bmagenta" => "95", "bcyan" => "96", "bwhite" => "97",
        _ => null
    };

    /// <summary>
    /// Render the markup: known tags become ANSI sequences (or nothing, with
    /// colour off), "{{" becomes "{", and anything else in braces stays as
    /// written. A style still open at the end of a line is closed there, so
    /// colour cannot leak into later output.
    /// </summary>
    protected static string Markup(string text, bool colour)
    {
        if (text.IndexOf('{') < 0) return text;
        var result = new StringBuilder();
        var open = false;
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '\n' && open)
            {
                if (colour) result.Append(AnsiReset);
                open = false;
            }
            if (c != '{') { result.Append(c); i++; continue; }
            if (i + 1 < text.Length && text[i + 1] == '{') { result.Append('{'); i += 2; continue; }
            var end = text.IndexOf('}', i + 1);
            var tag = end < 0 ? null : text.Substring(i + 1, end - i - 1);
            var code = tag == null || tag == "/" ? null : StyleCode(tag);
            if (tag == "/" || code != null)
            {
                if (colour) result.Append(tag == "/" ? AnsiReset : "\u001b[" + code + "m");
                open = tag != "/";
                i = end + 1;
                continue;
            }
            result.Append('{'); // not a known tag: left as written
            i++;
        }
        if (open && colour) result.Append(AnsiReset);
        return result.ToString();
    }

    /// <summary>
    /// Make text typed by a player safe to put into game text: "{" becomes "{{",
    /// which players see as "{" and which cannot start a colour tag. Use it on
    /// every piece of player input that is shown to anyone or stored.
    /// </summary>
    protected static string Esc(string text) => text.Replace("{", "{{");

    /// <summary>The text with its first letter in upper case.</summary>
    protected static string Cap(string text) => text.Length == 0 ? text : char.ToUpper(text[0]) + text.Substring(1);
}
