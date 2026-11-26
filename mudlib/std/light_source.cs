// @inherit /std/item

/// <summary>
/// A torch or a lantern you carry (docs/specs/light.md, section 3). Set Name, Ids, ShortDesc, LongDesc and Value, and in Create(): LightValue (+1 torch,
/// +2 lantern), Capacity (seconds of burning when new), WarnAt (seconds left when it warns), Noun ("torch" or "lantern", for the messages and for
/// `light torch`), and Lantern = true for one that is refilled with lamp oil and stays when empty (a torch that burns out is destroyed).
///
/// Only a lit light gives light (to the room it lies in, or the room of the creature who carries it; a light in a container lights nothing). The
/// character's commands light, extinguish and fill live in /std/adventurer. Burn time runs only while lit, and is kept in the item's own
/// property ("burn" for a torch, "oil" for a lantern: seconds left; none means a full one), written when the light goes out and when the
/// character is saved. A lit light is never saved lit: it goes out when its carrier leaves the game.
/// </summary>
public class LightSource : Item
{
    protected int LightValue = 1;
    /// <summary>Seconds of burning when new.</summary>
    protected int Capacity = 600;
    /// <summary>It warns once when this many seconds are left.</summary>
    protected int WarnAt = 30;
    protected string Noun = "torch";
    /// <summary>A lantern: refilled with lamp oil, and stays when it is empty.</summary>
    protected bool Lantern;

    private const string Daemon = "/secure/light";
    private const int MaxLightStep = 60;

    private bool lit;
    private bool loaded;
    private bool warned;
    private double left;      // seconds left, as of litFrom when lit
    private long litAt;
    private int timer;

    public bool IsLightSource() => true;

    public bool QueryLit() => lit;

    public int QueryLightValue() => lit ? LightValue : 0;

    public string QueryLightWord() => Noun;

    public bool QueryRefillable() => Lantern;

    /// <summary>Can it be lit now (does it have anything left)?</summary>
    public bool QueryUsable() => Remaining() > 0.5;

    public string QueryUseVerb() => "light";

    public double QueryRecovery() => 0.5;

    private string KeyName => Lantern ? "oil" : "burn";

    private double Remaining()
    {
        if (!loaded)
        {
            loaded = true;
            left = int.TryParse(QueryProperty(KeyName), out var n) ? Math.Clamp(n, 0, Capacity) : Capacity;
        }
        if (!lit) return left;
        var scale = LoadObject(Daemon).Call<int>("QueryBurnScale");
        return Math.Max(0, left - (TimeMs() - litAt) / 1000.0 * Math.Max(1, scale));
    }

    /// <summary>Seconds of burning left, whole.</summary>
    public int QueryRemaining() => (int)Math.Ceiling(Remaining() - 1e-9);

    public int QueryCapacity() => Capacity;

    /// <summary>Is a refillable light already full?</summary>
    public bool QueryFull() => Remaining() >= Capacity - 0.5;

    public override string? Short() => base.Short() + (lit ? " (lit)" : "");

    private static readonly string[] Units = { "no", "five", "ten", "fifteen", "twenty", "twenty-five", "thirty", "thirty-five", "forty", "forty-five", "fifty", "fifty-five", "sixty" };

    /// <summary>"about twenty minutes of burning": to the nearest five minutes; "nearly out" under two and a half.</summary>
    public string QueryTimeLeft()
    {
        var rest = Remaining();
        if (rest <= 0.5) return Lantern ? "no oil left" : "nothing left to burn";
        if (rest < 150) return "nearly out";
        var five = (int)Math.Round(rest / 300.0, MidpointRounding.AwayFromZero);
        return $"about {Units[Math.Min(five, Units.Length - 1)]} minutes of {(Lantern ? "oil" : "burning")}";
    }

    public override string Long() => base.Long().TrimEnd('\n') + (lit ? " It is burning." : "") + $" It has {QueryTimeLeft()}.\n";

    // ---- Lighting ----

    /// <summary>The light command, as a combat action's use: lights the light the character carries. Only the carrier's own action may.</summary>
    public void Consume(ObjectRef user)
    {
        if (Environment() != user) return;
        var caller = PreviousObject();
        if (caller != user) return;
        if (lit) { TellObject(user, "It is already burning.\n"); return; }
        if (Remaining() <= 0.5) { TellObject(user, $"The {Name.ToLowerInvariant()} has no oil left.\n"); return; }
        left = Remaining();
        warned = left <= WarnAt;
        lit = true;
        litAt = TimeMs();
        LoadObject(Daemon).Call("AddSource", Self);
        Arm();
        TellObject(user, $"You light {Plain()}. It flares, then settles to a steady flame.\n");
        if (Environment(user) is { } room) TellRoom(room, $"{user.Call<string>("QueryName") ?? "Someone"} lights {Plain(true)}.\n", user);
    }

    private string Plain(bool indefinite = false)
    {
        var single = ShortDesc ?? Name.ToLowerInvariant();
        if (indefinite) return single;
        return single.StartsWith("a ") || single.StartsWith("an ") ? "the " + single.Substring(single.IndexOf(' ') + 1) : single;
    }

    /// <summary>Put it out; it keeps the time it has left. The carrier's command (or the light's own end). how: what the carrier reads.</summary>
    public bool Douse(ObjectRef by)
    {
        var caller = PreviousObject();
        if (!lit || by == null || caller != by || Environment() != by) return false;
        PutOut();
        TellObject(by, $"You {(Lantern ? "turn down" : "pinch out")} {Plain()}.\n");
        if (Environment(by) is { } room) TellRoom(room, $"{by.Call<string>("QueryName") ?? "Someone"} puts out {Plain(true)}.\n", by);
        return true;
    }

    private void PutOut()
    {
        var room = RoomHere();
        left = Remaining();
        lit = false;
        if (timer != 0) { RemoveCallOut(timer); timer = 0; }
        Write2Property();
        try { LoadObject(Daemon).Call("DropSource", Self, room); }
        catch (MudRuntimeException) { }
    }

    private ObjectRef? RoomHere()
    {
        var env = Environment();
        if (env == null) return null;
        if (env.Call<bool>("IsRoom")) return env;
        return Living(env) ? Environment(env) : null;
    }

    // ---- The clock ----

    private void Arm()
    {
        if (timer != 0) RemoveCallOut(timer);
        var scale = Math.Max(1, LoadObject(Daemon).Call<int>("QueryBurnScale"));
        var rest = Remaining();
        var toNext = warned ? rest : Math.Min(rest, Math.Max(0.0, rest - WarnAt));
        if (!warned && rest - WarnAt <= 0) toNext = rest;
        var real = Math.Max(0.2, Math.Min(MaxLightStep, toNext / scale + 0.05));
        timer = CallOut(Tick, real);
    }

    private void Tick()
    {
        timer = 0;
        if (!lit) return;
        var rest = Remaining();
        if (rest <= 0.001) { BurnOut(); return; }
        if (!warned && rest <= WarnAt + 0.001)
        {
            warned = true;
            var holder = Environment();
            if (holder != null && Living(holder)) TellObject(holder, Lantern ? $"Your lantern's flame sinks; it is nearly out of oil.\n" : $"Your {Name.ToLowerInvariant()} burns low.\n");
        }
        Arm();
    }

    private void BurnOut()
    {
        var room = RoomHere();
        var holder = Environment();
        left = 0;
        lit = false;
        Write2Property();
        try { LoadObject(Daemon).Call("DropSource", Self, room); }
        catch (MudRuntimeException) { }
        if (holder != null && Living(holder))
        {
            TellObject(holder, Lantern ? "Your lantern gutters and dies.\n" : $"Your {Name.ToLowerInvariant()} gutters and dies. What is left of it crumbles.\n");
            if (room != null) TellRoom(room, $"{holder.Call<string>("QueryName") ?? "Someone"}'s {Noun} gutters and dies.\n", holder);
        }
        else if (room != null) TellRoom(room, $"The {Noun} on the floor gutters and dies.\n");
        if (!Lantern) Destruct();
    }

    // ---- Moving, water, containers ----

    public override void OnMoved(ObjectRef? from)
    {
        if (!lit) return;
        var env = Environment();
        var daemon = LoadObject(Daemon);
        var before = from == null ? null : from.Call<bool>("IsRoom") ? from : Living(from) ? Environment(from) : null;
        if (env != null && env.Call<bool>("IsRoom") && env.Call<bool>("QueryWet"))
        {
            PutOut();
            TellRoom(env, $"The {Noun} hisses out in the water.\n");
            daemon.Call("RoomChanged", env);
            daemon.Call("RoomChanged", before);
            return;
        }
        if (env == null || (!env.Call<bool>("IsRoom") && !Living(env)))
        {
            // Into a corpse or any container: it lights nothing, and is put out (keeping its time).
            PutOut();
            daemon.Call("RoomChanged", before);
            return;
        }
        daemon.Call("RoomChanged", before);
        daemon.Call("RoomChanged", RoomHere());
    }

    public override void OnDestruct()
    {
        if (!lit) return;
        var room = RoomHere();
        lit = false;
        try { LoadObject(Daemon).Call("DropSource", Self, room); }
        catch (MudRuntimeException) { }
    }

    // ---- Fuel ----

    /// <summary>Pour a flask of lamp oil into the lantern. Only a character's own fill command may (the caller is the character's body).</summary>
    public string Refill(ObjectRef user, ObjectRef flask)
    {
        var caller = PreviousObject();
        if (!Lantern || caller == null || caller != user || Environment() != user || !flask.IsAlive || Environment(flask) != user || !flask.Call<bool>("IsLampOil")) return "";
        var rest = Remaining();
        if (rest >= Capacity - 0.5) return "The lantern is full.";
        var add = Math.Max(0, flask.Call<int>("QueryOilSeconds"));
        if (!flask.Call<bool>("SpendOne")) return "";
        left = Math.Min(Capacity, rest + add);
        if (lit) { litAt = TimeMs(); warned = left <= WarnAt; Arm(); }
        Write2Property();
        return "You pour the oil into the lantern, and the reservoir fills.";
    }

    // ---- Saving ----

    /// <summary>The character is being saved: the time left goes into the item's own property (none for a full one). A lit light stays lit (it is out at the next login). Only a trusted caller.</summary>
    public void SyncLight()
    {
        var caller = PreviousObject();
        if (caller == null || !(caller.ProgramPath.StartsWith("/std/") || caller.ProgramPath.StartsWith("/secure/"))) return;
        Write2Property(true);
    }

    private void Write2Property(bool force = false)
    {
        var rest = (int)Math.Ceiling(Remaining() - 1e-9);
        try
        {
            if (rest >= Capacity) SetProperty(KeyName, null);
            else SetProperty(KeyName, rest.ToString());
        }
        catch (MudRuntimeException) { }
    }

    public override string Stat() => base.Stat() + $"  Light {LightValue}, lit {lit}, {QueryRemaining()} s of {Capacity}{(Lantern ? " (lantern)" : "")}\n";
}
