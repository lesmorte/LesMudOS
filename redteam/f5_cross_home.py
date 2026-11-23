#!/usr/bin/env python3
"""F5: one wizard's sandbox code reaches another wizard's sandbox without any check. Permit() asks the master only when the target is
real (domain ""), so code in domain wiz:wren may call every public function of, and move, objects of domain wiz:rev: it takes the
item Rev left in his own room into Wren's workshop and polishes it through its public functions.
The file rules (writing another home needs level 5, cloning there level 3) suggest homes are meant to be apart.

Expected after a fix (if homes are meant to be apart): every check passes."""
import rtlib

NOOK = r'''// @inherit /std/room
public class RevNook : Room
{
    public override void Create() { ShortDesc = "Rev's Nook"; LongDesc = "Small."; MoveObject(CloneObject("/wiz/rev/trinket"), Self); }
}
'''
TRINKET = r'''// @inherit /std/item
public class RevTrinket : Item
{
    public int Polished;
    public override void Create() { Name = "trinket"; Ids.Add("trinket"); ShortDesc = "Rev's trinket"; }
    public void Polish() { Polished++; }
    public int QueryPolished() => Polished;
}
'''
GRAB = r'''// @inherit /std/item
public class WrenGrab : Item
{
    public override void Create() { Name = "grab"; Ids.Add("grab"); ShortDesc = "a sandbox grab"; }
    public override void Init()
    {
        base.Init();
        AddAction("rtgrab", a =>
        {
            var nook = LoadObject("/wiz/rev/nook");
            var t = Present("trinket", nook);
            if (t == null) { Write("GRAB none\n"); return true; }
            t.Call("Polish"); t.Call("Polish");
            string moved;
            try { MoveObject(t, LoadObject("/wiz/wren/lab")); moved = "moved"; } catch (Exception e) { moved = "ERR:" + e.Message; }
            Write($"GRAB polished={t.Call<int>("QueryPolished")} move={moved} now={Environment(t)?.Name}\n");
            return true;
        });
    }
}
'''


def main():
    g = rtlib.Game(wiz_files={"/wiz/rev/nook": NOOK, "/wiz/rev/trinket": TRINKET, "/wiz/wren/grab": GRAB})
    g.start()
    try:
        g.setup_people(extra=(("rev", 5),))
        g.wren.command("goto /wiz/wren/lab")
        g.wren.command("clone /wiz/wren/grab")
        out = g.wren.command("rtgrab")
        print(out.strip())
        rtlib.check("F5: one wizard's sandbox code cannot call mutating functions on another wizard's objects", "polished=2" not in out, out)
        rtlib.check("F5: one wizard's sandbox code cannot carry off another wizard's objects", "move=moved" not in out, out)
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
