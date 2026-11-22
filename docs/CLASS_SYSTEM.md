# Class system: implementation design

The owner's brief is [specs/class-system.md](specs/class-system.md). This document says how it is built in LesMudOS. It follows combat timing pass 1 ([COMBAT_TIMING.md](COMBAT_TIMING.md)) and pulls in the parts of the timing brief that classes cannot work without: endurance, focus, skills and spells.

Status: stage A (everything up to and including the five archetypes, the travel pass and the Vale Road) is implemented; sections 8 (the Hall of Legends) and 9 (advanced classes) are the design for stages B and C. Section 12 describes how stage A is built and how to extend it.

## 1. Decisions from the owner

1. All 15 advanced classes are selectable, each with a title and two signature abilities. Full skill trees come later.
2. The level cap is raised and the levelling formula is data-driven. New areas will follow.
3. The hall where advanced classes are chosen is not in Vincent's Hollow. It is the Hall of Legends (the class brief calls it the "All Hall") in Oakenfell, the first major city of Greenvale, which is not yet built. Completing "Clear the Cellar" grants a travel pass that lets a character leave the main area of Vincent's Hollow and travel toward that city. (This replaces an earlier decision to put the hall behind a locked door in the village's Adventurers Guild.) See [WORLD.md](WORLD.md).
4. Existing characters choose an archetype at their next login.

## 2. Stages

| Stage | Contents |
|---|---|
| A | Endurance and focus; the skill and spell mechanism on the combat clock; critical, dodge and block rolls; timed effects; equipment categories and access; the class framework and levelling formula, with level-scaled attack and defense; archetype choice at creation and for existing characters; the five archetypes with their starting features and equipment; the travel pass and the road out of the village; region and area names; balance and a play-through per archetype |
| B | Oakenfell, the first major city of Greenvale (needs an area brief), with the Hall of Legends and its five hallmasters; advancement at level 5; the 15 advanced classes with two signature abilities each, including allies (summons and companions) |
| C | Balance simulation and tests for the advanced classes; a play-through that ends with travelling to the city and pledging a class |

Stage B cannot start until the city has its area brief. Stage A does not depend on it.

Subclasses (level 10) and prestige classes exist in the framework from stage A but have no content and cannot be chosen.

## 3. Resources

Every living thing, players and NPCs alike, has Vitality (the existing Vitality), Endurance and Focus.

| | Max Endurance | Max Focus | Endurance regained per 10 s | Focus regained per 10 s |
|---|---|---|---|---|
| Warrior | 100 | 20 | 10 | 2 |
| Scout | 100 | 20 | 8 | 2 |
| Priest | 40 | 100 | 4 | 4 |
| Mage | 30 | 100 | 3 | 8 |
| Adventurer | 70 | 70 | 6 | 5 |
| Creatures | per creature, default 50 | default 0 | 5 | 2 |

Regeneration is the same in and out of combat (the Adventurer's Survival Training makes it 50% faster out of combat); resting in a safe room doubles it. Resources do not grow with level in this pass (class identity comes from skills, not from stat growth).

`score` and the short `vitality` line show all three. Costs are checked when an action begins; an action the character cannot pay for is refused and not queued.

## 4. Skills and spells

A skill is an action in the sense of COMBAT_TIMING.md: it has a name, a command word, Preparation, Recovery, a reaction modifier, a Endurance and Focus cost, optional requirements (a weapon category, a shield, being unarmed, a target) and an effect. A spell is a skill whose cost is Focus. Both go through the same queue and the same formula as a basic attack. There is no separate cooldown mechanism.

A trait is a passive feature: it changes a number or unlocks something, and has no command.

- `skills` lists the character's skills and traits with their costs, timing in seconds for this character, and one line each. Skills granted by an equipped item are listed separately and disappear when the item is removed (the mechanism exists now: an item overrides `QueryGrantedSkills`; no item in the Hollow grants one yet).
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

A class is data: id, name, tier (archetype, advanced, subclass, prestige), parent class, title, requirements, experience modifier, attack rate, the skills and traits it grants, the equipment access it adds, resource values (archetypes only), guild, and whether it is enabled. A skill or trait is data in the same way.

Requirements:

- Adding a class, a skill or a trait means adding one mudlib file. No change to the player object, and none to existing classes.
- A character's save holds only ids: archetype, advanced class, subclass, prestige class, and nothing derived. Everything a class grants is worked out from the class data at login, so retuning a class retunes every character who has it, and a class added later affects nobody who has not chosen it.
- Requirements are data too: minimum level, a completed quest, a carried item, a parent class. The brief's other prestige requirements (achievements, reputation, discoveries) can be added as new requirement kinds without changing the framework.
- A disabled class cannot be chosen and is not shown.

Suggested layout: `/classes/<id>.cs` and `/skills/<id>.cs`, each inheriting a small base in `/std`.

### Levels and experience

One character level, shared by all classes. The cap becomes 100.

```
Levels 2 to 5:   50 × n × (n − 1) × experience modifier    (100, 300, 600, 1000)
Levels 6 to 10:  5,400, 12,000, 21,700, 34,100, 49,300 × experience modifier
Levels above 10: 49,300 × (n / 10)³ × experience modifier
```

The first four levels are unchanged from the original design (it was 100, 250, 500, 900 before the five callings). Levels 6 to 10 are a table measured against the corridor's pace (about 15 hours from level 5 to level 10 for a character playing well; [CORRIDOR_BALANCE.md](CORRIDOR_BALANCE.md) section 4). **The curve beyond level 10 is provisional**: a power law through level 10 that continues the last step smoothly (level 11 needs 65,618, level 20 needs 394,400, level 100 needs 49,300,000). It is an order of magnitude and nothing more, and it is to be re-fitted when there is content for levels above 10. The result is rounded to a whole number. The table and the power law live in one place (`Adventurer.ExperienceFor`). The experience modifier is the largest modifier among the character's classes: 1.0 for standard classes, 1.25 for Spellblade as the brief gives it, higher for future prestige classes. A character who gains a modifier keeps their level and experience; only the experience needed for later levels changes.

Per level: +8 maximum Vitality, as now.

### Attack and defense scaling

The attack roll has to work from level 1 to level 100, and how fast a character's attack grows depends on class. The roll becomes a contest of two level-scaled values:

```
Hit if  d20 + Attack ≥ 10 + Armor + Defense

Attack  = round(attack rate × level) + weapon, skill and effect bonuses
Defense = level − 1
```

- **Attack rate** is a field of the class, so it is data. It is the attack gained per level.
- **Defense** grows one point per level for everyone, players and creatures alike, so that two combatants of the same level and a full attack rate meet on the same terms at level 1 and at level 100. The defense rate is a single constant and can become a class field later if classes should differ in it too.
- A natural 20 always hits and a natural 1 always misses, as now.

Rates have to stay close together. Each point of attack is 5% on the die, and the gap between two rates is multiplied by level: a rate of 0.7 against 1.0 is unnoticeable at level 5 and a 30-point gap at level 100, where that class could no longer hit anything of its own level. So the difference between the best and worst class is held to a few points at the cap:

| Archetype | Attack rate | Attack at level 5 | Attack at level 50 | Attack at level 100 | Against an equal-level foe at level 100 |
|---|---|---|---|---|---|
| Warrior | 1.00 | 5 | 50 | 100 | as at level 1 |
| Scout | 1.00 | 5 | 50 | 100 | as at level 1 |
| Adventurer | 0.98 | 5 | 49 | 98 | 10% less likely to hit |
| Priest | 0.97 | 5 | 49 | 97 | 15% less likely to hit |
| Mage | 0.96 | 5 | 48 | 96 | 20% less likely to hit |

An advanced class may carry its own rate, which then replaces the archetype's: Templar 1.00, Monk 1.00, Spellblade 0.99, Bard 0.99. Classes that do not name one keep their archetype's. Creatures have a rate of 1.00 unless the creature says otherwise.

This rate governs weapon attacks and skills that make an attack roll. Spells that need no hit roll (Arcane Bolt, Fireball, Drain Life) are not affected by it, which is how the casting classes stay effective while being poorer with a weapon.

Consequence for the existing area: today the defender's level counts for nothing, so adding Defense makes every attack against a target above level 1 less likely to hit, in both directions (by 15 points between two level 4 combatants). The simulation (`tests/balance_sim.py`) shows the Hollow needs no creature change: with the class numbers of section 7 every archetype completes it by level 4-5. Creature armor was left as it was.

## 7. Archetypes

Chosen at character creation, after the password, from a short in-character presentation of the five with one line each; `help <archetype>` at that prompt shows the detail. Existing characters with no archetype get the same choice at their next login before they enter the game; they keep their level, items and gold, and do not receive a starting kit.

"Avoid large stat differences" is taken literally: Vitality and Reaction Time differ a little, and the rest is skills, resources and equipment access.

| Archetype | Starting Vitality | Base Reaction Time | Attack rate | Starting kit | Gold |
|---|---|---|---|---|---|
| Warrior | 40 | 3.0 | 1.00 | Rusty Sword, Small Shield | 10 |
| Scout | 26 | 2.0 | 1.00 | Hunting Bow, Iron Dagger | 10 |
| Priest | 26 | 2.5 | 0.97 | Novice's Mace, Minor Healing Potion | 15 |
| Mage | 24 | 2.5 | 0.96 | Apprentice's Staff, Minor Healing Potion | 15 |
| Adventurer | 30 | 2.5 | 0.98 | Training Spear, two Trail Rations | 20 |

### Starting features

| Archetype | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|
| Warrior | Shield Bash (`bash`) | skill | 15 Endurance | 0 / 1.5 | Needs a shield. A normal hit roll; on a hit, three quarters of weapon damage, and the target is staggered: an action it is preparing is interrupted, otherwise its Ready Time is pushed back 2 seconds. |
| Warrior | Defensive Training | trait | | | +2 armor. |
| Warrior | Weapon Proficiency | trait | | | +2 to hit with any weapon. |
| Scout | Evasion | trait | | | 5% dodge (8% before docs/ATTRIBUTES.md section 8). |
| Scout | Tracking (`track`) | skill | 5 Endurance | 0 / 0.5 | Lists the creatures in each room the scout could walk to now (never through a closed door or an unfound hidden way). Rank II, the last, reads two rooms away and senses creatures behind a closed way. |
| Scout | Precision Strike (`strike`) | skill | 25 Endurance | 0.5 / 1.0 | An attack at +2 to hit with a 15% critical chance. |
| Priest | Minor Heal (`heal`) | spell | 25 Focus | 0.5 / 1.0 | Restores 7 Vitality (9 with Divine Knowledge) to the caster or a target in the room. PROVISIONAL (was 8; the owner's choice of heal is pending, see section 13). |
| Priest | Blessing (`bless`) | spell | 25 Focus | 0.5 / 1.0 | +1 to hit and +1 armor for 60 seconds, on the caster or a target. |
| Priest | Divine Knowledge | trait | | | Healing spells restore 25% more. |
| Mage | Arcane Bolt (`bolt`) | spell | 20 Focus (16 with Focus Efficiency) | 0.5 / 1.0 | 6-9 damage that ignores armor; needs no hit roll, can be dodged. |
| Mage | Focus Efficiency | trait | | | Spells cost 20% less Focus. |
| Mage | Magical Lore (`lore`) | skill | 5 Focus | 0 / 0.5 | Shows an item's exact numbers and any magical property. |
| Adventurer | Survival Training | trait | | | Vitality, Endurance and Focus regenerate 50% faster out of combat. |
| Adventurer | Double Strike (`double`) | skill | 20 Endurance | 0.5 / 1.0 | Two blows with the wielded weapon (or fists), each with its own dodge, hit, critical and block rolls, each at 60% of the damage. Triple Strike at rank III, Quadruple Strike at rank IV. |
| Adventurer | Appraise (`appraise`) | skill | 5 Endurance | 0 / 0.5 | Shows an item's exact numbers and what shops pay for it. |
| Adventurer | Jack of All Trades | trait | | | Crafting and gathering proficiency is gained 25% faster in every craft. |
| Adventurer | Improvised Combat | trait | | | Unarmed damage 2-5, and weapons carry 0.5 less reaction modifier (never below 0). |
| Adventurer | Traveler's Instinct | trait | | | The exits line of a room also shows where each exit leads, and marks locked ones. |

(The wizard-only `heal` command is renamed `restore` to free the word for the Priest's spell.)

## 8. The travel pass and the Hall of Legends

### The travel pass (stage A)

- **What it is.** A pass of safe conduct issued by the Adventurers Guild of Vincent's Hollow: an item, carried like any other.
- **How it is earned.** Guildmaster Rowan gives it with the rewards for "Clear the Cellar". A character who has completed the quest and is not carrying a pass (an existing character, or one who lost it with a corpse) is given another when they `report` or `talk` to him.
- **What it does.** The road north out of Vincent's Hollow, on the North Road, is closed to anyone without a pass, with a refusal in keeping with the village (the wilds are no place for the untested). With the pass, the way is open.
- **What lies beyond, for now.** One new area, the Vale Road, in Greenvale: a short stretch of road north of the village, a single room in this stage, from which the road continues toward Oakenfell and is not yet open. It is not part of the village and is not safe.

### The Hall of Legends (stage B)

- **Location.** In Oakenfell, the first major city of Greenvale, reached by the Vale Road. The city needs an area brief. In the design bible the Hall of Legends is also a faction, a confederation of guilds, and it keeps the Grand Chronicle.
- **The hall.** A great chamber built by the founders of the five callings. One room in its first form, safe, with five hallmasters, one per archetype, and a wall of fifteen banners (readable) that describes every advanced class in a line.
- **Hallmasters.** Each speaks for one archetype: `talk` gives their welcome, and `ask <hallmaster> about <class>` describes each of their three advanced classes, what it gains, and what it costs in experience. They answer other archetypes' members politely and send them to the right hallmaster.
- **Choosing.** A level 5 character says `pledge <class>` to the hallmaster of their own archetype. The hallmaster states plainly that the choice is permanent and names what will be gained; `pledge <class>` a second time within a minute completes it, with a short ceremony the room sees. Below level 5 the hallmaster says to return when ready. A character who already has an advanced class is refused.
- **On choosing.** The character gains the class title (shown in `who` and `score`), the two signature abilities, the added equipment access, and guild membership (recorded as the guild id; guild content comes later). Everything from the archetype is kept.

## 9. Advanced classes

Standard experience modifier 1.0 unless stated. Each has two signature features.

### Warrior

| Class | Adds | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|---|
| Knight | | Shield Wall (`wall`) | skill | 20 Endurance | 0 / 1.5, reaction +1.5 | Needs a shield. Block chance +40% for 20 seconds. |
| Knight | | Guardian's Resolve | trait | | | +2 armor while wearing heavy armor; +1 otherwise. |
| Berserker | | Reckless Blow (`reckless`) | skill | 25 Endurance | 0.5 / 2.0 | An attack for double damage; the Berserker's armor is −2 for 10 seconds. |
| Berserker | | Battle Fury | trait | | | 15% haste while below half Vitality. |
| Monk | | Flurry (`flurry`) | skill | 25 Endurance | 0 / 1.5 | Unarmed only. Three unarmed attacks, each rolled separately. |
| Monk | | Iron Body | trait | | | Unarmed damage 3-6; +2 armor while wearing no body armor and no shield. |

### Scout

| Class | Adds | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|---|
| Rogue | | Backstab (`backstab`) | skill | 25 Endurance | 0.5 / 1.5 | Only against a target that is not yet fighting the Rogue. A normal hit roll at +2; triple damage if it hits. Starts the fight. |
| Rogue | | Opportunist | trait | | | Critical chance +10%. |
| Ranger | | Power Shot (`shot`) | skill | 20 Endurance | 1.0 / 2.5, reaction +1.0 | Needs a bow. A single attack at +2 to hit for double damage. |
| Ranger | | Hunter's Mark (`mark`) | skill | 10 Endurance | 0 / 0.5 | The target takes +2 damage from the Ranger's attacks for 60 seconds. |
| Bard | | Song of Courage (`sing`) | skill | 20 Endurance | 0.5 / 1.5 | The Bard and every other player in the room get +1 to hit and +1 damage for 60 seconds. |
| Bard | | Discordant Note (`note`) | skill | 20 Endurance | 0 / 1.0 | The target is slowed 30% for 20 seconds. |

### Priest

| Class | Adds | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|---|
| Cleric | | Greater Heal (`mend`) | spell | 35 Focus | 1.0 / 1.5 | Restores 35 Vitality to the caster or a target. |
| Cleric | | Sanctuary (`sanctuary`) | spell | 25 Focus | 0.5 / 1.0 | The caster or a target absorbs the next 15 damage, for up to 60 seconds. |
| Templar | Sword; heavy armor | Smite (`smite`) | spell | 20 Focus | 0 / 1.0 | A weapon attack that adds 6 damage ignoring armor if it hits. |
| Templar | | Holy Armor | trait | | | +1 armor. |
| Oracle | | Foresight (`foresight`) | spell | 25 Focus | 0.5 / 1.0 | Dodge +20% for 30 seconds, on the caster or a target. |
| Oracle | | Read Fate (`fate`) | spell | 10 Focus | 0 / 0.5 | Shows a creature's exact Vitality, its actions with their timing, and how many seconds until its next action. |

(The wizard-only `smite` command is renamed `slay` to free the word for the Templar.)

### Mage

| Class | Adds | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|---|
| Mage | | Fireball (`fireball`) | spell | 35 Focus | 1.0 / 1.5 | 9-14 damage ignoring armor to every enemy of the caster in the room. |
| Mage | | Arcane Mastery | trait | | | Spell damage +25%. |
| Druid | Spear | Entangle (`entangle`) | spell | 25 Focus | 0.5 / 1.0 | 3-5 damage, and the target is slowed 40% for 20 seconds. |
| Druid | | Bear Form (`bear`) | spell | 30 Focus | 1.0 / 1.0 | For 60 seconds: +15 maximum and current Vitality, unarmed damage 5-8 used in place of any weapon, +1 armor, and no spells can be cast. `bear` again ends it early. |
| Necromancer | | Raise Skeleton (`raise`) | spell | 40 Focus | 1.0 / 2.0 | Summons a skeleton that fights for the caster for 120 seconds. One at a time. |
| Necromancer | | Drain Life (`drain`) | spell | 20 Focus | 0.5 / 1.0 | 5-8 damage ignoring armor; the caster is healed by the damage done. |

### Adventurer

| Class | Adds | Feature | Kind | Cost | Prep / Recovery | Effect |
|---|---|---|---|---|---|---|
| Spellblade (1.25× experience) | | Arcane Strike (`astrike`) | skill | 10 Endurance, 15 Focus | 0 / 1.0 | A weapon attack that adds 5 damage ignoring armor if it hits. |
| Spellblade | | Weapon Infusion (`infuse`) | spell | 25 Focus | 0.5 / 1.0 | The wielded weapon does +2 damage for 60 seconds. |
| Beastmaster | Bow | Call Companion (`call`) | skill | 30 Endurance | 1.0 / 1.0 | A wolf companion joins the Beastmaster and fights their enemies until it dies or is dismissed (`dismiss`). One at a time. It follows from room to room. |
| Beastmaster | | Pack Tactics | trait | | | +1 to hit while the companion is in the room. |
| Artificer | | Deploy Turret (`turret`) | skill | 35 Endurance | 1.0 / 2.0 | A turret stands in the room for 60 seconds and shoots the Artificer's current enemy every 6 seconds for 3-5 damage. One at a time. It does not move. |
| Artificer | | Gadget Toss (`toss`) | skill | 15 Endurance | 0 / 1.0 | 4-7 damage and the target is slowed 20% for 10 seconds. |

### Allies

Skeletons, companions and turrets are living things under the same combat rules as everything else. An ally has an owner; it treats the owner's enemies as its own, cannot be attacked by its owner or by other players, gives its owner the experience and kill credit for what it kills (experience for a kill is otherwise shared by damage among the players who hurt the creature: see GAME_SYSTEMS.md, Shared experience), leaves no lootable corpse, and disappears when its time runs out, when its owner dies or logs out, or (for a companion) when dismissed. Monsters may target allies. Allies are not saved.

| Ally | Level | Vitality | Damage | Reaction Time | Attack Recovery |
|---|---|---|---|---|---|
| Skeleton | owner's level − 1, at least 1 | 20 | 3-5 | 3.0 | 1.0 |
| Wolf companion | owner's level − 1, at least 1 | 24 | 3-5 | 2.0 | 1.0 |
| Turret | owner's level | 15 | 3-5 | 2.0 | 1.0 |

## 10. What must keep working

Everything already built: the combat clock, corpses and lost property, quests, shops, colour, wizard tools, passwords, the area and its tests. Creatures gain Endurance and Focus but none in Vincent's Hollow uses a skill in this pass, so the area's balance changes only through what players can now do.

## 11. Balance and testing

- The simulation gains archetypes and advanced classes. Target: each of the five archetypes can complete Vincent's Hollow solo by level 5, with win rates against the boss within the band already used (roughly 50-75% for a prepared level 4-5 character), and no archetype more than about 15 points better or worse than the average. Caster archetypes are expected to lean on their spells and run out of Focus in long fights; that is intended, and the numbers are tuned so they are not helpless when they do.
- Advanced classes cannot be fought with in the Hollow beyond repeating the dungeon at level 5, so their abilities are tested for correctness, not for balance against content that does not exist yet.
- Tests: creation with each archetype; an existing character being asked at login; every starting feature; equipment access refusals; resources and regeneration; dodge, block and critical rolls with controlled test creatures; timed effects expiring; stagger; the travel pass from Rowan including re-issue; the closed road with and without the pass; (stage B and C:) each hallmaster; the two-step pledge and its refusals; every one of the 30 signature features; allies' lifetimes and credit; the experience formula and the Spellblade modifier; a class file added in the test's temporary mudlib being selectable without touching other files; persistence of all of it.
- The play-through is run once per archetype. Each must complete both quests solo without wizard help, receive the travel pass and walk out onto the Vale Road. From stage C it continues to the city and pledges an advanced class.

## 12. How stage A is built (for builders and for stage B)

Files:

| Path | What |
|---|---|
| `/std/living.cs` | Endurance and Focus (lazy regeneration), timed effects, dodge/block/critical, `Stagger`, `UseSkill`, the level-scaled `Attack` |
| `/std/adventurer.cs` | The player's classes, skills, equipment access, `score`, `skills`, `restore`, `slay`, `setlevel` |
| `/std/skill.cs`, `/std/trait.cs`, `/std/pclass.cs`, `/std/area.cs` | The small bases a skill, trait, class and area file inherit |
| `/secure/classes.cs` | The registry. It lists the `/classes` and `/skills` directories on demand (the master grants this object, and only it, `get-dir` on those two paths), so a new file is found without a restart and without editing any list |
| `/classes/*.cs`, `/skills/*.cs` | The five archetypes and the 18 starting skills and traits |
| `/secure/ranks.cs`, `/std/trainer.cs` | Skill ranks: the rank daemon (requirements, `Train`, kill counters) and the trainer's `ranks` and `train` (section 13) |
| `area.cs` in a domain directory | The area name and region name of every room in that directory |

To add a skill or trait: one file in `/skills` that inherits `/std/skill` (a command word, costs, timing, `Resolve(user, target)`) or `/std/trait` (`Mods["key"] = value`). The modifier keys understood are `tohit`, `tohit_armed`, `damage`, `armor`, `dodge`, `block`, `crit`, `regen_out`, `spell_cost_pct`, `healing_pct`, `spell_damage_pct`, `unarmed_min`, `unarmed_max`, `weapon_reaction_reduction`, `ignore_weapon`, `nospell` and `exit_detail`. To add a class: one file in `/classes` that inherits `/std/pclass` and sets its fields (tier, parent, requirements through `Require("level", "5")`, `ExpModifier`, `AttackRate`, the skills, traits and equipment access it adds, `Enabled`). Nothing else is edited. A class's id is its file name.

The save holds `archetype`, `advancedClass`, `subclass` and `prestigeClass` as ids and nothing derived; every number comes from the class files at login.

The entry point stage B uses to give an advanced class: `player.Call<string>("ApplyClass", id)` on the player object. It must be called by a game object (an NPC or room), never by a player's own commands; it returns `null` on success and otherwise a sentence saying why not (unknown or disabled class, parent class missing, requirement unmet, tier already filled). The Hall of Legends does the two-step pledge, then calls it.

Experience: `Adventurer.ExperienceFor(level)` is the single place for the formula and the modifier.

Balance as built (`python3 tests/balance_sim.py`): see the tables it prints. The class numbers of section 7 are the tuned values.

## 13. Skill and spell ranks (built November 2026)

The owner's brief is [specs/skill-ranks.md](specs/skill-ranks.md). Skills do not improve with use; a better version is bought from a trainer as a rank.

- **The rule.** Every skill or spell with a command word has ranks I, II, III ... A character starts at rank I of what the calling grants. Rank r needs level `Skill.RankLevelStep` x (r - 1), RankLevelStep = 5: II at 5, III at 10, IV at 15, V at 20. Traits have no ranks; Magical Lore and Appraise are rank I only (`MaxRank = 1`); Tracking stops at II (`MaxRank = 2`). A higher rank replaces the lower; the command word does not change. Advanced-class skills will start at rank I when the class is taken.
- **The numbers.** Each skill file holds its rank table: the rank I fields, `Effect("key", value)` for numbers of its own, and `RankRow(rank, "key=value;...", title)` for later ranks. `Skill.RankValue(key, rank)` is the one helper every number is read through: a row's number, else the rank before plus the default step (30% of the rank I value for an effect, 15% for a cost; nothing for a key in `FixedKeys`, such as a duration or a count). The combat engine (`Living.UseSkill`) reads the cost, to-hit, critical chance, damage fraction and number of blows at the user's rank (`Query...For(user)`); a skill's own `Resolve` reads its numbers with `RankInt(key, RankOf(user))`. Preparation and Recovery never change with a rank.
- **What is shipped.**

| Skill | I | II | III | IV | V |
|---|---|---|---|---|---|
| Arcane Bolt | 6-9, 20 Focus | 8-12, 23 | 10-14, 26 | 12-17, 29 | 14-19, 32 |
| Minor Heal (PROVISIONAL) | 7, 25 Focus | 9, 32 | 11, 40 | 13, 44 | 15, 48 |
| Blessing | +1/+1, 25 Focus | +2/+2, 29 | +3/+3, 33 | +4/+4, 36 | +4/+4, 40 |
| Shield Bash | 75%, 15 Endurance | 98%, 17 | 120%, 20 | 143%, 22 | 165%, 24 |
| Precision Strike | +2, 15%, 25 Endurance | +3, 20%, 29 | +4, 25%, 33 | +5, 30%, 36 | +6, 35%, 40 |
| Tracking | adjacent rooms, 5 Endurance | two rooms away, 6 | (none) | | |
| Double Strike | 2 x 60%, 20 Endurance | 2 x 75%, 23 | Triple Strike 3 x 60%, 26 | Quadruple Strike 4 x 55%, 29 | 4 x 73%, 32 |

  Blessing V's +4.3 rounds to +4; Double Strike V is the default step from IV. Durations (Blessing's 60 s) and Shield Bash's 2-second stagger do not change.
- **The saved data** (`/std/adventurer.cs`): `skillRanks` (skill id to rank, above I only), `rankUnlocks` (offers whose requirements were met, for good), `killsByKind` and `killsWithSkill` (the counters unlocks read, kept from the first kill after this build). Only `/secure/ranks` may change them (`RankSetSkill`, `RankRecordUnlock`, `RankCountKill`, `RankCountKillWith`, `RankPay`, `RankSave` check `PreviousObject()`). An old save loads at rank I with no unlocks and no counts.
- **Unlocks.** An offer of a rank (a trainer's, not the skill's) may list requirements, all of which must hold; the gold is always paid. Kinds, one line of data each, all in `/secure/ranks.cs` `Evaluate`: `level N` (always present, from the cap rule), `quest ID`, `kills KIND N`, `kills_with SKILL N`, `renown N`, `standing FACTION N` (accepted, never met until factions exist; the trainer says so), `item PATH` (shown, not taken) and `item PATH take` (handed over at the purchase). An unknown kind is never met. Once all are met at a trainer (at `ranks` or `train`) the unlock is recorded for good.
- **Creature kind** (for `kills`): the beast-table kind the creature declares (its `Skin`, else its `Blood`, the names of `mudlib/crafting/beasts.cs`: every goblin has `Blood = "goblin"`), else its `ChronicleKind` (by default its file name). `Monster.QueryCreatureKind()`.
- **Counting.** With shared experience a kill counts for every eligible participant (`Living.EligibleDamage`); a `kills_with` count goes only to the one who struck the killing blow, with the skill whose blow or spell was landing (`Living.QueryStrikingSkill`). The existing kill credit does not exclude wizards (they earn experience), so their kills count too; the Chronicle's "wizards make no history" is unchanged. A death that a wizard's object forces (calling `Die` or `FinishDamage` from `/wiz`) counts for nothing.
- **Trainers.** `/std/trainer`: `TeachCalling(calling, from, to)` offers every ranked skill of a calling at the default prices (`RankDaemon.RankPrices`: II 40, III 150, IV 400, V 900 gold; a rank with no price is not offered), `AddRankOffer(skill, rank, price, requirements...)` one offer on its own terms, `AddRankLine(requirement, text)` the trainer's words for an unmet one. Rank II: Halbert Quill (Mage; Arcane Bolt II needs ten goblins slain), Sister Merewyn (Priest), Wenna Farrow (Scout), Brann Odell (Warrior and Adventurer), in the new village rooms. Ranks III to V: the five hallmasters of the Hall of Callings (Gorrim, Veyla, Anselm, Ilsabet, Jory).
- **Jack of All Trades** reaches the crafting daemon's `LearnSpeed` through `QueryClassTraitMod("learn_speed")`: class traits only, so neither a timed effect nor an item can speed learning.
- **Balance.** `python3 tests/balance_sim.py ranks` prints the tables the spec asks for; the results and the open decisions are in [specs/skill-ranks.md](specs/skill-ranks.md), "As built".
