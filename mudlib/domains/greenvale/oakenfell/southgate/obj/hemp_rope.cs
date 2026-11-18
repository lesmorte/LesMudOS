// @inherit /std/item

public class SgHempRope : Item
{
    public override void Create()
    {
        Name = "Hempen Rope";
        Ids.Add("rope");
        Ids.Add("hemp rope");
        Ids.Add("hempen rope");
        Ids.Add("coil");
        ShortDesc = "a coil of hempen rope";
        LongDesc = "Fifty feet of sturdy hempen rope, thick as a thumb and neatly coiled.";
        Value = 6;
    }
}
