// @inherit /std/material
public class CtCleanWater : Material
{
    public override void Create()
    {
        Name = "Clean Water";
        Ids.Add("water");
        Ids.Add("flask");
        Ids.Add("clean water");
        ShortDesc = "a flask of clean water";
        Plural = "flasks of clean water";
        LongDesc = "Clear water from the well.";
        Value = 1;
    }
}
