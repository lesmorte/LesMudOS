// @inherit /std/monster

public class WanderingWolf : Monster
{
    public override void Create()
    {
        Name = "Wandering Wolf";
        Ids.Add("wolf");
        ShortDesc = "a wandering wolf";
        LongDesc = "A lean grey wolf with a ragged coat and a scarred muzzle. It moves along the edge of the road with its head low, and its yellow eyes never leave you.";
        Level = 2;
        MaxHp = 16;
        DamageMin = 2;
        DamageMax = 4;
        ToHit = 1;
        Armor = 1;
        XpValue = 25;
        Aggressive = true;
    }
}
