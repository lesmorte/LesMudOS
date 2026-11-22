// @inherit /std/skill

/// <summary>Scout skill. An attack at +2 to hit with a 15% critical chance.</summary>
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
    }
}
