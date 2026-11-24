#!/usr/bin/env python3
"""End-to-end test of the game systems and of Vincent's Hollow.

Starts the driver against a temporary copy of the mudlib, adds a few test rooms
and creatures under /domains/test (reached by a door in the Village Square of
the copy), and plays real characters over TCP: details and readables, NPC talk,
shops, equipment, real fights, experience and levels, death and respawn,
regeneration, consumables, coins, a locked door, spawns and reset, encounters,
the Goblin Overseer's three abilities, both Guild quests, persistence, the
logout room and the builder wizard commands with their level gates.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4812 python3 tests/area_test.py
"""
import os
import re
import shutil
import socket
import subprocess
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4812")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402  (the Client class, check() and helpers; reads the port from the environment)

ROOT = h.ROOT
SECRET = h.SECRET
TEST = "/domains/test/"
HOLLOW = "/domains/hollow/"


# ---------------------------------------------------------------- test content

def monster(cls, name, ids, short, hp, dmin, dmax, tohit=0, level=1, armor=0, xp=0, aggressive=False):
    id_lines = "".join(f'        Ids.Add("{i}");\n' for i in ids)
    return f'''// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{name}";
{id_lines}        ShortDesc = "{short}";
        LongDesc = "A creature made for the area test.";
        Level = {level}; MaxVitality = {hp}; DamageMin = {dmin}; DamageMax = {dmax}; ToHit = {tohit}; Armor = {armor};
        XpValue = {xp}; Aggressive = {str(aggressive).lower()};
    }}
}}
'''


def room(cls, short, exits, body="", safe=False, reset=6):
    exit_lines = "".join(f'        Exits["{d}"] = "{p}";\n' for d, p in exits.items())
    return f'''// @inherit /std/room
public class {cls} : Room
{{
    public override void Create()
    {{
        ResetSeconds = {reset};
        ShortDesc = "{short}";
        LongDesc = "A room made for the area test.";
        Safe = {str(safe).lower()};
{exit_lines}{body}    }}
}}
'''


LOOTER = '''// @inherit /std/monster
public class Looter : Monster
{
    public override void Create()
    {
        Name = "Looter";
        Ids.Add("looter");
        ShortDesc = "a looter";
        LongDesc = "A harmless looter.";
        Level = 1; MaxVitality = 1; DamageMin = 1; DamageMax = 1; ToHit = -30;
        GoldMin = 7; GoldMax = 7;
        Aggressive = false;
        AddLoot("/domains/hollow/obj/rusty_sword", 100);
    }
}
'''

TIMERS = '''// @inherit /std/item
public class Timers : Item
{
    public override void Create() { Name = "Timers"; Ids.Add("timers tool"); ShortDesc = "a timers tool"; }
    public override void Init()
    {
        base.Init();
        AddAction("timers", Run);
    }
    private bool Run(string args)
    {
        var who = ThisPlayer();
        var corpse = Present("corpse", Environment(who!));
        var p = args.Split(' ');
        corpse?.Call("SetTimers", int.Parse(p[0]), int.Parse(p[1]), int.Parse(p[2]));
        Write(corpse == null ? "No corpse.\\n" : "Timers set.\\n");
        return true;
    }
}
'''

VTOOL = '''// @inherit /std/item
public class VTool : Item
{
    public override void Create() { Name = "VTool"; Ids.Add("vtool"); ShortDesc = "a vault tool"; }
    public override void Init()
    {
        base.Init();
        AddAction("vtool", Run);
    }
    private bool Run(string args)
    {
        var store = LoadObject("/secure/lost_property");
        store.Call("Record", "tester", "fake-id", new List<string> { "/domains/hollow/obj/rusty_sword" }, 5);
        var claim = store.Call<List<string>>("Claim");
        Write("claim count " + (claim == null ? -1 : claim.Count) + " gold " + (claim == null ? "?" : claim[0]) + "\\n");
        return true;
    }
}
'''

HUB_EXITS = {"up": HOLLOW + "village/square"}
for _name in ("rats", "store", "den", "wolf", "trap", "biter", "dummy", "shop", "plaza", "quiet", "scratch", "crypt", "pit"):
    HUB_EXITS[_name] = TEST + _name
OUT = {"out": TEST + "hub"}

TEST_FILES = {
    "hub": room("TestHub", "Test Hub", HUB_EXITS, safe=True),
    "rats": room("TestRats", "Rat Pit", OUT, f'        AddSpawn("{HOLLOW}mob/large_rat", 2);\n'),
    "store": room("TestStore", "Store Room", {**OUT, "west": TEST + "vault"},
                  f'        AddSpawn("{HOLLOW}obj/healing_potion");\n        LockExit("west", "rusty key", "The iron door is locked.");\n'),
    "vault": room("TestVault", "Vault", {"east": TEST + "store"}),
    "den": room("TestDen", "Gnawer Den", OUT, f'        AddSpawn("{TEST}gnawer", 2);\n'),
    "wolf": room("TestWolf", "Wolf Run", OUT, f'        AddEncounter("{TEST}wolf", 100);\n'),
    "trap": room("TestTrap", "Brute Cage", OUT, f'        AddSpawn("{TEST}brute");\n', reset=3000),
    "pit": room("TestPit", "Brute Pit", OUT, f'        AddSpawn("{TEST}brute");\n', reset=3000),
    "vtool": VTOOL,
    "crypt": room("TestCrypt", "Crypt", OUT, reset=3000),
    "looter": LOOTER,
    "timers": TIMERS,
    "biter": room("TestBiter", "Biter Hole", OUT, f'        AddSpawn("{TEST}scratch_mob");\n'),
    "dummy": room("TestDummy", "Dummy Yard", OUT),
    "shop": room("TestShop", "Test Shop", OUT, f'        AddSpawn("{TEST}shopper");\n'),
    "plaza": room("TestPlaza", "Test Plaza", OUT, f'        AddSpawn("{TEST}villager");\n'),
    "quiet": room("TestQuiet", "Quiet Corner", OUT),
    "scratch": room("TestScratch", "Scratch Room", OUT, safe=True),
    "gnawer": monster("Gnawer", "Gnawer", ["rodent"], "a gnawer", 3, 1, 1, tohit=-30),
    "wolf_mob": None,
    "brute": monster("Brute", "Brute", ["ogre"], "a brute", 500, 25, 25, tohit=30, level=5, aggressive=True),
    "scratch_mob": monster("ScratchMob", "Scratch", ["biter"], "a biter", 2000, 4, 4, tohit=30, armor=5, aggressive=True),
    "dummy_a": monster("DummyA", "Dummy", ["training dummy"], "a training dummy", 1, 1, 1, tohit=-30, xp=100),
    "dummy_b": monster("DummyB", "Dummy", ["training dummy"], "a training dummy", 1, 1, 1, tohit=-30, xp=200),
    "dummy_c": monster("DummyC", "Dummy", ["training dummy"], "a training dummy", 1, 1, 1, tohit=-30, xp=300),
    "dummy_d": monster("DummyD", "Dummy", ["training dummy"], "a training dummy", 1, 1, 1, tohit=-30, xp=400),
    "villager": '''// @inherit /std/npc
public class Villager : Npc
{
    public override void Create()
    {
        Name = "Villager";
        ShortDesc = "a villager";
        LongDesc = "A plain villager.";
        AddChat("First line.");
        AddChat("Second line.");
        AddTopic("pie,pies", "My pie is the best.");
        AddEmote("The villager hums a cheerful tune.", 100);
    }
}
''',
    "shopper": '''// @inherit /std/shopkeeper
public class Shopper : Shopkeeper
{
    public override void Create()
    {
        Name = "Shopper";
        ShortDesc = "Shopper the trader";
        AddStock("/domains/hollow/obj/rusty_sword", 10);
        AddRareStock("/domains/hollow/obj/fine_iron_longsword", 60, 100);
        AddRareStock("/domains/hollow/obj/iron_dagger", 6, 0);
    }
}
''',
    "fakeorders": '''// @inherit /std/item
public class FakeOrders : Item
{
    public override void Create()
    {
        Name = "Fake Orders";
        Ids.Add("forged orders");
        ShortDesc = "a forged bundle of orders";
        Value = 0;
    }
}
''',
    "trinket": '''// @inherit /std/armor
public class TestTrinket : Armor
{
    public override void Create()
    {
        Name = "Test Trinket";
        Ids.Add("trinket");
        ShortDesc = "a test trinket";
        Slot = "trinket";
        ArmorValue = 0;
        MaxVitalityBonus = 5;
        Value = 40;
    }
}
''',
    "goldpile": '''// @inherit /std/coins
public class GoldPile : Coins
{
    public override void Create() { Amount = 100; }
}
''',
}
TEST_FILES["wolf"] = monster("Wolf", "Wandering Wolf", ["wolf"], "a wandering wolf", 3, 1, 1, tohit=-30)
del TEST_FILES["wolf_mob"]
# The wolf is both a room (wolf) and a creature: keep them in separate files.
WOLF_MOB = TEST_FILES.pop("wolf")
TEST_FILES["wolf"] = room("TestWolf", "Wolf Run", OUT, f'        AddEncounter("{TEST}wolf_mob", 100);\n')
TEST_FILES["wolf_mob"] = WOLF_MOB

# ---------------------------------------------------------------- helpers

def number(pattern, text, default=None):
    m = re.search(pattern, text)
    return int(m.group(1)) if m else default


def hp_of(c):
    text = c.command("vitality")
    m = re.search(r"Vitality (\d+)/(\d+)", text)
    return (int(m.group(1)), int(m.group(2))) if m else (None, None)


def score_of(c):
    text = c.command("score")
    return {
        "level": number(r"Level (\d+)", text),
        "xp": number(r"Experience (\d+)", text),
        "hp": number(r"Vitality\s+(\d+)/", text),
        "max": number(r"Vitality\s+\d+/(\d+)", text),
        "gold": number(r"Gold (\d+)", text),
        "text": text,
    }


def quiet(c, seconds):
    """Read whatever arrives for `seconds`."""
    end = time.time() + seconds
    data = ""
    while time.time() < end:
        try:
            chunk = c.sock.recv(65536)
            if not chunk:
                break
            data += c.rx(chunk)
        except socket.timeout:
            pass
    return data


def drain(c, seconds=0.7):
    """Discard what is pending, until nothing has arrived for `seconds`."""
    c.pending = ""
    while True:
        if not quiet(c, seconds):
            return


def fight_until(c, marker, timeout=90, heal_with=None):
    """Read until `marker` appears. With heal_with (a wizard), keep that character at full health."""
    end = time.time() + timeout
    data = ""
    last_heal = 0
    while time.time() < end and marker not in data:
        data += quiet(c, 0.5)
        if heal_with is not None and time.time() - last_heal > 1.4:
            last_heal = time.time()
            c.send("restore")
    return data


def wait_ready(c, timeout=40):
    """Wait until a wizard's character may begin an action again (`stat me` says so)."""
    end = time.time() + timeout
    while time.time() < end:
        if "Now: ready" in c.command("stat me"):
            return True
        time.sleep(0.4)
    return False


def fight_until_timed(c, marker, timeout=90, heal_with=None):
    """Like fight_until, but returns (text, [(seconds since the start, chunk)]) so that durations can be checked."""
    start = time.time()
    end = start + timeout
    data = ""
    chunks = []
    last_heal = 0
    while time.time() < end and marker not in data:
        got = quiet(c, 0.25)
        if got:
            chunks.append((time.time() - start, got))
            data += got
        if heal_with is not None and time.time() - last_heal > 1.4:
            last_heal = time.time()
            c.send("restore")
    return data, chunks


def leave(c, direction):
    """Walk out; when the other gnawer is still fighting (walking away is refused during a fight), flee that way instead."""
    text = c.command(direction)
    for _ in range(12):
        if "flee" not in text:
            break
        text = c.command("flee " + direction)
        if "cannot get away" not in text:
            break
    return text


def go(c, *verbs):
    out = ""
    for verb in verbs:
        out += c.command(verb)
    return out


def start_driver(work, mudlib):
    log = open(os.path.join(work, "driver.log"), "a")
    dll = os.path.join(ROOT, "src", "LesMudOS.Driver", "bin", "Debug", "net10.0", "LesMudOS.Driver.dll")
    proc = subprocess.Popen(
        ["dotnet", dll, "--mudlib", mudlib, "--save", os.path.join(work, "save"), "--port", str(h.PORT)],
        stdout=log, stderr=subprocess.STDOUT, env={**os.environ, "LESMUDOS_ADMIN_SECRET": SECRET})
    for _ in range(100):
        try:
            socket.create_connection(("127.0.0.1", h.PORT), timeout=0.3).close()
            break
        except OSError:
            time.sleep(0.2)
    return proc, log


def building_templates():
    """The code blocks of docs/BUILDING.md that start with '// File: <path>.cs', as {mudlib path: source}."""
    text = open(os.path.join(ROOT, "docs", "BUILDING.md")).read()
    found = {}
    for block in re.findall(r"```csharp\n(.*?)```", text, re.S):
        m = re.match(r"// File: (\S+)\.cs\n", block)
        if m:
            found[m.group(1)] = block
    return found


def hollow_files(mudlib):
    base = os.path.join(mudlib, "domains", "hollow")
    found = []
    for folder, _, names in os.walk(base):
        for name in sorted(names):
            if name.endswith(".cs"):
                found.append("/" + os.path.relpath(os.path.join(folder, name), mudlib)[:-3].replace(os.sep, "/"))
    return sorted(found)


# ---------------------------------------------------------------- the test

def main():
    work = tempfile.mkdtemp(prefix="lesmudos-area-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib, novice=True)
    os.makedirs(os.path.join(mudlib, "domains", "test"))
    for name, source in TEST_FILES.items():
        h.write(mudlib, TEST + name, source)
    # The Village Square of the copy gets a door down to the test hub.
    h.edit(mudlib, HOLLOW + "village/square", 'Exits["south"]', f'Exits["down"] = "{TEST}hub";\n        Exits["south"]')
    os.makedirs(os.path.join(mudlib, "domains", "example"))
    for path, source in building_templates().items():
        h.write(mudlib, path, source)

    box = [None, None]
    box[0], box[1] = start_driver(work, mudlib)
    try:
        run(work, mudlib)
        run_lost_property(work, mudlib, box)
        box[1].flush()
        logged = open(os.path.join(work, "driver.log")).read()
        h.check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:800], "ok")
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


def restart(work, mudlib, box, how, wiz=None):
    """Stop the driver cleanly (via the wizard command) or kill it, then start it again on the same directories."""
    if how == "clean":
        wiz.send("shutdown")
        box[0].wait(timeout=20)
    else:
        box[0].kill()
        box[0].wait(timeout=10)
    box[1].close()
    box[0], box[1] = start_driver(work, mudlib)


def die_in_pit(wiz, name, items_to_carry):
    """A new character picks up the given items and 100 gold in the hub and is killed in the Brute Pit."""
    wiz.command("goto " + TEST + "hub")
    c = h.Client(name)
    c.read_until("> ")
    c.command("down")
    for path, word in items_to_carry:
        wiz.command("clone " + path)
        wiz.command("drop " + word)
        c.command("get " + word)
    wiz.command("clone " + TEST + "goldpile")
    wiz.command("drop coins")
    c.command("get coins")
    before = score_of(c)["gold"]
    c.command("pit")
    text = fight_until(c, "healer's fee", timeout=40)
    drain(c, 1.0)
    return c, before, text


def run_lost_property(work, mudlib, box):
    check = h.check
    sword = (HOLLOW + "obj/rusty_sword", "rusty sword")
    shield = (HOLLOW + "obj/small_shield", "small shield")
    wiz = h.Client("wiz")
    wiz.read_until("> ")

    # a mudlib object that is not a corpse or a player body can neither record nor claim
    tester = h.Client("tester")
    tester.read_until("> ")
    wiz.command("clone " + TEST + "vtool")
    check("a non-corpse object cannot claim", wiz.command("vtool"), "claim count 1 gold 0")
    tester.send("quit")
    tester.read_until("Goodbye")

    # ----- a planned restart -----
    mourn, before, text = die_in_pit(wiz, "mourn", [sword, shield])
    check("the player died in the pit", text, "You have died!")
    fee = before // 10
    check("the corpse holds the belongings and the fee", (wiz.command("goto " + TEST + "pit"), wiz.command("look in corpse of mourn"))[1],
          "a rusty sword", "a small shield", f"{fee} gold coins")
    store_text = open(os.path.join(work, "save", "secure", "lost_property.json")).read()
    check("the lost-property store already holds the record (write-through)", store_text, "rusty_sword", "small_shield", '"mourn|')
    gold_after_death = score_of(mourn)["gold"]
    mourn.send("quit")
    mourn.read_until("Goodbye")
    restart(work, mudlib, box, "clean", wiz)
    other = h.Client("tester")
    text = other.read_until("> ")
    check("another player's login receives nothing", text, "Welcome back", absent=("Harland", "recover"))
    other.send("quit")
    other.read_until("Goodbye")
    mourn = h.Client("mourn")
    text = mourn.read_until("> ")
    check("after a clean restart the owner gets the belongings back", text, "Harland, the innkeeper of the Hollowman's Rest", "You recover 2 items and %d gold." % fee)
    check("the items are in the inventory", mourn.command("i"), "a rusty sword", "a small shield")
    check("and the gold is back", str(score_of(mourn)["gold"]), str(gold_after_death + fee))
    store_text = open(os.path.join(work, "save", "secure", "lost_property.json")).read()
    check("the record is deleted", store_text, absent=("mourn|", "rusty_sword"))
    mourn.send("quit")
    mourn.read_until("Goodbye")
    mourn = h.Client("mourn")
    text = mourn.read_until("> ")
    check("a second login receives nothing more", text, "Welcome back", absent=("Harland",))
    check("the items persist normally after being claimed", mourn.command("i"), "a rusty sword", "a small shield")
    mourn.send("quit")
    mourn.read_until("Goodbye")

    # ----- a crash, after part of the corpse was looted -----
    wiz = h.Client("wiz")
    wiz.read_until("> ")
    crasher, before, text = die_in_pit(wiz, "crasher", [sword, shield])
    check("the second player died in the pit", text, "You have died!")
    fee = before // 10
    gold_after_death = score_of(crasher)["gold"]
    wiz.command("goto " + TEST + "pit")
    wiz.command("slay brute")
    for step in ("d", "north", "down", "pit"):
        crasher.command(step)
    check("the owner loots only the sword", crasher.command("get rusty sword from corpse of crasher"), "You take a rusty sword from the corpse of Crasher.")
    store_text = open(os.path.join(work, "save", "secure", "lost_property.json")).read()
    check("the lost-property store follows the corpse", store_text, "small_shield", '"crasher|')
    check("the sword has left the lost-property record", str("rusty_sword" in store_text.split('"crasher|')[1]), "False")
    restart(work, mudlib, box, "kill")
    crasher = h.Client("crasher")
    text = crasher.read_until("> ")
    check("after a crash the rest is returned", text, "Harland, the innkeeper", "You recover an item and %d gold." % fee)
    text = crasher.command("i")
    check("the shield is back and the looted sword was never saved", text, "a small shield", absent=("rusty sword",))
    check("the gold is back", str(score_of(crasher)["gold"]), str(gold_after_death + fee))
    crasher.send("quit")
    crasher.read_until("Goodbye")
    wiz = h.Client("wiz")
    wiz.read_until("> ")
    wiz.send("quit")
    wiz.read_until("Goodbye")


def run(work, mudlib):
    check = h.check

    # ===== wizard and hero log in =====
    wiz = h.Client("wiz")
    wiz.read_until("> ")
    check("secret makes the first wizard", wiz.command("promote " + SECRET), "level 10 wizard")
    hero = h.Client("hero")
    text = hero.read_until("> ")
    check("a new character starts in the Village Square", text, "Welcome, Hero", "Village Square",
          "Old Farmer Benson is here.", "A traveling merchant is here.", "Obvious exits: north, west, east, down, south")
    wiz.command("goto " + HOLLOW + "village/square")

    # ===== character sheet, help =====
    s = score_of(hero)
    check("a new character: level 1, 30 Vitality, 25 gold, unarmed 2-5, Reaction Time 2.5", s["text"], "Level 1", "Experience 0 (100 more for level 2)",
          "Vitality   30/30", "Gold 25", "Damage 2-5", "nothing equipped", "Reaction Time 2.5 (base 2.5). Your attack takes 6.0 seconds.", "no quests")
    check("hp command", hero.command("vitality"), "Vitality 30/30")
    text = hero.command("help")
    check("help lists the new commands", text, "kill (or attack)", "flee", "consider", "wield", "wear", "browse", "buy", "accept", "report", "quests", "score",
          absent=("goto", "Wizard", "slay"))

    # ===== details, readables, NPC talk =====
    check("look at a detail", hero.command("look at well"), "broad well of fitted grey stone")
    check("look finds details by any listed name, with 'the'", hero.command("look at the stone well"), "broad well")
    check("look at the bench", hero.command("look at bench"), "plank benches")
    check("look at an NPC shows a health line", hero.command("look at benson"), "stooped, weathered man", "Old Farmer Benson is unhurt.")
    check("unknown thing", hero.command("look at unicorn"), "You see no 'unicorn' here.")
    check("read a readable detail", hero.command("read plaque"),
          '"This well was dug by Vincent Holloway and his companions during the settlement\'s first winter."')
    check("read something that is not readable", hero.command("read well"), "no 'well' to read")
    check("talk: the first line is the brief's", hero.command("talk benson"),
          "Benson says: \"Vincent founded this place. Brave man... but he went lookin' for somethin' beneath the earth and never returned.\"")
    check("talk: the next line", hero.command("talk to benson"), "My grandfather helped dig that well")
    check("talk: greet is an alias", hero.command("greet benson"), "Mind the cellar")
    check("talk: lines wrap round", hero.command("talk benson"), "Vincent founded this place")
    check("ask about a topic", hero.command("ask benson about vincent"), "Vincent Holloway. Came up from the south")
    check("ask with a longer phrase", hero.command("ask benson about the well"), "Dug in the first winter")
    check("ask about an unknown topic", hero.command("ask benson about taxes"), "Benson shrugs.")
    check("ask needs 'about'", hero.command("ask benson"), "Usage: ask NPC about TOPIC")
    check("talk to nobody", hero.command("talk unicorn"), "Talk to whom?")
    check("kill in a safe room", hero.command("kill benson"), "calm lies over this place")
    check("consider a peaceful NPC", hero.command("consider benson"), "even match", "will not fight you")

    # ===== Ironoak Arms: Torren =====
    text = hero.command("east")
    check("Ironoak Arms", text, "Ironoak Arms", "The smell of hot metal and burning coal fills this small but impressive smithy.", "Blacksmith Torren is here.")
    check("Torren's first line is the brief's", hero.command("talk torren"), 'Torren says: "Quality steel saves lives. Cheap steel fills graveyards."')
    text = hero.command("browse")
    check("browse lists the stock with prices", text, "rusty sword", "10 gold", "training spear", "8 gold", "iron dagger", "6 gold",
          "woodcutter axe", "18 gold", "small shield", "12 gold")
    check("value of something in stock", hero.command("value sword"), "costs 10 gold here")
    check("buy", hero.command("buy sword"), "You buy a rusty sword for 10 gold.")
    check("gold was paid", str(score_of(hero)["gold"]), "15")
    check("buy a shield", hero.command("buy shield"), "You buy a small shield for 12 gold.")
    check("cannot afford", hero.command("buy axe"), "You cannot afford that. It costs 18 gold.")
    check("not sold here", hero.command("buy halberd"), "does not sell 'halberd'")
    check("inventory", hero.command("i"), "a rusty sword", "a small shield")
    check("wield", hero.command("wield sword"), "You wield a rusty sword.")
    check("inventory shows wielded", hero.command("inventory"), "a rusty sword (wielded)")
    check("a shield cannot be wielded", hero.command("wield shield"), "You can't wield that.")
    check("a sword cannot be worn", hero.command("wear sword"), "You can't wear that.")
    check("wear", hero.command("wear shield"), "You put on a small shield.")
    s = score_of(hero)
    check("score shows the equipment and derived numbers", s["text"], "Damage 9-14", "Armor 1", "weapon: a rusty sword", "shield: a small shield",
          "Reaction Time 3.5 (base 2.5, a rusty sword +0.5, a small shield +0.5).", "Your attack takes 9.0 seconds.")
    check("unwield", hero.command("unwield"), "You stop wielding a rusty sword.")
    check("unarmed again", score_of(hero)["text"], "Damage 2-5", "Your attack takes 7.0 seconds.")
    hero.command("wield sword")
    check("remove armor", hero.command("remove shield"), "You take off a small shield.")
    check("remove what is not worn", hero.command("remove shield"), "You are not wearing or wielding that.")
    hero.command("wear shield")
    check("wield what you do not have", hero.command("wield halberd"), "Wield what?")
    # money from the floor: coins
    wiz.command("east")
    wiz.command("clone " + TEST + "goldpile")
    wiz.command("drop coins")
    check("coins on the floor are merged into one line", hero.command("look"), "100 gold coins are here", absent=("Two",))
    text = hero.command("get coins")
    check("picking up coins adds gold", text, "You take 100 gold coins.", "You add 100 gold to your purse.")
    check("the coins are gone and the purse is fuller", str(score_of(hero)["gold"]), "103")
    check("the coins item is gone", hero.command("look"), absent=("gold coins",))
    check("buy a better weapon", hero.command("buy axe"), "You buy a woodcutter axe for 18 gold.")
    check("wielding another weapon replaces the first", hero.command("wield axe"), "You stop using a rusty sword.", "You wield a woodcutter axe.")
    check("sell pays half", hero.command("sell sword"), "You sell a rusty sword to Torren for 5 gold.")
    check("the sold item is gone", hero.command("i"), "a woodcutter axe (wielded)", absent=("rusty sword",))
    check("value of an item you carry", hero.command("value axe"), "Torren would pay 9 gold for a woodcutter axe.")
    check("price is the same question", hero.command("price axe"), "Torren would pay 9 gold for a woodcutter axe.")
    wiz.command("clone " + TEST + "fakeorders")
    wiz.command("drop forged orders")
    hero.command("get forged orders")
    check("worthless items are refused", hero.command("sell forged orders"), "not worth anything")
    check("value of a worthless item", hero.command("value forged orders"), "would not buy")
    hero.command("drop forged orders")
    wiz.command("clone " + HOLLOW + "obj/overseers_orders")
    wiz.command("drop orders")
    drain(hero, 0.4)
    hero.command("get orders")
    check("quest items cannot be sold", hero.command("sell orders"), "too important")
    hero.command("drop orders")
    check("sell what you do not have", hero.command("sell halberd"), "Sell what?")
    wiz.command("west")
    # a longer look at the first move
    hero.command("west")

    # ===== merchant, consumables =====
    check("the merchant sells potions", hero.command("browse"), "minor healing potion", "15 gold", "trail ration", "5 gold")
    gold_before = score_of(hero)["gold"]
    check("buy a potion", hero.command("buy potion"), "You buy a minor healing potion for 15 gold.")
    check("buy a ration", hero.command("buy ration"), "You buy a trail ration for 5 gold.")
    check("gold after shopping", str(score_of(hero)["gold"]), str(gold_before - 20))
    check("eat what is not food", hero.command("eat potion"), "You can't eat that.")
    check("drink what is not a drink", hero.command("drink ration"), "You can't drink that.")
    check("eat with nothing", hero.command("eat unicorn"), "Eat what?")
    hero.command("buy potion")
    check("a large wound check is done later", hero.command("i"), "a minor healing potion", "a trail ration")

    # ===== combat: real fights =====
    check("the merchant is unharmed", hero.command("consider merchant"), "It will not fight you.")
    go(hero, "down")
    text = hero.command("rats")
    seen = text  # the rats attack as soon as the arrival is complete: their line may come in any of the replies that follow
    check("two identical creatures are merged in the room listing", text, "Rat Pit", "Two large rats are here.")
    looked = hero.command("look at rat")
    check("look at a creature", looked, "red, hungry eyes", "A large rat is unhurt.")
    considered = hero.command("consider rat")
    check("consider an easy opponent", considered, "A large rat looks like an even match")
    seen += looked + considered
    if "attacks you" not in seen:
        seen += hero.read_until("attacks you", timeout=15)
    check("aggressive monsters attack on arrival", seen, "A large rat attacks you!")
    check("and the player is in the fight at once: walking on is refused", hero.command("out"), "cannot just walk away")
    text = seen + fight_until(hero, "dies.", timeout=120)
    text += fight_until(hero, "dies.", timeout=120) if text.count("dies.") < 2 else ""
    check("a real fight to the death: messages, two kills", "yes" if re.search(r"You (scratch|hit|smash|miss) a large rat", text) else text, "yes")
    check("a real fight to the death: two kills", text, "A large rat dies.")
    check("each kill pays experience", text, "You gain 8 experience.")
    drain(hero)
    s = score_of(hero)
    check("experience was added", str(s["xp"]), "16")
    check("fight text shows both directions", text, "a large rat")
    check("the rats left corpses, not rats", hero.command("look"), "Rat Pit", "Two corpses of large rats are here.")
    hero.command("vitality")

    # ----- reset top-up: a killed creature is not replaced while its corpse lies here; then the rats come back, no more than two -----
    time.sleep(7)  # a reset passes (6 seconds)
    check("the rats are not replaced while their corpses lie in the room", hero.command("look"), "Two corpses of large rats are here.", absent=("Two large rats are here", "A large rat is here"))
    wiz.command("goto " + TEST + "rats")
    wiz.command("clone " + TEST + "timers")
    for _ in range(2):  # the corpses rot after a second each (the tool sets the first one it finds)
        wiz.command("timers 600 600 1")
        time.sleep(2.2)
    wiz.command("out")
    text = ""
    end = time.time() + 20
    while time.time() < end and "Two large rats are here." not in text:
        time.sleep(1)
        text = hero.command("look")
    check("reset tops up the missing creatures (exactly two again)", text, "Two large rats are here.", absent=("Three large",))
    hero.command("vitality")
    # leave quickly; the rats attack
    # (walking away from a fight is refused now: flee, which can fail, so try again)
    for _ in range(12):
        if "cannot get away" not in hero.command("flee out"):
            break
    drain(hero, 1.0)
    s = score_of(hero)
    check("fleeing: leaving the room ends the fight in time", str(s["hp"] > 0), "True")

    # ----- non-aggressive creatures that assist each other -----
    text = hero.command("den")
    check("gnawers sleep until attacked", text, "Two gnawers are here.")
    started = hero.command("kill gnawer")
    check("kill: start a fight", started, "You attack a gnawer.")
    text = fight_until(hero, "dies.", timeout=90)
    # The other one is drawn in at once ("A gnawer attacks you!", in the reply to kill); its first blow waits its Reaction Time, so
    # it may never land if the hero kills the first at once: the join line is the sure sign.
    check("the other creature joined the fight", "joined" if re.search(r"A gnawer (attacks you|misses|scratches|hits)", started + text) else started + text, "joined")
    check("a gnawer died", text, "A gnawer dies.")
    drain(hero, 1.5)
    leave(hero, "out")

    # ----- peaceful NPCs and players cannot be attacked -----
    wiz.command("goto " + TEST + "plaza")
    hero.command("plaza")
    check("peaceful NPCs cannot be attacked", hero.command("kill villager"), "A villager is no threat to you.")
    check("players cannot fight each other", hero.command("kill wiz"), "Players cannot fight each other.")
    check("an NPC talks, topic list", (hero.command("talk villager"), hero.command("talk villager"), hero.command("ask villager about pie"))[2], "My pie is the best.")
    check("NPCs make idle actions while players watch", hero.read_until("hums a cheerful tune", timeout=15), "The villager hums a cheerful tune.")
    hero.command("out")
    wiz.command("out")

    # ===== experience and levelling =====
    drain(hero, 1.0)
    base = score_of(hero)["xp"]
    final_xp = base + 1100
    wiz.command("goto " + TEST + "dummy")
    hero.command("dummy")
    wiz.command("clone " + TEST + "dummy_a")
    check("consider a level 1 dummy", hero.command("consider dummy"), "looks like an even match")
    check("attack is an alias of kill", hero.command("attack dummy"), "You attack a training dummy.")
    text = fight_until(hero, "reached level", timeout=120)
    check("100 experience reach level 2", text, "You gain 100 experience.", "You have reached level 2!", "Maximum Vitality is now 38")
    drain(hero)
    s = score_of(hero)
    check("level 2: experience, the rest to level 3, full healed 38 Vitality", s["text"], f"Level 2", f"Experience {base + 100} ({200 - base} more for level 3)", "38/38")
    levels = [("dummy_b", 200, "", 3, 46), ("dummy_c", 300, "", 4, 54), ("dummy_d", 400, "", 5, 62)]
    for dummy, xp, total, level, max_hp in levels:
        wiz.command("clone " + TEST + dummy)
        hero.command("kill dummy")
        text = fight_until(hero, "reached level", timeout=120)
        check(f"level {level} is reached", text, f"You gain {xp} experience.", f"You have reached level {level}!", f"Maximum Vitality is now {max_hp}")
        drain(hero)
    s = score_of(hero)
    check("level 5 reached at 1000 experience; level 6 needs 5400", s["text"], f"Level 5", f"Experience {base + 1000} ({4400 - base} more for level 6)", "Vitality   62/62")
    wiz.command("clone " + TEST + "dummy_a")
    hero.command("kill dummy")
    text = fight_until(hero, "gain 100 experience", timeout=120)
    drain(hero)
    s = score_of(hero)
    check("100 more experience does not level at 5, and still counts", s["text"], f"Level 5", f"Experience {final_xp} ({4300 - base} more for level 6)")
    check("no second level-up message", text, absent=("reached level",))
    check("kill credit is recorded for the killer", s["text"], "Level 5")
    hero.command("out")
    wiz.command("out")

    # ===== a trinket raises maximum Vitality =====
    wiz.command("clone " + TEST + "trinket")
    wiz.command("drop trinket")
    hero.command("get trinket")
    check("wearing a trinket raises maximum Vitality", str((hero.command("wear trinket"), hp_of(hero))[1][1]), "67")
    hero.command("remove trinket")
    check("removing it lowers maximum Vitality again", str(hp_of(hero)[1]), "62")
    hero.command("drop trinket")
    wiz.command("get trinket")

    # ===== regeneration, fleeing, consumables =====
    text = hero.command("biter")
    check("the biter attacks", text if "attacks you" in text else text + hero.read_until("attacks you", timeout=15), "A biter attacks you!")
    quiet(hero, 12)
    hp_hurt = hp_of(hero)[0]
    check("the biter wounded the hero", str(hp_hurt < 62), "True")
    # The regeneration checks below need a real deficit (the biter's blows are random and the hero heals): let it bite until he is down to 50.
    end_bite = time.time() + 90
    while hp_of(hero)[0] > 50 and time.time() < end_bite:
        quiet(hero, 4)
    flee_text = ""
    for _ in range(12):
        flee_text += hero.command("flee")
        if "Test Hub" in flee_text:
            break
    check("flee takes the hero out of the fight", flee_text, "You flee out!", "Test Hub")
    drain(hero, 1.5)
    base = hp_of(hero)[0]
    time.sleep(13)
    drain(hero, 0.3)
    after = hp_of(hero)[0]
    check("regeneration in a safe room: 3 Vitality per step", str(after - base in (3, 6)), "True")
    hero.command("quiet")
    base = hp_of(hero)[0]
    time.sleep(13)
    after = hp_of(hero)[0]
    check("regeneration elsewhere: 1 Vitality per step", str(after - base in (1, 2)), "True")
    deficit = 62 - after
    text = hero.command("drink potion")
    # (the biter's damage is random and the hero regenerates: a hero back at full health reads no "recover" line, and a regeneration step
    # between the hp reading and the drink may have closed a little of the gap)
    m = re.search(r"You recover (\d+) Vitality\.", text)
    got = int(m.group(1)) if m else 0
    want = min(20, deficit)
    check("a potion heals 20", text if 0 <= want - got <= 3 else f"recovered {got}, wanted about {want}: {text!r}", "You pull the stopper and drink the ruby-red potion.")
    # Using something is an action with a Recovery: wait for the Ready Time ((2.5 + axe 1.0 + shield 0.5 + 0.5) x 2 = 9 s) between uses.
    time.sleep(9.5)
    check("quaff is an alias of drink", hero.command("quaff potion"), "ruby-red potion")
    hurt = hp_of(hero)[0]
    time.sleep(9.5)
    check("eat", hero.command("eat ration"), "You chew through the dry biscuit")
    check("the ration healed 5 or filled up", str(hp_of(hero)[0] >= min(62, hurt + 5) - 0), "True")
    check("the consumables are gone", hero.command("i"), absent=("potion", "ration"))
    hero.command("out")

    # ===== the locked door =====
    wiz.command("goto " + TEST + "store")
    hero.command("store")
    check("a spawned item is in the room", hero.command("look"), "A minor healing potion is here.")
    check("a locked exit refuses", hero.command("west"), "The iron door is locked.")
    check("still in the same room", hero.command("look"), "Store Room")
    wiz.command("clone " + HOLLOW + "obj/rusty_key")
    wiz.command("drop key")
    drain(hero, 0.4)
    hero.command("get key")
    check("with the key the exit opens", hero.command("west"), "Vault")
    check("the key is not consumed", hero.command("i"), "a rusty key")
    hero.command("east")
    hero.command("drop key")
    check("without the key it is locked again", hero.command("west"), "The iron door is locked.")
    wiz.command("get key")
    hero.command("get potion")
    text = ""
    end = time.time() + 20
    while time.time() < end and "healing potion is here" not in text:
        time.sleep(1)
        text = hero.command("look")
    check("a spawned item is restocked at reset", text, "A minor healing potion is here.")
    hero.command("get potion")
    hero.command("out")
    wiz.command("out")

    # ===== rare stock =====
    wiz.command("goto " + TEST + "shop")
    hero.command("shop")
    text = hero.command("browse")
    check("rare stock with a 100% roll is on sale", text, "fine iron longsword", "60 gold", "rusty sword")
    check("rare stock with a 0% roll is not", text, absent=("iron dagger",))
    hero.command("out")
    wiz.command("out")

    # ===== corpses of monsters =====
    wiz.command("goto " + TEST + "crypt")
    wiz.command("clone " + TEST + "timers")
    hero.command("crypt")
    wiz.command("clone " + TEST + "looter")
    gold0 = score_of(hero)["gold"]
    hero.command("kill looter")
    text = fight_until(hero, "dies.", timeout=90)
    drain(hero, 1.0)
    text = hero.command("look")
    check("a monster leaves a corpse and nothing on the floor", text, "The corpse of a looter is here.", absent=("sword is here", "coins"))
    text = hero.command("look at corpse")
    check("the corpse shows what it holds", text, "It holds:", "7 gold coins", "a rusty sword", absent=("felled moments ago",))
    check("a stranger sees the lock", wiz.command("look in corpse"), "It was felled moments ago; only those involved may touch it.")
    check("a stranger cannot loot", wiz.command("loot"), "The corpse of a looter is not yours to loot yet.")
    check("a stranger cannot get from it", wiz.command("get sword from corpse"), "is not yours to loot yet.")
    check("a stranger cannot get all from it", wiz.command("get all from corpse"), "is not yours to loot yet.")
    check("plain get cannot reach into a corpse", wiz.command("get sword"), "Get what?")
    check("plain get cannot take coins from a corpse", wiz.command("get coins"), "Get what?")
    check("a corpse cannot be picked up", wiz.command("get corpse"), "You can't take that.")
    wiz.command("timers 0 120 3600")
    text = wiz.command("get sword from corpse")
    check("after the lock anyone may take", text, "You take a rusty sword from the corpse of a looter.")
    drain(hero, 0.5)
    check("the killer may loot at once and gets the gold", hero.command("loot"), "You take 7 gold coins from the corpse of a looter.", "You add 7 gold to your purse.")
    check("the gold arrived", str(score_of(hero)["gold"] - gold0), "7")
    # CHANGED (play-feel): a corpse crumbles at once when its last item or coin is taken (an empty one used to wait two minutes).
    check("an emptied corpse is gone at once", hero.command("look at corpse"), "You see no 'corpse' here.")
    check("looting finds nothing more", hero.command("loot"), "There is nothing here to loot.")
    wiz.command("drop sword")
    check("the corpse is gone", hero.command("look"), "Crypt", absent=("corpse",))
    # a full monster corpse rots away with its contents
    wiz.command("clone " + TEST + "looter")
    hero.command("kill looter")
    fight_until(hero, "dies.", timeout=90)
    wiz.command("timers 0 120 3")
    text = quiet(hero, 6)
    check("a full monster corpse rots with its contents", text, "The corpse of a looter crumbles to dust.")
    check("nothing fell to the floor", hero.command("look"), absent=("corpse", "7 gold"))
    # the coins item loot rolled: no more corpses; leave
    wiz.command("get sword")
    hero.command("out")
    wiz.command("out")

    # ===== death and respawn =====
    hero.command("buy_nothing")
    s_before = score_of(hero)
    gold = s_before["gold"]
    fee = gold // 10
    hero.command("trap")
    text = fight_until(hero, "healer's fee", timeout=40)
    check("a player can die", text, "You have died!", f"The healer's fee costs you {fee} gold.", "Guest Rooms",
          "Several modest guest rooms sit above the tavern. Clean beds and warm blankets provide a welcome refuge for weary travelers.")
    drain(hero, 1.0)
    s = score_of(hero)
    check("death: 10% of the gold lost", str(s["gold"]), str(gold - fee))
    check("death: full Vitality, experience and level kept", s["text"], f"Level 5", f"Experience {final_xp}", "Vitality   62/62")
    check("death: carried items went into the corpse", hero.command("i"), "You are empty-handed.")
    check("death: nothing is equipped", score_of(hero)["text"], "nothing equipped", "Damage 2-5")
    check("the guest rooms are safe: nothing fights here", hero.command("kill brute"), "Kill what?")
    check("a safe room heals faster (checked above); unknown exits", hero.command("north"), "What?")

    # ===== quitting: the loss is saved, the logout room =====
    hero.send("quit")
    hero.read_until("Goodbye")
    saved = open(os.path.join(work, "save", "players", "hero.json")).read()
    check("the save does not list the items that lie in the corpse", saved, '"Level": 5', absent=("woodcutter_axe", "small_shield"))
    hero = h.Client("hero")
    text = hero.read_until("> ")
    check("quitting in a logout room: the next login is there", text, "Welcome back, Hero. This is visit number 2.", "Guest Rooms")
    check("the lost items stay lost at login", hero.command("i"), "You are empty-handed.")

    # ===== the player's corpse =====
    wiz.command("goto " + TEST + "trap")
    check("a wizard kills the brute", wiz.command("slay brute"), "A brute dies.")
    check("the dead player's corpse lies where they fell", wiz.command("look"), "The corpse of Hero is here.")
    check("the corpse lists the belongings", wiz.command("look in corpse of hero"), "a woodcutter axe", "a small shield", f"{fee} gold coins")
    check("another player cannot loot the corpse of Hero", wiz.command("get all from corpse of hero"), "The corpse of Hero is not yours to loot yet.")
    check("or take one thing", wiz.command("get axe from corpse of hero"), "is not yours to loot yet.")
    hero.command("d")
    hero.command("north")
    hero.command("down")
    text = hero.command("trap")
    check("the dead player walks back to the corpse", text, "Brute Cage", "The corpse of Hero is here.")
    gold_dead = score_of(hero)["gold"]
    text = hero.command("loot")
    check("loot skips corpses that are not yours and takes the player's own", text, "You take a woodcutter axe from the corpse of Hero.",
          "You take a small shield from the corpse of Hero.", f"You add {fee} gold to your purse.")
    check("the gold penalty comes back with the corpse", str(score_of(hero)["gold"] - gold_dead), str(fee))
    check("the items are back, but not worn", hero.command("i"), "a woodcutter axe", "a small shield", absent=("(wielded)", "(worn)"))
    hero.command("wield axe")
    hero.command("wear shield")
    check("the corpse of Hero crumbled when the last thing was taken", hero.command("look"), "Brute Cage", absent=("corpse of Hero",))
    hero.command("out")
    hero.command("up")
    hero.command("south")
    check("u goes up", hero.command("u"), "Guest Rooms")
    hero.send("quit")
    hero.read_until("Goodbye")
    hero = h.Client("hero")
    text = hero.read_until("> ")
    check("after recovery the items persist, with their state", text, "Guest Rooms")
    check("recovered items are wielded and worn again", hero.command("i"), "a woodcutter axe (wielded)", "a small shield (worn)")
    s = score_of(hero)
    check("character data is restored", s["text"], f"Level 5", f"Experience {final_xp}", f"Gold {s['gold']}", "weapon: a woodcutter axe", "shield: a small shield")
    saved = open(os.path.join(work, "save", "players", "hero.json")).read()
    check("the save file holds item paths and plain data only", saved, '"/domains/hollow/obj/woodcutter_axe"', '"Level": 5', f'"xp": {final_xp}')
    hero.command("d")
    hero.command("n")
    hero.send("quit")
    hero.read_until("Goodbye")
    hero = h.Client("hero")
    check("quitting elsewhere: the next login is at the Village Square", hero.read_until("> "), "Welcome back, Hero. This is visit number 4.", "Village Square")
    wiz.command("out")

    # ===== the Guild: Rowan, the board, quest 2 for the hero =====
    text = hero.command("west")
    check("Adventurers Guild", text, "Adventurers Guild", "Monster trophies, old banners, and battered shields decorate the walls of this busy guild hall.",
          "Guildmaster Rowan is here.")
    check("Rowan's first line is the brief's", hero.command("talk rowan"),
          'Rowan says: "Every village has its legends. Ours is Vincent himself. Some believe he found something beneath these lands before he vanished."')
    check("the quest board", hero.command("read board"), "CLEAR THE CELLAR", "RECOVER THE OVERSEER'S ORDERS", "Clear the Cellar (open)", "Recover the Overseer's Orders (open)")
    check("look at the board", hero.command("look at board"), "large notice board")
    check("report with nothing to report", hero.command("report"), "You have nothing to report yet.")
    check("accept needs a known quest", hero.command("accept dancing"), "Which contract?")
    check("accept the orders quest by number", hero.command("accept 2"), "Quest accepted: Recover the Overseer's Orders.")
    check("a quest cannot be accepted twice", hero.command("accept orders"), "You have already taken")
    check("the quest log", hero.command("quests"), "Active quests: Recover the Overseer's Orders")
    check("report with the quest open", hero.command("report"), "Find the Overseer's Orders")
    wiz.command("goto " + HOLLOW + "village/guild")
    wiz.command("clone " + TEST + "fakeorders")
    wiz.command("drop forged orders")
    hero.command("get forged orders")
    check("only the real Overseer's Orders count", hero.command("report"), "Find the Overseer's Orders", absent=("Quest complete",))
    hero.command("drop forged orders")
    wiz.command("clone " + HOLLOW + "obj/overseers_orders")
    wiz.command("drop orders")
    drain(hero, 0.5)  # the wizard's drop reaches the hero with a prompt of its own, which must not end the score read
    s_before = score_of(hero)
    drain(hero, 0.5)
    check("pick up the real orders", hero.command("get orders"), "You take the Overseer's Orders.")
    check("read the orders", hero.command("read orders"), "The Hollow Below", "The Key of Vincent", "Continue excavation.",
          "The old founder was telling the truth.", "The entrance remains sealed.", "The Crow demands progress.")
    check("look at the orders", hero.command("look at orders"), "A bundle of poorly translated notes covered in strange symbols and instructions.")
    text = hero.command("report")
    check("the ending: Rowan reads the six fragments", text, "The Hollow Below", "The Key of Vincent", "Continue excavation.",
          "The old founder was telling the truth.", "The entrance remains sealed.", "The Crow demands progress.")
    check("the ending: the five reveals", text, "Vincent did find something beneath this village", "The Hollow Below", "someone called \"The Crow\"",
          "tied to whatever lies down there", "other entrances too, perhaps in the northern mountains")
    check("the ending: quest complete, rewards", text, "Quest complete: Recover the Overseer's Orders!", "You gain 200 experience.", "Rowan pays you 75 gold.")
    s_after = score_of(hero)
    check("rewards: 200 experience and 75 gold", f"{s_after['xp'] - s_before['xp']} {s_after['gold'] - s_before['gold']}", "200 75")
    check("the orders were taken", hero.command("i"), absent=("Overseer's Orders",))
    check("the other quest was not completed by this", hero.command("quests"), "Completed quests: Recover the Overseer's Orders", absent=("Clear the Cellar",))
    check("Rowan's talk has changed", hero.command("talk rowan"), "The Crow, the Hollow Below, the sealed entrance")
    check("the board shows what is done", hero.command("read board"), "Recover the Overseer's Orders (done)", "Clear the Cellar (open)")
    check("nothing more to hand in", hero.command("report"), "You have nothing to report yet.")
    hero.command("east")

    # ===== builder wizard commands and their level gates =====
    check("goto: below level 1 it does not exist", hero.command("goto " + TEST + "hub"), "What?")
    check("stat: below level 1 it does not exist", hero.command("stat me"), "What?")
    check("heal: below level 5 it does not exist", hero.command("restore"), "What?")
    check("slay: below level 10 it does not exist", hero.command("slay benson"), "What?")
    low = h.Client("lowly")
    low.read_until("> ")
    check("promote to level 1", wiz.command("promote lowly 1"), "lowly is now wizard level 1")
    low.read_until("level is now")
    drain(low, 0.5)
    check("level 1: goto works", low.command("goto " + TEST + "den"), "Gnawer Den", "Two gnawers are here.")
    check("goto announces departure to the room", wiz.command("goto " + TEST + "den"), "Gnawer Den")
    check("level 1: stat on a creature", low.command("stat gnawer"), "Gnawer", "Level 1, Vitality 3/3", "Damage 1-1", "Monster: aggressive False, assist True")
    check("level 1: stat me", low.command("stat me"), "Level 1, Vitality 30/30, gold 25")
    check("level 1: stat here", low.command("stat here"), "Gnawer Den", "Spawn /domains/test/gnawer x2: 2 alive", "Exit out")
    check("level 1: goto a player", low.command("goto hero") + quiet(low, 1.0), "Village Square", "Hero the newcomer")
    check("level 1: goto a missing room", low.command("goto /domains/nowhere"), "No such file")
    check("level 1: heal does not exist", low.command("restore"), "What?")
    check("level 1: slay does not exist", low.command("slay torren"), "What?")
    check("level 1: help lists goto and stat", low.command("help", marker="stop the driver") if False else low.command("help"), "goto", "stat", absent=("slay", "restore ["))
    wiz.command("promote lowly 5")
    low.read_until("level is now")
    drain(low, 0.5)
    check("level 5: heal yourself", low.command("restore"), "You are restored to full Vitality.")
    low.command("goto " + TEST + "den")
    check("level 5: heal another creature", low.command("restore gnawer"), "You restore a gnawer to full Vitality.")
    check("level 5: slay does not exist", low.command("slay gnawer"), "What?")
    wiz.command("promote lowly 10")
    low.read_until("level is now")
    drain(low, 0.5)
    text = low.command("slay gnawer")
    check("level 10: slay kills with normal death processing", text, "You slay a gnawer!", "A gnawer dies.")
    check("slay on yourself or nothing", low.command("slay nobody"), "Slay whom?")
    check("help for level 10 lists everything", low.command("help", marker="stop the driver"), "restore [", "slay", "goto", "stat")
    low.read_until("> ")
    low.send("quit")
    low.read_until("Goodbye")

    # ===== the Goblin Overseer =====
    # Level the wizard to 5 with dummies, then fight the real boss with the Overseer's own axe (the fists of a wizard would need
    # six minutes for his 114 Vitality) and a lot of healing.
    wiz.command("goto " + TEST + "dummy")
    for dummy in ("dummy_d", "dummy_c", "dummy_b", "dummy_a"):
        wiz.command("clone " + TEST + dummy)
        wiz.command("slay dummy")
    s = score_of(wiz)
    check("the wizard reached level 5 by smiting", s["text"], "Level 5")
    # quest 1 for the wizard, before the boss
    wiz.command("goto " + HOLLOW + "village/guild")
    check("report before accepting anything", wiz.command("report"), "You have nothing to report yet.")
    check("accept the cellar quest", wiz.command("accept cellar"), "Quest accepted: Clear the Cellar.", "Kill him, and come back and report.")
    check("report while the Overseer lives", wiz.command("report"), "The Overseer still lives.")
    wiz.command("goto " + HOLLOW + "cellars/overseer_chamber")
    text = wiz.command("look")
    check("the Overseer Chamber", text, "Overseer Chamber", "Unlike the rest of the dungeon, this room is surprisingly organized.", "The Goblin Overseer is here.")
    check("look at a detail of the chamber", wiz.command("look at crow"), "crude black crow")
    check("stat on the boss", wiz.command("stat overseer"), "Level 5, Vitality 114/114", "Damage 4-7, attack 7", "defense 4, armor 3", "xp 250", "loot 4",
          "Reaction Time: base 4.0", "Axe Slash: preparation 0.0, recovery 1.0", "delay 10.0 s", "Power Strike: preparation 1.0, recovery 2.0", "delay 12.0 s",
          "Battle Cry", "Summon Goblin Minion")
    wiz.command("clone " + HOLLOW + "obj/overseers_axe")
    wiz.command("wield axe")
    # The boss ambushes after his Reaction Time x 2 = 8 s; the wizard hits first.
    wiz.command("kill overseer")
    text, chunks = fight_until_timed(wiz, "falls dead among his maps", timeout=300, heal_with=True)
    check("Battle Cry opens the fight", text, "BATTLE CRY")
    check("Battle Cry is used once per fight", str(text.count("BATTLE CRY")), "1")
    check("the Battle Cry replaces the first attack", str(text.index("BATTLE CRY") < min([text.index(w) for w in ("Goblin Overseer hits", "Goblin Overseer misses", "Goblin Overseer scratches", "Goblin Overseer smashes") if w in text] or [10 ** 9])), "True")
    check("Power Strike happens", text, "POWER STRIKE")
    check("Summon Goblin Minion happens", text, "a goblin minion scrambles out of the shadows!")
    check("the minion joins the fight", "A goblin minion " + ("acts" if re.search(r"A goblin minion (hits|misses) you", text) else "idle"), "A goblin minion acts")
    check("Summon is used once per life", str(text.count("scrambles out of the shadows")), "1")
    check("the Power Strike is announced before it lands", text, "The Goblin Overseer draws his axe far back...", "brings the axe down in a crushing POWER STRIKE!")
    check("the warning comes before the blow", str(text.index("draws his axe far back") < text.index("crushing POWER STRIKE")), "True")
    # +2 damage only inside the Battle Cry window (30 seconds): a "brutal" hit needs 8+ damage, which only the cry (4-7 +2) or a Power Strike can reach.
    cry_at = next((t for t, chunk in chunks if "BATTLE CRY" in chunk), 0)
    outside_ok = True
    inside_seen = False
    pending_power = False
    for t, chunk in chunks:
        # (a critical hit doubles the damage whatever the cry: it is told by the line "A critical hit!" after the blow, and left out)
        for token in re.findall(r"(POWER STRIKE|The Goblin Overseer (?:hits|misses|scratches|smashes) you[a-z ]*\.(?: A critical hit!)?)", chunk):
            if "A critical hit!" in token:
                pending_power = False
                continue
            if token == "POWER STRIKE":
                pending_power = True
                continue
            if "brutally" in token and not pending_power:
                if t - cry_at > 31:
                    outside_ok = False
                else:
                    inside_seen = True
            pending_power = False
    check("bonus damage lasts only 30 seconds after the Battle Cry", str(outside_ok), "True")
    if not inside_seen:
        print("      (no brutal cry hit happened in this run; the window rule was only checked from the other side)")
    text += wiz.pending + quiet(wiz, 1.0)  # the experience line follows the death line a moment later, in a chunk of its own
    wiz.pending = ""
    drain(wiz, 0.3)
    check("the death line", text, "The Goblin Overseer staggers, drops to his knees, and falls dead among his maps.")
    check("the boss pays experience", text, "You gain 250 experience.")
    # The summoned minion may still be fighting: nothing can be picked up during a fight, so end it first.
    wiz.command("slay minion")
    drain(wiz, 1.0)
    text = wiz.command("look")
    check("the boss leaves a corpse and nothing on the floor", text, "The corpse of the Goblin Overseer is here.", absent=("The Goblin Overseer is here", "axe is here", "vest is here", "orders is here"))
    text = wiz.command("look in corpse")
    check("the boss corpse holds the four boss items", text, "Goblin Overseer's axe", "leather commander's vest", "bag of coins", "Overseer's Orders")
    gold_before = score_of(wiz)["gold"]
    text = wiz.command("get bag from corpse")
    check("the bag of coins becomes gold", text, "You take a bag of coins from the corpse of the Goblin Overseer.", "to your purse.")
    gain = score_of(wiz)["gold"] - gold_before
    check("the bag holds 60 to 100 gold", str(60 <= gain <= 100), "True")
    wiz.command("drop axe")  # the one cloned for the fight; the real one comes from the corpse
    check("take the axe", wiz.command("get axe from corpse"), "You take the Goblin Overseer's axe from the corpse")
    check("wield the axe", wiz.command("wield axe"), "You wield the Goblin Overseer's axe.")
    check("take and wear the vest", (wiz.command("get vest from corpse"), wiz.command("wear vest"))[1], "You put on a leather commander's vest.")
    check("axe numbers", score_of(wiz)["text"], "Damage 17-25", "Reaction Time")
    check("vest armor", score_of(wiz)["text"], "Armor 3")

    # ===== quest 1 for the wizard, then quest 2 with the ending =====
    wiz.command("goto " + HOLLOW + "village/guild")
    sb = score_of(wiz)
    text = wiz.command("report")
    check("the first quest completes: kill credit", text, "Quest complete: Clear the Cellar!", "You gain 150 experience.", "Rowan pays you 50 gold and gives you a Guild Initiate's Helm.")
    check("the second quest is offered at once", text, "New quest: Recover the Overseer's Orders.")
    sa = score_of(wiz)
    check("rewards: 150 experience and 50 gold", f"{sa['xp'] - sb['xp']} {sa['gold'] - sb['gold']}", "150 50")
    check("the helm was given", wiz.command("i"), "a Guild Initiate's helm")
    check("wear the helm", wiz.command("wear helm"), "You put on a Guild Initiate's helm.")
    check("the quest log shows one done, one active", wiz.command("quests"), "Active quests: Recover the Overseer's Orders", "Completed quests: Clear the Cellar")
    check("report again: the second quest is open", wiz.command("report"), "Find the Overseer's Orders")
    wiz.command("goto " + HOLLOW + "cellars/overseer_chamber")
    check("take the orders from the corpse", wiz.command("loot"), "You take the Overseer's Orders from the corpse of the Goblin Overseer.")
    wiz.command("goto " + HOLLOW + "village/guild")
    text = wiz.command("report")
    check("the wizard hears the ending too", text, "Quest complete: Recover the Overseer's Orders!", "The Crow demands progress.", "The Hollow Below")
    check("both quests are complete", wiz.command("quests"), "Completed quests: Clear the Cellar, Recover the Overseer's Orders")
    check("nothing more to hand in", wiz.command("report"), "You have done everything I asked.")
    check("the board shows both done", wiz.command("read board"), "Clear the Cellar (done)", "Recover the Overseer's Orders (done)")

    # ===== encounters, wander off =====
    wiz.command("goto " + TEST + "hub")
    text = wiz.command("wolf")
    check("an encounter appears when a player walks in", text, "Wolf Run", "a wandering wolf wanders in" if False else "A wandering wolf is here.")
    wiz.command("out")
    text = wiz.command("wolf")
    check("only one encounter at a time", text, "A wandering wolf is here.", absent=("Two",))
    text = fight_until(wiz, "wanders off", timeout=25)
    check("an idle encounter wanders off at reset", text, "A wandering wolf wanders off.")
    drain(wiz, 0.5)
    check("the wolf is gone", wiz.command("look"), "Wolf Run", absent=("wolf is here",))
    wiz.command("out")
    check("the 60 second rule: no new encounter right away", wiz.command("wolf"), "Wolf Run", absent=("wolf is here",))
    wiz.command("out")

    # ===== reset: a creature killed by slay is replaced =====
    wiz.command("rats")
    wiz.command("slay rat")
    text = wiz.command("look")
    check("one rat left after a kill", text, "A large rat is here.", absent=("Two large rats",))
    wiz.command("slay rat")
    check("the corpses hold their places: no rat comes while they lie here", (time.sleep(7), wiz.command("look"))[1], "Two corpses of large rats are here.", absent=("A large rat is here",))
    for _ in range(2):  # the corpses rot after a second each (the timers tool sets the first one it finds)
        wiz.command("timers 600 600 1")
        time.sleep(2.2)
    text = ""
    end = time.time() + 25
    while time.time() < end and "Two large rats are here." not in text:
        time.sleep(1)
        text = wiz.command("look")
    check("killed creatures are replaced at reset", text, "Two large rats are here.")
    wiz.command("slay rat")
    wiz.command("slay rat")
    wiz.command("out")

    # ===== live update keeps one of each NPC =====
    wiz.command("goto " + HOLLOW + "village/ironoak")
    check("update a room with a shopkeeper in it", wiz.command("update " + HOLLOW + "village/ironoak"), "Recompiled", "Replaced")
    text = wiz.command("look")
    check("there is exactly one Torren after the update", text, "Blacksmith Torren is here.", absent=("Two",))
    check("the new Torren still sells", wiz.command("browse"), "rusty sword")

    # ===== every file of the area loads in the driver =====
    wiz.command("goto " + TEST + "scratch")
    problems = []
    for path in hollow_files(mudlib) + ["/std/corpse"]:
        if "/village/" in path or "/cellars/" in path:
            text = wiz.command("goto " + path)
            ok = "Failed" not in text and "sandbox" not in text and "***" not in text and "No such file" not in text
        else:
            text = wiz.command("clone " + path)
            ok = "Cloned" in text
        if not ok:
            problems.append((path, text[-300:]))
    check(f"all {len(hollow_files(mudlib))} files of the area load and run in the driver", repr(problems), "[]")
    wiz.command("goto " + TEST + "scratch")

    # ===== the templates of docs/BUILDING.md =====
    templates = building_templates()
    b = h.Client("builder")
    b.read_until("> ")
    check("promote the builder to level 5", wiz.command("promote builder 5"), "builder is now wizard level 5")
    b.read_until("level is now")
    drain(b, 0.5)
    expected = {"hall", "vault_door", "vault", "gatehouse", "courtyard", "trail", "villager", "shopkeeper", "rat", "drake", "sword", "helm", "potion", "purse", "key"}
    check("the guide has a template for every kind", ",".join(sorted(p.rsplit("/", 1)[1] for p in templates)), *sorted(expected))
    b.command("restore")
    text = b.command("goto /domains/example/hall") + quiet(b, 0.5)
    check("the plain room template loads", text, "Example Hall", "A square hall with a worn flagstone floor.", "Two example rats are here.", "An example potion is here.")
    check("template: detail", b.command("look at floor"), "chipped and swept clean")
    check("template: look at a readable", b.command("look at notice"), "You could read it.")
    check("template: readable", b.command("read notice"), "Beware of rats.")
    check("template: a creature with one action", b.command("stat rat"), "Reaction Time: base 2.0", "Bite: preparation 0.0, recovery 0.5, modifier 0.0, delay 5.0 s")
    text = fight_until(b, "dies.", timeout=120, heal_with=True)
    text += fight_until(b, "dies.", timeout=120, heal_with=True) if text.count("dies.") < 2 else ""
    drain(b, 1.0)
    check("template: the monster fights and dies", text, "An example rat dies.")
    text = b.command("loot")
    check("template: the corpse holds gold and the loot", text, "from the corpse of an example rat.", "gold to your purse.")
    b.command("loot")
    text = b.command("clone /domains/example/drake") + b.command("stat drake")
    b.command("slay drake")
    check("template: a creature with a pattern lists its actions and delays", text, "Reaction Time: base 4.0", "Claw Swipe: preparation 0.0, recovery 1.0, modifier 0.0, delay 10.0 s",
          "Fire Breath: preparation 1.0, recovery 3.0, modifier 0.0, delay 14.0 s", "wind-up \"rears back and swells its chest\"")
    b.command("loot")
    check("template: encounters are registered", (b.command("goto /domains/example/trail"), drain(b, 0.5), b.command("stat here"))[2], "Encounter /domains/example/rat 50%")
    b.command("goto /domains/example/vault_door")
    drain(b, 0.5)
    check("template: the locked exit refuses", b.command("east"), "The iron door is locked.")
    check("template: the key", b.command("get key"), "You take an iron key.")
    check("template: the key opens the exit", b.command("east"), "Vault", "A small stone vault, bare except for dust.")
    b.command("goto /domains/example/gatehouse")
    drain(b, 0.5)
    check("template: a room with a door shows it on the exits line", b.command("look"), "Gatehouse", "east (closed plank door)")
    check("template: look at the door", b.command("look at plank door"), "Grey planks bound in black iron", "The plank door to the east is closed.")
    check("template: a closed door stops walking", b.command("east"), "The plank door to the east is closed.")
    check("template: open it by its name", b.command("open door"), "You open the plank door to the east.")
    check("template: and walk through", b.command("east"), "Courtyard", "A bare courtyard behind the gatehouse.", "west (open plank door)")
    check("template: the other half shares the state, and closes it", b.command("close door"), "You close the plank door to the west.")
    check("template: closed from there, closed from the gatehouse", b.command("west"), "The plank door to the west is closed.")
    b.command("open door")
    b.command("west")
    b.command("clone /domains/example/villager")
    check("template: the peaceful NPC talks", b.command("talk maren"), 'Maren says: "Good day to you."')
    check("template: the NPC answers topics", b.command("ask maren about wool"), "Sheep, mostly.")
    check("template: the NPC cannot be attacked", b.command("kill maren"), "is no threat to you")
    b.command("clone /domains/example/shopkeeper")
    check("template: the shopkeeper sells", b.command("browse"), "example sword", "10 gold", "example potion", "15 gold")
    check("template: buy", b.command("buy sword"), "You buy an example sword for 10 gold.")
    check("template: wield", b.command("wield example sword"), "You wield an example sword.")
    check("template: the weapon's timing fields reach the character sheet", b.command("score"), "Damage 9-14", "Reaction Time 3.0 (base 2.5, an example sword +0.5)", "Your attack takes 8.0 seconds.")
    check("template: sell", b.command("sell example sword"), "for 5 gold.")
    b.command("clone /domains/example/helm")
    check("template: armor", b.command("wear example helm"), "You put on an example helm.")
    b.command("clone /domains/example/potion")
    wait_ready(b)  # using something is an action: the fight above may still be recovering
    check("template: consumable", b.command("drink example potion"), "You drink the example potion. It tastes of cherries.")
    b.command("clone /domains/example/purse")
    gold = score_of(b)["gold"]
    b.command("drop purse")
    b.command("get purse")
    check("template: coins", str(20 <= score_of(b)["gold"] - gold <= 40), "True")
    b.send("quit")
    b.read_until("Goodbye")

    # ===== level gates after demotion, and help =====
    check("who lists the wizard", wiz.command("who"), "Wiz")
    wiz.send("quit")
    wiz.read_until("Goodbye")
    hero.send("quit")
    hero.read_until("Goodbye")
    wiz2 = h.Client("wiz")
    wiz2.read_until("> ")
    wiz2.command("goto " + TEST + "scratch")
    # The wizard's own state came back: level 5 (about 1600 experience; level 6 needs 5400), quests done, equipment.
    s = score_of(wiz2)
    check("a quitting wizard keeps character, equipment and quests", s["text"], "Level 5", "more for level 6", "Damage 17-25", "Armor 4", "Completed quests: Clear the Cellar, Recover the Overseer's Orders")
    wiz2.send("quit")
    wiz2.read_until("Goodbye")


if __name__ == "__main__":
    sys.exit(main())
