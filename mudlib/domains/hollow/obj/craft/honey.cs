// @inherit /std/material
public class CrHoney : Material
{
    public override void Create()
    {
        Name = "Honey";
        Ids.Add("honey");
        Ids.Add("jar");
        ShortDesc = "a jar of honey";
        Plural = "jars of honey";
        LongDesc = "Thick golden honey from the village hives, set in a small clay jar. It sweetens a drink and calms a cough with equal ease.";
        Value = 3;
    }
}
