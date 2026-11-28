// @inherit /std/questjournal
// @use /std/skill

/// <summary>
/// Everything about playing the game: the character sheet, gameplay commands,
/// death and respawn, quests and the saved character. /std/player inherits this
/// and adds accounts, wizard tools and files.
/// </summary>
public abstract class Adventurer : QuestJournal
{
    private const string RespawnRoom = "/domains/hollow/village/guest_rooms";
    private const string Registry = "/secure/classes";
    // The levelling constants, all in one place. Experience to reach a level is in the table and the power law of ExperienceFor.
    private const int MaxLevel = 100;
    private const int AttributePointsPerLevel = 2; // placed with "improve"; level 10 has 18, level 100 has 198
    private const int RegenBeats = 5; // one regeneration step per 10 seconds
    // Resting (docs/specs/rest.md): the two numbers of the rule, in one place.
    private const double RestFactor = 5.0; // Vitality, Endurance and Focus return this many times faster while seated
    private const int RestSurprise = 2;    // seconds added to the first action of a seated character who is attacked
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
    [Saved] private List<string> killed = new();
    // One save record per carried object: its program path, then "?" and its state (quality, quantity, properties) unless
    // that is the default. Saves made before item state existed hold bare paths, which load as items in the default state.
    [Saved] private List<string> carried = new();
    [Saved] private List<int> carriedEquipped = new();
    [Saved] private string logoutRoom = "";
    // "verbose on": combat lines and healing lines show the numbers. Off by default; saved with the character.
    [Saved] private bool verbose;

    private int regenBeats;
    private bool dying;
    private bool inventoryRestored;
    private double vitalityCarry; // fractions of a point of Vitality regenerated but not yet given
    private bool resting;          // sitting down to recover; never saved, so a new login starts standing
    private string restRoom = "";  // where the character sat down: resting ends when they are anywhere else
    private bool restedTold;       // "You are fully rested." has been said for this rest
    private int restBeats;         // heartbeats spent resting in the current ten-second regeneration step
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
    // The resource numbers of docs/ATTRIBUTES.md section 2, from the class files: base and gain per level. Until a calling is
    // chosen (only between the password and the choice) they are the Adventurer's.
    private int vitalityBase = 10, vitalityPerLevel = 8;
    private int enduranceBase = 50, endurancePerLevel = 2;
    private int focusBase = 50, focusPerLevel = 2;
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

    // ---- Resources from the class and the attributes (docs/ATTRIBUTES.md section 2) ----
    // Maximum Vitality = class base + class per-level x Level + Vigor x 2; Endurance = base + per-level x Level + Resolve x 2 + Vigor;
    // Focus = base + per-level x Level + Insight x 2 + Spirit. Class modifiers (percent) apply after. Regeneration per 10 seconds:
    // Vitality 0.3 + Vigor / 10; Endurance Maximum x (6% + (Resolve + Vigor) x 0.3%); Focus Maximum x (1% + (Insight + Spirit) x 0.15%).

    private const double VitalityRegenBase = 0.3, VitalityRegenPerVigor = 0.1;
    private const double EnduranceRegenBase = 0.06, EnduranceRegenPerPoint = 0.003;
    private const double FocusRegenBase = 0.01, FocusRegenPerPoint = 0.0015; // 0.15% a point (docs/ATTRIBUTES.md section 8)

    private int Modified(double value, string key) => (int)Math.Round(value * (1 + ClassModifier(key) / 100.0), MidpointRounding.AwayFromZero);

    public override int QueryMaxVitality() =>
        Modified(vitalityBase + vitalityPerLevel * Level + 2 * QueryAttribute("vigor"), "vitality") + EquipmentVitalityBonus();

    public override int QueryMaxEndurance() =>
        Modified(enduranceBase + endurancePerLevel * Level + 2 * QueryAttribute("resolve") + QueryAttribute("vigor"), "endurance");

    public override int QueryMaxFocus() =>
        Modified(focusBase + focusPerLevel * Level + 2 * QueryAttribute("insight") + QueryAttribute("spirit"), "focus");

    public override double QueryEnduranceRegen() =>
        QueryMaxEndurance() * (EnduranceRegenBase + (QueryAttribute("resolve") + QueryAttribute("vigor")) * EnduranceRegenPerPoint) * (1 + ClassModifier("endurance_regen") / 100.0);

    public override double QueryFocusRegen() =>
        QueryMaxFocus() * (FocusRegenBase + (QueryAttribute("insight") + QueryAttribute("spirit")) * FocusRegenPerPoint) * (1 + ClassModifier("focus_regen") / 100.0);

    /// <summary>Vitality regained per 10 seconds out of combat, before the safe room and Survival Training.</summary>
    public override double QueryVitalityRegen() =>
        (VitalityRegenBase + QueryAttribute("vigor") * VitalityRegenPerVigor) * (1 + ClassModifier("vitality_regen") / 100.0);

    protected override double ClassModifier(string key)
    {
        var total = 0.0;
        foreach (var cls in classObjects) total += cls.Call<double>("QueryModifier", key);
        return total;
    }

    public override string QueryPossessive() => "their";

    // ---- Attributes (docs/ATTRIBUTES.md) ----
    // The six values are saved with the character: the calling's starting values plus every point placed with "improve". Two
    // points come with every level; the unspent ones are 2 x (level - 1) less those placed. Only the character's own "improve"
    // command changes them: there is no public setter, so no other object (a wizard's, say) can set them or grant points.

    [Saved] private Dictionary<string, int> attributeValues = new();
    [Saved] private int attributePointsSpent;

    public override int QueryBaseAttribute(string name) =>
        !AttributeNames.Contains(name) ? 0 : attributeValues.TryGetValue(name, out var value) ? value : AttributeNeutral;

    /// <summary>Attribute points earned and not yet placed.</summary>
    public int QueryAttributePoints() => Math.Max(0, AttributePointsPerLevel * (Level - 1) - attributePointsSpent);

    /// <summary>
    /// A character with no attributes (a new one, or one saved before attributes existed) takes the calling's starting values;
    /// every point of its levels is left to place. Runs when the classes are worked out, so at login and at the choice of a calling.
    /// </summary>
    private void EnsureAttributes(ObjectRef? archetypeClass)
    {
        if (attributeValues.Count > 0 || archetypeClass == null) return;
        foreach (var name in AttributeNames) attributeValues[name] = archetypeClass.Call<int>("QueryStartAttribute", name);
        attributePointsSpent = 0;
    }

    private static string AttributeTitle(string name) => Cap(name);

    /// <summary>"improve" shows the attributes and the points to place; "improve ATTRIBUTE [N]" places one point (or N).</summary>
    private bool CmdImprove(string args)
    {
        var words = args.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var points = QueryAttributePoints();
        if (words.Length == 0)
        {
            Write(AttributeLine() + (points > 0 ? $"You have {PointsText(points)} to place: {{cmd}}improve{{/}} ATTRIBUTE [NUMBER].\n" : "You have no attribute points to place. Every level brings two.\n"));
            return true;
        }
        var matches = AttributeNames.Where(a => words[0].Length >= 3 && a.StartsWith(words[0])).ToList();
        if (matches.Count != 1) { Write("Improve which? Might, Grace, Vigor, Insight, Spirit or Resolve.\n"); return true; }
        var count = 1;
        if (words.Length > 2 || (words.Length == 2 && (!int.TryParse(words[1], out count) || count < 1))) { NotifyFail("Usage: {cmd}improve{/} ATTRIBUTE [NUMBER]"); return false; }
        if (attributeValues.Count == 0) { Write("You have no calling yet.\n"); return true; }
        if (points <= 0) { Write("You have no attribute points to place. Every level brings two.\n"); return true; }
        if (count > points) { Write($"You have only {PointsText(points)} to place.\n"); return true; }
        var name = matches[0];
        attributeValues[name] = attributeValues.GetValueOrDefault(name, AttributeNeutral) + count;
        attributePointsSpent += count;
        Write($"You place {PointsText(count)} in {AttributeTitle(name)}. {AttributeTitle(name)} is now {QueryBaseAttribute(name)}" +
              (QueryAttributePoints() > 0 ? $"; {PointsText(QueryAttributePoints())} left to place.\n" : ".\n"));
        PersistNow();
        return true;
    }

    private static string PointsText(int n) => n == 1 ? "1 point" : $"{n} points";

    /// <summary>"Might 8  Grace 5  Vigor 7  Insight 5  Spirit 5  Resolve 6", with equipment shown where it adds.</summary>
    private string AttributeLine()
    {
        var parts = AttributeNames.Select(a =>
        {
            var total = QueryAttribute(a);
            var basis = QueryBaseAttribute(a);
            return $"{AttributeTitle(a)} {total}" + (total != basis ? $" ({basis})" : "");
        });
        return string.Join("   ", parts) + "\n";
    }

    public override double QueryAttackRate() => classRate;

    protected override double TraitMod(string key)
    {
        var total = 0.0;
        foreach (var trait in classTraits) total += trait.Call<double>("Modifier", key, Self);
        foreach (var trait in GrantedTraits()) total += trait.Call<double>("Modifier", key, Self);
        return total;
    }

    /// <summary>A modifier from the character's class traits only (not items, not timed effects): what the crafting daemon's learning reads.</summary>
    public double QueryClassTraitMod(string key)
    {
        var total = 0.0;
        foreach (var trait in classTraits) total += trait.Call<double>("Modifier", key ?? "", Self);
        return total;
    }

    public override bool IsPlayerCharacter() => true;

    public bool QueryVerbose() => verbose;

    // ---- The prompt ----
    // Shown by the driver after every command and after unprompted output. The player's text is untrusted input: it is cleaned of
    // control characters and of every {tag} but the listed tokens, limited in length, and only the listed tokens are expanded.

    private const string DefaultPrompt = "{name} ({vitality}/{maxvitality}) ({endurance}/{maxendurance}) ({focus}/{maxfocus}) > ";
    private const string OldDefaultPrompt = "{name} ({hp}/{maxhp}) ({stamina}/{maxstamina}) ({mana}/{maxmana}) > ";
    private const int MaxPromptLength = 80;

    /// <summary>The prompt as the player wrote it (tokens unexpanded). Saved with the character; the plain "> " is "prompt off".</summary>
    [Saved] private string promptFormat = DefaultPrompt;

    /// <summary>The prompt tokens and what each stands for: the one table.</summary>
    private static readonly (string token, string meaning)[] PromptTokens =
    {
        ("name", "your name"),
        ("vitality", "your current Vitality"), ("maxvitality", "your maximum Vitality"),
        ("endurance", "your current Endurance"), ("maxendurance", "your maximum Endurance"),
        ("focus", "your current Focus"), ("maxfocus", "your maximum Focus"),
        ("rest", "\"(resting) \" while you rest, otherwise nothing"),
        ("sneak", "\"(sneaking) \" while you sneak, otherwise nothing"),
        ("light", "\"(dim) \" in a dim room, \"(dark) \" in a dark one, otherwise nothing"),
    };

    /// <summary>
    /// The old token names, from when the resources were called HP, Stamina and Mana. They still work, so prompts saved before the rename keep
    /// showing their numbers, and they are not listed in `prompt`. OBSOLETE: new prompts use the names in PromptTokens.
    /// </summary>
    private static readonly Dictionary<string, string> OldPromptTokens = new()
    {
        ["hp"] = "vitality", ["maxhp"] = "maxvitality",
        ["stamina"] = "endurance", ["maxstamina"] = "maxendurance",
        ["mana"] = "focus", ["maxmana"] = "maxfocus",
    };

    private static string CurrentToken(string token) => OldPromptTokens.TryGetValue(token, out var now) ? now : token;

    private string PromptValue(string token) => token switch
    {
        "name" => Name,
        "vitality" => Vitality.ToString(),
        "maxvitality" => QueryMaxVitality().ToString(),
        "endurance" => QueryEndurance().ToString(),
        "maxendurance" => QueryMaxEndurance().ToString(),
        "focus" => QueryFocus().ToString(),
        "maxfocus" => QueryMaxFocus().ToString(),
        "rest" => QueryResting() ? "(resting) " : "",
        "sneak" => sneaking ? "(sneaking) " : "",
        "light" => RoomLight() switch { "dim" => "(dim) ", "dark" => "(dark) ", _ => "" },
        _ => "",
    };

    /// <summary>Text typed for a prompt, made safe: no control characters, no {tag} that is not a listed token, no stray braces.</summary>
    private static string CleanPrompt(string raw)
    {
        var text = new StringBuilder();
        for (var i = 0; i < raw.Length; i++)
        {
            var c = raw[i];
            if (char.IsControl(c) || c == '}') continue;
            if (c != '{') { text.Append(c); continue; }
            var end = raw.IndexOf('}', i + 1);
            if (end < 0) continue;
            var inner = raw.Substring(i + 1, end - i - 1);
            if (PromptTokens.Any(t => t.token == inner) || OldPromptTokens.ContainsKey(inner)) text.Append('{').Append(inner).Append('}');
            i = end; // a tag that is not a token (a colour tag, say) is dropped whole
        }
        return text.ToString();
    }

    /// <summary>The prompt with its tokens replaced by the live values. Always cleaned first, whatever is stored.</summary>
    private string ExpandPrompt()
    {
        if (promptFormat == OldDefaultPrompt) promptFormat = DefaultPrompt; // a character saved with the old default gets the new one
        var clean = CleanPrompt(promptFormat);
        if (clean.Trim().Length == 0) return "> ";
        var text = new StringBuilder();
        for (var i = 0; i < clean.Length; i++)
        {
            if (clean[i] != '{') { text.Append(clean[i]); continue; }
            var end = clean.IndexOf('}', i);
            text.Append(PromptValue(CurrentToken(clean.Substring(i + 1, end - i - 1))));
            i = end;
        }
        return text.ToString();
    }

    public override string Prompt()
    {
        GmcpPoll();
        return adventureBegun ? ExpandPrompt() : "> ";
    }

    public override bool PromptsAfterOutput() => adventureBegun;

    /// <summary>"prompt" shows it; "prompt TEXT" sets it; "prompt default" restores the default; "prompt off" is the plain "> ".</summary>
    private bool CmdPrompt(string args)
    {
        var word = args.Trim();
        if (promptFormat == OldDefaultPrompt) promptFormat = DefaultPrompt;
        if (word == "")
        {
            var tokens = string.Join("\n", PromptTokens.Select(t => $"  {{{{{t.token}}}}}  {t.meaning}"));
            Write($"Your prompt is: {Esc(CleanPrompt(promptFormat))}\n  which now reads: {Esc(ExpandPrompt())}\nIt can use these tokens:\n{tokens}\n" +
                  "{cmd}prompt{/} TEXT sets it (up to " + MaxPromptLength + " characters), {cmd}prompt default{/} restores the default, {cmd}prompt off{/} gives the plain '> '.\n");
            return true;
        }
        if (word.ToLowerInvariant() == "default") { promptFormat = DefaultPrompt; Write("Your prompt is back to the default.\n"); return true; }
        if (word.ToLowerInvariant() == "off") { promptFormat = "> "; Write("Your prompt is now the plain '> '.\n"); return true; }
        var clean = CleanPrompt(word).TrimStart();
        if (clean.Trim().Length == 0) { Write("That leaves nothing to show. Type {cmd}prompt{/} to see what you can use.\n"); return true; }
        if (clean.Length > MaxPromptLength) { Write($"Your prompt can be at most {MaxPromptLength} characters.\n"); return true; }
        if (!clean.EndsWith(" ")) clean += " ";
        promptFormat = clean;
        Write($"Your prompt is now: {Esc(clean)}\n");
        return true;
    }

    public override string DamageNote(int applied, bool dealt) =>
        !verbose ? "" : dealt ? $" {{green}}(+{applied} damage){{/}}" : $" {{red}}(-{applied} damage){{/}}";

    public override string HealNote(int healed) => !verbose || healed <= 0 ? "" : $" {{green}}(+{healed} Vitality){{/}}";

    /// <summary>"verbose" shows the setting; "verbose on" and "verbose off" change it.</summary>
    private bool CmdVerbose(string args)
    {
        var word = args.Trim().ToLowerInvariant();
        if (word == "")
        {
            Write(verbose ? "Verbose is on: combat shows the damage you deal {green}(+2 damage){/} and take {red}(-5 damage){/}, and healing shows {green}(+4 Vitality){/}. Type {cmd}verbose off{/} to turn it off.\n"
                : "Verbose is off. Type {cmd}verbose on{/} to see the numbers in combat: the damage you deal and take, and healing.\n");
            return true;
        }
        if (word != "on" && word != "off") { NotifyFail("Usage: {cmd}verbose{/} [{cmd}on{/}|{cmd}off{/}]"); return false; }
        verbose = word == "on";
        Write(verbose ? "Verbose is now on. Combat lines will show the damage done.\n" : "Verbose is now off.\n");
        return true;
    }

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
        if (amount <= 0 || QueryIdle()) return; // nobody earns experience while away from the keyboard
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
            Vitality = QueryMaxVitality();
            Tell($"You have reached level {Level}! You feel stronger and fully healed. Maximum Vitality is now {QueryMaxVitality()}.\n" +
                 $"You have {PointsText(QueryAttributePoints())} to place in your attributes: {{cmd}}improve{{/}} ATTRIBUTE.\n");
            if (Level is 10 or 25 or 50 or 75 or 100) ReportEvent("level.milestone", Level.ToString(), new Dictionary<string, string> { ["level"] = Level.ToString() });
            LearnClassSkills();
        }
    }

    /// <summary>A class gives some skills at a level (docs/specs/bureaucrat.md section 7.1): work out what the classes give now and tell the player of any new skill.</summary>
    private void LearnClassSkills()
    {
        var before = classSkills.ToList();
        RefreshClasses();
        BindSkillCommands();
        foreach (var skill in classSkills.Where(k => !before.Contains(k)))
            Tell($"You have learned {skill.Call<string>("QueryTitle")}: {{cmd}}{skill.Call<string>("QueryCommand")}{{/}}.\n");
    }

    // Signposts to the Adventurers Guild (docs/specs/play-feel.md, section 15): for a mortal who has never taken a quest, up to level 10.
    [Saved] private List<string> tipsShown = new();

    public bool QueryNeedsGuild() => quests.Count == 0 && Level <= 10 && QueryWizLevel() < 1;

    /// <summary>True the first time it is asked for a known tip, for a character who still needs the guild; false after, and for any other id.</summary>
    public bool TakeTip(string id)
    {
        if (id != "stair" || tipsShown.Contains(id) || !QueryNeedsGuild()) return false;
        tipsShown.Add(id);
        return true;
    }

    // QueryQuest and SetQuest are the quest journal's (/std/questjournal).

    public void NotifyKill(string programPath)
    {
        if (!killed.Contains(programPath)) killed.Add(programPath);
    }

    public bool QueryKilled(string programPath) => killed.Contains(programPath);

    // ---- Classes (docs/CLASS_SYSTEM.md sections 6 and 7) ----

    public string QueryArchetype() => archetype;

    /// <summary>True if the character has no calling yet (or its class file is gone) and must choose at login.</summary>
    public bool NeedsArchetype() => !adventureBegun && (archetype == "" || ClassFile(archetype) == null);

    private List<string> RawClassIds() => new[] { archetype, advancedClass, subclass, prestigeClass }.Where(c => c != "").ToList();

    private bool IsHiddenClass(string id) => id != "" && ClassFile(id)?.Call<bool>("QueryHidden") == true;

    /// <summary>
    /// The ids of the character's classes, archetype first. A hidden class is left out when the asker is restricted code (a wizard's
    /// object can never detect one: docs/specs/hidden-classes.md section 2); the game's own code, which asks through the same call,
    /// sees it. Anything shown to another player must go through QueryVisibleClassIds.
    /// </summary>
    public List<string> QueryClassIds()
    {
        var ids = RawClassIds();
        return RestrictedCaller() != null ? ids.Where(c => !IsHiddenClass(c)).ToList() : ids;
    }

    /// <summary>The class ids that this viewer may be told of: all of them for the character themselves and for a level 10 wizard, the unhidden ones for everyone else.</summary>
    public List<string> QueryVisibleClassIds(ObjectRef? viewer)
    {
        var ids = RawClassIds();
        if (viewer == Self || (viewer != null && viewer.IsAlive && viewer.Call<int>("QueryWizLevel") >= 10 && viewer.Domain == "" && RestrictedCaller() == null)) return ids;
        return ids.Where(c => !IsHiddenClass(c)).ToList();
    }

    public bool QueryHasClass(string id) => id != "" && QueryClassIds().Contains(id);

    /// <summary>The class a character has in a tier ("archetype", "advanced", "subclass" or "prestige"), or "".</summary>
    public string QueryClassOfTier(string tier)
    {
        var id = tier switch
        {
            "archetype" => archetype, "advanced" => advancedClass, "subclass" => subclass, "prestige" => prestigeClass, _ => "",
        };
        return id != "" && RestrictedCaller() != null && IsHiddenClass(id) ? "" : id;
    }

    /// <summary>The title of the highest class the character has ("Warrior", later "Knight").</summary>
    public string QueryClassTitle() => RestrictedCaller() != null && IsHiddenClass(advancedClass) ? archetypeName : classTitle;

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
    /// Work out everything the classes give from the class files: starting Vitality, resources, attack rate, experience modifier,
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
        ObjectRef? archetypeClass = null;
        int vb = 10, vp = 8, eb = 50, ep = 2, fb = 50, fp = 2; // the Adventurer's, for an archetype that names none
        foreach (var id in RawClassIds())
        {
            var cls = ClassFile(id);
            if (cls == null || (!cls.Call<bool>("QueryEnabled") && cls.Call<string>("QueryTier") != "archetype")) continue;
            classObjects.Add(cls);
            classTitle = cls.Call<string>("QueryTitle") ?? classTitle;
            expModifier = Math.Max(expModifier, cls.Call<double>("QueryExpModifier"));
            if (cls.Call<double>("QueryAttackRate") > 0) rate = cls.Call<double>("QueryAttackRate");
            // Resource numbers: the archetype's, then any later class that names its own (-1 names none).
            void Take(string query, ref int field) { var v = cls.Call<int>(query); if (v >= 0) field = v; }
            Take("QueryVitalityBase", ref vb); Take("QueryVitalityPerLevel", ref vp);
            Take("QueryEnduranceBase", ref eb); Take("QueryEndurancePerLevel", ref ep);
            Take("QueryFocusBase", ref fb); Take("QueryFocusPerLevel", ref fp);
            foreach (var w in cls.Call<List<string>>("QueryWeapons") ?? new List<string>()) weaponAccess.Add(w);
            foreach (var a in cls.Call<List<string>>("QueryArmorTypes") ?? new List<string>()) armorAccess.Add(a);
            if (cls.Call<bool>("QueryShield")) shieldAccess = true;
            foreach (var sid in cls.Call<List<string>>("QuerySkills") ?? new List<string>())
                if (cls.Call<int>("QuerySkillLevel", sid) <= Level && SkillFile(sid) is { } skill && !classSkills.Contains(skill)) classSkills.Add(skill); // a skill given at a level waits for it
            foreach (var tid in cls.Call<List<string>>("QueryTraits") ?? new List<string>())
                if (SkillFile(tid) is { } trait && !classTraits.Contains(trait)) classTraits.Add(trait);
            if (cls.Call<string>("QueryTier") == "archetype")
            {
                known = true;
                archetypeName = cls.Call<string>("QueryClassName") ?? "";
                BaseReaction = cls.Call<double>("QueryStartReaction");
                archetypeClass = cls;
            }
        }
        unrestricted = !known;
        classRate = rate > 0 ? rate : 1.0;
        if (!known) { vb = 10; vp = 8; eb = 50; ep = 2; fb = 50; fp = 2; }
        vitalityBase = vb; vitalityPerLevel = vp;
        enduranceBase = eb; endurancePerLevel = ep;
        focusBase = fb; focusPerLevel = fp;
        EnsureAttributes(archetypeClass);
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
        // The attributes are the new calling's starting values, with every point of the character's levels left to place.
        attributeValues.Clear();
        RefreshClasses();
        if (pristine)
        {
            Gold = cls.Call<int>("QueryStartGold");
            carried.Clear();
            carriedEquipped.Clear();
            foreach (var path in cls.Call<List<string>>("QueryKit") ?? new List<string>()) carried.Add(path);
            equipKit = true;
        }
        Vitality = QueryMaxVitality();
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
        if (QueryClassOfTier(tier) != "") return $"You already have {(tier == "advanced" ? "an" : "a")} {tier} class.";
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
        var had = classSkills.ToList();
        RefreshClasses();
        CheckLevelUp();
        BindSkillCommands();
        Tell($"You are now a {classTitle}.\n");
        foreach (var skill in classSkills.Where(k => !had.Contains(k) && k.Call<string>("QueryKind") != "trait" && k.Call<string>("QueryCommand") != ""))
            Tell($"You have learned {skill.Call<string>("QueryTitle")}: {{cmd}}{skill.Call<string>("QueryCommand")}{{/}}.\n");
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
        if (skill.Call<string>("QueryId") == "sneak") return CmdSneak(skill, args);
        args = args.Trim();
        var room = Environment();
        ObjectRef? target = null;
        var kind = skill.Call<string>("QueryTarget");
        if (kind == "enemy")
        {
            if (InDark() && args != "")
            {
                if (!UnseenWord(args)) { Write($"You cannot see {WithArticle(args)}.\n"); return true; }
                target = PickUnseen();
                if (target == null) { Write("You can sense nothing to aim at.\n"); return true; }
            }
            else
            target = args == "" ? Enemies().FirstOrDefault(e => Environment(e) == room) : room == null ? null : Present(args, room);
            if (target == null) { Write(args == "" ? $"You are not fighting anyone: name a target, as in '{QueryVerb()} rat'.\n" : $"You see no '{Esc(args)}' here.\n"); return true; }
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
        LeaveRest();
        var opening = false;
        var ambush = false;
        if (kind == "enemy" && target != null)
        {
            if (!QueryFightingWith(target))
            {
                var no = CanFight(target);
                if (no != null) { Write(no + "\n"); return true; }
                opening = !QueryInCombat(); // the shot that starts this character's fight; one aimed at a second creature mid-fight is an ordinary shot
                ambush = opening && sneaking; // the blow that starts the fight from hiding
                OpenFight(target);
            }
            Focus(target);
        }
        if (skill.Call<string>("QueryId") != "tracking") EndSneakLoud(); // every skill but Tracking is loud
        refusal = UseSkill(skill, target, opening, ambush);
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

    /// <summary>
    /// What "help NAME" says for a class this character holds (not the calling) or for a skill of a hidden class they hold; null for any
    /// other word, so that a character without the class gets the answer given to any word the game does not know.
    /// </summary>
    protected string? HeldHelp(string topic)
    {
        topic = (topic ?? "").Trim().ToLowerInvariant();
        if (topic == "") return null;
        foreach (var id in RawClassIds())
        {
            var cls = ClassFile(id);
            if (cls == null || cls.Call<string>("QueryTier") == "archetype") continue;
            var title = (cls.Call<string>("QueryTitle") ?? "").ToLowerInvariant();
            if (topic == id || topic == title || topic == (cls.Call<string>("QueryClassName") ?? "").ToLowerInvariant())
            {
                var text = new StringBuilder($"{cls.Call<string>("QueryTitle")}\n{cls.Call<string>("QueryDescription")}\n");
                foreach (var sid in cls.Call<List<string>>("QuerySkills") ?? new List<string>())
                    if (cls.Call<int>("QuerySkillLevel", sid) <= Level && SkillFile(sid) is { } have) text.Append("  " + SkillLine(have) + "\n");
                foreach (var sid in cls.Call<List<string>>("QuerySkills") ?? new List<string>())
                    if (cls.Call<int>("QuerySkillLevel", sid) > Level && SkillFile(sid) is { } later)
                        text.Append($"  {later.Call<string>("QueryTitle")} (at level {cls.Call<int>("QuerySkillLevel", sid)})\n");
                return text.ToString();
            }
            if (!cls.Call<bool>("QueryHidden")) continue;
            foreach (var sid in cls.Call<List<string>>("QuerySkills") ?? new List<string>())
            {
                if (cls.Call<int>("QuerySkillLevel", sid) > Level || SkillFile(sid) is not { } skill) continue;
                if (topic == (skill.Call<string>("QueryCommand") ?? "").ToLowerInvariant() || topic == (skill.Call<string>("QueryTitle") ?? "").ToLowerInvariant())
                    return skill.Call<string>("QueryHelpText") is { Length: > 0 } help ? help + "\n" : SkillLine(skill) + "\n";
            }
        }
        return null;
    }

    private string SkillLine(ObjectRef skill)
    {
        var delay = (QueryReaction() + skill.Call<double>("QueryReactionMod") + skill.Call<double>("QueryRecovery")) * 2;
        var id = skill.Call<string>("QueryId") ?? "";
        var rank = QuerySkillRank(id);
        var line = skill.Call<string>("LineAt", delay, Math.Max(1, rank)) ?? "";
        if (QuerySkillWaitLeft(id) is > 0 and var waitLeft) line += $" (you may cast it again in {waitLeft} second{(waitLeft == 1 ? "" : "s")})";
        if (!skill.Call<bool>("QueryHasRanks") || rank < 1) return line;
        var next = rank + 1;
        if (!skill.Call<bool>("QueryRankExists", next)) return line + " (the highest rank)";
        var level = Skill.LevelForRank(next);
        return line + (level > Level ? $" (rank {Skill.Roman(next)} at level {level})" : $" (rank {Skill.Roman(next)} can be learned from a trainer)");
    }

    /// <summary>Why a skill cannot be paid for or used now, or null. Checked before a fight is started for it.</summary>
    private string? SkillRefusal(ObjectRef skill)
    {
        var isSpell = skill.Call<string>("QueryKind") == "spell";
        if (isSpell && QueryMod("nospell") > 0) return "You cannot cast spells like this.";
        var focus = skill.Call<int>("QueryFocusCostFor", Self);
        if (isSpell && focus > 0) focus = Math.Max(1, (int)Math.Round(focus * (1 + QueryMod("spell_cost_pct") / 100.0), MidpointRounding.AwayFromZero));
        if (QueryEndurance() < skill.Call<int>("QueryEnduranceCostFor", Self)) return "You are too tired for that.";
        if (QueryFocus() < focus) return "You do not have the focus for that.";
        return null;
    }

    // ---- Skill ranks, unlocks and the counters they read (docs/specs/skill-ranks.md) ----
    // Saved with the character. Only the rank daemon, /secure/ranks, may change any of it: it does so when a trainer sells a rank to the
    // player who asked (the player's own `train`), and when a creature from a trusted tree dies. There is no other setter, so no other
    // object (a wizard's, say) can grant a rank, an unlock or a kill. A save made before ranks existed has none of it: rank I everywhere.

    private const string RankDaemon = "/secure/ranks";
    private const int MaxRankRecords = 500;
    private const int MaxCounterValue = 100_000_000;

    /// <summary>Skill id to the rank held, for ranks above I only (absent is rank I).</summary>
    [Saved] private Dictionary<string, int> skillRanks = new();
    /// <summary>The offers whose unlock requirements this character has met at a trainer, for good: "trainer path|skill id|rank".</summary>
    [Saved] private List<string> rankUnlocks = new();
    /// <summary>Creatures killed (a share of the kill), by creature kind ("goblin"). Kept from creation; read by unlocks.</summary>
    [Saved] private Dictionary<string, int> killsByKind = new();
    /// <summary>Killing blows struck with a skill, by skill id ("arcane_bolt").</summary>
    [Saved] private Dictionary<string, int> killsWithSkill = new();

    private bool FromRankDaemon()
    {
        var caller = PreviousObject();
        return caller != null && !caller.IsClone && caller.ProgramPath == RankDaemon;
    }

    /// <summary>Does the character have this skill or spell now (from a class, or from what they wear or wield)?</summary>
    public bool QueryHasSkill(string id) =>
        !string.IsNullOrEmpty(id) && classSkills.Concat(GrantedSkills()).Any(s => s.Call<string>("QueryId") == id);

    /// <summary>The rank held in a skill: 0 if the character does not have it, 1 for rank I, more for a rank bought from a trainer.</summary>
    public int QuerySkillRank(string id)
    {
        if (!QueryHasSkill(id)) return 0;
        return Math.Max(1, skillRanks.GetValueOrDefault(id ?? ""));
    }

    /// <summary>Every rank above I, by skill id (a copy).</summary>
    public Dictionary<string, int> QuerySkillRanks() => new(skillRanks);

    public bool QueryRankUnlocked(string key) => rankUnlocks.Contains(key ?? "");

    public List<string> QueryRankUnlocks() => new(rankUnlocks);

    public int QueryKillsOfKind(string kind) => killsByKind.GetValueOrDefault(kind ?? "");

    public int QueryKillsWith(string skillId) => killsWithSkill.GetValueOrDefault(skillId ?? "");

    public Dictionary<string, int> QueryKillCounts() => new(killsByKind);

    public Dictionary<string, int> QueryKillsWithCounts() => new(killsWithSkill);

    private static bool RankKey(string key) => !string.IsNullOrEmpty(key) && key.Length <= 160;

    /// <summary>Set the rank of a skill (1 removes the record). Only the rank daemon may.</summary>
    public bool RankSetSkill(string id, int rank)
    {
        if (!FromRankDaemon() || !RankKey(id) || id.Length > 40 || rank < 1 || rank > 100) return false;
        if (rank == 1) { skillRanks.Remove(id); return true; }
        if (!skillRanks.ContainsKey(id) && skillRanks.Count >= MaxRankRecords) return false;
        skillRanks[id] = rank;
        return true;
    }

    /// <summary>Record an unlock for good. Only the rank daemon may.</summary>
    public bool RankRecordUnlock(string key)
    {
        if (!FromRankDaemon() || !RankKey(key) || rankUnlocks.Contains(key) || rankUnlocks.Count >= MaxRankRecords) return false;
        rankUnlocks.Add(key);
        return true;
    }

    /// <summary>One more kill of a creature kind. Only the rank daemon may, for a death in the game.</summary>
    public bool RankCountKill(string kind)
    {
        if (!FromRankDaemon() || !RankKey(kind) || kind.Length > 40) return false;
        if (!killsByKind.ContainsKey(kind) && killsByKind.Count >= MaxRankRecords) return false;
        killsByKind[kind] = Math.Min(MaxCounterValue, killsByKind.GetValueOrDefault(kind) + 1);
        return true;
    }

    /// <summary>One more killing blow struck with a skill. Only the rank daemon may.</summary>
    public bool RankCountKillWith(string skillId)
    {
        if (!FromRankDaemon() || !RankKey(skillId) || skillId.Length > 40) return false;
        if (!killsWithSkill.ContainsKey(skillId) && killsWithSkill.Count >= MaxRankRecords) return false;
        killsWithSkill[skillId] = Math.Min(MaxCounterValue, killsWithSkill.GetValueOrDefault(skillId) + 1);
        return true;
    }

    /// <summary>Take gold for a rank. Only the rank daemon may; false if the character has too little.</summary>
    public bool RankPay(int amount)
    {
        if (!FromRankDaemon() || amount < 0 || Gold < amount) return false;
        Gold -= amount;
        return true;
    }

    /// <summary>Write the character to disk after a rank is bought. Only the rank daemon may ask.</summary>
    public void RankSave()
    {
        if (FromRankDaemon()) PersistNow();
    }

    // ---- Equipment access (docs/CLASS_SYSTEM.md section 5) ----

    /// <summary>The calling and class ids of this character: the calling first, then the advanced class, subclass and prestige class. An advanced class counts as the calling it grew from because the calling is always in the list.</summary>
    private List<string> CallingIds() => new[] { archetype, advancedClass, subclass, prestigeClass }.Where(i => i != "").ToList();

    /// <summary>
    /// Why this character's calling bars the piece (its WornBy and NotWornBy), in words, or null. Asking changes nothing; the shop
    /// asks it before taking gold, and wear asks it before anything else.
    /// </summary>
    public string? QueryCallingRefusal(ObjectRef item)
    {
        if (unrestricted || item == null || !item.IsAlive || item.Call<bool>("QueryAllowsCalling", CallingIds())) return null;
        var verb = item.Call<string>("QuerySlot") == "weapon" ? "wield" : "wear";
        return $"You cannot {verb} {item.Call<string>("Short")}. {item.Call<string>("QueryCallingNote")}";
    }

    /// <summary>
    /// Why this character may not wield or wear the item, or null. An item without a category is for anyone. Fresh from a save
    /// (restoring) a piece already worn stays worn whatever its calling rule says, until it is taken off.
    /// </summary>
    private string? AccessRefusal(ObjectRef item, bool restoring = false)
    {
        if (unrestricted) return null;
        if (!restoring && QueryCallingRefusal(item) is { } barred) return barred;
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
    protected void PrepareSave(ObjectRef? at = null)
    {
        if (!inventoryRestored) return;
        carried.Clear();
        carriedEquipped.Clear();
        foreach (var item in AllInventory())
            if (item.Domain == "" && item.IsAlive && item.Call<bool>("IsLightSource")) item.Call("SyncLight"); // the time left goes into the record (a lit light is out at the next login)
        foreach (var item in AllInventory())
        {
            if (item.Domain != "") continue; // a sandbox thing is never a record of the real game (docs/SANDBOX.md)
            carried.Add(RecordOf(item));
            if (IsEquipped(item)) carriedEquipped.Add(carried.Count - 1);
        }
        // Where the next login begins (docs/specs/play-feel.md, "Where a login begins"): the room they stand in, or for the workshop
        // checkpoint the room they entered it from. Never sandbox space (docs/SANDBOX.md): a room with a domain, the Void, a clone.
        var room = at ?? Environment();
        logoutRoom = room != null && room.Domain == "" && WakeRoomOk(room.Name) ? room.Name : "";
    }

    /// <summary>Remove the carried items from the game, as the player leaves. They return at the next login.</summary>
    protected void ReleaseInventory()
    {
        // A sandbox thing is destructed without running any of its code (the driver runs no OnDestruct of sandbox code for real
        // code), so it can neither stop nor delay a quit or a link death; a real thing vanishes as it always has.
        foreach (var item in AllInventory())
        {
            try
            {
                if (!item.IsAlive) continue;
                if (item.Domain != "") Destruct(item);
                else item.Call("Vanish");
            }
            catch (MudRuntimeException) { if (item.IsAlive) Destruct(item); }
        }
    }

    // ---- Visibility (docs/specs/who-and-visibility.md) ----

    /// <summary>True for a wizard who has made themselves invisible (/std/player keeps it). Nothing outside this body can read it: /secure/roster holds the set the others ask.</summary>
    protected virtual bool WizUnseen() => false;

    /// <summary>
    /// A line to the others in the room. From an invisible wizard, the ones who cannot see them read the same line with the wizard's name
    /// replaced by "Someone" (as in the dark); movement, login and logout are not said at all (their own code decides that).
    /// </summary>
    protected new void Say(string message)
    {
        if (!WizUnseen()) { base.Say(message); return; }
        var room = Environment();
        if (room == null) return;
        var roster = LoadObject("/secure/roster");
        var unnamed = message.Replace(Name, "Someone");
        foreach (var other in AllInventory(room))
            if (other.IsAlive && other != Self && Living(other)) TellObject(other, roster.Call<bool>("Visible", other, Self) ? message : unnamed);
    }

    /// <summary>A line about this character arriving, leaving or logging in or out: told to the room, and for an invisible wizard only to those who can see them.</summary>
    protected void SayToSeers(string message)
    {
        if (!WizUnseen()) { base.Say(message); return; }
        var room = Environment();
        if (room == null) return;
        var roster = LoadObject("/secure/roster");
        foreach (var other in AllInventory(room))
            if (other.IsAlive && other != Self && Living(other) && roster.Call<bool>("Visible", other, Self)) TellObject(other, message);
    }

    /// <summary>True while a worn item hides its wearer from the who list (an item's `HidesFromWho`). It does nothing else.</summary>
    public bool QueryHiddenFromWho() => QueryEquipment().Any(item => item.IsAlive && item.Call<bool>("QueryHidesFromWho"));

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
        AddAction("rest", CmdRest);
        AddAction("sit", CmdRest);
        AddAction("stand", CmdStand);
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
        AddAction("stats", CmdStats);
        AddAction("vitality", CmdVitality);
        AddAction("hp", CmdVitality); // OBSOLETE name of the command, kept as an alias; help shows vitality
        AddAction("verbose", CmdVerbose);
        AddAction("prompt", CmdPrompt);
        AddAction("skills", CmdSkills);
        AddAction("improve", CmdImprove);
        AddAction("quests", CmdQuests);
        BindJournalCommands(); // quest NAME, abandon NAME
        AddAction("search", CmdSearch);
        AddAction("light", CmdLight);
        AddAction("ignite", CmdLight);
        AddAction("extinguish", CmdExtinguish);
        AddAction("douse", CmdExtinguish);
        AddAction("snuff", CmdExtinguish);
        AddAction("put", CmdPut);
        AddAction("fill", CmdFill);
        AddAction("refill", CmdFill);
        BindCraftCommands(); // crafting and gathering, below
        // Builder tools. Each handler checks the wizard level on every use and
        // declines below it, so to everyone else these verbs do not exist.
        AddAction("goto", CmdGoto);
        AddAction("stat", CmdStat);
        AddAction("restore", CmdRestore);
        AddAction("slay", CmdSlay);
        AddAction("speed", CmdSpeed);
        AddAction("setlevel", CmdSetLevel);
        AddAction("hidden", CmdHidden);

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
                if (put && AccessRefusal(item, restoring: true) == null) Equip(item);
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

    /// <summary>
    /// A room a character may wake in: not the Void, not a clone, and never under /wiz, a wizard's own home included: their home is
    /// a workshop, entered only through the master's move with its checkpoint (docs/SANDBOX.md), so a wizard who quits there wakes in
    /// the room they entered it from.
    /// </summary>
    private static bool WakeRoomOk(string path) =>
        path != "" && path != "/std/void" && !path.Contains('#') && !path.ToLowerInvariant().StartsWith("/wiz/") && path.ToLowerInvariant() != "/wiz";

    /// <summary>Move the player into the world: the saved logout room, if there is one, else the start room.</summary>
    protected void EnterWorld(string startRoom)
    {
        // Never into sandbox space: a body enters a workshop only through the master's move, which takes the checkpoint first.
        var destination = logoutRoom != "" && WakeRoomOk(logoutRoom) ? logoutRoom : startRoom;
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
        if (adventureBegun && GmcpActive(Self)) LoadObject("/secure/gmcp").Call("Look", Self); // Room.Info, whether or not it changed
    }

    // ---- GMCP (docs/specs/gmcp.md) ----
    // The service /secure/gmcp decides what is sent; this body gives it the facts about this character, and only to it, and asks it
    // after every prompt and every heart beat whether anything changed. A client without GMCP costs one efun call.

    private void GmcpPoll()
    {
        JournalPoll(adventureBegun); // the quest journal's GMCP (Char.Quests.*) and its objective notes
        if (adventureBegun && GmcpActive(Self)) LoadObject("/secure/gmcp").Call("Poll", Self);
    }

    /// <summary>What Char.Vitals and Char.Status are made of. Only /secure/gmcp may ask; anyone else gets null.</summary>
    public Dictionary<string, object?>? GmcpFacts()
    {
        var caller = PreviousObject();
        if (caller == null || caller.IsClone || caller.ProgramPath != "/secure/gmcp") return null;
        return new Dictionary<string, object?>
        {
            ["vitality"] = Vitality, ["maxvitality"] = QueryMaxVitality(), ["endurance"] = QueryEndurance(), ["maxendurance"] = QueryMaxEndurance(),
            ["focus"] = QueryFocus(), ["maxfocus"] = QueryMaxFocus(), ["resting"] = QueryResting(), ["sneaking"] = sneaking, ["fighting"] = QueryInCombat(),
            ["name"] = Name, ["calling"] = archetypeName, ["level"] = Level, ["experience"] = xp,
            ["next_level"] = Level >= MaxLevel ? 0 : QueryXpForLevel(Level + 1), ["gold"] = Gold, ["points_to_place"] = QueryAttributePoints(),
        };
    }

    protected override bool NeedsHeartBeat() => true;

    /// <summary>Called on every heart beat. A player overrides it to watch for idleness; other characters have none.</summary>
    protected virtual void IdleCheck() { }

    protected override void Tick()
    {
        IdleCheck();
        if (Self.IsAlive) GmcpPoll(); // regeneration shows in the gauges with no command typed
        if (Dead || !Self.IsAlive) return;
        if (resting) RestBeat();
        if (++regenBeats < RegenBeats) return;
        regenBeats = 0;
        var seated = restBeats; // beats of this step spent resting: a step half rested gets half the bonus
        restBeats = 0;
        if (QueryInCombat() || Vitality >= QueryMaxVitality()) { vitalityCarry = 0; return; }
        var room = Environment();
        var restBoost = 1 + (RestFactor - 1) * seated / RegenBeats;
        vitalityCarry += QueryVitalityRegen() * (room != null && room.Call<bool>("QuerySafe") ? 3 : 1) * (1 + QueryMod("regen_out")) * restBoost;
        var whole = (int)Math.Floor(vitalityCarry + 1e-9);
        vitalityCarry -= whole;
        if (whole > 0) Heal(whole);
    }

    // ---- Resting (docs/specs/rest.md) ----
    // `rest` sits the character down: Vitality, Endurance and Focus return RestFactor times as fast. Only this character's own
    // commands start or end it, and nothing public changes it: a probe object can ask QueryResting and nothing else. Whatever ends
    // it by itself (a fight, a different room, death) is noticed by RestEnded wherever the state is looked at, and the Endurance and
    // Focus regained up to then are settled at the seated rate first.

    public override bool QueryResting()
    {
        if (resting && RestEnded()) EndRest();
        return resting;
    }

    protected override double RegenStateFactor() => resting ? RestFactor : 1.0;

    protected override void BeforeSettle()
    {
        if (resting && RestEnded()) EndRest();
    }

    private bool RestEnded() => Dead || QueryInCombat() || Environment()?.Name != restRoom;

    /// <summary>Stand up, quietly: the resources are settled at the seated rate up to this moment.</summary>
    private void EndRest()
    {
        if (!resting) return;
        SettleCore();
        resting = false;
        restBeats = 0;
    }

    /// <summary>Whatever the character does that is not looking or talking gets them up.</summary>
    protected void LeaveRest()
    {
        if (resting) EndRest();
    }

    /// <summary>Once per heartbeat while resting: keep the resources current and say once when everything is full.</summary>
    private void RestBeat()
    {
        if (RestEnded()) { EndRest(); return; }
        restBeats++;
        SettleCore();
        if (restedTold || Vitality < QueryMaxVitality() || QueryEndurance() < QueryMaxEndurance() || QueryFocus() < QueryMaxFocus()) return;
        restedTold = true;
        Tell("You are fully rested.\n");
    }

    private bool CmdRest(string args)
    {
        var room = Environment();
        if (QueryInCombat()) { Write(FightingNote.Replace("do that", "rest") + "\n"); return true; }
        if (QueryResting()) { Write("You are already resting.\n"); return true; }
        if (room == null || Dead) return true;
        SettleCore(); // what was regained so far was regained standing
        resting = true;
        restRoom = room.Name;
        restedTold = false;
        restBeats = 0;
        Write("You sit down to rest.\n");
        Say($"{Name} sits down to rest.\n");
        return true;
    }

    private bool CmdStand(string args)
    {
        if (!QueryResting()) { Write("You are already on your feet.\n"); return true; }
        EndRest();
        Write("You stand up.\n");
        Say($"{Name} stands up.\n");
        return true;
    }

    /// <summary>A creature that has just attacked a seated character: they scramble up and lose RestSurprise seconds more. Only a creature's own attack counts.</summary>
    protected override void Ambushed(ObjectRef enemy)
    {
        if (sneaking) { sneaking = false; Tell("You are no longer sneaking.\n"); }
        if (AbandonPreparation("search"))
        {
            searchRoom = null;
            Tell($"You break off your search as {enemy.Call<string>("QueryLabel") ?? "something"} attacks!\n");
        }
        if (!resting) return;
        var attacker = PreviousObject();
        if (RestEnded() || attacker != enemy || !enemy.Call<bool>("IsCreature") || Environment(enemy) != Environment()) { if (RestEnded()) EndRest(); return; }
        EndRest();
        DelayActionBy(RestSurprise);
        Tell("You scramble to your feet!\n");
        TellRoom(Environment()!, $"{Name} scrambles to {QueryPossessive()} feet!\n", Self);
    }

    /// <summary>
    /// A shop or a find tells a seated character that what they just did there (buy, sell, get, take, loot) gets them up. It
    /// works only for the character's own command, with that very verb, from an object standing in their room; anything else
    /// is ignored, so no other object can end anyone's rest with it.
    /// </summary>
    public void RestDisturbed(string verb)
    {
        var caller = PreviousObject();
        if (!resting || caller == null || caller == Self || Self != ThisPlayer() || Environment(caller) != Environment()) return;
        if (verb != QueryVerb() || verb is not ("buy" or "sell" or "get" or "take" or "loot")) return;
        if (caller.Call<bool>("IsPlayerCharacter")) return;
        EndRest();
    }

    // ---- Sneaking (docs/specs/sneak.md) ----
    // Not saved. The state changes only through the character's own commands and the game's trusted paths: a creature's own notice roll
    // (SneakNotice), the room the character leaves (SneakDeparts), a trusted object in the room reporting a loud deed (Disturbed). The
    // numbers and the dice are in /secure/sneak, the numbers by rank in /skills/sneak.

    private const string SneakDaemon = "/secure/sneak";
    private static readonly string[] SneakTrusted = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    private bool sneaking;
    /// <summary>Creatures in the current room that have seen this character, while they live and the character stays.</summary>
    private readonly List<ObjectRef> spotters = new();
    /// <summary>Aggressive creatures in the current room that have not noticed this character yet.</summary>
    private readonly List<ObjectRef> unaware = new();
    private ObjectRef? sneakRoom;
    private bool sneakBeatOn;

    public bool QuerySneaking() => sneaking;

    protected override bool OpeningUnseen() => sneaking;

    private static bool TrustedCaller(ObjectRef? caller) =>
        caller != null && SneakTrusted.Any(t => caller.ProgramPath.StartsWith(t, StringComparison.Ordinal));

    /// <summary>The lists belong to one room: a new room starts them empty.</summary>
    private void SyncSneakRoom()
    {
        var room = Environment();
        if (room != sneakRoom) { sneakRoom = room; spotters.Clear(); unaware.Clear(); }
        spotters.RemoveAll(c => !c.IsAlive || Environment(c) != room);
        unaware.RemoveAll(c => !c.IsAlive || Environment(c) != room);
    }

    private bool NoticeRoll(ObjectRef creature, bool half)
    {
        var daemon = LoadObject(SneakDaemon);
        var skill = LoadObject("/skills/sneak");
        var adjust = skill.Call<int>("QueryNoticeAt", Math.Max(1, QuerySkillRank("sneak")));
        var lightAdjust = Environment() is { } here ? LoadObject("/secure/light").Call<int>("NoticeAdjust", here, Self, creature) : 0;
        var chance = daemon.Call<int>("NoticeChance", QueryAttribute("grace"), Level, creature.Call<int>("QueryLevel"), creature.Call<bool>("HasTag", "alert"), adjust, lightAdjust);
        return daemon.Call<bool>("Notices", chance, half);
    }

    /// <summary>A creature has seen the character: they stop sneaking, and it is the creature's to attack.</summary>
    private void Spotted(ObjectRef creature)
    {
        unaware.Remove(creature);
        if (!spotters.Contains(creature)) spotters.Add(creature);
        sneaking = false;
        var noticer = Cap(creature.Call<string>("QueryLabel") ?? "it");
        CallOut(() => { if (!Dead) Tell($"{noticer} has noticed you!\n"); }, 0); // after the arrival, so the room is described first
        creature.Call("Engage");
    }

    /// <summary>
    /// A creature that has come within reach of a sneaking character asks whether it notices them. Only the creature itself may ask, from a
    /// trusted tree, while it is in the character's room: 1 it notices (a Boss always does), 0 it does not (and may roll again every ten
    /// seconds, at half the chance), -1 it has no standing to ask (the creature then acts as it always did).
    /// </summary>
    public int SneakNotice(ObjectRef creature)
    {
        var caller = PreviousObject();
        if (caller == null || caller != creature || !TrustedCaller(caller) || !creature.IsAlive || !creature.Call<bool>("IsMonster") ||
            !creature.Call<bool>("QueryAggressive") || Dead || !Interactive(Self) || Environment(creature) != Environment() || Environment() == null) return -1;
        if (!sneaking) return -1;
        SyncSneakRoom();
        if (spotters.Contains(creature)) return 1;
        if (creature.Call<bool>("HasTag", "boss") || NoticeRoll(creature, false)) { Spotted(creature); return 1; }
        if (!unaware.Contains(creature)) unaware.Add(creature);
        ArmSneakBeat();
        CallOut(() => { if (!Dead && unaware.Contains(creature)) Tell($"{Cap(creature.Call<string>("QueryLabel") ?? "it")} has not noticed you.\n"); }, 0);
        return 0;
    }

    private void ArmSneakBeat()
    {
        if (sneakBeatOn) return;
        sneakBeatOn = true;
        CallOut(SneakBeat, 10); // seconds between the re-rolls (/secure/sneak)
    }

    /// <summary>Every ten seconds, each creature that has not noticed the character rolls again, at half the chance.</summary>
    private void SneakBeat()
    {
        sneakBeatOn = false;
        if (Dead) return;
        SyncSneakRoom();
        foreach (var creature in unaware.ToList())
            if (creature.IsAlive && NoticeRoll(creature, true)) Spotted(creature);
        if (unaware.Count > 0) ArmSneakBeat();
    }

    /// <summary>
    /// The room the character is in asks leave for them to go to another: each creature here that has not noticed them rolls at the entry
    /// chance, and one that notices starts the fight and the character stays (false). Otherwise the room is paid for in Endurance (false
    /// if they are too tired: sneaking ends and they go openly, true). Only the character's own room, from a trusted tree, may ask.
    /// </summary>
    public bool SneakDeparts(ObjectRef destination)
    {
        var caller = PreviousObject();
        if (caller == null || caller != Environment() || !TrustedCaller(caller) || !sneaking || Dead) return true;
        SyncSneakRoom();
        var stays = false;
        foreach (var creature in unaware.ToList())
            if (creature.IsAlive && NoticeRoll(creature, false)) { Spotted(creature); stays = true; }
        if (stays) return false;
        var cost = LoadObject("/skills/sneak").Call<int>("QueryRoomCostAt", Math.Max(1, QuerySkillRank("sneak")));
        if (!SpendResources(cost, 0))
        {
            sneaking = false;
            Tell("You are too tired to keep sneaking, and go on openly.\n");
        }
        return true;
    }

    /// <summary>The character did something loud: sneaking ends, and every creature that had not noticed them now has.</summary>
    protected void EndSneakLoud()
    {
        SyncSneakRoom();
        var was = sneaking;
        sneaking = false;
        var woken = unaware.ToList();
        unaware.Clear();
        foreach (var creature in woken)
        {
            if (!spotters.Contains(creature)) spotters.Add(creature);
            creature.Call("Engage");
        }
        if (was) Tell("You are no longer sneaking.\n");
    }

    /// <summary>
    /// A trusted object in the character's room (a door, a chest) says that what they just did there, with this very verb, was loud. Only
    /// for the character's own command; no other object can end anyone's sneaking with it.
    /// </summary>
    public void Disturbed(string verb)
    {
        var caller = PreviousObject();
        if (caller == null || caller == Self || Self != ThisPlayer() || !TrustedCaller(caller) || Environment() == null) return;
        if (caller != Environment() && Environment(caller) != Environment()) return;
        if (verb != QueryVerb() || verb is "look" or "l" or "rest" or "tell" or "track" or "sneak") return;
        EndSneakLoud();
    }

    private bool CmdSneak(ObjectRef skill, string args)
    {
        var rank = Math.Max(1, QuerySkillRank("sneak"));
        args = args.Trim().ToLowerInvariant();
        var turnOn = args == "on" || (args == "" && !sneaking);
        if (args != "" && args != "on" && args != "off") { Write("Sneak on, or sneak off.\n"); return true; }
        if (!turnOn)
        {
            if (!sneaking) { Write("You are not sneaking.\n"); return true; }
            sneaking = false;
            Write("You stop sneaking.\n");
            return true;
        }
        if (sneaking) { Write("You are already sneaking.\n"); return true; }
        SyncSneakRoom();
        if (spotters.FirstOrDefault(c => c.IsAlive) is { } seen)
        {
            Write($"{Cap(seen.Call<string>("QueryLabel") ?? "it")} has seen you; there is no hiding from it here.\n");
            return true;
        }
        if (QueryInCombat()) { Write("You cannot slip out of sight while you are fighting.\n"); return true; }
        LeaveRest();
        sneaking = true;
        var cost = skill.Call<int>("QueryRoomCostAt", rank);
        Write($"You begin to move quietly. Each room you enter costs {cost} Endurance.\n");
        return true;
    }

    // ---- Death ----

    /// <summary>
    /// A player does not stay dead: a corpse in the room where they fell takes their
    /// belongings and a tenth of their gold, they forget
    /// their enemies, and wake in the Guest Rooms at full Vitality.
    /// </summary>
    public override void Die(ObjectRef? killer)
    {
        if (DiesInWorkshop(killer)) return;
        if (dying) return;
        dying = true;
        EndRest();
        sneaking = false;
        unaware.Clear();
        spotters.Clear();
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
            Vitality = QueryMaxVitality();
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
        if (linkDead) LinkDeadDied(); // a dropped player's body that died: it is saved (above) and goes at once
    }

    // ---- A dropped connection (docs/specs/play-feel.md section 14): the body lingers; /std/player holds the rest ----

    /// <summary>True while this body has lost its connection and is still in the world.</summary>
    protected bool linkDead;

    public bool QueryLinkDead() => linkDead;

    protected virtual void LinkDeadDied() { }

    /// <summary>Write the character to disk now. Players override this.</summary>
    protected virtual void PersistNow() { }

    /// <summary>A player in a wizard's workshop dies without a corpse and is restored (docs/SANDBOX.md); true if that is what happened.</summary>
    protected virtual bool DiesInWorkshop(ObjectRef? killer) => false;

    /// <summary>What score adds after the name while the character is in a wizard's workshop. Players override it.</summary>
    protected virtual string WorkshopNote() => "";

    /// <summary>Put everything carried (equipment included) and the fee into a corpse in the room.</summary>
    private void LeaveCorpse(ObjectRef? room, ObjectRef? killer, int fee)
    {
        if (room == null) return;
        var corpse = CloneObject("/std/corpse");
        MoveObject(corpse, room);
        foreach (var slot in new[] { "weapon", "shield", "body", "head", "trinket" }) Unequip(slot);
        foreach (var item in AllInventory()) if (!item.Call<bool>("QueryBound")) MoveObject(item, corpse); // a bound item stays with its holder
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
        if (InDark())
        {
            Write(Present(args) != null ? "You feel it over, but cannot see it properly.\n" : "It is too dark to see that.\n");
            return true;
        }
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
        if (InDark()) { Write("It is too dark to see what is here.\n"); return true; }
        if (QueryInCombat()) { Write(FightingNote.Replace("do that", "pick things up") + "\n"); return true; }
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
            if (all && noun == "") { Write(room != null && FindsByName(room) ? "Nothing here can be taken at a sweep; something must be taken by name.\n" : "There is nothing here to take.\n"); return true; }
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
        if (all && noun == "" && FindsByName(room)) Write("Something here must be taken by name.\n"); // a find is searched, once, by its name: never by "all"
        return true;
    }

    /// <summary>Is there a find here that this character may still take from? ("get all" leaves finds alone and says so.)</summary>
    private bool FindsByName(ObjectRef room) => AllInventory(room).Any(o => o.IsAlive && o.Call<bool>("QueryTakeByName"));

    /// <summary>Pick up n of an item lying in the room, with the messages.</summary>
    private void Pick(ObjectRef item, int n)
    {
        var description = Describe(item, n);
        var held = MovePortion(item, n, Self);
        if (held == null) { Write($"You cannot take {description}.\n"); return; }
        LeaveRest();
        EndSneakLoud();
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
        return CorpsesHere(room, name).FirstOrDefault();
    }

    /// <summary>The corpses in the room that answer to a name, in listed order; with a position in the name ("corpse 2", "2.corpse") just that one.</summary>
    private List<ObjectRef> CorpsesHere(ObjectRef room, string name)
    {
        var id = name.ToLowerInvariant();
        var corpses = AllInventory(room).Where(o => o.IsAlive && o.Call<bool>("IsCorpse")).ToList();
        var named = corpses.Where(o => id == "" || o.Call<bool>("Id", id)).ToList();
        if (named.Count > 0 || id == "") return named;
        var position = SplitOrdinal(id, out var bare);
        if (position == 0) return named;
        var same = corpses.Where(o => o.Call<bool>("Id", bare)).ToList();
        return position <= same.Count ? new List<ObjectRef> { same[position - 1] } : named;
    }

    private bool GetFromCorpse(string what, string corpseName)
    {
        var corpse = CorpseHere(corpseName);
        if (corpse == null || what == "") { NotifyFail("Get what from what?"); return false; }
        var refusal = what == "all" ? corpse.Call<string>("TakeAll") : corpse.Call<string>("Take", what);
        if (refusal != null) Write(refusal + "\n");
        else { LeaveRest(); EndSneakLoud(); }
        return true;
    }

    private bool CmdLoot(string args)
    {
        if (InDark()) { Write("It is too dark to see what is here.\n"); return true; }
        var room = Environment();
        if (room == null) return false;
        if (QueryInCombat()) { Write(FightingNote.Replace("do that", "pick things up") + "\n"); return true; }
        string? refusal = null;
        var found = false;
        foreach (var ob in CorpsesHere(room, args))
        {
            found = true;
            var result = ob.Call<string>("TakeAll");
            if (result == null) { LeaveRest(); EndSneakLoud(); return true; }
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
            if (item.Call<bool>("QueryBound")) { Write(item.Call<string>("QueryBoundLine") + "\n"); continue; }
            if (MovePortion(item, n, room) == null)
            {
                // The one rule of a wizard's workshop that a drop can meet (docs/SANDBOX.md).
                Write(room.Domain != "" && item.Domain == "" ? "Real things cannot be set down in the workshop; they leave with you.\n" : $"You cannot drop {description}.\n");
                continue;
            }
            LeaveRest();
            Write($"You drop {description}.\n");
            Say(InDark() ? "Someone drops something.\n" : $"{Name} drops {description}.\n");
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
        if (receiver.Call<bool>("QueryLinkDead")) { Write($"{receiver.Call<string>("QueryName")} stares at nothing and does not notice you.\n"); return true; }
        if (!receiver.Call<bool>("IsPlayerCharacter") || !Interactive(receiver))
        {
            Write($"{Cap(receiver.Call<string>("QueryLabel") ?? receiver.Call<string>("Short") ?? "It")} does not take things from you. You can give things only to other players.\n");
            return true;
        }
        if (!ParseAmount(args.Substring(0, to), out var count, out var all, out var noun)) { NotifyFail("Give what?"); return false; }
        var plan = FromInventory(count, all, noun, out var refusal);
        if (plan == null) { NotifyFail(all && noun == "" ? "You have nothing to give." : $"You have no '{Esc(noun)}'."); return false; }
        if (refusal != null) { Write(refusal + "\n"); return true; }
        var boundOne = plan.FirstOrDefault(p => p.item.Call<bool>("QueryBound"));
        if (boundOne.item != null) { Write(boundOne.item.Call<string>("QueryBoundLine") + "\n"); plan = plan.Where(p => !p.item.Call<bool>("QueryBound")).ToList(); if (plan.Count == 0) return true; }
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
        LeaveRest();
        PersistNow();
        receiver.Call("SaveAfterTrade");
        return true;
    }

    private bool CmdInventory(string args)
    {
        var shown = AllInventory().Where(i => i.Call<string>("Short") != null).ToList();
        if (shown.Count == 0) { Write("You are empty-handed.\n"); return true; }
        var lines = Collapse(shown, i => IsEquipped(i) ? (i.Call<string>("QuerySlot") == "weapon" ? " (wielded)" : " (worn)") : "");
        Write("Inventory\n" + string.Concat(lines.Select(l => "   " + l.text + "\n")));
        return true;
    }

    private bool CmdRead(string args)
    {
        if (args == "") { NotifyFail("Read what?"); return false; }
        if (InDark()) { Write("It is too dark to read.\n"); return true; }
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
        if (InDark() && args != "")
        {
            // You cannot name what you cannot see; "something" and "it" pick the one you are fighting, or one at random.
            if (!UnseenWord(args)) { Write($"You cannot see {WithArticle(args.Trim())}.\n"); return true; }
            target = PickUnseen();
            if (target == null) { Write("You can sense nothing to fight.\n"); return true; }
        }
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
        LeaveRest();
        OpenFight(target);
        Focus(target);
        EndSneakLoud();
        return true;
    }

    // Where the character came from by an exit: the room they came from, and the room they arrived in (flee leads back only while they are still there).
    private string cameFrom = "";
    private string arrivedIn = "";

    /// <summary>A room tells the character they have just walked through one of its exits into their current room. Only a room may; it is remembered for flee.</summary>
    public void RecordArrival()
    {
        QueryResting(); // a seated character who has walked away is standing now
        var from = PreviousObject();
        var here = Environment();
        if (from == null || from.IsClone || here == null || from == here) return;
        cameFrom = from.Name;
        arrivedIn = here.Name;
    }

    // ---- Light (docs/specs/light.md, section 3): light, extinguish, fill. They are the character's commands, so names, numbers and stacks work as everywhere. ----

    /// <summary>The state of the room the character stands in, as they see it: "lit", "dim" or "dark".</summary>
    private string RoomLight() => Environment() is { } room ? room.Call<string>("QueryLightState") ?? "lit" : "lit";

    private bool InDark() => RoomLight() == "dark";

    /// <summary>The lights the character carries that answer to these words (all of them for no words), unlit ones first or lit ones first.</summary>
    private List<ObjectRef> CarriedLights(string words, bool litFirst)
    {
        var all = AllInventory().Where(i => i.IsAlive && i.Call<bool>("IsLightSource")).ToList();
        if (words != "") all = all.Where(i => i.Call<bool>("Id", words.ToLowerInvariant())).ToList();
        return all.OrderBy(i => i.Call<bool>("QueryLit") == litFirst ? 0 : 1).ToList();
    }

    private bool CmdLight(string args)
    {
        var words = args.Trim();
        var lights = CarriedLights(words, false);
        if (lights.Count == 0)
        {
            if (words == "") { Write("You have nothing to light.\n"); return true; }
            NotifyFail("Light what?");
            return false; // another object's light command (the toy lantern) may answer
        }
        if (words == "")
        {
            var unlit = lights.Where(l => !l.Call<bool>("QueryLit")).ToList();
            if (unlit.Count == 0) { Write("It is already burning.\n"); return true; }
            if (unlit.Count > 1 && unlit.Select(l => l.Call<string>("Short")).Distinct().Count() > 1) { Write("Light which? (" + string.Join(", ", unlit.Select(l => "{cmd}light " + l.Call<string>("QueryLightWord") + "{/}").Distinct()) + ")\n"); return true; }
            lights = unlit;
        }
        var item = lights[0];
        if (item.Call<bool>("QueryLit")) { Write("It is already burning.\n"); return true; }
        if (!item.Call<bool>("QueryUsable")) { Write($"The {(item.Call<string>("QueryName") ?? "light").ToLowerInvariant()} has no oil left.\n"); return true; }
        UseAsAction(item); // a quick action on the combat clock (Recovery 0.5): now if ready, else queued
        return true;
    }

    private bool CmdPut(string args)
    {
        var words = args.Trim();
        if (!words.StartsWith("out ") && words != "out") return false;
        return Extinguish(words.Length > 3 ? words.Substring(4).Trim() : "");
    }

    private bool CmdExtinguish(string args) => Extinguish(args.Trim());

    private bool Extinguish(string words)
    {
        var lights = CarriedLights(words, true);
        if (lights.Count == 0)
        {
            Write(words == "" ? "You have nothing to put out.\n" : $"You have no '{Esc(words)}'.\n");
            return true;
        }
        var item = lights[0];
        if (!item.Call<bool>("QueryLit")) { Write("It is not lit.\n"); return true; }
        item.Call("Douse", Self);
        return true;
    }

    private bool CmdFill(string args)
    {
        var words = args.Trim();
        var lanterns = AllInventory().Where(i => i.IsAlive && i.Call<bool>("IsLightSource") && i.Call<bool>("QueryRefillable") && (words == "" || i.Call<bool>("Id", words.ToLowerInvariant()))).ToList();
        if (lanterns.Count == 0)
        {
            if (words != "" && AllInventory().Any(i => i.Call<bool>("IsLightSource") && i.Call<bool>("Id", words.ToLowerInvariant()))) { Write("That cannot be filled.\n"); return true; }
            Write(words == "" ? "You have nothing to fill.\n" : $"You have no '{Esc(words)}' to fill.\n");
            return true;
        }
        if (lanterns[0].Call<bool>("QueryFull")) { Write("The lantern is full.\n"); return true; }
        var flask = AllInventory().FirstOrDefault(i => i.IsAlive && i.Call<bool>("IsLampOil"));
        if (flask == null) { Write("You have no lamp oil.\n"); return true; }
        var told = lanterns[0].Call<string>("Refill", Self, flask) ?? "";
        if (told != "") Write(told + "\n");
        return true;
    }

    // ---- Darkness: what the character knows of the ways out (docs/specs/light.md, section 5). Not saved. ----

    private readonly Dictionary<string, List<string>> usedWays = new();

    /// <summary>The room the character came from, if they came into this room by an exit (""), for the way in that a dark room tells them.</summary>
    public string QueryArrivedFrom() => Environment() is { } here && arrivedIn == here.Name ? cameFrom : "";

    /// <summary>A room tells the character they have just left it by this exit. Only that room may (it stands in the room, and it is the character's own room).</summary>
    public void RememberWay(string roomName, string direction)
    {
        var caller = PreviousObject();
        if (caller == null || caller != Environment() || caller.Name != roomName || !TrustedCaller(caller) || direction == null || direction.Length > 32) return;
        if (!usedWays.TryGetValue(roomName, out var list)) { if (usedWays.Count > 400) usedWays.Clear(); usedWays[roomName] = list = new List<string>(); }
        if (!list.Contains(direction) && list.Count < 16) list.Add(direction);
    }

    /// <summary>The exits the character has used from this room since they logged in.</summary>
    public List<string> QueryUsedWays(string roomName) => usedWays.TryGetValue(roomName ?? "", out var list) ? new List<string>(list) : new List<string>();

    /// <summary>" (light torch)" if the character carries a light that is not lit, for the line a dark room gives on entering; "" if they carry none.</summary>
    public string QueryUnlitLightHint()
    {
        foreach (var item in AllInventory())
            if (item.IsAlive && item.Call<bool>("IsLightSource") && !item.Call<bool>("QueryLit") && item.Call<bool>("QueryUsable"))
                return " ({cmd}light " + (item.Call<string>("QueryLightWord") ?? "light") + "{/})";
        return "";
    }

    private static bool UnseenWord(string args) => args.Trim().ToLowerInvariant() is "something" or "it";

    private static string WithArticle(string noun) => noun.StartsWith("a ") || noun.StartsWith("an ") || noun.StartsWith("the ") ? noun : Article(noun) + " " + noun;

    /// <summary>In the dark: the enemy the character is already fighting, or else one creature of the room chosen at random (never one that is no threat).</summary>
    private ObjectRef? PickUnseen()
    {
        var room = Environment();
        if (room == null) return null;
        var first = Enemies().FirstOrDefault(e => Environment(e) == room);
        if (first != null) return first;
        var choices = AllInventory(room).Where(o => o != Self && o.IsAlive && o.Call<bool>("IsCreature") && !Interactive(o) && o.Call<bool>("IsMonster") && CanFight(o) == null).ToList();
        return choices.Count == 0 ? null : choices[Random(choices.Count)];
    }

    private const string FightingNote = "You are fighting! You cannot do that now. Break away with {cmd}flee{/} first.";

    /// <summary>
    /// flee [direction]: try to break away from a fight (one chance in four to fail). With a direction, go that way; without,
    /// go back the way you came; if that way cannot be used, any usable exit. A named direction that cannot be used fails at
    /// once and costs no attempt.
    /// </summary>
    private bool CmdFlee(string args)
    {
        var room = Environment();
        if (room == null || !QueryInCombat()) { Write("You are not fighting anyone.\n"); return true; }
        string? direction;
        args = args.Trim();
        if (args != "")
        {
            direction = room.Call<string>("QueryUsableExit", Self, args);
            if (direction == null) { Write($"You cannot flee that way: there is no usable exit '{Esc(args)}' here.\n"); return true; }
        }
        else
        {
            direction = arrivedIn == room.Name ? room.Call<string>("QueryExitTowards", Self, cameFrom) : null;
            if (direction == null)
            {
                var exits = room.Call<List<string>>("QueryUsableExits", Self) ?? new List<string>();
                if (exits.Count == 0) { Write("There is no way out!\n"); return true; }
                direction = exits[Random(exits.Count)];
            }
        }
        if (QueryMovementBarred() > 0) { Tell(QueryMovementBarLine() + "\n"); return true; }
        if (Random(4) == 0) { Tell("You try to flee, but cannot get away!\n"); return true; }
        // Told to this character directly (not to whoever the driver thinks is typing), and always before the new room is described.
        Tell($"You flee {direction}!\n");
        room.Call("MoveThrough", Self, direction);
        return true;
    }

    // ---- World features: hidden exits and search (docs/GAME_SYSTEMS.md) ----

    private const int MaxFoundExits = 1000;
    private const int MaxFoundExitsInWiz = 50; // records for rooms under /wiz are capped, so a sandbox cannot fill the save file

    /// <summary>
    /// Everything this character has found in a room, as "room path|direction" for a hidden exit and "room path|#id" for any other hidden thing
    /// (docs/specs/search.md section 5.1). Kept on the character; a room asks for its own. The saved field keeps its old name so old saves load unchanged.
    /// </summary>
    [Saved] private List<string> foundExits = new();

    public bool QueryFoundExit(string roomPath, string direction) => foundExits.Contains(roomPath + "|" + direction);

    /// <summary>Has this character found the thing with this key ("#id", or "#" and the program path of a hidden find) in this room?</summary>
    public bool QueryFound(string roomPath, string key) => foundExits.Contains(roomPath + "|" + key);

    /// <summary>
    /// A room tells the character they have found one of its hidden exits. The room is the caller and can only record itself, for a
    /// character who is in it, so no other object (a wizard's, say) can put a record against a room it is not. Returns true if the exit
    /// was newly recorded; the record is saved at once.
    /// </summary>
    public bool RecordFoundExit(string direction)
    {
        var room = PreviousObject();
        if (direction == "" || direction.StartsWith('#') || !(room?.Call<List<string>>("QueryHiddenExits")?.Contains(direction) ?? false)) return false;
        return AddFound(room, direction);
    }

    /// <summary>
    /// A room tells the character they have found a hidden thing of its own: key is a direction (an exit) or "#id" for an item, node, clue or hidden
    /// find the room declares. The same rules as RecordFoundExit: the room is the caller, can record only itself, for a character who is in it, and only a key
    /// it really declares (QueryHiddenKeys). True if newly recorded; saved at once.
    /// </summary>
    public bool RecordFound(string key)
    {
        var room = PreviousObject();
        if (room == null || key == "" || !key.StartsWith('#')) return RecordFoundExit(key);
        if (!(room.Call<List<string>>("QueryHiddenKeys")?.Contains(key) ?? false)) return false;
        return AddFound(room, key);
    }

    private bool AddFound(ObjectRef? room, string tail)
    {
        if (room == null || room.IsClone || Environment() != room || tail.Contains('|')) return false;
        var key = room.Name + "|" + tail;
        if (foundExits.Contains(key) || foundExits.Count >= MaxFoundExits) return false;
        if (room.Name.StartsWith("/wiz/", StringComparison.OrdinalIgnoreCase) && foundExits.Count(k => k.StartsWith("/wiz/", StringComparison.OrdinalIgnoreCase)) >= MaxFoundExitsInWiz) return false;
        foundExits.Add(key);
        PersistNow();
        return true;
    }

    // ---- Search (docs/specs/search.md) ----
    // The numbers and the dice are in /secure/search. The wait after a failed search is kept here, on the character, and never saved; only the
    // character's own search action (below) starts one. What a search finds is the room's business (Room.Search).

    private const string SearchDaemon = "/secure/search";
    private const int MaxSearchWaits = 32;
    private readonly Dictionary<string, long> searchWaits = new();
    private string searchNamed = "";
    private ObjectRef? searchRoom;

    /// <summary>The bonus this character brings to a search: Insight above 5 (one point of Insight is one point of the d20, 5%), plus the "search" modifier (the Scout's Keen Eye +2, a lens, a charm).</summary>
    public int QuerySearchBonus() => QueryAttribute("insight") - 5 + (int)Math.Round(QueryMod("search"));

    /// <summary>Whole seconds until this character may search this room again (0 if they may now).</summary>
    public int QuerySearchWaitLeft(string roomPath)
    {
        if (!searchWaits.TryGetValue(roomPath ?? "", out var until)) return 0;
        var left = until - TimeMs();
        if (left <= 0) { searchWaits.Remove(roomPath ?? ""); return 0; }
        return (int)((left + 999) / 1000);
    }

    private void StartSearchWait(string roomPath)
    {
        var now = TimeMs();
        foreach (var old in searchWaits.Where(w => w.Value <= now).Select(w => w.Key).ToList()) searchWaits.Remove(old);
        while (searchWaits.Count >= MaxSearchWaits) searchWaits.Remove(searchWaits.OrderBy(w => w.Value).First().Key);
        searchWaits[roomPath] = now + LoadObject(SearchDaemon).Call<int>("QueryWaitSeconds") * 1000L;
    }

    private bool CmdSearch(string args)
    {
        var room = Environment();
        if (room == null) return false;
        if (QueryInCombat()) { Write("You cannot search while you are fighting.\n"); return true; }
        var named = args.Trim().ToLowerInvariant();
        foreach (var article in new[] { "the ", "a ", "an " }) if (named.StartsWith(article)) { named = named.Substring(article.Length).Trim(); break; }
        if (named.Length > 40) named = named.Substring(0, 40);
        var left = QuerySearchWaitLeft(room.Name);
        if (left > 0)
        {
            Write($"You have only just searched here; your eyes need a rest. Try again in about {(left + 9) / 10 * 10} seconds.\n");
            return true;
        }
        // Searching is loud, like opening a chest: it ends sneaking (and wakes whatever had not noticed), and it gets a seated character up.
        LeaveRest();
        EndSneakLoud();
        searchNamed = named;
        searchRoom = room;
        // 3 seconds to the result (Preparation 1.5 units); the character may act again after Reaction Time less 0.5, doubled. A blow spoils it, and so does an attack.
        ActAsAction(new CombatAction
        {
            Id = "search", Name = "Search", Kind = "search", Preparation = 1.5, Recovery = 0, ReactionMod = -0.5, Spoilable = true,
            WindUp = named == "" ? "searches the area carefully" : $"searches the {named} carefully",
        }, named == "" ? "search" : $"search the {named}");
        return true;
    }

    protected override void Special(CombatAction action, ObjectRef? target)
    {
        if (action.Kind != "search") { base.Special(action, target); return; }
        var room = Environment();
        var started = searchRoom;
        searchRoom = null;
        if (room == null || room != started) return; // walked away while searching: nothing comes of it
        if (QueryInCombat()) { Tell("You cannot search while you are fighting.\n"); return; }
        var result = room.Call<int>("Search", Self, searchNamed);
        if (result > 0) return; // the room has told the finder, and the others
        if (result == 0) Tell("You search carefully but find nothing of note.\n");
        Tell("(You will need a minute before you can look this place over properly again.)\n");
        StartSearchWait(room.Name);
    }

    private bool CmdConsider(string args)
    {
        var room = Environment();
        if (InDark() && args != "") { Write($"You cannot see {WithArticle(args.Trim())}.\n"); return true; }
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
        LeaveRest();
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
        LeaveRest();
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
        LeaveRest();
        Write($"You {(slot == "weapon" ? "stop wielding" : "take off")} {item.Call<string>("Short")}.\n");
        return true;
    }

    private bool CmdUse(string args)
    {
        var verb = QueryVerb();
        var item = args == "" ? null : Present(args);
        if (item == null) { NotifyFail($"{Cap(verb)} what?"); return false; }
        var kind = item.Call<string>("QueryUseVerb");
        if (kind == null || (kind != "eat" && kind != "drink") || (kind == "eat") != (verb == "eat")) { Write($"You can't {verb} that.\n"); return true; }
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
        // The layout is /secure/scoresheet's (docs/specs/score.md); the numbers are this body's own.
        var sheet = LoadObject("/secure/scoresheet");
        var wizard = QueryWizLevel();
        var text = new StringBuilder();
        var renownTitle = wizard > 0 ? "" : LoadObject("/secure/renown").Call<string>("QueryActiveTitle", QueryName()) ?? "";
        text.Append(sheet.Call<string>("Box", (Short() ?? Name).Replace("{{", "{"), renownTitle));
        if (WorkshopNote() is { Length: > 0 } note) text.Append(note.Trim() + "\n"); // in the workshop: nothing here is saved
        var calling = archetypeName == "" ? "No calling yet" : archetypeName;
        text.Append($"\n{calling}" + (advancedClass != "" || subclass != "" || prestigeClass != "" ? $" • Class {classTitle}" : "") + $" • Level {Level}" + (WizUnseen() ? " (invisible)" : "") + "\n");
        if (wizard == 0) text.Append(sheet.Call<string>("RankLine", QueryName()));
        text.Append($"\nVitality   {Vitality}/{QueryMaxVitality()}\nEndurance  {QueryEndurance()}/{QueryMaxEndurance()}\nFocus      {QueryFocus()}/{QueryMaxFocus()}\n\n");
        text.Append(sheet.Call<string>("Standing", QueryName(), wizard) + "\n");
        var power = QueryStat("physical_power");
        text.Append(sheet.Call<string>("Heading", "Combat Summary"));
        text.Append($"Damage {QueryDamageMin()}-{QueryDamageMax()}" + (power != 0 ? $" ({(power > 0 ? "+" : "")}{Pct(power)}%)" : "") +
                    $"   To hit +{QueryAttack()}   Defense {QueryDefense()}   Armor {QueryArmor()}\nDodge {QueryDodge()}%   Block {QueryBlock()}%   Critical {Pct(QueryCrit())}%\n");
        var reaction = ReactionReport().TrimEnd('\n');
        if (reaction.Length > 80) reaction = reaction.Replace(". Your attack", ".\nYour attack"); // two sentences, two lines, when one line is too wide
        text.Append(sheet.Call<string>("Wrap", reaction) + "\n");
        text.Append(sheet.Call<string>("Heading", "Attributes"));
        text.Append(sheet.Call<string>("Columns", AttributeNames.Select(a =>
        {
            var total = QueryAttribute(a);
            var basis = QueryBaseAttribute(a);
            return $"{AttributeTitle(a)} {total}" + (total != basis ? $" ({basis})" : "");
        }).ToList()));
        var points = QueryAttributePoints();
        if (points > 0) text.Append($"Attribute points to place: {points} ({{cmd}}improve{{/}} ATTRIBUTE)\n");
        text.Append(sheet.Call<string>("Columns", QueryStatDisplay().Select(s => $"{s.name} {s.value}").ToList()));
        var effectLines = QueryEffectLines();
        if (effectLines.Count > 0) text.Append(sheet.Call<string>("Wrap", "Effects: " + string.Join(", ", effectLines)));
        text.Append("\n" + sheet.Call<string>("Heading", "Progress"));
        text.Append($"Experience {xp}" + (Level >= MaxLevel ? " (highest level)" : $" ({QueryXpForLevel(Level + 1) - xp} more for level {Level + 1})") + $"   Gold {Gold}   Carry Capacity {QueryCarryCapacity()} lb\n");
        var area = Environment()?.Call<string>("QueryAreaName") ?? "";
        var region = Environment()?.Call<string>("QueryRegionName") ?? "";
        if (area != "" || region != "") text.Append($"Area {area}   Region {region}\n");
        // Light (docs/specs/light.md, section 15): nothing when the room is lit and nothing of yours burns.
        var lightHere = RoomLight();
        if (lightHere == "dim") text.Append("The light here is dim.\n");
        else if (lightHere == "dark") text.Append("It is dark here.\n");
        foreach (var burning in AllInventory().Where(i => i.IsAlive && i.Call<bool>("IsLightSource") && i.Call<bool>("QueryLit")))
            text.Append(sheet.Call<string>("Wrap", $"You carry a light: {burning.Call<string>("Short")?.Replace(" (lit)", "")} ({burning.Call<string>("QueryTimeLeft")?.Replace("about ", "about ").Replace(" of burning", " left").Replace(" of oil", " of oil left")})."));
        var gear = new List<string>();
        foreach (var slot in new[] { "weapon", "shield", "body", "head", "trinket" })
            if (QueryEquipped(slot) is { } item) gear.Add($"{slot}: {item.Call<string>("Short")}");
        text.Append(sheet.Call<string>("Wrap", gear.Count == 0 ? "You have nothing equipped." : "Equipment: " + string.Join(", ", gear)));
        text.Append(sheet.Call<string>("Wrap", QuestLog()));
        if (QueryResting()) text.Append(sheet.Call<string>("Wrap", $"You are resting: Vitality, Endurance and Focus return {RestFactor:0.#} times as fast. ({{cmd}}stand{{/}} to get up)"));
        if (sneaking)
        {
            var sneakRank = Math.Max(1, QuerySkillRank("sneak"));
            var sneakSkill = LoadObject("/skills/sneak");
            var notice = sneakSkill.Call<int>("QueryNoticeAt", sneakRank);
            text.Append(sheet.Call<string>("Wrap", $"You are sneaking (rank {Skill.Roman(sneakRank)}): each room costs {sneakSkill.Call<int>("QueryRoomCostAt", sneakRank)} Endurance" + (notice != 0 ? $", and creatures notice you {-notice} points less often" : "") + $". ({{cmd}}sneak off{{/}} to stop)"));
        }
        if (WizUnseen()) text.Append(sheet.Call<string>("Wrap", "You are invisible: mortals and lower wizards cannot see you. ({cmd}vis{/} to be seen)"));
        if (QueryHiddenFromWho()) text.Append("You are keeping out of sight of the who list.\n");
        Write(text.ToString());
        return true;
    }

    /// <summary>
    /// "stats": the numbers of `score` and nothing else, in two columns of label and value, 12 rows and 48 columns at most, plain ASCII
    /// (docs/specs/who-and-visibility.md section 7). A wizard's own; another object's numbers are `stat THING`.
    /// </summary>
    private bool CmdStats(string args)
    {
        var derived = QueryStatDisplay().ToDictionary(s => s.name, s => s.value);
        var cells = new List<(string label, string value)>();
        foreach (var a in AttributeNames)
        {
            var total = QueryAttribute(a);
            var basis = QueryBaseAttribute(a);
            cells.Add((AttributeTitle(a), total + (total != basis ? $" ({basis})" : "")));
        }
        var attributeRows = (cells.Count + 1) / 2;
        var power = QueryStat("physical_power");
        cells.AddRange(new (string, string)[]
        {
            ("Vitality", $"{Vitality}/{QueryMaxVitality()}"), ("Damage", $"{QueryDamageMin()}-{QueryDamageMax()}"),
            ("Endurance", $"{QueryEndurance()}/{QueryMaxEndurance()}"), ("To hit", $"+{QueryAttack()}"),
            ("Focus", $"{QueryFocus()}/{QueryMaxFocus()}"), ("Defense", QueryDefense().ToString()),
            ("Armor", QueryArmor().ToString()), ("Dodge", $"{QueryDodge()}%"),
            ("Block", $"{QueryBlock()}%"), ("Critical", $"{Pct(QueryCrit())}%"),
            ("Reaction", Num(QueryReaction())), ("Attack Time", $"{Num(QueryTypicalDelay())} s"),
            ("Mental Res", derived.GetValueOrDefault("Mental Resistance").ToString()), ("Physical Res", derived.GetValueOrDefault("Physical Resistance").ToString()),
            ("Carry", $"{QueryCarryCapacity()} lb"),
        });
        var calling = archetypeName == "" ? "No calling yet" : archetypeName;
        var text = new StringBuilder();
        text.Append($"{Name} - {calling}" + (advancedClass != "" || subclass != "" || prestigeClass != "" ? $" - {classTitle}" : "") + $" - Level {Level}" + (WizUnseen() ? " (invisible)" : "") + "\n");
        for (var i = 0; i < cells.Count; i += 2)
        {
            if (i == attributeRows * 2 && QueryAttributePoints() > 0) text.Append($"Points to place: {QueryAttributePoints()} ({{cmd}}improve{{/}} ATTRIBUTE)\n");
            var left = cells[i];
            var row = left.label.PadRight(11) + " " + left.value.PadRight(10);
            if (i + 1 < cells.Count) row += " " + cells[i + 1].label.PadRight(12) + " " + cells[i + 1].value;
            text.Append(row.TrimEnd() + "\n");
        }
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

    private bool CmdVitality(string args)
    {
        Write($"Vitality {Vitality}/{QueryMaxVitality()}   Endurance {QueryEndurance()}/{QueryMaxEndurance()}   Focus {QueryFocus()}/{QueryMaxFocus()}\n");
        return true;
    }

    private bool CmdQuests(string args)
    {
        if (JournalQuestsCommand(args)) return true; // quests completed | abandoned | NAME
        Write(QuestLog() + JournalFooter());
        return true;
    }

    private string QuestLog()
    {
        var active = quests.Where(q => q.Value == 1).Select(q => QuestTitle(q.Key)).ToList();
        var done = quests.Where(q => q.Value == 2).Select(q => QuestTitle(q.Key)).ToList();
        if (active.Count == 0 && done.Count == 0) return "You have no quests.\n" + (QueryNeedsGuild() ? LoadObject("/secure/guidepost").Call<string>("Hint") : "");
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
            var roster = LoadObject("/secure/roster");
            var player = Users().FirstOrDefault(u => string.Equals(u.Call<string>("QueryName"), args, StringComparison.OrdinalIgnoreCase) && roster.Call<bool>("Visible", Self, u)); // a wizard you cannot see is not a name here
            var place = player == null ? null : Environment(player);
            destination = place ?? LoadObject(ResolvePath(args));
        }
        catch (MudRuntimeException problem)
        {
            Write(Esc(problem.Message) + "\n");
            return true;
        }
        SayToSeers($"{Name} vanishes in a puff of smoke.\n"); // an invisible wizard is seen to come and go only by those who can see them
        MoveObject(destination);
        SayToSeers($"{Name} appears in a puff of smoke.\n");
        Look();
        return true;
    }

    private bool CmdStat(string args)
    {
        if (!IsWizard(LevelStat)) return args.Trim() == "" && CmdScore(args); // a player's bare stat is score
        var thing = args.Trim() == "" ? null : FindHere(args.Trim());
        if (thing == null) { NotifyFail("Stat what? (a name, me, or here)"); return false; }
        Write((thing.Call<string>("Stat") ?? thing.Name + "\n") + (thing.Domain != "" ? $"  Sandbox object ({thing.Domain}): it cannot leave the workshop.\n" : ""));
        return true;
    }

    private bool CmdRestore(string args)
    {
        if (!IsWizard(LevelHeal)) return false;
        var target = args.Trim() == "" ? Self : FindHere(args.Trim());
        if (target == null || !target.Call<bool>("IsCreature")) { NotifyFail("Restore whom?"); return false; }
        target.Call("Heal", 100000);
        target.Call("FillResources");
        if (target == Self) Write("You are restored to full Vitality.\n");
        else
        {
            Write($"You restore {target.Call<string>("QueryLabel")} to full Vitality.\n");
            TellObject(target, "A warm light restores you to full Vitality.\n");
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
        // AddEffect, not AddSpeedEffect: a wizard's slow is never resisted (Physical Resistance is for slows in a fight).
        var factor = (100 - percent) / 100.0;
        target.Call("AddEffect", "wizard", factor < 1.0 ? "Haste" : "Slow", seconds, "", factor, 0);
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
    /// Vitality and resources are filled. Level 10 wizards only.
    /// </summary>
    private bool CmdSetLevel(string args)
    {
        if (!IsWizard(LevelSetLevel)) return false;
        if (!int.TryParse(args.Trim(), out var level) || level < 1 || level > MaxLevel) { NotifyFail("Usage: {cmd}setlevel{/} 1-" + MaxLevel); return false; }
        Level = level;
        xp = QueryXpForLevel(level);
        Vitality = QueryMaxVitality();
        FillResources();
        Write($"You are now level {Level} with {xp} experience; the next level needs {(Level >= MaxLevel ? 0 : QueryXpForLevel(Level + 1))}.\n");
        return true;
    }

    /// <summary>
    /// "hidden": level 10 only, to anyone else not a command. Lists the hidden classes with their counts, the people for one, serves a summons to a
    /// mortal who is playing (a test aid), clears a record, sets the loot counter, fixes the dice and runs the roll's formula many times.
    /// Everything is the register's (/secure/hidden), which checks the level again.
    /// </summary>
    private bool CmdHidden(string args)
    {
        if (!IsWizard(10)) return false;
        var reg = LoadObject("/secure/hidden");
        var w = args.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        const string usage = "Usage: {cmd}hidden{/} | {cmd}hidden{/} CLASS | {cmd}hidden grant{/} NAME CLASS | {cmd}hidden reset{/} NAME CLASS | {cmd}hidden count{/} NAME CLASS N | {cmd}hidden dice{/} N|random | {cmd}hidden simulate{/} CLASS TRIALS | {cmd}hidden refusals{/}\n";
        if (w.Length == 0 || (w.Length == 1 && w[0] != "refusals" && w[0] != "dice"))
        {
            var lines = reg.Call<List<string>>("Report", w.Length == 0 ? "" : w[0]) ?? new List<string>();
            Write(lines.Count == 0 ? "No hidden classes" + (w.Length == 0 ? "" : " by that name") + ".\n" : string.Join("\n", lines) + "\n");
            return true;
        }
        string? answer = null;
        switch (w[0])
        {
            case "grant" when w.Length == 3: answer = reg.Call<string>("Grant", w[1], w[2]); Write(answer == "" ? $"A summons is served to {w[1]}.\n" : answer + "\n"); return true;
            case "reset" when w.Length == 3: answer = reg.Call<string>("Reset", w[1], w[2]); Write(answer == "" ? $"The record of {w[1]} is cleared.\n" : answer + "\n"); return true;
            case "count" when w.Length == 4 && int.TryParse(w[3], out var n): answer = reg.Call<string>("SetLooted", w[1], w[2], n); Write(answer == "" ? "Counted.\n" : answer + "\n"); return true;
            case "dice" when w.Length == 1: Write($"Dice: {reg.Call<string>("DiceMode")}\n"); return true;
            case "dice" when w.Length == 2: Write(reg.Call<bool>("SetTestRolls", w[1] == "random" ? "random" : "roll:" + w[1]) ? $"Dice: {reg.Call<string>("DiceMode")}\n" : "Not set.\n"); return true;
            case "simulate" when w.Length == 3 && int.TryParse(w[2], out var trials):
                var sims = reg.Call<List<int>>("Simulate", w[1], trials) ?? new List<int>();
                if (sims.Count == 0) { Write("There is no such hidden class.\n"); return true; }
                var sorted = sims.OrderBy(x => x).ToList();
                Write($"{sims.Count} trials: mean {sims.Average():0.0}, median {sorted[sorted.Count / 2]}, 90th {sorted[(int)(sorted.Count * 0.9)]}, never {sorted[0]} before, longest {sorted[^1]}\n");
                return true;
            case "refusals": Write(string.Join("\n", reg.Call<List<string>>("Refusals") ?? new List<string>()) + "\n"); return true;
        }
        Write(usage);
        return true;
    }

    // ---- Crafting and gathering (docs/CRAFTING.md) ----
    // A self-contained block. The commands are run by the crafting daemon, /secure/crafting; this body binds the verbs, keeps
    // the character's proficiencies and known recipes, and runs the timed part of each action on its own combat clock (the
    // action's "skill" is the daemon, so the clock calls the daemon's Resolve). Only the daemon may change what is kept here.

    private const string CraftDaemon = "/secure/crafting";
    private static readonly string[] CraftVerbs = { "skin", "bleed", "harvest", "craft", "combine", "recipes", "recipe", "proficiencies" };

    /// <summary>Proficiency id to its value in tenths of a point (0 to 1000). Ids are those of the crafting data.</summary>
    [Saved] private Dictionary<string, int> craftProficiencies = new();
    /// <summary>Proficiency id to the character's place on the growth ladder (never shown). Absent means the first rung.</summary>
    [Saved] private Dictionary<string, int> craftLadder = new();
    /// <summary>Recipe id to how it was learned: "taught" or "discovered".</summary>
    [Saved] private Dictionary<string, string> craftRecipes = new();

    /// <summary>A proficiency, 0 to 100 (whole points).</summary>
    public int QueryProficiency(string id) => craftProficiencies.GetValueOrDefault(id ?? "") / 10;

    /// <summary>A proficiency in tenths of a point, 0 to 1000.</summary>
    public int QueryProficiencyTenths(string id) => craftProficiencies.GetValueOrDefault(id ?? "");

    public Dictionary<string, int> QueryProficiencies() => new(craftProficiencies);

    /// <summary>The recipes this character knows: id to "taught" or "discovered".</summary>
    public Dictionary<string, string> QueryKnownRecipes() => new(craftRecipes);

    public bool QueryKnowsRecipe(string id) => craftRecipes.ContainsKey(id ?? "");

    private bool FromCraftDaemon()
    {
        var caller = PreviousObject();
        return caller != null && !caller.IsClone && caller.ProgramPath == CraftDaemon;
    }

    /// <summary>Set a proficiency (tenths, 0 to 1000). Only the crafting daemon may; it does so for work the character has done.</summary>
    public bool CraftSetProficiency(string id, int tenths)
    {
        if (!FromCraftDaemon() || string.IsNullOrEmpty(id) || id.Length > 40 || tenths < 0 || tenths > 1000) return false;
        if (!craftProficiencies.ContainsKey(id) && craftProficiencies.Count >= 100) return false;
        craftProficiencies[id] = tenths;
        return true;
    }

    /// <summary>The place on the growth ladder for a proficiency. Only the crafting daemon is told; anyone else gets 0.</summary>
    public int QueryCraftLadder(string id) => FromCraftDaemon() ? craftLadder.GetValueOrDefault(id ?? "") : 0;

    /// <summary>Set the place on the growth ladder. Only the crafting daemon may.</summary>
    public bool CraftSetLadder(string id, int position)
    {
        if (!FromCraftDaemon() || string.IsNullOrEmpty(id) || id.Length > 40 || position < 0 || position > 10000) return false;
        if (!craftLadder.ContainsKey(id) && craftLadder.Count >= 100) return false;
        if (position == 0) craftLadder.Remove(id); else craftLadder[id] = position;
        return true;
    }

    /// <summary>Learn a recipe ("taught" or "discovered"). Only the crafting daemon may: for a trainer, a book, a quest or an experiment.</summary>
    public bool CraftLearn(string id, string how)
    {
        if (!FromCraftDaemon() || string.IsNullOrEmpty(id) || id.Length > 60 || craftRecipes.ContainsKey(id) || craftRecipes.Count >= 5000) return false;
        craftRecipes[id] = how == "discovered" ? "discovered" : "taught";
        return true;
    }

    /// <summary>Write the character to disk after a harvest, a craft or a lesson. Only the crafting daemon may ask.</summary>
    public void CraftSave()
    {
        if (FromCraftDaemon()) PersistNow();
    }

    /// <summary>
    /// The next step of a batch of crafting: queued for this character's next Ready Time, quietly. Only the crafting daemon may
    /// ask, and not while the character is fighting. True if queued.
    /// </summary>
    public bool CraftContinue(string name, string windUp, double preparation, double recovery, double reactionMod)
    {
        if (!FromCraftDaemon() || QueryInCombat() || Dead) return false;
        QueueAction(CraftAction(name, windUp, preparation, recovery, reactionMod));
        return true;
    }

    private CombatAction CraftAction(string name, string windUp, double preparation, double recovery, double reactionMod)
    {
        ObjectRef? daemon;
        try { daemon = LoadObject(CraftDaemon); }
        catch (MudRuntimeException) { daemon = null; }
        return new CombatAction { Id = "craft", Name = name, Kind = "skill", Skill = daemon, WindUp = windUp, Preparation = preparation, Recovery = recovery, ReactionMod = reactionMod };
    }

    private void BindCraftCommands()
    {
        foreach (var verb in CraftVerbs) AddAction(verb, CmdCraft);
        AddAction("apply", CmdApply);
    }

    private bool CmdCraft(string args)
    {
        List<object>? reply;
        try { reply = LoadObject(CraftDaemon).Call<List<object>>("Command", Self, QueryVerb(), args); }
        catch (MudRuntimeException) { reply = null; }
        if (reply == null || reply.Count == 0 || reply[0] is not string text) { Write("Nobody here knows how to do that yet.\n"); return true; }
        if (text != "") Write(text.EndsWith("\n") ? text : text + "\n");
        if (reply.Count >= 8 && reply[1] is "act" && reply[2] is string name && reply[3] is string windUp && reply[4] is double prep && reply[5] is double recovery && reply[6] is double mod && reply[7] is string waiting)
        {
            if (QueryInCombat()) { Write("You cannot do that while you are fighting.\n"); return true; }
            LeaveRest();
            ActAsAction(CraftAction(name, windUp, prep, recovery, mod), waiting);
        }
        return true;
    }

    /// <summary>"apply bandage": use a bandage or salve (a consumable whose use verb is "apply") on the combat clock, like a potion.</summary>
    private bool CmdApply(string args)
    {
        var item = args == "" ? null : Present(args);
        if (item == null) { NotifyFail("Apply what?"); return false; }
        if (item.Call<string>("QueryUseVerb") != "apply") { Write("You can't apply that.\n"); return true; }
        UseAsAction(item);
        return true;
    }

    /// <summary>Help on crafting ("help crafting", "help skin", ...), or null for another topic. The player's help command asks.</summary>
    protected string? CraftHelp(string topic)
    {
        try { return LoadObject(CraftDaemon).Call<string>("Help", topic); }
        catch (MudRuntimeException) { return null; }
    }
}
