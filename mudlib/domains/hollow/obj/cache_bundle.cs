// @inherit /std/find

/// <summary>A long bundle in oilcloth in the Hidden Cache: a keepsake. Every character may take one Warden's Recurve, once.</summary>
public class CacheBundle : Find
{
    public override void Create()
    {
        Name = "Oilcloth Bundle";
        Ids.Add("bundle");
        Ids.Add("oilcloth");
        Ids.Add("oilcloth bundle");
        Ids.Add("long bundle");
        ShortDesc = "a long bundle wrapped in oilcloth";
        LongDesc = "A long bundle wrapped in greased oilcloth and tied with green cord has been laid carefully across the top of the chests, away from the damp. Whatever is inside it has been looked after better than anything else in the cache.";
        TakenShort = "a long oilcloth wrapping, folded open";
        TakenLong = "The oilcloth lies folded open across the chests, and the green cord is looped neatly beside it. Whatever it held is yours now.";
        ItemLine = "You pick the knots out of the green cord, unroll the oilcloth, and lift out %item%.";
        TakenLine = "You have already unwrapped the bundle; there is only oilcloth left in it.";
        ItemPath = "/domains/hollow/obj/wardens_recurve";
    }
}
