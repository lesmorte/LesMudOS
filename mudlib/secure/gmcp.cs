/// <summary>
/// GMCP for the game (docs/specs/gmcp.md): what is sent to a player's client, when, and what it is made of. The driver does the
/// protocol and the JSON; this daemon decides what a player may be told and builds plain data for it. Packages sent:
/// Room.Info, Char.Vitals, Char.Status and Comm.Channel.Text.
///
/// Rules, all enforced here:
///  - Only the player body itself (the poll and look hooks in /std/adventurer), /secure/comm and /secure/channels (a line they
///    have just delivered) may ask for anything to be sent, and a player is sent only data about that player: the body's own
///    state, the room it stands in as that player may see it, and lines that were delivered to that player. A wizard's object
///    that calls these methods is refused (PreviousObject), and the driver refuses GmcpSend to anything outside /std and /secure.
///  - Nothing is sent to a connection that did not ask for GMCP: the driver would refuse, and the daemon forgets the player.
///  - Nothing is built from text a player typed except channel lines, which go to the driver as plain strings.
///  - Char.Vitals is coalesced: at most one per 250 ms for a player; a change that comes sooner is sent when the gap is over.
/// Blueprint only. Nothing here is saved (the room numbers are /secure/roomids's).
/// </summary>
public class GmcpDaemon : MudObject
{
    private const string RoomIds = "/secure/roomids";
    private const string PlayerProgram = "/std/player";
    private const long MinVitalsGapMs = 250;
    private const int MaxExits = 32;
    private const int MaxLine = 600;

    private sealed class State
    {
        public string Room = "";
        public string Vitals = "";
        public string Status = "";
        public long VitalsAt;
        public bool Pending;
    }

    private readonly Dictionary<ObjectRef, State> states = new();

    // ------------------------------------------------------------------
    // Who may ask
    // ------------------------------------------------------------------

    /// <summary>The call comes from the player's own body (its hooks), about that same player.</summary>
    private bool FromBody(ObjectRef player)
    {
        var caller = PreviousObject();
        return !Self.IsClone && caller != null && caller == player && player.IsAlive && player.IsClone && player.ProgramPath == PlayerProgram;
    }

    private bool FromComm()
    {
        var caller = PreviousObject();
        return !Self.IsClone && caller != null && caller.IsAlive && !caller.IsClone && (caller.ProgramPath == "/secure/comm" || caller.ProgramPath == "/secure/channels");
    }

    // ------------------------------------------------------------------
    // The hooks
    // ------------------------------------------------------------------

    /// <summary>The player's body asks, after a command and at each heart beat, whether anything changed. Sends what did.</summary>
    public void Poll(ObjectRef player)
    {
        if (!FromBody(player)) return;
        Refresh(player, false);
    }

    /// <summary>The player has looked at the room (or has just entered it): Room.Info is sent whether or not it changed.</summary>
    public void Look(ObjectRef player)
    {
        if (!FromBody(player)) return;
        Refresh(player, true);
    }

    /// <summary>A tell, shout or channel line has just been delivered to this player. The talker and text are as the player saw them.</summary>
    public void Channel(ObjectRef player, string channel, string talker, string text)
    {
        if (!FromComm() || player == null || !player.IsAlive || !GmcpActive(player)) return;
        if (channel == null || talker == null || text == null) return;
        GmcpSend(player, "Comm.Channel.Text", new Dictionary<string, object?>
        {
            ["channel"] = Cut(channel, 32), ["talker"] = Cut(talker, 40), ["text"] = Cut(text, MaxLine),
        });
    }

    private static string Cut(string text, int length) => text.Length <= length ? text : text.Substring(0, length);

    // ------------------------------------------------------------------
    // What to send
    // ------------------------------------------------------------------

    private void Refresh(ObjectRef player, bool look)
    {
        if (!GmcpActive(player)) { states.Remove(player); return; }
        if (states.Count > 64) foreach (var dead in states.Keys.Where(k => !k.IsAlive).ToList()) states.Remove(dead);
        if (!states.TryGetValue(player, out var st)) states[player] = st = new State();

        var facts = player.Call<Dictionary<string, object?>>("GmcpFacts");
        if (facts != null)
        {
            SendStatus(player, st, facts);
            SendVitals(player, st, facts);
        }
        SendRoom(player, st, look);
    }

    private static int Int(Dictionary<string, object?> facts, string key) => facts.TryGetValue(key, out var v) && v is int n ? n : 0;
    private static bool Flag(Dictionary<string, object?> facts, string key) => facts.TryGetValue(key, out var v) && v is bool b && b;
    private static string Str(Dictionary<string, object?> facts, string key) => facts.TryGetValue(key, out var v) && v is string s ? s : "";

    private void SendStatus(ObjectRef player, State st, Dictionary<string, object?> facts)
    {
        var status = new Dictionary<string, object?>
        {
            ["name"] = Str(facts, "name"), ["calling"] = Str(facts, "calling"), ["level"] = Int(facts, "level"),
            ["experience"] = Int(facts, "experience"), ["next_level"] = Int(facts, "next_level"), ["gold"] = Int(facts, "gold"),
            ["points_to_place"] = Int(facts, "points_to_place"),
        };
        var key = string.Join("|", status.Values);
        if (key == st.Status) return;
        st.Status = key;
        GmcpSend(player, "Char.Status", status);
    }

    private void SendVitals(ObjectRef player, State st, Dictionary<string, object?> facts)
    {
        var vitality = Int(facts, "vitality"); var maxVitality = Int(facts, "maxvitality");
        var endurance = Int(facts, "endurance"); var maxEndurance = Int(facts, "maxendurance");
        var focus = Int(facts, "focus"); var maxFocus = Int(facts, "maxfocus");
        var vitals = new Dictionary<string, object?>
        {
            ["vitality"] = vitality, ["maxvitality"] = maxVitality,
            ["endurance"] = endurance, ["maxendurance"] = maxEndurance,
            ["focus"] = focus, ["maxfocus"] = maxFocus,
            ["resting"] = Flag(facts, "resting"), ["sneaking"] = Flag(facts, "sneaking"), ["fighting"] = Flag(facts, "fighting"),
            // The names stock Mudlet gauges and most clients know.
            ["hp"] = vitality, ["maxhp"] = maxVitality, ["mp"] = focus, ["maxmp"] = maxFocus,
        };
        var key = string.Join("|", vitals.Values);
        if (key == st.Vitals) return;
        var now = TimeMs();
        var wait = st.VitalsAt + MinVitalsGapMs - now;
        if (wait > 0)
        {
            // Too soon after the last one: send what is true when the gap is over, once.
            if (!st.Pending)
            {
                st.Pending = true;
                CallOut(() => Later(player), wait / 1000.0 + 0.01);
            }
            return;
        }
        st.Vitals = key;
        st.VitalsAt = now;
        GmcpSend(player, "Char.Vitals", vitals);
    }

    private void Later(ObjectRef player)
    {
        if (!states.TryGetValue(player, out var st)) return;
        st.Pending = false;
        if (player.IsAlive) Refresh(player, false);
        else states.Remove(player);
    }

    private static string Words(string name) => name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);

    private void SendRoom(ObjectRef player, State st, bool look)
    {
        var room = Environment(player);
        if (room == null) return;
        var facts = room.Call<Dictionary<string, object?>>("GmcpRoomFacts", player);
        if (facts == null) return;
        var path = room.IsClone ? room.Name : room.ProgramPath;
        var ids = LoadObject(RoomIds);
        var number = ids.Call<int>("Assign", path);
        if (number <= 0) return;

        var area = Str(facts, "area");
        if (path.StartsWith("/wiz/", StringComparison.Ordinal))
        {
            var owner = path.Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(1).FirstOrDefault() ?? "";
            area = "Workshop of " + Words(owner);
        }
        if (area == "") area = "Elsewhere";
        var environment = Str(facts, "environment");
        if (environment is not ("outdoors" or "indoors" or "underground" or "road" or "water")) environment = "outdoors";

        var exits = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        var doors = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        var ways = facts.TryGetValue("exits", out var e) ? e as Dictionary<string, string> : null;
        var states2 = facts.TryGetValue("doors", out var d) ? d as Dictionary<string, string> : null;
        if (ways != null)
        {
            foreach (var way in ways)
            {
                if (exits.Count >= MaxExits) break;
                if (way.Key == null || way.Key.Length == 0 || way.Key.Length > 32 || way.Value == null) continue;
                var target = ids.Call<int>("Assign", way.Value);
                if (target <= 0) continue;
                exits[way.Key] = target;
                if (states2 != null && states2.TryGetValue(way.Key, out var state) && state is "open" or "closed" or "locked") doors[way.Key] = state;
            }
        }

        var info = new Dictionary<string, object?>
        {
            ["num"] = number, ["name"] = Cut(Str(facts, "name"), 120), ["area"] = Cut(area, 80), ["environment"] = environment,
            ["exits"] = exits, ["doors"] = doors,
        };
        var key = number + "|" + info["name"] + "|" + area + "|" + environment + "|" + string.Join(",", exits.Select(x => x.Key + "=" + x.Value)) + "|" + string.Join(",", doors.Select(x => x.Key + "=" + x.Value));
        if (!look && key == st.Room) return;
        st.Room = key;
        GmcpSend(player, "Room.Info", info);
    }
}
