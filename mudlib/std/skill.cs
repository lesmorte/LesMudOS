/// <summary>
/// Base of every skill and spell: an action on the combat clock, defined as data. A skill file lives in /skills,
/// inherits this class (or /std/trait for a passive feature) and sets its numbers in Create(); the id of a skill is
/// its file name. Adding a skill is adding one file. See docs/BUILDING.md for compiled templates.
///
/// The user's own Stamina and Mana are paid when the action begins; Preparation and Recovery are in time units of
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
    /// <summary>"skill" (physical, Stamina), "spell" (Mana, subject to spell modifiers) or "trait".</summary>
    protected string Kind = "skill";
    /// <summary>One line for the skills list.</summary>
    protected string Summary = "";
    protected int StaminaCost;
    protected int ManaCost;
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
    protected bool NeedsShield;
    protected bool NeedsUnarmed;

    // ---- What the engine and the commands read ----

    /// <summary>The id: the file name.</summary>
    public string QueryId() => Self.ProgramPath.Substring(Self.ProgramPath.LastIndexOf('/') + 1);
    public string QueryTitle() => Title;
    public string QueryCommand() => Command;
    public string QueryKind() => Kind;
    public string QuerySummary() => Summary;
    public int QueryStaminaCost() => StaminaCost;
    public int QueryManaCost() => ManaCost;
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

    /// <summary>The line the skills list shows. delaySeconds is this user's total delay for it.</summary>
    public string Line(double delaySeconds)
    {
        var cost = new List<string>();
        if (StaminaCost > 0) cost.Add($"{StaminaCost} Stamina");
        if (ManaCost > 0) cost.Add($"{ManaCost} Mana");
        var timing = Kind == "trait" ? "" : $" [{(cost.Count == 0 ? "free" : string.Join(", ", cost))}; " +
            (Preparation > 0 ? $"wind-up {Num(Preparation * 2)} s, " : "") + $"{Num(delaySeconds)} s]";
        var name = Command == "" ? Title : $"{{cmd}}{Command}{{/}} ({Title})";
        return $"{name}{timing}: {Summary}";
    }

    private static string Num(double value)
    {
        var tenths = (long)Math.Round(value * 10, MidpointRounding.AwayFromZero);
        return (tenths / 10) + "." + (Math.Abs(tenths) % 10);
    }
}
