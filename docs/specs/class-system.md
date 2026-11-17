# Specification: character class progression (the "Class System")

This is the class system brief as supplied by the project owner. It is the source of truth for the progression structure. It is queued as the next element after combat timing pass 1; no implementation design exists yet.

---

# Character Class Progression System

Implement a hierarchical class progression system based on Archetypes, Advanced Classes, and future Subclasses.

The system should be designed for long-term expansion while remaining simple for initial release.

## Design Goals

The class system should:

- Be easy for new players to understand
- Provide meaningful choices
- Support future content expansion
- Encourage exploration and discovery
- Support hybrid and non-traditional playstyles
- Allow prestige classes without creating separate leveling systems
- Support guilds and class trainers as world content

All classes in the game share the same core leveling system.

There is only one character level.

There are no separate class levels.

There are no separate prestige levels.

## Character Progression Structure

```
Level 1    Choose Archetype
   ↓
Level 5    Choose Advanced Class
   ↓
Level 10   Choose Subclass (future system)
   ↓
Future Prestige Classes (optional specialization path)
```

## Character Levels

All characters level from Level 1 → 100 (or whatever maximum level is selected).

All characters use the same progression formula.

The exact leveling formula will be configurable and data-driven.

## Archetypes (Level 1)

Upon character creation, every player selects one of five archetypes.

These archetypes provide:

- Starting statistics
- Initial equipment
- Initial skills
- Resource focus
- Guild affiliation
- Character identity

Archetypes should remain relevant throughout a character's life.

### Warrior

- Role: Front-line martial combat
- Primary Resource: Stamina
- Focus: Defense, melee combat, weapon mastery
- Starting Features: Shield Bash, Defensive Training, Weapon Proficiency

### Scout

- Role: Agility and precision
- Primary Resource: Stamina
- Focus: Speed, ranged combat, critical strikes
- Starting Features: Evasion, Tracking, Precision Strike

### Priest

- Role: Support and divine magic
- Primary Resource: Mana
- Focus: Healing, blessings, defensive magic
- Starting Features: Minor Heal, Blessing, Divine Knowledge

### Mage

- Role: Arcane magic
- Primary Resource: Mana
- Focus: Spellcasting, utility, magical damage
- Starting Features: Arcane Bolt, Mana Efficiency, Magical Lore

### Adventurer

- Role: Versatile generalist
- Primary Resource: Stamina and Mana
- Focus: Adaptability, exploration, utility, hybrid playstyles

The Adventurer sacrifices specialization for flexibility.

They begin with moderate combat ability, broad equipment access, and unique utility skills.

- Starting Features: Survival Training, Appraise, Improvised Combat, Traveler's Instinct
- Starting Benefits:
  - Can use most basic weapon types
  - Can equip light armor
  - Gains bonus exploration-related abilities
  - Greater flexibility for future class paths

The Adventurer exists to support hybrid classes, unusual character concepts, and future prestige paths.

## Level 5 Advancement

At Level 5, players must travel to the All Hall Guild.

At the All Hall, players select an Advanced Class associated with their Archetype.

This choice is intended to feel significant and should be performed through interaction with the game world rather than menu selection.

### Warrior Advanced Classes

| Class | Description |
|---|---|
| Knight | Heavy armor defender. |
| Berserker | Aggressive melee damage dealer. |
| Monk | Disciplined martial artist. |

### Scout Advanced Classes

| Class | Description |
|---|---|
| Rogue | Stealth and assassination. |
| Ranger | Ranged combat and survival. |
| Bard | Support, inspiration, and utility. |

### Priest Advanced Classes

| Class | Description |
|---|---|
| Cleric | Healing and protection. |
| Templar | Holy warrior. |
| Oracle | Divine support and foresight. |

### Mage Advanced Classes

| Class | Description |
|---|---|
| Mage | Pure arcane spellcaster. |
| Druid | Nature magic and shapeshifting. |
| Necromancer | Undeath, curses, and summons. |

### Adventurer Advanced Classes

These classes serve as bridges between traditional archetypes and future prestige paths.

**Spellblade.** Hybrid martial and magical combatant.

- Focus: Weapon-enhanced spells, Combat enchantments, Mana and Stamina management
- Example Skills: Arcane Strike, Weapon Infusion, Mana Blade
- Potential Future Subclasses: Arcane Knight, Rune Knight, Spell Duelist

**Beastmaster.** Explorer and companion handler.

- Focus: Animal companions, Wilderness survival, Tracking, Utility
- Example Skills: Call Companion, Beast Bond, Pack Tactics
- Potential Future Subclasses: Falconer, Wolfmaster, Warden

**Artificer.** Inventor, gadgeteer, and crafter.

- Focus: Devices, Consumables, Engineering, Item enhancement
- Example Skills: Deploy Turret, Mechanical Repair, Gadget Toss
- Potential Future Subclasses: Engineer, Alchemist, Runesmith

## Level 5 Advancement Benefits

Upon selecting an Advanced Class:

Gain:

- Advanced Class Title
- New Skill Tree
- Class-specific abilities
- Additional equipment options
- Guild Membership

Retain:

- Archetype Skills
- Archetype Traits
- Archetype Benefits

Example: Warrior → Knight. The character retains Warrior features while gaining Knight-specific features.

## Level 10 Subclasses (Future Feature)

The implementation framework should exist now but remain disabled until future content is developed.

Upon reaching Level 10, players become eligible for specialization.

Subclasses are not available through the All Hall.

Instead, players must discover and travel to specialized guild halls located throughout the world.

This encourages exploration and world interaction.

## Example progressions

| Archetype | Advanced Class | Subclass | Prestige |
|---|---|---|---|
| Warrior | Knight | Guardian | Royal Guardian |
| Warrior | Knight | Crusader | Holy Crusader |
| Scout | Ranger | Hunter | Beast Hunter |
| Scout | Ranger | Marksman | Eagle Eye |
| Mage | Necromancer | Bone Caller | Lich |
| Mage | Necromancer | Soul Reaper | Death Lord |
| Adventurer | Spellblade | Arcane Knight | Dragon Spellblade |
| Adventurer | Beastmaster | Warden | Wild Lord |
| Adventurer | Artificer | Runesmith | Master Runesmith |

## Prestige Classes

Prestige Classes are optional advanced specializations.

Prestige Classes should represent truly exceptional paths.

These classes do not replace the normal leveling system.

All characters continue using the same character level progression.

### Prestige Class Requirements

Prestige Classes may require:

- Character Level
- Quest Completion
- Difficult Achievements
- Rare Discoveries
- Guild Reputation
- Hidden Trainers
- World Exploration

Prestige Classes should feel earned rather than selected.

### Prestige Experience Modifier

Prestige Classes use an Experience Modifier.

This modifier increases the experience required per level.

| Class | EXP Modifier |
|---|---|
| Standard Class | 1.0x |
| Spellblade | 1.25x |
| Dragon Knight | 1.50x |
| Arch Lich | 2.00x |

The base leveling formula remains unchanged.

Required EXP = Base EXP × Prestige Modifier

This allows prestige classes to feel rare and prestigious without introducing separate progression systems.

## Guild System

Guild Halls are physical world structures.

Guilds serve as progression hubs.

Guilds may provide:

- Advancement
- Quest Chains
- Trainers
- Lore
- Equipment Vendors
- Class Skills
- Prestige Opportunities

Players should physically travel to guilds to advance their careers.

Class progression should feel like part of the world rather than a menu selection.

## Class Identity Philosophy

Classes should primarily differ through:

- Skills
- Spells
- Item interactions
- Resource efficiency
- Reaction Time modifiers
- Equipment access
- Utility options

Avoid large stat differences that become difficult to balance.

This allows future tuning without redesigning existing classes.

## Long-Term Scalability

The progression system must support:

```
Archetype → Advanced Class → Subclass → Prestige Class → Future Prestige Tiers
```

without requiring database redesigns or progression rewrites.

All classes, subclasses, guilds, abilities, requirements, experience modifiers, and advancement paths should be data-driven and configurable through game content.

New classes should be addable years later without impacting existing player progression.
