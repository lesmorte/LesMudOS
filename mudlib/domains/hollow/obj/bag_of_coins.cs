// @inherit /std/coins

public class BagOfCoins : Coins
{
    public override void Create()
    {
        Name = "Bag of Coins";
        Ids.Add("bag");
        ShortDesc = "a bag of coins";
        LongDesc = "A fat leather bag that clinks when you touch it. Picking it up adds its coins to your purse.";
        AmountMin = 60;
        AmountMax = 100;
    }
}
