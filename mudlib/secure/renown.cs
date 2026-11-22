/// <summary>
/// The Renown ledger: one record per character name, kept apart from the
/// character's own save file so that it survives the character's deletion.
/// Holds Renown, Infamy, earned titles and the chosen title, what each
/// character has already been credited for, and what they have discovered.
///
/// Only the event service may change a record. The one exception is a player
/// choosing which earned title to show (TitleCommand), which works on the
/// calling player's own record.
/// </summary>
public class RenownLedger : MudObject
{
    private const string SaveFile = "/secure/renown";
    private const string Service = "/secure/events";

    private static readonly string[] FrozenTierNames = { "Unknown", "Local", "Recognized", "Acclaimed", "Renowned", "Famous", "Heroic", "Legendary", "Mythic" };
    /// <summary>A copy: the table itself cannot be changed from outside (docs/SANDBOX.md).</summary>
    public static string[] TierNames => (string[])FrozenTierNames.Clone();
    private static readonly int[] FrozenTierFloor = { 0, 1, 100, 500, 1000, 2500, 5000, 10000, 25000 };
    /// <summary>A copy: the table itself cannot be changed from outside (docs/SANDBOX.md).</summary>
    public static int[] TierFloor => (int[])FrozenTierFloor.Clone();
    private static readonly string[] FrozenTierTitles = { "", "Local Hero", "Wanderer", "Champion", "Defender of Greenvale", "Voice of the People", "Hero of Hollowmere", "Living Legend", "The Mythic" };
    /// <summary>A copy: the table itself cannot be changed from outside (docs/SANDBOX.md).</summary>
    public static string[] TierTitles => (string[])FrozenTierTitles.Clone();

    /// <summary>What an NPC says first to a character of this tier, in the bible's spirit. Below Recognized: nothing.</summary>
    private static readonly string[] Recognition =
    {
        "", "", "You seem familiar.", "I have heard stories about your travels. Some sound like feats of Vigor.",
        "Travelers speak your name on the road, and they tell your deeds as feats of Might or Grace.", "Your name is known to most folk in these parts.",
        "They sing songs about the likes of you, and the best of them are about Spirit.", "I never thought I would meet you.",
        "Stand aside, everyone. It is you. The one from the stories."
    };

    [Saved] private Dictionary<string, int> renown = new();
    [Saved] private Dictionary<string, int> infamy = new();
    [Saved] private Dictionary<string, List<string>> titles = new();
    [Saved] private Dictionary<string, string> activeTitle = new();
    /// <summary>Name to the "event id|subject" keys the character has already been credited for.</summary>
    [Saved] private Dictionary<string, List<string>> done = new();
    /// <summary>Name to the "event id|subject" keys of what they have discovered.</summary>
    [Saved] private Dictionary<string, List<string>> discovered = new();

    private bool persistent = true;

    public override void Create()
    {
        if (Self.IsClone) return;
        try { RestoreObject(SaveFile); }
        catch (MudRuntimeException) { persistent = false; }
    }

    private void Save()
    {
        if (!persistent) return;
        try { SaveObject(SaveFile); }
        catch (MudRuntimeException) { persistent = false; }
    }

    public bool QueryPersistent() => persistent;

    private bool FromService()
    {
        var caller = PreviousObject();
        return !Self.IsClone && caller != null && !caller.IsClone && caller.ProgramPath == Service;
    }

    private static string Key(string name) => name.Trim().ToLowerInvariant();

    // ---- tiers ----

    public int TierIndex(int amount)
    {
        var tier = 0;
        for (var i = 0; i < TierFloor.Length; i++) if (amount >= TierFloor[i]) tier = i;
        return tier;
    }

    public string TierFor(int amount) => TierNames[TierIndex(amount)];

    public int QueryRenown(string name) => renown.GetValueOrDefault(Key(name));
    public int QueryInfamy(string name) => infamy.GetValueOrDefault(Key(name));
    public string QueryTier(string name) => TierFor(QueryRenown(name));
    public List<string> QueryTitles(string name) => new(titles.GetValueOrDefault(Key(name)) ?? new List<string>());
    public string QueryActiveTitle(string name) => activeTitle.GetValueOrDefault(Key(name)) ?? "";
    public bool HasDone(string name, string key) => (done.GetValueOrDefault(Key(name)) ?? new List<string>()).Contains(key);
    public List<string> QueryDiscovered(string name) => new(discovered.GetValueOrDefault(Key(name)) ?? new List<string>());

    /// <summary>"name|renown" for the most renowned characters, highest first.</summary>
    public List<string> TopRenown(int count)
        => renown.Where(r => r.Value > 0).OrderByDescending(r => r.Value).ThenBy(r => r.Key).Take(count).Select(r => r.Key + "|" + r.Value).ToList();

    public List<string> NamesAtOrAbove(int amount) => renown.Where(r => r.Value >= amount).Select(r => r.Key).OrderBy(n => n).ToList();

    // ---- writes (event service only) ----

    /// <summary>
    /// Add Renown. Returns what changed, in order: "tier:Recognized" for each
    /// tier newly reached and "title:Wanderer" for each title newly earned.
    /// Renown is never reduced. An empty list also means a refused caller.
    /// </summary>
    public List<string> Award(string name, int amount, string source)
    {
        var changes = new List<string>();
        if (!FromService() || amount <= 0) return changes;
        var key = Key(name);
        var before = TierIndex(renown.GetValueOrDefault(key));
        renown[key] = renown.GetValueOrDefault(key) + amount;
        var after = TierIndex(renown[key]);
        for (var tier = before + 1; tier <= after; tier++)
        {
            changes.Add("tier:" + TierNames[tier]);
            if (AddTitle(key, TierTitles[tier])) changes.Add("title:" + TierTitles[tier]);
        }
        Save();
        return changes;
    }

    public void AddInfamy(string name, int amount)
    {
        if (!FromService() || amount <= 0) return;
        infamy[Key(name)] = infamy.GetValueOrDefault(Key(name)) + amount;
        Save();
    }

    private bool AddTitle(string key, string title)
    {
        if (title == "") return false;
        if (!titles.TryGetValue(key, out var list)) titles[key] = list = new List<string>();
        if (list.Contains(title)) return false;
        list.Add(title);
        return true;
    }

    /// <summary>Grant a title outside the tier ladder (Pathfinder). True if it is new.</summary>
    public bool GrantTitle(string name, string title)
    {
        if (!FromService()) return false;
        var added = AddTitle(Key(name), title);
        if (added) Save();
        return added;
    }

    /// <summary>Mark that a character has been credited for a key. True if this is the first time.</summary>
    public bool MarkDone(string name, string key)
    {
        if (!FromService()) return false;
        var k = Key(name);
        if (!done.TryGetValue(k, out var list)) done[k] = list = new List<string>();
        if (list.Contains(key)) return false;
        list.Add(key);
        Save();
        return true;
    }

    public void AddDiscovered(string name, string key)
    {
        if (!FromService()) return;
        var k = Key(name);
        if (!discovered.TryGetValue(k, out var list)) discovered[k] = list = new List<string>();
        if (list.Contains(key)) return;
        list.Add(key);
        Save();
    }

    // ---- text ----

    /// <summary>One line for `score`: the tier in words.</summary>
    public string TierLine(string name) => "Renown: " + QueryTier(name) + "\n";

    /// <summary>What the `renown` command shows: the exact number, tier, next tier, Infamy if any, titles.</summary>
    public string RenownText(string name)
    {
        var amount = QueryRenown(name);
        var tier = TierIndex(amount);
        var sb = new StringBuilder();
        sb.Append($"Renown: {amount} ({TierNames[tier]})\n");
        if (tier + 1 < TierNames.Length)
            sb.Append($"Next tier: {TierNames[tier + 1]} at {TierFloor[tier + 1]} ({TierFloor[tier + 1] - amount} to go)\n");
        else
            sb.Append("You have reached the highest tier.\n");
        if (QueryInfamy(name) > 0) sb.Append($"Infamy: {QueryInfamy(name)}\n");
        var earned = QueryTitles(name);
        if (earned.Count > 0) sb.Append("Titles earned: " + string.Join(", ", earned) + "\n");
        return sb.ToString();
    }

    /// <summary>The first words an NPC says to this character; empty below Recognized.</summary>
    public string RecognitionLine(string name) => Recognition[TierIndex(QueryRenown(name))];

    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    /// <summary>
    /// The `title` command for the calling player: no argument lists the titles
    /// earned, `none` shows no title, a title name chooses it. The player is
    /// the calling object, never an argument.
    /// </summary>
    public string TitleCommand(string args)
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || !caller.IsClone || caller.ProgramPath != "/std/player" || caller != ThisPlayer())
            return "Only a player can choose a title.\n";
        return Choose(caller.Call<string>("QueryName") ?? "", args);
    }

    /// <summary>
    /// The same as TitleCommand for a named character, for trusted code that
    /// acts for a player (a class trainer, a guild NPC). Objects outside the
    /// trusted trees are refused. It can only choose among titles already earned.
    /// </summary>
    public string ChooseTitle(string name, string args)
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller.Domain != "" || caller.ProgramPath.Contains("..") || !TrustedTrees.Any(t => caller.ProgramPath.StartsWith(t, StringComparison.Ordinal)))
            return "Only trusted code can choose a title for someone.\n";
        return Choose(name, args);
    }

    private string Choose(string name, string args)
    {
        var key = Key(name);
        var earned = QueryTitles(key);
        args = (args ?? "").Trim();
        if (args == "")
        {
            if (earned.Count == 0) return "You have not earned any titles yet.\n";
            var shown = QueryActiveTitle(key);
            return "Titles you have earned: " + string.Join(", ", earned) + "\n"
                   + (shown == "" ? "You are using no title.\n" : "You are using: " + shown + "\n")
                   + "Use 'title <name>' to choose one, or 'title none'.\n";
        }
        if (args.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            activeTitle.Remove(key);
            Save();
            return "You will use no title.\n";
        }
        var match = earned.FirstOrDefault(t => t.Equals(args, StringComparison.OrdinalIgnoreCase));
        if (match == null) return "You have not earned a title called '" + args + "'.\n";
        activeTitle[key] = match;
        Save();
        return "You will be known as " + match + ".\n";
    }
}
