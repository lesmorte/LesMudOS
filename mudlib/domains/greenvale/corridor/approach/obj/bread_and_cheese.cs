// @inherit /std/consumable

public class OaBreadAndCheese : Consumable
{
    public override void Create()
    {
        Name = "Bread and Cheese";
        Ids.Add("bread");
        Ids.Add("cheese");
        Ids.Add("bread and cheese");
        ShortDesc = "a farmhouse bread and cheese";
        LongDesc = "A thick heel of warm farm bread wrapped round a wedge of hard yellow cheese, in a clean cloth. It is simple, filling and exactly what a long road calls for. You could {cmd}eat{/} it.";
        HealAmount = 10;
        UseVerb = "eat";
        UseMessage = "You eat the bread and cheese on the move. The cheese is sharp, the bread is still warm, and for a moment the road is a long way off.";
        Value = 4;
    }
}
