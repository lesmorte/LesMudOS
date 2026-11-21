#!/usr/bin/env python3
"""Tests for the rename of the three resources (docs/ATTRIBUTES.md section 4): HP, Mana and Stamina are Vitality, Focus and
Endurance everywhere a player can see them.

1. The old `prompt` tokens ({hp} {maxhp} {stamina} {maxstamina} {mana} {maxmana}) still expand, silently, and a prompt
   saved with the old default is shown and used in the new words.
2. A character saved before the rename (the keys hp, stamina and mana in its file, the old default prompt) loads with its
   numbers, and a save written now still uses those keys.
3. The old public names still work in content: a creature, an armour piece, a skill and a crafted potion written against
   MaxHp, MaxHpBonus, StaminaCost, ManaCost and RestoreStamina compile and behave; so do the `hp` command and the old
   Query names called by other objects.
4. A scan: nothing a player sees from `score`, `help`, `skills`, `stat`, a fight (with verbose on), a healing potion,
   a crafted drink and the skills of a warrior, a mage and a priest contains HP, hit points, Mana or Stamina.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4950 python3 tests/rename_test.py
"""
import json
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4850")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_test as a  # noqa: E402
import smoke_test as h  # noqa: E402

R = "/domains/renametest/"
HOLLOW = "/domains/hollow/"
check = h.check

# The words that must not reach a player. Short allow-list: nothing at the moment.
OLD_WORDS = re.compile(r"\bHP\b|hit points?|\bMana\b|\bStamina\b", re.IGNORECASE)
ALLOWED = []  # (text that may contain an old word, why); empty on purpose


# ---------------------------------------------------------------- test content written against the OLD public names

ARENA = '''// @inherit /std/room
public class Arena : Room
{
    public override void Create()
    {
        ShortDesc = "The Rename Arena";
        LongDesc = "A room made for the rename test.";
        Safe = false;
        ResetSeconds = 600;
    }
}
'''

# A creature that is hard to kill and hits often but lightly, written against MaxHp.
OLD_DUMMY = '''// @inherit /std/monster
public class OldDummy : Monster
{
    public override void Create()
    {
        Name = "Old Dummy";
        Ids.Add("dummy");
        ShortDesc = "an old dummy";
        LongDesc = "A creature written against MaxHp.";
        Level = 1; MaxHp = 700; DamageMin = 1; DamageMax = 2; ToHit = 30; Armor = 0; XpValue = 0; Aggressive = false;
    }
}
'''

OLD_HELM = '''// @inherit /std/armor
public class OldHelm : Armor
{
    public override void Create()
    {
        Name = "old helm";
        Ids.Add("helm");
        ShortDesc = "an old helm";
        LongDesc = "A helm written against MaxHpBonus.";
        Slot = "head";
        ArmorValue = 1;
        MaxHpBonus = 7;
        Magic = "raises your maximum Vitality by 7";
    }
}
'''

OLD_SKILL = '''// @inherit /std/skill
public class OldSkill : Skill
{
    public override void Create()
    {
        Title = "Old Skill";
        Command = "oldskill";
        Kind = "spell";
        Summary = "A spell written against ManaCost and StaminaCost.";
        ManaCost = 9;
        StaminaCost = 4;
        Recovery = 0.5;
        Target = "none";
    }
    public override void Resolve(ObjectRef user, ObjectRef? target) { TellObject(user, "OLDSKILLED\\n"); }
}
'''

OLD_RING = '''// @inherit /std/armor
public class OldRing : Armor
{
    public override void Create()
    {
        Name = "Old Ring"; Ids.Add("ring"); ShortDesc = "an old ring"; LongDesc = "Grants a skill written against ManaCost.";
        Slot = "trinket"; ArmorValue = 0; Value = 1;
    }
    public override List<string> QueryGrantedSkills() => new List<string> { "old_skill" };
}
'''

OLD_POTION = '''// @inherit /std/crafted
public class OldPotion : Crafted
{
    public override void Create()
    {
        Name = "old tonic";
        Ids.Add("tonic");
        ShortDesc = "an old tonic";
        Plural = "old tonics";
        LongDesc = "A tonic written against RestoreStamina.";
        UseVerb = "drink";
        RestoreStamina = 30;
    }
}
'''

# An object that asks a character for its numbers by the old method names, as content written before the rename does.
OLD_PROBE = '''// @inherit /std/item
public class OldProbe : Item
{
    public override void Create()
    {
        Name = "probe";
        Ids.Add("probe");
        ShortDesc = "a probe";
        LongDesc = "Asks by the old names.";
    }

    public override void Init() => AddAction("oldasks", CmdAsk);

    private bool CmdAsk(string args)
    {
        var who = ThisPlayer();
        Write("OLDASKS " + who.Call<int>("QueryHp") + " " + who.Call<int>("QueryMaxHp") + " " + who.Call<int>("QueryStamina") + " " +
              who.Call<int>("QueryMaxStamina") + " " + who.Call<int>("QueryMana") + " " + who.Call<int>("QueryMaxMana") + "\\n");
        Write("NEWASKS " + who.Call<int>("QueryVitality") + " " + who.Call<int>("QueryMaxVitality") + " " + who.Call<int>("QueryEndurance") + " " +
              who.Call<int>("QueryMaxEndurance") + " " + who.Call<int>("QueryFocus") + " " + who.Call<int>("QueryMaxFocus") + "\\n");
        return true;
    }
}
'''


def login(name, archetype=None):
    c = h.Client(name, archetype=archetype)
    c.read_until("> ")
    return c


def scan(label, text):
    """Fail if `text` holds one of the old words outside the allow-list."""
    shown = text
    for ok, _why in ALLOWED:
        shown = shown.replace(ok, "")
    found = sorted({m.group(0) for m in OLD_WORDS.finditer(shown)})
    check(f"scan: {label} has no HP, hit points, Mana or Stamina", "ok" if not found else f"found {found} in {shown[:300]!r}", "ok")


def save_path(work, name):
    for root, _, files in os.walk(os.path.join(work, "save")):
        for f in files:
            if f.startswith(name) and f.endswith(".json"):
                return os.path.join(root, f)
    return None


def numbers(text):
    """(vitality, max, endurance, max, focus, max) from the vitality command."""
    m = re.search(r"Vitality (\d+)/(\d+)   Endurance (\d+)/(\d+)   Focus (\d+)/(\d+)", text)
    return tuple(int(x) for x in m.groups()) if m else None


def run(work, mudlib):
    wiz = login("wiz", "warrior")
    check("setup: the first wizard", wiz.command("promote " + h.SECRET), "level 10 wizard")
    wiz.command("goto " + R + "arena")

    # ===== 1. the prompt =====
    pat = login("prue", "warrior")
    text = pat.command("look")
    check("prompt: the default is in the new words' numbers", text.splitlines()[-1], "Prue (35/35) (100/100) (20/20) > ")
    text = pat.command("prompt")
    check("prompt: shows the new tokens", text, "{name}", "{vitality}", "{maxvitality}", "{endurance}", "{maxendurance}", "{focus}", "{maxfocus}",
          absent=("{hp}", "{maxhp}", "{stamina}", "{maxstamina}", "{mana}", "{maxmana}"))
    scan("prompt help", text)
    pat.command("prompt [{hp}|{maxhp}] [{stamina}|{maxstamina}] [{mana}|{maxmana}] {name}> ")
    check("prompt: the old tokens expand (silently)", pat.command("look").splitlines()[-1], "[35|35] [100|100] [20|20] Prue> ")
    pat.command("prompt [{vitality}|{maxvitality}] [{endurance}|{maxendurance}] [{focus}|{maxfocus}] {name}> ")
    check("prompt: and the new tokens give the same numbers", pat.command("look").splitlines()[-1], "[35|35] [100|100] [20|20] Prue> ")
    pat.command("prompt default")
    check("prompt: the default restores", pat.command("look").splitlines()[-1], "Prue (35/35) (100/100) (20/20) > ")
    pat.command("quit")
    time.sleep(0.5)

    # ===== 2. a character saved before the rename =====
    old = login("oldhand", "warrior")
    old.command("save")
    old.command("quit")
    time.sleep(0.6)
    path = save_path(work, "oldhand")
    check("old save: the save file is there", str(path is not None), "True")
    with open(path) as f:
        saved = json.load(f)
    check("old save: a save written now still uses the keys hp, stamina and mana", " ".join(k for k in ("hp", "stamina", "mana") if k in saved), "hp stamina mana")
    # Write it as the game wrote it before the rename: those three keys, and the old default prompt.
    saved["hp"] = 9
    saved["stamina"] = 41.0
    saved["mana"] = 3.0
    saved["promptFormat"] = "{name} ({hp}/{maxhp}) ({stamina}/{maxstamina}) ({mana}/{maxmana}) > "
    with open(path, "w") as f:
        json.dump(saved, f)
    old = login("oldhand", "warrior")
    out = old.command("vitality")
    n = numbers(out)
    check("old save: it loads, with its Vitality, Endurance and Focus", "ok" if n and 9 <= n[0] <= 12 and 41 <= n[2] <= 60 and 3 <= n[4] <= 8 and n[1] == 35 else repr(out), "ok")
    check("old save: and the maxima are the calling's", f"{n[1]} {n[3]} {n[5]}" if n else out, "35 100 20")
    check("old save: the saved old default prompt shows the numbers in the new default", old.command("look").splitlines()[-1], "Oldhand (")
    check("old save: and `prompt` lists the new tokens for it", old.command("prompt"), "{vitality}", "{maxendurance}", absent=("{hp}", "{stamina}", "{mana}"))
    old.command("prompt <{hp}:{mana}>")
    old.command("save")
    old.command("quit")
    time.sleep(0.6)
    with open(path) as f:
        saved = json.load(f)
    check("old save: a prompt typed with the old tokens is saved as typed", saved.get("promptFormat", ""), "<{hp}:{mana}> ")
    old = login("oldhand", "warrior")
    m = re.match(r"^<(\d+):(\d+)> $", old.command("look").splitlines()[-1])
    check("old save: and still expands after another login", "ok" if m and int(m.group(1)) >= 9 else "no", "ok")
    old.command("quit")

    # ===== 3. content written against the old public names =====
    wiz.command("goto " + R + "arena")
    out = wiz.command("clone " + R + "old_dummy")
    check("old names: a creature with MaxHp loads", out, "Cloned")
    out = wiz.command("stat dummy")
    check("old names: and its MaxHp is its maximum Vitality", out, "Vitality 700/700")
    wiz.command("slay dummy")
    out = wiz.command("clone " + R + "old_helm")
    check("old names: an armour piece with MaxHpBonus loads", out, "Cloned")
    wiz.command("get helm")
    before = numbers(wiz.command("vitality"))
    wiz.command("wear helm")
    after = numbers(wiz.command("vitality"))
    check("old names: and its bonus adds to the maximum Vitality", f"{after[1] - before[1]}" if before and after else "?", "7")
    wiz.command("remove helm")
    out = wiz.command("clone " + R + "old_potion")
    check("old names: a crafted potion with RestoreStamina loads", out, "Cloned")
    wiz.command("get tonic")
    # `hp`, the old command, is the same as `vitality`.
    check("old names: the hp command still works and says the same", wiz.command("hp"), "Vitality ", "Endurance ", "Focus ")
    out = wiz.command("clone " + R + "old_probe")
    wiz.command("get probe")
    out = wiz.command("oldasks")
    check("old names: QueryHp, QueryMaxHp, QueryStamina and the others still answer, with the new answers", out,
          "OLDASKS", "NEWASKS")
    olds = re.search(r"OLDASKS ([\d ]+)", out)
    news = re.search(r"NEWASKS ([\d ]+)", out)
    check("old names: and agree with the new methods", "ok" if olds and news and olds.group(1).split()[1::2] == news.group(1).split()[1::2] else out, "ok")
    # A spell written against ManaCost and StaminaCost, granted by a ring: it costs Focus 9 and Endurance 4.
    wiz.command("clone " + R + "old_ring")
    wiz.command("get ring")
    out = wiz.command("wear ring")
    check("old names: an item granting a skill written against the old cost names", out, "You put on an old ring")
    wiz.command("restore")
    before = numbers(wiz.command("vitality"))
    out = wiz.command("oldskill") + a.quiet(wiz, 2)
    check("old names: the skill runs", out, "OLDSKILLED")
    after = numbers(wiz.command("vitality"))
    check("old names: ManaCost 9 is 9 Focus and StaminaCost 4 is 4 Endurance", f"{before[4] - after[4]} {before[2] - after[2]}" if before and after else "?", "9 4")
    wiz.command("remove ring")
    out = wiz.command("drink tonic")
    check("old names: a crafted drink with RestoreStamina restores Endurance", "ok" if re.search(r"You recover \d+ Endurance\.", out) else out, "ok")

    # ===== 4. the scan =====
    scanners = {"warrior": login("scanwar", "warrior"), "mage": login("scanmag", "mage"), "priest": login("scanpri", "priest")}
    texts = {}
    for cls, c in scanners.items():
        c.command("down")  # from the square into the arena
        t = c.command("score") + c.command("sc") + c.command("vitality") + c.command("hp") + c.command("skills") + c.command("prompt") + c.command("verbose")
        t += c.command("help") + c.command("help crafting") + c.command("help harvest") + c.command("stat me") + c.command("consider me")
        texts[cls] = t
        scan(f"{cls}: score, vitality, skills, prompt, verbose, help, stat", t)
    check("scan: the score shows the new words", texts["warrior"], "Vitality 35/35", "Endurance 100/100", "Focus 20/20")
    check("scan: and the skill lists show their costs in them", texts["warrior"] + texts["mage"], "Endurance", "Focus")
    for c in scanners.values():
        c.command("verbose on")
    wiz.command("goto " + R + "arena")
    wiz.command("clone " + R + "old_dummy")
    fight = {}
    war = scanners["warrior"]
    war.command("kill dummy")
    text = a.quiet(war, 3) + war.command("bash dummy") + a.quiet(war, 12)
    fight["warrior"] = text
    war.command("flee")
    mag = scanners["mage"]
    mag.command("kill dummy")
    text = a.quiet(mag, 2) + mag.command("bolt dummy") + a.quiet(mag, 12)
    fight["mage"] = text
    mag.command("flee")
    pri = scanners["priest"]
    pri.command("kill dummy")
    text = a.quiet(pri, 5) + pri.command("heal") + a.quiet(pri, 12)
    fight["priest"] = text
    pri.command("flee")
    check("scan: a fight happened, with verbose notes", "".join(fight.values()), "damage)")
    check("scan: the priest healed, and the verbose note says Vitality", fight["priest"], "Vitality")
    for cls, text in fight.items():
        scan(f"{cls}: a fight with a skill and verbose damage notes", text)
    wiz.command("goto " + R + "arena")
    wiz.command("slay dummy")

    # Potions, drinks and healing, taken by the wizard after the dummy has hurt it.
    wiz.command("goto " + R + "arena")
    wiz.command("clone " + R + "old_dummy")
    wiz.command("kill dummy")
    out = a.quiet(wiz, 6)
    wiz.command("flee")
    wiz.command("goto " + R + "arena")
    wiz.command("slay dummy")
    for thing in ("healing_potion", "craft/sweet_berry_juice", "craft/minor_stamina_tonic", "craft/herbal_bandage", "craft/minor_healing_salve"):
        wiz.command("clone " + HOLLOW + "obj/" + thing)
    wiz.command("get all")
    for verb, word in (("drink", "potion"), ("drink", "juice"), ("drink", "tonic"), ("apply", "bandage"), ("apply", "salve")):
        out += wiz.command(f"{verb} {word}")
        out += a.quiet(wiz, 1.5)
    out += wiz.command("vitality") + wiz.command("i") + wiz.command("restore") + wiz.command("score")
    check("scan: the potion and the drinks said something", out, "You recover", "Endurance")
    scan("a healing potion, berry juice, a tonic, a bandage, a salve, `restore` and `score`", out)
    for c in list(scanners.values()):
        c.command("quit")
    wiz.command("quit")


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-rename-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    os.makedirs(os.path.join(mudlib, "domains", "renametest"), exist_ok=True)
    for name, source in (("arena", ARENA), ("old_dummy", OLD_DUMMY), ("old_helm", OLD_HELM),
                         ("old_potion", OLD_POTION), ("old_probe", OLD_PROBE), ("old_ring", OLD_RING)):
        h.write(mudlib, R + name, source)
    h.write(mudlib, "/skills/old_skill", OLD_SKILL)
    h.edit(mudlib, HOLLOW + "village/square", 'Exits["south"]', f'Exits["down"] = "{R}arena";\n        Exits["south"]')
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
