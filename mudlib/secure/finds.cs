/// <summary>
/// The register of finds: treasure in hidden places that each character can take once, and that is worth less to
/// every finder than to the one before, until nothing is left.
///
/// For each find it keeps the lower-case names of the characters who have taken it, in the order they did. Whoever took it
/// k-th (counting from 0) is paid by a fixed rule from the find's own numbers, so the register stores no amounts and a
/// builder who changes a number changes what later finders get, never who has already taken it.
///
/// One public method, Claim, does the whole job at once: it checks who is calling, works out what this character
/// gets and writes the record to disk, and only then returns. The game thread runs one command to its end before the next
/// begins, so two characters taking in the same moment are two calls in a row, never two calls at once.
///
/// Rules of the register, all enforced here and not in the find object that asks:
///  - The caller is read from PreviousObject(). It must run from a trusted tree (the same list as the event service), it must
///    lie in a room that is itself in a trusted tree, and the character must be a player body standing in that room and be
///    the character whose command is running. Anything else is refused and nothing is recorded.
///  - The find's id is the caller's own program path. A caller cannot name another id, so no object can drain a stash it does
///    not own, and a find under /wiz can never be accepted. (Two stashes need two files.)
///  - Wizards (any level) may look and take nothing: they are not paid and not recorded.
///  - If the record cannot be saved, nothing is paid. A failed save never turns into free gold.
///
/// Blueprint only. Save file: save/secure/finds.json.
/// </summary>
public class Finds : MudObject
{
    private const string SaveFile = "/secure/finds";
    private const string WizListFile = "/secure/wizlist";
    private const string PlayerBody = "/std/player";

    /// <summary>The trees whose code may ask for a find. The same list as the event service (/secure/events); /wiz is not in it.</summary>
    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    // The codes in the third place of Claim's answer, and the answer of QueryStatus.
    public const int Ok = 0;        // paid
    public const int Taken = 1;     // this character has taken it before
    public const int Empty = 2;     // nothing is left for a new finder
    public const int Wizard = 3;    // a wizard: looks, takes nothing, is not recorded
    public const int Refused = 4;   // the caller or its room is not in a trusted tree
    public const int Away = 5;      // the character is not a player standing here, or is not the one whose command is running

    /// <summary>Find id (the program path of the find) to the lower-case names of the characters who took it, in order.</summary>
    [Saved] private Dictionary<string, List<string>> found = new();

    private readonly List<string> refusals = new();

    public override void Create()
    {
        if (!Self.IsClone) RestoreObject(SaveFile);
    }

    // ------------------------------------------------------------------
    // Who may ask
    // ------------------------------------------------------------------

    private static bool IsTrusted(ObjectRef? ob)
    {
        if (ob == null || !ob.IsAlive || ob.Domain != "") return false; // a sandbox object is never trusted (docs/SANDBOX.md)
        var path = ob.ProgramPath;
        if (path.Contains("..") || path.Contains("//")) return false;
        foreach (var tree in TrustedTrees) if (path.StartsWith(tree, StringComparison.Ordinal)) return true;
        return false;
    }

    private void Refuse(string why)
    {
        refusals.Add(Time() + " " + why);
        while (refusals.Count > 50) refusals.RemoveAt(0);
    }

    /// <summary>The last refusals (this run only), for builders who wonder why a find pays nothing.</summary>
    public List<string> QueryRefusals() => new(refusals);

    // ------------------------------------------------------------------
    // The rule
    // ------------------------------------------------------------------

    /// <summary>
    /// What the finder number k (0 for the first) is paid, or less than minimum when nothing is left: the first amount, times
    /// the fall-off percent once for each finder before, each time rounded down. Rounding down makes every finder get strictly
    /// less than the one before (the fall-off is held below 100), so the stash always ends.
    /// </summary>
    private static long AmountFor(int k, int first, int percent, int minimum)
    {
        long amount = first;
        for (var i = 0; i < k && amount >= minimum; i++) amount = amount * percent / 100;
        return amount;
    }

    /// <summary>
    /// The state of a find for a name: (code, gold, item, finders so far). Pure: changes nothing.
    /// first &lt;= 0 is a keepsake: no gold, and the item (required then) goes to each character once, with no end unless
    /// itemLimit says so. itemLimit is how many of the first finders get the item (0 for every finder while the stash lasts).
    /// </summary>
    private (int code, int gold, int item, int finders) Evaluate(string id, string name, int first, int percent, int minimum, bool hasItem, int itemLimit)
    {
        var list = found.GetValueOrDefault(id);
        var k = list?.Count ?? 0;
        if (list != null && list.Contains(name)) return (Taken, 0, 0, k);
        percent = Math.Clamp(percent, 0, 99);
        minimum = Math.Max(1, minimum);
        var gives = hasItem && (itemLimit <= 0 || k < itemLimit);
        if (first <= 0)
            return gives ? (Ok, 0, 1, k) : (Empty, 0, 0, k);
        var amount = AmountFor(k, first, percent, minimum);
        if (amount < minimum) return (Empty, 0, 0, k);
        return (Ok, (int)amount, gives ? 1 : 0, k);
    }

    // ------------------------------------------------------------------
    // Asking
    // ------------------------------------------------------------------

    /// <summary>The caller's id, or null with the refusal code in why when the caller or its room is not trusted.</summary>
    private string? CallerId(ObjectRef? caller, out int why)
    {
        why = Ok;
        if (Self.IsClone || !IsTrusted(caller)) { why = Refused; return null; }
        var room = Environment(caller);
        if (room == null || !IsTrusted(room)) { why = Refused; return null; }
        return caller!.ProgramPath;
    }

    private static bool IsPlayerBody(ObjectRef? ob)
        => ob != null && ob.IsAlive && ob.IsClone && ob.ProgramPath == PlayerBody && !string.IsNullOrEmpty(ob.Call<string>("QueryName"));

    /// <summary>
    /// Take a find. Called by the find object itself, from the command of the character who is taking it. Answers a list of
    /// three: the gold this character receives, 1 if they also receive the item and 0 if not, and a code
    /// (Ok, Taken, Empty, Wizard, Refused or Away). Gold and item are 0 unless the code is Ok, and the finding is recorded and
    /// saved before this returns. The find's id is the caller's program path; the numbers are the find's own.
    /// </summary>
    public List<int> Claim(ObjectRef who, int first, int percent, int minimum, bool hasItem, int itemLimit)
    {
        var caller = PreviousObject();
        var id = CallerId(caller, out var why);
        if (id == null)
        {
            Refuse($"{caller?.ProgramPath ?? "the driver"} (in {(caller == null ? "nowhere" : Environment(caller)?.ProgramPath ?? "nowhere")}) is not in a trusted tree");
            return new List<int> { 0, 0, why };
        }
        if (!IsPlayerBody(who) || who != ThisPlayer() || !Interactive(who) || Environment(who) != Environment(caller))
        {
            Refuse($"{id}: the taker is not the player here");
            return new List<int> { 0, 0, Away };
        }
        var name = who.Call<string>("QueryName")!.ToLowerInvariant();
        if (LoadObject(WizListFile).Call<int>("QueryLevel", name) > 0) return new List<int> { 0, 0, Wizard };

        var (code, gold, item, _) = Evaluate(id, name, first, percent, minimum, hasItem, itemLimit);
        if (code != Ok) return new List<int> { 0, 0, code };

        var had = found.TryGetValue(id, out var list);
        list ??= new List<string>();
        list.Add(name);
        found[id] = list;
        try
        {
            SaveObject(SaveFile);
        }
        catch (MudRuntimeException)
        {
            // Nothing is paid that is not on disk.
            list.RemoveAt(list.Count - 1);
            if (!had) found.Remove(id);
            Refuse($"{id}: the register could not be saved");
            return new List<int> { 0, 0, Refused };
        }
        return new List<int> { gold, item, Ok };
    }

    /// <summary>
    /// What a name would get from the calling find now, without recording anything: Ok (something is left for them), Taken,
    /// Empty or Refused. For descriptions and for builders; a wizard is answered like anyone else, since they only look.
    /// </summary>
    public int QueryStatus(string name, int first, int percent, int minimum, bool hasItem, int itemLimit)
    {
        var id = CallerId(PreviousObject(), out var why);
        if (id == null) return why;
        return Evaluate(id, (name ?? "").ToLowerInvariant(), first, percent, minimum, hasItem, itemLimit).code;
    }

    /// <summary>How many characters have taken the calling find (0 for a caller that is not trusted).</summary>
    public int QueryFinders()
    {
        var id = CallerId(PreviousObject(), out _);
        return id == null ? 0 : found.GetValueOrDefault(id)?.Count ?? 0;
    }
}
