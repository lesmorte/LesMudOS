/// <summary>
/// The communication service: say, tell, reply, whisper, shout and the cleaning of everything players type.
/// docs/specs/communication.md.
///
/// Blueprint only. Every method that speaks for a player checks who is calling: PreviousObject() must be the body of the
/// player whose command is running (see Speaker), so code written by a wizard cannot make anyone say, tell or shout
/// anything through this service. Messages are cleaned here before anyone sees them; names are the character's real
/// name, taken from the body. Nothing here writes to the Chronicle or Renown, and tells and whispers are logged nowhere.
/// </summary>
public class Comm : MudObject
{
    // ---- The limits, in one place ----
    /// <summary>The longest message, in characters. Longer text is cut.</summary>
    public const int MaxLength = 300;
    /// <summary>One shout per character in this many seconds. Wizards are exempt.</summary>
    public const int ShoutIntervalSeconds = 10;
    /// <summary>Characters below this level cannot shout. Wizards are exempt.</summary>
    public const int ShoutMinLevel = 2;
    /// <summary>One channel message per character in this many seconds. Wizards are exempt.</summary>
    public const int ChannelIntervalSeconds = 2;
    /// <summary>The longest ignore list.</summary>
    public const int MaxIgnored = 50;

    /// <summary>The longest ignore list, for the player body.</summary>
    public int QueryMaxIgnored() => MaxIgnored;

    private const string WizListFile = "/secure/wizlist";
    private const string PlayerProgram = "/std/player";

    /// <summary>Lower-case character name to the time (ms) of the last shout.</summary>
    private readonly Dictionary<string, long> lastShout = new();

    // ------------------------------------------------------------------
    // Who is calling
    // ------------------------------------------------------------------

    /// <summary>The body of the player whose command is running, if that body is what called us. Null for anything else.</summary>
    private ObjectRef? Speaker()
    {
        if (Self.IsClone) return null;
        var caller = PreviousObject();
        if (caller == null || caller != ThisPlayer() || !caller.IsAlive) return null;
        if (!caller.IsClone || caller.ProgramPath != PlayerProgram || !Interactive(caller)) return null;
        return string.IsNullOrEmpty(caller.Call<string>("QueryName")) ? null : caller;
    }

    private static bool IsPlayerBody(ObjectRef? ob) => ob != null && ob.IsAlive && ob.IsClone && ob.ProgramPath == PlayerProgram;

    private bool InWorld(ObjectRef? ob)
        => IsPlayerBody(ob) && Interactive(ob!) && Environment(ob!) != null && !string.IsNullOrEmpty(ob!.Call<string>("QueryName"));

    private int WizLevel(string name) => LoadObject(WizListFile).Call<int>("QueryLevel", name);

    private int WizLevel(ObjectRef who) => WizLevel(who.Call<string>("QueryName") ?? "");

    private static string Esc(string text) => text.Replace("{", "{{");

    /// <summary>Can the viewer see this character? Invisible wizards (if the body says it is invisible) are seen by wizards of at least their level.</summary>
    private bool CanSee(ObjectRef viewer, ObjectRef who)
    {
        if (viewer == who || !who.Call<bool>("QueryInvisible")) return true;
        var mine = WizLevel(viewer);
        return mine >= 1 && mine >= WizLevel(who);
    }

    /// <summary>The name of a character as this viewer sees it: the capitalised name, or "Someone" for a wizard they cannot see.</summary>
    /// <summary>Tell the player's client (GMCP, Comm.Channel.Text) about a line that has just been delivered to them, as they saw it. Costs nothing for a client without GMCP.</summary>
    private void Gmcp(ObjectRef player, string channel, string talker, string text)
    {
        if (GmcpActive(player)) LoadObject("/secure/gmcp").Call("Channel", player, channel, talker, text);
    }

    private string NameFor(ObjectRef viewer, ObjectRef who) => CanSee(viewer, who) ? who.Call<string>("QueryName") ?? "Someone" : "Someone";

    /// <summary>The answer to a tell or whisper aimed at a dropped player's body that is still in the world (nothing is delivered), or null.</summary>
    private string? CannotHear(ObjectRef speaker, string name, bool sameRoom)
    {
        if (name == "" || LoadObject("/secure/linkdead").Call<ObjectRef>("Find", name) is not { } body || body == speaker || !CanSee(speaker, body)) return null;
        if (sameRoom && Environment(body) != Environment(speaker)) return null;
        return $"{NameFor(speaker, body)} cannot hear you: their eyes are blank and distant.\n";
    }

    private ObjectRef? FindPlayer(string name)
    {
        foreach (var user in Users())
            if (InWorld(user) && string.Equals(user.Call<string>("QueryName"), name, StringComparison.OrdinalIgnoreCase)) return user;
        return null;
    }

    private static string FirstWord(string text, out string rest)
    {
        text = text.Trim();
        var space = text.IndexOf(' ');
        if (space < 0) { rest = ""; return text; }
        rest = text.Substring(space + 1).Trim();
        return text.Substring(0, space);
    }

    // ------------------------------------------------------------------
    // Cleaning
    // ------------------------------------------------------------------

    private static bool IsBreak(char c) => c == '\n' || c == '\r' || c == '\t' || c == '\u0085' || c == '\u2028' || c == '\u2029';

    // Control characters, and the invisible formatting characters that can reorder or hide text.
    private static bool IsDropped(char c)
        => c < ' ' || (c >= '\u007f' && c <= '\u009f') || (c >= '\u200b' && c <= '\u200f') || (c >= '\u202a' && c <= '\u202e')
           || (c >= '\u2060' && c <= '\u206f') || c == '\ufeff';

    // "{red}", "{/}", "{cmd}": a brace, a word of lower-case letters, digits or underscore (or a slash), a closing brace.
    private static string StripTags(string text)
    {
        if (text.IndexOf('{') < 0) return text;
        var sb = new StringBuilder();
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '{' && i + 1 < text.Length && text[i + 1] == '{')
            {
                sb.Append("{{"); // an escaped brace is not the start of a tag; it is shown as typed
                i += 2;
                continue;
            }
            if (text[i] == '{')
            {
                var end = text.IndexOf('}', i + 1);
                if (end > i && end - i <= 14)
                {
                    var tag = text.Substring(i + 1, end - i - 1);
                    if (tag == "/" || (tag.Length > 0 && tag.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')))
                    {
                        i = end + 1;
                        continue;
                    }
                }
            }
            sb.Append(text[i]);
            i++;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Make player text fit to show: line breaks become spaces, control characters and colour tags are removed, runs of spaces
    /// shrink to one, the ends are trimmed, and it is cut to MaxLength characters. Braces that remain are not tags and are
    /// shown as typed (callers escape them with Esc). Returns "" if nothing is left. Pure, so anyone may call it.
    /// </summary>
    public string Clean(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (IsBreak(c)) sb.Append(' ');
            else if (!IsDropped(c)) sb.Append(c);
        }
        // Removing a tag can join the pieces around it into a new tag ("{r{red}ed}"), so repeat until nothing changes.
        var result = sb.ToString();
        while (true)
        {
            var stripped = StripTags(result);
            if (stripped == result) break;
            result = stripped;
        }
        sb.Clear();
        var space = true;
        foreach (var c in result)
        {
            if (c == ' ') { if (!space) sb.Append(' '); space = true; }
            else { sb.Append(c); space = false; }
        }
        result = sb.ToString().TrimEnd();
        if (result.Length > MaxLength)
        {
            result = result.Substring(0, MaxLength);
            if (char.IsHighSurrogate(result[^1])) result = result.Substring(0, result.Length - 1);
            result = result.TrimEnd();
        }
        return result;
    }

    // ------------------------------------------------------------------
    // say
    // ------------------------------------------------------------------

    /// <summary>"say": tell everything living in the room. The speaker is told "You say: ...".</summary>
    public string DoSay(string args)
    {
        var speaker = Speaker();
        if (speaker == null) return "";
        var text = Clean(args);
        if (text == "") return "Usage: {cmd}say{/} MESSAGE\n";
        var room = Environment(speaker);
        TellObject(speaker, $"You say: {Esc(text)}\n");
        if (room == null) return "";
        foreach (var other in AllInventory(room))
            if (other.IsAlive && other != speaker && Living(other)) TellObject(other, $"{NameFor(other, speaker)} says: {Esc(text)}\n");
        return "";
    }

    // ------------------------------------------------------------------
    // tell, reply
    // ------------------------------------------------------------------

    private bool RoomFlag(ObjectRef who, string query)
        => Environment(who) is { } room && room.Call<bool>(query);

    /// <summary>"tell player message".</summary>
    public string DoTell(string args)
    {
        var speaker = Speaker();
        if (speaker == null) return "";
        var name = FirstWord(args, out var rest);
        var text = Clean(rest);
        if (name == "" || text == "") return "Usage: {cmd}tell{/} PLAYER MESSAGE\n";
        var target = FindPlayer(name);
        if (target == null && CannotHear(speaker, name, false) is { } deaf) return deaf;
        if (target == null || !CanSee(speaker, target)) return "No one by that name is in the world.\n";
        return TellTo(speaker, target, text);
    }

    /// <summary>"reply message": tell the last player who told you something.</summary>
    public string DoReply(string args)
    {
        var speaker = Speaker();
        if (speaker == null) return "";
        var text = Clean(args);
        if (text == "") return "Usage: {cmd}reply{/} MESSAGE\n";
        var last = speaker.Call<string>("QueryLastTeller") ?? "";
        if (last == "") return "You have no one to reply to.\n";
        // Whoever told you is not asked to be visible: they spoke first.
        var target = FindPlayer(last);
        if (target == null && CannotHear(speaker, last, false) is { } deaf) return deaf;
        if (target == null) return "No one by that name is in the world.\n";
        return TellTo(speaker, target, text);
    }

    private string TellTo(ObjectRef speaker, ObjectRef target, string text)
    {
        var myName = speaker.Call<string>("QueryName")!;
        if (target == speaker) return "You cannot tell yourself anything.\n";
        var targetName = NameFor(speaker, target);
        // A room that blocks tells binds everyone in it, in both directions, but a wizard at either end is not bound.
        if (WizLevel(speaker) < 1 && WizLevel(target) < 1)
        {
            if (RoomFlag(speaker, "QueryNoTell")) return "Nothing you say here can be sent to anyone far away.\n";
            if (RoomFlag(target, "QueryNoTell")) return $"{targetName} cannot be reached by tell just now.\n";
        }
        if (target.Call<bool>("QueryIgnores", myName.ToLowerInvariant())) return $"{targetName} is not listening to you.\n";

        TellObject(target, $"{NameFor(target, speaker)} tells you: {Esc(text)}\n");
        Gmcp(target, "tell", NameFor(target, speaker), text);
        target.Call("SetLastTeller", myName);
        TellObject(speaker, $"You tell {targetName}: {Esc(text)}\n");
        // The idle timer (when the game has one) says whether the target will see it soon.
        if (target.Call<bool>("QueryIdle")) return $"{targetName} is idle and may not answer for a while.\n";
        return "";
    }

    // ------------------------------------------------------------------
    // whisper
    // ------------------------------------------------------------------

    private ObjectRef? PresentPlayer(ObjectRef speaker, string name)
    {
        if (name == "") return null;
        var found = FindPlayer(name);
        return found != null && found != speaker && Environment(found) == Environment(speaker) && CanSee(speaker, found) ? found : null;
    }

    /// <summary>"whisper message to name", or "whisper name message".</summary>
    public string DoWhisper(string args)
    {
        var speaker = Speaker();
        if (speaker == null) return "";
        args = args.Trim();
        const string usage = "Usage: {cmd}whisper{/} MESSAGE {cmd}to{/} NAME   (or {cmd}whisper{/} NAME MESSAGE)\n";
        if (args == "") return usage;

        ObjectRef? target = null;
        var message = "";
        var named = "";
        var at = args.LastIndexOf(" to ", StringComparison.OrdinalIgnoreCase);
        if (at > 0)
        {
            named = args.Substring(at + 4).Trim();
            if (named != "" && !named.Contains(' '))
            {
                target = PresentPlayer(speaker, named);
                if (target != null) message = args.Substring(0, at);
            }
        }
        if (target == null)
        {
            var first = FirstWord(args, out var rest);
            var t = PresentPlayer(speaker, first);
            if (t != null) { target = t; message = rest; }
        }
        if (target == null && (CannotHear(speaker, named, true) ?? CannotHear(speaker, FirstWord(args, out _), true)) is { } deaf) return deaf;
        if (target == null)
        {
            // Name the one that was meant if we can tell; both ways of writing it end here for someone who is not present.
            return at > 0 && named != "" && !named.Contains(' ') ? "No one by that name is here.\n" : usage;
        }
        var text = Clean(message);
        if (text == "") return usage;

        var myName = speaker.Call<string>("QueryName")!;
        var targetName = NameFor(speaker, target);
        TellObject(speaker, $"You whisper to {targetName}: {Esc(text)}\n");
        var ignored = target.Call<bool>("QueryIgnores", myName.ToLowerInvariant());
        if (!ignored) TellObject(target, $"{NameFor(target, speaker)} whispers to you: {Esc(text)}\n");
        foreach (var other in AllInventory(Environment(speaker)!))
        {
            if (!other.IsAlive || other == speaker || other == target || !InWorld(other)) continue;
            if (other.Call<bool>("QueryIgnores", myName.ToLowerInvariant())) continue;
            TellObject(other, $"{NameFor(other, speaker)} whispers something to {NameFor(other, target)}.\n");
        }
        if (ignored) return $"{targetName} is not listening to you.\n";
        return "";
    }

    // ------------------------------------------------------------------
    // shout
    // ------------------------------------------------------------------

    /// <summary>"shout message", or "shout on" and "shout off". (A channel message is sent by the channel service, not here.)</summary>
    public string DoShout(string args)
    {
        var speaker = Speaker();
        if (speaker == null) return "";
        args = args.Trim();
        var lower = args.ToLowerInvariant();
        if (lower == "off" || lower == "on")
        {
            speaker.Call("SetShoutOn", lower == "on");
            return lower == "on" ? "You will hear shouts again.\n" : "You will not hear shouts. Type {cmd}shout on{/} to hear them again.\n";
        }
        var text = Clean(args);
        if (text == "") return "Usage: {cmd}shout{/} MESSAGE   ({cmd}shout off{/} and {cmd}shout on{/} stop and start hearing shouts)\n";

        var myName = speaker.Call<string>("QueryName")!;
        var wizard = WizLevel(myName) >= 1;
        if (!wizard)
        {
            if (speaker.Call<int>("QueryLevel") < ShoutMinLevel) return $"You cannot shout until you reach level {ShoutMinLevel}.\n";
            if (RoomFlag(speaker, "QueryNoShout")) return "Your voice does not carry here.\n";
            var now = TimeMs();
            if (lastShout.TryGetValue(myName.ToLowerInvariant(), out var then) && now - then < ShoutIntervalSeconds * 1000L)
            {
                var seconds = (int)((ShoutIntervalSeconds * 1000L - (now - then) + 999) / 1000);
                return $"You must wait {seconds} more second(s) before you shout again.\n";
            }
            lastShout[myName.ToLowerInvariant()] = now;
        }

        TellObject(speaker, $"You shout: {Esc(text)}\n");
        foreach (var user in Users())
        {
            if (user == speaker || !InWorld(user)) continue;
            if (!user.Call<bool>("QueryShoutOn")) continue;
            if (user.Call<bool>("QueryIgnores", myName.ToLowerInvariant())) continue;
            if (RoomFlag(user, "QueryNoShout") && WizLevel(user) < 1) continue;
            TellObject(user, $"{NameFor(user, speaker)} shouts: {Esc(text)}\n");
            Gmcp(user, "shout", NameFor(user, speaker), text);
        }
        return "";
    }
}
