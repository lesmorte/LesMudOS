// @inherit /std/crafted
public class HcSweetBerryJuice : Crafted
{
    public override void Create()
    {
        Name = "Sweet Berry Juice";
        Ids.Add("juice");
        Ids.Add("sweet juice");
        Ids.Add("sweet berry juice");
        ShortDesc = "a flask of sweet berry juice";
        Plural = "flasks of sweet berry juice";
        LongDesc = "Made by hand in Vincent's Hollow.";
        Value = 2;
        UseVerb = "drink";
        RestoreVitality = 10; RestoreStamina = 10;
    }
}
