# Area plan: Oakenfell

The owner's [city design document](../specs/oakenfell-city-design.md) describes Oakenfell by district and landmark. This plan turns it into rooms, exits, people and services that can be built. Everything in the design document is treated as fixed. Everything this plan adds (street names, minor NPCs, which doors exist) is a proposal and is marked as such where it matters.

Status: draft for the owner's review. Not built. It is built with class system stage B, because the Hall of Legends is where advanced classes are chosen.

## 1. Principles for the build

- **Lived-in before useful.** Each district has streets, homes and working people as well as the buildings players need. Citizens have homes; roads lead somewhere.
- **Safe.** The whole city is safe ground: no combat inside the walls. The city watch are present and peaceful.
- **Scale by contrast.** Vincent's Hollow is 8 rooms. Oakenfell's first build is about 45, with five gates and roads out. A player arriving from the village should feel the difference at the gate.
- **Honest doors.** A building the game cannot yet do anything with (the auction house, the crafting guilds, the Assembly's politics) is present, staffed and describable, and its people say plainly what it is for and that its business is not open to newcomers yet. No dead ends without a reason.
- **Every road suggests more.** Each gate has a road beyond it with one room of travel and a named destination that is not yet open.
- **History everywhere.** Each district carries one or two details tied to the Great Oak and the motto, "From Strong Roots Grow Great Things".

## 2. The way there

The Vale Road, built in class stage A as a single room, becomes a short journey so that arriving is an event:

| Room | Notes |
|---|---|
| Vale Road, south (exists) | North of Vincent's Hollow's North Road. Needs the travel pass. |
| Vale Road, the long meadows | Farms and a waystone carved with an oak. Wandering wolf encounter. |
| Vale Road, river bend | First sight of the River Mere. A Riverwatch Ranger on patrol (peaceful, talks about the roads). |
| Vale Road, the rise | The first view of Oakenfell: walls, the river, and the roofs of the Hall of Legends. The description is the "wonder" moment. |
| Southgate approach | Carts, travelers, the city wall. North into the city. |

## 3. Map

```
                         [Northgate] -> the Northern Road (to the mountains)
                              |
        GUILD QUARTER --- FOUNDER'S DISTRICT --- RIVER DISTRICT -> [Rivergate] -> downriver
                              |                       |
   [Westgate] <- MARKET DISTRICT ------------------ (Mere Bridge) -> [Eastgate] -> Watchfort Crossing
     (the Elderwood)          |
                        SOUTHGATE DISTRICT
                              |
                         [Southgate] -> the Vale Road -> Vincent's Hollow
```

## 4. Districts and rooms

### Southgate District (rough but welcoming)

| Room | Exits | What is here |
|---|---|---|
| Southgate | south to the approach; north to Wayfarer's Row | City watch; a readable board of city rules and the motto over the arch |
| Wayfarer's Row | Southgate; north to Stag Lane; east to the stables; west to the Free Lodge | Traveler housing, porters, a street food seller (food) |
| The Silver Stag Inn, common room | Stag Lane; up to rooms | Innkeeper (food, drink, a bed); the city's gossips for rumors and news; a quest starter |
| The Silver Stag Inn, rooms | down | Safe room, logout room, and the respawn point for anyone who dies beyond the village |
| Stag Lane | Wayfarer's Row; the inn; north to the Market District | |
| Southgate Stables | Wayfarer's Row | Stablemaster; horses to look at; says mounts are for those the city knows better (future) |
| The Free Lodge | Wayfarer's Row | A mercenary lodge of the Free Companies: a recruiter and off-duty sellswords; faction contact |
| The Outfitter | Wayfarer's Row | Adventurer services: a shop for potions, rations and general gear |

### Market District (busy, crowded, noisy, colorful)

| Room | Exits | What is here |
|---|---|---|
| Greenvale Market, south rows | Stag Lane; north rows; west to Coin Street | Stallholders; an armorer's stall (shop: armor of each category) |
| Greenvale Market, north rows | south rows; north to Founder's Square; east to Mere Bridge | A weaponsmith's stall (shop: one better weapon in every category); a herbalist who is quietly a Thorn Pact sympathizer |
| Coin Street | the market; the bank; the Consortium House; the auction house; west to Westgate | Permanent storefronts |
| The Oakenfell Bank | Coin Street | Banker. Deposit and withdraw gold; banked gold is not lost on death (a small new service; see section 6) |
| Consortium House | Coin Street | The Merchant Consortium's seat in the city: a factor at a desk; faction contact |
| The Auction House | Coin Street | An auctioneer between sales; auctions are not yet open |
| Caravan Office | Coin Street | A caravan master; talks of routes and of trouble on the western road |
| Westgate | Coin Street; west to the Westwood Road | City watch; the road toward the Elderwood, not yet open |

### Founder's District (traditional, respectable, historic)

| Room | Exits | What is here |
|---|---|---|
| Founder's Square | south to the market; north to Northgate Street; east to Old River Street; west to Guild Row; City Hall; Assembly Hall | The civic center: a notice board (public notices, readable), a speaker's stone, a town crier (news); a mosaic of the Great Oak underfoot |
| City Hall | Founder's Square | Clerks; the city's records of citizens; a clerk who explains the districts |
| Assembly Hall, the public gallery | Founder's Square | The Greenvale Assembly's meeting place: a steward; delegates arguing in the chamber beyond; faction contact. Political content is not yet open. |
| Old Stone Street | Founder's Square (as Northgate Street); Elder Lane; Northgate | Historic homes; a retired adventurer on a bench |
| Elder Lane | Old Stone Street | A dead-end lane of the oldest houses, ending at an ivy-grown wall. See the Hidden Grove. |
| Northgate | Old Stone Street; north to the Northern Road | City watch; the road toward the mountains, not yet open |

### The Hidden Grove (inside the Founder's District)

| Room | Exits | What is here |
|---|---|---|
| The Overgrown Way | Elder Lane (hidden); the grove | A path between old walls, mossed stones |
| The Hidden Grove | the Overgrown Way; the Great Stump | Birdsong, quiet, offerings left on a stone; a caretaker who says little |
| The Great Stump | the grove | The petrified stump, forty to sixty feet across: lightning scars, ancient carvings, memorial plaques and historical markers, each readable; roots running under the walls |

Finding it:

- There is no exit shown in Elder Lane. `look at wall` and `look at ivy` describe a gap; `search wall` (or `push ivy`) reveals it for that character, who can from then on go `through` it.
- Local knowledge also opens it: the retired adventurer on Old Stone Street and the oldest regular at the Silver Stag each give a hint if asked about the oak, and a hint is enough.
- A second entrance exists and is not usable yet: a rusted garden door seen from inside the grove, said to open onto the Guild Quarter. The document allows several entrances; one more is left to find later.
- Finding the Great Stump, Oakenfell's namesake, is recorded in the Chronicle as a first: the first character ever to stand before it gets a featured Chronicle entry and is named among the recorded firsts (decided by the owner; CHRONICLE.md section 4). Every later finder earns the Renown of a hidden discovery. So the grove is findable from the first build.
- The legends in the design document (the roots still live, spirits gather, Druids hear the tree whisper, passages lie beneath) are told by the caretaker and by townsfolk, and nothing in the first build confirms or denies them. Nothing can be dug, opened or entered at the stump yet.
- A scholar in the Athenaeum who asks too many questions about the oldest parts of the city is the Circle of Ash's interest in the grove. He is never named as such.

### Guild Quarter (aspirational, scholarly, ambitious)

| Room | Exits | What is here |
|---|---|---|
| Guild Row | Founder's Square; the Hall of Legends; the Athenaeum; Crafters' Court; the Proving Yard | Banners of the guilds; apprentices hurrying |
| Hall of Legends, the Great Doors | Guild Row; the Hall of Callings | A timber and stone front that looks like many buildings joined over generations; a doorwarden who greets newcomers by Renown |
| Hall of Legends, the Hall of Callings | the Great Doors; the Chronicle Gallery; the Envoys' Gallery | The five hallmasters and the fifteen banners. Advanced classes are chosen here (CLASS_SYSTEM.md section 8). |
| Hall of Legends, the Chronicle Gallery | the Hall of Callings | The Grand Chronicle on its lectern, the Halls of Heroes, Legends and Infamy as carved walls, and the Renown records; a chronicler (CHRONICLE.md) |
| Hall of Legends, the Envoys' Gallery | the Hall of Callings | Faction contacts: a representative each of the Assembly, the Consortium and the Riverwatch; guild representatives. They explain their factions and, once Faction Reputation exists, tell a character where they stand. |
| The Athenaeum | Guild Row | Library and archives: a librarian; shelves of readable histories (the founding, the storm, the motto); the inquisitive scholar |
| Crafters' Court | Guild Row | The crafting guilds' shared yard: smiths, weavers, a guild clerk. Crafting is not yet open. |
| The Proving Yard | Guild Row | Trainers at their drills: a place to learn that trainers will matter. A training dummy that can be attacked to try skills (the one exception to "no combat": it cannot fight back or die). |

### River District (industrial, working-class, practical)

| Room | Exits | What is here |
|---|---|---|
| Old River Street | Founder's Square; the quay | Warehouses, carters |
| Mere Bridge | Greenvale Market north rows; Eastgate; the quay below | The stone bridge over the River Mere; anglers |
| The Mere Quay | Old River Street; Mere Bridge; the fish market; the shipwright; the Riverwatch office; Rivergate | The docks: barges, dockhands, a harbormaster |
| The Fish Market | the quay | Fishmongers (food); gossip of the river |
| Shipwright's Yard | the quay | A hull on the slip; a shipwright who talks of the river trade |
| River Trade Office | the quay | Clerks and manifests; passage downriver is not yet sold |
| Riverwatch Office | the quay | The Riverwatch Rangers' city office: a duty sergeant and a wall map of Greenvale's roads; faction contact |
| Rivergate | the quay; downriver | A water gate and landing; the river road, not yet open |
| Eastgate | Mere Bridge; east to the Crossing Road | City watch; the road to Watchfort Crossing, not yet open |

## 5. People

About thirty named NPCs, each with a main line, one or two more, and topics. Named in the build, not here, except where the design document or the bible fixes something:

- The five hallmasters, one per archetype.
- The chronicler, the doorwarden and the three envoys in the Hall of Legends.
- The innkeeper of the Silver Stag, and two or three regulars who are the city's rumor source.
- One faction contact in each faction's own house: the Assembly's steward, the Consortium's factor, the Riverwatch sergeant, the Free Companies' recruiter.
- The Thorn Pact has no house: the herbalist in the market speaks for it only to those who ask the right things.
- The Circle of Ash has no one: only the scholar's questions.
- The grove's caretaker.
- Shopkeepers: outfitter, armorer, weaponsmith, food sellers, banker.
- City watch at each gate, a town crier, clerks, dockhands and citizens as unnamed, peaceful presence with idle behaviour, so that streets are not empty.

## 6. What the city needs from the systems

| Need | From | State |
|---|---|---|
| Advanced class choice | Class system stage B | Designed; waits on this city |
| The Grand Chronicle, Renown records, rumors | Chronicle pass 1 | Designed, queued |
| Faction contacts who know a character's standing | Faction Reputation | Not yet designed; until then the envoys only explain their factions |
| Hidden exits found by searching | New, small | A room feature: an exit that is not listed or usable until a character has found it |
| A bank | New, small | Deposit and withdraw; banked gold is saved with the character and is not subject to the death penalty |
| A second respawn point | Small change | A character who dies respawns at the inn of the last settlement where they rested, not always in Vincent's Hollow |
| Shops with better gear | Existing shop system | A tier of weapons and armor above the village's, in every equipment category, priced for level 5 and up |
| A training dummy | Small | A creature that cannot die or fight back |

## 7. Not in the first build

Mounts, auctions, crafting, housing, political quests, the grove's secrets, the crafting guilds' and trainers' real functions, passage by river, and everything beyond the gates. Each has a place and a person in the city already, so that adding it later is filling a room, not building one.

## 8. Questions for the owner

1. Is about 45 rooms the right first size, or should the first build be smaller (the route from Southgate to the Hall of Legends, with the other districts as one street each)?
2. Is the whole city safe ground, as planned here?
3. (Settled: the Hidden Grove is findable in the first build, and finding the Great Stump is a recorded first.)
4. The bank and the second respawn point are additions of mine. Keep them?
5. The gates name four destinations: the Elderwood (west), the mountains (north), Watchfort Crossing (east, from the bible) and downriver. Are these the directions the world should grow in?
