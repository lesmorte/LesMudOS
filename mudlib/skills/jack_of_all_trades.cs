// @inherit /std/trait

/// <summary>
/// Adventurer trait (docs/specs/skill-ranks.md): crafting and gathering proficiency is gained faster in every craft. The crafting
/// daemon's LearnSpeed reads "learn_speed" from the character's class traits; LearnBonusPercent is the one number.
/// </summary>
public class JackOfAllTradesTrait : Trait
{
    /// <summary>How much faster proficiency is gained, in percent.</summary>
    public const double LearnBonusPercent = 25;

    public override void Create()
    {
        Title = "Jack of All Trades";
        Summary = "Crafting and gathering proficiency is gained 25% faster in every craft.";
        Mods["learn_speed"] = LearnBonusPercent;
    }
}
