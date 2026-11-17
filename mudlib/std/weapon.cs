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

    public override string QuerySlot() => "weapon";

    public int QueryDamageMin() => DamageMin;

    public int QueryDamageMax() => DamageMax;

    public int QueryToHit() => ToHit;

    public override double QueryReactionMod() => ReactionMod;

    public double QueryRecovery() => Recovery;

    public override string Stat() => base.Stat() + $"  Weapon: damage {DamageMin}-{DamageMax}, to hit {ToHit}, reaction {Num(ReactionMod)}, recovery {Num(Recovery)}\n";
}
