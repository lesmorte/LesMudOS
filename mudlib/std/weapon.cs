// @inherit /std/item

/// <summary>
/// A wieldable weapon. Set DamageMin, DamageMax and optionally ToHit in Create(), and its timing:
/// ReactionMod is added to the wielder's Reaction Time (heavy weapons are slow to bring to bear),
/// Recovery is the Recovery of its basic attack (time units of 2 seconds).
/// </summary>
public class Weapon : Item
{
    protected int DamageMin = 1;
    protected int DamageMax = 2;
    protected int ToHit;
    /// <summary>Added to the wielder's Reaction Time while wielded.</summary>
    protected double ReactionMod;
    /// <summary>Recovery of the basic attack with this weapon.</summary>
    protected double Recovery = 0.5;
    /// <summary>"dagger", "sword", "spear", "axe", "mace", "staff" or "bow". Which characters may wield it depends on this.</summary>
    protected string Category = "";

    public override string QuerySlot() => "weapon";

    public int QueryDamageMin() => DamageMin;

    public int QueryDamageMax() => DamageMax;

    public int QueryToHit() => ToHit;

    public override double QueryReactionMod() => ReactionMod;

    public double QueryRecovery() => Recovery;

    public string QueryCategory() => Category;

    public override string Appraisal() =>
        $"{Cap(Short() ?? Name)}: a {(Category == "" ? "weapon" : Category)}. Damage {DamageMin}-{DamageMax}, to hit +{ToHit}, reaction modifier {Num(ReactionMod)}, recovery {Num(Recovery)}. Worth {Value} gold." +
        (Magic != "" ? $" Magical property: {Magic}." : " It holds no magic.") + "\n";

    public override string Stat() => base.Stat() + $"  Weapon: damage {DamageMin}-{DamageMax}, to hit {ToHit}, reaction {Num(ReactionMod)}, recovery {Num(Recovery)}\n";
}
