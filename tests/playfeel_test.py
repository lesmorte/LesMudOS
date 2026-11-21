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
        Level = 1; MaxVitality = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
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
        Level = 1; MaxVitality = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
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
        Level = 1; MaxVitality = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
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
        Level = 1; MaxVitality = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
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
        Level = 1; MaxVitality = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
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
        Level = 1; MaxVitality = 100000; DamageMin = 1; DamageMax = 1; ToHit = -30;
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
        Level = 1; MaxVitality = {hp}; DamageMin = 1; DamageMax = 1; ToHit = -30; XpValue = 100;
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
        Level = 1; MaxVitality = 5000; DamageMin = 1; DamageMax = 1; ToHit = 30;
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


# An aggressive creature that always hits for 1 and takes a long time to die. BaseReaction 3.0 makes its first action wait about 6 seconds.
WOLF = '''// @inherit /std/monster
public class PfWolf : Monster
{
    public override void Create()
    {
        Name = "wolf"; Ids.Add("wolf");
        ShortDesc = "a grey wolf"; LongDesc = "A wolf made for the aggression tests.";
        Level = 1; MaxVitality = 100000; DamageMin = 1; DamageMax = 1; ToHit = 30; BaseReaction = 3.0;
        Aggressive = true;
    }
}
'''

# A creature that leaves a corpse holding a sword (nothing to skin), for the tests of the refill.
HARE = '''// @inherit /std/monster
public class PfHare : Monster
{
    public override void Create()
    {
        Name = "hare"; Ids.Add("hare");
        ShortDesc = "a hare"; LongDesc = "A hare made for the refill tests.";
        Level = 1; MaxVitality = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
        Aggressive = false;
        AddLoot("/domains/hollow/obj/rusty_sword", 100);
    }
}
'''

# A wizard's tool: "resetroom" runs this room's reset now, "timers A B C" sets the timers of every corpse here, "movecorpse PATH" moves a corpse to that room.
PFTOOL = '''// @inherit /std/item
public class PfTool : Item
{
    public override void Create() { Name = "pftool"; Ids.Add("pftool"); ShortDesc = "a refill tool"; }
    public override void Init() { base.Init(); AddAction("resetroom", ResetRoom); AddAction("timers", Timers); AddAction("movecorpse", MoveCorpse); }
    private bool ResetRoom(string args) { Environment(ThisPlayer()!)!.Call("Reset"); Write("Reset done.\\n"); return true; }
    private bool Timers(string args)
    {
        var p = args.Split(' ');
        foreach (var c in AllInventory(Environment(ThisPlayer()!)!).Where(o => o.Call<bool>("IsCorpse"))) c.Call("SetTimers", int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]));
        Write("Timers set.\\n");
        return true;
    }
    private bool MoveCorpse(string args)
    {
        var corpse = AllInventory(Environment(ThisPlayer()!)!).FirstOrDefault(o => o.Call<bool>("IsCorpse"));
        if (corpse == null) { Write("No corpse.\\n"); return true; }
        MoveObject(corpse, LoadObject(args.Trim()));
        Write("Moved.\\n");
        return true;
    }
}
'''


def files():
    hub_exits = {"up": HOLLOW + "village/square", "pen": TEST + "pen", "yard": TEST + "yard", "shop": TEST + "shop", "arena": TEST + "arena", "cell": TEST + "cell", "lab": TEST + "lab", "bite": TEST + "bite", "den": TEST + "den", "twoden": TEST + "twoden", "calmden": TEST + "calmden", "mild": TEST + "mild",
                "burrow": TEST + "burrow", "burrow2": TEST + "burrow2", "warren": TEST + "warren", "trail": TEST + "trail"}
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
        "den": a.room("PfDen", "Wolf Den", {"out": TEST + "hub", "east": TEST + "den2"}, f'        AddSpawn("{TEST}wolf");\n', reset=3000),
        "twoden": a.room("PfTwoDen", "Twin Wolf Den", {"out": TEST + "hub", "east": TEST + "den2"}, f'        AddSpawn("{TEST}wolf", 2);\n', reset=3000),
        "calmden": a.room("PfCalmDen", "Calm Den", {"out": TEST + "hub", "east": TEST + "den2"}, f'        AddSpawn("{TEST}wolf");\n', safe=True, reset=3000),
        "mild": a.room("PfMild", "Mild Den", {"out": TEST + "hub", "east": TEST + "den2"}, f'        AddSpawn("{TEST}dummy");\n', reset=3000),
        "den2": a.room("PfDen2", "Far Den", {"west": TEST + "den", "out": TEST + "hub"}, reset=3000),
        "burrow": a.room("PfBurrow", "Hare Burrow", {"out": TEST + "hub"}, f'        AddSpawn("{TEST}hare");\n', reset=3000),
        "burrow2": a.room("PfBurrow2", "Second Burrow", {"out": TEST + "hub"}, f'        AddSpawn("{TEST}hare");\n', reset=3000),
        "warren": a.room("PfWarren", "Hare Warren", {"out": TEST + "hub"}, f'        AddSpawn("{TEST}hare");\n        AddSpawn("{TEST}hare");\n', reset=3000),
        "trail": a.room("PfTrail", "Hare Trail", {"out": TEST + "hub"}, f'        AddEncounter("{TEST}hare", 100);\n', reset=3000),
        "wolf": WOLF, "hare": HARE, "pftool": PFTOOL,
        "lab": a.room("PfLab", "Zap Lab", {"out": TEST + "hub"}, f'        AddSpawn("{TEST}zapper", 40);\n', reset=3000),
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
    run_aggro(wiz)
    run_refill(wiz)
    run_shared_xp(wiz)
    run_verbose(wiz)
    run_prompt(wiz)
    wiz.command("save")
    wiz.command("shutdown")
    box[0].wait(timeout=15)
    box[1].flush()
    # The idle tests need short times: a second driver on the same mudlib and saves, with the two settings in seconds.
    os.environ["LESMUDOS_IDLE_MINUTES"] = "4s"
    os.environ["LESMUDOS_TIMEOUT_MINUTES"] = "14s"
    box[0], box[1] = a.start_driver(work, mudlib)
    wiz = login("wiz")
    wiz.command("goto " + TEST + "hub")
    run_idle(wiz)


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


# ===== 9. aggressive creatures engage on arrival =====

WOLF_HIT = re.compile(r"(?im)^(?:The )?(?:a )?grey wolf (?!attacks you!)\w+ you")


def player_at_hub(name):
    c = login(name)
    c.command("down")
    return c


def run_aggro(wiz):
    # The player sees the room, then the attack line, is in combat at once, and the first blow waits the creature's Reaction Time (3.0 x 2 s).
    p = player_at_hub("wanda")
    t0 = time.time()
    p.send("den")
    text, hit_at, attack_at = "", None, None
    while time.time() - t0 < 25 and hit_at is None:
        text += a.quiet(p, 0.1)
        if attack_at is None and "attacks you!" in text:
            attack_at = time.time() - t0
        if WOLF_HIT.search(ansi_free(text)):
            hit_at = time.time() - t0
    plain = ansi_free(text)
    check("aggro: the player sees the room first", plain, "Wolf Den")
    check("aggro: then the attack line", "ok" if "Wolf Den" in plain and "attacks you!" in plain and plain.index("Wolf Den") < plain.index("attacks you!") else repr(plain[:200]), "ok")
    check("aggro: the attack line says who attacks", plain, "A grey wolf attacks you!")
    check("aggro: the fight begins at once, not after the Reaction Time", "ok" if attack_at is not None and attack_at < 3.0 else str(attack_at), "ok")
    check("aggro: the first blow waits the creature's Reaction Time", "ok" if hit_at is not None and hit_at >= 5.5 else str(hit_at), "ok")
    check("aggro: but not much longer", "ok" if hit_at is not None and hit_at < 12 else str(hit_at), "ok")
    check("aggro: walking on is refused", p.command("east"), "cannot just walk away", "flee")
    check("aggro: still in the same room", p.command("look"), "Wolf Den")
    text = flee_until(p, "flee east")
    check("aggro: flee gets the player out", text, "You flee east!", "Far Den")
    check("aggro: the fight is over for the player", p.command("flee"), "You are not fighting anyone.")
    plain = ansi_free(p.command("west") + a.quiet(p, 1.0))
    check("aggro: the player can walk again, and into the den again", plain, "Wolf Den")
    check("aggro: the creature still hunting them has them in the fight at once", p.command("east"), "cannot just walk away", "flee")
    flee_until(p, "flee east")
    p.command("out")

    # Several aggressive creatures: each one engages.
    p.send("twoden")
    text = ansi_free(a.quiet(p, 0.5) + a.quiet(p, 1.0))
    check("aggro: with two aggressive creatures each one engages", "ok" if text.count("attacks you!") == 2 else f"{text.count('attacks you!')}: {text[:200]!r}", "ok")
    flee_until(p, "flee east")
    p.command("out")

    # A creature that is not aggressive changes nothing.
    text = ansi_free(p.command("mild") + a.quiet(p, 1.5))
    check("aggro: a creature that is not aggressive does not attack", text, "Mild Den", absent=("attacks you",))
    check("aggro: and the player walks on freely", p.command("east"), "Far Den", absent=("cannot just walk away",))
    p.command("out")

    # A safe room changes nothing.
    text = ansi_free(p.command("calmden") + a.quiet(p, 1.5))
    check("aggro: in a safe room an aggressive creature does not attack", text, "Calm Den", absent=("attacks you",))
    check("aggro: and the player walks on freely", p.command("east"), "Far Den", absent=("cannot just walk away",))
    p.command("out")

    # The creature is gone before it can engage: a player who leaves at once is not attacked on the way.
    wiz.command("goto " + TEST + "hub")
    p.command("quit")


# ===== 10. a room does not refill while the creature's corpse is there =====

def look_hares(c):
    """(hares alive, corpses of hares) in the room, from the room description ("A hare", "Two hares", "the corpse of a hare", "Two corpses of a hare")."""
    text = ansi_free(c.command("look"))
    words = {"a": 1, "an": 1, "one": 1, "two": 2, "three": 3}
    corpses = 0
    for m in re.finditer(r"(?i)\b(?:(a|one|two|three) corpses? of a hare|the corpse of a hare)", text):
        corpses += words.get((m.group(1) or "a").lower(), 1)
    rest = re.sub(r"(?i)\b(?:(a|one|two|three) corpses? of a hare|the corpse of a hare)", "", text)
    alive = 0
    for m in re.finditer(r"(?i)\b(a|one|two|three) hares?\b", rest):
        alive += words[m.group(1).lower()]
    return (alive, corpses)


def run_refill(wiz):
    wiz.command("goto " + TEST + "hub")
    wiz.command("drop all")
    wiz.command("clone " + TEST + "pftool")
    wiz.command("get pftool")

    # An encounter whose corpse is here: the throttle is a minute, so the wait is shared with the other checks.
    text = wiz.command("trail")
    check("refill: an encounter wanders in (it is there when the wizard arrives)", text, "Hare Trail", "A hare is here")
    wiz.command("slay hare")
    encounter_since = time.time()
    check("refill: the encounter's corpse lies in the room", wiz.command("look"), "corpse of a hare is here")

    # A killed spawn is not replaced while its corpse is there.
    wiz.command("goto " + TEST + "burrow")
    check("refill: the spawn is there to begin with", "ok" if look_hares(wiz) == (1, 0) else str(look_hares(wiz)), "ok")
    wiz.command("slay hare")
    check("refill: killed, it leaves a corpse", "ok" if look_hares(wiz) == (0, 1) else str(look_hares(wiz)), "ok")
    check("refill: the reset runs", wiz.command("resetroom"), "Reset done.")
    check("refill: with the corpse still there the reset does not replace it", "ok" if look_hares(wiz) == (0, 1) else str(look_hares(wiz)), "ok")
    wiz.command("resetroom")
    check("refill: nor at the reset after that", "ok" if look_hares(wiz) == (0, 1) else str(look_hares(wiz)), "ok")
    # Looted to dust: the next reset replaces it.
    check("refill: the corpse is looted to dust", wiz.command("loot corpse"), "crumbles to dust")
    check("refill: the room has neither corpse nor creature", "ok" if look_hares(wiz) == (0, 0) else str(look_hares(wiz)), "ok")
    wiz.command("resetroom")
    check("refill: the next reset replaces the creature", "ok" if look_hares(wiz) == (1, 0) else str(look_hares(wiz)), "ok")
    check("refill: and only one", "ok" if wiz.command("resetroom") and look_hares(wiz) == (1, 0) else str(look_hares(wiz)), "ok")

    # Rotted away.
    wiz.command("slay hare")
    wiz.command("resetroom")
    check("refill: the corpse blocks again", "ok" if look_hares(wiz) == (0, 1) else str(look_hares(wiz)), "ok")
    text = wiz.command("stat corpse")
    check("refill: a creature's corpse lasts ten minutes at most", text, "lifetime 600s")
    wiz.command("timers 600 600 2")
    text = wiz.read_until("crumbles to dust", timeout=15)
    check("refill: the corpse rots away", text, "crumbles to dust")
    wiz.command("resetroom")
    check("refill: and the next reset replaces the creature", "ok" if look_hares(wiz) == (1, 0) else str(look_hares(wiz)), "ok")

    # Carried away: the slot is free.
    wiz.command("slay hare")
    wiz.command("resetroom")
    check("refill: (the corpse blocks)", "ok" if look_hares(wiz) == (0, 1) else str(look_hares(wiz)), "ok")
    check("refill: the corpse is carried out", wiz.command("movecorpse " + TEST + "yard"), "Moved.")
    wiz.command("resetroom")
    check("refill: a corpse carried out of the room frees the slot", "ok" if look_hares(wiz) == (1, 0) else str(look_hares(wiz)), "ok")
    wiz.command("goto " + TEST + "yard")
    wiz.command("loot corpse")
    wiz.command("goto " + TEST + "burrow")
    wiz.command("slay hare")
    wiz.command("loot corpse")

    # The corpse of another hare, dragged in from elsewhere, blocks nothing: the link is to the spawn, not the name.
    wiz.command("goto " + TEST + "burrow")
    wiz.command("slay hare")
    wiz.command("loot corpse")
    check("refill: (the burrow has neither hare nor corpse)", "ok" if look_hares(wiz) == (0, 0) else str(look_hares(wiz)), "ok")
    wiz.command("goto " + TEST + "burrow2")
    wiz.command("slay hare")
    wiz.command("movecorpse " + TEST + "burrow")
    wiz.command("goto " + TEST + "burrow")
    check("refill: another spawn's corpse lies here", "ok" if look_hares(wiz) == (0, 1) else str(look_hares(wiz)), "ok")
    wiz.command("resetroom")
    check("refill: it does not stop this spawn's refill", "ok" if look_hares(wiz) == (1, 1) else str(look_hares(wiz)), "ok")
    wiz.command("slay hare")
    wiz.command("loot corpse")
    wiz.command("loot corpse")

    # A spawn of two creatures: one killed, one corpse, one blocked slot.
    wiz.command("goto " + TEST + "warren")
    check("refill: two separate spawns of the same creature", "ok" if look_hares(wiz) == (2, 0) else str(look_hares(wiz)), "ok")
    wiz.command("slay hare")
    wiz.command("resetroom")
    check("refill: one killed, its corpse blocks its own slot only", "ok" if look_hares(wiz) == (1, 1) else str(look_hares(wiz)), "ok")
    wiz.command("slay hare")
    wiz.command("resetroom")
    check("refill: both killed, both corpses block", "ok" if look_hares(wiz) == (0, 2) else str(look_hares(wiz)), "ok")
    wiz.command("loot corpse")
    wiz.command("resetroom")
    check("refill: one corpse gone, one creature back", "ok" if look_hares(wiz) == (1, 1) else str(look_hares(wiz)), "ok")

    # A player's corpse never blocks anything.
    wiz.command("goto " + TEST + "burrow")
    wiz.command("loot corpse")
    wiz.command("slay hare")
    wiz.command("loot corpse")
    check("refill: (the burrow is empty)", "ok" if look_hares(wiz) == (0, 0) else str(look_hares(wiz)), "ok")
    vic = lab_player("harry")
    vic.command("out")
    vic.command("burrow")
    vic.command("zap me 100000")
    a.drain(vic, 0.7)
    check("refill: a player's corpse lies in the burrow", wiz.command("look"), "corpse of Harry")
    wiz.command("resetroom")
    check("refill: and does not block the refill", "ok" if look_hares(wiz)[0] == 1 else str(look_hares(wiz)), "ok")

    # The encounter: the corpse of this room's encounter still lies on the trail, a minute after it came.
    time.sleep(max(0, encounter_since + 62 - time.time()))
    wiz.command("goto " + TEST + "hub")
    text = wiz.command("trail") + a.quiet(wiz, 0.5)
    check("refill: no new encounter while the corpse of the last one lies here", text, "Hare Trail", absent=("A hare is here", "wanders in"))
    check("refill: (the corpse is still there)", wiz.command("look"), "corpse of a hare is here")
    wiz.command("loot corpse")
    wiz.command("out")
    text = wiz.command("trail") + a.quiet(wiz, 0.5)
    check("refill: once it is gone a new encounter can come", text, "Hare Trail", "A hare is here")
    wiz.command("slay hare")
    wiz.command("loot corpse")
    wiz.command("goto " + TEST + "hub")


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
    base = int(re.search(r"Vitality (\d+)/5000", wiz.command("stat biter")).group(1))
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
    lost_by_biter = base - int(re.search(r"Vitality (\d+)/5000", wiz.command("stat biter")).group(1))
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
    m = re.search(r"You recover (\d+) Vitality\. \(\+(\d+) Vitality\)", text)
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


# ===== 2. the prompt =====

def last_line(text):
    return text.replace("\r", "").split("\n")[-1]


def stat3(prompt):
    """((hp, max), (endurance, max), (focus, max)) from a default prompt, or None."""
    m = re.match(r"^\w+ \((\d+)/(\d+)\) \((\d+)/(\d+)\) \((\d+)/(\d+)\) > $", prompt)
    return tuple(int(x) for x in m.groups()) if m else None


def poll_prompt(c, wanted, timeout=25):
    """Ask `hp` until the numbers in the prompt satisfy `wanted`; returns them (the last ones if it never does)."""
    end = time.time() + timeout
    got = None
    while time.time() < end:
        got = stat3(last_line(c.command("vitality")))
        if got and wanted(got):
            break
        time.sleep(0.5)
    return got


def run_prompt(wiz):
    pat = h.Client("prue")
    pat.read_until("> ")
    text = pat.command("look")
    check("prompt: the default shows name, Vitality, Endurance and Focus after a command", "ok" if stat3(last_line(text)) == (30, 30, 70, 70, 70, 70) else last_line(text), "ok")
    check("prompt: it begins with the character's name", last_line(text), "Prue (30/30) (70/70) (70/70) > ")
    text = pat.command("prompt")
    check("prompt: alone it shows the prompt and the tokens", text, "Your prompt is:", "{name}", "{vitality}", "{maxvitality}", "{endurance}", "{maxendurance}", "{focus}", "{maxfocus}", "prompt default", "prompt off",
          absent=("{hp}", "{maxhp}", "{stamina}", "{maxstamina}", "{mana}", "{maxmana}", "HP", "Mana", "Stamina"))
    # Live values.
    pat.command("down")
    pat.command("lab")
    a.drain(pat, 0.5)
    check("prompt: a zapper is taken", pat.command("get zapper"), "You take a zapper.")
    text = pat.command("zap me 5")
    check("prompt: Vitality is live", "ok" if stat3(last_line(text)) and stat3(last_line(text))[0] == 25 else repr(text), "ok")
    wiz.command("goto " + TEST + "hub")
    # Endurance and Focus are spent by a warrior's bash and a mage's bolt.
    wiz.command("goto " + TEST + "lab")
    wiz.command("clone " + TEST + "dummy")
    wizard_dummy_room = True
    war = h.Client("walt", archetype="warrior")
    war.read_until("> ")
    war.command("down")
    war.command("lab")
    war.command("kill dummy")
    a.drain(war, 0.3)
    war.command("bash dummy")
    war_prompt = poll_prompt(war, lambda p: p[2] < p[3])
    check("prompt: Endurance is live (a bash costs some)", "ok" if war_prompt and war_prompt[2] < war_prompt[3] else str(war_prompt), "ok")
    mag = h.Client("mags", archetype="mage")
    mag.read_until("> ")
    mag.command("down")
    mag.command("lab")
    mag.command("kill dummy")
    a.drain(mag, 0.3)
    mag.command("bolt dummy")
    mag_prompt = poll_prompt(mag, lambda p: p[4] < p[5])
    check("prompt: Focus is live (a bolt costs some)", "ok" if mag_prompt and mag_prompt[4] < mag_prompt[5] else str(mag_prompt), "ok")
    for _ in range(40):
        if "You flee" in war.command("flee"):
            break
    for _ in range(40):
        if "You flee" in mag.command("flee"):
            break
    wiz.command("slay dummy")
    wiz.command("goto " + TEST + "hub")

    # Setting it.
    check("prompt: set to your own text with tokens", pat.command("prompt [{vitality}/{maxvitality}] {name}> "), "Your prompt is now:")
    text = pat.command("look")
    check("prompt: the tokens are expanded live (Vitality 25 and regenerating)", "ok" if re.match(r"^\[(2[5-9]|30)/30\] Prue> $", last_line(text)) else last_line(text), "ok")
    pat.command("prompt {focus} focus,{endurance} end")
    check("prompt: a space is added when the text does not end in one", last_line(pat.command("look")), "70 focus,70 end ")
    # The old token names still work, silently: a prompt saved before the rename keeps showing its numbers.
    check("prompt: the old tokens are accepted", pat.command("prompt [{hp}/{maxhp}] ({stamina}/{maxstamina}) ({mana}/{maxmana}) {name}> "), "Your prompt is now:")
    text = last_line(pat.command("look"))
    check("prompt: the old tokens expand to the same numbers", "ok" if re.match(r"^\[(2[5-9]|30)/30\] \(70/70\) \(70/70\) Prue> $", text) else text, "ok")
    check("prompt: the old tokens are not listed", pat.command("prompt"), "Your prompt is:", absent=("{hp}", "{maxhp}", "{stamina}", "{maxstamina}", "{mana}", "{maxmana}"))
    pat.command("prompt {red}[{maxvitality}]{/}\t{{name}}{bogus} {name")
    text = pat.raw_command("look")
    check("prompt: colour tags, control characters, unknown and unclosed tokens are removed from the player's text", last_line(ansi_free(text)), "[30] name ")
    check("prompt: and no escape sequence, tab or tag reaches the screen from it", last_line(text), absent=("\x1b", "\t", "{red}"))
    text = pat.command("prompt " + "x" * 81)
    check("prompt: a prompt that is too long is refused", text, "at most 80 characters")
    check("prompt: and nothing changed", last_line(pat.command("look")), "[30] name ")
    check("prompt: 80 characters are fine", pat.command("prompt " + "y" * 80), "Your prompt is now:")
    check("prompt: a prompt of only tags says there is nothing left", pat.command("prompt {red}{/}"), "leaves nothing")
    check("prompt: prompt off gives the plain '> '", last_line(pat.command("prompt off")), "> ")
    check("prompt: and stays that way", str(pat.command("look").endswith("\r\n> ")), "True")
    # After unprompted output.
    pat.command("prompt default")
    text = pat.command("look")
    check("prompt: prompt default restores it", "ok" if re.match(r"^Prue \(\d+/30\) \(70/70\) \(70/70\) > $", last_line(text)) else last_line(text), "ok")
    wiz.command("goto " + TEST + "lab")
    a.drain(pat, 0.3)
    wiz.command("say hello there")
    text = pat.read_until("> ", timeout=5)
    check("prompt: after unprompted output (someone speaks) the player ends on a current prompt", text, "Wiz says: hello there")
    check("prompt: the prompt is a fresh line after the news", "ok" if stat3(last_line(text)) else last_line(text), "ok")
    check("prompt: and the news did not start on the prompt's line", "ok" if text.startswith("\r\nWiz says") or text.startswith("\nWiz says") else repr(text[:30]), "ok")
    # A combat round is unprompted output too.
    wiz.command("clone " + TEST + "biter")
    pat.command("kill biter")
    text = fight_text(pat, until=lambda t: "biter" in t and bool(stat3(last_line(t))), timeout=40)
    check("prompt: a combat round ends on a prompt with live numbers", "ok" if stat3(last_line(text)) else last_line(text), "ok")
    for _ in range(40):
        if "You flee" in pat.command("flee"):
            break
    wiz.command("slay biter")
    # Not in the middle of a question: the wizard is in the editor when Prue speaks.
    pat.command("lab") if "Zap Lab" not in pat.command("look") else None
    wiz.command("goto " + TEST + "lab")
    wiz.send("edit /wiz/wiz/pfnote")
    a.drain(wiz, 0.5)
    pat.send("say anyone there")
    text = a.quiet(wiz, 1.0)
    check("prompt: while a player is in the editor the news arrives with no prompt", text, "Prue says: anyone there", absent=("Wiz (", "> "))
    wiz.send("q")
    a.drain(wiz, 0.5)
    wiz.command("goto " + TEST + "hub")
    # Saved with the character.
    pat.command("prompt <{vitality}> ")
    pat.command("quit")
    pat.closed(3)
    again = login("prue")
    check("prompt: saved with the character", again.command("prompt"), "Your prompt is: <{vitality}>")
    check("prompt: and used after login", "ok" if re.match(r"^<(2\d|30)> $", last_line(again.command("look"))) else "no", "ok")


ANSI_RE = h.ANSI


def keep_alive(c, seconds):
    """Send only what is not typing: empty lines and the telnet NOP, once a second."""
    end = time.time() + seconds
    while time.time() < end:
        c.sock.sendall(b"\r\n")
        c.sock.sendall(b"\xff\xf1")
        time.sleep(1)


def who_line(wiz, name):
    for line in wiz.command("who").splitlines():
        if name in line:
            return line
    return "(not listed)"


def run_idle(wiz):
    # The sync line counts as input for the idle timer, so the idle checks read in the raw mode (until the prompt).
    sync_before, h.Client.sync_default = h.Client.sync_default, False
    try:
        run_idle_checks(wiz)
    finally:
        h.Client.sync_default = sync_before


def run_idle_checks(wiz):
    dozer = login("dozer")
    dozer.command("title Dozer")
    sleepy = login("sleepy")
    check("idle: setup, a second wizard", wiz.command("promote sleepy 1"), "wizard level 1")
    sleepy.command("save")
    sleepy_quiet_since = time.time()
    napper = login("napper")
    napper.command("look")
    keep_alive(napper, 6)  # dozer and sleepy are as quiet; the wizard keeps typing
    check("idle: empty lines and telnet NOP do not count as activity", who_line(wiz, "Napper"), "(idle ")
    check("idle: a player who keeps typing is not marked", who_line(wiz, "Wiz"), "Wiz", absent=("(idle",))
    napper.command("look")
    a.drain(napper, 0.7)
    check("idle: typing clears the mark", who_line(wiz, "Napper"), "Napper", absent=("(idle",))
    check("idle: a quiet wizard is marked too", who_line(wiz, "Sleepy"), "[wizard 1]", "(idle ")

    # The warning comes before the disconnect (timeout 14 s, warning at 7 s).
    check("idle: a warning comes before the disconnect", dozer.read_until("idle for a while", timeout=15), "You have been idle for a while", "disconnected")
    check("idle: then the disconnect, with a message", dozer.read_until("idle for too long", timeout=20), "saved and you are disconnected")
    check("idle: and the connection closes", "ok" if dozer.closed(timeout=10) else "still open", "ok")
    again = login("dozer")
    check("idle: the character was saved (its title is back)", who_line(wiz, "Dozer"), "Dozer")
    again.command("quit")

    # A wizard is exempt from the disconnect.
    time.sleep(max(0, sleepy_quiet_since + 17 - time.time()))
    check("idle: a wizard idle past the timeout is still connected", sleepy.command("look"), "Obvious exits", absent=("disconnected",))

    # Item 8: someone idle drops out of the shared experience.
    fresh_pinata(wiz)
    p1, p2 = lab_player("idlea"), lab_player("idleb")
    x1, x2 = xp_of(p1), xp_of(p2)
    p1.command("zap pinata 50")
    keep_alive(p1, 5)
    p2.command("zap pinata 50")
    a.drain(p2, 0.5)
    check("idle shared xp: the idle contributor is dropped, the other gets it all", "ok" if xp_of(p2) - x2 == 100 else str(xp_of(p2) - x2), "ok")
    a.drain(p1, 0.7)
    gained = xp_of(p1) - x1
    check("idle shared xp: the idle one gains no experience", "ok" if gained == 0 else str(gained), "ok")


def ansi_free(text):
    return ANSI_RE.sub("", text)


if __name__ == "__main__":
    sys.exit(main())
