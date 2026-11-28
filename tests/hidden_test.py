#!/usr/bin/env python3
"""Tests of the hidden-class pattern (docs/specs/hidden-classes.md), with a TEST-ONLY fixture class, not the real one.

The fixture: a hidden advanced class `hdfix` for the Mage (summons, gated door, a giver with a one-line case), an ordinary advanced class
`hdplain`, creatures of every level, and a probe in the arena that kills things, shares kills and calls the register as another object would.

  1. The register: the chance table (fixed dice), eligibility line by line, the loot counter, the certain 50th, the statistics.
  2. Delivery: the line, the bound paper, drop/give/sell refused, kept at death, re-issued by the door, `read`.
  3. The door: What? for everyone without it, byte for byte; the paper opens it; exits, look, a fight, a holder.
  4. The quest: begins on arrival, not abandonable, the offer, pledge twice / decline twice, the minute, withdrawn; the middle of each step
     through quit, a dropped connection, a takeover and a restart of the driver.
  5. Skills at a level, the trainer, `help`, the lists, other players, the Chronicle, the wizard command and a wizard's objects.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=5070 TMPDIR=/tmp/hiddenagent python3 tests/hidden_test.py
"""
import json
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "5070")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import gmcp_test as g  # noqa: E402
import area_test as a  # noqa: E402

check = h.check
truth = g.truth
T = "/domains/hdtest/"
PW = h.PASSWORD

CLASS = '''// @inherit /std/pclass
public class HdFix : PlayerClass
{
    public override void Create()
    {
        ClassName = "Test Seat"; Title = "Sitter of the Test"; Tagline = "A test-only seat."; Tier = "advanced"; Parent = "mage"; Guild = "hdtest";
        Description = "The Sitter of the Test is a fixture. It sits.";
        Hidden = true;
        Require("level", "5"); Require("class", "mage"); Require("hidden", "hdfix");
        QuestId = "hd-quest"; GateKey = "hd-door"; SerialBase = 700;
        SummonsItem = "/domains/hdtest/obj/hd_summons";
        OwnPaths.Add("/domains/hdtest/inner"); OwnPaths.Add("/domains/hdtest/npc/"); OwnPaths.Add("/classes/hdfix"); OwnPaths.Add("/skills/hd_sit");
        SummonsFound = "Among the oddments you take from {corpse}, a slip of grey paper with your name on it.";
        SummonsText = "TEST SUMMONS No. {serial} for {Name}, issued {date}. Present at the plain door.";
        BoundLine = "It is addressed to you. It is not a thing one parts with.";
        GateKnock = "You knock. The door opens as if it had been about to.";
        GateAgain = "You knock. 'Come in,' says a voice. 'You are expected.'";
        GateOpen = "The door opens before you knock. It has your file.";
        GateLook = "The door has a brass knocker. It looks as though it was expecting someone with paper.";
        GateReissue = "'Paper,' says the voice, and a duplicate comes through the hatch.";
        Skills.Add("hd_sit"); SkillAt("hd_sit", 7);
        Weapons.AddRange(new[] { "dagger", "staff" }); ArmorTypes.Add("light");
    }
}
'''

PLAIN = '''// @inherit /std/pclass
public class HdPlain : PlayerClass
{
    public override void Create()
    {
        ClassName = "Plain Seat"; Title = "Plain Sitter"; Tier = "advanced"; Parent = "mage"; Tagline = "Test only."; Description = "Ordinary advanced class for the test.";
        Require("level", "5");
    }
}
'''

SKILL = '''// @inherit /std/skill
public class HdSit : Skill
{
    public override void Create()
    {
        Title = "Sit"; Command = "hdsit"; Kind = "skill"; Summary = "Sits."; EnduranceCost = 1; Preparation = 0; Recovery = 1.0; Target = "none";
        HelpText = "Sit: the fixture's one skill, at level 7.";
    }
    public override void Resolve(ObjectRef user, ObjectRef? target) { TellObject(user, "You sit.\\n"); }
}
'''

SUMMONS = '''// @inherit /std/summons
public class HdSummons : SummonsPaper
{
    public override void Create()
    {
        base.Create();
        ClassId = "hdfix";
        Name = "summons"; Ids.Add("summons"); Ids.Add("paper"); ShortDesc = "a summons";
        LongDesc = "A grey paper, folded in three. You could {cmd}read{/} it.";
    }
}
'''

KEEPSAKE = '''// @inherit /std/item
public class HdCert : Item
{
    public override void Create() { Name = "certificate"; Ids.Add("certificate"); ShortDesc = "a test certificate"; LongDesc = "A keepsake."; Value = 0; }
}
'''

def monster(n, extra="", boss=False, xp=None, name=None):
    xp = 10 + n if xp is None else xp
    nm = name or f"hdm{n}"
    return f'''// @inherit /std/monster
public class {nm.capitalize()} : Monster
{{
    public override void Create()
    {{
        Name = "{nm}"; Ids.Add("{nm}"); Ids.Add("mob"); ShortDesc = "a {nm}"; LongDesc = "A test creature of level {n}.";
        Level = {n}; MaxVitality = 3; DamageMin = 1; DamageMax = 1; ToHit = 0; Armor = 0; XpValue = {xp}; GoldMin = 3; GoldMax = 3; Aggressive = false; AddLoot("/domains/hdtest/obj/hd_cert", 100);
        {"Boss = true;" if boss else ""} {extra}
    }}
}}
'''

ARENA = '''// @inherit /std/room
public class HdArena : Room
{
    public override void Create()
    {
        ShortDesc = "The Test Arena"; LongDesc = "A bare arena for the hidden class test. Light is not an issue here.";
        Exits["north"] = "/domains/hdtest/gate";
        MoveObject(CloneObject("/domains/hdtest/probe"), Self);
    }
}
'''

# A copy of City Hall's door as the real one is built: details, a readable, one exit.
GATE = '''// @inherit /std/room
public class HdGate : Room
{
    public override void Create()
    {
        ShortDesc = "The Test Hall"; LongDesc = "A high plain hall. Above the inner door a line of carved letters has been picked out in old gilt.";
        Exits["out"] = "/domains/hdtest/arena";
        MoveObject(CloneObject("/domains/hdtest/probe"), Self);
        AddDetail("letters,lintel,motto,inner door,door,gilt", "Carved over the inner door are the words of the motto. You could {cmd}read{/} them.");
        AddReadable("letters,lintel,motto,inscription", "Carved over the inner door:\\n\\"FROM STRONG ROOTS\\"", "Carved over the inner door.");
        GatedExit("in", "/domains/hdtest/inner", "hd-door", "in", "enter", "knock", "open");
        GateDetail("door,inner door");
    }
}
'''

INNER = '''// @inherit /std/room
public class HdInner : Room
{
    public override void Create()
    {
        ShortDesc = "The Test Waiting Room"; LongDesc = "A waiting room.";
        Exits["out"] = "/domains/hdtest/gate";
        MoveObject(CloneObject("/domains/hdtest/probe"), Self);
        AddSpawn("/domains/hdtest/npc/giver");
    }
}
'''

GIVER = '''// @inherit /std/hiddengiver
public class HdGiver : HiddenGiver
{
    public override void Create()
    {
        Name = "Registrar"; Ids.Add("registrar"); ShortDesc = "the test registrar"; LongDesc = "She files.";
        HiddenId = "hdfix"; QuestTitle = "Test Duty";
        DefineQuest("hd-quest", "Test Duty", "The Test Waiting Room", "You did your test duty.", "You did not.", "", "", false);
        RewardTakenText = "1 gold and the seat"; RewardDeclinedText = "1 gold and a certificate"; RewardReleasedText = "1 gold";
        PayGold = 1; PayXp = 250; KeepsakeDeclined = "/domains/hdtest/obj/hd_cert";
        OfferLine = "Registrar says: Will you take the seat, {name}? Say pledge, or decline.";
        PermanenceLine = "Registrar says: It is for life. Say pledge again within the minute.";
        DeclineWarning = "Registrar says: Say decline again within the minute and it is closed for good.";
        AcceptedLine = "Registrar says: It is entered.";
        DeclinedLine = "Registrar says: The Chancery does not summon twice.";
        ReleaseLine = "Registrar says: You have taken another seat. The fine is waived.";
        TeachCalling("hdfix", 2, 5);
    }
    protected override int ConfirmSeconds => 4;
    protected override void Begin(ObjectRef who) { QuestSay(who, "hd-quest", "Registrar says: Welcome, juror."); }
    protected override string? Note(ObjectRef who, int stage) => stage >= 5 ? "The case is heard." : "The case is open.";
    protected override void Noticed(ObjectRef other)
    {
        base.Noticed(other);
        if (Interactive(other)) AddAction("finish", a => { var p = ThisPlayer()!; if (!AdvanceTo(p, 3) || !AdvanceTo(p, 4) || !AdvanceTo(p, 5)) return false; QuestSay(p, "hd-quest", OfferLine.Replace("{name}", "juror")); return true; });
    }
}
'''

PROBE = r'''// @inherit /std/thing
public class HdProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        foreach (var v in new[] { "hspawn", "hkill", "hshare", "hdie", "hplain", "hcall", "hclass", "hitems", "hstate", "htimers", "hvanish", "hlist" }) AddAction(v, Do);
    }
    private ObjectRef? FirstMob(ObjectRef me) { foreach (var o in AllInventory(Environment(me)!).ToList()) if (o != me && o.IsAlive && o.Call<bool>("IsMonster")) return o; return null; }
    private static object? Arg(string a) { if (a == "true") return true; if (a == "false") return false; if (int.TryParse(a, out var n)) return n; return a; }
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (QueryVerb())
        {
            case "hspawn": for (var i = 0; i < (w.Length > 1 ? int.Parse(w[1]) : 1); i++) MoveObject(CloneObject("/domains/hdtest/" + w[0]), Environment(me)!); Write("SPAWNED\n"); return true;
            case "hkill": { var m = FirstMob(me); if (m == null) { Write("NOMOB\n"); return true; } m.Call("Die", me); Write("KILLED\n"); return true; }
            case "hshare": { var m = FirstMob(me); if (m == null) { Write("NOMOB\n"); return true; }
                foreach (var o in AllInventory(Environment(me)!).ToList()) if (o.IsAlive && o.Call<bool>("IsPlayerCharacter") && w.Skip(1).Contains((o.Call<string>("QueryName") ?? "").ToLowerInvariant())) m.Call("ApplyDamage", 1, o);
                m.Call("Die", me); Write("KILLED\n"); return true; }
            case "htimers": { foreach (var c in AllInventory(Environment(me)!).Where(o => o.Call<bool>("IsCorpse")).ToList()) c.Call("SetTimers", int.Parse(w[0]), int.Parse(w[1]), int.Parse(w[2])); Write("TIMERS\n"); return true; }
            case "hvanish": { foreach (var i in AllInventory(me).ToList()) if (i.Call<string>("Short") == "a summons") i.Call("Vanish"); Write("VANISHED\n"); return true; }
            case "hlist": { var c = LoadObject("/secure/classes"); Write("LIST children=" + string.Join(",", c.Call<List<string>>("EnabledChildren", "mage", "advanced") ?? new List<string>()) + " advanced=" + string.Join(",", c.Call<List<string>>("EnabledOfTier", "advanced") ?? new List<string>()) + " archetypes=" + string.Join(",", c.Call<List<string>>("EnabledOfTier", "archetype") ?? new List<string>()) + "\n"); return true; }
            case "hdie": me.Call("Die", (ObjectRef?)null); Write("DIED\n"); return true;
            case "hplain": Write("PLAIN " + (me.Call<string>("ApplyClass", "hdplain") ?? "ok") + "\n"); return true;
            case "hclass": Write("CLASSES " + string.Join(",", me.Call<List<string>>("QueryClassIds") ?? new List<string>()) + " | tier=" + me.Call<string>("QueryClassOfTier", "advanced") + " | title=" + me.Call<string>("QueryClassTitle") + "\n"); return true;
            case "hitems": Write("ITEMS " + string.Join(",", AllInventory(me).Select(i => i.Call<string>("Short"))) + "\n"); return true;
            case "hcall":
            {
                // hcall FUNCTION args...  (as this real object, on the register)
                try { var r = LoadObject("/secure/hidden").Call(w[0], w.Skip(1).Select(Arg).ToArray()); Write("CALL " + (r?.ToString() ?? "null") + "\n"); }
                catch (MudRuntimeException e) { Write("CALL ERR " + e.Message + "\n"); }
                return true;
            }
            case "hstate":
            {
                var reg = LoadObject("/secure/hidden");
                Write($"STATE offer={reg.Call<bool>("OfferIsOpen", me, "hdfix")}\n"); return true;
            }
        }
        return false;
    }
}
'''

WIZ_MEDDLER = r'''// @inherit /std/item
public class HdMeddler : Item
{
    public override void Create() { Name = "meddler"; Ids.Add("meddler"); ShortDesc = "a meddler"; }
    public override void Init() { base.Init(); AddAction("hmeddle", Meddle); }
    private bool Meddle(string args)
    {
        var me = ThisPlayer()!;
        var reg = LoadObject("/secure/hidden");
        foreach (var fn in new[] { "RollOnLoot", "Admits", "GateLine", "GateStage", "Reissue", "SerialOf", "Advance", "Note", "NoteOf", "Answer", "StageFor", "OutcomeFor", "OfferIsOpen", "SetTestRolls", "DiceMode", "Report", "Grant", "Reset", "SetLooted", "Simulate", "Refusals" })
        {
            try { var r = reg.Call(fn, me, "hd-door", "hdfix", 1); Write("MEDDLE " + fn + "=" + (r?.ToString() ?? "null") + "\n"); }
            catch (Exception) { Write("MEDDLE " + fn + "=err\n"); }
        }
        var classes = LoadObject("/secure/classes");
        try { Write("MEDDLE HiddenIds=" + string.Join(",", classes.Call<List<string>>("HiddenIds") ?? new List<string>()) + "\n"); } catch (Exception) { Write("MEDDLE HiddenIds=err\n"); }
        try { Write("MEDDLE Children=" + string.Join(",", classes.Call<List<string>>("EnabledChildren", "mage", "advanced") ?? new List<string>()) + "\n"); } catch (Exception) { }
        Write("MEDDLE ClassIds=" + string.Join(",", me.Call<List<string>>("QueryClassIds") ?? new List<string>()) + "\n");
        Write("MEDDLE Tier=" + me.Call<string>("QueryClassOfTier", "advanced") + "\n");
        Write("MEDDLE Has=" + me.Call<bool>("QueryHasClass", "hdfix") + "\n");
        Write("MEDDLE Title=" + me.Call<string>("QueryClassTitle") + "\n");
        return true;
    }
}
'''


PLAYER_ACTIONS = ('        AddAction("xp", a => { AddXp(int.Parse(a)); return true; });\n'
                  '        AddAction("tp", a => { MoveObject(LoadObject(a)); Look(); return true; });\n'
                  '        AddAction("mk", a => { MoveObject(CloneObject(a), Self); return true; });\n')


def prepare(mudlib):
    path = os.path.join(mudlib, "std", "player.cs")
    src = open(path).read()
    anchor = '        AddAction("channels", a => ToChannels("Listing", ""));'
    assert anchor in src
    open(path, "w").write(src.replace(anchor, PLAYER_ACTIONS + anchor, 1))
    # any character may use setlevel here (a test aid only; in the game it is a level 10 command)
    h.edit(mudlib, "/std/adventurer", "if (!IsWizard(LevelSetLevel)) return false;", "")
    for d in ("domains/hdtest", "domains/hdtest/npc", "domains/hdtest/obj"):
        os.makedirs(os.path.join(mudlib, d), exist_ok=True)
    h.write(mudlib, "/classes/hdfix", CLASS)
    h.write(mudlib, "/classes/hdplain", PLAIN)
    h.write(mudlib, "/skills/hd_sit", SKILL)
    h.write(mudlib, T + "obj/hd_summons", SUMMONS)
    h.write(mudlib, T + "obj/hd_cert", KEEPSAKE)
    for n in range(1, 13):
        h.write(mudlib, T + f"hdm{n}", monster(n))
    h.write(mudlib, T + "hdtank", monster(1, extra="MaxVitality = 5000;", xp=0, name="hdtank"))
    h.write(mudlib, T + "hdboss", monster(7, boss=True, name="hdboss"))
    h.write(mudlib, T + "hdzero", monster(7, xp=0, name="hdzero"))
    h.write(mudlib, T + "hdnosum", monster(7, extra="NoSummons = true;", name="hdnosum"))
    h.write(mudlib, T + "arena", ARENA)
    h.write(mudlib, T + "gate", GATE)
    h.write(mudlib, T + "inner", INNER)
    h.write(mudlib, T + "npc/giver", GIVER)
    h.write(mudlib, T + "probe", PROBE)


def body(text):
    return "\n".join(line for line in text.replace("\r", "").split("\n") if line.strip() and not re.match(r"^\w* ?\(\d+/\d+\)", line)).strip()


def oneline(text):
    return re.sub(r"\s+", " ", text)


def login(name, archetype="mage"):
    c = h.Client(name, archetype=archetype)
    c.read_until("> ")
    return c


def relog(c, name, how):
    """Leave the game the way `how` says and come back: quit, drop (a closed connection), takeover (a second connection)."""
    if how == "quit":
        c.send("quit")
        c.closed(5)
        time.sleep(0.5)
        n = h.Client(name)
        n.read_until("> ")
        return n
    if how == "drop":
        c.sock.close()
        time.sleep(1.5)
        n = h.Client(name)
        n.read_until("> ")
        return n
    n = h.Client(name, login=False)
    n.read_until("Password: ")
    n.send(PW)
    n.choose_calling(None)
    n.read_until("> ", 15.0)
    return n


def kill_loot(c, mob, n=1):
    """Spawn a creature, kill it and loot its corpse; the text of the loot."""
    c.command("hspawn " + mob)
    c.command("hkill")
    return c.command("loot")


WN = {5: "five", 8: "eight", 12: "twelve"}
FOUND = "a slip of grey paper with your name on it"



def mage(name, level=5, to_arena=True):
    c = login(name)
    c.command(f"setlevel {level}")
    if to_arena:
        c.command("tp " + T + "arena")
    a.drain(c, 0.3)
    return c


def wizard(name="hdwiz"):
    w = h.Client(name, archetype="warrior")
    w.read_until("> ")
    check("the secret makes the first wizard", w.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(w, 0.4)
    return w


def rec(w, name):
    """The register's line for a character: (looted, issued, stage, outcome) or None when there is no record."""
    m = None
    for _ in range(3):
        out = w.command("hidden hdfix")
        m = re.search(rf"(?<![A-Za-z]){name}: looted (\d+), issued (no|yes #\d+), stage (\d+), outcome (\d+)", out)
        if m or f"{name}:" not in out and "looted" in out: break
    return None if not m else (int(m.group(1)), m.group(2) != "no", int(m.group(3)), int(m.group(4)))


def tenths(n, floor=10, base=2.0, step=0.5, guarantee=50):
    if n >= guarantee: return 1000
    if n <= floor: return 0
    return int(round((base + step * (n - floor - 1)) * 10))


def step_roll(w):
    print("\n=== 1. the register: roll and eligibility")
    w.command("hidden dice 0")
    ann = mage("hdann")
    # 1-10: no roll even with the luckiest dice; the 11th is served at once
    for i in range(1, 11):
        out = kill_loot(ann, "hdm5")
        if FOUND in out: check(f"loot {i}: nothing is served in the first ten", out, absent=(FOUND,))
    truth("ten eligible loots counted, nothing issued", rec(w, "hdann") == (10, False, 0, 0), rec(w, "hdann"))
    out = kill_loot(ann, "hdm5")
    check("the 11th eligible loot with a roll of 0: served", out, FOUND, "corpse of a hdm5")
    truth("recorded: issued, stage 1, serial 1", rec(w, "hdann") == (11, True, 1, 0), rec(w, "hdann"))
    out = kill_loot(ann, "hdm5")
    check("once served it is over: no second summons", out, absent=(FOUND,))
    truth("the counter stops counting after it is served", rec(w, "hdann")[0] == 11, rec(w, "hdann"))
    # the table, exactly: served at roll tenths-1, not at roll tenths
    ben = mage("hdben")
    for n in (11, 15, 20, 25, 30, 37, 40, 45, 49):
        t = tenths(n)
        w.command(f"hidden count hdben hdfix {n - 1}")
        w.command(f"hidden reset hdben hdfix") if False else None
        w.command(f"hidden dice {t}")
        out = kill_loot(ben, "hdm5")
        check(f"loot {n} at roll {t} (chance {t / 10}%): not served", out, absent=(FOUND,))
        w.command(f"hidden count hdben hdfix {n - 1}")
        w.command(f"hidden dice {t - 1}")
        out = kill_loot(ben, "hdm5")
        check(f"loot {n} at roll {t - 1}: served", out, FOUND)
        w.command("hidden reset hdben hdfix")
    w.command("hidden count hdben hdfix 49")
    w.command("hidden dice 999")
    check("the 50th is certain even at the worst roll", kill_loot(ben, "hdm5"), FOUND)
    w.command("hidden reset hdben hdfix")
    w.command("hidden count hdben hdfix 77")
    check("and so is any later one (a counter that ran past 50)", kill_loot(ben, "hdm5"), FOUND)
    # eligibility, line by line (dice 0: the first eligible loot after the floor is served; the counter shows who was counted)
    w.command("hidden dice 0")
    print("--- eligibility")
    def counted(c, name, mob, level=None, calling="mage"):
        if level: c.command(f"setlevel {level}")
        before = rec(w, name)
        w.command(f"hidden count {name} hdfix 0") if before else None
        kill_loot(c, mob)
        after = rec(w, name)
        return (after[0] if after else 0) - (0 if not before else 0)
    cal = login("hdcal", archetype="warrior"); cal.command("setlevel 5"); cal.command("tp " + T + "arena")
    kill_loot(cal, "hdm5")
    truth("a warrior is not counted", rec(w, "hdcal") is None, rec(w, "hdcal"))
    low = mage("hdlow", 4)
    kill_loot(low, "hdm5")
    truth("a level 4 mage is not counted", rec(w, "hdlow") is None, rec(w, "hdlow"))
    plain = mage("hdpl", 5)
    check("an ordinary advanced class is taken first", plain.command("hplain"), "PLAIN ok")
    kill_loot(plain, "hdm5")
    truth("a mage with an ordinary advanced class is never counted", rec(w, "hdpl") is None, rec(w, "hdpl"))
    wiz_out = kill_loot(w, "hdm5")
    truth("a wizard is not counted", rec(w, "hdwiz") is None and FOUND not in wiz_out, wiz_out[-200:])
    # the window: level 5: 4..7; level 8: 7..10; level 12: 9..14
    for lvl, ok in ((5, (4, 5, 6, 7)), (8, (7, 8, 9, 10)), (12, (9, 10, 11, 12))):
        c = mage(f"hdw{WN[lvl]}", lvl)
        for m in range(1, 13):
            before = rec(w, f"hdw{WN[lvl]}")
            kill_loot(c, f"hdm{m}")
            after = rec(w, f"hdw{WN[lvl]}")
            was = (before[0] if before else 0)
            now = after[0] if after else 0
            want = m in ok
            truth(f"level {lvl} against a level {m} creature: {'counted' if want else 'not counted'}", (now - was == 1) == want, (before, after))
            if after and after[1]: w.command(f"hidden reset hdw{WN[lvl]} hdfix")
            if after and after[1] is False and now >= 9: w.command(f"hidden count hdw{WN[lvl]} hdfix 0")
    for mob, why in (("hdboss", "a boss"), ("hdzero", "a creature worth no experience"), ("hdnosum", "a creature flagged NoSummons")):
        c = mage("hdx" + mob[2:], 6)
        kill_loot(c, mob)
        truth(f"{why} is not counted", rec(w, "hdx" + mob[2:]) is None, rec(w, "hdx" + mob[2:]))
    # one corpse, two loots: one roll
    two = mage("hdtwo", 5)
    two.command("hspawn hdm5"); two.command("hkill")
    two.command("get certificate from corpse"); two.command("loot")
    truth("one corpse looted in two goes rolls once", rec(w, "hdtwo") == (1, False, 0, 0), rec(w, "hdtwo"))
    # not entitled: another character, and after the lock
    kim = mage("hdkim", 5)
    two.command("hspawn hdm5"); two.command("hkill")
    out = kim.command("loot")
    check("someone who shared nothing may not loot during the lock", out, "not yours")
    truth("and is not counted", rec(w, "hdkim") is None, rec(w, "hdkim"))
    w.command("htimers 1 100 100")
    time.sleep(2.2)
    kim.command("loot")
    truth("after the lock a scavenger is not counted either", rec(w, "hdkim") is None, rec(w, "hdkim"))
    # a shared kill: each rolls on their own
    sa = mage("hdsa", 5); sb = mage("hdsb", 5)
    w.command("hidden count hdsa hdfix 10")
    sa.command("hspawn hdm5")
    sb.command("look")
    sa.command("hshare hdsa hdsb")
    out_a = sa.command("get certificate from corpse"); out_b = sb.command("loot")
    truth("a shared kill: both were counted", rec(w, "hdsa") is not None and rec(w, "hdsb") is not None, (rec(w, "hdsa"), rec(w, "hdsb")))
    truth("... the one at the 11th was served, the other's miss does not touch it", FOUND in (out_a + out_b) and (FOUND in out_a) != (FOUND in out_b) and rec(w, "hdsa")[1] and not rec(w, "hdsb")[1], (rec(w, "hdsa"), rec(w, "hdsb")))
    check("only the one served was told", out_a if FOUND in out_a else out_b, FOUND)
    # a player's corpse yields nothing
    w.command("hidden count hdkim hdfix 0")
    sb.command("hdie")
    sb.command("tp " + T + "arena")
    kim.command("loot")
    truth("a player's corpse is not counted", rec(w, "hdkim") is None or rec(w, "hdkim")[0] == 0, rec(w, "hdkim"))
    # statistics: the exact mean of the formula, from the register's own dice
    sim = w.command("hidden simulate hdfix 20000")
    m = re.search(r"20000 trials: mean ([\d.]+), median (\d+), 90th (\d+), never (\d+) before, longest (\d+)", sim)
    exact = 0.0; prev = 1.0
    for n in range(1, 51):
        t = tenths(n) / 1000
        exact += n * prev * t
        prev *= 1 - t
    truth(f"statistics: the exact mean of the formula is {exact:.2f} (spec: 24.8)", abs(exact - 24.8) < 0.1, exact)
    truth("the simulated mean is within 0.4 of it, the median 24, 90 in 100 by the 37th, never before the 11th, never after the 50th",
          bool(m) and abs(float(m.group(1)) - exact) < 0.4 and int(m.group(2)) in (23, 24, 25) and int(m.group(3)) in (36, 37, 38) and int(m.group(4)) >= 11 and int(m.group(5)) <= 50, sim)
    w.command("hidden dice random")
    return ann


def plain(text):
    """A reply without prompts, for comparing what two commands print."""
    return re.sub(r"\s+", " ", body(text)).strip()


def serve(w, c, name, count=10):
    """Give a character the summons through the real roll (the 11th loot, dice at 0); the text of the loot."""
    w.command("hidden dice 0")
    w.command(f"hidden count {name} hdfix {count}")
    out = kill_loot(c, "hdm5")
    return out


def at_gate(c):
    c.command("tp " + T + "gate")
    a.drain(c, 0.3)


def inner(c):
    c.command("tp " + T + "inner")
    a.drain(c, 0.5)


def step_deliver(w):
    print("\n=== 2. delivery, the bound paper, death, re-issue")
    dan = mage("hddan", 6)
    bob = mage("hdbob", 6)
    out = serve(w, dan, "hddan")
    check("the finder is told, by the class's own line", out, FOUND, "from the corpse of a hdm5")
    bob_out = bob.command("look")
    check("the other character in the room sees nothing of it", bob_out, absent=("slip of grey", "summons", "oddments"))
    check("the paper is in the pack as 'a summons'", dan.command("i"), "a summons")
    text = dan.command("read summons")
    m = re.search(r"TEST SUMMONS No\. (\d+) for (\w+), issued (.+)\. Present", text)
    truth("read: the text with the serial, the name and the game date", bool(m) and int(m.group(1)) > 700 and m.group(2).lower() == "hddan" and "Year" in m.group(3), text[-200:])
    check("look at summons", dan.command("look at summons"), "A grey paper, folded in three.")
    for cmd in ("drop summons", "drop all", "drop paper"):
        out = dan.command(cmd)
        check(f"`{cmd}` is refused in the paper's own words", out, "It is addressed to you. It is not a thing one parts with.")
        check(f"... and it is still in the pack after `{cmd}`", dan.command("i"), "a summons")
    out = dan.command("give summons to hdbob")
    check("give is refused", out, "not a thing one parts with")
    check("... and the other has nothing", bob.command("i"), absent=("summons",))
    dan.command("hdie")
    dan.command("tp " + T + "arena")
    check("a death leaves the paper with the character", dan.command("i"), "a summons")
    check("... and no corpse holds it", dan.command("look in corpse"), absent=("summons",))
    # destroy it: the door re-issues it
    dan.command("hvanish")
    check("the paper is gone", dan.command("i"), absent=("summons",))
    dan.command("north")
    out = dan.command("knock")
    check("the door says the class's re-issue line and the paper comes back", out, "a duplicate comes through the hatch", "You knock.")
    check("it is in the pack again", dan.command("i"), "a summons")
    for who_, label in ((dan.command("hclass"), "holder of a paper"),):
        check(f"{label}: the class is not on the character (a summons is not a class)", who_, absent=("hdfix",))
    truth("the register: issued, stage 1 or 2, serial counted once", (lambda r: r is not None and r[1])(rec(w, "hddan")), rec(w, "hddan"))
    return dan, bob


DOOR_VERBS = ("in", "enter", "knock", "open door", "go in", "knock on door", "open", "enter door", "open inner door")


def step_door(w):
    print("\n=== 3. the door")
    kim = mage("hdkd", 6)
    at_gate(kim)
    unknown = plain(kim.command("flarb"))
    for v in DOOR_VERBS:
        check(f"without a summons `{v}` prints what an unknown command prints", plain(kim.command(v)), unknown)
    base_look = plain(kim.command("look"))
    truth("the exits line shows `out` only", "Obvious exits: out" in base_look and " in" not in base_look.split("Obvious exits:")[1], base_look[-120:])
    base_door = plain(kim.command("look at door"))
    base_inner = plain(kim.command("look at inner door"))
    base_letters = plain(kim.command("read letters"))
    check("look at door is the room's own text", base_door, "Carved over the inner door are the words of the motto.", absent=("brass knocker",))
    w.command("tp " + T + "gate")
    for v in DOOR_VERBS[:4]:
        check(f"a wizard gets nothing from `{v}` either", plain(w.command(v)), unknown)
    # with a summons
    ann = mage("hdanb", 6)
    serve(w, ann, "hdanb")
    at_gate(ann)
    look = plain(ann.command("look"))
    truth("with the paper the exits line also shows `in`", "Obvious exits:" in look and re.search(r"Obvious exits: [^\n]*\bin\b", look) is not None, look[-150:])
    check("look at door: the room's text, then the class's line", plain(ann.command("look at door")), base_door, "brass knocker")
    truth("look at inner door the same", plain(ann.command("look at inner door")).startswith(base_inner), plain(ann.command("look at inner door")))
    truth("read letters is unchanged", plain(ann.command("read letters")) == base_letters, plain(ann.command("read letters")))
    # a fight blocks it
    ann.command("hspawn hdtank")
    ann.command("kill hdtank")
    check("in a fight the usual refusal", ann.command("in"), "You are in the middle of a fight")
    ann.command("flee")
    a.drain(ann, 1.0)
    w.command("tp " + T + "gate")
    w.command("hkill")
    ann.command("tp " + T + "gate")
    a.drain(ann, 0.5)
    out = ann.command("knock")
    check("knock: the class's line, and in", out, "The door opens as if it had been about to.", "The Test Waiting Room")
    a.drain(ann, 1.8)
    return ann


def step_quest(w):
    print("\n=== 4. the quest, answered both ways, and the middle of each step")
    flarb = None
    cy = mage("hdcy", 5)
    w.command("hidden grant hdcy hdfix")
    check("the register: summoned", w.command("hidden hdfix"), "hdcy: looted 0, issued yes", "stage 1")
    cy = relog(cy, "hdcy", "quit")
    check("after quit and login the paper is still there", cy.command("i"), "a summons")
    at_gate(cy)
    out = cy.command("knock")
    check("arriving begins the quest", out, "The Test Waiting Room")
    out = cy.command("look") + (a.drain(cy, 1.6) or "")
    q = cy.command("quests")
    check("it is in `quests`", q, "Test Duty")
    check("... with the giver's welcome in the journal", cy.command("quest duty"), "Test Duty (active)", "Welcome, juror.", "The case is open.")
    check("the register: admitted", w.command("hidden hdfix"), "hdcy: looted 0, issued yes #", "stage 2")
    out = cy.command("abandon duty")
    check("it cannot be abandoned", out, "cannot be abandoned")
    unknown = plain(cy.command("flarb"))
    check("`pledge` before the offer is not a command", plain(cy.command("pledge")), unknown)
    check("nor `decline`", plain(cy.command("decline")), unknown)
    cy = relog(cy, "hdcy", "drop")
    inner(cy)
    check("after a dropped connection the quest and the stage are as they were", cy.command("quests"), "Test Duty")
    check("... the register agrees", w.command("hidden hdfix"), "stage 2")
    out = cy.command("finish")
    check("the case ends: the offer is made", out, "Will you take the seat")
    check("the progress note changed", cy.command("quest duty"), "The case is heard.")
    cy = relog(cy, "hdcy", "takeover")
    inner(cy)
    a.drain(cy, 1.6)
    check("after a takeover the offer is still open", w.command("hidden hdfix"), "stage 5")
    out = cy.command("pledge")
    check("the first pledge says the permanence line", out, "It is for life", absent=("You are now",))
    check("and nothing was taken", cy.command("hclass"), "CLASSES mage", absent=("hdfix",))
    time.sleep(5.2)
    out = cy.command("pledge")
    check("after the minute, `pledge` starts over", out, "It is for life", absent=("You are now",))
    gold = cy.command("score")
    out = cy.command("pledge")
    check("the second pledge within the minute takes the class", out, "You are now a Sitter of the Test.", "It is entered.")
    check("the class is held, `score` shows it", cy.command("score"), "Class Sitter of the Test")
    check("`hclass` agrees", cy.command("hclass"), "hdfix", "title=Sitter of the Test")
    check("the quest is completed with the authored summary and the reward", cy.command("quests completed"), "Test Duty", "1 gold and the seat")
    check("the paper is kept by the court", cy.command("i"), absent=("summons",))
    check("the register: taken", w.command("hidden hdfix"), "hdcy: looted 0, issued yes #", "stage 9, outcome 1")
    check("a second pledge is not a command any more", plain(cy.command("pledge")), unknown)
    at_gate(cy)
    check("a holder walks in without knocking", cy.command("knock"), "The door opens before you knock. It has your file.", "The Test Waiting Room")
    a.drain(cy, 1.0)
    check("the holder's `help` for the class", cy.command("help hdfix"), "Sitter of the Test", "It sits.")
    check("`skills` has no Sit yet (it comes at level 7)", cy.command("skills"), absent=("Sit",))
    check("`help hdsit` is the ordinary help page at level 5", cy.command("help hdsit"), "New here?", absent=("fixture's one skill",))
    check("`ranks` at the giver: the class's trainer answers a holder", cy.command("ranks"), "teaches")
    out = cy.command("xp 100000")
    check("a skill given at a level: told when the level is reached", out, "You have learned Sit: hdsit")
    check("`skills` lists it now", cy.command("skills"), "Sit")
    check("and `help hdsit` is its own text", cy.command("help hdsit"), "Sit: the fixture's one skill, at level 7.")
    cy.command("tp " + T + "arena")
    # the same without the class: a stranger
    st = mage("hdst", 6)
    inner(st)
    check("`ranks` from the giver for a stranger is not a command", plain(st.command("ranks")), plain(st.command("flarb")))
    check("`help hdfix` for a stranger is the ordinary page", plain(st.command("help hdfix")), plain(st.command("help flarb")))
    check("`help sitter of the test` too", plain(st.command("help sitter of the test")), plain(st.command("help flarb")))
    # a late taker gets the skills at once
    late = mage("hdlate", 8)
    w.command("hidden grant hdlate hdfix")
    at_gate(late); late.command("knock"); a.drain(late, 1.8)
    late.command("finish")
    late.command("pledge")
    check("taken at level 8: every skill up to the level at once", late.command("pledge"), "You are now a Sitter of the Test.", "You have learned Sit: hdsit")

    # --- decline
    de = mage("hdde", 5)
    w.command("hidden grant hdde hdfix")
    at_gate(de); de.command("knock"); a.drain(de, 1.8)
    de.command("finish")
    check("the first decline warns", de.command("decline"), "Say decline again within the minute")
    out = de.command("decline")
    check("the second closes it for good", out, "The Chancery does not summon twice.")
    check("a certificate is given", de.command("i"), "a test certificate")
    check("the paper is kept", de.command("i"), absent=("summons",))
    check("the quest is completed with its own reward", de.command("quests completed"), "1 gold and a certificate")
    check("the register: declined", w.command("hidden hdfix"), "hdde: looted 0, issued yes #", "stage 9, outcome 2")
    check("pledge now is not a command", plain(de.command("pledge")), unknown)
    de.command("out")
    a.drain(de, 0.5)
    for v in DOOR_VERBS[:5]:
        check(f"a decliner at the door: `{v}` is an unknown command for ever", plain(de.command(v)), unknown)
    check("... the exits line has no `in`", de.command("look"), absent=("in,", ", in"))
    check("... and cannot be summoned again", w.command("hidden grant hdde hdfix"), "They have been summoned already.")
    de = relog(de, "hdde", "quit")
    at_gate(de)
    check("after a login: still closed", plain(de.command("knock")), unknown)

    # --- neither
    ne = mage("hdne", 5)
    w.command("hidden grant hdne hdfix")
    at_gate(ne); ne.command("knock"); a.drain(ne, 1.8)
    ne.command("finish")
    ne.command("out")
    a.drain(ne, 0.3)
    ne.command("knock")
    out = a.drain(ne, 1.8) or ""
    check("walking away answers nothing: the offer is still open", w.command("hidden hdfix"), "hdne: looted 0, issued yes #", "stage 5, outcome 0")
    check("... and the giver repeats it on return", ne.command("quest duty"), "Will you take the seat")

    # --- withdrawn: an ordinary class taken while the summons was outstanding
    wd = mage("hdwd", 5)
    w.command("hidden grant hdwd hdfix")
    check("an ordinary class is taken", wd.command("hplain"), "PLAIN ok")
    at_gate(wd)
    wd.command("knock")
    out = a.drain(wd, 1.8) or ""
    check("the giver releases them, in her own words", wd.command("quests completed"), "Test Duty", "1 gold")
    check("the register: withdrawn", w.command("hidden hdfix"), "hdwd: looted 0, issued yes #", "stage 9, outcome 3")
    check("the paper is kept", wd.command("i"), absent=("summons",))
    wd.command("out")
    check("and the door is closed to them", plain(wd.command("knock")), unknown)
    check("a class that is not the hidden one is held", wd.command("hclass"), "hdplain")

    # --- below level 5
    lo = mage("hdlo", 3)
    w.command("hidden grant hdlo hdfix")
    at_gate(lo); lo.command("knock"); a.drain(lo, 1.8)
    lo.command("finish"); lo.command("pledge")
    out = lo.command("pledge")
    check("below level 5 the class refuses (and the offer stays)", out, "You must be level 5.")
    check("... nothing was taken", lo.command("hclass"), absent=("hdfix",))
    check("a wizard cannot be summoned", w.command("hidden grant hdwiz hdfix"), "A wizard cannot be summoned.")
    pd = mage("hdpd", 5)
    w.command("hidden grant hdpd hdfix")
    return cy


def step_leaks(w, cy):
    print("\n=== 5. leaks")
    st = mage("hdsta", 6)
    check("the class lists omit it, the ordinary one is listed", st.command("hlist"), "children=hdplain", "advanced=hdplain", absent=("hdfix",))
    # the Hall of Legends: ask the hallmasters about the class's words
    st.command("tp /domains/greenvale/oakenfell/guild/hall_of_callings")
    a.drain(st, 0.4)
    unknown = plain(st.command("ask hallmaster about flarb"))
    for word in ("hdfix", "sitter", "seat", "test seat", "registrar", "summons", "duty"):
        check(f"a hallmaster asked about '{word}' answers as for any unknown word", plain(st.command(f"ask hallmaster about {word}")), unknown)
    check("the Hall's own banner and talk do not name it", st.command("look") + st.command("talk hallmaster"), absent=("hdfix", "Sitter", "Test Seat"))
    st.command("tp " + T + "arena")
    # other players see no class
    cy.command("tp " + T + "arena")
    a.drain(st, 0.3)
    who = st.command("who")
    look = st.command("look at hdcy")
    check("`who` and `look at` show nothing of a hidden class", who + look, absent=("Sitter", "Test Seat", "hdfix"))
    check("the holder's own `score` does", cy.command("score"), "Sitter of the Test")
    # a wizard's code
    # the wizard command
    check("below level 10 `hidden` is not a command", plain(st.command("hidden")), plain(st.command("flarb")))
    check("the level 10 `hidden` lists the class with counts", w.command("hidden"), "hdfix: summoned", "taken", "declined", "withdrawn")
    check("... and the people", w.command("hidden hdfix"), "hdcy:")
    check("its usage", w.command("hidden grant hdwiz"), "Usage:")


WIZ_DEN = "// @inherit /std/room\npublic class HdDen : Room { public override void Create() { ShortDesc = \"Den\"; LongDesc = \"A workshop.\"; } }\n"

WIZ_MEDDLER2 = WIZ_MEDDLER.replace('        var classes = LoadObject("/secure/classes");', """        foreach (var u in Users())
        {
            if (u == me) continue;
            try { Write("TARGET " + u.Call<string>("QueryName") + " ClassIds=" + string.Join(",", u.Call<List<string>>("QueryClassIds") ?? new List<string>()) + " Tier=" + u.Call<string>("QueryClassOfTier", "advanced") + " Has=" + u.Call<bool>("QueryHasClass", "hdfix") + " Title=" + u.Call<string>("QueryClassTitle") + "\\n"); }
            catch (Exception) { Write("TARGET err\\n"); }
        }
        var classes = LoadObject("/secure/classes");""")


def step_wizard(w, mudlib):
    print("\n=== 8. a wizard's objects")
    os.makedirs(os.path.join(mudlib, "wiz", "hdwiz"), exist_ok=True)
    h.write(mudlib, "/wiz/hdwiz/meddler", WIZ_MEDDLER2)
    h.write(mudlib, "/wiz/hdwiz/den", WIZ_DEN)
    w.command("goto /wiz/hdwiz/den")
    a.drain(w, 0.4)
    check("clone the meddler in the workshop", w.command("clone ~/meddler"), "meddler")
    out = w.command("hmeddle")
    check("the meddler ran", out, "MEDDLE ")
    for fn in ("RollOnLoot", "Admits", "GateLine", "GateStage", "Reissue", "SerialOf", "Advance", "Note", "NoteOf", "Answer", "StageFor", "OutcomeFor", "OfferIsOpen",
               "SetTestRolls", "DiceMode", "Report", "Grant", "Reset", "SetLooted", "Simulate", "Refusals", "HiddenIds"):
        m = re.search(rf"MEDDLE {fn}=(\S*)", out)
        truth(f"{fn} is refused to a wizard's object (nothing, false or an error; never a value)", bool(m) and m.group(1) in ("null", "err", "False", "0", ""), m.group(0) if m else out[-200:])
    for m in re.finditer(r"TARGET (\w+) ClassIds=(\S*) Tier=(\S*) Has=(\w+) Title=(.*)", out):
        truth(f"a real holder ({m.group(1)}) shows no hidden class to the wizard's code", "hdfix" not in m.group(2) and "hdfix" not in m.group(3) and m.group(4) == "False" and "Sitter" not in m.group(5), m.group(0))
    truth("the wizard's code could list some real characters (else the probe proved less)", "TARGET " in out, out[-300:])
    check("a creature cloned in the workshop does not roll: its corpse is not summonable", w.command("stat here"), "Den")
    w.command("goto " + T + "arena")
    a.drain(w, 0.4)


def step_restart(work, mudlib, log, box):
    print("\n=== 9. a restart of the driver")
    g.stop_driver(box[0])
    box[0] = g.start_driver(work, mudlib, log)
    cy = login("hdcy")
    check("a holder is still a holder", cy.command("tp " + T + "arena") + cy.command("hclass"), "hdfix")
    pd = login("hdpd")
    check("a summoned character still has the paper", pd.command("i"), "a summons")
    de = login("hdde")
    at_gate(de)
    check("a decliner's door is still closed", plain(de.command("knock")), plain(de.command("flarb")))
    ne = login("hdne")
    at_gate(ne)
    ne.command("knock")
    a.drain(ne, 1.8)
    check("the open offer survived", ne.command("quest duty"), "Will you take the seat")
    pd.command("tp " + T + "gate")
    a.drain(pd, 0.3)
    check("and the unadmitted still pass the door", pd.command("knock"), "The Test Waiting Room")


def step_files(work):
    print("\n=== 6. nothing in the history or the website's copies")
    sec = os.path.join(work, "save", "secure")
    seen = {}
    for fn in sorted(os.listdir(sec)) if os.path.isdir(sec) else []:
        text = open(os.path.join(sec, fn), errors="replace").read()
        seen[fn] = text
    for fn, text in seen.items():
        if fn == "hidden.json":
            continue
        truth(f"save/secure/{fn} never names the hidden class", "hdfix" not in text and "Sitter" not in text and "Test Seat" not in text, fn)
    truth("the register's own file holds the records", "hdfix" in seen.get("hidden.json", ""), list(seen))
    ch = json.loads(seen.get("chronicle.json", "{}") or "{}")
    truth("the Chronicle has no entry about it", not any("hdfix" in json.dumps(e) or "seat" in json.dumps(e).lower() for e in (ch.get("entries", []) if isinstance(ch, dict) else [])), "")
    rum = seen.get("rumors.json", "")
    truth("no rumour", "hdfix" not in rum and "Sitter" not in rum, rum[:100])


def step_lint():
    print("\n=== 7. the lint: check 20")
    import subprocess
    fixture = os.path.join(h.ROOT, "tests", "fixtures", "hidden_leak", "mudlib")
    out = subprocess.run([sys.executable, os.path.join(h.ROOT, "tests", "lint_area.py"), "--mudlib", fixture], capture_output=True, text=True).stdout
    for what, needle in (
        ("the gate key naming the class", "the gate key"),
        ("class.hidden missing from the filter", "class.hidden is missing"),
        ("the id in publicClasses", "is in publicClasses"),
        ("a string with the title", "'Keeper of Leaks'"),
        ("a string with the id", "'leaky'"),
        ("a string with a skill name", "'Seep Away'"),
        ("a string with the quest id", "'leak hunt'"),
        ("a public page of the web", "/web/page.html: public text shows"),
        ("a public document", "/docs/GUIDE.md: public text shows"),
        ("the filter file naming the class", "publicfilter.json: public text shows"),
        ("a hidden class's file reporting class.advanced", "reports class.advanced"),
    ):
        truth(f"the leaky fixture is caught: {what}", needle in out, out[-400:])
    truth("the own files may speak of the class", "/domains/leak/own/room: a string shows" not in out, "")
    real = subprocess.run([sys.executable, os.path.join(h.ROOT, "tests", "lint_area.py")], capture_output=True, text=True).stdout
    truth("the real mudlib has no check 20 error", " 20" not in real and re.search(r"(\d+) error\(s\)", real).group(1) == "0", real[-300:])


def run(work, mudlib, log, box):
    only = [x for x in os.environ.get("HD_STEPS", "").split(",") if x]
    want = lambda n: not only or n in only
    if want("lint"): step_lint()
    w = wizard()
    w.command("tp " + T + "arena")
    if want("roll"): step_roll(w)
    if want("deliver"): step_deliver(w)
    if want("door"): step_door(w)
    cy = None
    if want("quest"): cy = step_quest(w)
    if want("leaks") and cy is not None: step_leaks(w, cy)
    if want("wizard"): step_wizard(w, mudlib)
    time.sleep(1.0)
    if want("files"): step_files(work)
    if want("restart"): step_restart(work, mudlib, log, box)


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-hd-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    prepare(mudlib)
    logpath = os.path.join(work, "driver.log")
    log = open(logpath, "w")
    driver = g.start_driver(work, mudlib, log)
    box = [driver]
    try:
        run(work, mudlib, log, box)
    finally:
        g.stop_driver(box[0])
        log.close()
    print()
    print("driver log tail:\n" + open(logpath).read()[-3000:])
    if h.failures:
        print(f"{len(h.failures)} FAILED:", *h.failures, sep="\n  ")
        sys.exit(1)
    print("all checks passed")
    shutil.rmtree(work, ignore_errors=True)


if __name__ == "__main__":
    main()
