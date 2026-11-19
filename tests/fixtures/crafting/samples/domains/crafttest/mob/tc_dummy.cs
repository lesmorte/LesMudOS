// @inherit /std/monster
public class CtTcDummy : Monster
{
    public string Skin { get; set; } = "";
    public string Blood { get; set; } = "";

    public override void Create()
    {
        Name = "Test Dummy";
        Ids.Add("dummy");
        ShortDesc = "a test dummy";
        LongDesc = "A creature for the crafting test.";
        Level = 1;
        MaxHp = 1;
        DamageMin = 1;
        DamageMax = 1;
        ToHit = -30;
    }
}
