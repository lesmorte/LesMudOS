# Builder's guide

How to add rooms, creatures, shops and items to the game. Every template below is real code: `tests/area_test.py` extracts the blocks whose first line is `// File: <path>.cs`, writes them into a test mudlib under `/domains/example`, loads them in the driver and plays with them. If a template here stops compiling, that test fails.

You need wizard level 3 or more to `clone`, 1 to `goto` and `stat`, 5 to `update` and to edit files. See [GAME_SYSTEMS.md](GAME_SYSTEMS.md) for the rules and numbers behind the classes, and [DRIVER_API.md](DRIVER_API.md) for the sandbox.

## The checklist

1. **File name.** Lower-case `snake_case`, ending in `.cs`: `goblin_guard.cs`. The mudlib path is the file name without `.cs`: `/domains/hollow/mob/goblin_guard`. Use the folders of the area: `village/` and `cellars/` for rooms, `npc/` for peaceful people and shopkeepers, `mob/` for monsters, `obj/` for items.
2. **One class per file.** One public class that derives (directly or through its base) from `MudObject`. Other helper types in the file must not.
3. **Class names are unique across the whole mudlib** (the mudlib is also compiled as one project to check it). Prefix them with your area if in doubt: `HollowGoblinGuard`.
4. **The `// @inherit` line.** The first line of the file names the base file: `// @inherit /std/room`, `/std/npc`, `/std/shopkeeper`, `/std/monster`, `/std/weapon`, `/std/armor`, `/std/consumable`, `/std/coins`, `/std/item` (keys and plain things). The class then derives from the class in that file: `public class GoblinGuard : Monster`.
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

Room fields and methods: `Safe`, `LogoutRoom` (players who quit here log in here), `ResetSeconds`, `Exits`, `AddDetail`, `AddReadable`, `AddSpawn`, `AddEncounter`, `LockExit`, `BlockExit`. Override `Reset()` (and call `base.Reset()`) for anything special.

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

Set the numbers in `Create()`. `Aggressive` attacks anyone who walks in; `Assist` (default true) joins fights against other monsters. On death the killer gets `XpValue` experience at once, and a **corpse** appears holding `GoldMin`-`GoldMax` gold and each `AddLoot(path, percent)` item that was rolled in. The killer (and nobody else for half an hour) loots it with `loot`. Use `DeathMessage` to replace the standard "dies." line. Level is for the to-hit roll; `ToHit` and `Armor` are bonuses (see GAME_SYSTEMS.md for the formulas and the Hollow's creature table). A level 1 creature with 6 HP and 1-2 damage is a rat; a boss with special abilities overrides `CombatRound` (see `/domains/hollow/mob/goblin_overseer`).

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
        AddLoot("/domains/example/potion", 100);
    }
}
```

## Items

Every item has `Name`, `Ids` (words that name it), `ShortDesc` (with article, for lists), `LongDesc` (for `look at`) and optionally `Value` (gold; shopkeepers pay half; 0 means they will not buy it), `QuestItem = true` (cannot be sold) and `ReadText` (shown by `read`).

### A weapon

`wield` it. Damage replaces the unarmed 1-3; `ToHit` is added to attack rolls.

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
        DamageMin = 3;
        DamageMax = 5;
        ToHit = 0;
        Value = 10;
    }
}
```

### Armor

`wear` it. `Slot` is one of `shield`, `body`, `head`, `trinket`. `ArmorValue` adds to the wearer's armor; `MaxHpBonus` (optional) adds to maximum HP while worn.

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
        Value = 15;
    }
}
```

### A consumable

`eat`, `drink` or `quaff` it. `UseVerb` is `"eat"` or `"drink"`; `HealAmount` is restored; `UseMessage` is shown to the user. The item is used up. For anything stranger, override `OnUse(ObjectRef user)`.

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

## Corpses, in one paragraph

You do not build corpses; `Monster` and the player's death make them. For a builder it means: loot and gold are in the corpse (`loot`, `get X from corpse`, `look in corpse`), nothing falls to the floor, the killer has half an hour to themselves, an empty corpse is gone after two minutes, and a monster corpse still holding loot rots after an hour. Items you want a monster to always drop: `AddLoot(path, 100)`. Items lying on the floor of a room are done with `AddSpawn`.

## Testing in the game

Log in as a wizard (level 5 lets you do everything below except `smite`).

```
update /domains/example/hall      compile (or recompile) a room and put you back in it
goto /domains/example/hall        walk into a room by path (or goto <player>)
clone /domains/example/sword      put a new object in your inventory
stat here                         the room: safe flag, spawns and how many are alive, exits
stat rat                          a creature or item in the room: level, HP, damage, loot
stat me                           your own numbers
heal                              restore your HP (level 5); heal <creature> too
smite rat                         kill something at once with normal death (level 10)
```

A good loop: edit the file with `edit`, `update` it, `goto` the room, then try it as a player would (`ansi off` shows the plain text, `cat` shows your tags as you wrote them): `look`, `look at` every detail, `read`, `talk`, `ask ... about`, `browse`/`buy`, `kill`, `loot`. Compile errors and sandbox complaints are shown when the file loads. `update` of a room rebuilds its non-player occupants; to refresh a creature, `update` its file and then `update` the room that spawns it. Rooms re-spawn creatures at their reset time; set `ResetSeconds = 10` while testing.
