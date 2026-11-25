#!/usr/bin/env python3
"""T2: run out the tick budget on purpose so that the workshop checkpoint is skipped (the builder's "exhaust the tick budget, then
call real code that is part-way through a multi-step update").

`goto /wiz/wren/gN` loads the sandbox room in the wizard's own command; its Create burns k ticks of the 5,000,000 budget of that one
execution, then the real code goes on: goto moves the body, Player.OnMoved -> EnterWorkshop runs with almost no ticks left.
 - k in the top ~10 ticks before the limit: EvalCostExceededException in EnterWorkshop before `inWorkshop = true` (QueryWizLevel,
   Outermost): the wizard stands in the workshop UNMARKED, is never checkpointed or restored, and walks out with a sandbox
   weapon (F3b again: a real creature killed in one blow, real experience).
 - k ~10-30 ticks lower: the exception is thrown inside PrepareSave/SaveObject, which EnterWorkshop catches ("the last save stands
   as the checkpoint: restoring to it can only lose what happened since, never gain"). It can gain: whatever left the character
   since its last save (a drop is not saved) comes back on the restore. Here a real relic is dropped, an ordinary player picks it
   up, and the wizard is restored holding it too: a duplicated real item, in both save files.
Ticks are deterministic, so the attacker finds k by bisection on whether the goto moved them (no special access: new file per try).

Expected after a fix: every check passes."""
import os
import re
import time
import rt2
import smoke_test as h
from f3b_sandbox_weapon_out import BLADE, ARENA, DUMMY


def gate(n, k):
    return f'''// @inherit /std/room
public class WrenGate{n} : Room
{{
    public override void Create() {{ ShortDesc = "Gate {n}"; LongDesc = "x"; Exits["out"] = "/domains/hollow/village/square"; for (var i = 0; i < {k}; i++) {{ }} }}
}}
'''


def main():
    g = rt2.Game(wiz_files={"/wiz/wren/blade": BLADE}, real_files={"/domains/rt/arena": ARENA, "/domains/rt/dummy": DUMMY})
    g.start()
    n = [0]
    save = os.path.join(g.work, "save", "players", "wren.json")

    def attempt(k, leave=True):
        n[0] += 1
        h.write(g.mudlib, f"/wiz/wren/g{n[0]}", gate(n[0], k))
        before = os.stat(save).st_mtime_ns if os.path.exists(save) else 0
        g.wren.command(f"goto /wiz/wren/g{n[0]}")
        after = os.stat(save).st_mtime_ns if os.path.exists(save) else 0
        d = g.dump("wren")
        moved, marked = f"Gate {n[0]}" in g.wren.command("look") or f"g{n[0]}" in d, "workshop=True" in d
        if moved and leave:
            g.wren.command("goto /domains/hollow/village/square")
            time.sleep(0.3)
        return moved, marked, after != before

    try:
        g.setup_people()
        g.chief.command("clone /domains/rt/relic")
        g.chief.command("goto /domains/hollow/village/square")
        g.chief.command("give relic to wren")          # both saved: wren's save file holds the relic
        g.chief.command("clone /domains/rt/probe")
        lo, hi = 0, 5_000_000
        while hi - lo > 1:
            mid = (lo + hi) // 2
            if attempt(mid)[0]:
                lo = mid
            else:
                hi = mid
        print(f"largest burn that still lets the goto move: {lo} ticks ({n[0]} tries)")

        # Duplication: drop the relic (not saved), Pat takes it, then enter with the checkpoint's save cut short.
        print(g.wren.command("drop relic").strip()[:60])
        print(g.pat.command("get relic").strip()[:60])
        r = attempt(lo - 15, leave=False)
        print(f"burn {lo - 15}: moved={r[0]} marked={r[1]} checkpoint written={r[2]}")
        g.wren.command("goto /domains/hollow/village/square")  # the restore, from the stale checkpoint
        time.sleep(1)
        wd, pd = g.dump("wren"), g.dump("pat")
        print("   ", wd.strip()[:220])
        print("   ", pd.strip()[:220])
        rt2.check("T2: leaving the workshop never gives back a real item the character no longer had on entering",
                  not ("rt/relic[real]" in wd and "rt/relic[real]" in pd), wd + pd)
        g.pat.command("quit", marker="Goodbye", timeout=5)
        g.wren.command("quit", marker="Goodbye", timeout=5)
        time.sleep(0.5)
        both = all("/domains/rt/relic" in open(os.path.join(g.work, "save", "players", f)).read() for f in ("wren.json", "pat.json"))
        rt2.check("T2: the relic is not in both save files", not both, "relic in wren.json and pat.json")

        # Unmarked: the top of the window.
        g.wren = h.Client("wren")
        g.wren.read_until("> ")
        r = attempt(lo, leave=False)
        d = g.dump("wren")
        print(f"burn {lo}: moved={r[0]} marked={r[1]} checkpoint written={r[2]}")
        rt2.check("T2: a wizard who is in sandbox space is always marked as in the workshop", not (r[0] and not r[1]), d)
        g.wren.command("clone /wiz/wren/blade")
        g.wren.command("wield blade")
        g.wren.command("goto /domains/rt/arena")
        out = g.wren.command("kill dummy")
        deadline = time.time() + 10
        while time.time() < deadline and "dies" not in out.lower():
            out += g.wren.read_until("> ", timeout=2)
        print("    fight:", re.sub(r"\s+", " ", out)[-160:])
        rt2.check("T2: a sandbox weapon cannot kill a real creature in the real game", "dies" not in out.lower(), out[-300:])
        took = g.wren.command("get relic from corpse")
        print("    wren loots:", re.sub(r"\s+", " ", took)[:100])
        g.wren.command("quit", marker="Goodbye", timeout=5)
        time.sleep(0.5)
        relics = open(save).read().count("/domains/rt/relic")
        rt2.check("T2: no real loot of that kill reaches the wizard's save file (it held one relic before the kill)", relics < 2, f"{relics} relics in wren.json")
    finally:
        g.stop()
    rt2.finish()


if __name__ == "__main__":
    main()
