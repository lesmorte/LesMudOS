// @inherit /std/armor

/// <summary>Armour 2, body, light. Sold by Oswy Thackett. Spec: all but the Mage. No per-calling restriction exists in /std, so it is in the description only.</summary>
public class LeatherJerkin : Armor
{
    public override void Create()
    {
        Name = "Leather Jerkin";
        Ids.Add("jerkin");
        Ids.Add("leather jerkin");
        Ids.Add("leather");
        ShortDesc = "a leather jerkin";
        LongDesc = "A jerkin of hard-boiled leather, laced at the sides, plain except for a stitched maker's mark. It is stiff for the first week and wonderful after. It is cut heavy in the shoulder: every calling wears it but the mage, whose hands must move freely, and Oswy will say so.";
        ArmorValue = 2;
        Slot = "body";
        ReactionMod = 0.0;
        Category = "light";
        Value = 50;
    }
}
