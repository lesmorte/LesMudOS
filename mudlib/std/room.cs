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

    private sealed class Hidden
    {
        public string Path = "";
        public List<string> Hints = new();
        public int Chance;
        public string FoundLine = "";
    }

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

    public virtual string? QueryDetail(string name) => details.GetValueOrDefault(Names(name).FirstOrDefault() ?? "");

    public virtual string? QueryReadable(string name) => readables.GetValueOrDefault(Names(name).FirstOrDefault() ?? "");

    /// <summary>May this creature use the exit right now (is it not locked against them)?</summary>
    public bool CanUseExit(ObjectRef who, string direction)
    {
        if (!Exits.ContainsKey(direction) && !(hidden.ContainsKey(direction) && Found(who, direction))) return false;
        return !locks.TryGetValue(direction, out var door) || (door.KeyId != "" && Present(door.KeyId, who) != null);
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
        text.Append(ways.Count == 0 ? "There are no obvious exits.\n" : "Obvious exits: " + string.Join(", ", ways.Select(d => "{cmd}" + d + "{/}" + (wizard && hidden.ContainsKey(d) && !Exits.ContainsKey(d) ? " (hidden)" : "") + (detailed ? ExitNote(d, viewer!) : ""))) + "\n");

        // Identical shorts are merged: "Two giant rats are here." Things lying about (not creatures) are listed the same way as
        // everywhere else: "A minor healing potion (x2) is here."
        var counts = new Dictionary<string, int>();
        var order = new List<string>();
        var floor = new Dictionary<string, (string single, int n)>();
        foreach (var ob in AllInventory())
        {
            if (ob == viewer) continue;
            var desc = ob.Call<string>("Short");
            if (desc == null) continue;
            if (!Living(ob) && ob.Call<int>("QueryQuantity") > 0)
            {
                var single = ob.Call<int>("QueryAmount") > 0 ? desc : ob.Call<string>("DescribeCount", 1) ?? desc;
                var state = string.Join("&", (ob.Call<string>("QueryState") ?? "").Split('&', StringSplitOptions.RemoveEmptyEntries).Where(p => !p.StartsWith("n=")));
                var key = single + "|" + state;
                if (!floor.TryGetValue(key, out var g)) { g = (single, 0); order.Add("\u0001" + key); }
                floor[key] = (g.single, g.n + Math.Max(1, ob.Call<int>("QueryQuantity")));
                continue;
            }
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
