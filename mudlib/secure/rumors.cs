/// <summary>
/// The rumor queue: a rolling list of recent talk per region, the last fifty,
/// saved. The event service adds to it; gossips (the NPC side is wired later)
/// draw from it with DrawRumor. Reading is open to every object, writing is
/// for the event service only.
/// </summary>
public class RumorDaemon : MudObject
{
    private const string SaveFile = "/secure/rumors";
    private const string Service = "/secure/events";
    public const int Limit = 50;

    /// <summary>Lower-cased region to its rumors, oldest first.</summary>
    [Saved] private Dictionary<string, List<string>> byRegion = new();

    private bool persistent = true;
    private string lastDrawn = "";

    public override void Create()
    {
        if (Self.IsClone) return;
        try { RestoreObject(SaveFile); }
        catch (MudRuntimeException) { persistent = false; }
    }

    public bool QueryPersistent() => persistent;

    /// <summary>Add a rumor for a region. Event service only. Returns false if refused.</summary>
    public bool AddRumor(string region, string text)
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller.IsClone || caller.ProgramPath != Service || text == "") return false;
        var key = region.ToLowerInvariant();
        if (!byRegion.TryGetValue(key, out var list)) byRegion[key] = list = new List<string>();
        list.Add(text);
        while (list.Count > Limit) list.RemoveAt(0);
        if (persistent)
        {
            try { SaveObject(SaveFile); }
            catch (MudRuntimeException) { persistent = false; }
        }
        return true;
    }

    public int QueryCount(string region) => byRegion.TryGetValue(region.ToLowerInvariant(), out var list) ? list.Count : 0;

    /// <summary>The most recent rumors of a region, newest first. An empty region name means all regions.</summary>
    public List<string> QueryRumors(string region, int count)
    {
        var source = region == ""
            ? byRegion.Values.SelectMany(l => l).ToList()
            : byRegion.GetValueOrDefault(region.ToLowerInvariant()) ?? new List<string>();
        return Enumerable.Reverse(source).Take(count).ToList();
    }

    /// <summary>
    /// What a gossip says: one recent rumor of the region, newer ones likelier,
    /// phrased as hearsay, or "" if there is nothing to tell. Does not repeat
    /// the rumor it told last when it has another.
    /// </summary>
    public string DrawRumor(string region)
    {
        var all = region == ""
            ? byRegion.Values.SelectMany(l => l).ToList()
            : byRegion.GetValueOrDefault(region.ToLowerInvariant()) ?? new List<string>();
        if (all.Count == 0) return "";
        var pick = all[Math.Max(Random(all.Count), Random(all.Count))];
        if (pick == lastDrawn && all.Count > 1) pick = all[all.Count - 1] == lastDrawn ? all[all.Count - 2] : all[all.Count - 1];
        lastDrawn = pick;
        return pick;
    }
}
