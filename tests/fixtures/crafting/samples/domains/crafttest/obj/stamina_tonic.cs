// @inherit /std/crafted
public class CtStaminaTonic : Crafted
{
    public override void Create()
    {
        Name = "Minor Endurance Tonic";
        Ids.Add("tonic");
        Ids.Add("endurance tonic");
        ShortDesc = "a minor endurance tonic";
        LongDesc = "Made by hand, for the crafting test.";
        Value = 3;
        UseVerb = "drink";
        RestoreEndurance = 20;
    }
}
