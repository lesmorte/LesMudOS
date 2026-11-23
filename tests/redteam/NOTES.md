# Red-team notes (working file)

## Proven
- F1 crash (stack overflow via expression lambda, exit -6) and hang (1e9 LINQ steps = 3.6 s stall, no ticks): f1_dos.py
- F2 AggregateException window: Pat gold/quest/real relic/forged text, kept in save: f2_exception_window.py
- F3 restore abort via OnDestruct throwing System.Exception: body keeps 100000 gold, buys potion, gives to Pat (saved): f3_restore_abort.py
- P1 U+2028 hides live code in a // comment line as cat shows it; P2 swap-and-swap-back: p1_publish_review.py
- P3 reviewer in workshop: cat rebinding (fake file) and ESC[8m + InputTo password capture: p3_reviewer_in_workshop.py
- F2 also drags a level 10 wizard into the workshop (combine with P3b: forced phishing)
- F3b sandbox blade kept wielded after F3, kills real 5000-vit dummy, loot to Pat: f3b_sandbox_weapon_out.py
- F4 tuple shallow cross (latent): f4_tuple_cross.py; F5 cross-home calls/moves: f5_cross_home.py
- C1 chain F2 drag + P3b phish of level 10: c1_chain_phish_admin.py
- A1 allow-list: 60+ probes held (nint compiles, harmless): a1_allowlist.py
- F2 window: WriteFile/Promote/CheckAdminSecret still refused (identity checks hold)

## Suspected / to prove
- F1 DoS: expression-bodied lambda recursion has no Tick/EnsureSufficientExecutionStack -> StackOverflow kills the driver.
- F2 Unrestricted window: a non-mudlib exception (AggregateException) wrapping a sandbox exception escapes In() unconverted;
  Execute -> ReportError reads error.Message after ClearStack and _thisPlayer=null -> sandbox Message getter runs unrestricted.
- F3 Restore abort: sandbox item's OnDestruct throws System.Exception during LeaveWorkshop -> catch(MudRuntimeException) misses ->
  old body stays alive, leavingWorkshop=true, inWorkshop=false, keeps workshop state in the real game.
- F4 ValueTuple crosses without copy (TryCross returns value types as-is) -> shared List inside a tuple.
- CPU hang without ticks: LINQ over huge ranges with expression lambdas.

## Next
- REPORT.md: the Write tool refused it in this agent session (subagent rule); full report text handed back to the caller instead
- publishing: cat shows raw ESC/CR (hide lines from reviewer); swap-and-swap-back between submit and approve (cat does not show hashes)
- F4 tuple shallow cross
- look for password capture / level escalation from the F2 window
