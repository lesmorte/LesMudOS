# The archer Scout: how easy is it, and what would change it

*24 November 2026. Written with `python3 tests/balance_sim.py scout` (new). Nothing in the game has been changed: every option below is a switch in the simulator for the owner to choose from. Branch `work/scout-sim`.*

## 1. Recommendation

**What the owner saw is real, and the simulator reproduces it.** A played Scout who sneaks, with the starting Hunting Bow, no armour and one potion, goes from level 1 to level 5 through the cellars in **30.7 simulated minutes** (the owner: 29), with **2.7 potions** (the owner: 2) and **0.39 deaths** a run (68% of runs have no death at all, so "no deaths" was an ordinary evening, not a lucky one; the Warrior's run has 0.17). The first action (the unnoticed opener) kills 19% of everything he kills, and 16% of the creatures die before they have done anything at all.

**But three things in the numbers change what can be done about it:**

1. **The standard test cannot tell a sneaking archer from one who does not sneak** (90.1 against 89.2, rank I). The archer's lead over the mean of the five callings (+10.9) is Aimed Shot itself, as `CLASS_SYSTEM.md` section 14 already found, not the sneak opener. So **no option that leaves a Scout who does not sneak alone can bring the standard-test number down**: options A, B, C and D (which only touch the unnoticed opener) change the archer's standard-test result by less than the noise (0.3 to 0.7 points). Only option E, which makes Aimed Shot dearer for everybody, moves it.
2. **Options A, B and C do almost nothing to the cellar run either.** An ordinary x1.75 Aimed Shot already kills a rat, a scavenger or a bat, so lowering the opener from x2.5 to x1.75 changes the share killed by the first action from 19% to 17%. The second notice roll (B) fires only one and a half to two and a half times in a whole run (1.5 for B1, 2.6 for B2), because from level 3 an ordinary creature's chance to notice the Scout sits at the 10% floor. The extra points per level (C) touch only creatures above the Scout's level, which from level 3 is the two Elite Guards alone, and the "Elite" flag costs the **melee** Scout 2.6 points.
3. **The Elite Guards are not an archer problem.** At level 3 a Warrior in starting gear beats the pair as often as the archer does (91% each); the archer with the bow wins 91% at level 3 and 98% at level 4, and in 0% of runs does it take no damage. They notice a sneaking Scout 45% (level 3) or 40% (level 4) of the time each, so the unnoticed opener lands on both only 30% to 36% of the time, and even then it does not kill one (40 Vitality).

**So the recommendation is two separate steps. Take the first; take the second only if the owner wants the standard-test number in the band, knowing what it costs.**

**Step 1: D2, the opener may not deal more than 60% of the target's maximum Vitality** (75% measures the same). It touches only the Aimed Shot that starts a fight from hiding. A rat (12 Vitality) takes at most 7 from the opener and now needs a second shot; a goblin warrior (28) at most 16. Effect on the sneaking archer, Hunting Bow, 1500 runs:

| | now | with D2 |
|---|---|---|
| creatures killed by the first action | 19% | **7%** |
| creatures killed before they acted | 16% | 11% |
| minutes from level 1 to level 5 | 30.7 | 33.7 |
| deaths a run / potions used | 0.39 / 2.72 | 0.41 / 2.74 (the same, within noise) |
| the same, with the Warden's Recurve from level 3: first-action kills / minutes | 22% / 24.9 | 10% / 27.2 |
| standard test, archer, rank I / rank II | 90.1 / 93.3 | 89.8 / 93.4 (the same) |

What it does to everyone else: **nothing**. The melee Scout (83.7 / 86.9), the Scout or archer who does not sneak (82.2 and 89.2), the five callings, and the Elite Guards pair (archer level 3: 91% to 89%, level 4: 98% to 98%) are unchanged within noise; the six standard-test numbers for the five callings are identical to the decimal because the option never reaches their code. It does not make the cellar hard; it takes away the one-shot feel and costs about three minutes of pace.

**Step 2 (only if the standard-test number must be in the 72 to 83 band): E2, Aimed Shot costs 45 Endurance at rank I** (the default step then gives 52 at rank II). Standard test, rank I: archer **90.1 to 83.3** (mean of the five 79.2); rank II: 93.3 to 90.0 (mean 91.9, so 1.9 below it). 40 Endurance (E1) gives 85.1 / 89.4, just outside the band at rank I; 50 (E3) gives 83.1 / 87.1 and adds nothing at rank I. The cost: **it lowers every archer, sneaking or not** (the one who does not sneak goes 89.2 to 83.7 at rank I), and it slows the cellar run by about three minutes for both (sneaking 30.7 to 33.8, not sneaking 33.7 to 37.1). It does not touch the melee Scout (83.7 stays) or the five callings. The brief's wish "without making a Scout who does not sneak worse" cannot be met together with "bring the standard test into the band": section 4 says why.

**Both together (D2 + E2),** measured: standard test 84.7 / 88.9; cellar run 36.1 minutes (+5.4), 0.45 deaths, 2.86 potions, 7% killed by the first action, 12% killed before they acted; with the Recurve 29.0 minutes and 0.33 deaths; the Elite Guards pair 85% at level 3 and 96% at level 4. (Within noise of the two alone: the combination's standard-test number is 1.4 above E2's, which is inside the spread of section 3.)

**Pace** (section 6) is not a Scout problem alone. Under the current experience table the cellars take **30.7 minutes (archer), 33.6 (melee Scout), 36.4 (Adventurer), 46.0 (Warrior), 52.0 (Mage) and 53.1 (Priest)** to level 5. The Scout is the fastest, the Adventurer is six minutes behind, and the three that fight slowly or must rest for Focus take half as long again. A clean pass through every room is worth only 812 experience (level 5 needs 1000), so every calling needs a second partial pass; waiting for resets is 2 to 5 minutes of the Scout's 31 (12 of the Mage's 52). If "level 5 in half an hour" is the thing to change, it is the experience table or the cellars' reward, not Aimed Shot.

Not recommended, with the numbers: A (no measurable effect), B (no measurable effect, any of its three variants), C (costs the melee Scout), and F, the notice floor, which I added because it is the real reason an unnoticed start is so common (at level 3 and above the floor of 10% is what every ordinary creature rolls): a floor of 20% cuts hidden fights from 52% to 44% and free kills from 16% to 13%, but it lowers the melee Scout (rank II 86.9 to 84.2) and does not change the standard test.

## 2. How the simulator was checked against the game

I read `tests/balance_sim.py` (all of it), `docs/BALANCE_HUMAN.md`, `docs/specs/sneak.md`, the Aimed Shot section of `docs/specs/skill-ranks.md`, `mudlib/skills/aimed_shot.cs`, `mudlib/skills/precision_strike.cs`, `mudlib/secure/sneak.cs`, `mudlib/std/living.cs` (`BeginFight`, `AddEnemy`, `UseSkill`, `Attack`), `mudlib/std/adventurer.cs` (`CmdSkill`, the sneaking section, `CheckLevelUp`), `mudlib/std/monster.cs` and the cellar rooms and creatures.

**Matches the game (no change needed):** Aimed Shot x1.75 and, as the shot that starts the fight, x2.5, +3 to hit, 30 Endurance, Preparation 1.0 (2 s) and Recovery 1.0, spoiled by a blow that lands in its Preparation with the Endurance spent and half the Recovery still paid; the notice chance (50 - 5 per Grace above 5 + 5 per level difference, kept 10 to 90, + 30 for Alert, + the rank's change, kept 5 to 100, a Boss always notices) and the roll in tenths of a percent; Alert on the Goblin Guard (and the keyholder) and the Elite Guard, Boss on the Overseer, every cellar creature Aggressive; the target's first action delayed by its Reaction Time x 2 s, a further 2 s if the attacker holds a bow, and a further Reaction Time x 2 s if the attacker is unnoticed (surprised); the other creatures of the room woken at the Scout's first loud act and waiting their own Reaction Time; Precision Strike's ambush x1.5; level-ups healing Vitality only (not Endurance); the experience table. `python3 tests/balance_sim.py check` passes.

**Differences found, and fixed in the simulator** (both are switches that default to the game; with both off the old numbers come back exactly, 81.1 / 83.3 / 76.4 / 72.5 / 82.3 / archer 89.9, 1500 runs, seed 1; with both on, 81.1 / 83.7 / 76.4 / 72.5 / 82.3 / 90.1, so neither fix moves a standard-test number by more than the noise):

1. **Re-rolls while the Scout is typing.** In the game a creature that has not noticed the Scout rolls again every 10 seconds at half the chance (`SneakBeat`); a command typed after a distraction (6 to 20 extra seconds in the human model) can come after one or two beats. The simulator rolled once. Now `SIM_REROLL` (default on) gives each unaware creature the extra rolls, and a noticing creature turns the fight into an ordinary start.
2. **The ranged opening for the melee Scout.** The melee Scout of the standard test holds the Hunting Bow (his starting weapon) and uses Precision Strike. In the game `OpenFight` gives the target the extra 2 seconds for any character whose weapon category is `bow`, whatever skill starts the fight. The simulator gave it to the archer only. Now `SIM_BOW_STRIKE` (default on) gives it to every Scout holding a bow.

**Smaller differences left as they are** (same count of events, effect below the noise): the game charges the sneak Endurance when the Scout leaves a room, the simulator on entering the next one; in the game a creature that notices on entry wakes the others only when the Scout next acts or they roll themselves, the simulator starts the whole room at once; a Scout too tired to sneak goes on openly until he types `sneak` again, the simulator tries again at every room; the Overseer Hall's door is not modelled (the guards are always met, as in the game); the brief's "1 s preparation" is 2 seconds in the files (Preparation 1.0 is one time unit of 2 s), which is what the simulator uses.

## 3. How it was measured, and how big the noise is

- **Standard test** (`BALANCE_HUMAN.md`): level 5, starting gear, 4 potions, points placed, the middle human, RestFactor 5, from the cellar entrance, kill% against the Overseer. **1500 runs a cell, seed 1** (the seed `approach_stats` takes). Rank I: nobody holds a rank. Rank II: everyone holds rank II. The Scouts sneak into every room they can pay for. "Scout" is the melee line (Precision Strike, with the Hunting Bow it starts with); "archer" opens with Aimed Shot.
- **Cellar run** (new, `cellar_run`): 1500 runs a row, seed 20261124. The assumptions are in section 8. **Elite Guards pair** (`hall_stats`): 1500 runs, seed 20261125, from the Barracks' door at full Vitality with 2 potions.
- **Noise.** The baseline over seeds 1 to 8 (table in section 7): the standard deviation of one 1500-run cell is 0.7 (archer) to 1.3 (Mage) points, which is the binomial 1.0 for a result near 80%. **The difference of two cells is therefore uncertain by about 1.5 points (one deviation), 3 points (two).** Read nothing into a difference below 2 points in the standard test; the apparent rise from E1 (85.1) to E1+B2 (87.4), and the small differences between A1, A2 and A3, are noise. The options share a seed, so cells are partly paired, but their random streams diverge as soon as an option changes a fight, so do not count on it. In the cellar run the standard error of the mean is 0.02 deaths a run and 0.2 to 0.3 minutes (in brackets in the tables); differences under 0.05 deaths and 0.6 minutes are noise.
- **The five callings do not move** except where they should: in the standard table the Warrior, Priest, Mage and Adventurer columns are the same to the decimal in every row (81.1 / 76.4 / 72.5 / 82.3 at rank I; 94.1 / 97.6 / 92.3 / 88.3 at rank II). The melee Scout moves only under C and F (they change the notice rolls every sneaker makes) and the non-sneaking Scouts only under E.

## 4. Why the standard test cannot be fixed without touching a Scout who does not sneak

The Overseer is a Boss: it always notices, so a sneaking Scout fights it exactly as one who does not (the room costs him 4 to 5 Endurance, which he has regained by the time he has sat down to rest). Everything the sneak opener does happens in earlier rooms, and the standard test only asks whether he survives all of them and the Overseer. Sneaking therefore adds 0.9 points to the archer (90.1 against 89.2) and 1.5 to the melee Scout (83.7 against 82.2). `docs/CLASS_SYSTEM.md` section 15 found the same (9.4 against 9.2). Whatever brings the archer's number down has to act on Aimed Shot as a whole, and so on the archer who does not sneak as well. That leaves one honest choice for the owner: **either the archer's standard-test number stays near 90 (the owner's decision of 22 November was that a strong archer is acceptable) and D2 takes the one-shot feel out of the cellars for sneakers only, or Aimed Shot costs more for everybody (E2).**

## 5. Tables

### 5.1 The standard test, every option

Kill% against the Overseer from the cellar entrance. "no sneak" columns: the same Scouts with sneaking switched off (they are what they are today, apart from E). Rank I: nobody holds a rank. Rank II: everyone holds rank II (level 5).

| option | rank | warrior | scout (melee) | priest | mage | adventurer | archer | mean of five | archer - mean | scout - mean | scout, no sneak | archer, no sneak |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| baseline: the game now | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 90.1 | 79.2 | +10.9 | +4.5 | 82.2 | 89.2 |
| baseline: the game now | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.3 | 91.9 | +1.5 | -4.9 | 83.7 | 92.5 |
| A1 opener x2.25 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.8 | 79.2 | +10.6 | +4.5 | 82.2 | 89.2 |
| A1 opener x2.25 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 92.7 | 91.9 | +0.9 | -4.9 | 83.7 | 92.5 |
| A2 opener x2.0 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.7 | 79.2 | +10.5 | +4.5 | 82.2 | 89.2 |
| A2 opener x2.0 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 94.0 | 91.9 | +2.1 | -4.9 | 83.7 | 92.5 |
| A3 opener x1.75 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.4 | 79.2 | +10.2 | +4.5 | 82.2 | 89.2 |
| A3 opener x1.75 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.1 | 91.9 | +1.3 | -4.9 | 83.7 | 92.5 |
| B1 second roll, entry chance | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.7 | 79.2 | +10.5 | +4.5 | 82.2 | 89.2 |
| B1 second roll, entry chance | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 92.5 | 91.9 | +0.7 | -4.9 | 83.7 | 92.5 |
| B2 second roll, entry + 15 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 90.1 | 79.2 | +10.9 | +4.5 | 82.2 | 89.2 |
| B2 second roll, entry + 15 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 92.7 | 91.9 | +0.9 | -4.9 | 83.7 | 92.5 |
| B3 second roll, entry / 2 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 90.3 | 79.2 | +11.1 | +4.5 | 82.2 | 89.2 |
| B3 second roll, entry / 2 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.3 | 91.9 | +1.5 | -4.9 | 83.7 | 92.5 |
| C1 +5 a level above, Elite +30 | I | 81.1 | 81.1 | 76.4 | 72.5 | 82.3 | 90.0 | 78.7 | +11.3 | +2.4 | 82.2 | 89.2 |
| C1 +5 a level above, Elite +30 | II | 94.1 | 85.3 | 97.6 | 92.3 | 88.3 | 92.3 | 91.5 | +0.8 | -6.2 | 83.7 | 92.5 |
| C2 +10 a level above, Elite +30 | I | 81.1 | 81.1 | 76.4 | 72.5 | 82.3 | 90.0 | 78.7 | +11.3 | +2.4 | 82.2 | 89.2 |
| C2 +10 a level above, Elite +30 | II | 94.1 | 85.3 | 97.6 | 92.3 | 88.3 | 92.3 | 91.5 | +0.8 | -6.2 | 83.7 | 92.5 |
| C3 +5 a level above only | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 90.1 | 79.2 | +10.9 | +4.5 | 82.2 | 89.2 |
| C3 +5 a level above only | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.3 | 91.9 | +1.5 | -4.9 | 83.7 | 92.5 |
| C4 Elite +30 only | I | 81.1 | 81.1 | 76.4 | 72.5 | 82.3 | 90.0 | 78.7 | +11.3 | +2.4 | 82.2 | 89.2 |
| C4 Elite +30 only | II | 94.1 | 85.3 | 97.6 | 92.3 | 88.3 | 92.3 | 91.5 | +0.8 | -6.2 | 83.7 | 92.5 |
| D1 opener cap 75% of max Vitality | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 90.5 | 79.2 | +11.3 | +4.5 | 82.2 | 89.2 |
| D1 opener cap 75% of max Vitality | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.9 | 91.9 | +2.1 | -4.9 | 83.7 | 92.5 |
| D2 opener cap 60% of max Vitality | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.8 | 79.2 | +10.6 | +4.5 | 82.2 | 89.2 |
| D2 opener cap 60% of max Vitality | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.4 | 91.9 | +1.5 | -4.9 | 83.7 | 92.5 |
| E1 Aimed Shot costs 40 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 85.1 | 79.2 | +5.9 | +4.5 | 82.2 | 85.4 |
| E1 Aimed Shot costs 40 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 89.4 | 91.9 | -2.5 | -4.9 | 83.7 | 90.0 |
| E2 Aimed Shot costs 45 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 83.3 | 79.2 | +4.1 | +4.5 | 82.2 | 83.7 |
| E2 Aimed Shot costs 45 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 90.0 | 91.9 | -1.9 | -4.9 | 83.7 | 86.5 |
| E3 Aimed Shot costs 50 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 83.1 | 79.2 | +3.9 | +4.5 | 82.2 | 83.1 |
| E3 Aimed Shot costs 50 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 87.1 | 91.9 | -4.8 | -4.9 | 83.7 | 83.5 |
| F1 notice floor 20% (not in the brief) | I | 81.1 | 84.3 | 76.4 | 72.5 | 82.3 | 89.6 | 79.3 | +10.3 | +5.0 | 82.2 | 89.2 |
| F1 notice floor 20% (not in the brief) | II | 94.1 | 84.2 | 97.6 | 92.3 | 88.3 | 92.9 | 91.3 | +1.6 | -7.1 | 83.7 | 92.5 |
| F2 notice floor 30% (not in the brief) | I | 81.1 | 82.5 | 76.4 | 72.5 | 82.3 | 89.1 | 78.9 | +10.2 | +3.5 | 82.2 | 89.2 |
| F2 notice floor 30% (not in the brief) | II | 94.1 | 84.9 | 97.6 | 92.3 | 88.3 | 93.6 | 91.4 | +2.2 | -6.6 | 83.7 | 92.5 |
| combo D2+B2 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.6 | 79.2 | +10.4 | +4.5 | 82.2 | 89.2 |
| combo D2+B2 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.0 | 91.9 | +1.1 | -4.9 | 83.7 | 92.5 |
| combo E1+D2 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 85.6 | 79.2 | +6.4 | +4.5 | 82.2 | 85.4 |
| combo E1+D2 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 90.7 | 91.9 | -1.2 | -4.9 | 83.7 | 90.0 |
| combo E1+B2 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 87.4 | 79.2 | +8.2 | +4.5 | 82.2 | 85.4 |
| combo E1+B2 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 90.5 | 91.9 | -1.4 | -4.9 | 83.7 | 90.0 |
| combo E1+D2+B2 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 86.2 | 79.2 | +7.0 | +4.5 | 82.2 | 85.4 |
| combo E1+D2+B2 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 90.1 | 91.9 | -1.7 | -4.9 | 83.7 | 90.0 |
| combo E2+D2 | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 84.7 | 79.2 | +5.5 | +4.5 | 82.2 | 83.7 |
| combo E2+D2 | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 88.9 | 91.9 | -2.9 | -4.9 | 83.7 | 86.5 |
| combo E1+D2+F1 | I | 81.1 | 84.3 | 76.4 | 72.5 | 82.3 | 85.7 | 79.3 | +6.4 | +5.0 | 82.2 | 85.4 |
| combo E1+D2+F1 | II | 94.1 | 84.2 | 97.6 | 92.3 | 88.3 | 90.0 | 91.3 | -1.3 | -7.1 | 83.7 | 90.0 |
| combo D2+B2+F1 | I | 81.1 | 84.3 | 76.4 | 72.5 | 82.3 | 89.6 | 79.3 | +10.3 | +5.0 | 82.2 | 89.2 |
| combo D2+B2+F1 | II | 94.1 | 84.2 | 97.6 | 92.3 | 88.3 | 93.1 | 91.3 | +1.8 | -7.1 | 83.7 | 92.5 |

### 5.2 The cellar run, archer Scout sneaking, Hunting Bow throughout

Level 1 to level 5, starting kit, one potion. "Creatures killed by the first action": of all creatures he kills, how many die to his first action of the fight. "Killed before they acted": the creature never began an action. "Vitality lost a fight": mean share of maximum Vitality lost in a room fight.

| option | deaths a run | potions used | creatures killed by the first action | creatures killed before they acted | fights with no damage taken | Vitality lost a fight | minutes to level 5 | reached level 5 | fights a run | fights opened from hiding |
|---|---|---|---|---|---|---|---|---|---|---|
| baseline: the game now | 0.39 (+-0.02) | 2.72 | 19% | 16% | 6% | 44% | 30.7 (+-0.2) | 100% | 18.0 | 52% |
| A1 opener x2.25 | 0.38 (+-0.02) | 2.73 | 18% | 16% | 5% | 44% | 31.0 (+-0.2) | 100% | 18.0 | 53% |
| A2 opener x2.0 | 0.39 (+-0.02) | 2.74 | 18% | 15% | 5% | 45% | 30.7 (+-0.2) | 100% | 17.9 | 53% |
| A3 opener x1.75 | 0.39 (+-0.02) | 2.72 | 17% | 15% | 5% | 45% | 31.1 (+-0.2) | 100% | 18.0 | 52% |
| B1 second roll, entry chance | 0.40 (+-0.02) | 2.73 | 19% | 15% | 5% | 44% | 30.8 (+-0.2) | 100% | 18.0 | 53% |
| B2 second roll, entry + 15 | 0.42 (+-0.02) | 2.73 | 18% | 14% | 5% | 45% | 31.1 (+-0.2) | 100% | 18.0 | 53% |
| B3 second roll, entry / 2 | 0.38 (+-0.02) | 2.72 | 18% | 15% | 6% | 44% | 30.7 (+-0.2) | 100% | 17.8 | 52% |
| C1 +5 a level above, Elite +30 | 0.42 (+-0.02) | 2.79 | 19% | 16% | 6% | 45% | 31.6 (+-0.2) | 100% | 18.2 | 49% |
| C2 +10 a level above, Elite +30 | 0.45 (+-0.02) | 2.75 | 19% | 15% | 6% | 45% | 31.8 (+-0.2) | 99% | 18.3 | 48% |
| C3 +5 a level above only | 0.40 (+-0.02) | 2.73 | 19% | 15% | 6% | 44% | 31.0 (+-0.2) | 100% | 17.9 | 50% |
| C4 Elite +30 only | 0.42 (+-0.02) | 2.75 | 19% | 16% | 6% | 44% | 30.5 (+-0.2) | 100% | 18.0 | 50% |
| D1 opener cap 75% of max Vitality | 0.41 (+-0.02) | 2.72 | 7% | 11% | 4% | 46% | 33.5 (+-0.3) | 100% | 18.4 | 52% |
| D2 opener cap 60% of max Vitality | 0.41 (+-0.02) | 2.74 | 7% | 11% | 4% | 47% | 33.7 (+-0.3) | 100% | 18.3 | 53% |
| E1 Aimed Shot costs 40 | 0.47 (+-0.02) | 2.85 | 19% | 16% | 5% | 45% | 33.5 (+-0.2) | 100% | 18.8 | 53% |
| E2 Aimed Shot costs 45 | 0.45 (+-0.02) | 2.87 | 18% | 16% | 5% | 46% | 33.8 (+-0.2) | 100% | 18.8 | 54% |
| E3 Aimed Shot costs 50 | 0.48 (+-0.02) | 2.87 | 18% | 16% | 5% | 46% | 35.0 (+-0.2) | 99% | 19.1 | 53% |
| F1 notice floor 20% (not in the brief) | 0.38 (+-0.02) | 2.74 | 19% | 13% | 5% | 45% | 31.0 (+-0.2) | 100% | 18.0 | 44% |
| F2 notice floor 30% (not in the brief) | 0.40 (+-0.02) | 2.78 | 18% | 10% | 5% | 45% | 31.6 (+-0.2) | 100% | 18.1 | 35% |
| combo D2+B2 | 0.40 (+-0.02) | 2.72 | 10% | 11% | 4% | 46% | 32.6 (+-0.3) | 100% | 18.2 | 53% |
| combo E1+D2 | 0.41 (+-0.02) | 2.86 | 7% | 11% | 3% | 48% | 35.7 (+-0.3) | 99% | 19.0 | 54% |
| combo E1+B2 | 0.42 (+-0.02) | 2.86 | 18% | 14% | 5% | 46% | 33.5 (+-0.2) | 100% | 18.7 | 53% |
| combo E1+D2+B2 | 0.44 (+-0.02) | 2.86 | 11% | 11% | 3% | 47% | 34.8 (+-0.2) | 100% | 18.8 | 54% |
| combo E2+D2 | 0.45 (+-0.02) | 2.86 | 7% | 12% | 3% | 48% | 36.1 (+-0.3) | 100% | 19.1 | 54% |
| combo E1+D2+F1 | 0.44 (+-0.02) | 2.91 | 9% | 9% | 3% | 48% | 36.0 (+-0.3) | 100% | 19.1 | 45% |
| combo D2+B2+F1 | 0.38 (+-0.02) | 2.79 | 12% | 9% | 3% | 47% | 33.0 (+-0.2) | 99% | 18.3 | 44% |

### 5.3 The same, with the Warden's Recurve (13 to 19) from level 3

The Recurve is simply in his hands from level 3 (see section 8).

| option | deaths a run | potions used | creatures killed by the first action | creatures killed before they acted | fights with no damage taken | Vitality lost a fight | minutes to level 5 | reached level 5 | fights a run | fights opened from hiding |
|---|---|---|---|---|---|---|---|---|---|---|
| baseline: the game now | 0.26 (+-0.01) | 1.94 | 22% | 17% | 8% | 40% | 24.9 (+-0.2) | 100% | 15.6 | 50% |
| A1 opener x2.25 | 0.26 (+-0.01) | 2.01 | 21% | 17% | 8% | 40% | 25.2 (+-0.2) | 100% | 15.8 | 49% |
| A2 opener x2.0 | 0.24 (+-0.01) | 1.98 | 21% | 17% | 8% | 40% | 24.8 (+-0.2) | 100% | 15.6 | 49% |
| A3 opener x1.75 | 0.25 (+-0.01) | 1.99 | 21% | 16% | 8% | 40% | 24.8 (+-0.2) | 100% | 15.6 | 49% |
| B1 second roll, entry chance | 0.26 (+-0.01) | 2.02 | 21% | 16% | 8% | 40% | 24.6 (+-0.2) | 100% | 15.6 | 49% |
| B2 second roll, entry + 15 | 0.24 (+-0.01) | 1.97 | 21% | 16% | 7% | 40% | 24.9 (+-0.2) | 100% | 15.6 | 49% |
| B3 second roll, entry / 2 | 0.27 (+-0.01) | 2.00 | 22% | 17% | 8% | 40% | 25.2 (+-0.2) | 100% | 15.7 | 50% |
| C1 +5 a level above, Elite +30 | 0.26 (+-0.01) | 2.02 | 21% | 16% | 8% | 40% | 24.9 (+-0.2) | 100% | 15.7 | 46% |
| C2 +10 a level above, Elite +30 | 0.27 (+-0.01) | 2.06 | 22% | 16% | 8% | 41% | 24.9 (+-0.2) | 100% | 15.8 | 44% |
| C3 +5 a level above only | 0.25 (+-0.01) | 1.99 | 22% | 17% | 8% | 40% | 25.4 (+-0.2) | 100% | 15.7 | 48% |
| C4 Elite +30 only | 0.25 (+-0.01) | 2.01 | 22% | 16% | 8% | 40% | 24.8 (+-0.2) | 100% | 15.6 | 47% |
| D1 opener cap 75% of max Vitality | 0.28 (+-0.01) | 2.05 | 10% | 12% | 6% | 43% | 27.2 (+-0.2) | 100% | 16.0 | 50% |
| D2 opener cap 60% of max Vitality | 0.29 (+-0.01) | 2.06 | 10% | 12% | 6% | 43% | 27.2 (+-0.2) | 100% | 16.0 | 50% |
| E1 Aimed Shot costs 40 | 0.27 (+-0.01) | 2.23 | 22% | 17% | 8% | 42% | 26.5 (+-0.2) | 100% | 16.1 | 50% |
| E2 Aimed Shot costs 45 | 0.32 (+-0.01) | 2.22 | 21% | 17% | 7% | 42% | 27.2 (+-0.2) | 100% | 16.2 | 50% |
| E3 Aimed Shot costs 50 | 0.31 (+-0.01) | 2.32 | 21% | 17% | 7% | 43% | 27.8 (+-0.2) | 100% | 16.4 | 50% |
| F1 notice floor 20% (not in the brief) | 0.25 (+-0.01) | 2.01 | 21% | 14% | 7% | 41% | 24.8 (+-0.2) | 100% | 15.6 | 42% |
| F2 notice floor 30% (not in the brief) | 0.29 (+-0.01) | 2.03 | 21% | 11% | 7% | 41% | 25.1 (+-0.2) | 100% | 15.7 | 35% |
| combo D2+B2 | 0.25 (+-0.01) | 2.08 | 13% | 12% | 6% | 43% | 26.5 (+-0.2) | 100% | 15.9 | 49% |
| combo E1+D2 | 0.29 (+-0.01) | 2.30 | 10% | 13% | 5% | 45% | 29.0 (+-0.3) | 100% | 16.6 | 50% |
| combo E1+B2 | 0.32 (+-0.01) | 2.27 | 21% | 16% | 7% | 42% | 27.0 (+-0.2) | 100% | 16.3 | 50% |
| combo E1+D2+B2 | 0.32 (+-0.01) | 2.28 | 13% | 13% | 6% | 44% | 28.5 (+-0.2) | 100% | 16.5 | 50% |
| combo E2+D2 | 0.33 (+-0.02) | 2.31 | 10% | 13% | 5% | 45% | 29.0 (+-0.2) | 100% | 16.6 | 50% |
| combo E1+D2+F1 | 0.32 (+-0.01) | 2.27 | 12% | 11% | 5% | 45% | 29.0 (+-0.3) | 100% | 16.5 | 43% |
| combo D2+B2+F1 | 0.26 (+-0.01) | 2.07 | 15% | 10% | 6% | 43% | 26.5 (+-0.2) | 100% | 16.0 | 43% |

### 5.4 The archer who does not sneak

Options A to D and F do not reach a Scout who does not sneak (identical rows); E does.

| option | deaths a run | potions used | creatures killed by the first action | creatures killed before they acted | fights with no damage taken | Vitality lost a fight | minutes to level 5 | reached level 5 | fights a run | fights opened from hiding |
|---|---|---|---|---|---|---|---|---|---|---|
| baseline: the game now | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| A1 opener x2.25 | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| A2 opener x2.0 | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| A3 opener x1.75 | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| B1 second roll, entry chance | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| B2 second roll, entry + 15 | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| B3 second roll, entry / 2 | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| C1 +5 a level above, Elite +30 | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| C2 +10 a level above, Elite +30 | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| C3 +5 a level above only | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| C4 Elite +30 only | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| D1 opener cap 75% of max Vitality | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| D2 opener cap 60% of max Vitality | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| E1 Aimed Shot costs 40 | 0.45 (+-0.02) | 3.00 | 18% | 0% | 1% | 49% | 35.8 (+-0.3) | 100% | 19.3 | 0% |
| E2 Aimed Shot costs 45 | 0.48 (+-0.02) | 3.03 | 18% | 0% | 1% | 49% | 37.1 (+-0.3) | 100% | 19.6 | 0% |
| E3 Aimed Shot costs 50 | 0.41 (+-0.02) | 3.07 | 18% | 0% | 1% | 50% | 38.1 (+-0.3) | 99% | 19.9 | 0% |
| F1 notice floor 20% (not in the brief) | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| F2 notice floor 30% (not in the brief) | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| combo D2+B2 | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| combo E1+D2 | 0.45 (+-0.02) | 3.00 | 18% | 0% | 1% | 49% | 35.8 (+-0.3) | 100% | 19.3 | 0% |
| combo E1+B2 | 0.45 (+-0.02) | 3.00 | 18% | 0% | 1% | 49% | 35.8 (+-0.3) | 100% | 19.3 | 0% |
| combo E1+D2+B2 | 0.45 (+-0.02) | 3.00 | 18% | 0% | 1% | 49% | 35.8 (+-0.3) | 100% | 19.3 | 0% |
| combo E2+D2 | 0.48 (+-0.02) | 3.03 | 18% | 0% | 1% | 49% | 37.1 (+-0.3) | 100% | 19.6 | 0% |
| combo E1+D2+F1 | 0.45 (+-0.02) | 3.00 | 18% | 0% | 1% | 49% | 35.8 (+-0.3) | 100% | 19.3 | 0% |
| combo D2+B2+F1 | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |

### 5.5 Which creatures the first action kills (archer sneaking, Hunting Bow)

Each cell: share killed by the first action / share killed before they acted.

| creature (Vitality) | kills a run | baseline: the game now | A3 opener x1.75 | B2 second roll, entry + 15 | D1 opener cap 75% of max Vitality | D2 opener cap 60% of max Vitality | combo D2+B2 | combo E1+D2 | combo E1+B2 | combo E1+D2+B2 | combo E2+D2 | combo E1+D2+F1 | combo D2+B2+F1 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Large Rat (12) | 7.13 | 26% / 17% | 26% / 17% | 26% / 16% | 13% / 15% | 12% / 15% | 17% / 15% | 13% / 15% | 26% / 16% | 17% / 15% | 12% / 16% | 15% / 12% | 19% / 12% |
| Giant Rat (15) | 4.50 | 34% / 29% | 30% / 29% | 32% / 28% | 10% / 27% | 10% / 26% | 16% / 26% | 10% / 27% | 32% / 28% | 16% / 26% | 10% / 26% | 13% / 23% | 19% / 22% |
| Goblin Scavenger (13) | 4.92 | 36% / 24% | 35% / 23% | 35% / 24% | 13% / 5% | 12% / 5% | 20% / 11% | 12% / 7% | 33% / 23% | 19% / 12% | 12% / 7% | 15% / 6% | 23% / 10% |
| Spore Bat (9) | 5.82 | 22% / 16% | 22% / 16% | 22% / 15% | 11% / 7% | 12% / 7% | 14% / 9% | 11% / 8% | 22% / 15% | 15% / 10% | 11% / 9% | 14% / 6% | 16% / 7% |
| Goblin Guard (26) | 4.49 | 5% / 9% | 1% / 8% | 3% / 4% | 1% / 8% | 1% / 7% | 1% / 4% | 1% / 8% | 3% / 5% | 1% / 4% | 1% / 8% | 1% / 6% | 1% / 2% |
| Goblin Warrior (28) | 5.13 | 6% / 12% | 0% / 10% | 5% / 9% | 0% / 10% | 0% / 9% | 0% / 7% | 0% / 9% | 4% / 9% | 0% / 6% | 0% / 9% | 0% / 7% | 0% / 5% |
| Goblin Elite Guard (40) | 2.64 | 0% / 5% | 0% / 2% | 0% / 2% | 0% / 5% | 0% / 5% | 0% / 2% | 0% / 5% | 0% / 3% | 0% / 3% | 0% / 6% | 0% / 4% | 0% / 1% |
| Giant Cave Toad (66) | 1.82 | 0% / 5% | 0% / 1% | 0% / 4% | 0% / 5% | 0% / 5% | 0% / 4% | 0% / 1% | 0% / 1% | 0% / 1% | 0% / 1% | 0% / 1% | 0% / 3% |

### 5.6 The yardstick: the other callings, and the Scouts as they are now

A Warrior's run is what "a normal run" looks like in this model.

| option | deaths a run | potions used | creatures killed by the first action | creatures killed before they acted | fights with no damage taken | Vitality lost a fight | minutes to level 5 | reached level 5 | fights a run | fights opened from hiding |
|---|---|---|---|---|---|---|---|---|---|---|
| warrior (the yardstick) | 0.17 (+-0.01) | 2.84 | 4% | 0% | 0% | 45% | 46.0 (+-0.2) | 100% | 19.4 | 0% |
| melee Scout, sneaking | 0.43 (+-0.02) | 2.91 | 13% | 14% | 3% | 47% | 33.6 (+-0.2) | 100% | 18.7 | 53% |
| melee Scout, not sneaking | 0.48 (+-0.02) | 3.00 | 7% | 0% | 0% | 52% | 39.9 (+-0.3) | 99% | 20.0 | 0% |
| archer, not sneaking | 0.41 (+-0.02) | 2.87 | 18% | 0% | 2% | 48% | 33.7 (+-0.3) | 100% | 18.6 | 0% |
| priest | 0.24 (+-0.01) | 3.06 | 1% | 0% | 0% | 76% | 53.2 (+-0.4) | 100% | 21.8 | 0% |
| mage | 0.53 (+-0.02) | 3.30 | 3% | 0% | 0% | 59% | 52.0 (+-0.4) | 99% | 22.6 | 0% |
| adventurer | 0.25 (+-0.01) | 2.99 | 7% | 0% | 0% | 48% | 36.4 (+-0.2) | 99% | 19.1 | 0% |

### 5.7 The Elite Guards pair

Both guards (40 Vitality each, level 4, Alert) alone, the character of that level at full Vitality, 2 potions, middle human. Cell: win%, and in brackets the share of runs in which he lost no Vitality. Baseline row: the chance that neither guard notices a sneaking Scout is 30% at level 3 and 36% at level 4.

| option | archer L3 | archer L3 + Recurve | archer L4 | archer L4 + Recurve | melee Scout L3 | warrior L3 |
|---|---|---|---|---|---|---|
| baseline: the game now | 91 (0) | 97 (1) | 98 (0) | 100 (2) | 83 (0) | 91 (0) |
| A1 opener x2.25 | 89 (0) | 97 (0) | 98 (0) | 99 (1) | 83 (0) | 91 (0) |
| A2 opener x2.0 | 91 (0) | 97 (1) | 97 (0) | 100 (1) | 83 (0) | 91 (0) |
| A3 opener x1.75 | 90 (0) | 97 (0) | 97 (0) | 100 (1) | 83 (0) | 91 (0) |
| B1 second roll, entry chance | 90 (0) | 97 (1) | 98 (0) | 100 (1) | 83 (0) | 91 (0) |
| B2 second roll, entry + 15 | 89 (0) | 97 (1) | 98 (0) | 100 (2) | 83 (0) | 91 (0) |
| B3 second roll, entry / 2 | 91 (0) | 97 (1) | 98 (0) | 99 (2) | 83 (0) | 91 (0) |
| C1 +5 a level above, Elite +30 | 89 (0) | 96 (0) | 98 (0) | 99 (1) | 80 (0) | 91 (0) |
| C2 +10 a level above, Elite +30 | 89 (0) | 96 (0) | 98 (0) | 99 (1) | 80 (0) | 91 (0) |
| C3 +5 a level above only | 91 (0) | 97 (1) | 98 (0) | 100 (2) | 81 (0) | 91 (0) |
| C4 Elite +30 only | 90 (0) | 96 (0) | 98 (0) | 99 (1) | 79 (0) | 91 (0) |
| D1 opener cap 75% of max Vitality | 91 (0) | 97 (1) | 98 (0) | 100 (2) | 83 (0) | 91 (0) |
| D2 opener cap 60% of max Vitality | 89 (0) | 97 (1) | 98 (0) | 100 (1) | 83 (0) | 91 (0) |
| E1 Aimed Shot costs 40 | 88 (0) | 97 (1) | 97 (0) | 99 (2) | 83 (0) | 91 (0) |
| E2 Aimed Shot costs 45 | 86 (0) | 96 (1) | 95 (0) | 99 (2) | 83 (0) | 91 (0) |
| E3 Aimed Shot costs 50 | 84 (0) | 94 (1) | 97 (0) | 99 (2) | 83 (0) | 91 (0) |
| F1 notice floor 20% (not in the brief) | 91 (0) | 97 (1) | 98 (0) | 100 (1) | 81 (0) | 91 (0) |
| F2 notice floor 30% (not in the brief) | 90 (0) | 98 (1) | 97 (0) | 100 (1) | 81 (0) | 91 (0) |
| combo D2+B2 | 92 (0) | 96 (0) | 98 (0) | 100 (1) | 83 (0) | 91 (0) |
| combo E1+D2 | 88 (0) | 96 (1) | 97 (0) | 99 (1) | 83 (0) | 91 (0) |
| combo E1+B2 | 87 (0) | 96 (1) | 97 (0) | 99 (1) | 83 (0) | 91 (0) |
| combo E1+D2+B2 | 87 (0) | 96 (0) | 97 (0) | 100 (1) | 83 (0) | 91 (0) |
| combo E2+D2 | 85 (0) | 96 (0) | 96 (0) | 99 (1) | 83 (0) | 91 (0) |
| combo E1+D2+F1 | 87 (0) | 95 (1) | 96 (0) | 100 (1) | 81 (0) | 91 (0) |
| combo D2+B2+F1 | 91 (0) | 97 (0) | 97 (0) | 99 (1) | 81 (0) | 91 (0) |

## 6. Pace

Minutes of simulated play to reach each level, same progression, the starting kit, one potion, the game's experience table (100 / 300 / 600 / 1000 for levels 2 to 5), the middle human with `rest`. "Waiting for resets" is the time spent standing out of the cellars until the creatures return (the rest is fighting, walking and sitting). The Scouts sneak.

| calling | level 2 | level 3 | level 4 | level 5 | of which waiting for resets | reached level 5 | deaths a run | potions used |
|---|---|---|---|---|---|---|---|---|
| warrior | 10.2 | 18.9 | 31.4 | 46.0 (+-0.2) | 2.4 | 100% | 0.17 | 2.84 |
| scout (melee, sneaking) | 8.1 | 14.0 | 22.2 | 33.6 (+-0.2) | 3.9 | 100% | 0.43 | 2.91 |
| priest | 14.5 | 24.8 | 38.1 | 53.1 (+-0.4) | 7.6 | 100% | 0.24 | 3.06 |
| mage | 16.1 | 26.2 | 38.8 | 52.0 (+-0.4) | 12.5 | 99% | 0.53 | 3.30 |
| adventurer | 8.0 | 14.9 | 24.7 | 36.4 (+-0.2) | 2.6 | 99% | 0.25 | 2.99 |
| archer, sneaking | 7.5 | 13.2 | 20.4 | 30.7 (+-0.2) | 3.9 | 100% | 0.39 | 2.72 |
| archer, not sneaking | 8.5 | 14.9 | 22.9 | 33.7 (+-0.3) | 4.9 | 100% | 0.41 | 2.87 |

Experience in the cellars (from the mob files): a clean pass is Tavern Cellar 24, Collapsed Passage 24, Storage Chamber 44, Guard Post 64, Flooded Tunnel 50, Mushroom Cavern 60, Barracks 126, Overseer Hall 170, Overseer 250: 812 in all, 562 before the Overseer. The pass up to the Hall is 562, just short of level 4 (600), and in this model the Overseer is attempted only at level 4, so every calling has to go round again for creatures that have returned (the rats come back after 10 to 15 minutes, the goblins after 7 to 12). That is why every calling is slow in the last stretch, and why the pace belongs to the table and the rooms, not to one calling.

## 7. Notice chances, and the spread of the baseline

### 7.1 Which cellar creatures options C touches

| creature (level) | flags | Scout level 1 | Scout level 2 | Scout level 3 | Scout level 4 | Scout level 5 |
|---|---|---|---|---|---|---|
| **now** | | | | | | |
| Large Rat (1) | - | 30 | 15 | 10 | 10 | 10 |
| Giant Rat (1) | - | 30 | 15 | 10 | 10 | 10 |
| Goblin Scavenger (2) | - | 35 | 20 | 10 | 10 | 10 |
| Goblin Guard (2) | Alert | 65 | 50 | 40 | 40 | 40 |
| Spore Bat (2) | - | 35 | 20 | 10 | 10 | 10 |
| Giant Cave Toad (3) | - | 40 | 25 | 10 | 10 | 10 |
| Goblin Warrior (3) | - | 40 | 25 | 10 | 10 | 10 |
| Goblin Elite Guard (4) | Alert | 75 | 60 | 45 | 40 | 40 |
| Goblin Overseer (5) | Boss | always | always | always | always | always |
| **C1: +5 a level above, Elite +30** | | | | | | |
| Large Rat (1) | - | 30 | 15 | 10 | 10 | 10 |
| Giant Rat (1) | - | 30 | 15 | 10 | 10 | 10 |
| Goblin Scavenger (2) | - | 40 | 20 | 10 | 10 | 10 |
| Goblin Guard (2) | Alert | 70 | 50 | 40 | 40 | 40 |
| Spore Bat (2) | - | 40 | 20 | 10 | 10 | 10 |
| Giant Cave Toad (3) | - | 50 | 30 | 10 | 10 | 10 |
| Goblin Warrior (3) | - | 50 | 30 | 10 | 10 | 10 |
| Goblin Elite Guard (4) | Alert Elite | 100 | 100 | 80 | 70 | 70 |
| Goblin Overseer (5) | Boss | always | always | always | always | always |
| **C2: +10 a level above, Elite +30** | | | | | | |
| Large Rat (1) | - | 30 | 15 | 10 | 10 | 10 |
| Giant Rat (1) | - | 30 | 15 | 10 | 10 | 10 |
| Goblin Scavenger (2) | - | 45 | 20 | 10 | 10 | 10 |
| Goblin Guard (2) | Alert | 75 | 50 | 40 | 40 | 40 |
| Spore Bat (2) | - | 45 | 20 | 10 | 10 | 10 |
| Giant Cave Toad (3) | - | 60 | 35 | 10 | 10 | 10 |
| Goblin Warrior (3) | - | 60 | 35 | 10 | 10 | 10 |
| Goblin Elite Guard (4) | Alert Elite | 100 | 100 | 85 | 70 | 70 |
| Goblin Overseer (5) | Boss | always | always | always | always | always |

Level 5 is the standard test: there the per-level points touch nothing (no creature is above level 5, and the Overseer is a Boss) and only the Elite flag matters (the guards go from 40% to 70%). At the Scout's levels 1 to 3 in the cellar run the per-level points touch the Scavengers, Guards and Bats (level 2) when he is level 1, the Toad and Warriors (level 3) when he is level 1 or 2, and the Elite Guards (level 4) when he is below 4. From level 3 only the Elite Guards are above him. The floor at 10% is what makes every ordinary creature notice a level 3 Scout one time in ten.

### 7.2 The baseline over eight seeds

| seed | warrior | scout | priest | mage | adventurer | archer |
|---|---|---|---|---|---|---|
| 1 | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 90.1 |
| 2 | 80.5 | 82.1 | 77.9 | 70.9 | 82.5 | 90.6 |
| 3 | 80.3 | 82.1 | 76.9 | 71.5 | 80.7 | 91.7 |
| 4 | 81.5 | 81.7 | 76.4 | 73.3 | 82.7 | 89.9 |
| 5 | 80.4 | 83.1 | 78.0 | 74.5 | 82.8 | 89.9 |
| 6 | 82.8 | 82.0 | 75.9 | 72.4 | 82.5 | 91.1 |
| 7 | 81.2 | 82.8 | 75.1 | 74.3 | 82.5 | 89.9 |
| 8 | 80.5 | 84.1 | 77.8 | 71.8 | 84.2 | 90.9 |
| mean | 81.0 | 82.7 | 76.8 | 72.6 | 82.5 | 90.5 |
| sd of one cell | 0.85 | 0.86 | 1.04 | 1.29 | 0.96 | 0.68 |

## 8. What the simulator does not model

**The progression (assumptions I made; the owner's play differed in unknown ways).**
- **The route.** The character walks the cellars in room order (Tavern Cellar, Collapsed Passage, Storage Chamber, Guard Post, Flooded Tunnel, Mushroom Cavern, Goblin Barracks, Overseer Hall, Overseer Chamber), fights whatever is there, and goes through the Hall's door at level 3 and the Overseer's at level 4 (the owner took the guards at level 3). After a flight or a death he leaves that room alone until he gains a level or five minutes pass. Real players also fight outside the cellars (wolves and the like), buy and wear things, take quests; the owner's 29 minutes may include some of that. What matches is the order of magnitude, not the route.
- **Resets.** A room's creatures return the reset after their corpse is gone (420 s for goblins and bats, 600 s for rats and the toad, then 0 to 300 s), as `BALANCE_HUMAN.md` models for rest. The Storage Chamber's potion returns 0 to 300 s after it is taken. A rest that outlasts a reset and brings a creature back to the room where he sits (the `refills` of the standard test) is not modelled in the progression.
- **Potions and gold.** One potion at the start, one from the Storage Chamber each lap; no buying, no armour, no trinket, no shield bought. Gold is ignored.
- **Death.** He wakes at full Vitality 90 s later with his potions (corpse looted); the room's creatures are as they were.
- **Level-ups and points.** The two attribute points of each level are placed at once (Grace for the Scout, as in every table); a level-up heals Vitality in the middle of the fight.
- **The Warden's Recurve** is in his hands from level 3 for free. Getting it (the keyholder's rusty key at the Guard Post, the locked door in the Mushroom Cavern, the Hidden Cache) costs time and a fight that the simulator does not charge, so the "with the Recurve" rows are a best case. The melee Scout is not given it.
- **Seated rests** use the middle human's patience rule (until full or 60 s, 1% chance of a long lingering) at RestFactor 5.

**The options (my readings of the brief).**
- **A:** every opener value is lowered by the same amount (x2.5, 3.0, 3.5 ... become x2.25, 2.75, 3.25 ... for A1), so rank II keeps its step. The ordinary factor stays x1.75 / 2.0 / ...
- **B:** one extra roll, for the first target only, when the opener is an Aimed Shot begun from hiding, at the entry chance, entry + 15 (kept at 100) or half the entry chance. If it notices, the shot is x1.75 and the target's first action is at its own Reaction Time x 2 s from that moment (no ranged opening, no surprise). The roll is made at the start of the Preparation; as every creature's first action is at least 3 seconds away, the moment inside the 2 seconds does not matter. A melee Scout's Precision Strike gets no such roll (it is not an Aimed Shot).
- **C:** the per-level points add N for each level the creature is above the Scout, inside the 10 to 90 clamp; "Elite" is a new flag on the Goblin Elite Guard and adds 30 on top of Alert (70% at level 4 and 5). Neither exists in the game. They apply to every sneaker, melee Scout included.
- **D:** only to the Aimed Shot that starts the fight from hiding; the damage is cut after the roll, crit and block, to a share of the target's maximum Vitality (rounded down, at least 1). Precision Strike's ambush is not capped.
- **E:** rank I costs 40 (E1), 45 (E2) or 50 (E3); later ranks follow the default step (15% of rank I: 46, 52, 58 for E1, E2 ...). It applies to every Aimed Shot, not only the opener.
- **F** (mine, not in the brief): the 10% floor of the base notice chance raised to 20% or 30%.

**Everything the human model leaves out** (see `BALANCE_HUMAN.md`): a player who reads the room, kites, or uses `flee` or `track` well; other players; creatures that wander. The Elite Guards test ignores that a real player arrives from the Barracks with what the Barracks left him. The standard test's 4 potions and the cellar run's 1 are both assumptions. The simulator's creature numbers match the mudlib (`check`); nothing in the game was changed.

## 9. Reproduce

```
python3 tests/balance_sim.py check
python3 tests/balance_sim.py scout notice          # 7.1
python3 tests/balance_sim.py scout std 1500        # 5.1   (about 30 minutes)
python3 tests/balance_sim.py scout cell 1500       # 5.2 to 5.5
python3 tests/balance_sim.py scout pace 1500       # 5.6 and 6
python3 tests/balance_sim.py scout hall 1500       # 5.7
python3 tests/balance_sim.py scout spread 1500     # 7.2
```
The switches: `scout_option(mode, opener, second, per_level, elite, cap, cost, floor)` for the options; `SIM_REROLL` and `SIM_BOW_STRIKE` for the two fixes of section 2; `OPT` holds the state.


## 10. Option G: a cap that depends on the level difference (added 24 November 2026)

The owner asked: what if the opener's cap were a formula of d = Scout level - creature level, lower when the creature is above the Scout, higher when the Scout is above it, so that a strong character one-shots rats? Measured like D2 (same seeds, 1500 runs a cell). The cap is a share of the target's maximum Vitality, and **no cap** once the share reaches 100%.

- **G1:** 60% + 20% x d, floor 40%, no cap from d = +2.
- **G2:** 60% + 15% x d, floor 30%, no cap from d = +3.
- **G3:** 50% + 25% x d, floor 25%, no cap from d = +2.
- Each is run twice: on the Aimed Shot opener only, and ("+ Precision Strike") on every from-hiding opener, so the melee Scout's ambush is capped the same way. For the archer the two are identical, so only the melee Scout's table tells them apart. D = the level used is the Scout's level at the moment the shot lands.

### 10.1 What d is

| creature | Vitality | level | d at Scout level 1 | d at level 3 | d at level 5 |
|---|---|---|---|---|---|
| Large Rat | 12 | 1 | +0 | +2 | +4 |
| Giant Rat | 15 | 1 | +0 | +2 | +4 |
| Goblin Scavenger | 13 | 2 | -1 | +1 | +3 |
| Spore Bat | 9 | 2 | -1 | +1 | +3 |
| Goblin Guard | 26 | 2 | -1 | +1 | +3 |
| Giant Cave Toad | 66 | 3 | -2 | +0 | +2 |
| Goblin Warrior | 28 | 3 | -2 | +0 | +2 |
| Goblin Elite Guard | 40 | 4 | -3 | -1 | +1 |
| Goblin Overseer | 114 | 5 | -4 | -2 | +0 |

The cap by d:

| option | d=-3 | d=-2 | d=-1 | d=0 | d=+1 | d=+2 | d=+3 |
|---|---|---|---|---|---|---|---|
| G1 60% + 20% x d, floor 40% | 40% | 40% | 40% | 60% | 80% | none | none |
| G2 60% + 15% x d, floor 30% | 30% | 30% | 45% | 60% | 75% | 90% | none |
| G3 50% + 25% x d, floor 25% | 25% | 25% | 25% | 50% | 75% | none | none |

So a level 1 Scout meets rats at d = 0 (60% cap: a rat takes 7 at most from the opener) and everything else below him (d -1 to -3: 30% to 40%); a level 3 Scout is at d = +2 against rats (uncapped under G1 and G3, 90% under G2) and at d = +1 against scavengers, bats and guards; a level 5 Scout is at +3 or +4 against the rats, scavengers, bats and guards (uncapped in all three), +2 against the toad and warriors (uncapped under G1 and G3, 90% under G2), and +1 against the Elite Guards (75% to 80%).

### 10.2 Standard test (1500 runs a cell, seed 1)

| option | rank | warrior | scout (melee) | priest | mage | adventurer | archer | mean of five | archer - mean | scout - mean | scout, no sneak | archer, no sneak |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| baseline: the game now | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 90.1 | 79.2 | +10.9 | +4.5 | 82.2 | 89.2 |
| baseline: the game now | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.3 | 91.9 | +1.5 | -4.9 | 83.7 | 92.5 |
| D2 flat cap 60% | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.8 | 79.2 | +10.6 | +4.5 | 82.2 | 89.2 |
| D2 flat cap 60% | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.4 | 91.9 | +1.5 | -4.9 | 83.7 | 92.5 |
| G1 60% + 20% x d, floor 40% | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.2 | 79.2 | +10.0 | +4.5 | 82.2 | 89.2 |
| G1 60% + 20% x d, floor 40% | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 94.3 | 91.9 | +2.4 | -4.9 | 83.7 | 92.5 |
| G2 60% + 15% x d, floor 30% | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.9 | 79.2 | +10.7 | +4.5 | 82.2 | 89.2 |
| G2 60% + 15% x d, floor 30% | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.6 | 91.9 | +1.7 | -4.9 | 83.7 | 92.5 |
| G3 50% + 25% x d, floor 25% | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.4 | 79.2 | +10.2 | +4.5 | 82.2 | 89.2 |
| G3 50% + 25% x d, floor 25% | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.3 | 91.9 | +1.4 | -4.9 | 83.7 | 92.5 |
| G1 + Precision Strike | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.2 | 79.2 | +10.0 | +4.5 | 82.2 | 89.2 |
| G1 + Precision Strike | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 94.3 | 91.9 | +2.4 | -4.9 | 83.7 | 92.5 |
| G2 + Precision Strike | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.9 | 79.2 | +10.7 | +4.5 | 82.2 | 89.2 |
| G2 + Precision Strike | II | 94.1 | 84.8 | 97.6 | 92.3 | 88.3 | 93.6 | 91.4 | +2.2 | -6.6 | 83.7 | 92.5 |
| G3 + Precision Strike | I | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.4 | 79.2 | +10.2 | +4.5 | 82.2 | 89.2 |
| G3 + Precision Strike | II | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.3 | 91.9 | +1.4 | -4.9 | 83.7 | 92.5 |

Nothing moves outside the noise (section 3): the archer is 89.2 to 89.9 at rank I and 93.3 to 94.3 at rank II against 90.1 / 93.3 now; the five callings are identical; the non-sneaking columns are untouched. The one cell that moves is the melee Scout at rank II under G2 + Precision Strike (86.9 to 84.8), 2 points, at the edge of the noise.

### 10.3 The archer's cellar run

Hunting Bow:

| option | deaths a run | potions used | killed by the first action | killed before they acted | fights with no damage | minutes to level 5 | first-action kills at level 1-2 | level 3 | level 4-5 |
|---|---|---|---|---|---|---|---|---|---|
| baseline: the game now | 0.39 | 2.72 | 19% | 16% | 6% | 30.7 (+-0.2) | 19% | 19% | 18% |
| D2 flat cap 60% | 0.41 | 2.74 | 7% | 11% | 4% | 33.7 (+-0.3) | 11% | 4% | 5% |
| G1 60% + 20% x d, floor 40% | 0.42 | 2.74 | 13% | 12% | 5% | 32.6 (+-0.2) | 11% | 11% | 16% |
| G2 60% + 15% x d, floor 30% | 0.40 | 2.73 | 9% | 11% | 4% | 33.5 (+-0.3) | 11% | 5% | 10% |
| G3 50% + 25% x d, floor 25% | 0.42 | 2.79 | 13% | 12% | 5% | 33.7 (+-0.3) | 11% | 11% | 17% |
| G1 + Precision Strike | 0.42 | 2.74 | 13% | 12% | 5% | 32.6 (+-0.2) | 11% | 11% | 16% |
| G2 + Precision Strike | 0.40 | 2.73 | 9% | 11% | 4% | 33.5 (+-0.3) | 11% | 5% | 10% |
| G3 + Precision Strike | 0.42 | 2.79 | 13% | 12% | 5% | 33.7 (+-0.3) | 11% | 11% | 17% |

Warden's Recurve from level 3:

| option | deaths a run | potions used | killed by the first action | killed before they acted | fights with no damage | minutes to level 5 | first-action kills at level 1-2 | level 3 | level 4-5 |
|---|---|---|---|---|---|---|---|---|---|
| baseline: the game now | 0.26 | 1.94 | 22% | 17% | 8% | 24.9 (+-0.2) | 20% | 22% | 26% |
| D2 flat cap 60% | 0.29 | 2.06 | 10% | 12% | 6% | 27.2 (+-0.2) | 11% | 7% | 10% |
| G1 60% + 20% x d, floor 40% | 0.29 | 2.10 | 15% | 13% | 7% | 27.0 (+-0.2) | 11% | 12% | 24% |
| G2 60% + 15% x d, floor 30% | 0.26 | 2.10 | 12% | 13% | 6% | 27.4 (+-0.2) | 11% | 7% | 16% |
| G3 50% + 25% x d, floor 25% | 0.27 | 2.12 | 15% | 13% | 6% | 27.7 (+-0.3) | 11% | 13% | 24% |
| G1 + Precision Strike | 0.29 | 2.10 | 15% | 13% | 7% | 27.0 (+-0.2) | 11% | 12% | 24% |
| G2 + Precision Strike | 0.26 | 2.10 | 12% | 13% | 6% | 27.4 (+-0.2) | 11% | 7% | 16% |
| G3 + Precision Strike | 0.27 | 2.12 | 15% | 13% | 6% | 27.7 (+-0.3) | 11% | 13% | 24% |

Archer who does not sneak (identical in every row: G reaches only an opener from hiding):

| option | deaths a run | potions used | killed by the first action | killed before they acted | fights with no damage | minutes to level 5 | first-action kills at level 1-2 | level 3 | level 4-5 |
|---|---|---|---|---|---|---|---|---|---|
| baseline: the game now | 0.41 | 2.87 | 18% | 0% | 2% | 33.7 (+-0.3) | 19% | 17% | 16% |
| D2 flat cap 60% | 0.41 | 2.87 | 18% | 0% | 2% | 33.7 (+-0.3) | 19% | 17% | 16% |
| G1 60% + 20% x d, floor 40% | 0.41 | 2.87 | 18% | 0% | 2% | 33.7 (+-0.3) | 19% | 17% | 16% |
| G2 60% + 15% x d, floor 30% | 0.41 | 2.87 | 18% | 0% | 2% | 33.7 (+-0.3) | 19% | 17% | 16% |
| G3 50% + 25% x d, floor 25% | 0.41 | 2.87 | 18% | 0% | 2% | 33.7 (+-0.3) | 19% | 17% | 16% |
| G1 + Precision Strike | 0.41 | 2.87 | 18% | 0% | 2% | 33.7 (+-0.3) | 19% | 17% | 16% |
| G2 + Precision Strike | 0.41 | 2.87 | 18% | 0% | 2% | 33.7 (+-0.3) | 19% | 17% | 16% |
| G3 + Precision Strike | 0.41 | 2.87 | 18% | 0% | 2% | 33.7 (+-0.3) | 19% | 17% | 16% |

Reading it: **G does what was asked in shape.** At levels 1 and 2 it behaves like D2 (11% of creatures killed by the first action against 19% now) because everything is at or above the Scout. From level 3 it opens up: with the Hunting Bow G1 and G3 give 11% at level 3 and 16% to 17% at level 4 and 5 (now 18% to 19%; D2 5%), G2 5% and 10%. So **G1 and G3 give back almost the whole one-shot at level 4 and 5, G2 about half of it**; D2 stays at 4% to 5% throughout. Overall first-action kills: baseline 19%, D2 7%, G2 9%, G1 and G3 13%. The cost in pace is 2 to 3 minutes (30.7 to 32.6 for G1, 33.5 for G2, 33.7 for G3, 33.7 for D2). Deaths (0.39 to 0.42), potions (2.72 to 2.79) and killed-before-acting (16% to 11-12%) are the same as D2 within noise.

### 10.4 The melee Scout, and the Elite Guards

| option | deaths a run | potions used | killed by the first action | killed before they acted | minutes to level 5 | first-action kills at level 1-2 | level 3 | level 4-5 |
|---|---|---|---|---|---|---|---|---|
| baseline: the game now | 0.43 | 2.91 | 13% | 14% | 33.6 | 12% | 15% | 13% |
| D2 flat cap 60% | 0.43 | 2.91 | 13% | 14% | 33.6 | 12% | 15% | 13% |
| G1 60% + 20% x d, floor 40% | 0.43 | 2.91 | 13% | 14% | 33.6 | 12% | 15% | 13% |
| G2 60% + 15% x d, floor 30% | 0.43 | 2.91 | 13% | 14% | 33.6 | 12% | 15% | 13% |
| G3 50% + 25% x d, floor 25% | 0.43 | 2.91 | 13% | 14% | 33.6 | 12% | 15% | 13% |
| G1 + Precision Strike | 0.47 | 2.92 | 8% | 11% | 35.1 | 4% | 8% | 13% |
| G2 + Precision Strike | 0.42 | 2.88 | 4% | 9% | 35.2 | 4% | 2% | 7% |
| G3 + Precision Strike | 0.44 | 2.95 | 8% | 11% | 35.2 | 4% | 8% | 13% |

G on the Aimed Shot alone leaves the melee Scout exactly as he is (identical rows). Applied to Precision Strike's ambush as well, his first-action kills fall from 13% to 8% (G1, G3) or 4% (G2), his pace slows from 33.6 to 35.1 / 35.2 minutes and deaths rise 0.43 to 0.47 for G1: a real cost to the line the owner did not call too easy.

The Elite Guards pair (win%, in brackets the share of runs without damage):

| option | archer L3 | archer L3 + Recurve | archer L4 | archer L4 + Recurve | melee Scout L3 | warrior L3 |
|---|---|---|---|---|---|---|
| baseline: the game now | 91 (0) | 97 (1) | 98 (0) | 100 (2) | 83 (0) | 91 (0) |
| D2 flat cap 60% | 89 (0) | 97 (1) | 98 (0) | 100 (1) | 83 (0) | 91 (0) |
| G1 60% + 20% x d, floor 40% | 89 (0) | 97 (1) | 98 (0) | 100 (1) | 83 (0) | 91 (0) |
| G2 60% + 15% x d, floor 30% | 90 (0) | 96 (1) | 98 (0) | 100 (1) | 83 (0) | 91 (0) |
| G3 50% + 25% x d, floor 25% | 89 (0) | 98 (0) | 98 (0) | 99 (1) | 83 (0) | 91 (0) |
| G1 + Precision Strike | 89 (0) | 97 (1) | 98 (0) | 100 (1) | 83 (0) | 91 (0) |
| G2 + Precision Strike | 90 (0) | 96 (1) | 98 (0) | 100 (1) | 83 (0) | 91 (0) |
| G3 + Precision Strike | 89 (0) | 98 (0) | 98 (0) | 99 (1) | 83 (0) | 91 (0) |

Unchanged by every variant (88 to 91% at level 3 and 98% at level 4 with the Hunting Bow): the guards are 40 Vitality at d = -1 (level 3) or d = 0 (level 4) and are capped at 40% to 60%, so a cap on the opener never was what decides that fight.

### 10.5 For reference: a Warrior's first action

| level 1-2 | level 3 | level 4-5 | all |
|---|---|---|---|
| 4.0% | 3.7% | 4.6% | 4.1% |

The Warrior kills 4% of what he kills with his first action at every level from 1 to 5, and none before it acts. This is not because his blow is weak: he does not open the fights. Every cellar creature is Aggressive and starts its own fight, and a Warrior (Reaction Time 3.0, so 6 s) waits longer than most creatures before his first action, which is usually the Shield Bash.

Extrapolation, not a simulator run: the chance that an ordinary blow that **hits** kills a full-Vitality Large Rat (12 Vitality) outright, for a Warrior who places one point in Might each level (Might 8 + level - 1) at +2% damage a point above 5, rounded as the game does it, no critical, no block:

| level | Might | rusty sword 9-14 | woodcutter axe 14-21 | fine longsword 12-18 |
|---|---|---|---|---|
| 1 | 8 | 66% | 100% | 100% |
| 3 | 10 | 66% | 100% | 100% |
| 5 | 12 | 66% | 100% | 100% |
| 8 | 15 | 83% | 100% | 100% |
| 10 | 17 | 83% | 100% | 100% |
| 12 | 19 | 100% | 100% | 100% |
| 15 | 22 | 100% | 100% | 100% |

With the starting rusty sword a Warrior's ordinary hit already kills a rat two times in three at level 1 (the damage rolls of 12 to 14 after the Might bonus), and it is a certainty from **level 12** (Might 19). With the woodcutter axe or the fine longsword (the Warrior's next two weapons) it is a certainty from level 1. So "a strong character one-shots rats" is a matter of weapon more than of level in these numbers; what the Scout has that the Warrior has not is the first action, the surprised target and the 10 s before the rat acts.

### 10.6 Recommendation on G

Of the three, **G2 (60% + 15% x d, floor 30%)** is the one to take if the owner prefers a level-scaled cap to the flat D2: it keeps the first-action kills at 9% overall (D2 7%), 5% at level 3 and 10% at level 4 and 5, so a Scout who is well above the room gets back some of the feeling of strength but not the whole of the one-shot, and it costs the same three minutes as D2. G1 and G3 un-cap at d = +2 and so restore 16% to 17% first-action kills at level 4 and 5 (the owner's complaint was about level 1 to 4, where they match D2 until level 3), and cost less pace (G1: 32.6 minutes). Apply it to **the Aimed Shot only**: putting G on Precision Strike's ambush lowers the melee Scout (first-action kills 13% to 4-8%, 1.5 minutes slower, deaths +0.04 for G1) and, at rank II, G2 + Precision Strike took the melee Scout's standard test from 86.9 to 84.8. As before, none of the G variants moves the standard-test number for the archer (89 to 94 against 90 to 93), so if that number must be in the 72 to 83 band the lever is still E2 (section 1, step 2).
