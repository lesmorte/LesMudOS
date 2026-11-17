// @inherit /std/item

/// <summary>
/// Something worn. Set ArmorValue and Slot ("shield", "body", "head" or
/// "trinket") in Create(); MaxHpBonus and ReactionMod (added to the wearer's Reaction Time) are optional.
/// </summary>
public class Armor : Item
{
    protected int ArmorValue = 1;
    protected string Slot = "body";
    protected int MaxHpBonus;
    /// <summary>Added to the wearer's Reaction Time while worn.</summary>
    protected double ReactionMod;

    public override string QuerySlot() => Slot;

    public int QueryArmorValue() => ArmorValue;

    public int QueryMaxHpBonus() => MaxHpBonus;

    public override double QueryReactionMod() => ReactionMod;

    public override string Stat() => base.Stat() + $"  Armor: {ArmorValue}, slot {Slot}, max HP bonus {MaxHpBonus}, reaction {Num(ReactionMod)}\n";
}
