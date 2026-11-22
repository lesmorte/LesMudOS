// @inherit /std/crafted
public class CrSweetBerryJuice : Crafted
{
    public override void Create()
    {
        Name = "Sweet Berry Juice";
        Ids.Add("juice");
        Ids.Add("sweet juice");
        Ids.Add("sweet berry juice");
        ShortDesc = "a flask of sweet berry juice";
        Plural = "flasks of sweet berry juice";
        LongDesc = "Berry juice stirred through with honey until it is glossy and thick. It revives the body and the wind together. You could {cmd}drink{/} it.";
        Value = 2;
        UseVerb = "drink";
        UseMessage = "You drink the sweet juice and feel the strength come back into your legs.";
        RestoreVitality = 10;
        RestoreEndurance = 10;
    }
}
