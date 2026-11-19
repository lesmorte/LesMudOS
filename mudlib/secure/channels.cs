// @use /secure/comm

/// <summary>
/// The channel service. A channel is one data file in /channels (id, title, who belongs, who may speak, whether members may
/// switch it off or join and leave it). This daemon reads them, decides membership and delivers messages.
/// docs/specs/communication.md.
///
/// Membership is decided here, from the character's real wizard level in /secure/wizlist, at every send and every delivery;
/// it is never read from anything the player holds and never granted by asking, except for a channel whose file allows
/// joining. Every method that acts for a player checks that PreviousObject() is that player's own body. A character who does
/// not belong to a channel gets the same answer as for a channel that does not exist.
///
/// Blueprint only.
/// </summary>
public class Channels : MudObject
{
    private const string Dir = "/channels";
    private const string WizListFile = "/secure/wizlist";
    private const string PlayerProgram = "/std/player";

    private sealed class Def
    {
        public string Id = "", Title = "";
        public int MemberLevel;   // wizard level needed to belong
        public int SpeakLevel;    // wizard level needed to speak
        public bool CanMute, CanJoin;
    }

    /// <summary>Lower-case character name to the time (ms) of the last channel message.</summary>
    private readonly Dictionary<string, long> lastSent = new();
    private readonly List<string> errors = new();

    // ------------------------------------------------------------------
    // The catalogue
    // ------------------------------------------------------------------

    private static bool ValidId(string id) => id.Length > 0 && id.Length <= 16 && id.All(c => c >= 'a' && c <= 'z');

    private int WizLevel(string name) => LoadObject(WizListFile).Call<int>("QueryLevel", name);

    private static string Esc(string text) => text.Replace("{", "{{");

    private static bool Yes(string v) => v.Trim().ToLowerInvariant() is "yes" or "true" or "1";

    private Def? Parse(string source, string id)
    {
        var d = new Def();
        var bad = false;
        foreach (var raw in source.Split('\n'))
        {
            var line = raw.Trim();
            if (line == "" || line.StartsWith("#")) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) { errors.Add($"{id}: bad line '{line}'"); bad = true; continue; }
            var k = line.Substring(0, eq).Trim().ToLowerInvariant();
            var v = line.Substring(eq + 1).Trim();
            switch (k)
            {
                case "id": d.Id = v; break;
                case "title": d.Title = v; break;
                case "member_level": if (!int.TryParse(v, out d.MemberLevel) || d.MemberLevel < 0 || d.MemberLevel > 10) { errors.Add($"{id}: bad member_level"); bad = true; } break;
                case "speak_level": if (!int.TryParse(v, out d.SpeakLevel) || d.SpeakLevel < 0 || d.SpeakLevel > 10) { errors.Add($"{id}: bad speak_level"); bad = true; } break;
                case "can_mute": d.CanMute = Yes(v); break;
                case "can_join": d.CanJoin = Yes(v); break;
                default: errors.Add($"{id}: unknown field '{k}'"); bad = true; break;
            }
        }
        if (d.Id != id) { errors.Add($"{id}: id '{d.Id}' does not match the file name"); return null; }
        if (d.Title == "") d.Title = id;
        if (d.SpeakLevel < d.MemberLevel) d.SpeakLevel = d.MemberLevel;
        return bad ? null : d;
    }

    private Def? Find(string id)
    {
        id = id.Trim().ToLowerInvariant();
        if (!ValidId(id)) return null;
        try
        {
            var source = LoadObject(Dir + "/" + id).Call<string>("Definition");
            return source == null ? null : Parse(source, id);
        }
        catch (MudRuntimeException e) { errors.Add($"{id}: {e.Message}"); return null; }
    }

    /// <summary>Every channel that exists, in name order. Private: which channels exist is not for everyone to know.</summary>
    private List<Def> All()
    {
        var list = new List<Def>();
        var entries = GetDir(Dir);
        if (entries == null) return list;
        foreach (var entry in entries)
        {
            if (entry.IsDirectory || !entry.Name.EndsWith(".cs")) continue;
            var d = Find(entry.Name.Substring(0, entry.Name.Length - 3));
            if (d != null) list.Add(d);
        }
        return list;
    }

    /// <summary>Problems found in channel files, for builders. Level 10 wizards only.</summary>
    public List<string> QueryErrors()
        => Speaker() is { } who && WizLevel(who.Call<string>("QueryName") ?? "") >= 10 ? new List<string>(errors) : new List<string>();

    // ------------------------------------------------------------------
    // Who is calling, who belongs
    // ------------------------------------------------------------------

    private ObjectRef? Speaker()
    {
        if (Self.IsClone) return null;
        var caller = PreviousObject();
        if (caller == null || caller != ThisPlayer() || !caller.IsAlive) return null;
        if (!caller.IsClone || caller.ProgramPath != PlayerProgram || !Interactive(caller)) return null;
        return string.IsNullOrEmpty(caller.Call<string>("QueryName")) ? null : caller;
    }

    /// <summary>Does this character belong to the channel now? Decided from the wizard level in the registry, and (for a channel that allows it) the character's own choice to join.</summary>
    private bool Belongs(Def d, ObjectRef who)
    {
        if (!who.IsAlive || !who.IsClone || who.ProgramPath != PlayerProgram) return false;
        var level = WizLevel(who.Call<string>("QueryName") ?? "");
        if (level < d.MemberLevel) return false;
        return !d.CanJoin || who.Call<bool>("QueryChannelJoined", d.Id);
    }

    private static string NoSuchChannel(string name) => $"There is no channel called '{Esc(name)}'.\n";

    /// <summary>Does the calling player belong to the channel of this name?</summary>
    public bool IsMember(string name)
    {
        var who = Speaker();
        return who != null && Find(name) is { } d && Belongs(d, who);
    }

    /// <summary>The ids of all channels, for binding their names as commands. A player body only; membership is still checked at every use.</summary>
    public List<string> QueryVerbs()
    {
        var list = new List<string>();
        if (Speaker() == null) return list;
        foreach (var d in All()) list.Add(d.Id);
        return list;
    }

    // ------------------------------------------------------------------
    // Sending
    // ------------------------------------------------------------------

    /// <summary>
    /// A message on a channel, for "shout channel message" and for the channel's name used as a command. Returns null if the
    /// caller does not belong (the command does not exist for them), "" when sent, otherwise the text to show the speaker.
    /// </summary>
    public string? Send(string channel, string message)
    {
        var speaker = Speaker();
        if (speaker == null) return null;
        var d = Find(channel);
        if (d == null || !Belongs(d, speaker)) return null;
        var name = speaker.Call<string>("QueryName")!;
        var level = WizLevel(name);
        if (level < d.SpeakLevel) return $"You may listen to the {d.Id} channel but not speak on it.\n";
        var text = LoadObject("/secure/comm").Call<string>("Clean", message ?? "") ?? "";
        if (text == "") return $"Usage: {{cmd}}{d.Id}{{/}} MESSAGE   (or {{cmd}}shout {d.Id}{{/}} MESSAGE)\n";
        if (d.CanMute && speaker.Call<bool>("QueryChannelOff", d.Id)) return $"You have the {d.Id} channel switched off. Type {{cmd}}channel {d.Id} on{{/}}.\n";
        if (level < 1)
        {
            var now = TimeMs();
            if (lastSent.TryGetValue(name.ToLowerInvariant(), out var then) && now - then < Comm.ChannelIntervalSeconds * 1000L)
                return "You are speaking too fast. Wait a moment.\n";
            lastSent[name.ToLowerInvariant()] = now;
        }
        // Delivery decides membership again for every listener, from the registry, at this moment.
        foreach (var user in Users())
        {
            if (!user.IsAlive || !user.IsClone || user.ProgramPath != PlayerProgram || Environment(user) == null) continue;
            if (!Belongs(d, user)) continue;
            if (user != speaker && d.CanMute && user.Call<bool>("QueryChannelOff", d.Id)) continue;
            if (user != speaker && user.Call<bool>("QueryIgnores", name.ToLowerInvariant())) continue;
            TellObject(user, $"[{d.Id}] {Seen(user, speaker)}: {Esc(text)}\n");
        }
        return "";
    }

    private string Seen(ObjectRef viewer, ObjectRef who)
    {
        var name = who.Call<string>("QueryName") ?? "Someone";
        if (viewer == who || !who.Call<bool>("QueryInvisible")) return name;
        var mine = WizLevel(viewer.Call<string>("QueryName") ?? "");
        return mine >= 1 && mine >= WizLevel(name) ? name : "Someone";
    }

    // ------------------------------------------------------------------
    // channels, channel <name> on|off|join|leave
    // ------------------------------------------------------------------

    /// <summary>"channels": the channels the caller belongs to, and whether each is on.</summary>
    public string Listing(string args = "")
    {
        var speaker = Speaker();
        if (speaker == null) return "";
        var lines = new List<string>();
        foreach (var d in All())
        {
            if (!Belongs(d, speaker)) continue;
            var on = !(d.CanMute && speaker.Call<bool>("QueryChannelOff", d.Id));
            lines.Add($"  {d.Id,-12} {Esc(d.Title),-24} {(on ? "on" : "off")}");
        }
        if (lines.Count == 0) return "You do not belong to any channels.\n";
        return "Your channels:\n" + string.Join("\n", lines) + "\nType {cmd}channel{/} NAME {cmd}off{/} or {cmd}on{/} to switch one, and the channel's name followed by your message to speak on it.\n";
    }

    /// <summary>"channel name on|off|join|leave" (and "channel name" for its state). The text to show the speaker.</summary>
    public string Switch(string args)
    {
        var speaker = Speaker();
        if (speaker == null) return "";
        var name = args.Trim();
        var action = "";
        var space = name.IndexOf(' ');
        if (space > 0) { action = name.Substring(space + 1).Trim().ToLowerInvariant(); name = name.Substring(0, space); }
        if (name == "") return Listing("");

        var d = Find(name);
        // A channel the character is not entitled to looks like no channel at all.
        if (d == null || WizLevel(speaker.Call<string>("QueryName") ?? "") < d.MemberLevel) return NoSuchChannel(name);
        var joined = Belongs(d, speaker);
        if (!joined && action != "join")
        {
            if (!d.CanJoin) return NoSuchChannel(name);
            return $"You are not on the {d.Id} channel. Type {{cmd}}channel {d.Id} join{{/}}.\n";
        }

        switch (action)
        {
            case "":
                return $"The {d.Id} channel is {(d.CanMute && speaker.Call<bool>("QueryChannelOff", d.Id) ? "off" : "on")}.\n";
            case "on":
            case "off":
                if (!d.CanMute) return $"The {d.Id} channel cannot be switched off.\n";
                speaker.Call("SetChannelOff", d.Id, action == "off");
                return action == "off" ? $"You switch the {d.Id} channel off.\n" : $"You switch the {d.Id} channel on.\n";
            case "join":
                if (!d.CanJoin) return $"You cannot join the {d.Id} channel; it is not something you ask to join.\n";
                if (joined) return $"You are already on the {d.Id} channel.\n";
                speaker.Call("SetChannelJoined", d.Id, true);
                return $"You join the {d.Id} channel.\n";
            case "leave":
                if (!d.CanJoin) return $"You cannot leave the {d.Id} channel.\n";
                speaker.Call("SetChannelJoined", d.Id, false);
                return $"You leave the {d.Id} channel.\n";
            default:
                return "Usage: {cmd}channel{/} NAME [{cmd}on{/}|{cmd}off{/}|{cmd}join{/}|{cmd}leave{/}]\n";
        }
    }
}
