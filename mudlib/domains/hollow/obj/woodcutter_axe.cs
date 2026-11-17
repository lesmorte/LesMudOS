// @inherit /std/weapon

public class WoodcutterAxe : Weapon
{
    public override void Create()
    {
        Name = "Woodcutter Axe";
        Ids.Add("axe");
        ShortDesc = "a woodcutter axe";
        LongDesc = "A heavy-headed axe made for felling pines. It will do for goblins as well.";
        DamageMin = 4;
        DamageMax = 7;
        Value = 18;
    }
}
