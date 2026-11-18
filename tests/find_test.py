#!/usr/bin/env python3
"""End-to-end test of finds: treasure in hidden places that each character takes once, for less each time.

The rule: each character can take a find once and never again, and each new finder gets less than the one before,
until nothing is left; it never refills.

Starts the driver against a temporary copy of the mudlib, writes test finds, rooms and probes under /domains/ftest and
under /wiz/probe (a wizard's sandbox), and plays real, non-wizard characters over TCP. It proves the amounts exactly, the
once-per-character rule, wizards, items, the sandbox and trusted-caller checks, the record on disk (save/secure/finds.json)
through a clean restart, a killed driver, a quit, simultaneous takes and `update` of the find and of its room, the copy-ready
example of docs/BUILDING.md, and then the five converted places of the Corridor with real characters.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4827 python3 tests/find_test.py
"""
import json
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4827")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_test as a  # noqa: E402
import smoke_test as h  # noqa: E402

T = "/domains/ftest/"
COR = "/domains/greenvale/corridor/"
RECORD = None  # path of save/secure/finds.json, set in main
CHECKS = [0]


def check(label, text, *expected, absent=()):
    CHECKS[0] += 1
    h.check(label, text, *expected, absent=absent)


def table(first, percent=60, minimum=1, n=12):
    """What the finders get by the rule: the first amount, then percent of the one before, each rounded down, until
    below the minimum."""
    out, amount = [], first
    while amount >= minimum and len(out) < n:
        out.append(amount)
        amount = amount * percent // 100
    return out


# ---------------------------------------------------------------- test content

def find_src(cls, name, ids, gold, fall=None, minimum=None, item=None, finders=None, extra=""):
    lines = [f'        Name = "{name}";'] + [f'        Ids.Add("{i}");' for i in ids]
    lines += [f'        ShortDesc = "a {name.lower()}";', f'        LongDesc = "A {name.lower()}, full to the brim.";']
    lines += [f'        TakenShort = "a {name.lower()} you have emptied";', f'        EmptyShort = "a {name.lower()}, picked clean";']
    lines += [f'        Gold = {gold};']
    if fall is not None:
        lines.append(f"        FallOff = {fall};")
    if minimum is not None:
        lines.append(f"        MinimumGold = {minimum};")
    if item:
        lines.append(f'        ItemPath = "{item}";')
    if finders is not None:
        lines.append(f"        ItemFinders = {finders};")
    body = "\n".join(lines)
    return f"""// @inherit /std/find
public class {cls} : Find
{{
    public override void Create()
    {{
{body}
    }}
{extra}}}
"""


def room_src(cls, short, exits, spawns=(), extra_create=""):
    ex = "".join(f'        Exits["{d}"] = "{p}";\n' for d, p in exits.items())
    sp = "".join(f'        AddSpawn("{p}");\n' for p in spawns)
    return f"""// @inherit /std/room
public class {cls} : Room
{{
    public override void Create()
    {{
        ShortDesc = "{short}";
        LongDesc = "A room made for the find test.";
{ex}{sp}{extra_create}    }}
}}
"""


PROBE = """// @inherit /std/thing
public class FtProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        AddAction("tele", Tele);
        AddAction("carryclaim", CarryClaim);
    }
    private bool Tele(string args)
    {
        var who = ThisPlayer()!;
        MoveObject(who, LoadObject(args.Trim()));
        who.Call("Look");
        return true;
    }
    // A probe in a pocket asks the register: the register must refuse, because the caller is not lying in the character's room.
    private bool CarryClaim(string args)
    {
        var r = LoadObject("/secure/finds").Call<List<int>>("Claim", ThisPlayer()!, 1000, 60, 1, false, 0);
        Write("CLAIM g=" + r[0] + " i=" + r[1] + " c=" + r[2] + "\\n");
        return true;
    }
}
"""

# A trusted object lying in the room asks for itself: allowed, and keyed by its own path, so it cannot touch any other stash.
FORGER = """// @inherit /std/thing
public class FtForger : Thing
{
    public override void Create() { Name = "Forger"; Ids.Add("forger"); }
    public override string? Short() => "a forger";
    public override void Init()
    {
        AddAction("forge", Forge);
        AddAction("forgeother", ForgeOther);
    }
    private bool Forge(string args)
    {
        var r = LoadObject("/secure/finds").Call<List<int>>("Claim", ThisPlayer()!, 1000, 60, 1, false, 0);
        Write("CLAIM g=" + r[0] + " i=" + r[1] + " c=" + r[2] + "\\n");
        return true;
    }
    private bool ForgeOther(string args)
    {
        var other = Users().FirstOrDefault(u => u.Call<string>("QueryName") == args.Trim());
        if (other == null) { Write("CLAIM nobody\\n"); return true; }
        var r = LoadObject("/secure/finds").Call<List<int>>("Claim", other, 1000, 60, 1, false, 0);
        Write("CLAIM g=" + r[0] + " i=" + r[1] + " c=" + r[2] + "\\n");
        return true;
    }
}
"""

# The same, from a wizard's sandbox: refused.
MINTER = (FORGER.replace("FtForger", "WizMinter").replace('AddAction("forge", Forge)', 'AddAction("mint", Forge)').replace('AddAction("forgeother", ForgeOther)', 'AddAction("mintother", ForgeOther)')
          .replace('Name = "Forger"', 'Name = "Minter"').replace('Ids.Add("forger")', 'Ids.Add("minter")').replace("a forger", "a minter"))

TRINKET = """// @inherit /std/item
public class FtTrinket : Item
{
    public override void Create()
    {
        Name = "Trinket";
        Ids.Add("trinket");
        ShortDesc = "a brass trinket";
        LongDesc = "A brass trinket.";
        Value = 3;
    }
}
"""

HUB = """// @inherit /std/room
public class FtHub : Room
{
    public override void Create()
    {
        ShortDesc = "Find Test Hub";
        LongDesc = "A room made for the find test.";
        Exits["up"] = "/domains/hollow/village/square";
        Exits["vault"] = "/domains/ftest/vault";
        Exits["wiz"] = "/wiz/probe/room";
        Exits["example"] = "/domains/example/purse_room";
        MoveObject(CloneObject("/domains/ftest/forger"), Self);
        MoveObject(CloneObject("/wiz/probe/minter"), Self);
    }
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who != null && Interactive(who) && Present("probe", who) == null) MoveObject(CloneObject("/domains/ftest/probe"), who);
    }
}
"""


def files():
    f = {}
    f["probe"] = PROBE
    f["forger"] = FORGER
    f["trinket"] = TRINKET
    f["hub"] = HUB
    stashes = ["stash", "stash_b", "stash_c", "potion_stash", "keepsake", "relic", "bad_item", "tin"]
    f["vault"] = room_src("FtVault", "Find Test Vault", {"out": T + "hub"}, [T + s for s in stashes])
    f["stash"] = find_src("FtStash", "Test Stash", ["stash", "hoard"], 100)
    f["stash_b"] = find_src("FtStashB", "Test Cache", ["cache"], 100)
    f["stash_c"] = find_src("FtStashC", "Test Vase", ["vase"], 50)
    f["potion_stash"] = find_src("FtPotionStash", "Test Jar", ["jar"], 10, fall=50, item="/domains/hollow/obj/healing_potion", finders=2)
    f["keepsake"] = find_src("FtKeepsake", "Test Charm", ["charm"], 0, item=T + "trinket")
    f["relic"] = find_src("FtRelic", "Test Relic", ["relic"], 0, item=T + "trinket", finders=2)
    f["bad_item"] = find_src("FtBadItem", "Test Bag", ["bag"], 5, item=T + "missing")
    f["tin"] = find_src("FtTin", "Test Tin", ["tin"], 40, fall=50, minimum=10)
    f["stash_wizhost"] = find_src("FtStashWizHost", "Test Hosted Stash", ["hosted"], 500)
    return f


WIZ_ROOM = room_src("WizProbeRoom", "Wizard Probe Room", {"out": T + "hub"}, ["/wiz/probe/wizstash", T + "stash_wizhost"])


def gold(c):
    m = re.search(r"Gold (\d+)", c.command("score"))
    return int(m.group(1)) if m else -1


def take(c, word, verb="get"):
    """Take a find; return (text, gold gained)."""
    before = gold(c)
    text = c.command(f"{verb} {word}")
    return text, gold(c) - before


def record():
    with open(RECORD) as fh:
        return json.load(fh)["found"]


def login(name):
    c = h.Client(name, archetype="warrior")
    c.read_until("> ")
    return c


def to_hub(c):
    c.command("testdoor")


# ---------------------------------------------------------------- the test

def ok(label, condition, detail=""):
    """A check on a Python condition."""
    CHECKS[0] += 1
    print(("PASS  " if condition else "FAIL  ") + label)
    if not condition:
        h.failures.append(label)
        print("      ", detail)


def relog(name):
    """Log a character in again (after a quit or a restart) and walk to the vault."""
    c = h.Client(name, archetype="warrior")
    c.read_until("> ")
    return c


def enter(c, where="vault"):
    c.command("testdoor")
    c.command(where)


def run(work, mudlib, box):
    wiz = h.Client("Warden")
    wiz.read_until("> ")
    check("the secret makes the first wizard", wiz.command("promote " + h.SECRET), "level 10 wizard")

    names = ["Alice", "Bob", "Carol", "Dave", "Erin", "Fay", "Gus", "Hal", "Ivy", "Jo"]
    low = [n.lower() for n in names]
    P = [login(n) for n in names]
    for c in P:
        enter(c)
    # ================= the amounts: a stash of 100 at 60 percent =================
    expected = table(100)
    check("the rule gives 100, 60, 36, 21, 12, 7, 4, 2, 1 and then nothing", str(expected), "[100, 60, 36, 21, 12, 7, 4, 2, 1]")
    check("a character who has not taken it sees the full line in the room", P[0].command("look"), "A test stash is here.")
    check("look at it: the full description", P[0].command("look at stash"), "full to the brim")
    g0 = gold(P[0])
    check("it cannot be picked up as an object (get all leaves it)", P[0].command("get all"), absent=("You take",))
    ok("and get all changes no purse", gold(P[0]) == g0)
    for i, amount in enumerate(expected):
        text, gained = take(P[i], "stash", ["get", "take", "loot"][i % 3])
        check(f"stash finder {i + 1} is told {amount} gold", text, f"You take {amount} gold from the test stash.")
        ok(f"stash finder {i + 1} gets exactly {amount} gold in the purse", gained == amount, gained)
    text, gained = take(P[9], "stash")
    check("the tenth finder finds it empty and is told someone has been here", text, "Someone has been here before you. Nothing is left.")
    ok("and gets nothing", gained == 0, gained)
    text, gained = take(P[0], "stash")
    ok("a repeat take gives nothing", gained == 0, gained)
    check("a repeat take is told so in one line", text, "You have already taken what was here")
    check("a repeat take is not told it is empty", text, absent=("Someone has been here",))
    check("someone who took it sees it as taken by them", P[3].command("look"), "A test stash you have emptied is here.")
    check("someone who found it empty sees it picked clean", P[9].command("look"), "A test stash, picked clean is here.")
    check("look at it, for a character who has taken it: no promise of treasure", P[1].command("look at stash"), "nothing more in it for you", absent=("full to the brim",))
    check("look at it, for a character who finds it empty", P[9].command("look at stash"), "Someone has been here before you. Nothing is left.", absent=("full to the brim",))
    rec = record()
    ok("the record lists the nine finders by name, in order", rec[T + "stash"] == low[:9], rec[T + "stash"])
    ok("the character who found it empty is not recorded", "jo" not in rec[T + "stash"])
    ok("another stash is another record", T + "stash_b" not in rec)

    # ================= wizards look and take nothing; they are not counted =================
    wiz.command("goto " + T + "vault")
    a.drain(wiz, 0.3)
    check("a wizard sees the full description of a find with something left", wiz.command("look at tin"), "full to the brim")
    before = gold(wiz)
    check("a wizard is told in one line that wizards take nothing", wiz.command("get tin"), "wizards take nothing from the world and leave no mark on it")
    ok("a wizard receives no gold", gold(wiz) == before)
    ok("a wizard is not recorded", T + "tin" not in record())
    check("stat on a find: its numbers and its count", wiz.command("stat tin"), "Find id /domains/ftest/tin: first 40 gold, 50% kept", "0 finder(s) so far")
    text, gained = take(P[0], "tin")
    ok("the next character after a wizard is the first finder: 40 gold", gained == 40, gained)
    for i, amount in enumerate([20, 10], start=1):
        text, gained = take(P[i], "tin")
        ok(f"tin (minimum 10): finder {i + 1} gets {amount}", gained == amount, gained)
    text, gained = take(P[3], "tin")
    check("tin: below the minimum the find is empty", text, "Someone has been here before you")
    ok("tin: and pays nothing", gained == 0)
    check("a wizard who comes after is told the same as before", wiz.command("get tin"), "wizards take nothing")
    ok("the register holds no wizard", "warden" not in json.dumps(record()))
    check("stat on a find after three finders", wiz.command("stat tin"), "3 finder(s) so far")

    # ================= another file is another stash =================
    for i, amount in enumerate(table(100)[:2]):
        text, gained = take(P[i + 4], "cache")
        ok(f"a second file is its own stash (cache, finder {i + 1} gets {amount})", gained == amount, gained)

    # ================= simultaneous takes =================
    for c in P[:4]:
        c.send("get vase")
    results = [c.read_until("> ") for c in P[:4]]
    gains = sorted(int(m.group(1)) for t in results for m in [re.search(r"You take (\d+) gold", t)] if m)
    ok("four characters taking in the same moment: four distinct amounts, the first four of the rule", gains == sorted(table(50)[:4]), gains)
    ok("four at once: the register has exactly four names for the vase", len(record()[T + "stash_c"]) == 4)

    # ================= items =================
    texts, golds = [], []
    for i in range(5):
        t, g = take(P[i], "jar")
        texts.append(t)
        golds.append(g)
    ok("jar (10 gold, 50 percent): 10, 5, 2, 1, then empty", golds == [10, 5, 2, 1, 0], golds)
    check("jar: the first finder also gets the item", texts[0], "You also take a minor healing potion.")
    check("jar: the second finder also gets the item", texts[1], "You also take a minor healing potion.")
    check("jar: the third finder gets gold but no item (limit 2)", texts[2], "You take 2 gold", absent=("healing potion",))
    check("jar: the fourth finder gets gold but no item", texts[3], "You take 1 gold", absent=("healing potion",))
    check("jar: the fifth finds it empty", texts[4], "Someone has been here")
    check("the first finder has the item in the inventory", P[0].command("i"), "minor healing potion")
    check("the third finder has none from it", P[2].command("i"), absent=("healing potion",))
    t, g = take(P[0], "jar")
    check("an item find cannot be had twice", t, "You have already taken")
    ok("and the item is not given again", P[0].command("i").count("minor healing potion") == 1)

    # keepsake: no gold, one each, no end
    bad = []
    for i, c in enumerate(P):
        t, g = take(c, "charm")
        if "You also take a brass trinket." not in t or g != 0 or "gold" in t:
            bad.append((i, t, g))
    ok("charm (no gold, no end): all ten characters get one trinket and no gold", not bad, bad)
    t, g = take(P[0], "charm")
    check("charm: a second take gets nothing", t, "You have already taken")
    ok("charm: the character has exactly one trinket", P[0].command("i").count("brass trinket") == 1)
    late = login("Late")
    enter(late)
    t, g = take(late, "charm")
    check("charm: the eleventh character still gets one (no end)", t, "You also take a brass trinket.")
    check("charm: a keepsake that was taken shows as taken", late.command("look"), "A test charm you have emptied is here.")
    t0, _ = take(P[0], "relic")
    t1, _ = take(P[1], "relic")
    t2, _ = take(P[2], "relic")
    check("relic (limit 2): the first gets one", t0, "You also take a brass trinket.")
    check("relic (limit 2): the second gets one", t1, "You also take a brass trinket.")
    check("relic (limit 2): the third finds it empty", t2, "Someone has been here before you")
    t, g = take(P[0], "bag")
    ok("bag: gold is paid even when the item file is gone", g == 5, g)
    check("bag: no error reaches the player", t, absent=("***",))

    # ================= the sandbox and the trusted-caller checks =================
    P[0].command("out")
    check("back in the hub", P[0].command("look"), "Find Test Hub")
    check("a /wiz object asking the register for gold is refused (code 4)", P[0].command("mint"), "CLAIM g=0 i=0 c=4")
    check("a trusted object lying in the room is paid under its own path (a legitimate find)", P[0].command("forge"), "CLAIM g=1000 i=0 c=0")
    check("and the same character a second time gets nothing (code 1)", P[0].command("forge"), "CLAIM g=0 i=0 c=1")
    check("a probe in a pocket is refused: it does not lie in the character's room (code 5)", P[0].command("carryclaim"), "c=5")
    check("asking for someone else's character is refused (code 5)", P[0].command("forgeother Bob"), "c=5")
    rec = record()
    ok("the forger's claim is recorded under the forger's own path only", rec.get(T + "forger") == ["alice"], rec.get(T + "forger"))
    ok("the forger cannot touch any other stash: the stash record is as it was", rec[T + "stash"] == low[:9])
    ok("the register holds no /wiz id", not any(k.startswith("/wiz") for k in rec))
    P[0].command("wiz")
    check("a /wiz room is reachable (control)", P[0].command("look"), "Wizard Probe Room")
    mid = gold(P[0])
    check("a find under /wiz is refused: the character is told nothing comes of it", P[0].command("get wizstash"), "Nothing comes of it.")
    ok("a find under /wiz pays no gold", gold(P[0]) == mid)
    check("a trusted find lying in a /wiz room is refused too", P[0].command("get hosted"), "Nothing comes of it.")
    ok("and pays no gold", gold(P[0]) == mid)
    rec = record()
    ok("the register holds no /wiz id and no hosted find", not any(k.startswith("/wiz") or k.endswith("stash_wizhost") for k in rec))
    check("look at a /wiz find: described in full (nothing was taken)", P[0].command("look at wizstash"), "full to the brim")
    P[0].command("out")
    P[0].command("vault")

    # ================= the copy-ready example of docs/BUILDING.md =================
    ex = login("Example")
    to_hub(ex)
    ex.command("example")
    check("example: the room loads and lists the find", ex.command("look"), "Example Purse Room", "A mouldering example purse is here.")
    t, g = take(ex, "purse")
    ok("example: the first finder gets the stated amount, 20", g == 20, g)
    check("example: a repeat gets nothing", ex.command("get purse"), "You have already taken")
    ex3 = login("Exampletwo")
    to_hub(ex3)
    ex3.command("example")
    t, g = take(ex3, "purse")
    ok("example: the second finder gets 12", g == 12, g)

    # ================= the converted places of the Corridor =================
    places = [
        ("Dry Well", COR + "south_road/s1", "purse", 30),
        ("Sealed Culvert", COR + "approach/s1", "coins", 70),
        ("Borrowed Tomb", COR + "barrow/borrowed_tomb", "coins", 150),
        ("Cold Larder", COR + "root_warrens/larder", "chest", 60),
    ]
    walkers = [login("Walker" + n) for n in names]
    for c in walkers:
        to_hub(c)
    table8 = {}
    for title, path, word, first in places:
        want = table(first)
        got = []
        for i, c in enumerate(walkers[:8]):
            c.command("tele " + path)
            if i == 0:
                check(f"{title}: a first character sees the find in the room", c.command("look"), "is here.")
            if i == len(want):
                check(f"{title}: once it is empty, a character who has not taken it is shown it emptied", c.command("look at " + word), "Someone has been here before you")
            text, g = take(c, word, ["get", "take", "loot"][i % 3])
            got.append(g)
            if i == 0:
                check(f"{title}: the first finder is told how much ({first})", text, f"{first} gold")
                check(f"{title}: and then sees it as taken by them", c.command("look"), "is here.", absent=("Someone has been here",))
            if i == len(want):
                check(f"{title}: after the last amount the next finder is told someone has been here", text, "Someone has been here before you")
            c.command("tele " + T + "hub")
        table8[title] = got
        eight = (want + [0] * 8)[:8]
        ok(f"{title}: the first eight finders get {eight}", got == eight, got)
    # the larder's potion: one for each of the first five finders
    potions = [c.command("i").count("minor healing potion") for c in walkers[:8]]
    ok("the larder gave a healing potion to each of the first five finders only", potions == [1, 1, 1, 1, 1, 0, 0, 0], potions)
    # a repeat at each place
    for title, path, word, first in places:
        c = walkers[0]
        c.command("tele " + path)
        before = gold(c)
        check(f"{title}: a repeat by the first finder says so", c.command("get " + word), "You have already taken")
        ok(f"{title}: and the purse is unchanged", gold(c) == before)
        c.command("tele " + T + "hub")
    # the bowl token: one each, no end
    tokens = []
    for i, c in enumerate(walkers):
        c.command("tele " + COR + "barrow/bowl_tomb")
        text, g = take(c, "token")
        tokens.append(("You lift a flat stone token" in text, g))
        again_text = c.command("get token")
        if i in (0, 9):
            check(f"bowl: character {i + 1} cannot take a second", again_text, "You have already taken")
        c.command("tele " + T + "hub")
    ok("bowl: all ten characters got a token and no gold", tokens == [(True, 0)] * 10, tokens)
    ok("bowl: each has exactly one token in the inventory", [c.command("i").count("flat stone token") for c in walkers] == [1] * 10)
    rec = record()
    ids = {COR + "south_road/obj/old_purse": 6, COR + "approach/obj/old_coins": 8, COR + "barrow/obj/tomb_coins": 8,
           COR + "root_warrens/obj/larder_chest": 8, COR + "barrow/obj/bowl_token": 10}
    for k, n in ids.items():
        ok(f"the register records {k.split('/')[-1]}: {n} finders", len(rec.get(k, [])) == n, rec.get(k))
    print("TABLE", json.dumps(table8))
    wiz.command("goto " + COR + "south_road/s1")
    a.drain(wiz, 0.3)
    check("the Dry Well names the find in its stat", wiz.command("stat here"), "old_purse")
    wiz.command("goto " + COR + "root_warrens/larder")
    a.drain(wiz, 0.3)
    text = wiz.command("look")
    check("the larder keeps its description (the chest under its scrap of hide)", text, "a small chest lies under a scrap of hide")
    check("a wizard sees the larder's chest as it is: nothing is left after eight finders", text, "A small chest, its lid propped open is here.")

    # ================= quit, kill, restart, update =================
    quitter = login("Quitter")
    enter(quitter)
    before = gold(quitter)
    want = table(50)[4]
    check(f"quitter: the fifth finder of the vase gets {want}", quitter.command("get vase"), f"You take {want} gold")
    quitter.send("quit")
    quitter.read_until("Goodbye", 5)
    time.sleep(0.5)
    quitter = relog("Quitter")
    ok("quitter: after quitting straight away, the gold is kept", gold(quitter) - before == want, (gold(quitter), before))
    enter(quitter)
    check("quitter: taking it again after the quit gives nothing", quitter.command("get vase"), "You have already taken")
    killed = login("Killed")
    enter(killed)
    before = gold(killed)
    want = table(50)[5]
    check(f"killed: the sixth finder of the vase gets {want}", killed.command("get vase"), f"You take {want} gold")
    ok("killed: the register is on disk before the driver is killed", "killed" in record()[T + "stash_c"])
    a.restart(work, mudlib, box, "kill")
    time.sleep(0.5)
    killed = relog("Killed")
    ok("killed: the driver was killed without a quit and the gold was already saved", gold(killed) - before == want, (gold(killed), before))
    enter(killed)
    check("killed: the record survived the kill; they cannot take it again", killed.command("get vase"), "You have already taken")
    second = login("Second")
    enter(second)
    t, g = take(second, "vase")
    ok("after a killed driver the next finder gets the next amount", g == table(50)[6], g)
    ok("the register after the kill lists seven names for the vase", len(record()[T + "stash_c"]) == 7)
    # the stash and the others after the kill
    alice = relog("Alice")
    enter(alice)
    check("after the kill alice still has taken the stash", alice.command("get stash"), "You have already taken")
    # clean restart
    wiz = relog("Warden")
    wiz.command("goto " + T + "vault")
    a.restart(work, mudlib, box, "clean", wiz)
    time.sleep(0.5)
    alice = relog("Alice")
    enter(alice)
    check("clean restart: alice still has taken the stash", alice.command("get stash"), "You have already taken")
    check("clean restart: the kept potion is still in alice's inventory", alice.command("i"), "minor healing potion")
    new = login("Newcomer")
    enter(new)
    t, g = take(new, "stash")
    check("clean restart: the empty stash stays empty", t, "Someone has been here before you")
    ok("clean restart: and pays nothing", g == 0)
    t, g = take(new, "cache")
    ok("clean restart: a part-taken stash goes on at the next amount (cache, third finder)", g == table(100)[2], g)
    ok("clean restart: tin remembers its three finders", len(record()[T + "tin"]) == 3)
    # update of the find, the room, and the base class
    wiz = relog("Warden")
    wiz.command("goto " + T + "hub")
    a.drain(wiz, 0.3)
    n_before = len(record()[T + "stash_b"])
    check("update of the find file compiles", wiz.command("update " + T + "stash_b"), absent=("Failed", "error"))
    check("update of the room compiles", wiz.command("update " + T + "vault"), absent=("Failed", "error"))
    check("update of the base class compiles", wiz.command("update /std/find"), absent=("Failed", "error"))
    alice.command("out")
    alice.command("vault")
    check("after the update the find is in the room", alice.command("look"), "A test cache")
    ok("after the updates the record is intact", len(record()[T + "stash_b"]) == n_before)
    erin = relog("Erin")
    enter(erin)
    check("after update a character who had taken it still cannot take it again", erin.command("get cache"), "You have already taken")
    fresh = login("Fresh")
    enter(fresh)
    t, g = take(fresh, "cache")
    ok("after update of the find, the room and the base class the next finder gets the next amount", g == table(100)[n_before], g)
    check("a record of a find survives update: the same character is still turned away", fresh.command("get cache"), "You have already taken")
    ok("the wizard walked through all of it and is still not in the register", "warden" not in json.dumps(record()))
    return wiz


def main():
    global RECORD
    work = tempfile.mkdtemp(prefix="lesmudos-find-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    RECORD = os.path.join(work, "save", "secure", "finds.json")
    os.makedirs(os.path.join(mudlib, "domains", "ftest"), exist_ok=True)
    os.makedirs(os.path.join(mudlib, "domains", "example"), exist_ok=True)
    os.makedirs(os.path.join(mudlib, "wiz", "probe"), exist_ok=True)
    for name, source in files().items():
        h.write(mudlib, T + name, source)
    for path, source in a.building_templates().items():
        h.write(mudlib, path, source)
    h.write(mudlib, "/wiz/probe/room", WIZ_ROOM)
    h.write(mudlib, "/wiz/probe/wizstash", find_src("WizStash", "Wiz Stash", ["wizstash"], 1000))
    h.write(mudlib, "/wiz/probe/minter", MINTER)
    # the test door in the Village Square
    h.edit(mudlib, "/domains/hollow/village/square", 'Exits["south"] = "/domains/hollow/village/tavern";',
           'Exits["south"] = "/domains/hollow/village/tavern";\n        Exits["testdoor"] = "/domains/ftest/hub";')
    box = [None, None]
    box[0], box[1] = a.start_driver(work, mudlib)
    try:
        wiz = run(work, mudlib, box)
        box[1].flush()
        logged = open(os.path.join(work, "driver.log")).read()
        check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:1200], "ok")
        wiz.send("shutdown")
        box[0].wait(timeout=20)
    finally:
        if box[0].poll() is None:
            box[0].kill()
        box[1].close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-4000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\n{CHECKS[0]} checks, {len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
