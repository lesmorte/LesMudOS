#!/usr/bin/env python3
"""D2: no quota on a wizard's home, and an I/O failure at the workshop checkpoint leaves the wizard in sandbox space unmarked.

(a) A level 1 wizard's own file commands (copy, mkdir) have no quota: any number of files of up to 256 KB and any number of
    directories in /wiz/<name>. Filling the disk (bytes or inodes) needs only a script; it outlasts a restart, and from then on
    every SaveObject fails with an IOException. Shown here with a bounded number of copies.
(b) Player.EnterWorkshop catches only MudRuntimeException around the checkpoint's SaveObject. A full disk makes SaveStore.Save
    throw IOException, so OnMoved throws before inWorkshop is set: the wizard stands in the workshop with no mark, is never
    restored, and walks out carrying a sandbox weapon (F3b again). The full disk is simulated here by a directory standing where
    SaveStore writes the temporary file (save/players/wren.json.tmp), which makes File.WriteAllText throw the same way.

Expected after a fix: every check passes."""
import os
import time
import rt2
import rtlib
from f3b_sandbox_weapon_out import BLADE, ARENA, DUMMY

COPIES = 40


def main():
    g = rt2.Game(wiz_files={"/wiz/wren/blade": BLADE}, real_files={"/domains/rt/arena": ARENA, "/domains/rt/dummy": DUMMY})
    g.start()
    try:
        g.setup_people()
        w = g.wren
        # (a)
        w.command("mkdir /wiz/wren/junk")
        ok = 0
        for i in range(COPIES):
            if "Copied" in w.command(f"copy /secure/crafting.cs /wiz/wren/junk/c{i}.cs"):
                ok += 1
        for i in range(20):
            w.command(f"mkdir /wiz/wren/junk/d{i}")
        size = sum(os.path.getsize(os.path.join(g.mudlib, "wiz", "wren", "junk", f)) for f in os.listdir(os.path.join(g.mudlib, "wiz", "wren", "junk")) if f.endswith(".cs"))
        ndirs = len([d for d in os.listdir(os.path.join(g.mudlib, "wiz", "wren", "junk")) if d.startswith("d")])
        print(f"(a) {ok} of {COPIES} copies made ({size // 1024} KB), {ndirs} of 20 directories made; nothing refused")
        rt2.check("D2a: a level 1 wizard's home has a quota (files, bytes or directories)", ok < COPIES or ndirs < 20, f"{ok} copies, {ndirs} dirs")

        # (b)
        os.makedirs(os.path.join(g.work, "save", "players", "wren.json.tmp"), exist_ok=True)
        out = w.command("goto /wiz/wren/lab")
        print("(b) wren enters with the checkpoint failing:", out.strip()[:300])
        print("   ", g.dump("wren").strip()[:200])
        w.command("clone /wiz/wren/blade")
        print("   ", w.command("wield blade").strip()[:100])
        out = w.command("goto /domains/rt/arena")
        print("    out to a real room:", out.strip()[:200])
        dump = g.dump("wren")
        print("   ", dump.strip()[:300])
        rt2.check("D2b: a wizard in sandbox space is always marked as in the workshop", "workshop=True" in dump or "blade" not in dump, dump)
        out = w.command("kill dummy")
        deadline = time.time() + 10
        while time.time() < deadline and "dies" not in out.lower() and "corpse" not in out.lower():
            out += w.read_until("> ", timeout=2)
        print("    fight:", out.strip()[-300:])
        rt2.check("D2b: a sandbox weapon cannot kill a real creature in the real game", "dies" not in out.lower() and "corpse" not in out.lower(), out[-300:])
    finally:
        g.stop()
    rt2.finish()


if __name__ == "__main__":
    main()
