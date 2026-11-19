// @inherit /std/crafted
public class HcHuntersPoultice : Crafted
{
    public override void Create()
    {
        Name = "Hunter's Poultice";
        Ids.Add("poultice");
        Ids.Add("hunter's poultice");
        ShortDesc = "a hunter's poultice";
        LongDesc = "Made by hand in Vincent's Hollow.";
        Value = 8;
        UseVerb = "apply";
        HealPerTick = 5; Ticks = 8; TickSeconds = 5;
    }
}
