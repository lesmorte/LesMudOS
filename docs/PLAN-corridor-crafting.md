# Build plan: the Greenvale Transit Corridor and the Crafting System

How the two owner's briefs, [specs/transit-corridor.md](specs/transit-corridor.md) and [specs/crafting-system.md](specs/crafting-system.md), get built: what has to exist first, the order of work, which subagent does which part, and the decisions needed from the owner before work starts. Nothing here is built yet.

## 1. Summary

The two briefs depend on each other. The corridor supplies the things crafting needs (rabbits, boars, deer, goblins for blood, herb patches, resource nodes), and crafting gives the corridor's woodland routes their purpose. Both rest on the same handful of engine features that do not exist yet. So the work is three layers:

1. **Foundations** (two agents in parallel): item instances with quality and stacks; world features for hidden places, patrols and resource nodes.
2. **Systems and content** (one engine agent, then up to five content agents at a time): the crafting engine; the corridor's creatures, rooms and dungeons.
3. **Integration and proof** (the lead, plus two agents): connecting everything, the economy and exploit review, and bot play-throughs from level 5 to 10.

Everything waits on class stage A being merged, because stage A rewrites the player, living and creature objects that all of this builds on.

## 2. What the briefs need that the game does not have

| Need | From | Today | Work item |
|---|---|---|---|
| Each item has its own quality | Crafting | Items are identical copies of a file; a character's inventory is saved as a list of file paths | F1 |
| Stacks of materials | Crafting | Every item is a separate object | F1 |
| Value depends on quality | Crafting | Fixed `Value` per file | F1 |
| A carry limit ("+1 carry capacity") | Crafting | No limit exists | Not built (owner's decision 5) |
| Gathering skills that improve with use | Crafting | Stage A skills are abilities you use, not numbers that grow | C1 |
| Harvesting corpses | Crafting | Corpses hold loot only, locked 30 minutes to killer and deceased | C1 |
| Resource nodes that regrow | Both | None | F2 |
| Recipes as data, matcher, inheritance, discovery | Crafting | None | C1 |
| Stations; a healer, tanner and craftsman in the Hollow | Crafting | The Hollow has a tavern, a smith and a guild only | C2 |
| Hidden exits found by looking | Corridor (and Oakenfell's grove) | None | F2 |
| Patrols that walk the road | Corridor | NPCs never move between rooms | F2 |
| Creatures for levels 5 to 10 and their experience | Corridor | The Hollow stops near level 5; nothing beyond is balanced | K1 |
| About 100 rooms and two dungeons | Corridor | Five Vale Road rooms | K2 to K8 |
| First-discovery of a recipe in the Chronicle | Crafting | Chronicle core is merged but not wired into play | Chronicle wiring (already queued) |
| Faction presence | Corridor | Faction design written, not built | Marked in content now, switched on when factions are built |
| A player market | Crafting | Players cannot trade except by dropping items | F1 (`give`), auction house later |

## 3. Design decisions already taken in this plan

These follow from the briefs and the existing game. The owner's answers to the open questions are in section 8.

### Items (F1)

- **Instance state.** Any item may carry saved state: quality (0 Poor to 5 Legendary), quantity, and a small set of named values. Inventories, corpses, the lost-property store and later bank vaults save `path + state` instead of a path alone. Old saves load unchanged (a bare path is an item with default state).
- **Stacks.** A material is one object with a quantity. Two stacks merge only if file and quality both match. `get 3 skins`, `drop 2 fine wolf skin`, `give`, `sell` and shops all understand quantities.
- **Names.** Quality is shown as a prefix and can be typed: `fine wolf skin`. Common has no prefix in ordinary lists, so the inventory stays readable.
- **Value.** Base value × quality multiplier: Poor 0.5, Common 1, Fine 1.5, Excellent 2.5, Masterwork 4, Legendary 8. Shops buy at their usual fraction of that.
- **Carry limit.** Not built in this pass (owner's decision 5). Weight and encumbrance stay a later combat-timing pass.
- **`give`.** Player to player, with quantity. This is the whole of player trade in pass 1.

### World features (F2)

- **Hidden exits.** `HiddenExit(direction, destination, hintDetail, findChance)` on a room: the exit is not listed and cannot be walked until the character has found it, by `search` or by looking at the named detail. Found exits are remembered per character. Used for the Root Warrens, the Forgotten Barrow, the five secret locations and Elder Lane in Oakenfell.
- **Patrols.** `/std/patrol`: an NPC with a list of rooms it walks at a set pace, with arrival and departure lines. Peaceful patrols greet; Riverwatch patrols attack creatures that are fighting a player in the room.
- **Resource nodes.** `/std/node`: a thing in a room with a kind (berry bush, herb patch, wildflowers, thistle), a yield table, and a regrow time. Harvesting empties it for everyone until it regrows.
- **Danger bands.** A room states its level band (5-7, 6-8, 8-10). `look` gives no number, but `consider` and the area notice on entering a band tell a player when they have left the safe road.

### Crafting (C1)

- **Proficiencies.** Skinning, Bleeding, Harvesting and one crafting proficiency per station family (Cooking, Herbalism, Tanning, Tinkering), each 0 to 100, kept on the character. They rise by use, faster on work near your level of skill and not at all on work far beneath it. No profession locks.
- **Harvest quality.** The owner's formula: proficiency + tool modifier + creature or node difficulty modifier + random variation, mapped onto the quality tiers. Legendary materials do not come from ordinary harvesting.
- **Corpses.** `skin corpse` and `bleed corpse`. Each corpse can be skinned once and bled once. Bleeding needs a fresh corpse (within five minutes of death). Harvesting follows the loot lock: for the first 30 minutes only the killer may harvest. Each creature file declares what it yields: `Skin = "wolf"`, `Blood = "wolf"`. A creature with neither yields nothing.
- **Tools.** Skinning Knife, Bleeding Kit and Harvest Sickle, in the five tool qualities. Held in the inventory, not wielded. Common tools are sold in the Hollow; better ones are crafted or found later.
- **Recipes as data.** One entry per recipe in `/recipes/*.cs` (a trusted directory): id, name, ingredients as material ids with counts, station, proficiency and difficulty, output, output count, discovery level, effect parameters. No recipe has its own code.
- **Matcher.** Ingredients are matched as an unordered set of material ids, ignoring quality. An ingredient may be a category (`any berry`) so that later recipes generalize.
- **Inheritance.** Output quality is drawn around the average ingredient quality, shifted by proficiency against the recipe's difficulty. Crafting never fails; low skill lowers quality, high skill can add a bonus output.
- **Discovery.** A character knows a recipe after being taught (trainer, book, quest) or after combining its ingredients at the right station: `combine herb and wildflower`. A wrong combination consumes nothing and says so. Each recipe's discovery level (Known, Discovered, Rare, Secret) is data. The first character to discover a Rare or Secret recipe by experiment reports an event to the Chronicle.
- **Commands.** `recipes`, `recipe <name>`, `craft <name> [count]`, `combine <a> and <b> [and <c>]`, `skin`, `bleed`, `harvest`, `proficiencies`.
- **Effects.** Vitality is hit points. Healing over time, Stamina restoration and the carry bonus use stage A's timed effects and resources.

### The corridor

- **Geography.** The game already has the Hollow's gate on its north side and Oakenfell's Southgate facing it. The corridor runs north from the Hollow's gate to Southgate, and "The South Road" is the name of the road as the city calls it. The owner has confirmed this (section 8).
- **The existing Vale Road.** The five rooms built so far are absorbed: the Long Meadows into segment 1, the River Bend, the Rise and the Southgate Approach into segment 5. The "two miles" waystone goes.
- **Shape.** Each of the five segments is about 11 main-road rooms, 5 woodland rooms and 3 deep-forest rooms, with one secret location each. The main road runs straight enough to follow without a map; side trails leave it at named landmarks and rejoin it.
- **Dungeons.** In addition to the hundred rooms: the Root Warrens at 12 rooms and the Forgotten Barrow at 10.
- **Deer.** The crafting brief needs deer and the corridor's creature lists have none; a Roe Deer (level 3, peaceful until attacked) is added to segments 1 and 2.
- **Factions.** Riverwatch people and patrols, Thorn Pact signs and Circle of Ash clues are written now. NPCs get their faction marker when the faction system is built.
- **Circle of Ash lore.** The Barrow's inscriptions and journals are the first real lore of the Circle. The builder writes them from the bible and the lead sends the full text to the owner before it is merged.

## 4. Order of work

```
Stage A merged (in progress)
   |
   +-- Chronicle wiring, Oakenfell integration        lead, small
   |
   +-- F1 item instances ----+
   +-- F2 world features ----+--- in parallel, different files
                             |
        +--------------------+--------------------+
        |                                         |
   C1 crafting engine                      K1 creatures and balance
        |                                  K2-K6 five segment builders
   C2 crafting content                     K7, K8 two dungeon builders
        |                                         |
        +--------------------+--------------------+
                             |
              I1 integration (lead)  ->  R1 exploit and economy review
                             |
              T1 bot play-throughs, levels 5 to 10
```

The machine has two cores. Content builders write files and build once at the end, so five can run together. Engine agents (F1, F2, C1) run timing-sensitive tests and get the machine with at most one other engine agent. Sandbox and publishing, already queued, runs after F1 and F2 while the content builders write, since content builders add new files only.

## 5. Subagents

Model choice follows the standing rule: the cheapest that will do the job well. Opus where a mistake corrupts saved characters or opens an exploit; Sonnet for everything else. The lead (this session) writes every brief, reviews every branch, runs the tests itself, merges and pushes.

### Foundations

| Id | Agent | Model | Builds | Touches | Done when |
|---|---|---|---|---|---|
| F1 | Item instances | Opus | Instance state, quality, stacks, quantities in commands, quality value, `give`, save format for inventory, corpses and lost property, migration of old saves | `/std/thing`, `item`, `adventurer`, `corpse`, `shopkeeper`, `/secure/lost_property`, SaveStore if needed | New `tests/item_test.py`; every existing test script still passes; an old save file loads |
| F2 | World features | Sonnet | Hidden exits and `search`, patrols, resource nodes, danger bands | `/std/room`, `npc`, new `/std/patrol`, `/std/node` | New `tests/world_test.py`; Elder Lane in Oakenfell opens into the grove as the first use |

### Crafting

| Id | Agent | Model | Builds | Depends on | Done when |
|---|---|---|---|---|---|
| C1 | Crafting engine | Opus | Proficiencies, harvesting from corpses and nodes, tools, recipe registry, matcher, inheritance, discovery and experimentation, stations as a standard object, all commands, the Chronicle event, a simulator for quality odds and gold flow | F1, F2 | New `tests/craft_test.py`; `docs/CRAFTING.md`; three sample recipes working end to end |
| C2 | Crafting content | Sonnet | The 16 materials, 15 tools, the 10 launch recipes as data, four stations, the Hollow's healer, tanner and craftsman with their rooms, vendors selling every material except Goblin Blood, trainers and two recipe books, stations and suppliers in Oakenfell (Crafters' Court, the herbalist's stall) | C1 | Load check clean; every launch recipe craftable by a bot from bought or gathered materials |

### Corridor

| Id | Agent | Model | Builds | Depends on | Done when |
|---|---|---|---|---|---|
| K1 | Creatures and balance | Sonnet | All 30 corridor and dungeon creatures plus the deer, with level, HP, damage, timing, loot, skin and blood yields; the balance simulator extended to levels 5 to 10 for all five callings; an experience table that hits the agreed pace | Stage A; the build map | Simulator `check` mode clean; each creature beatable by its level band's callings at the simulator's target win rate |
| K2 | Segment 1, the South Road | Sonnet | About 20 rooms with people, details, readables, nodes and encounters | F2; the build map | Load check clean; exits match the map |
| K3 | Segment 2, Eldergreen Edge | Sonnet | The same, including the Riverwatch Outpost | as K2 | as K2 |
| K4 | Segment 3, the Green Road | Sonnet | The same, including the Crossroads | as K2 | as K2 |
| K5 | Segment 4, Heartwood | Sonnet | The same, including the two dungeon entrances | as K2 | as K2 |
| K6 | Segment 5, Oakenfell Approach | Sonnet | The same, absorbing the four existing rooms and joining Southgate | as K2 | as K2 |
| K7 | The Root Warrens | Sonnet | 12 rooms, the Broodmother fight, journals, Goblin Blood supply | F2, K1 | Load check clean; boss fight scripted with stage A's combat actions |
| K8 | The Forgotten Barrow | Sonnet | 10 rooms, the Guardian and the Fallen Knight, the lore | F2, K1 | Load check clean; lore text listed in full in the report |

Lessons from the Oakenfell build that go into these briefs:

- The lead writes one build map first (paths, short names, exits, who owns what), as for Oakenfell.
- Each builder is given its own list of first names to draw from, because three builders working apart reused sixteen names between them.
- Each builder's report lists everything it invented that is not in the owner's documents.

### Integration and proof

| Id | Agent | Model | Does |
|---|---|---|---|
| I1 | Integration | Lead | Merges in order; connects the Hollow's gate, the corridor and Southgate; places nodes and patrols; four to six side quests from what the builders wrote; Chronicle events for the dungeons and secret places; area and region data |
| R1 | Exploit and economy review | Opus, fresh context | Tries to break it: duplicating stacks, saving and quitting mid-trade, harvesting others' kills, gold loops (buy, craft, sell at a profit), proficiency farming, sandbox code reaching the new efuns. Reports; the lead fixes |
| T1 | Play-throughs | Sonnet | Extends the bot to walk the corridor from level 5 to 10 with each calling, gathering and crafting on the way, and to report the time to reach each level |

## 6. The lead's own work before any agent starts

1. Merge and verify class stage A.
2. Wire the Chronicle into play and do the Oakenfell integration.
3. Write the corridor build map: about 100 rooms with paths, exits, band, owner; the dungeon maps; the creature list with paths; the name lists.
4. Write the briefs for F1 and F2 from section 3, with the save-format rules spelled out.

## 7. Risks

- **Saved characters.** F1 changes how inventories are saved. The brief requires old saves to load and a test that proves it.
- **Gold loops.** If any recipe's output sells to a vendor for more than its vendor-bought ingredients cost, gold is free. C1's simulator checks every recipe; R1 checks again.
- **Goblin Blood.** Its scarcity is the point of the economy, so no vendor, quest reward or node may hand it out. The Root Warrens' "Goblin Blood supplies" are goblins to bleed, not bottles to pick up.
- **Pace.** Levels 5 to 10 need 3,500 experience, three and a half times everything before. K1 sets creature experience against a target the owner chooses (section 8); T1 measures it.
- **Scale.** A hundred rooms from five builders will be uneven. The lead reads every room; a second pass by one builder for consistency is budgeted if needed.
- **Restarts.** Sessions restart and stop agents. Every brief requires a commit after each step and a notes file, as now.

## 8. The owner's decisions (18 November 2026)

1. **Direction and gates.** As built: north out of the Hollow's gate, arriving at Oakenfell's Southgate past Southfarm Hamlet.
2. **Forests.** Two forests. The corridor runs through the Eldergreen Wood; the Elderwood lies west of Oakenfell, beyond Westgate.
3. **Dungeons.** In addition to the hundred rooms: the Root Warrens at 12 rooms, the Forgotten Barrow at 10.
4. **The Hollow.** Three small new rooms off the square: the healer (Herbalist Table), the tanner (Tanning Rack) and the craftsman (Workbench). The Cooking Fire goes in the tavern.
5. **Carry limit.** None for now. The Traveler's Leather Wrap is built as a wearable item whose "+1 carry capacity" is recorded on it and has no effect until a limit exists. F1 does not build a carry limit.
6. **Player market.** `give` between players and vendors that pay by quality. The auction house stays closed.
7. **Pace from level 5 to 10.** 15 hours for a player with perfect knowledge; about 40 hours for an ordinary player who explores and reads.

### What the pace decision means

Levels 5 to 10 need 3,500 experience under the current formula. The bot reaches level 5 (1,000 experience) in 16 to 30 minutes. Fifteen hours for the next 3,500 is therefore a far slower rate than the Hollow, about a fifteenth of it. Two ways to get there, to be settled when K1 runs the numbers:

- **Steepen the table** for levels 6 to 10 so that more experience is needed, and let corridor creatures pay well. Kills feel rewarding; the number to reach grows.
- **Keep the table** and have corridor creatures and quests pay little. Simple, but a level 8 bear worth less than a cellar goblin reads badly.

The plan assumes the first: the experience formula is data in one place, so K1 proposes a new table for levels 6 to 10 (and a curve onward to 100 on the same footing), sets creature and quest experience against it, and T1's bot measures the perfect-knowledge time against the 15-hour target. Fifteen hours of bot play cannot be run on every change, so T1 measures experience per hour over one-hour samples in each band and the total is projected. Section 3's carry-limit bullet under Items no longer applies.
