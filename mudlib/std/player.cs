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
    private const int LevelEdit = 5; // mkdir, edit, copy, move, rename, delete, rmdir
    private const int LevelSystemFiles = 10; // mkdir, edit, copy, move, rename, delete, rmdir under /secure and /std
    private const int MaxEditLines = 5000;
    private const int LevelMudstat = 1;
    private const int LevelClone = 3;
    private const int LevelUpdate = 5;
    private const int LevelShutdown = 10;

    private const int MinPasswordLength = 8;
    private const int MaxPasswordChecks = 3;
    private const string LoginProgram = "/secure/login";

    [Saved] private string title = "the newcomer";
    [Saved] private int logins;
    [Saved] private long lastLogin;
    [Saved] private string cwd = "/"; // the mudlib directory a wizard is "in"
    // Made by the HashPassword efun. Empty for accounts saved before passwords
    // existed. No command may ever show it.
    [Saved] private string passwordHash = "";

    private bool ready;

    // Login hand-shake with /secure/login, used only until the player is ready.
    private ObjectRef? loginObject; // the login object that called BeginLogin
    private string accountState = ""; // "new", "password" or "unset" after BeginLogin
    private bool authenticated;
    private bool returning; // a save file was restored
    private int failedChecks;

    // Editor session; edLines is null when the player is not in the editor.
    private List<string>? edLines;
    private string edPath = "";
    private bool edDirty;
    private int edInsertAt = -1; // >= 0 while typed lines are being inserted at that index

    // Counts every message sent to this player. delete compares it before and
    // after its question, so a "y" typed after anything else was shown (which
    // may have been another object's question) does not delete.
    private int messagesShown;

    public override void CatchTell(string message)
    {
        messagesShown++;
        base.CatchTell(message);
    }

    private string SaveFile => "/players/" + Name.ToLower();

    public override string? Short() => $"{Name} {title}";

    public override string Long() => $"{Name} {title}, who has visited {logins} time(s).\n";

    // ---- Login ----
    // These public methods decide who gets into an account, so each one checks
    // that it is called by the login object that started this login (or, for
    // BeginLogin, by a connected login object), and each one does nothing once
    // the player is set up.

    private static bool IsLoginObject(ObjectRef? ob) => ob != null && ob.IsClone && ob.ProgramPath == LoginProgram;

    private bool CalledByOurLogin() => !ready && loginObject != null && loginObject.IsAlive && PreviousObject() == loginObject;

    /// <summary>
    /// Step 1, called by /secure/login on a freshly cloned body: take the name
    /// and restore its save file. Returns "new" (no save file), "password"
    /// (the account has a password) or "unset" (an account saved before
    /// passwords existed). Returns "" if the call is refused. Works once.
    /// </summary>
    public string BeginLogin(string name)
    {
        var caller = PreviousObject();
        if (ready || loginObject != null || !IsLoginObject(caller) || !Interactive(caller!)) return "";
        if (name.Length < 2 || name.Length > 16 || !name.All(char.IsLetter)) return "";
        loginObject = caller;
        Name = name;
        returning = RestoreObject(SaveFile);
        accountState = !returning ? "new" : string.IsNullOrEmpty(passwordHash) ? "unset" : "password";
        return accountState;
    }

    /// <summary>
    /// Step 2 for an account with a password. True if it matches. After
    /// three wrong passwords every later check fails.
    /// </summary>
    public bool CheckPassword(string password)
    {
        if (!CalledByOurLogin() || accountState != "password" || authenticated || failedChecks >= MaxPasswordChecks) return false;
        if (VerifyPassword(password, passwordHash))
        {
            authenticated = true;
            return true;
        }
        failedChecks++;
        return false;
    }

    /// <summary>
    /// Step 2 for a new account or one without a password: store the password
    /// and save at once, so a new name is reserved with its password. False if
    /// refused, including when the password breaks the rules in CheckNewPassword.
    /// </summary>
    public bool SetPassword(string password)
    {
        if (!CalledByOurLogin() || authenticated || accountState is not ("new" or "unset")) return false;
        if (CheckNewPassword(password) != null) return false;
        passwordHash = HashPassword(password);
        authenticated = true;
        SaveObject(SaveFile);
        return true;
    }

    /// <summary>Why a password may not be used for this player, or null if it may. Reveals nothing about the account.</summary>
    public string? CheckNewPassword(string password)
    {
        if (password.Length < MinPasswordLength) return $"A password needs at least {MinPasswordLength} characters.";
        if (string.Equals(password, Name, StringComparison.OrdinalIgnoreCase)) return "Your password cannot be your name.";
        return null;
    }

    /// <summary>
    /// Step 3, called by /secure/login once the connection has been handed
    /// over to this body: enter the game. Needs a successful step 2.
    /// </summary>
    public void SetupPlayer(string name)
    {
        if (ready || !authenticated || !CalledByOurLogin() || name != Name || !Interactive(Self)) return;
        ready = true;
        loginObject = null;

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
        AddAction("password", CmdPassword);
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
        AddAction("copy", CmdCopy);
        AddAction("move", CmdMove);
        AddAction("rename", CmdRename);
        AddAction("delete", CmdDelete);
        AddAction("rmdir", CmdRmdir);
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

    /// <summary>
    /// "password": change the password. Asks for the current one, then the new
    /// one twice, with echo off. Nothing typed here is ever written back.
    /// </summary>
    private bool CmdPassword(string args)
    {
        Write("Current password: ");
        InputTo(PasswordCurrent, true);
        return true;
    }

    private void PasswordCurrent(string current)
    {
        if (!VerifyPassword(current, passwordHash))
        {
            Write("Wrong password. Your password is unchanged.\n");
            return;
        }
        Write($"New password (at least {MinPasswordLength} characters): ");
        InputTo(PasswordNew, true);
    }

    private void PasswordNew(string password)
    {
        var problem = CheckNewPassword(password);
        if (problem != null)
        {
            Write(problem + " Your password is unchanged.\n");
            return;
        }
        Write("Type it again: ");
        InputTo(again => PasswordConfirm(password, again), true);
    }

    private void PasswordConfirm(string password, string again)
    {
        if (again != password)
        {
            Write("The passwords do not match. Your password is unchanged.\n");
            return;
        }
        passwordHash = HashPassword(password);
        SaveObject(SaveFile);
        Write("Password changed.\n");
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
            "          who, title <text>, password, save, quit, and the exits of the room you are in.\n" +
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
              "          copy <from> <to>           copy a file (into a directory: same name)\n" +
              "          move <from> <to>           move a file to a new name or directory\n" +
              "          rename <file> <newname>    rename a file within its directory\n" +
              "          delete <file>              delete a file, after you confirm (cannot be undone)\n" +
              "          rmdir <dir>                remove an empty directory\n" +
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
        // Compared without case: on a case-insensitive file system /STD is /std.
        => (path + "/").ToUpperInvariant().StartsWith("/SECURE/") || (path + "/").ToUpperInvariant().StartsWith("/STD/") ? LevelSystemFiles : LevelEdit;

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

    private bool CmdCopy(string args) => TransferFile(args, false);

    private bool CmdMove(string args) => TransferFile(args, true);

    /// <summary>
    /// "copy <from> <to>" and "move <from> <to>". If the destination is an
    /// existing directory the file keeps its name inside it. The driver never
    /// overwrites a file. Copying needs the level to write the destination;
    /// moving changes both places, so it needs the level to write both.
    /// </summary>
    private bool TransferFile(string args, bool move)
    {
        if (!IsWizard(LevelEdit)) return false;
        var verb = move ? "move" : "copy";
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2) { NotifyFail($"Usage: {verb} <from> <to>"); return false; }
        try
        {
            var from = ResolveFile(words[0]);
            var to = ResolvePath(words[1]);
            to = GetDir(to) != null ? (to == "/" ? "" : to) + from.Substring(from.LastIndexOf('/')) : ResolveFile(words[1]);
            var level = move ? Math.Max(LevelToWrite(from), LevelToWrite(to)) : LevelToWrite(to);
            if (!IsWizard(level)) { Write($"You need wizard level {LevelSystemFiles} to change /secure and /std.\n"); return true; }
            var done = move ? RenameFile(from, to) : CopyFile(from, to);
            Write(done ? $"{(move ? "Moved" : "Copied")} {from} to {to}.\n" : $"{from}: no such file.\n");
        }
        catch (MudRuntimeException refusal)
        {
            Write(refusal.Message + "\n");
        }
        return true;
    }

    /// <summary>
    /// "rename <file> <newname>": a new name in the same directory. ".cs" is
    /// assumed for a new name with no extension, as for every file name.
    /// </summary>
    private bool CmdRename(string args)
    {
        if (!IsWizard(LevelEdit)) return false;
        var words = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != 2) { NotifyFail("Usage: rename <file> <newname>"); return false; }
        if (words[1].Contains('/')) { Write("The new name cannot contain '/'. Use move to change directory.\n"); return true; }
        try
        {
            var from = ResolveFile(words[0]);
            // Built directly rather than with ResolvePath, so ".." cannot turn
            // into a parent directory; the driver refuses dot names itself.
            var to = from.Substring(0, from.LastIndexOf('/') + 1) + (words[1].Contains('.') ? words[1] : words[1] + ".cs");
            if (!IsWizard(Math.Max(LevelToWrite(from), LevelToWrite(to)))) { Write($"You need wizard level {LevelSystemFiles} to change /secure and /std.\n"); return true; }
            Write(RenameFile(from, to) ? $"Renamed {from} to {to}.\n" : $"{from}: no such file.\n");
        }
        catch (MudRuntimeException refusal)
        {
            Write(refusal.Message + "\n");
        }
        return true;
    }

    /// <summary>
    /// "delete <file>": delete one file. It cannot be undone, so the wizard
    /// must answer y or yes first; anything else keeps the file. Directories
    /// are removed with rmdir instead.
    /// </summary>
    private bool CmdDelete(string args)
    {
        if (!IsWizard(LevelEdit)) return false;
        args = args.Trim();
        if (args == "") { NotifyFail("Usage: delete <file>"); return false; }
        try
        {
            var file = ResolveFile(args);
            if (!IsWizard(LevelToWrite(file))) { Write($"You need wizard level {LevelSystemFiles} to change /secure and /std.\n"); return true; }
            if (!IsFile(file))
            {
                // "delete sub" looks for sub.cs, but the wizard may have meant the directory sub.
                var directory = GetDir(file) != null ? file : GetDir(ResolvePath(args)) != null ? ResolvePath(args) : null;
                Write(directory != null ? $"{directory} is a directory; use rmdir to remove an empty directory.\n" : $"{file}: no such file.\n");
                return true;
            }
            Write($"Delete {file}? This cannot be undone. (y/n) ");
            var asked = messagesShown;
            InputTo(answer => DeleteConfirmed(file, answer, asked));
        }
        catch (MudRuntimeException refusal)
        {
            Write(refusal.Message + "\n");
        }
        return true;
    }

    private void DeleteConfirmed(string file, string answer, int asked)
    {
        var reply = answer.Trim().ToLowerInvariant();
        if (reply != "y" && reply != "yes") { Write("Not deleted.\n"); return; }
        // Checked again here: the level may have changed while the question was open.
        if (!IsWizard(LevelToWrite(file))) { Write("You may no longer delete that file. Not deleted.\n"); return; }
        if (messagesShown != asked) { Write($"Other messages arrived after the question, so {file} was not deleted. Type delete again.\n"); return; }
        try
        {
            if (!RemoveFile(file)) { Write($"{file}: no such file.\n"); return; }
            Write($"Deleted {file}.\n");
            var objectPath = file.EndsWith(".cs") ? file.Substring(0, file.Length - 3) : null;
            if (objectPath != null && FindObject(objectPath) != null)
                Write($"{objectPath} is still loaded and will stay until it is destructed or the driver restarts.\n");
        }
        catch (MudRuntimeException refusal)
        {
            Write(refusal.Message + "\n");
        }
    }

    /// <summary>
    /// True if the path names a file. Looked up in the directory listing, so
    /// files too large for ReadFile are found as well.
    /// </summary>
    private bool IsFile(string path)
    {
        var slash = path.LastIndexOf('/');
        var name = path.Substring(slash + 1);
        var entries = GetDir(slash == 0 ? "/" : path.Substring(0, slash));
        return name != "" && entries != null && entries.Any(e => !e.IsDirectory && e.Name == name);
    }

    /// <summary>
    /// "rmdir <dir>": remove one empty directory. The driver refuses a
    /// directory with anything in it, hidden files included, and is never
    /// recursive.
    /// </summary>
    private bool CmdRmdir(string args)
    {
        if (!IsWizard(LevelEdit)) return false;
        args = args.Trim();
        if (args == "") { NotifyFail("Usage: rmdir <dir>"); return false; }
        var target = ResolvePath(args);
        if (!IsWizard(LevelToWrite(target))) { Write($"You need wizard level {LevelSystemFiles} to change /secure and /std.\n"); return true; }
        // The root is left to the driver, which refuses it with its own message.
        if (target == cwd && target != "/") { Write("You are in that directory.\n"); return true; }
        if (target != "/" && cwd.StartsWith(target + "/")) { Write("You are inside that directory.\n"); return true; }
        try
        {
            Write(RemoveDir(target) ? $"Removed {target}/\n" : $"{target}: no such directory.\n");
        }
        catch (MudRuntimeException refusal)
        {
            Write(refusal.Message + "\n");
        }
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
