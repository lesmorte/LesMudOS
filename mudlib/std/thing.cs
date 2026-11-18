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
        return AllInventory(container).Where(o => o.IsAlive && o != Self && (id == "" || o.Call<bool>("Id", id))).ToList();
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
    /// them (the last one, for separate objects), or null if refused.
    /// </summary>
    protected ObjectRef? CreateIn(string path, int count, int quality, ObjectRef container)
    {
        if (count < 1 || quality < 0 || quality > 5) return null;
        ObjectRef? result = null;
        for (var made = 0; made < count;)
        {
            var item = CloneObject(path);
            var stack = item.Call<bool>("QueryStackable");
            if (!stack && count > 100) { item.Call("Vanish"); return null; }
            var n = stack ? count : 1;
            if ((quality != 1 && item.Call<bool>("SetQuality", quality) != true) || (n > 1 && item.Call<bool>("SetQuantity", n) != true))
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
