# The `rest` command

Owner's decision (21 November 2026), after `docs/BALANCE_HUMAN.md` showed that arriving hurt at the next fight is most of what makes the cellars hard for a real player: out of combat, Vitality returns at about a point every ten seconds, so nobody waits for it.

## Rule
- `rest` (also `sit`): the character sits down to recover. Refused while fighting, and refused in the same breath an aggressive creature engages. Everyone in the room sees "{name} sits down to rest." and the room lists "{name} is resting here."
- While resting, Vitality, Endurance and Focus all return at **RestFactor** times the ordinary rate (starting value 5; the simulator settles it). It multiplies with anything else that changes regeneration (the Adventurer's Survival Training, the Priest's faster Focus).
- Resting ends when the character: types `stand`; moves; attacks or uses a skill or spell; picks up, drops, wields, wears, crafts, gathers, buys or sells; or is attacked. Looking, checking `score` or `inventory`, and talking (`say`, `tell`, channels) do not end it.
- Being attacked while resting costs something: the character scrambles up, and their first action is delayed by a further **RestSurprise** seconds (starting value 2) on top of the usual delay for the one attacked. That is the risk of resting where creatures return.
- When all three resources are full the character is told once ("You are fully rested.") and stays seated.
- Resting does not stop the idle timer: a resting player who types nothing is idle after the usual time and is disconnected after the usual time. Experience is not involved.
- Creatures and NPCs do not rest. A wizard may.
- `score` and the prompt may show the state: a `{rest}` prompt token that expands to "(resting) " or nothing.
- No cost, no skill, no cooldown. Every calling has it from level 1.

## Numbers
One place, named: `RestFactor` and `RestSurprise`. The human player model in `tests/balance_sim.py` gains resting as the game implements it (time spent is counted, since the pace targets are in hours). The simulator report must give, for RestFactor 3, 5 and 8: the Overseer kill rate from the cellar entrance for each calling under the low, middle and high human models (level 4 and 5, starting gear and best shop gear, 2 and 4 potions), and the minutes a run takes. The owner's target is about 70% for a level 5 character in starting gear with 4 potions and points placed, middle human model, callings within a few points of each other.

## Tests
`rest` and `stand`; each thing that ends resting and each that does not; the rate, measured over ticks, for each resource; refused in a fight; the surprise delay when attacked (the creature's blow lands before the player's first action by the stated margin); the room listing; fully rested message once; idle still applies; a wizard-written object cannot put a player into or out of resting.

## Order
The simulator work can start at once (it touches only `tests/balance_sim.py` and docs). The game change waits for the Vitality / Endurance / Focus rename to merge, because both edit the regeneration code.

## Simulator result (21 November 2026)

Details in `docs/BALANCE_HUMAN.md` section 10. Numbers to use: **RestFactor 5, RestSurprise 2.** RestSurprise is not decisive: 0 to 10 seconds moves the kill rate by 4 points at most, and it is only ever tested if a rest outlasts a room's reset.

- Rest alone (RestFactor 5): the middle human beats the Overseer 73% of the time at level 5 with starting gear and 4 potions (13% today), with the callings 64 to 84 apart (Mage and Scout lowest). RestFactor 3 gives 60%, 4 gives 69%, 8 gives 76%. The result depends mainly on how long people will sit (20 s: 40%, 60 s: 74%, 120 s: 76%).
- To bring the callings together the smallest tested change is Mage Vitality 7 a level (6), Scout 8 (7) and Priest Minor Heal 7 (8): 81 / 71 / 78 / 73 / 74, mean 75, spread 10. Not applied.
- The perfect player's quoted level 4, 2-potion table stays at 72 to 73 on average.
- Safe places: every cellar room is safe to sit in for about 5 minutes after its fight (a creature cannot return before its corpse is gone, 420 to 600 s); Smuggler Hall has no spawn at all. If corpses are taken away at once (skinning, bleeding, looting), creatures return within 0 to 300 s and about one rest in four is interrupted; the game would need a rule (no refill while someone sits in the room, or corpses stay while someone sits) to keep resting safe.
- The pace of levels 1 to 5 shortens by about 25% to 35% (87 to 94 minutes without, 62 to 69 with RestFactor 5, farming limited by respawns).

## Owner's decisions (21 November 2026)
1. **RestFactor 5**, RestSurprise 2.
2. With it, two class numbers change: **Mage Vitality 4 base + 7 a level** (was 6 a level) and **Scout Vitality 3 base + 8 a level** (was 7). The Priest's Minor Heal is NOT changed here: it is settled in the skill ranks work, where the simulator sets it together with the larger heal already approved there.
3. **Rooms refill as they do now, resting or not.** Resting is a risk and reward action: a player who strips every corpse at once and then sits down may be caught by the room's creatures returning. No protection for a resting player is to be added.
