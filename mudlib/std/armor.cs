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
    /// <summary>"light", "medium" or "heavy" for body and head armor. Shields and trinkets are categorised by their slot.</summary>
    protected string Category = "";
    /// <summary>
    /// Extra carrying capacity, recorded on the item (the Traveler's Leather Wrap: 1). There is no carry limit yet, so it has no
    /// effect; appraise and stat show it, and a later carry limit can read QueryCarryBonus().
    /// </summary>
    protected int CarryBonus;

    public int QueryCarryBonus() => CarryBonus;

    public override string QuerySlot() => Slot;

    public int QueryArmorValue() => ArmorValue;

    /// <summary>The access category: "shield" and "trinket" by slot, otherwise light, medium or heavy.</summary>
    public string QueryCategory() => Slot == "shield" || Slot == "trinket" ? Slot : Category;

    public override string Appraisal() =>
        $"{Cap(Short() ?? Name)}: {(QueryCategory() == "" ? "armor" : QueryCategory() + (Slot == "shield" || Slot == "trinket" ? "" : " armor"))}, slot {Slot}. Armor {ArmorValue}, max HP +{MaxHpBonus}, reaction modifier {Num(ReactionMod)}. Worth {QueryValue()} gold." + AppraisalQuality() +
        (Magic != "" ? $" Magical property: {Magic}." : " It holds no magic.") +
        (CarryBonus != 0 ? $" Carry capacity +{CarryBonus} (no effect yet: nothing limits what you carry)." : "") + "\n";

    public int QueryMaxHpBonus() => MaxHpBonus;

    public override double QueryReactionMod() => ReactionMod;

    public override string Stat() => base.Stat() + $"  Armor: {ArmorValue}, slot {Slot}, max HP bonus {MaxHpBonus}, reaction {Num(ReactionMod)}{(CarryBonus != 0 ? $", carry bonus {CarryBonus} (inert)" : "")}\n";
}
