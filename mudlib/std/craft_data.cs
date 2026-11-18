/// <summary>
/// The base of a crafting data table: a file under /crafting whose Create() is nothing but lines of data. The crafting daemon
/// (/secure/crafting) finds every such file by listing /crafting, as the class registry finds classes, and reads its entries;
/// there is no list to maintain. A table has no code of its own. One file may hold any mix of entries. See docs/CRAFTING.md.
///
///   Number("harvest.base", 30)                      a number of a formula (all of them are in /crafting/numbers.cs)
///   Numbers("harvest.tool_quality", -10, 0, 5, ...) a row of numbers (one per quality, Poor to Legendary)
///   Proficiency("skinning", "Skinning", "gathering", "Taking the hides of beasts.")
///   Rank(25, "apprentice")                          the word for a proficiency from this number up
///   Difficulty("novice", 10)                        a recipe difficulty and the work level it stands for (0 to 100)
///   Station("herbalist table", "Herbalist Table", "herbalism")
///   Gather("skin", "skinning", "skinning knife")    which proficiency and tool a gathering command uses
///   Beast("wolf", "Wolf", 6, 4, 4)                  a creature kind: display name, skin value, blood value, difficulty
///   Beast("goblin", "Goblin", 0, 8, 4, scarceBlood: true)   its blood cannot be made by anything but bleeding
///   Material("honey", "/domains/hollow/obj/craft/honey", "sweetener")   a material id, its file and its categories
///   NodeKind("copper seam", "mining", "mining pick")  which proficiency and tool a node kind is gathered with (default: harvest's)
///   Yield("berry bush", "blackberries", 50, 2)      what a node of a kind gives: a material, its weight among the kind's yields, units
/// </summary>
public abstract class CraftData : MudObject
{
    private readonly List<Dictionary<string, string>> entries = new();
    private readonly Dictionary<string, List<double>> numbers = new();

    /// <summary>The entries of this table, for the crafting daemon. Each is a dictionary with "type" and the fields of that type.</summary>
    public List<Dictionary<string, string>> QueryEntries() => entries.Select(e => new Dictionary<string, string>(e)).ToList();

    /// <summary>The numbers of this table by key.</summary>
    public Dictionary<string, List<double>> QueryNumbers() => numbers.ToDictionary(kv => kv.Key, kv => new List<double>(kv.Value));

    private void Add(string type, params string[] pairs)
    {
        var entry = new Dictionary<string, string> { ["type"] = type, ["order"] = entries.Count.ToString() };
        for (var i = 0; i + 1 < pairs.Length; i += 2) entry[pairs[i]] = pairs[i + 1];
        entries.Add(entry);
    }

    protected void Number(string key, double value) => numbers[key] = new List<double> { value };

    protected void Numbers(string key, params double[] values) => numbers[key] = values.ToList();

    protected void Proficiency(string id, string name, string kind, string description) =>
        Add("proficiency", "id", id, "name", name, "kind", kind, "description", description);

    protected void Rank(int from, string word) => Add("rank", "from", from.ToString(), "word", word);

    protected void Difficulty(string word, int work) => Add("difficulty", "word", word, "work", work.ToString());

    protected void Station(string kind, string name, string proficiency) => Add("station", "kind", kind, "name", name, "proficiency", proficiency);

    protected void Gather(string verb, string proficiency, string tool) => Add("gather", "verb", verb, "proficiency", proficiency, "tool", tool);

    protected void Beast(string kind, string name, int skinValue, int bloodValue, int difficulty, bool scarceBlood = false) =>
        Add("beast", "kind", kind, "name", name, "skin", skinValue.ToString(), "blood", bloodValue.ToString(), "difficulty", difficulty.ToString(), "scarce", scarceBlood ? "1" : "0");

    protected void Material(string id, string path, string categories = "") => Add("material", "id", id, "path", path, "categories", categories);

    protected void NodeKind(string kind, string proficiency, string tool) => Add("nodekind", "kind", kind, "proficiency", proficiency, "tool", tool);

    protected void Yield(string nodeKind, string material, int weight = 1, int count = 1) =>
        Add("yield", "kind", nodeKind, "material", material, "weight", weight.ToString(), "count", count.ToString());
}
