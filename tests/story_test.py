#!/usr/bin/env python3
"""End-to-end test of the storytelling pass (docs/ATTRIBUTES.md section 5, step 5): the people, readables, Chronicle templates,
Renown lines and calling descriptions that now speak of Might, Grace, Vigor, Insight, Spirit and Resolve.

Plays the real game as ordinary (non-wizard) characters. For every person touched, asks every key of every added topic (so that no key
is shadowed by an older one) and checks that the answer is the added one and names the attribute it is about. Reads each new readable
and each reworded one. Checks that the level-up message and `score` say, in one line that names `improve`, that points are waiting, and
that the line is gone once they are placed. Checks the calling descriptions shown at creation. Scans everything added for the
words the game never uses (HP, hit points, Mana, Stamina, Circle of Ash, Philippa Reeve, Goblin Blood) and checks that only the
guild master names the `improve` command.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4836 python3 tests/story_test.py
"""
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4836")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_test as a  # noqa: E402
import smoke_test as h  # noqa: E402

MUDLIB_SRC = os.path.join(h.ROOT, "mudlib")
D = "domains/"
CHECKS = [0]
STR = r'"((?:[^"\\]|\\.)*)"'

PEOPLE = {'greenvale/corridor/approach/npc/ranger.cs': [('grace,graceful,quiet foot,aim,steady,tracking,archery', ['Grace'])],
 'greenvale/corridor/eldergreen_edge/npc/berenice.cs': [('resolve,patience,endurance,burn,long nights', ['Resolve', 'Endurance', 'Vigor'])],
 'greenvale/corridor/eldergreen_edge/npc/gwenllian.cs': [('resolve,duty,endurance,standing,perseverance,steadfast,remembered', ['Resolve']),
                                                         ('insight,noticing,observe,observing,reading,track reading', ['Insight'])],
 'greenvale/corridor/eldergreen_edge/npc/hywel.cs': [('insight,noticing,patience,reading,sense,instinct,attention,attend', ['Insight'])],
 'greenvale/corridor/heartwood/npc/cassian.cs': [('resolve,remembered,vigil,watch,edric,dusk watch', ['Resolve'])],
 'greenvale/corridor/heartwood/npc/marcella.cs': [('insight,study,method,understanding,learn,learning,discovery,discoveries,notes,patience',
                                                   ['Insight'])],
 'greenvale/corridor/south_road/npc/innkeeper_aldith.cs': [('seat,sit,sitting,bench,hearth,breath,recover,recovery,mend,mending,vigor,vitality,endurance,resting',
                                                            ['Vigor'])],
 'greenvale/corridor/south_road/npc/miller_leofric.cs': [('might,strength,strong,sacks,sack,lift,lifting,arm,arms', ['Might'])],
 'greenvale/corridor/south_road/npc/veteran_kenrick.cs': [('vigor,toughness,tough,hardy,survive,survived,survival,healing,wounds',
                                                           ['Vigor']),
                                                          ('resolve,nerve,fear,courage,brave,bravery', ['Resolve'])],
 'greenvale/oakenfell/founders/npc/garrett_vance.cs': [('vigor,vitality,survive,survival,scars,scar,remembered,wounds',
                                                        ['Vigor', 'Insight'])],
 'greenvale/oakenfell/guild/npc/chronicler.cs': [('attributes,attribute,feat,feats,deed,deeds,virtue,virtues,remembered,remember',
                                                  ['Might', 'Resolve', 'Insight']),
                                                 ('might,grace,vigor,insight,spirit,resolve,six words,six',
                                                  ['Might', 'Grace', 'Vigor', 'Insight', 'Spirit', 'Resolve'])],
 'greenvale/oakenfell/guild/npc/hallmaster_adventurer.cs': [('attributes,attribute,lean,leans,stats,points,all six,six',
                                                             ['Vigor', 'Insight'])],
 'greenvale/oakenfell/guild/npc/hallmaster_mage.cs': [('attributes,attribute,insight,focus,lean,leans,stats,points',
                                                       ['Insight', 'Spirit', 'Vigor'])],
 'greenvale/oakenfell/guild/npc/hallmaster_priest.cs': [('attributes,attribute,spirit,focus,lean,leans,stats,points',
                                                         ['Spirit', 'Vigor', 'Insight'])],
 'greenvale/oakenfell/guild/npc/hallmaster_scout.cs': [('attributes,attribute,grace,insight,resolve,quickness,lean,leans,stats,points',
                                                        ['Grace', 'Insight', 'Resolve'])],
 'greenvale/oakenfell/guild/npc/hallmaster_warrior.cs': [('attributes,attribute,might,vigor,resolve,strength,lean,leans,stats,points',
                                                          ['Might', 'Vigor', 'Resolve'])],
 'greenvale/oakenfell/guild/npc/librarian.cs': [('insight,study,learning,knowledge,scholars,wisdom', ['Insight'])],
 'greenvale/oakenfell/guild/npc/trainer_rurik.cs': [('might,strength,power,heavy,arm,arms,resolve,endurance,wind',
                                                     ['Might', 'Resolve', 'Endurance'])],
 'greenvale/oakenfell/guild/npc/trainer_wren.cs': [('grace,steady,steadiness,aim,hand,hands,precision,accuracy', ['Grace'])],
 'greenvale/oakenfell/river/npc/sergeant_maren.cs': [('resolve,vigilance,vigilant,duty,vigil,remembered', ['Resolve'])],
 'hollow/npc/benson.cs': [('resolve,stubborn,stubbornness,remembered,remember,digging', ['Resolve'])],
 'hollow/npc/brann_odell.cs': [('resolve,endurance,wind,breath,tire,tired,tiring,discipline,stand,grit', ['Resolve', 'Endurance']),
                               ('remembered,best soldier,legend,fame', ['Resolve'])],
 'hollow/npc/elspeth.cs': [('vigor,vitality,health,body,constitution,strong,tough,toughness,hardy', ['Vigor', 'Vitality']),
                           ('remembered,stories,old hob,hob,sickness winter', ['Vigor'])],
 'hollow/npc/guard.cs': [('watch,watchmen,duty,night,nights,resolve,vigil', ['Resolve'])],
 'hollow/npc/halbert_quill.cs': [('knowledge,learning,understanding,study,discovery,discoveries,recipes,secrets,wisdom,mind',
                                  ['Insight', 'Focus']),
                                 ('remembered,famous,scholars', ['Insight'])],
 'hollow/npc/harland.cs': [('seat,sit,sitting,breath,recover,recovery,mend,mending,vigor,vitality,endurance,focus,resting',
                            ['Vigor', 'Resolve'])],
 'hollow/npc/rowan.cs': [('attributes,attribute,improve,points,point,placing,placed,spend,spending,six', ['Might', 'Resolve']),
                         ('callings,calling,warrior,scout,priest,mage,adventurer,class,classes,lean,leans',
                          ['Might', 'Grace', 'Spirit', 'Insight']),
                         ('remembered,remember,fame,renown,stories,songs,legend,legends', ['Resolve'])],
 'hollow/npc/sister_merewyn.cs': [('spirit,inner,belief,inner strength,calm,stillness', ['Spirit', 'Focus']),
                                  ('focus,concentration,mind,casting,cast', ['Focus', 'Insight', 'Spirit']),
                                  ('remembered,names,wall,sill names', ['Spirit'])],
 'hollow/npc/torren.cs': [('might,strength,strong,arm,arms,muscle,power,heavy,swing', ['Might']),
                          ('remembered,famous smith,fame', ['Might'])],
 'hollow/npc/wenna_farrow.cs': [('grace,graceful,steady,steadiness,accuracy,accurate,precise,precision,quick,critical', ['Grace']),
                                ('remembered,fame,legend', ['Grace'])]}

# (room, key to read, words the text must contain)
READABLES = [
    ("hollow/village/guild", "labels", ["Grace", "Might", "Resolve"]),
    ("hollow/village/shrine", "names", ["Spirit", "Resolve"]),
    ("hollow/village/tavern", "toasts", ["Resolve", "Vigor"]),
    ("greenvale/oakenfell/guild/chronicle_gallery", "six words", ["Might", "Grace", "Vigor", "Insight", "Spirit", "Resolve"]),
    ("greenvale/oakenfell/southgate/silver_stag", "toasts", ["Resolve", "Vigor", "Might"]),
    ("greenvale/oakenfell/guild/proving_yard", "slate", ["Might", "Grace", "Resolve"]),
    # existing readables, reworded
    ("greenvale/oakenfell/grove/great_stump", "hale plaque", ["remembered for his Resolve"]),
    ("greenvale/oakenfell/grove/great_stump", "rowan plaque", ["remembered for her Resolve"]),
    ("greenvale/corridor/heartwood/m07", "slab", ["Edric Vane", "Resolve", "Room left"]),
]
FORBIDDEN = ["Circle of Ash", "Philippa", "Reeve", "Goblin Blood", "goblin blood", "Stamina", "Mana", " HP", "HP ", "hit points", "Hit points"]
ATTRS = ["Might", "Grace", "Vigor", "Insight", "Spirit", "Resolve"]
CLASSES = {"warrior": ["Might", "Vigor", "Resolve"], "scout": ["Grace", "Insight", "Resolve"], "priest": ["Spirit", "Vigor", "Insight"],
           "mage": ["Insight", "Spirit"], "adventurer": ATTRS}


def check(label, text, *expected, absent=()):
    CHECKS[0] += 1
    h.check(label, text, *expected, absent=absent)


def truth(label, ok, detail=""):
    CHECKS[0] += 1
    h.check(label, "ok" if ok else "NOT OK " + str(detail)[-700:], "ok")


def source(path):
    with open(os.path.join(MUDLIB_SRC, path), encoding="utf-8") as f:
        return f.read()


def unescape(text):
    return text.replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\")


def plain(text):
    return re.sub(r"\{[a-z/]*\}", "", unescape(text)).replace("{{", "{")


def topics_of(path):
    return [(m.group(1), m.group(2)) for m in re.finditer(r'AddTopic\(\s*' + STR + r'\s*,\s*' + STR + r'\s*\)', source(path))]


def spawn_rooms():
    """NPC file (path under mudlib, no .cs) -> a room file (path under mudlib, no .cs) that spawns it."""
    found = {}
    for root, _, files in os.walk(os.path.join(MUDLIB_SRC, "domains")):
        for n in files:
            if not n.endswith(".cs"):
                continue
            full = os.path.join(root, n)
            with open(full, encoding="utf-8") as f:
                for m in re.finditer(r'AddSpawn\("(/domains/[^"]+)"', f.read()):
                    found.setdefault(m.group(1).lstrip("/"), os.path.relpath(full, MUDLIB_SRC)[:-3])
    return found


def word_of(path):
    src = source(path)
    name = unescape(re.search(r'\bName = ' + STR, src).group(1)).lower()
    words = name.split()
    for m in re.finditer(r'Ids\.Add\(' + STR + r'\)', src):
        i = m.group(1).lower()
        if " " not in i and i in words:
            return i
    return words[0] if words[0] not in ("the", "old", "commander", "ranger", "sister") else words[-1]


LAB_PROBE = r'''// @inherit /std/thing
public class StProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init() { foreach (var v in new[] { "tele", "xp" }) AddAction(v, Do); }
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        switch (QueryVerb())
        {
            case "tele": MoveObject(me, LoadObject(args.Trim())); me.Call("Look"); return true;
            case "xp": me.Call("AddXp", int.Parse(args.Trim())); return true;
        }
        return false;
    }
}
'''
LAB = """// @inherit /std/room
public class StLab : Room
{
    public override void Create()
    {
        ShortDesc = "Story Lab";
        LongDesc = "A bare room made for the story test.";
        Exits["out"] = "/domains/hollow/village/square";
    }
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who != null && Interactive(who) && Present("probe", who) == null) MoveObject(CloneObject("/domains/sttest/probe"), who);
    }
}
"""


def run(work, mudlib):
    w = h.Client("warden")
    w.read_until("> ")
    check("the secret makes the warden a wizard", w.command("promote " + h.SECRET), "level 10 wizard")

    # ===== the files: forbidden words, and `improve` only from the guild master =====
    rooms = spawn_rooms()
    added_text = []
    for path, topics in PEOPLE.items():
        src_topics = {k: t for k, t in topics_of(D + path)}
        for keys, expect in topics:
            added_text.append((path, plain(src_topics[keys])))
    bad = [(p, wd) for p, t in added_text for wd in FORBIDDEN if wd in t]
    truth(f"none of the {len(added_text)} added answers uses a forbidden word", not bad, bad)
    wrong = [p for p, t in added_text if "improve" in t.lower() and not p.endswith("rowan.cs")]
    truth("only the guild master names the improve command", not wrong, wrong)
    truth("the guild master names it plainly", any("improve" in t for p, t in added_text if p.endswith("rowan.cs")))
    caps = [(p, t[:40]) for p, t in added_text if re.search(r"\b(HP|Mana|Stamina|hit points)\b", t)]
    truth("no old resource name in an added answer", not caps, caps)
    for path in ("events/boss.cs", "events/discover.cs", "events/level.cs", "events/craft.cs", "events/quest.cs", "secure/renown.cs"):
        strings = " ".join(unescape(m) for m in re.findall(STR, source(path)))
        truth(f"{path} has no forbidden word", not [x for x in FORBIDDEN if x in strings])
    for room, key, words in READABLES:
        s = source(D + room + ".cs")
        truth(f"{room} has no forbidden word in a string", not [x for x in FORBIDDEN if x in " ".join(re.findall(STR, s))])

    # ===== a player walks to every person and asks every key =====
    ann = h.Client("annwen", archetype="adventurer")
    ann.read_until("> ")
    ann.command("lab")  # the lab hands out the probe
    ann.command("out")
    asked = 0
    for path, topics in PEOPLE.items():
        npc = D + path[:-3]
        room = rooms.get(npc)
        if room is None:
            truth(f"{path}: a room spawns this person", False)
            continue
        who = unescape(re.search(r'\bName = ' + STR, source(D + path)).group(1))
        word = word_of(D + path)
        ann.command("tele /" + room)
        src_topics = dict(topics_of(D + path))
        for keys, expect in topics:
            text = plain(src_topics[keys])
            lead = text[:30]
            for key in keys.split(","):
                got = ann.command(f"ask {word} about {key}")
                asked += 1
                check(f"{who}: ask about '{key}' gets the added answer", got, who + " says:", lead, absent=("can't tell you",))
            ans = ann.command(f"ask {word} about {keys.split(',')[0]}")
            for ex in expect:
                check(f"{who}: the answer on '{keys.split(',')[0]}' names {ex}", ans, ex)
    print(f"{asked} questions asked of {len(PEOPLE)} people")

    # ===== the guild master's quest talk still works =====
    ann.command("tele /domains/hollow/village/guild")
    check("Rowan still answers about the cellar", ann.command("ask rowan about cellar"), "old smugglers' tunnels")

    # ===== the readables =====
    for room, key, words in READABLES:
        ann.command("tele /" + D + room)
        got = ann.command("read " + key)
        for wd in words:
            check(f"{room}: read {key} names '{wd}'", got, wd)

    # ===== unspent points: the level-up line and score =====
    ann.command("tele /domains/hollow/village/square")
    s0 = ann.command("score")
    check("no reminder at level 1", s0, absent=("Attribute points to place",))
    up = ann.command("xp 100")
    check("the level-up message says points wait and names improve", up, "You have reached level 2", "You have 2 points to place in your attributes", "improve")
    s1 = ann.command("score")
    check("score says so on one line that names improve", s1, "Attribute points to place: 2 (improve ATTRIBUTE)")
    ann.command("improve might 2")
    check("the reminder is gone once the points are placed", ann.command("score"), absent=("Attribute points to place",))

    # ===== the callings at creation =====
    for name, attrs in CLASSES.items():
        c = h.Client("pick" + name[:5], login=False)
        c.read_any(("Password: ",))
        c.send(h.PASSWORD)
        c.read_until("again: ")
        c.send(h.PASSWORD)
        c.read_until("Your calling: ")
        c.send("help " + name)
        got = c.read_until("Your calling: ")
        for at in attrs:
            check(f"creation, help {name}: names {at}", got, at)
        c.sock.close()

    # ===== the Chronicle and Renown =====
    for needle, f in (("a feat of Resolve", "events/boss.cs"), ("a discovery of Insight", "events/discover.cs"), ("a feat of Insight", "events/craft.cs"),
                      ("Some sound like feats of Resolve", "secure/renown.cs"), ("brought down the Goblin Overseer in the tunnels beneath Vincent's Hollow", "events/boss.cs")):
        truth(f"{f} says '{needle}'", needle in source(f))
    for f in os.listdir(os.path.join(MUDLIB_SRC, "events")):
        s = source("events/" + f)
        for text_line in re.findall(r"^text=(.*)$", s, re.M):
            for ph in re.findall(r"\{(\w+)\}", text_line):
                truth(f"events/{f}: placeholder {{{ph}}} is a known one", ph in ("actor", "participants", "area", "region", "date", "name", "subject", "level", "class", "count"), ph)
    for f in ("boss.cs",):
        s = source("events/" + f)
        truth("no template claims a score of the actor", not re.search(r"(his|her|their) (Might|Grace|Vigor|Insight|Spirit|Resolve)", s))


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-story-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    os.makedirs(os.path.join(mudlib, "domains", "sttest"))
    h.write(mudlib, "/domains/sttest/probe", LAB_PROBE)
    h.write(mudlib, "/domains/sttest/lab", LAB)
    h.edit(mudlib, "/domains/hollow/village/square", 'Exits["south"]', 'Exits["lab"] = "/domains/sttest/lab";\n        Exits["south"]')
    proc, log = a.start_driver(work, mudlib)
    started = time.time()
    try:
        run(work, mudlib)
        w = h.Client("warden")
        w.read_until("> ")
        w.command("shutdown")
        proc.wait(timeout=20)
    finally:
        if proc.poll() is None:
            proc.kill()
        log.close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-4000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\nwhole test {time.time() - started:.0f} s")
    print(f"{CHECKS[0]} checks, {len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
