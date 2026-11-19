// @inherit /std/material
public class CrEmptyBottle : Material
{
    public override void Create()
    {
        Name = "Empty Bottle";
        Ids.Add("bottle");
        Ids.Add("empty bottle");
        Ids.Add("bottles");
        ShortDesc = "an empty bottle";
        Plural = "empty bottles";
        LongDesc = "A small glass bottle with a cork stopper, washed and dry. It is waiting for something to be poured into it.";
        Value = 1;
    }
}
