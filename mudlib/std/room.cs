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
    /// <summary>No tell, from or to anyone in this room (wizards are not bound). docs/specs/communication.md.</summary>
    protected bool NoTell;
    /// <summary>Shouts neither leave this room nor are heard in it (wizards are not bound).</summary>
    protected bool NoShout;

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

    private sealed class Hidden
    {
        public string Path = "";
        public List<string> Hints = new();
        public int Chance;
        public string FoundLine = "";
    }

    private sealed class DoorInfo
    {
        public string Name = "door";
        public bool StartClosed = true;
        public string KeyId = "";
        public bool ClosesOnReset = true;
        public string Description = "";
        public string Partner = "";
        public string Key = "";
    }

    private const string DoorDaemon = "/secure/doors";

    private readonly Dictionary<string, DoorInfo> doors = new();
    private bool timedReset;
    private readonly Dictionary<string, string> details = new();
    private readonly Dictionary<string, string> readables = new();
    private readonly Dictionary<string, Lock> locks = new();
    private readonly List<Spawn> spawns = new();
    private readonly List<Encounter> encounters = new();
    private readonly Dictionary<string, Hidden> hidden = new();
    private int bandLow;
    private int bandHigh;
    private bool bandSet;
    private ObjectRef? areaRef;
    private long areaLooked;
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

    /// <summary>
    /// A door on an exit that is already in Exits (call it after setting Exits[direction]). The room on the other side declares the matching
    /// door on its own exit back, with AddDoor too; the two halves share one state (the register /secure/doors keeps it), so opening it
    /// from one side opens it from both, and a reload or a reset of one room cannot make the two disagree. A closed door stops walking,
    /// fleeing, patrols and Tracking; `look` and the exits line show it. name is what players type ("door", "iron gate"); closed: it
    /// starts closed (false: open); keyId: the id of the key that locks and unlocks it (a closed door with a key starts locked; "" for none);
    /// closesOnReset: the room's reset shuts it again (and locks it again if it has a key). Opening a door is loud (docs/BUILDING.md, sneaking).
    /// Two rooms can share one door only: it is named by the pair of rooms.
    /// </summary>
    protected void AddDoor(string direction, string name = "door", bool closed = true, string keyId = "", bool closesOnReset = true, string description = "")
    {
        if (!Exits.TryGetValue(direction, out var partner)) return; // a door needs an ordinary exit
        doors[direction] = new DoorInfo
        {
            Name = string.IsNullOrWhiteSpace(name) ? "door" : name.Trim().ToLowerInvariant(), StartClosed = closed, KeyId = keyId ?? "", ClosesOnReset = closesOnReset,
            Description = description ?? "", Partner = partner, Key = string.CompareOrdinal(Self.ProgramPath, partner) <= 0 ? Self.ProgramPath + "|" + partner : partner + "|" + Self.ProgramPath,
        };
        ArmReset();
    }

    /// <summary>
    /// An exit that is not listed and cannot be walked until a character has found it. It is found by `search` (findChance
    /// percent per attempt, 0 to 100) or, for certain, by looking at the detail named by hintDetail (comma-separated names, as in
    /// AddDetail; "" for none: only searching finds it). What a character has found is saved with the character. foundLine is
    /// the line the character reads when they find it; "" for a standard one. Other characters see nothing until they find it.
    /// </summary>
    protected void HiddenExit(string direction, string destination, string hintDetail, int findChance, string foundLine = "")
    {
        hidden[direction] = new Hidden
        {
            Path = destination, Hints = Names(hintDetail), Chance = Math.Max(0, Math.Min(100, findChance)),
            FoundLine = foundLine != "" ? foundLine : $"You find a hidden way {{cmd}}{direction}{{/}}!",
        };
    }

    /// <summary>
    /// Called once for each character and hidden exit, at the moment the character first finds the exit (by search or by looking at
    /// its detail). Does nothing here; a room overrides it to report a discovery (to the Chronicle, a quest, a counter).
    /// It is protected on purpose: only the room itself can run it.
    /// </summary>
    protected virtual void ExitFound(ObjectRef who, string direction) { }

    /// <summary>This room's level band: ground meant for characters of level low to high. Entering a room whose low end is two or more above the character's level warns them once. A room that does not call this takes the band of its area.cs, if that has one.</summary>
    protected void DangerBand(int low, int high)
    {
        bandLow = Math.Max(0, low);
        bandHigh = Math.Max(bandLow, high);
        bandSet = true;
    }

    /// <summary>Put a resource node (a /std/node file) here. The same as AddSpawn, under the name a reader looks for: the node is cloned in now and replaced at reset if it is gone. A node regrows by itself.</summary>
    protected void AddNode(string path, int count = 1) => AddSpawn(path, count);

    /// <summary>A way nobody can use yet (a road into the wilderness). It is not listed among the exits, but the direction gives the message.</summary>
    protected void BlockExit(string direction, string message) => LockExit(direction, "", message);

    // ---- Queries ----

    public bool QuerySafe() => Safe;

    public bool QueryNoTell() => NoTell;

    public bool QueryNoShout() => NoShout;

    public bool QueryLogoutRoom() => LogoutRoom;

    /// <summary>The ordinary exits. Hidden exits are not among them, found or not.</summary>
    public List<string> QueryExits() => Exits.Keys.ToList();

    /// <summary>The directions of the hidden exits of this room (for tools; a character sees only those they have found).</summary>
    public List<string> QueryHiddenExits() => hidden.Keys.ToList();

    /// <summary>Has this character found the hidden exit in this direction?</summary>
    public bool QueryExitFoundBy(ObjectRef who, string direction) => hidden.ContainsKey(direction) && Found(who, direction);

    private bool Found(ObjectRef who, string direction) => who.IsAlive && who.Call<bool>("QueryFoundExit", Self.Name, direction);

    public int QueryBandLow() => bandSet ? bandLow : FindArea()?.Call<int>("QueryBandLow") ?? 0;

    public int QueryBandHigh() => bandSet ? bandHigh : FindArea()?.Call<int>("QueryBandHigh") ?? 0;

    /// <summary>The path of the room an exit leads to, or null.</summary>
    public string? QueryExitPath(string direction) => Exits.GetValueOrDefault(direction);

    /// <summary>
    /// The area this room belongs to is named by the file area.cs in the room's own directory (see /std/area), so the area
    /// and region names are written once per area. A room with no such file reports no area.
    /// </summary>
    private ObjectRef? FindArea()
    {
        if (areaRef is { IsAlive: true }) return areaRef;
        if (areaRef == null && areaLooked != 0 && Time() - areaLooked < 30) return null; // no area.cs: do not look again at every step
        areaLooked = Time();
        var path = Self.ProgramPath;
        try { areaRef = LoadObject(path.Substring(0, path.LastIndexOf('/')) + "/area"); }
        catch (MudRuntimeException) { areaRef = null; }
        return areaRef;
    }

    public string QueryAreaName() => FindArea()?.Call<string>("QueryAreaName") ?? "";

    public string QueryRegionName() => FindArea()?.Call<string>("QueryRegionName") ?? "";

    public virtual string? QueryDetail(string name) => details.GetValueOrDefault(Names(name).FirstOrDefault() ?? "") ?? DoorDetail(name);

    /// <summary>"look at door", "look at iron gate": what a door looks like and what state it is in. Only if no detail has the name.</summary>
    private string? DoorDetail(string name)
    {
        var matches = MatchDoors(name ?? "");
        if (matches.Count != 1) return null;
        var direction = matches[0];
        var door = doors[direction];
        var state = QueryDoorState(direction);
        return (door.Description != "" ? door.Description + " " : "") + $"The {door.Name} {DoorWhere(direction)} is {state}." + (state == "locked" ? " It has a lock." : state == "closed" && door.KeyId != "" ? " It has a lock, and it is not locked." : "");
    }

    public virtual string? QueryReadable(string name) => readables.GetValueOrDefault(Names(name).FirstOrDefault() ?? "");

    /// <summary>May this creature use the exit right now (is it not locked against them)?</summary>
    public bool CanUseExit(ObjectRef who, string direction)
    {
        if (!Exits.ContainsKey(direction) && !(hidden.ContainsKey(direction) && Found(who, direction))) return false;
        if (DoorShut(direction)) return false;
        return !locks.TryGetValue(direction, out var door) || (door.KeyId != "" && Present(door.KeyId, who) != null);
    }

    // ---- Doors: the state is the register's (/secure/doors), never this room's ----

    /// <summary>The directions of this room's doors.</summary>
    public List<string> QueryDoors() => doors.Keys.ToList();

    /// <summary>The state of the door in this direction: "open", "closed" or "locked"; "" if there is no door there.</summary>
    public string QueryDoorState(string direction)
    {
        if (direction == null || !doors.TryGetValue(direction, out var door)) return "";
        var v = DoorBits(door);
        return (v & 1) != 0 ? "open" : (v & 2) != 0 ? "locked" : "closed";
    }

    /// <summary>What players call the door in this direction ("door", "iron gate"); "" if none.</summary>
    public string QueryDoorName(string direction) => direction != null && doors.TryGetValue(direction, out var door) ? door.Name : "";

    private int DoorBits(DoorInfo door)
    {
        try
        {
            var v = LoadObject(DoorDaemon).Call<int>("State", door.Key, !door.StartClosed, door.StartClosed && door.KeyId != "");
            return v < 0 ? 0 : v; // a register that does not answer leaves the door closed
        }
        catch (MudRuntimeException) { return 0; }
    }

    private bool DoorShut(string direction) => doors.TryGetValue(direction, out var door) && (DoorBits(door) & 1) == 0;

    private static string DoorWhere(string direction) => direction == "up" ? "above" : direction == "down" ? "below" : "to the " + direction;

    private string DoorExitNote(string direction) => doors.ContainsKey(direction) ? $" ({QueryDoorState(direction)} {doors[direction].Name})" : "";

    private string DoorRefusal(string direction)
    {
        var door = doors[direction];
        return QueryDoorState(direction) == "locked" ? $"The {door.Name} {DoorWhere(direction)} is locked." : $"The {door.Name} {DoorWhere(direction)} is closed. ({{cmd}}open {door.Name}{{/}})";
    }

    /// <summary>
    /// The way a typed direction ("north", "n") goes for this creature, if it can be used right now (it exists for them, is not locked
    /// against them), else null. Hidden exits count only for a creature that has found them.
    /// </summary>
    public string? QueryUsableExit(ObjectRef who, string typed)
    {
        typed = typed.Trim().ToLowerInvariant();
        if (typed == "") return null;
        var ways = Exits.Keys.Concat(hidden.Keys.Where(d => Found(who, d))).Distinct().ToList();
        var direction = ways.Contains(typed) ? typed : typed.Length == 1 ? ways.FirstOrDefault(d => d.StartsWith(typed)) : null;
        return direction != null && CanUseExit(who, direction) ? direction : null;
    }

    /// <summary>Where an exit leads for this creature: an ordinary exit's room, or a hidden exit's if they have found it; else null.</summary>
    public string? QueryExitDestination(ObjectRef who, string direction) =>
        Exits.TryGetValue(direction ?? "", out var path) ? path : hidden.TryGetValue(direction ?? "", out var h) && Found(who, direction!) ? h.Path : null;

    /// <summary>
    /// The rooms behind the ways this creature cannot take right now: an exit locked against them (a closed door) and a hidden exit
    /// they have not found. For Tracking's sense of something close (docs/specs/skill-ranks.md); it reveals no direction and finds nothing.
    /// </summary>
    public List<string> QueryClosedDestinations(ObjectRef who)
    {
        var result = new List<string>();
        foreach (var direction in Exits.Keys.Concat(hidden.Keys).Distinct())
        {
            if (CanUseExit(who, direction)) continue;
            var path = Exits.TryGetValue(direction, out var p) ? p : hidden[direction].Path;
            if (!result.Contains(path)) result.Add(path);
        }
        return result;
    }

    /// <summary>Every exit this creature can use right now.</summary>
    public List<string> QueryUsableExits(ObjectRef who)
        => Exits.Keys.Concat(hidden.Keys.Where(d => Found(who, d))).Distinct().Where(d => CanUseExit(who, d)).ToList();

    /// <summary>The exit that leads to the room at a path, if this creature can use it right now, else null.</summary>
    public string? QueryExitTowards(ObjectRef who, string path)
    {
        if (path == "") return null;
        foreach (var direction in QueryUsableExits(who))
            if ((Exits.GetValueOrDefault(direction) ?? (hidden.TryGetValue(direction, out var h) ? h.Path : null)) == path) return direction;
        return null;
    }

    public string Stat()
    {
        var text = new StringBuilder($"{Self.Name}: {ShortDesc}\n  Safe {Safe}, logout room {LogoutRoom}, reset every {ResetSeconds}s\n");
        foreach (var spawn in spawns) text.Append($"  Spawn {spawn.Path} x{spawn.Count}: {spawn.Handles.Count(h => h.IsAlive)} alive\n");
        foreach (var encounter in encounters) text.Append($"  Encounter {encounter.Path} {encounter.Percent}%\n");
        foreach (var exit in Exits) text.Append($"  Exit {exit.Key} -> {exit.Value}{(locks.ContainsKey(exit.Key) ? " (locked)" : "")}\n");
        foreach (var door in doors) text.Append($"  Door {door.Key}: {door.Value.Name}, {QueryDoorState(door.Key)}, key '{door.Value.KeyId}', closes on reset {door.Value.ClosesOnReset}\n");
        foreach (var exit in hidden) text.Append($"  Hidden exit {exit.Key} -> {exit.Value.Path} (find chance {exit.Value.Chance}%, detail '{string.Join(",", exit.Value.Hints)}'{(locks.ContainsKey(exit.Key) ? ", locked" : "")})\n");
        text.Append(QueryBandHigh() > 0 ? $"  Danger band {QueryBandLow()}-{QueryBandHigh()}{(bandSet ? "" : " (from the area)")}\n" : "  Danger band none\n");
        return text.ToString();
    }

    // ---- Movement ----

    public override void Init()
    {
        ArmReset();
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        foreach (var direction in Exits.Keys.Concat(locks.Keys).Concat(hidden.Keys).Distinct().ToList())
        {
            AddAction(direction, GoExit);
            AddAction(direction.Substring(0, 1), GoExit); // "n" for "north", "u" for "up", "d" for "down"
        }
        if (doors.Count > 0)
        {
            AddAction("open", a => DoorCommand("open", a));
            AddAction("close", a => DoorCommand("close", a));
            AddAction("unlock", a => DoorCommand("unlock", a));
            AddAction("lock", a => DoorCommand("lock", a));
        }
        MaybeEncounter();
    }

    private bool GoExit(string args)
    {
        var player = ThisPlayer();
        var verb = QueryVerb();
        if (player == null) return false;
        // Hidden exits are bound for everyone, but only a character who has found one has it among their ways.
        var ways = Exits.Keys.Concat(locks.Keys).Concat(hidden.Keys.Where(d => Found(player, d))).Distinct().ToList();
        var direction = ways.Contains(verb) ? verb : verb.Length == 1 ? ways.FirstOrDefault(d => d.StartsWith(verb)) : null;
        if (direction == null)
        {
            // A wizard sees hidden exits listed and is told why this one will not open; to everyone else the word means nothing.
            var unfound = hidden.Keys.FirstOrDefault(d => d == verb || (verb.Length == 1 && d.StartsWith(verb)));
            if (unfound == null || player.Call<int>("QueryWizLevel") < 1) return false;
            TellObject(player, "That way is hidden, and you have not found it yet. (Wizard note: it is found by search or by looking at its detail; see stat here.)\n");
            return true;
        }
        if (player.Call<bool>("QueryInCombat"))
        {
            TellObject(player, "You are in the middle of a fight and cannot just walk away. Try {cmd}flee{/} (or {cmd}flee{/} DIRECTION).\n");
            return true;
        }
        if (!Exits.ContainsKey(direction) && !hidden.ContainsKey(direction)) TellObject(player, locks[direction].Refusal + "\n"); // blocked, and not listed
        else MoveThrough(player, direction);
        return true;
    }

    /// <summary>Move a creature of this room through an exit, with the usual messages. False if it could not go.</summary>
    public bool MoveThrough(ObjectRef who, string direction)
    {
        var secret = !Exits.ContainsKey(direction) && hidden.ContainsKey(direction);
        string? path = Exits.GetValueOrDefault(direction);
        if (secret && Found(who, direction)) path = hidden[direction].Path;
        if (path == null || !who.IsAlive || Environment(who) != Self) return false;
        if (!CanUseExit(who, direction))
        {
            TellObject(who, (doors.ContainsKey(direction) && DoorShut(direction) ? DoorRefusal(direction) : locks[direction].Refusal) + "\n");
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

        // A sneaking character who is leaving: creatures that have not noticed them roll once more, and a room costs Endurance.
        if (Interactive(who) && !who.Call<bool>("SneakDeparts", destination)) return false;

        var name = who.Call<string>("QueryName") ?? "Someone";
        if (secret)
        {
            // Only those who have found the way know which way it was.
            foreach (var other in AllInventory())
                if (other != who && Living(other)) TellObject(other, $"{name} leaves {(Found(other, direction) ? direction : "somewhere")}.\n");
        }
        else TellRoom(Self, $"{name} leaves {direction}.\n", who);
        MoveObject(who, destination);
        if (Interactive(who)) who.Call("RecordArrival"); // so that flee can lead back the way they came
        TellRoom(destination, $"{name} arrives.\n", who);
        who.Call("Look");
        WarnIfDangerous(who, destination);
        return true;
    }

    /// <summary>Entering ground whose band starts two or more levels above the character, from ground that did not, earns one line.</summary>
    private void WarnIfDangerous(ObjectRef who, ObjectRef destination)
    {
        if (!Interactive(who)) return;
        var level = who.Call<int>("QueryLevel");
        if (destination.Call<int>("QueryBandLow") - level < 2 || QueryBandLow() - level >= 2) return;
        TellObject(who, "{yellow}You feel a chill of warning: this ground is more dangerous than you are ready for.{/}\n");
    }

    // ---- Doors: the commands ----

    /// <summary>The directions of the doors that the words name: a direction ("east", "e"), a name ("door", "iron gate", the last word of it), or both ("east door").</summary>
    private List<string> MatchDoors(string words)
    {
        words = words.Trim().ToLowerInvariant();
        foreach (var article in new[] { "the ", "a ", "an " })
            if (words.StartsWith(article)) { words = words.Substring(article.Length); break; }
        var result = new List<string>();
        if (words == "") return result;
        foreach (var (direction, door) in doors)
        {
            var last = door.Name.Contains(' ') ? door.Name.Substring(door.Name.LastIndexOf(' ') + 1) : door.Name;
            if (words == direction || (words.Length == 1 && direction.StartsWith(words)) || words == door.Name || words == last
                || words == direction + " " + door.Name || words == direction + " " + last || words == door.Name + " " + direction || words == last + " " + direction
                || words == DoorWhere(direction) + " " + door.Name) result.Add(direction);
        }
        return result;
    }

    private bool DoorCommand(string verb, string args)
    {
        var player = ThisPlayer();
        if (player == null || !Interactive(player) || Environment(player) != Self) return false;
        var words = args.Trim();
        string direction;
        if (words == "")
        {
            if (doors.Count != 1) { TellObject(player, $"{(verb == "open" ? "Open" : verb == "close" ? "Close" : verb == "lock" ? "Lock" : "Unlock")} which door? ({string.Join(", ", doors.Keys.Select(d => "{cmd}" + d + "{/}"))})\n"); return true; }
            direction = doors.Keys.First();
        }
        else
        {
            var matches = MatchDoors(words);
            if (matches.Count == 0) return false; // not one of this room's doors: another object's command may take it
            if (matches.Count > 1) { TellObject(player, $"Which one? ({string.Join(", ", matches.Select(d => "{cmd}" + verb + " " + d + "{/}"))})\n"); return true; }
            direction = matches[0];
        }
        var door = doors[direction];
        var state = QueryDoorState(direction);
        var name = player.Call<string>("QueryName") ?? "Someone";
        var where = DoorWhere(direction);
        var daemon = LoadObject(DoorDaemon);
        switch (verb)
        {
            case "open":
                if (state == "open") { TellObject(player, $"The {door.Name} {where} is already open.\n"); return true; }
                if (state == "locked") { TellObject(player, $"The {door.Name} {where} is locked.\n"); return true; }
                if (!daemon.Call<bool>("Open", door.Key)) { TellObject(player, $"The {door.Name} {where} will not open.\n"); return true; }
                TellObject(player, $"You open the {door.Name} {where}.\n");
                TellRoom(Self, $"{name} opens the {door.Name} {where}.\n", player);
                TellPartner(door, "swings open");
                player.Call("Disturbed", "open"); // loud: it ends sneaking and wakes the room
                return true;
            case "close":
                if (state != "open") { TellObject(player, $"The {door.Name} {where} is already closed.\n"); return true; }
                if (!daemon.Call<bool>("Close", door.Key)) { TellObject(player, $"The {door.Name} {where} will not close.\n"); return true; }
                TellObject(player, $"You close the {door.Name} {where}.\n");
                TellRoom(Self, $"{name} closes the {door.Name} {where}.\n", player);
                TellPartner(door, "is pushed shut");
                return true;
            case "unlock":
                if (door.KeyId == "") { TellObject(player, $"The {door.Name} {where} has no lock.\n"); return true; }
                if (state != "locked") { TellObject(player, $"The {door.Name} {where} is not locked.\n"); return true; }
                if (Present(door.KeyId, player) is not { } unlocking) { TellObject(player, $"You have nothing that fits the lock of the {door.Name} {where}.\n"); return true; }
                if (!daemon.Call<bool>("Unlock", door.Key)) { TellObject(player, $"The lock of the {door.Name} {where} will not turn.\n"); return true; }
                TellObject(player, $"You unlock the {door.Name} {where} with {unlocking.Call<string>("Short") ?? "the key"}.\n");
                TellRoom(Self, $"{name} unlocks the {door.Name} {where}.\n", player);
                return true;
            default:
                if (door.KeyId == "") { TellObject(player, $"The {door.Name} {where} has no lock.\n"); return true; }
                if (state == "open") { TellObject(player, $"You cannot lock the {door.Name} {where} while it is open. Close it first.\n"); return true; }
                if (state == "locked") { TellObject(player, $"The {door.Name} {where} is already locked.\n"); return true; }
                if (Present(door.KeyId, player) is not { } locking) { TellObject(player, $"You have nothing that fits the lock of the {door.Name} {where}.\n"); return true; }
                if (!daemon.Call<bool>("Lock", door.Key)) { TellObject(player, $"The lock of the {door.Name} {where} will not turn.\n"); return true; }
                TellObject(player, $"You lock the {door.Name} {where} with {locking.Call<string>("Short") ?? "the key"}.\n");
                TellRoom(Self, $"{name} locks the {door.Name} {where}.\n", player);
                return true;
        }
    }

    // ---- Hidden exits: finding them ----

    /// <summary>
    /// Search the room once, for this character, who must be the caller (the search command). Each hidden exit they have not found
    /// is found with its own chance. True if anything was found.
    /// </summary>
    public bool Search(ObjectRef who)
    {
        if (PreviousObject() != who || Environment(who) != Self) return false;
        var any = false;
        foreach (var exit in hidden.ToList())
            if (!Found(who, exit.Key) && Random(100) < exit.Value.Chance && Reveal(who, exit.Key)) any = true;
        return any;
    }

    /// <summary>The character (the caller) has just looked at a detail of this room. A hidden exit that names it is found, for certain.</summary>
    public void Examined(ObjectRef who, string name)
    {
        if (PreviousObject() != who || hidden.Count == 0 || Environment(who) != Self) return;
        var key = Names(name).FirstOrDefault();
        if (key == null) return;
        foreach (var exit in hidden.ToList())
            if (exit.Value.Hints.Contains(key) && !Found(who, exit.Key)) Reveal(who, exit.Key);
    }

    private bool Reveal(ObjectRef who, string direction)
    {
        if (!who.Call<bool>("RecordFoundExit", direction)) return false; // the character keeps the record; false if already there or refused
        TellObject(who, hidden[direction].FoundLine + "\n");
        try { ExitFound(who, direction); }
        catch (MudRuntimeException)
        {
            // A fault in a room's discovery hook must not undo the discovery.
        }
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
        var detailed = viewer != null && viewer.Call<double>("QueryMod", "exit_detail") > 0;
        var wizard = viewer != null && viewer.Call<int>("QueryWizLevel") >= 1;
        // Hidden exits are listed for a character who has found them; a wizard sees all of them, marked.
        var ways = Exits.Keys.Concat(hidden.Keys.Where(d => !Exits.ContainsKey(d) && viewer != null && (wizard || Found(viewer, d)))).ToList();
        text.Append(ways.Count == 0 ? "There are no obvious exits.\n" : "Obvious exits: " + string.Join(", ", ways.Select(d => "{cmd}" + d + "{/}" + DoorExitNote(d) + (wizard && hidden.ContainsKey(d) && !Exits.ContainsKey(d) ? " (hidden)" : "") + (detailed ? ExitNote(d, viewer!) : ""))) + "\n");

        // Identical shorts are merged: "Two giant rats are here." Things lying about (not creatures) are listed the same way as
        // everywhere else: "A minor healing potion (x2) is here."
        var counts = new Dictionary<string, int>();
        var order = new List<string>();
        var floor = new Dictionary<string, (string single, int n)>();
        var sitting = new List<string>();
        foreach (var ob in AllInventory())
        {
            if (ob == viewer) continue;
            var desc = ob.Call<string>("Short");
            if (desc == null) continue;
            if (!Living(ob) && ob.Call<int>("QueryQuantity") > 0)
            {
                var single = ob.Call<int>("QueryAmount") > 0 || ob.Call<int>("QueryQuantity") == 1 ? desc : ob.Call<string>("DescribeCount", 1) ?? desc;
                var state = string.Join("&", (ob.Call<string>("QueryState") ?? "").Split('&', StringSplitOptions.RemoveEmptyEntries).Where(p => !p.StartsWith("n=")));
                var key = single + "|" + state;
                if (!floor.TryGetValue(key, out var g)) { g = (single, 0); order.Add("\u0001" + key); }
                floor[key] = (g.single, g.n + Math.Max(1, ob.Call<int>("QueryQuantity")));
                continue;
            }
            if (Living(ob) && ob.Call<bool>("QueryResting")) { sitting.Add(desc); continue; } // "Keith is resting here."
            if (!counts.ContainsKey(desc)) { counts[desc] = 0; order.Add(desc); }
            counts[desc]++;
        }
        foreach (var desc in order)
        {
            if (desc.Length > 0 && desc[0] == '\u0001')
            {
                var g = floor[desc.Substring(1)];
                // A pile of coins names its own count ("100 gold coins"): it reads as a plural.
                text.Append(Cap(g.n > 1 ? $"{g.single} (x{g.n})" : g.single) + (char.IsDigit(g.single[0]) ? " are here.\n" : " is here.\n"));
                continue;
            }
            var n = counts[desc];
            text.Append(n == 1 ? Cap(desc) : $"{(n < CountWords.Length ? CountWords[n] : n.ToString())} {Plural(desc)}");
            text.Append(n == 1 ? " is here.\n" : " are here.\n");
        }
        foreach (var desc in sitting) text.Append(Cap(desc)).Append(" is resting here.\n");
        return text.ToString();
    }

    /// <summary>Where an exit leads, and whether it is locked against the viewer (Traveler's Instinct): " (to Village Square)".</summary>
    private string ExitNote(string direction, ObjectRef viewer)
    {
        var where = "unexplored";
        try { where = LoadObject(Exits.GetValueOrDefault(direction) ?? hidden[direction].Path).Call<string>("Short") ?? where; }
        catch (MudRuntimeException) { }
        return $" (to {where}{(locks.ContainsKey(direction) && !CanUseExit(viewer, direction) ? ", locked" : "")})";
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
        if (timedReset) ResetDoors(); // only the room's own timed reset shuts its doors: a call from another object cannot
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
        timedReset = true;
        try { Reset(); }
        finally { timedReset = false; }
    }

    /// <summary>The reset shuts every door that closes on reset (and locks it again if it has a key). The side that shuts it tells both rooms.</summary>
    private void ResetDoors()
    {
        foreach (var (direction, door) in doors.ToList())
        {
            if (!door.ClosesOnReset) continue;
            bool shut;
            try { DoorBits(door); shut = LoadObject(DoorDaemon).Call<bool>("Reset", door.Key, door.KeyId != ""); }
            catch (MudRuntimeException) { continue; }
            if (!shut) continue;
            TellRoom(Self, $"The {door.Name} {DoorWhere(direction)} swings shut.\n");
            TellPartner(door, "swings shut");
        }
    }

    /// <summary>Tell the room on the other side of a door what has happened to it.</summary>
    private void TellPartner(DoorInfo door, string what)
    {
        try { LoadObject(door.Partner).Call("DoorHeard", Self.ProgramPath, what); }
        catch (MudRuntimeException) { }
    }

    /// <summary>
    /// The room on the other side of a door says what has happened to the door, to this room. Only that room may say it, and it changes nothing
    /// (the state is the register's): it is a line for the people here.
    /// </summary>
    public void DoorHeard(string fromPath, string what)
    {
        var caller = PreviousObject();
        if (caller == null || caller.IsClone || caller.ProgramPath != fromPath || what is not ("swings open" or "is pushed shut" or "swings shut")) return;
        foreach (var (direction, door) in doors)
            if (door.Partner == fromPath) TellRoom(Self, $"The {door.Name} {DoorWhere(direction)} {what}.\n");
    }

    /// <summary>Replace spawned things that are gone: dead creatures, items that left the room.</summary>
    private void Fill(Spawn spawn, bool strict)
    {
        spawn.Handles.RemoveAll(h => !h.IsAlive || (!h.Call<bool>("IsCreature") && Environment(h) != Self));
        // A killed creature whose corpse still lies here still counts: it is not replaced until the corpse is gone.
        var key = "s" + spawns.IndexOf(spawn);
        var corpses = CorpsesHere(key);
        while (spawn.Handles.Count + corpses < spawn.Count)
        {
            try
            {
                var thing = CloneObject(spawn.Path);
                MoveObject(thing, Self);
                if (thing.Call<bool>("IsMonster")) thing.Call("SetSpawnSlot", Self, key);
                spawn.Handles.Add(thing);
            }
            catch (MudRuntimeException) when (!strict)
            {
                return; // a later reset tries again
            }
        }
    }

    /// <summary>How many corpses of creatures from this spawn or encounter of this room lie in it (a player's corpse, a corpse from elsewhere or one carried out does not count).</summary>
    private int CorpsesHere(string key) =>
        AllInventory(Self).Count(o => o.Call<bool>("IsCorpse") && o.Call<ObjectRef>("QuerySpawnRoom") == Self && o.Call<string>("QuerySpawnKey") == key);

    private void MaybeEncounter()
    {
        if (encounters.Count == 0 || Time() - lastEncounter < 60) return;
        if (encounters.Any(e => e.Current is { IsAlive: true })) return;
        foreach (var encounter in encounters)
        {
            if (Random(100) >= encounter.Percent) continue;
            if (CorpsesHere("e" + encounters.IndexOf(encounter)) > 0) continue; // the last one's corpse still lies here
            try
            {
                var creature = CloneObject(encounter.Path);
                MoveObject(creature, Self);
                if (creature.Call<bool>("IsMonster")) creature.Call("SetSpawnSlot", Self, "e" + encounters.IndexOf(encounter));
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
