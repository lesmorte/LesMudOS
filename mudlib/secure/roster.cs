/// <summary>
/// Who may see whom, and who has ever been made (docs/specs/who-and-visibility.md).
///
/// Visibility. A wizard may make their own body invisible (`invis`). The body tells this blueprint (SetInvisible) and this blueprint holds
/// the set; the body keeps nothing anyone else can read. Everything that shows characters to each other asks Visible(viewer, target):
/// the target is seen unless it is an invisible wizard and the viewer is not a wizard of at least its level. Sandbox code that asks
/// directly is told "visible", so it learns nothing from asking; real code that asks for its own viewer gets the truth.
/// A wearable that hides its wearer from the who list is a different thing and lives with the item (QueryHidesFromWho): WhoMarks adds it.
///
/// The list of every character (`all-players`, level 10). The index below is in memory: name, level, calling, class, dates. It is built at
/// boot from the save files, 40 at a time, and kept up to date by the bodies themselves (Note). The saves are read through RestoreObject
/// into the few fields declared here, which have the names of the player's own; the master lets this blueprint read /players/ and nothing
/// else, so a password hash or a keyed address (fields this object does not declare) is never loaded by anything but the player's own body.
/// </summary>
public class Roster : MudObject
{
    private const string WizList = "/secure/wizlist";
    private const string PlayerProgram = "/std/player";
    private const string CommProgram = "/secure/comm";
    private const string SaveDir = "/players";
    private const int PerSlice = 40;
    private const double SliceSeconds = 0.25;
    private const int PageSize = 40;
    private const int TopLevel = 10;

    // ---- The fields a save is read into (names as in /std/player and its bases) ----
    [Saved] private int Level;
    [Saved] private string archetype = "";
    [Saved] private string advancedClass = "";
    [Saved] private string subclass = "";
    [Saved] private string prestigeClass = "";
    [Saved] private int logins;
    [Saved] private long lastLogin;
    [Saved] private long created;

    private class Entry
    {
        public string Name = "";
        public int Level;
        public string Archetype = "", Advanced = "", Subclass = "", Prestige = "";
        public long Created, LastLogin;
    }

    private readonly Dictionary<string, Entry> index = new();
    private readonly Queue<string> pending = new();
    private readonly HashSet<string> queued = new();
    private int pump;
    private int knownTotal;
    private readonly List<string> logQueue = new();

    // ---- Invisibility ----
    private readonly Dictionary<string, ObjectRef> unseen = new();
    private readonly List<(ObjectRef Wizard, ObjectRef Other)> spoke = new();

    public override void Create()
    {
        if (Self.IsClone) return;
        try { Refresh(); }
        catch (MudRuntimeException e) { Log("roster: could not list the saves: " + e.Message); }
    }

    // ------------------------------------------------------------------
    // Who is calling
    // ------------------------------------------------------------------

    private static bool IsBody(ObjectRef? ob) => ob != null && ob.IsAlive && ob.IsClone && ob.ProgramPath == PlayerProgram && ob.Domain == "";

    private int WizLevelOf(ObjectRef? body) => IsBody(body) ? LoadObject(WizList).Call<int>("QueryLevel", body!.Call<string>("QueryName") ?? "") : 0;

    private static string KeyOf(ObjectRef body) => (body.Call<string>("QueryName") ?? "").ToLowerInvariant();

    // ------------------------------------------------------------------
    // Visibility
    // ------------------------------------------------------------------

    /// <summary>Is this body an invisible wizard now (registered, still a wizard, still alive)? Prunes what is no longer true.</summary>
    private bool IsUnseen(ObjectRef? target)
    {
        if (target == null || unseen.Count == 0 || !IsBody(target)) return false;
        var key = KeyOf(target);
        if (!unseen.TryGetValue(key, out var held)) return false;
        if (held != target || !held.IsAlive) { if (!held.IsAlive) unseen.Remove(key); return false; }
        if (WizLevelOf(target) < 1) { unseen.Remove(key); return false; } // no longer a wizard
        return true;
    }

    /// <summary>May this viewer see this target's body?</summary>
    private bool Sees(ObjectRef? viewer, ObjectRef target)
    {
        if (viewer == target || !IsUnseen(target)) return true;
        var mine = WizLevelOf(viewer);
        return mine >= 1 && mine >= WizLevelOf(target);
    }

    /// <summary>
    /// Can this viewer see that body? True for anything but an invisible wizard the viewer does not outrank or equal. A call straight
    /// from sandbox code is answered "visible": nothing can be learned by asking.
    /// </summary>
    public bool Visible(ObjectRef? viewer, ObjectRef? target)
    {
        if (target == null || unseen.Count == 0) return true;
        if (PreviousObject() is { } caller && caller.Domain != "") return true;
        return Sees(viewer, target);
    }

    /// <summary>
    /// A wizard's own body turns its invisibility on or off (at its command, and again at login for a saved state). True if the state
    /// holds afterwards. Only a real player body, and only with no sandbox code on the stack; only a wizard can be made invisible.
    /// </summary>
    public bool SetInvisible(bool on)
    {
        var caller = PreviousObject();
        if (Self.IsClone || !IsBody(caller) || RestrictedCaller() != null) return false;
        var key = KeyOf(caller!);
        if (key == "") return false;
        if (on && WizLevelOf(caller) < 1) return false;
        if (on) unseen[key] = caller!; else unseen.Remove(key);
        spoke.RemoveAll(p => p.Wizard == caller || !p.Wizard.IsAlive || !p.Other.IsAlive);
        return on;
    }

    /// <summary>The marks `who` shows after a character's name for this viewer ("", " (invis)", " (hidden)", both), or null if the viewer must not see the character listed. Only a body asking for itself.</summary>
    public string? WhoMarks(ObjectRef viewer, ObjectRef target)
    {
        if (Self.IsClone || PreviousObject() != viewer || !IsBody(viewer) || !IsBody(target) || RestrictedCaller() != null) return null;
        var marks = "";
        if (IsUnseen(target))
        {
            if (!Sees(viewer, target)) return null;
            marks += " (invis)";
        }
        if (target.Call<bool>("QueryHiddenFromWho"))
        {
            if (viewer != target && WizLevelOf(viewer) < 1) return null;
            marks += " (hidden)";
        }
        return marks;
    }

    /// <summary>/secure/comm says that a wizard has spoken to a character (a tell, reply or whisper), so that the character may answer by name.</summary>
    public void NoteSpoke(ObjectRef wizard, ObjectRef other)
    {
        if (!IsCaller(CommProgram) || !IsUnseen(wizard) || !IsBody(other) || spoke.Any(p => p.Wizard == wizard && p.Other == other)) return;
        spoke.RemoveAll(p => !p.Wizard.IsAlive || !p.Other.IsAlive);
        if (spoke.Count < 2000) spoke.Add((wizard, other));
    }

    /// <summary>Has this wizard spoken to this character in this session? Only /secure/comm asks.</summary>
    public bool Spoke(ObjectRef wizard, ObjectRef other)
        => IsCaller(CommProgram) && wizard != null && other != null && spoke.Any(p => p.Wizard == wizard && p.Other == other && p.Wizard.IsAlive && p.Other.IsAlive);

    private bool IsCaller(string program) => !Self.IsClone && PreviousObject() is { } c && !c.IsClone && c.Domain == "" && c.ProgramPath == program && RestrictedCaller() == null;

    // ------------------------------------------------------------------
    // The index of every character
    // ------------------------------------------------------------------

    private void Log(string line)
    {
        logQueue.Add(line);
        while (logQueue.Count > 100) logQueue.RemoveAt(0);
        if (ThisPlayer() == null) FlushLog();
        else CallOut(FlushLog, 1);
    }

    private void FlushLog()
    {
        if (ThisPlayer() != null) { CallOut(FlushLog, 1); return; }
        foreach (var line in logQueue) Write(line + "\n");
        logQueue.Clear();
    }

    /// <summary>List the save directory: forget characters whose file is gone, queue the ones the index does not know.</summary>
    private void Refresh()
    {
        var names = SaveNames(SaveDir);
        knownTotal = names.Count;
        var present = new HashSet<string>(names);
        foreach (var gone in index.Keys.Where(k => !present.Contains(k)).ToList()) index.Remove(gone);
        foreach (var name in names)
            if (!index.ContainsKey(name) && queued.Add(name)) pending.Enqueue(name);
        if (pending.Count > 0 && pump == 0) pump = CallOut(Pump, SliceSeconds);
    }

    private void Pump()
    {
        pump = 0;
        for (var i = 0; i < PerSlice && pending.Count > 0; i++)
        {
            var name = pending.Dequeue();
            queued.Remove(name);
            ReadSave(name);
        }
        if (pending.Count > 0) pump = CallOut(Pump, SliceSeconds);
    }

    private void ReadSave(string name)
    {
        Level = 0; archetype = ""; advancedClass = ""; subclass = ""; prestigeClass = ""; logins = 0; lastLogin = 0; created = 0;
        try { RestoreObject(SaveDir + "/" + name); }
        catch (MudRuntimeException) { /* an unreadable file is listed with what is known: its name */ }
        var live = index.GetValueOrDefault(name);
        index[name] = new Entry
        {
            Name = name, Level = Level, Archetype = archetype, Advanced = advancedClass, Subclass = subclass, Prestige = prestigeClass,
            Created = created, LastLogin = lastLogin,
        };
        if (live != null && live.LastLogin > index[name].LastLogin) index[name].LastLogin = live.LastLogin;
        Level = 0; archetype = ""; advancedClass = ""; subclass = ""; prestigeClass = ""; logins = 0; lastLogin = 0; created = 0; // nothing of a save is kept in the fields
    }

    /// <summary>What a live body says of itself, as an index entry.</summary>
    private static Entry Capture(ObjectRef body) => new Entry
    {
        Name = KeyOf(body), Level = body.Call<int>("QueryLevel"), Archetype = body.Call<string>("QueryArchetype") ?? "",
        Advanced = body.Call<string>("QueryClassOfTier", "advanced") ?? "", Subclass = body.Call<string>("QueryClassOfTier", "subclass") ?? "",
        Prestige = body.Call<string>("QueryClassOfTier", "prestige") ?? "", Created = body.Call<long>("QueryCreated"), LastLogin = body.Call<long>("QueryLastLogin"),
    };

    /// <summary>A player body says what is true of it now (at login, creation and logout). Only a real body, about itself.</summary>
    public void Note()
    {
        var body = PreviousObject();
        if (Self.IsClone || !IsBody(body) || RestrictedCaller() != null) return;
        var name = KeyOf(body!);
        if (name.Length < 2) return;
        index[name] = Capture(body!);
    }

    // ------------------------------------------------------------------
    // all-players
    // ------------------------------------------------------------------

    private static string Cap(string name) => name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);

    /// <summary>A date as year-month-day in UTC (the civil calendar from a day number), or "before records began" for none.</summary>
    private static string DateText(long seconds)
    {
        if (seconds <= 0) return "before records began";
        var z = seconds / 86400 + 719468;
        var era = (z >= 0 ? z : z - 146096) / 146097;
        var doe = z - era * 146097;
        var yoe = (doe - doe / 1460 + doe / 36524 - doe / 146096) / 365;
        var y = yoe + era * 400;
        var doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
        var mp = (5 * doy + 2) / 153;
        var d = doy - (153 * mp + 2) / 5 + 1;
        var m = mp < 10 ? mp + 3 : mp - 9;
        if (m <= 2) y++;
        return $"{y:0000}-{m:00}-{d:00}";
    }

    private static string Fit(string text, int width) => text.Length <= width ? text.PadRight(width) : text.Substring(0, Math.Max(1, width - 1)) + ".";

    private string ClassName(ObjectRef registry, string id, bool title)
    {
        if (id == "") return "";
        try { return registry.Call<ObjectRef>("Class", id)?.Call<string>(title ? "QueryTitle" : "QueryClassName") ?? ""; }
        catch (MudRuntimeException) { return ""; }
    }

    /// <summary>
    /// "all-players [recent|new|PREFIX] [PAGE]": the list of every character ever made. Null (the words for an unknown command) unless the
    /// caller is the real body of a level 10 wizard, acting on their own command, with no sandbox code anywhere on the stack.
    /// </summary>
    public string? AllPlayers(string args)
    {
        var caller = PreviousObject();
        if (Self.IsClone || !IsBody(caller) || caller != ThisPlayer() || RestrictedCaller() != null || WizLevelOf(caller) < TopLevel) return null;
        try { Refresh(); } catch (MudRuntimeException) { }
        Log($"all-players: {caller!.Call<string>("QueryName")}");

        var online = new HashSet<string>();
        foreach (var user in Users())
            if (IsBody(user) && KeyOf(user).Length >= 2) { online.Add(KeyOf(user)); index[KeyOf(user)] = Capture(user); } // the ones in the game are read live
        foreach (var dropped in LoadObject("/secure/linkdead").Call<List<ObjectRef>>("All") ?? new List<ObjectRef>())
            if (IsBody(dropped) && KeyOf(dropped).Length >= 2) { online.Add(KeyOf(dropped)); index[KeyOf(dropped)] = Capture(dropped); }

        var order = "name";
        var prefix = "";
        var page = 1;
        foreach (var word in args.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var w = word.ToLowerInvariant();
            if (w == "recent" || w == "new") order = w;
            else if (w.All(char.IsDigit) && w.Length <= 6) page = Math.Max(1, int.Parse(w));
            else if (w.All(char.IsLetter) && w.Length <= 16) prefix = w;
            else return "Usage: {cmd}all-players{/} [{cmd}recent{/}|{cmd}new{/}|NAME-PREFIX] [PAGE]\n";
        }

        var rows = index.Values.Where(e => prefix == "" || e.Name.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        if (order == "recent") rows = rows.OrderByDescending(e => e.LastLogin).ThenBy(e => e.Name, StringComparer.Ordinal).ToList();
        else if (order == "new") rows = rows.OrderByDescending(e => e.Created).ThenBy(e => e.Name, StringComparer.Ordinal).ToList();
        else rows = rows.OrderBy(e => e.Name, StringComparer.Ordinal).ToList();

        var pages = Math.Max(1, (rows.Count + PageSize - 1) / PageSize);
        if (page > pages) page = pages;
        var shown = rows.Skip((page - 1) * PageSize).Take(PageSize).ToList();


        var registry = LoadObject("/secure/classes");
        var wiz = LoadObject(WizList);
        var calling = new Dictionary<string, string>();
        var klass = new Dictionary<string, string>();
        foreach (var e in shown)
        {
            calling[e.Name] = ClassName(registry, e.Archetype, false);
            var top = e.Prestige != "" ? e.Prestige : e.Subclass != "" ? e.Subclass : e.Advanced;
            klass[e.Name] = ClassName(registry, top, true);
        }
        var wName = Math.Max(4, shown.Select(e => e.Name.Length).DefaultIfEmpty(4).Max());
        var wCalling = Math.Max(7, Math.Min(10, shown.Select(e => calling[e.Name].Length).DefaultIfEmpty(7).Max()));
        var wCreated = shown.Any(e => e.Created <= 0) ? 20 : 10;
        var fixedWidth = wName + 1 + wCalling + 1 + 3 + 1 + 3 + 1 + wCreated + 1 + 10 + 1 + 3 + 1;
        var wClass = Math.Max(5, Math.Min(14, 80 - fixedWidth));
        wClass = Math.Min(wClass, Math.Max(5, shown.Select(e => klass[e.Name].Length).DefaultIfEmpty(5).Max()));

        var text = new System.Text.StringBuilder();
        text.Append("Name".PadRight(wName) + " " + "Calling".PadRight(wCalling) + " " + "Class".PadRight(wClass) + " Lvl Wiz " + "Created".PadRight(wCreated) + " Last login Now\n");
        foreach (var e in shown)
        {
            var level = wiz.Call<int>("QueryLevel", e.Name);
            text.Append(Cap(e.Name).PadRight(wName) + " " + Fit(calling[e.Name] == "" ? "-" : calling[e.Name], wCalling) + " " + Fit(klass[e.Name] == "" ? "-" : klass[e.Name], wClass)
                        + " " + e.Level.ToString().PadLeft(3) + " " + (level > 0 ? level.ToString() : "-").PadLeft(3) + " " + DateText(e.Created).PadRight(wCreated)
                        + " " + (e.LastLogin > 0 ? DateText(e.LastLogin) : "-").PadRight(10) + " " + (online.Contains(e.Name) ? "yes" : "-") + "\n");
        }
        text.Append($"{rows.Count} character(s)" + (prefix != "" ? $" beginning with {prefix}" : "") + $". Page {page} of {pages}." + (page < pages ? $" Type {{cmd}}all-players {page + 1}{{/}} for the next." : "") + "\n");
        if (pending.Count > 0) text.Append($"The list is still being read ({index.Count} of {knownTotal} characters so far); try again in a moment.\n");
        return text.ToString();
    }
}
