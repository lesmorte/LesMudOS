/// <summary>
/// The tag registry (docs/specs/tags-and-saves.md, sections 2 and 3): the one place that says which tags exist, which family each belongs to,
/// which families are exclusive (at most one tag of the family on an object) and active (some rule reads them) or reserved (the words are held,
/// nothing reads them), and which reach and damage type each kind of weapon or natural attack implies.
///
/// A tag is a lower-case word of 2 to 16 letters and hyphens. It states what a thing IS, never a number. Every public function here begins
/// with "Query" and changes nothing, so a restricted caller (a wizard's workshop) may call them: a workshop can look a tag up, and can ask what
/// a tag set would become, but nobody can change the tables, which are frozen. A wizard's object that claims a tag the registry does not know,
/// or a reserved one, or two tags of one exclusive family, has the extra ones dropped when its tags are first read (QueryResolved), and
/// `stat` shows what was dropped; real code never takes a tag from a call.
///
/// The three kinds of line below (Family, Implied, Ladder, Only) are written one to a line so that tests/lint_area.py can read the vocabulary
/// without running the game. To add a tag: one word on one Family line (and for a new kind, an Implied line and a ladder in /secure/blows).
/// To start using a reserved family: change Reserved to Active in the change that adds the rule that reads it. Blueprint only.
/// </summary>
public class TagRegistry : MudObject
{
    private sealed class FamilyInfo
    {
        public string Name = "";
        public bool Exclusive, Active;
        public string[] Carriers = Array.Empty<string>();
        public string[] Tags = Array.Empty<string>();
    }

    private static readonly List<FamilyInfo> Families = new();
    private static readonly Dictionary<string, string> ImpliedTable = new();
    private static readonly HashSet<string> LadderPairs = new();
    private static readonly Dictionary<string, string[]> OnlyTable = new();

    private const bool Exclusive = true, Open = false, Active = true, Reserved = false;

    private static void Family(string name, bool exclusive, bool active, string carriers, string tags) =>
        Families.Add(new FamilyInfo { Name = name, Exclusive = exclusive, Active = active, Carriers = carriers.Split(' ', StringSplitOptions.RemoveEmptyEntries), Tags = tags.Split(' ', StringSplitOptions.RemoveEmptyEntries) });

    private static void Implied(string kind, string reach, string damage) => ImpliedTable[kind] = reach + " " + damage;

    private static void Ladder(string reach, string damage) => LadderPairs.Add(reach + " " + damage);

    private static void Only(string carrier, string family, string tags) => OnlyTable[carrier + " " + family] = tags.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static readonly bool Built = Build();

    private static bool Build()
    {
        Family("reach",    Exclusive, Active,   "weapon creature action", "melee ranged thrown");
        Family("damage",   Exclusive, Active,   "weapon creature action", "blunt slashing piercing");
        Family("kind",     Exclusive, Active,   "weapon creature action", "dagger sword spear axe mace staff bow pick club bite claw gore kick slam lash touch spit thorns");
        Family("armour",   Exclusive, Active,   "armour",                  "light medium heavy shield trinket");
        Family("material", Exclusive, Active,   "weapon armour",           "cloth leather wood iron steel bone stone");
        Family("rank",     Exclusive, Active,   "creature",                "elite boss");
        Family("nature",   Open,      Active,   "creature",                "beast goblin human undead spirit plant construct");
        Family("senses",   Open,      Active,   "creature",                "alert darksight");
        Family("element",  Open,      Reserved, "weapon armour creature action", "fire frost shock holy shadow");
        Family("affinity", Open,      Reserved, "creature armour",         "");   // weak-X, resists-X, immune-X for X in element
        Only("weapon", "kind", "dagger sword spear axe mace staff bow");
        Implied("dagger", "melee", "piercing");  Implied("sword", "melee", "slashing");  Implied("axe", "melee", "slashing");
        Implied("spear",  "melee", "piercing");  Implied("mace",  "melee", "blunt");     Implied("staff", "melee", "blunt");
        Implied("bow",    "ranged", "piercing"); Implied("pick",  "melee", "piercing");  Implied("club",  "melee", "blunt");
        Implied("bite",   "melee", "piercing");  Implied("claw",  "melee", "slashing");  Implied("gore",  "melee", "piercing");
        Implied("kick",   "melee", "blunt");     Implied("slam",  "melee", "blunt");     Implied("lash",  "melee", "blunt");
        Implied("touch",  "melee", "blunt");     Implied("spit",  "ranged", "blunt");    Implied("thorns", "ranged", "piercing");
        Ladder("melee", "blunt");  Ladder("melee", "slashing");  Ladder("melee", "piercing");  Ladder("ranged", "piercing");
        return true;
    }

    private static readonly string[] Elements = { "fire", "frost", "shock", "holy", "shadow" };
    private static readonly string[] AffinityForms = { "weak-", "resists-", "immune-" };

    private static bool Shaped(string tag)
    {
        if (tag == null || tag.Length < 2 || tag.Length > 16 || tag[0] < 'a' || tag[0] > 'z') return false;
        foreach (var c in tag) if ((c < 'a' || c > 'z') && c != '-') return false;
        return true;
    }

    private static FamilyInfo? FamilyFor(string tag)
    {
        if (!Shaped(tag)) return null;
        foreach (var f in Families) if (Array.IndexOf(f.Tags, tag) >= 0) return f;
        foreach (var form in AffinityForms)
            if (tag.StartsWith(form, StringComparison.Ordinal) && Array.IndexOf(Elements, tag.Substring(form.Length)) >= 0) return Families.First(f => f.Name == "affinity");
        return null;
    }

    private static FamilyInfo? Named(string family) => Families.FirstOrDefault(f => f.Name == family);

    /// <summary>The family names, in registry order.</summary>
    public List<string> QueryFamilies() => Families.Select(f => f.Name).ToList();

    /// <summary>The tags of a family, in registry order (empty for an unknown family or for the affinity forms).</summary>
    public List<string> QueryTagsOf(string family) => Named(family)?.Tags.ToList() ?? new List<string>();

    /// <summary>The family a tag belongs to, or "" if it is not registered.</summary>
    public string QueryFamilyOf(string tag) => FamilyFor(tag)?.Name ?? "";

    public bool QueryIsRegistered(string tag) => FamilyFor(tag) != null;

    /// <summary>Is the tag registered in a family that rules read? False for a reserved or an unknown tag.</summary>
    public bool QueryIsActive(string tag) => FamilyFor(tag) is { Active: true };

    /// <summary>Is the family exclusive (at most one tag of it on an object)?</summary>
    public bool QueryIsExclusive(string family) => Named(family) is { Exclusive: true };

    /// <summary>The reach and damage a kind implies, "melee piercing", or "" for a kind that implies nothing.</summary>
    public string QueryImplied(string kind) => ImpliedTable.TryGetValue(kind ?? "", out var pair) ? pair : "";

    /// <summary>Is there a hit-wording ladder for this (reach, damage) pair? (A kind with its own ladder needs none: see /secure/blows.)</summary>
    public bool QueryHasLadder(string reach, string damage) => LadderPairs.Contains(reach + " " + damage);

    /// <summary>The kinds a weapon may have (the same as its Category).</summary>
    public List<string> QueryWeaponKinds() => OnlyTable["weapon kind"].ToList();

    private static int OrderOf(FamilyInfo f, string tag) => Array.IndexOf(f.Tags, tag);

    /// <summary>
    /// The faults in a tag set for a carrier of kind "weapon", "armour", "creature" or "action" ("" for anything else: no carrier rules).
    /// Empty when the set is valid. A weapon must have exactly one reach, damage and kind; armour one armour tag; a creature one of each of
    /// reach, damage and kind (as its basic attack). Used by tests and by workshops; the run-time repair is QueryResolved.
    /// </summary>
    public List<string> QueryProblems(List<string> tags, string carrier)
    {
        var faults = new List<string>();
        var seen = new Dictionary<string, string>();
        foreach (var tag in tags ?? new List<string>())
        {
            var f = FamilyFor(tag);
            if (f == null) { faults.Add($"tag '{tag}' is not registered"); continue; }
            if (!f.Active) { faults.Add($"tag '{tag}' is reserved: no rule reads it yet"); continue; }
            if (carrier != "" && Array.IndexOf(f.Carriers, carrier) < 0) { faults.Add($"tag '{tag}' ({f.Name}) is not for a {carrier}"); continue; }
            if (OnlyTable.TryGetValue(carrier + " " + f.Name, out var only) && Array.IndexOf(only, tag) < 0) { faults.Add($"tag '{tag}' is not a {f.Name} of a {carrier}"); continue; }
            if (f.Exclusive && seen.TryGetValue(f.Name, out var first) && first != tag) faults.Add($"tags '{first}' and '{tag}' are both of the exclusive family '{f.Name}'");
            else seen[f.Name] = tag;
        }
        void Need(params string[] families) { foreach (var fam in families) if (!seen.ContainsKey(fam)) faults.Add($"a {carrier} needs a {fam} tag"); }
        if (carrier == "weapon" || carrier == "creature") Need("reach", "damage", "kind");
        else if (carrier == "armour") Need("armour");
        return faults;
    }

    /// <summary>
    /// What a declared set of tags becomes at run time for a carrier: the valid tags, each as "E:family:tag" (exclusive family) or "O:family:tag"
    /// (open), plus the tags the implied table supplies for a kind that has no reach or damage of its own, and a "!tag" entry for every tag
    /// that was dropped (not registered, reserved, not for this carrier, or a second tag of an exclusive family: the first in registry order
    /// stays). Never throws; a long or odd list is cut at 32 entries.
    /// </summary>
    public List<string> QueryResolved(List<string> tags, string carrier)
    {
        var result = new List<string>();
        var kept = new Dictionary<string, string>();   // exclusive family -> tag
        var open = new List<(string family, string tag)>();
        var dropped = new List<string>();
        var count = 0;
        foreach (var raw in tags ?? new List<string>())
        {
            if (++count > 32) break;
            var tag = raw ?? "";
            var f = FamilyFor(tag);
            var bad = f == null || !f.Active || (carrier != "" && Array.IndexOf(f.Carriers, carrier) < 0) || (OnlyTable.TryGetValue(carrier + " " + (f?.Name ?? ""), out var only) && Array.IndexOf(only, tag) < 0);
            if (bad) { if (!dropped.Contains(tag)) dropped.Add(tag); continue; }
            if (f!.Exclusive)
            {
                if (kept.TryGetValue(f.Name, out var have))
                {
                    if (have == tag) continue;
                    if (OrderOf(f, tag) < OrderOf(f, have)) { dropped.Add(have); kept[f.Name] = tag; }
                    else dropped.Add(tag);
                }
                else kept[f.Name] = tag;
            }
            else if (!open.Contains((f.Name, tag))) open.Add((f.Name, tag));
        }
        if (kept.TryGetValue("kind", out var kind) && ImpliedTable.TryGetValue(kind, out var pair) && (carrier == "weapon" || carrier == "creature" || carrier == "action"))
        {
            var parts = pair.Split(' ');
            if (!kept.ContainsKey("reach")) kept["reach"] = parts[0];
            if (!kept.ContainsKey("damage")) kept["damage"] = parts[1];
        }
        foreach (var kv in kept) result.Add($"E:{kv.Key}:{kv.Value}");
        foreach (var (family, tag) in open) result.Add($"O:{family}:{tag}");
        foreach (var d in dropped) result.Add("!" + d);
        return result;
    }
}
