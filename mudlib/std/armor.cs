// @inherit /std/item

/// <summary>
/// Something worn. Set ArmorValue and Slot ("shield", "body", "head" or
/// "trinket") in Create(); MaxVitalityBonus and ReactionMod (added to the wearer's Reaction Time) are optional.
/// </summary>
public class Armor : Item
{
    protected int ArmorValue = 1;
    protected string Slot = "body";
    protected int MaxVitalityBonus;
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

    /// <summary>
    /// True for a piece that keeps its wearer off the who list of everyone who is not a wizard (docs/specs/who-and-visibility.md section 5).
    /// It does nothing else: the wearer is still in the room, still hears tells. Set it in Create (`HidesFromWho = true;`) or override it.
    /// </summary>
    protected virtual bool HidesFromWho { get; set; }

    public bool QueryHidesFromWho() => HidesFromWho;

    public override string QuerySlot() => Slot;

    // WornBy and NotWornBy (callings that may or may not wear this piece) are fields of Item.

    public int QueryArmorValue() => ArmorValue;

    /// <summary>The access category: "shield" and "trinket" by slot, otherwise light, medium or heavy.</summary>
    public string QueryCategory() => Slot == "shield" || Slot == "trinket" ? Slot : Category;

    protected override string TagCarrier() => "armour";

    private static readonly string[] ArmourTypes = { "light", "medium", "heavy", "shield", "trinket" };

    /// <summary>A piece whose file states no armour tag has its access category as its tag.</summary>
    protected override void CollectTags(List<string> tags)
    {
        var category = QueryCategory();
        if (category != "" && !tags.Any(t => ArmourTypes.Contains(t))) tags.Add(category);
    }

    /// <summary>" (leather)" for a piece with a material, else "".</summary>
    private string MaterialWords() => QueryTagIn("material") is { Length: > 0 } m ? $" ({m})" : "";

    public override string Appraisal() =>
        $"{Cap(Short() ?? Name)}: {(QueryCategory() == "" ? "armor" : QueryCategory() + (Slot == "shield" || Slot == "trinket" ? "" : " armor") + MaterialWords())}, slot {Slot}. Armor {ArmorValue}, max Vitality +{MaxVitalityBonus}, reaction modifier {Num(ReactionMod)}. Worth {QueryValue()} gold." + AppraisalQuality() +
        (Magic != "" ? $" Magical property: {Magic}." : " It holds no magic.") +
        (CarryBonus != 0 ? $" Carry capacity +{CarryBonus} (no effect yet: nothing limits what you carry)." : "") + "\n";

    public int QueryMaxVitalityBonus() => MaxVitalityBonus;

    // OBSOLETE names (MaxHpBonus), kept so that content written against them still loads.
    protected int MaxHpBonus { get => MaxVitalityBonus; set => MaxVitalityBonus = value; }
    public int QueryMaxHpBonus() => MaxVitalityBonus;

    public override double QueryReactionMod() => ReactionMod;

    public override string Stat() => base.Stat() + $"  Armor: {ArmorValue}, slot {Slot}, max Vitality bonus {MaxVitalityBonus}, reaction {Num(ReactionMod)}{(CarryBonus != 0 ? $", carry bonus {CarryBonus} (inert)" : "")}\n";
}
