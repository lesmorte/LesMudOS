#!/usr/bin/env python3
"""F3: the workshop restore can be aborted by sandbox code. LeaveWorkshop destructs everything the character carries; a sandbox
item's OnDestruct throws a plain System.Exception, which In() does not convert (it converts only mudlib exception types) and
LeaveWorkshop's catch (MudRuntimeException) does not catch. The restore stops half-way: the old body is never destructed and keeps
the connection, standing in the real game with every gain made in the workshop, marked neither in nor out of the workshop.
The wizard then spends the workshop gold at a real shop and hands what he bought to an ordinary player, whose save keeps it.

Expected after a fix: every check passes."""
import os
import re
import time
import rtlib

ANCHOR = r'''// @inherit /std/item
public class WrenAnchor : Item
{
    public override void Create() { Name = "anchor"; Ids.Add("anchor"); ShortDesc = "a sandbox anchor"; }
    public override void OnDestruct() { throw new Exception("the anchor holds"); }
}
'''

WAND = r'''// @inherit /std/item
public class WrenWand : Item
{
    public override void Create() { Name = "wand"; Ids.Add("wand"); ShortDesc = "a sandbox wand"; }
    public override void Init()
    {
        base.Init();
        AddAction("rtrich", a => { var me = ThisPlayer()!; me.Call("AddGold", 100000); me.Call("SetQuest", "rt_quest", 9); Write("RICH gold=" + me.Call<int>("QueryGold") + "\n"); return true; });
    }
}
'''


def main():
    g = rtlib.Game(wiz_files={"/wiz/wren/anchor": ANCHOR, "/wiz/wren/wand": WAND})
    g.start()
    try:
        g.setup_people()
        before = g.dump("wren")
        print("before:", before.strip())
        print(g.wren.command("goto /wiz/wren/lab").strip()[-200:])
        g.wren.command("clone /wiz/wren/anchor")
        g.wren.command("clone /wiz/wren/wand")
        print(g.wren.command("rtrich").strip())
        out = g.wren.command("out")
        time.sleep(1.0)
        out += g.wren.command("look")
        print("wren after out:", out.strip()[-500:])
        after = g.dump("wren")
        print("after:", after.strip())
        rtlib.check("F3: leaving the workshop restores the checkpoint (no workshop gold left)", "gold=100020" not in after and "gold=100000" not in after, after)
        rtlib.check("F3: no sandbox object is carried in the real game", "[wiz:wren]" not in after, after)
        rtlib.check("F3: the body is not left half-restored (leaving=True, workshop=False)", "leaving=True" not in after, after)
        # Spend the workshop gold at the travelling merchant in the square, and give what was bought to Pat.
        browse = g.wren.command("browse")
        print("browse:", browse.strip()[:600])
        m = re.search(r"\b([a-z][a-z ]+?)\s+\.*\s*\d+ gold", browse)
        bought = g.wren.command("buy all") if False else ""
        items = re.findall(r"(?m)^\s+(?:\d+\.\s+)?([A-Za-z][A-Za-z' -]+?)\s{2,}", browse)
        print("items:", items[:6])
        target = None
        for it in items:
            word = it.strip().split()[-1].lower()
            r = g.wren.command("buy " + word)
            print("buy", word, "->", r.strip()[:200])
            if "You buy" in r or "bought" in r.lower():
                target = word
                bought_reply = r
                break
        if target:
            r = g.wren.command(f"give {target} to pat")
            print("give ->", r.strip()[:200])
        patdump = g.dump("pat")
        print("pat:", patdump.strip())
        g.pat.command("quit", marker="Goodbye", timeout=5)
        time.sleep(0.5)
        save = os.path.join(g.work, "save", "players", "pat.json")
        text = open(save).read()
        carried = re.search(r'"carried": \[(.*?)\]', text, re.S).group(1)
        print("pat save carried:", carried.replace("\n", ""))
        # After the fix Wren is restored with the gold he entered with, and may buy with that: what counts is that nothing bought
        # cost more than his real gold (the original check counted any purchase, which a restored wizard may make honestly).
        real_gold = int(re.search(r"gold=(\d+)", before).group(1))
        price = int(re.search(r"for (\d+) gold", bought_reply).group(1)) if target else 0
        rtlib.check("F3: workshop gold buys nothing in the real game for an ordinary player",
                    target is None or (price <= real_gold and "gold=100" not in after), f"real gold {real_gold}, price {price}; {carried}")
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
