// @inherit /std/find

/// <summary>
/// A long bundle in oilcloth in the Hidden Cache: a keepsake. Every character may take one Warden's Recurve, once. It is hidden: a character does not see it
/// until a search of the room finds it (Hard, 17: the owner's decision of 26 November 2026; docs/specs/search.md section 6). A character the register
/// already lists as having taken it sees it as before.
/// </summary>
public class CacheBundle : Find
{
    public override void Create()
    {
        Name = "Oilcloth Bundle";
        Ids.Add("bundle");
        Ids.Add("oilcloth");
        Ids.Add("oilcloth bundle");
        Ids.Add("long bundle");
        Ids.Add("cord");
        Ids.Add("green cord");
        Ids.Add("wrapping");
        Ids.Add("knots");
        Ids.Add("oilcloth wrapping");
        SearchDifficulty = 17; // Hard (Room.SearchHard)
        SearchNames = "sacking,rotted sacking,chests,stack,hump";
        FoundLine = "You lift the rotted sacking and find, laid along the top of the stack where the damp could not reach it, a long bundle wrapped in greased oilcloth and tied with green cord.";
        ShortDesc = "a long bundle wrapped in oilcloth";
        LongDesc = "A long bundle wrapped in greased oilcloth and tied with green cord has been laid carefully across the top of the chests, away from the damp. Whatever is inside it has been looked after better than anything else in the cache.";
        TakenShort = "a long oilcloth wrapping, folded open";
        TakenHere = "A long oilcloth wrapping lies folded open here.";
        TakenLong = "The oilcloth lies folded open across the chests, and the green cord is looped neatly beside it. Whatever it held is yours now.";
        ItemLine = "You pick the knots out of the green cord, unroll the oilcloth, and lift out %item%.";
        TakenLine = "You have already unwrapped the bundle; there is only oilcloth left in it.";
        ItemPath = "/domains/hollow/obj/wardens_recurve";
    }
}
