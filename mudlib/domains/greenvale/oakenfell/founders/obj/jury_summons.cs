// @inherit /std/summons

/// <summary>The summons of the Mage's hidden class (docs/specs/bureaucrat.md section 2.2). The text, the name and the number are the class's; this file holds only what it looks like.</summary>
public class JurySummons : SummonsPaper
{
    public override void Create()
    {
        base.Create();
        ClassId = "bureaucrat";
        Name = "summons";
        Ids.Add("summons");
        Ids.Add("paper");
        Ids.Add("form");
        ShortDesc = "a summons";
        LongDesc = "A stiff buff-coloured paper, folded in three and stamped in red. It has your name on it. You could {cmd}read{/} it.";
    }
}
