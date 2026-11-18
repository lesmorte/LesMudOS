// @inherit /std/armor
public class CtLeatherWrap : Armor
{
    public override void Create()
    {
        Name = "Leather Wrap";
        Ids.Add("wrap");
        Ids.Add("leather wrap");
        ShortDesc = "a leather wrap";
        LongDesc = "A belt of treated leather with loops for tools.";
        Slot = "trinket";
        ArmorValue = 0;
        CarryBonus = 1;
        Value = 4;
    }
}
