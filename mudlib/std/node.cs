// @inherit /std/thing

/// <summary>
/// A resource node: a berry bush, an herb patch, a seam of clay. It stays where it is, cannot be picked up, and holds a number of
/// charges that gatherers take with TryGather. Each charge taken comes back RegrowSeconds later, for everyone: a node picked clean
/// by one character is bare for the next until it regrows. This object only holds and counts the charges; the
/// `harvest` command, tools, skills and what a charge yields belong to the crafting system, which reads Kind and Difficulty.
/// A room places nodes with AddNode(path). See docs/BUILDING.md.
/// </summary>
public class Node : Thing
{
    /// <summary>What it is, in free text: "berry bush", "herb patch". Also answers to its words.</summary>
    protected string Kind = "plant";
    /// <summary>The most charges it holds.</summary>
    protected int Charges = 3;
    /// <summary>Seconds after a charge is taken until it is back.</summary>
    protected int RegrowSeconds = 600;
    /// <summary>How hard it is to gather well. An integer the builder sets; the crafting system decides what it means.</summary>
    protected int Difficulty = 1;
    /// <summary>What `look` shows while it has charges. If empty, LongDesc.</summary>
    protected string FullDesc = "";
    /// <summary>What `look` shows while it has none. If empty, FullDesc.</summary>
    protected string PickedDesc = "";
    /// <summary>The short description in the room while it has none ("a picked-over berry bush"). If empty, ShortDesc.</summary>
    protected string PickedShort = "";

    private readonly List<long> regrowAt = new();

    public bool IsNode() => true;

    public string QueryKind() => Kind;

    public int QueryMaxCharges() => Charges;

    public int QueryDifficulty() => Difficulty;

    public int QueryRegrowSeconds() => RegrowSeconds;

    /// <summary>Charges there are right now.</summary>
    public int QueryCharges()
    {
        var now = TimeMs();
        regrowAt.RemoveAll(t => t <= now);
        return Math.Max(0, Charges - regrowAt.Count);
    }

    /// <summary>
    /// Take one charge for this creature, who must be alive and in the room with the node. True if a charge was taken (it comes
    /// back RegrowSeconds later); false if the node is bare or the gatherer is not here.
    /// </summary>
    public bool TryGather(ObjectRef who)
    {
        // A node in a trusted tree can be gathered only by trusted code (the crafting daemon), so a wizard's sandbox cannot strip
        // real nodes bare. A node under /wiz answers anyone, for wizards' own tests.
        // A sandbox object (docs/SANDBOX.md) answers anyone too, and only a real object of a trusted tree counts as trusted code.
        var caller = PreviousObject();
        if (Self.Domain == "" && InTrusted(Self.ProgramPath) && !(caller != null && caller.Domain == "" && InTrusted(caller.ProgramPath))) return false;
        var room = Environment();
        if (room == null || !who.IsAlive || Environment(who) != room || !who.Call<bool>("IsCreature")) return false;
        if (QueryCharges() <= 0 || HiddenFrom(who)) return false;
        regrowAt.Add(TimeMs() + RegrowSeconds * 1000L);
        return true;
    }

    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    private static bool InTrusted(string path)
    {
        if (path.Contains("..")) return false;
        foreach (var tree in TrustedTrees) if (path.StartsWith(tree, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>Is this node one its room hides (Room.HideNode) from this character, who has not found it? Then it is not listed, not looked at, not harvested.</summary>
    private bool HiddenFrom(ObjectRef? who) => who != null && Environment() is { } room && room.Call<bool>("QueryNodeHiddenFrom", Self, who);

    public override bool Id(string id) => !HiddenFrom(ThisPlayer()) && (base.Id(id) || Kind.ToLowerInvariant() == id || Kind.ToLowerInvariant().Split(' ').Contains(id));

    public override string? Short()
    {
        if (HiddenFrom(ThisPlayer())) return null;
        return QueryCharges() > 0 || PickedShort == "" ? base.Short() : PickedShort;
    }

    public override string Long()
    {
        var text = QueryCharges() > 0 ? FullDesc : PickedDesc != "" ? PickedDesc : FullDesc;
        return (text != "" ? text : LongDesc ?? $"You see nothing special about {Short()}.") + "\n";
    }

    public override string Stat() => base.Stat() + $"  Node: kind '{Kind}', charges {QueryCharges()}/{Charges}, regrow {RegrowSeconds}s, difficulty {Difficulty}\n";
}
