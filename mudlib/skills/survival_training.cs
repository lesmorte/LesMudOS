// @inherit /std/trait

/// <summary>Adventurer trait: Vitality, Endurance and Focus regenerate 50% faster out of combat.</summary>
public class SurvivalTrainingTrait : Trait
{
    public override void Create()
    {
        Title = "Survival Training";
        Summary = "Vitality, Endurance and Focus regenerate 50% faster out of combat.";
        Mods["regen_out"] = 0.5;
    }
}
