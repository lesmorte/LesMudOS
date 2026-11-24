# Second red-team review: working notes (keep current; sessions restart)

Branch work/redteam2 at 422e00d + these scripts. Run one at a time: `LESMUDOS_TEST_PORT=508x python3 tests/redteam2/<script>.py`
(boot ~15 s). Shared helper: rt2.py (imports tests/redteam/rtlib.py).

## Done (proof script, result)
- T1 t1_tickless_recursion.py: FAIL x4 (field init, auto-prop init, ctor initialiser, expression-bodied ctor recursion -> stack
  overflow, process exit -6, no quarantine). Root: TickInjector.VisitConstructorDeclaration skips expression bodies; nothing
  visits initialisers.

- W1 w1_blame_shift.py: FAIL. _lastRestricted set on Push, never restored on Pop: touch another home (LoadObject/Short), then stall;
  the other home (owner's) is quarantined, driver restarts, staller is never closed. Repeatable.
- C2 c2_compile_freeze.py: FAIL. submit compiles each file (<=200) one after another, each under the 30 s compile limit; 6 files of
  depth-11 nested overloaded lambdas froze everyone 79 s, no quarantine. Depth 12 ~30 s here (watchdog fires), 10 ~7 s.

- D2 d2_disk_and_checkpoint.py: FAIL x3. (a) no quota on home files/dirs (copy of a 90 KB file x40, mkdir x20 all fine);
  (b) EnterWorkshop catches only MudRuntimeException around SaveObject; an IOException (disk full, simulated by a directory at
  save/players/wren.json.tmp) leaves the wizard in sandbox space unmarked: walks out with a sandbox blade and kills a real
  creature (F3b again), gains real xp. Also the error text shows the host save path to the wizard.

- T2 t2_tick_exhaustion_checkpoint.py: FAIL x5. HIGH. Sandbox room Create burns k ticks in the entering goto's execution;
  EnterWorkshop then runs out: k=T-15 -> exception inside the caught save: stale checkpoint -> dropped real relic comes back on
  restore (duplicated, in both save files); k=T -> exception before inWorkshop=true: unmarked -> sandbox blade kills real dummy,
  real loot into wren's save. T found by bisection (~23 tries, one new file each).
- T3 t3_tick_exhaustion_exit.py: FAIL x3. Same on the way out: sandbox verb burns k then moves wizard to a real room; OnMoved
  throws before ScheduleRestore: marked body stays in real room with workshop gold + blade, never swept; kills a real creature.
  Held: the corpse/loot are marked sandbox (CallerChain rule) and vanish.

- L3 l3_log_flood.py: FAIL. Kernel.Write with no ThisPlayer logs the whole text, uncapped: 80 MB in 8 s from one item.
- S1 s1_visitor_mark_wrap.py: FAIL. Visitor mark is per \n line; 205 spaces push "Chief tells you: ..." onto its own unmarked
  80-column screen line.
- Read, not proven: lone CR is allowed by Publish.Unprintable (ends a // comment for Roslyn; ShowFile shows <U+000D>), but a wizard
  cannot type one (telnet drops bytes < 32 but tab), so not reachable for a stranger.

- N1 n1_telnet_iac_line.py: FAIL. IAC IAC appended to the line with no MaxLineLength check: anonymous +231 MB RSS for 200 MB sent.
- W2 w2_markup_blind_stall.py: FAIL. 0.2 MB of "{a" as a sandbox room's LongDesc; the body's Markup (IndexOf per '{') is quadratic
  with no sandbox frame on the stack: 45 s freeze, no quarantine (1.5 MB ran past 14 min). Repeatable with each look.

## State
All proofs done and committed; run_all.py runs them. The final report went to the caller as text (the file tool refused to write
REPORT.md from this agent); if a later session continues, write it from that message.
