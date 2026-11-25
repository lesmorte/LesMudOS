/// <summary>
/// The room numbers of GMCP (docs/specs/gmcp.md): a whole number for every room of the game, the same after every restart, so that
/// a client's map can name a room by its number for ever. A number is given once, for the room's file path, saved at once, and never
/// given again: a room that is deleted or renamed keeps its number to itself. Numbers start at 1.
///
/// Rooms under /wiz (a wizard's home, which changes all the time and is not part of the game) and rooms that are clones get numbers
/// from a separate range, above 1,000,000,000, that is not saved: they are new after a restart. If the saved table cannot be read or
/// written, every room gets such a number until the next restart, so that a saved number is never given to a second room.
///
/// Only /secure/gmcp may ask for a number to be assigned (it takes PreviousObject()); reading a number changes nothing, and anyone
/// may do it. Blueprint only. Save file: save/secure/roomids.json.
/// </summary>
public class RoomIds : MudObject
{
    private const string SaveFile = "/secure/roomids";
    private const string Service = "/secure/gmcp";
    /// <summary>The first number of the range that is not saved.</summary>
    public const int TemporaryBase = 1_000_000_000;

    /// <summary>Room path to its number.</summary>
    [Saved] private Dictionary<string, int> ids = new();
    [Saved] private int next = 1;

    private readonly Dictionary<string, int> temporary = new();
    private int nextTemporary = TemporaryBase + 1;
    private bool persistent = true;

    public override void Create()
    {
        if (Self.IsClone) return;
        try { RestoreObject(SaveFile); }
        catch (MudRuntimeException) { persistent = false; }
        // Never trust a table to be consistent: the next number is above every number in it.
        foreach (var n in ids.Values) if (n >= next) next = n + 1;
        if (next < 1) next = 1;
    }

    private static bool Temporary(string name) => name.ToLowerInvariant().StartsWith("/wiz/") || name.Contains('#');

    /// <summary>The number of a room, or 0 if it has none yet. Reading is harmless.</summary>
    public int QueryNumber(string path)
    {
        if (path == null) return 0;
        return ids.TryGetValue(path, out var n) ? n : temporary.TryGetValue(path, out var t) ? t : 0;
    }

    /// <summary>True if the number is from the range that is not saved.</summary>
    public bool QueryTemporary(int number) => number > TemporaryBase;

    /// <summary>How many rooms have a saved number.</summary>
    public int QueryCount() => ids.Count;

    /// <summary>
    /// The number of a room (its blueprint path, or a clone's name), given now if it has none. Only /secure/gmcp may ask; anyone else
    /// is refused with 0. The number is saved before it is returned.
    /// </summary>
    public int Assign(string path) => Assign(path, false);

    /// <summary>
    /// The same; with temporaryOnly a path that has no number yet gets one that is never saved (what a room of a wizard's sandbox
    /// names: docs/SANDBOX.md). Sandbox code itself is always refused.
    /// </summary>
    public int Assign(string path, bool temporaryOnly)
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller.IsClone || caller.Domain != "" || caller.ProgramPath != Service || RestrictedCaller() != null) return 0;
        if (path == null || path.Length == 0 || path.Length > 200 || path[0] != '/') return 0;
        var known = QueryNumber(path);
        if (known != 0) return known;
        if (temporaryOnly || Temporary(path) || !persistent)
        {
            // Bounded: a runaway builder cannot make this table grow without end.
            if (temporary.Count >= 100000) return 0;
            temporary[path] = nextTemporary++;
            return temporary[path];
        }
        var number = next++;
        ids[path] = number;
        try { SaveObject(SaveFile); }
        catch (MudRuntimeException)
        {
            // Not saved: this number must not outlive this run, and no saved number may be given twice.
            ids.Remove(path);
            persistent = false;
            temporary[path] = nextTemporary++;
            return temporary[path];
        }
        return number;
    }
}
