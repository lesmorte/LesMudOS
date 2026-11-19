// @inherit /std/crafted
public class CrMinorStaminaTonic : Crafted
{
    public override void Create()
    {
        Name = "Minor Stamina Tonic";
        Ids.Add("tonic");
        Ids.Add("stamina tonic");
        Ids.Add("minor stamina tonic");
        ShortDesc = "a minor stamina tonic";
        Plural = "minor stamina tonics";
        LongDesc = "A stoppered flask of blueberry and honey, thick and sweet, that puts the spring back in tired legs. You could {cmd}drink{/} it.";
        Value = 3;
        UseVerb = "drink";
        UseMessage = "You swallow the sweet, thick tonic.";
        RestoreStamina = 25;
    }
}
