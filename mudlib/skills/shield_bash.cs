// @inherit /std/skill

/// <summary>Warrior skill. An attack at half weapon damage that staggers the target.</summary>
public class ShieldBashSkill : Skill
{
    public override void Create()
    {
        Title = "Shield Bash";
        Command = "bash";
        Summary = "A shield blow for half weapon damage. A hit staggers the target: an action it is preparing is interrupted, otherwise its next action is pushed back 2 seconds. Needs a shield.";
        StaminaCost = 15;
        Preparation = 0;
        Recovery = 1.5;
        IsAttack = true;
        DamageFactor = 0.5;
        NeedsShield = true;
        Target = "enemy";
    }

    public override void OnHit(ObjectRef user, ObjectRef target, int damage)
    {
        if (!target.IsAlive || target.Call<bool>("IsDead")) return;
        TellObject(target, "The blow rocks you!\n");
        target.Call("Stagger", 2.0, user);
    }
}
