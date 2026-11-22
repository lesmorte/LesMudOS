# Game systems design

The sample mudlib so far has rooms, items and wizard tools, but no game. This document specifies the gameplay layer needed for the first real area, [Vincent's Hollow](areas/vincents-hollow.md): living things, combat, equipment, NPC dialogue, shops, quests, spawning and encounters.

Everything here is mudlib code under `/std`. Driver changes are small and generic, and recorded in DRIVER_API.md (colour support added one more: `NotifyFail` texts now arrive through `CatchTell`, see section 14): a creature that calls `EnableCommands` in its constructor no longer changes `ThisPlayer()` for the object that cloned it.

Status: **implemented and played through.** Sections 2 to 10 and the corpses are built, and the whole of Vincent's Hollow (eight village rooms, ten dungeon rooms, every creature, item and NPC) exists and has been played from level 1 to the end of both quests by `tests/playthrough_test.py` (section 13). Where the implementation differs from the first design, this document has been corrected to describe what the code does. The list of differences is at the end of section 11.

## 1. Principles

- **Classic and small.** Automatic melee on an event clock (Reaction Time and Recovery, no rounds), levels 1 to 5, a handful of numbers per creature. No classes, skills, magic or parties yet.
- **Data in `Create()`.** An area builder writes a room, creature or item by setting fields in `Create()`, with no logic for the common cases. Logic is only needed for special behaviour (a boss ability, a quest giver).
- **Late-bound calls between objects.** Objects talk through `ObjectRef.Call`, as elsewhere in this mudlib, so everything survives `update`.
- **Game state is not a security boundary.** Any mudlib object can call `AddGold` on a player. That is the LPmud norm and is accepted here: builders are trusted. Wizard levels, passwords and files remain guarded as before and must not be weakened.

## 2. File layout

```
mudlib/std/
  thing.cs        name, ids, short, long, Stat()
  item.cs         portable thing; Value, QuestItem, ReadText, OnGet hook, Vanish()
  weapon.cs      : Item with damage
  armor.cs       : Item with armor value and slot
  consumable.cs  : Item that is eaten or drunk for an effect
  coins.cs       : Item that turns into gold when picked up
  living.cs      : abstract Thing with Vitality, combat, equipment
  npc.cs         : Living with dialogue; peaceful by default
  monster.cs     : Npc that fights, gives experience and drops loot
  shopkeeper.cs  : Npc that sells from a stock list and buys items back
  adventurer.cs  : abstract Living with the player's gameplay commands and saved character
  corpse.cs       what a dead creature or player leaves: a locked container with decay
  player.cs       now inherits adventurer; keeps login, wizard and file commands
  room.cs         details, readable text, spawns, encounters, locked exits, hidden exits, danger band, safe flag, reset
  patrol.cs      : Npc that walks a route of rooms (section 3a)
  node.cs        : Thing that holds gatherable charges and regrows (section 3a)
  find.cs        : Thing, treasure in a hidden place that each character takes once, for less each time (Finds, section 9)
mudlib/secure/
  lost_property.cs  the lost-property store (section 9, Corpses)
  finds.cs          the register of finds: who has taken each hidden stash (section 9, Finds)
  comm.cs           say, tell, reply, whisper, shout and the cleaning of player text (section 15)
  channels.cs       the channel service: membership, switches, delivery (section 15)
mudlib/channels/    one data file per channel (wizard.cs, sysop.cs), read by /secure/channels (section 15)
mudlib/domains/hollow/
  village/        the eight village rooms (north_road, gate, square, guild, ironoak, tavern, guest_rooms, cellar)
  cellars/        the ten dungeon rooms (collapsed_passage to overseer_chamber)
  npc/            villagers
  mob/            hostile creatures
  obj/            weapons, armor, potions, keys, quest items
```

`/domains/town` stays as the small technical demo that the driver smoke test uses.

## 3. Rooms

New protected members on `Room`, all optional:

| Member | Purpose |
|---|---|
| `AddDetail(string names, string description)` | Scenery that `look at` finds when no object matches. `names` is a comma-separated list of lower-case words or phrases (`"well,stone well"`). |
| `AddReadable(string names, string text, string? look = null)` | Text shown by `read <name>`. The optional third argument is what `look at <name>` shows instead of the text (the quest board has a short look and a long read). Without it, `look at` shows the text too. |
| `AddSpawn(string path, int count = 1)` | Creatures or items this room should contain. Cloned in at creation and topped up at each reset: the room keeps the handles it spawned and replaces the ones that are no longer alive or, for items, no longer in the room. |
| `AddEncounter(string path, int percent)` | Each time a player walks in, each encounter has `percent` chance to appear, if none spawned by this room is still alive and at least 60 seconds have passed since the last one. Encounter creatures that are not in combat leave ("wanders off") at the next reset. |
| `LockExit(string direction, string keyId, string refusal)` | The exit can be used only by someone carrying an object whose `Id(keyId)` is true. Others get `refusal`. The key is not consumed. |
| `BlockExit(string direction, string message)` | A way nobody can use yet (the wilderness road). It is not listed among the exits (the brief lists only south), but typing the direction gives the message. |
| `Safe` | No combat can start or continue here. |
| `ResetSeconds` | Default 300. Each room re-arms a call-out to its own `Reset()`. `Reset()` tops up spawns and calls `OnReset` on the creatures it spawned (a shopkeeper rerolls its rare stock there); override it (and call `base.Reset()`) for anything else. Corpses are not touched by a reset. |

Public functions for other objects: `QueryDetail(name)`, `QueryReadable(name)`, `QuerySafe()`, `QueryLogoutRoom()`, `QueryExits()`, `CanUseExit(who, direction)`, `MoveThrough(who, direction)` and `Stat()`. An exit whose destination file does not exist answers with a friendly "not open yet" message.

`Long()` for a room lists, after the description and exits, the visible objects. Creatures and items are shown as today (`A ginger tavern cat is here.`); several identical shorts are merged (`Two giant rats are here.`; corpses merge as `Two corpses of a large rat are here.`).

Exit names may be any single word (`north`, `up`, `down`). The one-letter abbreviation rule stays, with `u` and `d` for up and down.

## 3a. World features: hidden exits, search, patrols, nodes, danger bands

Built for the wilderness road (docs/PLAN-corridor-crafting.md, F2). Builder examples are in BUILDING.md; this is how they work.

**Hidden exits.** `Room.HiddenExit(direction, destination, hintDetail, findChance, foundLine)` keeps the exit apart from `Exits`: `QueryExits()` and `flee` never see it, the room binds the direction for every player but `GoExit` answers only a character who has found it (to anyone else the word is not a command at all). What a character has found is the saved list `foundExits` on the adventurer, entries of the form `room path|direction`. Only a room can write to it: `RecordFoundExit(direction)` takes the room from `PreviousObject()`, refuses a clone, refuses unless the character is standing in that room and the room declares that exit, and so a record can only ever name the room that made it. Records under `/wiz` are capped at 50 per character (1000 in all), so a sandbox cannot grow a save file or reach a trusted room's record. The record is written to disk as soon as it is made. Finding happens in two places: the player's `look at` calls `Room.Examined`, and the `search` action calls `Room.Search`; both require the character to be the caller. A newly found exit tells the character the exit's `foundLine`, then calls the room's `protected virtual ExitFound(who, direction)` once. Someone watching a hidden departure reads the direction only if they have found that exit themselves; otherwise "leaves somewhere". Wizards (`QueryWizLevel() >= 1`) see all hidden exits in the list, marked, and are told "that way is hidden" if they try one they have not found; `stat here` lists them.

**`search`** is a command for every player. It cannot be used while fighting. Otherwise it is an action on the combat clock (COMBAT_TIMING.md): Preparation 0, Recovery 0, reaction modifier -1.0, so it takes the character's Reaction Time less 1 (not below 0), doubled into seconds: 3 seconds at 2.5. If the character is still recovering from something else it is queued like a potion (`stop` cancels), and it is checked again when it happens, so a search queued before a fight does nothing once the fight has begun. It uses `Living.ActAsAction(action, waiting)`, a general version of `UseAsAction`, and the adventurer's `Special` override. A room with nothing to find, and a room where every roll failed, give the same line ("You search carefully but find nothing of note."), and a search always costs the same time, so the delay and the text give nothing away. The room hears "Name searches the area carefully."

**Danger bands.** `Room.DangerBand(low, high)` or `BandLow`/`BandHigh` in the directory's `area.cs`; `QueryBandLow()` and `QueryBandHigh()` answer for the room, falling back to the area. `Room.MoveThrough` warns an interactive character once, after the new room's description, when the new room's low end is at least 2 above their level and the room they left was not (a room with no band counts as not dangerous). Entering by login, `goto`, or the respawn does not warn. `consider`, `score` and `look` are unchanged.

**Patrols** (`/std/patrol`, an `Npc`). A route, a pace, loop or there-and-back; walks only through real, ordinary, unlocked exits; stops while fighting and for `TalkPauseSeconds` after `talk` or `ask` (the `Npc.Spoken` hook); with `DefendPlayers` joins (`BeginFight(creature, true)`, so it waits its own Reaction Time before its first action) against any creature fighting a player in its room, both when the fight starts (the room's `JoinFight` call) and when the patrol arrives; dies by leaving for `/std/void` and returning to the start of its route after `ReturnSeconds`. Cost: nothing is scheduled to walk while no player is in a room of the route; a poll every `PollSeconds` (default 15) compares the rooms of the users to a set of route names, loads nothing and moves nothing. When a player is on the route it steps every `PaceSeconds` and checks the players at each step. Safety: a patrol whose file is under `/wiz/<name>/` walks only in `/wiz/<name>/`; any other patrol never walks into `/wiz`.

**Resource nodes** (`/std/node`, a `Thing`). `Kind`, `Charges`, `RegrowSeconds`, `Difficulty`, `FullDesc`, `PickedDesc`, `PickedShort`. Regrowing is lazy: each `TryGather` records the time its charge returns, and every query clears the ones that have passed, so a node has no timer and costs nothing while untouched. `TryGather(who)` needs `who` alive, a creature and in the node's room.

## 4. Living things

`Living` (abstract, inherits `Thing`) holds:

| Field | Meaning | Default |
|---|---|---|
| `Level` | 1 and up | 1 |
| `MaxVitality` (old name `MaxHp` still works), `Vitality` | Maximum and current Vitality | 10 |
| `DamageMin`, `DamageMax` | Unarmed or natural attack | 1, 2 |
| `ToHit` | Bonus on attack rolls | 0 |
| `Armor` | Natural armor | 0 |
| `Gold` | Coins carried | 0 |

Derived values include equipment: `QueryDamage()` uses the wielded weapon if any, `QueryArmor()` adds worn armor, `QueryToHit()` adds the weapon's bonus.

Equipment slots: `weapon`, `shield`, `body`, `head`, `trinket`. One item per slot. An item leaves its slot automatically when it leaves the wearer's inventory.

### Combat

Combat has no rounds. Each fighter has its own clock, described fully in [COMBAT_TIMING.md](COMBAT_TIMING.md); this is the summary.

- `Living.StartFight(ObjectRef enemy)` starts a fight. Refused if either is dead, they are in different rooms, the room is safe, or the target is peaceful (`QueryPeaceful()`, true for villagers). The starter may act at once; the target and any monsters that join in must wait their own Reaction Time x 2 seconds before their first action. A player's fight is started only by the player's own commands (`kill`, a skill): `StartFight` refuses for a player character, so no other object can start one for them. **The ranged opening:** when a character who chose the fight has a bow in their hands, the target's first action waits a further `RangedOpening` = 2 seconds, once per fight, never when a creature starts the fight (every aggressive creature engages on arrival, so the bonus is the hunter's); CLASS_SYSTEM.md section 14.
- **Delay between actions** = (final Reaction Time + the action's reaction modifier + the action's Recovery) x 2 seconds. Final Reaction Time = (base + equipment modifiers + encumbrance, 0 for now) x the product of haste and slow effects. Players have base 2.5, so an unarmed attack (Recovery 0.5) comes every 6 seconds.
- An action with a Preparation shows a wind-up line when it begins and takes effect Preparation x 2 seconds later (inside its delay, not added to it). If the target has died or left in between, the blow is wasted.
- One call-out per fighter drives the clock. Heartbeats are used only for regeneration and idle NPC behaviour.
- A player can queue one action (a potion, food) while recovering; it happens at the next ready moment in place of an attack. A newer command replaces it and `stop` clears it.
- **To hit:** the defender's dodge is rolled first; then `d20 + attacker QueryAttack() + action bonus >= 10 + defender QueryArmor() + defender QueryDefense()`, where `QueryAttack() = round(attack rate x level) + QueryToHit()` and `QueryDefense() = level - 1`. A natural 20 always hits, a natural 1 always misses. A hit is critical with 5% (x1.5); a defender with a shield blocks 15% of hits (damage halved). See [CLASS_SYSTEM.md](CLASS_SYSTEM.md).
- **Damage:** uniform in the action's damage range (times the action's multiplier), minimum 1.
- Enemies that are no longer in the room are forgotten after 30 seconds, so walking away ends a fight, and a creature does not chase.
- Messages: the attacker, the defender and bystanders each get a line, varying by damage (miss, scratch, hit, hard hit). One line per attack per observer.
- `Living.Die(ObjectRef? killer)` is virtual and calls `ResetCombat()`. See monsters, players and corpses below.
- Players cannot fight each other: `CanFight` refuses with "Players cannot fight each other." Monsters do not regenerate.
- `Living.Heal(int amount)`, `Living.TakeDamage(int amount, ObjectRef? from)`.

The creature API is `BaseReaction` and `AddCombatAction(id, name, preparation, recovery, windUp, damageMultiplier, reactionMod, strike, kind)`; see BUILDING.md for compiled templates.

### Hooks for special behaviour

- `protected virtual CombatAction? ChooseAction(ObjectRef target)`: which action to begin next (default: the first action). Boss patterns are written here, usually with `ActionsTaken()`.
- `protected virtual void Special(CombatAction action, ObjectRef target)`: what a non-attack action (kind other than "attack") does when it resolves.
- `protected virtual int ModifyDamage(int damage, ObjectRef target)`: last word on outgoing damage.
- `protected virtual void FightEnded()`: the enemy list became empty; reset per-fight state here.

## 5. NPCs

`Npc` (inherits `Living`):

- `Peaceful = true` by default: cannot be attacked ("... is no threat to you" style refusal).
- `AddChat(string line)`: what the NPC says to `talk <npc>` (alias `talk to <npc>`, `greet <npc>`). Lines are given in order; each `talk` gives the next line and wraps round. The first line added is the NPC's main dialogue.
- `AddChatAfterQuest(string questId, string line)`: once a player has completed the quest, the main (first) chat line is replaced for that player by this one. Harland uses it to thank the player for clearing the cellar.
- `AddTopic(string keywords, string line)`: reply to `ask <npc> about <keyword>`. Unknown topics get a shrug line.
- `AddEmote(string line, int percent)`: idle action shown to the room on heartbeat, only while a player is present. The NPC turns its own heartbeat off when the room has no players.
- Speech is shown as `Harland says: "..."`.

## 6. Monsters

`Monster` (inherits `Npc`, `Peaceful = false`):

| Field | Meaning |
|---|---|
| `Aggressive` | Attacks any player who enters, as soon as the arrival is complete: the player sees the room, then "A wolf attacks you!", and is in combat (walking on is refused, `flee` is the way out). The creature's first action still waits its own Reaction Time x 2 seconds. Nothing changes where it could not attack before (it is dead, the player left, a safe room, `CanFight` refuses). |
| `XpValue` | Experience for the kill. |
| `GoldMin`, `GoldMax` | Coins given to the killer. |
| `AddLoot(string path, int percent)` | Items that may be found in the corpse, each with this percent chance. |
| `Assist` | Default true: joins a fight when another monster in the room is attacked. |

On death: a death line for the room; experience at once, shared by the players who hurt it (see Shared experience below), and `NotifyKill` to each of them so quests can react; a **corpse** is cloned into the room holding the rolled gold (as a coins item) and the loot (see Corpses in section 9); then the monster is destructed. The room's reset brings a new one. Gold and loot are no longer paid or dropped directly: the killer must `loot` the corpse.

### Creature numbers for the Hollow

| Creature | Level | Vitality | Damage | To hit | Armor | XP | Gold | Base Reaction | Actions (Preparation / Recovery) |
|---|---|---|---|---|---|---|---|---|---|
| Large Rat | 1 | 12 | 1-2 | 0 | 0 | 8 | 0 | 2.0 | Bite 0 / 0.5; aggressive |
| Giant Rat | 1 | 15 | 1-3 | 0 | 0 | 12 | 0 | 2.0 | Bite 0 / 0.5; aggressive |
| Wandering Wolf | 2 | 26 | 2-4 | 1 | 1 | 25 | 0 | 2.0 | Bite 0 / 1.0; aggressive; encounter |
| Goblin Scavenger | 2 | 13 | 1-3 | 2 | 1 | 22 | 2-8 | 1.5 | Quick Stab 0 / 0.5; fast but fragile |
| Spore Bat | 2 | 9 | 1-2 | 1 | 3 | 20 | 0 | 1.5 | Swoop 0 / 0.5; evasive |
| Goblin Guard | 2 | 26 | 2-4 | 0 | 2 | 32 | 4-12 | 3.5 | Spear Jab 0 / 1.0; one carries the Rusty Key |
| Giant Cave Toad | 3 | 66 | 2-4 | 0 | 0 | 50 | 0 | 4.5 | Tongue Lash 0 / 1.0; Crushing Leap 1.0 / 2.0 (x2 damage) as every third action |
| Goblin Warrior | 3 | 28 | 2-4 | 0 | 2 | 42 | 5-14 | 3.0 | Slash 0 / 1.0 |
| Goblin Minion | 2 | 20 | 2-4 | 0 | 1 | 15 | 0 | 2.5 | Stab 0 / 0.5; summoned by the boss only |
| Goblin Elite Guard | 4 | 40 | 3-5 | 2 | 3 | 85 | 12-22 | 3.5 | Slash 0 / 1.0; Shield Bash 0.5 / 1.5 (x2) as every fourth action |
| Goblin Overseer | 5 | 114 | 4-7 | 2 | 3 | 250 | 0 | 4.0 | See below |

Goblin Overseer actions (delays in seconds are (4.0 + reaction mod + Recovery) x 2):

- **Battle Cry** (0 / 0.5, 9 s): his first action in a fight. For the next 30 seconds his damage is +2.
- **Axe Slash** (0 / 1.0, 10 s): the normal attack.
- **Power Strike** (1.0 / 2.0, 12 s): double damage, announced 2 seconds before it lands; every third attack, in a fixed pattern (slash, slash, power).
- **Summon Goblin Minion** (0.5 / 1.0, 10 s): his next action once his Vitality first falls to half or below. Once per life. The minion joins the fight but waits its own Reaction Time (5 s) before acting.

These are the final numbers after the event-clock balance simulation (section 12). Compared with the 2-second-round version, Vitality is about 1.5 to 2.2 times higher and weapon damage about 3 times higher, because a player now strikes every 6 to 11 seconds instead of every 2. Every creature of the table has a mudlib file (the Guard Post keeps one plain Goblin Guard and one `goblin_guard_keyholder` that is identical except that its corpse always holds the Rusty Key). The Overseer is aggressive (it attacks anyone who enters its chamber). Gold from monsters is in the corpse. Creature names are capitalised (`Name = "Goblin Guard"`, used by `stat` and by shopkeepers and NPCs when they speak); what players read in fights and on corpses is the short description with its article ("a goblin guard", "the corpse of a goblin guard").

## 7. Items

`Item` gains `Value` (gold), `QuestItem` (cannot be sold or given), `ReadText` and `protected virtual void OnGet(ObjectRef taker)`, called after the item has been moved to a taker by `get`, `get ... from` or `loot`. `Vanish()` lets an item destruct itself when it is sold, eaten or handed in (only player bodies may destruct other objects).

| Class | Fields | Commands |
|---|---|---|
| `Weapon` | `DamageMin`, `DamageMax`, `ToHit`, `ReactionMod`, `Recovery` | `wield <weapon>`, `unwield` |
| `Armor` | `ArmorValue`, `Slot`, `ReactionMod`, optional `MaxVitalityBonus` | `wear <item>`, `remove <item>` |
| `Consumable` | `HealAmount`, `UseVerb` (`eat`, `drink`), `UseMessage`, `Recovery` | `eat <item>`, `drink <item>` (also `quaff`); a queued action |
| `Coins` | `Amount` (or `AmountMin`/`AmountMax`, rolled when first seen), `SetAmount(n)` | Picking it up (or looting it) adds the gold and destructs the item |

### Quality, stacks and the save record (F1)

Every item carries instance state: a **quality** (0 Poor, 1 Common, 2 Fine, 3 Excellent, 4 Masterwork, 5 Legendary; default Common), a **quantity** (default 1) and up to 16 named text **properties** for later systems. The state lives in `/std/item` as private fields; the helpers in `/std/thing` work through its public functions.

- **Value.** `QueryValue()` is the value of one item: base `Value` times the quality multiplier, rounded half away from zero and at least 1 (a base of 0 stays 0). The multipliers are data in one place, `Item.QualityMultipliers`: Poor 0.5, Common 1, Fine 1.5, Excellent 2.5, Masterwork 4, Legendary 8. `Item.ValueFor(base, quality)` computes it.
- **Names.** Quality is shown as a prefix word ("a fine wolf skin", "an excellent wolf skin"); Common shows none. A quantity above one shows as a count with the plural ("3 fine wolf skins"); the plural is `Plural`, or the short description without its article plus "s" ("es" after s, x, z, ch, sh). An item answers to its names, to their plurals, and to either after its own quality word; every name it answered to before still works. `look at` a stack, or at any item whose quality is not Common, adds a line with its quality and count; `appraise` adds the quality. No colour tags are added to `Name`, `Ids` or `ShortDesc`.
- **Stacks.** An item type is stackable with `Stackable = true`. A stack is one object. `Settle()` merges a stack into another in the same container if both are of the same file, quality and properties (and neither is equipped); the survivor is the stack that was already there, and the arriving object is destructed. Every standard move (get, drop, give, buy, loot, a corpse spilling, login, lost property, `CreateIn`) settles; the bare `MoveObject` efun does not, so room spawns and monster loot never merge by themselves. `Split(n)` makes a new object of the same file and state beside the stack, holding n. The two halves of a merge or split talk to each other through `ReceiveStack` and `TakeSplit`, which accept only an object of the same program file in the same place, so the quantity is never written by anyone else.
- **Who may change state.** `SetQuality`, `SetQuantity`, `SetProperty`, `RestoreState` and `Split` check `PreviousObject()`: code from `/std`, `/secure`, `/domains`, `/classes`, `/skills` and `/events` may change any item; code from anywhere else (a wizard's `/wiz` home) may change only items that are themselves outside those trees. A direct typed call (`As<Item>()`) is caught too, because the driver reports the calling object as the previous object. Cloning is unchanged: sandbox code can still clone a real item in its default state, as before, but cannot raise its quality or quantity or split someone's stack.
- **The save record.** Wherever items are persisted, each item is one string: its program path, then `?` and its state if that is not the default. State is `q=<quality>`, `n=<quantity>` and `p.<name>=<value>` joined by `&`, with `%`, `&`, `=` and `?` in names and values written as `%XX`. A Common single item is still a bare path, which is also what saves made before F1 hold, so old saves load unchanged as items in the default state. Examples: `/domains/hollow/obj/rusty_sword`, `/domains/test/wolf_skin?n=3`, `/domains/test/wolf_skin?q=2&n=5&p.maker=Ann`. The path always comes from the object's `ProgramPath`, never from what the item says about itself. A malformed state part is ignored; a file that is gone loses that item, as before. If a file has stopped being stackable, a record with `n=` comes back as that many separate objects (at most a thousand).
- **Commands.** `get`, `drop`, `give`, `sell` and `get ... from <corpse>` take `N THING`, `all THING` or `THING` (one); `get all`, `drop all` and `give all` take everything (equipped things are left alone by "all", but can be named). Asking for more than there is refuses and moves nothing. `buy N THING` works for stackable stock and makes one stack.

### Equipment for the Hollow

| Item | Type | Numbers | Value | Source |
|---|---|---|---|---|
| Rusty Sword | weapon | 9-14, reaction +0.5, Recovery 1.0 | 10 | Ironoak Arms |
| Training Spear | weapon | 8-12, to hit +1, reaction +0.5, Recovery 1.0 | 8 | Ironoak Arms |
| Iron Dagger | weapon | 6-9, to hit +1, Recovery 0.5 | 6 | Ironoak Arms |
| Woodcutter Axe | weapon | 14-21, reaction +1.0, Recovery 1.5 | 18 | Ironoak Arms |
| Small Shield | armor, shield | 1, reaction +0.5 | 12 | Ironoak Arms |
| Fine Iron Longsword | weapon | 12-18, reaction +1.0, Recovery 1.0 | 60 | Ironoak Arms, rare |
| Guild Initiate's Helm | armor, head | 1 | 15 | Reward, Clear the Cellar |
| Goblin Overseer's Axe | weapon | 17-25, reaction +1.5, Recovery 1.5 | 70 | Boss corpse, always |
| Leather Commander's Vest | armor, body | 3 | 60 | Boss, always |
| Bag of Coins | coins | 60-100 | | Boss, always |
| Overseer's Orders | quest item | readable | 0 | Boss, always |
| Rusty Key | key | | 0 | The corpse of one of the two Goblin Guards, always |
| Minor Healing Potion | consumable, drink | heals 20, Recovery 0.5 | 15 | Storage Chamber; merchant |
| Magical Trinket | armor, trinket | max Vitality +5 | 40 | Hidden Cache, always |
| Rare Enchanted Dagger | weapon | 7-11, to hit +2, Recovery 0.5 | 80 | Hidden Cache, 25% |
| Random Gold | coins | 30-60 | | Hidden Cache, always |

Any armour or weapon may also name the callings that can or cannot use it (`WornBy`, `NotWornBy`; BUILDING.md, "Restricting a piece by calling"): `wear` and `wield` refuse in words, `look`, a shop's `browse` and a warning before `buy` show it, and an advanced class counts as its calling. The Leather Jerkin is not worn by a Mage; the Scholar's Robe is worn by Mage and Priest only.

## 8. Shops and services

`Shopkeeper` (inherits `Npc`):

- `AddStock(string path, int price)` and `AddRareStock(string path, int price, int percent)`. Rare stock is rolled when the shopkeeper is created, so it changes at reset of its room.
- Commands offered to players in the room: `browse` (what is for sale, with prices), `buy [N] <item>` (several only of stackable stock, as one stack, at most 1000), `sell [N|all] <item>` (pays half the value of one item, quality included, times the number sold; refuses value 0 and quest items), `value <item>` (per item and for the whole stack).
- `AddStock(path, price, quality)` sells a quality; the price is the listed price times the quality's multiplier. A purchase of one calls `OnBuy(buyer, path)`, a purchase of several `OnBuyStack(buyer, path, count)`; both make the items with `CreateIn`.
- Stock is unlimited; each purchase clones a new item into the buyer's inventory.
- The command is `browse`, not `list`, because `list` is the wizard directory command.

Services use the same class with a different stock:

- **Harland the Innkeeper** sells food and drink (consumables) and a bed. Buying the bed restores the buyer to full Vitality and takes them upstairs to the Guest Rooms.
- **Traveling Merchant** sells Minor Healing Potions and trail rations.

`Shopkeeper` has `protected virtual bool OnBuy(ObjectRef buyer, string path)` so a service can do something other than hand over an item. Stock samples are kept inside the shopkeeper so that `buy sword` and `value sword` find them by name. The Traveling Merchant sells potions at 15 and rations at 5. Harland sells stew (4), ale (2) and the bed (5); buying the bed heals the buyer fully and takes them up to the Guest Rooms.

## 8a. Classes, skills and areas

The five callings, their skills and traits, equipment categories and access, the level-scaled attack rule, the travel pass and the Vale Road are described in [CLASS_SYSTEM.md](CLASS_SYSTEM.md). Every living thing has Endurance and Focus. Weapons and armor have a `Category`. Rooms get their area and region names from an `area.cs` in their directory (`score` shows them). Creature numbers in the tables here are unchanged by the class system; `tests/balance_sim.py` shows how each archetype fares against them.

**Skill ranks** ([specs/skill-ranks.md](specs/skill-ranks.md); CLASS_SYSTEM.md section 13). Skills and spells do not improve with use. Each one with a command word has ranks (I, II, III ...), the rank a character may hold capped by level (rank r at level 5 x (r - 1)), bought with gold from a trainer of the calling: `ranks` at the trainer lists every offer with its price and each requirement's progress ("Goblins slain: 6 of 10"), `train SKILL` buys the next rank. Rank II is taught in Vincent's Hollow (Halbert Quill at the Hedge-Scholar's Loft for Mages, Sister Merewyn at the shrine for Priests, Wenna Farrow at the Strung Yew for Scouts, Brann Odell at the Practice Yard for Warriors and Adventurers); ranks III to V by the hallmasters of the Hall of Callings in Oakenfell. An offer may ask for deeds as well as gold (Arcane Bolt II: ten goblins slain); once met at the trainer the unlock is the character's for good. The counters the requirements read (kills by creature kind, killing blows by skill) are saved with the character; only `/secure/ranks` may change them, a rank or an unlock. `skills` shows the rank of each skill and the level of the next. The Priest's Minor Heal restores 20 / 38 / 60 Vitality at ranks I / II / III and cannot be cast again by the same caster for 15 seconds (`skills` shows the wait). The Scout has Aimed Shot (`aim`, bow only; 30 Endurance; hits hardest as the shot that starts the fight; a blow that lands while it is aimed spoils it; rank II from Wenna Farrow, III and above from Veyla; CLASS_SYSTEM.md section 14). The Adventurer has Double Strike (`double`, 35 Endurance; Triple and Quadruple Strike at III and IV) and the trait Jack of All Trades (crafting and gathering proficiency gained 25% faster).

## 9. The player character

`Adventurer` (abstract, inherits `Living`) carries everything about playing the game; `Player` inherits it and keeps accounts, wizard tools and files.

Saved with the player: level, experience, Vitality, gold, quest states, a save record (path and state, section 7) for each carried item and which are equipped, and the logout room.

- **New character:** chooses one of five callings after the password (Warrior, Scout, Priest, Mage, Adventurer); level 1, the calling's Vitality, gold, resources and starting kit (CLASS_SYSTEM.md section 7); unarmed 1-3.
- **Experience to reach level n**, times the experience modifier of the character's classes: 100, 300, 600 and 1000 for levels 2 to 5 (50 x n x (n - 1)); 5,400, 12,000, 21,700, 34,100 and 49,300 for levels 6 to 10 (the corridor pace model, [CORRIDOR_BALANCE.md](CORRIDOR_BALANCE.md) section 4); and 49,300 x (n / 10)^3 above level 10. **The curve beyond level 10 is provisional**: it only continues the last step smoothly and is to be re-fitted when there is content for those levels. The level cap is 100.
- **Each level:** +8 max Vitality, and full heal on levelling. (Attack grows by the class's attack rate per level and Defense by 1.)
- **Regeneration:** 1 Vitality every 10 seconds out of combat; 3 in a safe room.
- **Resting** (`rest` or `sit`, `stand`; the rule is `docs/specs/rest.md`): a seated character recovers Vitality, Endurance and Focus **RestFactor** (5) times as fast, on top of the safe room and every trait. `RestFactor` and `RestSurprise` (2) are two named constants at the top of `std/adventurer.cs`, the only place the numbers live. It is refused in a fight. It ends when the character stands, moves, attacks or uses a skill, picks up, drops, wields, wears or removes something, crafts or gathers, buys or sells, or is attacked; looking, `score`, `inventory` and talking do not end it. A creature that attacks a seated character gets in first: the character scrambles up and its first action waits `RestSurprise` seconds more than the usual Reaction Time. The room lists "X is resting here."; "You are fully rested." is said once; the state is never saved; resting does not reset the idle timer; creatures and NPCs never rest. Rooms refill exactly as before, whether someone is resting in them or not. The state is private to the character: the only public doors are `QueryResting()` (a question) and `RestDisturbed(verb)`, which a shop or a find calls for the character's own `buy`, `sell`, `get`, `take` or `loot` and which does nothing for any other caller or verb; and `AddEnemy` now accepts only a creature making itself the enemy (so no other object can start a fight for it). The prompt token `{rest}` shows "(resting) ".
- **Death:** the player is told so, and a **corpse** in the room where they died takes everything they carry (equipment included) and a gold fee of 10% of their purse. They wake in the Guest Rooms at full Vitality, with experience and level intact, and must walk back to the corpse and `loot` it. Their killer forgets them. The loss is saved at once.
- **Logout room:** quitting in a room whose `QueryLogoutRoom()` is true (the Guest Rooms) makes that the next login room. Otherwise players enter at the Village Square.
- **Inventory persistence:** on save, the record of each carried item (path, quality, quantity, properties) and which are equipped are stored and the items are destructed with the player on quit; on login they are cloned again with their state, merged into stacks and re-equipped. Other per-item state (a lantern being lit) is not kept. Items do not drop to the floor on quit.

Commands, available to every player:

| Command | Effect |
|---|---|
| `kill <target>` (also `attack`) | Start a fight |
| `flee [direction]` | Leave the fight; fails 25% of the time. With no direction it goes back the way you came, else by a random usable exit, else "There is no way out!". A direction that is not a usable exit fails without spending the attempt |
| `verbose [on\|off]` | Show `(+N damage)` in green for damage you deal, `(-N damage)` in red for damage you take, `(+N Vitality)` for healing. Off by default, saved with the character |
| `prompt [text\|default\|off]` | Show or set the line printed after every command and after unprompted output. Tokens `{name} {vitality} {maxvitality} {endurance} {maxendurance} {focus} {maxfocus}`; default `{name} ({vitality}/{maxvitality}) ({endurance}/{maxendurance}) ({focus}/{maxfocus}) > `. The old tokens `{hp} {maxhp} {stamina} {maxstamina} {mana} {maxmana}` still work, silently, so saved prompts do not break; `off` gives `> `. At most 80 characters, no colour tags or control characters from the player's text. Saved. The token table is `Adventurer.PromptTokens` |
| `sync TOKEN` | A client aid. Answers to the sender only, in one line `[sync TOKEN]`; the token keeps letters and digits only, 16 at most. It takes no game time and is not an action on the combat clock. A client sends it right behind a command to know where the reply ends (the prompt cannot tell, since it also follows unprompted output). It counts as input for the idle timer. Help lists it |
| `consider <target>` | Rough comparison of levels |
| `search` | Look for a hidden exit; a short timed action; not while fighting (section 3a) |
| `wield`, `unwield`, `wear`, `remove` | Equipment |
| `eat`, `drink`, `quaff` | Consumables |
| `talk <npc>`, `ask <npc> about <topic>` | Dialogue |
| `read <thing>` | Readable details and readable items |
| `get`, `drop` `[N\|all] <thing>` | Pick up or put down; amounts split stacks (section 7) |
| `give [N\|all] <thing> to <player>` | Hand things to another player in the room. Quest items (the travel pass, the Overseer's Orders) cannot be given. The giver, the receiver and the room are told. Both characters are saved at once, the giver first, so a crash cannot leave a gift in both save files. Not to NPCs. |
| `loot [<corpse>]`, `get <thing> from <corpse>`, `get all from <corpse>`, `look in <corpse>` | Corpses, see below |
| `browse`, `buy`, `sell`, `value` (or `price`) | In a shop |
| `accept`, `report` | In the Adventurers Guild |
| `ansi [on\|off]` (also `color`, `colour`) | Show or change ANSI colour (section 14) |
| `score` (also `sc`) | Level, experience and what the next level needs, Vitality, gold, equipment, quests |
| `vitality` (old name `hp` still works) | One-line Vitality, Endurance and Focus |
| `skills` | Your skills, spells and traits with costs and timing |
| `quests` | Quest log |

`look at <thing>` falls back to room details, and `look at <creature>` adds a rough health line. `stat <thing>` accepts `me`, `here` or a name present in the room.

Public functions on the player for other objects: `QueryLevel`, `QueryVitality`, `QueryMaxVitality`, `QueryGold`, `AddGold(int)` (negative to take; refuses to go below zero and returns false), `AddXp(int)`, `QueryQuest(string id)`, `SetQuest(string id, int state)`, `NotifyKill(string programPath)`, `QueryKilled(string programPath)`.

### Wizard tools for builders

| Command | Level | Effect |
|---|---|---|
| `goto <room path or player>` | 1 | Move to a room, or to where a player is |
| `stat <thing>` | 1 | Show a living thing's or item's numbers |
| `restore [<target>]` | 5 | Restore full Vitality, Endurance and Focus (was `heal`; the word now belongs to the Priest's spell) |
| `slay <target>` | 10 | Kill a creature at once, with normal death processing and kill credit |

These follow the existing rule: below the level, the command answers "What?". `shutdown` now saves every online player before the driver stops.

### Corpses

`/std/corpse` is a container that cannot be picked up. Its short is `the corpse of <label>` (`the corpse of a goblin guard`, `the corpse of Keith`), its ids are `corpse` and `corpse of <name>`, and `look at corpse` (or `look in corpse`) lists what it holds, or says it is empty, and for a viewer who may not loot it adds "It was felled moments ago; only those involved may touch it."

- **Who made it.** A monster leaves one holding its rolled gold (a coins item) and its loot; always-drop items (the Overseer's four items, the Rusty Key) are in it. A player leaves one holding everything they carried and the gold fee. Experience and kill credit are still immediate.
- **The lock.** For `LockSeconds` (1800) only a player whose name equals the killer's or the dead player's may take things. Both are recorded by lower-case player name, because a player's object changes at logout. A non-player killer or victim records nothing. After the lock anyone may loot. The check uses the clock at every attempt; there is no timer. Refusal: "The corpse of X is not yours to loot yet."
- **Taking things.** Only `Take(itemId)` and `TakeAll()` on the corpse hand things over; they identify the taker with `PreviousObject()` and `ThisPlayer()` and return null or the refusal text. The plain `get` command sees only what lies directly in the room. `loot` goes through the room's corpses, skipping those the player may not loot.
- **Decay.** A corpse crumbles to dust at once when its last item or coin is taken and no skin or blood remains, and when the last yield is taken from an empty corpse. A corpse that was empty from the start keeps a short timer (two minutes). A creature's corpse rots away 600 seconds after the death, whatever is still in or on it (loot, a skin), and its contents with it; the number is one field on the corpse, and a creature file may set its own with a public `int CorpseSeconds` (1 to 3600). A player's corpse that still holds things keeps 60 minutes, then rots and spills them on the floor. Reset does not touch corpses. A wizard of level 10 can shorten the times of a corpse (`SetTimers`) for tests.
- **The lost-property store.** `/secure/lost_property` is a blueprint-only daemon (loaded at boot, saved as `/secure/lost_property`; a save left by the old name `/secure/vault` is read once and carried over). A player corpse reports its contents to it when created and after every change; an empty or gone corpse removes its record. A player may have several corpses; records are keyed by name plus a corpse id. After a restart every record on disk refers to a corpse that no longer exists, so it is unclaimed; records made during the current run are live and cannot be claimed. At login, after the inventory is restored, the player is handed anything recorded under their name (Harland, the innkeeper, kept it safe), the records are deleted, and the player is saved. Only a corpse clone may record or clear, and only a player body may claim, for its own name taken from the calling object. Monster corpses are not recorded. Equipment state is not kept in the vault. Items are recorded as save records (section 7), so stacks come back with their quality and quantity; records written before F1 are bare paths and come back as items in the default state. `get N <thing> from <corpse>` takes part of a stack, under the same lock.
- **Player versus player.** The code does not allow players to attack each other, so no player can currently kill another; the lock rule would let a player killer loot a victim's corpse if that ever changes.

### Finds

**The rule: each character can take a find once and never again, and each new finder gets less than the one before, until nothing is left; it never refills.**

A find is treasure in a hidden place. It replaces coins placed with `AddSpawn`, which came back at every reset so that anyone could farm them. The first character to take a find gets its full amount; the next gets a fixed percent of that (default 60), rounded down; and so on until the amount would fall below the minimum (default 1 gold), when the find is empty for everyone after, for good. With 100 gold and 60 percent the finders get 100, 60, 36, 21, 12, 7, 4, 2 and 1, and the tenth gets nothing. Rounding down means every finder gets strictly less than the one before, so a find always ends. Builders' fields and a copy-ready example are in BUILDING.md ("A find").

- **The object.** `/std/find` is a `Thing` that lies in a room and cannot be picked up (`CanGet()` is false, so `get all` leaves it). It binds `get`, `take` and `loot` for any player who comes near, newest binding first; it handles the words only when they name it and it is the first thing in the room that answers to them, and otherwise the ordinary commands go on. On a successful take the character is told how much, `AddGold` pays it, an optional item is cloned into their inventory (at their feet if they cannot carry it; no carry limit exists today, so that path is only a guard), the room is told that the character searches it, and the character is saved. A character who has taken it is told so in one line; one who comes when it is empty is told that someone has been here before them and that nothing is left.
- **The register.** `/secure/finds` is a blueprint-only daemon, preloaded and saved as `save/secure/finds.json` (the master lets only that blueprint write it, like the history daemons). For each find id it keeps the lower-case names of the characters who took it, in order, and nothing else: the amounts are worked out from the find's own numbers and the count, so changing a number changes what later finders get and never who has already taken it. Its one public method is `Claim(character, gold, fallOff, minimum, hasItem, itemFinders)`; it answers a list of three: the gold, 1 or 0 for the item, and a code (0 paid, 1 already taken, 2 empty, 3 wizard, 4 refused, 5 not here). It checks, from `PreviousObject()` and never from an argument: the caller is a program in a trusted tree (`/std`, `/secure`, `/domains`, `/classes`, `/skills`, `/events`: the same list as the event service, in which `/wiz` is never trusted, because it is the wizards' sandbox) and lies in a room of a trusted tree; the character is a player body in the same room and is the character whose command is running. **The find's id is the caller's own program path**, so one trusted object cannot drain a stash it does not own, a find under `/wiz` is refused (it pays nothing and records nothing), a trusted find that a wizard drops into a `/wiz` room is refused too, and two stashes need two files. Renaming or moving a find's file starts a new stash.
- **Wizards.** A wizard of any level may look at a find but receives nothing and is not recorded, and is told so in one line. This mirrors the rule of the event service: wizards make no history.
- **What others see.** The description is honest for the character looking, in three states. `look` and the room's list call `Short()` and `Long()` with the looking character as `ThisPlayer()`, and the find asks the register (`QueryStatus`) what that character may take: the full description for a character who has not taken it and for whom something is left, the builder's `Taken` pair for one who has, the `Empty` pair for one who has not but would find nothing. A viewer who is not a player standing in the room (a call-out running for a monster, say) sees the full description. Taking is always decided by the register, never by what was shown.
- **Items.** An item is given by cloning it into the taker (`MoveObject`, then `Settle`, so stacks merge); if that fails the item is put at their feet, and if the file is gone the gold is still paid. `ItemFinders` limits it to the first n finders; `Gold = 0` with an item makes a keepsake that every character may take once, with no fall-off and no end (the grave token, worth 15).
- **Order of writes, and what a crash can do.** One command runs to its end before the next begins, so two characters taking in the same moment are two calls in a row and each gets a different amount. Inside one take the order is: (1) the register records the name and writes `finds.json` (temporary file, then rename, so a file is never half written); (2) the find pays the gold and the item; (3) the character is saved at once (`SaveAfterFind` on the player body; a `quit` saves again). The register goes first on purpose. A driver killed between (1) and (3) loses that one character's reward and the find stays taken; it can never pay twice. The other order would turn a crash into a second take. If the register cannot be saved, nothing is paid. A clean `shutdown`, a `quit` right after a take, a killed driver and an `update` of the room, the find file or `/std/find` all leave the record alone, because it lives in the register and not in the room or the object; a register file that cannot be read stops the daemon from loading, so every find then pays nothing, instead of paying everyone again.
- **The places.** The Dry Well purse (30 gold), the Sealed Culvert's coins (70), the split purse in the Borrowed Tomb (150) and the Cold Larder's chest (60 gold and a healing potion for each of the first five finders) are finds, and so is the token in the Tomb of the Seven Bowls (a keepsake). The first eight finders of the four gold stashes get 30, 18, 10, 6, 3, 1 and then nothing for the Well; 70, 42, 25, 15, 9, 5, 3, 1 for the Culvert; 150, 90, 54, 32, 19, 11, 6, 3 for the tomb (then 1); 60, 36, 21, 12, 7, 4, 2, 1 for the larder (then nothing).
- **Tests.** `tests/find_test.py` (port 4827).

## 9a. Crafting and gathering

Built as C1 of the corridor and crafting plan; the full design, every number with its file, the data formats and the launch recipes as data are in [CRAFTING.md](CRAFTING.md).

- **The daemon** `/secure/crafting` (blueprint only, preloaded) reads data tables under `/crafting` and recipe files under `/recipes`, found by listing (the master grants it `get-dir` on those two trees only) and checked again every five seconds. It indexes recipes for the matcher, refuses clashes at load and logs them, and runs every crafting command for the player body, which binds the verbs (`skin`, `bleed`, `harvest`, `craft`, `combine`, `recipes`, `recipe`, `proficiencies`, `apply`) in a self-contained block of `/std/adventurer`.
- **Proficiencies** (Skinning, Bleeding, Harvesting, Cooking, Herbalism, Tanning, Tinkering; more are one data line each) are saved on the character in tenths (`craftProficiencies`), with the known recipes (`craftRecipes`, id to "taught" or "discovered"). Only the daemon may change them. A successful piece of work rolls a chance at one whole point; the chance climbs a ladder (20, 25, 50, 75, 100 percent, each rung used 1 + skill / 20 times) and the character's place on it is saved beside the proficiency (`craftLadder`), never shown, and readable and settable by the daemon alone (docs/CRAFTING.md, Growth).
- **Gathering** is an action on the combat clock (`ActAsAction`; the action's skill is the daemon, so the clock calls back its `Resolve`), never while fighting. Corpses remember the creature's kind, level, skin and blood (read from the creature's `Skin` and `Blood` properties when it dies, only from trusted creature files); each part is taken once, under the loot lock, blood within five minutes. Nodes give what their kind's yields say. Quality is the owner's Harvest Quality Score; tool quality also raises success and bonus units; Legendary never comes from harvesting.
- **Crafting** consumes and creates in one step that happens entirely or not at all, saves the character before telling them, and never fails: output quality is drawn around the count-weighted average quality of the ingredients, shifted by proficiency against difficulty. The lowest quality held is used unless the player says `best` or `using`. Batches queue quietly at each Ready Time (`Living.QueueAction`).
- **Discovery**: trainers (`/std/trainer`), books (`/std/recipe_book`), any trusted object (`Teach`), or experiment (`combine`). The first experimental discovery of a Rare or Secret recipe reports `craft.discover_rare` or `craft.discover_secret` (`/events/craft.cs`) to the event service; wizards make no history.
- **Items**: `/std/material`, `/std/skin` and `/std/blood` (one object for every beast, the kind an item property; Goblin Blood can be given its kind only by the daemon or a player body restoring its save), `/std/tool`, `/std/station`, `/std/crafted` (effects as data, scaled by quality), and `CarryBonus` on `/std/armor` (recorded, inert). `Item` gained two hooks (`PropertyAllowed`, `StateChanged`); `RestoreState` fails when a property is refused; `CreateIn` and shop stock accept save records with properties (`/std/skin?p.kind=rabbit`).
- **Nodes**: `TryGather` on a node in a trusted tree now answers only trusted callers.
- **Tests**: `tests/craft_test.py` (port 4829) and `tests/craft_sim.py`.

## 10. Quests

Quest state per player is an integer per quest id: 0 not started, 1 active, 2 completed. There is no quest engine beyond that; a quest giver is an NPC with code.

Guildmaster Rowan, with the Quest Board:

| Quest | Id | Offered | Completed when the player returns and | Reward |
|---|---|---|---|---|
| Clear the Cellar | `clear-the-cellar` | `accept cellar` (or reading the board and `accept 1`) | has killed the Goblin Overseer | 150 XP, 50 gold, Guild Initiate's Helm |
| Recover the Overseer's Orders | `overseers-orders` | `accept orders` (or `accept 2`); also offered automatically on completing the first | is carrying the Overseer's Orders, which Rowan takes | 200 XP, 75 gold |

Commands in the guild hall: `read board`, `accept <quest>`, `report` (hand in whatever is complete).

Kills count only if they happened while the quest was active or before it was accepted; either way a player who has killed the Overseer can complete the quest. The orders must be the item itself.

**The ending.** When Rowan takes the orders he reads the fragments aloud, and his closing speech makes the five reveals from the brief: Vincent found something beneath the village; a complex called "The Hollow Below" lies under it; the Overseer took orders from someone called "The Crow"; Vincent's disappearance is probably connected; and there may be other entrances in the northern mountains. The player should feel the goblin problem is solved and a larger mystery has opened. Villagers get a changed line after the first quest is complete (Harland thanks the player).

## 11. Area notes that the brief leaves open

- The Traveling Merchant is a fixed NPC in the Village Square and also a possible encounter on the North Road, as the brief lists him in both. They are two separate merchants; nothing coordinates them.
- North Road has an exit north in prose only, and the brief's exit list names only south. `north` is not listed among the obvious exits, but typing it gives a message that the wilderness road is not yet open.
- Tavern Cellar: the brief asks for 2-3 Large Rats; spawn 3.
- Guard Post: of the two Goblin Guards, one is a variant that always drops the Rusty Key.
- Hidden Cache: the north exit of the Mushroom Cavern is locked with the Rusty Key. Its loot is placed at creation and is not restocked by reset; it returns only when the driver restarts.
- Storage Chamber's potion is restocked by reset.
- Guest Rooms: safe, logout room.
- The village rooms are safe except the North Road (the wolf) and the Tavern Cellar.
- Dungeon rooms get a short name from their heading in the brief ("Collapsed Passage").
- The brief's descriptions are used word for word as each room's long description. Builders add details (things to look at), ambient emotes and extra NPC chat in keeping with them, but do not replace or contradict the given text. Dialogue quoted in the brief is used word for word as the NPC's first lines.

### Differences from the first design

- Extra room helpers: `BlockExit`, `MoveThrough`, `CanUseExit`, `LongFor(viewer)`, `Stat()`; `AddReadable` has an optional third argument.
- `Npc`'s constructor calls `EnableCommands`; a builder does not. The driver change that makes this safe is in DRIVER_API.md.
- Rowan offers the second quest automatically when the first is completed, and `report` also completes a quest that is finished but was never accepted. The quest log shows the title that the quest giver passed to `SetQuest(id, state, title)` ("Clear the Cellar", "Recover the Overseer's Orders"); without a title the id is made readable.
- Spawned creatures get an `OnReset` hook (used by the shopkeeper's rare stock).
- The `update` command rebuilds only interactive occupants of the room, so NPCs are not duplicated.
- Monsters do not regenerate. The Overseer is aggressive. The Traveling Merchant is built.
- There are no rounds, so there is no acting order: every fighter has its own ready time. `kill <name>` picks the first match in the room, in the order the room lists things; if the player is already fighting, `kill <other>` turns on the other (it is attacked first from then on). There is no numbering (`kill 2.goblin`).
- Combat details: one action per ready time; fleeing is not an action. While fighting, `get`, `loot` and ordinary movement are refused with a pointer to `flee`.
- **Shared experience.** The damage each player deals to a creature is recorded, capped at the Vitality actually lost. Eligible players are those with recorded damage who are alive, in the room and not idle. `Living.KillingBlowPercent` (10%) goes to the player who struck the killing blow if that player is eligible and dealt at least `KillingBlowFairPercent`/N (85%/N) of the eligible damage, N being the number of eligible players; otherwise everything is split by damage. The rest is split by damage. Shares are rounded down, minimum 1. A lone killer gets 100%. All eligible players get `NotifyKill` and may loot the corpse during its lock; the Chronicle report names the killing-blow player as actor and all of them as participants (wizards are left out of history). A killing blow by a non-player gives no bonus.
- **Numbered targets.** Every command that picks a thing by name accepts a number: `rat 2`, `2.rat`, `second rat` (first to tenth). Implemented once in `NameIndex` (API assembly), used by `Kernel.Present` and by `MudObject.SplitOrdinal` in the mudlib.
- **Lists.** `inventory` prints a heading and one item per line; identical items collapse to `a minor healing potion (x2)`; worn and wielded items are listed apart. Lists inside sentences (shop sale, corpse contents, floor) use the same form.
- **Idle and timeout.** Driver settings `--idle-minutes` / `LESMUDOS_IDLE_MINUTES` (default 5) and `--timeout-minutes` / `LESMUDOS_TIMEOUT_MINUTES` (default 10). An idle player is marked in `who` and earns no experience. Two minutes before the timeout (or half of it, if shorter) a warning is printed; at the timeout the character is saved and disconnected as with `quit`. Wizards are exempt from the disconnect. Empty lines and telnet NOP do not count as activity.
- Corpses and the lost-property store were added after the first design (section 9).

## 12. Testing

- `tests/smoke_test.py` keeps testing the driver and wizard tools against `/domains/town`. Because new players now start in Vincent's Hollow, the smoke test patches the start room in its temporary copy of the mudlib. Its existing checks must keep passing.
- `tests/area_test.py` is a new end-to-end test for the gameplay systems and the area: movement through every exit of the area, look/read on the brief's objects, each NPC's quoted dialogue, buying and equipping, a real fight with a weak creature, corpses and looting, death and respawn, a driver restart and a crash with the lost-property store, the locked cache, the boss abilities (using `smite` and `heal` where luck would make the test flaky), both quests through to the ending, and persistence of the character across logout.
- A simulation script, `tests/balance_sim.py`, plays the combat rules on an event clock outside the driver (the same delay formula as the mudlib; `python3 tests/balance_sim.py check` parses the mudlib files and fails if the numbers differ) with the numbers from this document and reports, for a solo player with sensible purchases, the win rate and average Vitality lost per room. Target: the area is completable solo by level 5 with potions and rests, and the boss is dangerous but beatable (roughly 50-75% for a level 4-5 player in the best village gear).
- `tests/playthrough_test.py` (port 4813, `LESMUDOS_TEST_PORT`) is the first-player test. It copies the mudlib unmodified (no stub rooms), makes a brand-new non-wizard level 1 character and plays the whole area: every exit of all 18 rooms in both directions, the sign, plaque and quest board, the brief's objects, every NPC's quoted lines word for word, the shop and the tavern (including the bed and logging out in the Guest Rooms), a deliberate death with corpse recovery and the locked-corpse message for a second player, then both quests with a fight loop (flee, rest, bed, potions, looting, the Hidden Cache, levelling) up to the Goblin Overseer, `report`, the rewards and the ending speech. A warden wizard account is used only to peek at state; if the solo character cannot finish, the test records a finding and falls back to `heal`, clearly labelled. It takes 15 to 20 minutes.
- `tests/timing_test.py` (port 4815) measures real intervals between combat actions against the formula (design section 6): unarmed, sword, shield, haste, slow, fast and slow creatures, the first-action delay of creatures drawn in, the Overseer's pattern and its Power Strike warning, the potion queue and `stop`, and lifetime cases (spamming `kill`, dying with a potion queued, a target quitting during a wind-up).
- `tests/world_test.py` (port 4823) tests the world features of section 3a: hidden exits (both ways of finding them, who sees what, saved across logout, a clean restart and a kill), `search` and its timing, danger bands, patrols (route, there and back, loop, real exits only, talk pause, defending, dying and returning, dormancy), nodes, the wizard sandbox, the docs' examples and Elder Lane in Oakenfell.
- `tests/find_test.py` (port 4827) tests finds (section 9, Finds): the exact amounts, once per character, wizards, items and keepsakes, the sandbox and the trusted-caller checks, the register on disk through a quit, a killed driver, a clean restart and `update`, the example of BUILDING.md, and the five converted places of the Corridor with real characters.
- `tests/city_test.py` walks Oakenfell (the city and the two remaining Vale Road rooms): the map against `docs/areas/oakenfell-build.md`, every exit both ways, details, readables, people, topics and shops, and a new character brought to the Southgate Approach who walks north to the Hall of Callings.
- `tests/corridor_test.py` (port 4828) walks the Greenvale Transit Corridor: the 100 road rooms against `docs/areas/transit-corridor-build.md`, a wizard through every exit of all 122 rooms both ways, the seven hidden ways (not listed and not walkable, found by looking at their detail, then walkable both ways), the Chronicle's reports for the five secret rooms and two dungeons (for a character, not for a wizard), the 29 resource nodes, the five Riverwatch patrols (at the start of the route; walking when a player stands on it), danger bands in `stat here` and the warning for a level 5 character, the creatures each zone may hold, every detail, readable, person, topic and shop, and a new character who walks the whole road from the Village Square to Oakenfell's Southgate. The aggression of the corridor's creatures is switched off in the test's own copy of the mudlib. `python3 tests/corridor_test.py static` runs the checks that need no game.
- `tests/craft_test.py` (port 4829) tests crafting and gathering end to end (section 9a, CRAFTING.md section 8); `tests/craft_sim.py` checks the economy and the odds without a driver.
- `tests/rest_test.py` (port 4835, about 4 minutes) tests `rest` (section 9): the commands and what the room sees, each thing that ends resting and each that does not, the rate for each resource (standing, seated, and seated in a safe room), the refusal in a fight, the surprise delay (measured against a standing character), "fully rested" once, the idle timer, wizards and creatures, and probe objects (a trusted one and a wizard's) that try every public door to resting.
- `tests/load_check.py` (port 4821) compiles and loads every mudlib file.
- `tests/comm_test.py` (port 4832, about 5 minutes) tests the communication commands (section 15): every command and block, the flood limits, cleaning, channel membership for players and for wizards of level 1 and 10, `reply`, invisible wizards, saving of the settings, and a probe object written like a low-level wizard's that tries to post to `sysop`.
- `tests/ansi_test.py` (port 4814) tests colour: rendering, `ansi off`, persistence, raw `cat` and editor output, escaping of player input, unclosed tags, shop columns and what a non-player object is told (section 14). `Client` in `smoke_test.py` strips ANSI sequences by default so that all other tests read plain text; `Client.raw` and `raw_command()` give the bytes.

## 13. What was verified by play

The area was played from a fresh level 1 character, solo, with village gear only. Two consecutive clean runs (294 checks each): 9.0 and 12.4 minutes, one deliberate death each, final level 5, no wizard help. The Overseer's door was reached at level 4 after 8.0 and 7.3 minutes and the boss took 2 to 3 engagements with 2 retreats. Natural deaths: none in the recorded runs.

Bugs and clumsy points that only play showed (fixed):

- `BlockExit` did not work: typing `north` on the North Road answered "What?" because the blocked direction was never bound as a command. It now gives the room's refusal text and is not listed among the exits.
- Creatures and some items had lower-case names ("giant rat"), which read badly in combat, corpse and death messages next to capitalised NPC names. Names are now capitalised ("Giant Rat", "Village Guard", "Traveling Merchant"); ids stay lower case.
- Quest log titles came out as readable ids ("Overseers orders"). `SetQuest` takes an optional title and the log shows "Clear the Cellar" and "Recover the Overseer's Orders".
- Harland's changed thanks line after the first quest was missing; `AddChatAfterQuest` adds it.
- The guard's stockade topic named the wrong place.
- `kill <name>` during a fight was ignored; it now turns on the named enemy.

Behaviour that was examined and left as it is:

- Targeting is by first match with no numbering. A Village Guard and a Goblin Guard (and a Large Rat and a Giant Rat) are never in the same room, so `kill guard` and `kill rat` are unambiguous in this area. In a room with two Goblin Warriors the first listed is attacked, and `kill goblin` while fighting turns to the next.
- Late text after a prompt is not an ordering bug: a command's own output is always queued before its "> ". What arrives afterwards is asynchronous by design (NPC emotes, ambushes, combat actions, other players' actions). See DESIGN.md section 7.
- The balance simulation matches the code once the minion's timing and the level-up heal mid-fight were corrected in the script. It predicts a Level 4-5 player with a longsword and two potions beats the Overseer 57-78% of the time; the real runs agree (two to three engagements). No numbers in the area were changed.

Not verified: the Traveling Merchant's North Road appearance and the wolf are random and seen only by chance in some runs; the rare longsword is a 30% roll; recovery after a natural death was only tested through the deliberate death.

## 14. Colour and command highlighting

The point of the feature: wherever the game says what the player can type, that text is shown in a colour, so a new player can see what is a command and what is only description.

**Markup.** Game text may contain tags in braces: `{cmd}` ... `{/}` (a command the player can type; bold bright cyan, `ESC[1;96m`), `{title}` (a room name; bold, `ESC[1m`), the plain styles `{b}`, `{u}`, `{red}` `{green}` `{yellow}` `{blue}` `{magenta}` `{cyan}` `{white}` and the bright forms `{bred}` to `{bwhite}`, and `{/}` (reset, `ESC[0m`). `{{` is a literal `{`. Any other brace sequence (`{0}`, `{foo}`, C# source) is left exactly as written. The builder rules are in [BUILDING.md](BUILDING.md#colour-and-command-highlighting).

**Where it is rendered.** In one place: `Player.CatchTell` in `/std/player`, which every message to a player passes through (`Write`, `Say`, `TellRoom`, `TellObject`, and, since the driver change below, the `NotifyFail` text and `What?`). The renderer is `Thing.Markup(text, colour)` in `/std/thing`. With colour on, known tags become ANSI sequences; with colour off they are removed. In both cases `{{` becomes `{` and unknown sequences are kept. A style still open at the end of a line gets an `ESC[0m` before the newline (and at the end of a message that has no newline), so colour cannot leak into later lines or into the prompt. The existing message counter in `CatchTell` (used by `delete`) is unchanged and counts the same messages as before, plus the `What?` and `NotifyFail` texts, which now pass through `CatchTell`.

**Non-players.** NPCs, monsters and the login object are not rendered for: they get the text with the tags as written. Nothing in the mudlib reads such text. Tags are never put in `Name`, `Ids` or `ShortDesc`; the shop's `browse` pads its columns by the width of the visible text (`Markup(text, false)`) in case a name does carry one. The login dialogue runs before a player body exists and stays plain text.

**Setting.** `ansi` shows the setting, `ansi on` and `ansi off` change it; `color` and `colour` are aliases of the command. It is a `[Saved]` field of the player (`colour`) that defaults to on, so new characters and characters saved before the setting existed have colour. It is listed in `help`.

**Raw output.** `cat` and the line editor's `p` show file text exactly as stored. They send through the private `WriteRaw` of `/std/player`, which sets a private flag around the `Write` so that `CatchTell` passes the text on unrendered; nothing outside the player body can set it. Everything else, including `list`, whose names are escaped, is rendered.

**Text typed by players.** A player must not be able to colour their own speech or put tags in front of others. Every piece of player input that is shown or stored is passed through `Thing.Esc`, which turns `{` into `{{`, before it enters game text; the renderer shows `{{` as `{`, so the text appears as typed. This is done for `say` (to the speaker and to the room), `title` (escaped wherever `Short()` or `Long()` show it, so an old save is covered too), `look at X` and `read X` failures, `get X from corpse`, the shop's "does not sell 'X'", and for every wizard message that repeats a typed path, file name or player name (`cd`, `list`, `cat`, `mkdir`, `copy`, `move`, `rename`, `delete`, `rmdir`, `edit`, `update`, `clone`, `promote`, `goto`) and the driver's exception messages they show. The talk/ask commands echo nothing the player typed (NPC and topic names come from the builders). Raw ESC bytes cannot be typed at all: the telnet reader drops every byte below 32 except tab (`TelnetServer.ReadLinesAsync`), so a player cannot send `ESC[31m`; the test types one and sees `[31m` arrive as plain characters.

**Driver change.** `Kernel.Command` used to write the `NotifyFail` message straight to the connection. It now calls `TellObject` on the player, so it reaches `CatchTell` like all other text. The prompt and the driver's `***` error lines still go straight to the connection and are never rendered, so they show tags as typed.

**Clients.** Colour is on by default and is plain ANSI SGR (no 256-colour or truecolour codes). A client that does not understand ANSI shows `ESC[1;96m` as junk; such a player types `ansi off`. Nothing is negotiated with the client (no TTYPE or MTTS detection).

## 15. Communication

Design: [specs/communication.md](specs/communication.md). The commands are run by two daemons, `/secure/comm` (say, tell, reply, whisper, shout, and the cleaning of text) and `/secure/channels` (channels). The player body binds the verbs and keeps what the character chose; all the player-body code is in one region at the end of `/std/player`.

| Command | What it does |
|---|---|
| `say TEXT`, `' TEXT` | `You say: ...`; everything living in the room gets `{name} says: ...` (NPC hooks as before). The `'` form needs a space after the quote: the driver takes the first word as the verb. |
| `tell PLAYER TEXT` | `You tell {name}: ...` / `{name} tells you: ...`, from anywhere. The same answer, "No one by that name is in the world.", for a name that does not exist, a player who is not logged in and a wizard the sender cannot see. |
| `reply TEXT` | A tell to the last player who told you something (kept only in memory). |
| `whisper TEXT to NAME`, `whisper NAME TEXT` | The target, who must be in the room, gets the words; the others in the room see `{name} whispers something to {target}.` |
| `shout TEXT` | Everyone in the world hears `{name} shouts: ...`. `shout off` / `shout on` stop and restart hearing (saved). |
| `shout CHANNEL TEXT`, `CHANNEL TEXT` | A message on a channel, `[wizard] {name}: ...`, when the first word is a channel the speaker belongs to; otherwise `shout` is an ordinary shout. |
| `channels`, `channel NAME on/off/join/leave` | The channels you belong to; switch one for yourself (saved); join or leave a channel whose file allows it. |
| `ignore [PLAYER]`, `unignore PLAYER` | Saved list. Tells and whispers from an ignored player are not delivered (the sender is told "{name} is not listening to you."); their shouts and channel messages are not shown. |

**Cleaning.** `Comm.Clean`, for every command: line breaks and tabs become spaces; control characters and the invisible format characters (zero width, bidi overrides) are dropped; colour tags such as `{red}`, `{cmd}` and `{/}` are removed (repeatedly, so that `{r{red}ed}` leaves nothing); runs of blanks shrink to one; the text is cut to 300 characters. An empty result gets the usage line. Remaining braces are escaped with `Esc` when shown, so nothing a player types can start a tag. This changed `say`, which used to show tags as typed.

**Limits, in one place** (the constants at the top of `/secure/comm`): message length 300; one shout per 10 seconds; characters below level 2 cannot shout (they are told); one channel message per 2 seconds; at most 50 names ignored. Wizards (any level) are exempt from the shout and channel limits. Shouting also needs the character level only for players, not wizards.

**Blocks.** A room may set `NoTell` and `NoShout` (protected fields of `/std/room`; no room sets them). A room with `NoTell` lets no tell in or out; one with `NoShout` lets no shout leave and none be heard in it. A wizard at either end of a tell, or in the room for a shout, is not bound. `say` and `whisper` are not affected. A target who is idle (`QueryIdle()` on the body, when the game has an idle timer) gets the tell and the sender is told so.

**Channels.** A channel is a file `/channels/<id>.cs` whose class has `public string Definition()` returning lines `id=`, `title=`, `member_level=` (wizard level needed to belong), `speak_level=`, `can_mute=yes|no`, `can_join=yes|no`. Adding a channel is adding the file (the verbs for it are bound at the next login of each player). `wizard`: level 1 and above; `sysop`: level 10 only, and it cannot be switched off. Membership is read from `/secure/wizlist` for the character's name at every send and for every recipient of every message; it is never read from the player body, and nobody asks to join `wizard` or `sysop`. For a channel with `can_join`, a character also has to have joined (saved).

**Security.** Every method of `/secure/comm` and `/secure/channels` that acts for a player checks that `PreviousObject()` is the interactive body of the player whose command is running (and a `/std/player` clone), so another object, including one a wizard wrote, cannot post, tell or shout for anyone through them; it gets nothing back. What a body keeps (ignores, switches, the last teller) can only be read or changed by those two daemons. A character who does not belong to a channel gets the answer "There is no channel called 'x'." for it, word for word what they get for a channel that does not exist, and the channel's name is not a command for them (`What?`). The files in `/channels` need wizard level 10 to change (`ChangeRefusal` in `/std/player`), as they decide who may listen to what. `/secure/channels` may list `/channels` and nothing else (`ValidOperation("get-dir")`). Limits of the design: any object can still `TellObject` any text to any player, and a wizard of level 5 or more can still write code that does so; the services cannot stop that, only make sure that what they deliver is genuine.

**Names.** Players see the capitalised character name. A body that answers `QueryInvisible()` with true is shown as "Someone" to everyone who is not a wizard of at least its level, and cannot be targeted by `tell` or `whisper` by them. (No body is invisible yet.) Nothing here writes to the Chronicle or Renown, and tells and whispers are not logged.

**Not done.** A linkdead target ("{name} has lost their link."): a dropped connection destroys the body at once (`NetDead`), so there is none to find. When linkdead characters exist, `FindPlayer` in `/secure/comm` is the one place to teach about them.
