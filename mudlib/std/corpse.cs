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
/// A corpse that is empty vanishes two minutes after it became empty. A
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

    /// <summary>Write the contents of a player's corpse through to the lost-property store.</summary>
    private void Report(bool gone)
    {
        if (!playerCorpse) return;
        try
        {
            var store = LoadObject("/secure/lost_property");
            var paths = new List<string>();
            var coins = 0;
            foreach (var item in Contents())
            {
                var amount = item.Call<int>("QueryAmount");
                if (amount > 0) coins += amount; else paths.Add(item.ProgramPath);
            }
            if (gone || (paths.Count == 0 && coins == 0)) store.Call("Clear", deceasedName, corpseId);
            else store.Call("Record", deceasedName, corpseId, paths, coins);
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
        var shorts = Contents().Select(i => i.Call<string>("Short")).Where(s => s != null).ToList();
        text.Append(shorts.Count == 0 ? "It is empty.\n" : "It holds: " + string.Join(", ", shorts) + ".\n");
        if (LockedFor(ThisPlayer())) text.Append("It was felled moments ago; only those involved may touch it.\n");
        return text.ToString();
    }

    /// <summary>Take one thing out. Returns null on success, otherwise the refusal text.</summary>
    public string? Take(string itemId)
    {
        var taker = ThisPlayer();
        if (taker == null || PreviousObject() != taker) return "You cannot do that.";
        if (LockedFor(taker)) return Refusal();
        var item = Present(itemId, Self);
        if (item == null) return $"There is no '{Esc(itemId)}' in {Short()}.";
        Hand(item, taker);
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
        foreach (var item in items) Hand(item, taker);
        AfterTaking();
        return null;
    }

    private void Hand(ObjectRef item, ObjectRef taker)
    {
        var description = item.Call<string>("Short") ?? "something";
        MoveObject(item, taker);
        TellObject(taker, $"You take {description} from {Short()}.\n");
        var room = Environment();
        if (room != null) TellRoom(room, $"{taker.Call<string>("QueryName")} takes {description} from {Short()}.\n", taker);
        item.Call("Taken", taker);
    }

    private void AfterTaking()
    {
        Report(false);
        if (Contents().Count == 0) ArmEmpty();
    }

    // ---- Decay ----

    private void Arm()
    {
        if (Contents().Count == 0) { ArmEmpty(); return; }
        var generation = ++fullGeneration;
        CallOut(() => { if (generation == fullGeneration) RotAway(); }, fullSeconds);
    }

    private void ArmEmpty()
    {
        if (emptyArmed) return;
        emptyArmed = true;
        var generation = ++emptyGeneration;
        CallOut(() => { if (generation == emptyGeneration) { emptyArmed = false; if (Contents().Count == 0) RotAway(); } }, emptySeconds);
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
            foreach (var item in items) if (room != null) MoveObject(item, room);
            if (room != null) TellRoom(room, $"The corpse of {Strip(label)} rots away, leaving its belongings on the ground.\n");
        }
        Destruct();
    }

    public override string Stat() => base.Stat() + $"  Corpse of {label}: killer '{killerName}', deceased '{deceasedName}', lock {Math.Max(0, lockSeconds - (Time() - diedAt))}s left, holds {Contents().Count}\n";
}
