// @inherit /std/material
public class CtClothStrip : Material
{
    public override void Create()
    {
        Name = "Cloth Strip";
        Ids.Add("cloth");
        Ids.Add("strip");
        Ids.Add("cloth strip");
        ShortDesc = "a cloth strip";
        LongDesc = "A clean strip of linen.";
        Value = 2;
    }
}
