// @inherit /std/monster
public class CtTcGoblin : Monster
{
    public string Skin { get; set; } = "";
    public string Blood { get; set; } = "goblin";

    public override void Create()
    {
        Name = "Test Goblin";
        Ids.Add("goblin");
        ShortDesc = "a test goblin";
        LongDesc = "A creature for the crafting test.";
        Level = 2;
        MaxHp = 1;
        DamageMin = 1;
        DamageMax = 1;
        ToHit = -30;
    }
}
