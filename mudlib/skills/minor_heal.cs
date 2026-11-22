// @inherit /std/skill

/// <summary>
/// Priest spell. Restores Vitality to the caster or to someone in the room (Divine Knowledge and Healing Power add their share on top).
/// The large heal: 20 / 38 / 60 Vitality at ranks I / II / III, and the caster cannot cast it again for HealWait seconds.
/// </summary>
public class MinorHealSpell : Skill
{
    public override void Create()
    {
        Title = "Minor Heal";
        Command = "heal";
        Kind = "spell";
        Summary = "Restores 20 Vitality to you or to someone in the room. You cannot cast it again for 15 seconds.";
        FocusCost = 25;
        Preparation = 0.5;
        Recovery = 1.0;
        WindUp = "murmurs a prayer";
        Target = "friend";
        // The owner's table (docs/specs/skill-ranks.md, "Owner's decisions after the build"): 20 / 38 / 60 Vitality for 25 / 32 / 40 Focus,
        // later ranks by the default step from rank III's numbers, and no second Minor Heal by the same caster for HealWait seconds.
        Effect("heal", 20);
        Effect("wait", HealWait);
        RankRow(2, "heal=38;focus=32");
        RankRow(3, "heal=60;focus=40");
    }

    /// <summary>Seconds after a Minor Heal begins during which the same caster cannot cast another (per caster, not per target). The one place it is named.</summary>
    private const int HealWait = 15;

    public override string QuerySummaryAt(int rank) => $"Restores {RankInt("heal", rank)} Vitality to you or to someone in the room. You cannot cast it again for {RankInt("wait", rank)} seconds.";

    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        target ??= user;
        var amount = user.Call<int>("ScaleHealing", RankInt("heal", RankOf(user)));
        var healed = target.Call<int>("Heal", amount);
        var room = Environment(user);
        var who = target == user ? "yourself" : target.Call<string>("QueryLabel") ?? "it";
        TellObject(user, $"Warm light flows from you to {who}, restoring {healed} Vitality." + (user.Call<string>("HealNote", healed) ?? "") + "\n");
        if (target != user) TellObject(target, $"{user.Call<string>("QueryLabel")} heals you for {healed} Vitality." + (target.Call<string>("HealNote", healed) ?? "") + "\n");
        if (room != null) TellRoom(room, $"{user.Call<string>("QueryLabel")} heals {(target == user ? "themselves" : target.Call<string>("QueryLabel"))}.\n", user, target);
    }
}
