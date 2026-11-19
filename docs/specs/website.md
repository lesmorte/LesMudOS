# Specification: the public website (owner's brief)

The owner's brief for the public website of Legends of Hollowmere, condensed from the source document without changing its content. Source: <https://claude.ai/artifact/X3nxeU59ghu4SL8X4gmE9r>. How it is built is in [../WEBSITE.md](../WEBSITE.md).

## The task

Build a modern, responsive marketing website for **Legends of Hollowmere**, designed to attract both classic MUD players and modern RPG players.

The visual design communicates fantasy, exploration, adventure, discovery, community and a living world. Avoid dark "grimdark" aesthetics; favor warm medieval fantasy themes. Think classic fantasy novels, old-world maps, hearthfires, forest roads, adventurer guilds, small villages becoming grand adventures. The site should feel welcoming and mysterious.

## Primary goal

A visitor should understand within 30 seconds: what Legends of Hollowmere is, why it is unique, how to start playing, and what kind of world Hollowmere is.

## Sections

**Hero.** Large header. Title: Legends of Hollowmere. Tagline: "The World Beyond Vincent's Hollow" (alternative taglines may be tested). Call-to-action buttons: Play Now, Create Account, Read the Chronicle. Background imagery: forest roads, Greenvale, Oakenfell, adventurers beginning a journey. No dragons, demons or high-level content. The focus is beginnings and exploration.

**About Hollowmere.** Brief overview. Suggested content: "Legends of Hollowmere is a persistent online fantasy world where players carve their own path through exploration, guilds, crafting, faction politics, and adventure. Begin your journey in the village of Vincent's Hollow. Travel the roads of Greenvale. Train within the Hall of Legends. Earn Renown. Become part of the Chronicle. Create your legend."

**World overview** (public information only). World: Hollowmere. Starting region: Greenvale, "a prosperous frontier region of farms, forests, villages, and ancient mysteries." Starting village: Vincent's Hollow, "a quiet village where every adventurer's story begins." First major city: Oakenfell, "the largest city in Greenvale and home to the Hall of Legends." Include a simple world map illustration. Do not reveal hidden locations, secret dungeons or future regions.

**Features.** The core public systems:
- *Choose Your Path*: five archetypes: Warrior, Scout, Priest, Mage, Adventurer.
- *Grow Your Legend*: advance from archetype to advanced classes; future specializations await discovery.
- *Living History*: player actions influence the Chronicle; the world remembers achievements.
- *Factions*: support the Assembly, protect the wilds, build wealth, forge your own loyalties.
- *Craft and Gather*: explore Greenvale collecting resources, craft valuable goods, support the economy.
- *Discover Secrets*: the world contains hidden stories, locations and mysteries. Do not describe any specific hidden content.

**Hall of Legends.** Public class advancement: "At Level 5 adventurers may travel to Oakenfell and swear themselves to a profession within the Hall of Legends." Warrior paths: Knight, Berserker, Monk. Scout paths: Ranger, Rogue, Bard. Priest paths: Cleric, Templar, Oracle. Mage paths: Mage, Druid, Necromancer. Adventurer paths: Spellblade, Beastmaster, Artificer.

**Factions** (public information only, brief descriptions): Greenvale Assembly, Merchant Consortium, Riverwatch Rangers, Thorn Pact. Do not reveal secret factions. Do not reveal the Circle of Ash.

**Chronicle** (one of the most important sections). The top 5 Chronicle events, ranked by Event Significance Score, queried from the Chronicle records and sorted descending. For each: event name, date, ESS score, summary, link to the full Chronicle entry. Example layout: "1. The Fall of Blackstone Peak, ESS: 2750". Future entries populate the section automatically. No hardcoded data. The Chronicle acts as a content engine for the homepage.

**Hall of Heroes.** The most renowned living characters: name, title, Renown tier, primary class. Top 10. Updates automatically.

**Latest news.** World events, patch notes, community announcements, seasonal activities. Designed for future expansion.

**Join the adventure.** Calls to action: Create Account, Download Client (future), Enter Hollowmere.

**Footer.** Links: Chronicle, Hall of Heroes, World Map, Rules, Discord, Support. "© Legends of Hollowmere".

## Design style

Visual theme: Greenvale Frontier Fantasy. Palette: forest green, warm brown, parchment, copper, dark oak. Avoid neon colors, sci-fi themes and modern game aesthetics.

## Technical requirements

Modern responsive design, mobile-first, fast loading, SEO-friendly, accessible, server-rendered where possible.

## Dynamic data sources

Database-backed sections: Chronicle, Hall of Heroes, news, character statistics, world statistics. They update automatically without code changes.

## Public information rules

Only display what a citizen of Hollowmere could reasonably know.

- Publicly known: Hollowmere, Greenvale, Vincent's Hollow, Oakenfell, the Hall of Legends, the public factions, the classes, Chronicle entries.
- Do not reveal: hidden dungeons, secret factions, secret passages, hidden recipes, future regions, unknown prestige classes, world mysteries, unreleased content.

The website creates curiosity without spoiling discovery. The world should feel larger than what is shown. Visitors should leave believing there are countless stories waiting beyond Vincent's Hollow.
