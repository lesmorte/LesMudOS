// @inherit /std/consumable

public class SgHoneyedNuts : Consumable
{
    public override void Create()
    {
        Name = "Honeyed Nuts";
        Ids.Add("nuts");
        Ids.Add("honeyed nuts");
        Ids.Add("bag of nuts");
        Ids.Add("bag");
        ShortDesc = "a bag of honeyed nuts";
        LongDesc = "A twist of paper filled with hazelnuts and almonds roasted in honey and salt. Sticky and sweet. You could {cmd}eat{/} them.";
        HealAmount = 4;
        UseVerb = "eat";
        UseMessage = "You crunch through the honeyed nuts, licking your fingers.";
        Value = 2;
    }
}
