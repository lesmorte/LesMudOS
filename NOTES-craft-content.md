# C2 crafting content: working notes

## Done
- A: launch data moved from tests/fixtures/crafting/launch into the mudlib (/crafting/launch_materials, /recipes/launch,
  /domains/hollow/obj/craft/*), classes renamed Hc* -> Cr*; items completed (descriptions, effects, tools, book). craft_sim reads real data;
  craft_test removes the real launch files from its copy (the sample set reuses the launch ids); craft_sim hides them for the samples run.
- B: station objects in healer, tannery, workshop, tavern (Hollow) and court (workbench, rack), Silver Stag (fire). Room details with
  the same words removed (the station answers to them).
- C/D: Elspeth, Dunstan, Tobin, Harland are /std/trainer (Trainer now inherits Shopkeeper: needed so one NPC both sells and teaches;
  Shopkeeper adds its verbs only when it has stock). Hesper sells, Corwin adds the tools.
- E: notice post on the Hollow square; help text.

## Next
- Nothing: final report. All parts committed (A c8fdad3, B 305f79e, C/D 5f24650, E 7557698).

## Results
- build 0/0; lint hollow 0 errors, oakenfell 0; load_check 0 failed; craft_sim PASS; craft_content_test 285/0; craft_test 254/0;
  smoke, area, class, item, find (158), chronicle, chronicle_play 0 failures.
- city_test: 234 failures, identical count on the base commit b59db8f (corridor rooms not in the build doc, etc.); 3 non-corridor ones also on base.
- world_test: 2 fail (hidden exit use, room band) mine; 4 fail on the base copy (partial run, killed by restart): timing-related/pre-existing.

## Decisions
- Lessons: Berry Juice 3, Simple Bandage 2 (Tobin) , Herbal Bandage 5, Minor Healing Salve 8, Minor Stamina Tonic 6, Hunter's Poultice 15,
  Treated Leather 8, Traveler's Leather Wrap 10. Primer 5 gold.
- Discovery: Sweet Berry Juice = discovered, Blood Ink = rare, the other eight known.
- Hollow prices = material value; Hesper +1; tool prices 6/8/5 Hollow, 8/10/7 Corwin.
- Herbal Bandage 12 hp (fixture had 14) so even Masterwork-quality stays near a Common healing potion (20).
