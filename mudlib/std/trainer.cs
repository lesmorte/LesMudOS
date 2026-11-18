// @inherit /std/npc

/// <summary>
/// A crafting trainer: an NPC who teaches recipes for gold. Add lessons in Create() with AddLesson("berry_juice", 10) (recipe id
/// and price; 0 is free). Players in the room get two commands: "lessons" lists what the trainer teaches, with prices and
/// whether you know it, and "learn NAME" pays and learns. Teaching goes through the crafting daemon's Teach, which checks that
/// the trainer's file lies in a trusted tree and refuses Secret recipes. Any other NPC (a quest giver) can teach the same way:
/// LoadObject("/secure/crafting").Call&lt;string&gt;("Teach", player, "recipe_id"). See docs/CRAFTING.md.
/// </summary>
public class Trainer : Npc
{
    private readonly List<(string Id, int Price)> lessons = new();

    /// <summary>Teach this recipe for this many gold.</summary>
    protected void AddLesson(string recipeId, int price) => lessons.Add((recipeId, Math.Max(0, price)));

    public List<string> QueryLessons() => lessons.Select(l => l.Id).ToList();

    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (!Interactive(other)) return;
        AddAction("lessons", CmdLessons);
        AddAction("learn", CmdLearn);
    }

    private ObjectRef Daemon => LoadObject("/secure/crafting");

    private bool CmdLessons(string args)
    {
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

    public override string Stat() => base.Stat() + $"  Trainer: {string.Join(", ", lessons.Select(l => l.Id + " " + l.Price))}\n";
}
