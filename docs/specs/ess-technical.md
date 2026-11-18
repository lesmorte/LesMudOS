# Specification: Event Significance Score, technical

This is the technical specification for the Event Significance Score and Chronicle entries as supplied by the project owner. It is newer and more exact than the ESS chapter of the [design bible](design-bible.md); where the two differ (the formula, the multiplier values, the thresholds), this document wins. How it is being implemented is in [../CHRONICLE.md](../CHRONICLE.md).

Spelling is kept as supplied ("Oakenfall", "Greenvale"); see [../WORLD.md](../WORLD.md) for the names the game uses.

---

# Event Significance Score (ESS) Technical Specification

## System Purpose

The Event Significance Score (ESS) is a numerical measure of an event's historical importance.

ESS drives:

- Chronicle Entries
- Renown Awards
- NPC Rumors
- World News
- Books
- Monuments
- Statues
- Hall of Heroes
- Hall of Legends
- Historical Recognition

ESS is intended to create an emergent history system where player actions organically create the history of Hollowmere.

---

# Core Design Philosophy

Every event begins with a score of zero.

The system evaluates:

- What happened
- Who participated
- Whether it was the first occurrence
- Number of players involved
- Impact on the region
- Impact on the world
- Lasting consequences

The final ESS determines whether the event becomes part of history.

---

# ESS Formula

Base ESS = Category Score + Impact Score + Participation Score + Rarity Score + Persistence Score

Final ESS = Base ESS × Event Multipliers

---

# Category Score

Description:

Represents the category of event.

Only the highest applicable category score should be used.

Examples:

Local Quest Completion 10

Dungeon Boss Defeat 25

Raid Boss Defeat 100

World Boss Defeat 250

Legendary Item Creation 500

Artifact Creation 1000

New Region Discovery 1000

New Continent Discovery 2500

Faction Leadership Change 500

World Threat Defeated 3000

Server-Wide Story Resolution 5000

---

# Impact Score

Measures whom the event affects.

Personal Impact 0

Party Impact 25

Guild Impact 50

Settlement Impact 100

Regional Impact 250

Multi-Regional Impact 500

World Impact 1000

---

# Participation Score

Measures scale of involvement.

1 Player 0

2-5 Players 10

6-10 Players 25

11-25 Players 50

26-50 Players 100

51-100 Players 250

100+ 500

Participation should never outweigh significance.

A giant group killing a common goblin is still insignificant.

---

# Rarity Score

Measures uniqueness.

Common 0

Uncommon 25

Rare 100

Very Rare 250

Legendary 500

Unique 1000

Server First 2000

Only one rarity category should apply.

---

# Persistence Score

Measures lasting world impact.

Temporary 0

Reoccurring 25

Long-Term 100

Permanent 500

World-Altering 1000

Examples:

Temporary

Festival completed.

Long-Term

Bridge rebuilt.

Permanent

Dungeon unlocked forever.

World-Altering

New city founded.

---

# Event Multipliers

After Base ESS is calculated:

Apply relevant multipliers.

Multiple multipliers stack multiplicatively.

---

# First Occurrence

x2.0

Examples:

First Dragon Kill

First Artifact Creation

First Prestige Class

---

# Storyline Event

x1.5

Impacts major narrative.

---

# Regional Event

x1.25

Affects Greenvale or other region.

---

# World Event

x2.0

Affects all of Hollowmere.

---

# Seasonal Event

x1.25

Limited-time major content.

---

# Developer Flagged Event

x2.0

Reserved for special events.

---

# Chronicle Thresholds

ESS determines Chronicle visibility.

---

# ESS 0 - 99

Routine Event

No Chronicle Entry.

May generate basic logs.

Examples:

Normal Quest

Creature Kill

Routine Crafting

---

# ESS 100 - 249

Noteworthy Event

Eligible for:

- Tavern rumors
- Local news boards

No permanent Chronicle entry.

---

# ESS 250 - 499

Historical Event

Chronicle Entry Created.

Eligible for:

- Minor Renown bonus
- Historical search

---

# ESS 500 - 999

Major Historical Event

Featured Chronicle Entry.

Becomes part of Greenvale history.

NPCs may reference event.

---

# ESS 1000 - 2499

Legendary Event

Hall of Heroes review.

Special Chronicle formatting.

Books may be generated.

---

# ESS 2500 - 4999

Realm-Shaping Event

Permanent Chronicle prominence.

Regional world changes possible.

Monuments possible.

---

# ESS 5000+

World-Defining Event

Hall of Legends review.

Permanent historical importance.

Future stories may reference event.

Historical books created.

Statues and memorials may be generated.

---

# Chronicle Entry Generation

All Chronicle Entries should be generated from structured templates.

Avoid generic logging.

Chronicles should read like historical records.

---

# Chronicle Data Structure

ChronicleEntry

Id

Timestamp

ESS

Category

Subcategory

Location

Participants

Primary Actor

Related Factions

Narrative Text

Historical Significance

Tags

References

---

# Chronicle Narrative Generation

Chronicle entries should contain:

Headline

Summary

Historical Significance

Participants

Location

Date

---

# Example Template

Headline:

"The Fall of Blackstone Peak"

Summary:

"On the 17th Day of Harvest, a group of adventurers defeated the ancient Black Wyrm that had plagued northern Greenvale for generations."

Historical Significance:

"The reopening of Blackstone Pass restored trade between Oakenfall and the northern settlements."

Participants:

Keith Ironhart

Selene Ashwind

Korran Stonefist

ESS:

2750

Classification:

Realm-Shaping Event

---

# Chronicle Entry Types

## Discovery Entry

Focus:

Exploration

Template:

"Explorer(s) discovered [Location] beneath [Region], revealing knowledge thought lost to time."

---

## Faction Entry

Focus:

Politics

Template:

"[Faction] experienced a significant turning point when [Event] occurred."

---

## Victory Entry

Focus:

Combat

Template:

"[Boss/Threat] was defeated by [Participants], ending a period of danger in [Region]."

---

## Crafting Entry

Focus:

Creation

Template:

"[Player] forged [Item Name], the first artifact of its kind in recorded history."

---

## World Entry

Focus:

Major Milestone

Template:

"The course of Hollowmere changed when [Event] altered the future of the realm."

---

# Chronicle Compression Rules

Avoid filling the Chronicle with noise.

Repeated events should be aggregated.

Bad:

Player killed Goblin #1

Player killed Goblin #2

Player killed Goblin #3

Good:

"The Goblin Purge of Greenvale"

10,000 Goblins defeated during a regional conflict.

---

# Chronicle Categories

All entries belong to one category.

- Discovery
- Combat
- Exploration
- Crafting
- Politics
- Guilds
- Economy
- Factions
- Renown
- World Events
- Seasonal Events
- Player Created Content

---

# Named Historical Events

Events with ESS ≥ 2500 should automatically receive generated historical names.

Examples:

The Fall of Blackstone Peak

The Greenvale Expedition

The Wolf Wars

The Ashen Uprising

The Founding of Stonewatch

The Dragonwinter Campaign

Future NPCs should reference these events by name.

---

# Hall of Heroes Eligibility

Requirements:

ESS Contribution ≥ 5000 Lifetime

or

Multiple entries totaling ≥ 10000 ESS

Hall of Heroes candidates should be reviewed automatically.

---

# Hall of Legends Eligibility

Requirements:

Participation in World-Defining Events

Server First Achievements

Mythic Renown

World-Changing Accomplishments

Hall of Legends entries should be extremely rare.

Many years may pass before a new name qualifies.

---

# Implementation Requirements

All ESS calculations must be deterministic.

All Chronicle records must be stored permanently.

Chronicle records must survive character deletion.

Chronicle records become part of the world's permanent history.

NPC rumor systems, Renown systems, books, libraries, monuments, and future world lore systems should all consume Chronicle data as their primary historical source.

The Chronicle is the memory of Hollowmere.

ESS is the mechanism that decides what deserves to be remembered.
