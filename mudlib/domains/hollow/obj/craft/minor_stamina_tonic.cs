// @inherit /std/crafted
// The file keeps its old name (stamina): its path is saved in characters and recipes. Everything a player sees says Endurance.
public class CrMinorStaminaTonic : Crafted
{
    public override void Create()
    {
        Name = "Minor Endurance Tonic";
        Ids.Add("tonic");
        Ids.Add("endurance tonic");
        Ids.Add("minor endurance tonic");
        ShortDesc = "a minor endurance tonic";
        Plural = "minor endurance tonics";
        LongDesc = "A stoppered flask of blueberry and honey, thick and sweet, that puts the spring back in tired legs. You could {cmd}drink{/} it.";
        Value = 3;
        UseVerb = "drink";
        UseMessage = "You swallow the sweet, thick tonic.";
        RestoreEndurance = 25;
    }
}
