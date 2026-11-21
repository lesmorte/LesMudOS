# Combat timing: implementation design

The owner's brief is [specs/combat-timing.md](specs/combat-timing.md). It replaces the 2-second combat round with one event-driven clock that players and every kind of NPC obey identically. This document says how that is built in LesMudOS and in what order.

Status: pass 1 is implemented (driver efun `TimeMs`, `/std/living`, the Hollow content, `tests/timing_test.py`, the event-clock `tests/balance_sim.py`). Passes 2 to 5 are not. Section 7 lists where the implementation differs from the first design.

## 1. Passes

The brief is large. It is built in passes so that each one leaves the game playable and tested.

| Pass | Contents | Status |
|---|---|---|
| 1 | The clock: Reaction Time, per-action Recovery, Preparation, the timing formula, equipment reaction modifiers, haste and slow, an action queue per combatant, creatures and the boss defined as action lists, consumables as actions, the area rebalanced and retested | Implemented |
| 2 | Stamina; item-granted skills (Triple Thrust and the other examples); a `skills` list; critical, dodge and block rolls | Stamina, the `skills` list, the item-granted-skill mechanism and the three rolls are implemented by the class system (stage A, [CLASS_SYSTEM.md](CLASS_SYSTEM.md)); the Triple Thrust style examples are not |
| 3 | Interrupts during Preparation (stun, knockdown, silence, fear, disarm) and status effects | Later |
| 4 | Mana and spells | Mana, spells and the first interrupt (stagger, which cancels an action in its Preparation) are implemented by the class system; the rest of pass 3 stays later |
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
- An aggressive creature that notices a player entering starts the fight at once (a call-out of zero seconds, so the room is described first), but it too waits its `Final Reaction Time × 2` seconds before its first action. The player is in combat from that moment: walking on is refused and `flee` is the way out, so a gauntlet of aggressive rooms is a risk, not a free walk. The player can still strike first within the creature's Reaction Time.

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

The reaction and Recovery values below are the design's and were kept unchanged. HP and damage were tuned with `tests/balance_sim.py`; the final values are in section 3.1 and in GAME_SYSTEMS.md.

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
| Woodcutter Axe | +1.0 | 1.5 | 11 |
| Goblin Overseer's Axe | +1.5 | 1.5 | 12 |
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

### Final numbers (3.1)

Weapons (damage in the old rounds was 2-4 to 6-10; delays are for a base 2.5 player):

| Weapon | Damage (was) | To hit | Delay s | Average DPS vs armor 2 |
|---|---|---|---|---|
| Fists | 2-5 (1-3) | 0 | 6 | 0.30 |
| Iron Dagger | 6-9 (2-4) | +1 | 6 | 0.70 |
| Rare Enchanted Dagger | 7-11 (3-6) | +2 | 6 | 0.90 |
| Training Spear | 8-12 (2-5) | +1 | 8 | 0.72 |
| Rusty Sword | 9-14 (3-5) | 0 | 8 | 0.77 |
| Fine Iron Longsword | 12-18 (5-9) | 0 | 9 | 0.90 |
| Woodcutter Axe | 14-21 (4-7) | 0 | 11 | 0.95 |
| Goblin Overseer's Axe | 17-25 (6-10) | 0 | 12 | 1.05 |

(The delay column in the equipment table assumes a player with no shield; the DPS column assumes a level 3 player with a small shield, which adds 1 s.) Damage per second rises gently from dagger to axe, with the fast daggers better against the evasive and low-HP creatures and when a potion has to be squeezed in. The Overseer's axe is the best weapon in the area.

Creatures: base Reaction Time and actions are exactly the table above. HP (was) and damage (was): Large Rat 12 (6); Giant Rat 15 (8); Wolf 26 (16); Scavenger 13 (10), damage 1-3 (2-3); Spore Bat 9 (6), damage 1-2 (1-3); Guard 26 (16); Toad 66 (30), damage 2-4 (2-5); Warrior 28 (16); Minion 20 (12); Elite Guard 40 (22); Overseer 114 (54), damage 4-7 (4-6). The Crushing Leap and Shield Bash do double damage (the design left the multiplier open). Spawn counts per room are unchanged.

Why: a player action is now 6 to 12 seconds apart instead of 2, so damage per action had to roughly triple for a fight to take 5 to 9 actions; creature HP rose about 1.5 to 2.2 times (more for creatures that only appear alone), and creature damage per action stayed almost the same because creatures also act once every 4 to 12 seconds. The simulation (`python3 tests/balance_sim.py`) gives room win rates of 88 to 99 percent at the right level, and the Overseer 50 to 75 percent for a level 4 to 5 player with a longsword and two potions.

### Haste and slow

`AddSpeedEffect(string id, double factor, int seconds)` on any living thing; effects with the same id replace each other; they expire by time. Pass 1 content that uses it: none in the area. A wizard command `speed <target> <percent> [<seconds>]` (level 5; positive is haste, negative is slow) exists so the mechanism can be tested and tried.

## 4. Writing a creature

A creature lists its actions in `Create()` and, if it has more than one, says how it chooses:

```csharp
AddCombatAction("slash", "Axe Slash", preparation: 0, recovery: 1.0);
AddCombatAction("power", "Power Strike", preparation: 1.0, recovery: 2.0, windUp: "draws his axe far back", damageMultiplier: 2);
```

with a `ChooseAction(target)` override for the pattern (it uses `ActionsTaken()`, the number of actions this combatant has begun in this fight). A creature with one action needs no override. The method is called `AddCombatAction` because `AddAction` is the command binding. BUILDING.md shows both cases with compiled templates.

## 5. What does not change

Armor, levels, experience, death, corpses, lost property, loot, quests, shops, safe rooms, peaceful villagers, players unable to attack players, and enemies being forgotten 30 seconds after they are out of the room.

## 5a. What the class system added to the clock

Skills and spells are ordinary actions on this clock (Preparation, Recovery, reaction modifier; a Stamina or Mana cost paid when the action begins). Level now matters on both sides of a hit: `d20 + Attack >= 10 + Armor + Defense`, with `Attack = round(attack rate x level) + bonuses` and `Defense = level - 1`. The defender's dodge is rolled first, a hit may be critical (x1.5), and a block halves the damage. Timed effects (haste and slow among them) carry the speed factor. Stagger interrupts an action in its Preparation (half its cost is lost, half its Recovery is still paid) or pushes a ready creature's next action back. See [CLASS_SYSTEM.md](CLASS_SYSTEM.md) and `tests/balance_sim.py`.

## 6. Testing

- A timing test measures real intervals between a combatant's actions and checks them against the formula within a tolerance, for: an unarmed player, the same player after wielding a slower weapon, after adding a shield, under haste and under slow, a fast creature and a slow creature, and the boss's pattern including the Power Strike warning arriving 2 seconds before the blow.
- It checks that a player and an NPC with the same numbers act at the same intervals.
- It checks the queue: a potion drunk mid-recovery happens at the next Ready Time and displaces one attack; `stop` clears it.
- It checks entering a fight: the attacked creature's first action comes after its own reaction delay.
- The balance simulation is rewritten around the event clock and its numbers kept identical to the code.
- The existing systems test and play-through are updated for the new rhythm and must pass; the play-through must still finish solo without wizard help.

## 7. Where the implementation differs from this design

- **Action API.** `AddCombatAction`, not `AddAction` (see section 4). The helper types `CombatAction` and `SpeedEffect` are plain public classes in `/std/living`.
- **Ambush.** An aggressive monster does not start the fight and then hold back: `Noticed` schedules the ambush call-out for zero seconds (after the arrival has finished), and `BeginFight(target, true)` happens when it fires, if the player is still there and the creature is alive: the creature then waits its Reaction Time x 2 seconds (`DelayFirstAction`) before its first action.
- **Who waits.** The initiator of `StartFight` acts at once (if ready); the target, assisting monsters and the summoned minion (`JoinFight`) wait their own Reaction Time x 2 seconds. `AddEnemy` is the path for "drawn in", `AddEnemyCore` for the initiator.
- **Multipliers.** Crushing Leap and Shield Bash do x2 damage (the design left them open). The Power Strike warning is the wind-up line printed when the action begins; with Preparation 1.0 it lands exactly 2 seconds later.
- **Pattern details.** The Toad uses its leap as every third action (`ActionsTaken() % 3 == 2`), the Elite Guard its bash as every fourth, the Overseer: Battle Cry first, Summon once at half HP, then slash, slash, power. The cry bonus is a 30 second window from when the cry resolves.
- **Speed effects** are not retroactive: they change the delay of the next action begun, not one already in progress.
- **Forgetting enemies.** An enemy who is not present is forgotten 30 seconds after the combatant first notices it is absent (a timer is scheduled when the combatant is idle with absent enemies).
- **Queue.** An item used while not ready prints "You are not ready yet. You will ... as soon as you can. (stop cancels.)" and runs at the next Ready Time, whether or not the fight is still on; if the item is gone by then the player is told. Death (`ResetCombat`) drops the queue.
- **Heartbeats.** `Living.HeartBeat` only calls the regeneration tick and turns itself off when it is not needed; combat never depends on it.
- **score / stat.** `score` reports "Reaction Time X (base ..., item +mod)" and the delay of the basic attack; `stat` appends the timing block (current action, ready time, queue, every action with delay). `consider` adds a speed verdict.
- **Numbers.** Section 3.1: HP and damage values changed as described, unarmed damage is 2-5, and the axe is 14-21 so that its damage per second does not fall behind the longsword.
- **Driver.** One generic efun, `TimeMs()` (DRIVER_API.md). Fractional-second call-outs needed no fix: the game loop already waits in milliseconds, and the timing test measures them.
