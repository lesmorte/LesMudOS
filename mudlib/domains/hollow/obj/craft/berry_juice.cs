// @inherit /std/crafted
public class CrBerryJuice : Crafted
{
    public override void Create()
    {
        Name = "Berry Juice";
        Ids.Add("juice");
        Ids.Add("berry juice");
        ShortDesc = "a flask of berry juice";
        Plural = "flasks of berry juice";
        LongDesc = "Blackberries pressed and thinned with clean water, cold and dark as wine. You could {cmd}drink{/} it to put a little strength back.";
        Value = 1;
        UseVerb = "drink";
        UseMessage = "You drink the cold, tart juice down.";
        RestoreVitality = 8;
    }
}
