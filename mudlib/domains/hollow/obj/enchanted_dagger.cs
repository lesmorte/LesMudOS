// @inherit /std/weapon

/// <summary>A rare blade from the hidden cache, sharper and quicker than any ordinary knife in the Hollow.</summary>
public class EnchantedDagger : Weapon
{
    public override void Create()
    {
        Name = "Enchanted Dagger";
        Ids.Add("dagger");
        ShortDesc = "an enchanted dagger";
        LongDesc = "A slender dagger of dark steel, its blade faintly etched with lines that catch the light like frost. It balances perfectly in the hand and seems to want to be used.";
        DamageMin = 7;
        DamageMax = 11;
        ReactionMod = 0.0;
        Recovery = 0.5;
        ToHit = 2;
        Value = 80;
    }
}
