using System.Runtime.CompilerServices;

namespace LesMudOS.Api;

/// <summary>
/// Implemented by the master object (/secure/master). The driver consults it
/// for every policy decision, so security policy lives in the mudlib.
/// </summary>
public interface IMaster
{
    /// <summary>A new connection arrived. Return the object that will own it; the driver then calls its Logon().</summary>
    ObjectRef Connect();

    /// <summary>Paths to load at boot, so that compile errors surface immediately.</summary>
    IEnumerable<string> Preload();

    /// <summary>May the caller read this save path?</summary>
    bool ValidRead(string path, ObjectRef caller);

    /// <summary>May the caller write this save path?</summary>
    bool ValidWrite(string path, ObjectRef caller);

    /// <summary>May the caller perform a privileged operation: "update", "shutdown", "destruct", "exec", "admin-secret", "get-dir", "read-file", "write-file", "make-dir", "remove-file", "remove-dir"? arg is the target, if any.</summary>
    bool ValidOperation(string operation, ObjectRef caller, string? arg);

    /// <summary>A runtime or compile error occurred during an execution started by or for the given object.</summary>
    void LogError(string objectName, string message);

    /// <summary>
    /// The domain of a new object, asked once, before its constructor runs. "" (the default) is the unrestricted world; any other
    /// value makes the object restricted: while a restricted object's code is anywhere on the call stack, calls to unrestricted
    /// objects are put to ValidCall first. For a clone the call stack is the cloner's; a blueprint is always created with an empty one.
    /// </summary>
    string ObjectDomain(string programPath, bool isClone) => "";

    /// <summary>
    /// Asked before every move (the MoveObject efun, and an object falling out of a container that is destroyed). Null allows it;
    /// any other value refuses it, and is the text of the error the efun throws. caller is the object whose code called the efun.
    /// </summary>
    string? MoveRefusal(ObjectRef what, ObjectRef destination, ObjectRef caller) => null;

    /// <summary>
    /// Asked only while restricted code is on the call stack, before it reaches an unrestricted object: a Call (function is the
    /// method name), a typed view ("As"), text sent by an efun ("CatchTell"), a verb binding ("AddAction:" and the verb) or an
    /// input capture ("InputTo", or "InputToNoEcho" for a hidden line such as a password) on the target. False refuses: the call returns null, the view is null, the text or binding is dropped.
    /// The master's own calls while it answers are not checked.
    /// </summary>
    bool ValidCall(ObjectRef target, string function) => true;
}

/// <summary>One entry of a mudlib directory, as returned by GetDir.</summary>
public sealed record DirEntry(string Name, bool IsDirectory, long Size)
{
    /// <summary>True if the entry is a symbolic link (to a file or a directory).</summary>
    public bool IsLink { get; init; }
}

/// <summary>Marks a field that SaveObject / RestoreObject persist.</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class SavedAttribute : Attribute { }

/// <summary>An error raised by the driver in response to a mudlib call.</summary>
public class MudRuntimeException : Exception
{
    public MudRuntimeException(string message) : base(message) { }
}

/// <summary>The current execution used more than its budget of ticks.</summary>
public sealed class EvalCostExceededException : MudRuntimeException
{
    public EvalCostExceededException() : base("Evaluation cost limit exceeded (runaway loop or recursion?)") { }
}

/// <summary>
/// Called by code the driver injects into every mudlib loop body and method
/// body at compile time. Mudlib authors never call it.
/// </summary>
public static class Runtime
{
    internal static long Ticks;
    internal static long Limit = 5_000_000;

    /// <summary>The domain of the object whose frame is on top of the driver's call stack; null when the stack is empty.</summary>
    internal static string? CurrentDomain;

    public static void Tick()
    {
        if (++Ticks > Limit) throw new EvalCostExceededException();
        // Turns a would-be process-killing StackOverflow into a catchable exception.
        RuntimeHelpers.EnsureSufficientExecutionStack();
    }

    /// <summary>
    /// The tick of sandbox code (docs/SANDBOX.md, "Sandbox code runs only in its own frames"): the same count and stack check, and
    /// first a check that the object on top of the driver's call stack is of the domain this code was compiled for. Sandbox code
    /// reached in any other way (a Message, ToString or Equals read by real code, a delegate or enumerator kept and run later, an
    /// exception filter run while another object's frame is on top) stops at its first statement.
    /// </summary>
    public static void Tick(string domain)
    {
        if (!string.Equals(domain, CurrentDomain, StringComparison.Ordinal)) throw new SandboxFrameException(domain);
        if (++Ticks > Limit) throw new EvalCostExceededException();
        RuntimeHelpers.EnsureSufficientExecutionStack();
        // Sandbox code may use only the first part of the game thread's stack, so that whatever runs below it without a tick
        // (framework code, the driver) still has most of the stack left: a depth limit that does not depend on counting ticks.
        if (StackStart != 0 && StackStart - StackHere() > SandboxStackBytes) throw new InsufficientExecutionStackException();
    }

    /// <summary>The address of the game thread's stack where the driver's loop runs (set by the driver); 0 if unknown.</summary>
    internal static nuint StackStart;
    internal static nuint SandboxStackBytes = 4 * 1024 * 1024;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe nuint StackHere()
    {
        byte probe = 0;
        return (nuint)(&probe);
    }

    /// <summary>Called by the driver on the game thread, near the bottom of its stack.</summary>
    internal static void MarkStackStart() => StackStart = StackHere();

    /// <summary>The tick as an expression, for code that runs outside any body (field and property initialisers, constructor initialisers).</summary>
    public static bool Pass()
    {
        Tick();
        return true;
    }

    /// <summary>The same for sandbox code: the tick of its domain.</summary>
    public static bool Pass(string domain)
    {
        Tick(domain);
        return true;
    }

    /// <summary>Set by the driver: may code of this domain run now as a catch filter (see Filter)?</summary>
    internal static Func<string, bool>? FilterCheck;

    /// <summary>
    /// The check for a catch filter of sandbox code, which runs while the frames the exception came out of are still on the stack:
    /// true if this domain's frame is on top, or if it is the sandbox code acting (the nearest restricted frame) and the master is
    /// not answering a question; otherwise the exception (a throwing filter counts as false, so the handler is skipped).
    /// </summary>
    public static bool Filter(string domain)
    {
        if (!string.Equals(domain, CurrentDomain, StringComparison.Ordinal) && FilterCheck?.Invoke(domain) != true)
            throw new SandboxFrameException(domain);
        if (++Ticks > Limit) throw new EvalCostExceededException();
        RuntimeHelpers.EnsureSufficientExecutionStack();
        return true;
    }
}

/// <summary>Sandbox code was reached outside a frame of its own domain (see Runtime.Tick(string)).</summary>
public sealed class SandboxFrameException : MudRuntimeException
{
    internal SandboxFrameException(string domain) : base($"code of {domain} cannot run outside its own objects") { }
}

/// <summary>The kernel entry points behind the efuns. Implemented by the driver host.</summary>
internal interface IDriverServices
{
    ObjectRef ClaimPendingRef(MudObject instance);
    object? CallOther(ObjectRef target, string function, object?[] args);

    ObjectRef LoadObject(string path);
    ObjectRef CloneObject(string path);
    ObjectRef? FindObject(string name);
    void Destruct(ObjectRef caller, ObjectRef target);
    IReadOnlyList<string> UpdateProgram(ObjectRef caller, string path);

    void Move(ObjectRef caller, ObjectRef what, ObjectRef destination);
    ObjectRef? Present(string id, ObjectRef container);

    ObjectRef? PreviousObject(ObjectRef self);
    IReadOnlyList<ObjectRef> CallerChain();
    bool ProgramCurrent(string path);
    ObjectRef? RestrictedCaller();
    bool MayView(ObjectRef target);
    ObjectRef? ThisPlayer { get; }
    string Verb { get; }
    void EnableCommands(ObjectRef ob);
    IReadOnlyList<ObjectRef> Users();
    void Exec(ObjectRef caller, ObjectRef to, ObjectRef from);
    void AddAction(ObjectRef owner, string verb, ActionHandler handler);
    void NotifyFail(string message);
    void InputTo(ObjectRef caller, Action<string> handler, bool noEcho);

    void Receive(ObjectRef ob, string message);
    void Write(string message);
    void TellObject(ObjectRef ob, string message);
    void TellRoom(ObjectRef room, string message, ObjectRef[] exclude);
    void Say(ObjectRef caller, string message);

    bool GmcpSend(ObjectRef caller, ObjectRef player, string package, object? value);
    bool GmcpActive(ObjectRef player);

    long TimeMs();
    string[] TimeZoneNames();
    int LingerSeconds();
    bool DropConnection(ObjectRef caller, ObjectRef target);
    string LocalTime(string zoneId);
    int IdleSeconds(ObjectRef ob);
    int IdleLimitSeconds();
    int TimeoutSeconds();
    int CallOut(ObjectRef owner, Action function, double delaySeconds);
    bool RemoveCallOut(ObjectRef owner, int id);
    void SetHeartBeat(ObjectRef ob, bool on);

    bool SaveObject(ObjectRef caller, string path);
    bool RestoreObject(ObjectRef caller, string path);

    bool CheckAdminSecret(ObjectRef caller, string candidate);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string Sha256(string text);
    IReadOnlyList<DirEntry>? GetDir(ObjectRef caller, string path);
    string? ReadFile(ObjectRef caller, string path);
    void WriteFile(ObjectRef caller, string path, string content);
    bool CopyFile(ObjectRef caller, string from, string to);
    bool RenameFile(ObjectRef caller, string from, string to);
    bool RemoveFile(ObjectRef caller, string path);
    bool RemoveDir(ObjectRef caller, string path);
    bool MakeDir(ObjectRef caller, string path);
    string Status();
    void Shutdown(ObjectRef caller);
}

internal static class DriverHost
{
    private static IDriverServices? _services;

    internal static IDriverServices Services
    {
        get => _services ?? throw new InvalidOperationException("No driver is running.");
        set => _services = value;
    }
}
