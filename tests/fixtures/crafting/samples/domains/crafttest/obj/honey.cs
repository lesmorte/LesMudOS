// @inherit /std/material
public class CtHoney : Material
{
    public override void Create()
    {
        Name = "Honey";
        Ids.Add("honey");
        Ids.Add("jar");
        ShortDesc = "a jar of honey";
        Plural = "jars of honey";
        LongDesc = "Thick golden honey.";
        Value = 3;
    }
}
