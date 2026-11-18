# Events, significance, Renown and the Chronicle: implementation design

The owner's briefs are the [ESS technical specification](specs/ess-technical.md) and the Renown, Chronicle and ESS chapters of the [design bible](specs/design-bible.md). Where they differ, the technical specification wins. This document says how the first pass is built in LesMudOS.

Status: design, queued. It is built after class system stage A, because both change the same player, creature and quest code.

## 1. One pipeline

The four systems are one pipeline, and are built as one:

```
something happens → an event is reported → its ESS is computed
    → Renown is awarded to the participants
    → by threshold: nothing, a rumor, a Chronicle entry, a featured entry, a Hall listing
```

Nothing else in the game writes Renown or the Chronicle directly. That keeps the rule in the brief true: the Chronicle is the memory of Hollowmere and ESS decides what is remembered.

## 2. Pass 1 scope

In:

- The event catalogue, event reporting, and deterministic ESS.
- Renown: the number, the tiers, awards, NPC recognition, earned titles. Infamy as a tracked number with no sources yet.
- The Chronicle: permanent storage, narrative entries from templates, server firsts, aggregation of repeated events, the three Halls as computed lists, the world records, and a way to read it in the game.
- Rumors: recent noteworthy events, told by tavern folk.
- The game calendar, because entries carry in-world dates.
- Events from content that exists: Vincent's Hollow and the Forgotten Cellars, levels, and (as hooks, exercised by tests) class advancement.

Out, for later passes: books and Chronicle volumes, statues and monuments, world changes caused by events, NPCs quoting named historical events in ordinary dialogue, seasonal recaps, faction entries (they arrive with Faction Reputation), and any human review step for the Halls.

## 3. Events

### The catalogue

An event type is data: an id, the five score components, the multiplier flags, the Renown it awards, the Chronicle category and entry type, and the narrative template. Event types live in mudlib data files, one file per event type or per group, under a trusted directory (suggested `/events`). Adding an event type is adding a file.

| Field | Meaning |
|---|---|
| Category score | From the specification's Category table. One value. |
| Impact | Personal 0, Party 25, Guild 50, Settlement 100, Regional 250, Multi-Regional 500, World 1000 |
| Rarity | Common 0, Uncommon 25, Rare 100, Very Rare 250, Legendary 500, Unique 1000, Server First 2000. One value. |
| Persistence | Temporary 0, Reoccurring 25, Long-Term 100, Permanent 500, World-Altering 1000 |
| Flags | Storyline ×1.5, Regional ×1.25, World ×2.0, Seasonal ×1.25, Developer Flagged ×2.0 |
| First-occurrence rule | Whether the first time ever earns ×2.0, and whether that first time is listed among the recorded firsts with its holder's name. Being a recorded first is separate from the Server First rarity score. |
| Renown | Renown to each participant, and extra for the first time ever |

### Reporting

Content reports that something happened by event id, with the context: the primary actor, the participants, the location (area and region come from the room), and any names the template needs (the creature, the item, the place). It does not pass scores. The scores come from the catalogue.

Participation is counted by the event service from the participant list: 1 player 0, 2-5 players 10, 6-10 players 25, 11-25 players 50, 26-50 players 100, 51-100 players 250, more than 100 players 500.

### Who may report

History must not be forgeable, and any wizard can now write code in their own directory. So:

- Event types are read only from the trusted catalogue directory.
- Reports are accepted only from objects whose program lies under the trusted trees of the mudlib (`/std`, `/secure`, `/domains`, and the class, skill and event directories). A report from anywhere else, including any wizard's home, is refused and logged.
- The event service, the Renown ledger and the Chronicle are blueprint-only daemons under `/secure`, each checking its caller with `PreviousObject()`, like the wizard registry and the vault.

### The formula

Exactly as the specification gives it:

```
Base ESS  = Category + Impact + Participation + Rarity + Persistence
Final ESS = Base ESS × every applicable multiplier, multiplied together
```

It is computed from the event's data alone, so the same event always scores the same. The first-occurrence multiplier depends on the Chronicle's record of what has already happened, which is itself permanent, so it too is repeatable.

### Server First is reserved

Read literally, the tables would make the first kill of the starter dungeon's boss a world-defining event: Server First rarity alone is 2000, doubled by First Occurrence. The brief's own examples of server firsts are milestones (first level 10, first advanced class, first dragon kill, first legendary item). So:

- **First occurrence** (×2.0) applies to any event type that says so. The first time anyone kills the Goblin Overseer is a first occurrence.
- **Server First rarity** (2000) is used only by event types that are declared milestones: first to reach a milestone level, first advanced class, first prestige class, first kill of a world boss or greater, first legendary or artifact item, first Legendary reputation, first Mythic Renown. A dungeon boss has its own rarity, Uncommon to Rare.

### Thresholds

From the technical specification:

| Final ESS | Class | What happens |
|---|---|---|
| 0-99 | Routine | Nothing recorded. Counted toward aggregates. |
| 100-249 | Noteworthy | Becomes a rumor. No Chronicle entry. |
| 250-499 | Historical | Chronicle entry. Minor Renown bonus. |
| 500-999 | Major Historical | Featured Chronicle entry. |
| 1,000-2,499 | Legendary | Featured entry with special formatting. Counts toward the Hall of Heroes. |
| 2,500-4,999 | Realm-Shaping | Given a generated historical name. Permanent prominence. |
| 5,000+ | World-Defining | Hall of Legends listing. |

### Shared credit

Every participant receives the event's full ESS as contribution credit. The event is recorded once, with all participants named. A participant is a player who dealt damage to the creature and was present when it died, or who is named by the content for non-combat events. (There are no parties yet; this rule gives the same answer when there are.)

### Aggregation

Routine events are never listed one by one. The event service keeps counters by region and kind (goblins killed in Greenvale). A counter crossing a milestone in a data table raises an event of its own, with its own catalogue entry, which is how "The Goblin Purge of Greenvale" comes to exist.

## 4. Events in pass 1

### Repeats are not news

The first time something happens in the world it may matter to a village or a region. The hundredth character to finish the same quest has not changed the world again. So an event type that can be repeated carries two sets of scores: the full ones, used the first time it ever happens, and repeat scores with Personal impact and Common rarity, used every time after. This is what keeps the Chronicle from filling with one entry per player per quest, and it follows the brief's rule that thousands of routine victories are not history.

Final ESS is rounded to the nearest whole number, halves up. Values are for a solo player.

| Event | Category | Impact | Rarity | Persistence | Flags | First time ever | Every later time |
|---|---|---|---|---|---|---|---|
| Ordinary creature killed | 0 | 0 | 0 | 0 | | 0 | 0 (counted) |
| Local quest completed ("Clear the Cellar") | 10 | Settlement 100 | 0 | 0 | | 220 | 10 |
| Dungeon boss killed (Goblin Overseer) | 25 | Settlement 100 | Uncommon 25 | 0 | Storyline | 450 | 38 |
| Story quest completed ("Recover the Overseer's Orders") | 10 | Regional 250 | Uncommon 25 | 0 | Storyline, Regional | 1,069 | 19 |
| Area discovered | 5 | 0 | 0 | 0 | | 10 | 5 |
| Hidden location discovered (Hidden Cache) | 25 | 0 | Uncommon 25 | 0 | | 100 | 25 |
| Secret dungeon discovered (Forgotten Cellars) | 100 | Settlement 100 | Rare 100 | 0 | | 600 | 100 |
| The Great Stump found (Oakenfell's namesake, in the Hidden Grove) | 25 | Settlement 100 | Very Rare 250 | 0 | | 750, and a recorded first | 25 |
| Left the village with a travel pass | 10 | 0 | 0 | 0 | | 20 | 10 |
| Reached a milestone level (10, 25, 50, 75, 100) | 100 | 0 | Server First 2000, then Rare 100 | 0 | | 4,200 | 200 |
| Chose an advanced class | 10 | 0 | Server First 2000, then Common 0 | 0 | | 4,020 | 10 |
| Renown tier reached (Recognized and above) | from the bible's Renown Significance table | 0 | 0 | 0 | | doubled | as listed |
| A thousand goblins killed in the region (aggregate, happens once per thousand) | 100 | Regional 250 | Rare 100 | 0 | Regional | 1,125 | 563 |

What Vincent's Hollow produces:

- **The first character ever** to kill the Overseer (450) and to find the Forgotten Cellars (600) gets a Chronicle entry for each. The first to bring Rowan the orders (1,069) gets a Legendary entry, and the first to find the Hidden Cache (100) and to finish "Clear the Cellar" (220) start rumors.
- **Every later character** doing the same things earns Renown for them but makes no history, except that finding the Forgotten Cellars for oneself (100) is still talked about in the tavern.
- **Everyone together** makes history through the aggregates: the thousandth goblin killed in Greenvale is a featured entry no matter who strikes the blow.
- **The first character to find the Great Stump** in Oakenfell's Hidden Grove gets a featured entry (750) and is recorded among the firsts, by the owner's decision: finding the city's namesake is history. It is a recorded first without the Server First rarity, which stays reserved for milestones. Later finders earn the hidden-location Renown and no entry.
- **Milestones** such as the first character to reach level 10 or to choose an advanced class are Realm-Shaping events with a server first. They are meant to be the entries a new player reads years later.

The numbers are a starting point. The implementer tabulates what a full play-through produces for the first character and for a later one, and the table is adjusted until that reads sensibly.

## 5. Renown

A single number per character, saved with the character, and mirrored in the Renown ledger daemon by name so that records survive the character's deletion.

- **Awards.** Each event type carries its Renown, from the bible's tables: minor quest 1, regional story quest 10, discover a location 1, hidden location 5, first player to discover 25, first boss kill 10 (the character's own first kill of that boss), and so on. A Chronicle-worthy event adds a bonus by class: Historical +5, Major Historical +10, Legendary +25, Realm-Shaping +50, World-Defining +100.
- **Losing it.** Renown is never reduced in this pass.
- **Tiers.** Unknown 0, Local 1-99, Recognized 100-499, Acclaimed 500-999, Renowned 1,000-2,499, Famous 2,500-4,999, Heroic 5,000-9,999, Legendary 10,000-24,999, Mythic 25,000+.
- **Seeing it.** `score` shows the tier in words. The exact number is shown by `renown`.
- **NPC recognition.** When a character greets an NPC (`talk`), the NPC's first words acknowledge their tier, from Recognized upward, using lines in the bible's spirit ("You seem familiar." / "I have heard stories about your travels."). The lines are data on the NPC base and an NPC may have its own. Below Recognized nothing changes.
- **Titles.** Reaching a tier earns its title permanently: Local Hero, Wanderer, Champion, Defender of Greenvale, Voice of the People, Hero of Hollowmere, Living Legend, The Mythic. Pathfinder is earned by discovering every area and hidden location in a region. `title` lists a character's earned titles and sets one of them (or none). The free-text `title` that exists today remains for wizards only.
- **Infamy.** A second number, saved and shown in `renown` only when above zero. Nothing awards it yet.

## 6. The Chronicle

### Storage

The Chronicle daemon owns an append-only list of entries with its own save file. Entries are never edited or removed by the game, and they refer to characters by name, so they survive character deletion. Each write saves at once.

An entry holds the specification's structure: id, timestamp (real and in-world), ESS, category, subcategory, location (area and region), participants, primary actor, related factions (empty until factions exist), headline, narrative text, historical significance, tags, references (ids of related entries).

The daemon also keeps, permanently: which first occurrences and server firsts have happened and who holds them; each character's lifetime ESS contribution and Chronicle ESS total; the aggregate counters.

### Narrative

Entries are written from templates, never as logs. Each entry type from the specification has a template (Discovery, Victory, Crafting, Faction, World), plus Quest and Milestone. An event type may supply its own headline, summary and significance text, which is how a named event such as the fall of the Goblin Overseer gets words that fit it:

> **The Quieting of the Cellars.** On the 4th Day of Harvest, Year 1, Keith brought down the Goblin Overseer in the tunnels beneath The Village of Vincent's Hollow. *The goblins that had plagued the Hollowman's Rest were left leaderless, and the village slept easier.*

Participants are listed by name; three or more are written as a list with "and".

Events of 2,500 ESS or more are given a historical name from pattern tables ("The Fall of ...", "The ... Expedition", "The Founding of ...") unless the event type supplies one.

### Categories

Discovery, Combat, Exploration, Crafting, Politics, Guilds, Economy, Factions, Renown, World Events, Seasonal Events, Player Created Content. One per entry.

### The Halls and the records

All computed, with no review step in this pass:

- **Hall of Heroes:** characters whose lifetime ESS contribution is at least 5,000, or whose Chronicle entries total at least 10,000.
- **Hall of Legends:** characters who took part in a World-Defining event, hold a server first, or have reached Mythic Renown.
- **Hall of Infamy:** empty until Infamy has sources.
- **World records:** each server first with its holder; the most renowned characters; the first discoverer of each area.

### Reading it

The Chronicle is kept by the Hall of Legends, and its home will be the Grand Chronicle in Oakenfell. Until that exists, the Adventurers Guild in Vincent's Hollow holds a copy: a chronicle ledger on a stand, an object in the room.

Where a ledger is present:

| Command | Shows |
|---|---|
| `read chronicle` (or `chronicle`) | The most recent entries, featured ones marked |
| `chronicle <number>` | One entry in full |
| `chronicle search <name or word>` | Entries naming a character, place or creature |
| `chronicle firsts` | Server firsts and their holders |
| `chronicle heroes`, `chronicle legends` | The Halls |
| `chronicle records` | The world records |

The ledger is the same object that the Hall of Legends will use, so the Grand Chronicle is a placement, not new code.

### Rumors

Noteworthy events (100-249) and the headline of anything greater are kept in a rolling list of recent talk per region, saved, limited to the last fifty. NPCs marked as gossips answer `ask <npc> about news` (also `rumors`) with one of them, phrased as hearsay ("They say ... "). In Vincent's Hollow the gossips are Mara the Barmaid, Harland the Innkeeper and the Drunken Prospector.

## 7. The calendar

Entries are dated in the world's own calendar.

- One real day is one day in Hollowmere. A year is twelve months of thirty days, followed by the Turning, a festival of five days (six in a real leap year).
- Months: Deepfrost, Thawmoon, Sowing, Blossom, Greentide, Highsun, Goldfields, Harvest, Leaf-fall, Mistwane, Firstfrost, Longnight.
- Year 1 begins on the day the Chronicle is first started on a server; that date is stored by the Chronicle and never changes.
- Dates read "the 17th Day of Harvest, Year 3".
- A `date` command shows today's date.

The month names are placeholders of mine and are one table to change.

## 8. Where content reports events

- Creature death: the base creature reports its kill, with a rank set on the creature (ordinary, dungeon boss, world boss). Ordinary kills only feed counters.
- Quest completion: reported where the quest state is set to completed, with the quest's event type.
- Discovery: an area reports when a character enters it for the first time; a room may be marked a hidden location. Each character remembers what they have discovered.
- Levels, advanced classes, Renown tiers: reported by the player object and the class framework.
- The travel pass: reported the first time a character walks out with it.

## 9. Testing

- ESS: the formula against hand-worked cases from the specification's tables, every multiplier, stacking, determinism (the same event twice scores the same apart from first occurrence).
- First occurrence and server firsts across a driver restart; a second character does not get them.
- Each event in the pass 1 table raised by really doing the thing in Vincent's Hollow, with the expected ESS, Renown, rumor or entry.
- Thresholds at their boundaries with test event types.
- Shared credit with two players who both damaged a boss.
- Aggregation: a counter milestone raised with a small threshold in a test catalogue.
- Renown tiers, the NPC recognition line, titles earned and set, wizard free-text title still working.
- Chronicle persistence across restart and after the character's save file is deleted; search; the Halls and records.
- Forgery: an object in a wizard's home cannot report an event, define an event type, write the Chronicle or the Renown ledger, or raise its owner's Renown.
- The play-through ends by reading the hero's own entries in the guild's ledger.
