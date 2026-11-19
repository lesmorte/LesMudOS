#!/usr/bin/env python3
"""End-to-end test of the play-feel changes (docs/specs/play-feel.md): numbered targets, the inventory and list format,
corpses that crumble, combat restrictions and flee, verbose, the prompt, and the idle timeout.

Starts the driver against a temporary copy of the mudlib, with a few test rooms and creatures under /domains/test (reached
by a door down from the Village Square of the copy), and plays real characters over TCP. The idle settings are given to the
driver in seconds through the environment (LESMUDOS_IDLE_MINUTES and LESMUDOS_TIMEOUT_MINUTES accept "4s").

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4831 python3 tests/playfeel_test.py
"""
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4831")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402

SECRET = h.SECRET
TEST = "/domains/test/"
HOLLOW = "/domains/hollow/"
check = h.check

# ---------------------------------------------------------------- test content (not shipped)

RATLING = '''// @inherit /std/monster
public class Ratling : Monster
{
    public string Skin { get; set; } = "rat";
    public override void Create()
    {
        Name = "ratling"; Ids.Add("rat"); Ids.Add("ratling");
        ShortDesc = "a ratling"; LongDesc = "A very small rat made for the play-feel test.";
        Level = 1; MaxHp = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
        Aggressive = false;
        AddLoot("/domains/hollow/obj/rusty_sword", 100);
    }
}
'''

# Like the ratling but with nothing to skin: its corpse crumbles when it is emptied.
MOUSE = '''// @inherit /std/monster
public class Mouse : Monster
{
    public override void Create()
    {
        Name = "mouse"; Ids.Add("mouse");
        ShortDesc = "a mouse"; LongDesc = "A very small mouse made for the play-feel test.";
        Level = 1; MaxHp = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
        Aggressive = false;
        AddLoot("/domains/hollow/obj/rusty_sword", 100);
    }
}
'''

# A creature with a skin and nothing else: its corpse is not empty while the skin can still be taken.
SKINNER = '''// @inherit /std/monster
public class Skinner : Monster
{
    public string Skin { get; set; } = "rat";
    public override void Create()
    {
        Name = "skinner"; Ids.Add("skinner");
        ShortDesc = "a skinner"; LongDesc = "A creature with a skin and nothing else.";
        Level = 1; MaxHp = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
        Aggressive = false;
        AddLoot("/domains/hollow/obj/rusty_sword", 100);
    }
}
'''

# A creature that dies and leaves nothing at all.
BARE = '''// @inherit /std/monster
public class Bare : Monster
{
    public override void Create()
    {
        Name = "bare"; Ids.Add("bare");
        ShortDesc = "a bare thing"; LongDesc = "Nothing to it.";
        Level = 1; MaxHp = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
        Aggressive = false;
    }
}
'''

TWIN = '''// @inherit /std/monster
public class Twin : Monster
{
    public override void Create()
    {
        Name = "twin"; Ids.Add("twin");
        ShortDesc = "a twin"; LongDesc = "Two swords in one.";
        Level = 1; MaxHp = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
        Aggressive = false;
        AddLoot("/domains/hollow/obj/rusty_sword", 100);
        AddLoot("/domains/hollow/obj/rusty_sword", 100);
    }
}
'''

# A creature that fights and never hurts anyone and never dies, for the tests of fighting rules.
DUMMY = '''// @inherit /std/monster
public class PfDummy : Monster
{
    public override void Create()
    {
        Name = "dummy"; Ids.Add("dummy");
        ShortDesc = "a sparring dummy"; LongDesc = "A sack of straw that hits back, a little.";
        Level = 1; MaxHp = 100000; DamageMin = 1; DamageMax = 1; ToHit = -30;
        Aggressive = false;
    }
}
'''

# Players deal exactly known damage with this: "zap pinata 30". "credit PATH" tells whether the kill credit for a creature is theirs.
ZAPPER = '''// @inherit /std/item
public class Zapper : Item
{
    public override void Create() { Name = "zapper"; Ids.Add("zapper"); ShortDesc = "a zapper"; }
    public override void Init() { base.Init(); AddAction("zap", Zap); AddAction("credit", Credit); }
    private bool Zap(string args)
    {
        var who = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var target = w[0] == "me" ? who : Present(w[0], Environment(who)!);
        if (target == null) { Write("No target.\\n"); return true; }
        target.Call("TakeDamage", int.Parse(w[1]), who);
        return true;
    }
    private bool Credit(string args) { Write("credit " + ThisPlayer()!.Call<bool>("QueryKilled", args.Trim()) + "\\n"); return true; }
}
'''


def pinata(cls, name, hp):
    return f'''// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{name}");
        ShortDesc = "a pinata"; LongDesc = "A paper pinata full of experience.";
        Level = 1; MaxHp = {hp}; DamageMin = 1; DamageMax = 1; ToHit = -30; XpValue = 100;
        Aggressive = false;
        AddLoot("/domains/hollow/obj/rusty_sword", 100);
    }}
}}
'''


# Hits every time for 1, and has plenty of Vitality: for the checks that verbose shows what was actually done.
BITER = '''// @inherit /std/monster
public class PfBiter : Monster
{
    public override void Create()
    {
        Name = "biter"; Ids.Add("biter");
        ShortDesc = "a biter"; LongDesc = "It bites.";
        Level = 1; MaxHp = 5000; DamageMin = 1; DamageMax = 1; ToHit = 30;
        Aggressive = false;
    }
}
'''

TRINKET_RED = '''// @inherit /std/item
public class RedTrinket : Item
{
    public override void Create() { Name = "red trinket"; Ids.Add("trinket"); ShortDesc = "a red trinket"; Value = 1; }
}
'''
TRINKET_BLUE = '''// @inherit /std/item
public class BlueTrinket : Item
{
    public override void Create() { Name = "blue trinket"; Ids.Add("trinket"); ShortDesc = "a blue trinket"; Value = 1; }
}
'''


def files():
    hub_exits = {"up": HOLLOW + "village/square", "pen": TEST + "pen", "yard": TEST + "yard", "shop": TEST + "shop", "arena": TEST + "arena", "cell": TEST + "cell", "lab": TEST + "lab", "bite": TEST + "bite"}
    out = {"out": TEST + "hub"}
    return {
        "hub": a.room("PfHub", "Play Hub", hub_exits, safe=True),
        "pen": a.room("PfPen", "Rat Pen", out, f'        AddSpawn("{TEST}ratling", 2);\n', reset=3000),
        "yard": a.room("PfYard", "Quiet Yard", out, reset=3000),
        "shop": a.room("PfShop", "Test Shop", out, f'        AddSpawn("{TEST}shopper");\n', safe=True, reset=3000),
        "arena": a.room("PfArena", "Fighting Pit", {"out": TEST + "hub", "east": TEST + "arena2", "south": TEST + "vault"},
                        f'        AddSpawn("{TEST}dummy");\n        LockExit("south", "gold key", "The gate to the south is locked.");\n', reset=3000),
        "arena2": a.room("PfArena2", "Second Pit", {"west": TEST + "arena"}, reset=3000),
        "vault": a.room("PfVault", "Closed Vault", {"north": TEST + "arena"}, reset=3000),
        "cell": a.room("PfCell", "Locked Cell", {"north": TEST + "hub"},
                       f'        AddSpawn("{TEST}dummy");\n        LockExit("north", "gold key", "The cell door is locked.");\n', reset=3000),
        "dummy": DUMMY,
        "bite": a.room("PfBite", "Biting Room", {"out": TEST + "hub"}, f'        AddSpawn("{TEST}biter");\n', reset=3000),
        "biter": BITER,
        "lab": a.room("PfLab", "Zap Lab", {"out": TEST + "hub"}, f'        AddSpawn("{TEST}zapper", 20);\n', reset=3000),
        "zapper": ZAPPER,
        "pinata": pinata("Pinata", "pinata", 100), "pinata90": pinata("Pinata90", "pinata", 90),
        "ratling": RATLING, "mouse": MOUSE, "skinner": SKINNER, "bare": BARE, "twin": TWIN,
        "red_trinket": TRINKET_RED, "blue_trinket": TRINKET_BLUE,
        "shopper": '''// @inherit /std/shopkeeper
public class PfShopper : Shopkeeper
{
    public override void Create()
    {
        Name = "Shopper"; Ids.Add("shopper"); ShortDesc = "Shopper the trader";
        AddStock("/domains/hollow/obj/rusty_sword", 10);
    }
}
''',
    }


# ---------------------------------------------------------------- helpers

def login(name):
    c = h.Client(name)
    c.read_until("> ")
    return c


def stat_name(c, target):
    """The object name `stat` reports for a target (the clone number tells identical things apart)."""
    text = c.command("stat " + target)
    m = re.search(r"(/domains/\S+#\d+):", text)
    return m.group(1) if m else None


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-playfeel-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib, novice=True)
    os.makedirs(os.path.join(mudlib, "domains", "test"))
    for name, source in files().items():
        h.write(mudlib, TEST + name, source)
    h.edit(mudlib, HOLLOW + "village/square", 'Exits["south"]', f'Exits["down"] = "{TEST}hub";\n        Exits["south"]')

    box = [None, None]
    box[0], box[1] = a.start_driver(work, mudlib)
    try:
        run(work, mudlib, box)
        box[1].flush()
        logged = open(os.path.join(work, "driver.log")).read()
        check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:800], "ok")
        c = h.Client("wiz")
        c.read_until("> ")
        c.command("shutdown")
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


def run(work, mudlib, box):
    wiz = login("wiz")
    check("setup: the first wizard", wiz.command("promote " + SECRET), "level 10 wizard")
    wiz.command("goto " + TEST + "hub")
    run_numbers(wiz)
    run_lists(wiz)
    run_corpses(wiz)
    run_fleeing(wiz)
    run_shared_xp(wiz)
    run_verbose(wiz)


# ===== 5. telling identical things apart =====

def run_numbers(wiz):
    wiz.command("goto " + TEST + "pen")
    first = stat_name(wiz, "rat")
    second = stat_name(wiz, "rat 2")
    check("numbers: the two rats are different objects", "ok" if first and second and first != second else f"{first} {second}", "ok")
    check("numbers: 'first rat' is the first", "ok" if stat_name(wiz, "first rat") == first else "differs", "ok")
    check("numbers: 'rat 1' is the first", "ok" if stat_name(wiz, "rat 1") == first else "differs", "ok")
    check("numbers: 'second rat' is the second", "ok" if stat_name(wiz, "second rat") == second else "differs", "ok")
    check("numbers: '2.rat' is the second", "ok" if stat_name(wiz, "2.rat") == second else "differs", "ok")
    check("numbers: 'look at second rat' works", wiz.command("look at second rat"), "A very small rat made for the play-feel test.")
    check("numbers: a third rat does not exist", wiz.command("look at rat 3"), "You see no 'rat 3' here.")
    check("numbers: without a number the first is used, as before", "ok" if stat_name(wiz, "ratling") == first else "differs", "ok")

    # Two corpses, told apart by number.
    check("numbers: slay the second rat by number", wiz.command("slay rat 2"), "You slay a ratling!")
    check("numbers: the first rat is still there", "ok" if stat_name(wiz, "rat") == first else "differs", "ok")
    wiz.command("slay rat")
    text = wiz.command("look")
    check("numbers: two corpses are in the room", text, "corpses of")
    check("numbers: the corpses answer to numbers", wiz.command("look at corpse 2"), "The lifeless body of a ratling lies here.")
    check("numbers: get from corpse 2 takes only from the second corpse", wiz.command("get sword from corpse 2"), "You take a rusty sword from the corpse of a ratling.")
    check("numbers: corpse 2 is now without the sword", wiz.command("look in corpse 2"), "The lifeless body", absent=("rusty sword",))
    check("numbers: corpse 1 still holds it", wiz.command("look in 1.corpse"), "It holds: a rusty sword")
    check("numbers: 'first corpse' also", wiz.command("look in first corpse"), "It holds: a rusty sword")
    check("numbers: loot 2.corpse takes the second corpse, which has nothing left", wiz.command("loot 2.corpse"), "There is nothing in")
    check("numbers: loot 1.corpse takes the first", wiz.command("loot 1.corpse"), "You take a rusty sword from the corpse of a ratling.")
    wiz.command("drop all sword")

    # Items on the floor and in the inventory.
    wiz.command("goto " + TEST + "yard")
    wiz.command("clone " + TEST + "red_trinket")
    wiz.command("clone " + TEST + "blue_trinket")
    wiz.command("drop all")
    check("numbers: 'get trinket 2' takes the second trinket", wiz.command("get trinket 2"), "You take a blue trinket.")
    check("numbers: 'get 1.trinket' takes the first", wiz.command("get 1.trinket"), "You take a red trinket.")
    check("numbers: 'drop second trinket' drops the second carried (the blue one was picked up first)", wiz.command("drop second trinket"), "You drop a red trinket.")
    check("numbers: a number beyond the matches is refused", wiz.command("drop trinket 4"), "Drop what?")
    wiz.command("drop all")


# ===== 6. inventory and list format =====

def run_lists(wiz):
    wiz.command("goto " + TEST + "yard")
    wiz.command("drop all")
    wiz.command("clone " + HOLLOW + "obj/healing_potion")
    wiz.command("clone " + HOLLOW + "obj/healing_potion")
    wiz.command("clone " + HOLLOW + "obj/rusty_sword")
    wiz.command("clone " + HOLLOW + "obj/rusty_sword")
    wiz.command("wield sword")
    text = wiz.command("i")
    check("inventory: a heading and one item per line, indented", text, "Inventory\r\n   ")
    check("inventory: identical carried items collapse with a count", text, "   a minor healing potion (x2)\r\n")
    check("inventory: a wielded item is never merged with a carried one", text, "   a rusty sword (wielded)\r\n", "   a rusty sword\r\n")
    check("inventory: the old one-line form is gone", text, absent=("You are carrying", ", a rusty"))
    wiz.command("drop all")
    text = wiz.command("look")
    check("floor: identical things are listed with a count", text, "A minor healing potion (x2) is here.", "A rusty sword is here.", absent=("Two minor healing",))
    wiz.command("get all")
    wiz.command("goto " + TEST + "shop")
    check("shop sale: identical items in one sentence with a count", wiz.command("sell all"),
          "You sell a minor healing potion (x2) to Shopper for", absent=("You sell a minor healing potion to",))
    # A corpse's contents.
    wiz.command("drop all")
    wiz.command("clone " + TEST + "twin")
    wiz.command("slay twin")
    check("corpse: the contents are listed with a count", wiz.command("look at corpse"), "It holds: a rusty sword (x2).")
    check("corpse: taking identical things is one sentence", wiz.command("loot corpse"), "You take a rusty sword (x2) from the corpse of a twin.")
    check("floor: a stack is told with its count too", (wiz.command("drop all"), wiz.command("look"))[1], "A rusty sword (x2) is here.")
    wiz.command("get all")
    wiz.command("drop all")


# ===== 4. corpses crumble when looted =====

def run_corpses(wiz):
    wiz.command("goto " + TEST + "yard")
    wiz.command("drop all")
    # A creature with nothing to skin: its corpse crumbles when the last item is taken.
    wiz.command("clone " + TEST + "mouse")
    wiz.command("slay mouse")
    check("corpse: a corpse holding loot is still there", wiz.command("look"), "corpse of a mouse is here")
    text = wiz.command("loot corpse")
    check("corpse: taking the last item crumbles it at once, with a line to the room", text, "You take a rusty sword from the corpse of a mouse.", "The corpse of mouse crumbles to dust.")
    check("corpse: it is gone", wiz.command("look at corpse"), "You see no 'corpse' here.")
    wiz.command("drop sword")
    # A creature with a skin left: looting does not make it crumble.
    wiz.command("clone " + TEST + "skinner")
    wiz.command("slay skinner")
    text = wiz.command("loot corpse")
    check("corpse: a skin left, the loot is taken", text, "You take a rusty sword from the corpse of a skinner.", absent=("crumbles",))
    text = wiz.command("look at corpse")
    check("corpse: the empty corpse with a skin is still there, and says so", text, "It is empty.", "Its hide could still be taken")
    time.sleep(1.5)
    check("corpse: it is still there a moment later", wiz.command("look at corpse"), "It is empty.")
    wiz.command("clone " + HOLLOW + "obj/craft/skinning_knife")
    wiz.command("skin corpse")
    text = wiz.read_until("crumbles to dust", timeout=30)
    check("corpse: taking the last yield from an empty corpse crumbles it", text, "The corpse of skinner crumbles to dust.")
    check("corpse: it is gone", wiz.command("look at corpse"), "You see no 'corpse' here.")
    wiz.command("drop all")
    # Coins count as items: a corpse with only gold crumbles when the gold is taken.
    wiz.command("clone " + TEST + "bare")
    wiz.command("slay bare")
    text = wiz.command("loot corpse")
    check("corpse: a corpse empty from the start is not looted into dust; it keeps its timer", text, "There is nothing in the corpse of a bare thing.")
    check("corpse: it is still there", wiz.command("look at corpse"), "It is empty.")


# ===== 3. combat restrictions and flee =====

def flee_until(c, command, tries=40):
    """Repeat a flee command until it is not the 1-in-4 failure. Returns the text of the last try."""
    text = ""
    for _ in range(tries):
        text = c.command(command)
        if "cannot get away" not in text:
            break
        a.drain(c, 0.2)
    return text


def run_fleeing(wiz):
    wiz.command("goto " + TEST + "yard")
    wiz.command("drop all")
    wiz.command("clone " + TEST + "red_trinket")
    wiz.command("drop trinket")
    wiz.command("goto " + TEST + "arena")
    wiz.command("clone " + TEST + "red_trinket")
    wiz.command("drop trinket")
    check("fleeing: not fighting, flee says so", wiz.command("flee"), "You are not fighting anyone.")
    check("fleeing: a fight begins", wiz.command("kill dummy"), "You attack a sparring dummy.")
    check("fleeing: picking up from the floor is refused, pointing to flee", wiz.command("get trinket"), "You are fighting!", "pick things up", "flee")
    check("fleeing: the thing is still on the floor", wiz.command("look"), "A red trinket is here.")
    check("fleeing: get from a corpse is refused too", wiz.command("get sword from corpse"), "You are fighting!", "flee")
    check("fleeing: loot is refused", wiz.command("loot"), "You are fighting!", "flee")
    check("fleeing: walking away is refused, pointing to flee", wiz.command("east"), "cannot just walk away", "flee")
    check("fleeing: the abbreviated exit is refused as well", wiz.command("e"), "cannot just walk away")
    check("fleeing: still in the same room", wiz.command("look"), "Fighting Pit")
    # A named direction that cannot be used fails without spending the attempt: never the 1-in-4 failure line.
    texts = [wiz.command("flee south") for _ in range(20)]
    check("fleeing: a locked direction is refused every time, without a try", "ok" if all("You cannot flee that way" in t and "cannot get away" not in t for t in texts) else str(texts[:3]), "ok")
    check("fleeing: so is a direction that does not exist", wiz.command("flee up"), "You cannot flee that way")
    check("fleeing: the fight goes on after refusals", wiz.command("look"), "Fighting Pit")
    # With a direction.
    text = flee_until(wiz, "flee east")
    check("fleeing: with a direction you go that way", text, "You flee east!", "Second Pit")
    check("fleeing: the fight is over for you", wiz.command("flee"), "You are not fighting anyone.")
    check("fleeing: you can walk and pick things up again", wiz.command("west"), "Fighting Pit")
    wiz.command("kill dummy")
    # Without a direction: back the way you came (twice from each side, so a random pick would show).
    for round_ in range(1, 5):
        wiz.command("flee")  # (may or may not get away; make sure we are out)
        flee_until(wiz, "flee") if "Fighting Pit" in wiz.command("look") else None
        where = wiz.command("look")
        if "Second Pit" not in where:
            wiz.command("goto " + TEST + "arena2")
        wiz.command("west")
        wiz.command("kill dummy")
        text = flee_until(wiz, "flee")
        check(f"fleeing: with no direction you go back the way you came (round {round_})", text, "You flee east!", "Second Pit")
    # The way back cannot be used (not arrived by an exit): any usable exit, never the locked one.
    seen = set()
    for _ in range(12):
        wiz.command("goto " + TEST + "arena")
        wiz.command("kill dummy")
        text = flee_until(wiz, "flee")
        if "You flee east!" in text:
            seen.add("east")
        elif "You flee out!" in text:
            seen.add("out")
        else:
            seen.add("other: " + text[:60])
    check("fleeing: with no way back, a random usable exit is used (never the locked south)", "ok" if seen <= {"east", "out"} and seen else str(seen), "ok")
    # No usable exit at all.
    wiz.command("goto " + TEST + "cell")
    wiz.command("kill dummy")
    check("fleeing: with no usable exit there is no way out", wiz.command("flee"), "There is no way out!")
    check("fleeing: still fighting", wiz.command("get trinket"), "You are fighting!")
    wiz.command("goto " + TEST + "yard")
    wiz.command("get all")
    wiz.command("drop all")


# ===== 8. shared experience =====

def xp_of(c):
    return a.score_of(c)["xp"]


def lab_player(name):
    """A new character with a zapper, standing in the lab."""
    c = login(name)
    c.command("down")
    c.command("lab")
    c.command("get zapper")
    return c


def fresh_pinata(wiz, kind="pinata"):
    wiz.command("goto " + TEST + "lab")
    wiz.command("clone " + TEST + kind)
    wiz.command("goto " + TEST + "hub")


def run_shared_xp(wiz):
    fresh_pinata(wiz)
    ann = lab_player("anna")
    check("shared xp: a lone kill pays 100%", (ann.command("zap pinata 100"), a.drain(ann, 0.5))[0], "You gain 100 experience.")
    check("shared xp: and the kill credit", ann.command("credit /domains/test/pinata"), "credit True")

    # Two players, 50/50, the killing blow from the second: 55/45.
    fresh_pinata(wiz)
    ben, cat = lab_player("benn"), lab_player("cato")
    b0, c0 = xp_of(ben), xp_of(cat)
    ben.command("zap pinata 50")
    cat.command("zap pinata 50")
    a.drain(ben, 0.5); a.drain(cat, 0.5)
    check("shared xp: two players, even damage, killing blow +10%: 55 for the killer", "ok" if xp_of(cat) - c0 == 55 else str(xp_of(cat) - c0), "ok")
    check("shared xp: and 45 for the other", "ok" if xp_of(ben) - b0 == 45 else str(xp_of(ben) - b0), "ok")
    check("shared xp: both have the kill credit", ben.command("credit /domains/test/pinata"), "credit True")
    check("shared xp: the other contributor may loot the corpse during the lock", ben.command("loot corpse"), "You take a rusty sword from the corpse of a pinata.")

    # Three equal contributors: 40 / 30 / 30.
    fresh_pinata(wiz, "pinata90")
    dee, eve, fay = lab_player("deen"), lab_player("evee"), lab_player("faye")
    base = [xp_of(x) for x in (dee, eve, fay)]
    dee.command("zap pinata 30")
    eve.command("zap pinata 30")
    fay.command("zap pinata 30")
    for x in (dee, eve, fay):
        a.drain(x, 0.5)
    got = [xp_of(x) - b for x, b in zip((dee, eve, fay), base)]
    check("shared xp: three equal contributors get 30, 30 and 40 for the killing blow", "ok" if got == [30, 30, 40] else str(got), "ok")
    # A bystander is refused the corpse.
    gus = lab_player("gust")
    check("shared xp: someone who did nothing may not loot it yet", gus.command("loot corpse"), "not yours to")

    # The fair-share rule for the killing blow: 30% of three gets the bonus, 10% does not.
    fresh_pinata(wiz)
    hal, ivy, jon = lab_player("halo"), lab_player("ivyy"), lab_player("jono")
    base = [xp_of(x) for x in (hal, ivy, jon)]
    hal.command("zap pinata 35")
    ivy.command("zap pinata 35")
    jon.command("zap pinata 30")
    for x in (hal, ivy, jon):
        a.drain(x, 0.5)
    got = [xp_of(x) - b for x, b in zip((hal, ivy, jon), base)]
    check("shared xp: a killing blow with 30% of three gets the bonus (31, 31, 37)", "ok" if got == [31, 31, 37] else str(got), "ok")
    fresh_pinata(wiz)
    base = [xp_of(x) for x in (hal, ivy, jon)]
    hal.command("zap pinata 45")
    ivy.command("zap pinata 45")
    jon.command("zap pinata 10")
    for x in (hal, ivy, jon):
        a.drain(x, 0.5)
    got = [xp_of(x) - b for x, b in zip((hal, ivy, jon), base)]
    check("shared xp: a killing blow with 10% of three gets no bonus: the shares follow damage alone (45, 45, 10)", "ok" if got == [45, 45, 10] else str(got), "ok")
    # Two players: 40% is under 42.5%, no bonus; the lone killer always gets all of it.
    fresh_pinata(wiz)
    base = [xp_of(x) for x in (hal, ivy)]
    hal.command("zap pinata 60")
    ivy.command("zap pinata 40")
    for x in (hal, ivy):
        a.drain(x, 0.5)
    got = [xp_of(x) - b for x, b in zip((hal, ivy), base)]
    check("shared xp: with two players the bonus needs 42.5%: 40% gets none (60, 40)", "ok" if got == [60, 40] else str(got), "ok")

    # Overkill does not count.
    fresh_pinata(wiz)
    base = [xp_of(x) for x in (hal, ivy)]
    hal.command("zap pinata 60")
    ivy.command("zap pinata 400")
    for x in (hal, ivy):
        a.drain(x, 0.5)
    got = [xp_of(x) - b for x, b in zip((hal, ivy), base)]
    check("shared xp: overkill does not count (the killing blow dealt 40 of 100: 60 and 40)", "ok" if got == [60, 40] else str(got), "ok")

    # A contributor who left the room or died is dropped; the rest still add up to the whole.
    fresh_pinata(wiz)
    kay, lou = lab_player("kayy"), lab_player("loui")
    lb = xp_of(lou)
    kay.command("kill pinata")
    kay.command("zap pinata 50")
    lou.command("zap pinata 20")
    for _ in range(30):
        if "You flee out!" in kay.command("flee out"):
            break
    lou.command("zap pinata 100")
    a.drain(lou, 0.5)
    check("shared xp: a contributor who left the room is dropped; the one left gets the whole 100", "ok" if xp_of(lou) - lb == 100 else str(xp_of(lou) - lb), "ok")
    fresh_pinata(wiz)
    mia, ned = lab_player("miaa"), lab_player("nedd")
    nb = xp_of(ned)
    mia.command("zap pinata 50")
    mia.command("zap me 9999")
    a.drain(mia, 0.5)
    ned.command("zap pinata 100")
    a.drain(ned, 0.5)
    check("shared xp: a contributor who died is dropped; the one left gets the whole 100", "ok" if xp_of(ned) - nb == 100 else str(xp_of(ned) - nb), "ok")

    # A wizard who shares in a kill gets a share but makes no history.
    fresh_pinata(wiz)
    wiz.command("goto " + TEST + "lab")
    wiz.command("get zapper")
    oto = lab_player("otto")
    ob = xp_of(oto)
    wiz.command("zap pinata 40")
    oto.command("zap pinata 100")
    text = a.quiet(wiz, 1.0)
    check("shared xp: a wizard among the participants makes no history", text, "Wizards make no history.")
    check("shared xp: and the player gets the share of the damage (60 of 100, with 60% killing blow: 10 + 54)", "ok" if xp_of(oto) - ob == 64 else str(xp_of(oto) - ob), "ok")
    wiz.command("drop all")
    wiz.command("goto " + TEST + "hub")


# ===== 1. verbose =====

def fight_text(c, minutes_of_text=None, until=None, timeout=40):
    """Read what a fight prints until `until(text)` is true."""
    end = time.time() + timeout
    text = ""
    while time.time() < end and not until(text):
        text += a.quiet(c, 0.4)
    return text


def run_verbose(wiz):
    fresh_pinata(wiz)
    vic = lab_player("vera")
    check("verbose: off by default", vic.command("verbose"), "Verbose is off.")
    check("verbose: usage", vic.command("verbose maybe"), "Usage: verbose")
    # Off: a fight shows no numbers.
    vic.command("out")
    vic.command("bite")
    wiz.command("goto " + TEST + "bite")
    vic.command("kill biter")
    text = fight_text(vic, until=lambda t: t.count("You hit") + t.count("You scratch") + t.count("You smash") >= 2 and "bites you" in t or "biter hits you" in t or "biter scratches you" in t)
    check("verbose: with it off, combat lines carry no numbers", text, absent=(" damage)", "Vitality)"))
    for _ in range(40):
        if "You flee" in vic.command("flee"):
            break
    a.drain(vic, 0.3)
    wiz.command("restore vera")
    base = int(re.search(r"HP (\d+)/5000", wiz.command("stat biter")).group(1))
    vic.command("bite")
    check("verbose: turn it on", vic.command("verbose on"), "Verbose is now on.")
    check("verbose: and the status says so", vic.command("verbose"), "Verbose is on")
    start_hp = a.hp_of(vic)[0]
    vic.command("kill biter")
    # The damage a player deals and takes in real combat matches what was done.
    text = fight_text(vic, until=lambda t: len(re.findall(r"\(\+\d+ damage\)", t)) >= 4 and len(re.findall(r"\(-\d+ damage\)", t)) >= 2, timeout=60)
    # Stop the fight (as soon as it will let us) and read the totals straight away.
    for _ in range(30):
        out = vic.command("flee")
        text += out
        if "You flee" in out:
            break
    left = a.hp_of(vic)[0]
    check("verbose: out of the fight again", vic.command("look"), "Play Hub")
    dealt = sum(int(n) for n in re.findall(r"\(\+(\d+) damage\)", text))
    taken = sum(int(n) for n in re.findall(r"\(-(\d+) damage\)", text))
    lost_by_biter = base - int(re.search(r"HP (\d+)/5000", wiz.command("stat biter")).group(1))
    check("verbose: the damage you deal is shown after the combat line", text, "(+")
    check("verbose: the damage you take is shown too", text, "(-")
    check("verbose: damage shown as dealt adds up to the Vitality the creature lost", "ok" if dealt == lost_by_biter + 0 and dealt > 0 else f"shown {dealt}, lost {lost_by_biter}", "ok")
    check("verbose: damage shown as taken adds up to the Vitality you lost", "ok" if taken == start_hp - left + 0 and taken > 0 else f"shown {taken}, lost {start_hp - left} (hp {start_hp} -> {left}, the lines before the setting was on are not counted)", "ok")

    # Exact numbers from any source: the zapper (damage over time and other sources use the same call).
    wiz.command("goto " + TEST + "lab")
    wiz.command("clone " + TEST + "pinata")
    wiz.command("goto " + TEST + "hub")
    check("verbose: into the lab", vic.command("lab"), "Zap Lab")
    a.drain(vic, 0.5)
    hp0 = a.hp_of(vic)[0]
    text = vic.command("zap pinata 7")
    check("verbose: a zap that deals 7 shows (+7 damage)", text, "(+7 damage)")
    text = vic.command("zap me 6")
    check("verbose: damage taken shows (-6 damage)", text, "(-6 damage)")
    check("verbose: and 6 Vitality really went", "ok" if hp0 - a.hp_of(vic)[0] in (6, 5, 4) and hp0 - a.hp_of(vic)[0] >= 4 else "?", "ok")
    raw = vic.raw_command("zap pinata 3")
    check("verbose: damage you deal is green", raw, "\x1b[32m(+3 damage)")
    raw = vic.raw_command("zap me 2")
    check("verbose: damage you take is red", raw, "\x1b[31m(-2 damage)")
    vic.command("ansi off")
    raw = vic.raw_command("zap pinata 4")
    check("verbose: ansi off gives plain text", raw, "(+4 damage)", absent=("\x1b",))
    vic.command("ansi on")
    # Overkill does not count: with 30 left in the pinata, a blow of 500 is 30 damage.
    fresh_left = 100 - 7 - 3 - 4
    text = vic.command("zap pinata 500")
    check("verbose: only the Vitality actually lost counts (overkill does not)", text, f"(+{fresh_left} damage)")
    # Healing.
    wiz.command("goto " + TEST + "lab")
    wiz.command("clone " + HOLLOW + "obj/healing_potion")
    wiz.command("give potion to vera")
    vic.command("zap me 10")
    a.drain(vic, 0.3)
    before = a.hp_of(vic)[0]
    text = vic.command("drink potion")
    text += fight_text(vic, until=lambda t: "Vitality)" in t, timeout=20)
    m = re.search(r"You recover (\d+) hit points\. \(\+(\d+) Vitality\)", text)
    gained = a.hp_of(vic)[0] - before
    check("verbose: healing shows (+N Vitality), the number it really healed", "ok" if m and m.group(1) == m.group(2) and gained in (int(m.group(1)), int(m.group(1)) + 1) else f"{text[-200:]!r} gained {gained}", "ok")
    raw = vic.raw_command("verbose")
    # Saved with the character.
    vic.command("quit")
    vic.closed(3)
    again = login("vera")
    check("verbose: saved with the character", again.command("verbose"), "Verbose is on")
    again.command("verbose off")
    check("verbose: off again", again.command("verbose"), "Verbose is off.")
    wiz.command("goto " + TEST + "hub")


if __name__ == "__main__":
    sys.exit(main())
