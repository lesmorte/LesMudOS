// @inherit /std/consumable

public class SgStagAmber : Consumable
{
    public override void Create()
    {
        Name = "Stag Amber";
        Ids.Add("amber");
        Ids.Add("ale");
        Ids.Add("stag amber");
        Ids.Add("mug");
        Ids.Add("mug of amber");
        ShortDesc = "a mug of Stag Amber";
        LongDesc = "A pewter mug of the inn's own amber ale, nutty and dark gold with a creamy head. A silver stag is stamped on the mug. You could {cmd}drink{/} it.";
        HealAmount = 5;
        UseVerb = "drink";
        UseMessage = "You drink the amber ale in long, grateful swallows.";
        Value = 2;
    }
}
