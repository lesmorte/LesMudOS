// @inherit /std/material
public class HcEmptyBottle : Material
{
    public override void Create()
    {
        Name = "Empty Bottle";
        Ids.Add("bottle");
        Ids.Add("empty bottle");
        ShortDesc = "an empty bottle";
        LongDesc = "A small corked glass bottle.";
        Value = 1;
    }
}
