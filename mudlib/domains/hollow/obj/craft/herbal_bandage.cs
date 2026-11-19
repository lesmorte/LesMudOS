// @inherit /std/crafted
public class CrHerbalBandage : Crafted
{
    public override void Create()
    {
        Name = "Herbal Bandage";
        Ids.Add("bandage");
        Ids.Add("herbal bandage");
        ShortDesc = "an herbal bandage";
        Plural = "herbal bandages";
        LongDesc = "Linen soaked in crushed healing herb and rolled tight, sharp with the smell of camphor. It closes a wound and soothes it. You could {cmd}apply{/} it.";
        Value = 3;
        UseVerb = "apply";
        UseMessage = "You bind the herb-soaked linen over your wounds. The sting gives way to a cool ease.";
        RestoreVitality = 12;
    }
}
