/// <summary>
/// The master object. The driver loads it first and asks it for every policy
/// decision, so the security model lives here in the mudlib, not in the driver.
/// </summary>
public class Master : MudObject, IMaster
{
    private const string WizList = "/secure/wizlist";
    private const string LostProperty = "/secure/lost_property";
    private const string LegacyVault = "/secure/vault";  // the old name of the lost-property save file, read once by the upgrade

    public ObjectRef Connect() => CloneObject("/secure/login");

    public IEnumerable<string> Preload() => new[] { WizList, LostProperty, "/secure/chronicle", "/secure/renown", "/secure/rumors", "/secure/events", "/secure/finds", "/secure/crafting", "/secure/comm", "/secure/channels", "/std/void", "/domains/town/square", "/domains/hollow/village/square", "/domains/hollow/village/guest_rooms" };

    // Save files: player bodies own /players/*, the wizard registry owns its own file.
    public bool ValidRead(string path, ObjectRef caller) => OwnsSaveFile(path, caller);
    public bool ValidWrite(string path, ObjectRef caller) => OwnsSaveFile(path, caller);

    private static bool OwnsSaveFile(string path, ObjectRef caller)
    {
        if (path == WizList) return IsWizList(caller);
        if (path == LostProperty || path == LegacyVault) return !caller.IsClone && caller.ProgramPath == LostProperty;
        // The history daemons (and the register of finds) each own the save file of their own name.
        if (path is "/secure/chronicle" or "/secure/renown" or "/secure/rumors" or "/secure/events" or "/secure/finds") return !caller.IsClone && caller.ProgramPath == path;
        return path.StartsWith("/players/") && IsPlayerBody(caller);
    }

    // The class registry blueprint may list the two directories where classes and skills live, and nothing else.
    private static bool IsClassRegistry(ObjectRef ob, string? path)
        => !ob.IsClone && ob.ProgramPath == "/secure/classes" && (path == "/classes" || path == "/skills");

    // The channel daemon blueprint may list the directory where channels live, and nothing else.
    private static bool IsChannelDaemon(ObjectRef ob, string? path)
        => !ob.IsClone && ob.ProgramPath == "/secure/channels" && path == "/channels";

    // The crafting daemon blueprint may list the two trees where crafting data and recipes live, and nothing else.
    private static bool IsCraftingDaemon(ObjectRef ob, string? path)
        => !ob.IsClone && ob.ProgramPath == "/secure/crafting" && path != null && !path.Contains("..")
           && (path == "/crafting" || path == "/recipes" || path.StartsWith("/crafting/") || path.StartsWith("/recipes/"));

    private static bool IsPlayerBody(ObjectRef ob) => ob.IsClone && ob.ProgramPath == "/std/player";

    // Only the registry blueprint counts. A clone of it has no authority.
    private static bool IsWizList(ObjectRef ob) => !ob.IsClone && ob.ProgramPath == WizList;

    // Privileged efuns. This says which code may call them at all; whether a
    // particular player may use the command that leads here is decided by
    // wizard level, in /std/player and /secure/wizlist.
    public bool ValidOperation(string operation, ObjectRef caller, string? arg)
    {
        switch (operation)
        {
            case "admin-secret": return IsWizList(caller);
            case "exec": return caller.ProgramPath == "/secure/login";
            case "get-dir":
                return IsPlayerBody(caller) || IsClassRegistry(caller, arg) || IsCraftingDaemon(caller, arg) || IsChannelDaemon(caller, arg);
            case "read-file":
            case "write-file":
            case "make-dir":
            case "remove-file":
            case "remove-dir":
            case "update":
            case "shutdown": return IsPlayerBody(caller);
            case "destruct": return IsPlayerBody(caller) || LoginMayDestruct(caller, arg);
            default: return false;
        }
    }

    // The login object destructs the player body it cloned when a login fails
    // or the connection drops. It may only destruct bodies without a connection.
    private bool LoginMayDestruct(ObjectRef caller, string? arg)
        => caller.IsClone && caller.ProgramPath == "/secure/login"
           && arg != null && FindObject(arg) is { } target && IsPlayerBody(target) && !Interactive(target);

    public void LogError(string objectName, string message)
    {
        // A real mudlib would append to a per-domain error log here.
    }
}
