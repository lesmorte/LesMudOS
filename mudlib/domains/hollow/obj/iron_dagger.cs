// @inherit /std/weapon

public class IronDagger : Weapon
{
    public override void Create()
    {
        Name = "Iron Dagger";
        Ids.Add("dagger");
        ShortDesc = "an iron dagger";
        LongDesc = "A plain iron dagger, light and quick. It sits well in the hand.";
        DamageMin = 6;
        DamageMax = 9;
        ReactionMod = 0.0;
        Recovery = 0.5;
        ToHit = 1;
        Value = 6;
    }
}
