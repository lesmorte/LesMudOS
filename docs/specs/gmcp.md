# GMCP and Mudlet mapping

Owner's request (22 November 2026): "implement ... mudlet mapping". Mudlet cannot map by itself: it needs the game to say which room the player is in. The standard way is GMCP (telnet option 201), which the driver does not speak today.

## 1. Driver
- On connect the server offers GMCP (`IAC WILL 201`). A client that answers `DO` gets GMCP; everyone else sees exactly what they see today. Clients that refuse, or plain telnet, are unaffected, and no GMCP bytes ever reach a client that did not ask.
- Incoming: `Core.Hello`, `Core.Supports.Set / Add / Remove` and `Core.Ping` are understood (ping is answered); anything else is ignored. Incoming messages are size-limited and rate-limited, and never interpreted as commands.
- Outgoing: the mudlib asks the driver to send a package name and a JSON value to one player's connection. Only trusted game code (`/std`, `/secure`) may send, and only to the player the data is about. The driver does the JSON encoding and escaping; game code passes values, never raw JSON text.
- Subnegotiation framing is handled correctly inside the existing telnet state machine (IAC doubling, a message split across packets).

## 2. What is sent
- **`Room.Info`**, on entering a room, on `look`, and when the room's exits change for that player (a hidden way found, a door opened or closed): `{ "num": 1234, "name": "Village Square", "area": "Vincent's Hollow", "environment": "outdoors|indoors|underground|road|water", "exits": { "north": 1235, "lane": 1301 }, "doors": { "east": "closed" } }`.
  - `num` is a stable whole number for the room, the same after every restart: assigned once by a small daemon (`/secure/roomids`) that keeps a saved table of room path to number and never reuses one.
  - `exits` lists only ways this character can see: a hidden exit is absent until this character has found it; a locked or closed door's exit is present and marked in `doors`. Named exits (`lane`, `shed`) are sent under their own names.
  - Rooms in a wizard's home are sent with area `Workshop of <name>` and numbers from a separate range that is not saved.
- **`Char.Vitals`**, when any of them changes (at most a few times a second): `{ "vitality": 30, "maxvitality": 40, "endurance": 55, "maxendurance": 70, "focus": 70, "maxfocus": 70, "resting": false, "sneaking": false, "fighting": true }`, plus `hp`/`maxhp`/`mp`/`maxmp` duplicates so stock Mudlet gauges work.
- **`Char.Status`**, on login and on change: `{ "name", "calling", "level", "experience", "next_level", "gold", "points_to_place" }`.
- **`Comm.Channel.Text`** for tells, shouts and channel lines the player receives: `{ "channel", "talker", "text" }`, with the text already cleaned as the player sees it.
- Nothing is sent that the player could not learn from the text of the game: no creature numbers, no hidden things, no other player's vitals.

## 3. Mudlet package
`clients/mudlet/Hollowmere.mpackage` (source under `clients/mudlet/src`): a Lua script that builds the map from `Room.Info` (creates the room and area, links exits both ways as they are learned, lays rooms out by compass direction, draws named exits as special exits, marks doors), follows the player, and shows three gauges from `Char.Vitals`. Installation and use in `docs/MUDLET.md`, linked from the website's How to Play page. This cannot be run in the build environment (no Mudlet there): it is written against Mudlet's documented API and marked untested until the owner tries it.

## 4. Tests
`tests/gmcp_test.py`: a client that negotiates GMCP and one that does not, side by side; `Room.Info` on every move with stable numbers across a server restart; a hidden exit absent, then present after `search` finds it, for that character only; a door's state; `Char.Vitals` after damage, rest and a potion; `Char.Status` after a level; a channel line; a wizard's object unable to send GMCP to a player; malformed and oversized incoming subnegotiations doing nothing; the non-GMCP client's byte stream identical to before.
