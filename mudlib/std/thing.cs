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
    public virtual string Stat() => $"{Self.Name}: {Name}\n" + TagStatLine();

    // ---- Tags (docs/specs/tags-and-saves.md) ----
    // A tag is a registered lower-case word (reach, damage type, kind, material, rank, nature, senses) that states what a thing IS. An
    // object declares its own in Create() (Tags.Add("sword")); the set is checked against /secure/tags the first time anyone reads it, so
    // an unregistered, reserved or doubled tag (from a wizard's object, say) is dropped, never trusted. There is deliberately no public
    // function that adds or removes a tag: only the object's own code writes Tags, and the readers below only read.

    /// <summary>The tags this object declares, written in Create(). Weapons, armour and creatures add what their kind implies when it is read.</summary>
    protected readonly List<string> Tags = new();

    private HashSet<string>? tagSet;
    private Dictionary<string, string>? tagExclusive;
    private List<string>? tagDropped;

    /// <summary>"weapon", "armour" or "creature" for the kinds of thing the registry has rules for; "" otherwise.</summary>
    protected virtual string TagCarrier() => "";

    /// <summary>Add the tags that come from somewhere other than Tags: a weapon's kind from its Category, a creature's flags. Called once, when the tags are first read.</summary>
    protected virtual void CollectTags(List<string> tags) { }

    private void EnsureTags()
    {
        if (tagSet != null) return;
        var set = new HashSet<string>();
        var exclusive = new Dictionary<string, string>();
        var dropped = new List<string>();
        var all = new List<string>(Tags);
        CollectTags(all);
        if (all.Count > 0)
        {
            List<string>? resolved = null;
            try { resolved = LoadObject("/secure/tags").Call<List<string>>("QueryResolved", all, TagCarrier()); }
            catch (MudRuntimeException) { }
            if (resolved == null) foreach (var tag in all) set.Add(tag); // no registry (it did not compile): the object's own words, unchecked
            else
                foreach (var entry in resolved)
                {
                    if (entry.StartsWith("!")) { dropped.Add(entry.Substring(1)); continue; }
                    var parts = entry.Split(':');
                    if (parts.Length != 3) continue;
                    set.Add(parts[2]);
                    if (parts[0] == "E") exclusive[parts[1]] = parts[2];
                }
        }
        tagExclusive = exclusive;
        tagDropped = dropped;
        tagSet = set;
    }

    /// <summary>Does the object carry this tag (declared, implied by its kind, or derived from a flag)?</summary>
    public bool HasTag(string tag)
    {
        EnsureTags();
        return tag != null && tagSet!.Contains(tag);
    }

    /// <summary>Every tag of the object, sorted (a copy). A player carries none.</summary>
    public List<string> QueryTags()
    {
        EnsureTags();
        var list = tagSet!.ToList();
        list.Sort(StringComparer.Ordinal);
        return list;
    }

    /// <summary>The object's one tag of an exclusive family ("reach", "damage", "kind", "armour", "material", "rank"), or "".</summary>
    public string QueryTagIn(string family)
    {
        EnsureTags();
        return family != null && tagExclusive!.TryGetValue(family, out var tag) ? tag : "";
    }

    /// <summary>The tags the object declared that the registry refused (not registered, reserved, a second of an exclusive family). Empty for real files that passed the lint.</summary>
    public List<string> QueryDroppedTags()
    {
        EnsureTags();
        return new List<string>(tagDropped!);
    }

    private string TagStatLine()
    {
        EnsureTags();
        if (tagSet!.Count == 0 && tagDropped!.Count == 0) return "";
        var line = "  Tags: " + string.Join(" ", QueryTags());
        if (tagDropped!.Count > 0) line += (tagSet.Count > 0 ? " " : "") + "(unregistered or refused: " + string.Join(" ", tagDropped.Take(8)) + ")";
        return line + "\n";
    }

    /// <summary>"a" or "an" and the word.</summary>
    protected static string AOrAn(string word) => (word.Length > 0 && "aeiou".IndexOf(word[0]) >= 0 ? "an " : "a ") + word;

    private static string MaterialWord(string material) => material == "wood" ? "wooden" : material;

    /// <summary>
    /// The short phrase for displays (the shop's list, appraise): "slashing sword", "piercing bow, at range", "light leather armour",
    /// "goblin, elite". "" for a thing with no tags that say so.
    /// </summary>
    public string QueryTagSummary()
    {
        EnsureTags();
        switch (TagCarrier())
        {
            case "weapon":
            {
                var damage = QueryTagIn("damage");
                var kind = QueryTagIn("kind");
                var words = (damage + " " + kind).Trim();
                if (words == "") return "";
                var reach = QueryTagIn("reach");
                return words + (reach == "ranged" || reach == "thrown" ? ", at range" : "");
            }
            case "armour":
            {
                var type = QueryTagIn("armour");
                var material = QueryTagIn("material");
                if (type == "") return "";
                if (type == "shield" || type == "trinket") return (material == "" ? "" : MaterialWord(material) + " ") + type;
                return type + (material == "" ? "" : " " + material) + " armour";
            }
            case "creature":
            {
                var words = new List<string>();
                foreach (var n in QueryTagsOfFamily("nature")) words.Add(n);
                var rank = QueryTagIn("rank");
                if (rank != "") words.Add(rank);
                return string.Join(", ", words);
            }
        }
        return "";
    }

    private List<string> QueryTagsOfFamily(string family)
    {
        EnsureTags();
        var members = new List<string>();
        try { members = LoadObject("/secure/tags").Call<List<string>>("QueryTagsOf", family) ?? members; } catch (MudRuntimeException) { }
        return members.Where(t => tagSet!.Contains(t)).ToList();
    }

    /// <summary>
    /// The sentence that look adds for a weapon or a piece of armour: "It is a sword: it cuts, and it is used up close." / "It is light armour,
    /// made of leather." Empty for a thing whose tags say nothing.
    /// </summary>
    protected string TagSentence()
    {
        EnsureTags();
        switch (TagCarrier())
        {
            case "weapon":
            {
                var kind = QueryTagIn("kind");
                var damage = QueryTagIn("damage") switch { "blunt" => "crushes", "slashing" => "cuts", "piercing" => "pierces", _ => "" };
                var reach = QueryTagIn("reach") switch { "melee" => "used up close", "ranged" => "used from a distance", "thrown" => "made to be thrown", _ => "" };
                if (kind == "" && damage == "" && reach == "") return "";
                var head = kind == "" ? "It" : $"It is {AOrAn(kind)}";
                var rest = new List<string>();
                if (damage != "") rest.Add($"it {damage}");
                if (reach != "") rest.Add($"it is {reach}");
                if (rest.Count == 0) return head + ".";
                return head + (kind == "" ? " " : ": ") + string.Join(", and ", rest) + ".";
            }
            case "armour":
            {
                var type = QueryTagIn("armour");
                if (type == "") return "";
                var material = QueryTagIn("material");
                var noun = type == "shield" || type == "trinket" ? AOrAn(type) : type + " armour";
                return $"It is {noun}" + (material == "" ? "" : $", made of {material}") + ".";
            }
        }
        return "";
    }

    private const string AnsiReset = "\u001b[0m";
    private const int MaxTagLength = 8; // the longest style tag ("bmagenta")

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
    /// The text with every character above 127 replaced by plain ASCII (the `charset ascii` setting): the box characters of `score` become
    /// + - |, a bullet at the start of a line becomes *, a bullet between two words becomes " - " (its spaces stay), curly quotes become straight,
    /// dashes become -, the ellipsis becomes ..., accented letters lose their accents, and anything else becomes ?. Text that is already ASCII
    /// is returned as it is. The one place it is applied is Player.CatchTell, where every message to a player passes.
    /// </summary>
    protected static string Asciify(string text)
    {
        var plain = true;
        foreach (var c in text) if (c > 127) { plain = false; break; }
        if (plain) return text;
        var result = new StringBuilder();
        var words = false;   // something other than spaces and colour codes has been written on this line
        var escape = false;  // inside a colour code
        foreach (var c in text)
        {
            if (c <= 127)
            {
                result.Append(c);
                if (c == '\u001b') escape = true;
                else if (escape) escape = !char.IsLetter(c);
                else if (c == '\n') words = false;
                else if (c > ' ') words = true;
                continue;
            }
            switch (c)
            {
                case '\u2022': case '\u25CF': case '\u25AA': case '\u00B7': case '\u2023':
                    // At the start of a line a bullet; between words a separator.
                    result.Append(words ? '-' : '*');
                    words = true;
                    break;
                case '\u2554': case '\u2557': case '\u255A': case '\u255D': case '\u250C': case '\u2510': case '\u2514': case '\u2518': case '\u253C': result.Append('+'); break;
                case '\u2550': case '\u2500': case '\u2501': result.Append('-'); break;
                case '\u2551': case '\u2502': case '\u2503': result.Append('|'); break;
                case '\u2018': case '\u2019': case '\u201A': case '\u2032': case '\u00B4': case '\u0060': result.Append('\''); break;
                case '\u201C': case '\u201D': case '\u201E': case '\u2033': case '\u00AB': case '\u00BB': result.Append('"'); break;
                case '\u2013': case '\u2014': case '\u2012': case '\u2015': case '\u2212': case '\u2010': case '\u2011': result.Append('-'); break;
                case '\u2026': result.Append("..."); break;
                case '\u00A0': case '\u2002': case '\u2003': case '\u2009': case '\u202F': result.Append(' '); break;
                case '\u2192': result.Append("->"); break;
                case '\u2190': result.Append("<-"); break;
                case '\u00D7': result.Append('x'); break;
                case '\u00B0': result.Append("deg"); break;
                case '\u00BD': result.Append("1/2"); break;
                case '\u00DF': result.Append("ss"); break;
                case '\u00C6': result.Append("AE"); break;
                case '\u00E6': result.Append("ae"); break;
                case '\u0152': result.Append("OE"); break;
                case '\u0153': result.Append("oe"); break;
                default:
                    var folded = FoldLetter(c);
                    if (folded != '\0') result.Append(folded); else result.Append('?');
                    break;
            }
            if (c != '\u00A0' && c != '\u2002' && c != '\u2003' && c != '\u2009' && c != '\u202F') words = true;
        }
        return result.ToString();
    }

    private static readonly string[] LetterGroups =
    {
        "A\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u0100\u0102\u0104", "a\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u0101\u0103\u0105",
        "C\u00C7\u0106\u010C", "c\u00E7\u0107\u010D", "D\u00D0\u010E\u0110", "d\u00F0\u010F\u0111",
        "E\u00C8\u00C9\u00CA\u00CB\u0112\u0118\u011A", "e\u00E8\u00E9\u00EA\u00EB\u0113\u0119\u011B",
        "I\u00CC\u00CD\u00CE\u00CF\u012A\u0130", "i\u00EC\u00ED\u00EE\u00EF\u012B\u0131", "L\u0141", "l\u0142",
        "N\u00D1\u0143\u0147", "n\u00F1\u0144\u0148", "O\u00D2\u00D3\u00D4\u00D5\u00D6\u00D8\u014C\u0150", "o\u00F2\u00F3\u00F4\u00F5\u00F6\u00F8\u014D\u0151",
        "S\u015A\u0160\u015E", "s\u015B\u0161\u015F", "T\u0162\u0164", "t\u0163\u0165",
        "U\u00D9\u00DA\u00DB\u00DC\u016A\u016E\u0170", "u\u00F9\u00FA\u00FB\u00FC\u016B\u016F\u0171", "Y\u00DD\u0178", "y\u00FD\u00FF",
        "Z\u0179\u017B\u017D", "z\u017A\u017C\u017E",
    };

    /// <summary>The plain letter an accented letter stands for, or '\0'.</summary>
    private static char FoldLetter(char c)
    {
        foreach (var group in LetterGroups)
            if (group.IndexOf(c, 1) > 0) return group[0];
        return '\0';
    }

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
            // A tag is a few letters: look for its end only that far, so that text full of "{" costs one pass, not one per brace.
            var end = text.IndexOf('}', i + 1, Math.Min(MaxTagLength + 1, text.Length - i - 1));
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

    /// <summary>A number with one decimal place, written the same on every machine: 2.5, 11.0, -0.5.</summary>
    protected static string Num(double value)
    {
        var tenths = (long)Math.Round(value * 10, MidpointRounding.AwayFromZero);
        var sign = tenths < 0 ? "-" : "";
        tenths = Math.Abs(tenths);
        return sign + (tenths / 10) + "." + (tenths % 10);
    }

    /// <summary>The text with its first letter in upper case.</summary>
    protected static string Cap(string text) => text.Length == 0 ? text : char.ToUpper(text[0]) + text.Substring(1);

    // ---- Items in containers: quantities, stacks and save records ----
    // The builder's API for items with quality and quantity (docs/BUILDING.md, "Quality and stacks"). The state itself
    // lives in /std/item; these helpers work through its public functions, so they have exactly the rights of the object
    // that calls them: sandbox code cannot use them to raise the quality or quantity of a real item.

    /// <summary>How many items an object stands for: its quantity, or 1 for anything that is not a stack.</summary>
    protected static int QuantityOf(ObjectRef ob) => Math.Max(1, ob.Call<int>("QueryQuantity"));

    /// <summary>How n of an item are described ("3 fine wolf skins"); its short description for things that are not items.</summary>
    protected static string Describe(ObjectRef item, int n) =>
        (n >= QuantityOf(item) ? item.Call<string>("Short") : item.Call<string>("DescribeCount", n)) ?? item.Call<string>("Short") ?? "something";

    /// <summary>One item of a stack or one separate object, described singly ("a fine wolf skin"); never the count.</summary>
    protected static string Single(ObjectRef item) =>
        (item.Call<int>("QueryAmount") > 0 || QuantityOf(item) == 1 ? item.Call<string>("Short") : item.Call<string>("DescribeCount", 1)) ?? item.Call<string>("Short") ?? "something";

    /// <summary>
    /// What makes two items identical in a list: the same single description and the same state (quality and properties; the
    /// quantity of a stack does not count), and the same mark (so worn or wielded items never merge with carried ones).
    /// </summary>
    protected static string SameKey(ObjectRef item, string mark = "")
    {
        var state = (item.Call<string>("QueryState") ?? "").Split('&', StringSplitOptions.RemoveEmptyEntries).Where(p => !p.StartsWith("n="));
        return Single(item) + "|" + string.Join("&", state) + "|" + mark;
    }

    /// <summary>A list entry: "a minor healing potion", or "a minor healing potion (x2)" for more than one.</summary>
    protected static string ListName(string single, int n) => n > 1 ? $"{single} (x{n})" : single;

    /// <summary>
    /// The items grouped into list entries, in the order each first appears: identical items collapse into one entry with a
    /// count ("a minor healing potion (x2)"). mark gives an item's suffix (" (wielded)"); marked items are never merged with
    /// unmarked ones. Each entry also carries the items it stands for and how many there are in all.
    /// </summary>
    protected static List<(string text, int n, List<ObjectRef> items)> Collapse(IEnumerable<ObjectRef> items, Func<ObjectRef, string>? mark = null)
    {
        var order = new List<string>();
        var groups = new Dictionary<string, (string single, string mark, int n, List<ObjectRef> items)>();
        foreach (var item in items)
        {
            if (!item.IsAlive) continue;
            var m = mark?.Invoke(item) ?? "";
            var key = SameKey(item, m);
            if (!groups.TryGetValue(key, out var g)) { g = (Single(item), m, 0, new List<ObjectRef>()); order.Add(key); }
            g.n += QuantityOf(item);
            g.items.Add(item);
            groups[key] = g;
        }
        return order.Select(k => (ListName(groups[k].single, groups[k].n) + groups[k].mark, groups[k].n, groups[k].items)).ToList();
    }

    /// <summary>
    /// Read a typed amount: "3 skins" (count 3), "all skins" (all), "all" (all, noun ""), "skin" (count 1). False if the
    /// text names nothing or the count is not positive.
    /// </summary>
    protected static bool ParseAmount(string args, out int count, out bool all, out string noun)
    {
        args = args.Trim();
        count = 1;
        all = false;
        noun = args;
        if (args == "all") { all = true; noun = ""; return true; }
        if (args.StartsWith("all ")) { all = true; noun = args.Substring(4).Trim(); return noun != ""; }
        var space = args.IndexOf(' ');
        if (space > 0 && int.TryParse(args.Substring(0, space), out var n))
        {
            if (n < 1) return false;
            count = n;
            noun = args.Substring(space + 1).Trim();
        }
        return noun != "";
    }

    /// <summary>The live objects directly in a container that answer to a name (all of them for ""), in inventory order.</summary>
    protected List<ObjectRef> Matching(ObjectRef container, string noun)
    {
        var id = noun.ToLowerInvariant();
        var all = AllInventory(container).Where(o => o.IsAlive && o != Self).ToList();
        var found = all.Where(o => id == "" || o.Call<bool>("Id", id)).ToList();
        if (found.Count > 0 || id == "") return found;
        // "skin 2", "2.skin", "second skin": only that one of the things the name fits, in listed order.
        var position = SplitOrdinal(id, out var name);
        if (position == 0) return found;
        var same = all.Where(o => o.Call<bool>("Id", name)).ToList();
        return position <= same.Count ? new List<ObjectRef> { same[position - 1] } : found;
    }

    /// <summary>
    /// Move n items of a stack (or the whole object, for n at or above its quantity) into a destination and merge them with
    /// a matching stack there. Returns the object that holds them afterwards, or null if nothing moved (the split was
    /// refused or the move failed; a part already split off is merged back where it was).
    /// </summary>
    protected ObjectRef? MovePortion(ObjectRef item, int n, ObjectRef destination)
    {
        if (n < 1 || !item.IsAlive) return null;
        var part = n >= QuantityOf(item) ? item : item.Call<ObjectRef>("Split", n);
        if (part == null) return null;
        try
        {
            MoveObject(part, destination);
        }
        catch (MudRuntimeException)
        {
            if (part != item && part.IsAlive) part.Call("Settle");
            return null;
        }
        return part.Call<ObjectRef>("Settle") ?? part;
    }

    /// <summary>The save record of an item: its program path, then "?" and its state if that is not the default.</summary>
    protected static string RecordOf(ObjectRef item)
    {
        var state = item.Call<string>("QueryState") ?? "";
        return state == "" ? item.ProgramPath : item.ProgramPath + "?" + state;
    }

    /// <summary>How many items a save record stands for (its "n=", or 1).</summary>
    protected static int RecordQuantity(string record)
    {
        var mark = record.IndexOf('?');
        if (mark < 0) return 1;
        foreach (var part in record.Substring(mark + 1).Split('&', StringSplitOptions.RemoveEmptyEntries))
            if (part.StartsWith("n=") && int.TryParse(part.Substring(2), out var n) && n > 1) return n;
        return 1;
    }

    /// <summary>
    /// Make the item a save record describes (a bare path, as old saves hold, is an item in the default state), put it in
    /// the destination and merge it with a matching stack. Returns the object that holds it, or null if its state could
    /// not be set. Throws (MudRuntimeException) if the file is gone or does not compile.
    /// </summary>
    protected ObjectRef? CloneRecord(string record, ObjectRef destination)
    {
        var mark = record.IndexOf('?');
        var path = mark < 0 ? record : record.Substring(0, mark);
        var state = mark < 0 ? "" : record.Substring(mark + 1);
        var wanted = RecordQuantity(record);
        ObjectRef? first = null;
        var made = 0;
        while (made < wanted)
        {
            var item = CloneObject(path);
            if (state != "" && item.Call("RestoreState", state) is false)
            {
                item.Call("Vanish");
                return first;
            }
            made += QuantityOf(item);
            MoveObject(item, destination);
            var holder = item.Call<ObjectRef>("Settle") ?? item;
            first ??= holder;
            if (made < wanted && made >= 1000) break; // a file that stopped stacking: at most a thousand separate copies
        }
        return first;
    }

    /// <summary>
    /// Create count items of a file at a quality in a container, in one call: one stack if the file is stackable (merged
    /// with a matching stack already there), otherwise count separate objects (at most 100). Returns the object holding
    /// them (the last one, for separate objects), or null if refused. The path may be a save record with properties
    /// ("/std/skin?p.kind=wolf", see CloneStated); its "n=" and "q=" are overridden by count and quality.
    /// </summary>
    protected ObjectRef? CreateIn(string path, int count, int quality, ObjectRef container)
    {
        if (count < 1 || quality < 0 || quality > 5) return null;
        ObjectRef? result = null;
        for (var made = 0; made < count;)
        {
            var item = CloneStated(path);
            if (item == null) return result;
            var stack = item.Call<bool>("QueryStackable");
            if (!stack && count > 100) { item.Call("Vanish"); return null; }
            var n = stack ? count : 1;
            if ((item.Call("QueryQuality") is int iq ? iq != quality : quality != 1) && item.Call<bool>("SetQuality", quality) != true
                || (item.Call("QueryQuantity") is int iqn ? iqn != n : n > 1) && item.Call<bool>("SetQuantity", n) != true)
            {
                item.Call("Vanish");
                return result;
            }
            MoveObject(item, container);
            result = item.Call<ObjectRef>("Settle") ?? item;
            made += n;
        }
        return result;
    }

    /// <summary>
    /// Clone the item a save record names, in the state the record gives (quality, quantity and properties after the "?"; a
    /// bare path is the default state), without putting it anywhere. Null if the item refused that state (it is then gone).
    /// Throws (MudRuntimeException) if the file is gone or does not compile. CreateIn and shop stock accept such records, so a
    /// generic item whose kind is a property ("/std/skin?p.kind=rabbit") can be made and sold like any other.
    /// </summary>
    protected ObjectRef? CloneStated(string record)
    {
        var mark = record.IndexOf('?');
        var item = CloneObject(mark < 0 ? record : record.Substring(0, mark));
        if (mark < 0 || mark == record.Length - 1) return item;
        if (item.Call("RestoreState", record.Substring(mark + 1)) is true) return item;
        item.Call("Vanish");
        return null;
    }

    /// <summary>
    /// Take count items that match a test out of a container: destroy them, or move them to destination if one is given.
    /// All or nothing: false, and nothing taken, if the container holds fewer. Splits the last stack it takes from.
    /// </summary>
    protected bool TakeFrom(ObjectRef container, int count, Func<ObjectRef, bool> match, ObjectRef? destination = null)
    {
        if (count < 1) return false;
        var found = AllInventory(container).Where(o => o.IsAlive && o.Call("QueryQuantity") is int && match(o)).ToList();
        if (found.Sum(o => (long)QuantityOf(o)) < count) return false;
        var plan = new List<(ObjectRef ob, int n)>();
        var left = count;
        foreach (var ob in found)
        {
            if (left == 0) break;
            var n = Math.Min(left, QuantityOf(ob));
            plan.Add((ob, n));
            left -= n;
        }
        // Only the last stack can need splitting: do that first, so a refusal leaves everything as it was.
        var (lastOb, lastN) = plan[plan.Count - 1];
        var lastPart = lastN >= QuantityOf(lastOb) ? lastOb : lastOb.Call<ObjectRef>("Split", lastN);
        if (lastPart == null) return false;
        plan[plan.Count - 1] = (lastPart, lastN);
        foreach (var (ob, n) in plan)
        {
            if (destination == null) ob.Call("Vanish");
            else MovePortion(ob, n, destination);
        }
        return true;
    }

    /// <summary>How many items of one file a container holds directly, by quality (0 Poor to 5 Legendary). Qualities it has none of are left out.</summary>
    protected Dictionary<int, int> CountIn(ObjectRef container, string path)
    {
        var result = new Dictionary<int, int>();
        foreach (var ob in AllInventory(container))
        {
            if (!ob.IsAlive || ob.ProgramPath != path || ob.Call("QueryQuality") is not int q) continue;
            result[q] = result.GetValueOrDefault(q) + QuantityOf(ob);
        }
        return result;
    }
}
