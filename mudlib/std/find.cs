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

    /// <summary>The room's listing line (a whole sentence) for a character who has already taken it. Default: "&lt;The short description&gt; lies here; you have taken what it held."</summary>
    protected string? TakenHere;
    /// <summary>The room's listing line (a whole sentence) for a character who finds it empty. Default: "&lt;The short description&gt; lies here, picked clean."</summary>
    protected string? EmptyHere;

    /// <summary>What the finder is told when gold is paid; "%gold%" is the amount. Default: "You take %gold% gold from the &lt;name&gt;."</summary>
    protected string? GoldLine;
    /// <summary>What the finder is told when an item is given; "%item%" is its short description. Default: "You also take %item%."</summary>
    protected string? ItemLine;
    /// <summary>What the finder is told when they try again. Default: one plain line.</summary>
    protected string? TakenLine;
    /// <summary>What a later finder is told when nothing is left. Default: someone has been here before them.</summary>
    protected string? EmptyLine;

    /// <summary>
    /// 0 (the default): the find is in plain view, as every find is. A number from 6 to 25 (Room.SearchEasy, SearchModerate, SearchHard, SearchVeryHard) hides it from every
    /// character until they have found it by a search in its room that beats this difficulty (docs/specs/search.md section 5.2): until then it is not listed, and "look at" and
    /// "get" say they see no such thing. A character the register already lists as having taken it counts as having found it. Wizards see it, marked "(hidden)".
    /// </summary>
    protected int SearchDifficulty;
    /// <summary>The words `search NAME` takes for this find (comma-separated, as in AddDetail; each is also a detail of its room).</summary>
    protected string SearchNames = "";
    /// <summary>What the finder reads when the search finds it.</summary>
    protected string? FoundLine;
    /// <summary>Found by touch or smell: the dark does not hide it (see Room.SearchModifier).</summary>
    protected bool ByFeel;

    private const string Register = "/secure/finds";

    // The answers of the register that matter for a description (see /secure/finds).
    private const int Open = 0, WasTaken = 1, WasEmpty = 2;

    public override bool CanGet() => false;

    // ---- Hidden finds ----

    public int QuerySearchDifficulty() => SearchDifficulty;

    public string QuerySearchNames() => SearchNames;

    /// <summary>The key of this find in a character's record of what they have found: "#" and the program path (the register's key). Empty for a find in plain view.</summary>
    public string QuerySearchKey() => SearchDifficulty > 0 ? "#" + Self.ProgramPath : "";

    public string QueryFoundLine() => FoundLine ?? "";

    public bool QueryByFeel() => ByFeel;

    /// <summary>Is this find still hidden from this character? Only for a hidden find, a player character who is not a wizard, who has not found it and whom the register does not list as having taken it.</summary>
    public bool QueryHiddenFrom(ObjectRef? who)
    {
        if (SearchDifficulty <= 0 || who == null || !who.IsAlive || !who.Call<bool>("IsPlayerCharacter") || who.Call<int>("QueryWizLevel") >= 1) return false;
        var room = Environment();
        if (room == null || who.Call<bool>("QueryFound", room.Name, QuerySearchKey())) return false;
        return StatusFor(who) != WasTaken;
    }

    private bool WizardSeesHidden(ObjectRef? who)
        => SearchDifficulty > 0 && who != null && who.IsAlive && who.Call<bool>("IsPlayerCharacter") && who.Call<int>("QueryWizLevel") >= 1
           && Environment() is { } room && !who.Call<bool>("QueryFound", room.Name, QuerySearchKey());

    public override bool Id(string id) => base.Id(id) && !QueryHiddenFrom(ThisPlayer());

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
        if (QueryHiddenFrom(ThisPlayer())) return null;
        if (WizardSeesHidden(ThisPlayer())) full += " (hidden)";
        return StatusFor(ThisPlayer()) switch
        {
            WasTaken => TakenShort ?? full + " (taken by you)",
            WasEmpty => EmptyShort ?? full + " (picked clean)",
            _ => full,
        };
    }

    /// <summary>
    /// The room's listing of this find for the looker, as a sentence, once they have taken from it or found it empty; "" while it is still
    /// there to be taken (then the room lists it like any object: "A purse is here.").
    /// </summary>
    public string QueryHereLine()
    {
        if (QueryHiddenFrom(ThisPlayer())) return "";
        var full = ShortDesc ?? Name;
        var cap = full.Length == 0 ? full : char.ToUpper(full[0]) + full.Substring(1);
        return StatusFor(ThisPlayer()) switch
        {
            WasTaken => TakenHere ?? cap + " lies here; you have taken what it held.",
            WasEmpty => EmptyHere ?? cap + " lies here, picked clean.",
            _ => "",
        };
    }

    /// <summary>True while the looker may still take from this find: "get all" skips finds (each is searched once, by name) and says so when this is true.</summary>
    public bool QueryTakeByName() => !QueryHiddenFrom(ThisPlayer()) && StatusFor(ThisPlayer()) == Open;

    public override string Long()
    {
        if (QueryHiddenFrom(ThisPlayer())) return "You see nothing like that.\n"; // never reached by a look (Id says no), but never a description either
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
               $"; {finders} finder(s) so far\n" +
               (SearchDifficulty > 0 ? $"  Hidden: difficulty {SearchDifficulty}, names '{SearchNames}'{(ByFeel ? ", by feel" : "")}\n" : "");
    }

    // ---- The words ----

    public override void Init()
    {
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        AddAction("get", TryTake);
        AddAction("take", TryTake);
        AddAction("loot", TryTake);
        foreach (var verb in new[] { "open", "unwrap", "untie", "unroll", "search", "grab", "pick" }) AddAction(verb, TryTake); // the other obvious words
    }

    /// <summary>
    /// "get purse", "take purse", "loot purse", and open, unwrap, untie, unroll, search, grab, pick up. Handled only when the words name this find and it is the first thing in
    /// the room that answers to them; everything else goes on to the ordinary commands.
    /// </summary>
    private bool TryTake(string args)
    {
        var who = ThisPlayer();
        var room = Environment();
        var word = args.Trim().ToLowerInvariant();
        if (QueryVerb() == "pick" && word.StartsWith("up ")) word = word.Substring(3).Trim(); // "pick up bundle"
        if (who == null || room == null || word == "" || Environment(who) != room || QueryHiddenFrom(who) || !Id(word) || Present(word, room) != Self) return false;

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
        who.Call("Disturbed", QueryVerb()); // opening a container is loud for a sneaking Scout
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
