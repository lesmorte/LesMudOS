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
- tests/craft_content_test.py; run all checks; docs (CRAFTING.md appendix, BUILDING.md); final report.

## Decisions
- Lessons: Berry Juice 3, Simple Bandage 2 (Tobin) , Herbal Bandage 5, Minor Healing Salve 8, Minor Stamina Tonic 6, Hunter's Poultice 15,
  Treated Leather 8, Traveler's Leather Wrap 10. Primer 5 gold.
- Discovery: Sweet Berry Juice = discovered, Blood Ink = rare, the other eight known.
- Hollow prices = material value; Hesper +1; tool prices 6/8/5 Hollow, 8/10/7 Corwin.
- Herbal Bandage 12 hp (fixture had 14) so even Masterwork-quality stays near a Common healing potion (20).
