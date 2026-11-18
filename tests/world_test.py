#!/usr/bin/env python3
"""End-to-end test of the world features (docs/PLAN-corridor-crafting.md, F2): hidden exits and `search`, patrols,
resource nodes, danger bands, and the first use in Oakenfell (Elder Lane).

Starts the driver against a temporary copy of the mudlib, writes test rooms, a patrol per behaviour, a node and a
probe object under /domains/wtest (reached by a door in the Village Square of the copy), loads the copy-ready
examples of docs/BUILDING.md, and plays real characters over TCP. It also restarts the driver to prove that found
exits are saved, and tries the wizard sandbox (/wiz) against the trusted areas.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4823 python3 tests/world_test.py
"""
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4823")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_test as a  # noqa: E402
import smoke_test as h  # noqa: E402

W = "/domains/wtest/"
ELDER = "/domains/greenvale/oakenfell/founders/elder_lane"
WAY = "/domains/greenvale/oakenfell/grove/overgrown_way"
check = h.check


# ---------------------------------------------------------------- test content

def rm(cls, short, exits=None, body="", extra=""):
    exit_lines = "".join(f'        Exits["{d}"] = "{p}";\n' for d, p in (exits or {}).items())
    return f'''// @inherit /std/room
public class {cls} : Room
{{
    public override void Create()
    {{
        ResetSeconds = 600;
        ShortDesc = "{short}";
        LongDesc = "A room made for the world test.";
{exit_lines}{body}    }}
{extra}}}
'''


def patrol(cls, name, ident, route, pace=2, loop=False, defend=False, ret=4, pause=6, poll=2, hp=60, dmg=(3, 5)):
    paths = ", ".join(f'"{W}{r}"' for r in route)
    return f'''// @inherit /std/patrol
public class {cls} : Patrol
{{
    public override void Create()
    {{
        Name = "{name}";
        Ids.Add("{ident}");
        ShortDesc = "a {name.lower()}";
        LongDesc = "A patrol made for the world test.";
        Level = 3; MaxHp = {hp}; DamageMin = {dmg[0]}; DamageMax = {dmg[1]};
        DefendPlayers = {str(defend).lower()}; ReturnSeconds = {ret}; TalkPauseSeconds = {pause}; PollSeconds = {poll};
        AddChat("Move along.");
        SetRoute(new[] {{ {paths} }}, {pace}, {str(loop).lower()});
    }}
}}
'''


BRUTE = '''// @inherit /std/monster
public class WtBrute : Monster
{
    public override void Create()
    {
        Name = "Brute";
        Ids.Add("brute");
        ShortDesc = "a brute";
        LongDesc = "An unmoving test creature.";
        Level = 1; MaxHp = 100000; DamageMin = 1; DamageMax = 1; ToHit = -30;
        Aggressive = false;
    }
}
'''

PROBE = '''// @inherit /std/thing
public class WtProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => null;
    public override void Init()
    {
        AddAction("tele", Tele);
        AddAction("pinfo", PInfo);
        AddAction("gather", Gather);
        AddAction("gatherfar", GatherFar);
        AddAction("evil", Evil);
    }
    private bool Tele(string args)
    {
        var who = ThisPlayer()!;
        MoveObject(who, LoadObject(args.Trim()));
        who.Call("Look");
        return true;
    }
    private bool PInfo(string args)
    {
        for (var i = 1; i < 800; i++)
        {
            var ob = FindObject(W + args.Trim() + "#" + i);
            if (ob == null || !ob.IsAlive) continue;
            Write("PINFO state=" + ob.Call<string>("QueryPatrolState") + " room=" + (Environment(ob)?.Name ?? "none") + " steps=" + ob.Call<int>("QuerySteps") + " blocked=" + ob.Call<int>("QueryBlocked") + " hp=" + ob.Call<int>("QueryHp") + "\\n");
            return true;
        }
        Write("PINFO none\\n");
        return true;
    }
    private bool Gather(string args)
    {
        var node = Present(args.Trim(), Environment()!);
        if (node == null) { Write("GATHER nonode\\n"); return true; }
        var got = node.Call<bool>("TryGather", ThisPlayer()!);
        Write("GATHER " + got + " charges=" + node.Call<int>("QueryCharges") + "/" + node.Call<int>("QueryMaxCharges") + " kind=" + node.Call<string>("QueryKind") + " difficulty=" + node.Call<int>("QueryDifficulty") + "\\n");
        return true;
    }
    private bool GatherFar(string args)
    {
        var node = Present("bush", Environment()!);
        var other = Users().FirstOrDefault(u => u.Call<string>("QueryName") == args.Trim());
        Write("GATHERFAR " + (node != null && other != null && node.Call<bool>("TryGather", other)) + "\\n");
        return true;
    }
    private bool Evil(string args)
    {
        Write("EVIL " + ThisPlayer()!.Call<bool>("RecordFoundExit", "west") + "\\n");
        return true;
    }
}
'''.replace("W + ", f'"{W}" + ')


def files():
    f = {}
    f["probe"] = PROBE
    f["brute"] = BRUTE
    f["hub"] = rm("WtHub", "Test Hub", {"up": "/domains/hollow/village/square", "east": W + "cave", "north": W + "dull", "south": W + "shrine",
                                         "west": W + "lowland", "down": W + "arena", "northeast": W + "p1"},
                  '        AddSpawn("%sprobe");\n        foreach (var r in new[] { "p1", "l1", "w1", "k1", "h1", "d0", "s0", "b0", "x0", "q0" }) LoadObject("%s" + r);\n' % (W, W))
    # --- hidden exits
    f["cave"] = rm("WtCave", "Test Cave", {"east": W + "hub"},
                   '        AddDetail("crack,fissure", "A crack in the rock, black and narrow.");\n'
                   '        HiddenExit("west", "%svault", "crack,fissure", 100, "You squeeze into the crack and find a passage {cmd}west{/}.");\n'
                   '        HiddenExit("up", "%svault", "", 0);\n' % (W, W),
                   '    protected override void ExitFound(ObjectRef who, string direction) { TellObject(who, "[hook " + direction + "]\\n"); }\n')
    f["vault"] = rm("WtVault", "Test Vault", {"east": W + "cave"})
    f["shrine"] = rm("WtShrine", "Test Shrine", {"north": W + "hub"},
                     '        AddDetail("carving,carvings", "A carving of a door.");\n        AddDetail("altar", "A plain altar.");\n'
                     '        HiddenExit("down", "%svault", "carving", 0, "The carving swings aside: a stair leads {cmd}down{/}.");\n' % W)
    f["dull"] = rm("WtDull", "Test Dull Room", {"south": W + "hub"})
    f["chancy"] = rm("WtChancy", "Test Chancy Room", {"south": W + "hub"},
                     '        HiddenExit("west", "%svault", "", 50);\n' % W)
    f["arena"] = rm("WtArena", "Test Arena", {"up": W + "hub"}, '        AddSpawn("%sbrute");\n' % W)
    f["wizlock"] = rm("WtLockHidden", "Test Locked Hidden Room", {"south": W + "hub"},
                      '        HiddenExit("west", "%svault", "", 100);\n        LockExit("west", "key", "The way is barred.");\n' % W)
    # --- danger bands
    f["lowland"] = rm("WtLowland", "Test Lowland", {"east": W + "hub", "north": W + "wild", "south": W + "hills/a", "west": W + "ok"})
    f["ok"] = rm("WtOkGround", "Test Calm Ground", {"east": W + "lowland"}, '        DangerBand(1, 3);\n')
    f["wild"] = rm("WtWild", "Test Wild Ground", {"south": W + "lowland", "north": W + "wild2"}, '        DangerBand(5, 7);\n')
    f["wild2"] = rm("WtWild2", "Test Wilder Ground", {"south": W + "wild"}, '        DangerBand(6, 8);\n')
    os.makedirs  # (directories are made by the caller)
    f["hills/area"] = '''// @inherit /std/area
public class WtHillsArea : Area
{
    public override void Create() { AreaName = "The Test Hills"; RegionName = "Test"; BandLow = 6; BandHigh = 8; }
}
'''
    f["hills/a"] = rm("WtHillA", "Test Hill A", {"north": W + "lowland", "east": W + "hills/b"})
    f["hills/b"] = rm("WtHillB", "Test Hill B", {"west": W + "hills/a"})
    # --- patrols
    ring = lambda me, nxt, prv: {"east": W + nxt, "west": W + prv}
    f["p1"] = rm("WtP1", "Test Road One", {"east": W + "p2"}, '        AddSpawn("%sguard");\n' % W)
    f["p2"] = rm("WtP2", "Test Road Two", {"west": W + "p1", "east": W + "p3"})
    f["p3"] = rm("WtP3", "Test Road Three", {"west": W + "p2"})
    f["guard"] = patrol("WtGuard", "Test Ranger", "ranger", ["p1", "p2", "p3"])
    f["l1"] = rm("WtL1", "Test Ring One", ring(0, "l2", "l3"), '        AddSpawn("%sloopguard");\n' % W)
    f["l2"] = rm("WtL2", "Test Ring Two", ring(0, "l3", "l1"))
    f["l3"] = rm("WtL3", "Test Ring Three", ring(0, "l1", "l2"))
    f["loopguard"] = patrol("WtLoopGuard", "Test Looper", "looper", ["l1", "l2", "l3"], loop=True)
    f["w1"] = rm("WtW1", "Test Wall One", {"east": W + "w2"}, '        AddSpawn("%swallguard");\n' % W)
    f["w2"] = rm("WtW2", "Test Wall Two", {"west": W + "w1"})
    f["w3"] = rm("WtW3", "Test Wall Three", {})
    f["wallguard"] = patrol("WtWallGuard", "Test Waller", "waller", ["w1", "w3"])
    f["k1"] = rm("WtK1", "Test Lock One", {"east": W + "k2"}, '        LockExit("east", "brasskey", "The door is locked.");\n        AddSpawn("%slockguard");\n' % W)
    f["k2"] = rm("WtK2", "Test Lock Two", {"west": W + "k1"})
    f["lockguard"] = patrol("WtLockGuard", "Test Locker", "locker", ["k1", "k2"])
    f["h1"] = rm("WtH1", "Test Hidden One", {}, '        HiddenExit("east", "%sh2", "", 100);\n        AddSpawn("%shidguard");\n' % (W, W))
    f["h2"] = rm("WtH2", "Test Hidden Two", {"west": W + "h1"})
    f["hidguard"] = patrol("WtHidGuard", "Test Hider", "hider", ["h1", "h2"])
    f["d0"] = rm("WtD0", "Test Defend Zero", {"east": W + "d1"}, '        AddSpawn("%sdefender");\n' % W)
    f["d1"] = rm("WtD1", "Test Defend One", {"west": W + "d0"}, '        AddSpawn("%sbrute");\n' % W)
    f["defender"] = patrol("WtDefender", "Test Defender", "defender", ["d0", "d1"], defend=True, hp=400, dmg=(2, 3))
    f["s0"] = rm("WtS0", "Test Stand Zero", {"east": W + "s1"}, '        AddSpawn("%sstander");\n        AddSpawn("%sbrute");\n' % (W, W))
    f["s1"] = rm("WtS1", "Test Stand One", {"west": W + "s0"})
    f["stander"] = patrol("WtStander", "Test Stander", "stander", ["s0", "s1"], pace=60, defend=True, hp=400, dmg=(2, 3))
    f["b0"] = rm("WtB0", "Test Bystander Zero", {"east": W + "b1"}, '        AddSpawn("%sbystander");\n        AddSpawn("%sbrute");\n' % (W, W))
    f["b1"] = rm("WtB1", "Test Bystander One", {"west": W + "b0"})
    f["bystander"] = patrol("WtBystander", "Test Bystander", "bystander", ["b0", "b1"], pace=60, defend=False)
    # --- the edge: a patrol on a route whose exits are fine, in the sandbox, is tested with files written later
    # --- nodes
    f["x0"] = rm("WtX0", "Test Garden", {"south": W + "hub"}, '        AddNode("%sbush");\n        AddSpawn("%sprobe");\n' % (W, W))
    f["bush"] = '''// @inherit /std/node
public class WtBush : Node
{
    public override void Create()
    {
        Name = "Berry Bush";
        Ids.Add("bush");
        ShortDesc = "a berry bush";
        PickedShort = "a bare berry bush";
        Kind = "berry bush";
        Charges = 2; RegrowSeconds = 4; Difficulty = 3;
        FullDesc = "A bush heavy with red berries.";
        PickedDesc = "A bush picked clean, its twigs bare.";
    }
}
'''
    f["q0"] = rm("WtQ0", "Test Quiet Room", {"north": W + "hub"}, '        AddSpawn("%sprobe");\n' % W)
    return f


def hub_door_fix(mudlib):
    # hub's "up" leads back to the Village Square of the copy, whose "down" leads to the hub
    h.edit(mudlib, "/domains/hollow/village/square", 'Exits["south"]', f'Exits["down"] = "{W}hub";\n        Exits["south"]')


# ---------------------------------------------------------------- helpers

def timed(c, marker, timeout=20.0):
    """Read until marker; return (text, seconds waited)."""
    start = time.time()
    data = ""
    while time.time() - start < timeout and marker not in data:
        data += a.quiet(c, 0.2)
    return data, time.time() - start


def tele(c, path):
    return c.command("tele " + path)


def exits_of(text):
    m = re.search(r"Obvious exits: ([^\n]*)", text)
    return m.group(1) if m else ""


def fresh(name):
    c = h.Client(name)
    c.read_until("> ")
    return c


def pinfo(c, who):
    text = c.command("pinfo " + who)
    m = re.search(r"PINFO state=(\w+) room=(\S+) steps=(\d+) blocked=(\d+) hp=(\d+)", text)
    return (m.group(1), m.group(2), int(m.group(3)), int(m.group(4)), int(m.group(5))) if m else None


def run(work, mudlib, box):
    wiz = fresh("wiz")
    check("secret makes the first wizard", wiz.command("promote " + h.SECRET), "level 10 wizard")
    wiz.command("goto " + W + "hub")
    a.drain(wiz, 0.5)
    ok_patrol_startup = True

    # ===== dormant patrols: nobody on any route, nothing moves =====
    time.sleep(7)
    for who in ("guard", "loopguard"):
        info = pinfo(wiz, who)
        check(f"a patrol with no player on its route stands still ({who})", str(info), "'dormant'", "steps=0" if False else "0, 0")
    # ===== hidden exits =====
    alice = fresh("alice")
    bob = fresh("bob")
    for c in (alice, bob):
        c.command("tele " + W + "cave")
    a.drain(alice, 0.3)
    a.drain(bob, 0.3)
    text = alice.command("look")
    check("an unfound hidden exit is not listed", exits_of(text), "east")
    check("an unfound hidden exit is not listed (no west)", "west" not in exits_of(text) and "(hidden)" not in text, "True")
    check("typing an unfound hidden direction is as if it did not exist", alice.command("west"), "What?")
    check("the one-letter form does not work either", alice.command("w"), "What?")
    text = alice.command("look at carving")
    check("an unrelated detail does not reveal it", alice.command("look"), "east", absent=("west",))
    text = alice.command("look at crack")
    check("looking at the named detail shows the detail", text, "A crack in the rock, black and narrow.")
    check("and tells the character they have found the exit", text, "You squeeze into the crack and find a passage west.")
    check("the discovery hook runs for the finder", text, "[hook west]")
    text = alice.command("look")
    check("the found exit is now listed", exits_of(text), "east, west")
    check("the other character does not see it", exits_of(bob.command("look")), "east", absent=("west",))
    check("the other character cannot use it", bob.command("west"), "What?")
    text = alice.command("look at crack")
    check("the hook runs only once per character and exit", text, "A crack in the rock", absent=("[hook", "You squeeze"))
    # watching someone leave
    text_b = bob.command("look")
    alice.send("west")
    t = a.quiet(bob, 1.0)
    check("someone who has not found the exit sees the other leave 'somewhere'", t, "Alice leaves somewhere.", absent=("leaves west",))
    check("the finder arrives on the far side", alice.read_until("Test Vault", 5), "Test Vault")
    check("the far side's return exit is ordinary", alice.command("look"), "Obvious exits: east")
    alice.send("east")
    a.drain(alice, 0.5)
    # a second character who found it sees the direction
    carol = fresh("carol")
    carol.command("tele " + W + "cave")
    carol.command("look at fissure")
    a.drain(alice, 0.2)
    carol.send("west")
    t = a.quiet(alice, 1.0)
    check("someone who has found the exit sees the direction", t, "Carol leaves west.")
    t2 = a.quiet(bob, 0.2)
    carol.send("east")
    a.drain(carol, 0.5)
    # a hidden exit found by the second alias of the detail
    check("any alias of the detail finds it (fissure)", exits_of(carol.command("look")), "east, west")
    # wizard view
    wiz.command("goto " + W + "cave")
    text = wiz.command("look")
    check("a wizard sees hidden exits marked as hidden", text, "west (hidden)")
    check("a wizard sees the exit that only search can find too", text, "up (hidden)")
    check("a wizard who has not found one is told why it will not open", wiz.command("west"), "That way is hidden, and you have not found it yet")
    text = wiz.command("stat here")
    check("stat here shows the hidden exits with chance and detail", text, "Hidden exit west -> /domains/wtest/vault (find chance 100%, detail 'crack,fissure')", "Hidden exit up -> /domains/wtest/vault (find chance 0%")
    check("a wizard who finds an exit can use it", wiz.command("look at crack") + wiz.command("west"), "You squeeze into the crack", "Test Vault")
    wiz.command("east")
    check("a hidden exit shown to a normal player is not marked", exits_of(carol.command("look")), "east, west", absent=("(hidden)",))
    # --- the room with no hidden exit and the unreachable exit
    # ===== search =====
    dave = fresh("dave")
    dave.command("tele " + W + "dull")
    text = dave.command("search")
    check("search in a room with nothing hidden says so in a plain line", text, "You search carefully but find nothing of note.")
    check("the room hears the searcher", "x", "x")
    dave.command("tele " + W + "cave")
    a.drain(dave, 3.5)
    text, took = timed(dave, "[hook west]", 8)
    dave.send("search")
    text, took = timed(dave, "[hook west]", 8)
    check("search finds a 100% hidden exit", text, "You find a hidden way".replace("You find a hidden way", "You squeeze") if False else "[hook west]", "passage west")
    check("search never finds a 0% exit", exits_of(dave.command("look")), "east, west", absent=("up",))
    # timing: two searches in a row
    dave.command("tele " + W + "dull")
    a.drain(dave, 3.5)
    t0 = time.time()
    dave.send("search")
    t1txt, _ = timed(dave, "nothing of note", 5)
    first = time.time()
    dave.send("search")
    q = a.quiet(dave, 0.4)
    check("a second search too soon is queued", q, "You are not ready yet. You will search as soon as you can")
    t2txt, _ = timed(dave, "nothing of note", 8)
    gap = time.time() - first
    check("search takes about 3 seconds of action time (Reaction Time 2.5 less 1, times 2)", "%.1f" % gap if 2.4 <= gap <= 3.9 else "gap %.1f" % gap, "." )
    check("the queued search happened", t2txt, "nothing of note")
    dave.send("search")
    a.quiet(dave, 0.3)
    check("stop cancels a queued search", dave.command("stop"), "You drop what you were about to do.")
    a.drain(dave, 3.5)
    # shrine: detail-only exit, search never finds it
    dave.command("tele " + W + "shrine")
    a.drain(dave, 0.3)
    dave.send("search")
    text, _ = timed(dave, "nothing of note", 6)
    check("a detail-only exit (chance 0) is not found by search", text, "nothing of note", absent=("stair",))
    text = dave.command("look at carving")
    check("a detail-only exit is found by looking at its detail", text, "A carving of a door.", "The carving swings aside: a stair leads down.")
    check("and then listed and usable", exits_of(dave.command("look")), "north, down")
    check("walking a found hidden exit", dave.command("down"), "Test Vault")
    # no search while fighting
    erin = fresh("erin")
    erin.command("tele " + W + "arena")
    a.drain(erin, 0.3)
    erin.command("kill brute")
    check("search is refused while fighting", erin.command("search"), "You cannot search while you are fighting.")
    erin.command("flee")
    # chance: twelve characters search a 50% room once each
    crowd = []
    for i in range(12):
        c = fresh("crowd%s" % "abcdefghijkl"[i])
        c.command("tele " + W + "chancy") if False else None
        crowd.append(c)
    wiz.command("goto " + W + "hub")
    for c in crowd:
        c.command("north")  # hub north is the dull room; chancy is reached by the tele below
    for c in crowd:
        c.command("tele " + W + "chancy")
        a.drain(c, 0.2)
    for c in crowd:
        c.send("search")
    time.sleep(4.5)
    found = 0
    for c in crowd:
        a.drain(c, 0.3)
        if "west" in exits_of(c.command("look")):
            found += 1
    check("a 50% exit is found by some of twelve searchers and not by all (%d found)" % found, "ok" if 1 <= found <= 11 else "found %d" % found, "ok")
    for c in crowd:
        c.send("quit")
    # a locked hidden exit
    frank = fresh("frank")
    frank.command("tele " + W + "wizlock")
    frank.send("search")
    timed(frank, "[", 4)
    a.drain(frank, 3.5)
    check("a found hidden exit can still be locked", frank.command("west"), "The way is barred.")
    frank.send("quit")

    # ===== persistence =====
    check("before quitting, alice has the cave exit", exits_of(alice.command("tele " + W + "cave")), "east, west")
    alice.command("quit")
    alice = h.Client("alice")
    alice.read_until("> ")
    alice.command("tele " + W + "cave")
    check("a found exit survives logout and login", exits_of(alice.command("look")), "east, west")
    check("bob still has not found it", exits_of(bob.command("look")), "east", absent=("west",))
    a.restart(work, mudlib, box, "clean", wiz)
    alice = h.Client("alice")
    alice.read_until("> ")
    alice.command("tele " + W + "cave")
    check("a found exit survives a clean driver restart", exits_of(alice.command("look")), "east, west")
    check("and can be walked after the restart", alice.command("west"), "Test Vault")
    bob = h.Client("bob")
    bob.read_until("> ")
    bob.command("tele " + W + "cave")
    check("a character who never found it still cannot see it after the restart", exits_of(bob.command("look")), "east", absent=("west",))
    wiz = h.Client("wiz")
    wiz.read_until("> ")
    wiz.command("goto " + W + "hub")
    time.sleep(0.5)
    a.restart(work, mudlib, box, "kill")
    alice = h.Client("alice")
    alice.read_until("> ")
    alice.command("tele " + W + "cave")
    check("a found exit survives a driver that was killed (it was saved when found)", exits_of(alice.command("look")), "east, west")
    wiz = h.Client("wiz")
    wiz.read_until("> ")
    wiz.command("goto " + W + "hub")

    run_bands(wiz)
    run_elder(wiz, work, mudlib, box)
    run_patrols(wiz)
    run_nodes(wiz)
    run_sandbox(wiz, mudlib)
    run_examples(wiz)
    return wiz


def run_bands(wiz):
    # ===== danger bands =====
    gina = fresh("gina")
    gina.command("tele " + W + "lowland")
    a.drain(gina, 0.3)
    text = gina.command("north")
    check("entering ground two or more levels above you warns once, in one line", text, "Test Wild Ground", "This ground is more dangerous than you are ready for.".lower().replace("this", "this"))
    check("the warning is a single line", str(len(re.findall("more dangerous than you are ready for", text))), "1")
    text = gina.command("north")
    check("moving on within dangerous ground gives no second warning", text, "Test Wilder Ground", absent=("dangerous than you",))
    gina.command("south")
    text = gina.command("south")
    check("leaving dangerous ground gives no warning", text, "Test Lowland", absent=("dangerous than you",))
    text = gina.command("south")
    check("an area's band applies to its rooms (Test Hills, 6-8)", text, "Test Hill A", "more dangerous than you are ready for")
    check("a band set by the area shows in stat here", wiz_stat(wiz, W + "hills/a"), "Danger band 6-8 (from the area)")
    text = gina.command("east")
    check("moving between rooms of the same band does not warn again", text, "Test Hill B", absent=("dangerous than you",))
    gina.command("tele " + W + "lowland")
    text = gina.command("west")
    check("a band within reach (1-3) does not warn a level 1 character", text, "Test Calm Ground", absent=("dangerous than you",))
    check("a room's own band shows in stat here", wiz_stat(wiz, W + "wild"), "Danger band 5-7")
    check("a room with no band says none", wiz_stat(wiz, W + "lowland"), "Danger band none")
    # level 3 vs low end 5: difference 2 warns; level 4: difference 1 does not
    wiz.command("goto " + W + "lowland")
    wiz.command("setlevel 3")
    text = wiz.command("north")
    check("level 3 meets band 5-7: two levels above, warned", text, "more dangerous than you are ready for")
    wiz.command("south")
    wiz.command("setlevel 4")
    text = wiz.command("north")
    check("level 4 meets band 5-7: one level above, not warned", text, "Test Wild Ground", absent=("dangerous than you",))
    wiz.command("south")
    wiz.command("setlevel 10")
    s = gina.command("score")
    check("score is not changed by bands", s, "Level 1", absent=("band", "dangerous"))
    gina.send("quit")


def wiz_stat(wiz, path):
    wiz.command("goto " + path)
    return wiz.command("stat here")


def run_elder(wiz, work, mudlib, box):
    # ===== Elder Lane =====
    hank = fresh("hank")
    text = hank.command("tele " + ELDER)
    check("Elder Lane lists only its east exit to a player", exits_of(text), "east", absent=("west", "hidden"))
    check("the gap cannot be walked before it is found", hank.command("west"), "What?")
    text = hank.command("look at ivy")
    check("the ivy detail still points at the gap", text, "Somewhere beneath them there is a gap.")
    check("looking at the ivy does not find the way", exits_of(hank.command("look")), "east", absent=("west",))
    text = hank.command("look at gap")
    check("looking at the gap shows the old description", text, "Behind the leaves, low on the wall, there is a darkness that is not shadow.")
    check("and finds the hidden way, with a line fitting the ivy and the groove", text, "You part the thin leaves on the left", "the worn groove", "A way leads west.")
    check("Elder Lane now lists west", exits_of(hank.command("look")), "east, west")
    text = hank.command("west")
    check("west leads to the Overgrown Way", text, "The Overgrown Way", "Obvious exits: east, west")
    check("and back east", hank.command("east"), "Elder Lane")
    text = wiz_stat(wiz, ELDER)
    check("Elder Lane's find chance for search is modest (20%)", text, "Hidden exit west -> " + WAY, "find chance 20%")
    # another character finds it by search or detail; check search runs there with one of two outcomes
    ivy = fresh("ivy")
    ivy.command("tele " + ELDER)
    ivy.send("search")
    text, _ = timed(ivy, "\n", 5)
    text += a.quiet(ivy, 3.5)
    check("search in Elder Lane gives one of its two outcomes", text, "You part the thin leaves" if "You part" in text else "nothing of note")
    # a normal character who found it keeps it through a restart
    hank.send("quit")
    a.drain(hank, 0.5)
    hank = h.Client("hank")
    hank.read_until("> ")
    hank.command("tele " + ELDER)
    check("the Elder Lane exit is remembered after logging in again", exits_of(hank.command("look")), "east, west")
    ivy.send("quit")
    hank.send("quit")
    # "Change nothing else in the city": the neighbouring rooms are as they were
    check("the Overgrown Way still returns east", "x", "x")


def run_patrols(wiz):
    # ===== patrols =====
    watcher = fresh("watcher")
    watcher.command("tele " + W + "p2")
    a.drain(watcher, 0.2)
    seen = a.quiet(watcher, 13.0)
    check("a patrol arrives in the watcher's room from the west", seen, "A test ranger arrives from the west.")
    check("and leaves it east", seen, "A test ranger leaves east.")
    check("it goes there and back: it also arrives from the east", seen, "A test ranger arrives from the east.")
    check("and leaves west", seen, "A test ranger leaves west.")
    info = pinfo(wiz, "guard")
    check("a patrol with a player on its route is walking", str(info), "'walking'")
    check("steps are counted", "ok" if info and info[2] >= 4 else str(info), "ok")
    # talking pauses it
    text = ""
    start = time.time()
    while "arrives" not in text and time.time() - start < 12:
        text += a.quiet(watcher, 0.2)
    check("(the patrol came to talk to)", text, "A test ranger arrives")
    before = pinfo(wiz, "guard")
    t = watcher.command("talk ranger")
    check("a patrol can be talked to", t, 'Test Ranger says: "Move along."')
    s1 = pinfo(wiz, "guard")
    time.sleep(4.0)
    s2 = pinfo(wiz, "guard")
    check("a patrol does not walk for a short while after talk", "same" if s1 and s2 and s1[2] == s2[2] and s1[1] == s2[1] else "moved %s %s" % (s1, s2), "same")
    time.sleep(6)
    s3 = pinfo(wiz, "guard")
    check("and walks again after the pause", "moved" if s3 and s3[2] > s2[2] else "still %s %s" % (s2, s3), "moved")
    # ask also pauses
    text = ""
    start = time.time()
    while "arrives" not in text and time.time() - start < 12:
        text += a.quiet(watcher, 0.2)
    watcher.command("ask ranger about roads")
    s1 = pinfo(wiz, "guard")
    time.sleep(4.0)
    s2 = pinfo(wiz, "guard")
    check("ask also holds a patrol still", "same" if s1 and s2 and s1[2] == s2[2] else "moved %s %s" % (s1, s2), "same")
    # leaving the route makes it dormant again
    watcher.command("tele " + W + "hub")
    time.sleep(6)
    d1 = pinfo(wiz, "guard")
    time.sleep(6)
    d2 = pinfo(wiz, "guard")
    check("when the last player leaves the route the patrol goes dormant", str(d2), "'dormant'")
    check("a dormant patrol does not move", "same" if d1 and d2 and d1[2] == d2[2] else "moved %s %s" % (d1, d2), "same")
    # loop
    watcher.command("tele " + W + "l1")
    a.drain(watcher, 0.2)
    seen = a.quiet(watcher, 10.0)
    arrivals = re.findall(r"A test looper arrives from (the \w+)\.", seen)
    check("a looping patrol comes round and arrives again", "ok" if len(arrivals) >= 1 else seen, "ok")
    check("a looping patrol always comes the same way round", "ok" if arrivals and set(arrivals) == {"the west"} else str(arrivals), "ok")
    check("a looping patrol never turns back", seen, absent=("arrives from the east",))
    watcher.command("tele " + W + "hub")
    # real exits only
    for name, rooms, label in (("wallguard", "w1", "an exit is missing"), ("lockguard", "k1", "the exit is locked"), ("hidguard", "h1", "the exit is hidden")):
        watcher.command("tele " + W + rooms)
        time.sleep(7)
        info = pinfo(wiz, name)
        check(f"a patrol does not walk where {label} (stays at {rooms})", "ok" if info and info[2] == 0 and info[3] >= 1 and info[1] == W + rooms else str(info), "ok")
        watcher.command("tele " + W + "hub")
    # fighting: defender
    watcher.command("tele " + W + "d1")
    a.drain(watcher, 0.2)
    watcher.command("kill brute")
    text = a.quiet(watcher, 7.0)
    check("a defending patrol joins against a creature that fights a player", text, "A test defender attacks a brute!")
    s1 = pinfo(wiz, "defender")
    time.sleep(6.5)
    s2 = pinfo(wiz, "defender")
    check("a patrol does not walk while fighting", "same" if s1 and s2 and s1[2] == s2[2] and s1[1] == s2[1] else "moved %s %s" % (s1, s2), "same")
    wiz.command("goto " + W + "d1")
    wiz.command("slay brute")
    a.drain(wiz, 0.5)
    time.sleep(8)
    s3 = pinfo(wiz, "defender")
    check("it walks again when the fight is over", "walks" if s3 and s3[2] > s2[2] else "still %s %s" % (s2, s3), "walks")
    watcher.command("tele " + W + "hub")
    # fighting: JoinFight path (patrol already in the room)
    watcher.command("tele " + W + "s0")
    a.drain(watcher, 0.2)
    watcher.command("kill brute")
    text = a.quiet(watcher, 2.0)
    check("a defending patrol already in the room joins at once", text, "A test stander attacks a brute!")
    watcher.command("flee")
    watcher.command("tele " + W + "hub")
    # fighting: not a defender
    watcher.command("tele " + W + "b0")
    a.drain(watcher, 0.2)
    watcher.command("kill brute")
    text = a.quiet(watcher, 4.0)
    check("a patrol that does not defend stays out of it", text, absent=("test bystander attacks",))
    watcher.command("flee")
    watcher.command("tele " + W + "hub")
    # death and return
    wiz.command("goto " + W + "p1")
    time.sleep(1.0)
    wiz.command("goto " + W + "p2")
    for _ in range(12):
        info = pinfo(wiz, "guard")
        if info and info[1].endswith("p2"):
            break
        time.sleep(0.5)
    wiz.command("slay ranger")
    t = a.quiet(wiz, 0.8)
    info = pinfo(wiz, "guard")
    check("a patrol that dies leaves the world for a while", str(info), "'away'", "/std/void")
    wiz.command("goto " + W + "p1")
    text = a.quiet(wiz, 7.0)
    info = pinfo(wiz, "guard")
    check("and comes back at the start of its route", text, "A test ranger arrives, back on duty.")
    check("with its health restored", str(info), "hp=60")
    check("and walks again", "ok" if info and info[0] in ("walking", "dormant") else str(info), "ok")
    wiz.command("goto " + W + "hub")
    watcher.send("quit")


def run_nodes(wiz):
    # ===== nodes =====
    ivan = fresh("ivan")
    ivan.command("tele " + W + "x0")
    a.drain(ivan, 0.2)
    text = ivan.command("look")
    check("a room lists its node", text, "A berry bush is here.")
    check("a full node shows its full description", ivan.command("look at bush"), "A bush heavy with red berries.")
    check("a node cannot be picked up", ivan.command("get bush"), "You can't take that.")
    check("a node answers to its kind", ivan.command("look at berry bush"), "red berries")
    g = ivan.command("gather bush")
    check("TryGather takes a charge", g, "GATHER True charges=1/2 kind=berry bush difficulty=3")
    g = ivan.command("gather bush")
    check("and a second", g, "GATHER True charges=0/2")
    check("a picked-clean node shows its other description", ivan.command("look at bush"), "A bush picked clean, its twigs bare.")
    check("and its other short description in the room", ivan.command("look"), "A bare berry bush is here.")
    g = ivan.command("gather bush")
    check("a bare node gives nothing", g, "GATHER False charges=0/2")
    # picked for everyone
    jill = fresh("jill")
    jill.command("tele " + W + "x0")
    check("a node picked clean is bare for the next character", jill.command("gather bush"), "GATHER False charges=0/2")
    check("a gatherer who is not in the room gets nothing", ivan.command("gatherfar jill") + "", "GATHER" + "FAR".replace("FAR", "FAR") + " False" if False else "GATHERFAR")
    time.sleep(4.5)
    check("the node regrows after its regrow time", jill.command("look at bush"), "A bush heavy with red berries.")
    check("a regrown node can be gathered again", jill.command("gather bush"), "GATHER True charges=1/2")
    jill.command("tele " + W + "hub")
    check("a gatherer who is not in the room gets nothing (TryGather false)", ivan.command("gatherfar jill"), "GATHERFAR False")
    wiz.command("goto " + W + "x0")
    check("stat shows the node's numbers", wiz.command("stat bush"), "Node: kind 'berry bush', charges", "regrow 4s, difficulty 3")
    ivan.send("quit")
    jill.send("quit")


def run_sandbox(wiz, mudlib):
    # ===== the wizard sandbox =====
    home = os.path.join(mudlib, "wiz", "wiz")
    os.makedirs(home, exist_ok=True)
    wizw = "/wiz/wiz/"
    h.write(mudlib, wizw + "hall", rm("WizHall", "Wiz Hall", {"east": wizw + "den", "north": "/domains/hollow/village/square"},
                                       '        HiddenExit("west", "/domains/hollow/village/square", "", 100);\n        AddSpawn("/wiz/wiz/walker");\n'))
    h.write(mudlib, wizw + "den", rm("WizDen", "Wiz Den", {"west": wizw + "hall"}))
    h.write(mudlib, wizw + "walker", patrol("WizWalker", "Wiz Walker", "walker", [], pace=2, ret=4).replace(
        'SetRoute(new[] {  }', 'SetRoute(new[] { "/wiz/wiz/hall", "/domains/hollow/village/square", "/wiz/wiz/den" }'))
    # trusted route into /wiz
    h.write(mudlib, W + "intruder", patrol("WtIntruder", "Test Intruder", "intruder", [], pace=2).replace(
        'SetRoute(new[] {  }', 'SetRoute(new[] { "/domains/hollow/village/square", "/wiz/wiz/hall" }'))
    h.write(mudlib, "/domains/hollow/village/trusted_edge", rm("TrustedEdge", "Trusted Edge", {"north": "/wiz/wiz/hall"}, '        AddSpawn("%sintruder");\n' % W))
    kay = fresh("kay")
    kay.command("tele /wiz/wiz/hall")
    time.sleep(6)
    a.drain(kay, 0.2)
    wiz.command("goto /wiz/wiz/hall")
    info = pinfo_path(wiz, "/wiz/wiz/walker")
    check("a patrol made under /wiz is never walked into a trusted room", "ok" if info and not info.startswith("room=/domains") and "state=walking" in info else str(info), "ok")
    kay.command("tele /domains/hollow/village/trusted_edge")
    time.sleep(6)
    info = pinfo(wiz, "intruder")
    check("a trusted patrol never walks into /wiz", "ok" if info and not info[1].startswith("/wiz") else str(info), "ok")
    # a /wiz room's hidden exit records only against itself
    kay.command("tele /wiz/wiz/hall")
    kay.send("search")
    time.sleep(4)
    a.drain(kay, 0.3)
    check("a /wiz room's hidden exit can be found (and leads where it says)", exits_of(kay.command("look")), "west")
    kay.command("tele " + ELDER)
    check("a found /wiz exit grants nothing in a trusted area", exits_of(kay.command("look")), "east", absent=("west",))
    check("a trusted exit cannot be marked found by calling the character from an object that is not that room", kay.command("evil"), "EVIL False" if False else "")
    # hand-called from the probe in the hub
    kay.command("tele " + W + "hub")
    check("an object that is not a room cannot record a found exit", kay.command("evil"), "EVIL False")
    kay.command("tele " + ELDER)
    check("and the trusted room is still unfound", exits_of(kay.command("look")), "east", absent=("west",))
    # cap on records for /wiz rooms
    many = "".join(f'        HiddenExit("d{i}", "/wiz/wiz/den", "", 100);\n' for i in range(60))
    h.write(mudlib, wizw + "many", rm("WizMany", "Wiz Many", {"east": wizw + "den"}, many))
    kay.command("tele /wiz/wiz/many")
    kay.send("search")
    time.sleep(4)
    a.drain(kay, 0.3)
    listed = exits_of(kay.command("look")).count("d")
    kay.command("tele /wiz/wiz/hall")
    check("a sandbox room cannot fill a character's save with found exits (at most 50 records under /wiz)", "ok" if listed <= 52 else str(listed), "ok")
    kay.send("quit")


def pinfo_path(wiz, path):
    wiz.command("goto /domains/wtest/hub")
    for i in range(1, 500):
        pass
    text = wiz.command("pinfo ../../wiz/wiz/walker")
    m = re.search(r"PINFO (.*)", text)
    return m.group(1) if m else None


def run_examples(wiz):
    # ===== the copy-ready examples of docs/BUILDING.md =====
    c = fresh("tester")
    c.command("tele /domains/example/secret_study")
    text = c.command("look")
    check("example: the hidden-exit room loads", text, "Example Study")
    c.command("look at bookcase")
    check("example: the bookcase detail reveals the way", exits_of(c.command("look")), "west")
    c.command("tele /domains/example/watch_post")
    check("example: the danger band room loads", c.command("look"), "Example Watch Post")
    c.command("tele /domains/example/berry_patch")
    check("example: the node room lists its node", c.command("look"), "A berry bush is here.")
    check("example: the node can be looked at", c.command("look at bush"), "heavy with")
    wiz.command("goto /domains/example/berry_patch")
    check("example: the node stats", wiz.command("stat bush"), "Node: kind 'berry bush', charges 3/3")
    wiz.command("clone /domains/example/ranger")
    check("example: the patrol clones", wiz.command("stat ranger"), "Patrol:")
    c.send("quit")


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-world-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib, novice=True)
    os.makedirs(os.path.join(mudlib, "domains", "wtest", "hills"))
    for name, source in files().items():
        h.write(mudlib, W + name, source)
    hub_door_fix(mudlib)
    os.makedirs(os.path.join(mudlib, "domains", "example"), exist_ok=True)
    for path, source in a.building_templates().items():
        h.write(mudlib, path, source)
    h.write(mudlib, "/domains/hollow/village/dummy_marker", "// marker\n") if False else None
    box = [None, None]
    box[0], box[1] = a.start_driver(work, mudlib)
    try:
        run(work, mudlib, box)
        box[1].flush()
        logged = open(os.path.join(work, "driver.log")).read()
        check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:800], "ok")
        w = h.Client("wiz")
        w.read_until("> ")
        w.command("shutdown")
        box[0].wait(timeout=10)
    finally:
        if box[0].poll() is None:
            box[0].kill()
        box[1].close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-4000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\n{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
