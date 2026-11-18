// @inherit /std/trait

/// <summary>Warrior trait: +2 armor.</summary>
public class DefensiveTrainingTrait : Trait
{
    public override void Create()
    {
        Title = "Defensive Training";
        Summary = "+2 armor.";
        Mods["armor"] = 2;
    }
}
