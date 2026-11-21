// @inherit /std/weapon

/// <summary>The improved step of the axe line: sold by Torren. Same timing as the Woodcutter Axe, more damage.</summary>
public class FellingAxe : Weapon
{
    public override void Create()
    {
        Name = "Felling Axe";
        Ids.Add("axe");
        Ids.Add("felling axe");
        ShortDesc = "a felling axe";
        LongDesc = "A long-hafted axe with a wide, keen bit, made to bring down the oldest pines on the north ridge. It is better hung than the woodcutter's and sits heavy on the shoulder, and it asks for the same slow, whole-body swing.";
        Category = "axe";
        DamageMin = 15;
        DamageMax = 23;
        ReactionMod = 1.0;
        Recovery = 1.5;
        Value = 65;
    }
}
