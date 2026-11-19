// @inherit /std/armor
public class HcTravelersLeatherWrap : Armor
{
    public override void Create()
    {
        Name = "Traveler's Leather Wrap";
        Ids.Add("wrap");
        Ids.Add("leather wrap");
        ShortDesc = "a traveler's leather wrap";
        LongDesc = "A belt of treated leather with loops and pouches for a traveler's odds and ends.";
        Slot = "trinket";
        ArmorValue = 0;
        CarryBonus = 1;   // recorded; no effect until carrying has a limit
        Value = 3;
    }
}
