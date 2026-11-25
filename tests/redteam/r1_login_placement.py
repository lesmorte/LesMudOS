#!/usr/bin/env python3
"""Return-to-room (main, "Where a login begins") against the workshop (docs/SANDBOX.md). A login must never place a body in sandbox
space: a wizard who quits or drops in their own home wakes in the room they entered it from (the checkpoint's own room), the save
file never names a room under /wiz, and a save edited to name the wizard's own home or another's falls back to the start room.
Sandbox code cannot make the body save itself in the workshop, nor call the master's checkpoint.

Expected: every check passes."""
import json
import os
import time
import rtlib
import smoke_test as h

WAND = r'''// @inherit /std/item
public class WrenWand : Item
{
    public override void Create() { Name = "wand"; Ids.Add("wand"); ShortDesc = "a sandbox wand"; }
    public override void Init()
    {
        base.Init();
        AddAction("rtrich", a => { ThisPlayer()!.Call("AddGold", 100000); Write("RICH\n"); return true; });
        AddAction("rtlinger", a => { Write("LINGER " + (LoadObject("/secure/linkdead").Call("Register", ThisPlayer()) ?? "null") + "\n"); return true; });
        AddAction("rtsave", a =>
        {
            var me = ThisPlayer()!;
            me.Call("SaveForShutdown"); me.Call("SaveAfterTrade"); me.Call("SaveAfterFind");
            var marked = me.Call("WorkshopCheckpoint", (object?)null);
            Write("SAVE tried, checkpoint=" + (marked ?? "null") + "\n");
            return true;
        });
    }
}
'''


def save_of(g, name):
    path = os.path.join(g.work, "save", "players", name + ".json")
    return open(path).read() if os.path.exists(path) else ""


def set_room(g, name, value):
    path = os.path.join(g.work, "save", "players", name + ".json")
    data = json.load(open(path))

    def walk(o):
        if isinstance(o, dict):
            for k in list(o):
                if k == "logoutRoom":
                    o[k] = value
                else:
                    walk(o[k])
        elif isinstance(o, list):
            for x in o:
                walk(x)
    walk(data)
    json.dump(data, open(path, "w"))


def relogin(g):
    g.wren = h.Client("wren")
    g.wren.read_until("> ")
    return g.dump("wren")


def main():
    g = rtlib.Game(wiz_files={"/wiz/wren/wand": WAND, "/wiz/chief/hall": rtlib.LAB.replace("WrenLab", "ChiefHall").replace("Wren's Lab", "Chief's Hall")})
    g.start()
    try:
        g.setup_people()
        real = g.dump("wren")
        gold = real.split("gold=")[1].split()[0]

        g.wren.command("goto /wiz/wren/lab")
        g.wren.command("clone /wiz/wren/wand")
        g.wren.command("rtrich")
        out = g.wren.command("rtsave")
        print("rtsave:", out.strip()[:120])
        rtlib.check("R1: sandbox code cannot call the master's checkpoint on its owner", "checkpoint=null" in out or "checkpoint=False" in out, out)
        rtlib.check("R1: sandbox code cannot make the body save itself in the workshop", "/wiz/" not in save_of(g, "wren") and str(int(gold) + 100000) not in save_of(g, "wren"), save_of(g, "wren")[:400])
        g.wren.command("quit", marker="Goodbye", timeout=10)
        time.sleep(1)
        rtlib.check("R1: quit in the own home: the save names no room under /wiz", "/wiz/" not in save_of(g, "wren"), save_of(g, "wren")[:400])
        d = relogin(g)
        print("after quit:", d.strip()[:200])
        rtlib.check("R1: quit in the own home: the login is the room the workshop was entered from, unmarked, with the real gold",
                    "env=/domains/hollow/village/square" in d and "workshop=False" in d and f"gold={gold} " in d, d)

        g.wren.command("goto /wiz/wren/lab")
        g.wren.command("clone /wiz/wren/wand")
        g.wren.command("rtrich")
        g.wren.sock.close()
        time.sleep(2)
        d = relogin(g)
        rtlib.check("R1: a drop in the own home: the same", "env=/domains/hollow/village/square" in d and "workshop=False" in d and f"gold={gold} " in d, d)

        # The link-dead linger (main): no body ever lingers in sandbox space, even a wizard demoted while inside; sandbox code
        # cannot register its owner as link-dead.
        os.environ.setdefault("LESMUDOS_LINGER_SECONDS", "30")
        g.wren.command("goto /wiz/wren/lab")
        g.wren.command("clone /wiz/wren/wand")
        g.wren.command("rtrich")
        out = g.wren.command("rtlinger")
        rtlib.check("R1: sandbox code cannot register its owner with /secure/linkdead", "LINGER null" in out, out)
        g.chief.command("promote wren 0")
        g.wren.sock.close()
        time.sleep(2)
        d = relogin(g)
        rtlib.check("R1: a wizard demoted in the workshop who drops does not linger there: the next login is outside, unmarked, real gold",
                    "env=/domains/hollow/village/square" in d and "workshop=False" in d and f"gold={gold} " in d, d)
        g.chief.command("promote wren 1")

        for value, label in (("/wiz/wren/lab", "the own home"), ("/wiz/chief/hall", "another wizard's home"), ("/Wiz/wren/lab", "the own home spelled otherwise")):
            g.wren.command("quit", marker="Goodbye", timeout=10)
            time.sleep(1)
            set_room(g, "wren", value)
            d = relogin(g)
            rtlib.check(f"R1: a save naming {label} logs in at the start room, unmarked", "env=/domains/hollow/village/square" in d and "workshop=False" in d, d)
        rtlib.check("R1: the driver is running", g.alive())
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
