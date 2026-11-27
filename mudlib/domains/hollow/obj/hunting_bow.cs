// @inherit /std/weapon

public class HuntingBow : Weapon
{
    public override void Create()
    {
        Name = "Hunting Bow";
        Ids.Add("bow");
        Ids.Add("hunting bow");
        ShortDesc = "a hunting bow";
        LongDesc = "A supple yew bow with a worn grip, made for rabbits and rooks. In practiced hands it works just as well on goblins.";
        Category = "bow";
        Tags.Add("ranged"); Tags.Add("piercing"); Tags.Add("bow"); Tags.Add("wood");
        DamageMin = 8;
        DamageMax = 12;
        ToHit = 1;
        ReactionMod = 0.5;
        Recovery = 1.0;
        Value = 20;
    }
}
