# Skill and spell ranks

**Status (22 November 2026): built**, including the owner's decisions after the build (Minor Heal 20 / 38 / 60 with a 15-second wait; Double Strike 35 Endurance) and Aimed Shot's ranks (docs/CLASS_SYSTEM.md section 14). Implementation notes: docs/CLASS_SYSTEM.md section 13; the builder's side: docs/BUILDING.md. See "As built" at the end.

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
| Scout | Precision Strike | +4 to hit, 15% critical | +5, 20% | +6, 25% | default cost steps |
| Scout | Tracking | adjacent rooms | two rooms away | rank II is the last | |
| Adventurer | Double Strike (new) | 2 hits at 60% | 2 hits at 75% | Triple Strike: 3 at 60% | Quadruple Strike: 4 at 55% |
| Adventurer | Appraise | rank I only | | | |

- **Tracking** (owner's decision) does not go through closed or locked doors, or through hidden and secret ways: it follows only exits the character could walk through now, at every rank. At rank II, when there are creatures one room away behind a closed door or an unfound hidden way, it adds one line, "Something is close by, though you cannot tell where.", with no direction, no names and no count. A hidden way the character has already found and can use is an ordinary exit. Tracking never marks a hidden exit as found and never reports a discovery to the Chronicle.
- **Minor Heal** restores about half the Vitality of a Warrior with starting attributes at the rank's level (Warrior Vitality = 17 + 9 x level + Vigor x 2 with Vigor 7: 40, 76, 121). The rank I change (8 to 20) applies to every Priest at once. Divine Knowledge's +25% still applies on top. The simulator must show a Priest's Focus runs out before a creature of the Priest's own level does, so the heal cannot make solo fights riskless.
- **Double Strike** (`strike` is the Scout's word; use `double`): new Adventurer skill granted at level 1, 20 Endurance, preparation 0.5, recovery 1.0. Each hit makes its own hit roll with the wielded weapon (or fists) at the listed fraction of its damage. The name changes with the rank; the command does not.
- **Jack of All Trades**: new Adventurer trait (no ranks). Crafting and gathering proficiency is gained 25% faster in every craft. One named number.
## Open
- Unlocks for the other rank II skills and for rank III in Oakenfell (a Hall of Legends trial quest, Renown, or Assembly standing are the natural candidates).

## As built (22 November 2026)

Built as written: the rule and the cap (`Skill.RankLevelStep` = 5), the shared helper (`Skill.RankValue`), rank tables in each skill file, `ranks` and `train` on `/std/trainer`, unlocks with the seven kinds, the counters, the rank II trainers in the new village rooms (Arcane Bolt II at the Hedge-Scholar's Loft: level 5, 40 gold, 10 goblins), ranks III to V by the five hallmasters of the Hall of Callings, Tracking's rules, Double Strike, Jack of All Trades, the simulator (`python3 tests/balance_sim.py ranks`) and `tests/rank_test.py`.

Decisions where this spec was silent:
- **The default step** is cumulative from the rank before: each rank adds 30% (effect) or 15% (cost) of the rank I value to the previous rank's number, so a rank a table does not name continues from the last one it does (Arcane Bolt IV 12-17 for 29, from III's 10-14 for 26). Numbers are rounded half away from zero when used.
- **"default cost steps"** in the Blessing and Precision Strike rows: the cost follows the default step at every rank, and the IV column continues the table's own step (+4 / +4; +5 / 30%).
- **Creature kind** for `kills`: the beast-table kind (the creature's `Skin`, else its `Blood`; every goblin is `goblin`), else its `ChronicleKind` (by default its file name). No third naming.
- **Wizards**: the kill credit does not exclude wizards (they earn experience), so their kills count; only the Chronicle ignores them. A death forced by a wizard's object (from `/wiz`) counts for nobody.
- **When an unlock is recorded**: at `ranks` or `train` at that trainer, once every requirement holds, whether or not the gold is there.
- **Prices** are the daemon's table (II 40, III 150, IV 400, V 900); the hallmasters offer III to V; rank VI and beyond have no price and are not offered yet.
- **Several trainers in one room** (the Hall of Callings): each answers only the calling it teaches.
- **Aimed Shot** (docs/specs/hollow-expansion.md section 1) was not built then; it is now (see the end of this document): it has ranks I to V and Wenna Farrow teaches rank II.

Provisional, for the owner (SUPERSEDED by the owner's decisions below; kept as the record of the options):
- **Minor Heal** shipped option (b): 7 / 9 / 11 / 13 / 15 Vitality for 25 / 32 / 40 / 44 / 48 Focus (rank I 7, later ranks by the default step from it, the spec's costs). The spec's 20 / 38 / 60 (option a) puts the Priest about 18 points above the mean at rank I and 9 at rank II. Option (c): the spec's 20 / 38 / 60 with no heal for 15 seconds after a heal lands within 2 points at both; it needs a new engine rule (a lockout), which is not built. A higher Focus cost alone does not fit both (60 / 75 / 95 is +11 at rank I and +2 at rank II; 75 / 95 / 120 is +3 and -10), and a longer Preparation changes nothing (the wind-up is inside the delay, and nothing at the Overseer interrupts it). Table: `python3 tests/balance_sim.py ranks heal`.
- **Double Strike** ships the spec's 2 x 60% / 2 x 75% / 3 x 60% / 4 x 55%, flagged: at rank I it puts the Adventurer about 8 points above the mean (74 to 87). Numbers that would not: 2 x 50% (+3), or 2 x 60% for 35 Endurance (+4) or 50 (+1). Table: `ranks double`.

Balance requirement, as measured (`ranks level5 gap bolt focus climb`):
- **The gap at level 5** (middle human, resting, 4 potions, starting gear, 3000 runs a cell): today 81 / 71 / 84 / 73 / 74 (Warrior, Scout, Priest, Mage, Adventurer; spread 13, mean 76.5). With ranks and nobody buying: 81 / 71 / 78 / 73 / 87 (spread 16: Double Strike). With every calling holding rank II: 94 / 75 / 88 / 92 / 93 (spread 19, mean 88.5). So **the requirement fails here**: rank II lifts everyone by about 12 points and the Scout least (Precision Strike II is worth 4 points, Arcane Bolt II 19, Shield Bash II 13). Against the Overseer alone for the perfect player (2 potions, best gear) the spread narrows (6 to 5). **At level 10** the ordinary corridor creatures are won about 100% by everyone with or without ranks; against the five level 10 bosses (5 potions) the spread narrows from 20 to 11 with rank III. Numbers not changed: the owner decides (the spec says failing numbers change in the table; candidates are a larger Precision Strike II and smaller Arcane Bolt II and Shield Bash II).
- **Arcane Bolt against the best staff** (per action): it beats the staff against an armoured creature at every rank (x1.7, 2.1, 3.7, 6.7), but it also beats it against an unarmoured one by far more than a small margin (x1.4 at rank I already, 1.6, 2.3, 3.3), because a Mage's staff gains nothing from Might and nothing better than the Staff of the Hollow Oak exists. Not changed: the failure is in rank I too.
- **The Priest's Focus** runs out before a creature of the Priest's own level does: against a level 5 or level 10 creature that does not die, the Priest dies every time (in about 4 to 6 minutes); a full Focus pool pays for 4 to 5 heals at rank I and 3 at rank II or III.
- **Arcane Bolt II's ten goblins**: every calling has killed 13 goblins on the way to level 5 in the simulator's whole run.

## Owner's decisions after the build (22 November 2026)
1. **Minor Heal** is the large heal: 20 / 38 / 60 Vitality for 25 / 32 / 40 Focus at ranks I / II / III (later ranks by the default step), with **no second Minor Heal for 15 seconds after one** (a named number; the wait is on the caster, shown in the refusal and in `skills`). This replaces the provisional heal of 7. Simulated: Priest within 2 points of the mean.
2. **Double Strike** keeps two hits at 60% and costs **35 Endurance** at rank I (was 20); later ranks' costs follow the default step from 35.
3. The uneven gains from rank II (Mage +19, Warrior +13, Priest +10, Adventurer +6, Scout +4 at level 5) are left until Aimed Shot is built, then measured again.

## Built after the decisions (22 November 2026)
- **Minor Heal** is 20 / 38 / 60 Vitality for 25 / 32 / 40 Focus, later ranks by the default step (66 for 44, 72 for 48), with a 15-second wait per caster (`HealWait` in `minor_heal.cs`; the wait lives on the caster in `Living`, is not saved, and cannot be set or cleared by another object; a cast during it is refused before any Focus is spent, with the seconds left; `skills` shows it). The wait starts when a cast begins.
- **Double Strike** costs 35 Endurance at rank I and 40 / 46 / 51 / 56 at ranks II to V (default step from 35); its hits are unchanged.
- **Simulator correction (important).** The simulator's old option (c) ("no heal for 15 seconds, the Priest within 2 points of the mean") rested on a fault: a heal's lockout was kept as a time inside one fight and carried into the next fight, whose clock starts again at 0, so a heal near the end of one fight blocked heals for tens of seconds in the next. The wait is now counted in real time across fights (the walking and resting between rooms count). With that corrected, the 20 / 38 / 60 heal with a 15-second wait leaves the Priest about 18 points above the mean of the five callings at rank I and about 10 at rank II (3000 runs a cell, `python3 tests/balance_sim.py ranks wait`): a Priest's Focus pays for about three heals, so a 15-second wait is not what limits it. The numbers are shipped as the owner decided; the table is in the simulator for the next decision.
- **Aimed Shot**: see docs/CLASS_SYSTEM.md section 14 and the measurement in the same place.

## Open decision: the large heal's Focus cost (measured 22 November 2026)
The Priest against the mean of the five callings, standard test (level 5, starting gear, 4 potions, points placed, middle human, RestFactor 5, Overseer from the cellar entrance), 3000 runs a cell, 15-second wait, `python3 tests/balance_sim.py ranks 3000 healcost`. A level 5 Priest has 120 Focus; "heals" is what the full pool pays for at that rank's cost. Shipped: 20 / 38 / 60 Vitality for 25 / 32 / 40 Focus.

| Minor Heal (Vitality I/II/III; Focus I/II/III) | rank I | vs mean | heals | everyone at II | vs mean | heals |
|---|---|---|---|---|---|---|
| 20/38/60, 25/32/40 (shipped) | 99.9 | +18.6 | 4 | 99.9 | +10.1 | 3 |
| 20/38/60, 35/45/56 | 99.5 | +18.4 | 3 | 99.6 | +9.8 | 2 |
| 20/38/60, 45/58/72 | 98.1 | +17.3 | 2 | 98.0 | +8.5 | 2 |
| 20/38/60, 55/70/88 | 94.3 | +14.2 | 2 | 93.5 | +4.9 | 1 |
| 20/38/60, 70/90/112 | 85.7 | +7.3 | 1 | 81.0 | -5.1 | 1 |
| 12/23/36, 25/32/40 | 99.1 | +18.0 | 4 | 99.9 | +10.1 | 3 |
| 15/28/45, 25/32/40 | 99.8 | +18.6 | 4 | 99.9 | +10.0 | 3 |
| 7/9/11 (the old small heal), 25/32/40 | 76.7 | +0.1 | 4 | 88.4 | +0.9 | 3 |

Reading: the Priest pins near 100 for any heal from 12 upward unless the cost leaves only one or two heals a fight; no cost in the table puts both columns within 5 points (70/90/112 gives +7.3 and -5.1). The cliff is between a heal of 7 and one of 12 (a few Vitality per heal, not the cost). The numbers are unchanged in the game; the choice is the owner's.


## Owner's decision on Minor Heal (22 November 2026, after the corrected measurement)
The large heal stays for others; **healing yourself gives half** (10 / 19 / 30 at ranks I / II / III, rounded half up, later ranks likewise). The 15-second wait and the costs stay. The skill's description and the shrine's teaching say why in the world's voice: the Quiet Hand gives freely to the one who reaches for another and sparingly to the one who reaches for himself. The simulator must report the solo Priest at half (the earlier table found a heal of 12 already too strong, so half may still leave the Priest ahead): that number goes to the owner; the game ships half as decided.

**Built (22 November 2026):** `MinorHealSpell.SelfShare`, the skill's summary and help line, Sister Merewyn's and Anselm's new topics, the simulator (`python3 tests/balance_sim.py ranks 3000 selfheal`), tests in `tests/aim_test.py` and `tests/rank_test.py`. Measured: with half the solo Priest is still ahead (standard test, 3000 runs a cell: rank I 95 against a mean of the five callings of 80.2, +14.8; everyone at rank II 100 against 89.8, +10.2). A self-heal that would leave the Priest level is about 7 at rank I (II 13) and about 5 at rank II (II 10): no one value is inside 5 points in both columns. Shipped as decided.

## Doors (owner's decision, 22 November 2026)
Builders get openable doors: see `docs/specs/sneak.md`, which needs one before the Overseer.

## Owner's later decisions (22 November 2026, after sneak was measured)
- **Minor Heal on the caster gives a third**, rounded half up: 7 / 13 / 20 at ranks I / II / III (then 22, 24). Half left the solo Priest at 95% and 100% in the standard test; a quarter was level at rank II but dropped the rank I Priest to 50%, because the Priest's result falls sharply between a self-heal of 7 and 5. A third measured 76% at rank I (level with the others) and 98% with everyone at rank II (about 6 over). This replaces "half" wherever this document says it.
- **Precision Strike gains +2 to hit at every rank**: +4 / +5 / +6 / +7 (was +2 / +3 / +4 / +5). Sneak's ambush did not lift the melee Scout, which sat 10 to 15 points under the other callings; with this it measures about 83% at rank I and 87% at rank II with sneaking, within about 5 points.

## Aimed Shot: the opener's cap (Owner's decision, 24 Nov 2026)
Owner: "slow cap to aimed shot". When an Aimed Shot is the action that starts a fight (the x2.5 opener at rank I, x3.0 to x4.5 at the later ranks), the damage it deals is capped at a share of the target's **maximum** Vitality: **60% + 15% x d**, where d = the Scout's level minus the creature's level at the moment the shot lands, **floor 30%**, and **no cap at all** once the share reaches 100% (d = +3 or more). This is option G2 of docs/BALANCE_SCOUT.md section 10, and `tests/balance_sim.py` uses it by default (`check` compares the three numbers with `mudlib/skills/aimed_shot.cs`).

| d | -2 or less | -1 | 0 | +1 | +2 | +3 or more |
|---|---|---|---|---|---|---|
| share of maximum Vitality | 30% | 45% | 60% | 75% | 90% | none |

- The cap is applied to the final damage, after Might, the factor, the critical hit and the block, and before the target's absorption. It is `max(1, floor(share x maximum Vitality))`.
- It applies only to this opener (hidden or not, the engine knows no difference: the x2.5 shot at the start of a fight). Not to an ordinary Aimed Shot (x1.75, a shot at a second creature in a fight), not to Precision Strike's ambush, not to any other attack.
- When the cap really lowers the damage the shooter is told so ("The shot is true, but the creature twists away at the last moment and takes only part of it."); verbose damage shows the damage dealt.
- Built in `Skill.QueryOpenerCapPercent` (0: no cap) overridden by `AimedShotSkill`, applied in `Living.Attack`; tests in `tests/aim_test.py`.
