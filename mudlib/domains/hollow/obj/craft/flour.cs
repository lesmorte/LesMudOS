// @inherit /std/material
public class CrFlour : Material
{
    public override void Create()
    {
        Name = "Flour";
        Ids.Add("flour");
        Ids.Add("sack");
        ShortDesc = "a sack of flour";
        Plural = "sacks of flour";
        LongDesc = "A small sack of coarse flour, milled in the Hollow and dusting everything it touches.";
        Value = 1;
    }
}
