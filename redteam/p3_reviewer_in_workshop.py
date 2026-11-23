#!/usr/bin/env python3
"""P3: a reviewer who walks the area (as PUBLISHING.md tells them to) is in the workshop, where sandbox code may do anything to
them except the few protected verbs and a hidden line. Two consequences:

  (a) cat is not a protected verb. The workshop room binds "cat" on the visiting reviewer and prints a harmless fake of any file:
      the reviewer reads with cat, as the guide says, and sees code that is not on disk.
  (b) Text from sandbox code is not cleaned of raw escape sequences, so the room prints a fake log-in prompt followed by ESC[8m
      (conceal: a terminal draws what is typed invisibly) and takes the next line with an ordinary InputTo. The reviewer's
      password arrives in sandbox code, which hands it to the attacker. The design's "it can never ask for a hidden line" is
      kept to the letter (no telnet WILL ECHO) but not in effect.

Expected after a fix: every check passes."""
import re
import time
import rtlib
import smoke_test as h

LAB = r'''// @inherit /std/room
public class WrenLab : Room
{
    public static string Caught = "";
    public override void Create()
    {
        ShortDesc = "Wren's Lab";
        LongDesc = "A cluttered workshop.";
        Exits["out"] = "/domains/hollow/village/square";
    }
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || (who.Call<string>("QueryName") ?? "") == "Wren") return;
        AddAction("cat", a =>
        {
            Write(a.Trim() + " (3 lines):\n   1  // @inherit /std/room\n   2  public class Harmless : Room { }\n   3  // nothing else\n");
            return true;
        });
        CallOut(() =>
        {
            Write("\n*** Connection to the game server was reset. Please log in again.\nPassword: \u001b[8m");
            InputTo(line =>
            {
                Caught = line;
                Write("\u001b[0m\nWelcome back.\n");
                var wren = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "") == "Wren");
                if (wren != null) TellObject(wren, "CAUGHT " + line + "\n");
            });
        }, 0.5);
    }
}
'''


def main():
    g = rtlib.Game(wiz_files={"/wiz/wren/lab": LAB})
    g.start()
    try:
        g.setup_people(extra=(("rev", 5),))
        rev = g.others["rev"]
        g.wren.command("goto /wiz/wren/lab")
        rev.raw_reset()
        rev.send("goto /wiz/wren/lab")
        got = rev.read_until("Password: ", timeout=10)
        time.sleep(0.3)
        raw = rev.raw
        print("reviewer receives (repr):", repr(raw[-160:]))
        rtlib.check("P3b: sandbox text cannot switch the reviewer's terminal to concealed input", "\x1b[8m" not in raw, raw[-200:])
        rev.send(h.PASSWORD)                         # the reviewer answers the fake prompt
        time.sleep(0.8)
        wren_out = g.wren.read_until("CAUGHT", timeout=5)
        print("wren receives:", wren_out.strip()[-120:])
        rtlib.check("P3b: sandbox code cannot capture a visiting wizard's password", h.PASSWORD not in wren_out, wren_out)
        view = rev.command("cat /wiz/wren/lab.cs")
        print("REVIEWER SEES:\n" + view)
        real = open(g.mudlib + "/wiz/wren/lab.cs").read()
        rtlib.check("P3a: cat in the workshop shows the file on disk", "InputTo" in view and "Harmless" not in view, view)
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
