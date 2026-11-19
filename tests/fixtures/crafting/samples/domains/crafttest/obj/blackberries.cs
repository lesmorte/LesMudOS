// @inherit /std/material
public class CtBlackberries : Material
{
    public override void Create()
    {
        Name = "Blackberries";
        Ids.Add("blackberries");
        Ids.Add("berries");
        Ids.Add("blackberry");
        ShortDesc = "a handful of blackberries";
        Plural = "handfuls of blackberries";
        LongDesc = "Dark, sweet and a little crushed.";
        Value = 2;
    }
}
