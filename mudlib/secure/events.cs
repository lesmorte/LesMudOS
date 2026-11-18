/// <summary>
/// The event service: the single entry point through which the game reports
/// that something happened. It checks who is reporting, reads the event type
/// from the trusted catalogue (/events), applies "wizards make no history",
/// computes the Event Significance Score from the catalogue's data alone, and
/// then, by the score, awards Renown, starts a rumor and writes a Chronicle
/// entry. Nothing else in the game writes Renown or the Chronicle.
///
/// Blueprint only. Design: docs/CHRONICLE.md. Implementation: docs/CHRONICLE_IMPL.md.
/// </summary>
public class EventService : MudObject
{
    private const string SaveFile = "/secure/events";
    private const string ChronicleFile = "/secure/chronicle";
    private const string LedgerFile = "/secure/renown";
    private const string RumorFile = "/secure/rumors";
    private const string WizListFile = "/secure/wizlist";

    /// <summary>The trees whose code may report events. /wiz is not among them.</summary>
    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    private static readonly Dictionary<string, int> Impacts = new()
    {
        ["personal"] = 0, ["party"] = 25, ["guild"] = 50, ["settlement"] = 100, ["regional"] = 250, ["multiregional"] = 500, ["world"] = 1000
    };
    private static readonly Dictionary<string, int> Rarities = new()
    {
        ["common"] = 0, ["uncommon"] = 25, ["rare"] = 100, ["veryrare"] = 250, ["legendary"] = 500, ["unique"] = 1000, ["serverfirst"] = 2000
    };
    private static readonly Dictionary<string, int> Persistences = new()
    {
        ["temporary"] = 0, ["reoccurring"] = 25, ["longterm"] = 100, ["permanent"] = 500, ["worldaltering"] = 1000
    };
    private static readonly Dictionary<string, decimal> FlagMultipliers = new()
    {
        ["storyline"] = 1.5m, ["regional"] = 1.25m, ["world"] = 2.0m, ["seasonal"] = 1.25m, ["developer"] = 2.0m
    };
    private static readonly string[] Categories =
    {
        "Discovery", "Combat", "Exploration", "Crafting", "Politics", "Guilds", "Economy", "Factions", "Renown",
        "World Events", "Seasonal Events", "Player Created Content"
    };

    /// <summary>"event id|region|subject" to how many routine events were counted.</summary>
    [Saved] private Dictionary<string, int> counters = new();

    private sealed class Def
    {
        public string Id = "", Title = "", Kind = "World", Chron = "World Events", Sub = "";
        public string Headline = "", Text = "", Significance = "", Rumor = "", Historic = "", Tags = "", Listed = "";
        public int Category, Impact, Rarity, Persistence;
        public int? RCategory, RImpact, RRarity, RPersistence;
        public decimal Mult = 1m;
        public bool First, Milestone, Discovery, RepeatSame;
        public bool Once = true;
        public int Renown, RenownFirst, AggStep;
        public string AggOf = "", AggSubject = "";
    }

    private bool persistent = true;
    private readonly Dictionary<string, Def> cache = new();
    private readonly HashSet<string> loadedGroups = new();
    private readonly List<Def> aggregates = new();
    private readonly List<string> catalogueErrors = new();
    private readonly List<string> refusals = new();
    private List<string[]>? places;
    private List<string[]>? regions;

    public override void Create()
    {
        if (Self.IsClone) return;
        try { RestoreObject(SaveFile); }
        catch (MudRuntimeException) { persistent = false; }
    }

    private void Save()
    {
        if (!persistent) return;
        try { SaveObject(SaveFile); }
        catch (MudRuntimeException) { persistent = false; }
    }

    public bool QueryPersistent() => persistent;

    private ObjectRef Chron => LoadObject(ChronicleFile);
    private ObjectRef Ledger => LoadObject(LedgerFile);
    private ObjectRef Rumors => LoadObject(RumorFile);

    /// <summary>Whether the three daemons this service writes to can save. False means nothing survives a restart.</summary>
    public string Status()
        => $"events={persistent} chronicle={Chron.Call<bool>("QueryPersistent")} renown={Ledger.Call<bool>("QueryPersistent")} rumors={Rumors.Call<bool>("QueryPersistent")}";

    public List<string> QueryRefusals() => new(refusals);
    public List<string> QueryCatalogueErrors() => new(catalogueErrors);

    private string Refuse(string why)
    {
        refusals.Add(Time() + " " + why);
        while (refusals.Count > 50) refusals.RemoveAt(0);
        return "refused: " + why;
    }

    private static bool IsTrusted(ObjectRef? caller)
    {
        if (caller == null) return false;
        var path = caller.ProgramPath;
        if (path.Contains("..") || path.Contains("//")) return false;
        foreach (var tree in TrustedTrees) if (path.StartsWith(tree, StringComparison.Ordinal)) return true;
        return false;
    }

    // ------------------------------------------------------------------
    // The catalogue
    // ------------------------------------------------------------------

    private static bool ValidPrefix(string prefix)
        => prefix.Length > 0 && prefix.All(c => c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_');

    private static string Squash(string s) => s.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");

    private int Level(string value, Dictionary<string, int> table, string field, string id)
    {
        if (int.TryParse(value.Trim(), out var number)) return number;
        if (table.TryGetValue(Squash(value), out var found)) return found;
        catalogueErrors.Add($"{id}: unknown {field} '{value}'");
        return -1;
    }

    private Def? Parse(string source, string prefix)
    {
        var d = new Def();
        var bad = false;
        foreach (var raw in source.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line == "" || line.StartsWith("#")) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) { catalogueErrors.Add("bad line: " + line); bad = true; continue; }
            var k = line.Substring(0, eq).Trim().ToLowerInvariant();
            var v = line.Substring(eq + 1).Trim();
            switch (k)
            {
                case "id": d.Id = v; break;
                case "title": d.Title = v; break;
                case "kind": d.Kind = v; break;
                case "chron": d.Chron = v; break;
                case "sub": d.Sub = v; break;
                case "headline": d.Headline = v; break;
                case "text": d.Text = v; break;
                case "significance": d.Significance = v; break;
                case "rumor": d.Rumor = v; break;
                case "historic_name": d.Historic = v; break;
                case "tags": d.Tags = v; break;
                case "listed": d.Listed = v == "none" ? "" : v; break;
                case "category": d.Category = Level(v, new(), k, d.Id); break;
                case "impact": d.Impact = Level(v, Impacts, k, d.Id); break;
                case "rarity": d.Rarity = Level(v, Rarities, k, d.Id); break;
                case "persistence": d.Persistence = Level(v, Persistences, k, d.Id); break;
                case "repeat_category": d.RCategory = Level(v, new(), k, d.Id); break;
                case "repeat_impact": d.RImpact = Level(v, Impacts, k, d.Id); break;
                case "repeat_rarity": d.RRarity = Level(v, Rarities, k, d.Id); break;
                case "repeat_persistence": d.RPersistence = Level(v, Persistences, k, d.Id); break;
                case "repeat": d.RepeatSame = v == "same"; break;
                case "flags":
                    foreach (var f in v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (FlagMultipliers.TryGetValue(f.ToLowerInvariant(), out var m)) d.Mult *= m;
                        else { catalogueErrors.Add($"{d.Id}: unknown flag '{f}'"); bad = true; }
                    }
                    break;
                case "first": d.First = v == "yes"; break;
                case "milestone": d.Milestone = v == "yes"; break;
                case "once": d.Once = v != "no"; break;
                case "discovery": d.Discovery = v == "yes"; break;
                case "renown": d.Renown = int.TryParse(v, out var r) ? r : 0; break;
                case "renown_first": d.RenownFirst = int.TryParse(v, out var rf) ? rf : 0; break;
                case "aggregate_of": d.AggOf = v; break;
                case "aggregate_subject": d.AggSubject = v; break;
                case "aggregate_step": d.AggStep = int.TryParse(v, out var st) ? st : 0; break;
                default: catalogueErrors.Add($"{d.Id}: unknown field '{k}'"); bad = true; break;
            }
        }
        if (d.Id == "" || !d.Id.StartsWith(prefix + ".")) { catalogueErrors.Add($"{prefix}: id '{d.Id}' does not belong to this group"); return null; }
        if (d.Category < 0 || d.Impact < 0 || d.Rarity < 0 || d.Persistence < 0) bad = true;
        if (d.AggOf != "" && d.AggStep <= 0) { catalogueErrors.Add(d.Id + ": aggregate needs aggregate_step"); bad = true; }
        if (!Categories.Contains(d.Chron)) { catalogueErrors.Add($"{d.Id}: unknown Chronicle category '{d.Chron}'"); bad = true; }
        return bad ? null : d;
    }

    private void LoadGroup(string prefix)
    {
        if (loadedGroups.Contains(prefix) || !ValidPrefix(prefix)) return;
        List<string>? definitions;
        try { definitions = LoadObject("/events/" + prefix).Call<List<string>>("Definitions"); }
        catch (MudRuntimeException e) { catalogueErrors.Add("group " + prefix + ": " + e.Message); return; }
        if (definitions == null) return;
        loadedGroups.Add(prefix);
        foreach (var text in definitions)
        {
            var d = Parse(text, prefix);
            if (d == null) continue;
            cache[d.Id] = d;
            if (d.AggOf != "") aggregates.Add(d);
        }
    }

    private Def? Find(string id)
    {
        if (cache.TryGetValue(id, out var found)) return found;
        var dot = id.IndexOf('.');
        if (dot <= 0) return null;
        LoadGroup(id.Substring(0, dot));
        return cache.GetValueOrDefault(id);
    }

    /// <summary>Forget the loaded catalogue so that changed files are read again. Trusted callers only.</summary>
    public bool ReloadCatalogue()
    {
        if (Self.IsClone || !IsTrusted(PreviousObject())) return false;
        cache.Clear();
        loadedGroups.Clear();
        aggregates.Clear();
        catalogueErrors.Clear();
        places = null;
        regions = null;
        return true;
    }

    private void LoadPlaces()
    {
        if (places != null && regions != null) return;
        places = new List<string[]>();
        regions = new List<string[]>();
        try
        {
            var book = LoadObject("/events/places");
            foreach (var row in book.Call<List<string>>("Places") ?? new List<string>()) places.Add(row.Split('|'));
            foreach (var row in book.Call<List<string>>("Regions") ?? new List<string>()) regions.Add(row.Split('|', 3));
        }
        catch (MudRuntimeException e) { catalogueErrors.Add("places: " + e.Message); }
        places = places.OrderByDescending(p => p[0].Length).ToList();
    }

    // ------------------------------------------------------------------
    // Scoring
    // ------------------------------------------------------------------

    private static int ParticipationScore(int players)
        => players <= 1 ? 0 : players <= 5 ? 10 : players <= 10 ? 25 : players <= 25 ? 50 : players <= 50 ? 100 : players <= 100 ? 250 : 500;

    private static int Compute(Def d, int players, bool first)
    {
        var full = first || d.RepeatSame;
        var category = full ? d.Category : d.RCategory ?? d.Category;
        var impact = full ? d.Impact : d.RImpact ?? 0;
        var rarity = full ? d.Rarity : d.RRarity ?? 0;
        var persistence = full ? d.Persistence : d.RPersistence ?? d.Persistence;
        decimal baseEss = category + impact + ParticipationScore(players) + rarity + persistence;
        var final = baseEss * d.Mult * (first && d.First ? 2m : 1m);
        return (int)Math.Floor(final + 0.5m);
    }

    private static readonly string[] ClassNames = { "Routine", "Noteworthy", "Historical", "Major Historical", "Legendary", "Realm-Shaping", "World-Defining" };

    private static int ClassOf(int ess)
        => ess >= 5000 ? 6 : ess >= 2500 ? 5 : ess >= 1000 ? 4 : ess >= 500 ? 3 : ess >= 250 ? 2 : ess >= 100 ? 1 : 0;

    private static readonly int[] ClassBonus = { 0, 0, 5, 10, 25, 50, 100 };

    /// <summary>The ESS an event type would score for a number of players, as a first occurrence or not. No side effects; -1 if unknown.</summary>
    public int PreviewEss(string eventId, int players, bool first)
    {
        var d = Find(eventId);
        return d == null ? -1 : Compute(d, players, first);
    }

    /// <summary>The class name for a score: Routine ... World-Defining.</summary>
    public string ClassName(int ess) => ClassNames[ClassOf(ess)];

    /// <summary>A one-line description of an event type's data, for tests and builders; "" if unknown.</summary>
    public string Describe(string eventId)
    {
        var d = Find(eventId);
        if (d == null) return "";
        return $"{d.Id} category={d.Category} impact={d.Impact} rarity={d.Rarity} persistence={d.Persistence} mult={d.Mult} first={d.First} once={d.Once} renown={d.Renown}+{d.RenownFirst} listed={d.Listed} milestone={d.Milestone}";
    }

    public int QueryCounter(string eventId, string region, string subject) => counters.GetValueOrDefault(eventId + "|" + region + "|" + subject);

    // ------------------------------------------------------------------
    // Reporting
    // ------------------------------------------------------------------

    private static bool IsPlayerBody(ObjectRef? ob)
        => ob != null && ob.IsAlive && ob.IsClone && ob.ProgramPath == "/std/player" && !string.IsNullOrEmpty(ob.Call<string>("QueryName"));

    /// <summary>
    /// Report that something happened. The caller must run from a trusted tree.
    /// actor and participants are player bodies (the actor is added to the
    /// participants if missing); room is where it happened (null: the actor's
    /// room); subject tells apart things of one type (which boss, which area,
    /// which level), and is part of what "first" means; names holds the words
    /// the narrative needs (name, area, region, count, ...). No score is passed:
    /// it comes from the catalogue.
    ///
    /// Returns "ok ess=N class=C first=0|1 entry=ID rumor=0|1 credited=a,b",
    /// "ignored: reason", or "refused: reason".
    /// </summary>
    public string Report(string eventId, ObjectRef actor, List<ObjectRef> participants, ObjectRef room, string subject, Dictionary<string, string> names)
    {
        if (Self.IsClone) return "refused: not the service";
        var caller = PreviousObject();
        if (!IsTrusted(caller)) return Refuse($"{caller?.ProgramPath ?? "the driver"} is not in a trusted tree (event {eventId})");
        var d = Find(eventId ?? "");
        if (d == null) return Refuse($"unknown event type '{eventId}' from {caller!.ProgramPath}");

        var people = new List<ObjectRef>();
        var seen = new HashSet<string>();
        foreach (var p in (participants ?? new List<ObjectRef>()).Prepend(actor))
        {
            if (!IsPlayerBody(p)) continue;
            if (seen.Add(p.Call<string>("QueryName")!.ToLowerInvariant())) people.Add(p);
        }
        if (people.Count == 0) return Refuse($"no player takes part in {eventId}");

        // Wizards make no history: not recorded, not counted, told in one line.
        var wizlist = LoadObject(WizListFile);
        var kept = new List<ObjectRef>();
        foreach (var p in people)
        {
            if (wizlist.Call<int>("QueryLevel", p.Call<string>("QueryName")!) > 0)
                TellObject(p, "Wizards make no history.\n");
            else kept.Add(p);
        }
        if (kept.Count == 0) return "ignored: wizards make no history";

        return Process(d, kept, room ?? Environment(kept[0]), subject ?? "", names, false, 0);
    }

    private (string area, string region) Locate(ObjectRef? room, Dictionary<string, string> names)
    {
        var area = names.GetValueOrDefault("area", "");
        var region = names.GetValueOrDefault("region", "");
        if (room != null && (area == "" || region == ""))
        {
            if (area == "") area = room.Call<string>("QueryArea") ?? "";
            if (region == "") region = room.Call<string>("QueryRegion") ?? "";
            LoadPlaces();
            var row = places!.FirstOrDefault(p => room.ProgramPath.StartsWith(p[0], StringComparison.Ordinal));
            if (row != null)
            {
                if (area == "") area = row[1];
                if (region == "") region = row[2];
            }
        }
        return (area == "" ? "an unmapped corner of Hollowmere" : area, region == "" ? "Hollowmere" : region);
    }

    private static string Join(List<string> names)
        => names.Count <= 1 ? string.Concat(names) : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1];

    private static string Expand(string template, Dictionary<string, string> vars)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (i < template.Length)
        {
            if (template[i] == '{')
            {
                var j = template.IndexOf('}', i);
                if (j > i)
                {
                    sb.Append(vars.GetValueOrDefault(template.Substring(i + 1, j - i - 1), ""));
                    i = j + 1;
                    continue;
                }
            }
            sb.Append(template[i]);
            i++;
        }
        return sb.ToString();
    }

    private static string DefaultText(string kind) => kind switch
    {
        "Discovery" => "On {date}, {participants} discovered {name} in {region}, revealing knowledge thought lost to time.",
        "Victory" => "On {date}, {name} was defeated by {participants}, ending a period of danger in {region}.",
        "Crafting" => "On {date}, {participants} forged {name}, the first of its kind in recorded history.",
        "Faction" => "On {date}, {name} experienced a significant turning point.",
        "Quest" => "On {date}, {participants} completed {name}, and word of it spread through {region}.",
        "Milestone" => "On {date}, {participants} reached a milestone: {name}.",
        _ => "On {date}, the course of Hollowmere changed when {name} altered the future of the realm."
    };

    private static string DefaultSignificance(int cls) => cls switch
    {
        >= 6 => "A deed of permanent importance, which Hollowmere will not forget.",
        5 => "A deed that changed the history of {region} and will be told for years.",
        4 => "A legendary deed, long to be spoken of in {region}.",
        3 => "A major event in the history of {region}.",
        _ => "A notable event in the history of {region}."
    };

    private static string HistoricName(Def d, Dictionary<string, string> vars) => d.Kind switch
    {
        "Victory" => "The Fall of " + vars["name"],
        "Discovery" => "The " + vars["region"] + " Expedition",
        "World" => "The Founding of " + vars["name"],
        "Crafting" => "The Forging of " + vars["name"],
        _ => "The Rise of " + (vars["actor"] != "" ? vars["actor"] : vars["name"])
    };

    private string Process(Def d, List<ObjectRef> people, ObjectRef? room, string subject, Dictionary<string, string>? givenNames, bool internalCall, int depth)
    {
        var names = givenNames == null ? new Dictionary<string, string>() : new Dictionary<string, string>(givenNames);
        var chron = Chron;
        var ledger = Ledger;
        var key = d.Id + "|" + subject;

        // Who is credited: players who have not already been credited for this very thing.
        var credited = new List<ObjectRef>();
        var creditedNames = new List<string>();
        foreach (var p in people)
        {
            var nm = p.Call<string>("QueryName")!;
            if (d.Once && ledger.Call<bool>("HasDone", nm, key)) continue;
            credited.Add(p);
            creditedNames.Add(nm);
        }
        if (people.Count > 0 && credited.Count == 0) return "ignored: already credited";

        var first = !chron.Call<bool>("HasOccurred", key);
        var ess = Compute(d, credited.Count, first);
        var cls = ClassOf(ess);
        var (area, region) = Locate(room, names);

        var vars = new Dictionary<string, string>(names)
        {
            ["actor"] = creditedNames.Count > 0 ? creditedNames[0] : "",
            ["participants"] = Join(creditedNames),
            ["area"] = area,
            ["region"] = region,
            ["subject"] = subject,
            ["date"] = chron.Call<string>("DateText") ?? "",
        };
        foreach (var alias in new[] { "level", "class", "boss" })
            if (!vars.ContainsKey(alias)) vars[alias] = subject;
        if (!vars.ContainsKey("name") || vars["name"] == "") vars["name"] = subject != "" ? subject : d.Title;
        vars["title"] = Expand(d.Title, vars);

        // Routine events are counted toward aggregates and recorded nowhere else.
        // The Chronicle entry, if any, must exist before the first occurrence points at it.
        var entryId = 0;
        var headline = "";
        if (cls >= 2)
        {
            var historic = "";
            if (cls >= 5) historic = d.Historic != "" ? Expand(d.Historic, vars) : HistoricName(d, vars);
            headline = d.Headline != "" ? Expand(d.Headline, vars) : cls >= 5 ? historic : vars["name"];
            var text = Expand(d.Text != "" ? d.Text : DefaultText(d.Kind), vars);
            var significance = Expand(d.Significance != "" ? d.Significance : DefaultSignificance(cls), vars);
            var tags = new List<string>();
            if (d.Tags != "") tags.AddRange(d.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            tags.Add(region.ToLowerInvariant());
            tags.Add(d.Id);
            var refs = first ? "" : chron.Call<string>("QueryOccurred", key, "entry") ?? "";
            var fields = new Dictionary<string, string>
            {
                ["ess"] = ess.ToString(), ["classname"] = ClassNames[cls], ["category"] = d.Chron,
                ["subcategory"] = d.Sub != "" ? d.Sub : d.Kind, ["area"] = area, ["region"] = region,
                ["participants"] = string.Join("|", creditedNames), ["actor"] = vars["actor"], ["factions"] = "",
                ["headline"] = headline, ["text"] = text, ["significance"] = significance,
                ["tags"] = string.Join(",", tags), ["refs"] = refs == "0" ? "" : refs,
                ["event"] = d.Id, ["subject"] = subject, ["first"] = first ? "1" : "0",
                ["featured"] = cls >= 3 ? "1" : "0", ["historicname"] = historic,
            };
            entryId = chron.Call<int>("AddEntry", fields);
        }

        var rumored = false;
        if (cls >= 1)
        {
            var rumor = d.Rumor != "" ? Expand(d.Rumor, vars)
                : cls >= 2 ? $"They say the tale of {headline} is being written into the Chronicle."
                : $"They say {(vars["participants"] != "" ? vars["participants"] : "someone")} has been up to something remarkable around {area}.";
            rumored = Rumors.Call<bool>("AddRumor", region, rumor);
        }

        if (first)
        {
            var info = new Dictionary<string, string>
            {
                ["holders"] = string.Join("|", creditedNames.Select(n => n.ToLowerInvariant())), ["ess"] = ess.ToString(),
                ["entry"] = entryId.ToString(), ["listed"] = d.Listed, ["milestone"] = d.Milestone ? "1" : "0",
                ["title"] = vars["title"] != "" ? vars["title"] : vars["name"], ["event"] = d.Id, ["subject"] = subject,
            };
            chron.Call<bool>("MarkOccurred", key, info);
        }

        // Credit, Renown, discovery records.
        var bonus = ClassBonus[cls];
        foreach (var p in credited)
        {
            var nm = p.Call<string>("QueryName")!;
            if (d.Once) ledger.Call<bool>("MarkDone", nm, key);
            chron.Call("AddContribution", nm, ess, entryId > 0);
            var award = d.Renown + (first ? d.RenownFirst : 0) + bonus;
            if (award > 0) Announce(p, ledger.Call<List<string>>("Award", nm, award, d.Id), room, depth);
            if (d.Discovery) CheckRegions(p, nm, d.Id + "|" + subject);
        }

        if (cls == 0) CountRoutine(d, subject, region, area);

        return $"ok ess={ess} class={ClassNames[cls].Replace(" ", "").Replace("-", "")} first={(first ? 1 : 0)} entry={entryId} rumor={(rumored ? 1 : 0)} credited={string.Join(",", creditedNames.Select(n => n.ToLowerInvariant()))}";
    }

    /// <summary>Tell a player what their Renown just earned them, and raise the events for the tiers reached.</summary>
    private void Announce(ObjectRef player, List<string>? changes, ObjectRef? room, int depth)
    {
        if (changes == null) return;
        foreach (var change in changes)
        {
            var parts = change.Split(':', 2);
            if (parts[0] == "title")
            {
                TellObject(player, $"You have earned the title: {parts[1]}.\n");
                continue;
            }
            TellObject(player, $"Your renown grows. You are now known as {parts[1]}.\n");
            var tierEvent = Find("renown." + parts[1].ToLowerInvariant());
            if (tierEvent != null && depth < 3)
                Process(tierEvent, new List<ObjectRef> { player }, room, "", new Dictionary<string, string> { ["name"] = parts[1] }, true, depth + 1);
        }
    }

    /// <summary>Pathfinder: every area and hidden location of a closed region discovered.</summary>
    private void CheckRegions(ObjectRef player, string name, string discoveryKey)
    {
        var ledger = Ledger;
        ledger.Call("AddDiscovered", name, discoveryKey);
        LoadPlaces();
        var found = ledger.Call<List<string>>("QueryDiscovered", name) ?? new List<string>();
        foreach (var region in regions!)
        {
            if (region.Length < 3 || region[1] != "closed") continue;
            var needed = region[2].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!needed.All(found.Contains) || !needed.Contains(discoveryKey)) continue;
            if (!ledger.Call<bool>("MarkDone", name, "region|" + region[0])) continue;
            if (ledger.Call<bool>("GrantTitle", name, "Pathfinder"))
                TellObject(player, "You have earned the title: Pathfinder.\n");
            TellObject(player, $"You have mapped all of {region[0]}.\n");
            Announce(player, ledger.Call<List<string>>("Award", name, 50, "map " + region[0]), null, 0);
        }
    }

    /// <summary>A routine event: count it, and raise an aggregate event when a counter crosses a multiple of its step.</summary>
    private void CountRoutine(Def d, string subject, string region, string area)
    {
        var counterKey = d.Id + "|" + region + "|" + subject;
        var before = counters.GetValueOrDefault(counterKey);
        counters[counterKey] = before + 1;
        Save();
        LoadGroup("aggregate");
        foreach (var agg in aggregates.ToList())
        {
            if (agg.AggOf != d.Id || agg.AggSubject != subject && agg.AggSubject != "*") continue;
            if (before / agg.AggStep >= (before + 1) / agg.AggStep) continue;
            var count = (before + 1) / agg.AggStep * agg.AggStep;
            Process(agg, new List<ObjectRef>(), null, region,
                new Dictionary<string, string> { ["count"] = count.ToString(), ["region"] = region, ["area"] = area, ["name"] = subject }, true, 1);
        }
    }
}
