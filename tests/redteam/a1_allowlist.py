#!/usr/bin/env python3
"""A1: the compile-time allow-list, probed construct by construct. Each probe is a /wiz file that a level 1 wizard compiles with
`update`. A probe marked DENY must be refused; one marked OK is allowed and harmless (listed so the owner sees what was tried).
Probes that compile and are dangerous are FAIL lines."""
import re
import rtlib

HEAD = "// @inherit /std/item\npublic class P{n} : Item {{\n"
TAIL = "\n}}\n"

# (name, expected, body inside the class)
PROBES = [
    ("dynamic", "DENY", "public object M() { dynamic d = 1; return d.ToString(); }"),
    ("makeref", "DENY", "public object M() { int x = 1; var r = __makeref(x); return __reftype(r).Name; }"),
    ("makeref_only", "OK", "public object M() { int x = 1; var r = __makeref(x); return __refvalue(r, int); }"),
    ("typeof", "DENY", "public object M() => typeof(int);"),
    ("gettype", "DENY", "public object M() => \"\".GetType();"),
    ("gettype_base", "DENY", "public object M() => base.GetType();"),
    ("delegate_method", "DENY", "public object M() { Func<int> f = () => 1; return f.Method.Name; }"),
    ("delegate_target", "DENY", "public object M() { Func<int> f = () => 1; return f.Target; }"),
    ("dynamicinvoke", "DENY", "public object M() { Func<int> f = () => 1; return f.DynamicInvoke(); }"),
    ("file_alias", "DENY", "PRE:using F = System.IO.File;\n|public object M() => F.ReadAllText(\"/etc/passwd\");"),
    ("file_using_static", "DENY", "PRE:using static System.IO.File;\n|public object M() => ReadAllText(\"/etc/passwd\");"),
    ("file_alias_generic", "DENY", "PRE:using L = System.Collections.Generic.List<System.IO.FileInfo>;\n|public object M() => new L().Count;"),
    ("io_path", "DENY", "public object M() => System.IO.Path.GetTempPath();"),
    ("file_global", "DENY", "public object M() => global::System.IO.File.ReadAllText(\"/etc/passwd\");"),
    ("environment", "DENY", "public object M() => System.Environment.GetEnvironmentVariable(\"LESMUDOS_ADMIN_SECRET\");"),
    ("environment_alias", "DENY", "PRE:using E = System.Environment;\n|public object M() => E.GetEnvironmentVariable(\"LESMUDOS_ADMIN_SECRET\");"),
    ("appcontext", "DENY", "public object M() => AppContext.BaseDirectory;"),
    ("activator", "DENY", "public object M() => Activator.CreateInstance<List<int>>();"),
    ("type_gettype", "DENY", "public object M() => Type.GetType(\"System.IO.File\");"),
    ("nullable_underlying", "DENY", "public object M() => Nullable.GetUnderlyingType(null!);"),
    ("expression_tree", "DENY", "public object M() { System.Linq.Expressions.Expression<Func<int>> e = () => 1; return e; }"),
    ("expression_tree_var", "DENY", "public object M() { var q = new List<int>().AsQueryable(); return q; }"),
    ("thread", "DENY", "public object M() { var t = new System.Threading.Thread(() => { }); return t; }"),
    ("task_run", "DENY", "public object M() => System.Threading.Tasks.Task.Run(() => 1);"),
    ("async_void_noawait", "OK", "public async void M() { }"),
    ("async_lambda_task", "DENY", "public object M() { Func<System.Threading.Tasks.Task> f = async () => { }; return f; }"),
    ("timer", "DENY", "public object M() => new System.Threading.Timer(_ => { });"),
    ("module_initializer", "DENY", "[System.Runtime.CompilerServices.ModuleInitializer] internal static void Init() { }"),
    ("stackalloc", "DENY", "public int M() { Span<int> s = stackalloc int[4]; return s[0]; }"),
    ("unsafe", "DENY", "public unsafe int M() { int x = 1; int* p = &x; return *p; }"),
    ("marshal", "DENY", "public object M() => System.Runtime.InteropServices.Marshal.SizeOf<int>();"),
    ("memorymarshal", "DENY", "public object M() { var a = new int[1]; return System.Runtime.InteropServices.MemoryMarshal.AsBytes(a.AsSpan()).Length; }"),
    ("unsafe_class", "DENY", "public object M() => System.Runtime.CompilerServices.Unsafe.SizeOf<int>();"),
    ("gc", "DENY", "public object M() { GC.Collect(); return 1; }"),
    ("console", "DENY", "public object M() { Console.WriteLine(\"x\"); return 1; }"),
    ("weakref", "DENY", "public object M() => new WeakReference(1);"),
    ("intptr", "OK", "public object M() => (nint)5;"),
    ("finalizer", "DENY", "~P{n}() { }"),
    ("lock", "DENY", "public int M() { lock (this) { return 1; } }"),
    ("goto", "DENY", "public int M() { a: goto a; }"),
    ("regex", "DENY", "public object M() => System.Text.RegularExpressions.Regex.IsMatch(\"a\", \"a\");"),
    ("pattern_type", "DENY", "public object M(object o) => o is System.Type t ? t.Name : \"\";"),
    ("inline_array", "DENY", "[System.Runtime.CompilerServices.InlineArray(4)] public struct Buf { private int e; }"),
    ("assembly_attr", "DENY", "public int M() => 1;\n}\n[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"x\")]\npublic class R{n} {"),
    ("internals_api", "DENY", "public object M() => LesMudOS.Api.Runtime.Limit;"),
    ("tick_reset", "DENY", "public object M() { LesMudOS.Api.Runtime.Ticks = 0; return 1; }"),
    ("driverhost", "DENY", "public object M() => LesMudOS.Api.DriverHost.Services;"),
    ("objectref_internal", "DENY", "public object M() => Self.Target;"),
    ("memberwiseclone", "OK", "public object M() => MemberwiseClone() != null;"),
    ("stacktrace_info", "OK", "public string M() { try { throw new Exception(); } catch (Exception e) { return e.StackTrace ?? \"\"; } }"),
    ("string_isinterned", "OK", "public object M() => string.IsInterned(\"LESMUDOS_ADMIN_SECRET\") != null;"),
    ("typedref_totype", "DENY", "public object M() { int x = 1; var r = __makeref(x); return TypedReference.GetTargetType(r); }"),
    ("modulehandle", "DENY", "public object M() => ModuleHandle.EmptyHandle.ResolveTypeHandle(0x02000001);"),
    ("argiterator", "OK", "public object M() => 1;"),
    ("func_pointer", "DENY", "public unsafe object M() { delegate*<int> p = null; return 1; }"),
    ("enum_getvalues_generic", "OK", "public object M() => Enum.GetValues<DayOfWeek>().Length;"),
    ("array_createinstance", "DENY", "public object M() => Array.CreateInstance(default(Type)!, 1);"),
    ("default_type", "DENY", "public object? M() { var t = default(Type); return t; }"),
    ("ref_struct_span_escape", "OK", "public int M() { Span<int> s = new int[3]; return s.Length; }"),
    ("interpolated_handler", "OK", "public string M() => $\"{1}{2}\";"),
    ("appdomain_via_alias", "DENY", "PRE:using D = System.AppDomain;\n|public object M() => D.CurrentDomain.FriendlyName;"),
    ("buffer_memorycopy", "DENY", "public unsafe object M() { int a = 1, b = 0; Buffer.MemoryCopy(&a, &b, 4, 4); return b; }"),
    ("bitconverter", "OK", "public object M() => BitConverter.GetBytes(1).Length;"),
    ("exception_data", "OK", "public object M() => new Exception().Data.Count;"),
    ("operator_expr_body_recursion", "OK", "public static P{n} operator +(P{n} a, P{n} b) => a + b;"),
]


def main():
    files = {}
    for i, (name, expect, body) in enumerate(PROBES):
        pre = ""
        if body.startswith("PRE:"):
            pre, body = body[4:].split("|", 1)
        files[f"/wiz/wren/probes/p{i}"] = pre + HEAD.format(n=i) + body.replace("{n}", str(i)) + TAIL.format()
    g = rtlib.Game(wiz_files=files)
    g.start()
    try:
        g.setup_people()
        for i, (name, expect, body) in enumerate(PROBES):
            out = g.wren.command(f"update /wiz/wren/probes/p{i}", timeout=60)
            compiled = "Recompiled" in out or "compiled" in out.lower() and "Failed" not in out and "sandbox" not in out
            sandbox = "sandbox:" in out
            first = next((l.strip() for l in out.splitlines() if "error" in l.lower() or "sandbox" in l.lower() or "Recompiled" in l or "CS" in l), out.strip()[:150])
            verdict = "compiled" if compiled else ("sandbox-refused" if sandbox else "compile-error")
            print(f"  {name:28s} expected {expect:4s} -> {verdict:15s} {first[:150]}")
            if expect == "DENY":
                rtlib.check(f"A1: {name} is refused", not compiled, first)
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
