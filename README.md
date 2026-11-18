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
Creating a new account for Keith.
Choose a password (at least 8 characters):
Type it again:
Account created.

Welcome, Keith. Type 'help' for commands.

Village Square
The center of Vincent's Hollow bustles with life. A large stone well marks the center of the square while merchants display goods from temporary stalls. Locals gather around benches exchanging news while children dart between travelers.
Obvious exits: north, west, east, south
Old Farmer Benson is here.
A traveling merchant is here.
> talk benson
Benson says: "Vincent founded this place. Brave man... but he went lookin' for somethin' beneath the earth and never returned."
> east
Ironoak Arms
The smell of hot metal and burning coal fills this small but impressive smithy. Weapon racks cover the walls while a roaring forge illuminates newly finished blades.
Obvious exits: west
Blacksmith Torren is here.
> browse
Torren has these for sale:
  A rusty sword                      10 gold
  A training spear                    8 gold
  An iron dagger                      6 gold
  A woodcutter axe                   18 gold
  A small shield                     12 gold
> buy sword
You buy a rusty sword for 10 gold.
> wield sword
You wield a rusty sword.
```

Later, in the Tavern Cellar below the Hollowman's Rest:

```
> kill rat
You attack a large rat.
A large rat attacks you!
You hit a large rat hard.
A large rat dies.
You gain 8 experience.
```

The first-time walk-through of the whole area (about 15 to 20 minutes of real fighting) is what `tests/playthrough_test.py` does.

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
| 1 | `promote`, `wizards`, `mudstat`, `goto`, `stat`, `pwd`, `cd`, `list`, `cat` (browse and read the whole mudlib), and in your own home `/wiz/<you>`: `mkdir`, `edit` (line editor), `copy`, `move`, `rename`, `delete`, `rmdir`, `update`, `clone` |
| 3 | `clone` outside your home |
| 5 | `update`, `mkdir`, `edit`, `copy`, `move`, `rename`, `delete`, `rmdir` outside your home and inside other wizards' homes (level 10 for files under `/secure` and `/std`, and for `/wiz` itself and the home directories) |
| 10 | `shutdown` |

Every wizard has a home directory `/wiz/<name>` (your name in lower case). The game creates it when you become a wizard and tells you where it is. `~` stands for it in any path (`cd ~`, `list ~/rooms`, `edit ~/rooms/hall`), and `cd` with no argument goes there. Other wizards can read your home but, below level 5, not change it. Wizards write code that runs on the server, so promote only people you trust (see [Design](docs/DESIGN.md) section 8).

### Build your own area

As any wizard, in the game:

```
> cd
/wiz/yourname
> mkdir rooms
> edit rooms/hall
```

Type `a`, paste a room (for example the one in [docs/BUILDING.md](docs/BUILDING.md)), end with a line containing only `.`, then `x`. Then:

```
> update ~/rooms/hall
> goto ~/rooms/hall
```

Make an item the same way in `~/items/`, `clone ~/items/gem` to try it, and point an exit at `/wiz/yourname/rooms/hall` from your room to link them. `delete`, `copy`, `move`, `rename` and `rmdir` work as well, inside your home, at any wizard level.

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

## The game: Vincent's Hollow

New characters now start in the Village Square of Vincent's Hollow (the start room is the `StartRoom` constant in `mudlib/std/player.cs`; `/domains/town` stays as the small technical demo). The gameplay systems are described in [docs/GAME_SYSTEMS.md](docs/GAME_SYSTEMS.md), and builders should read [docs/BUILDING.md](docs/BUILDING.md).

Player commands: `ansi`, `kill`/`attack`, `flee`, `stop`, `consider`, `wield`, `unwield`, `wear`, `remove`, `eat`/`drink`, `talk`, `ask NPC about TOPIC`, `read`, `loot`, `get THING from CORPSE`, `look in CORPSE`, `score`, `hp`, `skills`, `quests`, and in town `browse`, `buy`, `sell`, `value`, `accept`, `report`. Death costs 10% of your gold and leaves a corpse with your belongings where you fell; you wake in the Guest Rooms and must fetch it.

Combat has no rounds. Each fighter acts when its own Reaction Time and the Recovery of its last action have passed (an unarmed player strikes every 6 seconds, one with a sword every 8), and `score`, `consider` and `stat` show the numbers. Drinking a potion mid-fight is queued for your next ready moment; `stop` cancels it. See docs/COMBAT_TIMING.md.

Builder commands: `goto ROOM-OR-PLAYER` and `stat THING` (wizard level 1), `restore [TARGET]` and `speed TARGET PERCENT [SECONDS]` (5), `slay TARGET` (10), plus `update` and `clone` as before.

### Colour

Text that tells you what to type, such as the exits of a room, the commands in help and the hints from shopkeepers, is shown in bold bright cyan when your terminal understands ANSI colour, and room names in bold. Colour is on by default. `ansi` shows the setting, `ansi off` turns it off (and `ansi on` back on; `color` and `colour` work as well). The setting is saved with the character. Builders mark commands with `{cmd}...{/}` in their text; see [docs/BUILDING.md](docs/BUILDING.md#colour-and-command-highlighting). `cat` and the editor show files with their tags as written.

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
| `mudlib` | Sample mudlib: `/secure` (master, login, wizard registry), `/std` (thing, item, room, living, npc, monster, shopkeeper, weapon, armor, consumable, coins, corpse, adventurer, player, void), `/secure/lost_property` (lost-property store), `/domains/town` (technical demo), `/domains/hollow` (Vincent's Hollow) |
| `mudlib/Mudlib.csproj` | Not used at run time. Gives IntelliSense for mudlib files and lets `dotnet build` check the whole mudlib. |
| `tests/smoke_test.py` | End-to-end test of the driver over TCP (port 4811 in the examples below) |
| `tests/area_test.py` | End-to-end test of the game systems and of the Hollow's content, with test rooms and wizard help (port 4812, about 8 minutes) |
| `tests/playthrough_test.py` | A brand-new level 1 character plays the finished area from the start to the end of both quests, against an unmodified mudlib (port 4813, 10 to 25 minutes) |
| `tests/ansi_test.py` | Colour: tag rendering, `ansi on/off`, raw `cat`, escaping of player input (port 4814, about 30 seconds) |
| `tests/find_test.py` | Finds: treasure in hidden places that each character takes once, for less each time; the register, wizards, the sandbox, restarts, and the five converted places (port 4827, about 1 minute) |
| `tests/load_check.py` | Quick check that every area file compiles and loads (port 4821, under a minute) |
| `tests/timing_test.py` | Measures real intervals between combat actions against the timing formula (port 4815, about 3 minutes) |
| `tests/balance_sim.py` | Standalone combat balance simulation (event clock); `check` compares it with the mudlib numbers |
| `deploy/lesmudos.service` | Sample systemd unit (a starting point; not yet tested on a server) |
| `docs` | Design and API reference |

## Tests

```
dotnet build
LESMUDOS_TEST_PORT=4811 python3 tests/smoke_test.py
LESMUDOS_TEST_PORT=4812 python3 tests/area_test.py
LESMUDOS_TEST_PORT=4813 python3 tests/playthrough_test.py
LESMUDOS_TEST_PORT=4821 python3 tests/load_check.py
LESMUDOS_TEST_PORT=4814 python3 tests/ansi_test.py
LESMUDOS_TEST_PORT=4815 python3 tests/timing_test.py
python3 tests/balance_sim.py
```

The smoke test starts the driver on port 4777 (or `LESMUDOS_TEST_PORT`) against a temporary copy of the mudlib, logs in two players, and checks account creation and passwords, movement, object-supplied verbs, heartbeats, call-outs, wizard levels and promotion, live update of a room and of a base class, compile-error handling, the sandbox, the evaluation-cost limit, the recursion guard, unloading of old program versions, and save/restore.

## Deploying to a Linux VM

On the build machine:

```
dotnet publish src/LesMudOS.Driver -c Release -r linux-x64 --self-contained -o publish
```

Copy `publish/` to `/opt/lesmudos/bin` and `mudlib/` to `/opt/lesmudos/mudlib` on the server, create a `lesmudos` user that owns `/opt/lesmudos/save`, install `deploy/lesmudos.service`, and open the port in the firewall. Do not use single-file publishing: the compiler needs the framework assemblies as files on disk.

## Limitations to know about

- The sandbox is a guard rail for trusted builders, not a security boundary. Do not give mudlib write access to people you would not give a shell account.
- Telnet is plain text, so passwords cross the network unencrypted. A wizard's password is what stands between an attacker and `edit`, which writes code onto the server. Until there is TLS, do not let wizards log in over a network you do not trust.
- With in-game editing, the mudlib directory must be writable by the driver. The sample systemd unit keeps it read-only; add it to `ReadWritePaths` if you want `edit`, `mkdir`, `copy`, `move`, `rename`, `delete` and `rmdir` to work there. `delete` and `rmdir` cannot be undone, so keep the mudlib in version control.

The full list is in [DESIGN.md](docs/DESIGN.md#13-proof-of-concept-status).

## License

No license has been chosen yet. Until one is added, the code is published for reading only.
