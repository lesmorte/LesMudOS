// @inherit /std/crafted
public class CrSimpleBandage : Crafted
{
    public override void Create()
    {
        Name = "Simple Bandage";
        Ids.Add("bandage");
        Ids.Add("simple bandage");
        ShortDesc = "a simple bandage";
        Plural = "simple bandages";
        LongDesc = "A length of clean linen rolled tight. It will stop the worst of a bleed and not much more. You could {cmd}apply{/} it.";
        Value = 1;
        UseVerb = "apply";
        UseMessage = "You wind the bandage tight over your wounds.";
        RestoreVitality = 6;
    }
}
