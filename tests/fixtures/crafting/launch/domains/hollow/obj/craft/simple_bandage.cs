// @inherit /std/crafted
public class HcSimpleBandage : Crafted
{
    public override void Create()
    {
        Name = "Simple Bandage";
        Ids.Add("bandage");
        Ids.Add("simple bandage");
        ShortDesc = "a simple bandage";
        LongDesc = "Made by hand in Vincent's Hollow.";
        Value = 1;
        UseVerb = "apply";
        RestoreVitality = 6;
    }
}
