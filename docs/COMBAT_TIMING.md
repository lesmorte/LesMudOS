# Combat timing: implementation design

The owner's brief is [specs/combat-timing.md](specs/combat-timing.md). It replaces the 2-second combat round with one event-driven clock that players and every kind of NPC obey identically. This document says how that is built in LesMudOS and in what order.

Status: pass 1 is designed here and not yet implemented.

## 1. Passes

The brief is large. It is built in passes so that each one leaves the game playable and tested.

| Pass | Contents | Status |
|---|---|---|
| 1 | The clock: Reaction Time, per-action Recovery, Preparation, the timing formula, equipment reaction modifiers, haste and slow, an action queue per combatant, creatures and the boss defined as action lists, consumables as actions, the area rebalanced and retested | This document |
| 2 | Stamina; item-granted skills (Triple Thrust and the other examples); a `skills` list; critical, dodge and block rolls | Later |
| 3 | Interrupts during Preparation (stun, knockdown, silence, fear, disarm) and status effects | Later |
| 4 | Mana and spells | Later |
| 5 | Item weight and encumbrance | Later |

Pass 1 must not paint later passes into a corner: every field the brief names for an action (resource cost, preparation, recovery, reaction modifier) exists on the action type from the start, even where pass 1 does not use it.

## 2. The model

### The clock

There is one clock: the driver's time, read in milliseconds. Nothing in combat depends on heartbeats any more. A combatant's next action is a driver call-out scheduled for the moment it becomes ready, so the driver's call-out queue is the event queue the brief asks for. Heartbeats remain for regeneration and idle NPC behaviour only.

Driver addition (generic, small): an efun returning the current time in milliseconds, since `Time()` is whole seconds.

### Reaction Time

Every living thing has a base Reaction Time, 1.0 to 6.0 in steps of 0.5. Lower is faster.

```
Final Reaction Time = (Base + equipment reaction modifiers + encumbrance) × speed factor
```

- Equipment reaction modifiers come from every equipped item (weapon, shield, armor).
- Encumbrance is always 0 until pass 5.
- The speed factor is the product of active haste and slow effects: 20% haste is × 0.8, 20% slow is × 1.2. All speed effects go through this; there is no cooldown-reduction mechanism anywhere.

### Actions

An action is data: name, Preparation, Recovery, reaction modifier, resource costs (unused in pass 1), and what it does when it resolves.

```
Total Delay = (Final Reaction Time + action's reaction modifier + action's Recovery) × 2 seconds
```

An action goes through three phases:

1. **Preparation.** The action begins at time T. If its Preparation is above 0, observers see a wind-up line ("The Goblin Overseer draws his axe far back...") and nothing else happens yet. Preparation lasts `Preparation × 2` seconds. Basic attacks have Preparation 0.
2. **Resolution.** At `T + Preparation × 2` seconds the effect happens: the hit roll and damage, the heal, the summon. If the target has died or left, the action is wasted.
3. **Recovery.** The combatant may not begin another action until `T + Total Delay`. That moment is its Ready Time.

Preparation is inside the Total Delay, not added to it. An action whose Preparation is longer than its Total Delay is a content error and is clamped.

### What each combatant tracks

Current action, action start time, resolution time, Ready Time, and one queued action. `stat` shows all of these.

### Choosing the next action

When a combatant is ready and has an enemy present, it begins an action at once:

- If it has a queued action (a player typed a command for one), that.
- Otherwise whatever its `ChooseAction` returns. For players this is the basic attack with the wielded weapon. For creatures it is a rule written with the creature (section 4).

When it has no enemy present it does nothing and schedules nothing.

### Entering a fight

- The one who starts the fight acts as soon as it is ready, which is usually at once.
- Everyone drawn into the fight by someone else (the target, assisting monsters) cannot begin their first action until their own `Final Reaction Time × 2` seconds have passed. A fast creature reacts quickly; a slow one is caught flat-footed.
- An aggressive creature that notices a player entering is the one starting the fight, but it too waits its `Final Reaction Time × 2` seconds before its first action. This replaces the old one-round pause and gives a player a chance to strike first or leave.

### Player commands and the queue

- `kill <target>` starts or redirects a fight, as now. Basic attacks then repeat automatically.
- Using a consumable (`eat`, `drink`, `quaff`) is an action with its own Recovery (0.5 for a potion). If the player is not ready, it is queued and happens at the next Ready Time in place of an attack; the player is told so. Out of a fight the same rule applies, and the player is nearly always ready.
- A newer command replaces the queued action. `stop` clears it.
- Movement and `flee` are not actions in pass 1 and work as they do now. This is deliberate for now and is revisited with the skills pass.

### Predictability

- `stat <creature>` (wizards) shows base and final Reaction Time and each action with its Preparation, Recovery and resulting delay in seconds.
- `score` shows the player's base and final Reaction Time, each modifier and where it comes from, and the delay of their basic attack.
- `consider <creature>` adds a line comparing speed ("It strikes much more often than you can.").
- Wind-up lines for prepared actions are always shown, so rhythms can be learned.

## 3. Numbers

These are starting values. The implementer tunes them with the simulation and records the final values here and in GAME_SYSTEMS.md.

### Players

Base Reaction Time 2.5. Unarmed attack: Recovery 0.5.

### Equipment

| Item | Reaction modifier | Basic attack Recovery | Delay for a player (seconds) |
|---|---|---|---|
| Unarmed | 0 | 0.5 | 6 |
| Iron Dagger | 0.0 | 0.5 | 6 |
| Rare Enchanted Dagger | 0.0 | 0.5 | 6 |
| Training Spear | +0.5 | 1.0 | 8 |
| Rusty Sword | +0.5 | 1.0 | 8 |
| Fine Iron Longsword | +1.0 | 1.0 | 9 |
| Woodcutter Axe | +1.0 | 1.5 | 10 |
| Goblin Overseer's Axe | +1.5 | 1.5 | 11 |
| Small Shield | +0.5 | | |
| Leather Commander's Vest | 0.0 | | |
| Guild Initiate's Helm | 0.0 | | |
| Magical Trinket | 0.0 | | |

Slow weapons must hit correspondingly harder, or nobody would use them: damage per second should rise gently from dagger to axe, with the fast weapons better against low-HP, hard-to-hit targets and when a potion needs to be squeezed in.

### Creatures

| Creature | Base Reaction Time | Actions (Preparation / Recovery) | Rhythm |
|---|---|---|---|
| Large Rat | 2.0 | Bite 0 / 0.5 | Every 5 s |
| Giant Rat | 2.0 | Bite 0 / 0.5 | Every 5 s |
| Wandering Wolf | 2.0 | Bite 0 / 1.0 | Every 6 s |
| Goblin Scavenger | 1.5 | Quick Stab 0 / 0.5 | Every 4 s; fast but fragile |
| Spore Bat | 1.5 | Swoop 0 / 0.5 | Every 4 s |
| Goblin Guard | 3.5 | Spear Jab 0 / 1.0 | Every 9 s; defensive |
| Goblin Warrior | 3.0 | Slash 0 / 1.0 | Every 8 s |
| Goblin Minion | 2.5 | Stab 0 / 0.5 | Every 6 s |
| Giant Cave Toad | 4.5 | Tongue Lash 0 / 1.0; Crushing Leap 1.0 / 2.0 every third action | 11 s, then 13 s with a 2 s warning |
| Goblin Elite Guard | 3.5 | Slash 0 / 1.0; Shield Bash 0.5 / 1.5 every fourth action | 9 s, then 10 s |
| Goblin Overseer | 4.0 | See below | |

Goblin Overseer, base Reaction Time 4.0. His abilities from the area brief become actions with a fixed, learnable pattern in place of random chance:

| Action | Preparation | Recovery | Delay | Effect |
|---|---|---|---|---|
| Battle Cry | 0 | 0.5 | 9 s | His first action in a fight. His damage is +2 for the next 30 seconds. |
| Axe Slash | 0 | 1.0 | 10 s | Normal attack. |
| Power Strike | 1.0 | 2.0 | 12 s | Double damage. Announced 2 seconds before it lands. Every third attack. |
| Summon Goblin Minion | 0.5 | 1.0 | 10 s | Once, as his next action after his HP first falls to half. |

### Rebalancing

Actions are now several seconds apart where rounds were 2 seconds, so with unchanged numbers every fight would take three to five times as long. Damage is scaled up (and creature HP adjusted where needed) so that:

- an ordinary creature dies in about 3 to 5 player actions, a tough one in 6 to 8, the boss in roughly 12 to 18;
- win rates per room and against the boss stay close to the current targets in GAME_SYSTEMS.md section 12;
- potions and regeneration keep roughly their present value relative to incoming damage;
- a full solo play-through still fits in about 15 minutes of real time.

### Haste and slow

`AddSpeedEffect(string id, double factor, int seconds)` on any living thing; effects with the same id replace each other; they expire by time. Pass 1 content that uses it: none in the area. A wizard command `speed <target> <percent> [<seconds>]` (level 5; positive is haste, negative is slow) exists so the mechanism can be tested and tried.

## 4. Writing a creature

A creature lists its actions in `Create()` and, if it has more than one, says how it chooses:

```csharp
AddAction("slash", "Axe Slash", preparation: 0, recovery: 1.0);
AddAction("power", "Power Strike", preparation: 1.0, recovery: 2.0, windUp: "draws his axe far back", damageMultiplier: 2);
```

with a `ChooseAction()` override for the pattern. A creature with one action needs no override. The exact API is the implementer's to settle; the requirement is that a simple creature stays a few lines of data, and that BUILDING.md shows both cases with compiled templates.

## 5. What does not change

To-hit and damage rolls, armor, levels, experience, death, corpses, the vault, loot, quests, shops, safe rooms, peaceful villagers, players unable to attack players, and enemies being forgotten 30 seconds after they are out of the room.

## 6. Testing

- A timing test measures real intervals between a combatant's actions and checks them against the formula within a tolerance, for: an unarmed player, the same player after wielding a slower weapon, after adding a shield, under haste and under slow, a fast creature and a slow creature, and the boss's pattern including the Power Strike warning arriving 2 seconds before the blow.
- It checks that a player and an NPC with the same numbers act at the same intervals.
- It checks the queue: a potion drunk mid-recovery happens at the next Ready Time and displaces one attack; `stop` clears it.
- It checks entering a fight: the attacked creature's first action comes after its own reaction delay.
- The balance simulation is rewritten around the event clock and its numbers kept identical to the code.
- The existing systems test and play-through are updated for the new rhythm and must pass; the play-through must still finish solo without wizard help.
