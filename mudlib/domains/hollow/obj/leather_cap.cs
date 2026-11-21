// @inherit /std/armor

/// <summary>Armour 1, head, light. Sold by Oswy Thackett. Any calling. The quest helm has the same armour.</summary>
public class LeatherCap : Armor
{
    public override void Create()
    {
        Name = "Leather Cap";
        Ids.Add("cap");
        Ids.Add("leather cap");
        ShortDesc = "a leather cap";
        LongDesc = "A close-fitting cap of boiled leather with ear flaps and a strap that buckles under the chin. It is not a helm, but it is a good deal better than a hat.";
        ArmorValue = 1;
        Slot = "head";
        ReactionMod = 0.0;
        Category = "light";
        Value = 25;
    }
}
