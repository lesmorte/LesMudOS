#!/usr/bin/env python3
"""End-to-end test of the 31 Transit Corridor creatures.

Starts the driver against a temporary copy of the mudlib, with a safe test room and a fighting room under /domains/test, and
plays a wizard over TCP:

1. every creature file is cloned and `stat` must show the numbers the file says (level, hit points, damage, armor, dodge,
   experience, gold, loot, Reaction Time and every action with its preparation and recovery);
2. one scripted fight against an ordinary creature (the Forest Spider) shows its Slowing Bite;
3. one scripted fight against a boss (the Bandit Leader) shows Shield Smash staggering and the whistle that calls a lookout.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4966 python3 tests/corridor_mob_test.py
"""
import os
import re
import shutil
import sys
import tempfile

os.environ.setdefault("LESMUDOS_TEST_PORT", "4825")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402  (its helpers: start_driver, fight_until, score_of, quiet, drain)

ROOT = h.ROOT
SECRET = h.SECRET
TEST = "/domains/test/"
MOBS = "/domains/greenvale/corridor/mob/"
HUB = TEST + "hub"
ARENA = TEST + "arena"

FILES = {
    "hub": a.room("TestHub", "Test Hub", {"east": ARENA}, safe=True),
    "arena": a.room("TestArena", "Test Arena", {"west": HUB}, safe=False, reset=600),
}
for _n in sorted(f[:-3] for f in os.listdir(os.path.join(ROOT, "mudlib", "domains", "greenvale", "corridor", "mob"))):
    FILES["s_" + _n] = a.room("Stat" + _n.title().replace("_", ""), "A stat room", {"east": ARENA}, safe=True)   # one safe room each
for _i, _xp in enumerate((100, 200, 300, 400, 3500)):
    FILES[f"dummy_{_i}"] = a.monster(f"Dummy{_i}", "Dummy", ["training dummy"], "a training dummy", 1, 1, 1, tohit=-30, xp=_xp)


def mob_dir(mudlib):
    return os.path.join(mudlib, "domains", "greenvale", "corridor", "mob")


def expected(text):
    """What a creature file says, for comparison with `stat`."""
    def num(field, default=0.0):
        m = re.search(rf"\b{field} = (-?[\d.]+);", text)
        return float(m.group(1)) if m else default

    acts = []
    for m in re.finditer(r'AddCombatAction\("(\w+)", "([^"]+)", preparation: ([\d.]+), recovery: ([\d.]+)([^;]*?)\);', text):
        mult = re.search(r"damageMultiplier: (\d+)", m.group(5))
        acts.append((m.group(2), float(m.group(3)), float(m.group(4)), int(mult.group(1)) if mult else 1))
    return {
        "level": int(num("Level")), "hp": int(num("MaxHp")), "dmin": int(num("DamageMin")), "dmax": int(num("DamageMax")),
        "armor": int(num("Armor")), "dodge": int(num("Dodge")), "xp": int(num("XpValue")), "gmin": int(num("GoldMin")),
        "gmax": int(num("GoldMax")), "react": num("BaseReaction", 2.5), "aggressive": "Aggressive = true;" in text,
        "loot": len(re.findall(r"AddLoot\(", text)), "acts": acts,
        "first_id": re.search(r'Ids\.Add\("([^"]+)"\)', text).group(1),
    }


def run(work, mudlib):
    check = h.check
    wiz = h.Client("wiz")
    wiz.read_until("> ")
    check("secret makes the first wizard", wiz.command("promote " + SECRET), "level 10 wizard")

    # ===== 1. stat of every creature =====
    names = sorted(n[:-3] for n in os.listdir(mob_dir(mudlib)) if n.endswith(".cs"))
    check("31 creature files", str(len(names)), "31")
    for name in names:
        wiz.command("goto " + TEST + "s_" + name)
        want = expected(open(os.path.join(mob_dir(mudlib), name + ".cs"), encoding="utf-8").read())
        cloned = wiz.command("clone " + MOBS + name)
        check(f"{name}: cloned", cloned, "Cloned")
        text = wiz.command("stat " + want["first_id"])
        a_lo, a_hi = want["dmin"], want["dmax"]
        check(f"{name}: level, hit points, damage, armor, dodge", text, f"Level {want['level']}, HP {want['hp']}/{want['hp']}",
              f"Damage {a_lo}-{a_hi}", f"armor {want['armor']}, dodge {want['dodge']}%")
        check(f"{name}: experience, gold, loot, aggression", text, f"aggressive {want['aggressive']}", f"xp {want['xp']}",
              f"gold {want['gmin']}-{want['gmax']}", f"loot {want['loot']}")
        check(f"{name}: Reaction Time", text, f"Reaction Time: base {want['react']:.1f}")
        for act_name, prep, rec, mult in want["acts"]:
            check(f"{name}: action {act_name}", text, f"{act_name}: preparation {prep:.1f}, recovery {rec:.1f}")
            if mult > 1:
                check(f"{name}: {act_name} damage x{mult}", text, f"damage x{mult}")

    # ===== level the wizard to 10 with dummies (4,500 experience) =====
    wiz.command("goto " + HUB)
    for i in range(5):
        wiz.command("clone " + TEST + f"dummy_{i}")
        wiz.command("slay dummy")
    check("the wizard reached level 10", a.score_of(wiz)["text"], "Level 10")

    # ===== 2. an ordinary creature: the Forest Spider's Slowing Bite (bare-handed, so that the fight lasts long enough) =====
    wiz.command("goto " + ARENA)
    wiz.command("clone " + MOBS + "forest_spider")
    wiz.command("kill spider")
    text = a.fight_until(wiz, "Forest Spider", timeout=5)  # the fight has started; collect it to the end below
    text += a.fight_until(wiz, "You gain 135 experience", timeout=400, heal_with=True)
    check("the spider bites", text, "spider")
    check("the Slowing Bite lands and slows", text, "cold, creeping numbness spreads along your limbs. You are slowed!")
    check("the spider dies and pays its experience", text, "You gain 135 experience.")
    a.drain(wiz, 1.0)

    # ===== 3. a boss: the Bandit Leader's Shield Smash and his whistle (with the Overseer's axe) =====
    wiz.command("clone /domains/hollow/obj/overseers_axe")
    check("wield the axe", wiz.command("wield axe"), "You wield the Goblin Overseer's axe.")
    wiz.command("goto " + ARENA)
    wiz.command("clone " + MOBS + "bandit_leader")
    wiz.command("kill leader")
    text = a.fight_until(wiz, "You gain 1550 experience", timeout=420, heal_with=True)
    check("Shield Smash is wound up", text, "swings his shield up and round")
    check("Shield Smash staggers", text, "The shield slams into you and you stagger!")
    check("the whistle calls a lookout", text, "A shrill whistle splits the air, and a lookout comes crashing out of the brush!")
    check("the whistle is used once", str(text.count("A shrill whistle splits the air")), "1")
    check("the boss dies and pays its experience", text, "You gain 1550 experience.")
    a.drain(wiz, 1.0)


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-corridor-mob-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib, novice=True)
    os.makedirs(os.path.join(mudlib, "domains", "test"))
    for name, source in FILES.items():
        h.write(mudlib, TEST + name, source)
    proc, log = a.start_driver(work, mudlib)
    try:
        run(work, mudlib)
        log.flush()
        logged = open(os.path.join(work, "driver.log")).read()
        h.check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:800], "ok")
        c = h.Client("wiz")
        c.read_until("> ")
        c.command("shutdown")
        proc.wait(timeout=10)
    finally:
        if proc.poll() is None:
            proc.kill()
        log.close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-4000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\n{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
