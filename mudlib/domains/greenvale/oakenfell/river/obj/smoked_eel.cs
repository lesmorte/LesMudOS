// @inherit /std/consumable

public class RvSmokedEel : Consumable
{
    public override void Create()
    {
        Name = "Smoked Eel";
        Ids.Add("eel");
        Ids.Add("smoked eel");
        Ids.Add("fish");
        ShortDesc = "a smoked eel";
        LongDesc = "A split River Mere eel, cured over alder smoke until the skin is dark and shiny, folded in a twist of paper. It smells of wood-smoke and salt. You could {cmd}eat{/} it.";
        HealAmount = 8;
        UseVerb = "eat";
        UseMessage = "You peel the smoky flesh from the bone. It is rich, salty and a little oily, and tastes of the river.";
        Value = 3;
    }
}
