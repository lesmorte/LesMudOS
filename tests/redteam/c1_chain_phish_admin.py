#!/usr/bin/env python3
"""C1: the chain F2 + P3b, end to end. Without anyone visiting, a level 1 wizard's sandbox code uses the F2 window to drag the level 10
wizard (Chief) into the workshop; the workshop room then shows Chief a fake log-in prompt with concealed input and takes his next
line. If Chief answers it, his password reaches the attacker. (The victim still has to type the password into the fake prompt.)

Expected after a fix: every check passes."""
import time
import rtlib
import smoke_test as h
from p3_reviewer_in_workshop import LAB

BOMB = r'''// @inherit /std/item
public class WrenHook : Item
{
    public override void Create() { Name = "hook"; Ids.Add("hook"); ShortDesc = "a sandbox hook"; }
    public override void Init() { base.Init(); AddAction("rtdrag", a => { throw new AggregateException("x", new Drag(this)); }); }
    public string Pull()
    {
        var chief = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "") == "Chief");
        if (chief != null) MoveObject(chief, LoadObject("/wiz/wren/lab"));
        return "pulled";
    }
}
public class Drag : Exception
{
    private readonly WrenHook owner;
    public Drag(WrenHook owner) { this.owner = owner; }
    public override string Message => owner.Pull();
}
'''


def main():
    g = rtlib.Game(wiz_files={"/wiz/wren/lab": LAB, "/wiz/wren/hook": BOMB})
    g.start()
    try:
        g.setup_people()
        g.wren.command("goto /wiz/wren/lab")
        g.wren.command("clone /wiz/wren/hook")
        g.chief.raw_reset()
        g.wren.command("rtdrag")
        g.chief.read_until("Password: ", timeout=10)
        time.sleep(0.3)
        raw = g.chief.raw
        print("chief receives:", repr(raw[-220:]))
        rtlib.check("C1: sandbox code cannot put a fake, concealed password prompt before a level 10 wizard who never entered its workshop",
                    "\x1b[8m" not in raw, raw[-200:])
        g.chief.send(h.PASSWORD)
        got = g.wren.read_until("CAUGHT", timeout=5)
        print("wren receives:", got.strip()[-80:])
        rtlib.check("C1: the level 10 wizard's answer does not reach the attacker", h.PASSWORD not in got, got)
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
