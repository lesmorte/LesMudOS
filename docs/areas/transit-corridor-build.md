# The Greenvale Transit Corridor: build map

The file names, exits, zones and ownership for the road from Vincent's Hollow to Oakenfell. It turns the owner's brief ([../specs/transit-corridor.md](../specs/transit-corridor.md)) into exact paths so that several builders can work at once and their rooms connect. The plan this belongs to is [../PLAN-corridor-crafting.md](../PLAN-corridor-crafting.md).

One hundred rooms in five segments of twenty, plus two dungeons of 12 and 10 rooms. The road runs north from the Hollow to Oakenfell's Southgate, through the Eldergreen Wood. (The Elderwood is a different forest, west of the city.)

```
Vincent's Hollow, North Road
   |
/domains/greenvale/vale_road/road        (exists)
   |
Segment 1  The South Road           m01 .. m11
Segment 2  Eldergreen Edge          m01 .. m11
Segment 3  The Green Road           m01 .. m11   (m11 is the Crossroads Stone)
Segment 4  Heartwood                m01 .. m11   (Root Warrens under w2; Forgotten Barrow behind d3)
Segment 5  Oakenfell Approach       m01 .. m11
   |
/domains/greenvale/vale_road/approach    (exists)  ->  Oakenfell, Southgate
```

## The shape of every segment

Every segment has the same skeleton, so that a player learns to read the land:

```
            m11
             |
            ...
 d3 -- w5 -- m07        the woodland trail rejoins the road
 |     |     m06
 d2    w4    m05
 |     |     m04
 d1 -- w3    |
       w2    |
       w1 -- m03        the woodland trail leaves the road at a landmark
             m02
             m01
```

- **Main road** `m01` to `m11`: straight north and south. Safest. Creatures appear only as chance encounters.
- **Woodland trail** `w1` to `w5`: leaves the road at `m03` and rejoins it at `m07`. Standing creatures, resource nodes, the segment's side interest.
- **Deep forest** `d1` to `d3`: reached from `w3`, returning at `w5`. The segment's most dangerous creatures.
- **Secret room** `s1`: reached by a hidden way from one named room.

The diagram shows the trail on the west. Odd segments have it on the west of the road and even segments on the east; the tables give the real directions.

## Rules for every builder

- Add new files only, and only in the directory you own. Do not edit any existing file.
- Rooms go directly in your segment directory with exactly the file names in the table. People go in `<your directory>/npc/`, things in `<your directory>/obj/`. You create no creatures: they are being written by someone else at the paths in the creature table below, and you refer to them by path.
- Class names must be unique across the whole mudlib: start every class name with your prefix.
- Exits are exactly those in your table. Every exit has its return exit; the tables give both ends.
- **Hidden ways are not exits yet.** The feature that hides an exit is being built now. Write the room so that an observant player would suspect the way, and write the room at the far end with its ordinary exit back. Do not add the hidden exit itself. List every hidden way in your report: from, direction, to, and the detail a player should look at to find it.
- **Resource nodes and patrols are not objects yet.** Describe the berry bushes, herb patches, wildflowers and thistle in the rooms where they grow, as details. List in your report which rooms should get which kind of node, and any stretch of road a Riverwatch patrol should walk.
- **Creatures.** Use `AddEncounter(path, percent)` on main-road rooms (5 to 15 percent) and `AddSpawn(path, count)` on woodland and deep rooms, with the creatures your segment's table allows for that zone. Not every room needs a creature: about half the woodland rooms and every deep room. Named places with people (camps, the outpost, the inn, the hamlet) have no creatures.
- **Safety.** Rooms are not safe, except the one or two havens named in your segment's notes.
- **Danger.** The band column is for your information; the band is set on rooms later.
- **People.** Use only the first names given for your segment. Peaceful NPCs with ShortDesc, LongDesc, chat lines, topics and emotes, as in docs/BUILDING.md. Riverwatch Rangers are the faction of the road: their motto is "Ever Vigilant" and their headquarters is Watchfort Crossing.
- **Factions in the landscape.** Thorn Pact: strange markings, protected groves, ritual stones, odd animal behavior; nobody present can explain them and no Pact member appears. Circle of Ash: never named, by anyone or anything; clues only where your notes say so.
- **Tone.** The journey is a rite of passage, from farms and familiar faces through increasingly wild land to the walls of the city. Each room's long description is three to five sentences written for that place: what is seen, heard and smelled, and what has changed since the last room. Five or six details in each room, readables where a waystone, sign or carving would be. Never the same sentence twice on the road.
- Follow docs/BUILDING.md: the templates, the colour tag rule (`{cmd}` only around what a player can literally type, never in Name, Ids or ShortDesc), no "> " in any text.
- Before you commit, run `python3 tests/lint_area.py corridor` if that script exists in your worktree, and fix what it reports in your files.

## Creatures

Written by the creature builder at `/domains/greenvale/corridor/mob/<id>.cs`. Levels are the owner's.

| Id | Name | Level | Where |
|---|---|---|---|
| `field_rabbit` | Field Rabbit | 1 | Segment 1 |
| `roe_deer` | Roe Deer | 3 | Segments 1, 2 (added for skins; flees or fights only when attacked) |
| `grey_fox` | Grey Fox | 4 | Segment 1 |
| `stray_wolf` | Stray Wolf | 5 | Segment 1 |
| `bandit_lookout` | Bandit Lookout | 6 | Segment 1 |
| `forest_spider` | Forest Spider | 5 | Segment 2 |
| `forest_wolf` | Forest Wolf | 6 | Segment 2 |
| `wild_boar` | Wild Boar | 6 | Segment 2 |
| `goblin_scout` | Goblin Scout | 7 | Segment 2 |
| `goblin_raider` | Goblin Raider | 7 | Segment 3 |
| `boar_brute` | Boar Brute | 7 | Segment 3 |
| `woodland_spider` | Woodland Spider | 8 | Segment 3 |
| `rogue_bandit` | Rogue Bandit | 8 | Segment 3 |
| `young_black_bear` | Young Black Bear | 8 | Segment 3 |
| `black_bear` | Black Bear | 8 | Segment 4 |
| `heartwood_goblin` | Goblin Warrior | 8 | Segment 4 (the file is not named goblin_warrior because the Hollow has one) |
| `thorn_woodling` | Thorn Woodling | 9 | Segment 4 |
| `forest_stalker` | Forest Stalker | 9 | Segment 4 |
| `ancient_wolf` | Ancient Wolf | 9 | Segment 4 |
| `highway_bandit` | Highway Bandit | 8 | Segment 5 |
| `rogue_goblin_veteran` | Rogue Goblin Veteran | 9 | Segment 5 |
| `bandit_leader` | Bandit Leader | 10 | Segment 5, the Bandit Camp |
| `forest_panther` | Forest Panther | 10 | Segment 5, the Panther's Ledge |
| `tunnel_rat` | Tunnel Rat | 7 | Root Warrens |
| `goblin_tunneler` | Goblin Tunneler | 8 | Root Warrens |
| `root_creeper` | Root Creeper | 8 | Root Warrens |
| `warren_broodmother` | Warren Broodmother | 10 | Root Warrens, the boss |
| `restless_spirit` | Restless Spirit | 9 | Forgotten Barrow |
| `ancient_skeleton` | Ancient Skeleton | 9 | Forgotten Barrow |
| `barrow_guardian` | Barrow Guardian | 10 | Forgotten Barrow |
| `fallen_knight` | Fallen Knight | 10 | Forgotten Barrow, the boss |

## Segments

### Segment 1: The South Road (builder 1, prefix `Sr`) — directory `/domains/greenvale/corridor/south_road/`

Open grassland, scattered farms, wooden fences, small streams. Relatively safe. Traveler's Rest is a wayside inn yard: an innkeeper selling food and a bed is welcome (a shopkeeper), and it is a safe room and a place to log out. Berry bushes and wildflowers as resource nodes in the woodland rooms. Names: Aldith, Bram, Cuthbert, Edda, Gytha, Hamo, Ives, Jocosa, Kenrick, Leofric, Mildred, Odo.

Creatures: Main road (encounters): field_rabbit, grey_fox. Woodland: field_rabbit, roe_deer, grey_fox, stray_wolf. Deep: stray_wolf, bandit_lookout.

Hidden way to the secret room: from `d2`, `west`, to `s1`. An old well-head lost in brambles: a cache of a traveler long gone. Lore of the farms; a small treasure.

| File | Short name | Zone | Band | Exits |
|---|---|---|---|---|
| `m01` | The South Road, Beyond the Hollow | main | 5-6 | south `/domains/greenvale/vale_road/road`; north m02 |
| `m02` | The South Road, Hedgerow Mile | main | 5-6 | south m01; north m03 |
| `m03` | Old Miller's Farm, the Lane End | main | 5-6 | south m02; north m04; west w1 |
| `m04` | The South Road, Barley Fields | main | 5-6 | south m03; north m05 |
| `m05` | The South Road, below Windmill Hill | main | 5-6 | south m04; north m06 |
| `m06` | The South Road, the Sheepwalk | main | 5-6 | south m05; north m07 |
| `m07` | Traveler's Rest | main | 5-6 | south m06; north m08; west w5 |
| `m08` | The South Road, the Long Meadows | main | 5-6 | south m07; north m09 |
| `m09` | The South Road, Willow Brook | main | 5-6 | south m08; north m10 |
| `m10` | The Stone Bridge | main | 5-6 | south m09; north m11 |
| `m11` | The South Road, the Far Bank | main | 5-6 | south m10; north `/domains/greenvale/corridor/eldergreen_edge/m01` |
| `w1` | Old Miller's Farm, the Yard | woodland | 5-6 | east m03; north w2 |
| `w2` | Old Miller's Farm, the Orchard | woodland | 5-6 | south w1; north w3 |
| `w3` | Windmill Hill | woodland | 5-6 | south w2; north w4; west d1 |
| `w4` | The Brook Pasture | woodland | 5-6 | south w3; north w5 |
| `w5` | The Hay Meadow | woodland | 5-6 | south w4; east m07; west d3 |
| `d1` | The Fox Covert | deep | 6-7 | east w3; north d2 |
| `d2` | The Bramble Hollow | deep | 6-7 | south d1; north d3 |
| `d3` | The Old Sheepfold | deep | 6-7 | south d2; east w5 |
| `s1` | The Dry Well | secret | 6-7 | east d2 |

### Segment 2: Eldergreen Edge (builder 2, prefix `Ee`) — directory `/domains/greenvale/corridor/eldergreen_edge/`

Farmland gives way to forest; the road narrows; ancient trees; sunlight still reaches the ground. The Riverwatch Outpost is the Rangers' main post on the road: a commander, two or three rangers, a notice of bounties (text only), a safe room. The Marked Oak carries Thorn Pact markings that no one present can explain. Herb patches and thistle as nodes. Names: Alwin, Berenice, Cadoc, Delwyn, Elowen, Finnian, Gwenllian, Hywel, Idris, Kerensa, Morwenna, Tegan.

Creatures: Main road (encounters): forest_spider, forest_wolf. Woodland: roe_deer, forest_spider, forest_wolf, wild_boar. Deep: wild_boar, goblin_scout.

Hidden way to the secret room: from `d2`, `east`, to `s1`. A concealed hide in the fork of a great tree, with a dead hunter's notes on the movements of goblins.

| File | Short name | Zone | Band | Exits |
|---|---|---|---|---|
| `m01` | The Edge Road, the Last Fences | main | 5-7 | south `/domains/greenvale/corridor/south_road/m11`; north m02 |
| `m02` | The Edge Road, the First Oaks | main | 5-7 | south m01; north m03 |
| `m03` | The Forester's Cabin | main | 5-7 | south m02; north m04; east w1 |
| `m04` | The Edge Road, under the Eaves | main | 5-7 | south m03; north m05 |
| `m05` | The Broken Waystone | main | 5-7 | south m04; north m06 |
| `m06` | The Edge Road, Fern Banks | main | 5-7 | south m05; north m07 |
| `m07` | The Edge Road, the Charcoal Clearing | main | 5-7 | south m06; north m08; east w5 |
| `m08` | The Edge Road, the Narrows | main | 5-7 | south m07; north m09 |
| `m09` | The Riverwatch Outpost | main | 5-7 | south m08; north m10 |
| `m10` | The Edge Road, the Patrol Mile | main | 5-7 | south m09; north m11 |
| `m11` | The Edge Road, the Green Gate | main | 5-7 | south m10; north `/domains/greenvale/corridor/green_road/m01` |
| `w1` | The Woodcutters' Ride | woodland | 6-7 | west m03; north w2 |
| `w2` | The Hunter's Camp | woodland | 6-7 | south w1; north w3 |
| `w3` | The Deer Lawn | woodland | 6-7 | south w2; north w4; east d1 |
| `w4` | The Spider Thicket | woodland | 6-7 | south w3; north w5 |
| `w5` | The Birch Stand | woodland | 6-7 | south w4; west m07; east d3 |
| `d1` | The Boar Wallow | deep | 7-8 | west w3; north d2 |
| `d2` | The Goblin Scouts' Trail | deep | 7-8 | south d1; north d3 |
| `d3` | The Marked Oak | deep | 7-8 | south d2; west w5 |
| `s1` | The Hunter's Hide | secret | 7-8 | west d2 |

### Segment 3: The Green Road (builder 3, prefix `Gr`) — directory `/domains/greenvale/corridor/green_road/`

Dense forest; the road enclosed beneath the canopy; travelers move in groups. The Merchant Camp is a caravan halted for the night: a merchant who sells travel food and potions, guards, a safe room. The Old Shrine is a wayside shrine still tended. The Crossroads Stone is an old marker where an overgrown track leaves the road. Herb patches and wildflowers as nodes. Names: Ambrose, Blaise, Clemence, Drogo, Estrild, Fulk, Godiva, Hereward, Isabeau, Lambert, Melisande, Wystan.

Creatures: Main road (encounters): goblin_raider, boar_brute. Woodland: boar_brute, woodland_spider, young_black_bear. Deep: goblin_raider (several), rogue_bandit, woodland_spider.

Hidden way to the secret room: from `w3`, `down`, to `s1`. A sealed cellar beneath the watchtower: the last garrison's log, and the first arcane symbol a traveler may notice without understanding it.

| File | Short name | Zone | Band | Exits |
|---|---|---|---|---|
| `m01` | The Green Road, into the Deep Wood | main | 6-7 | south `/domains/greenvale/corridor/eldergreen_edge/m11`; north m02 |
| `m02` | The Green Road, the Canopy | main | 6-7 | south m01; north m03 |
| `m03` | The Merchant Camp | main | 6-7 | south m02; north m04; west w1 |
| `m04` | The Green Road, the Sunless Mile | main | 6-7 | south m03; north m05 |
| `m05` | The Green Road, the Corduroy Causeway | main | 6-7 | south m04; north m06 |
| `m06` | The Old Shrine | main | 6-7 | south m05; north m07 |
| `m07` | The Green Road, the Wagon Halt | main | 6-7 | south m06; north m08; west w5 |
| `m08` | The Green Road, the Leaning Stones | main | 6-7 | south m07; north m09 |
| `m09` | The Green Road, below the Watchtower | main | 6-7 | south m08; north m10 |
| `m10` | The Green Road, the Dark Hollow | main | 6-7 | south m09; north m11 |
| `m11` | The Crossroads Stone | main | 6-7 | south m10; north `/domains/greenvale/corridor/heartwood/m01` |
| `w1` | The Pedlars' Path | woodland | 7-8 | east m03; north w2 |
| `w2` | The Raiders' Ford | woodland | 7-8 | south w1; north w3 |
| `w3` | The Abandoned Watchtower | woodland | 7-8 | south w2; north w4; west d1 |
| `w4` | The Webbed Glade | woodland | 7-8 | south w3; north w5 |
| `w5` | The Bear Trees | woodland | 7-8 | south w4; east m07; west d3 |
| `d1` | The Goblin Raiders' Camp | deep | 8-9 | east w3; north d2 |
| `d2` | The Spider Ravine | deep | 8-9 | south d1; north d3 |
| `d3` | The Bandits' Lookout | deep | 8-9 | south d2; east w5 |
| `s1` | The Watchtower Cellar | secret | 8-9 | up w3 |

### Segment 4: Heartwood (builder 4, prefix `Hw`) — directory `/domains/greenvale/corridor/heartwood/`

Ancient forest, very old growth, moss-covered ruins, signs of older civilizations; it should feel older than Greenvale itself. Few people: a lone Riverwatch hunter station, a scholar sketching the ruins. The Ritual Stones are a Thorn Pact place, tended and empty. Two hidden ways lead out of this segment to the dungeons, written by other builders: from the Uprooted Giant, `down` to /domains/greenvale/corridor/root_warrens/entrance; from the Collapsed Shrine, `in` to /domains/greenvale/corridor/barrow/entrance. Write both rooms so that an observant player suspects the way (a hollow under the root plate; a gap behind fallen stones) and list them among your hidden features; do not add the exits. Names: Aurelian, Briseis, Cassian, Drusilla, Evander, Faustina, Lysander, Marcella, Octavian, Petronella, Silvanus, Valeria.

Creatures: Main road (encounters): black_bear, heartwood_goblin. Woodland: black_bear, heartwood_goblin, thorn_woodling, forest_stalker. Deep: forest_stalker, ancient_wolf (one, in its dell), thorn_woodling.

Hidden way to the secret room: from `d2`, `east`, to `s1`. A cache of burned journals and a scorched satchel: a clue to the Circle of Ash, which is never named.

| File | Short name | Zone | Band | Exits |
|---|---|---|---|---|
| `m01` | The Heartwood Road, the Old Growth | main | 7-8 | south `/domains/greenvale/corridor/green_road/m11`; north m02 |
| `m02` | The Fallen Arch | main | 7-8 | south m01; north m03 |
| `m03` | The Heartwood Road, the Root Stair | main | 7-8 | south m02; north m04; east w1 |
| `m04` | The Heartwood Road, the Moss Mile | main | 7-8 | south m03; north m05 |
| `m05` | The Mossgate Ruins | main | 7-8 | south m04; north m06 |
| `m06` | The Heartwood Road, the Standing Trunks | main | 7-8 | south m05; north m07 |
| `m07` | The Hunter's Memorial | main | 7-8 | south m06; north m08; east w5 |
| `m08` | The Heartwood Road, the Green Dusk | main | 7-8 | south m07; north m09 |
| `m09` | The Heartwood Road, the Paved Remnant | main | 7-8 | south m08; north m10 |
| `m10` | The Heartwood Road, the Thinning | main | 7-8 | south m09; north m11 |
| `m11` | The Heartwood Road, the Last Great Oak | main | 7-8 | south m10; north `/domains/greenvale/corridor/approach/m01` |
| `w1` | The Mossgate Ruins, the Inner Court | woodland | 8-9 | west m03; north w2 |
| `w2` | The Uprooted Giant | woodland | 8-9 | south w1; north w3 |
| `w3` | The Dryad's Spring | woodland | 8-9 | south w2; north w4; east d1 |
| `w4` | The Ritual Stones | woodland | 8-9 | south w3; north w5 |
| `w5` | The Stalker's Run | woodland | 8-9 | south w4; west m07; east d3 |
| `d1` | The Ancient Wolf's Dell | deep | 9-10 | west w3; north d2 |
| `d2` | The Thorn Thicket | deep | 9-10 | south d1; north d3 |
| `d3` | The Collapsed Shrine | deep | 9-10 | south d2; west w5 |
| `s1` | The Hidden Cache | secret | 9-10 | west d2 |

### Segment 5: Oakenfell Approach (builder 5, prefix `Oa`) — directory `/domains/greenvale/corridor/approach/`

The forest opens; stone road replaces dirt; merchant wagons become common; the city is glimpsed in the distance. The Toll House is the Assembly's: a toll keeper who waves adventurers with a travel pass through. The Staging Grounds hold caravans forming up. Southfarm Hamlet is a farming hamlet under the city's walls: a safe room, a farm wife selling food. The Rise is the first full sight of Oakenfell and must carry the moment of wonder: read mudlib/domains/greenvale/vale_road/rise.cs and river_bend.cs, which this segment replaces, and carry over the best of their text and the ranger. Your last room's north exit is /domains/greenvale/vale_road/approach, an existing room. Names: Adelina, Baldric, Constance, Diggory, Emmeline, Florian, Griselda, Humphrey, Jessamy, Lorcan, Oswin, Philippa.

Creatures: Main road (encounters): highway_bandit. Woodland: highway_bandit, rogue_goblin_veteran; the Bandit Camp holds bandit_leader (one) with highway_bandit guards. Deep: forest_panther (one, on its ledge), rogue_goblin_veteran.

Hidden way to the secret room: from `d1`, `down`, to `s1`. A culvert of the aqueduct, walled up long ago: masonry far older than the city, and marks no mason of Oakenfell would know.

| File | Short name | Zone | Band | Exits |
|---|---|---|---|---|
| `m01` | The Approach Road, the Forest's Edge | main | 6-8 | south `/domains/greenvale/corridor/heartwood/m11`; north m02 |
| `m02` | The Approach Road, the First Paving | main | 6-8 | south m01; north m03 |
| `m03` | The Greenvale Toll House | main | 6-8 | south m02; north m04; west w1 |
| `m04` | The Approach Road, Milestone Row | main | 6-8 | south m03; north m05 |
| `m05` | The Merchant Staging Grounds | main | 6-8 | south m04; north m06 |
| `m06` | The Approach Road, under the Old Aqueduct | main | 6-8 | south m05; north m07 |
| `m07` | The Approach Road, the River Bend | main | 6-8 | south m06; north m08; west w5 |
| `m08` | Southfarm Hamlet, the Green | main | 6-8 | south m07; north m09 |
| `m09` | Southfarm Hamlet, Mill Lane | main | 6-8 | south m08; north m10 |
| `m10` | The Approach Road, the Rise | main | 6-8 | south m09; north m11 |
| `m11` | The Approach Road, the Carters' Mile | main | 6-8 | south m10; north `/domains/greenvale/vale_road/approach` |
| `w1` | The Old Aqueduct, the Broken Span | woodland | 8-9 | east m03; north w2 |
| `w2` | The Bandits' Road | woodland | 8-9 | south w1; north w3 |
| `w3` | The Bandit Camp | woodland | 8-9 | south w2; north w4; west d1 |
| `w4` | The Quarry Track | woodland | 8-9 | south w3; north w5 |
| `w5` | Southfarm Hamlet, the Back Fields | woodland | 8-9 | south w4; east m07; west d3 |
| `d1` | The Aqueduct Cistern | deep | 9-10 | east w3; north d2 |
| `d2` | The Panther's Ledge | deep | 9-10 | south d1; north d3 |
| `d3` | The Veterans' Den | deep | 9-10 | south d2; east w5 |
| `s1` | The Sealed Culvert | secret | 9-10 | up d1 |

## Dungeons

Each dungeon has one builder, who designs its internal layout. Only the entrance is fixed.

### The Root Warrens (builder 6, prefix `Rw`) — directory `/domains/greenvale/corridor/root_warrens/`

Twelve rooms, levels 7 to 8, boss level 10. A network of tunnels dug beneath ancient roots, once goblin, now something else's: the goblins that remain are tunnelers holding a corner of it against the creepers and the Broodmother.

- `entrance` is fixed: its exit `up` leads to `/domains/greenvale/corridor/heartwood/w2` (the Uprooted Giant). The way down from there is hidden and is added later.
- The other eleven rooms are yours to name and connect, with file names in lower case with underscores. Give it a shape with a choice in it (a loop or two branches), one optional side chamber that is easy to miss, and the Broodmother's chamber at the far end.
- Creatures: `tunnel_rat`, `goblin_tunneler`, `root_creeper`, and one `warren_broodmother`.
- The owner's notable features: goblin blood supplies (this means goblins to fight, which a later system lets players bleed; do NOT place bottles or any item of goblin blood); early crafting materials (describe what grows and lies here: pale roots, fungus, old hides, as details); forgotten journals (two or three readables: a goblin overseer's tally, a prospector's last pages); a future expansion hook (a sealed or collapsed way deeper, as a detail, leading nowhere).
- No people. No Circle of Ash material here.

### The Forgotten Barrow (builder 7, prefix `Fb`) — directory `/domains/greenvale/corridor/barrow/`

Ten rooms, levels 9 to 10. An ancient burial site predating Greenvale, one of the oldest places in the region.

- `entrance` is fixed: its exit `out` leads to `/domains/greenvale/corridor/heartwood/d3` (the Collapsed Shrine). The way in from there is hidden and is added later.
- The other nine rooms are yours to name and connect. A descent: forecourt, passage, side tombs, a guardian's hall, the knight's tomb at the end.
- Creatures: `restless_spirit`, `ancient_skeleton`, one or two `barrow_guardian`, and one `fallen_knight`.
- This is the first strong lore of three things: lost civilizations, Hollowmere's history, and the Circle of Ash. Write inscriptions, grave goods and wall carvings as details and readables. The people buried here are older than Greenvale and are given no name the bible does not give. The Circle of Ash is never named: its trace here is that someone has been in before the player, recently and carefully: rubbings taken of one inscription, a tomb opened and resealed, ash from a burned page, an arcane symbol chalked small beside a door. Every piece of lore text you write goes in full into your report, because the owner reviews it before it is used.
- No people.

## After the builders

The lead connects the Vale Road's first room to segment 1, retires the three Vale Road rooms the corridor replaces (`meadows`, `river_bend`, `rise`), adds the hidden ways, nodes, patrols and bands, places the dungeon entrances, and adds area data for each directory.
