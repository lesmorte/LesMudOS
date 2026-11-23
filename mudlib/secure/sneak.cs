/// <summary>
/// The rules of sneaking that are numbers (docs/specs/sneak.md): the chance that a creature notices a sneaking Scout, and the dice.
///
/// The chance is a pure function, written once, here: 50% less 5% for every point of the Scout's Grace above 5, plus 5% for every level the
/// creature is above the Scout (less 5% for every level below), kept between 10% and 90%; then +30% if the creature is `Alert`, then less the
/// Scout's rank in Sneak (0, 10, 20), and never below 5% or above 100%. A creature flagged `Boss` does not roll: it always notices.
///
/// The dice are rolled in tenths of a percent (0 to 999): a creature notices if the roll is below ten times its chance (below five times for
/// the re-rolls while the Scout stays, which are at half the chance). A test may fix the dice (SetTestRolls), as the crafting daemon's
/// tests do: only trusted code acting for a wizard of level 10 may; a wizard's own object cannot. Nothing here is saved. Blueprint only.
/// </summary>
public class SneakRules : MudObject
{
    /// <summary>The chance floor and ceiling of the base formula, in percent.</summary>
    public const int BaseMin = 10, BaseMax = 90;
    /// <summary>What an Alert creature adds, in percent.</summary>
    public const int AlertBonus = 30;
    /// <summary>The lowest and highest chance after the Alert bonus and the Scout's rank.</summary>
    public const int FinalMin = 5, FinalMax = 100;
    /// <summary>Seconds between the re-rolls of a creature that has not noticed a Scout who stays.</summary>
    public const int RerollSeconds = 10;

    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    private int fixedRoll = -1;

    /// <summary>
    /// The chance, in percent, that a creature notices a Scout who sneaks. grace and levels as they are; alert: the creature is flagged Alert;
    /// rankAdjust: the Scout's rank in Sneak adds this (0, -10, -20).
    /// </summary>
    public int NoticeChance(int grace, int scoutLevel, int creatureLevel, bool alert, int rankAdjust)
    {
        var chance = 50 - 5 * (grace - 5) + 5 * (creatureLevel - scoutLevel);
        chance = Math.Clamp(chance, BaseMin, BaseMax);
        if (alert) chance += AlertBonus;
        chance += rankAdjust;
        return Math.Clamp(chance, FinalMin, FinalMax);
    }

    /// <summary>One roll in tenths of a percent, 0 to 999 (the fixed value if a test set one).</summary>
    public int Roll() => fixedRoll >= 0 ? fixedRoll : Random(1000);

    /// <summary>Does this roll notice? chance in percent; half: the re-roll, at half the chance.</summary>
    public bool Notices(int chance, bool half)
    {
        var roll = Roll();
        return half ? roll * 2 < chance * 10 : roll < chance * 10;
    }

    /// <summary>
    /// Fix the dice, for tests: "random" (the default), or "notice:NN.N", the roll in percent (0 to 99.9) that every notice roll then takes: it notices
    /// if that is below the chance (below half the chance for a re-roll). Only trusted code acting for a wizard of level 10 may change it.
    /// </summary>
    public bool SetTestRolls(string mode)
    {
        var who = ThisPlayer();
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller.Domain != "" || !TrustedTrees.Any(t => caller.ProgramPath.StartsWith(t, StringComparison.Ordinal)) || who == null || who.Call<int>("QueryWizLevel") < 10) return false;
        if (mode == "random") { fixedRoll = -1; return true; }
        if (mode == null || !mode.StartsWith("notice:")) return false;
        var parts = mode.Substring(7).Split('.');
        if (parts.Length > 2 || parts[0].Length == 0 || !parts[0].All(char.IsDigit) || (parts.Length == 2 && (parts[1].Length != 1 || !char.IsDigit(parts[1][0])))) return false;
        var tenths = int.Parse(parts[0]) * 10 + (parts.Length == 2 ? parts[1][0] - '0' : 0);
        if (tenths < 0 || tenths > 999) return false;
        fixedRoll = tenths;
        return true;
    }

    public string QueryTestRolls() => fixedRoll < 0 ? "random" : $"notice:{fixedRoll / 10}.{fixedRoll % 10}";
}
