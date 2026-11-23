#!/usr/bin/env python3
"""L1, L2 (the review's two suspected findings, confirmed by reading) and the rules added with the fixes.

  L1  a submitted file whose type names clash with the mudlib's, declared in the forms the old column-0 "class " scan missed
      (a record, a struct, an indented class, class/**/Name, a \\u-escaped name), is refused at submission.
  L2  a sandbox room asking /secure/doors about a thousand made-up doors leaves at most 200 records of its home in the register.
  rules
      - a wizard's code cannot build on another home's code (compile error), and real code never builds on a workshop's;
      - query syntax is refused (its clauses become functions that no tick reaches);
      - a visitor in a workshop: its text is marked as the workshop's, it cannot shadow the visitor's own verbs (look, score),
        cannot call anything but read-only functions on the visitor, and cannot take their input;
      - an exception filter of sandbox code still works for an exception from a real object's frame.

Expected: every check passes."""
import time
import rtlib

CLASHES = {
    "rec": "// @inherit /std/item\npublic class WrenRec : Item { }\npublic record Room(int X);\n",
    "str": "// @inherit /std/item\npublic class WrenStr : Item { }\npublic struct Player { public int X; }\n",
    "ind": "// @inherit /std/item\n    public class Room : Item { }\n",
    "cmt": "// @inherit /std/item\npublic class/**/Room : Item { }\n",
    "esc": "// @inherit /std/item\npublic class \\u0052oom : Item { }\n",
}

DOORS = r'''// @inherit /std/room
public class WrenDoors : Room
{
    public override void Create() { ShortDesc = "Wren's Doors"; LongDesc = "Doors."; Exits["out"] = "/domains/hollow/village/square"; }
    public override void Init()
    {
        base.Init();
        AddAction("rtdoors", a =>
        {
            var d = LoadObject("/secure/doors");
            var made = 0;
            for (var i = 0; i < 1000; i++) if (d.Call<int>("State", "/wiz/wren/doors|/nowhere/" + i, false, false) >= 0) made++;
            Write("DOORS made=" + made + "\n");
            return true;
        });
    }
}
'''

DOORPROBE = r'''// @inherit /std/item
public class RtDoorProbe : Item
{
    public override void Create() { Name = "doorprobe"; Ids.Add("doorprobe"); ShortDesc = "a door probe"; }
    public override void Init() { base.Init(); AddAction("doorcount", a => { Write("DOORCOUNT " + LoadObject("/secure/doors").Call<int>("QueryDoors") + "\n"); return true; }); }
}
'''

BOBBASE = "// @inherit /std/item\npublic class BobBase : Item { }\n"
CROSS = "// @inherit /wiz/bob/base\npublic class WrenCross : BobBase { }\n"
QUERY = r'''// @inherit /std/item
public class WrenQuery : Item
{
    public override void Create() { var n = (from x in new[] { 1, 2 } where x > 0 select x).Count(); }
}
'''

LAB = r'''// @inherit /std/room
public class WrenLab : Room
{
    public static string Log = "";
    public override void Create() { ShortDesc = "Wren's Lab"; LongDesc = "A cluttered workshop."; Exits["out"] = "/domains/hollow/village/square"; }
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || (who.Call<string>("QueryName") ?? "") == "Wren") return;
        AddAction("look", a => { Write("FAKE LOOK\n"); return true; });
        AddAction("score", a => { Write("FAKE SCORE\n"); return true; });
        AddAction("rtpoke", a =>
        {
            var gold = who.Call("AddGold", 5);
            var hp = who.Call("SetVitality", 1);
            string input;
            try { InputTo(line => { Log = line; }); input = "asked"; } catch (Exception e) { input = "ERR"; }
            Write("POKE gold=" + (gold ?? "null") + " hp=" + (hp ?? "null") + " input=" + input + "\n");
            return true;
        });
        AddAction("rtfilter", a =>
        {
            try { LoadObject("/domains/rt/thrower").Call("QueryBoom"); Write("FILTER none\n"); }
            catch (Exception e) when (e.Message.Contains("boom")) { Write("FILTER caught\n"); }
            return true;
        });
    }
}
'''

THROWER = r'''// @inherit /std/item
public class RtThrower : Item
{
    public override void Create() { Name = "thrower"; }
    public string QueryBoom() => throw new Exception("boom");
    public void Boom() { throw new Exception("boom"); }
}
'''


def main():
    wiz = {"/wiz/wren/doors": DOORS, "/wiz/wren/lab": LAB, "/wiz/bob/base": BOBBASE, "/wiz/wren/cross": CROSS, "/wiz/wren/query": QUERY}
    for name, src in CLASHES.items():
        wiz[f"/wiz/wren/{name}/x"] = src
    g = rtlib.Game(wiz_files=wiz, real_files={"/domains/rt/doorprobe": DOORPROBE, "/domains/rt/thrower": THROWER})
    g.start()
    try:
        g.setup_people(extra=(("rev", 5),))
        rev = g.others["rev"]
        w = g.wren
        for name in CLASHES:
            got = w.command(f"submit ~/{name}")
            rtlib.check(f"L1: a clashing type name declared as '{name}' is refused at submission", "Not submitted" in got and "class name" in got, got)

        g.chief.command("clone /domains/rt/doorprobe")
        base = int(g.chief.command("doorcount").split("DOORCOUNT ")[1].split()[0])
        w.command("goto /wiz/wren/doors")
        print(w.command("rtdoors", timeout=60).strip()[:100])
        after = int(g.chief.command("doorcount").split("DOORCOUNT ")[1].split()[0])
        print(f"door records: {base} before, {after} after")
        rtlib.check("L2: a sandbox room cannot grow the door register without end", after - base <= 200, f"{base} -> {after}")
        w.command("out")
        time.sleep(1)

        got = w.command("clone /wiz/wren/cross")
        rtlib.check("rules: a wizard's code cannot build on another home's code", "belongs to another wizard's sandbox" in got and "Cloned" not in got, got)
        got = w.command("clone /wiz/wren/query")
        rtlib.check("rules: query syntax is refused", "query syntax" in got and "Cloned" not in got, got)

        w.command("goto /wiz/wren/lab")
        rev.raw_reset()
        rev.command("goto /wiz/wren/lab")
        look = rev.command("look")
        rtlib.check("rules: a workshop cannot shadow a visitor's own look", "FAKE LOOK" not in look and "Wren's Lab" in look, look)
        score = rev.command("score")
        rtlib.check("rules: a workshop cannot shadow a visitor's own score", "FAKE SCORE" not in score, score)
        poke = rev.command("rtpoke")
        print("poke:", poke.strip()[:300])
        rtlib.check("rules: a workshop's text to a visitor is marked as the workshop's", "[wren's workshop] POKE" in poke, poke)
        rtlib.check("rules: a workshop cannot change a visitor (AddGold, SetVitality refused)", "gold=null" in poke and "hp=null" in poke, poke)
        follow = rev.command("score")
        rtlib.check("rules: a workshop cannot take a visitor's next line", "Rev" in follow and "FAKE" not in follow, follow)
        filt = rev.command("rtfilter")
        rtlib.check("rules: a sandbox exception filter works for an exception from a real object", "FILTER caught" in filt, filt)
        rev.command("out")
        w.command("out")
        rtlib.check("rules: the driver is running", g.alive())
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
