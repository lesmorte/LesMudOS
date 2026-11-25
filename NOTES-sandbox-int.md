# NOTES sandbox-int (untracked)
Merge commit c99c7ad (master.cs: both preloads + roomids&publish save files; Kernel.cs: partial class + RefusedException).
GMCP items: MaySendGmcp Domain=="" + Permit(player,"GmcpSend"); gmcp FromBody/FromComm Domain=="" & !restricted;
roomids Assign(path, temporaryOnly) refuses restricted/sandbox callers, /wiz case-insensitive temporary; gmcp passes sandbox flag
for a sandbox room and its exits. Workshop map: via the body's Poll after each prompt/heartbeat (unrestricted); Look from sandbox
exits is refused by the call guard.
Runs:
- sandbox_test 918 PASS 0 failure(s); gmcp_test 101 checks, 0 failure(s)
- smoke_test exit=0 secs=211 last: 0 failure(s)
- area_test exit=1 secs=572 last: 1 failure(s)
- class_test exit=0 secs=447 last: 0 failure(s)
- item_test exit=0 secs=77 last: 0 failure(s)
- world_test exit=0 secs=262 last: 0 failure(s)
- find_test exit=0 secs=75 last: 159 checks, 0 failure(s)
- chronicle_test exit=0 secs=60 last: 0 failure(s)
- chronicle_play_test exit=0 secs=82 last: 0 failure(s)
- craft_test exit=0 secs=342 last: 313 checks, 0 failure(s)
- craft_content_test exit=0 secs=233 last: 289 checks, 0 failure(s)
- comm_test exit=0 secs=166 last: 221 checks, 0 failure(s)
- playfeel_test exit=0 secs=238 last: 0 failure(s)
- attribute_test exit=0 secs=302 last: 0 failure(s)
- rest_test exit=0 secs=193 last: 0 failure(s)
- rename_test exit=0 secs=115 last: 0 failure(s)
- (session 3) summary3: smoke..rename all passed, area 1 fail (rat corpse timing, rerunning); sandbox_test 918/0 and gmcp_test 101/0 passed on c099929 (986fe75 docs only)
- area_test exit=0 secs=559 last: 0 failure(s)
- rank_test exit=0 secs=646 last: 0 failure(s)
- aim_test exit=0 secs=380 last: 0 failure(s)
- sneak_test exit=0 secs=355 last: 0 failure(s)
- story_test exit=0 secs=174 last: 544 checks, 0 failure(s)
- area_test rerun 0 failure(s) (flake confirmed); rank, aim, sneak, story ok; hollow_expansion 673/0; run stopped for redteam findings

## Red-team fixes (November 2026 review), in progress
- 54a7659: F2, C1, F3, F4, F5, P1, P2, P3a, P3b, F1a closed. Red-team scripts now PASS: f2, c1, f3, f3b, f4, f5, p1, p3, f1 (port 5003).
  Three script checks were measuring the old behaviour and were corrected (f3 purchase with real gold; p3 header; p1 review route).
- Next: F1b watchdog + per-domain time budget + quarantine; L2 doors; a1 run; sandbox_test update; new proofs; docs; full run.
- cc7ccbb F1b (share + watchdog + quarantine; also carries the git mv redteam -> tests/redteam), be5acf8 L2, 6760ff1 visitor exits +
  hostile-ground notice + throw-expression fix + new proofs (f3c, l1_l2_and_rules, run_all) + sandbox_test P2 update.
- Proofs all PASS individually on port 5003: f2, c1, f3, f3b, f3c, f4, f5, p1, p3, f1, f1c, l1_l2_and_rules, a1 (52 PASS).
- sandbox_test first run after 54a7659: 912 PASS, 6 FAIL, all the P2 publish checks (expected; rewritten in 6760ff1).

## Full run after the red-team fixes (tree after the compile-stall commit), one suite at a time, port 5005
- build exit=0 secs=3 last: Build succeeded.
- build exit=0 secs=2 last: Build succeeded.
- load_check exit=0 secs=227 last: 0 file(s) failed to load
- sandbox_test exit=0 secs=188 last: 0 failure(s)
- redteam/run_all exit=0 secs=481 last: 13 of 13 proofs pass
- gmcp_test exit=0 secs=68 last: 101 checks, 0 failure(s)
- smoke_test exit=0 secs=212 last: 0 failure(s)
- area_test exit=0 secs=525 last: 0 failure(s)
- class_test exit=0 secs=451 last: 0 failure(s)
- item_test exit=0 secs=85 last: 0 failure(s)
- world_test exit=0 secs=266 last: 0 failure(s)
- find_test exit=0 secs=84 last: 159 checks, 0 failure(s)
- chronicle_test exit=0 secs=60 last: 0 failure(s)
- chronicle_play_test exit=0 secs=81 last: 0 failure(s)
- craft_test exit=0 secs=348 last: 313 checks, 0 failure(s)
- craft_content_test exit=0 secs=232 last: 289 checks, 0 failure(s)
- comm_test exit=0 secs=168 last: 221 checks, 0 failure(s)
- playfeel_test exit=0 secs=251 last: 0 failure(s)
- attribute_test exit=0 secs=301 last: 0 failure(s)
- rest_test exit=1 secs=202 last: 1 failure(s)
- rename_test exit=0 secs=117 last: 0 failure(s)
- rank_test exit=0 secs=657 last: 0 failure(s)
- aim_test exit=0 secs=379 last: 0 failure(s)
- sneak_test exit=1 secs=364 last: 1 failure(s)
- story_test exit=0 secs=178 last: 544 checks, 0 failure(s)
- rest_test exit=0 secs=195 last: 0 failure(s)
- sneak_test exit=0 secs=364 last: 0 failure(s)
- hollow_expansion_test exit=0 secs=52 last: 673 checks, 0 failure(s)
- corridor_mob_test exit=0 secs=245 last: 0 failure(s)
- ansi_test exit=0 secs=111 last: 0 failure(s)

## State after the session restart (written first, as asked)
- All 13 findings closed and committed: 54a7659 (F2, C1, F3, F4, F5, P1, P2, P3a, P3b, F1a, L1), cc7ccbb + 30dd846 (F1b),
  be5acf8 (L2), 6760ff1 (visitor exits, hostile-ground notice, new proofs, tests/redteam), 326e5f3 (docs). Nothing in progress.
- Suites passed on tree 30dd846 (list above): build, load_check, sandbox_test (921), redteam/run_all 13/13, gmcp 101, smoke, area,
  class, item, world, find, chronicle, chronicle_play, craft, craft_content, comm, playfeel, attribute, rest (rerun; first run one
  timing flake), rename, rank, aim, sneak (rerun; first run one patrol-timing flake), story, hollow_expansion, corridor_mob, ansi.
- Killed by the restart, still to run: timing, city, corridor, then playthrough last.
- build exit=0 secs=24 last: Build succeeded.
- timing_test exit=0 secs=552 last: 0 failure(s)
- city_test exit=0 secs=634 last: 4685 checks, 4685 passed, 0 failed (wolves killed: 0; 634 s in the game)
- corridor_test exit=0 secs=1300 last: 6861 checks, 6861 passed, 0 failed (1299 s in the game)
- playthrough_test exit=1 secs=1568 last: 1 failure(s)
- playthrough_test exit=0 secs=1516 last: 0 failure(s)
- playthrough first run: 1 failure (the Overseer died from above half Vitality before his next action, so no summon: combat chance,
  /domains code untouched); rerun: 0 failures, 1516 s. Full run complete on tree 30dd846/ffabb42: every suite passes.

## Second independent review (tree 422e00d): findings to close (written first; proofs in tests/redteam2, from work/redteam2)
CLASS A (real multi-step code on the budget sandbox code left; checkpoint/restore not atomic): T2 (goto loads a sandbox room whose
Create burns ticks; EnterWorkshop dies half-way: unmarked, or stale checkpoint + duplicated relic), T3 (burn then move out: OnMoved
throws before ScheduleRestore; marked body in real room never swept), D2b (IOException at the checkpoint; host path shown), by reading:
visitor stale checkpoint, approval rollback half done. Required: own budget per entry into sandbox code with the caller's count put back;
checkpoint+mark in the driver/master move path before hooks, refuse entry on any failure; restore scheduled by the move path;
heartbeat invariant with sandbox: log lines; no host path in player text; audit every multi-step sequence.
CLASS B (watchdog/share blind and misattributing): W1 (_lastRestricted never restored on Pop: wrong home quarantined), W2 ("{a"*0.2MB
LongDesc, quadratic Markup with no sandbox frame: 45 s), C2 (compile per file limit; submit compiles 200 files: 79 s), suspected
quadratic destroy; several homes at once (global cap). Required: domain from frames on the stack (outermost); TryCross caps; linear
Markup; step attributed to first sandbox domain entered; compile time charged; per-command/window compile budget; lower per-file limit;
objects per home cap; linear destroy; global share cap.
Individual: T1 (tickless bodies: expression ctor, field/auto-prop initialisers, ctor initialiser args; enumerate all places; depth
counter), D2a (home quota in the driver: bytes, files, dirs, depth; settings), L3 (Write log flood; cap every log line restricted code
causes), S1 (visitor mark per line: collapse spaces, wrap/cut, mark every piece), lone CR (only before LF), N1 (fixed on main 07bb29f;
also cap a non-reading connection's outbox), memory (TryCross caps, objects per home, call-outs per home; state what remains).
At the end: tests/redteam2 run_all all PASS, tests/redteam run_all 13/13, sandbox_test, every suite one at a time; docs "Second review".
- class A committed; T1 committed (t1 redteam2 4/4, t1_every_body 28/28, load_check 0 failed)
- class B + individual committed (w1 w2 c2 d2 l3 s1 n1 t1 t2 t3 PASS individually)
- build exit=0 secs=2 last: Build succeeded.
- redteam2/run_all exit=0 secs=355 last: 10 of 10 proofs pass
- redteam/run_all exit=1 secs=501 last: 13 of 14 proofs pass: open in t1_every_body
- sandbox_test exit=0 secs=159 last: 0 failure(s)
- ab34f4a + 8a5f0b1 (docs): redteam2/run_all 10 of 10; redteam/run_all 13 of 14 (t1_every_body hit the 0.5 s share: my test fired
  commands faster than the window; now paced, passes); sandbox_test 0 failures. Next: full run, one suite at a time.
- build exit=0 secs=16 last: Build succeeded.
- load_check exit=0 secs=226 last: 0 file(s) failed to load
- sandbox_test exit=0 secs=170 last: 0 failure(s)
- redteam2/run_all exit=0 secs=388 last: 10 of 10 proofs pass
- redteam/run_all exit=0 secs=635 last: 14 of 14 proofs pass
- gmcp_test exit=0 secs=81 last: 101 checks, 0 failure(s)
- smoke_test exit=0 secs=222 last: 0 failure(s)
- area_test exit=1 secs=20 last: ConnectionRefusedError: [Errno 111] Connection refused
- area_test exit=0 secs=632 last: 0 failure(s)
- class_test exit=1 secs=452 last: 3 failure(s)
- item_test exit=0 secs=91 last: 0 failure(s)
- world_test exit=0 secs=270 last: 0 failure(s)
- find_test exit=0 secs=84 last: 159 checks, 0 failure(s)
- chronicle_test exit=0 secs=91 last: 0 failure(s)
- chronicle_play_test exit=0 secs=84 last: 0 failure(s)
- craft_test exit=1 secs=313 last: ConnectionRefusedError: [Errno 111] Connection refused
- craft_content_test exit=1 secs=20 last: ConnectionRefusedError: [Errno 111] Connection refused
- comm_test exit=0 secs=176 last: 221 checks, 0 failure(s)
- playfeel_test exit=0 secs=254 last: 0 failure(s)
- class_test exit=0 secs=447 last: 0 failure(s)
- craft_test exit=0 secs=351 last: 313 checks, 0 failure(s)
- craft_content_test exit=0 secs=233 last: 289 checks, 0 failure(s)
- attribute_test exit=0 secs=300 last: 0 failure(s)
- rest_test exit=0 secs=197 last: 0 failure(s)
- rename_test exit=0 secs=117 last: 0 failure(s)
- rank_test exit=1 secs=636 last: 1 failure(s)
- aim_test exit=0 secs=380 last: 0 failure(s)
- sneak_test exit=0 secs=348 last: 0 failure(s)
- story_test exit=0 secs=173 last: 544 checks, 0 failure(s)
- hollow_expansion_test exit=0 secs=52 last: 673 checks, 0 failure(s)
- rank_test exit=0 secs=646 last: 0 failure(s)
- corridor_mob_test exit=0 secs=246 last: 0 failure(s)
- ansi_test exit=0 secs=109 last: 0 failure(s)
- timing_test exit=0 secs=550 last: 0 failure(s)
- city_test exit=0 secs=625 last: 4685 checks, 4685 passed, 0 failed (wolves killed: 0; 625 s in the game)
- corridor_test exit=0 secs=1293 last: 6861 checks, 6861 passed, 0 failed (1293 s in the game)
- playthrough_test exit=0 secs=1556 last: 0 failure(s)

## Full run after the second review's fixes (tree 81693ec), one suite at a time, port 5005 — COMPLETE, every suite passes
build ok; load_check 0 failed; sandbox_test 0 failures; redteam2/run_all 10 of 10; redteam/run_all 14 of 14; gmcp 101/0; smoke 0;
area 0 (first try: driver boot > the harness's 20 s wait, rerun 0); class 0 (first run 3 combat-sampling failures, "Kill what?", rerun 0);
item, world, find 159, chronicle, chronicle_play, craft 313, craft_content 289 (first tries: boot-wait timeouts, reruns 0), comm 221,
playfeel, attribute, rest, rename, rank (first run one Arcane Bolt II sample, rerun 0), aim, sneak, story 544, hollow_expansion 673,
corridor_mob, ansi, timing, city 4685/0, corridor 6861/0, playthrough 0 (25.7 min).
Note: boot is ~14 s idle (was ~12 s); the suites' 20 s boot wait is tight under load.
