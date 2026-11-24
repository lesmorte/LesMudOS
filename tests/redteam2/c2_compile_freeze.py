#!/usr/bin/env python3
"""C2: Roslyn work just under the compile limit, repeated in one execution. The watchdog stops a compile of a home's program only
past max(30 s, 6 x stall) per compile, and pauses the step clock while compiling (Kernel.CompileStarted). `submit` compiles every
file of the submitted directory, one after another, in one command (Publish.Submit -> UpdateProgram per file, up to 200 files):
a level 1 wizard holds the game thread for files x (seconds per file) with no quarantine and no restart. Each file here is a
~300-character nested-lambda overload resolution (exponential in depth: ~1.45x per level here, so depth 14 is ~20 s, 15 ~30 s);
the attacker calibrates the depth with one `update` and stays under the limit. 200 files of ~20 s is over an hour.
Usage: c2_compile_freeze.py [files] [depth]  (default 6 files of depth 11, ~12-15 s each on this machine)."""
import os
import sys
import threading
import time
import rt2


def slow(depth, n):
    body = "0"
    for i in range(depth):
        body = f"M(a{i} => {body})"
    return ("// @inherit /std/item\npublic class WrenSlow%d : Item {\n static int M(Func<int,int> f) { return 0; }\n"
            " static int M(Func<string,int> f) { return 0; }\n static int M(Func<long,int> f) { return 0; }\n"
            " public override void Create() { var x = %s; }\n}\n") % (n, body)


def main():
    files = int(sys.argv[1]) if len(sys.argv) > 1 else 6
    depth = int(sys.argv[2]) if len(sys.argv) > 2 else 11
    wiz = {f"/wiz/wren/slow/s{i}": slow(depth, i) for i in range(files)}
    g = rt2.Game(wiz_files=wiz)
    g.start()
    try:
        g.setup_people()
        t0 = time.time()
        g.wren.command("update /wiz/wren/slow/s0", timeout=600)
        one = time.time() - t0
        print(f"one file compiles in {one:.1f}s (calibration with update)")
        g.wren.send("submit /wiz/wren/slow")
        time.sleep(0.5)
        t0 = time.time()
        g.pat.command("look", timeout=3600)
        stall = time.time() - t0
        print(f"pat's look, sent while wren's submit compiles {files} files, took {stall:.1f}s")
        out = g.wren.read_until("> ", timeout=3600)
        print("wren sees:", out.strip()[:160]); print("alive:", g.alive(), "quarantine:", os.path.exists(os.path.join(g.work, "save", "sandbox_quarantine.txt")))
        q = os.path.join(g.work, "save", "sandbox_quarantine.txt")
        unnoticed = stall >= 10 and g.alive() and not os.path.exists(q)
        rt2.check(f"C2: one level 1 command cannot hold every player for {stall:.0f}s ({files} compiles of {one:.0f}s) unnoticed by the watchdog",
                  not unnoticed, f"stall {stall:.1f}s, driver alive, no quarantine line")
    finally:
        g.stop()
    rt2.finish()


if __name__ == "__main__":
    main()
