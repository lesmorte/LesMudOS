// @inherit /std/crafted
public class HcBerryJuice : Crafted
{
    public override void Create()
    {
        Name = "Berry Juice";
        Ids.Add("juice");
        Ids.Add("berry juice");
        ShortDesc = "a flask of berry juice";
        Plural = "flasks of berry juice";
        LongDesc = "Made by hand in Vincent's Hollow.";
        Value = 1;
        UseVerb = "drink";
        RestoreVitality = 8;
    }
}
