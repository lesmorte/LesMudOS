// @inherit /std/weapon

public class FineIronLongsword : Weapon
{
    public override void Create()
    {
        Name = "Fine Iron Longsword";
        Ids.Add("longsword");
        Ids.Add("sword");
        Ids.Add("fine sword");
        ShortDesc = "a fine iron longsword";
        LongDesc = "A beautifully balanced longsword, its iron folded and quenched until it rings. Torren makes one only when the metal sings to him.";
        DamageMin = 12;
        DamageMax = 18;
        ReactionMod = 1.0;
        Recovery = 1.0;
        Value = 60;
    }
}
