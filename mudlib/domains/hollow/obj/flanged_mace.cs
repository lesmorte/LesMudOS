// @inherit /std/weapon

/// <summary>The improved step of the mace line: sold by Torren.</summary>
public class FlangedMace : Weapon
{
    public override void Create()
    {
        Name = "Flanged Mace";
        Ids.Add("mace");
        Ids.Add("flanged mace");
        ShortDesc = "a flanged mace";
        LongDesc = "An iron mace whose head is forged with seven thin flanges, each one a hand's breadth of bite. It is no heavier than a novice's mace, and a good deal more honest about what it is for.";
        Category = "mace";
        Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("mace"); Tags.Add("iron");
        DamageMin = 12;
        DamageMax = 17;
        ReactionMod = 0.5;
        Recovery = 1.0;
        Value = 55;
    }
}
