# The world

Facts about the game world that the owner has fixed. Area briefs and system designs must agree with this page.

| | |
|---|---|
| Game | Legends of Hollowmere |
| Website | legendsofhollowmere.com |
| Engine | LesMudOS (this repository: the driver and the standard mudlib) |
| Starting region | Green Vale |
| Starting village | The Village of Vincent's Hollow |
| First major city of Green Vale | Not yet named or built. The All Hall is there. |

## Structure

- The world is divided into regions. A region contains areas: villages, cities, dungeons, roads.
- Every room belongs to one area and, through it, to one region. Both names are data on the area, not text repeated in each room.
- Green Vale is the first region. Its areas so far are The Village of Vincent's Hollow (levels 1-5) and The Forgotten Cellars beneath it (levels 2-5). See [areas/vincents-hollow.md](areas/vincents-hollow.md).

## Leaving the village

- A new character cannot leave the main area of Vincent's Hollow. The road north is closed to them.
- Completing the first quest, "Clear the Cellar", grants a travel pass. With it, the road north out of Vincent's Hollow is open, and leads toward the first major city of Green Vale.
- The pass replaces the earlier idea of a key to an All Hall inside the village. The All Hall is in the city.
- Until the city is built, the road beyond the village is a short stretch that ends with the way ahead not yet open.

## Names in the game

- The game introduces itself as Legends of Hollowmere (the login banner, `who`, help).
- The village is "The Village of Vincent's Hollow" where a full name is called for (area name, the pass, arrival messages). Text quoted from the area brief, such as the sign on the North Road, keeps the brief's wording.
