# NOTES-world (F2 world features)

## Done
- room.cs: HiddenExit, ExitFound hook, DangerBand, AddNode, Search/Examined, Stat lines, wizard marker
- area.cs: BandLow/BandHigh
- npc.cs: Spoken hook (talk/ask pause for patrols)
- living.cs: ActAsAction (generic timed action)
- adventurer.cs: saved foundExits, RecordFoundExit, search command, Special override, look-at calls Examined
- patrol.cs, node.cs, Elder Lane hidden exit

## Next
- tests/world_test.py, docs (BUILDING.md, GAME_SYSTEMS.md), help line, run all checks

## Decisions
- ExitFound is protected virtual (only the room runs it)
- found record lives on the adventurer ([Saved] foundExits, "room|dir"), written only by the room itself
- patrol dormant = no walking call-out, only a poll every PollSeconds; no fast-forward
- node regrow is lazy (timestamps), per charge
