// @inherit /std/find

/// <summary>The purse in the niche of the Dry Well: a find (thirty gold for the first finder, then less), taken once by each character.</summary>
public class SrOldPurse : Find
{
    public override void Create()
    {
        Name = "Old Purse";
        Ids.Add("purse");
        Ids.Add("old purse");
        Ids.Add("coins");
        Ids.Add("mouldering purse");
        ShortDesc = "a mouldering purse";
        LongDesc = "A small leather purse gone green with age, the strings rotted but the coins inside still good.";
        TakenShort = "a mouldering purse, emptied by you";
        TakenHere = "A mouldering purse lies here, emptied by you.";
        EmptyHere = "A mouldering purse lies here, slack and flat.";
        TakenLong = "The purse you emptied lies slack in the leaves, its strings rotted through and its mouth open. There is nothing more in it for you.";
        EmptyShort = "a mouldering purse, slack and flat";
        EmptyLong = "A small leather purse gone green with age, lying open with its strings rotted through. It is flat. Someone has been here before you, and nothing is left in it.";
        GoldLine = "You loosen what is left of the strings and tip %gold% gold into your hand.";
        Gold = 30;
    }
}
