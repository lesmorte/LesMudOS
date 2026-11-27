// @inherit /std/skill

/// <summary>
/// Scout skill (docs/specs/hollow-expansion.md section 1). A bow shot taken with care: 35 Endurance (the owner raised it from 30 on 26 Nov 2026), a wind-up of 2 seconds and 2 seconds of
/// recovery, +3 to hit and 1.75 times the weapon's damage; as the action that starts the fight, 2.5 times. Being hit while the shot is
/// being aimed spoils it, and the Endurance stays spent. It needs a bow in the hands, a target, and a target that is not already fighting
/// someone else (it is the shot of the one who chooses the fight).
///
/// Ranks (the table's x2.0 / x3.0 and x2.25 / x3.5; IV and V continue by +0.25 and +0.5 a rank, and the engine goes no further because
/// no price exists for a rank VI): factor 1.75 / 2.0 / 2.25 / 2.5 / 2.75, as opener 2.5 / 3.0 / 3.5 / 4.0 / 4.5. The Endurance cost
/// follows the default step (35, 40, 46, 53, 61). The to-hit bonus is +3 at every rank.
/// </summary>
public class AimedShotSkill : Skill
{
    public override void Create()
    {
        Title = "Aimed Shot";
        Command = "aim";
        Summary = "A shot at +3 to hit for 1.75 times the bow's damage, 2.5 times as the shot that starts the fight. The opener never takes more than part of the target's full Vitality unless you are three levels above it. A blow that lands while you aim spoils it. Needs a bow; not at one that is fighting someone else.";
        EnduranceCost = 35;
        Preparation = 1.0;
        Recovery = 1.0;
        WindUp = "draws a long breath and takes aim";
        IsAttack = true;
        ToHitBonus = 3;
        DamageFactor = 1.75;
        Target = "enemy";
        Spoilable = true;
        WeaponCategories.Add("bow");
        Effect("opener", 2.5);
        FixedKeys.Add("tohit");
        RankRow(2, "factor=2.0;opener=3.0");
        RankRow(3, "factor=2.25;opener=3.5");
        RankRow(4, "factor=2.5;opener=4.0");
        RankRow(5, "factor=2.75;opener=4.5");
        MaxRank = 5;
    }

    // The opener's cap (owner's decision, 24 Nov 2026; docs/BALANCE_SCOUT.md section 10, option G2): the shot that starts the fight deals at most
    // OpenerCapBase + OpenerCapPerLevel x d percent of the target's maximum Vitality (d = the Scout's level - the creature's level when it lands),
    // never less than OpenerCapFloor percent, and no cap once that reaches 100.
    const int OpenerCapBase = 60;
    const int OpenerCapPerLevel = 15;
    const int OpenerCapFloor = 30;

    public override int QueryOpenerCapPercent(int levelDiff)
    {
        var share = Math.Max(OpenerCapFloor, OpenerCapBase + OpenerCapPerLevel * levelDiff);
        return share >= 100 ? 0 : share;
    }

    public override string QuerySummaryAt(int rank) =>
        $"A shot at +{RankInt("tohit", rank)} to hit for {Fmt(RankValue("factor", rank))} times the bow's damage, {Fmt(RankValue("opener", rank))} times as the shot that starts the fight. The opener never takes more than part of the target's full Vitality unless you are three levels above it. A blow that lands while you aim spoils it. Needs a bow; not at one that is fighting someone else.";

    private static string Fmt(double value)
    {
        var text = ((long)Math.Round(value * 100, MidpointRounding.AwayFromZero)).ToString();
        text = text.Length < 3 ? text.PadLeft(3, '0') : text;
        var whole = text.Substring(0, text.Length - 2);
        var frac = text.Substring(text.Length - 2).TrimEnd('0');
        return frac == "" ? whole : whole + "." + frac;
    }

    protected override string? Check(ObjectRef user, ObjectRef? target)
    {
        if (target == null) return "Aim at whom?";
        if (!target.Call<bool>("QueryFightingWith", user) && target.Call<bool>("QueryInCombat"))
            return $"{char.ToUpper((target.Call<string>("QueryLabel") ?? "it")[0]) + (target.Call<string>("QueryLabel") ?? "it").Substring(1)} is busy fighting someone else; you cannot take aim at it.";
        return null;
    }
}
