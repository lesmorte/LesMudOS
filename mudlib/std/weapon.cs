// @inherit /std/item

/// <summary>A wieldable weapon. Set DamageMin, DamageMax and optionally ToHit in Create().</summary>
public class Weapon : Item
{
    protected int DamageMin = 1;
    protected int DamageMax = 2;
    protected int ToHit;

    public override string QuerySlot() => "weapon";

    public int QueryDamageMin() => DamageMin;

    public int QueryDamageMax() => DamageMax;

    public int QueryToHit() => ToHit;

    public override string Stat() => base.Stat() + $"  Weapon: damage {DamageMin}-{DamageMax}, to hit {ToHit}\n";
}
