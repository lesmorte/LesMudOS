// @inherit /std/material
public class CrBlackberries : Material
{
    public override void Create()
    {
        Name = "Blackberries";
        Ids.Add("blackberries");
        Ids.Add("berries");
        Ids.Add("blackberry");
        ShortDesc = "a handful of blackberries";
        Plural = "handfuls of blackberries";
        LongDesc = "Fat, glossy hedgerow berries, so dark they stain the fingers purple. Sweet when ripe, sour when you are impatient.";
        Value = 2;
    }
}
