// @inherit /std/thing
// @use /std/skill

/// <summary>
/// The rank daemon (docs/specs/skill-ranks.md): the one object that may change a character's skill ranks, unlocks and the kill counters
/// unlocks read. A blueprint-only daemon.
///
///   * Requirement kinds, each one line of data on a trainer's offer: "level 5", "quest the-quiet-offering", "kills goblin 10",
///     "kills_with arcane_bolt 5", "renown 100", "standing assembly 10" (accepted, always unmet until factions exist), "item
///     /domains/x/obj/key" (shown to the trainer; "item /domains/x/obj/key take" takes it on purchase). A new kind is one more case
///     in Evaluate. The level line comes from the cap rule and is always there.
///   * Train: a trainer from a trusted tree sells the next rank to the player whose own command is running. All checks are made here.
///   * CreditKill: a dying creature from a trusted tree reports who shares its death and which skill struck the killing blow.
///
/// Nothing here trusts an argument for who is asking: every entry point that changes a character checks PreviousObject(). Code under
/// /wiz (the wizards' sandbox) can call none of them to any effect.
/// </summary>
public class RankDaemon : Thing
{
    private const string PlayerProgram = "/std/player";
    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    /// <summary>
    /// What a rank costs at a trainer whose offer names no price (docs/specs/skill-ranks.md, "Trainers"; starting values for the
    /// simulator): rank II 40 gold, III 150, IV 400, V 900. A rank with no price here is not offered unless a trainer names one.
    /// </summary>
    public static readonly Dictionary<int, int> RankPrices = new() { [2] = 40, [3] = 150, [4] = 400, [5] = 900 };

    /// <summary>The requirement kinds a line may name, in one place: Evaluate has one case for each.</summary>
    public static readonly string[] RequirementKinds = { "level", "quest", "kills", "kills_with", "renown", "standing", "item" };

    private static bool InTrustedTree(string path)
    {
        if (path.Contains("..") || path.Contains("//")) return false;
        foreach (var tree in TrustedTrees) if (path.StartsWith(tree, StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool Trusted(ObjectRef? ob) => ob != null && ob.IsAlive && InTrustedTree(ob.ProgramPath);

    private static bool IsPlayerBody(ObjectRef? ob) => ob != null && ob.IsAlive && ob.IsClone && ob.ProgramPath == PlayerProgram;

    /// <summary>The default price of a rank, or 0 if there is none.</summary>
    public int PriceFor(int rank) => RankPrices.TryGetValue(rank, out var p) ? p : 0;

    public List<string> QueryRequirementKinds() => RequirementKinds.ToList();

    private ObjectRef? SkillFile(string id)
    {
        try { return LoadObject("/secure/classes").Call<ObjectRef>("Skill", id); }
        catch (MudRuntimeException) { return null; }
    }

    // ------------------------------------------------------------------
    // Requirements
    // ------------------------------------------------------------------

    private static string Plural(string word)
    {
        if (word.EndsWith("f")) return word.Substring(0, word.Length - 1) + "ves";
        if (word.EndsWith("y") && word.Length > 1 && "aeiou".IndexOf(word[word.Length - 2]) < 0) return word.Substring(0, word.Length - 1) + "ies";
        if (word.EndsWith("s") || word.EndsWith("x") || word.EndsWith("ch") || word.EndsWith("sh")) return word + "es";
        return word + "s";
    }

    private static string Title(string id) => id.Length == 0 ? id : char.ToUpper(id[0]) + id.Substring(1).Replace('_', ' ').Replace('-', ' ');

    /// <summary>Is this requirement line one the game knows (a known kind with the right words)?</summary>
    public bool QueryValidRequirement(string line) => Parse(line, out _, out _) && Evaluate(null, line).Known;

    private static bool Parse(string line, out string kind, out string[] args)
    {
        var words = (line ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        kind = words.Length > 0 ? words[0].ToLowerInvariant() : "";
        args = words.Skip(1).ToArray();
        return words.Length > 0;
    }

    private sealed class Verdict
    {
        public readonly bool Met, Known;
        public readonly string Progress;
        public Verdict(bool met, string progress, bool known) { Met = met; Progress = progress; Known = known; }
    }

    /// <summary>
    /// One requirement for one character: met or not, and the progress line ("Goblins slain: 6 of 10"). who may be null (only the
    /// form is checked). An unknown kind, or a line with the wrong words, is never met: it fails closed.
    /// </summary>
    private Verdict Evaluate(ObjectRef? who, string line)
    {
        if (!Parse(line, out var kind, out var a)) return new Verdict(false, "A requirement the game does not know.", false);
        int Num(int i) => i < a.Length && int.TryParse(a[i], out var n) && n >= 0 ? n : -1;
        switch (kind)
        {
            case "level":
            {
                var need = Num(0);
                if (a.Length != 1 || need < 0) break;
                var have = who?.Call<int>("QueryLevel") ?? 0;
                return new Verdict(have >= need, $"Level {need}" + (who == null ? "" : have >= need ? ": done" : $" (you are level {have})"), true);
            }
            case "quest":
            {
                if (a.Length != 1) break;
                var done = who != null && who.Call<int>("QueryQuest", a[0]) == 2;
                return new Verdict(done, $"Quest \"{Title(a[0])}\": " + (done ? "done" : "not yet done"), true);
            }
            case "kills":
            {
                var need = Num(1);
                if (a.Length != 2 || need < 0) break;
                var have = who?.Call<int>("QueryKillsOfKind", a[0].ToLowerInvariant()) ?? 0;
                return new Verdict(have >= need, $"{Title(Plural(a[0].ToLowerInvariant()))} slain: {Math.Min(have, need)} of {need}", true);
            }
            case "kills_with":
            {
                var need = Num(1);
                if (a.Length != 2 || need < 0) break;
                var name = SkillFile(a[0])?.Call<string>("QueryTitle") ?? Title(a[0]);
                var have = who?.Call<int>("QueryKillsWith", a[0]) ?? 0;
                return new Verdict(have >= need, $"Killing blows with {name}: {Math.Min(have, need)} of {need}", true);
            }
            case "renown":
            {
                var need = Num(0);
                if (a.Length != 1 || need < 0) break;
                var have = 0;
                if (who != null)
                {
                    try { have = LoadObject("/secure/renown").Call<int>("QueryRenown", who.Call<string>("QueryName") ?? ""); }
                    catch (MudRuntimeException) { have = 0; }
                }
                return new Verdict(have >= need, $"Renown: {Math.Min(have, need)} of {need}", true);
            }
            case "standing":
            {
                // Accepted in data so that an offer can be written now; never met until factions are built (docs/FACTIONS.md).
                var need = Num(1);
                if (a.Length != 2 || need < 0) break;
                return new Verdict(false, $"Standing with the {Title(a[0])}: {need} (nobody keeps that account yet)", true);
            }
            case "item":
            {
                if (a.Length < 1 || a.Length > 2 || !a[0].StartsWith("/") || (a.Length == 2 && a[1] != "take")) break;
                string label;
                try { label = LoadObject(a[0]).Call<string>("Short") ?? Title(a[0].Substring(a[0].LastIndexOf('/') + 1)); }
                catch (MudRuntimeException) { label = Title(a[0].Substring(a[0].LastIndexOf('/') + 1)); }
                var carried = who != null && Carried(who, a[0]) != null;
                return new Verdict(carried, (a.Length == 2 ? "To hand over: " : "To show: ") + label + (carried ? " (you have it)" : " (you do not have it)"), true);
            }
        }
        return new Verdict(false, "A requirement the game does not know yet.", false);
    }

    private ObjectRef? Carried(ObjectRef who, string path) =>
        AllInventory(who).FirstOrDefault(o => o.IsAlive && o.ProgramPath == path && !who.Call<bool>("IsEquipped", o)) ??
        AllInventory(who).FirstOrDefault(o => o.IsAlive && o.ProgramPath == path);

    /// <summary>The level line every offer has, from the cap rule.</summary>
    public static string LevelLine(int rank) => "level " + Skill.LevelForRank(rank);

    /// <summary>
    /// The state of an offer for a character, for a trainer's `ranks` list: one entry per requirement, "1|progress" if met or "0|progress"
    /// if not, the level line first. Read-only. An unlock already recorded makes every requirement but the level count as met.
    /// </summary>
    public List<string> OfferState(ObjectRef who, string trainerPath, string skillId, int rank, List<string> requirements)
    {
        var unlocked = who != null && who.Call<bool>("QueryRankUnlocked", UnlockKey(trainerPath, skillId, rank));
        var result = new List<string>();
        foreach (var line in new[] { LevelLine(rank) }.Concat(requirements ?? new List<string>()))
        {
            var v = Evaluate(who, line);
            var kept = unlocked && v.Known && !v.Met && !line.StartsWith("level ") && !(line.StartsWith("item ") && line.EndsWith(" take"));
            result.Add((v.Met || kept ? "1|" : "0|") + v.Progress + (kept ? " (met before: it stays met)" : ""));
        }
        return result;
    }

    /// <summary>
    /// A trainer's `ranks` list records, for good, the unlock of every offer whose requirements the player meets now. The same guards
    /// as Train: a trainer from a trusted tree, the player's own command, the same room. True if an unlock was recorded.
    /// </summary>
    public bool RecordIfMet(ObjectRef who, string skillId, int rank, List<string> requirements)
    {
        if (Self.IsClone) return false;
        var trainer = PreviousObject();
        if (!Trusted(trainer) || !trainer!.IsClone || !trainer.Call<bool>("IsTrainer")) return false;
        if (!IsPlayerBody(who) || ThisPlayer() != who || Environment(who) == null || Environment(who) != Environment(trainer)) return false;
        var reqs = (requirements ?? new List<string>()).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        if (reqs.Count == 0 || who.Call<int>("QuerySkillRank", skillId ?? "") < 1) return false;
        var key = UnlockKey(trainer.ProgramPath, skillId!, rank);
        if (who.Call<bool>("QueryRankUnlocked", key)) return false;
        foreach (var line in reqs) if (!Evaluate(who, line).Met) return false;
        if (!who.Call<bool>("RankRecordUnlock", key)) return false;
        who.Call("RankSave");
        return true;
    }

    private static string UnlockKey(string trainerPath, string skillId, int rank) => $"{trainerPath}|{skillId}|{rank}";

    // ------------------------------------------------------------------
    // Training
    // ------------------------------------------------------------------

    /// <summary>
    /// Sell the next rank of a skill. The caller must be a trainer from a trusted tree, in the room of the player, and the player's own
    /// command must be running (ThisPlayer). Checks, in order: the player has the skill; the rank is the next one and exists; the level
    /// cap; every requirement (or the unlock already recorded at this trainer); the gold. The gold is always paid. Returns "ok" or
    /// "refused|" and the reason in words. The character is told nothing here; the trainer speaks.
    /// </summary>
    public string Train(ObjectRef who, string skillId, int rank, int price, List<string> requirements)
    {
        if (Self.IsClone) return "refused|That is not possible now.";
        var trainer = PreviousObject();
        if (!Trusted(trainer) || !trainer!.IsClone || !trainer.Call<bool>("IsTrainer")) return "refused|That is not possible now.";
        if (!IsPlayerBody(who) || ThisPlayer() != who || Environment(who) == null || Environment(who) != Environment(trainer)) return "refused|That is not possible now.";
        var skill = SkillFile(skillId ?? "");
        if (skill == null) return "refused|There is no such skill.";
        var title = skill.Call<string>("QueryTitle") ?? skillId!;
        var current = who.Call<int>("QuerySkillRank", skillId);
        if (current < 1) return $"refused|{title} is not one of your skills.";
        if (!skill.Call<bool>("QueryHasRanks")) return $"refused|{title} has no ranks to learn.";
        if (rank != current + 1) return current >= rank ? $"refused|You already know {skill.Call<string>("QueryTitleAt", current)}." : $"refused|Ranks cannot be skipped: learn {skill.Call<string>("QueryTitleAt", current + 1)} first.";
        if (!skill.Call<bool>("QueryRankExists", rank)) return $"refused|{title} has no rank {Skill.Roman(rank)}.";
        var name = skill.Call<string>("QueryTitleAt", rank) ?? title;
        var level = who.Call<int>("QueryLevel");
        if (level < Skill.LevelForRank(rank)) return $"refused|{name} needs level {Skill.LevelForRank(rank)}. You are level {level}.";
        if (price < 0) return "refused|That is not for sale.";
        var key = UnlockKey(trainer.ProgramPath, skillId!, rank);
        var unlocked = who.Call<bool>("QueryRankUnlocked", key);
        var reqs = (requirements ?? new List<string>()).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        var unmet = new List<string>();
        foreach (var line in reqs)
        {
            var v = Evaluate(who, line);
            if (!v.Known) unmet.Add(v.Progress);
            else if (!v.Met && !unlocked) unmet.Add(v.Progress);
            else if (!v.Met && line.StartsWith("item ") && line.EndsWith(" take")) unmet.Add(v.Progress); // a thing to hand over is needed each time
        }
        if (unmet.Count > 0) return "unmet|" + string.Join("|", unmet);
        // Met: the unlock is recorded for good, whether or not the gold is there today.
        if (!unlocked && reqs.Count > 0) who.Call<bool>("RankRecordUnlock", key);
        var gold = who.Call<int>("QueryGold");
        if (gold < price) { who.Call("RankSave"); return $"refused|{name} costs {price} gold, and you have {gold}."; }
        // Hand over what the offer takes, before the gold.
        foreach (var line in reqs.Where(r => r.StartsWith("item ") && r.EndsWith(" take")))
        {
            var path = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1];
            var thing = Carried(who, path);
            if (thing == null) return "refused|You do not have what this needs.";
            thing.Call("Vanish");
            if (thing.IsAlive) return "refused|You do not have what this needs.";
        }
        if (!who.Call<bool>("RankSetSkill", skillId, rank)) return "refused|That is not possible now.";
        if (price > 0 && !who.Call<bool>("RankPay", price))
        {
            who.Call<bool>("RankSetSkill", skillId, current);   // no gold, no rank
            return $"refused|{name} costs {price} gold, and you have {gold}.";
        }
        who.Call("RankSave");
        return "ok";
    }

    // ------------------------------------------------------------------
    // Kill counters
    // ------------------------------------------------------------------

    /// <summary>
    /// A creature has died: everyone who shares the death counts one of its kind, and the one who struck the killing blow with a skill
    /// counts one killing blow with it. The caller must be the creature itself, a clone from a trusted tree, already dead; the
    /// participants must be player bodies in its room (the eligible players of Living.EligibleDamage). Returns how many were counted.
    /// </summary>
    public int CreditKill(List<ObjectRef> participants, ObjectRef? killer, string kind, string skillId)
    {
        if (Self.IsClone) return 0;
        var creature = PreviousObject();
        if (!Trusted(creature) || !creature!.IsClone || !creature.Call<bool>("IsMonster") || !creature.Call<bool>("IsDead")) return 0;
        kind = (kind ?? "").Trim().ToLowerInvariant();
        if (kind == "" || kind.Length > 40 || creature.Call<string>("QueryCreatureKind") != kind) return 0;
        var room = Environment(creature);
        var counted = 0;
        foreach (var who in (participants ?? new List<ObjectRef>()).Distinct())
        {
            if (!IsPlayerBody(who) || room == null || Environment(who) != room) continue;
            if (who.Call<bool>("RankCountKill", kind)) counted++;
            if (who == killer && !string.IsNullOrEmpty(skillId) && SkillFile(skillId) != null) who.Call<bool>("RankCountKillWith", skillId);
        }
        return counted;
    }
}
