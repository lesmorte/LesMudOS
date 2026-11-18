# NOTES-mobs.md (branch work/mobs): corridor creatures and pace

Task: 31 creature files in mudlib/domains/greenvale/corridor/mob/, the pace model and a proposed experience table in tests/balance_sim.py, docs/CORRIDOR_BALANCE.md, tests/corridor_mob_test.py. Work only in /home/claude/wt-mobs. No push, merge or branch switch. Do not edit existing mudlib/src files.

## Done
- Simulator engine extended (timed effects on both sides, slow/stagger/weaken/buff/summon actions, player stagger, creature dodge). Roster of 31 draft in `CORRIDOR` (tests/balance_sim.py).

## Next
1. Tune bosses by hand (HP, specials), remove outliers (priest vs fast hitters), pace model, xp numbers, table.
2. Generate the 31 .cs files (generator lives in /tmp/mobs_work, not committed), `check` mode for corridor files.
3. docs/CORRIDOR_BALANCE.md, tests/corridor_mob_test.py, load_check.

## Assumptions (so far)
- Gear: village only. Warrior/Adventurer Overseer's axe + small shield; Scout Fine Iron Longsword; Priest Novice's Mace + shield; Mage Iron Dagger. All: vest 3 + helm 1, +5 HP trinket.
- Player HP regen 1 HP/10 s (adventurer x1.5); potions 15 gold for 20 HP.
- 4 s per room, 3 rooms between kills, 3 s to loot.
