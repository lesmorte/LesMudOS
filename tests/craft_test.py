#!/usr/bin/env python3
"""End-to-end test of the crafting and gathering engine (docs/CRAFTING.md).

Starts the driver against a temporary copy of the mudlib with the sample content of tests/fixtures/crafting/samples laid
over it (six recipes, materials, tools, stations, a recipe book, a trainer, nodes and test creatures under
/domains/crafttest), plus rooms and a trusted probe written here, and a wizard's sandbox probe under /wiz/wizzy. It plays
real non-wizard characters over TCP and checks every command and refusal, proficiency growth and its limits, harvest
quality (exact odds against tests/craft_sim.py, statistical samples with honest bounds, and deterministic rolls), the corpse
rules, node yields, the matcher and the clash rule, quality inheritance (the owner's herb-and-wildflower example), stations,
discovery by teaching, by book and by experiment, the Chronicle first for a Rare recipe and none for a wizard, persistence
across a clean restart and a killed driver, a crash right after a craft and quitting mid-queue, the sandbox refusals, and a
thousand generated recipes loaded and matched (timed).

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4829 python3 tests/craft_test.py
"""
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4829")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_test as a  # noqa: E402
import craft_sim as sim  # noqa: E402
import smoke_test as h  # noqa: E402

T = "/domains/crafttest/"
O = T + "obj/"
SAMPLES = os.path.join(h.ROOT, "tests", "fixtures", "crafting", "samples")
CHECKS = [0]
TIMINGS = {}


def check(label, text, *expected, absent=()):
    CHECKS[0] += 1
    h.check(label, text, *expected, absent=absent)


def truth(label, ok, detail=""):
    CHECKS[0] += 1
    h.check(label, "ok" if ok else "NOT OK " + str(detail), "ok")


# ---------------------------------------------------------------- test content written here

def room(cls, short, exits, body="", safe=True, extra=""):
    ex = "".join(f'        Exits["{d}"] = "{p}";\n' for d, p in exits.items())
    return f"""// @inherit /std/room
public class {cls} : Room
{{
    public override void Create()
    {{
        ShortDesc = "{short}";
        LongDesc = "A room made for the crafting test.";
        Safe = {"true" if safe else "false"};
{ex}{body}    }}
{extra}}}
"""


HALL_INIT = """    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who != null && Interactive(who) && Present("probe", who) == null) MoveObject(CloneObject("/domains/crafttest/probe"), who);
    }
"""

PROBE = r'''// @inherit /std/thing
public class CtProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    private ObjectRef D => LoadObject("/secure/crafting");
    public override void Init()
    {
        foreach (var v in new[] { "tele", "mk", "spawn", "slaynow", "dq", "hurt", "tired", "ctimers", "pinfo", "who", "chron", "teachme", "thief" })
            AddAction(v, Do);
    }
    private static string F(double d) => ((long)Math.Round(d * 1000)).ToString();
    private static string L(List<double> l) => string.Join(" ", l.Select(F));
    private static double P(string s) { var neg = s.StartsWith("-"); var t = neg ? s.Substring(1) : s; var parts = t.Split('.'); var v = (double)long.Parse(parts[0]); if (parts.Length > 1) v += long.Parse(parts[1]) / Math.Pow(10, parts[1].Length); return neg ? -v : v; }
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var room = Environment(me)!;
        switch (QueryVerb())
        {
            case "tele": MoveObject(me, LoadObject(args.Trim())); me.Call("Look"); return true;
            case "mk":
            {
                // mk RECORD COUNT QUALITY
                var made = CreateIn(w[0], int.Parse(w[1]), int.Parse(w[2]), me);
                Write("MK " + (made == null ? "null" : made.Call<string>("Short")) + "\n");
                return true;
            }
            case "spawn": { var ob = CloneObject("/domains/crafttest/mob/" + w[0]); MoveObject(ob, room); Write("SPAWNED " + ob.Name + "\n"); return true; }
            case "slaynow": { var ob = Present(w[0], room); if (ob == null) { Write("SLAY none\n"); return true; } ob.Call("Die", me); Write("SLAIN\n"); return true; }
            case "hurt": me.Call("Heal", 9999); me.Call("TakeDamage", int.Parse(w[0]), null); Write("Vitality " + me.Call<int>("QueryVitality") + "/" + me.Call<int>("QueryMaxVitality") + "\n"); return true;
            case "tired": me.Call("SetResources", int.Parse(w[0]), 0); Write("ST " + me.Call<int>("QueryEndurance") + "\n"); return true;
            case "ctimers":
            {
                // Every corpse in the room; SetTimers itself checks that the current player is a wizard of level 10.
                var n = 0;
                foreach (var c in AllInventory(room).Where(o => o.Call<bool>("IsCorpse"))) { c.Call("SetTimers", int.Parse(w[0]), int.Parse(w[1]), int.Parse(w[2])); n++; }
                Write("TIMERS " + n + "\n");
                return true;
            }
            case "pinfo":
            {
                var who = Users().FirstOrDefault(u => u.Call<string>("QueryName")!.ToLowerInvariant() == w[0]);
                if (who == null) { Write("PINFO none\n"); return true; }
                var profs = who.Call<Dictionary<string, int>>("QueryProficiencies")!;
                var known = who.Call<Dictionary<string, string>>("QueryKnownRecipes")!;
                Write("PINFO profs=" + string.Join(",", profs.OrderBy(k => k.Key).Select(k => k.Key + ":" + k.Value)) + " known=" + string.Join(",", known.OrderBy(k => k.Key).Select(k => k.Key + ":" + k.Value)) + " gold=" + who.Call<int>("QueryGold") + "\n");
                return true;
            }
            case "who":
            {
                // who NAME: the player's inventory as records
                var who = Users().FirstOrDefault(u => u.Call<string>("QueryName")!.ToLowerInvariant() == w[0]);
                Write("INV " + (who == null ? "none" : string.Join(" | ", AllInventory(who).Where(o => o.Call("QueryQuantity") is int).Select(o => RecordOf(o)))) + "\n");
                return true;
            }
            case "chron":
            {
                var chron = LoadObject("/secure/chronicle");
                var key = w[0];
                var has = chron.Call<bool>("HasOccurred", key);
                var entry = has ? chron.Call<string>("QueryOccurred", key, "entry") ?? "" : "";
                var text = int.TryParse(entry, out var id) && id > 0 ? chron.Call<string>("EntryText", id) ?? "" : "";
                var holders = has ? chron.Call<string>("QueryOccurred", key, "holders") ?? "" : "";
                Write("CHRON has=" + has + " holders=" + holders + " entry=" + entry + " text=" + text.Replace("\n", " ") + "\n");
                return true;
            }
            case "teachme": Write("TEACH " + D.Call<string>("Teach", me, w[0]) + "\n"); return true;
            case "thief":
            {
                // A trusted object (this probe) asks the corpse to give up its yield directly: refused, only the daemon may.
                var c = Present("corpse", room);
                Write("THIEF " + (c != null && c.Call<bool>("TakeYield", "skin")) + "\n");
                return true;
            }
            case "dq":
            {
                switch (w[0])
                {
                    case "status": Write("DQ " + D.Call<string>("QueryStatus") + "\n"); return true;
                    case "problems": Write("DQ " + string.Join(" || ", D.Call<List<string>>("QueryProblems")!) + "\n"); return true;
                    case "clashes": Write("DQ " + string.Join(" || ", D.Call<List<string>>("QueryClashes")!) + "\n"); return true;
                    case "ids": Write("DQ " + string.Join(",", D.Call<List<string>>("QueryRecipeIds")!) + "\n"); return true;
                    case "mats": Write("DQ " + string.Join(",", D.Call<List<string>>("QueryMaterialIds")!) + "\n"); return true;
                    case "match": Write("DQ match=" + D.Call<string>("QueryMatch", w[1].Split(',').ToList()) + "\n"); return true;
                    case "rolls": Write("DQ rolls=" + D.Call<bool>("SetTestRolls", w[1]) + "\n"); return true;
                    case "refresh": D.Call("Refresh", true); Write("DQ refreshed\n"); return true;
                    case "hodds": Write("DQ " + L(D.Call<List<double>>("HarvestOdds", P(w[1]), int.Parse(w[2]), P(w[3]))!) + "\n"); return true;
                    case "codds": Write("DQ " + L(D.Call<List<double>>("CraftOdds", P(w[1]), P(w[2]), P(w[3]))!) + "\n"); return true;
                    case "hsample": Write("DQ " + string.Join(" ", D.Call<List<int>>("RollHarvestSample", P(w[1]), int.Parse(w[2]), P(w[3]), int.Parse(w[4]))!) + "\n"); return true;
                    case "csample": Write("DQ " + string.Join(" ", D.Call<List<int>>("RollCraftSample", P(w[1]), P(w[2]), P(w[3]), int.Parse(w[4]))!) + "\n"); return true;
                    case "gstep": Write("DQ gstep=" + string.Join(",", D.Call<List<int>>("GrowthFor", int.Parse(w[1]), P(w[2]), int.Parse(w[3]), P(w[4]))!) + "\n"); return true;
                    case "ladderlen": Write("DQ ladderlen=" + string.Join(",", D.Call<List<double>>("LadderFor", int.Parse(w[1]))!) + "\n"); return true;
                    case "ladder":
                    {
                        var who = Users().FirstOrDefault(u => u.Call<string>("QueryName")!.ToLowerInvariant() == w[1]);
                        Write("DQ ladder=" + (who == null ? -9 : D.Call<int>("WizardQueryLadder", who, w[2])) + "\n");
                        return true;
                    }
                    case "setprof":
                    {
                        var who = Users().FirstOrDefault(u => u.Call<string>("QueryName")!.ToLowerInvariant() == w[1]);
                        Write("DQ setprof=" + (who != null && D.Call<bool>("WizardSetProficiency", who, w[2], int.Parse(w[3]))) + "\n");
                        return true;
                    }
                    case "info": { var i = D.Call<Dictionary<string, string>>("QueryRecipeInfo", w[1]); Write("DQ " + (i == null ? "null" : string.Join(";", i.Select(k => k.Key + "=" + k.Value))) + "\n"); return true; }
                    case "timematch":
                    {
                        // timematch N LIST|LIST|... : N lookups cycling through the offerings, timed; reports hits.
                        var lists = w[2].Split('|').Select(x => x.Split(',').ToList()).ToList();
                        var n = int.Parse(w[1]);
                        var hits = 0;
                        var start = TimeMs();
                        for (var i = 0; i < n; i++) if (D.Call<string>("QueryMatch", lists[i % lists.Count]) != "") hits++;
                        Write("DQ timed n=" + n + " hits=" + hits + " ms=" + (TimeMs() - start) + "\n");
                        return true;
                    }
                }
                Write("DQ ?\n");
                return true;
            }
        }
        return false;
    }
}
'''

POST = """// @inherit /std/monster
public class CtPost : Monster
{
    public override void Create()
    {
        Name = "Training Post"; Ids.Add("post"); ShortDesc = "a training post"; LongDesc = "It fights back, feebly.";
        Level = 1; MaxVitality = 5000; DamageMin = 1; DamageMax = 1; ToHit = -40;
    }
}
"""

GOBLIN_SHOP = """// @inherit /std/shopkeeper
public class CtSkinShop : Shopkeeper
{
    public override void Create()
    {
        Name = "Skinner Sal"; Ids.Add("sal"); ShortDesc = "Skinner Sal"; LongDesc = "A seller of skins.";
        AddStock("/std/skin?p.kind=rabbit", 3);
        AddStock("/std/blood?p.kind=wolf", 4);
        AddStock("/std/blood?p.kind=goblin", 8);
        AddStock("/domains/crafttest/obj/cloth_strip", 2);
    }
}
"""

# ---- the wizard's sandbox: everything here must be refused
EVIL = r'''// @inherit /std/thing
public class Evil : Thing
{
    public override void Create() { Name = "Evil"; Ids.Add("evil"); }
    public override string? Short() => "an evil gadget";
    public override bool CanGet() => true;
    public override void Init() { AddAction("evil", Do); }
    private bool Do(string args)
    {
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var target = Users().FirstOrDefault(u => u.Call<string>("QueryName")!.ToLowerInvariant() == w[0])!;
        var d = LoadObject("/secure/crafting");
        var room = Environment(target)!;
        var r = new List<string>();
        r.Add("teach=" + d.Call<string>("Teach", target, "berry_juice"));
        r.Add("learn=" + target.Call<bool>("CraftLearn", "berry_juice", "taught"));
        r.Add("setprof=" + target.Call<bool>("CraftSetProficiency", "skinning", 1000));
        r.Add("wizset=" + d.Call<bool>("WizardSetProficiency", target, "skinning", 1000));
        r.Add("setladder=" + target.Call<bool>("CraftSetLadder", "skinning", 3));
        r.Add("readladder=" + target.Call<int>("QueryCraftLadder", "skinning"));
        r.Add("wizladder=" + d.Call<int>("WizardQueryLadder", target, "skinning"));
        r.Add("rolls=" + d.Call<bool>("SetTestRolls", "high"));
        var cmd = d.Call<List<object>>("Command", target, "recipes", "");
        r.Add("command=" + (cmd == null || cmd.Count == 0 ? "null" : "[" + cmd[0] + "]"));
        d.Call("Resolve", target, null);
        r.Add("resolve=done");
        var corpse = Present("corpse", room);
        r.Add("takeyield=" + (corpse != null && corpse.Call<bool>("TakeYield", "skin")));
        var node = Present("bush", room);
        r.Add("trygather=" + (node != null && node.Call<bool>("TryGather", target)));
        // CHANGED (sandbox): what sandbox code makes cannot be moved to a player outside the workshop: the move throws.
        ObjectRef? made = null;
        try { made = CreateIn("/std/blood?p.kind=goblin", 3, 4, target); } catch (MudRuntimeException) { }
        r.Add("mintgoblin=" + (made == null ? "null" : made.Call<string>("Short")));
        var vial = CloneObject("/std/blood");
        r.Add("setkind=" + vial.Call<bool>("SetProperty", "kind", "goblin"));
        vial.Call("Vanish");
        ObjectRef? herb = null;
        try { herb = CreateIn("/domains/crafttest/obj/healing_herb", 2, 4, target); } catch (MudRuntimeException) { }
        r.Add("mintherb=" + (herb == null ? "null" : herb.Call<string>("Short")));
        r.Add("heal=" + d.Call<bool>("StartHeal", target, 50, 10, 1, "Evil"));
        r.Add("craftsave=" + "ok");
        target.Call("CraftSave");
        r.Add("continue=" + target.Call<bool>("CraftContinue", "x", "y", 1.0, 0.0, 0.0));
        // A corpse set up by the sandbox from a fake creature that claims goblin blood.
        var fake = CloneObject("/wiz/wizzy/fakegob");
        try { MoveObject(fake, room); } catch (MudRuntimeException) { }
        var c2 = CloneObject("/std/corpse");
        try { MoveObject(c2, room); } catch (MudRuntimeException) { }
        c2.Call("Setup", "a fake goblin", target, fake);
        r.Add("fakeyield=[" + c2.Call<string>("QueryYieldKind", "bleed") + "]");
        fake.Call("Vanish");
        Write("EVIL " + string.Join(" ", r) + "\n");
        return true;
    }
}
'''

FAKEGOB = """// @inherit /std/monster
public class FakeGob : Monster
{
    public string Skin { get; set; } = "wolf";
    public string Blood { get; set; } = "goblin";
    public override void Create() { Name = "Fake Goblin"; Ids.Add("fakegob"); ShortDesc = "a fake goblin"; MaxVitality = 1; }
    public void Vanish() => Destruct();
}
"""

EVIL_KNIFE = """// @inherit /std/tool
public class EvilKnife : Tool
{
    public override void Create() { Name = "Evil Knife"; Ids.Add("knife"); Ids.Add("evil knife"); ShortDesc = "an evil knife"; ToolType = "skinning knife"; SetQuality(4); }
}
"""

EVIL_TABLE = """// @inherit /std/station
public class EvilTable : Station
{
    public override void Create() { Name = "Evil Table"; Ids.Add("table"); ShortDesc = "an evil herbalist table"; StationKind = "herbalist table"; }
}
"""

EVIL_HERB = """// @inherit /std/material
public class EvilHerb : Material
{
    public override void Create() { Name = "Healing Herb"; Ids.Add("herb"); Ids.Add("healing herb"); ShortDesc = "a healing herb"; Value = 3; SetQuality(4); }
}
"""

EVIL_ROOM = """// @inherit /std/room
public class WizzyLab : Room
{
    public override void Create()
    {
        ShortDesc = "Wizzy's Lab";
        LongDesc = "A wizard's room.";
        Exits["up"] = "/domains/crafttest/hall";
        AddSpawn("/wiz/wizzy/table");
    }
}
"""


def test_files():
    f = {}
    f[T + "probe"] = PROBE
    f[T + "mob/tc_post"] = POST
    f[T + "skinshop"] = GOBLIN_SHOP
    f[T + "hall"] = room("CtHall", "Craft Hall", {"up": "/domains/hollow/village/square", "east": T + "workshop", "west": T + "field", "down": T + "arena", "north": T + "market"},
                         "", True, HALL_INIT)
    f[T + "workshop"] = room("CtWorkshop", "Craft Workshop", {"west": T + "hall"},
                             f'        AddSpawn("{O}herbalist_table");\n        AddSpawn("{O}cooking_fire");\n        AddSpawn("{O}workbench");\n        AddSpawn("{T}trainer");\n')
    f[T + "field"] = room("CtField", "Craft Field", {"east": T + "hall"},
                          f'        AddNode("{O}berry_bush");\n        AddNode("{O}herb_patch");\n        AddNode("{O}stone_pile");\n', False)
    f[T + "arena"] = room("CtArena", "Craft Arena", {"up": T + "hall"}, "", False)
    f[T + "lockroom"] = room("CtLockRoom", "Craft Lock Room", {"up": T + "hall"}, "", False)
    f[T + "decayroom"] = room("CtDecayRoom", "Craft Decay Room", {"up": T + "hall"}, "", False)
    f[T + "market"] = room("CtMarket", "Craft Market", {"south": T + "hall"}, f'        AddSpawn("{T}skinshop");\n')
    return f


# ---------------------------------------------------------------- helpers

def fresh(name):
    c = h.Client(name)
    c.read_until("> ")
    c.command("down")  # to the hall, which hands out the probe
    return c


def login(name):
    c = h.Client(name)
    c.read_until("> ")
    return c


def act(c, line, *markers, timeout=20, settle=0.0):
    """Send a command that is an action on the combat clock and read until one of the markers appears."""
    start = time.time()
    c.send(line)
    text = c.read_any(markers, timeout=timeout) if markers else a.quiet(c, 4)
    text += a.quiet(c, 0.3)
    left = settle - (time.time() - start)
    if left > 0:
        text += a.quiet(c, left)
    return text


GATHER_SETTLE = 6.4  # (Reaction Time 2.5 + Recovery 0.5) x 2 s: until the character may act again
RESULT = ("take", "ruined", "spills", "fumble", "not yours", "already", "gone cold", "need ", "There is", "cannot", "picked clean",
          "do not know", "Nothing", "You can only", "Corpses are not", "nothing comes", "You make", "You stop", "still need", "must be made",
          "do not know a recipe")


def dq(c, rest):
    text = c.command("dq " + rest)
    m = re.search(r"DQ ([^\r\n]*)", text)
    return m.group(1) if m else text


def pinfo(c, name):
    text = c.command("pinfo " + name)
    m = re.search(r"PINFO profs=(\S*) known=(\S*) gold=(\d+)", text)
    if not m:
        return {}, {}, -1
    profs = dict((k, int(v)) for k, v in (p.split(":") for p in m.group(1).split(",") if p))
    known = dict(p.split(":") for p in m.group(2).split(",") if p)
    return profs, known, int(m.group(3))


def inventory(c, name):
    text = c.command("who " + name)
    m = re.search(r"INV ([^\r\n]*)", text)
    return m.group(1) if m else ""


def count_in(inv, record_prefix, quality=None):
    """How many units of a record path (with or without ?p.kind) the inventory records hold."""
    total = 0
    for rec in inv.split(" | "):
        rec = rec.strip()
        if not rec:
            continue
        path, _, state = rec.partition("?")
        parts = dict(p.split("=", 1) for p in state.split("&") if "=" in p)
        kind = parts.get("p.kind")
        ident = path + ("?p.kind=" + kind if kind else "")
        if ident != record_prefix:
            continue
        q = int(parts.get("q", "1"))
        if quality is not None and q != quality:
            continue
        total += int(parts.get("n", "1"))
    return total


def numbers_now():
    numbers, _ = sim.parse_numbers(sim.Tree(MUDLIB[0]))
    return sim.Numbers(numbers)


MUDLIB = [None]


def odds_close(daemon_text, python_list, label):
    got = [int(x) for x in daemon_text.split()]
    want = [int(round(x * 1000)) for x in python_list]
    truth(label, got == want, f"daemon {got} python {want}")


# ---------------------------------------------------------------- the run

def run(work, mudlib, box):
    log_path = os.path.join(work, "driver.log")

    def logged():
        box[1].flush()
        return open(log_path).read()

    w = fresh("warden")
    check("secret makes the warden a level-10 wizard", w.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(w, 0.4)

    # ===== A. the data loaded at boot =====
    check("the daemon logs what it loaded at boot", logged(), "crafting: loaded 6 recipes")
    st = dq(w, "status")
    check("status: six sample recipes and the materials", st, "recipes=6")
    mats = dq(w, "mats")
    for m in ("wolf_skin", "wolf_blood", "goblin_blood", "rabbit_skin", "healing_herb", "berry_juice", "bear_skin"):
        check(f"the materials table has {m}", "," + mats + ",", "," + m + ",")
    check("no load problems in the sample data", dq(w, "problems"), absent=("not loaded", "unknown"))
    N = numbers_now()
    for prof, tool, work_ in [(0, 1, 6), (0, 0, 22), (37.5, 2, 22), (50, 3, 48), (100, 4, 6), (100, 5, 70)]:
        odds_close(dq(w, f"hodds {prof} {tool} {work_}"), sim.harvest_odds(N, prof, tool, work_), f"harvest odds match the simulator exactly (prof {prof}, tool {tool}, work {work_})")
    for avg, prof, work_ in [(2.5, 0, 10), (1, 0, 45), (1, 100, 10), (0.5, 33.3, 25), (4, 100, 0)]:
        odds_close(dq(w, f"codds {avg} {prof} {work_}"), sim.craft_odds(N, avg, prof, work_), f"crafting odds match the simulator exactly (avg {avg}, prof {prof}, work {work_})")
    for tenths in (0, 190, 200, 399, 400, 599, 600, 850, 995, 1000):
        got = dq(w, f"ladderlen {tenths}")
        truth(f"the ladder follows the simulator (skill {tenths / 10})", got.startswith("ladderlen=") and [float(x) for x in got.split("=")[1].split(",")] == sim.ladder(N, tenths), got)
    for tenths, work_, pos, roll in [(0, 6, 0, 19.9), (0, 6, 0, 20.0), (0, 6, 4, 99.9), (0, 6, 9, 0.0), (190, 6, 3, 74.9), (190, 6, 3, 75.0),
                                     (200, 6, 0, 19.9), (200, 6, 1, 20.0), (200, 6, 9, 99.0), (300, 6, 0, 9.9), (300, 6, 0, 10.0), (300, 6, 4, 24.9), (300, 6, 4, 25.0),
                                     (300, 6, 9, 49.9), (300, 6, 9, 50.0), (300, 6, 99, 10.0), (300, 22, 4, 49.9), (300, 22, 4, 50.0), (300, 22, 4, 99.0),
                                     (300, 80, 0, 19.9), (300, 80, 0, 20.0), (500, 45, 3, 30.0), (500, 6, 3, 0.0), (500, 6, 3, 99.0),
                                     (995, 100, 0, 19.9), (995, 100, 14, 99.9), (1000, 6, 0, 0.0), (1000, 100, 0, 0.0)]:
        want = sim.growth_step(N, tenths, work_, pos, roll)
        check(f"growth step matches the simulator (skill {tenths / 10}, work {work_}, position {pos}, roll {roll})",
              dq(w, f"gstep {tenths} {work_} {pos} {roll}"), f"gstep={1 if want[0] else 0},{want[1]}")

    # ===== B. proficiencies, help =====
    ann = fresh("ann")
    text = ann.command("proficiencies")
    check("proficiencies lists the gathering and crafting skills at 0", text, "Gathering", "Skinning", "Bleeding", "Harvesting", "Crafting", "Cooking", "Herbalism", "Tanning", "Tinkering")
    check("proficiencies are shown in numbers and words", text, "0  untrained")
    check("help lists the crafting commands", ann.command("help", marker="Colour:"), "Crafting:", "skin", "combine", "proficiencies")
    a.drain(ann, 0.4)
    check("help crafting explains the system", ann.command("help crafting"), "Crafting and gathering", "never fails")
    for verb in ("skin", "bleed", "harvest", "craft", "combine", "recipes", "recipe", "proficiencies", "apply"):
        check(f"help {verb} has its own text", ann.command("help " + verb), verb)
    check("recipes for a new character says how to learn", ann.command("recipes"), "You know no recipes yet", "combine")

    # ===== C. gathering refusals before any tool =====
    ann.command("tele " + T + "arena")
    check("skin with no corpse", ann.command("skin"), "There is no corpse here to skin.")
    check("bleed with no corpse", ann.command("bleed"), "There is no corpse here to bleed.")
    check("harvest where nothing grows", ann.command("harvest"), "There is nothing here to harvest.")
    ann.command("spawn tc_wolf")
    check("skinning a living creature is refused", ann.command("skin wolf"), "You can only skin the dead.")
    ann.command("slaynow wolf")
    check("a fresh corpse invites skinning and bleeding", ann.command("look at corpse"), "Its hide could still be taken (skin).", "It is fresh enough to bleed.")
    check("skin without a knife explains what is needed", ann.command("skin corpse"), "You need a skinning knife to skin a corpse.", "carried, not wielded")
    check("bleed without a kit explains what is needed", ann.command("bleed corpse"), "You need a bleeding kit to bleed a corpse.")
    check("nothing was used by a refusal", ann.command("look at corpse"), "Its hide could still be taken")

    # ===== D. corpses: skin and bleed =====
    ann.command(f"mk {O}skinning_knife 1 1")
    ann.command(f"mk {O}bleeding_kit 1 1")
    w.command("goto " + T + "arena")
    check("set the rolls to the middle for exact results", dq(w, "rolls mid"), "rolls=True")
    bob = fresh("bob")
    bob.command("tele " + T + "arena")
    bob.command(f"mk {O}skinning_knife 1 1")
    check("someone else may not skin a fresh kill (the loot lock)", bob.command("skin corpse"), "is not yours to skin yet.")
    check("nor bleed it", bob.command("bleed corpse"), "is not yours to bleed yet.")
    text = act(ann, "skin corpse", *RESULT, settle=GATHER_SETTLE)
    check("skinning is a timed action: the room sees the wind-up", text, "kneels beside the corpse of a test wolf and begins to skin it")
    # mid rolls, skill 0, common knife, wolf difficulty 4 at level 2: work 22, score 30 - 11 = 19: Poor
    check("skinning a wolf gives a wolf skin of the quality the formula says (Poor)", text, "You skin the corpse of a test wolf and take a poor wolf skin (with your common skinning knife).")
    check("the first success raises Skinning to 1", text, "Your Skinning rises to 1 (untrained).")
    check("bystanders see it", bob.read_until("skins"), "Ann skins the corpse of a test wolf.")
    inv = inventory(w, "ann")
    truth("the skin is a generic /std/skin with the kind wolf", count_in(inv, "/std/skin?p.kind=wolf", 0) == 1, inv)
    check("look at the skin", ann.command("look at wolf skin"), "The skin of a wolf", "It is used in crafting.")
    check("a corpse can be skinned only once", ann.command("skin corpse"), "has already been skinned.")
    text = act(ann, "bleed corpse", *RESULT, settle=GATHER_SETTLE)
    check("bleeding a fresh wolf gives wolf blood", text, "You bleed the corpse of a test wolf and take a poor vial of wolf blood")
    # With nothing left in it (skin and blood both taken) the corpse crumbles at once, so it cannot be bled twice for want of a corpse.
    check("a corpse can be bled only once (it crumbles when the last yield is taken)", ann.command("bleed corpse"), "There is no corpse")
    check("the corpse no longer offers either", ann.command("look at corpse"), absent=("could still be taken", "fresh enough"))
    profs, _, _ = pinfo(w, "ann")
    truth("Skinning and Bleeding are saved in tenths (1.0 each)", profs.get("skinning") == 10 and profs.get("bleeding") == 10, profs)

    # A creature with nothing to give.
    ann.command("spawn tc_dummy")
    ann.command("slaynow dummy")
    check("a creature with no skin: refused", ann.command("skin dummy"), "There is no hide worth taking on the corpse of a test dummy.")
    check("a creature with no blood: refused", ann.command("bleed dummy"), "There is no blood worth drawing from the corpse of a test dummy.")
    check("its corpse does not invite either", ann.command("look at corpse of test dummy"), absent=("could still be taken", "fresh enough"))

    # A goblin: blood, no skin.
    ann.command("spawn tc_goblin")
    ann.command("slaynow goblin")
    check("a goblin has no hide", ann.command("skin goblin"), "There is no hide worth taking on the corpse of a test goblin.")
    text = act(ann, "bleed goblin", *RESULT, settle=GATHER_SETTLE)
    check("a goblin's corpse gives goblin blood", text, "vial of goblin blood")
    inv = inventory(w, "ann")
    truth("Goblin Blood is a /std/blood of kind goblin, from bleeding", count_in(inv, "/std/blood?p.kind=goblin") == 1, inv)

    # The loot lock ends; then anyone may.
    w.command("goto " + T + "lockroom")
    bob.command("tele " + T + "lockroom")
    w.command("spawn tc_wolf")
    w.command("slaynow wolf")  # the warden's kill: locked for bob
    check("someone else's kill is locked", bob.command("skin corpse"), "is not yours to skin yet.")
    check("the corpse says so to whoever may not touch it", bob.command("look at corpse"), "only those involved may touch it")
    check("the warden shortens the lock (a level-10 wizard's power)", w.command("ctimers 1 120 3600"), "TIMERS 1")
    time.sleep(1.5)
    text = act(bob, "skin corpse", *RESULT, settle=GATHER_SETTLE)
    check("after the lock anyone may skin", text, "You skin the corpse of a test wolf and take", absent=("not yours",))
    w.command("goto " + T + "arena")

    # Fighting.
    ann.command("spawn tc_post")
    ann.command("kill post")
    time.sleep(0.5)
    ann.command("spawn tc_rabbit")
    ann.command("slaynow rabbit")
    check("no skinning while fighting", ann.command("skin corpse of test rabbit"), "You cannot skin anything while you are fighting.")
    check("no crafting while fighting", ann.command("craft bandage"), "fighting")
    w.command("slay post")
    a.drain(ann, 1.0)
    time.sleep(0.5)
    text = act(ann, "skin corpse of test rabbit", *RESULT, settle=GATHER_SETTLE)
    # mid rolls, skill 2 (two successes), rabbit: work 5 + 1 = 6: score 30 + 2 - 3 = 29: Common
    check("a rabbit skinned by a beginner is Common", text, "take a rabbit skin", absent=("poor rabbit",))

    # Freshness: the test copy sets bleed.fresh_seconds to 20.
    ann.command("spawn tc_wolf")
    ann.command("slaynow wolf")
    stale_at = time.time()

    # ===== E. harvest quality: statistics and deterministic rolls =====
    check("rolls back to random for the statistics", dq(w, "rolls random"), "rolls=True")
    n = 4000
    for prof, tool, work_ in [(0, 1, 22), (50, 1, 22), (50, 3, 22), (100, 1, 48)]:
        counts = [int(x) for x in dq(w, f"hsample {prof} {tool} {work_} {n}").split()]
        odds = sim.harvest_odds(N, prof, tool, work_)
        worst = 0.0
        for t in range(5):
            p = odds[2 + t] / 100.0
            sd = (p * (1 - p) / n) ** 0.5
            worst = max(worst, abs(counts[t] / n - p) / max(sd, 1e-9) if sd > 0 else (0 if counts[t] == round(p * n) else 99))
        truth(f"sampled harvest qualities follow the exact odds within 4.5 standard deviations (prof {prof}, tool {tool}, work {work_})", worst < 4.5, (counts, odds))
        sp = odds[0] / 100.0
        sd = (sp * (1 - sp) / n) ** 0.5
        truth(f"sampled successes follow the odds (prof {prof}, tool {tool})", abs(counts[5] / n - sp) < 4.5 * sd + 1e-9, (counts[5], sp))
    def mean_q(prof, tool, work_):
        o = sim.harvest_odds(N, prof, tool, work_)
        return sum(t * o[2 + t] for t in range(5)) / 100.0
    truth("better tools give better skins", mean_q(30, 0, 22) < mean_q(30, 1, 22) < mean_q(30, 2, 22) < mean_q(30, 3, 22) < mean_q(30, 4, 22))
    truth("more skill gives better skins", mean_q(0, 1, 22) < mean_q(25, 1, 22) < mean_q(50, 1, 22) < mean_q(75, 1, 22) < mean_q(100, 1, 22))
    truth("harder creatures give poorer skins", mean_q(50, 1, 6) > mean_q(50, 1, 22) > mean_q(50, 1, 48))
    truth("better tools succeed more often", sim.harvest_odds(N, 20, 0, 22)[0] < sim.harvest_odds(N, 20, 1, 22)[0] < sim.harvest_odds(N, 20, 4, 22)[0])
    truth("better tools give more bonus units", sim.harvest_odds(N, 20, 1, 22)[1] < sim.harvest_odds(N, 20, 3, 22)[1])
    top = [int(x) for x in dq(w, "hodds 100 5 0").split()]
    truth("Legendary never comes from harvesting: a grandmaster with the best tool gets Masterwork", top[6] == 100000 and len(top) == 7, top)
    counts = [int(x) for x in dq(w, "hsample 100 5 0 2000").split()]
    truth("and the real rolls never give more than Masterwork", counts[4] == 2000, counts)
    # Deterministic end to end: skill 50 Skinning, a Fine knife, mid rolls, a rabbit (work 6): 30 + 50 + 5 - 3 = 82: Excellent.
    check("the warden sets a skill (level 10, trusted probe)", dq(w, "setprof ann skinning 500"), "setprof=True")
    ann.command(f"mk {O}skinning_knife 1 2")
    dq(w, "rolls mid")
    ann.command("spawn tc_rabbit")
    ann.command("slaynow rabbit")
    text = act(ann, "skin corpse of test rabbit", *RESULT, settle=GATHER_SETTLE)
    check("the best knife carried is used automatically, and quality follows skill + tool - difficulty", text, "take an excellent rabbit skin (with your fine skinning knife)")
    profs, _, _ = pinfo(w, "ann")
    # far beneath: gap 50 - 6 = 44 > 30: no growth, and the ladder does not move
    truth("work far beneath your skill teaches nothing (Skinning stays 50.0)", profs.get("skinning") == 500, profs)
    # The growth dice: mid and high always gain a point (roll 0), low never does (roll 100), fail also fails the work itself.
    # Skin one thing with ann; returns (text, skinning in tenths, ladder position of skinning).
    def skin_one(kind):
        ann.command(f"spawn {kind}")
        ann.command("slaynow " + kind.replace("tc_", ""))
        t = act(ann, f"skin corpse of test {kind.replace('tc_', '')}", *RESULT, settle=GATHER_SETTLE)
        pr, _, _ = pinfo(w, "ann")
        return t, pr.get("skinning"), int(dq(w, "ladder ann skinning").split("=")[1])
    lad_pre = int(dq(w, "ladder ann skinning").split("=")[1])
    t, sk, lad0 = skin_one("tc_rabbit")
    truth("work far beneath your skill does not move the ladder", sk == 500 and lad0 == lad_pre, (sk, lad_pre, lad0))
    check("the warden sets a skill of 30", dq(w, "setprof ann skinning 300"), "setprof=True")
    dq(w, "rolls low")
    t, sk, lad1 = skin_one("tc_wolf")
    truth("work near your skill (wolf, work 22): a missed roll teaches nothing", sk == 300, sk)
    truth("and moves the ladder up one rung", lad1 == lad0 + 1, (lad0, lad1))
    t, sk, lad2 = skin_one("tc_rabbit")
    truth("easy work (rabbit, work 6, 24 below) teaches nothing on a miss either", sk == 300 and "rises" not in t, (sk, t))
    truth("but moves the ladder up one rung too", lad2 == lad1 + 1, (lad1, lad2))
    check("the player is never told about the ladder or the chance", ann.command("proficiencies"), "Skinning", absent=("ladder", "chance", "rung", "%"))
    check("the warden sets a skill of 50", dq(w, "setprof ann skinning 500"), "setprof=True")
    t, sk, lad3 = skin_one("tc_rabbit")
    truth("work more than 30 below your skill does not move the ladder", sk == 500 and lad3 == lad2, (sk, lad2, lad3))
    check("the warden sets a skill of 30 again", dq(w, "setprof ann skinning 300"), "setprof=True")
    dq(w, "rolls fail")
    ann.command("spawn tc_rabbit")
    ann.command("slaynow rabbit")
    text = act(ann, "skin corpse of test rabbit", *RESULT, settle=GATHER_SETTLE)
    check("a failed skinning ruins the hide", text, "Your knife slips and the hide of the corpse of a test rabbit is ruined.")
    check("and uses the corpse's one chance", ann.command("skin corpse of test rabbit"), "has already been skinned.")
    profs2, _, _ = pinfo(w, "ann")
    truth("a failure teaches nothing", profs2.get("skinning") == 300, profs2)
    truth("and does not move the ladder", int(dq(w, "ladder ann skinning").split("=")[1]) == lad2, lad2)
    dq(w, "rolls mid")
    t, sk, lad4 = skin_one("tc_rabbit")
    truth("a won roll on easy work gives one whole point (30.0 to 31.0)", sk == 310 and "rises to 31" in t, (sk, t))
    truth("and sends the ladder back to the first rung", lad4 == 0, lad4)
    dq(w, "rolls high")
    ann.command("spawn tc_rabbit")
    ann.command("slaynow rabbit")
    text = act(ann, "skin corpse of test rabbit", *RESULT, settle=GATHER_SETTLE)
    check("a bonus unit (high rolls)", text, "take 2 ", "rabbit skins", "one more than you hoped")
    dq(w, "rolls mid")

    # ===== F. nodes =====
    ann.command("tele " + T + "field")
    check("harvest without a sickle", ann.command("harvest bush"), "You need a harvest sickle to harvest a berry bush.")
    ann.command(f"mk {O}harvest_sickle 1 1")
    w.command("goto " + T + "field")
    check("a node kind with no yields", ann.command("harvest pile"), "You do not know how to gather anything from a pile of stones.")
    text = act(ann, "harvest bush", *RESULT, settle=GATHER_SETTLE)
    check("a berry bush gives berries (mid rolls: the first yield, two units)", text, "You harvest a berry bush and take 2 handfuls of blackberries")
    check("the node lost a charge", w.command("stat bush"), "charges 1/2")
    text = act(ann, "harvest bush", *RESULT, settle=GATHER_SETTLE)
    check("the second charge", text, "You harvest a berry bush and take")
    check("a picked-over bush", ann.command("look"), "picked-over berry bush is here")
    check("a bare node refuses", ann.command("harvest bush"), "has been picked clean. It will grow back in time.")
    dq(w, "rolls fail")
    text = act(ann, "harvest patch", *RESULT, settle=GATHER_SETTLE)
    check("a fumble at a plant", text, "You fumble at a herb patch and come away with nothing.")
    check("costs the time but not the plant", w.command("stat patch"), "charges 30/30")
    dq(w, "rolls mid")
    text = act(ann, "harvest patch", *RESULT, settle=GATHER_SETTLE)
    check("an herb patch gives healing herbs", text, "healing herb")
    time.sleep(max(0, 16 - 2 * GATHER_SETTLE))
    check("the bush has grown back (15 seconds)", ann.command("look"), "berry bush is here", absent=("picked-over",))
    check("harvest of a corpse is refused", (ann.command("tele " + T + "arena"), ann.command("harvest corpse"))[1], "Corpses are not harvested")
    profs, _, _ = pinfo(w, "ann")
    truth("Harvesting rose with use", profs.get("harvesting", 0) >= 20, profs)

    # Freshness, now that more than 20 seconds have passed.
    time.sleep(max(0, 21 - (time.time() - stale_at)))
    ann.command("tele " + T + "arena")
    check("bleeding needs a fresh corpse", ann.command("bleed corpse of test wolf"), "has already gone cold. Bleeding needs a fresh kill")
    check("the trusted probe cannot take a corpse's part directly (only the daemon may)", ann.command("thief"), "THIEF False")

    # ===== G. corpse decay with parts to take =====
    w.command("goto " + T + "decayroom")
    ann.command("tele " + T + "decayroom")
    ann.command("spawn tc_dummy")
    ann.command("slaynow dummy")
    ann.command("spawn tc_wolf")
    ann.command("slaynow wolf")
    check("the warden shortens the empty-corpse timer of both", w.command("ctimers 1800 3 3600"), "TIMERS 2")
    a.drain(ann, 0.2)
    text = a.quiet(ann, 5)
    check("an empty corpse with nothing to take crumbles as before", text, "The corpse of a test dummy crumbles to dust.")
    check("an empty corpse with a hide still to take does not", text + ann.command("look"), "corpse of a test wolf", absent=("test wolf crumbles",))
    act(ann, "skin corpse", *RESULT, settle=GATHER_SETTLE)
    act(ann, "bleed corpse", *RESULT, settle=0)
    text = a.quiet(ann, 5)
    check("once its parts are taken it crumbles like any empty corpse", text, "The corpse of a test wolf crumbles to dust.")

    # ----- creature corpses rot at their lifetime, skin or no skin; a player's keeps its hour -----
    ann.command("spawn tc_wolf")
    ann.command("slaynow wolf")
    check("a creature's corpse lives 600 seconds by default", w.command("stat corpse"), "lifetime 600s")
    ann.command("spawn tc_boar")
    ann.command("slaynow boar")
    check("a creature file may set its own corpse lifetime (90 seconds)", w.command("stat corpse of test boar"), "lifetime 90s")
    w.command("ctimers 1800 120 5")
    check("a corpse with its hide still on it is there before its lifetime ends", ann.command("look"), "corpse of a test wolf", "corpse of a test boar")
    time.sleep(2)
    check("and still there partway", ann.command("look at corpse of test wolf"), "Its hide could still be taken")
    text = a.quiet(ann, 5)
    check("with the hide still on it, the corpse rots at its lifetime", text, "The corpse of a test wolf crumbles to dust.", "The corpse of a test boar crumbles to dust.")
    check("and is gone", ann.command("look"), absent=("corpse of a test wolf",))
    eve = fresh("eve")
    eve.command("tele " + T + "decayroom")
    eve.command(f"mk {O}healing_herb 1 1")
    check("a player dies (the warden's probe)", w.command("slaynow eve"), "SLAIN")
    check("the corpse holds the player's things and keeps its hour", w.command("stat corpse"), "lifetime 3600s")
    w.command("ctimers 1800 3 3")
    text = a.quiet(ann, 6)
    check("a player's corpse does not rot at a creature's lifetime", text + ann.command("look"), "corpse of Eve", absent=("crumbles", "rots away"))

    # ===== H. stations, recipes by teaching, by book =====
    ann.command("tele " + T + "workshop")
    text = ann.command("lessons")
    check("the trainer lists lessons with prices", text, "Old Marta can teach you", "Berry Juice", "5 gold", "Minor Endurance Tonic", "free")
    _, _, gold_before = pinfo(w, "ann")
    text = ann.command("learn berry juice")
    check("learning from a trainer", text, "You learn how to make Berry Juice", "You pay Old Marta 5 gold.")
    _, known, gold_after = pinfo(w, "ann")
    truth("the price was paid and the recipe saved as taught", gold_after == gold_before - 5 and known.get("berry_juice") == "taught", (gold_before, gold_after, known))
    check("learning it again", ann.command("learn berry juice"), "You already know how to make Berry Juice.")
    check("a secret recipe cannot be taught, and the gold comes back", ann.command("learn minor endurance tonic"), "cannot teach you that")
    _, known, gold = pinfo(w, "ann")
    truth("the secret was not learned", "stamina_tonic" not in known and gold == gold_after, (known, gold))
    ann.command(f"mk {O}salve_book 1 1")
    text = ann.command("read primer")
    check("reading a recipe book shows its text and teaches", text, "Crush a healing herb", "You learn how to make Minor Healing Salve")
    check("reading it again teaches nothing new", ann.command("read primer"), "nothing here you do not already know")
    ann.command(f"mk {O}secret_scroll 1 1")
    ann.command("read scroll")
    _, known, _ = pinfo(w, "ann")
    truth("a book cannot teach a secret recipe", "stamina_tonic" not in known, known)
    check("the trusted teaching call refuses a secret", ann.command("teachme stamina_tonic"), "TEACH refused: a secret recipe")
    check("the trusted teaching call teaches", ann.command("teachme simple_bandage"), "TEACH ok")
    check("and says known for a known recipe", ann.command("teachme simple_bandage"), "TEACH known")
    text = ann.command("recipes")
    check("recipes groups by where they are made, with what each needs", text, "Anywhere", "Simple Bandage", "At a cooking fire", "Berry Juice", "2 of any berry", "flask of clean water", "At a herbalist table", "Minor Healing Salve")
    text = ann.command("recipe salve")
    check("recipe NAME in full", text, "Minor Healing Salve", "Needs:", "a healing herb and a wildflower", "Where:  at a herbalist table", "Skill:  Herbalism, novice", "Makes:")
    check("recipe for an unknown name", ann.command("recipe flying carpet"), "You do not know a recipe called 'flying carpet'.")
    check("craft an unknown recipe", ann.command("craft flying carpet"), "You do not know a recipe called")
    check("craft with nothing named", ann.command("craft"), "Craft what?")
    check("craft too many at once", ann.command("craft bandage 51"), "at most 50")

    # ===== I. inheritance: the owner's example, mixed stacks, refusals =====
    ann.command("tele " + T + "hall")
    check("a station recipe away from its station", ann.command("craft salve"), "Minor Healing Salve must be made at a herbalist table, and there is none here.")
    ann.command("tele " + T + "workshop")
    check("missing ingredients are named", ann.command("craft salve"), "To make Minor Healing Salve you still need a wildflower.")
    ann.command(f"mk {O}healing_herb 1 3")   # excellent
    ann.command(f"mk {O}wildflower 1 2")     # fine
    # Use the excellent herb, not the common one from the herb patch: name it.
    text = act(ann, "craft salve using excellent herb", *RESULT)
    # mid rolls: average 2.5, shift (0 - 10) x 0.02 = -0.2: 2.8 rounds to Fine
    check("the owner's example: an Excellent herb and a Fine wildflower give a Fine salve (mid rolls)", text, "You make a fine pot of minor healing salve, using an excellent healing herb and a fine wildflower.")
    odds = [int(x) / 1000 for x in dq(w, "codds 2.5 0 10").split()]
    truth("the owner's example: a high chance of Fine or Excellent (exact odds, a beginner)", odds[2] + odds[3] >= 95, odds)
    dq(w, "rolls random")
    counts = [int(x) for x in dq(w, "csample 2.5 0 10 4000").split()]
    truth("and the real rolls agree: at least 95% Fine or Excellent in 4000 tries", (counts[2] + counts[3]) / 4000 >= 0.95, counts)
    odds = [int(x) / 1000 for x in dq(w, "codds 1 0 45").split()]
    truth("low skill on a hard recipe gives poorer results (mostly Poor from Common)", odds[0] > 80, odds)
    odds = [int(x) / 1000 for x in dq(w, "codds 1 100 10").split()]
    truth("high skill raises quality and can give a bonus output or return an ingredient", odds[2] > 80 and odds[6] > 0 and odds[7] > 0, odds)
    dq(w, "rolls mid")
    # Mixed stacks: default to the lowest quality held.
    ann.command(f"mk {O}healing_herb 1 0")   # poor
    ann.command(f"mk {O}healing_herb 1 4")   # masterwork
    ann.command(f"mk {O}wildflower 3 1")
    text = act(ann, "craft salve", *RESULT)
    check("by default the lowest quality is used (the poor herb), and the output says so", text, "using a poor healing herb and a wildflower.")
    text = act(ann, "craft salve best", *RESULT)
    check("best uses the highest quality", text, "using a masterwork healing herb")
    inv = inventory(w, "ann")
    truth("the common herb from the patch is still there", count_in(inv, O + "healing_herb", 1) == 1, inv)
    # Crafting never destroys materials it does not use, and never fails: a beginner still gets a salve.
    truth("nothing else was taken", count_in(inv, O + "wildflower") == 1, inv)

    # ===== J. discovery by experiment =====
    cat = fresh("cat")
    cat.command(f"mk {O}healing_herb 2 1")
    cat.command(f"mk {O}wildflower 2 1")
    cat.command(f"mk {O}honey 1 1")
    check("combine with nothing named", cat.command("combine"), "Combine what?")
    check("combine something you do not have", cat.command("combine herb and moonstone"), "You have no 'moonstone'.")
    check("combine the same thing twice", cat.command("combine herb and herb"), "You named the same thing twice.")
    cat.command(f"mk {O}leather_wrap 1 1")
    check("combine something that is not a material", cat.command("combine herb and wrap"), "is not something you can craft with")
    text = act(cat, "combine herb and wildflower", *RESULT)
    check("the right mix in the wrong place makes nothing and uses nothing", text, "nothing comes of it. Nothing is used up.")
    inv = inventory(w, "cat")
    truth("nothing was used", count_in(inv, O + "healing_herb") == 2 and count_in(inv, O + "wildflower") == 2, inv)
    cat.command("tele " + T + "workshop")
    text = act(cat, "combine honey and herb", *RESULT)
    check("a mix that is no recipe makes nothing", text, "nothing comes of it. Nothing is used up.")
    text = act(cat, "combine wildflower and herb", *RESULT)
    check("experiment at the right station: the recipe is discovered (order does not matter)", text, "You have discovered a recipe: Minor Healing Salve!")
    check("and the first craft happens", text, "You make a pot of minor healing salve")
    _, known, _ = pinfo(w, "cat")
    truth("a discovered recipe is saved as discovered", known.get("minor_healing_salve") == "discovered", known)
    text = act(cat, "combine herb and wildflower", *RESULT)
    check("combining a known recipe just makes it", text, "You recognise this: it is Minor Healing Salve.", "You make")
    check("recipes shows the discovered recipe", cat.command("recipes"), "Minor Healing Salve")
    # A category ingredient by experiment.
    cat.command(f"mk {O}blueberries 2 1")
    cat.command(f"mk {O}clean_water 1 1")
    text = act(cat, "combine blueberries and water", *RESULT)
    check("a category ingredient: any berry makes Berry Juice (a Known recipe learned by experiment)", text, "You have discovered a recipe: Berry Juice!", "You make a flask of berry juice")
    # The secret: blueberries and honey. Cat has no blueberries left; give some.
    cat.command(f"mk {O}blueberries 1 1")
    text = act(cat, "combine honey and blueberries", *RESULT)
    check("a Secret recipe is found by experiment", text, "You have discovered a recipe: Minor Endurance Tonic!")
    check("the Chronicle records the first discovery of a secret", cat.command("chron craft.discover_secret|stamina_tonic"), "has=True")
    # The Rare recipe with three ingredients, one of them Goblin Blood. Even the trusted test probe cannot make Goblin Blood:
    check("not even trusted code can make Goblin Blood", cat.command("mk /std/blood?p.kind=goblin 1 1"), "MK null")
    cat.command(f"mk {O}bleeding_kit 1 1")
    cat.command("tele " + T + "arena")
    cat.command("spawn tc_goblin")
    cat.command("slaynow goblin")
    act(cat, "bleed goblin", *RESULT, settle=GATHER_SETTLE)
    cat.command("tele " + T + "workshop")
    cat.command(f"mk {O}cloth_strip 1 1")
    cat.command(f"mk {O}healing_herb 1 1")
    check("the Rare recipe has not been discovered by anyone yet", cat.command("chron craft.discover_rare|hunters_poultice"), "has=False")
    text = act(cat, "combine cloth and goblin blood and herb", *RESULT)
    check("a Rare three-ingredient recipe found by experiment", text, "You have discovered a recipe: Hunter's Poultice!", "You make a hunter's poultice")
    text = cat.command("chron craft.discover_rare|hunters_poultice")
    check("the first character to discover a Rare recipe reaches the Chronicle", text, "has=True", "holders=cat ")
    check("with an entry that names her and the recipe", text, "Cat worked out how to make Hunter's Poultice")

    # A wizard makes no history.
    wiz = fresh("wizzy")
    w.command("promote wizzy 1")
    a.drain(wiz, 0.5)
    dq(w, "rolls mid")
    wiz.command("tele " + T + "workshop")
    wiz.command(f"mk {O}blueberries 1 1")
    wiz.command(f"mk {O}clean_water 1 1")
    wiz.command("mk /std/blood?p.kind=wolf 1 1")
    # A recipe nobody has found by experiment: none in the samples is rare apart from the poultice; use the poultice again.
    wiz.command(f"mk {O}healing_herb 1 1")
    wiz.command(f"mk {O}cloth_strip 1 1")
    wiz.command(f"mk {O}bleeding_kit 1 1")
    wiz.command("tele " + T + "arena")
    wiz.command("spawn tc_goblin")
    wiz.command("slaynow goblin")
    act(wiz, "bleed goblin", *RESULT, settle=GATHER_SETTLE)
    wiz.command("tele " + T + "workshop")
    text = act(wiz, "combine herb and goblin blood and cloth", "Wizards make no history", *RESULT)
    check("a wizard who discovers a Rare recipe is told that wizards make no history", text + a.quiet(wiz, 1), "Wizards make no history.")
    check("and the Chronicle still names only Cat", re.sub(r"(?m)^Wizzy \(\d+/\d+\).*$", "", wiz.command("chron craft.discover_rare|hunters_poultice")), "holders=cat ", absent=("wizzy", "Wizzy"))  # the prompt carries the name

    # ===== K. the effects of crafted things =====
    ann.command("tele " + T + "hall")
    ann.command(f"mk {O}berry_juice 1 1")
    ann.command(f"mk {O}berry_juice 1 2")
    ann.command("hurt 20")
    text = act(ann, "drink juice", "recover", "drink")
    check("Berry Juice restores Vitality now (8 at Common)", text, "You recover 8 Vitality.")
    a.quiet(ann, 3)
    ann.command("hurt 20")
    text = act(ann, "drink fine juice", "recover")
    check("a Fine juice restores more (8 x 1.25 = 10)", text, "You recover 10 Vitality.")
    a.quiet(ann, 3)
    ann.command(f"mk {O}simple_bandage 1 1")
    ann.command("hurt 20")
    text = act(ann, "apply bandage", "recover")
    check("apply uses a bandage", text, "You apply a simple bandage.", "You recover 6 Vitality.")
    a.quiet(ann, 3)
    check("drink does not apply a bandage", (ann.command(f"mk {O}simple_bandage 1 1"), ann.command("drink bandage"))[1], "You can't drink that.")
    check("apply does not drink a juice", (ann.command(f"mk {O}berry_juice 1 1"), ann.command("apply juice"))[1], "You can't apply that.")
    ann.command("hurt 29")
    text = act(ann, "apply salve", "Warmth")
    check("a salve starts healing over time (a fine salve: 3 x 1.25, rounded, every 2 seconds)", text, "you will recover 4 Vitality every 2 seconds, 6 times")
    check("score shows the effect", ann.command("score"), "Minor Healing Salve")
    text = a.quiet(ann, 13)
    truth("it heals six times", text.count("soothes you") == 6, text)
    ann.command("tired 0")
    ann.command(f"mk {O}stamina_tonic 1 1")
    text = act(ann, "drink tonic", "Endurance", "drink")
    check("a tonic restores Endurance", text, "You recover 20 Endurance.")
    ann.command(f"mk {O}leather_wrap 1 1")
    check("the wrap can be worn", ann.command("wear wrap"), "leather wrap")
    ann.command("remove wrap")
    ann.command("drop wrap")
    w.command("goto " + T + "hall")
    check("stat shows the carry bonus is recorded and inert", w.command("stat wrap"), "carry bonus 1 (inert)")
    ann.command("get wrap")

    # ===== L. batches, stop, quitting mid-queue, a crash right after a craft =====
    ann.command(f"mk {O}cloth_strip 5 1")
    inv = inventory(w, "ann")
    cloth0, band0 = count_in(inv, O + "cloth_strip"), count_in(inv, O + "simple_bandage")
    text = act(ann, "craft bandage 2", "finish your work", timeout=20)
    check("craft COUNT makes several in a row", text, "You set to work on 2 of Simple Bandage.", "You finish your work: 2 done.")
    inv = inventory(w, "ann")
    truth("two cloth strips became two bandages", count_in(inv, O + "cloth_strip") == cloth0 - 2 and count_in(inv, O + "simple_bandage") == band0 + 2, inv)
    ann.send("craft bandage 3")
    ann.read_until("You make", timeout=10)
    ann.command("stop")
    text = a.quiet(ann, 5)
    inv = inventory(w, "ann")
    truth("stop ends a batch after the current one", count_in(inv, O + "simple_bandage") <= band0 + 4 and "finish your work: 3" not in text, (inv, text))
    total = count_in(inv, O + "simple_bandage") + count_in(inv, O + "cloth_strip")
    truth("and nothing was lost or doubled", total == cloth0 + band0, inv)
    # Quitting mid-queue.
    ann.command(f"mk {O}cloth_strip 4 1")
    inv = inventory(w, "ann")
    before = count_in(inv, O + "simple_bandage") + count_in(inv, O + "cloth_strip")
    ann.send("craft bandage 4")
    ann.read_until("You make", timeout=10)
    ann.send("quit")
    time.sleep(1.5)
    ann = login("ann")
    inv = inventory(w, "ann")
    after = count_in(inv, O + "simple_bandage") + count_in(inv, O + "cloth_strip")
    truth("quitting mid-queue loses nothing and doubles nothing", after == before and count_in(inv, O + "cloth_strip") >= 1, (before, after, inv))
    # A crash right after a craft: the result message comes after the save.
    cloth = count_in(inv, O + "cloth_strip")
    band = count_in(inv, O + "simple_bandage")
    dq(w, "setprof ann herbalism 200")
    dq(w, "rolls low")  # the growth roll misses (the ladder moves) but the work succeeds
    ladder_before = int(dq(w, "ladder ann herbalism").split("=")[1])
    ann.send("craft bandage")
    got = ann.read_until("You make", timeout=10)
    box[0].kill()
    box[0].wait(timeout=10)
    box[1].close()
    box[0], box[1] = a.start_driver(work, mudlib)
    w = login("warden")
    w.command("goto " + T + "hall")  # the warden was last saved before it had a probe: the hall hands it one
    ann = login("ann")
    inv = inventory(w, "ann")
    truth("a driver killed right after a craft: the bandage is there and the cloth strip is gone, once",
          "You make" in got and count_in(inv, O + "simple_bandage") == band + 1 and count_in(inv, O + "cloth_strip") == cloth - 1, (cloth, band, inv))
    truth("the ladder position survives a killed driver (one rung up from the miss)", int(dq(w, "ladder ann herbalism").split("=")[1]) == ladder_before + 1 >= 1, (ladder_before, dq(w, "ladder ann herbalism")))
    profs, known, _ = pinfo(w, "ann")
    truth("proficiencies survive a killed driver", profs.get("skinning", 0) >= 300 and profs.get("harvesting", 0) >= 20, profs)
    truth("known recipes survive a killed driver", known.get("berry_juice") == "taught" and known.get("minor_healing_salve") == "taught", known)

    # ===== M. shops and Goblin Blood =====
    ann.command("tele " + T + "market")
    text = ann.command("browse")
    check("a shop can stock a generic skin by record", text, "A rabbit skin", "A vial of wolf blood")
    check("no shop can stock Goblin Blood", text, absent=("goblin",))
    check("buy a rabbit skin", ann.command("buy rabbit skin"), "You buy a rabbit skin")
    inv = inventory(w, "ann")
    truth("the bought skin is a real rabbit skin (a material)", count_in(inv, "/std/skin?p.kind=rabbit") >= 1, inv)
    check("nor a worthless blood in its place", text, absent=("smear",))

    # ===== N. the sandbox =====
    wiz = login("wizzy")
    wizhome = os.path.join(mudlib, "wiz", "wizzy")
    os.makedirs(wizhome, exist_ok=True)
    for name, src in (("evil", EVIL), ("fakegob", FAKEGOB), ("knife", EVIL_KNIFE), ("table", EVIL_TABLE), ("herb", EVIL_HERB), ("lab", EVIL_ROOM)):
        with open(os.path.join(wizhome, name + ".cs"), "w") as f:
            f.write(src)
    # CHANGED (sandbox): a wizard's own code is cloned and run in their workshop; it reaches the player from there, or not at all.
    wiz.command("goto ~/lab")
    check("a level-1 wizard clones its gadget at home", wiz.command("clone ~/evil"), "Cloned")
    victim = fresh("dan")
    victim.command("tele " + T + "field")
    victim.command("spawn tc_wolf")
    victim.command("slaynow wolf")
    text = wiz.command("evil dan")
    # CHANGED (sandbox): the calls below no longer reach the daemons and the player at all; they return nothing (null, False, 0).
    check("sandbox: cannot teach", text, "teach= learn=")
    check("sandbox: cannot learn for a player", text, "learn=False")
    check("sandbox: cannot set a proficiency", text, "setprof=False")
    check("sandbox: cannot use the wizard's setter", text, "wizset=False")
    check("sandbox: cannot set the ladder position", text, "setladder=False")
    check("sandbox: cannot read the ladder position", text, "readladder=0")
    check("sandbox: cannot use the wizard's ladder query", text, "wizladder=0")
    check("sandbox: cannot fix the rolls", text, "rolls=False")
    check("sandbox: cannot run a crafting command for a player", text, "command=null")
    check("sandbox: cannot take a corpse's part", text, "takeyield=False")
    check("sandbox: cannot strip a trusted node", text, "trygather=False")
    check("sandbox: cannot mint Goblin Blood into a player's hands", text, "mintgoblin=null")
    check("sandbox: cannot make a blood into Goblin Blood", text, "setkind=False")
    check("sandbox: a real herb it makes is only Common (quality refused, so nothing is made)", text, "mintherb=null")
    check("sandbox: cannot start healing over time", text, "heal=False")
    check("sandbox: cannot queue crafting for a player", text, "continue=False")
    check("sandbox: a corpse it sets up from a fake creature yields nothing", text, "fakeyield=[]")
    profs, known, _ = pinfo(w, "dan")
    truth("and the player is unchanged", not profs and not known, (profs, known))
    victim.command(f"mk {O}healing_herb 1 1")
    victim.command(f"mk {O}wildflower 1 1")
    wiz.command("clone ~/knife")
    wiz.command("give knife to dan")
    check("a tool from a wizard's sandbox does not count", victim.command("skin corpse"), "You need a skinning knife")
    wiz.command("clone ~/herb")
    wiz.command("give herb to dan")
    victim.command("teachme minor_healing_salve")
    check("sandbox: a player cannot be teleported into a wizard's workshop", victim.command("tele /wiz/wizzy/lab"), "Only wizards can enter a wizard's workshop.")
    check("a station from a wizard's sandbox does not count", victim.command("craft salve"), "there is none here")
    text = victim.command("combine masterwork herb and wildflower")
    # CHANGED (sandbox): the wizard's herb cannot be given to a player at all, so it never reaches the crafting table.
    check("an item from a wizard's sandbox never reaches a player", text, "You have no 'masterwork herb'.")
    check("no errors in the sandbox section", logged(), absent=("ERROR (dan)",))

    # ===== O. the matcher and the clash rule =====
    for offer, want in [("healing_herb,wildflower", "minor_healing_salve"), ("wildflower,healing_herb", "minor_healing_salve"),
                        ("blackberries,clean_water", "berry_juice"), ("clean_water,blueberries", "berry_juice"),
                        ("blueberries,honey", "stamina_tonic"), ("cloth_strip", "simple_bandage"),
                        ("healing_herb,goblin_blood,cloth_strip", "hunters_poultice"), ("cloth_strip,healing_herb,goblin_blood", "hunters_poultice"),
                        ("goblin_blood,cloth_strip,healing_herb", "hunters_poultice"), ("wolf_skin,cloth_strip", "leather_wrap"),
                        ("healing_herb", ""), ("healing_herb,wildflower,honey", ""), ("honey,clean_water", ""), ("rabbit_skin,cloth_strip", "")]:
        check(f"the matcher: {offer} -> {want or 'nothing'}", dq(w, "match " + offer), f"match={want}" + ("" if want else ""), absent=() if want else ("match=minor", "match=berry", "match=endurance", "match=simple", "match=hunters", "match=leather"))
    clash_src = """// @inherit /std/recipe_set
public class CtClashRecipes : RecipeSet
{
    public override void Create()
    {
        Recipe("salve_twin", "Salve Twin").Needs("wildflower").Needs("healing_herb").At("herbalist table").Skill("herbalism", "novice").Makes("/domains/crafttest/obj/minor_healing_salve");
        Recipe("herb_paste", "Herb Paste").NeedsAny("herb").Needs("wildflower").Skill("herbalism", "simple").Makes("/domains/crafttest/obj/simple_bandage");
        Recipe("berry_honey", "Berry Honey").NeedsAny("berry").Needs("honey").Skill("cooking", "simple").Makes("berry_juice");
        Recipe("black_sweet", "Black Sweet").Needs("blackberries").NeedsAny("sweetener").Skill("cooking", "simple").Makes("berry_juice");
        Recipe("bad_mat", "Bad Material").Needs("unicorn_hair").Skill("cooking", "simple").Makes("berry_juice");
        Recipe("bad_blood", "Bad Blood").Needs("honey").Needs("wolf_blood").Skill("cooking", "simple").Makes("goblin_blood");
        Recipe("too_many", "Too Many").Needs("honey").Needs("wildflower").Needs("cloth_strip").Needs("clean_water").Skill("cooking", "simple").Makes("berry_juice");
        Recipe("bad_station", "Bad Station").Needs("honey").Needs("wildflower").At("anvil").Skill("cooking", "simple").Makes("berry_juice");
    }
}
"""
    h.write(mudlib, "/recipes/zz_clash", clash_src)
    time.sleep(7)  # the daemon looks for new files every five seconds
    log = logged()
    check("a duplicate of an existing recipe is a clash, reported in the driver log", log, "CLASH: recipe 'salve_twin'", "clashes with 'minor_healing_salve'")
    check("two equally exact recipes that could take the same materials clash", log, "CLASH: recipe 'black_sweet'", "'berry_honey'", "blackberries and honey")
    ids = "," + dq(w, "ids") + ","
    truth("the refused recipes are not loaded", ",salve_twin," not in ids and ",black_sweet," not in ids, ids)
    truth("a less exact recipe that overlaps a more exact one is kept", ",herb_paste," in ids and ",berry_honey," in ids, ids)
    check("the more exact recipe wins: herb + wildflower is still the salve", dq(w, "match healing_herb,wildflower"), "match=minor_healing_salve")
    check("blueberries and honey: the exact Secret beats the category recipe", dq(w, "match blueberries,honey"), "match=stamina_tonic")
    check("blackberries and honey: only the category recipe fits", dq(w, "match blackberries,honey"), "match=berry_honey")
    probs = dq(w, "problems")
    check("an unknown material is a load problem", probs, "recipe 'bad_mat'", "unknown material 'unicorn_hair'")
    check("a recipe that would make Goblin Blood is refused", probs, "recipe 'bad_blood'", "can only come from bleeding")
    check("more than three kinds of ingredient is refused", probs, "recipe 'too_many'", "at most 3")
    check("an unknown station is refused", probs, "recipe 'bad_station'", "unknown station 'anvil'")
    os.remove(os.path.join(mudlib, "recipes", "zz_clash.cs"))
    time.sleep(7)
    check("a removed recipe file is noticed", dq(w, "status"), "recipes=6")

    # ===== P. persistence across a clean restart =====
    cat = login("cat")
    profs_before, known_before, _ = pinfo(w, "cat")
    w.send("shutdown")
    box[0].wait(timeout=20)
    box[1].close()
    box[0], box[1] = a.start_driver(work, mudlib)
    w = login("warden")
    w.command("goto " + T + "hall")
    cat = login("cat")
    profs_after, known_after, _ = pinfo(w, "cat")
    truth("proficiencies survive a clean restart", profs_after == profs_before and profs_after.get("herbalism", 0) > 0, (profs_before, profs_after))
    truth("known recipes survive a clean restart, with how they were learned", known_after == known_before and known_after.get("stamina_tonic") == "discovered", (known_before, known_after))
    check("the Chronicle first survives too", cat.command("chron craft.discover_rare|hunters_poultice"), "has=True", "holders=cat ")
    check("proficiencies shows the words", cat.command("proficiencies"), "Herbalism")

    # ===== Q. a thousand recipes =====
    gen_materials(mudlib)
    w.send("shutdown")
    box[0].wait(timeout=20)
    box[1].close()
    start = time.time()
    box[0], box[1] = a.start_driver(work, mudlib)
    TIMINGS["boot_s"] = time.time() - start
    w = login("warden")
    w.command("goto " + T + "hall")
    st = dq(w, "status")
    m = re.search(r"recipes=(\d+) materials=(\d+) files=(\d+) load_ms=(\d+)", st)
    truth("a thousand generated recipes load (plus the six samples)", m is not None and int(m.group(1)) == 1006, st + " " + dq(w, "problems") + " " + dq(w, "clashes"))
    if m:
        TIMINGS["load_ms"] = int(m.group(4))
        TIMINGS["files"] = int(m.group(3))
        truth(f"they load in a sensible time ({m.group(4)} ms for {m.group(3)} files, compiling included)", int(m.group(4)) < 20000, st)
    check("no clashes among the generated recipes", dq(w, "clashes"), absent=("CLASH",))
    offers = "|".join(GEN_OFFERS)
    hits, ms, texts = 0, 0, []
    for _ in range(5):  # 5 x 200: each command is one execution, under the driver's tick limit
        text = dq(w, f"timematch 200 {offers}")
        texts.append(text)
        mm = re.search(r"n=(\d+) hits=(\d+) ms=(\d+)", text)
        if mm:
            hits += int(mm.group(2))
            ms += int(mm.group(3))
    truth("a thousand matches against a thousand recipes all find their recipe", hits == 1000, texts)
    TIMINGS["match_ms_per_1000"] = ms
    truth(f"and take a sensible time ({ms} ms for 1000)", ms < 5000, texts)
    check("a generated three-ingredient recipe", dq(w, "match g1,g2,g3"), "match=gen_t_0")
    check("a generated category recipe", dq(w, "match g5,h5"), "match=gen_c_5")
    check("order does not matter at scale", dq(w, "match g3,g2,g1"), "match=gen_t_0")
    check("the samples still match at scale", dq(w, "match healing_herb,wildflower"), "match=minor_healing_salve")
    log = logged()
    check("the driver logged no errors", "ok" if "ERROR" not in log else log[log.index("ERROR"):][:900], "ok")


GEN_OFFERS = []


def gen_materials(mudlib):
    """300 materials g0..g199 (categories gcat0..gcat9) and h0..h99, and 1000 recipes in 20 files of 50."""
    lines = []
    for i in range(200):
        lines.append(f'        Material("g{i}", "/domains/crafttest/gen/g{i}", "gcat{i % 10}");')
    for i in range(100):
        lines.append(f'        Material("h{i}", "/domains/crafttest/gen/h{i}");')
    h.write(mudlib, "/crafting/gen_materials", "// @inherit /std/craft_data\npublic class CtGenMaterials : CraftData\n{\n    public override void Create()\n    {\n" + "\n".join(lines) + "\n    }\n}\n")
    recipes = []
    pairs = [(x, y) for x in range(200) for y in range(x + 1, 200)]
    for k in range(800):
        x, y = pairs[k * 7 % len(pairs)]
        recipes.append(f'Recipe("gen_p_{k}", "Gen Pair {k}").Needs("g{x}").Needs("g{y}").Skill("cooking", "simple").Makes("g0")')
        if k % 40 == 0:
            GEN_OFFERS.append(f"g{y},g{x}")
    for k in range(100):
        x, y, z = k + 1, k + 2, k + 3
        recipes.append(f'Recipe("gen_t_{k}", "Gen Triple {k}").Needs("g{x}").Needs("g{y}").Needs("g{z}").Skill("tinkering", "novice").Makes("g1")')
        if k % 10 == 0:
            GEN_OFFERS.append(f"g{z},g{x},g{y}")
    for k in range(100):
        recipes.append(f'Recipe("gen_c_{k}", "Gen Category {k}").NeedsAny("gcat{k % 10}").Needs("h{k}").Skill("herbalism", "simple").Makes("g2")')
        if k % 10 == 5:
            GEN_OFFERS.append(f"h{k},g{(k % 10) + 10 * (k // 10)}")
    for f in range(20):
        body = ";\n        ".join(recipes[f * 50:(f + 1) * 50])
        h.write(mudlib, f"/recipes/gen_{f:02d}", f"// @inherit /std/recipe_set\npublic class CtGen{f:02d} : RecipeSet\n{{\n    public override void Create()\n    {{\n        {body};\n    }}\n}}\n")


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-craft-")
    mudlib = os.path.join(work, "mudlib")
    MUDLIB[0] = mudlib
    h.copy_mudlib(mudlib, novice=True)
    # The sample set has its own recipes and materials under the launch ids: take the real launch data out of this copy.
    for gone in ("crafting/launch_materials.cs", "recipes/launch.cs"):
        os.remove(os.path.join(mudlib, gone))
    shutil.copytree(SAMPLES, mudlib, dirs_exist_ok=True)
    for path, source in test_files().items():
        h.write(mudlib, path, source)
    h.edit(mudlib, "/domains/hollow/village/square", 'Exits["south"]', f'Exits["down"] = "{T}hall";\n        Exits["south"]')
    # Bleeding goes cold after 20 seconds in this copy, so the test need not wait five minutes.
    h.edit(mudlib, "/crafting/numbers", 'Number("bleed.fresh_seconds", 300);', 'Number("bleed.fresh_seconds", 20);')
    box = [None, None]
    box[0], box[1] = a.start_driver(work, mudlib)
    started = time.time()
    try:
        run(work, mudlib, box)
        w = h.Client("warden")
        w.read_until("> ")
        w.command("shutdown")
        box[0].wait(timeout=20)
    finally:
        if box[0].poll() is None:
            box[0].kill()
        box[1].close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-5000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\ntimings: {TIMINGS}, whole test {time.time() - started:.0f} s")
    print(f"{CHECKS[0]} checks, {len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
