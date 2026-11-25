#!/usr/bin/env python3
"""L3: unbounded log output from sandbox code. Kernel.Write with no ThisPlayer() (a heartbeat or call-out of a sandbox thing that is
not living) logs the whole text: Log.Info($"write: {PlainText.Clean(message)}") on every call, with no cap per execution or per home
(NoteRefusal is capped at 3 lines, this is not). A level 1 wizard's item fills the service log (journald, DEPLOY.md) as fast as the
machine writes: the log rotates out everything else (the refusals and STALL lines that show what the wizard did), and on a host
whose journal is not capped the disk fills. Measured here: bytes added to the driver's log in about 8 seconds by one item.

Expected after a fix: a few lines, not megabytes."""
import os
import time
import rt2

FLOOD = r'''// @inherit /std/item
public class WrenFlood : Item
{
    public override void Create() { Name = "flood"; Ids.Add("flood"); ShortDesc = "a flood"; SetHeartBeat(true); }
    public override void HeartBeat()
    {
        var line = new string('x', 100000);
        for (var i = 0; i < 200; i++) Write(line);   // 20 MB per heartbeat, all to the log
    }
}
'''


def main():
    g = rt2.Game(wiz_files={"/wiz/wren/flood": FLOOD})
    g.start()
    try:
        g.setup_people()
        g.wren.command("goto /wiz/wren/lab")
        log = os.path.join(g.work, "driver.log")
        before = os.path.getsize(log)
        g.wren.command("clone /wiz/wren/flood")
        g.wren.command("drop flood")
        time.sleep(8)
        grown = os.path.getsize(log) - before
        print(f"driver log grew by {grown / 1e6:.1f} MB in 8 s from one level 1 wizard's item")
        rt2.check("L3: sandbox code cannot write megabytes to the driver log", grown < 1_000_000, f"{grown} bytes")
        rt2.check("L3: the driver is still running", g.alive())
    finally:
        g.stop()
    rt2.finish()


if __name__ == "__main__":
    main()
