# The Bureaucrat: the Mage's hidden class, found by jury duty (revised 27 November 2026)

Owner's idea (27 November 2026): "a funny class 'Bureaucrat', it would be a caster class and it would cast things like 'forms in triplicate' (which is basically a slow spell) or 'restraining order' (can't get within melee range, ranged weapons only)". The humour comes from clerks treating a goblin ambush as a filing matter, and nobody in the fiction thinks it is funny.

Owner's decisions of 27 November 2026, now folded into this spec: the Bureaucrat "should be a 'hidden' type of class without an OBVIOUS training/pledger", each path gets one, and this is the Mage's; the Chancery room is redesigned; it is found by "a 'jury duty summons' in loot ONCE", and "if they arrive for jury duty the quest should begin and end with them having the option of taking the b class"; "A bureaucrat probably isn't fighting the Overseer anyway", so the class starts in Oakenfell and the Overseer is no longer its yardstick. The owner accepts that this spec sits in a public repository for now.

This spec is the first use of [hidden-classes.md](hidden-classes.md), which holds the pattern: the register, the summons roll, the gated door, the shape of the quest, the leak fixes and the lint check. Read it first; this file does not repeat it, and says "the pattern" where it relies on it. It is built on [tags-and-saves.md](tags-and-saves.md): it needs the `melee` and `ranged` tags (Part A) and the saving-throw rule (Part B: "above the law" for bosses, "ignores your authority" for higher level). Every number is a starting value for `tests/balance_sim.py` and play-testing; nothing here has been run in the game. "Today" means the files in the repository on 27 November 2026.

**Nothing in this spec may appear in any public text** (hidden-classes.md section 2): not the class title, not its skills, not the quest name, not the Chancery's rooms and people, not the summons. This document is the one place they are named, and it is a spec.

## Decisions for the owner
Each has a recommendation and the alternative. The spec is written as if you accept every recommendation.

1. **The Bureaucrat is hidden, and found by jury duty.** A Mage of level 5 or more with no advanced class may find a "summons to attend as juror" once, in the loot of an ordinary creature. It sends them to City Hall's inner door; the quest "Jury Duty" ends with the offer of the class (sections 2 and 11). Recommendation: yes, as you described. Alternative: the first spec's open Registrar who offers a pledge for 25 gold (withdrawn: it contradicts "hidden").
2. **x for this class: nothing in the first 10 eligible loots; 2.0% on the 11th, rising 0.5 points a loot; certain on the 50th** (the pattern's numbers, hidden-classes.md section 5.2). Expected 24.8 eligible loots. **For a level 5 Mage, that is about 2.3 hours of hunting on average at 11 eligible kills an hour** (3.1 hours at 8 an hour, 1.7 at 15), never in the first 55 minutes, and certain by about 4.5 hours at 11 an hour. Eligible kills are ordinary creatures of level 4 to 7 for a level 5 character: eleven kinds exist (Grey Fox, Goblin Elite Guard, Forest Spider, Stray Wolf, Bandit Lookout, Wild Boar, Forest Wolf, Tunnel Rat, Goblin Raider, Boar Brute, Goblin Scout). Alternative: a flat 2% (a third of Mages would still be waiting after 50 kills).
3. **The class is Mage only, taken at level 5 or later, trained at the Chancery afterwards.** The first spec said "taken at level 5 at a pledge"; now any level from 5 up may take it, and a Mage who takes it at level 8 receives every skill whose level is at most 8 at once, at rank I. Adventurer is a later decision. Recommendation: yes. Alternative: Mage and Adventurer now (section 2.6).
4. **Nothing points to it from outside the Chancery.** No hallmaster, no clerk, no banner, no help page, no website line (the first spec's Ilsabet "fourth road" line, Mabyn's and Maude's topics and the Registrar's open pledge are all removed). The Hall of Legends files do not change at all. Recommendation: yes.
5. **The place is City Hall's inner door**, which exists today in the description and leads nowhere. It becomes a gated door that exists only for someone holding the summons (or the class). Behind it are two new rooms: **a waiting room** (the Chancery proper: ticket machine, benches, a clock that is wrong, the Registrar behind a counter) and **a hearing room** (the Clerk of the Court, the parties, the goose). Recommendation: yes. Alternative: a new building off Founder's Square (costs a district exit and gives the secret away on the map).
6. **The quest "Jury Duty" is a self-contained case, "Pruett v. Marrowby, the matter of the goose": 15 to 25 minutes, no combat, one comic interruption.** The player uses `ask`, `read`, `search`, hands in two exhibits and gives a yes or no verdict, and uses trial versions of three of the class's orders (restrain, desist, triplicate) on authority the court lends them. It is not abandonable and cannot fail. Recommendation: yes.
7. **It ends with a two-step choice either way, and no fee.** `pledge` twice within a minute takes the class; `decline` twice within a minute refuses it for ever ("The Chancery does not summon twice."). Both pay the same: 1 gold and 250 experience. A taker also receives the Notary's Seal (60 gold in the shop; a gift once); a decliner receives a keepsake Certificate of Jury Service. No Renown either way. Recommendation: yes. Alternative: a taker pays a fee (the first spec's 25 gold); it would make the secret cost money and the refusal free.
8. **The Overseer is not the yardstick.** The class is judged by its own pace and survival at levels 5 to 10 against its calling and the five callings, and by four set pieces (section 10). The three boss files' small summon-flag edits stay (they are needed for Red Tape to be correct, section 7.5) but nothing is tuned to the Overseer. Recommendation: yes (your words).
9. **Resources: Vitality 6 + 7 a level, Endurance 14 + 1 a level, Focus 76 + 4 a level**, replacing the Mage's (4 + 7, 14 + 1, 71 + 4). Alternative: name no numbers (the Mage's stand): five Focus fewer.
10. **No new weapon kind and never a ranged weapon.** Dagger and staff. The Clerk's Staff of Office (45 gold) and the Notary's Seal (60 gold) carry the fiction. Both are sold only to holders of the class (the Registrar's shop). Recommendation: this.
11. **Eleven spells: six at launch (levels 5 to 10), five later (12 to 20).** Launch: Forms in Triplicate, Restraining Order, Cease and Desist, Red Tape, Audit, Application Denied. Later: Per My Last Letter, Stamp of Approval, Take a Number, Summons, Due Process. A class gives a skill at a level (section 7.1).
12. **Restraining Order bars melee attacks against the caster only** (section 6.2); it lapses at once if the caster strikes in melee. **Audit gives a penalty on the next order's save, not a to-hit bonus** (section 6.5).
13. **Prices and ranks are the existing ladder** (II 40, III 150, IV 400, V 900 gold; rank r at level 5 x (r - 1)), all from the Registrar, who teaches only holders. A Bureaucrat still buys Arcane Bolt ranks from the Mage hallmaster. Alternative: rank unlocks (section 5).
14. **Ally spells (Stamp of Approval, Application Denied) work on any player in the room**; there is no party system.
15. **Small engine changes**, each a few lines and each listed in section 7: a skill at a level; the trainer teaches an advanced class; bars, restraints, wards, interrupts; a journal progress note; the court's `submit` verb. The shared pieces are in hidden-classes.md section 9.

## 1. What exists, what is promised, and what the world lacks
Read of docs/CLASS_SYSTEM.md (sections 1, 2, 6, 8, 9, 12, 13), docs/specs/skill-ranks.md and the files named below on 27 November 2026.

| Already there | Not built |
|---|---|
| A class is data: `/classes/<id>.cs` inheriting `/std/pclass`, `Tier = "advanced"`, `Parent`, `Require("level", "5")`, kinds `level`, `quest`, `item`, `class`, skills, traits, its own resource numbers, `ExpModifier`, `AttackRate`, `Guild`. `/secure/classes` finds a new file without a restart. | **No advanced class file exists**: `/classes` holds only the five callings. |
| `Adventurer.ApplyClass(id)`: only another object may call it; checks parent and requirements; refuses a second advanced class; tells "You are now a {title}."; saves the id. | A Chronicle report from `ApplyClass`, a `pledge` verb (the Hall's hallmasters say "the rite of pledging is not yet open"), a **hidden** flag, a skill **at a level**. The trainer answers by the archetype only. |
| The quest journal: states 0/1/2, tagged dialogue, `DefineQuest(..., abandonable)`; the objective lines `kill` and `have`. | No real quest uses `abandonable: no`; the journal has no per-stage text (section 7.7). |
| City Hall has an inner door described but not built: its only exit is `out`. `in`, `knock` and `open door` print "What?" today. Maude Tarrow and Barnaby are its clerks; Maude's topics are districts, founder, market, river, guild, southgate. | The Chancery. Everything in section 2.4 is new. |

**What the world lacks, plainly.**
- **No Oakenfell creatures.** The city has none (no monster file under `oakenfell/`): all 44 monster files are in Vincent's Hollow and the Corridor. A Mage who stays in the city never meets the summons; the first eligible kills are in the Hollow cellars (level 4) and the Corridor (`south_road`, `eldergreen_edge`, ...).
- **No creature above level 10, and the level-10 ones are all bosses.** The roll's window above level 9 has only the six level-9 kinds; the class's balance can be measured only on levels 5 to 9 ordinary creatures and the five level-10 bosses.
- **`give` refuses NPCs** (`CmdGive`: "does not take things from you. You can give things only to other players."), so the case's "giving an item" is a new verb on the Clerk, `submit`.
- **No weight, no calendar item.** The summons prints the game date from `/secure/chronicle` (`DateText()`: "the 17th Day of Harvest, Year 3").
- **No `knock` verb exists**; the gated door binds it (hidden-classes.md section 6).
- **The journal has one text per quest state**, not per stage; section 7.7 adds `ProgressNote`.

## 2. How it is found and where it sits
### 2.1 The class, in the Chancery's own words
- **Name** (`ClassName`, `Title`): "Clerk of the Chancery". **Id**: `bureaucrat`. **Tier**: `advanced`. **Parent**: `mage`. **Guild**: `chancery`. **Hidden**: true. **Experience modifier** 1.0. **Tagline**: "The Bureaucrat: nothing may happen until the correct form has been lodged." (never printed outside the class's own files and a holder's `help`).
- **Requirements:** `Require("level", "5")`, `Require("class", "mage")`, `Require("hidden", "bureaucrat")` (the quest's offer is open). **QuestId** `jury-duty`; **GateKey** `inner-door`; **SummonsItem** `/domains/greenvale/oakenfell/founders/obj/jury_summons`; **OwnPaths** `/domains/greenvale/oakenfell/founders/chancery`, `.../founders/npc/registrar_ashcombe`, `.../npc/clerk_crake`, `.../npc/pruett`, `.../npc/marrowby`, `.../npc/goose`, `.../founders/obj/jury_`, `.../obj/jurors_badge`, `.../obj/clerks_staff`, `.../obj/notarys_seal`, `/classes/bureaucrat`, `/skills/bureaucrat/` (the skill files go in their own folder so one prefix covers them). Roll fields: the pattern's defaults (10, 2.0, 0.5, 50).
- **Description** (the owner's text, kept word for word, for `help bureaucrat` once held and for the Registrar after):

> The Chancery of Oakenfell has never lost a war, because it has never agreed that one was properly declared. Its clerks carry no blade worth the name. They carry seals, ledgers and the absolute conviction that nothing may happen until the correct form has been lodged. Creatures that cannot read are bound all the same; ignorance of the procedure is no defence.

  The class file's `Description` adds the plain facts: "A Clerk of the Chancery is a Mage who has taken the Chancery's seal. Control, not damage: it slows, bars, interrupts and delays, and leans on Insight and Resolve. Weapons: dagger, staff. Armor: light."

### 2.2 The summons (verbatim, as `read summons` prints it)
`{serial}` is 4400 plus the register's serial (so the first summons ever served reads 4401); `{date}` is the game date; `{Name}` is the character's name. The text never says "class", "path", "seal", "Bureaucrat", "Clerk of the Chancery" or what the court does.

```
                      THE CHANCERY OF OAKENFELL
                    SUMMONS TO ATTEND AS JUROR
              Form 27-J (third copy: the bearer's copy)

  Reference:  CH/J/{serial}-B
  Issued:     {date}, being the date on which you read this
  To:         {Name}, a person of sufficient standing to be inconvenienced

  You are hereby summoned to attend the Chancery as a juror in a matter
  to be heard. Present this summons at the inner door of City Hall,
  Founder's Square, Oakenfell. Knock.

  You must attend within fourteen days of the date of issue. The date of
  issue is the date on which you read this. The Chancery does not regard
  this as circular.

  Bring this summons. Do not bring a weapon, an opinion or a friend. You
  will not be required to remember anything. You will be required to be
  present while it happens.

  Failure to attend renders you liable to a fine of THIRTY-SEVEN GOLD AND
  ONE THIRD OF A GOLD (37 1/3), together with one eighth of a gold for
  the paper this notice is written on. The Chancery will write to you
  about the fine. It has never been known to finish the letter.

  Jurors are paid. The rate is one gold. It is entered.

                                   For the Chancery, M. Ashcombe, Registrar.
                                   Stamped in red. NOTHING IS DONE UNTIL IT
                                   IS ENTERED.
```

- **When found** (`SummonsFound`, to the taker only; the room sees nothing): "Among the coins and oddments you take from {the corpse}, one thing is not like the others: a stiff buff-coloured paper, folded in three, with your name on it in a clerk's hand. You do not remember picking it up. It is already in your hand." (The corpse's own label fills the name.)
- **`look at summons`, `inventory`:** "a summons" / "A stiff buff-coloured paper, folded in three and stamped in red. It has your name on it. You could {cmd}read{/} it."
- **The pack:** it is `Bound` and `QuestItem` (hidden-classes.md 5.6): "It is addressed to you. It is not a thing one drops."
- **At the end** the Registrar retains it: a taker's is "entered", a decliner's "kept by the court" (section 11.6); in both cases the item is removed by the giver and the register stage is 9.

### 2.3 City Hall's inner door
The pattern's `GatedExit("in", ".../founders/chancery", "inner-door", "in", "enter", "knock", "open")` goes in `city_hall.cs`. Nothing else in that file changes: not the long description (it already says "above the inner door a line of carved letters has been picked out in old gilt"), not the `AddDetail` and `AddReadable` lines, not the spawns, not the `out` exit.

**Without a summons (and for everyone who has declined or been released): today's exact text, kept.**
- `look at door` (also `inner door`, `letters`, `lintel`, `motto`, `gilt`): "Carved over the inner door, with the gilt rubbed off the high points of each letter, are the words of the city's motto. You could {cmd}read{/} them."
- `read letters` (also `lintel`, `motto`, `inscription`): "Carved over the inner door:" then, on the next line, "FROM STRONG ROOTS GROW GREAT THINGS" in double quotes.
- `in`, `enter`, `go in`, `knock`, `open door`, `knock on door`: "What?" (the unknown-command answer; byte for byte what they print today). The exits line shows only `out`. `search` finds nothing.

**With the summons outstanding (stage 1 or later, not finished):**
- `look at door`: today's text, then on a new line: "The door is plain oak under the gilt, with a brass knocker in the shape of a rubber stamp. It looks as though it has been expecting someone with paper."
- The exits line also shows `in`.
- `knock` (also `in`, `enter`, `open door`), first time (stage 1): "You knock on the inner door, under the gilt letters. A voice on the other side says, without hurry, 'Reference?' You hold up the summons. The door opens, politely, as though it had been about to." and you are moved in. Room (City Hall): "{Name} knocks on the inner door and goes through it. It closes behind {them}." Room (waiting room): "{Name} comes in from City Hall, holding a summons the way people hold a ticket."
- Again (stage 2 to 4): "You knock. 'Come in,' says the voice. 'You are expected. You are always expected. It is the Chancery's only courtesy.'"
- In a fight: the usual refusal. A watcher in City Hall learns that someone went through the inner door; nothing more. That is accepted (the pattern, section 6).

**With the class (stage 9, outcome taken):** `knock` or `in`: "The inner door opens before you knock. It has your file." The summons text and the Registrar's file are the only records.

### 2.4 The rooms (both `Safe`, area Oakenfell, region Greenvale; new files in `mudlib/domains/greenvale/oakenfell/founders/`)
**The Chancery: Waiting Room** (`chancery.cs`). Exits: `out` (City Hall) and `east` (the court; locked with `LockExit("east", "jurors badge", ...)`, the same mechanism as the north road's travel pass; the badge item's `Ids` include "jurors badge" and "badge"; refusal below).

> **The Chancery: Waiting Room.** A long, low room with a floor of worn green tile and a ceiling that has been repainted so often it has lost its corners. Wooden benches, each bolted to the floor facing a different wall, run down one side. At the far end a brass counter with a grille divides the room in two, and behind it, floor to ceiling, climb pigeonholes whose brass numbers do not run in order; a chained lectern beside the counter holds a book as large as a door. A green iron machine by the entrance wears a small sign, and above the counter a clock tells the wrong time with great confidence. A door in the east wall has two cards pinned to it. The air smells of ink, hot wax and old tea, and the only sound is a pen.

Details and readables (the exact words):
- *ticket machine, machine, crank, slot, ticket* : "A green iron machine with a crank, a brass slot and a glass window. The window says NOW SERVING: 3. A plate beneath it says: THE WINDOW DOES NOT CHANGE. A sign says: TAKE A NUMBER." `take number` (also `turn crank`, `pull ticket`, `take ticket`; room verbs): "You turn the crank. The machine clanks, thinks about it, and drops a paper ticket into your hand: 'No. {serial}.'" Room: "{Name} turns the crank on the ticket machine." One ticket per juror (a second: "The machine has already issued you a number. It does not do refunds."). `read ticket`: "No. {serial}. Please hold this ticket until you are called, and for some time afterwards." The ticket is a harmless item that vanishes when the quest ends or the character leaves the Chancery for City Hall.
- *benches, bench, seats* : "Eleven benches, bolted down facing eleven different walls. They are empty. A faded paper sign says TAKE A SEAT, and someone has added in another hand: NOT THAT ONE. You cannot tell which one."
- *clock, wrong clock* : "A plain wooden clock with its hands at twenty past nine. It has been at twenty past nine for as long as anyone can say. A card beneath it reads: THE CLOCK IS CORRECT. THE TIME IS WRONG." (`read card`: the same.)
- *counter, grille, brass counter* : "A brass counter with a grille. The brass hatch for passing papers is the only part of it that is ever opened."
- *pigeonholes, pigeonhole, rolls, holes* : "Hundreds of pigeonholes, each with a rolled paper and a brass number. The numbers do not run in order, and the order they do run in has never been explained. A neat card gives the filing rule: BOUNDARIES UNDER WEATHER. WEATHER UNDER ADJOURNMENTS." (`search pigeonholes`: section 11.4.)
- *book, lectern, book of proceedings* readable: "Entries in a dozen hands. The latest, dated this morning: 'Goblins, Vincent's Hollow: boundary dispute, referred.'" For a juror (stage 2 to 4) a further line: "Below it, in a fresh hand: 'Pruett v. Marrowby: the matter of the goose. Set down for hearing, a jury having been found.'"
- *card, cards, east door, door* : "Two cards on the east door. The first: JURORS ONLY. The second, underneath: ALL OTHERS TAKE A NUMBER." The east exit's refusal (no badge): "The east door is shut. The Registrar says, without looking up, 'Jurors are sworn first. It is not a complicated rule. It is merely a rule.'" (If the Registrar is not present the card text only.)
- *plate, brass plate, motto* readable: "NOTHING IS DONE UNTIL IT IS ENTERED. Below it, smaller: Fees are posted. The clerks are not to be hurried."
- *lamps* : "Green-shaded lamps on short brass arms. They burn all day. The Chancery has a view on daylight and has not yet shared it."

**The Chancery: Court of First Instance** (`chancery_court.cs`). Exit: `west` only.

> **The Chancery: Court of First Instance.** A small, high court with a raised bench of black oak, a clerk's desk below it, a witness stand with a rail worn pale by hands, and two long tables facing each other across a strip of green carpet. A box of twelve chairs stands to one side; one of them has a cushion. A tall window of painted glass shows a sky that has been the same since the room was built. A brass plate on the bench reads THE COURT IS ALWAYS SITTING. There is straw on the floor, and a smell of goose.

Details: *bench* ("The Clerk of the Court's bench. A brass plate reads THE COURT IS ALWAYS SITTING. Beneath it, in pencil: whether at you is another matter."), *desk, clerk's desk* ("Piled with exhibit tags, a stamp pad and a bell that says 'ring for the clerk'. The clerk is two feet away."), *witness stand, stand, rail* ("A rail worn pale by hands. Everyone who has stood here has held on."), *tables, table* (the search target; section 11.4: `search table`), *chairs, box, jury box, cushion* ("Twelve chairs. Eleven are for the other jurors, who have not come. The twelfth has a cushion, and is yours. It is a very good cushion. It has been sat on by an honest person, once."), *window, glass* ("Painted glass: a sky, a hill, a small scale held by a hand. The scale is very slightly out."), *straw* ("It is straw. It is not decorative.").

### 2.5 Mage only, level 5 or later
`Parent = "mage"`. `ApplyClass` already demands the Mage. The offer needs level 5 (the class file's requirement), so nobody under 5 sees the offer even if they were summoned (the roll's level floor is 5, so none is). A character who levels nothing after the summons is unaffected: the quest has no level gate beyond 5. The resource numbers replace the Mage's (section 3). Everything a Mage has is kept (the pattern, 8.3).

### 2.6 Opening it to Adventurers (a later decision)
Change `Parent` to none, add a `Require("calling", "mage,adventurer")` via `CheckRequirement` in the class file, make the pattern's `RollOnLoot` read a list of parent callings (`SummonsCallings`), and give the Adventurer a small trait for the Mage's Focus Efficiency (spells 20% less). Their Vitality falls from 10 + 8 a level to 6 + 7. Not in this spec.
## 3. Resources and attributes (docs/ATTRIBUTES.md sections 2, 8 and 9)
An advanced class that names its own numbers **replaces** the archetype's (`Adventurer.RefreshClasses`: "the archetype's, then any later class that names its own"). The Bureaucrat names all three:

| | Base | Per level | Formula |
|---|---|---|---|
| Vitality | 6 | 7 | 6 + 7 x Level + Vigor x 2 |
| Endurance | 14 | 1 | 14 + 1 x Level + Resolve x 2 + Vigor |
| Focus | 76 | 4 | 76 + 4 x Level + Insight x 2 + Spirit |

(the Mage: 4 and 7, 14 and 1, 71 and 4: two more Vitality and five more Focus). **Attack rate** 0.96 (the Mage's; the class names none so the Mage's stands). Reaction Time 2.5 base (the archetype). **No class modifier.** A Bureaucrat keeps every Mage skill and trait (Arcane Bolt, Magical Lore, Glow, Focus Efficiency).

**Attributes.** The class changes no starting attribute (a character keeps the Mage's: Might 5, Grace 5, Vigor 5, Insight 9, Spirit 7, Resolve 5) and gets two points a level to place, as everyone. The class **leans on Insight, then Resolve**: Insight is the size of the well, the spell power, and the Authority that lowers a target's save (tags-and-saves.md section 12.3); Resolve fills Endurance (almost unused) and, with Spirit, is Mental Resistance, which the class's own mind-effect enemies will test. A recommended placing (a pointer for the class description, not a rule): five of the first eight points in Insight and three in Resolve; at level 10, eleven in Insight and seven in Resolve. Derived numbers for that character:

| | Level 5 (8 points: Insight 14, Resolve 8) | Level 10 (18 points: Insight 20, Resolve 12) |
|---|---|---|
| Maximum Vitality (Vigor 5) | 6 + 35 + 10 = **51** (Mage 49) | 6 + 70 + 10 = **86** (Mage 84) |
| Maximum Endurance | 14 + 5 + 16 + 5 = 40 | 14 + 10 + 24 + 5 = 53 |
| Maximum Focus | 76 + 20 + 28 + 7 = **131** (Mage 126) | 76 + 40 + 40 + 7 = **163** (Mage 158) |
| Focus regained per 10 s (Maximum x (1% + (Insight + Spirit) x 0.15%)) | 131 x 4.15% = 5.4 | 163 x 5.05% = 8.2 |
| Spell Power (+2% per Insight above 5, +1% per Spirit above 5) | +20% | +32% |
| **Authority** (better of Insight, Resolve above 5; +4 a rank above I) | 9 (rank I) | 15 + 8 = **23** (rank III) |
| Mental Resistance | 2% x (2 + 3) = 10% | 2% x (2 + 7) = 18% |
| Physical Resistance | 2% x (0 + 3) = 6% | 2% x (0 + 7) = 14% |

A Mage's Focus Efficiency applies to every Bureaucrat spell (they are `Kind = "spell"`): the costs below are rank I **before** it; the Bureaucrat (a Mage) pays 20% less (Forms in Triplicate 24 becomes 19).

## 4. Armour, weapons and the fiction of the staff
- **Armour:** light only (the Mage's `ArmorTypes`); no shield. **Weapons:** dagger and staff, as a Mage; the class adds none. **A Bureaucrat can never use a ranged weapon** (a Mage's weapon access has no bow): the degenerate loop of "restrain a melee creature and shoot it" is closed at the door (section 8). The class's own damage is a spell.
- **Tags (tags-and-saves.md section 2):** every item below states its reach, damage and kind.
- **The Clerk's Staff of Office** (`/domains/greenvale/oakenfell/founders/obj/clerks_staff.cs`; sold by the Registrar to holders of the class for 45 gold): a black-ash staff capped with a brass seal-head. `Category = "staff"`; tags `staff`, `blunt`, `melee`, `wood`; Damage 5-9, to hit 0, Reaction modifier 0.5, Recovery 1.0 (the Apprentice's Staff is 7-11, +1; the Clerk's is lighter in the hand and weaker, on purpose); `AttributeBonus["resolve"] = 1`; `WornBy.Add("bureaucrat")`. Description: "A staff of black ash, shod in brass, its head a flat seal-die. It is better for pressing wax than for hitting, and the Chancery does not recommend using it for anything else." (`Magic`: "the seal-head carries the Chancery's mark: +1 Resolve while wielded".)
- **The Notary's Seal** (`.../founders/obj/notarys_seal.cs`; 60 gold from the Registrar to holders; the first is a gift at the end of Jury Duty, section 11.6): a trinket on a cord; tags `trinket`, `steel`; `StatBonus["authority"] = 2`. "A brass seal-die with a plain wooden grip. What it presses becomes official, and a good deal of what it presses becomes binding."
- **No quill, no ledger as an item with a skill.** The ledger and the quill are in the words (the wind-up lines of the spells), not in an item slot. A "held focus" slot does not exist and the spec adds none.

## 5. The spells in one table
Prices for every rank: **II 40, III 150, IV 400, V 900 gold**, all from the Registrar, who teaches only holders (`TeachCalling("bureaucrat", 2, 5)` in `/std/hiddengiver`; `RankDaemon.RankPrices`); rank r needs level 5 x (r - 1) (rank II at 5, III at 10, IV at 15, V at 20); a spell starts at rank I when it is learned; the command word never changes. A spell is learned **when the character reaches its level** (section 7), or all at once at rank I if the class is taken later than its level. No unlock beyond level and gold at launch; candidates if the owner wants one: Forms in Triplicate II asks "kills goblin 10" (as Arcane Bolt II does), Cease and Desist III asks "kills_with desist 10", Application Denied II asks the quest "the-quiet-offering".

| Level | Spell | Command | Focus (I) | Prep / Recovery | What it does | Save |
|---|---|---|---|---|---|---|
| **5** | Forms in Triplicate | `triplicate` | 24 | 0.5 / 1.0 | slow: Reaction x1.5 for 20 s | Physical |
| **6** | Restraining Order | `restrain` | 26 | 0.5 / 1.0 | cannot melee the caster, 10 s | Mental |
| **7** | Cease and Desist | `desist` | 28 | 0 / 1.5 | interrupts what it is preparing; bars that action 10 s | Mental |
| **8** | Red Tape | `tape` | 30 | 0.5 / 1.0 | cannot call for help or leave, 20 s | Physical |
| **9** | Audit | `audit` | 20 | 0.5 / 1.0 | reveals the numbers; the next order is served at a penalty; out of combat +3 to `search` | none |
| **10** | Application Denied | `deny` | 32 | 0.5 / 1.0 | ward: the next attack on you is refused | none (a friend's gift) |
| 12 | Per My Last Letter | `letter` | 10 | 0.5 / 1.0 | small damage, +1 each cast on the same target | none (dodgeable, as the bolt) |
| 14 | Stamp of Approval | `stamp` | 20 | 0.5 / 1.0 | an ally's next skill costs half | none |
| 16 | Take a Number | `number` | 36 | 1.0 / 1.5 | creatures take turns to attack, 20 s | Mental |
| 18 | Summons | `summons` | 40 | 1.0 / 1.5 | a creature from the next room comes to you | Mental |
| 20 | Due Process | `process` | 60 | 1.0 / 2.0 | the target cannot act until struck three times | Physical |

(Preparation and Recovery are in time units of 2 seconds. The delay of a cast is (Reaction Time + reaction modifier + Recovery) x 2 seconds: a Mage with a staff, Reaction 3.0, casts a spell with Recovery 1.0 every (3.0 + 1.0) x 2 = **8 seconds**; Cease and Desist (Recovery 1.5) every 9.) The save column is the kind in tags-and-saves.md section 12.1. Every command word was checked against `mudlib/std`, `skills`, `secure` and `domains`: none is in use.

**Why these levels.** Five to ten is one a level, because levels 1 to 10 are what exists. Nothing is learned between 10 and 12 or after that faster than every two levels; the later five are listed so the spec is complete and are not built at launch. Section 12.1 of tags-and-saves.md says no control effect is more than a bolt's worth of help to a lone caster; the later spells are the ones that need a group (Stamp, Take a Number) or a map (Summons) or a capstone (Due Process).

### 5.1 Common to every control spell
- The spell is refused before any Focus is spent if the target is not an enemy in the room that may be fought (`CanFight`: a safe room, a peaceful creature).
- The target's save, its strength, "half" and the lines the caster reads are tags-and-saves.md sections 12.2 to 12.5, exactly. A line below named "Full" is what is printed when the effect lands in full; for Half and None, the caster reads the extra line of that table's right-hand column, with the spell's noun (form, order, notice, tape, summons, hearing) in `{effect}`. **A boss reads "{X} is above the law. The {effect} half holds." when Half and "{X} is above the law, and knows it. Nothing holds." when None; a target above the caster's level reads "{X} ignores your authority. The {effect} holds for half its term." when Half.** Half is half the time, rounded up.
- The repeat rule (one more step off per use of the same spell on the same target within 60 seconds, two at most) and the flood guard (four control effects of any kind in 60 seconds) apply to every control spell. The effect id of each is the spell's id (`triplicate`, `restrain`, `desist`, `tape`, `number`, `summons`, `process`), so two different spells do not count against each other, but the flood guard counts all.
- Authority: the caster's Authority is read at rank (tags-and-saves.md section 12.3).
- An effect on a creature is not saved and ends when the creature dies or the fight ends (the creature forgets; its record in the daemon is pruned).

## 6. The spells, one by one

### 6.1 Forms in Triplicate (`triplicate`), level 5
*Given with the class (a character takes it at level 5 or later, so always).* Wind-up (the room reads): "{Name} draws three sheets of paper and a carbon from a satchel..."

| Rank (level) | I (5) | II (5) | III (10) | IV (15) | V (20) |
|---|---|---|---|---|---|
| Focus (before Focus Efficiency) | 24 | 28 | 31 | 35 | 38 |
| Reaction Time of the target multiplied by | 1.5 | 1.5 | 1.6 | 1.6 | 1.7 |
| Full duration | 20 s | 25 s | 30 s | 35 s | 40 s |
| Half duration | 10 s | 13 s | 15 s | 18 s | 20 s |

- **Effect:** the target's Reaction Time is multiplied (`AddEffect("slow-forms", "Forms in Triplicate", seconds, "", factor, 0)`); a creature of Reaction 2.5 and a Recovery of 0.5 acts every (2.5 + 0.5) x 2 = 6.0 s, and every (3.75 + 0.5) x 2 = 8.5 s under rank I (+42%). The Overseer's Axe Slash (Reaction 4.0, Recovery 1.0) goes from every 10.0 s to every 14.0 s (+40%); at Half he is slowed for 10 s. All hostile slows on one creature multiply and the product is capped at x2.0 (tags-and-saves.md section 13).
- **Save:** Physical. Ordinary and above-level targets: Full or Half. A boss: Half or None. (Table of chances: tags-and-saves.md section 12.4.)
- **Messages.** You, Full: "You serve {X} with Form 27-B, in triplicate. It begins, slowly, to look for the second copy." Room: "{Name} serves {X} with a form, in triplicate. {X} slows, studying it." A creature that is a player (none today) reads nothing: creatures do not read. When it ends nothing is printed (`TimedEffect` has no expiry line; none is added).
- **Counterplay:** a high Physical Resistance (Vigor and Resolve); being a boss or an elite; a creature that is already slowed to the x2.0 cap; the repeat rule (the third in a minute is nothing); a Boss's Half; killing the caster.

### 6.2 Restraining Order (`restrain`), level 6
Wind-up: "{Name} uncaps an inkwell and begins to write..."

| Rank (level) | I (5) | II (5) | III (10) | IV (15) | V (20) |
|---|---|---|---|---|---|
| Focus | 26 | 30 | 34 | 38 | 42 |
| Full duration | 10 s | 12 s | 14 s | 16 s | 18 s |
| Half duration | 5 s | 6 s | 7 s | 8 s | 9 s |

- **Effect, exactly.** For the duration the target **cannot make a melee attack against the caster** (the one who served it). A melee attack is an **action of kind "attack" whose reach tag is `melee`** (tags-and-saves.md section 7) made against the caster. Not barred: a ranged or thrown action (a Webbed Spit, a Thorn Barrage), any action that is not an attack (a Battle Cry, a Summon Goblin Minion, a ward), and every attack on **anyone else**. The order is one caster's: another character's fight with the same creature is unaffected. **It lapses at once** if the caster makes any attack action of reach `melee` against the target (a staff blow, Shield-Bash-like skills, a dagger); the caster's spells (Arcane Bolt, Per My Last Letter, every Bureaucrat spell) never lapse it; neither does anyone else's attack. It also ends when the target or the caster dies, or leaves the room.
- **What a restrained creature does instead** (the engine's rule, section 7.3). Each time its engine chooses an action that is a melee attack on the caster:
  1. If the creature has a **declared ranged action** (reach `ranged` or `thrown`) that is not barred, it uses that, against the caster (the Warren Broodmother's Webbed Spit; the Thorn Woodling's Thorn Barrage). Nothing is "stopped" for it.
  2. Else if another enemy of the creature is in the room (another player, an ally), it **attacks that one** with the action it chose.
  3. Else it **waits**: its next action is put off by one Reaction Time (its own, slowed or not), and, at most once in six seconds, the room reads: "{X} takes a step toward {Name}, stops, and reads the order again." It does not flee (no creature in the game has a flee rule: checked in `Monster`, `Npc` and `Living`); if one is ever given one, a restrained creature under a quarter of its Vitality may use it. It does not leave the fight.
- **Save:** Mental (the creature's Spirit and Resolve). Ordinary and above-level: Full or Half; boss: Half or None.
- **Messages.** You, Full: "You serve {X} with a restraining order. It may not come within reach of you." Room: "{Name} serves {X} with a restraining order." Lapse: "Your order against {X} lapses: you struck it in person. A petitioner may not seek protection and strike in the same breath." Room: "{Name}'s order against {X} lapses."
- **Counterplay:** a creature with a ranged action; a second enemy (the order shields the caster, not the room); the lapse; being a boss; the repeat rule (a Full order, then a Half, then nothing for a minute); a creature that makes its Mental save.

### 6.3 Cease and Desist (`desist`), level 7
Prep 0 (it is the one spell with no wind-up: the interrupt must be timed), Recovery 1.5. Wind-up: none.

| Rank (level) | I (5) | II (5) | III (10) | IV (15) | V (20) |
|---|---|---|---|---|---|
| Focus | 28 | 32 | 36 | 41 | 45 |
| Bar on that action, Full | 10 s | 12 s | 14 s | 16 s | 18 s |
| Bar on that action, Half | 5 s | 6 s | 7 s | 8 s | 9 s |
| Ready Time pushed back, Full / Half | 2 s / 1 s | 2 / 1 | 2 / 1 | 2 / 1 | 2 / 1 |

- **Effect.** If the target is in the **Preparation** of an action at the moment the spell resolves: that action is cancelled (as a stagger's cancel: what the creature has spent is lost; half of the Recovery is still paid), **that action is barred**, and the creature's Ready Time is pushed back as above. The **bar** names the action by id (the creature's `ChooseAction` is asked again and, if it returns a barred action, the engine takes the creature's first declared action that is not barred, else its basic attack, else waits). If the target is **not** preparing: it is refused at the command, before any Focus is spent ("{X} is doing nothing that has not already been filed."), unless the creature's last action (the one it finished within the last 6 seconds) was a special (not its first declared action), in which case that action is barred (and nothing is cancelled): "You serve {X} with a notice to cease and desist from its {Action}."
- **Save:** Mental. **Full** = cancel, push back 2 s and bar 10 s; **Half** = the cancel still happens (it is instantaneous; there is nothing of it to halve), push back 1 s, bar 5 s; **None** = nothing at all, not even the cancel. A boss: Half 95%, None 5% (at the caster's level), so the cancel of the Overseer's Power Strike lands nineteen times in twenty.
- **Messages.** You, Full: "You serve {X} with a notice to cease and desist. Its {Action} stops mid-motion." Room: "{Name} serves {X} with a notice to cease and desist. {X}'s {Action} stops mid-motion." Overseer, Half: the boss line of 5.1 after it.
- **Counterplay:** actions with no Preparation cannot be cancelled (every Slash and Bite); a creature that cycles to another action; a timing miss (the Overseer's Power Strike is announced two seconds ahead; a caster who is mid-Recovery misses it: the cast is queued and goes when the caster is ready); the bar is on one action only.

### 6.4 Red Tape (`tape`), level 8
Wind-up: "{Name} produces a length of red ribbon and a thick stack of forms..."

| Rank (level) | I (5) | II (5) | III (10) | IV (15) | V (20) |
|---|---|---|---|---|---|
| Focus | 30 | 35 | 39 | 44 | 48 |
| Full duration | 20 s | 24 s | 28 s | 32 s | 36 s |
| Half duration | 10 s | 12 s | 14 s | 16 s | 18 s |

- **Effect.** While it lasts the target **cannot use an action of kind "summon"** (a call for help: the Overseer's Summon Goblin Minion, the Bandit Leader's Call the Pack, the Warren Broodmother's Call the Brood), **cannot leave the room by its own will, and cannot flee** (none of the corridor's creatures moves or flees in a fight today, so this is the lesser half). It **delays**: a creature whose pattern says "summon now" (the Overseer's rule: once, when his Vitality first falls to half) is simply asked for another action; its summon is **not spent** (the Overseer's `summoned` flag is set only when the summon really happens: section 7.5) and comes at his first action after the tape ends, if he is still at or below half.
- **Save:** Physical (a bind). Ordinary: Full or Half. Boss: Half or None: the Overseer's summon is delayed 10 s (Half) or not at all.
- **Messages.** You, Full: "You wind {X} in red tape. It is not going anywhere, and it is not calling anyone." Room: "{Name} winds {X} in red tape." When it tries (rate-limited as the order's): "{X} draws breath to call for help, and finds the request has not been countersigned." The Overseer, in his own words: "The Goblin Overseer draws breath to summon his minions, and is informed that a summons requires a second signature."
- **Counterplay:** creatures that never summon (it does nothing to a rat); the Half; the end of the tape.

### 6.5 Audit (`audit`), level 9
Wind-up: "{Name} opens a ledger and licks a pencil..." Target: an enemy in the room (in a fight or not; refused on a peaceful creature or a person).

| Rank (level) | I (5) | II (5) | III (10) | IV (15) | V (20) |
|---|---|---|---|---|---|
| Focus | 20 | 23 | 26 | 29 | 32 |
| The next order served on it: its save chance is lowered by | 6 | 8 | 10 | 12 | 14 |
| ...for | 30 s | 30 s | 30 s | 30 s | 30 s |
| Out of combat: +to `search`, for 120 s | +3 | +3 | +4 | +4 | +5 |

- **In a fight or facing a creature.** It reveals the creature's numbers, and marks it: **the first control spell the caster serves on that creature in the next 30 seconds is served at an Authority of +N** (the table's N: its save chance is lowered by N points, within the 5% floor). The mark is consumed by the first control spell, hit or not; one Audit marks one creature. It is not a control effect: no save, and the creature does not know.
- **What it reveals** (all read-only functions of the creature; a new `QueryActionLines()` on `Living` gives the actions with their timing, the one that Read Fate of the Oracle will also use):

```
You open your ledger on the Goblin Overseer and make an entry.
  Goblin Overseer: level 5, above the law (a boss).
  Vitality 114 of 114   Armor 3   Defense 4   Attack 7   Damage 4-7
  Resists: mind 4%, body 4%.
  Acts every 10.0 s: Axe Slash; Power Strike every 12.0 s (winds up 2.0 s); Battle Cry; Summon Goblin Minion.
  Your next order against it is served at -6 to its save.
```
  Rank-I tiers shown: the rank (boss: "above the law"; elite: "hard to serve"; above your level: "ignores your authority"; otherwise nothing), the numbers, the resistances, the actions. No tag is named beyond those words (the nature is not told to the player yet: tags-and-saves.md Out of scope).
- **Out of combat** (the target is not a creature, or none is named): "You go through the room with a ledger and a very sharp pencil. It is dull work, and you notice things." `AddEffect("audit", "Audited", 120, "search=3", 1.0, 0)`: a timed effect whose modifier `search` is already read by `QuerySearchBonus` (Keen Eye's key). No fight; refused in a fight with "Not now: the room is being audited by a goblin." (the fight is the audit).
- **Messages.** Room, in combat: "{Name} opens a ledger on {X} and makes an entry."
- **Counterplay:** none needed: it harms nobody. A target that dies first wastes it; the 30 seconds; one mark at a time.

### 6.6 Application Denied (`deny`), level 10
Wind-up: "{Name} lifts a rubber stamp..." Target: the caster or a **friend** in the room (the `friend` target of Blessing: a player character).

| Rank (level) | I (5) | II (5) | III (10) | IV (15) | V (20) |
|---|---|---|---|---|---|
| Focus | 32 | 37 | 42 | 46 | 51 |
| Attacks refused | 1 | 1 | 2 | 2 | 3 |
| Lasts up to | 20 s | 24 s | 28 s | 32 s | 36 s |

- **Effect.** A ward on the bearer: the next N attacks made **against** them are **refused**. The check is before the dodge and the hit roll in `Living.Attack` (a refused blow does no damage, rolls no critical, and cannot be dodged, blocked or missed, because it never happens); an attack is an **action of kind "attack"** (melee or ranged; the Overseer's Power Strike is one). It does not stop: damage that is not an attack roll (a spell that needs no roll), a creature's special actions, damage over time, a fall. A refused attack uses one charge; so does one that would have missed. At the end of the duration the ward lapses with its charges unused. Recasting replaces the ward. It never stacks. The effect is not saved.
- **Save:** none. It is a gift; the friend does not roll against it (tags-and-saves.md section 12.7).
- **Messages.** You, on yourself: "You stamp the air before you: APPLICATION DENIED. The next blow aimed at you will be refused." On a friend: "You stamp the air before {Friend}: APPLICATION DENIED." The friend reads: "{Name} stamps the air in front of you. A red DENIED hangs there for a moment." Room: "{Name} stamps the air before {Friend}." When it works, the one who attacked reads "{Bearer}'s application is denied: your attack is returned unopened." The bearer reads: "{Attacker}'s attack on you is denied." The room: "{Attacker}'s attack on {Bearer} is returned unopened."
- **Counterplay:** several attackers (one charge each); a creature's attack that is not an attack action; waiting out the 20 seconds; a second charge needs rank III.

### 6.7 Per My Last Letter (`letter`), level 12 (later)
Wind-up: "{Name} clears {their} throat and begins to dictate..." (the wind-up phrase names no pronoun: "begins to dictate a letter...")

| Rank (level) | I (5) | II (5) | III (10) | IV (15) | V (20) |
|---|---|---|---|---|---|
| Focus | 10 | 12 | 13 | 15 | 16 |
| Damage, before stacks | 2-3 | 3-4 | 3-5 | 4-6 | 4-7 |
| Added by each earlier letter on the same target within 30 s (at most 6 earlier) | +1 | +1 | +1 | +1 | +1 |

- **Effect.** Damage that ignores armour, needs no hit roll and can be dodged, exactly as Arcane Bolt resolves (`target.RollDodge()`, then `ApplyDamage`, `ScaleSpellDamage` for Spell Power). Each letter cast on the **same target** within 30 seconds of the last one adds +1 to both the minimum and maximum of the next, up to +6; the count resets after 30 seconds without one or when the target dies. The count is kept in `/secure/control` (a "stack" helper; skills hold no state).
- **Numbers.** Rank I letters: 2-3, 3-4, 4-5, 5-6, 6-7, 7-8, 8-9: average 2.5 then 3.5 ... 8.5, **seven casts to match an Arcane Bolt's 7.5** (a bolt at rank I is 6-9 for 20 Focus, 16 with Focus Efficiency). Against a creature with 57 Vitality: seven letters cost 70 Focus (56 with Focus Efficiency) and do 42; seven bolts cost 112 and do 52. The letter is the better use of Focus and the worse use of time (seven casts of 8 seconds) and its first four casts are weaker than a bolt. That is "weak direct damage" and "growing with each cast on the same target".
- **Save:** none (a damage spell). **Messages.** You, first: "Per your records, {X} takes {n} damage." Second: "As per my last letter, {X} takes {n} damage." Third: "As per my last two letters, {X} takes {n} damage." Fourth and after: "As per my last {k} letters, {X} takes {n} damage. The Chancery regrets the tone." Room: "{Name}'s letter reaches {X}." (and the same counts without the damage figure). A dodge: "{X} dodges your letter." as the bolt's.
- **Counterplay:** a creature that dodges; changing targets (the stack is per target); killing the caster before the sixth letter.

### 6.8 Stamp of Approval (`stamp`), level 14 (later)
Target: another **player character in the room** (not the caster; a party system later changes nothing). Wind-up: "{Name} reaches for a stamp and an ink pad..."

| Rank (level) | I | II | III | IV | V |
|---|---|---|---|---|---|
| Focus | 20 | 23 | 26 | 29 | 32 |
| The ally's next skill costs | 50% | 50% | 60% | 60% | 70% less |
| Lasts up to | 30 s | 30 s | 30 s | 30 s | 30 s |

- **Effect.** A timed effect (`stamp`) on the ally: the next **skill or spell** they begin (anything that pays Endurance or Focus; not a potion, not a basic attack) costs the percentage less in each resource, rounded up in their favour, and the effect is spent. The discount applies where the cost is paid (`Living.Begin` reads the effect; the cost shown at queue time is the discounted one). It does not stack; it does not apply to the Bureaucrat's own next spell (the spell refuses the caster).
- **Save:** none. **Messages.** You: "You stamp {Friend}'s next undertaking APPROVED." The friend: "{Name} stamps your next undertaking APPROVED. It will cost you half as much." When spent, to the friend: "The stamp is spent: {Skill} cost you only {n}." Room: "{Name} stamps {Friend} APPROVED."
- **Counterplay:** none (a friendly spell); a friend who waits out the 30 seconds loses it.

### 6.9 Take a Number (`number`), level 16 (later)
Prep 1.0, Recovery 1.5. Wind-up: "{Name} hangs a brass ticket-machine on the nearest wall..."

| Rank (level) | I | II | III | IV | V |
|---|---|---|---|---|---|
| Focus | 36 | 41 | 47 | 52 | 58 |
| Full duration | 20 s | 24 s | 28 s | 32 s | 36 s |
| Half duration | 10 s | 12 s | 14 s | 16 s | 18 s |

- **Effect.** Every creature in the room that is fighting the caster rolls its **own** Mental save. A creature that fails (Full) or makes it (Half) is **queued** for the strength's duration: of the creatures in the room that are queued, **one at a time** may begin an action; the others wait one Reaction Time and ask again. The turn passes to the next in the order they were drawn into the fight when the one that has the turn finishes an action (any action, attack or not). A creature that has no standing in the queue (None, a boss that makes its save) acts as it always did. Without a party system the queue limits what the creatures do to **whoever** they fight: it protects the whole room.
- **Messages.** You: "You turn the handle. A brass ticket drops: 'Please take a number.' The creatures here begin, grudgingly, to queue." Each creature, once (rate-limited): "{X} has been given number {n}. It waits." Room: "{Name} turns a handle on the wall, and {X}, {Y} and {Z} fall into a queue."
- **Counterplay:** a boss in the group (Half: its wait is shortened to half); creatures that make their save; an ally in the room who draws one out; the end of the term.

### 6.10 Summons (`summons`), level 18 (later)
Prep 1.0, Recovery 1.5. Wind-up: "{Name} signs a summons with a flourish..." Target: a **creature named by the caster in an adjacent room** (rank I to III; two rooms at rank IV and V, as Tracking's second rank). It must have a **walkable way** to the caster now (no closed or locked door, no hidden or unfound way: Tracking's own rule), must not be fighting, and the caster's room must not be `Safe`. Never a creature that is `boss` (the line: "{X} is above the law, and not at home to callers.", nothing is spent). A creature that is not `Aggressive` (a deer) can be summoned and fights.

| Rank (level) | I | II | III | IV | V |
|---|---|---|---|---|---|
| Focus | 40 | 46 | 52 | 58 | 64 |
| Reach | next room | next room | next room | 2 rooms | 2 rooms |
| Arrives: Full / Half | at once / 8 s | at once / 8 s | at once / 6 s | at once / 6 s | at once / 5 s |

- **Effect.** Mental save. Full: the creature comes at once and attacks the caster as a creature `drawn` into a fight does (its first action waits its Reaction Time x 2). Half: it comes after the delay (it dawdles; "half the compulsion"). The creature is moved by the spell as if by its own walk (it leaves a line in the room it leaves; it brings its place in its `AddSpawn` with it: the room's refill treats it as away). A summoned creature that makes its save and is None stays where it was.
- **Messages.** You: "You serve a summons on {X}, in the next room. It is not in a position to decline." Arrival: "{X} arrives, summons in hand and with an air of grievance, and attacks {Name}!" Room that it left: "{X} looks at a paper in its hand, and leaves in a hurry."
- **Counterplay:** a boss (never); creatures behind doors; a Half that arrives late; the caster must be able to fight what comes. **It cannot be used to bypass a guard or a door** (the walkable-way rule) and never to pull a creature out of its room's special rules (a boss).

### 6.11 Due Process (`process`), level 20 (later, the capstone)
Prep 1.0, Recovery 2.0. Wind-up: "{Name} opens the Book of Proceedings to a marked page..."

| Rank (level) | I | II | III | IV | V |
|---|---|---|---|---|---|
| Focus | 60 | 69 | 78 | 87 | 96 |
| Full: up to | 12 s | 14 s | 16 s | 18 s | 20 s |
| Half: up to | 6 s | 7 s | 8 s | 9 s | 10 s |
| ...or until it has been struck | 3 times | 3 | 3 | 3 | 3 |

- **Effect.** The target **cannot act** (no action begins; one in its Preparation is cancelled as Cease and Desist does) until it has been **struck three times** (any blow from anyone that lands; a missed or refused blow does not count) or the time is out. The count stays at three at Half; the time limit halves (tags-and-saves.md section 12.2). Physical save (a hold). It is the one spell that lets a Bureaucrat deal with a group: it stops the one that matters.
- **Messages.** You: "You read {X} its rights, at length. It cannot act until the matter has been heard." Struck once: "The hearing proceeds." Twice: "The clerk has an objection." Third: "The matter is heard. {X} is free to go, and does." Or when the time is out: "The hearing is adjourned."
- **Counterplay:** a boss (Half at best, a third of the time); a creature's friends who hurt it for free (this is what the three strikes are for); the repeat and flood rules.

## 7. What the engine and the files must gain (small, listed)
### 7.1 A class gives a skill at a level
`/std/pclass.cs`: `protected void SkillAt(string skillId, int level)` and `public int QuerySkillLevel(string skillId)` (0 for a skill the class gives at once, as every class does now). `/std/adventurer.cs` `RefreshClasses` grants only the skills whose level is at most the character's level; `CheckLevelUp` calls `RefreshClasses()` and `BindSkillCommands()` after a level is gained and tells the player: "You have learned {Title}: {cmd}{command}{/}." (the existing level-up message gains the line). A class taken at a level above a skill's level grants that skill at once at rank I, each with the same "You have learned" line. Existing classes name no level, so nothing changes for them. Test: a test class with a skill at level 3.

### 7.2 The trainer teaches an advanced class's skills
`/std/trainer.cs` `CallingOf`: the calling a trainer answers a player as is, in order, the player's **advanced class** if the trainer teaches it, else the archetype (so a Bureaucrat still buys Arcane Bolt ranks from the Mage hallmaster and Bureaucrat ranks from the Registrar). `OffersFor` already reads `ClassFile(c).QuerySkills()` for any class id; `ranks` shows locked skills as "level 12" (a skill the class has not yet given). **The Registrar teaches only holders:** her `ranks`, `train` and shop verbs are bound by `/std/hiddengiver` only for a character whose advanced class is `bureaucrat`; for anyone else they print "What?".

### 7.3 Actions that can be barred, restrained or refused (`/std/living.cs`)
New small members, each guarded as `Stagger` and `SpoilPreparation` are (the caller is the creature's enemy, or a skill from `/skills/` of a real object; checked with `PreviousObject()`; never a sandbox object):

| Member | Used by |
|---|---|
| `public bool BarAction(string actionIdOrKind, int seconds)` / `public bool QueryActionBarred(string idOrKind)` | Cease and Desist (an id), Red Tape (the kind "summon"); read by the Overseer, the Bandit Leader and the Broodmother's `ChooseAction` |
| `public bool RestrainMelee(ObjectRef from, int seconds)` / `public bool QueryRestrainedFrom(ObjectRef who)` | Restraining Order |
| `public bool GrantWard(int attacks, int seconds)` / `ConsumeWard(ObjectRef attacker)` (called by the attacker's `Attack`, after `PreviousObject()` checks) | Application Denied |
| `public bool InterruptPreparation(ObjectRef by, double pushSeconds)` (the preparing branch of `Stagger` without its resistance roll: the resistance is the spell's own save) | Cease and Desist, Due Process |
| `public bool HoldUntilStruck(ObjectRef by, int strikes, int seconds)` | Due Process |
| `SetQueue(...)` | Take a Number |

**The choice of a restrained creature's action** is one block in `TryBegin` after `ChooseAction(target)`: (i) if the chosen action is barred (by id or kind), take the first declared action that is not barred, else the basic attack, else wait; (ii) if it is an attack action whose reach is `melee` and the target is one it is restrained from: the substitution of 6.2 (a declared ranged action, else another present enemy, else wait one Reaction Time). `Special` actions are untouched. Waiting is "ready again in one Reaction Time" (it re-asks).

### 7.4 Control spells read the rule
Each control spell, in its `Resolve`, calls `/secure/control.Resolve(user, target, effectId, kind, RankOf(user))` and applies strength 2, 1 or 0, the half time from `QueryHalfSeconds`, the line it prints (tags-and-saves.md section 13).

### 7.5 Content files that read a bar
`goblin_overseer.cs`: `if (!summoned && Vitality * 2 <= QueryMaxVitality() && !QueryActionBarred("summon"))` (the flag set only when it really happens); `bandit_leader.cs`, `warren_broodmother.cs`: the same for their summons. A creature that does not summon is unchanged. These three edits exist so that Red Tape is correct against any summoner; no number in this spec is tuned to the Overseer.

### 7.6 Everything else (the Bureaucrat's own files; the shared files are in hidden-classes.md section 9)
| File | Change |
|---|---|
| `mudlib/classes/bureaucrat.cs` (new) | the class (2.1, 3): `Hidden`, the pattern's fields, `SkillAt` for the eleven skills, the summons text, the gate lines |
| `mudlib/skills/bureaucrat/triplicate.cs`, `restraining_order.cs` (command `restrain`), `cease_and_desist.cs`, `red_tape.cs`, `audit.cs`, `application_denied.cs`, and later `per_my_last_letter.cs`, `stamp_of_approval.cs`, `take_a_number.cs`, `summons.cs`, `due_process.cs` (new) | one file per skill, inheriting `/std/skill`, rank tables as `RankRow` lines (section 6). The folder is new, so the lint's own-paths rule covers them |
| `mudlib/domains/greenvale/oakenfell/founders/chancery.cs`, `chancery_court.cs` (new) | the two rooms (2.4) |
| `.../founders/npc/registrar_ashcombe.cs`, `clerk_crake.cs`, `pruett.cs`, `marrowby.cs`, `goose.cs` (new) | the people (11.1); the Registrar inherits `/std/hiddengiver` |
| `.../founders/obj/jury_summons.cs`, `jury_ticket.cs`, `jurors_badge.cs`, `boundary_roll.cs`, `mason_chit.cs`, `jury_certificate.cs`, `clerks_staff.cs`, `notarys_seal.cs` (new) | the items |
| `.../founders/city_hall.cs` | one `GatedExit` line, nothing else (2.3) |
| `mudlib/secure/control.cs`, `tags.cs`, `blows.cs` | tags-and-saves.md |
| `mudlib/std/living.cs`, `adventurer.cs`, `pclass.cs`, `trainer.cs`, `questjournal.cs` | 7.1 to 7.3, 7.7 |
| `tests/bureaucrat_test.py` (new), `tests/class_test.py`, `tests/balance_sim.py`, `tests/lint_area.py` | section 15 |
| `docs/CLASS_SYSTEM.md` (section 12, `SkillAt`), `docs/BUILDING.md` (a class gives a skill at a level; the hidden-class pattern) | one paragraph each. The docs are public, so the Bureaucrat is not named in them |

The Hall of Legends files (`guild/hall_of_callings.cs`, `guild/npc/hallmaster_mage.cs`, `guild/npc/hall_clerk.cs`) and `founders/npc/clerk_maude.cs` **do not change**: the first spec's topics there are removed.

### 7.7 The quest's own pieces
- **`ProgressNote`** (`/std/questjournal.cs`): a virtual `ProgressNote(string questId, ObjectRef who)` on the giver NPC, returning one line that the journal appends under the objectives of an active quest (null for none). Existing quests return null. `/std/hiddengiver` returns the line for the register's stage (section 11.5).
- **`submit`** (`/std/hiddengiver` and the Clerk): `submit ITEM` in the room with the Clerk takes a carried exhibit (a `QuestItem` the Clerk knows) and records it. It exists because `give` refuses NPCs today. It prints nothing for anyone who is not a sworn juror (the verb falls through to "What?").
- **The trial verbs** (`triplicate`, `restrain`, `desist`) in the hearing room only: bound by `chancery_court.cs` in `Init`, a handler that returns false unless the register's stage for the character is 3 (sworn, case open), so a character who is not a juror, or has finished, falls through to their own skill command if they have one, else "What?". They cost nothing, need no skill and are not saved. Their strengths are fixed by the scene, not by the tags-and-saves daemon; they do not touch `/secure/control`.

## 8. Balance: why it stays in the same band
The first spec judged the class by the Overseer at level 5 (band 72% to 83%). That test is withdrawn for this class (decision 8; the owner: "A bureaucrat probably isn't fighting the Overseer anyway"): the class starts at level 5 or later, in a city, and its control spells do half or nothing against a boss. The yardstick is section 10. This section keeps the argument for why control does not break the game, and the list of what keeps the class near its calling. Nothing has been measured.

### 8.1 Rough numbers: control against a damage cast
Assumptions (stated so they can be changed): a Mage-origin Bureaucrat at level 7, Insight 17, spell power +26%, Focus 145 (Focus Efficiency applies), staff (Reaction 3.0, so every spell is an 8-second cast), Arcane Bolt average 9.5 for 16 Focus; a creature of level 7 with 53 Vitality, armour 3, damage 7-11 (average 9), a delay of 7 s, hitting a character 60% of the time: **0.77 damage a second**. Pure bolting kills it in 6 bolts: **48 seconds, 37 Vitality lost**. Count each cast as one 8-second slot in which the caster is not bolting; a bolt in that slot saves about 8 seconds of the creature's life, worth 6.2 Vitality.

| Plan on one level 7 creature | What a cast gives | Compared with a bolt in the slot (6.2) |
|---|---|---|
| Forms in Triplicate, Full (20 s, creature slowed 29%) | 0.29 x 20 x 0.77 = 4.5 Vitality | worse |
| Restraining Order, Full (10 s) | 10 x 0.77 = 7.7 | slightly better, **once** |
| Restraining Order, second use in the minute (Half, 5 s) | 3.9 | worse |
| Cease and Desist on a creature with a 2 s wind-up that hits for 15 (the Overseer's Power Strike) | 0.65 x 15 = 9.8 | better, if it is timed |
| Application Denied on the same blow | 9.8 | better; needs a second Focus bar |

**The loop the owner worried about** (restrain it, then plink it): in a fight with **no** repeat rule and no half, an order every 16 seconds alternating with a bolt keeps the creature restrained 69% of the time. The caster loses half his bolts. Result: the fight lasts 96 seconds, the creature's active time 30 seconds, **damage taken 23 instead of 37 (-38%)** for 206 Focus against 112. Not a free win (the Focus pool of 145 plus 45 regained over the fight just pays it), but a **one-third** reduction in damage taken for nothing but a command, repeated.

With **this spec's rules**: the order is Full once, Half once, then None for 60 seconds, and a boss makes it Half then None; the cast cannot be repeated for effect. Over the same 6-bolt fight (48 seconds of bolts): add two orders (16 seconds): 64 seconds; the creature is restrained 15 of them; **active 49 seconds: 38 damage, the same as not casting them**. That is what the saves, the repeat resistance and the weak damage are for: **control is about timing, groups and bosses' dangerous moves, not about stopping the whole fight.** And it cannot be done with a ranged weapon at all: a Mage has no bow; the plinking is bolts and letters, which have to be paid for and cast in the 8-second slot.

### 8.2 What keeps the class in the band
- **Weak damage** (the Mage's bolt, and Per My Last Letter, which needs seven casts on one target to equal a bolt; the Clerk's Staff 5-9).
- **Low Vitality** (Mage's, +2).
- **A boss gives half or nothing:** every control spell is Half or None against a boss (tags-and-saves.md section 12.4). The class is a poor choice for fighting bosses alone and a good one for the creatures between them.
- **The repeat and flood rules** (a slow at most 38% of the time; an order 21%).
- **Everything costs Focus at 24 to 32** (the bolt costs 16 to 20), and a long fight is Focus-limited.
- **No ranged weapon, ever.** Weapons: dagger and staff.
- **The class is given at level 5 or later with every skill whose level has passed,** so a Mage who takes it at level 9 has four control spells at rank I and nothing yet at rank II or above: they are bought (II at 5, III at 10). It is a gain of control and a loss of nothing, and the yardstick checks the pace does not rise above the Mage's by more than 8%.

### 8.3 What `tests/balance_sim.py` must model for the class
- Vitality 6 + 7, Endurance 14 + 1, Focus 76 + 4, the Mage's attributes and placement (Insight, then Resolve), Focus Efficiency, the Clerk's Staff and the Notary's Seal as options.
- The control module of tags-and-saves.md section 18: each spell's strength, half time and repeat steps; Authority from placement and rank.
- Each spell as an action on the combat clock with its Preparation, Recovery and Focus cost: slow as a factor on a creature's Reaction Time (and the x2.0 cap); the restraint as the substitution of 6.2 (another enemy, a declared ranged action, or a Reaction-Time wait) for the creatures' real actions; a bar; the ward (refuse before the dodge); the interrupt of a preparation as the engine does it (a human's delay before typing, BALANCE_HUMAN.md); Per My Last Letter's stack; Stamp; the queue; the summons; the hold.
- **The class's policy** (a human of the class): Forms on a creature that is not a boss and has more than half its life; Cease and Desist on a telegraphed action when ready (Wild Boar Charge, Boar Brute Trample, Goblin Raider War Whoop); Audit before the first order on a creature above your level; Restrain when two or more melee creatures are present or when a creature has no ranged action; Application Denied on the caster when a Charge or Trample is not answerable.
- Pace (`pace_cell`, `pace_mix`) with the class as a sixth column beside the five callings, and a column for the Mage who took the class at level 5 and one who took it at 8.
- A solo "loop" test: restrain, bolt, restrain, bolt, to check the section 8.1 figures (a 38-second fight with two orders should show no gain).
- **Not simulated:** the summons roll (a tally, exact table in hidden-classes.md section 5.2) and the quest (no combat).

## 9. Groups: the party system does not exist
Application Denied and Stamp of Approval use `Target = "friend"`, which is the caster or **any player character in the room** (as Blessing). They need no party and no consent. Take a Number protects whoever the creatures fight. Restraining Order shields only the caster and Audit marks for the caster's own next order. When a party system arrives: Stamp of Approval may be limited to party members; nothing else changes. A second Bureaucrat in the room is allowed: orders from two casters on one creature are separate on the creature but the daemon's repeat and flood records are per target (tags-and-saves.md section 14), so the pair gets Full once.

## 10. The yardstick for levels 5 to 10 (replaces the Overseer test)
Run `python3 tests/balance_sim.py pace` and `corridor` for the class at levels 5, 6, 7, 8 and 9 in the four variants of the pace model (perfect; no haven; no potions; neither), with the policy of 8.3. Pass means every line below holds; the owner then sets the exact band. The method is the pattern's (hidden-classes.md section 11); these are the class's own set pieces and numbers.

1. **Pace.** Experience an hour on `prey(level)` (the ordinary creatures of the character's level and one above): **within 8% of the Mage's at each level 5 to 9** (CORRIDOR_BALANCE.md figures for the Mage today, perfect play: 2,189 an hour at level 5, 2,846 at 6, 3,968 at 9; "neither": 1,187, 1,562, 3,239), and **within 12% of the harmonic mean of the five callings** (2,180 at level 5, 2,654 at 6, 3,234 at 7, 3,532 at 8, 3,797 at 9). The class at level 5 has only Forms in Triplicate: expect it a few points under the Mage, never over.
2. **Survival.** Deaths an hour: no more than the Mage's by more than 25% at any level.
3. **Four set pieces**, the policy's win rate within 10 points of the Mage's:
   - **Wild Boar** (level 6, 40 Vitality, damage 11-19, opens with Charge: wind-up one unit = 2 s, damage x2, recovery 1.5; then Gore). The interrupt answer: Cease and Desist at the wind-up.
   - **Boar Brute** (level 7, 45 Vitality, damage 8-14, Trample with a one-unit preparation: interruptible).
   - **Thorn Woodling** (level 9, 53 Vitality, Thorn Barrage with a half-unit preparation = 1 s): only answerable by prediction; the table must show the human policy cannot interrupt it reliably, and the class wins by other means (a slow, a ward).
   - **A Goblin Raider (level 7, 53 Vitality, War Whoop buff, half-unit preparation) with a Goblin Scout (level 7, 80 Vitality):** a pair, the class's Restraining Order and Forms against two.
4. **The gap between the five callings and the class** at levels 5 and 10 is not wider than the gap between the callings today (skill-ranks.md).
5. **Bosses are reported, not required:** the Overseer (level 5) and the five level-10 bosses (Bandit Leader, Fallen Knight, Forest Panther, Warren Broodmother, Barrow Guardian) are run and printed with no band.
6. **What exists to measure on:** the 44 creatures of the Hollow and the Corridor. Level 4: 2 kinds; 5: 2 (and the Overseer); 6: 3; 7: 4; 8: 8; 9: 6; 10: five bosses. Nothing above 10. The yardstick therefore ends at level 9 for ordinary creatures; levels 10 and above wait for content.

## 11. The people and the quest "Jury Duty"
Everything in this section is in the class's own files (`OwnPaths`), reachable only through the gated door. The giver, the Registrar, inherits `/std/hiddengiver` (hidden-classes.md section 7), which inherits `/std/trainer`. The journal quest is `jury-duty`, title "Jury Duty", area "Oakenfell", **not abandonable**. Case state is kept in the register's `stage` and `notes` for each character (hidden-classes.md section 3), so it survives a logout or a restart; the NPCs are shared, the state is each player's.

### 11.1 The people
All are `Peaceful`, pay no experience and cannot be fought (the rooms are `Safe`). Their first names are unused elsewhere (lint check 6; checked against `mudlib`).
- **Marian Ashcombe, Registrar of the Chancery** (`npc/registrar_ashcombe.cs`, Level 20, Vitality 90; waiting room, behind the counter): "A tall, unhurried woman in a grey coat with ink to the second knuckle and a brass seal on a cord at her throat. She is writing, and does not look up until she has finished the line." Giver, swearer, finisher and, afterwards, the class's trainer and shop. `AddChat` (in turn, only while a juror is present): "The Chancery is open. Kindly state your business, in writing if you can." / "A matter not entered is a matter that has not occurred. I say this to everyone. It is generally the first thing they dispute." / "Please form a queue. There is no queue. Please form one anyway."
- **Osbert Crake, Clerk of the Court** (`npc/clerk_crake.cs`, Level 12, Vitality 60; court): "A small, pale man in a black gown and a wig that was made for a larger head. He holds a bell in one hand, a pen in the other, and an air of having been right for a long time without anyone noticing." Takes the evidence and the verdict.
- **Gervase Pruett, plaintiff** (`npc/pruett.cs`, Level 3; court, left table): "A square man in a gardener's apron, red to the ears. He is gripping a marrow-shaped hole in the air."
- **Tilda Marrowby, defendant** (`npc/marrowby.cs`, Level 3; court, right table): "A broad, composed woman in a good shawl, who looks at the court as one would look at weather."
- **The goose, Counsel for the Defence** (`npc/goose.cs`, Level 1; court, on the right table): "A large grey goose with an expression of settled judgement. A small card on a string round its neck reads COUNSEL." Never fights; `kill goose` prints "The court takes a dim view." and nothing happens.

### 11.2 The Registrar, by stage (every line below is quest-tagged `jury-duty`)
"Stage" is the register's stage (hidden-classes.md section 3). A line shows only at the stage named. Anything not listed gets the unknown-word answer.

**Entry (stage 1 to 2; the room's `Noticed` begins the quest).** Room line for the player: "Marian Ashcombe does not look up from her ledger until she has finished the line. 'Summons.' It is not a question. You hold it up. She reads the front, and the back, and stamps the corner. 'Reference accepted. You are Juror Twelve of Twelve. The other eleven are not coming. They never do. That is not your concern, and it is now partly your duty.' She slides a card across the counter. TAKE A NUMBER, it says, in case you had not noticed the machine. 'Take a number. Then ask me about the oath. The court sits when it is sworn.'" The journal begins (quest state 1); stage 2.

| Topic keys (stage) | She says |
|---|---|
| *oath, swear, sworn, badge* (2, no ticket) | "I cannot swear an unnumbered juror. It has been tried. Take a number." |
| *oath, swear, sworn, badge* (2, ticket taken) | Marian Ashcombe rises. "Raise your right hand. The other one. Repeat after me. I will hear the matter. I will not be moved by the length of the speeches. I will say yes or no, and mean one of them." You say it. She stamps a small brass badge on the pad and slides it across. "A juror's badge. It is a loan from the court and it will be asked for back. Through the east door. Clerk Crake will take it from there. Do not touch the goose." (The badge is given; stage 3. Journal objective met.) |
| *number, machine, ticket, clock* (2) | "The machine is correct. The clock is correct. The time is wrong. If you can explain that to me, I will find you something better to do." |
| *summons, form, reference, juror, jury, duty* (2, 3) | "Form 27-J. Third copy. It means what it says. It said fourteen days, from the date you read it. You have read it. Nobody has ever been late." |
| *fine, thirty-seven, one third* (2, 3) | "The fine is thirty-seven gold and one third of a gold. It has never been collected. The third of a gold is the difficulty." |
| *court, east door, hearing, case, matter, goose* (2) | "The court sits when you are sworn. It is Pruett against Marrowby, the matter of the goose. You will know it when you hear it." |
| *orders, authority, borrowed, trial, forms* (3) | "In the court a juror may serve three of the court's orders, on its authority and for the length of the hearing. A restraining order, a notice to cease and desist, and a form in triplicate. They are on loan. They work on the parties and on Counsel, and nowhere else. They will not work in a corridor. Do not become attached." |
| *roll, boundary, pigeonhole, pigeonholes, weather* (3) | "Boundary matters are filed under Weather, because they are always changing, and Weather is under Adjournments. Pigeonhole two hundred and twelve. I cannot stop you looking. I am not permitted to help." |
| *verdict, clerk, crake* (3) | "That is for Clerk Crake. I only enter it." |
| *badge, lost* (3) | If the badge is gone: "Again? It is a loan." and another is given. Otherwise: "You have it. You are wearing it. Is it not remarkable?" |
| *anything else, chat* (2, 3) | the `AddChat` lines |

**Return (stage 4: the Registrar's `Noticed` fires when a stage-4 juror enters or speaks to her).** "Marian Ashcombe holds out her hand. You give her the badge. She enters the verdict in the Book of Proceedings and reads it back to you, without comment, and with a different comment for each kind of verdict." The closing remark by verdict and by the number of exhibits the player entered (none, one, both):

| Verdict | no exhibits | one exhibit | both exhibits |
|---|---|---|---|
| liable (y) | "Liable, on no exhibits. The court admires decisiveness. The court also wonders what it was based on." | "Liable, on one exhibit. A sound habit: when in doubt, find against the person who has been shouting." | "Liable, on both exhibits, with the elm in evidence. Rigorous. Mrs Marrowby has been told that the goose will in future be supervised." |
| not liable (n) | "Not liable, on no exhibits. Generous. The Chancery does not hold it against you." | "Not liable, on one exhibit. Cautious. It will survive an appeal, which is no part of the verdict." | "Not liable, on both exhibits. The elm has been moved, the marrows were on her side, and the goose cannot be shown to have known. Mr Pruett has appealed. He has appealed to the elm." |

Then she pays: "The juror's fee is one gold. It is entered." You receive 1 gold and 250 experience ("You have served the court."). Stage 5; the offer follows at once (11.6).

### 11.3 The Clerk, the parties and the case
All of this is stage 3. A juror enters the court from the waiting room.

**On first entry** (once; the room, to the juror): "Clerk Crake rings his bell. 'All rise,' he says, and then, seeing that you are already standing, 'Noted.' He consults a paper. 'Pruett against Marrowby: the matter of the marrows. Three marrows, prize, the Harvest Show nine days hence, said to have been eaten by a goose, said to belong to the defendant, said by the defendant to belong to no one. The plaintiff claims eleven gold, and the stakes. Counsel for the defence is the goose, entered as such since Tuesday. The jury is you.' He puts the bell down. 'You will hear the plaintiff and the defendant. You may examine the exhibits, which are not yet exhibits. You will give a verdict, yes or no. You may use the court's orders on the parties if the proceedings require it. They usually do.'"

**Asking the parties** (`ask X about ...`):
- *Pruett: marrows, marrow, goose, boundary, elm, claim, line* (first time sets `p_heard`): "'Three marrows. Three! Prize marrows, madam, sir, whatever you are. The Harvest Show is in nine days. The goose ate them, and the goose is hers, and the line is where the Roll says it is, and the Roll says my bed is mine. Eleven gold. And the stakes.' He sits. He stands again. 'And the stakes.'" Repeats: "'Eleven gold. And the stakes.'"
- *Tilda (and the goose) while Pruett is unrestrained* (note `pr` is 0): "Gervase Pruett is on his feet before she can answer. 'Objection! She is lying! The goose is hers! The goose has always been hers!' The Clerk rings his bell. 'The court notes the objection,' he says. 'The court notes that it was not made to anything.' Tilda looks at her hands. 'Jurors,' Clerk Crake adds, to no one in particular, 'have been known to have a form for that.'"
- **`restrain pruett`** (trial order, first use sets `pr`): "You serve Gervase Pruett with a trial restraining order, on the court's authority. He may not come within reach of Tilda Marrowby, which he takes personally. He sits, writes something, and underlines it." Room: "{Name} serves Gervase Pruett with a restraining order. Pruett sits." Repeat: "Pruett has been served. He is writing about it."
- **`ask tilda about goose`** with `pr` set (first time): "Tilda Marrowby draws a very long breath. 'It begins,' she says, 'with my great-grandmother, Hester Marrowby, who came to Greenvale in the year of the late frost, and who kept geese, and whose mother kept geese, and whose mother's mother-'" **A window of 8 seconds opens** (memory only, not saved).
  - **`desist tilda` inside the window** (note `ds` = 1): "You serve Tilda Marrowby with a trial notice to cease and desist, on the court's authority. Her great-grandmother stops mid-sentence. 'Oh,' says Tilda. 'Well. Briefly, then.'"
  - **Outside the window or not at all** (`ds` = 2): "The history proceeds as far as the Founders' second winter. Clerk Crake rings his bell and waits. At length: 'Briefly, then,' says Tilda, and has the grace to look tired." The Clerk takes no question for the next 20 seconds ("The court is recovering."). No other penalty.
  - **`desist tilda` with no window open:** "Tilda Marrowby is not saying anything that has not already been filed."
  - Either way, then (sets `t_heard`): "'The goose is mine. I have never said otherwise. I said it is not guilty, which is different. It was on my side of the line when it ate his marrows. Ask the court where the line is. Hollis the mason moved the old elm, and Pruett's father paid him for it; there will be a receipt, because there is always a receipt. Try the table.'"
- *Goose: any topic:* "The goose looks at you. You have the impression of having been entered." (`ask goose about roll`, while it holds the Roll: "The goose looks at the Roll, and then at you, and holds the Roll a little higher.")

**The Boundary Roll (the comic interruption).** The roll is found in the waiting room (11.4). When a juror with the Roll (`roll` = 1) walks east into the court and the goose has not yet been served (`gs` = 0):
> "Counsel rises. It crosses the court with great dignity and at surprising speed, takes the Boundary Roll out of your hand, and returns to the right-hand table with it. Clerk Crake does not look up. 'Counsel has asked to see the exhibit,' he says. 'Counsel always asks to see the exhibit.'"
The Roll leaves the pack (`roll` = 2). `take roll` then: "The goose is holding it. The goose looks at you." **`triplicate goose`** (the trial form, sets `gs`): "You serve the goose with a trial Form 27-B, in triplicate, on the court's authority. It begins, slowly, to look for the second copy. It does not put the Roll down. It is, however, looking elsewhere." Room: "{Name} serves the goose with a form. The goose studies it." After this `take roll` works ("The goose is still looking for the second copy. You take the Roll from under its wing. It lets you."), `roll` = 1, and it is not taken again. If `gs` was already 1 when the Roll arrives: "Counsel rises, looks at you, and then at the Roll, and then, slowly, at the space where its second copy should be. It sits." No interruption. `restrain goose` or `desist goose`: "The court's authority does not stretch that far. The goose is Counsel, not a party." Nothing in this needs combat.

**Exhibits.** `read roll`: "BOUNDARY ROLL: MARROW LANE. 'The line runs from the great elm, west two paces to the stake, and thence to the pump. Weather: fair. Entered in the year of the late frost. Witness: H. Hollis, mason.'" `read chit` (the mason's receipt, found by `search table`, 11.4): "'Received of Mr G. Pruett, senior, three gold, for moving the great elm two paces east, the old one being in his opinion in the wrong place. Entered. H. Hollis, mason. (Nobody asked for a receipt. This is it.)'"
- **`submit roll`** / **`submit chit`** (the Clerk, in the court): takes the item (a Bound exhibit): "You hand the Roll to Clerk Crake. He holds it at arm's length, as one holds a baby that has been returned. 'Exhibit One,' he says, and writes." / "Clerk Crake reads the chit twice. 'Exhibit Two,' he says, quietly. At the left-hand table Gervase Pruett finds something very interesting in his boots." (`roll` = 3, `chit` = 3.) `submit` anything else: "Clerk Crake does not accept that. The court has never accepted that." Nothing in the pack that matches: "You have nothing the court recognises."

**The verdict.** `ask clerk about verdict` (also `verdict`, `finished`, `decide`): if `p_heard` and `t_heard` are not both set: "The court cannot take a verdict from a juror who has not heard the parties. It has tried." Otherwise Clerk Crake asks, and the next line the juror types answers (the prompt is `InputTo` with the Clerk as owner): "Clerk Crake rises. 'The jury will say, yes or no, and without elaboration. Is the defendant liable for the marrows? (y/n)'"
- `y` / `yes`: "'The court enters: liable.' He writes. 'Eleven gold, and the stakes. The court regrets the stakes.'" (`v` = 1)
- `n` / `no`: "'The court enters: not liable.' He writes. 'Mr Pruett will be informed. He will be informed at length.'" (`v` = 2)
- Anything else: "'The court requires a yes or a no. The court has waited longer for less.'" and asks once more; a second wrong line ends the prompt with "'The court will ask again when you are ready.'" (no verdict; ask again later).
Then both parties: Pruett: "'The marrows,' says Gervase Pruett, to no one. 'The stakes.'" Tilda: "Tilda Marrowby nods to the court as one nods to the weather, and collects the goose." The Clerk: "'The matter is heard. Return your badge to the Registrar. She will have a question for you. I would not be hurried.'" The register moves to stage 4. The case cannot be reheard. A juror who walks away at any point before the verdict finds the case where it was left.

### 11.4 The two finds (hidden things, per character)
- **`search pigeonholes`** (waiting room; only a juror at stage 3 with `roll` = 0): the room declares one `HideItem`-style find under the search rules (`/secure/search.cs`): difficulty Moderate (12), a per-character find (`/std/find` with first <= 0), item `boundary_roll` (`QuestItem`, `Bound`). First failures: "You go through the pigeonholes. They hold a great many things that are not what you want." On success: "In pigeonhole two hundred and twelve, between a notice of adjournment and a very old umbrella, is a long roll tied with green tape. You take it." If lost: the same pigeonhole yields it again: "The Roll is in the pigeonhole again. It generally is."
- **`search table`** (court; stage 3, `chit` = 0, and `t_heard` set, so Tilda has said where to look): the same rule, Moderate (12), item `mason_chit`. Fail: "You find an inkwell, a stub of wax and a peppermint of some age." Success: "In the drawer of the left-hand table, under a stub of wax and a peppermint, is a slip of paper, curled with age. You take it." A character who has not heard Tilda yet does not find it: "You find an inkwell and a peppermint. Perhaps someone in the room has a suggestion." (A search that cannot succeed yet still takes its normal time; no lockout.)
- Neither find is visible to a character who is not a juror; neither is a lint-reported hidden thing for the world (check 15 learns the class's own paths are exempt).
- Neither is needed to finish: a juror who finds nothing still gets a verdict (the closing remark says "no exhibits").

### 11.5 The journal
`DefineQuest("jury-duty", "Jury Duty", "Oakenfell", completed, abandoned, items: the three exhibit paths, objectives: "have|/domains/greenvale/oakenfell/founders/obj/jurors_badge|Be sworn in and take a juror's badge|1", abandonable: false)`. `Follow()` reads the summaries from the caller's definition, so the Registrar (finisher) defines it too (hidden-classes.md section 7).
- **Completed** (the same for either answer): "You were summoned to the Chancery of Oakenfell, sworn as a juror, heard the matter of the goose and the marrows, gave your verdict and answered the Registrar's last question. The court has entered all of it."
- **Abandoned** (written because `DefineQuest` requires it; never shown unless a wizard clears the quest): "You set the summons aside. The court notes that the matter is still open."
- **Rewards line** at completion (what `quests` shows): a taker: "1 gold, 250 experience and a seal of the Chancery."; a decliner: "1 gold, 250 experience and a Certificate of Jury Service."
- **`ProgressNote`** (below the objective, by register stage): 2: "You are at the Chancery with a summons. Take a number, then ask the Registrar about the oath." 3: "You are sworn. The court sits to the east: Pruett against Marrowby, the matter of the goose. Hear the parties; the Clerk will take your verdict." 4: "The verdict is entered. Return your badge to the Registrar." 5: "The Registrar has a question for you. Neither answer is urgent. Neither is reversible." Nothing says "class".

### 11.6 The ending: the offer, `pledge`, `decline`
After the fee (11.2), stage 5. The Registrar says, in order:
> "Marian Ashcombe closes the Book of Proceedings and, for the first time, looks at you. 'One further matter. The court has an opening. It is not a post, and it is not advertised, and nobody applies to it. A clerk of the Chancery is a mage who has taken the seal. You have this afternoon served three orders in this building without being struck by lightning, which is not nothing. If you wish to take the seal, say {cmd}pledge{/}. If you do not, say {cmd}decline{/}. Whichever you say, the juror's fee is yours and the court thanks you. I would think about it.'"
The two verbs are bound by the giver only while the register says the offer is open (stage 5, no outcome), otherwise "What?".

**`pledge`, first time** (the permanence line): "Marian Ashcombe sets down her pen. 'I will say this once, as the Chancery requires. The seal is for life. You will keep all you are: the staff, the Bolt, the Lore. You will gain the seal, and the forms that go with it, and you will not be able to put either down. No fee is asked. If you are sure, say {cmd}pledge{/} again within the minute.'" A 60-second memory timer starts (not saved).
**`pledge`, second time within the minute:** `ApplyClass("bureaucrat")` (level 5, Mage, no other advanced class, the offer open), then the **ceremony**: to you: "Marian Ashcombe rises. 'Raise your right hand. The other one. Repeat after me. I will lodge before I act. I will bind nothing that has not been served. I will not, upon any provocation, agree that a war was properly declared.' She takes the seal from the cord at her throat, presses it into a pool of hot wax beside your name in the Book of Proceedings, and blows on it. 'It will be dry in a moment.'" Room: "Marian Ashcombe presses the Chancery's seal into hot wax beside {Name}'s name in the Book of Proceedings. The room is very still." Then the engine's own line "You are now a Clerk of the Chancery." and hers: "Your first form is Form 27-B. It is in triplicate. Please do not lose the carbon. And take this. The Chancery issues one to every clerk, and it is not to be sold." (the Notary's Seal enters your pack; the summons is "entered": taken from the pack.) Skills whose level is at most yours are bound at once (section 7.1). The register records "taken" and stage 9; the journal completes; `class.hidden` is reported (section 14). A `pledge` after the minute is a first `pledge` again. If `ApplyClass` refuses (another advanced class taken meanwhile: section 11.7) nothing is taken and she says the release line.
**`decline`, first time:** "Marian Ashcombe inclines her head. 'You may decline. It is permitted, and it is final. The Chancery does not summon twice. Say {cmd}decline{/} again within the minute and it is entered.'"
**`decline`, second time within the minute:** "'Entered.' She writes. 'Declined, with thanks. The court keeps the summons.' She takes it from your pack and files it under Weather. She stamps a small card and hands it to you. 'A certificate of jury service. You are exempt for seven years. This is not a joke and it is not, so far as I know, a lie.' She looks back at her ledger. 'The door will not trouble you again.'" The register records "declined", stage 9; the journal completes; the certificate (a keepsake: `read certificate`: "CERTIFICATE OF JURY SERVICE. The bearer has served. The bearer is exempt from further service for seven years, or until the Chancery forgets, whichever is the longer.") enters the pack. **No `class.hidden` is reported for a decline** (nothing happened). The inner door answers "What?" from then on for ever.
**Neither:** walking away, or typing anything else, leaves the offer open. When the character next enters the waiting room the Registrar says, once: "Marian Ashcombe looks up. 'Still pending. It is perfectly in order for it to remain so.'"
**A character who `pledge`s with no offer open:** "What?".

### 11.7 Edge cases
- **Quit, crash or restart mid-case:** register and notes persist; an open prompt (the verdict, the 8-second window, the 60-second timers) is dropped and begins again. The summons survives (or is re-issued at the door).
- **A second player in the room:** each juror's case is separate (the notes are per character); the NPCs speak to the one who asked and the rooms show the others each other's lines. Two jurors may hold the case together; the goose takes each one's roll.
- **A lost badge, roll or chit:** the badge from the Registrar ("Again? It is a loan."); the roll and chit by searching again.
- **A juror who takes an ordinary advanced class while summoned:** at the next knock, or at the offer, the register finds an advanced class: the Registrar says, and nothing else is offered: "You have taken another seal. The Chancery does not issue a second. The fine is waived, as it was always going to be. The juror's fee is one gold, entered." (1 gold and 250 experience; the journal completes; the door closes; the summons is retained.)
- **Level up, or a different level at the offer:** no effect; the offer needs level 5, and the character has it.
- **A wizard:** the door and the roll treat wizards as the pattern says; a wizard cannot start the case. The `hidden` command can set a stage for a test character (hidden-classes.md section 9).
- **Dropping or giving:** the summons, badge, roll and chit are `Bound`; `drop`/`give` print "It is addressed to you. It is not a thing one drops." (the summons) and, for the others, "It belongs to the court."
- **Dying:** impossible in the Chancery (`Safe`, no creatures).
- **The trial orders outside the court:** "What?" (the handlers are the court's). A Bureaucrat who returns later and types `restrain` in the court gets their own skill's answer (the room handler returns false at stage 9).
- **A character who never speaks to the Registrar after the verdict:** stays at stage 4, may stand in the court as long as they like, and cannot leave the offer behind: the Registrar says it on entry.

### 11.8 After the class (holders only)
The Registrar then trains and sells. **Topics for holders** (`AddTopic`, untagged to a quest; the unknown-word answer for anyone else):
- *chancery, this place:* "The Chancery of Oakenfell keeps the Assembly's papers and the city's peace, in that order, and the one has never once required the other. We have never lost a war. We have never agreed that one was properly declared."
- *bureaucrat, clerk, clerks, class, path, seal:* "A Clerk of the Chancery is a Mage who has taken the seal. The common word is Bureaucrat, and I do not correct it, because to correct it would be to open a file. A clerk slows what must wait, bars what must not approach, stays what must not be said, and serves notice on what cannot read. It is not a calling for the impatient."
- *spells, forms, triplicate, restraining order, desist, tape, audit:* one line each. *Forms in Triplicate:* "Form 27-B. Three copies. The creature keeps none and wishes it understood why." *Restraining Order:* "It may not come within reach of you. It may do other things. The order is quite specific." *Cease and Desist:* "Served at the moment it begins, which is the only moment it may be served." *Red Tape:* "A creature may not call for help without a countersignature. I need not tell you who countersigns." *Audit:* "Nothing is so useful as knowing precisely what one is dealing with. Nothing is so unpopular."
- *boss, above the law, ignores your authority:* "There are things in Hollowmere that are above the law, and they are almost always the things one most wishes to serve. Half the order will hold; sometimes none. There are also things that ignore your authority, as there are in any office: they are only above your grade. They are served more often than you would think. A senior clerk is a good deal harder to ignore."
- *fee, gold, price, ranks, staff, seal:* "Ranks are forty, a hundred and fifty, four hundred and nine hundred, entered separately. The staff is forty-five. A second seal is sixty. Fees are posted."
- *jury, court, goose:* "Mrs Marrowby's goose is, I understand, in good health. Mr Pruett's marrows are not." 
- **Rank trainer** (`ranks`, `train`): "Ranks are bought of me and entered." The refusal lines of `/std/trainer` are used unchanged. She sells the Clerk's Staff of Office and the Notary's Seal (`sell` list) to holders only.
- **A character who declined:** the door is closed, so none of this is reachable.
## 12. Sample fights (the tone)
Prompt and wiz lines are omitted. A level 7 Bureaucrat (a Mage, rank I spells, Focus Efficiency), Insight 17, Authority 9.

### 12.1 A tunnel rat (level 7, 40 Vitality, melee only)
```
> triplicate rat
You attack a tunnel rat.
You draw three sheets of paper and a carbon from a satchel...
You serve a tunnel rat with Form 27-B, in triplicate. It begins, slowly, to look for the second copy.
A tunnel rat slows, studying it.
> bolt rat
Your bolt of arcane force strikes a tunnel rat for 9 damage.
A tunnel rat bites you.
> restrain rat
You uncap an inkwell and begin to write...
You serve a tunnel rat with a restraining order. It may not come within reach of you.
A tunnel rat takes a step toward you, stops, and reads the order again.
> bolt rat
Your bolt of arcane force strikes a tunnel rat for 8 damage.
A tunnel rat takes a step toward you, stops, and reads the order again.
> bolt rat
...The tunnel rat dies.
```
(The rat's own lines in this log use the Part A wording: "A tunnel rat nips you." and "bites you", by the share of your Vitality it takes.) The point: the rat stops, reads, and is bolted.

### 12.2 A goblin guard (level 2, Alert, in the Guard Post; the Bureaucrat has come back at level 6) and a keyholder beside it
```
> restrain guard
You uncap an inkwell and begin to write...
You serve a goblin guard with a restraining order. It may not come within reach of you.
The goblin keyholder jabs you.
> desist keyholder
The goblin keyholder is doing nothing that has not already been filed.
```
A goblin guard alone, level 2 against a level 6 caster: S = 5%.
```
> restrain guard
You serve a goblin guard with a restraining order. It may not come within reach of you.
The goblin guard takes a step toward you, stops, and reads the order again.
> bolt guard
Your bolt of arcane force strikes a goblin guard for 9 damage.
> bolt guard
...
> staff guard                 [the caster hits it with the staff in person]
Your order against the goblin guard lapses: you struck it in person. A petitioner may not seek protection and strike in the same breath.
The goblin guard pierces you deeply.
```

### 12.3 A Wild Boar (level 6, 40 Vitality): its Charge, answered
The boar opens with Charge (wind-up one unit, 2 seconds, damage x2 of 11-19), then Gore. The interrupt must be typed in those two seconds; Cease and Desist has no wind-up of its own, so it resolves the moment the caster is ready.
```
> bolt boar
You attack a wild boar.
Your bolt of arcane force strikes a wild boar for 9 damage.
The wild boar lowers its tusks and paws at the ground.
> desist boar
You serve a wild boar with a notice to cease and desist. Its Charge stops mid-motion.
A wild boar's Charge stops mid-motion.
The wild boar gores you.
> triplicate boar
You draw three sheets of paper and a carbon from a satchel...
You serve a wild boar with Form 27-B, in triplicate. It begins, slowly, to look for the second copy.
A wild boar slows, studying it.
> bolt boar
Your bolt of arcane force strikes a wild boar for 8 damage.
```
The Charge (22 to 38 damage on a 58-Vitality character) never lands, and the boar may not Charge again for ten seconds. Missed by a second, the same fight costs the caster about half their Vitality: that is what the set piece in section 10 measures.

### 12.4 A creature above your level: "ignores your authority"
A level 5 Bureaucrat serves a Boar Brute (level 7: two above; resistance 8%, so a save chance of 8 + 20 - 9 = 19%):
```
> restrain boar
You serve a boar brute with a restraining order.
The boar brute ignores your authority. The order holds for half its term.
```

## 13. Help text
**None before the class is held.** `help bureaucrat`, `help restrain` and every other word of the class answer as any unknown word does ("There is no help on that."), for everyone who does not hold the class, including wizards below level 10. For a holder:
`help bureaucrat`: "BUREAUCRAT (the Clerk of the Chancery). A Mage who has taken the Chancery's seal. Control, not damage: slow, bar, interrupt, delay, and mark. Leans on Insight, then Resolve. Weapons: dagger, staff. Armor: light. You keep every Mage skill. The seal is for life."
`help restrain`: "RESTRAIN (Restraining Order), Bureaucrat level 6. For 10 seconds the creature cannot make a melee attack against you. It may use a ranged attack, or turn on someone else, or wait. Your own spells do not break it; your own staff does. Bosses are above the law and the order half holds; a creature above your level may ignore your authority. See {cmd}help control{/}."
`help control` (one paragraph, shown to anyone, as it names no class): "Orders, slows, bars and holds are served on creatures. A creature may resist: a creature above your level resists more ('ignores your authority'), and a boss is above the law: for a boss an order holds for half its term, or not at all. A creature served the same order twice in a minute resists it more. The better of your Insight and Resolve lowers its chance to resist." (It is shipped with the tags-and-saves work; nothing in it names the class.)
There is no `help pledge`: the verb exists only at the Registrar, at the end of the quest.

## 14. Chronicle, Renown and the website
- **Chronicle.** Taking the class reports **`class.hidden`**, subject `bureaucrat`, never `class.advanced` (hidden-classes.md section 7, item 7). It scores as a routine event: counted, no entry, no rumour, no Renown. A refusal reports nothing. Wizards make no history. So there is no "First Clerk of the Chancery".
- **Renown:** none. The class does not change a Renown title. The quest awards no Renown.
- **The website:** `class.hidden` is in `hiddenEventPrefixes` of `publicfilter.json`; the Heroes page shows a character's class only if it is in the new `publicClasses` list, which never names `bureaucrat`; the roster copy written by `deploy/setup-website.sh` must not hold the id (hidden-classes.md section 9). The `/classes` page (copy in `web/content/world.json`) does not list it. This document is public in the repository; the owner accepts that.
- **Lint (hidden-classes.md section 10), a finding:** `bureaucrat` appears as the stem of the word "bureaucratic" in two public strings today (`mudlib/factions/assembly.cs` and `mudlib/domains/greenvale/oakenfell/guild/npc/envoy_assembly.cs`). The lint check must match whole words, not substrings, or it fails on text that has nothing to do with the class.
- **Rumours:** none.

## 15. Tests
**`tests/bureaucrat_test.py`** (the driver's test client, in the style of `tests/class_test.py`; the summons is served with the wizard `hidden grant` and the roll left closed).
- **The class file:** resources and formulas at levels 5 and 10 against section 3's table; skills by level (nothing at 4, Forms at 5, Restraining Order at 6, and so on), the "You have learned..." line, and a character who takes the class at level 8 holding four skills at once.
- **The summons text:** the exact text of 2.2 with serial, date and name; no forbidden word (class, path, seal, bureaucrat, clerk of the chancery); `read` and `look` as specified; the bound lines.
- **The door:** the without-summons texts of 2.3 are byte for byte what City Hall printed before the change (capture them once from the unchanged file and store them in the test); `in`, `enter`, `knock`, `open door` print "What?"; with the summons they work; a decliner and a released character get "What?".
- **The quest, one character, the whole case, in order:** knock, the Registrar's entry line, `quests` shows "Jury Duty" and the progress note; `oath` before a ticket (refused), `take number`, `oath`, the badge, the east door (refused before the badge, open with it); `ask pruett`, `ask tilda` before the order (the interruption), `restrain pruett`, `ask tilda about goose`, `desist tilda` in time and out of time and with no window; `search pigeonholes`, the goose takes the Roll, `take roll` refused, `triplicate goose`, `take roll`; `ask tilda` first and `search table` (the chit), the chit before Tilda has spoken (not found); `submit roll`, `submit chit`, `submit` a stranger item; the verdict `y`, `n`, a wrong line twice; the return, the badge, the fee and experience, the closing lines for all six verdict and exhibit combinations; the offer.
- **The ending:** `pledge` twice within a minute (the class, the seal, the Chronicle count unchanged, `score`), after a minute, with no offer (What?), below level 5 impossible, with an ordinary advanced class taken meanwhile (the release line and the fee); `decline` twice (the certificate, the door closed for good, the register shows declined, no event); neither (the offer persists across a logout and a restart).
- **Resuming:** quit at every stage and return; the notes, the roll, the badge and the chit are as left; the goose does not take the Roll twice.
- **Concurrency:** two jurors in the court at once, each with their own notes.
- **Cannot be abandoned:** `abandon jury-duty` is refused with the journal's own message.
- **Leaks:** nothing in the Hall, Maude, Barnaby or any hallmaster answers `ask` about the class's words; `help bureaucrat` as a non-holder; the exits line of City Hall; `search` in City Hall.
- **Every spell:** Preparation, Recovery and Focus cost at ranks I to V against the tables of section 6; Focus Efficiency; the save outcomes with fixed dice (`SetTestRolls`) for ordinary, above-level, elite and boss targets with the exact lines; each spell's own rules as in the first spec (Forms: the factor and the x2.0 cap; Restraining Order: a rat waits, a ranged action is used, a second enemy is attacked, the lapse on a staff blow and not on a bolt; Cease and Desist: the refusal when nothing is prepared, the cancel, the bar, the Half's push-back; Red Tape: the summon is delayed, the flag not spent; Audit: the lines, the mark consumed by the first order, the out-of-combat search bonus; Application Denied: one charge, rank III two, before the dodge; Per My Last Letter: the stack and its reset; Stamp, Take a Number, Summons (a creature behind a closed door is not summoned; a boss never), Due Process); the repeat window and the flood guard across spells; a sandbox creature cannot be served; nothing is saved.
- **The trial orders** are separate from the spells: `restrain`, `desist`, `triplicate` in the court cost no Focus and need no skill; outside the court, "What?"; a Bureaucrat in the court gets their own spell.
**`tests/class_test.py`:** the level-gated skill grant; the trainer's advanced class; the `hidden` requirement kind. **`tests/lint_area.py`:** the new rooms and NPCs (the usual checks), the tag lines of the new items, Check 17 including the whole-word rule. **`tests/sandbox_test.py`:** a wizard's object cannot call `BarAction`, `RestrainMelee`, `GrantWard`, `InterruptPreparation` or `HoldUntilStruck`, or any register method. **`tests/balance_sim.py`:** section 8.3 and 10; `check` compares the class numbers and the spells' Focus costs and durations with the files.

## 16. Build order
Each stage is shippable; nothing changes for players before Stage 5.
1. **Tags and saves, Stages 1 to 4** (tags-and-saves.md section 17): tags, reach, wording, the control daemon.
2. **The shared hidden-class engine** (hidden-classes.md section 14, stage 1), and this spec's engine pieces: 7.1 (a skill at a level), 7.2 (the trainer), the 7.3 hooks, 7.7 (`ProgressNote`, `submit`); tests of each with a fixture class and fixture creatures. No content, nothing visible.
3. **The class, the place and the people, with the roll closed:** `bureaucrat.cs`, the Chancery rooms, the five people, the items, the summons text, the quest, Forms in Triplicate and the trial verbs, the Overseer-file edits (7.5), `help`. The owner plays it through with `hidden grant`; the standard checks of section 10 at levels 5 to 6.
4. **The other five launch spells** (Restraining Order, Cease and Desist, Red Tape, Audit, Application Denied) with their engine bits; the simulator's full table; the numbers of 8.3 and 10 settled by the owner.
5. **The roll opens** (hidden-classes.md section 14, stage 3), **not before Stage 4**: the quest shows the class's orders, and a Mage should not be offered a class whose spells are missing.
6. **Later (not built at launch):** Per My Last Letter, Stamp of Approval, Take a Number, Summons, Due Process, as the owner asks and the simulator allows; the Adventurer parent (2.6).

## Out of scope / future
- The other four hidden classes (hidden-classes.md section 12 is a sketch); the other ten advanced classes of docs/CLASS_SYSTEM.md section 9, and the Hall's open rite, which stays "not yet open" for them.
- Subclasses, prestige classes, a second advanced class, un-pledging, a second summons.
- A party system (Stamp of Approval and the queue are written to need none).
- Allies and pets of a Bureaucrat; a "Notary" prestige path.
- A guild (the `chancery` guild id is recorded; no guild content).
- More Chancery quests (a Chronicle-worthy errand; a good later hook is the Assembly and the boundary rolls in City Hall). Case variety for the same summons: one case only.
- Rank unlocks beyond level and gold; thematic prices.
- Creatures that use these spells on players (a "Bureaucrat" monster).
- The Mental Resistance's other users (fear, charm); the tags that would let a spell read a creature's nature.
- An NPC outside the Chancery who knows of it (a rumour of "a clerk's seal"): an owner-chosen later decision.
