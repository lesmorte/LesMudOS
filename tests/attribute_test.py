#!/usr/bin/env python3
"""Tests for the attribute engine (docs/ATTRIBUTES.md): the six attributes and their starting values by calling, the two
points a level placed with `improve` (and kept across a login), the maximum Vitality, Endurance and Focus at levels 1, 5
and 10 against the document's table, regeneration by the approved formulas (exact rates, and live), every derived effect
in the rule it enters (weapon damage, the hit roll, critical hits, spell damage, spell healing, Physical Resistance against
slows and staggers, Mental Resistance, Carry Capacity, harvest quality and crafted quality), creature attributes (the
template by level and a creature's own numbers), the equipment hooks, the conversion of a character saved before
attributes existed, and that an object a wizard writes can neither set a character's attributes nor grant points.

Boots the driver on a temporary copy of the mudlib with the crafting sample content laid over it (as tests/craft_test.py
does) and a little test content of its own under /domains/attrtest and /wiz/chief. Chance is either forced (the crafting
daemon's fixed rolls, a 100% critical charm, a creature no blow can reach) or sampled with bounds that cannot flake.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=4880 python3 tests/attribute_test.py
"""
import json
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4833")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402

T = "/domains/attrtest/"
CT = "/domains/crafttest/obj/"
SAMPLES = os.path.join(h.ROOT, "tests", "fixtures", "crafting", "samples")
check = h.check
ATTRS = ["might", "grace", "vigor", "insight", "spirit", "resolve"]
CALLINGS = ["warrior", "scout", "priest", "mage", "adventurer"]

# docs/ATTRIBUTES.md section 3 (decision 1): the starting attributes.
START = {
    "warrior": (8, 5, 7, 5, 5, 6),
    "scout": (5, 9, 5, 6, 5, 6),
    "priest": (5, 5, 6, 6, 9, 5),
    "mage": (5, 5, 5, 9, 7, 5),
    "adventurer": (6, 6, 6, 6, 6, 6),
}
# Sections 2, 8 and 9: class base and per-level amount for Vitality, Endurance and Focus (section 8 changed the Vitality of
# the Warrior, the Scout and the Mage; section 9 the Scout's and the Mage's per-level amounts to 8 and 7).
CLASS = {
    "warrior": ((13, 8), (79, 2), (4, 1)),
    "scout": ((3, 8), (81, 2), (2, 1)),
    "priest": ((7, 7), (23, 1), (75, 4)),
    "mage": ((4, 7), (14, 1), (71, 4)),
    "adventurer": ((10, 8), (50, 2), (50, 2)),
}
# The table of section 2 as sections 8 and 9 revised it: Vitality at 1, 5, 10; Endurance at 1, 10; Focus at 1, 10.
TABLE = {
    "warrior": ((35, 67, 107), (100, 118), (20, 29)),
    "scout": ((21, 53, 93), (100, 118), (20, 29)),
    "priest": ((26, 54, 89), (40, 49), (100, 136)),
    "mage": ((21, 49, 84), (30, 39), (100, 136)),
    "adventurer": ((30, 62, 102), (70, 88), (70, 88)),
}
# Section 2, regeneration per tick at the start (Vitality, Endurance, Focus), with section 8's Focus rate (0.15% a point).
REGEN = {
    "warrior": (1.0, 9.9, 0.5),
    "scout": (0.8, 9.3, 0.53),
    "priest": (0.9, 3.72, 3.25),
    "mage": (0.8, 2.7, 3.4),
    "adventurer": (0.9, 6.72, 1.96),
}


def formula(calling, level, attrs):
    """(Vitality, Endurance, Focus) by the formulas of section 2."""
    m, g, v, i, s, r = attrs
    (vb, vp), (eb, ep), (fb, fp) = CLASS[calling]
    return vb + vp * level + v * 2, eb + ep * level + r * 2 + v, fb + fp * level + i * 2 + s


def regen(calling, attrs):
    m, g, v, i, s, r = attrs
    _, end, foc = formula(calling, 1, attrs)
    return 0.3 + v / 10, end * (0.06 + (r + v) * 0.003), foc * (0.01 + (i + s) * 0.0015)


# ---------------------------------------------------------------- test content

def room(cls, short, exits, body="", safe=False, extra=""):
    ex = "".join(f'        Exits["{d}"] = "{p}";\n' for d, p in exits.items())
    return f"""// @inherit /std/room
public class {cls} : Room
{{
    public override void Create()
    {{
        ShortDesc = "{short}";
        LongDesc = "A room made for the attribute test.";
        Safe = {"true" if safe else "false"};
{ex}{body}    }}
{extra}}}
"""


HALL_INIT = """    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who != null && Interactive(who) && Present("probe", who) == null) MoveObject(CloneObject("/domains/attrtest/probe"), who);
    }
"""


def creature(cls, name, ident, level, hp, attrs="", dmg=(1, 1), tohit=-40, dodge=0):
    return f"""// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); ShortDesc = "a {name.lower()}"; LongDesc = "A creature made for the attribute test.";
        Level = {level}; MaxVitality = {hp}; DamageMin = {dmg[0]}; DamageMax = {dmg[1]}; ToHit = {tohit}; Armor = 0; Dodge = {dodge}; XpValue = 0;
        BaseReaction = 3.0;
{attrs}    }}
}}
"""


# A trusted probe (it lives under /domains): what a player cannot do by command, for the test only.
PROBE = r'''// @inherit /std/thing
public class AtProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    private ObjectRef D => LoadObject("/secure/crafting");
    public override void Init()
    {
        foreach (var v in new[] { "atele", "aspawn", "ahurt", "aset", "aslow", "astagger", "astat", "amk", "dq", "teachme" })
            AddAction(v, Do);
    }
    private static string F(double d) => ((long)Math.Round(d * 1000)).ToString();
    private ObjectRef? Named(string name, ObjectRef room) =>
        Users().FirstOrDefault(u => u.Call<string>("QueryName")!.ToLowerInvariant() == name) ?? Present(name, room);
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var room = Environment(me)!;
        switch (QueryVerb())
        {
            case "atele": MoveObject(me, LoadObject(args.Trim())); Write("TELE ok\n"); return true;
            case "aspawn": { var ob = CloneObject("/domains/attrtest/" + w[0]); MoveObject(ob, room); Write("SPAWNED " + ob.Name + "\n"); return true; }
            case "ahurt": me.Call("Heal", 99999); me.Call("TakeDamage", int.Parse(w[0]), null); Write("HURT " + me.Call<int>("QueryVitality") + "/" + me.Call<int>("QueryMaxVitality") + "\n"); return true;
            case "aset": me.Call("SetResources", int.Parse(w[0]), int.Parse(w[1])); Write("SET " + me.Call<int>("QueryEndurance") + " " + me.Call<int>("QueryFocus") + "\n"); return true;
            case "aslow":
            {
                // aslow N: N slows from a creature's blow; how many the character shook off.
                var n = int.Parse(w[0]);
                var resisted = 0;
                for (var i = 0; i < n; i++)
                {
                    me.Call("RemoveEffect", "atest-slow");
                    me.Call("AddSpeedEffect", "atest-slow", 1.5, 30);
                    if (!me.Call<bool>("QueryHasEffect", "atest-slow")) resisted++;
                }
                me.Call("RemoveEffect", "atest-slow");
                Write("SLOW resisted=" + resisted + " of " + n + "\n");
                return true;
            }
            case "astagger": { for (var i = 0; i < int.Parse(w[0]); i++) me.Call("Stagger", 0.0, Self); Write("STAGGERED\n"); return true; }
            case "astat":
            {
                // astat NAME: the rule values of a player or a creature here, in thousandths.
                var who = w.Length > 0 ? Named(w[0], room) : me;
                if (who == null) { Write("ASTAT none\n"); return true; }
                var keys = new[] { "physical_power", "precision", "evasion", "crit", "spell_power", "healing_power", "mental_resistance", "physical_resistance", "carry_capacity", "gathering", "crafting" };
                Write("ASTAT " + string.Join(" ", keys.Select(k => k + "=" + F(who.Call<double>("QueryStat", k)))) +
                      " " + string.Join(" ", new[] { "might", "grace", "vigor", "insight", "spirit", "resolve" }.Select(k => k + "=" + who.Call<int>("QueryAttribute", k))) +
                      " critchance=" + F(who.Call<double>("QueryCrit")) + " attack=" + who.Call<int>("QueryAttack") + " defense=" + who.Call<int>("QueryDefense") +
                      " vregen=" + F(who.Call<double>("QueryVitalityRegen")) + " eregen=" + F(who.Call<double>("QueryEnduranceRegen")) + " fregen=" + F(who.Call<double>("QueryFocusRegen")) +
                      " maxhp=" + who.Call<int>("QueryMaxVitality") + " maxend=" + who.Call<int>("QueryMaxEndurance") + " maxfoc=" + who.Call<int>("QueryMaxFocus") +
                      " points=" + who.Call<int>("QueryAttributePoints") + "\n");
                return true;
            }
            case "amk": { var made = CreateIn(w[0], int.Parse(w[1]), int.Parse(w[2]), me); Write("MK " + (made == null ? "null" : made.Call<string>("Short")) + "\n"); return true; }
            case "teachme": Write("TEACH " + D.Call<string>("Teach", me, w[0]) + "\n"); return true;
            case "dq":
            {
                switch (w[0])
                {
                    case "rolls": Write("DQ rolls=" + D.Call<bool>("SetTestRolls", w[1]) + "\n"); return true;
                    case "setprof":
                    {
                        var who = Users().FirstOrDefault(u => u.Call<string>("QueryName")!.ToLowerInvariant() == w[1]);
                        Write("DQ setprof=" + (who != null && D.Call<bool>("WizardSetProficiency", who, w[2], int.Parse(w[3]))) + "\n");
                        return true;
                    }
                    case "info": { var i = D.Call<Dictionary<string, string>>("QueryRecipeInfo", w[1]); Write("DQ " + (i == null ? "null" : string.Join(";", i.Select(k => k.Key + "=" + k.Value))) + "\n"); return true; }
                    case "hodds": Write("DQ " + string.Join(" ", D.Call<List<double>>("HarvestOdds", double.Parse(w[1]), int.Parse(w[2]), double.Parse(w[3]), double.Parse(w[4]))!.Select(F)) + "\n"); return true;
                }
                Write("DQ ?\n");
                return true;
            }
        }
        return false;
    }
}
'''

FLATBLADE = """// @inherit /std/weapon
public class AtFlatblade : Weapon
{
    public override void Create()
    {
        Name = "Flat Blade"; Ids.Add("blade"); ShortDesc = "a flat blade"; LongDesc = "It always strikes for ten.";
        DamageMin = 10; DamageMax = 10; Recovery = 0.5; Value = 1;
    }
}
"""

# The equipment hooks: +4 Grace and +95% Critical Chance while worn.
CHARM = """// @inherit /std/armor
public class AtCharm : Armor
{
    public override void Create()
    {
        Name = "Keen Charm"; Ids.Add("charm"); ShortDesc = "a keen charm"; LongDesc = "It makes every blow tell.";
        Slot = "trinket"; ArmorValue = 0; Value = 1;
        AttributeBonus["grace"] = 4;
        StatBonus["crit"] = 95;
    }
}
"""

# The Evasion hook: +50 Evasion while worn.
VEIL = """// @inherit /std/armor
public class AtVeil : Armor
{
    public override void Create()
    {
        Name = "Shadow Veil"; Ids.Add("veil"); ShortDesc = "a shadow veil"; LongDesc = "Blows slide off it.";
        Slot = "head"; ArmorValue = 0; Value = 1;
        StatBonus["evasion"] = 50;
    }
}
"""

# A wizard's sandbox object that tries every public door to a character's attributes.
MEDDLER = r'''// @inherit /std/thing
public class Meddler : Thing
{
    public override void Create() { Name = "meddler"; Ids.Add("meddler"); }
    public override string? Short() => "a meddler";
    public override void Init() { AddAction("meddle", Meddle); }
    private bool Meddle(string args)
    {
        var who = Present(args.Trim(), Environment(ThisPlayer()!)!);
        if (who == null) { Write("MEDDLE none\n"); return true; }
        var tries = new List<string>();
        foreach (var call in new (string f, object[] a)[] {
            ("SetAttribute", new object[] { "might", 50 }), ("SetAttributes", new object[] { new Dictionary<string, int> { ["might"] = 50 } }),
            ("AddAttributePoints", new object[] { 20 }), ("GrantAttributePoints", new object[] { 20 }), ("EnsureAttributes", new object[] { Self }),
            ("CmdImprove", new object[] { "might" }), ("Improve", new object[] { "might" }), ("ImproveAttribute", new object[] { "might", 5 }) })
        {
            try { tries.Add(call.f + "=" + (who.Call(call.f, call.a) ?? "null")); }
            catch (MudRuntimeException e) { tries.Add(call.f + "!" + e.Message.Length); }
        }
        // A timed effect is open to any object; it must not move an attribute.
        who.Call("AddEffect", "meddle", "Meddling", 600, "might=50;grace=50;vigor=50;insight=50;spirit=50;resolve=50", 1.0, 0);
        Write("MEDDLE " + string.Join(" ", tries) + " might=" + who.Call<int>("QueryAttribute", "might") + " points=" + who.Call<int>("QueryAttributePoints") + "\n");
        return true;
    }
}
'''


def test_files():
    f = {}
    f[T + "probe"] = PROBE
    f[T + "flatblade"] = FLATBLADE
    f[T + "charm"] = CHARM
    f[T + "veil"] = VEIL
    f[T + "striker"] = creature("AtStriker", "Striker", "striker", 1, 50000, "        BaseReaction = 1.0;\n", tohit=10)
    # A creature no blow can miss, one no blow can reach, the level template, a mighty one and a stalwart one.
    f[T + "dummy"] = creature("AtDummy", "Dummy", "dummy", 1, 50000)
    f[T + "wisp"] = creature("AtWisp", "Wisp", "wisp", 1, 50000, '        Attributes["grace"] = 205;\n')
    f[T + "elder"] = creature("AtElder", "Elder", "elder", 10, 50000)
    f[T + "brute"] = creature("AtBrute", "Brute", "brute", 10, 50000, '        Attributes["might"] = 15; Attributes["grace"] = 13;\n')
    f[T + "stalwart"] = creature("AtStalwart", "Stalwart", "stalwart", 3, 50000, '        Attributes["vigor"] = 30; Attributes["resolve"] = 30; Attributes["spirit"] = 30;\n')
    f[T + "hall"] = room("AtHall", "Attribute Hall", {"up": "/domains/hollow/village/square", "down": T + "arena", "west": T + "field"}, "", True, HALL_INIT)
    f[T + "arena"] = room("AtArena", "Attribute Arena", {"up": T + "hall"})
    f[T + "field"] = room("AtField", "Attribute Field", {"east": T + "hall"}, '        AddNode("/domains/crafttest/obj/herb_patch");\n')
    return f


# ---------------------------------------------------------------- helpers

def astat(c, who=""):
    text = c.command(("astat " + who).strip())
    m = re.search(r"ASTAT ([^\r\n]*)", text)
    if not m:
        return {}
    return dict((k, int(v)) for k, v in (p.split("=") for p in m.group(1).split()))


def dq(c, rest):
    text = c.command("dq " + rest)
    m = re.search(r"DQ ([^\r\n]*)", text)
    return m.group(1) if m else text


def attr_line(values):
    return "   ".join(f"{n.capitalize()} {v}" for n, v in zip(ATTRS, values))


def resources(c):
    """(vitality, max, endurance, max, focus, max) from the vitality command."""
    for _ in range(4):
        t = c.command("vitality")
        m = re.search(r"Vitality (\d+)/(\d+)   Endurance (\d+)/(\d+)   Focus (\d+)/(\d+)", t)
        if m:
            return tuple(int(x) for x in m.groups())
        time.sleep(0.3)
    return (None,) * 6


def login(name, calling, wiz=None):
    c = h.Client(name, archetype=calling)
    c.read_until("> ")
    if wiz is not None:
        wiz.command(f"promote {name} 10")
        c.read_until("level is now")
    a.drain(c, 0.4)
    c.command("goto " + T + "hall") if wiz is not None else None
    return c


def hits(text):
    return [int(x) for x in re.findall(r"\(\+(\d+) damage\)", text)]


def fight_for(c, target, seconds):
    c.command("kill " + target)
    data = a.quiet(c, seconds)
    c.send("restore")
    return data


def end_fight(c, wiz, target):
    wiz.command("slay " + target)
    a.drain(c, 0.5)


# ---------------------------------------------------------------- the run

def run(work, mudlib):
    chief = h.Client("chief", archetype="warrior")
    chief.read_until("> ")
    check("the secret makes the first wizard", chief.command("promote " + h.SECRET), "level 10 wizard")
    chief.command("goto " + T + "hall")
    a.drain(chief, 0.4)

    # ===== 1. starting attributes, level 1 resources and score, by calling (plain players) =====
    plain = {}
    for calling in CALLINGS:
        c = h.Client("p" + calling[:6], archetype=calling)
        c.read_until("> ")
        a.drain(c, 0.3)
        plain[calling] = c
        s = c.command("score")
        check(f"{calling}: starting attributes (decision 1)", s, attr_line(START[calling]))
        hp, mhp, st, mst, ma, mma = resources(c)
        (v1, _, _), (e1, _), (f1, _) = TABLE[calling]
        check(f"{calling}: level 1 Vitality, Endurance and Focus as the table says ({v1}, {e1}, {f1})", f"{mhp} {mst} {mma}", f"{v1} {e1} {f1}")
        check(f"{calling}: and the formula gives the same", str(formula(calling, 1, START[calling])), str((v1, e1, f1)))
        check(f"{calling}: score shows Carry Capacity (50 + Might x 5)", s, f"Carry Capacity {50 + 5 * START[calling][0]} lb")
        m, g, v, i, sp, r = START[calling]
        check(f"{calling}: score shows the brief's statistics", s, f"Physical Power {m * 2}", f"Precision {g * 2}", f"Evasion {g}",
              f"Spell Power {i * 2 + sp}", f"Healing Power {sp * 2 + i}", f"Mental Resistance {sp + r}", f"Physical Resistance {v + r}",
              f"Gathering Efficiency {g + i}", f"Crafting Aptitude {i + r}")
        check(f"{calling}: no points to place at level 1", c.command("improve might"), "no attribute points to place", absent=("You place",))
        check(f"{calling}: score shows no points", s, absent=("Attribute points to place",))

    # The level 1 derived effects that score shows: to hit, defense, critical, damage.
    s = plain["scout"].command("score")
    check("scout: Grace 9 is no Precision yet (8 points above 5 make one), no Evasion, and 6% critical (unarmed, level 1)",
          plain["scout"].command("unwield") + plain["scout"].command("score"), "To hit +1", "Defense 0", "Critical 6%")
    check("adventurer: Grace 6 is 5.25% critical", plain["adventurer"].command("score"), "Critical 5.25%", "Defense 0")
    check("warrior: Might 8 is +6% damage on the score's damage line", plain["warrior"].command("score"), "Damage 9-14 (+6%)", "Critical 5%")
    check("mage: no damage bonus, 5% critical", plain["mage"].command("score"), "Critical 5%", absent=("(+",))

    # ===== 2. resources at levels 5 and 10, and the points they bring =====
    wizzes = {}
    for calling in CALLINGS:
        c = plain[calling]
        chief.command(f"promote p{calling[:6]} 10")
        c.read_until("level is now")
        a.drain(c, 0.3)
        c.command("goto " + T + "hall")
        a.drain(c, 0.3)
        wizzes[calling] = c
        st = astat(c)
        rv, re_, rf = REGEN[calling]
        check(f"{calling}: regeneration per tick at the start (Vitality, Endurance, Focus) as section 2 says",
              f"{st['vregen'] / 1000:.2f} {st['eregen'] / 1000:.2f} {st['fregen'] / 1000:.2f}", f"{rv:.2f} {re_:.2f} {rf:.2f}")
        want = regen(calling, START[calling])
        check(f"{calling}: and exactly by the formulas", f"{st['vregen']} {st['eregen']} {st['fregen']}", " ".join(str(int(round(x * 1000))) for x in want))
        check(f"{calling}: stat shows the regeneration", c.command("stat me"), "Regeneration per 10 s: Vitality")
        (v1, v5, v10), (e1, e10), (f1, f10) = TABLE[calling]
        c.command("setlevel 5")
        _, mhp, _, mst, _, mma = resources(c)
        check(f"{calling}: level 5 Vitality {v5} (the table), Endurance and Focus by the formula", f"{mhp} {mst} {mma}",
              " ".join(str(x) for x in (v5,) + formula(calling, 5, START[calling])[1:]))
        check(f"{calling}: level 5 has 8 points to place", c.command("score"), "Attribute points to place: 8")
        c.command("setlevel 10")
        _, mhp, _, mst, _, mma = resources(c)
        check(f"{calling}: level 10 Vitality {v10}, Endurance {e10}, Focus {f10} (the table)", f"{mhp} {mst} {mma}", f"{v10} {e10} {f10}")
        check(f"{calling}: level 10 has 18 points to place", c.command("score"), "Attribute points to place: 18")
        check(f"{calling}: improve with no argument lists the attributes and points", c.command("improve"), attr_line(START[calling]), "18 points to place")

    # ===== 3. improve =====
    w = wizzes["warrior"]
    _, mhp0, _, mst0, _, _ = resources(w)
    check("improve vigor 3", w.command("improve vigor 3"), "You place 3 points in Vigor. Vigor is now 10; 15 points left to place.")
    _, mhp, _, mst, _, _ = resources(w)
    check("Vigor is worth 2 Vitality and 1 Endurance a point", f"{mhp - mhp0} {mst - mst0}", "6 3")
    check("a prefix of three letters is enough", w.command("improve res"), "You place 1 point in Resolve. Resolve is now 7; 14 points left")
    check("an unknown attribute is refused", w.command("improve wisdom"), "Improve which?")
    check("two letters are not enough", w.command("improve gr"), "Improve which?")
    check("more points than there are is refused", w.command("improve might 99"), "You have only 14 points to place.")
    check("a bad number is refused", w.command("improve might zero"), "Usage")
    st = astat(w)
    check("points left: 14", str(st["points"]), "14")
    w.command("save")
    w.command("quit")
    time.sleep(0.5)
    w = h.Client("pwarrio", archetype="warrior")
    w.read_until("> ")
    a.drain(w, 0.3)
    check("attributes and points are kept across a login", w.command("score"), "Vigor 10", "Resolve 7", "Attribute points to place: 14")
    w.command("goto " + T + "hall")
    a.drain(w, 0.3)
    wizzes["warrior"] = w

    # ===== 4. regeneration, live =====
    mg = wizzes["mage"]
    mg.command("setlevel 1")
    mg.command("atele " + T + "arena")
    a.drain(mg, 0.3)
    mg.command("aset 0 0")
    t0 = time.time()
    time.sleep(20.5)
    _, _, st, _, ma, _ = resources(mg)
    t = time.time() - t0
    check("mage Focus comes back at 3.4 per 10 s", str(int(3.4 * (t - 1.5) / 10) <= ma <= int(3.4 * (t + 1.5) / 10) + 1), "True")
    check("mage Endurance at 2.7 per 10 s", str(int(2.7 * (t - 1.5) / 10) <= st <= int(2.7 * (t + 1.5) / 10) + 1), "True")
    wr = wizzes["warrior"]
    wr.command("setlevel 1")
    wr.command("atele " + T + "arena")
    wr.command("ahurt 20")
    hp0 = resources(wr)[0]
    time.sleep(31)
    gained = resources(wr)[0] - hp0
    check("warrior Vitality comes back at 1.0 per 10 s (Vigor 10 now: 1.3; three ticks)", str(2 <= gained <= 5), "True")

    # ===== 5. Physical Power in weapon damage =====
    chief.command("goto " + T + "arena")
    a.drain(chief, 0.3)
    wr.command("setlevel 10")
    wr.command("amk " + T + "flatblade 1 1")
    wr.command("wield blade")
    wr.command("verbose on")
    wr.command("aspawn dummy")
    data = fight_for(wr, "dummy", 21)
    got = set(hits(data))
    check("Might 8: a 10-point blade hits for 11 (10 x 1.06), a critical for 17", str(bool(got) and got <= {11, 17}), "True")
    end_fight(wr, chief, "dummy")
    wr.command("improve might 9")
    check("Might 17 shows +24%", wr.command("score"), "Damage 10-10 (+24%)")
    wr.command("aspawn dummy")
    data = fight_for(wr, "dummy", 21)
    got = set(hits(data))
    check("Might 17: the blade hits for 12 (10 x 1.24), a critical for 18", str(bool(got) and got <= {12, 18}), "True")
    end_fight(wr, chief, "dummy")

    # ===== 6. Critical Chance and the equipment hooks =====
    wr.command("amk " + T + "charm 1 1")
    wr.command("wear charm")
    st = astat(wr)
    check("a worn charm adds 4 Grace (5 -> 9) and its +95% critical: the chance is 100", f"{st['grace']} {st['critchance']}", "9 100000")
    check("score shows the Grace with the worn bonus and the base", wr.command("score"), "Grace 9 (5)", "Critical 100%")
    wr.command("aspawn dummy")
    data = fight_for(wr, "dummy", 12)
    n_hits = len(re.findall(r"You (?:scratch|hit|smash)", data))
    n_crit = data.count("A critical hit!")
    check("with the charm every hit is critical", f"{n_hits > 0} {n_hits == n_crit}", "True True")
    end_fight(wr, chief, "dummy")
    wr.command("remove charm")
    st = astat(wr)
    check("removed, the charm adds nothing", f"{st['grace']} {st['critchance']}", "5 5000")
    sc = wizzes["scout"]
    st = astat(sc)
    check("scout: Critical Chance from Grace 9 is +1% (0.25% a point): 6% in all", f"{st['crit']} {st['critchance']}", "1000 6000")

    # ===== 7. Precision and Evasion in the hit roll =====
    st = astat(sc)
    check("scout level 10: Grace 9 gives no Precision and no Evasion", f"{st['precision']} {st['evasion']} {st['defense']}", "0 0 9")
    sc.command("improve grace 8")
    st = astat(sc)
    check("Grace 17: +1 to hit (12 points above 5), still no Evasion", f"{st['precision']} {st['evasion']} {st['defense']}", "1000 0 9")
    sc.command("atele " + T + "arena")
    sc.command("aspawn wisp")
    st = astat(sc, "wisp")
    check("a creature with Grace 205: Precision 25 enters its attack, Grace gives it no Evasion", f"{st['precision']} {st['attack']} {st['evasion']} {st['defense']}", "25000 -14 0 0")
    sc.command("amk " + T + "veil 1 1")
    sc.command("wear veil")
    st = astat(sc)
    check("a worn veil with +50 Evasion puts 50 on the scout's side of the hit roll", f"{st['evasion']} {st['defense']}", "50000 59")
    sc.command("aspawn striker")
    sc.command("unwield")
    data = fight_for(sc, "striker", 36)
    landed = len(re.findall(r"(?:scratches|hits|smashes) you", data))
    missed = data.count("misses you")
    check("so the striker reaches the scout only on a natural 20", f"{landed + missed >= 6} {landed <= 2}", "True True")
    end_fight(sc, chief, "striker")
    sc.command("remove veil")
    chief.command("slay wisp")

    # ===== 8. Spell Power and Healing Power =====
    mg.command("setlevel 10")
    mg.command("aspawn dummy")
    mg.command("verbose on")
    bolts = []
    for _ in range(5):
        mg.command("restore")
        mg.send("bolt dummy")
        bolts += [int(x) for x in re.findall(r"strikes a dummy for (\d+) damage", a.quiet(mg, 7.2))]
    check("Insight 9 and Spirit 7: Spell Power +10%, an Arcane Bolt of 6-9 does 7-10", str(bool(bolts) and set(bolts) <= {7, 8, 9, 10}), "True")
    mg.command("improve insight 18")
    st = astat(mg)
    check("Insight 27: Spell Power 2 x 22 + 2 = 46%", str(st["spell_power"]), "46000")
    bolts = []
    for _ in range(6):
        mg.command("restore")
        mg.send("bolt dummy")
        bolts += [int(x) for x in re.findall(r"strikes a dummy for (\d+) damage", a.quiet(mg, 7.2))]
    check("now 6-9 becomes 9, 10, 12 or 13", str(bool(bolts) and set(bolts) <= {9, 10, 12, 13}), "True")
    end_fight(mg, chief, "dummy")
    pr = wizzes["priest"]
    pr.command("setlevel 1")
    pr.command("atele " + T + "arena")
    pr.command("ahurt 15")
    out = a.quiet(pr, 0.1) + (pr.send("heal") or "") + pr.read_until("Vitality", timeout=12)
    check("Spirit 9 and Insight 6: Healing Power +9% with Divine Knowledge's 25%: Minor Heal restores 9 (7 x 1.34; 7 is provisional, docs/specs/skill-ranks.md)", out, "restoring 9 Vitality")
    a.drain(pr, 3.5)
    pr.command("setlevel 10")
    pr.command("improve spirit 18")
    pr.command("ahurt 60")
    out = (pr.send("heal") or "") + pr.read_until("Vitality", timeout=12)
    check("Spirit 27: +44% +1% +25%: Minor Heal restores 12 (7 x 1.70, rank I: no rank was bought)", out, "restoring 12 Vitality")
    a.drain(pr, 3.5)
    pr.command("restore")

    # ===== 9. Physical and Mental Resistance =====
    st = astat(wizzes["warrior"])
    check("warrior: Physical Resistance 2% a point of Vigor and Resolve above 5 (now 10 and 7: 14%)", str(st["physical_resistance"]), "14000")
    st = astat(pr)
    check("priest: Mental Resistance (Spirit 27 and Resolve 5: 44%)", str(st["mental_resistance"]), "44000")
    wr.command("setlevel 40")
    wr.command("improve vigor 30")
    wr.command("improve resolve 30")
    st = astat(wr)
    check("Physical Resistance stops at 75%", str(st["physical_resistance"]), "75000")
    out = wr.command("aslow 400")
    n = int(re.search(r"resisted=(\d+)", out).group(1)) if "resisted=" in out else -1
    check("at 75% three slows in four are shaken off (400 tries: 260 to 340)", str(260 <= n <= 340), "True")
    out = mg.command("aslow 60")
    check("at 0% none is", out, "resisted=0 of 60")
    wr.command("atele " + T + "arena")
    out = wr.command("astagger 40") + a.quiet(wr, 0.5)
    check("staggers are resisted too, not always", f"{'keeps their footing' in out} {'is staggered' in out}", "True True")
    out = wr.command("speed me -30 20")
    check("the wizard's speed command is never resisted", wr.command("score"), "Slow")
    wr.command("speed me 0")
    wr.command("aspawn stalwart")
    st = astat(wr, "stalwart")
    check("a creature's own Vigor, Spirit and Resolve of 30: Physical and Mental Resistance 75% (the cap)",
          f"{st['physical_resistance']} {st['mental_resistance']}", "75000 75000")
    wr.command("aspawn elder")
    st = astat(wr, "elder")
    check("a level 10 creature with no attributes of its own has 4 + 10 / 2 = 9 in each", " ".join(str(st[x]) for x in ATTRS), "9 9 9 9 9 9")
    check("so its blows are 8% harder, its Grace gives no Precision or Evasion yet, it crits 1% more, and resists 16%",
          f"{st['physical_power']} {st['precision']} {st['evasion']} {st['crit']} {st['physical_resistance']}", "8000 0 0 1000 16000")
    wr.command("aspawn brute")
    st = astat(wr, "brute")
    check("a creature file may set its own: Might 15 is +20% damage, Grace 13 is +1 to hit", f"{st['might']} {st['physical_power']} {st['precision']}", "15 20000 1000")
    check("stat shows a creature's attributes", wr.command("stat brute"), "Attributes might 15, grace 13, vigor 9")
    out = wr.command("stat stalwart")
    check("stat shows the resistances", out, "physical resistance 75%")

    # ===== 10. Gathering Efficiency and Crafting Aptitude =====
    check("fixed rolls for the crafting checks", dq(chief, "rolls mid"), "rolls=True")
    gatherers = {"warrior": wizzes["warrior"], "scout": h.Client("gscout", archetype="scout")}
    gatherers["scout"].read_until("> ")
    a.drain(gatherers["scout"], 0.3)
    gatherers["scout"].command("down")
    results = {}
    for calling, c in gatherers.items():
        name = "pwarrio" if calling == "warrior" else "gscout"
        c.command("atele " + T + "field")
        c.command(f"amk {CT}harvest_sickle 1 1")
        check(f"{calling}: harvesting set to 20", dq(chief, f"setprof {name} harvesting 200"), "setprof=True")
        c.send("harvest patch")
        text = c.read_any(("You harvest", "fumble", "need", "nothing"), timeout=20) + a.quiet(c, 0.5)
        results[calling] = text
    st_w, st_s = astat(gatherers["warrior"]), astat(gatherers["scout"])
    check("Gathering Efficiency: warrior (Grace 5 now, Insight 5) 0, scout (Grace 9, Insight 6) +5", f"{st_w['gathering']} {st_s['gathering']}", "0 5000")
    check("warrior: score 30 + 20 - 2.5 = 47.5, a common herb", results["warrior"], "You harvest a herb patch and take", "healing herb", absent=("fine",))
    check("scout: +5 makes 52.5, a fine herb", results["scout"], "You harvest a herb patch and take", "fine healing herb")
    info = dq(chief, "info simple_bandage")
    work_level = int(float(re.search(r"work=([\d.]+)", info).group(1))) if "work=" in info else 0
    st_w, st_m = astat(wizzes["warrior"]), astat(wizzes["mage"])
    check("Crafting Aptitude: 0.02 a point of Insight and Resolve above 5 (warrior now Insight 5, Resolve 37: 0.64; mage Insight 27, Resolve 5: 0.44)",
          f"{st_w['crafting']} {st_m['crafting']}", "640 440")
    # Two fresh crafters at 22 points over the work: the shift 0.44 and the aptitude (warrior 0.02, mage 0.08) decide the quality.
    made = {}
    for calling, name in (("warrior", "cwar"), ("mage", "cmage")):
        c = h.Client(name, archetype=calling)
        c.read_until("> ")
        a.drain(c, 0.3)
        c.command("down")
        c.command("teachme simple_bandage")
        c.command(f"amk {CT}cloth_strip 1 1")
        dq(chief, f"setprof {name} herbalism {(work_level + 22) * 10}")
        c.send("craft bandage")
        made[calling] = c.read_any(("You make", "need", "cannot", "do not know"), timeout=20) + a.quiet(c, 0.5)
        made[calling + "_apt"] = str(astat(c)["crafting"])
        c.command("quit")
    check("aptitude: warrior 0.02, mage 0.08", made["warrior_apt"] + " " + made["mage_apt"], "20 80")
    check("warrior: 1 + 0.46 rounds to a common bandage", made["warrior"], "You make a simple bandage")
    check("mage: 1 + 0.52 rounds to a fine one", made["mage"], "You make a fine simple bandage")
    dq(chief, "rolls random")
    gatherers["scout"].command("quit")

    # ===== 11. a character saved before attributes existed =====
    old = h.Client("oldhand", archetype="warrior")
    old.read_until("> ")
    a.drain(old, 0.3)
    old.command("save")
    old.command("quit")
    time.sleep(0.6)
    path = None
    for root, _, files in os.walk(os.path.join(work, "save")):
        for f in files:
            if f.startswith("oldhand"):
                path = os.path.join(root, f)
    check("the save file is there", str(path is not None), "True")
    with open(path) as f:
        saved = json.load(f)
    keys = [k for k in saved if "ttribute" in k]
    check("a new save holds the attributes", str(sorted(keys)), "['attributePointsSpent', 'attributeValues']")
    for k in keys:
        del saved[k]
    saved["Level"] = 4
    saved["xp"] = 600
    with open(path, "w") as f:
        json.dump(saved, f)
    old = h.Client("oldhand", archetype="warrior")
    old.read_until("> ")
    a.drain(old, 0.3)
    s = old.command("score")
    check("converted at login: the calling's starting attributes", s, attr_line(START["warrior"]))
    check("and 2 points a level left to place (level 4: 6)", s, "Attribute points to place: 6")
    _, mhp, _, mst, _, mma = resources(old)
    check("its resources follow the formulas at level 4", f"{mhp} {mst} {mma}", " ".join(str(x) for x in formula("warrior", 4, START["warrior"])))
    old.command("improve might 2")
    old.command("quit")
    time.sleep(0.6)
    with open(path) as f:
        saved = json.load(f)
    check("and saved in the new form", f"{saved.get('attributeValues', {}).get('might')} {saved.get('attributePointsSpent')}", "10 2")

    # ===== 12. a wizard's object cannot set attributes or grant points =====
    os.makedirs(os.path.join(mudlib, "wiz", "chief"), exist_ok=True)
    h.write(mudlib, "/wiz/chief/meddler", MEDDLER)
    vic = h.Client("victim", archetype="mage")
    vic.read_until("> ")
    a.drain(vic, 0.3)
    vic.command("down")
    chief.command("goto " + T + "hall")
    out = chief.command("clone ~/meddler")
    before = vic.command("score")
    out = chief.command("meddle victim")
    check("the meddler ran", out, "MEDDLE ")
    check("no public method sets an attribute or grants a point", out, "SetAttribute=null", "AddAttributePoints=null", "EnsureAttributes=null", "CmdImprove=null", "might=5 points=0",
          absent=("=True", "=10"))
    after = vic.command("score")
    check("the victim's attributes are as they were", after, attr_line(START["mage"]), absent=("Attribute points to place",))
    vic.command("quit")


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-attr-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    for gone in ("crafting/launch_materials.cs", "recipes/launch.cs"):
        os.remove(os.path.join(mudlib, gone))
    shutil.copytree(SAMPLES, mudlib, dirs_exist_ok=True)
    os.makedirs(os.path.join(mudlib, "domains", "attrtest"), exist_ok=True)
    for path, source in test_files().items():
        h.write(mudlib, path, source)
    h.edit(mudlib, "/domains/hollow/village/square", 'Exits["south"]', f'Exits["down"] = "{T}hall";\n        Exits["south"]')
    proc, log = a.start_driver(work, mudlib)
    started = time.time()
    try:
        run(work, mudlib)
        log.flush()
        logged = open(os.path.join(work, "driver.log")).read()
        check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:800], "ok")
    finally:
        proc.kill()
        log.close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-4000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\nwhole test {time.time() - started:.0f} s")
    print(f"{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
