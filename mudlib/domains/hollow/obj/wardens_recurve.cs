// @inherit /std/weapon

/// <summary>The fine step of the bow line: found once by each character in the Hidden Cache.</summary>
public class WardensRecurve : Weapon
{
    public override void Create()
    {
        Name = "Warden's Recurve";
        Ids.Add("bow");
        Ids.Add("recurve");
        Ids.Add("warden's recurve");
        Ids.Add("wardens recurve");
        ShortDesc = "a warden's recurve";
        LongDesc = "A short, deeply curved bow of horn and yew glued up in the old style, its grip wrapped in green cord. It has outlived whoever carried it, and it pulls like it remembers him.";
        Category = "bow";
        DamageMin = 13;
        DamageMax = 19;
        ToHit = 1;
        ReactionMod = 0.5;
        Recovery = 1.0;
        Value = 80;
    }
}
