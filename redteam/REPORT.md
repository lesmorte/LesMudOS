# Red-team review of the wizard sandbox and publishing (commit 986fe75)

Attacker: a level 1 wizard (Wren) writing C# in /wiz/wren. Each script in this directory prints FAIL where the attack works and exits 1; after a fix it must print only PASS. Run from the repository root after `dotnet build`, one at a time, e.g. `LESMUDOS_TEST_PORT=5041 python3 redteam/f2_exception_window.py`. Shared harness: rtlib.py (a level 10 wizard "chief", an ordinary player "pat" in the village square, the attacker "wren", a level 5 reviewer "rev").

| # | Severity | Finding | Script |
|---|---|---|---|
| C1 | critical | Any online wizard, level 10 included, is dragged into the workshop and shown a concealed fake password prompt; the typed answer goes to the attacker (F2 + P3b) | c1_chain_phish_admin.py |
| F2 | high | A framework exception (AggregateException) wrapping a sandbox exception is not converted by Kernel.In; Execute's finally clears the call stack and ThisPlayer, then ReportError reads error.Message, which runs the sandbox exception's Message getter with an empty stack: Restricted is false, Permit allows everything, ObjectDomain gives clones the real domain. Proven: +4242 gold, a quest, a real item cloned into an ordinary player's inventory (all saved), forged text, a level 10 wizard moved into the workshop. Works from a heartbeat or call-out with the wizard offline. | f2_exception_window.py |
| F3 | high | A sandbox item whose OnDestruct throws a non-mudlib exception stops LeaveWorkshop before Exec/SetupPlayer/Destruct: the old body keeps the connection with leavingWorkshop=true, inWorkshop=false, is treated as real, never restored. 100000 workshop gold spent in a real shop for an ordinary player; a sandbox blade kills a real creature and its loot reaches a player's save. | f3_restore_abort.py, f3b_sandbox_weapon_out.py |
| P1 | high | U+2028 (also U+2029, U+0085) ends a C# `//` comment; cat and the editor split only at \n; the in-game editor stores it. A line that reads as a comment holds live code. | p1_publish_review.py |
| P2 | high | Swap and swap back: approval compares disk with the hashes taken at submit, but nothing ties what the reviewer reads to those hashes. Harmless file while the reviewer looks; submitted bytes restored before approval. | p1_publish_review.py |
| P3a | high | `cat` (and list, ls, pwd, cd, help, look, tell, ...) is not a protected verb: sandbox code rebinds it on a visiting reviewer and shows a fake file. | p3_reviewer_in_workshop.py |
| P3b | high | Sandbox text passes raw ESC (conceal ESC[8m, clear screen, cursor moves, OSC); ordinary InputTo (echo on) is allowed on a visitor: a concealed prompt in effect. | p3_reviewer_in_workshop.py |
| F1a | medium | TickInjector does not add Tick and the stack check to expression-bodied lambdas, expression-bodied and conversion operators, or indexers: `f = x => f!(x + 1) + 1` overflows the stack and kills the driver process. | f1_dos.py crash |
| F1b | medium | Framework loops with expression-bodied callbacks or none run without ticks: `Enumerable.Range(0, 1_000_000_000).Count(x => x >= 0)` stalls every player; nested forms are effectively unbounded. | f1_dos.py hang |
| F5 | medium | Permit returns true whenever the target is restricted, in ANY domain: one wizard's code calls and moves another wizard's sandbox objects. | f5_cross_home.py |
| F4 | low | TryCross passes value tuples uncopied, so a list inside a ValueTuple is shared across domains (latent: no base-game function has that shape). | f4_tuple_cross.py |
| L1 | low, suspected | Publish.ClassNames sees only `class ` at column 0: records, structs, interfaces, enums, indented classes and `/**/class` are missed. Nothing is replaced at run time; `dotnet build` of the mudlib breaks on a duplicate. | none |
| L2 | low, suspected | /secure/doors State() creates a record for any key from a calling sandbox room: the real daemon grows without bound and is never cleaned. | none |

## Fixes the reviewer proposes (smallest that closes each)
- F2: in Kernel.In / In<T>, convert EVERY exception that crosses a domain boundary (`catch (Exception error) when (ob.Domain != below && error is not EvalCostExceededException)` -> `throw new MudRuntimeException(SafeMessage(error))`), reading the message in the catch block while the frame is still on the stack; the same in InstantiateCore's TargetInvocationException branch for sandbox objects; ReportError uses only MudRuntimeException.Message and type names, never a foreign exception's Message.
- C1: F2 and P3b, and MoveRefusal refuses a body's move into sandbox space unless the body moves itself by its own command (`caller == what && ThisPlayer() == what`).
- F3: the F2 change is not enough. Each Destruct(item) in LeaveWorkshop in its own catch, the old body's Destruct in a finally; better, destroy carried sandbox objects without running their code. The same for ReleaseInventory (Vanish) on quit and NetDead.
- P1: Submit and Approve refuse any file containing anything but \n, \t and printable characters (refuse U+0000-001F except tab and newline, U+007F-009F, U+2028, U+2029, U+200B-200F, U+202A-202E, U+2066-2069, U+FEFF); cat shows such characters visibly.
- P2: keep the submitted content in the register; `submission <n> cat <file>` reads that copy; approval publishes that copy.
- P3a: refuse to restricted code an AddAction for any verb the body binds itself, and refuse AddAction entirely on a visitor who is not the workshop's owner.
- P3b: while Restricted, strip C0 and C1 control characters except \n and \t in Write, TellObject, TellRoom and Say; refuse restricted InputTo on anyone but the workshop's owner.
- F1a: rewrite expression bodies into blocks with a Tick and stack check, as for methods.
- F1b: the F1a rewrite bounds every case with a mudlib callback; for the rest, a watchdog that logs the restricted caller and restarts the driver after a few seconds in one execution (.NET cannot abort a thread).
- F5: in Permit, also ask the master when the target's domain differs from the restricted code's domain; allow read-only functions only and refuse moves.
- F4: copy tuple elements through TryCross and rebuild, or refuse tuples whose elements are not scalars, strings or handles.
- L1: take declared type names from the compiler's syntax tree. L2: bound and reclaim door records made from sandbox rooms.

## Tried and held
Privileged efuns from sandbox code, even inside the F2 window (WriteFile, CheckAdminSecret, wizlist Promote refused by program identity and PreviousObject() == ThisPlayer()); 66 allow-list probes (reflection by every route tried, IO, Environment, AppDomain, threads, tasks, timers, unsafe, Marshal, module initialisers, finalizers, lock, goto, stackalloc, aliases, using static; dynamic, expression trees, Regex and Console do not compile); typed views from restricted code; RequireOwnCode for delegates; placing sandbox objects in the real game (only F3 gets them out); the read-only naming rule (a scan of every Query/Is/Has/Can function found only lazy clean-up); rule objects under /skills and /classes; the callable doors and sneak functions; the shared static tables; the Exec handover; the publish actor check; names, symlinks, destination spelling and the hash recheck; the telnet control-byte filter; GMCP limits; second login of an online character; a link-dead body in the workshop.

## Not tested
CLR or CoreLib escapes not thought of; the non-terminating F1b form; memory exhaustion; L1 and L2 (read only); how real clients render ESC[8m, U+2028 and bidi controls; the death and goto restore routes with the anchor item, and an anchor slipped into a reviewer's inventory; restart or shutdown while a body is half-restored; GMCP input fuzzing.

## Reviewer's judgement
No: until F2, F3 and the P-series are fixed, do not give a level 1 wizard account to anyone not fully trusted. The fixes are small and local. Even then the allow-list is a guard rail, not a process boundary, as DESIGN.md says.
