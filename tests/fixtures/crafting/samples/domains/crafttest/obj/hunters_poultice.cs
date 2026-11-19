// @inherit /std/crafted
public class CtHuntersPoultice : Crafted
{
    public override void Create()
    {
        Name = "Hunter's Poultice";
        Ids.Add("poultice");
        Ids.Add("hunter's poultice");
        ShortDesc = "a hunter's poultice";
        LongDesc = "Made by hand, for the crafting test.";
        Value = 10;
        UseVerb = "apply";
        HealPerTick = 6; Ticks = 6; TickSeconds = 2;
    }
}
