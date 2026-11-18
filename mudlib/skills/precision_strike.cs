// @inherit /std/skill

/// <summary>Scout skill. An attack at +5 to hit with a 30% critical chance.</summary>
public class PrecisionStrikeSkill : Skill
{
    public override void Create()
    {
        Title = "Precision Strike";
        Command = "strike";
        Summary = "An attack at +5 to hit with a 30% chance of a critical hit.";
        StaminaCost = 20;
        Preparation = 0.5;
        Recovery = 1.0;
        WindUp = "takes careful aim";
        IsAttack = true;
        ToHitBonus = 5;
        CritChance = 30;
        Target = "enemy";
    }
}
