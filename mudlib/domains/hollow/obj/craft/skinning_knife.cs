// @inherit /std/tool
public class CrSkinningKnife : Tool
{
    public override void Create()
    {
        Name = "Skinning Knife";
        Ids.Add("knife");
        Ids.Add("skinning knife");
        ShortDesc = "a skinning knife";
        LongDesc = "A short curved blade with a bone handle, made for taking hides off beasts in one piece.";
        ToolType = "skinning knife";
        Value = 5;
    }
}
