# Specification: the Hollowmere attribute system (owner's brief)

The owner's brief for replacing traditional RPG attributes, condensed from the source document without changing its content. Source: <https://claude.ai/artifact/HEy3U6Gg8p13RykAnn9b31>. How it is to be built is in [../ATTRIBUTES.md](../ATTRIBUTES.md).

## The task

Refactor the character system to use a custom Hollowmere attribute model that reinforces the game's identity and worldbuilding. The attributes should feel like qualities spoken of in stories, songs, legends and historical records, not like traditional D&D-style statistics.

Players see **Might, Grace, Vigor, Insight, Spirit, Resolve** instead of Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma.

The attributes become foundational, used by: character creation, character progression, combat, crafting, gathering, spells, skills, NPC generation, creatures, equipment, Renown, and future prestige classes. The implementation is data-driven and extensible.

## Primary attributes

| Attribute | Represents | Impacts |
|---|---|---|
| Might | Physical power, force, martial strength | Melee damage, heavy weapon effectiveness, carry capacity, certain crafting activities |
| Grace | Precision, agility, speed, coordination | Accuracy, critical chance, evasion, harvesting performance, ranged combat |
| Vigor | Health, toughness, physical resilience | Maximum Vitality, disease resistance, poison resistance, Vitality regeneration. Primary survival stat. |
| Insight | Knowledge, learning, understanding | Maximum Focus, spell effectiveness, recipe discovery, magical crafting, exploration discoveries. Primary magical stat. |
| Spirit | Faith, intuition, inspiration, inner strength | Healing effectiveness, buffs, magical resistance, Focus regeneration |
| Resolve | Determination, discipline, endurance, perseverance | Maximum Endurance, fear resistance, stun resistance, recovery effects. Primary stamina stat. |

## Resources

HP becomes **Vitality**. Mana becomes **Focus**. Stamina becomes **Endurance**. All game systems use these names.

Level is the largest contributor; attributes create specialization.

| Resource | Formula |
|---|---|
| Maximum Vitality | 50 + (Level × 12) + (Vigor × 6) |
| Maximum Focus | 20 + (Level × 8) + (Insight × 5) + (Spirit × 2) |
| Maximum Endurance | 30 + (Level × 10) + (Resolve × 5) + (Vigor × 2) |

## Secondary derived statistics

Calculated automatically; players may not assign points to them directly.

| Statistic | Formula | Used for |
|---|---|---|
| Physical Power | (Might × 2) + (Level ÷ 2) | Physical damage scaling |
| Precision | Grace × 2 | Weapon accuracy |
| Critical Chance | Grace × 0.25% (Grace 20 gives 5%) | Base critical chance |
| Evasion | Grace + (Level ÷ 2) | |
| Spell Power | (Insight × 2) + Spirit | |
| Healing Power | (Spirit × 2) + Insight | |
| Mental Resistance | Spirit + Resolve | Fear, charm, confusion, domination, mind effects |
| Physical Resistance | Vigor + Resolve | Stuns, knockdowns, interrupts, forced movement |
| Carry Capacity | 50 + (Might × 5), in pounds | Integrates with the Encumbrance System |
| Gathering Efficiency | Grace + Insight | Harvesting, skinning, bleeding, future gathering |
| Crafting Aptitude | Insight + Resolve | Crafting quality, experimentation, recipe discovery, rare outcomes |

## Regeneration (per minute)

| Resource | Formula |
|---|---|
| Vitality | 1 + (Vigor ÷ 5) |
| Focus | 1 + ((Insight + Spirit) ÷ 10) |
| Endurance | 1 + ((Resolve + Vigor) ÷ 10) |

## Progression

Every level grants 2 attribute points, which players allocate freely. Attribute growth remains player-driven.

## Class modifiers

Classes do not replace attribute scaling. Modifiers apply after calculation, so that all classes work from the same formulas. Examples: Knight +15% Vitality; Mage +15% Focus; Berserker +15% Endurance; Rogue +10% Critical Chance; Druid +10% Focus and +10% Endurance.

## Design goals

The system should feel unique to the setting. NPC dialogue, books, Chronicle entries, quest text and reputation descriptions should naturally reference attributes: "The knight was remembered for his Resolve." "The ranger possessed remarkable Grace." "Her Insight uncovered secrets thought lost forever." Attributes reinforce the themes of legends, memory and storytelling. The system should not feel derivative of tabletop RPGs while remaining immediately understandable.

Implement all attributes, resource calculations, derived statistics and future extensibility based on this specification.
