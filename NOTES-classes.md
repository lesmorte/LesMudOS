# Stage A progress log (feature/classes-a)

Removed at merge.

## State (after the test and balance pass)
- Steps 1-6 implemented and committed. Step 7: balance_sim extended (all five archetypes, `check` mode compares class files and skills too); class numbers tuned from it (warrior 40 HP, +2 armor, +2 armed to-hit, bash 3/4 damage; scout 26 HP, 8% dodge, 8 stamina regen/10 s, strike 25 stamina +2 hit 15% crit; priest 26 HP, mana regen 4, heal 8 HP for 25 mana; mage 24 HP, bolt 6-9 for 20 mana). Creature armor NOT changed (not needed).
- Tests green: smoke 236, area 303, timing 74 (x3), ansi 82, load_check, class_test (x3, 0 failures). Play-throughs: warrior done (23.4 min, 1 deliberate death, level 5, no help); scout, priest, mage, adventurer pending/see final report.
- Docs updated: CLASS_SYSTEM (with section 12), COMBAT_TIMING, GAME_SYSTEMS, BUILDING, README, WORLD.
- Lost-property rename (/secure/vault -> /secure/lost_property) is the last commit, after the play-throughs.

## Decisions
- Sandbox forbids System.Globalization: effect mod strings are parsed by hand.
- Unarmed base is 1-3; Improvised Combat makes it 2-5 (design implied this).
- Test default archetype is adventurer.
