// @inherit /std/material
public class HcCleanWater : Material
{
    public override void Create()
    {
        Name = "Clean Water";
        Ids.Add("water");
        Ids.Add("clean water");
        Ids.Add("flask");
        ShortDesc = "a flask of clean water";
        Plural = "flasks of clean water";
        LongDesc = "Clear water from the well.";
        Value = 1;
    }
}
