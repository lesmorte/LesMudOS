# Search and hidden things (proposed 24 November 2026; not built)

Owner's idea (24 November 2026): "'hidden loot' so the recurve bow could be hidden in the stash room requiring a search (and successful skill check) to find." Every number here is a starting value for play-testing; none has been run in the game. Where this document says "today" it means the files in the repository on 24 November 2026.

## Decisions for the owner
Each has a recommendation; the spec is written as if you accept all of them.
1. **Extend what exists, do not add a second system.** A `search` command and hidden exits (`HiddenExit`, saved per character) are already in the game, with a flat chance per try and no wait. This spec replaces the flat chance with the Insight check below, adds the wait, and builds the other hidden things on the same record. Consequence: the eight hidden ways (seven in the Corridor, one in Elder Lane) keep their layout, but they now also depend on Insight and the Scout's bonus, and a failed try costs a wait. Their old percentages are converted so that an Insight 5 character who is not a Scout finds them exactly as often as before (section 9). Recommendation: yes.
2. **Failure costs a 60-second wait per character and room, not saved.** Recommendation: yes. A second, longer wait after many failures is not proposed.
3. **Numbers: one Insight point is worth 5% (one point on a d20); difficulties Easy 7, Moderate 12, Hard 17; the Scout gets +2 from a new passive trait, Keen Eye.** An Insight 5 character finds a Moderate thing 45% of the time per try; a Scout 55%. Alternative: half a point per Insight point, which flattens the gap between a Warrior and a Mage. Recommendation: as written (Insight is the Mage's attribute, and finding things should reward it).
4. **A search takes 3 seconds, can be broken off by being attacked at no cost, and ends sneaking the moment it begins.** Recommendation: yes. (It is loud: searching a room while its guards have not noticed you wakes them, like opening a chest.)
5. **The Warden's Recurve is Moderate (12).** The key and the Guard Post fight already gate the room, so the search is a short puzzle, not a grind. Expected time to find it, a character who has to wait out each failure: Insight 5 non-Scout about 2.3 minutes, Scout of Insight 6 about 1.8. Alternative: Hard (17), about 5 minutes for a Warrior. Recommendation: Moderate. (If the cellars go dark as in docs/specs/light.md, the unlit room costs 5 more points: Section 8 gives both.)
6. **Anyone who has already taken the Recurve keeps seeing the bundle as they do now; anyone who has been to the cache but did not take it must now search.** Recommendation: yes; nobody loses anything.
7. **The hint is written into the room's own details, and the detail says "search" in plain words** ("You could {cmd}search{/} it."), so a new player needs no guide. Recommendation: yes for the first few hidden things a character will meet (the Tavern Cellar's flagstone, the Storage Chamber's sacks), and for the rest a hint that says only what a careful eye would notice.
8. **Small once-per-reset hidden items are allowed** (a jar of honey, a herb patch). They are a trickle of cheap materials, not gold. Recommendation: yes; all treasure in gold stays a once-per-character find with the fall-off.
9. **The Staff of the Hollow Oak root in the Mushroom Cavern stays visible.** docs/specs/hollow-expansion.md calls it "hidden", but it is in plain view today and Halbert Quill's rumour sends the Mage to it. Recommendation: leave it, and revisit once the Recurve search has been played.
10. **Chronicle: only secret ways to new places may be a Chronicle first, through a new event `discover.hidden_find` (Renown 2, 10 for the first on the server); items, clues and nodes never are.** Wizards are left out as always. The website's filter already hides every event whose id starts with `discover.hidden`, so the new event is hidden without any change to the site. Recommendation: yes.
11. **A first search lesson in the lit Tavern Cellar: a rat-chewed purse under a flagstone (8 gold, then 4, 2, 1).** It is the only new gold. Recommendation: yes; 8 gold is a real prize to a character who starts with 10 to 20, which is the point, and the fall-off ends it after four finders.

## 1. What exists and what this changes
- `search` (in `/std/adventurer`, `CmdSearch`) is an action on the combat clock, refused in a fight. It calls `Room.Search`, which rolls each unfound hidden exit against its flat `findChance` percent. Looking at the exit's hint detail (`look at bookcase`) finds it for certain. A found exit is recorded on the character (`foundExits`, saved at once). A failed search says "You search carefully but find nothing of note."
- `/std/find` (and `/secure/finds`) is treasure that each character takes once, for less each time. A find is visible to everyone and is taken with `get`, `take` or `loot`. The Hidden Cache's oilcloth bundle is one (a keepsake: one Warden's Recurve for every character, once).
- Nothing can be hidden except an exit. Nothing has a skill check. Nothing stops a player searching a hundred times a minute.

This spec keeps all of that and adds: the check, the wait, a few more kinds of hidden thing, hints, a per-builder API, and the Hidden Cache change. It does not touch `/secure/finds`: the register still decides what is paid and who has taken it. What changes is only whether a character can *see* the find yet.

## 2. The check
When a character searches, each hidden thing in the room that they have not yet found gets its own roll.

**Found if** the d20 roll is 20, or the roll is not 1 and `roll + bonus >= difficulty`. This is the same shape as the combat hit roll (a natural 20 always works, a natural 1 never does), so the chance is always between 5% and 95%.

**bonus** = (Insight - 5) + Keen Eye (+2 for a Scout, from the trait's `search` modifier) + 3 if the search named the thing (`search sacking`) + the light penalty (section 7: 0 until docs/specs/light.md is built; then -2 in a dim room and -5 in a dark one, except for things found by feel). Insight is the attribute as the character has it now (equipment such as the Staff of the Hollow Oak counts).

**Difficulties** (named numbers in `/secure/search`, one place): Easy 7, Moderate 12, Hard 17, Very Hard 22. A builder may also write a number from 6 to 25.

Chance of finding, per try, in percent: 5 x (21 - max(2, difficulty - bonus)), kept between 5 and 95. Worked table, no light penalty, not named:

| Insight | Calling | Bonus | Easy (7) | Moderate (12) | Hard (17) | Very Hard (22) |
|---|---|---|---|---|---|---|
| 5 | not a Scout | 0 | 70% | 45% | 20% | 5% |
| 5 | Scout | +2 | 80% | 55% | 30% | 5% |
| 8 | not a Scout | +3 | 85% | 60% | 35% | 10% |
| 8 | Scout | +5 | 95% | 70% | 45% | 20% |
| 12 | not a Scout | +7 | 95% | 80% | 55% | 30% |
| 12 | Scout | +9 | 95% | 90% | 65% | 40% |

For reference the starting Insights are Warrior 5, Scout 6, Priest 6, Mage 9, Adventurer 6. Naming the thing (`search sacking` when the sacks hide something) adds 3 (15 points): a Scout of Insight 6 (bonus 3) looking for a Moderate thing by name has bonus 6: 75%.

**Why these numbers.**
- One Insight point is one d20 point (5%), because a search is rare and a character who has put points into Insight should notice. A level 10 Mage with all 18 points in Insight has Insight 27 and finds anything below Very Hard nearly every time; that is what the points are for. The 95% ceiling keeps a failed try possible.
- The three difficulties sit five points apart so that, at the Insight everyone starts near (5 to 6), Easy is "usually", Moderate is "about as often as not" and Hard is "now and then". Very Hard is for a reward that should need either luck or a character built for it.
- The Scout's +2 is "modest": 10 points, worth two Insight points, enough to matter on Hard (30% against 20%) without making the Scout the only one who finds things. It is a trait, so advanced classes that grow from the Scout inherit it.
- With a 60-second wait after every failure, the expected time to find a thing is about 1.05 minutes divided by the chance: 1.5 minutes for Easy at Insight 5, 2.3 for Moderate, 5.3 for Hard. Nothing is permanently out of reach, and nothing can be brute-forced in seconds.

## 3. The command
`search` searches the room. `search THING` searches for something at that spot: THING is a name from the room's details (`search sacking`, `search chests`), and rolls only the hidden things whose names include it, with the +3. Everyone sees the same text for a spot that hides something and one that does not.

- **Time.** The result comes 3 seconds after the command (Preparation 1.5 time units; the unit is 2 seconds); you can act again after your Reaction Time less 0.5, doubled (3 seconds at Reaction Time 2.0, 4 at 2.5). Today the result is immediate and only the recovery is 3 seconds. The room is told at the start: "Keith searches the area carefully..." (or "searches the sacking carefully...").
- **Refused** in a fight ("You cannot search while you are fighting."). It ends resting. Searching in a Safe room is allowed.
- **Sneaking.** Starting a search ends sneaking and wakes every creature that had not noticed the character, as picking up an item does (docs/specs/sneak.md): the search is not quiet. The 3 seconds then run with the creatures awake, so a Scout who searches next to unaware guards is fighting before the result.
- **Interrupted.** If a creature starts a fight with the character during the 3 seconds, the search is broken off: "You break off your search as {creature} attacks!" Nothing is found, no wait starts, and the character is not otherwise penalised. A blow that lands during a search also spoils it (the same rule as a shot being aimed), also with no wait.
- **The wait.** Any search that finishes and finds nothing starts a wait of 60 seconds (`SearchWaitSeconds`, one number) for that character in that room. Trying again early gives: "You have only just searched here; your eyes need a rest. Try again in about 40 seconds." (the time left, rounded up to ten). The wait starts whether or not the room hides anything, so the wait itself tells nobody that there is nothing to find. A search that finds something starts no wait. Leaving the room and returning does not clear it.
- **Not saved.** The waits are kept in the character's body (a small table, at most 32 rooms, old entries dropped) and are lost when they leave the game. Quitting and logging in takes a minute and a walk back from the Guest Rooms, so saving would add nothing but a file change; it is not saved.
- **Messages.** Success, one line per thing found, written by the builder (defaults below). Nothing found: "You search carefully but find nothing of note." and then "(You will need a minute before you can look this place over properly again.)" Others in the room see, on a success, "Keith finds something." and never what. A thing found is found for that character only; everyone else still has to find it.
- **Wizards** search like anyone else, are shown every hidden thing marked "(hidden)" in `look`, and are left out of the Chronicle and of the finds register as they are everywhere ("wizards make no history").

## 4. Hints: how a player knows to search
- **Every room that hides something says so in a detail**, and the detail's words are the names for `search NAME`. The hint is something a careful eye would notice: disturbed dust, a draught that makes a candle lean, a stone that sits wrong, a smell, scuff marks, sacking lying in a hump that is the wrong shape. The room's `LongDesc` mentions the first of those words, so a player can read their way to it. Lint (section 11) checks that much; the wording is the builder's.
- **Looking never finds a hidden item, node or clue.** Only an exit may be found by looking at its hint detail (and only if the builder gave one): that is how the existing ways work and they keep working. Everything else needs the search and its check.
- **The first two the player meets say "search" outright** in the detail ("You could {cmd}search{/} it."): the Tavern Cellar's flagstone and the Storage Chamber's sacks. After that a hint need not.
- **`help search`** describes the command and the wait. The `help` command line "{cmd}search{/} (for hidden ways)" becomes "(for hidden things)".
- **The website.** Nothing hidden is described on the website or in the public Chronicle. The site shows only Chronicle events on its allow-list, denies everything whose id starts with `discover.hidden` (which covers the new `discover.hidden_find`), and rejects any entry that names a forbidden term; no new page lists rooms or finds. A test (section 12) checks this.

## 5. What can be hidden
One record, one set of rules: the character's saved list of found things.

### 5.1 The record
Today a character keeps `foundExits`, a saved list of entries `room path|direction`. It becomes the list of everything the character has found, the second part being a direction for an exit (as now) or `#id` for anything else (`#` never starts a direction). The cap (1000 entries; 50 for rooms under `/wiz`) and the rule that only the room itself can write its own entry stay. `RecordFoundExit(direction)` stays as it is; a new `RecordFound(key)` does the same for any key the room declares (it checks that the room, the caller, really declares that key, as now). Old saves load unchanged. The record is written to disk when it is made.

### 5.2 Items, once per character: the hidden find
`/std/find` gets four fields: `SearchDifficulty` (0 means not hidden, as every find is today), `SearchNames` (comma-separated, as in `AddDetail`), `FoundLine` (what the finder reads) and `ByFeel` (false). A find with `SearchDifficulty > 0` is **hidden from a character until they have found it**:
- `Short()` answers null for a character who has not found it, so it is not in the room's list; `Id()` answers false, so `look at bundle` and `get bundle` say "You see no 'bundle' here."; `Long()` is never reached.
- The room's `search` finds it by the check, under the key `#` plus the find's program path (the same key the register uses), and records it. From then on, for that character, it is an ordinary find: visible, taken with `get`, `take` or `loot`, paid by `/secure/finds` once with the fall-off.
- **A character the register already lists as having taken it counts as having found it**, with no record needed. That is how everyone who has the Recurve keeps the bundle they see today.
- Use it for treasure: gold with the fall-off, keepsakes, and anything that must not be farmed.

### 5.3 Items, once per reset: `HideItem`
For small things that may come back. `HideItem(id, path, difficulty, names, foundLine, byFeel)` in a room's `Create()`: the room keeps the path and one flag, "taken since the last reset". The first character to find it (by the check) gets a clone of the item put into their hands at once, and the flag is set. A second finder in the same period is told "You find where something was kept, but the place is empty. Someone has been before you." and it counts as a failed search for the wait. The room's own timed reset clears the flag (only the room's timer can; a call from another object cannot), so the item is back at the next reset (5 minutes for most rooms; `ResetSeconds`). Nothing is recorded on the character: they may find it again every reset.
- When to use which: **once per character** (5.2) for anything worth gold or that gives equipment; **once per reset** (5.3) for a cheap consumable or material, worth a few gold at most, where a trickle is wanted and a farm costs the farmer more time than it pays. A 5-minute reset, a 60-second wait and a Moderate check give a farmer about one honey jar every three to five minutes; honey is worth 3 gold.
- A hidden item is not a spawn: it is not listed by `stat` as alive and cannot be stolen from the room before it is found.

### 5.4 Exits and secret doors
`HideExit(direction, destination, hintDetail, difficulty, foundLine, landmark)` is `HiddenExit` with a difficulty instead of a percent (and the Chronicle flag). Everything else is as today: unlisted until found, saved per character, "leaves somewhere" for those who have not found it, a wizard sees it marked, `flee` never uses it, the far room has an ordinary exit back.
- **Looking at the hint detail still finds it for certain** (`hintDetail` non-empty), exactly as today, unless the builder leaves `hintDetail` empty. A new hidden exit that should need the search leaves it empty; a puzzle exit uses the detail.
- **Doors.** A secret door is a hidden exit that is also a door: declare `HideExit(...)`, then `AddDoor(...)` on the same direction, and the far room's `AddDoor` as usual (docs/BUILDING.md, "A room with a door"). Finding the exit shows the way; the door still has to be opened, and opening is loud for a sneaking Scout as always. Today `AddDoor` refuses a direction that is not in `Exits` (a hidden exit is kept apart), so it must accept a hidden direction too, taking the far room from the hidden exit; the door's closed-state checks already run on any usable exit. The lint check for doors (Check 14) accepts a hidden exit as the exit the door is on.
- **Tracking.** Unchanged (docs/specs/skill-ranks.md): it never passes a closed door or a hidden way the character has not found, never marks one found, and at rank II adds the one line "Something is close by, though you cannot tell where." for creatures behind either. A character who has found the way and the door is open has an ordinary exit.

### 5.5 Gathering nodes: `HideNode`
`HideNode(id, path, difficulty, names, foundLine, byFeel)` puts a `/std/node` in the room, as `AddNode` does, but the node is hidden from every character who has not found it: its `Short()` is null for them (not in the list), `harvest` with no word skips it, and `TryGather` refuses a character who has not found it. The charges and regrowth are shared, as for any node. Found per character, saved as `#id`.

### 5.6 Written clues: `HideClue`
`HideClue(id, difficulty, names, foundLine, readNames, text)` hides a scrap of writing. Found per character (saved as `#id`); after that `read READNAME` shows the text to that character, and to nobody else. A clue is for hints and lore. It cannot hide an item.

### 5.7 How it joins `/secure/finds`
It does not replace it. Finds keep their whole rule (who has taken what, the fall-off, the register, the save order). Hiding adds a second question in front of it: has this character found it? The register is not asked until they have, and the register's "Taken" answer counts as found. The two records are separate on purpose: the register is the world's (who has taken what, in order), the found list is the character's (what they have noticed). Both survive a restart; neither can be written by anything but the object that owns it.

## 6. The Hidden Cache: the Warden's Recurve
- **The rusty key still opens the room.** (The Goblin Guard who keeps the Rusty Key stands in the Guard Post, not in the Overseer Hall: it is the "Goblin Guard (keyholder)", `AddLoot("/domains/hollow/obj/rusty_key", 100)`. The Elite Guards drop the Captain's Sabre. Nothing about the key changes.)
- **The oilcloth bundle becomes a hidden find.** In `/domains/hollow/obj/cache_bundle`: `SearchDifficulty = 12` (Moderate), `SearchNames = "sacking,rotted sacking,chests,stack,hump"`, and
  `FoundLine = "You lift the rotted sacking and find, laid along the top of the stack where the damp could not reach it, a long bundle wrapped in greased oilcloth and tied with green cord."`
  Everything else in the file stays (the keepsake, the item line, the taken lines).
- **The room's hint** (`/domains/hollow/cellars/hidden_cache.cs`): the `LongDesc` gains a sentence, "Rotted sacking has been thrown over the top of the stacked chests, and it lies in a long, stiff hump." A new detail `sacking,rotted sacking,hump`: "Grey sacking, stiff as bark with damp. It has been spread over the stack with some care, and the hump in it is too long and too straight to be a chest lid. You could {cmd}search{/} it." The existing `chests` detail is unchanged.
- **Unseen until found.** A character who has not found the bundle sees no bundle in the room and cannot `get` or `look at` it. Everything else in the room (the old coins, the trinket, the dagger that sometimes lies there) is visible and works as today.
- **Who already has it.** A character the register lists as having taken the Recurve sees the bundle as today: "a long oilcloth wrapping, folded open", and the room's list says it is there. A character who has been in the room but not taken it must search.
- **The numbers in the dark.** If docs/specs/light.md is built the room is dark (the search penalty is 5 unless a light is carried). Chance per try for the Recurve (Moderate 12), Insight 5 non-Scout / Scout of Insight 6 (bonus 3): no light 20% / 35%; one torch (the room becomes dim, penalty 2) 35% / 50%; a lantern (the room is lit) 45% / 60%. Naming it (`search sacking`) adds 15 points to each. That is the intended trade: bring a light to a place you want to search.
- **The Chronicle.** Entering the cache still reports `discover.hidden` once (the room's `Init`), as today. Finding the bundle reports nothing.

## 7. Other hidden things (small; none gives Goblin Blood)
Each is written in the setting's plain voice. Gold is only in the first.

| Room | Hidden thing | Kind | Difficulty | Hint | Reward |
|---|---|---|---|---|---|
| Tavern Cellar (in the Hollow, lit) | A rat-chewed purse under a flagstone | find, once per character | Easy 7 | A flat stone by the barrels sits a little crooked in the dirt (detail says "{cmd}search{/}") | 8 gold, then 4, 2, 1 (`Gold = 8`, `FallOff = 50`) |
| Storage Chamber | A jar of honey in the grain sacks | `HideItem`, once per reset | Easy 7 | One sack at the bottom of the heap is the wrong shape for grain (detail says "{cmd}search{/}") | `/domains/hollow/obj/craft/honey` (worth 3) |
| Smuggler Hall | A smuggler's tally behind a lantern hook | `HideClue` | Moderate 12 | Crusts of tallow and a crack behind the last hook | A scrap that points at the Recurve (below) |
| Mushroom Cavern | A herb patch in a crack beyond the glow | `HideNode`, by feel | Moderate 12 | A cold draught and a green, sharp smell where the light gives out | Two charges of healing herb, back in 15 minutes |

Texts (builders may polish; keep to the voice):
- **Purse.** Detail `flagstone,flat stone,stone`: "A flat stone about the size of a bread board, set into the dirt by the barrels. It sits a finger's width higher than the floor around it, and the dirt at its edges is scuffed, as if something small has been at it. You could {cmd}search{/} it." The room's `LongDesc` gains "One flat stone by the barrels sits a little crooked in the dirt." Found: "You work your fingers under the edge of the stone and lever it up. In a hollow scraped out of the dirt lies a small leather purse, chewed at one corner." Item: Name "Rat-Chewed Purse", ids `purse`, `leather purse`, `rat-chewed purse`, short "a rat-chewed purse", `GoldLine` "You shake %gold% gold out of the purse." Taken: "an empty rat-chewed purse".
- **Honey.** The existing `sacks` detail gets one more sentence: "One of them, at the bottom of the heap, is the wrong shape for grain. You could {cmd}search{/} the sacks." The room's `LongDesc` gains "A slumped heap of grain sacks fills one corner." Found: "Under the rotted sacks, in a gap between the stones, someone has kept a stoppered jar of honey out of the damp. You take it." Second finder: the empty line in 5.3.
- **Tally.** The existing `hooks` detail gains: "Behind the last one a crack runs back into the stone, and something pale is wedged in it." Found: "Behind the last hook, wedged into a crack and forgotten, is a slip of greasy paper." `read slip`:
  "A smuggler's tally, in a quick, cramped hand.\n  Casks, six. Cloth, nine bolts. Lamp-oil, a dozen flasks.\n  The long things go in the dry room, up on top and under sacking. Never on the floor. They are the Warden's, and he will ask.\n  The key stays with the post. Whoever holds the post holds the door."
  It names no one and nothing beyond the cache, the key and the guard who holds it.
- **Herbs.** A new node file (`/domains/hollow/obj/cave_herbs`, Kind "herb patch" so the existing yield line applies, `Charges = 2`, `RegrowSeconds = 900`, `Difficulty = 3`): short "a crowd of pale herbs in a crack", full "A handful of pale-leaved herbs grows in a crack at the foot of the wall, fed by the seep and the spore dust. Not many; the smell of them is a healing herb's, for all their colour.", picked "A crack in the wall, with a few pale stalks left." Found: "Where the glow gives out, you find a crack at knee height, and in it a small crowd of pale herbs. They smell like healing herb." The `LongDesc` gains "Beyond the farthest cluster the wall is dark and damp, and a cold draught carries a green, sharp smell." New detail `draught,smell,damp wall,wall,crack`.

None of these mentions the Circle of Ash, Philippa Reeve or anyone's informer, and none gives Goblin Blood (which stays an item only a goblin drops).

## 8. Chronicle, GMCP, the website
- **Chronicle.** A secret way to a new place may be marked `landmark: true` in `HideExit`. The first time a character finds it, the room's `ThingFound(who, id)` (a new protected hook beside `ExitFound`, which stays) reports the new event `discover.hidden_find` with the room as the subject, as `Init` does for `discover.hidden` today. The event is added to `mudlib/events/discover.cs` with `first=yes`, Renown 2 and 10 for the first on the server (the cache's own entry is 5 and 25: finding a way is smaller than finding a place). Wizards are not credited by the event service. Nothing else reports: items, clues and nodes never reach the Chronicle. No exit in the Hollow or the cellars is a landmark now; the Corridor's seven hidden ways and Elder Lane's may be, one at a time, as the owner chooses.
- **GMCP.** Nothing changes in the packages. `Room.Info` already lists a hidden exit only for a character who has found it and is sent again when the list changes. Hidden items, nodes and clues are not exits and are never in any package ("nothing is sent that the player could not learn from the text of the game"). The wait, the search and the found list are not sent.
- **Website.** See section 4.

## 9. The per-builder API
All in `/std/room`; a room declares each hidden thing in one or two lines. Difficulty names are constants of `/std/room`: `SearchEasy` (7), `SearchModerate` (12), `SearchHard` (17), `SearchVeryHard` (22).

```csharp
// An exit (unlisted, and unwalkable until found).
HideExit("west", "/domains/example/hidden_cellar", "", SearchHard, "A seam in the panelling gives. A way leads {cmd}west{/}.", landmark: true);
// A once-per-reset item (one finder takes it; back after the next reset).
HideItem("honey", "/domains/hollow/obj/craft/honey", SearchEasy, "sacks,sack,grain sacks", "Under the sacks you find a jar of honey.");
// A node nobody can see or gather from until they find it.
HideNode("herbs", "/domains/hollow/obj/cave_herbs", SearchModerate, "crack,draught,smell", "...", byFeel: true);
// A written clue; "read slip" works for finders only.
HideClue("tally", SearchModerate, "hooks,hook", "Behind the last hook is a slip of paper.", "slip,paper", "The text of the clue.");
```
A once-per-character hidden item is not a room line: it is a find file (docs/BUILDING.md, "A find") with `SearchDifficulty`, `SearchNames`, `FoundLine` set in its `Create()`, spawned with `AddSpawn` as now.

- **`HiddenExit(direction, destination, hintDetail, findChance, foundLine)` stays** and keeps working; its percent is converted to a difficulty: difficulty = 21 - percent / 5 (rounded down), and 0 percent means "found by the detail only". An Insight 5 non-Scout finds it as often as before: 10% becomes 19, 15 becomes 18, 20 becomes 17 (Hard). New exits use `HideExit`.
- **Names.** Every name given is also an `AddDetail` key of the room (so `look at NAME` works and `search NAME` is a spot the player can see).
- **`protected virtual void ThingFound(ObjectRef who, string id)`** runs once per character and thing, as `ExitFound` does, after the finding is recorded; a fault in it does not undo the finding.
- **Wizards' view.** `stat here` lists every hidden thing with its id, kind, difficulty, names, and for a once-per-reset item whether it is taken this period.
- **Where it works.** The rules of finds apply: a hidden find, node or item must be a file in a trusted tree and in a room of a trusted tree; one under `/wiz` hides and finds as a test would, is capped as `foundExits` is, and pays nothing from the register.

## 10. Files to change
| File | Change |
|---|---|
| `mudlib/secure/search.cs` (new) | The rules: difficulties, `Chance(difficulty, bonus)`, `Found(roll, ...)`, the dice (`SetTestRolls("d20:NN")`, only trusted code for a wizard of level 10, as in `/secure/sneak`), `SearchWaitSeconds`. Blueprint only; nothing saved. |
| `mudlib/std/room.cs` | `AddDoor` accepts a hidden direction; `HideExit`, `HideItem`, `HideNode`, `HideClue`, the difficulty constants; the hidden table generalised (kind, difficulty, names, found line, by-feel flag, landmark); `Search(who, named)` rewritten to the check; `Examined` unchanged; `QueryReadable` answers a clue only to finders; `ThingFound`; the once-per-reset flag cleared only in the timed reset; `stat` and the wizard markers; `QueryHiddenKeys` for the record's check. `HiddenExit` becomes a wrapper. |
| `mudlib/std/adventurer.cs` | `CmdSearch` takes a word, checks the wait and the fight, ends sneaking and rest, queues the action with Preparation 1.5 and a wind-up; `Special` resolves it; `Ambushed` breaks it off; the wait table; `QuerySearchBonus`; `RecordFound(key)` beside `RecordFoundExit`; `QueryFound(room, key)`. |
| `mudlib/std/living.cs` | A small protected method that abandons an action in its Preparation without refunds or penalties (used by the search). The search action sets `Spoilable`. |
| `mudlib/std/find.cs` | The four fields; viewer-aware `Short()` and `Id()`; `TryTake` requires "found"; `QuerySearchDifficulty`, `QuerySearchNames`, `QuerySearchKey`, `QueryFoundLine`, `QueryByFeel`; `Stat`. |
| `mudlib/std/node.cs` | A hidden flag and the viewer test; `TryGather` refuses non-finders. |
| `mudlib/secure/crafting.cs` | `harvest` with no word picks only a node the character can see (the line that takes the first `IsNode`). |
| `mudlib/skills/keen_eye.cs` (new), `mudlib/classes/scout.cs` | A passive trait with `Mods["search"] = 2`; the Scout's `Traits` list gains `keen_eye`; its description says so. |
| `mudlib/std/player.cs` | `help search`; the commands line. |
| `mudlib/events/discover.cs` | `discover.hidden_find`. |
| `mudlib/domains/hollow/...` | `obj/cache_bundle.cs`, `cellars/hidden_cache.cs` (section 6); the four things in section 7: `village/cellar.cs`, `obj/cellar_purse.cs` (new), `cellars/storage_chamber.cs`, `cellars/smuggler_hall.cs`, `cellars/mushroom_cavern.cs`, `obj/cave_herbs.cs` (new). |
| `tests/lint_area.py`, `tests/search_test.py` (new), `tests/find_test.py`, `tests/world_test.py`, `tests/hollow_expansion_test.py`, `tests/gmcp_test.py`, `tests/sneak_test.py` | Section 12. |
| `docs/BUILDING.md`, `docs/GAME_SYSTEMS.md` | A new "A hidden thing" section; the hidden-exit paragraphs; the find section's "visible" claims. |

## 11. The lint rule
`tests/lint_area.py` gets Check 15, for rooms (by reading the room's tokens, as Check 14 reads `AddDoor`) and for the finds a room spawns (read from the find's file):
- ERROR: a hidden thing's id is not unique in its room; its difficulty is not a constant or a number from 6 to 25; its path does not exist (Check 3 already does this for strings); an exit with no `Exits` entry for a `HideExit`'s far room; a clue with no `readNames`.
- ERROR: none of its `names` is an `AddDetail` key of the room.
- WARN: the first of its `names` does not appear in the room's `LongDesc` (the player cannot read their way to it).
- WARN: a hidden find with `Gold > 20` or a `HideItem` whose item is worth more than 5 gold (the rule in 5.3).
- WARN: a hidden thing in a room that has a shopkeeper (a shop is not a search puzzle).

## 12. Tests
`tests/search_test.py` (new), real characters over TCP with the dice fixed through `/secure/search`'s test hook:
- **The check.** For Insight 5, 8 and 12 (placed with `improve`), Scout and non-Scout, at Easy, Moderate and Hard, the rolls that succeed are exactly those the formula gives (roll 1 never, roll 20 always); the table in section 2 is generated from the same function and compared with the numbers in this document. Naming a thing adds 3. A thing the search did not name is not rolled.
- **Time and the wait.** The result comes after 3 seconds, not before. A second `search` inside 60 seconds is refused with the time left; after 60 seconds it works; the wait applies in a room with nothing hidden exactly as in one with something; a success starts none; leaving and returning does not clear it; it does not survive quitting. Fixed in the other direction: the wait is per room (searching elsewhere is allowed).
- **Interruption and sneaking.** A creature that starts a fight during the 3 seconds breaks the search with no wait; a blow spoils it. A sneaking Scout who starts a search is no longer sneaking and the unaware creature engages. Refused in a fight; ends resting.
- **Hidden exits.** The old behaviour (`world_test.py`: detail finds it for certain, found is saved, others see "leaves somewhere", a wizard sees it marked) still passes; `HiddenExit` percent conversions give the same chance at Insight 5; `HideExit` with a door.
- **Hidden finds.** The Hidden Cache's bundle: invisible to `look`, `get bundle` and `look at bundle` before it is found; found at the fixed roll; taken once; a second character still must find it; a character the register lists as having taken it sees it without searching; a restart keeps both records; a wizard sees it and takes nothing.
- **Once per reset.** First finder gets the jar, the second is told it is empty and waits; after the room's timed reset a character finds it again; a call from another object cannot clear the flag or take the item.
- **Nodes and clues.** The herb patch is not listed, not harvestable (`harvest` and `harvest patch`) and not gatherable by `TryGather` before it is found, and is for the finder; `read slip` works only after finding.
- **Chronicle.** A landmark exit reports `discover.hidden_find` once per character, the first finder is the server's first, a wizard's find reports nothing. The public filter hides the entry (a web test row).
- **GMCP.** A hidden exit is absent from `Room.Info` and present after the find for that character only; no package names a hidden item, node or clue.
- **Security.** A wizard-written object cannot call `RecordFound` for a room it is not, cannot clear the reset flag, cannot set the dice.
- **Lint.** Check 15 reports each ERROR and WARN case in a small fixture.
- **Content.** The four rooms of section 7 and the Hidden Cache: hint in `look`, the found lines, every hint word is a detail, the honey and herbs rules, the purse's 8, 4, 2, 1.
- Changes to existing tests: `find_test.py` and `hollow_expansion_test.py` take the bundle only after finding it; `world_test.py` keeps its search checks and fixes the dice; `sneak_test.py` gains the search-ends-sneaking case.

## 13. Build order
Each stage can be merged and played.
1. **The check and the wait.** `/secure/search`, `CmdSearch`, Keen Eye, the conversion of `HiddenExit`, the help text; the Corridor's seven ways and Elder Lane's play as before for an Insight 5 character.
2. **The record and the hidden find.** The generalised record, `/std/find` hidden fields, the Hidden Cache, the Tavern Cellar's purse.
3. **The other hidden things.** `HideItem`, `HideNode`, `HideClue`, `HideExit`, and the three cellar things.
4. **Lint, Chronicle landmark, docs.** Check 15, `discover.hidden_find`, `ThingFound`, BUILDING.md and GAME_SYSTEMS.md.
After docs/specs/light.md stage 3 lands, the light penalty in the search formula switches on; until then it is 0.

## Out of scope / future
- Searching a *person* or a corpse (corpses are looted).
- Traps found by searching (a trapped chest, a pressure plate): the same check could find them; they need a trap system.
- A "detect hidden" spell or potion, and a tool (a lens, a lantern with a hooded shutter) adding to `search`: the `search` modifier already exists for them.
- Hiding the Staff of the Hollow Oak's root (decision 9).
- A group search (several characters searching together) and a "search again" that scales the chance with the number of tries.
- Hidden things with a second step (a lever found, then a door).
- Saving the wait across logins, or a longer wait after many failures.
- Hidden rooms in the cellars; the Corridor's landmark choices; a Chronicle first for the Recurve.
