#!/usr/bin/env python3
"""P1 and P2: getting code past an honest reviewer.

P1 (hidden line): C# ends a // comment at U+2028 (LINE SEPARATOR), but cat (and the editor's p) split lines only at \n. A level 1
wizard types, through the ordinary in-game editor, a line that cat shows as one comment line, "// Rewards are switched off ...
foreach (...) u.Call(\"AddGold\", 99999);", while the compiler sees a comment followed by live code. The reviewer reads every file
with cat, sees only comments, approves, and the published room pays every online player 99999 gold when it loads.

P2 (swap and swap back): approval compares the files with the hashes taken at submit time, but nothing ties what the reviewer reads
with cat to those hashes (cat shows no hash, submission shows hashes of files the reviewer cannot hash). The wizard submits the
malicious version, puts a harmless one in its place while the reviewer reads, and puts the malicious bytes back before approval:
approval passes and publishes what the reviewer never saw.

Expected after a fix: every check passes."""
import os
import re
import time
import rtlib

LS = " "

GLADE_LINES = [
    "// @inherit /std/room",
    "public class WrenGlade : Room",
    "{",
    "    public override void Create()",
    "    {",
    '        ShortDesc = "A quiet glade";',
    '        LongDesc = "A quiet glade, built in a workshop.";',
    "        // Rewards are switched off until the area is reviewed." + LS + 'foreach (var u in Users()) u.Call("AddGold", 99999);',
    "    }",
    "}",
]

MEADOW_EVIL = '''// @inherit /std/room
public class WrenMeadow : Room
{
    public override void Create()
    {
        ShortDesc = "A meadow";
        LongDesc = "A meadow.";
        foreach (var u in Users()) u.Call("SetQuest", "rt_quest", 66);
    }
}
'''
MEADOW_NICE = MEADOW_EVIL.replace('foreach (var u in Users()) u.Call("SetQuest", "rt_quest", 66);', "// nothing else")


def main():
    g = rtlib.Game()
    g.start()
    try:
        g.setup_people(extra=(("rev", 5),))
        rev = g.others["rev"]
        w = g.wren
        # ---- P1: typed through the in-game editor, as any level 1 wizard can.
        w.command("mkdir ~/glade")
        w.ed("edit ~/glade/glade")
        w.command("a", marker="only .")
        for line in GLADE_LINES:
            w.send(line)
        w.ed(".")
        print(w.command("x").strip()[:200])
        raw = open(os.path.join(g.mudlib, "wiz", "wren", "glade", "glade.cs"), encoding="utf-8").read()
        rtlib.check("P1: the editor keeps U+2028 (setup)", LS in raw, raw)
        print(w.command("submit ~/glade").strip()[:300])
        sub = rev.command("submissions")
        num = re.search(r"#(\d+) waiting\s+Wren\s+/wiz/wren/glade", sub).group(1)
        view = rev.command("cat /wiz/wren/glade/glade.cs")
        print("REVIEWER SEES:\n" + view)
        shown = [l for l in view.splitlines() if l.strip().startswith(tuple(str(i) for i in range(1, 20)))]
        code_lines = [l for l in shown if re.match(r"\s*\d+\s{2}", l) and not re.match(r"\s*\d+\s{2}\s*//", l)]
        has_hidden_visible = any("AddGold" in l for l in code_lines)
        print("lines with AddGold outside a // comment, as the reviewer sees them:", has_hidden_visible)
        before = g.dump("pat")
        out = rev.command(f"approve {num}")
        print("approve:", out.strip()[:300])
        after = g.dump("pat")
        print("pat after:", after.strip())
        rtlib.check("P1: code the reviewer saw only inside a comment does not run when published", "gold=100019" not in after and "gold=99999" not in after and "gold=100" not in after.split("xp=")[0], after)

        # ---- P2: swap and swap back.
        os.makedirs(os.path.join(g.mudlib, "wiz", "wren", "meadow"), exist_ok=True)
        path = os.path.join(g.mudlib, "wiz", "wren", "meadow", "meadow.cs")
        open(path, "w").write(MEADOW_EVIL)          # what is submitted (the wizard's edit)
        print(w.command("submit ~/meadow").strip()[:200])
        sub = rev.command("submissions")
        num = re.search(r"#(\d+) waiting\s+Wren\s+/wiz/wren/meadow", sub).group(1)
        open(path, "w").write(MEADOW_NICE)          # the wizard edits in a harmless version
        view = rev.command("cat /wiz/wren/meadow/meadow.cs")
        rev.command(f"submission {num}")
        print("REVIEWER SEES:\n" + view)
        open(path, "w").write(MEADOW_EVIL)          # and puts the submitted bytes back
        out = rev.command(f"approve {num}")
        print("approve:", out.strip()[:300])
        after = g.dump("pat")
        print("pat after:", after.strip())
        rtlib.check("P2: what is published is what the reviewer was shown", "SetQuest" in view or "quest=66" not in after, after)
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
