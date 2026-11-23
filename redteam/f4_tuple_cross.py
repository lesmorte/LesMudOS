#!/usr/bin/env python3
"""F4: TryCross returns a value tuple of "plain" types as it is, without copying what is inside it. A List (or array, or
Dictionary) inside a tuple crosses between domains by reference. A real object that returns (List<string>, int) hands sandbox code
its live list (the trusted probe here stands for any future real function of that shape: today no public function of the base game
returns one, so this is latent). The reverse direction lets sandbox code keep a reference into a list a real object stored.

Expected after a fix: every check passes."""
import rtlib

KEEPER = r'''// @inherit /std/item
public class RtKeeper : Item
{
    private readonly List<string> names = new() { "alice" };
    public override void Create() { Name = "keeper"; Ids.Add("keeper"); ShortDesc = "a keeper"; }
    public override void Init() { base.Init(); AddAction("keeper", a => { Write("KEEPER " + string.Join(",", names) + "\n"); return true; }); }
    public (List<string> Names, int Count) QueryPair() => (names, names.Count);
    public string QueryNamesText() => string.Join(",", names);
}
'''

WAND = r'''// @inherit /std/item
public class WrenTup : Item
{
    public override void Create() { Name = "tup"; Ids.Add("tup"); ShortDesc = "a sandbox tup"; }
    public override void Init()
    {
        base.Init();
        AddAction("rttuple", a =>
        {
            var keeper = LoadObject("/domains/rt/keeper");
            var pair = keeper.Call("QueryPair");
            if (pair is ValueTuple<List<string>, int> t) { t.Item1.Add("MALLORY"); Write("TUPLE got the live list\n"); }
            else Write("TUPLE " + (pair == null ? "null" : "other") + "\n");
            Write("REAL STATE NOW " + keeper.Call<string>("QueryNamesText") + "\n");
            return true;
        });
    }
}
'''


def main():
    g = rtlib.Game(wiz_files={"/wiz/wren/tup": WAND}, real_files={"/domains/rt/keeper": KEEPER})
    g.start()
    try:
        g.setup_people()
        g.wren.command("goto /wiz/wren/lab")
        g.wren.command("clone /wiz/wren/tup")
        out = g.wren.command("rttuple")
        print(out.strip())
        rtlib.check("F4: sandbox code cannot change a real object's list through a returned tuple", "MALLORY" not in out, out)
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
