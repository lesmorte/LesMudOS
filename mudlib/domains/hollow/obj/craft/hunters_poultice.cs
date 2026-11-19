// @inherit /std/crafted
public class CrHuntersPoultice : Crafted
{
    public override void Create()
    {
        Name = "Hunter's Poultice";
        Ids.Add("poultice");
        Ids.Add("hunter's poultice");
        Ids.Add("hunters poultice");
        ShortDesc = "a hunter's poultice";
        Plural = "hunter's poultices";
        LongDesc = "A dark bundle of healing herb and goblin blood bound in cloth, foul to smell and slow to work, but it keeps working for a long while. You could {cmd}apply{/} it to a deep wound.";
        Value = 8;
        UseVerb = "apply";
        UseMessage = "You press the dark poultice against your worst wound and bind it in place.";
        HealPerTick = 5;
        Ticks = 8;
        TickSeconds = 5;
    }
}
