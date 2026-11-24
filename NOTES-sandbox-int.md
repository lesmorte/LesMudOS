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
