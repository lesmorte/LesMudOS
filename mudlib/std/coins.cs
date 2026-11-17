// @inherit /std/item

/// <summary>
/// Gold on the floor. Becomes gold in the purse of whoever picks it up. Set
/// Amount, or AmountMin and AmountMax for a random pile (rolled when first seen).
/// </summary>
public class Coins : Item
{
    protected int Amount;
    protected int AmountMin;
    protected int AmountMax;

    private void Roll()
    {
        if (Amount <= 0 && AmountMax > 0) Amount = AmountMin + Random(Math.Max(1, AmountMax - AmountMin + 1));
        if (Amount <= 0) Amount = 1;
    }

    public int QueryAmount()
    {
        Roll();
        return Amount;
    }

    public override bool Id(string id) => id is "coins" or "gold" or "gold coins" or "pile of gold coins" || base.Id(id);

    public override string? Short()
    {
        if (ShortDesc != null) return ShortDesc;
        Roll();
        return Amount == 1 ? "a gold coin" : $"{Amount} gold coins";
    }

    public override string Long() => $"{Cap(Short()!)}, glittering. Pick them up to add them to your purse.\n";

    protected override void OnGet(ObjectRef taker)
    {
        Roll();
        taker.Call("AddGold", Amount);
        TellObject(taker, $"You add {Amount} gold to your purse.\n");
        Destruct();
    }
}
