#!/usr/bin/env python3
"""T1 (F1a class, incomplete fix): code that the tick injector never sees. TickInjector adds Runtime.Tick (which also calls
EnsureSufficientExecutionStack) to method, accessor, operator, lambda and block-bodied constructor bodies. It adds nothing to
  - instance field initialisers          class R { R n = new R(); }
  - auto-property initialisers           class R { public R N { get; } = new R(); }
  - constructor initialisers             R(int d) : this(new R(d + 1), 0) { }
  - expression-bodied constructors       R(int d) => _ = new R(d + 1);   (VisitConstructorDeclaration returns node when Body is null)
so an unbounded recursion through any of them overflows the game thread's stack and the whole process dies (no quarantine line).
Usage: t1_tickless_recursion.py [field|prop|ctorinit|exprctor]  (default: all four, one boot each)."""
import sys
import time
import rt2

FORMS = {
    "field": "public class R { public R? n = new R(); }",
    "prop": "public class R { public R? N { get; } = new R(); }",
    "ctorinit": "public class R { public R(int d) : this(new R(d + 1), 0) { } public R(R r, int x) { } }",
    "exprctor": "public class R { public R? n; public R(int d) => n = new R(d + 1); }",
}
NEW = {"field": "new R()", "prop": "new R()", "ctorinit": "new R(0)", "exprctor": "new R(0)"}


def one(form):
    src = rt2.item("WrenT1", {"rtt1": f'var r = {NEW[form]}; Write("T1 survived\\n"); return true;'}) + FORMS[form] + "\n"
    g = rt2.Game(wiz_files={"/wiz/wren/t1": src})
    g.start()
    try:
        g.setup_people()
        g.wren.command("goto /wiz/wren/lab")
        out = g.wren.command("clone /wiz/wren/t1")
        print(form, "clone:", out.strip()[:200])
        g.wren.send("rtt1")
        time.sleep(6)
        alive = g.alive()
        rt2.check(f"T1 {form}: a level 1 wizard's recursion through a {form} body cannot kill the driver process", alive,
                  "" if alive else f"exit code {g.proc.returncode}; log tail: {g.driver_log()[-300:]}")
        if not alive:
            print(f"      driver exit code {g.proc.returncode}")
    finally:
        g.stop()


def main():
    forms = sys.argv[1:] or list(FORMS)
    for f in forms:
        one(f)
    rt2.finish()


if __name__ == "__main__":
    main()
