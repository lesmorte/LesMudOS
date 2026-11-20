# Skill and spell ranks

Owner's decision (November 2026): skills and spells do not improve with use. A better version is bought from a trainer as a rank, and the rank a character may hold is capped by level.

## Rule
- Every active skill and spell (anything with a command: `bolt`, `heal`, `bash`, `strike`, ...) has ranks, written I, II, III and so on. A character starts with rank I of what their calling grants, as today. Passive traits have no ranks.
- Rank cap by level: rank r needs level 5 x (r - 1). Rank II at level 5, III at 10, IV at 15, V at 20. The step (5) is one named number. Ranks beyond what the game's content reaches are defined by the same rule and need no extra work.
- A rank is bought from a trainer of the character's calling with gold: `ranks` at a trainer lists what can be learned, the price, and what is locked and at which level; `train <skill>` buys the next rank. Ranks cannot be skipped. The trainer refuses, in words, a character who is too low in level, short of gold, or of the wrong calling.
- A higher rank replaces the lower one. The command word stays the same (`bolt`), so no aliases or habits break. `skills` shows the rank beside each entry, and the next rank's level if it is locked.

## What a rank changes
One table per skill, in the skill's own data file, read by one shared helper so no skill computes its own scaling. Default when a skill names nothing special:
- effect (damage, healing, absorb, bonus size): + 30% of the rank I value per rank, rounded to nearest; durations do not change;
- resource cost: + 15% of the rank I cost per rank;
- preparation and recovery times: unchanged.
Arcane Bolt as the worked case: I 6-9 for 20 Focus, II 8-12 for 23, III 10-14 for 26. Rank effects stack with attributes (Insight, Spirit, Might) and traits the same way rank I does today.

## Trainers
- Reuse `/std/trainer` (the crafting teachers already sell lessons through it). One trainer per calling.
- (Superseded: rank II trainers live in the new village rooms of `docs/specs/hollow-expansion.md`, one per calling; Arcane Bolt II is taught at the Hedge-Scholar's Loft.) Rank II is taught in Vincent's Hollow, so nobody must cross the corridor to keep pace at level 5. Rank III and above are taught in Oakenfell at the Hall of Legends.
- Prices are data beside the rank table. Starting values, to be settled by the balance simulator: rank II 40 gold, III 150, IV 400, V 900.

## Balance requirement
`tests/balance_sim.py` gains ranks: each calling buys ranks when level and gold allow. The gap between callings at levels 5 and 10 must not be wider than it is today, and the Mage's bolt at each rank must beat the Mage's best staff of that level per action against an armoured creature of that level, and not against an unarmoured one by more than a small margin. Numbers that fail are changed in the table, not in the rule.

## Build
After the attribute build (it renames the resources these tables cost) and before class stage B, whose fifteen advanced classes then define their skills with rank tables from the start. One Sonnet agent: shared helper and data, trainer rooms and people, `ranks` / `train` / `skills` output, simulator, tests, docs.

## Unlocks (owner's decision)
Some ranks need more than level and gold. The gold is always paid; the unlock only makes the rank available to that character.
- An unlock belongs to a trainer's offer of a rank, not to the skill, so two trainers may teach the same rank on different terms, and one skill's ranks may each be unlocked differently.
- An offer carries a list of requirements, all of which must hold. Kinds, each one line of data: `level` (always present, from the cap rule), `quest <id>` completed, `kills <creature kind> <n>`, `kills_with <skill> <n>` (killing blows struck with that skill), `renown <n>`, `standing <faction> <n>` (when factions are built), `item <path>` shown to the trainer (not taken unless the offer says so). New kinds are added in one place.
- Once all requirements are met at a trainer, the unlock is recorded on the character for good (`[Saved]`), so later losing Renown or standing does not take it back. Counters the requirements read (kills by kind, kills with a skill) are kept from character creation, so progress made before meeting the trainer counts.
- `ranks` at a trainer shows each requirement with progress ("Goblins slain: 6 of 10") and never hides that a rank exists. A trainer may give a line of their own for each unmet requirement.
- Advanced-class skills start at rank I when the class is taken.

First use: **Arcane Bolt II**, taught at the Hedge-Scholar's Loft in Vincent's Hollow: level 5, 40 gold, and 10 goblins slain. Most Mages will have it by the time they finish the cellars; it is there to teach the player that ranks can ask for deeds. The other callings' rank II skills there need level and gold only, unless the owner names unlocks for them.

## Rank tables (owner-approved November 2026; numbers are starting values for the simulator)
Skills not listed use the default (+30% effect, +15% cost per rank). Resource names are the attribute build's (Focus, Endurance, Vitality).

| Calling | Skill | I (level 1) | II (level 5) | III (level 10) | IV (level 15) |
|---|---|---|---|---|---|
| Mage | Arcane Bolt | 6-9, 20 Focus | 8-12, 23 | 10-14, 26 | default |
| Mage | Magical Lore | rank I only | | | |
| Priest | Minor Heal | 20 Vitality, 25 Focus | 38, 32 | 60, 40 | half a starting Warrior's Vitality at level 15 |
| Priest | Blessing | +1 to hit, +1 armour | +2, +2 | +3, +3 | default cost steps |
| Warrior | Shield Bash | 75% weapon damage, stagger | 98% | 120% | default |
| Scout | Precision Strike | +2 to hit, 15% critical | +3, 20% | +4, 25% | default cost steps |
| Scout | Tracking | adjacent rooms | two rooms away | rank II is the last | |
| Adventurer | Double Strike (new) | 2 hits at 60% | 2 hits at 75% | Triple Strike: 3 at 60% | Quadruple Strike: 4 at 55% |
| Adventurer | Appraise | rank I only | | | |

- **Tracking** (owner's decision) does not go through closed or locked doors, or through hidden and secret ways: it follows only exits the character could walk through now, at every rank. At rank II, when there are creatures one room away behind a closed door or an unfound hidden way, it adds one line, "Something is close by, though you cannot tell where.", with no direction, no names and no count. A hidden way the character has already found and can use is an ordinary exit. Tracking never marks a hidden exit as found and never reports a discovery to the Chronicle.
- **Minor Heal** restores about half the Vitality of a Warrior with starting attributes at the rank's level (Warrior Vitality = 17 + 9 x level + Vigor x 2 with Vigor 7: 40, 76, 121). The rank I change (8 to 20) applies to every Priest at once. Divine Knowledge's +25% still applies on top. The simulator must show a Priest's Focus runs out before a creature of the Priest's own level does, so the heal cannot make solo fights riskless.
- **Double Strike** (`strike` is the Scout's word; use `double`): new Adventurer skill granted at level 1, 20 Endurance, preparation 0.5, recovery 1.0. Each hit makes its own hit roll with the wielded weapon (or fists) at the listed fraction of its damage. The name changes with the rank; the command does not.
- **Jack of All Trades**: new Adventurer trait (no ranks). Crafting and gathering proficiency is gained 25% faster in every craft. One named number.
## Open
- Unlocks for the other rank II skills and for rank III in Oakenfell (a Hall of Legends trial quest, Renown, or Assembly standing are the natural candidates).
