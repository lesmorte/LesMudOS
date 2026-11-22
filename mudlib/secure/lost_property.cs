/// <summary>
/// The lost-property store. When a player dies, the belongings and gold in
/// their corpse are written here as well, and every change to the corpse is
/// written through, so a crash or restart cannot lose them. After a restart
/// every record on disk refers to a corpse that no longer exists; the owner
/// gets the contents back when they next log in.
///
/// Only a clone of /std/corpse may record or clear a record, and only a player
/// body may claim, and only the records of its own name (the name is taken
/// from the calling object, never from an argument). Monster corpses are never
/// recorded.
/// </summary>
public class LostProperty : MudObject
{
    private const string SaveFile = "/secure/lost_property";

    /// <summary>The save file of the store when it was still called the vault. Read once, if the new file does not exist yet.</summary>
    private const string LegacySaveFile = "/secure/vault";

    /// <summary>
    /// "name|corpse id" to the save records of the items in that corpse: a program path, then "?" and the item's state
    /// (quality, quantity, properties) unless it is the default. Records written before item state existed are bare
    /// paths and are handed back as items in the default state.
    /// </summary>
    [Saved] private Dictionary<string, List<string>> items = new();

    /// <summary>"name|corpse id" to the gold in that corpse.</summary>
    [Saved] private Dictionary<string, int> gold = new();

    /// <summary>Records made during this run. Their corpses still exist, so they cannot be claimed. Not saved.</summary>
    private readonly HashSet<string> live = new();

    public override void Create()
    {
        if (Self.IsClone) return;
        if (!RestoreObject(SaveFile) && RestoreObject(LegacySaveFile)) SaveObject(SaveFile);  // upgrade: carry the old records over
    }

    private static string Key(string player, string corpseId) => player.ToLowerInvariant() + "|" + corpseId;

    private bool CalledByCorpse()
    {
        var caller = PreviousObject();
        return !Self.IsClone && caller != null && caller.IsClone && caller.ProgramPath == "/std/corpse" && caller.Domain == "" && RestrictedCaller() == null;
    }

    /// <summary>A corpse reports its current contents. An empty report removes the record.</summary>
    public void Record(string player, string corpseId, List<string> records, int coins)
    {
        if (!CalledByCorpse() || player == "") return;
        var key = Key(player, corpseId);
        if (records.Count == 0 && coins <= 0)
        {
            Remove(key);
            return;
        }
        items[key] = new List<string>(records);
        gold[key] = coins;
        live.Add(key);
        SaveObject(SaveFile);
    }

    /// <summary>A corpse is empty or gone.</summary>
    public void Clear(string player, string corpseId)
    {
        if (!CalledByCorpse()) return;
        Remove(Key(player, corpseId));
    }

    private void Remove(string key)
    {
        live.Remove(key);
        var changed = items.Remove(key);
        changed |= gold.Remove(key);
        if (changed) SaveObject(SaveFile);
    }

    /// <summary>
    /// The calling player takes everything recorded under their name whose
    /// corpse no longer exists. Returns the gold as the first element, then
    /// the item records. The records are deleted.
    /// </summary>
    public List<string> Claim()
    {
        var result = new List<string> { "0" };
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller != ThisPlayer() || !caller.IsClone || caller.ProgramPath != "/std/player" || caller.Domain != "" || RestrictedCaller() != null) return result;
        var name = caller.Call<string>("QueryName")?.ToLowerInvariant();
        if (string.IsNullOrEmpty(name)) return result;

        var total = 0;
        foreach (var key in items.Keys.Where(k => k.StartsWith(name + "|") && !live.Contains(k)).ToList())
        {
            result.AddRange(items[key]);
            total += gold.GetValueOrDefault(key);
            items.Remove(key);
            gold.Remove(key);
        }
        result[0] = total.ToString();
        if (result.Count > 1 || total > 0) SaveObject(SaveFile);
        return result;
    }
}
