// @inherit /std/skill

/// <summary>
/// Priest spell. Restores Vitality to the caster or to someone in the room (Divine Knowledge and Healing Power add their share on top).
/// The large heal: 20 / 38 / 60 Vitality at ranks I / II / III, and the caster cannot cast it again for HealWait seconds. On the caster
/// it gives a third (7 / 13 / 20, rounded half up; the owner's decision of 22 November 2026, after half proved too strong and a quarter too weak at rank I): Divine Knowledge applies afterwards.
/// </summary>
public class MinorHealSpell : Skill
{
    public override void Create()
    {
        Title = "Minor Heal";
        Command = "heal";
        Kind = "spell";
        Summary = "Restores 20 Vitality to someone in the room, or 10 to you: the Quiet Hand gives freely to the one who reaches for another and sparingly to the one who reaches for himself. You cannot cast it again for 15 seconds.";
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

    /// <summary>What the spell gives the one who casts it on themselves: a third of the amount it gives another, rounded half up (20 gives 7, 38 gives 13).</summary>
    public static int SelfShare(int amount) => (2 * amount + 3) / 6;

    /// <summary>What a rank gives the caster (before Divine Knowledge): 7, 13, 20, 22, 24.</summary>
    public int QuerySelfHealAt(int rank) => SelfShare(RankInt("heal", rank));

    public override string QuerySummaryAt(int rank) =>
        $"Restores {RankInt("heal", rank)} Vitality to someone in the room, or {SelfShare(RankInt("heal", rank))} to you: the Quiet Hand gives freely to the one who reaches for another and sparingly to the one who reaches for himself. You cannot cast it again for {RankInt("wait", rank)} seconds.";

    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        target ??= user;
        var given = RankInt("heal", RankOf(user));
        if (target == user) given = SelfShare(given); // a healer's own hurts are the slowest to close; Divine Knowledge comes after the halving
        var amount = user.Call<int>("ScaleHealing", given);
        var healed = target.Call<int>("Heal", amount);
        var room = Environment(user);
        var who = target == user ? "yourself" : target.Call<string>("QueryLabel") ?? "it";
        TellObject(user, (target == user
            ? $"The light comes slowly to your own hurts, restoring {healed} Vitality."
            : $"Warm light flows from you to {who}, restoring {healed} Vitality.") + (user.Call<string>("HealNote", healed) ?? "") + "\n");
        if (target != user) TellObject(target, $"{user.Call<string>("QueryLabel")} heals you for {healed} Vitality." + (target.Call<string>("HealNote", healed) ?? "") + "\n");
        if (room != null) TellRoom(room, $"{user.Call<string>("QueryLabel")} heals {(target == user ? "themselves" : target.Call<string>("QueryLabel"))}.\n", user, target);
    }
}
