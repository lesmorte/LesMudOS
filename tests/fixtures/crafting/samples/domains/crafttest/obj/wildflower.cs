// @inherit /std/material
public class CtWildflower : Material
{
    public override void Create()
    {
        Name = "Wildflower";
        Ids.Add("flower");
        Ids.Add("wildflower");
        ShortDesc = "a wildflower";
        LongDesc = "A small yellow flower from the meadow.";
        Value = 2;
    }
}
