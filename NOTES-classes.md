# Stage A progress log (feature/classes-a)

Removed at merge.

## State (final)
- All seven steps done and committed. Tests: smoke 236, area 303, timing 74 (x3), ansi 82, load_check 0 failed, class_test 140 checks (4 clean runs). Play-throughs, all without wizard help, all level 5 (mage run 3: see report): warrior 23.4 min; scout 20.2; priest 29.5 (rerun clean); mage 16-18 min; adventurer 19.3 (rerun clean). Earlier failures were test expectations only (axe for non-axe callings, bolt lines, Traveler's Instinct exit detail, desynchronised drop).
- Lost-property rename done: /secure/lost_property, legacy /secure/vault.json read once and carried over (tested in class_test).
- Balance: tests/balance_sim.py tables are the reference; creature armor unchanged.

## Decisions
- Sandbox forbids System.Globalization: effect mod strings are parsed by hand.
- Unarmed base is 1-3; Improvised Combat makes it 2-5 (design implied this).
- Test default archetype is adventurer.
