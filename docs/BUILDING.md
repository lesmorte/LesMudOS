# Builder's guide

How to add rooms, creatures, shops and items to the game. Every template below is real code: `tests/area_test.py` extracts the blocks whose first line is `// File: <path>.cs`, writes them into a test mudlib under `/domains/example`, loads them in the driver and plays with them. If a template here stops compiling, that test fails.

Every wizard may build in their own home directory `/wiz/<name>` (see "Where your files go" below) at any level: `edit`, `update`, `clone` and `goto` work there from level 1. Your home is your workshop: what you build and run there cannot touch the real game (see "Your workshop" below), and your work reaches the game when another wizard approves it ("Publishing your work"). Outside `/wiz` you need level 10 to edit or `update` anything; level 3 to `clone` in the real game, level 1 for `goto` and `stat`. See [GAME_SYSTEMS.md](GAME_SYSTEMS.md) for the rules and numbers behind the classes, [SANDBOX.md](SANDBOX.md) for the sandbox and [PUBLISHING.md](PUBLISHING.md) for the reviewer's side.

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

The mudlib project excludes `wiz/**` from `dotnet build`, so the rule that class names are unique across the whole mudlib (checklist item 3) is not checked for your files until you submit them; a compile error in them shows when you `update` them.

### Your workshop

Your home is a sandbox. Walk into one of your rooms (`goto ~/rooms/hall`, or an exit) and you are in your workshop:

```
You step into the workshop. Nothing that happens here will follow you out.
```

From that moment your character is saved as it was and not saved again until you leave. Inside, anything goes: give yourself a thousand levels, a fortune, a sword that kills in one blow, die, try your quests. `score` shows `[in the workshop: nothing here is saved]`. Leave by walking out, by `goto` to a real room, by dying, by quitting or losing your connection: you come back exactly as you walked in, with what you carried then, and are told so in one line:

```
You leave the workshop as you entered it.
```

The rules, each refused in one line when you meet it:

- Only wizards can enter. Players cannot be brought in, by an exit, by your code or by anything else.
- Nothing made in the workshop can leave it: not dropped in a real room, not given, not carried out (it is gone when you are restored). That includes real things your code clones there: a goblin from `/domains` cloned by your room is a workshop goblin, and its corpse and loot are workshop things. `stat` marks them `Sandbox object`.
- Real things you carried in cannot be set down inside ("Real things cannot be set down in the workshop; they leave with you").
- Your code reaches only the workshop: it can read anything (`Query...`, `Short`, `Long`), but it cannot change, move, hurt, heal, pay, tell or bind a verb on anyone or anything outside, and the history (Chronicle, Renown, rumours) never hears of it. Calls that would are refused silently: the call returns null.
- In the workshop `clone` works on any file at any level (`clone /domains/hollow/mob/goblin` to fight a real goblin's copy). Out in the real game you cannot clone your own home's code at all.
- Change your password outside the workshop: nothing done inside is saved.

Wizards of any level make no history anywhere: no Renown, no Chronicle entries or firsts.

### Publishing your work

When an area is ready, ask for it to be published:

```
submit ~/glade : a quiet glade east of the village      the directory goes to /domains/glade
submit ~/glade to /domains/eastglade                     or name the destination (one name below /domains)
submissions                                              your requests and what happened to them
submission 3                                             one in full, with every file's fingerprint
withdraw 3                                               take it back while it waits
```

Write the paths in your files as paths in your home (`/wiz/yourname/glade/inner`): approval rewrites every one of them to the destination. A request is refused at once if a file does not compile, names any other path under `/wiz`, uses a class name the mudlib already has, or has a name that is not lower-case letters, digits and `_` (`.cs` files only). The request keeps its own copy of the files: that copy is what the reviewer reads and what is published, so you may go on working in the directory meanwhile (to publish the newer version, submit again: a new request for the same directory replaces the one waiting). Files must hold only printable characters, tabs and line ends. Any one wizard of level 5 or higher other than you decides; you are told either way, with the reason for a rejection. Your files stay in your home: change them and submit again to publish an update, which goes to the same place. Players reach a published area once a real room has an exit to it, which a level 10 wizard adds.

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

`AddDetail` gives scenery for `look at`. `AddReadable` gives text for `read` (the optional third argument is what `look at` shows). `AddSpawn` keeps a creature or item in the room: it is cloned in when the room loads and topped up every `ResetSeconds` (default 300). A killed creature is not replaced while its corpse still lies in the room: the corpse counts as the creature until it is looted to dust, rots (at most ten minutes) or is carried out, and the next reset after that replaces it. The corpse remembers the spawn (room and line) it came from, so a corpse brought in from elsewhere, or a player's corpse, blocks nothing. `Safe = true` forbids fighting.

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

Treasure in a hidden place is a find, not coins (see "A find" under Items). Room fields and methods: `Safe`, `LogoutRoom` (no longer needed: every player who leaves a room logs in there), `ResetSeconds`, `Exits`, `AddDetail`, `AddReadable`, `AddSpawn`, `AddEncounter`, `LockExit`, `BlockExit`, `HiddenExit`, `HideExit`, `HideItem`, `HideNode`, `HideClue`, `DangerBand`, `AddNode`. Override `Reset()` (and call `base.Reset()`) for anything special.

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

### A room with a door

A door is a closed (or open) barrier on an ordinary exit that players open and close with words. `AddDoor(direction, name, closed, keyId, closesOnReset, description)` goes in `Create()`, **after** `Exits[direction]` is set (a door needs an exit to stand on). Every part but the direction has a default: `AddDoor("east")` is a closed, unlocked door named "door" that is shut again at every reset. **The room on the other side declares the matching door on its own exit back**, in the same way; the two halves are one door and share one state.

```csharp
// File: /domains/example/gatehouse.cs
// @inherit /std/room
public class ExampleGatehouse : Room
{
    public override void Create()
    {
        ShortDesc = "Gatehouse";
        LongDesc = "A cold stone gatehouse. A plank door in the east wall leads out to the courtyard.";
        Exits["east"] = "/domains/example/courtyard";
        AddDoor("east", "plank door", description: "Grey planks bound in black iron, and an iron ring for a handle.");
    }
}
```

```csharp
// File: /domains/example/courtyard.cs
// @inherit /std/room
public class ExampleCourtyard : Room
{
    public override void Create()
    {
        ShortDesc = "Courtyard";
        LongDesc = "A bare courtyard behind the gatehouse.";
        Exits["west"] = "/domains/example/gatehouse";
        AddDoor("west", "plank door", closesOnReset: false);
    }
}
```

What players get, with no more work from you:

- `open DOOR`, `close DOOR`, `unlock DOOR`, `lock DOOR`, where DOOR is the name you gave it ("door", "plank door", "gate"), a direction ("east", "e") or both ("east door"). With one door in the room the name can be left out. `look` shows the door on the exits line ("east (closed plank door)", "(open ...)", "(locked ...)"), and `look at door` describes it with your `description` and says whether it is open.
- A **closed door stops everything that uses the exit**: walking, `flee`, a patrol on its route, and Tracking (which does not read through it; at rank II a creature behind a closed door is the one "Something is close by" line). A locked door is closed too. An open door is an ordinary exit.
- Opening or closing is heard: the room on the other side is told ("The plank door to the west swings open."), and so is the room itself. **Opening a door is loud** for a sneaking Scout (see "Placing a door against sneaking" below).
- The **key**: `keyId` is the id of an item (a "key" such as the Locked exit's); a closed door with a key starts locked, and `unlock` and `lock` need the key in the character's inventory (`unlock` opens nothing: it only turns the lock). A door with no key has no lock.
- **Reset**: unless `closesOnReset: false`, the room's reset shuts the door again (and locks it, if it has a key). Either room's reset does it, for both halves, and the room that shuts it tells both rooms.

How the two halves stay in agreement: neither room keeps the door's state. The register `/secure/doors` does, under the name of the pair of rooms (so **two rooms can share one door only**), and every question a room asks about its door (open? locked?) goes there. Reloading (`update`) either room, or a reset of one, therefore cannot make the two sides disagree. The register accepts a change only from one of the two rooms of that door: no other object (a creature, an item, a wizard's tool in another room) can open, close, lock, unlock or reset anyone's door, and a room in your own home can change only the doors that have it as one end. A door declared on only one side leaves the other room's exit as an open way: declare both. `tests/lint_area.py` reports a door whose partner does not declare it.

`description` is shown by `look at door`. A door shows in a room only through `look` and the exits line: if the door is a feature of the room, give the room a detail for it as well, as for any scenery. Existing locked exits (`LockExit`) are not doors and stay as they are.

#### Placing a door against sneaking

A Scout who is sneaking can slip past creatures that do not notice, in and out of a room, and a door in the way undoes that: **opening a door is loud**. It ends the Scout's sneaking and wakes the room, so every aggressive creature in it engages at once. Use a door when the way beyond should have to be earned by facing what guards it: put it on the exit **behind** the guards (the Overseer Hall's east door has the Elite Guards in front of it), never on an exit that a creature stands behind alone. A door is not needed to stop a Scout in front of a `Boss`: a creature flagged `Boss` always notices a sneaking Scout. See "Sneaking" under Creatures for the `Alert` and `Boss` flags.

#### Sneaking: the `Alert` and `Boss` flags

A Scout who has `sneak` on rolls against every **aggressive** creature (`Aggressive = true`) of a room as they enter, and again as they leave. Two flags in a creature's `Create()` change that creature's roll:

- `Alert = true`: a sentry or guard. +30 points to its chance to notice. Set it on anything that stands watch (the Hollow's guards, the Elite Guards, the corridor's lookouts).
- `Boss = true`: always notices; it never rolls, so it cannot be sneaked past or ambushed. Set it on every boss, whatever else it is.

```csharp
// @inherit /std/monster
public class ExampleSentry : Monster
{
    public override void Create()
    {
        Name = "Gate Sentry";
        Ids.Add("sentry");
        ShortDesc = "a gate sentry";
        LongDesc = "He watches the road and not much else.";
        Level = 3;
        MaxVitality = 30;
        DamageMin = 2;
        DamageMax = 4;
        Aggressive = true;
        Alert = true;
    }
}
```

A creature that is not `Aggressive` is never rolled for. The chance is 50% - 5% for each point of the Scout's Grace above 5 + 5% for each level the creature is above the Scout, kept between 10% and 90%, + 30% if `Alert`, less the Scout's Sneak rank (0 / 10 / 20 points), and never below 5%. Where an unnoticed Scout should not slip past, put a door (above); where he should not be able to ambush, set `Boss`. Creatures written under a wizard's home directory have no standing to be rolled for and always notice, as every creature did before sneaking.

### A room with random encounters

`AddEncounter(path, percent)`: each time a player walks in there is that chance that the creature wanders in, if none from this room is still alive and a minute has passed since the last. The same rule holds for corpses: no new encounter while the corpse of this room's previous one still lies here. An encounter that is not fighting wanders off at the next reset.

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

`HideExit(direction, destination, hintDetail, difficulty, foundLine = "", landmark = false)` declares an exit that is **not listed** and cannot be walked (typing it answers "What?", as if it did not exist) until the character has found it. There are two ways to find it, and you choose them:

- **`search`** in the room: each attempt is a d20 roll plus the character's bonus (one point of Insight above 5, a Scout's Keen Eye +2, +3 for naming the spot) against `difficulty`: `SearchEasy` (7), `SearchModerate` (12), `SearchHard` (17), `SearchVeryHard` (22), or a number from 6 to 25. A natural 20 always finds it and a natural 1 never does, so the chance is between 5% and 95%; one point of bonus is 5%. A failed search starts a 60-second wait for that character in that room. Searching is an action on the combat clock and takes 3 seconds; it cannot be used while fighting, ends sneaking and resting, and an attack breaks it off. A room with nothing to find gives the same plain line as a room where the attempt failed. See "A hidden thing" below for the other kinds of hidden thing and the full rules.
- **Looking at a detail**: `hintDetail` is a comma-separated list of the names of a detail you added with `AddDetail`; `look at` any of them finds the exit for certain. Use `""` if only searching should find it (a hidden exit that should need the search leaves the hint empty), and `difficulty` 0 if only looking should.

The older `HiddenExit(direction, destination, hintDetail, findChance, foundLine = "")` still works: its percent becomes a difficulty (21 less a twentieth of the percent: 10% is 19, 20% is 17; 0 is found only by the detail), which is the same chance for an Insight 5 character who is not a Scout. New exits use `HideExit`. A secret door is `HideExit` and then `AddDoor` on the same direction (and the far room's `AddDoor` as usual): finding the exit shows the way, and the door still has to be opened.

When a character finds the exit they read `foundLine` (a standard line if you give none) and from then on it is listed among their exits and usable; the record is saved with the character at once, so it survives logout and restart. Other characters see nothing: until they find it themselves the exit is not listed for them, and when someone leaves by it they read "Name leaves somewhere." (a character who has found it reads the direction). Wizards (level 1 and up) see every hidden exit in the list marked `(hidden)`, and `stat here` lists them with their difficulty and detail; a wizard still has to find an exit before walking it. The room on the far side needs nothing special: give it an ordinary return exit. A hidden exit can be locked like any other (`LockExit`), and `flee` never uses one. `landmark: true` makes the first time a character finds it a Chronicle event (`discover.hidden_find`, Renown 2 and 10 for the first on the server); use it only for a secret way to a new place.

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

### A hidden thing

Searching finds more than exits. Every kind is declared in the room's `Create()` in one or two lines, with a difficulty from `SearchEasy` (7), `SearchModerate` (12), `SearchHard` (17) and `SearchVeryHard` (22) (or a number from 6 to 25), the words `search NAME` takes (each also an `AddDetail` key of the room, so `look at NAME` works), the line the finder reads, and, for things found by touch or smell, `byFeel: true`:

```csharp
HideItem("honey", "/domains/hollow/obj/craft/honey", SearchEasy, "sacks,sack", "Under the sacks you find a jar of honey.");  // once per reset
HideNode("herbs", "/domains/hollow/obj/cave_herbs", SearchModerate, "crack,draught", "In the crack you find pale herbs.", byFeel: true);
HideClue("tally", SearchModerate, "hooks,hook", "Behind the last hook is a slip of paper.", "slip,paper", "The text of the clue.");
```

- **`HideItem`** is for a small, cheap thing that may come back: the first character to find it takes a copy of the item, and until the room's own timed reset nobody else finds anything there ("Someone has been before you"). Nothing is recorded on the character. Use it for a material or a consumable worth a few gold at most.
- **A hidden find** is for treasure and anything that must not be farmed: an ordinary find file (see "A find") with `SearchDifficulty`, `SearchNames`, `FoundLine` and, if it is found by feel, `ByFeel = true` set in `Create()`. Until a character finds it, it is not in the room's list, and `look at` and `get` say they see no such thing; afterwards it is an ordinary find (taken once, for less each time). A character the register lists as having taken it counts as having found it.
- **`HideNode`** puts a node nobody can see, `harvest`, or gather from until they have found it; the charges and regrowth are shared. **`HideClue`** hides a scrap of writing; after finding it `read` with one of `readNames` shows the text to that character and to nobody else.
- **Hints.** Every room that hides something says so in a detail, and the detail's words are what `search NAME` takes. The room's `LongDesc` mentions the first of those words, so a player can read their way to it. The first things a new player meets say "You could {cmd}search{/} it" outright; a Hard thing says that more than one try may be needed. Looking never finds an item, node or clue; only an exit's hint detail finds it by looking.
- **Words.** `search` alone searches the room; `search THING` rolls only the hidden things whose names include THING, with +3. If THING names a find that the character can already see in the room, the find takes the word instead (`search bundle` takes a found bundle, as `get bundle` does).
- **The dark.** A room may override `protected virtual int SearchModifier(ObjectRef searcher, bool byFeel)` to make searching there harder (a negative number) or easier; it does nothing by default. docs/specs/light.md uses it.
- **Chronicle.** Only a secret way to a new place may be a Chronicle first (`landmark: true` on `HideExit`); items, clues and nodes never are. Override `ThingFound(ObjectRef who, string id)` to do something when a character finds a thing (it runs once per character and thing).
- **Lint.** `tests/lint_area.py` Check 15 reports a duplicate id, a difficulty that is not a constant or 6 to 25, a clue with no `readNames`, a far room with no way back, and names that are no `AddDetail` key of the room (errors); and a first name missing from the `LongDesc`, a hidden find over 20 gold, a once-per-reset item worth over 5 gold, and a shop in the room (warnings).
- **Where it works.** The rules of finds apply: a hidden find, node or item must be a file in a trusted tree and in a room of a trusted tree; one under `/wiz` hides and finds as a test would, is capped as found exits are, and pays nothing from the register.

The numbers and the full design are in docs/specs/search.md.

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
        MaxVitality = 12;
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

- It walks only through **real exits**: an ordinary exit in the room it is in that leads to the next room of the route. A missing exit, a locked exit it has no key for, a closed door, and a hidden exit all stop it (it stays where it is and tries again at the next step; `stat` counts the blocked steps).
- It does not walk while it is **fighting**, and it stands still for `TalkPauseSeconds` (default 20) after a player uses `talk` or `ask` on it.
- `DefendPlayers = true`: if a creature is fighting a player in the room the patrol is in (or the patrol arrives where one is), the patrol joins against the creature. A patrol is peaceful (it cannot be attacked) unless you set `Peaceful = false`; give it `Level`, `MaxVitality`, damage and, if you like, combat actions as for any creature.
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
        MaxVitality = 120;
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
        MaxVitality = 25;
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

Set the numbers in `Create()`. `Aggressive` attacks anyone who walks in; `Assist` (default true) joins fights against other monsters. On death the players who hurt it share `XpValue` experience at once (by damage, with a 10% bonus for the killing blow; a lone killer gets all of it), and a **corpse** appears holding `GoldMin`-`GoldMax` gold and each `AddLoot(path, percent)` item that was rolled in. Those players (and nobody else for half an hour) loot it with `loot`. Use `DeathMessage` to replace the standard "dies." line. Level is for the to-hit roll; `ToHit` and `Armor` are bonuses (see GAME_SYSTEMS.md for the formulas and the Hollow's creature table).

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
        MaxVitality = 5;
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

### Tags: what a creature is and how it hits

Every creature declares `Tags.Add("goblin"); Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("mace");` after its short description: a nature (`goblin`, `beast`, `human`, `undead`...), the reach and damage type of its basic attack, and a kind (a weapon kind, or a natural one: `bite`, `claw`, `gore`, `kick`, `slam`, `lash`, `touch`, `spit`, `thorns`). The flags `Boss`, `Elite`, `Alert` and `DarkSight` become the tags `boss`, `elite`, `alert` and `darksight` by themselves; read them with `HasTag`. An action that differs says so: `AddCombatAction("bash", "Shield Bash", ..., reach: "melee", damage: "blunt", tagKind: "slam")`. Words of a blow come from the kind and the damage type, so a creature that the text arms with a spear needs a piercing action. `tests/lint_area.py` checks the tags (check 17) and the name words (check 18).

### Timing: a creature with one action, and one with a pattern

Combat has no rounds: every creature acts on the event clock of [COMBAT_TIMING.md](COMBAT_TIMING.md), exactly as a player does. A creature has a **base Reaction Time** (`BaseReaction`, 1.0 fast to 6.0 slow, steps of 0.5; default 2.5, the same as a player) and a list of **actions**. Each action has a `preparation` (a wind-up before it takes effect, 0 for a plain attack) and a `recovery`, both in time units of 2 seconds, and the time to its next action is `(Reaction Time + recovery) x 2` seconds. A rat with Reaction Time 2.0 and a bite with recovery 0.5 acts every `(2.0 + 0.5) x 2 = 5` seconds. The rest of the numbers (Vitality, damage, to hit, armor) work as before; because actions are several seconds apart, damage and Vitality are larger than they were when a round was 2 seconds (the table in GAME_SYSTEMS.md has the Hollow's).

- **A creature with one action** needs one line of data and no code: `AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);`. A creature that declares no action at all uses the plain attack of its weapon, or its fists (recovery 0.5).
- **A creature with several actions** declares them all and overrides `ChooseAction`, which is called whenever the creature is ready and has an enemy. `ActionsTaken()` counts the actions chosen so far in this fight (0 while choosing the first), so a fixed rhythm that players can learn is a one-liner. `windUp` is the verb phrase the room reads when the preparation begins ("The ogre rears back..."): always give a prepared action one, because it is how a player sees the blow coming. `damageMultiplier` scales an attack. Other kinds of action (a summons, a shout) use `kind:` with your own word and are done in an override of `Special(CombatAction action, ObjectRef? target)`; see `/domains/hollow/mob/goblin_overseer`.
- The fight rules for everybody: the one who starts a fight acts at once; everyone drawn in (the one attacked, monsters that assist) waits their own Reaction Time x 2 seconds before the first action; an aggressive creature starts the fight as soon as a player's arrival is complete (the room is described first, then "attacks you!"; the player cannot just walk on, only `flee`), and then waits that long before its first action; an action aimed at someone who has died or left is wasted.

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
        MaxVitality = 40;
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

**Tags.** A weapon declares what it is with `Tags.Add(...)`, one per family: a reach (`melee`, `ranged`, `thrown`), a damage type (`slashing`, `piercing`, `blunt`), a kind (the category word: `sword`, `axe`, `mace` and so on) and a material (`iron`, `wood` and so on). If you add no kind tag the `Category` is the kind. A kind implies a reach and a damage type, so a plain sword is melee and slashing without saying so; add the others only to differ (a thrown spear). The words of a blow follow the damage type, and `appraise`, `examine` and the shops show "a sword (slashing, melee)". A misspelt or unknown tag is dropped by the registry without an error (`stat` shows what was dropped; the lint makes it an error). A description and an attack must agree: a creature whose text carries a hammer has a hammer blow (`mace`, `blunt`), not a spear jab (the lint warns when a description names a weapon that none of its attacks use).

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

`wear` it. `Slot` is one of `shield`, `body`, `head`, `trinket`. `ArmorValue` adds to the wearer's armor; `MaxVitalityBonus` (optional) adds to maximum Vitality while worn. `ReactionMod` (optional, default 0) is added to the wearer's Reaction Time while worn: a shield or heavy armor makes its wearer a little slower. `Category` is `light`, `medium` or `heavy` for body and head armor (it decides who may wear it; a shield or trinket needs none). `WornBy` and `NotWornBy` restrict a piece by calling as well (next section).

Armour tags: the `Category` is the armour tag (`light`, `medium`, `heavy`; shown in words as "light cloth armour" with the material) and `Tags.Add("cloth")` or `"leather"`, `"iron"` and so on gives the material.

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
        MaxVitalityBonus = 0;
        ReactionMod = 0.0;
        Value = 15;
    }
}
```

### Restricting a piece by calling

Armour and weapons are restricted by weight or kind (`Category`) alone unless the file also names callings. Both fields are lists of calling ids (`"warrior"`, `"scout"`, `"priest"`, `"mage"`, `"adventurer"`), set in `Create()`, and both work on armour **and** on weapons:

```csharp
WornBy.Add("mage");       // only these callings may wear (or wield) it ...
WornBy.Add("priest");
NotWornBy.Add("mage");    // ... and these may not (use one or the other; if both are set, both rules apply)
```

With neither, the category decides, as always. An advanced class, subclass or prestige class counts as the calling it grew from: a Mage who has become a Mage-advanced class is still a Mage here. What the game does with the rule:

- `wear` and `wield` refuse in words that say who the piece is for ("You cannot wear a leather jerkin. Mages cannot wear it." / "Only Mages and Priests can wear it."). The calling rule is checked first, then the weight category.
- `look at` and `appraise` add the same sentence, so the description needs no words of its own about it (the Leather Jerkin and the Scholar's Robe keep theirs for flavour).
- A shop's `browse` appends the restriction to the line: `A scholar's robe    40 gold  (Mages and Priests only)`, `A leather jerkin   50 gold  (not for Mages)`.
- `buy` warns before it takes any gold when the buyer's calling bars the piece, and sells it only if the buyer says the same `buy` again within two minutes ("No gold has been taken. Say buy ... again if you want it anyway.").
- A piece worn when a rule arrives (a character saved before the rule) stays worn at login until it is taken off; it cannot be put back on.
- Nothing outside the character puts a piece on it: `Equip` is not a public method, so another object (a wizard's, say) cannot dress a player in a forbidden piece.

Applied in the Hollow: the Leather Jerkin (`NotWornBy.Add("mage")`) and the Scholar's Robe (`WornBy.Add("mage")`, `WornBy.Add("priest")`).

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
| `TakenShort`, `TakenLong` | what a character who has already taken it sees when they look at it | the short description and "(taken by you)"; a plain line |
| `TakenHere`, `EmptyHere` | the room's listing of it, a whole sentence, for a character who has taken it or found it empty ("A long oilcloth wrapping lies folded open here.") | "The short description lies here; you have taken what it held." / "... lies here, picked clean." |
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

## Light

Every room has a light number: its `Light` (default 1, "lit") plus the light of every lit source in it (a torch carried in, a wall lantern, a Glow on someone there), clamped to -3..3. **1 or more is lit, 0 is dim, -1 or less is dark.** A lit room is as it ever was. Design and numbers: [specs/light.md](specs/light.md); measured balance: [BALANCE_LIGHT.md](BALANCE_LIGHT.md).

- **A dim room**: `Light = LightDim;` (0). Players fight at -1 to hit (-2 with a bow), notice you 10 points less, and harvest and craft a little worse. The room says "It is dim here."
- **A dark room**: `Light = LightDark;` (-1), and **give it `DarkDesc`** (what a player who cannot see feels or hears: a sentence about water, cold stone, the smell; no exits, no creatures, no items). In the dark a player sees no exits except the ways they know (the way they came in, and any they have used from the room), no creatures (they are "Someone" and "Something"), no items, cannot get, loot, read, craft, harvest, skin or shop, fights at -3 (-6 with a bow) against a creature that sees in the dark, and can still `kill something`. `LightBlack` (-2) exists but is for content that does not exist yet; `tests/lint_area.py` check 16 allows `Light` from -3 to 1 and an ERROR for a dark room with no `DarkDesc`, and warns about a shop in a room below 1 and a wall light in a `Wet` room.
- **Lighting a dark room for good**: spawn a wall light: `AddSpawn("/domains/hollow/obj/wall_lantern")` (+1; a table lamp is the same). A wall light is furniture (`/std/wall_light`, cannot be taken) and lights its room as long as it stands. A base 0 room with a wall lantern is lit; a base -1 room with one is dim.
- **`Wet = true;`** puts out any light carried in ("The torch hisses out in the water."), a lantern too. Use it for water you can wade in.
- **A creature that cannot see in the dark**: `DarkSight = false;` in its `Create` (every `/std/monster` has `DarkSight = true`, so only the rare cave toad suffers the penalties). It fights, and notices a sneaking Scout, as a player does.
- **A light item**: inherit `/std/light_source` and set `LightValue` (1 torch, 2 lantern), `Capacity` (seconds of burning), `WarnAt` (seconds left when it says so), `Noun`, and `Lantern = true` if it can be refilled from a flask of lamp oil (`/domains/hollow/obj/lamp_oil`, 30 minutes). A light is never saved lit: it is out when the player returns. `light THING` is a quick action; `extinguish` and `put out` too; water puts torches out.
- **Never call** `/secure/light` from a wizard's object: `AddSource`, `NoteGlow` and `SetTestBurnScale` check their caller. A wizard's own lamp lights only its own workshop, never a real room, and a source's value is clamped to +3.
- **Search** will use `Room.LightSearchModifier(searcher, byFeel)` (dim -2, dark -5; searching by feel costs nothing): see the NOTES in the search spec; the hook is not connected yet.
- Prompt token `{light}` and `score` show the room's light to the player; GMCP `Room.Info` carries `light`.

## Crafting

The full reference, with every number and the launch recipes as data, is [CRAFTING.md](CRAFTING.md). The builder's part in brief:

- **A material** is an item file (inherit `/std/material`: names, value, nothing else) and one line in a table under `/crafting`: `Material("honey", "/domains/hollow/obj/craft/honey", "sweetener")` (id, file, categories). Without the line it is not a material.
- **A creature with a skin or blood** declares `public string Skin { get; set; } = "wolf";` and `public string Blood { get; set; } = "wolf";`, and its kind has a line in `/crafting/beasts.cs`: `Beast("wolf", "Wolf", 6, 4, 4)` (kind, name, skin value, blood value, difficulty). Skins and bloods are the generic `/std/skin` and `/std/blood`; no file per creature.
- **A tool**: inherit `/std/tool`, set `ToolType` ("skinning knife", "bleeding kit", "harvest sickle"); its quality is the item quality.
- **A station**: inherit `/std/station`, set `StationKind` ("herbalist table", "tanning rack", "workbench", "cooking fire"), place it with `AddSpawn`.
- **A node's yield**: `Yield("berry bush", "blackberries", 1, 2)` in a table (node kind, material, weight, units).
- **A recipe**: a chain in a `/recipes` file (inherit `/std/recipe_set`): `Recipe("id", "Name").Needs("healing_herb").NeedsAny("berry", 2).At("herbalist table").Skill("herbalism", "novice").Makes(path or material id).Discovery("known")`. At most three kinds of ingredient.
- **A crafted consumable**: inherit `/std/crafted`, set `UseVerb` ("eat", "drink", "apply") and any of `RestoreVitality`, `RestoreEndurance`, `HealPerTick` + `Ticks` + `TickSeconds`. Quality scales them.
- **A trainer**: inherit `/std/trainer`, `AddLesson("recipe_id", price)`; it is also a shopkeeper, so `AddStock` lines make the same NPC a supplier. **A book**: inherit `/std/recipe_book`, `Teaches.Add("recipe_id")`. **A quest reward**: `LoadObject("/secure/crafting").Call<string>("Teach", player, "recipe_id")`.
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

**A skill, spell or trait** is one file in `/skills`, found by the registry without any list to edit. It inherits `/std/skill` (a `Command` word, `EnduranceCost` or `FocusCost`, `Preparation`, `Recovery`, `Target` of `enemy`, `friend` or `item`, and `Resolve(user, target)`) or `/std/trait` (`Mods["armor"] = 1;` and the other modifier keys listed in CLASS_SYSTEM.md section 12). Items grant skills by overriding `QueryGrantedSkills()`.

**A skill's ranks** (docs/specs/skill-ranks.md; CLASS_SYSTEM.md section 13). Every skill with a command word has ranks without any work: rank r needs level 5 x (r - 1), and a rank you do not describe adds 30% of the rank I value to each effect and 15% of the rank I cost to each cost. Write a table only where you want other numbers, in the skill's own `Create()`:

```
// Path: /skills/arcane_bolt.cs (the real one, shortened)
        FocusCost = 20;                              // rank I's cost (key "focus"; "endurance" for EnduranceCost)
        Effect("min", 6);                            // rank I numbers of the skill's own, by name
        Effect("max", 9);
        RankRow(2, "min=8;max=12;focus=23");         // rank II; a key left out takes the default step
        RankRow(3, "min=10;max=14;focus=26");        // rank IV and on: the default step from rank III
```

Read every number through the helper, never with your own arithmetic: `RankInt("min", RankOf(user))` in `Resolve` (the engine already reads `focus`, `endurance`, `tohit` (ToHitBonus), `crit` (CritChance), `factor` (DamageFactor) and `hits` (Hits, the separate blows of an attack skill) at the user's rank). Override `QuerySummaryAt(rank)` so `skills` shows the rank's numbers. `FixedKeys.Add("seconds")` keeps a number (a duration, a count) the same at every rank unless a row names it; `hits` is fixed already. `RankRow(3, "hits=3;factor=0.6", "Triple Strike")` renames the skill from that rank on. `MaxRank = 1` makes a skill rank I only; `MaxRank = 2` stops it at II. `python3 tests/balance_sim.py check` compares the fighting skills' tables with the simulator's `SKILL_RANKS`, so change both.

Three more things a skill's data can say (docs/CLASS_SYSTEM.md section 14):
- **A wait between uses:** `Effect("wait", 15)` (name the number once, as Minor Heal's `HealWait`). The wait is on the creature that used the skill, not on the target, is not saved, and is started by the engine when the action begins; a use during it is refused before anything is paid, with the seconds left, and `skills` shows it. `wait` is a fixed key (a rank does not change it).
- **A bigger blow as the action that starts the fight:** `Effect("opener", 2.5)` names the damage factor (rows may name `opener=` too). It applies only when the character's own command started the fight with that very action (`UseSkill(skill, target, opening: true)` from the player's `CmdSkill`); Aimed Shot (`aim`) is the one skill that has it.
- **A preparation that a blow spoils:** set `Spoilable = true`. Being hit while the action is in its Preparation cancels it and gives nothing back (Stagger gives half back); half the Recovery is still paid. Only the creature that struck the blow can cause it (`Living.SpoilPreparation(attacker)` checks `PreviousObject()`).
- A skill may demand a bow (`WeaponCategories.Add("bow")`) and refuse in `Check` (return the text; nothing is spent and no fight begins).

**A trainer of ranks** inherits `/std/trainer` (and may sell and teach recipes as well). Its players get `ranks` and `train SKILL`; every check and the change to the character are the rank daemon's (`/secure/ranks`), and the trainer must live in a trusted tree (`/domains`, not `/wiz`).

```
// Path: /domains/hollow/npc/halbert_quill.cs (the rank lines)
        TeachCalling("mage", 2, 2);                                  // rank II of every ranked Mage skill, on level and the default price
        AddRankOffer("arcane_bolt", 2, 40, "kills goblin 10");       // this offer on its own terms: price and requirement lines
        AddRankLine("kills goblin 10", "Ten goblins, and then we will talk about the second working.");
        ElsewhereLine = "The third rank and beyond are taught in Oakenfell, at the Hall of Legends.";
```

Requirement lines, all of which must hold (the level line is added from the cap rule): `quest QUEST-ID` (completed), `kills KIND N` (creatures of a kind slain: the beast-table kind, `Skin` or `Blood`, else `ChronicleKind`, else the file name), `kills_with SKILL-ID N` (killing blows struck with that skill), `renown N`, `standing FACTION N` (always unmet until factions exist), `item /path/of/item` (shown) or `item /path/of/item take` (handed over). Default prices are `RankDaemon.RankPrices` (II 40, III 150, IV 400, V 900); a price of -1 in `AddRankOffer` uses them. Several trainers may share a room: each answers only the callings it teaches.

**A class** is one file in `/classes` inheriting `/std/pclass`: tier, parent, `Require("level", "5")`, `ExpModifier`, `AttackRate`, `Skills`, `Traits`, `Weapons`, `ArmorTypes`, `Enabled`. Advanced classes are given with `player.Call<string>("ApplyClass", id)`. CLASS_SYSTEM.md section 12 has the full story.

## Vitality, Endurance and Focus in content

The three resources are called Vitality, Endurance and Focus (they were HP, Stamina and Mana), in every message and in the code. Write new content with the new names:

| Use | Old name (still works, obsolete) |
|---|---|
| `MaxVitality`, `QueryVitality()`, `QueryMaxVitality()` | `MaxHp`, `Hp`, `QueryHp()`, `QueryMaxHp()` |
| `MaxEndurance`, `EnduranceRegen`, `QueryEndurance()`, `QueryMaxEndurance()` | `MaxStamina`, `StaminaRegen`, `QueryStamina()`, `QueryMaxStamina()` |
| `MaxFocus`, `FocusRegen`, `QueryFocus()`, `QueryMaxFocus()` | `MaxMana`, `ManaRegen`, `QueryMana()`, `QueryMaxMana()` |
| skill `EnduranceCost`, `FocusCost` | `StaminaCost`, `ManaCost` |
| armour `MaxVitalityBonus` | `MaxHpBonus` |
| crafted item `RestoreVitality`, `RestoreEndurance` | `RestoreStamina` |

The old names are thin forwarding aliases kept so that content written before the rename still loads; they will go when nothing uses them. In `prompt`, the tokens are `{vitality} {maxvitality} {endurance} {maxendurance} {focus} {maxfocus}`; the old tokens `{hp} {maxhp} {stamina} {maxstamina} {mana} {maxmana}` still expand. The keys `hp`, `stamina` and `mana` in a character's save file are unchanged.

## Corpses, in one paragraph

(Crafting adds: a corpse remembers the creature's kind, level, skin and blood; each can be taken once, by `skin` and `bleed`, under the same 30-minute lock, and a corpse with a part still to take does not crumble when it is empty. See CRAFTING.md.)

You do not build corpses; `Monster` and the player's death make them. For a builder it means: loot and gold are in the corpse (`loot`, `get X from corpse`, `look in corpse`), nothing falls to the floor, the players who hurt the creature have half an hour to themselves, a corpse is gone at once when its last item is taken (one that was empty from the start, after two minutes), and a creature's corpse rots after ten minutes whatever it still holds (a skin included; give the creature a public `int CorpseSeconds` to change that, up to an hour), while a player's keeps its hour. Items you want a monster to always drop: `AddLoot(path, 100)`. Items lying on the floor of a room are done with `AddSpawn`.

## Testing in the game

Log in as a wizard. In your own home every command below works from level 1; on the real game's files `update` needs level 10, and `restore` level 5.

```
update ~/rooms/hall               compile (or recompile) a room and put you back in it
goto ~/rooms/hall                 walk into a room by path (or goto <player>); a room of yours is your workshop
clone /domains/example/sword      put a new object in your inventory (in the workshop: a workshop copy)
stat here                         the room: safe flag, spawns and how many are alive, exits, hidden exits, danger band
stat rat                          a creature or item in the room: level, Vitality, damage, loot
stat me                           your own numbers
restore                           restore your Vitality, Endurance and Focus (level 5); restore <creature> too
slay rat                          kill something at once with normal death (level 10)
```

A good loop: edit the file with `edit`, `update` it, `goto` the room, then try it as a player would (`ansi off` shows the plain text, `cat` shows your tags as you wrote them): `look`, `look at` every detail, `read`, `talk`, `ask ... about`, `browse`/`buy`, `kill`, `loot`. Compile errors and sandbox complaints are shown when the file loads. `update` of a room rebuilds its non-player occupants; to refresh a creature, `update` its file and then `update` the room that spawns it. Rooms re-spawn creatures at their reset time; set `ResetSeconds = 10` while testing.
