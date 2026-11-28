# Hidden calling: balance measurement (plan numbers vs a recommendation)

This document may name the hidden calling; it is excluded from the player-text lint. The numbers come from `python3 tests/balance_sim.py hidden 1500 yard sets bosses` (seed 20261127, 1500 fights a cell) and the other parts named below (`alt`, `ablate`, `spread`; add `variant=durations_x2` for the recommended numbers). "hidden/cautious" and "hidden/greedy" are the two rotations. A "human-mid" player is modelled: typing delay, five carried potions, haven and natural recovery, rank II at levels 5 to 9, Corridor gear, hunting the Corridor `prey(level)` mix in equal numbers.

## Recommendation

With the numbers in bureaucrat.md sections 3 to 6 the calling survives like the Mage, but it is 7 to 13% slower with the careful rotation and 28 to 32% slower with the greedy one from level 6, and it loses 6 to 16% more Vitality per won fight than the Mage (more with the greedy rotation), so it is not the least hit. Doubling every duration (restraint, bars, tape, ward, triplicate window) while keeping the costs, the repeat window and the flood guard puts both rotations within 0.2 points of the Mage's survival and within about 5% (careful) or 5 to 14% (greedy) of its pace, with Vitality lost level with the Mage's. Recommend keeping the spec's names, costs and repeat rules and doubling the durations (a one-column change in the spell table, `secs x2` below); cost cuts and a shorter repeat window were measured and do not help.

The calling is never the best killer (42 to 47 seconds a won fight beside the Mage's 31 to 35), as intended. It is not "clearly best at not being hit" under any measured variant: with doubled durations it ties the Mage and is a few points better than the warrior, scout, priest and adventurer.

Weak spots: two ordinary enemies at once (Goblin Raider plus Goblin Scout, level 7) cost the calling 9 points of win rate against the Mage (22 for the greedy rotation); against bosses the greedy rotation collapses (it spends its Focus early) and the careful one trails the Mage on the Forest Panther and the Barrow Guardian by 10 to 19 points. Only Triplicate and Restrain move the Corridor numbers; switching off Desist, Tape, Audit or Deny changes nothing measurable, and the Interrupt lands about 0.01 to 0.03 times a fight with a human's typing delay and a 2 second window.

## The yardstick with the plan's numbers

experience an hour
level      warrior       scout      priest        mage  adventurer hidden/ hidden/   mean of five   (harmonic)
    5         3243        2680        2124        2678        3704        2477        2401      2885.5       2784
    6         3963        3274        2548        3493        4502        3151        2516      3556.0       3428
    7         4871        4044        3023        4343        5908        3872        2942      4437.8       4227
    8         6089        4288        3599        4813        7648        4237        3273      5287.3       4932
    9         7533        4616        4223        5093        9792        4439        3533      6251.6       5652

minutes a level (experience to the next level / experience an hour)
level      warrior       scout      priest        mage  adventurer hidden/ hidden/   mean of five   (harmonic)
    5           81          99         124          99          71         107         110        94.8          0
    6          100         121         155         113          88         126         157       115.5          0
    7          119         144         193         134          99         150         198       137.7          0
    8          122         174         207         155          97         176         227       150.9          0
    9          121         198         216         179          93         205         258       161.4          0

win rate a fight (%)
level      warrior       scout      priest        mage  adventurer hidden/ hidden/   mean of five   (harmonic)
    5         99.6        98.4        98.3        98.8        99.5        98.9        98.0        98.9          0
    6         99.9        99.5        99.5        99.8        99.8        99.6        99.0        99.7          0
    7         99.9        99.8        99.5        99.9        99.8        99.8        99.1        99.8          0
    8         99.8        99.8        99.3        99.9        99.8        99.8        99.4        99.7          0
    9        100.0       100.0        99.9        99.9       100.0        99.9        99.8        99.9          0

deaths an hour
level      warrior       scout      priest        mage  adventurer hidden/ hidden/   mean of five   (harmonic)
    5         0.08        0.30        0.22        0.22        0.10        0.18        0.32         0.2          0
    6         0.02        0.10        0.06        0.04        0.05        0.07        0.15         0.1          0
    7         0.01        0.03        0.04        0.01        0.03        0.03        0.10         0.0          0
    8         0.01        0.02        0.04        0.02        0.02        0.02        0.05         0.0          0
    9         0.00        0.00        0.01        0.01        0.00        0.01        0.02         0.0          0

potions an hour
level      warrior       scout      priest        mage  adventurer hidden/ hidden/   mean of five   (harmonic)
    5         1.36        2.52        6.57        2.26        2.74        2.66        3.16         3.1          0
    6         1.24        2.48        3.70        1.31        2.13        1.64        4.06         2.2          0
    7         0.96        0.98        3.00        0.48        1.64        0.65        2.20         1.4          0
    8         1.01        0.85        2.83        0.44        1.65        0.68        1.74         1.4          0
    9         0.40        0.37        1.64        0.29        0.67        0.42        1.20         0.7          0

Vitality lost a won fight (after potions)
level      warrior       scout      priest        mage  adventurer hidden/ hidden/   mean of five   (harmonic)
    5         21.6        24.9        36.3        25.3        24.0        27.4        28.8        26.4          0
    6         21.0        23.8        34.2        21.1        22.9        23.5        33.5        24.6          0
    7         23.9        22.4        38.5        20.1        24.6        22.6        33.3        25.9          0
    8         26.4        25.9        43.5        22.2        27.3        25.4        35.3        29.1          0
    9         26.4        26.6        45.9        23.9        26.8        27.3        36.2        29.9          0

verdict, hidden/cautious:
  level 5: survival  98.9% vs Mage  98.8% ( +0.1)   pace   -7.5% of the Mage's   not the fastest killer   not the least hit   [in the band]
  level 6: survival  99.6% vs Mage  99.8% ( -0.2)   pace   -9.8% of the Mage's   not the fastest killer   not the least hit   [in the band]
  level 7: survival  99.8% vs Mage 100.0% ( -0.1)   pace  -10.8% of the Mage's   not the fastest killer   not the least hit   [in the band]
  level 8: survival  99.8% vs Mage  99.9% ( -0.0)   pace  -12.0% of the Mage's   not the fastest killer   not the least hit   [in the band]
  level 9: survival  99.9% vs Mage  99.9% ( -0.0)   pace  -12.9% of the Mage's   not the fastest killer   not the least hit   [in the band]

verdict, hidden/greedy:
  level 5: survival  98.0% vs Mage  98.8% ( -0.7)   pace  -10.3% of the Mage's   not the fastest killer   not the least hit   [in the band]
  level 6: survival  99.0% vs Mage  99.8% ( -0.8)   pace  -28.0% of the Mage's   not the fastest killer   not the least hit   [OUT of the band]
  level 7: survival  99.2% vs Mage 100.0% ( -0.8)   pace  -32.2% of the Mage's   not the fastest killer   not the least hit   [OUT of the band]
  level 8: survival  99.5% vs Mage  99.9% ( -0.4)   pace  -32.0% of the Mage's   not the fastest killer   not the least hit   [OUT of the band]
  level 9: survival  99.8% vs Mage  99.9% ( -0.1)   pace  -30.6% of the Mage's   not the fastest killer   not the least hit   [OUT of the band]

## The same with durations doubled (recommended)

experience an hour
level      warrior       scout      priest        mage  adventurer hidden/ hidden/   mean of five   (harmonic)
    5         3243        2680        2124        2678        3704        2547        2545      2885.5       2784
    6         3963        3274        2548        3493        4502        3310        3102      3556.0       3428
    7         4871        4044        3023        4343        5908        4100        3755      4437.8       4227
    8         6089        4288        3599        4813        7648        4545        4342      5287.3       4932
    9         7533        4616        4223        5093        9792        4833        4737      6251.6       5652

minutes a level (experience to the next level / experience an hour)
level      warrior       scout      priest        mage  adventurer hidden/ hidden/   mean of five   (harmonic)
    5           81          99         124          99          71         104         104        94.8          0
    6          100         121         155         113          88         120         128       115.5          0
    7          119         144         193         134          99         142         155       137.7          0
    8          122         174         207         155          97         164         171       150.9          0
    9          121         198         216         179          93         189         193       161.4          0

Vitality lost a won fight (after potions)
level      warrior       scout      priest        mage  adventurer hidden/ hidden/   mean of five   (harmonic)
    5         21.6        24.9        36.3        25.3        24.0        25.2        25.5        26.4          0
    6         21.0        23.8        34.2        21.1        22.9        20.4        19.9        24.6          0
    7         23.9        22.4        38.5        20.1        24.6        20.1        20.9        25.9          0
    8         26.4        25.9        43.5        22.2        27.3        22.5        22.5        29.1          0
    9         26.4        26.6        45.9        23.9        26.8        24.1        23.7        29.9          0

verdict, hidden/cautious:
  level 5: survival  99.0% vs Mage  98.8% ( +0.2)   pace   -4.9% of the Mage's   not the fastest killer   not the least hit   [in the band]
  level 6: survival  99.9% vs Mage  99.8% ( +0.1)   pace   -5.2% of the Mage's   not the fastest killer   least Vitality lost of the six   [in the band]
  level 7: survival 100.0% vs Mage 100.0% ( +0.0)   pace   -5.6% of the Mage's   not the fastest killer   not the least hit   [in the band]
  level 8: survival  99.9% vs Mage  99.9% ( +0.1)   pace   -5.5% of the Mage's   not the fastest killer   not the least hit   [in the band]
  level 9: survival 100.0% vs Mage  99.9% ( +0.0)   pace   -5.1% of the Mage's   not the fastest killer   not the least hit   [in the band]

verdict, hidden/greedy:
  level 5: survival  98.9% vs Mage  98.8% ( +0.1)   pace   -5.0% of the Mage's   not the fastest killer   not the least hit   [in the band]
  level 6: survival  99.8% vs Mage  99.8% ( +0.0)   pace  -11.2% of the Mage's   not the fastest killer   least Vitality lost of the six   [in the band]
  level 7: survival  99.9% vs Mage 100.0% ( -0.1)   pace  -13.5% of the Mage's   not the fastest killer   not the least hit   [in the band]
  level 8: survival  99.9% vs Mage  99.9% ( +0.1)   pace   -9.8% of the Mage's   not the fastest killer   not the least hit   [in the band]
  level 9: survival 100.0% vs Mage  99.9% ( +0.1)   pace   -7.0% of the Mage's   not the fastest killer   least Vitality lost of the six   [in the band]

## Set pieces and bosses (win percent): plan numbers, then doubled durations

set piece                      lvl   warrior     scout    priest      mage adventure  bur/caut  bur/gree   (win %, then deaths % in the Vitality sense: died)
Wild Boar                        6     100.0      99.2      99.0      99.2      99.9      98.1      99.2   mage 99; hidden minus Mage: -1.1, +0.0
Boar Brute                       7     100.0      99.9      99.9     100.0      99.9     100.0      99.7   mage 100; hidden minus Mage: +0.0, -0.3
Thorn Woodling                   9      99.9     100.0     100.0     100.0      99.9     100.0     100.0   mage 100; hidden minus Mage: +0.0, +0.0
Goblin Raider + Goblin Scout     7      92.0      89.1      78.3      92.1      89.3      75.4      43.1   mage 92; hidden minus Mage: -16.7, -48.9

set piece                      lvl   warrior     scout    priest      mage adventure  bur/caut  bur/gree   (win %, then deaths % in the Vitality sense: died)
Wild Boar                        6     100.0      99.2      99.0      99.2      99.9      99.5      99.7   mage 99; hidden minus Mage: +0.3, +0.5
Boar Brute                       7     100.0      99.9      99.9     100.0      99.9     100.0     100.0   mage 100; hidden minus Mage: +0.0, +0.0
Thorn Woodling                   9      99.9     100.0     100.0     100.0      99.9     100.0     100.0   mage 100; hidden minus Mage: +0.0, +0.0
Goblin Raider + Goblin Scout     7      92.0      89.1      78.3      92.1      89.3      83.1      70.0   mage 92; hidden minus Mage: -8.9, -22.1

boss                           lvl   warrior     scout    priest      mage adventure  bur/caut bur/greed   (win %)
Bandit Leader                    9      95.1      86.5      75.8      94.9      89.4      87.9      41.5
Fallen Knight                    9      73.0      65.3      47.1      95.6      60.1      86.5      12.1
Forest Panther                   9      73.6      63.5      45.7      83.3      72.1      56.3       2.3
Warren Broodmother               9      96.9      92.0      77.0      98.1      93.3      92.1      48.5
Barrow Guardian                  9      83.9      69.7      59.3      89.0      76.7      74.5      16.4
Goblin Overseer (hollow)       (not in the sim's creature table)


boss                           lvl   warrior     scout    priest      mage adventure  bur/caut bur/greed   (win %)
Bandit Leader                    9      95.1      86.5      75.8      94.9      89.4      88.1      43.0
Fallen Knight                    9      73.0      65.3      47.1      95.6      60.1      86.9      17.3
Forest Panther                   9      73.6      63.5      45.7      83.3      72.1      64.6       4.9
Warren Broodmother               9      96.9      92.0      77.0      98.1      93.3      93.4      55.5
Barrow Guardian                  9      83.9      69.7      59.3      89.0      76.7      76.1      24.3
Goblin Overseer (hollow)       (not in the sim's creature table)


## Alternatives measured (600 fights a cell; pace and Vitality lost against the Mage, survival in points)

== Alternatives (600 fights a cell) ==
variant / policy                                    L5 pace  surv  hit  L6 pace  surv  hit  L7 pace  surv  hit  L8 pace  surv  hit  L9 pace  surv  hit
spec numbers / cautious                               -7.1%  -0.1   +6%    -9.9%  -0.1  +12%   -10.4%  -0.1  +12%   -12.0%  -0.0  +14%   -12.8%  -0.1  +14%
spec numbers / greedy                                 -9.9%  -0.4  +13%   -28.1%  -0.7  +60%   -32.1%  -0.8  +65%   -31.5%  -0.5  +56%   -30.3%  -0.2  +51%
Focus x0.75 / cautious                                -7.1%  -0.1   +6%   -10.1%  +0.0  +12%   -11.2%  -0.1  +14%   -11.9%  -0.1  +14%   -13.2%  -0.0  +15%
Focus x0.75 / greedy                                  -9.8%  -0.4  +12%   -27.2%  -0.5  +54%   -31.0%  -0.5  +61%   -31.3%  -0.5  +56%   -30.1%  -0.6  +50%
durations x1.5 / cautious                             -6.0%  -0.2   +3%    -7.1%  +0.1   +2%    -7.4%  -0.0   +4%    -7.2%  -0.0   +5%    -7.1%  -0.1   +4%
durations x1.5 / greedy                               -4.9%  -0.1   +1%   -17.7%  +0.0  +15%   -21.5%  -0.1  +27%   -19.6%  -0.2  +22%   -17.9%  -0.1  +18%
repeat window 30 s / cautious                         -7.8%  -0.2   +7%   -10.5%  -0.1  +12%   -11.7%  -0.0  +14%   -12.4%  -0.1  +15%   -12.3%  -0.0  +13%
repeat window 30 s / greedy                           -8.9%  -0.3  +10%   -30.8%  -1.3  +68%   -35.8%  -1.2  +78%   -36.7%  -1.1  +75%   -35.5%  -0.5  +67%
Focus x0.8 + durations x1.25 + window  / cautious     -6.3%  +0.0   +3%    -8.3%  +0.1   +6%    -9.4%  -0.0   +8%    -9.4%  -0.1   +8%   -10.7%  +0.0  +10%
Focus x0.8 + durations x1.25 + window  / greedy       -7.3%  -0.1   +6%   -23.2%  -0.2  +30%   -27.0%  -0.4  +39%   -25.4%  -0.3  +34%   -22.7%  -0.2  +26%
interrupt push 3.5 s / cautious                       -7.1%  -0.1   +6%    -9.9%  -0.1  +12%   -10.3%  -0.2  +11%   -11.8%  -0.0  +14%   -12.9%  -0.1  +14%
interrupt push 3.5 s / greedy                         -9.9%  -0.4  +13%   -28.1%  -0.7  +60%   -31.9%  -0.7  +64%   -31.5%  -0.5  +56%   -30.0%  -0.3  +50%
Focus x0.75 + durations x1.5 / cautious               -6.0%  -0.2   +3%    -7.3%  +0.0   +3%    -7.7%  -0.0   +5%    -7.1%  -0.0   +4%    -6.8%  +0.0   +4%
Focus x0.75 + durations x1.5 / greedy                 -4.6%  -0.1   +1%   -17.1%  +0.1  +14%   -20.7%  -0.2  +23%   -18.8%  -0.2  +20%   -16.9%  -0.0  +17%
durations x2 / cautious                               -4.1%  +0.0   -2%    -5.0%  +0.1   -3%    -5.7%  -0.0   +0%    -5.0%  -0.0   +0%    -4.6%  -0.0   +1%
durations x2 / greedy                                 -4.1%  +0.2   -1%   -11.8%  +0.2   -4%   -13.6%  -0.0   +4%    -9.6%  -0.1   +1%    -6.4%  -0.0   -1%
Focus x0.75 + durations x2 / cautious                 -4.1%  +0.0   -2%    -5.6%  +0.2   -2%    -6.1%  -0.0   +1%    -5.2%  -0.0   +1%    -4.3%  +0.0   +0%
Focus x0.75 + durations x2 / greedy                   -4.0%  +0.2   -1%   -10.6%  +0.2   -7%   -12.0%  -0.0   +1%    -8.5%  -0.0   -1%    -5.7%  +0.0   -2%

## Spell by spell (doubled durations, each spell switched off; 600 fights a cell)

== Spell by spell (600 fights a cell) ==
policy cautious; each spell switched off in turn (the rest of the rotation unchanged); pace and Vitality lost against the Mage
switched off          L5 pace  surv  hit  L6 pace  surv  hit  L7 pace  surv  hit  L8 pace  surv  hit  L9 pace  surv  hit
none                    -4.1%  +0.0   -2%    -5.0%  +0.1   -3%    -5.7%  -0.0   +0%    -5.0%  -0.0   +0%    -4.6%  -0.0   +1%
triplicate              -2.8%  +0.1   +4%    -2.9%  +0.1   +1%    -4.6%  -0.0   +5%    -4.5%  +0.0   +5%    -6.7%  +0.0   +7%
restrain                -4.1%  +0.0   -2%    -5.8%  +0.2   +2%    -6.0%  +0.0   +2%    -6.0%  +0.0   +3%    -3.7%  +0.0   -0%
desist                  -4.1%  +0.0   -2%    -5.0%  +0.1   -3%    -5.9%  -0.0   +1%    -5.0%  -0.0   +0%    -5.3%  -0.0   +2%
tape                    -4.1%  +0.0   -2%    -5.0%  +0.1   -3%    -5.7%  -0.0   +0%    -5.0%  -0.0   +0%    -4.6%  -0.0   +1%
audit                   -4.1%  +0.0   -2%    -5.0%  +0.1   -3%    -5.7%  -0.0   +0%    -5.0%  -0.0   +0%    -4.6%  -0.0   +1%
deny                    -4.1%  +0.0   -2%    -5.0%  +0.1   -3%    -5.7%  -0.0   +0%    -5.0%  -0.0   +0%    -4.6%  -0.0   +1%
all six                 -2.8%  +0.1   +4%    -2.6%  +0.2   +5%    -4.9%  -0.0   +8%    -5.4%  -0.1   +7%    -7.3%  +0.0   +8%
policy greedy; each spell switched off in turn (the rest of the rotation unchanged); pace and Vitality lost against the Mage
switched off          L5 pace  surv  hit  L6 pace  surv  hit  L7 pace  surv  hit  L8 pace  surv  hit  L9 pace  surv  hit
none                    -4.1%  +0.2   -1%   -11.8%  +0.2   -4%   -13.6%  -0.0   +4%    -9.6%  -0.1   +1%    -6.4%  -0.0   -1%
triplicate              -3.2%  +0.0   +4%    -4.3%  +0.2  -12%    -6.9%  -0.0   -3%    -4.4%  -0.1   -3%    -3.3%  -0.1   -2%
restrain                -4.1%  +0.2   -1%    -5.3%  +0.2   -0%    -5.0%  -0.0   -0%    -5.8%  -0.0   +3%    -5.7%  +0.0   +3%
desist                  -4.1%  +0.2   -1%   -11.8%  +0.2   -4%   -13.7%  +0.0   +5%    -9.5%  -0.0   +1%    -6.9%  -0.0   -0%
tape                    -4.1%  +0.2   -1%   -11.8%  +0.2   -4%   -13.6%  -0.0   +4%    -9.6%  -0.1   +1%    -6.4%  -0.0   -1%
audit                   -4.1%  +0.2   -1%   -11.8%  +0.2   -4%   -13.6%  -0.0   +4%    -9.6%  -0.1   +1%    -6.4%  -0.0   -1%
deny                    -4.1%  +0.2   -1%   -11.8%  +0.2   -4%   -13.6%  -0.0   +4%    -9.6%  -0.1   +1%    -6.4%  -0.0   -1%
all six                 -3.2%  +0.0   +4%    -3.1%  +0.0   +6%    -3.7%  +0.0   +5%    -5.3%  -0.1   +7%    -7.3%  -0.0   +9%

## Run-to-run spread (five seeds, 500 fights a cell, pace against the Mage; mean and min..max)

Plan numbers:
== Run-to-run spread ==
the same table at 5 seeds (1, 2, 3, 4, 5), 500 fights a cell: pace against the Mage, min to max
  cautious   L5   -7.4% [-7.9..-6.4]  L6  -10.2% [-11.1..-9.2]  L7  -11.0% [-11.8..-10.6]  L8  -12.0% [-12.3..-11.8]  L9  -13.5% [-14.4..-13.0]
  greedy     L5   -9.6% [-10.2..-8.9]  L6  -28.0% [-28.5..-27.6]  L7  -32.1% [-32.2..-31.9]  L8  -31.7% [-31.8..-31.4]  L9  -30.1% [-30.6..-29.3]

Doubled durations:
== Run-to-run spread ==
the same table at 5 seeds (1, 2, 3, 4, 5), 500 fights a cell: pace against the Mage, min to max
  cautious   L5   -4.4% [-5.0..-3.4]  L6   -5.7% [-6.7..-4.1]  L7   -5.7% [-5.9..-5.2]  L8   -5.3% [-5.6..-4.7]  L9   -5.6% [-6.1..-4.9]
  greedy     L5   -4.7% [-5.9..-4.0]  L6  -12.0% [-12.8..-10.9]  L7  -13.2% [-13.8..-12.7]  L8   -9.7% [-10.2..-9.1]  L9   -6.7% [-7.9..-6.1]

The spread is about 1 point of pace; differences under 2 points between variants are noise.

## Level 10

There is no ordinary creature above level 9, so level 10 can only be levelled on bosses and quests; the five level-9 bosses above are the nearest yardstick. The level-10 spell (Deny) was not measured on the prey mix.

## What the simulation does not model

Other players and parties (party spells, shared marks, everyone else's reactions); real typing speed (a fixed human model is used); the light (the Corridor loops appear lit, so no dark-room penalty); the out-of-combat search bonus of Audit; the summons roll and the quest; elite creatures (none exists on the Corridor); any rank other than II at levels 5 to 9. The Goblin Overseer is not in the sim's creature table and was exercised only in tests/effects_test.py. Creature numbers are checked against the mudlib by `python3 tests/balance_sim.py check`.
