// @inherit /std/thing

/// <summary>
/// The player body. Its own commands are ordinary AddAction bindings, added
/// once at login. Everything else a player can do is offered by the room and
/// the objects around them.
/// </summary>
public class Player : Thing
{
    private const string StartRoom = "/domains/town/square";

    [Saved] private string title = "the newcomer";
    [Saved] private int logins;
    [Saved] private long lastLogin;

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
        // Wizard commands.
        AddAction("update", CmdUpdate);
        AddAction("clone", CmdClone);
        AddAction("mudstat", CmdMudstat);
        AddAction("shutdown", CmdShutdown);

        Write(returning ? $"\nWelcome back, {Name}. This is visit number {logins}.\n\n" : $"\nWelcome, {Name}. Type 'help' for commands.\n\n");
        MoveObject(LoadObject(StartRoom));
        Say($"{Name} enters the game.\n");
        Look();
    }

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
        var users = Users().Select(u => u.Call<string>("Short")).Where(s => s != null).ToList();
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
            "          Objects nearby add their own verbs (try the lantern and the cat).\n" +
            "Wizard:   update <path>   recompile a file and replace the loaded object\n" +
            "          clone <path>    clone an object into your inventory or room\n" +
            "          mudstat         driver statistics\n" +
            "          shutdown        stop the driver\n");
        return true;
    }

    // ---- Wizard commands ----

    /// <summary>
    /// The LPmud "update": recompile a source file. If its blueprint object is
    /// loaded, move the living things out, destruct the old object together
    /// with its other contents, load the new one (whose Create() restocks it),
    /// and move the living things back in.
    /// </summary>
    private bool CmdUpdate(string args)
    {
        if (args == "") { NotifyFail("Usage: update <path>"); return false; }

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
        if (args == "") { NotifyFail("Usage: clone <path>"); return false; }
        var ob = CloneObject(args);
        var room = Environment();
        MoveObject(ob, ob.Call<bool>("CanGet") || room == null ? Self : room);
        Write($"Cloned {ob.Name}.\n");
        return true;
    }

    private bool CmdMudstat(string args)
    {
        Write(DriverStatus());
        return true;
    }

    private bool CmdShutdown(string args)
    {
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
