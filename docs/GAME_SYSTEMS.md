# Game systems design

The sample mudlib so far has rooms, items and wizard tools, but no game. This document specifies the gameplay layer needed for the first real area, [Vincent's Hollow](areas/vincents-hollow.md): living things, combat, equipment, NPC dialogue, shops, quests, spawning and encounters.

Everything here is mudlib code under `/std`. No driver change is expected. If an implementer finds one is needed, it must be small, generic, and recorded in DRIVER_API.md.

Status: design. Sections are marked as implemented when the code lands.

## 1. Principles

- **Classic and small.** Automatic round-based melee, levels 1 to 5, a handful of numbers per creature. No classes, skills, magic or parties yet.
- **Data in `Create()`.** An area builder writes a room, creature or item by setting fields in `Create()`, with no logic for the common cases. Logic is only needed for special behaviour (a boss ability, a quest giver).
- **Late-bound calls between objects.** Objects talk through `ObjectRef.Call`, as elsewhere in this mudlib, so everything survives `update`.
- **Game state is not a security boundary.** Any mudlib object can call `AddGold` on a player. That is the LPmud norm and is accepted here: builders are trusted. Wizard levels, passwords and files remain guarded as before and must not be weakened.

## 2. File layout

```
mudlib/std/
  thing.cs        (exists) name, ids, short, long
  item.cs         (exists) portable thing; gains Value and Weight-free pick-up hook
  weapon.cs       new: Item with damage
  armor.cs        new: Item with armor value and slot
  consumable.cs   new: Item that is eaten or drunk for an effect
  coins.cs        new: Item that turns into gold when picked up
  living.cs       new: abstract Thing with hit points, combat, equipment
  npc.cs          new: Living with dialogue; peaceful by default
  monster.cs      new: Npc that fights, gives experience and drops loot
  shopkeeper.cs   new: Npc that sells from a stock list and buys items back
  adventurer.cs   new: abstract Living with the player's gameplay commands and saved character
  player.cs       (exists) now inherits adventurer; keeps login, wizard and file commands
  room.cs         (exists) gains details, readable text, spawns, encounters, locked exits, safe flag, reset
mudlib/domains/hollow/
  village/        the 8 village rooms
  cellars/        the 10 dungeon rooms
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
| `AddReadable(string names, string text)` | Text shown by `read <name>`. Also registers a detail if none exists for that name. |
| `AddSpawn(string path, int count = 1)` | Creatures or items this room should contain. Cloned in at creation and topped up at each reset: the room keeps the handles it spawned and replaces the ones that are no longer alive or, for items, no longer in the room. |
| `AddEncounter(string path, int percent)` | Each time a player walks in, each encounter has `percent` chance to appear, if none spawned by this room is still alive and at least 60 seconds have passed since the last one. Encounter creatures that are not in combat leave ("wanders off") at the next reset. |
| `LockExit(string direction, string keyId, string refusal)` | The exit can be used only by someone carrying an object whose `Id(keyId)` is true. Others get `refusal`. The key is not consumed. |
| `Safe` | No combat can start or continue here. |
| `ResetSeconds` | Default 300. Each room re-arms a call-out to its own `Reset()`. `Reset()` tops up spawns; override it (and call `base.Reset()`) for anything else. |

Public functions for other objects: `QueryDetail(name)`, `QueryReadable(name)`, `QuerySafe()`.

`Long()` for a room lists, after the description and exits, the visible objects. Creatures and items are shown as today (`A ginger tavern cat is here.`); several identical shorts are merged (`Two giant rats are here.`).

Exit names may be any single word (`north`, `up`, `down`). The one-letter abbreviation rule stays, with `u` and `d` for up and down.

## 4. Living things

`Living` (abstract, inherits `Thing`) holds:

| Field | Meaning | Default |
|---|---|---|
| `Level` | 1 and up | 1 |
| `MaxHp`, `Hp` | Hit points | 10 |
| `DamageMin`, `DamageMax` | Unarmed or natural attack | 1, 2 |
| `ToHit` | Bonus on attack rolls | 0 |
| `Armor` | Natural armor | 0 |
| `Gold` | Coins carried | 0 |

Derived values include equipment: `QueryDamage()` uses the wielded weapon if any, `QueryArmor()` adds worn armor, `QueryToHit()` adds the weapon's bonus.

Equipment slots: `weapon`, `shield`, `body`, `head`, `trinket`. One item per slot. An item leaves its slot automatically when it leaves the wearer's inventory.

### Combat

- `Living.StartFight(ObjectRef enemy)` adds the enemy to the attacker's enemy list and the attacker to the enemy's. Refused if either is dead, they are in different rooms, the room is safe, or the target is peaceful (`QueryPeaceful()`, true for villagers).
- Each heartbeat (2 seconds) a living thing with enemies present in its room makes one attack against its current target: the first enemy still alive and present.
- **To hit:** `d20 + attacker level + QueryToHit() >= 10 + defender QueryArmor()`. A natural 20 always hits, a natural 1 always misses.
- **Damage:** uniform in the attack's damage range, minimum 1.
- Enemies that are no longer in the room are dropped from the list after 30 seconds, so walking away ends a fight, and a creature does not chase.
- Messages: the attacker, the defender and bystanders each get a line, varying by damage (miss, scratch, hit, hard hit). Keep each round to one line per attack per observer.
- `Living.Die(ObjectRef? killer)` is virtual. See monsters and players below.
- `Living.Heal(int amount)`, `Living.TakeDamage(int amount, ObjectRef? from)`.

### Hooks for special behaviour

- `protected virtual void CombatRound(ObjectRef target)`: called once per round before the normal attack. Return value none; a subclass may set `SkipAttack` for the round. Used for boss abilities.
- `protected virtual int ModifyDamage(int damage, ObjectRef target)`: last word on outgoing damage.

## 5. NPCs

`Npc` (inherits `Living`):

- `Peaceful = true` by default: cannot be attacked ("... is no threat to you" style refusal).
- `AddChat(string line)`: what the NPC says to `talk <npc>` (alias `talk to <npc>`, `greet <npc>`). Lines are given in order; each `talk` gives the next line and wraps round. The first line added is the NPC's main dialogue.
- `AddTopic(string keywords, string line)`: reply to `ask <npc> about <keyword>`. Unknown topics get a shrug line.
- `AddEmote(string line, int percent)`: idle action shown to the room on heartbeat, only while a player is present. The NPC turns its own heartbeat off when the room has no players.
- Speech is shown as `Harland says: "..."`.

## 6. Monsters

`Monster` (inherits `Npc`, `Peaceful = false`):

| Field | Meaning |
|---|---|
| `Aggressive` | Attacks any player who enters, after a one-round pause. |
| `XpValue` | Experience for the kill. |
| `GoldMin`, `GoldMax` | Coins given to the killer. |
| `AddLoot(string path, int percent)` | Items that may drop to the floor on death. |
| `Assist` | Default true: joins a fight when another monster in the room is attacked. |

On death: a death line for the room, experience and gold to the killer (the one whose attack killed it), loot cloned into the room, `killer.Call("NotifyKill", Self.ProgramPath)` so quests can react, then the monster is destructed. The room's reset brings it back.

### Creature numbers for the Hollow

| Creature | Level | HP | Damage | To hit | Armor | XP | Gold | Notes |
|---|---|---|---|---|---|---|---|---|
| Large Rat | 1 | 6 | 1-2 | 0 | 0 | 8 | 0 | Aggressive |
| Giant Rat | 1 | 10 | 1-3 | 0 | 0 | 12 | 0 | Aggressive |
| Wandering Wolf | 2 | 16 | 2-4 | 1 | 1 | 25 | 0 | Aggressive; encounter |
| Goblin Scavenger | 2 | 10 | 2-4 | 2 | 1 | 22 | 1-6 | Fast but fragile |
| Spore Bat | 2 | 8 | 1-3 | 1 | 4 | 20 | 0 | Evasive: high armor, low HP |
| Goblin Guard | 2 | 20 | 2-4 | 0 | 3 | 32 | 2-8 | Defensive; one carries the Rusty Key |
| Giant Cave Toad | 3 | 40 | 2-5 | 0 | 0 | 50 | 0 | High health |
| Goblin Warrior | 3 | 22 | 3-5 | 1 | 2 | 42 | 3-10 | Balanced |
| Goblin Minion | 2 | 12 | 2-4 | 0 | 1 | 15 | 0 | Summoned by the boss only |
| Goblin Elite Guard | 4 | 38 | 3-6 | 2 | 3 | 85 | 8-16 | Mini-boss |
| Goblin Overseer | 5 | 75 | 4-7 | 2 | 3 | 250 | 0 | Boss; see below |

Goblin Overseer abilities:

- **Battle Cry:** on the first round of a fight, instead of attacking. For the next 5 rounds his damage is +2. Once per fight.
- **Power Strike:** each round, 20% chance that the attack does double damage, with its own message.
- **Summon Goblin Minion:** the first time his HP falls to half or below, one Goblin Minion is cloned into the room and joins the fight. Once per life.

These numbers are a starting point. The implementer checks them with a short simulation (section 12) and may adjust within reason, recording the final values here.

## 7. Items

`Item` gains `Value` (gold) and `protected virtual void OnGet(ObjectRef taker)`, called by the `get` command after the move.

| Class | Fields | Commands |
|---|---|---|
| `Weapon` | `DamageMin`, `DamageMax`, `ToHit` | `wield <weapon>`, `unwield` |
| `Armor` | `ArmorValue`, `Slot`, optional `MaxHpBonus` | `wear <item>`, `remove <item>` |
| `Consumable` | `HealAmount`, `UseVerb` (`eat`, `drink`), `UseMessage` | `eat <item>`, `drink <item>` (also `quaff`) |
| `Coins` | `Amount` (or `AmountMin`/`AmountMax` rolled in `Create`) | Picking it up adds the gold and destructs the item |

### Equipment for the Hollow

| Item | Type | Numbers | Value | Source |
|---|---|---|---|---|
| Rusty Sword | weapon | 2-5 | 10 | Ironoak Arms |
| Training Spear | weapon | 2-4, to hit +1 | 8 | Ironoak Arms |
| Iron Dagger | weapon | 1-4, to hit +1 | 6 | Ironoak Arms |
| Woodcutter Axe | weapon | 3-6 | 18 | Ironoak Arms |
| Small Shield | armor, shield | 1 | 12 | Ironoak Arms |
| Fine Iron Longsword | weapon | 4-8 | 60 | Ironoak Arms, rare |
| Guild Initiate's Helm | armor, head | 1 | 15 | Reward, Clear the Cellar |
| Goblin Overseer's Axe | weapon | 5-9 | 70 | Boss, always |
| Leather Commander's Vest | armor, body | 3 | 60 | Boss, always |
| Bag of Coins | coins | 60-100 | | Boss, always |
| Overseer's Orders | quest item | readable | 0 | Boss, always |
| Rusty Key | key | | 0 | One Goblin Guard, always |
| Minor Healing Potion | consumable, drink | heals 15 | 15 | Storage Chamber; merchant |
| Magical Trinket | armor, trinket | max HP +5 | 40 | Hidden Cache, always |
| Rare Enchanted Dagger | weapon | 3-6, to hit +2 | 80 | Hidden Cache, 25% |
| Random Gold | coins | 30-60 | | Hidden Cache, always |

## 8. Shops and services

`Shopkeeper` (inherits `Npc`):

- `AddStock(string path, int price)` and `AddRareStock(string path, int price, int percent)`. Rare stock is rolled when the shopkeeper is created, so it changes at reset of its room.
- Commands offered to players in the room: `browse` (what is for sale, with prices), `buy <item>`, `sell <item>` (pays half the item's `Value`; refuses value 0 and quest items), `value <item>`.
- Stock is unlimited; each purchase clones a new item into the buyer's inventory.
- The command is `browse`, not `list`, because `list` is the wizard directory command.

Services use the same class with a different stock:

- **Harland the Innkeeper** sells food and drink (consumables) and a bed. Buying the bed restores the buyer to full HP and takes them upstairs to the Guest Rooms.
- **Traveling Merchant** sells Minor Healing Potions and trail rations.

`Shopkeeper` has `protected virtual bool OnBuy(ObjectRef buyer, string path)` so a service can do something other than hand over an item.

## 9. The player character

`Adventurer` (abstract, inherits `Living`) carries everything about playing the game; `Player` inherits it and keeps accounts, wizard tools and files.

Saved with the player: level, experience, HP, gold, quest states, the program paths of carried items and which are equipped, and the logout room.

- **New character:** level 1, 30 HP, 25 gold, unarmed 1-3.
- **Experience to reach level:** 2: 100, 3: 250, 4: 500, 5: 900. Level 5 is the cap for now.
- **Each level:** +8 max HP, and full heal on levelling. (The +1 to hit comes from level in the attack roll.)
- **Regeneration:** 1 HP every 10 seconds out of combat; 3 in a safe room.
- **Death:** the player is told so, loses 10% of their gold, and wakes in the Guest Rooms at full HP. No experience or items are lost. Their killer forgets them.
- **Logout room:** quitting in a room whose `QueryLogoutRoom()` is true (the Guest Rooms) makes that the next login room. Otherwise players enter at the Village Square.
- **Inventory persistence:** on save, the paths of carried items are stored and the items are destructed with the player on quit; on login they are cloned again. Per-item state is not kept. Items no longer drop to the floor on quit.

Commands, available to every player:

| Command | Effect |
|---|---|
| `kill <target>` (also `attack`) | Start a fight |
| `flee` | Leave by a random exit; fails 25% of the time |
| `consider <target>` | Rough comparison of levels |
| `wield`, `unwield`, `wear`, `remove` | Equipment |
| `eat`, `drink`, `quaff` | Consumables |
| `talk <npc>`, `ask <npc> about <topic>` | Dialogue |
| `read <thing>` | Readable details and readable items |
| `score` (also `sc`) | Level, experience and what the next level needs, HP, gold, equipment, quests |
| `hp` | One-line health |
| `quests` | Quest log |

`look at <thing>` falls back to room details, and `look at <creature>` adds a rough health line.

Public functions on the player for other objects: `QueryLevel`, `QueryHp`, `QueryMaxHp`, `QueryGold`, `AddGold(int)` (negative to take; refuses to go below zero and returns false), `AddXp(int)`, `QueryQuest(string id)`, `SetQuest(string id, int state)`, `NotifyKill(string programPath)`, `QueryKilled(string programPath)`.

### Wizard tools for builders

| Command | Level | Effect |
|---|---|---|
| `goto <room path>` | 1 | Move to a room |
| `stat <thing>` | 1 | Show a living thing's or item's numbers |
| `heal [<target>]` | 5 | Restore full HP |
| `smite <target>` | 10 | Kill a creature at once, with normal death processing and kill credit |

These follow the existing rule: below the level, the command answers "What?".

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
- North Road has an exit north in prose only. `north` gives a message that the wilderness road is not yet open.
- Tavern Cellar: the brief asks for 2-3 Large Rats; spawn 3.
- Guard Post: of the two Goblin Guards, one is a variant that always drops the Rusty Key.
- Hidden Cache: the north exit of the Mushroom Cavern is locked with the Rusty Key. Its loot is placed at creation and is not restocked by reset; it returns only when the driver restarts.
- Storage Chamber's potion is restocked by reset.
- Guest Rooms: safe, logout room.
- The village rooms are safe except the North Road (the wolf) and the Tavern Cellar.
- Dungeon rooms get a short name from their heading in the brief ("Collapsed Passage").
- The brief's descriptions are used word for word as each room's long description. Builders add details (things to look at), ambient emotes and extra NPC chat in keeping with them, but do not replace or contradict the given text. Dialogue quoted in the brief is used word for word as the NPC's first lines.

## 12. Testing

- `tests/smoke_test.py` keeps testing the driver and wizard tools against `/domains/town`. Because new players now start in Vincent's Hollow, the smoke test patches the start room in its temporary copy of the mudlib. Its existing checks must keep passing.
- `tests/area_test.py` is a new end-to-end test for the gameplay systems and the area: movement through every exit of the area, look/read on the brief's objects, each NPC's quoted dialogue, buying and equipping, a real fight with a weak creature, death and respawn, the locked cache, the boss abilities (using `smite` and `heal` where luck would make the test flaky), both quests through to the ending, and persistence of the character across logout.
- A simulation script, `tests/balance_sim.py`, plays the combat rules outside the driver with the numbers from this document and reports, for a solo player with sensible purchases, the win rate and average HP lost per room. Target: the area is completable solo by level 5 with potions and rests, and the boss is dangerous but beatable (roughly 50-75% for a level 4-5 player in the best village gear).
