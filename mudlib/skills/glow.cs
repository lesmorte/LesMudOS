// @inherit /std/skill

/// <summary>
/// Mage and Priest spell (docs/specs/light.md, section 7). A small light about you or a friend in the room: +1 light (rank III +2) for 5 / 8 / 12 minutes
/// at 20 / 23 / 26 Focus. It lights the room for everyone in it; a creature's Glow counts at most +2, and the register (/secure/light) hears of
/// it only from this file. Recasting refreshes it; it does not add. The effect is not saved. Rank III is the last.
/// </summary>
public class GlowSpell : Skill
{
    public override void Create()
    {
        Title = "Glow";
        Command = "glow";
        Kind = "spell";
        Summary = "A small light about you or a friend: +1 light for 5 minutes.";
        FocusCost = 20;
        Preparation = 0.5;
        Recovery = 1.0;
        WindUp = "murmurs a word of light";
        Target = "friend";
        MaxRank = 3; // rank III is the last
        Effect("light", 1);
        Effect("seconds", 300);
        FixedKeys.Add("seconds");
        FixedKeys.Add("light");
        RankRow(2, "seconds=480;focus=23");
        RankRow(3, "light=2;seconds=720;focus=26");
    }

    public override string QuerySummaryAt(int rank) =>
        $"A small light about you or a friend: +{RankInt("light", rank)} light for {RankInt("seconds", rank) / 60} minutes.";

    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        target ??= user;
        if (!target.IsAlive || Environment(target) != Environment(user)) return;
        var rank = RankOf(user);
        var seconds = RankInt("seconds", rank);
        var level = Math.Clamp(RankInt("light", rank), 1, 2);
        var daemon = LoadObject("/secure/light");
        target.Call("AddEffect", "light", "Glow", seconds, $"light={level}", 1.0, 0);
        daemon.Call("NoteGlow", target, seconds);
        var room = Environment(user);
        var who = user.Call<string>("QueryName") ?? "Someone";
        var named = target.Call<string>("QueryName") ?? "someone";
        if (target == user)
        {
            TellObject(user, "A small, steady light gathers about you.\n");
            if (room != null) TellRoom(room, $"A small light gathers about {who}.\n", user);
        }
        else
        {
            TellObject(user, $"You call a small light about {named}.\n");
            TellObject(target, "A small light gathers about you.\n");
            if (room != null) TellRoom(room, $"A small light gathers about {named}.\n", user, target);
        }
    }
}
