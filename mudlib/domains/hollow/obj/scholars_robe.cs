// @inherit /std/armor

/// <summary>Armour 0, body, light, +1 Insight. Sold by Halbert Quill. Spec: Mage and Priest only, enforced by WornBy (docs/BUILDING.md).</summary>
public class ScholarsRobe : Armor
{
    public override void Create()
    {
        Name = "Scholar's Robe";
        Ids.Add("robe");
        Ids.Add("scholar's robe");
        Ids.Add("scholars robe");
        ShortDesc = "a scholar's robe";
        LongDesc = "A long robe of undyed wool, its hems and cuffs stitched over with the runes a student learns first, in ink-black thread. It stops nothing at all, and it makes the head clearer. It is cut for mages and for the temple folk, and sits oddly on anyone else.";
        ArmorValue = 0;
        Slot = "body";
        ReactionMod = 0.0;
        Category = "light";
        Tags.Add("light"); Tags.Add("cloth");
        WornBy.Add("mage");
        WornBy.Add("priest");
        AttributeBonus["insight"] = 1;
        Magic = "ink-black runes in the hems: +1 Insight while worn";
        Value = 40;
    }
}
