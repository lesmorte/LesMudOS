// @inherit /std/consumable

public class SgHandPie : Consumable
{
    public override void Create()
    {
        Name = "Hand Pie";
        Ids.Add("pie");
        Ids.Add("hand pie");
        Ids.Add("mutton pie");
        ShortDesc = "a mutton hand pie";
        LongDesc = "A golden crust, still warm, filled with mutton, onion and a little pepper. It is just small enough to eat on the walk. You could {cmd}eat{/} it.";
        HealAmount = 8;
        UseVerb = "eat";
        UseMessage = "You bite into the hot pie. The gravy runs down your chin.";
        Value = 3;
    }
}
