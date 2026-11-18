# NOTES-world (F2 world features)

## Done (all committed)
- room.cs: HiddenExit, protected virtual ExitFound, DangerBand, AddNode, Search/Examined, QueryBandLow/High, stat lines, wizard "(hidden)" marker
- area.cs: BandLow/BandHigh
- npc.cs: Spoken hook (talk/ask pause for patrols)
- living.cs: ActAsAction (generic timed action, like UseAsAction)
- adventurer.cs: [Saved] foundExits, RecordFoundExit/QueryFoundExit, search command + Special override, look-at calls Examined
- player.cs: help line for search
- patrol.cs, node.cs, Elder Lane hidden exit (west, detail "gap", search 20%)
- docs: BUILDING.md (examples), GAME_SYSTEMS.md section 3a
- tests/world_test.py: 131 checks, all pass (port 4823 default)
- all other scripts pass (smoke, area, class, ansi, timing, chronicle, load_check)

## Next
- final report only

## Decisions
- ExitFound is protected virtual (only the room runs it); QueryHiddenExits/QueryExitFoundBy are the public queries
- found record on the adventurer ("room|dir"), written only by the room itself, /wiz records capped at 50
- patrol dormant = no walking call-out, only a poll every PollSeconds; first step is a pace after waking; no fast-forward
- patrol under /wiz walks only in its own home; others never enter /wiz
- node regrow lazy (timestamps), per charge; node inherits /std/thing
- search = Recovery 0, ReactionMod -1.0 (3 s at Reaction 2.5); same line and cost whether or not anything is hidden
- wizards see hidden exits marked but must find them to walk them
