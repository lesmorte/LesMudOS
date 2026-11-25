#!/usr/bin/env python3
"""F3, the other routes (the review's "Not tested" list): every way out of the workshop must restore the checkpoint, whatever the
sandbox things carried do. The anchor throws a plain System.Exception from OnDestruct, Vanish and ForgetEnemy, so any route that
runs its code as a step it must finish would stop half-way.

  death     the wizard dies in the workshop (sandbox code calls Die on its owner)
  goto      the wizard leaves by goto, not by an exit
  quit      the wizard quits in the workshop, and logs in again
  linkdeath the connection drops in the workshop, and the wizard logs in again
  visitor   sandbox code slips an anchor into a visiting reviewer's inventory; the reviewer walks out
  shutdown  a level 10 shuts the game down while the wizard is in the workshop; the driver starts again
  kill      the driver is killed (SIGKILL) at the moment the wizard leaves; the driver starts again

Expected: every check passes."""
import time
import rtlib
import smoke_test as h

ANCHOR = r'''// @inherit /std/item
public class WrenAnchor : Item
{
    public override void Create() { Name = "anchor"; Ids.Add("anchor"); ShortDesc = "a sandbox anchor"; }
    public override void OnDestruct() { throw new Exception("the anchor holds"); }
    public void Vanish() { throw new Exception("the anchor will not vanish"); }
    public void ForgetEnemy(ObjectRef who) { throw new Exception("the anchor remembers"); }
}
'''

WAND = r'''// @inherit /std/item
public class WrenWand : Item
{
    public override void Create() { Name = "wand"; Ids.Add("wand"); ShortDesc = "a sandbox wand"; }
    public override void Init()
    {
        base.Init();
        AddAction("rtrich", a => { var me = ThisPlayer()!; me.Call("AddGold", 100000); Write("RICH gold=" + me.Call<int>("QueryGold") + "\n"); return true; });
        AddAction("rtdie", a => { ThisPlayer()!.Call("Die", (object?)null); return true; });
        AddAction("rtgive", a =>
        {
            var who = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "").ToLower() == a.Trim());
            if (who == null) { Write("GIVE nobody\n"); return true; }
            var anchor = CloneObject("/wiz/wren/anchor");
            try { MoveObject(anchor, who); Write("GIVE moved\n"); } catch (Exception e) { Write("GIVE refused " + e.Message + "\n"); }
            return true;
        });
    }
}
'''


def gold(dump):
    import re
    m = re.search(r"gold=(\d+)", dump)
    return int(m.group(1)) if m else -1


def clean(label, dump, real_gold):
    rtlib.check(f"F3 {label}: the checkpoint's gold, not the workshop's", gold(dump) == real_gold, dump)
    rtlib.check(f"F3 {label}: nothing made in the workshop is carried", "[wiz:" not in dump, dump)
    rtlib.check(f"F3 {label}: not left half-restored", "leaving=True" not in dump and "workshop=True" not in dump, dump)
    rtlib.check(f"F3 {label}: standing in the real game", "env=/wiz" not in dump and "env=none" not in dump, dump)


def enter(g, rich=True):
    g.wren.command("goto /wiz/wren/lab")
    g.wren.command("clone /wiz/wren/anchor")
    g.wren.command("clone /wiz/wren/wand")
    if rich:
        out = g.wren.command("rtrich")
        assert "RICH gold=" in out, out


def relogin(g):
    g.wren = h.Client("wren")
    g.wren.read_until("> ")


def restart(g, kill=False):
    if kill:
        g.proc.kill()
    g.proc.wait(30)
    g.log.close()
    g.start()
    g.chief = h.Client("chief")
    g.chief.read_until("> ")
    out = g.chief.command("clone /domains/rt/probe")
    assert "Cloned" in out, out
    relogin(g)


def main():
    g = rtlib.Game(wiz_files={"/wiz/wren/anchor": ANCHOR, "/wiz/wren/wand": WAND})
    g.start()
    try:
        g.setup_people(extra=(("rev", 5),))
        rev = g.others["rev"]
        real = gold(g.dump("wren"))
        print("wren's real gold:", real)

        enter(g)
        g.wren.command("rtdie")
        time.sleep(1.0)
        clean("death", g.dump("wren"), real)

        enter(g)
        g.wren.command("goto /domains/hollow/village/square")
        time.sleep(1.0)
        clean("goto", g.dump("wren"), real)

        enter(g)
        g.wren.command("quit", marker="Goodbye", timeout=10)
        time.sleep(1.0)
        relogin(g)
        clean("quit", g.dump("wren"), real)

        enter(g)
        g.wren.sock.close()
        time.sleep(1.5)
        relogin(g)
        clean("linkdeath", g.dump("wren"), real)

        rev_gold = gold(g.dump("rev"))
        g.wren.command("goto /wiz/wren/lab")
        g.wren.command("clone /wiz/wren/wand")
        rev.command("goto /wiz/wren/lab")
        given = g.wren.command("rtgive rev")
        print("give:", given.strip()[:200])
        inside = g.dump("rev")
        print("rev inside:", inside.strip())
        rev.command("out")
        time.sleep(1.0)
        after = g.dump("rev")
        print("rev after:", after.strip())
        rtlib.check("F3 visitor: the reviewer leaves with nothing of the workshop", "[wiz:" not in after and "leaving=True" not in after and "workshop=True" not in after, after)
        rtlib.check("F3 visitor: the reviewer is still connected", "offline" not in after and "Rev" in rev.command("score"), after)
        rtlib.check("F3 visitor: the reviewer's gold is untouched", gold(after) == rev_gold, after)
        g.wren.command("out")
        time.sleep(1.0)

        enter(g)
        g.chief.command("shutdown", timeout=10)
        restart(g)
        clean("shutdown", g.dump("wren"), real)

        enter(g)
        g.wren.send("out")
        time.sleep(0.05)
        restart(g, kill=True)
        clean("kill at the moment of leaving", g.dump("wren"), real)
        rtlib.check("F3: the driver is running", g.alive())
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
