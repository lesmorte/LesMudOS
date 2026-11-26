/// <summary>
/// The rules of searching that are numbers (docs/specs/search.md): the difficulties, the chance of finding, the check itself, the dice and the
/// length of the wait after a failed search. They are written once, here, and every room, find and test reads them from here.
///
/// The check: a d20 is rolled for each hidden thing. A natural 20 always finds it and a natural 1 never does; otherwise it is found if the roll plus
/// the searcher's bonus reaches the thing's difficulty. So the chance is always between 5% and 95%, one point of bonus is worth 5%, and the table in
/// the spec is generated from Chance. Difficulties: Easy 7, Moderate 12, Hard 17, Very Hard 22; a builder may write any number from 6 to 25.
///
/// All the public rule functions are pure (they read nothing and change nothing, apart from the dice a test may fix), so a wizard's sandbox room
/// may call them (they are on the master's list of sandbox-aware daemon functions). The two setters that change a number (the dice and the
/// wait) are refused to everyone but trusted code acting for a wizard of level 10, and to any clone. Nothing here is saved. Blueprint only.
/// </summary>
public class SearchRules : MudObject
{
    public const int Easy = 7, Moderate = 12, Hard = 17, VeryHard = 22;

    /// <summary>Seconds a character must wait, in a room, after a search there that finds nothing. One number for the whole game.</summary>
    public const int WaitSeconds = 60;

    /// <summary>The lowest and highest difficulty a builder may write as a plain number.</summary>
    public const int LowestDifficulty = 6, HighestDifficulty = 25;

    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    private int fixedRoll = -1;
    private int testWait = -1;

    /// <summary>The d20 roll that finds a thing of this difficulty for a searcher with this bonus: the lowest roll that works, 2 to 20 (20 when only a natural 20 does).</summary>
    private static int Needed(int difficulty, int bonus) => Math.Clamp(difficulty - bonus, 2, 20);

    /// <summary>Does this roll (1 to 20) find a thing of this difficulty? A natural 20 always does, a natural 1 never.</summary>
    public bool Check(int roll, int difficulty, int bonus) => roll >= 20 || (roll > 1 && roll + bonus >= difficulty);

    /// <summary>The chance of finding, per try, in percent: 5 for every roll that works, between 5 and 95.</summary>
    public int Chance(int difficulty, int bonus) => Math.Clamp(5 * (21 - Needed(difficulty, bonus)), 5, 95);

    /// <summary>One d20 roll, 1 to 20 (the fixed value if a test set one).</summary>
    public int Roll() => fixedRoll > 0 ? fixedRoll : Random(20) + 1;

    /// <summary>Roll once against a difficulty with a bonus: true if the thing is found.</summary>
    public bool Succeeds(int difficulty, int bonus) => Check(Roll(), difficulty, bonus);

    /// <summary>The wait after a failed search, in seconds (60, unless a test shortened it).</summary>
    public int QueryWaitSeconds() => testWait >= 0 ? testWait : WaitSeconds;

    /// <summary>Is this number a legal difficulty for a builder to write? 6 to 25.</summary>
    public bool IsLegalDifficulty(int difficulty) => difficulty >= LowestDifficulty && difficulty <= HighestDifficulty;

    private bool MayTest()
    {
        var who = ThisPlayer();
        var caller = PreviousObject();
        return !Self.IsClone && caller != null && caller.Domain == "" && TrustedTrees.Any(t => caller.ProgramPath.StartsWith(t, StringComparison.Ordinal)) && who != null && who.Call<int>("QueryWizLevel") >= 10;
    }

    /// <summary>Fix the dice, for tests: "random" (the default) or "d20:NN", the roll (1 to 20) every search roll then takes. Only trusted code acting for a wizard of level 10 may; a wizard's own object cannot.</summary>
    public bool SetTestRolls(string mode)
    {
        if (!MayTest()) return false;
        if (mode == "random") { fixedRoll = -1; return true; }
        if (mode == null || !mode.StartsWith("d20:") || !int.TryParse(mode.Substring(4), out var roll) || roll < 1 || roll > 20) return false;
        fixedRoll = roll;
        return true;
    }

    /// <summary>Shorten (or lengthen) the wait after a failed search, for tests: a number of seconds from 0 to 600, or -1 for the real one. Only trusted code acting for a wizard of level 10 may.</summary>
    public bool SetTestWait(int seconds)
    {
        if (!MayTest() || seconds < -1 || seconds > 600) return false;
        testWait = seconds;
        return true;
    }

    public string QueryTestRolls() => fixedRoll < 0 ? "random" : "d20:" + fixedRoll;
}
