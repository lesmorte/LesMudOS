// @inherit /std/tool
public class CtSkinningKnife : Tool
{
    public override void Create()
    {
        Name = "Skinning Knife";
        Ids.Add("skinning knife");
        Ids.Add("knife");
        ShortDesc = "a skinning knife";
        LongDesc = "A skinning knife for the crafting test.";
        ToolType = "skinning knife";
        Value = 5;
    }
}
