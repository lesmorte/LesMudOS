// @inherit /std/living

/// <summary>
/// The quest journal (docs/specs/quest-journal.md): what a character keeps of the quests it has been given. It sits between /std/living and
/// /std/adventurer, so every character body has it, and it is saved with the character.
///
/// A quest is in one of four states in the journal: offered (the giver has spoken of it, and nothing more: a small hidden buffer that is
/// never listed or sent), active, completed and abandoned. The rest of the game still reads the older three-valued state (0 not started,
/// 1 active, 2 completed) with QueryQuest and writes it with SetQuest; the journal follows every SetQuest, and an abandoned quest reads
/// as 0 again, so its giver offers it as to a newcomer.
///
/// An active quest keeps a conversation log: what the giver said to this character about the quest, what the character asked, and a note
/// whenever an objective moves. The lines are recorded at the source (/std/npc: QuestSay and the quest tags of AddTopic and AddChat), never
/// taken from the screen. A completed or abandoned quest keeps only a small summary: its log is dropped at that moment.
///
/// Rules, all enforced here:
///  - The journal can be written only by trusted game code (a real object of /std, /secure, /domains, /classes, /skills or /events that is in
///    the same room as the character, for dialogue), never by a restricted execution (a wizard's sandbox code) except on the wizard in their
///    own workshop, whose body is thrown away when they leave it (docs/SANDBOX.md). The master's ValidCall already refuses the calls; the
///    checks here are the second layer.
///  - GMCP (Char.Quests.List and Char.Quests.Log) is sent by this body, to its own connection, and only for quests that are active,
///    completed or abandoned: never for one that was only spoken of.
///  - Bounds, in the constants below: the log of an active quest keeps its newest 80 entries and 8,000 characters.
/// </summary>
public abstract class QuestJournal : Living
{
    /// <summary>The most entries and the most characters of text the log of one active quest keeps. The oldest are dropped first.</summary>
    public const int MaxLogEntries = 80;
    public const int MaxLogChars = 8000;
    /// <summary>The first line of a log that has lost its oldest entries.</summary>
    public const string GapText = "(earlier conversation not kept)";

    private const int MaxLineChars = 1200;      // one entry's text
    private const int MaxPendingEntries = 6;    // what is kept of a quest only spoken of (offered), or abandoned and spoken of again
    private const int MaxPendingChars = 1500;
    private const int MaxRecords = 60;          // quests a character's journal holds
    private const int MaxSummaryChars = 400;
    private const int ListBudgetBytes = 11000;  // Char.Quests.List stays well under the driver's 16 KB per message
    private const int LogChunkBytes = 7000;     // and so does each Char.Quests.Log
    private const int LogChunkEntries = 40;
    private const long MinListGapMs = 250;      // Char.Quests.List is coalesced like Char.Vitals
    private const long PollGapMs = 900;
    private const char Sep = '\u001f';
    private const string Registry = "/secure/chronicle";

    /// <summary>The quests that were given before the journal was kept: who gave them, so that their text can be found. Nothing else uses this.</summary>
    private static readonly Dictionary<string, string> LegacyGivers = new()
    {
        ["clear-the-cellar"] = "/domains/hollow/npc/rowan",
        ["overseers-orders"] = "/domains/hollow/npc/rowan",
        ["the-quiet-offering"] = "/domains/hollow/npc/sister_merewyn",
    };

    private static readonly string[] TrustedTrees = { "/std/", "/secure/", "/domains/", "/classes/", "/skills/", "/events/" };

    /// <summary>Quest id to its state: 1 active, 2 completed. A quest not here is not started (an abandoned one is not here either).</summary>
    [Saved] protected Dictionary<string, int> quests = new();
    [Saved] protected Dictionary<string, string> questTitles = new();
    /// <summary>The journal: quest id to its record (state, giver, area, dates, reward, summary, the last sequence number ...).</summary>
    [Saved] private Dictionary<string, Dictionary<string, string>> journal = new();
    /// <summary>The conversation of each quest: encoded entries, oldest first.</summary>
    [Saved] private Dictionary<string, List<string>> journalLog = new();

    private readonly Dictionary<string, Dictionary<string, string>> definitions = new(); // what the givers say about their quests; not saved
    private readonly Dictionary<long, string> dateCache = new();
    private bool burstSent;
    private bool listDirty;
    private bool listScheduled;
    private bool muted; // a transition writes its entries and then sends the log once, whole
    private long listAt;
    private long polledAt;

    // ------------------------------------------------------------------
    // The quest state the rest of the game reads and writes
    // ------------------------------------------------------------------

    public int QueryQuest(string id) => quests.TryGetValue(id, out var state) ? state : 0;

    public void SetQuest(string id, int state) => Transition(PreviousObject(), id, state, "", "");

    /// <summary>Set a quest's state and the title the quest log shows for it ("Clear the Cellar").</summary>
    public void SetQuest(string id, int state, string title) => Transition(PreviousObject(), id, state, title, "");

    /// <summary>As above, and what the character was given for completing it ("150 experience, 50 gold and a helm"), for the journal.</summary>
    public void SetQuest(string id, int state, string title, string reward) => Transition(PreviousObject(), id, state, title, reward);

    private void Transition(ObjectRef? caller, string id, int state, string title, string reward)
    {
        var before = QueryQuest(id);
        quests[id] = state;
        if (title != "") questTitles[id] = title;
        if (!ValidId(id) || state is < 0 or > 2) return;
        try { Follow(caller, id, before, state, reward); }
        catch (MudRuntimeException) { } // the journal never stops a quest: the state above is already set
    }

    // ------------------------------------------------------------------
    // Who may write
    // ------------------------------------------------------------------

    private static bool ValidId(string id)
    {
        if (id.Length == 0 || id.Length > 48) return false;
        foreach (var c in id) if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_')) return false;
        return true;
    }

    private static bool TrustedPath(string path)
    {
        if (path.Contains("..") || path.Contains("//")) return false;
        foreach (var tree in TrustedTrees) if (path.StartsWith(tree, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>The state and the number of log entries of one quest in the journal ("active:12"), or "none". Tests and tools read it.</summary>
    public string QueryJournal(string id) => journal.TryGetValue(id, out var r) ? r.GetValueOrDefault("state") + ":" + (journalLog.TryGetValue(id, out var l) ? l.Count : 0) : "none";

    private bool InWorkshop() => Self.Call<bool>("QueryInWorkshop");

    /// <summary>The caller may write this journal: trusted real code, or (only for the wizard in their own workshop, which is never saved) sandbox code.</summary>
    private bool MayWrite(ObjectRef? caller)
    {
        if (caller == null || !caller.IsAlive || !Self.IsAlive) return false;
        if (RestrictedCaller() != null) return InWorkshop() && Self.Domain == "";
        if (caller.Domain != "" || !TrustedPath(caller.ProgramPath)) return false;
        return caller.ProgramPath != "/std/player"; // another character's body never writes this one's journal
    }

    // ------------------------------------------------------------------
    // Records
    // ------------------------------------------------------------------

    private Dictionary<string, string>? Rec(string id) => journal.TryGetValue(id, out var r) ? r : null;
    private string Fld(string id, string key) => Rec(id) is { } r && r.TryGetValue(key, out var v) ? v : "";
    private long Num(string id, string key) => long.TryParse(Fld(id, key), out var n) ? n : 0;
    private string StateOf(string id) => Fld(id, "state");

    private void SetFld(string id, string key, string value)
    {
        if (!journal.TryGetValue(id, out var r)) journal[id] = r = new Dictionary<string, string>();
        r[key] = value;
    }

    private string TitleOf(string id)
    {
        var t = Fld(id, "title");
        if (t != "") return t;
        return questTitles.TryGetValue(id, out var q) ? q : Cap(id.Replace('-', ' ').Replace('_', ' '));
    }

    /// <summary>Make room for one more record, or say there is none.</summary>
    private bool RoomForRecord(string id)
    {
        if (journal.ContainsKey(id) || journal.Count < MaxRecords) return true;
        foreach (var state in new[] { "offered", "abandoned", "completed" })
        {
            var oldest = journal.Where(r => r.Value.GetValueOrDefault("state") == state).OrderBy(r => long.TryParse(r.Value.GetValueOrDefault("ended"), out var e) ? e : 0).Select(r => r.Key).FirstOrDefault();
            if (oldest == null) continue;
            journal.Remove(oldest);
            journalLog.Remove(oldest);
            return true;
        }
        return false;
    }

    private static string Clean(string text, int max)
    {
        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (c == '\n' || c == '\r' || c == '\t') sb.Append(' ');
            else if (c < ' ' || (c >= '\u007f' && c <= '\u009f') || c == Sep) continue;
            else sb.Append(c);
            if (sb.Length >= max) break;
        }
        var s = sb.ToString().Trim();
        return text.Length > max && s.Length >= max ? s.Substring(0, max - 3) + "..." : s;
    }

    /// <summary>The text of an entry or summary as a client should show it: the game's {tags} removed, "{{" a brace.</summary>
    private static string Plain(string text)
    {
        var sb = new StringBuilder();
        var i = 0;
        while (i < text.Length)
        {
            if (text[i] == '{' && i + 1 < text.Length && text[i + 1] == '{') { sb.Append('{'); i += 2; continue; }
            if (text[i] == '{')
            {
                var end = text.IndexOf('}', i + 1);
                if (end > i && end - i <= 14)
                {
                    var tag = text.Substring(i + 1, end - i - 1);
                    if (tag == "/" || (tag.Length > 0 && tag.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_'))) { i = end + 1; continue; }
                }
            }
            sb.Append(text[i]);
            i++;
        }
        return sb.ToString();
    }

    private static string Escaped(string text) => text.Replace("{", "{{");

    // ------------------------------------------------------------------
    // What the givers say about their quests
    // ------------------------------------------------------------------

    /// <summary>The definition of a quest as its giver gave it (title, area, completed, abandoned, items, objectives, abandonable), or null.</summary>
    private Dictionary<string, string>? Definition(string id)
    {
        if (definitions.TryGetValue(id, out var known)) return known;
        var path = Fld(id, "giverpath");
        if (path == "" && LegacyGivers.TryGetValue(id, out var legacy)) path = legacy;
        if (path == "" || !TrustedPath(path)) return null;
        try
        {
            var found = LoadObject(path).Call<Dictionary<string, string>>("QueryQuestDefinition", id);
            if (found != null) definitions[id] = found;
            return found;
        }
        catch (MudRuntimeException) { return null; }
    }

    private static string Of(Dictionary<string, string>? def, string key) => def != null && def.TryGetValue(key, out var v) ? v : "";

    private static string Generated(string kind, string title, string giver)
    {
        if (kind == "completed") return giver != "" ? $"You completed \"{title}\" for {giver}." : $"You completed \"{title}\".";
        return giver != "" ? $"You gave up \"{title}\", which {giver} had given you." : $"You gave up \"{title}\".";
    }

    // ------------------------------------------------------------------
    // Following SetQuest
    // ------------------------------------------------------------------

    private void Follow(ObjectRef? caller, string id, int before, int state, string reward)
    {
        if (state == 0)
        {
            journal.Remove(id);
            journalLog.Remove(id);
            MarkListChanged();
            return;
        }
        var old = StateOf(id);
        if (!RoomForRecord(id)) return;
        var giverObject = caller != null && caller.IsAlive && caller.Domain == "" && TrustedPath(caller.ProgramPath) ? caller : null;
        var def = giverObject != null ? giverObject.Call<Dictionary<string, string>>("QueryQuestDefinition", id) : null; // null if it has none
        if (def != null) definitions[id] = def;
        if (Fld(id, "title") == "") SetFld(id, "title", questTitles.TryGetValue(id, out var t) ? t : Of(def, "title") != "" ? Of(def, "title") : TitleOf(id));
        if (giverObject != null) SetFld(id, "giverpath", giverObject.ProgramPath);
        if (Fld(id, "giver") == "" && caller != null && caller.IsAlive)
            SetFld(id, "giver", Clean(Of(def, "giver") != "" ? Of(def, "giver") : giverObject?.Call<string>("QueryName") ?? "", 60));
        var area = Of(def, "area");
        if (area == "" && Fld(id, "area") == "" && giverObject != null) area = Environment(giverObject)?.Call<string>("QueryAreaName") ?? "";
        if (area != "") SetFld(id, "area", Clean(area, 80));

        var now = Time().ToString();
        if (state == 1)
        {
            if (old == "active") return;
            SetFld(id, "state", "active");
            SetFld(id, "started", now);
            SetFld(id, "ended", "");
            SetFld(id, "summary", "");
            SetFld(id, "reward", "");
            SetFld(id, "trimmed", "");
            SetFld(id, "prog", ProgressKey(id));
            Retain(id, MaxPendingEntries); // only what was said since the quest was last offered (or given up) carries over: a fresh log
            RenumberLog(id);
            muted = true;
            try { AddEntry(id, "note", "", "Quest taken on: " + TitleOf(id) + "."); }
            finally { muted = false; }
            ResetLog(id);
            MarkListChanged();
            return;
        }
        // completed: the conversation is dropped, the summary kept
        var giver = Fld(id, "giver");
        var summary = Of(def, "completed");
        SetFld(id, "state", "completed");
        if (Fld(id, "started") == "") SetFld(id, "started", before == 1 ? now : "0");
        SetFld(id, "ended", now);
        SetFld(id, "reward", Clean(reward, 200));
        SetFld(id, "summary", Clean(summary != "" ? summary : Generated("completed", TitleOf(id), giver), MaxSummaryChars));
        SetFld(id, "trimmed", "");
        journalLog.Remove(id);
        ResetLog(id);
        MarkListChanged();
    }

    /// <summary>Quests the older saves hold that the journal has no record of (given before it was kept), and records that disagree with the state.</summary>
    private void Sync()
    {
        foreach (var q in quests.ToList())
        {
            if (q.Value is not (1 or 2) || !ValidId(q.Key)) continue;
            var st = StateOf(q.Key);
            if (q.Value == 1 && st == "active") continue;
            if (q.Value == 2 && st == "completed") continue;
            if (!RoomForRecord(q.Key)) continue;
            var def = Definition(q.Key);
            if (Fld(q.Key, "title") == "") SetFld(q.Key, "title", questTitles.TryGetValue(q.Key, out var t) ? t : Of(def, "title") != "" ? Of(def, "title") : TitleOf(q.Key));
            if (LegacyGivers.TryGetValue(q.Key, out var lg) && Fld(q.Key, "giverpath") == "") SetFld(q.Key, "giverpath", lg);
            if (Fld(q.Key, "giver") == "" && Of(def, "giver") != "") SetFld(q.Key, "giver", Of(def, "giver"));
            if (Fld(q.Key, "area") == "" && Of(def, "area") != "") SetFld(q.Key, "area", Of(def, "area"));
            if (q.Value == 1)
            {
                SetFld(q.Key, "state", "active");
                if (Fld(q.Key, "started") == "") SetFld(q.Key, "started", "0");
                SetFld(q.Key, "prog", ProgressKey(q.Key));
            }
            else
            {
                SetFld(q.Key, "state", "completed");
                if (Fld(q.Key, "started") == "") SetFld(q.Key, "started", "0");
                if (Fld(q.Key, "ended") == "") SetFld(q.Key, "ended", "0");
                var summary = Of(def, "completed");
                SetFld(q.Key, "summary", Clean(summary != "" ? summary : Generated("completed", TitleOf(q.Key), Fld(q.Key, "giver")), MaxSummaryChars));
                journalLog.Remove(q.Key);
            }
            listDirty = true;
        }
        foreach (var id in journal.Keys.ToList())
        {
            var st = StateOf(id);
            if ((st == "active" || st == "completed") && QueryQuest(id) == 0) { journal.Remove(id); journalLog.Remove(id); listDirty = true; }
        }
    }

    // ------------------------------------------------------------------
    // The conversation log
    // ------------------------------------------------------------------

    private static string Encode(int seq, long ts, string kind, string speaker, string text) => string.Join(Sep.ToString(), seq, ts, kind, speaker, text);

    private static (int seq, long ts, string kind, string speaker, string text) Decode(string raw)
    {
        var p = raw.Split(Sep, 5);
        if (p.Length < 5) return (0, 0, "note", "", "");
        return (int.TryParse(p[0], out var s) ? s : 0, long.TryParse(p[1], out var t) ? t : 0, p[2], p[3], p[4]);
    }

    private List<string> LogOf(string id)
    {
        if (!journalLog.TryGetValue(id, out var log)) journalLog[id] = log = new List<string>();
        return log;
    }

    /// <summary>Keep only the newest entries of a log that was spoken into before the quest was active.</summary>
    private void Retain(string id, int entries)
    {
        var log = LogOf(id);
        while (log.Count > entries) log.RemoveAt(0);
    }

    private void RenumberLog(string id)
    {
        var log = LogOf(id);
        for (var i = 0; i < log.Count; i++)
        {
            var e = Decode(log[i]);
            log[i] = Encode(i + 1, e.ts, e.kind, e.speaker, e.text);
        }
        SetFld(id, "seq", log.Count.ToString());
    }

    private static int Chars(List<string> log) => log.Sum(raw => Decode(raw).text.Length);

    /// <summary>Add one entry to the log of a quest that is active, offered or abandoned (and so being spoken of). True if the log had to drop old entries.</summary>
    private void AddEntry(string id, string kind, string speaker, string text)
    {
        var state = StateOf(id);
        if (state is not ("active" or "offered" or "abandoned")) return;
        text = Clean(text, MaxLineChars);
        if (text == "") return;
        var log = LogOf(id);
        var seq = (int)Num(id, "seq") + 1;
        SetFld(id, "seq", seq.ToString());
        var raw = Encode(seq, Time(), kind, Clean(speaker, 60), text);
        log.Add(raw);
        var limit = state == "active" ? MaxLogEntries : MaxPendingEntries;
        var chars = state == "active" ? MaxLogChars : MaxPendingChars;
        var dropped = false;
        while (log.Count > 1 && (log.Count > limit || Chars(log) > chars)) { log.RemoveAt(0); dropped = true; }
        if (dropped && state == "active") SetFld(id, "trimmed", "1");
        if (state != "active" || muted) return;
        if (dropped) ResetLog(id); // a client cannot know what was dropped: it starts the log again from the game's copy
        else SendLogEntry(id, raw);
        listDirty = true;
    }

    /// <summary>
    /// Dialogue at the source: what the giver said to this character about quests (comma-separated ids), and what the character asked to
    /// draw it out. Only an NPC that is in this character's room may write it. A quest not yet spoken of becomes "offered" (hidden); a
    /// completed one is not recorded again.
    /// </summary>
    public void QuestHeard(string questIds, string asked, string speaker, string line)
    {
        var caller = PreviousObject();
        if (!MayWrite(caller) || questIds.Length > 200 || line.Length == 0) return;
        if (caller!.Domain == "" && Self.Domain == "" && Environment(caller) != Environment(Self)) return; // heard only by those in the room
        foreach (var raw in questIds.Split(','))
        {
            var id = raw.Trim();
            if (!ValidId(id)) continue;
            var state = StateOf(id);
            if (state == "completed" || QueryQuest(id) == 2) continue;
            if (state == "")
            {
                if (!RoomForRecord(id)) continue;
                SetFld(id, "state", "offered");
                SetFld(id, "seq", "0");
                SetFld(id, "title", questTitles.TryGetValue(id, out var qt) ? qt : "");
                if (caller.IsAlive)
                {
                    SetFld(id, "giver", Clean(speaker, 60));
                    if (caller.Domain == "" && TrustedPath(caller.ProgramPath)) SetFld(id, "giverpath", caller.ProgramPath);
                }
                if (Fld(id, "title") == "")
                {
                    var def = caller.Domain == "" ? caller.Call<Dictionary<string, string>>("QueryQuestDefinition", id) : null;
                    if (def != null) { definitions[id] = def; SetFld(id, "title", Of(def, "title")); }
                }
            }
            if (asked != "") AddEntry(id, "you", "You", asked);
            AddEntry(id, "npc", speaker, line);
        }
    }

    // ------------------------------------------------------------------
    // Objectives
    // ------------------------------------------------------------------

    /// <summary>The objectives of an active quest from its definition: kill lines count a creature kind killed, have lines the items carried.</summary>
    private List<(string text, int have, int need)> Objectives(string id)
    {
        var result = new List<(string, int, int)>();
        var lines = Of(Definition(id), "objectives");
        if (lines == "") return result;
        foreach (var line in lines.Split('\n'))
        {
            var p = line.Split('|');
            if (p.Length < 3) continue;
            var need = p.Length > 3 && int.TryParse(p[3], out var n) && n > 0 ? n : 1;
            var have = 0;
            if (p[0] == "kill") have = Self.Call<bool>("QueryKilled", p[1]) ? need : 0;
            else if (p[0] == "have") have = Math.Min(need, CountIn(Self, p[1]).Values.Sum());
            result.Add((Clean(p[2], 160), have, need));
        }
        return result;
    }

    private string ProgressKey(string id) => string.Join(",", Objectives(id).Select(o => o.have));

    private static string ProgressText(List<(string text, int have, int need)> objectives)
    {
        if (objectives.Count == 0) return "";
        var done = objectives.Count(o => o.have >= o.need);
        return done + " of " + objectives.Count;
    }

    /// <summary>A note in the log of each active quest whose objectives have moved since last looked.</summary>
    private void CheckProgress()
    {
        foreach (var id in journal.Where(r => r.Value.GetValueOrDefault("state") == "active").Select(r => r.Key).ToList())
        {
            var objectives = Objectives(id);
            if (objectives.Count == 0) continue;
            var key = string.Join(",", objectives.Select(o => o.have));
            var old = Fld(id, "prog");
            if (key == old) continue;
            SetFld(id, "prog", key);
            var was = old.Split(',');
            for (var i = 0; i < objectives.Count; i++)
            {
                var before = i < was.Length && int.TryParse(was[i], out var b) ? b : 0;
                if (before != objectives[i].have) AddEntry(id, "note", "", $"{objectives[i].text} ({objectives[i].have} of {objectives[i].need})");
            }
            listDirty = true;
        }
    }

    // ------------------------------------------------------------------
    // Dates
    // ------------------------------------------------------------------

    private string DateOf(long ts)
    {
        if (ts <= 0) return "";
        var day = ts / 86400;
        if (dateCache.TryGetValue(day, out var known)) return known;
        string text;
        try { text = LoadObject(Registry).Call<string>("DateOfTimestamp", ts) ?? ""; }
        catch (MudRuntimeException) { text = ""; }
        if (dateCache.Count > 64) dateCache.Clear();
        dateCache[day] = text;
        return text;
    }

    // ------------------------------------------------------------------
    // Asking: the commands
    // ------------------------------------------------------------------

    /// <summary>Binds the journal's own verbs. The body calls it where it binds its other commands; "quests" stays the body's.</summary>
    protected void BindJournalCommands()
    {
        AddAction("quest", CmdQuest);
        AddAction("abandon", CmdAbandon);
    }

    private static int Visible(string text) => Plain(text).Length;

    /// <summary>The text wrapped at a space to 80 columns (markup tags do not count), every line starting with the indent.</summary>
    private string Wrapped(string text, string indent = "")
    {
        var result = new StringBuilder();
        foreach (var paragraph in text.TrimEnd('\n').Split('\n'))
        {
            var current = "";
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (current != "" && indent.Length + Visible(current) + 1 + Visible(word) > 80) { result.Append(indent).Append(current).Append('\n'); current = word; }
                else current = current == "" ? word : current + " " + word;
            }
            result.Append(indent).Append(current).Append('\n');
        }
        return result.ToString();
    }

    private List<string> IdsIn(params string[] states)
    {
        var ids = journal.Where(r => states.Contains(r.Value.GetValueOrDefault("state"))).Select(r => r.Key).ToList();
        ids.Sort((a, b) =>
        {
            var c = Num(b, "ended").CompareTo(Num(a, "ended"));
            return c != 0 ? c : Num(a, "started").CompareTo(Num(b, "started"));
        });
        return ids;
    }

    /// <summary>What "quests" adds under the old lines: how many are completed and abandoned, and where to read them. Empty with no quests at all.</summary>
    protected string JournalFooter()
    {
        Sync();
        CheckProgress();
        SendBurstIfWanted();
        var done = IdsIn("completed").Count;
        var gone = IdsIn("abandoned").Count;
        var active = IdsIn("active").Count;
        if (done + gone + active == 0) return "";
        if (done + gone == 0) return "{cmd}quest{/} NAME tells the story of one.\n";
        return $"{done} completed, {gone} abandoned. {{cmd}}quest{{/}} NAME tells the story of one; {{cmd}}quests completed{{/}} and {{cmd}}quests abandoned{{/}} list them.\n";
    }

    /// <summary>"quests completed", "quests abandoned", "quests NAME": the part of "quests" the journal answers. False when the plain list is wanted.</summary>
    protected bool JournalQuestsCommand(string args)
    {
        args = args.Trim();
        if (args == "" || args.ToLowerInvariant() == "active") return false;
        Sync();
        CheckProgress();
        var word = args.ToLowerInvariant();
        if (word == "completed" || word == "done" || word == "complete") { Write(SummaryList("completed")); SendBurstIfWanted(); return true; }
        if (word == "abandoned" || word == "given up") { Write(SummaryList("abandoned")); SendBurstIfWanted(); return true; }
        return ShowQuest(args);
    }

    private bool CmdQuest(string args)
    {
        args = args.Trim();
        if (args == "")
        {
            Write("Usage: {cmd}quest{/} NAME (the story of one quest), {cmd}quest abandon{/} NAME, {cmd}quests{/}, {cmd}quests completed{/}, {cmd}quests abandoned{/}.\n");
            return true;
        }
        var lower = args.ToLowerInvariant();
        if (lower.StartsWith("abandon ") || lower == "abandon") return CmdAbandon(args.Substring(7));
        Sync();
        CheckProgress();
        if (lower is "completed" or "abandoned" or "active") return JournalQuestsCommand(lower) || ShowActiveList();
        return ShowQuest(args);
    }

    private bool ShowActiveList()
    {
        var active = IdsIn("active").Select(TitleOf).ToList();
        Write(active.Count == 0 ? "You have no active quests.\n" : "Active quests: " + string.Join(", ", active) + "\n");
        return true;
    }

    private void SendBurstIfWanted()
    {
        if (GmcpActive(Self)) SendBurst();
    }

    /// <summary>The id of the quest the words mean among the given states: its id or its title, whole or in part. Null with a message written if none or several.</summary>
    private string? FindQuest(string words, params string[] states)
    {
        var text = words.Trim().ToLowerInvariant();
        if (text.StartsWith("the ")) text = text.Substring(4);
        var ids = IdsIn(states);
        var exact = ids.Where(i => i == text || TitleOf(i).ToLowerInvariant() == text).ToList();
        if (exact.Count == 1) return exact[0];
        var part = ids.Where(i => i.Replace('-', ' ').Contains(text) || TitleOf(i).ToLowerInvariant().Contains(text)).ToList();
        if (part.Count == 1) return part[0];
        if (part.Count == 0) return null;
        Write("Which quest? " + string.Join(", ", part.Select(TitleOf)) + ".\n");
        return "";
    }

    private bool ShowQuest(string words)
    {
        var id = FindQuest(words, "active", "completed", "abandoned");
        if (id == "") return true;
        if (id == null) { Write($"You have no quest called \"{Escaped(words.Trim())}\". ({{cmd}}quests{{/}} lists yours.)\n"); return true; }
        Write(Story(id));
        SendBurstIfWanted();
        return true;
    }

    private string GivenBy(string id)
    {
        var giver = Fld(id, "giver");
        var area = Fld(id, "area");
        if (giver == "" && area == "") return "";
        return "Given by " + (giver == "" ? "someone" : Escaped(giver)) + (area != "" ? " in " + Escaped(area) : "") + ".";
    }

    private string Story(string id)
    {
        var state = StateOf(id);
        var text = new StringBuilder();
        text.Append("{b}" + Escaped(TitleOf(id)) + "{/} (" + state + ")\n");
        var given = GivenBy(id);
        if (state == "active")
        {
            var started = DateOf(Num(id, "started"));
            text.Append(Wrapped(given + (started != "" ? " Taken on " + started + "." : "")));
            var objectives = Objectives(id);
            if (objectives.Count > 0)
            {
                text.Append("Objectives:\n");
                foreach (var o in objectives)
                    text.Append(Wrapped($"[{(o.have >= o.need ? "x" : " ")}] {Escaped(o.text)}" + (o.need > 1 ? $" ({o.have} of {o.need})" : ""), "  "));
            }
            text.Append("Conversation:\n");
            var log = LogOf(id);
            if (Fld(id, "trimmed") == "1") text.Append(Wrapped(GapText, "  "));
            if (log.Count == 0) text.Append("  Nothing has been said yet.\n");
            var lastDate = "";
            foreach (var raw in log)
            {
                var e = Decode(raw);
                var date = DateOf(e.ts);
                if (date != "" && date != lastDate) { text.Append("  {b}" + Cap(date) + "{/}\n"); lastDate = date; }
                text.Append(Wrapped(Line(e), "  "));
            }
            return text.ToString();
        }
        var ended = DateOf(Num(id, "ended"));
        var verb = state == "completed" ? "Completed" : "Given up";
        text.Append(Wrapped(given + (ended != "" ? $" {verb} {ended}." : $" {verb}.")));
        if (state == "completed" && Fld(id, "reward") != "") text.Append(Wrapped("Reward: " + Escaped(Fld(id, "reward")) + "."));
        text.Append(Wrapped(Escaped(Fld(id, "summary"))));
        if (state == "abandoned") text.Append(Wrapped("Its giver will offer it again if you ask."));
        return text.ToString();
    }

    private static string Line(string raw) => Line(Decode(raw));

    private static string Line((int seq, long ts, string kind, string speaker, string text) e)
        => e.kind switch
        {
            "npc" => $"{{b}}{Escaped(e.speaker)}{{/}}: \"{e.text}\"",
            "you" => $"{{b}}You{{/}}: {e.text}",
            _ => "* " + e.text,
        };

    private string SummaryList(string state)
    {
        var ids = IdsIn(state);
        if (ids.Count == 0) return state == "completed" ? "You have completed no quests.\n" : "You have abandoned no quests.\n";
        var text = new StringBuilder(state == "completed" ? "Completed quests:\n" : "Abandoned quests:\n");
        foreach (var id in ids)
        {
            var date = DateOf(Num(id, "ended"));
            text.Append("{b}" + Escaped(TitleOf(id)) + "{/}" + (date != "" ? " - " + date : "") + "\n");
            var given = GivenBy(id);
            if (given != "") text.Append(Wrapped(given));
            if (state == "completed" && Fld(id, "reward") != "") text.Append(Wrapped("Reward: " + Escaped(Fld(id, "reward")) + "."));
            text.Append(Wrapped(Escaped(Fld(id, "summary"))));
        }
        return text.ToString();
    }

    // ------------------------------------------------------------------
    // Abandoning
    // ------------------------------------------------------------------

    private bool CmdAbandon(string args)
    {
        args = args.Trim();
        if (args == "") { Write("Abandon which quest? {cmd}abandon{/} NAME. ({cmd}quests{/} lists the quests you have.)\n"); return true; }
        Sync();
        var id = FindQuest(args, "active");
        if (id == "") return true;
        if (id == null) { Write($"You have no active quest called \"{Escaped(args)}\". ({{cmd}}quests{{/}} lists yours.)\n"); return true; }
        var def = Definition(id);
        if (Of(def, "abandonable") == "no") { Write($"\"{Escaped(TitleOf(id))}\" cannot be abandoned.\n"); return true; }
        if (QueryInCombat()) { Write("You cannot abandon a quest in the middle of a fight.\n"); return true; }
        var items = Of(def, "items");
        Write($"Abandon \"{Escaped(TitleOf(id))}\"? The conversation will be forgotten" + (items != "" ? ", and what you carry for it taken back" : "") +
              $", but {(Fld(id, "giver") != "" ? Escaped(Fld(id, "giver")) : "its giver")} will offer it again. ({{cmd}}y{{/}}/{{cmd}}n{{/}}) ");
        var asked = TitleOf(id);
        InputTo(answer => AbandonConfirmed(id, asked, answer));
        return true;
    }

    private void AbandonConfirmed(string id, string title, string answer)
    {
        var reply = answer.Trim().ToLowerInvariant();
        if (reply != "y" && reply != "yes") { Write("Not abandoned.\n"); return; }
        if (StateOf(id) != "active" || QueryQuest(id) != 1) { Write("That quest is no longer one you can abandon.\n"); return; }
        if (QueryInCombat()) { Write("You cannot abandon a quest in the middle of a fight. It stays as it was.\n"); return; }
        var def = Definition(id);
        if (Of(def, "abandonable") == "no") { Write($"\"{Escaped(title)}\" cannot be abandoned.\n"); return; }
        // What the quest holds: the items it made the character carry. Rewards already given are not taken back.
        var items = Of(def, "items");
        if (items != "")
            foreach (var path in items.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
            {
                var held = CountIn(Self, path).Values.Sum();
                if (held > 0) TakeFrom(Self, held, o => o.ProgramPath == path);
            }
        quests.Remove(id); // not started again: its giver offers it as to a newcomer
        var summary = Of(def, "abandoned");
        SetFld(id, "state", "abandoned");
        SetFld(id, "ended", Time().ToString());
        SetFld(id, "summary", Clean(summary != "" ? summary : Generated("abandoned", TitleOf(id), Fld(id, "giver")), MaxSummaryChars));
        SetFld(id, "reward", "");
        SetFld(id, "trimmed", "");
        SetFld(id, "prog", "");
        SetFld(id, "seq", "0");
        journalLog.Remove(id);
        ResetLog(id);
        MarkListChanged();
        Write($"You give up \"{Escaped(title)}\".\n" + Wrapped(Escaped(Fld(id, "summary"))));
    }

    // ------------------------------------------------------------------
    // GMCP: Char.Quests.List and Char.Quests.Log (docs/specs/gmcp.md)
    // ------------------------------------------------------------------

    private static int Bytes(string text)
    {
        var n = 0;
        foreach (var c in text) n += c < 0x80 ? 1 : c < 0x800 ? 2 : 3;
        return n;
    }

    private static int Estimate(object? value)
    {
        switch (value)
        {
            case null: return 4;
            case string s: return Bytes(s) + 4;
            case bool: return 6;
            case int or long: return 12;
            case Dictionary<string, object?> map: return 2 + map.Sum(e => Bytes(e.Key) + 6 + Estimate(e.Value));
            case List<object?> list: return 2 + list.Sum(e => Estimate(e) + 1);
            default: return 16;
        }
    }

    private Dictionary<string, object?> EntryData(string raw)
    {
        var e = Decode(raw);
        return new Dictionary<string, object?>
        {
            ["seq"] = e.seq, ["ts"] = e.ts, ["date"] = DateOf(e.ts), ["kind"] = e.kind, ["speaker"] = Plain(e.speaker), ["text"] = Plain(e.text),
        };
    }

    private List<Dictionary<string, object?>> LogData(string id)
    {
        var list = new List<Dictionary<string, object?>>();
        if (Fld(id, "trimmed") == "1")
            list.Add(new Dictionary<string, object?> { ["seq"] = 0, ["ts"] = 0L, ["date"] = "", ["kind"] = "gap", ["speaker"] = "", ["text"] = GapText });
        foreach (var raw in LogOf(id)) list.Add(EntryData(raw));
        return list;
    }

    private void SendLogEntry(string id, string raw)
    {
        if (!GmcpActive(Self)) return;
        GmcpSend(Self, "Char.Quests.Log", new Dictionary<string, object?>
        {
            ["id"] = id, ["reset"] = false, ["entries"] = new List<object?> { EntryData(raw) },
        });
    }

    /// <summary>The whole log of one quest, from its start: the first message has reset true, and the rest (a long log goes in several) append.</summary>
    private void ResetLog(string id)
    {
        if (!GmcpActive(Self)) return;
        var entries = StateOf(id) == "active" ? LogData(id) : new List<Dictionary<string, object?>>();
        var chunk = new List<object?>();
        var size = 0;
        var first = true;
        foreach (var entry in entries)
        {
            var cost = Estimate(entry) + 1;
            if (chunk.Count > 0 && (chunk.Count >= LogChunkEntries || size + cost > LogChunkBytes))
            {
                GmcpSend(Self, "Char.Quests.Log", new Dictionary<string, object?> { ["id"] = id, ["reset"] = first, ["entries"] = chunk });
                first = false;
                chunk = new List<object?>();
                size = 0;
            }
            chunk.Add(entry);
            size += cost;
        }
        if (chunk.Count > 0 || first) GmcpSend(Self, "Char.Quests.Log", new Dictionary<string, object?> { ["id"] = id, ["reset"] = first, ["entries"] = chunk });
    }

    private Dictionary<string, object?> ListData()
    {
        var items = new List<object?>();
        var budget = ListBudgetBytes;
        var truncated = false;
        foreach (var id in IdsIn("active", "completed", "abandoned").OrderBy(i => StateOf(i) == "active" ? 0 : StateOf(i) == "completed" ? 1 : 2))
        {
            var state = StateOf(id);
            var d = new Dictionary<string, object?>
            {
                ["id"] = id, ["name"] = Plain(TitleOf(id)), ["state"] = state, ["giver"] = Plain(Fld(id, "giver")), ["area"] = Plain(Fld(id, "area")),
                ["started"] = Num(id, "started"), ["started_date"] = DateOf(Num(id, "started")),
            };
            if (state == "active")
            {
                var objectives = Objectives(id);
                d["objective"] = string.Join(" ", objectives.Select(o => o.text));
                d["progress"] = ProgressText(objectives);
                d["objectives"] = objectives.Select(o => (object?)new Dictionary<string, object?> { ["text"] = o.text, ["have"] = o.have, ["need"] = o.need }).ToList();
                d["entries"] = LogOf(id).Count;
                d["seq"] = (int)Num(id, "seq");
            }
            else
            {
                d["ended"] = Num(id, "ended");
                d["ended_date"] = DateOf(Num(id, "ended"));
                d["summary"] = Plain(Fld(id, "summary"));
                if (state == "completed") d["reward"] = Plain(Fld(id, "reward"));
            }
            var cost = Estimate(d) + 1;
            if (cost > budget) { truncated = true; continue; }
            budget -= cost;
            items.Add(d);
        }
        return new Dictionary<string, object?> { ["quests"] = items, ["truncated"] = truncated };
    }

    /// <summary>The list changed (a state, an objective): it is sent soon, at most one in 250 ms.</summary>
    private void MarkListChanged()
    {
        listDirty = true;
        FlushList();
    }

    private void FlushList()
    {
        if (!listDirty || !GmcpActive(Self)) return;
        var wait = listAt + MinListGapMs - TimeMs();
        if (wait > 0)
        {
            if (!listScheduled)
            {
                listScheduled = true;
                CallOut(() => { listScheduled = false; FlushList(); }, wait / 1000.0 + 0.01);
            }
            return;
        }
        listDirty = false;
        listAt = TimeMs();
        GmcpSend(Self, "Char.Quests.List", ListData());
    }

    /// <summary>Everything again: the list, and the whole log of each active quest. At login, after a reconnect, and whenever "quests" is typed.</summary>
    private void SendBurst()
    {
        Sync();
        CheckProgress();
        listDirty = true;
        listAt = 0;
        FlushList();
        foreach (var id in IdsIn("active")) ResetLog(id);
    }

    /// <summary>
    /// Called by the body after every prompt and every heart beat, with whether the character has begun its adventure: sends the first
    /// burst of a GMCP connection, notes moved objectives, and sends a changed list.
    /// </summary>
    protected void JournalPoll(bool begun)
    {
        if (!begun || !Self.IsAlive) return;
        var on = GmcpActive(Self);
        if (!on) burstSent = false;
        else if (!burstSent) { burstSent = true; SendBurst(); }
        var now = TimeMs();
        if (now - polledAt < PollGapMs) return;
        polledAt = now;
        if (quests.Count == 0 && journal.Count == 0) return;
        Sync();
        CheckProgress();
        if (listDirty && on) FlushList();
    }
}
