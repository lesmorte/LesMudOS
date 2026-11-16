# LesMudOS driver API

Everything a mudlib file can use from the driver is in the `LesMudOS.Api` assembly. This reference covers the version implemented by the proof of concept. Design background is in [DESIGN.md](DESIGN.md).

## 1. Mudlib files

A mudlib file is a C# source file under the mudlib root. Its path without `.cs` is its mudlib path: `mudlib/std/room.cs` is `/std/room`.

Rules:

- Define **exactly one public class derived from `MudObject`**. Its name is free; objects are addressed by path, not class name. Other helper types in the file must not derive from `MudObject`.
- An `abstract` class makes the file inherit-only.
- Do setup in `Create()`. Do not write constructors that take arguments.
- No namespace is needed.

### Directives

```csharp
// @inherit /std/room      this file's class derives from a class in /std/room
// @use /std/item          this file refers to types in /std/item without inheriting
```

Both make the named file's types visible and record a dependency edge, so that updating the dependency invalidates this file. They are transitive: inheriting `/std/item` also brings in `/std/thing`.

### Implicit usings

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LesMudOS.Api;
```

### What mudlib code may use

| Allowed | Rejected at compile time |
|---|---|
| Types in `System`, `System.Collections`, `System.Collections.Generic`, `System.Linq`, `System.Text`, `LesMudOS.Api`, and mudlib types brought in by a directive | Everything else, including `System.IO`, `System.Reflection`, `System.Threading`, `System.Net`, `System.Diagnostics` |
| | `System.Type`, `Activator`, `Environment`, `GC`, `AppDomain`, `AppContext`, `Console`, `WeakReference`, `IntPtr` |
| | `GetType()`, `typeof`, `goto`, `lock`, `async`/`await`, finalizers, `unsafe`, `stackalloc`, `dynamic` |

### Example

```csharp
// @inherit /std/room

public class Square : Room
{
    public override void Create()
    {
        ShortDesc = "Town Square";
        LongDesc = "Cobblestones spread out around a dry fountain.";
        Exits["north"] = "/domains/town/tavern";
        MoveObject(CloneObject("/domains/town/lantern"), Self);
    }
}
```

## 2. `ObjectRef`

The handle to an object. Store and pass handles, never `this`.

| Member | Description |
|---|---|
| `string Name` | `/path` for a blueprint, `/path#n` for a clone |
| `string ProgramPath` | Mudlib path of the source file |
| `bool IsClone` | |
| `bool IsAlive` | False once destructed |
| `object? Call(string function, params object?[] args)` | Late-bound call to a public method (`call_other`). Returns null if the object is dead or has no public method with that name and argument count. |
| `T? Call<T>(string function, params object?[] args)` | Same, with a typed result. Returns `default(T)` if the function is missing or returns another type. |
| `T? As<T>()` | Typed view, or null. Safe for interfaces in `LesMudOS.Api`. For mudlib types it returns null once caller and target are on different program versions. Do not store the result. |

`==` on two handles tests object identity.

## 3. Applies

Virtual methods on `MudObject` that the driver calls. Override the ones you need.

| Apply | Called when | `ThisPlayer()` |
|---|---|---|
| `void Create()` | Once, right after the object is loaded or cloned | Unchanged |
| `void Init()` | A living object comes within reach: it enters this object or this object's environment, or this object is moved next to it or into it | That living object |
| `void HeartBeat()` | Every 2 seconds while `SetHeartBeat(true)` | This object if living, else null |
| `bool Id(string id)` | `Present()` asks whether this object answers to a name (lower-cased) | Unchanged |
| `string? Short()` | By mudlib convention; null means invisible | Unchanged |
| `string Long()` | By mudlib convention | Unchanged |
| `void CatchTell(string message)` | Text is sent to this object. The default writes it to the object's connection. | Unchanged |
| `void NetDead()` | The object's connection dropped | This object |
| `void OnDestruct()` | Just before this object is destructed by `Destruct` | Unchanged |

Called by name on specific objects:

| Function | On | Called when |
|---|---|---|
| `void Logon()` | The object returned by `master.Connect()` | A connection has been attached to it |

## 4. Efuns

Protected methods on `MudObject`. `ob` parameters default to this object where marked optional.

### Objects and programs

| Efun | Description |
|---|---|
| `ObjectRef LoadObject(string path)` | The blueprint for a path. Compiles and creates it if needed. Throws on compile error or if the file is abstract. |
| `ObjectRef CloneObject(string path)` | A new clone from the current program for the path |
| `ObjectRef? FindObject(string name)` | A loaded object by name, or null. Does not load. |
| `void Destruct(ObjectRef? ob = null)` | Destruct an object. Contents move to its environment, or are destroyed with it if it has none. A connection, if any, is flushed and closed. Destructing another object needs `ValidOperation("destruct")`. |
| `IReadOnlyList<string> UpdateProgram(string path)` | Invalidate the program for the path and all dependents, then recompile the path. Returns the invalidated paths. Live objects are untouched. Throws with the diagnostics on compile error. Needs `ValidOperation("update")`. |

### Containment

| Efun | Description |
|---|---|
| `void MoveObject(ObjectRef destination)` | Move this object |
| `void MoveObject(ObjectRef what, ObjectRef destination)` | Move another object. Triggers unbinding of stale actions, then `Init` as described in DESIGN.md section 7. |
| `ObjectRef? Environment(ObjectRef? ob = null)` | The container, or null |
| `IReadOnlyList<ObjectRef> AllInventory(ObjectRef? ob = null)` | Snapshot of the direct contents |
| `ObjectRef? Present(string id, ObjectRef? container = null)` | First object in the container whose `Id(id)` is true |

### Living and interactive objects

| Efun | Description |
|---|---|
| `ObjectRef? ThisPlayer()` | The living object the current execution is for |
| `string QueryVerb()` | The verb of the command being executed |
| `void EnableCommands()` | Mark this object living. Also makes it `ThisPlayer()` for the rest of the execution, so it can bind its own commands. |
| `bool Living(ObjectRef ob)` | |
| `bool Interactive(ObjectRef ob)` | Has a connection |
| `IReadOnlyList<ObjectRef> Users()` | All interactive objects |
| `void Exec(ObjectRef to, ObjectRef from)` | Move a connection between objects. Needs `ValidOperation("exec")`. |
| `void AddAction(string verb, ActionHandler handler)` | Bind a verb for `ThisPlayer()`, owned by this object. `ActionHandler` is `bool (string args)`; return true if handled. Rebinding the same verb from the same object replaces the old binding. |
| `void NotifyFail(string message)` | Message shown if no handler accepts the command |
| `void InputTo(Action<string> handler)` | Route the player's next line to a handler instead of the command parser |

### Communication

| Efun | Description |
|---|---|
| `void Write(string message)` | To `ThisPlayer()` |
| `void TellObject(ObjectRef ob, string message)` | To one object |
| `void TellRoom(ObjectRef room, string message, params ObjectRef[] exclude)` | To every living object directly inside `room` |
| `void Say(string message)` | To everyone in the room of `ThisPlayer()` (or of this object, if there is none) except that object |

Messages are not newline-terminated for you; include `\n`.

### Time

| Efun | Description |
|---|---|
| `int CallOut(Action function, double delaySeconds)` | Schedule a call. Returns an id. Skipped if this object is destructed first. `ThisPlayer()` during the call is whoever it was at scheduling time, if still alive. |
| `bool RemoveCallOut(int id)` | Cancel a pending call-out owned by this object |
| `void SetHeartBeat(bool on)` | |
| `long Time()` | Unix time in seconds |
| `int Random(int n)` | Uniform in `[0, n)` |

### Persistence

| Efun | Description |
|---|---|
| `bool SaveObject(string path)` | Write this object's `[Saved]` fields to `save/<path>.json`. Throws unless `master.ValidWrite` allows it. |
| `bool RestoreObject(string path)` | Read them back. False if there is no file. Throws unless `master.ValidRead` allows it. |

```csharp
[Saved] private string title = "the newcomer";
[Saved] private int logins;
```

Saved fields may be numbers, strings, booleans, and lists, arrays or dictionaries of those.

### Administration

| Efun | Description |
|---|---|
| `string DriverStatus()` | Object, user, timer, program-version and memory statistics |
| `void Shutdown()` | Stop the driver. Needs `ValidOperation("shutdown")`. |

## 5. The master object

`/secure/master` is loaded first and must implement `IMaster`.

| Member | Description |
|---|---|
| `ObjectRef Connect()` | Return the object that takes a new connection. The driver attaches the connection and calls its `Logon()`. |
| `IEnumerable<string> Preload()` | Paths to load at boot |
| `bool ValidRead(string path, ObjectRef caller)` | Gate for `RestoreObject` |
| `bool ValidWrite(string path, ObjectRef caller)` | Gate for `SaveObject` |
| `bool ValidOperation(string operation, ObjectRef caller, string? arg)` | Gate for `"update"` (arg: path), `"exec"` (arg: target name), `"destruct"` of another object (arg: target name), `"shutdown"` |
| `void LogError(string objectName, string message)` | Called after any error in an execution |

## 6. Errors

| Exception | Raised when |
|---|---|
| `MudRuntimeException` | The driver refuses an efun call (dead object, permission denied, illegal path) |
| `EvalCostExceededException` | An execution used more than 5,000,000 ticks |
| `InsufficientExecutionStackException` | Recursion went too deep (reported to the player as "Recursion too deep") |
| Compile failure (a `MudRuntimeException`) | `LoadObject`, `CloneObject` or `UpdateProgram` hit a file that does not compile or breaks the sandbox rules. The message lists `path.cs(line,col): code: text`. |

Any exception that reaches the driver aborts the current execution, is logged, and is shown to `ThisPlayer()` prefixed with `***`. Mudlib code may catch exceptions, but cannot use that to outrun the tick limit.

## 7. Planned additions

Not in the proof of concept; names are provisional.

| Efun or apply | Purpose |
|---|---|
| `ReadFile`, `WriteFile`, `GetDir`, `FileSize`, `RemoveFile` | In-game editing, gated by `ValidRead` / `ValidWrite` |
| `PreviousObject()` | Caller identification for mudlib-level security checks |
| `Reset()` apply and `SetResetInterval` | Periodic restocking |
| `CleanUp()` apply | Unload idle objects |
| `Command(string)` | Force a living object to execute a command |
| `Shadow(ObjectRef)` | LPmud shadows |
| `QueryIdle`, `QueryIpNumber`, `Snoop` | Connection inspection |
| `Task<string> Prompt(string)` | `await`-style input, once async scripts are supported |
