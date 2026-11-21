# Balance for a person, not a perfect player

*21 November 2026. Written with `tests/balance_sim.py` (new sections: `calibrate`, `ablate`, `human`, `recommend`). Nothing in the game has been changed; every recommendation below is for the owner to choose.*

## 1. The short version

- The simulator played perfectly. It acted the instant it could, used its best skill every time, drank at exactly 35% and started every fight at full Vitality. Against the two Goblin Elite Guards and the Overseer, that is a different game from the one a person plays.
- The creature numbers were **not** wrong. `python3 tests/balance_sim.py check` still passes, and I read the Elite Guard, Overseer and Minion files again by hand (section 2). What was wrong was the player.
- A human model now sits beside the perfect one. Its single biggest effect is not slowness or mistakes, it is **that Vitality, Endurance and Focus are carried from room to room** instead of being full at every fight. That alone takes the old game's level 5 Scout (Hunting Bow, no armour, 4 potions) from 92% to 33%.
- With the human habits added, the model reproduces the evening in outline: the Scout loses or flees the Overseer far more often than not (56% to 92% of runs, depending on the version), and the commonest way to fail is to flee. It does **not** reproduce "usually 3 potions on the guards": it gives 1.5 to 2.0 on average, and 3 or more in 15% to 35% of runs. One evening cannot tell whether that gap is the model or the evening (section 5).
- In the current game, a level 5 character in starting gear with 4 potions kills the Overseer about **13% of the time** under the middle human, against 94% for the perfect player. The 71% in `ATTRIBUTES.md` section 8 describes the perfect player at level 4 with 2 potions; it does not describe a person.
- To bring the middle human to about 70% the smallest single change I found is **the Minor Healing Potion healing 35 instead of 20** (66%), with the Mage's Vitality a level 6 to 8 to keep the callings together (69%, spread 7 points). Other routes are in section 7. All of them make the Overseer close to trivial for a perfect player (93% to 99% at level 4 with 2 potions, against 71% today).

## 2. Were the old numbers for the guards and the summon wrong?

No, with one thing to note.

| Thing | Game file | Simulator |
|---|---|---|
| Goblin Elite Guard | Level 4, 40 Vitality, damage 3-5, to-hit 2, armour 3, Reaction 3.5, Slash 0/1.0, Shield Bash 0.5/1.5 at x2, Bash as every fourth action | the same (`check` compares every field) |
| Two of them in the Overseer Hall | `AddSpawn(elite_guard, 2)`, both Aggressive | `ROOMS` has two |
| Goblin Overseer | Level 5, 114, 4-7, to-hit 2, armour 3, Reaction 4.0; Battle Cry first (+2 damage for 30 s), then Slash, Slash, Power Strike (x2, 1.0 prep, 2.0 recovery); Summon once, as his next action after Vitality first falls to half or below | the same |
| Goblin Minion | Level 2, 20, 2-4, to-hit 0, armour 1, Reaction 2.5, Stab 0/0.5; `JoinFight`: it waits its own Reaction Time (5 s) | the same: it is drawn in and waits 5 s |

The game applies the Battle Cry's +2 after the Might scaling and before the Power Strike's x2; the simulator adds it before the scaling. For a creature with 6 in each attribute the difference is a rounding step, not a result.

What the simulator got wrong is the player, in four ways:

1. **How a fight starts.** The simulator lets the player act at second 0. In the old game that was nearly right (the player types `kill`, acts at once; the creatures wait their Reaction Time). In the current game (the aggro merge, commit `980f437`, which came after the attribute branch left main), aggressive creatures start the fight as the player arrives, and the player, being the one attacked, waits his **own** Reaction Time x 2 s before his first action (5 s for a Scout with the bow). Every creature in the approach is Aggressive. The perfect model in the existing tables still ignores this, because those tables must stay as they are; the human model includes it. At level 5 it costs the perfect player about 3 points (section 6, table A).
2. **Resources.** Every room fight started at full Vitality, Endurance and Focus. In the Hollow nothing heals a character quickly: out-of-combat Vitality returns at 0.3 + Vigor/10 every 10 s (1 per 10 s in the old game), and no cellar room is Safe.
3. **Timing of potions.** A person sees the hit, decides, types, and the potion then waits in the queue for the next Ready Time (up to 6 or 7 s for a Scout). The simulator looked at Vitality at each Ready Time and drank at once.
4. **Fleeing.** A person with no potion and little Vitality leaves. The simulator fought to the death.

One thing the simulator and the game agree on, and a person may not: the auto-attack stays on the Overseer when the minion arrives (`enemies[0]`). A player who types `kill minion` is not modelled.

## 3. The human model

`HumanModel` in `tests/balance_sim.py`. Every number is a field with a comment. Times are seconds. There are three versions; the reason for them is section 5.

| Name | What it is | low | **mid** | high | Believable range |
|---|---|---|---|---|---|
| `delay_median` | The wait before every typed command (skill, potion, flee, the `kill` that starts a fight): read, decide, type. Log-normal, so mostly near the median and sometimes much longer. | 1.0 | **1.5** | 2.0 | 1 to 3 |
| `delay_sigma` | The spread of that delay (0.6 means 3.7 s at the 90th percentile of a 1.5 s median). | 0.5 | **0.6** | 0.7 | 0.4 to 0.9 |
| `p_distract` / `distract_s` | Chance that a command is also held up by a distraction, and by how long (uniform). | 3%, 6-20 s | **5%, 6-20 s** | 8%, 6-20 s | 2% to 10% |
| `p_skip_skill` | Chance in each action slot that the player types no skill and lets the auto-attack run. | 10% | **15%** | 25% | 5% to 40% |
| `drink_lo` / `drink_hi` | The potion is typed when Vitality falls to a share drawn per fight from this range (the perfect player: exactly 35%). | 28-35% | **25-35%** | 20-32% | 15% to 50% |
| `p_no_drink` | Chance that in a fight the player never types the potion nor the flee (frozen, fascinated, away). | 3% | **6%** | 10% | 0 to 15% |
| `flee_lo` / `flee_hi`, `flee` | With no potion left and Vitality at or below a share drawn per fight, the player types `flee`. | 25-35% | **25-35%** | 25-35% | 15% to 50% |
| `FLEE_SUCCESS` | The game: 75% per try; a failure is retried after another typing delay. A flight counts as a failed attempt. | 75% | 75% | 75% | fixed |
| `carry_over` | Vitality, Endurance and Focus at the start of a fight are what the last fight left, plus the rest the walk and the looting allow (`ROOM_SECONDS` 4 per room, `LOOT_SECONDS` 3 per goblin corpse, `pause_s` 3). | yes | **yes** | yes | the spec's reading |
| `rest_to` | Before the next door the player waits (out-of-combat regeneration only) until Vitality is at least this share. 0 is the strict reading: no deliberate rest. | 70% | **50%** | 0 | 0 to 100% |
| `topup_below` | Drinks potions between fights while below this share. Off in all three (section 5). | off | off | off | |

`HUMAN_ZERO` is the same engine with every habit off (no delays, no skipping, drink at exactly 35%, never frozen, no flight, full Vitality for every fight). It is the check that the engine is the perfect player: in the old-game calibration it gives 92% against the perfect model's 91%.

How a human fight runs (`HumanPolicy`, called from `fight()`): the player's typed commands arrive as events of their own, `delay` after the thing that prompted them (the start of the previous action for the next skill, the hit that lowered Vitality for a potion or a flight). A typed command waits in the queue until the next Ready Time, as in `COMBAT_TIMING.md`; a newer command replaces it. The human uses the calling's skill each slot (Scout `strike`, Mage `bolt`, Warrior `bash`, Priest `heal` at half Vitality and `bless` at the start of a hard fight) unless it is skipped or cannot be paid. The perfect model's random numbers are untouched: a policy draws from its own generator.

**The gear parameter.** `starting`: the calling's own weapon (Scout: Hunting Bow), no armour, no trinket, and a shield only where the calling starts with one (the Warrior). `best`: the best weapon of the calling's ladder, a shield where the calling carries one, and the +5 Vitality trinket. No armour is sold in the Hollow, so "what is sold" is the same as none until `docs/specs/hollow-expansion.md` is built; the recommendations try armour as a number.

## 4. The approach

`APPROACH` in the simulator, the rooms a player must cross, read from `mudlib/domains/hollow/cellars` (`Exits`, `AddSpawn`):

Tavern Cellar (3 Large Rats) - Collapsed Passage (2 Giant Rats) - Storage Chamber (2 Scavengers) - Smuggler Hall (empty) - Flooded Tunnel (Cave Toad) - Mushroom Cavern (3 Spore Bats) - Goblin Barracks (3 Warriors) - Overseer Hall (2 Elite Guards) - Overseer Chamber (Overseer, and the Minion he summons).

The Guard Post (its goblin guard and keyholder, for the rusty key and the Hidden Cache) is a side trip and not on the way. All creatures in a room fight together, as in the game. The player's level is fixed for the whole run (no experience is gained on the way).

**Potions.** "N potions" means N in the pack when the player opens the Overseer Hall's door. The rooms before it draw on a spare supply (the player has restocked) and what they use is reported as "on the way". The other reading, N in the pack from the first room, is much harsher (the middle human kills the old game's Scout 5% of the time with 4 potions from the entrance, against 17% from the Hall's door) and is available as `pack_at="entrance"`. The Storage Chamber's potion on the floor is not counted (`STORAGE_POTION`).

## 5. Calibration against the owner's evening

**What was observed.** Level 5 Scout, Hunting Bow, no armour, 4 potions, the game before attributes. He needed about 3 potions to get past the two guards, then had to flee the Overseer when he summoned the minion. The perfect model said 97% (`boss_stats` run again: 98%).

**What was fitted.** Only the human numbers were tuned (the creature and class numbers are the old game's, switched on by `LEGACY`, the old simulator's values). Two targets: about 3 potions spent on the guards; the Overseer lost or fled more often than not. `python3 tests/balance_sim.py calibrate` (3000 runs a row, 130 s):

| Model | Overseer killed | fled | died | Potions on the guards, mean | 3 or more | Vitality at the Overseer's door |
|---|---|---|---|---|---|---|
| perfect, from the Hall (no typing, full Vitality each fight) | 91% | 0 | 9% | 0.80 | 5% | 100% |
| the engine, every habit off | 92% | 0 | 8% | 0.80 | 5% | 100% |
| human-low | 44% | 48% | 7% | 1.49 | 15% | 70% |
| human-mid | 17% | 66% | 17% | 2.01 | 32% | 53% |
| human-high | 8% | 57% | 36% | 1.88 | 35% | 41% |

**How well it fits.**
- *Loses or flees more often than not:* yes in all three (56%, 83%, 92%). The commonest end is **fleeing from the Overseer**, 48% to 66% of runs, which is what the owner did. Fleeing at the minion's arrival is a good description of the mid model: the Overseer has 114 Vitality and the human arrives with 53% of his own.
- *About 3 potions on the guards:* not reproduced. The model averages 1.5 (low), 2.0 (mid), 1.9 (high). Three or more happens in 15%, 32% and 35% of runs; two or more in 47%, 67% and 62%. The most the model reaches with any believable setting is a mean of about 2.5 (carry-over alone, no resting, no other habit). Seeing 3 is therefore a roughly one-in-three event for the model, not an impossible one; it is also a single evening.

**Which assumption carries the most weight.** By far the carried-over resources. Starting from the perfect player and adding one habit at a time (old game, same scenario):

| Habit added alone to the perfect engine | Overseer killed |
|---|---|
| nothing (the engine) | 92% |
| Vitality, Endurance and Focus carried over | **33%** |
| typing delays | 91% |
| skipping the skill 15% | 91% |
| potions drunk at 25-35% | 92% |
| 6% never drink | 88% |
| fleeing when out of potions | 81% (19% fled instead) |
| resting to 50% before each door (with carry-over) | 38% |

All the human habits together on top of the carry-over lower it from 33% to 24%; fleeing then moves most of the remaining deaths into flights (10%), because a flight counts as a failed attempt. In words: it is not that a person types slowly, it is that after seven rooms he arrives at the guards with about half his Vitality and little Endurance, and the guards take most of it back.

**What one evening cannot pin down.** Several numbers move the same two outputs in the same direction, so they cannot be told apart:
- *Resting.* Whether the owner waited between rooms is the one assumption the evening cannot separate from "the game is hard". With the other human numbers fixed, kill% against the Overseer and potions spent on the guards trade against each other as he rests more: no resting 10% and 2.18 potions; rest to 50%, 17% and 2.01; to 60%, 28% and 1.69; to 70%, 40% and 1.42; to 80%, 55% and 1.13. The evening ("lose, and 3 potions") argues for little resting; "loses more often than not" tolerates resting up to about 70%. The three versions therefore differ chiefly here (70%, 50%, none).
- *Typing speed, skipping, the potion threshold:* in the old game a slower typist also delays the creatures (they wait their Reaction Time from the moment he starts the fight), so these three numbers change the result by only a few points. The evening says nothing about them. They are set from what is believable and from how they behave in the current game, where they matter somewhat more.
- *Where the 4 potions are carried* (Hall door or entrance), the Storage Chamber's potion, whether he drank between rooms, whether he stopped to kill the minion: unknown.

Low is the mildest person who still loses more often than not; high never rests and is clumsier; mid is in between and is the one used for the recommendations. If the owner remembers that he did rest between rooms, use low; if he did not, high.

## 6. The current game

Everything from here on is the current game: section 8 formulas, attribute points placed, aggressive creatures engaging on arrival. Percentages of runs that end with the Overseer dead, from the cellar, one attempt. 3000 runs a cell. "Perfect" is the simulator's own player, as in every other table.

**Table A. What each habit costs in the current game** (`ablate`, level 5, 4 potions at the Hall, starting gear, 2000 runs a cell):

| | warrior | scout | priest | mage | adventurer | mean |
|---|---|---|---|---|---|---|
| perfect (the simulator's player) | 93 | 91 | 95 | 99 | 94 | 94 |
| + the arrival ambush (waits his own Reaction Time) | 90 | 87 | 94 | 95 | 91 | 91 |
| + Vitality, Endurance, Focus carried over | 24 | 32 | 38 | 6 | 25 | 25 |
| + typing delays | 25 | 28 | 36 | 5 | 24 | 24 |
| + skips the skill 15% | 28 | 28 | 37 | 5 | 22 | 24 |
| + potion at 25-35% | 26 | 24 | 34 | 3 | 20 | 22 |
| + never drinks 6% | 21 | 21 | 32 | 4 | 16 | 19 |
| + flees when out of potions (no resting) | 7 | 10 | 15 | 1 | 6 | 8 |
| + rests to 50% before each door (= human-mid) | 13 | 17 | 20 | 4 | 11 | 13 |

**Table H1a. Starting gear (own weapon, no armour, no trinket).**

| calling | level | potions | perfect | low | mid | high |
|---|---|---|---|---|---|---|
| warrior | 4 | 2 | 25 | 1 | 0 | 0 |
| warrior | 4 | 4 | 54 | 8 | 2 | 1 |
| warrior | 5 | 2 | 75 | 10 | 1 | 0 |
| warrior | 5 | 4 | 93 | 39 | 14 | 7 |
| scout | 4 | 2 | 19 | 1 | 0 | 0 |
| scout | 4 | 4 | 55 | 10 | 3 | 1 |
| scout | 5 | 2 | 60 | 5 | 1 | 0 |
| scout | 5 | 4 | 90 | 37 | 17 | 7 |
| priest | 4 | 2 | 40 | 1 | 0 | 0 |
| priest | 4 | 4 | 67 | 9 | 3 | 2 |
| priest | 5 | 2 | 84 | 16 | 3 | 2 |
| priest | 5 | 4 | 96 | 48 | 20 | 13 |
| mage | 4 | 2 | 29 | 0 | 0 | 0 |
| mage | 4 | 4 | 80 | 2 | 0 | 0 |
| mage | 5 | 2 | 83 | 2 | 0 | 0 |
| mage | 5 | 4 | 98 | 22 | 4 | 1 |
| adventurer | 4 | 2 | 27 | 0 | 0 | 0 |
| adventurer | 4 | 4 | 62 | 8 | 2 | 1 |
| adventurer | 5 | 2 | 74 | 6 | 0 | 0 |
| adventurer | 5 | 4 | 94 | 34 | 11 | 4 |

**Table H1b. Best gear the Hollow sells (best weapon, shield for the callings that carry one, +5 Vitality trinket; no armour is sold).**

| calling | level | potions | perfect | low | mid | high |
|---|---|---|---|---|---|---|
| warrior | 4 | 2 | 58 | 4 | 1 | 0 |
| warrior | 4 | 4 | 81 | 25 | 7 | 4 |
| warrior | 5 | 2 | 91 | 26 | 6 | 2 |
| warrior | 5 | 4 | 99 | 60 | 30 | 19 |
| scout | 4 | 2 | 59 | 7 | 2 | 1 |
| scout | 4 | 4 | 86 | 36 | 16 | 8 |
| scout | 5 | 2 | 89 | 26 | 8 | 2 |
| scout | 5 | 4 | 97 | 68 | 39 | 22 |
| priest | 4 | 2 | 57 | 4 | 1 | 0 |
| priest | 4 | 4 | 80 | 20 | 6 | 4 |
| priest | 5 | 2 | 92 | 34 | 8 | 6 |
| priest | 5 | 4 | 99 | 69 | 32 | 27 |
| mage | 4 | 2 | 49 | 0 | 0 | 0 |
| mage | 4 | 4 | 90 | 6 | 0 | 0 |
| mage | 5 | 2 | 93 | 3 | 0 | 0 |
| mage | 5 | 4 | 100 | 36 | 5 | 1 |
| adventurer | 4 | 2 | 56 | 4 | 1 | 0 |
| adventurer | 4 | 4 | 81 | 24 | 9 | 5 |
| adventurer | 5 | 2 | 91 | 28 | 7 | 2 |
| adventurer | 5 | 4 | 98 | 64 | 31 | 16 |

Reading them: at level 5 with 4 potions and starting gear the middle human wins 14% (Warrior), 17% (Scout), 20% (Priest), 4% (Mage), 11% (Adventurer). With best gear, 30, 39, 32, 5, 31. At level 4 nobody wins (0% to 7% with 4 potions). The perfect player's figures in the 2-potion rows are the old table 2 of the simulator, shifted by the approach (the perfect player now also spends potions on the way and in the Hall).

**Where the runs end and what the potions go on** (middle human, level 5, 4 potions, starting gear; "on the way" is the spare supply before the Hall):

| calling | killed | potions on the way | on the guards | on the Overseer | Vitality at his door | commonest ends |
|---|---|---|---|---|---|---|
| warrior | 14% | 1.31 | 2.31 | 1.30 | 51% | fled Chamber 68%, fled Hall 7%, died Chamber 5%, died Hall 4% |
| scout | 17% | 2.03 | 1.88 | 1.33 | 55% | fled Chamber 56%, died Hall 11%, died Chamber 10%, died Barracks 4% |
| priest | 20% | 0.81 | 1.98 | 1.53 | 55% | fled Chamber 63%, died Chamber 7%, died Hall 6%, fled Hall 4% |
| mage | 4% | 3.11 | 2.07 | 0.90 | 56% | fled Chamber 59%, died Hall 18%, died Chamber 11%, died Barracks 6% |
| adventurer | 11% | 1.63 | 2.25 | 1.25 | 51% | fled Chamber 68%, died Hall 7%, died Chamber 6%, fled Hall 5% |

For the perfect player the same table reads: killed 90% to 98%, 0.3 to 0.8 potions on the guards, 1.7 to 2.0 on the Overseer, the rest of the runs ending in the Chamber. For the middle human the Overseer is mostly lost by **fleeing from him**; the Hall (the guards) is where most of the deaths are; the Mage also dies in the Barracks, because he is the thinnest.

## 7. Recommendations

Target: a level 5 character in starting gear with 4 potions and points placed beats the Overseer about 70% of the time under the middle human, with the callings within a few points. `recommend`, 3000 runs a cell for the middle human, a third of that for the low and the high. "Spread" is highest minus lowest calling.

| Option | warrior | scout | priest | mage | adv. | mean | spread | low human | high human |
|---|---|---|---|---|---|---|---|---|---|
| the game as it is | 14 | 17 | 20 | 4 | 11 | 13 | 16 | 36 | 7 |
| 1a  the Overseer's Summon taken away | 20 | 23 | 34 | 7 | 16 | 20 | 28 | 52 | 11 |
| 1b  Elite Guards 40 to 32 Vitality, damage 3-5 to 2-4 | 32 | 36 | 43 | 15 | 28 | 31 | 29 | 60 | 18 |
| 1c  one Elite Guard in the Hall instead of two | 50 | 51 | 56 | 34 | 44 | 47 | 22 | 75 | 29 |
| 2a  Minor Healing Potion heals 30 (was 20) | 53 | 59 | 55 | 33 | 49 | 50 | 26 | 73 | 34 |
| 2b  Minor Healing Potion heals 35 | 72 | 71 | 69 | 49 | 68 | 66 | 23 | 82 | 48 |
| **2c  = 2b and the Mage's Vitality 4 + 8 a level (was 6)** | 72 | 71 | 69 | 65 | 68 | **69** | **7** | 85 | 50 |
| 3a  armour sold: Leather Jerkin 2 + Cap 1 (Mage one less) | 65 | 50 | 78 | 16 | 43 | 50 | 62 | 75 | 39 |
| 3b  = 3a and Overseer 100 Vitality, damage 3-6 | 83 | 70 | 92 | 35 | 64 | 69 | 57 | 89 | 53 |
| 3c  = 3b and the Mage's Vitality 4 + 8 a level | 83 | 70 | 92 | 47 | 64 | 71 | 46 | 92 | 54 |
| 4a  Guards 32 / 2-4, Overseer 100 / 3-6, no Summon | 71 | 74 | 91 | 57 | 69 | 73 | 34 | 94 | 51 |
| 4b  = 4a and the Mage's Vitality 4 + 8 a level | 71 | 74 | 91 | 68 | 69 | 75 | 23 | 95 | 52 |
| **4c  = 4b and the Priest's Minor Heal heals 5 (was 8)** | 71 | 74 | 64 | 68 | 69 | **69** | **10** | 93 | 45 |
| 5   one Elite Guard, no Summon | 66 | 67 | 80 | 52 | 61 | 65 | 28 | 87 | 42 |

**Three alternatives, in order of how little they change:**

- **A. Potions (2c).** The potion heals 35 instead of 20, and the Mage gets 8 Vitality a level instead of 6. Two numbers; 69% mean, 65-72% for every calling. The slope is steep (30 gives 50%, 35 gives 66%), so it is the number to try first and to move in steps of 2 or 3. Cautions: the potion is also the corridor's healing, and `CORRIDOR_BALANCE.md` and the pace model are built on 20 HP for 15 gold; a stronger potion makes the corridor easier and may want a higher price. I have not rerun them. With a perfect player the table 2 figures (level 4, 2 potions, best gear) go from 74 / 77 / 70 / 70 / 72 to 93 / 93 / 89 / 98 / 93.
- **B. The creatures (4c).** Elite Guard 32 Vitality and damage 2-4; Overseer 100 Vitality and damage 3-6; no Summon; Mage Vitality 8 a level; Priest's Minor Heal 5. Seven numbers in five files; 69%, spread 10. This is the "make the fight smaller" route, with no change to potions or the corridor. The perfect player goes to 96-100% at level 4. If you want fewer numbers: **1c alone** (one Elite Guard) gives 47%, one guard and no Summon (row 5) 65%, though with a spread of 28 points (Priest 80, Mage 52) that wants the same calling corrections.
- **C. Armour (3a to 3c).** The planned Leather Jerkin and Cap (+3) give only 50% alone, and they widen the spread (Warrior 65, Priest 78, Scout 50, Adventurer 43, Mage 16) because they stack on the Warrior's shield and base armour, and the Mage cannot wear the jerkin. With the Overseer at 100 Vitality and damage 3-6 it reaches 69% but the Mage is at 35% (47% with 8 Vitality a level). Armour is worth building for its own sake; it is not by itself the answer, and it needs calling corrections.

**What does not help much.** Taking the Summon away on its own moves the middle human from 13% to 20%. The owner met the minion as the moment he gave up, but the cause was the 53% of Vitality he brought to the Overseer and the potions the guards ate. Making the minion's Vitality smaller changes nothing at all, because the player never attacks it (the auto-attack stays on the Overseer); only its damage matters. The lever that matters is the Hall: one Elite Guard fewer moves the middle human's mean from 13% to 47%, the Summon's removal from 13% to 20%.

The callings stay in the same order in every option: the Mage is the thinnest at level 5 (44 Vitality, against 48 for the Scout, 62 Priest, 70 Adventurer, 75 Warrior) and the Priest, who can heal from a Focus pool, is the strongest in a long fight. Those two are the ones the options pull apart, which is why each alternative ends with a Mage (and for B a Priest) correction. It is also the part of the model I trust least, because the Mage and Priest use their skills with the simplest possible policy.

## 8. What cannot be concluded from one evening

- **The numbers are not the owner's.** One evening is one sample from a distribution that, for this level and gear, the model puts at 8% to 44% success. A single loss does not distinguish them. It supports "much less than 97%" and "a person who arrives hurt loses". It does not support any particular percentage.
- **The three human versions fit the evening about equally.** The differences between their predictions (13% against 36% against 7% in the current game) are the real uncertainty, and they are mostly the rest assumption.
- **The model is mostly resource carry-over.** Typing speed and similar habits matter little here. That finding is robust; the precise numbers are not. Whether a real player rests, buys potions between rooms, or leaves for the inn and the bed (5 gold, full Vitality) is the thing to ask the owner and, better, to watch on a few more evenings. If players routinely rest, the game is easier than this model says (rest to 80% gives 55% kills in the old game).
- **Things that are not modelled:** killing the minion first; fighting at the doors (stepping back a room to separate the guards); the Guard Post and the Hidden Cache's +5 Vitality (only in the `best` gear); buying potions mid-run; experience gained on the way (a level-up heals fully); Aimed Shot and the bow's ranged opening from `hollow-expansion.md`; the Power Strike's wind-up warning, which a person can use to bash or drink and the simulator's human cannot.
- **Calling policies for the human are simple.** One skill each slot, Priest heals at half Vitality, Warrior bashes whenever it can pay; no calling uses the Power Strike's warning. The Warrior's, Priest's and Mage's numbers are therefore less trustworthy than the Scout's, which is the only calling calibrated.
- **3000 runs a cell** gives about plus or minus 1 point on a middle value (3 in a third of that); differences of a few points between neighbouring cells are noise.

## 9. How to run it, and how long it takes

| Command | What | Time (3000 runs a cell unless stated) |
|---|---|---|
| `python3 tests/balance_sim.py` (and `runs`, `check`, `corridor`, `pace`) | everything it printed before, byte for byte under the perfect model (checked: 300 runs, same output) | about 110 s at 300 runs |
| `python3 tests/balance_sim.py calibrate [runs]` | section 5, old game | 130 s |
| `python3 tests/balance_sim.py ablate [runs]` | table A, 2000 runs a cell is the default | a few minutes |
| `python3 tests/balance_sim.py human [runs]` | tables H1 and the ends | 1110 s (18.5 min) |
| `python3 tests/balance_sim.py recommend [runs] [row prefix ...]` | section 7; a prefix such as `4c` runs one row | 700 s (12 min) |

The human tables are off by default, behind those four words. The random numbers of the perfect model are never touched.
