// @inherit /std/weapon

public class IronDagger : Weapon
{
    public override void Create()
    {
        Name = "Iron Dagger";
        Ids.Add("dagger");
        ShortDesc = "an iron dagger";
        LongDesc = "A plain iron dagger, light and quick. It sits well in the hand.";
        DamageMin = 2;
        DamageMax = 4;
        ToHit = 1;
        Value = 6;
    }
}
