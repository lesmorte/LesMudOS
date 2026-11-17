/// <summary>
/// Base room. A room offers one command per exit to every player who walks
/// in; that is all "movement" is. It also holds scenery details, readable
/// text, creatures that are restocked at reset, wandering encounters and
/// locked exits. See docs/BUILDING.md for templates.
/// </summary>
public abstract class Room : MudObject
{
    private static readonly string[] CountWords = { "No", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" };

    protected string ShortDesc = "A room";
    protected string LongDesc = "A featureless room.";
    protected Dictionary<string, string> Exits = new();

    /// <summary>No combat can start or continue here, and players heal faster.</summary>
    protected bool Safe;
    /// <summary>A player who quits here enters the game here next time.</summary>
    protected bool LogoutRoom;
    /// <summary>Seconds between resets. Set it before the first AddSpawn.</summary>
    protected int ResetSeconds = 300;

    private sealed class Spawn
    {
        public string Path = "";
        public int Count;
        public List<ObjectRef> Handles = new();
    }

    private sealed class Encounter
    {
        public string Path = "";
        public int Percent;
        public ObjectRef? Current;
    }

    private sealed class Lock
    {
        public string KeyId = "";
        public string Refusal = "";
    }

    private readonly Dictionary<string, string> details = new();
    private readonly Dictionary<string, string> readables = new();
    private readonly Dictionary<string, Lock> locks = new();
    private readonly List<Spawn> spawns = new();
    private readonly List<Encounter> encounters = new();
    private long lastEncounter;
    private bool resetArmed;

    public override string? Short() => ShortDesc;

    // ---- Builder API ----

    /// <summary>Scenery that "look at" finds. names is comma-separated: "well,stone well".</summary>
    protected void AddDetail(string names, string description)
    {
        foreach (var name in Names(names)) details[name] = description;
    }

    /// <summary>Text shown by "read". Also registers a detail with the same names if there is none.</summary>
    protected void AddReadable(string names, string text, string? look = null)
    {
        foreach (var name in Names(names))
        {
            readables[name] = text;
            if (!details.ContainsKey(name)) details[name] = look ?? "There is writing on it. You could {cmd}read{/} it.";
        }
    }

    /// <summary>The room should contain this many of these. Cloned in now, topped up at each reset.</summary>
    protected void AddSpawn(string path, int count = 1)
    {
        var spawn = new Spawn { Path = path, Count = count };
        spawns.Add(spawn);
        Fill(spawn, true);
        ArmReset();
    }

    /// <summary>Each time a player walks in there is a percent chance that this creature wanders in.</summary>
    protected void AddEncounter(string path, int percent)
    {
        encounters.Add(new Encounter { Path = path, Percent = percent });
        ArmReset();
    }

    /// <summary>The exit can only be used by someone carrying an object that answers to keyId.</summary>
    protected void LockExit(string direction, string keyId, string refusal)
    {
        locks[direction] = new Lock { KeyId = keyId, Refusal = refusal };
    }

    /// <summary>A way nobody can use yet (a road into the wilderness). It is not listed among the exits, but the direction gives the message.</summary>
    protected void BlockExit(string direction, string message) => LockExit(direction, "", message);

    // ---- Queries ----

    public bool QuerySafe() => Safe;

    public bool QueryLogoutRoom() => LogoutRoom;

    public List<string> QueryExits() => Exits.Keys.ToList();

    public virtual string? QueryDetail(string name) => details.GetValueOrDefault(Names(name).FirstOrDefault() ?? "");

    public virtual string? QueryReadable(string name) => readables.GetValueOrDefault(Names(name).FirstOrDefault() ?? "");

    /// <summary>May this creature use the exit right now (is it not locked against them)?</summary>
    public bool CanUseExit(ObjectRef who, string direction)
    {
        if (!Exits.ContainsKey(direction)) return false;
        return !locks.TryGetValue(direction, out var door) || (door.KeyId != "" && Present(door.KeyId, who) != null);
    }

    public string Stat()
    {
        var text = new StringBuilder($"{Self.Name}: {ShortDesc}\n  Safe {Safe}, logout room {LogoutRoom}, reset every {ResetSeconds}s\n");
        foreach (var spawn in spawns) text.Append($"  Spawn {spawn.Path} x{spawn.Count}: {spawn.Handles.Count(h => h.IsAlive)} alive\n");
        foreach (var encounter in encounters) text.Append($"  Encounter {encounter.Path} {encounter.Percent}%\n");
        foreach (var exit in Exits) text.Append($"  Exit {exit.Key} -> {exit.Value}{(locks.ContainsKey(exit.Key) ? " (locked)" : "")}\n");
        return text.ToString();
    }

    // ---- Movement ----

    public override void Init()
    {
        ArmReset();
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        foreach (var direction in Exits.Keys.Concat(locks.Keys).Distinct().ToList())
        {
            AddAction(direction, GoExit);
            AddAction(direction.Substring(0, 1), GoExit); // "n" for "north", "u" for "up", "d" for "down"
        }
        MaybeEncounter();
    }

    private bool GoExit(string args)
    {
        var player = ThisPlayer();
        var verb = QueryVerb();
        var ways = Exits.Keys.Concat(locks.Keys).Distinct().ToList();
        var direction = ways.Contains(verb) ? verb : verb.Length == 1 ? ways.FirstOrDefault(d => d.StartsWith(verb)) : null;
        if (player == null || direction == null) return false;
        if (!Exits.ContainsKey(direction)) TellObject(player, locks[direction].Refusal + "\n"); // blocked, and not listed
        else MoveThrough(player, direction);
        return true;
    }

    /// <summary>Move a creature of this room through an exit, with the usual messages. False if it could not go.</summary>
    public bool MoveThrough(ObjectRef who, string direction)
    {
        if (!Exits.TryGetValue(direction, out var path) || !who.IsAlive || Environment(who) != Self) return false;
        if (!CanUseExit(who, direction))
        {
            TellObject(who, locks[direction].Refusal + "\n");
            return false;
        }

        ObjectRef destination;
        try
        {
            destination = LoadObject(path);
        }
        catch (MudRuntimeException problem) when (problem.Message.StartsWith("No such file"))
        {
            TellObject(who, $"The way {direction} is not open yet.\n");
            return false;
        }

        var name = who.Call<string>("QueryName") ?? "Someone";
        TellRoom(Self, $"{name} leaves {direction}.\n", who);
        MoveObject(who, destination);
        TellRoom(destination, $"{name} arrives.\n", who);
        who.Call("Look");
        return true;
    }

    // ---- Description ----

    public override string Long() => LongFor(ThisPlayer());

    /// <summary>The room as seen by one viewer, who is not listed among the occupants.</summary>
    public string LongFor(ObjectRef? viewer)
    {
        var text = new StringBuilder();
        // The room name is a title; every exit is a command the player can type.
        text.Append("{title}").Append(ShortDesc).Append("{/}\n").Append(LongDesc).Append('\n');
        text.Append(Exits.Count == 0 ? "There are no obvious exits.\n" : "Obvious exits: " + string.Join(", ", Exits.Keys.Select(d => "{cmd}" + d + "{/}")) + "\n");

        // Identical shorts are merged: "Two giant rats are here."
        var counts = new Dictionary<string, int>();
        var order = new List<string>();
        foreach (var ob in AllInventory())
        {
            if (ob == viewer) continue;
            var desc = ob.Call<string>("Short");
            if (desc == null) continue;
            if (!counts.ContainsKey(desc)) { counts[desc] = 0; order.Add(desc); }
            counts[desc]++;
        }
        foreach (var desc in order)
        {
            var n = counts[desc];
            text.Append(n == 1 ? Cap(desc) : $"{(n < CountWords.Length ? CountWords[n] : n.ToString())} {Plural(desc)}");
            text.Append(n == 1 ? " is here.\n" : " are here.\n");
        }
        return text.ToString();
    }

    private static string Cap(string text) => text.Length == 0 ? text : char.ToUpper(text[0]) + text.Substring(1);

    /// <summary>"a giant rat" becomes "giant rats".</summary>
    private static string Plural(string desc)
    {
        foreach (var article in new[] { "a ", "an ", "the " })
            if (desc.StartsWith(article)) { desc = desc.Substring(article.Length); break; }
        var of = desc.IndexOf(" of ", StringComparison.Ordinal);
        if (of > 0) return Plural(desc.Substring(0, of)) + desc.Substring(of);
        if (desc.EndsWith("s") || desc.EndsWith("x") || desc.EndsWith("ch") || desc.EndsWith("sh")) return desc + "es";
        if (desc.EndsWith("f")) return desc.Substring(0, desc.Length - 1) + "ves";
        return desc + "s";
    }

    private static List<string> Names(string names)
    {
        var result = new List<string>();
        foreach (var raw in names.Split(','))
        {
            var name = raw.Trim().ToLowerInvariant();
            foreach (var article in new[] { "the ", "a ", "an " })
                if (name.StartsWith(article)) { name = name.Substring(article.Length); break; }
            if (name != "") result.Add(name);
        }
        return result;
    }

    // ---- Reset, spawns and encounters ----

    /// <summary>Called every ResetSeconds. Tops up the spawns. Override it, and call base.Reset(), for more.</summary>
    public virtual void Reset()
    {
        foreach (var spawn in spawns) Fill(spawn, false);
        foreach (var encounter in encounters)
        {
            if (encounter.Current is not { IsAlive: true } creature) continue;
            if (creature.Call<bool>("QueryInCombat")) continue;
            TellRoom(Self, $"{Cap(creature.Call<string>("Short") ?? "Someone")} wanders off.\n");
            creature.Call("WanderOff");
            encounter.Current = null;
        }
        foreach (var spawn in spawns)
            foreach (var handle in spawn.Handles)
                if (handle.IsAlive) handle.Call("OnReset");
    }

    private void ArmReset()
    {
        if (resetArmed) return;
        resetArmed = true;
        CallOut(ResetCall, Math.Max(1, ResetSeconds));
    }

    private void ResetCall()
    {
        resetArmed = false;
        ArmReset(); // first, so that an error in Reset() cannot stop the cycle
        Reset();
    }

    /// <summary>Replace spawned things that are gone: dead creatures, items that left the room.</summary>
    private void Fill(Spawn spawn, bool strict)
    {
        spawn.Handles.RemoveAll(h => !h.IsAlive || (!h.Call<bool>("IsCreature") && Environment(h) != Self));
        while (spawn.Handles.Count < spawn.Count)
        {
            try
            {
                var thing = CloneObject(spawn.Path);
                MoveObject(thing, Self);
                spawn.Handles.Add(thing);
            }
            catch (MudRuntimeException) when (!strict)
            {
                return; // a later reset tries again
            }
        }
    }

    private void MaybeEncounter()
    {
        if (encounters.Count == 0 || Time() - lastEncounter < 60) return;
        if (encounters.Any(e => e.Current is { IsAlive: true })) return;
        foreach (var encounter in encounters)
        {
            if (Random(100) >= encounter.Percent) continue;
            try
            {
                var creature = CloneObject(encounter.Path);
                MoveObject(creature, Self);
                encounter.Current = creature;
                lastEncounter = Time();
                TellRoom(Self, $"{Cap(creature.Call<string>("Short") ?? "Something")} wanders in.\n");
            }
            catch (MudRuntimeException)
            {
                // A missing creature file must not stop a player from walking in.
            }
            return;
        }
    }
}
