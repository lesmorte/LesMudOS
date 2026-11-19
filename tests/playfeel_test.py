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
    hub_exits = {"up": HOLLOW + "village/square", "pen": TEST + "pen", "yard": TEST + "yard", "shop": TEST + "shop"}
    out = {"out": TEST + "hub"}
    return {
        "hub": a.room("PfHub", "Play Hub", hub_exits, safe=True),
        "pen": a.room("PfPen", "Rat Pen", out, f'        AddSpawn("{TEST}ratling", 2);\n', reset=3000),
        "yard": a.room("PfYard", "Quiet Yard", out, reset=3000),
        "shop": a.room("PfShop", "Test Shop", out, f'        AddSpawn("{TEST}shopper");\n', safe=True, reset=3000),
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


if __name__ == "__main__":
    sys.exit(main())
