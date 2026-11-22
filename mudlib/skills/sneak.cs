// @inherit /std/skill

/// <summary>
/// Scout skill (docs/specs/sneak.md). A toggle, not an action on the combat clock: `sneak` turns it on or off. While it is on, the Scout pays
/// Endurance for every room entered (5 at rank I, 4 at II, 3 at III) and each aggressive creature there rolls to notice them, with a chance
/// less 0 / 10 / 20 points by rank. The rules and the dice are in /secure/sneak; the state and the command are the character's own
/// (/std/adventurer). This file holds the numbers by rank: "room" is the Endurance a room costs, "notice" the change to the chance.
/// </summary>
public class SneakSkill : Skill
{
    public override void Create()
    {
        Title = "Sneak";
        Command = "sneak";
        Summary = "Moves quietly: each room you enter costs 5 Endurance and the creatures there may fail to notice you. Turn it on before you enter.";
        Recovery = 0.0;
        MaxRank = 3;
        Effect("room", 5);
        Effect("notice", 0);
        FixedKeys.Add("room");
        FixedKeys.Add("notice");
        RankRow(2, "room=4;notice=-10");
        RankRow(3, "room=3;notice=-20");
    }

    /// <summary>Endurance for each room entered while sneaking, at a rank.</summary>
    public int QueryRoomCostAt(int rank) => RankInt("room", rank);

    /// <summary>The change in percentage points to a creature's chance to notice, at a rank (0, -10, -20).</summary>
    public int QueryNoticeAt(int rank) => RankInt("notice", rank);

    public override string QuerySummaryAt(int rank) =>
        $"Moves quietly: each room you enter costs {QueryRoomCostAt(rank)} Endurance, and the creatures there notice you " +
        (QueryNoticeAt(rank) == 0 ? "at the usual chance" : $"{-QueryNoticeAt(rank)} points less often") + ". Turn it on before you enter; loud deeds end it.";
}
