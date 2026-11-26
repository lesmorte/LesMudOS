#!/usr/bin/env python3
"""Tests for search and hidden things (docs/specs/search.md): the check (formula, table of the spec, bonus, Keen Eye), time and the wait,
interruption, sneaking and resting, hidden exits (the old HiddenExit percents, HideExit, a secret door, landmarks and the Chronicle), hidden finds,
once-per-reset items, hidden nodes and clues, the sandbox, and the real content (Hidden Cache, Tavern Cellar, Storage Chamber, Smuggler Hall,
Mushroom Cavern). Real characters over TCP, the dice fixed through /secure/search's test hook.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=5061 python3 tests/search_test.py        (SEARCH_STEPS=check,time to run some steps)
"""
import json
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4841")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402
import sneak_test as sn  # noqa: E402

T = "/domains/searchtest/"
check = h.check
HOLLOW = "/domains/hollow/"
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

PROBE = r'''// @inherit /std/thing
public class SrProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        foreach (var v in new[] { "qrolls", "qwait", "qinfo", "qchance", "qcheck", "qgather", "qtele", "qlevel", "qspawn", "qreset", "qforce", "qdirect", "qfound", "qgold", "qimprove", "qroom", "qengage", "qclear", "qsneakrolls" })
            AddAction(v, Do);
    }
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var room = Environment(me)!;
        var daemon = LoadObject("/secure/search");
        switch (QueryVerb())
        {
            case "qrolls": Write("ROLLS " + daemon.Call<bool>("SetTestRolls", w[0]) + " " + daemon.Call<string>("QueryTestRolls") + "\n"); return true;
            case "qwait": Write("WAIT " + daemon.Call<bool>("SetTestWait", int.Parse(w[0])) + " " + daemon.Call<int>("QueryWaitSeconds") + "\n"); return true;
            case "qinfo":
                Write($"INFO bonus {me.Call<int>("QuerySearchBonus")} insight {me.Call<int>("QueryAttribute", "insight")} left {me.Call<int>("QuerySearchWaitLeft", room.Name)} resting {me.Call<bool>("QueryResting")} sneaking {me.Call<bool>("QuerySneaking")} fight {me.Call<bool>("QueryInCombat")} gold {me.Call<int>("QueryGold")}\n");
                return true;
            case "qchance": Write("CHANCE " + daemon.Call<int>("Chance", int.Parse(w[0]), int.Parse(w[1])) + "\n"); return true;
            case "qcheck": Write("CHECK " + daemon.Call<bool>("Check", int.Parse(w[0]), int.Parse(w[1]), int.Parse(w[2])) + "\n"); return true;
            case "qgather":
            {
                var node = AllInventory(room).FirstOrDefault(o => o.IsAlive && o.Call<bool>("IsNode"));
                Write("GATHER " + (node == null ? "nonode" : node.Call<bool>("TryGather", me).ToString()) + "\n"); return true;
            }
            case "qtele": MoveObject(me, LoadObject(args.Trim())); Write("TELE ok\n"); return true;
            case "qlevel":
            {
                var want = int.Parse(w[0]);
                while (me.Call<int>("QueryLevel") < want) me.Call("AddXp", me.Call<int>("QueryXpForLevel", me.Call<int>("QueryLevel") + 1) - me.Call<int>("QueryXp"));
                Write("LEVEL " + me.Call<int>("QueryLevel") + "\n"); return true;
            }
            case "qspawn": MoveObject(CloneObject("/domains/searchtest/" + w[0]), room); Write("SPAWNED\n"); return true;
            case "qreset": room.Call("Reset"); Write("RESET called\n"); return true;
            case "qforce": Write("FORCE " + me.Call<bool>("RecordFound", w[0]) + "\n"); return true;
            case "qdirect": Write("DIRECT " + room.Call<int>("Search", me, "") + "\n"); return true;
            case "qfound": Write("FOUND " + me.Call<bool>("QueryFound", room.Name, w[0]) + "\n"); return true;
            case "qgold": me.Call("AddGold", int.Parse(w[0])); Write("GOLD " + me.Call<int>("QueryGold") + "\n"); return true;
            case "qroom": Write("ROOM " + room.Name + "\n"); return true;
            case "qengage":
            {
                var m = AllInventory(room).FirstOrDefault(o => o.IsAlive && o.Call<bool>("IsMonster"));
                if (m == null) { Write("ENGAGE nomonster\n"); return true; }
                m.Call("Engage"); Write("ENGAGE called\n"); return true;
            }
            case "qclear": { foreach (var o in AllInventory(room).ToList()) if (o != me && o.IsAlive && o.Call<bool>("IsMonster")) o.Call("Die", (ObjectRef?)null); Write("CLEARED\n"); return true; }
            case "qsneakrolls": Write("SNEAKROLLS " + LoadObject("/secure/sneak").Call<bool>("SetTestRolls", w[0]) + "\n"); return true;
        }
        return false;
    }
}
'''

ROOM = """// @inherit /std/room
public class {cls} : Room
{{
    public override void Create()
    {{
        ShortDesc = "{short}";
        LongDesc = "{long}";
        ResetSeconds = {reset};
{exits}{body}    }}
{extra}}}
"""


def room(cls, short, exits, body="", extra="", reset=3000, long="A room made for the search test. A crack runs along one wall, and some sacks lie in a corner."):
    ex = "".join(f'        Exits["{d}"] = "{p}";\n' for d, p in exits.items())
    return ROOM.format(cls=cls, short=short, exits=ex, body=body, extra=extra, reset=reset, long=long)


HALL_INIT = """    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who != null && Interactive(who) && Present("probe", who) == null) MoveObject(CloneObject("/domains/searchtest/probe"), who);
    }
"""

DETAILS = '        AddDetail("crack,fissure", "A crack in the wall.");\n        AddDetail("sacks,sack", "Grey sacks.");\n        AddDetail("stone,flagstone", "A flat stone.");\n'

HFIND = """// @inherit /std/find
public class SrHFind : Find
{
    public override void Create()
    {
        Name = "Test Purse"; Ids.Add("purse"); ShortDesc = "a test purse"; LongDesc = "A purse made for the search test.";
        SearchDifficulty = 12; SearchNames = "stone,flagstone"; FoundLine = "You lever up the stone and find a purse.";
        Gold = 5; FallOff = 50; ByFeel = false;
    }
}
"""
HFIND_FEEL = (HFIND.replace("SrHFind", "SrHFindFeel").replace("ByFeel = false", "ByFeel = true").replace("Test Purse", "Feel Pouch").replace('Ids.Add("purse")', 'Ids.Add("pouch")')
              .replace("a test purse", "a test pouch").replace("You lever up the stone and find a purse.", "You feel under the stone and find a pouch."))
VFIND = HFIND.replace("SrHFind", "SrVFind").replace("SearchDifficulty = 12", "SearchDifficulty = 0").replace("Test Purse", "Visible Purse").replace('Ids.Add("purse")', 'Ids.Add("wallet")').replace("a test purse", "a visible wallet")
JAR = """// @inherit /std/item
public class SrJar : Item
{
    public override void Create() { Name = "Jar"; Ids.Add("jar"); ShortDesc = "a test jar"; LongDesc = "A jar made for the search test."; }
}
"""
HERBS = """// @inherit /std/node
public class SrHerbs : Node
{
    public override void Create()
    {
        Name = "Test Herbs"; ShortDesc = "a crowd of test herbs"; Kind = "herb patch"; Charges = 2; RegrowSeconds = 900; Difficulty = 3;
        FullDesc = "Pale herbs in a crack."; PickedDesc = "A bare crack.";
    }
}
"""
LURKER = sn.creature("SrLurker", "Lurker", "lurker", level=1, aggressive=True, reaction=6.0)

MEDDLER = r'''// @inherit /std/thing
public class SrMeddler : Thing
{
    public override void Create() { Name = "srmeddler"; Ids.Add("srmeddler"); }
    public override string? Short() => "a srmeddler";
    public override void Init() { AddAction("smeddle", Meddle); }
    private bool Meddle(string args)
    {
        var who = Users().First(u => u.Call<string>("QueryName")!.ToLowerInvariant() == args.Trim());
        var tries = new List<string>();
        void Try(string label, Func<object?> f)
        {
            try { tries.Add(label + "=" + (f() ?? "null")); }
            catch (MudRuntimeException e) { tries.Add(label + "!" + e.Message.Length); }
        }
        var daemon = LoadObject("/secure/search");
        var room = Environment(who)!;
        Try("RecordFound", () => who.Call<bool>("RecordFound", "#honey"));
        Try("RecordFoundExit", () => who.Call<bool>("RecordFoundExit", "up"));
        Try("Rolls", () => daemon.Call<bool>("SetTestRolls", "d20:20"));
        Try("Wait", () => daemon.Call<bool>("SetTestWait", 0));
        Try("RollsNow", () => daemon.Call<string>("QueryTestRolls"));
        Try("Reset", () => { room.Call("Reset"); return "called"; });
        Try("Search", () => room.Call<int>("Search", who, ""));
        Try("Chance", () => daemon.Call<int>("Chance", 12, 0));
        Try("WaitNow", () => daemon.Call<int>("QueryWaitSeconds"));
        Write("MEDDLE " + string.Join(" ", tries) + "\n");
        return true;
    }
}
'''


WIZ_ROOM = """// @inherit /std/room
public class SrWizRoom : Room
{
    public override void Create() { ShortDesc = "Wizard Workshop"; LongDesc = "A wizard's room."; Exits["out"] = "/domains/searchtest/hall"; }
}
"""


def test_files():
    f = {T + "probe": PROBE, T + "hpurse": HFIND, T + "hpouch": HFIND_FEEL, T + "vwallet": VFIND, T + "jar": JAR, T + "herbs": HERBS, T + "lurker": LURKER}
    exits = {"up": HOLLOW + "village/square"}
    names = ("plain", "other", "exits", "named", "mod", "hfind", "items", "reset", "nodes", "clue", "door", "landmark", "conv10", "conv15", "conv20", "conv50", "conv0", "lurk", "bare", "verb")
    for n in names:
        exits[n] = T + n
    f[T + "hall"] = room("SrHall", "Search Hall", exits, "", HALL_INIT)
    back = {"out": T + "hall"}
    f[T + "plain"] = room("SrPlain", "Plain Room", dict(back, other=T + "other"), DETAILS)
    f[T + "other"] = room("SrOther", "Other Room", dict(back, plain=T + "plain"), DETAILS)
    f[T + "vault"] = room("SrVault", "Vault", {"east": T + "exits"})
    f[T + "exits"] = room("SrExits", "Exits Room", dict(back),
                          DETAILS + '        HideExit("west", "/domains/searchtest/vault", "", SearchModerate, "You find a seam. A way leads {cmd}west{/}.");\n'
                          '        HideExit("down", "/domains/searchtest/vault", "fissure", SearchHard, "A gap opens: {cmd}down{/}.");\n')
    f[T + "named"] = room("SrNamed", "Named Room", dict(back), DETAILS + '        HideExit("west", "/domains/searchtest/vault", "crack", SearchModerate, "You find a way {cmd}west{/}.");\n')
    f[T + "mod"] = room("SrMod", "Dim Room", dict(back), DETAILS +
                        '        HideExit("west", "/domains/searchtest/vault", "crack", SearchModerate, "You find a way {cmd}west{/}.");\n'
                        '        HideItem("jar", "/domains/searchtest/jar", SearchModerate, "sacks", "You find a jar.", byFeel: true);\n',
                        extra="    protected override int SearchModifier(ObjectRef searcher, bool byFeel) => byFeel ? 0 : -5;\n")
    f[T + "hfind"] = room("SrHFindRoom", "Hidden Find Room", dict(back), DETAILS + '        AddSpawn("/domains/searchtest/hpurse");\n        AddSpawn("/domains/searchtest/vwallet");\n        AddSpawn("/domains/searchtest/hpouch");\n',
                          extra="    protected override int SearchModifier(ObjectRef searcher, bool byFeel) => byFeel ? 0 : -4;\n")
    f[T + "items"] = room("SrItems", "Items Room", dict(back), DETAILS + '        HideItem("jar", "/domains/searchtest/jar", SearchEasy, "sacks,sack", "Under the sacks you find a jar.");\n')
    f[T + "reset"] = room("SrReset", "Reset Room", dict(back), DETAILS + '        HideItem("jar", "/domains/searchtest/jar", SearchEasy, "sacks,sack", "Under the sacks you find a jar.");\n', reset=12)
    f[T + "nodes"] = room("SrNodes", "Nodes Room", dict(back), DETAILS + '        HideNode("herbs", "/domains/searchtest/herbs", SearchModerate, "crack,fissure", "In the crack you find pale herbs.", byFeel: true);\n')
    f[T + "clue"] = room("SrClue", "Clue Room", dict(back), DETAILS + '        HideClue("slip", SearchModerate, "crack", "In the crack is a slip of paper.", "slip,paper", "The slip reads: the long things go on top.");\n')
    f[T + "door"] = room("SrDoor", "Door Room", dict(back, east=T + "doorfar"), DETAILS.replace("A crack", "A hairline crack") +
                         '        Exits.Remove("east");\n        HideExit("east", "/domains/searchtest/doorfar", "", SearchEasy, "You find a secret door {cmd}east{/}.");\n        AddDoor("east", "door");\n')
    f[T + "doorfar"] = room("SrDoorFar", "Beyond the Door", {"west": T + "door"}, '        AddDoor("west", "door");\n')
    f[T + "landmark"] = room("SrLandmark", "Landmark Room", dict(back), DETAILS + '        HideExit("north", "/domains/searchtest/vault", "", SearchEasy, "You find a way {cmd}north{/}.", landmark: true);\n')
    for percent in (10, 15, 20, 50, 0):
        f[T + f"conv{percent}"] = room(f"SrConv{percent}", f"Conversion {percent}", dict(back), DETAILS +
                                       f'        HiddenExit("north", "/domains/searchtest/vault", "fissure", {percent}, "You find north.");\n')
    f[T + "lurk"] = room("SrLurk", "Lurker Room", dict(back), DETAILS)
    f[T + "bare"] = room("SrBare", "Bare Room", dict(back), "")
    f[T + "verb"] = room("SrVerb", "Verb Room", dict(back), DETAILS + '        AddSpawn("/domains/searchtest/vwallet");\n        AddSpawn("/domains/searchtest/hpurse");\n')
    return f


# ---------------------------------------------------------------- helpers

CALLING = {}


def login(name, calling="scout"):
    c = h.Client(name, archetype=calling)
    c.read_until("> ")
    a.drain(c, 0.4)
    c.command("down")
    CALLING[name.lower()] = calling
    c.calling = calling
    return c


def to(c, where):
    c.command("qtele " + (where if where.startswith("/") else T + where))
    a.drain(c, 0.3)


def srch(c, line="search", wait=None):
    """A search and what follows it: the result comes after 3 seconds, and a Warrior (Reaction Time 3.0) may act again after 5."""
    c.send(line)
    return a.quiet(c, wait if wait is not None else (4.4 if getattr(c, "calling", "scout") == "scout" else 5.6))


def info(c):
    raw = c.command("qinfo")
    m = re.search(r"INFO bonus (-?\d+) insight (\d+) left (\d+) resting (\w+) sneaking (\w+) fight (\w+) gold (\d+)", raw)
    if not m:
        print("qinfo gave:", repr(raw))
        return {"bonus": 0, "insight": 0, "left": -1, "resting": None, "sneaking": None, "fight": None, "gold": -1}
    return {"bonus": int(m.group(1)), "insight": int(m.group(2)), "left": int(m.group(3)), "resting": m.group(4) == "True", "sneaking": m.group(5) == "True",
            "fight": m.group(6) == "True", "gold": int(m.group(7))}


def dice(chief, roll):
    check(f"dice fixed at d20:{roll}", chief.command(f"qrolls d20:{roll}"), "ROLLS True")


def wait_seconds(chief, n):
    check(f"wait set to {n}", chief.command(f"qwait {n}"), "WAIT True")


def expected_chance(d, b):
    return max(5, min(95, 5 * (21 - max(2, d - b))))


def expected_check(roll, d, b):
    return roll >= 20 or (roll > 1 and roll + b >= d)


def need(c, d, named=False, extra=0):
    """The lowest roll that finds a thing of difficulty d for this character (their bonus, +3 for naming it, + a room's modifier)."""
    return max(2, min(20, d - info(c)["bonus"] - (3 if named else 0) - extra))


def exits_line(c):
    m = re.search(r"Obvious exits: ([^\n]*)", c.command("look"))
    return m.group(1) if m else ""


# ---------------------------------------------------------------- 1. the check

def spec_table():
    """The table of docs/specs/search.md section 2 as {(insight, calling): (bonus, [easy, moderate, hard, very hard])}."""
    text = open(os.path.join(ROOT, "docs", "specs", "search.md")).read()
    rows = {}
    for m in re.finditer(r"^\| (\d+) \| (not a Scout|Scout) \| \+?(\d+) \| (\d+)% \| (\d+)% \| (\d+)% \| (\d+)% \|$", text, re.M):
        rows[(int(m.group(1)), m.group(2))] = (int(m.group(3)), [int(m.group(i)) for i in (4, 5, 6, 7)])
    return rows


def step_check(chief):
    bad = []
    for d in (6, 7, 12, 17, 22, 25):
        for b in (-5, -2, 0, 3, 7, 9, 14, 25):
            if f"CHANCE {expected_chance(d, b)}" not in chief.command(f"qchance {d} {b}"):
                bad.append((d, b))
    check("the chance function matches the formula at every difficulty and bonus tried", "ok" if not bad else repr(bad[:5]), "ok")
    bad = []
    for d, b in ((7, 0), (12, 0), (17, 0), (22, 0), (12, 3), (17, 5), (7, 20), (25, -3)):
        wins = 0
        for roll in range(1, 21):
            got = chief.command(f"qcheck {roll} {d} {b}")
            want = expected_check(roll, d, b)
            wins += want
            if f"CHECK {want}" not in got:
                bad.append((roll, d, b, got))
        if 5 * wins != expected_chance(d, b) and not (wins * 5 < 5 or wins == 1 and expected_chance(d, b) == 5):
            bad.append(("count", d, b, wins))
    check("the roll that finds is exactly what the formula says (1 never, 20 always) and the count of rolls is the chance", "ok" if not bad else repr(bad[:3]), "ok")
    check("a natural 1 never finds, whatever the bonus", chief.command("qcheck 1 7 30"), "CHECK False")
    check("a natural 20 always finds, whatever the difficulty", chief.command("qcheck 20 25 -5"), "CHECK True")
    rows = spec_table()
    check("the spec's table has its six rows", str(len(rows)), "6")
    off = []
    for (insight, calling), (bonus, percents) in rows.items():
        expect_bonus = (insight - 5) + (2 if calling == "Scout" else 0)
        if bonus != expect_bonus:
            off.append(("bonus", insight, calling, bonus, expect_bonus))
        for d, p in zip((7, 12, 17, 22), percents):
            if p != expected_chance(d, bonus):
                off.append((insight, calling, d, p, expected_chance(d, bonus)))
    check("every number of the spec's table is the function's number", "ok" if not off else repr(off[:3]), "ok")
    # HiddenExit's old percents are the same chance for an Insight 5 character.
    check("old 10% is difficulty 19 and gives 10%", chief.command("qchance 19 0"), "CHANCE 10")
    check("old 15% is difficulty 18 and gives 15%", chief.command("qchance 18 0"), "CHANCE 15")
    check("old 20% is difficulty 17 and gives 20%", chief.command("qchance 17 0"), "CHANCE 20")
    check("old 50% is difficulty 11 and gives 50%", chief.command("qchance 11 0"), "CHANCE 50")
    check("the wait is 60 seconds", chief.command("qwait -1"), "WAIT True 60")
    war = login("srwar", "warrior")
    sc = login("srscout", "scout")
    check("a Warrior (Insight 5) has bonus 0", str(info(war)["bonus"]) + "/" + str(info(war)["insight"]), "0/5")
    check("a Scout (Insight 6, Keen Eye +2) has bonus 3", str(info(sc)["bonus"]) + "/" + str(info(sc)["insight"]), "3/6")
    check("the skills list shows Keen Eye for a Scout", sc.command("skills"), "Keen Eye")
    check("and not for a Warrior", war.command("skills"), absent=("Keen Eye",))
    war.command("qlevel 7")
    check("improve insight by 3", war.command("improve insight 3"), "Insight is now 8")
    check("Insight 8 is bonus 3", str(info(war)["bonus"]), "3")
    war.command("improve insight 4")
    check("Insight 12 is bonus 7", str(info(war)["bonus"]), "7")
    war.command("quit")
    sc.command("quit")


# ---------------------------------------------------------------- 2. time, the wait, resting

def step_time(chief):
    wait_seconds(chief, -1)
    dice(chief, 10)
    p = login("srtime")
    to(p, "plain")
    start = time.time()
    p.send("search")
    early = a.quiet(p, 2.4)
    check("the room is told at the start, and no result comes before 3 seconds", early, "Srtime searches the area carefully...", absent=("nothing of note",))
    late = early + a.quiet(p, 1.6)
    took = time.time() - start
    check("the result comes after 3 seconds: nothing of note, and the wait line", late, "You search carefully but find nothing of note.", "You will need a minute")
    check("it took about 3 seconds", "ok" if 2.6 <= took <= 4.6 else f"{took:.1f}", "ok")
    check("trying again at once is refused with the time left", p.command("search"), "You have only just searched here; your eyes need a rest. Try again in about 60 seconds.")
    check("the wait is 60 seconds long (59 or 60 left)", "ok" if info(p)["left"] in (59, 60) else str(info(p)["left"]), "ok")
    to(p, "other")
    check("another room is not under the wait", srch(p), "You search carefully but find nothing of note.")
    to(p, "plain")
    check("leaving and returning does not clear the wait", p.command("search"), "You have only just searched here")
    dice(chief, 5)
    to(p, "exits")
    check("a failed search of a room that hides something says the same as one that hides nothing", srch(p), "You search carefully but find nothing of note.", "You will need a minute")
    p.command("quit")
    p = login("srtime")
    to(p, "plain")
    check("the wait is not saved: a new login searches at once", srch(p), "You search carefully but find nothing of note.")
    time.sleep(53)
    check("late in the wait the time left is told in tens of seconds", p.command("search"), "Try again in about 10 seconds.")
    time.sleep(8)
    check("after the minute the character may search again", srch(p), "You search carefully but find nothing of note.", absent=("only just searched",))
    p.command("quit")
    # Resting: the search gets the character up.
    wait_seconds(chief, 0)
    r = login("srrest")
    to(r, "plain")
    r.command("rest")
    check("the character is resting", str(info(r)["resting"]), "True")
    r.send("search")
    time.sleep(0.5)
    check("a search ends resting", str(info(r)["resting"]), "False")
    a.quiet(r, 3.5)
    # Refused in a fight.
    to(r, "lurk")
    r.command("qspawn lurker")
    a.quiet(r, 7)
    check("a search is refused in a fight", r.command("search"), "You cannot search while you are fighting.")
    r.command("qclear")
    r.command("quit")


# ---------------------------------------------------------------- 3. exits: the check against a room, naming, the hook

def step_exits(chief):
    wait_seconds(chief, 0)
    p = login("srexit")
    w = login("srwatch")
    to(p, "exits")
    to(w, "exits")
    check("a hidden exit is not listed", exits_line(p), "out", absent=("west", "down"))
    check("and the word is nothing", p.command("west"), "What?")
    n = need(p, 12)
    dice(chief, n - 1)
    check(f"roll {n - 1} does not find a Moderate thing for a Scout (bonus 3)", srch(p), "nothing of note", absent=("seam",))
    dice(chief, n)
    a.drain(w, 0.2)
    out = srch(p)
    check(f"roll {n} finds it, and the finder reads the builder's line", out, "You find a seam. A way leads west.")
    check("the others in the room are told only that something was found", a.quiet(w, 0.5), "Srexit finds something.", absent=("seam", "A way leads"))
    check("the finder now sees the exit listed", exits_line(p), "west")
    check("another character does not", exits_line(w), "out", absent=("west",))
    check("the finder walks it", p.command("west"), "Vault")
    p.command("east")
    check("the other character cannot", w.command("west"), "What?")
    n = need(p, 17)
    dice(chief, n - 1)
    check(f"roll {n - 1} does not find a Hard thing", srch(p), "nothing of note", absent=("gap opens",))
    dice(chief, n)
    check(f"roll {n} finds a Hard thing", srch(p), "A gap opens: down.")
    dice(chief, 1)
    check("a natural 1 never finds anything", srch(w), "nothing of note", absent=("seam", "gap"))
    check("looking at the hint detail finds an exit for certain", w.command("look at fissure"), "A gap opens: down.")
    check("a wizard sees every hidden exit marked", chief.command("goto " + T + "exits") + chief.command("look"), "(hidden)")
    check("stat here lists the difficulty", chief.command("stat here"), "Hidden exit west", "difficulty 12", "Hidden exit down", "difficulty 17")
    dice(chief, 20)
    check("a thing found is not found twice", srch(p), "nothing of note")
    check("an object calling the room's Search for the character gets nothing", p.command("qdirect"), "DIRECT 0")
    p.command("quit")
    w.command("quit")


def step_named(chief):
    wait_seconds(chief, 0)
    p = login("srname")
    to(p, "named")
    n = need(p, 12, named=True)
    dice(chief, n)
    check(f"roll {n} does not find a Moderate thing in an unnamed search", srch(p), "nothing of note", absent=("find a way",))
    check("naming it adds 3: the same roll finds it", srch(p, "search crack"), "You find a way west.")
    p2 = login("srnameb")
    to(p2, "named")
    dice(chief, 20)
    check("a thing the search did not name is not rolled, even at roll 20", srch(p2, "search sacks"), "nothing of note", absent=("find a way",))
    check("an article is allowed: 'search the crack'", srch(p2, "search the crack"), "You find a way west.")
    p.command("quit")
    p2.command("quit")


def step_mod(chief):
    """The one hook for a situational modifier (Room.SearchModifier): -5 in the room, 0 for a thing found by feel."""
    wait_seconds(chief, 0)
    p = login("srmod")
    to(p, "mod")
    n = need(p, 12)
    dice(chief, n)
    check(f"the room's modifier (-5) spoils roll {n} for the exit, but a thing found by feel (the jar) is not affected", srch(p), "You find a jar.", absent=("find a way",))
    dice(chief, n + 5)
    check(f"roll {n + 5} with the -5 finds the exit", srch(p), "You find a way west.")
    p.command("quit")


def step_conv(chief):
    """HiddenExit's old percent is a difficulty: 20% is 17, 15% is 18, 10% is 19, 50% is 11, 0 is found by the detail only."""
    wait_seconds(chief, 0)
    for percent, d in ((20, 17), (15, 18), (10, 19), (50, 11)):
        out = chief.command("goto " + T + f"conv{percent}") + chief.command("stat here")
        check(f"{percent}% is difficulty {d}", out, f"Hidden exit north", f"difficulty {d}")
    check("0% is found by the detail only", chief.command("goto " + T + "conv0") + chief.command("stat here"), "by detail only")
    p = login("srconv")
    for percent, d in ((20, 17), (15, 18), (10, 19), (50, 11)):
        to(p, f"conv{percent}")
        n = need(p, d)
        dice(chief, n - 1)
        check(f"a {percent}% exit: roll {n - 1} does not find it", srch(p), "nothing of note", absent=("You find north.",))
        dice(chief, n)
        check(f"a {percent}% exit: roll {n} finds it", srch(p), "You find north.")
    to(p, "conv0")
    dice(chief, 20)
    check("a 0% exit is never found by search, even at roll 20", srch(p), "nothing of note", absent=("You find north.",))
    check("but looking at its detail finds it", p.command("look at fissure"), "You find north.")
    p.command("quit")


# ---------------------------------------------------------------- 4. interruption, sneaking

def step_interrupt(chief):
    wait_seconds(chief, 60)
    dice(chief, 20)
    p = login("srbreak")
    to(p, "lurk")
    p.send("search")
    time.sleep(1.0)
    chief.command("goto " + T + "lurk")
    chief.command("qspawn lurker")
    out = a.quiet(p, 4.5)
    check("a creature that starts a fight breaks the search off", out, "You break off your search as", absent=("find a way", "nothing of note"))
    check("no wait is started by a broken search", str(info(p)["left"]), "0")
    chief.command("qclear")
    a.quiet(p, 1.0)
    p.command("quit")
    wait_seconds(chief, 0)


# ---------------------------------------------------------------- 5. hidden finds

def step_finds(chief):
    wait_seconds(chief, 0)
    p = login("srfind")
    q = login("srfindb")
    to(p, "hfind")
    to(q, "hfind")
    out = p.command("look")
    check("a hidden find is not in the room's list; a visible one is", out, "A visible wallet is here.", absent=("test purse", "test pouch"))
    check("look at it: no such thing", p.command("look at purse"), "You see no 'purse' here.")
    nothing = p.command("get zzzz")
    check("get it: the same answer as for a thing that is not there", p.command("get purse"), nothing.split("\r\n")[0])
    check("take it: the same", p.command("take purse"), p.command("take zzzz").split("\r\n")[0])
    check("get all does not mention it", p.command("get all"), absent=("test purse", "test pouch"))
    check("a wizard sees it, marked", chief.command("goto " + T + "hfind") + chief.command("look"), "test purse (hidden)", "test pouch (hidden)")
    # The room's modifier is -4 for a thing not found by feel: the purse (Moderate 12) needs bonus + roll >= 16; the pouch (by feel) needs 12.
    n_feel = need(p, 12)
    n_purse = need(p, 12, extra=-4)
    dice(chief, n_feel)
    out = srch(p)
    check(f"roll {n_feel}: the pouch (by feel) is found, the purse is not", out, "You feel under the stone and find a pouch.", absent=("lever up the stone",))
    dice(chief, n_purse)
    out = srch(p)
    check(f"roll {n_purse}: the purse is found", out, "You lever up the stone and find a purse.")
    check("now it is in the list", p.command("look"), "A test purse is here.")
    g0 = info(p)["gold"]
    check("get it: gold, once", p.command("get purse"), "You take 5 gold")
    check("gold is paid", str(info(p)["gold"] - g0), "5")
    check("a second try is refused", p.command("get purse"), "already taken")
    p.command("quit")
    p = login("srfind")
    to(p, "hfind")
    check("after logging in again the found state is kept: listed (taken)", p.command("look"), "test purse")
    a.drain(q, 0.3)
    check("a second character does not see it", q.command("look"), absent=("test purse",))
    dice(chief, 20)
    check("and finds it by search", srch(q), "You lever up the stone and find a purse.")
    g1 = info(q)["gold"]
    q.command("get purse")
    check("the second finder got the fall-off (half)", str(info(q)["gold"] - g1), "2")
    # The register counts as found: forget the record in the character file, log in again, and the find is in plain view.
    p.command("quit")
    save = None
    for root_, _, files in os.walk(SAVE_DIR):
        for name in files:
            if name.lower().startswith("srfind.") and "players" in root_.lower():
                save = os.path.join(root_, name)
    check("the character's save file exists", "ok" if save else "none", "ok")
    if save:
        text = open(save).read()
        text = text.replace("\\/", "/")
        keys = re.findall(r'"[^"|]*\|#/domains/searchtest/hpurse"', text)
        check("the record of what they found is in the file", "ok" if keys else "none", "ok")
        for k in keys:
            text = text.replace(k, '"/gone|x"')
        open(save, "w").write(text)
    p = login("srfind")
    to(p, "hfind")
    check("without the record, the register still counts them as having found it: it is listed", p.command("look"), "test purse")
    # The verb: 'search wallet' takes a visible find (the find's own word); plain 'search' searches the room.
    to(p, "verb")
    out = srch(p, "search wallet", 1.0)
    check("search NAME on a visible find takes it (the find's own word)", out, "You take 5 gold")
    r = login("srfindc")
    to(r, "hfind")
    dice(chief, 20)
    out = srch(r, "search purse")
    check("a hidden find is not yet a find: 'search purse' is a search for a spot called purse and finds nothing", out, "nothing of note", absent=("You take", "lever up"))
    r.command("quit")
    q.command("quit")
    p.command("quit")


# ---------------------------------------------------------------- 6. once per reset, nodes, clues

def step_items(chief):
    wait_seconds(chief, 0)
    dice(chief, 20)
    p = login("sritem")
    q = login("sritemb")
    to(p, "items")
    to(q, "items")
    check("a hidden item is not in the room", p.command("look"), absent=("jar",))
    check("the first finder is told, and takes it", srch(p), "Under the sacks you find a jar.")
    check("the jar is in their hands", p.command("i"), "a test jar")
    out = srch(q)
    check("the second finder finds the place empty, and the wait begins", out, "You find where something was kept, but the place is empty. Someone has been before you.", "You will need a minute")
    check("the empty line is not followed by 'nothing of note'", out, absent=("nothing of note",))
    check("the stat says taken", chief.command("goto " + T + "items") + chief.command("stat here"), "taken this period")
    check("a call to Reset from another object cannot put it back", p.command("qreset"), "RESET called")
    check("so the jar is still taken", chief.command("stat here"), "taken this period")
    p.command("quit")
    q.command("quit")
    r = login("srreset")
    to(r, "reset")
    check("a once-per-reset item: the first finder gets it", srch(r), "Under the sacks you find a jar.")
    check("the second search the same period finds the place empty", srch(r), "the place is empty")
    time.sleep(6)
    check("after the room's own reset the same character finds it again", srch(r), "Under the sacks you find a jar.")
    check("and nothing is recorded on the character", r.command("qfound #jar"), "FOUND False")
    r.command("quit")


def step_nodes(chief):
    wait_seconds(chief, 0)
    dice(chief, 20)
    p = login("srnode")
    q = login("srnodeb")
    to(p, "nodes")
    to(q, "nodes")
    check("a hidden node is not in the room's list", p.command("look"), absent=("herbs",))
    check("look at it: nothing", p.command("look at herbs"), "You see no 'herbs' here.")
    check("harvest with no word skips it", p.command("harvest"), "There is nothing here to harvest.")
    check("harvest by name is refused as no such thing", p.command("harvest herbs"), "There is no 'herbs' here to harvest.")
    check("TryGather refuses a character who has not found it", p.command("qgather"), "GATHER False")
    check("a wizard sees it marked", chief.command("goto " + T + "nodes") + chief.command("look"), "A crowd of test herbs (hidden) is here.")
    check("the search finds it", srch(p), "In the crack you find pale herbs.")
    check("now it is listed for the finder", p.command("look"), "A crowd of test herbs is here.")
    check("and not for the other", q.command("look"), absent=("herbs",))
    check("the finder may gather", p.command("qgather"), "GATHER True")
    check("the other still may not", q.command("qgather"), "GATHER False")
    check("harvest now picks it out for the finder", p.command("harvest"), absent=("There is nothing here to harvest.",))
    p.command("quit")
    q.command("quit")


def step_clues(chief):
    wait_seconds(chief, 0)
    dice(chief, 20)
    p = login("srclue")
    q = login("srclueb")
    to(p, "clue")
    to(q, "clue")
    check("read slip before finding it", p.command("read slip"), "You see no 'slip' to read.")
    check("the search finds the clue", srch(p), "In the crack is a slip of paper.")
    check("the finder reads it", p.command("read slip"), "The slip reads: the long things go on top.")
    check("and by the other name", p.command("read paper"), "The slip reads")
    check("another character cannot read it", q.command("read slip"), "You see no 'slip' to read.")
    p.command("quit")
    q.command("quit")


def step_door(chief):
    wait_seconds(chief, 0)
    dice(chief, 20)
    p = login("srdoor")
    to(p, "door")
    check("a secret door is not listed", exits_line(p), absent=("east",))
    check("it is found like any hidden exit", srch(p), "You find a secret door east.")
    check("it is listed with its door", exits_line(p), "east (closed door)")
    check("the door still has to be opened", p.command("east"), "The door to the east is closed.")
    check("open it", p.command("open door"), "You open the door to the east.")
    check("walk through", p.command("east"), "Beyond the Door")
    p.command("quit")


def step_landmark(chief):
    wait_seconds(chief, 0)
    dice(chief, 20)
    p = login("srmark")
    to(p, "landmark")
    check("a landmark exit is found like any", srch(p), "You find a way north.")
    time.sleep(0.6)
    check("finding it earned Renown (2, and 10 for the first on the server)", p.command("renown"), "Renown: 12")
    to(chief, "landmark")
    check("a wizard may find it", srch(chief), "You find a way north.")
    time.sleep(0.4)
    check("and makes no history", chief.command("renown"), absent=("Renown: 2", "Renown: 12"))
    p.command("quit")


# ---------------------------------------------------------------- 7. the sandbox

def step_probe(chief, mudlib):
    wait_seconds(chief, -1)
    dice(chief, 10)
    h.write(mudlib, "/wiz/srchief/srmeddler", MEDDLER)
    h.write(mudlib, "/wiz/srchief/wroom", WIZ_ROOM)
    p = login("srprobe")
    to(p, "items")
    chief.command("goto ~/wroom")
    check("the meddler is cloned", chief.command("clone ~/srmeddler"), "srmeddler")
    out = chief.command("smeddle srprobe")
    check("the meddler ran", out, "MEDDLE ")
    check("a wizard's object cannot put a record on a character", out, absent=("RecordFound=True", "RecordFoundExit=True"))
    check("nor fix the dice", out, absent=("Rolls=True",))
    check("nor shorten the wait", out, absent=("Wait=True",))
    check("the dice are as they were", out, "RollsNow=d20:10")
    check("nor does it search for anyone", out, absent=("Search=1",))
    check("the wait is still 60", out, "WaitNow=60")
    check("nothing was recorded on the character", p.command("qfound #honey"), "FOUND False")
    p.command("quit")


# ---------------------------------------------------------------- 8. the real content

def step_content(chief):
    wait_seconds(chief, 0)
    dice(chief, 16)
    p = login("srcache", "warrior")
    to(p, HOLLOW + "cellars/hidden_cache")
    out = p.command("look")
    check("the Hidden Cache's description names the sacking", out, "Rotted sacking has been thrown over the top of the stacked chests, and it lies in a long, stiff hump.")
    check("the bundle is not in the room", out, absent=("oilcloth", "bundle"))
    check("the sacking detail says search in plain words, and that it may take more than one try", p.command("look at sacking"), "You could search the sacking", "try again")
    check("get bundle: the same answer as for a thing that is not there", p.command("get bundle"), p.command("get zzzz").split("\r\n")[0])
    chief.command("goto " + HOLLOW + "cellars/hidden_cache")
    check("stat names the difficulty (Hard, 17)", chief.command("stat bundle"), "Hidden: difficulty 17")
    check("a wizard sees it marked", chief.command("look"), "oilcloth (hidden)")
    check("roll 16 does not find the Hard bundle (Insight 5)", srch(p), "nothing of note", absent=("oilcloth",))
    dice(chief, 17)
    out = srch(p)
    check("roll 17 finds it, with the room's line", out, "You lift the rotted sacking and find, laid along the top of the stack where the damp could not reach it, a long bundle wrapped in greased oilcloth and tied with green cord.")
    check("now the bundle is in the list", p.command("look"), "long bundle wrapped in oilcloth")
    check("search bundle on a found bundle takes it as get does", p.command("search bundle"), "lift out a warden's recurve")
    check("a second try", p.command("get bundle"), "already unwrapped")
    check("the wrapping is listed for them as folded open", p.command("look"), "A long oilcloth wrapping lies folded open here.")
    p.command("quit")
    p = login("srcache", "warrior")
    to(p, HOLLOW + "cellars/hidden_cache")
    check("after login the record is kept: still listed", p.command("look"), "oilcloth wrapping")
    q = login("srcacheb", "scout")
    to(q, HOLLOW + "cellars/hidden_cache")
    check("a second character sees no bundle", q.command("look"), absent=("oilcloth",))
    dice(chief, 11)
    check("a Scout of Insight 6 naming the sacking: bonus 3+3, roll 11 gives 17", srch(q, "search sacking"), "a long bundle wrapped in greased oilcloth")
    p.command("quit")
    q.command("quit")
    # The Tavern Cellar's purse: Easy 7, 8 gold and then 4, 2, 1.
    chief.command("goto " + HOLLOW + "village/cellar")
    chief.command("qclear")
    t = login("srtavern", "warrior")
    to(t, HOLLOW + "village/cellar")
    check("the Tavern Cellar mentions the flagstone", t.command("look"), "One flat stone by the barrels sits a little crooked in the dirt.", absent=("purse",))
    check("the stone's detail says search outright", t.command("look at stone"), "You could search it.")
    dice(chief, 7)
    check("roll 7 finds an Easy thing at bonus 0", srch(t), "You work your fingers under the edge of the stone")
    before = info(t)["gold"]
    t.command("get purse")
    check("8 gold for the first finder", str(info(t)["gold"] - before), "8")
    check("an empty purse is listed", t.command("look"), "empty rat-chewed purse")
    gains = []
    for i in range(4):
        c = login("srtavern" + "abcd"[i], "warrior")
        to(c, HOLLOW + "village/cellar")
        srch(c)
        g = info(c)["gold"]
        c.command("get purse")
        gains.append(info(c)["gold"] - g)
        c.command("quit")
    check("then 4, 2, 1, and nothing", str(gains), "[4, 2, 1, 0]")
    t.command("quit")
    dice(chief, 20)
    chief.command("goto " + HOLLOW + "cellars/storage_chamber")
    chief.command("qclear")
    s = login("srstore", "scout")
    to(s, HOLLOW + "cellars/storage_chamber")
    check("the Storage Chamber's sacks say search", s.command("look at sacks"), "You could search the sacks.")
    check("its description mentions the heap of sacks", s.command("look"), "A slumped heap of grain sacks fills one corner.")
    check("a search finds the jar of honey", srch(s, "search sacks"), "someone has kept a stoppered jar of honey out of the damp. You take it.")
    check("honey in the hands", s.command("i"), "honey")
    h_ = login("srtally", "scout")
    to(h_, HOLLOW + "cellars/smuggler_hall")
    check("the Smuggler Hall's hooks mention the crack", h_.command("look at hooks"), "Behind the last one a crack runs back into the stone")
    check("nothing to read yet", h_.command("read slip"), "You see no 'slip' to read.")
    check("the tally is found", srch(h_), "Behind the last hook, wedged into a crack and forgotten, is a slip of greasy paper.")
    out = h_.command("read slip")
    check("the tally reads", out, "A smuggler's tally", "Whoever holds the post holds the door.")
    check("the tally names nobody", out, absent=("Philippa", "Reeve", "informer", "Circle of Ash", "Ash"))
    chief.command("goto " + HOLLOW + "cellars/mushroom_cavern")
    chief.command("qclear")
    m = login("srherb", "scout")
    to(m, HOLLOW + "cellars/mushroom_cavern")
    check("the Mushroom Cavern mentions the draught", m.command("look"), "Beyond the farthest cluster the wall is dark and damp")
    check("no herbs listed", m.command("look"), absent=("herbs in a crack",))
    check("the herbs are found by feel", srch(m), "you find a crack at knee height, and in it a small crowd of pale herbs. They smell like healing herb.")
    check("and listed", m.command("look"), "A crowd of pale herbs in a crack is here.")
    for c in (s, h_, m):
        c.command("quit")


# ---------------------------------------------------------------- main

SAVE_DIR = ""


def run(work, mudlib):
    chief = h.Client("srchief", archetype="adventurer")
    chief.read_until("> ")
    check("the secret makes the first wizard", chief.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(chief, 0.4)
    chief.command("down")
    only = [x for x in os.environ.get("SEARCH_STEPS", "").split(",") if x]
    steps = (("check", lambda: step_check(chief)), ("time", lambda: step_time(chief)), ("exits", lambda: step_exits(chief)), ("named", lambda: step_named(chief)),
             ("mod", lambda: step_mod(chief)), ("conv", lambda: step_conv(chief)), ("interrupt", lambda: step_interrupt(chief)), ("finds", lambda: step_finds(chief)),
             ("items", lambda: step_items(chief)), ("nodes", lambda: step_nodes(chief)), ("clues", lambda: step_clues(chief)), ("door", lambda: step_door(chief)),
             ("landmark", lambda: step_landmark(chief)), ("probe", lambda: step_probe(chief, mudlib)), ("content", lambda: step_content(chief)))
    for name, step in steps:
        if not only or name in only:
            print(f"\n=== {name}")
            step()
    return chief


def main():
    global SAVE_DIR
    work = tempfile.mkdtemp(prefix="lesmudos-search-")
    mudlib = os.path.join(work, "mudlib")
    SAVE_DIR = os.path.join(work, "save")
    h.copy_mudlib(mudlib)
    os.makedirs(os.path.join(mudlib, "domains", "searchtest"), exist_ok=True)
    for path, source in test_files().items():
        h.write(mudlib, path, source)
    h.edit(mudlib, "/domains/hollow/village/square", 'Exits["south"]', f'Exits["down"] = "{T}hall";\n        Exits["south"]')
    proc, log = a.start_driver(work, mudlib)
    started = time.time()
    try:
        chief = run(work, mudlib)
        chief.command("shutdown")
        time.sleep(1.5)
        log.flush()
        logged = open(os.path.join(work, "driver.log")).read()
        check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:800], "ok")
    finally:
        proc.kill()
        log.close()
        if h.failures:
            print("\n--- driver log ---")
            text = open(os.path.join(work, "driver.log")).read()
            print("\n".join(l for l in text.splitlines() if "compiled" not in l)[-4000:])
        if os.environ.get("SEARCH_KEEP_LOG"):
            shutil.copy(os.path.join(work, "driver.log"), os.environ["SEARCH_KEEP_LOG"])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\nwhole test {time.time() - started:.0f} s")
    print(f"{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
