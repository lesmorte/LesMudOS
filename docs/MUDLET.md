# Mudlet map and gauges for Legends of Hollowmere

> **UNTESTED in a real Mudlet.** There is no Mudlet in the build environment, so this package has never been loaded into one. It is written against Mudlet's documented Lua API, its Lua logic is tested with a stand-in for that API (see "How it was tested"), and the game side (GMCP) is fully tested over TCP. Expect to find things to fix the first time you try it, and please report them. Exactly what is and is not verified is listed at the end.

Mudlet cannot draw a map by itself: it needs the game to say which room the player is in. The game now says so with GMCP (telnet option 201); this package turns those messages into a Mudlet map, follows you around it, and shows your Vitality, Endurance and Focus as bars.

## Install

1. In Mudlet, connect to the game. GMCP is on by default in Mudlet (Settings, General, "Enable GMCP"; leave it ticked).
2. Toolbox, Package Manager, Install, and choose `clients/mudlet/Hollowmere.mpackage` from this repository.
3. Open the map: Toggle Map (the map button in the toolbar). The first room you enter is drawn in the middle; the package moves the view with you.
4. Log in and walk. The map is saved with the Mudlet profile like any other map.

To rebuild the package from the Lua source (`clients/mudlet/src`): `python3 clients/mudlet/build.py`. `python3 clients/mudlet/build.py --check` says whether the committed package matches the source.

## What the map does

- Every room the game sends (`Room.Info`) becomes a Mudlet room whose id is the game's own room number, which is the same after every restart of the game, so the saved map stays correct for ever. The room gets its name, its area (`The Village of Vincent's Hollow`, `The Forgotten Cellars`, and for wizards' home rooms `Workshop of <Name>`) and an environment colour (outdoors, indoors, underground, road, water).
- **Layout.** A new room is placed one step from the room you came from in the direction you went (north is up the screen; up and down change level). If that spot is taken, the nearest free spot is used, so two rooms are never drawn on top of each other. A room you reach with no exit leading to it (a respawn, a wizard's `goto`) is placed to the right of everything else in its area. The first room of an area is at 0, 0, 0.
- **Named exits.** An exit that is not a compass direction (`lane`, `shed`, `healer`, `in`, `out`, `enter` ...) becomes a Mudlet special exit with that word as its command, and the room it leads to is placed on a free spot beside the room you left, so the mapper's speedwalk (`gotoRoom`) types the right word.
- **Links both ways as they are learned.** The game lists every exit you can see in the room you stand in. An exit to a room you have not seen is a stub (compass exits) or a note kept in memory (named exits); it becomes a real link the moment that room is first entered. The way back is linked when you stand in the room and the game lists it.
- **Hidden exits** appear on the map only after your character has found them, because the game does not send them before. Another character finding one does not show it to you.
- **Doors.** An exit with a door is drawn with Mudlet's door marker: open, closed or locked, kept up to date when anyone opens or closes it in your room.
- **Gauges.** Three bars along the bottom of the main window, from `Char.Vitals`: Vitality, Endurance, Focus. The Vitality bar also says `resting`, `sneaking` or `fighting`.

The package asks the game for `Room`, `Char` and `Comm.Channel` with `Core.Supports.Add`. The game also sends `Char.Status` (name, calling, level, experience, gold) and `Comm.Channel.Text` (tells, shouts and channel lines); this package does not use them, but Mudlet puts them in the `gmcp` table (`gmcp.Char.Status`, `gmcp.Comm.Channel.Text`) for your own scripts.

## What the game sends

See `docs/DRIVER_API.md` (the protocol) and `docs/GAME_SYSTEMS.md`, section 16 (the packages). In short, `Room.Info` is `{num, name, area, environment, exits = {direction = number}, doors = {direction = "open"|"closed"|"locked"}}`.

## Known limits

- **Untested in Mudlet** (see the top). The Mudlet functions used are listed at the head of `src/map.lua` and `src/gauges.lua`. `getRoomsByPosition`, `killAnonymousEventHandler` and the `sysProtocolEnabled` event are newer than the others: use a current Mudlet 4.x; the oldest version that works has not been determined.
- The layout is greedy and never rearranges: a loop that closes badly, or a spot that was taken early, can leave a long exit line, and rooms are not moved after they are placed. Edit them in the mapper as usual.
- A named exit's door is drawn only once the room it leads to is on the map (Mudlet cannot mark a door on a special exit that does not exist yet); the next time you stand in the room it is drawn.
- Exits that disappear (a bridge that is raised) are not removed from the map. The map only ever adds and updates.
- The gauges are placed when the first `Char.Vitals` arrives, from the window size at that moment; resize the window and reload the script (or reinstall the package) to move them. They use the main window's bottom border.
- Rooms in wizards' homes have numbers that are not kept when the game restarts, so a map of a workshop goes stale after a restart. Game rooms are not affected.
- If the game's save directory is ever thrown away (a new world), the room numbers start again and your saved map is wrong: delete the map and start again.
- Only the exits the character can see are sent, so a one-way exit appears only on its own side, and a room the game has no file for yet ("not open yet") is drawn as a stub that never connects.

## How it was tested

- `lua clients/mudlet/test/map_test.lua` (Lua 5.1, LuaJIT and 5.4 all pass) runs the layout and map logic against `test/stub.lua`, an in-memory stand-in for the Mudlet functions used, with small invented worlds (compass and named exits, doors, jumps, other areas, malformed messages) and with `test/village_fixture.lua`, 23 real `Room.Info` messages captured from the running game as a character walked 13 rooms of Vincent's Hollow. The stand-in follows Mudlet's documentation; it is not Mudlet.
- `luacheck clients/mudlet/src clients/mudlet/test` (settings in `clients/mudlet/.luacheckrc`) is clean; every file also passes `luac5.1 -p`.
- `tests/gmcp_test.py` tests the game's side over TCP: the offer, the framing, every package, the room numbers across a driver restart, hidden exits and doors per character, and malformed incoming messages.
- The package builds reproducibly (`build.py --check`); the generated XML parses and each script in it is byte-for-byte the source and passes `luac5.1 -p`.

**Not verified:** that Mudlet accepts the package XML; that every Mudlet function behaves as documented for the arguments used; that the gauges are placed and styled sensibly; that `centerview` and the map widget follow as intended; that `Core.Supports.Add` is accepted in every Mudlet version; anything about performance with a large map. These are what the first real test will show.
