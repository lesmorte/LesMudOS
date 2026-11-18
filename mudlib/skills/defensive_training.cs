// @inherit /std/trait

/// <summary>Warrior trait: +1 armor.</summary>
public class DefensiveTrainingTrait : Trait
{
    public override void Create()
    {
        Title = "Defensive Training";
        Summary = "+1 armor.";
        Mods["armor"] = 1;
    }
}
