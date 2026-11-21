# Area specification: Vincent's Hollow

This is the area brief as supplied by the project owner. It is the source of truth for names, descriptions, dialogue, exits and contents. Mechanics it leaves open (numbers, commands) are fixed in [GAME_SYSTEMS.md](../GAME_SYSTEMS.md).

> Design and implement the following LP MUD area exactly as described. Focus on immersive room descriptions, atmospheric details, NPCs, environmental storytelling, and classic fantasy gameplay. This is a starter area designed to introduce players to the world while establishing a larger mystery that can drive future content.

## AREA: VINCENT'S HOLLOW

Vincent's Hollow is a small frontier village founded nearly seventy years ago by an explorer named Vincent Holloway. Seeking refuge from the dangers of the northern wilderness, he established a trading post that slowly grew into a thriving settlement. Though Vincent disappeared under mysterious circumstances decades ago, the village still bears his name and many locals claim his spirit watches over the community.

Traders, woodsmen, miners, and adventurers use Vincent's Hollow as the last safe stop before venturing into the wild lands beyond.

Recommended player level: 1-5

Theme: Classic fantasy starter village with mystery, history, and a dungeon hidden beneath the local tavern.

## Village layout

```
                 [North Road]
                      |
                 Village Gate
                      |
                Village Square
               /      |      \
      Guild Hall    Tavern    Weapon Shop
                      |
               Tavern Basement
                      |
                Dungeon Level
```

The village itself contains 8 rooms plus the dungeon.

### Room 1: North Road

Short: North Road

Description: A dusty road stretches north into the wilderness and south toward Vincent's Hollow. Wagon ruts and horse tracks mark the path used by traders and adventurers. Towering pines sway gently in the breeze while distant mountains dominate the northern skyline. A carved wooden sign welcomes travelers to the settlement.

Exits: south -> Village Gate

Objects: Village Sign

Sign text:

    "Welcome to Vincent's Hollow
    Founded by Vincent Holloway
    Shelter, Ale, and Honest Trade"

Random encounters: Traveling Merchant, Wandering Wolf

### Room 2: Village Gate

Short: Village Gate

Description: A timber palisade surrounds Vincent's Hollow. The village gates stand open during daylight hours, allowing visitors to come and go freely. Watchtowers overlook the road while faded banners flutter in the wind.

Exits: north -> North Road, south -> Village Square

NPC: Village Guard

Village Guard dialogue: "If you're lookin' for glory, ask old Harland at the tavern. The cellar's got troubles no one's managed to solve."

Additional dialogue: "Some folk say Vincent still watches over us. I just know strange things have been happening underground."

### Room 3: Village Square

Short: Village Square

Description: The center of Vincent's Hollow bustles with life. A large stone well marks the center of the square while merchants display goods from temporary stalls. Locals gather around benches exchanging news while children dart between travelers.

Exits: north -> Village Gate, west -> Adventurers Guild, east -> Ironoak Arms, south -> Hollowman's Rest

Objects: Stone Well, Bench

Plaque text: "This well was dug by Vincent Holloway and his companions during the settlement's first winter."

NPCs: Old Farmer Benson, Traveling Merchant

Old Farmer Benson: provides local lore. Dialogue: "Vincent founded this place. Brave man... but he went lookin' for somethin' beneath the earth and never returned."

### Room 4: Adventurers Guild

Short: Adventurers Guild

Description: Monster trophies, old banners, and battered shields decorate the walls of this busy guild hall. Adventurers gather around tables discussing rumors, contracts, and treasure maps. A large notice board dominates one wall.

Exits: east -> Village Square

Objects: Quest Board

NPC: Guildmaster Rowan

Guildmaster dialogue: "Every village has its legends. Ours is Vincent himself. Some believe he found something beneath these lands before he vanished."

Starter quest: Clear the Cellar. Objective: Defeat the Goblin Overseer beneath the tavern. Rewards: Experience, Gold, Starter equipment reward.

Additional quest: Recover the Overseer's Orders.

### Room 5: Ironoak Arms

Short: Ironoak Arms

Description: The smell of hot metal and burning coal fills this small but impressive smithy. Weapon racks cover the walls while a roaring forge illuminates newly finished blades.

Exits: west -> Village Square

NPC: Blacksmith Torren

Dialogue: "Quality steel saves lives. Cheap steel fills graveyards."

Inventory: Rusty Sword, Training Spear, Iron Dagger, Woodcutter Axe, Small Shield

Rare inventory: Fine Iron Longsword

### Room 6: The Hollowman's Rest

Short: The Hollowman's Rest

Description: Warm firelight illuminates the village tavern. Travelers, adventurers, and locals enjoy food and drink while musicians play near the fireplace. Hanging above the hearth is a weathered portrait of Vincent Holloway himself. Though faded by age, his confident smile remains intact.

A sense of comfort fills the room, though whispers occasionally mention strange noises in the cellar below.

Exits: north -> Village Square, up -> Guest Rooms, down -> Tavern Cellar

NPCs: Harland the Innkeeper, Mara the Barmaid, Drunken Prospector

Harland dialogue: "Started with rats years ago. Then goblins moved in. Now something's organizing them."

Secondary dialogue: "Vincent used to talk about tunnels beneath the village. Nobody believed him."

Services: Food, Drink, Resting

### Room 7: Guest Rooms

Short: Guest Rooms

Description: Several modest guest rooms sit above the tavern. Clean beds and warm blankets provide a welcome refuge for weary travelers.

Exits: down -> The Hollowman's Rest

Function: Safe room, Logout room

### Room 8: Tavern Cellar

Short: Tavern Cellar

Description: Dusty barrels and crates fill the cellar beneath the tavern. Several have been torn apart and strange footprints cover the dirt floor. A rough opening has been smashed through the far wall.

Exits: up -> The Hollowman's Rest, east -> Forgotten Cellars

Objects: Broken Crates, Chewed Grain Sacks

NPCs: Large Rat (2-3)

## Dungeon: The Forgotten Cellars

Theme: Abandoned smuggling tunnels beneath Vincent's Hollow.

Recommended level: 2-5

Boss: Goblin Overseer

### Dungeon room 1: Collapsed Passage

Description: A narrow tunnel littered with loose stone and broken support beams.

Exits: west -> Tavern Cellar, east -> Storage Chamber

Mobs: 2 Giant Rats

### Dungeon room 2: Storage Chamber

Description: Rotting crates and abandoned trade goods are stacked against damp walls.

Exits: west -> Collapsed Passage, north -> Smuggler Hall

Mobs: 2 Goblin Scavengers

Treasure: Minor Healing Potion

### Dungeon room 3: Smuggler Hall

Description: A broad corridor once used by smugglers. Rusted lantern hooks remain attached to the walls.

Exits: south -> Storage Chamber, east -> Flooded Tunnel, west -> Guard Post

### Dungeon room 4: Guard Post

Description: Crude goblin barricades create a defensive position.

Exits: east -> Smuggler Hall

Mobs: 2 Goblin Guards

Loot: Rusty Key

### Dungeon room 5: Flooded Tunnel

Description: Ankle-deep water covers the floor while distant drips echo through darkness.

Exits: west -> Smuggler Hall, east -> Mushroom Cavern

Mobs: Giant Cave Toad

### Dungeon room 6: Mushroom Cavern

Description: Hundreds of bioluminescent mushrooms bathe the chamber in eerie blue light.

Exits: west -> Flooded Tunnel, north -> Hidden Cache, east -> Goblin Barracks

Mobs: 3 Spore Bats

### Dungeon room 7: Hidden Cache

Description: A concealed smuggler stash untouched for decades.

Exits: south -> Mushroom Cavern

Requirements: Rusty Key

Loot: Random Gold, Magical Trinket, Rare Enchanted Dagger

### Dungeon room 8: Goblin Barracks

Description: Crude bedding and stolen goods clutter this crowded chamber.

Exits: west -> Mushroom Cavern, east -> Overseer Hall

Mobs: 3 Goblin Warriors

### Dungeon room 9: Overseer Hall

Description: Animal skulls and trophies decorate this intimidating chamber.

Exits: west -> Goblin Barracks, east -> Overseer Chamber

Mobs: 2 Goblin Elite Guards

### Dungeon room 10: Overseer Chamber

Description: Unlike the rest of the dungeon, this room is surprisingly organized. Maps, notes, and excavation sketches cover a large wooden table. A strange symbol resembling a crow is repeatedly scrawled throughout the chamber.

Exits: west -> Overseer Hall

Boss: Goblin Overseer

Abilities: Battle Cry, Power Strike, Summon Goblin Minion (once)

Loot: Goblin Overseer's Axe, Leather Commander's Vest, Bag of Coins, Quest Item: Overseer's Orders

## Mobs

| Mob | Role |
|---|---|
| Large Rat | Weak starter enemy. |
| Goblin Scavenger | Fast but fragile. |
| Goblin Guard | Defensive melee fighter. |
| Giant Cave Toad | High health creature. |
| Spore Bat | Weak but evasive flying enemy. |
| Goblin Warrior | Balanced combatant. |
| Goblin Elite Guard | Powerful mini-boss enemy. |
| Goblin Overseer | Dungeon boss. |

(The Giant Rats of dungeon room 1 and the Wandering Wolf of the North Road are named in the room entries but not in this table.)

## Quest item: Overseer's Orders

Description: A bundle of poorly translated notes covered in strange symbols and instructions.

Key text fragments:

- "The Hollow Below"
- "The Key of Vincent"
- "Continue excavation."
- "The old founder was telling the truth."
- "The entrance remains sealed."
- "The Crow demands progress."

## Future expansion hooks

The Overseer's Orders reveal:

1. Vincent Holloway discovered something important beneath the village.
2. A hidden underground complex known as "The Hollow Below" exists somewhere beneath Vincent's Hollow.
3. The Goblin Overseer was receiving orders from a mysterious figure known only as "The Crow."
4. Vincent's disappearance may be connected to whatever lies beneath the village.
5. Additional entrances may exist in the northern mountains.

This area should act as the foundation of a larger campaign:

| Levels | Area |
|---|---|
| 1-5 | Vincent's Hollow, The Forgotten Cellars |
| 5-10 | The Hollow Below |
| 10-15 | Vincent's Lost Expedition |
| 15-20 | The Mystery of The Crow |

The ending should leave players feeling they have solved the village's immediate goblin problem while uncovering the first clues in a much larger mystery surrounding Vincent Holloway's disappearance.

## The village expansion (November 2026)

The brief above is the original village. The expansion of [specs/hollow-expansion.md](../specs/hollow-expansion.md) adds six rooms, five people, eleven weapons, four pieces of armour, a quest and a training dummy. The files are in `mudlib/domains/hollow/` (`village/`, `npc/`, `obj/`, `mob/`); the test is `tests/hollow_expansion_test.py`.

### Rooms

| Room | File | Reached by | What is there |
|---|---|---|---|
| Fletcher's Lane | `village/fletchers_lane` | `lane` from the Village Square | The street that joins the new shops. Exits: `square`, `yew`, `loft`, `shrine`, `yard` |
| The Strung Yew | `village/strung_yew` | `yew` from the lane (back: `out`) | Wenna Farrow, bowyer. Sells the Yew Longbow |
| The Armourer's Shed | `village/armourers_shed` | `shed` from Ironoak Arms (back: `smithy`) | Oswy Thackett, armourer. Sells the three armour pieces |
| The Shrine of the Quiet Hand | `village/shrine` | `shrine` from the lane (back: `out`) | Sister Merewyn. The blessing (6 gold, full restoration); the quest The Quiet Offering |
| The Hedge-Scholar's Loft | `village/hedge_scholars_loft` | `loft` from the lane (back: `out`) | Halbert Quill, hedge-scholar. Sells the Ironshod Staff and the Scholar's Robe |
| The Practice Yard | `village/practice_yard` | `yard` from the lane (back: `lane`) or `yard` from the North Road (back: `road`) | Brann Odell, yard master; the training dummy; archery butts. Not a safe room, so that the dummy can be attacked |

The Practice Yard lies in the angle between the palisade and the North Road, with a postern door to the lane and a gate to the road. The square gained the exit `lane`, Ironoak Arms the exit `shed`, the North Road the exit `yard`, and the square's notice post a line that names the new places.

### People

| Name | Trade | Sells |
|---|---|---|
| Wenna Farrow | Bowyer | Yew Longbow 60 |
| Oswy Thackett | Armourer | Padded Jerkin 20, Leather Jerkin 50, Leather Cap 25 |
| Sister Merewyn | Shrine keeper | The Quiet Hand's blessing 6 (restores Vitality, Endurance and Focus at once; not an item) |
| Halbert Quill | Hedge-scholar | Ironshod Staff 55, Scholar's Robe 40 |
| Brann Odell | Yard master | nothing |

Torren (the smith) keeps the starters and now also sells the other improved weapons: Felling Axe 65, Flanged Mace 55, Boar Spear 50, Steel Dirk 45 (and, as before, the Fine Iron Longsword at 60 as a rare stock). The Traveling Merchant sells what he did. Every person has topics for the trade, the village, the cellars, a rumour, and teaching; the rank trainers (the Scout's, the Priest's, the Mage's, the Warrior's and Adventurer's) only talk of teaching as coming "soon".

### Weapons and armour

Quality applies to every one of these as to any item. Reaction modifier (RM), recovery (Rec) and to-hit (Hit) are those of the existing weapon of the same line, so a line's steps differ in damage only.

| Line | Step | Item | Damage | RM / Rec / Hit | Value | Got |
|---|---|---|---|---|---|---|
| Sword | improved | Fine Iron Longsword (existing) | 12-18 | 1.0 / 1.0 / 0 | 60 | Torren, rare stock |
| Sword | fine | Captain's Sabre | 14-20 | 1.0 / 1.0 / 0 | 90 | Goblin Elite Guard, 10 percent |
| Axe | improved | Felling Axe | 15-23 | 1.0 / 1.5 / 0 | 65 | Torren |
| Axe | fine | Goblin Overseer's Axe (existing) | 17-25 | 1.5 / 1.5 / 0 | 70 | the Overseer |
| Bow | improved | Yew Longbow | 11-16 | 0.5 / 1.0 / +1 | 60 | Wenna Farrow |
| Bow | fine | Warden's Recurve | 13-19 | 0.5 / 1.0 / +1 | 80 | the Hidden Cache, once per character |
| Mace | improved | Flanged Mace | 12-17 | 0.5 / 1.0 / 0 | 55 | Torren |
| Mace | fine | Shrine-Blessed Mace, +1 Spirit | 13-18 | 0.5 / 1.0 / 0 | 75 | The Quiet Offering |
| Staff | improved | Ironshod Staff, +1 Insight | 9-13 | 0.5 / 1.0 / +1 | 55 | Halbert Quill |
| Staff | fine | Staff of the Hollow Oak, +2 Insight | 10-14 | 0.5 / 1.0 / +1 | 90 | the Mushroom Cavern, once per character |
| Spear | improved | Boar Spear | 11-16 | 0.5 / 1.0 / +1 | 50 | Torren |
| Spear | fine | Goblin War-Spear | 13-18 | 0.5 / 1.0 / +1 | 70 | Goblin Warrior, 6 percent |
| Dagger | improved | Steel Dirk | 8-12 | 0.0 / 0.5 / +1 | 45 | Torren |
| Dagger | fine | Enchanted Dagger (existing) | 7-11 | 0.0 / 0.5 / +2 | 80 | the Hidden Cache (existing) |

| Armour | Armour | Slot | Value | Got |
|---|---|---|---|---|
| Padded Jerkin | 1 | body | 20 | Oswy Thackett |
| Leather Jerkin | 2 | body | 50 | Oswy Thackett |
| Leather Cap | 1 | head | 25 | Oswy Thackett |
| Scholar's Robe, +1 Insight | 0 | body | 40 | Halbert Quill |

Every price equals the item's Value, so selling a thing straight back (a merchant pays half the Value) is always a loss. Attribute bonuses use `AttributeBonus` of `/std/item`, which the attribute system reads for equipped items. All armour is `light`, which is the only wear rule `/std` has: it cannot say "all but the Mage" or "Mage and Priest", so those two restrictions are in the items' descriptions only.

### The training dummy

`mob/practice_dummy` is a `/std/monster` that cannot die. Its `Die` is overridden to make it whole again, so a blow of any size gives no experience, no kill credit, no gold, no corpse and no Chronicle entry; its one action (`kind: "sway"`) does nothing; it is made whole when a fight ends. With `verbose on` every blow of the player shows its damage. No change to `/std/monster` was needed.

### The quest

The Quiet Offering, from Sister Merewyn at the shrine (id `the-quiet-offering`): `accept offering`; bring a wildflower and a flask of clean water (Elspeth sells both) and have slain a goblin scavenger; `report`. The reward is 100 experience, 25 gold and the Shrine-Blessed Mace, and a `quest.local` Chronicle report, the generic event that Guildmaster Rowan's two quests are the specific forms of.
