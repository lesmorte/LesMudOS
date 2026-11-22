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

## 10. Resting (the `rest` command, 21 November 2026)

*`docs/specs/rest.md` adds a `rest` command that multiplies regeneration of Vitality, Endurance and Focus by RestFactor while the character sits. This section is what the simulator says. Nothing in the game has been changed. Run it with `python3 tests/balance_sim.py rest [runs] [grid|sens|options|perfect|harvest|pace]`; the tables below are from those runs.*

### 10.1 The short version

- **Rest alone nearly does the job.** At RestFactor 5 the middle human beats the Overseer 73% of the time (level 5, starting gear, 4 potions), up from 13%. That is the owner's target. What it does not do is bring the callings together: Priest 84, Warrior 81, Adventurer 74, Scout 64, Mage 64 (spread 20 points).
- **RestFactor matters up to about 5, little after.** 3 gives 60%, 4 gives 69%, 5 gives 73%, 8 gives 76%. Past 5 the player is already getting up full inside his patience, so a faster rest only saves time.
- **The big unknown is the player, not the number.** How long he is willing to sit decides everything (10.3). A person who sits only 20 seconds gets 40% at RestFactor 5; one who sits two minutes gets 76%. The three human models give 79% (low), 73% (middle) and 35% (high) at RestFactor 5.
- **Recommendation: RestFactor 5, RestSurprise 2, plus three small calling numbers** (Mage Vitality 7 a level instead of 6, Scout 8 instead of 7, Priest's Minor Heal 7 instead of 8). That gives 81 / 71 / 78 / 73 / 74 (mean 75, spread 10). The perfect player's level 4, 2-potion table does not move (mean 72 today, 73 after).
- **One real risk to the spec:** a cleared room's creatures come back after a reset only once their corpses are gone. With corpses as built that is 7 minutes at the earliest, so sitting is safe. A player who skins, bleeds and loots every corpse at once (the crafting game) makes the corpses vanish, and then about one rest in four is interrupted (10.4); the kill rate falls from 73% to 42%.

### 10.2 What was modelled

**Resting as the spec has it.** After a fight the human loots (3 s a goblin corpse), sits down in the room he has just cleared, and stays seated until the *patience rule* ends it; then he walks on (4 s a room). While seated all three resources regenerate at RestFactor times the ordinary rate (the rates of `resources()`); looting and walking are at the ordinary rate. Time is counted: fighting, walking and looting, resting.

**The patience rule ("sit until content or bored")**: stand when Vitality reads full, or when the patience runs out, whichever comes first.

| Human | Stands up when | Patience | Why it is believable |
|---|---|---|---|
| low (the careful one) | all three resources are full (the "You are fully rested" message) | 120 s | waits for the message, and a couple of minutes between fights is what a MUD player expects |
| **middle** | Vitality is full | **60 s** | about as long as the old model had him wait (25 to 45 s at each door to reach 50%), stretched because the wait is now cheap |
| high (the clumsy one) | Vitality is full | 20 s | types `rest`, gets bored, goes on |

Each also has a small chance (low 0.5%, middle 1%, high 2%) per rest of staying seated for 1 to 10 minutes (away from the keyboard). This is the only way a rest outlasts a reset in the base tables. The old tables, with no rest command, are reproduced exactly (the baseline rows equal section 6 to the digit).

**Rooms that reset (read from the files).** Every cellar room resets every 300 s (`ResetSeconds` is the default 300 in `mudlib/std/room.cs`; no cellar file changes it) and none has an encounter. But a reset only replaces a creature whose corpse is gone (`Fill` counts a corpse as the creature), and a corpse lasts 600 s while it holds a skin (rats, toad) or 300 s of fresh blood plus 120 s empty (goblins, bats), unless the player takes the skin or blood. So a creature can return no earlier than the first reset after about 420 s (goblins, bats; Storage Chamber, Mushroom Cavern, Barracks, Overseer Hall) or 600 s (rats and toad; Tavern Cellar, Collapsed Passage, Flooded Tunnel), counted from each kill, then 0 to 300 s more. **Every room on the route is therefore safe for a rest of up to about 5 minutes** after the fight with corpses left alone, and the **Smuggler Hall has no spawn and is safe for ever.** The Overseer Chamber does not refill until he is dead. The simulator resolves it this way: the human rests only in the room he has just cleared; a rest that outlasts that moment (the earliest return time minus the length of the fight, plus a uniform 0 to 300 s) is interrupted, the creatures come back aggressive, and the player's first action waits his own Reaction Time x 2 plus RestSurprise. The new creatures are fought with whatever he has, and he sits down again by the same rule. A returning creature that finds the player resting is how RestSurprise acts.

**Not modelled:** the Adventurer's Survival Training (x1.5 out of combat) is not in any table in this document; with it on, the Adventurer at RestFactor 5 goes from 74% to 78% (1500 runs) and without the command from 11% to 14%. Resting at the Guard Post, in the village (the inn, 5 gold) or in a Safe room (regeneration doubled there) is not modelled either.

### 10.3 Headline table

Level 5, 4 potions at the Hall, starting gear, attributes placed, middle human; Overseer kill% from the cellar entrance. 3000 runs a cell (1500 for the low and high human). Plus or minus 1 to 2 points.

| | warrior | scout | priest | mage | adv. | mean | spread | low human | high human |
|---|---|---|---|---|---|---|---|---|---|
| no rest command | 14 | 17 | 20 | 4 | 11 | 13 | 16 | 36 | 7 |
| RestFactor 3 | 66 | 49 | 83 | 46 | 56 | 60 | 36 | 78 | 22 |
| RestFactor 4 | 76 | 60 | 85 | 56 | 68 | 69 | 29 | 79 | 29 |
| **RestFactor 5** | 81 | 64 | 84 | 64 | 74 | **73** | 20 | 79 | 35 |
| RestFactor 6 | 83 | 66 | 84 | 66 | 77 | 75 | 19 | 79 | 40 |
| RestFactor 8 | 84 | 68 | 85 | 66 | 78 | 76 | 19 | 79 | 48 |

The full grid (three humans, level 4 and 5, starting and best gear, 2 and 4 potions, each RestFactor) is in the simulator output (`rest grid`, 3000 runs a cell for the middle human, 1500 for the others, about 50 minutes). Middle human, **best** gear, level 5, 4 potions: none 30 / 39 / 32 / 5 / 31; RestFactor 3: 83 / 74 / 91 / 52 / 81; 5: 92 / 85 / 92 / 73 / 90; 8: 94 / 88 / 93 / 78 / 91. Level 5 with 2 potions, starting gear, RestFactor 5: 51 / 26 / 60 / 22 / 42. Level 4, 4 potions, starting gear, RestFactor 5: 36 / 29 / 41 / 27 / 34. Level 4 is still hard (2 potions: 3% to 15%), which is as intended.

**Potions, where runs end, minutes** (middle human, level 5, 4 potions, starting gear; potions are the whole run, including the spare supply on the way; minutes are for a *successful* run, fighting / walking and looting / resting):

| | potions used | where the others end | minutes (fight / walk / rest) |
|---|---|---|---|
| no rest command | 4.3 to 6.1 | fled at the Overseer 56% to 68%, died in the Hall 6% to 18% | 8 to 13 / 1.2 / 0.5 to 3.8 |
| RestFactor 3 | 2.2 to 3.4 | fled at the Overseer 11% to 35%, died there 4% to 16% | 8 to 13 / 1.2 / 3.1 to 5.9 |
| RestFactor 5 | 2.0 to 3.1 | fled 10% to 18%, died at the Overseer 4% to 16% | 8 to 13 / 1.2 / 2.9 to 4.3 |
| RestFactor 8 | 2.0 to 3.0 | fled 10% to 15%, died at the Overseer 3% to 18% | 8 to 13 / 1.2 / 2.1 to 3.0 |

With rest the player arrives at the Overseer's door with 94% to 100% of his Vitality (51% to 56% before) and uses about half the potions. The run takes 12 to 18 minutes either way: resting adds 1 to 3 minutes, and the old model's unpaid waiting is already inside the baseline's minutes. The Mage and Scout fight 8 to 9 minutes, the Warrior and Priest 13 (their fights are longer, with slower weapons).

**What the answer depends on** (middle human, level 5, 4 potions, starting gear, mean of the five callings, 1500 runs):

| Assumption | 20 s | 30 s | 60 s | 120 s |
|---|---|---|---|---|
| RestFactor 3, patience | 26 | 36 | 60 | 76 |
| RestFactor 5, patience | 40 | 54 | **74** | 76 |
| RestFactor 8, patience | 55 | 69 | 76 | 76 |

Sitting only until Vitality reaches 50 / 65 / 80 / 90 / 100% (RestFactor 5, 60 s): 13 / 30 / 54 / 66 / 74. So the question for the owner is **how long and how far a person rests**. The factor only decides how much he gets per second he is willing to give.

### 10.4 Interruptions and RestSurprise

With the corpses as built, an interrupted rest is rare (about 1 in 100 runs, only after a long linger) and RestSurprise changes nothing you can measure: with an exaggerated 20% of rests lingering, RestSurprise 0 / 2 / 5 / 10 s gives 74 / 72 / 72 / 72% (noise is plus or minus 2). **Keep RestSurprise at 2.**

The harvest case (`rest harvest`: corpses gone at once, so a room refills within 0 to 300 s of the last kill; middle human, RestFactor 5): 73% becomes 43% (RestSurprise 0), 42% (2), 42% (5), 39% (10), with about two fights per run started by returning creatures; RestSurprise 0 to 10 moves it by 4 points at most. The danger is the returning creature itself and the time, not the delay. If this matters, the fix is in the game, not in the number: for example, a reset does not refill a room that holds a resting player, or corpses persist while anyone sits in the room.

### 10.5 Recommendation and alternatives

Rest alone gives the owner's mean but not the callings together. The smallest tested additions: the Mage is lifted by Vitality, the Scout by Vitality, and the Priest, who is then the highest, trimmed by the heal. Level 5, 4 potions, middle human (3000 runs a cell), starting gear, no armour:

| Option | warrior | scout | priest | mage | adv. | mean | spread | low | high |
|---|---|---|---|---|---|---|---|---|---|
| rest alone, RestFactor 5 | 81 | 64 | 84 | 64 | 74 | 73 | 20 | 79 | 35 |
| **A. RestFactor 5, RestSurprise 2, Mage 4 + 7 a level (was 6), Scout 3 + 8 (was 7), Priest Minor Heal 7 (was 8)** | 81 | 71 | 78 | 73 | 74 | **75** | **10** | 81 | 33 |
| A'. as A with Minor Heal 6 | 81 | 71 | 72 | 73 | 74 | 74 | 10 | 80 | 31 |
| A-. as A, the Priest unchanged | 81 | 71 | 84 | 73 | 74 | 77 | 13 | 82 | 36 |
| C. RestFactor 4, Mage 8, Scout 9, Minor Heal 6 | 76 | 67 | 70 | 70 | 68 | 70 | 9 | 82 | 26 |
| D. RestFactor 5, Minor Healing Potion heals 25, no calling changes | 90 | 76 | 91 | 75 | 85 | 83 | 16 | 88 | 51 |
| E. RestFactor 3, Mage 8, Scout 8, Heal 6 | 66 | 52 | 66 | 56 | 56 | 59 | 14 | 79 | 19 |

- **A (recommended)**: smallest set that reaches a mean of 75 and a spread of 10. The Priest's Minor Heal is very steep: 8 gives 84, 7 gives 78, 6 gives 72, 5 gives 50. Move it by single steps. If you will not touch the Priest, A- keeps the spread at 13.
- **C** is nearest to 70 exactly, and has the narrowest spread, but changes four numbers and RestFactor 4; the Mage and Scout figures are +2 and +2 Vitality a level.
- **D** (a stronger potion) needs no calling number but gives 83 and a 51% high human, and the potion is also the corridor's healing (not rerun here).
- **E** shows a weak rest (3) does not work however the callings are corrected.

**Armour (Padded Jerkin 1, Leather Jerkin 2, Leather Cap 1).** I assumed a level 5 character has spent 135 gold on armour and potions: 4 potions (60), Leather Jerkin (50) and Cap (25); the Mage, who cannot wear the Leather Jerkin, has Padded Jerkin and Cap (45 gold, armour 2). A first pass through the cellars brings about 134 gold (Storage Chamber 10, Guard Post 16, Barracks 29, Hall 34, Hidden Cache 45) and the character starts with 10 to 20; level 5 takes more than one pass, so about 190 to 230 gold is in hand. Armour is the strongest single lever in the game: it adds 20 or more points.

| With armour bought | warrior | scout | priest | mage | adv. | mean | spread | low | high |
|---|---|---|---|---|---|---|---|---|---|
| no rest command | 65 | 50 | 78 | 16 | 43 | 50 | 62 | 74 | 38 |
| RestFactor 3 | 97 | 82 | 98 | 74 | 91 | 88 | 24 | 95 | 63 |
| RestFactor 5 | 98 | 87 | 99 | 83 | 95 | 93 | 16 | 95 | 71 |
| A | 98 | 92 | 98 | 89 | 95 | 95 | 10 | 96 | 73 |
| C | 98 | 92 | 96 | 89 | 94 | 94 | 9 | 97 | 70 |
| E | 97 | 86 | 96 | 82 | 91 | 90 | 14 | 97 | 63 |

So with option A an armoured character beats the Overseer 95% of the time (unarmoured 75%). If the owner wants the target to hold for a character *with* armour, RestFactor would have to be below 3 and the Mage would still lag (Mage 74 and the rest 91 to 98 at RestFactor 3): I would not choose that. I read "starting gear" as the unarmoured set used in every table here; armour is then the reward for shopping.

### 10.6 The other end: is the Overseer trivial for a good player?

Level 4, best gear (best weapon, shield where carried, +5 Vitality trinket), 2 potions, the Overseer alone from full Vitality (the table 2 of `ATTRIBUTES.md` section 8; 3000 fights a cell). Resting cannot change this row, because that player is full already; only the calling changes do.

| | warrior | scout | priest | mage | adv. | mean |
|---|---|---|---|---|---|---|
| the game as it is | 73 | 75 | 69 | 73 | 71 | 72 |
| option A | 73 | 80 | 61 | 79 | 71 | 73 |
| option C | 73 | 83 | 44 | 85 | 72 | 71 |
| (for comparison, the potion at 35, section 7 option 2c) | 93 | 93 | 89 | 99 | 92 | 93 |

The same character from the cellar entrance (1500 runs a cell), mean of the five callings: the simulator's perfect player, rested before every fight, 55% (today), 56% (A). A player with perfect hands who carries his resources and has **no** rest command: 2%. The same player resting by the middle human's patience rule at RestFactor 5: 42% today, 43% (A). So resting gives a skilled player what he already had in theory, it does not make level 4 trivial, and options A and C leave the quoted table 2 at 72 to 73. (Option C moves the Priest to 44 there; A to 61.)

### 10.7 What resting does to the pace of levels 1 to 5

`rest pace` (100 runs a cell): the route and farming of the simulator's whole run (section 3 of the report), time kept, the player waiting (or sitting) until all three resources are full between fights, and waiting for farmed rooms' creatures to come back (corpses 420 to 600 s plus up to 300 s). Mean minutes from the first step to level 5 (1000 experience):

| calling | no rest command | RestFactor 3 | RestFactor 5 | RestFactor 8 |
|---|---|---|---|---|
| warrior | 87 | 69 | 67 | 64 |
| scout | 87 | 70 | 64 | 62 |
| priest | 89 | 72 | 69 | 67 |
| mage | 94 | 67 | 62 | 58 |
| adventurer | 83 | 65 | 62 | 60 |

Levels 2, 3, 4 come at about 29 / 44 / 57 minutes at RestFactor 5 (35 / 53 / 70 without). For the Warrior at level 5: 24 minutes of fighting, 4 of walking, 39 of recovering (8 with RestFactor 5) and 22 to 31 of waiting for respawns. **Resting cuts the recovering by about 80% and the whole climb by about 25% to 35%; what is left is the wait for the rooms to refill, which resting does not touch.** This is a perfect-knowledge climb in which the cellars alone supply the experience, so it is slower than the "16 to 30 minutes" of `PLAN-corridor-crafting.md` and should be read only as a comparison. The corridor's targets (hours for levels 5 to 10, `CORRIDOR_BALANCE.md`) were not rerun: the pace model there has natural regeneration as 'the thing that dominates recovery', so a RestFactor of 5 will shorten them noticeably unless the corridor's experience table is raised. That needs a rerun of `python3 tests/balance_sim.py pace` with RestFactor in `regen_hp`; I did not do it.

### 10.8 What is still unknown

- **How long people sit.** Everything above rests on 20 / 60 / 120 s and on getting up at full Vitality. The sensitivity table in 10.3 shows the range (40% to 76% at RestFactor 5). Watching a few evenings, or logging `rest` durations once the command exists, would settle it.
- **Whether corpses will be cleared quickly.** The crafting game takes skins and blood; if most players do, rests are interrupted (10.4). The base tables assume corpses stay.
- **The Adventurer's Survival Training** is not in the tables (about +4 points at RestFactor 5).
- **The Priest, Warrior and Mage policies** are the simple ones of section 3; the Priest's steep sensitivity to Minor Heal in particular is a property of that simple policy and should be confirmed in play.
- Aimed shots, stepping back a room, and drinking potions between rooms are still not modelled (a person with spare potions would drink them while sitting, which would raise the figures).

### 10.9 Run times

`rest grid` 3000: about 50 minutes (320 cells of 3000 / 1500 runs); `rest sens`: 10 minutes; `rest options`: 25 minutes; `rest perfect`: 8 minutes; `rest harvest`: 4 minutes; `rest pace`: 18 minutes. Everything printed before this section is unchanged (300-run default output compared byte for byte).

## Aimed Shot, the archer Scout and the large Minor Heal (22 November 2026)

`python3 tests/balance_sim.py ranks 3000 aim` adds the archer Scout (a Scout with the Hunting Bow, or the Yew Longbow in the best-gear tables, that opens with Aimed Shot where it may and then aims whenever it can pay; a blow that lands during the 2-second wind-up spoils the shot) beside the five callings. Both player models are in it; the numbers and what would change them are in [CLASS_SYSTEM.md](CLASS_SYSTEM.md) section 14. The simulator now counts Minor Heal's 15-second wait in real time across fights (before, it was kept as a time inside one fight and carried into the next, which blocked heals for tens of seconds in the next room and made the Priest look 18 points lower than it is): with the owner's heal the Priest is 100 at the standard test, 18.6 above the mean at rank I and 10.1 at rank II (`ranks wait`: 15 s changes nothing, 60 s gives +13.9 and +9.1). Table entries above that used Minor Heal 7 or 8 are history.

