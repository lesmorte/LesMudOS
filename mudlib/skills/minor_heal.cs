// @inherit /std/skill

/// <summary>
/// Priest spell. Restores Vitality to the caster or to someone in the room (Divine Knowledge and Healing Power add their share on top).
/// The rank table is below. PROVISIONAL: see the note on the numbers.
/// </summary>
public class MinorHealSpell : Skill
{
    public override void Create()
    {
        Title = "Minor Heal";
        Command = "heal";
        Kind = "spell";
        Summary = "Restores 7 Vitality to you or to someone in the room.";
        FocusCost = 25;
        Preparation = 0.5;
        Recovery = 1.0;
        WindUp = "murmurs a prayer";
        Target = "friend";
        // PROVISIONAL, pending the owner's choice (docs/specs/skill-ranks.md, "Minor Heal"; the options are in `python3 tests/balance_sim.py
        // ranks heal`). The spec's table is 20 / 38 / 60 Vitality for 25 / 32 / 40 Focus. Shipped is the simulator's option (b): the largest
        // rank I heal that keeps the Priest within 5 points of the five callings' mean (level 5 Overseer, middle human, resting), with the
        // spec's costs, and the later ranks by the default step from it: 7 / 9 / 11 / 13 Vitality for 25 / 32 / 40 / 44 Focus.
        Effect("heal", 7);
        RankRow(2, "focus=32");
        RankRow(3, "focus=40");
    }

    public override string QuerySummaryAt(int rank) => $"Restores {RankInt("heal", rank)} Vitality to you or to someone in the room.";

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
