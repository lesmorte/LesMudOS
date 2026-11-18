// @inherit /std/consumable

public class GrCampPasty : Consumable
{
    public override void Create()
    {
        Name = "Camp Pasty";
        Ids.Add("pasty");
        Ids.Add("pie");
        Ids.Add("camp pasty");
        Ids.Add("food");
        ShortDesc = "a camp pasty";
        LongDesc = "A fat half-moon of pastry, crimped at the edge, filled with mutton and onion and still faintly warm from the caravan's pot. You could {cmd}eat{/} it.";
        HealAmount = 8;
        UseVerb = "eat";
        UseMessage = "You bite through the crust into hot mutton and onion. It is the best thing you have eaten since Eldergreen.";
        Value = 6;
    }
}
