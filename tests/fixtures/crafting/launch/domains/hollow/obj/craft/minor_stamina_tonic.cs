// @inherit /std/crafted
public class HcMinorStaminaTonic : Crafted
{
    public override void Create()
    {
        Name = "Minor Stamina Tonic";
        Ids.Add("tonic");
        Ids.Add("stamina tonic");
        ShortDesc = "a minor stamina tonic";
        LongDesc = "Made by hand in Vincent's Hollow.";
        Value = 3;
        UseVerb = "drink";
        RestoreStamina = 25;
    }
}
