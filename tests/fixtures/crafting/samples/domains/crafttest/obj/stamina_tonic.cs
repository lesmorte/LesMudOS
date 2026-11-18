// @inherit /std/crafted
public class CtStaminaTonic : Crafted
{
    public override void Create()
    {
        Name = "Minor Stamina Tonic";
        Ids.Add("tonic");
        Ids.Add("stamina tonic");
        ShortDesc = "a minor stamina tonic";
        LongDesc = "Made by hand, for the crafting test.";
        Value = 3;
        UseVerb = "drink";
        RestoreStamina = 20;
    }
}
