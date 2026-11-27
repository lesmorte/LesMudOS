# Who, visibility, all-players and stats

Owner's request (27 Nov 2026): "On the player front 3 commands: 'who' lists who is active (and visible) there should be a wizard command to go vis/invis and some items that can hide the user even from the who list. 'all-players' a wizard only (level 10?) that lists all players that have been created. 'stats' a shortened stats only list of stats".

## Decisions for the owner
Each has a recommendation and the alternative. The spec is written, and the code built, as if you accept every recommendation.

1. **`all-players` is level 10 only.** You wrote "level 10?". Alternative: level 5 and up (the level that approves publications and edits outside a home). The list shows creation and login dates of every player, which is the sort of thing the top wizard looks at; nothing in it is private (section 4), so 5+ would be safe too. Below the level the command does not exist: the words are the ones for any unknown command ("What?").
2. **Invisibility is a wizard's own command and nothing else.** `invis` and `vis` (also `invis on|off`) for wizard level 1 and up, on the wizard's own body only. No wizard can make anyone else invisible (no such tool exists today: there is no `force`, and there will not be one for this). The state is saved with the character and survives quitting, a lost connection and a take-over. Alternative: it ends at logout (a wizard who forgets would walk about unseen for weeks; saving it is the safer default for the wizard, and `vis` is one word).
3. **Who may see an invisible wizard: wizards of equal or higher level.** A level 3 wizard is seen by levels 3 to 10 and not by levels 0 to 2. Level 10 sees everyone. (The code that was waiting for this, `CanSee` in `/secure/comm`, already said exactly this.) Alternative: only higher levels see them, so two equals cannot see each other; rejected, equals are colleagues.
4. **What an invisible wizard's words and deeds look like** (section 3). Speech (`say`, `whisper`, `shout`, and a `tell`) shows "Someone", as the communication spec already decided and its test already checks, so that a mortal cannot learn a name by being spoken to; the mortal can `reply` and the words reach the wizard. A wizard-level channel (`wizard`, `sysop`) always shows the name to its members, who are all entitled to know it. A public channel shows "Someone" to those who cannot see the wizard. Doing something to another character on purpose (`give`, and the wizard tools `slay`, `restore`, `speed`) names the wizard: the act reveals them for that line. Ordinary room lines about what they do (sit, stand, take, drop, wear and the like) read "Someone ...", as in the dark. Alternative for channels: the name on public channels too ("they chose to speak"); the cost is that the mortals then know the name of a wizard they cannot see, and `tell` to that name is still refused until the wizard has spoken to them.
5. **Going and coming is silent.** An invisible wizard's arrival, departure, login, logout, `goto`, and a lost connection are not shown to anyone who cannot see them. `invis` and `vis` themselves are announced to the wizard only (the room is told nothing: a mortal then does not see the wizard "fade", they are simply no longer listed in the room). Alternative: a line to the room ("X fades from sight"), as many games do; it would tell everyone the moment the wizard hid.
6. **`tell` and `whisper` to an invisible wizard get the answer for a name that is not in the game, word for word**, until the wizard has spoken to that mortal in this session (a `tell`, a `whisper` or a `reply`); then the mortal may answer by name or by `reply`. "This session" ends when either body leaves the game, and when the wizard types `vis` or `invis`. Alternative: always refuse and allow only `reply`; simpler, but a mortal who knows the name could not answer a wizard who has just spoken to them.
7. **Creatures ignore an invisible wizard.** An aggressive creature does not notice one, and a creature asked to attack one does not. A wizard who attacks first is, as ever, fought (a creature in a fight keeps its enemy). Alternative: nothing; then an invisible wizard walking through the cellars would be ambushed and the mortals would see "Someone" being killed.
8. **The hood.** A new wearable property, `HidesFromWho` (false by default), keeps the wearer off the `who` list of everyone who is not a wizard, and does nothing else (section 5). The one item is **the Deep Traveler's Hood**, head slot, armour 0, any calling, **120 gold**, sold by **Corwin Dray, the outfitter, in Southgate, Oakenfell**. Prices of the neighbourhood: a leather cap is worth 25 (Oswy Thackett sells at 25), a minor healing potion is 20 gold at Corwin's, a boar spear 50, a felling axe 65, the hunting bow 20 at Torren's. 120 is two and a half steel dirks: a real decision for a character with 150 gold, and a real choice against a leather cap (armour 1) or a helm. Alternative: 80 (cheaper, would be bought by everyone who ever read about it) or 200 (out of reach until level 8 or so).
9. **A hidden mortal sees themselves on `who`, marked "(hidden)".** A hooded character who types `who` and saw an empty list would think the command was broken. The other players do not see them. Wizards see them, marked "(hidden)" (every wizard, whatever the level: it is a convenience of the player, not a rank). Alternative: they do not see themselves; rejected, it looks like a bug.
10. **`who` counts only what the asker sees.** "3 player(s) online:" counts the rows shown; nothing says how many were left out. A wizard's count includes the invisible and hidden ones they may see. The idle marks, the wizard mark and the link-dead mark are unchanged.
11. **The reminder is in `score` and at login, not in the prompt.** "You are invisible. Type vis to be seen." appears in the wizard's score and when they log in. Alternative: a prefix "(invisible)" on every prompt; rejected because prompts are parsed by clients and by tests, and the wizard's own prompt format is theirs.
12. **The website never shows an invisible wizard because it shows no one at all.** `/play/status` returns whether the button is offered and nothing else; the Heroes page reads `roster.json` (characters, wizards left out) and not who is online. Nothing in `web/` changes. The sync job copies four fields of each save and nothing new.
13. **`all-players` reads the saves through a small index kept by `/secure/roster`** (section 4), built in the background at boot, 40 saves at a time, and kept up to date at creation, login, logout and when a save file disappears. Nothing is read from disk on a command except a directory listing. Alternative: read every save on every command; fine at 200 characters and bad at 20,000 on the game thread. A new driver function, `SaveNames`, lists the names in the save directory (only the roster may call it); no other function can list the saves today.
14. **Creation and last-login dates.** Last login was already saved. A creation date is new (`created`, set when the account is made). An older save shows "before records began" for it. Dates are shown as year-month-day in UTC.
15. **`stats` leaves out seven numbers.** Physical Power, Precision, Evasion, Spell Power, Healing Power, Gathering Efficiency and Crafting Aptitude stay in `score`; with them the list is 15 lines, not 12. Resistances are shown (Mental and Physical); `score` shows no regeneration figure, so `stats` shows none. Alternative: add the seven as four more rows.
16. **A bare `stat` for a mortal still runs `score`**, and a wizard's `stat THING` is untouched. A wizard's `stats` shows their own, the same way.

## 1. Who

`who` lists the characters in the game that the asker can see, one to a line, in the order the driver lists connections, then the dropped bodies (link-dead):

```
4 player(s) online:
  Rayek the newcomer
  Alice the newcomer (idle 5m)
  Mina the newcomer [wizard 1] (invis)         seen by Mina herself and by wizards of level 1 and up
  Dana the newcomer (link-dead)
```

Marks, in this order after the name: `[wizard N]`, `(idle Nm)` (a link-dead body has `(link-dead)` in its place), then `(invis)` for an invisible wizard the asker may see (and for the wizard themselves), then `(hidden)` for a wearer of a hiding item, shown to wizards and to the wearer. The look and columns are as before. The count is the number of rows.

Who is shown to whom (`Roster.WhoMarks(viewer, target)`, null meaning not listed):

| Target | A mortal sees it | A wizard sees it | Itself |
|---|---|---|---|
| ordinary | yes | yes | yes |
| in a hiding item | no | yes, "(hidden)" | yes, "(hidden)" |
| invisible wizard (level L) | no | yes, "(invis)", if their level is L or more | yes, "(invis)" |
| both | no | as the invisible rule, plus "(hidden)" | both marks |

## 2. Invisibility

`invis` and `vis` (also `invis on`, `invis off`) are bound for everyone and answer only a wizard of level 1 or more (anyone else gets "What?"). They change a saved flag of the wizard's own body (`invisible`) and tell the registry (`/secure/roster.SetInvisible`), which holds the set of invisible bodies in memory. `vis` also ends the "spoke first" permissions (section 3). At login, the body tells the registry again (`SetupPlayer`) and says "You are invisible. Type vis to be seen." A wizard demoted to level 0 is no longer invisible (the registry checks the level on every question).

The one question everything asks is `Roster.Visible(viewer, target)`: true if the target is not invisible, or the viewer is the target, or the viewer is a wizard whose level is at least the target's. It is the only function other code uses, and it may be called by restricted code *only* through real code that asks it for its own viewer (section 6).

Where it is asked:

| Place | What changes for those who cannot see the wizard |
|---|---|
| `who` | not listed (section 1) |
| a room's contents (`look`, arrival, `Room.Describe`) | not listed; the "stands here, staring at nothing" line of a dropped body is not made for them either |
| `look at NAME`, `consider`, `give ... to`, `ask`, `kill`: any word that names a character | the wizard does not answer to the name (`Id` is false): "You see no 'mina' here." |
| arrival, leaving, login, logout, `goto`, a lost connection | no line |
| `say`, `shout`, `whisper` to another, `tell` | the speaker is "Someone" (existing rule) |
| a channel | by name on a wizard-level channel; "Someone" on a public one |
| a line about what the wizard does (`Say(...)` in `/std/adventurer`) | "Someone ..." with the wizard's name replaced |
| an act on someone (`give`, `slay`, `restore`, `speed`) | the wizard is named |
| `tell`, `reply`, `whisper` aimed at the wizard | the answer for a name not in the game ("No one by that name is in the world." / "No one by that name is here.") unless the wizard spoke to that character first |
| `goto NAME` by a lower wizard | "No such file" as for a name that is not there |
| aggressive creatures | do not notice, do not engage |
| GMCP | nothing in `Room.Info` or `Char.Status` lists other characters; `Comm.Channel.Text` carries the same name as the text line |
| `wizards` | lists every wizard with levels, online or not: unchanged |
| `finger` | does not exist |

A wizard still appears to themself everywhere.

Messages, per type, each decided once:
* **no line at all:** moving (leaves, arrives), entering and leaving the game, going link-dead (wizards never do), `goto`'s puffs of smoke.
* **"Someone":** say, shout, whisper to another, tell, a room line about an ordinary deed.
* **revealing:** a deed done to someone, and speech on a wizard-level channel.

Changes an invisible wizard makes while in a workshop are left behind when they leave it, like everything else done there (the workshop's body is restored from the checkpoint): they should type `invis` outside.

## 3. Tell, reply and whisper

`/secure/comm` asks `Roster.Visible` where it used to ask `QueryInvisible` of the body (no body answers that now). When a wizard sends a `tell`, `reply` or `whisper` to a character, comm tells the registry (`NoteSpoke`) and the registry remembers the pair of bodies. `Roster.Spoke(wizard, mortal)` is then true while both bodies live and the wizard has not typed `vis` or `invis`. Comm lets a tell or whisper reach an invisible wizard if `Visible` or `Spoke` holds; the name shown is still "Someone" (the existing test: "You tell Someone: who is this"). Everyone else gets the same words as for a name not in the game.

## 4. `all-players`

Level 10 only. `all-players`, `allplayers` and `all players` (the verbs `all-players`, `allplayers`, and `all` with the word `players`). Below level 10, or with anything else after `all`, the command does not answer: "What?", exactly as any unknown word, not a refusal that tells the command exists.

```
all-players [recent | new | PREFIX] [PAGE]
```

| Form | Meaning |
|---|---|
| `all-players` | all characters by name, page 1 |
| `all-players 2` | page 2 |
| `all-players recent` | by last login, newest first |
| `all-players new` | by creation, newest first (those with no date last, by name) |
| `all-players ra` | names beginning with "ra" (letters only), by name |

A page is 40 rows. Sample (80 columns at most; each column is as wide as its longest entry, a class name is cut to fit):

```
Name   Calling    Class  Lvl Wiz Created              Last login Now
Alice  Adventurer -        7   - 2026-11-27           2026-11-27 yes
Oldie  Warrior    Knight  12   - before records began 2023-11-14 -
Warden Mage       -       10  10 2026-10-01           2026-11-27 yes
3 character(s). Page 1 of 1.
```

The columns are exactly: name, calling, class, level, wizard level, created, last login, in the game now. Never shown: addresses or their keyed hashes, password hashes, the ignore list, inventory, anything else.

How it is read. `/secure/roster` is a blueprint that declares only eight `[Saved]` fields with the names of the player's own (`Level`, `archetype`, `advancedClass`, `subclass`, `prestigeClass`, `logins`, `lastLogin`, `created`). The master lets that one blueprint read a file under `/players/` (`ValidRead`), and the driver's `RestoreObject` loads only the fields the object declares, so the password hash and the address hashes are never loaded into any object but the player's own. The index (name, those fields) is in memory. It is built at boot by a call-out that reads 40 saves every quarter second; it is updated when a body logs in (`Note`), when a character is made, and when it logs out; each use of the command lists the save directory (`SaveNames`) and drops the characters whose file is gone and queues the ones it does not know. While the index is still being built the command says so and lists what it has. A caller must be the real body of a level 10 wizard acting on their own command (`ThisPlayer`), never restricted code, never a clone made in a workshop. One log line is written when it is used: "all-players: NAME" (no output). No character deletion exists today; the list follows the save directory, so a deleted save leaves it.

## 5. The hiding item

`Armor` (and so every wearable) has `protected virtual bool HidesFromWho` (false), and `public bool QueryHidesFromWho()`. A builder writes `HidesFromWho = true;` in `Create`; docs/BUILDING.md says so; `tests/lint_area.py` check 17 reports it on anything that does not inherit `/std/armor` and on a value that is not `true` or `false`. The body answers `QueryHiddenFromWho()` (any worn item hides). That is all it does: the wearer is in the room list, hears `tell`, shows link-dead in the room, and is named in say. A wizard sees "(hidden)". The wearer's `score` has "You are keeping out of sight of the who list." Taking the item off shows them at once (nothing is stored).

The Deep Traveler's Hood: `/domains/greenvale/oakenfell/southgate/obj/deep_hood`; "a deep traveler's hood", "A hood of heavy grey wool, cut deep enough to hide a face and long enough to fall over the shoulders. It has seen a good many roads and does not mind who knows it."; head, armour 0, light, any calling, Value 40, 120 gold at Corwin Dray, with one line of talk: "A deep hood, for those who would rather not be asked for at every inn. Folk who wear one are left off the lists the town criers keep. One hundred and twenty gold."

Later items (not built): **the Veil Ring** (a trinket, a quest reward from a Chancery clerk: the same flag, so a character can keep it with a helm); **the Cloak of the Unremarked** (body slot, a reward of the thieves' quest, armour 2, the same flag); **a Dyer's Charm** (a consumable that hides the wearer from `who` until the next logout). When the tag system arrives, `HidesFromWho` becomes a tag `hides_from_who` of a new family (`presence`): `Armor.QueryHidesFromWho()` becomes `HasTag("hides_from_who")`, every file that wrote `HidesFromWho = true;` writes `Tags.Add("hides_from_who");`, and lint check 17 becomes a tag check; the roster's one call stays the same.

## 6. Sandbox

* **Setting.** The only code that sets invisibility is the body's own command handler (private) and its login. The registry's `SetInvisible` accepts only a real body of `/std/player` with no domain and no restricted code on the stack, and only turns it on for a wizard. Sandbox code cannot call a body's private handler, cannot be a body, and a clone of `/std/player` made in a workshop has a domain and is refused. There is no `force`, and no function that sets another body's state.
* **Reading.** There is no `Query` function on a body for it (a name starting with `Query`, `Is`, `Has` or `Can` is callable by sandbox code on any real object; this one is not one of those). `Roster.Visible` is in the sandbox-aware list because real code that runs under sandbox code (a room's contents when a wizard in a workshop looks) must be able to ask; but it answers "visible" to a direct call from sandbox code, so sandbox code learns nothing from asking. What sandbox code can still learn is what the player whose command is running can see (the same as `who` and the room tell that player).
* **`all-players`.** The roster's list function checks level 10, the caller's own command, and that nothing restricted is on the stack; the master allows only that blueprint to read `/players/` and to list the saves (`SaveNames`). A /wiz object that calls it gets nothing.
* **In a workshop.** A wizard may be invisible in their own workshop; a visitor's view of who is there follows the same `Visible` rule, since the room code asks it.

## 7. `stats`

For any player (and a wizard's own). No box, no Chronicle, no quests, no equipment: two columns of label and value, 12 rows (13 with the points line), at most 48 columns (so it also reads in 80 and in a narrow client), plain ASCII only, so `charset ascii` changes nothing.

```
Alice - Adventurer - Level 7
Might       6          Grace        6
Vigor       6          Insight      6
Spirit      6          Resolve      6
Points to place: 12 (improve ATTRIBUTE)
Vitality    38/78      Damage       8-12
Endurance   82/82      To hit       +8
Focus       82/82      Defense      6
Armor       0          Dodge        0%
Block       0%         Critical     5.25%
Reaction    2.5        Attack Time  7.0 s
Mental Res  12         Physical Res 12
Carry       80 lb
```

A line "Points to place: 2 (improve ATTRIBUTE)" follows the attributes when there are points. An attribute raised or lowered by an effect shows the base in brackets as `score` does ("Might 6 (5)"). `help` lists it with `score`.

## 8. Tests

`tests/who_test.py` (180 checks, port 4843) covers sections 1 to 7: mortals see mortals; an invisible wizard is absent for mortals and lower wizards, marked for equal and higher, seen by level 10; the count lines; the room, `look at`, the arrival and login lines; `tell` and `whisper` refused with the same words as an absent name, byte for byte, and allowed after the wizard spoke; it persists over a reconnect and a take-over; the hood; `all-players` refused for level 9 and mortals with the unknown-command words, working for level 10, with every stored hash of the test characters grepped out of the output, sorting, prefix, paging, dates on new and old saves; `stats` widths and `charset ascii`; and a /wiz probe that cannot set or read invisibility or call the roster. `tests/comm_test.py` uses the real `invis` and `vis` where it used a test-only command.
