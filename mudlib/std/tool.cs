// @inherit /std/item

/// <summary>
/// A gathering or crafting tool: a skinning knife, a bleeding kit, a harvest sickle. Carried, not wielded: the gathering
/// commands use the best tool of the right type that the character carries, by its item quality (Poor to Masterwork). Set
/// ToolType in Create() to the type the gathering data names ("skinning knife", "bleeding kit", "harvest sickle"). Tools do
/// not wear out in this pass. Only tools whose file lies in a trusted tree count (a tool from a wizard's /wiz home does not).
/// </summary>
public class Tool : Item
{
    /// <summary>What kind of tool this is, as the gathering data names it ("skinning knife").</summary>
    protected string ToolType = "";

    public bool IsTool() => true;

    public string QueryToolType() => ToolType;

    public override string Long() => base.Long() + $"It is a tool ({ToolType}) of {QueryQualityName()} quality. Carry it: it is used when you need it.\n";

    public override string Appraisal() => base.Appraisal().TrimEnd('\n') + $" Tool: {ToolType}.\n";

    public override string Stat() => base.Stat() + $"  Tool: {ToolType}\n";
}
