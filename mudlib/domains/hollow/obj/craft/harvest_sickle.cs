// @inherit /std/tool
public class CrHarvestSickle : Tool
{
    public override void Create()
    {
        Name = "Harvest Sickle";
        Ids.Add("sickle");
        Ids.Add("harvest sickle");
        ShortDesc = "a harvest sickle";
        LongDesc = "A small hooked sickle with a worn ash handle, good for cutting berries, herbs and flowers without tearing the plant.";
        ToolType = "harvest sickle";
        Value = 4;
    }
}
