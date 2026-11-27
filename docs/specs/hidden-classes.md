# Hidden classes: one pattern for the five callings (proposed 27 November 2026)

Owner's decisions (27 November 2026): the Bureaucrat "should be a 'hidden' type of class without an OBVIOUS training/pledger. I think each path should have one of these classes, with B being the mage classes hidden class." And: "a quest from something odd works well. I would say that a mage of appropriate level that doesn't have a specialty class should have an x% chance of finding a 'jury duty summons' in loot ONCE. If they arrive for jury duty the quest should begin and end with them having the option of taking the b class, this can be a pattern for all the 'hidden' classes." He also said "A bureaucrat probably isn't fighting the Overseer anyway", and he knows the specs sit in a public repository and accepts that for now.

This document is the pattern. The first use is the Bureaucrat, the Mage's hidden class, whose own spec is [bureaucrat.md](bureaucrat.md): that file holds the summons text, the rooms, the people, the quest "Jury Duty" and the spells. The other four callings get a one-paragraph sketch here (section 12) and are not designed. Every number is a starting value for play-testing and `tests/balance_sim.py`; nothing here has been run. "Today" means the files in the repository on 27 November 2026. Where this document says a thing exists, I read the file; where the world lacks what the design needs, the text says so.

## Decisions for the owner
Each has my recommendation and the alternative. The spec is written as if you accept every recommendation.

1. **One shared pattern, built once, so the other four are content only.** A hidden class is an ordinary advanced class file plus a flag (`Hidden`), one register (`/secure/hidden`), one gated door in a room, one odd item, one quest and one NPC base (`/std/hiddengiver`). Recommendation: yes. Alternative: build the Bureaucrat as a one-off and extract the pattern later; cheaper now, and the four others would then each need engine work and a fresh review of the leaks in section 2.
2. **The roll happens when the character loots the corpse, not when the creature dies.** Only the killer and those the corpse "entitles" (everyone who shared the kill) roll, once per corpse each. A scavenger who takes a corpse after the lock ends does not. Recommendation: yes: it is literally "in loot", it needs no item that only one person may take out of a shared corpse, and a corpse that rots before it is looted costs nothing. Alternative: roll at death and put the paper in the corpse marked for one character (needs a per-item owner in `/std/corpse` and loses the paper if the corpse rots; I do not recommend it).
3. **x: nothing in the first 10 eligible loots; then 2.0% on the 11th, rising by 0.5 points for each further one (2.5%, 3.0% ...); certain on the 50th.** Expected 24.8 eligible loots, median 24, nine in ten by the 37th, everyone by the 50th. At 8 to 15 eligible kills an hour (section 5.4) that is about 2.2 hours on average at 11 an hour, never in the first 45 minutes, never later than about 4.5 hours. Recommendation: this. Alternative: a flat 2% with no floor and no pity: expected 50 loots, but 36% of Mages would still be waiting after 50 and 13% after 100, and a few after 200; the owner said "once", and a hidden class that some players never meet through bad luck is a worse secret than one that every Mage meets and few find the door of.
4. **Eligible kills are creatures that a character of this level would hunt: not bosses, worth experience, from one level below the character's own to two above (never below level 9, so the window does not run off the end of the world).** Recommendation: yes (section 5.3). Alternative: any creature at all (rats and rabbits would farm it) or creatures of exactly the character's level (too narrow: most levels have one to three kinds).
5. **"Appropriate level" is level 5 and up, with no upper limit.** Advanced classes begin at 5; a character who reaches 12 without finding it still can. Recommendation: yes. Alternative: a window of 5 to 15; it ends the secret for anyone who overshoots, and the world has no creature above level 10 to roll against anyway.
6. **The summons is "served", not "found like loot": it goes straight into the pack, cannot be dropped, given or sold, is kept at death, and if it is ever gone the door re-issues a copy.** ONCE means the roll succeeds once per character, ever, recorded before the paper is handed over. Recommendation: yes. Alternative: an ordinary item; then a death with a rotted corpse, or a dropped paper, ends the secret for good, which is a bug dressed as flavour.
7. **The summons says where and when and never the word "class", "path", "seal" or the class's name.** Recommendation: yes (the text is in bureaucrat.md).
8. **The quest begins when the character presents the summons at the door. It cannot be abandoned and cannot fail.** If they leave in the middle, the case is held and resumed. Recommendation: yes: it is the first real use of the journal's `abandonable: no`, and it is what "cannot be failed by accident" needs. Alternative: abandonable like the others; then "abandon" would have to reset the case and re-offer a summons that the owner said comes once.
9. **No deadline.** The summons names a period in the Chancery's voice and a fine; the fine is never collected and the door never closes on its own. Recommendation: yes. Alternative: the door closes after 14 real days of play; a player who is away for a fortnight loses the class forever, and one can only argue that the voice demanded it.
10. **The end of the quest is a two-step choice either way: `pledge` then `pledge` again within a minute accepts, `decline` then `decline` again within a minute refuses. Refusal is permanent for that character: "The Chancery does not summon twice."** Walking away without answering is neither: the offer stays open. No pledge fee. Recommendation: yes (section 7). Alternative: a fee as the first Bureaucrat spec had (25 gold); the quest is the price now.
11. **One advanced class per character, as `ApplyClass` already enforces ("You already have a advanced class.").** A character who takes an ordinary advanced class first is never eligible; a character who holds a hidden class cannot take an ordinary one; a character with a summons outstanding who then takes an ordinary one is politely released (section 8.4). Recommendation: yes. Alternative: a second, "hidden" tier slot so a character could hold both: it needs `subclass`-like plumbing and gives every Mage both classes; not recommended.
12. **Nothing shows the class to other players, because nothing shows any class to other players today.** `who` prints a name and a title the player chose; `look at` prints a name, a title and a health line; the name banner is that same title; GMCP sends the calling only. `score` shows the character's own class. A hidden class follows the same rule and adds nothing. Recommendation: yes (the in-world title is on the character's own `score` and in the Registrar's mouth). Alternative: show the class title in `look at`; then a hidden class is the only thing anyone can see about another character, which is how rumours start.
13. **The Chronicle gets nothing: a new event `class.hidden` is scored as a routine event (no entry, no rumour, no Renown), and `class.hidden` is added to the website filter's hidden prefixes as a second lock.** The ordinary `class.advanced` is on the public list and must never be used. Recommendation: yes. Alternative: a real Chronicle entry hidden by the filter; one filter mistake would then publish it.
14. **The website's Heroes page needs a whitelist of public class ids.** I found that `/heroes` prints each top character's advanced class straight from the roster copy (section 2). Recommendation: add `publicClasses` to `publicfilter.json` (the fifteen ordinary ids) and show a class only if it is listed. The whitelist never names a hidden class. Alternative: a blacklist of hidden ids; it would have to name them in a public file.
15. **A lint check (Check 17) makes a leak fail the build** (section 10). Recommendation: yes.
16. **Hidden classes are not judged by the Overseer.** The yardstick is the character's own pace and survival in the Corridor at levels 5 to 10 against its calling and the mean of the five callings (section 11). Recommendation: yes (your words).
17. **A level-10 wizard command, `hidden`, lists who holds, has been summoned for, or has refused each hidden class, and can issue a summons to a test character.** Nobody below level 10 sees a hidden class id in any listing. Recommendation: yes: without it nobody can test the class without killing for hours.

## 1. What exists today, and what is missing
| Exists | Missing |
|---|---|
| A class is data: `/classes/<id>.cs`, tier `advanced`, `Parent`, requirements of kind `level`, `quest`, `item`, `class` (`mudlib/std/pclass.cs`). `/secure/classes` finds a new file without a restart. | A `Hidden` flag. No class has one. `EnabledOfTier` and `EnabledChildren` ("for the Hall of Legends") would list a hidden class to anything that asks. |
| `ApplyClass(id)` on the character: only another object may call it; checks the class is enabled, the tier is free, the parent is held, every requirement; says "You are now a {title}."; saves. | Nothing reports an event when it runs. The Hall's hallmasters say "The rite of pledging is not yet open." There is no `pledge` verb anywhere (CLASS_SYSTEM.md section 8 describes it). |
| The quest journal: `DefineQuest` on the giver, `SetQuest` states 0, 1, 2, tagged dialogue (`QuestSay`, `AddTopic(..., quests)`), authored completed and abandoned summaries, `abandonable: no` (built, used by no real quest). | Per-character progress inside a quest: the journal has three states and no flags. |
| `/secure/finds` and `/std/find`: a once-per-character item that a register records before it hands anything over. | A once-per-character thing that is not a find in a room: nothing records "this character was handed a paper". |
| `Monster.Die` shares a kill among everyone with damage (`EligibleDamage`); `Corpse.Entitle` lets each of them loot during the lock; the corpse keeps the creature's kind and level. | The corpse does not remember whether the creature was a boss or paid experience; nothing hooks `loot`. |
| `Room.HideExit`, `LockExit`, `AddDoor`; `City Hall` has an "inner door" in its description that leads nowhere. | An exit that does not exist for most people and does for some (a key-locked exit is listed, and a `LockExit` on an unlisted way only prints a refusal). |
| The Chronicle's event service scores an event; below 100 points it is "Routine": counted, never written, no rumour (`mudlib/secure/events.cs`, `ClassOf`). | A class event that is routine. `class.advanced` scores 4020 for the first and is on the website's public list. |

**The leaks I found, all closed by this spec** (each is a place where a hidden class would have become visible):
1. `/secure/classes.EnabledChildren(parent, "advanced")` and `EnabledOfTier`: the lists a pledging NPC or a help page would build.
2. `class.advanced` is in `publicEvents` of `web/LesMudOS.Web/publicfilter.json`; `docs/WEBSITE.md` lists it as public.
3. **The Heroes page** (`web/LesMudOS.Web/Data/GameData.cs`, line 115) prints `Character.AdvancedClass`, which `Data/SaveReader.cs` reads from `roster.json`, which the 30-second sync job in `deploy/setup-website.sh` writes from each character's save (`"class": advancedClass`). A hidden class held by a top-ten Renown character would appear on the public site.
4. The Hall's "paths" topics and banners: they are text, and say three roads per calling. They must not change. (`ask ilsabet about <anything not a key>` answers "I can't tell you anything about that.", which is the same answer for every unknown word; the hidden class gets no key.)
5. `Item.QueryReadText`, `help`, `skills`: a `help NAME` for a class the character does not hold must answer as for any unknown word; today `help` has no class topics at all, so nothing leaks, but the Bureaucrat spec's `help bureaucrat` would have.
6. Wizard tools: `stat` on a creature prints numbers; no tool lists characters' classes today. Any future one must go through `QueryVisibleClassIds(viewer)` (section 9).
7. Unlisted exits: `search` in City Hall must keep finding nothing there, and the exits line must keep saying only "out".

## 2. What a hidden class is (the rules, in eleven lines)
1. It is an advanced class (tier `advanced`) with `Hidden = true` in its file, so it takes the character's one advanced slot and is subject to every ordinary rule of `ApplyClass`.
2. Nobody offers it. No banner in the Hall of Legends, no hallmaster's remark, no trainer who advertises, no topic key on any NPC outside its own rooms, nothing on the website, in the Chronicle, in the Mudlet package or in the news.
3. It has no `help` page until the character holds it (then `help NAME` shows its description and skills, like any class the character has).
4. It is found: one odd item (the **summons**) turns up once, in loot, for an eligible character (section 5).
5. The item sends the character to one place, through a door that does not exist for anyone without it (section 6).
6. There they begin a quest, and the quest ends with the option of taking the class (section 7).
7. The place becomes the class's home: the trainer, the shop and the door stay, open only to those who hold the class or the summons (section 8).
8. Its numbers are ordinary: resources, skills, ranks by the existing ladder (skill-ranks.md), the only difference being the yardstick (section 11).
9. It is recorded in one register and one place in the character's save (`advancedClass`, as any class). Nothing else is saved.
10. Wizards (any level) never receive the summons, never take the class by the quest, and "make no history".
11. A class that is hidden is listed to nobody but a level-10 wizard, and to the character who has it.

**How the class shows once held.**
- `score`: "Mage • Class Clerk of the Chancery • Level 7", exactly as an ordinary advanced class is shown (`mudlib/std/adventurer.cs`, line 2194). This is the only place.
- `who`, `look at PLAYER`, the name banner (the player's own chosen `title`, default "the newcomer"): unchanged. A player may type any title they like (`title Clerk of the Chancery`); that is the player telling.
- GMCP: unchanged (`calling` only, no class). The Mudlet Quests panel lists "Jury Duty" and its conversation for the character who holds it (it is their own quest, like any).
- The skills appear in `skills` and level-up lines like any class's.

## 3. The register, `/secure/hidden.cs`
New, blueprint only, save file `save/secure/hidden.json`, in the style of `/secure/finds`: one daemon owns every number and every record, writes the file **before** it returns, and refuses (and records nothing) if the write fails. No object but the ones named below can change it.

**Record**, per class id and lower-case character name:

| Field | Meaning |
|---|---|
| `looted` | eligible loot events counted so far (the pity counter) |
| `issued` | 0, or the time the summons was served; `serial` its number in the order served (the reference number on the paper) |
| `stage` | 0 nothing; 1 summoned; 2 admitted (the door was passed and the quest begun); 3 sworn; 4 the case heard; 5 the offer open; 9 answered |
| `outcome` | "", "taken", "declined", "withdrawn" |
| `notes` | small named integers the class's quest needs (the case's choices); at most 16 keys, each name at most 24 characters |

**Methods** (every one reads `PreviousObject()`; all trusted-tree rules are those of `/secure/finds`: the caller is in `/std`, `/secure`, `/domains`, `/classes`, `/skills` or `/events`, never a sandbox object):
- `RollOnLoot(who, creatureLevel, creatureBoss, creatureXp, corpse)`: caller must be a corpse (`/std/corpse` clone) in a trusted room; `who` must be the player body whose command is running, in the corpse's room, awake, and not a wizard (a wizard is answered "no" and nothing is recorded). Returns the id of a class whose summons is now served, or "". Section 5.
- `MayEnter(who, gateKey)`: true if the class that owns the gate key is held by `who`, or `who`'s stage for it is 1 to 8 with no outcome. Read-only; any caller.
- `Advance(who, classId, stage)`, `Note(who, classId, key, value)`, `QueryNote(...)`, `Answer(who, classId, outcome)`: callable only by an NPC or room whose program path is one of the class's `OwnPaths` (section 4) and which stands in the same room as `who`. `Advance` only goes up (never back), except `Answer`, which is the last step.
- `Reissue(who, classId)`: from the gated door only, when the record says issued and the character's pack holds no summons.
- `QueryLine(classId)`, `QueryHolders(classId)`: level-10 wizards only (the `hidden` command, section 9).
- `SetTestRolls(...)`: only trusted code acting for a wizard of level 10, never a clone, as `/secure/sneak` does; for tests.

## 4. The class file and the fields a hidden class adds
`mudlib/classes/<id>.cs` inherits `/std/pclass` as every class does and sets, besides its own numbers:

| Field | Bureaucrat | Meaning |
|---|---|---|
| `Hidden = true` | yes | never listed; never given by a list-driven pledge |
| `Require("level", "5")`, `Require("hidden", "<id>")` | yes | a new requirement kind, `hidden`, passes only if the register says this character's offer is open (stage 5, no outcome) |
| `SummonsItem` | `/domains/greenvale/oakenfell/founders/obj/jury_summons` | the paper that is served |
| `QuestId` | `jury-duty` | the journal's id |
| `GateKey` | `inner-door` | opaque: never the class id or title (it appears in a public room file) |
| `OwnPaths` | the Chancery's files | path prefixes of every file that may speak of the class (lint, section 10; register callers) |
| `SummonsText` lines | in the class file | what the player reads at each step (found, looked at, door, knock): the room files hold none of it |
| `SummonsFloor`, `SummonsBase`, `SummonsStep`, `SummonsGuarantee` | 10, 2.0, 0.5, 50 | the roll; if absent the register's defaults |

`pclass.cs` gains `QueryHidden()` and the other `Query...` accessors (every public `Query` function is read-only for the sandbox, as the rule of docs/SANDBOX.md says), and `CheckRequirement` gains the `hidden` kind. `/secure/classes.cs`: `EnabledOfTier` and `EnabledChildren` skip a class whose `QueryHidden()` is true; a new `HiddenIds()` returns them (for the register, the lint, `hidden`).

## 5. The summons
### 5.1 Eligibility, all of it, at the moment of looting
A character `C` looting a creature's corpse is **eligible** for class `K` if every line holds:
1. `C` is a player body, not a wizard of any level, not in a wizard's workshop, not idle, and is the character whose command is running.
2. `C`'s archetype is `K`'s parent (`QueryParent()`: for the Bureaucrat, `mage`).
3. `C`'s level is at least `K`'s level requirement (5).
4. `C` holds **no advanced class** (`QueryClassOfTier("advanced") == ""`; so no subclass or prestige either). The owner's "doesn't have a specialty class."
5. The register has no `issued` for `(K, C)`: never summoned before, never refused.
6. `C` was the killer of that creature or one of the characters the corpse entitles (everyone who dealt damage and was in the room, awake: `EligibleDamage`). A character looting after the lock has run out is not.
7. The corpse is a creature's, from a trusted tree (the existing `Corpse.ReadYields` rule; a player's corpse and a wizard's creature yield nothing), and has not yet rolled for `C` (the corpse keeps a set of up to 50 names).
8. The creature was not a boss (`QueryBoss()`), paid experience (`XpValue > 0`), and its level `Lc` satisfies `min(L - 1, 9) <= Lc <= L + 2`, where `L` is `C`'s level. The `9` is one named constant, `TopOrdinaryLevel`, the highest level of a creature that is not a boss; a test reads the creature files and fails if the world gains a higher ordinary creature (so the number is raised with the content).

A creature may carry a flag to opt out (`NoSummons = true`) for any creature a builder adds that is farmable (a spawner). No creature has it now.

### 5.2 The roll
Each eligible loot event is counted first: `looted` rises by 1 and is saved. Let `n` be that count.
- `n <= SummonsFloor` (10): no roll.
- otherwise the chance is `SummonsBase + SummonsStep x (n - SummonsFloor - 1)` percent (2.0 at the 11th, 2.5, 3.0, ...) and is 100 on the `SummonsGuarantee`th (50th).
- The dice are the register's own: a number from 0 to 999 against ten times the percent (a tenth of a percent).

| Eligible loots so far | chance of this one | chance it has been served by now |
|---|---|---|
| 10 | 0% | 0% |
| 11 | 2.0% | 2.0% |
| 15 | 4.0% | 14.1% |
| 20 | 6.5% | 35.3% |
| 25 | 9.0% | 57.4% |
| 30 | 11.5% | 75.5% |
| 37 | 15.0% | 90% |
| 40 | 16.5% | 94.7% |
| 45 | 19.0% | 98.1% |
| 50 | 100% | 100% |

Mean 24.8, median 24. These figures are exact for the formula (I computed them, not simulated).

### 5.3 Why those kills
The window is "what a character of this level would hunt": the pace model's prey is the creatures of the character's level and the one above (`prey(level)` in `tests/balance_sim.py`; CORRIDOR_BALANCE.md section 2.3); the window adds one level below and one more above so that a character who steps down to finish off a lower creature, or who meets one above, is not penalised. It excludes rats and rabbits for a level 5 character (level 1 and 2 creatures: below 4) and every boss. **Creatures at those levels that exist today** (from the 44 monster files): level 4: Grey Fox, Goblin Elite Guard; level 5: Forest Spider, Stray Wolf (and the Overseer, a boss); level 6: Bandit Lookout, Wild Boar, Forest Wolf; level 7: Tunnel Rat, Goblin Raider, Boar Brute, Goblin Scout; level 8: Highway Bandit, Heartwood Goblin, Goblin Tunneler, Black Bear, Rogue Bandit, Young Black Bear, Root Creeper, Woodland Spider; level 9: Rogue Goblin Veteran, Ancient Skeleton, Thorn Woodling, Restless Spirit, Ancient Wolf, Forest Stalker; level 10: five bosses only. So a level 5 Mage has 11 kinds in the window (levels 4 to 7) and a level 8 Mage has 18; a level 10 Mage and above have the six level-9 kinds only (hence the floor of 9). **The world does not yet have what the roll needs above level 10**: no ordinary creature is above level 9, so the window for a level 12 character is the same six kinds; it works, it is thin, and it should be widened with the content. **Oakenfell itself has no creatures** (the city is peaceful: no `Monster` file under `oakenfell/`), so a Mage who never leaves the city never rolls; every eligible creature is in the Hollow's cellars (level 4 only) or the Corridor (`south_road`, `eldergreen_edge`, `green_road`, `heartwood`, `root_warrens`, `barrow`, `approach`).

### 5.4 How long it takes
Kills per hour, from CORRIDOR_BALANCE.md section 3 (the Mage at level 5: 2,189 experience an hour with a haven and potions, 1,187 with neither) divided by the average experience of the level 5 and 6 prey (135, 140, 145, 145, 185: about 150): **14.6 kills an hour at best, 7.9 at worst**; I use 8, 11 and 15. Every kill in the window is looted by a character who loots (the pace model assumes 3 seconds a corpse). Hours until the summons, by the number of eligible loots:

| | 8 an hour | 11 an hour | 15 an hour |
|---|---|---|---|
| nothing possible before | 1.3 h | 0.9 h | 0.7 h |
| mean (24.8) | 3.1 h | 2.3 h | 1.7 h |
| median (24) | 3.0 h | 2.2 h | 1.6 h |
| nine in ten by (37) | 4.6 h | 3.4 h | 2.5 h |
| certain by (50) | 6.3 h | 4.5 h | 3.3 h |

The plan's pace is about 2 hours from level 5 to 6 (CORRIDOR_BALANCE.md section 4.1), so most Mages meet it around the turn from level 5 to 6 and nearly all by 7.

### 5.5 Delivery
When `RollOnLoot` returns a class id, `Corpse.Take` or `TakeAll`, **after** the usual "You take ..." lines and before the corpse may crumble, clones the class's `SummonsItem` into the taker's pack and prints, to the taker only, the class's `SummonsFound` line (the room sees nothing; no one else can have it; there is no copy in the corpse). The register has already recorded `issued` and `serial` and saved. If the clone fails (a broken file), the record stands and the door re-issues the paper later (section 6). A character who loots a corpse and gets nothing is told nothing: nothing in the loot lines changes.

### 5.6 The paper cannot be lost
The summons item has `QuestItem = true` (shops and `give` refuse it, as for the travel pass) and a new `Bound = true`:
- `drop`: "{BoundLine}" (the item's own words; for the summons: "It is addressed to you. It is not a thing one drops.") and nothing moves.
- `give`: refused the same way.
- Death: `Adventurer.LeaveCorpse` leaves a bound item on the character.
- A wizard's `destruct` or a damaged save can still remove it: then the door says "Paper." to a character whose record says issued, and passes a duplicate through the hatch (section 6), re-cloned by `Reissue`. The register's record, not the item, is the authority.
- There is no weight in the game (checked: no weight field in `Thing` or `Item`), so "weightless" needs nothing.

### 5.7 Reading it
`read summons` prints the class's `SummonsText`: a form, in the Chancery's voice, with a reference number, the holder's name, a place (City Hall, the inner door), a period and a fine. It never says "class", "path", "seal", the class's title or what the court does. `Item.QueryReadText` becomes `virtual` so the item can print the holder's name and its serial. `look at summons` and `inventory` show "a summons".

## 6. The door
**`Room.GatedExit(direction, destination, gateKey, verbs...)`** (new, `mudlib/std/room.cs`). In City Hall: `GatedExit("in", "/domains/greenvale/oakenfell/founders/chancery", "inner-door", "in", "enter", "knock", "open")`.
- It is not in `Exits`, so it is not listed, not in `QueryExits()` and not in Tracking's "something close" (`QueryClosedDestinations` skips it), and the exits line shows "out" only. A wizard's `stat here` lists it ("Gated exit in -> path (gate inner-door)").
- The room binds the listed verbs for everyone, like its other exits, and the handler asks `/secure/hidden.MayEnter(who, gateKey)`. If **false** it returns `false` without a word, so the command falls through to "What?" exactly as `in`, `enter`, `knock` and `open door` do in City Hall today (checked: City Hall binds only `out`, and `open` is bound by a room only when it has an `AddDoor`).
- If **true**, `MoveThrough` takes the character in, with the gate's lines from the class (`GateKnock` the first time and `GateAgain` after; `GateOpen` for a holder of the class). The usual refusals apply (in a fight: "You are in the middle of a fight and cannot just walk away.").
- A character for whom it is true also sees `in` in the exits line and a line more when they `look at door` (`GateLook`, from the class). For everyone else `look at door`, `look at inner door` and `read letters` print what they print today (below).
- **What City Hall says today and keeps**: `look at door` / `inner door` / `letters` / `lintel` / `motto` / `gilt`: "Carved over the inner door, with the gilt rubbed off the high points of each letter, are the words of the city's motto. You could {cmd}read{/} them."; `read letters` (also `lintel`, `motto`, `inscription`): "Carved over the inner door:\n"FROM STRONG ROOTS GROW GREAT THINGS"". The long description mentions "the inner door" and "a line of carved letters"; it does not change. `search` finds nothing in City Hall (no `Hide...` is declared there, and none may be).
- `tests/lint_area.py` learns `GatedExit` so Check 1 (the destination exists) and Check 2 (it has a way back) cover it.
- A character who holds the class, or has the summons outstanding, passes. A character who **declined** gets "What?" again for ever; one who was **released** (8.4) likewise.

## 7. The quest
The shape every hidden class follows. Content (names, dialogue, the case) is the class's own; the Bureaucrat's is in [bureaucrat.md](bureaucrat.md).

1. **Begins** when the character passes the door with the summons: the giver NPC (in the first room beyond, an NPC inheriting `/std/hiddengiver`) notices them and calls `SetQuest(QuestId, 1, title)`; the register moves to stage 2. The quest appears in `quests` as an active quest. Nothing is asked of the player, nothing is accepted: arriving is the acceptance.
2. **Middle.** The class's own business, using existing mechanics only (`ask`, `read`, `search`, giving, a y/n prompt, one comic interruption, no combat), 15 to 25 minutes, with the character teaching themselves by example what the class does. Progress is kept in the register (`stage`, `notes`) and in what the character carries (one `have` objective for the journal's progress note). Leaving in the middle changes nothing; coming back resumes.
3. **Not abandonable.** `DefineQuest(..., abandonable: false)`. The first real quest to use it (the journal's test fixture already does). The abandoned summary text is still required by `DefineQuest` and is written, in case a wizard clears the quest.
4. **No failure.** No choice in the middle can stop the class being offered. Choices change the story, a small fee, and what the NPCs say.
5. **Ends with the offer.** The giver says the offer in the class's voice. Two verbs, bound by the giver only while the register says the offer is open (stage 5), otherwise "What?":
   - `pledge`: the first time, the giver says the **permanence line** (what is kept, what is gained, that it is for life, that no fee is asked) and the register notes a pending acceptance for 60 seconds; the second `pledge` within the minute calls `ApplyClass` (which requires the quest completed, level 5 and the `hidden` requirement), then `SetQuest(QuestId, 2, title, reward)`, `Answer(..., "taken")`, `class.hidden` (below), and the class's **accepted** lines. A `pledge` after the minute is a first `pledge` again.
   - `decline`: the first time, "You may decline. Say so again within the minute and it is closed. The Chancery does not summon twice." (the class's own words; the sentence "does not summon twice" is the line the owner's design uses); the second within the minute calls `Answer(..., "declined")`, `SetQuest(QuestId, 2, title, reward)`, and the class's **declined** lines.
   - Anything else typed does not answer. Walking away does not answer: the offer stays open for as long as the character plays, and the giver repeats it when they return.
   - The giver pays the same fee and gives the same thanks whatever is answered, so there is nothing to gain by choosing either way except the class.
6. **The journal.** One authored `completed` summary that is true whichever answer was given, and a `reward` string that says what each got ("1 gold and the seal"; "1 gold and a certificate"). Renown: none (a Renown award is announced in the room, and listed by the website's Heroes page; a hidden path must not shout). Experience: a flat 250, the same for both answers, a quest-sized reward for 15 to 25 minutes at level 5 to 6 (the pace is about 2,200 experience an hour, CORRIDOR_BALANCE.md section 3; the quest share of the budget is 20%, section 7).
7. **The event.** `/std/hiddengiver` reports `class.hidden` (never `class.advanced`), subject the class id, only for "taken". It is routine: counted, never written, no rumour, no Renown. Wizards are dropped by the event service as always.
8. **The decline closes the door** (section 6), the summons is retained by the court ("The court keeps the summons. It has kept every one."), and the character receives the class's keepsake, a certificate that reads as an exemption.

`/std/hiddengiver` (new; inherits `/std/trainer` so the same person can also teach ranks later): `AddHiddenOffer(classId, lines...)`, `Begin(who)`, `OfferOpen(who)`, the two verbs, the permanence line, the pending timers (60 seconds, kept in memory, not saved), the event report and the journal calls. A content NPC overrides only the lines.

## 8. After the class is taken
### 8.1 Home
The place that held the quest is the class's home: the door stays gated for the class's holders; the giver becomes the class's trainer; whatever the class sells (its staff and seal) is sold there. Nothing about it is marked on any map or in any list. A holder who walks the door sees the gate's `GateOpen` line.

### 8.2 Skills, ranks, prices
A hidden class gives its skills by level (the shared `SkillAt` of bureaucrat.md section 7.1: one skill at the pledge, the rest by level) at rank I, and teaches ranks by the existing ladder (skill-ranks.md): rank r needs level 5 x (r - 1), and costs II 40, III 150, IV 400, V 900 gold. The trainer (`TeachCalling(classId, 2, 5)` in the giver) is the class's home trainer; `/std/trainer.CallingOf` learns to answer by the character's advanced class when the trainer teaches it (bureaucrat.md section 7.2), so a Bureaucrat still buys Arcane Bolt ranks from the Mage hallmaster and Bureaucrat ranks from the Registrar.

### 8.3 The base calling
Everything of the archetype is kept: skills, traits, weapons and armour. An advanced class that names its own resource numbers replaces the archetype's (`RefreshClasses`); the Bureaucrat does. The ranks of the base skills are still bought from the base calling's trainers (the Hollow's, the Hall's), who never mention the hidden class.

### 8.4 Two advanced classes cannot be held: the cases
- **Ordinary class first**: the character is never eligible (5.1 line 4); `RollOnLoot` answers "no" and records nothing.
- **Hidden class held, tries an ordinary pledge**: `ApplyClass` refuses ("You already have a advanced class."; I recommend the sentence be corrected to "an" when the file is next touched). The hallmaster's refusal for a character with any advanced class is the same text whatever the class (CLASS_SYSTEM.md section 8: "A character who already has an advanced class is refused"), so nothing reveals which.
- **Summons outstanding, then an ordinary class taken** (the Hall opens its rite): at the next door knock or offer the register finds an advanced class and answers "withdrawn": the giver says the class's release line ("You have taken another seal. The Chancery does not issue a second. The fine is waived, as it was always going to be."), pays the fee, and completes the quest with the same summary and the reward "1 gold"; the door closes for them.
- **Summons outstanding, character becomes a wizard**: the door is closed to them (wizards make no history) and the record stays.

## 9. Files to change (shared by all five)
| File | Change |
|---|---|
| `mudlib/std/pclass.cs` | `Hidden`, the fields of section 4, `QueryHidden` and friends, requirement kind `hidden` |
| `mudlib/secure/classes.cs` | `EnabledOfTier` and `EnabledChildren` skip hidden; `HiddenIds()` |
| `mudlib/secure/hidden.cs` (new) | the register (section 3) and its save file; the level-10 `hidden` wizard command's data |
| `mudlib/std/corpse.cs` | remember `creatureBoss`, `creatureXp`; the roll in `Take` and `TakeAll` after the lock check and the taken lines, before `AfterTaking`; the set of names that rolled |
| `mudlib/std/monster.cs` | nothing (its `Die` already gives the corpse the facts); `NoSummons` flag read by the corpse |
| `mudlib/std/room.cs` | `GatedExit`; gated exits excluded from `QueryExits`, `QueryClosedDestinations`; shown in the exits line and `QueryUsableExits` for those who may |
| `mudlib/std/item.cs` | `Bound` flag and `QueryBound()`, `BoundLine`; `QueryReadText` virtual |
| `mudlib/std/adventurer.cs` | `CmdDrop` and `CmdGive` refuse bound items; `LeaveCorpse` keeps them; `QueryVisibleClassIds(viewer)`; `help NAME` for a class the character holds; `SkillAt` support (bureaucrat.md 7.1); the sentence "an advanced class" |
| `mudlib/std/player.cs` | `CmdHelp`: a class topic only for a holder; every other word as today |
| `mudlib/std/trainer.cs` | `CallingOf` by advanced class (bureaucrat.md 7.2) |
| `mudlib/std/hiddengiver.cs` (new) | section 7 |
| `mudlib/events/class.cs` | the new routine event `class.hidden` |
| `web/LesMudOS.Web/publicfilter.json` | `"class.hidden"` in `hiddenEventPrefixes`; `"publicClasses"` list (the fifteen ordinary ids) |
| `web/LesMudOS.Web/Data/PublicFilter.cs`, `GameData.cs`, `SaveReader.cs` | `PublicClasses`; a hero's class is shown only if listed; `roster.json` reading unchanged |
| `deploy/setup-website.sh` | the sync writes `"class": ""` for any class not in a list the script reads from `publicfilter.json` (belt and braces: the web layer is the authority, the roster copy should not even hold the id) |
| `docs/WEBSITE.md`, `docs/CLASS_SYSTEM.md`, `docs/BUILDING.md` | one paragraph each: hidden classes, the secrets list, the gate |
| `tests/lint_area.py` | Check 17 (section 10), `GatedExit` in Checks 1 and 2 |
| `tests/hidden_class_test.py` (new), `tests/class_test.py`, `tests/sandbox_test.py`, `tests/chronicle_test.py`, `web/LesMudOS.Web.Tests/SiteTests.cs`, `tests/balance_sim.py` | section 12 |

The `hidden` wizard command (level 10, in `adventurer.cs` beside the others): `hidden` lists each hidden class with counts (summoned, admitted, answered: taken, declined, withdrawn); `hidden CLASS` lists names; `hidden grant NAME CLASS` serves a summons to an online mortal (for tests; recorded as issued, with the wizard's name in the register's refusal log, not in the Chronicle); `hidden reset NAME CLASS` clears a record and the quest (a test aid; refused for a character who holds the class). Below level 10 the command is "What?".

## 10. The lint rule (Check 17 in `tests/lint_area.py`)
**Hidden classes** are the class files under `mudlib/classes` that set `Hidden = true`. For each, the lint reads its id, `ClassName`, `Title`, `Tagline`, the `OwnPaths`, and the names of its skills (`Skills.Add`) and quest id.

**"Public" means any string a player without the class, or a visitor to the site, can be shown**: every string literal (plain, interpolated or verbatim, comments excluded) in
- `mudlib/domains/**`, `mudlib/events/**`, `mudlib/std/**`, `mudlib/secure/**`, `mudlib/skills/**` and `mudlib/classes/**`, **except** files under the class's own `OwnPaths` and the class's own class and skill files;
- `web/**` (pages, content, the filter file, fixtures, tools), `clients/**`, `deploy/**`;
- `docs/*.md` other than `docs/specs/` (the specs are public and say so; the owner accepts it).
A string that begins with a path prefix (`/domains/`, `/std/` ...) is not shown to anyone and is exempt (that is how a City Hall exit can name the Chancery's room).

**ERROR** if a public string contains, case-insensitively and with punctuation ignored, any of: the class id; its `Title` or `ClassName`; its quest id with the hyphen as a space ("jury duty") **unless** the string is in the class's own files; the name of any of its skills; the first line of its `Tagline`. **ERROR** if the `GateKey` equals or contains the class id or title. **ERROR** if `class.hidden` is missing from `hiddenEventPrefixes`, or `class.advanced` is called anywhere in a file under the class's `OwnPaths`. **ERROR** if the class id is in `publicClasses`. **WARN** if a hidden class has no `OwnPaths`, or a file under `OwnPaths` has a topic key (`AddTopic`) that is also a topic key of a Hall hallmaster.

The check needs no list of hidden names: it finds them in the class files. A fixture directory in `tests/` holds a deliberately leaky class and expects each ERROR.

## 11. Balance: the yardstick for levels 5 to 10
A hidden class starts at level 5, in a city, and the owner has said it is not the Overseer's fight. The old standard test (a level 5 character against the Goblin Overseer, band 72% to 83%) is not used for any hidden class. **What replaces it**, from what the simulator already has (`python3 tests/balance_sim.py pace`, `corridor`, and CORRIDOR_BALANCE.md):
1. **Pace.** Experience an hour hunting `prey(level)` (the ordinary creatures of the character's level and one above, killed in equal numbers) at levels 5, 6, 7, 8 and 9, in the four variants of the pace model (perfect; no haven; no potions; neither). **The hidden class must be within 8% of its own base calling's experience an hour at every level, and within 12% of the harmonic mean of the five callings** (the pace table's "harmonic mean" column: 2,180 at level 5, 2,654, 3,234, 3,532, 3,797 at level 9, perfect play).
2. **Survival.** Deaths an hour in the same runs: no more than the base calling's by more than 25%.
3. **Four set pieces,** each played by the class's own policy, win rate within 10 points of the base calling: a **Wild Boar** (level 6, 40 Vitality, damage 11-19, a Charge with a one-unit wind-up: the thing an interrupt answers), a **Boar Brute** (level 7, 45 Vitality, Trample), a **Thorn Woodling** (level 9, 53 Vitality, Thorn Barrage), and a **Goblin Raider with a Goblin Scout** (levels 7 and 7, a pair). They are the corridor groups tags-and-saves.md already names.
4. **Gap between callings.** The gap between the five callings' paces at levels 5 and 10 must not be wider than it is today (skill-ranks.md says this of every calling).
5. **Bosses are reported, not required.** The level-10 bosses (Bandit Leader, Fallen Knight, Forest Panther, Warren Broodmother, Barrow Guardian) and the Overseer are run and printed for the owner, with no band.

**What exists to measure against:** Corridor creatures at level 5 to 9 (section 5.3), the five level-10 bosses, the Hollow's Overseer; Oakenfell has none. So the yardstick measures levels 5 to 9 on ordinary creatures and 10 on bosses only.
**What `tests/balance_sim.py` must model for any hidden class:** the class's resource formulas and attribute placement; its skills as actions on the combat clock with their ranks; the policy a human of the class would follow (written per class in its spec); the pace cells (`pace_cell`, `pace_mix`) with the class as a column beside the five callings; the four set pieces as new `cfight` cells; its `check` entries (class numbers equal the class file's). The summons roll is not simulated (it is a tally, and the table in 5.2 is exact).

## 12. The other four callings: sketches only (proposals for the owner, NOT designs)
Comic, played straight, rooted in places and people that exist. Each has the same pattern: an odd item in loot, one place, a quest that teaches the class by example, a clean choice at the end. The names are working titles.

| Calling | Name idea | The odd item (the summons) | Where, and the quest's hook | Flavour of its skills |
|---|---|---|---|---|
| **Warrior** | **Toll Warden** (of the Consortium's pole) | "Notice of Unclaimed Change": a receipt for a toll paid at the Approach by the bearer, "change owing: 3 gold", never refunded (the Approach has a toll pole: Humphrey Cobb, the toll keeper, and a toll guard who "stands so he can count in peace") | A back office of the Consortium House (Market District, Coin Street). The quest: collect your change; the clerks require you to hold a doorway against a queue (a crowd of NPCs, no combat) while they find the form | Holds ground by standing in the way: a hit that roots, a "Halt" that stops a creature leaving the room, a "Pay the Toll" that takes a fee from a creature's gold in damage, a block that only works while you have not moved |
| **Scout** | **Dead-Letter Runner** (the Caravan Office's undelivered post) | A parcel marked DELIVER TO: THE BEARER, found in a creature's den, return address: the bearer | The Caravan Office (Market District, a wagon yard and a slate board of routes): a back room of undelivered post; the quest: deliver three misdirected parcels in the city against the clock using only the street names on them | Fast, evasive, a nose for where things are: a strike that always lands from stealth ("Special Delivery"), a reflect ("Return to Sender"), a short sprint, a way to arrive unseen ("Forwarding Address") |
| **Priest** | **Lamplighter of the Three Nights** | The lamplighters' night rota with the bearer's name pencilled in for the Feast of the Three Days (the board in Founder's Square says a lamp is lit in every window for three nights) | A lamp-store under the Assembly Hall steward's desk in the Founder's District (new room); the quest: the night round, lighting a route of lamps (the light system, docs/specs/light.md, supplies the mechanic) before the street goes dark, and refusing a dark corner that bites | Light as a blessing: lamps that heal while they burn, a flame that is passed to an ally, a "Snuff" that dims an enemy room, a ward that holds as long as a flame does |
| **Adventurer** | **Auctioneer of the Greenvale Market** | An unclaimed lot ticket: "Lot 1: the bearer, in fair condition" (the Auction House's notice board lists lots of past sales) | The Auction House's side room (Market District): a sale you must conduct for a roomful of NPC bidders, using `give`, `appraise` and `ask`, on a lot that turns out to be yourself | The Adventurer's appraisal made into a weapon: "Going Once" (a slow that counts down), "Sold!" (a finishing blow that sells the foe), a reserve price (an armour floor), lot appraisal beyond Appraise |

Nothing in the table is built or named in game text. The Mage's is the Bureaucrat (bureaucrat.md).

## 13. Tests
`tests/hidden_class_test.py` (new, real characters over TCP, dice fixed through the register's test hook, using a fixture hidden class in the test mudlib so the pattern is tested without the Bureaucrat's content):
- **Register**: a record is written before the paper is handed over; a failed save pays nothing; a restart keeps counts and the issued flag; a wizard-written object cannot call any method (probes as in `tests/sandbox_test.py`); a clone of the daemon refuses.
- **Eligibility, one case for each line of 5.1**: wrong calling, level 4, a wizard, a character with an ordinary advanced class, a character already summoned, a scavenger after the lock, a player's corpse, a boss, a creature of experience 0 (the practice dummy), levels below and above the window, level 12 using the floor of 9, the `NoSummons` flag, the same corpse looted twice (one roll).
- **Roll**: nothing for loots 1 to 10; exactly the chances of 5.2 for loots 11, 15, 20, 37, 49 against fixed dice; certain on the 50th; the count stays after the summons is served and the roll never happens again; two characters who shared a kill roll separately and a miss for one does not affect the other.
- **Delivery and the bound paper**: the line, the item in the pack, nothing in the corpse, the room sees nothing; `drop`, `give`, `sell` refused; death keeps it; destroy it and `knock` re-issues; `read summons` has no forbidden word.
- **The door**: for a character with no summons `in`, `enter`, `knock`, `open door`, `go in` print exactly what an unknown command does; the exits line, `look`, `look at door`, `read letters` and `search` are byte-for-byte what they were; with the summons the verbs work, a closed offer or a refused one closes it again; in a fight it refuses; a holder passes; Tracking never reports it.
- **The quest** (for the fixture class): begins on arrival, is in `quests`, cannot be abandoned, survives a logout and a restart mid-case, `pledge` twice within a minute (the class, the journal, the event) and after a minute (starts over), `decline` twice (the door closes for good; the certificate), neither (the offer stays), `pledge` below level 5 refused, with an ordinary class refused, "withdrawn" when an ordinary class was taken while the summons was outstanding.
- **Leaks**: `EnabledChildren("mage", "advanced")` omits it; `ask` every hallmaster about the class's words answers as for an unknown word; `help NAME` as a non-holder answers as for an unknown word; `who`, `look at`, GMCP and the Mudlet package never carry the class; the Chronicle file after a full run has no new entry and the rumours file has none; `chronicle_test.py` proves `class.hidden` is routine; `SiteTests.cs` feeds a roster with a hidden class and the Heroes page shows no class; the filter file has `class.hidden` and not the hidden id.
- **Lint**: Check 17 reports each ERROR and WARN of section 10 in the leaky fixture and nothing in the real mudlib.
- **Changes to existing tests**: `class_test.py` gets the hidden flag and the `hidden` requirement kind; `sandbox_test.py` the register's probes.

## 14. Build order
Each stage can be merged and played; nothing is visible to players before stage 4.
1. **The engine, with no content.** `Hidden`, the `hidden` requirement kind, the register, the corpse roll with `SummonsOpen = false` (nothing is ever served), `Bound`, `GatedExit`, `/std/hiddengiver`, the event, the website whitelist and filter row, the lint check, the wizard command, the fixture class and the test file. The leak checks (section 13) pass on an empty set of real hidden classes.
2. **The Bureaucrat's content** (bureaucrat.md): class file, the rooms, the people, the summons, the quest, the trial orders and the spell files that exist at launch. Played by the owner through `hidden grant`; the roll still closed.
3. **The roll opens.** `SummonsOpen = true` for the Mage; the numbers of 5.2 as shipped; the owner watches the register for a week.
4. **The other four**, one at a time, content only, each with its own spec, yardstick results and lint.

## Out of scope / future
- Designing the other four classes (section 12 is a sketch).
- A second hidden class for one calling, or a hidden subclass or prestige class.
- Un-pledging, changing class, or a way to be summoned a second time.
- Making the roll depend on anything but the level window and the count (a region, a time of day, a rumour).
- A rumour system that hints at hidden classes: rumours are Chronicle-fed and the event is routine, so there is none; an NPC who speaks of "a clerk's seal" in the city is a later, owner-chosen decision.
- Creatures above level 10 (the roll's window needs them one day: `TopOrdinaryLevel`).
- Party and guild effects of a hidden class.
