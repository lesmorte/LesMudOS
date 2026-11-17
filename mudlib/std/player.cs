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
    private const int LevelFiles = 1; // pwd, cd, list
    private const int LevelMudstat = 1;
    private const int LevelClone = 3;
    private const int LevelUpdate = 5;
    private const int LevelShutdown = 10;

    [Saved] private string title = "the newcomer";
    [Saved] private int logins;
    [Saved] private long lastLogin;
    [Saved] private string cwd = "/"; // the mudlib directory a wizard is "in"

    private bool ready;

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
              "          list [<dir>]               list a directory (also: ls)\n");
        if (level >= LevelMudstat) Write("          mudstat                    driver statistics\n");
        if (level >= LevelClone) Write("          clone <path>               clone an object into your inventory or room\n");
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
