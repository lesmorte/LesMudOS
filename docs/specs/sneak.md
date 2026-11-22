# Sneak (approved by the owner 22 November 2026; built 22 November 2026, see "Built" below)

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

## Owner's decisions (22 November 2026; they override the Rule above where they differ)
1. **Build it, for the Scout only.** A strong archer is acceptable; mitigation may come later. The simulator reports the numbers but nothing is tuned down on that account.
2. **Leaving is not free.** Moving on from a room that holds aggressive creatures who have not noticed the Scout "should work like flee": it needs a roll. Each such creature rolls to notice again, at the entry chance, as the Scout leaves; if any notices, the fight starts and the Scout stays. So crossing a guarded room unseen takes two successes, in and out. (Walking out of a room with no aggressive creature needs nothing.)
3. **Doors as the builder's answer.** Opening a door is loud: it ends sneaking and every aggressive creature in the room engages at once. A door on the path therefore stops a Scout slipping through. Add one in the cellars before the Overseer: a closed door on the way from the Overseer Hall into the Overseer's chamber, so the Elite Guards in the Hall must be faced. Use the existing door mechanic; the door closes again on the room's reset. Builders' notes (docs/BUILDING.md) say when to place such a door.
4. **Elites and sentries are harder to pass** (`Alert`, +30% to notice). **Bosses cannot be sneaked past or ambushed**: a creature flagged `Boss` always notices.
5. **One command.** Sneaking is something you do on the way in: it must be on before entering the room. Once a creature in the room has spotted the Scout, `sneak` is refused there for as long as that creature lives and the Scout stays; there is no separate `hide`.

## Build
One Sonnet agent after the Aimed Shot work merges: the skill and its ranks, the `Alert` flag and its placement on bosses, elites and sentries in the Hollow, the corridor and its dungeons, the simulator (melee and archer Scouts with sneaking, in both player models), tests including a probe that no outside object can hide a player or silence a creature.

## Built (22 November 2026)
`sneak` (`mudlib/skills/sneak.cs`, ranks I-III: 5 / 4 / 3 Endurance a room, notice -0 / -10 / -20), the rules and dice in `/secure/sneak`, the state and commands in `/std/adventurer` (the state is not saved), the `Boss` and `Alert` flags in `/std/monster`, the leave roll in `Room.MoveThrough`, the surprise in `/std/living` (`QuerySurprising`: the target's first-action delay doubled) and Precision Strike's `ambush` effect (x1.5 / 1.75 / 2.0 / 2.25). Wenna Farrow teaches II, the Scout hallmaster III. Flagged `Boss`: Goblin Overseer, Bandit Leader, Forest Panther, Fallen Knight, Warren Broodmother, Barrow Guardian. Flagged `Alert`: Goblin Elite Guard, Goblin Guard, Goblin Guard (keyholder), Bandit Lookout, Heartwood Goblin. Where the spec was silent: a creature that has not noticed keeps rolling every 10 s at half chance until the Scout leaves the room, even after sneaking ended by a quiet `sneak off`; `sneak off` is quiet; a creature noticing at entry does not wake the others (they wake at the Scout's first loud deed or by their own re-roll); the notice chance has a final floor of 5% and ceiling of 100% after Alert and rank; the spotter refusal comes before the fight refusal; `{sneak}` prompt shows "(sneaking) ". The simulator results are in `python3 tests/balance_sim.py ranks sneak`; tests in `tests/sneak_test.py`.

## Doors (owner's decision, 22 November 2026: "add openable doors")
**Built (22 November 2026)**: `AddDoor` in `/std/room`, the register `/secure/doors`, the commands, the door between the Overseer Hall (east) and the chamber (west); see docs/BUILDING.md, "A room with a door", and `tests/sneak_test.py`. Existing locked exits (`LockExit`) were left as they are.

The engine has locked exits but no door a builder can place and a player can open. Add one:
- A room declares a door on an exit: `AddDoor(direction, name)` with options for starting closed (default), a key item or none, and whether it closes again on reset (default yes). The room on the other side declares the matching door, and the two halves share one state (open one side, both are open).
- `open <door|direction>` and `close <door|direction>`; a locked door needs its key (`unlock`, `lock`), and existing locked exits may be expressed as locked doors where that is a clean change, otherwise left alone.
- A closed door stops walking, fleeing, patrols and wandering creatures, Tracking (already specified: it does not read through closed doors), and anything thrown or shot later. `look` and the exits line show the door and whether it is open.
- Opening or closing is heard: the room on each side is told. Opening a door is "loud" for sneaking.
- Old saves and existing rooms are unaffected: no existing exit becomes a door unless a builder says so.
- The first one: a closed, unlocked door between the Overseer Hall and the Overseer's chamber.
