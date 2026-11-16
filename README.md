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

### Try a live update

While connected, edit `mudlib/domains/town/square.cs` (change the description), then in the game:

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
| `mudlib` | Sample mudlib: `/secure` (master, login), `/std` (thing, item, room, player, void), `/domains/town` |
| `mudlib/Mudlib.csproj` | Not used at run time. Gives IntelliSense for mudlib files and lets `dotnet build` check the whole mudlib. |
| `tests/smoke_test.py` | End-to-end test over TCP |
| `deploy/lesmudos.service` | Sample systemd unit (a starting point; not yet tested on a server) |
| `docs` | Design and API reference |

## Tests

```
dotnet build
python3 tests/smoke_test.py
```

The test starts the driver on port 4777 against a temporary copy of the mudlib, logs in two players, and checks movement, object-supplied verbs, heartbeats, call-outs, live update of a room and of a base class, compile-error handling, the sandbox, the evaluation-cost limit, the recursion guard, unloading of old program versions, and save/restore.

## Deploying to a Linux VM

On the build machine:

```
dotnet publish src/LesMudOS.Driver -c Release -r linux-x64 --self-contained -o publish
```

Copy `publish/` to `/opt/lesmudos/bin` and `mudlib/` to `/opt/lesmudos/mudlib` on the server, create a `lesmudos` user that owns `/opt/lesmudos/save`, install `deploy/lesmudos.service`, and open the port in the firewall. Do not use single-file publishing: the compiler needs the framework assemblies as files on disk.

## Limitations to know about

- The sandbox is a guard rail for trusted builders, not a security boundary. Do not give mudlib write access to people you would not give a shell account.
- Every player can use the wizard commands in the sample mudlib.
- Traffic is plain-text telnet.

The full list is in [DESIGN.md](docs/DESIGN.md#13-proof-of-concept-status).

## License

No license has been chosen yet. Until one is added, the code is published for reading only.
