// @inherit /std/material
public class CrBlueberries : Material
{
    public override void Create()
    {
        Name = "Blueberries";
        Ids.Add("blueberries");
        Ids.Add("berries");
        Ids.Add("blueberry");
        ShortDesc = "a handful of blueberries";
        Plural = "handfuls of blueberries";
        LongDesc = "Small, dusty-blue berries from the heath, cool and faintly tart. Healers say a few of them steady tired legs.";
        Value = 2;
    }
}
