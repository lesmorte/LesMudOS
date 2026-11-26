/// <summary>
/// Light and darkness (docs/specs/light.md): the numbers, the rules and the register of lit things. The light of a room is an integer,
/// clamp(base + the light of every lit source in it, -3, +3): 1 or more is lit, 0 is dim, -1 or less is dark. This daemon keeps the rules
/// as named constants and pure functions (the to-hit penalty, the sneaking adjustment, the search modifier), and the short list of the
/// things that are lit at this moment (a handful at any time), and answers "what is the light here?".
///
/// Rules, all enforced here:
///  - Only an item can put itself in the register (the caller is the item), only from its own lighting code, and only a creature's Glow
///    can put a creature in (the caller is the Glow spell). Nothing is ever taken out of the register by anything but itself or the sweep.
///  - A source counts in a room only if it is in the same domain as the room (a wizard's workshop is its own world: a sandbox lamp lights
///    the workshop, never a real room, and a real lamp never lights the workshop); and in a real room only a source from a trusted tree.
///  - Every source and every total is clamped, and so is a room's own base, so nothing can make daylight or a pit.
///  - The state-changing functions are named for what they do (AddSource, DropSource, RoomChanged...), never Query/Is/Has/Can.
/// Blueprint only. Nothing here is saved.
/// </summary>
public class LightDaemon : MudObject
{
    // ---- The scale ----
    public const int StateLit = 1, StateDim = 0, StateDark = -1;
    /// <summary>The limits of one source, of a room's base and of the sum.</summary>
    public const int SourceMin = -3, SourceMax = 3, TotalMin = -3, TotalMax = 3;
    /// <summary>What one creature's Glow can add at most.</summary>
    public const int GlowMax = 2;

    // ---- To hit (section 5): a point is 5% on the d20 ----
    public const int DimMelee = -1, DimRanged = -2, DarkMelee = -3, DarkRanged = -6;

    // ---- Sneaking (section 8): added to the notice chance, in percent ----
    public const int NoticeDim = -10, NoticeDarkSeeing = -15, NoticeDarkBlind = -25, NoticeOwnLight = 20;

    // ---- Search (docs/specs/search.md): points off the search check, a number of points of difficulty ----
    public const int SearchDim = -2, SearchDark = -5;

    // ---- Harvest and craft (section 5) ----
    public const int HarvestDimPenalty = 5;
    public const double CraftDimTierShift = 0.25;

    /// <summary>Seconds between the sweeps that heal anything the hooks missed.</summary>
    public const int SweepSeconds = 10;

    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    private readonly List<ObjectRef> sources = new();
    private readonly Dictionary<ObjectRef, long> glowers = new();
    private readonly Dictionary<ObjectRef, int> known = new();
    private bool sweeping;
    private int burnScale = 1;

    // ------------------------------------------------------------------
    // Pure rules
    // ------------------------------------------------------------------

    /// <summary>The state a level means: 1 lit, 0 dim, -1 dark.</summary>
    public static int StateOf(int level) => level >= 1 ? StateLit : level == 0 ? StateDim : StateDark;

    public string StateName(int level) => level >= 1 ? "lit" : level == 0 ? "dim" : "dark";

    /// <summary>The to-hit modifier for an attack in a room of this light. ranged: a bow is wielded. sees: the attacker sees in the dark (or the room is lit).</summary>
    public int ToHitPenalty(int level, bool ranged, bool sees)
    {
        if (level >= 1 || sees && level < 0) return 0;
        if (level == 0) return ranged ? DimRanged : DimMelee;
        return ranged ? DarkRanged : DarkMelee;
    }

    /// <summary>
    /// The chance adjustment for the notice roll of a creature against a sneaker, in percent. state: the room's light level with the sneaker's own
    /// light counted; darkSight: the creature sees in the dark; ownLight: the sneaker carries a lit light.
    /// </summary>
    public int NoticeAdjustFor(int level, bool darkSight, bool ownLight)
    {
        var adjust = level >= 1 ? 0 : level == 0 ? NoticeDim : darkSight ? NoticeDarkSeeing : NoticeDarkBlind;
        return adjust + (ownLight ? NoticeOwnLight : 0);
    }

    /// <summary>
    /// The modifier of the room's light to a search check, in points of the check (negative: harder): dim -2, dark -5, lit 0. A thing found by feel
    /// (a draught, a smell, a loose stone) is not hindered: byFeel true gives 0.
    /// TODO(search): not yet hooked into the search check (docs/specs/search.md); the hook is Room.SearchModifier(searcher), see Room.LightSearchModifier.
    /// </summary>
    public int SearchModifier(int level, bool byFeel) => byFeel || level >= 1 ? 0 : level == 0 ? SearchDim : SearchDark;

    // ------------------------------------------------------------------
    // Asking
    // ------------------------------------------------------------------

    private static bool Trusted(ObjectRef? ob)
    {
        if (ob == null || ob.Domain != "" || ob.ProgramPath.Contains("..")) return false;
        foreach (var tree in TrustedTrees) if (ob.ProgramPath.StartsWith(tree, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>The room an object is in, walking up: the room itself, or the room of the creature that carries it; null inside a container.</summary>
    private ObjectRef? RoomOf(ObjectRef? ob)
    {
        if (ob == null || !ob.IsAlive) return null;
        var env = Environment(ob);
        if (env == null) return ob.Call<bool>("IsRoom") ? ob : null;
        if (env.Call<bool>("IsRoom")) return env;
        if (Living(env)) return Environment(env) is { } room && room.Call<bool>("IsRoom") ? room : null; // carried
        return null; // in a container (a corpse, a bag)
    }

    private int BaseOf(ObjectRef room) => Math.Clamp(room.Call<int>("QueryBaseLight"), TotalMin, TotalMax);

    private int Compute(ObjectRef room)
    {
        var total = BaseOf(room);
        for (var i = sources.Count - 1; i >= 0; i--)
        {
            var src = sources[i];
            if (!src.IsAlive) { sources.RemoveAt(i); continue; }
            if (!src.Call<bool>("QueryLit") || RoomOf(src) != room || !MayLight(src, room)) continue;
            total += Math.Clamp(src.Call<int>("QueryLightValue"), SourceMin, SourceMax);
        }
        foreach (var who in glowers.Keys.ToList())
        {
            if (!who.IsAlive) { glowers.Remove(who); continue; }
            if (RoomOf(who) != room || !MayLight(who, room)) continue;
            total += Math.Clamp((int)Math.Round(who.Call<double>("QueryMod", "light")), 0, GlowMax);
        }
        return Math.Clamp(total, TotalMin, TotalMax);
    }

    /// <summary>The domain wall: a source lights only the room of its own domain, and in the real game only if it is trusted code.</summary>
    private static bool MayLight(ObjectRef source, ObjectRef room) => source.Domain == room.Domain && (room.Domain != "" || Trusted(source));

    /// <summary>The light of a room now, -3 to +3. Never throws: a thing that is not a room has the default light of 1.</summary>
    public int QueryLevel(ObjectRef room)
    {
        if (room == null || !room.IsAlive || !room.Call<bool>("IsRoom")) return StateLit;
        return Compute(room);
    }

    /// <summary>The state of a room as a character in it sees it: "lit", "dim" or "dark".</summary>
    public string QueryState(ObjectRef room) => StateName(QueryLevel(room));

    /// <summary>The state as a number: 1, 0 or -1.</summary>
    public int QueryStateNumber(ObjectRef room) => StateOf(QueryLevel(room));

    /// <summary>Is any lit light carried by this creature, or Glow on it? (A sneaker with a light is a beacon.)</summary>
    public bool QueryCarriesLight(ObjectRef who)
    {
        if (who == null || !who.IsAlive) return false;
        if (glowers.ContainsKey(who) && who.Call<double>("QueryMod", "light") > 0) return true;
        foreach (var src in sources)
            if (src.IsAlive && Environment(src) == who && src.Call<bool>("QueryLit") && src.Call<int>("QueryLightValue") > 0 && MayLight(src, Environment(who) ?? who)) return true;
        return false;
    }

    /// <summary>The adjustment to the chance that this creature notices this sneaker, in percent (section 8): the room's state with the sneaker's own light, and the beacon.</summary>
    public int NoticeAdjust(ObjectRef room, ObjectRef sneaker, ObjectRef creature)
    {
        if (room == null || creature == null) return 0;
        var sees = creature.Call<bool>("QueryDarkSight");
        return NoticeAdjustFor(QueryLevel(room), sees, QueryCarriesLight(sneaker));
    }

    // ------------------------------------------------------------------
    // The register
    // ------------------------------------------------------------------

    /// <summary>A light has been lit (or a wall light put up). Only the item itself, from /std or a domain tree, may ask; it is counted at once.</summary>
    public void AddSource(ObjectRef source)
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller != source || !source.IsAlive || !source.Call<bool>("IsLightSource")) return;
        var room = RoomOf(source);
        if (room != null) Touch(room);
        if (!sources.Contains(source)) sources.Add(source);
        if (room != null) Settle(room);
        Arm();
    }

    /// <summary>A light has gone out, burnt out, been destroyed or put in a container. Only the item itself may ask (its own state is its own).</summary>
    public void DropSource(ObjectRef source, ObjectRef? room)
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller != source) return;
        if (room != null) Touch(room);
        sources.Remove(source);
        if (room != null) Settle(room);
    }

    /// <summary>The caller (a Glow spell) has put a Glow on a creature that lasts this many seconds.</summary>
    public void NoteGlow(ObjectRef who, int seconds)
    {
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || !Trusted(caller) || caller.ProgramPath != "/skills/glow" || who == null || !who.IsAlive || seconds <= 0 || seconds > 3600) return;
        var room = RoomOf(who);
        if (room != null) Touch(room);
        glowers[who] = TimeMs() + seconds * 1000L;
        if (room != null) Settle(room);
        CallOut(() => GlowEnds(who), seconds + 0.2);
        Arm();
    }

    private void GlowEnds(ObjectRef who)
    {
        if (!who.IsAlive) { glowers.Remove(who); return; }
        if (glowers.TryGetValue(who, out var until) && until > TimeMs()) return; // recast: a later end is already waiting
        var room = RoomOf(who);
        glowers.Remove(who);
        if (room != null) { Touch(room); Settle(room); }
    }

    /// <summary>Remember the state of a room before something changes it, so that the change can be told.</summary>
    public void Touch(ObjectRef room)
    {
        if (room == null || !room.IsAlive || known.Count > 512 && !known.ContainsKey(room)) return;
        if (!room.Call<bool>("IsRoom")) return;
        if (!known.ContainsKey(room)) known[room] = Compute(room);
    }

    /// <summary>Something that may have changed the light of these rooms has moved (a creature through an exit, a pick-up or a drop, a death). Recomputes and tells a change of state.</summary>
    public void RoomChanged(ObjectRef? room)
    {
        if (room == null || !room.IsAlive) return;
        Settle(room);
    }

    private void Settle(ObjectRef room)
    {
        if (!room.Call<bool>("IsRoom")) return;
        var now = Compute(room);
        if (!known.TryGetValue(room, out var before)) { known[room] = now; return; }
        known[room] = now;
        var was = StateOf(before);
        var is_ = StateOf(now);
        if (was == is_) return;
        var line = (was, is_) switch
        {
            (StateDark, StateDim) => "A dim glow lets you make out the shapes of things.",
            (StateDim, StateLit) => "The light grows until the room is plain to see.",
            (StateDark, StateLit) => "Light fills the room, and the dark is gone.",
            (StateLit, StateDim) => "The light dwindles; it is dim here.",
            (StateDim, StateDark) => "The dark closes in.",
            _ => "The light fails, and the dark is complete.",
        };
        try { TellRoom(room, line + "\n"); }
        catch (MudRuntimeException) { }
    }

    private void Arm()
    {
        if (sweeping || Self.IsClone) return;
        sweeping = true;
        CallOut(Sweep, SweepSeconds);
    }

    /// <summary>Every few seconds, while anything is lit, the rooms that have a source are looked at again: it heals anything a hook missed.</summary>
    private void Sweep()
    {
        sweeping = false;
        var rooms = new List<ObjectRef>();
        foreach (var src in sources.ToList())
        {
            if (!src.IsAlive) { sources.Remove(src); continue; }
            if (RoomOf(src) is { } r && !rooms.Contains(r)) rooms.Add(r);
        }
        foreach (var who in glowers.Keys.ToList())
        {
            if (!who.IsAlive || glowers[who] < TimeMs()) { glowers.Remove(who); continue; }
            if (RoomOf(who) is { } r && !rooms.Contains(r)) rooms.Add(r);
        }
        foreach (var room in rooms) Settle(room);
        // Forget rooms nobody is lit in once they are settled and the table is long.
        if (known.Count > 256) foreach (var room in known.Keys.Where(k => !k.IsAlive || !rooms.Contains(k)).ToList()) known.Remove(room);
        if (sources.Count > 0 || glowers.Count > 0) Arm();
    }

    // ------------------------------------------------------------------
    // The burn clock (a test may speed it up)
    // ------------------------------------------------------------------

    /// <summary>How many times faster than real time lights burn (1 normally).</summary>
    public int QueryBurnScale() => burnScale;

    /// <summary>For tests: burn n times faster (1 to 1000). Only trusted code acting for a wizard of level 10 may; a wizard's own object cannot.</summary>
    public bool SetTestBurnScale(int n)
    {
        var who = ThisPlayer();
        var caller = PreviousObject();
        if (Self.IsClone || caller == null || caller.Domain != "" || !Trusted(caller) || who == null || who.Call<int>("QueryWizLevel") < 10) return false;
        if (n < 1 || n > 1000) return false;
        burnScale = n;
        return true;
    }
}
