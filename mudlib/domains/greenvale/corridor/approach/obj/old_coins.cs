// @inherit /std/find

/// <summary>The drift of old coins in the Sealed Culvert: a find (seventy gold for the first finder, then less), taken once by each character.</summary>
public class OaOldCoins : Find
{
    public override void Create()
    {
        Name = "Old Coins";
        Ids.Add("coins");
        Ids.Add("old coins");
        Ids.Add("pile");
        Ids.Add("scatter");
        Ids.Add("scatter of coins");
        Ids.Add("scatter of old coins");
        ShortDesc = "a scatter of old coins";
        LongDesc = "A little drift of thick, dull coins lies in the dust, stamped on one face with a ring and a bar and on the other with nothing at all. No mint in Greenvale made them, but gold is gold.";
        TakenShort = "a bare patch of dust where old coins lay";
        TakenHere = "A bare patch of dust lies here where old coins once lay.";
        EmptyHere = "A bare patch of dust lies here where old coins once lay.";
        TakenLong = "A bare patch in the pale dust, scuffed by your own hands, where the drift of old coins lay. You took all you could find; there is nothing more for you.";
        EmptyShort = "a bare patch of dust where old coins lay";
        EmptyLong = "A bare patch in the pale dust, scuffed and trodden, where a drift of old coins must once have lain. Someone has been here before you, and nothing is left but a faint ring-and-bar stamped in the dust by a dropped coin.";
        GoldLine = "You gather the thick, dull coins, stamped with their ring and bar: %gold% gold in all.";
        Gold = 70;
    }
}
