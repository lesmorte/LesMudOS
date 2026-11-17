# Class system: implementation design

The owner's brief is [specs/class-system.md](specs/class-system.md). This document says how it is built in LesMudOS. It follows combat timing pass 1 ([COMBAT_TIMING.md](COMBAT_TIMING.md)) and pulls in the parts of the timing brief that classes cannot work without: stamina, mana, skills and spells.

Status: design. Nothing here is implemented yet.

## 1. Decisions from the owner

1. All 15 advanced classes are selectable, each with a title and two signature abilities. Full skill trees come later.
2. The level cap is raised and the levelling formula is data-driven. New areas will follow.
3. The All Hall is reached by a new exit from the Adventurers Guild in Vincent's Hollow. The exit is locked. The key is given for completing the quest to kill the goblin boss ("Clear the Cellar").
4. Existing characters choose an archetype at their next login.

## 2. Stages

| Stage | Contents |
|---|---|
| A | Stamina and mana; the skill and spell mechanism on the combat clock; critical, dodge and block rolls; timed effects; equipment categories and access; the class framework and levelling formula; archetype choice at creation and for existing characters; the five archetypes with their starting features and equipment |
| B | The All Hall, its key and its five hallmasters; advancement at level 5; the 15 advanced classes with two signature abilities each, including allies (summons and companions) |
| C | Balance simulation per archetype, tests, a play-through per archetype, documentation |

Subclasses (level 10) and prestige classes exist in the framework from stage A but have no content and cannot be chosen.

## 3. Resources

Every living thing, players and NPCs alike, has Vitality (the existing HP), Stamina and Mana.

| | Max Stamina | Max Mana | Stamina regained per 10 s | Mana regained per 10 s |
|---|---|---|---|---|
| Warrior | 100 | 20 | 10 | 2 |
| Scout | 100 | 20 | 10 | 2 |
| Priest | 40 | 100 | 4 | 8 |
| Mage | 30 | 100 | 3 | 8 |
| Adventurer | 70 | 70 | 6 | 5 |
| Creatures | per creature, default 50 | default 0 | 5 | 2 |

Regeneration is the same in and out of combat; resting in a safe room doubles it. Resources do not grow with level in this pass (class identity comes from skills, not from stat growth).

`score` and the short `hp` line show all three. Costs are checked when an action begins; an action the character cannot pay for is refused and not queued.

## 4. Skills and spells

A skill is an action in the sense of COMBAT_TIMING.md: it has a name, a command word, Preparation, Recovery, a reaction modifier, a Stamina and Mana cost, optional requirements (a weapon category, a shield, being unarmed, a target) and an effect. A spell is a skill whose cost is Mana. Both go through the same queue and the same formula as a basic attack. There is no separate cooldown mechanism.

A trait is a passive feature: it changes a number or unlocks something, and has no command.

- `skills` lists the character's skills and traits with their costs, timing in seconds for this character, and one line each. Skills granted by an equipped item are listed separately and disappear when the item is removed (the mechanism exists now; the first item-granted skills come with the timing brief's examples in a later pass).
- A skill is used by typing its command word, with a target where it takes one (`bash goblin`, `heal`, `bolt rat`). With no target it uses the current enemy, or the character for helpful spells.
- Using a skill while not ready queues it, replacing whatever was queued, exactly as a potion does.

### Rolls

The attack roll is unchanged. Three rolls are added around it, for every attack by anyone:

| Roll | Who | Base chance | Effect |
|---|---|---|---|
| Dodge | Defender, before the hit roll | 0% | The attack misses |
| Block | Defender with a shield, after a hit | 15% | Damage halved |
| Critical | Attacker, on a hit | 5% | Damage × 1.5 |

A natural 20 is still an automatic hit, and is no longer special beyond that.

### Timed effects

One mechanism carries every temporary change: an effect has an id, a duration, and modifiers (to hit, damage, armor, dodge, block, critical, speed factor, damage absorbed). Effects with the same id replace each other. Haste and slow from the timing pass are effects of this kind. `score` lists active effects and their remaining time.

### Stagger (the first interrupt)

One interrupt is needed now, for Shield Bash: an action in its Preparation phase is cancelled, with half its resource cost lost and half its Recovery still paid. The full interrupt system of the timing brief (stun, silence, fear and so on) stays a later pass, and builds on this.

## 5. Equipment categories and access

Every weapon and armor piece gets a category.

| Kind | Categories |
|---|---|
| Weapons | dagger, sword, spear, axe, mace, staff, bow |
| Armor | light, medium, heavy |
| Other | shield, trinket (anyone) |

Bows are ordinary weapons in this pass: there is no range model.

| Archetype | Weapons | Armor | Shield |
|---|---|---|---|
| Warrior | all | light, medium, heavy | yes |
| Scout | dagger, sword, spear, bow | light | no |
| Priest | mace, staff | light, medium | yes |
| Mage | dagger, staff | light | no |
| Adventurer | dagger, sword, spear, axe, mace, staff | light | yes |

An item outside a character's access cannot be wielded or worn, with a message saying why. Advanced classes add to the list. Existing items: Rusty Sword and Fine Iron Longsword are swords; Training Spear is a spear; Iron Dagger and Enchanted Dagger are daggers; Woodcutter Axe and Goblin Overseer's Axe are axes; Leather Commander's Vest and Guild Initiate's Helm are light armor.

New items for starting kits and the smithy: Novice's Mace (mace), Apprentice's Staff (staff), Hunting Bow (bow). Torren sells all three.

## 6. The class framework

A class is data: id, name, tier (archetype, advanced, subclass, prestige), parent class, title, requirements, experience modifier, the skills and traits it grants, the equipment access it adds, resource values (archetypes only), guild, and whether it is enabled. A skill or trait is data in the same way.

Requirements:

- Adding a class, a skill or a trait means adding one mudlib file. No change to the player object, and none to existing classes.
- A character's save holds only ids: archetype, advanced class, subclass, prestige class, and nothing derived. Everything a class grants is worked out from the class data at login, so retuning a class retunes every character who has it, and a class added later affects nobody who has not chosen it.
- Requirements are data too: minimum level, a completed quest, a carried item, a parent class. The brief's other prestige requirements (achievements, reputation, discoveries) can be added as new requirement kinds without changing the framework.
- A disabled class cannot be chosen and is not shown.

Suggested layout: `/classes/<id>.cs` and `/skills/<id>.cs`, each inheriting a small base in `/std`.

### Levels and experience

One character level, shared by all classes. The cap becomes 100.

```
Experience to reach level n = 50 × n × (n − 1) × experience modifier
```

That gives 100, 300, 600 and 1000 for levels 2 to 5, close to the present 100, 250, 500, 900. The constants live in one place. The experience modifier is the largest modifier among the character's classes: 1.0 for standard classes, 1.25 for Spellblade as the brief gives it, higher for future prestige classes. A character who gains a modifier keeps their level and experience; only the experience needed for later levels changes.

Per level: +8 maximum HP, as now. The attack roll's level bonus is kept as it is; it will need a cap or a curve before areas above about level 10 exist, and that is recorded here as known debt.

## 7. Archetypes

Chosen at character creation, after the password, from a short in-character presentation of the five with one line each; `help <archetype>` at that prompt shows the detail. Existing characters with no archetype get the same choice at their next login before they enter the game; they keep their level, items and gold, and do not receive a starting kit.

"Avoid large stat differences" is taken literally: HP and Reaction Time differ a little, and the rest is skills, resources and equipment access.

| Archetype | Starting HP | Base Reaction Time | Starting kit | Gold |
|---|---|---|---|---|
| Warrior | 34 | 3.0 | Rusty Sword, Small Shield | 10 |
| Scout | 30 | 2.0 | Hunting Bow, Iron Dagger | 10 |
| Priest | 28 | 2.5 | Novice's Mace, Minor Healing Potion | 15 |
| Mage | 26 | 2.5 | Apprentice's Staff, Minor Healing Potion | 15 |
| Adventurer | 30 | 2.5 | Training Spear, two Trail Rations | 20 |

### Starting features

| Archetype | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|
| Warrior | Shield Bash (`bash`) | skill | 15 Stamina | 0 / 1.5 | Needs a shield. A normal hit roll; on a hit, half weapon damage, and the target is staggered: an action it is preparing is interrupted, otherwise its Ready Time is pushed back 2 seconds. |
| Warrior | Defensive Training | trait | | | +1 armor. |
| Warrior | Weapon Proficiency | trait | | | +1 to hit with any weapon. |
| Scout | Evasion | trait | | | 10% dodge. |
| Scout | Tracking (`track`) | skill | 5 Stamina | 0 / 0.5 | Lists the creatures in each adjacent room. |
| Scout | Precision Strike (`strike`) | skill | 20 Stamina | 0.5 / 1.0 | An attack at +5 to hit with a 30% critical chance. |
| Priest | Minor Heal (`heal`) | spell | 20 Mana | 0.5 / 1.0 | Restores 15 HP to the caster or a target in the room. |
| Priest | Blessing (`bless`) | spell | 25 Mana | 0.5 / 1.0 | +1 to hit and +1 armor for 60 seconds, on the caster or a target. |
| Priest | Divine Knowledge | trait | | | Healing spells restore 25% more. |
| Mage | Arcane Bolt (`bolt`) | spell | 15 Mana | 0.5 / 1.0 | 6-10 damage that ignores armor; needs no hit roll, can be dodged. |
| Mage | Mana Efficiency | trait | | | Spells cost 20% less Mana. |
| Mage | Magical Lore (`lore`) | skill | 5 Mana | 0 / 0.5 | Shows an item's exact numbers and any magical property. |
| Adventurer | Survival Training | trait | | | HP, Stamina and Mana regenerate 50% faster out of combat. |
| Adventurer | Appraise (`appraise`) | skill | 5 Stamina | 0 / 0.5 | Shows an item's exact numbers and what shops pay for it. |
| Adventurer | Improvised Combat | trait | | | Unarmed damage 2-5, and weapons carry 0.5 less reaction modifier (never below 0). |
| Adventurer | Traveler's Instinct | trait | | | The exits line of a room also shows where each exit leads, and marks locked ones. |

(The wizard-only `heal` command is renamed `restore` to free the word for the Priest's spell.)

## 8. The All Hall

- **Location.** A new exit `north` from the Adventurers Guild leads to the All Hall. It is locked with the All Hall Key.
- **The key.** Guildmaster Rowan gives it with the rewards for "Clear the Cellar". A character who has completed the quest and is not carrying the key (an existing character, or one who lost it) is given another when they `report` or `talk` to him.
- **The hall.** A great chamber built by the founders of the five callings, older than the village around it. One room in this pass, safe, with five hallmasters, one per archetype, and a wall of fifteen banners (readable) that describes every advanced class in a line.
- **Hallmasters.** Each speaks for one archetype: `talk` gives their welcome, and `ask <hallmaster> about <class>` describes each of their three advanced classes, what it gains, and what it costs in experience. They answer other archetypes' members politely and send them to the right hallmaster.
- **Choosing.** A level 5 character says `pledge <class>` to the hallmaster of their own archetype. The hallmaster states plainly that the choice is permanent and names what will be gained; `pledge <class>` a second time within a minute completes it, with a short ceremony the room sees. Below level 5 the hallmaster says to return when ready. A character who already has an advanced class is refused.
- **On choosing.** The character gains the class title (shown in `who` and `score`), the two signature abilities, the added equipment access, and guild membership (recorded as the guild id; guild content comes later). Everything from the archetype is kept.

## 9. Advanced classes

Standard experience modifier 1.0 unless stated. Each has two signature features.

### Warrior

| Class | Adds | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|---|
| Knight | | Shield Wall (`wall`) | skill | 20 Stamina | 0 / 1.5, reaction +1.5 | Needs a shield. Block chance +40% for 20 seconds. |
| Knight | | Guardian's Resolve | trait | | | +2 armor while wearing heavy armor; +1 otherwise. |
| Berserker | | Reckless Blow (`reckless`) | skill | 25 Stamina | 0.5 / 2.0 | An attack for double damage; the Berserker's armor is −2 for 10 seconds. |
| Berserker | | Battle Fury | trait | | | 15% haste while below half HP. |
| Monk | | Flurry (`flurry`) | skill | 25 Stamina | 0 / 1.5 | Unarmed only. Three unarmed attacks, each rolled separately. |
| Monk | | Iron Body | trait | | | Unarmed damage 3-6; +2 armor while wearing no body armor and no shield. |

### Scout

| Class | Adds | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|---|
| Rogue | | Backstab (`backstab`) | skill | 25 Stamina | 0.5 / 1.5 | Only against a target that is not yet fighting the Rogue. A normal hit roll at +2; triple damage if it hits. Starts the fight. |
| Rogue | | Opportunist | trait | | | Critical chance +10%. |
| Ranger | | Power Shot (`shot`) | skill | 20 Stamina | 1.0 / 2.5, reaction +1.0 | Needs a bow. A single attack at +2 to hit for double damage. |
| Ranger | | Hunter's Mark (`mark`) | skill | 10 Stamina | 0 / 0.5 | The target takes +2 damage from the Ranger's attacks for 60 seconds. |
| Bard | | Song of Courage (`sing`) | skill | 20 Stamina | 0.5 / 1.5 | The Bard and every other player in the room get +1 to hit and +1 damage for 60 seconds. |
| Bard | | Discordant Note (`note`) | skill | 20 Stamina | 0 / 1.0 | The target is slowed 30% for 20 seconds. |

### Priest

| Class | Adds | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|---|
| Cleric | | Greater Heal (`mend`) | spell | 35 Mana | 1.0 / 1.5 | Restores 35 HP to the caster or a target. |
| Cleric | | Sanctuary (`sanctuary`) | spell | 25 Mana | 0.5 / 1.0 | The caster or a target absorbs the next 15 damage, for up to 60 seconds. |
| Templar | Sword; heavy armor | Smite (`smite`) | spell | 20 Mana | 0 / 1.0 | A weapon attack that adds 6 damage ignoring armor if it hits. |
| Templar | | Holy Armor | trait | | | +1 armor. |
| Oracle | | Foresight (`foresight`) | spell | 25 Mana | 0.5 / 1.0 | Dodge +20% for 30 seconds, on the caster or a target. |
| Oracle | | Read Fate (`fate`) | spell | 10 Mana | 0 / 0.5 | Shows a creature's exact HP, its actions with their timing, and how many seconds until its next action. |

(The wizard-only `smite` command is renamed `slay` to free the word for the Templar.)

### Mage

| Class | Adds | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|---|
| Mage | | Fireball (`fireball`) | spell | 35 Mana | 1.0 / 1.5 | 9-14 damage ignoring armor to every enemy of the caster in the room. |
| Mage | | Arcane Mastery | trait | | | Spell damage +25%. |
| Druid | Spear | Entangle (`entangle`) | spell | 25 Mana | 0.5 / 1.0 | 3-5 damage, and the target is slowed 40% for 20 seconds. |
| Druid | | Bear Form (`bear`) | spell | 30 Mana | 1.0 / 1.0 | For 60 seconds: +15 maximum and current HP, unarmed damage 5-8 used in place of any weapon, +1 armor, and no spells can be cast. `bear` again ends it early. |
| Necromancer | | Raise Skeleton (`raise`) | spell | 40 Mana | 1.0 / 2.0 | Summons a skeleton that fights for the caster for 120 seconds. One at a time. |
| Necromancer | | Drain Life (`drain`) | spell | 20 Mana | 0.5 / 1.0 | 5-8 damage ignoring armor; the caster is healed by the damage done. |

### Adventurer

| Class | Adds | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|---|
| Spellblade (1.25× experience) | | Arcane Strike (`astrike`) | skill | 10 Stamina, 15 Mana | 0 / 1.0 | A weapon attack that adds 5 damage ignoring armor if it hits. |
| Spellblade | | Weapon Infusion (`infuse`) | spell | 25 Mana | 0.5 / 1.0 | The wielded weapon does +2 damage for 60 seconds. |
| Beastmaster | Bow | Call Companion (`call`) | skill | 30 Stamina | 1.0 / 1.0 | A wolf companion joins the Beastmaster and fights their enemies until it dies or is dismissed (`dismiss`). One at a time. It follows from room to room. |
| Beastmaster | | Pack Tactics | trait | | | +1 to hit while the companion is in the room. |
| Artificer | | Deploy Turret (`turret`) | skill | 35 Stamina | 1.0 / 2.0 | A turret stands in the room for 60 seconds and shoots the Artificer's current enemy every 6 seconds for 3-5 damage. One at a time. It does not move. |
| Artificer | | Gadget Toss (`toss`) | skill | 15 Stamina | 0 / 1.0 | 4-7 damage and the target is slowed 20% for 10 seconds. |

### Allies

Skeletons, companions and turrets are living things under the same combat rules as everything else. An ally has an owner; it treats the owner's enemies as its own, cannot be attacked by its owner or by other players, gives its owner the experience and kill credit for what it kills, leaves no lootable corpse, and disappears when its time runs out, when its owner dies or logs out, or (for a companion) when dismissed. Monsters may target allies. Allies are not saved.

| Ally | Level | HP | Damage | Reaction Time | Attack Recovery |
|---|---|---|---|---|---|
| Skeleton | owner's level − 1, at least 1 | 20 | 3-5 | 3.0 | 1.0 |
| Wolf companion | owner's level − 1, at least 1 | 24 | 3-5 | 2.0 | 1.0 |
| Turret | owner's level | 15 | 3-5 | 2.0 | 1.0 |

## 10. What must keep working

Everything already built: the combat clock, corpses and the vault, quests, shops, colour, wizard tools, passwords, the area and its tests. Creatures gain Stamina and Mana but none in Vincent's Hollow uses a skill in this pass, so the area's balance changes only through what players can now do.

## 11. Balance and testing

- The simulation gains archetypes and advanced classes. Target: each of the five archetypes can complete Vincent's Hollow solo by level 5, with win rates against the boss within the band already used (roughly 50-75% for a prepared level 4-5 character), and no archetype more than about 15 points better or worse than the average. Caster archetypes are expected to lean on their spells and run out of Mana in long fights; that is intended, and the numbers are tuned so they are not helpless when they do.
- Advanced classes cannot be fought with in the Hollow beyond repeating the dungeon at level 5, so their abilities are tested for correctness, not for balance against content that does not exist yet.
- Tests: creation with each archetype; an existing character being asked at login; every starting feature; equipment access refusals; resources and regeneration; dodge, block and critical rolls with controlled test creatures; timed effects expiring; stagger; the key from Rowan including re-issue; the locked door; each hallmaster; the two-step pledge and its refusals; every one of the 30 signature features; allies' lifetimes and credit; the experience formula and the Spellblade modifier; a class file added in the test's temporary mudlib being selectable without touching other files; persistence of all of it.
- The play-through is run once per archetype. Each must complete both quests solo without wizard help, then obtain the key, enter the All Hall and pledge an advanced class.
