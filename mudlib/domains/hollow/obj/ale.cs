// @inherit /std/consumable

public class TavernAle : Consumable
{
    public override void Create()
    {
        Name = "Tavern Ale";
        Ids.Add("ale");
        Ids.Add("mug of ale");
        Ids.Add("mug");
        ShortDesc = "a mug of ale";
        LongDesc = "A pewter mug of dark, malty ale with a thick head of foam. You could drink it.";
        HealAmount = 4;
        UseVerb = "drink";
        UseMessage = "You drink the ale down in a few long swallows.";
        Value = 2;
    }
}
