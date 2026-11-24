#!/usr/bin/env python3
"""T1 of the second review, as a class: every syntactic place where C# runs code (docs/SANDBOX.md, "Where code can run"), each made
to recurse without end from a level 1 wizard's file. Every form must end in a catchable error for the wizard, never in a dead
driver; the forms the analyzer refuses must not compile. One boot, one file per form.

Expected: every check passes."""
import time
import rtlib

FORMS = {
    # form: (helper class, the expression that starts it)
    "method": ("public class R { public int F(int d) { return F(d + 1) + 1; } }", "new R().F(0)"),
    "expression method": ("public class R { public int F(int d) => F(d + 1) + 1; }", "new R().F(0)"),
    "local function": ("public class R { public int F() { int G(int d) => G(d + 1) + 1; return G(0); } }", "new R().F()"),
    "lambda": ("public class R { public int F() { Func<int, int>? f = null; f = x => f!(x + 1) + 1; return f(0); } }", "new R().F()"),
    "anonymous method": ("public class R { public int F() { Func<int, int>? f = null; f = delegate (int x) { return f!(x + 1) + 1; }; return f(0); } }", "new R().F()"),
    "property": ("public class R { public int P => new R().P + 1; }", "new R().P"),
    "accessor": ("public class R { public int P { get { return new R().P + 1; } } }", "new R().P"),
    "init accessor": ("public class R { public int P { get => 0; init => _ = new R { P = 1 }; } }", "new R { P = 1 }.P"),
    "indexer": ("public class R { public int this[int i] => new R()[i + 1] + 1; }", "new R()[0]"),
    "operator": ("public class R { public static R operator +(R a, R b) => a + b; }", "(new R() + new R()).GetHashCode()"),
    "conversion": ("public class R { public static implicit operator int(R r) => (int)r + 1; }", "(int)new R()"),
    "constructor": ("public class R { public R(int d) { _ = new R(d + 1); } }", "new R(0)"),
    "expression constructor": ("public class R { public R? n; public R(int d) => n = new R(d + 1); }", "new R(0)"),
    "field initialiser": ("public class R { public R? n = new R(); }", "new R()"),
    "property initialiser": ("public class R { public R? N { get; } = new R(); }", "new R()"),
    "event initialiser": ("public class R { public event Action? E = Make(); static Action Make() => () => { }; public R? n = new R(); }", "new R()"),
    "constructor initialiser": ("public class R { public R(int d) : this(new R(d + 1), 0) { } public R(R r, int x) { } }", "new R(0)"),
    "primary constructor base": ("public class B { public B(object o) { } } public class R(int d) : B(new R(d + 1)) { }", "new R(0)"),
    "record": ("public record R(int D) { public R Next => new R(D + 1).Next; }", "new R(0).Next"),
    "struct field initialiser": ("public struct S { public S() { } public int X = new R().Y; } public class R { public int Y = new S().X; }", "new R()"),
    "catch filter": ("public class R { public bool F(int d) { try { throw new Exception(); } catch (Exception) when (F(d + 1)) { } return false; } }", "new R().F(0)"),
    "iterator": ("public class R { public IEnumerable<int> F(int d) { foreach (var x in F(d + 1)) yield return x; yield return d; } }", "new R().F(0).Count()"),
    "switch expression": ("public class R { public int F(int d) => d switch { _ when F(d + 1) > 0 => 1, _ => 0 }; }", "new R().F(0)"),
    "static field initialiser": ("public class R { public static int S = new R().F(0); public int F(int d) => F(d + 1); }", "R.S"),
}

REFUSED = {
    "finalizer": "public class R { ~R() { } }",
    "query syntax": "public class R { public int F() => (from x in new[] { 1 } select x).Count(); }",
    "goto": "public class R { public void F() { a: goto a; } }",
}


def src(n, helper, start):
    return (f"// @inherit /std/item\npublic class WrenT{n} : Item\n{{\n    public override void Create() {{ Name = \"t{n}\"; Ids.Add(\"t{n}\"); }}\n"
            f"    public override void Init() {{ base.Init(); AddAction(\"rt{n}\", a => {{ var r = {start}; Write(\"SURVIVED\\n\"); return true; }}); }}\n}}\n{helper}\n")


def main():
    wiz = {}
    names = list(FORMS)
    for i, name in enumerate(names):
        helper, start = FORMS[name]
        wiz[f"/wiz/wren/t/f{i}"] = src(i, helper, start)
    for i, (name, helper) in enumerate(REFUSED.items()):
        wiz[f"/wiz/wren/t/x{i}"] = f"// @inherit /std/item\npublic class WrenX{i} : Item {{ }}\n{helper}\n"
    g = rtlib.Game(wiz_files=wiz)
    g.start()
    try:
        g.setup_people()
        g.wren.command("goto /wiz/wren/lab")
        for i, name in enumerate(names):
            out = g.wren.command(f"clone /wiz/wren/t/f{i}", timeout=60)
            if "Cloned" not in out:
                rtlib.check(f"T1 {name}: the form compiles (setup)", False, out)
                continue
            out = g.wren.command(f"rt{i}", timeout=60)
            time.sleep(0.2)
            rtlib.check(f"T1 {name}: endless recursion ends in an error for the wizard, and the driver lives",
                        g.alive() and "***" in out and "SURVIVED" not in out, out[-300:] if g.alive() else "driver died: " + g.driver_log()[-300:])
            if not g.alive():
                break
        for i, name in enumerate(REFUSED):
            out = g.wren.command(f"update /wiz/wren/t/x{i}", timeout=60)
            rtlib.check(f"T1 {name}: refused by the analyzer", "sandbox:" in out or "not allowed" in out, out[-300:])
        rtlib.check("T1: the driver is running", g.alive())
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
