// @inherit /std/weapon

public class OverseersAxe : Weapon
{
    public override void Create()
    {
        Name = "Goblin Overseer's Axe";
        Ids.Add("axe");
        Ids.Add("overseer axe");
        Ids.Add("overseers axe");
        Ids.Add("overseer's axe");
        ShortDesc = "the Goblin Overseer's axe";
        LongDesc = "A brutal, broad-bladed axe with a haft wrapped in strips of dark leather. Crude crow symbols are scratched into the metal.";
        DamageMin = 17;
        DamageMax = 25;
        ReactionMod = 1.5;
        Recovery = 1.5;
        Category = "axe";
        Tags.Add("melee"); Tags.Add("slashing"); Tags.Add("axe"); Tags.Add("iron");
        Value = 70;
    }
}
