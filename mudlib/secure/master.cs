/// <summary>
/// The master object. The driver loads it first and asks it for every policy
/// decision, so the security model lives here in the mudlib, not in the driver.
/// </summary>
public class Master : MudObject, IMaster
{
    public ObjectRef Connect() => CloneObject("/secure/login");

    public IEnumerable<string> Preload() => new[] { "/std/void", "/domains/town/square" };

    // Save files: only player bodies may touch /players/*.
    public bool ValidRead(string path, ObjectRef caller) => IsPlayerFile(path, caller);
    public bool ValidWrite(string path, ObjectRef caller) => IsPlayerFile(path, caller);

    private static bool IsPlayerFile(string path, ObjectRef caller)
        => path.StartsWith("/players/") && caller.ProgramPath == "/std/player";

    // Privileged efuns: only code under /secure and /std may use them.
    // (In this proof of concept every player is a wizard; a real mudlib would
    // check a wizard level inside /std/player before it calls these.)
    public bool ValidOperation(string operation, ObjectRef caller, string? arg)
        => caller.ProgramPath.StartsWith("/secure/") || caller.ProgramPath.StartsWith("/std/");

    public void LogError(string objectName, string message)
    {
        // A real mudlib would append to a per-domain error log here.
    }
}
