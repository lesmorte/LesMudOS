// @inherit /std/consumable

public class SrMuttonPasty : Consumable
{
    public override void Create()
    {
        Name = "Mutton Pasty";
        Ids.Add("pasty");
        Ids.Add("mutton pasty");
        Ids.Add("pie");
        ShortDesc = "a mutton pasty";
        LongDesc = "A thick-crusted pasty crimped along one edge, stuffed with mutton, onion and turnip, and still warm. You could {cmd}eat{/} it.";
        HealAmount = 16;
        UseVerb = "eat";
        UseMessage = "You eat the pasty down to the last flaky crumb. It is hot, peppery, and entirely satisfying.";
        Value = 6;
    }
}
