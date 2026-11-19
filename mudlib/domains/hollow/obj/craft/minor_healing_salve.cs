// @inherit /std/crafted
public class CrMinorHealingSalve : Crafted
{
    public override void Create()
    {
        Name = "Minor Healing Salve";
        Ids.Add("salve");
        Ids.Add("healing salve");
        Ids.Add("minor healing salve");
        ShortDesc = "a pot of minor healing salve";
        Plural = "pots of minor healing salve";
        LongDesc = "A small clay pot of green paste, healing herb and wildflower worked together. Spread on a hurt, it eases it steadily for half a minute. You could {cmd}apply{/} it.";
        Value = 3;
        UseVerb = "apply";
        UseMessage = "You spread the green salve over your hurts.";
        HealPerTick = 3;
        Ticks = 6;
        TickSeconds = 5;
    }
}
