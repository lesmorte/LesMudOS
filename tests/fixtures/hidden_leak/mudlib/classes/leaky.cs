// @inherit /std/pclass
public class LeakyClass : PlayerClass
{
    public override void Create()
    {
        ClassName = "Keeper of Leaks"; Title = "Keeper of Leaks"; Tier = "advanced"; Parent = "mage";
        Tagline = "Everything drips.";
        Hidden = true;
        QuestId = "leak-hunt"; GateKey = "leaky-door";
        OwnPaths.Add("/domains/leak/own/");
        Skills.Add("leak_skill");
    }
}
