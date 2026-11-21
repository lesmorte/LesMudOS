// @inherit /std/armor

/// <summary>Armour 1, body, light. Sold by Oswy Thackett. Any calling.</summary>
public class PaddedJerkin : Armor
{
    public override void Create()
    {
        Name = "Padded Jerkin";
        Ids.Add("jerkin");
        Ids.Add("padded jerkin");
        Ids.Add("padded");
        ShortDesc = "a padded jerkin";
        LongDesc = "A quilted jerkin of wool and flax, stitched in diamonds and stuffed with fleece. It will not stop a blade, but it will take the sting out of a blow, and any calling may wear it.";
        ArmorValue = 1;
        Slot = "body";
        ReactionMod = 0.0;
        Category = "light";
        Value = 20;
    }
}
