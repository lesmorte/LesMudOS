// @inherit /std/skill

/// <summary>
/// Adventurer skill (docs/specs/skill-ranks.md). Two quick blows with the wielded weapon (or fists), each with its own dodge, hit,
/// critical and block rolls, each for part of the weapon's damage. Ranks: 2 hits at 60% (I), 2 at 75% (II), 3 at 60% as Triple
/// Strike (III), 4 at 55% as Quadruple Strike (IV). The command stays `double` at every rank (`strike` is the Scout's).
/// </summary>
public class DoubleStrikeSkill : Skill
{
    public override void Create()
    {
        Title = "Double Strike";
        Command = "double";
        Summary = "Two quick blows, each at 60% of weapon damage and each with its own hit roll.";
        EnduranceCost = 35;
        Preparation = 0.5;
        Recovery = 1.0;
        WindUp = "shifts their grip for a quick pair of blows";
        IsAttack = true;
        Hits = 2;
        DamageFactor = 0.6;
        Target = "enemy";
        RankRow(2, "factor=0.75");
        RankRow(3, "hits=3;factor=0.6", "Triple Strike");
        RankRow(4, "hits=4;factor=0.55", "Quadruple Strike");
    }

    private static readonly string[] Count = { "", "One", "Two", "Three", "Four", "Five", "Six" };

    public override string QuerySummaryAt(int rank)
    {
        var hits = Math.Max(1, RankInt("hits", rank));
        var pct = (int)Math.Round(RankValue("factor", rank) * 100, MidpointRounding.AwayFromZero);
        return $"{(hits < Count.Length ? Count[hits] : hits.ToString())} quick blows, each at {pct}% of weapon damage and each with its own hit roll.";
    }
}
