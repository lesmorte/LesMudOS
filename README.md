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
| 5 | `update`, `mkdir`, `edit`, `copy`, `move`, `rename`, `delete`, `rmdir` outside your home and inside other wizards' homes (level 10 for files under `/secure`, `/std` and `/channels`, and for `/wiz` itself and the home directories) |
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

Player commands: `say`, `tell`, `reply`, `whisper`, `shout`, `ignore`, `channels` (see [Talking to others](#talking-to-others)), `ansi`, `verbose`, `prompt`, `kill`/`attack`, `flee [DIRECTION]`, `rest`/`sit` and `stand`, `stop`, `consider`, `wield`, `unwield`, `wear`, `remove`, `eat`/`drink`, `talk`, `ask NPC about TOPIC`, `read`, `loot`, `get THING from CORPSE`, `look in CORPSE`, `score`, `vitality`, `skills`, `quests`, and in town `browse`, `buy`, `sell`, `value`, `accept`, `report`; at a trainer of your calling `ranks` and `train SKILL` (skill and spell ranks: rank II in Vincent's Hollow from level 5, rank III and above at the Hall of Legends in Oakenfell; `help ranks`). Death costs 10% of your gold and leaves a corpse with your belongings where you fell; you wake in the Guest Rooms and must fetch it.

Combat has no rounds. Each fighter acts when its own Reaction Time and the Recovery of its last action have passed (an unarmed player strikes every 6 seconds, one with a sword every 8), and `score`, `consider` and `stat` show the numbers. Drinking a potion mid-fight is queued for your next ready moment; `stop` cancels it. See docs/COMBAT_TIMING.md.

Builder commands: `goto ROOM-OR-PLAYER` and `stat THING` (wizard level 1), `restore [TARGET]` and `speed TARGET PERCENT [SECONDS]` (5), `slay TARGET` (10), plus `update` and `clone` as before.

### Talking to others

| Command | |
|---|---|
| `say TEXT` (or `' TEXT`) | Everyone in the room hears it. |
| `tell PLAYER TEXT`, `reply TEXT` | A private message to a player anywhere in the world; `reply` answers the last player who told you something. |
| `whisper TEXT to NAME` (or `whisper NAME TEXT`) | Only the player you name, in the same room, hears the words; the others see that you whispered. |
| `shout TEXT` | Everyone in the world hears it (from character level 2, once every 10 seconds). `shout off` and `shout on` stop and restart hearing shouts. |
| `ignore [PLAYER]`, `unignore PLAYER` | Stop (or start again) hearing tells, whispers, shouts and channel messages from a player. Saved. |
| `channels`, `channel NAME on/off`, `NAME TEXT` | The channels you belong to. Wizards belong to `wizard`, level 10 wizards also to `sysop`; `shout wizard TEXT` and `wizard TEXT` both speak on it. |

Text is cleaned before anyone sees it (colour tags and control characters removed, 300 characters at most). Rooms can block tells (`NoTell`) and shouts (`NoShout`). Details and the security rules: [docs/GAME_SYSTEMS.md](docs/GAME_SYSTEMS.md#15-communication).

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
| `tests/comm_test.py` | Communication: say, tell, reply, whisper, shout, ignore, the room blocks, flood limits, text cleaning, channels and who may post to or hear `wizard` and `sysop`, invisible wizards, and a probe object that tries to post to `sysop` (port 4832, about 5 minutes) |
| `tests/ansi_test.py` | Colour: tag rendering, `ansi on/off`, raw `cat`, escaping of player input (port 4814, about 30 seconds) |
| `tests/find_test.py` | Finds: treasure in hidden places that each character takes once, for less each time; the register, wizards, the sandbox, restarts, and the five converted places (port 4827, about 1 minute) |
| `tests/city_test.py` | Walk-through of Oakenfell (the city and the two Vale Road rooms that remain): the map against its build document, every detail, person and shop, a new character walking from the Southgate Approach to the Hall of Callings (about 7 minutes) |
| `tests/corridor_test.py` | Walk-through of the Greenvale Transit Corridor (122 rooms): map, bands, areas, creatures per zone, hidden ways and the Chronicle, resource nodes, patrols, every detail, person and shop, and a new character walking from the Village Square to Oakenfell (port 4828, about 9 minutes; `corridor_test.py static` needs no game) |
| `tests/playfeel_test.py` | Numbered targets, inventory and list format, corpses, combat restrictions and flee, aggressive creatures engaging on arrival, rooms not refilling while the corpse lies there, shared experience, verbose, prompt, idle and timeout (port 4831, about 8 minutes) |
| `tests/aim_test.py` | The large Minor Heal and its 15-second wait (amounts at ranks I to III, the refusal and `skills` line, per caster, another Priest unaffected, gone on logout), Double Strike's 35 / 40 / 46 Endurance, the ranged opening (a creature's first action with and without a bow, once per fight, absent when the creature starts the fight), Aimed Shot (damage as the opener and mid-fight at ranks I and II, the spoiled shot, the refusals, training at Wenna Farrow and the hallmaster, an existing Scout gaining it) and a wizard-written probe. Port 4838. |
| `tests/sneak_test.py` | Openable doors (both sides, reset, flee, patrols, Tracking, the Overseer Hall door) and the Scout's Sneak: the notice chance and its edges, entry, leaving, staying, loud and quiet deeds, the opener and the surprise, ranks and training, and wizard-written probes. Port 4839. |
| `tests/rank_test.py` | Skill and spell ranks: every ranked skill's numbers at ranks I to V against the data, names by rank, the level cap, `skills`, an old save; each unlock requirement kind met, unmet, its progress and its permanence; the kill counters; `ranks` and `train` at the village trainers and the hallmasters with every refusal; each skill in play at rank I and II, Tracking's four cases, Double Strike's separate rolls and Triple and Quadruple Strike, Jack of All Trades through fixed dice; a wizard's probe that tries every new public method (port 4837, about 13 minutes) |
| `tests/rest_test.py` | The `rest` command: what ends resting and what does not, the five-times rate for Vitality, Endurance and Focus, the refusal in a fight, the surprise when a seated character is attacked, the idle timer, and probe objects that try to put a player into or out of resting (port 4835, about 4 minutes) |
| `tests/rename_test.py` | Vitality, Endurance and Focus: the old prompt tokens, a save from before the rename, content written against the old names, and a scan that no player-visible text says HP, hit points, Mana or Stamina (port 4850, about 2 minutes) |
| `tests/load_check.py` | Quick check that every area file compiles and loads (port 4821, under a minute) |
| `tests/timing_test.py` | Measures real intervals between combat actions against the timing formula (port 4815, about 3 minutes) |
| `tests/balance_sim.py` | Standalone combat balance simulation (event clock); `check` compares it with the mudlib numbers |
| `deploy/lesmudos.service` | Sample systemd unit (a starting point; not yet tested on a server) |
| `tests/gmcp_test.py` | GMCP (telnet option 201): the offer and the stream of a client without it, Room.Info with room numbers that survive a driver restart, hidden exits and doors per character, Char.Vitals and Char.Status, Comm.Channel.Text, Core.Supports and Core.Ping, malformed and oversized incoming messages, and a wizard's probe (port 4841, about 2 minutes) |
| `docs` | Design and API reference |

## Tests

```
dotnet build
LESMUDOS_TEST_PORT=4811 python3 tests/smoke_test.py
LESMUDOS_TEST_PORT=4812 python3 tests/area_test.py
LESMUDOS_TEST_PORT=4813 python3 tests/playthrough_test.py
LESMUDOS_TEST_PORT=4821 python3 tests/load_check.py
LESMUDOS_TEST_PORT=4831 python3 tests/playfeel_test.py
LESMUDOS_TEST_PORT=4850 python3 tests/rename_test.py
LESMUDOS_TEST_PORT=4835 python3 tests/rest_test.py
LESMUDOS_TEST_PORT=4837 python3 tests/rank_test.py
LESMUDOS_TEST_PORT=4838 python3 tests/aim_test.py
LESMUDOS_TEST_PORT=4814 python3 tests/ansi_test.py
LESMUDOS_TEST_PORT=4832 python3 tests/comm_test.py
LESMUDOS_TEST_PORT=4815 python3 tests/timing_test.py
LESMUDOS_TEST_PORT=4841 python3 tests/gmcp_test.py
python3 tests/balance_sim.py
python3 tests/balance_sim.py ranks [runs] [level5 heal double gap bolt focus climb aim wait aimfix healcost selfheal]
```

The test client (`Client` in `tests/smoke_test.py`, imported by the other suites) ends a reply at the game's answer to `sync TOKEN`, a second line it sends right behind every command, with a token unique to the call: reading to the prompt cannot tell the prompt that ends a reply from one that follows unprompted output (a combat line, another player's action), and on a busy machine that left every later reply one step late. `command(line, sync=False)`, or `Client.sync_default = False`, gives the raw mode that reads to the prompt; use it for checks of the prompt itself, the login sequence, and the idle timer (the sync line counts as input, so `playfeel_test` reads its idle checks raw).

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

## The website

The public website for Legends of Hollowmere (the Chronicle, the Hall of Heroes, the world and how to play) lives in [web/](web/) and reads the game's own save files, read only. How it is built, configured and deployed: [docs/WEBSITE.md](docs/WEBSITE.md).

## License

The repository holds two things under different terms. The LesMudOS engine (the driver, the standard objects, the tests and the deployment scripts) is under the MIT license: see [LICENSE](LICENSE). The game built on it, Legends of Hollowmere (its world, classes, factions and design documents), is all rights reserved: see [LICENSE-GAME.md](LICENSE-GAME.md). [LICENSING.md](LICENSING.md) lists which files are which.
