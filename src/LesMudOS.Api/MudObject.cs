namespace LesMudOS.Api;

/// <summary>
/// Base class of every mudlib object. It carries two things:
///
///   * Applies: virtual methods the driver calls into the object
///     (Create, Init, HeartBeat, ...). Override them.
///   * Efuns: protected methods the object calls into the driver
///     (CloneObject, MoveObject, Write, ...). They are protected so that
///     "this object" is always known to the driver and so that one object
///     cannot invoke an efun on behalf of another through Call().
///
/// Objects are created only by the driver (LoadObject / CloneObject).
/// Do the setup work in Create(), not in a constructor.
/// </summary>
public abstract class MudObject
{
    protected MudObject()
    {
        Self = DriverHost.Services.ClaimPendingRef(this);
    }

    /// <summary>The handle to this object. Pass this to other objects, never `this`.</summary>
    public ObjectRef Self { get; }

    // =====================================================================
    // Applies (driver -> object)
    // =====================================================================

    /// <summary>Called once, right after the object is loaded or cloned.</summary>
    public virtual void Create() { }

    /// <summary>
    /// Called when a living object comes within reach of this object (it
    /// enters this object, enters the same environment, or this object is
    /// moved next to it). ThisPlayer() is that living object. This is where
    /// an object offers commands with AddAction.
    /// </summary>
    public virtual void Init() { }

    /// <summary>Called every heartbeat interval while SetHeartBeat(true) is in effect.</summary>
    public virtual void HeartBeat() { }

    /// <summary>Does this object answer to the given name? Used by Present().</summary>
    public virtual bool Id(string id) => false;

    /// <summary>One-line description, or null if the object is invisible.</summary>
    public virtual string? Short() => null;

    /// <summary>Full description shown when the object is looked at.</summary>
    public virtual string Long() => (Short() ?? "You see nothing special") + ".\n";

    /// <summary>
    /// Receives all text sent to this object. The default sends it down the
    /// network connection if the object is interactive. NPCs can override this
    /// to react to what they hear.
    /// </summary>
    public virtual void CatchTell(string message) => DriverHost.Services.Receive(Self, message);

    /// <summary>Called on an interactive object when its connection drops.</summary>
    public virtual void NetDead() { }

    /// <summary>Called just before the object is destructed.</summary>
    public virtual void OnDestruct() { }

    // =====================================================================
    // Efuns (object -> driver)
    // =====================================================================

    private static IDriverServices D => DriverHost.Services;

    // ---- Objects and programs ----

    /// <summary>Return the blueprint object for a path, compiling and creating it if needed.</summary>
    protected ObjectRef LoadObject(string path) => D.LoadObject(path);

    /// <summary>Create a new clone from the current program for a path.</summary>
    protected ObjectRef CloneObject(string path) => D.CloneObject(path);

    /// <summary>Find a loaded object by name ("/path" or "/path#id"), or null.</summary>
    protected ObjectRef? FindObject(string name) => D.FindObject(name);

    /// <summary>Destruct an object (this object if omitted). Its contents move to its environment.</summary>
    protected void Destruct(ObjectRef? ob = null) => D.Destruct(Self, ob ?? Self);

    /// <summary>
    /// Discard the compiled program for a path and for every program that
    /// inherits or uses it, then recompile the path. Existing objects keep
    /// running the code they were created with. Returns the invalidated paths.
    /// Throws with the compiler diagnostics if the new source does not compile.
    /// Subject to master.ValidOperation("update", ...).
    /// </summary>
    protected IReadOnlyList<string> UpdateProgram(string path) => D.UpdateProgram(Self, path);

    // ---- Containment ----

    /// <summary>Move this object into a destination object.</summary>
    protected void MoveObject(ObjectRef destination) => D.Move(Self, destination);

    /// <summary>Move another object into a destination object.</summary>
    protected void MoveObject(ObjectRef what, ObjectRef destination) => D.Move(what, destination);

    /// <summary>The object that contains ob (or this object), or null.</summary>
    protected ObjectRef? Environment(ObjectRef? ob = null) => (ob ?? Self).Environment;

    /// <summary>Snapshot of the objects directly inside ob (or this object).</summary>
    protected IReadOnlyList<ObjectRef> AllInventory(ObjectRef? ob = null) => (ob ?? Self).Inventory.ToArray();

    /// <summary>First object inside the container (this object if omitted) whose Id() accepts the name.</summary>
    protected ObjectRef? Present(string id, ObjectRef? container = null) => D.Present(id, container ?? Self);

    // ---- Living and interactive objects ----

    /// <summary>The living object whose command, heartbeat or arrival caused the current execution.</summary>
    protected ObjectRef? ThisPlayer() => D.ThisPlayer;

    /// <summary>
    /// The object whose code called into this object: through Call(), through
    /// an efun that triggered one of this object's applies, or directly. Null
    /// if the driver called this object at top level (heartbeat, call-out,
    /// command). Use it for security checks instead of trusting an argument.
    /// </summary>
    protected ObjectRef? PreviousObject() => D.PreviousObject(Self);

    /// <summary>The verb of the command being executed.</summary>
    protected string QueryVerb() => D.Verb;

    /// <summary>Mark this object as living: it can receive Init() offers and run commands. Sets ThisPlayer() to this object.</summary>
    protected void EnableCommands() => D.EnableCommands(Self);

    protected bool Living(ObjectRef ob) => ob.Living && ob.IsAlive;

    /// <summary>True if the object has a network connection attached.</summary>
    protected bool Interactive(ObjectRef ob) => ob.Connection is not null && ob.IsAlive;

    /// <summary>All interactive objects.</summary>
    protected IReadOnlyList<ObjectRef> Users() => D.Users();

    /// <summary>Move the network connection from one object to another (login object to player body).</summary>
    protected void Exec(ObjectRef to, ObjectRef from) => D.Exec(Self, to, from);

    /// <summary>Offer a command to ThisPlayer(). The binding disappears when this object and the player part ways.</summary>
    protected void AddAction(string verb, ActionHandler handler) => D.AddAction(Self, verb, handler);

    /// <summary>Set the message shown if no action handles the current command. Default is "What?".</summary>
    protected void NotifyFail(string message) => D.NotifyFail(message);

    /// <summary>
    /// Send the next line typed by ThisPlayer() to a handler instead of the
    /// command parser. With noEcho the client is asked not to echo that line
    /// (telnet IAC WILL ECHO), and echo is restored when it arrives. Use it
    /// for passwords.
    /// </summary>
    protected void InputTo(Action<string> handler, bool noEcho = false) => D.InputTo(Self, handler, noEcho);

    // ---- Communication ----

    /// <summary>Send text to ThisPlayer().</summary>
    protected void Write(string message) => D.Write(message);

    /// <summary>Send text to one object.</summary>
    protected void TellObject(ObjectRef ob, string message) => D.TellObject(ob, message);

    /// <summary>Send text to every living object inside a room, except those excluded.</summary>
    protected void TellRoom(ObjectRef room, string message, params ObjectRef[] exclude) => D.TellRoom(room, message, exclude);

    /// <summary>Send text to everyone in the same room as ThisPlayer() (or this object), except that object.</summary>
    protected void Say(string message) => D.Say(Self, message);

    // ---- Time ----

    /// <summary>Run a function after a delay in seconds. Returns an id for RemoveCallOut. Skipped if this object is destructed first.</summary>
    protected int CallOut(Action function, double delaySeconds) => D.CallOut(Self, function, delaySeconds);

    protected bool RemoveCallOut(int id) => D.RemoveCallOut(Self, id);

    /// <summary>Turn the HeartBeat() apply on or off for this object.</summary>
    protected void SetHeartBeat(bool on) => D.SetHeartBeat(Self, on);

    /// <summary>Seconds since the Unix epoch.</summary>
    protected long Time() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// Milliseconds since the Unix epoch: the same clock that call-outs run on, so
    /// intervals measured with it match the delays given to CallOut. Use it where
    /// whole seconds are too coarse (combat timing).
    /// </summary>
    protected long TimeMs() => D.TimeMs();

    /// <summary>Random integer in [0, n).</summary>
    protected int Random(int n) => n <= 0 ? 0 : System.Random.Shared.Next(n);

    // ---- Persistence ----

    /// <summary>Write every [Saved] field of this object to a save file. Subject to master.ValidWrite.</summary>
    protected bool SaveObject(string path) => D.SaveObject(Self, path);

    /// <summary>Load [Saved] fields from a save file. Returns false if it does not exist. Subject to master.ValidRead.</summary>
    protected bool RestoreObject(string path) => D.RestoreObject(Self, path);

    // ---- Administration ----

    /// <summary>
    /// Compare a candidate against the admin secret the driver was started
    /// with (environment variable LESMUDOS_ADMIN_SECRET). The secret itself is
    /// never visible to mudlib code. Always false if no secret is configured,
    /// or after five wrong guesses since boot. Subject to
    /// master.ValidOperation("admin-secret", ...).
    /// </summary>
    protected bool CheckAdminSecret(string candidate) => D.CheckAdminSecret(Self, candidate);

    /// <summary>
    /// Hash a password for storage: PBKDF2-HMAC-SHA256 with a random 16-byte
    /// salt and 210,000 iterations. The result is self-describing,
    /// "pbkdf2-sha256$&lt;iterations&gt;$&lt;salt&gt;$&lt;hash&gt;" (base64), and is
    /// checked with VerifyPassword. Takes some tens of milliseconds.
    /// </summary>
    protected string HashPassword(string password) => D.HashPassword(password);

    /// <summary>
    /// Check a password against a string made by HashPassword, in constant
    /// time. False if it does not match or the hash is malformed or empty.
    /// </summary>
    protected bool VerifyPassword(string password, string hash) => D.VerifyPassword(password, hash);

    /// <summary>
    /// List a directory of the mudlib: sub-directories first, then files, each
    /// in name order. The path is absolute ("/" is the mudlib root). Returns
    /// null if the path is not a directory. Subject to
    /// master.ValidOperation("get-dir", caller, path).
    /// </summary>
    protected IReadOnlyList<DirEntry>? GetDir(string path) => D.GetDir(Self, path);

    /// <summary>
    /// The text of a mudlib file, or null if there is no such file. The path
    /// is absolute and includes the extension ("/std/room.cs"). Files over
    /// 256 KB are refused. Subject to master.ValidOperation("read-file", caller, path).
    /// </summary>
    protected string? ReadFile(string path) => D.ReadFile(Self, path);

    /// <summary>
    /// Create or replace a mudlib file. The directory must exist. The write is
    /// atomic: readers see the old file or the new one, never half of each.
    /// Content over 256 KB is refused. Writing a file does not recompile it;
    /// call UpdateProgram for that. Subject to
    /// master.ValidOperation("write-file", caller, path).
    /// </summary>
    protected void WriteFile(string path, string content) => D.WriteFile(Self, path, content);

    /// <summary>
    /// Copy a mudlib file. Both paths are absolute and include the extension.
    /// Returns false if the source does not exist. Throws if the source is a
    /// directory, the destination directory does not exist, the destination
    /// already exists (nothing is ever overwritten), both paths are the same,
    /// or the file is over 256 KB. Subject to
    /// master.ValidOperation("read-file", caller, from) and
    /// master.ValidOperation("write-file", caller, to).
    /// </summary>
    protected bool CopyFile(string from, string to) => D.CopyFile(Self, from, to);

    /// <summary>
    /// Move or rename a mudlib file, within a directory or to another one.
    /// Returns false if the source does not exist. Throws under the same
    /// conditions as CopyFile, except that there is no size limit. Moving a
    /// file does not unload its program. Subject to
    /// master.ValidOperation("write-file", caller, path) for both paths.
    /// </summary>
    protected bool RenameFile(string from, string to) => D.RenameFile(Self, from, to);

    /// <summary>
    /// Delete one mudlib file. The path is absolute and includes the extension.
    /// Returns false if nothing exists at the path. Throws if the path is a
    /// directory or "/", or goes through a symbolic link. This cannot be
    /// undone. Deleting a file does not unload its program or its objects.
    /// Subject to master.ValidOperation("remove-file", caller, path).
    /// </summary>
    protected bool RemoveFile(string path) => D.RemoveFile(Self, path);

    /// <summary>
    /// Remove one empty mudlib directory. Returns false if nothing exists at
    /// the path. Throws if the path is a file or "/", goes through a symbolic
    /// link, or the directory is not empty on disk: hidden files count, even
    /// though GetDir does not show them. Never recursive. Subject to
    /// master.ValidOperation("remove-dir", caller, path).
    /// </summary>
    protected bool RemoveDir(string path) => D.RemoveDir(Self, path);

    /// <summary>
    /// Create a mudlib directory. The parent must exist. Returns false if
    /// something with that name is already there. Subject to
    /// master.ValidOperation("make-dir", caller, path).
    /// </summary>
    protected bool MakeDir(string path) => D.MakeDir(Self, path);

    /// <summary>Human-readable driver statistics: object count, program versions loaded and unloaded.</summary>
    protected string DriverStatus() => D.Status();

    /// <summary>Stop the driver. Subject to master.ValidOperation("shutdown", ...).</summary>
    protected void Shutdown() => D.Shutdown(Self);
}
