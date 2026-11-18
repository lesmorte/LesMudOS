# The wizard sandbox and publishing: implementation design

Every wizard is a game creator with a home directory, `/wiz/<name>/`, where they can write and run code from wizard level 1 (see DRIVER_API.md section 6). This document makes that home a sandbox, and adds the way finished work moves into the real game.

Decided by the owner:

- A wizard's home is a sandbox. What is built there cannot affect the real game.
- A wizard asks for their work to be published. Any single wizard of level 5 or higher can approve it, and on approval it moves to the real environment.

Status: design, queued. It is built after class system stage A, because it changes the same player and creature code, and it needs one small driver addition.

## 1. Terms

- **Sandbox code:** any object whose program path is under `/wiz/`. That covers rooms, creatures and items written in a home, including when they inherit from `/std`.
- **Sandbox space:** the rooms that are sandbox code.
- **The real game:** everything else.

## 2. The rule in one sentence

Nothing that happens in sandbox space survives leaving it, and nothing from sandbox space can reach anyone who is not in it.

## 3. How the rule is kept

### Who can be in sandbox space

Only wizards (level 1 and up). An ordinary player cannot be moved into a sandbox room by any means: not by an exit, not by a command, not by a wizard's code.

### The workshop checkpoint

When a wizard steps from the real game into sandbox space, their character is checkpointed: saved exactly as at logout, and marked as being in the workshop. While the mark is set:

- The character is not saved. Level, experience, gold, quests, class, Renown, effects and inventory can all change freely, and none of it is written anywhere.
- Nothing the character does is reported as an event, so it earns no Renown and can make no history, and no vault record is written for it.
- A death leaves no corpse.

When the wizard leaves sandbox space, by walking or `goto` into a real room, by quitting, by losing their connection, by dying, or by a driver restart, the character is restored from the checkpoint exactly as at login: everything carried is destroyed and the checkpointed inventory is cloned again. They are told in one line that the workshop's changes were left behind.

So a wizard can give themselves a sword that kills in one blow, a thousand levels and a fortune inside their own area, test with them, and walk out as the character who walked in.

### What sandbox code may touch

A call from sandbox code that would change a living thing (damage, healing, gold, experience, quests, kill credit, equipment, effects, class data, movement) is accepted only if the target is in the workshop or is itself sandbox code. Otherwise it is refused silently and logged. This covers players and also the real game's creatures and NPCs, so a wizard's object cannot kill the Goblin Overseer from afar or hand a friend gold through `Users()`.

Text follows the same rule: sandbox code can send text only to characters who are in the workshop. It cannot print a convincing lie on an ordinary player's screen.

### What can cross the boundary

| Mover | Into sandbox space | Out of sandbox space |
|---|---|---|
| A wizard | Yes; checkpoint | Yes; restore |
| An ordinary player | No | n/a |
| A sandbox object (item, creature) | Yes | No. It cannot be placed in a real room, in a real container, or in the hands of anyone not in the workshop. |
| A real object (a sword bought in the village) | Only carried by a wizard; it cannot be dropped, given or put anywhere in sandbox space | It leaves with the restore, as it was at the checkpoint |

Because real objects cannot be set down in sandbox space and everything carried is rebuilt from the checkpoint on leaving, nothing real can be lost, duplicated or altered there.

Sandbox code may move only sandbox objects and characters in the workshop. It cannot move, summon or teleport anything else.

### Where it is enforced

- **Movement** is an efun, so the driver must ask. One driver addition: the master object is consulted before any object is moved (`ValidMove(what, destination, caller)`), in the same way it is consulted for other privileged operations. The policy above lives in the sample master. This is generic and has no knowledge of wizards or `/wiz` in the driver.
- **State changes** are guarded in one place in the base living object, used by every public method that changes state, with `PreviousObject()` identifying the caller.
- **Text** is guarded in the player's single output path.
- **Events, Renown, the Chronicle and the vault** already refuse callers outside the trusted trees; the workshop mark adds that the real code paths skip them for a character in the workshop.

Existing guards are unchanged and still apply: sandbox code cannot use file efuns, read or write save files, change wizard levels, reach the admin secret, update programs, destruct other objects or shut the driver down.

## 4. Publishing

The existing `promote` command sets wizard levels, so the publishing commands have their own names.

### Asking

`submit <directory> [to <destination>] [: <note>]`, from wizard level 1, for a directory inside the wizard's own home.

- The destination defaults to `/domains/<directory name>` and must be under `/domains`. If something is already published there by the same submitter, this is an update to it; if it belongs to someone else or to the base game, the request is refused.
- The request records the submitter, the source, the destination, the note, the time, and the list of files with a fingerprint of each file's content. What is approved is exactly what was submitted: if any file changes afterward, approval is refused until it is submitted again.
- Refused at once, with reasons, if the directory is empty, holds a file that does not compile in the sandbox, or defines a class name already used elsewhere in the mudlib (the whole-mudlib build needs unique names).
- Every online wizard of level 5 or higher is told, and others see a count of waiting requests when they log in.

### Reviewing

- `submissions` lists requests: one's own for any wizard, all of them for level 5 and up. `submission <number>` shows one in full, with its files and, for an update, which files are new, changed or removed.
- A reviewer reads the files with `cat` and walks the area with `goto`, in the workshop like anyone else.

### Deciding

- `approve <number>`: any one wizard of level 5 or higher, other than the submitter. A level 10 wizard may approve their own.
- `reject <number> <reason>`: the same people. The submitter is told the reason.
- `withdraw <number>`: the submitter.

### What approval does

1. Checks the fingerprints.
2. Copies the directory tree to the destination. In each copied file, every occurrence of the source path is rewritten to the destination path, so exits and spawns that pointed at `/wiz/<name>/<directory>/...` point at the published copy.
3. Compiles every copied file in the real game. If any fails, the whole copy is removed and the approval is refused with the errors; nothing is half-published.
4. Records who approved it and when, tells the submitter, and writes it to the driver log.

The published copy is ordinary game code under `/domains`: it follows the usual rule that level 5 is needed to change it. The original stays in the wizard's home, where they can keep working and submit an update.

Approval uses no new privilege. The approver's own character does the copying with the file commands a level 5 wizard already has.

### Connecting it to the world

A published area is reachable by `goto` at once, and by players only when some real room gains an exit to it. Adding that exit is an edit to a real file, so it is done by a level 5 wizard, separately and deliberately. `approve` reports the published entry rooms as a reminder.

### Records

Requests and their outcomes are kept permanently by a daemon under `/secure`, with its own save file. When the Chronicle exists, publication can be reported as an event in its "Player Created Content" category; that is left for the Chronicle's own pass.

## 5. What a wizard sees

- Entering: "You step into the workshop. Nothing that happens here will follow you out."
- Leaving: "You leave the workshop as you entered it."
- Trying to carry work out, drop real gear inside, or bring a player in: a one-line refusal saying which rule applies.
- `score` shows a workshop marker while it applies.

## 6. Testing

- A wizard enters their area, gains levels, gold, quest states and items from their own objects, dies once, and leaves by each route (walking out, `goto`, quit, dropped connection, driver restart, death): the character is identical to the checkpoint each time, and the save file never changed.
- An ordinary player cannot be brought into sandbox space by an exit, by `goto` used on them, or by sandbox code calling the move efun.
- Sandbox objects cannot be dropped, given, spawned or moved into the real game; real objects cannot be set down in the sandbox.
- From a wizard's own object, against an ordinary player and against a real creature elsewhere: gold, experience, quest, kill credit, damage, death, healing, effects, equipment, movement and text are each attempted and each refused, with the targets byte-for-byte unchanged afterward.
- The same attempts against a wizard who is in the workshop succeed, and are undone when they leave.
- No event, Renown, Chronicle entry or vault record results from anything done in the workshop, including levelling.
- Publishing: submit, the notices, review listing, approve by a second wizard, path rewriting, the published area loading and working for an ordinary player once connected, an update submission, rejection with a reason, withdrawal, refusal when a file changed after submission, refusal on a compile error with nothing left behind, refusal on a duplicate class name, refusal of self-approval below level 10, and a level 4 wizard unable to approve.
- An independent review, by someone who did not write the code, that tries to break the boundary.
