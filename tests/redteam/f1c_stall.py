#!/usr/bin/env python3
"""F1b, what the ticks cannot bound (the review's "Not tested" list): framework loops with no mudlib code inside them.

  share     sixty sandbox things whose heartbeats each burn the whole tick budget: without a limit a heartbeat pass holds the game
            for seconds; with each domain's share of time limited, an ordinary player's commands still answer within a second or two.
  stall     the nested form: Enumerable.Range(0, 100000).SelectMany(_ => big).LongCount() ticks a hundred thousand times and runs ten
            billion steps inside the framework. Nothing can stop that thread, so the watchdog stops the driver after the stall
            limit (2 s here, LESMUDOS_SANDBOX_STALL), names the domain, and closes it: started again, the driver refuses to make
            objects of that wizard's sandbox until an administrator reopens it, and the rest of the game is as it was.

Expected: every check passes."""
import os
import time
import rtlib
import smoke_test as h

SPIN = r'''// @inherit /std/item
public class WrenSpin : Item
{
    public override void Create() { Name = "spin"; Ids.Add("spin"); ShortDesc = "a sandbox spinner"; SetHeartBeat(true); }
    public override void HeartBeat() { var n = 0; while (true) n++; }
}
'''

NEST = r'''// @inherit /std/item
public class WrenNest : Item
{
    public override void Create() { Name = "nest"; Ids.Add("nest"); ShortDesc = "a sandbox nest"; }
    public override void Init()
    {
        base.Init();
        AddAction("rtspawn", a => { for (var i = 0; i < 60; i++) MoveObject(CloneObject("/wiz/wren/spin"), Environment(Self)!); Write("SPAWNED\n"); return true; });
        AddAction("rtnest", a =>
        {
            var big = Enumerable.Range(0, 100000).ToList();
            Write("NEST " + Enumerable.Range(0, 100000).SelectMany(_ => big).LongCount() + "\n");
            return true;
        });
    }
}
'''


def main():
    os.environ["LESMUDOS_SANDBOX_STALL"] = "2"
    g = rtlib.Game(wiz_files={"/wiz/wren/spin": SPIN, "/wiz/wren/nest": NEST})
    g.start()
    try:
        g.setup_people()
        g.wren.command("goto /wiz/wren/lab")
        g.wren.command("clone /wiz/wren/nest")
        print(g.wren.command("rtspawn", timeout=120).strip()[-80:])
        time.sleep(3)
        worst = 0.0
        for _ in range(8):
            t0 = time.time()
            g.pat.command("look", timeout=60)
            worst = max(worst, time.time() - t0)
            time.sleep(0.7)
        print(f"pat's slowest look among eight, with sixty spinners: {worst:.2f}s")
        rtlib.check("F1b share: a wizard's heartbeats cannot hold the game for more than its share", worst < 2.5, f"{worst:.2f}s")
        rtlib.check("F1b share: the driver is running", g.alive())
        g.wren.command("goto /domains/hollow/village/square", timeout=60)  # the spinners go with the restore
        time.sleep(3)

        g.wren.command("goto /wiz/wren/lab")
        g.wren.command("clone /wiz/wren/nest")
        g.wren.send("rtnest")
        t0 = time.time()
        while g.alive() and time.time() - t0 < 60:
            time.sleep(0.2)
        took = time.time() - t0
        print(f"driver stopped after {took:.1f}s, exit code {g.proc.returncode}")
        rtlib.check("F1b stall: the watchdog stops a stalled driver within a few seconds", not g.alive() and took < 15, f"{took:.1f}s alive={g.alive()}")
        rtlib.check("F1b stall: it stops with exit code 3", g.proc.returncode == 3, str(g.proc.returncode))
        quarantine = os.path.join(g.work, "save", "sandbox_quarantine.txt")
        text = open(quarantine).read() if os.path.exists(quarantine) else ""
        rtlib.check("F1b stall: the stalling domain is written to the quarantine file", text.startswith("wiz:wren "), text)
        rtlib.check("F1b stall: the log names the domain", "STALL: code of wiz:wren" in g.driver_log(), g.driver_log()[-600:])

        if g.alive():  # the watchdog did not act: stop it here so that the rest can run
            g.proc.kill()
            g.proc.wait(30)
        g.log.close()
        g.start()
        g.chief = h.Client("chief")
        g.chief.read_until("> ")
        g.chief.command("clone /domains/rt/probe")
        g.pat = h.Client("pat")
        g.pat.read_until("> ")
        rtlib.check("F1b stall: an ordinary player plays on after the restart", "Village Square" in g.pat.command("look") or "square" in g.pat.command("look").lower())
        g.wren = h.Client("wren")
        g.wren.read_until("> ")
        out = g.wren.command("goto /wiz/wren/lab")
        out += g.wren.command("clone /wiz/wren/nest")
        print("wren after the restart:", out.strip()[-300:])
        rtlib.check("F1b stall: the closed workshop's code is not loaded again", "closed" in out and "Cloned" not in out, out)
        rtlib.check("F1b stall: the driver is running after the restart", g.alive())
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
