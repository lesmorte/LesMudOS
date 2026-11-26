// @inherit /std/find

/// <summary>
/// A rat-chewed purse under a flagstone in the Tavern Cellar: the first search lesson. Hidden (Easy, 7), once per character, 8 gold and then 4, 2 and 1
/// (docs/specs/search.md section 7). It is the only new gold the search puts in the world.
/// </summary>
public class CellarPurse : Find
{
    public override void Create()
    {
        Name = "Rat-Chewed Purse";
        Ids.Add("purse");
        Ids.Add("leather purse");
        Ids.Add("rat-chewed purse");
        Ids.Add("chewed purse");
        ShortDesc = "a rat-chewed purse";
        LongDesc = "A small leather purse, chewed at one corner and furred with dust. It has the weight of a few coins.";
        SearchDifficulty = 7; // Easy (Room.SearchEasy)
        SearchNames = "flagstone,flat stone,stone";
        FoundLine = "You work your fingers under the edge of the stone and lever it up. In a hollow scraped out of the dirt lies a small leather purse, chewed at one corner.";
        Gold = 8;
        FallOff = 50;
        GoldLine = "You shake %gold% gold out of the purse.";
        TakenShort = "an empty rat-chewed purse";
        TakenHere = "An empty rat-chewed purse lies by the barrels.";
        TakenLong = "The purse is empty, and so is the hollow under the stone. You have what it held.";
        EmptyShort = "an empty rat-chewed purse";
        EmptyHere = "An empty rat-chewed purse lies by the barrels.";
        EmptyLong = "The purse is empty. Someone found it before you.";
        TakenLine = "You have already shaken out the purse; there is nothing more in it.";
        EmptyLine = "The purse is empty. Someone found it before you.";
    }
}
