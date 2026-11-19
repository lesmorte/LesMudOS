// @inherit /std/crafted
public class HcHerbalBandage : Crafted
{
    public override void Create()
    {
        Name = "Herbal Bandage";
        Ids.Add("bandage");
        Ids.Add("herbal bandage");
        ShortDesc = "an herbal bandage";
        LongDesc = "Made by hand in Vincent's Hollow.";
        Value = 3;
        UseVerb = "apply";
        RestoreVitality = 14;
    }
}
