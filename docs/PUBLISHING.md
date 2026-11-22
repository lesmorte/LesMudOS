# Publishing: the reviewer's guide

A wizard's home is a sandbox (SANDBOX.md): nothing built there touches the real game. Work reaches the game only when a wizard of level 5 or higher approves a request. This is what a reviewer sees and does; the builder's side is in BUILDING.md ("Publishing your work"), the design in SANDBOX.md section 4.

## The commands

| Command | Who | What it does |
|---|---|---|
| `submissions` | 1 | Waiting requests and your own (`submissions all`: the last 20 decided too). Level 5 and up see everyone's. |
| `submission <n>` | 1 (own), 5 (any) | One request in full: who, when, note, source, destination, every file with its SHA-256, and for an update which files are new, changed or removed. |
| `approve <n> [to /domains/<name>]` | 5 | Publish it. Never your own request, at any level. |
| `reject <n> <reason>` | 5 | Refuse it. The submitter is told the reason. Never your own. |
| `withdraw <n>` | the submitter | Take back a waiting request. |
| `revoke <n> [: <reason>]` | 10 | Remove a publication from the game (its files); objects already loaded run until destructed or the next restart. |

Wizards of level 5 and up who are online are told of every new request; every wizard sees the number waiting when they log in.

## How to review

1. `submission <n>`: read the list of files and the destination.
2. `cat` every file. You are approving source code that will run as trusted game code, with the same power as the base game's.
3. Walk the area in the submitter's workshop with `goto`. You are in the workshop like any wizard there: you are checkpointed, nothing happens to your character, and the submitter's code cannot act through you (it cannot make your character edit, promote, approve or shut anything down).
4. `approve <n>` or `reject <n> <reason>`.

What to look for in the code, because the checks below cannot see it:

- **Behaviour that depends on where the code lives.** Code can test its own path (`Self.ProgramPath`), the time, who is online, or a random roll, and behave well in the workshop and badly once published. Read every branch.
- **Paths built at run time.** Approval refuses any file that mentions `/wiz/`, but `"/wi" + "z/..."` is not caught. Any `LoadObject`, `CloneObject`, `FindObject` or `Call` whose path or name is not a plain literal deserves a question. Published code that loads a workshop object could change behaviour after approval, when the workshop changes.
- **Reach.** `Users()`, loops over other objects, gold, experience, quest states, items handed out, and anything that writes history. A published room is real: it can pay, kill and record like the base game.
- **Cost.** Heavy loops, many call-outs or heartbeats.

## What approval checks and does

All of this is done by `/secure/publish` for your command, in one step:

1. Every file is read once and its SHA-256 compared with the request: a changed, added or removed file refuses the approval ("changed since request #n was submitted"); the submitter must submit again. What you read with `cat` is what is published, provided nothing changed in between, and the hashes recorded prove it.
2. Every occurrence of the source path is rewritten to the destination, as a whole path (`/wiz/bob/glade` but not `/wiz/bob/glade2`). After the rewrite no file may mention `/wiz/`.
3. Class names must be unique in the mudlib.
4. The files are written, compiled and loaded (each one that is not abstract). If anything fails, everything written is removed (or, for an update, the files are put back as they were) and the approval is refused with the error: nothing is half-published. A file can compile differently after the move only if a real file it depends on changed since it was submitted; the compile then catches it.
5. The request is recorded as approved, with the approver, the time, and the SHA-256 of each file as published; the submitter is told; every file written is in the driver log (`file written: /domains/... by /secure/publish`).

Names are checked strictly: directories and files of lower-case ASCII letters, digits and `_` only (`.cs` files), the destination exactly `/domains/<name>`, no symbolic links, at most 200 files, no other spelling of an existing destination. That rules out `..`, hidden names, case tricks and Unicode look-alike letters.

The destination must be new, or a publication of the same submitter (then it is an update). The base game's directories and other wizards' publications are refused.

## After approval

The published copy is ordinary game code under `/domains`. From then on it changes only by an approved update or by a level 10 wizard. Players reach it once a real room has an exit to it, which is a level 10 change (approval lists the published rooms as a reminder). The submitter's original stays in their home.

## The record

`/secure/publish` keeps every request for good in its save file (`save/secure/publish.json`): submitter, source, destination, note, time, state (waiting, approved, rejected, withdrawn, replaced, superseded, revoked), the files with their hashes as submitted and as published, and who decided when and why. Nothing in the game can change a record except these commands, and only for the player whose own command is running; sandbox code is refused outright.
