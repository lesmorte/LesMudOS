// @inherit /std/material
public class HcWildflower : Material
{
    public override void Create()
    {
        Name = "Wildflower";
        Ids.Add("flower");
        Ids.Add("wildflower");
        ShortDesc = "a wildflower";
        LongDesc = "A small yellow meadow flower.";
        Value = 2;
    }
}
