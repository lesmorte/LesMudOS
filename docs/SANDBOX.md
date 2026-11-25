# The wizard sandbox and publishing: implementation design

Every wizard is a game creator with a home directory, `/wiz/<name>/`, where they can write and run code from wizard level 1 (see DRIVER_API.md section 6). This document makes that home a sandbox, and adds the way finished work moves into the real game.

Decided by the owner:

- A wizard's home is a sandbox. What is built there cannot affect the real game.
- Any wizard, level 1 and up, may write and run code in their own home.
- A wizard asks for their work to be published. Any single wizard of level 5 or higher can approve it, and on approval it moves to the real environment.
- Wizards of any level make no history: no Renown, no Chronicle entries or firsts, whatever their level and wherever they are.
- The admin secret lives only in the environment variable `LESMUDOS_ADMIN_SECRET` and is never readable from game code.

Status: built. The wizard's view is in BUILDING.md ("Your workshop" and "Publishing your work"), the reviewer's in PUBLISHING.md, the driver pieces in DRIVER_API.md. The red-team suite is `tests/sandbox_test.py`, with the independent review's proofs in `tests/redteam/` (section 8).

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

When a wizard steps from the real game into sandbox space, their character is checkpointed, by the master, in the move itself: before the move happens and before any code of the workshop or of the body runs. If the checkpoint cannot be written, for any reason, the wizard does not enter. The character is saved exactly as at logout (with the real room they came from as the place they would log in again), and marked as being in the workshop. A marked character that arrives anywhere outside sandbox space has its restore scheduled by the master in the same move, whatever its own code does next. While the mark is set:

- The character is not saved. Level, experience, gold, quests, class, skill ranks, unlock counters, proficiencies and recipes, attributes, effects and inventory can all change freely, and none of it is written anywhere. The master refuses a write of the save file while the mark is set, as a second lock.
- Nothing the character does is history: the event service already refuses every wizard (section 3, "The game's systems"), and the daemons that keep their own records refuse a character in the workshop and every sandbox object.
- A death leaves no corpse and no lost-property record.
- Whatever is cloned for the character (the `clone` command, crafting, gathering) is a sandbox object.

When the wizard leaves sandbox space, by walking or `goto` into a real room, by quitting, by losing their connection, by dying, or by a driver restart, the character is restored from the checkpoint exactly as at login:

- Walking out, `goto` and death: a fresh body is made from the save file, the connection is handed to it, the old body and everything it carried is destroyed, and the checkpointed inventory is cloned again. The fresh body stands where the wizard went (after a death: in the room they entered the workshop from). The restore runs as soon as the move that left the workshop has finished, before any other command is read.
- Quitting and losing the connection: the save file already holds the checkpoint, and the body is destroyed as usual without being saved.
- A driver restart: the same; nothing was ever saved from the workshop.

They are told in one line that the workshop's changes were left behind.

### Leaving the workshop cannot be stopped

Every route out is a step that must complete, so no sandbox code runs in it. Real code that destructs a sandbox object does not run that object's `OnDestruct`, and a container destructed by real code destroys its sandbox contents instead of letting them fall out (which would run their `OnMoved` and `Init`). The restore destroys what was carried that way, and whatever goes wrong, it destructs the old body in the end: it can never stay in the game as a real character with what the workshop gave it. Quitting and losing the connection destruct sandbox things directly (never through their `Vanish`), and destruct the body whatever happened before. A death in the workshop schedules the restore before anything else (sandbox enemies forgetting the fight cannot stop it). As a last line, the master keeps an invariant every heartbeat, apart from any body's own code: a character in sandbox space is marked (if not, what the workshop made goes and they are moved out); a marked character outside sandbox space is restored; an unmarked character carries nothing made in a workshop. Each repair is logged with a `sandbox:` line. A body never records a sandbox thing in its save file.

**Budgets.** Each side of a domain boundary runs on its own tick budget: code entered across a boundary (sandbox code called by real code, real code called by sandbox code, a blueprint being made, the master answering) starts at zero ticks, and the caller's count is put back when it returns. So sandbox code can never spend the ticks a real sequence needs to finish: a checkpoint, a restore, an approval and its rollback each run to the end. The budget of one entry is the usual 5,000,000 ticks; what one execution can do in all is bounded in time, not in ticks, by the home's share and the watchdog (section 7). The save file holds the checkpoint from the moment of entering, so a shutdown, a crash or a kill at any moment, the moment of leaving included, also comes back as the checkpoint. So a wizard can give themselves a sword that kills in one blow, a thousand levels and a fortune inside their own area, test with them, and walk out as the character who walked in.

### What sandbox code may touch

The driver keeps a stack of the objects whose code is running, the one `PreviousObject()` reads. While a restricted object is anywhere on that stack, the execution is *restricted*, whoever's code is running at the top: sandbox code that calls a real object's method, and the real code that method runs, are both restricted until they return. A delayed call or heartbeat of a sandbox object starts restricted, because its owner is the bottom frame, and a delayed call can only run code of its owner's own class (the driver checks), so a sandbox object cannot hand a delegate to a real object to run later.

A restricted execution may do these to real objects, and nothing else:

- call the read-only functions: names beginning `Query`, `Is`, `Has` or `Can`, and `Short`, `Long`, `LongFor`, `Id`, `HealthLine`, `Stat`, `Appraisal`, `DescribeCount` (every such public function in `/std`, `/secure`, `/domains`, `/classes`, `/skills` and `/events` is read-only, and that is the rule for new code);
- call anything on the rule objects that hold no state: the skill, trait and class blueprints under `/skills` and `/classes`, and the class registry;
- call the few daemon functions that know about sandbox space (the door register; the notice dice of sneaking);
- call anything on the wizard in their own workshop, or on what they carry, because all of it is thrown away on leaving (but never a hidden-input prompt or a protected verb, below);
- of a *visitor* in its workshop (another wizard, a reviewer, a level 10 who walked in): read, walk them through an exit as `/std/room` does, offer verbs they do not bind themselves, and show them text, which the driver marks (below). Never their input, never their own verbs, nothing else.

Another wizard's sandbox is not sandbox code's own: it may call only the read-only functions of another home's objects, and can move neither them nor the wizards in another workshop. A home's code may build (`@inherit`, `@use`) only on real programs and on its own home's; real code never builds on a home's.

Everything else is refused silently (the call returns nothing) and logged, at most a few lines per execution.

What crosses between a sandbox object and a real one, in either direction, is plain data only: numbers, strings, object handles, and fresh copies of arrays, lists, sets and dictionaries of those (a dictionary always with the default comparer). Anything else (an object of a class written in a home, whose `ToString` or `Equals` would run later in the other side's frame; an enumerator; a delegate) does not cross: an argument like that refuses the call, a result like that arrives as null. An exception that leaves a frame of another domain, of any type (a wrapper such as `AggregateException` included), reaches the other side as a plain error with its message, read while the frame it left is still on top of the stack; a message from sandbox code becomes one line without control characters, at most 300 characters. Strings returned from sandbox code to real code lose every terminal control and invisible character. Typed views (`As<T>()`) never cross at all, in either direction. So sandbox code can never run in a real object's frame, and real code can never be handed a reference into a real object's state.

**Sandbox code runs only in its own frames.** As a second wall, a home's code is compiled with a tick of its own: the first statement of every method, accessor, operator, constructor, local function and lambda checks that an object of its own domain is on top of the call stack, and stops there if not. A `Message`, `ToString`, `Equals` or enumerator of a home's class that real code reached somehow, a delegate kept and run later, would stop at its first statement. A catch filter is checked the same way, but may also run while the frame the exception came out of is still on top, provided its home is the sandbox code acting and the master is not answering a question. Query syntax (`from ... select`) is refused, because its clauses become functions with no body to check.

**Where code can run.** Every syntactic place C# runs code, and what the injector does there (`tests/redteam/t1_every_body.py` makes each recurse without end):

| Place | Treatment |
|---|---|
| Method, local function, accessor (get, set, init, add, remove), operator, conversion, constructor (instance and static), indexer, lambda, anonymous method; block or expression body | the tick first |
| Iterator bodies, switch expressions, patterns, `using`, `foreach` over a home's enumerator | inside a body, or a method of their own: ticked |
| Loop bodies (`while`, `do`, `for`, `foreach`) | the tick first |
| Field, event-field and property initialisers (instance and static), struct field initialisers | `Runtime.Pass() ? (expr) : default!`: the tick before the expression |
| Arguments of `: base(...)`, `: this(...)` and of a primary constructor's base type | the same, on the first argument passed by value |
| Catch filters | the frame check (`Runtime.Filter`) |
| Records: generated `ToString`, `Equals`, `GetHashCode`, `with` | no code of the author's runs except through members above |
| Constants, enum values, attribute arguments, default parameter values | evaluated by the compiler, no code at run time |
| Finalizers, query syntax, `goto`, `unsafe`, `stackalloc`, `async`/`await`, `lock` | refused by the analyzer |
| Module initialisers, collection builders, interceptors | need attributes from namespaces the analyzer refuses |

Sandbox code may use at most the first 4 MB of the game thread's 16 MB stack (checked in its tick by stack address), so whatever runs below it without a tick still has the rest. Sandbox code that the master calls while it answers a question is restricted all the same. `As<T>()` from a restricted execution returns null for any real object, so direct typed calls are impossible. The guard is one check in the driver's `Call` path, decided by the master's `ValidCall`, so it covers every object type (players, creatures, rooms, items, daemons) and every public method, including ones written later; there is no list of mutators to keep in step.

So a wizard's object cannot kill the Goblin Overseer from afar, hand a friend gold through `Users()`, set a quest, give experience or kill credit, start a fight, equip, add an effect, move, rest or wake anyone, or reach a daemon to write history, through any route: directly, from a creature, an item, a room, a delayed call, a heartbeat, an `Init` hook, or from inherited `/std` code running in a sandbox object.

### Text from the sandbox

`Write`, `TellObject`, `TellRoom`, `Say` and `NotifyFail` from a restricted execution reach only sandbox objects and characters in the workshop, and always lose every terminal control first: ESC (so no concealed text, cleared screen, moved cursor or terminal title), the other C0 and C1 controls but tab and newline, the Unicode line and paragraph separators, the zero-width characters, the bidirectional controls and the byte-order mark. Sandbox code cannot print a convincing lie on an ordinary player's screen.

- To the wizard in their own workshop the text goes as it is (cleaned).
- To a visitor every line is marked `[<owner>'s workshop] `, so it can never pass for the game's own output or a prompt: runs of spaces and tabs become one space, and a line too long for an 80-column screen with its mark is cut into pieces, each marked. Entering another wizard's workshop says so: whose code it is, that its text is marked, that it cannot ask for input or answer the visitor's own commands, and that a submission is reviewed from outside.
- To anyone else: nothing.

Sandbox code cannot bind verbs (`AddAction`) on, or capture the next line (`InputTo`) of, a character who is not in the workshop, nor take a visitor's next line, nor shadow a verb a visitor's body binds itself (`look`, `cat`, `tell`, `score`, every command of the body). Even in their own workshop it can never ask a wizard for a hidden line (a password prompt), and cannot take over the commands that protect a character or decide for the real game (`password`, `quit`, `save`, `goto`, the file and publishing commands, `promote`, `update`, `shutdown`).

No code but a character's own command (`goto`, an exit) takes them into a workshop: nothing can carry a wizard in, the level 10 included.

### What can cross the boundary

| Mover | Into sandbox space | Out of sandbox space |
|---|---|---|
| A wizard | Yes; checkpoint | Yes; restore |
| An ordinary player | No | n/a |
| A real creature or NPC | No | n/a |
| A sandbox object (item, creature) | Yes | No. It cannot be placed in a real room, in a real container, or in the hands of anyone not in the workshop. |
| A real object (a sword bought in the village) | Only carried by a wizard; it cannot be dropped, given or put anywhere in sandbox space | It leaves with the restore, as it was at the checkpoint |

Because real objects cannot be set down in sandbox space and everything carried is rebuilt from the checkpoint on leaving, nothing real can be lost, duplicated or altered there. Nothing a character in the workshop carries can be moved into the real game at all, by anyone, so nothing can be passed out in the moment between leaving and the restore.

Sandbox code may move only its own home's objects and the characters in its own workshop, and what the wizard in their own workshop carries, and only within its own workshop (a wizard also out into a room). It cannot move, summon or teleport anything else, cannot put things into another wizard's workshop, and cannot pull a real thing into the workshop.

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
| Doors and their shared state | A door's state lives in `/secure/doors`, and only a room that is one end of a door may change it. A sandbox room is therefore one end of every door it can change: a door between two of the game's rooms cannot be reached from a workshop. Nothing there is saved. The register keeps at most 200 doors asked about by one home's rooms, the oldest forgotten first. |
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
- The request records the submitter, the source, the destination, the note, the time, the list of files with the SHA-256 of each file's content, and a copy of every file. That copy is what a reviewer reads and what approval publishes: the files in the home may change afterwards, and it makes no difference to the request. A request holds at most 1,000,000 characters, and the register holds at most 4,000,000 for all waiting requests together; the copy is dropped when the request is decided (the hashes stay).
- Every file must hold only printable characters, tabs and line ends (a carriage return only directly before a line feed: alone it ends a `//` comment). A character that a reader cannot see, or that the compiler reads differently from a reader (U+2028 ends a `//` comment for the compiler but not on the screen; the bidirectional controls reorder what is shown; the zero-width characters and controls hide things), refuses the request with its line and column.
- Refused at once, with reasons, if the directory is empty, holds a file that does not compile in the sandbox, names a path in any wizard's home other than the submitted directory, or may define a type name already used elsewhere in the mudlib (the whole-mudlib build needs unique names). A submission's names are over-counted on purpose: every word after `class`, `struct`, `interface`, `enum`, `record` or `delegate`, anywhere in the text, comments and strings included, `\uXXXX` escapes decoded, so that no form of declaration is missed.
- Every online wizard of level 5 or higher is told, and others see a count of waiting requests when they log in.

### Reviewing

- `submissions` lists requests: one's own for any wizard, all of them for level 5 and up. `submission <number>` shows one in full, with its files and hashes and, for an update, which files are new, changed or removed.
- A reviewer reads the files with `submission <number> cat <file>`: the register's own copy, numbered, every character outside printable ASCII shown as `<U+XXXX>`, printed exactly as it is (no colour markup is read in it), with its SHA-256. That is exactly what approval publishes, shown by a route sandbox code cannot touch: the register and the body's own command, outside any workshop. Review is done from outside; a workshop is its owner's ground (PUBLISHING.md).

### Deciding

- `approve <number> [to <destination>]`: any one wizard of level 5 or higher, other than the submitter. Nobody approves their own request, at any level.
- `reject <number> <reason>`: the same people. The submitter is told the reason.
- `withdraw <number>`: the submitter.
- `revoke <number> [: <reason>]`: level 10 only, for a request that was approved. The published files are removed; objects already loaded from them keep running until they are destructed or the driver restarts.

### What approval does

1. Takes the register's copy and checks each file's SHA-256 against the request, and again that every character is printable. The files in the home are not read again.
2. Writes the directory tree to the destination from exactly that copy. In each file, every occurrence of the source path is rewritten to the destination path, so exits and spawns that pointed at `/wiz/<name>/<directory>/...` point at the published copy. After the rewrite no file may mention `/wiz/`.
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
- Publishing: submit, the notices, review listing, reading the register's copy, approve by a second wizard, path rewriting, the published area loading and working as real game code, a change made after submission not published, an update submission, rejection with a reason, withdrawal, refusal on a compile error with nothing left behind, refusal on a duplicate class name, refusal of self-approval, a level 4 wizard unable to approve, revocation by level 10.
- An independent review, by someone who did not write the code, that tries to break the boundary: section 8, and its proofs in `tests/redteam/` (`python3 tests/redteam/run_all.py`), which run in the suite from now on.

## 7. Limits

- The sandbox is a boundary for game state, not for the machine. Sandbox code still runs inside the driver process with only the compile-time allow-list (DESIGN.md section 8) between it and the host. Promotion to wizard remains a decision about trusting someone with the machine.
- **A stall.** Sandbox code's own loops and bodies tick, so its own work is bounded by the tick budget, but framework code it calls is not (`Enumerable.Range(0, 100000).SelectMany(_ => list).LongCount()` runs ten billion steps on a hundred thousand ticks), and .NET cannot stop a running thread. Time is charged to the first home whose code an execution entered, for the whole execution (compiling left out), so real code working on what a home returned (rendering its text, sorting its lists) is that home's time. Each home may use at most half a second of every two, and all homes together at most one second of every two; past that, entering a home's code is refused until the window ends. And a watchdog stops the driver if one step of the game loop runs longer than the stall limit (default 5 seconds; `LESMUDOS_SANDBOX_STALL`, DEPLOY.md) in an execution that entered sandbox code at all: it writes the first home entered into `save/sandbox_quarantine.txt`, exits with code 3, and the service restarts it. The restarted driver makes no object of that home until an administrator deletes its line. A compile of a home's program has the same limit, and a home may compile for at most 15 seconds a minute; `submit` compiles only the files not already compiled from their present text, for at most three seconds, and names the rest. Compiling off the game thread was considered and not done: the call that needs the program is waiting for it in the same execution, so the game would wait just the same. What remains: once per home, every player waits up to the stall limit, everyone is disconnected, and what was not yet saved is lost (characters are saved at their usual moments). Memory is bounded only in part: a home may hold at most 2000 objects and 1000 waiting call-outs; what its code hands to real code is cut at 16,384 characters a string and 10,000 elements a value; text it sends is cut at 16,384 characters; a connection that does not read is closed past 4,000,000 queued bytes. One statement can still allocate a very large array or string inside its own code: the third review (November 2026, M1) measured a level 1 wizard's item allocating a 100 MB string per command and keeping it, more than 1 GB in 2.5 seconds. Its judgement, which this document shares: an in-process sandbox cannot be made safe for strangers; that would need an operating-system process boundary with a memory limit of its own. What the deployment does instead: the service has a memory ceiling (`MemoryMax=768M`, no swap) and .NET a heap limit below it (`DOTNET_GCHeapHardLimitPercent`, 70% of the ceiling), so an allocation past the limit fails as an error in the statement that made it rather than the machine or the process dying, and a kill by the ceiling restarts only the driver (`Restart=on-failure`; the in-game `shutdown` still stops it for good). Past three quarters of the heap limit the driver saves every player, once a minute at most, and logs `memory: ... saving every player` (`tests/redteam/m1_low_memory.py`). What this buys: the droplet survives, and players lose little. What it does not: the memory a home's code keeps stays kept until a restart, everyone's allocations fail meanwhile, and nothing names the home that did it (memory is not attributed); an administrator reads the log and the home's code.
- **Disk and log.** A home holds at most 3,000,000 bytes, 1000 files, 64 directories, 8 deep (driver settings `LESMUDOS_HOME_BYTES`, `_FILES`, `_DIRS`, `_DEPTH`). Every log line a home's code can cause (its text with nobody to read it, its errors, refusals, the file commands in its home) counts against it: 30 lines a minute, 500 characters each, then one note.
- A sandbox room can destroy itself with a visiting wizard inside, which disconnects them (their save file is the checkpoint, so nothing is lost). It cannot do this to anyone outside the workshop.
- Approval is a code review. Code can behave differently depending on where it lives (its own path, the time, who is online); the reviewer must read it. The record of hashes proves what was approved.

## 8. Review by an independent party, November 2026: what was found and how it was closed

A reviewer who had not written the code attacked commit 986fe75 as a level 1 wizard, with scripts that print FAIL where an attack works (`tests/redteam/REPORT.md`). Thirteen findings; each is closed by class, not by script, and each script now prints only PASS.

| # | Found | Closed by |
|---|---|---|
| F2 | A framework exception (`AggregateException`) wrapping a sandbox exception left the sandbox frame unconverted; the driver read its `Message` after the call stack was cleared, so the sandbox getter ran unrestricted (gold, quests, items, text, a level 10 dragged into a workshop). | Every exception that leaves a frame of another domain is converted, of any type, read while that frame is on top; sandbox code checks its own frame at its first statement and in catch filters ("Sandbox code runs only in its own frames"); sandbox code called while the master answers is restricted; the error reporter trusts no exception's getters. |
| C1 | F2 plus raw escape codes: any wizard dragged in and shown a concealed fake password prompt. | F2, the text rules, and: only a character's own command takes them into a workshop. |
| F3 | A sandbox item whose `OnDestruct` threw stopped the restore half-way: the old body stayed in the real game with the workshop's gold and a sandbox weapon. | "Leaving the workshop cannot be stopped": no sandbox code runs in any route out; the old body is destructed whatever happens; quit, link death, death and destructs of containers too. |
| P1 | U+2028 ends a `//` comment for the compiler but not in `cat`: live code passed as a comment. | Submission and approval refuse every character that is not printable; `cat` shows such characters as `<U+XXXX>`. |
| P2 | Swap and swap back: what the reviewer read was not tied to what was approved. | The register keeps a copy; the reviewer reads that copy (`submission N cat FILE`), and approval publishes it. |
| P3a | `cat` (and every other unprotected verb) could be rebound on a visiting reviewer to show a fake file. | A visitor's own verbs cannot be shadowed; a visitor is reachable only by reading, walking an exit and marked text. |
| P3b | Raw ESC passed in sandbox text; an ordinary `InputTo` on a visitor took the next line. | All terminal controls and invisible characters are removed from sandbox text and from strings sandbox code returns; text to a visitor is marked; a visitor's input cannot be taken. |
| F1a | Expression-bodied lambdas, operators, conversions and indexers had no tick: a recursion overflowed the stack and killed the process. | They are rewritten into blocks with the tick (a void delegate's lambda without the return); query syntax is refused. |
| F1b | Framework loops with no mudlib callback stalled every player. | The share of time per home and the stall watchdog with quarantine (section 7, "A stall"). A watchdog, not a narrower allow-list: no allow-list of framework calls bounds their nesting. |
| F5 | One wizard's sandbox code called and moved another's. | Sandbox code reaches freely only its own home's objects; another home's answer read-only calls only, and nothing moves between homes; no code builds on another home's. |
| F4 | A list inside a value tuple crossed uncopied. | Tuples and nullables cross only with scalar, string, handle or enum elements. |
| L1 | (suspected) The duplicate class-name check saw only `class ` at column 0. | Confirmed by reading. Every type keyword anywhere in a submission counts, escapes decoded. |
| L2 | (suspected) The door register grew without bound from sandbox rooms. | Confirmed by reading. At most 200 doors per home, the oldest forgotten. |

The review's "Not tested" list is now tested where it can be: the death, `goto`, quit and link-death routes with a throwing item, such an item slipped into a reviewer's inventory, a shutdown in the workshop and a kill at the moment of leaving (`f3c_restore_routes.py`), the nested F1b form (`f1c_stall.py`). Still not tested: escapes from the CLR or CoreLib not thought of, memory exhaustion, how real clients render what is now removed, GMCP input fuzzing.

## 9. Second review, November 2026

A second independent review attacked commit 422e00d and found that the first fixes had closed instances of two classes, not the classes. Both classes are now closed as classes, and each of its ten proofs (`tests/redteam2/`, `run_all.py`) prints only PASS.

**Class A: real multi-step code ran on whatever tick budget sandbox code left, and the checkpoint and restore could stop half-way.** A sandbox room's `Create`, loaded by the wizard's own `goto`, spent all but a few ticks, and the checkpoint then died part-way: the wizard stood in the workshop unmarked (and walked out with a sandbox weapon), or kept a stale checkpoint (and a dropped relic came back, duplicated) (T2). The same on the way out (T3), and with a failing disk (D2b, which also showed the host's path). Closed by: a budget of its own on each side of every domain boundary, for every blueprint and for every answer of the master; the checkpoint and the mark taken by the master in the move, before anything else runs, with the entry refused on any failure; the restore scheduled by the master whenever a marked body arrives outside; the heartbeat invariant; host paths scrubbed from every error a player sees. Every multi-step real sequence sandbox code can reach, and why it now finishes: the checkpoint (the master's own budget, any exception refuses the move); the restore (a call-out of its own, no sandbox code runs in it, the old body destructed in a `finally`); quit and link death (an execution of their own, sandbox things destructed without their code, the body destructed in a `finally`); death in the workshop (entered across the boundary on its own budget, the restore scheduled first); approval and its rollback (each published blueprint made on its own budget, so its `Create` cannot spend the approver's; every exception type caught; each file was compiled in the home under the compile limits, so its copy compiles in about the same time); `update` of a room with people in it (the marked stay marked through the Void).

**Class B: the watchdog and the time share saw a stall only while a sandbox frame was on the stack, and could name the wrong home.** Touching another home's object before stalling closed that home instead (W1); a 0.2 MB description of `{a` made the body's colour renderer quadratic with no sandbox frame on the stack, 45 s (W2); `submit` compiled 200 files one after another, each under the per-compile limit (C2). Closed by: charging an execution and a stall to the first home it entered; a linear renderer; caps on what crosses to real code; a compile limit per file, a compile budget per home per minute, `submit` compiling for three seconds at most; a global share; objects and call-outs per home; a linear destroy.

**Individual findings.** T1 (code with no tick: expression-bodied constructors, initialisers, constructor initialisers; process killed) closed as a class with the table above and the stack limit; D2a (no quota) by the home quota; L3 (the log flooded at 10 MB/s) by the per-home log budget; S1 (padding pushed text onto an unmarked screen line) by the visitor wrapping; a lone carriage return refused in submissions; N1 (an escaped telnet byte bypassed the line limit) and an unread connection's queue, bounded.

What remains: a home can still make the game wait up to the stall limit once (then it is closed); one statement can still allocate memory without limit inside sandbox code; the real code of an approved publication runs with the base game's power, as the reviewer decided.

