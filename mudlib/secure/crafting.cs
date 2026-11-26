// @inherit /std/thing
// @use /std/item

/// <summary>
/// The crafting and gathering engine (docs/CRAFTING.md). A blueprint-only daemon, preloaded at boot, that
///
///   * reads the crafting data: every table file under /crafting (numbers, proficiencies, ranks, difficulties, stations,
///     gathering, beasts, materials, node yields) and every recipe file under /recipes. The master lets only this blueprint list
///     those two trees. Files are found by listing, like classes, and checked again every few seconds, so a new file needs no
///     list and no code change; an edited file is picked up after `update`;
///   * indexes the recipes for the matcher (unordered, by material id or category, quality ignored), refuses clashes when they
///     load and reports them in the driver log;
///   * runs every crafting and gathering command for a player body (skin, bleed, harvest, craft, combine, recipes, recipe,
///     proficiencies): the body binds the verbs and forwards them here; the timed part of an action comes back here through the
///     body's combat clock;
///   * teaches recipes for trainers, books and quests (Teach), runs healing over time for crafted consumables (StartHeal).
///
/// Nothing here trusts an argument for who is asking: every entry point checks PreviousObject(). Code under /wiz (the wizards'
/// sandbox) can call none of the entry points that change a character, a corpse, a node or an item.
/// </summary>
public class CraftingDaemon : Thing
{
    private const string DataDir = "/crafting";
    private const string RecipeDir = "/recipes";
    private const string PlayerProgram = "/std/player";
    private const int MaxListDepth = 3;
    private const int MaxExpansion = 5000;
    private const int MaxBatch = 50;

    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };
    private static readonly string[] DiscoveryLevels = { "known", "discovered", "rare", "secret" };

    // ------------------------------------------------------------------
    // The data model
    // ------------------------------------------------------------------

    private sealed class Prof { public string Id = "", Name = "", Kind = "", Description = ""; public int Order; }
    private sealed class StationDef { public string Kind = "", Name = "", Proficiency = ""; }
    private sealed class GatherDef { public string Verb = "", Proficiency = "", Tool = ""; }
    private sealed class BeastDef { public string Kind = "", Name = ""; public int Skin, Blood, Difficulty; public bool Scarce; }
    private sealed class Mat { public string Id = "", Path = "", Kind = "", Record = "", Source = ""; public List<string> Categories = new(); public bool Scarce; }
    private sealed class YieldDef { public string Material = ""; public int Weight = 1, Count = 1; }
    private sealed class NodeDef { public string Kind = "", Proficiency = "", Tool = ""; public List<YieldDef> Yields = new(); }
    private sealed class Slot { public string Token = ""; public int Count = 1; public bool IsCategory => Token.StartsWith("c:"); public string Name => Token.Substring(2); }

    private sealed class Recipe
    {
        public string Id = "", Name = "", Station = "", Proficiency = "", Difficulty = "", Output = "", Discovery = "", Description = "", Source = "";
        public int Work, Count = 1, Order, Specificity;
        public List<Slot> Slots = new();
        public string Key = "";
    }

    private sealed class Job
    {
        public string Verb = "";
        public ObjectRef? Target;
        public string RecipeId = "";
        public int Remaining = 1;
        public int Made;
        public bool Best;
        public List<ObjectRef> Prefer = new();
        public List<string> Offered = new(); // combine: the material ids offered
    }

    private sealed class HealOverTime { public ObjectRef Who = null!; public int PerTick, Left, Seconds, Generation; public string Name = ""; }

    private Dictionary<string, List<double>> numbers = new();
    private Dictionary<string, Prof> profs = new();
    private List<(int From, string Word)> ranks = new();
    private Dictionary<string, int> difficulties = new();
    private Dictionary<string, StationDef> stations = new();
    private Dictionary<string, GatherDef> gathers = new();
    private Dictionary<string, BeastDef> beasts = new();
    private Dictionary<string, Mat> mats = new();
    private Dictionary<string, Mat> matByIdentity = new(); // "path" or "path#kind"
    private Dictionary<string, List<Mat>> byCategory = new();
    private Dictionary<string, NodeDef> nodeKinds = new();
    private Dictionary<string, Recipe> recipes = new();
    private Dictionary<string, List<Recipe>> index = new();     // slot key ("c:berry+m:clean_water") to recipes
    private Dictionary<string, Recipe> concrete = new();         // sorted material ids to the recipe that wins them
    private List<string> problems = new();
    private List<string> clashes = new();

    private readonly Dictionary<string, ObjectRef> files = new();
    private readonly List<string> logQueue = new();
    private readonly Dictionary<string, Job> jobs = new();
    private readonly Dictionary<string, HealOverTime> heals = new();
    private bool loaded;
    private long lastCheck;
    private long loadMs;
    private int loads;
    private string rollMode = "random";
    private int healGeneration;

    // ------------------------------------------------------------------
    // Boot, the background check and the driver log
    // ------------------------------------------------------------------

    public override void Create()
    {
        if (Self.IsClone) return;
        Name = "crafting daemon";
        Refresh(true);
        CallOut(Background, 5);
    }

    private void Background()
    {
        try { Refresh(false); }
        catch (Exception e) when (e is MudRuntimeException || e is InvalidOperationException) { Log("crafting: refresh failed: " + e.Message); }
        FlushLog();
        CallOut(Background, 5);
    }

    /// <summary>A line for the driver log. A message written while no player is the current one goes to the log; otherwise it waits for the background call.</summary>
    private void Log(string line)
    {
        logQueue.Add(line);
        while (logQueue.Count > 500) logQueue.RemoveAt(0);
        if (ThisPlayer() == null) FlushLog();
    }

    private void FlushLog()
    {
        if (ThisPlayer() != null) return;
        foreach (var line in logQueue) Write(line + "\n");
        logQueue.Clear();
    }

    private static bool InTrustedTree(string path)
    {
        if (path.Contains("..") || path.Contains("//")) return false;
        foreach (var tree in TrustedTrees) if (path.StartsWith(tree, StringComparison.Ordinal)) return true;
        return false;
    }

    private static bool Trusted(ObjectRef? ob) => ob != null && ob.Domain == "" && InTrustedTree(ob.ProgramPath); // never a sandbox object (docs/SANDBOX.md)

    private static bool IsPlayerBody(ObjectRef? ob) => ob != null && ob.IsAlive && ob.IsClone && ob.ProgramPath == PlayerProgram;

    // ------------------------------------------------------------------
    // Loading the data
    // ------------------------------------------------------------------

    private void ListFiles(string dir, int depth, List<string> into)
    {
        var entries = GetDir(dir);
        if (entries == null) return;
        foreach (var e in entries)
        {
            if (e.Name.StartsWith(".")) continue;
            var path = dir + "/" + e.Name;
            if (e.IsDirectory) { if (depth < MaxListDepth) ListFiles(path, depth + 1, into); continue; }
            if (!e.Name.EndsWith(".cs")) continue;
            var id = e.Name.Substring(0, e.Name.Length - 3);
            if (id.Length == 0 || !id.All(c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_')) continue;
            into.Add(path.Substring(0, path.Length - 3));
        }
    }

    /// <summary>
    /// Look for new, removed and replaced data files and rebuild everything if anything changed. Called at boot, every five
    /// seconds in the background, and before a command if nothing has loaded yet. Public so that a wizard's tools and tests can
    /// ask for it; it only ever reads the two trusted trees.
    /// </summary>
    public void Refresh(bool force)
    {
        if (Self.IsClone) return;
        lastCheck = TimeMs();
        var found = new List<string>();
        ListFiles(DataDir, 1, found);
        ListFiles(RecipeDir, 1, found);
        found.Sort(StringComparer.Ordinal);
        var changed = force || !loaded || found.Count != files.Count || found.Any(p => !files.ContainsKey(p))
                      || files.Any(kv => !kv.Value.IsAlive || FindObject(kv.Key) != kv.Value);
        if (!changed) return;
        Rebuild(found);
    }

    private void EnsureLoaded()
    {
        if (!loaded) Refresh(true);
    }

    private void Rebuild(List<string> found)
    {
        var started = TimeMs();
        files.Clear();
        problems = new List<string>();
        clashes = new List<string>();
        var dataObjects = new List<(string Path, ObjectRef Ob)>();
        var recipeObjects = new List<(string Path, ObjectRef Ob)>();
        foreach (var path in found)
        {
            ObjectRef? ob = FindObject(path);
            if (ob == null || !ob.IsAlive)
            {
                try { ob = LoadObject(path); }
                catch (MudRuntimeException e) { Problem($"{path}: does not load: {FirstLine(e.Message)}"); continue; }
            }
            files[path] = ob;
            if (path.StartsWith(DataDir + "/")) dataObjects.Add((path, ob)); else recipeObjects.Add((path, ob));
        }

        numbers = new Dictionary<string, List<double>>();
        profs = new Dictionary<string, Prof>();
        ranks = new List<(int, string)>();
        difficulties = new Dictionary<string, int>();
        stations = new Dictionary<string, StationDef>();
        gathers = new Dictionary<string, GatherDef>();
        beasts = new Dictionary<string, BeastDef>();
        mats = new Dictionary<string, Mat>();
        matByIdentity = new Dictionary<string, Mat>();
        byCategory = new Dictionary<string, List<Mat>>();
        nodeKinds = new Dictionary<string, NodeDef>();
        recipes = new Dictionary<string, Recipe>();
        index = new Dictionary<string, List<Recipe>>();
        concrete = new Dictionary<string, Recipe>();

        // Tables first, in path order; inside a table, in the order of its lines. Entry types are processed in dependency order.
        var all = new List<(string Source, Dictionary<string, string> E)>();
        foreach (var (path, ob) in dataObjects)
        {
            var entries = ob.Call<List<Dictionary<string, string>>>("QueryEntries");
            var nums = ob.Call<Dictionary<string, List<double>>>("QueryNumbers");
            if (entries == null || nums == null) { Problem($"{path}: is not a crafting table (inherit /std/craft_data)"); continue; }
            foreach (var kv in nums)
            {
                if (numbers.ContainsKey(kv.Key)) Problem($"{path}: number '{kv.Key}' is also set elsewhere; this value is used");
                numbers[kv.Key] = kv.Value;
            }
            foreach (var e in entries) all.Add((path, e));
        }
        foreach (var type in new[] { "proficiency", "rank", "difficulty", "station", "gather", "beast", "material", "nodekind", "yield" })
            foreach (var (source, e) in all.Where(x => x.E.GetValueOrDefault("type") == type)) LoadEntry(source, e);
        ranks.Sort((a, b) => a.From.CompareTo(b.From));

        foreach (var (path, ob) in recipeObjects)
        {
            var list = ob.Call<List<Dictionary<string, string>>>("QueryRecipes");
            if (list == null) { Problem($"{path}: is not a recipe file (inherit /std/recipe_set)"); continue; }
            foreach (var e in list) LoadRecipe(path, e);
        }

        loaded = true;
        loads++;
        loadMs = TimeMs() - started;
        Log($"crafting: loaded {recipes.Count} recipes, {mats.Count} materials, {profs.Count} proficiencies from {files.Count} files in {loadMs} ms" +
            (problems.Count > 0 ? $"; {problems.Count} problem(s)" : "") + (clashes.Count > 0 ? $"; {clashes.Count} clash(es)" : ""));
    }

    private static string FirstLine(string text) { var i = text.IndexOf('\n'); return i < 0 ? text : text.Substring(0, i); }

    private void Problem(string text)
    {
        problems.Add(text);
        Log("crafting: " + text);
    }

    private static int Int(Dictionary<string, string> e, string key, int fallback = 0) => int.TryParse(e.GetValueOrDefault(key) ?? "", out var v) ? v : fallback;

    private static bool ValidId(string id) => id.Length > 0 && id.Length <= 60 && id.All(c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_');

    private static List<string> SplitList(string text) =>
        (text ?? "").Split(',').Select(s => s.Trim().ToLowerInvariant()).Where(s => s != "").Distinct().ToList();

    private void AddMaterial(string source, string id, string record, List<string> categories, bool scarce)
    {
        if (!ValidId(id)) { Problem($"{source}: material id '{id}' is not valid (lower case letters, digits and _)"); return; }
        if (mats.ContainsKey(id)) { Problem($"{source}: material '{id}' is defined twice; the first is kept ({mats[id].Source})"); return; }
        var mark = record.IndexOf('?');
        var path = mark < 0 ? record : record.Substring(0, mark);
        var kind = "";
        if (mark >= 0)
            foreach (var part in record.Substring(mark + 1).Split('&'))
                if (part.StartsWith("p.kind=")) kind = part.Substring(7);
        if (!path.StartsWith("/") || !InTrustedTree(path + "/")) { Problem($"{source}: material '{id}': its file {path} is not in a trusted tree"); return; }
        var identity = kind == "" ? path : path + "#" + kind;
        if (matByIdentity.ContainsKey(identity)) { Problem($"{source}: material '{id}': {identity} is already material '{matByIdentity[identity].Id}'"); return; }
        var mat = new Mat { Id = id, Path = path, Kind = kind, Record = kind == "" ? path : path + "?p.kind=" + kind, Source = source, Categories = categories, Scarce = scarce };
        mats[id] = mat;
        matByIdentity[identity] = mat;
        foreach (var c in categories)
        {
            if (!byCategory.TryGetValue(c, out var members)) byCategory[c] = members = new List<Mat>();
            members.Add(mat);
        }
    }

    private void LoadEntry(string source, Dictionary<string, string> e)
    {
        var type = e.GetValueOrDefault("type") ?? "";
        switch (type)
        {
            case "proficiency":
            {
                var id = e.GetValueOrDefault("id") ?? "";
                if (!ValidId(id) || profs.ContainsKey(id)) { Problem($"{source}: proficiency '{id}' is not valid or is defined twice"); return; }
                profs[id] = new Prof { Id = id, Name = e.GetValueOrDefault("name") ?? id, Kind = e.GetValueOrDefault("kind") ?? "", Description = e.GetValueOrDefault("description") ?? "", Order = profs.Count };
                return;
            }
            case "rank": ranks.Add((Int(e, "from"), e.GetValueOrDefault("word") ?? "")); return;
            case "difficulty": difficulties[(e.GetValueOrDefault("word") ?? "").ToLowerInvariant()] = Int(e, "work"); return;
            case "station":
            {
                var kind = (e.GetValueOrDefault("kind") ?? "").ToLowerInvariant();
                stations[kind] = new StationDef { Kind = kind, Name = e.GetValueOrDefault("name") ?? kind, Proficiency = e.GetValueOrDefault("proficiency") ?? "" };
                return;
            }
            case "gather":
            {
                var verb = e.GetValueOrDefault("verb") ?? "";
                var prof = e.GetValueOrDefault("proficiency") ?? "";
                if (!profs.ContainsKey(prof)) { Problem($"{source}: gathering '{verb}' uses unknown proficiency '{prof}'"); return; }
                gathers[verb] = new GatherDef { Verb = verb, Proficiency = prof, Tool = (e.GetValueOrDefault("tool") ?? "").ToLowerInvariant() };
                return;
            }
            case "beast":
            {
                var kind = (e.GetValueOrDefault("kind") ?? "").ToLowerInvariant();
                if (!ValidId(kind) || beasts.ContainsKey(kind)) { Problem($"{source}: beast '{kind}' is not valid or is defined twice"); return; }
                var b = new BeastDef { Kind = kind, Name = e.GetValueOrDefault("name") ?? kind, Skin = Int(e, "skin"), Blood = Int(e, "blood"), Difficulty = Int(e, "difficulty"), Scarce = e.GetValueOrDefault("scarce") == "1" };
                beasts[kind] = b;
                AddMaterial(source, kind + "_skin", "/std/skin?p.kind=" + kind, new List<string> { "skin", kind }, false);
                AddMaterial(source, kind + "_blood", "/std/blood?p.kind=" + kind, new List<string> { "blood", kind }, b.Scarce);
                return;
            }
            case "material":
                AddMaterial(source, e.GetValueOrDefault("id") ?? "", e.GetValueOrDefault("path") ?? "", SplitList(e.GetValueOrDefault("categories") ?? ""), false);
                return;
            case "nodekind":
            {
                var kind = (e.GetValueOrDefault("kind") ?? "").ToLowerInvariant();
                var prof = e.GetValueOrDefault("proficiency") ?? "";
                if (!profs.ContainsKey(prof)) { Problem($"{source}: node kind '{kind}' uses unknown proficiency '{prof}'"); return; }
                var def = NodeFor(kind, true)!;
                def.Proficiency = prof;
                def.Tool = (e.GetValueOrDefault("tool") ?? "").ToLowerInvariant();
                return;
            }
            case "yield":
            {
                var kind = (e.GetValueOrDefault("kind") ?? "").ToLowerInvariant();
                var material = e.GetValueOrDefault("material") ?? "";
                if (!mats.TryGetValue(material, out var mat)) { Problem($"{source}: yield of '{kind}': unknown material '{material}'"); return; }
                if (mat.Scarce) { Problem($"{source}: yield of '{kind}': {material} can only come from bleeding; refused"); return; }
                NodeFor(kind, true)!.Yields.Add(new YieldDef { Material = material, Weight = Math.Max(1, Int(e, "weight", 1)), Count = Math.Clamp(Int(e, "count", 1), 1, 100) });
                return;
            }
            default:
                Problem($"{source}: unknown entry type '{type}'");
                return;
        }
    }

    private NodeDef? NodeFor(string kind, bool create)
    {
        if (nodeKinds.TryGetValue(kind, out var def)) return def;
        if (!create) return null;
        var harvest = gathers.GetValueOrDefault("harvest");
        def = new NodeDef { Kind = kind, Proficiency = harvest?.Proficiency ?? "harvesting", Tool = harvest?.Tool ?? "harvest sickle" };
        nodeKinds[kind] = def;
        return def;
    }

    private void LoadRecipe(string source, Dictionary<string, string> e)
    {
        var id = e.GetValueOrDefault("id") ?? "";
        var where = $"{source}: recipe '{id}'";
        if (!ValidId(id)) { Problem($"{where}: the id is not valid (lower case letters, digits and _)"); return; }
        if (recipes.TryGetValue(id, out var twin)) { Problem($"{where}: the id is also used in {twin.Source}; this one is not loaded"); return; }
        var r = new Recipe
        {
            Id = id, Name = (e.GetValueOrDefault("name") ?? "").Trim(), Station = (e.GetValueOrDefault("station") ?? "").Trim().ToLowerInvariant(),
            Proficiency = e.GetValueOrDefault("proficiency") ?? "", Difficulty = (e.GetValueOrDefault("difficulty") ?? "").ToLowerInvariant(),
            Output = (e.GetValueOrDefault("output") ?? "").Trim(), Count = Int(e, "count", 1), Discovery = (e.GetValueOrDefault("discovery") ?? "").ToLowerInvariant(),
            Description = e.GetValueOrDefault("description") ?? "", Source = source, Order = Int(e, "order"),
        };
        var fail = new List<string>();
        if (r.Name == "") fail.Add("it has no name");
        foreach (var part in (e.GetValueOrDefault("ingredients") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var star = part.LastIndexOf('*');
            var token = star < 0 ? part : part.Substring(0, star);
            var count = star < 0 ? 1 : int.TryParse(part.Substring(star + 1), out var c) ? c : 0;
            var slot = new Slot { Token = token.Trim().ToLowerInvariant(), Count = count };
            if (count < 1 || count > 99) fail.Add($"ingredient {token} has a count of {count} (1 to 99)");
            else if (slot.Token.StartsWith("m:") && !mats.ContainsKey(slot.Name)) fail.Add($"unknown material '{slot.Name}'");
            else if (slot.Token.StartsWith("c:") && !byCategory.ContainsKey(slot.Name)) fail.Add($"no material is in category '{slot.Name}'");
            else if (!slot.Token.StartsWith("m:") && !slot.Token.StartsWith("c:")) fail.Add($"bad ingredient '{token}'");
            else if (r.Slots.Any(s => s.Token == slot.Token)) fail.Add($"ingredient {slot.Name} is named twice (give it a count instead)");
            else r.Slots.Add(slot);
        }
        var max = (int)Num1("recipe.max_ingredients", 3);
        if (r.Slots.Count == 0) fail.Add("it has no ingredients");
        if (r.Slots.Count > max) fail.Add($"it has {r.Slots.Count} kinds of ingredient (at most {max})");
        if (r.Station != "" && !stations.ContainsKey(r.Station)) fail.Add($"unknown station '{r.Station}'");
        if (!profs.ContainsKey(r.Proficiency)) fail.Add($"unknown proficiency '{r.Proficiency}'");
        if (!difficulties.TryGetValue(r.Difficulty, out var work)) fail.Add($"unknown difficulty '{r.Difficulty}'");
        r.Work = work;
        if (!DiscoveryLevels.Contains(r.Discovery)) fail.Add($"unknown discovery level '{r.Discovery}' (known, discovered, rare, secret)");
        if (r.Count < 1 || r.Count > 100) fail.Add($"output count {r.Count} (1 to 100)");
        if (r.Output.StartsWith("/"))
        {
            if (!InTrustedTree(r.Output.Split('?')[0] + "/")) fail.Add($"output {r.Output} is not in a trusted tree");
            else if (OutputMaterial(r.Output) is { Scarce: true } scarceOut) fail.Add($"output {scarceOut.Id} can only come from bleeding");
        }
        else if (!mats.TryGetValue(r.Output, out var outMat)) fail.Add(r.Output == "" ? "it makes nothing (Makes)" : $"output '{r.Output}' is neither a path nor a known material");
        else if (outMat.Scarce) fail.Add($"output {outMat.Id} can only come from bleeding");
        if (fail.Count > 0) { Problem($"{where}: not loaded: {string.Join("; ", fail)}"); return; }

        r.Slots = r.Slots.OrderBy(s => s.Token, StringComparer.Ordinal).ToList();
        r.Key = string.Join("+", r.Slots.Select(s => s.Token));
        r.Specificity = r.Slots.Count(s => !s.IsCategory);

        // The clash rule. One set of materials makes one thing. Of the recipes that could take the same materials, the one with
        // more exact (material id) ingredients wins; two that are equally exact clash, and the one loaded later is refused.
        if (index.TryGetValue(r.Key, out var same) && same.Count > 0)
        {
            Clash(r, same[0], "they have the same ingredients");
            return;
        }
        var combos = Expand(r);
        if (combos == null)
            Problem($"{where}: its categories have too many members to check every combination for clashes; only the exact ingredients were checked");
        else
        {
            foreach (var combo in combos)
                if (concrete.TryGetValue(combo, out var other) && other.Specificity == r.Specificity)
                {
                    Clash(r, other, "both could be made from " + combo.Replace("+", " and "));
                    return;
                }
            foreach (var combo in combos)
                if (!concrete.TryGetValue(combo, out var other) || other.Specificity < r.Specificity) concrete[combo] = r;
        }
        recipes[r.Id] = r;
        if (!index.TryGetValue(r.Key, out var bucket)) index[r.Key] = bucket = new List<Recipe>();
        bucket.Add(r);
    }

    private void Clash(Recipe refused, Recipe kept, string why)
    {
        var line = $"CLASH: recipe '{refused.Id}' ({refused.Source}) clashes with '{kept.Id}' ({kept.Source}): {why}; '{refused.Id}' is not loaded";
        clashes.Add(line);
        Log("crafting: " + line);
    }

    /// <summary>Every set of distinct material ids that could fill the recipe's slots, as sorted keys; null if there would be too many.</summary>
    private List<string>? Expand(Recipe r)
    {
        var lists = r.Slots.Select(s => s.IsCategory ? byCategory.GetValueOrDefault(s.Name)?.Select(m => m.Id).ToList() ?? new List<string>() : new List<string> { s.Name }).ToList();
        long size = 1;
        foreach (var l in lists) size *= Math.Max(1, l.Count);
        if (size > MaxExpansion) return null;
        var result = new List<string>();
        var pick = new string[lists.Count];
        void Walk(int i)
        {
            if (i == lists.Count)
            {
                if (pick.Distinct().Count() == pick.Length) result.Add(string.Join("+", pick.OrderBy(p => p, StringComparer.Ordinal)));
                return;
            }
            foreach (var id in lists[i]) { pick[i] = id; Walk(i + 1); }
        }
        Walk(0);
        return result;
    }

    private Mat? OutputMaterial(string output)
    {
        if (!output.StartsWith("/")) return mats.GetValueOrDefault(output);
        var mark = output.IndexOf('?');
        var path = mark < 0 ? output : output.Substring(0, mark);
        var kind = "";
        if (mark >= 0) foreach (var part in output.Substring(mark + 1).Split('&')) if (part.StartsWith("p.kind=")) kind = part.Substring(7);
        return matByIdentity.GetValueOrDefault(kind == "" ? path : path + "#" + kind);
    }

    private string OutputRecord(Recipe r) => r.Output.StartsWith("/") ? r.Output : mats[r.Output].Record;

    // ------------------------------------------------------------------
    // Numbers
    // ------------------------------------------------------------------

    private static readonly Dictionary<string, double[]> Defaults = new()
    {
        ["harvest.base"] = new[] { 30.0 }, ["harvest.variation"] = new[] { 15.0 },
        ["harvest.tool_quality"] = new[] { -10.0, 0, 5, 10, 15, 20 }, ["harvest.difficulty_factor"] = new[] { 0.5 },
        ["harvest.thresholds"] = new[] { 20.0, 50, 80, 110 },
        ["harvest.success_base"] = new[] { 75.0 }, ["harvest.success_per_point"] = new[] { 0.2 },
        ["harvest.success_tool"] = new[] { -10.0, 0, 5, 10, 15, 20 }, ["harvest.success_difficulty"] = new[] { 0.4 },
        ["harvest.success_min"] = new[] { 30.0 }, ["harvest.success_max"] = new[] { 98.0 },
        ["harvest.bonus_per_point"] = new[] { 0.15 }, ["harvest.bonus_tool"] = new[] { 0.0, 0, 5, 10, 15, 20 }, ["harvest.bonus_max"] = new[] { 40.0 },
        ["harvest.max_quality"] = new[] { 4.0 },
        ["difficulty.beast_scale"] = new[] { 5.0 }, ["difficulty.level_weight"] = new[] { 1.0 }, ["difficulty.node_scale"] = new[] { 5.0 },
        ["bleed.fresh_seconds"] = new[] { 300.0 },
        ["growth.near_band"] = new[] { 10.0 }, ["growth.easy_band"] = new[] { 30.0 },
        ["growth.ladder"] = new[] { 20.0, 25, 50, 75, 100 }, ["growth.repeat_every"] = new[] { 20.0 }, ["growth.easy_factor"] = new[] { 0.5 },
        ["craft.shift_per_point"] = new[] { 0.02 }, ["craft.shift_min"] = new[] { -1.5 }, ["craft.shift_max"] = new[] { 1.0 },
        ["craft.noise"] = new[] { 0.75 }, ["craft.noise_steps"] = new[] { 20.0 }, ["craft.max_quality"] = new[] { 4.0 },
        ["craft.bonus_margin"] = new[] { 20.0 }, ["craft.bonus_per_point"] = new[] { 0.5 }, ["craft.bonus_max"] = new[] { 25.0 },
        ["craft.return_margin"] = new[] { 30.0 }, ["craft.return_per_point"] = new[] { 0.4 }, ["craft.return_max"] = new[] { 20.0 },
        ["time.gather"] = new[] { 1.0, 0.5, 0.0 }, ["time.craft"] = new[] { 1.5, 0.0, -1.0 },
        ["effect.quality_scale"] = new[] { 0.6, 1.0, 1.25, 1.5, 1.8, 2.2 },
        ["recipe.max_ingredients"] = new[] { 3.0 },
    };

    private double Num1(string key, double fallback) => NumAt(key, 0, fallback);

    private double NumAt(string key, int i, double fallback)
    {
        if (numbers.TryGetValue(key, out var list) && i < list.Count) return list[i];
        if (Defaults.TryGetValue(key, out var d) && i < d.Length) return d[i];
        return fallback;
    }

    /// <summary>The numbers in force, for tests and the simulator: key to values (the data file's, or the built-in default if the data lacks one).</summary>
    public Dictionary<string, List<double>> QueryNumbers()
    {
        EnsureLoaded();
        var result = Defaults.ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
        foreach (var kv in numbers) result[kv.Key] = new List<double>(kv.Value);
        return result;
    }

    /// <summary>What a crafted consumable of this quality multiplies its effects by.</summary>
    public double EffectScale(int quality) => NumAt("effect.quality_scale", Math.Clamp(quality, 0, 5), 1.0);

    // ------------------------------------------------------------------
    // The formulas. Pure functions: the exact odds (for tests and the simulator) and the rolls use the same code.
    // ------------------------------------------------------------------

    private double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;

    private int ToolIndex(int toolQuality) => Math.Clamp(toolQuality, 0, 5);

    /// <summary>
    /// Harvest Quality Score = proficiency + tool quality modifier - difficulty x factor + base + random variation + the gatherer's
    /// Gathering Efficiency (docs/ATTRIBUTES.md: +1 a point of Grace and of Insight above 5).
    /// </summary>
    private double HarvestScore(double prof, int toolQ, double work, int variation, double efficiency) =>
        Num1("harvest.base", 30) + prof + NumAt("harvest.tool_quality", ToolIndex(toolQ), 0) - work * Num1("harvest.difficulty_factor", 0.5) + variation + efficiency;

    /// <summary>A character's Gathering Efficiency, the one place the daemon asks for it.</summary>
    private static double Efficiency(ObjectRef who) => who.Call<double>("QueryStat", "gathering");

    /// <summary>A character's Crafting Aptitude (added to the crafted quality shift), the one place the daemon asks for it.</summary>
    private static double Aptitude(ObjectRef who) => who.Call<double>("QueryStat", "crafting");

    /// <summary>Dim light: -5 to a harvest quality score (docs/specs/light.md, section 5).</summary>
    private double LightHarvest(ObjectRef who) => Environment(who) is { } room ? LoadObject("/secure/light").Call<int>("QueryHarvestModifier", LoadObject("/secure/light").Call<int>("QueryLevel", room)) : 0;

    /// <summary>Dim light: -0.25 of a tier to a crafted quality.</summary>
    private double LightCraft(ObjectRef who) => Environment(who) is { } room ? LoadObject("/secure/light").Call<double>("QueryCraftShift", LoadObject("/secure/light").Call<int>("QueryLevel", room)) : 0;

    private int HarvestTier(double score)
    {
        var tier = 0;
        for (var i = 0; i < 4; i++) if (score >= NumAt("harvest.thresholds", i, 999)) tier = i + 1;
        return Math.Min(tier, (int)Num1("harvest.max_quality", 4));
    }

    private double SuccessChance(double prof, int toolQ, double work) =>
        Clamp(Num1("harvest.success_base", 75) + prof * Num1("harvest.success_per_point", 0.2) + NumAt("harvest.success_tool", ToolIndex(toolQ), 0) - work * Num1("harvest.success_difficulty", 0.4),
              Num1("harvest.success_min", 30), Num1("harvest.success_max", 98));

    private double BonusChance(double prof, int toolQ) =>
        Clamp(prof * Num1("harvest.bonus_per_point", 0.15) + NumAt("harvest.bonus_tool", ToolIndex(toolQ), 0), 0, Num1("harvest.bonus_max", 40));

    /// <summary>
    /// The exact odds of one harvest: [success %, bonus unit %, then % for Poor, Common, Fine, Excellent, Masterwork] for a
    /// proficiency (0 to 100), a tool quality (0 Poor to 5; -1 for none) and a work level (difficulty, 0 to 100). Pure.
    /// </summary>
    public List<double> HarvestOdds(double prof, int toolQ, double work) => HarvestOdds(prof, toolQ, work, 0);

    /// <summary>The exact odds of one harvest for a gatherer with this Gathering Efficiency.</summary>
    public List<double> HarvestOdds(double prof, int toolQ, double work, double efficiency)
    {
        EnsureLoaded();
        var v = (int)Num1("harvest.variation", 15);
        var tiers = new double[5];
        for (var a = 0; a <= v; a++)
            for (var b = 0; b <= v; b++)
                tiers[HarvestTier(HarvestScore(prof, toolQ, work, a + b - v, efficiency))] += 1;
        var total = (double)(v + 1) * (v + 1);
        var result = new List<double> { SuccessChance(prof, toolQ, work), BonusChance(prof, toolQ) };
        result.AddRange(tiers.Select(t => 100.0 * t / total));
        return result;
    }

    /// <summary>
    /// The quality shift: (proficiency - work) x 0.02, plus the crafter's Crafting Aptitude (docs/ATTRIBUTES.md: +0.02 a point of
    /// Insight and of Resolve above 5), within the shift's limits.
    /// </summary>
    private double CraftShift(double prof, double work, double aptitude) =>
        Clamp((prof - work) * Num1("craft.shift_per_point", 0.02) + aptitude, Num1("craft.shift_min", -1.5), Num1("craft.shift_max", 1.0));

    private int CraftTier(double average, double prof, double work, int a, int b, double aptitude)
    {
        var n = (int)Num1("craft.noise_steps", 20);
        var noise = (double)(a + b - n) / n * Num1("craft.noise", 0.75);
        var tier = (int)Math.Floor(average + CraftShift(prof, work, aptitude) + noise + 0.5);
        return Math.Clamp(tier, 0, (int)Num1("craft.max_quality", 4));
    }

    private double CraftBonusChance(double prof, double work) =>
        Clamp((prof - work - Num1("craft.bonus_margin", 20)) * Num1("craft.bonus_per_point", 0.5), 0, Num1("craft.bonus_max", 25));

    private double CraftReturnChance(double prof, double work) =>
        Clamp((prof - work - Num1("craft.return_margin", 30)) * Num1("craft.return_per_point", 0.4), 0, Num1("craft.return_max", 20));

    /// <summary>
    /// The exact odds of one craft: % for Poor .. Legendary, then bonus output %, then ingredient returned %, for an average
    /// ingredient quality (weighted by count), a proficiency and the recipe's work level. Pure.
    /// </summary>
    public List<double> CraftOdds(double average, double prof, double work) => CraftOdds(average, prof, work, 0);

    /// <summary>The exact odds of one craft for a crafter with this Crafting Aptitude.</summary>
    public List<double> CraftOdds(double average, double prof, double work, double aptitude)
    {
        EnsureLoaded();
        var n = (int)Num1("craft.noise_steps", 20);
        var tiers = new double[6];
        for (var a = 0; a <= n; a++)
            for (var b = 0; b <= n; b++)
                tiers[CraftTier(average, prof, work, a, b, aptitude)] += 1;
        var total = (double)(n + 1) * (n + 1);
        var result = tiers.Select(t => 100.0 * t / total).ToList();
        result.Add(CraftBonusChance(prof, work));
        result.Add(CraftReturnChance(prof, work));
        return result;
    }

    /// <summary>
    /// Fix the random rolls, for tests: "random" (the default), "mid" (no variation, every attempt succeeds, no bonus), "high"
    /// (the best variation, success, bonus), "low" (the worst variation, success, no bonus), "fail" (every gathering attempt
    /// fails). Only trusted code acting for a wizard of level 10 may change it.
    /// </summary>
    public bool SetTestRolls(string mode)
    {
        var who = ThisPlayer();
        if (!Trusted(PreviousObject()) || who == null || who.Call<int>("QueryWizLevel") < 10) return false;
        // "growth:NN.N" is "mid" for every roll but the growth roll, which is fixed at NN.N (0 to 100): for tests of the learning speed.
        var fixedGrowth = mode != null && mode.StartsWith("growth:") && ParseNumber(mode.Substring(7), out var g) && g >= 0 && g <= 100;
        if (!fixedGrowth && !new[] { "random", "mid", "high", "low", "fail" }.Contains(mode)) return false;
        rollMode = mode;
        return true;
    }

    public string QueryTestRolls() => rollMode;

    /// <summary>
    /// Set a character's proficiency (tenths, 0 to 1000), for tests and wizards' tools: only trusted code acting for a wizard of
    /// level 10. Code under /wiz can never use it. True if set.
    /// </summary>
    public bool WizardSetProficiency(ObjectRef who, string id, int tenths)
    {
        var actor = ThisPlayer();
        if (Self.IsClone || !Trusted(PreviousObject()) || actor == null || actor.Call<int>("QueryWizLevel") < 10 || !IsPlayerBody(who)) return false;
        EnsureLoaded();
        if (!profs.ContainsKey(id ?? "")) return false;
        var ok = who.Call<bool>("CraftSetProficiency", id, Math.Clamp(tenths, 0, 1000));
        if (ok) who.Call("CraftSave");
        return ok;
    }

    /// <summary>
    /// Roll n harvests with the real random rolls and return how many came out at each quality (Poor .. Masterwork), then the
    /// successes and the bonus units. No side effects: it only exercises the same code as a real harvest, for statistical tests.
    /// </summary>
    public List<int> RollHarvestSample(double prof, int toolQ, double work, int n) => RollHarvestSample(prof, toolQ, work, n, 0);

    /// <summary>RollHarvestSample for a gatherer with this Gathering Efficiency.</summary>
    public List<int> RollHarvestSample(double prof, int toolQ, double work, int n, double efficiency)
    {
        EnsureLoaded();
        var counts = new int[7];
        var v = (int)Num1("harvest.variation", 15);
        for (var i = 0; i < Math.Clamp(n, 0, 20000); i++)
        {
            var (a, b) = RollPair(v);
            counts[HarvestTier(HarvestScore(prof, toolQ, work, a + b - v, efficiency))]++;
            if (RollPercent(SuccessChance(prof, toolQ, work), true)) counts[5]++;
            if (RollPercent(BonusChance(prof, toolQ), false)) counts[6]++;
        }
        return counts.ToList();
    }

    /// <summary>Roll n crafts with the real random rolls: counts per quality (Poor .. Legendary), then bonus outputs, then returns. No side effects.</summary>
    public List<int> RollCraftSample(double average, double prof, double work, int n) => RollCraftSample(average, prof, work, n, 0);

    /// <summary>RollCraftSample for a crafter with this Crafting Aptitude.</summary>
    public List<int> RollCraftSample(double average, double prof, double work, int n, double aptitude)
    {
        EnsureLoaded();
        var counts = new int[8];
        var steps = (int)Num1("craft.noise_steps", 20);
        for (var i = 0; i < Math.Clamp(n, 0, 20000); i++)
        {
            var (a, b) = RollPair(steps);
            counts[CraftTier(average, prof, work, a, b, aptitude)]++;
            if (RollPercent(CraftBonusChance(prof, work), false)) counts[6]++;
            if (RollPercent(CraftReturnChance(prof, work), false)) counts[7]++;
        }
        return counts.ToList();
    }

    private bool RollPercent(double chance, bool forSuccess) => rollMode switch
    {
        "mid" or "low" => forSuccess,
        _ when rollMode.StartsWith("growth:") => forSuccess,
        "high" => true,
        "fail" => false,
        _ => Random(10000) < (int)Math.Round(chance * 100),
    };

    private (int A, int B) RollPair(int range) => rollMode switch
    {
        "mid" => (range / 2, range - range / 2),
        _ when rollMode.StartsWith("growth:") => (range / 2, range - range / 2),
        "high" => (range, range),
        "low" or "fail" => (0, 0),
        _ => (Random(range + 1), Random(range + 1)),
    };

    /// <summary>
    /// The ladder of chances (percent) for a skill in tenths: each rung of growth.ladder repeated (1 + skill / growth.repeat_every)
    /// times, whole points. A character's position is an index into this list.
    /// </summary>
    private List<double> Ladder(int tenths)
    {
        var rungs = numbers.TryGetValue("growth.ladder", out var l) && l.Count > 0 ? l : Defaults["growth.ladder"].ToList();
        var every = Math.Max(1.0, Num1("growth.repeat_every", 20));
        var repeat = 1 + (int)Math.Floor(Math.Max(0, tenths / 10.0) / every);
        var list = new List<double>();
        foreach (var r in rungs) for (var i = 0; i < repeat; i++) list.Add(r);
        return list;
    }

    /// <summary>
    /// One successful piece of work at a work level, by a character with this skill (tenths) and ladder position. The roll is
    /// 0 up to (not including) 100; a point is gained if it is below the chance. Work more than growth.easy_band below the skill
    /// teaches nothing and leaves the position; easy work rolls at growth.easy_factor of the rung's chance; either way a miss
    /// moves to the next rung (the last stays). A gain sends the position back to the first rung. speed multiplies the chance
    /// (1.0 now; a later bonus to learning), which is capped at 100.
    /// </summary>
    private (bool Gained, int Next) GrowthStep(int tenths, double work, int position, double roll, double speed)
    {
        if (tenths >= 1000) return (false, position);
        var gap = tenths / 10.0 - work;
        if (gap > Num1("growth.easy_band", 30)) return (false, position);
        var rungs = Ladder(tenths);
        var at = Math.Clamp(position, 0, rungs.Count - 1);
        var chance = rungs[at];
        if (gap > Num1("growth.near_band", 10)) chance *= Num1("growth.easy_factor", 0.5);
        chance = Math.Min(100, chance * speed);
        return roll < chance ? (true, 0) : (false, Math.Min(at + 1, rungs.Count - 1));
    }

    /// <summary>Parse "-1" or "0.5" with a dot on every machine (mudlib code may not use cultures).</summary>
    private static bool ParseNumber(string text, out double value)
    {
        value = 0;
        text = text.Trim();
        var negative = text.StartsWith("-");
        if (negative) text = text.Substring(1);
        var parts = text.Split('.');
        if (parts.Length > 2 || parts[0].Length == 0 || !parts[0].All(char.IsDigit) || (parts.Length == 2 && (parts[1].Length == 0 || !parts[1].All(char.IsDigit)))) return false;
        var fraction = parts.Length == 2 ? double.Parse(parts[1]) / Math.Pow(10, parts[1].Length) : 0;
        value = (double.Parse(parts[0]) + fraction) * (negative ? -1 : 1);
        return true;
    }

    /// <summary>The growth roll, 0 to 100. Test rolls: mid and high always gain (roll 0), low never does, random is random.</summary>
    private double RollGrowth() => rollMode.StartsWith("growth:") && ParseNumber(rollMode.Substring(7), out var fixedRoll) ? fixedRoll : rollMode switch
    {
        "mid" or "high" => 0.0,
        "low" or "fail" => 100.0,
        _ => Random(10000) / 100.0,
    };

    /// <summary>
    /// What multiplies the chance of a point for this character: 1.0, plus the "learn_speed" percent of the character's class traits
    /// (the Adventurer's Jack of All Trades: +25%). Only traits that come from class files count: not timed effects (any object may add
    /// one) and not items.
    /// </summary>
    private double LearnSpeed(ObjectRef who) => 1.0 + Math.Max(0, who.Call<double>("QueryClassTraitMod", "learn_speed")) / 100.0;

    /// <summary>The learning multiplier of a character, for tests and tools (read-only).</summary>
    public double LearnSpeedOf(ObjectRef who) => who == null || !who.IsAlive ? 1.0 : LearnSpeed(who);

    /// <summary>A growth step for tests, with a learning multiplier. Pure. As GrowthFor.</summary>
    public List<int> GrowthForSpeed(int tenths, double work, int position, double roll, double speed)
    {
        EnsureLoaded();
        var (gained, next) = GrowthStep(tenths, work, position, roll, Math.Max(0, speed));
        return new List<int> { gained ? 1 : 0, next };
    }

    /// <summary>The chance (percent) of the rung a ladder position stands on, for a skill in tenths, for tests. Pure.</summary>
    public double RungChance(int tenths, int position)
    {
        EnsureLoaded();
        var rungs = Ladder(tenths);
        return rungs[Math.Clamp(position, 0, rungs.Count - 1)];
    }

    /// <summary>
    /// A growth step for tests. Pure. Returns [1 if a point is gained else 0, the next ladder position] for a skill in tenths,
    /// a work level, a ladder position and a roll (0 to 100).
    /// </summary>
    public List<int> GrowthFor(int tenths, double work, int position, double roll)
    {
        EnsureLoaded();
        var (gained, next) = GrowthStep(tenths, work, position, roll, 1.0);
        return new List<int> { gained ? 1 : 0, next };
    }

    /// <summary>The number of rungs a skill (tenths) climbs through, for tests.</summary>
    public List<double> LadderFor(int tenths) { EnsureLoaded(); return Ladder(tenths); }

    /// <summary>A character's ladder position for a proficiency, for tests: trusted code acting for a wizard of level 10 only.</summary>
    public int WizardQueryLadder(ObjectRef who, string id)
    {
        var actor = ThisPlayer();
        if (Self.IsClone || !Trusted(PreviousObject()) || actor == null || actor.Call<int>("QueryWizLevel") < 10 || !IsPlayerBody(who)) return -1;
        return who.Call<int>("QueryCraftLadder", id);
    }

    // ------------------------------------------------------------------
    // Public queries (read-only, for items, tests and builders)
    // ------------------------------------------------------------------

    /// <summary>The material id the table gives this object ("wolf_skin"), or "".</summary>
    public string MaterialIdOf(ObjectRef ob) { EnsureLoaded(); return MatOf(ob)?.Id ?? ""; }

    private Mat? MatOf(ObjectRef ob)
    {
        if (!ob.IsAlive) return null;
        var path = ob.ProgramPath;
        if (matByIdentity.TryGetValue(path, out var plain)) return plain;
        var kind = ob.Call<string>("QueryProperty", "kind") ?? "";
        return kind == "" ? null : matByIdentity.GetValueOrDefault(path + "#" + kind);
    }

    /// <summary>A beast's line of the table: display name, skin value, blood value, difficulty, scarce (1 or 0); null if unknown.</summary>
    public List<string>? QueryBeast(string kind)
    {
        EnsureLoaded();
        return beasts.TryGetValue(kind ?? "", out var b) ? new List<string> { b.Name, b.Skin.ToString(), b.Blood.ToString(), b.Difficulty.ToString(), b.Scarce ? "1" : "0" } : null;
    }

    /// <summary>Is this beast's blood scarce (it can only come from bleeding)? Unknown kinds are not.</summary>
    public bool QueryScarceBlood(string kind) { EnsureLoaded(); return beasts.TryGetValue(kind ?? "", out var b) && b.Scarce; }

    public List<string> QueryRecipeIds() { EnsureLoaded(); return recipes.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList(); }

    public List<string> QueryMaterialIds() { EnsureLoaded(); return mats.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList(); }

    public List<string> QueryProficiencyIds() { EnsureLoaded(); return profs.Values.OrderBy(p => p.Order).Select(p => p.Id).ToList(); }

    public List<string> QueryProblems() { EnsureLoaded(); return new List<string>(problems); }

    public List<string> QueryClashes() { EnsureLoaded(); return new List<string>(clashes); }

    /// <summary>"recipes=N materials=N files=N load_ms=N loads=N" for tests and wizards.</summary>
    public string QueryStatus() { EnsureLoaded(); return $"recipes={recipes.Count} materials={mats.Count} files={files.Count} load_ms={loadMs} loads={loads} rolls={rollMode}"; }

    /// <summary>A recipe's data, or null: id, name, ingredients, station, proficiency, difficulty, work, output, count, discovery, source.</summary>
    public Dictionary<string, string>? QueryRecipeInfo(string id)
    {
        EnsureLoaded();
        if (!recipes.TryGetValue(id ?? "", out var r)) return null;
        return new Dictionary<string, string>
        {
            ["id"] = r.Id, ["name"] = r.Name, ["ingredients"] = string.Join(",", r.Slots.Select(s => s.Token + "*" + s.Count)), ["station"] = r.Station,
            ["proficiency"] = r.Proficiency, ["difficulty"] = r.Difficulty, ["work"] = r.Work.ToString(), ["output"] = r.Output, ["record"] = OutputRecord(r),
            ["count"] = r.Count.ToString(), ["discovery"] = r.Discovery, ["source"] = r.Source,
        };
    }

    /// <summary>A material's data, or null: id, path, record (path with its kind), categories, scarce.</summary>
    public Dictionary<string, string>? QueryMaterialInfo(string id)
    {
        EnsureLoaded();
        if (!mats.TryGetValue(id ?? "", out var m)) return null;
        return new Dictionary<string, string> { ["id"] = m.Id, ["path"] = m.Path, ["record"] = m.Record, ["categories"] = string.Join(",", m.Categories), ["scarce"] = m.Scarce ? "1" : "0" };
    }

    /// <summary>The matcher on material ids, with no side effects: the id of the recipe these materials make, or "".</summary>
    public string QueryMatch(List<string> materialIds)
    {
        EnsureLoaded();
        var offered = new List<Mat>();
        foreach (var id in materialIds ?? new List<string>())
        {
            if (!mats.TryGetValue(id, out var m)) return "";
            offered.Add(m);
        }
        return Match(offered)?.Id ?? "";
    }

    // ------------------------------------------------------------------
    // The matcher
    // ------------------------------------------------------------------

    /// <summary>
    /// The recipe an offering of distinct materials makes, or null. Each material can stand for its own id or any of its
    /// categories; every combination is looked up in the index by its sorted key, so the cost depends on the offering (at most
    /// a few dozen lookups for three materials), not on the number of recipes. Of several matches the most exact wins
    /// (clashes between equally exact recipes are refused when they load).
    /// </summary>
    private Recipe? Match(List<Mat> offered)
    {
        if (offered.Count == 0 || offered.Select(m => m.Id).Distinct().Count() != offered.Count) return null;
        var options = offered.Select(m => new List<string> { "m:" + m.Id }.Concat(m.Categories.Select(c => "c:" + c)).ToList()).ToList();
        Recipe? best = null;
        var pick = new string[options.Count];
        void Walk(int i)
        {
            if (i == options.Count)
            {
                var key = string.Join("+", pick.OrderBy(p => p, StringComparer.Ordinal));
                if (!index.TryGetValue(key, out var found)) return;
                foreach (var r in found)
                    if (best == null || r.Specificity > best.Specificity || (r.Specificity == best.Specificity && string.CompareOrdinal(r.Source + "#" + r.Order.ToString("D5"), best.Source + "#" + best.Order.ToString("D5")) < 0))
                        best = r;
                return;
            }
            foreach (var t in options[i])
            {
                if (pick.Take(i).Contains(t)) continue;
                pick[i] = t;
                Walk(i + 1);
            }
        }
        Walk(0);
        return best;
    }

    // ------------------------------------------------------------------
    // Characters: proficiencies and known recipes, through the guarded setters on the player body
    // ------------------------------------------------------------------

    private int Tenths(ObjectRef who, string prof) => who.Call<int>("QueryProficiencyTenths", prof);

    private Dictionary<string, string> Known(ObjectRef who) => who.Call<Dictionary<string, string>>("QueryKnownRecipes") ?? new Dictionary<string, string>();

    private string Word(int points)
    {
        var word = "untrained";
        foreach (var (from, w) in ranks) if (points >= from) word = w;
        return word;
    }

    /// <summary>Raise a proficiency for one piece of successful work. Returns the line that tells the character the whole number went up, or "".</summary>
    private string Grow(ObjectRef who, string prof, double work)
    {
        if (!profs.TryGetValue(prof, out var p)) return "";
        var before = Tenths(who, prof);
        var position = who.Call<int>("QueryCraftLadder", prof);
        var (gained, next) = GrowthStep(before, work, position, RollGrowth(), LearnSpeed(who));
        if (next != position && !who.Call<bool>("CraftSetLadder", prof, next)) return "";
        if (!gained) return "";
        var after = Math.Min(1000, before + 10);
        if (who.Call<bool>("CraftSetProficiency", prof, after) != true) return "";
        return after / 10 > before / 10 ? $"Your {p.Name} rises to {after / 10} ({Word(after / 10)}).\n" : "";
    }

    private bool Learn(ObjectRef who, Recipe r, string how) => who.Call<bool>("CraftLearn", r.Id, how);

    /// <summary>
    /// Teach a recipe: the call that trainers, books and quest rewards make. The caller must be code from a trusted tree (never
    /// /wiz); the character must be a player body. A Secret recipe cannot be taught: it is found by experiment only. Returns
    /// "ok" (learned now), "known" (already known), or "refused: why". The character is told and saved.
    /// </summary>
    public string Teach(ObjectRef who, string recipeId)
    {
        if (Self.IsClone) return "refused: not the daemon";
        var caller = PreviousObject();
        if (!Trusted(caller)) return "refused: the caller is not in a trusted tree";
        if (!IsPlayerBody(who)) return "refused: not a player";
        EnsureLoaded();
        if (!recipes.TryGetValue(recipeId ?? "", out var r)) return "refused: no such recipe";
        if (r.Discovery == "secret") return "refused: a secret recipe is found only by experiment";
        if (Known(who).ContainsKey(r.Id)) { TellObject(who, $"You already know how to make {r.Name}.\n"); return "known"; }
        if (!Learn(who, r, "taught")) return "refused: the character would not learn it";
        TellObject(who, $"You learn how to make {{cmd}}{r.Name}{{/}}: {Needs(r)}{(r.Station == "" ? "" : " at " + StationArticle(r.Station))}. ({{cmd}}recipes{{/}} lists what you know.)\n");
        who.Call("CraftSave");
        return "ok";
    }

    // ------------------------------------------------------------------
    // Describing things
    // ------------------------------------------------------------------

    private string StationName(string kind) => stations.TryGetValue(kind, out var s) ? s.Name : Cap(kind);

    private string StationArticle(string kind) { var n = StationName(kind).ToLowerInvariant(); return ("aeiou".IndexOf(n[0]) >= 0 ? "an " : "a ") + n; }

    private string MatName(string id, int n)
    {
        if (!mats.TryGetValue(id, out var m)) return id.Replace('_', ' ');
        var sample = Sample(m);
        if (sample == null) return id.Replace('_', ' ');
        var text = n == 1 ? sample.Call<string>("DescribeCount", 1) : sample.Call<string>("DescribeCount", n);
        return text ?? id.Replace('_', ' ');
    }

    private readonly Dictionary<string, ObjectRef> samples = new();

    /// <summary>A sample of a material, kept by the daemon (in no container) to name it in messages.</summary>
    private ObjectRef? Sample(Mat m)
    {
        if (samples.TryGetValue(m.Id, out var s) && s.IsAlive && s.ProgramPath == m.Path) return s;
        try
        {
            s = CloneStated(m.Record);
            if (s == null) return null;
            samples[m.Id] = s;
            return s;
        }
        catch (MudRuntimeException) { return null; }
    }

    /// <summary>How n of an item record at a quality are described: "a fine wolf skin", "3 excellent healing herbs".</summary>
    private string Named(string record, int n, int quality, string fallback)
    {
        try
        {
            var sample = CloneStated(record);
            if (sample == null) return fallback;
            if (quality != sample.Call<int>("QueryQuality")) sample.Call("SetQuality", quality);
            var text = sample.Call<string>("DescribeCount", n) ?? fallback;
            sample.Call("Vanish");
            return text;
        }
        catch (MudRuntimeException) { return fallback; }
    }

    private string SlotText(Slot s) => s.IsCategory ? $"{s.Count} {(s.Count == 1 ? "" : "of ")}any {s.Name}" : $"{(s.Count == 1 ? MatName(s.Name, 1) : MatName(s.Name, s.Count))}";

    private string Needs(Recipe r) => Join(r.Slots.Select(SlotText).ToList());

    private static string Join(List<string> parts) => parts.Count switch
    {
        0 => "nothing",
        1 => parts[0],
        2 => parts[0] + " and " + parts[1],
        _ => string.Join(", ", parts.Take(parts.Count - 1)) + " and " + parts[parts.Count - 1]
    };

    private string OutputName(Recipe r, int n, int quality) => Named(OutputRecord(r), n, quality, r.Name);

    private string ProfName(string id) => profs.TryGetValue(id, out var p) ? p.Name : Cap(id);

    // ------------------------------------------------------------------
    // Commands: the entry point for the player body
    // ------------------------------------------------------------------

    /// <summary>
    /// Run a crafting or gathering command for a player body. Only the body itself may ask, during its own command. Returns
    /// [text] for an answer, or [text, "act", name, wind-up, preparation, recovery, reaction modifier, waiting words] when the
    /// command is an action on the combat clock: the body begins (or queues) it, and its timed part comes back through Resolve.
    /// </summary>
    public List<object> Command(ObjectRef who, string verb, string args)
    {
        if (Self.IsClone || PreviousObject() != who || ThisPlayer() != who || !IsPlayerBody(who)) return Say1("");
        EnsureLoaded();
        args = (args ?? "").Trim();
        // Darkness (docs/specs/light.md, section 5): no gathering and no crafting where you cannot see what you are doing.
        if (verb is "skin" or "bleed" or "harvest" or "craft" or "combine" && Environment(who) is { } here && LoadObject("/secure/light").Call<int>("QueryLevel", here) < 0)
            return Say1("It is too dark to see what you are doing.");
        switch (verb)
        {
            case "proficiencies": return Say1(Proficiencies(who));
            case "recipes": return Say1(RecipesText(who, args));
            case "recipe": return Say1(RecipeText(who, args));
            case "skin":
            case "bleed": return BeginCorpse(who, verb, args);
            case "harvest": return BeginHarvest(who, args);
            case "craft": return BeginCraft(who, args);
            case "combine": return BeginCombine(who, args);
            default: return Say1("");
        }
    }

    private static List<object> Say1(string text) => new() { text };

    private List<object> Act(string text, string name, string windUp, string kind, string waiting)
    {
        var key = kind == "craft" ? "time.craft" : "time.gather";
        return new List<object> { text, "act", name, windUp, NumAt(key, 0, 1.0), NumAt(key, 1, 0.5), NumAt(key, 2, 0.0), waiting };
    }

    /// <summary>
    /// The timed part of a crafting or gathering action, called by the player body's combat clock when the action resolves
    /// (the action's "skill" is this daemon). Only the body may call it, and only for the job its own command registered.
    /// </summary>
    public void Resolve(ObjectRef who, ObjectRef? target)
    {
        if (Self.IsClone || PreviousObject() != who || !IsPlayerBody(who)) return;
        if (!jobs.TryGetValue(who.Name, out var job)) return;
        EnsureLoaded();
        if (who.Call<bool>("QueryInCombat"))
        {
            jobs.Remove(who.Name);
            TellObject(who, "You cannot do that while you are fighting.\n");
            return;
        }
        switch (job.Verb)
        {
            case "skin":
            case "bleed": jobs.Remove(who.Name); FinishCorpse(who, job); break;
            case "harvest": jobs.Remove(who.Name); FinishHarvest(who, job); break;
            case "craft":
            case "combine": FinishCraft(who, job); break;
            default: jobs.Remove(who.Name); break;
        }
    }

    // ---- Tools ----

    /// <summary>The best tool of a type the character carries (by quality); only tools from trusted trees count.</summary>
    private ObjectRef? BestTool(ObjectRef who, string type)
    {
        ObjectRef? best = null;
        var bestQ = -1;
        foreach (var ob in AllInventory(who))
        {
            if (!ob.IsAlive || !Trusted(ob) || !ob.Call<bool>("IsTool")) continue;
            if (!string.Equals(ob.Call<string>("QueryToolType") ?? "", type, StringComparison.OrdinalIgnoreCase)) continue;
            var q = ob.Call<int>("QueryQuality");
            if (q > bestQ) { best = ob; bestQ = q; }
        }
        return best;
    }

    private static string WithArticle(string type) => ("aeiou".IndexOf(type.Length > 0 ? type[0] : 'x') >= 0 ? "an " : "a ") + type;

    private string NoTool(string type, string doing) => $"You need {WithArticle(type)} to {doing}. (Tools are carried, not wielded; the best one you carry is used.)";

    // ---- Corpses ----

    private List<ObjectRef> CorpsesHere(ObjectRef who)
    {
        var room = Environment(who);
        return room == null ? new List<ObjectRef>() : AllInventory(room).Where(o => o.IsAlive && o.Call<bool>("IsCorpse")).ToList();
    }

    private ObjectRef? FindCorpse(ObjectRef who, string verb, string args)
    {
        var corpses = CorpsesHere(who);
        if (corpses.Count == 0) return null;
        if (args == "" || args == "corpse") return corpses.FirstOrDefault(c => c.Call<bool>("QueryYieldAvailable", verb)) ?? corpses[0];
        var named = CorpsesNamed(corpses, args.ToLowerInvariant());
        if (named.Count == 0)
        {
            // A position among identical corpses: "skin corpse 2", "skin 2.wolf", "skin second rat".
            var position = SplitOrdinal(args.ToLowerInvariant(), out var bare);
            if (position == 0) return null;
            var same = bare == "corpse" ? corpses : CorpsesNamed(corpses, bare);
            return position <= same.Count ? same[position - 1] : null;
        }
        // Of several corpses that answer, the first that still has this part to give, else the first.
        return named.FirstOrDefault(c => c.Call<bool>("QueryYieldAvailable", verb)) ?? named.FirstOrDefault();
    }

    private static List<ObjectRef> CorpsesNamed(List<ObjectRef> corpses, string name)
    {
        var id = name;
        if (id.StartsWith("corpse of ")) id = id.Substring(10);
        return corpses.Where(c => c.Call<bool>("Id", name) || c.Call<bool>("Id", "corpse of " + id) || (c.Call<string>("QueryCreatureKind") ?? "") == id
                                  || (c.Call<string>("QueryName") ?? "").ToLowerInvariant().Contains(id)).ToList();
    }

    /// <summary>Why this character cannot take this part from this corpse now, or null.</summary>
    private string? CorpseRefusal(ObjectRef who, ObjectRef corpse, string verb)
    {
        var label = corpse.Call<string>("Short") ?? "the corpse";
        var kind = corpse.Call<string>("QueryYieldKind", verb) ?? "";
        if (kind == "" || !beasts.ContainsKey(kind))
            return verb == "skin" ? $"There is no hide worth taking on {label}." : $"There is no blood worth drawing from {label}.";
        if (!corpse.Call<bool>("QueryYieldAvailable", verb))
            return verb == "skin" ? $"{Cap(label)} has already been skinned." : $"{Cap(label)} has already been bled.";
        if (verb == "bleed" && corpse.Call<long>("QueryAgeMs") > (long)(Num1("bleed.fresh_seconds", 300) * 1000))
            return $"The blood of {label} has already gone cold. Bleeding needs a fresh kill, within {(int)Num1("bleed.fresh_seconds", 300) / 60} minutes of its death.";
        if (!corpse.Call<bool>("MayHarvest", who)) return $"{Cap(label)} is not yours to {verb} yet.";
        return null;
    }

    private List<object> BeginCorpse(ObjectRef who, string verb, string args)
    {
        if (who.Call<bool>("QueryInCombat")) return Say1($"You cannot {verb} anything while you are fighting.");
        var corpse = FindCorpse(who, verb, args);
        if (corpse == null)
        {
            var room = Environment(who);
            var living = args == "" || room == null ? null : Present(args.ToLowerInvariant(), room);
            if (living != null && living.Call<bool>("IsCreature")) return Say1($"You can only {verb} the dead.");
            return Say1(args == "" ? $"There is no corpse here to {verb}." : $"There is no corpse of '{Esc(args)}' here.");
        }
        var refusal = CorpseRefusal(who, corpse, verb);
        if (refusal != null) return Say1(refusal);
        if (!gathers.TryGetValue(verb, out var g)) return Say1("Nobody knows how to do that yet.");
        var tool = BestTool(who, g.Tool);
        if (tool == null) return Say1(NoTool(g.Tool, verb == "skin" ? "skin a corpse" : "bleed a corpse"));
        jobs[who.Name] = new Job { Verb = verb, Target = corpse };
        var label = corpse.Call<string>("Short") ?? "the corpse";
        return Act("", verb == "skin" ? "Skin" : "Bleed", verb == "skin" ? $"kneels beside {label} and begins to skin it" : $"kneels beside {label} and opens a vein", "gather", verb == "skin" ? "skin the corpse" : "bleed the corpse");
    }

    private void FinishCorpse(ObjectRef who, Job job)
    {
        var corpse = job.Target;
        var room = Environment(who);
        if (corpse == null || !corpse.IsAlive || room == null || Environment(corpse) != room) { TellObject(who, "The corpse is gone.\n"); return; }
        var refusal = CorpseRefusal(who, corpse, job.Verb);
        if (refusal != null) { TellObject(who, refusal + "\n"); return; }
        var g = gathers[job.Verb];
        var tool = BestTool(who, g.Tool);
        if (tool == null) { TellObject(who, NoTool(g.Tool, job.Verb + " a corpse") + "\n"); return; }
        var kind = corpse.Call<string>("QueryYieldKind", job.Verb) ?? "";
        var beast = beasts[kind];
        var work = beast.Difficulty * Num1("difficulty.beast_scale", 5) + corpse.Call<int>("QueryCreatureLevel") * Num1("difficulty.level_weight", 1);
        var mat = mats[kind + "_" + job.Verb.Replace("bleed", "blood")];
        var label = corpse.Call<string>("Short") ?? "the corpse";
        // The one chance is used whether it works or not: a botched cut ruins the hide, spilt blood cannot be gathered again.
        if (corpse.Call<bool>("TakeYield", job.Verb) != true) { TellObject(who, $"{Cap(label)} has nothing more to give.\n"); return; }
        Gathered(who, g.Proficiency, tool, work, mat, 1,
            job.Verb == "skin" ? $"Your knife slips and the hide of {label} is ruined." : $"The blood of {label} spills before you can catch it.",
            job.Verb == "skin" ? $"You skin {label}" : $"You bleed {label}",
            job.Verb == "skin" ? $"{who.Call<string>("QueryName")} skins {label}." : $"{who.Call<string>("QueryName")} draws blood from {label}.");
    }

    // ---- Nodes ----

    private List<object> BeginHarvest(ObjectRef who, string args)
    {
        if (who.Call<bool>("QueryInCombat")) return Say1("You cannot harvest anything while you are fighting.");
        var room = Environment(who);
        if (room == null) return Say1("There is nothing here to harvest.");
        ObjectRef? node;
        if (args == "") node = AllInventory(room).FirstOrDefault(o => o.IsAlive && o.Call<bool>("IsNode") && o.Call<string>("Short") != null); // a node the character has not found is not in the list
        else
        {
            node = Present(args.ToLowerInvariant(), room);
            if (node != null && node.Call<bool>("IsCorpse")) return Say1("Corpses are not harvested: use {cmd}skin{/} or {cmd}bleed{/}.");
            if (node != null && !node.Call<bool>("IsNode")) return Say1($"You cannot harvest {node.Call<string>("Short") ?? "that"}.");
        }
        if (node == null) return Say1(args == "" ? "There is nothing here to harvest." : $"There is no '{Esc(args)}' here to harvest.");
        var refusal = NodeRefusal(who, node);
        if (refusal != null) return Say1(refusal);
        var def = NodeFor((node.Call<string>("QueryKind") ?? "").ToLowerInvariant(), false)!;
        var tool = BestTool(who, def.Tool);
        if (tool == null) return Say1(NoTool(def.Tool, "harvest " + (node.Call<string>("Short") ?? "that")));
        jobs[who.Name] = new Job { Verb = "harvest", Target = node };
        return Act("", "Harvest", $"bends to gather from {node.Call<string>("Short") ?? "the plant"}", "gather", "harvest");
    }

    private string? NodeRefusal(ObjectRef who, ObjectRef node)
    {
        var label = node.Call<string>("Short") ?? "it";
        if (!Trusted(node)) return $"Nothing can be gathered from {label}."; // a node from a wizard's sandbox gives nothing to anyone
        var def = NodeFor((node.Call<string>("QueryKind") ?? "").ToLowerInvariant(), false);
        if (def == null || def.Yields.Count == 0) return $"You do not know how to gather anything from {label}.";
        if (node.Call<int>("QueryCharges") <= 0) return $"{Cap(label)} has been picked clean. It will grow back in time.";
        return null;
    }

    private void FinishHarvest(ObjectRef who, Job job)
    {
        var node = job.Target;
        var room = Environment(who);
        if (node == null || !node.IsAlive || room == null || Environment(node) != room) { TellObject(who, "What you meant to harvest is gone.\n"); return; }
        var refusal = NodeRefusal(who, node);
        if (refusal != null) { TellObject(who, refusal + "\n"); return; }
        var def = NodeFor((node.Call<string>("QueryKind") ?? "").ToLowerInvariant(), false)!;
        var tool = BestTool(who, def.Tool);
        if (tool == null) { TellObject(who, NoTool(def.Tool, "harvest that") + "\n"); return; }
        var work = node.Call<int>("QueryDifficulty") * Num1("difficulty.node_scale", 5);
        var total = def.Yields.Sum(y => y.Weight);
        var roll = rollMode == "random" ? Random(total) : 0;
        var pick = def.Yields[0];
        foreach (var y in def.Yields) { if (roll < y.Weight) { pick = y; break; } roll -= y.Weight; }
        var label = node.Call<string>("Short") ?? "the plant";
        var prof = Tenths(who, def.Proficiency) / 10.0;
        var toolQ = tool.Call<int>("QueryQuality");
        // A fumble at a living plant costs time, not the plant: the charge is taken only when something is gathered.
        if (!RollPercent(SuccessChance(prof, toolQ, work), true))
        {
            TellObject(who, $"You fumble at {label} and come away with nothing.\n");
            return;
        }
        if (node.Call<bool>("TryGather", who) != true) { TellObject(who, $"{Cap(label)} has been picked clean.\n"); return; }
        Gathered(who, def.Proficiency, tool, work, mats[pick.Material], pick.Count, "", $"You harvest {label}", $"{who.Call<string>("QueryName")} gathers from {label}.", true);
    }

    /// <summary>The shared end of every gathering: the success roll (unless already made), the quality, a bonus unit, the item, growth, saving.</summary>
    private void Gathered(ObjectRef who, string profId, ObjectRef tool, double work, Mat mat, int units, string failText, string doneText, string roomText, bool succeeded = false)
    {
        var prof = Tenths(who, profId) / 10.0;
        var toolQ = tool.Call<int>("QueryQuality");
        if (!succeeded && !RollPercent(SuccessChance(prof, toolQ, work), true))
        {
            TellObject(who, failText + "\n");
            who.Call("CraftSave");
            return;
        }
        var v = (int)Num1("harvest.variation", 15);
        var (a, b) = RollPair(v);
        var quality = HarvestTier(HarvestScore(prof, toolQ, work, a + b - v, Efficiency(who)) + LightHarvest(who));
        var bonus = RollPercent(BonusChance(prof, toolQ), false) ? 1 : 0;
        var made = CreateIn(mat.Record, units + bonus, quality, who);
        if (made == null) { TellObject(who, "Something went wrong; nothing was gathered.\n"); return; }
        var what = Named(mat.Record, units + bonus, quality, mat.Id.Replace('_', ' '));
        var grew = Grow(who, profId, work);
        who.Call("CraftSave"); // on disk before anyone is told, so a crash after the message cannot lose it
        TellObject(who, $"{doneText} and take {what}" +
            $" (with your {Item.QualityNames[toolQ]} {tool.Call<string>("QueryToolType")}){(bonus > 0 ? ", one more than you hoped" : "")}.\n" + grew);
        var room = Environment(who);
        if (room != null) TellRoom(room, roomText + "\n", who);
    }

    /// <summary>"3 wolf skins" or "a wolf skin" to "wolf skins" or "wolf skin", for building "3 fine wolf skins".</summary>
    private static string StripCount(string text, int n)
    {
        var space = text.IndexOf(' ');
        if (space < 0) return text;
        var first = text.Substring(0, space);
        return first == n.ToString() || first is "a" or "an" or "the" ? text.Substring(space + 1) : text;
    }

    // ---- Crafting ----

    private sealed class Unit { public ObjectRef Ob = null!; public Mat Mat = null!; public int Quality; public int Left; }

    private List<Unit> UnitsOf(ObjectRef who)
    {
        var result = new List<Unit>();
        foreach (var ob in AllInventory(who))
        {
            if (!ob.IsAlive || who.Call<bool>("IsEquipped", ob)) continue;
            var mat = MatOf(ob);
            if (mat == null) continue;
            result.Add(new Unit { Ob = ob, Mat = mat, Quality = ob.Call<int>("QueryQuality"), Left = QuantityOf(ob) });
        }
        return result;
    }

    private static bool Fits(Slot s, Mat m) => s.IsCategory ? m.Categories.Contains(s.Name) : m.Id == s.Name;

    /// <summary>
    /// Choose the units for one craft: exact ingredients first, then categories. Within a slot: units the player named first,
    /// then the lowest quality held (or the highest, with best), so that good materials are not spent by accident. Returns the
    /// plan, or null with the missing ingredients in words.
    /// </summary>
    private List<(Unit U, int N, Slot S)>? Plan(Recipe r, List<Unit> units, List<ObjectRef> prefer, bool best, out string missing)
    {
        var plan = new List<(Unit, int, Slot)>();
        var lacking = new List<string>();
        foreach (var slot in r.Slots.OrderBy(s => s.IsCategory ? 1 : 0).ThenBy(s => units.Count(u => Fits(s, u.Mat))))
        {
            var need = slot.Count;
            var candidates = units.Where(u => u.Left > 0 && Fits(slot, u.Mat))
                .OrderBy(u => prefer.Contains(u.Ob) ? 0 : 1).ThenBy(u => best ? -u.Quality : u.Quality).ToList();
            foreach (var u in candidates)
            {
                if (need == 0) break;
                var n = Math.Min(need, u.Left);
                u.Left -= n;
                need -= n;
                plan.Add((u, n, slot));
            }
            if (need > 0)
                lacking.Add(slot.IsCategory ? $"{need} more of any {slot.Name}" : (need == slot.Count ? MatName(slot.Name, need) : $"{need} more {StripCount(MatName(slot.Name, need), need)}"));
        }
        foreach (var u in units) u.Left = QuantityOf(u.Ob); // the plan is only a plan until it is carried out
        missing = Join(lacking);
        return lacking.Count == 0 ? plan : null;
    }

    private Recipe? KnownByName(ObjectRef who, string name, out string? why)
    {
        why = null;
        var known = Known(who);
        var mine = known.Keys.Where(recipes.ContainsKey).Select(k => recipes[k]).ToList();
        var key = name.Trim().ToLowerInvariant();
        var hit = mine.FirstOrDefault(r => r.Name.ToLowerInvariant() == key || r.Id == key.Replace(' ', '_'))
                  ?? mine.FirstOrDefault(r => r.Name.ToLowerInvariant().StartsWith(key))
                  ?? mine.FirstOrDefault(r => r.Name.ToLowerInvariant().Contains(key));
        if (hit == null) why = $"You do not know a recipe called '{Esc(name)}'. Type {{cmd}}recipes{{/}} to see the ones you know.";
        return hit;
    }

    private bool StationHere(ObjectRef who, string kind)
    {
        if (kind == "") return true;
        var room = Environment(who);
        return room != null && AllInventory(room).Any(o => o.IsAlive && Trusted(o) && o.Call<bool>("IsStation") && string.Equals(o.Call<string>("QueryStationKind") ?? "", kind, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>craft NAME [COUNT] [best] [using THING [and THING]]</summary>
    private List<object> BeginCraft(ObjectRef who, string args)
    {
        if (args == "") return Say1("Craft what? Type {cmd}recipes{/} to see what you know, then {cmd}craft{/} NAME [COUNT].");
        if (who.Call<bool>("QueryInCombat")) return Say1("You cannot craft while you are fighting.");
        var prefer = new List<ObjectRef>();
        var usingAt = (" " + args + " ").IndexOf(" using ", StringComparison.OrdinalIgnoreCase);
        if (usingAt >= 0)
        {
            // In the padded text " using " starts at usingAt; in args the word "using" starts at usingAt.
            var named = args.Length > usingAt + 5 ? args.Substring(usingAt + 5).Trim() : "";
            args = args.Substring(0, Math.Max(0, usingAt - 1)).Trim();
            if (named == "") return Say1("Using what? {cmd}craft{/} NAME {cmd}using{/} THING [{cmd}and{/} THING]");
            foreach (var part in SplitAnd(named))
            {
                var ob = Present(part.ToLowerInvariant(), who);
                if (ob == null) return Say1($"You have no '{Esc(part)}'.");
                prefer.Add(ob);
            }
        }
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var best = false;
        if (words.Count > 1 && words[words.Count - 1].ToLowerInvariant() == "best") { best = true; words.RemoveAt(words.Count - 1); }
        var count = 1;
        if (words.Count > 1 && int.TryParse(words[words.Count - 1], out var n)) { count = n; words.RemoveAt(words.Count - 1); }
        if (count < 1) return Say1("Craft how many?");
        if (count > MaxBatch) return Say1($"You can craft at most {MaxBatch} at a time.");
        var recipe = KnownByName(who, string.Join(" ", words), out var why);
        if (recipe == null) return Say1(why!);
        var refusal = CraftRefusal(who, recipe, prefer, best);
        if (refusal != null) return Say1(refusal);
        jobs[who.Name] = new Job { Verb = "craft", RecipeId = recipe.Id, Remaining = count, Best = best, Prefer = prefer };
        return Act(count > 1 ? $"You set to work on {count} of {recipe.Name}." : "", "Craft " + recipe.Name, "sets to work", "craft", "craft " + recipe.Name);
    }

    private string? CraftRefusal(ObjectRef who, Recipe r, List<ObjectRef> prefer, bool best)
    {
        if (!StationHere(who, r.Station)) return $"{r.Name} must be made at {StationArticle(r.Station)}, and there is none here.";
        if (Plan(r, UnitsOf(who), prefer, best, out var missing) == null) return $"To make {r.Name} you still need {missing}.";
        return null;
    }

    private static List<string> SplitAnd(string text) =>
        text.Replace(",", " and ").Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s != "").ToList();

    /// <summary>combine A and B [and C]: the experiment. Unknown recipes are learned; a wrong mix uses nothing.</summary>
    private List<object> BeginCombine(ObjectRef who, string args)
    {
        if (args == "") return Say1("Combine what? {cmd}combine{/} THING {cmd}and{/} THING [{cmd}and{/} THING]");
        if (who.Call<bool>("QueryInCombat")) return Say1("You cannot do that while you are fighting.");
        var parts = SplitAnd(args);
        var max = (int)Num1("recipe.max_ingredients", 3);
        if (parts.Count > max) return Say1($"You cannot combine more than {max} things at once.");
        var prefer = new List<ObjectRef>();
        var offered = new List<string>();
        foreach (var part in parts)
        {
            var ob = Present(part.ToLowerInvariant(), who);
            if (ob == null) return Say1($"You have no '{Esc(part)}'.");
            var mat = MatOf(ob);
            if (mat == null) return Say1($"{Cap(ob.Call<string>("Short") ?? "That")} is not something you can craft with.");
            if (offered.Contains(mat.Id)) return Say1("You named the same thing twice.");
            offered.Add(mat.Id);
            prefer.Add(ob);
        }
        jobs[who.Name] = new Job { Verb = "combine", Offered = offered, Prefer = prefer };
        return Act("", "Combine", "experiments with some materials", "craft", "try your combination");
    }

    private void FinishCraft(ObjectRef who, Job job)
    {
        Recipe? r;
        if (job.Verb == "combine")
        {
            jobs.Remove(who.Name);
            var offered = job.Offered.Where(mats.ContainsKey).Select(id => mats[id]).ToList();
            r = offered.Count == job.Offered.Count ? Match(offered) : null;
            var stillHeld = job.Prefer.All(o => o.IsAlive && Environment(o) == who);
            var names = Join(job.Prefer.Where(o => o.IsAlive).Select(o => o.Call<string>("Short") ?? "something").ToList());
            if (r == null || !stillHeld || !StationHere(who, r.Station))
            {
                TellObject(who, $"You try {names} together in every way you can think of, but nothing comes of it. Nothing is used up.\n");
                return;
            }
            if (!Known(who).ContainsKey(r.Id))
            {
                if (!Learn(who, r, "discovered")) { TellObject(who, "Nothing comes of it.\n"); return; }
                TellObject(who, $"{{bgreen}}You have discovered a recipe: {r.Name}!{{/}} You will remember it ({{cmd}}recipe {r.Name}{{/}}).\n");
                if (r.Discovery is "rare" or "secret") ReportDiscovery(who, r);
                who.Call("CraftSave");
            }
            else TellObject(who, $"You recognise this: it is {r.Name}.\n");
            job = new Job { Verb = "craft", RecipeId = r.Id, Remaining = 1, Prefer = job.Prefer };
        }
        else if (!recipes.TryGetValue(job.RecipeId, out r) || !Known(who).ContainsKey(r.Id))
        {
            jobs.Remove(who.Name);
            TellObject(who, "You no longer know how to make that.\n");
            return;
        }

        var refusal = CraftRefusal(who, r, job.Prefer, job.Best);
        if (refusal != null)
        {
            jobs.Remove(who.Name);
            TellObject(who, (job.Made > 0 ? $"You stop after {job.Made}. " : "") + refusal + "\n");
            return;
        }
        if (!CraftOne(who, r, job))
        {
            jobs.Remove(who.Name);
            return;
        }
        job.Made++;
        job.Remaining--;
        if (job.Remaining <= 0 || !(who.Call<bool>("CraftContinue", "Craft " + r.Name, "sets to work", NumAt("time.craft", 0, 1.5), NumAt("time.craft", 1, 0), NumAt("time.craft", 2, -1.0)) is true))
        {
            jobs.Remove(who.Name);
            if (job.Made > 1) TellObject(who, $"You finish your work: {job.Made} done.\n");
            return;
        }
        jobs[who.Name] = job;
    }

    /// <summary>
    /// One craft, all or nothing: the outputs are made first (inside this daemon), then the ingredients are split off and
    /// destroyed, then the outputs are handed over and the character is saved. If anything fails before the hand-over, what was
    /// made is destroyed and the ingredients are left as they were.
    /// </summary>
    private bool CraftOne(ObjectRef who, Recipe r, Job job)
    {
        var plan = Plan(r, UnitsOf(who), job.Prefer, job.Best, out var missing);
        if (plan == null) { TellObject(who, $"To make {r.Name} you still need {missing}.\n"); return false; }
        var units = plan.Sum(p => p.N);
        var average = plan.Sum(p => (double)p.U.Quality * p.N) / units;
        var prof = Tenths(who, r.Proficiency) / 10.0;
        var steps = (int)Num1("craft.noise_steps", 20);
        var (a, b) = RollPair(steps);
        var quality = CraftTier(average + LightCraft(who), prof, r.Work, a, b, Aptitude(who));
        var bonus = RollPercent(CraftBonusChance(prof, r.Work), false);
        var giveBack = RollPercent(CraftReturnChance(prof, r.Work), false);
        var count = r.Count + (bonus ? 1 : 0);

        var used = plan.OrderBy(p => r.Slots.IndexOf(p.S)).Select(p => Named(p.U.Mat.Record, p.N, p.U.Quality, p.U.Mat.Id)).ToList();

        // 1. Make the outputs here first, so that a broken output file costs nothing.
        foreach (var leftover in AllInventory(Self)) leftover.Call("Vanish");
        ObjectRef? made;
        try { made = CreateIn(OutputRecord(r), count, quality, Self); }
        catch (MudRuntimeException e) { Log($"crafting: recipe '{r.Id}': cannot make {r.Output}: {FirstLine(e.Message)}"); made = null; }
        var outputs = AllInventory(Self).Where(o => o.IsAlive).ToList();
        if (made == null || outputs.Count == 0)
        {
            foreach (var o in outputs) o.Call("Vanish");
            TellObject(who, $"Something is wrong with the recipe for {r.Name}; nothing was used. Please tell a wizard.\n");
            return false;
        }

        // 2. Split off exactly the planned units, then destroy them. A refused split undoes the ones already made.
        var parts = new List<ObjectRef>();
        foreach (var (u, n, _) in plan)
        {
            var part = n >= QuantityOf(u.Ob) ? u.Ob : u.Ob.Call<ObjectRef>("Split", n);
            if (part == null)
            {
                foreach (var p in parts) if (p.IsAlive) p.Call("Settle");
                foreach (var o in outputs) o.Call("Vanish");
                TellObject(who, "Your materials would not come apart; nothing was used.\n");
                return false;
            }
            parts.Add(part);
        }
        var returned = giveBack ? plan[0] : default;
        foreach (var p in parts) p.Call("Vanish");

        // 3. Hand the outputs over, give back an ingredient if the craftsman saved one, and save the character.
        foreach (var o in outputs) MovePortion(o, QuantityOf(o), who);
        var returnText = "";
        if (giveBack && returned.U != null)
        {
            if (CreateIn(returned.U.Mat.Record, 1, returned.U.Quality, who) != null)
                returnText = $" You waste nothing: {MatName(returned.U.Mat.Id, 1)} is left over.";
        }
        var grew = Grow(who, r.Proficiency, r.Work);
        who.Call("CraftSave"); // on disk before anyone is told, so a crash after the message cannot lose it
        TellObject(who, $"You make {OutputName(r, count, quality)}{(bonus ? ", one more than the recipe promises" : "")}, using {Join(used)}.{returnText}\n" + grew);
        var room = Environment(who);
        if (room != null) TellRoom(room, $"{who.Call<string>("QueryName")} makes {OutputName(r, count, quality)}.\n", who);
        return true;
    }

    private void ReportDiscovery(ObjectRef who, Recipe r)
    {
        try
        {
            var room = Environment(who);
            LoadObject("/secure/events").Call<string>("Report", "craft.discover_" + r.Discovery, who, new List<ObjectRef> { who }, room!, r.Id,
                new Dictionary<string, string> { ["name"] = r.Name });
        }
        catch (MudRuntimeException) { }
    }

    // ---- The read-only commands ----

    private string Proficiencies(ObjectRef who)
    {
        var text = new StringBuilder("Your proficiencies:\n");
        foreach (var group in profs.Values.OrderBy(p => p.Order).GroupBy(p => p.Kind))
        {
            text.Append($"  {Cap(group.Key == "" ? "other" : group.Key)}\n");
            foreach (var p in group)
            {
                var points = Tenths(who, p.Id) / 10;
                text.Append($"    {p.Name}{new string(' ', Math.Max(1, 16 - p.Name.Length))}{points,3}  {Word(points)}\n");
            }
        }
        text.Append("They rise as you use them, fastest on work near your skill. ({cmd}help crafting{/})\n");
        return text.ToString();
    }

    private string RecipesText(ObjectRef who, string filter)
    {
        var known = Known(who).Keys.Where(recipes.ContainsKey).Select(k => recipes[k]).ToList();
        if (filter != "") known = known.Where(r => r.Name.ToLowerInvariant().Contains(filter.ToLowerInvariant()) || StationName(r.Station).ToLowerInvariant().Contains(filter.ToLowerInvariant())).ToList();
        if (known.Count == 0)
            return filter == "" ? "You know no recipes yet. Trainers and books teach them, and you can find your own: {cmd}combine{/} THING {cmd}and{/} THING.\n" : $"You know no recipe matching '{Esc(filter)}'.\n";
        var text = new StringBuilder("The recipes you know:\n");
        foreach (var group in known.GroupBy(r => r.Station).OrderBy(g => g.Key == "" ? "" : StationName(g.Key), StringComparer.Ordinal))
        {
            text.Append(group.Key == "" ? "  Anywhere\n" : $"  At {StationArticle(group.Key)}\n");
            foreach (var r in group.OrderBy(r => r.Name, StringComparer.Ordinal))
                text.Append($"    {{cmd}}{r.Name}{{/}}: {Needs(r)} ({ProfName(r.Proficiency)}, {r.Difficulty})\n");
        }
        return text.ToString();
    }

    private string RecipeText(ObjectRef who, string name)
    {
        if (name == "") return "Which recipe? {cmd}recipe{/} NAME. ({cmd}recipes{/} lists the ones you know.)\n";
        var r = KnownByName(who, name, out var why);
        if (r == null) return why + "\n";
        var mine = Tenths(who, r.Proficiency) / 10;
        var text = new StringBuilder($"{r.Name}\n");
        if (r.Description != "") text.Append($"  {r.Description}\n");
        text.Append($"  Needs:  {Needs(r)}\n");
        text.Append($"  Where:  {(r.Station == "" ? "anywhere" : "at " + StationArticle(r.Station))}\n");
        text.Append($"  Skill:  {ProfName(r.Proficiency)}, {r.Difficulty} (yours: {mine}, {Word(mine)})\n");
        text.Append($"  Makes:  {OutputName(r, r.Count, 1)}\n");
        text.Append($"  Type {{cmd}}craft {r.Name}{{/}} [COUNT] to make it. You use the lowest quality you have unless you add {{cmd}}best{{/}} or {{cmd}}using{{/}} THING.\n");
        return text.ToString();
    }

    /// <summary>Help on the crafting commands: "crafting", or one command word. Null for a topic this daemon does not know.</summary>
    public string? Help(string topic)
    {
        switch ((topic ?? "").Trim().ToLowerInvariant())
        {
            case "crafting":
            case "gathering":
                return "Crafting and gathering\n" +
                       "  Gather materials with {cmd}skin{/} and {cmd}bleed{/} (corpses) and {cmd}harvest{/} (bushes, patches, flowers). Each needs its tool,\n" +
                       "  carried, not wielded: a skinning knife, a bleeding kit, a harvest sickle. Better tools and more skill give better materials.\n" +
                       "  A corpse can be skinned once and bled once; blood must be drawn within five minutes of the death. For half an hour only the\n" +
                       "  one who made the kill may touch it.\n" +
                       "  Make things with {cmd}craft{/} NAME [COUNT]. Some recipes need a station (a herbalist table, a tanning rack, a workbench,\n" +
                       "  a cooking fire); simple ones can be made anywhere. The quality of what you make follows the quality of what you put in,\n" +
                       "  raised or lowered by your skill against the recipe's difficulty. Crafting never fails and never wastes materials.\n" +
                       "  Learn recipes from trainers and books: where a trainer stands, {cmd}lessons{/} lists what they teach and {cmd}learn{/} NAME\n" +
                       "  pays for it. Or find them yourself: {cmd}combine{/} THING {cmd}and{/} THING [{cmd}and{/} THING] at a station.\n" +
                       "  See also: help skin, bleed, harvest, craft, combine, recipes, recipe, proficiencies.\n";
            case "skin": return "skin [CORPSE]: take the skin of a dead beast. Needs a skinning knife. Once per corpse; a botched cut ruins the hide.\n";
            case "bleed": return "bleed [CORPSE]: draw the blood of a fresh kill into a vial. Needs a bleeding kit. Once per corpse, within five minutes of its death.\n";
            case "harvest": return "harvest [THING]: gather from a berry bush, an herb patch, wildflowers or thistle. Needs a harvest sickle. A plant picked clean grows back in time; a fumble costs time, not the plant.\n";
            case "craft": return "craft NAME [COUNT] [best] [using THING [and THING]]: make a recipe you know, COUNT times in a row (stop cancels the rest).\n  You use the lowest quality you carry, so good materials are not spent by accident; best uses the highest, using names the stacks to take first.\n";
            case "combine": return "combine THING and THING [and THING]: experiment. If the mix is a recipe (at the right station, for one that needs it), you learn it and make it once.\n  If not, nothing is used up.\n";
            case "recipes": return "recipes [WORD]: the recipes you know, grouped by where they are made, with what each needs.\n";
            case "recipe": return "recipe NAME: one recipe in full: what it needs, where, the skill it uses and what it makes.\n";
            case "proficiencies": return "proficiencies: your gathering and crafting skills, 0 to 100, in numbers and words. They rise as you use them.\n";
            case "apply": return "apply THING: use a bandage or salve, as an action on the combat clock.\n";
            default: return null;
        }
    }

    // ------------------------------------------------------------------
    // Healing over time, for crafted consumables
    // ------------------------------------------------------------------

    /// <summary>
    /// Start healing over time on a creature: perTick Vitality every seconds, ticks times. A character has one at a time; a new
    /// one replaces the old. The caller must be code from a trusted tree (a crafted consumable). Returns true if started.
    /// </summary>
    public bool StartHeal(ObjectRef who, int perTick, int ticks, int seconds, string name)
    {
        if (Self.IsClone || !Trusted(PreviousObject()) || who == null || !who.IsAlive || !who.Call<bool>("IsCreature")) return false;
        if (perTick < 1 || ticks < 1 || seconds < 1 || ticks > 100 || perTick > 10000) return false;
        var h = new HealOverTime { Who = who, PerTick = perTick, Left = ticks, Seconds = seconds, Generation = ++healGeneration, Name = name };
        heals[who.Name] = h;
        who.Call("AddEffect", "craft.heal", name, ticks * seconds, "heal_tick=" + perTick, 1.0, 0);
        var key = who.Name;
        var generation = h.Generation;
        CallOut(() => HealTick(key, generation), seconds);
        return true;
    }

    private void HealTick(string key, int generation)
    {
        if (!heals.TryGetValue(key, out var h) || h.Generation != generation) return;
        if (!h.Who.IsAlive || h.Who.Call<bool>("IsDead")) { heals.Remove(key); return; }
        var healed = h.Who.Call<int>("Heal", h.PerTick);
        if (healed > 0) TellObject(h.Who, $"{h.Name} soothes you. (+{healed} Vitality)" + (h.Who.Call<string>("HealNote", healed) ?? "") + "\n");
        h.Left--;
        if (h.Left <= 0) { heals.Remove(key); return; }
        CallOut(() => HealTick(key, generation), h.Seconds);
    }

    public override string Stat() => base.Stat() + "  " + QueryStatus() + "\n";
}
