#!/usr/bin/env python3
"""F1: cheap denial of service from a level 1 wizard's own item.
  (a) rtcrash: recursion through an expression-bodied lambda. The tick injector adds Runtime.Tick() (which also calls
      EnsureSufficientExecutionStack) only to block bodies; an expression-bodied lambda, operator or indexer gets none, so the
      recursion is neither counted nor stopped and the game thread overflows its stack: the whole driver process dies.
  (b) rthang: framework code runs without ticks, so one expression-bodied LINQ call holds the game thread for as long as the
      author likes (here a bounded ~1e9-step count), far past the 5,000,000-tick budget that is meant to stop runaway code.
Run with an argument "hang" or "crash" (default: both, hang first).

Expected after a fix: every check passes."""
import sys
import time
import rtlib

BOMB = r'''// @inherit /std/item
public class WrenDos : Item
{
    public override void Create() { Name = "dos"; Ids.Add("dos"); ShortDesc = "a sandbox dos"; }
    public override void Init()
    {
        base.Init();
        AddAction("rthang", a =>
        {
            var n = Enumerable.Range(0, 1_000_000_000).Count(x => x >= 0);   // 1e9 steps, not one tick
            Write("HANG done " + n + "\n");
            return true;
        });
        AddAction("rtcrash", a =>
        {
            Func<int, int>? f = null;
            f = x => f!(x + 1) + 1;
            Write("CRASH " + f(0) + "\n");
            return true;
        });
    }
}
'''


def main():
    which = sys.argv[1] if len(sys.argv) > 1 else "both"
    g = rtlib.Game(wiz_files={"/wiz/wren/dos": BOMB})
    g.start()
    try:
        g.setup_people()
        g.wren.command("goto /wiz/wren/lab")
        print(g.wren.command("clone /wiz/wren/dos").strip())
        if which in ("hang", "both"):
            g.wren.send("rthang")
            time.sleep(0.3)
            t0 = time.time()
            reply = g.pat.command("look", timeout=120)
            stall = time.time() - t0
            print(f"pat's look took {stall:.1f}s")
            print(g.wren.read_until("HANG", timeout=120).strip()[-120:])
            rtlib.check("F1b: one sandbox command cannot stall every player for seconds (evaluation limit applies)", stall < 2.0, f"stall {stall:.1f}s")
        if which in ("crash", "both"):
            g.wren.send("rtcrash")
            time.sleep(8)
            rtlib.check("F1a: a sandbox recursion cannot kill the driver process", g.alive(), g.driver_log()[-1500:])
            if not g.alive():
                print("driver exit code:", g.proc.returncode)
                print(g.driver_log()[-600:])
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
