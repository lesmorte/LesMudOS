# The world

Facts about the game world that the owner has fixed. Area briefs and system designs must agree with this page.

| | |
|---|---|
| Game | Legends of Hollowmere |
| Website | legendsofhollowmere.com |
| Engine | LesMudOS (this repository: the driver and the standard mudlib) |
| Starting region | Greenvale |
| Starting village | The Village of Vincent's Hollow |
| First major city of Greenvale | Oakenfell. Not yet built. The Hall of Legends is there. |
| Where advanced classes are chosen | The Hall of Legends, in Oakenfell (called the "All Hall" in the class brief and in earlier notes) |

## Structure

- The world is divided into regions. A region contains areas: villages, cities, dungeons, roads.
- Every room belongs to one area and, through it, to one region. Both names are data on the area, not text repeated in each room.
- Greenvale is the first region. Its areas so far are The Village of Vincent's Hollow (levels 1-5) and The Forgotten Cellars beneath it (levels 2-5). See [areas/vincents-hollow.md](areas/vincents-hollow.md).
- The village has six rooms more than its original twelve (November 2026): Fletcher's Lane, The Strung Yew, The Armourer's Shed, The Shrine of the Quiet Hand, The Hedge-Scholar's Loft and The Practice Yard, with Wenna Farrow, Oswy Thackett, Sister Merewyn, Halbert Quill and Brann Odell. Their rooms, shops, items and quest are in the last section of the village's area document.

## Leaving the village

- A new character cannot leave the main area of Vincent's Hollow. The road north is closed to them.
- Completing the first quest, "Clear the Cellar", grants a travel pass. With it, the road north out of Vincent's Hollow is open, and leads toward Oakenfell, the first major city of Greenvale.
- The pass replaces the earlier idea of a key to a hall inside the village. The Hall of Legends is in Oakenfell.
- Until Oakenfell is built, the road beyond the village is a short stretch that ends with the way ahead not yet open.

## Names in the game

- The game introduces itself as Legends of Hollowmere (the login banner, `who`, help).
- The village is "The Village of Vincent's Hollow" where a full name is called for (area name, the pass, arrival messages). Text quoted from the area brief, such as the sign on the North Road, keeps the brief's wording.

## Systems from the design bible

The owner's design bible ([specs/design-bible.md](specs/design-bible.md)) adds five connected systems. None is designed or built yet.

| System | What it is |
|---|---|
| Faction Reputation | A standing with each major faction, from Hostile to Legendary, earned and lost through quests, deeds, discoveries and donations; rival factions pull against each other; tiers unlock quests, vendors, equipment, titles |
| Factions of Greenvale | The Greenvale Assembly, the Hall of Legends, the Merchant Consortium, the Riverwatch Rangers, the Thorn Pact, the Free Companies, the Circle of Ash |
| Renown | One global measure of how widely a character is known, from Unknown to Mythic, earned by memorable deeds; NPCs react to it; Infamy is tracked separately |
| The Chronicle | The permanent history of the world, kept by the Hall of Legends: narrative entries for discoveries, deeds, victories, server firsts, with a Hall of Heroes, a Hall of Legends and a Hall of Infamy |
| Event Significance Score | A score for every event that decides whether it is forgotten, becomes gossip, earns Renown, or enters the Chronicle |

At launch, Vincent's Hollow has strong Assembly influence, moderate Merchant Consortium influence, a minor Riverwatch presence, hidden Circle of Ash activity and occasional Thorn Pact interaction.

## Names, settled by the owner

- The city is Oakenfell. (The design bible and the ESS specification write "Oakenfall"; those documents are kept as supplied, and the game uses Oakenfell.)
- The region is Greenvale, one word, as in the Greenvale Assembly.
- The owner's city design document for Oakenfell is [specs/oakenfell-city-design.md](specs/oakenfell-city-design.md). The room-by-room plan drawn from it, awaiting the owner's review, is [areas/oakenfell.md](areas/oakenfell.md).
- The river through Oakenfell is the River Mere. The city's motto is "From Strong Roots Grow Great Things".
