/// <summary>
/// Base of every character class, from the five archetypes to the advanced, subclass and prestige classes of later
/// stages. A class is data: the file lives in /classes, inherits this class and sets its fields in Create(); its id
/// is its file name. Adding a class is adding one file; nothing else changes (docs/CLASS_SYSTEM.md section 6, and
/// docs/BUILDING.md for a compiled template). A character's save holds only class ids; everything below is read
/// from the class when the character logs in.
/// </summary>
public abstract class PlayerClass : MudObject
{
    /// <summary>The name: "Warrior".</summary>
    protected string ClassName = "";
    /// <summary>"archetype", "advanced", "subclass" or "prestige".</summary>
    protected string Tier = "archetype";
    /// <summary>The class this one builds on (an advanced class names its archetype). Empty for an archetype.</summary>
    protected string Parent = "";
    /// <summary>The title a character with the class is called ("Knight").</summary>
    protected string Title = "";
    /// <summary>One line for the choice at character creation and the Hall of Legends.</summary>
    protected string Tagline = "";
    /// <summary>Longer text for help.</summary>
    protected string Description = "";
    /// <summary>The guild a character joins with the class (an id; guild content comes later).</summary>
    protected string Guild = "";
    /// <summary>A disabled class cannot be chosen or applied and is not shown.</summary>
    protected bool Enabled = true;
    /// <summary>Where the class stands in a list (the five archetypes are 1 to 5).</summary>
    protected int SortOrder = 100;
    /// <summary>Experience needed per level is multiplied by the largest modifier among a character's classes.</summary>
    protected double ExpModifier = 1.0;
    /// <summary>Attack gained per level. 0 means the class names none and the previous class's rate stands.</summary>
    protected double AttackRate;
    /// <summary>Skills and spells the class grants (ids in /skills).</summary>
    protected List<string> Skills = new();
    /// <summary>Traits the class grants (ids in /skills).</summary>
    protected List<string> Traits = new();
    /// <summary>Weapon categories the class adds to what the character may wield; "*" for all.</summary>
    protected List<string> Weapons = new();
    /// <summary>Armor categories the class adds ("light", "medium", "heavy"); "*" for all.</summary>
    protected List<string> ArmorTypes = new();
    /// <summary>True if the class adds the right to carry a shield.</summary>
    protected bool Shield;

    // ---- Hidden classes (docs/specs/hidden-classes.md section 4). A hidden class is an ordinary advanced class that is never listed,
    // never offered and found only through a summons. Everything below is data; the register (/secure/hidden) and the quest giver
    // (/std/hiddengiver) do the work. Nothing from here may be shown to a character who does not hold the class.
    /// <summary>True for a hidden class: never listed by the class registry, no help page until it is held.</summary>
    protected bool Hidden;
    /// <summary>The paper that is served (a path in /domains).</summary>
    protected string SummonsItem = "";
    /// <summary>The quest journal id of the quest that ends with the offer.</summary>
    protected string QuestId = "";
    /// <summary>Opaque key of the gated door; never the class id or title.</summary>
    protected string GateKey = "";
    /// <summary>Path prefixes of every file that may speak of the class (the lint's allowance and the register's callers).</summary>
    protected List<string> OwnPaths = new();
    /// <summary>False closes the roll for this class (nothing is ever served).</summary>
    protected bool SummonsOpen = true;
    /// <summary>The roll: no roll up to the floor, then base percent plus step per further eligible loot, certain on the guarantee-th.</summary>
    protected int SummonsFloor = 10;
    protected double SummonsBase = 2.0;
    protected double SummonsStep = 0.5;
    protected int SummonsGuarantee = 50;
    /// <summary>The lowest character level of the roll (a class may name more than its level requirement).</summary>
    protected int SummonsMinLevel = 5;
    /// <summary>What the player reads: the line when the paper is served, the paper itself ({serial}, {date}, {Name}), the door's lines.</summary>
    protected string SummonsFound = "";
    protected string SummonsText = "";
    protected string BoundLine = "";
    protected string GateKnock = "";
    protected string GateAgain = "";
    protected string GateOpen = "";
    protected string GateLook = "";
    protected string GateReissue = "";
    /// <summary>The first number of a reference on the paper (the register's serial is added to it).</summary>
    protected int SerialBase;
    /// <summary>The level at which each skill is given (docs/specs/bureaucrat.md section 7.1); a skill not named here is given at once.</summary>
    protected Dictionary<string, int> SkillLevels = new();

    /// <summary>A skill that the class gives only when the character reaches this level (and at once, at rank I, if the class is taken later).</summary>
    protected void SkillAt(string skillId, int level) => SkillLevels[skillId] = level;

    // The resources (docs/ATTRIBUTES.md section 2): maximum = base + per-level x Level + attributes. An archetype names all six;
    // a later class may name its own. -1 means the class names none: the previous class's number stands, and an archetype that
    // names none has the Adventurer's (10 / 8, 50 / 2, 50 / 2).
    /// <summary>Vitality base and gain per level (Warrior 17 and 9).</summary>
    protected int VitalityBase = -1;
    protected int VitalityPerLevel = -1;
    /// <summary>Endurance base and gain per level.</summary>
    protected int EnduranceBase = -1;
    protected int EndurancePerLevel = -1;
    /// <summary>Focus base and gain per level.</summary>
    protected int FocusBase = -1;
    protected int FocusPerLevel = -1;
    /// <summary>
    /// Percentage modifiers applied after the formulas (docs/ATTRIBUTES.md section 3, "Class modifiers"): "vitality" 15 is
    /// +15% maximum Vitality. Keys: vitality, endurance, focus, vitality_regen, endurance_regen, focus_regen and the derived
    /// statistics (physical_power, precision, evasion, crit, spell_power, healing_power, mental_resistance, physical_resistance,
    /// carry_capacity, gathering, crafting). The modifiers of all of a character's classes add up. The five callings have none.
    /// </summary>
    protected Dictionary<string, double> Modifiers = new();

    // Archetype values (ignored by the other tiers).
    /// <summary>
    /// The attributes a new character of this calling starts with: might, grace, vigor, insight, spirit, resolve (5 each and 6
    /// points by calling). An attribute not named starts at 5.
    /// </summary>
    protected Dictionary<string, int> StartAttributes = new();
    protected double StartReaction = 2.5;
    protected int StartGold = 25;
    /// <summary>Paths of the items a new character starts with. Weapons and shields are put on.</summary>
    protected List<string> Kit = new();

    private readonly List<string> requirementKinds = new();
    private readonly List<string> requirementValues = new();

    /// <summary>
    /// A requirement for choosing the class. Kinds: "level" (minimum character level), "quest" (a completed quest id),
    /// "item" (a carried item that answers to the id), "class" (a class the character has). A new kind is
    /// one more case in CheckRequirement (an override in the class file that needs it, or here).
    /// </summary>
    protected void Require(string kind, string value)
    {
        requirementKinds.Add(kind);
        requirementValues.Add(value);
    }

    public string QueryId() => Self.ProgramPath.Substring(Self.ProgramPath.LastIndexOf('/') + 1);
    public string QueryClassName() => ClassName;
    public string QueryTier() => Tier;
    public string QueryParent() => Parent;
    public string QueryTitle() => Title == "" ? ClassName : Title;
    public string QueryTagline() => Tagline;
    public string QueryDescription() => Description;
    public string QueryGuild() => Guild;
    public bool QueryEnabled() => Enabled;
    public bool QueryHidden() => Hidden;
    public string QuerySummonsItem() => Hidden ? SummonsItem : "";
    public string QueryQuestId() => QuestId;
    public string QueryGateKey() => GateKey;
    public List<string> QueryOwnPaths() => new(OwnPaths);
    public bool QuerySummonsOpen() => Hidden && SummonsOpen;
    public int QuerySummonsFloor() => SummonsFloor;
    public double QuerySummonsBase() => SummonsBase;
    public double QuerySummonsStep() => SummonsStep;
    public int QuerySummonsGuarantee() => SummonsGuarantee;
    public int QuerySummonsMinLevel() => SummonsMinLevel;
    public int QuerySerialBase() => SerialBase;
    /// <summary>One of the class's own lines by name (found, text, bound, knock, again, open, look); "" for none.</summary>
    public string QueryLine(string which) => which switch
    {
        "found" => SummonsFound, "text" => SummonsText, "bound" => BoundLine, "knock" => GateKnock, "again" => GateAgain,
        "open" => GateOpen, "look" => GateLook, "reissue" => GateReissue, _ => "",
    };
    /// <summary>The level at which the class gives a skill: 0 (at once) for a skill it names no level for.</summary>
    public int QuerySkillLevel(string skillId) => SkillLevels.GetValueOrDefault(skillId ?? "", 0);
    public int QuerySortOrder() => SortOrder;
    public double QueryExpModifier() => ExpModifier;
    public double QueryAttackRate() => AttackRate;
    public List<string> QuerySkills() => new(Skills);
    public List<string> QueryTraits() => new(Traits);
    public List<string> QueryWeapons() => new(Weapons);
    public List<string> QueryArmorTypes() => new(ArmorTypes);
    public bool QueryShield() => Shield;
    public double QueryStartReaction() => StartReaction;
    public int QueryVitalityBase() => VitalityBase;
    public int QueryVitalityPerLevel() => VitalityPerLevel;
    public int QueryEnduranceBase() => EnduranceBase;
    public int QueryEndurancePerLevel() => EndurancePerLevel;
    public int QueryFocusBase() => FocusBase;
    public int QueryFocusPerLevel() => FocusPerLevel;
    /// <summary>A class modifier in percent ("vitality" 15 is +15%), 0 if the class has none.</summary>
    public double QueryModifier(string key) => Modifiers.TryGetValue(key ?? "", out var value) ? value : 0;
    public Dictionary<string, double> QueryModifiers() => new(Modifiers);
    /// <summary>The starting value of an attribute for this calling: the class's number, or 5.</summary>
    public int QueryStartAttribute(string name) => StartAttributes.TryGetValue(name ?? "", out var value) ? value : 5;
    public int QueryStartGold() => StartGold;
    public List<string> QueryKit() => new(Kit);
    public List<string> QueryRequirementLines() => requirementKinds.Select((k, i) => k + " " + requirementValues[i]).ToList();

    /// <summary>Why this character does not meet the requirements, or null if they all hold. Fails closed on an unknown kind.</summary>
    public string? CheckRequirements(ObjectRef who)
    {
        for (var i = 0; i < requirementKinds.Count; i++)
        {
            var problem = CheckRequirement(requirementKinds[i], requirementValues[i], who);
            if (problem != null) return problem;
        }
        return null;
    }

    protected virtual string? CheckRequirement(string kind, string value, ObjectRef who)
    {
        switch (kind)
        {
            case "level":
                return int.TryParse(value, out var level) && who.Call<int>("QueryLevel") >= level ? null : $"You must be level {value}.";
            case "quest":
                return who.Call<int>("QueryQuest", value) == 2 ? null : "You have not completed the quest this needs.";
            case "item":
                return Present(value, who) != null ? null : "You are not carrying what this needs.";
            case "class":
                return who.Call<bool>("QueryHasClass", value) ? null : "You do not have the class this needs.";
            case "hidden":
                // The offer of a hidden class is open: the register says the character has done the quest and not yet answered.
                return LoadObject("/secure/hidden").Call<bool>("OfferIsOpen", who, value) ? null : "You are not offered this.";
            default:
                return "This class has a requirement the game does not know yet.";
        }
    }
}
