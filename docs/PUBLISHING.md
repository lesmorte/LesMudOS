# Publishing: the reviewer's guide

A wizard's home is a sandbox (SANDBOX.md): nothing built there touches the real game. Work reaches the game only when a wizard of level 5 or higher approves a request. This is what a reviewer sees and does; the builder's side is in BUILDING.md ("Publishing your work"), the design in SANDBOX.md section 4.

## The commands

| Command | Who | What it does |
|---|---|---|
| `submissions` | 1 | Waiting requests and your own (`submissions all`: the last 20 decided too). Level 5 and up see everyone's. |
| `submission <n>` | 1 (own), 5 (any) | One request in full: who, when, note, source, destination, every file with its SHA-256, and for an update which files are new, changed or removed. |
| `submission <n> cat <file>` | 1 (own), 5 (any) | The register's own copy of one file of a waiting request: numbered, printed exactly as stored, every character outside printable ASCII shown as `<U+XXXX>`, with its SHA-256. This is what approval publishes. |
| `approve <n> [to /domains/<name>]` | 5 | Publish it. Never your own request, at any level. |
| `reject <n> <reason>` | 5 | Refuse it. The submitter is told the reason. Never your own. |
| `withdraw <n>` | the submitter | Take back a waiting request. |
| `revoke <n> [: <reason>]` | 10 | Remove a publication from the game (its files); objects already loaded run until destructed or the next restart. |

Wizards of level 5 and up who are online are told of every new request; every wizard sees the number waiting when they log in.

## How to review

Review from outside the workshop. A workshop is its owner's ground: everything in it is their code, and what that code shows you there is theirs.

1. `submission <n>`: read the list of files and the destination.
2. `submission <n> cat <file>` for every file, from anywhere outside the workshop. That is the register's copy, taken when the request was made, and exactly what `approve` publishes, whatever has happened to the files in the home since. No wizard's code can change what this shows you. (`cat` of the files in the home shows what is there now, which may differ.) You are approving source code that will run as trusted game code, with the same power as the base game's.
3. If you want to see the area work, walk it in the submitter's workshop with `goto`. You are checkpointed like any wizard there, and nothing done to you lasts. The workshop's code can show you things, but every line of its text is marked `[<owner>'s workshop]`; it cannot ask for your input, answer your own commands (`look`, `cat`, `tell`, `score`, and every other command of yours) or act through you (edit, promote, approve, shut down). Treat what you see there as a demonstration, not as evidence: the code is the evidence.
4. `approve <n>` or `reject <n> <reason>`.

What to look for in the code, because the checks below cannot see it:

- **Behaviour that depends on where the code lives.** Code can test its own path (`Self.ProgramPath`), the time, who is online, or a random roll, and behave well in the workshop and badly once published. Read every branch.
- **Paths built at run time.** Approval refuses any file that mentions `/wiz/`, but `"/wi" + "z/..."` is not caught. Any `LoadObject`, `CloneObject`, `FindObject` or `Call` whose path or name is not a plain literal deserves a question. Published code that loads a workshop object could change behaviour after approval, when the workshop changes.
- **Reach.** `Users()`, loops over other objects, gold, experience, quest states, items handed out, and anything that writes history. A published room is real: it can pay, kill and record like the base game.
- **Cost.** Heavy loops, many call-outs or heartbeats.

## What approval checks and does

All of this is done by `/secure/publish` for your command, in one step:

1. The register's copy is taken, and each file's SHA-256 is compared with the request (a mismatch refuses the approval; the submitter must submit again), and every character checked again. The files in the home are not read: what you read with `submission <n> cat` is what is published, and the hashes recorded prove it.
2. Every occurrence of the source path is rewritten to the destination, as a whole path (`/wiz/bob/glade` but not `/wiz/bob/glade2`). After the rewrite no file may mention `/wiz/`.
3. Type names must be unique in the mudlib. A submission's names are over-counted (every word after a type keyword, comments and strings included), so a comment such as "this class Room" may need rewording.
4. The files are written, compiled and loaded (each one that is not abstract). If anything fails, everything written is removed (or, for an update, the files are put back as they were) and the approval is refused with the error: nothing is half-published. A file can compile differently after the move only if a real file it depends on changed since it was submitted; the compile then catches it.
5. The request is recorded as approved, with the approver, the time, and the SHA-256 of each file as published; the submitter is told; every file written is in the driver log (`file written: /domains/... by /secure/publish`).

Content is checked strictly: only printable characters, tabs and line ends (a file with U+2028, a bidirectional or zero-width character, or a control character is refused at submission, with its line and column), at most 1,000,000 characters a request. Names are checked strictly: directories and files of lower-case ASCII letters, digits and `_` only (`.cs` files), the destination exactly `/domains/<name>`, no symbolic links, at most 200 files, no other spelling of an existing destination. That rules out `..`, hidden names, case tricks and Unicode look-alike letters.

The destination must be new, or a publication of the same submitter (then it is an update). The base game's directories and other wizards' publications are refused.

## After approval

The published copy is ordinary game code under `/domains`. From then on it changes only by an approved update or by a level 10 wizard. Players reach it once a real room has an exit to it, which is a level 10 change (approval lists the published rooms as a reminder). The submitter's original stays in their home.

## The record

`/secure/publish` keeps every request for good in its save file (`save/secure/publish.json`): submitter, source, destination, note, time, state (waiting, approved, rejected, withdrawn, replaced, superseded, revoked), the files with their hashes as submitted and as published, and who decided when and why; while a request waits, also the copy of its files (dropped when it is decided). Nothing in the game can change a record except these commands, and only for the player whose own command is running; sandbox code is refused outright.

## Review by an independent party, November 2026

An independent review (SANDBOX.md section 8) got code past an honest reviewer in two ways and showed a reviewer fake output in a third; all three are closed. **P1:** a line that `cat` showed as a comment held live code after U+2028, which ends a `//` comment for the compiler only; such characters are now refused and shown visibly. **P2:** the submitter put a harmless version in place while the reviewer read it and the submitted one back before approval; the reviewer now reads, and approval publishes, the register's own copy. **P3:** a reviewer walking the workshop could be shown a fake `cat` and a concealed fake password prompt; a visitor's commands cannot be shadowed, a visitor's input cannot be taken, terminal controls are removed, and every line of a workshop's text to a visitor is marked as the workshop's.

## Second review, November 2026

What changed for a reviewer (SANDBOX.md section 9): `submit` compiles only files not already compiled from their present text, and for at most three seconds; a larger submission names the files still to compile (`update` them, then submit again). A carriage return is allowed only directly before a line feed. Approving runs each published file's `Create` on a budget of its own, so a file that spends its whole budget cannot leave a half-done rollback; each file of a request was compiled in the home under the compile limits. A workshop's text to a visitor is cut to fit the screen, every piece marked. Nothing about how to review has changed: read `submission N cat FILE` from outside the workshop.

