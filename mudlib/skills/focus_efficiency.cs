// @inherit /std/trait

/// <summary>Mage trait: spells cost 20% less Focus.</summary>
public class FocusEfficiencyTrait : Trait
{
    public override void Create()
    {
        Title = "Focus Efficiency";
        Summary = "Spells cost 20% less Focus.";
        Mods["spell_cost_pct"] = -20;
    }
}
