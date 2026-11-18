// @inherit /std/coins

public class OaOldCoins : Coins
{
    public override void Create()
    {
        Name = "Old Coins";
        Ids.Add("coins");
        Ids.Add("old coins");
        Ids.Add("pile");
        ShortDesc = "a scatter of old coins";
        LongDesc = "A little drift of thick, dull coins lies in the dust, stamped on one face with a ring and a bar and on the other with nothing at all. No mint in Greenvale made them, but gold is gold.";
        AmountMin = 40;
        AmountMax = 70;
    }
}
