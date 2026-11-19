// @inherit /std/monster
public class CtTcBoar : Monster
{
    public string Skin { get; set; } = "wolf";
    public string Blood { get; set; } = "wolf";
    public int CorpseSeconds { get; set; } = 90;   // its own corpse lifetime

    public override void Create()
    {
        Name = "Test Boar";
        Ids.Add("boar");
        ShortDesc = "a test boar";
        LongDesc = "A creature for the crafting test.";
        Level = 2;
        MaxHp = 1;
        DamageMin = 1;
        DamageMax = 1;
        ToHit = -30;
    }
}
