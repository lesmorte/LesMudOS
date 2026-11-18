// @inherit /std/trait

/// <summary>Priest trait: healing spells restore 25% more.</summary>
public class DivineKnowledgeTrait : Trait
{
    public override void Create()
    {
        Title = "Divine Knowledge";
        Summary = "Healing spells restore 25% more.";
        Mods["healing_pct"] = 25;
    }
}
