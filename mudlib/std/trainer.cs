// @inherit /std/shopkeeper
// @use /std/skill

/// <summary>
/// A trainer: an NPC who teaches for gold. Two kinds of teaching, either or both:
///
///   * Crafting recipes. Add lessons in Create() with AddLesson("berry_juice", 10) (recipe id and price; 0 is free). Players in the
///     room get "lessons" (what the trainer teaches, with prices and whether you know it) and "learn NAME" (pay and learn). Teaching
///     goes through the crafting daemon's Teach, which checks that the trainer's file lies in a trusted tree and refuses Secret
///     recipes. See docs/CRAFTING.md.
///   * Skill ranks (docs/specs/skill-ranks.md). TeachCalling("mage", 2, 2) offers rank II of every ranked skill of the Mage's calling;
///     AddRankOffer("arcane_bolt", 2, 40, "kills goblin 10") is one offer with its price and requirement lines (one line each; the
///     level line comes from the cap rule); AddRankLine("kills goblin 10", "...") is what the trainer says of an unmet requirement.
///     Players get "ranks" (every offer for their calling, its price, and each requirement with progress) and "train SKILL" (buy the
///     next rank). All the checks and the change to the character are the rank daemon's, /secure/ranks.
///
/// A trainer is also a shopkeeper: give it AddStock lines and it sells (browse, buy, sell, value). Any other NPC (a quest giver) can
/// teach a recipe the same way: LoadObject("/secure/crafting").Call&lt;string&gt;("Teach", player, "recipe_id").
/// </summary>
public class Trainer : Shopkeeper
{
    private readonly List<(string Id, int Price)> lessons = new();

    /// <summary>Teach this recipe for this many gold.</summary>
    protected void AddLesson(string recipeId, int price) => lessons.Add((recipeId, Math.Max(0, price)));

    public List<string> QueryLessons() => lessons.Select(l => l.Id).ToList();

    public bool IsTrainer() => true;

    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (!Interactive(other)) return;
        if (lessons.Count > 0)
        {
            AddAction("lessons", CmdLessons);
            AddAction("learn", CmdLearn);
        }
        if (callings.Count > 0 || offers.Count > 0)
        {
            AddAction("ranks", CmdRanks);
            AddAction("train", CmdTrain);
        }
    }

    private ObjectRef Daemon => LoadObject("/secure/crafting");

    private bool CmdLessons(string args)
    {
        if (TooDark()) return true;
        var who = ThisPlayer();
        if (who == null) return false;
        var text = new StringBuilder($"{Name} can teach you:\n");
        var any = false;
        foreach (var (id, price) in lessons)
        {
            var info = Daemon.Call<Dictionary<string, string>>("QueryRecipeInfo", id);
            if (info == null) continue;
            any = true;
            var known = who.Call<bool>("QueryKnowsRecipe", id);
            text.Append($"  {{cmd}}{info["name"]}{{/}}{new string(' ', Math.Max(1, 28 - info["name"].Length))}{(price == 0 ? "free" : price + " gold")}{(known ? " (you know it)" : "")}\n");
        }
        if (!any) text.Append("  nothing at the moment.\n");
        else text.Append("Type {cmd}learn{/} NAME.\n");
        Write(text.ToString());
        return true;
    }

    private bool CmdLearn(string args)
    {
        if (TooDark()) return true;
        var who = ThisPlayer();
        if (who == null) return false;
        if (args.Trim() == "") { NotifyFail("Learn what? Type {cmd}lessons{/}."); return false; }
        var key = args.Trim().ToLowerInvariant();
        (string Id, int Price)? found = null;
        string name = "";
        foreach (var lesson in lessons)
        {
            var info = Daemon.Call<Dictionary<string, string>>("QueryRecipeInfo", lesson.Id);
            if (info == null) continue;
            var n = info["name"];
            if (n.ToLowerInvariant() == key || lesson.Id == key.Replace(' ', '_') || n.ToLowerInvariant().StartsWith(key)) { found = lesson; name = n; break; }
        }
        if (found == null) { Write($"{Name} does not teach '{Esc(args.Trim())}'. Type {{cmd}}lessons{{/}}.\n"); return true; }
        var (id, price) = found.Value;
        if (who.Call<bool>("QueryKnowsRecipe", id)) { Write($"You already know how to make {name}.\n"); return true; }
        if (price > 0 && !who.Call<bool>("AddGold", -price)) { Write($"{name} costs {price} gold to learn, and you do not have it.\n"); return true; }
        var answer = Daemon.Call<string>("Teach", who, id) ?? "refused";
        if (!answer.StartsWith("ok"))
        {
            if (price > 0) who.Call("AddGold", price);
            Write($"{Name} cannot teach you that.\n");
            return true;
        }
        if (price > 0) Write($"You pay {Name} {price} gold.\n");
        Say($"{Name} shows {who.Call<string>("QueryName")} how to make {name}.\n");
        return true;
    }

    // ---- Skill ranks ----

    private sealed class Offer
    {
        public string Skill = "";
        public int Rank;
        public int Price = -1; // -1: the rank daemon's price for the rank
        public List<string> Requirements = new();
    }

    private readonly List<(string Calling, int From, int To)> callings = new();
    private readonly List<Offer> offers = new();
    private readonly Dictionary<string, string> rankLines = new();

    /// <summary>The trainer's words when a rank is not taught here ("Rank III and above are taught in Oakenfell ...").</summary>
    protected string ElsewhereLine = "";

    /// <summary>
    /// Teach ranks from to to of every skill of a calling (an archetype id) that has ranks, at the daemon's prices and on level alone.
    /// Read from the class file when a player asks, so a skill added to the calling is taught without a change here.
    /// </summary>
    protected void TeachCalling(string calling, int fromRank, int toRank) => callings.Add((calling, Math.Max(2, fromRank), Math.Max(fromRank, toRank)));

    /// <summary>
    /// One offer: a rank of a skill, its price (-1 for the daemon's price for the rank) and its requirement lines. It replaces the offer
    /// TeachCalling would make for the same skill and rank. The callings taught are still those of TeachCalling.
    /// </summary>
    protected void AddRankOffer(string skillId, int rank, int price, params string[] requirements)
    {
        offers.RemoveAll(o => o.Skill == skillId && o.Rank == rank);
        offers.Add(new Offer { Skill = skillId, Rank = rank, Price = price, Requirements = requirements.Where(r => !string.IsNullOrWhiteSpace(r)).ToList() });
    }

    /// <summary>What the trainer says beside an unmet requirement (the requirement line, as in AddRankOffer).</summary>
    protected void AddRankLine(string requirement, string line) => rankLines[requirement] = line;

    private ObjectRef Ranks => LoadObject("/secure/ranks");

    private ObjectRef? SkillFile(string id)
    {
        try { return LoadObject("/secure/classes").Call<ObjectRef>("Skill", id); }
        catch (MudRuntimeException) { return null; }
    }

    private ObjectRef? ClassFile(string id)
    {
        try { return LoadObject("/secure/classes").Call<ObjectRef>("Class", id); }
        catch (MudRuntimeException) { return null; }
    }

    /// <summary>The callings this trainer teaches, by id.</summary>
    public List<string> QueryRankCallings() => callings.Select(c => c.Calling).Distinct().ToList();

    private string CallingName(string id) => ClassFile(id)?.Call<string>("QueryClassName") ?? char.ToUpper(id[0]) + id.Substring(1);

    private static string Plural(string calling) => calling.EndsWith("y") ? calling.Substring(0, calling.Length - 1) + "ies" : calling + "s";

    /// <summary>Every offer of this trainer for one calling, in the order of the class's skills and by rank. Prices resolved.</summary>
    private List<Offer> OffersFor(string calling)
    {
        var result = new List<Offer>();
        foreach (var (c, from, to) in callings.Where(c => c.Calling == calling))
        {
            var skills = ClassFile(c)?.Call<List<string>>("QuerySkills") ?? new List<string>();
            foreach (var id in skills)
            {
                var skill = SkillFile(id);
                if (skill == null || !skill.Call<bool>("QueryHasRanks")) continue;
                for (var r = from; r <= to; r++)
                {
                    if (!skill.Call<bool>("QueryRankExists", r)) break;
                    var own = offers.FirstOrDefault(o => o.Skill == id && o.Rank == r);
                    var price = own?.Price >= 0 ? own.Price : Ranks.Call<int>("PriceFor", r);
                    if (price <= 0 && own == null) continue; // no price for this rank yet: not offered
                    result.Add(new Offer { Skill = id, Rank = r, Price = price, Requirements = own?.Requirements ?? new List<string>() });
                }
            }
        }
        return result;
    }

    /// <summary>The offers for a calling as "skill id|rank|price|requirement;requirement", for tests and tools. Read-only.</summary>
    public List<string> QueryRankOffers(string calling) =>
        OffersFor(calling ?? "").Select(o => $"{o.Skill}|{o.Rank}|{o.Price}|{string.Join(";", o.Requirements)}").ToList();

    /// <summary>Does another trainer in this room teach this calling? Then this one leaves the command to it.</summary>
    private bool AnotherTeaches(string calling)
    {
        var room = Environment();
        if (room == null) return false;
        return AllInventory(room).Any(o => o != Self && o.IsAlive && o.Call<bool>("IsTrainer") && (o.Call<List<string>>("QueryRankCallings") ?? new List<string>()).Contains(calling));
    }

    /// <summary>The calling this trainer answers a player as, or null when it should stay quiet (another here teaches them).</summary>
    private string? CallingOf(ObjectRef who, out string refusal)
    {
        refusal = "";
        // An advanced class this trainer teaches comes first, so a holder still buys the calling's skills from the calling's trainers.
        var advanced = who.Call<string>("QueryClassOfTier", "advanced") ?? "";
        if (advanced != "" && QueryRankCallings().Contains(advanced)) return advanced;
        var calling = who.Call<string>("QueryArchetype") ?? "";
        if (QueryRankCallings().Contains(calling)) return calling;
        if (calling != "" && AnotherTeaches(calling)) return null;
        var taught = string.Join(" and ", QueryRankCallings().Select(c => Plural(CallingName(c))));
        refusal = calling == "" ? $"{Name} teaches {taught}, and you have no calling yet."
            : $"{Name} teaches {taught}, and you are {Article(CallingName(calling))}. Your own calling's trainer can teach you.";
        return "";
    }

    private static string Article(string word) => ("AEIOU".IndexOf(char.ToUpper(word[0])) >= 0 ? "an " : "a ") + word;

    /// <summary>May this trainer teach this player at all? A trainer of a hidden class answers only its holders; anyone else gets the unknown-command answer.</summary>
    protected virtual bool TeachesTo(ObjectRef who) => true;

    private bool CmdRanks(string args)
    {
        var who = ThisPlayer();
        if (who == null || !TeachesTo(who)) return false;
        var calling = CallingOf(who, out var refusal);
        if (calling == null) return false;
        if (calling == "") { Write(refusal + "\n"); return true; }
        var list = OffersFor(calling);
        var text = new StringBuilder($"{Name} teaches {Plural(CallingName(calling))}:\n");
        if (list.Count == 0) text.Append("  nothing at the moment.\n");
        foreach (var o in list)
        {
            var skill = SkillFile(o.Skill);
            if (skill == null) continue;
            var held = who.Call<int>("QuerySkillRank", o.Skill);
            Ranks.Call<bool>("RecordIfMet", who, o.Skill, o.Rank, o.Requirements);
            var state = Ranks.Call<List<string>>("OfferState", who, Self.ProgramPath, o.Skill, o.Rank, o.Requirements) ?? new List<string>();
            var allMet = state.All(l => l.StartsWith("1|"));
            var skillLevel = ClassFile(calling)?.Call<int>("QuerySkillLevel", o.Skill) ?? 0;
            var status = held < 1 && skillLevel > who.Call<int>("QueryLevel") ? $"at level {skillLevel}"
                : held < 1 ? "not one of your skills"
                : held >= o.Rank ? "you know it"
                : held < o.Rank - 1 ? $"learn {skill.Call<string>("QueryTitleAt", o.Rank - 1)} first"
                : allMet ? "you can learn it now" : "locked";
            var name = skill.Call<string>("QueryTitleAt", o.Rank) ?? o.Skill;
            var word = skill.Call<string>("QueryCommand") ?? "";
            text.Append($"  {{cmd}}{name}{{/}} ({word}){new string(' ', Math.Max(1, 26 - name.Length - word.Length))}{o.Price,4} gold   {status}\n");
            if (held >= o.Rank) continue;
            for (var i = 0; i < state.Count; i++)
            {
                var met = state[i].StartsWith("1|");
                text.Append($"      {(met ? "[x]" : "[ ]")} {state[i].Substring(2)}\n");
                var line = i == 0 ? "" : o.Requirements[i - 1];
                if (!met && line != "" && rankLines.TryGetValue(line, out var said)) text.Append($"          {Name} says: \"{said}\"\n");
            }
            if (state.Count > 0 && state.Skip(1).Any(l => l.Contains("nobody keeps that account yet")))
                text.Append($"          {Name} says: \"There is no one to vouch for that yet. When there is, I will know.\"\n");
        }
        text.Append("The gold is always paid. Type {cmd}train{/} SKILL to learn the next rank.\n");
        if (ElsewhereLine != "") text.Append(ElsewhereLine + "\n");
        Write(text.ToString());
        return true;
    }

    private bool CmdTrain(string args)
    {
        var who = ThisPlayer();
        if (who == null || !TeachesTo(who)) return false;
        var calling = CallingOf(who, out var refusal);
        if (calling == null) return false;
        if (calling == "") { Write(refusal + "\n"); return true; }
        var key = args.Trim().ToLowerInvariant();
        if (key == "") { NotifyFail("Train what? Type {cmd}ranks{/} to see what is taught here."); return false; }
        if (who.Call<bool>("QueryInCombat")) { Write("Not while you are fighting.\n"); return true; }
        var list = OffersFor(calling);
        // The skill by its command word, its id, or its name (any rank's name), or the start of one.
        ObjectRef? skill = null;
        string id = "";
        foreach (var sid in list.Select(o => o.Skill).Distinct())
        {
            var s = SkillFile(sid);
            if (s == null) continue;
            var names = new List<string> { s.Call<string>("QueryCommand") ?? "", sid, sid.Replace('_', ' '), (s.Call<string>("QueryTitle") ?? "").ToLowerInvariant() };
            for (var r = 2; r <= 6; r++) names.Add((s.Call<string>("QueryTitleAt", r) ?? "").ToLowerInvariant());
            if (names.Contains(key) || names.Any(n => n.Length > 0 && key.Length >= 3 && n.StartsWith(key))) { skill = s; id = sid; break; }
        }
        if (skill == null)
        {
            // Perhaps a skill of the calling that is not taught here at all (rank I only, or another trainer's).
            Write($"{Name} does not teach '{Esc(args.Trim())}'. Type {{cmd}}ranks{{/}}.\n");
            return true;
        }
        var held = who.Call<int>("QuerySkillRank", id);
        var title = skill.Call<string>("QueryTitle") ?? id;
        if (held < 1) { Write($"{title} is not one of your skills.\n"); return true; }
        var next = held + 1;
        var offer = list.FirstOrDefault(o => o.Skill == id && o.Rank == next);
        if (offer == null)
        {
            var mine = list.Where(o => o.Skill == id).Select(o => o.Rank).ToList();
            if (!skill.Call<bool>("QueryRankExists", next)) Write($"You know {skill.Call<string>("QueryTitleAt", held)}, the highest rank there is.\n");
            else if (mine.Count > 0 && next > mine.Max()) Write($"{Name} teaches {title} no further than rank {Skill.Roman(mine.Max())}. " + ElsewhereLine + "\n");
            else Write($"{Name} does not teach {skill.Call<string>("QueryTitleAt", next)}. " + ElsewhereLine + "\n");
            return true;
        }
        var answer = Ranks.Call<string>("Train", who, id, next, offer.Price, offer.Requirements) ?? "refused|That is not possible now.";
        var name = skill.Call<string>("QueryTitleAt", next) ?? title;
        if (answer.StartsWith("unmet|"))
        {
            var lines = answer.Substring(6).Split('|');
            var text = new StringBuilder($"{Name} says: \"Not yet.\" {name} asks for more than gold:\n");
            foreach (var l in lines) text.Append($"  [ ] {l}\n");
            foreach (var req in offer.Requirements)
                if (rankLines.TryGetValue(req, out var said) && lines.Any(l => l.Length > 0)) { text.Append($"{Name} says: \"{said}\"\n"); break; }
            text.Append("No gold has been taken.\n");
            Write(text.ToString());
            return true;
        }
        if (!answer.StartsWith("ok"))
        {
            Write(answer.Substring(answer.IndexOf('|') + 1) + (answer.Contains("costs") || answer.Contains("needs level") ? " No gold has been taken." : "") + "\n");
            return true;
        }
        who.Call("RestDisturbed", "buy");
        Write($"You pay {Name} {offer.Price} gold. {Name} works with you until it comes right.\nYou have learned {{cmd}}{name}{{/}}. It answers to {{cmd}}{skill.Call<string>("QueryCommand")}{{/}}, as before.\n");
        Say($"{Name} teaches {who.Call<string>("QueryName")} {name}.\n");
        return true;
    }

    public override string Stat() => base.Stat() + $"  Trainer: {string.Join(", ", lessons.Select(l => l.Id + " " + l.Price))}" +
        (callings.Count == 0 ? "" : $"; ranks: {string.Join(", ", callings.Select(c => $"{c.Calling} {Skill.Roman(c.From)}-{Skill.Roman(c.To)}"))}") + "\n";
}
