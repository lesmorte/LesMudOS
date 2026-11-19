// @inherit /std/tool
public class CrBleedingKit : Tool
{
    public override void Create()
    {
        Name = "Bleeding Kit";
        Ids.Add("kit");
        Ids.Add("bleeding kit");
        ShortDesc = "a bleeding kit";
        LongDesc = "A roll of waxed cloth holding a thin lancet, a funnel and a row of small corked vials, for drawing blood while it is still fresh.";
        ToolType = "bleeding kit";
        Value = 6;
    }
}
