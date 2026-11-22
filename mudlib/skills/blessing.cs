// @inherit /std/skill

/// <summary>
/// Priest spell. To hit and armor for 60 seconds, on the caster or someone in the room. Ranks: +1 / +1 at I, +2 / +2 at II, +3 / +3
/// at III, +4 / +4 at IV (docs/specs/skill-ranks.md); the Focus cost follows the default step; the duration never changes.
/// </summary>
public class BlessingSpell : Skill
{
    public override void Create()
    {
        Title = "Blessing";
        Command = "bless";
        Kind = "spell";
        Summary = "+1 to hit and +1 armor for 60 seconds, on you or someone in the room.";
        FocusCost = 25;
        Preparation = 0.5;
        Recovery = 1.0;
        WindUp = "speaks a blessing";
        Target = "friend";
        Effect("hit", 1);
        Effect("armor", 1);
        Effect("seconds", 60);
        FixedKeys.Add("seconds");
        RankRow(2, "hit=2;armor=2");
        RankRow(3, "hit=3;armor=3");
        RankRow(4, "hit=4;armor=4");
    }

    public override string QuerySummaryAt(int rank) =>
        $"+{RankInt("hit", rank)} to hit and +{RankInt("armor", rank)} armor for {RankInt("seconds", rank)} seconds, on you or someone in the room.";

    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        target ??= user;
        var rank = RankOf(user);
        target.Call("AddEffect", "blessing", "Blessing", RankInt("seconds", rank), $"tohit={RankInt("hit", rank)};armor={RankInt("armor", rank)}", 1.0, 0);
        TellObject(user, target == user ? "A blessing settles over you.\n" : $"You bless {target.Call<string>("QueryLabel")}.\n");
        if (target != user) TellObject(target, $"{user.Call<string>("QueryLabel")} blesses you. You feel protected.\n");
    }
}
