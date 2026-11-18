// @inherit /std/monster
public class CtTcRabbit : Monster
{
    public string Skin { get; set; } = "rabbit";
    public string Blood { get; set; } = "rabbit";

    public override void Create()
    {
        Name = "Test Rabbit";
        Ids.Add("rabbit");
        ShortDesc = "a test rabbit";
        LongDesc = "A creature for the crafting test.";
        Level = 1;
        MaxHp = 1;
        DamageMin = 1;
        DamageMax = 1;
        ToHit = -30;
    }
}
