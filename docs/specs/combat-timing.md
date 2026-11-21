# Specification: combat timing (the "Speed System")

This is the combat timing brief as supplied by the project owner. It is the source of truth for the timing model. How it is being implemented, and in what order, is in [COMBAT_TIMING.md](../COMBAT_TIMING.md).

---

# Combat Timing System: Reaction Time and Recovery

Implement a universal combat timing system that applies equally to:

- Players
- NPCs
- Bosses
- Summons
- Pets
- Guards
- Creatures
- Event Characters

There must be no separate combat rules for NPCs. All entities in the game operate under the same timing model.

---

# Design Goals

The combat system should:

- Feel tactical rather than turn-based
- Create meaningful differences between fast and slow combatants
- Allow equipment to influence combat style
- Support future skills, spells, subclasses, and creatures
- Allow players to learn and predict enemy attack patterns
- Scale from 1v1 combat to large raids and world events

Combat should be event-driven rather than round-based.

---

# Core Resources

Every combatant has:

- Vitality
- Endurance (used for physical skills)
- Focus (used for spells)
- Reaction Time
- Skills
- Spells
- Equipment

---

# Reaction Time

Reaction Time defines:

"When can I begin my next action?"

Reaction Time is a character statistic.

Lower values are better.

Fast characters recover their composure, identify opportunities, and commit to new actions more quickly.

Reaction Time belongs to the character, not the skill.

Examples:

Rogue: Reaction Time 1.5

Knight: Reaction Time 4.0

Goblin Scout: Reaction Time 1.5

Ogre Brute: Reaction Time 5.0

Reaction Time should normally range:

1.0 - 6.0

Using 0.5 increments.

---

# Recovery

Recovery belongs to an individual action.

Every attack, skill, spell, item ability, monster attack, or special move has its own Recovery value.

Recovery represents:

"How long does it take to complete and recover from this specific action?"

Examples:

Quick Jab Recovery 0.5

Arrow Shot Recovery 1.0

Fireball Recovery 1.5

Shield Slam Recovery 2.0

Earthquake Recovery 3.0

Meteor Swarm Recovery 4.0

Two abilities used by the same character may have dramatically different Recovery values.

---

# Action Timing Formula

Total Delay = (Reaction Time + Recovery Modifiers + Action Recovery) × 2 seconds

Example:

Knight

Reaction Time: 4.0

Heavy Slash

Action Recovery: 1.5

Total Delay:

(4.0 + 1.5) × 2

= 11 seconds

The Knight cannot begin another action until 11 seconds have elapsed.

---

# Equipment Modifiers

Equipment may modify Reaction Time.

Examples:

Dagger Reaction Modifier +0.0

Short Sword Reaction Modifier +0.5

Long Sword Reaction Modifier +1.0

War Hammer Reaction Modifier +2.0

Tower Shield Reaction Modifier +1.0

Heavy Plate Armor Reaction Modifier +1.0

This represents additional effort, positioning, preparation, and recovery required by cumbersome equipment.

The final Reaction Time for a character is:

Base Reaction Time + Equipment Modifiers + Buffs + Debuffs + Encumbrance

---

# Equipment Granted Abilities

Equipment may grant active skills while equipped.

These skills do not belong to the class.

They belong to the item.

When the item is equipped:

- Skill becomes available
- Skill appears in ability list
- Skill may be learned automatically
- Skill disappears when item is removed

This allows equipment to dramatically alter play styles.

---

# Example: Spear

Equipping a Spear grants:

Triple Thrust

Cost: 25 Endurance

Reaction Modifier: +1.0

Recovery: 3.0

Description:

Perform three rapid thrust attacks against a single target.

Resolution:

Attack #1

Attack #2

Attack #3

Each attack rolls independently for:

- Hit
- Critical
- Dodge
- Block

The skill has a significant timing penalty.

Example:

Ranger

Base Reaction: 2.0

Triple Thrust:

Reaction Modifier: +1.0

Recovery: 3.0

Total Delay:

(2.0 + 1.0 + 3.0) × 2

= 12 seconds

This creates a powerful burst option that significantly delays further actions.

---

# Example Item-Granted Skills

Great Axe

Whirlwind

Attack all adjacent targets

Reaction Modifier: +1.5

Recovery: 3.0

---

Longbow

Power Shot

Single devastating ranged attack

Reaction Modifier: +1.0

Recovery: 2.5

---

Staff

Arcane Burst

Small area magical attack

Reaction Modifier: +0.5

Recovery: 2.0

---

Tower Shield

Shield Wall

Defensive stance

Reaction Modifier: +1.5

Recovery: 1.5

---

# Action Phases

Every action has three phases.

## 1. Preparation

Action begins.

Animation, casting, charging, aiming, positioning.

Interrupts can occur.

## 2. Resolution

Action executes.

Damage, healing, buffs, debuffs, movement, summons, etc.

occur here.

## 3. Recovery

The combatant regains balance and composure.

Cannot begin another action until recovery completes.

---

# Interrupt System

Actions may be interrupted during Preparation.

Possible interrupts:

- Stun
- Silence
- Knockdown
- Fear
- Disarm
- Specialized interrupt abilities

Interrupted actions:

- May consume partial resources
- May incur partial recovery
- Produce no primary effect

Powerful abilities should have longer preparation windows and greater interrupt vulnerability.

---

# Combat Queue

Every combatant tracks:

Current Action

Action Start Time

Action Completion Time

Ready Time

A combatant becomes eligible for a new action when:

Current Time >= Ready Time

The combat engine should process actions through a priority queue or event queue rather than traditional rounds.

This system must support:

- Duels
- Groups
- Raids
- Massive PvE battles
- Massive NPC encounters

without changing the rules.

---

# NPC and Monster Design Rules

NPCs must completely obey the same timing rules as players.

If a player can see statistics, the behavior should be predictable.

Example:

Goblin Scout

Reaction Time: 1.5

Quick Stab Recovery: 0.5

Acts frequently.

Deals moderate damage.

---

Ogre Brute

Reaction Time: 5.0

Crushing Blow Recovery: 2.5

Acts infrequently.

Deals heavy damage.

---

Ancient Dragon

Reaction Time: 4.0

Claw Swipe: 1.0 Recovery

Tail Sweep: 2.0 Recovery

Fire Breath: 4.0 Recovery

Different attacks produce different action rhythms.

Experienced players should learn and anticipate these rhythms.

---

# Haste and Slow

Buffs and debuffs modify Reaction Time.

Never implement separate cooldown reduction systems.

Examples:

Reaction Time: 3.0

20% Haste

New Reaction Time: 2.4

---

Reaction Time: 3.0

20% Slow

New Reaction Time: 3.6

All speed-related effects should flow through the Reaction Time system.

---

# Encumbrance

Inventory weight affects Reaction Time.

Light Load: +0.0

Medium Load: +0.5

Heavy Load: +1.0

Overloaded: +2.0

This creates meaningful tradeoffs between mobility and carrying capacity.

---

# Long-Term Extensibility

This combat timing model must support:

- Skills
- Spells
- Weapon abilities
- Item-granted abilities
- Dual wielding
- Shields
- Mounts
- Summoned creatures
- Pets
- Boss mechanics
- Area attacks
- Combo systems
- Status effects
- Haste
- Slow
- PvP
- PvE
- Raids
- World bosses

Future systems should extend or modify:

- Reaction Time
- Action Recovery
- Preparation Time
- Resource Cost

rather than introducing alternate timing systems.

There should be a single universal combat clock governing the entire game world.

Players and NPCs follow identical combat timing rules.
