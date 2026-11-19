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
| `void CatchTell(string message)` | Text is sent to this object: `Write`, `TellObject`, `TellRoom`, `Say` and the `NotifyFail` message (or `What?`) all arrive here. The default writes it to the object's connection. `/std/player` overrides it to render colour tags. | Unchanged |
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
| `ObjectRef? PreviousObject()` | The object whose code called into this one: through `Call`, through an efun that triggered one of this object's applies, or directly. Null if the driver entered this object at top level (command, heartbeat, call-out). Use it for security checks instead of trusting an argument. |
| `string QueryVerb()` | The verb of the command being executed |
| `void EnableCommands()` | Mark this object living. Also makes it `ThisPlayer()` for the rest of the execution, so it can bind its own commands. When called from a constructor or `Create()` of an object that another object is cloning, `ThisPlayer()` is restored to the cloner when the clone call returns, so creating a creature from a command does not hijack the command. |
| `bool Living(ObjectRef ob)` | |
| `bool Interactive(ObjectRef ob)` | Has a connection |
| `IReadOnlyList<ObjectRef> Users()` | All interactive objects |
| `void Exec(ObjectRef to, ObjectRef from)` | Move a connection between objects. Needs `ValidOperation("exec")`. |
| `void AddAction(string verb, ActionHandler handler)` | Bind a verb for `ThisPlayer()`, owned by this object. `ActionHandler` is `bool (string args)`; return true if handled. Rebinding the same verb from the same object replaces the old binding. |
| `void NotifyFail(string message)` | Message shown if no handler accepts the command. Like every other message it is delivered with `CatchTell` on the player's object (before colour support it was written straight to the connection, which a player body could not intercept). The `> ` prompt and the driver's own `*** error` lines still go straight to the connection. |
| `void InputTo(Action<string> handler, bool noEcho = false)` | Route the player's next line to a handler instead of the command parser. With `noEcho` the driver sends telnet IAC WILL ECHO, so the client stops echoing what is typed; when the line arrives it sends IAC WONT ECHO and a newline before calling the handler. Use it for passwords. |

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
| `int CallOut(Action function, double delaySeconds)` | Schedule a call. Returns an id. Skipped if this object is destructed first. `ThisPlayer()` during the call is whoever it was at scheduling time, if still alive. The delay may be fractional and is honoured to the millisecond: the game loop sleeps exactly until the earliest call-out is due (it is not rounded up to a second or to the 2-second heartbeat), so a call-out runs within a few milliseconds of its time unless another execution is running. The combat clock (docs/COMBAT_TIMING.md) is built on this. |
| `bool RemoveCallOut(int id)` | Cancel a pending call-out owned by this object |
| `void SetHeartBeat(bool on)` | |
| `long Time()` | Unix time in seconds |
| `long TimeMs()` | Unix time in milliseconds, from the same clock as the call-out queue. For measuring and scheduling intervals shorter than a second; the combat engine stamps every action with it. |
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

### Passwords

Mudlib code cannot use `System.Security.Cryptography`, so password hashing is done by the driver.

| Efun | Description |
|---|---|
| `string HashPassword(string password)` | A hash for storage: PBKDF2-HMAC-SHA256, a random 16-byte salt, 210,000 iterations. The result describes itself: `pbkdf2-sha256$<iterations>$<salt base64>$<hash base64>`. Takes some tens of milliseconds on the game thread. |
| `bool VerifyPassword(string password, string hash)` | Recompute and compare in constant time. False if the password does not match, or if the hash is empty or malformed (it never throws for a bad hash). |

Neither efun needs the master's permission: a hash is useless without the save file it is stored in, which the master protects. The driver never logs a password or a hash, and builds no error message from either.

### Administration

| Efun | Description |
|---|---|
| `IReadOnlyList<DirEntry>? GetDir(string path)` | List a mudlib directory: sub-directories first, then files, in name order. `DirEntry` has `Name`, `IsDirectory` and `Size` (bytes; 0 for directories). The path must be absolute and clean (`/` is the mudlib root; no `.` or `..` segments, no names starting with a dot), so it cannot leave the mudlib. Returns null if it is not a directory. Needs `ValidOperation("get-dir")` (arg: path). |
| `string? ReadFile(string path)` | The text of a mudlib file, or null if it does not exist. Paths here include the extension (`/std/room.cs`) and follow the same rules as `GetDir`. Files over 256 KB are refused. Needs `ValidOperation("read-file")` (arg: path). |
| `void WriteFile(string path, string content)` | Create or replace a mudlib file, atomically. The directory must exist; content over 256 KB is refused. Writing does not recompile: call `UpdateProgram`. Every write is logged by the driver. Needs `ValidOperation("write-file")` (arg: path). |
| `bool CopyFile(string from, string to)` | Copy a mudlib file. Paths follow the same rules as `ReadFile`. False if the source does not exist. Throws if the source is a directory, the destination directory does not exist, the destination already exists (nothing is ever overwritten), both paths are the same, or the file is over 256 KB. Logged by the driver. Needs `ValidOperation("read-file")` (arg: from) and `ValidOperation("write-file")` (arg: to). |
| `bool RenameFile(string from, string to)` | Move or rename a mudlib file, in the same directory or another. Same rules as `CopyFile`, without the size limit. Does not unload the program compiled from the old path. Logged by the driver. Needs `ValidOperation("write-file")` for both paths. |
| `bool RemoveFile(string path)` | Delete one mudlib file. Paths follow the same rules as `ReadFile`. False if nothing exists at the path. Throws if the path is a directory or `/`, or if it or any directory on the way to it is a symbolic link. Cannot be undone, and does not unload the program or objects made from the file. Logged by the driver. Needs `ValidOperation("remove-file")` (arg: path). |
| `bool RemoveDir(string path)` | Remove one empty mudlib directory. False if nothing exists at the path. Throws if the path is a file or `/`, if it or any directory on the way to it is a symbolic link, or if the directory is not empty. Empty means empty on disk: hidden files (dot-files, and the temporary files of `WriteFile` and `CopyFile`) count, although `GetDir` does not show them. Never recursive. Logged by the driver. Needs `ValidOperation("remove-dir")` (arg: path). |
| `bool MakeDir(string path)` | Create a mudlib directory whose parent exists. False if the name is taken. Needs `ValidOperation("make-dir")` (arg: path). |
| `string DriverStatus()` | Object, user, timer, program-version and memory statistics |
| `bool CheckAdminSecret(string candidate)` | Compare a candidate with the secret in the driver's `LESMUDOS_ADMIN_SECRET` environment variable. The secret is never readable from mudlib code. Always false if none is configured, or after five wrong guesses since boot. Needs `ValidOperation("admin-secret")`. |
| `void Shutdown()` | Stop the driver. Needs `ValidOperation("shutdown")`. |

## 5. The master object

`/secure/master` is loaded first and must implement `IMaster`.

| Member | Description |
|---|---|
| `ObjectRef Connect()` | Return the object that takes a new connection. The driver attaches the connection and calls its `Logon()`. |
| `IEnumerable<string> Preload()` | Paths to load at boot |
| `bool ValidRead(string path, ObjectRef caller)` | Gate for `RestoreObject` |
| `bool ValidWrite(string path, ObjectRef caller)` | Gate for `SaveObject` |
| `bool ValidOperation(string operation, ObjectRef caller, string? arg)` | Gate for `"update"` (arg: path), `"exec"` (arg: target name), `"destruct"` of another object (arg: target name), `"shutdown"`, `"admin-secret"`, `"get-dir"`, `"read-file"`, `"write-file"`, `"make-dir"`, `"remove-file"`, `"remove-dir"` (arg: path) |
| `void LogError(string objectName, string message)` | Called after any error in an execution |

## 6. Wizard levels in the sample mudlib

Wizard levels are mudlib policy, not a driver feature. The driver contributes only `PreviousObject()` and `CheckAdminSecret()`.

`/secure/wizlist` is the registry. It maps player names to levels 1 to 10; anyone not listed is level 0. Its public functions:

| Function | Description |
|---|---|
| `int QueryLevel(string name)` | 0 if not a wizard |
| `List<string> QueryWizards()` | `name (level)`, highest first |
| `string? Promote(string target, int level)` | Set a level. Returns null on success or the reason for refusing. The actor is taken from `PreviousObject()` and must be the body of the player issuing the current command. |
| `bool Bootstrap(string secret)` | Make the calling player level 10 if the admin secret matches and no level-10 wizard exists |

Rules enforced by `Promote`:

- The actor must be at least level 1.
- The new level must be between 0 and the actor's own level.
- The target must currently be below the actor's level, and cannot be the actor.
- The target must be online or already in the registry.

Commands in `/std/player` and the level each needs:

| Command | Level |
|---|---|
| `promote <player> <level>`, `wizards` | 1 |
| `mudstat` | 1 |
| `goto <room path or player>`, `stat <me, here or name>` | 1 |
| `heal [<target>]` | 5 |
| `smite <target>` | 10 |
| `pwd`, `cd [<dir>]`, `list [<dir>]` (alias `ls`), `cat <file>` | 1, anywhere |
| `mkdir <dir>`, `edit <file>`, `copy <from> <to>`, `move <from> <to>`, `rename <file> <newname>`, `delete <file>`, `rmdir <dir>`, `update <path>`, `clone <path>` | 1 in your own home `/wiz/<you>/...`; otherwise as in the next table |
| `shutdown` (saves every online player first) | 10 |
| `promote <secret>` | Any, but only works while there is no level-10 wizard |

#### Wizard homes and who may change what

Every wizard (level 1 or higher) has a home directory `/wiz/<name>`, where `<name>` is the player's own name in lower case. The player body creates it with `MakeDir` when the player becomes a wizard (the level-10 bootstrap or `promote`) and again at login if it is missing, and says so once, in one line, when it makes it. Demotion to level 0 leaves the directory and its files where they are; the former wizard has no wizard commands until promoted again, and then has the home back.

The single rule is `ChangeRefusal(path)` in `/std/player`. Every command that creates, writes, copies to, moves, renames or removes a file or directory asks it for each path it touches (`move` and `rename` ask for both, `copy` for the destination only; reading is open to every wizard), and the editor and `delete` ask again when they save or when the answer arrives. Names are compared whole segment by whole segment and without regard to case, so `bob` has nothing to do with `/wiz/bobby` and `/WIZ/Bob` is `/wiz/bob`.

| Path | `mkdir`, `edit`, `copy` to, `move`, `rename`, `delete`, `rmdir` | `update` | `clone` | `goto` |
|---|---|---|---|---|
| Under your own home, `/wiz/<you>/...` | level 1 | level 1 | level 1 | level 1 |
| Under another wizard's home | level 5 | level 5 | level 3 | level 1 |
| The home directory itself `/wiz/<name>`, `/wiz`, or a file directly in `/wiz` | level 10 (see below) | level 5 | level 3 | level 1 |
| `/secure`, `/std` | level 10 | level 5 | level 3 | level 1 |
| Anywhere else | level 5 | level 5 | level 3 | level 1 |

Reading (`list`, `cd`, `cat`) is level 1 everywhere. A wizard below level 5 who tries to change something in another wizard's home is refused with `/wiz/<name> is another wizard's home. Below wizard level 5 you may read it (list, cd, cat) but not change it.` `update`, `clone` and `goto` are not loosened for other people's files: below the level shown they are refused (`goto` is level 1 everywhere, as before). The home directory and `/wiz` cannot be removed, renamed or moved, or have files created directly in them, by anyone below level 10, the owner included; the owner controls everything under the home. (The one exception: `mkdir` of your own home, which normally exists already, is allowed.) The directory commands cannot rename directories anyway, since `RenameFile` moves files only.

`~` at the start of a path (`~` alone, or `~/...`) means the wizard's own home in every command that takes a path, including `goto`, `update` and `clone`; `cd` with no argument goes there (`cd /` goes to the root). `~name` is not special. `pwd` and all messages show the real path.

The master gives file efuns to player bodies only. Code that a wizard writes under `/wiz` is an ordinary program: it gets no file efuns, cannot call `Promote` or `Bootstrap`, cannot read or write save files, and cannot reach the admin secret. The tests in the "wizard homes" section of `tests/smoke_test.py` try each of these from a level-1 wizard's object.

`mudlib/Mudlib.csproj` leaves `wiz/**` out, so files that wizards create at run time cannot break `dotnet build`.

Each wizard has a current mudlib directory, saved with the player. `cd` and `list` take absolute or relative paths, with `.` and `..`; `cd` alone goes to the wizard's home (and `cd /` to the root). `update` and `clone` resolve relative paths against it too.

File names given to `cat`, `edit`, `copy`, `move`, `rename` and `delete` get `.cs` appended if they have no extension. If the destination of `copy` or `move` is an existing directory, the file keeps its name inside it. `rename` takes a bare new name and keeps the file in its directory. None of them overwrites an existing file. `copy` needs permission to write the destination; `move` and `rename` change both places, so they need it for both. Changing `/secure` and `/std` (compared without regard to case, since some file systems ignore it) needs level 10 because that code is what enforces wizard levels: a wizard who could edit `/std/player.cs` could grant themselves anything.

`delete` and `rmdir` are the only commands that destroy anything, and nothing they remove can be recovered. `delete <file>` asks `Delete <path>? This cannot be undone. (y/n)`; only `y` or `yes` (in any case) deletes, and anything else answers `Not deleted.` The level is checked again when the answer arrives. If any other message reached the wizard between the question and the answer (it may have been another object's question that the `y` was meant for), nothing is deleted and `delete` must be typed again. If the file was a `.cs` file whose object is loaded, the object stays loaded until it is destructed or the driver restarts, and `delete` says so. `delete` on a directory says to use `rmdir`. `rmdir <dir>` removes an empty directory without asking. It refuses a directory with anything in it, hidden files included, and the directory you are in or one above it. There is no recursive delete: empty a directory file by file first.

#### The editor

`edit <file>` opens a line editor on an existing or new file. While it is open, everything the player types goes to the editor, which prompts with `:`.

| Command | Effect |
|---|---|
| `p [n [m]]` | Print the file, line n, or lines n to m, with line numbers |
| `a [n]` | Add lines after line n (default: at the end) |
| `i [n]` | Insert lines before line n (default: at the top) |
| `c n [m]` | Replace line n, or lines n to m, with new lines |
| `d n [m]` | Delete line n, or lines n to m |
| `w` | Write the file |
| `x` | Write the file and leave |
| `q`, `q!` | Leave; `q!` discards unsaved changes |
| `h` | Help |

After `a`, `i` or `c`, every line typed or pasted goes into the file, indentation included, until a line containing only `.`. Saving does not load the code; the editor prints the `update` command to run.

`goto`, `stat`, `restore` and `slay` live in `/std/adventurer` with the other gameplay commands. `/secure/lost_property` is a second blueprint-only daemon beside the wizard registry: the master lets only the lost-property blueprint read and write its save file `/secure/lost_property` (and read the old `/secure/vault`, once, to carry old records over), and the store accepts records only from `/std/corpse` clones (see GAME_SYSTEMS.md, Corpses). `/secure/finds` is a third such daemon: the master lets only its blueprint read and write `/secure/finds`, which records who has taken each hidden stash (see GAME_SYSTEMS.md, Finds).

Below the required level a command answers `What?`, the same as a verb that does not exist. Levels are checked on every use, so a demotion takes effect immediately.

## 7. Login in the sample mudlib

Accounts and passwords are mudlib policy too. `/secure/login` owns a new connection and asks for a name (2 to 16 letters; a name already online is refused). Only a player body may read player save files, so it then clones `/std/player` and asks the body about the account:

| Body function | Called by the login object to |
|---|---|
| `string BeginLogin(string name)` | Take the name and restore its save file, once. Returns `"new"` (no save file), `"password"`, or `"unset"` (an account saved before passwords existed); `""` if refused. |
| `bool CheckPassword(string password)` | Check the password of a `"password"` account. Fails for good after three wrong tries. |
| `string? CheckNewPassword(string password)` | Why a password may not be used (fewer than 8 characters, or the player's name in any case), or null. Changes nothing. |
| `bool SetPassword(string password)` | Store the hash for a `"new"` or `"unset"` account and save at once, so a new name is reserved with its password. |
| `void SetupPlayer(string name)` | Enter the game, after `Exec` has moved the connection to the body. Needs a successful `CheckPassword` or `SetPassword`. |

Every one of these except `CheckNewPassword` checks `PreviousObject()`: `BeginLogin` accepts only a clone of `/secure/login` that has a connection, and records it; the others accept only that same login object. All of them do nothing once the player is in the game, so other mudlib objects cannot use them to test or set a password or to take over a body. The hash is a `[Saved]` field of the body, stored in `save/players/<name>.json`, and no command shows it.

The prompts, all password lines with echo off:

- New name: `Creating a new account for <Name>.`, then `Choose a password (at least 8 characters):` and `Type it again:`. A refused or mismatched password goes back to the first prompt.
- Account with a password: `Password:`. After three wrong passwords in all: `Wrong password. Goodbye.` and the connection is closed.
- Account without a password: `<Name>, your account has no password. You must set one before you can play.`, then the same two prompts as for a new account.

Two connections cannot set up the same new name at once, and the name is checked against the players online again just before the hand-over. If the login fails or the connection drops, the login object destructs the body it cloned; the sample master allows a `/secure/login` clone to destruct a player body that has no connection, and nothing else.

In the game, `password` asks for the current password, then the new one twice, with the same rules.

## 8. Errors

| Exception | Raised when |
|---|---|
| `MudRuntimeException` | The driver refuses an efun call (dead object, permission denied, illegal path) |
| `EvalCostExceededException` | An execution used more than 5,000,000 ticks |
| `InsufficientExecutionStackException` | Recursion went too deep (reported to the player as "Recursion too deep") |
| Compile failure (a `MudRuntimeException`) | `LoadObject`, `CloneObject` or `UpdateProgram` hit a file that does not compile or breaks the sandbox rules. The message lists `path.cs(line,col): code: text`. |

Any exception that reaches the driver aborts the current execution, is logged, and is shown to `ThisPlayer()` prefixed with `***`. Mudlib code may catch exceptions, but cannot use that to outrun the tick limit.

## 9. Planned additions

Not in the proof of concept; names are provisional.

| Efun or apply | Purpose |
|---|---|
| `Reset()` apply and `SetResetInterval` | Periodic restocking |
| `CleanUp()` apply | Unload idle objects |
| `Command(string)` | Force a living object to execute a command |
| `Shadow(ObjectRef)` | LPmud shadows |
| `QueryIpNumber`, `Snoop` | Connection inspection |
| `IdleSeconds(ObjectRef)`, `IdleLimitSeconds()`, `TimeoutSeconds()` | Implemented: seconds since the player last typed, and the two server settings (see DEPLOY.md) |
| `virtual Prompt()`, `PromptsAfterOutput()` | Implemented: the line the driver prints for a player after each command (default `> `); a player object returns its own |
| `SplitOrdinal(text, out name)` | Implemented: `rat 2`, `2.rat`, `second rat` to a position and a name |
| `Task<string> Prompt(string)` | `await`-style input, once async scripts are supported |
