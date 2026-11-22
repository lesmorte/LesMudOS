// @inherit /std/skill

/// <summary>
/// Scout skill. An attack with a bonus to hit and a high critical chance. Ranks: +2 and 15% at I, +3 and 20% at II, +4 and 25% at III,
/// +5 and 30% at IV (docs/specs/skill-ranks.md); the Endurance cost follows the default step.
/// </summary>
public class PrecisionStrikeSkill : Skill
{
    public override void Create()
    {
        Title = "Precision Strike";
        Command = "strike";
        Summary = "An attack at +2 to hit with a 15% chance of a critical hit.";
        EnduranceCost = 25;
        Preparation = 0.5;
        Recovery = 1.0;
        WindUp = "takes careful aim";
        IsAttack = true;
        ToHitBonus = 2;
        CritChance = 15;
        Target = "enemy";
        RankRow(2, "tohit=3;crit=20");
        RankRow(3, "tohit=4;crit=25");
        RankRow(4, "tohit=5;crit=30");
    }

    public override string QuerySummaryAt(int rank) => $"An attack at +{RankInt("tohit", rank)} to hit with a {RankInt("crit", rank)}% chance of a critical hit.";
}
