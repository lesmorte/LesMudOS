// @inherit /std/weapon

public class NovicesMace : Weapon
{
    public override void Create()
    {
        Name = "Novice's Mace";
        Ids.Add("mace");
        Ids.Add("novice's mace");
        ShortDesc = "a novice's mace";
        LongDesc = "A plain iron-headed mace with an ash handle, the kind a temple hands to its newest brothers and sisters. Blunt, honest and hard to break.";
        Category = "mace";
        Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("mace"); Tags.Add("iron");
        DamageMin = 9;
        DamageMax = 13;
        ReactionMod = 0.5;
        Recovery = 1.0;
        Value = 12;
    }
}
