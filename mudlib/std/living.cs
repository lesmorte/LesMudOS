// @inherit /std/thing

/// <summary>
/// One thing a combatant can do: an attack, a potion, a special move. It is data.
/// Total delay = (final Reaction Time + ReactionMod + Recovery) x 2 seconds; the
/// first Preparation x 2 seconds of that are the wind-up, then the action resolves.
/// docs/COMBAT_TIMING.md section 2.
/// </summary>
public class CombatAction
{
    /// <summary>Short key a creature's ChooseAction uses to find it ("slash").</summary>
    public string Id = "";
    /// <summary>What the action is called in stat ("Axe Slash").</summary>
    public string Name = "";
    /// <summary>Wind-up before the effect, in time units (2 seconds each). 0 for basic attacks.</summary>
    public double Preparation;
    /// <summary>How long the action takes to complete and recover from, in time units.</summary>
    public double Recovery;
    /// <summary>Added to the combatant's Reaction Time for this action only.</summary>
    public double ReactionMod;
    /// <summary>Shown to the room when the wind-up begins: a verb phrase, "draws his axe far back".</summary>
    public string WindUp = "";
    /// <summary>Shown to the room when the blow lands, before the roll. Optional.</summary>
    public string Strike = "";
    /// <summary>Damage multiplier of an attack action.</summary>
    public int DamageMultiplier = 1;
    /// <summary>Endurance paid when the action begins.</summary>
    public int EnduranceCost;
    /// <summary>Focus paid when the action begins.</summary>
    public int FocusCost;
    /// <summary>"attack" (a to-hit roll and damage), "use" (a consumable), "skill" (a skill or spell file resolves it), or a builder's own kind handled in Special().</summary>
    public string Kind = "attack";
    /// <summary>Added to the attack roll of an attack action (Precision Strike: +5).</summary>
    public int ToHitBonus;
    /// <summary>Critical chance in percent that replaces the basic chance for this attack; -1 keeps the basic chance.</summary>
    public int CritChance = -1;
    /// <summary>Multiplier on the damage roll as a fraction (Shield Bash does 0.5). Applied after the integer multiplier.</summary>
    public double DamageFactor = 1.0;
    /// <summary>The skill or spell file behind this action, if any. Its OnHit, OnMiss and Resolve are called.</summary>
    public ObjectRef? Skill;
    /// <summary>The consumable of a "use" action.</summary>
    public ObjectRef? Item;
}

/// <summary>
/// A timed effect: one mechanism for every temporary change. It has an id (an effect with the same id replaces
/// the old one), a duration, and modifiers: a speed factor on Reaction Time (haste below 1, slow above 1), an
/// amount of damage it absorbs, and named additive modifiers (see Living.QueryMod for the keys).
/// </summary>
public class TimedEffect
{
    public string Id = "";
    public string Name = "";
    public long Until;
    public double SpeedFactor = 1.0;
    public int Absorb;
    public Dictionary<string, double> Mods = new();
}

/// <summary>
/// Anything that has Vitality and can fight: players, villagers, monsters.
/// Holds the numbers, the equipment slots and the combat engine, which runs on the
/// event clock of docs/COMBAT_TIMING.md: every combatant has a Reaction Time, every
/// action a Preparation and a Recovery, and a combatant's next step is one driver
/// call-out. Set the numbers in Create(); Vitality starts full by itself.
/// </summary>
public abstract class Living : Thing
{
    private const int ForgetMs = 30000; // an enemy that stays out of the room this long is forgotten
    private const int UnitMs = 2000; // one time unit of the brief is 2 seconds
    private static readonly string[] HitYou = { "scratch", "hit", "hit", "smash" };
    private static readonly string[] HitThem = { "scratches", "hits", "hits", "smashes" };
    private static readonly string[] HitHow = { "", "", " hard", " brutally" };
    // A bow does not smash: its own words, by the same four tiers of damage.
    private static readonly string[] ShotYou = { "graze", "shoot", "shoot", "shoot" };
    private static readonly string[] ShotThem = { "grazes", "shoots", "shoots", "shoots" };
    private static readonly string[] ShotHow = { "", "", " squarely", " clean through" };

    [Saved] protected int Level = 1;
    protected int MaxVitality = 10;
    // The three saved resource fields keep their old names, hp, stamina and mana: the names are the keys in the character files,
    // and every saved character must still load. They are private; nothing else sees them.
    [Saved] private int hp = -1; // current Vitality. -1: not yet set, meaning full
    protected int DamageMin = 1;
    protected int DamageMax = 2;
    protected int ToHit;
    protected int Armor;
    [Saved] protected int Gold;
    /// <summary>Base Reaction Time, 1.0 (fast) to 6.0 (slow) in steps of 0.5. When can I begin my next action?</summary>
    protected double BaseReaction = 2.5;

    // Resources (docs/CLASS_SYSTEM.md section 3). Creatures set these in Create(); players get them from their class.
    /// <summary>Maximum Endurance. Spent by physical skills.</summary>
    protected int MaxEndurance = 50;
    /// <summary>Maximum Focus. Spent by spells. Creatures default to none.</summary>
    protected int MaxFocus;
    /// <summary>Endurance regained per 10 seconds.</summary>
    protected double EnduranceRegen = 5;
    /// <summary>Focus regained per 10 seconds.</summary>
    protected double FocusRegen = 2;
    /// <summary>Natural chance in percent to dodge an attack (before any effect or trait).</summary>
    protected int Dodge;
    /// <summary>Natural chance in percent to block a hit with a shield the creature has by nature. Shield-bearers use the shield rule instead.</summary>
    protected int BlockNatural;
    /// <summary>Attack gained per level (Attack = round(AttackRate x Level) + bonuses). Creatures have 1.0 unless they say otherwise.</summary>
    protected double AttackRate = 1.0;
    [Saved] private double stamina = -1; // current Endurance. -1: not yet set, meaning full
    [Saved] private double mana = -1; // current Focus
    private long resourceTime;

    /// <summary>True once Die has run for a creature that does not come back.</summary>
    protected bool Dead;

    private readonly List<ObjectRef> enemies = new();
    // The damage each player has dealt to this creature, capped at the Vitality it actually lost (overkill does not count).
    // It is cleared when the creature is back at full Vitality with no fight in progress. See Experience below.
    private readonly Dictionary<ObjectRef, int> damageDealt = new();
    private readonly Dictionary<ObjectRef, long> absentSince = new();
    private readonly Dictionary<string, ObjectRef> equipment = new();
    private readonly List<CombatAction> actions = new();
    private readonly Dictionary<string, TimedEffect> effects = new();
    private bool wasFighting;

    // The combat clock. Exactly one call-out (timerId) drives a combatant; chain is its token.
    private string phase = "idle"; // idle, preparing (wind-up running), recovering
    private CombatAction? current;
    private ObjectRef? currentTarget;
    private CombatAction? queued;
    private ObjectRef? queuedTarget;
    private int paidEndurance; // what the current action cost when it began (half comes back if it is interrupted)
    private int paidFocus;
    private long actionStart;
    private long resolveAt;
    private long readyAt;
    private int timerId;
    private long timerDue;
    private int chain;
    private int actionsTaken;

    protected int Vitality
    {
        get
        {
            var max = QueryMaxVitality();
            if (hp < 0 || hp > max) hp = max;
            return hp;
        }
        set => hp = Math.Max(0, Math.Min(value, QueryMaxVitality()));
    }

    // ---- Queries other objects use ----

    public bool IsCreature() => true;

    public bool IsDead() => Dead;

    /// <summary>True for players; they cannot fight each other.</summary>
    public virtual bool IsPlayerCharacter() => false;

    public int QueryLevel() => Level;

    public int QueryVitality() => Vitality;

    // ---- Obsolete names ----
    // The resources were called Hp, Stamina and Mana. Content written against the old public names still loads: these forward to the
    // new ones. OBSOLETE: do not use in new content; they will go when no content file uses them.
    /// <summary>OBSOLETE name of MaxVitality.</summary>
    protected int MaxHp { get => MaxVitality; set => MaxVitality = value; }
    /// <summary>OBSOLETE name of Vitality.</summary>
    protected int Hp { get => Vitality; set => Vitality = value; }
    /// <summary>OBSOLETE name of MaxEndurance.</summary>
    protected int MaxStamina { get => MaxEndurance; set => MaxEndurance = value; }
    /// <summary>OBSOLETE name of MaxFocus.</summary>
    protected int MaxMana { get => MaxFocus; set => MaxFocus = value; }
    /// <summary>OBSOLETE name of EnduranceRegen.</summary>
    protected double StaminaRegen { get => EnduranceRegen; set => EnduranceRegen = value; }
    /// <summary>OBSOLETE name of FocusRegen.</summary>
    protected double ManaRegen { get => FocusRegen; set => FocusRegen = value; }
    /// <summary>OBSOLETE name of QueryVitality.</summary>
    public int QueryHp() => QueryVitality();
    /// <summary>OBSOLETE name of QueryMaxVitality.</summary>
    public int QueryMaxHp() => QueryMaxVitality();
    /// <summary>OBSOLETE name of QueryEndurance.</summary>
    public int QueryStamina() => QueryEndurance();
    /// <summary>OBSOLETE name of QueryMaxEndurance.</summary>
    public int QueryMaxStamina() => QueryMaxEndurance();
    /// <summary>OBSOLETE name of QueryFocus.</summary>
    public int QueryMana() => QueryFocus();
    /// <summary>OBSOLETE name of QueryMaxFocus.</summary>
    public int QueryMaxMana() => QueryMaxFocus();

    public virtual int QueryMaxVitality() => MaxVitality + EquipmentVitalityBonus();

    public int QueryGold() => Gold;

    public virtual bool QueryPeaceful() => false;

    /// <summary>How this creature is named in combat messages: "a large rat", "Keith".</summary>
    public virtual string QueryLabel() => Short() ?? Name;

    public int QueryArmor()
    {
        var total = Armor + (int)Math.Round(QueryMod("armor"));
        foreach (var item in QueryEquipment()) total += item.Call<int>("QueryArmorValue");
        return total;
    }

    /// <summary>Bonuses to the attack roll other than level: natural, weapon, effects and traits, and Precision (Grace).</summary>
    public int QueryToHit()
    {
        var weapon = QueryEquipped("weapon");
        return ToHit + (weapon?.Call<int>("QueryToHit") ?? 0) + (int)Math.Round(QueryMod("tohit") + (weapon != null ? QueryMod("tohit_armed") : 0)) + QueryPrecision();
    }

    /// <summary>Attack gained per level. Players take it from their class.</summary>
    public virtual double QueryAttackRate() => AttackRate;

    /// <summary>Attack = round(attack rate x level) + QueryToHit(). The only place the rate is applied.</summary>
    public int QueryAttack() => (int)Math.Floor(QueryAttackRate() * Level + 0.5 + 1e-9) + QueryToHit();

    /// <summary>The defender's side of the hit roll besides armor: one point per level for everyone, plus Evasion (equipment and traits; nothing from Grace since section 8 of docs/ATTRIBUTES.md).</summary>
    public int QueryDefense() => Math.Max(0, Level - 1) + QueryEvasion();

    // ---- Rolls ----

    private static int Clamp(int value, int low, int high) => Math.Max(low, Math.Min(high, value));

    /// <summary>Chance in percent to dodge an attack.</summary>
    public int QueryDodge() => Clamp(Dodge + (int)Math.Round(QueryMod("dodge")), 0, 75);

    /// <summary>Chance in percent to block a hit: 15 with a shield (or the creature's own), plus effects. 0 without one.</summary>
    public int QueryBlock()
    {
        var basis = QueryEquipped("shield") != null ? 15 : BlockNatural;
        if (basis <= 0) return 0;
        return Clamp(basis + (int)Math.Round(QueryMod("block")), 0, 90);
    }

    /// <summary>Chance in percent that a basic hit is a critical hit: 5 plus effects, traits and Critical Chance (Grace).</summary>
    public double QueryCrit() => QueryCritFor(-1);

    /// <summary>
    /// The critical chance of an attack, in percent: the action's own chance (a skill's 15) or the basic 5, plus effects and
    /// traits, plus Critical Chance from Grace and equipment; class modifiers apply to the total. The one place it is worked out.
    /// </summary>
    public double QueryCritFor(int actionChance)
    {
        var chance = (actionChance >= 0 ? actionChance : BaseCritChance) + Math.Round(QueryMod("crit")) + StatRaw("crit");
        chance *= 1 + ClassModifier("crit") / 100.0;
        return Math.Max(0, Math.Min(100, chance));
    }

    /// <summary>The defender's dodge roll, made before the attacker's hit roll. True if the attack is dodged.</summary>
    public bool RollDodge() => Random(100) < QueryDodge();

    private ObjectRef? UsedWeapon() => QueryMod("ignore_weapon") > 0 ? null : QueryEquipped("weapon");

    public int QueryDamageMin() => UsedWeapon() is { } w ? w.Call<int>("QueryDamageMin") : DamageMin + (int)Math.Round(QueryMod("unarmed_min"));

    public int QueryDamageMax() => UsedWeapon() is { } w ? w.Call<int>("QueryDamageMax") : DamageMax + (int)Math.Round(QueryMod("unarmed_max"));

    /// <summary>The category of the wielded weapon ("sword"), or "" when unarmed.</summary>
    public string QueryWeaponCategory() => QueryEquipped("weapon") is { } w ? w.Call<string>("QueryCategory") ?? "" : "";

    public bool QueryHasShield() => QueryEquipped("shield") != null;

    /// <summary>
    /// Healing from a spell: the base amount plus what the caster's traits add (Divine Knowledge) and Healing Power (Spirit and
    /// Insight), the percentages added together. Potions and food are not scaled. The one place spell healing is scaled.
    /// </summary>
    public int ScaleHealing(int amount) => Math.Max(1, (int)Math.Round(amount * (1 + (QueryMod("healing_pct") + QueryStat("healing_power")) / 100.0), MidpointRounding.AwayFromZero));

    /// <summary>
    /// Damage from a spell: the base amount plus what the caster's traits add (Arcane Mastery) and Spell Power (Insight and
    /// Spirit), the percentages added together. The one place spell damage is scaled.
    /// </summary>
    public int ScaleSpellDamage(int amount) => Math.Max(1, (int)Math.Round(amount * (1 + (QueryMod("spell_damage_pct") + QueryStat("spell_power")) / 100.0), MidpointRounding.AwayFromZero));

    /// <summary>
    /// Weapon and unarmed damage after the roll: Physical Power (Might) adds its percentage. The one place physical damage is
    /// scaled; the hit roll, multipliers, critical and block follow.
    /// </summary>
    public int ScalePhysicalDamage(int amount) => Math.Max(1, (int)Math.Round(amount * (1 + QueryStat("physical_power") / 100.0), MidpointRounding.AwayFromZero));

    protected int EquipmentVitalityBonus()
    {
        var total = 0;
        foreach (var item in QueryEquipment()) total += item.Call<int>("QueryMaxVitalityBonus");
        return total;
    }

    /// <summary>One line on how hurt this creature looks.</summary>
    public string HealthLine()
    {
        var percent = Vitality * 100 / Math.Max(1, QueryMaxVitality());
        var state = percent >= 100 ? "is unhurt"
            : percent >= 70 ? "has a few scratches"
            : percent >= 40 ? "is wounded"
            : percent >= 15 ? "is badly wounded"
            : "is near death";
        return $"{Cap(QueryLabel())} {state}.\n";
    }

    public override string Long() => base.Long() + HealthLine();

    public override string Stat() =>
        base.Stat() +
        $"  Level {Level}, Vitality {Vitality}/{QueryMaxVitality()}, gold {Gold}\n" +
        $"  Endurance {QueryEndurance()}/{QueryMaxEndurance()}, Focus {QueryFocus()}/{QueryMaxFocus()}\n" +
        $"  Regeneration per 10 s: Vitality {Num2(QueryVitalityRegen())}, Endurance {Num2(QueryEnduranceRegen())}, Focus {Num2(QueryFocusRegen())}\n" +
        $"  Damage {QueryDamageMin()}-{QueryDamageMax()}, attack {QueryAttack()} (rate {Num(QueryAttackRate())}), defense {QueryDefense()}, armor {QueryArmor()}, dodge {QueryDodge()}%, block {QueryBlock()}%, crit {Pct(QueryCrit())}%\n" +
        "  Attributes " + string.Join(", ", AttributeNames.Select(a => $"{a} {QueryAttribute(a)}")) + "\n" +
        $"  Effects of them: damage +{Pct(QueryStat("physical_power"))}%, to hit +{QueryPrecision()}, evasion +{QueryEvasion()}, crit +{Pct(StatRaw("crit"))}%, spell +{Pct(QueryStat("spell_power"))}%, healing +{Pct(QueryStat("healing_power"))}%, physical resistance {Pct(QueryStat("physical_resistance"))}%, mental resistance {Pct(QueryStat("mental_resistance"))}%\n" +
        EffectsStat() +
        $"  Peaceful {QueryPeaceful()}, enemies {enemies.Count(e => e.IsAlive)}\n" +
        TimingStat();

    /// <summary>Send text to this creature itself. Safe to call from any execution context.</summary>
    protected void Tell(string message) => TellObject(Self, message);

    // ---- Attributes (docs/ATTRIBUTES.md) ----
    // Six attributes on every living thing: Might, Grace, Vigor, Insight, Spirit, Resolve. Each derived statistic acts through how
    // far the attributes behind it stand above 5, the value every character starts from, so that a new character plays as before.
    // The rules read them only through QueryStat and the helpers beside it (QueryPrecision, QueryEvasion, QueryCritFor,
    // ScalePhysicalDamage, ScaleSpellDamage, ScaleHealing, RollPhysicalResistance), so that skill ranks, advanced classes and
    // equipment change a statistic in one place. The rule numbers are these constants.

    /// <summary>The six attributes, in the order they are shown.</summary>
    public static readonly string[] AttributeNames = { "might", "grace", "vigor", "insight", "spirit", "resolve" };

    /// <summary>The value every character starts each attribute from; the derived effects count the points above it.</summary>
    public const int AttributeNeutral = 5;
    /// <summary>The basic critical chance in percent.</summary>
    public const double BaseCritChance = 5;
    private const double DamagePctPerMight = 2;      // Physical Power: weapon and unarmed damage +2% a point of Might above 5
    // Section 8 of docs/ATTRIBUTES.md (the owner's balance revision) set these three: Precision per 8 points, no Evasion from
    // Grace (the statistic stays, for equipment and traits), Critical Chance +0.25% a point.
    private const int GracePerPrecision = 8;         // Precision: +1 to hit for every 8 points of Grace above 5
    private const double CritPctPerGrace = 0.25;     // Critical Chance: +0.25% a point of Grace above 5
    private const double SpellPctPerInsight = 2, SpellPctPerSpirit = 1;   // Spell Power
    private const double HealPctPerSpirit = 2, HealPctPerInsight = 1;     // Healing Power
    private const double ResistPctPerPoint = 2, ResistPctMax = 75;        // Mental (Spirit, Resolve) and Physical (Vigor, Resolve) Resistance
    private const int CarryBase = 50, CarryPerMight = 5;                  // Carry Capacity in pounds (shown, not enforced)
    private const double GatherPerPoint = 1;         // Gathering Efficiency: harvest quality score +1 a point of Grace and of Insight above 5
    private const double CraftShiftPerPoint = 0.02;  // Crafting Aptitude: crafted quality shift +0.02 a point of Insight and of Resolve above 5
    /// <summary>A creature's attribute when its file names none: 4 + level / 2 (whole levels).</summary>
    private int CreatureAttribute() => 4 + Level / 2;

    /// <summary>A creature's own attributes, set in Create() (Attributes["might"] = 12): a bear is mighty, a spirit is not. Those not set follow the template by level.</summary>
    protected readonly Dictionary<string, int> Attributes = new();

    /// <summary>An attribute before equipment and effects. Creatures: their file's number or the template; players override it with their saved values.</summary>
    public virtual int QueryBaseAttribute(string name) =>
        !AttributeNames.Contains(name) ? 0 : Attributes.TryGetValue(name, out var value) ? value : CreatureAttribute();

    /// <summary>
    /// An attribute as the rules use it: the base, plus what equipment adds, plus what traits add (modifier key = the attribute's
    /// name). Timed effects do not change attributes: AddEffect is open to any object, and an attribute is not to be set from outside.
    /// </summary>
    public int QueryAttribute(string name)
    {
        if (!AttributeNames.Contains(name)) return 0;
        var total = QueryBaseAttribute(name) + (int)Math.Round(TraitMod(name));
        foreach (var item in QueryEquipment()) total += item.Call<int>("QueryAttributeBonus", name);
        return Math.Max(0, total);
    }

    /// <summary>All six, by name.</summary>
    public Dictionary<string, int> QueryAttributes() => AttributeNames.ToDictionary(a => a, QueryAttribute);

    /// <summary>Points of an attribute above the neutral 5 (never below 0).</summary>
    private int Above(string name) => Math.Max(0, QueryAttribute(name) - AttributeNeutral);

    /// <summary>A class modifier in percent for a key ("vitality", "crit"). Players sum their classes; creatures have none.</summary>
    protected virtual double ClassModifier(string key) => 0;

    private double EquipmentStat(string key)
    {
        var total = 0.0;
        foreach (var item in QueryEquipment()) total += item.Call<double>("QueryStatBonus", key);
        return total;
    }

    /// <summary>A derived statistic from the attributes and equipment, before class modifiers.</summary>
    private double StatRaw(string key)
    {
        var value = key switch
        {
            "physical_power" => DamagePctPerMight * Above("might"),
            "precision" => Above("grace") / GracePerPrecision,
            "evasion" => QueryMod("evasion"),                   // nothing from Grace (section 8); effects, traits and equipment may add
            "crit" => CritPctPerGrace * Above("grace"),
            "spell_power" => SpellPctPerInsight * Above("insight") + SpellPctPerSpirit * Above("spirit"),
            "healing_power" => HealPctPerSpirit * Above("spirit") + HealPctPerInsight * Above("insight"),
            "mental_resistance" => ResistPctPerPoint * (Above("spirit") + Above("resolve")),
            "physical_resistance" => ResistPctPerPoint * (Above("vigor") + Above("resolve")),
            "carry_capacity" => CarryBase + CarryPerMight * QueryAttribute("might"),
            "gathering" => GatherPerPoint * (Above("grace") + Above("insight")),
            "crafting" => CraftShiftPerPoint * (Above("insight") + Above("resolve")),
            _ => 0.0,
        };
        return value + EquipmentStat(key);
    }

    /// <summary>
    /// A derived statistic as the rules use it, in its own unit: physical_power, spell_power and healing_power in percent added to
    /// damage or healing; precision and evasion in points of the hit roll; crit in percent added to the critical chance;
    /// mental_resistance and physical_resistance in percent chance (at most 75); carry_capacity in pounds; gathering in points of
    /// the harvest quality score; crafting added to the crafted quality shift. Class modifiers apply after the formula.
    /// </summary>
    public double QueryStat(string key)
    {
        var value = StatRaw(key) * (1 + ClassModifier(key) / 100.0);
        if (key is "mental_resistance" or "physical_resistance") value = Math.Max(0, Math.Min(ResistPctMax, value));
        return value;
    }

    /// <summary>Precision: points added to the attack roll.</summary>
    public int QueryPrecision() => (int)Math.Floor(QueryStat("precision") + 1e-9);

    /// <summary>Evasion: points added to the defender's side of the hit roll.</summary>
    public int QueryEvasion() => (int)Math.Floor(QueryStat("evasion") + 1e-9);

    /// <summary>Carry Capacity in pounds. Shown, not enforced: there is no encumbrance yet.</summary>
    public int QueryCarryCapacity() => (int)Math.Round(QueryStat("carry_capacity"), MidpointRounding.AwayFromZero);

    /// <summary>A roll against a chance in percent, to a tenth of a percent.</summary>
    protected bool RollPercent(double chance) => chance > 0 && Random(1000) < (int)Math.Round(chance * 10, MidpointRounding.AwayFromZero);

    /// <summary>Physical Resistance: true if this creature resists a stagger, slow or knockdown. The one place it is rolled.</summary>
    public bool RollPhysicalResistance() => RollPercent(QueryStat("physical_resistance"));

    /// <summary>Mental Resistance: true if this creature shakes off fear, charm, confusion or another mind effect. Ready; nothing uses it yet.</summary>
    public bool RollMentalResistance() => RollPercent(QueryStat("mental_resistance"));

    /// <summary>The statistics as the owner's brief defines them, for display (score): name and value. The rules use QueryStat.</summary>
    public List<(string name, int value)> QueryStatDisplay()
    {
        var a = QueryAttributes();
        return new List<(string, int)>
        {
            ("Physical Power", a["might"] * 2 + Level / 2), ("Precision", a["grace"] * 2), ("Evasion", a["grace"] + Level / 2),
            ("Spell Power", a["insight"] * 2 + a["spirit"]), ("Healing Power", a["spirit"] * 2 + a["insight"]),
            ("Mental Resistance", a["spirit"] + a["resolve"]), ("Physical Resistance", a["vigor"] + a["resolve"]),
            ("Gathering Efficiency", a["grace"] + a["insight"]), ("Crafting Aptitude", a["insight"] + a["resolve"]),
        };
    }

    /// <summary>"his", "her", "its" or "their" for messages. Creatures say "its"; players override it.</summary>
    public virtual string QueryPossessive() => "its";

    /// <summary>A percentage to two decimals at most, without needless zeros: 5, 5.5, 5.25, 100.</summary>
    protected static string Pct(double value)
    {
        var text = Num2(value);
        if (text.EndsWith("0")) text = text.Substring(0, text.Length - 1);
        if (text.EndsWith("0")) text = text.Substring(0, text.Length - 1);
        return text.EndsWith(".") ? text.Substring(0, text.Length - 1) : text;
    }

    // ---- Vitality ----

    /// <summary>Restore Vitality, up to the maximum. Returns how many were restored.</summary>
    public int Heal(int amount)
    {
        if (Dead || amount <= 0) return 0;
        var before = Vitality;
        Vitality = before + amount;
        ForgetDamageIfRecovered();
        return Vitality - before;
    }

    /// <summary>
    /// Lose Vitality from a source that tells its own story (damage over time, say). A creature that reaches 0 dies. The attacker,
    /// if any, becomes an enemy. A player with verbose on is shown the damage actually done: the one who dealt it "(+N damage)",
    /// the one who took it "(-N damage)", each on a line of its own, because the caller has no line to put it on.
    /// Attacks that have a combat line use ApplyDamage and FinishDamage and put DamageNote on that line instead.
    /// </summary>
    public void TakeDamage(int amount, ObjectRef? from)
    {
        var applied = ApplyDamage(amount, from);
        if (applied >= 0)
        {
            if (from != null && from.IsAlive && from != Self && from.Call<string>("DamageNote", applied, true) is { Length: > 0 } dealt) TellObject(from, dealt.TrimStart() + "\n");
            if (DamageNote(applied, false) is { Length: > 0 } taken) Tell(taken.TrimStart() + "\n");
        }
        FinishDamage(from);
    }

    private readonly StringBuilder absorbed = new();

    /// <summary>
    /// The first half of taking damage: the Vitality are lost now and the result is returned, but a death waits for FinishDamage so
    /// that the caller can first say what happened. Returns the damage actually applied (after absorption, and no more than the
    /// Vitality the creature had), or -1 if nothing could be done (already dead, or no damage).
    /// </summary>
    public int ApplyDamage(int amount, ObjectRef? from)
    {
        if (Dead || amount <= 0) return -1;
        ForgetDamageIfRecovered();
        if (from != null && from.IsAlive && from != Self && Environment(from) == Environment()) AddEnemy(from);
        amount = Absorb(amount);
        if (amount <= 0) return 0;
        var applied = Math.Min(amount, Vitality);
        // A player's damage is recorded as the Vitality actually lost, for the sharing of experience.
        if (from != null && from.IsAlive && from != Self && from.Call<bool>("IsPlayerCharacter"))
            damageDealt[from] = damageDealt.GetValueOrDefault(from) + applied;
        Vitality -= amount;
        return applied;
    }

    /// <summary>The second half of taking damage: what absorbed damage has to say, then the death if the creature is out of Vitality.</summary>
    public void FinishDamage(ObjectRef? from)
    {
        if (absorbed.Length > 0) { var text = absorbed.ToString(); absorbed.Clear(); Tell(text); }
        if (!Dead && hp <= 0) Die(from);
    }

    /// <summary>
    /// What verbose adds to a combat line for damage: " {green}(+2 damage){/}" for the one who dealt it, " {red}(-5 damage){/}" for the
    /// one who took it; "" when this creature has verbose off (creatures never have it on).
    /// </summary>
    public virtual string DamageNote(int applied, bool dealt) => "";

    /// <summary>What verbose adds to a line about healing received or given: " {green}(+4 Vitality){/}", or "".</summary>
    public virtual string HealNote(int healed) => "";

    // ---- Experience: shared by damage ----
    // Experience goes by the share of the damage each player dealt, among those still alive, in the room and awake when the
    // creature dies; a small bonus goes to the killing blow if it was a fair share of the work. The numbers are named here, once.

    /// <summary>The percent of a creature's experience that goes to the killing blow, if it earns it.</summary>
    protected const int KillingBlowPercent = 10;

    /// <summary>The killing blow earns its bonus only with at least this percent of an even share of the eligible damage (85 / the number of eligible players).</summary>
    protected const int KillingBlowFairPercent = 85;

    /// <summary>Is this creature too long without input to earn experience? Players override it; nothing else is ever idle.</summary>
    public virtual bool QueryIdle() => false;

    /// <summary>True while a player sits resting (docs/specs/rest.md). Creatures and NPCs never do.</summary>
    public virtual bool QueryResting() => false;

    private void ForgetDamageIfRecovered()
    {
        if (damageDealt.Count > 0 && enemies.Count == 0 && Vitality >= QueryMaxVitality()) damageDealt.Clear();
    }

    /// <summary>
    /// The players who share this creature's death: those with damage recorded who are alive, in its room and not idle, with the
    /// damage each dealt. A player who struck the killing blow with no record (a wizard's "slay") counts alone, as before.
    /// </summary>
    protected List<(ObjectRef who, int damage)> EligibleDamage(ObjectRef? killer)
    {
        var room = Environment();
        var list = new List<(ObjectRef, int)>();
        if (room == null) return list;
        foreach (var kv in damageDealt)
            if (Eligible(kv.Key, room) && kv.Value > 0) list.Add((kv.Key, kv.Value));
        if (list.Count == 0 && killer != null && killer != Self && killer.Call<bool>("IsPlayerCharacter") && Eligible(killer, room)) list.Add((killer, 1));
        return list;
    }

    private bool Eligible(ObjectRef who, ObjectRef room) =>
        who.IsAlive && who.Call<bool>("IsPlayerCharacter") && !who.Call<bool>("IsDead") && who.Call<int>("QueryVitality") > 0 && Environment(who) == room && !who.Call<bool>("QueryIdle");

    /// <summary>
    /// Divide experience among the eligible players by damage. The killing blow (if it came from an eligible player who dealt at least
    /// 85 / N percent of the damage) gets 10% of the whole besides its share of the other 90%; otherwise all of it goes by damage.
    /// Each share is rounded down, at least 1. A lone contributor gets all of it.
    /// </summary>
    protected List<(ObjectRef who, int xp)> DivideExperience(int xp, ObjectRef? killer, List<(ObjectRef who, int damage)> eligible)
    {
        var result = new List<(ObjectRef, int)>();
        if (xp <= 0 || eligible.Count == 0) return result;
        long total = eligible.Sum(e => (long)e.damage);
        var bonusTo = eligible.FindIndex(e => e.who == killer);
        if (bonusTo >= 0 && (long)eligible[bonusTo].damage * eligible.Count * 100 < (long)KillingBlowFairPercent * total) bonusTo = -1;
        for (var i = 0; i < eligible.Count; i++)
        {
            long numerator = bonusTo >= 0
                ? (100 - KillingBlowPercent) * (long)eligible[i].damage + (i == bonusTo ? KillingBlowPercent * total : 0)
                : 100L * eligible[i].damage;
            result.Add((eligible[i].who, (int)Math.Max(1, xp * numerator / (100 * total))));
        }
        return result;
    }

    /// <summary>The creature dies. Monsters, players and bystanders override this.</summary>
    public virtual void Die(ObjectRef? killer)
    {
        if (Dead) return;
        Dead = true;
        ResetCombat();
        Destruct();
    }

    // ---- Equipment ----

    /// <summary>
    /// Put an item from this creature's inventory into its slot. Replaces what was there. No messages. Not public: only the creature's
    /// own code (a player's wear and wield, which check who may use the piece; a creature's Create) dresses it, so no other object can
    /// dress a player in a piece its calling bars.
    /// </summary>
    protected bool Equip(ObjectRef item)
    {
        if (!item.IsAlive || Environment(item) != Self) return false;
        var slot = item.Call<string>("QuerySlot");
        if (string.IsNullOrEmpty(slot)) return false;
        equipment[slot] = item;
        return true;
    }

    public bool Unequip(string slot) => equipment.Remove(slot);

    /// <summary>The item in a slot, or null. Items that left the inventory are no longer equipped.</summary>
    public ObjectRef? QueryEquipped(string slot)
    {
        if (!equipment.TryGetValue(slot, out var item)) return null;
        if (item.IsAlive && Environment(item) == Self) return item;
        equipment.Remove(slot);
        return null;
    }

    public bool IsEquipped(ObjectRef item) => QueryEquipment().Contains(item);

    public List<ObjectRef> QueryEquipment()
    {
        var result = new List<ObjectRef>();
        foreach (var slot in equipment.Keys.ToList())
            if (QueryEquipped(slot) is { } item) result.Add(item);
        return result;
    }

    // ---- Reaction Time ----

    /// <summary>Add equipment's reaction modifiers here; pass 5 adds the load.</summary>
    protected virtual double Encumbrance() => 0;

    /// <summary>Base Reaction Time, before anything else.</summary>
    public double QueryBaseReaction() => BaseReaction;

    /// <summary>The sum of the reaction modifiers of everything equipped.</summary>
    public double QueryEquipmentReaction()
    {
        var total = 0.0;
        foreach (var item in QueryEquipment()) total += QueryItemReaction(item);
        return total;
    }

    /// <summary>One item's reaction modifier for this wearer: a weapon's is reduced by Improvised Combat, never below 0.</summary>
    public double QueryItemReaction(ObjectRef item)
    {
        var mod = item.Call<double>("QueryReactionMod");
        if (mod > 0 && item.Call<string>("QuerySlot") == "weapon") mod = Math.Max(0, mod - QueryMod("weapon_reaction_reduction"));
        return mod;
    }

    /// <summary>The product of all active haste and slow effects (and the traits that speed their owner). 1.0 when there are none.</summary>
    public double QuerySpeedFactor()
    {
        var factor = ExtraSpeedFactor();
        foreach (var effect in LiveEffects()) factor *= effect.SpeedFactor;
        return factor;
    }

    /// <summary>A speed factor that does not come from a timed effect (a trait such as Battle Fury). 1.0 for none.</summary>
    protected virtual double ExtraSpeedFactor() => 1.0;

    /// <summary>Final Reaction Time = (base + equipment modifiers + encumbrance) x speed factor.</summary>
    public double QueryReaction() => (BaseReaction + QueryEquipmentReaction() + Encumbrance()) * QuerySpeedFactor();

    // ---- Timed effects ----

    private List<TimedEffect> LiveEffects()
    {
        var now = TimeMs();
        foreach (var id in effects.Keys.ToList())
            if (effects[id].Until <= now || (effects[id].Mods.Count == 0 && effects[id].Absorb <= 0 && Math.Abs(effects[id].SpeedFactor - 1.0) < 0.0001))
                effects.Remove(id);
        return effects.Values.ToList();
    }

    /// <summary>
    /// A timed effect: the one mechanism for every temporary change. An effect with the same id replaces the old one;
    /// 0 seconds removes it. mods is "key=value;key=value" with keys tohit, tohit_armed, damage, armor, dodge, block, crit
    /// (additive; percentages for dodge, block and crit); speedFactor multiplies Reaction Time (0.8 is 20% haste);
    /// absorb is the number of damage points the effect soaks up before it is used up.
    /// </summary>
    public void AddEffect(string id, string name, int seconds, string mods, double speedFactor, int absorb)
    {
        if (Dead) return;
        if (seconds <= 0) { effects.Remove(id); return; }
        var effect = new TimedEffect
        {
            Id = id, Name = name, Until = TimeMs() + seconds * 1000L,
            SpeedFactor = Math.Max(0.1, Math.Min(10.0, speedFactor)), Absorb = Math.Max(0, absorb),
        };
        foreach (var part in mods.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=');
            if (pair.Length == 2 && ParseNumber(pair[1], out var value)) effect.Mods[pair[0].Trim()] = value;
        }
        effects[id] = effect;
    }

    /// <summary>Parse "-1" or "0.5" with a dot on every machine (mudlib code may not use cultures).</summary>
    private static bool ParseNumber(string text, out double value)
    {
        value = 0;
        text = text.Trim();
        var negative = text.StartsWith("-");
        if (negative) text = text.Substring(1);
        var parts = text.Split('.');
        if (parts.Length > 2 || parts[0].Length == 0 || !parts[0].All(char.IsDigit) || (parts.Length == 2 && (parts[1].Length == 0 || !parts[1].All(char.IsDigit)))) return false;
        var fraction = parts.Length == 2 ? double.Parse(parts[1]) / Math.Pow(10, parts[1].Length) : 0;
        value = (double.Parse(parts[0]) + fraction) * (negative ? -1 : 1);
        return true;
    }

    public void RemoveEffect(string id) => effects.Remove(id);

    public bool QueryHasEffect(string id) => LiveEffects().Any(e => e.Id == id);

    /// <summary>
    /// Haste (factor below 1) or slow (above 1) on Reaction Time, for this many seconds. 20% haste is 0.8,
    /// 20% slow is 1.2. It is a timed effect like any other.
    /// </summary>
    public void AddSpeedEffect(string id, double factor, int seconds)
    {
        // Physical Resistance: a slow may be shaken off. (The wizard's speed command uses AddEffect, which never is.)
        if (factor > 1.0 && seconds > 0 && !Dead && RollPhysicalResistance())
        {
            if (Environment() is { } room) TellRoom(room, $"{Cap(QueryLabel())} shakes off the slowing.\n");
            return;
        }
        AddEffect(id, factor < 1.0 ? "Haste" : "Slow", seconds, "", factor, 0);
    }

    public void RemoveSpeedEffect(string id) => RemoveEffect(id);

    /// <summary>A named modifier: the sum over active effects and the owner's traits and class.</summary>
    public double QueryMod(string key)
    {
        var total = TraitMod(key);
        foreach (var effect in LiveEffects()) if (effect.Mods.TryGetValue(key, out var value)) total += value;
        return total;
    }

    /// <summary>A modifier that comes from the owner's traits and class rather than from an effect. Players override it.</summary>
    protected virtual double TraitMod(string key) => 0;

    private int Absorb(int amount)
    {
        foreach (var effect in LiveEffects())
        {
            if (effect.Absorb <= 0) continue;
            var soaked = Math.Min(effect.Absorb, amount);
            effect.Absorb -= soaked;
            amount -= soaked;
            absorbed.Append($"{effect.Name} absorbs {soaked} damage.\n");
            if (amount <= 0) break;
        }
        return amount;
    }

    /// <summary>The active effects with their remaining seconds, for score: "Blessing (42 s)".</summary>
    public List<string> QueryEffectLines()
    {
        var now = TimeMs();
        return LiveEffects().Select(e => $"{e.Name} ({Math.Max(0, (e.Until - now + 999) / 1000)} s)").ToList();
    }

    private string EffectsStat()
    {
        var lines = QueryEffectLines();
        return lines.Count == 0 ? "" : "  Effects: " + string.Join(", ", lines) + "\n";
    }

    // ---- Resources ----

    public virtual int QueryMaxEndurance() => MaxEndurance;

    public virtual int QueryMaxFocus() => MaxFocus;

    public virtual double QueryEnduranceRegen() => EnduranceRegen;

    public virtual double QueryFocusRegen() => FocusRegen;

    /// <summary>Vitality regained per 10 seconds out of combat. Creatures do not regenerate; players override it.</summary>
    public virtual double QueryVitalityRegen() => 0;

    /// <summary>A number with two decimal places, the same on every machine: 9.90, 0.80.</summary>
    protected static string Num2(double value)
    {
        var hundredths = (long)Math.Round(value * 100, MidpointRounding.AwayFromZero);
        var sign = hundredths < 0 ? "-" : "";
        hundredths = Math.Abs(hundredths);
        return sign + (hundredths / 100) + "." + (hundredths % 100).ToString().PadLeft(2, '0');
    }

    /// <summary>
    /// Bring Endurance and Focus up to the present. Regeneration is lazy: nothing ticks, the amount regained since the last
    /// call is worked out here at the current rate (doubled in a safe room, faster out of combat for those with the trait).
    /// </summary>
    private void SettleResources()
    {
        BeforeSettle();
        SettleCore();
    }

    /// <summary>Called before regeneration is settled: a player uses it to notice that sitting down has ended (the room changed, a fight began).</summary>
    protected virtual void BeforeSettle() { }

    /// <summary>A multiplier on Endurance and Focus regeneration that comes from the character's own state: a seated player's recovery (docs/specs/rest.md).</summary>
    protected virtual double RegenStateFactor() => 1.0;

    /// <summary>Settle regeneration up to now at the rates in force until now. Call it before anything changes a rate.</summary>
    protected void SettleCore()
    {
        var now = TimeMs();
        var maxEndurance = QueryMaxEndurance();
        var maxFocus = QueryMaxFocus();
        if (stamina < 0 || stamina > maxEndurance) stamina = stamina < 0 ? maxEndurance : maxEndurance;
        if (mana < 0 || mana > maxFocus) mana = mana < 0 ? maxFocus : maxFocus;
        if (resourceTime == 0) resourceTime = now;
        var tens = (now - resourceTime) / 10000.0;
        resourceTime = now;
        if (tens <= 0) return;
        var factor = RegenStateFactor();
        var room = Environment();
        if (room != null && room.Call<bool>("QuerySafe")) factor *= 2;
        if (!QueryInCombat()) factor *= 1 + QueryMod("regen_out");
        stamina = Math.Min(maxEndurance, stamina + QueryEnduranceRegen() * factor * tens);
        mana = Math.Min(maxFocus, mana + QueryFocusRegen() * factor * tens);
    }

    public int QueryEndurance() { SettleResources(); return (int)Math.Floor(stamina + 1e-9); }

    public int QueryFocus() { SettleResources(); return (int)Math.Floor(mana + 1e-9); }

    /// <summary>Pay Endurance and Focus together. False, and nothing is paid, if either is short.</summary>
    public bool SpendResources(int enduranceCost, int focusCost)
    {
        SettleResources();
        if (stamina + 1e-9 < enduranceCost || mana + 1e-9 < focusCost) return false;
        stamina -= enduranceCost;
        mana -= focusCost;
        return true;
    }

    /// <summary>Give back resources (an interrupted action), up to the maximum.</summary>
    public void RefundResources(int enduranceAmount, int focusAmount)
    {
        SettleResources();
        stamina = Math.Min(QueryMaxEndurance(), stamina + enduranceAmount);
        mana = Math.Min(QueryMaxFocus(), mana + focusAmount);
    }

    /// <summary>Fill Endurance and Focus.</summary>
    public void FillResources()
    {
        stamina = QueryMaxEndurance();
        mana = QueryMaxFocus();
        resourceTime = TimeMs();
    }

    /// <summary>Set Endurance and Focus to exact values (wizard tools and tests).</summary>
    public void SetResources(int enduranceValue, int focusValue)
    {
        SettleResources();
        stamina = Clamp(enduranceValue, 0, QueryMaxEndurance());
        mana = Clamp(focusValue, 0, QueryMaxFocus());
    }

    /// <summary>The delay of an action in milliseconds: (final Reaction Time + action modifier + action Recovery) x 2 s.</summary>
    private long DelayMs(CombatAction a) => Math.Max(0, (long)Math.Round((QueryReaction() + a.ReactionMod + a.Recovery) * UnitMs));

    /// <summary>The preparation of an action in milliseconds, never longer than its total delay.</summary>
    private long PrepMs(CombatAction a) => Math.Min(DelayMs(a), Math.Max(0, (long)Math.Round(a.Preparation * UnitMs)));

    /// <summary>The delay in seconds of an action of this creature, as stat shows it.</summary>
    public double QueryDelay(CombatAction a) => DelayMs(a) / 1000.0;

    /// <summary>Seconds between this creature's basic actions (its first listed action, or the wielded weapon's attack). For "consider".</summary>
    public double QueryTypicalDelay() => QueryDelay(actions.Count > 0 ? actions[0] : BasicAttack());

    /// <summary>True if the creature has more than one kind of action.</summary>
    public bool QueryVariedAttacks() => actions.Count > 1;

    // ---- Actions ----

    /// <summary>
    /// Declare an action this creature can take. preparation and recovery are in time units (2 s each),
    /// windUp is a verb phrase shown when the preparation begins, damageMultiplier scales an attack.
    /// A creature with no declared action uses the plain attack of its weapon (or its fists).
    /// </summary>
    protected CombatAction AddCombatAction(string id, string name, double preparation = 0, double recovery = 0.5,
        string windUp = "", int damageMultiplier = 1, double reactionMod = 0, string strike = "", string kind = "attack")
    {
        var action = new CombatAction
        {
            Id = id, Name = name, Preparation = preparation, Recovery = recovery, WindUp = windUp,
            DamageMultiplier = damageMultiplier, ReactionMod = reactionMod, Strike = strike, Kind = kind,
        };
        actions.Add(action);
        return action;
    }

    /// <summary>A declared action by id, or null.</summary>
    protected CombatAction? FindAction(string id)
    {
        foreach (var action in actions) if (action.Id == id) return action;
        return null;
    }

    /// <summary>All declared actions.</summary>
    protected List<CombatAction> Actions() => actions;

    /// <summary>The plain attack with the wielded weapon, or unarmed (Recovery 0.5).</summary>
    protected CombatAction BasicAttack()
    {
        var weapon = QueryEquipped("weapon");
        var recovery = weapon != null ? weapon.Call<double>("QueryRecovery") : 0.5;
        return new CombatAction { Id = "attack", Name = weapon != null ? "Attack" : "Unarmed attack", Recovery = recovery };
    }

    /// <summary>
    /// How many actions this creature has chosen since this fight began (0 while choosing the first). Potions
    /// and other queued actions do not count. For patterns: "every third action" is ActionsTaken % 3 == 2.
    /// </summary>
    protected int ActionsTaken() => actionsTaken;

    /// <summary>
    /// The next action when this creature is ready and has an enemy. Players, and creatures with one declared action,
    /// need no override: the default is the first declared action, or the basic attack of the weapon.
    /// </summary>
    protected virtual CombatAction ChooseAction(ObjectRef target) => actions.Count > 0 ? actions[0] : BasicAttack();

    /// <summary>The effect of an action whose kind is not "attack" or "use". Own kinds (a battle cry, a summons) go here.</summary>
    protected virtual void Special(CombatAction action, ObjectRef? target) { }

    // ---- Combat: fights ----

    /// <summary>Why this creature cannot attack the target, or null if it can.</summary>
    public string? CanFight(ObjectRef enemy)
    {
        if (Dead) return "You are in no state to fight.";
        if (!enemy.IsAlive || enemy.Call<bool>("IsDead")) return "That is already dead.";
        if (!enemy.Call<bool>("IsCreature")) return "You cannot fight that.";
        if (enemy == Self) return "Attacking yourself would not help.";
        if (IsPlayerCharacter() && enemy.Call<bool>("IsPlayerCharacter")) return "Players cannot fight each other.";
        var room = Environment();
        if (room == null || Environment(enemy) != room) return "That is not here.";
        if (room.Call<bool>("QuerySafe")) return "A calm lies over this place; nobody may fight here.";
        if (enemy.Call<bool>("QueryPeaceful")) return $"{Cap(enemy.Call<string>("QueryLabel") ?? "It")} is no threat to you.";
        return null;
    }

    /// <summary>Begin fighting. Both sides add each other as enemies. False if it is not allowed.</summary>
    public bool StartFight(ObjectRef enemy) => BeginFight(enemy, false);

    /// <summary>
    /// Start a fight. The one who starts it acts as soon as it is ready. A creature that is drawn in (drawn is
    /// true: it joins a fight someone else started) and the one attacked cannot begin their first action
    /// until their own Reaction Time x 2 seconds have passed.
    /// </summary>
    protected bool BeginFight(ObjectRef enemy, bool drawn)
    {
        if (CanFight(enemy) != null) return false;
        var fresh = !enemies.Contains(enemy);
        if (drawn && !QueryInCombat()) DelayFirstAction();
        AddEnemyCore(enemy);
        enemy.Call("AddEnemy", Self);
        if (!fresh) return true;

        var room = Environment()!;
        var label = QueryLabel();
        var theirs = enemy.Call<string>("QueryLabel") ?? "it";
        Tell($"You attack {theirs}.\n");
        TellObject(enemy, $"{Cap(label)} attacks you!\n");
        TellRoom(room, $"{Cap(label)} attacks {theirs}!\n", Self, enemy);
        foreach (var other in AllInventory(room))
            if (other != Self && other != enemy) other.Call("JoinFight", Self, enemy);
        return true;
    }

    /// <summary>Another creature has made this one its enemy. Unless this one is already fighting, its first action is delayed by its Reaction Time.</summary>
    public void AddEnemy(ObjectRef enemy)
    {
        if (Dead || enemy == Self || !enemy.IsAlive || PreviousObject() != enemy || !enemy.Call<bool>("IsCreature")) return; // only a creature itself makes itself someone's enemy (BeginFight): no other object can start a fight for it
        if (!enemies.Contains(enemy) && !QueryInCombat())
        {
            DelayFirstAction();
            Ambushed(enemy);
        }
        AddEnemyCore(enemy);
    }

    /// <summary>Another creature has just made this one its enemy and no fight was on. A seated player gets up and loses a little more time.</summary>
    protected virtual void Ambushed(ObjectRef enemy) { }

    /// <summary>Put off this one's next action by a further number of seconds.</summary>
    protected void DelayActionBy(int seconds) => readyAt += seconds * 1000L;

    private void AddEnemyCore(ObjectRef enemy)
    {
        if (Dead || enemy == Self || !enemy.IsAlive) return;
        if (!enemies.Contains(enemy)) enemies.Add(enemy);
        absentSince.Remove(enemy);
        wasFighting = true;
        Kick();
    }

    private void DelayFirstAction()
    {
        var until = TimeMs() + (long)Math.Round(QueryReaction() * UnitMs);
        if (until > readyAt) readyAt = until;
    }

    /// <summary>Make this enemy the one attacked first. A player's "kill" uses it to switch targets. False if it is not an enemy.</summary>
    public bool Focus(ObjectRef enemy)
    {
        var index = enemies.IndexOf(enemy);
        if (index < 0) return false;
        if (index > 0)
        {
            enemies.RemoveAt(index);
            enemies.Insert(0, enemy);
        }
        return true;
    }

    public void ForgetEnemy(ObjectRef enemy)
    {
        enemies.Remove(enemy);
        absentSince.Remove(enemy);
        CheckFightOver();
    }

    /// <summary>Drop every enemy, and make them drop this creature.</summary>
    public void ClearEnemies()
    {
        foreach (var enemy in enemies.ToList())
            if (enemy.IsAlive) enemy.Call("ForgetEnemy", Self);
        enemies.Clear();
        absentSince.Clear();
        CheckFightOver();
    }

    /// <summary>Is there an enemy in the room right now?</summary>
    public bool QueryInCombat()
    {
        var room = Environment();
        return room != null && enemies.Any(e => e.IsAlive && Environment(e) == room);
    }

    public bool QueryFightingWith(ObjectRef other) => enemies.Contains(other);

    /// <summary>The enemies in the order they will be attacked.</summary>
    protected List<ObjectRef> Enemies() => enemies.Where(e => e.IsAlive).ToList();

    private ObjectRef? FirstPresentEnemy()
    {
        var room = Environment();
        return room == null ? null : enemies.FirstOrDefault(e => e.IsAlive && Environment(e) == room);
    }

    private void CheckFightOver()
    {
        if (enemies.Count == 0) ForgetDamageIfRecovered();
        if (enemies.Count > 0 || !wasFighting) return;
        wasFighting = false;
        actionsTaken = 0;
        FightEnded();
    }

    /// <summary>Drop dead enemies and enemies that have been away for 30 seconds; a safe room ends every fight.</summary>
    private void PruneEnemies()
    {
        var room = Environment();
        if (room != null && room.Call<bool>("QuerySafe")) { ClearEnemies(); return; }
        var now = TimeMs();
        foreach (var enemy in enemies.ToList())
        {
            if (!enemy.IsAlive || enemy.Call<bool>("IsDead")) { ForgetEnemy(enemy); continue; }
            if (room != null && Environment(enemy) == room) { absentSince.Remove(enemy); continue; }
            if (!absentSince.TryGetValue(enemy, out var since)) absentSince[enemy] = now;
            else if (now - since >= ForgetMs) ForgetEnemy(enemy);
        }
        CheckFightOver();
    }

    /// <summary>The last word on outgoing damage, after the roll and before it is applied.</summary>
    protected virtual int ModifyDamage(int damage, ObjectRef target) => damage;

    /// <summary>Called once when the last enemy is gone and the fight is over.</summary>
    protected virtual void FightEnded() { }

    /// <summary>Per-heartbeat work for subclasses (regeneration, idle actions). Combat does not use heartbeats.</summary>
    protected virtual void Tick() { }

    /// <summary>Should the heartbeat keep running?</summary>
    protected virtual bool NeedsHeartBeat() => false;

    /// <summary>Called when another living thing comes within reach. ThisPlayer() is that thing.</summary>
    protected virtual void Noticed(ObjectRef other) { }

    public override void Init()
    {
        var other = ThisPlayer();
        if (other == null || other == Self || !other.IsAlive) return;
        Noticed(other);
        if (enemies.Contains(other)) Kick(); // an enemy came back within the 30 seconds
    }

    public override void HeartBeat()
    {
        if (Dead) return;
        Tick();
        if (Self.IsAlive && !NeedsHeartBeat()) SetHeartBeat(false);
    }

    // ---- Combat: the clock ----

    /// <summary>Forget everything in progress: the call-out, the current and the queued action. A revived player starts clean.</summary>
    protected void ResetCombat()
    {
        chain++;
        if (timerId != 0) RemoveCallOut(timerId);
        timerId = 0;
        phase = "idle";
        current = null;
        currentTarget = null;
        queued = null;
        readyAt = TimeMs();
        actionsTaken = 0;
    }

    /// <summary>
    /// Make sure the one call-out of this combatant is set for the next thing that has to happen: the end of a wind-up,
    /// or the moment it is ready. Safe to call at any time and as often as you like; it never leaves two call-outs.
    /// </summary>
    private void Kick()
    {
        if (Dead || !Self.IsAlive) return;
        if (phase == "preparing") Schedule(resolveAt);
        else Schedule(readyAt);
    }

    private void Schedule(long at)
    {
        var now = TimeMs();
        var due = Math.Max(at, now);
        if (timerId != 0)
        {
            if (timerDue == due) return;
            RemoveCallOut(timerId);
            timerId = 0;
        }
        var token = ++chain;
        timerDue = due;
        timerId = CallOut(() => OnTimer(token), (due - now) / 1000.0);
    }

    private void OnTimer(int token)
    {
        if (token != chain) return; // an older chain, cancelled
        timerId = 0;
        if (Dead || !Self.IsAlive) return;
        var now = TimeMs();
        if (phase == "preparing")
        {
            var action = current!;
            var target = currentTarget;
            phase = "recovering";
            Resolve(action, target);
            if (Dead || !Self.IsAlive) return;
            Schedule(readyAt);
            return;
        }
        if (now < readyAt) { Schedule(readyAt); return; } // ready time was pushed back
        phase = "idle";
        current = null;
        currentTarget = null;
        TryBegin();
    }

    /// <summary>Ready: take the queued action if there is one, else, with an enemy present, whatever ChooseAction says.</summary>
    private void TryBegin()
    {
        if (Dead || !Self.IsAlive) return;
        PruneEnemies();
        if (Dead || !Self.IsAlive) return;
        var action = queued;
        queued = null;
        var target = FirstPresentEnemy();
        if (action != null && queuedTarget != null && queuedTarget.IsAlive && Environment(queuedTarget) == Environment()) target = queuedTarget;
        queuedTarget = null;
        if (action != null && action.Item != null && (!action.Item.IsAlive || Environment(action.Item) != Self))
        {
            Tell("You no longer have what you meant to use.\n");
            action = null;
        }
        if (action != null && !CanPay(action))
        {
            Tell($"You no longer have the strength or the focus for {action.Name}.\n");
            action = null;
        }
        if (action == null)
        {
            if (target == null)
            {
                // Nothing to do. Remembered enemies that are away are forgotten after 30 seconds.
                if (absentSince.Count > 0) Schedule(absentSince.Values.Min() + ForgetMs);
                return;
            }
            action = ChooseAction(target);
            actionsTaken++;
        }
        Begin(action, target);
    }

    /// <summary>Start an action now: announce the wind-up, resolve at once if there is none, and set the Ready Time.</summary>
    private void Begin(CombatAction action, ObjectRef? target)
    {
        paidEndurance = 0;
        paidFocus = 0;
        if (action.EnduranceCost > 0 || action.FocusCost > 0)
        {
            if (!SpendResources(action.EnduranceCost, action.FocusCost)) { Tell($"You cannot pay for {action.Name}.\n"); return; }
            paidEndurance = action.EnduranceCost;
            paidFocus = action.FocusCost;
        }
        var now = TimeMs();
        var prep = PrepMs(action);
        current = action;
        currentTarget = target;
        actionStart = now;
        resolveAt = now + prep;
        readyAt = now + DelayMs(action);
        var room = Environment();
        if (prep > 0 && action.WindUp != "" && room != null) TellRoom(room, $"{Cap(QueryLabel())} {action.WindUp}...\n");
        if (prep > 0)
        {
            phase = "preparing";
            Schedule(resolveAt);
            return;
        }
        phase = "recovering";
        Resolve(action, target);
        if (Dead || !Self.IsAlive) return;
        Schedule(readyAt);
    }

    /// <summary>The effect of an action. A target that died or left makes it wasted.</summary>
    private void Resolve(CombatAction action, ObjectRef? target)
    {
        var room = Environment();
        if (room == null) return;
        if (action.Kind == "use")
        {
            if (action.Item != null && action.Item.IsAlive && Environment(action.Item) == Self) action.Item.Call("Consume", Self);
            return;
        }
        var present = target != null && target.IsAlive && !target.Call<bool>("IsDead") && (Environment(target) == room || Environment(target) == Self);
        if (action.Kind == "attack")
        {
            if (!present)
            {
                if (action.Preparation > 0) TellRoom(room, $"{Cap(QueryLabel())}'s blow finds nothing to hit.\n");
                PruneEnemies();
                return;
            }
            if (action.Strike != "") TellRoom(room, $"{Cap(QueryLabel())} {action.Strike}\n");
            Attack(target!, action);
        }
        else if (action.Kind == "skill" && action.Skill != null && action.Skill.IsAlive) action.Skill.Call("Resolve", Self, present ? target : null);
        else Special(action, present ? target : null);
        if (!Dead && Self.IsAlive) PruneEnemies();
    }

    private void Attack(ObjectRef target, CombatAction action)
    {
        var room = Environment();
        if (room == null) return;
        var label = Cap(QueryLabel());
        var theirs = target.Call<string>("QueryLabel") ?? "it";

        // 1. The defender's dodge, before the hit roll.
        if (target.Call<bool>("RollDodge"))
        {
            Tell($"{Cap(theirs)} dodges your attack.\n");
            TellObject(target, $"You dodge {label}'s attack.\n");
            TellRoom(room, $"{Cap(theirs)} dodges {label}'s attack.\n", Self, target);
            action.Skill?.Call("OnMiss", Self, target);
            return;
        }

        // 2. d20 + Attack against 10 + Armor + Defense. A natural 20 always hits, a natural 1 always misses.
        var roll = Random(20) + 1;
        var need = 10 + target.Call<int>("QueryArmor") + target.Call<int>("QueryDefense");
        var hit = roll == 20 || (roll != 1 && roll + QueryAttack() + action.ToHitBonus >= need);
        if (!hit)
        {
            Tell($"You miss {theirs}.\n");
            TellObject(target, $"{label} misses you.\n");
            TellRoom(room, $"{label} misses {theirs}.\n", Self, target);
            action.Skill?.Call("OnMiss", Self, target);
            return;
        }

        // 3. Damage: the roll, flat bonuses, the creature's own rule, the action's multipliers.
        var low = QueryDamageMin();
        var high = Math.Max(low, QueryDamageMax());
        var damage = ScalePhysicalDamage(Math.Max(1, low + Random(high - low + 1) + (int)Math.Round(QueryMod("damage"))));
        damage = Math.Max(1, ModifyDamage(damage, target)) * Math.Max(1, action.DamageMultiplier);
        if (Math.Abs(action.DamageFactor - 1.0) > 0.0001) damage = Math.Max(1, (int)Math.Round(damage * action.DamageFactor, MidpointRounding.AwayFromZero));

        // 4. Critical hit, then the defender's block.
        var critical = RollPercent(QueryCritFor(action.CritChance));
        if (critical) damage = Math.Max(1, (int)Math.Round(damage * 1.5, MidpointRounding.AwayFromZero));
        var blocked = Random(100) < target.Call<int>("QueryBlock");
        if (blocked) damage = Math.Max(1, damage / 2);

        var tier = damage <= 2 ? 0 : damage <= 4 ? 1 : damage <= 7 ? 2 : 3;
        // The damage is applied first, so that the lines can say how much of it really was; the death waits until they are told.
        var applied = Math.Max(0, target.Call<int>("ApplyDamage", damage, Self));
        var mine = DamageNote(applied, true);
        var theirNote = target.Call<string>("DamageNote", applied, false) ?? "";
        var shot = QueryWeaponCategory() == "bow";
        var (youWord, themWord, how) = shot ? (ShotYou[tier], ShotThem[tier], ShotHow[tier]) : (HitYou[tier], HitThem[tier], HitHow[tier]);
        Tell($"You {youWord} {theirs}{how}." + (critical ? " A critical hit!" : "") + (blocked ? $" {Cap(theirs)} blocks part of the blow." : "") + mine + "\n");
        TellObject(target, $"{label} {themWord} you{how}." + (critical ? " A critical hit!" : "") + (blocked ? " You block part of the blow." : "") + theirNote + "\n");
        TellRoom(room, $"{label} {themWord} {theirs}{how}." + (critical ? " A critical hit!" : "") + (blocked ? $" {Cap(theirs)} blocks part of the blow." : "") + "\n", Self, target);
        target.Call("FinishDamage", Self);
        action.Skill?.Call("OnHit", Self, target, damage);
    }

    // ---- Combat: interrupts ----

    /// <summary>
    /// The first interrupt (docs/CLASS_SYSTEM.md section 4). An action in its Preparation is cancelled: half its resource
    /// cost is lost (the other half comes back) and half its Recovery is still paid. Otherwise the Ready Time is pushed
    /// back by the given seconds. Returns true if an action was interrupted.
    /// </summary>
    public bool Stagger(double seconds, ObjectRef? by)
    {
        if (Dead || !Self.IsAlive) return false;
        // Physical Resistance: a blow from another creature may fail to stagger.
        if (by != null && by != Self && RollPhysicalResistance())
        {
            if (Environment() is { } steady) TellRoom(steady, $"{Cap(QueryLabel())} keeps {QueryPossessive()} footing.\n");
            return false;
        }
        var now = TimeMs();
        if (phase == "preparing")
        {
            var name = current?.Name ?? "action";
            var halfRecovery = Math.Max(0, readyAt - resolveAt) / 2;
            RefundResources(paidEndurance / 2, paidFocus / 2);
            paidEndurance = 0;
            paidFocus = 0;
            phase = "recovering";
            current = null;
            currentTarget = null;
            readyAt = now + halfRecovery;
            if (Environment() is { } room) TellRoom(room, $"{Cap(QueryLabel())} is thrown off balance, and the {name.ToLowerInvariant()} is interrupted!\n");
            Schedule(readyAt);
            return true;
        }
        readyAt = Math.Max(readyAt, now) + (long)Math.Round(seconds * 1000);
        if (phase == "idle") phase = "recovering";
        if (Environment() is { } here) TellRoom(here, $"{Cap(QueryLabel())} is staggered.\n");
        Kick();
        return false;
    }

    // ---- Combat: skills ----

    /// <summary>Can this action be paid for right now?</summary>
    private bool CanPay(CombatAction action)
    {
        if (action.EnduranceCost <= 0 && action.FocusCost <= 0) return true;
        return QueryEndurance() >= action.EnduranceCost && QueryFocus() >= action.FocusCost;
    }

    /// <summary>
    /// Use a skill or spell as an action on the clock. The skill file supplies the numbers (Preparation, Recovery, the costs,
    /// the attack modifiers). Returns a refusal, or null if the action began or was queued. Costs are paid when it begins.
    /// </summary>
    protected string? UseSkill(ObjectRef skill, ObjectRef? target)
    {
        if (Dead) return "You are in no state to do that.";
        var isSpell = skill.Call<string>("QueryKind") == "spell";
        if (isSpell && QueryMod("nospell") > 0) return "You cannot cast spells like this.";
        var enduranceCost = skill.Call<int>("QueryEnduranceCost");
        var focusCost = skill.Call<int>("QueryFocusCost");
        if (isSpell && focusCost > 0) focusCost = Math.Max(1, (int)Math.Round(focusCost * (1 + QueryMod("spell_cost_pct") / 100.0), MidpointRounding.AwayFromZero));
        var isAttack = skill.Call<bool>("QueryIsAttack");
        var action = new CombatAction
        {
            Id = skill.Call<string>("QueryId") ?? "skill", Name = skill.Call<string>("QueryTitle") ?? "Skill",
            Preparation = skill.Call<double>("QueryPreparation"), Recovery = skill.Call<double>("QueryRecovery"),
            ReactionMod = skill.Call<double>("QueryReactionMod"), WindUp = skill.Call<string>("QueryWindUp") ?? "",
            Kind = isAttack ? "attack" : "skill", EnduranceCost = enduranceCost, FocusCost = focusCost,
            ToHitBonus = skill.Call<int>("QueryToHitBonus"), CritChance = skill.Call<int>("QueryCritChance"),
            DamageFactor = skill.Call<double>("QueryDamageFactor"), DamageMultiplier = Math.Max(1, skill.Call<int>("QueryDamageMultiplier")),
            Skill = skill,
        };
        if (!CanPay(action))
            return QueryEndurance() < action.EnduranceCost ? "You are too tired for that." : "You do not have the focus for that.";
        if (QueryReady())
        {
            queued = null;
            Begin(action, target ?? FirstPresentEnemy());
            return null;
        }
        var replaced = queued != null;
        queued = action;
        queuedTarget = target;
        Tell($"You are not ready yet. You will use {action.Name} as soon as you can" + (replaced ? ", instead of what you had planned" : "") + ". ({cmd}stop{/} cancels.)\n");
        Kick();
        return null;
    }

    // ---- Combat: the queue ----

    /// <summary>Can this creature begin an action this very moment?</summary>
    public bool QueryReady() => phase != "preparing" && TimeMs() >= readyAt;

    /// <summary>
    /// Use a consumable as an action (Recovery from the item). If this creature is ready it begins at once;
    /// otherwise the action is queued, replacing any queued one, and happens at the next Ready Time in place of an attack.
    /// </summary>
    protected void UseAsAction(ObjectRef item)
    {
        var action = new CombatAction
        {
            Id = "use", Name = "Use " + (item.Call<string>("Short") ?? "item"), Kind = "use",
            Recovery = item.Call<double>("QueryRecovery"), Item = item,
        };
        if (QueryReady())
        {
            queued = null;
            Begin(action, FirstPresentEnemy());
            return;
        }
        var replaced = queued != null;
        queued = action;
        queuedTarget = null;
        Tell($"You are not ready yet. You will {item.Call<string>("QueryUseVerb")} {item.Call<string>("Short")} as soon as you can" +
             (replaced ? ", instead of what you had planned" : "") + ". ({cmd}stop{/} cancels.)\n");
        Kick();
    }

    /// <summary>
    /// Do any action on the clock (its Kind is handled in Special, as for a builder's own kind): at once if ready, otherwise queued
    /// for the next Ready Time in place of an attack. waiting completes "You will ... as soon as you can" ("search").
    /// </summary>
    protected void ActAsAction(CombatAction action, string waiting)
    {
        if (QueryReady())
        {
            queued = null;
            Begin(action, null);
            return;
        }
        var replaced = queued != null;
        queued = action;
        queuedTarget = null;
        Tell($"You are not ready yet. You will {waiting} as soon as you can" + (replaced ? ", instead of what you had planned" : "") + ". ({cmd}stop{/} cancels.)\n");
        Kick();
    }

    /// <summary>
    /// Queue an action for the next Ready Time, replacing any queued one, without a message: the next step of a batch of work
    /// (crafting five salves). It begins when the current action's delay is over, like any queued action; stop clears it.
    /// </summary>
    protected void QueueAction(CombatAction action)
    {
        queued = action;
        queuedTarget = null;
        Kick();
    }

    /// <summary>Clear the queued action. True if there was one.</summary>
    protected bool ClearQueued()
    {
        var had = queued != null;
        queued = null;
        return had;
    }

    protected bool HasQueued() => queued != null;

    // ---- Combat: what stat shows ----

    private string ActionLine(CombatAction a) =>
        $"    {a.Name}: preparation {Num(a.Preparation)}, recovery {Num(a.Recovery)}, modifier {Num(a.ReactionMod)}, delay {Num(QueryDelay(a))} s" +
        (a.WindUp != "" ? $", wind-up \"{a.WindUp}\"" : "") + (a.DamageMultiplier > 1 ? $", damage x{a.DamageMultiplier}" : "") + "\n";

    private string TimingStat()
    {
        var text = new StringBuilder();
        text.Append($"  Reaction Time: base {Num(BaseReaction)}, equipment {Num(QueryEquipmentReaction())}, encumbrance {Num(Encumbrance())}, speed x{Num(QuerySpeedFactor())} = final {Num(QueryReaction())}\n");
        text.Append("  Actions:\n");
        if (actions.Count == 0) text.Append(ActionLine(BasicAttack()));
        foreach (var action in actions) text.Append(ActionLine(action));
        var now = TimeMs();
        var state = phase == "preparing" ? $"preparing {current?.Name}, resolves in {Num(Math.Max(0, resolveAt - now) / 1000.0)} s, ready in {Num(Math.Max(0, readyAt - now) / 1000.0)} s"
            : now < readyAt ? $"recovering, ready in {Num((readyAt - now) / 1000.0)} s"
            : "ready";
        text.Append($"  Now: {state}; queued: {(queued == null ? "nothing" : queued.Name)}; actions this fight {actionsTaken}\n");
        return text.ToString();
    }
}
