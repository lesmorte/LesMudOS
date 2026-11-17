# LesMudOS

An LPmud-style game driver in C#, with C# as the mudlib language.

The driver is small and knows nothing about the game. The world is a directory of `.cs` files that are compiled on demand with Roslyn, one object per file, and can be edited and reloaded while players are connected.

**Status: proof of concept.** It boots a sample mudlib, accepts telnet connections, and demonstrates the parts that carry the technical risk: per-file compilation, hot reload with unloading, a compile-time sandbox, and protection against runaway code. It is not ready to host a game.

- [Design](docs/DESIGN.md)
- [Driver API reference](docs/DRIVER_API.md)

## Quick start

Requires the .NET 10 SDK.

```
dotnet build
dotnet run --project src/LesMudOS.Driver -- --mudlib mudlib --port 4000
```

Then, from another terminal:

```
telnet localhost 4000
```

```
By what name are you known? keith

Welcome, Keith. Type 'help' for commands.

Town Square
Cobblestones spread out around a dry fountain. A tavern door stands open to the north.
Obvious exits: north
A brass lantern is here.
> get lantern
You take a brass lantern.
> north
The Compiled Griffin
A low-beamed tavern that smells of woodsmoke. The square lies to the south.
Obvious exits: south
A ginger tavern cat is here.
> pet cat
The cat purrs like a small engine.
```

### Become a wizard

New players are wizard level 0 and have no wizard commands. The first level-10 wizard is created with a secret that you give the driver in an environment variable:

```
LESMUDOS_ADMIN_SECRET='pick something long' dotnet run --project src/LesMudOS.Driver -- --mudlib mudlib --port 4000
```

(PowerShell: `$env:LESMUDOS_ADMIN_SECRET = 'pick something long'` on its own line first.) Then, in the game:

```
> promote pick something long
```

This works only while no level-10 wizard exists, and stops working after five wrong guesses until the driver is restarted. After that, wizards promote each other with `promote <player> <level>`, up to their own level. Levels are stored in `save/secure/wizlist.json`.

| Level | Commands |
|---|---|
| 1 | `promote`, `wizards`, `mudstat`, `pwd`, `cd`, `list` (browse the mudlib directories) |
| 3 | `clone` |
| 1 | `cat` (view a file) |
| 5 | `update`, `mkdir`, `edit` (line editor; level 10 for files under `/secure` and `/std`) |
| 10 | `shutdown` |

### Create a file from inside the game

```
> cd /domains
> mkdir lab
> cd lab
> edit widget
New file /domains/lab/widget.cs.
:a
```

Paste the source, finish with a line containing only `.`, then `x` to save and leave. `update widget` compiles it and `clone widget` makes one. Type `h` in the editor for its commands.

### Try a live update

As a wizard of level 5 or more, edit `mudlib/domains/town/square.cs` (change the description), then in the game:

```
> update /domains/town/square
Recompiled /domains/town/square; 1 program(s) invalidated: /domains/town/square
Replaced /domains/town/square; moved 1 occupant(s) back in.
> look
```

Introduce a syntax error and update again: the error comes back with file and line, and the room you are standing in is untouched.

## What a mudlib file looks like

```csharp
// @inherit /std/item

public class Lantern : Item
{
    private bool lit;

    public override void Create()
    {
        Name = "lantern";
        LongDesc = "A dented brass lantern. You could light it.";
    }

    public override void Init() => AddAction("light", CmdLight);

    private bool CmdLight(string args)
    {
        if (!Id(args)) { NotifyFail("Light what?"); return false; }
        lit = true;
        CallOut(BurnOut, 20);
        Write("You light the lantern.\n");
        return true;
    }

    private void BurnOut() => lit = false;
}
```

## Repository layout

| Path | Contents |
|---|---|
| `src/LesMudOS.Api` | The contract mudlib code compiles against: `MudObject` (applies and efuns), `ObjectRef`, `IMaster`, `[Saved]` |
| `src/LesMudOS.Driver` | The host: kernel and game loop, Roslyn compiler, tick injector, sandbox analyzer, telnet server, save store |
| `mudlib` | Sample mudlib: `/secure` (master, login, wizard registry), `/std` (thing, item, room, player, void), `/domains/town` |
| `mudlib/Mudlib.csproj` | Not used at run time. Gives IntelliSense for mudlib files and lets `dotnet build` check the whole mudlib. |
| `tests/smoke_test.py` | End-to-end test over TCP |
| `deploy/lesmudos.service` | Sample systemd unit (a starting point; not yet tested on a server) |
| `docs` | Design and API reference |

## Tests

```
dotnet build
python3 tests/smoke_test.py
```

The test starts the driver on port 4777 against a temporary copy of the mudlib, logs in two players, and checks movement, object-supplied verbs, heartbeats, call-outs, wizard levels and promotion, live update of a room and of a base class, compile-error handling, the sandbox, the evaluation-cost limit, the recursion guard, unloading of old program versions, and save/restore.

## Deploying to a Linux VM

On the build machine:

```
dotnet publish src/LesMudOS.Driver -c Release -r linux-x64 --self-contained -o publish
```

Copy `publish/` to `/opt/lesmudos/bin` and `mudlib/` to `/opt/lesmudos/mudlib` on the server, create a `lesmudos` user that owns `/opt/lesmudos/save`, install `deploy/lesmudos.service`, and open the port in the firewall. Do not use single-file publishing: the compiler needs the framework assemblies as files on disk.

## Limitations to know about

- The sandbox is a guard rail for trusted builders, not a security boundary. Do not give mudlib write access to people you would not give a shell account.
- There are no player passwords yet. Anyone who logs in under a wizard's name gets that wizard's level, and a level-5 wizard can write code onto the server with `edit`. Do not expose the port to the internet.
- With in-game editing, the mudlib directory must be writable by the driver. The sample systemd unit keeps it read-only; add it to `ReadWritePaths` if you want `edit` and `mkdir` to work there.
- Traffic is plain-text telnet.

The full list is in [DESIGN.md](docs/DESIGN.md#13-proof-of-concept-status).

## License

No license has been chosen yet. Until one is added, the code is published for reading only.
