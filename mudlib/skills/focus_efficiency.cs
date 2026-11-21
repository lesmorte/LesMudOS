// @inherit /std/trait

/// <summary>Mage trait: spells cost 20% less Mana.</summary>
public class ManaEfficiencyTrait : Trait
{
    public override void Create()
    {
        Title = "Mana Efficiency";
        Summary = "Spells cost 20% less Mana.";
        Mods["spell_cost_pct"] = -20;
    }
}
