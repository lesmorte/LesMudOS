// @inherit /std/tool
public class CtBleedingKit : Tool
{
    public override void Create()
    {
        Name = "Bleeding Kit";
        Ids.Add("bleeding kit");
        Ids.Add("kit");
        ShortDesc = "a bleeding kit";
        LongDesc = "A bleeding kit for the crafting test.";
        ToolType = "bleeding kit";
        Value = 5;
    }
}
