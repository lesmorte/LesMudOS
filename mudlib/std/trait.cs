// @inherit /std/skill

/// <summary>
/// A passive feature: no command, no cost. It changes numbers through Mods, a table of named modifiers
/// ("armor" +1, "dodge" +10, "crit" +10, "spell_cost_pct" -20 ...; the keys are listed in docs/BUILDING.md), or, for a
/// conditional bonus, through an override of Modifier. A trait lives in /skills with the skills, inherits this class,
/// and is granted by a class file (or an item). Adding a trait is adding one file.
/// </summary>
public abstract class Trait : Skill
{
    /// <summary>Named modifiers, set in Create().</summary>
    protected Dictionary<string, double> Mods = new();

    protected Trait()
    {
        Kind = "trait";
    }

    /// <summary>The value of one modifier for this owner right now. Override for a condition (heavy armor worn, below half HP).</summary>
    public virtual double Modifier(string key, ObjectRef owner) => Mods.TryGetValue(key, out var value) ? value : 0;
}
