/// <summary>
/// The master object. The driver loads it first and asks it for every policy
/// decision, so the security model lives here in the mudlib, not in the driver.
///
/// The wizard sandbox (docs/SANDBOX.md) is policy here too: ObjectDomain marks sandbox objects, MoveRefusal keeps them and
/// ordinary players on their own side, ValidCall decides what restricted code may reach, and the privileged efuns refuse
/// restricted code outright.
/// </summary>
public class Master : MudObject, IMaster
{
    private const string WizList = "/secure/wizlist";
    private const string LostProperty = "/secure/lost_property";
    private const string LegacyVault = "/secure/vault";  // the old name of the lost-property save file, read once by the upgrade
    private const string Publish = "/secure/publish";
    private const string PlayerProgram = "/std/player";

    public ObjectRef Connect() => CloneObject("/secure/login");

    public override void Create() => SetHeartBeat(true); // the workshop invariant, below

    public IEnumerable<string> Preload() => new[] { WizList, LostProperty, "/secure/chronicle", "/secure/renown", "/secure/rumors", "/secure/events", "/secure/finds", "/secure/crafting", "/secure/comm", "/secure/channels", "/secure/roomids", "/secure/gmcp", Publish, "/std/void", "/domains/town/square", "/domains/hollow/village/square", "/domains/hollow/village/guest_rooms" };

    // Save files: player bodies own /players/*, the wizard registry owns its own file. Restricted code owns none, and a character in
    // the workshop is never written.
    public bool ValidRead(string path, ObjectRef caller) => RestrictedCaller() == null && OwnsSaveFile(path, caller);
    public bool ValidWrite(string path, ObjectRef caller)
        => RestrictedCaller() == null && OwnsSaveFile(path, caller) && !(IsPlayerBody(caller) && InWorkshop(caller));

    private static bool OwnsSaveFile(string path, ObjectRef caller)
    {
        if (caller.Domain != "") return false;
        if (path == WizList) return IsWizList(caller);
        if (path == LostProperty || path == LegacyVault) return !caller.IsClone && caller.ProgramPath == LostProperty;
        // The history daemons (and the register of finds, the register of room numbers and the publication register) each own the
        // save file of their own name.
        if (path is "/secure/chronicle" or "/secure/renown" or "/secure/rumors" or "/secure/events" or "/secure/finds" or "/secure/roomids" or Publish) return !caller.IsClone && caller.ProgramPath == path;
        return path.StartsWith("/players/") && IsPlayerBody(caller);
    }

    // The class registry blueprint may list the two directories where classes and skills live, and nothing else.
    private static bool IsClassRegistry(ObjectRef ob, string? path)
        => !ob.IsClone && ob.Domain == "" && ob.ProgramPath == "/secure/classes" && (path == "/classes" || path == "/skills");

    // The channel daemon blueprint may list the directory where channels live, and nothing else.
    private static bool IsChannelDaemon(ObjectRef ob, string? path)
        => !ob.IsClone && ob.Domain == "" && ob.ProgramPath == "/secure/channels" && path == "/channels";

    // The crafting daemon blueprint may list the two trees where crafting data and recipes live, and nothing else.
    private static bool IsCraftingDaemon(ObjectRef ob, string? path)
        => !ob.IsClone && ob.Domain == "" && ob.ProgramPath == "/secure/crafting" && path != null && !path.Contains("..")
           && (path == "/crafting" || path == "/recipes" || path.StartsWith("/crafting/") || path.StartsWith("/recipes/"));

    // A player body is a clone of /std/player made by the real game. A copy cloned by sandbox code is a sandbox object, not a body.
    private static bool IsPlayerBody(ObjectRef ob) => ob.IsClone && ob.ProgramPath == PlayerProgram && ob.Domain == "";

    // Only the registry blueprint counts. A clone of it has no authority.
    private static bool IsWizList(ObjectRef ob) => !ob.IsClone && ob.Domain == "" && ob.ProgramPath == WizList;

    // The publication register blueprint (docs/SANDBOX.md section 4) reads anywhere, and writes and compiles under /domains only.
    private static bool IsPublisher(ObjectRef ob) => !ob.IsClone && ob.Domain == "" && ob.ProgramPath == Publish;

    private static bool UnderDomains(string? path) => path != null && path.StartsWith("/domains/") && !path.Contains("..");

    // Privileged efuns. This says which code may call them at all; whether a
    // particular player may use the command that leads here is decided by
    // wizard level, in /std/player and /secure/wizlist. Restricted code (an object of a wizard's sandbox anywhere on the call
    // stack) may only read: whatever program is asking, it is acting for sandbox code.
    public bool ValidOperation(string operation, ObjectRef caller, string? arg)
    {
        if (RestrictedCaller() != null && operation is not ("get-dir" or "read-file")) return false;
        switch (operation)
        {
            case "admin-secret": return IsWizList(caller);
            case "exec": return (caller.ProgramPath == "/secure/login" && caller.Domain == "") || WorkshopHandover(caller, arg);
            case "get-dir":
                return IsPlayerBody(caller) || IsClassRegistry(caller, arg) || IsCraftingDaemon(caller, arg) || IsChannelDaemon(caller, arg) || IsPublisher(caller);
            case "read-file": return IsPlayerBody(caller) || IsPublisher(caller);
            case "write-file":
            case "make-dir":
            case "remove-file":
            case "remove-dir": return IsPlayerBody(caller) || (IsPublisher(caller) && UnderDomains(arg));
            // The register compiles a request where it is (to refuse one that does not compile) and where it is published.
            case "update": return IsPlayerBody(caller) || (IsPublisher(caller) && (UnderDomains(arg) || SandboxOwner(arg ?? "") is { Length: > 0 }));
            case "shutdown": return IsPlayerBody(caller);
            case "destruct": return IsPlayerBody(caller) || LoginMayDestruct(caller, arg) || (IsPublisher(caller) && arg != null && FindObject(arg) is { } ob && UnderDomains(ob.ProgramPath));
            default: return false;
        }
    }

    // The login object destructs the player body it cloned when a login fails
    // or the connection drops. It may only destruct bodies without a connection.
    private bool LoginMayDestruct(ObjectRef caller, string? arg)
        => caller.IsClone && caller.ProgramPath == "/secure/login" && caller.Domain == ""
           && arg != null && FindObject(arg) is { } target && IsPlayerBody(target) && !Interactive(target);

    // A character leaving the workshop hands its connection to the fresh body made from its save file (docs/SANDBOX.md section 3).
    private bool WorkshopHandover(ObjectRef caller, string? arg)
        => IsPlayerBody(caller) && arg != null && FindObject(arg) is { } target && target != caller && IsPlayerBody(target) && !Interactive(target)
           && caller.Call<bool>("QueryLeavingWorkshop") && target.Call<string>("QueryResumingFrom") == caller.Name;

    public void LogError(string objectName, string message)
    {
        // A real mudlib would append to a per-domain error log here.
    }

    // =====================================================================
    // The wizard sandbox (docs/SANDBOX.md)
    // =====================================================================

    /// <summary>"bob" for a path under /wiz/bob, "" for /wiz itself or a file directly in it, null for any path outside /wiz.</summary>
    private static string? SandboxOwner(string path)
    {
        var lower = path.ToLowerInvariant();
        if (lower == "/wiz") return "";
        if (!lower.StartsWith("/wiz/")) return null;
        var rest = lower.Substring(5);
        var slash = rest.IndexOf('/');
        return slash < 0 ? "" : rest.Substring(0, slash);
    }

    /// <summary>
    /// Sandbox code is in the domain "wiz:&lt;owner&gt;". A clone made while sandbox code is running, or for a character in the
    /// workshop, is a sandbox object too, whatever its program: a goblin from /domains cloned by a wizard's room, its corpse and loot.
    /// </summary>
    public string ObjectDomain(string programPath, bool isClone)
    {
        var owner = SandboxOwner(programPath);
        if (owner != null) return "wiz:" + owner;
        if (!isClone) return "";
        if (RestrictedCaller() is { } restricted) return restricted.Domain;
        var chain = CallerChain();
        foreach (var ob in chain)
            if (IsPlayerBody(ob) && InWorkshop(ob)) return "wiz:" + (ob.Call<string>("QueryName") ?? "").ToLowerInvariant();
        return "";
    }

    private static bool InWorkshop(ObjectRef body) => body.IsAlive && body.Call<bool>("QueryInWorkshop");

    /// <summary>The outermost real container around a place, the room a checkpoint is taken in; null in sandbox space or nowhere.</summary>
    private ObjectRef? OutermostOf(ObjectRef? place)
    {
        var top = place;
        while (top != null && Environment(top) is { } up) top = up;
        return top != null && top.Domain == "" ? top : null;
    }

    /// <summary>
    /// The workshop invariant, kept every heartbeat whatever any body's own code did (the second review's class A): a character in
    /// sandbox space is marked; a marked character outside sandbox space is restored; an unmarked character carries nothing made
    /// in a workshop. A repair is logged with a "sandbox:" line.
    /// </summary>
    public override void HeartBeat()
    {
        foreach (var body in Users())
        {
            if (!IsPlayerBody(body) || !body.IsAlive) continue;
            try
            {
                var inside = PlaceOf(Environment(body)) == Place.Sandbox;
                var marked = InWorkshop(body);
                if (body.Call<bool>("QueryLeavingWorkshop")) continue;
                if (inside && !marked)
                {
                    Write($"sandbox: invariant: {body.Name} was in sandbox space without the workshop mark; moved out, workshop things removed\n");
                    body.Call("WorkshopRepair");
                }
                else if (!inside && marked)
                {
                    Write($"sandbox: invariant: {body.Name} was marked as in the workshop outside sandbox space; restored\n");
                    body.Call("WorkshopLeaving");
                }
                else if (!marked && AllInventory(body).Any(item => item.Domain != ""))
                {
                    Write($"sandbox: invariant: {body.Name} carried something made in a workshop outside it; removed\n");
                    body.Call("WorkshopSweep");
                }
            }
            catch (Exception) { }
        }
    }

    private enum Place { Real, Workshop, Sandbox }

    /// <summary>Where a container is: inside sandbox space, inside a character in the workshop, or in the real game.</summary>
    private Place PlaceOf(ObjectRef? container)
    {
        for (var e = container; e != null; e = Environment(e))
        {
            if (e.Domain != "") return Place.Sandbox;
            if (IsPlayerBody(e) && InWorkshop(e)) return Place.Workshop;
        }
        return Place.Real;
    }

    /// <summary>The sandbox domain of the space a container is in (the first sandbox object around it), or "" in the real game.</summary>
    private string SpaceOf(ObjectRef? container)
    {
        for (var e = container; e != null; e = Environment(e))
            if (e.Domain != "") return e.Domain;
        return "";
    }

    /// <summary>The character in the workshop that is this object or carries it, or null.</summary>
    private ObjectRef? WorkshopBodyOf(ObjectRef? container)
    {
        for (var e = container; e != null; e = Environment(e))
        {
            if (e.Domain != "") return null;
            if (IsPlayerBody(e) && InWorkshop(e)) return e;
        }
        return null;
    }

    /// <summary>The domain of the sandbox code acting now ("" if none).</summary>
    private string ActingDomain() => RestrictedCaller()?.Domain ?? "";

    /// <summary>Is this character the owner of that domain, standing in it: the wizard in their own workshop?</summary>
    private bool OwnerAtHome(ObjectRef body, string domain)
        => domain.StartsWith("wiz:") && domain.Length > 4 && SpaceOf(body) == domain
           && (body.Call<string>("QueryName") ?? "").ToLowerInvariant() == domain.Substring(4);

    private int WizLevelOf(ObjectRef body)
    {
        try { return LoadObject(WizList).Call<int>("QueryLevel", body.Call<string>("QueryName") ?? ""); }
        catch (MudRuntimeException) { return 0; }
    }

    /// <summary>The boundary of sandbox space, for every move. The table is in docs/SANDBOX.md, "What can cross the boundary".</summary>
    public string? MoveRefusal(ObjectRef what, ObjectRef destination, ObjectRef caller)
    {
        var domain = ActingDomain();
        var restricted = domain != "";
        var to = PlaceOf(destination);
        if (IsPlayerBody(what))
        {
            if (to == Place.Sandbox && WizLevelOf(what) < 1) return "Only wizards can enter a wizard's workshop.";
            if (restricted)
            {
                if (!InWorkshop(what)) return "Sandbox code cannot move anyone who is not in the workshop.";
                if (SpaceOf(what) != domain) return "Sandbox code can move only the wizards in its own workshop.";
                if (to == Place.Sandbox && SpaceOf(destination) != domain) return "Sandbox code cannot move anyone into another wizard's workshop.";
                if (to != Place.Sandbox && Environment(destination) != null) return "Sandbox code can move a wizard out of the workshop only into a room.";
                return null;
            }
            // Into a workshop only by the character's own command (goto, an exit): nothing else may carry a wizard in, the level 10
            // included (C1 of the November 2026 review: the wizard was dragged in to be shown a fake prompt).
            var marked = InWorkshop(what);
            if (to == Place.Sandbox && !marked && ThisPlayer() != what) return "Only a wizard's own command takes them into a workshop.";
            // The checkpoint is taken and the mark set here, in the move itself, before the move happens and before any code of the
            // workshop or of the body runs (the second review's class A): if it cannot be taken, for any reason, the wizard does not
            // enter. And a marked body that arrives outside sandbox space has its restore scheduled here, whatever its own hooks do.
            if (to == Place.Sandbox && !marked)
            {
                bool taken;
                try { taken = what.Call<bool>("WorkshopCheckpoint", OutermostOf(Environment(what))); }
                catch (Exception) { taken = false; }
                if (!taken) return "The workshop could not take your checkpoint, so you stay where you are. Try again in a moment.";
            }
            if (to != Place.Sandbox && marked)
            {
                try { what.Call("WorkshopLeaving"); }
                catch (Exception) { } // the heartbeat invariant restores the body if even this fails
            }
            return null;
        }
        if (what.Domain != "")
        {
            if (to == Place.Real) return "Things made in the workshop cannot leave it.";
            if (restricted)
            {
                if (what.Domain != domain) return "Sandbox code can move only its own things.";
                var space = to == Place.Sandbox ? SpaceOf(destination) : SpaceOf(WorkshopBodyOf(destination));
                if (space != domain) return "Sandbox code cannot put things in another wizard's workshop.";
            }
            return null;
        }
        // A real thing, or a real creature.
        if (to == Place.Sandbox) return Living(what) ? "Only wizards can enter a wizard's workshop." : "Real things cannot be set down in the workshop.";
        var at = PlaceOf(Environment(what));
        if (at == Place.Workshop && to != Place.Workshop) return "What a wizard carries in the workshop stays with them until they leave it.";
        if (restricted && (at != Place.Workshop || to != Place.Workshop)) return "Sandbox code cannot move real things.";
        if (restricted && (WorkshopBodyOf(Environment(what)) is not { } from || !OwnerAtHome(from, domain)
                           || WorkshopBodyOf(destination) is not { } into || !OwnerAtHome(into, domain)))
            return "Sandbox code can move only what the wizard in their own workshop carries.";
        return null;
    }

    private static readonly HashSet<string> ProtectedVerbs = new()
    {
        "password", "quit", "save", "promote", "submit", "submissions", "submission", "approve", "reject", "withdraw", "revoke",
        "edit", "delete", "rmdir", "copy", "move", "rename", "mkdir", "update", "shutdown", "goto",
    };

    // What a workshop's rooms may do to a visitor besides reading: walk them through an exit as /std/room does (the visitor's body
    // is checkpointed like the owner's, so nothing done to it lasts; what matters is that the workshop cannot speak as the game).
    private static readonly HashSet<string> VisitorMoves = new() { "SneakDeparts", "RecordArrival", "RecordFoundExit", "Look" };

    // Functions that only read, by the naming rule every public function of the game keeps (docs/SANDBOX.md section 3).
    private static readonly string[] ReadOnlyPrefixes = { "Query", "Is", "Has", "Can" };
    private static readonly HashSet<string> ReadOnlyNames = new() { "Short", "Long", "LongFor", "Id", "HealthLine", "Stat", "Appraisal", "DescribeCount" };

    // Daemon functions that are pure, or that know about sandbox space themselves.
    private static readonly Dictionary<string, HashSet<string>> SandboxAware = new()
    {
        ["/secure/classes"] = new() { "ClassIds", "SkillIds", "Class", "Skill", "EnabledOfTier", "EnabledChildren" },
        ["/secure/doors"] = new() { "State", "Open", "Close", "Lock", "Unlock", "Reset" },
        ["/secure/sneak"] = new() { "NoticeChance", "Roll", "Notices" },
        ["/secure/renown"] = new() { "TierIndex", "TierFor", "TierLine", "RenownText", "RecognitionLine" },
        ["/secure/chronicle"] = new() { "YearLength", "DateFor", "DateText", "DateOfTimestamp", "DateCommand" },
        ["/secure/events"] = new() { "PreviewEss", "ClassName", "Describe" },
        ["/secure/ranks"] = new() { "PriceFor" },
    };

    private static bool ReadOnly(string function)
    {
        if (ReadOnlyNames.Contains(function)) return true;
        foreach (var prefix in ReadOnlyPrefixes)
            if (function.Length > prefix.Length && function.StartsWith(prefix, StringComparison.Ordinal) && char.IsUpper(function[prefix.Length])) return true;
        return false;
    }

    /// <summary>
    /// What restricted code may do to an object outside its own domain (the driver asks only then, and only while sandbox code is
    /// on the call stack). To the wizard in their own workshop, and what they carry: anything but a typed view, a hidden-input
    /// prompt or a protected verb. To a visitor in that workshop: read, offer verbs the visitor does not bind itself, and text,
    /// which the driver marks as the workshop's (CatchTellQuoted); never their input, never their own verbs. Elsewhere: read
    /// anything, and use the stateless rule objects and the sandbox-aware daemon functions; another wizard's sandbox: read only.
    /// </summary>
    public bool ValidCall(ObjectRef target, string function)
    {
        if (function == "As") return false;
        // A hidden line is a password: no sandbox code may ask for one, of anyone. Nor may it take over the commands that
        // protect a character or decide for the real game, even of a wizard in its own workshop.
        if (function == "InputToNoEcho") return false;
        var verb = function.StartsWith("AddAction:") ? function.Substring("AddAction:".Length)
            : function.StartsWith("OwnVerb:") ? function.Substring("OwnVerb:".Length) : null;
        if (verb != null && ProtectedVerbs.Contains(verb)) return false;
        var domain = ActingDomain();
        if (target.Domain != "") return target.Domain == domain || (verb == null && !function.StartsWith("CatchTell") && function != "InputTo" && ReadOnly(function));
        if (WorkshopBodyOf(target) is { } body)
        {
            if (OwnerAtHome(body, domain)) return true;
            if (SpaceOf(body) == domain)
            {
                // A visitor (a reviewer, a level 10 who walked in): the workshop may show them things, but not speak as the game.
                if (function == "CatchTellQuoted" || function.StartsWith("AddAction:") || VisitorMoves.Contains(function)) return true;
                if (function is "CatchTell" or "InputTo" or "GmcpSend" || verb != null) return false;
                return ReadOnly(function);
            }
        }
        if (function is "CatchTell" or "CatchTellQuoted" or "InputTo" or "GmcpSend" || verb != null) return false;
        if (ReadOnly(function)) return true;
        if (target.IsClone) return false;
        if (target.ProgramPath.StartsWith("/skills/") || target.ProgramPath.StartsWith("/classes/")) return true;
        return SandboxAware.TryGetValue(target.ProgramPath, out var functions) && functions.Contains(function);
    }
}
