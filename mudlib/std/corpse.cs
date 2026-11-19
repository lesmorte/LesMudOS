// @inherit /std/thing

/// <summary>
/// What a creature or a player leaves behind. It holds the loot (or the
/// belongings of a dead player) and cannot be picked up. For the first
/// LockSeconds only the player who dealt the killing blow and the dead player
/// (recorded by name) may take things from it; after that anyone may.
///
/// Things leave a corpse only through Take() and TakeAll(), which check who is
/// asking. The ordinary "get" command only sees items lying directly in the room.
///
/// A corpse crumbles to dust at once when its last item or coin is taken, unless a skin or fresh blood can still be taken from
/// it (then it crumbles when that is taken). A corpse that was empty from the start and has nothing to take vanishes after
/// two minutes. A
/// monster's corpse that still holds loot rots away after an hour and its
/// contents with it; a player's corpse spills its contents on the floor.
/// </summary>
public class Corpse : Thing
{
    private int lockSeconds = 1800;
    private int emptySeconds = 120;
    private int fullSeconds = 3600;
    private long diedAt;
    private string killerName = "";
    private string deceasedName = "";
    private bool playerCorpse;
    private string label = "something";
    private bool ready;
    private int fullGeneration;
    private int emptyGeneration;
    private bool emptyArmed;
    private string corpseId = "";

    // What the creature was and what it yields to the crafting system (docs/CRAFTING.md): read from the dying creature in
    // Setup, only when the creature itself makes the call and its file lies in a trusted tree. Each part is taken once.
    private string creatureKind = "";
    private int creatureLevel;
    private string skinKind = "";
    private string bloodKind = "";
    private bool skinTaken;
    private bool bloodTaken;
    private long diedAtMs;
    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    public override bool CanGet() => false;

    public bool IsCorpse() => true;

    /// <summary>
    /// Called once by the creator, after it has put the contents inside.
    /// label is the dead creature's short description ("a goblin guard", "Keith");
    /// killer and deceased are the objects involved (only players are remembered, by name).
    /// </summary>
    public void Setup(string deadLabel, ObjectRef killer, ObjectRef deceased)
    {
        if (ready) return;
        ready = true;
        label = deadLabel;
        diedAt = Time();
        diedAtMs = TimeMs();
        ReadYields(deceased);
        killerName = NameOfPlayer(killer);
        deceasedName = NameOfPlayer(deceased);
        playerCorpse = deceasedName != "";
        corpseId = Time() + "-" + Self.Name;
        var plain = Strip(deadLabel).ToLowerInvariant();
        Name = "corpse of " + plain;
        Ids.Add("corpse");
        Ids.Add("corpse of " + plain);
        ShortDesc = "the corpse of " + deadLabel;
        LongDesc = $"The lifeless body of {deadLabel} lies here.";
        Arm();
        Report(false);
    }

    private static bool InTrustedTree(string path)
    {
        if (path.Contains("..")) return false;
        foreach (var tree in TrustedTrees) if (path.StartsWith(tree, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>
    /// Remember the creature's kind, level, skin and blood. Only the creature itself, dying, may describe itself (the caller of
    /// Setup must be the deceased), and only a creature from a trusted tree: a corpse set up by anything else, or a creature
    /// from a wizard's /wiz home, yields nothing. A creature declares its parts with public string properties Skin and Blood.
    /// </summary>
    private void ReadYields(ObjectRef deceased)
    {
        if (deceased == null || !deceased.IsAlive || PreviousObject() != deceased || !InTrustedTree(deceased.ProgramPath)) return;
        if (deceased.Call<bool>("IsPlayerCharacter")) return;
        var file = deceased.ProgramPath.Substring(deceased.ProgramPath.LastIndexOf('/') + 1);
        var chronicle = deceased.Call<string>("get_ChronicleKind") ?? "";
        creatureKind = chronicle != "" ? chronicle : file;
        creatureLevel = deceased.Call<int>("QueryLevel");
        skinKind = Clean(deceased.Call<string>("get_Skin"));
        bloodKind = Clean(deceased.Call<string>("get_Blood"));
    }

    private static string Clean(string? kind) => (kind ?? "").Trim().ToLowerInvariant();

    public string QueryCreatureKind() => creatureKind;

    public int QueryCreatureLevel() => creatureLevel;

    /// <summary>Milliseconds since the death.</summary>
    public long QueryAgeMs() => TimeMs() - diedAtMs;

    /// <summary>The kind of part this corpse yields for "skin" or "bleed" ("wolf"), or "" for none.</summary>
    public string QueryYieldKind(string verb) => verb == "skin" ? skinKind : verb == "bleed" ? bloodKind : "";

    /// <summary>Has the part for "skin" or "bleed" not been taken yet (the corpse declares one and nobody has tried)?</summary>
    public bool QueryYieldAvailable(string verb) => verb == "skin" ? skinKind != "" && !skinTaken : verb == "bleed" && bloodKind != "" && !bloodTaken;

    /// <summary>May this character skin or bleed the corpse now? The loot lock: during it, only the killer and the deceased.</summary>
    public bool MayHarvest(ObjectRef who) => !LockedFor(who);

    /// <summary>
    /// Use the one chance for "skin" or "bleed". Only the crafting daemon may call it; it then rolls for the part. True if the
    /// part was there to take.
    /// </summary>
    public bool TakeYield(string verb)
    {
        var caller = PreviousObject();
        if (caller == null || caller.IsClone || caller.ProgramPath != "/secure/crafting" || !QueryYieldAvailable(verb)) return false;
        if (verb == "skin") skinTaken = true; else bloodTaken = true;
        // The last thing to take from an already empty corpse: it crumbles at once. A moment's delay, not a call from inside the
        // crafting daemon's own action, which still has this corpse in hand.
        if (Contents().Count == 0 && !HasYields()) CallOut(() => { if (Contents().Count == 0 && !HasYields()) RotAway(); }, 1);
        return true;
    }

    /// <summary>A part still to take: a skin, or blood while it is fresh. A corpse with one does not crumble like an empty one.</summary>
    private bool HasYields() => (skinKind != "" && !skinTaken) || (bloodKind != "" && !bloodTaken && QueryAgeMs() < BleedFreshMs());

    private long BleedFreshMs()
    {
        try { return (long)(LoadObject("/secure/crafting").Call<Dictionary<string, List<double>>>("QueryNumbers")?.GetValueOrDefault("bleed.fresh_seconds")?.FirstOrDefault() ?? 300) * 1000; }
        catch (MudRuntimeException) { return 300000; }
    }

    /// <summary>Write the contents of a player's corpse through to the lost-property store.</summary>
    private void Report(bool gone)
    {
        if (!playerCorpse) return;
        try
        {
            var store = LoadObject("/secure/lost_property");
            var records = new List<string>(); // one save record per item: path, and its quality and quantity (see /std/thing RecordOf)
            var coins = 0;
            foreach (var item in Contents())
            {
                var amount = item.Call<int>("QueryAmount");
                if (amount > 0) coins += amount; else records.Add(RecordOf(item));
            }
            if (gone || (records.Count == 0 && coins == 0)) store.Call("Clear", deceasedName, corpseId);
            else store.Call("Record", deceasedName, corpseId, records, coins);
        }
        catch (MudRuntimeException)
        {
            // Without the store the corpse still works; only the restart protection is missing.
        }
    }

    private static string Strip(string text)
    {
        foreach (var article in new[] { "a ", "an ", "the " })
            if (text.StartsWith(article, StringComparison.OrdinalIgnoreCase)) return text.Substring(article.Length);
        return text;
    }

    private static string NameOfPlayer(ObjectRef? who)
    {
        if (who == null || !who.IsAlive || !who.Call<bool>("IsPlayerCharacter")) return "";
        return (who.Call<string>("QueryName") ?? "").ToLowerInvariant();
    }

    // ---- Locking ----

    private bool LockedFor(ObjectRef? who)
    {
        if (Time() - diedAt >= lockSeconds) return false;
        var name = NameOfPlayer(who);
        return name == "" || (name != killerName && name != deceasedName);
    }

    private string Refusal() => $"The corpse of {Strip(label)} is not yours to {{cmd}}loot{{/}} yet.";

    /// <summary>The timers, for tests and wizards: lock length, and decay times when empty and when full. Wizard level 10 only.</summary>
    public void SetTimers(int lockSecs, int emptySecs, int fullSecs)
    {
        var who = ThisPlayer();
        if (who == null || who.Call<int>("QueryWizLevel") < 10) return;
        lockSeconds = lockSecs;
        emptySeconds = emptySecs;
        fullSeconds = fullSecs;
        fullGeneration++;
        emptyGeneration++;
        emptyArmed = false;
        Arm();
    }

    // ---- Contents ----

    private List<ObjectRef> Contents() => AllInventory().Where(i => i.IsAlive).ToList();

    public override string Long()
    {
        var text = new System.Text.StringBuilder(LongDesc + "\n");
        var shorts = Collapse(Contents().Where(i => i.Call<string>("Short") != null)).Select(l => l.text).ToList();
        text.Append(shorts.Count == 0 ? "It is empty.\n" : "It holds: " + string.Join(", ", shorts) + ".\n");
        if (LockedFor(ThisPlayer())) text.Append("It was felled moments ago; only those involved may touch it.\n");
        if (QueryYieldAvailable("skin")) text.Append("Its hide could still be taken ({cmd}skin{/}).\n");
        if (QueryYieldAvailable("bleed") && QueryAgeMs() < BleedFreshMs()) text.Append("It is fresh enough to {cmd}bleed{/}.\n");
        return text.ToString();
    }

    /// <summary>
    /// Take one thing out, or an amount: "skin", "3 skins", "all skins". Returns null on success, otherwise the refusal
    /// text. Nothing is taken if the corpse holds fewer than asked.
    /// </summary>
    public string? Take(string itemId)
    {
        var taker = ThisPlayer();
        if (taker == null || PreviousObject() != taker) return "You cannot do that.";
        if (LockedFor(taker)) return Refusal();
        if (!ParseAmount(itemId, out var count, out var all, out var noun)) return $"There is no '{Esc(itemId)}' in {Short()}.";
        var found = !all && count == 1
            ? (Present(noun.ToLowerInvariant(), Self) is { } one ? new List<ObjectRef> { one } : new List<ObjectRef>())
            : Matching(Self, noun);
        if (found.Count == 0) return $"There is no '{Esc(noun)}' in {Short()}.";
        var held = found.Sum(QuantityOf);
        if (!all && held < count) return $"There {(held == 1 ? "is" : "are")} only {held} in {Short()}.";
        var left = all ? int.MaxValue : count;
        var plan = new List<(ObjectRef item, int n)>();
        foreach (var item in found)
        {
            if (left <= 0) break;
            var n = Math.Min(left, QuantityOf(item));
            plan.Add((item, n));
            left -= n;
        }
        Hand(plan, taker);
        AfterTaking();
        return null;
    }

    /// <summary>Take everything. Returns null on success, otherwise the refusal text.</summary>
    public string? TakeAll()
    {
        var taker = ThisPlayer();
        if (taker == null || PreviousObject() != taker) return "You cannot do that.";
        if (LockedFor(taker)) return Refusal();
        var items = Contents();
        if (items.Count == 0) return $"There is nothing in {Short()}.";
        Hand(items.Select(i => (i, QuantityOf(i))).ToList(), taker);
        AfterTaking();
        return null;
    }

    /// <summary>Move the planned amounts to the taker and say so once per kind of thing: "a rusty sword (x2)", not two lines.</summary>
    private void Hand(List<(ObjectRef item, int n)> plan, ObjectRef taker)
    {
        var order = new List<string>();
        var groups = new Dictionary<string, (string single, int n, List<ObjectRef> held)>();
        foreach (var (item, n) in plan)
        {
            var key = SameKey(item);
            var single = Single(item);
            var held = MovePortion(item, n, taker);
            if (held == null) continue;
            if (!groups.TryGetValue(key, out var g)) { g = (single, 0, new List<ObjectRef>()); order.Add(key); }
            g.n += n;
            g.held.Add(held);
            groups[key] = g;
        }
        var room = Environment();
        foreach (var key in order)
        {
            var g = groups[key];
            var description = ListName(g.single, g.n);
            TellObject(taker, $"You take {description} from {Short()}.\n");
            if (room != null) TellRoom(room, $"{taker.Call<string>("QueryName")} takes {description} from {Short()}.\n", taker);
            foreach (var held in g.held.Distinct()) held.Call("Taken", taker);
        }
    }

    private void AfterTaking()
    {
        Report(false);
        // Looting should leave nothing behind unless there is something still to extract: the last item or coin taken from a
        // corpse with no skin or blood left makes it crumble to dust at once.
        if (Contents().Count == 0 && !HasYields()) RotAway();
    }

    // ---- Decay ----

    private void Arm()
    {
        if (Contents().Count == 0 && !HasYields())
        {
            ArmEmpty();
            return;
        }
        if (bloodKind != "" && !bloodTaken)
        {
            // Blood to take: once it has gone cold, a corpse with nothing else left is as good as empty.
            var cold = Math.Max(1, (BleedFreshMs() - QueryAgeMs()) / 1000.0);
            CallOut(() => { if (Contents().Count == 0 && !HasYields()) ArmEmpty(); }, cold);
        }
        var generation = ++fullGeneration;
        CallOut(() => { if (generation == fullGeneration) RotAway(); }, fullSeconds);
    }

    private void ArmEmpty()
    {
        if (emptyArmed) return;
        emptyArmed = true;
        var generation = ++emptyGeneration;
        CallOut(() => { if (generation == emptyGeneration) { emptyArmed = false; if (Contents().Count == 0 && !HasYields()) RotAway(); } }, emptySeconds);
    }

    private void RotAway()
    {
        Report(true);
        var room = Environment();
        var items = Contents();
        if (items.Count == 0 || !playerCorpse)
        {
            foreach (var item in items) item.Call("Vanish");
            if (room != null) TellRoom(room, $"The corpse of {Strip(label)} crumbles to dust.\n");
        }
        else
        {
            foreach (var item in items) if (room != null) MovePortion(item, QuantityOf(item), room);
            if (room != null) TellRoom(room, $"The corpse of {Strip(label)} rots away, leaving its belongings on the ground.\n");
        }
        Destruct();
    }

    public override string Stat() => base.Stat() + $"  Corpse of {label}: killer '{killerName}', deceased '{deceasedName}', lock {Math.Max(0, lockSeconds - (Time() - diedAt))}s left, holds {Contents().Count}\n" +
        $"  Creature '{creatureKind}' level {creatureLevel}, skin '{skinKind}'{(skinTaken ? " (taken)" : "")}, blood '{bloodKind}'{(bloodTaken ? " (taken)" : "")}, age {QueryAgeMs() / 1000}s\n";
}
