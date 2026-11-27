/// <summary>
/// Base of every skill and spell: an action on the combat clock, defined as data. A skill file lives in /skills,
/// inherits this class (or /std/trait for a passive feature) and sets its numbers in Create(); the id of a skill is
/// its file name. Adding a skill is adding one file. See docs/BUILDING.md for compiled templates.
///
/// The user's own Endurance and Focus are paid when the action begins; Preparation and Recovery are in time units of
/// 2 seconds and go through the same formula as a basic attack. An attack skill (IsAttack) is resolved by the
/// combat engine with the hit roll, dodge, block and critical rolls; the modifiers below shape that attack and
/// OnHit / OnMiss add the rider. Any other skill does its work in Resolve.
/// </summary>
public abstract class Skill : MudObject
{
    /// <summary>The name players see: "Shield Bash".</summary>
    protected string Title = "";
    /// <summary>The word that uses it ("bash"). Empty for a trait.</summary>
    protected string Command = "";
    /// <summary>"skill" (physical, Endurance), "spell" (Focus, subject to spell modifiers) or "trait".</summary>
    protected string Kind = "skill";
    /// <summary>One line for the skills list.</summary>
    protected string Summary = "";
    protected int EnduranceCost;
    protected int FocusCost;
    protected double Preparation;
    protected double Recovery = 0.5;
    protected double ReactionMod;
    /// <summary>Shown to the room when the preparation begins: "gathers power".</summary>
    protected string WindUp = "";
    /// <summary>True if the skill makes a weapon attack (a hit roll and damage).</summary>
    protected bool IsAttack;
    protected int ToHitBonus;
    /// <summary>Critical chance in percent for this attack; -1 uses the basic 5.</summary>
    protected int CritChance = -1;
    /// <summary>Fraction of the damage roll that is dealt (Shield Bash: 0.5).</summary>
    protected double DamageFactor = 1.0;
    protected int DamageMultiplier = 1;
    /// <summary>What the command needs: "enemy" (a target in the room, a fight is started), "friend" (the user or someone in the room),
    /// "item" (something carried or here), "none".</summary>
    protected string Target = "none";
    /// <summary>If not empty, the wielded weapon must be of one of these categories (dagger, sword, spear, axe, mace, staff, bow).</summary>
    protected List<string> WeaponCategories = new();
    /// <summary>Tags the wielded weapon must carry, every one ("ranged" for Aimed Shot); NeedsWeaponText names it in the refusal ("a bow"). docs/specs/tags-and-saves.md.</summary>
    protected List<string> WeaponTags = new();
    protected string NeedsWeaponText = "";
    protected bool NeedsShield;
    protected bool NeedsUnarmed;
    /// <summary>Separate weapon blows one use of an attack skill makes, each with its own hit roll (Double Strike: 2).</summary>
    protected int Hits = 1;
    /// <summary>
    /// True if being hit while this skill's Preparation runs spoils it (Aimed Shot): the action is cancelled and what it cost stays spent.
    /// Nothing else interrupts a preparation but a stagger, which gives half the cost back.
    /// </summary>
    protected bool Spoilable;

    // ---- Ranks (docs/specs/skill-ranks.md) ----
    // Every active skill has ranks I, II, III ... The rank I numbers are the fields above (and Effect lines for numbers of the skill's
    // own); RankRow lines give later ranks. Every number of every rank is read through RankValue, the one shared helper: no skill
    // works out its own scaling. A rank a row does not name, and a number a row leaves out, follows the default step from the rank
    // before: each rank adds EffectStep of the rank I value to an effect and CostStep of the rank I cost to a cost. Keys named in
    // FixedKeys (durations, ranges, counts) do not change unless a row names them. Preparation and Recovery never change.

    /// <summary>Rank r needs level RankLevelStep x (r - 1): rank II at 5, III at 10, IV at 15. The one place the step is named.</summary>
    public const int RankLevelStep = 5;
    /// <summary>The default step of an effect (damage, healing, bonus size) per rank: 30% of the rank I value.</summary>
    public const double EffectStep = 0.30;
    /// <summary>The default step of a resource cost per rank: 15% of the rank I cost.</summary>
    public const double CostStep = 0.15;
    /// <summary>The highest rank this skill has; 0 means every rank the level cap allows (the rule defines them all). 1: rank I only.</summary>
    protected int MaxRank;
    /// <summary>The skill's own rank I numbers by name ("min" 6, "max" 9, "heal" 8), for what the fields above do not hold.</summary>
    protected readonly Dictionary<string, double> Effects = new();
    /// <summary>Numbers that a rank does not change unless a row names them ("hits", "range", "seconds").</summary>
    protected readonly HashSet<string> FixedKeys = new() { "hits", "wait" };
    private readonly Dictionary<int, Dictionary<string, double>> rankRows = new();
    private readonly Dictionary<int, string> rankTitles = new();

    /// <summary>A rank I number of the skill's own: Effect("heal", 8).</summary>
    protected void Effect(string key, double value) => Effects[key] = value;

    /// <summary>
    /// The numbers of one rank as data: RankRow(2, "min=8;max=12;focus=23"). Keys: endurance, focus (costs), tohit, crit, factor, hits,
    /// and the skill's own Effect keys. A key left out follows the default step. title renames the skill at this rank and above
    /// ("Triple Strike"); the command word never changes.
    /// </summary>
    protected void RankRow(int rank, string values, string title = "")
    {
        if (rank < 2) return;
        var row = new Dictionary<string, double>();
        foreach (var part in (values ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq > 0 && ParseNumber(part.Substring(eq + 1), out var v))
                row[part.Substring(0, eq).Trim()] = v;
        }
        rankRows[rank] = row;
        if (title != "") rankTitles[rank] = title;
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

    /// <summary>The level a rank needs: 1 for rank I, RankLevelStep x (rank - 1) after.</summary>
    public static int LevelForRank(int rank) => rank <= 1 ? 1 : RankLevelStep * (rank - 1);

    /// <summary>The highest rank a character of this level may hold, by the cap rule alone.</summary>
    public static int RankCapForLevel(int level) => Math.Max(1, level / RankLevelStep + 1);

    /// <summary>I, II, III, IV ...</summary>
    public static string Roman(int n)
    {
        if (n <= 0) return "";
        var values = new[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
        var words = new[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
        var text = new StringBuilder();
        for (var i = 0; i < values.Length; i++) while (n >= values[i]) { text.Append(words[i]); n -= values[i]; }
        return text.ToString();
    }

    /// <summary>True for a skill with ranks above I (an active skill with a command, not a trait, and not marked rank I only).</summary>
    public bool QueryHasRanks() => Kind != "trait" && Command != "" && MaxRank != 1;

    /// <summary>The highest rank there is (0: no end but the level cap).</summary>
    public int QueryMaxRank() => Kind == "trait" || Command == "" ? 1 : MaxRank;

    /// <summary>Does the skill have this rank at all (ignoring level)?</summary>
    public bool QueryRankExists(int rank) => rank == 1 || (rank > 1 && QueryHasRanks() && (MaxRank == 0 || rank <= MaxRank));

    private double BaseValue(string key) => key switch
    {
        "endurance" => EnduranceCost,
        "focus" => FocusCost,
        "tohit" => ToHitBonus,
        "crit" => CritChance,
        "factor" => DamageFactor,
        "hits" => Hits,
        _ => Effects.TryGetValue(key, out var v) ? v : 0,
    };

    /// <summary>
    /// The one shared helper: a number of the skill at a rank. Rank I is the base; each later rank is its row's number if the row names
    /// it, else the previous rank's plus the default step (CostStep for endurance and focus, EffectStep for the rest, nothing for a
    /// fixed key or a crit of -1, the basic chance) times the rank I value.
    /// </summary>
    public double RankValue(string key, int rank)
    {
        var value = BaseValue(key);
        if (key == "crit" && value < 0) return value;
        var step = FixedKeys.Contains(key) ? 0 : key is "endurance" or "focus" ? CostStep : EffectStep;
        for (var r = 2; r <= rank; r++)
            value = rankRows.TryGetValue(r, out var row) && row.TryGetValue(key, out var named) ? named : value + step * BaseValue(key);
        return value;
    }

    /// <summary>RankValue rounded to the nearest whole number (half away from zero).</summary>
    public int RankInt(string key, int rank) => (int)Math.Round(RankValue(key, rank), MidpointRounding.AwayFromZero);

    /// <summary>The rank this user holds (1 for anyone who has not bought more; creatures use rank I).</summary>
    public int RankOf(ObjectRef? user) => user == null ? 1 : Math.Max(1, user.Call<int>("QuerySkillRank", QueryId()));

    /// <summary>The name at a rank: "Arcane Bolt II", or the rank's own title ("Triple Strike") with no numeral.</summary>
    public string QueryTitleAt(int rank)
    {
        var own = "";
        for (var r = 2; r <= rank; r++) if (rankTitles.TryGetValue(r, out var t)) own = t;
        if (own != "") return own;
        return rank <= 1 || !QueryHasRanks() ? Title : Title + " " + Roman(rank);
    }

    public string QueryTitleFor(ObjectRef? user) => QueryTitleAt(RankOf(user));
    public int QueryEnduranceCostAt(int rank) => RankInt("endurance", rank);
    public int QueryFocusCostAt(int rank) => RankInt("focus", rank);
    public int QueryEnduranceCostFor(ObjectRef? user) => QueryEnduranceCostAt(RankOf(user));
    public int QueryFocusCostFor(ObjectRef? user) => QueryFocusCostAt(RankOf(user));
    public int QueryToHitBonusFor(ObjectRef? user) => RankInt("tohit", RankOf(user));
    public int QueryCritChanceFor(ObjectRef? user) => RankInt("crit", RankOf(user));
    public double QueryDamageFactorFor(ObjectRef? user) => RankValue("factor", RankOf(user));
    public int QueryHitsFor(ObjectRef? user) => Math.Max(1, RankInt("hits", RankOf(user)));

    /// <summary>The rank this user holds, for the engine.</summary>
    public int QueryRankFor(ObjectRef? user) => RankOf(user);

    /// <summary>Seconds the caster must wait after one use before using this skill again (the "wait" Effect; 0: no wait).</summary>
    public int QueryWaitSeconds(int rank) => Math.Max(0, RankInt("wait", rank));

    /// <summary>The damage factor when the skill is the action that starts the fight (the "opener" Effect), or 0 if it has no such bonus.</summary>
    public double QueryOpenerFactorFor(ObjectRef? user) => RankValue("opener", RankOf(user));

    /// <summary>The most, in percent of the target's maximum Vitality, the opener (the action that starts the fight) may deal to a target levelDiff levels below the user (negative: above); 0: no cap. Only Aimed Shot has one.</summary>
    public virtual int QueryOpenerCapPercent(int levelDiff) => 0;

    /// <summary>The damage factor when the attack starts the fight from hiding (the "ambush" Effect: Precision Strike), or 0 if it has no such bonus.</summary>
    public double QueryAmbushFactorFor(ObjectRef? user) => RankValue("ambush", RankOf(user));

    /// <summary>Does being hit while preparing spoil this skill (Aimed Shot)?</summary>
    public bool QuerySpoilable() => Spoilable;

    /// <summary>The summary at a rank. A skill whose summary quotes numbers overrides it.</summary>
    public virtual string QuerySummaryAt(int rank) => Summary;

    // ---- What the engine and the commands read ----

    /// <summary>The id: the file name.</summary>
    public string QueryId() => Self.ProgramPath.Substring(Self.ProgramPath.LastIndexOf('/') + 1);
    public string QueryTitle() => Title;
    public string QueryCommand() => Command;
    public string QueryKind() => Kind;
    public string QuerySummary() => Summary;
    public int QueryEnduranceCost() => EnduranceCost;
    public int QueryFocusCost() => FocusCost;

    // OBSOLETE names, kept so that content written against them still loads: they forward to EnduranceCost and FocusCost.
    protected int StaminaCost { get => EnduranceCost; set => EnduranceCost = value; }
    protected int ManaCost { get => FocusCost; set => FocusCost = value; }
    public int QueryStaminaCost() => EnduranceCost;
    public int QueryManaCost() => FocusCost;
    public double QueryPreparation() => Preparation;
    public double QueryRecovery() => Recovery;
    public double QueryReactionMod() => ReactionMod;
    public string QueryWindUp() => WindUp;
    public bool QueryIsAttack() => IsAttack;
    public int QueryToHitBonus() => ToHitBonus;
    public int QueryCritChance() => CritChance;
    public double QueryDamageFactor() => DamageFactor;
    public int QueryDamageMultiplier() => DamageMultiplier;
    public string QueryTarget() => Target;

    /// <summary>Why the user cannot use this now (wrong weapon, no shield), or null. Extra rules go in Check.</summary>
    public string? CanUse(ObjectRef user, ObjectRef? target)
    {
        if (WeaponCategories.Count > 0)
        {
            var category = user.Call<string>("QueryWeaponCategory") ?? "";
            if (!WeaponCategories.Contains(category))
                return $"{Title} needs " + (WeaponCategories.Count == 1 ? $"a {WeaponCategories[0]}" : "one of: " + string.Join(", ", WeaponCategories)) + " in your hands.";
        }
        if (WeaponTags.Count > 0 && !WeaponTags.All(t => user.Call<bool>("QueryWeaponHasTag", t)))
            return $"{Title} needs {(NeedsWeaponText == "" ? "a fitting weapon" : NeedsWeaponText)} in your hands.";
        if (NeedsShield && !user.Call<bool>("QueryHasShield")) return $"{Title} needs a shield.";
        if (NeedsUnarmed && user.Call<string>("QueryWeaponCategory") != "") return $"{Title} can only be done unarmed.";
        return Check(user, target);
    }

    /// <summary>Extra requirements of a particular skill. Return the refusal text, or null.</summary>
    protected virtual string? Check(ObjectRef user, ObjectRef? target) => null;

    /// <summary>The effect of a non-attack skill, when its preparation is over. target may be null.</summary>
    public virtual void Resolve(ObjectRef user, ObjectRef? target) { }

    /// <summary>After an attack skill's blow has landed (damage is the damage dealt).</summary>
    public virtual void OnHit(ObjectRef user, ObjectRef target, int damage) { }

    /// <summary>After an attack skill's blow has missed or been dodged.</summary>
    public virtual void OnMiss(ObjectRef user, ObjectRef target) { }

    /// <summary>The line the skills list shows, at rank I. delaySeconds is this user's total delay for it.</summary>
    public string Line(double delaySeconds) => LineAt(delaySeconds, 1);

    /// <summary>The line the skills list shows at a rank: name and rank, cost and time, the summary.</summary>
    public string LineAt(double delaySeconds, int rank)
    {
        var cost = new List<string>();
        var endurance = QueryEnduranceCostAt(rank);
        var focus = QueryFocusCostAt(rank);
        if (endurance > 0) cost.Add($"{endurance} Endurance");
        if (focus > 0) cost.Add($"{focus} Focus");
        var timing = Kind == "trait" ? "" : $" [{(cost.Count == 0 ? "free" : string.Join(", ", cost))}; " +
            (Preparation > 0 ? $"wind-up {Num(Preparation * 2)} s, " : "") + $"{Num(delaySeconds)} s]";
        var name = Command == "" ? Title : $"{{cmd}}{Command}{{/}} ({QueryTitleAt(rank)})";
        return $"{name}{timing}: {QuerySummaryAt(rank)}";
    }

    private static string Num(double value)
    {
        var tenths = (long)Math.Round(value * 10, MidpointRounding.AwayFromZero);
        return (tenths / 10) + "." + (Math.Abs(tenths) % 10);
    }
}
