// @inherit /std/coins

public class FbTombCoins : Coins
{
    public override void Create()
    {
        Name = "Tarnished Coins";
        Ids.Add("coins");
        Ids.Add("spilled coins");
        Ids.Add("tarnished coins");
        Ids.Add("purse");
        ShortDesc = "a spill of tarnished coins";
        LongDesc = "Coins from a split purse, black with tarnish and worn so smooth that no face can be made out on them. A mint that no longer strikes made them, but gold is gold. Picking them up adds them to your purse.";
        AmountMin = 90;
        AmountMax = 150;
    }
}
