// @inherit /std/find

/// <summary>The split purse in the Borrowed Tomb: a find (a hundred and fifty gold for the first finder, then less), taken once by each character.</summary>
public class FbTombCoins : Find
{
    public override void Create()
    {
        Name = "Tarnished Coins";
        Ids.Add("coins");
        Ids.Add("spilled coins");
        Ids.Add("tarnished coins");
        Ids.Add("purse");
        Ids.Add("spill");
        Ids.Add("spill of coins");
        Ids.Add("spill of tarnished coins");
        ShortDesc = "a spill of tarnished coins";
        LongDesc = "Coins from a split purse, black with tarnish and worn so smooth that no face can be made out on them. A mint that no longer strikes made them, but gold is gold. Picking them up adds them to your purse.";
        TakenShort = "a split purse, its coins gone";
        TakenLong = "A split purse lies among the rust and rag by the seat, with nothing in it now. You have taken what spilled from it.";
        EmptyShort = "a split purse, its coins gone";
        EmptyLong = "A split purse lies among the rust and rag by the seat, with nothing in it. Someone has been here before you, and nothing is left of what spilled from it.";
        GoldLine = "You pick the tarnished coins out of the rust and rag: %gold% gold.";
        Gold = 150;
    }
}
