#!/usr/bin/env python3
"""T3: the same tick-budget trick on the way OUT. A sandbox room's verb burns k ticks, then moves the wizard out into a real room
(allowed: "a wizard also out into a room"). Player.OnMoved runs with the budget spent and throws before ScheduleRestore: the body
stands in the real game still marked as in the workshop, with no restore scheduled, carrying the workshop's gold and a wielded
sandbox blade. SweepSandboxThings runs only for bodies NOT marked, so the blade is never swept; the next move would restore,
so the wizard fights where it landed: a real creature dies in one blow (its corpse and loot are marked as sandbox objects
and vanish: that part holds). k is found by bisection with the verb's argument, in one boot.

Expected after a fix: every check passes."""
import os
import re
import time
import rt2
from f3b_sandbox_weapon_out import BLADE, ARENA, DUMMY

LAB = r'''// @inherit /std/room
public class WrenLeap : Room
{
    public override void Create() { ShortDesc = "Wren's Leap"; LongDesc = "x"; Exits["out"] = "/domains/hollow/village/square"; }
    public override void Init()
    {
        base.Init();
        AddAction("leap", a =>
        {
            var k = long.Parse(a.Trim());
            for (long i = 0; i < k; i++) { }
            MoveObject(ThisPlayer()!, LoadObject("/domains/rt/arena"));
            return true;
        });
        AddAction("rich", a => { ThisPlayer()!.Call("AddGold", 100000); Write("RICH\n"); return true; });
    }
}
'''


def main():
    g = rt2.Game(wiz_files={"/wiz/wren/leap": LAB, "/wiz/wren/blade": BLADE}, real_files={"/domains/rt/arena": ARENA, "/domains/rt/dummy": DUMMY})
    g.start()
    try:
        g.setup_people()
        w = g.wren
        g.chief.command("goto /domains/rt/arena")  # loads the arena (and its dummy) before the tries

        def attempt(k):
            w.command("goto /wiz/wren/leap")
            w.command(f"leap {k}")
            time.sleep(0.2)
            d = g.dump("wren")
            return "env=/domains/rt/arena" in d, "workshop=True" in d, d

        lo, hi = 0, 5_000_000
        while hi - lo > 1:
            mid = (lo + hi) // 2
            if attempt(mid)[0]:
                lo = mid
            else:
                hi = mid
        print(f"largest burn that still moves: {lo}")
        stuck = None
        for k in range(lo, lo - 60, -1):
            moved, marked, d = attempt(k)
            if moved and marked:
                stuck = k
                break
        print("burn that leaves the body marked in a real room:", stuck)
        rt2.check("T3: a wizard moved out of the workshop is always restored", stuck is None, d)
        if stuck is None:
            return
        # Prepare: blade and gold in the workshop, then leap out with that burn.
        g.dump("wren")
        w.command("goto /wiz/wren/leap")
        w.command("clone /wiz/wren/blade")
        w.command("wield blade")
        w.command("rich")
        w.command(f"leap {stuck}")
        time.sleep(3)  # a heartbeat or two: nothing sweeps the blade, nothing restores
        d = g.dump("wren")
        print("   ", d.strip()[:300])
        rt2.check("T3: workshop gold and a sandbox blade are not carried in a real room after a heartbeat",
                  not ("gold=100020" in d and "blade[wiz:wren]" in d and "env=/domains/rt/arena" in d), d)
        out = w.command("kill dummy")
        deadline = time.time() + 10
        while time.time() < deadline and "dies" not in out.lower():
            out += w.read_until("> ", timeout=2)
        print("    fight:", re.sub(r"\s+", " ", out)[-120:])
        rt2.check("T3: a sandbox weapon cannot kill a real creature in the real game", "dies" not in out.lower(), out[-300:])
        # Held: the corpse and loot are made while the marked body is on the call stack, so they are sandbox objects and vanish.
        look = g.chief.command("goto /domains/rt/arena") + g.chief.command("look")
        rt2.check("T3 (held): the corpse of that kill is not left in the real room", "corpse" not in look.lower(), look)
    finally:
        g.stop()
    rt2.finish()


if __name__ == "__main__":
    main()
