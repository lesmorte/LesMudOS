/// <summary>
/// The rule for control effects (docs/specs/tags-and-saves.md, Part B, sections 12 and 13): one ladder of three strengths for every hostile effect
/// that takes away what a creature may do or how fast it may do it (a slow, a bind, an interrupt, a forced target, a bar on melee, a stun).
///
///   * Strength: Full (2), Half (1) or None (0). Every target rolls one save. A failed save leaves the effect at Full and a made save takes one
///     step off. A boss starts one step down ("above the law"). A repeat of the same effect on the same target within 60 seconds takes one
///     more step off, and a second repeat another (two at most). "Half" means half the time (QueryHalfSeconds).
///   * The save chance, in percent: the target's resistance of the effect's kind (physical or mental), plus 10 for every level the target is
///     above the caster ("ignores your authority"), plus 15 for an elite, less the caster's Authority (the derived statistic, plus 4 for every
///     rank of the skill above I), kept between 5 and 90.
///   * The flood guard: a target that has had four control effects of any kind take hold in the last 60 seconds ignores the fifth.
///
/// The pure rule functions begin with "Query" and take plain numbers, so a wizard's workshop may build and test an effect with them. Resolve is
/// the stateful one: it keeps the repeat records and the flood guard, and it refuses (and records nothing) when the caller is not real code
/// acting for a real player or creature, when any restricted code is anywhere on the call stack, or when the caster or the target is not a real
/// living creature: a sandbox effect can never reach a real target, and the records can never be changed by sandbox code. Nothing here is
/// saved; the records are forgotten at a restart. Blueprint only. Nothing in the game calls Resolve yet.
/// </summary>
public class ControlRules : MudObject
{
    /// <summary>The save chance, in percent: never below or above these.</summary>
    public const int SaveMin = 5, SaveMax = 90;
    /// <summary>Points of save chance for each level the target is above the caster, and for an elite.</summary>
    public const int LevelPoints = 10, ElitePoints = 15;
    /// <summary>Points of Authority for each rank of the skill above I.</summary>
    public const int RankAuthority = 4;
    /// <summary>The repeat window and the flood guard: seconds, and how many effects of any kind may take hold in the window.</summary>
    public const int WindowSeconds = 60, FloodLimit = 4;
    /// <summary>Steps a repeat may take off, at most.</summary>
    public const int MaxRepeatSteps = 2;

    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    private sealed class Record
    {
        public readonly Dictionary<string, (long Last, int Count)> Effects = new();
        public readonly List<long> Applied = new();
    }

    private readonly Dictionary<ObjectRef, Record> records = new();
    private readonly Dictionary<(ObjectRef caster, ObjectRef target), (int Points, long Until)> penalties = new();
    private int fixedRoll = -1;
    private long clockShiftMs;

    // ---- The pure rule: plain numbers in, a number out ----

    /// <summary>The target's save chance in percent (5 to 90): resistance + 10 per level above the caster + 15 for an elite - the caster's Authority.</summary>
    public int QuerySaveChance(int resistance, int levelAbove, bool elite, int authority) =>
        Math.Clamp(resistance + LevelPoints * Math.Max(0, levelAbove) + (elite ? ElitePoints : 0) - authority, SaveMin, SaveMax);

    /// <summary>The strength, 0 (None) to 2 (Full): 2, less 1 for a boss, less the repeat steps (0 to 2), less the made-save steps (1 for a made save; the old rule's callers pass 2).</summary>
    public int QueryStrength(bool boss, int repeatSteps, bool made, int madeSaveSteps) =>
        Math.Max(0, 2 - (boss ? 1 : 0) - Math.Clamp(repeatSteps, 0, MaxRepeatSteps) - (made ? Math.Clamp(madeSaveSteps, 0, 2) : 0));

    /// <summary>What Half means: half the seconds, rounded up to a whole second, at least 1.</summary>
    public int QueryHalfSeconds(int seconds) => Math.Max(1, (Math.Max(0, seconds) + 1) / 2);

    /// <summary>The Authority of a caster from the statistic and the rank of the skill (rank I is 1): the statistic plus 4 for each rank above I.</summary>
    public int QueryAuthority(int statistic, int skillRank) => Math.Max(0, statistic) + RankAuthority * Math.Max(0, skillRank - 1);

    /// <summary>The rank tag of a creature ("boss", "elite", ""): "" for an object that is not real (a wizard's), whatever tags it claims.</summary>
    private static string RankOf(ObjectRef creature) => creature.Domain == "" ? creature.Call<string>("QueryTagIn", "rank") ?? "" : "";

    /// <summary>Is this creature a boss, for a spell's own messages? A wizard's creature never is.</summary>
    public bool QueryWouldBeBoss(ObjectRef target) => target != null && target.IsAlive && RankOf(target) == "boss";

    // ---- The dice ----

    /// <summary>One roll in tenths of a percent, 0 to 999 (the fixed value if a test set one). A made save is a roll below ten times the chance.</summary>
    private int Roll() => fixedRoll >= 0 ? fixedRoll : Random(1000);

    private bool MayTest()
    {
        var who = ThisPlayer();
        var caller = PreviousObject();
        return !Self.IsClone && caller != null && caller.Domain == "" && TrustedTrees.Any(t => caller.ProgramPath.StartsWith(t, StringComparison.Ordinal)) && who != null && who.Call<int>("QueryWizLevel") >= 10;
    }

    /// <summary>Fix the dice, for tests: "random" (the default) or "save:NN.N", the roll in percent (0 to 99.9) every save then takes (a save is made if that is below the chance). Only trusted code acting for a wizard of level 10 may.</summary>
    public bool SetTestRolls(string mode)
    {
        if (!MayTest()) return false;
        if (mode == "random") { fixedRoll = -1; return true; }
        if (mode == null || !mode.StartsWith("save:")) return false;
        var parts = mode.Substring(5).Split('.');
        if (parts.Length > 2 || parts[0].Length == 0 || !parts[0].All(char.IsDigit) || (parts.Length == 2 && (parts[1].Length != 1 || !char.IsDigit(parts[1][0])))) return false;
        var tenths = int.Parse(parts[0]) * 10 + (parts.Length == 2 ? parts[1][0] - '0' : 0);
        if (tenths < 0 || tenths > 999) return false;
        fixedRoll = tenths;
        return true;
    }

    /// <summary>Move the daemon's clock forward by this many seconds (0 to 3600), for tests of the repeat window. Only trusted code acting for a wizard of level 10 may.</summary>
    public bool SetTestClock(int seconds)
    {
        if (!MayTest() || seconds < 0 || seconds > 3600) return false;
        clockShiftMs = seconds * 1000L;
        return true;
    }

    /// <summary>Forget every repeat record and every flood record, for tests. Only trusted code acting for a wizard of level 10 may.</summary>
    public bool ForgetAllRecords()
    {
        if (!MayTest()) return false;
        records.Clear();
        return true;
    }

    public string QueryTestRolls() => fixedRoll < 0 ? "random" : $"save:{fixedRoll / 10}.{fixedRoll % 10}";

    /// <summary>How many targets have a record (for tests and for the sandbox tests: it does not move for sandbox callers).</summary>
    public int QueryRecordCount() => records.Count;

    /// <summary>How many repeat steps a new application of this effect on this target would take off now (0 to 2).</summary>
    public int QueryRepeatSteps(ObjectRef target, string effectId)
    {
        if (target == null || !records.TryGetValue(target, out var record) || !record.Effects.TryGetValue(effectId ?? "", out var e)) return 0;
        return Now() - e.Last < WindowSeconds * 1000L ? Math.Min(MaxRepeatSteps, e.Count) : 0;
    }

    /// <summary>How many control effects have taken hold on this target in the last 60 seconds.</summary>
    public int QueryRecentEffects(ObjectRef target)
    {
        if (target == null || !records.TryGetValue(target, out var record)) return 0;
        var cutoff = Now() - WindowSeconds * 1000L;
        return record.Applied.Count(t => t > cutoff);
    }

    /// <summary>
    /// Mark a target for the caster's next control effect: that effect's save chance is lowered by this many points (the 5% floor still holds) when the
    /// caster next calls Resolve on this target within the seconds given, whether it holds or not; one mark per caster and target (a new one replaces the
    /// old). The mark is not a control effect: no save, no record against the flood guard. Refused (false) for a caller that is not real code, with
    /// restricted code on the stack, or a caster or target that is not a real living creature. 0 points or 0 seconds clears it.
    /// </summary>
    public bool AddSavePenalty(ObjectRef caster, ObjectRef target, int points, int seconds)
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller.Domain != "" || !TrustedTrees.Any(t => caller.ProgramPath.StartsWith(t, StringComparison.Ordinal)) || RestrictedCaller() != null) return false;
        if (!RealCreature(caster) || !RealCreature(target) || points < 0 || points > 50 || seconds < 0 || seconds > 600) return false;
        Prune();
        if (points == 0 || seconds == 0) { penalties.Remove((caster, target)); return true; }
        penalties[(caster, target)] = (points, Now() + seconds * 1000L);
        return true;
    }

    /// <summary>The points the caster's next effect on this target will have off its save chance (0 for none or lapsed). Read-only.</summary>
    public int QuerySavePenalty(ObjectRef caster, ObjectRef target) =>
        caster != null && target != null && penalties.TryGetValue((caster, target), out var p) && p.Until > Now() ? p.Points : 0;

    private long Now() => TimeMs() + clockShiftMs;

    private static string Cap(string text) => text.Length == 0 ? text : char.ToUpper(text[0]) + text.Substring(1);

    private bool RealCreature(ObjectRef ob) => ob != null && ob.IsAlive && ob.Domain == "" && Living(ob) && !ob.Call<bool>("IsDead");

    private void Prune()
    {
        if (penalties.Count > 0) foreach (var key in penalties.Where(kv => kv.Value.Until <= Now() || !kv.Key.caster.IsAlive || !kv.Key.target.IsAlive).Select(kv => kv.Key).ToList()) penalties.Remove(key);
        if (records.Count == 0) return;
        foreach (var dead in records.Keys.Where(k => !k.IsAlive).ToList()) records.Remove(dead);
        if (records.Count > 200)
        {
            var cutoff = Now() - WindowSeconds * 1000L;
            foreach (var old in records.Where(kv => kv.Value.Applied.All(t => t <= cutoff)).Select(kv => kv.Key).ToList()) records.Remove(old);
        }
    }

    /// <summary>
    /// Put a control effect on a target, or fail to. kind is "physical" or "mental" (which resistance the target rolls); skillRank is the caster's rank
    /// in the skill (1 for rank I); madeSaveSteps is how many steps a made save takes off (1; the legacy rule of "a made save means no effect" is 2);
    /// noun names the effect in the caster's message ("order", "form", "notice"). Returns 2 (Full), 1 (Half) or 0 (None), tells the caster why when it is
    /// not Full, and records the application unless it came out None. The skill then applies the effect for its whole term, or half of it
    /// (QueryHalfSeconds). A refused call (a caller that is not real code, any restricted code on the stack, a caster or target that is not a real
    /// living creature) answers Full and records nothing.
    /// </summary>
    public int Resolve(ObjectRef caster, ObjectRef target, string effectId, string kind, int skillRank, int madeSaveSteps = 1, string noun = "effect")
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller.Domain != "" || !TrustedTrees.Any(t => caller.ProgramPath.StartsWith(t, StringComparison.Ordinal)) || RestrictedCaller() != null) return 2;
        if (!RealCreature(caster) || !RealCreature(target) || string.IsNullOrEmpty(effectId) || effectId.Length > 64 || (kind != "physical" && kind != "mental")) return 2;
        Prune();
        var now = Now();
        var label = Cap(target.Call<string>("QueryLabel") ?? "it");
        noun = string.IsNullOrEmpty(noun) || noun.Length > 24 ? "effect" : noun;
        if (!records.TryGetValue(target, out var record)) { record = new Record(); records[target] = record; }
        record.Applied.RemoveAll(t => t <= now - WindowSeconds * 1000L);

        // The flood guard: four effects of any kind in the last minute, and the fifth does nothing.
        if (record.Applied.Count >= FloodLimit)
        {
            TellObject(caster, $"{label} has had notice enough for one minute.\n");
            return 0;
        }

        var casterLevel = Math.Max(1, caster.Call<int>("QueryLevel"));
        var targetLevel = Math.Max(1, target.Call<int>("QueryLevel"));
        var rank = RankOf(target);
        var boss = rank == "boss";
        var resistance = (int)Math.Round(target.Call<double>("QueryStat", kind + "_resistance"));
        var authority = QueryAuthority((int)Math.Round(caster.Call<double>("QueryStat", "authority")), skillRank);
        var penalty = penalties.Remove((caster, target), out var marked) && marked.Until > now ? marked.Points : 0; // the mark is used up by this resolution, hit or not
        authority += penalty;
        var chance = QuerySaveChance(resistance, targetLevel - casterLevel, rank == "elite", authority);
        var roll = Roll();
        var made = roll < chance * 10;
        var steps = 0;
        if (record.Effects.TryGetValue(effectId, out var last) && now - last.Last < WindowSeconds * 1000L) steps = Math.Min(MaxRepeatSteps, last.Count);
        var strength = QueryStrength(boss, steps, made, madeSaveSteps);

        // What the caster reads (docs/specs/tags-and-saves.md section 12.5): the cause of the lowest step, in the order boss, repeat, level, ordinary.
        string? line = null;
        if (strength < 2)
        {
            if (boss) line = strength == 1 ? $"{label} is above the law. The {noun} half holds." : $"{label} is above the law, and knows it. Nothing holds.";
            else if (steps > 0) line = strength == 1 ? $"{label} has been served with that already. It holds for half its term." : $"{label} has had notice of that already. It will not be served again for a while.";
            else if (made) line = targetLevel > casterLevel ? $"{label} ignores your authority. The {noun} holds for half its term." : $"{label} resists. The {noun} holds for half its term.";
        }
        var numbers = caster.Call<bool>("QueryVerbose") ? $" (save {chance}%, rolled {roll / 10}: {(made ? "made" : "failed")}; {(strength == 2 ? "Full" : strength == 1 ? "Half" : "None")})" : "";
        var text = line ?? "";
        if (numbers != "") text += text == "" ? numbers.TrimStart() : numbers;
        if (text != "") TellObject(caster, text + "\n");

        if (strength > 0)
        {
            var inWindow = record.Effects.TryGetValue(effectId, out var before) && now - before.Last < WindowSeconds * 1000L;
            record.Effects[effectId] = (now, (inWindow ? before.Count : 0) + 1);
            record.Applied.Add(now);
            while (record.Applied.Count > 8) record.Applied.RemoveAt(0);
        }
        return strength;
    }
}
