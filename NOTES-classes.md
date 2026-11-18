# Stage A progress log (feature/classes-a)

Removed at merge.

## State (WIP commit 1)
- Step 1 resources and rolls: DONE in code (/std/living: Stamina/Mana lazy regen, dodge/block/crit, timed effects incl. speed, stagger, absorb). Untested beyond smoke.
- Step 2 skills/spells/traits: DONE in code (/std/skill, /std/trait, /skills/*, `skills` command, item-granted skill hook). No class_test yet.
- Step 3 equipment categories and access: DONE in code (categories on every weapon/armor, 3 new weapons at Torren, refusals).
- Step 4 class framework: DONE in code (/std/pclass, /secure/classes registry discovered by directory listing, master allows get-dir for it on /classes and /skills only, level cap 100, xp formula in Adventurer.ExperienceFor, attack/defense rule). Creature armor NOT yet retuned.
- Step 5 archetypes: DONE in code (5 class files, choice in login.cs, heal->restore, smite->slay, setlevel). Smoke test adapted (Client.choose_calling).
- Step 6 naming/pass: DONE in code (banner, area.cs per directory, travel pass, Rowan, North Road lock, Vale Road). Oakenfell / Hall of Legends names applied.
- Step 7 balance/tests/docs: NOT STARTED (balance_sim, class_test, other tests, docs).

## Decisions
- Sandbox forbids System.Globalization: effect mod strings are parsed by hand.
- Unarmed base is 1-3; Improvised Combat makes it 2-5 (design implied this).
- Test default archetype is adventurer.
