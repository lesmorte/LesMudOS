// @inherit /std/npc

/// <summary>
/// A creature that walks a fixed route of rooms at a set pace, there and back or in a loop, with arrival and departure lines
/// for those who watch. It walks only through real, ordinary exits (never a hidden or a locked one), does not walk while fighting
/// or for a short while after someone has talked to it, and can be made to defend players. A patrol that dies comes back
/// after ReturnSeconds at the start of its route. See docs/BUILDING.md.
///
/// Cost: a patrol that has no player in any room of its route has no walking call-out at all, only a check every
/// PollSeconds that looks at where the players are (a comparison of a handful of room names per player). It does not
/// load rooms, does not move and does not use a heartbeat. The moment a player stands in a room of the route it walks.
/// </summary>
public class Patrol : Npc
{
    /// <summary>Seconds between steps while it walks.</summary>
    protected int PaceSeconds = 20;
    /// <summary>True: the last room leads on to the first. False: it turns round at each end.</summary>
    protected bool Loop;
    /// <summary>True: if a creature is fighting a player in the room it is in, it joins in against the creature.</summary>
    protected bool DefendPlayers;
    /// <summary>Seconds a dead patrol is away before it returns at the start of its route.</summary>
    protected int ReturnSeconds = 300;
    /// <summary>Seconds it stands still after someone talks to it or asks it something.</summary>
    protected int TalkPauseSeconds = 20;
    /// <summary>Seconds between looks for a player on the route while it is not walking.</summary>
    protected int PollSeconds = 15;
    /// <summary>What the room it arrives in reads. {name} is its short description, {from} where it came from ("the south"; "somewhere" if the way back is not an exit).</summary>
    protected string ArriveLine = "{name} arrives from {from}.";
    /// <summary>What the room it leaves reads. {dir} is the direction of the exit.</summary>
    protected string LeaveLine = "{name} leaves {dir}.";
    /// <summary>What the room reads when it falls.</summary>
    protected string FallLine = "{name} falls.";
    /// <summary>What the room reads when it comes back after dying.</summary>
    protected string ReturnLine = "{name} arrives, back on duty.";

    private readonly List<string> route = new();
    private readonly HashSet<string> routeSet = new();
    private int cursor;
    private bool forward = true;
    private bool away;
    private bool walking;
    private bool armed;
    private int timer;
    private long pausedUntil;
    private int steps;
    private int blocked;

    /// <summary>The rooms of the route, in order, and the pace in seconds. loop: true to go round, false to go there and back. Call it in Create().</summary>
    protected void SetRoute(IEnumerable<string> rooms, int paceSeconds, bool loop = false)
    {
        route.Clear();
        routeSet.Clear();
        foreach (var room in rooms)
        {
            route.Add(room);
            routeSet.Add(room);
        }
        PaceSeconds = Math.Max(1, paceSeconds);
        Loop = loop;
        if (!armed)
        {
            armed = true;
            Schedule(Poll, PollSeconds);
        }
    }

    // ---- Queries ----

    /// <summary>"away", "walking", or "dormant" (no player on the route).</summary>
    public string QueryPatrolState() => away ? "away" : walking ? "walking" : "dormant";

    public int QueryRouteLength() => route.Count;

    public int QueryRouteIndex() => cursor;

    public int QuerySteps() => steps;

    /// <summary>How many times a step found no real exit along the route (or was not allowed).</summary>
    public int QueryBlocked() => blocked;

    public override string Stat() => base.Stat() +
        $"  Patrol: {QueryPatrolState()}, route {route.Count} rooms ({(Loop ? "loop" : "there and back")}), at {cursor}, pace {PaceSeconds}s, steps {steps}, blocked {blocked}, defends players {DefendPlayers}\n";

    // ---- The clock ----

    private void Schedule(Action what, double seconds)
    {
        if (timer != 0) RemoveCallOut(timer);
        timer = CallOut(what, seconds);
    }

    private bool PlayerOnRoute()
    {
        foreach (var user in Users())
            if (Environment(user) is { } room && routeSet.Contains(room.Name)) return true;
        return false;
    }

    /// <summary>Dormant: nothing is scheduled but this look.</summary>
    private void Poll()
    {
        timer = 0;
        if (away || !Self.IsAlive) return;
        if (route.Count > 1 && PlayerOnRoute())
        {
            walking = true;
            Schedule(Step, PaceSeconds); // it does not leave the moment a player arrives: its first step is a pace away
            return;
        }
        walking = false;
        Schedule(Poll, PollSeconds);
    }

    private void Step()
    {
        timer = 0;
        if (away || !Self.IsAlive) return;
        if (!PlayerOnRoute())
        {
            walking = false;
            Schedule(Poll, PollSeconds);
            return;
        }
        Defend();
        if (!QueryInCombat() && TimeMs() >= pausedUntil) MoveNext();
        var wait = Math.Max(PaceSeconds, (pausedUntil - TimeMs()) / 1000.0);
        Schedule(Step, wait);
    }

    // ---- Walking ----

    /// <summary>May this patrol enter this room? A patrol made under /wiz walks only in its own wizard's home; any other never enters /wiz.</summary>
    private bool Allowed(string path)
    {
        var mine = Self.ProgramPath.ToLowerInvariant();
        var there = path.ToLowerInvariant();
        if (!mine.StartsWith("/wiz/")) return !there.StartsWith("/wiz/");
        var parts = mine.Split('/');
        return parts.Length > 3 && there.StartsWith("/wiz/" + parts[2] + "/");
    }

    private void MoveNext()
    {
        var here = Environment();
        if (here == null || route.Count < 2) return;
        if (route[cursor] != here.Name)
        {
            var at = route.IndexOf(here.Name);
            if (at < 0) return; // not on its route: it stays where it is
            cursor = at;
        }
        var next = Next(out var nextForward);
        var path = route[next];
        if (!Allowed(path)) { blocked++; return; }
        string? direction = null;
        foreach (var d in here.Call<List<string>>("QueryExits") ?? new List<string>())
            if (here.Call<string>("QueryExitPath", d) == path && here.Call<bool>("CanUseExit", Self, d)) { direction = d; break; }
        if (direction == null) { blocked++; return; } // no real exit that way: it does not walk through walls
        ObjectRef destination;
        try { destination = LoadObject(path); }
        catch (MudRuntimeException) { blocked++; return; }

        var label = Cap(QueryLabel());
        TellRoom(here, LeaveLine.Replace("{name}", label).Replace("{dir}", direction) + "\n", Self);
        MoveObject(Self, destination);
        cursor = next;
        forward = nextForward;
        steps++;
        TellRoom(destination, ArriveLine.Replace("{name}", label).Replace("{from}", From(destination, here.Name)) + "\n", Self);
        Defend();
    }

    private int Next(out bool nextForward)
    {
        nextForward = forward;
        if (Loop) return (cursor + 1) % route.Count;
        if (forward && cursor >= route.Count - 1) nextForward = false;
        else if (!forward && cursor <= 0) nextForward = true;
        return cursor + (nextForward ? 1 : -1);
    }

    /// <summary>Where the patrol came from, as the people in the new room would say it.</summary>
    private string From(ObjectRef room, string previous)
    {
        foreach (var d in room.Call<List<string>>("QueryExits") ?? new List<string>())
            if (room.Call<string>("QueryExitPath", d) == previous) return d == "up" ? "above" : d == "down" ? "below" : "the " + d;
        return "somewhere";
    }

    // ---- Talking, fighting, dying ----

    protected override void Spoken(ObjectRef who) => pausedUntil = TimeMs() + TalkPauseSeconds * 1000L;

    /// <summary>Called by the room when a creature starts a fight in it.</summary>
    public void JoinFight(ObjectRef attacker, ObjectRef victim)
    {
        if (!DefendPlayers || away) return;
        var attackerIsPlayer = attacker.Call<bool>("IsPlayerCharacter");
        var victimIsPlayer = victim.Call<bool>("IsPlayerCharacter");
        if (attackerIsPlayer == victimIsPlayer) return;
        var creature = attackerIsPlayer ? victim : attacker;
        if (creature != Self) BeginFight(creature, true);
    }

    /// <summary>On arrival and at each step: join against any creature that is fighting a player in this room.</summary>
    private void Defend()
    {
        if (!DefendPlayers || away || Environment() is not { } room) return;
        var inside = AllInventory(room);
        foreach (var creature in inside)
        {
            if (creature == Self || !creature.Call<bool>("IsCreature") || creature.Call<bool>("IsPlayerCharacter") || creature.Call<bool>("IsDead")) continue;
            if (QueryFightingWith(creature)) continue;
            foreach (var player in inside)
                if (player.Call<bool>("IsPlayerCharacter") && creature.Call<bool>("QueryFightingWith", player)) { BeginFight(creature, true); break; }
        }
    }

    /// <summary>A patrol is not destroyed: it leaves the world and comes back after ReturnSeconds at the start of its route.</summary>
    public override void Die(ObjectRef? killer)
    {
        if (away) return;
        away = true;
        walking = false;
        if (Environment() is { } room) TellRoom(room, FallLine.Replace("{name}", Cap(QueryLabel())) + "\n", Self);
        ClearEnemies();
        ResetCombat();
        Hp = QueryMaxHp();
        FillResources();
        MoveObject(LoadObject("/std/void"));
        Schedule(Return, ReturnSeconds);
    }

    private void Return()
    {
        timer = 0;
        if (!Self.IsAlive) return;
        try
        {
            var start = LoadObject(route.Count > 0 ? route[0] : "/std/void");
            if (!Allowed(start.Name)) { Schedule(Return, ReturnSeconds); return; }
            MoveObject(Self, start);
            cursor = 0;
            forward = true;
            away = false;
            TellRoom(start, ReturnLine.Replace("{name}", Cap(QueryLabel())) + "\n", Self);
            Schedule(Poll, PollSeconds);
        }
        catch (MudRuntimeException)
        {
            Schedule(Return, 60); // the start room is not there yet: try again later
        }
    }
}
