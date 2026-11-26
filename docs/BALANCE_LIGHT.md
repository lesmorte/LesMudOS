# Light and darkness: balance (26 November 2026)

Measured with `python3 tests/balance_sim.py light [rooms|std|cell|alt] [runs]` (1500 runs a cell, the existing fixed seeds: 1 for the standard test, 20261124 for the cellar run). The simulator models what `docs/specs/light.md` says and `python3 tests/balance_sim.py check` compares every number it uses (the penalties and notice changes in `mudlib/secure/light.cs`, Glow's table in `mudlib/skills/glow.cs`, the torches and lantern, each cellar room's `Light` and wall light, the Toad's `DarkSight`) with the mudlib. Nothing here changes the game: the spec's numbers are the named constants of `/secure/light` and `/skills/glow`, and they are what shipped.

## 1. Recommendation (three sentences)

**Ship the spec's numbers as they are.** They cost a character without a light up to 6 points in the standard test (the Mage none) and 1.5 to 10 minutes in the cellars (a run of 43 minutes against 33.5 for the archer), they cost a character with a torch or a lantern almost nothing, and no calling falls below 71% (the owner's floor was 65%). **The owner's target of 72 to 83% for the archer Scout at rank I is not reached by light and cannot be: the archer who carries a torch or a lantern stands in a lit or dim-but-lit room, where there are no penalties, so he scores what he did before (89.9; 89.6 with a lantern; 86.3 with no light at all), and his lead over the mean of the five callings (+10.7) is Aimed Shot itself, as `BALANCE_SCOUT.md` found.** If the owner still wants him in the band, the lever is Aimed Shot's cost (BALANCE_SCOUT.md option E2, 45 Endurance), not the light; the nearest light-only alternative (D2 below: every room from the Guard Post on dark and the two deepest rooms black) brings him to 83.2 and costs the Warrior and the melee Scout 5 to 6.5 points and makes a lantern compulsory, which I do not recommend.

## 2. What was modelled

- **Rooms** (`LIGHT_ROOMS`): lit = Tavern Cellar, Collapsed Passage, Storage Chamber, Smuggler Hall, Overseer Chamber (a wall lantern on a base of 0); dim = Guard Post, Mushroom Cavern, Overseer Hall; dark = Flooded Tunnel, Goblin Barracks. The Hidden Cache (dark) is a side room and is not on the route.
- **To hit**: dim -1 melee / -2 bow, dark -3 / -6, for an attacker who cannot see. A player never sees in the dark; every creature does except the Giant Cave Toad (`DarkSight = false`), which fights the player on equal terms. A bow is a bow in the game's sense (`QueryWeaponCategory() == "bow"`): the melee Scout of the standard test holds the Hunting Bow, so his Precision Strike takes the bow penalty. Spells that need no roll (Arcane Bolt) are not affected. Dodge, damage and the creatures' attacks on the player are not changed.
- **Sneaking**: dim -10, dark -15 (-25 for the Toad), and +20 when the Scout carries a lit light, added to each creature's chance before the 5 to 100 clamp.
- **A character's light** (the choice): `none`; `torch` (+1, lit only in rooms that are not lit already: a sensible player puts it out in a lit room, which also stops the +20); `lantern` (+2, same); `glow` (Mage and Priest only: Focus 20 / 23 / 26 by rank, the Mage's Focus Efficiency makes it 16 / 18 / 21, +1 light, 5 / 8 minutes, recast when it runs out, no light if the Focus is not there); `torch+glow`.
- **Not modelled**: a torch burning down (10 or 25 minutes; a lantern lasts an hour, so the 33 to 53 minute cellar runs need one to three torches: 5 to 15 gold, spec section 9), the gold, the half second a `light` command takes, and the free Rag Torch. A character is assumed always to have a fresh light, which flatters the torch a little.

## 3. The cellars' light, and what each light makes of it

The cellars' light with nothing carried, and what each light makes of it (level: 1 or more lit, 0 dim, -1 dark):
| room | nothing | torch | lantern | Glow | torch + Glow | to-hit melee / bow with nothing | notice change (creature that sees / the toad) |
|---|---|---|---|---|---|---|---|
| Tavern Cellar | lit | lit | lit | lit | lit | 0 / 0 | 0 / 0 |
| Collapsed Passage | lit | lit | lit | lit | lit | 0 / 0 | 0 / 0 |
| Storage Chamber | lit | lit | lit | lit | lit | 0 / 0 | 0 / 0 |
| Smuggler Hall | lit | lit | lit | lit | lit | 0 / 0 | 0 / 0 |
| Guard Post | dim | lit | lit | lit | lit | -1 / -2 | -10 / -10 |
| Flooded Tunnel | dark | dim | lit | dim | lit | -3 / -6 | -15 / -25 |
| Mushroom Cavern | dim | lit | lit | lit | lit | -1 / -2 | -10 / -10 |
| Goblin Barracks | dark | dim | lit | dim | lit | -3 / -6 | -15 / -25 |
| Overseer Hall | dim | lit | lit | lit | lit | -1 / -2 | -10 / -10 |
| Overseer Chamber | lit | lit | lit | lit | lit | 0 / 0 | 0 / 0 |

## 4. The standard test: all six rows, ranks I and II

Level 5, starting gear, 4 potions, points placed, the middle human with `rest` at RestFactor 5, from the cellar entrance to the Overseer; Scouts sneak. "lit world" is the game before this change (the numbers of `BALANCE_SCOUT.md`, within noise: one cell is about +-1 point and the difference of two about 1.5, so read nothing into less than 2). Rank I: nobody has bought a rank; rank II: everyone holds rank II.

Standard test, kill% against the Overseer from the cellar entrance, 1500 runs a cell, seed 1; the Scouts sneak. Rank I = nobody has bought a rank; rank II = everyone holds rank II.
| rank | light | warrior | scout (melee) | priest | mage | adventurer | archer | mean of five | archer - mean |
|---|---|---|---|---|---|---|---|---|---|
| I | lit world (before light) | 81.1 | 83.7 | 76.4 | 72.5 | 82.3 | 89.9 | 79.2 | +10.7 |
| I | none | 75.2 | 77.6 | 71.6 | 72.9 | 76.4 | 86.3 | 74.7 | +11.5 |
| I | torch | 80.5 | 81.2 | 77.1 | 73.9 | 82.9 | 89.9 | 79.1 | +10.7 |
| I | lantern | 81.1 | 83.3 | 76.4 | 72.5 | 82.3 | 89.6 | 79.1 | +10.5 |
| I | glow | - | - | 75.2 | 74.1 | - | - | - | - |
| I | torch+glow | - | - | 75.7 | 73.1 | - | - | - | - |
| II | lit world (before light) | 94.1 | 86.9 | 97.6 | 92.3 | 88.3 | 93.6 | 91.9 | +1.7 |
| II | none | 92.3 | 79.8 | 97.2 | 90.8 | 84.9 | 91.1 | 89.0 | +2.1 |
| II | torch | 93.8 | 85.9 | 98.1 | 92.5 | 87.7 | 92.9 | 91.6 | +1.3 |
| II | lantern | 94.1 | 85.9 | 97.6 | 92.3 | 88.3 | 91.9 | 91.6 | +0.2 |
| II | glow | - | - | 96.7 | 91.3 | - | - | - | - |
| II | torch+glow | - | - | 97.7 | 92.3 | - | - | - | - |

Reading it. With no light, the callings lose: Warrior -5.9, melee Scout -6.1, Priest -4.8, Adventurer -5.9, archer -3.6, Mage +0.4 (no change: Arcane Bolt needs no roll, and the Mage's Focus is not touched). **With a torch they are back to where they were** (Warrior 80.5 against 81.1, Mage 73.9 against 72.5, archer 89.9 against 89.9): the torch makes every dim room lit and every dark room dim, and the dark rooms are 2 of the 9. A lantern makes everything lit. Glow (+1) is the same as a torch for the two casters, a little worse because it costs Focus the Mage would spend on bolts (74.1 with it at rank I against 72.9 with nothing, within noise; 91.3 against 90.8 at rank II). **No calling falls below 71.6% in any row (the Priest with no light at rank I); with a torch the lowest is the Mage's 73.9%.**

## 5. A played progression through the cellars (level 1 to 5)

Rank I, starting gear, one potion at the start, the middle human, the Scouts sneaking; the first row of each calling is the game before light. Standard errors of the mean are in brackets.

A played progression level 1 to 5 through the cellars, rank I, 1500 runs a row, seed 20261124; Scouts sneak; the first row of each calling is the game before light.
| calling | light | deaths a run | potions used | creatures killed by the first action | creatures killed before they acted | fights with no damage taken | Vitality lost a fight | minutes to level 5 | reached level 5 | fights a run | fights opened from hiding |
|---|---|---|---|---|---|---|---|---|---|---|---|
| archer | lit world | 0.40 (+-0.02) | 2.73 | 9% | 11% | 4% | 47% | 33.5 (+-0.3) | 100% | 18.4 | 53% |
| archer | none | 0.53 (+-0.02) | 3.53 | 9% | 12% | 3% | 49% | 43.1 (+-0.3) | 99% | 21.7 | 65% |
| archer | torch | 0.45 (+-0.02) | 2.95 | 10% | 9% | 4% | 48% | 35.5 (+-0.3) | 100% | 19.1 | 44% |
| archer | lantern | 0.47 (+-0.02) | 2.82 | 10% | 9% | 4% | 47% | 34.4 (+-0.3) | 100% | 18.7 | 40% |
| melee Scout | lit world | 0.43 (+-0.02) | 2.91 | 13% | 14% | 3% | 47% | 33.6 (+-0.2) | 100% | 18.7 | 53% |
| melee Scout | none | 0.55 (+-0.02) | 3.62 | 14% | 16% | 3% | 48% | 43.1 (+-0.2) | 98% | 22.0 | 67% |
| melee Scout | torch | 0.48 (+-0.02) | 3.10 | 13% | 12% | 2% | 48% | 36.4 (+-0.2) | 100% | 19.6 | 44% |
| melee Scout | lantern | 0.47 (+-0.02) | 2.96 | 13% | 12% | 2% | 48% | 34.7 (+-0.2) | 99% | 19.1 | 40% |
| Warrior | lit world | 0.17 (+-0.01) | 2.84 | 4% | 0% | 0% | 45% | 46.0 (+-0.2) | 100% | 19.4 | 0% |
| Warrior | none | 0.18 (+-0.01) | 3.18 | 4% | 0% | 1% | 44% | 51.9 (+-0.2) | 100% | 21.1 | 0% |
| Warrior | torch | 0.16 (+-0.01) | 2.91 | 4% | 0% | 1% | 45% | 47.2 (+-0.2) | 100% | 19.8 | 0% |
| Warrior | lantern | 0.17 (+-0.01) | 2.84 | 4% | 0% | 0% | 45% | 46.0 (+-0.2) | 100% | 19.4 | 0% |
| Mage | lit world | 0.53 (+-0.02) | 3.30 | 3% | 0% | 0% | 59% | 52.0 (+-0.4) | 99% | 22.6 | 0% |
| Mage | none | 0.52 (+-0.02) | 3.39 | 3% | 0% | 0% | 58% | 53.5 (+-0.4) | 99% | 23.0 | 0% |
| Mage | torch | 0.52 (+-0.02) | 3.33 | 3% | 0% | 0% | 59% | 52.5 (+-0.4) | 99% | 22.6 | 0% |
| Mage | glow | 0.52 (+-0.02) | 3.34 | 3% | 0% | 0% | 59% | 52.7 (+-0.4) | 99% | 22.7 | 0% |
| Mage | torch+glow | 0.52 (+-0.02) | 3.33 | 3% | 0% | 0% | 59% | 53.5 (+-0.4) | 99% | 22.7 | 0% |
| Mage | lantern | 0.53 (+-0.02) | 3.30 | 3% | 0% | 0% | 59% | 52.0 (+-0.4) | 99% | 22.6 | 0% |
| Priest | lit world | 0.24 (+-0.01) | 3.06 | 1% | 0% | 0% | 76% | 53.2 (+-0.4) | 100% | 21.8 | 0% |
| Priest | none | 0.26 (+-0.01) | 3.38 | 1% | 0% | 0% | 77% | 59.4 (+-0.3) | 100% | 23.4 | 0% |
| Priest | torch | 0.23 (+-0.01) | 3.13 | 1% | 0% | 0% | 76% | 54.9 (+-0.4) | 100% | 22.2 | 0% |
| Priest | glow | 0.25 (+-0.01) | 3.15 | 1% | 0% | 0% | 76% | 55.2 (+-0.4) | 100% | 22.3 | 0% |
| Priest | torch+glow | 0.25 (+-0.01) | 3.04 | 1% | 0% | 0% | 76% | 53.1 (+-0.3) | 100% | 21.8 | 0% |
| Priest | lantern | 0.24 (+-0.01) | 3.06 | 1% | 0% | 0% | 76% | 53.2 (+-0.4) | 100% | 21.8 | 0% |

Reading it. **A character who goes in without a light pays in time and in life, not in the Overseer fight**: the archer needs 43.1 minutes, not 33.5, and dies 0.53 times a run, not 0.40; the Priest takes 59.4 minutes, not 53.2. A torch brings most of it back (archer 35.5 minutes, 0.45 deaths; Priest 54.9, 0.23) and a lantern the rest (34.4; 53.2). **The sneaking Scouts show the cost of carrying a light**: fights opened from hiding fall from 53% to 44% with a torch and 40% with a lantern (the +20), and the Scout who sneaks with no light opens 65%, because in the dark rooms every creature notices him 15 to 25 points less, but he fights at -6 with the bow and so spends much longer on each fight (43 minutes). That is the intended trade, and it is why the dark, not the lantern, is the Scout's hard choice. The Mage and Warrior are barely touched (Mage 52.5 against 52.0 minutes with a torch; a Warrior 47.2 against 46.0). Creatures killed by the first action and before they acted do not move with the light.

**What I would choose for each calling.** Warrior, Adventurer, Mage: a torch (a 30 gold lantern buys the Warrior 1.2 minutes and nothing in the standard test). Priest: a torch, and Glow when the torch is out. Scouts: a torch, put out in the lit rooms; the lantern only buys the dark rooms lit (the Barracks), at the price of +20 notice there. Glow gives the Mage and Priest nothing a torch does not in these rooms (Mage 52.7 minutes against 52.5; Priest 55.2 against 54.9), and costs Focus; it is for the character who has lost the torch, and for lighting a friend.

## 6. Alternatives that were measured, and why none is recommended

The standard test, every calling with a torch, the archer also with no light and with a lantern. S is the spec. N: a carried light adds more to the notice chances. R: the bow's penalties doubled and a half again. D: more rooms dark or black (D1 = the Guard Post, Mushroom Cavern and Overseer Hall dark, which in the standard test is the Cavern and the Hall; D2 adds the Tunnel and the Barracks black, -2, so that a torch only makes them dark and a lantern dim). C: combinations.

Alternatives to the spec's light numbers, standard test, 1500 runs a cell, seed 1: every calling with a torch (the archer also with no light / a lantern).
| option | rank | warrior | scout (melee) | priest | mage | adventurer | archer, torch | mean of five | archer - mean | archer, no light | archer, lantern |
|---|---|---|---|---|---|---|---|---|---|---|---|
| S: the spec as built | I | 80.5 | 81.2 | 77.1 | 73.9 | 82.9 | 89.9 | 79.1 | +10.7 | 86.3 | 89.6 |
| S: the spec as built | II | 93.8 | 85.9 | 98.1 | 92.5 | 87.7 | 92.9 | 91.6 | +1.3 | 91.1 | 91.9 |
| N1: a carried light adds 30 to the notice chances | I | 80.5 | 80.3 | 77.1 | 73.9 | 82.9 | 90.0 | 78.9 | +11.1 | 86.3 | 90.1 |
| N1: a carried light adds 30 to the notice chances | II | 93.8 | 85.3 | 98.1 | 92.5 | 87.7 | 92.9 | 91.5 | +1.5 | 91.1 | 93.3 |
| N2: a carried light adds 40 | I | 80.5 | 80.1 | 77.1 | 73.9 | 82.9 | 88.5 | 78.9 | +9.6 | 86.3 | 88.9 |
| N2: a carried light adds 40 | II | 93.8 | 83.5 | 98.1 | 92.5 | 87.7 | 94.3 | 91.1 | +3.1 | 91.1 | 93.2 |
| R1: a bow at -3 in the dim and -9 in the dark | I | 80.5 | 82.3 | 77.1 | 73.9 | 82.9 | 87.9 | 79.4 | +8.6 | 79.8 | 89.6 |
| R1: a bow at -3 in the dim and -9 in the dark | II | 93.8 | 85.3 | 98.1 | 92.5 | 87.7 | 93.1 | 91.5 | +1.7 | 87.5 | 91.9 |
| D1: Guard Post, Mushroom Cavern and Overseer Hall dark | I | 76.1 | 74.7 | 74.5 | 71.9 | 77.7 | 87.0 | 75.0 | +12.0 | 67.9 | 89.6 |
| D1: Guard Post, Mushroom Cavern and Overseer Hall dark | II | 92.5 | 80.3 | 96.7 | 93.1 | 85.3 | 90.8 | 89.6 | +1.2 | 75.2 | 91.9 |
| D2: D1, and the Tunnel and the Barracks black (-2) | I | 75.2 | 74.7 | 71.6 | 72.9 | 76.4 | 83.2 | 74.2 | +9.0 | 67.9 | 89.9 |
| D2: D1, and the Tunnel and the Barracks black (-2) | II | 92.3 | 76.3 | 97.2 | 90.8 | 84.9 | 90.8 | 88.3 | +2.5 | 75.2 | 92.9 |
| D3: Mushroom Cavern and Overseer Hall dark, the Chamber dim | I | 76.1 | 74.7 | 74.5 | 71.9 | 77.7 | 87.0 | 75.0 | +12.0 | 52.7 | 89.6 |
| D3: Mushroom Cavern and Overseer Hall dark, the Chamber dim | II | 92.5 | 80.3 | 96.7 | 93.1 | 85.3 | 90.8 | 89.6 | +1.2 | 66.5 | 91.9 |
| C1: D1 + N1 | I | 76.1 | 74.9 | 74.5 | 71.9 | 77.7 | 86.6 | 75.0 | +11.6 | 67.9 | 90.1 |
| C1: D1 + N1 | II | 92.5 | 79.5 | 96.7 | 93.1 | 85.3 | 91.8 | 89.4 | +2.4 | 75.2 | 93.3 |
| C2: D3 + R1 | I | 76.1 | 70.1 | 74.5 | 71.9 | 77.7 | 84.2 | 74.1 | +10.1 | 17.6 | 89.6 |
| C2: D3 + R1 | II | 92.5 | 77.3 | 96.7 | 93.1 | 85.3 | 90.4 | 89.0 | +1.4 | 26.5 | 91.9 |

- **N1, N2** (a carried light costs 30, 40): nothing in the standard test (archer 90.0, 88.5; the stealth opener is worth 0.9 points there). They should show in the cellar run (fights opened from hiding), which I did not measure for the alternatives, and they make the torch Scout openly worse than the one who goes in the dark, which I think is wrong.
- **R1** (-3 / -9 for a bow): lowers only the archer who has no light (86.3 to 79.8); with a light nothing changes.
- **D1, D3** (more dim rooms made dark): archer with a torch 87.0, but the Warrior falls to 76.1, the Mage 71.9, the Priest 74.5; a lantern becomes the only way to see, and the archer with a lantern is untouched (89.6).
- **D2** (the two deepest rooms black): archer 83.2 at rank I, the nearest to the band; Priest 71.6, Mage 72.9, Warrior 75.2. At rank II the archer is 90.8, so the band is still missed there. It turns the cellars into a place where a lantern is compulsory from the Guard Post on.
- **C1, C2**: the same story with the numbers worse for the Scout (melee Scout 70.1 in C2).

**So**: the light is a tax on the unprepared, as the spec intended, not a lever on the best line. The only things that bring the archer down are Aimed Shot's cost, a Scout who goes into a dark room with no light, or a room that stays dark whatever he carries. If the owner wants the first, take `BALANCE_SCOUT.md` E2; the second he already pays; the third (D2) is a design change for the owner to make with open eyes.

## 7. Limits of these measurements

- The simulator's player always has the light he chose, and uses it perfectly: he lights it on entering a room that needs it and puts it out in a lit one. A real player forgets, and a torch burns out in the middle of a fight; that is more cost than modelled.
- Darkness also takes away the exits, the creature names and the loot, and the player may not `get` in the dark (they light a torch for that): it costs time not modelled here. The cellar-run times for "none" are probably too kind.
- The Glow rows cast the spell when a room needs it and no Glow is running, and pay the Focus then (rank I in the rank I rows, rank II in the rank II rows).
- The `light_test.py` step `tohit` checks the penalties against real fights: 62% of a lit blind creature's swings land, 44% in the dark (-3), 61% for a creature that sees in the dark (about 55 swings a cell, so +-6 points).
