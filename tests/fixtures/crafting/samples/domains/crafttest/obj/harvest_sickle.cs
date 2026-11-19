// @inherit /std/tool
public class CtHarvestSickle : Tool
{
    public override void Create()
    {
        Name = "Harvest Sickle";
        Ids.Add("harvest sickle");
        Ids.Add("sickle");
        ShortDesc = "a harvest sickle";
        LongDesc = "A harvest sickle for the crafting test.";
        ToolType = "harvest sickle";
        Value = 5;
    }
}
