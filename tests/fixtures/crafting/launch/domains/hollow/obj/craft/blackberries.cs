// @inherit /std/material
public class HcBlackberries : Material
{
    public override void Create()
    {
        Name = "Blackberries";
        Ids.Add("blackberries");
        Ids.Add("berries");
        Ids.Add("blackberry");
        ShortDesc = "a handful of blackberries";
        Plural = "handfuls of blackberries";
        LongDesc = "Dark, sweet berries from the hedgerows.";
        Value = 2;
    }
}
