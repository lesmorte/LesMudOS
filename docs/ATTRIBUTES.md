# The Hollowmere attribute system: design and build plan

How the owner's brief ([specs/attributes.md](specs/attributes.md)) becomes part of the game: what changes, what the formulas do to today's numbers, the choices the brief leaves open, and the order of work. Nothing here is built yet.

## 1. What changes

| Today | After |
|---|---|
| A character has a level, a calling and its fixed numbers. There are no attributes. | Six attributes: Might, Grace, Vigor, Insight, Spirit, Resolve. Two points to place at every level. |
| HP, Mana, Stamina | Vitality, Focus, Endurance, in every message, command, help text, document and in the code. |
| Maximum HP is the calling's starting HP plus 8 a level. Mana and Stamina maximums are fixed by the calling (Warrior 100 Stamina and 20 Mana; Mage 30 and 100). | All three come from the owner's formulas: level first, then attributes. Every character has all three resources. |
| To hit: d20 + level × the calling's attack rate against 10 + armor + (level − 1). Damage is the weapon's. Critical hits, dodge and block come only from traits. | Precision and Evasion enter the hit roll. Physical Power scales weapon damage. Grace gives a base critical chance. Spell Power and Healing Power scale spells. |
| A calling's differences are fixed numbers. | A calling starts its members with different attributes and applies percentage modifiers after the formulas. |
| Gathering and crafting use proficiency only. | Gathering Efficiency and Crafting Aptitude add to them. |
| Creatures have hand-set HP, damage and armor. | Creatures also have attributes, generated from level and a template, so that resistances and powers work on them as on players. |

## 2. What the formulas do to today's numbers

This is the part that needs the owner's eye before anything is built.

### Vitality is about three times today's HP

| | Level 1 | Level 5 | Level 10 |
|---|---|---|---|
| Warrior today | 40 | 72 | 112 |
| Mage today | 24 | 56 | 96 |
| Formula, Vigor 5 | 92 | 140 | 200 |
| Formula, Vigor 10 | 122 | 170 | 230 |

Nothing is wrong with larger numbers, but every creature's damage, every healing item and spell, and both balance simulations were tuned to the old ones. They all have to be re-tuned, for the Hollow and for the corridor. That is expected work, and it is in the plan below.

### Regeneration as written is far slower than today's

| | Today | Formula (attributes 5 to 10) |
|---|---|---|
| Vitality | 6 a minute, of 24 to 40 | 2 to 3 a minute, of about 100 |
| Endurance (Stamina) | 24 to 60 a minute, of 30 to 100 | 2 to 3 a minute, of about 110 |
| Focus (Mana) | 12 to 48 a minute, of 20 to 100 | 2 to 3 a minute, of about 100 |

Taken literally:

- A badly hurt character needs about 40 minutes to recover by resting, against about 7 today.
- A Warrior's Shield Bash (15 Stamina) could be used about once every six minutes of regeneration, against several times a fight today.
- A Mage's Arcane Bolt (20 Mana) is the Mage's whole attack. With about 100 Focus that is five bolts, then about one more every eight minutes. Today the Mage casts continuously.

So the per-minute figures cannot be dropped in while ability costs stay as they are. Three ways to reconcile them, for the owner to choose (decision 2):

- **A. As written, and re-cost everything.** Abilities become rare, decisive things; a caster needs a basic attack that costs nothing; food, potions, crafted tonics and havens become the main way to recover. This is a different and slower game, and the 15-hour pace for levels 5 to 10 would have to be rebuilt around it.
- **B. As written out of combat only, with a separate in-combat rule.** Not what the brief says.
- **C. Read the formulas as the rate per regeneration tick** (every ten seconds, as today) instead of per minute. Vitality then returns at 12 to 18 a minute of about 100 to 200, Endurance and Focus at 12 to 18 a minute: close to today's feel for Vitality, slower than today for Endurance and Focus but workable with modest cost changes. Attributes still decide the rate exactly as the owner wrote. **Recommended**, with the tick length kept in one data file so that it can be lengthened later.

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

The brief names the statistics and their formulas but not how they act. Proposed, all as data in one file:

| Statistic | Effect |
|---|---|
| Physical Power | Weapon and unarmed damage × (1 + Physical Power ÷ 100). Might 8 at level 1 gives +16%. |
| Precision | Adds Precision ÷ 4 to the attacker's hit roll. |
| Evasion | Adds Evasion ÷ 4 to the defender's side of the hit roll. (Level stays in the roll through the calling's attack rate, as now.) |
| Critical Chance | Grace × 0.25% base, added to what traits and skills give. |
| Spell Power | Spell damage × (1 + Spell Power ÷ 100). |
| Healing Power | Healing from spells × (1 + Healing Power ÷ 100). Potions and food are not scaled. |
| Mental Resistance | Percentage chance to shake off fear, charm, confusion and other mind effects (capped at 75%). No such effects exist yet; the statistic is computed and ready. |
| Physical Resistance | Percentage chance to resist stagger, slow and knockdown (capped at 75%). Stagger and slow exist today. |
| Carry Capacity | Computed and shown. Not enforced: items have no weights and there is no encumbrance system, by the owner's earlier decision. It takes effect when encumbrance is built. |
| Gathering Efficiency | Adds Gathering Efficiency ÷ 4 to the harvest quality score and ÷ 8 to the success chance. |
| Crafting Aptitude | Adds Crafting Aptitude ÷ 50 to the crafted quality shift, and raises the chance of a bonus output. |

The divisors are starting values. The balance simulation sets them so that two level-matched characters with ordinary attributes hit each other about as often as today.

### Class modifiers

Percentage modifiers on a class file, applied after the formulas, as the brief says. The brief's examples are advanced classes, which are not built yet; they are recorded for stage B. Proposed for the five callings, small on purpose:

| Calling | Modifier |
|---|---|
| Warrior | +10% Vitality |
| Scout | +5% Critical Chance |
| Priest | +10% Healing Power |
| Mage | +10% Focus |
| Adventurer | +5% Vitality, +5% Endurance |

The callings' other differences today (Reaction Time, attack rate, equipment, skills, traits) stay. Their fixed HP, Mana and Stamina numbers go.

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
| 4 | Sonnet agent | Rebalance: both simulations rebuilt on the new rules; every creature's damage and Vitality, every healing item and spell, ability costs, and the experience table re-checked against the 15-hour target; the five-calling play-throughs. |
| 5 | Sonnet agent | The storytelling pass: attributes in NPC speech, books, Chronicle wording and reputation lines, as the brief's design goals ask. |
| 6 | Lead | Review, run everything, merge. |

Steps 2 and 3 are one after the other on purpose. Step 5 can run beside step 4.

## 6. What this does to work already done

- **Class stage A's balance** and **the corridor's balance** are both re-done in step 4. The corridor's experience table was derived from experience per hour under the old rules; it is re-derived, and may change.
- **The Priest's slowness**, accepted earlier, is worth another look in step 4: Healing Power and Spirit give the Priest something the old rules did not.
- **Crafted consumables** restore fixed amounts tuned to the old HP. They are re-tuned in step 4.
- **Advanced classes** (stage B) should be built on the new system, so stage B comes after this.

## 7. Decisions needed from the owner

1. **Starting attributes.** The table in section 3 (5 each, plus 6 points by calling), or something else, for example points the player places at creation?
2. **Regeneration.** A (as written, re-cost everything, a slower game), B (as written out of combat only), or C (the formulas per ten-second tick; recommended)?
3. **The statistics' effects.** The table in section 3 as the starting point, to be calibrated by simulation?
4. **Carry Capacity.** Computed and shown but not enforced until encumbrance is built?
5. **Rebalance.** Agreed that the Hollow's and the corridor's numbers, and the experience table, are re-derived under the new rules?
