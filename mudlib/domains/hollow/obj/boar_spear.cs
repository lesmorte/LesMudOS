// @inherit /std/weapon

/// <summary>The improved step of the spear line: sold by Torren.</summary>
public class BoarSpear : Weapon
{
    public override void Create()
    {
        Name = "Boar Spear";
        Ids.Add("spear");
        Ids.Add("boar spear");
        ShortDesc = "a boar spear";
        LongDesc = "A stout ash spear with a broad leaf-shaped head and a crossbar below it, so that an angry boar cannot run up the shaft. It is the kind of spear a man carries when he means to come home.";
        Category = "spear";
        DamageMin = 11;
        DamageMax = 16;
        ToHit = 1;
        ReactionMod = 0.5;
        Recovery = 1.0;
        Value = 50;
    }
}
