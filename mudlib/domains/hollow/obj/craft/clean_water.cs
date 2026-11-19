// @inherit /std/material
public class CrCleanWater : Material
{
    public override void Create()
    {
        Name = "Clean Water";
        Ids.Add("water");
        Ids.Add("clean water");
        Ids.Add("flask");
        ShortDesc = "a flask of clean water";
        Plural = "flasks of clean water";
        LongDesc = "Cold, clear water drawn from the village well and stoppered in a flask. Plain stuff, but half the recipes in the Hollow cannot do without it.";
        Value = 1;
    }
}
