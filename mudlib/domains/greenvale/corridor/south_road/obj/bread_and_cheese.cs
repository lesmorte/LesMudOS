// @inherit /std/consumable

public class SrBreadAndCheese : Consumable
{
    public override void Create()
    {
        Name = "Bread and Cheese";
        Ids.Add("bread");
        Ids.Add("cheese");
        Ids.Add("bread and cheese");
        Ids.Add("loaf");
        ShortDesc = "a heel of bread and a wedge of cheese";
        LongDesc = "A crusty heel of the miller's bread and a wedge of hard yellow cheese, wrapped in a cloth. You could {cmd}eat{/} it.";
        HealAmount = 8;
        UseVerb = "eat";
        UseMessage = "You eat the bread and cheese in a few hungry bites, and brush the crumbs from your coat.";
        Value = 3;
    }
}
