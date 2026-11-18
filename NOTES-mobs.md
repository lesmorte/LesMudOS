# NOTES-mobs.md (branch work/mobs): corridor creatures and pace

Task: 31 creature files in mudlib/domains/greenvale/corridor/mob/, the pace model and a proposed experience table in tests/balance_sim.py, docs/CORRIDOR_BALANCE.md, tests/corridor_mob_test.py. Work only in /home/claude/wt-mobs. No push, merge or branch switch. Do not edit existing mudlib/src files.

## Done (all three parts)
- Part 1: pace model (`python3 tests/balance_sim.py pace 250`), table 6:5400 7:12000 8:21700 9:34100 10:49300, tail T10*(n/10)^3.
- Part 2: 31 creature files; build 0 warnings 0 errors; load_check all OK.
- Part 3: `check` compares the 31 files; `corridor` prints the win table; tests/corridor_mob_test.py (port 4966-4969) passes; docs/CORRIDOR_BALANCE.md written.

## Next
- Nothing on this branch. The lead applies section 4.3 of the doc to /std/adventurer.cs ExperienceFor, and the tests named there.

## Assumptions
- Gear: village only. 4 s per room, 3 rooms between kills, 3 s to loot, regen 0.1 HP/s, potions 15 gold per 20 HP, havens by level (see the doc, 2.3).
- Low animals (rabbit, deer, fox) pay token XP, a deliberate deviation from "more than the village".
