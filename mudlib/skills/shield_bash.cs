// @inherit /std/skill

/// <summary>
/// Warrior skill. A shield blow for part of the weapon's damage that staggers the target. Ranks: 75% of weapon damage at I, 98% at II,
/// 120% at III, then the default step (docs/specs/skill-ranks.md); the Endurance cost follows the default step; the stagger never changes.
/// </summary>
public class ShieldBashSkill : Skill
{
    public override void Create()
    {
        Title = "Shield Bash";
        Command = "bash";
        Summary = "A shield blow for three quarters of weapon damage. A hit staggers the target: an action it is preparing is interrupted, otherwise its next action is pushed back 2 seconds. Needs a shield.";
        EnduranceCost = 15;
        Preparation = 0;
        Recovery = 1.5;
        IsAttack = true;
        DamageFactor = 0.75;
        NeedsShield = true;
        Target = "enemy";
        Effect("stagger", 2.0);
        FixedKeys.Add("stagger");
        RankRow(2, "factor=0.98");
        RankRow(3, "factor=1.2");
    }

    public override string QuerySummaryAt(int rank) => rank <= 1 ? Summary :
        $"A shield blow for {(int)Math.Round(RankValue("factor", rank) * 100, MidpointRounding.AwayFromZero)}% of weapon damage. A hit staggers the target: an action it is preparing is interrupted, otherwise its next action is pushed back {RankInt("stagger", rank)} seconds. Needs a shield.";

    public override void OnHit(ObjectRef user, ObjectRef target, int damage)
    {
        if (!target.IsAlive || target.Call<bool>("IsDead")) return;
        TellObject(target, "The blow rocks you!\n");
        target.Call("Stagger", RankValue("stagger", RankOf(user)), user);
    }
}
