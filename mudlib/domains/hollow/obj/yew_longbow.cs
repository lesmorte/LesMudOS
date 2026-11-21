// @inherit /std/weapon

/// <summary>The improved step of the bow line: sold at the Strung Yew. Same timing and to-hit as the Hunting Bow.</summary>
public class YewLongbow : Weapon
{
    public override void Create()
    {
        Name = "Yew Longbow";
        Ids.Add("bow");
        Ids.Add("longbow");
        Ids.Add("yew bow");
        Ids.Add("yew longbow");
        ShortDesc = "a yew longbow";
        LongDesc = "A tall bow of seasoned yew, heartwood on the inside of the stave and sapwood on the outside, waxed dark by use. It draws harder than a hunting bow, and Wenna Farrow strings every one herself.";
        Category = "bow";
        DamageMin = 11;
        DamageMax = 16;
        ToHit = 1;
        ReactionMod = 0.5;
        Recovery = 1.0;
        Value = 60;
    }
}
