#!/usr/bin/env python3
"""M1 (the third review): memory. A level 1 wizard's item allocates a 40 MB string per command and keeps it. Nothing in-process
can stop that; what the driver and the service do (docs/SANDBOX.md section 7, "Memory"): with a heap limit for .NET (here
DOTNET_GCHeapHardLimit, 400 MB; in service, the systemd MemoryMax that .NET reads), past three quarters of it the driver saves every
player and logs it, and an allocation past the limit fails in the wizard's own statement, as an error for the wizard, instead of
killing the process. This proves the mitigation, not a cure: the memory kept stays kept until the driver restarts.

Expected: every check passes."""
import os
import time
import rtlib

HOG = r'''// @inherit /std/item
public class WrenHog : Item
{
    public static List<string> Kept = new();
    public override void Create() { Name = "hog"; Ids.Add("hog"); ShortDesc = "a sandbox hog"; }
    public override void Init() { base.Init(); AddAction("rthog", a => { Kept.Add(new string('x', 20_000_000)); Write("HOG " + Kept.Count + "\n"); return true; }); }
}
'''


def main():
    os.environ["DOTNET_GCHeapHardLimit"] = hex(400 * 1024 * 1024)
    g = rtlib.Game(wiz_files={"/wiz/wren/hog": HOG})
    g.start()
    try:
        g.setup_people()
        g.wren.command("goto /wiz/wren/lab")
        g.wren.command("clone /wiz/wren/hog")
        last = ""
        for _ in range(14):
            last = g.wren.command("rthog", timeout=60)
            time.sleep(1.2)
            if "***" in last:
                break
        print("wren's last:", last.strip()[:160])
        log = g.driver_log()
        rtlib.check("M1: past three quarters of the heap limit the driver saves every player and logs it", "saving every player" in log, log[-500:])
        rtlib.check("M1: an allocation past the limit fails as an error for the wizard", "***" in last, last)
        rtlib.check("M1: the driver is still running", g.alive())
        rtlib.check("M1: an ordinary player is still answered", "Village Square" in g.pat.command("look", timeout=60))
    finally:
        g.stop()
        os.environ.pop("DOTNET_GCHeapHardLimit", None)
    rtlib.finish()


if __name__ == "__main__":
    main()
