# Play-feel changes from the owner's play-through (November 2026)

Owner's requests, in his words where quoted. Each item needs tests and a line in `help` and the docs.

## 1. `verbose on|off`
Shows "the actual damage done to and by you" as a parenthetical after the combat line: `(+2 damage)` in green for damage you deal, `(-5 damage)` in red for damage you take. Applies to every source (weapon, skill, spell, damage over time, healing shown as `(+N Vitality)` in green). Off by default; saved with the character. Colour goes through the existing colour tags so `ansi off` players get plain text.

## 2. `prompt`
`prompt` alone shows the current prompt and the tokens; `prompt <text>` sets it; `prompt default` restores the default; `prompt off` gives the plain `> `. Default:
`{name} ({vitality}/{maxvitality}) ({endurance}/{maxendurance}) ({focus}/{maxfocus}) > ` (as first written, the tokens were `{hp} {maxhp} {stamina} {maxstamina} {mana} {maxmana}`; they still work as silent aliases)
which the owner described as "{PlayerName} (Vit Current/Vit Max) (Stamina Current/Stamina Max) (Mana Current/Mana Max) >". It "should display after you take an action of any sort": after every command's output, and after unprompted output (a combat round, someone arriving) so the player always ends on a current prompt. Saved with the character. Length-limited; no colour tags or control characters from the player's text other than the listed tokens. The editor and login prompts are untouched. The token names are Vitality, Endurance and Focus; keep the token table in one place.

## 3. Combat restrictions and `flee`
- While fighting: picking things up from the floor (or from a corpse or container on the floor) is refused, and so is ordinary movement. Say why and point to `flee`.
- `flee [direction]`: an attempt to disengage (keep today's 3-in-4 chance unless the numbers doc says otherwise). On success with a direction, move that way. With no direction, move back to the room the player last came from. If that way cannot be used (no such exit now, closed, blocked, the player logged in here), fall back to a random usable exit; if none, "There is no way out!". A named direction that cannot be used fails without spending the attempt. Fleeing ends the fight for the player as it does today.

## 4. Corpses
"Looting a corpse should cause it to crumble to dust unless there are components one can extract from it." When the last item or coin is taken from a corpse and it has no skin or blood left to take, it crumbles at once with a line to the room. When the last yield is taken from an already empty corpse, the same. A corpse that was empty from the start and has no yields keeps its present short timer.

## 5. Telling identical things apart
Every command that picks an object by name accepts a number: `get sword from corpse 2`, `loot 2.corpse`, `look at second rat`, `kill rat 2`. Order is the order shown in the room or inventory. Without a number the first match is used, as today. Implement once in the shared matching code, not per command.

## 6. Inventory
One item per line, identical items collapsed with a count:
```
Inventory
   an apprentice's staff (wielded)
   a minor healing potion (x2)
```
Identical means same name, quality and state (worn/wielded items are never merged with carried ones). Wherever lists of the same item appear in a sentence (a shop sale, a corpse's contents, the floor) use `a minor healing potion (x2)`, not the name repeated.

## 7. Idle and timeout (server level)
"There should be an idle and then timeout setting at the server level, otherwise a character could sit in a room with an easy mob and power level while AFK." Two server settings (driver configuration, with environment overrides, documented in DEPLOY.md): minutes without typed input until a player is marked idle (default 5), and minutes until disconnected (default 10). Idle players show as idle in `who`, and gain no experience while idle. On timeout the character is saved and disconnected with a message, the same clean way as `quit` (out of combat rules apply as for link loss). A warning is sent two minutes before. Wizards are exempt from the disconnect. Input from a client keep-alive (empty line, telnet NOP) does not count as activity.

## 8. Shared experience
Owner's rule: experience goes "based on percent of damage dealt to players still alive and in the room", with a small bonus for the killing blow; three roughly equal contributors get 40% (killing blow) and 30% each.
- A creature records the damage each player has dealt to it, capped at the Vitality it actually lost (overkill does not count). The record clears when the creature is back at full Vitality with no fight in progress.
- On death, the eligible players are those with recorded damage who are alive, in the creature's room, and not idle. 10% of the creature's experience (a named number in one place) goes to whoever struck the killing blow, if eligible and if that player dealt at least 85% of an even share of the eligible damage (0.85 / number of eligible players: 42.5% with two, 28.3% with three, 17% with five; the 85% is a named number beside the 10%). If the killing-blow player falls short, nobody gets the bonus and the whole amount is split by damage; the other 90% is split among the eligible in proportion to their recorded damage. Damage dealt by players who died or left is dropped, so the shares of those present always add up to the whole. A lone killer gets 100%, as today.
- Each share is rounded down, minimum 1, and then passes through that player's own experience rules (class modifier and any level rule) as a solo kill would.
- Every eligible player gets the kill credit for quests and counters (`NotifyKill`). The Chronicle report names the killing-blow player as the actor and all eligible players as participants. Wizards are still left out of history.
- The corpse's loot lock belongs to all eligible players, not only the killer.
- A killing blow from a non-player (another creature, a damage-over-time effect with no owner) gives no bonus; the full amount is split by damage.
- Parties will change this later (Future mechanic): healers and other non-damaging help earn nothing under this rule.

## 9. Aggressive creatures engage at once
Owner's report: "a character can bypass rooms with aggro creatures by just hitting a direction again; once in a room with aggro it should count as being in a fight even if they haven't hit yet." Today an aggressive creature starts its fight only after its Reaction Time, and until then the player is not fighting and may walk on.
- When a player arrives in a room (or logs in there) with an aggressive creature that would attack them, the fight starts as soon as the arrival is complete: the player sees the room, then "{creature} attacks you!", and is in combat, so walking is refused and `flee` is the way out. With several aggressive creatures, each one engages.
- The creature's first action still waits its own Reaction Time, exactly as now, so nobody is hit sooner than before. Only the moment the fight begins changes.
- Everything that stops an ambush today still stops it (the creature is dead, the player has left, a safe room, whatever `CanFight` refuses).
- `flee <direction>` onward remains possible at the usual 3-in-4 chance: running a gauntlet is a risk, not a free walk.
- Tests that walk through aggressive rooms (area_test, playthrough_test, corridor_mob_test, the simulator's assumptions) are updated to fight or flee; corridor_test keeps switching aggression off in its copy.

## 10. A room does not refill while the creature's corpse is still there
Owner's decision: "the refill should count the corpse of the creature too." Today a room's reset replaces a killed spawn whether or not its corpse still lies in the room, so a hunting ground piles up bodies beside fresh creatures.
- At a reset, a spawn that was killed is not replaced while a corpse of that same spawn is still in the room it was spawned in. The corpse counts as the creature until it is gone (looted to dust, rotted, or carried away). The next reset after that replaces it.
- The link is to the spawn itself (the corpse remembers which room's spawn it came from), not to the creature's name, so the corpse of another rat dragged in from elsewhere blocks nothing, and a corpse carried out of the room frees the slot.
- Encounters (`AddEncounter`) follow the same rule: no new encounter of a creature whose corpse from this room still lies here.
- A player's corpse never blocks anything.
- With creature corpses lasting ten minutes at most (less once emptied), a spawn returns no later than the first reset after that.


## 11. Leftovers from the owner's play log (24 November 2026)
- **Plural corpses.** "Three corpses of large rats are here." The room's grouping pluralises the creature's name after "corpse of" (a name without an article, a player's, stays as it is) and knows the irregular words (wolf/wolves, thief/thieves, deer, sheep, fish, mouse/mice, -man/-men). "corpse 2" targeting is unchanged: the ids are not touched.
- **A find after it is taken.** The room lists it as a sentence of its own (`TakenHere` / `EmptyHere` in `/std/find`; default "A purse lies here; you have taken what it held." / "... lies here, picked clean."); the Hidden Cache's bundle reads "A long oilcloth wrapping lies folded open here." The old `TakenShort` still serves `look at`.
- **`get all` and finds.** Finds are deliberately left out of "all": each is searched once, by name, and a search is loud for a sneaking Scout. `get all` says "Something here must be taken by name." when a find here can still be taken from.
- **`flee`.** "You flee west!" is told to the character directly, before the move, so it always comes before the new room.
- **News after the room.** The Renown, title and "mapped" lines that arriving earns are told after the command has finished (`EventService.Later`), so they come under the room description.
- **`aim` with no target** says "You are not fighting anyone: name a target, as in 'aim rat'."

## 12. `charset ascii`
Owner: "generate an ASCII fallback for score". A saved per-player setting, default UTF-8: `charset ascii` / `charset utf8` (`ascii on` / `ascii off`); `charset` alone shows it. In ASCII mode everything sent to the player is 7-bit: the one translation is `Thing.Asciify`, applied in `Player.CatchTell`. Box characters become + - |, a bullet at the start of a line becomes *, a bullet between words becomes " - ", curly quotes straight, dashes -, the ellipsis ..., accented letters lose their accents, anything else ?. GMCP is not touched. The new-character greeting and `help` name the command. See docs/specs/score.md.

## 13. `time` and the player's time zone
Owner: "I would like a time command that shows the users current time, it should have a timezone option to pick/set a timezone, if no timezone is provided it should show all timezones available."
- `time` (and `date`, the same command) shows the date and time in the player's zone, with the zone's name and offset, daylight saving followed: "It is 9:40 am on Tuesday 24 November 2026, Eastern time (America/New_York, UTC-5)." and, under it, the in-world date as before. With no zone set it shows UTC and "Set your own with time -timezone ZONE."
- `time -timezone` (also `-tz`, `timezone`, `zones`) lists about forty common zones by region with their offsets now, says any standard zone name is accepted, and says `time -timezone REGION` (America, Europe, Asia, Africa, Australia, Indian, Antarctica, Arctic, and `Pacific/`, `Atlantic/` with the slash, because "pacific" and "atlantic" mean US/Canadian time zones) lists every zone of that region.
- `time -timezone ZONE` sets it: an exact IANA name (any case, spaces for underscores), the city alone when unambiguous ("london", "new york"), the short names (eastern, central, mountain, pacific, alaska, hawaii, est, cst, mst, pst, edt ... mapped to the regional zone so that daylight saving is right), a fixed offset ("utc-5", "+1", "gmt+5:30"; -12 to +14). More than one match: the matches are listed. Unknown: said, and the list is pointed at. `none` (or `clear`) unsets.
- Saved with the character; nobody else sees it (not `who`, `finger`, `look`, `score`).
- The zone data comes from two driver efuns, `TimeZoneNames()` and `LocalTime(zone)` (src/LesMudOS.Driver/KernelTime.cs), because the mudlib sandbox does not allow TimeZoneInfo. A name typed by a player is only ever compared with the driver's own list; it never reaches the file system or the log. Without a zone database on the machine the list says so and only UTC and fixed offsets work.
- The logic is in `/secure/clock`; the player body keeps only the setting and the command.

## 14. A dropped player's body lingers (24 November 2026)
Owner: "On dropping from a room a player should stay and be vulnerable (and fight back)". He first asked for five rounds of combat, then fixed it: **"let's make the window fixed at 1 minute"**. So:
- When a connection ends without `quit` (a dropped socket, a closed client, the idle timeout) the body stays where it stands, link-dead, for **one minute** (`LingerSeconds`, 60; the driver setting `LESMUDOS_LINGER_SECONDS` shortens it for tests), whether or not a fight is on. The character is saved at the drop, and again when the body goes or dies. Then it is removed as `quit` would.
- It is fully vulnerable: creatures attack it and keep attacking, aggressive creatures arriving engage it. It fights back with ordinary attacks only (no skills, potions, flight, aimed shots; what was being prepared or queued is abandoned). Experience and loot are as for a present player: a kill leaves a corpse with the gold and loot in the room, entitled to everyone who shared in it (there is no auto-loot).
- A death is an ordinary death (corpse, penalties); the body is then removed at once and the next login wakes in the Guest Rooms.
- `quit` is refused in a fight ("You cannot leave in the middle of a fight. Flee first."); before this change `quit` saved and destroyed the body at once, which ended the fight: a free escape. Out of a fight it leaves at once. `shutdown` saves and removes link-dead bodies; a wizard's body is removed at once as before.
- Logging in again during the minute gives the new connection to the same body (`/secure/login` asks `/secure/linkdead`): no second body, Vitality and fight as they stand, "You shake off the daze and find yourself where you stood." After the minute a login is the ordinary one, to the saved room.
- Others see "Lothan's eyes go blank." at the drop, "Lothan stands here, staring at nothing." in the room, "(link-dead)" in `who`, and "cannot hear you" to a tell or whisper; nothing can be given to them. "Lothan fades from sight." when the body goes. The disconnect line in the driver log ends ", body lingers 60 s".
- Code: `Player.StartLinger / EndLinger / Reconnected`, `/secure/linkdead`, the driver's `LingerSeconds`, `DropConnection` (src/LesMudOS.Driver/KernelLink.cs).

## 15. A nudge to the Adventurers Guild (26 November 2026)
Owner's evidence: a new Scout reached level 4 and "You have no quests." was all `quests` ever said. Nothing told a new character that the Guild (Rowan, the board, the contract that sends them down the tavern's cellar, and later the travel pass) is where the game starts. Owner approved a gentle nudge. It tells; it never blocks.
- **Who.** A "newcomer" is a mortal (wizard level 0) who has never taken a quest (the quest record is empty: a taken quest stays in it, active or done) and is level 10 or lower. Wizards, returning characters for the welcome, and everyone with a quest see none of it.
- **The welcome.** After the first look at the Village Square a new character is told: "Your first stop is the Adventurers Guild (west from the Village Square). Guildmaster Rowan gives out the work that pays in experience and gold: read the board there, then accept a contract. help start tells you more." Only on the first login.
- **`quests` and `score`.** "You have no quests." is followed, for a newcomer, by "Contracts are given out at the Adventurers Guild (west from the Village Square): read the board and accept one from Guildmaster Rowan." It goes with the first quest taken, and a completed or abandoned history never brings it back.
- **The cellar stair.** The first time a newcomer goes down from the Hollowman's Rest into the Tavern Cellar, once per character (saved, `tipsShown`), the line comes under the room: the stair leads to goblins, the Guild pays for exactly this work, and they may still go on. Nothing is refused.
- **`help start`** (also `begin`, `guild`, `new`) says where to begin, in four sentences: the Guild and the board, `accept cellar`, kit and the practice yard and `improve`, `rest`, `report`. The `help` list opens with a line that points at it. It is shown to everyone who asks.
- **Where the words live.** `/secure/guidepost` holds all four texts and reads the way to the Guild from the Village Square's own exits, so the direction is true by construction. The body keeps `QueryNeedsGuild()` and `TakeTip("stair")` (only that id, once, only for a newcomer); `Player` calls the welcome; `village/cellar.cs` overrides `Init` for the stair line (no change to `room.cs`).
- **Tests.** `tests/playfeel_test.py` `run_guild` (`PF_ONLY=guild` runs it alone): the welcome and its direction walked, the hint under `quests` and `score`, `help start`, the stair once only, a returning character, a character with a quest, a wizard.

## Where a login begins

A character who leaves the game, by `quit`, by the idle timeout or by a dropped connection (NetDead), is saved in the room they stand in, and the next login puts them back in it. The room path is the saved `logoutRoom`, refreshed at every save and at the moment of leaving. The arrival is the ordinary move into the room, so an aggressive creature there engages as it would for anyone walking in. Silent fallbacks to the Village Square: no saved room (a new character, an old save), the room no longer loads, or the room is not a legitimate place to wake: the void, any clone, anything under /wiz (a wizard's own /wiz/NAME home is allowed for that wizard). Death is separate: a dead character wakes in the Guest Rooms. On a dropped connection or timeout in a fight the body is saved and destroyed at once (no lingering body, the fight ends with it).

## The prompt after a crossed line

A prompt that followed unprompted output (a combat round, someone arriving) can cross, on the wire, a line the player had already sent. If a line arrives within 300 ms of such a prompt, the driver does not assume the player's Enter moved the cursor to a fresh line: the reply (or the next prompt, if there is no reply) starts with a newline.
