// @inherit /std/thing

/// <summary>
/// Treasure hidden in a place. It lies in a room and cannot be picked up as an object. A character takes what it holds
/// with the ordinary words ("get purse", "take purse", "loot purse"), once, and ever after is told they have.
///
/// The rule it carries out: each character can take a find once and never again, and the find is worth less to each new
/// finder than to the one before, until nothing is left. It never refills. The first finder gets Gold; each next finder gets
/// FallOff percent of what the one before got, rounded down, until that is below MinimumGold, and then the find is empty for
/// everyone who comes after. The record of who has taken it lives in /secure/finds (not in this object, so reloading the
/// room or the file changes nothing), and the register decides, so this object is only the place and the words.
///
/// A find is told apart by its file: the register keys it by the program path of this object, so a find is one file, two
/// stashes are two files, and renaming or moving the file makes a new stash. Only finds in the trusted trees (/domains and
/// the other trees of the event service) are accepted, and only in rooms of those trees; a find under /wiz pays nothing.
///
/// What a looker sees follows what they can take: the full description for a character who has not taken it and for whom
/// something is left, TakenShort/TakenLong for one who has, EmptyShort/EmptyLong for one who has not but finds it empty.
///
/// Set everything in Create(). See docs/BUILDING.md, "A find".
/// </summary>
public class Find : Thing
{
    /// <summary>Gold for the first finder. 0 with an ItemPath makes a keepsake: no gold, one item for every character, no end.</summary>
    protected int Gold;
    /// <summary>The percent of the one before's amount that each next finder gets, rounded down (0 to 99; default 60: 100, 60, 36, 21, 12 ...).</summary>
    protected int FallOff = 60;
    /// <summary>When the amount for the next finder would be below this, the find is empty (default 1).</summary>
    protected int MinimumGold = 1;
    /// <summary>A file of an item that each finder receives, one each. Null for none.</summary>
    protected string? ItemPath;
    /// <summary>How many of the first finders receive the item. 0 means every finder while gold remains (or every character, for a keepsake).</summary>
    protected int ItemFinders;

    /// <summary>The short description for a character who has already taken it. Default: the short description and "(taken by you)".</summary>
    protected string? TakenShort;
    /// <summary>The long description for a character who has already taken it.</summary>
    protected string? TakenLong;
    /// <summary>The short description for a character who has not taken it, when nothing is left. Default: the short description and "(picked clean)".</summary>
    protected string? EmptyShort;
    /// <summary>The long description for a character who has not taken it, when nothing is left.</summary>
    protected string? EmptyLong;

    /// <summary>What the finder is told when gold is paid; "%gold%" is the amount. Default: "You take %gold% gold from the &lt;name&gt;."</summary>
    protected string? GoldLine;
    /// <summary>What the finder is told when an item is given; "%item%" is its short description. Default: "You also take %item%."</summary>
    protected string? ItemLine;
    /// <summary>What the finder is told when they try again. Default: one plain line.</summary>
    protected string? TakenLine;
    /// <summary>What a later finder is told when nothing is left. Default: someone has been here before them.</summary>
    protected string? EmptyLine;

    private const string Register = "/secure/finds";

    // The answers of the register that matter for a description (see /secure/finds).
    private const int Open = 0, WasTaken = 1, WasEmpty = 2;

    public override bool CanGet() => false;

    // ---- What the register says ----

    /// <summary>How the register sees this character and this find: Open, WasTaken or WasEmpty (anything else shows the full description).</summary>
    private int StatusFor(ObjectRef? who)
    {
        if (who == null || !who.IsAlive || !who.Call<bool>("IsPlayerCharacter") || Environment(who) != Environment() || Environment() == null) return Open;
        var name = who.Call<string>("QueryName");
        if (string.IsNullOrEmpty(name)) return Open;
        try
        {
            var status = LoadObject(Register).Call<int>("QueryStatus", name, Gold, FallOff, MinimumGold, ItemPath != null, ItemFinders);
            return status is WasTaken or WasEmpty ? status : Open;
        }
        catch (MudRuntimeException)
        {
            return Open;
        }
    }

    private static string Plain(string text)
    {
        foreach (var article in new[] { "a ", "an ", "the " })
            if (text.StartsWith(article, StringComparison.OrdinalIgnoreCase)) return text.Substring(article.Length);
        return text;
    }

    // ---- Description: honest for the character looking ----

    /// <summary>
    /// The short description for whoever is looking (ThisPlayer(), which is the looker for a room's listing and for the look
    /// command). Anyone who is not a player standing here sees the full one.
    /// </summary>
    public override string? Short()
    {
        var full = ShortDesc ?? Name;
        return StatusFor(ThisPlayer()) switch
        {
            WasTaken => TakenShort ?? full + " (taken by you)",
            WasEmpty => EmptyShort ?? full + " (picked clean)",
            _ => full,
        };
    }

    public override string Long()
    {
        return StatusFor(ThisPlayer()) switch
        {
            WasTaken => (TakenLong ?? "You have already taken what it held. There is nothing more in it for you.") + "\n",
            WasEmpty => (EmptyLong ?? "Someone has been here before you. Nothing is left.") + "\n",
            _ => (LongDesc ?? $"You see nothing special about {ShortDesc ?? Name}.") + "\n",
        };
    }

    public string QueryFindId() => Self.ProgramPath;

    public override string Stat()
    {
        var finders = 0;
        try { finders = LoadObject(Register).Call<int>("QueryFinders"); }
        catch (MudRuntimeException) { }
        return base.Stat() + $"  Find id {Self.ProgramPath}: first {Gold} gold, {FallOff}% kept by each next finder, empty below {MinimumGold}" +
               (ItemPath != null ? $", item {ItemPath} for {(ItemFinders > 0 ? "the first " + ItemFinders : "every")} finder{(ItemFinders == 1 ? "" : "s")}" : "") +
               $"; {finders} finder(s) so far\n";
    }

    // ---- The words ----

    public override void Init()
    {
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        AddAction("get", TryTake);
        AddAction("take", TryTake);
        AddAction("loot", TryTake);
    }

    /// <summary>
    /// "get purse", "take purse", "loot purse". Handled only when the words name this find and it is the first thing in
    /// the room that answers to them; everything else goes on to the ordinary commands.
    /// </summary>
    private bool TryTake(string args)
    {
        var who = ThisPlayer();
        var room = Environment();
        var word = args.Trim().ToLowerInvariant();
        if (who == null || room == null || word == "" || Environment(who) != room || !Id(word) || Present(word, room) != Self) return false;

        List<int>? answer;
        try { answer = LoadObject(Register).Call<List<int>>("Claim", who, Gold, FallOff, MinimumGold, ItemPath != null, ItemFinders); }
        catch (MudRuntimeException) { answer = null; }
        if (answer == null || answer.Count < 3)
        {
            Write("Nothing comes of it.\n");
            return true;
        }

        switch (answer[2])
        {
            case 1:
                Write((TakenLine ?? "You have already taken what was here; there is nothing more for you.") + "\n");
                return true;
            case 2:
                Write((EmptyLine ?? "Someone has been here before you. Nothing is left.") + "\n");
                return true;
            case 3:
                Write("You look it over, but wizards take nothing from the world and leave no mark on it. Nothing is recorded.\n");
                return true;
            case 0:
                break;
            default:
                Write("Nothing comes of it.\n");
                return true;
        }

        who.Call("RestDisturbed", QueryVerb());
        var name = who.Call<string>("QueryName") ?? "Someone";
        TellRoom(room, $"{name} searches {(ShortDesc != null ? Plain(ShortDesc) : Name.ToLowerInvariant())}.\n", who);
        var gold = answer[0];
        if (gold > 0 && who.Call<bool>("AddGold", gold))
            Write((GoldLine ?? $"You take %gold% gold from the {Name.ToLowerInvariant()}.").Replace("%gold%", gold.ToString()) + "\n");
        if (answer[1] == 1 && ItemPath != null) GiveItem(who, room);
        // The register is already on disk; now the character is, so a driver killed from here on loses nothing.
        who.Call("SaveAfterFind");
        return true;
    }

    /// <summary>One item for this finder: into their inventory, or at their feet if they cannot carry it.</summary>
    private void GiveItem(ObjectRef who, ObjectRef room)
    {
        ObjectRef? item = null;
        try
        {
            item = CloneObject(ItemPath!);
            MoveObject(item, who);
            item = item.Call<ObjectRef>("Settle") ?? item;
        }
        catch (MudRuntimeException)
        {
            // Too heavy, or the file is gone: leave it at their feet if there is an item at all.
        }
        if (item == null || !item.IsAlive) return;
        var text = item.Call<string>("Short") ?? "something";
        var line = (ItemLine ?? "You also take %item%.").Replace("%item%", text);
        if (Environment(item) == who)
        {
            Write(line + "\n");
            return;
        }
        try { MoveObject(item, room); }
        catch (MudRuntimeException) { item.Call("Vanish"); return; }
        Write($"You cannot carry {text}; it falls at your feet.\n");
    }
}
