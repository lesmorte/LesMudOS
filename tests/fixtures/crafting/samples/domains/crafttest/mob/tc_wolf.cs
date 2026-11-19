// @inherit /std/monster
public class CtTcWolf : Monster
{
    public string Skin { get; set; } = "wolf";
    public string Blood { get; set; } = "wolf";

    public override void Create()
    {
        Name = "Test Wolf";
        Ids.Add("wolf");
        ShortDesc = "a test wolf";
        LongDesc = "A creature for the crafting test.";
        Level = 2;
        MaxHp = 1;
        DamageMin = 1;
        DamageMax = 1;
        ToHit = -30;
    }
}
