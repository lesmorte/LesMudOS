# Corridor lore review: what the writers invented

Status: for the owner's decision. Nothing in this area is live yet. All file paths below are relative to `mudlib/domains/greenvale/corridor/` and end in `.cs`; every quotation is the text exactly as a player would read it, with line breaks restored, plain quotation marks and the colour tags removed.

## 1. What this is and how to answer

Seven writers built the road from Vincent's Hollow to Oakenfell: five road segments (South Road, Eldergreen Edge, Green Road, Heartwood, Oakenfell Approach, 11 main-road rooms and 9 or 10 side rooms each) and two dungeons (the Root Warrens, 12 rooms; the Forgotten Barrow, 10 rooms). That is 122 rooms, 40 people, and 11 small items, in 173 files. They worked separately from your brief and a build plan, and without seeing each other's work, yet several of them invented the same storylines, and a good deal of lore that is not in your documents. Nothing is live. You decide what stays.

How to answer: every thread in section 2 ends with numbered questions (Q1.1, Q1.2 and so on), and every checklist line in section 4 and every problem in section 5 has a number. For each you can reply with one word:

- **keep**: it is canon as written.
- **change**: say what to change (a sentence is enough; the writers will do the rest).
- **cut**: remove it.

If you read only section 2 you will have made the decisions that matter most. Section 3 is every inscription and readable in full, in road order. Section 4 is a checklist of every invented fact. Section 5 lists errors and conflicts I found. Section 6 lists the people.

Two things were checked for you. First, the words Circle of Ash appear nowhere in the area (details in thread 2.2). Second, no passage anywhere has a person say the words Thorn Pact aloud, though five people will answer if the player types them (thread 2.6).

## 2. The big threads

Nine storylines run across segments, plus seven smaller ones I found that you did not list (2.10). Each gives: what a player pieces together; where it appears; the key passages in full; conflicts and loose ends; questions.

---

### 2.1 The older people

**What the player pieces together.** Before Oakenfell (founded "nearly four hundred years ago" by your history) another people built in a green-grey stone that is not found anywhere in Greenvale: no mortar, joints as narrow as a hair, pointed arches, walls that lean inward, and carving in groups of seven. Their carvings show a procession of tall figures with blank faces. They used three signs, a ring crossed by a bar, a wavering line like water, and a ring open to the north. They left a broken arch, a wall and court in the Heartwood, a paved road, an aqueduct whose arches "go from nowhere to nowhere", a sealed culvert, a shaft under the goblin warren, and a burial place in which the carved procession slowly lowers its arms until the border finally closes into a ring. Nobody alive knows who they were, and no writer gave them a name, as your plan required.

**Where it appears.**

- Heartwood: The Fallen Arch (`heartwood/m02.cs`) (the arch), The Heartwood Road, the Root Stair (`heartwood/m03.cs`) and The Heartwood Road, the Moss Mile (`heartwood/m04.cs`) (wall glimpsed, a cut face under moss), The Mossgate Ruins (`heartwood/m05.cs`) (the wall and gateway), The Heartwood Road, the Paved Remnant (`heartwood/m09.cs`) (the paving), The Mossgate Ruins, the Inner Court (`heartwood/w1.cs`) (the inner court, seven doorways, the procession), The Collapsed Shrine (`heartwood/d3.cs`) (the Collapsed Shrine), The Hidden Cache (`heartwood/s1.cs`) (the cache is built against the old wall).
- Root Warrens: The Broodmother's Nest (`root_warrens/nest.cs`) (the collapsed shaft and a carved lintel) and The Diggers' Junction (`root_warrens/junction.cs`) (the overseer's count, "stone that is not our stone").
- Oakenfell Approach: The Approach Road, under the Old Aqueduct (`approach/m06.cs`) (the aqueduct arch and its band of marks), The Old Aqueduct, the Broken Span (`approach/w1.cs`) (the Broken Span, seven pale stones in a ring), The Aqueduct Cistern (`approach/d1.cs`) (the cistern with a pointed arch filled in), The Sealed Culvert (`approach/s1.cs`) (the sealed culvert), The Veterans' Den (`approach/d3.cs`) (a slab of "old masonry"), and the item `approach/obj/old_coins.cs` (coins stamped with a ring and a bar).
- The Barrow: every room. The grave token `barrow/obj/grave_token.cs` is cut with the ring crossed by a bar.
- People who talk about it: Marcella, a scholar of the Athenaeum (`heartwood/npc/marcella.cs`), Cassian, a Riverwatch hunter (`heartwood/npc/cassian.cs`), Kenrick, a grizzled veteran (`south_road/npc/veteran_kenrick.cs`), Constance Ardley, a Riverwatch ranger (`approach/npc/ranger.cs`), Florian Thatch, an old farmer (`approach/npc/old_farmer.cs`), Lorcan Pike, a drover (`approach/npc/drover_lorcan.cs`), Oswin Hale, an Assembly guard (`approach/npc/toll_guard.cs`).

**Key passages.**

The Fallen Arch (`heartwood/m02.cs`), what a player sees on arriving:

> Two tall piers of dark stone stand on either side of the road, and between them lies what was once their arch, broken into heavy wedge-shaped blocks sunk in moss. The stone is a green-grey that no wall in the region shares, and the blocks are fitted so closely that you cannot slide a knife blade into the joints that remain. The road runs through the gap as though the arch had never been there. Cold comes off the stone even at noon.

The Fallen Arch (`heartwood/m02.cs`), look at the joints:

> No mortar was used. Each stone has been shaped to its neighbour, and where the joints are open, they are narrower than a coin. Whoever built it did not guess the weight of the arch; they knew it. The stone is not local. There is no quarry within a week's walk that gives this green-grey.

The Fallen Arch (`heartwood/m02.cs`), read the carving:

> You trace the carved band with a finger. The strokes are cut deep and clean, and they repeat in groups of seven before a stroke changes, and then begin again. It looks like writing, or like a border, and nothing about it matches the marks on any stone, stall or coin that you know. Near the base of the east pier the pattern breaks off, and the last few groups are worn flat by weather.

The Heartwood Road, the Paved Remnant (`heartwood/m09.cs`), look at the paving:

> The slabs are of the same green-grey stone as the arch. Each is cut to a long rectangle and fitted to the next without mortar. They are laid in a pattern of long and short that repeats every seventh course. The middle of the road is worn into a shallow trough by feet, not by wheels, and the worn part is a pace wide.

The Heartwood Road, the Paved Remnant (`heartwood/m09.cs`), look at the gutter:

> The gutter on the east side is a groove a hand wide, still running in wet weather. It carries the rain off the paving and into the roots, and it has done so for longer than there has been a city.

The Mossgate Ruins, the Inner Court (`heartwood/w1.cs`), look at the carved band:

> The carved band is a hand and a half deep and runs unbroken round the three walls. It shows a procession of long thin shapes, taller than any man, each with one arm raised, between borders of the same interlaced line that is on the arch. The faces are blank. You could read the band, though no one has read it in a long time.

The Mossgate Ruins, the Inner Court (`heartwood/w1.cs`), read the band:

> You follow the band round the court. The procession goes always one way, with the raised arms, and every seventh figure carries something that might be a staff or a branch. At the end, where the band should close, it does not. The last figure has no arm raised, and the border under it stops in an unfinished line. There are no letters. The carving is crisp wherever the roof once shielded it.

The Broodmother's Nest (`root_warrens/nest.cs`), look at the shaft:

> The fallen earth fills what was once a shaft, the full width of the back wall, and heaped in it are broken blocks of dark green-grey stone, squared and fitted with no mortar. This is not goblin work. A single great lintel stands out of the fall, carved with a band of worn lines, and past it there is only rubble, packed tight and mounded to the dome. The rats' runs go into it and stop. Whatever lies below, there is no way through.

The Approach Road, under the Old Aqueduct (`approach/m06.cs`), look at the marks:

> The marks are small, regular and repeated, a ring, a bar, a wavering line, and they have worn so shallow that you feel them more than see them. They are not letters of any hand you know, and they are not masons' marks of the city.

The Approach Road, under the Old Aqueduct (`approach/m06.cs`), the plaque:

> THIS ARCH IS KEPT SOUND FOR THE SAFETY OF TRAVELERS BY ORDER OF THE GREENVALE ASSEMBLY. Do not climb the piers. Do not tie beasts to the stonework. The Assembly claims no knowledge of its builders and no right to its water.

The Old Aqueduct, the Broken Span (`approach/w1.cs`), look at the circle:

> Around the foot of one standing pier lie seven pale stones, laid in an exact ring. No ivy touches them, and the grass is cropped short around them, as if by something that respects the circle. If they mean anything, nobody here can tell you what.

The Aqueduct Cistern (`approach/d1.cs`), the filled arch:

> In the low corner the floor drops to a shallow sump, and beyond it a pointed arch has been filled solid with blocks of a paler, finer-grained stone, fitted so closely that no mortar shows. At the very top of the filling one block has slipped, leaving a black gap a person could wriggle through. A thin cold air breathes out of it, dry, as if from a place that is a long way down, and a careful person might well go down into it.

The Sealed Culvert (`approach/s1.cs`), the marks in the culvert:

> The marks are cut in rows along the wall at shoulder height, small and regular. Three recur: a ring crossed by a bar; a single wavering line, like water; and a ring with a gap in it, open to the north. Between them are columns of short strokes, always in groups of seven. They are not letters of the city, nor of any tongue the Assembly keeps. The ring-and-bar is the same sign that is worn into the piers of the arch on the road.

The coins, `approach/obj/old_coins.cs`:

> A little drift of thick, dull coins lies in the dust, stamped on one face with a ring and a bar and on the other with nothing at all. No mint in Greenvale made them, but gold is gold.

The Sunken Forecourt (`barrow/forecourt.cs`), read the band:

> The band begins at the foot of the stair, as though it had come down it. The figures are the long, thin figures of the court in the wood, with the same blank faces and the same single raised arm, and they walk in sevens toward the doorway. Every seventh carries a branch, but here the branch is bare and bound with a cord at its middle. Above their heads the border that broke off unfinished in the court runs on without a flaw. There are no letters.

The Long Gallery (`barrow/long_gallery.cs`), read the band:

> Forty-two figures walk north along the two walls, and you count them, because the count is so plainly meant. In each group of seven one figure has let its raised arm fall, and the group after it has two, and so on, until at the northern end of the gallery there are no raised arms at all, and the figures walk with their hands open at their sides. Their faces are blank to the last. The border above them runs on, not yet closed.

The Inscribed Tomb (`barrow/inscribed_tomb.cs`), read the panel:

> Three rows of interlaced strokes run the width of the panel, each in seven groups. Above them stand three signs, one over each row: a ring crossed by a bar, a single wavering line like water, and a ring with a gap open to the north. The strokes are cut very deep and are as sharp as on the day they were made. You cannot read them, and it seems that no one could have read them in a very long time. Whoever came before you could not, either, or they would not have needed a copy to carry away.

The Tomb of the Seven Bowls (`barrow/bowl_tomb.cs`), read the walls:

> The carving is one scene, repeated on both walls. A single figure lies on a bier, and seven others stand about it, each holding a bowl in the hand that is not raised. A branch, bound with a cord, is held at the head. At the feet stands a last figure whose arm is not raised, as it was not in the court in the wood. The faces are as blank as ever. Nothing says who lies on the bier.

The Hall of the Standing Guard (`barrow/guardian_hall.cs`), read the band:

> The procession ends here. The last seven figures have turned about, so that they stand with their backs to the great door and their faces toward the gallery, toward you; their arms are down and their hands are open. Above them the border that was unfinished in the court in the wood, and ran on through every passage since, bends at last and meets itself, a closed ring. It is the only closed ring in the barrow. The faces, to the end, are blank.

Marcella, a scholar of the Athenaeum (`heartwood/npc/marcella.cs`), asked about the builders ("old"), the carving ("arch") and the stone:

> Oakenfell's founding was nearly four hundred years ago, and everything in the Athenaeum agrees on that. This is older. How much older, I cannot say, and no record that I have seen mentions it at all. I wish I could tell you more.

> The pattern repeats in sevens and then varies. That is exactly how writing behaves, or music, or a border made by someone who loved counting. I cannot tell which. I have copied nine hundred strokes and found no stroke that I recognise.

> Not quarried anywhere in Greenvale that I know of. I took a chip, as the Athenaeum asked. It would not burn, would not dissolve in vinegar, and is lighter than it should be. That is all I know.

Florian Thatch, an old farmer (`approach/npc/old_farmer.cs`), asked about the aqueduct:

> My grandmother used to say the water went somewhere else, before the city. That it ran to a place that isn't there now. The arches go from nowhere to nowhere, and she said that was the point.

**Conflicts and loose ends.**

- The three signs are described three ways. The culvert (The Sealed Culvert (`approach/s1.cs`)) says the three that recur are "a ring crossed by a bar; a single wavering line, like water; and a ring with a gap in it, open to the north", and adds that the ring-and-bar "is the same sign that is worn into the piers of the arch on the road". But the arch itself (The Approach Road, under the Old Aqueduct (`approach/m06.cs`)) says its marks are "a ring, a bar, a wavering line": a ring and a bar as two separate signs, and no ring open to the north. The coins and the grave token say "a ring and a bar" and "a ring crossed by a bar". Please settle the three signs once.
- The stone changes. It is "green-grey" in the Heartwood, the Collapsed Shrine, the Root Warrens shaft and the Barrow. At the aqueduct arch it is "dark, weathered stone"; in the cistern the fill is "a paler, finer-grained stone"; in the culvert the blocks have "a faint sheen like glazed pottery". One people, or several?
- The shape changes. The Fallen Arch is pointed ("unlike the round arches of Oakenfell") and the Barrow vaults are pointed, but the culvert roof is "a perfect half-circle" and the cistern vault is "a single unbroken curve". Either the aqueduct is by a different or later builder, or this is an accident of writing.
- Two walled-up openings are described differently: in the cistern a pointed arch is "filled solid with blocks of a paler, finer-grained stone" with no mortar; in the culvert behind it the far end is bricked up by "the Oakenfell masons" with "rough blocks bonded with grey mortar". They may be two different walls; a player will likely read them as one.
- The aqueduct's geometry disagrees with itself. At The Approach Road, under the Old Aqueduct (`approach/m06.cs`) the arches stride "from the west" and "breaks off in the fields to the east"; at The Old Aqueduct, the Broken Span (`approach/w1.cs`) the channel "runs north-east and out of sight, toward the city"; from the Rise (The Approach Road, the Rise (`approach/m10.cs`)) the last arches end "a mile short of the walls". The Assembly's own plaque and the toll guard say it is older than the city and no one knows who built it, yet Oakenfell's masons walled up a culvert in "long-ago days": so the city knew the culvert existed. That is a fine mystery but is not stated as one.
- The Barrow forecourt dish says it is "smaller than the great dish in the court above". The great dish is in the Mossgate court in the wood, not above the Barrow (see problem 5.13).
- The Crossroads Stone (The Crossroads Stone (`green_road/m11.cs`)) calls the east track "THE OLD WAY. NO CARTS." and Jocosa says "I took it once. I got out." There is no room on that track in the build, so the Old Way leads nowhere yet.
- The Root Warrens junction plank says a digger "hears scratching under" the old stone. Nothing answers this anywhere, including the Barrow. It reads as an unresolved hook.
- Nothing in the area says who is buried in the Barrow. That matches your plan (given no name the bible does not give).

**Questions.**

- **Q1.1** Are these one lost people (so the stone, shape and signs should agree everywhere) or do you want more than one? If one: should I have the aqueduct and culvert made pointed and green-grey?
- **Q1.2** Keep the writers' vocabulary as fixed canon: stone that is "not local", groups of seven, three named signs, a procession of blank-faced figures? It now appears in 25 rooms and two items. If you keep it, which description of the three signs is the right one?
- **Q1.3** Is the closed ring at the end of the Barrow (a "closed ring", "the only closed ring in the barrow") something you want to keep as a deliberate clue for later, or should the carvings never resolve?

---

### 2.2 The searchers (the Circle of Ash, never named)

**What the player pieces together.** Somebody has been through this country before the player, methodically and with great care to leave nothing. Years ago, in the Green Road watchtower, a strange mark appeared on a cellar wall while the garrison was there. More recently someone searched the Mossgate ruins for something "not set down where they said", gave out that they drew for the Athenaeum, burned their journals and maps in a hidden cleft in the thorn, and then went into the Forgotten Barrow: they chalked a small mark by the door, took a rubbing of the great inscription, opened and resealed a tomb, burned a page on a shelf, swept their own tracks, and dropped a pencil note that shows there were at least two of them.

**The search you asked for.** I searched every string in all 173 files, ignoring case, for the phrase circle of ash, for Ash with a capital letter, and for the word "circle". Circle of Ash appears nowhere. The capital word Ash appears nowhere. The only matches for "ash" are ordinary ash: a hearth swept clean of ash in the watchtower and cinders, plus the burned page's "ash" in the Barrow. The word "circle" appears only for rings of stone and similar shapes (the cellar's burned symbol, the Leaning Stones, the Broken Span stones), never as a society. Result: the Circle is never named, by anyone or anything.

**Where it appears.**

- Green Road: The Abandoned Watchtower (`green_road/w3.cs`) (the swept hearth and scuffed flagstones above the cellar), The Watchtower Cellar (`green_road/s1.cs`) (the burned symbol, the ledger, the path walked in the dust).
- Heartwood: The Hidden Cache (`heartwood/s1.cs`) (the Hidden Cache, reached from The Thorn Thicket (`heartwood/d2.cs`) where the thorn is cut in an arch), the item `heartwood/obj/scorched_satchel.cs`, the shrine (The Collapsed Shrine (`heartwood/d3.cs`)) whose gap has had the moss "rubbed away" at the lip, and Marcella, a scholar of the Athenaeum (`heartwood/npc/marcella.cs`) (a real scholar who has no connection to it).
- Barrow: The Mouth of the Barrow (`barrow/entrance.cs`) (soot smear, scuffed ramp), The Sunken Forecourt (`barrow/forecourt.cs`) (chalk mark), The Descending Way (`barrow/descending_way.cs`) (a single track, swept behind), The Resealed Tomb (`barrow/resealed_tomb.cs`) (lead seam and wax), The Low Niche (`barrow/low_niche.cs`) (burned page), The Inscribed Tomb (`barrow/inscribed_tomb.cs`) (the rubbing), The Long Gallery (`barrow/long_gallery.cs`) (the pencil slip), The Hall of the Standing Guard (`barrow/guardian_hall.cs`) (chalk ticks that stop three paces from the far door and cut bindings).

**Key passages.**

The Watchtower Cellar (`green_road/s1.cs`), look at the symbol:

> A circle about the size of a shield, burned into the stone with something hotter than any torch. It is broken at the top, with a short bar across the break and three dots beneath it. The lines are very fine and exactly even. It means nothing to you, but it makes your skin crawl a little.

The Watchtower Cellar (`green_road/s1.cs`), the path in the dust:

> The dust on the floor is thick except in one path, from the stair to the table to the wall. That path has been walked a good deal.

The Watchtower Cellar (`green_road/s1.cs`), read the garrison ledger (also quoted in thread 2.4):

> The last pages of the ledger, in a cramped, careful hand:
>
> ...day 61. No relief. Pay is five months owing. Stores sound. Sergeant Holm has forbidden anyone the lower stair after dark and will not say why.
>
> ...day 64. Two men gone from the night watch, their beds made. Holm says desertion. Their boots are by the door.
>
> ...day 66. Found the mark on the cellar wall. It was not there on the day we took stock. Holm says leave it be. I have copied it, and I am sorry I did.
>
> ...day 67. We go at first light. Holm will bar the door from within and go out by the roof. If any Ranger reads this: do not stay in the cellar after dusk, and do not trust the well.
>
> ...day 68. I do not think we are all going to...
>
> The entry ends there.

The Hidden Cache (`heartwood/s1.cs`), what a player sees:

> A cleft among the old stones, walled by the thorn on the one side and by the base of a very old wall on the other, so low that you must stoop to enter. The air is dry and bitter and smells of burning. The floor is dusted with black flakes, and in the corner stands a stack of books and papers that were set alight and not allowed to finish: their edges are charred to lace, and a satchel of scorched leather lies on top. Someone came here with a purpose, and then took pains that the purpose should not be known.

The Hidden Cache (`heartwood/s1.cs`), read the first journal:

> The paper is burned away on every side. What remains reads:
>
> "...third day in the court. The lowest course is as the old report said, and no one at the road-station has asked what I am cutting for. I have given out that I draw for the Athenaeum. [burned] ...should anyone come again I will say the same. Keep the sketches in the other book, and this one on me..."

The Hidden Cache (`heartwood/s1.cs`), read the second journal:

> Half the page is gone. What remains reads:
>
> "[burned] ...nothing under the arch, nor in the court, nor beneath the spring-stone. It was not set down where they said. If it was moved, it was moved before any of us. [burned] ...hollow trunks big enough to hide a cart; I will go through the thorn on the east side tomorrow. Burn the maps when they are copied. No one is to know that we were ever here..."

The Hidden Cache (`heartwood/s1.cs`), read the third journal:

> The last book was thrown on the heap, and the fire reached only the cover. The final page reads:
>
> "...the shrine in the deep wood is shut from within or fallen. I do not like what the cold air smells of. [burned] ...if it lies below, it is for those who come after me, and they will have better tools. Leave nothing. This book goes into the fire with the rest of--"
>
> The line stops there, in the middle of a stroke.

The Hidden Cache (`heartwood/s1.cs`), look at the satchel:

> The satchel is a plain leather bag with a buckled flap, scorched black on one side and whole on the other. It is empty except for a few flakes, and it has no mark, no name and no maker's stamp. The wearer was careful.

The item `heartwood/obj/scorched_satchel.cs`:

> A plain leather satchel with a buckled flap, burned black down one side and sound on the other. It is empty. There is no name, mark or maker's stamp on it, and the stitching has been cut and re-sewn once with a different thread.

The Sunken Forecourt (`barrow/forecourt.cs`), look at the chalk mark:

> Low on the left jamb of the doorway, no larger than your hand, is a mark in white chalk: a ring broken at the top, with a short bar across the break and three dots beneath it. The lines are fine and even, and the chalk is bright. It does not belong to the stone. It sits on the carving as a label sits on a jar, and whoever drew it knelt to do so and did not smudge it.

The Inscribed Tomb (`barrow/inscribed_tomb.cs`), look at the clean rectangle:

> The rectangle is about as large as a table-top and as sharply edged as if it had been ruled. The dust and the faint bloom that cover the rest of the panel are gone from it, wiped by something laid flat and pressed, over and over. It was not scrubbed. In the deepest grooves a few threads of white paper fibre have caught, and been left. It was copied.

The Resealed Tomb (`barrow/resealed_tomb.cs`), what a player sees:

> A square chamber, no larger than a farm kitchen, holds a single great chest of green-grey stone on a low plinth, its lid a slab as thick as a man is deep. The lid has been moved: the old tight joint is broken all round, and someone has run a bead of new pale lead along the seam to close it again, as neatly as a mason would, and a drop of pale wax has hardened on the plinth beside it. The floor is clean and cold, and the dust lies even except along a narrow track from the door to the plinth. Panels of the interlaced carving cover the walls from floor to vault.

The Low Niche (`barrow/low_niche.cs`), look at the ash:

> A single page was burned here, on the shelf, and left to burn out. The ash has kept the fold of the sheet, and in it the ghost of ruled lines and a column of small marks in a hand that is not the hand of the carvers. Not a word survives. A breath would take it, and so the draught from the stair has not been let in: the slab is as it was left.

The Long Gallery (`barrow/long_gallery.cs`), read the scrap of paper:

> A scrap of paper lies in the dust at the foot of the west wall, where it must have fallen from a sleeve. It is written in a quick, small, modern hand, in pencil:
>
> "Gallery 42, hall 7. The count is as it should be. Seven bowls east. Rubbing done, second sheet spoiled, third sheet clean. Far door: do not go through it without the others. You were right about the cold."
>
> There is no name.

The Descending Way (`barrow/descending_way.cs`), look at the floor:

> The pale dust on the landing is trodden along the centre in a single narrow track, and brushed level at the sides. The track goes down the stair, and it does not come back by the same line: someone swept it behind them with great patience.

The Hall of the Standing Guard (`barrow/guardian_hall.cs`), look at the chalk ticks:

> Along the paving between the pillars runs a line of small chalk ticks, one beside each of the fourteen pillars, neatly spaced, as a surveyor sets them. The last tick lies three paces from the door. There the line ends, and no chalk lies beyond it.

**Is the symbol described the same way everywhere?** Yes, in the two places it is shown. The cellar says: a circle "broken at the top, with a short bar across the break and three dots beneath it". The Barrow chalk mark says: a ring "broken at the top, with a short bar across the break and three dots beneath it". The words are identical except "circle" against "ring" and the size (a shield in the cellar, a hand in the Barrow). The Hidden Cache correctly shows no symbol ("There is nothing on the walls"). One point you may not want: the symbol is built from your older people's signs (the ring open to the north, with a bar) plus three dots that appear nowhere else. A careful player will read it as someone using the old sign as a label. That may be exactly what you want, but it is a decision.

**Conflicts and loose ends.**

- The searchers' cover story. The first journal says "I have given out that I draw for the Athenaeum". A real scholar of the Athenaeum, Marcella, sits sketching the Mossgate gateway a short walk away, and says "I am from the Athenaeum in Oakenfell". Your Oakenfell build notes (not the owner's specs) say a scholar in the Athenaeum is the Circle's interest in the grove. A suspicious player may think Marcella is the searcher. She is not. Keep or change the cover name.
- The journal also mentions "the road-station" where "no one at the road-station has asked what I am cutting for". The only road-station is Cassian's cairn camp. He is not shown to know anything.
- The cellar mark appeared while the garrison was there (years ago); the Barrow visit is "recent". Is it the same group over years? The ledger says its clerk "copied" the mark; that copy is nowhere.
- "Far door: do not go through it without the others. You were right about the cold." The pencil slip is in a "quick, small, modern hand" and refers to "the others" and a correspondent. Your brief said no faction members should appear and the Circle is clues only. This slip shows an organised group of at least three. It is a clue and not a person, but it is the plainest line in the area (see problem 5.8).
- The searchers did not go past the Hall of the Standing Guard: the chalk ticks stop "three paces from the door", and the knight's tomb lies beyond (thread 2.3).
- The Approach sealed culvert has "no footprints but your own", so the searchers have not found the aqueduct.

**Questions.**

- **Q2.1** Do you approve the symbol (ring broken at the top, bar across the break, three dots) as the Circle's first visible emblem? Once it is in play, later content will have to match.
- **Q2.2** Keep the Athenaeum as the searcher's cover story (it makes Marcella briefly suspicious), or have the writer swap in another institution?
- **Q2.3** Keep the pencil slip as written ("the others", "You were right about the cold"), or tone it down to a single person?

---

### 2.3 The Fallen Knight

**What the player pieces together.** In the last and finest tomb of the Barrow, a man who was not one of the old people made a camp for himself. He dragged the dead person's bier aside, stood a bar of oak in the door's socket so it could be shut from inside, sat facing the door for a long time, and tried and failed to scrape the carving off the walls. His shield is unreadable; his harness is of a fashion two or three hundred years out of date. In the hall in front stands an empty frame where something was bound, cut free, bound again and cut. He is the room's boss; the word knight is not written anywhere in the area's text (only the creature's file name carries it).

**Where it appears.** The Borrowed Tomb (`barrow/borrowed_tomb.cs`) (the camp and his gear), The Hall of the Standing Guard (`barrow/guardian_hall.cs`) (the empty frame and the door), `barrow/obj/tomb_coins.cs` (the spilled purse). No person mentions him.

**Key passages.**

The Borrowed Tomb (`barrow/borrowed_tomb.cs`), what a player sees:

> The great door has been drawn back just far enough to pass, and beyond it lies a tomb larger than any other, its bier dragged crosswise into a corner to clear the floor. Where it stood is a camp for one: a flat stone for a seat, a litter of rust and rag, and a ring of old soot on the paving, and against the wall a split shield. The carving on these walls is the finest in the barrow, and has been scraped at in places with something that had an edge. The cold is deeper here, and still; it has the smell of old iron.

The door and its bar:

> On the inner face of the great door is a deep socket cut in the stone, at the height of a man's hip, and a bar of oak lies in two rotted halves at its foot. The oak does not belong to the barrow; it was carried in and cut to fit. The door was meant to be shut from this side, and the floor beneath it is scored in long arcs, as of something that was pushed shut, and then pushed open.

The shield:

> A shield leans against the wall, split down the middle and gone to red flakes. On its face the paint has all but vanished: a ghost of a pale shape, a bar, perhaps a beast, and nothing a herald could name. Whoever he served, the paint has not kept the name.

The remains and the purse:

> A heap of rot and rust lies by the seat: the leather of a pack gone to black paper, a few buckles, plates of corroded harness of a make that has been out of fashion for two or three hundred years, the iron flaking to the touch. A purse lies split among them, with its coins spilled.

The camp:

> The seat is a block from the broken bier, placed to face the door. The soot ring is the size of a platter and long cold, with a handful of pale splinters in it that were once a stool or a chest. A man sat here a long while, and watched the door.

Read the walls:

> The carving here is cut with a precision that outdoes every other room, interlaced lines so fine that they make a grey shimmer of the stone. In six places on the nearest wall someone has tried to scrape it away, or to dig between the strokes with a knife, and has made no impression beyond a few bright scratches. The scratches are old, and the knife has long since rusted. The pattern, here too, ends in groups of seven.

The Hall of the Standing Guard (`barrow/guardian_hall.cs`), the empty frame:

> The frame is of dark stone and metal, taller than a tall man, standing on a slab worn by the weight of something heavy that stood there a very long time. It is the place of a guardian, and it is empty. The slab is scored with fresh marks, as by a tool and as by a heavy tread.

The cut bindings:

> The bindings are bands of grey metal as wide as a hand, set into the stone with lead, and cords of something black. They have been cut through, clean, with a tool made for it, and the ends are bright. The chips on the floor are pale where they have been struck from something recently, and dull with age where they were struck long ago. Something was broken here, and bound again, and broken. It no longer lies in the frame.

The door:

> The door is a single slab, taller than the pillars, slid aside in a groove in the floor until a narrow gap is left. The stone sill is thick with dust that no one has crossed in a very long time, and it has no chalk upon it. Whoever stood here stopped before it.

**Conflicts and loose ends.**

- The hall says the guardian's frame is "empty" and "It no longer lies in the frame", yet a Barrow Guardian (creature) is placed in this very hall. A player will find a guardian standing in a room that says it is gone.
- His harness is "out of fashion for two or three hundred years". Oakenfell is nearly four hundred. So he is a person from Oakenfell's first centuries, not one of the older people: you are not told who he served, what he feared, or whether he shut himself in or something out.
- The hall's door sill is "thick with dust that no one has crossed in a very long time", so the searchers have not been through. The knight's tomb is therefore a place no visitor has yet read. Do you want it to remain unread?

**Questions.**

- **Q3.1** Who was he? Leave as a mystery, or give one hint (a Ranger predecessor, an Assembly knight, a Thorn Pact or Circle precursor)? Nothing in the area connects him to any faction today.
- **Q3.2** Should the hall's text be changed so the guardian is present (or keep the frame empty and move the creature)?

---

### 2.4 The abandoned Riverwatch watchtower and Sergeant Holm

**What the player pieces together.** The Rangers' tower on the Green Road knoll was given up years ago, and travelers say it was the well, the pay, or something in the cellar. A hidden cellar holds the garrison's last log. Pay was five months late. Holm forbade anyone the lower stair after dark. Two men vanished from the night watch with their boots by the door. A mark appeared on the cellar wall that "was not there on the day we took stock". On the 67th day they decided to leave at first light; Holm would bar the door and go out by the roof. The log ends in the middle of a word. Now the tower is quiet; the drovers say they hear something from it at night.

**Where it appears.** The Green Road, below the Watchtower (`green_road/m09.cs`) (the milestone and the tower above), The Abandoned Watchtower (`green_road/w3.cs`) (the tower), The Watchtower Cellar (`green_road/s1.cs`) (the cellar), and the opinions of Drogo, a hired blade of the Free Companies (`green_road/npc/drogo.cs`), Hereward, the caravan guard captain (`green_road/npc/hereward.cs`), Melisande, keeper of the Old Shrine (`green_road/npc/melisande.cs`). No Ranger on the road mentions Holm or the cellar.

**Key passages.**

The Green Road, below the Watchtower (`green_road/m09.cs`), read the milestone:

> On the stone, in figures and then in a harder, later hand:
>
> ELDERGREEN 12 MI.  
> CROSSROADS 3 MI.
>
> RIVERWATCH POST. EVER VIGILANT.  
> (Beneath, scratched in with a knife:) NOBODY HOME.

The Abandoned Watchtower (`green_road/w3.cs`), what a player sees:

> On the crown of a wooded knoll stands the Riverwatch tower, a square stone shell with its roof fallen in and its door hanging from one hinge. The yard around it is thick with nettle and young birch, and a rusted signal-mast leans over the parapet. Inside it is dark, and the stairs end in rubble overhead. The ground on the west side of the knoll falls away into the forest, and a faint trail goes down there between the trees. The post was given up years ago, and the reasons given depend on who is asked.

The rumours:

> Travelers say the Rangers left because the well went bad, or because the post was starved of pay, or because of something in the cellar. The Rangers, when asked, say only that the post served its purpose.

The door:

> An oak door of two leaves, one gone and one hanging by its top hinge. The inside of it has been barred, and the bar is still there, snapped off, on the inner side.

The hearth and the flagstone:

> In the corner of the ground floor a hearth has been swept clean of ash, which is odd. The flagstones before it are scuffed in a half-circle, as if something heavy had been dragged back and forth, and one flag in the middle rings hollow when kicked.

Read the scratches by the door:

> Cut into the stone beside the door, by many hands over many years:
>
> EVER VIGILANT  
> R. HOLM, 3RD WATCH  
> SEND PAY OR SEND RELIEF  
> DON'T GO DOWN FOR WATER  
> (and the last, in a neater hand, the letters struck out)

The Watchtower Cellar (`green_road/s1.cs`), what a player sees:

> A low, vaulted cellar of dressed stone, dry and cold, with the smell of cold iron and old paper. Racks along the walls hold rusted lanterns, a few moldering sacks and the frames of cots. A table has been dragged into the middle, and on it a lantern, a heap of papers and a ledger bound in black cloth wait as if their owner stepped away. On the end wall, at the height of a man's eyes, something has been burned deep into the stone.

The ledger (full text in thread 2.2 above).

The Rangers' silence, as three travelers see it. Drogo:

> The Rangers do honest work and do not pay for it. The tower on the knoll was theirs once. Whatever they left in it, they left on purpose.

Hereward:

> The Rangers walk the road when they can. They kept a post on the knoll past the Leaning Stones once, but it is empty now, and I have heard every kind of reason why. I give none of them credit.

Melisande:

> The Rangers used to send a man every week for the lamp-oil. They stopped when the post was given up. I get the oil from the caravans now.

**Conflicts and loose ends.**

- The ledger and graffiti warn "do not trust the well" and "DON'T GO DOWN FOR WATER", and travelers say "the well went bad". No well is described anywhere in the tower, the yard or the cellar.
- The ledger says Holm would "bar the door from within and go out by the roof"; the door is found barred on the inside with the bar "snapped off", which suggests it was broken from outside. Neither is explained.
- "R. HOLM, 3RD WATCH" scratched by the door reads like a private soldier, while the ledger calls Holm the sergeant in command.
- The ledger numbers days 61 to 68 but never says what the count started from, and "years ago" is never given a number.
- The Rangers' public line ("the post served its purpose") is never contradicted by any Ranger NPC. Commander Gwenllian and Hywel never mention it.

**Questions.**

- **Q4.1** What happened to the garrison: left alive, dead, taken? Leave open, or decide?
- **Q4.2** Is there to be a well (and is the well the cause)? If so, add one; if not, remove the warnings.
- **Q4.3** Should the Rangers know about the cellar? Today the Rangers appear to know nothing, even though the cellar holds their own garrison's log.

---


### 2.5 The missing hunter Morwenna, and the goblins who count the road

**What the player pieces together.** Goblin scouts have been counting the traffic on the road with knotted cords, one knot a wagon and two for an armed party, and the Rangers think they "report to something north". A hunter, Morwenna, noticed it, kept notes in a hide high in a beech, resolved to tell the commander and then to look properly at the great carved oak, and has not been seen since the autumn. Her friends at the Hunter's Camp are anxious; the commander wants word of her.

**Where it appears.**

- Eldergreen Edge: The Riverwatch Outpost (`eldergreen_edge/m09.cs`) (the bounty board's small notice about Morwenna, the patrol roster), The Hunter's Camp (`eldergreen_edge/w2.cs`) (the hunters' tally; her unslept bedroll), The Goblin Scouts' Trail (`eldergreen_edge/d2.cs`) (the cords and the giant beech), The Marked Oak (`eldergreen_edge/d3.cs`) (cords stop twenty paces from the oak), The Hunter's Hide (`eldergreen_edge/s1.cs`) (her hide and notebook), The Birch Stand (`eldergreen_edge/w5.cs`) (a scrap of grey cord at the foot of the path east).
- People: Delwyn, a hunter (`eldergreen_edge/npc/delwyn.cs`), Elowen, a hunter (`eldergreen_edge/npc/elowen.cs`), Hywel, a veteran Riverwatch ranger (`eldergreen_edge/npc/hywel.cs`), Idris, a young Riverwatch ranger (`eldergreen_edge/npc/idris.cs`), Commander Gwenllian of the Riverwatch (`eldergreen_edge/npc/gwenllian.cs`), Tegan, the Outpost quartermaster (`eldergreen_edge/npc/tegan.cs`), Cadoc, a charcoal burner (`eldergreen_edge/npc/cadoc.cs`) (lights in the trees), Finnian, a woodcutter (`eldergreen_edge/npc/finnian.cs`), Alwin the forester (`eldergreen_edge/npc/alwin.cs`).
- Counting goblins and goblin discipline elsewhere (see 2.10-D): The Raiders' Ford (`green_road/w2.cs`) (tally sticks with three notches), The Goblin Raiders' Camp (`green_road/d1.cs`), The Veterans' Den (`approach/d3.cs`).

**Key passages.**

The Hunter's Hide (`eldergreen_edge/s1.cs`), read the notebook:

> A little oilskin notebook, its pages scrawled in a quick tidy hand:
>
> "Morwenna, her book. Goblins on the trail, by the moon.
>
> New moon: three scouts, northbound, cords at the belt, no spears. Counting.  
> First quarter: two, southbound, with a pack. They stopped at the beech and sniffed, then left. Did they know?  
> Full moon: five, in a line, very quiet. They went to the Oak and stopped twenty paces short. Walked round it, left hand always inward, as though round a grave. They will not touch the swept ground. I wonder who sweeps it.  
> Last quarter: they count the road. One knot a wagon, two for an armed party. Told no one. The commander should be told. Am going to the Edge Post at first light, will tell her myself, and then I will go to the Oak and look properly."
>
> The last page has been torn out.

The Hunter's Camp (`eldergreen_edge/w2.cs`), read the tally:

> A slab of bark propped against the lean-to is scratched with tallies in four hands, with names beside them:
>
> DELWYN. Deer, many. Boar, three.  
> ELOWEN. Deer, many. Hare, past counting. Spider, one (never again).  
> MORWENNA. Deer, boar, wolf, two goblin. Her marks stop at the leaf-fall.
>
> Beneath the last mark, someone has drawn a small tree with a line through it, and a question mark.

The Goblin Scouts' Trail (`eldergreen_edge/d2.cs`), look at the cords:

> The cords are plaited from bark fibre, each with a number of knots in it. One knot to a wagon, two to an armed party; a goblin who passes the road counts the traffic.

The Goblin Scouts' Trail (`eldergreen_edge/d2.cs`), look at the beech:

> The giant beech stands a little east of the trail behind a curtain of ivy that has been combed aside, and let fall again, many times. High in its fork, forty feet up, something that looks like a platform of branches is almost hidden by leaves, and in the grey bark below the ivy are neat square holes, in a line, like the rungs of a ladder. Nobody climbs trees by accident, but somebody has climbed this one.

The Marked Oak (`eldergreen_edge/d3.cs`), look at the cord:

> A knotted goblin cord lies in the leaves twenty paces back from the ring, dropped, not tied. Whatever the goblins were counting, they stopped here.

The Riverwatch Outpost (`eldergreen_edge/m09.cs`), the small notice on the bounty board is the last item of the board (the whole board is in section 3, entry for `eldergreen_edge/m09.cs`). It reads: "Any word of Morwenna, hunter, last at the Hunter's Camp in the autumn, to the commander."

Commander Gwenllian on goblins and on Morwenna:

> Goblin scouts are new on this road, or newly bold. They count the traffic and report to something north. They do not seem to want to fight. I do not like that. If you meet them, do not assume they are alone.

> Morwenna was a hunter, and a good one, who worked the east side of the wood. She set out in the autumn to look at something in the deep wood and has not been heard of since. If you find her, or what she left, tell me.

Elowen on Morwenna and her hide:

> My friend. We came up here together. She wanted to track the goblins to where they went, and she found something, and she went on alone. I should have gone with her.

> Morwenna had a hide, somewhere in the deep wood, up a tree. She never told me where. Said it was safer. If you find it, you'll find her notes.

Delwyn on Morwenna:

> She went out on the east trail in the autumn with a pack, and a week's food. I thought she'd gone to the Rangers. The commander says she never came. I don't like it.

Cadoc on lights in the trees:

> I saw small lights in the trees to the east, three nights back, bobbing low down. Too small for men. I told the Rangers, and they said thanks. Hm. They say thanks a lot.

**Conflicts and loose ends.**

- Your plan called the hide's owner a dead hunter. The text never says she is dead: she "vanished" and the question is left open. That is a better hook but different from the plan.
- Delwyn says "The commander says she never came", while the notebook says she meant to go to the Edge Post at first light. The pieces agree (she set out but never arrived); the last page of the notebook is torn out, so what she found at or on the way to the Oak is missing. No trace of her is anywhere at the Oak itself.
- Gwenllian says Morwenna "set out in the autumn to look at something in the deep wood", and does not mention that she meant to report goblins counting the road.
- "report to something north" is never answered. The Green Road's Goblin Raiders' Camp is north, as is Heartwood, as are the Veterans' Den and the Warrens; four unrelated goblin groups are described and nothing links them (see 2.10-D).

**Questions.**

- **Q5.1** What became of Morwenna: dead, taken by the Pact, alive somewhere, or deliberately never answered? A later quest would need the answer; for now I can leave a single detail at the Oak or in the Heartwood if you say where.
- **Q5.2** Should "something north" be left open, or is it meant to be the Raiders' Camp or the Veterans' Den?

---

### 2.6 Thorn Pact signs

**What the player pieces together.** Some of the country is tended by someone nobody sees: thread and thorn are changed, ground is swept, hawthorn is planted in rings and the animals will not cross them. The Rangers know the marks and have decided not to look closer. No Pact member appears anywhere.

**Every sign, in road order.**

South Road:

- The South Road, Beyond the Hollow (`south_road/m01.cs`): "a small ring of dots" scratched beneath the parish boundary stone ("a child's game or a farmer's tally"), probably innocent.
- The Brook Pasture (`south_road/w4.cs`): a ring of seven young hawthorns around a patch of bare ground where cattle will not graze, with strips of wool and cloth knotted to the lowest branch (the rag-knots); no bird nests in them.
- The Old Sheepfold (`south_road/d3.cs`): a knot of hawthorn twigs "bound with a strip of wool", hung over the sheepfold gate; the shepherds say they have not touched it.
- Old Leofric on the ring: "We do not cut it. We do not ask." Mildred on rings and rag-knots.

Eldergreen Edge:

- The Marked Oak (`eldergreen_edge/d3.cs`): the Marked Oak, with its carved band, plaited withy wreaths hung with small bones and feathers, swept bare earth, and small grey stones with one notch each. Nothing grows under it, animals keep away, goblin cords stop twenty paces off.
- The Broken Waystone (`eldergreen_edge/m05.cs`): the Broken Waystone, with ". . . FOREST REMEMBERS." scratched below the inscription (first of the two places).
- The Edge Road, the Green Gate (`eldergreen_edge/m11.cs`): ribbons, knotted cord and a child's wooden horse tied at the Green Gate, an arch of two oaks "trained" together by "some long-ago hand". This is ambiguous and may only be travelers' custom.

Green Road:

- The Old Shrine (`green_road/m06.cs`): acorns and "a twig bound in a knot" tucked behind the Old Shrine that nobody admits leaving.
- The Green Road, the Leaning Stones (`green_road/m08.cs`): the Leaning Stones, nine stones, moss on three sides and bare on the fourth, birds avoid them.
- The Pedlars' Path (`green_road/w1.cs`): charms on the Pedlars' Path, "one or two look much older than the rest" (ambiguous).

Heartwood:

- The Ritual Stones (`heartwood/w4.cs`): the Ritual Stones, seven standing stones, a thorn hedge with a single gap, red thread changed "within the month", a swept bare floor, a flat stone with a cup, and "THE FOREST REMEMBERS" (second of the two places).
- The Ancient Wolf's Dell (`heartwood/d1.cs`): the Ancient Wolf's Dell, where "the thorn has been allowed to grow in a ring on the rim".
- The Thorn Thicket (`heartwood/d2.cs`): the Thorn Thicket (hawthorn grown to the height of a tree; not a sign by itself).
- The Dryad Spring (`heartwood/w3.cs`): the Dryad Spring (animals unafraid; moss cropped short). The word dryad appears only in the room name.

Oakenfell Approach:

- The Old Aqueduct, the Broken Span (`approach/w1.cs`): seven pale stones laid in an exact ring at the foot of an aqueduct pier, grass cropped short. Ambiguous: could be the Pact or the older people.
- Southfarm Hamlet, the Back Fields (`approach/w5.cs`): the last sheaf tied to a post "for whatever is out there" (a custom; the child refuses to say more).
- The Greenvale Toll House (`approach/m03.cs`): a stake with "a rag tied to it" marking the track to the old arches (probably the bandits').

**Where a person names the Pact aloud: nowhere.** No spoken or written line contains the words Thorn Pact. But five people respond if the player types the words, because their topic keywords include them: Ranger Mildred (`thorn pact`), Commander Gwenllian (`thorn pact`, `pact`), Delwyn (`pact`), Hywel (`pact`, `thorn pact`), Cassian (`pact`, `thorn pact`). Their answers never use the name:

Mildred:

> There are hawthorn rings and rag-knots in some of the pastures. Old custom, the farmers say, or something else. I don't ask. I keep the road, and I leave the rest where it lies.

Gwenllian:

> There are old groves in this wood, and old marks, and the Rangers do not disturb them. I will not say more than that. If you find marks, report them. Do not cut them. Do not move them.

Hywel:

> I've seen the marks. I've never seen who makes them. Thirty years, never once. That tells you something, but I'll not tell you what.

Cassian:

> Someone keeps the stones north of the spring. The thread changes, the ground is swept. I've never seen who. I've never wanted to. I leave the thread as I find it.

Delwyn:

> East of the trail there's a great oak, carved. I've seen the animals go round it. They never cross the ring. Neither will I.

**Key passages.**

The Brook Pasture (`south_road/w4.cs`), the hawthorn ring and the bare ground:

> Seven young hawthorns stand in a neat ring, too regular for chance. Their bark is unmarked, and there is not a bird's nest in any of them.

> In the middle of the ring is a circle of bare, bitten-down earth, though nothing has been grazed there. Faded strips of wool and cloth are knotted to the lowest branch, wind-whitened and old.

The Old Sheepfold (`south_road/d3.cs`), the knot:

> A tight knot of hawthorn twigs, bound with a strip of wool and hung from the gate post. It is neither a farmer's mark nor a child's toy, and the shepherds say they have not touched it.

Leofric on the ring:

> The hawthorn ring in the brook pasture? It was there before my father. We do not cut it. We do not ask. The cattle keep their own counsel, and the milk is no worse for it.

The Marked Oak (`eldergreen_edge/d3.cs`), what a player sees:

> A colossal oak stands alone in a ring of bare, swept earth, so wide that a dozen people might not encompass it, and so old that its lower limbs have sunk into the ground and risen again as trunks. Its bark is carved all round at head height with a band of deep marks, and from every low bough hang wreaths of plaited withy, small bones and bright feathers. Nothing grows beneath it. The wood about the ring is silent and the animals keep a distance from it, as if by agreement, though the goblin cords end twenty paces away.

The Marked Oak (`eldergreen_edge/d3.cs`), read the markings:

> You cannot read the markings; they are not letters of any alphabet you know. Some show rings, some show a branching like thorns or antlers, and one, repeated nine times round the trunk, is a simple spiral. You have the feeling they were made to be understood by something other than people.

and what `look at` shows for the same band:

> The wreaths are plaited willow bent into rings, and each ring holds small bones laid in a pattern, a fox's skull, a bird's wing, three teeth. Some are green-fresh, and some have rotted to a thread.

The Broken Waystone (`eldergreen_edge/m05.cs`), read the waystone (also in section 3):

> What can be read of the inscription, standing half and fallen half together:
>
>    . . . ED BY THE WARDENS OF . . . . . .  
>    FROM THE HOL . . . . . TWELVE MILES  
>    TO OAKENF . . . . . . . . DAYS  
>    . . . . . . . . . . . . . . .  
>    KEEP TO THE ROAD. THE WOOD . . . . .
>
> Below, in rougher letters that were scratched in later:  
>    . . . FOREST REMEMBERS.
>
> A line in the middle has been chiselled away entirely, on purpose, by someone who did not want it read.

The Ritual Stones (`heartwood/w4.cs`), what a player sees:

> Seven standing stones, grey and lichened and each as tall as a man, stand in a ring in a clearing where the forest floor has been swept bare. The ground between them is cleared of leaf and the grass cut short, and a living thorn hedge has been trained around the outside, with a single gap. Twigs of thorn are bound to the stones with red thread, some old and bleached, others green. The place is empty, tended and still. There is no sound, but you have the feeling of having walked into the middle of something that has just ended.

The Ritual Stones (`heartwood/w4.cs`), the sign:

> The mark cut in each stone is a simple sign: a short upright stroke with three barbs, like a thorn drawn by a child. It is deeply cut and clean-edged, and the lichen has been scraped from the groove. Somebody keeps it open. You could read the tallest stone.

The Ritual Stones (`heartwood/w4.cs`), the thread:

> Each stone has a twig of thorn bound to it at the level of the heart with a loop of red thread. The oldest twigs are grey and brittle, and the newest are green and still have their leaves. They have been changed within the month.

The Ritual Stones (`heartwood/w4.cs`), read the tallest stone:

> Beneath the thorn sign on the tallest stone, in letters smaller than your thumb and scratched rather than cut, is a single line:  
> "THE FOREST REMEMBERS"

The Green Road, the Leaning Stones (`green_road/m08.cs`), what a player sees:

> A ring of tall grey stones stands on a slope beside the road, every one of them leaning outward as though in the grip of a long, slow wind. They are furred with moss on three sides and bare on the fourth, and the bare side faces the road. The trees keep their distance from the stones and the ground between them is thick with ferns. Birds that fly across the gap alter their course for a moment, as if to avoid something.

The Old Shrine (`green_road/m06.cs`), the acorns and the knotted twig:

> At the back of the shrine, tucked in the gap between wall and tree, lie a handful of acorns and a twig bound in a knot. They are not like the other offerings, and nobody here seems to know who leaves them.

Melisande on them:

> The acorns and twigs? I do not put them there. I do not move them either. They come in the night, and I leave them be. Mind you, no harm has come to the lamp in all my years.

**Conflicts and loose ends.**

- The numbers. The older people's number is seven (thread 2.1). The Pact places also use seven: seven hawthorns in the South Road ring, seven Ritual Stones. But the Leaning Stones are nine and the Marked Oak's spiral is repeated "nine times round the trunk". Was seven meant to link the Pact to the older people, or is this coincidence? The Ritual Stones' detail even says the stones "are far older than the thread on them", which suggests the Pact tends older sites.
- The Marked Oak room reads two ways. The text you get from `read` says one mark "repeated nine times round the trunk, is a simple spiral"; the text from `look at` says "a repeated shape that might be a crown or a tooth". Both cannot be the repeated mark.
- Rangers: Mildred "I don't ask", Alwin "I go round, and I don't ask", Gwenllian "I will not say more than that", Hywel "I've never seen who makes them", Cassian "I've never wanted to". Consistent and well judged: all of the Rangers know and none will say.
- Hale's Acre: Leofric says "The thorn took the place". That ties the Pact's aim (restoring forgotten places) to an abandoned farm (thread 2.9).
- Rangers' blazes are also a ring cut through the bark (Hunter's Memorial and the Last Great Oak). Alwin says a mark that is "a ring cut round the trunk" is not his. A player may confuse the Rangers' rings with Pact rings.
- Gwenllian says the Rangers "do not disturb" the groves and asks travelers to report but not cut marks. That is good; nothing contradicts it.

**Questions.**

- **Q6.1** Five people answer to the typed words thorn pact or pact without ever saying them. Keep (the player can discover the name by guessing), or remove the keywords so the name stays hidden until you decide?
- **Q6.2** Is the shared number seven (Pact and older people) intended? If not, I would change the Pact's sevens (hawthorn ring, Ritual Stones) to another number.
- **Q6.3** "THE FOREST REMEMBERS" is the Pact's motto in your bible. It is scratched on the waystone and on the tallest Ritual Stone. Fine to have the motto in two places, or only one?

---

### 2.7 The Root Warrens' story

**What the player pieces together.** A goblin clan dug under a great oak for clay and roots under an overseer, with families in the quarters. In the third moon the down-shaft struck old, square, fitted stone "that is not our stone", and a digger named Nub said he heard scratching under it. Rats came in the fourth moon; in the fifth the shaft was left open for a night and eleven diggers and the lamps were lost. In the sixth the families went up and out, the overseer had already gone up, and the tunnelers stayed to hold one corner, a number that shrank to four. Later a human prospector found clay and a claim and was lost in the dark. At the back of the nest is the fallen shaft and a carved lintel with no way through: a hook for a future expansion.

**Where it appears.** All twelve rooms. The story is told in six readables: The Diggers' Junction (`root_warrens/junction.cs`) (the overseer's count), The Tunnelers' Corner (`root_warrens/corner.cs`) (the tunnelers' tally), The Diggers' Quarters (`root_warrens/quarters.cs`) (a child's scratchings), The Clay Cut (`root_warrens/clay_cut.cs`) (the prospector's notebook); and the details in The Broodmother's Nest (`root_warrens/nest.cs`), The Cold Larder (`root_warrens/larder.cs`), The Root-Choked Hall (`root_warrens/root_hall.cs`). No person appears.

**Key passages.**

The Diggers' Junction (`root_warrens/junction.cs`), read the plank:

> The scoring is goblin work, cut with a knife and filled with soot. Whoever made it counted in bundles of five, and wrote a line of blunt words above each column:
>
>   'THE DIGGING. Overseer's count, in the long dig.'  
>   'FIRST MOON. West cut, forty paces. Clay. Good for pots, bad for roof. Props wanted. East cut, sixty paces. Roots. Axes dull. Hands split. Diggers eat too much.'  
>   'SECOND MOON. Lost two to roof-fall in the west. One run off up the stair. Not counted.'  
>   'THIRD MOON. Down-shaft reached stone that is not our stone. Square, fitted, old. Digger Nub says he hears scratching under it. Nub is a fool. Nub is not at this count.'  
>   'FOURTH MOON. Rats in the spoil. Big. Killed forty. More came.'  
>   'FIFTH MOON. Down-shaft left open one night. Lost eleven diggers. Lost the lamps. Overseer says close shaft. Nobody will go and close it.'  
>   'SIXTH MOON. Families go up to the quarters, then up out. Tunnelers hold the Corner. Everyone else go up. Overseer has already gone up.'  
>   'Count ends. Nobody left to count.'
>
> The last line is scored over and over, until the plank is almost cut through.

The Tunnelers' Corner (`root_warrens/corner.cs`), read the tally:

> The tally is cut with a knife in the goblin way, in bundles of five, and a few lines of rough writing are scratched beside it:
>
>   'RATS KILLED THIS MOON: more than the fingers of all of us.'  
>   'ROOT-CREEPERS BURNED: many. They come back.'  
>   'TUNNELERS LEFT: ' and a column of marks that grows shorter, line on line, until the last holds only four.
>
>   'OVERSEER SAID HOLD UNTIL RELIEF. NO RELIEF. WE HOLD ANYWAY.'

The Diggers' Quarters (`root_warrens/quarters.cs`), read the scratchings:

> Low on the wall, at the height of a small child's reach, a nail has been used to scratch pictures and crude letters into the clay:
>
>   A row of stick figures holding hands under a round sun with rays, a sun that nobody under the ground could have seen. Beneath them, in wobbling capitals: 'ME. MUM. BIG GRIT. UNCLE NUB.'
>
>   'UNCLE NUB HEARS THINGS. MUM SAYS HE IS DAFT.'
>
>   Farther along the figures have been crossed out, one by one. Over the last of them a fat creature with a long tail and too many teeth has been drawn, scratched deep.
>
>   At the very end, pressed hard: 'WE GO UP TODAY. MUM SAYS NO LOOK BACK. I LOOKED.'

The Clay Cut (`root_warrens/clay_cut.cs`), read the notebook:

> The notebook is swollen with damp and hard to turn, but the last pages can be made out:
>
>   'Day 1. North by the Greenvale road. Found a hollow under a big fallen oak, and below it goblin work, old, too square for a fox. Good clay in the west cut, near white. Worth a claim.'
>
>   'Day 2. Tools everywhere and nobody home. A cooking pot still on the hearth in the quarters, rotted to a skin. They left in a hurry. I do not like how quiet it is.'
>
>   'Day 3. Rats. Not the size a rat should be. I lit the lamp and they kept coming, so I stood in the clay cut with my back to the wall until they lost interest. Heard hammering to the south, behind a mass of roots. Goblins, living ones. I shall not go near.'
>
>   'Day 4. The draught is stronger at night and it comes from below. It smells like a nursery rat a hundred times over. I heard something gnaw through a timber as thick as my leg. Going up at first light.'
>
>   'Day 4, later. I cannot find the stair. I took the east cut and the roots have moved. No. I am confusing the cuts. West, then north, then'
>
> The line stops there, and the rest of the page is a smear of clay.

The Broodmother's Nest (`root_warrens/nest.cs`), what a player sees:

> The slope ends in a great round chamber, domed like a bell, and it is filled almost to the walls by a nest. The nest is a vast mound of shredded hide, fur, timbers and bone, packed and trampled into a bed, and it is warm. Smaller burrows open all round the walls at floor level, each as wide as a barrel. The reek is almost solid. At the back of the chamber, the far wall is a slope of fallen earth and broken stone, all that is left of the shaft the goblins dug and could not close.

The Cold Larder (`root_warrens/larder.cs`), the chest:

> The chest is small, plain and iron-bound, and it has stood unopened so long that the scrap of hide over it has dried to a board. It is a goblin's private hoard: the hidden savings of a digger who meant to come back for them.

**Conflicts and loose ends.**

- The plank says "Overseer has already gone up" and the Corner's tally quotes "OVERSEER SAID HOLD UNTIL RELIEF". That reads as a bitter irony and is consistent.
- Nub is "a fool" and "not at this count" in the overseer's tally and "UNCLE NUB HEARS THINGS" in the child's drawing: consistent, and he is the one who heard something under the old stone. "BIG GRIT" appears once and is never explained.
- Where the families went is not said. The Heartwood and Green Road goblins (the Mossgate court goblins, the Raiders' Camp) are described in no way as survivors of this clan.
- The prospector writes "North by the Greenvale road" and Worth a claim, which implies mining claims, a prospector's licence and a market for clay: new to the world. His notebook ends mid-line "West, then north, then".
- The nest layer: "many small bony hands and pointed skulls that are not rats'". This is the darkest image in the dungeon (see problem 5.21).
- The hook: the sealed shaft is a stone of the older people. Nothing connects it to the Barrow, which is reached separately through the Collapsed Shrine.
- Your brief said no people and no Circle material: that was followed.

**Questions.**

- **Q7.1** Keep the goblin clan's backstory (overseer, Nub, the families, the four tunnelers) as written? It is self-contained and consistent.
- **Q7.2** Should the surviving families be linked to the goblins elsewhere on the road (2.10-D)? Currently no goblin group knows any other.
- **Q7.3** Keep the prospector's claim? It invents mining law. I would rewrite it as a good seam of clay if you do not want claims.

---

### 2.8 Bandits and a possible informer

**What the player pieces together.** Someone is watching the road from far away, at three points: from the old sheepfold on the ridge next to the South Road, from a lookout in the deep wood of the Green Road, and, near the city, a bandit camp with a toll book listing which caravans to let through and which to rob. The wagon master says the bandits know which wagon rows carry a Ranger escort and complains "Somebody talks". The toll book names a Consortium clerk, "Reeve", who "pays on the Rise. Not yet." The Consortium tally clerk in the Staging Grounds is named Philippa Reeve, and says that she has carried the cash south on the Rise road every month and has never been stopped.

**Where it appears.**

- South Road: The Old Sheepfold (`south_road/d3.cs`) (the sheepfold lookout), Hamo, the mill hand (`south_road/npc/millhand_hamo.cs`), Kenrick, a grizzled veteran (`south_road/npc/veteran_kenrick.cs`), Mildred, a Riverwatch ranger (`south_road/npc/ranger_mildred.cs`), Bram, a shepherd (`south_road/npc/shepherd_bram.cs`).
- Green Road: The Bandits' Lookout (`green_road/d3.cs`) (the Bandits' Lookout), Drogo, a hired blade of the Free Companies (`green_road/npc/drogo.cs`), Hereward, the caravan guard captain (`green_road/npc/hereward.cs`).
- Approach: The Approach Road, the Forest's Edge (`approach/m01.cs`) (Ranger notice), The Bandits' Road (`approach/w2.cs`) (the Bandits' Road), The Bandit Camp (`approach/w3.cs`) (the camp and the toll book), The Merchant Staging Grounds (`approach/m05.cs`) (the tally board), Philippa Reeve, a Consortium tally clerk (`approach/npc/tally_clerk.cs`), Baldric Teague, the wagon master (`approach/npc/wagon_master.cs`), Humphrey Cobb, the toll keeper (`approach/npc/toll_keeper.cs`), Oswin Hale, an Assembly guard (`approach/npc/toll_guard.cs`), Constance Ardley, a Riverwatch ranger (`approach/npc/ranger.cs`), Lorcan Pike, a drover (`approach/npc/drover_lorcan.cs`).

**Key passages.**

The Old Sheepfold (`south_road/d3.cs`), what a player sees:

> A broken sheepfold crowns the ridge, its drystone walls fallen into heaps and its gate a pair of rotten posts. Beside it the roof of a shepherd's hut lies in on the floor. Nothing grows inside the ring but nettles and pale grass, and the view from the wall runs north across the whole country to the black line of the Eldergreen Wood. Someone has been here lately: the ashes of a small fire lie in the lee of the hut, a tally of notches runs along a fallen lintel, and a boot-print is pressed deep in the mud. Above the gate hangs a knot of hawthorn twigs that nobody put there for the sheep.

The Old Sheepfold (`south_road/d3.cs`), the watcher's tally:

> A row of notches cut in the lintel with a knife, one for each day, and a boot-print pressed into the mud beside it. Whoever it was kept watch on the road for a good many days.

Hamo asked about the fire:

> Yes, a fire. A little thing, shielded. Not a shepherd's: Bram sleeps in his own hut. Whoever it is keeps watch on the road, and I'd rather it were not me who asked why.

Mildred asked about bandits:

> There's been a watcher on the old sheepfold ridge for a fortnight. I've been up and he's been gone. I don't like it.

The Bandit Camp (`approach/w3.cs`), read the toll book (also in section 3):

> Scratched in charcoal on a board, in a crabbed hand:  
> THE TOLL BOOK  
>  Wool, 40 bales, Row 2, third Tuesday: let go, escort.  
>  Salt wagons, 6, no escort: take.  
>  Consortium clerk with the cash chest: 'Reeve', pays on the Rise. Not yet.  
>  Do not touch the Rangers' signal post. Ever.  
> Monthly share to the Boss, paid in kind.

The Bandit Camp (`approach/w3.cs`), the Riverwatch badge:

> A shallow pit, ringed with stones, with an iron pot bubbling on a tripod. The stew has gone grey; at its edge somebody's trophy, a Riverwatch badge, has been nailed to a post.

The Merchant Staging Grounds (`approach/m05.cs`), read the tally board (also in section 3):

> CARAVANS FORMING, by leave of the Merchant Consortium  
>  Row 1: wool and cloth for the Hollow, second bell.  
>  Row 2: salt, nails and lamp oil, south by the Heartwood road, at noon.  
>  Row 3: grain for the Hollow's mill, waiting on one wheel.  
>  Row 4: empty, returning south, passage for those who pay the master.  
> Riverwatch escort rides with Row 2. Guards are signed on at the master's wagon.  
> TRADE BUILDS TOMORROW

Philippa Reeve on bandits:

> Yes, I have heard the stories. I carry the cash south on the Rise road every month and I have never once been stopped. That seems a very good reason to be frightened.

and on the cash:

> The master's pay, the toll money, the hire for the oxen, all in the box. I would rather not talk about the box in public, if you do not mind.

Baldric Teague on bandits:

> We lose a wagon a month to them. They know our times. That is what troubles me; they know which row has the Ranger and which does not. Somebody talks.

Humphrey Cobb on the track to the old arches:

> That track? It goes to the old arches. There is nothing there, and nothing there is exactly what I would say about it to a stranger.

Constance Ardley on the bandit camp:

> There's a camp in the western woods, behind the old arches, with a man at the head of it who has taken the measure of us. We know where it is; we do not have the strength to go in. If you've the stomach and a good blade, it is not a thing I'd forbid you.

Oswin Hale on the bandit camp:

> There is a camp somewhere in the west woods. We know it, the Rangers know it, and it has a man at the head of it who is no fool. Not our job to go in. Our job is the pole.

**Conflicts and loose ends.**

- The toll book conflicts with the board. The toll book lists "Wool, 40 bales, Row 2, third Tuesday: let go, escort" and "Salt wagons, 6, no escort: take". The tally board in the Staging Grounds puts wool and cloth in Row 1 and salt, nails and lamp oil in Row 2 with "Riverwatch escort rides with Row 2". So the toll book says the escorted row is wool; the board says it is salt (see problem 5.5).
- The toll book names a surname and a role. You wrote that the camp's toll book seems to name a Consortium clerk. It does: "Reeve" is the surname of the only clerk in the Staging Grounds, who also carries a "cash box chained to her wrist". The line is ambiguous in a useful way: "pays on the Rise. Not yet." could mean she pays the bandits (an informer) or that the bandits mean to rob her (a mark). She herself says that never being stopped "seems a very good reason to be frightened".
- "Monthly share to the Boss, paid in kind." There is a "Boss" above the Bandit Leader. Nobody else in the area has heard of him.
- Authorities look away. The toll keeper says of the track to the camp "nothing there is exactly what I would say about it to a stranger"; the toll guard says the camp is not the Assembly's business; the Ranger says "we do not have the strength to go in". A player may read that as collusion or as weakness.
- The Rangers' signal post is the only thing the bandits fear: "Do not touch the Rangers' signal post. Ever." and a Riverwatch badge is nailed to a post in their camp.
- Two parties count the road: the goblins with knotted cords (thread 2.5) and the bandit watchers with notches and tally marks. Nothing connects them.
- Ranger Mildred says the watcher on the sheepfold ridge has been "gone" when she looked, which fits a man who sees her coming, from the very ridge Hamo says he watches the road from.

**Questions.**

- **Q8.1** Is Philippa Reeve the informer, the intended victim, or a cleared red herring? As written both are possible; a later quest needs you to choose.
- **Q8.2** Who is the Boss? Leave as an unnamed superior, or tie to the Consortium or a faction?
- **Q8.3** Should officials at the Toll House and the Rangers be suspect (collusion), or merely overstretched?

---

### 2.9 Hale's Acre and other small local histories

**Hale's Acre.** A farm settlement on the South Road that failed "the winter the well went dry" and whose people carried what they could to the Hollow; "The thorn took the place". The only trace is a hidden farmstead hearthstone and a dry well in a thorn clearing, with a traveler's hidden purse and a wax tablet signed M..

The Dry Well (`south_road/s1.cs`), read the tablet:

> Pressed in the wax of a small tablet, in a hurried hand:  
> "Hale's Acre failed the winter the well went dry. We carried what we could to the Hollow and left the rest to the thorn. I hid the purse in the well where no stranger would think to look, for the road is long and the wood is not what it was. If I do not come back for it, let it feed someone who is hungrier than I am. - M."

Old Leofric on Hale's Acre:

> Hale's Acre? Gone before my time. Their well failed, the story goes, and they went to the Hollow and never came back. The thorn took the place.

The mill's tally board at Windmill Hill (`south_road/w3.cs`):

> A board by the mill door carries chalked tallies and a few lines of a miller's hand:  
> "Tuesday: 14 sacks Hale's barley. Wednesday: wind from the north-west, good milling. Thursday: wolf took a lamb from Bram's flock at dusk. Friday: mended the tail pole."

The conflicts: the tally board shows the mill grinding "Hale's barley" this very week, yet Leofric (aged eighty) says Hale's Acre was "Gone before my time". And there is a third Hale: the toll guard at the Toll House is Oswin Hale. Is that Oswin's family? (Problem 5.1.) The tablet's M. is never identified: the mill hand, the miller's father and Mildred all have other initials; B + M is also carved on the stile at The South Road, Hedgerow Mile (`south_road/m02.cs`). The same word, a failing well, is echoed by the watchtower ("DON'T GO DOWN FOR WATER").

**Q9.1** Which Hale is which? I suggest the tally becomes another farm's barley. **Q9.2** Is "M." someone you want identified?

**Small local histories, each one self-contained.**

- Traveler's Rest: three hundred years old, "The road was cut around it, not the other way". An old rule that the yard is safe ("nobody's drawn a blade in this yard in thirty years"). Traveler's Rest (`south_road/m07.cs`) and Aldith, the innkeeper (`south_road/npc/innkeeper_aldith.cs`).
- Old Miller's Farm: toll of "ONE MEASURE IN SIXTEEN"; Leofric's grandfather raised the first post of the mill; sails replaced six times, the tail pole nine; the orchard was his late wife's.
- The Stone Bridge: the mason's mark in the middle keystone, underwater; the plaque "RAISED BY THE FREE HANDS OF THE VALE"; "older than the Assembly".
- The Old Shrine: its lamp "has not gone out in my mother's day, nor in her mother's", nobody knows who built it; offerings buried, coins to the Rangers for the causeway.
- Last Great Oak: props "Someone cared enough to keep the tree standing, and some time ago stopped".
- Hunter's Memorial: four named dead, "ONE WHOSE NAME WAS NEVER KNOWN" and room left on the stone.
- Southfarm: eleven cottages, one mill, moots held at a stone on the green, the last sheaf left on a post.
- Children's carvings and graffiti throughout (the stile's B + M, the oak's "B + E", Rhosyn and Tam, Old Garrick, J.M.).

---

### 2.10 Further threads you did not list

**A. Names and lines that someone chiselled away.** Four separate places have a name or line deliberately removed: the waystone ("A line in the middle has been chiselled away entirely, on purpose"), the west face of the Crossroads Stone ("(a name cut out with a chisel)"), the cartwheel at the Wagon Halt ("Someone carved a name into the rim and then scratched it out") and the tower graffiti ("the letters struck out"). Four writers wrote four erasures. Is there one hand doing this (the Pact, the Rangers, the Circle)? Q10.1: either connect them (one hidden hand) or let them stand as coincidence.

**B. The Rangers are thinning.** Hywel: "The order's smaller than it was." The tower is abandoned, the Hunter's Memorial has "Room left", Cassian is alone at his station, a Riverwatch badge is nailed in the bandit camp, and Constance says the Rangers "do not have the strength to go in". Q10.2: is this a deliberate arc (the Rangers are failing), or should they be reassured?

**C. People who vanish on the road.** Morwenna; Isabeau's brother ("He took this road in the spring, and I have had no letter"); Wystan's clerk "taken" by goblins; two men from Holm's night watch; the unnamed fifth name at the Memorial; "the last man" at the Bear Trees (Fulk); "a bride once" at the Old Shrine; the people of Hale's Acre. Q10.3: Several are candidates for later quests. Which, if any, are you willing to answer later, and which should be cut as bleak?

**D. Goblin organisation.** The scouts count traffic and report "to something north"; the raiders at The Goblin Raiders' Camp (`green_road/d1.cs`) live in a palisaded camp under stolen Consortium canvas; the "Veterans' Den" (The Veterans' Den (`approach/d3.cs`)) is a barracks of "soldiers" who "keep a drill" with a mended banner and, per Lorcan, were "soldiers once, my father said, in somebody's army. Discharged and not asked back."; the warren refugees (2.7). Four goblin societies, none aware of each other, and a goblin army "in somebody's army" that implies a wider war you have not described. Q10.4: keep "somebody's army"? It suggests a state or war outside Greenvale.

**E. The Athenaeum.** Marcella (real scholar), the searchers' cover (2.2) and the Oakenfell notes (an inquisitive scholar in the Athenaeum who is the Circle's interest) all use the Athenaeum. Q10.5 is in 2.2 (Q2.2).

**F. The Old Way.** The Crossroads Stone's east track "THE OLD WAY. NO CARTS." leads nowhere in the build; Jocosa "took it once" and "got out"; Hereward "never taken the Old Way, and do not mean to". Q10.6: is there a place at the end of this track?

**G. The wells.** Hale's Acre failed with its well; the tower ledger says "do not trust the well"; the Dry Well; Southfarm's well. This may be a deliberate motif, but only the Dry Well and the watchtower's missing well are mysteries. (See Q4.2.)

---


## 3. Every inscription and readable, in full

This section lists every `AddReadable` call in the corridor, in road order (South Road, Eldergreen Edge, Green Road, Heartwood, Oakenfell Approach, then the Root Warrens and the Barrow). Count check: searching the 173 files finds 57 `AddReadable` calls in 52 files, and this section has 57 entries. A room with two readables has two entries. Type is the word or words you type after `read`. Colour tags are removed; line breaks are kept.


### Segment 1, the South Road

**1. The South Road, Beyond the Hollow** — `south_road/m01.cs`  
Player types: `read stone` (other accepted words: boundary stone, marker)  
> The stone is knee-high and lichened, cut square on its south face:  
> "HERE ENDS THE PARISH OF VINCENT'S HOLLOW. KEEP THE ROAD."  
> Someone has scratched a small ring of dots beneath, a child's game or a farmer's tally.

**2. The South Road, Hedgerow Mile** — `south_road/m02.cs`  
Player types: `read stile` (other accepted words: footpath, path)  
> Two worn steps of oak climb over the hedge. Initials have been cut into the top rail through many summers, and one pair is fresh enough to still be yellow:  
> "B + M"  
> The path beyond runs off across the beans toward the farm.

**3. Old Miller's Farm, the Lane End** — `south_road/m03.cs`  
Player types: `read board` (other accepted words: painted board, sign)  
> The board is hand-lettered in white on black:  
> "OLD MILLER'S FARM. GRIST GROUND, FLOUR BY THE SACK. TOLL ONE MEASURE IN SIXTEEN. PLEASE SHUT THE GATE. THERE IS NO GATE. SHUT IT ANYWAY."

**4. The South Road, Barley Fields** — `south_road/m04.cs`  
Player types: `read scarecrow` (other accepted words: coat, figure, plate)  
> The scarecrow wears a brown coat patched in four colours, a straw hat, and a tin plate tied on its chest. The plate is scratched with letters:  
> "WATCHMAN. HIRED 3 SUMMERS. HAS NOT ONCE ASKED FOR A RAISE."  
> One crow sits on its hat, entirely unimpressed.

**5. The South Road, below Windmill Hill** — `south_road/m05.cs`  
Player types: `read signpost` (other accepted words: post, fingerpost)  
> A fingerpost with two arms stands at the foot of the hill. One points up the slope and reads "MILL". The other points up the road and reads:  
> "TRAVELER'S REST. HOT FOOD. BEDS."  
> Someone has carved a smaller arrow under the second arm and written "stew".

**6. Traveler's Rest** — `south_road/m07.cs`  
Player types: `read sign` (other accepted words: painted sign, inn sign)  
> The sign shows a boot and a bed and a full tankard, and beneath them the words:  
> "THE TRAVELER'S REST. BED, BOARD AND WATER FOR BEAST. NO QUESTIONS ASKED, SOME ANSWERS GIVEN."

**7. Traveler's Rest** — `south_road/m07.cs`  
Player types: `read notice` (other accepted words: riverwatch notice, board, notice board)  
> A notice board by the door carries a printed sheet with a silver eye at its head:  
> "RIVERWATCH RANGERS. Travelers bound north of the Stone Bridge are advised to go by daylight and in company. Report wolves, bandits and torn fences to any ranger. Ever Vigilant."  
> Underneath, in a different hand: "Eat first."

**8. The South Road, the Long Meadows** — `south_road/m08.cs`  
Player types: `read milestone` (other accepted words: stone, marker)  
> A milestone, leaning, with its lettering almost swallowed by moss:  
> "TO THE HOLLOW, SOUTH. NORTH, THE ELDERGREEN WOOD."  
> Someone has scratched below it, with a nail: "Mind the wolves."

**9. The Stone Bridge** — `south_road/m10.cs`  
Player types: `read plaque` (other accepted words: stone plaque, inscription)  
> A slab of slate is set into the parapet at the midpoint, its letters cut deep:  
> "THE STONE BRIDGE. RAISED BY THE FREE HANDS OF THE VALE, THAT THE ROAD MIGHT KEEP THE TRAVELER, AND THE TRAVELER THE ROAD."  
> Beneath, small and sharp: "Ever Vigilant."

**10. The South Road, the Far Bank** — `south_road/m11.cs`  
Player types: `read milestone` (other accepted words: stone, marker, waystone)  
> A grey stone, newer than the road, cut with a plain eye, the sign of the Riverwatch:  
> "TO THE HOLLOW, SOUTH. TO OAKENFELL, NORTH THROUGH THE ELDERGREEN WOOD. TRAVEL BY DAYLIGHT."

**11. Windmill Hill** — `south_road/w3.cs`  
Player types: `read tally` (other accepted words: tally board, board, mill board)  
> A board by the mill door carries chalked tallies and a few lines of a miller's hand:  
> "Tuesday: 14 sacks Hale's barley. Wednesday: wind from the north-west, good milling. Thursday: wolf took a lamb from Bram's flock at dusk. Friday: mended the tail pole."

**12. The Dry Well** — `south_road/s1.cs`  
Player types: `read note` (other accepted words: tablet, scrap, wax tablet, wax, paper)  
> Pressed in the wax of a small tablet, in a hurried hand:  
> "Hale's Acre failed the winter the well went dry. We carried what we could to the Hollow and left the rest to the thorn. I hid the purse in the well where no stranger would think to look, for the road is long and the wood is not what it was. If I do not come back for it, let it feed someone who is hungrier than I am. - M."


### Segment 2, Eldergreen Edge

**13. The Edge Road, the Last Fences** — `eldergreen_edge/m01.cs`  
Player types: `read post` (other accepted words: boundary post, brand)  
> Burned deep into the leaning gatepost, under a farmer's brand:  
> "THE STONE BRIDGE HOLDINGS END HERE."  
> Someone has scratched below it, in a different hand: "And so does the kindness."

**14. The Forester's Cabin** — `eldergreen_edge/m03.cs`  
Player types: `read sign` (other accepted words: notice, board)  
> Nailed to the door post on a plank:  
> "FORESTER'S LODGE. Blazed trees are not to be cut, barked, or tapped. No fires past this point in dry weather. Charcoal burners east of the Narrows by leave only. Report any tree marked other than with white paint to the forester."

**15. The Broken Waystone** — `eldergreen_edge/m05.cs`  
Player types: `read waystone` (other accepted words: inscription, letters, writing, inscription stone)  
> What can be read of the inscription, standing half and fallen half together:
>
>    . . . ED BY THE WARDENS OF . . . . . .  
>    FROM THE HOL . . . . . TWELVE MILES  
>    TO OAKENF . . . . . . . . DAYS  
>    . . . . . . . . . . . . . . .  
>    KEEP TO THE ROAD. THE WOOD . . . . .
>
> Below, in rougher letters that were scratched in later:  
>    . . . FOREST REMEMBERS.
>
> A line in the middle has been chiselled away entirely, on purpose, by someone who did not want it read.

**16. The Riverwatch Outpost** — `eldergreen_edge/m09.cs`  
Player types: `read board` (other accepted words: notice board, notices, bounties, bounty board, bounty)  
> The board is crowded with notices pinned in layers. The freshest, in a clear hand, read:
>
> RIVERWATCH RANGERS, EDGE POST. BOUNTIES.
>
> FOREST WOLVES. Packs hunting the Fern Banks and the Narrows, and bolder than they were. Proof of a kill to be shown to the quartermaster.  
> WILD BOAR. Sows with young in the deep wood, and big tuskers wallowing east of the road. Dangerous when cornered. Cull them before they come out to the farms.  
> GOBLIN SCOUTS. Seen on the old trail beyond the Birch Stand, in twos and threes. Do not seek them in numbers less than three. Report any sighting to the commander.  
> FOREST SPIDERS. The Spider Thicket grows. Burn webs, bring word.
>
> The Rangers will acknowledge all work done in the road's service. See the quartermaster.
>
> A smaller notice, pinned at the corner, says only: "Any word of Morwenna, hunter, last at the Hunter's Camp in the autumn, to the commander."

**17. The Riverwatch Outpost** — `eldergreen_edge/m09.cs`  
Player types: `read roster` (other accepted words: duty roster, patrol roster, rota)  
> The patrol roster, nailed to the bunkhouse door, reads:
>
> "Edge Post patrol: out from the Outpost along the Patrol Mile to the Green Gate and back; then south by the Narrows, the Charcoal Clearing and the Fern Banks as far as the Broken Waystone, and back. Two rangers at a time. Report anything moving on the road that should not be."

**18. The Edge Road, the Green Gate** — `eldergreen_edge/m11.cs`  
Player types: `read sign` (other accepted words: plank, notice, board)  
> Nailed to the south oak at head height, a painted plank:  
> "THE GREEN ROAD BEGINS. Keep to the road. Travel in company after dusk. Rangers keep the road as far as the Merchant Camp. Ever Vigilant."

**19. The Hunter's Camp** — `eldergreen_edge/w2.cs`  
Player types: `read tally` (other accepted words: tally board, board, bark board, hunters tally)  
> A slab of bark propped against the lean-to is scratched with tallies in four hands, with names beside them:
>
> DELWYN. Deer, many. Boar, three.  
> ELOWEN. Deer, many. Hare, past counting. Spider, one (never again).  
> MORWENNA. Deer, boar, wolf, two goblin. Her marks stop at the leaf-fall.
>
> Beneath the last mark, someone has drawn a small tree with a line through it, and a question mark.

**20. The Marked Oak** — `eldergreen_edge/d3.cs`  
Player types: `read markings` (other accepted words: marks, carvings, band)  
> You cannot read the markings; they are not letters of any alphabet you know. Some show rings, some show a branching like thorns or antlers, and one, repeated nine times round the trunk, is a simple spiral. You have the feeling they were made to be understood by something other than people.

What `look at` shows for the same thing:

> A band of marks has been cut around the trunk, three hands deep: rings within rings, notched lines like thorns, and a repeated shape that might be a crown or a tooth. They are fresh-edged in places and silvered with age in others, as though added to over centuries. You could read them.

**21. The Hunter's Hide** — `eldergreen_edge/s1.cs`  
Player types: `read notes` (other accepted words: journal, pages, tally, notebook, book, writing)  
> A little oilskin notebook, its pages scrawled in a quick tidy hand:
>
> "Morwenna, her book. Goblins on the trail, by the moon.
>
> New moon: three scouts, northbound, cords at the belt, no spears. Counting.  
> First quarter: two, southbound, with a pack. They stopped at the beech and sniffed, then left. Did they know?  
> Full moon: five, in a line, very quiet. They went to the Oak and stopped twenty paces short. Walked round it, left hand always inward, as though round a grave. They will not touch the swept ground. I wonder who sweeps it.  
> Last quarter: they count the road. One knot a wagon, two for an armed party. Told no one. The commander should be told. Am going to the Edge Post at first light, will tell her myself, and then I will go to the Oak and look properly."
>
> The last page has been torn out.


### Segment 3, the Green Road

**22. The Old Shrine** — `green_road/m06.cs`  
Player types: `read plaque` (other accepted words: carving, inscription, lintel, words, stone words)  
> Cut into the lintel, in letters worn almost smooth by thumbs:
>
> FOR THOSE WHO WENT BEFORE  
> AND THOSE WHO DID NOT ARRIVE.  
> KEEP THE LIGHT. KEEP THE ROAD.

**23. The Green Road, below the Watchtower** — `green_road/m09.cs`  
Player types: `read milestone words` (other accepted words: words, chiseled words, inscription, figures)  
> On the stone, in figures and then in a harder, later hand:
>
> ELDERGREEN 12 MI.  
> CROSSROADS 3 MI.
>
> RIVERWATCH POST. EVER VIGILANT.  
> (Beneath, scratched in with a knife:) NOBODY HOME.

**24. The Crossroads Stone** — `green_road/m11.cs`  
Player types: `read carving` (other accepted words: carvings, writing, letters, inscription, marker words)  
> Cut on the four faces of the stone, faint with age, in four different hands:
>
> NORTH: OAKENFELL, FOR THE LIVING  
> SOUTH: THE HOLLOW, FOR THE HOMESICK  
> EAST: THE OLD WAY. NO CARTS.  
> WEST: (a name cut out with a chisel)

**25. The Abandoned Watchtower** — `green_road/w3.cs`  
Player types: `read scratches` (other accepted words: graffiti, wall scratches, carved words, wall)  
> Cut into the stone beside the door, by many hands over many years:
>
> EVER VIGILANT  
> R. HOLM, 3RD WATCH  
> SEND PAY OR SEND RELIEF  
> DON'T GO DOWN FOR WATER  
> (and the last, in a neater hand, the letters struck out)

**26. The Watchtower Cellar** — `green_road/s1.cs`  
Player types: `read ledger` (other accepted words: log, logbook, book, garrison log, black ledger)  
> The last pages of the ledger, in a cramped, careful hand:
>
> ...day 61. No relief. Pay is five months owing. Stores sound. Sergeant Holm has forbidden anyone the lower stair after dark and will not say why.
>
> ...day 64. Two men gone from the night watch, their beds made. Holm says desertion. Their boots are by the door.
>
> ...day 66. Found the mark on the cellar wall. It was not there on the day we took stock. Holm says leave it be. I have copied it, and I am sorry I did.
>
> ...day 67. We go at first light. Holm will bar the door from within and go out by the roof. If any Ranger reads this: do not stay in the cellar after dusk, and do not trust the well.
>
> ...day 68. I do not think we are all going to...
>
> The entry ends there.


### Segment 4, Heartwood

**27. The Fallen Arch** — `heartwood/m02.cs`  
Player types: `read carving` (other accepted words: carvings, band, pattern, lines, interlaced)  
> You trace the carved band with a finger. The strokes are cut deep and clean, and they repeat in groups of seven before a stroke changes, and then begin again. It looks like writing, or like a border, and nothing about it matches the marks on any stone, stall or coin that you know. Near the base of the east pier the pattern breaks off, and the last few groups are worn flat by weather.

**28. The Hunter's Memorial** — `heartwood/m07.cs`  
Player types: `read slab` (other accepted words: slate, plaque, memorial, inscription, stone slab)  
> The inscription reads:
>
> TO THE HUNTERS OF THE RIVERWATCH  
> who went into the Heartwood and did not come out of it.
>
> EDRIC VANE      JOAN PRYOR  
> HOB TARRANT      SEFA LUND  
> AND ONE WHOSE NAME WAS NEVER KNOWN
>
> The forest took them. We carried the stones.  
> EVER VIGILANT.
>
> Below, in newer and shallower letters, a hand has scratched: "Room left."

**29. The Heartwood Road, the Last Great Oak** — `heartwood/m11.cs`  
Player types: `read initials` (other accepted words: carvings, carving, names, letters, cut, marks)  
> Most of it is the usual thing: pairs of initials bound by a knot, "B + E", "WE CAME THIS FAR", a ship scratched by a child, a tally of nights. High on the south side, in a firm hand and deeper than the rest, someone has cut: "THROUGH THE HEARTWOOD AND OUT AGAIN. NO ONE TOLD ME IT WOULD BE SO LONG."

**30. The Mossgate Ruins, the Inner Court** — `heartwood/w1.cs`  
Player types: `read band` (other accepted words: carving, carvings, frieze, figures, pattern, border)  
> You follow the band round the court. The procession goes always one way, with the raised arms, and every seventh figure carries something that might be a staff or a branch. At the end, where the band should close, it does not. The last figure has no arm raised, and the border under it stops in an unfinished line. There are no letters. The carving is crisp wherever the roof once shielded it.

**31. The Ritual Stones** — `heartwood/w4.cs`  
Player types: `read tallest stone` (other accepted words: tall stone, tallest, mark, marks, markings, thorn mark, symbol, sign, cut, carved)  
> Beneath the thorn sign on the tallest stone, in letters smaller than your thumb and scratched rather than cut, is a single line:  
> "THE FOREST REMEMBERS"

**32. The Hidden Cache** — `heartwood/s1.cs`  
Player types: `read first journal` (other accepted words: first, journal one, first book, 1)  
> The paper is burned away on every side. What remains reads:
>
> "...third day in the court. The lowest course is as the old report said, and no one at the road-station has asked what I am cutting for. I have given out that I draw for the Athenaeum. [burned] ...should anyone come again I will say the same. Keep the sketches in the other book, and this one on me..."

**33. The Hidden Cache** — `heartwood/s1.cs`  
Player types: `read second journal` (other accepted words: second, journal two, second book, 2)  
> Half the page is gone. What remains reads:
>
> "[burned] ...nothing under the arch, nor in the court, nor beneath the spring-stone. It was not set down where they said. If it was moved, it was moved before any of us. [burned] ...hollow trunks big enough to hide a cart; I will go through the thorn on the east side tomorrow. Burn the maps when they are copied. No one is to know that we were ever here..."

**34. The Hidden Cache** — `heartwood/s1.cs`  
Player types: `read third journal` (other accepted words: third, journal three, third book, 3)  
> The last book was thrown on the heap, and the fire reached only the cover. The final page reads:
>
> "...the shrine in the deep wood is shut from within or fallen. I do not like what the cold air smells of. [burned] ...if it lies below, it is for those who come after me, and they will have better tools. Leave nothing. This book goes into the fire with the rest of--"
>
> The line stops there, in the middle of a stroke.


### Segment 5, Oakenfell Approach

**35. The Approach Road, the Forest's Edge** — `approach/m01.cs`  
Player types: `read notice` (other accepted words: riverwatch notice, sign, tree)  
> RIVERWATCH RANGERS, NOTICE TO TRAVELERS  
> The road to Oakenfell is watched from the Toll House northward. Beyond the verge, in the western woods, it is not.  
> Travelers who meet armed men on the road are to report them at the Greenvale Toll House.  
> Ever Vigilant.

**36. The Greenvale Toll House** — `approach/m03.cs`  
Player types: `read toll board` (other accepted words: board, tolls, rates, toll)  
> GREENVALE TOLL HOUSE, by order of the Assembly  
> For the upkeep of the road and the safety of its travelers.  
>  Handcart, barrow or pack-beast: 1 gold  
>  Wagon, empty: 3 gold  
>  Wagon, laden: 5 gold  
>  Herd, per ten head: 2 gold  
>  Riders and walkers: free  
> Holders of the Greenvale Travel Pass pass without question.  
> PROSPERITY THROUGH UNITY

**37. The Approach Road, Milestone Row** — `approach/m04.cs`  
Player types: `read milestone` (other accepted words: nearest milestone, stone, distance)  
> OAKENFELL, 5 MILES  
> VINCENT'S HOLLOW, 36 MILES  
> From Strong Roots Grow Great Things

**38. The Merchant Staging Grounds** — `approach/m05.cs`  
Player types: `read tally board` (other accepted words: board, departures, tally, list)  
> CARAVANS FORMING, by leave of the Merchant Consortium  
>  Row 1: wool and cloth for the Hollow, second bell.  
>  Row 2: salt, nails and lamp oil, south by the Heartwood road, at noon.  
>  Row 3: grain for the Hollow's mill, waiting on one wheel.  
>  Row 4: empty, returning south, passage for those who pay the master.  
> Riverwatch escort rides with Row 2. Guards are signed on at the master's wagon.  
> TRADE BUILDS TOMORROW

**39. The Approach Road, under the Old Aqueduct** — `approach/m06.cs`  
Player types: `read plaque` (other accepted words: bronze plaque, brass plaque, notice)  
> THIS ARCH IS KEPT SOUND FOR THE SAFETY OF TRAVELERS BY ORDER OF THE GREENVALE ASSEMBLY. Do not climb the piers. Do not tie beasts to the stonework. The Assembly claims no knowledge of its builders and no right to its water.

**40. Southfarm Hamlet, the Green** — `approach/m08.cs`  
Player types: `read board` (other accepted words: hand-lettered board, prices, little board)  
> SOUTHFARM, bread, cheese and baked apples. Honest weight. Leave the plate.

**41. Southfarm Hamlet, Mill Lane** — `approach/m09.cs`  
Player types: `read mill sign` (other accepted words: sign, board, mill board)  
> SOUTHFARM MILL, flour ground to the Oakenfell weight  
> Carters loading for the city: leave your tally with the miller's wife.  
> No dogs in the mill, no children in the wheel.

**42. The Approach Road, the Rise** — `approach/m10.cs`  
Player types: `read boulder` (other accepted words: stone carvings, carved names, names)  
> Rhosyn and Tam, the spring they came. Old Garrick, home at last. J.M., 'I did not know it was so big.' Three notches and an arrow: SOUTHGATE, BEST PIES. Beneath all these, in deeper letters: THE ROAD IS SHORT, IF YOU KEEP WALKING.

**43. The Approach Road, the Carters' Mile** — `approach/m11.cs`  
Player types: `read last milestone` (other accepted words: milestone, stone, marker)  
> OAKENFELL, THE SOUTHGATE, HALF A MILE  
> Keep to the right with loaded wagons. Riders to walk their beasts under the arch.  
> From Strong Roots Grow Great Things

**44. The Bandit Camp** — `approach/w3.cs`  
Player types: `read ledger` (other accepted words: tally, tally board, board, toll book)  
> Scratched in charcoal on a board, in a crabbed hand:  
> THE TOLL BOOK  
>  Wool, 40 bales, Row 2, third Tuesday: let go, escort.  
>  Salt wagons, 6, no escort: take.  
>  Consortium clerk with the cash chest: 'Reeve', pays on the Rise. Not yet.  
>  Do not touch the Rangers' signal post. Ever.  
> Monthly share to the Boss, paid in kind.

**45. The Sealed Culvert** — `approach/s1.cs`  
Player types: `read marks` (other accepted words: carved marks, signs, carvings, symbols, glyphs, line of marks)  
> The marks are cut in rows along the wall at shoulder height, small and regular. Three recur: a ring crossed by a bar; a single wavering line, like water; and a ring with a gap in it, open to the north. Between them are columns of short strokes, always in groups of seven. They are not letters of the city, nor of any tongue the Assembly keeps. The ring-and-bar is the same sign that is worn into the piers of the arch on the road.


### Dungeon 1, the Root Warrens

**46. The Diggers' Junction** — `root_warrens/junction.cs`  
Player types: `read plank` (other accepted words: board, tally, marks, notches, scoring)  
> The scoring is goblin work, cut with a knife and filled with soot. Whoever made it counted in bundles of five, and wrote a line of blunt words above each column:
>
>   'THE DIGGING. Overseer's count, in the long dig.'  
>   'FIRST MOON. West cut, forty paces. Clay. Good for pots, bad for roof. Props wanted. East cut, sixty paces. Roots. Axes dull. Hands split. Diggers eat too much.'  
>   'SECOND MOON. Lost two to roof-fall in the west. One run off up the stair. Not counted.'  
>   'THIRD MOON. Down-shaft reached stone that is not our stone. Square, fitted, old. Digger Nub says he hears scratching under it. Nub is a fool. Nub is not at this count.'  
>   'FOURTH MOON. Rats in the spoil. Big. Killed forty. More came.'  
>   'FIFTH MOON. Down-shaft left open one night. Lost eleven diggers. Lost the lamps. Overseer says close shaft. Nobody will go and close it.'  
>   'SIXTH MOON. Families go up to the quarters, then up out. Tunnelers hold the Corner. Everyone else go up. Overseer has already gone up.'  
>   'Count ends. Nobody left to count.'
>
> The last line is scored over and over, until the plank is almost cut through.

**47. The Clay Cut** — `root_warrens/clay_cut.cs`  
Player types: `read satchel` (other accepted words: pages, journal, book, notebook, papers)  
> The notebook is swollen with damp and hard to turn, but the last pages can be made out:
>
>   'Day 1. North by the Greenvale road. Found a hollow under a big fallen oak, and below it goblin work, old, too square for a fox. Good clay in the west cut, near white. Worth a claim.'
>
>   'Day 2. Tools everywhere and nobody home. A cooking pot still on the hearth in the quarters, rotted to a skin. They left in a hurry. I do not like how quiet it is.'
>
>   'Day 3. Rats. Not the size a rat should be. I lit the lamp and they kept coming, so I stood in the clay cut with my back to the wall until they lost interest. Heard hammering to the south, behind a mass of roots. Goblins, living ones. I shall not go near.'
>
>   'Day 4. The draught is stronger at night and it comes from below. It smells like a nursery rat a hundred times over. I heard something gnaw through a timber as thick as my leg. Going up at first light.'
>
>   'Day 4, later. I cannot find the stair. I took the east cut and the roots have moved. No. I am confusing the cuts. West, then north, then'
>
> The line stops there, and the rest of the page is a smear of clay.

**48. The Tunnelers' Corner** — `root_warrens/corner.cs`  
Player types: `read tally` (other accepted words: post, posts, marks, scratches, cuts, scoring)  
> The tally is cut with a knife in the goblin way, in bundles of five, and a few lines of rough writing are scratched beside it:
>
>   'RATS KILLED THIS MOON: more than the fingers of all of us.'  
>   'ROOT-CREEPERS BURNED: many. They come back.'  
>   'TUNNELERS LEFT: ' and a column of marks that grows shorter, line on line, until the last holds only four.
>
>   'OVERSEER SAID HOLD UNTIL RELIEF. NO RELIEF. WE HOLD ANYWAY.'

**49. The Diggers' Quarters** — `root_warrens/quarters.cs`  
Player types: `read wall` (other accepted words: scratchings, scratches, pictures, drawings, drawing, writing, letters)  
> Low on the wall, at the height of a small child's reach, a nail has been used to scratch pictures and crude letters into the clay:
>
>   A row of stick figures holding hands under a round sun with rays, a sun that nobody under the ground could have seen. Beneath them, in wobbling capitals: 'ME. MUM. BIG GRIT. UNCLE NUB.'
>
>   'UNCLE NUB HEARS THINGS. MUM SAYS HE IS DAFT.'
>
>   Farther along the figures have been crossed out, one by one. Over the last of them a fat creature with a long tail and too many teeth has been drawn, scratched deep.
>
>   At the very end, pressed hard: 'WE GO UP TODAY. MUM SAYS NO LOOK BACK. I LOOKED.'


### Dungeon 2, the Forgotten Barrow

**50. The Sunken Forecourt** — `barrow/forecourt.cs`  
Player types: `read band` (other accepted words: carving, carvings, frieze, figures, figure, procession, pattern, border)  
> The band begins at the foot of the stair, as though it had come down it. The figures are the long, thin figures of the court in the wood, with the same blank faces and the same single raised arm, and they walk in sevens toward the doorway. Every seventh carries a branch, but here the branch is bare and bound with a cord at its middle. Above their heads the border that broke off unfinished in the court runs on without a flaw. There are no letters.

**51. The Descending Way** — `barrow/descending_way.cs`  
Player types: `read band` (other accepted words: carving, carvings, frieze, figures, figure, procession, pattern, border)  
> There are seven figures to a flight, one to a step, so that the procession seems to descend as you do. Each has one arm raised. On every seventh flight the figure on the landing bears the bound branch, and the cord at its middle is knotted seven times, and the knots are cut so finely that you can count them with a finger. Above them the border runs on unbroken. Nothing in it is a letter.

**52. The Long Gallery** — `barrow/long_gallery.cs`  
Player types: `read band` (other accepted words: carving, carvings, frieze, figures, figure, procession, pattern, border, files, arms)  
> Forty-two figures walk north along the two walls, and you count them, because the count is so plainly meant. In each group of seven one figure has let its raised arm fall, and the group after it has two, and so on, until at the northern end of the gallery there are no raised arms at all, and the figures walk with their hands open at their sides. Their faces are blank to the last. The border above them runs on, not yet closed.

**53. The Long Gallery** — `barrow/long_gallery.cs`  
Player types: `read slip` (other accepted words: scrap, paper, note, scrap of paper, fold)  
> A scrap of paper lies in the dust at the foot of the west wall, where it must have fallen from a sleeve. It is written in a quick, small, modern hand, in pencil:
>
> "Gallery 42, hall 7. The count is as it should be. Seven bowls east. Rubbing done, second sheet spoiled, third sheet clean. Far door: do not go through it without the others. You were right about the cold."
>
> There is no name.

**54. The Inscribed Tomb** — `barrow/inscribed_tomb.cs`  
Player types: `read panel` (other accepted words: inscription, inscriptions, strokes, rows, writing, carving, carvings, wall, signs)  
> Three rows of interlaced strokes run the width of the panel, each in seven groups. Above them stand three signs, one over each row: a ring crossed by a bar, a single wavering line like water, and a ring with a gap open to the north. The strokes are cut very deep and are as sharp as on the day they were made. You cannot read them, and it seems that no one could have read them in a very long time. Whoever came before you could not, either, or they would not have needed a copy to carry away.

**55. The Tomb of the Seven Bowls** — `barrow/bowl_tomb.cs`  
Player types: `read walls` (other accepted words: wall, carving, carvings, scene, figures, figure, panel, pattern)  
> The carving is one scene, repeated on both walls. A single figure lies on a bier, and seven others stand about it, each holding a bowl in the hand that is not raised. A branch, bound with a cord, is held at the head. At the feet stands a last figure whose arm is not raised, as it was not in the court in the wood. The faces are as blank as ever. Nothing says who lies on the bier.

**56. The Hall of the Standing Guard** — `barrow/guardian_hall.cs`  
Player types: `read walls` (other accepted words: wall, figures, figure, band, carving, carvings, frieze, procession, border, last figures)  
> The procession ends here. The last seven figures have turned about, so that they stand with their backs to the great door and their faces toward the gallery, toward you; their arms are down and their hands are open. Above them the border that was unfinished in the court in the wood, and ran on through every passage since, bends at last and meets itself, a closed ring. It is the only closed ring in the barrow. The faces, to the end, are blank.

**57. The Borrowed Tomb** — `barrow/borrowed_tomb.cs`  
Player types: `read walls` (other accepted words: wall, carving, carvings, panels, scraped, scrape, scrapes, pattern)  
> The carving here is cut with a precision that outdoes every other room, interlaced lines so fine that they make a grey shimmer of the stone. In six places on the nearest wall someone has tried to scrape it away, or to dig between the strokes with a knife, and has made no impression beyond a few bright scratches. The scratches are old, and the knife has long since rusted. The pattern, here too, ends in groups of seven.


---

## 4. Checklist of invented facts

Tick or strike each line. Every item below is something the writers made up that is not in your documents. There are 59 items, numbered continuously.

### 4.1 Distances and travel times

1. Your brief says the journey from the Hollow to Oakenfell is about two days. The text agrees roughly: Ives says two days, "a day and a half if the wagon is empty"; Lysander also says two days; the Rise text speaks of "the two long days of forest".
2. Baldric Teague (Approach wagon master) says three days with a heavy wagon and two light, which is longer than your two days.
3. Ranger Mildred says "By the third day it's real forest", while Jocosa says the forest starts on the second day. Both cannot hold in a two-day trip.
4. The Approach milestone (The Approach Road, Milestone Row (`approach/m04.cs`)) gives Oakenfell at 5 miles and Vincent's Hollow at 36 miles. Nothing in your documents gives a mileage. At about 0.65 miles per room, 36 miles fits the 55 main-road rooms.
5. The Green Road milestone (The Green Road, below the Watchtower (`green_road/m09.cs`)) gives Eldergreen at 12 miles and the Crossroads at 3 miles. At the road's scale the Crossroads is about 1.5 miles away, not 3; Hereward also says the Crossroads is three miles on from the camp, which is further still. See problem 5.3.
6. The Broken Waystone (The Broken Waystone (`eldergreen_edge/m05.cs`)) is partly legible and gives twelve miles and a number of days.
7. Jocosa says the walls of Oakenfell can be seen "from a day away", but the room at 5 miles (The Approach Road, Milestone Row (`approach/m04.cs`)) says you are still miles from the city.
8. Gwenllian says the Merchant Camp is a few miles in from the Edge.
9. The Riverwatch outpost is a half day's walk from the Stone Bridge; Oakenfell is a half day north of the Last Great Oak (Lysander).
10. Ives lost a steer three days north. Leofric took three days to walk home from Oakenfell.
11. The city wall of Oakenfell is thirty feet high.

### 4.2 Dates, ages and calendar

12. Oakenfell is "nearly four hundred years" old (Marcella, and the arch at The Approach Road, under the Old Aqueduct (`approach/m06.cs`)). Your bible says nearly four centuries; this agrees.
13. Traveler's Rest is three hundred years old (Aldith); the inn's detail says a hundred years of travelers.
14. Harness has been out of fashion for two or three hundred years (the borrowed tomb).
15. The Marked Oak is about a thousand years old.
16. Thirty years: Alwin and Hywel have walked the wood, no blade has been drawn in Aldith's yard, Lorcan has driven the road.
17. Leofric is eighty and has ground corn for sixty years; his grandfather raised the mill's first post.
18. Cuthbert has been at his post forty years; Florian sixty years; the hunters' camp has stood ten years.
19. The watchtower ledger is dated by day number (61 to 68) and records five months of pay owing.
20. In-world weekdays (Tuesday to Friday, Thursday, "third Tuesday") and a month name (June, in Edda's talk) are used. Your calendar uses Harvest-style names; see problem 5.17.
21. The moon is counted in phases and months (the Root Warrens tallies use third moon to sixth moon).
22. The flood poles at the Stone Bridge are dated "decades ago".

### 4.3 People mentioned but never met

23. The Memorial dead: Edric Vane, Joan Pryor, Hob Tarrant, Sefa Lund, and one whose name was never known. Cassian's former partner is also named Edric.
24. Morwenna, hunter, missing since the autumn.
25. Sergeant Holm of the Riverwatch tower, the unnamed ledger clerk, and Holm's two watchmen who left the tower.
26. In the Root Warrens: Nub, Big Grit, the Overseer and the unnamed prospector.
27. "M." on the Dry Well tablet; the carvers B + M and B + E; Rhosyn and Tam; Old Garrick; J.M.
28. Dorrin Stavely of the Caravan Office; Adelina and Emmeline (a farm wife's children); the miller's wife at Southfarm Mill; the Boss of the bandits.
29. Animals with names: Biscuit and Duke (oxen), Barley (horse), Bess (dog), Alderman (ram), Old Biddy (mule).
30. Isabeau's brother; the clerk taken by goblins (Wystan); a sergeant at the Oakenfell Rangers' office and a sergeant at the Outpost (Cassian).
31. Griselda's brother (dairy); the mason of the Stone Bridge; Finnian's father and grandfather; the Wardens named on the waystone.

### 4.4 Institutions, customs and heraldry

32. Assembly toll board at the Toll House: handcart, barrow or pack-beast 1 gold; empty wagon 3; laden wagon 5; a herd 2 per ten head; riders and walkers free; Travel Pass holders pass free.
33. Camp fee at the Merchant Camp: one copper a head, collected by Lambert, "the one tax on the Green Road that everyone pays".
34. Mill toll at Old Miller's Farm: one measure in sixteen.
35. The parish boundary stone: the parish of Vincent's Hollow ends there. A second stone says Stone Bridge holdings end there.
36. Forester's Lodge rules and white blaze markings.
37. Ranger customs: cairn-stones, notched posts, blazes cut in trees, and signal-post pennants (green all is well, white, grey-blue).
38. The last sheaf is left on a post for "whatever is out there"; the Memorial leaves room for more names; the Old Shrine's lamp is kept lit; shrine offerings are buried.
39. The Wardens (waystone), the Athenaeum, the Caravan Office, the Free Companies' dull red badge.
40. Heraldry: Assembly green banner with a white oak and green tabards; Consortium blue-and-silver scales; Rangers' silver eye on a green-and-grey pennant. The Outpost pennant has a pale shield and the blazes are a white shield on a green square with an eye; see problem 5.16.
41. Watchfort Crossing: Gwenllian places it far east, Idris east of Oakenfell with a drill yard and a hall with every Ranger's name carved in it.

### 4.5 Prices (every shop)

42. Aldith, Traveler's Rest: bread and cheese 3 gold (heals 8); mutton pasty 6 gold (heals 16); a bed for the night 5 gold (heals to full; this is a real, working purchase).
43. Lambert, Merchant Camp: healing potion 24 gold (heals 20); trail ration 7 gold (heals 5); camp pasty 9 gold (heals 8).
44. Elsewhere in the game the same healing potion costs 15 gold in the Hollow and 20 at the Oakenfell outfitter; the trail ration costs 5 and 6. Lambert is described as a touch above Oakenfell's prices, which fits.
45. Griselda, Southfarm: bread and cheese 4 gold (heals 10); honey baked apple 2 gold (heals 5). Aldith and Griselda both sell an item called Bread and Cheese with different prices and effects; see problem 5.4.
46. Loot values: old purse 15 to 30 gold; old coins 40 to 70; tomb coins 90 to 150; grave token worth 15. The larder chest in the Root Warrens spawns a gold cache and a healing potion.
47. The Rangers' bows are not for sale.

### 4.6 Oakenfell, the Hollow, the factions and the wider world

48. Oakenfell's walls are thirty feet high, twice rebuilt, with square towers.
49. A bronze-green dome crowns the Hall of Legends; bells ring the hour.
50. The Southgate; the Silver Stag inn just past it (Constance); the Rangers' office by the quay with a sergeant; Market District bakeries; the Athenaeum keeping the city's records; the Caravan Office; the River Mere with a stone bridge on the east.
51. The Consortium keeps the books for most of Greenvale's trade.
52. The Travel Pass is for the Northgate.
53. The Hollow has a single street, Hollow ale, and a mill.
54. Goblin veterans were soldiers once in somebody's army (a war elsewhere).
55. Eldergreen is used as a place name on the milestones.
56. Mining claims exist in Greenvale (the prospector).
57. The Approach NPCs have surnames (Teague, Reeve, Cobb, Hale, Ardley and others); your plan allowed first names only.
58. The Pact's motto is on a waystone and a standing stone, and the Rangers know the marks. Your bible leaves the Pact's presence to later.
59. Riverwatch is a named body with badges, a tower and a night watch; your documents name the Rangers but not Riverwatch.

---

## 5. Problems found

Each is numbered so you can reply by number.

### Contradictions and inconsistencies

**5.1 The name Hale used three ways.** Hale's Acre is a ruined farm "Gone before my time" (Leofric, Old Leofric, the miller (`south_road/npc/miller_leofric.cs`)); the mill's tally board at Windmill Hill (`south_road/w3.cs`) records "Hale's barley" being ground this week; and the Toll House guard is Oswin Hale. One of these must change.

**5.2 Travel-time disagreements.** See 4.1: Ives and Lysander say two days, Baldric three, Mildred and Jocosa disagree on which day the forest starts.

**5.3 Milestone arithmetic.** The Green Road, below the Watchtower (`green_road/m09.cs`) says the Crossroads is 3 miles away, Hereward says three miles on from the camp, and the road scale puts it at about 1.5. Also Jocosa's day-away view of the walls conflicts with The Approach Road, Milestone Row (`approach/m04.cs`).

**5.4 Two items named Bread and Cheese** with different price and healing (Aldith 3 gold heals 8; Griselda 4 gold heals 10). Rename one or unify.

**5.5 Toll book against tally board.** The toll book at The Bandit Camp (`approach/w3.cs`) lets the escorted Row 2 wool pass and takes unescorted salt; the tally board at The Merchant Staging Grounds (`approach/m05.cs`) puts wool in Row 1 and salt in Row 2 with the escort. Swap one.

**5.6 Reeve is named outright.** Your brief expected the toll book only to seem to name a clerk. It gives the surname, and the only clerk with that name is in the Staging Grounds (thread 2.8).

**5.7 Five people respond to the Thorn Pact keywords** (Mildred, Gwenllian, Delwyn, Hywel, Cassian). None says the name, but typing it works (thread 2.6).

**5.8 The Circle, more plainly than clues only.** The pencil slip in the Barrow refers to "the others", the cover story of the Athenaeum is explicit in the searchers' notes, and the line "No one is to know that we were ever here" shows organised secrecy (thread 2.2). No member appears, but these are the plainest lines.

**5.9 Promised mechanics that do not exist.** The text offers: a bounty board and quartermaster at the Edge ("Show the quartermaster proof"); Baldric's sign-on and the Caravan Office; Hereward's escort ("walk with us at dawn"); Drogo and Hereward hiring; Fulk's mending; the camp-fee; "Leave the plate"; Gwenllian's "tell me". Each needs either a mechanic or a rewrite.

**5.10 Aqueduct geometry and stone shapes.** The Approach arches, piers and the older people's stone shapes and colours are described differently by different writers (see thread 2.1).

**5.11 Two different infills** are described for the same arches (thread 2.1).

**5.12 Marked Oak read against look.** The read text says a spiral repeated nine times; the look text says a crown or a tooth (thread 2.6).

**5.13 The forecourt dish.** The Barrow forecourt dish is smaller than "the great dish in the court above", but the great dish is in the Mossgate court in the Heartwood, not above the Barrow.

**5.14 The guardian frame** in the guardian hall is described as empty while a guardian is spawned in the room.

**5.15 No well at the watchtower**, though the ledger says not to go down for water and not to trust the well (thread 2.4).

**5.16 Ranger badge and pennant descriptions differ** (silver eye on green and grey; pale shield; white shield on green square with an eye). Cassian's sergeant at the Outpost conflicts with the Outpost commander.

**5.17 Calendar.** Tuesday to Friday, June, and "third Tuesday" are used, while your calendar names months differently.

**5.18 A boulder readable without a boulder.** The Approach Rise has a readable keyed on a boulder, and the room has no boulder.

**5.19 The Dryad Spring has no dryad.** Only the name says so.

**5.20 Surnames beyond the plan** for the Approach NPCs (4.6).

**5.21 Dark content.** The Nest's small bones and skulls "not rats'", the child's scratchings crossed out in the Warrens, human bones in the Barrow, Fulk's "last man", and the taken clerk. Keep, soften or cut.

**5.22 Aldith against the sign.** Aldith asks questions of travelers while a sign at the inn saying no questions are asked.

**5.23 The Old Way** east of the Crossroads Stone leads to no room (thread 2.10-F).

**5.24 Duplicate names.** No two NPCs share a name. Shared surnames: Hale (5.1). Two items share the name Bread and Cheese (5.4). The Marked Oak, Last Great Oak and the Great Oak of Oakenfell are three different trees.

### Unclear or cut-off passages

**5.25** The notebooks, ledger and journals break off deliberately (the prospector's "West, then north, then", the hide notebook's torn page, the watchtower ledger, the three journals). These are intended, not defects.

### Poor taste

The items under 5.21 are the only ones I would flag. There is no slur or sexual content anywhere.

---

## 6. Every NPC

There are 40 NPC files and the table has 40 rows. The role and the line on what they tell the player are summaries; the quoted words are in the Key passages above and in each file.

| Name | Role | Room | File | What they tell the player |
|---|---|---|---|---|
| Ives | Drover, resting | Traveler's Rest | `south_road/npc/drover_ives.cs` | Oakenfell is two days from the Hollow (a day and a half with an empty wagon); the Rangers' outpost is half a day into the wood; wolves, and goblins "they say". |
| Edda | Miller's daughter | Old Miller's Farm, the Yard | `south_road/npc/farmwife_edda.cs` | Wolves have come to the hen-house; rangers "say they will send someone"; travelers who leave the road come back "Walking slow."; only Mildred visits the farm. |
| Cuthbert | Angler | The Stone Bridge | `south_road/npc/fisherman_cuthbert.cs` | The bridge is "older than the Assembly" and the mason's mark is underwater in the keystone; wolves drink at dusk; Mildred is the only ranger he trusts. |
| Aldith | Innkeeper and shopkeeper | Traveler's Rest | `south_road/npc/innkeeper_aldith.cs` | Sells bread and cheese, a mutton pasty and a bed (see prices); says the Rest has stood three hundred years and the yard is safe; sends you to Ives, Jocosa and Kenrick for gossip. |
| Old Leofric | Miller | Old Miller's Farm, the Yard | `south_road/npc/miller_leofric.cs` | Mill and farm history; a wolf and a den on the western slope; Hale's Acre failed and "The thorn took the place"; the hawthorn ring in the brook pasture is never cut. |
| Hamo | Mill hand | Windmill Hill | `south_road/npc/millhand_hamo.cs` | From the hill he sees lights moving in the Eldergreen and a small shielded fire at the old sheepfold: someone is watching the road. |
| Jocosa | Pedlar (no stock to sell) | Traveler's Rest | `south_road/npc/pedlar_jocosa.cs` | Previews the road: the Merchant Camp, the tended shrine, the Crossroads Stone ("Don't take it. I took it once. I got out."), Oakenfell's walls and market. |
| Ranger Mildred | Riverwatch ranger | The Stone Bridge | `south_road/npc/ranger_mildred.cs` | Riverwatch HQ is Watchfort Crossing, next post is the Outpost half a day north; a watcher on the sheepfold ridge for a fortnight; hawthorn rings and rag-knots she leaves alone. |
| Bram | Shepherd | The South Road, the Sheepwalk | `south_road/npc/shepherd_bram.cs` | Lost a lamb to a stray wolf; his dog will not enter the old fold on the ridge; his grandfather's flock once lived there. |
| Kenrick | Old soldier, resting | Traveler's Rest | `south_road/npc/veteran_kenrick.cs` | Goblin scouts then raiders in the deep wood; a lookout at the old sheepfold; carved ruins in the Heartwood older than Greenvale; lost two fingers to a goblin warrior. |
| Alwin | Forester | The Forester's Cabin | `eldergreen_edge/npc/alwin.cs` | Blaze rules (white paint keeps a tree); a great carved oak deep east that neither he nor the Rangers made; the waystone was struck by lightning "before my time". |
| Berenice | Charcoal burner | The Edge Road, the Charcoal Clearing | `eldergreen_edge/npc/berenice.cs` | How a clamp is burned; the trail east through the Birch Stand "gets quiet", so the burners do not go that way. |
| Cadoc | Charcoal burner (night watch) | The Edge Road, the Charcoal Clearing | `eldergreen_edge/npc/cadoc.cs` | Saw small lights bobbing low in the trees to the east three nights back and told the Rangers; has heard of the Marked Oak and will not go near. |
| Delwyn | Hunter | The Hunter's Camp | `eldergreen_edge/npc/delwyn.cs` | Goblins carry cords and "Morwenna said they counted"; Morwenna went out on the east trail in autumn and never reached the Rangers; animals never cross the ring round the carved oak. |
| Elowen | Hunter | The Hunter's Camp | `eldergreen_edge/npc/elowen.cs` | Morwenna was her friend and found something while tracking goblins; she kept a hide in a tree somewhere in the deep wood with her notes; somebody sweeps the ground round the oak. |
| Finnian | Woodcutter | The Woodcutters' Ride | `eldergreen_edge/npc/finnian.cs` | Cuts only what Alwin blazes; Oakenfell buys his oak; he leaves the ride before dusk because of goblins he hears in the evenings. |
| Commander Gwenllian | Riverwatch commander, Edge Post | The Riverwatch Outpost | `eldergreen_edge/npc/gwenllian.cs` | Edge Post is the main station on this road; goblin scouts count traffic and "report to something north"; Morwenna is missing; old groves and marks must not be cut or moved; show the quartermaster proof of kills. |
| Hywel | Veteran Riverwatch ranger | The Riverwatch Outpost | `eldergreen_edge/npc/hywel.cs` | Thirty years in the wood; taught Morwenna to read a track; has seen the marks but never who makes them; the order "smaller than it was". |
| Idris | Young Riverwatch ranger | The Riverwatch Outpost | `eldergreen_edge/npc/idris.cs` | Saw a goblin scout vanish on the east trail; goblins carry knotted cords and count wagons and men; Watchfort Crossing has a hall with every Ranger's name carved in it. |
| Tegan | Outpost quartermaster | The Riverwatch Outpost | `eldergreen_edge/npc/tegan.cs` | Keeps the bounty board; bring proof of a kill and she writes it in the book; wolf pelts sell in Oakenfell; names the herbs that grow along the road. |
| Drogo | Free Company sellsword | The Merchant Camp | `green_road/npc/drogo.cs` | "Coin before crown"; the tower on the knoll "was theirs once. Whatever they left in it, they left on purpose"; bandits send a boy to count your guards. |
| Fulk | Tinker | The Green Road, the Wagon Halt | `green_road/npc/fulk.cs` | Waits for a wagon north; do not take the west path to the bear trees, he has "seen what is left of the last man"; the only sweet water is the trough. |
| Hereward | Caravan guard captain | The Merchant Camp | `green_road/npc/hereward.cs` | Goblin raiders work the Raiders' Ford and the deep wood to the west; the Ranger post on the knoll is empty "and I have heard every kind of reason why"; he has never taken the Old Way. |
| Isabeau | Pilgrim | The Old Shrine | `green_road/npc/isabeau.cs` | Walking to Oakenfell to find a brother who took this road in spring and has not written; ties a ribbon at the shrine; afraid of the dark. |
| Lambert | Caravan merchant (shopkeeper) | The Merchant Camp | `green_road/npc/lambert.cs` | Sells healing potions, trail rations and camp pasties above city prices; camp fee is a copper a head; four wagons and sixteen oxen under a Consortium contract. |
| Melisande | Keeper of the Old Shrine | The Old Shrine | `green_road/npc/melisande.cs` | Nobody knows who built the shrine; the lamp has never gone out; she does not place or move the acorns and knotted twig that appear in the night; the Rangers stopped sending a man for lamp-oil when the post was given up. |
| Wystan | Caravan clerk | The Merchant Camp | `green_road/npc/wystan.cs` | Goblins took a clerk from the camp two caravans ago, "Not killed him, taken"; counts bales and kegs constantly; leaves a ribbon at the Old Shrine each run. |
| Cassian | Riverwatch hunter at the Memorial | The Hunter's Memorial | `heartwood/npc/cassian.cs` | The cairn names those who did not come back (Edric was his first partner); the east trail is the Stalker's Run; ruins older than the Riverwatch and Oakenfell; someone keeps the Ritual Stones and he leaves the thread as he finds it. |
| Lysander | Hurrying traveler (courier) | The Heartwood Road, the Paved Remnant | `heartwood/npc/lysander.cs` | Wants to be through before dark; walk on the stone paving; Oakenfell is half a day north past the big oak. |
| Marcella | Scholar of the Athenaeum | The Mossgate Ruins | `heartwood/npc/marcella.cs` | Has drawn the Mossgate gateway eleven times; the carving repeats in sevens; the stone is not from anywhere in Greenvale; Oakenfell is nearly four hundred years old and this is older. |
| Jessamy Wick | Carter (flour) | Southfarm Hamlet, Mill Lane | `approach/npc/carter.cs` | Drives flour to Oakenfell daily by the Southgate and stays on the stone; stops on the Rise every day to look. |
| Diggory Marsh | Drover | The Merchant Staging Grounds | `approach/npc/drover_diggory.cs` | The Heartwood is the worst stretch and the oxen hate it; he does not go west of the paving. |
| Lorcan Pike | Drover | The Merchant Staging Grounds | `approach/npc/drover_lorcan.cs` | The Rise is worth the walk; a bandit camp west of the road past the arches under a big man; a den of old goblin soldiers with a drill; his gran said the aqueduct ran "to somewhere else". |
| Griselda Hobday | Farm wife (shopkeeper) | Southfarm Hamlet, the Green | `approach/npc/farm_wife.cs` | Sells bread and cheese and honey baked apples; eleven cottages, one mill; nobody goes west of the last hedge. |
| Florian Thatch | Old farmer | Southfarm Hamlet, the Green | `approach/npc/old_farmer.cs` | The aqueduct's water "ran to a place that isn't there now"; the last sheaf stays on the post; the farmers leave the land past the last hedge alone. |
| Constance Ardley | Riverwatch ranger (River Bend) | The Approach Road, the River Bend | `approach/npc/ranger.cs` | The road is the Rangers' as far as the Toll House; they know the bandit camp but lack the strength to take it; a big cat above the old cistern; the Rangers keep an office in Oakenfell by the quay. |
| Philippa Reeve | Consortium tally clerk | The Merchant Staging Grounds | `approach/npc/tally_clerk.cs` | Counts every wagon; carries the cash south on the Rise road every month and has never been stopped, which frightens her. |
| Oswin Hale | Assembly guard | The Greenvale Toll House | `approach/npc/toll_guard.cs` | Waves pass-holders through; knows the bandit camp in the west woods and calls it not his job; the Assembly keeps the arch standing "out of habit". |
| Humphrey Cobb | Toll keeper (Assembly) | The Greenvale Toll House | `approach/npc/toll_keeper.cs` | Wagons pay by axle and load, herds by the ten head, walkers and riders nothing; the Travel Pass is "for the Northgate, not for me"; evasive about the track to the old arches. |
| Baldric Teague | Consortium wagon master | The Merchant Staging Grounds | `approach/npc/wagon_master.cs` | Caravans form up here for the road south (three days heavy, two light); bandits know which row has a Ranger escort, "Somebody talks"; Dorrin Stavely at the Caravan Office hires guards. |

---

## 7. Checks run

- `AddReadable` calls: 57 in 52 files; section 3 entries: 57.
- NPC files: 40; section 6 rows: 40.
- Quotes in this document were produced from the game files by script, and a second script checked every quoted span against the game text. Ten randomly chosen blockquoted passages were compared character by character (whitespace normalised) with the raw `.cs` files after the build; all ten matched exactly and no drift needed fixing.
