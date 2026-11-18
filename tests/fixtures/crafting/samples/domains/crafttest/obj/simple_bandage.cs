// @inherit /std/crafted
public class CtSimpleBandage : Crafted
{
    public override void Create()
    {
        Name = "Simple Bandage";
        Ids.Add("bandage");
        Ids.Add("simple bandage");
        ShortDesc = "a simple bandage";
        LongDesc = "Made by hand, for the crafting test.";
        Value = 1;
        UseVerb = "apply";
        RestoreVitality = 6;
    }
}
