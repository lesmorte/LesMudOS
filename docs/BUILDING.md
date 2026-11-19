# Builder's guide

How to add rooms, creatures, shops and items to the game. Every template below is real code: `tests/area_test.py` extracts the blocks whose first line is `// File: <path>.cs`, writes them into a test mudlib under `/domains/example`, loads them in the driver and plays with them. If a template here stops compiling, that test fails.

Every wizard may build in their own home directory `/wiz/<name>` (see "Where your files go" below) at any level: `edit`, `update`, `clone` and `goto` work there from level 1. Outside your home you need level 3 to `clone`, 1 to `goto` and `stat`, 5 to `update` and to edit files. See [GAME_SYSTEMS.md](GAME_SYSTEMS.md) for the rules and numbers behind the classes, and [DRIVER_API.md](DRIVER_API.md) for the sandbox.

## Where your files go

Every wizard has a home directory, `/wiz/<yourname>` (your name in lower case), made for you when you became a wizard. In every command, `~` at the start of a path means your home, and `cd` with no argument goes there:

```
cd                       go home: /wiz/yourname
mkdir ~/rooms            make directories for your rooms and items
mkdir ~/obj
edit ~/rooms/hall        create or change a file (the line editor; "h" for help)
update ~/rooms/hall      compile it; goto ~/rooms/hall walks in
clone ~/obj/gem          make an item to try
copy ~/rooms/hall ~/rooms/hall2     also: move, rename, delete (asks first), rmdir
```

You control everything under your home at any wizard level. Other wizards can read it, and wizards of level 5 or more can change it. The home directory itself and `/wiz` can be removed or renamed only by a level-10 wizard. An exit in one of your rooms names a path like any other: `Exits["north"] = "/wiz/yourname/rooms/hall";`.

The mudlib project excludes `wiz/**` from `dotnet build`, so the rule that class names are unique across the whole mudlib (checklist item 3) is not checked for your files; a compile error in them shows when you `update` them.

## The checklist

1. **File name.** Lower-case `snake_case`, ending in `.cs`: `goblin_guard.cs`. The mudlib path is the file name without `.cs`: `/domains/hollow/mob/goblin_guard`. Use the folders of the area: `village/` and `cellars/` for rooms, `npc/` for peaceful people and shopkeepers, `mob/` for monsters, `obj/` for items.
2. **One class per file.** One public class that derives (directly or through its base) from `MudObject`. Other helper types in the file must not.
3. **Class names are unique across the whole mudlib** (the mudlib is also compiled as one project to check it). Prefix them with your area if in doubt: `HollowGoblinGuard`.
4. **The `// @inherit` line.** The first line of the file names the base file: `// @inherit /std/room`, `/std/npc`, `/std/patrol`, `/std/node`, `/std/find` (hidden treasure), `/std/shopkeeper`, `/std/monster`, `/std/weapon`, `/std/armor`, `/std/consumable`, `/std/coins`, `/std/item` (keys and plain things). The class then derives from the class in that file: `public class GoblinGuard : Monster`.
5. **Set everything in `Create()`.** No constructors with arguments. Names of rooms, fields and helpers are all in the templates.
6. **Sandbox limits.** Mudlib code may use `System`, `System.Collections.Generic`, `System.Linq`, `System.Text` and the mudlib's own types. It cannot use files, threads, reflection, `typeof`, `GetType()`, `async`, `goto`, `lock`, `Console`, `Environment`. An error shows up when the file loads. A runaway loop is stopped by the evaluation-cost limit.
7. **Exits name rooms by path**, without `.cs`: `Exits["north"] = "/domains/hollow/village/gate";`. Any single word works as a direction (`up`, `down`, `west`); one-letter abbreviations are added automatically. If the destination file does not exist yet, players are told the way is not open yet.
8. **Say things the way the setting does.** Long descriptions are plain text apart from the colour tags described in the next section; give a creature a `Name` for speech and `ShortDesc` with its article ("a large rat") for room lists and fights.
9. **Tag the commands.** Wherever your text tells the player something to type, wrap it in `{cmd}` ... `{/}`. See the next section.
10. **Test it in the game.** See the last section.

## Colour and command highlighting

Text that reaches a player can carry tags in braces. Write them in ordinary strings. The player's body turns them into ANSI colour when the player has colour on (the default) and removes them when it is off, so you never need to care which.

| Tag | Meaning | Rendered as |
|---|---|---|
| `{cmd}` ... `{/}` | something the player can type | bold bright cyan (`ESC[1;96m`) |
| `{title}` ... `{/}` | a room name | bold (`ESC[1m`) |
| `{b}`, `{u}` | bold, underline | `ESC[1m`, `ESC[4m` |
| `{red}` `{green}` `{yellow}` `{blue}` `{magenta}` `{cyan}` `{white}` | colours | `ESC[31m` to `ESC[37m` |
| `{bred}` `{bgreen}` ... `{bwhite}` | bright colours | `ESC[91m` to `ESC[97m` |
| `{/}` | end every style | `ESC[0m` |
| `{{` | a literal `{` | `{` |

Anything else in braces, such as `{0}` or `{foo}`, is left exactly as written. A style that is still open at the end of a line is closed there automatically, so a forgotten `{/}` cannot colour the rest of the screen. Styles can be stacked (`{b}{red}Danger{/}`); `{/}` resets all of them.

**The rule: tag what the player can literally type.** Room exits and the words of the standard messages are done for you. In your own text, tag the exact word or phrase a player would type: `{cmd}browse{/}`, `{cmd}accept cellar{/}`, `{cmd}read{/}` in "You could {cmd}read{/} it". Do not tag ordinary words that merely resemble a command ("a long read"), words typed with a different spelling ("Browse" at the start of a sentence is not what the player types), or placeholders ("ask NPC about TOPIC": tag `ask` and `about`, not `NPC`).

**Never put tags in `Name`, `Ids` or `ShortDesc`.** Those are used for matching and for lists (`Present`, the merged "Two giant rats are here.", shop columns, corpse names, combat messages), and other objects read them as plain strings. Tags belong in text that is only shown: `LongDesc`, details, readables, NPC chat, topics and emotes, refusals, `Write` and `Say` calls. A non-player object (an NPC, a monster) that is told something with `CatchTell` receives the text with the tags as written, neither rendered nor removed.

Two more rules for code:

- Text a player typed must have `{` doubled before it goes into game text, or they could colour their own speech and everyone else's screen. In a class derived from `Thing` call `Esc(text)`: `Write($"You see no '{Esc(args)}' here.\n");`. The standard commands already do this for `say`, `title`, and for every message that repeats an argument.
- `cat` and the editor's `p` show files exactly as stored, so you see your own tags when you edit a room. Everything else is rendered.

```
LongDesc = "A notice hangs by the door. You could {cmd}read{/} it.";
AddChat("Buy, {cmd}sell{/} or just {cmd}browse{/}.");
AddTopic("bed", "Five gold a night. Just {cmd}buy bed{/}.");
```

The first player-facing line of a room, its name, is tagged by `Room` itself; do not tag `ShortDesc`.

## Rooms

### A plain room: details, readables, spawns

`AddDetail` gives scenery for `look at`. `AddReadable` gives text for `read` (the optional third argument is what `look at` shows). `AddSpawn` keeps a creature or item in the room: it is cloned in when the room loads and topped up every `ResetSeconds` (default 300). `Safe = true` forbids fighting.

```csharp
// File: /domains/example/hall.cs
// @inherit /std/room
public class ExampleHall : Room
{
    public override void Create()
    {
        ShortDesc = "Example Hall";
        LongDesc = "A square hall with a worn flagstone floor. A notice hangs by the door.";
        Exits["east"] = "/domains/example/vault_door";
        Exits["south"] = "/domains/example/yard";
        Exits["north"] = "/domains/example/trail";
        Safe = false;
        ResetSeconds = 120;

        AddDetail("floor,flagstones,flagstone", "The flagstones are chipped and swept clean.");
        AddReadable("notice,sign", "The notice reads:\n\"Beware of rats.\"", "A hand-lettered notice. You could {cmd}read{/} it.");

        AddSpawn("/domains/example/rat", 2);
        AddSpawn("/domains/example/potion");
    }
}
```

Treasure in a hidden place is a find, not coins (see "A find" under Items). Room fields and methods: `Safe`, `LogoutRoom` (players who quit here log in here), `ResetSeconds`, `Exits`, `AddDetail`, `AddReadable`, `AddSpawn`, `AddEncounter`, `LockExit`, `BlockExit`, `HiddenExit`, `DangerBand`, `AddNode`. Override `Reset()` (and call `base.Reset()`) for anything special.

### A room with a locked exit

`LockExit(direction, keyId, refusal)`: only someone carrying an object that answers to `keyId` can use the exit. The key is not used up. The exit is still listed.

```csharp
// File: /domains/example/vault_door.cs
// @inherit /std/room
public class ExampleVaultDoor : Room
{
    public override void Create()
    {
        ShortDesc = "Vault Door";
        LongDesc = "A short passage ends at a heavy iron door.";
        Exits["west"] = "/domains/example/hall";
        Exits["east"] = "/domains/example/vault";
        LockExit("east", "key", "The iron door is locked.");
        AddSpawn("/domains/example/key");
    }
}
```

```csharp
// File: /domains/example/vault.cs
// @inherit /std/room
public class ExampleVault : Room
{
    public override void Create()
    {
        ShortDesc = "Vault";
        LongDesc = "A small stone vault, bare except for dust.";
        Exits["west"] = "/domains/example/vault_door";
    }
}
```

`BlockExit(direction, message)` is the same idea for a way nobody can use yet (a road into the wilderness): the direction is not listed among the obvious exits, and typing it shows the message.

### A room with random encounters

`AddEncounter(path, percent)`: each time a player walks in there is that chance that the creature wanders in, if none from this room is still alive and a minute has passed since the last. An encounter that is not fighting wanders off at the next reset.

```csharp
// File: /domains/example/trail.cs
// @inherit /std/room
public class ExampleTrail : Room
{
    public override void Create()
    {
        ShortDesc = "Example Trail";
        LongDesc = "A muddy trail winds between dripping trees.";
        Exits["south"] = "/domains/example/hall";
        ResetSeconds = 60;
        AddEncounter("/domains/example/rat", 50);
    }
}
```

### A room with a hidden exit

`HiddenExit(direction, destination, hintDetail, findChance, foundLine = "")` declares an exit that is **not listed** and cannot be walked (typing it answers "What?", as if it did not exist) until the character has found it. There are two ways to find it, and you choose them:

- **`search`** in the room: each attempt finds the exit with `findChance` percent (0 to 100; 100 always finds it, 0 never does). Searching is an action on the combat clock and takes about 3 seconds; it cannot be used while fighting. A room with nothing to find gives the same plain line as a room where the attempt failed.
- **Looking at a detail**: `hintDetail` is a comma-separated list of the names of a detail you added with `AddDetail`; `look at` any of them finds the exit for certain. Use `""` if only searching should find it, and `findChance` 0 if only looking should.

When a character finds the exit they read `foundLine` (a standard line if you give none) and from then on it is listed among their exits and usable; the record is saved with the character at once, so it survives logout and restart. Other characters see nothing: until they find it themselves the exit is not listed for them, and when someone leaves by it they read "Name leaves somewhere." (a character who has found it reads the direction). Wizards (level 1 and up) see every hidden exit in the list marked `(hidden)`, and `stat here` lists them with their chance and detail; a wizard still has to find an exit before walking it. The room on the far side needs nothing special: give it an ordinary return exit. A hidden exit can be locked like any other (`LockExit`), and `flee` never uses one.

To be told when a character first finds an exit, override `ExitFound(ObjectRef who, string direction)` in the room. It runs once for each character and exit, at the moment of discovery, and does nothing by default (the Chronicle's discovery events will use it). It is `protected`: only the room runs it.

```csharp
// File: /domains/example/secret_study.cs
// @inherit /std/room
public class ExampleSecretStudy : Room
{
    public override void Create()
    {
        ShortDesc = "Example Study";
        LongDesc = "A quiet study lined with dusty shelves, one tall bookcase standing slightly out of true.";
        Exits["east"] = "/domains/example/hall";
        AddDetail("bookcase,shelves,shelf", "The bookcase is not quite flush with the wall, and the dust on the floor in front of it is scuffed in an arc.");
        HiddenExit("west", "/domains/example/hidden_cellar", "bookcase,shelves,shelf", 25,
            "You lean on the bookcase and it swings aside on a hidden hinge. A way leads {cmd}west{/}.");
    }

    protected override void ExitFound(ObjectRef who, string direction)
    {
        // Optional: report the discovery (a quest step, the Chronicle). Runs once per character.
    }
}
```

```csharp
// File: /domains/example/hidden_cellar.cs
// @inherit /std/room
public class ExampleHiddenCellar : Room
{
    public override void Create()
    {
        ShortDesc = "Example Hidden Cellar";
        LongDesc = "A cramped cellar behind the bookcase, smelling of old paper.";
        Exits["east"] = "/domains/example/secret_study";
    }
}
```

### A room with a level band

`DangerBand(low, high)` says the room is ground for characters of level `low` to `high`. A character who walks (or flees) from a room that is not dangerous to them into one whose `low` is two or more above their level reads one line, "this ground is more dangerous than you are ready for", once for that entry; walking on within dangerous ground does not repeat it. Nothing else changes: `look`, `consider` and `score` show no number, and wizards read the band in `stat here`. To give a whole directory a band, put it in the `area.cs` beside the rooms (`BandLow = 6; BandHigh = 8;`, next to `AreaName`); a room that calls `DangerBand` keeps its own.

```csharp
// File: /domains/example/watch_post.cs
// @inherit /std/room
public class ExampleWatchPost : Room
{
    public override void Create()
    {
        ShortDesc = "Example Watch Post";
        LongDesc = "A ruined watch post, its roof gone, at the edge of the wild country.";
        Exits["south"] = "/domains/example/trail";
        DangerBand(6, 8);
    }
}
```

```
// Path: /domains/example/area.cs   (every room in the directory takes this band unless it sets its own)
// @inherit /std/area
public class ExampleArea : Area
{
    public override void Create() { AreaName = "The Example"; RegionName = "Greenvale"; BandLow = 6; BandHigh = 8; }
}
```

### A room with a resource node

`AddNode(path)` (or `AddNode(path, count)`) places a resource node in the room, as `AddSpawn` places a creature: it is cloned in when the room loads and put back at reset if it is gone. A node does not need anything else from the room; it regrows by itself. See "Resource nodes" below for the node file.

```csharp
// File: /domains/example/berry_patch.cs
// @inherit /std/room
public class ExampleBerryPatch : Room
{
    public override void Create()
    {
        ShortDesc = "Example Berry Patch";
        LongDesc = "A sunny bank above the trail where brambles have taken over.";
        Exits["south"] = "/domains/example/trail";
        AddNode("/domains/example/berry_bush");
    }
}
```

## People

### A peaceful NPC: chat, topics, emotes

An `Npc` cannot be attacked. `AddChat` lines are given in turn by `talk <npc>` (and wrap round); the first is its main line. `AddTopic(keywords, line)` answers `ask <npc> about <keyword>` for any of the comma-separated keywords. `AddEmote(line, percent)` is an idle action shown while a player is present (percent chance per two-second heartbeat). Give it `Ids` so players can type short names. `AddChatAfterQuest(questId, line)` replaces the main line for a player who has completed that quest (Harland thanks the player this way).

```csharp
// File: /domains/example/villager.cs
// @inherit /std/npc
public class ExampleVillager : Npc
{
    public override void Create()
    {
        Name = "Maren";
        Ids.Add("villager");
        Ids.Add("woman");
        ShortDesc = "Maren the weaver";
        LongDesc = "A cheerful woman with ink-stained fingers and a shuttle in her belt.";
        Level = 1;
        MaxHp = 12;
        AddChat("Good day to you.");
        AddChat("The wool this year is the best I have seen.");
        AddTopic("wool,weaving", "Sheep, mostly. Then a great deal of patience.");
        AddTopic("rats", "They get into everything.");
        AddEmote("Maren hums as her shuttle flies back and forth.", 10);
    }
}
```

### A patrol

`/std/patrol` is an `Npc` that walks. `SetRoute(rooms, paceSeconds, loop)` gives it an ordered list of room paths and the seconds between steps; with `loop` false it walks the route there and back, with `loop` true the last room must lead on to the first. Put it in the first room of its route with `AddSpawn`. The people in the rooms read an arrival line and a departure line (`ArriveLine` with `{name}` and `{from}`, `LeaveLine` with `{name}` and `{dir}`; the defaults read "A ranger arrives from the south." and "A ranger leaves north."). The rules:

- It walks only through **real exits**: an ordinary exit in the room it is in that leads to the next room of the route. A missing exit, a locked exit it has no key for, and a hidden exit all stop it (it stays where it is and tries again at the next step; `stat` counts the blocked steps).
- It does not walk while it is **fighting**, and it stands still for `TalkPauseSeconds` (default 20) after a player uses `talk` or `ask` on it.
- `DefendPlayers = true`: if a creature is fighting a player in the room the patrol is in (or the patrol arrives where one is), the patrol joins against the creature. A patrol is peaceful (it cannot be attacked) unless you set `Peaceful = false`; give it `Level`, `MaxHp`, damage and, if you like, combat actions as for any creature.
- If it **dies** it leaves the world and comes back after `ReturnSeconds` (default 300) in the first room of its route, at full health. It leaves no corpse.
- **Cost.** A patrol with no player in any room of its route has no walking call-out and no heartbeat. All it does is look every `PollSeconds` (default 15) whether any player stands in one of its route's rooms, which is a comparison of room names for each player on the game, and it loads no room. The moment a player is on the route it walks, and when the last one leaves it stops again where it is. Patrols are therefore free in the quiet parts of a long road.
- A patrol defined under `/wiz` walks only in its own wizard's home, and no other patrol enters `/wiz`.

```csharp
// File: /domains/example/ranger.cs
// @inherit /std/patrol
public class ExampleRanger : Patrol
{
    public override void Create()
    {
        Name = "Ranger Hale";
        Ids.Add("ranger");
        Ids.Add("hale");
        ShortDesc = "Ranger Hale of the Riverwatch";
        LongDesc = "A weathered ranger in a green cloak, a longbow across her back, who walks the road as if she owned every stone of it.";
        Level = 5;
        MaxHp = 120;
        DamageMin = 4;
        DamageMax = 7;
        DefendPlayers = true;
        ReturnSeconds = 600;
        SetRoute(new[] { "/domains/example/hall", "/domains/example/trail" }, 30);
        ArriveLine = "{name} strides in from {from}, eyes on the treeline.";
        AddChat("Keep to the road after dusk.");
        AddTopic("road", "Quiet, today. That is when I worry.");
    }
}
```

To place it, give the first room `AddSpawn("/domains/example/ranger");`.

### A shopkeeper

`AddStock(path, price)` is always on sale; `AddRareStock(path, price, percent)` is rolled when the shopkeeper is created and again at each reset of its room. Players use `browse`, `buy`, `sell` (pays half the item's `Value`; refuses items worth nothing and quest items) and `value`. Shopkeepers are `Npc`s, so they can also chat. Put one in a room with `AddSpawn`.

```csharp
// File: /domains/example/shopkeeper.cs
// @inherit /std/shopkeeper
public class ExampleShopkeeper : Shopkeeper
{
    public override void Create()
    {
        Name = "Orrin";
        Ids.Add("shopkeeper");
        Ids.Add("trader");
        ShortDesc = "Orrin the trader";
        LongDesc = "A round man behind a counter, who weighs everything twice.";
        Level = 2;
        MaxHp = 25;
        AddChat("Everything is priced fairly. Mostly.");
        AddChat("Try {cmd}browse{/} to see what I have.");
        AddStock("/domains/example/sword", 10);
        AddStock("/domains/example/potion", 15);
        AddRareStock("/domains/example/helm", 40, 30);
    }
}
```

## Creatures

### A monster with loot

Set the numbers in `Create()`. `Aggressive` attacks anyone who walks in; `Assist` (default true) joins fights against other monsters. On death the killer gets `XpValue` experience at once, and a **corpse** appears holding `GoldMin`-`GoldMax` gold and each `AddLoot(path, percent)` item that was rolled in. The killer (and nobody else for half an hour) loots it with `loot`. Use `DeathMessage` to replace the standard "dies." line. Level is for the to-hit roll; `ToHit` and `Armor` are bonuses (see GAME_SYSTEMS.md for the formulas and the Hollow's creature table).

```csharp
// File: /domains/example/rat.cs
// @inherit /std/monster
public class ExampleRat : Monster
{
    public override void Create()
    {
        Name = "Example Rat";
        Ids.Add("rat");
        ShortDesc = "an example rat";
        LongDesc = "A rat that exists to be tested.";
        Level = 1;
        MaxHp = 5;
        DamageMin = 1;
        DamageMax = 2;
        ToHit = 0;
        Armor = 0;
        XpValue = 8;
        GoldMin = 1;
        GoldMax = 3;
        Aggressive = true;
        BaseReaction = 2.0;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);
        AddLoot("/domains/example/potion", 100);
    }
}
```

### Timing: a creature with one action, and one with a pattern

Combat has no rounds: every creature acts on the event clock of [COMBAT_TIMING.md](COMBAT_TIMING.md), exactly as a player does. A creature has a **base Reaction Time** (`BaseReaction`, 1.0 fast to 6.0 slow, steps of 0.5; default 2.5, the same as a player) and a list of **actions**. Each action has a `preparation` (a wind-up before it takes effect, 0 for a plain attack) and a `recovery`, both in time units of 2 seconds, and the time to its next action is `(Reaction Time + recovery) x 2` seconds. A rat with Reaction Time 2.0 and a bite with recovery 0.5 acts every `(2.0 + 0.5) x 2 = 5` seconds. The rest of the numbers (HP, damage, to hit, armor) work as before; because actions are several seconds apart, damage and HP are larger than they were when a round was 2 seconds (the table in GAME_SYSTEMS.md has the Hollow's).

- **A creature with one action** needs one line of data and no code: `AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);`. A creature that declares no action at all uses the plain attack of its weapon, or its fists (recovery 0.5).
- **A creature with several actions** declares them all and overrides `ChooseAction`, which is called whenever the creature is ready and has an enemy. `ActionsTaken()` counts the actions chosen so far in this fight (0 while choosing the first), so a fixed rhythm that players can learn is a one-liner. `windUp` is the verb phrase the room reads when the preparation begins ("The ogre rears back..."): always give a prepared action one, because it is how a player sees the blow coming. `damageMultiplier` scales an attack. Other kinds of action (a summons, a shout) use `kind:` with your own word and are done in an override of `Special(CombatAction action, ObjectRef? target)`; see `/domains/hollow/mob/goblin_overseer`.
- The fight rules for everybody: the one who starts a fight acts at once; everyone drawn in (the one attacked, monsters that assist) waits their own Reaction Time x 2 seconds before the first action; an aggressive creature waits that long after a player arrives; an action aimed at someone who has died or left is wasted.

```csharp
// File: /domains/example/drake.cs
// @inherit /std/monster
public class ExampleDrake : Monster
{
    public override void Create()
    {
        Name = "Example Drake";
        Ids.Add("drake");
        ShortDesc = "an example drake";
        LongDesc = "A small drake that exists to be timed.";
        Level = 3;
        MaxHp = 40;
        DamageMin = 3;
        DamageMax = 5;
        XpValue = 30;
        Aggressive = true;
        BaseReaction = 4.0;
        AddCombatAction("claw", "Claw Swipe", preparation: 0, recovery: 1.0);
        AddCombatAction("breath", "Fire Breath", preparation: 1.0, recovery: 3.0, windUp: "rears back and swells its chest", damageMultiplier: 2);
    }

    /// <summary>Claw Swipe, Claw Swipe, Fire Breath, and round again.</summary>
    protected override CombatAction ChooseAction(ObjectRef target) => FindAction(ActionsTaken() % 3 == 2 ? "breath" : "claw")!;
}
```

The drake claws every `(4.0 + 1.0) x 2 = 10` seconds, and every third action the room reads "An example drake rears back and swells its chest..." two seconds before the breath lands, which then costs it `(4.0 + 3.0) x 2 = 14` seconds in all. `stat drake` lists both actions with their delays.

### Resource nodes

`/std/node` is a thing in a room that cannot be picked up and holds **charges**. It knows its `Kind` (free text: "berry bush", "herb patch"), its `Difficulty` (an integer you choose; the crafting system decides what it means), its `Charges` (the most it holds), and its `RegrowSeconds`: each charge that is taken comes back that long afterwards, for everyone (a node picked clean by one character is bare for the next until it regrows). `FullDesc` is what `look` shows while it has charges, `PickedDesc` while it has none, and `PickedShort` replaces its line in the room's list while it is bare. The node answers to its `Name`, its `Ids` and the words of its `Kind`.

Other code gathers with `TryGather(ObjectRef who)`, which takes one charge and returns true, or returns false if the node is bare or `who` is not alive and in the room with it, and reads `QueryKind()`, `QueryCharges()`, `QueryMaxCharges()`, `QueryDifficulty()` and `QueryRegrowSeconds()`. The node does not build the `harvest` command, tools, skills or what a charge yields; the crafting engine does that on top.

```csharp
// File: /domains/example/berry_bush.cs
// @inherit /std/node
public class ExampleBerryBush : Node
{
    public override void Create()
    {
        Name = "Berry Bush";
        Ids.Add("bush");
        ShortDesc = "a berry bush";
        PickedShort = "a picked-over berry bush";
        Kind = "berry bush";
        Charges = 3;
        RegrowSeconds = 600;
        Difficulty = 2;
        FullDesc = "A thorny bush heavy with dark red berries.";
        PickedDesc = "A thorny bush, picked clean. A few shrivelled berries cling to the lowest twigs.";
    }
}
```

## Items

Every item has `Name`, `Ids` (words that name it), `ShortDesc` (with article, for lists), `LongDesc` (for `look at`) and optionally `Value` (gold for one Common item; shopkeepers pay half; 0 means they will not buy it), `QuestItem = true` (cannot be sold or given away) and `ReadText` (shown by `read`). Quality and stacks are below, under "Quality and stacks".

### A weapon

`wield` it. Damage replaces the unarmed 2-5; `ToHit` is added to attack rolls. Two timing fields: `ReactionMod` is added to the wielder's Reaction Time for as long as it is wielded (heavy weapons are slow to bring to bear), and `Recovery` is the Recovery of a basic attack with it. A player with Reaction Time 2.5 attacks every `(2.5 + ReactionMod + Recovery) x 2` seconds: 6 with fists, 9 with this sword (`0.5` and `1.0`). A slower weapon must hit harder so that its damage per second is no worse (see the weapon table in GAME_SYSTEMS.md). Set `Category` to one of `dagger`, `sword`, `spear`, `axe`, `mace`, `staff`, `bow`: a character may only wield categories their calling allows (`Magic = "the blade is enchanted: +2 to hit"` is a note shown by Appraise and Magical Lore).

```csharp
// File: /domains/example/sword.cs
// @inherit /std/weapon
public class ExampleSword : Weapon
{
    public override void Create()
    {
        Name = "Example Sword";
        Ids.Add("sword");
        ShortDesc = "an example sword";
        LongDesc = "A plain blade, balanced for testing.";
        DamageMin = 9;
        DamageMax = 14;
        ToHit = 0;
        ReactionMod = 0.5;
        Recovery = 1.0;
        Value = 10;
    }
}
```

### Armor

`wear` it. `Slot` is one of `shield`, `body`, `head`, `trinket`. `ArmorValue` adds to the wearer's armor; `MaxHpBonus` (optional) adds to maximum HP while worn. `ReactionMod` (optional, default 0) is added to the wearer's Reaction Time while worn: a shield or heavy armor makes its wearer a little slower. `Category` is `light`, `medium` or `heavy` for body and head armor (it decides who may wear it; a shield or trinket needs none).

```csharp
// File: /domains/example/helm.cs
// @inherit /std/armor
public class ExampleHelm : Armor
{
    public override void Create()
    {
        Name = "Example Helm";
        Ids.Add("helm");
        Ids.Add("helmet");
        ShortDesc = "an example helm";
        LongDesc = "A dented iron cap.";
        Slot = "head";
        ArmorValue = 1;
        MaxHpBonus = 0;
        ReactionMod = 0.0;
        Value = 15;
    }
}
```

### A consumable

`eat`, `drink` or `quaff` it. `UseVerb` is `"eat"` or `"drink"`; `HealAmount` is restored; `UseMessage` is shown to the user. The item is used up. Using it is a combat action like any other: its `Recovery` (default 0.5 time units) is part of the time before the user can act again, and a user who is still recovering has it queued for the next Ready Time (`stop` cancels). For anything stranger, override `OnUse(ObjectRef user)`.

```csharp
// File: /domains/example/potion.cs
// @inherit /std/consumable
public class ExamplePotion : Consumable
{
    public override void Create()
    {
        Name = "Example Potion";
        Ids.Add("potion");
        ShortDesc = "an example potion";
        LongDesc = "A vial of something red.";
        HealAmount = 20;
        UseVerb = "drink";
        UseMessage = "You drink the example potion. It tastes of cherries.";
        Recovery = 0.5;
        Value = 15;
    }
}
```

### Coins

Picking up (or looting) coins adds the gold to the purse and the item disappears. Set `Amount`, or `AmountMin` and `AmountMax` for a random pile. Put them in a room with `AddSpawn`, or in a monster's `AddLoot`.

```csharp
// File: /domains/example/purse.cs
// @inherit /std/coins
public class ExamplePurse : Coins
{
    public override void Create()
    {
        Name = "Example Purse";
        Ids.Add("purse");
        ShortDesc = "a purse of coins";
        LongDesc = "A small purse that clinks.";
        AmountMin = 20;
        AmountMax = 40;
    }
}
```

### A find: treasure in a hidden place

**The rule: each character can take a find once and never again, and each new finder gets less than the one before, until nothing is left; it never refills.**

Use a find, not coins, for any treasure that sits in a secret place: coins placed with `AddSpawn` come back at every reset, so anyone can farm them. A find lies in the room and cannot be picked up; a character takes what it holds with the ordinary words (`get purse`, `take purse`, `loot purse`). Put it in a room with `AddSpawn`: the find object comes back if it is lost, but the treasure never does, because who has taken it is kept in the register `/secure/finds`, not in the room or the object.

Fields, all set in `Create()`:

| Field | Meaning | Default |
|---|---|---|
| `Gold` | gold for the first finder; 0 with an `ItemPath` makes a keepsake (below) | 0 |
| `FallOff` | percent of the one before's amount that each next finder gets, rounded down (0 to 99) | 60 (100, 60, 36, 21, 12, 7, 4, 2, 1) |
| `MinimumGold` | when the next finder's amount would be below this, the find is empty for everyone after | 1 |
| `ItemPath` | a file of an item that each finder receives, one each (a cloned item into the inventory; at their feet if they cannot carry it) | none |
| `ItemFinders` | how many of the first finders receive the item; 0 means every finder while gold remains (every character, for a keepsake) | 0 |
| `TakenShort`, `TakenLong` | what a character who has already taken it sees | the short description and "(taken by you)"; a plain line |
| `EmptyShort`, `EmptyLong` | what a character who has not taken it sees, when nothing is left | the short description and "(picked clean)"; a plain line |
| `GoldLine`, `ItemLine` | what the finder is told; `%gold%` and `%item%` stand for the amount and the item | "You take %gold% gold from the (name)." and "You also take %item%." |
| `TakenLine`, `EmptyLine` | what a character is told who tries again, or who comes after it is empty | one plain line each |

The words `{` and `}` are for colour tags, so the two placeholders are written between percent signs.

**Descriptions follow the viewer.** `look` and the room's list call `Short()` and `Long()` with the looking character as `ThisPlayer()`, and the find asks the register what that character may take: a character who has not taken it, and for whom something is left, reads `ShortDesc` and `LongDesc`; one who has taken it reads the `Taken` pair; one who has not but would find it empty reads the `Empty` pair. So the description never promises coins that are gone. Write all three so that `Short()` reads in the room's list ("A mouldering purse is here.") and does not promise anything the other two states deny. Wizards see the full description while something is left, and take nothing.

**One file is one stash.** The register keys a find by the program path of its file (`/domains/example/old_purse`), taken from the object itself, so a find cannot name another's record, and two stashes in one room are two files. Renaming or moving the file makes a new stash that nobody has taken from; move a find only before it goes live. Changing `Gold`, `FallOff` or `MinimumGold` later changes what the later finders get (the register keeps who, not how much), never who has already taken it.

**Where it works.** The find must be a file in one of the trusted trees (`/std`, `/secure`, `/domains`, `/classes`, `/skills`, `/events`: the list of the event service), and its room too. A find under `/wiz` is refused: it pays nothing and records nothing. Wizards of any level may look but take nothing and are not recorded ("wizards make no history").

```csharp
// File: /domains/example/purse_room.cs
// @inherit /std/room
public class ExamplePurseRoom : Room
{
    public override void Create()
    {
        ShortDesc = "Example Purse Room";
        LongDesc = "A cramped cellar behind a loose stone, bare but for the scraps of an old camp. Someone hid a purse here.";
        Exits["north"] = "/domains/example/hall";
        AddDetail("camp,scraps,stone", "A few charred sticks and a loose stone, pushed aside.");
        AddSpawn("/domains/example/old_purse");
    }
}
```

```csharp
// File: /domains/example/old_purse.cs
// @inherit /std/find
public class ExampleOldPurse : Find
{
    public override void Create()
    {
        Name = "Old Purse";
        Ids.Add("purse");
        Ids.Add("old purse");
        ShortDesc = "a mouldering example purse";
        LongDesc = "A small leather purse gone green with age. The coins inside are still good.";
        TakenShort = "a mouldering example purse, emptied by you";
        TakenLong = "The purse you emptied lies slack on the floor. There is nothing more in it for you.";
        EmptyShort = "a mouldering example purse, slack and flat";
        EmptyLong = "A small leather purse, flat and open. Someone has been here before you, and nothing is left.";
        GoldLine = "You tip %gold% gold out of the purse into your hand.";
        Gold = 20;          // the first finder: 20, then 12, 7, 4, 2, 1, and then nothing for anyone
        FallOff = 60;       // the default
        MinimumGold = 1;    // the default
    }
}
```

An item along with the gold (the Cold Larder's chest gives sixty gold, less each time, and a healing potion to each of the first five finders):

```
Gold = 60;
ItemPath = "/domains/hollow/obj/healing_potion";
ItemFinders = 5;
```

A **keepsake** has no gold and no end: set `ItemPath` and leave `Gold` at 0. Every character may take one, once, however many come (the grave token in the Barrow's Tomb of the Seven Bowls, worth 15 gold, is one). `ItemFinders` can still limit it to the first few.

Behind the scenes (you do not call it): the find asks `/secure/finds` `Claim(character, Gold, FallOff, MinimumGold, hasItem, ItemFinders)`. The register checks the caller, works out the amount, records the character and saves its file, and only then answers with the gold, the item and a code; the find then pays and saves the character. See "Finds" in GAME_SYSTEMS.md for the order, and for what a crash can and cannot do.

### A key

A key is a plain item. A locked exit asks for any carried object that answers to the key id, so give the key that id (here `"key"`; use something more specific such as `"brass key"` when a level has several).

```csharp
// File: /domains/example/key.cs
// @inherit /std/item
public class ExampleKey : Item
{
    public override void Create()
    {
        Name = "Example Key";
        Ids.Add("key");
        Ids.Add("iron key");
        ShortDesc = "an iron key";
        LongDesc = "A heavy key with a square bow.";
    }
}
```

### Quality and stacks

Every item has a **quality** and a **quantity**, kept with it wherever it is saved (a character's inventory, the lost-property store). Quality is a number from 0 to 5, `Item.Poor`, `Common` (the default), `Fine`, `Excellent`, `Masterwork`, `Legendary`; players see it as a word in front of the name ("a fine wolf skin", nothing for Common) and can type it (`drop fine skin`). An item's worth is `Value` times the quality's multiplier, rounded and at least 1: Poor 0.5, Common 1, Fine 1.5, Excellent 2.5, Masterwork 4, Legendary 8. The table is `Item.QualityMultipliers` and `Item.ValueFor(value, quality)`; nothing else should hold those numbers. Shops pay half of that per item.

A **material** is stackable: set `Stackable = true`. A stack is one object with a quantity: "3 wolf skins". Two stacks merge when they come to rest in the same place if they are of the same file and quality (and have the same properties, below), and never otherwise. Commands split them: `get 3 skins`, `get all skins`, `drop 2 fine wolf skin`, `give 2 skins to Ann`, `buy 5 skins`, `sell 3 skins`. The plural is the short description without its article plus "s" (or "es"); set `Plural` when that is wrong. Every name in `Ids` also answers in the plural with an "s" or "es". Eating or drinking a stackable consumable uses one. Items that are not stackable behave as they always have.

```csharp
// File: /domains/example/pelt.cs
// @inherit /std/item
public class ExamplePelt : Item
{
    public override void Create()
    {
        Name = "example pelt";
        Ids.Add("pelt");
        ShortDesc = "an example pelt";
        LongDesc = "A soft grey pelt.";
        Value = 4;
        Stackable = true;
        // Plural = "example pelts";  (only when the default is wrong)
    }
}
```

**Working with quality and quantity in code.** Every object that derives from `/std/thing` (items, NPCs, creatures, corpses, and later stations and resource nodes; not rooms, which derive from `MudObject` directly) has these helpers, as protected functions:

| Helper | Does |
|---|---|
| `CreateIn(path, count, quality, container)` | Make count items at a quality in a container in one call: one stack for a stackable file (merged with a matching stack there), otherwise count objects (at most 100). Returns the object holding them, or null if refused. |
| `TakeFrom(container, count, ob => test)` | Destroy count items that pass the test, splitting the last stack. All or nothing: false, and nothing taken, if there are fewer. Give a fourth argument to move them to another container instead. |
| `CountIn(container, path)` | How many of a file a container holds, by quality: a `Dictionary<int, int>` from quality to count. |
| `MovePortion(item, n, destination)` | Move n of a stack (all of it for n at or above its quantity) and merge them there. |
| `QuantityOf(ob)`, `Describe(ob, n)` | A stack's quantity (1 for anything else); how n of it read ("2 fine wolf skins"). |

On an item, from other code: `QueryQuality()`, `QueryQualityName()`, `QueryQuantity()`, `QueryStackable()`, `QueryValue()` (one item, quality included), `QueryBaseValue()`, `QueryTotalValue()`, `SetQuality(q)`, `SetQuantity(n)`, `Split(n)` (returns the part split off, put beside it), `Settle()` (merge with a matching stack nearby; call it after moving a stackable item with `MoveObject` yourself), and named text properties for later systems, `QueryProperty(key)` and `SetProperty(key, value)` (at most 16, 256 characters each; stacks with different properties do not merge). The setters return false if refused: code in a wizard's home (`/wiz/...`) may change only items that are themselves under `/wiz`.

```csharp
// In an NPC, an item or a station: hand a player three Fine wolf skins, then take two skins of any quality back.
CreateIn("/domains/test/wolf_skin", 3, Item.Fine, player);
var tookThem = TakeFrom(player, 2, ob => ob.ProgramPath == "/domains/test/wolf_skin");
var fine = CountIn(player, "/domains/test/wolf_skin").GetValueOrDefault(Item.Fine);
```

A file that uses `Item.Fine` and the other names needs `// @use /std/item` (an item file already has it through `@inherit`). A shopkeeper can sell a quality with `AddStock(path, price, quality)`; the price shown is the listed price times the multiplier.

## Crafting

The full reference, with every number and the launch recipes as data, is [CRAFTING.md](CRAFTING.md). The builder's part in brief:

- **A material** is an item file (inherit `/std/material`: names, value, nothing else) and one line in a table under `/crafting`: `Material("honey", "/domains/hollow/obj/craft/honey", "sweetener")` (id, file, categories). Without the line it is not a material.
- **A creature with a skin or blood** declares `public string Skin { get; set; } = "wolf";` and `public string Blood { get; set; } = "wolf";`, and its kind has a line in `/crafting/beasts.cs`: `Beast("wolf", "Wolf", 6, 4, 4)` (kind, name, skin value, blood value, difficulty). Skins and bloods are the generic `/std/skin` and `/std/blood`; no file per creature.
- **A tool**: inherit `/std/tool`, set `ToolType` ("skinning knife", "bleeding kit", "harvest sickle"); its quality is the item quality.
- **A station**: inherit `/std/station`, set `StationKind` ("herbalist table", "tanning rack", "workbench", "cooking fire"), place it with `AddSpawn`.
- **A node's yield**: `Yield("berry bush", "blackberries", 1, 2)` in a table (node kind, material, weight, units).
- **A recipe**: a chain in a `/recipes` file (inherit `/std/recipe_set`): `Recipe("id", "Name").Needs("healing_herb").NeedsAny("berry", 2).At("herbalist table").Skill("herbalism", "novice").Makes(path or material id).Discovery("known")`. At most three kinds of ingredient.
- **A crafted consumable**: inherit `/std/crafted`, set `UseVerb` ("eat", "drink", "apply") and any of `RestoreVitality`, `RestoreStamina`, `HealPerTick` + `Ticks` + `TickSeconds`. Quality scales them.
- **A trainer**: inherit `/std/trainer`, `AddLesson("recipe_id", price)`. **A book**: inherit `/std/recipe_book`, `Teaches.Add("recipe_id")`. **A quest reward**: `LoadObject("/secure/crafting").Call<string>("Teach", player, "recipe_id")`.
- **A vendor of materials**: `AddStock(path, price)`; generic skins and bloods by record: `AddStock("/std/skin?p.kind=rabbit", 3)`. Never below the material's value. Goblin Blood cannot be stocked.
- **Pricing**: an output's value times its count at most about 60% of its ingredients' values. Run `python3 tests/craft_sim.py`.
- Every class name in the mudlib must be unique. After editing a data or recipe file, `update` it; the daemon notices within five seconds and writes what it loaded, and any problem or clash, to the driver log.

## Areas, regions, skills and classes

**Area and region names.** Put an `area.cs` in the directory of your rooms. Every room in that directory then belongs to the area it names, and `score` shows "Area ...   Region ...". Nothing is repeated in the rooms.

```
// Path: /domains/example/area.cs
// @inherit /std/area
public class ExampleArea : Area
{
    public override void Create() { AreaName = "The Example"; RegionName = "Greenvale"; }
}
```

**A locked exit that opens with an item.** Override `QueryExitPath(string direction)` in the room (see `/domains/hollow/village/north_road.cs`, which closes the road north without the travel pass).

**A skill, spell or trait** is one file in `/skills`, found by the registry without any list to edit. It inherits `/std/skill` (a `Command` word, `StaminaCost` or `ManaCost`, `Preparation`, `Recovery`, `Target` of `enemy`, `friend` or `item`, and `Resolve(user, target)`) or `/std/trait` (`Mods["armor"] = 1;` and the other modifier keys listed in CLASS_SYSTEM.md section 12). Items grant skills by overriding `QueryGrantedSkills()`.

**A class** is one file in `/classes` inheriting `/std/pclass`: tier, parent, `Require("level", "5")`, `ExpModifier`, `AttackRate`, `Skills`, `Traits`, `Weapons`, `ArmorTypes`, `Enabled`. Advanced classes are given with `player.Call<string>("ApplyClass", id)`. CLASS_SYSTEM.md section 12 has the full story.

## Corpses, in one paragraph

(Crafting adds: a corpse remembers the creature's kind, level, skin and blood; each can be taken once, by `skin` and `bleed`, under the same 30-minute lock, and a corpse with a part still to take does not crumble when it is empty. See CRAFTING.md.)

You do not build corpses; `Monster` and the player's death make them. For a builder it means: loot and gold are in the corpse (`loot`, `get X from corpse`, `look in corpse`), nothing falls to the floor, the killer has half an hour to themselves, an empty corpse is gone after two minutes, and a monster corpse still holding loot rots after an hour. Items you want a monster to always drop: `AddLoot(path, 100)`. Items lying on the floor of a room are done with `AddSpawn`.

## Testing in the game

Log in as a wizard (level 5 lets you do everything below except `slay`).

```
update /domains/example/hall      compile (or recompile) a room and put you back in it
goto /domains/example/hall        walk into a room by path (or goto <player>)
clone /domains/example/sword      put a new object in your inventory
stat here                         the room: safe flag, spawns and how many are alive, exits, hidden exits, danger band
stat rat                          a creature or item in the room: level, HP, damage, loot
stat me                           your own numbers
restore                           restore your HP, Stamina and Mana (level 5); restore <creature> too
slay rat                          kill something at once with normal death (level 10)
```

A good loop: edit the file with `edit`, `update` it, `goto` the room, then try it as a player would (`ansi off` shows the plain text, `cat` shows your tags as you wrote them): `look`, `look at` every detail, `read`, `talk`, `ask ... about`, `browse`/`buy`, `kill`, `loot`. Compile errors and sandbox complaints are shown when the file loads. `update` of a room rebuilds its non-player occupants; to refresh a creature, `update` its file and then `update` the room that spawns it. Rooms re-spawn creatures at their reset time; set `ResetSeconds = 10` while testing.
