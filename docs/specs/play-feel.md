# Play-feel changes from the owner's play-through (November 2026)

Owner's requests, in his words where quoted. Each item needs tests and a line in `help` and the docs.

## 1. `verbose on|off`
Shows "the actual damage done to and by you" as a parenthetical after the combat line: `(+2 damage)` in green for damage you deal, `(-5 damage)` in red for damage you take. Applies to every source (weapon, skill, spell, damage over time, healing shown as `(+N Vitality)` in green). Off by default; saved with the character. Colour goes through the existing colour tags so `ansi off` players get plain text.

## 2. `prompt`
`prompt` alone shows the current prompt and the tokens; `prompt <text>` sets it; `prompt default` restores the default; `prompt off` gives the plain `> `. Default:
`{name} ({hp}/{maxhp}) ({stamina}/{maxstamina}) ({mana}/{maxmana}) > `
which the owner described as "{PlayerName} (Vit Current/Vit Max) (Stamina Current/Stamina Max) (Mana Current/Mana Max) >". It "should display after you take an action of any sort": after every command's output, and after unprompted output (a combat round, someone arriving) so the player always ends on a current prompt. Saved with the character. Length-limited; no colour tags or control characters from the player's text other than the listed tokens. The editor and login prompts are untouched. Token names will be renamed by the attribute build (Vitality, Endurance, Focus); keep the token table in one place.

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
- On death, the eligible players are those with recorded damage who are alive, in the creature's room, and not idle. 10% of the creature's experience (a named number in one place) goes to whoever struck the killing blow, if eligible; the other 90% is split among the eligible in proportion to their recorded damage. Damage dealt by players who died or left is dropped, so the shares of those present always add up to the whole. A lone killer gets 100%, as today.
- Each share is rounded down, minimum 1, and then passes through that player's own experience rules (class modifier and any level rule) as a solo kill would.
- Every eligible player gets the kill credit for quests and counters (`NotifyKill`). The Chronicle report names the killing-blow player as the actor and all eligible players as participants. Wizards are still left out of history.
- The corpse's loot lock belongs to all eligible players, not only the killer.
- A killing blow from a non-player (another creature, a damage-over-time effect with no owner) gives no bonus; the full amount is split by damage.
- Parties will change this later (Future mechanic): healers and other non-damaging help earn nothing under this rule.
