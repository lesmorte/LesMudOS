// @inherit /std/armor

public class SmallShield : Armor
{
    public override void Create()
    {
        Name = "Small Shield";
        Ids.Add("shield");
        ShortDesc = "a small shield";
        LongDesc = "A round wooden shield bound with an iron rim. Small, but it turns a blow.";
        ArmorValue = 1;
        Slot = "shield";
        Tags.Add("shield"); Tags.Add("wood");
        ReactionMod = 0.5;
        Value = 12;
    }
}
