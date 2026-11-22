# Specification: Crafting and Gathering System v1 (owner's brief)

The owner's design for crafting and gathering, condensed from the source document without changing its content. Source: <https://claude.ai/artifact/7Kw3ZT5X5rAzpy3LUEDKkS>. How it is to be built is in [../PLAN-corridor-crafting.md](../PLAN-corridor-crafting.md).

## Design goals

The initial crafting system should:

- Be immediately understandable by new players
- Encourage gathering and exploration
- Create a player-driven market economy
- Support future profession systems
- Make every creature and resource node potentially valuable
- Reward player skill progression and better tools
- Introduce quality as a foundational mechanic

The primary objective is not crafting complexity. The objective is teaching players: everything has value; everything can become something else; every gatherer contributes to the economy.

Core loop: Gather → Refine → Craft → Use or Sell → Improve Skills → Gather Better Materials.

## Gathering skills

All characters can learn gathering skills. No profession lock exists at launch. Players improve naturally through successful harvesting.

| Skill | Tool | Valid targets | Produces |
|---|---|---|---|
| Skinning | Skinning Knife | Rabbits, wolves, deer, boars, future beasts | Skin of {Creature}: Rabbit Skin, Wolf Skin, Deer Skin, Boar Skin |
| Bleeding | Bleeding Kit | Fresh corpses | Blood of {Creature}: Rabbit Blood, Wolf Blood, Goblin Blood, Boar Blood |
| Harvesting | Harvest Sickle | Berry bushes, herb patches, wildflowers, thistle patches | Blackberries, Blueberries, Healing Herbs, Wildflowers, Thistle |

**Skinning formula.** Harvest Quality Score = Skinning Skill + Tool Quality Modifier + Creature Difficulty Modifier + Random Variation. Produces Poor, Common, Fine, Excellent or Masterwork Rabbit Skin.

Blood becomes important for alchemy, writing inks, runecrafting and ritual magic.

Future gathering skills, reserved but not implemented: Woodcutting, Mining, Fishing, Prospecting, Mushroom Gathering, Hunting, Archaeology, Relic Recovery.

## Tool quality

All gathering tools have quality ratings: Poor, Common, Fine, Excellent, Masterwork Skinning Knife. Higher-quality tools increase harvest quality, harvest success rates and rare material chances.

## Item quality

Every harvested material possesses a quality rating. Tiers: Poor, Common, Fine, Excellent, Masterwork, Legendary.

Quality impacts vendor value, market value, crafting output quality, future advanced recipes and quest requirements.

**Inheritance.** Crafted items inherit qualities from ingredients. Example: Excellent Healing Herb + Fine Wildflower = high chance of Fine Healing Salve or Excellent Healing Salve.

## Market seeding

All common materials begin available from village vendors, so players can immediately learn crafting. One material is not available.

**Launch scarcity material: Goblin Blood.** Cannot initially be purchased; must be harvested from goblins. Purpose: generate player demand, encourage hunting, encourage trading, create a functioning player economy. Every other beginner ingredient can be purchased from village suppliers. Goblin Blood becomes the first true player-supplied commodity.

## Recipe discovery

Recipes are not automatically known. Players discover them through trainers, books, quest rewards, experimentation, rumors and guilds.

| Discovery level | Meaning |
|---|---|
| Known | Recipe appears in the crafting interface. |
| Discovered | Player independently discovered the recipe. |
| Rare | Hidden recipe found through exploration. |
| Secret | Not taught by any trainer. Must be discovered through experimentation. |

**Experimentation.** Players may combine ingredients at crafting stations. When a valid combination exists, the recipe becomes permanently learned. The first player to discover a hidden recipe may receive Renown, Chronicle entry consideration and a discovery achievement. Future expansion should heavily utilize recipe discovery.

## Crafting stations

Simple items may be made anywhere. More advanced items require stations. Stations serve as social and economic hubs.

| Station | Used for | Location |
|---|---|---|
| Herbalist Table | Salves, tonics, poultices, potions | Vincent's Hollow healer |
| Tanning Rack | Leather processing, hide treatment, future leatherworking | Village tanner |
| Workbench | Utility items, tool creation, future engineering | Village craftsman |
| Cooking Fire | Cooked food, beverages, future cooking recipes | Village inn |
| Scribing Desk | Inks, scrolls, maps, books | Future use; not available at launch |

## Starting materials (available around Vincent's Hollow)

Rabbit Skin, Wolf Skin, Deer Skin, Rabbit Blood, Wolf Blood, Goblin Blood, Blackberries, Blueberries, Healing Herbs, Wildflowers, Thistle, Honey, Flour, Clean Water, Cloth Strip, Empty Bottle.

## Launch recipes

All recipes use at most three ingredients. All are obtainable near Vincent's Hollow.

| # | Recipe | Ingredients | Station | Effect |
|---|---|---|---|---|
| 1 | Berry Juice | Blackberries + Clean Water | Cooking Fire | Restore minor Vitality |
| 2 | Sweet Berry Juice | Berry Juice + Honey | Cooking Fire | Restore Vitality and Endurance |
| 3 | Simple Bandage | Cloth Strip | None | Minor healing |
| 4 | Herbal Bandage | Cloth Strip + Healing Herb | Herbalist Table | Moderate healing |
| 5 | Minor Healing Salve | Healing Herb + Wildflower | Herbalist Table | Healing over time |
| 6 | Minor Endurance Tonic | Blueberries + Honey | Herbalist Table | Restore Endurance |
| 7 | Treated Leather | Rabbit Skin + Wolf Blood | Tanning Rack | Crafting component; higher vendor value |
| 8 | Traveler's Leather Wrap | Treated Leather + Cloth Strip | Workbench | +1 carry capacity; starter utility equipment |
| 9 | Blood Ink | Goblin Blood + Wildflower | Workbench | Future crafting component, used by scribes, scholars and rune crafters. Primary reason Goblin Blood has economic value. |
| 10 | Hunter's Poultice | Healing Herb + Goblin Blood + Cloth Strip | Herbalist Table | Improved healing over time. Best healing item available near Vincent's Hollow. Creates consistent demand for Goblin Blood. |

## Recipe difficulty

Every recipe has a difficulty: Simple, Novice, Apprentice, Journeyman, Expert, Master. Launch recipes are Simple or Novice only.

## Recipe success

Crafting should not normally fail. Higher skill produces better quality results, bonus outputs and resource efficiency. Poor skill creates poorer quality products rather than destroying materials. This reduces frustration while preserving progression.

## Long-term expansion

Future systems must support Alchemy, Cooking, Leatherworking, Blacksmithing, Woodworking, Scribing, Runecrafting, Enchanting, Engineering, Tailoring and Jewelcrafting. All future crafting systems continue using gathering skills, material quality, recipe discovery, crafting stations and ingredient inheritance. The systems introduced in Vincent's Hollow should remain relevant throughout the lifetime of Hollowmere and serve as the foundation of the world's economy.

## Design note from the owner

Store recipes as data, not code. Use a generic ingredient matcher, quality inheritance engine and discovery system now, since the recipe count will grow from 10 to 1,000+. The architecture should assume that eventually every creature, plant, ore, fish and relic in Hollowmere can be harvested and incorporated into crafting.
