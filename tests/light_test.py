#!/usr/bin/env python3
"""Light and darkness (docs/specs/light.md): real characters over TCP against a driver on a copy of the mudlib.

Steps (LIGHT_STEPS=level,commands,burn,water,saving,messages,dark,tohit,sneak,glow,craft,bin,security,content to run some):
  level     the light of a room: base and sources sum and clamp, thresholds, torch/lantern/Glow, a light in a corpse or in another room
  commands  light, extinguish, fill: every success and refusal, "(lit)", time left, a quick action in a fight, works in the dark
  burn      the burn clock sped up: the warning, the end, a torch destroyed, a lantern kept, a torch put out keeps its time
  water     carried lit through the wet room it stays lit; dropped there it hisses out and keeps its time
  saving    lit, quit, log in: out, with the time that was left; an old save with no property has a full torch
  messages  the change of state told to the room each way
  dark      look in each state, the exits guess, hidden creatures and things, kill/something, Someone/Something, every refusal
  tohit     the table of the penalty; fights in the dark against a creature that sees and one that does not
  sneak     NoticeChance with lightAdjust for every row of the spec's table; the worked numbers
  glow      the rank table, the cost, the effect, on a friend, expiry, clamps
  craft     the Rag Torch recipe, Harland's lesson, stock, tallow
  bin       the torch bin: one per character, once, not for a wizard
  security  a wizard's object cannot register a source, set the burn clock, give light beyond the clamp
  content   every cellar room's light and state; each DarkDesc, the card, the bin, the topics

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=5081 python3 tests/light_test.py
"""
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "5081")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402

T = "/domains/lighttest/"
check = h.check
HOLLOW = "/domains/hollow/"
CELLARS = HOLLOW + "cellars/"
OBJ = HOLLOW + "obj/"
TORCH = OBJ + "pitch_torch"
RAG = OBJ + "rag_torch"
LANTERN = OBJ + "storm_lantern"
OIL = OBJ + "lamp_oil"

PROBE = r'''// @inherit /std/thing
public class LtProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        foreach (var v in new[] { "lgive", "ltele", "lroom", "lburn", "lrem", "lfloor", "lspawn", "lclear", "lfill", "llevel", "lglowx", "ladj", "lchance", "ltohit", "lsearch", "lcarry", "lstash", "lgold", "lstat", "lspeed", "lskillv", "lscale", "lcount" })
            AddAction(v, Do);
    }
    private ObjectRef? Who(string name) => Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "").ToLowerInvariant() == name.ToLowerInvariant());
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var light = LoadObject("/secure/light");
        switch (QueryVerb())
        {
            case "lgive": { var ob = CloneObject(w[1]); MoveObject(ob, Who(w[0])!); Write("GAVE " + ob.Name + "\n"); return true; }
            case "ltele": MoveObject(Who(w[0])!, LoadObject(w[1].StartsWith("/") ? w[1] : "/domains/lighttest/" + w[1])); Write("TELE ok\n"); return true;
            case "lroom":
            {
                var r = w.Length > 0 ? LoadObject(w[0].StartsWith("/") ? w[0] : "/domains/lighttest/" + w[0]) : Environment(me)!;
                Write($"LROOM {light.Call<int>("QueryLevel", r)} {light.Call<string>("QueryState", r)} base {r.Call<int>("QueryBaseLight")}\n");
                return true;
            }
            case "lburn": Write("BURN " + light.Call<bool>("SetTestBurnScale", int.Parse(w[0])) + "\n"); return true;
            case "lscale": Write("SCALE " + light.Call<int>("QueryBurnScale") + "\n"); return true;
            case "lrem":
            {
                var ob = Present(w[1], Who(w[0])!);
                Write(ob == null ? "REM none\n" : $"REM {ob.Call<int>("QueryRemaining")} lit {ob.Call<bool>("QueryLit")} value {ob.Call<int>("QueryLightValue")}\n");
                return true;
            }
            case "lfloor": { var ob = CloneObject(w[0]); MoveObject(ob, Environment(me)!); Write("FLOOR " + ob.Name + "\n"); return true; }
            case "lspawn": { var n = w.Length > 2 ? int.Parse(w[2]) : 1; for (var i = 0; i < n; i++) MoveObject(CloneObject("/domains/lighttest/" + w[1]), LoadObject("/domains/lighttest/" + w[0])); Write("SPAWNED " + n + "\n"); return true; }
            case "lclear": { var k = 0; foreach (var o in AllInventory(LoadObject("/domains/lighttest/" + w[0])).ToList()) if (o.IsAlive && o.Call<bool>("IsMonster")) { o.Call("Die", (ObjectRef?)null); k++; } Write("CLEARED " + k + "\n"); return true; }
            case "lfill": { var p = Who(w[0])!; p.Call("Heal", 99999); p.Call("FillResources"); Write("FILLED\n"); return true; }
            case "lgold": { Who(w[0])!.Call("AddGold", int.Parse(w[1])); Write("GOLD\n"); return true; }
            case "llevel":
            {
                var p = Who(w[0])!; var want = int.Parse(w[1]);
                while (p.Call<int>("QueryLevel") < want) p.Call("AddXp", p.Call<int>("QueryXpForLevel", p.Call<int>("QueryLevel") + 1) - p.Call<int>("QueryXp"));
                Write("LEVEL " + p.Call<int>("QueryLevel") + "\n"); return true;
            }
            case "lglowx": Who(w[0])!.Call("AddEffect", "evil", "Evil", 300, "light=" + w[1], 1.0, 0); Write("EVIL\n"); return true;
            case "ladj": Write("ADJ " + light.Call<int>("NoticeAdjustFor", int.Parse(w[0]), w[1] == "1", w[2] == "1") + "\n"); return true;
            case "lchance": { var n = w.Select(int.Parse).ToArray(); Write("CHANCE " + LoadObject("/secure/sneak").Call<int>("NoticeChance", n[0], n[1], n[2], n[3] == 1, n[4], n[5]) + "\n"); return true; }
            case "ltohit": Write("TOHIT " + light.Call<int>("ToHitPenalty", int.Parse(w[0]), w[1] == "1", w[2] == "1") + "\n"); return true;
            case "lsearch": Write("SEARCH " + light.Call<int>("SearchModifier", int.Parse(w[0]), w[1] == "1") + "\n"); return true;
            case "lcarry": Write("CARRY " + light.Call<bool>("QueryCarriesLight", Who(w[0])!) + "\n"); return true;
            case "lstash":
            {
                // Put what a character carries (by name) into the first corpse in the room.
                var p = Who(w[0])!; var ob = Present(w[1], p); var corpse = AllInventory(Environment(p)!).FirstOrDefault(o => o.Call<bool>("IsCorpse"));
                if (ob == null || corpse == null) { Write("STASH none\n"); return true; }
                MoveObject(ob, corpse); Write("STASH ok\n"); return true;
            }
            case "lstat": { var p = Who(w[0])!; Write($"STAT focus {p.Call<int>("QueryFocus")}/{p.Call<int>("QueryMaxFocus")} vit {p.Call<int>("QueryVitality")} fighting {p.Call<bool>("QueryInCombat")}\n"); return true; }
            case "lspeed": Who(w[0])!.Call("AddEffect", "quick", "Haste", 600, "", 0.1, 0); Write("SPEED\n"); return true;
            case "lskillv": { var k = LoadObject("/skills/" + w[0]); Write("SKILL " + string.Join(" ", Enumerable.Range(1, int.Parse(w[2])).Select(r => k.Call<double>("RankValue", w[1], r))) + "\n"); return true; }
            case "labsorb": Who(w[0])!.Call("AddEffect", "shield", "Shield", 600, "", 1.0, 100000000); Write("ABSORB\n"); return true;
            case "lcount": Write("COUNT " + AllInventory(Environment(me)!).Count(o => o.IsAlive && o.Call<bool>("IsMonster")) + "\n"); return true;
        }
        return false;
    }
}
'''

HALL_INIT = """    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who != null && Interactive(who) && who.Call<string>("QueryName")!.ToLowerInvariant() == "ltchief" && Present("probe", who) == null) MoveObject(CloneObject("/domains/lighttest/probe"), who);
    }
"""

ROOM = """// @inherit /std/room
public class {cls} : Room
{{
    public override void Create()
    {{
        ShortDesc = "{short}";
        LongDesc = "A room made for the light test.";
{exits}{body}    }}
{extra}}}
"""


def room(cls, short, exits, body="", extra=""):
    ex = "".join(f'        Exits["{d}"] = "{p}";\n' for d, p in exits.items())
    return ROOM.format(cls=cls, short=short, exits=ex, body=body, extra=extra)


def creature(cls, name, ident, hp=500000, level=2, armor=0, flags="", reaction=1.0):
    return f"""// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); ShortDesc = "a {name.lower()}"; LongDesc = "A creature made for the light test.";
        Level = {level}; MaxVitality = {hp}; DamageMin = 1; DamageMax = 1; ToHit = -40; Armor = {armor}; Dodge = 0; XpValue = 0;
        BaseReaction = {reaction};
{flags}    }}
}}
"""


GUNNER = """// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "Gunner"; Ids.Add("gunner"); ShortDesc = "a gunner"; LongDesc = "A creature made for the light test: it swings very fast.";
        Level = 2; MaxVitality = 500000; DamageMin = 1; DamageMax = 1; ToHit = {tohit}; Armor = 0; Dodge = 0; XpValue = 0; BaseReaction = 0.05; Aggressive = true;
{flags}        AddCombatAction("zap", "Zap", 0, 0.05, "", 1, -0.05);
    }}
}}
"""

SELLER = """// @inherit /std/shopkeeper
public class LtSeller : Shopkeeper
{
    public override void Create()
    {
        Name = "Seller"; Ids.Add("seller"); ShortDesc = "a seller"; LongDesc = "A seller made for the light test."; Level = 1; MaxVitality = 50;
        AddStock("/domains/hollow/obj/pitch_torch", 5);
    }
}
"""

# A wizard's object (under /wiz): everything a stranger could try with light.
MEDDLER = r'''// @inherit /std/thing
public class LtMeddler : Thing
{
    public override void Create() { Name = "meddler"; Ids.Add("meddler"); }
    public override string? Short() => "a meddler";
    public override void Init() { AddAction("lmeddle", Meddle); }
    private bool Meddle(string args)
    {
        var who = Users().First(u => (u.Call<string>("QueryName") ?? "").ToLowerInvariant() == args.Trim());
        var light = LoadObject("/secure/light");
        var tries = new List<string>();
        void Try(string label, Func<object?> f)
        {
            try { tries.Add(label + "=" + (f() ?? "null")); }
            catch (MudRuntimeException e) { tries.Add(label + "!" + e.Message.Length); }
        }
        Try("Burn", () => light.Call<bool>("SetTestBurnScale", 500));
        Try("Scale", () => light.Call<int>("QueryBurnScale"));
        Try("Add", () => { light.Call("AddSource", Self); return "called"; });
        Try("AddOther", () => { var t = AllInventory(who).FirstOrDefault(); if (t != null) light.Call("AddSource", t); return "called"; });
        Try("Drop", () => { var t = AllInventory(who).FirstOrDefault(o => o.Call<bool>("QueryLit")); if (t != null) light.Call("DropSource", t, Environment(who)); return t == null ? "none" : "called"; });
        Try("Glow", () => { light.Call("NoteGlow", who, 600); return "called"; });
        Try("Level", () => light.Call<int>("QueryLevel", Environment(who)!));
        Write("MEDDLE " + string.Join(" ", tries) + "\n");
        return true;
    }
}
'''

WIZ_LAMP = '''// @inherit /std/light_source
public class WizLamp : LightSource
{
    public override void Create()
    {
        Name = "Wizard Lamp"; Ids.Add("wizlamp"); ShortDesc = "a wizard lamp"; LongDesc = "A lamp made in a workshop.";
        LightValue = 99; Capacity = 600; Noun = "lamp";
    }
}
'''

WIZ_ROOM = '''// @inherit /std/room
public class WizDark : Room
{
    public override void Create()
    {
        ShortDesc = "Wizard Dim Room"; LongDesc = "A dim room in a wizard's home.";
        Exits["out"] = "/domains/lighttest/hall";
        Light = LightDim;
    }
}
'''


def test_files():
    f = {T + "probe": PROBE, T + "seller": SELLER}
    f[T + "goblin"] = creature("LtGoblin", "Goblin", "goblin")
    f[T + "toad"] = creature("LtToad", "Toad", "toad", flags="        DarkSight = false;\n")
    f[T + "gunner"] = GUNNER.format(cls="LtGunner", tohit=2, flags="")
    f[T + "gunnerb"] = GUNNER.format(cls="LtGunnerB", tohit=2, flags="        DarkSight = false;\n")
    f[T + "armored"] = creature("LtArmored", "Dummy", "dummy", armor=4, flags="        DarkSight = false;\n")
    f[T + "armored2"] = creature("LtArmored2", "Dummy", "dummy", armor=2)
    f[T + "hall"] = room("LtHall", "Light Hall", {"up": HOLLOW + "village/square", "north": T + "l_lit", "south": T + "l_dim", "east": T + "l_dark", "west": T + "l_wet", "northeast": T + "l_black", "southeast": T + "l_sun", "southwest": T + "l_shop"}, "", HALL_INIT)
    f[T + "l_lit"] = room("LtLit", "Lit Room", {"south": T + "hall"}, "        Light = 1;\n")
    f[T + "l_dim"] = room("LtDim", "Dim Room", {"north": T + "hall"}, "        Light = 0;\n")
    f[T + "l_dark"] = room("LtDark", "Dark Room", {"west": T + "hall", "east": T + "l_dark2"}, '        Light = -1;\n        DarkDesc = "Cold stone, and a drip somewhere.";\n')
    f[T + "l_dark2"] = room("LtDark2", "Far Dark Room", {"west": T + "l_dark", "north": T + "l_dark3"}, '        Light = -1;\n')
    f[T + "l_dark3"] = room("LtDark3", "Farther Dark Room", {"south": T + "l_dark2"}, '        Light = -1;\n        DarkDesc = "Silence.";\n')
    f[T + "l_wet"] = room("LtWet", "Wet Room", {"east": T + "hall"}, '        Light = -1;\n        Wet = true;\n        DarkDesc = "Water laps.";\n')
    f[T + "l_black"] = room("LtBlack", "Black Room", {"southwest": T + "hall"}, "        Light = -3;\n")
    f[T + "l_sun"] = room("LtSun", "Sun Room", {"northwest": T + "hall"}, "        Light = 9;\n")
    f[T + "l_shop"] = room("LtShop", "Dark Shop", {"northeast": T + "hall"}, '        Light = -1;\n        DarkDesc = "Dark.";\n        AddSpawn("/domains/lighttest/seller");\n')
    return f


def login(name, calling="warrior"):
    c = h.Client(name, archetype=calling)
    c.read_until("> ")
    a.drain(c, 0.4)
    c.command("down")
    return c


def say(c, line, wait=0.4):
    return c.command(line) + a.quiet(c, wait)


def lroom(chief, where=""):
    out = chief.command("lroom " + where)
    m = re.search(r"LROOM (-?\d+) (\w+) base (-?\d+)", out)
    return (int(m.group(1)), m.group(2), int(m.group(3))) if m else None


def give(chief, who, path):
    chief.command(f"lgive {who} {path}")


def tele(chief, who, where):
    chief.command(f"ltele {who} {where}")


def new_player(chief, name, calling="warrior", where="hall"):
    p = login(name, calling)
    chief.command(f"lfill {name}")
    chief.command(f"lspeed {name}")
    tele(chief, name, where)
    a.drain(p, 0.3)
    return p


# ---------------------------------------------------------------- steps

def step_level(chief):
    check("a room with no Light is lit by default (base 1)", str(lroom(chief, "hall")), "(1, 'lit', 1)")
    for room_name, want in (("l_lit", (1, "lit", 1)), ("l_dim", (0, "dim", 0)), ("l_dark", (-1, "dark", -1)), ("l_black", (-3, "dark", -3)), ("l_sun", (3, "lit", 9))):
        check(f"{room_name}: level and state (the base is clamped to -3..3)", str(lroom(chief, room_name)), str(want))
    p = new_player(chief, "ltlevel", "warrior", "l_dark")
    give(chief, "ltlevel", RAG)
    give(chief, "ltlevel", LANTERN)
    check("an unlit torch and an empty-handed dark room: dark", str(lroom(chief, "l_dark")), "(-1, 'dark', -1)")
    check("a torch in the pack that is not lit does nothing", str(lroom(chief, "l_dark")), "'dark'")
    say(p, "light torch", 2.4)
    check("a lit torch makes a dark room dim", str(lroom(chief, "l_dark")), "(0, 'dim', -1)")
    say(p, "light lantern", 2.4)
    check("a torch and a lantern make a dark room lit", str(lroom(chief, "l_dark")), "(2, 'lit', -1)")
    say(p, "extinguish torch")
    check("with the torch out, the lantern alone lights a dark room", str(lroom(chief, "l_dark")), "(1, 'lit', -1)")
    check("a light out adds nothing: the other dark room is still dark", str(lroom(chief, "l_dark2")), "(-1, 'dark', -1)")
    check("a lit light is in the carry test", chief.command("lcarry ltlevel"), "CARRY True")
    p.command("east")
    a.drain(p, 0.3)
    check("carried to the next room, it lights that room", str(lroom(chief, "l_dark2")), "(1, 'lit', -1)")
    check("and the room it left is dark again", str(lroom(chief, "l_dark")), "(-1, 'dark', -1)")
    say(p, "extinguish lantern")
    # A lit torch lying in one room lights nothing in another.
    p.command("west")
    a.drain(p, 0.3)
    say(p, "light torch", 2.4)
    say(p, "drop torch")
    check("a lit torch on the floor lights its room", str(lroom(chief, "l_dark")), "(0, 'dim', -1)")
    check("and not the next one", str(lroom(chief, "l_dark2")), "(-1, 'dark', -1)")
    say(p, "get torch")
    check("picked up it still lights the room it is carried in", str(lroom(chief, "l_dark")), "(0, 'dim', -1)")
    # In a corpse a light lights nothing and is put out.
    chief.command("lspawn l_dark goblin")
    say(p, "extinguish torch")
    p.command("quit")
    # A light in a corpse: a goblin dies in the lit room, the torch is lit, then put into the corpse.
    q = new_player(chief, "ltcorpse", "warrior", "l_dim")
    give(chief, "ltcorpse", RAG)
    say(q, "light torch", 2.4)
    check("a dim room with a lit torch is lit", str(lroom(chief, "l_dim")), "(1, 'lit', 0)")
    chief.command("lspawn l_dim goblin")
    chief.command("lclear l_dim")
    tele(chief, "ltcorpse", "l_dim")
    check("the torch goes into a corpse", chief.command("lstash ltcorpse torch"), "STASH ok")
    a.quiet(q, 0.3)
    check("a light in a corpse lights nothing: the room is dim again", str(lroom(chief, "l_dim")), "(0, 'dim', 0)")
    q.command("quit")


def step_commands(chief):
    p = new_player(chief, "ltcmd", "warrior", "l_lit")
    give(chief, "ltcmd", TORCH)
    give(chief, "ltcmd", LANTERN)
    check("light with nothing named lights nothing when two kinds are carried", say(p, "light"), "Light which?")
    out = say(p, "light pitch torch", 2.4)
    check("light: the success line", out, "You light the pitch torch. It flares, then settles to a steady flame.")
    check("a lit torch is listed as lit", say(p, "inventory"), "a pitch torch (lit)")
    check("light again: already burning", say(p, "light pitch torch", 2.4), "already burning")
    check("look at the torch: the time left to the nearest five minutes", say(p, "look at pitch torch"), "about twenty-five minutes of burning")
    check("the lantern with no oil in it burns oil: a fresh one is full, so it lights", say(p, "light lantern", 2.4), "You light the storm lantern.")
    check("score shows the light you carry", say(p, "score"), "You carry a light:")
    check("extinguish: the success line", say(p, "extinguish pitch torch"), "You pinch out the pitch torch.")
    check("extinguish a light that is out", say(p, "extinguish pitch torch"), "It is not lit.")
    check("put out, the other word", say(p, "put out lantern"), "storm lantern")
    check("douse with nothing lit", say(p, "douse"), "It is not lit.")
    check("fill with no lamp oil", say(p, "fill lantern"), "You have no lamp oil.")
    check("fill something that is not a lantern", say(p, "fill torch"), "That cannot be filled.")
    check("fill what you do not have", say(p, "fill bucket"), "You have no 'bucket' to fill.")
    # Half-burnt lantern takes oil.
    chief.command("lburn 600")
    say(p, "light lantern", 2.4)
    time.sleep(0.5)  # about 1700 s of oil at 600x
    say(p, "extinguish lantern")
    chief.command("lburn 1")
    rem = chief.command("lrem ltcmd lantern")
    m = re.search(r"REM (\d+)", rem)
    check("a lantern lit for a while has less than a full reservoir", str(m is not None and 1000 < int(m.group(1)) < 3500), "True")
    check("no flask: no oil", say(p, "fill lantern"), "You have no lamp oil.")
    give(chief, "ltcmd", OIL)
    check("fill: the success line", say(p, "fill lantern"), "You pour the oil into the lantern, and the reservoir fills.")
    rem = chief.command("lrem ltcmd lantern")
    check("a lantern is never filled past 3600 s", rem, "REM 3600")
    check("the flask is spent in full", say(p, "inventory"), absent=("lamp oil",))
    # In a fight, a quick action.
    chief.command("lspawn l_lit goblin")
    say(p, "kill goblin", 0.5)
    out = say(p, "light torch", 3.0)
    check("light in a fight is an action on the clock", out, "light", absent=("What?",))
    say(p, "stop")
    chief.command("lclear l_lit")
    say(p, "flee")
    # In the dark by feel.
    say(p, "extinguish torch")
    tele(chief, "ltcmd", "l_dark")
    a.drain(p, 0.3)
    check("lighting in the dark works by feel", say(p, "light torch", 2.4), "You light the pitch torch")
    check("and the room is dim now", str(lroom(chief, "l_dark")), "(0, 'dim', -1)")
    p.command("quit")


def step_burn(chief):
    p = new_player(chief, "ltburn", "warrior", "l_dim")
    give(chief, "ltburn", RAG)
    give(chief, "ltburn", RAG)
    give(chief, "ltburn", LANTERN)
    chief.command("lburn 50")
    check("the burn clock runs 50 times as fast", chief.command("lscale"), "SCALE 50")
    say(p, "light rag torch", 2.4)
    time.sleep(1.5)
    say(p, "extinguish rag torch")
    rem = int(re.search(r"REM (\d+)", chief.command("lrem ltburn torch")).group(1))
    check("a torch put out keeps the time it had left", str(300 < rem < 590), "True")
    say(p, "light rag torch", 2.4)
    out = a.quiet(p, 12.5)
    check("the warning at 30 seconds left", out, "Your rag torch burns low.")
    check("the end: a torch gutters and dies, and what is left of it crumbles", out, "Your rag torch gutters and dies. What is left of it crumbles.")
    check("one rag torch is left, the burnt one is gone", say(p, "inventory"), "a rag torch", absent=("(lit)",))
    inv = say(p, "inventory")
    check("the burnt-out torch was destroyed (one rag torch only)", str("(x2)" in inv), "False")
    # The lantern at 50x: 3600 s of oil lasts 72 s; put little oil in by lighting for a while: just watch it warn and stay.
    chief.command("lburn 500")
    say(p, "light lantern", 2.4)
    out = a.quiet(p, 7.6)
    check("the lantern warns", out, "Your lantern's flame sinks; it is nearly out of oil.")
    out += a.quiet(p, 1.0)
    check("a burnt-out lantern: the end", out, "Your lantern gutters and dies.")
    check("a lantern stays when it is empty", say(p, "inventory"), "storm lantern")
    check("and says so when lit empty", say(p, "light lantern", 2.4), "The storm lantern has no oil left.")
    give(chief, "ltburn", OIL)
    check("a flask refills it", say(p, "fill lantern"), "reservoir fills")
    check("by 1800 seconds", chief.command("lrem ltburn lantern"), "REM 1800")
    chief.command("lburn 1")
    p.command("quit")
    # A torch lying on the floor burns out there.
    q = new_player(chief, "ltfloor", "warrior", "l_dim")
    give(chief, "ltfloor", RAG)
    chief.command("lburn 100")
    say(q, "light rag torch", 2.4)
    say(q, "drop rag torch")
    out = a.quiet(q, 8.5)
    check("a lit torch on the floor: the room hears it gutter and die", out, "The torch on the floor gutters and dies.")
    chief.command("lburn 1")
    q.command("quit")


def step_water(chief):
    p = new_player(chief, "ltwater", "warrior", "l_dark")
    give(chief, "ltwater", TORCH)
    give(chief, "ltwater", RAG)
    say(p, "light pitch torch", 2.4)
    p.command("west")
    a.drain(p, 0.3)
    p.command("east")
    a.drain(p, 0.3)
    p.command("west")
    a.drain(p, 0.3)
    tele(chief, "ltwater", "l_wet")
    a.drain(p, 0.3)
    check("carried through the wet room, a lit light stays lit", chief.command("lrem ltwater pitch"), "lit True")
    check("so the wet room is dim", str(lroom(chief, "l_wet")), "(0, 'dim', -1)")
    out = say(p, "drop pitch torch")
    check("dropped in the water it hisses out", out, "The torch hisses out in the water.")
    check("the room is dark again", str(lroom(chief, "l_wet")), "(-1, 'dark', -1)")
    # It was put out, not destroyed; and it keeps its time. In the dark nothing can be picked up: light another first.
    check("in the dark a dropped torch cannot be found", say(p, "get pitch torch"), "It is too dark to see what is here.")
    say(p, "light rag torch", 2.4)
    say(p, "get pitch torch")
    out = chief.command("lrem ltwater pitch")
    check("it can be picked up unlit with its time", out, "lit False")
    rem = int(re.search(r"REM (\d+)", out).group(1))
    check("with nearly all its time left", str(rem > 1400), "True")
    p.command("quit")


def step_saving(chief):
    p = new_player(chief, "ltsave", "warrior", "l_lit")
    give(chief, "ltsave", TORCH)
    chief.command("lburn 100")
    say(p, "light torch", 2.4)
    time.sleep(2.0)
    p.command("save")
    a.quiet(p, 0.3)
    p.command("quit")
    time.sleep(0.8)
    chief.command("lburn 1")
    q = h.Client("ltsave", archetype="warrior")
    q.read_until("> ")
    a.drain(q, 0.5)
    out = q.command("inventory")
    check("after a login the torch is out", out, "a pitch torch", absent=("(lit)",))
    # Ask the time: it is the pitch torch with about 200 s gone, so under 1500.
    rem = int(re.search(r"REM (\d+)", chief.command("lrem ltsave torch")).group(1))
    print("      rem", rem)
    check("with the time that was left, not a full one", str(800 < rem < 1495), "True")
    q.command("quit")
    # A fresh character with a fresh torch has a full one (no property).
    r = new_player(chief, "ltfresh", "warrior", "l_lit")
    give(chief, "ltfresh", TORCH)
    check("a fresh torch is full", chief.command("lrem ltfresh torch"), "REM 1500")
    r.command("quit")


def step_messages(chief):
    p = new_player(chief, "ltmsga", "warrior", "l_dark")
    q = new_player(chief, "ltmsgb", "warrior", "l_dark")
    give(chief, "ltmsga", TORCH)
    give(chief, "ltmsga", LANTERN)
    say(q, "look", 0.2)
    p.command("light torch")
    out = a.quiet(q, 2.6)
    check("dark to dim is told to the room", out, "A dim glow lets you make out the shapes of things.")
    p.command("light lantern")
    out = a.quiet(q, 2.6)
    check("dim to lit is told to the room", out, "The light grows until the room is plain to see.")
    p.command("extinguish lantern")
    out = a.quiet(q, 2.6)
    check("lit to dim is told to the room", out, "The light dwindles; it is dim here.")
    p.command("extinguish torch")
    out = a.quiet(q, 2.6)
    check("dim to dark is told to the room", out, "The dark closes in.")
    p.command("quit")
    q.command("quit")


def step_dark(chief):
    p = new_player(chief, "ltdark", "warrior", "hall")
    chief.command("lspawn l_dark goblin")
    out = say(p, "east")
    check("entering a dark room: the name with (dark)", out, "Dark Room (dark)")
    check("it is too dark to see, and the room's own line", out, "It is too dark to see. Cold stone, and a drip somewhere.")
    check("the way you came in", out, "the way you came in is west", "try a direction")
    check("the warning, once, on entering unlit", out, "It is pitch black here. You will want a light.")
    check("no creature is listed", out, absent=("goblin", "is here"))
    check("a direction that is not an exit", say(p, "north"), "You grope along the wall and find only stone.")
    check("a short direction too", say(p, "u"), "You grope along the wall and find only stone.")
    check("look at a thing", say(p, "look at goblin"), "It is too dark to see that.")
    check("get in the dark", say(p, "get all"), "It is too dark to see what is here.")
    check("loot in the dark", say(p, "loot goblin"), "It is too dark to see what is here.")
    check("read in the dark", say(p, "read sign"), "It is too dark to read.")
    check("craft in the dark", say(p, "craft rag torch"), "It is too dark to see what you are doing.")
    check("combine in the dark", say(p, "combine cloth strip and tallow"), "It is too dark to see what you are doing.")
    check("harvest in the dark", say(p, "harvest"), "It is too dark to see what you are doing.")
    check("skin in the dark", say(p, "skin"), "It is too dark to see what you are doing.")
    check("kill goblin: you cannot name what you cannot see", say(p, "kill goblin"), "You cannot see a goblin.")
    check("consider needs sight", say(p, "consider goblin"), "You cannot see a goblin.")
    check("inventory works by feel", say(p, "inventory"), "Inventory")
    check("score works", say(p, "score"), "It is dark here.")
    check("rest works", say(p, "rest"), absent=("too dark",))
    say(p, "stand")
    # the exits it learns
    check("the dark exits are remembered once used", say(p, "east"), "Far Dark Room (dark)", "the way you came in is west")
    say(p, "west")
    say(p, "west")
    say(p, "east")
    out = say(p, "look")
    check("the exits used from this room are remembered, the way in first", out, "you know of west, east")
    out = say(p, "kill something", 4.0)
    check("kill something works: the character fights, and sees only Something", out, "You attack something.")
    check("combat lines name nobody", out, "something", absent=("a goblin",))
    check("look names the fight", say(p, "look"), "Something is fighting you.")
    say(p, "flee")
    chief.command("lclear l_dark")
    # an arrival is told as Someone
    q = new_player(chief, "ltdarkb", "warrior", "hall")
    tele(chief, "ltdark", "l_dark")
    a.drain(p, 0.3)
    q.command("east")
    out = a.quiet(p, 0.8)
    check("an arrival in the dark is Someone", out, "Someone arrives.", absent=("ltdarkb",))
    # shops: no
    tele(chief, "ltdark", "l_shop")
    a.drain(p, 0.5)
    check("a shop in the dark refuses browse", say(p, "browse"), "It is too dark to see the goods.")
    check("and buy", say(p, "buy torch"), "It is too dark to see the goods.")
    q.command("quit")
    p.command("quit")


def swings(p, seconds):
    """What the target of a gunner sees for a while: (hits, misses) of the attacks on them."""
    out = a.quiet(p, seconds)
    misses = len(re.findall(r"(?:Something|A gunner) misses you", out))
    hits = len(re.findall(r"(?:Something|A gunner) (?:scratches|hits|smashes) you", out))
    return hits, misses


def gunner_rate(chief, p, who, room, gunner, seconds=30):
    chief.command(f"labsorb {who}")
    for _ in range(4):
        chief.command(f"lspawn {room} {gunner}")
    p_hits, p_misses = swings(p, seconds)
    chief.command(f"lclear {room}")
    total = max(1, p_hits + p_misses)
    return p_hits / total, total


def step_tohit(chief):
    for level, ranged, sees, want in ((1, 0, 0, 0), (1, 1, 0, 0), (0, 0, 0, -1), (0, 1, 0, -2), (-1, 0, 0, -3), (-1, 1, 0, -6), (-1, 0, 1, 0), (-1, 1, 1, 0), (0, 0, 1, 0), (2, 1, 0, 0), (-3, 1, 0, -6)):
        check(f"to-hit penalty light {level} ranged {ranged} sees {sees}", chief.command(f"ltohit {level} {ranged} {sees}"), f"TOHIT {want}")
    # Real fights: a very fast creature swings at a character who soaks the damage; its rate of hitting, by light and by sight.
    p = new_player(chief, "lttarget", "warrior", "l_lit")
    rates = {}
    counts = []
    for label, where, gunner in (("lit, blind", "l_lit", "gunnerb"), ("dark, blind", "l_dark", "gunnerb"), ("dark, sighted", "l_dark", "gunner")):
        tele(chief, "lttarget", where)
        a.drain(p, 0.3)
        hits = n = 0
        for _ in range(3):
            rate, k = gunner_rate(chief, p, "lttarget", where, gunner)
            hits += rate * k
            n += k
            a.quiet(p, 0.5)
        rates[label] = hits / max(1, n)
        counts.append(n)
        print(f"      {label}: {100 * rates[label]:.0f}% of {n} swings")
    check("enough swings were counted to say anything", str(all(n >= 40 for n in counts)), "True")
    check("a creature that sees in the dark hits about as well in the dark as a blind one does in the light", str(abs(rates["dark, sighted"] - rates["lit, blind"]) < 0.12), "True")
    check("and a blind creature hits much less in the dark (-3: fifteen points)", str(rates["lit, blind"] - rates["dark, blind"] > 0.04), "True")
    p.command("quit")


def step_sneak(chief):
    # lightAdjust is the sixth term of NoticeChance.
    for args, want in (("9 3 2 0 0 0", 25), ("9 3 4 1 0 0", 65), ("9 3 2 0 0 -10", 15), ("9 3 4 1 0 -10", 55), ("9 3 2 0 0 -15", 10), ("9 3 4 1 0 -15", 50),
                       ("9 3 2 0 0 20", 45), ("9 3 4 1 0 20", 85), ("9 3 2 0 0 10", 35), ("9 3 4 1 0 10", 75), ("9 3 3 0 0 -25", 5), ("9 3 3 0 0 -5", 25), ("9 3 3 0 0 0", 30)):
        check(f"NoticeChance with a light term {args}", chief.command("lchance " + args), f"CHANCE {want}")
    # The adjustment itself: (level, sees, own light).
    for level, sees, own, want in ((1, 1, 0, 0), (1, 1, 1, 20), (0, 1, 0, -10), (0, 1, 1, 10), (-1, 1, 0, -15), (-1, 1, 1, 5), (-1, 0, 0, -25), (-1, 0, 1, -5), (0, 0, 0, -10), (-3, 0, 0, -25)):
        check(f"NoticeAdjust level {level} dark sight {sees} own light {own}", chief.command(f"ladj {level} {sees} {own}"), f"ADJ {want}")
    # The spec's table, as the whole path: a level 3 Scout, Grace 9. Each row is (room level seen, own light) for the scavenger (level 2) and the Elite Guard (level 4, Alert).
    table = (((1, 0), 25, 65), ((1, 1), 45, 85), ((0, 0), 15, 55), ((1, 1), 45, 85), ((-1, 0), 10, 50), ((0, 1), 35, 75), ((1, 1), 45, 85))
    for (level, own), scav, guard in table:
        adj = int(re.search(r"ADJ (-?\d+)", chief.command(f"ladj {level} 1 {own}")).group(1))
        s = int(re.search(r"CHANCE (\d+)", chief.command(f"lchance 9 3 2 0 0 {adj}")).group(1))
        g = int(re.search(r"CHANCE (\d+)", chief.command(f"lchance 9 3 4 1 0 {adj}")).group(1))
        check(f"the worked numbers: level {level}, own light {own}", f"{s} {g}", f"{scav} {guard}")
    # The toad: unlit in the dark 5%; with a torch (room dim, own light) 40%; with a lantern (room lit, own light) 50%.
    for level, own, want in ((-1, 0, 5), (0, 1, 40), (1, 1, 50)):
        adj = int(re.search(r"ADJ (-?\d+)", chief.command(f"ladj {level} 0 {own}")).group(1))
        check(f"the Giant Cave Toad, room {level}, own light {own}", chief.command(f"lchance 9 3 3 0 0 {adj}"), f"CHANCE {want}")
    # A real sneaker: the beacon test through the real daemon: a Scout who sneaks, with and without light.
    s = new_player(chief, "ltscout", "scout", "l_dark")
    check("a Scout with no light is no beacon", chief.command("lcarry ltscout"), "CARRY False")
    give(chief, "ltscout", RAG)
    say(s, "light torch", 2.4)
    check("a lit torch makes the carrier a beacon", chief.command("lcarry ltscout"), "CARRY True")
    s.command("quit")


def step_glow(chief):
    check("the rank table: light", chief.command("lskillv glow light 3"), "SKILL 1 1 2")
    check("the rank table: seconds", chief.command("lskillv glow seconds 3"), "SKILL 300 480 720")
    check("the rank table: focus", chief.command("lskillv glow focus 3"), "SKILL 20 23 26")
    m = new_player(chief, "ltmage", "mage", "l_dark")
    pr = new_player(chief, "ltpriest", "priest", "l_dark")
    chief.command("lfill ltpriest")
    before = int(re.search(r"focus (\d+)", chief.command("lstat ltpriest")).group(1))
    out = say(pr, "glow", 2.0)
    check("a Priest can cast Glow from level 1", out, "A small, steady light gathers about you.")
    after = int(re.search(r"focus (\d+)", chief.command("lstat ltpriest")).group(1))
    check("it costs 20 Focus", str(before - after), "20")
    chief.command("lfill ltmage")
    before = int(re.search(r"focus (\d+)", chief.command("lstat ltmage")).group(1))
    out = say(m, "glow", 2.0)
    check("a Mage can cast Glow from level 1", out, "A small, steady light gathers about you.")
    after = int(re.search(r"focus (\d+)", chief.command("lstat ltmage")).group(1))
    check("a Mage's Focus Efficiency makes it cheaper (16 of 20)", str(before - after), "16")
    check("two Glows (a Priest's and a Mage's) make a dark room lit", str(lroom(chief, "l_dark")), "(1, 'lit', -1)")
    out = say(m, "glow", 1.5)
    check("a recast refreshes: it does not add", str(lroom(chief, "l_dark")), "(1, 'lit', -1)")
    out = say(pr, "glow ltmage", 2.0)
    check("a Priest can call a light about a friend", out, "You call a small light about")
    check("the friend's glow was refreshed, not added", str(lroom(chief, "l_dark")), "(1, 'lit', -1)")
    give(chief, "ltpriest", RAG)
    say(pr, "light torch", 2.4)
    check("torch and two Glows: more light, within the clamp", str(lroom(chief, "l_dark")), "(2, 'lit', -1)")
    check("Glow is a light for the sneaking rule", chief.command("lcarry ltmage"), "CARRY True")
    pr.command("quit")
    chief.command("lglowx ltmage 99")
    check("an effect with a huge light is clamped: +2 at most for a creature (base -1 + 2)", str(lroom(chief, "l_dark")), "(1, 'lit', -1)")
    # A Warrior has no Glow.
    w = new_player(chief, "ltwarglow", "warrior", "l_dark")
    check("a Warrior has no Glow", say(w, "glow"), "What?")
    w.command("quit")
    m.command("quit")
    # Expiry (a mage at 5 minutes is too long: shorten by quitting; the effect ends with the character; the register forgets).
    check("after the casters left, the room is dark again", str(lroom(chief, "l_dark")), "(-1, 'dark', -1)")


def step_craft(chief):
    p = new_player(chief, "ltcraft", "warrior", HOLLOW + "village/tavern")
    check("Harland sells the pitch torch", say(p, "browse"), "pitch torch", "tallow", "lamp oil")
    check("Harland teaches the rag torch free", say(p, "lessons"), "Rag Torch", "free")
    out = say(p, "learn rag torch")
    check("learn rag torch", out, "Rag Torch")
    check("his topic on torches", say(p, "ask harland about torches"), "I hung every lamp on that wall myself")
    give(chief, "ltcraft", OBJ + "craft/cloth_strip")
    give(chief, "ltcraft", OBJ + "craft/tallow")
    out = say(p, "craft rag torch", 4.0)
    check("a rag torch is made with no station: two come of it", out, "You make 2 rag torches")
    check("they are in the pack", say(p, "inventory"), "rag torch")
    check("experimenting finds it too (a known recipe)", say(p, "recipe rag torch"), "cloth strip", "tallow")
    p.command("quit")
    t = new_player(chief, "lttobin", "warrior", HOLLOW + "village/workshop")
    check("Tobin sells the lantern and oil", say(t, "browse"), "storm lantern", "lamp oil")
    check("Tobin's topic", say(t, "ask tobin about lantern"), "thirty gold")
    t.command("quit")
    d = new_player(chief, "ltdun", "warrior", HOLLOW + "village/tannery")
    check("Dunstan sells tallow", say(d, "browse"), "tallow")
    d.command("quit")


def step_bin(chief):
    p = new_player(chief, "ltbin", "warrior", HOLLOW + "village/cellar")
    check("the bin is in the Tavern Cellar", say(p, "look"), "bin of old torches")
    out = say(p, "get torch from bin")
    out2 = say(p, "get torch")
    check("a character may take one rag torch", out + out2, "rag torch")
    check("it is in the pack", say(p, "inventory"), "a rag torch")
    out = say(p, "get torch")
    check("and once only", out, absent=("You take a rag torch",))
    check("the card is readable", say(p, "read card"), "HARLAND'S CARD", "The lamps end at the Smuggler Hall")
    p.command("quit")
    # a wizard takes nothing (a wizard makes no history)
    out = say(chief, "ltele ltchief " + HOLLOW + "village/cellar")
    chief.command("ltele ltchief " + HOLLOW + "village/cellar")
    out = say(chief, "get torch")
    check("a wizard is given no torch by the bin", chief.command("inventory"), absent=("rag torch",))
    chief.command("ltele ltchief hall")


def step_security(chief, mudlib):
    h.write(mudlib, "/wiz/ltchief/lmeddler", MEDDLER)
    h.write(mudlib, "/wiz/ltchief/wizlamp", WIZ_LAMP)
    h.write(mudlib, "/wiz/ltchief/wizdark", WIZ_ROOM)
    p = new_player(chief, "ltsec", "warrior", "l_dark")
    give(chief, "ltsec", RAG)
    say(p, "light torch", 2.4)
    chief.command("goto ~/wizdark")
    check("the meddler is cloned in the workshop", chief.command("clone ~/lmeddler"), "meddler")
    out = chief.command("lmeddle ltsec")
    check("the meddler ran", out, "MEDDLE ")
    check("a wizard's object cannot set the burn clock", out, "Burn=False")
    check("and it was not set", chief.command("lscale"), "SCALE 1")
    check("it cannot clear another's light", out, "Drop=called")
    check("the real torch is still lit afterwards", chief.command("lrem ltsec torch"), "lit True")
    check("a wizard's object cannot start a Glow", out, "Glow=called")
    check("the real room is unchanged by every try", str(lroom(chief, "l_dark")), "(0, 'dim', -1)")
    # A wizard's lamp in a wizard's workshop lights the workshop and only that.
    chief.command("goto ~/wizdark")
    out = chief.command("clone ~/wizlamp")
    check("a lamp can be made in the workshop", out, "Cloned /wiz/ltchief/wizlamp")
    chief.command("get wizlamp")
    chief.command("lspeed ltchief")
    check("the workshop room is dim", chief.command("lroom"), "LROOM 0 dim")
    out = say(chief, "light wizlamp", 2.4)
    check("a wizard's lamp lights the workshop (the clamp holds at +3)", chief.command("lroom"), "LROOM 3 lit")
    check("and does not light the real room", str(lroom(chief, "l_dark")), "(0, 'dim', -1)")
    chief.command("goto /domains/lighttest/hall")
    p.command("quit")


def step_content(chief):
    expect = {
        "village/cellar": ("lit", None), "cellars/collapsed_passage": ("lit", None), "cellars/storage_chamber": ("lit", None),
        "cellars/smuggler_hall": ("lit", None), "cellars/guard_post": ("dim", None), "cellars/flooded_tunnel": ("dark", "Cold water laps at your ankles."),
        "cellars/mushroom_cavern": ("dim", None), "cellars/goblin_barracks": ("dark", "The dark is thick with old straw, smoke and sour meat."),
        "cellars/hidden_cache": ("dark", "The air is dry and still, and smells of rust and old canvas."), "cellars/overseer_hall": ("dim", None), "cellars/overseer_chamber": ("lit", None),
    }
    for path, (state, darkdesc) in expect.items():
        r = lroom(chief, HOLLOW + path)
        check(f"{path}: {state}", str(r), f"'{state}'")
    p = new_player(chief, "ltcont", "warrior", HOLLOW + "village/cellar")
    give(chief, "ltcont", RAG)
    for path, (state, darkdesc) in expect.items():
        if state != "dark":
            continue
        tele(chief, "ltcont", HOLLOW + path)
        out = say(p, "look")
        check(f"{path}: the dark look carries its DarkDesc", out, "(dark)", darkdesc)
        say(p, "light torch", 2.4)
        check(f"{path}: with a torch it is dim", str(lroom(chief, HOLLOW + path)), "'dim'")
        say(p, "extinguish torch")
    for path in ("cellars/flooded_tunnel",):
        tele(chief, "ltcont", HOLLOW + path)
        say(p, "light torch", 2.4)
        out = say(p, "look")
        check("a torch makes the Flooded Tunnel dim: the line", out, "It is dim here.")
        say(p, "extinguish torch")
    tele(chief, "ltcont", HOLLOW + "cellars/smuggler_hall")
    check("the Smuggler Hall says its lantern is the last", say(p, "look"), "the only one; the rest wear their crusts of tallow")
    check("and lists the lantern", say(p, "look"), "A storm lantern hangs from a hook.")
    tele(chief, "ltcont", HOLLOW + "cellars/overseer_chamber")
    check("the Overseer Chamber has its lamp", say(p, "look"), "An iron lamp burns on the table among the maps")
    p.command("quit")


def run(work, mudlib):
    chief = h.Client("ltchief", archetype="adventurer")
    chief.read_until("> ")
    check("the secret makes the first wizard", chief.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(chief, 0.4)
    chief.command("down")
    only = [x for x in os.environ.get("LIGHT_STEPS", "").split(",") if x]
    steps = (("level", step_level), ("commands", step_commands), ("burn", step_burn), ("water", step_water), ("saving", step_saving),
             ("messages", step_messages), ("dark", step_dark), ("tohit", step_tohit), ("sneak", step_sneak), ("glow", step_glow),
             ("craft", step_craft), ("bin", step_bin), ("content", step_content))
    for name, step in steps:
        if not only or name in only:
            print(f"--- {name}")
            step(chief)
    if not only or "security" in only:
        print("--- security")
        step_security(chief, mudlib)
    return chief


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-light-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    os.makedirs(os.path.join(mudlib, "domains", "lighttest"), exist_ok=True)
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
            print(open(os.path.join(work, "driver.log")).read()[-4000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\nwhole test {time.time() - started:.0f} s")
    print(f"{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
