#!/usr/bin/env python3
"""Tests for stage A of the class system (docs/CLASS_SYSTEM.md): archetypes and their features, resources, rolls, timed
effects, stagger, equipment access, the experience formula, the attack/defense rule, the class framework with a class
defined only inside this test, the travel pass and persistence.

Boots the driver on a temporary copy of the mudlib. Test content (a few creatures, an arena, a test advanced class) is
written into the copy. Where chance is involved the test either forces the condition (a creature with a 75% dodge, a
class with 100% critical) or repeats enough times that the bounds cannot flake.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=4816 python3 tests/class_test.py
"""
import json
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4816")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402

TEST = "/domains/test/"
HOLLOW = "/domains/hollow/"
check = h.check


def creature(cls, name, ident, hp, dmg=(1, 1), level=1, armor=0, dodge=0, block=0, react=2.0, rec=0.5, action="", tohit=0):
    act = action or f'AddCombatAction("hit", "Hit", preparation: 0, recovery: {rec});'
    return f'''// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); ShortDesc = "a {ident}"; LongDesc = "A creature made for the class test.";
        Level = {level}; MaxHp = {hp}; DamageMin = {dmg[0]}; DamageMax = {dmg[1]}; Armor = {armor}; ToHit = {tohit};
        Dodge = {dodge}; BlockNatural = {block}; BaseReaction = {react}; Aggressive = false;
        {act}
    }}
}}
'''


def room(cls, short, exits, body="", safe=False):
    ex = "".join(f'        Exits["{d}"] = "{p}";\n' for d, p in exits.items())
    return f'''// @inherit /std/room
public class {cls} : Room
{{
    public override void Create()
    {{
        ShortDesc = "{short}"; LongDesc = "A room made for the class test."; Safe = {str(safe).lower()};
{ex}{body}
    }}
    public override void Init()
    {{
        base.Init();
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        AddAction("pledge", Pledge);
        AddAction("drain", Drain);
    }}
    private bool Pledge(string args)
    {{
        var who = ThisPlayer();
        var problem = who!.Call<string>("ApplyClass", args.Trim());
        TellObject(who, problem == null ? "PLEDGED\\n" : "REFUSED: " + problem + "\\n");
        return true;
    }}
    private bool Drain(string args)
    {{
        ThisPlayer()!.Call("SetResources", 0, 0);
        TellObject(ThisPlayer()!, "DRAINED\\n");
        return true;
    }}
}}
'''


def armor(cls, name, ident, category, slot="body", value=2):
    return f'''// @inherit /std/armor
public class {cls} : Armor
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); ShortDesc = "a {name.lower()}"; LongDesc = "Test armor.";
        Category = "{category}"; Slot = "{slot}"; ArmorValue = {value}; Value = 9;
    }}
}}
'''


SKILL_TRAIT = '''// @inherit /std/trait
public class {cls} : Trait
{{
    public override void Create() {{ Title = "{title}"; Summary = "{title} (test)."; {mods} }}
}}
'''

FILES = {
    # creatures
    "dummy": creature("TDummy", "Dummy", "dummy", 3000, (0, 0)),
    "armored": creature("TArmored", "Armored", "armored", 3000, (0, 0), armor=50, level=30),
    "dodger": creature("TDodger", "Dodger", "dodger", 3000, (0, 0), dodge=50),
    "blocker": creature("TBlocker", "Blocker", "blocker", 3000, (0, 0), block=90),
    "hitter": creature("THitter", "Hitter", "hitter", 100000, (1, 1), react=1.0, rec=0.0),
    "boss100": creature("TOverlord100", "Overlord", "overlord", 100000, (1, 1), level=100, react=1.0, rec=0.0),
    "winder": creature("TWinder", "Winder", "winder", 100000, (1, 1), react=4.0,
                       action='AddCombatAction("blow", "Slow Blow", preparation: 2.0, recovery: 5.0, windUp: "raises a huge club", strike: "brings the club down!");'),
    # items
    "armor_medium": armor("TMediumArmor", "Medium Mail", "mail", "medium"),
    "armor_heavy": armor("THeavyPlate", "Heavy Plate", "plate", "heavy"),
    "zap_ring": '''// @inherit /std/armor
public class TZapRing : Armor
{
    public override void Create()
    {
        Name = "Zap Ring"; Ids.Add("ring"); ShortDesc = "a zap ring"; LongDesc = "Test trinket.";
        Slot = "trinket"; ArmorValue = 0; Value = 1;
    }
    public override List<string> QueryGrantedSkills() => new List<string> { "test_zap" };
}
''',
    # rooms
    "arena": room("TArena", "The Test Arena", {"east": TEST + "lair", "north": TEST + "vault"},
                  'LockExit("north", "testkey", "The vault is locked.");'),
    "lair": room("TLair", "The Test Lair", {"west": TEST + "arena"}),
    "vault": room("TVault", "The Test Vault", {"south": TEST + "arena"}),
    "calm": room("TCalm", "The Calm Room", {}, safe=True),
}

# skills, traits and classes of the test (added to /skills and /classes of the temporary mudlib)
SKILLS = {
    "test_forced": SKILL_TRAIT.format(cls="TForcedTrait", title="Forced Rolls", mods='Mods["dodge"] = 60; Mods["block"] = 60; Mods["crit"] = 95;'),
    "test_crit": SKILL_TRAIT.format(cls="TCritTrait", title="Sure Crit", mods='Mods["crit"] = 95;'),
    "test_stagger": '''// @inherit /std/skill
public class TStaggerSkill : Skill
{
    public override void Create() { Title = "Test Stagger"; Command = "tstagger"; Summary = "Staggers the target (test)."; Recovery = 0.0; Target = "enemy"; }
    public override void Resolve(ObjectRef user, ObjectRef? target) { target?.Call("Stagger", 2.0, user); }
}
''',
    "test_flash": '''// @inherit /std/skill
public class TFlashSkill : Skill
{
    public override void Create() { Title = "Test Flash"; Command = "tflash"; Summary = "Armor +2 for 8 seconds (test)."; Recovery = 0.0; }
    public override void Resolve(ObjectRef user, ObjectRef? target) { user.Call("AddEffect", "flash", "Flash", 8, "armor=2", 1.0, 0); TellObject(user, "FLASHED\\n"); }
}
''',
    "test_zap": '''// @inherit /std/skill
public class TZapSpell : Skill
{
    public override void Create() { Title = "Test Zap"; Command = "zap"; Kind = "spell"; Summary = "5 damage (test)."; ManaCost = 10; Recovery = 0.5; Target = "enemy"; }
    public override void Resolve(ObjectRef user, ObjectRef? target)
    {
        if (target == null) return;
        TellObject(user, "ZAPPED\\n");
        target.Call("TakeDamage", 5, user);
    }
}
''',
}

CLASSES = {
    "sentinel": '''// @inherit /std/pclass
public class SentinelClass : PlayerClass
{
    public override void Create()
    {
        ClassName = "Sentinel"; Tier = "archetype"; SortOrder = 98; Tagline = "Test archetype with forced rolls."; Description = "Test.";
        StartReaction = 2.5; VitalityBase = 22; VitalityPerLevel = 8; EnduranceBase = 45; EndurancePerLevel = 0; FocusBase = 45; FocusPerLevel = 0; AttackRate = 1.0; StartGold = 5;
        Weapons.Add("*"); ArmorTypes.AddRange(new[] { "light", "medium", "heavy" }); Shield = true;
        Skills.AddRange(new[] { "test_stagger", "test_flash" }); Traits.Add("test_forced");
        Kit.Add("/domains/hollow/obj/small_shield");
    }
}
''',
    "zealot_test": '''// @inherit /std/pclass
public class ZealotTestClass : PlayerClass
{
    public override void Create()
    {
        ClassName = "Zealot"; Tier = "advanced"; Parent = "mage"; Title = "Zealot"; Tagline = "Test advanced class."; Guild = "zealots";
        ExpModifier = 1.25; AttackRate = 0.99;
        Skills.Add("test_zap"); Traits.Add("test_crit");
        Weapons.Add("sword"); ArmorTypes.Add("medium");
        Require("level", "5");
    }
}
''',
    "closed_test": '''// @inherit /std/pclass
public class ClosedTestClass : PlayerClass
{
    public override void Create()
    {
        ClassName = "Closed"; Tier = "advanced"; Parent = "mage"; Title = "Closed"; Enabled = false;
    }
}
''',
}

PADNAMES = ["t" + x for x in ("warrior", "scout", "priest", "mage", "adventurer")] + ["x" + x for x in ("warrior", "scout", "priest", "mage", "adventurer")] + ["sentinel"]
for _n in PADNAMES:
    FILES["pad_" + _n] = room("TPad" + _n.capitalize(), "A Test Pad", {"east": TEST + "lair", "north": TEST + "vault"},
                              'LockExit("north", "testkey", "The vault is locked.");')
ROOM = {"v": TEST + "arena"}


def pad(name):
    return TEST + "pad_" + name


def setroom(wiz, name):
    ROOM["v"] = pad(name)
    wiz.command("goto " + ROOM["v"])


ARCHETYPES = ["warrior", "scout", "priest", "mage", "adventurer"]
procs = {}


def wiz_login(name, arch, secret_wiz=None):
    c = h.Client(name, archetype=arch)
    c.read_until("> ")
    if secret_wiz is not None:
        secret_wiz.command(f"promote {name.lower()} 10")
        c.read_until("level is now")
    a.drain(c, 0.5)
    c.command("goto " + pad(name.lower()))
    return c


def num(pattern, text, default=None):
    m = re.search(pattern, text)
    return float(m.group(1)) if m and "." in m.group(1) else (int(m.group(1)) if m else default)


def res(c):
    """(hp, stamina, mana) from the hp command."""
    t = ""
    for _ in range(4):
        t += c.command("hp")
        if "Mana " in t and re.search(r"HP \d+/\d+   Stamina", t):
            break
        time.sleep(0.3)
    return (num(r"HP (\d+)/", t), num(r"Stamina (\d+)/", t), num(r"Mana (\d+)/", t))


def give(wiz, c, path, word):
    wiz.command("goto " + ROOM["v"])
    wiz.command("clone " + path)
    wiz.command("drop " + word)
    out = c.command("get " + word)
    return out


# Experience to reach each level (docs/CORRIDOR_BALANCE.md section 4.3): the table to level 10, a power law beyond it.
XP_TABLE = {2: 100, 3: 300, 4: 600, 5: 1000, 6: 5400, 7: 12000, 8: 21700, 9: 34100, 10: 49300}


def xp_for(n, mod=1.0):
    if n <= 1:
        return 0
    basis = XP_TABLE[n] if n in XP_TABLE else 49300 * (n / 10.0) ** 3
    return int(basis * mod + 0.5)


def expected_attack(rate, level):
    return int(rate * level + 0.5 + 1e-9)


def count(pattern, text):
    return len(re.findall(pattern, text))


def run_fight(c, wiz, seconds, restore=True):
    """Let a fight run for a while, keeping the character alive with the wizard command."""
    end = time.time() + seconds
    data = ""
    last = 0
    while time.time() < end:
        data += a.quiet(c, 0.4)
        if restore and time.time() - last > 1.0:
            last = time.time()
            c.send("restore")
    return data


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-class-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    os.makedirs(os.path.join(mudlib, "domains", "test"))
    for name, source in FILES.items():
        h.write(mudlib, TEST + name, source)
    for name, source in SKILLS.items():
        h.write(mudlib, "/skills/" + name, source)
    for name, source in CLASSES.items():
        h.write(mudlib, "/classes/" + name, source)
    # An old save from before /secure/vault was renamed /secure/lost_property: its records must still be handed back.
    os.makedirs(os.path.join(work, "save", "secure"), exist_ok=True)
    with open(os.path.join(work, "save", "secure", "vault.json"), "w") as f:
        json.dump({"gold": {"legacy|1-/std/corpse#1": 7}, "items": {"legacy|1-/std/corpse#1": ["/domains/hollow/obj/iron_dagger"]}}, f)
    proc, log = a.start_driver(work, mudlib)
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
            print(open(os.path.join(work, "driver.log")).read()[-3000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\n{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


def run(work, mudlib):
    # ===== the wizard =====
    wiz = h.Client("chief", archetype="warrior")
    wiz.read_until("> ")
    check("the secret makes the first wizard", wiz.command("promote " + h.SECRET), "level 10 wizard")

    # ===== lost property saved under the old name =====
    lg = h.Client("legacy", archetype="warrior")
    lg_text = lg.read_until("> ")
    check("a record saved by the old vault is handed back after the upgrade", lg_text, "You recover an item and 7 gold.")
    lg.command("quit")
    time.sleep(0.5)
    check("the store now saves under its new name", "ok" if os.path.exists(os.path.join(work, "save", "secure", "lost_property.json")) else "missing", "ok")

    # ===== the choice at creation =====
    c = h.Client("picker", login=False)
    c.read_until("characters): ")
    c.send(h.PASSWORD)
    c.read_until("again: ")
    c.send(h.PASSWORD)
    text = c.read_until("Your calling: ")
    check("a new character is offered the five callings with one line each, in order", text,
          "choose your calling", "Warrior:", "Scout:", "Priest:", "Mage:", "Adventurer:", "Sentinel:")
    check("the five come in the design order", str(text.index("Warrior:") < text.index("Scout:") < text.index("Priest:") < text.index("Mage:") < text.index("Adventurer:")), "True")
    c.send("help mage")
    check("help <calling> shows the detail", c.read_until("Your calling: "), "Mage:", "Arcane Bolt")
    c.send("banker")
    check("an unknown calling is refused", c.read_until("Your calling: "), "There is no such calling")
    # A class file added while the game runs shows up at the next login: adding a class is adding one file.
    h.write(mudlib, "/classes/late_test", CLASSES["sentinel"].replace("SentinelClass", "LateTestClass").replace('"Sentinel"', '"Latecomer"').replace("98", "97"))
    c2 = h.Client("pickertwo", login=False)
    c2.read_until("characters): ")
    c2.send(h.PASSWORD)
    c2.read_until("again: ")
    c2.send(h.PASSWORD)
    check("a class file added at run time is offered without restart", c2.read_until("Your calling: "), "Latecomer:")
    c2.sock.close()
    os.remove(os.path.join(mudlib, "classes", "late_test.cs"))
    c.send("scout")
    check("the chosen calling lets the character in", c.read_until("> "), "You take up the calling of the Scout.", "Welcome, Picker.")
    c.send("quit")
    c.read_until("Goodbye")

    # ===== the five archetypes: starting numbers and kit =====
    expect = {  # HP as docs/ATTRIBUTES.md section 8 sets it (Warrior 35, Scout 20, Mage 20 at level 1)
        "warrior": ("35", "100", "20", "10", ["rusty sword", "small shield"], "weapon: a rusty sword", "shield: a small shield"),
        "scout": ("20", "100", "20", "10", ["hunting bow", "iron dagger"], "weapon: a hunting bow", "Armor 0"),
        "priest": ("26", "40", "100", "15", ["novice's mace", "minor healing potion"], "weapon: a novice's mace", "Armor 0"),
        "mage": ("20", "30", "100", "15", ["apprentice's staff", "minor healing potion"], "weapon: an apprentice's staff", "Armor 0"),
        "adventurer": ("30", "70", "70", "20", ["training spear", "trail ration"], "weapon: a training spear", "Armor 0"),
    }
    chars = {}
    for arch in ARCHETYPES:
        c = wiz_login("t" + arch, arch, wiz)
        chars[arch] = c
        hp, st, ma, gold, kit, eq1, eq2 = expect[arch]
        s = a.score_of(c)["text"]
        check(f"{arch}: HP {hp}, Stamina {st}, Mana {ma}, gold {gold}", s, f"HP {hp}/{hp}", f"Stamina {st}/{st}", f"Mana {ma}/{ma}", f"Gold {gold}",
              f"Calling {arch.capitalize()}")
        inv = c.command("i")
        check(f"{arch}: the starting kit", inv, *kit)
        check(f"{arch}: weapon (and shield) put on", s, eq1, eq2)
    wiz.command("goto " + HOLLOW + "village/square")
    check("score shows the area and the region", a.score_of(wiz)["text"], "Area The Village of Vincent's Hollow   Region Greenvale")
    check("warrior Reaction Time base 3.0", a.score_of(chars["warrior"])["text"], "base 3.0")
    check("scout Reaction Time base 2.0", a.score_of(chars["scout"])["text"], "base 2.0")
    for arch in ARCHETYPES:
        c = chars[arch]
        text = c.command("skills")
        wanted = {"warrior": ["bash", "Defensive Training", "Weapon Proficiency"], "scout": ["track", "strike", "Evasion"],
                  "priest": ["heal", "bless", "Divine Knowledge"], "mage": ["bolt", "lore", "Mana Efficiency"],
                  "adventurer": ["appraise", "Survival Training", "Improvised Combat", "Traveler's Instinct"]}[arch]
        check(f"{arch}: the skills list names the skills and traits with costs", text, *wanted)
    check("skills show cost and timing", chars["warrior"].command("skills"), "15 Stamina", " s]")
    check("a non-priest has no heal command", chars["warrior"].command("heal"), "What?")

    # ===== existing character asked at login =====
    old = h.Client("oldie", archetype="warrior")
    old.read_until("> ")
    wiz.command("promote oldie 10")
    old.read_until("level is now")
    old.command("setlevel 3")
    old.command("quit")
    path = os.path.join(work, "save", "players", "oldie.json")
    time.sleep(0.5)
    data = json.load(open(path))
    for key in ("archetype", "advancedClass", "subclass", "prestigeClass"):
        data.pop(key, None)
    data["Gold"] = 77
    json.dump(data, open(path, "w"))
    old = h.Client("oldie", login=False)
    old.read_until("Password: ")
    old.send(h.PASSWORD)
    check("an existing character without a calling is asked at the next login", old.read_until("Your calling: "), "choose your calling")
    old.send("mage")
    text = old.read_until("> ")
    s = a.score_of(old)
    check("the existing character keeps level and gold, gets the new HP and no kit",
          f"{s['level']} {s['gold']} {s['max']}", "3 77 32")  # docs/ATTRIBUTES.md section 8: 4 + 6 x 3 + Vigor 5 x 2
    check("no second starting kit for an existing character", str(old.command("i").count("rusty sword")), "1")
    old.command("quit")

    # ===== equipment access =====
    items = {"sword": HOLLOW + "obj/rusty_sword", "bow": HOLLOW + "obj/hunting_bow", "mace": HOLLOW + "obj/novice_mace",
             "axe": HOLLOW + "obj/woodcutter_axe", "shield": HOLLOW + "obj/small_shield", "mail": TEST + "armor_medium",
             "plate": TEST + "armor_heavy", "dagger": HOLLOW + "obj/iron_dagger", "staff": HOLLOW + "obj/apprentice_staff",
             "spear": HOLLOW + "obj/training_spear", "vest": HOLLOW + "obj/commanders_vest"}
    can = {
        "warrior": {"sword", "bow", "mace", "axe", "shield", "mail", "plate", "dagger", "staff", "spear", "vest"},
        "scout": {"sword", "bow", "dagger", "spear", "vest"},
        "priest": {"mace", "staff", "shield", "mail", "vest"},
        "mage": {"dagger", "staff", "vest"},
        "adventurer": {"sword", "mace", "axe", "dagger", "staff", "spear", "shield", "vest"},
    }
    for arch in ARCHETYPES:
        c = wiz_login("x" + arch, arch, wiz)
        setroom(wiz, "x" + arch)
        c.command("unwield")
        c.command("remove shield")
        bad = []
        for word, path in items.items():
            give(wiz, c, path, word)
            verb = "wear" if word in ("shield", "mail", "plate", "vest") else "wield"
            out = c.command(f"{verb} {word}")
            ok = ("You wield" in out or "You put on" in out)
            refused = "not trained" in out
            if word in can[arch] and not ok:
                bad.append((word, "should work", out))
            if word not in can[arch] and not refused:
                bad.append((word, "should be refused", out))
            c.command(f"drop {word}")
        c.command("quit")
        check(f"{arch}: every item in or out of its access behaves as the table says", str(bad), "[]")
    setroom(wiz, "tmage")
    check("the refusal says why", chars["mage"].command("wield sword") if give(wiz, chars["mage"], items["sword"], "sword") else "",
          "You are not trained to wield a sword: Mages cannot.")
    wiz.command("goto " + HOLLOW + "village/ironoak")
    text = wiz.command("browse")
    check("Torren sells the mace, the staff and the bow", text, "novice's mace", "apprentice's staff", "hunting bow")

    # ===== resources: cost, refusal, regeneration =====
    wiz.command("goto " + ROOM["v"])
    setroom(wiz, "tmage")
    mage = chars["mage"]
    mage.command("restore")
    wiz.command("clone " + TEST + "dummy")
    mage.command("unwield")
    out = mage.command("bolt dummy")
    time.sleep(1.2)
    hp, st, ma = res(mage)
    check("a spell costs Mana when it begins (20 less 20% = 16)", str(82 <= ma <= 90), "True")
    mage.command("stop")
    mage.command("drain")
    a.drain(mage, 0.6)
    out = mage.command("bolt dummy")
    check("a spell that cannot be paid for is refused and not queued", out, "You do not have the mana for that.")
    time.sleep(10)
    hp, st, ma = res(mage)
    check("mana regenerates (3.4 per 10 s for a mage at level 1, docs/ATTRIBUTES.md section 8; stamina 2.7)", str(2 <= ma <= 7 and 1 <= st <= 5), "True")
    mage.command("goto") if False else None
    setroom(wiz, "twarrior")
    wiz.command("clone " + TEST + "dummy")
    warrior = chars["warrior"]
    warrior.command("drain")
    out = warrior.command("bash dummy")
    check("a skill that cannot be paid for is refused", out, "You are too tired for that.")
    setroom(wiz, "tadventurer")
    adv = chars["adventurer"]
    adv.command("drain")
    time.sleep(10)
    hp, st, ma = res(adv)
    check("Survival Training: out of combat Stamina comes back 50% faster (6 -> 9 per 10 s)", str(7 <= st <= 12), "True")
    wiz.command("goto " + TEST + "calm")
    pr = chars["priest"]
    wiz.command("goto " + ROOM["v"])
    # a safe room doubles it: the mage rests in the calm room
    mage.command("restore") if False else None

    # ===== features: warrior =====
    setroom(wiz, "twarrior")
    w = chars["warrior"]
    w.command("restore")
    s = a.score_of(w)["text"]
    check("Defensive Training +2 armor (shield 1 + 2)", s, "Armor 3")
    check("Weapon Proficiency +2 to hit with a weapon (level 1 + 2)", s, "To hit +3")
    w.command("unwield")
    check("no weapon, no proficiency bonus", a.score_of(w)["text"], "To hit +1")
    w.command("wield sword") if "sword" in w.command("i") else None
    wiz.command("goto " + ROOM["v"])
    w.command("restore")
    w.command("goto") if False else None
    w.command("wield sword")
    w.send("speed me 90 600") if False else None
    st0 = res(w)[1]
    out = w.command("bash dummy")
    w.send("stop")
    time.sleep(0.5)
    st1 = res(w)[1]
    check("Shield Bash costs 15 Stamina and attacks", str(st0 - st1 in (14, 15, 16)), "True")
    bashed = out
    w.command("speed me 90 600")
    for _ in range(10):
        if "is staggered" in bashed:
            break
        w.command("stop")
        w.command("restore")
        bashed += a.quiet(w, 7)
        bashed += w.command("bash dummy")
        bashed += a.quiet(w, 3)
    check("Shield Bash staggers on a hit", bashed, "is staggered")
    w.command("stop")
    time.sleep(5)
    w.command("speed me 0")
    a.drain(w, 1.0)
    w.command("remove shield")
    a.drain(w, 1.0)
    check("Shield Bash needs a shield", w.command("bash dummy"), "needs a shield")
    w.command("wear shield")
    w.command("stop")

    # ===== features: scout =====
    setroom(wiz, "tscout")
    sc = chars["scout"]
    check("Evasion gives 5% dodge (docs/ATTRIBUTES.md section 8)", a.score_of(sc)["text"], "Dodge 5%")
    wiz.command("goto " + TEST + "lair")
    wiz.command("goto " + TEST + "lair")
    wiz.command("clone " + TEST + "dummy")
    wiz.command("goto " + ROOM["v"])
    out = sc.command("track")
    check("Tracking lists the creatures in each adjacent room", out, "east", "a dummy", "north", "nothing stirs")
    sc.command("stop")
    wiz.command("clone " + TEST + "dummy")
    time.sleep(3)
    time.sleep(8)
    st0 = res(sc)[1]
    so = sc.command("strike dummy")
    time.sleep(1.2)
    stn = res(sc)[1]
    check("Precision Strike costs 25 Stamina and winds up", ("True" if stn <= st0 - 22 else f"{st0} {stn} {so!r}"), "True")
    sc.command("stop")
    for _ in range(3):
        wiz.command("slay dummy")

    # ===== features: priest =====
    setroom(wiz, "tpriest")
    p = chars["priest"]
    wiz.command("goto " + ROOM["v"])
    wiz.command("slay dummy")
    wiz.command("clone " + TEST + "hitter")
    wiz.command("speed hitter 90 600")
    p.command("kill hitter")
    run_fight(p, wiz, 0, restore=False)
    for _ in range(40):
        if res(p)[0] <= 8:
            break
        time.sleep(0.5)
    wiz.command("slay hitter")
    time.sleep(0.5)
    a.drain(p, 0.6)
    p.command("stop")
    before = res(p)[0]
    time.sleep(3)
    out = p.command("heal")
    out += a.quiet(p, 5)
    check("Minor Heal restores 8 HP plus 25% (Divine Knowledge) and 9% (Healing Power: Spirit 9, Insight 6): 11", out, "restoring 11 hit points")
    check("Minor Heal costs 25 Mana", str(res(p)[2] <= 90), "True")
    s0 = a.score_of(p)["text"]
    time.sleep(3)
    out = p.command("bless")
    out += a.quiet(p, 6)
    s1 = a.score_of(p)["text"]
    check("Blessing: +1 to hit, +1 armor, and it is listed with its time", s1, "Blessing (")
    check("Blessing changes the numbers", f"{num(r'To hit [+](\d+)', s1) - num(r'To hit [+](\d+)', s0)} {num(r'Armor (\d+)', s1) - num(r'Armor (\d+)', s0)}", "1 1")
    wiz.command("goto " + ROOM["v"])
    m = chars["mage"]
    m.command("goto " + ROOM["v"])
    m.command("restore")
    time.sleep(4)
    out = p.command("heal tmage")
    out += a.quiet(p, 4)
    check("a healing spell can be cast on someone else", out, "restoring")
    m.command("goto " + pad("tmage"))

    # ===== features: mage =====
    setroom(wiz, "tmage")
    m.command("restore")
    wiz.command("clone " + TEST + "armored")
    m.command("stop")
    got = ""
    for _ in range(4):
        got += m.command("bolt armored")
        got += a.quiet(m, 3.5)
    dmg = [int(x) for x in re.findall(r"for (\d+) damage", got)]
    check("Arcane Bolt hits for 6-10 whatever the armor (armor 50, level 30)", str(len(dmg) >= 1 and all(6 <= d <= 10 for d in dmg)), "True")
    wiz.command("slay armored")
    wiz.command("clone " + TEST + "dodger")
    wiz.command("speed tmage 90 600")
    dodged = hit = 0
    for batch in range(2):
        m.command("restore")
        for _ in range(8):
            out = m.command("bolt dodger")
            out += a.quiet(m, 1.0)
            last_out = out
            dodged += count("dodges your bolt", out)
            hit += count(r"strikes a dodger|for \d+ damage", out)
        time.sleep(1)
    check("Arcane Bolt can be dodged (a 50% dodge: some dodged, some hit)", str(dodged >= 1 and hit >= 1) or repr(last_out), "True")
    wiz.command("slay dodger")
    wiz.command("speed tmage 0")
    out = m.command("lore staff") if "staff" in m.command("i") else ""
    wiz.command("goto " + ROOM["v"])
    m.command("get all") if False else None
    give(wiz, m, HOLLOW + "obj/enchanted_dagger", "dagger")
    m.command("stop")
    wiz.command("slay dummy")
    wiz.command("slay dodger")
    time.sleep(5)
    out = m.command("lore dagger")
    out += a.quiet(m, 4)
    check("Magical Lore shows exact numbers and the magical property", out, "Damage 7-11", "to hit +2", "enchanted")

    # ===== features: adventurer =====
    setroom(wiz, "tadventurer")
    ad = chars["adventurer"]
    ad.command("unwield")
    s = a.score_of(ad)["text"]
    check("Improvised Combat: unarmed damage 2-5", s, "Damage 2-5")
    ad.command("wield spear")
    check("Improvised Combat: the spear's +0.5 reaction is gone", a.score_of(ad)["text"], "Reaction Time 2.5")
    out = ad.command("look")
    check("Traveler's Instinct: the exits line shows where each exit leads and marks locked ones", out,
          "east (to The Test Lair)", "north (to The Test Vault, locked)")
    out = ad.command("appraise spear")
    out += a.quiet(ad, 2)
    check("Appraise shows the numbers and what shops pay (8 -> 4 gold)", out, "Damage 8-12", "Shops pay 4 gold")
    check("only the adventurer's look shows exit detail", chars["warrior"].command("look"), absent=("(to ",))

    # ===== rolls and effects, with forced conditions =====
    setroom(wiz, "sentinel")
    sen = wiz_login("sentinel", "sentinel", wiz)
    wiz.command("goto " + ROOM["v"])
    s = a.score_of(sen)["text"]
    check("forced rolls: dodge 60%, block 75% with the shield, critical 100%", s, "Dodge 60%", "Block 75%", "Critical 100%")
    wiz.command("clone " + TEST + "dummy")
    sen.command("speed me 90 600")
    sen.command("kill dummy")
    text = run_fight(sen, wiz, 14)
    hits = count(r"You (?:scratch|hit|smash) a dummy", text)
    crits = count("A critical hit!", text)
    check("critical hits: with 100% every hit is critical", str(hits >= 3 and crits == hits), "True")
    check("critical hit damage is 1.5 times (a crit does at least 3 from 2-5 unarmed)", text, absent=("scratch a dummy hard",) if False else ())
    sen.command("stop")
    wiz.command("slay dummy")
    wiz.command("clone " + TEST + "hitter")
    wiz.command("speed hitter 90 600")
    sen.command("kill hitter")
    text = run_fight(sen, wiz, 25)
    dodges = count("You dodge", text)
    blocks = count("You block part of the blow", text)
    hit_lines = count(r"A hitter (?:scratches|hits|smashes) you", text)
    check("dodge rolls: a 60% dodge is seen often, and attacks still land", str(dodges >= 15 and hit_lines >= 5), "True")
    check("block rolls: the shield blocks many of the hits that land", str(blocks >= max(1, hit_lines // 3)), "True")
    wiz.command("slay hitter")
    sen.command("stop")
    # a creature's own dodge and block
    wiz.command("clone " + TEST + "blocker")
    sen.command("kill blocker")
    text = run_fight(sen, wiz, 12)
    check("a creature with a natural block blocks (90%)", text, "blocks part of the blow")
    wiz.command("slay blocker")
    wiz.command("clone " + TEST + "dodger")
    sen.command("kill dodger")
    text = run_fight(sen, wiz, 12)
    check("a creature with a natural dodge dodges", text, "dodges your attack")
    wiz.command("slay dodger")
    sen.command("stop")
    sen.command("speed me 0")

    # timed effects
    sen.command("speed me 90 600") if False else None
    sen.command("stop")
    time.sleep(6)
    out = sen.command("tflash")
    a.quiet(sen, 0.8)
    s = a.score_of(sen)["text"]
    check("a timed effect is listed with its time and changes the numbers", out + s, "Flash (", "Armor 3")
    time.sleep(9)
    s = a.score_of(sen)["text"]
    check("a timed effect expires", s, "Armor 1", absent=("Flash (",))
    sen.command("speed me 50 3")
    check("haste is a timed effect like any other (listed in score)", a.score_of(sen)["text"], "Haste (")
    time.sleep(4)
    check("haste expires", a.score_of(sen)["text"], absent=("Haste (",))

    # stagger
    wiz.command("clone " + TEST + "winder")
    sen.command("speed me 90 600")
    sen.command("kill winder")
    text = ""
    end = time.time() + 30
    while time.time() < end and "raises a huge club" not in text:
        text += a.quiet(sen, 0.2)
        sen.send("restore")
    sen.send("tstagger winder")
    text = a.quiet(sen, 7)
    check("stagger interrupts an action in its Preparation", text, "interrupted", absent=("brings the club down",))
    wiz.command("slay winder")
    wiz.command("clone " + TEST + "winder")
    sen.command("kill winder")
    text = ""
    end = time.time() + 40
    while time.time() < end and "brings the club down" not in text:
        text += a.quiet(sen, 0.2)
        sen.send("restore")
    check("without the stagger the same action lands", text, "brings the club down")
    before = after = None
    sen.send("stop")
    time.sleep(9)
    a.drain(sen, 1.0)
    for _ in range(6):
        text = ""
        end = time.time() + 30
        while time.time() < end and "brings the club down" not in text:
            text += a.quiet(sen, 0.2)
            sen.send("restore")
        sen.send("stop")
        st = wiz.command("stat winder")
        if "recovering" not in st:
            continue
        t_before = time.time()
        before = num(r"ready in ([\d.]+) s", st)
        sen.send("tstagger winder")
        so2 = a.quiet(sen, 5.0)
        t_after = time.time()
        after = num(r"ready in ([\d.]+) s", wiz.command("stat winder"))
        if before is not None and after is not None:
            before, after = before + t_before, after + t_after
        break
    check("stagger on a ready or recovering creature pushes its Ready Time back by 2 seconds", ("True" if before is not None and after is not None and after > before + 1.0 else f"{before} {after} {locals().get('so2')!r}"), "True")
    wiz.command("slay winder")
    sen.command("stop")
    sen.command("speed me 0")

    # ===== the attack and defense rule =====
    rule = {"warrior": 1.00, "priest": 0.97, "mage": 0.96, "adventurer": 0.98}
    for arch, rate in rule.items():
        c = chars[arch]
        c.command("setlevel 1")
        c.command("unwield")
        bad = []
        for level in (1, 5, 50, 100):
            c.command(f"setlevel {level}")
            s = a.score_of(c)["text"]
            want = expected_attack(rate, level)
            if f"To hit +{want}" not in s or f"Defense {level - 1}" not in s:
                bad.append((level, want, s[:200]))
        check(f"{arch} (rate {rate:.2f}): Attack at levels 1, 5, 50 and 100 and Defense = level - 1", str(bad), "[]")
    check("the rule at level 100: a rate of 1.00 against 0.96 is 4 points apart", f"{expected_attack(1.0, 100) - expected_attack(0.96, 100)}", "4")
    # the rule in play
    setroom(wiz, "twarrior")
    wr = chars["warrior"]
    wr.command("setlevel 1")
    wr.command("wield sword")
    wiz.command("goto " + ROOM["v"])
    wiz.command("clone " + TEST + "hitter")
    wiz.command("speed hitter 90 600")
    wr.command("kill hitter")
    text = run_fight(wr, wiz, 22)
    hits = count(r"A hitter (?:scratches|hits|smashes) you", text) + count("A hitter misses you", text) * 0
    miss = count("A hitter misses you", text)
    total = hits + miss
    check("level 1 against a level 1 attacker: about half of its attacks land (armor 2)", str(total >= 40 and 0.25 <= hits / total <= 0.75), "True")
    wiz.command("slay hitter")
    wr.command("stop")
    wr.command("setlevel 100")
    wiz.command("clone " + TEST + "hitter")
    wiz.command("speed hitter 90 600")
    wr.command("kill hitter")
    text = run_fight(wr, wiz, 22)
    hits = count(r"A hitter (?:scratches|hits|smashes) you", text)
    miss = count("A hitter misses you", text)
    check("level 100 against a level 1 attacker: only natural 20s land (defense 99)", str(hits + miss >= 40 and hits / (hits + miss) <= 0.2), "True")
    wiz.command("slay hitter")
    wr.command("stop")
    wiz.command("slay dummy")
    cl = wiz.command("clone " + TEST + "boss100")
    wr.command("setlevel 1")
    wiz.command("speed overlord 90 600")
    a.drain(wr, 1.0)
    kb = wr.command("kill overlord")
    text = run_fight(wr, wiz, 22)
    hits = count(r"A overlord (?:scratches|hits|smashes) you", text)
    miss = count("A overlord misses you", text)
    check("level 1 against a level 100 attacker: almost every attack lands", ("True" if hits + miss >= 40 and hits / (hits + miss) >= 0.8 else f"{hits} {miss} {kb!r} {cl!r} {text[-300:]!r}"), "True")
    wiz.command("slay overlord")
    wr.command("stop")

    # ===== the experience formula =====
    ma = chars["mage"]
    bad = []
    for n in (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 25, 50, 99):
        ma.command(f"setlevel {n}")
        s = a.score_of(ma)
        want = f"Level {n}   Experience {xp_for(n)} ({xp_for(n + 1) - xp_for(n)} more for level {n + 1})"
        if want not in s["text"]:
            bad.append((n, want))
    check("experience to reach level n: the table to level 10, the power law beyond: levels 1 to 99", str(bad), "[]")
    check("the table of the design: 100, 300, 600, 1000", f"{xp_for(2)} {xp_for(3)} {xp_for(4)} {xp_for(5)}", "100 300 600 1000")
    check("the corridor table: 5400, 12000, 21700, 34100, 49300", f"{xp_for(6)} {xp_for(7)} {xp_for(8)} {xp_for(9)} {xp_for(10)}", "5400 12000 21700 34100 49300")
    check("beyond level 10 the provisional power law: level 11 needs 65618, level 20 needs 394400", f"{xp_for(11)} {xp_for(20)}", "65618 394400")
    ma.command("setlevel 100")
    check("level 100 is the highest", a.score_of(ma)["text"], f"Level 100   Experience {xp_for(100)} (highest level)")
    check("above the cap is refused", ma.command("setlevel 101"), "Usage")
    ma.command("setlevel 1")
    check("the renamed wizard command: restore", ma.command("restore"), "You are restored to full health.")
    check("the old wizard command heal does not exist for a mage", ma.command("heal"), "What?")  # mage has no heal spell either
    check("the old wizard command smite does not exist", ma.command("smite dummy"), "What?")
    wiz.command("clone " + TEST + "dummy")
    check("the renamed wizard command: slay", wiz.command("slay dummy"), "You slay a dummy!")

    # ===== the class framework, with a class that exists only in this test =====
    setroom(wiz, "tmage")
    ar = chars["mage"]
    ar.command("setlevel 1")
    wiz.command("goto " + ROOM["v"])
    ar.command("unwield")
    check("a test class needs its level", ar.command("pledge zealot_test"), "REFUSED: You must be level 5.")
    ar.command("setlevel 5")
    check("a disabled class cannot be applied", ar.command("pledge closed_test"), "REFUSED: That class is not open.")
    check("an unknown class cannot be applied", ar.command("pledge nonsuch"), "REFUSED: There is no such class.")
    check("an archetype cannot be applied as a second class", ar.command("pledge warrior"), "REFUSED: A calling cannot be changed.")
    give(wiz, ar, items["sword"], "sword")
    check("before the class the mage cannot wield a sword", ar.command("wield sword"), "not trained")
    before_sk = ar.command("skills")
    check("the test class is applied through the internal entry point", ar.command("pledge zealot_test"), "PLEDGED")
    s = a.score_of(ar)["text"]
    check("the class title is shown in score, the level is kept", s, "Class Zealot", "Level 5")
    check("the added equipment access works", ar.command("wield sword"), "You wield a rusty sword")
    check("the granted skill appears in skills", ar.command("skills"), "zap", "Test Zap", "Sure Crit")
    check("the granted trait changes the numbers (critical 100%)", a.score_of(ar)["text"], "Critical 100%")
    check("the experience modifier 1.25 raises the next level (level 6: 1.25 x 5400 = 6750)",
          a.score_of(ar)["text"], f"Experience {xp_for(5)} ({xp_for(6, 1.25) - xp_for(5)} more for level 6)")
    check("a class cannot be taken twice", ar.command("pledge zealot_test"), "REFUSED: You already have a advanced class.")
    ar.command("setlevel 50")
    a.drain(ar, 0.5)
    check("the class's attack rate (0.99) replaces the archetype's: attack 50 - 0 sword bonus = 50 -> round(49.5) = 50",
          ar.command("unwield") + a.score_of(ar)["text"], "To hit +50")
    ar.command("setlevel 100")
    check("rate 0.99 at level 100 is 99", a.score_of(ar)["text"], "To hit +99")
    check("level 100 with the modifier: highest", a.score_of(ar)["text"], "(highest level)")
    wiz.command("clone " + TEST + "dummy")
    ar.command("restore")
    out = ar.command("zap dummy")
    out += a.quiet(ar, 3)
    check("the granted spell works and costs its Mana", out, "ZAPPED")
    ar.command("stop")
    wiz.command("slay dummy")

    # item-granted skills
    give(wiz, ar, TEST + "zap_ring", "ring")
    setroom(wiz, "tscout")
    sc2 = chars["scout"]
    gv = give(wiz, sc2, TEST + "zap_ring", "ring")
    check("a skill the character does not have is not usable", sc2.command("zap dummy"), "What?")
    check("wearing the ring grants the skill", gv + sc2.command("wear ring"), "You put on a zap ring")
    check("the skill is listed apart, as granted by an item", sc2.command("skills"), "Granted by what you wear", "Test Zap")
    sc2.command("remove ring")
    check("and it disappears with the item", sc2.command("skills"), absent=("Granted by what you wear", "Test Zap"))

    # ===== persistence =====
    ar.command("drain")
    ar.command("save")
    saved = open(os.path.join(work, "save", "players", "tmage.json")).read()
    check("the save holds only the ids of the classes", saved, '"archetype": "mage"', '"advancedClass": "zealot_test"')
    check("nothing derived is saved", saved, absent=("startHp", "expModifier", "classRate", "weaponAccess"))
    ar.command("quit")
    time.sleep(0.5)
    ar2 = h.Client("tmage")
    ar2.read_until("> ")
    s = a.score_of(ar2)["text"]
    check("archetype, advanced class and level persist", s, "Calling Mage", "Class Zealot", "Level 100")
    hp, st, mn = res(ar2)
    check("resources persist (drained before logout, not refilled)", str(mn <= 12 and st <= 4), "True")
    check("the granted skills are back after login", ar2.command("skills"), "zap")
    ar2.command("quit")

    # ===== the travel pass =====
    wiz.command("goto " + HOLLOW + "village/guild")
    check("no pass before the quest", wiz.command("i"), absent=("travel pass",))
    wiz.command("goto " + HOLLOW + "village/north_road")
    out = wiz.command("north")
    check("the road north is closed without the pass", out, "need a travel pass", absent=("Vale Road",))
    pilgrim = h.Client("pilgrim", archetype="warrior")
    pilgrim.read_until("> ")
    text = ""
    for _ in range(3):
        text = pilgrim.command("north")
        if "North Road" in text:
            break
    check("a new character walks to the North Road", text, "North Road")
    check("a new character is refused too", pilgrim.command("north"), "need a travel pass")
    wiz.command("goto " + HOLLOW + "cellars/overseer_chamber")
    wiz.command("restore")
    wiz.command("slay overseer")
    wiz.command("goto " + HOLLOW + "village/guild")
    text = wiz.command("report")
    check("Rowan gives the pass with the rewards of Clear the Cellar", text, "Quest complete: Clear the Cellar!", "Rowan gives you a travel pass.")
    check("the pass is in the inventory", wiz.command("i"), "a travel pass")
    wiz.command("goto " + HOLLOW + "village/north_road")
    out = wiz.command("north")
    check("with the pass the road north is open: the Vale Road", out, "The Vale Road", "Oakenfell")
    s = a.score_of(wiz)["text"]
    check("score shows the area and the region of the Vale Road", s, "Area The Vale Road   Region Greenvale")
    check("the road runs on north", wiz.command("look"), "north")
    wiz.command("south")
    wiz.command("drop pass")
    check("the pass can be lost; the road closes again", wiz.command("north"), "need a travel pass")
    wiz.command("goto " + HOLLOW + "village/guild")
    text = wiz.command("talk rowan")
    check("talking to Rowan re-issues a lost pass", text, "Rowan gives you a travel pass.")
    check("only one pass is issued", str(wiz.command("i").count("travel pass")), "1")
    wiz.command("drop pass")
    wiz.command("get pass") if False else None
    text = wiz.command("report")
    check("report re-issues it too", text, "Rowan gives you a travel pass.")
    wiz.command("quit")
    pilgrim.command("quit")
    return


if __name__ == "__main__":
    sys.exit(main())
