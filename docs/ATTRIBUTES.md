# The Hollowmere attribute system: design and build plan

How the owner's brief ([specs/attributes.md](specs/attributes.md)) becomes part of the game: what changes, the formulas (fitted to the game as it plays today, at the owner's direction), the choices the brief leaves open, and the order of work. Nothing here is built yet.

## 1. What changes

| Today | After |
|---|---|
| A character has a level, a calling and its fixed numbers. There are no attributes. | Six attributes: Might, Grace, Vigor, Insight, Spirit, Resolve. Two points to place at every level. |
| HP, Mana, Stamina | Vitality, Focus, Endurance, in every message, command, help text, document and in the code. |
| Maximum HP is the calling's starting HP plus 8 a level. Mana and Stamina maximums are fixed by the calling (Warrior 100 Stamina and 20 Mana; Mage 30 and 100). | All three come from one formula each: a class base, a class amount per level, and attributes. |
| To hit: d20 + level × the calling's attack rate against 10 + armor + (level − 1). Damage is the weapon's. Critical hits, dodge and block come only from traits. | Precision and Evasion enter the hit roll. Physical Power scales weapon damage. Grace gives a base critical chance. Spell Power and Healing Power scale spells. |
| A calling's differences are fixed numbers. | A calling starts its members with different attributes and applies percentage modifiers after the formulas. |
| Gathering and crafting use proficiency only. | Gathering Efficiency and Crafting Aptitude add to them. |
| Creatures have hand-set HP, damage and armor. | Creatures also have attributes, generated from level and a template, so that resistances and powers work on them as on players. |

## 2. Formulas fitted to the game as it plays today

The owner's brief gave formulas that roughly tripled Vitality and cut regeneration to a few points a minute. On seeing what that did to the present game, the owner asked instead for formulas that match the current system, which is working, with Vitality growing by a per-level amount that depends on the class (18 November 2026). These replace the formulas in the brief. Everything else in the brief stands: the six attributes, the three renamed resources, the derived statistics, two points a level, class modifiers applied after calculation.

The principle: **a new character has exactly today's numbers**, and attributes move them from there.

### Resources

| Resource | Formula |
|---|---|
| Maximum Vitality | class base + class per-level × Level + Vigor × 2 |
| Maximum Endurance | class base + class per-level × Level + Resolve × 2 + Vigor |
| Maximum Focus | class base + class per-level × Level + Insight × 2 + Spirit |

| Class | Vitality base / per level | Endurance base / per level | Focus base / per level |
|---|---|---|---|
| Warrior | 17 / 9 | 79 / 2 | 4 / 1 |
| Scout | 8 / 8 | 81 / 2 | 2 / 1 |
| Priest | 7 / 7 | 23 / 1 | 75 / 4 |
| Mage | 8 / 6 | 14 / 1 | 71 / 4 |
| Adventurer | 10 / 8 | 50 / 2 | 50 / 2 |

With the starting attributes of section 3, level 1 comes out exactly as today, for every class and every resource:

| Class | Vitality at 1 | at 5 (today) | at 10 (today) | Endurance at 1 | at 10 | Focus at 1 | at 10 |
|---|---|---|---|---|---|---|---|
| Warrior | 40 | 76 (72) | 121 (112) | 100 | 118 | 20 | 29 |
| Scout | 26 | 58 (58) | 98 (98) | 100 | 118 | 20 | 29 |
| Priest | 26 | 54 (58) | 89 (98) | 40 | 49 | 100 | 136 |
| Mage | 24 | 48 (56) | 78 (96) | 30 | 39 | 100 | 136 |
| Adventurer | 30 | 62 (62) | 102 (102) | 70 | 88 | 70 | 88 |

- **Vitality per level by class** is the owner's request. Today every class gains 8; here the Warrior gains 9, the Scout and Adventurer 8, the Priest 7 and the Mage 6. By level 10 the Warrior is 9 ahead of today and the Mage 18 behind. The numbers are data in each class file, and an advanced class may set its own (a Knight more than a Warrior).
- **Vigor is worth 2 Vitality a point.** A level 10 character who put all 18 points into Vigor has 36 more Vitality than one who put in none, against 60 to 90 from level: level stays the largest contributor, as the brief requires.
- **Endurance and Focus now grow with level**, a little, where today they are fixed. Ability costs do not change, so a higher-level character can do somewhat more before tiring.

### Regeneration

The game regenerates every ten seconds, and that stays. The amount per tick:

| Resource | Formula per tick | At the start, by class (today) |
|---|---|---|
| Vitality | 0.3 + Vigor ÷ 10 | Warrior 1.0, Adventurer 0.9, Priest 0.9, Scout 0.8, Mage 0.8 (today 1.0 for all) |
| Endurance | Maximum × (6% + (Resolve + Vigor) × 0.3%) | Warrior 9.9 (10), Scout 9.3 (8), Adventurer 6.7 (6), Priest 3.7 (4), Mage 2.7 (3) |
| Focus | Maximum × (1% + (Insight + Spirit) × 0.4%) | Mage 7.4 (8), Priest 7.0 (4), Adventurer 4.1 (5), Scout 1.1 (2), Warrior 1.0 (2) |

Every figure is within about a point of today's except one: **the Priest's Focus returns at 7.0 a tick against 4 today**, because the Priest's Spirit now counts. The Priest was the slowest class in the corridor's simulation by a wide margin, so this is left as a deliberate improvement to be checked in the rebalance; a class modifier of −40% Focus regeneration would restore today's figure exactly if the owner prefers.

The Adventurer's Survival Training trait still multiplies Vitality regeneration by 1.5.

## 3. Choices the brief leaves open

Each has a proposed default. The ones marked **decision** are in section 7.

### Starting attributes (decision 1)

The brief gives no starting values. Proposed: every attribute starts at 5, and the calling adds 6 points, so that a new character already looks like their calling:

| Calling | Might | Grace | Vigor | Insight | Spirit | Resolve |
|---|---|---|---|---|---|---|
| Warrior | 8 | 5 | 7 | 5 | 5 | 6 |
| Scout | 5 | 9 | 5 | 6 | 5 | 6 |
| Priest | 5 | 5 | 6 | 6 | 9 | 5 |
| Mage | 5 | 5 | 5 | 9 | 7 | 5 |
| Adventurer | 6 | 6 | 6 | 6 | 6 | 6 |

Then 2 points a level, placed with a command (`improve might`), with unspent points shown in `score`. At level 10 a character has placed 18 points; at level 100, 198. No cap on an attribute in this pass.

### How the derived statistics enter the rules

So that a new character plays as today, each statistic acts through how far the attributes behind it stand above 5, the value everyone starts from. The brief's formulas for the statistics themselves are kept for display (`score` shows Physical Power, Precision and the rest as the brief defines them); the effects below are what the rules use. All of it is data in one file.

| Statistic | Effect | A new character |
|---|---|---|
| Physical Power | Weapon and unarmed damage +2% for each point of Might above 5 | Warrior +6%, Adventurer +2%, others none |
| Precision | +1 to hit for every 4 points of Grace above 5 | Scout +1 |
| Evasion | +1 to the defender's side of the hit roll for every 4 points of Grace above 5 | Scout +1 |
| Critical Chance | +0.5% for each point of Grace above 5, added to what traits and skills give | Scout +2% |
| Spell Power | Spell damage +2% for each point of Insight above 5 and +1% for each point of Spirit above 5 | Mage +10% |
| Healing Power | Healing from spells +2% for each point of Spirit above 5 and +1% for each point of Insight above 5. Potions and food are not scaled. | Priest +9% |
| Mental Resistance | 2% chance for each point of Spirit and Resolve above 5 to shake off fear, charm, confusion and other mind effects (at most 75%). No such effects exist yet; the statistic is computed and ready. | |
| Physical Resistance | 2% chance for each point of Vigor and Resolve above 5 to resist stagger, slow and knockdown (at most 75%) | Warrior 6% |
| Carry Capacity | 50 + Might × 5 pounds, computed and shown. Not enforced: items have no weights and there is no encumbrance system, by the owner's earlier decision. | |
| Gathering Efficiency | +1 to the harvest quality score for each point of Grace and Insight above 5 | Scout +5 |
| Crafting Aptitude | +0.02 to the crafted quality shift for each point of Insight and Resolve above 5 | |

A new character therefore differs from today's by a few percent at most, in the direction of their class. A level 10 character who has specialized differs by more: 18 points of Might is +36% weapon damage, 18 of Grace is +4 to hit, +4 to be missed and +9% critical chance. Whether those slopes are right is for the rebalance to confirm; they are easy to change.

### Class modifiers

Percentage modifiers on a class file, applied after the formulas, as the brief says. With the fitted formulas the five callings need none: their differences are already in their bases, per-level amounts and starting attributes. The hook exists for the advanced classes of stage B, where the brief's examples belong (Knight +15% Vitality; Mage +15% Focus; Berserker +15% Endurance; Rogue +10% Critical Chance; Druid +10% Focus and +10% Endurance).

The callings' other differences today (Reaction Time, attack rate, equipment, skills, traits) stay.

### Creatures and NPCs

A creature's attributes default to a template by level (each attribute 4 + level ÷ 2), and a creature file may set any of them: a bear is mighty, a spirit is not. A creature's Vitality may still be set by hand, because a boss should not be bound to the player formula. Resistances and powers then work on creatures exactly as on players.

### Equipment

An item may add to an attribute or to a derived statistic (`+2 Grace`, `+5% Critical Chance`). No existing item is changed; the hook exists for crafted and found equipment.

### Renown

The brief lists Renown among the systems that use attributes. Proposed: no mechanical link. The Chronicle and NPCs may speak of a character's highest attribute ("remembered for her Resolve"), which is the brief's design goal.

### Existing characters

There are no live players. Saved test characters are converted at first login: attributes set from the calling's starting values plus 2 points a level left unspent.

## 4. The rename

Vitality, Focus and Endurance replace HP, Mana and Stamina everywhere:

- messages, `score`, prompts, help, shop and item text, NPC speech, room text;
- the code: fields, methods and data keys (`QueryHp` becomes `QueryVitality`, `ManaCost` becomes `FocusCost`, and so on), with no aliases left behind;
- every document and every test.

This touches several hundred files and most test expectations. It is mechanical, and it is done in one pass while no other branch is open, so that nothing has to be merged across it.

## 5. Order of work

The attribute work touches the same files as three branches now open (the corridor's integration, the crafting engine and the crafting content). It starts when all three are merged.

| Step | Who | What |
|---|---|---|
| 1 | Lead | Merge the corridor, the crafting engine and the crafting content. |
| 2 | Opus agent | The engine: attributes on every living thing, the formulas as data, derived statistics, point allocation, class modifiers, creature templates, equipment hooks, save conversion, regeneration; the statistics wired into the hit roll, damage, spells, healing, stagger and slow, gathering and crafting. Tests. |
| 3 | Sonnet agent, straight after 2 | The rename, in code, content, tests and documents. |
| 4 | Sonnet agent | Re-check: both simulations updated to the new rules and run; the slopes of the derived statistics confirmed or adjusted; the experience table checked against the 15-hour target; the five-calling play-throughs. |
| 5 | Sonnet agent | The storytelling pass: attributes in NPC speech, books, Chronicle wording and reputation lines, as the brief's design goals ask. |
| 6 | Lead | Review, run everything, merge. |

Steps 2 and 3 are one after the other on purpose. Step 5 can run beside step 4.

## 6. What this does to work already done

Because a new character has today's numbers, little needs redoing.

- **Class stage A's balance** and **the corridor's balance** are re-checked in step 4, not rebuilt. The expected differences are small: the Warrior hits about 6% harder, the Mage's bolt about 10% harder, the Mage has less Vitality from level 2 on, the Priest's Focus returns faster.
- **The corridor's experience table** stands unless the re-check shows the 15 hours has moved by more than about an hour.
- **Crafted consumables** and other healing keep their numbers.
- **Advanced classes** (stage B) should be built on the new system, so stage B comes after this.

## 7. The owner's decisions (18 November 2026)

1. **Starting attributes.** As in section 3: 5 in each, plus 6 points by calling.
2. **Vitality per level by class.** Warrior 9, Scout 8, Adventurer 8, Priest 7, Mage 6.
3. **Focus per level by class**, in the same way as Vitality: Mage 4, Priest 4, Adventurer 2, Warrior 1, Scout 1. Endurance likewise: Warrior 2, Scout 2, Adventurer 2, Priest 1, Mage 1. All three are numbers in each class's file, and an advanced class may set its own.
4. **The Priest's faster Focus regeneration** is kept.
5. **Carry Capacity** is computed and shown, and not enforced until encumbrance is built.

Nothing is open. The build starts when the corridor, the crafting engine and the crafting content have merged (section 5).

## 8. Balance revision (owner-approved 19 November 2026; overrides the tables above where they differ)

The simulator showed the formulas above make most callings much stronger once a character spends its points: a level 4 character with two potions and 6 points placed beat the Goblin Overseer 80 / 97 / 92 / 97 / 72 percent of the time (Warrior, Scout, Priest, Mage, Adventurer) against 60 / 75 / 53 / 55 / 59 today. The owner's target is about 71% for every calling. These changes give 74 / 75 / 68 / 73 / 70 (3,000 fights each, points placed in the calling's obvious attributes):

| What | Was | Now |
|---|---|---|
| Precision | +1 to hit per 4 points of Grace above 5 | +1 per **8** points |
| Evasion | +1 to the defender's side per 4 points of Grace above 5 | **Nothing from Grace.** The statistic stays (equipment and effects may add to it) |
| Critical Chance | +0.5% per point of Grace above 5 | **+0.25%** |
| Focus regeneration | Maximum x (1% + (Insight + Spirit) x 0.4%) | Maximum x (1% + (Insight + Spirit) x **0.15%**) |
| Warrior Vitality | 17 base, 9 a level | **13** base, **8** a level |
| Scout Vitality | 8 base, 8 a level | **3** base, **7** a level |
| Scout Evasion trait | 8% dodge | **5%** dodge |
| Mage Vitality | 8 base, 6 a level | **4** base, 6 a level |

Priest and Adventurer tables are unchanged. Known consequences: a character who places no points is weak (61 / 63 / 60 / 35 / 59), the Mage most of all; level 5 remains easy for everyone (93-98%). `tests/balance_sim.py` must model placed points (2 a level, in the calling's obvious attributes) as its main table and keep the unspent case as a second table. Skill ranks and the larger heal will need a second pass.
