#!/usr/bin/env python3
"""F3b: the half-finished restore of F3 also leaves workshop-made sandbox objects wielded by a body that is out of the workshop. Real
combat code reads the sandbox blade's numbers (read-only calls are allowed) and the wizard kills a real 5000-Vitality creature in
one blow in a real room; its corpse and loot are real, and the loot goes to an ordinary player's save.

Expected after a fix: every check passes."""
import os
import re
import time
import rtlib
from f3_restore_abort import ANCHOR

BLADE = r'''// @inherit /std/weapon
public class WrenBlade : Weapon
{
    public override void Create() { Name = "blade"; Ids.Add("blade"); ShortDesc = "a sandbox blade"; DamageMin = 100000; DamageMax = 100000; ToHit = 200; }
}
'''
ARENA = r'''// @inherit /std/room
public class RtArena : Room
{
    public override void Create() { ShortDesc = "Test Arena"; LongDesc = "A real room of the game."; Exits["square"] = "/domains/hollow/village/square"; AddSpawn("/domains/rt/dummy"); }
}
'''
DUMMY = r'''// @inherit /std/monster
public class RtDummy : Monster
{
    public override void Create()
    {
        Name = "dummy"; Ids.Add("dummy"); ShortDesc = "a heavy training dummy"; LongDesc = "Very hard to break.";
        Level = 5; MaxVitality = 5000; DamageMin = 1; DamageMax = 1; ToHit = -50; XpValue = 5; Aggressive = false;
        AddLoot("/domains/rt/relic", 100);
    }
}
'''


def main():
    g = rtlib.Game(wiz_files={"/wiz/wren/anchor": ANCHOR, "/wiz/wren/blade": BLADE},
                   real_files={"/domains/rt/arena": ARENA, "/domains/rt/dummy": DUMMY})
    g.start()
    try:
        g.setup_people()
        w = g.wren
        w.command("goto /wiz/wren/lab")
        w.command("clone /wiz/wren/anchor")
        w.command("clone /wiz/wren/blade")
        print(w.command("wield blade").strip()[:120])
        w.command("out")
        time.sleep(1.0)
        print(g.dump("wren").strip())
        print(w.command("goto /domains/rt/arena").strip()[-300:])
        out = w.command("kill dummy")
        deadline = time.time() + 20
        while time.time() < deadline and "corpse" not in out.lower() and "dies" not in out.lower():
            out += w.read_until("> ", timeout=2)
        print("fight:", out.strip()[-500:])
        killed = "dies" in out.lower() or "corpse" in out.lower()
        rtlib.check("F3b: a sandbox weapon cannot kill a real creature in the real game", not killed, out[-400:])
        look = w.command("look")
        loot = w.command("get relic from corpse")
        if "relic" not in loot.lower() or "no " in loot.lower()[:20]:
            loot += w.command("loot corpse")
        print("loot:", loot.strip()[:300])
        w.command("square")
        print(w.command("give relic to pat").strip()[:200])
        g.pat.command("quit", marker="Goodbye", timeout=5)
        time.sleep(0.5)
        text = open(os.path.join(g.work, "save", "players", "pat.json")).read()
        rtlib.check("F3b: the loot of that kill does not reach an ordinary player's save", "/domains/rt/relic" not in text, re.search(r'"carried": \[(.*?)\]', text, re.S).group(1))
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
