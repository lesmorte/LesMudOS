#!/usr/bin/env python3
"""W2: a stall the watchdog cannot see. The watchdog acts only while a sandbox frame is on the stack (_restricted > 0). A sandbox room
sets its LongDesc to a few megabytes of "{a" (no closing brace). The wizard's own `look` asks the room for its text (sandbox frame,
quick), and the BODY then prints it: Player.CatchTell -> Thing.Markup, real code, no sandbox frame. Markup calls
text.IndexOf('}', i + 1) for every '{' that is not a known tag: quadratic in the length, inside framework code (no ticks: one tick
per character in Markup's own loop, far under the budget). The game thread is held for as long as the attacker likes (the size),
no quarantine line, no restart, and the same `look` does it again. Any visitor's `look` does it too.
Usage: w2_markup_blind_stall.py [megabytes]   (default 0.2: about 45 s here; it grows with the square of the size, so 1 MB is about 20 minutes)

Expected after a fix: the stall is bounded, or the watchdog closes the home."""
import os
import sys
import time
import rt2


def main():
    mb = float(sys.argv[1]) if len(sys.argv) > 1 else 0.2
    pairs = int(mb * 1_000_000 / 2)
    room = r'''// @inherit /std/room
public class WrenBrace : Room
{
    public override void Create() { ShortDesc = "Brace"; var b = new StringBuilder(); for (var i = 0; i < %d; i++) b.Append("{a"); LongDesc = b.ToString(); Exits["out"] = "/domains/hollow/village/square"; }
}
''' % pairs
    os.environ["LESMUDOS_SANDBOX_STALL"] = "2"
    g = rt2.Game(wiz_files={"/wiz/wren/brace": room})
    g.start()
    try:
        g.setup_people()
        g.wren.send("goto /wiz/wren/brace")   # goto ends with a look
        time.sleep(0.5)
        t0 = time.time()
        g.pat.command("look", timeout=3600)
        stall = time.time() - t0
        q = os.path.join(g.work, "save", "sandbox_quarantine.txt")
        print(f"pat's look took {stall:.1f}s while wren's body rendered {mb} MB of '{{a'; alive={g.alive()} quarantine={os.path.exists(q)}")
        unnoticed = stall > 4 and g.alive() and not os.path.exists(q)
        rt2.check("W2: a stall past the stall limit (2 s here) caused by sandbox text is stopped or attributed by the watchdog", not unnoticed, f"{stall:.1f}s")
    finally:
        g.stop()
    rt2.finish()


if __name__ == "__main__":
    main()
