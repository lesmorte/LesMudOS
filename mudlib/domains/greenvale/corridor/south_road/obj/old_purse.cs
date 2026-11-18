// @inherit /std/coins

public class SrOldPurse : Coins
{
    public override void Create()
    {
        Name = "Old Purse";
        Ids.Add("purse");
        Ids.Add("old purse");
        Ids.Add("coins");
        ShortDesc = "a mouldering purse";
        LongDesc = "A small leather purse gone green with age, the strings rotted but the coins inside still good.";
        AmountMin = 15;
        AmountMax = 30;
    }
}
