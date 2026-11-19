// @inherit /std/material
public class HcFlour : Material
{
    public override void Create()
    {
        Name = "Flour";
        Ids.Add("flour");
        Ids.Add("sack");
        ShortDesc = "a sack of flour";
        Plural = "sacks of flour";
        LongDesc = "A small sack of milled flour.";
        Value = 1;
    }
}
