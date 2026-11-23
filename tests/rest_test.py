#!/usr/bin/env python3
"""Tests for the `rest` command (docs/specs/rest.md).

`rest` (and `sit`) and `stand`; the room's listing and the messages others see; the score line and the prompt token; Vitality,
Endurance and Focus recovering RestFactor (5) times as fast, measured against a standing character and with the safe room's
doubling on top; refused in a fight and refused when an aggressive creature engages on arrival; everything that ends resting
(stand, moving, attacking, using a skill, picking up, dropping, wielding, wearing, crafting or gathering, buying, selling, being
attacked) and everything that does not (look, score, inventory, vitality, say, tell, channels); the surprise delay of a seated
character who is attacked (the creature's blow lands RestSurprise seconds before the character can act); "You are fully
rested." once; the idle timer still running while resting; wizards may rest, creatures and NPCs do not; the two numbers
named once in the mudlib; and that a wizard-written object cannot put a player into or out of resting through any public method.

Boots the driver on a temporary copy of the mudlib (with the crafting sample content, for the gathering check) and a little
test content under /domains/resttest, reached by a door down from the Village Square of the copy.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=4835 python3 tests/rest_test.py
"""
import os
import re
import shutil
import socket
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4835")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402

T = "/domains/resttest/"
CT = "/domains/crafttest/obj/"
SAMPLES = os.path.join(h.ROOT, "tests", "fixtures", "crafting", "samples")
check = h.check
REST_FACTOR = 5
REST_SURPRISE = 2


# ---------------------------------------------------------------- test content

def room(cls, short, exits, body="", safe=False, extra=""):
    ex = "".join(f'        Exits["{d}"] = "{p}";\n' for d, p in exits.items())
    return f"""// @inherit /std/room
public class {cls} : Room
{{
    public override void Create()
    {{
        ShortDesc = "{short}";
        LongDesc = "A room made for the rest test.";
        Safe = {"true" if safe else "false"};
        ResetSeconds = 3000;
{ex}{body}    }}
{extra}}}
"""


HALL_INIT = """    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who != null && Interactive(who) && Present("probe", who) == null) MoveObject(CloneObject("/domains/resttest/probe"), who);
    }
"""


def creature(cls, name, ident, aggressive, reaction=2.5, article="a"):
    return f"""// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); ShortDesc = "{article} {name.lower()}"; LongDesc = "A creature made for the rest test.";
        Level = 1; MaxVitality = 50000; DamageMin = 1; DamageMax = 1; ToHit = -40; Armor = 0; Dodge = 0; XpValue = 0;
        Aggressive = {str(aggressive).lower()};
        BaseReaction = {reaction};
    }}
}}
"""


SHOPPER = """// @inherit /std/shopkeeper
public class RtShopper : Shopkeeper
{
    public override void Create()
    {
        Name = "Hob"; Ids.Add("hob"); Ids.Add("shopkeeper"); ShortDesc = "Hob the shopkeeper"; LongDesc = "He sells caps.";
        Level = 3; MaxVitality = 40;
        AddStock("/domains/resttest/cap", 3);
    }
}
"""

SWORD = """// @inherit /std/weapon
public class RtSword : Weapon
{
    public override void Create()
    {
        Name = "Test Sword"; Ids.Add("testblade"); ShortDesc = "a test sword"; LongDesc = "A plain blade.";
        DamageMin = 3; DamageMax = 4; Recovery = 0.5; Value = 4;
    }
}
"""

CAP = """// @inherit /std/armor
public class RtCap : Armor
{
    public override void Create()
    {
        Name = "Test Cap"; Ids.Add("cap"); ShortDesc = "a test cap"; LongDesc = "A cloth cap.";
        Slot = "head"; ArmorValue = 0; Value = 4;
    }
}
"""

# A trusted probe (it lives under /domains): what a player cannot do by command, for the test only.
PROBE = r'''// @inherit /std/thing
public class RtProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        foreach (var v in new[] { "rhurt", "rset", "rspawn", "rput", "rgive", "rslay", "rresting", "rhere", "rtele", "rfight" })
            AddAction(v, Do);
    }
    private ObjectRef? Named(string name, ObjectRef room) =>
        Users().FirstOrDefault(u => u.Call<string>("QueryName")!.ToLowerInvariant() == name) ?? Present(name, room);
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var room = Environment(me)!;
        switch (QueryVerb())
        {
            case "rhurt": me.Call("Heal", 99999); me.Call("TakeDamage", int.Parse(w[0]), null); Write("HURT " + me.Call<int>("QueryVitality") + "/" + me.Call<int>("QueryMaxVitality") + "\n"); return true;
            case "rset": me.Call("SetResources", int.Parse(w[0]), int.Parse(w[1])); Write("SET " + me.Call<int>("QueryEndurance") + " " + me.Call<int>("QueryFocus") + "\n"); return true;
            case "rspawn": { var ob = CloneObject("/domains/resttest/" + w[0]); MoveObject(ob, room); Write("SPAWNED " + ob.Name + "\n"); return true; }
            case "rput": { var ob = CloneObject("/domains/resttest/" + w[0]); MoveObject(ob, room); Write("PUT " + ob.Name + "\n"); return true; }
            case "rgive": { var ob = CloneObject(w[0].StartsWith("/") ? w[0] : "/domains/resttest/" + w[0]); MoveObject(ob, me); Write("GAVE " + ob.Name + "\n"); return true; }
            case "rslay":
            {
                var n = 0;
                foreach (var o in AllInventory(room).ToList())
                    if (o != me && o.IsAlive && o.Call<bool>("IsMonster")) { o.Call("Die", (ObjectRef?)null); n++; }
                Write("SLAIN " + n + "\n");
                return true;
            }
            case "rresting": { var who = Named(w[0], room); Write("RESTING " + (who != null && who.Call<bool>("QueryResting")) + "\n"); return true; }
            case "rhere": { var who = Named(w[0], room); Write("HERE " + (who != null) + "\n"); return true; }
            case "rtele": MoveObject(me, LoadObject(args.Trim())); Write("TELE ok\n"); return true;
            case "rfight":
            {
                // A creature in the room attacks me: the same call a creature's own attack makes.
                var c = Present(w[0], room);
                Write("FIGHT " + (c != null && c.Call<bool>("StartFight", me)) + "\n");
                return true;
            }
        }
        return false;
    }
}
'''

# What a wizard's object can do to a character, and does not: every public door to resting that comes to mind.
MEDDLER = r'''// @inherit /std/thing
public class Meddler : Thing
{
    public override void Create() { Name = "meddler"; Ids.Add("meddler"); }
    public override string? Short() => "a meddler";
    public override void Init() { AddAction("meddle", Meddle); }
    private bool Meddle(string args)
    {
        // CHANGED (sandbox): the meddler runs in its wizard's workshop and finds the victim wherever they are.
        var who = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "").ToLower() == args.Trim().ToLower());
        if (who == null) { Write("MEDDLE none\n"); return true; }
        var other = Present("dummy", Environment(who)!);
        var tries = new List<string>();
        tries.Add("fighting0=" + who.Call<bool>("QueryInCombat"));
        foreach (var call in new (string f, object[] a)[] {
            ("Rest", new object[0]), ("Sit", new object[0]), ("Stand", new object[0]), ("StartResting", new object[0]), ("StopResting", new object[0]),
            ("EndRest", new object[0]), ("LeaveRest", new object[0]), ("CmdRest", new object[] { "" }), ("CmdStand", new object[] { "" }),
            ("SetResting", new object[] { true }), ("SetResting", new object[] { false }), ("RestBeat", new object[0]), ("RestEnded", new object[0]),
            ("RestDisturbed", new object[] { "buy" }), ("RestDisturbed", new object[] { "sell" }), ("RestDisturbed", new object[] { "get" }),
            ("RestDisturbed", new object[] { "meddle" }), ("RestDisturbed", new object[] { "" }),
            ("Ambushed", new object[] { Self }), ("AddEnemy", new object[] { Self }), ("SettleCore", new object[0]), ("BeforeSettle", new object[0]),
            ("DelayActionBy", new object[] { 30 }), ("RecordArrival", new object[0]) })
        {
            try { tries.Add(call.f + "=" + (who.Call(call.f, call.a) ?? "null")); }
            catch (MudRuntimeException e) { tries.Add(call.f + "!" + e.Message.Length); }
        }
        if (other != null)
        {
            try { who.Call("AddEnemy", other); tries.Add("AddEnemy(creature)=done"); }
            catch (MudRuntimeException) { tries.Add("AddEnemy(creature)!"); }
        }
        // Timed effects are open to any object; they must not touch resting either.
        who.Call("AddEffect", "meddle", "Meddling", 600, "might=1", 1.0, 0);
        Write("MEDDLE " + string.Join(" ", tries) + " resting=" + who.Call<bool>("QueryResting") + " fighting=" + who.Call<bool>("QueryInCombat") + "\n");
        return true;
    }
}
'''


def test_files():
    f = {}
    f[T + "probe"] = PROBE
    f[T + "sword"] = SWORD
    f[T + "cap"] = CAP
    f[T + "shopper"] = SHOPPER
    f[T + "ambusher"] = creature("RtAmbusher", "Ambusher", "ambusher", True, article="an")
    f[T + "dummy"] = creature("RtDummy", "Dummy", "dummy", False)
    f[T + "hall"] = room("RtHall", "Rest Hall", {"up": "/domains/hollow/village/square", "north": T + "yard", "east": T + "shop",
                                                  "west": T + "calm", "south": T + "den", "northwest": T + "field"}, "", False, HALL_INIT)
    f[T + "yard"] = room("RtYard", "Rest Yard", {"south": T + "hall"})
    f[T + "calm"] = room("RtCalm", "Rest Calm", {"east": T + "hall"}, "", True)
    f[T + "shop"] = room("RtShop", "Rest Shop", {"west": T + "hall"}, f'        AddSpawn("{T}shopper");\n')
    f[T + "den"] = room("RtDen", "Rest Den", {"north": T + "hall"}, f'        AddSpawn("{T}ambusher");\n')
    f[T + "field"] = room("RtField", "Rest Field", {"southeast": T + "hall"}, '        AddNode("/domains/crafttest/obj/herb_patch");\n')
    return f


# ---------------------------------------------------------------- helpers

def login(name, calling, wiz=None):
    c = h.Client(name, archetype=calling)
    c.read_until("> ")
    if wiz is not None:
        wiz.command(f"promote {name} {wiz_level}")
        c.read_until("level is now")
    a.drain(c, 0.4)
    c.command("down")
    return c


wiz_level = 1


def resources(c):
    for _ in range(4):
        t = c.command("vitality")
        m = re.search(r"Vitality (\d+)/(\d+)   Endurance (\d+)/(\d+)   Focus (\d+)/(\d+)", t)
        if m:
            return tuple(int(x) for x in m.groups())
        time.sleep(0.3)
    return (None,) * 6


def resting(c):
    """True if the score says the character is resting."""
    return "You are resting" in c.command("score")


def timed(c, line, patterns, timeout=25.0):
    """Send a line and note, for each regex, how many seconds after the send it first appeared. Reads until all have or the timeout."""
    c.send(line)
    start = time.time()
    got, text = {}, ""
    while time.time() - start < timeout and len(got) < len(patterns):
        try:
            chunk = c.sock.recv(65536)
        except socket.timeout:
            continue
        if not chunk:
            break
        text += c.rx(chunk)
        for p in patterns:
            if p not in got and re.search(p, text):
                got[p] = time.time() - start
    return got, text


def clear_room(c):
    c.command("rslay")
    a.drain(c, 0.6)


def run(work, mudlib):
    chief = h.Client("chief", archetype="warrior")
    chief.read_until("> ")
    check("the secret makes the first wizard", chief.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(chief, 0.4)

    # The two numbers of the rule are named once, in the one place.
    source = open(os.path.join(h.ROOT, "mudlib", "std", "adventurer.cs")).read()
    check("RestFactor 5 is a named constant in adventurer.cs", "ok" if len(re.findall(r"const double RestFactor = 5(\.0)?;", source)) == 1 else "missing", "ok")
    check("RestSurprise 2 is a named constant in adventurer.cs", "ok" if len(re.findall(r"const int RestSurprise = 2;", source)) == 1 else "missing", "ok")

    mage = login("rmage", "mage")
    obs = login("robs", "adventurer")
    a.drain(obs, 0.4)

    # ===== 1. rest and stand, what everyone sees =====
    check("a character with no resting state is on its feet", mage.command("score"), absent=("You are resting",))
    check("stand while standing", mage.command("stand"), "You are already on your feet.")
    out = mage.command("rest")
    check("rest: you sit down", out, "You sit down to rest.")
    seen = a.quiet(obs, 0.6)
    check("the room sees the character sit down", seen, "Rmage", "sits down to rest.")
    check("the room lists them as resting", obs.command("look"), "is resting here.", "Rmage")
    check("score says so", mage.command("score"), "You are resting", "5 times as fast", "stand")
    check("rest again", mage.command("rest"), "You are already resting.")
    check("sit is the same as rest", mage.command("stand") + mage.command("sit"), "You stand up.", "You sit down to rest.")
    a.drain(obs, 0.6)
    out = mage.command("stand")
    check("stand: you get up", out, "You stand up.")
    check("the room sees them get up", a.quiet(obs, 0.6), "stands up.")
    check("and the listing is gone", obs.command("look"), absent=("is resting here",))
    check("standing again: score has no line", mage.command("score"), absent=("You are resting",))

    # The prompt token (read in the raw mode, which ends a reply at the prompt).
    listing = mage.command("prompt")
    check("prompt lists the rest token", listing, "{rest}", "(resting) ")
    mage.command("prompt {rest}{name}| ")
    a.drain(mage, 0.5)
    mage.send("rest")
    out = mage.read_any(("(resting) Rmage| ", "\nRmage| "), timeout=5) + a.quiet(mage, 0.3)
    check("prompt token {rest}: '(resting) ' while seated", out, "You sit down to rest.", "(resting) Rmage| ")
    mage.send("stand")
    out = mage.read_any(("Rmage| ",), timeout=5) + a.quiet(mage, 0.3)
    check("prompt token {rest}: nothing after standing", out, "You stand up.", "Rmage| ", absent=("(resting) Rmage|",))
    mage.command("prompt default")

    # ===== 2. what does not end resting =====
    mage.command("rest")
    a.drain(obs, 0.4)
    for what, line in (("look", "look"), ("look at the room's details", "look at hall"), ("score", "score"), ("inventory", "inventory"),
                       ("vitality", "vitality"), ("say", "say hello there"), ("tell", "tell robs still sitting"), ("channels", "channels"),
                       ("who", "who"), ("quests", "quests"), ("skills", "skills"), ("help", "help")):
        mage.command(line)
        check(f"does not end resting: {what}", "yes" if resting(mage) else "no", "yes")
    check("and the others hear the talk", a.quiet(obs, 0.5), "hello there")
    mage.command("stand")

    # ===== 3. what ends resting =====
    wr = login("rwar", "warrior")
    a.drain(wr, 0.3)
    wr.command("rgive sword")
    wr.command("rgive cap")

    def ends(label, setup, action, expect="", back=None):
        for line in setup:
            wr.command(line)
        wr.command("rest")
        was = resting(wr)
        out = wr.command(action)
        now = resting(wr)
        check(f"ends resting: {label}", f"{was} {now}", "True False")
        if expect:
            check(f"  and does what it should: {label}", out, expect)
        if back:
            for line in back:
                wr.command(line)

    ends("moving", [], "north", "Rest Yard", ["south"])
    ends("wielding", [], "wield testblade", "You wield")
    ends("unwielding", ["wield testblade"], "unwield", "You stop wielding")
    ends("wearing", [], "wear cap", "You put on")
    ends("removing", ["wear cap"], "remove cap", "You take off")
    ends("dropping", [], "drop cap", "You drop")
    ends("picking up", [], "get cap", "You take")
    wr.command("rput cap")
    wr.command("rest")
    out = wr.command("get nothing-of-the-kind")
    check("a failed pick-up does not end resting", "yes" if resting(wr) else "no", "yes")
    wr.command("stand")
    # attacking
    wr.command("rspawn dummy")
    wr.command("rest")
    check("seated before the attack", "yes" if resting(wr) else "no", "yes")
    out = wr.command("kill dummy")
    check("attacking ends resting", out, "You attack a dummy")
    check("and the character is on its feet", "yes" if not resting(wr) else "no", "yes")
    check("rest is refused in a fight", wr.command("rest"), "You are fighting!", "cannot rest")
    clear_room(wr)
    check("a fight over, resting is possible again", wr.command("rest"), "You sit down to rest.")
    wr.command("stand")

    # using a skill (a priest's Minor Heal on oneself)
    pr = login("rpriest", "priest")
    pr.command("rhurt 10")
    pr.command("rest")
    out = pr.command("heal")
    check("a skill ends resting (Minor Heal on yourself)", out, "murmurs a prayer")
    check("  and the priest is on its feet", "yes" if not resting(pr) else "no", "yes")

    # buying and selling
    wr.command("east")
    check("the shopkeeper is here", wr.command("look"), "Hob")
    wr.command("rest")
    check("a failed purchase does not end resting", wr.command("buy lantern") and ("yes" if resting(wr) else "no"), "yes")
    out = wr.command("buy cap")
    check("buying ends resting", out, "You buy a test cap for 3 gold")
    check("  and the character is on its feet", "yes" if not resting(wr) else "no", "yes")
    wr.command("rest")
    out = wr.command("sell cap")
    check("selling ends resting", out, "You sell a test cap")
    check("  and the character is on its feet", "yes" if not resting(wr) else "no", "yes")
    wr.command("west")

    # ===== 4. an aggressive creature engages: refused, and the one who is seated gets up =====
    out = wr.command("south")
    check("an aggressive creature engages on arrival", out, "attacks you!")
    check("rest is refused when it has engaged", wr.command("rest"), "You are fighting!", "cannot rest")
    clear_room(wr)
    wr.command("north")

    # ===== 5. wizards may rest; creatures and NPCs do not =====
    wz = login("rwizard", "warrior", chief)
    check("a wizard may rest", wz.command("rest"), "You sit down to rest.")
    check("and stand", wz.command("stand"), "You stand up.")
    wr.command("east")
    check("an NPC is never resting", wr.command("rresting hob"), "RESTING False")
    wr.command("west")
    wr.command("rspawn dummy")
    check("nor is a creature", wr.command("rresting dummy"), "RESTING False")
    clear_room(wr)

    # ===== 6. the rate, for each resource =====
    # The priest at level 1: Vitality 0.9, Endurance 3.72 and Focus 3.25 a ten seconds when standing (tests/attribute_test.py).
    pr.command("rset 0 0")
    t0 = time.time()
    time.sleep(8)
    _, _, e0, _, f0, _ = resources(pr)
    d = time.time() - t0
    stand_e, stand_f = e0, f0
    check(f"standing: Endurance regains about 3.72 a 10 s ({stand_e} in {d:.1f} s)", "ok" if abs(stand_e - 0.372 * d) <= 1.6 else f"{stand_e}", "ok")
    check(f"standing: Focus regains about 3.25 a 10 s ({stand_f} in {d:.1f} s)", "ok" if abs(stand_f - 0.325 * d) <= 1.6 else f"{stand_f}", "ok")
    pr.command("rest")
    pr.command("rset 0 0")
    t0 = time.time()
    time.sleep(8)
    _, _, e1, _, f1, _ = resources(pr)
    d = time.time() - t0
    check(f"resting: Endurance regains {REST_FACTOR} x 3.72 a 10 s ({e1} in {d:.1f} s)", "ok" if abs(e1 - REST_FACTOR * 0.372 * d) <= 3.0 else f"{e1}", "ok")
    check(f"resting: Focus regains {REST_FACTOR} x 3.25 a 10 s ({f1} in {d:.1f} s)", "ok" if abs(f1 - REST_FACTOR * 0.325 * d) <= 3.0 else f"{f1}", "ok")
    pr.command("stand")
    # The safe room doubles it again: they multiply.
    pr.command("west")
    pr.command("rset 0 0")
    pr.command("rest")
    pr.command("rset 0 0")
    t0 = time.time()
    time.sleep(5)
    _, _, e2, _, f2, _ = resources(pr)
    d = time.time() - t0
    check(f"resting in a safe room: Endurance regains 2 x {REST_FACTOR} x 3.72 a 10 s ({e2} in {d:.1f} s)", "ok" if abs(e2 - 2 * REST_FACTOR * 0.372 * d) <= 3.5 else f"{e2}", "ok")
    check(f"resting in a safe room: Focus regains 2 x {REST_FACTOR} x 3.25 a 10 s ({f2} in {d:.1f} s)", "ok" if abs(f2 - 2 * REST_FACTOR * 0.325 * d) <= 3.5 else f"{f2}", "ok")
    pr.command("stand")
    pr.command("east")
    # Vitality: three ten-second steps (priest 0.9 a step standing).
    pr.command("rhurt 20")
    v0 = resources(pr)[0]
    time.sleep(32)
    v1 = resources(pr)[0]
    stand_gain = v1 - v0
    pr.command("rhurt 20")
    pr.command("rest")
    v0 = resources(pr)[0]
    time.sleep(32)
    v1 = resources(pr)[0]
    rest_gain = v1 - v0
    pr.command("stand")
    check(f"standing: Vitality regains about 0.9 a 10 s (+{stand_gain} in 32 s)", "ok" if 1 <= stand_gain <= 3 else f"{stand_gain}", "ok")
    check(f"resting: Vitality regains about {REST_FACTOR} x 0.9 a 10 s (+{rest_gain} in 32 s)", "ok" if 8 <= rest_gain <= 14 else f"{rest_gain}", "ok")
    check("resting is at least three times as fast as standing", "ok" if rest_gain >= 3 * max(1, stand_gain) else f"{rest_gain} vs {stand_gain}", "ok")

    # ===== 7. fully rested, once =====
    wr.command("rhurt 1")
    wr.command("rset 999 999")
    wr.command("rest")
    text = a.quiet(wr, 16)
    check("fully rested: told once, when all three are full", "ok" if text.count("You are fully rested.") == 1 else f"{text.count('You are fully rested.')}: {text!r}", "ok")
    text = a.quiet(wr, 6)
    check("fully rested: and not told again", "ok" if "fully rested" not in text else text, "ok")
    check("fully rested: the character stays seated", "yes" if resting(wr) else "no", "yes")
    wr.command("stand")

    # ===== 8. being attacked: the surprise =====
    AMBUSH = [r"An ambusher attacks you!", r"An ambusher (?!attacks you)[a-z]+ you|You dodge an ambusher", r"(?<![A-Za-z])You (?!attack |scramble)\w+ an ambusher|dodges your attack"]

    def ambushed(client, seated):
        client.command("rhurt 0")
        client.command("rset 5 5")
        if seated:
            client.command("rest")
        got, text = timed(client, "rspawn ambusher", AMBUSH, timeout=20)
        a.quiet(client, 0.5)
        return got, text

    sc = login("rscout", "scout")
    sc.command("north")  # alone in the yard: a creature that fights several characters hits only the first
    standing_got, standing_text = ambushed(sc, False)
    clear_room(sc)
    a.drain(sc, 1.0)
    time.sleep(2)
    seated_got, seated_text = ambushed(sc, True)
    have = len(standing_got) == 3 and len(seated_got) == 3
    check("attacked while standing: the creature and the character both act", "ok" if have else f"{standing_got} {seated_got}", "ok")
    if have:
        k = AMBUSH
        margin_standing = standing_got[k[2]] - standing_got[k[1]]
        margin_seated = seated_got[k[2]] - seated_got[k[1]]
        check(f"the character's first action comes {REST_SURPRISE} s later when seated (standing {margin_standing:.1f} s after the creature's blow, seated {margin_seated:.1f} s)",
              "ok" if abs((margin_seated - margin_standing) - REST_SURPRISE) <= 0.6 else f"{margin_seated - margin_standing:.2f}", "ok")
        check("seated: the creature's blow lands before the character's first action", "ok" if margin_seated > 1.0 else f"{margin_seated:.2f}", "ok")
    check("being attacked: the character scrambles up", seated_text, "You scramble to your feet!")
    check("  and is no longer resting", "yes" if not resting(sc) else "no", "yes")
    clear_room(sc)
    sc.command("south")

    # ===== 9. a wizard's object cannot put a player into or out of resting =====
    os.makedirs(os.path.join(mudlib, "wiz", "chief"), exist_ok=True)
    h.write(mudlib, "/wiz/chief/meddler", MEDDLER)
    vic = login("rvictim", "mage")
    vic.command("rspawn dummy")
    h.write(mudlib, "/wiz/chief/den", "// @inherit /std/room\npublic class ChiefDen : Room { public override void Create() { ShortDesc = \"Chief's Den\"; LongDesc = \"A workshop.\"; } }\n")
    chief.command("goto /wiz/chief/den")  # CHANGED (sandbox): a wizard's code runs in their workshop
    a.drain(chief, 0.4)
    check("the meddler is cloned", chief.command("clone ~/meddler"), "meddler")
    # an aggressive creature in the room, for the call that names it as an enemy
    vic.command("rslay")
    vic.command("rspawn dummy")
    vic.command("rest")
    check("the victim sits", "yes" if resting(vic) else "no", "yes")
    out = chief.command("meddle rvictim")
    check("the meddler ran", out, "MEDDLE ")
    check("no public method of a seated character ends its resting", out, "resting=True", "fighting=False")
    check("the seated character is still resting (score)", "yes" if resting(vic) else "no", "yes")
    vic.command("stand")
    out = chief.command("meddle rvictim")
    check("no public method puts a standing character down", out, "resting=False", "fighting=False")
    check("the standing character is still on its feet (score)", "yes" if not resting(vic) else "no", "yes")
    chief.command("goto " + T + "hall")
    vic.command("rslay")

    # ===== 10. crafting or gathering ends resting =====
    wr2 = login("rgather", "warrior")
    wr2.command("northwest")
    check("in the field", wr2.command("look"), "Rest Field")
    wr2.command(f"rgive {CT}harvest_sickle")
    out = wr2.command("inventory")
    got_sickle = "sickle" in out
    if got_sickle:
        wr2.command("rest")
        wr2.send("harvest patch")
        text = wr2.read_any(("You harvest", "fumble", "need", "nothing"), timeout=20) + a.quiet(wr2, 0.5)
        check("gathering ends resting", "yes" if not resting(wr2) else "no", "yes")
    else:
        check("a sickle for the gathering check", out, "sickle")

    for c in (mage, obs, wr, pr, wz, sc, vic, wr2):
        try:
            c.command("quit")
        except OSError:
            pass
    return chief


# ---------------------------------------------------------------- the idle timer (a second driver, with short times)

def run_idle(work, mudlib):
    sync_before, h.Client.sync_default = h.Client.sync_default, False
    try:
        chief = h.Client("chief", archetype="warrior")
        chief.read_until("> ")
        a.drain(chief, 0.4)
        sitter = h.Client("rsitter", archetype="warrior")
        sitter.read_until("> ")
        a.drain(sitter, 0.4)
        sitter.command("rest")
        check("idle: the character is resting", sitter.command("score"), "You are resting")
        # nothing typed from here on: resting is no input
        check("idle: a warning comes while resting", sitter.read_until("idle for a while", timeout=20), "You have been idle for a while", "disconnected")
        check("idle: then the disconnect, resting or not", sitter.read_until("idle for too long", timeout=20), "saved and you are disconnected")
        check("idle: and the connection closes", "ok" if sitter.closed(timeout=10) else "still open", "ok")
        chief.command("shutdown")
    finally:
        h.Client.sync_default = sync_before


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-rest-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    for gone in ("crafting/launch_materials.cs", "recipes/launch.cs"):
        os.remove(os.path.join(mudlib, gone))
    shutil.copytree(SAMPLES, mudlib, dirs_exist_ok=True)
    os.makedirs(os.path.join(mudlib, "domains", "resttest"), exist_ok=True)
    for path, source in test_files().items():
        h.write(mudlib, path, source)
    h.edit(mudlib, "/domains/hollow/village/square", 'Exits["south"]', f'Exits["down"] = "{T}hall";\n        Exits["south"]')
    proc, log = a.start_driver(work, mudlib)
    started = time.time()
    try:
        chief = run(work, mudlib)
        chief.command("save")
        chief.command("shutdown")
        time.sleep(1.5)
        proc.wait(timeout=15)
        # The idle timer: the same mudlib and saves, with the two times in seconds.
        os.environ["LESMUDOS_IDLE_MINUTES"] = "7s"
        os.environ["LESMUDOS_TIMEOUT_MINUTES"] = "14s"
        proc, log = a.start_driver(work, mudlib)
        run_idle(work, mudlib)
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
