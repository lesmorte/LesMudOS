#!/usr/bin/env python3
"""W1: blame shifting. The stall watchdog quarantines Kernel._lastRestricted, "the sandbox domain whose frame was pushed last".
Push sets it; Pop never restores it. So Wren's code touches one object of another home (here LoadObject of a file in the level 10
owner's home, which pushes a wiz:chief frame while the blueprint is made; a read-only call such as Short does the same), returns to
its own frame and stalls there. The watchdog names wiz:chief: the owner's home is closed and the driver restarted, Wren's is not.
Repeatable once per restart against every home on the machine, and Wren is never quarantined.

Expected after a fix: wiz:wren is quarantined, wiz:chief is not."""
import os
import time
import rt2
import smoke_test as h

HALL = r'''// @inherit /std/room
public class ChiefHall : Room
{
    public override void Create() { ShortDesc = "Chief's hall"; LongDesc = "The owner's own work."; }
}
'''

BLAME = rt2.item("WrenBlame", {"rtblame": '''
            var other = LoadObject("/wiz/chief/hall");
            var s = other.Call<string>("Short");
            var big = Enumerable.Range(0, 100000).ToList();
            Write("BLAME " + s + Enumerable.Range(0, 100000).SelectMany(_ => big).LongCount() + "\\n");
            return true;'''})


def main():
    os.environ["LESMUDOS_SANDBOX_STALL"] = "2"
    g = rt2.Game(wiz_files={"/wiz/wren/blame": BLAME, "/wiz/chief/hall": HALL})
    g.start()
    try:
        g.setup_people()
        g.wren.command("goto /wiz/wren/lab")
        print(g.wren.command("clone /wiz/wren/blame").strip()[:100])
        g.wren.send("rtblame")
        t0 = time.time()
        while g.alive() and time.time() - t0 < 60:
            time.sleep(0.2)
        print(f"driver stopped after {time.time() - t0:.1f}s, exit code {g.proc.returncode}")
        quarantine = os.path.join(g.work, "save", "sandbox_quarantine.txt")
        text = open(quarantine).read() if os.path.exists(quarantine) else ""
        print("quarantine file:", text.strip())
        rt2.check("W1: the watchdog quarantines the home whose code stalled (wiz:wren)", "wiz:wren " in text, text)
        rt2.check("W1: a home whose code did not stall is not quarantined (wiz:chief)", "wiz:chief " not in text, text)
        if g.alive():
            g.proc.kill()
            g.proc.wait(30)
        g.log.close()
        g.start()
        g.chief = h.Client("chief")
        g.chief.read_until("> ")
        out = g.chief.command("goto /wiz/chief/hall")
        print("chief after the restart:", out.strip()[-160:])
        rt2.check("W1: after the restart the owner can still enter their own home", "closed" not in out, out)
        g.wren = h.Client("wren")
        g.wren.read_until("> ")
        out = g.wren.command("goto /wiz/wren/lab") + g.wren.command("clone /wiz/wren/blame")
        rt2.check("W1: after the restart the staller's home is closed", "closed" in out, out)
    finally:
        g.stop()
    rt2.finish()


if __name__ == "__main__":
    main()
