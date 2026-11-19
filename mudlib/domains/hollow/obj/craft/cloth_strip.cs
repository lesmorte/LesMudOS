// @inherit /std/material
public class CrClothStrip : Material
{
    public override void Create()
    {
        Name = "Cloth Strip";
        Ids.Add("cloth");
        Ids.Add("strip");
        Ids.Add("cloth strip");
        Ids.Add("strips");
        ShortDesc = "a cloth strip";
        Plural = "cloth strips";
        LongDesc = "A clean strip of linen torn to a hand's width. Good for binding a wound or tying up a bundle, and everyone is always short of it.";
        Value = 2;
    }
}
