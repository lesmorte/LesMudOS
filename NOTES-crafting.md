# C1 crafting engine: working notes

## Done
- Item hooks (PropertyAllowed, StateChanged); Thing.CloneStated + CreateIn with records; shop stock by record.
- /std/material, skin, blood (scarce guard), tool, station, recipe_book, crafted, craft_data, recipe_set.
- /secure/crafting daemon: data loading from /crafting and /recipes, index, clash rule, matcher, formulas, commands,
  Teach, StartHeal, test roll modes, help.
- corpse yields (kind, level, skin, blood, once each, freshness, loot lock); node TryGather guard; Hollow mobs Skin/Blood.
- adventurer crafting block (saved proficiencies and recipes, guarded setters, verbs, apply); living QueueAction; help.
- master: daemon preload + get-dir on /crafting and /recipes. events/craft.cs.
- /crafting/numbers.cs, proficiencies.cs, beasts.cs.

## Next
- /std/trainer; test fixtures (tests/fixtures/crafting); tests/craft_test.py; tests/craft_sim.py; armor CarryBonus;
  docs/CRAFTING.md, BUILDING.md, GAME_SYSTEMS.md; run all suites.

## Decisions
- Recipes and tables are C# data files (house style, type-checked by dotnet build); a file may hold many recipes
  (compile cost is per file, ~50 ms).
- Materials table (id -> file [+ kind], categories) is the single source of material ids and categories; item files
  hold only names and value. Identity = program path + "kind" property; forged /wiz items are never materials.
- Corpse failure uses the one chance (a botched cut ruins the hide); node failure keeps the charge. Growth only on success.
- Clash rule: more exact (material-id) ingredients win; equally exact recipes that could take the same materials clash,
  the later-loaded (path, then order) is refused and logged.
- Effects of crafted consumables live in the output's file (/std/crafted), not the recipe.
