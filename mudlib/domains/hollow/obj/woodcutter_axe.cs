// @inherit /std/weapon

public class WoodcutterAxe : Weapon
{
    public override void Create()
    {
        Name = "Woodcutter Axe";
        Ids.Add("axe");
        ShortDesc = "a woodcutter axe";
        LongDesc = "A heavy-headed axe made for felling pines. It will do for goblins as well.";
        DamageMin = 14;
        DamageMax = 21;
        ReactionMod = 1.0;
        Recovery = 1.5;
        Value = 18;
    }
}
