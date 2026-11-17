// @inherit /std/thing

/// <summary>
/// The player body. Its own commands are ordinary AddAction bindings, added
/// once at login. Everything else a player can do is offered by the room and
/// the objects around them.
/// </summary>
public class Player : Thing
{
    private const string StartRoom = "/domains/town/square";
    private const string WizList = "/secure/wizlist";

    // Wizard level needed for each wizard command. Level 0 has none of them.
    private const int LevelFiles = 1; // pwd, cd, list, cat
    private const int LevelEdit = 5; // mkdir, edit
    private const int LevelSystemFiles = 10; // mkdir and edit under /secure and /std
    private const int MaxEditLines = 5000;
    private const int LevelMudstat = 1;
    private const int LevelClone = 3;
    private const int LevelUpdate = 5;
    private const int LevelShutdown = 10;

    [Saved] private string title = "the newcomer";
    [Saved] private int logins;
    [Saved] private long lastLogin;
    [Saved] private string cwd = "/"; // the mudlib directory a wizard is "in"

    private bool ready;

    // Editor session; edLines is null when the player is not in the editor.
    private List<string>? edLines;
    private string edPath = "";
    private bool edDirty;
    private int edInsertAt = -1; // >= 0 while typed lines are being inserted at that index

    private string SaveFile => "/players/" + Name.ToLower();

    public override string? Short() => $"{Name} {title}";

    public override string Long() => $"{Name} {title}, who has visited {logins} time(s).\n";

    /// <summary>Called once by /secure/login right after the connection is handed over.</summary>
    public void SetupPlayer(string name)
    {
        if (ready) return;
        ready = true;
        Name = name;

        var returning = RestoreObject(SaveFile);
        logins++;
        lastLogin = Time();

        EnableCommands();
        AddAction("look", CmdLook);
        AddAction("l", CmdLook);
        AddAction("say", CmdSay);
        AddAction("get", CmdGet);
        AddAction("take", CmdGet);
        AddAction("drop", CmdDrop);
        AddAction("inventory", CmdInventory);
        AddAction("i", CmdInventory);
        AddAction("who", CmdWho);
        AddAction("title", CmdTitle);
        AddAction("save", CmdSave);
        AddAction("quit", CmdQuit);
        AddAction("help", CmdHelp);
        // Wizard commands. Each handler checks the caller's level on every use
        // and declines below it, so to a non-wizard these verbs do not exist.
        AddAction("promote", CmdPromote);
        AddAction("wizards", CmdWizards);
        AddAction("pwd", CmdPwd);
        AddAction("cd", CmdCd);
        AddAction("list", CmdList);
        AddAction("ls", CmdList);
        AddAction("cat", CmdCat);
        AddAction("mkdir", CmdMkdir);
        AddAction("edit", CmdEdit);
        AddAction("update", CmdUpdate);
        AddAction("clone", CmdClone);
        AddAction("mudstat", CmdMudstat);
        AddAction("shutdown", CmdShutdown);

        Write(returning ? $"\nWelcome back, {Name}. This is visit number {logins}.\n\n" : $"\nWelcome, {Name}. Type 'help' for commands.\n\n");
        MoveObject(LoadObject(StartRoom));
        Say($"{Name} enters the game.\n");
        Look();
    }

    /// <summary>This player's wizard level, 0 to 10, as held by the wizard registry.</summary>
    public int QueryWizLevel() => LoadObject(WizList).Call<int>("QueryLevel", Name);

    private bool IsWizard(int level) => QueryWizLevel() >= level;

    public void Look()
    {
        var room = Environment();
        TellObject(Self, room == null ? "You are nowhere.\n" : room.Call<string>("Long") ?? "You see nothing.\n");
    }

    // ---- Player commands ----

    private bool CmdLook(string args)
    {
        if (args.StartsWith("at ")) args = args.Substring(3);
        if (args == "") { Look(); return true; }
        var room = Environment();
        var target = Present(args) ?? (room == null ? null : Present(args, room));
        if (target == null) { NotifyFail($"You see no '{args}' here."); return false; }
        Write(target.Call<string>("Long") ?? "You see nothing special.\n");
        return true;
    }

    private bool CmdSay(string args)
    {
        if (args == "") { NotifyFail("Say what?"); return false; }
        Write($"You say: {args}\n");
        Say($"{Name} says: {args}\n");
        return true;
    }

    private bool CmdGet(string args)
    {
        var room = Environment();
        var item = room == null ? null : Present(args, room);
        if (item == null || item == Self) { NotifyFail("Get what?"); return false; }
        if (!item.Call<bool>("CanGet")) { Write("You can't take that.\n"); return true; }
        MoveObject(item, Self);
        Write($"You take {item.Call<string>("Short")}.\n");
        Say($"{Name} takes {item.Call<string>("Short")}.\n");
        return true;
    }

    private bool CmdDrop(string args)
    {
        var room = Environment();
        var item = Present(args);
        if (item == null || room == null) { NotifyFail("Drop what?"); return false; }
        MoveObject(item, room);
        Write($"You drop {item.Call<string>("Short")}.\n");
        Say($"{Name} drops {item.Call<string>("Short")}.\n");
        return true;
    }

    private bool CmdInventory(string args)
    {
        var items = AllInventory().Select(i => i.Call<string>("Short")).Where(s => s != null).ToList();
        Write(items.Count == 0 ? "You are empty-handed.\n" : "You are carrying: " + string.Join(", ", items) + ".\n");
        return true;
    }

    private bool CmdWho(string args)
    {
        var users = Users()
            .Where(u => u.Call<string>("Short") != null)
            .Select(u => u.Call<string>("Short") + (u.Call<int>("QueryWizLevel") > 0 ? $" [wizard {u.Call<int>("QueryWizLevel")}]" : ""))
            .ToList();
        Write($"{users.Count} player(s) online:\n  " + string.Join("\n  ", users) + "\n");
        return true;
    }

    private bool CmdTitle(string args)
    {
        if (args == "" || args.Length > 40) { NotifyFail("Usage: title <up to 40 characters>"); return false; }
        title = args;
        Write($"You are now {Short()}.\n");
        return true;
    }

    private bool CmdSave(string args)
    {
        SaveObject(SaveFile);
        Write("Saved.\n");
        return true;
    }

    private bool CmdQuit(string args)
    {
        SaveObject(SaveFile);
        Write("Goodbye.\n");
        Say($"{Name} leaves the game.\n");
        Destruct(); // carried items fall to the floor; the connection closes
        return true;
    }

    private bool CmdHelp(string args)
    {
        Write(
            "Commands: look [at <thing>], say <text>, get <thing>, drop <thing>, inventory,\n" +
            "          who, title <text>, save, quit, and the exits of the room you are in.\n" +
            "          Objects nearby add their own verbs (try the lantern and the cat).\n");

        var level = QueryWizLevel();
        if (level < 1) return true;
        Write($"Wizard commands at your level ({level}):\n" +
              "          promote <player> <level>   set a lower wizard's level, up to your own\n" +
              "          wizards                    list all wizards\n" +
              "          pwd                        show the mudlib directory you are in\n" +
              "          cd [<dir>]                 change directory (.. goes up, no argument goes to /)\n" +
              "          list [<dir>]               list a directory (also: ls)\n" +
              "          cat <file>                 show a file with line numbers\n");
        if (level >= LevelMudstat) Write("          mudstat                    driver statistics\n");
        if (level >= LevelClone) Write("          clone <path>               clone an object into your inventory or room\n");
        if (level >= LevelEdit) Write(
              "          mkdir <dir>                create a directory\n" +
              "          edit <file>                create or change a file in the line editor\n" +
              "                                     (level 10 for files under /secure and /std)\n");
        if (level >= LevelUpdate) Write("          update <path>              recompile a file and replace the loaded object\n");
        if (level >= LevelShutdown) Write("          shutdown                   stop the driver\n");
        return true;
    }

    // ---- Wizard commands ----

    /// <summary>
    /// "promote <player> <level>" sets another player's wizard level; the
    /// registry enforces who may set what. "promote <secret>" claims level 10
    /// with the driver's admin secret, which only works while no level-10
    /// wizard exists. To anyone who is not a wizard and does not know the
    /// secret, the command behaves as if it did not exist.
    /// </summary>
    private bool CmdPromote(string args)
    {
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var registry = LoadObject(WizList);
        var level = 0;
        var isPromotion = words.Length == 2 && int.TryParse(words[1], out level);

        // Anything that is not "<player> <number>" is treated as the secret, spaces and all.
        if (!isPromotion && args != "" && registry.Call<bool>("Bootstrap", args))
        {
            Write("The secret is accepted. You are now a level 10 wizard. Type 'help'.\n");
            return true;
        }
        if (!IsWizard(1)) return false;
        if (!isPromotion)
        {
            NotifyFail("Usage: promote <player> <level>");
            return false;
        }
        var refusal = registry.Call<string>("Promote", words[0], level);
        Write(refusal == null ? $"{words[0]} is now wizard level {level}.\n" : refusal + "\n");
        return true;
    }

    /// <summary>
    /// Turn what a wizard typed into an absolute mudlib path: relative paths
    /// start from the current directory, and "." and ".." are resolved here so
    /// that the driver only ever sees a clean absolute path.
    /// </summary>
    private string ResolvePath(string path)
    {
        var parts = new List<string>();
        var start = path.StartsWith("/") ? "" : cwd;
        foreach (var part in (start + "/" + path).Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == ".") continue;
            if (part == "..") { if (parts.Count > 0) parts.RemoveAt(parts.Count - 1); continue; }
            parts.Add(part);
        }
        return "/" + string.Join("/", parts);
    }

    private bool CmdPwd(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        Write(cwd + "\n");
        return true;
    }

    private bool CmdCd(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        var target = ResolvePath(args == "" ? "/" : args);
        if (GetDir(target) == null) { Write($"{target}: no such directory.\n"); return true; }
        cwd = target;
        Write(cwd + "\n");
        return true;
    }

    private bool CmdList(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        var target = ResolvePath(args);
        var entries = GetDir(target);
        if (entries == null) { Write($"{target}: no such directory.\n"); return true; }

        var text = new StringBuilder($"{target}:\n");
        foreach (var entry in entries)
            text.Append(entry.IsDirectory ? $"  {entry.Name}/\n" : $"  {entry.Name,-28} {entry.Size,8}\n");
        if (entries.Count == 0) text.Append("  (empty)\n");
        Write(text.ToString());
        return true;
    }

    // ---- Wizard commands: files ----

    /// <summary>A file name as typed, as an absolute mudlib path. ".cs" is assumed if no extension is given.</summary>
    private string ResolveFile(string name)
    {
        var path = ResolvePath(name);
        var last = path.Substring(path.LastIndexOf('/') + 1);
        return last == "" || last.Contains('.') ? path : path + ".cs";
    }

    /// <summary>
    /// The level needed to change a path. The code under /secure and /std is
    /// what enforces wizard levels, so only the top level may change it.
    /// </summary>
    private static int LevelToWrite(string path)
        => (path + "/").StartsWith("/secure/") || (path + "/").StartsWith("/std/") ? LevelSystemFiles : LevelEdit;

    private static List<string> SplitLines(string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
        if (lines.Count > 0 && lines[lines.Count - 1] == "") lines.RemoveAt(lines.Count - 1);
        return lines;
    }

    private static string Numbered(List<string> lines, int first, int last)
    {
        var text = new StringBuilder();
        for (var i = first; i <= last; i++) text.Append($"{i,4}  {lines[i - 1]}\n");
        return text.ToString();
    }

    private bool CmdCat(string args)
    {
        if (!IsWizard(LevelFiles)) return false;
        if (args == "") { NotifyFail("Usage: cat <file>"); return false; }
        var file = ResolveFile(args);
        var text = ReadFile(file);
        if (text == null) { Write($"{file}: no such file.\n"); return true; }
        var lines = SplitLines(text);
        Write($"{file} ({lines.Count} lines):\n" + Numbered(lines, 1, lines.Count));
        return true;
    }

    private bool CmdMkdir(string args)
    {
        if (!IsWizard(LevelEdit)) return false;
        if (args == "") { NotifyFail("Usage: mkdir <dir>"); return false; }
        var target = ResolvePath(args);
        if (!IsWizard(LevelToWrite(target))) { Write($"You need wizard level {LevelSystemFiles} to change /secure and /std.\n"); return true; }
        Write(MakeDir(target) ? $"Created {target}/\n" : $"{target} already exists.\n");
        return true;
    }

    /// <summary>
    /// A small line editor. While it is active every line the player types
    /// goes to EdInput instead of the command parser.
    /// </summary>
    private bool CmdEdit(string args)
    {
        if (!IsWizard(LevelEdit)) return false;
        if (args == "") { NotifyFail("Usage: edit <file>"); return false; }
        var file = ResolveFile(args);
        if (!IsWizard(LevelToWrite(file))) { Write($"You need wizard level {LevelSystemFiles} to change /secure and /std.\n"); return true; }

        var text = ReadFile(file);
        var slash = file.LastIndexOf('/');
        if (text == null && GetDir(slash == 0 ? "/" : file.Substring(0, slash)) == null)
        {
            Write($"{file}: no such directory. Create it with mkdir first.\n");
            return true;
        }

        edLines = text == null ? new List<string>() : SplitLines(text);
        edPath = file;
        edDirty = false;
        edInsertAt = -1;
        Write(text == null ? $"New file {file}.\n" : $"Editing {file}, {edLines.Count} lines.\n");
        Write("You are in the editor. h for help, a to add text, x to save and leave.\n");
        EdPrompt();
        return true;
    }

    private const string EdHelp =
        "Editor commands (n and m are line numbers):\n" +
        "  p [n [m]]   print the file, one line, or lines n to m\n" +
        "  a [n]       add lines after line n (default: at the end)\n" +
        "  i [n]       insert lines before line n (default: at the top)\n" +
        "  c n [m]     replace line n, or lines n to m, with new lines\n" +
        "  d n [m]     delete line n, or lines n to m\n" +
        "  w           write the file\n" +
        "  x           write the file and leave\n" +
        "  q           leave (q! to discard unsaved changes)\n" +
        "While adding lines, everything you type or paste goes into the file.\n" +
        "A line containing only a full stop ends the input.\n";

    private void EdPrompt()
    {
        if (edInsertAt < 0) Write(":");
        InputTo(EdInput);
    }

    private void EdInput(string line)
    {
        if (edLines == null) return;

        if (edInsertAt >= 0)
        {
            if (line.Trim() == ".") edInsertAt = -1;
            else if (edLines.Count >= MaxEditLines) { Write($"The editor holds at most {MaxEditLines} lines.\n"); edInsertAt = -1; }
            else { edLines.Insert(edInsertAt++, line.TrimEnd()); edDirty = true; }
            EdPrompt();
            return;
        }

        var words = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = words.Length == 0 ? "" : words[0];
        var count = edLines.Count;
        var first = 0;
        var last = 0;
        var hasFirst = words.Length > 1 && int.TryParse(words[1], out first);
        if (!(words.Length > 2 && int.TryParse(words[2], out last))) last = first;
        var validRange = hasFirst && first >= 1 && last >= first && last <= count;
        const string BadRange = "No such line. p shows the line numbers.\n";

        switch (command)
        {
            case "":
                break;
            case "h":
                Write(EdHelp);
                break;
            case "p":
                if (!hasFirst) Write(count == 0 ? "(empty)\n" : Numbered(edLines, 1, count));
                else if (validRange) Write(Numbered(edLines, first, last));
                else Write(BadRange);
                break;
            case "a":
                if (!hasFirst) first = count;
                if (first < 0 || first > count) { Write(BadRange); break; }
                edInsertAt = first;
                Write("Adding lines. End with a line containing only .\n");
                break;
            case "i":
                if (!hasFirst) first = 1;
                if (first < 1 || first > count + 1) { Write(BadRange); break; }
                edInsertAt = first - 1;
                Write("Inserting lines. End with a line containing only .\n");
                break;
            case "c":
                if (!validRange) { Write(BadRange); break; }
                edLines.RemoveRange(first - 1, last - first + 1);
                edDirty = true;
                edInsertAt = first - 1;
                Write("Replacing. End with a line containing only .\n");
                break;
            case "d":
                if (!validRange) { Write(BadRange); break; }
                edLines.RemoveRange(first - 1, last - first + 1);
                edDirty = true;
                Write($"Deleted {last - first + 1} line(s).\n");
                break;
            case "w":
                EdSave();
                break;
            case "x":
                if (EdSave()) { EdQuit(); return; }
                break;
            case "q":
                if (!edDirty) { EdQuit(); return; }
                Write("There are unsaved changes: w to save, q! to discard them.\n");
                break;
            case "q!":
                EdQuit();
                return;
            default:
                Write("Unknown editor command. h for help.\n");
                break;
        }
        EdPrompt();
    }

    private bool EdSave()
    {
        if (edLines == null) return false;
        // Checked again here: the level may have changed since the editor was opened.
        if (!IsWizard(LevelToWrite(edPath))) { Write("You may no longer write that file.\n"); return false; }
        try
        {
            WriteFile(edPath, string.Join("\n", edLines) + "\n");
        }
        catch (MudRuntimeException refusal)
        {
            Write(refusal.Message + "\n");
            return false;
        }
        edDirty = false;
        Write($"Wrote {edPath}, {edLines.Count} lines.\n");
        if (edPath.EndsWith(".cs")) Write($"Load it with: update {edPath.Substring(0, edPath.Length - 3)}\n");
        return true;
    }

    private void EdQuit()
    {
        edLines = null;
        edInsertAt = -1;
        Write("Left the editor.\n");
    }

    private bool CmdWizards(string args)
    {
        if (!IsWizard(1)) return false;
        var wizards = LoadObject(WizList).Call<List<string>>("QueryWizards") ?? new List<string>();
        Write("Wizards: " + (wizards.Count == 0 ? "none" : string.Join(", ", wizards)) + "\n");
        return true;
    }

    /// <summary>
    /// The LPmud "update": recompile a source file. If its blueprint object is
    /// loaded, move the living things out, destruct the old object together
    /// with its other contents, load the new one (whose Create() restocks it),
    /// and move the living things back in.
    /// </summary>
    private bool CmdUpdate(string args)
    {
        if (!IsWizard(LevelUpdate)) return false;
        if (args == "") { NotifyFail("Usage: update <path>"); return false; }

        args = ResolvePath(args); // relative to the current directory
        var old = FindObject(args);
        var stale = UpdateProgram(args); // throws with compiler errors; nothing has been touched yet
        Write($"Recompiled {args}; {stale.Count} program(s) invalidated: {string.Join(", ", stale)}\n");
        if (old == null) return true;

        var occupants = AllInventory(old).Where(Living).ToList();
        var limbo = LoadObject("/std/void");
        foreach (var ob in occupants) MoveObject(ob, limbo);
        Destruct(old);

        var fresh = LoadObject(args);
        foreach (var ob in occupants)
            if (ob.IsAlive) MoveObject(ob, fresh);
        Write($"Replaced {fresh.Name}; moved {occupants.Count} occupant(s) back in.\n");
        return true;
    }

    private bool CmdClone(string args)
    {
        if (!IsWizard(LevelClone)) return false;
        if (args == "") { NotifyFail("Usage: clone <path>"); return false; }
        var ob = CloneObject(ResolvePath(args));
        var room = Environment();
        MoveObject(ob, ob.Call<bool>("CanGet") || room == null ? Self : room);
        Write($"Cloned {ob.Name}.\n");
        return true;
    }

    private bool CmdMudstat(string args)
    {
        if (!IsWizard(LevelMudstat)) return false;
        Write(DriverStatus());
        return true;
    }

    private bool CmdShutdown(string args)
    {
        if (!IsWizard(LevelShutdown)) return false;
        foreach (var user in Users()) TellObject(user, $"{Name} shuts the game down.\n");
        Shutdown();
        return true;
    }

    public override void NetDead()
    {
        if (!ready) return;
        SaveObject(SaveFile);
        Say($"{Name} disconnects.\n");
        Destruct();
    }
}
