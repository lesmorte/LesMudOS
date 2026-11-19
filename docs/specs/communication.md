# Communication commands

Owner's request (November 2026). Five commands, in his words, then the rules that make them safe to run on a public server.

## The commands
1. `say <message>` or `'<message>`: "tells every object in the room `{name} says {message}`". The speaker sees `You say: ...`. Objects in the room that listen (NPC topic and speech hooks) still get it, as today.
2. `tell <player> <message>`: the target sees `{name} tells you: {message}`; the sender sees `You tell {target}: ...`. "Works anywhere the tell isn't specifically blocked." `reply <message>` answers the last player who told you something.
3. `shout <message>`: every player in the world sees `{name} shouts: {message}`.
4. `whisper <message> to <name>`: the target, who must be in the same room, sees `{name} whispers to you: {message}`; everyone else in the room sees `{name} whispers something to {target}.` and not the words. `whisper <name> <message>` is accepted as well when the first word names someone present.
5. `shout <channel> <message>`: the same as shout, but only members of the channel receive it, shown as `[wizard] {name}: {message}`. If the first word after `shout` is a channel the speaker belongs to, it is a channel message; otherwise it is an ordinary shout. Shortcut: the channel's name as a command (`wizard hello`).

## Channels
- A channel is one data file in `/channels` (id, title, who belongs, who may speak, whether members may switch it off), read by a daemon `/secure/channels`. Adding a channel is adding a file.
- First two: `wizard` (every wizard, level 1 and above) and `sysop` (level 10 wizards only). Membership of both is by rule, decided from the character's wizard level at the moment of every send and every delivery, never from a saved list the player could edit, and never by asking to join.
- `channels` lists the channels you belong to and whether each is on. `channel <name> off|on` mutes or unmutes one for yourself (saved). A channel file may allow players to join and leave (`channel <name> join|leave`) for later public channels; neither of the first two does.
- A player who is not a member is told there is no such channel; the reply must not confirm that a restricted channel exists.

## Blocks ("where the tell isn't specifically blocked")
- `ignore <player>` / `unignore <player>` / `ignore` (list), saved: tells and whispers from an ignored player are not delivered and that player's shouts are not shown. The sender of a tell is told only that the target "is not listening to you".
- A room may set `NoTell` (nothing in or out by tell) and `NoShout` (shouts neither leave nor are heard); wizards are not bound by either. No existing room sets them.
- `tell` to someone who is not logged in, or to a name that does not exist, gives the same answer: "No one by that name is in the world." A linkdead target: "{name} has lost their link." An idle target gets the tell and the sender is told they are idle.
- `shout off|on` stops and restarts hearing ordinary shouts (saved). Channel messages are governed by the channel switch.

## Rules for all of them
- The message is untrusted text: strip control characters and the game's colour tags, collapse line breaks, limit to 300 characters, refuse an empty message with the usage line.
- Shout limits against flooding, as named numbers in one place: one shout per 10 seconds per character, and characters below level 2 cannot shout (they are told when they can). Channel messages are limited to one per 2 seconds. Wizards are exempt.
- Speaking counts as activity for the idle timer.
- A dead character, or one in the login or editor, follows whatever `say` does today.
- Nothing here writes to the Chronicle or Renown. Tells and whispers are not logged anywhere.
- Players see names as the game shows them elsewhere (capitalised character name; an invisible wizard appears as "Someone" to players who cannot see them, and cannot be targeted by `tell` by players who cannot see them, with the same "No one by that name" answer).

## Tests and docs
`tests/comm_test.py` with three or more connected characters and a wizard of level 1 and one of level 10: every command, every block, the flood limits, the channel membership rules (a player cannot send to or hear `wizard`; a level 1 wizard cannot send to or hear `sysop`, and cannot learn it exists), text cleaning, and `reply`. Help text, `docs/GAME_SYSTEMS.md` and the README command list.
