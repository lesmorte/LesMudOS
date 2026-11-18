// @inherit /std/consumable

public class OaBakedApple : Consumable
{
    public override void Create()
    {
        Name = "Honey Baked Apple";
        Ids.Add("apple");
        Ids.Add("baked apple");
        Ids.Add("honey apple");
        ShortDesc = "a honey baked apple";
        LongDesc = "A whole apple baked soft in its skin with honey and a clove, still steaming slightly. A little butter has pooled in the hollow where the core was. You could {cmd}eat{/} it.";
        HealAmount = 5;
        UseVerb = "eat";
        UseMessage = "You eat the baked apple with careful sweetness. Honey runs down your wrist, and you lick it off without shame.";
        Value = 2;
    }
}
