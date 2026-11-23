# The wizard sandbox and publishing: implementation design

Every wizard is a game creator with a home directory, `/wiz/<name>/`, where they can write and run code from wizard level 1 (see DRIVER_API.md section 6). This document makes that home a sandbox, and adds the way finished work moves into the real game.

Decided by the owner:

- A wizard's home is a sandbox. What is built there cannot affect the real game.
- Any wizard, level 1 and up, may write and run code in their own home.
- A wizard asks for their work to be published. Any single wizard of level 5 or higher can approve it, and on approval it moves to the real environment.
- Wizards of any level make no history: no Renown, no Chronicle entries or firsts, whatever their level and wherever they are.
- The admin secret lives only in the environment variable `LESMUDOS_ADMIN_SECRET` and is never readable from game code.

Status: built. The wizard's view is in BUILDING.md ("Your workshop" and "Publishing your work"), the reviewer's in PUBLISHING.md, the driver pieces in DRIVER_API.md. The red-team suite is `tests/sandbox_test.py`.

## 1. Terms

- **Sandbox code:** any object whose program path is under `/wiz/`. That covers rooms, creatures and items written in a home, including when they inherit from `/std`: trust is decided by the object's program path, never by which base class's code happens to be running.
- **Sandbox object:** sandbox code, and also every object created while sandbox code is running or for a character in the workshop. A goblin from `/domains` cloned by a wizard's room, its corpse, its loot, the skin cut from it, a sword cloned with `clone` in the workshop: all of them are sandbox objects, although their program is real game code. The mark is given when the object is created and never changes.
- **Sandbox space:** the sandbox rooms, and everything inside them.
- **In the workshop:** a wizard's character from the moment it enters sandbox space until it is restored (section 3).
- **The real game:** everything else.

The driver knows none of these words. It gives every object a *domain*, a string the master chooses when the object is created (`""` for the real game, `wiz:<name>` here), and treats an object with a domain as *restricted*. Everything else in this document is policy in `/secure/master` and the mudlib.

## 2. The rule in one sentence

Nothing that happens in sandbox space survives leaving it, and nothing from sandbox space can reach anyone who is not in it.

## 3. How the rule is kept

### Who can be in sandbox space

Only wizards (level 1 and up). An ordinary player cannot be moved into a sandbox room by any means: not by an exit, not by a command, not by a wizard's code. A wizard demoted to level 0 while in the workshop is restored and put back where they entered within one heartbeat.

The wizard's own typed words are not sandbox code: a wizard in the workshop can still `tell`, `shout` and use the channels. What sandbox code makes them do cannot (below).

### The workshop checkpoint

When a wizard steps from the real game into sandbox space, their character is checkpointed: saved exactly as at logout (with the real room they came from as the place they would log in again), and marked as being in the workshop. While the mark is set:

- The character is not saved. Level, experience, gold, quests, class, skill ranks, unlock counters, proficiencies and recipes, attributes, effects and inventory can all change freely, and none of it is written anywhere. The master refuses a write of the save file while the mark is set, as a second lock.
- Nothing the character does is history: the event service already refuses every wizard (section 3, "The game's systems"), and the daemons that keep their own records refuse a character in the workshop and every sandbox object.
- A death leaves no corpse and no lost-property record.
- Whatever is cloned for the character (the `clone` command, crafting, gathering) is a sandbox object.

When the wizard leaves sandbox space, by walking or `goto` into a real room, by quitting, by losing their connection, by dying, or by a driver restart, the character is restored from the checkpoint exactly as at login:

- Walking out, `goto` and death: a fresh body is made from the save file, the connection is handed to it, the old body and everything it carried is destroyed, and the checkpointed inventory is cloned again. The fresh body stands where the wizard went (after a death: in the room they entered the workshop from). The restore runs as soon as the move that left the workshop has finished, before any other command is read.
- Quitting and losing the connection: the save file already holds the checkpoint, and the body is destroyed as usual without being saved.
- A driver restart: the same; nothing was ever saved from the workshop.

They are told in one line that the workshop's changes were left behind. So a wizard can give themselves a sword that kills in one blow, a thousand levels and a fortune inside their own area, test with them, and walk out as the character who walked in.

### What sandbox code may touch

The driver keeps a stack of the objects whose code is running, the one `PreviousObject()` reads. While a restricted object is anywhere on that stack, the execution is *restricted*, whoever's code is running at the top: sandbox code that calls a real object's method, and the real code that method runs, are both restricted until they return. A delayed call or heartbeat of a sandbox object starts restricted, because its owner is the bottom frame, and a delayed call can only run code of its owner's own class (the driver checks), so a sandbox object cannot hand a delegate to a real object to run later.

A restricted execution may do these to real objects, and nothing else:

- call the read-only functions: names beginning `Query`, `Is`, `Has` or `Can`, and `Short`, `Long`, `LongFor`, `Id`, `HealthLine`, `Stat`, `Appraisal`, `DescribeCount` (every such public function in `/std`, `/secure`, `/domains`, `/classes`, `/skills` and `/events` is read-only, and that is the rule for new code);
- call anything on the rule objects that hold no state: the skill, trait and class blueprints under `/skills` and `/classes`, and the class registry;
- call the few daemon functions that know about sandbox space (the door register; the notice dice of sneaking);
- call anything on a character in the workshop, or on what such a character carries, because all of it is thrown away on leaving.

Everything else is refused silently (the call returns nothing) and logged, at most a few lines per execution.

What crosses between a sandbox object and a real one, in either direction, is plain data only: numbers, strings, object handles, and fresh copies of arrays, lists, sets and dictionaries of those (a dictionary always with the default comparer). Anything else (an object of a class written in a home, whose `ToString` or `Equals` would run later in the other side's frame; an enumerator; a delegate) does not cross: an argument like that refuses the call, a result like that arrives as null. An exception thrown by sandbox code reaches real code as a plain error with its message, read while the sandbox frame is still on the stack. Typed views (`As<T>()`) never cross at all, in either direction. So sandbox code can never run in a real object's frame, and real code can never be handed a reference into a real object's state. `As<T>()` from a restricted execution returns null for any real object, so direct typed calls are impossible. The guard is one check in the driver's `Call` path, decided by the master's `ValidCall`, so it covers every object type (players, creatures, rooms, items, daemons) and every public method, including ones written later; there is no list of mutators to keep in step.

So a wizard's object cannot kill the Goblin Overseer from afar, hand a friend gold through `Users()`, set a quest, give experience or kill credit, start a fight, equip, add an effect, move, rest or wake anyone, or reach a daemon to write history, through any route: directly, from a creature, an item, a room, a delayed call, a heartbeat, an `Init` hook, or from inherited `/std` code running in a sandbox object.

Text follows the same rule: `Write`, `TellObject`, `TellRoom` and `Say` from a restricted execution reach only sandbox objects and characters in the workshop. Sandbox code cannot print a convincing lie on an ordinary player's screen. It cannot bind verbs (`AddAction`) on, or capture the next line (`InputTo`) of, a character who is not in the workshop. Even in the workshop it can never ask for a hidden line (a password prompt), and cannot take over the commands that protect a character or decide for the real game (`password`, `quit`, `save`, `goto`, the file and publishing commands, `promote`, `update`, `shutdown`), so a reviewer visiting a workshop keeps them.

### What can cross the boundary

| Mover | Into sandbox space | Out of sandbox space |
|---|---|---|
| A wizard | Yes; checkpoint | Yes; restore |
| An ordinary player | No | n/a |
| A real creature or NPC | No | n/a |
| A sandbox object (item, creature) | Yes | No. It cannot be placed in a real room, in a real container, or in the hands of anyone not in the workshop. |
| A real object (a sword bought in the village) | Only carried by a wizard; it cannot be dropped, given or put anywhere in sandbox space | It leaves with the restore, as it was at the checkpoint |

Because real objects cannot be set down in sandbox space and everything carried is rebuilt from the checkpoint on leaving, nothing real can be lost, duplicated or altered there. Nothing a character in the workshop carries can be moved into the real game at all, by anyone, so nothing can be passed out in the moment between leaving and the restore.

Sandbox code may move only sandbox objects and characters in the workshop, and things already inside them. It cannot move, summon or teleport anything else, and it cannot pull a real thing into the workshop.

### Where it is enforced

The single enforcement points, in the order they were built:

1. **Movement.** The driver asks the master before every move (`MoveRefusal(what, destination, caller)`; a refusal is an error whose text is the one-line reason) and after the move calls `OnMoved(from)` on the object, before any `Init`, so the workshop mark is set before sandbox code can see the character. When a container is destroyed, what may not fall out into its environment is destroyed with it.
2. **State changes on living things, and everything else.** The call guard above (`ValidCall`), one check in the driver, decided by the domain of the objects on the call stack. Real objects keep their own `PreviousObject()` checks as a second layer; every per-file "trusted tree" test also requires the object to be real (domain `""`), so a sandbox copy of a `/domains` creature is not trusted anywhere.
3. **Text output.** The same guard, applied by the driver to the `CatchTell` of every listener of a text efun.
4. **Object creation and destruction across the boundary.** The domain given at creation (section 1). A blueprint is always created with an empty call stack and no `ThisPlayer()`, so sandbox code that loads a real room loads it exactly as anyone else would: its spawns are real. Destructing another object, like every privileged efun, is refused to a restricted execution.
5. **The daemons.** A restricted execution reaches only their read-only functions. Those that keep records (events, Renown, Chronicle, rumours, finds, lost property, the wizard registry, the publication register) also refuse a character in the workshop and every sandbox object, and the event service refuses every wizard.

Existing guards are unchanged and still apply, and the master refuses every privileged efun (file efuns, save files, `update`, `exec`, destructing other objects, the admin secret, `shutdown`) to a restricted execution, whatever program is asking. A wizard's object cannot make a visiting level-10 wizard's body edit a file, promote anyone, approve a publication or shut the driver down.

### The game's systems and the rule

| System | How the rule applies |
|---|---|
| Combat, damage, healing, effects, equipment, kill credit, experience, gold, quests | Only between sandbox objects and characters in the workshop; undone by the restore. Calls on anyone else are refused. |
| Corpses, loot, skins and blood | A creature that dies in a restricted execution leaves a sandbox corpse with sandbox loot; what is cut from it is a sandbox object. None of it can leave. A character in the workshop leaves no corpse. |
| Crafting daemon and proficiencies | A wizard may craft and gather in the workshop; what is made is a sandbox object, and proficiency and recipes gained are undone by the restore. Gathering nodes and stations that are sandbox objects give nothing, as before. |
| Finds | The register of finds refuses sandbox rooms, sandbox objects and characters in the workshop: a find in the workshop pays nothing and records nothing. |
| Chronicle, Renown, rumours, firsts | Wizards of any level make no history anywhere (the event service drops them, in or out of the workshop). Sandbox code cannot reach the history daemons at all. |
| Skill ranks, unlock counters, trainers | A trainer that is a sandbox object cannot teach (the rank service trusts only real trainers). Kills in the workshop are not credited (the rank service is refused to restricted executions), and anything counted on the character is undone by the restore. A wizard's kills in the real game count for their own unlocks like anyone's: that is character progress, not history. |
| Rest | Resting is the character's own state: free in the workshop, undone on leaving. Sandbox code cannot rest or wake anyone outside. |
| Sneak | The notice dice are shared and read-only; sandbox creatures may roll them. Sandbox code cannot set the test dice, end anyone's sneaking or spot anyone outside. |
| Doors and their shared state | A door's state lives in `/secure/doors`, and only a room that is one end of a door may change it. A sandbox room is therefore one end of every door it can change: a door between two of the game's rooms cannot be reached from a workshop. Nothing there is saved. |
| Communication daemons and channels | Sandbox code cannot say, tell, shout or speak on a channel; it can only send text to characters in the workshop. The wizard's own typed words are not sandbox code. |
| Attributes | Placed points and attribute changes in the workshop are undone by the restore. |
| Shops and merchants | A sandbox shopkeeper trades only with characters in the workshop and its goods are sandbox objects; real coins cannot be dropped there, and gold earned there is undone. A real shop cannot be reached from sandbox space. |
| Idle timer | Unchanged. Wizards are never disconnected for idleness; a wizard idle in the workshop stays there, unsaved. |
| GMCP (the map and gauges) | Only real `/std` and `/secure` code may send GMCP, and while sandbox code runs only to a character in the workshop. Sandbox code cannot reach the GMCP service or read anyone's facts. A wizard's own map follows them in the workshop: their body asks the service after every prompt and heart beat, with no sandbox code running. Workshop rooms, clones, and every place a workshop room's exits name get room numbers from the range that is never saved. |
| The test client's `sync` | A player command; it changes nothing and works anywhere. |
| Classes, skills, traits | Rule objects with no state of their own; sandbox code may use them, and their effects go through the call guard like any other code. |

## 4. Publishing

The existing `promote` command sets wizard levels, so the publishing commands have their own names.

### Who may change the real game

Below level 10 nobody writes outside `/wiz` directly. Since the trusted trees (`/std`, `/secure`, `/domains`, `/classes`, `/skills`, `/events`, and everything else outside `/wiz`) are real game code, a wizard of level 5 to 9 changes them only through an approved publication:

| Path | write (`mkdir`, `edit`, `copy` to, `move`, `rename`, `delete`, `rmdir`) | `update` | `clone` |
|---|---|---|---|
| Your own home | 1 | 1 | 1 |
| Another wizard's home | 5 | 5 | 3 |
| Anywhere outside `/wiz` | 10 | 10 | 3 (in the workshop: 1; what is made is a sandbox object) |
| `/wiz` itself, a home directory itself | 10 | 10 | 3 |

This narrows the old rule (level 5 wrote `/domains` directly). It is the owner's to overrule.

### Asking

`submit <directory> [to <destination>] [: <note>]`, from wizard level 1, for a directory inside the wizard's own home.

- The destination defaults to `/domains/<directory name>` and must be under `/domains`. If something is already published there by the same submitter, this is an update to it; if it belongs to someone else or to the base game, the request is refused. The approver may name another destination from the same allowed set.
- Names are checked strictly: every directory and file name is lower-case ASCII letters, digits and `_` (files end in `.cs`), so `..`, hidden names, case tricks and Unicode look-alikes are refused; a directory or file that is a symbolic link is refused; at most 200 files.
- The request records the submitter, the source, the destination, the note, the time, and the list of files with the SHA-256 of each file's content. What is approved is exactly what was submitted: if any file changes, appears or disappears afterward, approval is refused until it is submitted again.
- Refused at once, with reasons, if the directory is empty, holds a file that does not compile in the sandbox, names a path in any wizard's home other than the submitted directory, or defines a class name already used elsewhere in the mudlib (the whole-mudlib build needs unique names).
- Every online wizard of level 5 or higher is told, and others see a count of waiting requests when they log in.

### Reviewing

- `submissions` lists requests: one's own for any wizard, all of them for level 5 and up. `submission <number>` shows one in full, with its files and hashes and, for an update, which files are new, changed or removed.
- A reviewer reads the files with `cat` and walks the area with `goto`, in the workshop like anyone else.

### Deciding

- `approve <number> [to <destination>]`: any one wizard of level 5 or higher, other than the submitter. Nobody approves their own request, at any level.
- `reject <number> <reason>`: the same people. The submitter is told the reason.
- `withdraw <number>`: the submitter.
- `revoke <number> [: <reason>]`: level 10 only, for a request that was approved. The published files are removed; objects already loaded from them keep running until they are destructed or the driver restarts.

### What approval does

1. Reads every file once and checks its SHA-256 against the request, and that no file was added or removed.
2. Writes the directory tree to the destination from exactly that content. In each file, every occurrence of the source path is rewritten to the destination path, so exits and spawns that pointed at `/wiz/<name>/<directory>/...` point at the published copy. After the rewrite no file may mention `/wiz/`.
3. Compiles every written file in the real game, then loads each one that is not abstract. If any fails, every written file and created directory is removed and the approval is refused with the errors; nothing is half-published.
4. Records who approved it and when, the SHA-256 of each file as published, tells the submitter, and writes it to the driver log.

The writing is done by the publication register `/secure/publish`, which the master allows to read `/wiz` and write and compile under `/domains` and nowhere else, and only from a real player's command. The original stays in the wizard's home, where they can keep working and submit an update. The published copy is ordinary game code under `/domains`: from now on it changes only by another approved publication or by a level 10 wizard.

### Connecting it to the world

A published area is reachable by `goto` at once, and by players only when some real room gains an exit to it. Adding that exit is an edit to a real file, so it is done by a level 10 wizard, or as part of an approved update of an area that already connects. `approve` reports the published entry rooms as a reminder.

### Records

Requests and their outcomes are kept permanently by `/secure/publish` with its own save file. Publication is not reported to the Chronicle: wizards make no history.

## 5. What a wizard sees

- Entering: "You step into the workshop. Nothing that happens here will follow you out."
- Leaving: "You leave the workshop as you entered it."
- Trying to carry work out, drop real gear inside, or bring a player in: a one-line refusal saying which rule applies.
- `score` shows a workshop marker while it applies.

## 6. Testing

`tests/sandbox_test.py` (port `LESMUDOS_TEST_PORT`, default 4840). Every attempt is one named check, made by a level 1 wizard's hostile objects from a room, a creature, an item, a delayed call, a heartbeat, an inherited base class and an `Init` hook, against a real player, a real creature, a real room, a daemon and the publishing queue; each must fail and leave the target unchanged. Then the positive cases.

- A wizard enters their area, gains levels, gold, quest states and items from their own objects, dies once, and leaves by each route (walking out, `goto`, quit, dropped connection, driver restart, death): the character is identical to the checkpoint each time, and the save file never changed.
- An ordinary player cannot be brought into sandbox space by an exit, by `goto` used on them, or by sandbox code calling the move efun.
- Sandbox objects cannot be dropped, given, spawned or moved into the real game; real objects cannot be set down in the sandbox.
- From a wizard's own object, against an ordinary player and against a real creature elsewhere: gold, experience, quest, kill credit, damage, death, healing, effects, equipment, movement and text are each attempted and each refused, with the targets unchanged afterward.
- The same attempts against a wizard who is in the workshop succeed, and are undone when they leave.
- No event, Renown, Chronicle entry or vault record results from anything done in the workshop, including levelling.
- Publishing: submit, the notices, review listing, approve by a second wizard, path rewriting, the published area loading and working as real game code, an update submission, rejection with a reason, withdrawal, refusal when a file changed after submission, refusal on a compile error with nothing left behind, refusal on a duplicate class name, refusal of self-approval, a level 4 wizard unable to approve, revocation by level 10.
- An independent review, by someone who did not write the code, that tries to break the boundary.

## 7. Limits

- The sandbox is a boundary for game state, not for the machine. Sandbox code still runs inside the driver process with only the compile-time allow-list (DESIGN.md section 8) between it and the host; it can use memory and time up to the per-execution limits and can slow the game for everyone. Promotion to wizard remains a decision about trusting someone with the machine.
- A sandbox room can destroy itself with a visiting wizard inside, which disconnects them (their save file is the checkpoint, so nothing is lost). It cannot do this to anyone outside the workshop.
- Approval is a code review. Code can behave differently depending on where it lives (its own path, the time, who is online); the reviewer must read it. The record of hashes proves what was approved.
