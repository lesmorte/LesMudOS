// @inherit /std/crafted
public class CtMinorHealingSalve : Crafted
{
    public override void Create()
    {
        Name = "Minor Healing Salve";
        Ids.Add("salve");
        Ids.Add("healing salve");
        Ids.Add("minor healing salve");
        Ids.Add("pot");
        ShortDesc = "a pot of minor healing salve";
        Plural = "pots of minor healing salve";
        LongDesc = "Made by hand, for the crafting test.";
        Value = 3;
        UseVerb = "apply";
        HealPerTick = 3; Ticks = 6; TickSeconds = 2;
    }
}
