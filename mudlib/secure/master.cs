/// <summary>
/// The master object. The driver loads it first and asks it for every policy
/// decision, so the security model lives here in the mudlib, not in the driver.
/// </summary>
public class Master : MudObject, IMaster
{
    private const string WizList = "/secure/wizlist";

    public ObjectRef Connect() => CloneObject("/secure/login");

    public IEnumerable<string> Preload() => new[] { WizList, "/std/void", "/domains/town/square" };

    // Save files: player bodies own /players/*, the wizard registry owns its own file.
    public bool ValidRead(string path, ObjectRef caller) => OwnsSaveFile(path, caller);
    public bool ValidWrite(string path, ObjectRef caller) => OwnsSaveFile(path, caller);

    private static bool OwnsSaveFile(string path, ObjectRef caller)
    {
        if (path == WizList) return IsWizList(caller);
        return path.StartsWith("/players/") && IsPlayerBody(caller);
    }

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
            case "update":
            case "shutdown":
            case "destruct": return IsPlayerBody(caller);
            default: return false;
        }
    }

    public void LogError(string objectName, string message)
    {
        // A real mudlib would append to a per-domain error log here.
    }
}
