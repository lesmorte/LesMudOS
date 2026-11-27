# The Bureaucrat: a control caster of the Chancery of Oakenfell (proposed 27 November 2026)

Owner's idea (27 November 2026): "a funny class 'Bureaucrat', it would be a caster class and it would cast things like 'forms in triplicate' (which is basically a slow spell) or 'restraining order' (can't get within melee range, ranged weapons only)". His adviser proposed, and he did not object: an advanced class pledged at a chancery or hall of records in Oakenfell, open to Mages and perhaps Adventurers, with an in-world title (Clerk of the Chancery / Notary) that players call the Bureaucrat; a control caster with low Vitality and weak direct damage, Focus as its resource, Insight and Resolve as its attributes. The humour comes from clerks treating a goblin ambush as a filing matter, and nobody in the fiction thinks it is funny.

This is built on [tags-and-saves.md](tags-and-saves.md): it needs the `melee` and `ranged` tags (Part A) and the saving-throw rule (Part B: "above the law" for bosses, "ignores your authority" for higher level). Every number is a starting value for `tests/balance_sim.py` and play-testing; nothing here has been run in the game. "Today" means the files in the repository on 27 November 2026.


> **Owner's decisions, 27 Nov 2026 (after this spec was written; they override what follows where they differ):**
> 1. **This is a HIDDEN class.** It has no obvious trainer or pledger: nothing in the Hall of Legends, no banner, no hallmaster's remark and no help page points to it, and the website never lists it. It is found. How it is found is not yet designed: decision 4 (the Chancery behind City Hall's inner door, with a Registrar who offers the pledge) and decision 5 must be reworked before this is built.
> 2. **Each path gets one hidden class.** The Bureaucrat is the Mage's. The other four are not designed.
> 3. **The Overseer is not this class's fight** ("A bureaucrat probably isn't fighting the Overseer anyway"): the class starts at level 5 in Oakenfell, so the standard Overseer test is not its balance yardstick. A yardstick for levels 5 to 10 is needed before the spells are tuned.
> 4. Tags and saves (docs/specs/tags-and-saves.md) are being built first.

## Decisions for the owner
Each has a recommendation and the alternative. The spec is written as if you accept every recommendation.

1. **An advanced class, taken at level 5 at a new place in Oakenfell, built before the other fourteen.** Recommendation: yes. It needs one class file, a `pledge` verb on a small shared base (so the other fourteen reuse it), one small change to the trainer, one change that lets a class give a skill at a level, and a room and a person (section 2). Alternative: **a sixth base calling**, chosen at character creation. It needs a starting kit, a village trainer and a balance of levels 1 to 5 in the Hollow where there is almost nothing for a control caster to control; it is listed in section 2.3 with its costs and I do not recommend it.
2. **Open to Mages only at launch.** The class's resource numbers replace the Mage's, and the Mage's Focus Efficiency (spells cost 20% less) comes with the Mage. An Adventurer who pledged would have no such discount and a smaller Focus pool; widening it needs one more requirement and a small trait (section 2.4). Recommendation: Mage only now, Adventurer a later decision. Alternative: both now.
3. **The in-world name is "Clerk of the Chancery"; "Bureaucrat" is the word players use** (the class id, `help bureaucrat`, `ask registrar about bureaucrat`, the line under the name on the banner). `score` and the Chronicle say "Clerk of the Chancery". Alternative: name it "Bureaucrat" everywhere (the joke is then on the surface, which is against "nobody in the fiction thinks it is funny").
4. **The place is City Hall's inner door**, which already exists in the description and leads nowhere: behind it, a new room, **The Chancery**, with a new person, **Marian Ashcombe, Registrar of the Chancery**. Alternative: a new building off Founder's Square. Recommendation: the inner door; it is a real place, it is in the Founder's District (the Assembly's), and it costs one room.
5. **The pledge is made at the Chancery, not at the Hall of Legends.** Level 5, a Mage, 25 gold, the two-step confirmation of docs/CLASS_SYSTEM.md section 8, a ceremony the room sees. The Hall's hallmasters know of it and say so. Alternative: through Ilsabet, which needs the Hall's rite to be opened for everyone first.
6. **Resources: Vitality 6 + 7 a level, Endurance 14 + 1 a level, Focus 76 + 4 a level**, replacing the Mage's (4 + 7, 14 + 1, 71 + 4). It is a Mage's body with a few more Focus: low Vitality, nothing for fighting. Alternative: name no numbers (the Mage's stand): five Focus fewer.
7. **No new weapon kind and never a ranged weapon.** The Bureaucrat wields what Mages wield (dagger, staff). Two small items carry the fiction: **the Clerk's Staff of Office** (a staff; `staff`, `blunt`, `melee`) and **the Notary's Seal** (a trinket, +2 Authority). A quill is not a weapon. Recommendation: this. It also removes the owner's worry about "restrain, then plink with a bow": a Mage cannot wield a bow.
8. **Eleven spells: six at launch (levels 5 to 10), five later (12 to 20).** Launch: Forms in Triplicate, Restraining Order, Cease and Desist, Red Tape, Audit, Application Denied. Later: Per My Last Letter, Stamp of Approval, Take a Number, Summons, Due Process. Recommendation: this order; the class keeps the Mage's Arcane Bolt as its damage until Per My Last Letter at 12.
9. **Restraining Order bars melee attacks against the caster only** (section 6.2). A restrained creature uses a ranged action if it has one, else attacks someone else in the room, else waits; it is never made to do nothing for longer than the order. The order lapses at once if the caster strikes the target in melee.
10. **Audit does not give a to-hit bonus** (the adviser's list had one): the Bureaucrat makes almost no attack rolls, so the bonus would be a dead word. It gives a save penalty on the next order served on the target instead (section 6.5). Alternative: the bonus as written, useful only to a Bureaucrat who also swings a staff.
11. **Prices and ranks are the existing ladder**: II 40, III 150, IV 400, V 900 gold, rank r at level 5 x (r - 1), all from the Registrar; no unlock beyond level and gold at launch. Alternative: an unlock on rank II (kills of a kind, as Arcane Bolt II asks ten goblins); candidates are in section 5.
12. **Ally spells (Stamp of Approval, Application Denied) work on any player in the room.** There is no party system. Recommendation: yes; a party system later changes nothing in them.
13. **The Overseer stays a boss ("above the law")**: nothing here makes him easier than the Mage is today. The class's gifts against him are small by design (section 8) and the standard test must stay between 72% and 83%.
14. **Small engine changes**, each a few lines and each listed in section 7: classes can give a skill at a level; the trainer teaches the advanced class's skills; a creature's actions can carry a bar, a restraint or a ward; Cease and Desist can interrupt a preparation without the stagger's resistance roll.

## 1. What the owner and CLASS_SYSTEM.md already promise
Read of docs/CLASS_SYSTEM.md (sections 1, 2, 6, 8, 9, 12, 13) and docs/specs/skill-ranks.md; what exists and what is promised but not built:

| Already there | Not built |
|---|---|
| A class is data: `/classes/<id>.cs` inheriting `/std/pclass`, with `Tier = "advanced"`, `Parent`, `Require("level", "5")` and kinds `level`, `quest`, `item`, `class`, the skills and traits it grants, the equipment access it adds, its own resource numbers, `ExpModifier`, `AttackRate`, `Guild`, `Modifiers`. The registry `/secure/classes` finds a new file without a restart. | **No advanced class file exists**: `/classes` holds only the five callings. The fifteen are described (section 9 of CLASS_SYSTEM.md) and the Hall's banners name them. |
| `Adventurer.ApplyClass(id)`: gives an advanced, subclass or prestige class; only another object (not a player's own command) may call it; checks the class exists and is enabled, the tier is free, the parent is held and every requirement; returns null or a sentence; then refreshes classes, binds skill commands and tells the player "You are now a {title}." The class is saved as an id (`advancedClass`) and nothing derived. | **The `pledge <class>` verb**: the two-step choice within a minute, the ceremony the room sees, the refusals below level 5 and to a character who already has an advanced class. The hallmasters say "the rite of pledging is not yet open". The only `pledge` in the repository is a test fixture in `tests/class_test.py`. |
| The Hall of Callings, with five hallmasters (Gorrim, Veyla, Anselm, Ilsabet, Jory), the readable wall of fifteen banners, and the rank trainers for ranks III to V. | A **level for each skill**: a class grants every skill it lists the moment it is taken. |
| `score` shows "Class {title}" for an advanced class; the Chronicle has the events `class.advanced` ("First {class}", a server first) and `class.prestige`. Skill ranks: "advanced-class skills start at rank I when the class is taken." | Nothing reports `class.advanced` when `ApplyClass` runs. The trainer answers by the **archetype** only (`QueryArchetype`), so it cannot teach an advanced class's ranks. |
| `WornBy` and `NotWornBy` already count a character's advanced class id as well as the calling it grew from. | Fifteen classes' skills ("full skill trees come later"; CLASS_SYSTEM.md decision 1 promised **two signature abilities** per class). |

The Bureaucrat is therefore the first advanced class and the first with a full list of skills: eleven, where the plan promised two. That is a decision the owner takes by accepting this spec; the other fourteen can still be built with two.

**What "pledging" needs, precisely.** (a) A class file (done by this spec). (b) A verb that, with a two-step confirmation inside a minute and a fee, calls `ApplyClass(id)` from an NPC (new, section 2.2). (c) A person and a place (new). (d) A report of `class.advanced` to the Chronicle (new, three lines). (e) The trainer teaching the advanced class's ranks (a change in `/std/trainer`, section 7). (f) A way for a class to give a skill at a level (section 7). (g) Text in the Hall: the Mage hallmaster's "paths" answer and the banners (section 9). Nothing else: the other fourteen classes need (a) each, and (b) to (g) are shared.

## 2. How it sits in the class plan

### 2.1 The class, in the Chancery's own words
- **Name** (`ClassName`, `Title`): "Clerk of the Chancery". **Id**: `bureaucrat`. **Tier**: `advanced`. **Parent**: `mage`. **Guild**: `chancery`. **Experience modifier** 1.0. **Tagline**: "The Bureaucrat: nothing may happen until the correct form has been lodged."
- **Requirements:** `Require("level", "5")`, `Require("class", "mage")`. (The parent check of `ApplyClass` already demands the Mage; the second line gives a clear refusal text.) The fee (25 gold) is taken by the Registrar, not by the class.
- **Description** (the owner's text, kept word for word, for `help bureaucrat` and the Registrar):

> The Chancery of Oakenfell has never lost a war, because it has never agreed that one was properly declared. Its clerks carry no blade worth the name. They carry seals, ledgers and the absolute conviction that nothing may happen until the correct form has been lodged. Creatures that cannot read are bound all the same; ignorance of the procedure is no defence.

  The class file's `Description` adds the plain facts: "A Clerk of the Chancery is a Mage who has taken the Chancery's seal. Control, not damage: it slows, bars, interrupts and delays, and leans on Insight and Resolve. Weapons: dagger, staff. Armor: light."

### 2.2 The place and the people
**The Chancery** (`/domains/greenvale/oakenfell/founders/chancery.cs`, a `Safe` room; area `Oakenfell`, region `Greenvale`; reached by a new exit `in` from City Hall, `/domains/greenvale/oakenfell/founders/city_hall.cs`, which gets `Exits["in"]`, a detail line for the exit, and its existing details for the inner door: "the carved letters over the inner door" now lead somewhere). City Hall's existing text already has the counter, the pigeonholes, the queue and the motto "FROM STRONG ROOTS GROW GREAT THINGS" over the inner door.

> **The Chancery of Oakenfell.** A narrow, windowless room, lit by a row of green-shaded lamps and very quiet. A long table of black oak runs its whole length, bare but for an inkwell, a sand-shaker, a stick of red wax and a book as large as a door, chained to its lectern: the Book of Proceedings. Behind the table, floor to ceiling, climb pigeonholes, each with a rolled paper and a brass number; the numbers do not run in order, and the order they do run in has never been explained. The air smells of ink, hot wax and old paper, and the only sound is a pen. A small brass plate by the door reads NOTHING IS DONE UNTIL IT IS ENTERED.

Details: the Book of Proceedings (`read book`: "Entries in a dozen hands. The latest, dated this morning: 'Goblins, Vincent's Hollow: boundary dispute, referred.'"), the pigeonholes, the seal and wax, the plate (readable: the motto; below it, smaller: "Fees are posted. The clerks are not to be hurried."), the lamps.

**Marian Ashcombe, Registrar of the Chancery** (`/domains/greenvale/oakenfell/founders/npc/registrar_ashcombe.cs`, inherits `/std/pledger`, which inherits `/std/trainer`): "A tall, unhurried woman in a grey coat with ink to the second knuckle and a brass seal on a cord at her throat. She is writing, and does not look up until she has finished the line." Level 20. Her lines are in section 11. She is also the rank trainer: `TeachCalling("bureaucrat", 2, 5)`.

**`/std/pledger`** (new, small; shared by the other fourteen): a trainer that offers `AddPledge("bureaucrat", fee: 25)`. It binds the verb `pledge` (a protected verb to the NPC: it refuses a sandbox caller and a non-player), and:
1. `pledge` with no word: lists the classes it offers and what each needs.
2. `pledge bureaucrat` (first time): checks the class (`QueryEnabled`, tier, parent, `CheckRequirements`, gold) **without changing anything** and, if all hold, says the **permanence line** and the fee, and records the offer for this player for 60 seconds. If not, the refusal (section 11).
3. `pledge bureaucrat` again within 60 seconds: takes the fee, calls `player.Call("ApplyClass", id)` (the NPC is "another object", as `ApplyClass` demands), tells the room the ceremony, reports `class.advanced` to `/secure/events` with the subject `bureaucrat` and `names["class"]` = "Clerk of the Chancery" (the NPC is a trusted tree; wizards are dropped by the event service as always), and says the closing line. If `ApplyClass` refuses after the fee was taken, the fee is returned in the same call.
4. A character who already has an advanced class: "You have already taken a seal." A wizard in the workshop cannot be pledged (the class is applied to a checkpointed body and lost on leaving: unchanged behaviour).

### 2.3 The alternative: a sixth base calling
A sixth archetype, "Clerk", chosen at character creation: needs a `/classes/clerk.cs` with `Tier = "archetype"`, `SortOrder = 6`, starting attributes (Insight and Resolve), kit, a rank II trainer in Vincent's Hollow (the Hollow's `TeachCalling`), a place in the creation list and `help`, the Hall's banner and a sixth hallmaster, a new simulator column, and a whole play-through (`tests/playthrough_test.py` runs one per archetype); and **a balance of levels 1 to 5 in the Hollow**, where a character with a slow, a bar and an interrupt but no damage must kill goblins alone. It also makes the Mage, Priest and Adventurer less distinct from it: the Mage's whole identity is Focus and spells. It is two to three times the work of 2.1 and it turns away the "advanced class" idea. **Not recommended.** If the owner prefers it, sections 4 to 9 stand with level 1 in place of level 5 and the levels of 6.1 shifted down by four.

### 2.4 Mages and Adventurers
At launch `Parent = "mage"`. To open it to Adventurers: remove `Parent` (an advanced class with no parent skips that test in `ApplyClass`), override `CheckRequirement` in the class file for a new kind `calling` taking "mage,adventurer" (the file may override it; `pclass.cs` needs no change), and give the class a small trait (a "Clerical Efficiency", spells cost 20% less) for those without Focus Efficiency, or accept that an Adventurer pays 25% more. The resource numbers of 2.1 replace the Adventurer's (Vitality 6 + 7 a level against 10 + 8: a thin price for their Focus), which is why this is a later decision.

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
- **The Clerk's Staff of Office** (`/domains/greenvale/oakenfell/founders/obj/clerks_staff.cs`; sold by the Registrar for 45 gold): a black-ash staff capped with a brass seal-head. `Category = "staff"`; tags `staff`, `blunt`, `melee`, `wood`; Damage 5-9, to hit 0, Reaction modifier 0.5, Recovery 1.0 (the Apprentice's Staff is 7-11, +1; the Clerk's is lighter in the hand and weaker, on purpose); `AttributeBonus["resolve"] = 1`; `WornBy.Add("bureaucrat")`. Description: "A staff of black ash, shod in brass, its head a flat seal-die. It is better for pressing wax than for hitting, and the Chancery does not recommend using it for anything else." (`Magic`: "the seal-head carries the Chancery's mark: +1 Resolve while wielded".)
- **The Notary's Seal** (`.../founders/obj/notarys_seal.cs`; 60 gold): a trinket on a cord; tags `trinket`, `steel`; `StatBonus["authority"] = 2`. "A brass seal-die with a plain wooden grip. What it presses becomes official, and a good deal of what it presses becomes binding."
- **No quill, no ledger as an item with a skill.** The ledger and the quill are in the words (the wind-up lines of the spells), not in an item slot. A "held focus" slot does not exist and the spec adds none.

## 5. The spells in one table
Prices for every rank: **II 40, III 150, IV 400, V 900 gold**, all from the Registrar (`TeachCalling("bureaucrat", 2, 5)`; `RankDaemon.RankPrices`); rank r needs level 5 x (r - 1) (rank II at 5, III at 10, IV at 15, V at 20); a spell starts at rank I when it is learned; the command word never changes. A spell is learned **when the character reaches its level** (section 7) or, at the pledge, the level-5 one. No unlock beyond level and gold at launch; candidates if the owner wants one: Forms in Triplicate II asks "kills goblin 10" (as Arcane Bolt II does), Cease and Desist III asks "kills_with desist 10", Application Denied II asks the quest "the-quiet-offering".

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
*Learned at the pledge.* Wind-up (the room reads): "{Name} draws three sheets of paper and a carbon from a satchel..."

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
`/std/pclass.cs`: `protected void SkillAt(string skillId, int level)` and `public int QuerySkillLevel(string skillId)` (0 for a skill the class gives at once, as every class does now). `/std/adventurer.cs` `RefreshClasses` grants only the skills whose level is at most the character's level; `CheckLevelUp` calls `RefreshClasses()` and `BindSkillCommands()` after a level is gained and tells the player: "You have learned {Title}: {cmd}{command}{/}." (the existing level-up message gains the line). Existing classes name no level, so nothing changes for them. Test: a test class with a skill at level 3.

### 7.2 The trainer teaches an advanced class's skills
`/std/trainer.cs` `CallingOf`: the calling a trainer answers a player as is, in order, the player's **advanced class** if the trainer teaches it, else the archetype (so a Bureaucrat still buys Arcane Bolt ranks from the Mage hallmaster and Bureaucrat ranks from the Registrar). `OffersFor` already reads `ClassFile(c).QuerySkills()` for any class id; `ranks` shows locked skills as "level 12" (a skill the class has not yet given).

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
`goblin_overseer.cs`: `if (!summoned && Vitality * 2 <= QueryMaxVitality() && !QueryActionBarred("summon"))` (the flag set only when it really happens); `bandit_leader.cs`, `warren_broodmother.cs`: the same for their summons. A creature that does not summon is unchanged.

### 7.6 Everything else
| File | Change |
|---|---|
| `mudlib/classes/bureaucrat.cs` (new) | the class (2.1, 3): `SkillAt` for the eleven skills |
| `mudlib/skills/triplicate.cs`, `restraining_order.cs` (command `restrain`), `cease_and_desist.cs`, `red_tape.cs`, `audit.cs`, `application_denied.cs`, and later `per_my_last_letter.cs`, `stamp_of_approval.cs`, `take_a_number.cs`, `summons.cs`, `due_process.cs` (new) | one file per skill, inheriting `/std/skill`, rank tables as `RankRow` lines (section 6) |
| `mudlib/std/pledger.cs` (new) | the shared pledge (2.2) |
| `mudlib/domains/greenvale/oakenfell/founders/chancery.cs`, `npc/registrar_ashcombe.cs`, `obj/clerks_staff.cs`, `obj/notarys_seal.cs` (new); `city_hall.cs` (an exit and one detail line) | the place and people |
| `mudlib/domains/greenvale/oakenfell/guild/npc/hallmaster_mage.cs`, `hall_clerk.cs`, `founders/npc/clerk_maude.cs` | topics (section 11) |
| `mudlib/secure/control.cs`, `tags.cs`, `blows.cs` | tags-and-saves.md |
| `mudlib/std/living.cs`, `adventurer.cs`, `pclass.cs`, `trainer.cs` | 7.1 to 7.3 |
| `tests/balance_sim.py`, `tests/class_test.py`, `tests/bureaucrat_test.py` (new), `tests/lint_area.py` | section 14 |
| `docs/CLASS_SYSTEM.md` (section 9, a seventh path for the Mage; section 12, `SkillAt`; "pledge"), `docs/BUILDING.md` (a short "A class gives a skill at a level" and "A pledging NPC"), `docs/specs/skill-ranks.md` (the Registrar) | one paragraph each |

## 8. Balance: why it stays in the same band
The standard test (docs/BALANCE_HUMAN.md and `tests/balance_sim.py`): a level 5 character in starting gear, 4 potions, points placed, the middle human, RestFactor 5, the Overseer from the cellar entrance. Today the five callings are 81 / 71 / 84 / 73 / 74 (mean about 77; the owner's band is **72% to 83%**). Nothing here has been measured; what follows is the argument and the list of what the simulator must check.

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
- **A boss gives half or nothing** (the standard test is the Overseer, a boss: every control spell is Half 95% of the time at level 5, None 5%). The Forms in Triplicate at Half slows him for 10 seconds, saving about 1.8 Vitality (his Axe Slash cycle is 10 s, the Power Strike 12 s; his damage rate is about 0.61 a second): worth about a third of one bolt. A Bureaucrat at level 5 in the standard test is a Mage with five more Focus and two more Vitality and one spell that is a bad trade against a boss: **expected 73% to 78%** (the Mage is 73%).
- **The repeat and flood rules** (a slow at most 38% of the time; an order 21%; against a boss 17% and 8%).
- **Everything costs Focus at 24 to 32** (the bolt costs 16 to 20), and a long fight is Focus-limited.
- **No ranged weapon, ever.** Weapons: dagger and staff.

### 8.3 What `tests/balance_sim.py` must model
- The class: Vitality 6 + 7, Endurance 14 + 1, Focus 76 + 4, the Mage's attributes and placement (Insight, then Resolve), Focus Efficiency, the Clerk's Staff and the Notary's Seal as options.
- The control module of tags-and-saves.md section 18 with each spell's strength, half time and repeat steps; Authority from placement and rank.
- Each spell as an action on the combat clock with its Preparation, Recovery and Focus cost: slow as a factor on a creature's Reaction Time (and the x2.0 cap); the restraint as the substitution of 6.2 (another enemy, a declared ranged action, or a Reaction-Time wait) for creatures with the real actions of the files; a bar; the ward (refuse before dodge); the interrupt of a preparation as the engine does it (which depends on the creature's real Preparation times: the Overseer's Power Strike 1.0 = 2 s, the Elite Guard's Shield Bash 0.5 = 1 s, so a human who types within 1.5 to 3 seconds misses the second and often the first); Per My Last Letter's stack; Stamp's discount; the queue; the summons; the hold.
- The Overseer's pattern (Battle Cry, Axe Slash, Axe Slash, Power Strike; Summon once at half Vitality) with Red Tape's bar and the `summoned` flag fix.
- The human model (docs/BALANCE_HUMAN.md): a delay before an interrupt, skipped casts, a late potion; a Bureaucrat's policy ("cast Forms on a creature that is not a boss and has more than half its life; Cease and Desist on a telegraphed action when ready; Audit before the first order on a creature that is above your level; Restrain when two or more are present or when a ranged action is absent").
- Table: the standard test at level 5 (only Forms in Triplicate and the Mage's kit: **target 72% to 83%, expected 73% to 78%**); the same character at level 6 to 10 against: the Goblin Elite Guards in the Overseer Hall (an elite: S +15), the Overseer with all launch spells, three corridor groups (a Goblin Raider with a Scout, Boar Brute and Wild Boar), and the five level 10 bosses; each calling's own numbers beside it. **The gap between the Bureaucrat and the other callings at levels 5 and 10 must not be wider than the gap between the callings is today**, as skill-ranks.md requires of every calling.
- A solo "loop" test: restrain, bolt, restrain, bolt, to check the section 8.1 figures (a 38-second fight with two orders should show no gain).

## 9. Groups: the party system does not exist
Application Denied and Stamp of Approval use `Target = "friend"`, which is the caster or **any player character in the room** (as Blessing). They need no party and no consent (a friend cannot refuse a blessing today either). Take a Number protects whoever the creatures fight. Restraining Order shields only the caster (the order is the caster's) and Audit marks for the caster's own next order. When a party system arrives: Stamp of Approval may be limited to party members; nothing else changes. A second Bureaucrat in the room is allowed: orders from two casters on one creature are separate on the creature but the daemon's repeat and flood records are per target (tags-and-saves.md section 14), so the pair gets Full once.

## 10. The Overseer fight specifically
He is a **boss** (`Boss = true`, so the tag `boss`): "above the law". Level 5, Vitality 114, armour 3, Reaction 4.0; the actions in the file are Battle Cry first (+2 damage for 30 s), then Axe Slash, Axe Slash, Power Strike (1.0 preparation, announced "draws his axe far back", damage x2), and once, as his next action when his Vitality first falls to half, Summon Goblin Minion.
- **At level 5 (the standard test):** one control spell, Forms in Triplicate: Half (10 s, x1.5) 95% of the time, None 5% (S = 4% - Authority 9, floor 5%). It saves about 1.8 Vitality a cast against a bolt's 4.9: **the class plays like a Mage here** and the band is not touched.
- **At level 6 to 9, with the launch spells:** **Restraining Order**: Half (5 s) 95%: he waits one Reaction Time (his own, 8 s at the slow end) instead of striking: three seconds saved, once a minute. **Cease and Desist**: his Power Strike is a 2-second wind-up; an interrupt cancels a 15-point blow (x2 of 7.5 after the cry) nineteen times in twenty (Half): the one strong answer, and one that needs the caster ready at that moment (Recovery 1.5 after a spell of 8 seconds: the best moment is the cast after the second Slash). **Red Tape**: Half (10 s) holds back the summons: his minion comes ten seconds later than it would. **Audit**: shows the numbers (and "above the law"). **Application Denied (level 10)**: refuses the Power Strike, but he often slashes first and the ward takes that.
- **His summons and his minions:** a goblin minion (level 2) is an ordinary creature: Restraining Order and Forms land in full 95%. A Bureaucrat who puts a Restraining Order on the minion and a Cease and Desist on the next Power Strike has fought the Overseer on his terms and the Chancery's. It is a nicer fight for the class and it is not an easier one.
- **The numbers to check:** the Bureaucrat at levels 6 to 9 against him should not exceed the Mage by more than 8 points with the launch spells, since the boss rule halves everything and the ordinary rule gives the minion's side to the caster.

## 11. People, in voice
**Marian Ashcombe, Registrar of the Chancery** (Level 20, Vitality 90, a trainer, a pledger). `AddChat` (spoken in turn): "The Chancery is open. Kindly state your business, in writing if you can." / "A matter not entered is a matter that has not occurred. I say this to everyone. It is generally the first thing they dispute." / "Please form a queue. There is no queue. Please form one anyway."

Topics (`AddTopic`), in her mouth:
- *chancery, this place:* "The Chancery of Oakenfell keeps the Assembly's papers and the city's peace, in that order, and the one has never once required the other. We have never lost a war. We have never agreed that one was properly declared."
- *bureaucrat, clerk, clerks, class, path, seal:* "A Clerk of the Chancery is a Mage who has taken the seal. The common word is Bureaucrat, and I do not correct it, because to correct it would be to open a file. A clerk slows what must wait, bars what must not approach, stays what must not be said, and serves notice on what cannot read. It is not a calling for the impatient."
- *pledge, take the seal, join, apply:* "To take the seal, one must be a Mage and of the fifth level, and pay twenty-five gold, entered. The seal is not a hat. It is for life. Everything you are, you keep: the staff, the Bolt, the Lore. If you are sure, say {cmd}pledge bureaucrat{/}."
- *spells, forms, triplicate, restraining order, desist, tape, audit:* one line each. *Forms in Triplicate:* "Form 27-B. Three copies. The creature keeps none and wishes it understood why." *Restraining Order:* "It may not come within reach of you. It may do other things. The order is quite specific." *Cease and Desist:* "Served at the moment it begins, which is the only moment it may be served." *Red Tape:* "A creature may not call for help without a countersignature. I need not tell you who countersigns." *Audit:* "Nothing is so useful as knowing precisely what one is dealing with. Nothing is so unpopular."
- *boss, above the law, ignores your authority:* "There are things in Hollowmere that are above the law, and they are almost always the things one most wishes to serve. Half the order will hold; sometimes none. There are also things that ignore your authority, as there are in any office: they are only above your grade. They are served more often than you would think. A senior clerk is a good deal harder to ignore."
- *hall, hallmaster, ilsabet:* "The Hall of Legends is very good to us, and very correct. Ilsabet is a stickler, and I mean that as the highest praise."
- *fee, gold, price:* "Twenty-five, entered. Ranks are forty, a hundred and fifty, four hundred and nine hundred, entered separately. Fees are posted."
- *oath:* the oath is not told until it is sworn (it is a surprise in the pledge).
Refusals: below level 5: "The Chancery does not take clerks of less than five levels' standing. Return when you have some. Nobody has ever been refused twice for the same reason." Not a Mage: "The Chancery's clerks are drawn from the Mages. (For now. There is a form.)" Already has a class: "You have taken a seal already. The Chancery will not duplicate it." Too poor: "The fee is twenty-five gold. The Chancery does not extend credit; it extends time." Not at the Chancery: n/a.

**The permanence line** (first `pledge`): "Marian Ashcombe sets down her pen. 'I will say this once, as the Chancery requires. The seal is for life. You will keep all you are: the staff, the Bolt, the Lore. You will gain the seal, and the forms that go with it, and you will not be able to put either down. The fee is twenty-five gold, entered. If you are sure, say {cmd}pledge bureaucrat{/} again within the minute.'"

**The ceremony** (second `pledge`): you read: "Marian Ashcombe rises. 'Raise your right hand. The other one. Repeat after me. I will lodge before I act. I will bind nothing that has not been served. I will not, upon any provocation, agree that a war was properly declared.' She takes the seal from the cord at her throat, presses it into a pool of hot wax beside your name in the Book of Proceedings, and blows on it. 'It will be dry in a moment.'" Room: "Marian Ashcombe presses the Chancery's seal into hot wax beside {Name}'s name in the Book of Proceedings. The room is very still." Then the engine's own line "You are now a Clerk of the Chancery." and hers: "Your first form is Form 27-B. It is in triplicate. Please do not lose the carbon."

**Ilsabet (Hallmaster of the Mages), a new topic** (*chancery, bureaucrat, clerk, clerks, seal, form*): "The Chancery? Do not look at me like that. They file things. The filing is, regrettably, effective. If you mean to take their seal you will not do it here; the Hall's rite for the paths is for the Hall's paths. Walk to City Hall, and go through the inner door, and be polite. They remember." Her existing "paths" answer ("Three roads for a Mage") gains: "There is a fourth, outside the banners, and I do not endorse it. It is also not wrong."

**Mabyn (Clerk of the Hall of Legends), a new topic** (*chancery, registrar*): "The Chancery is a good neighbour and a hard correspondent. We are, I am told, in its files. I have never seen the file." **Maude Tarrow (City Hall)**: topic *inner door, chancery, door*: "Through the inner door? That is the Chancery. Mind the queue, and say please. Mind the Registrar; she has never been wrong, because she has never been agreed with."

**Rank trainer** (`ranks`, `train`): "Ranks are bought of me and entered." The refusal lines of `/std/trainer` are used unchanged.

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

### 12.3 The Goblin Overseer (level 5, boss) against a level 8 Bureaucrat
```
> audit overseer
You open your ledger on the Goblin Overseer and make an entry.
  Goblin Overseer: level 5, above the law (a boss).
  Vitality 114 of 114   Armor 3   Defense 4   Attack 7   Damage 4-7
  Resists: mind 4%, body 4%.
  Acts every 10.0 s: Axe Slash; Power Strike every 12.0 s (winds up 2.0 s); Battle Cry; Summon Goblin Minion.
  Your next order against it is served at -6 to its save.
The Goblin Overseer throws back his head and bellows a BATTLE CRY! ...
> triplicate overseer
You serve the Goblin Overseer with Form 27-B, in triplicate.
The Goblin Overseer is above the law. The form half holds.
...
The Goblin Overseer draws his axe far back...
> desist overseer
You serve the Goblin Overseer with a notice to cease and desist. His Power Strike stops mid-motion.
The Goblin Overseer is above the law. The notice half holds.
...
The Goblin Overseer's Vitality falls to half.
> tape overseer
You wind the Goblin Overseer in red tape. It is not going anywhere, and it is not calling anyone.
The Goblin Overseer is above the law. The tape half holds.
The Goblin Overseer draws breath to summon his minions, and is informed that a summons requires a second signature.
```
The minion comes ten seconds later than it would have.

### 12.4 A creature above your level: "ignores your authority"
A level 5 Bureaucrat serves a Boar Brute (level 7: two above; resistance 8%, so a save chance of 8 + 20 - 9 = 19%):
```
> restrain boar
You serve a boar brute with a restraining order.
The boar brute ignores your authority. The order holds for half its term.
```

## 13. Help text
`help bureaucrat`: "BUREAUCRAT (the Clerk of the Chancery). A Mage who has taken the Chancery's seal at the Chancery in Oakenfell, through the inner door of City Hall. Control, not damage: slow, bar, interrupt, delay, and mark. Leans on Insight, then Resolve. Weapons: dagger, staff. Armor: light. You keep every Mage skill. The seal is for life. {cmd}ask registrar about pledge{/}."
`help restrain`: "RESTRAIN (Restraining Order), Bureaucrat level 6. For 10 seconds the creature cannot make a melee attack against you. It may use a ranged attack, or turn on someone else, or wait. Your own spells do not break it; your own staff does. Bosses are above the law and the order half holds; a creature above your level may ignore your authority. See {cmd}help control{/}."
`help control` (new, one paragraph): "Orders, slows, bars and holds are served on creatures. A creature may resist: a creature above your level resists more ('ignores your authority'), and a boss is above the law: for a boss an order holds for half its term, or not at all. A creature served the same order twice in a minute resists it more. The better of your Insight and Resolve lowers its chance to resist."
`help pledge` (a short entry for the verb).

## 14. Chronicle, Renown and the website
- **Chronicle.** The pledge reports the existing event `class.advanced` ("First {class}": a server first, renown 10 to the participant, 100 for the first) with the class name "Clerk of the Chancery"; the first one reads "The First Clerk of the Chancery. On {date}, {name} chose the path of the Clerk of the Chancery and was the first in Hollowmere to walk it." No other Chronicle entry is created. Wizards make no history (the event service drops them).
- **Renown:** none beyond the event. The class does not change a Renown title.
- **The website** `/classes` page: if it lists advanced classes from the class files it lists this one with its tagline; check `src/LesMudOS.Api` when building (it reads the repository's class data or a copy; I did not find an item or class page that lists advanced classes today, and the forbidden-terms scan is unaffected).
- **Rumours:** none.

## 15. Tests
**`tests/bureaucrat_test.py`** (the driver's test client, in the style of `tests/class_test.py`): the class file (resources and formulas at level 5 and 10 against section 3's table; the skills given by level: nothing at 4, Forms at 5, Restraining Order at 6, and so on, with the "You have learned..." line); `pledge`: below level 5, not a Mage, already has a class, too poor, the first `pledge` (nothing changes), the second within 60 seconds (the fee, the class, the Chronicle event, the ceremony lines, the room's line), the second after 60 seconds (the first again), a refused `ApplyClass` returns the fee; the Registrar's ranks (`ranks`, `train triplicate`) and a Bureaucrat still buying Arcane Bolt ranks from Ilsabet; the Chancery, the Staff, the Seal (WornBy; Authority +2; tags in `look`/`appraise`); every spell: Preparation, Recovery and Focus cost at ranks I to V against the tables of section 6; Focus Efficiency applied; the save outcomes with fixed dice (`SetTestRolls`) for an ordinary target, an above-level target, an elite and a boss, with the exact lines; the half durations; each spell's own rules: Forms (the factor, the x2.0 cap), Restraining Order (a rat waits; a creature with a declared ranged action uses it; a second enemy is attacked instead; the lapse on a staff blow and not on a bolt; a Webbed Spit is not barred; a Battle Cry is not barred), Cease and Desist (the refusal when nothing is prepared; the cancel of the Overseer's Power Strike; the bar; the Half's push-back), Red Tape (the summon is delayed, the flag not spent), Audit (the lines; the mark consumed by the first order; the out-of-combat search bonus), Application Denied (one charge refused, the second blow lands; rank III two; before the dodge; a special action not refused), Per My Last Letter (the stack and its reset), Stamp, Take a Number, Summons (a creature behind a closed door is not summoned; a boss never), Due Process (three strikes; the time limit); the repeat window and the flood guard across spells; a sandbox creature cannot be served; nothing is saved; a Mage that is not a Bureaucrat has none of it.
**`tests/class_test.py`:** the level-gated skill grant; the trainer's advanced class. **`tests/lint_area.py`:** the Chancery room and NPC (the usual checks) and the tag lines of the new items. **`tests/sandbox_test.py`:** a wizard's object cannot call `BarAction`, `RestrainMelee`, `GrantWard`, `InterruptPreparation` or `HoldUntilStruck` on a real creature. **`tests/balance_sim.py`:** section 8.3; `check` compares the class numbers and the spells' Focus costs and durations with the files.

## 16. Build order
Each stage is shippable; nothing changes for players before Stage 3.
1. **Tags and saves Stages 1 to 4** (tags-and-saves.md section 17): tags, reach, wording, the control daemon.
2. **Engine and framework:** 7.1 (a skill at a level), 7.2 (the trainer), `/std/pledger`, the 7.3 hooks; tests of each with test classes and creatures in fixtures. No content.
3. **The class, the place and the first spell:** `bureaucrat.cs`, the Chancery, the Registrar, the Staff and Seal, the pledge, Forms in Triplicate, the Overseer-file edits, the hallmaster and clerk topics, `help`. The standard test at level 5.
4. **The other five launch spells** (Restraining Order, Cease and Desist, Red Tape, Audit, Application Denied) with their engine bits; the simulator's full Bureaucrat table; the numbers of 8.3 settled by the owner.
5. **Later (not built at launch):** Per My Last Letter, Stamp of Approval, Take a Number, Summons, Due Process, as the owner asks and the simulator allows; the Adventurer parent (2.4).

## Out of scope / future
- The other fourteen advanced classes (docs/CLASS_SYSTEM.md section 9): only the shared pieces (`SkillAt`, the trainer, `/std/pledger`, the event report) are built here. The hallmasters' "the rite is not yet open" stays true for them.
- Subclasses, prestige classes, a second advanced class, un-pledging.
- A party system (Stamp of Approval and the queue are written to need none).
- Allies and pets of a Bureaucrat; a "Notary" prestige path.
- A guild (the `chancery` guild id is recorded; no guild content).
- Quest content for the Chancery (a Chronicle-worthy errand, a first Chancery quest: a good later hook is the Assembly and the boundary rolls in City Hall).
- Rank unlocks beyond level and gold; thematic prices.
- Effects on players by Chancery creatures; a creature's use of these spells on players (a "Bureaucrat" monster) is not designed.
- The Mental Resistance's other users (fear, charm); the tags that would let a spell read a creature's nature.
