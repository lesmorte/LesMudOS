# Sneak (PROPOSAL: not approved, not built)

Owner's idea (22 November 2026), after the simulator showed the archer never gets an opening shot in the cellars because every creature there attacks on arrival: "I wonder if scout needs a sneak/hide skill that prevents instant aggro attacks and also the aimed strike before the creature can react." Every number here is a starting value for `tests/balance_sim.py`.

## What it is for
- Lets the Scout choose the fight: arrive unseen, then open with Aimed Shot (bow) or Precision Strike (blade) before the creature acts.
- Gives the weaker melee Scout line something the bow does not already give.
- Deliberately brings back, for one calling and at a risk, the walk-past that aggro-on-arrival closed for everyone.

## Rule
- `sneak` (Scout, from level 1) switches sneaking on; `sneak` again, or `stand`, switches it off. Refused in a fight. Shown in `score` and as a `{sneak}` prompt token. Not saved.
- While sneaking, each room entered costs **5 Endurance**. With too little Endurance the Scout stops sneaking, and is told.
- **On entering a room**, each aggressive creature there rolls to notice. Chance to notice = 50% - 5% for each point of the Scout's Grace above 5 + 5% for each level the creature is above the Scout (and - 5% for each level below), kept between 10% and 90%. A creature flagged `Alert` (bosses, elites, sentries: a builder's choice per creature file) adds 30%.
- **Noticed:** that creature engages at once, as it would have, and sneaking ends. Creatures that did not notice join only if they are the kind that help their own (existing behaviour).
- **Not noticed:** the creature does nothing. While the Scout stays, each unaware aggressive creature rolls again every 10 seconds at half the chance, so a room cannot be camped for long. Walking on unnoticed is allowed.
- **Anything loud ends sneaking and wakes the room:** attacking, any skill other than Tracking, picking things up from the floor or a corpse, opening a door or a container, `say` and `shout`. `look`, `rest`, `tell` and Tracking are quiet.
- **Opening from hiding:** an attack begun while unnoticed is the action that starts the fight, so the ranged opening (2 s) and Aimed Shot's opener multiplier apply as already built. In addition the target is **surprised**: its usual first-action delay is doubled. Precision Strike begun from hiding gets an opener multiplier of its own (x1.5 at rank I), so the blade Scout has an ambush too.
- Other players and NPCs see the Scout normally: this is not invisibility, and it gives nothing against players.
- Resting while unnoticed is allowed; the 10-second re-rolls still apply, so it is a gamble.

## Ranks
II at level 5: notice chance - 10%, 4 Endurance a room, Precision Strike opener x1.75. III at level 10: - 20%, 3 Endurance, x2.0. Taught by Wenna Farrow and the Scout's hallmaster, like the Scout's other skills.

## What it risks
- **Skipping the dungeon.** A lucky Scout can reach the Overseer's door without a fight. Experience comes from kills, so they arrive under-levelled; but anything that is only guarded, not locked, is reachable. The Overseer, the Elite Guards and other sentries should be `Alert`.
- **Finds and chests behind guards.** Taking them is loud, so the room wakes; the Scout then has to fight or flee with the prize. That seems right, but each guarded find should be looked at.
- **Solo power.** Opening every fight with a x2.5 shot and a doubled delay may be too much. The simulator already says a hypothetical archer who opens every fight wins 92% of standard runs (mean 81). The opener multipliers, not the notice chance, are the dial.
- **The other callings get nothing.** That is the point of a calling feature, but the Adventurer ("Jack of All Trades") could reasonably have a weaker version later.

## To decide
1. Build it at all, and for the Scout only?
2. May a sneaking Scout walk straight past (as written), or does leaving the room also need a roll?
3. Should bosses be merely harder to sneak past (`Alert`, +30%), or impossible?
4. One command (`sneak`), or a separate stationary `hide` as well?

## Build
One Sonnet agent after the Aimed Shot work merges: the skill and its ranks, the `Alert` flag and its placement on bosses, elites and sentries in the Hollow, the corridor and its dungeons, the simulator (melee and archer Scouts with sneaking, in both player models), tests including a probe that no outside object can hide a player or silence a creature.
