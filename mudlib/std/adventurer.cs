// @inherit /std/living

/// <summary>
/// Everything about playing the game: the character sheet, gameplay commands,
/// death and respawn, quests and the saved character. /std/player inherits this
/// and adds accounts, wizard tools and files.
/// </summary>
public abstract class Adventurer : Living
{
    private const string RespawnRoom = "/domains/hollow/village/guest_rooms";
    private const string Registry = "/secure/classes";
    // The levelling constants, all in one place. Experience to reach a level is in the table and the power law of ExperienceFor.
    private const int MaxLevel = 100;
    private const int DefaultBaseHp = 30; // for a character that has no archetype yet (only between password and choice)
    private const int HpPerLevel = 8;
    private const int RegenBeats = 5; // one regeneration step per 10 seconds
    private const int LevelGoto = 1;
    private const int LevelStat = 1;
    private const int LevelHeal = 5;
    private const int LevelSpeed = 5;
    private const int LevelSlay = 10;
    private const int LevelSetLevel = 10;

    [Saved] private int xp;
    // A character's classes, as ids only. Everything a class grants is read from the class files at login.
    [Saved] private string archetype = "";
    [Saved] private string advancedClass = "";
    [Saved] private string subclass = "";
    [Saved] private string prestigeClass = "";
    [Saved] private Dictionary<string, int> quests = new();
    [Saved] private Dictionary<string, string> questTitles = new();
    [Saved] private List<string> killed = new();
    // One save record per carried object: its program path, then "?" and its state (quality, quantity, properties) unless
    // that is the default. Saves made before item state existed hold bare paths, which load as items in the default state.
    [Saved] private List<string> carried = new();
    [Saved] private List<int> carriedEquipped = new();
    [Saved] private string logoutRoom = "";

    private int regenBeats;
    private bool dying;
    private bool inventoryRestored;
    private double hpCarry; // fractions of a hit point regenerated but not yet given
    private bool adventureBegun;
    private bool equipKit; // a new character's weapon and shield are put on when the kit is created

    // What the classes give, worked out by RefreshClasses from the class files.
    private readonly List<ObjectRef> classObjects = new();
    private readonly List<ObjectRef> classSkills = new(); // skills and spells (not traits) of the classes
    private readonly List<ObjectRef> classTraits = new();
    private readonly HashSet<string> weaponAccess = new();
    private readonly HashSet<string> armorAccess = new();
    private bool shieldAccess;
    private bool unrestricted = true; // true until an archetype is chosen
    private int startHp = DefaultBaseHp;
    private int classMaxStamina = 70;
    private int classMaxMana = 70;
    private double classStaminaRegen = 6;
    private double classManaRegen = 5;
    private double classRate = 1.0;
    private double expModifier = 1.0;
    private string archetypeName = "";
    private string classTitle = "";
    private List<ObjectRef> grantedTraits = new();
    private long grantedAt;

    public override void Create()
    {
        Level = 1;
        DamageMin = 1; // unarmed 1-3; Improvised Combat makes it 2-5
        DamageMax = 3;
        Gold = 25;
    }

    public override int QueryMaxHp() => startHp + HpPerLevel * (Level - 1) + EquipmentHpBonus();

    public override int QueryMaxStamina() => classMaxStamina;

    public override int QueryMaxMana() => classMaxMana;

    public override double QueryStaminaRegen() => classStaminaRegen;

    public override double QueryManaRegen() => classManaRegen;

    public override double QueryAttackRate() => classRate;

    protected override double TraitMod(string key)
    {
        var total = 0.0;
        foreach (var trait in classTraits) total += trait.Call<double>("Modifier", key, Self);
        foreach (var trait in GrantedTraits()) total += trait.Call<double>("Modifier", key, Self);
        return total;
    }

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

    // Experience to reach levels 0 to 10 (modifier 1.0). Levels 6 to 10 come from the corridor pace model (docs/CORRIDOR_BALANCE.md).
    private static readonly int[] XpTable = { 0, 0, 100, 300, 600, 1000, 5400, 12000, 21700, 34100, 49300 };

    /// <summary>
    /// The levelling table, in one place: experience needed to reach level n, times the experience modifier. Level 1 needs nothing.
    /// Levels 2 to 5 are 50 n (n - 1); levels 6 to 10 are the table; beyond level 10 the curve is a provisional power law through
    /// level 10 (49,300 x (n / 10)^3), to be re-fitted when there is content for those levels.
    /// </summary>
    public static int ExperienceFor(int level, double modifier)
    {
        if (level <= 1) return 0;
        double basis = level < XpTable.Length ? XpTable[level] : XpTable[10] * Math.Pow(level / 10.0, 3.0);
        return (int)Math.Round(basis * modifier, MidpointRounding.AwayFromZero);
    }

    /// <summary>Experience needed to reach a level with this character's classes (the largest experience modifier among them).</summary>
    public int QueryXpForLevel(int level) => ExperienceFor(level, expModifier);

    public double QueryExpModifier() => expModifier;

    public void AddXp(int amount)
    {
        if (amount <= 0) return;
        xp += amount;
        Tell($"You gain {amount} experience.\n");
        CheckLevelUp();
    }

    /// <summary>Tells the event service about something this character did. Best-effort: history never stops play.</summary>
    protected void ReportEvent(string id, string subject, Dictionary<string, string>? names = null)
    {
        try { LoadObject("/secure/events").Call<string>("Report", id, Self, new List<ObjectRef> { Self }, Environment(), subject, names ?? new Dictionary<string, string>()); }
        catch (MudRuntimeException) { }
    }

    private void CheckLevelUp()
    {
        while (Level < MaxLevel && xp >= QueryXpForLevel(Level + 1))
        {
            Level++;
            Hp = QueryMaxHp();
            Tell($"You have reached level {Level}! You feel stronger and fully healed. Maximum HP is now {QueryMaxHp()}.\n");
            if (Level is 10 or 25 or 50 or 75 or 100) ReportEvent("level.milestone", Level.ToString(), new Dictionary<string, string> { ["level"] = Level.ToString() });
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

    // ---- Classes (docs/CLASS_SYSTEM.md sections 6 and 7) ----

    public string QueryArchetype() => archetype;

    /// <summary>True if the character has no calling yet (or its class file is gone) and must choose at login.</summary>
    public bool NeedsArchetype() => !adventureBegun && (archetype == "" || ClassFile(archetype) == null);

    /// <summary>The ids of the character's classes, archetype first.</summary>
    public List<string> QueryClassIds() => new[] { archetype, advancedClass, subclass, prestigeClass }.Where(c => c != "").ToList();

    public bool QueryHasClass(string id) => id != "" && QueryClassIds().Contains(id);

    /// <summary>The class a character has in a tier ("archetype", "advanced", "subclass" or "prestige"), or "".</summary>
    public string QueryClassOfTier(string tier) => tier switch
    {
        "archetype" => archetype, "advanced" => advancedClass, "subclass" => subclass, "prestige" => prestigeClass, _ => "",
    };

    /// <summary>The title of the highest class the character has ("Warrior", later "Knight").</summary>
    public string QueryClassTitle() => classTitle;

    private ObjectRef? ClassFile(string id)
    {
        try { return LoadObject(Registry).Call<ObjectRef>("Class", id); }
        catch (MudRuntimeException) { return null; }
    }

    private ObjectRef? SkillFile(string id)
    {
        try { return LoadObject(Registry).Call<ObjectRef>("Skill", id); }
        catch (MudRuntimeException) { return null; }
    }

    /// <summary>
    /// Work out everything the classes give from the class files: starting HP, resources, attack rate, experience modifier,
    /// equipment access, skills and traits. Nothing derived is saved, so this runs at login and whenever a class changes.
    /// A class file that is gone or disabled grants nothing.
    /// </summary>
    private void RefreshClasses()
    {
        classObjects.Clear();
        classSkills.Clear();
        classTraits.Clear();
        weaponAccess.Clear();
        armorAccess.Clear();
        shieldAccess = false;
        expModifier = 1.0;
        classTitle = "";
        archetypeName = "";
        var rate = 0.0;
        var known = false;
        foreach (var id in QueryClassIds())
        {
            var cls = ClassFile(id);
            if (cls == null || (!cls.Call<bool>("QueryEnabled") && cls.Call<string>("QueryTier") != "archetype")) continue;
            classObjects.Add(cls);
            classTitle = cls.Call<string>("QueryTitle") ?? classTitle;
            expModifier = Math.Max(expModifier, cls.Call<double>("QueryExpModifier"));
            if (cls.Call<double>("QueryAttackRate") > 0) rate = cls.Call<double>("QueryAttackRate");
            foreach (var w in cls.Call<List<string>>("QueryWeapons") ?? new List<string>()) weaponAccess.Add(w);
            foreach (var a in cls.Call<List<string>>("QueryArmorTypes") ?? new List<string>()) armorAccess.Add(a);
            if (cls.Call<bool>("QueryShield")) shieldAccess = true;
            foreach (var sid in cls.Call<List<string>>("QuerySkills") ?? new List<string>())
                if (SkillFile(sid) is { } skill && !classSkills.Contains(skill)) classSkills.Add(skill);
            foreach (var tid in cls.Call<List<string>>("QueryTraits") ?? new List<string>())
                if (SkillFile(tid) is { } trait && !classTraits.Contains(trait)) classTraits.Add(trait);
            if (cls.Call<string>("QueryTier") == "archetype")
            {
                known = true;
                archetypeName = cls.Call<string>("QueryClassName") ?? "";
                startHp = cls.Call<int>("QueryStartHp");
                BaseReaction = cls.Call<double>("QueryStartReaction");
                classMaxStamina = cls.Call<int>("QueryMaxStamina");
                classMaxMana = cls.Call<int>("QueryMaxMana");
                classStaminaRegen = cls.Call<double>("QueryStaminaRegen");
                classManaRegen = cls.Call<double>("QueryManaRegen");
            }
        }
        unrestricted = !known;
        classRate = rate > 0 ? rate : 1.0;
        if (!known)
        {
            startHp = DefaultBaseHp;
            classMaxStamina = 70;
            classMaxMana = 70;
            classStaminaRegen = 6;
            classManaRegen = 5;
        }
        grantedAt = 0;
    }

    /// <summary>
    /// Called by /secure/login after the password, for a character with no archetype (a new one, or one saved before classes):
    /// take the chosen archetype. A character that has played before keeps their level, items and gold; one that has not
    /// receives the archetype's starting gold and kit. Returns null, or why the choice was refused. Works once.
    /// </summary>
    public string? ChooseArchetype(string id)
    {
        var caller = PreviousObject();
        if (adventureBegun || archetype != "" && ClassFile(archetype) != null) return "Your calling is already chosen.";
        if (caller == null || !caller.IsClone || caller.ProgramPath != "/secure/login" || !Interactive(caller)) return "That is not possible now.";
        var cls = ClassFile(id);
        if (cls == null || cls.Call<string>("QueryTier") != "archetype" || !cls.Call<bool>("QueryEnabled")) return "There is no such calling.";
        var pristine = Level == 1 && xp == 0 && carried.Count == 0 && quests.Count == 0 && Gold == 25 && advancedClass == "";
        archetype = id;
        RefreshClasses();
        if (pristine)
        {
            Gold = cls.Call<int>("QueryStartGold");
            carried.Clear();
            carriedEquipped.Clear();
            foreach (var path in cls.Call<List<string>>("QueryKit") ?? new List<string>()) carried.Add(path);
            equipKit = true;
        }
        Hp = QueryMaxHp();
        FillResources();
        return null;
    }

    /// <summary>
    /// Give the character an advanced, subclass or prestige class. This is the entry point the Hall of Legends in Oakenfell (stage B) calls when a
    /// hallmaster completes a pledge; it is not a player command, and only another object may call it. It checks the class
    /// exists and is enabled, that the tier is free, that the parent class is one the character has, and every requirement
    /// of the class file. Returns null on success or the reason for refusing.
    /// </summary>
    public string? ApplyClass(string id)
    {
        var caller = PreviousObject();
        if (caller == null || caller == Self || caller.Call<bool>("IsPlayerCharacter")) return "A class is given by the game, not taken.";
        var cls = ClassFile(id);
        if (cls == null) return "There is no such class.";
        if (!cls.Call<bool>("QueryEnabled")) return "That class is not open.";
        var tier = cls.Call<string>("QueryTier") ?? "";
        if (tier == "archetype") return "A calling cannot be changed.";
        if (QueryClassOfTier(tier) != "") return $"You already have a {tier} class.";
        var parent = cls.Call<string>("QueryParent") ?? "";
        if (parent != "" && !QueryHasClass(parent)) return "You do not have the calling this class builds on.";
        var problem = cls.Call<string>("CheckRequirements", Self);
        if (problem != null) return problem;
        switch (tier)
        {
            case "advanced": advancedClass = id; break;
            case "subclass": subclass = id; break;
            case "prestige": prestigeClass = id; break;
            default: return "That class cannot be given.";
        }
        RefreshClasses();
        CheckLevelUp();
        BindSkillCommands();
        Tell($"You are now a {classTitle}.\n");
        PersistNow();
        return null;
    }

    // ---- Skills ----

    private List<ObjectRef> GrantedTraits()
    {
        if (TimeMs() - grantedAt < 1000 && grantedAt != 0) return grantedTraits;
        grantedAt = TimeMs();
        grantedTraits = GrantedSkills().Where(s => s.Call<string>("QueryKind") == "trait").ToList();
        return grantedTraits;
    }

    /// <summary>Skills, spells and traits that equipped items grant. They are listed apart and vanish with the item.</summary>
    private List<ObjectRef> GrantedSkills()
    {
        var result = new List<ObjectRef>();
        foreach (var item in QueryEquipment())
            foreach (var id in item.Call<List<string>>("QueryGrantedSkills") ?? new List<string>())
                if (SkillFile(id) is { } skill && !result.Contains(skill) && !classSkills.Contains(skill) && !classTraits.Contains(skill)) result.Add(skill);
        return result;
    }

    private ObjectRef? FindSkillByCommand(string word)
    {
        foreach (var skill in classSkills.Concat(GrantedSkills()))
            if (skill.Call<string>("QueryCommand") == word) return skill;
        return null;
    }

    /// <summary>Bind the command word of every skill the character has. A word bound once stays bound; one for a skill that is gone says so.</summary>
    private void BindSkillCommands()
    {
        if (ThisPlayer() != Self) return;
        foreach (var skill in classSkills.Concat(GrantedSkills()))
        {
            var word = skill.Call<string>("QueryCommand");
            if (!string.IsNullOrEmpty(word)) AddAction(word, CmdSkill);
        }
    }

    private bool CmdSkill(string args)
    {
        var skill = FindSkillByCommand(QueryVerb());
        if (skill == null) return false;
        args = args.Trim();
        var room = Environment();
        ObjectRef? target = null;
        var kind = skill.Call<string>("QueryTarget");
        if (kind == "enemy")
        {
            target = args == "" ? Enemies().FirstOrDefault(e => Environment(e) == room) : room == null ? null : Present(args, room);
            if (target == null) { Write(args == "" ? "Use it on whom? You are not fighting anyone.\n" : $"You see no '{Esc(args)}' here.\n"); return true; }
            if (!target.Call<bool>("IsCreature")) { Write("You cannot fight that.\n"); return true; }
        }
        else if (kind == "friend")
        {
            target = args == "" || args == "me" || args == "self" ? Self : room == null ? null : Present(args, room);
            if (target == null || !target.Call<bool>("IsCreature")) { Write($"There is no one called '{Esc(args)}' here.\n"); return true; }
        }
        else if (kind == "item")
        {
            target = args == "" ? null : Present(args) ?? (room == null ? null : Present(args, room));
            if (target == null) { Write(args == "" ? "Use it on what?\n" : $"You see no '{Esc(args)}' here.\n"); return true; }
        }
        var refusal = skill.Call<string>("CanUse", Self, target) ?? SkillRefusal(skill);
        if (refusal != null) { Write(refusal + "\n"); return true; }
        if (kind == "enemy" && target != null)
        {
            if (!QueryFightingWith(target))
            {
                var no = CanFight(target);
                if (no != null) { Write(no + "\n"); return true; }
                StartFight(target);
            }
            Focus(target);
        }
        refusal = UseSkill(skill, target);
        if (refusal != null) Write(refusal + "\n");
        return true;
    }

    private bool CmdSkills(string args)
    {
        var text = new StringBuilder();
        var own = classSkills.Where(s => s.Call<string>("QueryKind") != "trait").ToList();
        var traits = classTraits.ToList();
        text.Append(own.Count == 0 ? "You have no skills or spells.\n" : "Skills and spells (cost; time to use for you):\n");
        foreach (var skill in own) text.Append("  " + SkillLine(skill) + "\n");
        if (traits.Count > 0)
        {
            text.Append("Traits:\n");
            foreach (var trait in traits) text.Append("  " + SkillLine(trait) + "\n");
        }
        var granted = GrantedSkills();
        if (granted.Count > 0)
        {
            text.Append("Granted by what you wear or wield (gone when it is removed):\n");
            foreach (var skill in granted) text.Append("  " + SkillLine(skill) + "\n");
        }
        Write(text.ToString());
        return true;
    }

    private string SkillLine(ObjectRef skill)
    {
        var delay = (QueryReaction() + skill.Call<double>("QueryReactionMod") + skill.Call<double>("QueryRecovery")) * 2;
        return skill.Call<string>("Line", delay) ?? "";
    }

    /// <summary>Why a skill cannot be paid for or used now, or null. Checked before a fight is started for it.</summary>
    private string? SkillRefusal(ObjectRef skill)
    {
        var isSpell = skill.Call<string>("QueryKind") == "spell";
        if (isSpell && QueryMod("nospell") > 0) return "You cannot cast spells like this.";
        var mana = skill.Call<int>("QueryManaCost");
        if (isSpell && mana > 0) mana = Math.Max(1, (int)Math.Round(mana * (1 + QueryMod("spell_cost_pct") / 100.0), MidpointRounding.AwayFromZero));
        if (QueryStamina() < skill.Call<int>("QueryStaminaCost")) return "You are too tired for that.";
        if (QueryMana() < mana) return "You do not have the mana for that.";
        return null;
    }

    // ---- Equipment access (docs/CLASS_SYSTEM.md section 5) ----

    /// <summary>Why this character may not wield or wear the item, or null. An item without a category is for anyone.</summary>
    private string? AccessRefusal(ObjectRef item)
    {
        if (unrestricted) return null;
        var category = item.Call<string>("QueryCategory") ?? "";
        var slot = item.Call<string>("QuerySlot") ?? "";
        var who = Cap(archetypeName) + "s";
        if (category == "" || category == "trinket") return null;
        if (slot == "weapon")
            return weaponAccess.Contains("*") || weaponAccess.Contains(category) ? null : $"You are not trained to wield {Article(category)} {category}: {who} cannot.";
        if (category == "shield")
            return shieldAccess ? null : $"You are not trained to carry a shield: {who} cannot.";
        return armorAccess.Contains("*") || armorAccess.Contains(category) ? null : $"You are not trained to wear {category} armor: {who} cannot.";
    }

    private static string Article(string word) => word.Length > 0 && "aeiou".Contains(word[0]) ? "an" : "a";

    // ---- Saving ----

    /// <summary>Record the carried items and the logout room just before the player is saved.</summary>
    protected void PrepareSave()
    {
        if (!inventoryRestored) return;
        carried.Clear();
        carriedEquipped.Clear();
        foreach (var item in AllInventory())
        {
            carried.Add(RecordOf(item));
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
        AddAction("give", CmdGive);
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
        AddAction("skills", CmdSkills);
        AddAction("quests", CmdQuests);
        AddAction("search", CmdSearch);
        // Builder tools. Each handler checks the wizard level on every use and
        // declines below it, so to everyone else these verbs do not exist.
        AddAction("goto", CmdGoto);
        AddAction("stat", CmdStat);
        AddAction("restore", CmdRestore);
        AddAction("slay", CmdSlay);
        AddAction("speed", CmdSpeed);
        AddAction("setlevel", CmdSetLevel);

        SetHeartBeat(true);
        adventureBegun = true;
        RefreshClasses();

        for (var i = 0; i < carried.Count; i++)
        {
            try
            {
                var item = CloneRecord(carried[i], Self);
                if (item == null) continue;
                var slot = item.Call<string>("QuerySlot") ?? "";
                var put = carriedEquipped.Contains(i) || (equipKit && (slot == "weapon" || slot == "shield") && QueryEquipped(slot) == null);
                if (put && AccessRefusal(item) == null) Equip(item);
            }
            catch (MudRuntimeException)
            {
                // The file is gone or no longer compiles; the item is lost.
            }
        }
        equipKit = false;
        inventoryRestored = true;
        BindSkillCommands();
    }

    /// <summary>
    /// Belongings that were in this player's corpse when the game last stopped are
    /// handed back by the innkeeper. Call once at login, after the inventory is restored.
    /// </summary>
    protected void ClaimLostProperty()
    {
        List<string> claim;
        try { claim = LoadObject("/secure/lost_property").Call<List<string>>("Claim") ?? new List<string>(); }
        catch (MudRuntimeException) { return; }
        if (claim.Count == 0) return;
        var coins = int.TryParse(claim[0], out var parsed) ? parsed : 0;
        var returned = 0;
        for (var i = 1; i < claim.Count; i++)
        {
            try
            {
                if (CloneRecord(claim[i], Self) != null) returned += RecordQuantity(claim[i]);
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
        if (QueryInCombat() || Hp >= QueryMaxHp()) { hpCarry = 0; return; }
        var room = Environment();
        hpCarry += (room != null && room.Call<bool>("QuerySafe") ? 3 : 1) * (1 + QueryMod("regen_out"));
        var whole = (int)Math.Floor(hpCarry + 1e-9);
        hpCarry -= whole;
        if (whole > 0) Heal(whole);
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
            FillResources();
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
        room!.Call("Examined", Self, args); // a detail may be what reveals a hidden exit
        return true;
    }

    private bool CmdGet(string args)
    {
        var room = Environment();
        var from = args.IndexOf(" from ", StringComparison.Ordinal);
        if (from > 0) return GetFromCorpse(args.Substring(0, from).Trim(), args.Substring(from + 6).Trim());
        if (room == null || !ParseAmount(args, out var count, out var all, out var noun)) { NotifyFail("Get what?"); return false; }
        if (!all && count == 1)
        {
            var item = Present(noun, room);
            if (item == null || item == Self) { NotifyFail("Get what?"); return false; }
            if (!item.Call<bool>("CanGet")) { Write("You can't take that.\n"); return true; }
            Pick(item, 1);
            return true;
        }
        var named = Matching(room, noun);
        var found = named.Where(o => o.Call<bool>("CanGet")).ToList();
        if (found.Count == 0)
        {
            if (all && noun == "") { Write("There is nothing here to take.\n"); return true; }
            if (named.Count > 0) { Write("You can't take that.\n"); return true; }
            NotifyFail("Get what?");
            return false;
        }
        var here = found.Sum(QuantityOf);
        if (!all && here < count) { Write($"There {(here == 1 ? "is" : "are")} only {here} here.\n"); return true; }
        var left = all ? int.MaxValue : count;
        foreach (var item in found)
        {
            if (left <= 0) break;
            var n = Math.Min(left, QuantityOf(item));
            Pick(item, n);
            left -= n;
        }
        return true;
    }

    /// <summary>Pick up n of an item lying in the room, with the messages.</summary>
    private void Pick(ObjectRef item, int n)
    {
        var description = Describe(item, n);
        var held = MovePortion(item, n, Self);
        if (held == null) { Write($"You cannot take {description}.\n"); return; }
        Write($"You take {description}.\n");
        Say($"{Name} takes {description}.\n");
        held.Call("Taken", Self);
    }

    /// <summary>
    /// What a command that takes an amount acts on in the inventory, as (object, how many of it). Null if nothing carried
    /// answers to the name. With refusal set, the player has fewer than asked and nothing should happen. "all" leaves
    /// equipped things alone; naming one picks it as before.
    /// </summary>
    private List<(ObjectRef item, int n)>? FromInventory(int count, bool all, string noun, out string? refusal)
    {
        refusal = null;
        List<ObjectRef> found;
        if (!all && count == 1)
        {
            var one = Present(noun.ToLowerInvariant());
            found = one == null ? new List<ObjectRef>() : new List<ObjectRef> { one };
        }
        else found = Matching(Self, noun).Where(o => !(all && IsEquipped(o))).ToList();
        if (found.Count == 0) return null;
        var have = found.Sum(QuantityOf);
        if (!all && have < count) { refusal = $"You have only {have}."; return new List<(ObjectRef, int)>(); }
        var plan = new List<(ObjectRef item, int n)>();
        var left = all ? int.MaxValue : count;
        foreach (var ob in found)
        {
            if (left <= 0) break;
            var n = Math.Min(left, QuantityOf(ob));
            plan.Add((ob, n));
            left -= n;
        }
        return plan;
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
        if (room == null || !ParseAmount(args, out var count, out var all, out var noun)) { NotifyFail("Drop what?"); return false; }
        var plan = FromInventory(count, all, noun, out var refusal);
        if (plan == null) { NotifyFail("Drop what?"); return false; }
        if (refusal != null) { Write(refusal + "\n"); return true; }
        foreach (var (item, n) in plan)
        {
            var description = Describe(item, n);
            if (MovePortion(item, n, room) == null) { Write($"You cannot drop {description}.\n"); continue; }
            Write($"You drop {description}.\n");
            Say($"{Name} drops {description}.\n");
        }
        return true;
    }

    /// <summary>
    /// "give [NUMBER|all] THING to PLAYER": hand things to another player in the room. Quest items cannot be given (the
    /// travel pass and the Overseer's Orders prove what their holder did). Both characters are saved at once, the giver
    /// first, so that a crash straight after a gift cannot leave the item in both save files.
    /// </summary>
    private bool CmdGive(string args)
    {
        var room = Environment();
        var to = args.LastIndexOf(" to ", StringComparison.Ordinal);
        if (room == null || to <= 0) { NotifyFail("Usage: {cmd}give{/} [NUMBER] THING {cmd}to{/} PLAYER"); return false; }
        var whom = args.Substring(to + 4).Trim().ToLowerInvariant();
        var receiver = whom == "" ? null : Present(whom, room);
        if (receiver == Self) { Write("You already have it.\n"); return true; }
        if (receiver == null) { NotifyFail($"There is no one called '{Esc(whom)}' here."); return false; }
        if (!receiver.Call<bool>("IsPlayerCharacter") || !Interactive(receiver))
        {
            Write($"{Cap(receiver.Call<string>("QueryLabel") ?? receiver.Call<string>("Short") ?? "It")} does not take things from you. You can give things only to other players.\n");
            return true;
        }
        if (!ParseAmount(args.Substring(0, to), out var count, out var all, out var noun)) { NotifyFail("Give what?"); return false; }
        var plan = FromInventory(count, all, noun, out var refusal);
        if (plan == null) { NotifyFail(all && noun == "" ? "You have nothing to give." : $"You have no '{Esc(noun)}'."); return false; }
        if (refusal != null) { Write(refusal + "\n"); return true; }
        var important = plan.Where(p => p.item.Call<bool>("QueryQuestItem")).ToList();
        if (important.Count > 0 && !all) { Write($"You cannot give away {Describe(important[0].item, important[0].n)}; it is too important.\n"); return true; }
        foreach (var (item, _) in important) Write($"You keep {item.Call<string>("Short")}; it is too important to give away.\n");
        var given = 0;
        var other = receiver.Call<string>("QueryName") ?? "them";
        foreach (var (item, n) in plan)
        {
            if (important.Any(p => p.item == item)) continue;
            var description = Describe(item, n);
            if (MovePortion(item, n, receiver) == null) { Write($"You cannot give {description}.\n"); continue; }
            given++;
            Write($"You give {description} to {other}.\n");
            TellObject(receiver, $"{Name} gives you {description}.\n");
            TellRoom(room, $"{Name} gives {description} to {other}.\n", Self, receiver);
        }
        if (given == 0) return true;
        PersistNow();
        receiver.Call("SaveAfterTrade");
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

    // ---- World features: hidden exits and search (docs/GAME_SYSTEMS.md) ----

    private const int MaxFoundExits = 1000;
    private const int MaxFoundExitsInWiz = 50; // records for rooms under /wiz are capped, so a sandbox cannot fill the save file

    /// <summary>The hidden exits this character has found, as "room path|direction". Kept on the character; a room asks for its own.</summary>
    [Saved] private List<string> foundExits = new();

    public bool QueryFoundExit(string roomPath, string direction) => foundExits.Contains(roomPath + "|" + direction);

    /// <summary>
    /// A room tells the character they have found one of its hidden exits. The room is the caller and can only record itself, for a
    /// character who is in it, so no other object (a wizard's, say) can put a record against a room it is not. Returns true if the exit
    /// was newly recorded; the record is saved at once.
    /// </summary>
    public bool RecordFoundExit(string direction)
    {
        var room = PreviousObject();
        if (room == null || room.IsClone || Environment() != room || direction == "" || direction.Contains('|')) return false;
        if (!room.Call<List<string>>("QueryHiddenExits")?.Contains(direction) ?? true) return false;
        var key = room.Name + "|" + direction;
        if (foundExits.Contains(key) || foundExits.Count >= MaxFoundExits) return false;
        if (room.Name.StartsWith("/wiz/", StringComparison.OrdinalIgnoreCase) && foundExits.Count(k => k.StartsWith("/wiz/", StringComparison.OrdinalIgnoreCase)) >= MaxFoundExitsInWiz) return false;
        foundExits.Add(key);
        PersistNow();
        return true;
    }

    private bool CmdSearch(string args)
    {
        if (Environment() == null) return false;
        if (QueryInCombat()) { Write("You cannot search while you are fighting.\n"); return true; }
        // A short action on the combat clock: its delay is your Reaction Time less 1 (not below 0), about 3 seconds.
        ActAsAction(new CombatAction { Id = "search", Name = "Search", Kind = "search", Preparation = 0, Recovery = 0, ReactionMod = -1.0 }, "search");
        return true;
    }

    protected override void Special(CombatAction action, ObjectRef? target)
    {
        if (action.Kind != "search") { base.Special(action, target); return; }
        var room = Environment();
        if (room == null) return;
        if (QueryInCombat()) { Tell("You cannot search while you are fighting.\n"); return; }
        TellRoom(room, $"{Name} searches the area carefully.\n", Self);
        if (!room.Call<bool>("Search", Self)) Tell("You search carefully but find nothing of note.\n");
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
        var refusal = AccessRefusal(item);
        if (refusal != null) { Write(refusal + "\n"); return true; }
        var old = QueryEquipped(slot);
        if (old != null)
        {
            Unequip(slot);
            Write($"You stop using {old.Call<string>("Short")}.\n");
        }
        Equip(item);
        Write($"{you} {description}.\n");
        Say($"{Name} {they} {description}.\n");
        grantedAt = 0;
        BindSkillCommands();
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
        text.Append(Level >= MaxLevel ? " (highest level)\n" : $" ({QueryXpForLevel(Level + 1) - xp} more for level {Level + 1})\n");
        var calling = archetypeName == "" ? "none yet" : archetypeName;
        text.Append($"Calling {calling}" + (advancedClass != "" || subclass != "" || prestigeClass != "" ? $"   Class {classTitle}" : "") + "\n");
        var area = Environment()?.Call<string>("QueryAreaName") ?? "";
        var region = Environment()?.Call<string>("QueryRegionName") ?? "";
        if (area != "" || region != "") text.Append($"Area {area}   Region {region}\n");
        try { text.Append(LoadObject("/secure/renown").Call<string>("TierLine", QueryName()) ?? ""); }
        catch (MudRuntimeException) { }
        text.Append($"HP {Hp}/{QueryMaxHp()}   Stamina {QueryStamina()}/{QueryMaxStamina()}   Mana {QueryMana()}/{QueryMaxMana()}   Gold {Gold}\n");
        text.Append($"Damage {QueryDamageMin()}-{QueryDamageMax()}   To hit +{QueryAttack()}   Defense {QueryDefense()}   Armor {QueryArmor()}   Dodge {QueryDodge()}%   Block {QueryBlock()}%   Critical {QueryCrit()}%\n");
        var effectLines = QueryEffectLines();
        if (effectLines.Count > 0) text.Append("Effects: " + string.Join(", ", effectLines) + "\n");
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
            var mod = QueryItemReaction(item);
            if (mod != 0) parts.Add($"{item.Call<string>("Short")} {(mod > 0 ? "+" : "")}{Num(mod)}");
        }
        var speed = QuerySpeedFactor();
        if (Math.Abs(speed - 1.0) > 0.001) parts.Add($"speed x{Num(speed)}");
        var delay = QueryTypicalDelay();
        return $"Reaction Time {Num(QueryReaction())} ({string.Join(", ", parts)}). Your attack takes {Num(delay)} seconds.\n";
    }

    private bool CmdHp(string args)
    {
        Write($"HP {Hp}/{QueryMaxHp()}   Stamina {QueryStamina()}/{QueryMaxStamina()}   Mana {QueryMana()}/{QueryMaxMana()}\n");
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

    private bool CmdRestore(string args)
    {
        if (!IsWizard(LevelHeal)) return false;
        var target = args.Trim() == "" ? Self : FindHere(args.Trim());
        if (target == null || !target.Call<bool>("IsCreature")) { NotifyFail("Restore whom?"); return false; }
        target.Call("Heal", 100000);
        target.Call("FillResources");
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

    private bool CmdSlay(string args)
    {
        if (!IsWizard(LevelSlay)) return false;
        var target = args.Trim() == "" ? null : FindHere(args.Trim());
        if (target == null || target == Self || !target.Call<bool>("IsCreature")) { NotifyFail("Slay whom?"); return false; }
        var room = Environment();
        Write($"You slay {target.Call<string>("QueryLabel")}!\n");
        if (room != null) TellRoom(room, $"{Name} slays {target.Call<string>("QueryLabel")}!\n", Self, target);
        target.Call("Die", Self);
        return true;
    }

    /// <summary>
    /// A test and builder tool: set your level (1 to 100). Your experience is set to exactly what that level needs, your
    /// HP and resources are filled. Level 10 wizards only.
    /// </summary>
    private bool CmdSetLevel(string args)
    {
        if (!IsWizard(LevelSetLevel)) return false;
        if (!int.TryParse(args.Trim(), out var level) || level < 1 || level > MaxLevel) { NotifyFail("Usage: {cmd}setlevel{/} 1-" + MaxLevel); return false; }
        Level = level;
        xp = QueryXpForLevel(level);
        Hp = QueryMaxHp();
        FillResources();
        Write($"You are now level {Level} with {xp} experience; the next level needs {(Level >= MaxLevel ? 0 : QueryXpForLevel(Level + 1))}.\n");
        return true;
    }
}
