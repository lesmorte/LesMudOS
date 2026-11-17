// @inherit /std/consumable

public class TavernStew : Consumable
{
    public override void Create()
    {
        Name = "Tavern Stew";
        Ids.Add("stew");
        Ids.Add("bowl of stew");
        ShortDesc = "a bowl of stew";
        LongDesc = "A thick bowl of mutton stew with carrots, barley and a heel of brown bread. It steams in the cold air. You could {cmd}eat{/} it.";
        HealAmount = 10;
        UseVerb = "eat";
        UseMessage = "You spoon up the hot stew until the bowl is empty.";
        Value = 4;
    }
}
