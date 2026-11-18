// @inherit /std/crafted
public class CtBerryJuice : Crafted
{
    public override void Create()
    {
        Name = "Berry Juice";
        Ids.Add("juice");
        Ids.Add("berry juice");
        Ids.Add("flask");
        ShortDesc = "a flask of berry juice";
        Plural = "flasks of berry juice";
        LongDesc = "Made by hand, for the crafting test.";
        Value = 1;
        UseVerb = "drink";
        RestoreVitality = 8;
    }
}
