// @inherit /std/armor
public class CrTravelersLeatherWrap : Armor
{
    public override void Create()
    {
        Name = "Traveler's Leather Wrap";
        Ids.Add("wrap");
        Ids.Add("leather wrap");
        Ids.Add("traveler's leather wrap");
        ShortDesc = "a traveler's leather wrap";
        LongDesc = "A belt of treated leather with loops and little pouches for a traveler's odds and ends. It looks as if it could carry a little more than you can. You could {cmd}wear{/} it.";
        Slot = "trinket";
        Tags.Add("trinket"); Tags.Add("leather");
        ArmorValue = 0;
        CarryBonus = 1;   // recorded; no effect until carrying has a limit (the owner's decision)
        Value = 3;
    }
}
