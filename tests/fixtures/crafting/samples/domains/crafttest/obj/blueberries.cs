// @inherit /std/material
public class CtBlueberries : Material
{
    public override void Create()
    {
        Name = "Blueberries";
        Ids.Add("blueberries");
        Ids.Add("berries");
        Ids.Add("blueberry");
        ShortDesc = "a handful of blueberries";
        Plural = "handfuls of blueberries";
        LongDesc = "Small, round and dusty blue.";
        Value = 2;
    }
}
