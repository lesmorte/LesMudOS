// @inherit /std/trait

/// <summary>Adventurer trait: unarmed damage 2-5, and weapons carry 0.5 less reaction modifier (never below 0).</summary>
public class ImprovisedCombatTrait : Trait
{
    public override void Create()
    {
        Title = "Improvised Combat";
        Summary = "Unarmed damage 2-5, and weapons carry 0.5 less reaction modifier (never below 0).";
        Mods["unarmed_min"] = 1;
        Mods["unarmed_max"] = 2;
        Mods["weapon_reaction_reduction"] = 0.5;
    }
}
