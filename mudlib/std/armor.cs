// @inherit /std/item

/// <summary>
/// Something worn. Set ArmorValue and Slot ("shield", "body", "head" or
/// "trinket") in Create(); MaxHpBonus is optional.
/// </summary>
public class Armor : Item
{
    protected int ArmorValue = 1;
    protected string Slot = "body";
    protected int MaxHpBonus;

    public override string QuerySlot() => Slot;

    public int QueryArmorValue() => ArmorValue;

    public int QueryMaxHpBonus() => MaxHpBonus;

    public override string Stat() => base.Stat() + $"  Armor: {ArmorValue}, slot {Slot}, max HP bonus {MaxHpBonus}\n";
}
