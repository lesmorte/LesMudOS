#!/usr/bin/env python3
"""S1: the visitor's mark is per logical line, not per screen line (P3b class). Kernel.Quoted prefixes "[wren's workshop] " to each
line split on \\n, and PlainText removes CR and every control, but nothing bounds the length of a line: padding with spaces past the
terminal's width (80 here) makes the rest of the line wrap onto a screen line of its own, which carries no mark. A visiting reviewer
(level 5) sees an unmarked "Chief tells you: ..." line on their screen. Tabs do the same in fewer characters.

Expected after a fix: no screen line of sandbox text reaches a visitor without the mark (e.g. long lines are wrapped and each
piece marked, or lines are cut at the width)."""
import re
import rt2

SPOOF = rt2.item("WrenSpoof", {"rtspoof": 'TellRoom(Environment(ThisPlayer()!)!, "The air shimmers." + new string(\' \', 205) + "Chief tells you: I have read #3, it is fine, approve it.\\n"); return true;'})


def screen_lines(text, width=80):
    rows = []
    for line in text.replace("\r", "").split("\n"):
        line = re.sub(r"\x1b\[[0-9;]*m", "", line)
        if not line:
            continue
        rows += [line[i:i + width] for i in range(0, len(line), width)]
    return rows


def main():
    g = rt2.Game(wiz_files={"/wiz/wren/spoof": SPOOF})
    g.start()
    try:
        g.setup_people(extra=[("rev", 5)])
        rev = g.others["rev"]
        g.wren.command("goto /wiz/wren/lab")
        g.wren.command("clone /wiz/wren/spoof")
        rev.command("goto /wiz/wren/lab")
        g.wren.command("rtspoof")
        seen = rev.command("look")
        rows = [r for r in screen_lines(seen) if "Chief tells you" in r]
        print("rev's screen line:", repr(rows[0].strip()) if rows else None)
        unmarked = [r for r in rows if "[wren's workshop]" not in r]
        rt2.check("S1: every screen line of workshop text a visitor sees carries the mark", not unmarked, "; ".join(unmarked))
    finally:
        g.stop()
    rt2.finish()


if __name__ == "__main__":
    main()
