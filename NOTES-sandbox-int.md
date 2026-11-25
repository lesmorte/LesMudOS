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

## Integration with main (merge ec3edd1, then cbb1389 and 4acad3b)
Conflicts: adventurer.cs (PrepareSave, EnterWorld, CmdScore), TelnetServer.cs (main's N1 line taken).
Decisions:
1. Return-to-room vs the workshop: the saved character is the checkpoint, and its logoutRoom is the room the wizard entered the
   workshop from (PrepareSave(at) with the master's origin). WakeRoomOk refuses every path under /wiz (any spelling), the wizard's own
   home included, plus the Void and clones; PrepareSave also refuses any room with a domain. Quit and link death in a workshop do not
   save (Save is a no-op while marked), so the file stays the checkpoint. Login therefore never places a body in sandbox space; the
   only way in is the master's move with WorkshopCheckpoint. Checks: tests/redteam/r1_login_placement.py, return_test (wizard part).
2. Disconnect line: "closed by the game (outbox limit)" for the queue cap; player name = a real body's QueryName, cleaned, <= 32 chars;
   the rest of the line is the driver's own text (address, durations, reason). Sandbox code controls nothing in it.
3. Score: main's layout, the workshop line printed under the box ("[in the workshop: nothing here is saved]"); score, time and stat
   are body verbs, so a visitor's cannot be shadowed (OwnVerb rule). Check: l1_l2_and_rules (look, score, search, time, stat, get).
4. OnLine's pending prompt: only decides whether a "\n" precedes the next output; TextFor marks and cleans before CatchTell, so every
   visitor line still starts on its own line with the mark. No change needed.
5. Find verbs: a real find binds get/search/... over the body's own verbs (real code: allowed). In a workshop a find is a sandbox
   object: for the owner it may shadow body verbs; for a visitor get and search stay the visitor's own (search = hidden exits), while
   open/unwrap/untie/unroll/grab/pick (not body verbs) reach the visitor, marked. A workshop find pays nothing anyway.
6. timing_test: main's change is the score-layout join only; my expectations kept. Boot wait raised to 40 s in smoke_test, area_test,
   ansi_test, chronicle_test, city_infra, gmcp_test (rtlib already 120 s).
Memory (M1): deploy MemoryMax=768M + MemorySwapMax=0 + DOTNET_GCHeapHardLimitPercent=0x46; driver saves all players past 3/4 of the
heap limit; Restart=on-failure kept on purpose (the in-game shutdown exits 0 and stays down; OOM kill and exit 3 are failures).

## For the next batch from main (expected needs from the sandbox side)
- Aimed Shot opener cap: none (combat rules; sandbox creatures fight only workshop characters).
- charset ascii: the translation must come AFTER PlainText cleaning and the visitor mark (in the body's CatchTell/Receive, the one
  point where text is sent). Cleaning removes controls first; translation only maps printable non-ASCII to ASCII. The mark is ASCII
  already. Box-drawing in score becomes ASCII there too. Never translate before cleaning (a translation table must not produce ESC).
- time with a player time zone, two new driver efuns: read-only efuns are fine for sandbox code; if one sets a stored value on the body,
  it must go through a body function (not ReadOnly-named) so sandbox code cannot change a real player's zone; the zone string must be
  validated by the driver (no host paths in errors: TimeZoneInfo exceptions name files).
- Link-dead body lingering 60 s (vulnerable, reconnect resumes it): wizards are removed at once, so a workshop body is never link-dead.
  What must hold: (a) a body marked in the workshop is never kept link-dead (NetDead's wizard exemption must also cover any marked
  body, even one demoted during the visit); (b) a lingering body must not be movable into sandbox space (C1 rule: only its own
  command; a link-dead body has no command) and sandbox code must not reach it (it is outside sandbox space); (c) reconnect must not
  resume a body in sandbox space; (d) the heartbeat invariant iterates Users() only: a link-dead body is not a user, so the
  invariant must also walk lingering bodies (or they must be impossible in sandbox space, which (a)-(c) give).
- build exit=0 secs=2 last: Build succeeded.
- load_check exit=0 secs=229 last: 0 file(s) failed to load
- sandbox_test exit=0 secs=150 last: 0 failure(s)
- redteam/run_all exit=0 secs=565 last: 16 of 16 proofs pass
- redteam2/run_all exit=0 secs=341 last: 10 of 10 proofs pass
- return_test exit=0 secs=260 last: 0 failure(s)
- gmcp_test exit=0 secs=71 last: 101 checks, 0 failure(s)
- smoke_test exit=0 secs=209 last: 0 failure(s)
- area_test exit=0 secs=515 last: 0 failure(s)
- class_test exit=0 secs=478 last: 0 failure(s)
- item_test exit=0 secs=76 last: 0 failure(s)
- world_test exit=0 secs=265 last: 0 failure(s)
- find_test exit=0 secs=72 last: 159 checks, 0 failure(s)
- chronicle_test exit=0 secs=59 last: 0 failure(s)
- chronicle_play_test exit=0 secs=94 last: 0 failure(s)
- craft_test exit=0 secs=341 last: 313 checks, 0 failure(s)
- craft_content_test exit=0 secs=231 last: 289 checks, 0 failure(s)
- comm_test exit=0 secs=165 last: 221 checks, 0 failure(s)
- playfeel_test exit=0 secs=304 last: 0 failure(s)
- attribute_test exit=0 secs=301 last: 0 failure(s)
- rest_test exit=0 secs=190 last: 0 failure(s)
- rename_test exit=0 secs=116 last: 0 failure(s)
- rank_test exit=0 secs=633 last: 0 failure(s)
- aim_test exit=0 secs=377 last: 0 failure(s)
- sneak_test exit=0 secs=338 last: 0 failure(s)
- story_test exit=0 secs=173 last: 544 checks, 0 failure(s)
- hollow_expansion_test exit=0 secs=57 last: 684 checks, 0 failure(s)
