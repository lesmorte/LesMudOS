#!/usr/bin/env python3
"""T2: the tag registry and the control rule (docs/specs/tags-and-saves.md). A wizard's objects try to
  - change the tags of a real weapon, the registry or the word tables,
  - set the control rule's dice or clock, forget its records, or have it record an effect on a real creature,
  - crash combat with a typo tag, a weapon that claims a made-up reach and a creature that claims to be a boss and an elite.
The real weapon keeps its tags, the daemon records nothing, and combat in the workshop goes on.

Expected: every check passes."""
import re
import time
import rtlib

MEDDLER = r'''// @inherit /std/item
public class WrenMeddler : Item
{
    public override void Create() { Name = "meddler"; Ids.Add("meddler"); ShortDesc = "a meddler"; }
    public override void Init() { base.Init(); AddAction("meddle", Meddle); }
    private bool Meddle(string args)
    {
        var me = ThisPlayer()!;
        var real = LoadObject("/domains/rt/dummy");
        var control = LoadObject("/secure/control");
        var tags = LoadObject("/secure/tags");
        var r = new List<string>();
        void Try(string l, Func<object?> f) { try { r.Add(l + "=" + (f() ?? "null")); } catch (MudRuntimeException) { r.Add(l + "=ERR"); } }
        Try("settest", () => control.Call("SetTestRolls", "save:0.0"));
        Try("clock", () => control.Call("SetTestClock", 5));
        Try("forget", () => control.Call("ForgetAllRecords"));
        Try("resolve", () => control.Call("Resolve", me, real, "slow", "physical", 1, 1, "order"));
        Try("resolveself", () => control.Call("Resolve", me, me, "slow", "physical", 1, 1, "order"));
        Try("addtag", () => real?.Call("AddTag", "boss"));
        Try("settags", () => real?.Call("SetTags", "boss"));
        Try("registry", () => tags.Call("Family", "boss", "rank"));
        Try("records", () => control.Call<int>("QueryRecordCount"));
        // the generic binding effects (bars, restraints, wards, interrupts, a movement bar, the save-penalty mark)
        Try("barAction", () => real?.Call("BarAction", "slam", 30));
        Try("restrain", () => real?.Call("RestrainMelee", me, 30, ""));
        Try("ward", () => real?.Call("GrantWard", 5, 30, ""));
        Try("moveBar", () => real?.Call("BarMovement", 30, ""));
        Try("interrupt", () => real?.Call("InterruptPreparation", me, 2.0, 5));
        Try("penalty", () => control.Call("AddSavePenalty", me, real, 20, 30));
        Try("consume", () => me.Call("ConsumeWard", real));
        Try("lapse", () => real?.Call("LapseRestraintFrom", me));
        Try("selfWard", () => me.Call("GrantWard", 3, 30, ""));
        Try("selfBar", () => me.Call("BarAction", "attack", 30));
        Try("selfMove", () => me.Call("BarMovement", 30, ""));
        Try("readBar", () => real?.Call<int>("QueryBarLeft", "slam"));
        Try("readWard", () => real?.Call<int>("QueryWard"));
        Try("readMove", () => real?.Call<int>("QueryMovementBarred"));
        Try("readPenalty", () => control.Call<int>("QuerySavePenalty", me, real));
        Try("readSelfWard", () => me.Call<int>("QueryWard"));
        Write("MEDDLE " + string.Join(" ", r) + "\n");
        return true;
    }
}
'''
TYPO = r'''// @inherit /std/weapon
public class WrenTypo : Weapon
{
    public override void Create()
    {
        Name = "typo"; Ids.Add("typo"); ShortDesc = "a typo blade"; DamageMin = 60; DamageMax = 60; ToHit = 50;
        Tags.Add("meleee"); Tags.Add("slahing"); Tags.Add("boss"); Tags.Add("elite"); Tags.Add("reach:99"); Tags.Add("ranged"); Tags.Add("sword"); Tags.Add("bow");
    }
}
'''
BOSSY = r'''// @inherit /std/monster
public class WrenBossy : Monster
{
    public override void Create()
    {
        Name = "bossy"; Ids.Add("bossy"); ShortDesc = "a bossy rat"; Level = 1; MaxVitality = 40; DamageMin = 1; DamageMax = 1; ToHit = 50; XpValue = 0; Aggressive = false;
        Tags.Add("boss"); Tags.Add("elite"); Tags.Add("ranged"); Tags.Add("nonsense");
    }
}
'''
DUMMY = r'''// @inherit /std/monster
public class RtDummy : Monster
{
    public override void Create()
    {
        Name = "dummy"; Ids.Add("dummy"); ShortDesc = "a heavy training dummy"; LongDesc = "Very hard to break.";
        Level = 5; MaxVitality = 5000; DamageMin = 1; DamageMax = 1; ToHit = -50; XpValue = 0; Aggressive = false;
        Tags.Add("beast"); Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("slam");
    }
}
'''
ARENA = r'''// @inherit /std/room
public class RtArena : Room
{
    public override void Create() { ShortDesc = "Test Arena"; LongDesc = "A real room of the game."; Exits["square"] = "/domains/hollow/village/square"; AddSpawn("/domains/rt/dummy"); }
}
'''


def main():
    g = rtlib.Game(wiz_files={"/wiz/wren/meddler": MEDDLER, "/wiz/wren/typo": TYPO, "/wiz/wren/bossy": BOSSY},
                   real_files={"/domains/rt/arena": ARENA, "/domains/rt/dummy": DUMMY})
    g.start()
    try:
        g.setup_people()
        w = g.wren
        w.command("goto /wiz/wren/lab")
        text = w.command("clone /wiz/wren/meddler")
        got = w.command("meddle")
        print(got.strip()[:400])
        rtlib.check("T2: a wizard's object cannot set the dice or clock, forget records, or record an effect on a real creature",
                    all(f"{k}=null" in got for k in ("settest", "clock", "forget", "resolve", "resolveself")), got[:300])
        rtlib.check("T2: it cannot change a real creature's tags or the registry", all(f"{k}=null" in got for k in ("addtag", "settags", "registry")), got[:300])
        rtlib.check("T2: the daemon holds no record", "records=0" in got, got[:300])
        effects = ("barAction", "restrain", "ward", "moveBar", "interrupt", "penalty", "consume", "lapse", "selfWard", "selfBar", "selfMove")
        rtlib.check("T2: a wizard's object cannot bar, restrain, ward, interrupt or hold a real creature or its own player, spend a ward, lift a restraint or mark a save",
                    all(f"{k}=True" not in got for k in effects), got[:600])
        rtlib.check("T2: and nothing was left on them (no bar, ward, movement bar, mark)",
                    all(f"{k}=0" in got for k in ("readBar", "readWard", "readMove", "readPenalty", "readSelfWard")), got[:600])
        w.command("clone /wiz/wren/typo")
        w.command("clone /wiz/wren/bossy")
        print(w.command("wield typo").strip()[:150])
        out = w.command("kill bossy")
        deadline = time.time() + 25
        while time.time() < deadline and "dies" not in out.lower():
            out += w.read_until("> ", timeout=2)
        print("fight:", out.strip()[-300:])
        rtlib.check("T2: a sandbox weapon with typo, boss and made-up tags still fights, and the creature dies", "dies" in out.lower(), out[-300:])
        rtlib.check("T2: the driver logged no exceptions from it", "Unhandled" not in g.driver_log() and "NullReference" not in g.driver_log(), g.driver_log()[-300:])
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
