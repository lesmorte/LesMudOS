// @inherit /std/weapon

/// <summary>A rare blade from the hidden cache, sharper and quicker than any ordinary knife in the Hollow.</summary>
public class EnchantedDagger : Weapon
{
    public override void Create()
    {
        Name = "dagger";
        Ids.Add("dagger");
        Ids.Add("enchanted dagger");
        ShortDesc = "an enchanted dagger";
        LongDesc = "A slender dagger of dark steel, its blade faintly etched with lines that catch the light like frost. It balances perfectly in the hand and seems to want to be used.";
        DamageMin = 3;
        DamageMax = 6;
        ToHit = 2;
        Value = 80;
    }
}
