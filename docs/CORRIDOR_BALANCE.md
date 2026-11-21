# Corridor balance: creatures, pace and the experience table

Author: the `work/mobs` branch. Everything here is produced by `tests/balance_sim.py` (modes `check`, `corridor`, `pace`) and verified in
the driver by `tests/corridor_mob_test.py` and `tests/load_check.py corridor/mob`. This document proposed an experience table; the integration of the corridor applied it (section 4.3, `Adventurer.ExperienceFor`). The curve beyond level 10 is provisional.

## 1. What was built

* 31 creature files in `mudlib/domains/greenvale/corridor/mob/` (class names `Cm...`, ids and levels from the build map).
* A pace model and a Monte Carlo fight model for all five callings at levels 5 to 10 with village gear, in `tests/balance_sim.py`.
* `python3 tests/balance_sim.py check` now also compares every number in the 31 files with the simulation (level, Vitality, damage, to-hit,
  armor, dodge, experience, coins, Reaction Time, aggression, every action's preparation, recovery, multiplier and kind, damage factor
  and to-hit bonus, the opening/cycle/trigger pattern, the on-hit effects, the loot list).

## 2. Assumptions

### 2.1 The player

* Village gear only (nothing in the corridor drops better weapons or armor; the corridor may drop only the items that already exist).
  Warrior and Adventurer: Overseer's axe (17-25, reaction mod 1.5, recovery 1.5) and Small Shield. Scout: Fine Iron Longsword (12-18).
  Priest: Novice's Mace (9-13) and Small Shield. Mage: Iron Dagger (6-9) as melee fallback, arcane bolt 6-9 flat. All: Commander's Vest
  (armor 3), Guild Helm (armor 1), Trinket (+5 Vitality). Total armor 4 (plus 1 for a shield, 2 more for the warrior's training).
* A perfect-knowledge player: picks the right action at the right time, drinks a potion when it pays, uses abilities when the model's
  chooser says so (`choose_corridor`), flees nothing, and always knows the best prey for his level.
* Five potions in a boss fight (`CORRIDOR_POTIONS`); none in ordinary fights.

### 2.2 The clock and the combat rules

Taken from the engine (docs/COMBAT_TIMING.md and std/living.cs): delay = (Reaction Time x speed factor + action modifier + recovery) x 2 s;
dodge first, then d20 + round(rate x level) + to-hit >= 10 + armor + (level - 1); natural 20 hits, natural 1 misses; critical 5% (x1.5);
a shield blocks 15% (damage halved). Creatures drawn in wait Reaction x 2 s before their first action. Stagger, slow (speed factor on the
reaction part only), weaken (to-hit, damage, armor modifiers), buff and summon are modelled as the files implement them.

### 2.3 Pace (experience per hour)

| quantity | value | where it comes from |
|---|---|---|
| walking, one room | 4 s | command, room text, decision |
| rooms between two kills | 3 | spawn density; at least 6 respawning points per creature type, ResetSeconds <= 300 |
| looting a corpse | 3 s | |
| drinking a potion outside a fight | 2 s | |
| natural Vitality regeneration | 0.1 Vitality/s out of combat (Adventurer x1.5, Survival Training); 0.3 Vitality/s in a safe room | Adventurer.Tick |
| Endurance and Focus regeneration | per calling, from the class files | |
| bought healing | 15 gold per 20 Vitality; gold is the creature's own coins plus loot sold (a dropped potion is drunk) | prices in the Hollow shops |
| "haven" | a safe room within 6 rooms; share of the time one is at hand: 100% at levels 5 and 6, 80% at 7, 50% at 8, 25% at 9 and 10 | segments 1 to 3 of the build map |
| resupply | 5% extra time (buying potions in bulk) | |
| distance from the Guest Rooms to level-n prey | 14, 26, 40, 54, 68, 76 rooms at levels 5 to 10 | build map |
| death | the way back from the Guest Rooms plus 10 s for the corpse; the 10% gold fee is NOT modelled (see 8) | Adventurer.Die |

The prey of a level-n character is the mix of ordinary creatures of level n and n+1 (levels 5 and up), killed in equal numbers. Bosses
are visits, not a source of regular experience. Potions are paid for by the mix's own gold ("self-funded"): `f` is the share of damage taken
that the gold buys back. The shares come out 5% at level 5 and 15 to 20% at levels 6 to 8.

Four variants are reported: perfect (own gold buys potions, rests in a haven), no haven, no potions, neither.

## 3. Experience per hour

Harmonic mean over callings is the figure that matters: a table that gives each calling the same number of hours must use it.

```
experience per hour, perfect (own gold buys potions, rests in a haven)
level    warrio    scout   priest     mage   advent   harmonic mean
    5      2236     2399     1655     2189     2713        2180
    6      2732     2976     1948     2846     3152        2654
    7      3244     3598     2296     3648     3999        3234
    8      3580     3909     2494     3946     4422        3532
    9      3738     4100     2817     3968     5038        3797
experience per hour, no haven
level    warrio    scout   priest     mage   advent   harmonic mean
    5      1409     1530     1056     1236     1953        1377
    6      1776     1939     1326     1756     2453        1779
    7      2335     2573     1696     2537     3169        2363
    8      2853     3127     2056     3172     3824        2884
    9      3405     3707     2498     3574     4464        3407
experience per hour, no potions
level    warrio    scout   priest     mage   advent   harmonic mean
    5      2231     2382     1612     2120     2696        2145
    6      2553     2845     1860     2696     3065        2528
    7      2940     3195     2133     3258     3575        2926
    8      3189     3495     2323     3518     4043        3202
    9      3507     3780     2639     3656     4523        3513
experience per hour, neither
level    warrio    scout   priest     mage   advent   harmonic mean
    5      1366     1436     1076     1187     1888        1340
    6      1621     1734     1198     1562     2217        1603
    7      2011     2156     1522     2161     2751        2045
    8      2602     2696     1861     2754     3408        2565
    9      3141     3344     2366     3239     4150        3145
```

Reading: natural regeneration (0.1 Vitality/s) dominates recovery, so a fight that costs 30 Vitality costs 5 minutes of waiting unless a potion or a haven
shortens it. The Adventurer is the fastest (its regeneration is 1.5x, and its axe is the best in the village). The Priest is intrinsically the
slowest: one third to one half slower in killing, so it pays back about 0.6 of the others' rate. The variants show what a careless player loses:
"neither" is about 60% of "perfect" at level 5, rising to 83% at level 9, which matches the plan's "about 40 hours for an ordinary player"
when added to wandering, dying, shopping and reading.

## 4. The proposed experience table and the formula

### 4.1 Table (levels 6 to 10; levels 1 to 5 unchanged)

Method: the experience to leave level L is (target hours) x (harmonic mean of the five callings' experience per hour at level L), with
targets 2, 2.5, 3, 3.5, 4 hours for levels 5 to 9. The harmonic mean is what makes the mean of the hours over the callings equal the target.

| level | experience needed (modifier 1.0) | old formula 50n(n-1) | gap from the previous level | target hours |
|---|---|---|---|---|
| 1 to 5 | 0, 100, 300, 600, 1000 | same | | |
| 6 | 5,400 | 1,500 | 4,400 | 2.0 |
| 7 | 12,000 | 2,100 | 6,600 | 2.5 |
| 8 | 21,700 | 2,800 | 9,700 | 3.0 |
| 9 | 34,100 | 3,600 | 12,400 | 3.5 |
| 10 | 49,300 | 4,500 | 15,200 | 4.0 |

Projected hours from level 5 to 10 (perfect play), per calling:

| step | warrior | scout | priest | mage | adventurer | mean |
|---|---|---|---|---|---|---|
| 5 to 6 | 1.97 | 1.83 | 2.66 | 2.01 | 1.62 | 2.02 |
| 6 to 7 | 2.42 | 2.22 | 3.39 | 2.32 | 2.09 | 2.49 |
| 7 to 8 | 2.99 | 2.70 | 4.22 | 2.66 | 2.43 | 3.00 |
| 8 to 9 | 3.46 | 3.17 | 4.97 | 3.14 | 2.80 | 3.51 |
| 9 to 10 | 4.07 | 3.71 | 5.40 | 3.83 | 3.02 | 4.00 |
| cumulative | 14.9 | 13.6 | 20.6 | 14.0 | 12.0 | 15.0 |

The Priest needs half as long again as the Adventurer. That is the model's honest result with village gear (section 6); it is not tuned away.

### 4.2 Beyond level 10

Proposal: for n > 10, experience to reach level n = round(T10 x (n / 10)^p x modifier), T10 = 49,300, p = 3.0.
The experience per hour beyond level 9 is extrapolated as 3,797 x (L/9)^0.94 (fitted to the five points above). With that the implied hours,
cumulative from level 5 (level 10 is at 15 h), are:

| p | level 11 | level 20 | level 50 | level 100 |
|---|---|---|---|---|
| 2.5 | 62,565 xp, 18 h | 278,883 xp, 53 h | 2,755,954 xp, 231 h | 15,590,029 xp, 682 h |
| **3.0 (proposed)** | 65,618 xp, 19 h | 394,400 xp, 71 h | 6,162,500 xp, 474 h | 49,300,000 xp, 1,966 h |
| 3.5 | 68,821 xp, 20 h | 557,766 xp, 96 h | 13,779,769 xp, 998 h | 155,900,289 xp, 5,835 h |

p = 3.0 continues the last step smoothly: the step from 10 to 11 takes about 4 h, like 9 to 10 (p = 3.5 would jump to 5 h). With p = 3.0 the gap
per level at 100 is about 100 hours. **This extrapolation is weak.** It assumes that experience per hour keeps growing almost linearly with level,
that content exists for those levels, and that the pace of a player at level 50 resembles one at level 9 (it does not: gear, abilities, party
play, travel and the whole economy change). The numbers up to level 10 are measured; beyond that they only give the right order of magnitude
and a curve that does not kink. Re-fit when levels 11 to 20 content exists. Note the cap is 100 (`MaxLevel`).

### 4.3 The exact change in the game (applied in the integration of the corridor)

File `mudlib/std/adventurer.cs`, method `ExperienceFor` (currently line 121), replace

```csharp
    public static int ExperienceFor(int level, double modifier) =>
        level <= 1 ? 0 : (int)Math.Round(XpScale * level * (level - 1) * modifier, MidpointRounding.AwayFromZero);
```

with

```csharp
    // Levels 6 to 10 come from the corridor pace model (docs/CORRIDOR_BALANCE.md); beyond 10 a power law through level 10.
    private static readonly int[] XpTable = { 0, 0, 100, 300, 600, 1000, 5400, 12000, 21700, 34100, 49300 };

    public static int ExperienceFor(int level, double modifier)
    {
        if (level <= 1) return 0;
        double basis = level < XpTable.Length ? XpTable[level] : XpTable[10] * Math.Pow(level / 10.0, 3.0);
        return (int)Math.Round(basis * modifier, MidpointRounding.AwayFromZero);
    }
```

(Levels 2 to 5 equal 50 n (n-1) = 100, 300, 600, 1000: the table keeps them exactly. `XpScale` then has no user and can go.) Tests that
assume the old numbers at level 6 and above must change with it: tests/area_test.py lines 703 and 709 ("level 6 needs 1500", "more for level 6"),
and tests/class_test.py, whose `xp_for` helper re-implements 50 n (n-1) (lines 819 and 855). Level 5 and below are unaffected.

## 5. The creatures

All 31 numbers below are in the files and checked by `balance_sim.py check`. "react" is the base Reaction Time; actions are prep/recovery in
time units of 2 s; "x2" is a damage multiplier; hit is the to-hit bonus; arm is armor. Dodge is a percentage.

| id | name | L | Vitality | dmg | hit | arm | dodge | react | xp | coins | loot | aggr | actions (prep/rec) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| field_rabbit | Field Rabbit | 1 | 8 | 1-2 | 0 | 0 | - | 1.5 | 12 | - | - | no | Bite 0/0.5 |
| roe_deer | Roe Deer | 3 | 22 | 1-3 | 0 | 0 | - | 2.5 | 26 | - | - | no | Kick 0/1 |
| grey_fox | Grey Fox | 4 | 26 | 2-4 | 1 | 1 | 10 | 1.5 | 65 | - | - | no | Nip 0/0.5 |
| stray_wolf | Stray Wolf | 5 | 60 | 6-10 | 1 | 1 | - | 2 | 140 | - | - | no | Bite 0/0.5 |
| forest_spider | Forest Spider | 5 | 57 | 4-8 | 2 | 0 | - | 1.5 | 135 | - | - | yes | Bite 0/0.5, Slowing Bite 0/0.5 |
| bandit_lookout | Bandit Lookout | 6 | 48 | 10-15 | 2 | 2 | - | 3 | 145 | 3-8 | iron_dagger 20%, trail_ration 25% | yes | Slash 0/1 |
| forest_wolf | Forest Wolf | 6 | 73 | 3-6 | 2 | 1 | - | 2 | 185 | - | - | no | Bite 0/0.5, Savage Bite 0/0.5 x2 |
| wild_boar | Wild Boar | 6 | 40 | 11-19 | 1 | 2 | - | 3.5 | 145 | - | - | no | Gore 0/1, Charge 1/1.5 x2 |
| goblin_scout | Goblin Scout | 7 | 80 | 5-8 | 3 | 2 | - | 2 | 270 | 3-9 | iron_dagger 20%, healing_potion 4% | yes | Quick Stab 0/0.5 |
| goblin_raider | Goblin Raider | 7 | 53 | 7-11 | 2 | 3 | - | 3 | 170 | 4-10 | rusty_sword 15%, training_spear 15%, healing_potion 4% | yes | Cleaver Hack 0/1, War Whoop 0.5/0.5 (buff) |
| boar_brute | Boar Brute | 7 | 45 | 8-14 | 2 | 3 | - | 4.5 | 180 | - | - | no | Gore 0/1.5, Trample 1/2 x2 |
| tunnel_rat | Tunnel Rat | 7 | 40 | 3-6 | 3 | 1 | - | 1.5 | 165 | - | - | yes | Bite 0/0.5 |
| woodland_spider | Woodland Spider | 8 | 91 | 3-5 | 3 | 1 | - | 1.5 | 420 | - | - | yes | Bite 0/0.5, Slowing Bite 0/0.5 |
| rogue_bandit | Rogue Bandit | 8 | 60 | 10-16 | 3 | 3 | - | 3 | 300 | 5-12 | rusty_sword 15%, healing_potion 5%, trail_ration 20% | yes | Slash 0/1, Pommel Strike 0/1.5 |
| young_black_bear | Young Black Bear | 8 | 57 | 8-15 | 2 | 2 | - | 4 | 300 | - | - | no | Swipe 0/1.5, Maul 1/2 x2 |
| black_bear | Black Bear | 8 | 54 | 8-13 | 3 | 3 | - | 4.5 | 280 | - | - | no | Swipe 0/1.5, Maul 1/2 x2, Bellowing Roar 0.5/0.5 (buff) |
| heartwood_goblin | Goblin Warrior | 8 | 56 | 5-8 | 3 | 4 | - | 3 | 230 | 5-12 | training_spear 15%, iron_dagger 15%, healing_potion 4% | yes | Slash 0/1, Heavy Swing 0.5/1.5 x2 |
| highway_bandit | Highway Bandit | 8 | 59 | 6-9 | 3 | 3 | - | 3 | 195 | 5-13 | rusty_sword 15%, healing_potion 6%, trail_ration 20% | yes | Slash 0/1, Pommel Strike 0/1.5 |
| thorn_woodling | Thorn Woodling | 9 | 53 | 10-16 | 2 | 4 | - | 4 | 410 | - | - | no | Branch Lash 0/1.5, Thorn Barrage 0.5/1.5 |
| forest_stalker | Forest Stalker | 9 | 70 | 4-7 | 4 | 2 | 15 | 2 | 550 | - | - | yes | Claw 0/1, Ambush Strike 0.5/1 x2 |
| ancient_wolf | Ancient Wolf | 9 | 69 | 8-13 | 3 | 3 | - | 3 | 550 | - | - | yes | Rending Bite 0/1, Howl 0.5/0.5 (buff) |
| rogue_goblin_veteran | Rogue Goblin Veteran | 9 | 53 | 7-10 | 4 | 5 | - | 3.5 | 280 | 6-16 | healing_potion 7%, woodcutter_axe 10%, trail_ration 20% | yes | Slash 0/1, Dazing Blow 0/1.5 |
| restless_spirit | Restless Spirit | 9 | 64 | 8-13 | 3 | 0 | 15 | 2.5 | 520 | - | - | yes | Grave Touch 0/1, Chill of the Grave 0/1 |
| ancient_skeleton | Ancient Skeleton | 9 | 54 | 8-13 | 3 | 4 | - | 3.5 | 410 | 2-8 | - | yes | Rusted Blade 0/1, Bone Crusher 1/1.5 x2 |
| goblin_tunneler | Goblin Tunneler | 8 | 60 | 5-8 | 3 | 3 | - | 3 | 240 | 4-10 | iron_dagger 15%, healing_potion 4%, trail_ration 20% | yes | Pick Strike 0/1, Overhead Swing 1/1.5 x2 |
| root_creeper | Root Creeper | 8 | 58 | 8-13 | 2 | 3 | - | 3.5 | 310 | - | - | yes | Root Lash 0/1, Clinging Grasp 0/1.5 |
| bandit_leader | Bandit Leader | 10 | 100 | 6-10 | 4 | 4 | - | 3.5 | 1550 | 30-60 | bag_of_coins 100%, healing_potion 60%, enchanted_dagger 10% | yes | Sabre Slash 0/1, Shield Smash 0.5/1.5, Call the Pack 0.5/1 (summon) |
| forest_panther | Forest Panther | 10 | 120 | 6-10 | 5 | 3 | 15 | 2 | 1550 | - | - | yes | Claw 0/1, Pouncing Leap 0.5/1 x2, Raking Frenzy 0/1 |
| warren_broodmother | Warren Broodmother | 10 | 80 | 6-10 | 3 | 3 | - | 4 | 1550 | - | - | yes | Venom Fang 0/1, Webbed Spit 0.5/1.5, Call the Brood 0.5/1 (summon) |
| barrow_guardian | Barrow Guardian | 10 | 150 | 10-15 | 3 | 4 | - | 4.5 | 880 | 10-24 | - | yes | Stone Fist 0/1.5, Earthshaker 1/2 x2, Stone Skin 0.5/0.5 (buff) |
| fallen_knight | Fallen Knight | 10 | 155 | 6-10 | 5 | 5 | - | 3.5 | 1550 | 20-40 | healing_potion 50% | yes | Rusted Cleave 0/1, Crushing Blow 1/1.5 x2, Shield of the Fallen 0.5/0.5 (buff) |


Skin and Blood (public string properties, for the coming crafting system, unused by the game now): skins for rabbit, deer, fox, wolf (all
three wolves), boar (wild boar, boar brute), bear (both bears) and panther; blood is the same animal, and "goblin" for every goblin
(scout, raider, heartwood, veteran, tunneler); spiders, undead, woodling, creeper, humans and the Broodmother have neither. No creature drops
or carries blood or any crafting material.

Variety comes through the clock: wolves, spiders, the stalker and the rat are fast and weak per hit (Reaction 1.5 to 2); bears, boars, the
brute and the guardian are slow and heavy (4 to 4.5) with a wound-up double-damage strike; the specials are the spiders' slowing bite, the
bandits' stagger (Pommel Strike 2 s, Shield Smash 3 s), the stalker's opening Ambush Strike (x2, +4 to hit), the woodling's Thorn Barrage
(armor -2 for 12 s), the panther's opening Pouncing Leap and Raking Frenzy, the veteran's Dazing Blow, the creeper's slowing grasp, the
raider's War Whoop, the bears' Bellowing Roar and the Ancient Wolf's Howl. Bosses and near-bosses have two special actions each: Bandit
Leader (Shield Smash, Call the Pack), Forest Panther (Pouncing Leap, Raking Frenzy), Warren Broodmother (Webbed Spit, Call the Brood twice),
Fallen Knight (Shield of the Fallen, Crushing Blow), Barrow Guardian as the lesser one (Earthshaker, Stone Skin). Summons are light (one
Bandit Lookout, two Tunnel Rats).

### 5.1 Experience for the creatures

Rule: the experience of an ordinary creature is the price of the time it costs: (the target experience per hour at its level, which grows roughly as
L^0.9 like the pace model's own result) x (seconds a kill takes the five callings on average, including the walk, the rest and the lost fights, divided by 3600), rounded.
So a creature that is harder to kill (more damage taken) pays more, and nothing pays more per hour than anything else: no arbitrage. The best
single prey in the mix still yields up to about 1.5 times the mix rate at levels 7 to 9 (1.3 at level 5); that is the remaining unevenness, mostly
creatures that do little damage, and it is why a player who knows the best prey is faster than the mix the table is computed on.
Bosses are fixed at 3.5 times an ordinary level-9 kill (1,550), with the Barrow Guardian (a dungeon guard) at 880.

Comparison with the starter village: Goblin Elite Guard 85, Overseer 250 (boss). The corridor's ordinary creatures from level 5 pay 135 to 550,
all more than the Elite Guard; the bosses pay six times the Overseer. **Deliberate deviation from the brief:** the three low animals (Field Rabbit 12,
Roe Deer 26, Grey Fox 65) pay token experience, below the village's top. If they paid more than 250 a character could farm rabbits (8 Vitality,
nothing to fear) at several times the pace of the intended prey. The lead may overrule this; the cost is a pace distortion, not a code problem.

## 6. Win rates

Win percentage of each calling against each creature at the lowest, middle and highest player level of the creature's band, from full health,
village gear, 400 fights per cell. Ordinary creatures: no potions. Bosses: five potions. "spread" is the largest distance of any calling from
the five-calling average. `python3 tests/balance_sim.py corridor 400` reproduces the table.

```
creature                L  pl    war  sco  pri  mag  adv   avg  spread   sec  hp lost
field_rabbit            1   5    100  100  100  100  100   100       0     2        0
field_rabbit            1   6    100  100  100  100  100   100       0     2        0
roe_deer                3   5    100  100  100  100  100   100       0    17        1
roe_deer                3   6    100  100  100  100  100   100       0    15        1
roe_deer                3   7    100  100  100  100  100   100       0    14        1
grey_fox                4   5    100  100  100  100  100   100       0    28        6
grey_fox                4   6    100  100  100  100  100   100       0    25        5
stray_wolf              5   5     99   94   92   95   94    95       5    65       30
stray_wolf              5   6    100  100   99   99   98    99       1    60       27
stray_wolf              5   7    100  100  100  100  100   100       0    54       22
forest_spider           5   5    100   97   92   96   98    96       4    60       30
forest_spider           5   6    100  100   99   99   99    99       1    54       26
forest_spider           5   7    100  100  100  100  100   100       0    49       22
bandit_lookout          6   6     98   98   88  100   94    96       7    57       30
bandit_lookout          6   7    100  100   98  100   99    99       2    52       26
forest_wolf             6   5     97   90   88   92   90    92       6    86       34
forest_wolf             6   6    100   98   98  100   98    99       1    80       31
forest_wolf             6   7    100  100  100  100  100   100       0    73       27
wild_boar               6   6     99   96   90   96   96    95       6    45       27
wild_boar               6   7    100   98   98   99   99    99       1    42       23
wild_boar               6   8    100  100   99  100  100   100       1    38       20
goblin_scout            7   7     98   90   84   92   91    91       7    95       46
goblin_scout            7   8    100   99   97   98   99    98       2    89       41
goblin_raider           7   6     98   94   92  100   94    96       4    74       30
goblin_raider           7   7     99   99   98  100   99    99       1    69       27
goblin_raider           7   9    100  100  100  100  100   100       0    55       18
boar_brute              7   6     96   96   93  100   92    96       4    63       25
boar_brute              7   7    100   99   98  100   98    99       1    57       23
boar_brute              7   8    100  100  100  100   99   100       0    51       18
tunnel_rat              7   7    100  100  100  100  100   100       0    41       20
tunnel_rat              7   8    100  100  100  100  100   100       0    37       17
woodland_spider         8   7     98   92   88   95   94    93       6   115       48
woodland_spider         8   8    100   98   98  100   99    99       1   106       42
woodland_spider         8   9    100  100  100  100  100   100       0    96       37
rogue_bandit            8   8     96   95   86   98   94    94       8    79       40
rogue_bandit            8   9    100  100   98  100   98    99       1    73       36
young_black_bear        8   7     98   94   84  100   93    94      10    71       34
young_black_bear        8   8     99   98   95  100   98    98       3    65       29
black_bear              8   7     97   93   87  100   92    94       7    75       31
black_bear              8   8    100   99   98  100   97    99       2    69       27
black_bear              8   9    100  100  100  100  100   100       0    62       21
heartwood_goblin        8   7     97   94   92  100   92    95       5    88       35
heartwood_goblin        8   8    100  100   98  100   98    99       1    79       30
heartwood_goblin        8   9    100  100  100  100  100   100       0    71       26
highway_bandit          8   6     98   93   90  100   90    94       6    98       34
highway_bandit          8   7    100   99  100  100   98    99       1    89       30
highway_bandit          8   9    100  100  100  100  100   100       0    70       22
thorn_woodling          9   8     96   94   87  100   90    94       7    83       38
thorn_woodling          9   9     99   99   97  100   97    98       2    76       32
thorn_woodling          9  10    100  100  100  100  100   100       0    68       27
forest_stalker          9   8     96   90   88   95   90    92       4   109       51
forest_stalker          9   9    100   98   99   99   98    99       1   101       45
forest_stalker          9  10    100  100  100   99  100   100       0    93       40
ancient_wolf            9   9     98   96   89   99   93    95       6    89       46
ancient_wolf            9  10    100   99   98  100   98    99       1    81       39
rogue_goblin_veteran    9   8     98   93   87  100   92    94       7   100       38
rogue_goblin_veteran    9   9    100   99   98  100   98    99       1    91       33
rogue_goblin_veteran    9  10    100  100  100  100  100   100       0    78       28
restless_spirit         9   9     98   96   89   93   96    94       5    78       46
restless_spirit         9  10     99   99   96   98  100    98       3    73       42
ancient_skeleton        9   9     98   96   86  100   94    95       9    75       39
ancient_skeleton        9  10    100  100   97  100   99    99       2    68       33
goblin_tunneler         8   7     98   95   90  100   96    96       5    83       35
goblin_tunneler         8   8    100  100   98  100   99    99       2    76       30
goblin_tunneler         8   9    100  100  100  100  100   100       0    69       26
root_creeper            8   7     98   96   88  100   94    95       7    84       35
root_creeper            8   8    100   99   98  100   99    99       1    76       30
root_creeper            8   9    100  100  100  100  100   100       0    66       25
bandit_leader          10   8     63   57   16   72   50    52      35   257       59
bandit_leader          10   9     93   82   56   91   78    80      24   248       59
bandit_leader          10  10    100   97   86   98   95    95       9   224       58
forest_panther         10   9     62   63   21   58   53    51      30   209       69
forest_panther         10  10     87   84   46   84   80    76      30   212       70
warren_broodmother     10   8     58   48   10   47   56    44      34   231       62
warren_broodmother     10   9     91   81   32   85   81    74      42   232       63
warren_broodmother     10  10     98   98   78   97   96    93      16   215       61
barrow_guardian        10   9     72   63   32   74   54    59      27   271       63
barrow_guardian        10  10     92   90   67   87   83    84      17   255       63
fallen_knight          10   9     60   52   10   79   44    49      39   286       68
fallen_knight          10  10     89   80   47   96   73    77      30   274       68
```

Findings:

* Ordinary creatures: every one is won at 91 to 100% on average at the lowest band level and 98 to 100% at the highest; the largest gap of
  any calling to the average is 10 points (Young Black Bear, Priest, at level 7), within the target of 15. The Priest is the lowest on almost
  every row.
* Bosses at the band's low level: Bandit Leader 52%, Panther 51% (level 9, its band starts at 9), Broodmother 44%, Barrow Guardian 59%, Fallen Knight
  49%: near even with five potions. At the high level: 95%, 76%, 93%, 84%, 77%: likely with preparation. The Priest is far below the others
  against bosses (10 to 32% at the low level; the spread of the worst calling from the average is 27 to 39 points there, 9 to 30 at the high level). This is intrinsic: the mace is slow and the Priest has few
  damage skills. It is the model's biggest unresolved finding (see section 8).

### The gear gap

With village gear the fighters stand still: the best weapon (Overseer's axe 17-25) is reached in the Hollow and nothing in the corridor
improves it, nor the 4 points of armor. Creature Vitality is therefore flat (40 to 90 for ordinary creatures, 80 to 155 for bosses) and difficulty
comes from armor (up to 5), to-hit, damage and specials instead. Casters differ in kind: the arcane bolt (6-9) ignores armor, so a Mage kills
the armor-5 Goblin Veteran in 47 s against the Warrior's 104 s, (and the armor-1 Stray Wolf in 54 s against 65 s), but pays in Focus, which the model refills by resting. Fighters
slow down against armored foes (their hit chance falls by 5 points per armor point), casters do not. The Priest, with the weakest weapon and the
slowest cycle, takes about 1.5 to 2 times as long as the others to kill anything (91 s against 44 to 65 for a level-6 Stray Wolf). The
caps on creature armor (5) and the damage tuning were chosen to keep the spread within 15 points on ordinary creatures; the same gap decides
who suffers on bosses.

## 7. Quests and discoveries: share of the 15 hours

Proposal: about 20% of the experience from level 5 to 10 comes from quests and discoveries (first visits, hidden rooms, books, riddles, the
Haven's bounty board), the rest from killing. Budget (modifier 1.0), per level, as a share of the gap:

| level | gap | quests and discoveries (20%) | kills (80%) |
|---|---|---|---|
| 5 to 6 | 4,400 | 880 | 3,520 |
| 6 to 7 | 6,600 | 1,320 | 5,280 |
| 7 to 8 | 9,700 | 1,940 | 7,760 |
| 8 to 9 | 12,400 | 2,480 | 9,920 |
| 9 to 10 | 15,200 | 3,040 | 12,160 |
| total | 48,300 | 9,660 | 38,640 |

The table is computed on the kill pace alone, so the budget is carved out of it, not added: kills then supply 80% of the experience, which
takes 12 of the 15 hours, and the quest and discovery activity (walking, talking, searching) has to deliver its 9,660 experience in the other 3 hours,
about 3,200 per hour, which is the same rate as hunting (3,300 harmonic mean). If a quest is worth more than that per hour, players will
prefer quests and reach level 10 sooner; the per-quest rewards below should be checked against that rate. The budget should be spent mostly on a handful of 500 to 1,500 experience quest rewards per segment and on one-time discovery
awards of 50 to 150 each; bosses (1,550) are repeatable only through the room's reset, and boss rooms should use `ResetSeconds` of 1,800 or
more so that boss-camping stays slower than hunting. For comparison, the village quests pay 150 and 200 at level 5.

## 8. What is weak or unverified

1. **Regeneration and potion economics decide the pace.** 0.1 Vitality/s was read from the code; the haven and distance tables (section 2.3) are my
   reading of the build map, not measured. A wrong haven share moves the pace by up to 40% ("no haven" versus "perfect"), and the table
   would be wrong by the same amount.
2. **The perfect-knowledge player does not exist.** The model has no mistakes, no waiting for friends, no reading, no inventory, no shop
   queues, and no losses of focus. Real players will be slower; the plan expected about 40 hours for them, and "neither" gives about 1.2 to 1.6
   times perfect, not 2.7. Ordinary play also involves dying, which is only approximated.
3. **Death is cheap in the model.** The 10% gold fee, corpse recovery under threat, and the loss of unsaved resources are not modelled beyond a fixed
   walk and 10 s; at levels 9 and 10 losses are the cost of the lost fights only.
4. **The Priest.** The Priest's boss win rates (10 to 32% at the low level) and its 1.5x kill time are structural, not tuned. A
   Priest-specific skill or a better mace in the corridor would be needed, or the table gives that calling 20 hours instead of 15. I did not
   alter creatures for it, because that would make everything trivial for the other four.
5. **The extrapolation past level 10** (section 4.2) is a fitted guess; 20, 50 and 100 are orders of magnitude only.
6. **The fight model is a simplified rendition of the engine.** Dodge and block are applied as in the code, but the simulated player uses
   a scripted chooser (when to bolt, bash, drink); real skill lists are richer. Summon and buff timing are checked in the driver only for
   the Bandit Leader and the Forest Spider (tests/corridor_mob_test.py); the other special actions are verified by `stat` (numbers) and by
   the build only, not by scripted fights.
7. **Experience for the low animals** departs from the brief (section 5.1).
8. **Spawn supply.** The pace assumes at least 6 respawning points per creature type within 3 rooms of each other and a `ResetSeconds` of
   300 or less. If the areas are built with fewer or slower spawns, the pace falls (contested spawns, travel).
9. Simulation noise: 250 to 400 runs per cell; win rates are good to about +/- 3 points, hours to about +/- 3%.
