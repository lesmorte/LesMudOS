// @inherit /std/consumable

public class SgStagStew : Consumable
{
    public override void Create()
    {
        Name = "Stag Stew";
        Ids.Add("stew");
        Ids.Add("bowl of stew");
        Ids.Add("venison stew");
        Ids.Add("stag stew");
        ShortDesc = "a bowl of venison stew";
        LongDesc = "A thick venison stew with root vegetables and barley, served in a pewter bowl with a heel of black bread. It steams invitingly. You could {cmd}eat{/} it.";
        HealAmount = 14;
        UseVerb = "eat";
        UseMessage = "You eat the rich venison stew down to the last drop and mop the bowl with bread.";
        Value = 5;
    }
}
