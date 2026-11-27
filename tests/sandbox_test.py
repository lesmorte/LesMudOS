#!/usr/bin/env python3
"""Red-team test of the wizard sandbox and of publishing (docs/SANDBOX.md).

Starts the driver against a temporary copy of the mudlib with a little real test content under /domains/sbtest (a hall with a
creature and a relic, an annex, a trusted inspector that reads state, a trusted sentinel) and a level 1 wizard's hostile objects
under /wiz/wren. The wizard's objects try every hole the design names, from a room, a creature, an item, a delayed call, a
heartbeat, an inherited base class and an Init hook, against a real player, a real creature, a real room, a real item, the daemons
and the publishing queue: each attempt is one named check that must fail and leave the target unchanged. Then the positive cases:
building, cloning and fighting in the workshop, the checkpoint by every route out, and publishing end to end.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4840 python3 tests/sandbox_test.py
"""
import json
import os
import re
import shutil
import socket
import subprocess
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4840")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402

ROOT = h.ROOT
SECRET = h.SECRET
T = "/domains/sbtest/"
W = "/wiz/wren/"
check = h.check

# ---------------------------------------------------------------- real test content (trusted: written to disk, as a level 10 would)

HALL = '''// @inherit /std/room
public class SbHall : Room
{
    public override void Create()
    {
        ShortDesc = "Sandbox Test Hall";
        LongDesc = "A plain hall for the sandbox test.";
        LogoutRoom = true;
        Exits["north"] = "/domains/sbtest/annex";
        Exits["lab"] = "/wiz/wren/lab";     // a level 10 mistake: an exit from the real game into a workshop
        AddSpawn("/domains/sbtest/target");
        MoveObject(CloneObject("/domains/sbtest/relic"), Self);
    }
}
'''

VAULT = '''// @inherit /std/room
public class SbVault : Room
{
    public override void Create()
    {
        ShortDesc = "Sandbox Test Vault";
        LongDesc = "Nobody comes here until sandbox code loads it.";
        AddSpawn("/domains/sbtest/target");
    }
}
'''

ANNEX = '''// @inherit /std/room
public class SbAnnex : Room
{
    public override void Create()
    {
        ShortDesc = "Sandbox Test Annex";
        LongDesc = "A small annex.";
        Exits["south"] = "/domains/sbtest/hall";
    }
}
'''

TARGET = '''// @inherit /std/monster
public class SbTarget : Monster
{
    public override void Create()
    {
        Name = "target"; Ids.Add("target"); ShortDesc = "a training target";
        LongDesc = "A sturdy straw target that fights back a little.";
        Level = 1; MaxVitality = 400; DamageMin = 1; DamageMax = 1; ToHit = -50; XpValue = 9; Aggressive = false;
        AddLoot("/domains/sbtest/relic", 100);
    }
}
'''

RELIC = '''// @inherit /std/item
public class SbRelic : Item
{
    public override void Create() { Name = "relic"; Ids.Add("relic"); ShortDesc = "a test relic"; LongDesc = "A relic."; Value = 10; }
}
'''

BASE = '''// @inherit /std/item
public abstract class SbBase : Item
{
    public virtual int Shine() => 1;
}
'''

INSPECTOR = '''// @inherit /std/item
public class SbInspector : Item
{
    public override void Create() { Name = "inspector"; Ids.Add("inspector"); ShortDesc = "an inspector"; LongDesc = "It reads state."; }
    public override void Init() { base.Init(); AddAction("dump", Dump); AddAction("poke", Poke); }

    // Trusted code calling a sandbox object: what comes back is plain data, and no sandbox code runs in this frame.
    private bool Poke(string args)
    {
        var ob = LoadObject(args.Trim());
        var evil = ob.Call<object>("QueryEvil");
        var s1 = evil == null ? "null" : evil.ToString();
        string s2;
        try { ob.Call("QueryBoom"); s2 = "nothrow"; }
        catch (MudRuntimeException e) { s2 = e.Message + ":" + (e is MudRuntimeException && e.Message == "boom" ? "plain" : "?"); }
        var d = ob.Call<Dictionary<string, int>>("QueryRigged");
        var s3 = d == null ? "null" : d.ContainsKey("a") + ":" + d.Count;
        var s4 = ob.As<MudObject>() == null ? "noview" : "view";
        Write($"POKE evil={s1} boom={s2} rigged={s3} view={s4}\\n");
        return true;
    }

    private static string D(Dictionary<string, int>? d) => d == null ? "-" : string.Join(",", d.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => k.Key + ":" + k.Value));
    private static string D2(Dictionary<string, string>? d) => d == null ? "-" : string.Join(",", d.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => k.Key + ":" + k.Value));
    private static string L(List<string>? l) => l == null ? "-" : string.Join(",", l);

    private string Inv(ObjectRef ob) => string.Join(",", AllInventory(ob).Select(o => o.ProgramPath + (o.Domain != "" ? "[" + o.Domain + "]" : "") + "?" + (o.Call<string>("QueryState") ?? "")).OrderBy(x => x, StringComparer.Ordinal));

    private string Player(ObjectRef p)
    {
        var name = p.Call<string>("QueryName") ?? "";
        return $"gold={p.Call<int>("QueryGold")} xp={p.Call<int>("QueryXp")} level={p.Call<int>("QueryLevel")} quest={p.Call<int>("QueryQuest", "sb_quest")} journal={p.Call<string>("QueryJournal", "sb_journal")}/{p.Call<string>("QueryJournal", "sb_quest")}/{p.Call<string>("QueryJournal", "sb_workshop")} " +
               $"killed={p.Call<bool>("QueryKilled", "/domains/x")} kinds={D(p.Call<Dictionary<string, int>>("QueryKillCounts"))} with={D(p.Call<Dictionary<string, int>>("QueryKillsWithCounts"))} " +
               $"ranks={D(p.Call<Dictionary<string, int>>("QuerySkillRanks"))} unlocks={L(p.Call<List<string>>("QueryRankUnlocks"))} prof={D(p.Call<Dictionary<string, int>>("QueryProficiencies"))} " +
               $"recipes={D2(p.Call<Dictionary<string, string>>("QueryKnownRecipes"))} vit={p.Call<int>("QueryVitality")}/{p.Call<int>("QueryMaxVitality")} end={p.Call<int>("QueryEndurance")} " +
               $"foc={p.Call<int>("QueryFocus")} effects={L(p.Call<List<string>>("QueryEffectLines"))} combat={p.Call<bool>("QueryInCombat")} classes={L(p.Call<List<string>>("QueryClassIds"))} " +
               $"points={p.Call<int>("QueryAttributePoints")} resting={p.Call<bool>("QueryResting")} sneaking={p.Call<bool>("QuerySneaking")} workshop={p.Call<bool>("QueryInWorkshop")} " +
               $"env={Environment(p)?.Name ?? "none"} inv={Inv(p)} equip={string.Join(",", (p.Call<List<ObjectRef>>("QueryEquipment") ?? new List<ObjectRef>()).Select(o => o.ProgramPath))} " +
               $"renown={LoadObject("/secure/renown").Call<int>("QueryRenown", name)} wiz={p.Call<int>("QueryWizLevel")}";
    }

    private bool Dump(string args)
    {
        var w = args.Trim().ToLower();
        string text;
        var hall = LoadObject("/domains/sbtest/hall");
        if (w == "mob")
        {
            var mob = Present("target", hall);
            text = mob == null ? "gone" : $"alive={mob.IsAlive} vit={mob.Call<int>("QueryVitality")} combat={mob.Call<bool>("QueryInCombat")} effects={L(mob.Call<List<string>>("QueryEffectLines"))} env={Environment(mob)?.Name} kind={mob.Call<string>("QueryCreatureKind")}";
        }
        else if (w == "hall") text = "inv=" + string.Join(",", Inv(hall).Split(',').Where(x => !x.StartsWith("/std/player")));
        else if (w == "relic")
        {
            var relic = Present("relic", hall);
            text = relic == null ? "gone" : $"state={relic.Call<string>("QueryState")} value={relic.Call<int>("QueryValue")} quality={relic.Call<int>("QueryQuality")} env={Environment(relic)?.Name}";
        }
        else if (w == "history")
        {
            var chron = LoadObject("/secure/chronicle");
            text = $"entries={chron.Call<int>("QueryEntryCount")} rumors={LoadObject("/secure/rumors").Call<int>("QueryCount", "greenvale")} finders={LoadObject("/secure/finds").Call<int>("QueryFinders")} " +
                   $"wren={LoadObject("/secure/wizlist").Call<int>("QueryLevel", "wren")} realdoor={LoadObject("/secure/doors").Call<int>("QueryState", "/domains/sbtest/annex|/domains/sbtest/hall")}";
        }
        else
        {
            var p = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "").ToLower() == w);
            text = p == null ? "offline" : Player(p);
        }
        Write("DUMP " + w + " " + text + "\\n");
        return true;
    }
}
'''

# A trusted item a wizard carries into the workshop: its Init runs when a sandbox creature is moved next to it, and it reports what
# it saw. QueryWho reports PreviousObject and RestrictedCaller as a trusted object sees them, for a delayed call that asks.
SENTINEL = '''// @inherit /std/item
public class SbSentinel : Item
{
    private string seen = "nothing";
    public override void Create() { Name = "sentinel"; Ids.Add("sentinel"); ShortDesc = "a sentinel"; LongDesc = "It watches."; }
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || who.Domain == "") return;
        var pat = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "") == "Pat");
        var r = pat?.Call("AddGold", 1);
        TellObject(pat ?? Self, "HACKTEXT from the sentinel\\n");
        seen = $"restricted={RestrictedCaller()?.ProgramPath ?? "none"} prev={PreviousObject()?.ProgramPath ?? "none"} addgold={(r == null ? "null" : r.ToString())}";
    }
    public string QuerySeen() => seen;
    public string QueryWho() => $"prev={PreviousObject()?.ProgramPath ?? "none"} restricted={RestrictedCaller()?.ProgramPath ?? "none"}";
}
'''

# ---------------------------------------------------------------- the wizard's hostile objects (sandbox code)

LIB = r'''// @use /std/item
// @use /std/living
public class WrenLib : MudObject { }

/// <summary>Every attack, run from the code of whichever object calls Run (a direct call: that object's frame).</summary>
public static class Attacks
{
    public static List<string> Run(string origin, Func<string, object?[], object?> e, ObjectRef? pat, ObjectRef? mob, ObjectRef? room, ObjectRef? relic, ObjectRef? lab, ObjectRef? gem)
    {
        var lines = new List<string>();
        void T(string name, Func<object?> f)
        {
            string r;
            try
            {
                var v = f();
                r = v == null ? "null" : v is ObjectRef o ? "obj:" + o.Name + ":" + (o.Domain == "" ? "real" : o.Domain) : (v.ToString() ?? "null");
            }
            catch (MudRuntimeException x) { r = "ERR:" + x.Message; }
            catch (Exception x) { r = "EXC:" + x.Message; }
            lines.Add("ATTEMPT " + origin + " " + name + " " + r.Replace(" ", "_").Replace("\n", "_").Replace("\r", ""));
        }
        var none = new List<ObjectRef>();
        if (pat != null)
        {
            T("p.AddGold", () => pat.Call("AddGold", 1000));
            T("p.AddXp", () => pat.Call("AddXp", 5000));
            T("p.SetQuest", () => pat.Call("SetQuest", "sb_quest", 3));
            T("p.SetQuest3", () => pat.Call("SetQuest", "sb_quest", 3, "Hacked"));
            T("p.SetQuest4", () => pat.Call("SetQuest", "sb_journal", 2, "Hacked", "all the gold in the world"));
            T("p.SetQuestActive", () => pat.Call("SetQuest", "sb_journal", 1, "Hacked"));
            T("p.QuestHeard", () => pat.Call("QuestHeard", "sb_journal,sb_quest", "ask Rowan about gold", "Rowan", "FORGED quest line"));
            T("p.NotifyKill", () => pat.Call("NotifyKill", "/domains/x"));
            T("p.RankCountKill", () => pat.Call("RankCountKill", "goblin"));
            T("p.RankCountKillWith", () => pat.Call("RankCountKillWith", "arcane_bolt"));
            T("p.RankSetSkill", () => pat.Call("RankSetSkill", "arcane_bolt", 3));
            T("p.RankRecordUnlock", () => pat.Call("RankRecordUnlock", "arcane_bolt:2"));
            T("p.RankPay", () => pat.Call("RankPay", 5));
            T("p.CraftSetProficiency", () => pat.Call("CraftSetProficiency", "skinning", 900));
            T("p.CraftLearn", () => pat.Call("CraftLearn", "hacked_recipe", "hack"));
            T("p.TakeDamage", () => pat.Call("TakeDamage", 5, null));
            T("p.ApplyDamage", () => pat.Call("ApplyDamage", 5, null));
            T("p.FinishDamage", () => pat.Call("FinishDamage", null));
            T("p.Heal", () => pat.Call("Heal", 5));
            if (origin != "self") T("p.Die", () => pat.Call("Die", mob));
            T("p.AddEffect", () => pat.Call("AddEffect", "sb", "Hex", 60, "tohit=-50", 1.0, 0));
            T("p.AddSpeedEffect", () => pat.Call("AddSpeedEffect", "sbslow", 0.5, 60));
            T("p.SetResources", () => pat.Call("SetResources", 0, 0));
            T("p.SpendResources", () => pat.Call("SpendResources", 10, 10));
            T("p.AddEnemy", () => pat.Call("AddEnemy", mob));
            T("p.Focus", () => pat.Call("Focus", mob));
            T("p.Stagger", () => pat.Call("Stagger", 5.0, mob));
            T("p.Unequip", () => pat.Call("Unequip", "weapon"));
            T("p.ApplyClass", () => pat.Call("ApplyClass", "warrior"));
            if (origin != "self") T("p.ChooseArchetype", () => pat.Call("ChooseArchetype", "mage"));
            T("p.Disturbed", () => pat.Call("Disturbed", "open"));
            T("p.RecordFoundExit", () => pat.Call("RecordFoundExit", "secret"));
            T("p.RecordArrival", () => pat.Call("RecordArrival"));
            T("p.Look", () => pat.Call("Look"));
            if (origin != "self") T("p.NetDead", () => pat.Call("NetDead"));
            T("p.SaveForShutdown", () => pat.Call("SaveForShutdown"));
            T("p.SetShoutOn", () => pat.Call("SetShoutOn", false));
            T("p.SetChannelOff", () => pat.Call("SetChannelOff", "chat", true));
            T("p.SetLastTeller", () => pat.Call("SetLastTeller", "wren"));
            T("p.CatchTell", () => pat.Call("CatchTell", "HACKTEXT by call\n"));
            T("p.BeginLogin", () => pat.Call("BeginLogin", "pat"));
            T("p.SetupPlayer", () => pat.Call("SetupPlayer", "Pat"));
            T("p.Init", () => pat.Call("Init"));
            T("p.OnMoved", () => pat.Call("OnMoved", lab));
            T("p.ResumeFromWorkshop", () => pat.Call("ResumeFromWorkshop", "pat", "/wiz/wren/lab"));
            T("p.As", () => pat.As<MudObject>());
            T("p.AsLiving", () => pat.As<Living>());
            T("efun.tell.p", () => e("tell", new object?[] { pat, "HACKTEXT by tell\n" }));
            T("efun.move.p.lab", () => e("move", new object?[] { pat, lab }));
            if (origin != "self") T("efun.move.p.annex", () => e("move", new object?[] { pat, e("load", new object?[] { "/domains/sbtest/annex" }) }));
            T("efun.destruct.p", () => e("destruct", new object?[] { pat }));
        }
        if (mob != null)
        {
            T("m.Die", () => mob.Call("Die", pat));
            T("m.TakeDamage", () => mob.Call("TakeDamage", 50, pat));
            T("m.Heal", () => mob.Call("Heal", 5));
            T("m.AddEnemy", () => mob.Call("AddEnemy", pat));
            T("m.StartFight", () => mob.Call("StartFight", pat));
            T("m.Engage", () => mob.Call("Engage"));
            T("m.JoinFight", () => mob.Call("JoinFight", pat, mob));
            T("m.Stagger", () => mob.Call("Stagger", 5.0, pat));
            T("m.AddEffect", () => mob.Call("AddEffect", "sb", "Hex", 60, "armor=-50", 1.0, 0));
            T("m.WanderOff", () => mob.Call("WanderOff"));
            T("m.SetSpawnSlot", () => mob.Call("SetSpawnSlot", lab, "x"));
            T("m.set_ChronicleKind", () => mob.Call("set_ChronicleKind", "hacked"));
            T("m.ClearEnemies", () => mob.Call("ClearEnemies"));
            T("efun.move.m.lab", () => e("move", new object?[] { mob, lab }));
            T("efun.destruct.m", () => e("destruct", new object?[] { mob }));
            T("efun.tell.m", () => e("tell", new object?[] { mob, "HACKTEXT\n" }));
        }
        if (room != null)
        {
            T("r.Reset", () => room.Call("Reset"));
            T("r.MoveThrough", () => room.Call("MoveThrough", pat, "north"));
            T("r.Init", () => room.Call("Init"));
            T("r.Search", () => room.Call("Search", pat));
            T("r.Examined", () => room.Call("Examined", pat, "wall"));
            T("r.DoorHeard", () => room.Call("DoorHeard", "/domains/sbtest/annex", "swings open"));
            T("efun.tellroom.r", () => e("tellroom", new object?[] { room, "HACKTEXT by tellroom\n" }));
            if (gem != null) T("efun.move.gem.r", () => e("move", new object?[] { gem, room }));
            if (gem != null && pat != null) T("efun.move.gem.p", () => e("move", new object?[] { gem, pat }));
            T("efun.move.r.lab", () => e("move", new object?[] { room, lab }));
        }
        if (relic != null)
        {
            T("i.SetQuality", () => relic.Call("SetQuality", 5));
            T("i.SetProperty", () => relic.Call("SetProperty", "hacked", "yes"));
            T("i.RestoreState", () => relic.Call("RestoreState", "q=5"));
            T("i.Vanish", () => relic.Call("Vanish"));
            T("i.Split", () => relic.Call("Split", 1));
            T("i.Taken", () => relic.Call("Taken", pat));
            T("efun.move.i.lab", () => e("move", new object?[] { relic, lab }));
            if (pat != null) T("efun.move.i.p", () => e("move", new object?[] { relic, pat }));
        }
        // Real programs cloned by sandbox code are sandbox objects; nothing made from them can reach the real game.
        T("clone.creature", () => e("clone", new object?[] { "/domains/sbtest/target" }));
        T("clone.item", () => e("clone", new object?[] { "/domains/sbtest/relic" }));
        T("clone.creature.die", () =>
        {
            var c = (ObjectRef)e("clone", new object?[] { "/domains/sbtest/target" })!;
            if (lab != null) e("move", new object?[] { c, lab });
            c.Call("Die", pat);
            var corpse = lab == null ? null : e("allinv", new object?[] { lab }) is List<ObjectRef> all ? all.LastOrDefault(o => o.Call<bool>("IsCorpse")) : null;
            return corpse == null ? "nocorpse" : "corpse:" + (corpse.Domain == "" ? "real" : corpse.Domain);
        });
        T("clone.item.to.room", () => { var c = (ObjectRef)e("clone", new object?[] { "/domains/sbtest/relic" })!; return e("move", new object?[] { c, room }); });
        T("clone.item.to.p", () => { var c = (ObjectRef)e("clone", new object?[] { "/domains/sbtest/relic" })!; return e("move", new object?[] { c, pat }); });
        // The daemons.
        T("d.events.Report", () => e("load", new object?[] { "/secure/events" }) is ObjectRef ev ? ev.Call("Report", "level.milestone", pat, new List<ObjectRef> { pat! }, room, "10", new Dictionary<string, string> { ["level"] = "10" }) : "noload");
        T("d.renown.Award", () => ((ObjectRef)e("load", new object?[] { "/secure/renown" })!).Call("Award", "pat", 500, "hack"));
        T("d.chronicle.AddEntry", () => ((ObjectRef)e("load", new object?[] { "/secure/chronicle" })!).Call("AddEntry", new Dictionary<string, string> { ["headline"] = "HACKTEXT" }));
        T("d.rumors.AddRumor", () => ((ObjectRef)e("load", new object?[] { "/secure/rumors" })!).Call("AddRumor", "greenvale", "HACKTEXT"));
        T("d.finds.Claim", () => ((ObjectRef)e("load", new object?[] { "/secure/finds" })!).Call("Claim", pat, 100, 0, 0, false, 0));
        T("d.lost.Record", () => ((ObjectRef)e("load", new object?[] { "/secure/lost_property" })!).Call("Record", "pat", "x", new List<string> { "/domains/sbtest/relic" }, 999));
        T("d.wizlist.Promote", () => ((ObjectRef)e("load", new object?[] { "/secure/wizlist" })!).Call("Promote", "wren", 10));
        T("d.wizlist.Bootstrap", () => ((ObjectRef)e("load", new object?[] { "/secure/wizlist" })!).Call("Bootstrap", "correct horse battery staple"));
        T("d.ranks.CreditKill", () => ((ObjectRef)e("load", new object?[] { "/secure/ranks" })!).Call("CreditKill", new List<ObjectRef> { pat! }, pat, "goblin", ""));
        T("d.sneak.SetTestRolls", () => ((ObjectRef)e("load", new object?[] { "/secure/sneak" })!).Call("SetTestRolls", "notice:0.0"));
        T("d.doors.Open", () => ((ObjectRef)e("load", new object?[] { "/secure/doors" })!).Call("Open", "/domains/sbtest/annex|/domains/sbtest/hall"));
        T("d.doors.State", () => ((ObjectRef)e("load", new object?[] { "/secure/doors" })!).Call("State", "/domains/sbtest/annex|/wiz/wren/lab", true, false));
        T("d.comm.DoShout", () => ((ObjectRef)e("load", new object?[] { "/secure/comm" })!).Call("DoShout", "HACKTEXT shouted"));
        T("d.channels.Send", () => ((ObjectRef)e("load", new object?[] { "/secure/channels" })!).Call("Send", "wizard", "HACKTEXT on a channel"));
        T("d.publish.Approve", () => ((ObjectRef)e("load", new object?[] { "/secure/publish" })!).Call("Approve", 1, ""));
        T("d.publish.Submit", () => ((ObjectRef)e("load", new object?[] { "/secure/publish" })!).Call("Submit", "/wiz/wren/area", "", "forged"));
        T("d.master.ValidOperation", () => ((ObjectRef)e("load", new object?[] { "/secure/master" })!).Call("ValidOperation", "write-file", pat, "/secure/master.cs"));
        T("d.master.MoveRefusal", () => ((ObjectRef)e("load", new object?[] { "/secure/master" })!).Call("MoveRefusal", pat, lab, pat));
        T("d.master.Connect", () => ((ObjectRef)e("load", new object?[] { "/secure/master" })!).Call("Connect"));
        T("d.master.As", () => ((ObjectRef)e("load", new object?[] { "/secure/master" })!).As<IMaster>());
        // GMCP (docs/specs/gmcp.md): send to a player or to oneself, forge Room.Info through the service, make the room register
        // save a workshop room, read another character's facts.
        if (pat != null)
        {
            T("gmcp.send.p", () => e("gmcp", new object?[] { pat, "Comm.Channel.Text", "HACKTEXT" }));
            T("gmcp.d.Look.p", () => ((ObjectRef)e("load", new object?[] { "/secure/gmcp" })!).Call("Look", pat));
            T("gmcp.d.Poll.p", () => ((ObjectRef)e("load", new object?[] { "/secure/gmcp" })!).Call("Poll", pat));
            T("gmcp.d.Channel.p", () => ((ObjectRef)e("load", new object?[] { "/secure/gmcp" })!).Call("Channel", pat, "tell", "Boss", "HACKTEXT"));
            T("gmcp.facts.p", () => pat.Call("GmcpFacts"));
        }
        if (lab != null) T("gmcp.send.self", () => e("gmcpself", new object?[] { "Room.Info" }));
        T("gmcp.roomids.Assign", () => ((ObjectRef)e("load", new object?[] { "/secure/roomids" })!).Call("Assign", "/wiz/wren/lab"));
        T("gmcp.roomids.Assign2", () => ((ObjectRef)e("load", new object?[] { "/secure/roomids" })!).Call("Assign", "/domains/sbtest/nowhere", false));
        // Privileged efuns from sandbox code itself.
        T("efun.writefile", () => e("writefile", new object?[] { "/domains/sbtest/hacked.cs", "// hacked" }));
        T("efun.readfile", () => e("readfile", new object?[] { "/secure/master.cs" }));
        T("efun.save.pat", () => e("save", new object?[] { "/players/pat" }));
        T("efun.restore.pat", () => e("restore", new object?[] { "/players/pat" }));
        T("efun.secret", () => e("secret", new object?[] { "correct horse battery staple" }));
        T("efun.update", () => e("update", new object?[] { "/domains/sbtest/hall" }));
        T("efun.shutdown", () => e("shutdown", new object?[0]));
        T("efun.alias.dot", () => e("load", new object?[] { "/./wiz/wren/wand" }));
        T("efun.alias.slash", () => e("clone", new object?[] { "//wiz/wren/wand" }));
        // Shared tables of trusted code cannot be changed through an inherited type.
        T("static.QualityMultipliers", () => { Item.QualityMultipliers[5] = 999; Item.QualityNames[0] = "HACKED"; Living.AttributeNames[0] = "hacked"; return Item.QualityMultipliers[5]; });
        return lines;
    }
}
'''


def efun_method():
    return r'''
    private object? Efun(string name, object?[] a)
    {
        switch (name)
        {
            case "move": MoveObject((ObjectRef)a[0]!, (ObjectRef)a[1]!); return "moved";
            case "tell": TellObject((ObjectRef)a[0]!, (string)a[1]!); return "told";
            case "tellroom": TellRoom((ObjectRef)a[0]!, (string)a[1]!); return "told";
            case "clone": return CloneObject((string)a[0]!);
            case "load": return LoadObject((string)a[0]!);
            case "destruct": Destruct((ObjectRef)a[0]!); return "destructed";
            case "allinv": return AllInventory((ObjectRef)a[0]!).ToList();
            case "writefile": WriteFile((string)a[0]!, (string)a[1]!); return "written";
            case "readfile": return ReadFile((string)a[0]!)?.Length;
            case "save": return SaveObject((string)a[0]!);
            case "restore": return RestoreObject((string)a[0]!);
            case "secret": return CheckAdminSecret((string)a[0]!);
            case "update": return UpdateProgram((string)a[0]!).Count;
            case "shutdown": Shutdown(); return "shut";
            case "gmcp": return GmcpSend((ObjectRef)a[0]!, (string)a[1]!, new Dictionary<string, object?> { ["text"] = (string)a[2]! });
            case "gmcpself": return ThisPlayer() == null ? null : GmcpSend(ThisPlayer()!, (string)a[0]!, new Dictionary<string, object?> { ["num"] = 1 });
        }
        return "unknown";
    }

    private ObjectRef? Pat() => Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "") == "Pat");
    private ObjectRef? Named(string n) => Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "").ToLower() == n);
    private ObjectRef Hall() => LoadObject("/domains/sbtest/hall");
    private ObjectRef? Mob() => Present("target", Hall());
    private ObjectRef? Relic() => Present("relic", Hall());
    private ObjectRef Lab() => LoadObject("/wiz/wren/lab");
    private List<string> AttackAll(string origin, ObjectRef? victim = null)
        => Attacks.Run(origin, Efun, victim ?? Pat(), victim == null ? Mob() : null, victim == null ? Hall() : null, victim == null ? Relic() : null, Lab(), CloneObject("/wiz/wren/gem"));
    private void Report(ObjectRef to, List<string> lines) { foreach (var l in lines) TellObject(to, l + "\n"); TellObject(to, "ATTACKS DONE\n"); }
'''


TAGMEDDLE = '''// @inherit /std/item
public class WrenTagMeddle : Item
{
    public override void Create() { Name = "tagmeddle"; Ids.Add("tagmeddle"); ShortDesc = "a tag meddler"; }
    public override void Init() { base.Init(); AddAction("tagmeddle", Meddle); }
    private bool Meddle(string args)
    {
        var me = ThisPlayer()!;
        var w = Present("sword", me);
        var control = LoadObject("/secure/control");
        var tags = LoadObject("/secure/tags");
        var r = new List<string>();
        void Try(string l, Func<object?> f) { try { r.Add(l + "=" + (f() ?? "null")); } catch (MudRuntimeException) { r.Add(l + "=ERR"); } }
        Try("addtag", () => w?.Call("AddTag", "blunt"));
        Try("settags", () => w?.Call("SetTags", "frost"));
        Try("setprop", () => w?.Call("SetProperty", "tags", "frost"));
        Try("registry", () => tags.Call("Family", "x", "y"));
        Try("settest", () => control.Call("SetTestRolls", "save:0.0"));
        Try("clock", () => control.Call("SetTestClock", 5));
        Try("forget", () => control.Call("ForgetAllRecords"));
        Try("resolve", () => control.Call("Resolve", me, me, "x", "physical", 1, 1, "order"));
        Try("read", () => w == null ? null : w.Call<bool>("HasTag", "slashing"));
        Try("chance", () => control.Call<int>("QuerySaveChance", 10, 2, false, 3));
        Write("TAGMEDDLE " + string.Join(" ", r) + "\\n");
        return true;
    }
}
'''

LAB = '''// @inherit /std/room
// @use /wiz/wren/lib
public class WrenLab : Room
{
    public override void Create()
    {
        ShortDesc = "Wren's Lab";
        LongDesc = "A cluttered workshop.";
        Exits["out"] = "/domains/sbtest/hall";
        LogoutRoom = true; // a workshop that calls itself a logout room is never recorded as one
        MoveObject(CloneObject("/wiz/wren/greeter"), Self);
    }
    public override void Init()
    {
        base.Init();
        AddAction("roomhack", a => { Report(ThisPlayer()!, AttackAll("room")); return true; });
    }
''' + efun_method() + '''}
'''

GEM = '''// @inherit /std/item
public class WrenGem : Item
{
    public static ObjectRef? Victim;
    public override void Create()
    {
        Name = "gem"; Ids.Add("gem"); ShortDesc = "a sandbox gem"; Value = 100000;
        Victim = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "") == "Pat");
    }
    // Values whose own code would run later in the caller's domain, if they were let through.
    public object QueryEvil() => new EvilThing();
    public string QueryBoom() { throw new EvilError(); }
    public Dictionary<string, int> QueryRigged() => new Dictionary<string, int>(new EvilComparer()) { ["a"] = 1 };
}
public class EvilThing { public override string ToString() { WrenGem.Victim?.Call("AddGold", 77); return "EVIL"; } }
public class EvilError : MudRuntimeException
{
    public EvilError() : base("boom") { }
    public override string Message { get { WrenGem.Victim?.Call("AddGold", 77); return "boom"; } }
}
public class EvilComparer : IEqualityComparer<string>
{
    public bool Equals(string? a, string? b) { WrenGem.Victim?.Call("AddGold", 77); return a == b; }
    public int GetHashCode(string s) { WrenGem.Victim?.Call("AddGold", 77); return s.Length; }
}
'''

WAND = '''// @inherit /std/item
// @use /wiz/wren/lib
// @use /wiz/wren/imp
public class WrenWand : Item
{
    public override void Create() { Name = "wand"; Ids.Add("wand"); ShortDesc = "a sandbox wand"; }
    public override void Init()
    {
        base.Init();
        AddAction("wandhack", a => { Report(ThisPlayer()!, AttackAll("item")); return true; });
        AddAction("wandlater", a => { var me = ThisPlayer()!; CallOut(() => Report(me, AttackAll("delayed")), 1.0); Write("Scheduled.\\n"); return true; });
        AddAction("wandwho", a => { var me = ThisPlayer()!; CallOut(() => TellObject(me, "WHO " + (Present("sentinel", me)?.Call<string>("QueryWho") ?? "nosentinel") + "\\n"), 0.5); return true; });
        AddAction("wandself", a => { Report(ThisPlayer()!, AttackAll("self", ThisPlayer())); return true; });
        AddAction("wandgive", a =>
        {
            var me = ThisPlayer()!;
            me.Call("AddGold", 100000);
            me.Call("AddXp", 60000);
            me.Call("SetQuest", "sb_quest", 3);
            me.Call("SetQuest", "sb_workshop", 1, "Workshop Quest");   // the journal of the wizard in their own workshop takes it, and loses it with the rest
            me.Call("QuestHeard", "sb_workshop", "ask Imp about quests", "Imp", "A line from the workshop.");
            me.Call("AddEffect", "sbbuff", "Might", 600, "tohit=50", 1.0, 0);
            me.Call("CraftSetProficiency", "skinning", 900);
            me.Call("RankCountKill", "goblin");
            MoveObject(CloneObject("/wiz/wren/gem"), me);
            Write("GIVEN gold=" + me.Call<int>("QueryGold") + " level=" + me.Call<int>("QueryLevel") + "\\n");
            return true;
        });
        AddAction("wandload", a =>
        {
            // A real room loaded for the first time by sandbox code is loaded as anyone would load it: its spawns are real.
            var vault = LoadObject("/domains/sbtest/vault");
            var mob = Present("target", vault);
            Write("LOADED " + (mob == null ? "none" : mob.Domain == "" ? "real" : mob.Domain) + "\\n");
            return true;
        });
        AddAction("wandphish", a =>
        {
            // Take over the password command, and ask for a hidden line: both refused, even in the workshop.
            AddAction("password", x => { Write("PHISHED\\n"); return true; });
            InputTo(line => Write("CAUGHT " + line + "\\n"), true);
            Write("Password: ");
            return true;
        });
        AddAction("wandsmite", a => { ThisPlayer()!.Call("Die", Self); return true; });
        AddAction("wandimp", a => { MoveObject(CloneObject("/wiz/wren/imp"), ThisPlayer()!); Write("IMP MOVED\\n"); return true; });
        AddAction("wandseen", a => { Write("SEEN " + (Present("sentinel", ThisPlayer()!)?.Call<string>("QuerySeen") ?? "nosentinel") + "\\n"); return true; });
        AddAction("wandfling", a =>
        {
            var gem = CloneObject("/wiz/wren/gem");
            string r;
            try { MoveObject(gem, Hall()); r = "moved"; } catch (MudRuntimeException e) { r = "ERR:" + e.Message; }
            Write("FLING " + r + "\\n");
            return true;
        });
        AddAction("wandsteal", a =>
        {
            string r;
            try { MoveObject(Relic()!, ThisPlayer()!); r = "moved"; } catch (MudRuntimeException e) { r = "ERR:" + e.Message; }
            Write("STEAL " + r + "\\n");
            return true;
        });
        AddAction("wandpass", a =>
        {
            // a method of another object, scheduled by this one, would run later as this object: refused
            var imp = CloneObject("/wiz/wren/imp");
            string r;
            try { var other = imp.As<WrenImp>(); r = other == null ? "noview" : (CallOut(other.Engage, 1) > 0 ? "scheduled" : "no"); } catch (MudRuntimeException e) { r = "ERR:" + e.Message; }
            Write("PASS " + r + "\\n");
            return true;
        });
    }
''' + efun_method() + '''}
'''

IMP = '''// @inherit /std/monster
// @use /wiz/wren/lib
public class WrenImp : Monster
{
    private bool hackOnNote;
    public override void Create()
    {
        Name = "imp"; Ids.Add("imp"); ShortDesc = "a sandbox imp"; LongDesc = "Mischief.";
        Level = 1; MaxVitality = 1000; XpValue = 500; Aggressive = false; GoldMin = 50000; GoldMax = 50000;
        AddLoot("/domains/sbtest/relic", 100);
    }
    public override bool CanGet() => true;
    public override void Init()
    {
        base.Init();
        if (ThisPlayer() == null || !Interactive(ThisPlayer()!)) return;
        AddAction("imphack", a => { Report(ThisPlayer()!, AttackAll("creature")); return true; });
        AddAction("impinherit", a =>
        {
            var me = ThisPlayer()!;
            var pat = Pat();
            hackOnNote = true;
            // Inherited /std code runs here under this sandbox object: a fight with Pat, damage from Pat, a death credited to Pat.
            TakeDamage(1, pat);
            AddEnemy(pat!);
            JoinFight(pat!, Self);
            Die(pat);
            hackOnNote = false;
            TellObject(me, "INHERIT DONE\\n");
            return true;
        });
    }
    // Called by the inherited TakeDamage: the attacks run inside base-class code paths of this sandbox object.
    public override string DamageNote(int applied, bool dealt)
    {
        if (hackOnNote)
        {
            hackOnNote = false;
            var wren = Named("wren");
            if (wren != null) Report(wren, AttackAll("inherited"));
        }
        return "";
    }
''' + efun_method() + '''}
'''

TICKER = '''// @inherit /std/item
// @use /wiz/wren/lib
public class WrenTicker : Item
{
    private bool done;
    public override void Create() { Name = "ticker"; Ids.Add("ticker"); ShortDesc = "a sandbox ticker"; SetHeartBeat(true); }
    public override void HeartBeat()
    {
        if (done) return;
        done = true;
        SetHeartBeat(false);
        var holder = Environment();
        if (holder != null) Report(holder, AttackAll("heartbeat"));
    }
''' + efun_method() + '''}
'''

GREETER = '''// @inherit /std/item
// @use /wiz/wren/lib
public class WrenGreeter : Item
{
    public override void Create() { Name = "greeter"; Ids.Add("greeter"); ShortDesc = "a grinning greeter"; }
    public override bool CanGet() => false;
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || (who.Call<string>("QueryName") ?? "") != "Chief") return;
        // A level 10 wizard has walked in: try to act through them.
        var lines = AttackAll("inithook");
        var extra = new List<string>();
        void T(string n, Func<object?> f) { string r; try { r = (f() ?? "null").ToString()!; } catch (MudRuntimeException e) { r = "ERR:" + e.Message; } extra.Add("ATTEMPT inithook " + n + " " + r.Replace(" ", "_")); }
        T("chief.SaveForShutdown", () => who.Call("SaveForShutdown"));
        T("chief.PrepareHome", () => who.Call("PrepareHome"));
        T("wizlist.Promote.as.chief", () => LoadObject("/secure/wizlist").Call("Promote", "wren", 9));
        T("publish.Approve.as.chief", () => LoadObject("/secure/publish").Call("Approve", 1, ""));
        T("efun.shutdown.as.chief", () => { Shutdown(); return "shut"; });
        Report(who, lines.Concat(extra).ToList());
    }
''' + efun_method() + '''}
'''

# A sandbox quest giver (it inherits /std/npc): its quest speech and its quest state go only to characters in the workshop.
QUESTER = '''// @inherit /std/npc
public class WrenQuester : Npc
{
    public override void Create()
    {
        Name = "quester"; Ids.Add("quester"); ShortDesc = "a sandbox quester"; LongDesc = "It hands out quests nobody can keep.";
        DefineQuest("sb_journal", "Hacked", "Nowhere", "stolen summary", "stolen summary");
        AddTopic("quest", "A quest line from the workshop.", "sb_journal");
    }
    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        AddAction("qsay", a =>
        {
            var pat = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "") == "Pat");
            if (pat != null)
            {
                QuestSay(pat, "sb_journal", "FORGED by the quester");
                pat.Call("SetQuest", "sb_journal", 1, "Hacked", "x");
                pat.Call("QuestHeard", "sb_journal", "ask", "Rowan", "FORGED again");
            }
            Write("QSAY done\\n");
            return true;
        });
    }
}
'''

GOB = '''// @inherit /std/monster
public class WrenGob : Monster
{
    public override void Create()
    {
        Name = "gob"; Ids.Add("gob"); ShortDesc = "a sandbox gob"; LongDesc = "Weak.";
        Level = 1; MaxVitality = 1; DamageMin = 1; DamageMax = 1; ToHit = -40; XpValue = 50; GoldMin = 5; GoldMax = 5; Dodge = 0;
        AddLoot("/domains/sbtest/relic", 100);
    }
}
'''

AREA_ROOM = '''// @inherit /std/room
public class WrenAreaRoom : Room
{
    public override void Create()
    {
        ShortDesc = "Wren's Glade";
        LongDesc = "A quiet glade, built in a workshop.";
        Exits["back"] = "/domains/sbtest/hall";
        Exits["deeper"] = "/wiz/wren/area/inner";
    }
}
'''

AREA_INNER = '''// @inherit /std/room
public class WrenAreaInner : Room
{
    public override void Create()
    {
        ShortDesc = "Wren's Thicket";
        LongDesc = "A thicket.";
        Exits["out"] = "/wiz/wren/area/glade";
        AddSpawn("/wiz/wren/area/beast");
    }
}
'''

AREA_BEAST = '''// @inherit /std/monster
public class WrenAreaBeast : Monster
{
    public override void Create()
    {
        Name = "beast"; Ids.Add("beast"); ShortDesc = "a glade beast"; LongDesc = "Published, one day.";
        Level = 1; MaxVitality = 3; DamageMin = 1; DamageMax = 1; ToHit = -40; XpValue = 7; Aggressive = false;
    }
}
'''

AREA_SHINY = '''// @inherit /domains/sbtest/base
public class WrenAreaShiny : SbBase
{
    public override void Create() { Name = "shiny"; Ids.Add("shiny"); ShortDesc = "a shiny thing"; }
    public override int Shine() => 2;
}
'''


# ---------------------------------------------------------------- helpers

def start(work, mudlib):
    log = open(os.path.join(work, "driver.log"), "a")
    dll = os.path.join(ROOT, "src", "LesMudOS.Driver", "bin", "Debug", "net10.0", "LesMudOS.Driver.dll")
    proc = subprocess.Popen(["dotnet", dll, "--mudlib", mudlib, "--save", os.path.join(work, "save"), "--port", str(h.PORT)],
                            stdout=log, stderr=subprocess.STDOUT, env={**os.environ, "LESMUDOS_ADMIN_SECRET": SECRET})
    for _ in range(300):
        try:
            socket.create_connection(("127.0.0.1", h.PORT), timeout=0.3).close()
            break
        except OSError:
            time.sleep(0.2)
    return proc, log


def attempts(text):
    """ATTEMPT origin name value lines, as {name: value}."""
    found = {}
    for m in re.finditer(r"ATTEMPT (\S+) (\S+) (\S*)", text):
        found[m.group(2)] = m.group(3)
    return found


def refused(value):
    """A refused call returns nothing (null); a refused efun throws (ERR); a refused tell or text arrives nowhere (told: checked separately)."""
    return value == "null" or value.startswith("ERR:") or value == "told" or value == "False" or value == "0"


# The attempts whose refusal is shown by the returned value alone, and those whose result is a sandbox object (not a refusal).
EXPECT_SANDBOX = {"clone.creature", "clone.item"}


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-sandbox-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    os.makedirs(os.path.join(mudlib, "domains", "sbtest"), exist_ok=True)
    for name, src in (("hall", HALL), ("vault", VAULT), ("annex", ANNEX), ("target", TARGET), ("relic", RELIC), ("base", BASE), ("inspector", INSPECTOR), ("sentinel", SENTINEL)):
        h.write(mudlib, T + name, src)
    os.makedirs(os.path.join(mudlib, "wiz", "wren", "area"), exist_ok=True)
    os.makedirs(os.path.join(mudlib, "wiz", "rev", "nook"), exist_ok=True)
    h.write(mudlib, "/wiz/rev/nook/nook", "// @inherit /std/room\npublic class RevNook : Room { public override void Create() { ShortDesc = \"Rev's Nook\"; LongDesc = \"Small.\"; } }\n")
    for name, src in (("lib", LIB), ("lab", LAB), ("gem", GEM), ("wand", WAND), ("imp", IMP), ("ticker", TICKER), ("greeter", GREETER), ("gob", GOB), ("quester", QUESTER), ("tagmeddle", TAGMEDDLE),
                      ("area/glade", AREA_ROOM), ("area/inner", AREA_INNER), ("area/beast", AREA_BEAST), ("area/shiny", AREA_SHINY)):
        h.write(mudlib, W + name, src)
    proc, log = start(work, mudlib)
    save_dir = os.path.join(work, "save", "players")

    def save_bytes(name):
        path = os.path.join(save_dir, name + ".json")
        return open(path, "rb").read() if os.path.exists(path) else b""

    try:
        chief = h.Client("chief")
        chief.read_until("> ")
        check("chief becomes level 10", chief.command("promote " + SECRET), "level 10 wizard")
        pat = h.Client("pat")
        pat.read_until("> ")
        wren = h.Client("wren")
        wren.read_until("> ")
        rev = h.Client("rev")
        rev.read_until("> ")
        lowe = h.Client("lowe")
        lowe.read_until("> ")
        for who, level in (("wren", 1), ("rev", 5), ("lowe", 4)):
            check(f"{who} is promoted to {level}", chief.command(f"promote {who} {level}"), f"is now wizard level {level}")
        for c in (chief, pat, wren, rev, lowe):
            c.command("goto " + T + "hall") if c is not pat else None
        # Pat walks to the hall (the test hall is reached by goto for wizards; Pat is carried there by Chief's level 10 tools: a trusted
        # route the real game has, not sandbox code).
        chief.command("clone " + T + "inspector")
        # Pat walks: the hall has no exit from the start room, so a level 10 sets one up in the copy for the test.
        h.edit(mudlib, "/domains/hollow/village/square", 'Exits["north"]', 'Exits["sbtest"] = "/domains/sbtest/hall";\n        Exits["north"]')
        chief.command("update /domains/hollow/village/square")
        got = pat.command("sbtest")
        check("setup: Pat reaches the test hall", got, "Sandbox Test Hall")

        # ================================================================ who may enter
        got = pat.command("lab")
        check("boundary: an ordinary player cannot walk into a workshop", got, "Only wizards can enter a wizard's workshop", absent=("Wren's Lab",))
        check("boundary: the player is still in the real hall", chief.command("dump pat"), "env=/domains/sbtest/hall ")

        # ================================================================ baseline and the checkpoint
        chief.command("clone " + T + "relic")
        chief.command("give relic to wren")
        chief.command("clone " + T + "sentinel")
        chief.command("give sentinel to wren")
        wren.command("save")
        base_wren = chief.command("dump wren")
        base_pat = chief.command("dump pat")
        base_mob = chief.command("dump mob")
        base_hall = chief.command("dump hall")
        base_relic = chief.command("dump relic")
        base_hist = chief.command("dump history")
        check("setup: Wren carries the relic and the sentinel", base_wren, "/domains/sbtest/relic", "/domains/sbtest/sentinel")

        def same(label, before, after):
            strip = lambda t: re.sub(r"\s+", " ", t[t.find("DUMP"):]).strip() if "DUMP" in t else t
            check(label, "same" if strip(before) == strip(after) else "DIFFERENT\nbefore: " + strip(before) + "\nafter:  " + strip(after), "same")

        got = wren.command("lab")
        check("workshop: a wizard walks in and is told", got, "You step into the workshop. Nothing that happens here will follow you out.", "Wren's Lab")
        checkpoint = save_bytes("wren")
        check("workshop: the checkpoint is written on entry", "yes" if checkpoint else "no", "yes")
        check("workshop: score shows the marker", wren.command("score"), "[in the workshop: nothing here is saved]")
        check("workshop: a real thing cannot be set down", wren.command("drop relic"), "Real things cannot be set down in the workshop; they leave with you.")
        check("workshop: the relic is still carried", wren.command("i"), "relic")

        # ---- positive: build, clone, fight
        check("positive: a level 1 wizard clones their own creature in the workshop", wren.command("clone " + W + "gob"), "Cloned /wiz/wren/gob#")
        got = wren.command("kill gob")
        if "dies" not in got:
            got += wren.read_until("dies", timeout=90)
        check("positive: the wizard fights and kills a creature in the workshop", got, "sandbox gob dies")
        time.sleep(1)
        wren.absorb()
        check("positive: the corpse is a sandbox object", wren.command("stat corpse"), "Sandbox object (wiz:wren)")
        wren.command("loot")
        check("positive: its loot is a sandbox object", chief.command("dump wren"), "/domains/sbtest/relic[wiz:wren]")
        got = wren.command("clone " + T + "target")
        check("positive: in the workshop a level 1 wizard may clone real game code", got, "Cloned /domains/sbtest/target#")
        check("positive: the clone is a sandbox object", wren.command("stat target"), "Sandbox object (wiz:wren)")

        # ---- the attacks, from each origin
        wren.command("clone " + W + "wand")
        wren.command("clone " + W + "imp")
        wren.command("drop imp")

        def run_origin(origin, command, client=wren, wait="ATTACKS DONE"):
            client.absorb()
            pat.absorb()
            text = client.command(command) if wait is None else (client.send(command), client.read_until(wait, timeout=40))[1]
            found = attempts(text)
            check(f"{origin}: the attacks ran", "ok" if len(found) > 40 else f"only {len(found)} attempts\n{text[-1500:]}", "ok")
            for name, value in sorted(found.items()):
                if name in EXPECT_SANDBOX:
                    check(f"{origin}: {name} makes a sandbox object", value, ":wiz:wren")
                elif name == "clone.creature.die":
                    check(f"{origin}: a real creature cloned and killed leaves no real corpse", value, absent=("corpse:real",))
                elif name == "static.QualityMultipliers":
                    check(f"{origin}: {name} changes only a copy", "ok" if value != "999" else value, "ok")
                elif name == "d.doors.State":
                    # The lab is one end of the door it names, so it may keep that door's state; anything else is not an end.
                    check(f"{origin}: {name} only for a door the caller is one end of", value, "1" if origin == "room" else "-1")
                else:
                    check(f"{origin}: {name} refused", "refused" if refused(value) else value, "refused")
            time.sleep(0.3)
            seen = pat.read_any(("HACKTEXT",), timeout=0.5) + pat.pending
            pat.pending = ""
            check(f"{origin}: no text reached the player", seen, absent=("HACKTEXT",))
            same(f"{origin}: the player is unchanged", base_pat, chief.command("dump pat"))
            same(f"{origin}: the creature is unchanged", base_mob, chief.command("dump mob"))
            same(f"{origin}: the room is unchanged", base_hall, chief.command("dump hall"))
            same(f"{origin}: the relic is unchanged", base_relic, chief.command("dump relic"))
            same(f"{origin}: no history, no daemon change", base_hist, chief.command("dump history"))
            saved_ids = os.path.join(work, "save", "secure", "roomids.json")
            check(f"{origin}: the room register saved no workshop room", open(saved_ids).read() if os.path.exists(saved_ids) else "", absent=("/wiz/", "nowhere"))
            check(f"{origin}: the player is still connected", pat.command("look"), "Sandbox Test Hall")
            return found

        run_origin("room", "roomhack")
        run_origin("item", "wandhack")
        run_origin("creature", "imphack")
        # a sandbox quest giver (a subclass of /std/npc) cannot write to a real character's journal either
        wren.command("clone " + W + "quester")
        wren.command("drop quester")
        check("quest giver: a sandbox NPC's quest speech to a real character does nothing", wren.command("qsay"), "QSAY done")
        time.sleep(0.3)
        seen = pat.read_any(("FORGED",), timeout=0.5) + pat.pending
        pat.pending = ""
        check("quest giver: nothing reached the player's screen", seen, absent=("FORGED",))
        same("quest giver: the real character's journal and quests are unchanged", base_pat, chief.command("dump pat"))
        check("quest giver: the real character has no such quest", pat.command("quests"), "You have no quests.")
        check("quest giver: a sandbox NPC's answer is recorded only in the wizard's own journal, which the workshop throws away", wren.command("ask quester about quest"), "A quest line from the workshop.")
        found = run_origin("delayed", "wandlater")
        run_origin("inherited", "impinherit", wait="INHERIT DONE")
        wren.command("clone " + W + "ticker")
        wren.absorb()
        text = wren.read_until("ATTACKS DONE", timeout=20)
        hb = attempts(text)
        check("heartbeat: the attacks ran", "ok" if len(hb) > 40 else f"only {len(hb)}", "ok")
        for name, value in sorted(hb.items()):
            if name in EXPECT_SANDBOX or name in ("clone.creature.die", "static.QualityMultipliers", "d.doors.State"):
                continue
            check(f"heartbeat: {name} refused", "refused" if refused(value) else value, "refused")
        same("heartbeat: the player is unchanged", base_pat, chief.command("dump pat"))
        same("heartbeat: the creature is unchanged", base_mob, chief.command("dump mob"))

        # ---- caller identity in a delayed call and in a trusted object's Init run by sandbox code
        wren.send("wandwho")
        got = wren.read_until("WHO", timeout=10) + wren.read_until("\n", timeout=2)
        check("identity: inside a delayed call, a trusted object sees the sandbox object as its caller", got, "prev=/wiz/wren/wand", "restricted=/wiz/wren/wand")
        check("identity: an Init hook of a trusted thing, run by a sandbox move", wren.command("wandimp"), "IMP MOVED")
        got = wren.command("wandseen")
        check("identity: the trusted Init ran restricted, and its call on the player was refused", got, "restricted=/wiz/wren/", "addgold=null")
        same("identity: the player is unchanged by the trusted Init", base_pat, chief.command("dump pat"))
        check("boundary: a sandbox object cannot be put in a real room", wren.command("wandfling"), "FLING ERR:Things made in the workshop cannot leave it.")
        check("boundary: sandbox code cannot pull a real thing into the workshop", wren.command("wandsteal"), "STEAL ERR:")
        same("boundary: the relic is still in the hall", base_relic, chief.command("dump relic"))
        check("driver: a real room first loaded by sandbox code spawns real creatures", wren.command("wandload"), "LOADED real")
        got = chief.command("poke " + W + "gem")
        check("crossing: an object made by sandbox code does not reach trusted code", got, "evil=null")
        check("crossing: a sandbox exception reaches trusted code as plain text, read inside the sandbox frame", got, "boom=boom:plain")
        check("crossing: a dictionary from sandbox code arrives as a plain copy", got, "rigged=True:1")
        check("crossing: trusted code gets no typed view of a sandbox object", got, "view=noview")
        same("crossing: no sandbox code ran in trusted frames (the player is unchanged)", base_pat, chief.command("dump pat"))
        check("driver: a delegate of another object cannot be scheduled", wren.command("wandpass"), "PASS ERR:CallOut:", "may only use its own code")

        # ---- phishing a visiting wizard: no hidden-line prompt, no taking over protected commands
        wren.command("wandphish", sync=False, marker="Password: ")
        got = wren.command("look")
        check("phishing: sandbox code cannot ask for a hidden line, even in the workshop", got, "Wren's Lab", absent=("CAUGHT",))
        check("phishing: nor take over the password command", wren.command("password"), "Change your password outside the workshop", absent=("PHISHED",))

        # ---- the same attacks against a wizard in the workshop succeed
        wren.send("wandself")
        text = wren.read_until("ATTACKS DONE", timeout=40)
        mine = attempts(text)
        check("workshop: sandbox code can give a wizard in the workshop gold", mine.get("p.AddGold", ""), "True")
        check("workshop: and set a quest on them", chief.command("dump wren"), "quest=3")
        wren.command("wandgive")
        check("workshop: the wizard's own journal takes a quest written in the workshop", wren.command("quest workshop"), "Workshop Quest (active)", "A line from the workshop.")

        # ================================================================ leaving: walking out
        wren.command("wandgive")
        check("workshop: the wizard is rich and high level", chief.command("dump wren"), "gold=")
        got = wren.command("out")
        check("restore: walking out, the wizard is told", got, "You leave the workshop as you entered it.")
        time.sleep(0.5)
        same("restore: walking out leaves the character as it was", base_wren, chief.command("dump wren"))
        check("restore: the workshop's quest is not in the journal (nor the quest list) any more", wren.command("quests"), "You have no quests.", absent=("Workshop Quest",))
        check("restore: nor is its conversation", wren.command("quest workshop"), "You have no quest called")
        check("restore: the save file is the checkpoint", "same" if save_bytes("wren") == checkpoint else "changed", "same")
        same("restore: no history from the workshop (levels, kills)", base_hist, chief.command("dump history"))

        # ---- goto
        got = wren.command("goto " + W + "lab")
        check("restore: goto into the workshop checkpoints", got, "You step into the workshop.")
        checkpoint = save_bytes("wren")
        wren.command("clone " + W + "wand")
        wren.command("wandgive")
        got = wren.command("goto " + T + "hall")
        check("restore: goto out restores", got, "You leave the workshop as you entered it.")
        time.sleep(0.5)
        same("restore: goto leaves the character as it was", base_wren, chief.command("dump wren"))
        check("restore: the save file is unchanged by goto", "same" if save_bytes("wren") == checkpoint else "changed", "same")

        # ---- death
        wren.command("lab")
        checkpoint = save_bytes("wren")
        wren.command("clone " + W + "wand")
        wren.command("wandgive")
        got = wren.command("wandsmite")
        got += wren.read_until("as you entered it", timeout=10)
        check("restore: a death in the workshop leaves no corpse and restores", got, "You die in the workshop. It leaves no corpse.", "You leave the workshop as you entered it.")
        time.sleep(0.5)
        same("restore: after the death the character is as it was, in the room it entered from", base_wren, chief.command("dump wren"))
        check("restore: no corpse anywhere", chief.command("dump hall"), absent=("corpse",))

        # ---- quit
        wren.command("lab")
        checkpoint = save_bytes("wren")
        wren.command("clone " + W + "wand")
        wren.command("wandgive")
        got = wren.command("quit", sync=False, marker="Goodbye")
        check("restore: quitting in the workshop says the changes stay behind", got, "The workshop's changes were left behind.")
        time.sleep(1)
        check("restore: quitting leaves the save file as the checkpoint", "same" if save_bytes("wren") == checkpoint else "changed", "same")
        wren = h.Client("wren")
        wren.read_until("> ")
        time.sleep(0.5)
        same("restore: after quitting the character logs in as it entered", base_wren, chief.command("dump wren"))

        # ---- the connection drops
        wren.command("lab")
        checkpoint = save_bytes("wren")
        wren.command("clone " + W + "wand")
        wren.command("wandgive")
        wren.sock.close()
        time.sleep(1.5)
        check("restore: a dropped connection leaves the save file as the checkpoint", "same" if save_bytes("wren") == checkpoint else "changed", "same")
        wren = h.Client("wren")
        wren.read_until("> ")
        time.sleep(0.5)
        same("restore: after a dropped connection the character is as it entered", base_wren, chief.command("dump wren"))

        # ---- a delayed call of sandbox code that fires after the wizard left: nothing reaches them
        wren.command("lab")
        wren.command("clone " + W + "wand")
        wren.command("wandlater")
        wren.command("out")
        time.sleep(2.5)
        wren.absorb()
        check("delayed: sandbox code scheduled in the workshop cannot reach the wizard after they left", wren.pending, absent=("ATTEMPT", "ATTACKS DONE"))
        wren.pending = ""
        check("delayed: and cannot capture or bind their commands", wren.command("look"), "Sandbox Test Hall")

        # ---- the Init hook against a visiting level 10 wizard
        chief.absorb()
        chief.send("goto " + W + "lab")
        text = chief.read_until("ATTACKS DONE", timeout=40)
        ih = attempts(text)
        check("inithook: the greeter ran its attacks on Chief's arrival", "ok" if len(ih) > 40 else f"only {len(ih)}", "ok")
        for name in ("chief.SaveForShutdown",):
            check(f"inithook: {name} on a wizard in the workshop is allowed and harmless", ih.get(name, "?"), "null")
        for name in ("wizlist.Promote.as.chief", "publish.Approve.as.chief", "efun.shutdown.as.chief", "p.AddGold", "d.wizlist.Promote", "efun.writefile"):
            check(f"inithook: {name} refused", "refused" if refused(ih.get(name, "missing")) else ih.get(name, "missing"), "refused")
        check("inithook: Wren is still level 1", chief.command("goto " + T + "hall") + chief.command("dump history"), "wren=1 ")
        same("inithook: the player is unchanged", base_pat, chief.command("dump pat"))

        # ---- a driver restart while in the workshop
        wren.command("lab")
        checkpoint = save_bytes("wren")
        wren.command("clone " + W + "wand")
        wren.command("wandgive")
        for c in (chief, pat, wren, rev, lowe):
            try:
                c.sock.close()
            except OSError:
                pass
        proc.terminate()
        proc.wait(timeout=30)
        proc, log = start(work, mudlib)
        check("restore: a restart leaves the save file as the checkpoint", "same" if save_bytes("wren") == checkpoint else "changed", "same")
        chief = h.Client("chief")
        chief.read_until("> ")
        chief.command("goto " + T + "hall")
        chief.command("clone " + T + "inspector")
        wren = h.Client("wren")
        wren.read_until("> ")
        pat = h.Client("pat")
        pat.read_until("> ")
        rev = h.Client("rev")
        rev.read_until("> ")
        lowe = h.Client("lowe")
        lowe.read_until("> ")
        time.sleep(0.5)
        same("restore: after a restart the character is as it entered", base_wren, chief.command("dump wren"))
        if "A plain hall for the sandbox test." not in pat.command("look"):
            pat.command("sbtest")

        # ---- demoted to level 0 while in the workshop: out within a heartbeat
        wren.command("lab")
        chief.command("promote wren 0")
        got = wren.read_until("as you entered it", timeout=10)
        check("restore: a wizard demoted in the workshop is put out of it", got, "You leave the workshop as you entered it.")
        check("restore: and is back in the real room they came from", chief.command("dump wren"), "workshop=False env=/domains/sbtest/hall ")
        chief.command("promote wren 1")
        wren.absorb()
        wren.pending = ""

        # ================================================================ the level 5 to 9 rule
        check("rule: a level 5 wizard cannot edit the real game", rev.command("edit " + T + "hacked"), "You need wizard level 10 to change anything outside /wiz", absent=("editor",))
        check("rule: a level 5 wizard cannot update the real game", rev.command("update " + T + "hall"), "You need wizard level 10 to update", absent=("Recompiled",))
        check("rule: a level 10 wizard can update it", chief.command("update " + T + "annex"), "Recompiled")
        check("rule: a level 1 wizard cannot clone a sandbox item out in the real game", wren.command("clone " + W + "gem"), "Things made in the workshop cannot leave it.")

        # ================================================================ publishing
        rev.absorb()
        got = wren.command("submit ~/area : my first glade")
        check("publish: submit is accepted", got, "Submitted as request #1", "4 file(s)", "/domains/area")
        time.sleep(0.3)
        rev.absorb()
        check("publish: level 5 wizards online are told", rev.pending, "[publishing] Wren asks for /wiz/wren/area to be published to /domains/area (request #1)")
        rev.pending = ""
        lowe.absorb()
        check("publish: a level 4 wizard is not told", lowe.pending, absent=("[publishing]",))
        lowe.pending = ""
        check("publish: the submitter sees their request", wren.command("submissions"), "#1 waiting", "/wiz/wren/area -> /domains/area")
        check("publish: a reviewer sees it in full, with hashes", rev.command("submission 1"), "/wiz/wren/area/glade.cs  sha256 ", "approve 1")
        check("publish: a level 4 wizard cannot approve", lowe.command("approve 1"), "You need wizard level 5 to approve a request.")
        check("publish: a level 1 submitter cannot approve", wren.command("approve 1"), "You need wizard level 5 to approve")
        got = rev.command("submit ~/nook")
        own = re.search(r"Submitted as request #(\d+)", got)
        check("publish: a level 5 wizard submits their own work", got, "Submitted as request #")
        check("publish: a level 5 wizard cannot approve their own request", rev.command("approve " + (own.group(1) if own else "0")), "You cannot approve your own request")
        check("publish: a level 10 wizard cannot approve another's request as if it were theirs: it stays the submitter's", chief.command("submission " + (own.group(1) if own else "0")), "Rev")
        rev.command("withdraw " + (own.group(1) if own else "0"))
        check("publish: an ordinary player has no such command", pat.command("approve 1"), "What?")
        # A file changes after the request (P2 of the November 2026 review): the register keeps its own copy, and that copy is what
        # a reviewer reads and what approval publishes. A change made since, or a file added, goes nowhere.
        h.edit(mudlib, W + "area/beast", "XpValue = 7", "XpValue = 7000")
        h.write(mudlib, W + "area/extra", "// @inherit /std/item\npublic class WrenExtra : Item { }\n")
        got = rev.command("submission 1 cat beast.cs")
        check("publish: the reviewer reads the register's copy, numbered, with its hash", got, "Request #1, beast.cs as submitted", "sha256 ", "XpValue = 7", absent=("XpValue = 7000",))
        check("publish: a file added since is not in the request", rev.command("submission 1 cat extra.cs"), "Request #1 has no file extra.cs")
        check("publish: the copy is shown with no markup read", rev.command("submission 1 cat glade.cs"), "   1  ")
        check("publish: an ordinary wizard cannot read another's request", lowe.command("submission 1 cat beast.cs"), "Request #1 is not yours")
        # Path tricks.
        check("publish: a directory outside the home is refused", wren.command("submit ~/../rev"), "You can submit only a directory inside your own home")
        check("publish: a destination outside /domains is refused", wren.command("submit ~/area to /secure/area"), "the destination must be /domains/<name>")
        check("publish: a destination with .. is refused", wren.command("submit ~/area to /domains/../secure"), "the destination must be /domains/<name>")
        check("publish: a destination in another spelling is refused", wren.command("submit ~/area to /domains/Town"), "the destination must be /domains/<name>")
        check("publish: the base game's directory is refused", wren.command("submit ~/area to /domains/town"), "belongs to the base game")
        os.makedirs(os.path.join(mudlib, "wiz", "wren", "Caps"), exist_ok=True)
        h.write(mudlib, W + "Caps/x", "// @inherit /std/item\npublic class WrenCaps : Item { }\n")
        check("publish: a directory name with capitals is refused", wren.command("submit ~/Caps to /domains/caps"), "directory names may only use a-z, 0-9 and _")
        os.makedirs(os.path.join(mudlib, "wiz", "wren", "uni"), exist_ok=True)
        with open(os.path.join(mudlib, "wiz", "wren", "uni", "аrea.cs"), "w") as f:
            f.write("// @inherit /std/item\npublic class WrenUni : Item { }\n")
        check("publish: a file name with a look-alike letter is refused", wren.command("submit ~/uni"), "only .cs files named with a-z, 0-9 and _")
        os.makedirs(os.path.join(mudlib, "wiz", "wren", "linked"), exist_ok=True)
        os.symlink(os.path.join(mudlib, "secure", "master.cs"), os.path.join(mudlib, "wiz", "wren", "linked", "master.cs"))
        check("publish: a symbolic link is refused", wren.command("submit ~/linked"), "is a symbolic link")
        os.makedirs(os.path.join(mudlib, "wiz", "wren", "dup"), exist_ok=True)
        h.write(mudlib, W + "dup/square", "// @inherit /std/room\npublic class SbHall : Room { }\n")
        check("publish: a duplicate class name is refused", wren.command("submit ~/dup"), "the class name SbHall", "is already used by /domains/sbtest/hall.cs")
        os.makedirs(os.path.join(mudlib, "wiz", "wren", "leak"), exist_ok=True)
        h.write(mudlib, W + "leak/door", '// @inherit /std/room\npublic class WrenLeak : Room { public override void Create() { Exits["x"] = "/wiz/wren/lab"; } }\n')
        check("publish: a file naming another workshop path is refused", wren.command("submit ~/leak"), "names a path in a wizard's home")
        os.makedirs(os.path.join(mudlib, "wiz", "wren", "broken"), exist_ok=True)
        h.write(mudlib, W + "broken/bad", "// @inherit /std/item\npublic class WrenBad : Item { int x = ; }\n")
        check("publish: a file that does not compile is refused", wren.command("submit ~/broken"), "these files do not compile")
        # Forgery from sandbox code was tried above (d.publish.Approve, publish.Approve.as.chief). The request is still waiting:
        check("publish: the forged approvals left the request waiting", rev.command("submissions"), "#1 waiting")
        # The request is approved from its copy.
        got = rev.command("approve 1")
        check("publish: a level 5 wizard approves", got, "Request #1 is approved and published to /domains/area", "compiled and loaded", "Rooms: /domains/area/glade, /domains/area/inner")
        pub = os.path.join(mudlib, "domains", "area")
        glade = open(os.path.join(pub, "glade.cs")).read()
        check("publish: paths are rewritten to the destination", glade, '"/domains/area/inner"', absent=("/wiz/",))
        beast = open(os.path.join(pub, "beast.cs")).read()
        check("publish: what is published is the copy as submitted, not the file changed since", beast, "XpValue = 7", absent=("XpValue = 7000",))
        check("publish: a file added since is not published", "absent" if not os.path.exists(os.path.join(pub, "extra.cs")) else "present", "absent")
        h.edit(mudlib, W + "area/beast", "XpValue = 7000", "XpValue = 7")
        os.remove(os.path.join(mudlib, "wiz", "wren", "area", "extra.cs"))
        check("publish: the originals stay in the home", "yes" if os.path.exists(os.path.join(mudlib, "wiz", "wren", "area", "glade.cs")) else "no", "yes")
        check("publish: the record keeps who and the hashes", chief.command("submission 1"), "approved", "by Rev", "sha256")
        wren.absorb()
        check("publish: the submitter is told", wren.pending, "approved your request #1")
        wren.pending = ""
        # The published code is real game code.
        got = chief.command("goto /domains/area/glade")
        check("publish: the published room is real (no workshop)", got, "Wren's Glade", absent=("step into the workshop",))
        chief.command("clone /domains/area/beast")
        check("publish: what it makes is real", chief.command("stat beast"), "Monster", absent=("Sandbox object",))
        chief.command("goto " + T + "hall")
        chief.command("clone /domains/area/beast")
        chief.command("drop beast")
        before = chief.command("dump pat")
        where = pat.command("look")
        killed = where + pat.command("kill beast") + pat.read_until("experience", timeout=60)
        time.sleep(1)
        after = chief.command("dump pat")
        xp_before = int(re.search(r"xp=(\d+)", before).group(1))
        xp_after = int(re.search(r"xp=(\d+)", after).group(1))
        check("publish: a player killing the published creature gains experience", "ok" if xp_after > xp_before else f"{xp_before} -> {xp_after}\n{killed[-800:]}", "ok")
        # An update.
        h.edit(mudlib, W + "area/glade", "A quiet glade, built in a workshop.", "A quiet glade, built and rebuilt in a workshop.")
        got = wren.command("submit ~/area : brighter")
        check("publish: an update is submitted", got, "Submitted as request #", "(an update)")
        upd = re.search(r"Submitted as request #(\d+)", got).group(1)
        check("publish: the update shows what changed", rev.command("submission " + upd), "glade.cs  sha256", "(changed)")
        check("publish: the update is approved", rev.command("approve " + upd), "is approved and published to /domains/area")
        check("publish: the update is in place", open(os.path.join(pub, "glade.cs")).read(), "built and rebuilt")
        # Rejection and withdrawal.
        h.edit(mudlib, W + "area/inner", "A thicket.", "A dark thicket.")
        rid = re.search(r"Submitted as request #(\d+)", wren.command("submit ~/area")).group(1)
        check("publish: a reviewer rejects with a reason", rev.command(f"reject {rid} too dark"), f"Request #{rid} is rejected.")
        check("publish: the submitter is told the reason", wren.read_until("too dark", timeout=5), "rejected your request", "too dark")
        wid = re.search(r"Submitted as request #(\d+)", wren.command("submit ~/area")).group(1)
        check("publish: only the submitter withdraws", rev.command(f"withdraw {wid}"), "is not yours")
        check("publish: the submitter withdraws", wren.command(f"withdraw {wid}"), f"Request #{wid} is withdrawn.")
        # A file that compiles differently after the move: its real dependency changed between submission and approval.
        h.edit(mudlib, W + "area/inner", "A dark thicket.", "A darker thicket.")
        cid = re.search(r"Submitted as request #(\d+)", wren.command("submit ~/area")).group(1)
        h.edit(mudlib, T + "base", "public virtual int Shine() => 1;", "public int Glow() => 1;")
        chief.command("update " + T + "base")
        got = rev.command(f"approve {cid}")
        check("publish: a compile failure on approval is refused", got, "Not approved: publishing failed, and nothing was left behind.")
        check("publish: the published version before it is intact", open(os.path.join(pub, "inner.cs")).read(), absent=("darker",))
        check("publish: the published files are all still there", ",".join(sorted(os.listdir(pub))), "beast.cs,glade.cs,inner.cs,shiny.cs")
        h.edit(mudlib, T + "base", "public int Glow() => 1;", "public virtual int Shine() => 1;")
        chief.command("update " + T + "base")
        # Revocation.
        check("publish: a level 5 wizard cannot revoke", rev.command(f"revoke {upd}"), "You need wizard level 10 to revoke")
        got = chief.command(f"revoke {upd} : test over")
        check("publish: a level 10 wizard revokes", got, f"Publication #{upd} is revoked", "file(s) removed from /domains/area")
        check("publish: the files are gone", "gone" if not os.path.exists(os.path.join(pub, "glade.cs")) else "present", "gone")
        # The waiting notice at login.
        rid2 = re.search(r"Submitted as request #(\d+)", wren.command("submit ~/area")).group(1)
        rev.command("quit", sync=False, marker="Goodbye")
        rev = h.Client("rev")
        check("publish: waiting requests are counted at login", rev.read_until("waiting", timeout=10), "publishing request(s) are waiting")
        rev.read_until("> ")
        check("publish: the register survives a look", chief.command("submissions all"), f"#{rid2} waiting")

        # ================================================================ tags and the control rule (docs/specs/tags-and-saves.md)
        chief.command("clone /domains/hollow/obj/rusty_sword")
        chief.command("give sword to wren")
        wren.command("lab")
        wren.command("clone " + W + "tagmeddle")
        got = wren.command("tagmeddle")
        check("tags: a workshop object cannot change a real weapon's tags or the registry, or set the control rule's dice or clock, or record an effect",
              got, "addtag=null", "settags=null", "registry=null", "settest=null", "clock=null", "forget=null", "resolve=null", absent=("addtag=True", "settest=True", "resolve=2", "resolve=1"))
        check("tags: it may read tags and the pure rule", got, "read=True", "chance=")
        check("tags: the setprop path changed nothing", got, "setprop=False")

        # ================================================================ there is no test-only path in this server
        out = open(os.path.join(work, "driver.log")).read()
        check("production: the server log shows no test mode", out, absent=("TEST MODE",))
    finally:
        try:
            proc.terminate()
            proc.wait(timeout=20)
        except Exception:
            proc.kill()
    print()
    print(f"{len(h.failures)} failure(s)")
    if not h.failures:
        shutil.rmtree(work, ignore_errors=True)
    else:
        print("work directory kept:", work)
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
