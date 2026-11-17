// @inherit /std/coins

/// <summary>The smugglers' old hoard in the hidden cache: a random pile of coins that is placed once and never restocked.</summary>
public class CacheGold : Coins
{
    public override void Create()
    {
        Name = "Old Coins";
        Ids.Add("old coins");
        Ids.Add("heap");
        ShortDesc = "a heap of old coins";
        LongDesc = "A heap of tarnished coins, mostly silver and copper, stamped with the marks of towns that have long since changed their names. Picking it up adds the coins to your purse.";
        AmountMin = 30;
        AmountMax = 60;
    }
}
