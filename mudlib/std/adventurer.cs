// @inherit /std/living

/// <summary>
/// Everything about playing the game: the character sheet, gameplay commands,
/// death and respawn, quests and the saved character. /std/player inherits this
/// and adds accounts, wizard tools and files.
/// </summary>
public abstract class Adventurer : Living
{
    private const string RespawnRoom = "/domains/hollow/village/guest_rooms";
    private const int MaxLevel = 5;
    private const int BaseHp = 30;
    private const int HpPerLevel = 8;
    private const int RegenBeats = 5; // one regeneration step per 10 seconds
    private const int LevelGoto = 1;
    private const int LevelStat = 1;
    private const int LevelHeal = 5;
    private const int LevelSpeed = 5;
    private const int LevelSmite = 10;

    // Experience needed to reach level n is XpForLevel[n].
    private static readonly int[] XpForLevel = { 0, 0, 100, 250, 500, 900 };

    [Saved] private int xp;
    [Saved] private Dictionary<string, int> quests = new();
    [Saved] private Dictionary<string, string> questTitles = new();
    [Saved] private List<string> killed = new();
    [Saved] private List<string> carried = new();
    [Saved] private List<int> carriedEquipped = new();
    [Saved] private string logoutRoom = "";

    private int regenBeats;
    private bool dying;
    private bool inventoryRestored;

    public override void Create()
    {
        Level = 1;
        DamageMin = 2;
        DamageMax = 5;
        Gold = 25;
    }

    public override int QueryMaxHp() => BaseHp + HpPerLevel * (Level - 1) + EquipmentHpBonus();

    public override bool IsPlayerCharacter() => true;

    public override string QueryLabel() => Name;

    /// <summary>Wizard level; /std/player overrides this with the registry's answer.</summary>
    public virtual int QueryWizLevel() => 0;

    protected bool IsWizard(int level) => QueryWizLevel() >= level;

    /// <summary>Turn a typed path into an absolute one; /std/player knows the wizard's current directory.</summary>
    protected virtual string ResolvePath(string path) => path;

    // ---- Public API for other objects ----

    public int QueryXp() => xp;

    /// <summary>Add gold, or take it with a negative amount. Refuses (false) to go below zero.</summary>
    public bool AddGold(int amount)
    {
        if (Gold + amount < 0) return false;
        Gold += amount;
        return true;
    }

    public void AddXp(int amount)
    {
        if (amount <= 0) return;
        xp += amount;
        Tell($"You gain {amount} experience.\n");
        while (Level < MaxLevel && xp >= XpForLevel[Level + 1])
        {
            Level++;
            Hp = QueryMaxHp();
            Tell($"You have reached level {Level}! You feel stronger and fully healed. Maximum HP is now {QueryMaxHp()}.\n");
        }
    }

    public int QueryQuest(string id) => quests.TryGetValue(id, out var state) ? state : 0;

    public void SetQuest(string id, int state) => quests[id] = state;

    /// <summary>Set a quest's state and the title the quest log shows for it ("Clear the Cellar").</summary>
    public void SetQuest(string id, int state, string title)
    {
        quests[id] = state;
        questTitles[id] = title;
    }

    public void NotifyKill(string programPath)
    {
        if (!killed.Contains(programPath)) killed.Add(programPath);
    }

    public bool QueryKilled(string programPath) => killed.Contains(programPath);

    // ---- Saving ----

    /// <summary>Record the carried items and the logout room just before the player is saved.</summary>
    protected void PrepareSave()
    {
        if (!inventoryRestored) return;
        carried.Clear();
        carriedEquipped.Clear();
        foreach (var item in AllInventory())
        {
            carried.Add(item.ProgramPath);
            if (IsEquipped(item)) carriedEquipped.Add(carried.Count - 1);
        }
        var room = Environment();
        logoutRoom = room != null && room.Call<bool>("QueryLogoutRoom") ? room.Name : "";
    }

    /// <summary>Remove the carried items from the game, as the player leaves. They return at the next login.</summary>
    protected void ReleaseInventory()
    {
        foreach (var item in AllInventory()) item.Call("Vanish");
    }

    /// <summary>Bind the gameplay commands, start the heartbeat and bring the carried items back.</summary>
    protected void BeginAdventure()
    {
        AddAction("look", CmdLook);
        AddAction("l", CmdLook);
        AddAction("get", CmdGet);
        AddAction("take", CmdGet);
        AddAction("loot", CmdLoot);
        AddAction("drop", CmdDrop);
        AddAction("inventory", CmdInventory);
        AddAction("i", CmdInventory);
        AddAction("kill", CmdKill);
        AddAction("attack", CmdKill);
        AddAction("flee", CmdFlee);
        AddAction("stop", CmdStop);
        AddAction("consider", CmdConsider);
        AddAction("wield", CmdWield);
        AddAction("unwield", CmdUnwield);
        AddAction("wear", CmdWear);
        AddAction("remove", CmdRemove);
        AddAction("eat", CmdUse);
        AddAction("drink", CmdUse);
        AddAction("quaff", CmdUse);
        AddAction("talk", CmdTalk);
        AddAction("greet", CmdTalk);
        AddAction("ask", CmdAsk);
        AddAction("read", CmdRead);
        AddAction("score", CmdScore);
        AddAction("sc", CmdScore);
        AddAction("hp", CmdHp);
        AddAction("quests", CmdQuests);
        // Builder tools. Each handler checks the wizard level on every use and
        // declines below it, so to everyone else these verbs do not exist.
        AddAction("goto", CmdGoto);
        AddAction("stat", CmdStat);
        AddAction("heal", CmdHeal);
        AddAction("smite", CmdSmite);
        AddAction("speed", CmdSpeed);

        SetHeartBeat(true);

        for (var i = 0; i < carried.Count; i++)
        {
            try
            {
                var item = CloneObject(carried[i]);
                MoveObject(item, Self);
                if (carriedEquipped.Contains(i)) Equip(item);
            }
            catch (MudRuntimeException)
            {
                // The file is gone or no longer compiles; the item is lost.
            }
        }
        inventoryRestored = true;
    }

    /// <summary>
    /// Belongings that were in this player's corpse when the game last stopped are
    /// handed back by the innkeeper. Call once at login, after the inventory is restored.
    /// </summary>
    protected void ClaimLostProperty()
    {
        List<string> claim;
        try { claim = LoadObject("/secure/vault").Call<List<string>>("Claim") ?? new List<string>(); }
        catch (MudRuntimeException) { return; }
        if (claim.Count == 0) return;
        var coins = int.TryParse(claim[0], out var parsed) ? parsed : 0;
        var returned = 0;
        for (var i = 1; i < claim.Count; i++)
        {
            try
            {
                MoveObject(CloneObject(claim[i]), Self);
                returned++;
            }
            catch (MudRuntimeException)
            {
                // The file is gone or no longer compiles; the item is lost.
            }
        }
        if (coins > 0) Gold += coins;
        if (returned == 0 && coins == 0) return;
        Tell("Harland, the innkeeper of the Hollowman's Rest, hands you a bundle: he kept your belongings safe while the village was in turmoil.\n");
        if (returned > 0) Tell($"You recover {(returned == 1 ? "an item" : returned + " items")}{(coins > 0 ? $" and {coins} gold" : "")}.\n");
        else Tell($"You recover {coins} gold.\n");
        PersistNow();
    }

    /// <summary>Move the player into the world: the saved logout room, if there is one, else the start room.</summary>
    protected void EnterWorld(string startRoom)
    {
        var destination = logoutRoom != "" ? logoutRoom : startRoom;
        logoutRoom = "";
        try
        {
            MoveObject(LoadObject(destination));
        }
        catch (MudRuntimeException) when (destination != startRoom)
        {
            MoveObject(LoadObject(startRoom));
        }
    }

    public void Look()
    {
        var room = Environment();
        Tell(room == null ? "You are nowhere.\n" : room.Call<string>("LongFor", Self) ?? "You see nothing.\n");
    }

    protected override bool NeedsHeartBeat() => true;

    protected override void Tick()
    {
        if (++regenBeats < RegenBeats) return;
        regenBeats = 0;
        if (QueryInCombat() || Hp >= QueryMaxHp()) return;
        var room = Environment();
        Heal(room != null && room.Call<bool>("QuerySafe") ? 3 : 1);
    }

    // ---- Death ----

    /// <summary>
    /// A player does not stay dead: a corpse in the room where they fell takes their
    /// belongings and a tenth of their gold, they forget
    /// their enemies, and wake in the Guest Rooms at full health.
    /// </summary>
    public override void Die(ObjectRef? killer)
    {
        if (dying) return;
        dying = true;
        try
        {
            Tell("You have died!\n");
            var room = Environment();
            if (room != null) TellRoom(room, $"{Name} collapses and dies.\n", Self);
            var lost = Gold / 10;
            Gold -= lost;
            LeaveCorpse(room, killer, lost);
            ClearEnemies();
            ResetCombat(); // a pending attack or potion must not fire after the death
            Hp = QueryMaxHp();
            MoveObject(LoadObject(RespawnRoom));
            TellRoom(Environment()!, $"{Name} appears, looking battered.\n", Self);
            Tell($"You wake in the Guest Rooms, bruised but alive. The healer's fee costs you {lost} gold.\n");
            PersistNow();
            Look();
        }
        finally
        {
            dying = false;
        }
    }

    /// <summary>Write the character to disk now. Players override this.</summary>
    protected virtual void PersistNow() { }

    /// <summary>Put everything carried (equipment included) and the fee into a corpse in the room.</summary>
    private void LeaveCorpse(ObjectRef? room, ObjectRef? killer, int fee)
    {
        if (room == null) return;
        var corpse = CloneObject("/std/corpse");
        MoveObject(corpse, room);
        foreach (var slot in new[] { "weapon", "shield", "body", "head", "trinket" }) Unequip(slot);
        foreach (var item in AllInventory()) MoveObject(item, corpse);
        if (fee > 0)
        {
            var coins = CloneObject("/std/coins");
            coins.Call("SetAmount", fee);
            MoveObject(coins, corpse);
        }
        corpse.Call("Setup", Name, killer ?? Self, Self);
    }

    // ---- Commands: looking and carrying ----

    private bool CmdLook(string args)
    {
        if (args.StartsWith("at ")) args = args.Substring(3);
        else if (args.StartsWith("in ")) args = args.Substring(3);
        if (args == "") { Look(); return true; }
        var room = Environment();
        var target = Present(args) ?? (room == null ? null : Present(args, room));
        if (target != null)
        {
            Write(target.Call<string>("Long") ?? "You see nothing special.\n");
            return true;
        }
        var detail = room?.Call<string>("QueryDetail", args);
        if (detail == null) { NotifyFail($"You see no '{Esc(args)}' here."); return false; }
        Write(detail.EndsWith("\n") ? detail : detail + "\n");
        return true;
    }

    private bool CmdGet(string args)
    {
        var room = Environment();
        var from = args.IndexOf(" from ", StringComparison.Ordinal);
        if (from > 0) return GetFromCorpse(args.Substring(0, from).Trim(), args.Substring(from + 6).Trim());
        var item = room == null ? null : Present(args, room);
        if (item == null || item == Self) { NotifyFail("Get what?"); return false; }
        if (!item.Call<bool>("CanGet")) { Write("You can't take that.\n"); return true; }
        var description = item.Call<string>("Short");
        MoveObject(item, Self);
        Write($"You take {description}.\n");
        Say($"{Name} takes {description}.\n");
        item.Call("Taken", Self);
        return true;
    }

    private ObjectRef? CorpseHere(string name)
    {
        var room = Environment();
        if (room == null) return null;
        foreach (var ob in AllInventory(room))
            if (ob.Call<bool>("IsCorpse") && (name == "" || ob.Call<bool>("Id", name.ToLowerInvariant()))) return ob;
        return null;
    }

    private bool GetFromCorpse(string what, string corpseName)
    {
        var corpse = CorpseHere(corpseName);
        if (corpse == null || what == "") { NotifyFail("Get what from what?"); return false; }
        var refusal = what == "all" ? corpse.Call<string>("TakeAll") : corpse.Call<string>("Take", what);
        if (refusal != null) Write(refusal + "\n");
        return true;
    }

    private bool CmdLoot(string args)
    {
        var room = Environment();
        if (room == null) return false;
        string? refusal = null;
        var found = false;
        foreach (var ob in AllInventory(room).ToList())
        {
            if (!ob.Call<bool>("IsCorpse")) continue;
            if (args != "" && !ob.Call<bool>("Id", args.ToLowerInvariant())) continue;
            found = true;
            var result = ob.Call<string>("TakeAll");
            if (result == null) return true;
            refusal ??= result;
        }
        if (!found) { NotifyFail("There is nothing here to {cmd}loot{/}."); return false; }
        Write(refusal + "\n");
        return true;
    }

    private bool CmdDrop(string args)
    {
        var room = Environment();
        var item = Present(args);
        if (item == null || room == null) { NotifyFail("Drop what?"); return false; }
        var description = item.Call<string>("Short");
        MoveObject(item, room);
        Write($"You drop {description}.\n");
        Say($"{Name} drops {description}.\n");
        return true;
    }

    private bool CmdInventory(string args)
    {
        var items = AllInventory()
            .Where(i => i.Call<string>("Short") != null)
            .Select(i => i.Call<string>("Short") + (IsEquipped(i) ? (i.Call<string>("QuerySlot") == "weapon" ? " (wielded)" : " (worn)") : ""))
            .ToList();
        Write(items.Count == 0 ? "You are empty-handed.\n" : "You are carrying: " + string.Join(", ", items) + ".\n");
        return true;
    }

    private bool CmdRead(string args)
    {
        if (args == "") { NotifyFail("Read what?"); return false; }
        var room = Environment();
        var thing = Present(args) ?? (room == null ? null : Present(args, room));
        var text = thing?.Call<string>("QueryReadText") ?? room?.Call<string>("QueryReadable", args);
        if (text == null)
        {
            if (thing != null) { Write("There is nothing to read on that.\n"); return true; }
            NotifyFail($"You see no '{Esc(args)}' to read.");
            return false;
        }
        Write(text.EndsWith("\n") ? text : text + "\n");
        return true;
    }

    // ---- Commands: fighting ----

    private bool CmdKill(string args)
    {
        var room = Environment();
        var target = args == "" || room == null ? null : Present(args, room);
        if (target == null) { NotifyFail("Kill what?"); return false; }
        if (QueryFightingWith(target) && target.IsAlive)
        {
            // Already an enemy: "kill" picks it as the one to attack next.
            var first = Enemies().FirstOrDefault(e => Environment(e) == room);
            if (first == target) { Write("You are already fighting that.\n"); return true; }
            Focus(target);
            Write($"You turn on {target.Call<string>("QueryLabel")}.\n");
            return true;
        }
        var refusal = CanFight(target);
        if (refusal != null) { Write(refusal + "\n"); return true; }
        StartFight(target);
        Focus(target);
        return true;
    }

    private bool CmdFlee(string args)
    {
        var room = Environment();
        if (room == null || !QueryInCombat()) { Write("You are not fighting anyone.\n"); return true; }
        if (Random(4) == 0) { Write("You try to flee, but cannot get away!\n"); return true; }
        var exits = (room.Call<List<string>>("QueryExits") ?? new List<string>()).Where(d => room.Call<bool>("CanUseExit", Self, d)).ToList();
        if (exits.Count > 0)
        {
            var direction = exits[Random(exits.Count)];
            Write($"You flee {direction}!\n");
            room.Call("MoveThrough", Self, direction);
            return true;
        }
        Write("There is no way out!\n");
        return true;
    }

    private bool CmdConsider(string args)
    {
        var room = Environment();
        var target = args == "" || room == null ? null : Present(args, room);
        if (target == null) { NotifyFail("Consider whom?"); return false; }
        if (!target.Call<bool>("IsCreature")) { Write("You can't fight that.\n"); return true; }
        var label = Cap(target.Call<string>("QueryLabel") ?? "It");
        var difference = target.Call<int>("QueryLevel") - Level;
        var verdict = difference <= -2 ? "looks like an easy fight"
            : difference == -1 ? "looks a little weaker than you"
            : difference == 0 ? "looks like an even match"
            : difference == 1 ? "looks tougher than you"
            : "would crush you";
        Write($"{label} {verdict}.\n" + (target.Call<bool>("QueryPeaceful") ? "It will not fight you.\n" : SpeedVerdict(target)));
        return true;
    }

    /// <summary>How often the target acts compared with this player's own basic attack.</summary>
    private string SpeedVerdict(ObjectRef target)
    {
        var theirs = target.Call<double>("QueryTypicalDelay");
        var mine = QueryTypicalDelay();
        if (theirs <= 0 || mine <= 0) return "";
        var ratio = theirs / mine;
        var line = ratio <= 0.6 ? "It strikes much more often than you can."
            : ratio < 0.9 ? "It strikes more often than you."
            : ratio <= 1.1 ? "It strikes about as often as you."
            : ratio <= 1.7 ? "It strikes less often than you."
            : "It strikes much less often than you can.";
        return line + "\n" + (target.Call<bool>("QueryVariedAttacks") ? "It does not always attack the same way.\n" : "");
    }

    // ---- Commands: equipment and consumables ----

    private bool CmdWield(string args)
    {
        var item = args == "" ? null : Present(args);
        if (item == null) { NotifyFail("Wield what?"); return false; }
        if (item.Call<string>("QuerySlot") != "weapon") { Write("You can't wield that.\n"); return true; }
        return PutOn(item, "You wield", "wields");
    }

    private bool CmdWear(string args)
    {
        var item = args == "" ? null : Present(args);
        if (item == null) { NotifyFail("Wear what?"); return false; }
        var slot = item.Call<string>("QuerySlot");
        if (string.IsNullOrEmpty(slot) || slot == "weapon") { Write("You can't wear that.\n"); return true; }
        return PutOn(item, "You put on", "puts on");
    }

    private bool PutOn(ObjectRef item, string you, string they)
    {
        var slot = item.Call<string>("QuerySlot")!;
        var description = item.Call<string>("Short");
        if (IsEquipped(item)) { Write("You already have that on.\n"); return true; }
        var old = QueryEquipped(slot);
        if (old != null)
        {
            Unequip(slot);
            Write($"You stop using {old.Call<string>("Short")}.\n");
        }
        Equip(item);
        Write($"{you} {description}.\n");
        Say($"{Name} {they} {description}.\n");
        return true;
    }

    private bool CmdUnwield(string args)
    {
        var weapon = QueryEquipped("weapon");
        if (weapon == null) { Write("You are not wielding anything.\n"); return true; }
        Unequip("weapon");
        Write($"You stop wielding {weapon.Call<string>("Short")}.\n");
        return true;
    }

    private bool CmdRemove(string args)
    {
        var item = args == "" ? null : Present(args);
        if (item == null) { NotifyFail("Remove what?"); return false; }
        var slot = item.Call<string>("QuerySlot");
        if (string.IsNullOrEmpty(slot) || !IsEquipped(item)) { Write("You are not wearing or wielding that.\n"); return true; }
        Unequip(slot);
        Write($"You {(slot == "weapon" ? "stop wielding" : "take off")} {item.Call<string>("Short")}.\n");
        return true;
    }

    private bool CmdUse(string args)
    {
        var verb = QueryVerb();
        var item = args == "" ? null : Present(args);
        if (item == null) { NotifyFail($"{Cap(verb)} what?"); return false; }
        var kind = item.Call<string>("QueryUseVerb");
        if (kind == null || (kind == "eat") != (verb == "eat")) { Write($"You can't {verb} that.\n"); return true; }
        UseAsAction(item); // an action with its own Recovery: now if ready, else queued for the next Ready Time
        return true;
    }

    private bool CmdStop(string args)
    {
        Write(ClearQueued() ? "You drop what you were about to do.\n" : "You have nothing planned.\n");
        return true;
    }

    // ---- Commands: talking ----

    private bool CmdTalk(string args)
    {
        if (args.StartsWith("to ")) args = args.Substring(3);
        var room = Environment();
        var npc = args == "" || room == null ? null : Present(args, room);
        if (npc == null) { NotifyFail("Talk to whom?"); return false; }
        var line = npc.Call<string>("Talk", Self);
        Write(line == null ? $"{Cap(npc.Call<string>("QueryLabel") ?? "It")} does not answer.\n" : line + "\n");
        return true;
    }

    private bool CmdAsk(string args)
    {
        var split = args.IndexOf(" about ", StringComparison.Ordinal);
        if (split < 0) { NotifyFail("Usage: {cmd}ask{/} NPC {cmd}about{/} TOPIC"); return false; }
        var room = Environment();
        var npc = room == null ? null : Present(args.Substring(0, split).Trim(), room);
        if (npc == null) { NotifyFail("Ask whom?"); return false; }
        var reply = npc.Call<string>("Ask", Self, args.Substring(split + 7).Trim());
        Write(reply == null ? $"{Cap(npc.Call<string>("QueryLabel") ?? "It")} does not answer.\n" : reply + "\n");
        return true;
    }

    // ---- Commands: the character ----

    private bool CmdScore(string args)
    {
        var text = new StringBuilder();
        text.Append($"{Short()}\n");
        text.Append($"Level {Level}   Experience {xp}");
        text.Append(Level >= MaxLevel ? " (highest level)\n" : $" ({XpForLevel[Level + 1] - xp} more for level {Level + 1})\n");
        text.Append($"HP {Hp}/{QueryMaxHp()}   Gold {Gold}\n");
        text.Append($"Damage {QueryDamageMin()}-{QueryDamageMax()}   To hit +{Level + QueryToHit()}   Armor {QueryArmor()}\n");
        var gear = new List<string>();
        foreach (var slot in new[] { "weapon", "shield", "body", "head", "trinket" })
            if (QueryEquipped(slot) is { } item) gear.Add($"{slot}: {item.Call<string>("Short")}");
        text.Append(gear.Count == 0 ? "You have nothing equipped.\n" : "Equipment: " + string.Join(", ", gear) + "\n");
        text.Append(ReactionReport());
        text.Append(QuestLog());
        Write(text.ToString());
        return true;
    }

    /// <summary>Reaction Time with every modifier and its source, and how long the basic attack takes.</summary>
    private string ReactionReport()
    {
        var parts = new List<string> { $"base {Num(QueryBaseReaction())}" };
        foreach (var item in QueryEquipment())
        {
            var mod = item.Call<double>("QueryReactionMod");
            if (mod != 0) parts.Add($"{item.Call<string>("Short")} {(mod > 0 ? "+" : "")}{Num(mod)}");
        }
        var speed = QuerySpeedFactor();
        if (Math.Abs(speed - 1.0) > 0.001) parts.Add($"speed x{Num(speed)}");
        var delay = QueryTypicalDelay();
        return $"Reaction Time {Num(QueryReaction())} ({string.Join(", ", parts)}). Your attack takes {Num(delay)} seconds.\n";
    }

    private bool CmdHp(string args)
    {
        Write($"HP {Hp}/{QueryMaxHp()}\n");
        return true;
    }

    private bool CmdQuests(string args)
    {
        Write(QuestLog());
        return true;
    }

    private string QuestLog()
    {
        var active = quests.Where(q => q.Value == 1).Select(q => QuestTitle(q.Key)).ToList();
        var done = quests.Where(q => q.Value == 2).Select(q => QuestTitle(q.Key)).ToList();
        if (active.Count == 0 && done.Count == 0) return "You have no quests.\n";
        return (active.Count > 0 ? "Active quests: " + string.Join(", ", active) + "\n" : "") +
               (done.Count > 0 ? "Completed quests: " + string.Join(", ", done) + "\n" : "");
    }

    private string QuestTitle(string id) => questTitles.TryGetValue(id, out var title) ? title : Cap(id.Replace('-', ' '));

    // ---- Builder tools ----

    private ObjectRef? FindHere(string name)
    {
        var room = Environment();
        if (name == "me" || name == "self") return Self;
        if (name == "here") return room;
        return Present(name) ?? (room == null ? null : Present(name, room));
    }

    private bool CmdGoto(string args)
    {
        if (!IsWizard(LevelGoto)) return false;
        args = args.Trim();
        if (args == "") { NotifyFail("Usage: {cmd}goto{/} ROOM-PATH or {cmd}goto{/} PLAYER"); return false; }
        ObjectRef destination;
        try
        {
            var player = Users().FirstOrDefault(u => string.Equals(u.Call<string>("QueryName"), args, StringComparison.OrdinalIgnoreCase));
            var place = player == null ? null : Environment(player);
            destination = place ?? LoadObject(ResolvePath(args));
        }
        catch (MudRuntimeException problem)
        {
            Write(Esc(problem.Message) + "\n");
            return true;
        }
        Say($"{Name} vanishes in a puff of smoke.\n");
        MoveObject(destination);
        Say($"{Name} appears in a puff of smoke.\n");
        Look();
        return true;
    }

    private bool CmdStat(string args)
    {
        if (!IsWizard(LevelStat)) return false;
        var thing = args.Trim() == "" ? null : FindHere(args.Trim());
        if (thing == null) { NotifyFail("Stat what? (a name, me, or here)"); return false; }
        Write(thing.Call<string>("Stat") ?? thing.Name + "\n");
        return true;
    }

    private bool CmdHeal(string args)
    {
        if (!IsWizard(LevelHeal)) return false;
        var target = args.Trim() == "" ? Self : FindHere(args.Trim());
        if (target == null || !target.Call<bool>("IsCreature")) { NotifyFail("Heal whom?"); return false; }
        target.Call("Heal", 100000);
        if (target == Self) Write("You are restored to full health.\n");
        else
        {
            Write($"You restore {target.Call<string>("QueryLabel")} to full health.\n");
            TellObject(target, "A warm light restores you to full health.\n");
        }
        return true;
    }

    private bool CmdSpeed(string args)
    {
        if (!IsWizard(LevelSpeed)) return false;
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2 || words.Length > 3 || !int.TryParse(words[1], out var percent) || percent < -90 || percent > 90
            || (words.Length == 3 && (!int.TryParse(words[2], out var checkSeconds) || checkSeconds < 0)))
        {
            NotifyFail("Usage: {cmd}speed{/} TARGET PERCENT [SECONDS]   (positive is haste, negative is slow, 0 clears; at most 90)");
            return false;
        }
        var seconds = words.Length == 3 ? int.Parse(words[2]) : 60;
        var target = FindHere(words[0]);
        if (target == null || !target.Call<bool>("IsCreature")) { NotifyFail("Change the speed of whom?"); return false; }
        if (percent == 0) seconds = 0;
        target.Call("AddSpeedEffect", "wizard", (100 - percent) / 100.0, seconds);
        var who = target == Self ? "You are" : $"{target.Call<string>("QueryLabel")} is";
        Write(percent == 0 ? $"{who} no longer hasted or slowed by you.\n"
            : $"{who} {(percent > 0 ? "hasted" : "slowed")} by {Math.Abs(percent)}% for {seconds} seconds.\n");
        if (target != Self) TellObject(target, percent == 0 ? "You feel your pace return to normal.\n"
            : percent > 0 ? "You feel yourself speed up.\n" : "You feel yourself slow down.\n");
        return true;
    }

    private bool CmdSmite(string args)
    {
        if (!IsWizard(LevelSmite)) return false;
        var target = args.Trim() == "" ? null : FindHere(args.Trim());
        if (target == null || target == Self || !target.Call<bool>("IsCreature")) { NotifyFail("Smite whom?"); return false; }
        var room = Environment();
        Write($"You smite {target.Call<string>("QueryLabel")}!\n");
        if (room != null) TellRoom(room, $"{Name} smites {target.Call<string>("QueryLabel")}!\n", Self, target);
        target.Call("Die", Self);
        return true;
    }
}
