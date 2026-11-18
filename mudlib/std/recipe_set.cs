/// <summary>
/// One recipe, written as a chain of calls in a recipe file's Create(). See RecipeSet.
/// </summary>
public sealed class RecipeEntry
{
    internal readonly Dictionary<string, string> Fields = new();
    private readonly List<string> ingredients = new();

    internal RecipeEntry(string id, string name, int order)
    {
        Fields["id"] = id;
        Fields["name"] = name;
        Fields["order"] = order.ToString();
        Fields["station"] = "";
        Fields["proficiency"] = "";
        Fields["difficulty"] = "simple";
        Fields["output"] = "";
        Fields["count"] = "1";
        Fields["discovery"] = "known";
        Fields["description"] = "";
    }

    internal Dictionary<string, string> Export()
    {
        var result = new Dictionary<string, string>(Fields) { ["ingredients"] = string.Join(",", ingredients) };
        return result;
    }

    /// <summary>An ingredient by material id ("healing_herb"), with a count.</summary>
    public RecipeEntry Needs(string material, int count = 1) { ingredients.Add("m:" + material + "*" + count); return this; }

    /// <summary>An ingredient by category ("berry": any material of that category), with a count.</summary>
    public RecipeEntry NeedsAny(string category, int count = 1) { ingredients.Add("c:" + category + "*" + count); return this; }

    /// <summary>The station kind it must be made at ("herbalist table"). Without this call: anywhere.</summary>
    public RecipeEntry At(string stationKind) { Fields["station"] = stationKind; return this; }

    /// <summary>The proficiency it uses ("herbalism") and its difficulty: simple, novice, apprentice, journeyman, expert, master.</summary>
    public RecipeEntry Skill(string proficiency, string difficulty) { Fields["proficiency"] = proficiency; Fields["difficulty"] = difficulty; return this; }

    /// <summary>What it makes: a material id from the materials table ("treated_leather") or an item path, and how many.</summary>
    public RecipeEntry Makes(string output, int count = 1) { Fields["output"] = output; Fields["count"] = count.ToString(); return this; }

    /// <summary>How it is found: known (taught by trainers and books), discovered, rare, secret (by experiment only).</summary>
    public RecipeEntry Discovery(string level) { Fields["discovery"] = level; return this; }

    /// <summary>A line shown by the recipe command.</summary>
    public RecipeEntry Description(string text) { Fields["description"] = text; return this; }
}

/// <summary>
/// The base of a recipe file under /recipes. The file's Create() holds nothing but recipes; the crafting daemon finds the file by
/// listing /recipes (and its sub-directories), so adding a recipe needs no list to edit and no change to any code. A file may hold
/// one recipe or many (one per proficiency or station is a good size). Every class name in the mudlib must be unique.
///
///   Recipe("minor_healing_salve", "Minor Healing Salve")
///       .Needs("healing_herb").Needs("wildflower")
///       .At("herbalist table").Skill("herbalism", "novice")
///       .Makes("/domains/hollow/obj/craft/minor_healing_salve")
///       .Discovery("known");
///
/// See docs/CRAFTING.md, "Recipes".
/// </summary>
public abstract class RecipeSet : MudObject
{
    private readonly List<RecipeEntry> recipes = new();

    /// <summary>Begin a recipe: its id (lower case letters, digits and _) and the name players see.</summary>
    protected RecipeEntry Recipe(string id, string name)
    {
        var entry = new RecipeEntry(id, name, recipes.Count);
        recipes.Add(entry);
        return entry;
    }

    /// <summary>The recipes of this file, for the crafting daemon.</summary>
    public List<Dictionary<string, string>> QueryRecipes() => recipes.Select(r => r.Export()).ToList();
}
