#!/usr/bin/env python3
"""End-to-end test of the Vincent's Hollow expansion (docs/specs/hollow-expansion.md sections 2, 3 and 4).

Plays the real game as ordinary (non-wizard) characters: walks every new room both ways and looks at every detail, talks to every person
on every topic, browses and buys from every new shop, wields and wears every new item and reads its numbers in `score`, attacks the
Practice Yard's dummy and checks that no experience, kill credit, corpse or Chronicle entry comes of it, takes each find once and not
twice, and runs the Shrine-Blessed Mace quest from start to end. What is checked against the files: every shop's stock and price (parsed
from the NPC files), every weapon's and armour's numbers (parsed from the item files), every room's details, every person's topics and chat.

The test's own fixtures, the only help the characters get, are in a lab room off the square (it hands out a probe): gold; moving a
character to a place; a creature killed by damage that comes from the character (so the real death code runs: experience, kill credit,
Chronicle); damage to the character; and a count of how often a creature's corpse holds an item (the loot percentages).

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4834 python3 tests/hollow_expansion_test.py
"""
import json
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4834")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_test as a  # noqa: E402
import smoke_test as h  # noqa: E402

HOLLOW = "/domains/hollow/"
V = HOLLOW + "village/"
N = HOLLOW + "npc/"
O = HOLLOW + "obj/"
M = HOLLOW + "mob/"
CELLARS = HOLLOW + "cellars/"
T = "/domains/hxtest/"
MUDLIB_SRC = os.path.join(h.ROOT, "mudlib")
CHECKS = [0]


def check(label, text, *expected, absent=()):
    CHECKS[0] += 1
    h.check(label, text, *expected, absent=absent)


def truth(label, ok, detail=""):
    CHECKS[0] += 1
    h.check(label, "ok" if ok else "NOT OK " + str(detail)[-700:], "ok")


# ---------------------------------------------------------------- reading the files the content is made of

def source(mudpath):
    with open(os.path.join(MUDLIB_SRC, mudpath.lstrip("/") + ".cs"), encoding="utf-8") as f:
        return f.read()


STR = r'"((?:[^"\\]|\\.)*)"'


def unescape(text):
    return text.replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\")


def plain(text):
    """What a player reads of a string with colour tags: {cmd} and {/} removed, {{ made {."""
    return re.sub(r"\{[a-z/]*\}", "", unescape(text)).replace("{{", "{")


def lead(text, n=28):
    """The start of a text, up to the first tag, for a check that the right text was shown."""
    return unescape(text).split("{")[0].split("\n")[0][:n].rstrip()


def num(src, name, default=0):
    m = re.search(r"\b" + name + r"\s*=\s*(-?[\d.]+)\s*;", src)
    return float(m.group(1)) if m else default


def short_of(src):
    return unescape(re.search(r'ShortDesc\s*=\s*' + STR, src).group(1))


def name_of(src):
    return unescape(re.search(r'\bName\s*=\s*' + STR, src).group(1))


def stock_of(npc):
    """[(item path, price)] from the AddStock lines of an NPC file."""
    return [(m.group(1), int(m.group(2))) for m in re.finditer(r'AddStock\(\s*"([^"]+)"\s*,\s*(\d+)\s*\)', source(npc))]


def topics_of(npc):
    return [(m.group(1).split(","), m.group(2)) for m in re.finditer(r'AddTopic\(\s*' + STR + r'\s*,\s*' + STR + r'\s*\)', source(npc))]


def chats_of(npc):
    return [m.group(1) for m in re.finditer(r'AddChat\(\s*' + STR + r'\s*\)', source(npc))]


readables = {}


def details_of(room):
    """[(names, text, is_readable)] of a room file."""
    src = source(room)
    found = [(m.group(1).split(","), m.group(2), False) for m in re.finditer(r'AddDetail\(\s*' + STR + r'\s*,\s*' + STR + r'\s*\)', src)]
    found += [(m.group(1).split(","), m.group(3) or "", True) for m in re.finditer(r'AddReadable\(\s*' + STR + r'\s*,\s*' + STR + r'(?:\s*,\s*' + STR + r')?', src)]
    readables.update({m.group(1): m.group(2) for m in re.finditer(r'AddReadable\(\s*' + STR + r'\s*,\s*' + STR, src)})
    return found


# ---------------------------------------------------------------- the world of the new content

NEW_ROOMS = {  # room file: (short description, {direction: destination room file})
    "village/fletchers_lane": ("Fletcher's Lane", {"square": "village/square", "yew": "village/strung_yew", "loft": "village/hedge_scholars_loft",
                                                    "shrine": "village/shrine", "yard": "village/practice_yard"}),
    "village/strung_yew": ("The Strung Yew", {"out": "village/fletchers_lane"}),
    "village/armourers_shed": ("The Armourer's Shed", {"smithy": "village/ironoak"}),
    "village/shrine": ("The Shrine of the Quiet Hand", {"out": "village/fletchers_lane"}),
    "village/hedge_scholars_loft": ("The Hedge-Scholar's Loft", {"out": "village/fletchers_lane"}),
    "village/practice_yard": ("The Practice Yard", {"lane": "village/fletchers_lane", "road": "village/north_road"}),
}
PEOPLE = {  # npc file: room it stands in
    "wenna_farrow": "village/strung_yew", "oswy_thackett": "village/armourers_shed", "sister_merewyn": "village/shrine",
    "halbert_quill": "village/hedge_scholars_loft", "brann_odell": "village/practice_yard",
}
SHOPS = {  # shopkeeper file: (room, the words to talk to them by)
    "wenna_farrow": ("village/strung_yew", "wenna"), "oswy_thackett": ("village/armourers_shed", "oswy"),
    "halbert_quill": ("village/hedge_scholars_loft", "halbert"), "sister_merewyn": ("village/shrine", "merewyn"),
}
NEW_WEAPONS = ["felling_axe", "yew_longbow", "flanged_mace", "ironshod_staff", "boar_spear", "steel_dirk",
               "captains_sabre", "wardens_recurve", "shrine_blessed_mace", "hollow_oak_staff", "goblin_war_spear"]
NEW_ARMOUR = ["padded_jerkin", "leather_jerkin", "leather_cap", "scholars_robe"]
STARTER = {"axe": "woodcutter_axe", "bow": "hunting_bow", "mace": "novice_mace", "staff": "apprentice_staff", "spear": "training_spear",
           "dagger": "iron_dagger", "sword": "rusty_sword"}
ATTRIBUTE = {"ironshod_staff": ("Insight", 1), "hollow_oak_staff": ("Insight", 2), "shrine_blessed_mace": ("Spirit", 1), "scholars_robe": ("Insight", 1)}
# The values of section 2 of the spec: item -> (damage low, damage high, price or None, line, step)
SPEC = {
    "felling_axe": (15, 23, 65), "yew_longbow": (11, 16, 60), "flanged_mace": (12, 17, 55), "ironshod_staff": (9, 13, 55),
    "boar_spear": (11, 16, 50), "steel_dirk": (8, 12, 45), "captains_sabre": (14, 20, None), "wardens_recurve": (13, 19, None),
    "shrine_blessed_mace": (13, 18, None), "hollow_oak_staff": (10, 14, None), "goblin_war_spear": (13, 18, None),
}
SPEC_ARMOUR = {"padded_jerkin": (1, 20, "body"), "leather_jerkin": (2, 50, "body"), "leather_cap": (1, 25, "head"), "scholars_robe": (0, 40, "body")}


# ---------------------------------------------------------------- test content written here

PROBE = r'''// @inherit /std/thing
public class HxProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        foreach (var v in new[] { "tele", "gold", "killed", "smite", "hurt", "mk", "dropcount", "chron", "crit", "pledge", "sdice", "swait" })
            AddAction(v, Do);
    }
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var room = Environment(me)!;
        switch (QueryVerb())
        {
            case "tele": MoveObject(me, LoadObject(args.Trim())); me.Call("Look"); return true;
            case "gold": me.Call("AddGold", int.Parse(w[0])); Write("GOLD ok\n"); return true;
            case "sdice": Write("SDICE " + LoadObject("/secure/search").Call<bool>("SetTestRolls", w[0]) + "\n"); return true;
            case "swait": Write("SWAIT " + LoadObject("/secure/search").Call<bool>("SetTestWait", int.Parse(w[0])) + "\n"); return true;
            case "killed": Write("KILLED " + me.Call<bool>("QueryKilled", w[0]) + "\n"); return true;
            case "hurt": me.Call("TakeDamage", int.Parse(w[0]), null); Write("HURT ok\n"); return true;
            case "mk": MoveObject(CloneObject(w[0]), me); Write("GIVEN\n"); return true;
            case "pledge": { var problem = me.Call<string>("ApplyClass", w[0]); Write(problem == null ? "PLEDGED\n" : "REFUSED " + problem + "\n"); return true; }
            case "smite":
            {
                // damage from the character: the creature's own death code runs (experience, kill credit, Chronicle)
                var ob = Present(w[0], room);
                if (ob == null) { Write("SMITE none\n"); return true; }
                ob.Call("TakeDamage", 99999, me);
                Write("SMITTEN\n");
                return true;
            }
            case "crit":
            {
                // crit PATH: a new creature in the room, killed at once by damage from the character
                var ob = CloneObject(w[0]);
                MoveObject(ob, room);
                ob.Call("TakeDamage", 99999, me);
                Write("CRIT done\n");
                return true;
            }
            case "dropcount":
            {
                // dropcount MONSTER ITEM N: N creatures killed one after another; how many corpses held the item
                var hits = 0;
                for (var i = 0; i < int.Parse(w[2]); i++)
                {
                    var ob = CloneObject(w[0]);
                    MoveObject(ob, room);
                    ob.Call("Die", me);
                    foreach (var c in AllInventory(room).Where(o => o.Call<bool>("IsCorpse")).ToList())
                    {
                        if (AllInventory(c).Any(o => o.ProgramPath == w[1])) hits++;
                        MoveObject(c, Self);   // counted: out of the room, so that it is not counted again
                    }
                }
                Write("DROPS " + hits + "\n");
                return true;
            }
            case "chron":
            {
                var chron = LoadObject("/secure/chronicle");
                var has = chron.Call<bool>("HasOccurred", args.Trim());
                var holders = has ? chron.Call<string>("QueryOccurred", args.Trim(), "holders") ?? "" : "";
                Write("CHRON has=" + has + " holders=" + holders + "\n");
                return true;
            }
        }
        return false;
    }
}
'''

OATHBLADE = """// @inherit /std/weapon
public class HxOathblade : Weapon
{
    public override void Create()
    {
        Name = "Oathblade"; Ids.Add("oathblade"); ShortDesc = "an oathblade"; LongDesc = "A sword for warriors, sworn on.";
        DamageMin = 5; DamageMax = 6; Category = "sword"; Value = 5;
        WornBy.Add("warrior");
    }
}
"""

ADEPT = """// @inherit /std/pclass
public class HxAdept : PlayerClass
{
    public override void Create()
    {
        ClassName = "Adept"; Tier = "advanced"; Parent = "mage"; Title = "Adept"; Tagline = "Test advanced class."; Guild = "adepts";
    }
}
"""

CHAMPION = """// @inherit /std/pclass
public class HxChampion : PlayerClass
{
    public override void Create()
    {
        ClassName = "Champion"; Tier = "advanced"; Parent = "warrior"; Title = "Champion"; Tagline = "Test advanced class."; Guild = "champions";
    }
}
"""

# A wizard's object, written in a wizard's home, that tries every public door that could dress a player.
DRESSER = r'''// @inherit /std/thing
public class Dresser : Thing
{
    public override void Create() { Name = "dresser"; Ids.Add("dresser"); }
    public override string? Short() => "a dresser";
    public override void Init() { AddAction("dress", Dress); }
    private bool Dress(string args)
    {
        // CHANGED (sandbox): the dresser runs in its wizard's workshop and finds the victim wherever they are.
        var who = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "").ToLower() == args.Trim().ToLower());
        if (who == null) { Write("DRESS none\n"); return true; }
        var tries = new List<string>();
        foreach (var name in new[] { "jerkin", "oathblade", "robe" })
        {
            var item = Present(name, who);
            if (item == null) { tries.Add(name + "=missing"); continue; }
            foreach (var f in new[] { "Equip", "PutOn", "Wear", "Wield", "CmdWear", "CmdWield", "Dress", "SetEquipped", "EquipItem", "ForceEquip" })
            {
                try { tries.Add(f + "=" + (who.Call(f, item) ?? "null")); }
                catch (MudRuntimeException e) { tries.Add(f + "!" + e.Message.Length); }
            }
            try { tries.Add("Equip2=" + (who.Call("Equip", item, "body") ?? "null")); } catch (MudRuntimeException) { }
        }
        var body = who.Call<ObjectRef>("QueryEquipped", "body");
        var weapon = who.Call<ObjectRef>("QueryEquipped", "weapon");
        Write("DRESS " + string.Join(" ", tries.Where(t => !t.EndsWith("=null"))) + " body=" + (body == null ? "none" : body.Name) + " weapon=" + (weapon == null ? "none" : weapon.Name) + "\n");
        return true;
    }
}
'''

LAB = """// @inherit /std/room
public class HxLab : Room
{
    public override void Create()
    {
        ShortDesc = "Hollow Expansion Lab";
        LongDesc = "A bare room made for the hollow expansion test.";
        Exits["out"] = "/domains/hollow/village/square";
        Safe = false;
    }
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who != null && Interactive(who) && Present("probe", who) == null) MoveObject(CloneObject("/domains/hxtest/probe"), who);
    }
}
"""


# ---------------------------------------------------------------- helpers

def login(name, archetype=None):
    c = h.Client(name, archetype=archetype)
    c.read_until("> ")
    return c


def newchar(name, archetype=None, gold=0):
    """A fresh character; a visit to the lab hands out the probe, which is only a way of moving about and being paid."""
    c = login(name, archetype)
    c.command("lab")
    c.command("out")
    if gold:
        c.command(f"gold {gold}")
    return c


def score(c):
    text = c.command("score")
    m = re.search(r"(?:HP|Vitality)\s+(\d+)/(\d+)", text)
    dmg = re.search(r"Damage (\d+)-(\d+)", text)
    return {
        "text": text,
        "xp": int(re.search(r"Experience (\d+)", text).group(1)),
        "gold": int(re.search(r"Gold (\d+)", text).group(1)),
        "vit": int(m.group(1)) if m else -1,
        "vitmax": int(m.group(2)) if m else -1,
        "dmg": (int(dmg.group(1)), int(dmg.group(2))) if dmg else None,
        "hit": int(re.search(r"To hit \+(-?\d+)", text).group(1)),
        "armor": int(re.search(r"Armor (-?\d+)", text).group(1)),
    }


def attribute(text, name):
    """(total, base) of an attribute in the score line: 'Insight 7 (5)' is 7 with a base of 5; 'Insight 5' is 5 with a base of 5."""
    m = re.search(name + r" (\d+)(?: \((\d+)\))?", text)
    return int(m.group(1)), int(m.group(2) or m.group(1))


def count(c, word):
    """How many of a thing the inventory lists."""
    return c.command("i").lower().count(word.lower())


def goto(c, room):
    return c.command("tele " + HOLLOW + room)


def buy(c, who, word, price):
    before = score(c)["gold"]
    text = c.command("buy " + word)
    after = score(c)["gold"]
    truth(f"buy {word} from {who}: {price} gold", "You buy" in text and after == before - price, (text[-200:], before, after))
    return text


def quiet(c, seconds):
    return a.quiet(c, seconds)


# ---------------------------------------------------------------- the run

def run(work, mudlib, box):
    log_path = os.path.join(work, "driver.log")

    def logged():
        box[1].flush()
        return open(log_path).read()

    w = login("warden")
    check("the secret makes the warden a level-10 wizard", w.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(w, 0.4)
    # Searching (docs/specs/search.md): the dice are fixed at a natural 20 and the wait is off, so that every search finds what is there to find.
    w.command("clone /domains/hxtest/probe")
    check("the dice are fixed for the searches of the cache", w.command("sdice d20:20"), "SDICE True")
    check("and the wait after a failed search is off", w.command("swait 0"), "SWAIT True")

    # ===== 0. the files =====
    new_files = []
    for folder in ("village", "npc", "obj", "mob", "cellars"):
        for n in sorted(os.listdir(os.path.join(MUDLIB_SRC, "domains", "hollow", folder))):
            new_files.append(HOLLOW.lstrip("/") + folder + "/" + n[:-3])
    mine = [HOLLOW + "village/" + r.split("/")[1] for r in NEW_ROOMS] + [N + p for p in PEOPLE] + [O + i for i in NEW_WEAPONS + NEW_ARMOUR] + \
           [O + "shrine_blessing", O + "cache_bundle", O + "oak_root", M + "practice_dummy"]
    forbidden = ["Circle of Ash", "Philippa", "Reeve", "Goblin Blood", "goblin blood", "Goblin blood", "Stamina", "Mana", " HP", "hit points", "Hit points", "Hit Points"]
    bad = []
    for path in mine:
        src = source(path)
        # words in code identifiers (MaxHp, MaxHpBonus) are the engine's own; only player-facing strings are checked
        strings = " ".join(re.findall(STR, src))
        for word in forbidden:
            if word in strings:
                bad.append((path, word))
    truth(f"none of the {len(mine)} new files names the Circle of Ash, Philippa Reeve, Goblin Blood or HP / Stamina / Mana in a string", not bad, bad)
    truth("the new people are the five of the list", sorted(name_of(source(N + p)) for p in PEOPLE) ==
          sorted(["Wenna Farrow", "Oswy Thackett", "Sister Merewyn", "Halbert Quill", "Brann Odell"]))
    taken = []
    for folder, _, names in os.walk(os.path.join(MUDLIB_SRC, "domains")):
        for n in names:
            if not n.endswith(".cs"):
                continue
            path = os.path.join(folder, n)
            if any(path.endswith(x) for x in ("npc/" + p + ".cs" for p in PEOPLE)):
                continue
            for m in re.finditer(r'\bName = "([^"]+)"', open(path, encoding="utf-8").read()):
                if m.group(1) in ("Wenna Farrow", "Oswy Thackett", "Sister Merewyn", "Halbert Quill", "Brann Odell"):
                    taken.append((n, m.group(1)))
    truth("no other file in mudlib/domains has one of the five full names", not taken, taken)

    # ===== 1. a newcomer reads the signs =====
    ann = newchar("ann", gold=2000)
    text = ann.command("look")
    check("the square mentions the lane", text, "Fletcher's Lane", "lane")
    check("the square lists the lane among its exits", text, "lane")
    check("the notice post points at the new shops", ann.command("read notice"), "Fletcher's Lane", "Practice Yard", "shed", "Torren")
    ann.command("tele " + V + "guild")
    check("the guild's talk points at the new shops", "".join(ann.command("talk rowan") for _ in range(4)), "Fletcher's Lane", "practice yard")
    ann.command("tele " + V + "square")
    check("the merchant's talk points at the new shops", "".join(ann.command("talk merchant") for _ in range(3)), "Fletcher's Lane")
    check("Torren points at the new shops", ann.command("tele " + V + "ironoak") + ann.command("ask torren about better"), "felling axe", "Oswy")
    text = ann.command("look")
    check("the Iron Oak lists the shed door", text, "shed", "armourer's shed")

    # ===== 2. every new room, both ways, and every detail =====
    ann.command("tele " + V + "square")
    check("square -> lane", ann.command("lane"), "Fletcher's Lane", "Obvious exits")
    for room, (short, exits) in NEW_ROOMS.items():
        path = HOLLOW + room
        text = ann.command("tele " + path)
        check(f"{short}: arriving shows the room", text, short, "Obvious exits")
        src = source(path)
        long_desc = unescape(re.search(r'LongDesc\s*=\s*' + STR, src).group(1))
        check(f"{short}: full description", text, long_desc[:60])
        for direction in exits:
            check(f"{short}: {direction} is among the exits", text, direction)
        for direction, dest in exits.items():
            there = ann.command(direction)
            dest_short = NEW_ROOMS[dest][0] if dest in NEW_ROOMS else short_of(source(HOLLOW + dest))
            check(f"{short}: {direction} -> {dest_short}", there, dest_short, "Obvious exits", absent=("What?", "not open"))
            # and back the way the destination says
            back = [d for d, p in (NEW_ROOMS[dest][1].items() if dest in NEW_ROOMS else existing_exits(dest)) if p == room]
            truth(f"{dest_short} has an exit back to {short}", bool(back), (dest, room))
            if back:
                again = ann.command(back[0])
                check(f"{dest_short}: {back[0]} -> {short}", again, short, "Obvious exits", absent=("What?",))
            ann.command("tele " + path)
        # every detail, by every one of its names
        for names, body, readable in details_of(path):
            for n in names:
                got = ann.command("look at " + n)
                check(f"{short}: look at {n}", got, lead(body), absent=("You see no",))
            if readable:
                full = [t for k, t in readables.items() if k == ",".join(names)][0]
                got = ann.command("read " + names[0])
                check(f"{short}: read {names[0]}", got, lead(full, 20), absent=("You see no", "What?"))
    # the people are in the rooms
    for p, room in PEOPLE.items():
        src = source(N + p)
        text = ann.command("tele " + HOLLOW + room)
        check(f"{name_of(src)} is in {NEW_ROOMS[room][0]}", text, short_of(src) + " is here")
    # the two new connections of old rooms, and the way from the road to the yard
    ann.command("tele " + V + "gate")
    check("gate -> north road", ann.command("north"), "North Road")
    check("north road -> yard", ann.command("yard"), "The Practice Yard")
    check("yard -> north road", ann.command("road"), "North Road", "yard")
    ann.command("yard")
    check("yard -> lane", ann.command("lane"), "Fletcher's Lane")
    check("lane -> square", ann.command("square"), "Village Square")
    check("square -> east -> Iron Oak -> shed", ann.command("east") + ann.command("shed"), "The Armourer's Shed")
    check("shed -> smithy", ann.command("smithy"), "Ironoak Arms")
    check("Iron Oak -> square", ann.command("west"), "Village Square")
    check("the existing details of the old rooms still answer", ann.command("look at well") + ann.command("tele " + V + "north_road") + ann.command("read sign"),
          "broad well", "Welcome to Vincent's Hollow")
    ann.command("tele " + V + "north_road")
    check("a detail of the road leads to the yard", ann.command("look at fence"), "PRACTICE YARD")

    # ===== 3. every person: chat and topics =====
    for p, room in PEOPLE.items():
        src = source(N + p)
        who = name_of(src)
        word = p.split("_")[0] if p != "sister_merewyn" else "merewyn"
        ann.command("tele " + HOLLOW + room)
        chats = chats_of(N + p)
        for i in range(len(chats) + 1):
            line = chats[i % len(chats)]
            check(f"{who}: talk {i + 1}", ann.command("talk " + word), who + " says:", lead(line, 24))
        for keys, line in topics_of(N + p):
            for key in (keys[0], keys[-1]):
                got = ann.command(f"ask {word} about {key}")
                check(f"{who}: ask about {key}", got, who + " says:", lead(line, 24), absent=("can't tell you",))
        check(f"{who}: an unknown topic gets a shrug", ann.command(f"ask {word} about zzzz"), "can't tell you")
        keys = " ".join(k for ks, _ in topics_of(N + p) for k in ks).lower()
        topic_text = " ".join(l for _, l in topics_of(N + p)).lower()
        # Teaching is no longer to come (docs/specs/skill-ranks.md): each names the `ranks` command it is taught by now.
        truth(f"{who} has topics for the trade, the village, the cellars, a rumour and teaching (and says how ranks are learned now: ranks)",
              all(k in keys for k in ("village", "cellar", "rumour", "teach")) and "{cmd}ranks{/}" in topic_text and
              any(k in keys for k in ("trade", "dummy", "blessing", "armour", "staff", "bow")), keys[:120])

    # ===== 4. shops: browse, buy, and never profitable to sell back =====
    shops = dict(SHOPS)
    shops["torren"] = ("village/ironoak", "torren")
    for p, (room, word) in shops.items():
        path = N + p
        stock = stock_of(path)
        who = name_of(source(path))
        ann.command("tele " + HOLLOW + room)
        text = ann.command("browse")
        shown = {}
        for line in text.splitlines():
            m = re.match(r"\s*(.+?)\s+(\d+) gold(?:\s+\(.*\))?\s*$", line)
            if m:
                shown[m.group(1).lower()] = int(m.group(2))
        for item, price in stock:
            isrc = source(item)
            if p == "torren" and item.split("/")[-1] not in ("felling_axe", "flanged_mace", "boar_spear", "steel_dirk"):
                continue
            truth(f"{who} lists {short_of(isrc)} at {price} gold", shown.get(short_of(isrc).lower()) == price, shown)
            value = int(num(isrc, "Value"))
            truth(f"{short_of(isrc)}: Value {value} is not above the price {price}, so selling it back is never a profit", value <= price)
            if p != "sister_merewyn":
                before = score(ann)["gold"]
                text = ann.command("buy " + name_of(isrc).lower())
                if "No gold has been taken" in text:   # a piece the adventurer's calling bars: the shop asks to be told twice
                    truth(f"{who} warns the adventurer about {short_of(isrc)} and takes no gold", score(ann)["gold"] == before, text[-200:])
                    text = ann.command("buy " + name_of(isrc).lower())
                after = score(ann)["gold"]
                truth(f"buy {short_of(isrc)} from {who}", "You buy" in text and after == before - price, (text[-200:], before, after))
        if p == "torren":
            continue
        if p == "sister_merewyn":
            check(f"{who} sells exactly the blessing", text, "blessing")
            continue
        truth(f"{who} sells exactly the {len(stock)} lines of the file", len([k for k in shown]) == len(stock), (shown, stock))
        check(f"{who} does not sell goblin blood", text, absent=("goblin",))
        # sell one back: pays half the value, which is less than the price
        item, price = stock[0]
        before = score(ann)["gold"]
        got = ann.command("sell " + name_of(source(item)).lower())
        after = score(ann)["gold"]
        truth(f"{who} pays {after - before} for a {short_of(source(item))}: less than the {price} it cost", "You sell" in got and 0 < after - before <= price // 2, (got, before, after))

    # ===== 5. the numbers of every weapon, wielded =====
    ann.command("tele " + V + "square")
    pack = ann.command("i").lower()
    for item in NEW_WEAPONS + NEW_ARMOUR:
        if short_of(source(O + item)).lower() not in pack:   # the found ones, and whatever was sold back above
            ann.command("mk " + O + item)
    ann.command("unwield")
    base = score(ann)
    truth("unarmed: no weapon bonus on the character to start from", base["dmg"] is not None, base["text"])
    for item in NEW_WEAPONS:
        src = source(O + item)
        short = short_of(src)
        name = name_of(src).lower()
        lo, hi, price = SPEC[item]
        truth(f"{short}: damage {lo}-{hi} in the file as in section 2 of the spec", (int(num(src, "DamageMin")), int(num(src, "DamageMax"))) == (lo, hi))
        cat = re.search(r'Category = "(\w+)"', src).group(1)
        starter = source(O + STARTER[cat])
        # the existing weapon of the line, one step down: the longsword for the sabre, else the starter of the line
        twin = source(O + "fine_iron_longsword") if item == "captains_sabre" else starter
        # the line's steps differ in damage only: same reaction modifier, recovery and to-hit as the existing weapon
        same = all(num(src, f) == num(twin, f) for f in ("ReactionMod", "Recovery", "ToHit"))
        truth(f"{short}: reaction modifier, recovery and to-hit as {short_of(twin)}", same, [(f, num(src, f), num(twin, f)) for f in ("ReactionMod", "Recovery", "ToHit")])
        if price is not None:
            truth(f"{short}: sold for {price} gold, as in section 2", int(num(src, "Value")) == price)
        text = ann.command("wield " + name)
        check(f"wield {short}", text, "You wield " + short)
        s = score(ann)
        truth(f"{short}: score shows damage {lo}-{hi}", s["dmg"] == (lo, hi), s["text"])
        truth(f"{short}: score shows to hit +{int(num(src, 'ToHit'))} over the unarmed", s["hit"] - base["hit"] == int(num(src, "ToHit")), (s["hit"], base["hit"]))
        rm = num(src, "ReactionMod")
        pat = re.escape(short) + r" \+" + str(rm) + r"(?![\d.])"
        if rm:
            truth(f"{short}: Reaction Time line shows +{rm}", re.search(pat, s["text"]) is not None, s["text"][-300:])
        else:
            truth(f"{short}: no reaction modifier in the Reaction Time line", short not in s["text"].split("Reaction Time")[-1].replace("\r", "").split("\n\n")[0], s["text"][-300:])
        if item in ATTRIBUTE:
            attr, bonus = ATTRIBUTE[item]
            total, basis = attribute(s["text"], attr)
            truth(f"{short}: wielded it adds {bonus} {attr} ({total} over a base of {basis})", total - basis == bonus, s["text"])
            ann.command("unwield")
            total, basis = attribute(score(ann)["text"], attr)
            truth(f"{short}: put away, {attr} is the base again", total == basis, (total, basis))
        # the appraisal and the description exist
        check(f"look at {short} in the pack", ann.command("look at " + name), lead(re.search(r'LongDesc = ' + STR, src).group(1), 30), absent=("You see no",))
    ann.command("unwield")

    # ===== 6. armour, worn =====
    ann.command("remove")
    rosa = newchar("rosa", "priest", gold=100)   # the Scholar's Robe is for Mages and Priests only: a priest wears it here
    for item in NEW_ARMOUR:
        wearer = rosa if item == "scholars_robe" else ann
        if wearer is rosa:
            rosa.command("mk " + O + item)
        src = source(O + item)
        short = short_of(src)
        av, price, slot = SPEC_ARMOUR[item]
        truth(f"{short}: armour {av}, {slot}, {price} gold as in section 3", (int(num(src, "ArmorValue")), int(num(src, "Value"))) == (av, price) and f'Slot = "{slot}"' in src)
        before = score(wearer)
        text = wearer.command("wear " + name_of(src).lower())
        check(f"wear {short}", text, "You put on " + short)
        s = score(wearer)
        truth(f"{short}: score shows armor +{av}", s["armor"] - before["armor"] == av, (s["armor"], before["armor"]))
        check(f"{short}: listed among the equipment", s["text"], short)
        if item in ATTRIBUTE:
            attr, bonus = ATTRIBUTE[item]
            total, basis = attribute(s["text"], attr)
            truth(f"{short}: wearing it adds {bonus} {attr}", total - basis == bonus, s["text"])
        wearer.command("remove " + name_of(src).lower())
        after = score(wearer)
        truth(f"{short}: taken off, armor is as before", after["armor"] == before["armor"], (after["armor"], before["armor"]))
        if item in ATTRIBUTE:
            attr, bonus = ATTRIBUTE[item]
            truth(f"{short}: taken off, {attr} is the base again", attribute(after["text"], attr)[0] == attribute(after["text"], attr)[1])
    truth("the jerkin and robe name their callings in the files (the rule itself is tested in section 7b)",
          'NotWornBy.Add("mage")' in source(O + "leather_jerkin") and 'WornBy.Add("mage")' in source(O + "scholars_robe") and 'WornBy.Add("priest")' in source(O + "scholars_robe"))

    # ===== 7. who may wield what: the callings =====
    for archetype, ok_words, refused_words in (
            ("scout", ["yew longbow", "boar spear", "steel dirk"], ["flanged mace", "felling axe", "ironshod staff"]),
            ("priest", ["flanged mace", "ironshod staff"], ["yew longbow", "felling axe", "steel dirk"]),
            ("mage", ["ironshod staff", "steel dirk"], ["flanged mace", "boar spear", "yew longbow"]),
            ("warrior", ["felling axe", "captain's sabre", "yew longbow"], [])):
        c = newchar("t" + archetype, archetype, gold=500)
        for word in set(ok_words + refused_words):
            c.command("mk " + O + word.replace("captain's ", "captains_").replace(" ", "_").replace("sabre", "sabre"))
        for word in ok_words:
            check(f"a {archetype} may wield the {word}", c.command("wield " + word), "You wield")
        for word in refused_words:
            check(f"a {archetype} may not wield the {word}", c.command("wield " + word), "not trained to wield")
        if archetype == "priest":
            c.command("unwield")
            c.command("mk " + O + "scholars_robe")
            check("a priest may wear the scholar's robe", c.command("wear robe"), "You put on a scholar's robe")
            total, basis = attribute(score(c)["text"], "Insight")
            truth("and gains 1 Insight", total - basis == 1, (total, basis))
        if archetype == "mage":
            c.command("mk " + O + "scholars_robe")
            check("a mage may wear the scholar's robe", c.command("wear robe"), "You put on a scholar's robe")
            c.command("mk " + O + "padded_jerkin")
            check("a mage may wear the padded jerkin", c.command("wear padded jerkin"), "You put on a padded jerkin")

    # ===== 7b. armour by calling (docs/specs/hollow-expansion.md, "Armour by calling") =====
    # Leather Jerkin: not worn by a Mage. Scholar's Robe: worn by Mage and Priest only. Refused in words, shown in the description
    # and in the shop's list, warned about before gold is taken, an advanced class counts as its calling, an old save keeps a worn
    # piece, weapons take the same fields, and no wizard's object can dress a player in a forbidden piece.
    rule = {"leather_jerkin": {"warrior": True, "scout": True, "priest": True, "mage": False, "adventurer": True},
            "scholars_robe": {"warrior": False, "scout": False, "priest": True, "mage": True, "adventurer": False}}
    note = {"leather_jerkin": "Mages cannot wear it.", "scholars_robe": "Only Mages and Priests can wear it."}
    callers = {}
    for archetype in ("warrior", "scout", "priest", "mage", "adventurer"):
        c = newchar("cal" + archetype, archetype, gold=300)
        callers[archetype] = c
        for item in ("leather_jerkin", "scholars_robe"):
            src = source(O + item)
            short = short_of(src)
            c.command("mk " + O + item)
            out = c.command("wear " + name_of(src).lower())
            if rule[item][archetype]:
                check(f"a {archetype} may wear the {short}", out, "You put on " + short)
                c.command("remove " + name_of(src).lower())
            else:
                check(f"a {archetype} is refused the {short}, in words", out, f"You cannot wear {short}.", note[item], absent=("You put on",))
                truth(f"and the {archetype} is not wearing it", short not in c.command("score"))
    mage, warrior = callers["mage"], callers["warrior"]
    check("the jerkin's description says who may not wear it", warrior.command("look at jerkin"), "Mages cannot wear it.", "hard-boiled leather")
    check("the robe's description says who may wear it", warrior.command("look at robe"), "Only Mages and Priests can wear it.", "undyed wool")
    warrior.command("mk " + O + "padded_jerkin")
    check("a piece with no rule says nothing of callings", warrior.command("look at padded jerkin"), absent=("cannot wear it", "Only "))
    goto(mage, "village/armourers_shed")
    text = mage.command("browse")
    check("Oswy's list marks the leather jerkin as not for mages", text, "leather jerkin", "50 gold  (not for Mages)")
    truth("and not the padded jerkin or the cap", all("(" not in l for l in text.splitlines() if "padded jerkin" in l or "leather cap" in l), text)
    gold = score(mage)["gold"]
    out = mage.command("buy leather jerkin")
    check("buying the jerkin as a mage: a warning before any gold is taken", out, "You cannot wear a leather jerkin.", "No gold has been taken", "buy leather jerkin", absent=("You buy",))
    truth("the gold is still there", score(mage)["gold"] == gold, (score(mage)["gold"], gold))
    out = mage.command("buy leather jerkin")
    check("saying so again buys it", out, "You buy a leather jerkin for 50 gold")
    truth("and only then is the gold gone", score(mage)["gold"] == gold - 50, (score(mage)["gold"], gold))
    out = mage.command("buy padded jerkin")
    check("a mage buying a piece it can wear is not warned", out, "You buy a padded jerkin", absent=("No gold",))
    goto(warrior, "village/hedge_scholars_loft")
    text = warrior.command("browse")
    check("Halbert's list marks the robe as for mages and priests only", text, "scholar's robe", "40 gold  (Mages and Priests only)")
    truth("and not the staff", all("(" not in l for l in text.splitlines() if "ironshod staff" in l), text)
    gold = score(warrior)["gold"]
    out = warrior.command("buy robe")
    check("buying the robe as a warrior: warned, no gold taken", out, "You cannot wear a scholar's robe.", "Only Mages and Priests can wear it.", "No gold has been taken", absent=("You buy",))
    truth("the warrior still has its gold", score(warrior)["gold"] == gold)
    check("and buys it when asked again", warrior.command("buy robe"), "You buy a scholar's robe for 40 gold")
    goto(callers["priest"], "village/hedge_scholars_loft")
    check("a priest buying the robe is not warned", callers["priest"].command("buy robe"), "You buy a scholar's robe", absent=("No gold",))

    # An advanced class counts as the calling it grew from.
    adept = newchar("caladept", "mage", gold=100)
    check("a mage takes an advanced class of the mage's", adept.command("pledge hx_adept"), "PLEDGED")
    adept.command("mk " + O + "leather_jerkin")
    adept.command("mk " + O + "scholars_robe")
    check("an advanced mage is still refused the jerkin", adept.command("wear jerkin"), "Mages cannot wear it.", absent=("You put on",))
    check("and still may wear the robe", adept.command("wear robe"), "You put on a scholar's robe")
    champ = newchar("calchamp", "warrior", gold=100)
    check("a warrior takes an advanced class of the warrior's", champ.command("pledge hx_champion"), "PLEDGED")
    champ.command("mk " + O + "scholars_robe")
    champ.command("mk " + O + "leather_jerkin")
    check("an advanced warrior is refused the robe", champ.command("wear robe"), "Only Mages and Priests can wear it.", absent=("You put on",))
    check("and wears the jerkin", champ.command("wear jerkin"), "You put on a leather jerkin")

    # The same fields work for a weapon.
    check("a scout may not wield a warriors-only sword", (callers["scout"].command("mk " + T + "oathblade") + callers["scout"].command("wield oathblade")),
          "You cannot wield an oathblade. Only Warriors can wield it.", absent=("You wield",))
    warrior.command("mk " + T + "oathblade")
    check("a warrior may", warrior.command("wield oathblade"), "You wield an oathblade")
    check("the weapon's description says so", warrior.command("look at oathblade"), "Only Warriors can wield it.")

    # A piece worn when the rule arrived (a save from before it) stays worn until it is taken off.
    olds = newchar("calolds", "mage")
    olds.command("save")
    olds.command("quit")
    time.sleep(0.8)
    path = None
    for root, _, files in os.walk(os.path.join(work, "save")):
        for f in files:
            if f.startswith("calolds") and f.endswith(".json"):
                path = os.path.join(root, f)
    truth("the old character's save file is there", path is not None)
    if path:
        with open(path) as f:
            saved = json.load(f)
        saved["carried"] = list(saved.get("carried", [])) + [O + "leather_jerkin"]
        saved["carriedEquipped"] = list(saved.get("carriedEquipped", [])) + [len(saved["carried"]) - 1]
        with open(path, "w") as f:
            json.dump(saved, f)
        olds = login("calolds")
        check("an old save with the forbidden jerkin worn: it is still worn at login", olds.command("score"), "body: a leather jerkin")
        check("it is taken off", olds.command("remove jerkin"), "You take off a leather jerkin")
        check("and may not be put back on", olds.command("wear jerkin"), "Mages cannot wear it.", absent=("You put on",))
        olds.command("quit")

    # No wizard's object can dress a player in a forbidden piece through a public method.
    os.makedirs(os.path.join(mudlib, "wiz", "warden"), exist_ok=True)
    h.write(mudlib, "/wiz/warden/dresser", DRESSER)
    victim = newchar("caldupe", "mage")
    victim.command("mk " + O + "leather_jerkin")
    victim.command("mk " + O + "scholars_robe")
    victim.command("mk " + T + "oathblade")
    h.write(mudlib, "/wiz/warden/den", "// @inherit /std/room\npublic class WardenDen : Room { public override void Create() { ShortDesc = \"Den\"; LongDesc = \"A workshop.\"; } }\n")
    w.command("goto /wiz/warden/den")  # CHANGED (sandbox): a wizard's code runs in their workshop
    victim.command("lab")
    check("the dresser is cloned", w.command("clone ~/dresser"), "dresser")
    out = w.command("dress caldupe")
    w.command("goto " + T + "lab")
    check("the dresser ran", out, "DRESS ")
    check("no public method dresses a mage in the jerkin or the blade (it keeps its own staff)", out, "body=none", "weapon=/domains/hollow/obj/apprentice_staff", absent=("oathblade#", "leather_jerkin#"))
    truth("and the mage is wearing nothing it was not given", "body:" not in victim.command("score") and "jerkin" not in victim.command("score"), victim.command("score"))
    check("a plain wear by the player is still refused", victim.command("wear jerkin"), "Mages cannot wear it.")
    for c in list(callers.values()) + [adept, champ, victim]:
        c.command("quit")

    # ===== 8. the Practice Yard's dummy =====
    hero = newchar("hero", gold=100)
    hero.command("tele " + V + "practice_yard")
    check("the yard lists the dummy", hero.command("look"), "training dummy")
    check("look at the dummy", hero.command("look at dummy"), "man-shaped dummy")
    check("consider the dummy", hero.command("consider dummy"), "training dummy", absent=("What?",))
    before = score(hero)
    check("verbose on", hero.command("verbose on"), "Verbose is now on")
    hero.command("unwield")
    text = hero.command("kill dummy")
    check("the dummy can be attacked", text, "You attack a training dummy")
    got = hero.read_until("damage", timeout=25)
    check("a blow on the dummy shows its damage", got, "(+")
    got += quiet(hero, 14)
    check("the dummy does not strike back", got, absent=("misses you", "hits you", "attacks you", "(-"))
    mid = score(hero)
    truth("the character's Vitality is untouched after the fight", mid["vit"] == before["vit"], (mid["vit"], before["vit"]))
    hero.command("tele " + V + "practice_yard")
    for _ in range(3):
        hero.command("smite dummy")
    after = score(hero)
    truth("killing the dummy with a blow of 99999: no experience", after["xp"] == before["xp"], (after["xp"], before["xp"]))
    check("the dummy is still there", hero.command("look"), "training dummy", absent=("corpse",))
    check("and no corpse of it is listed", hero.command("look"), absent=("corpse",))
    check("no kill credit for the dummy", hero.command("killed " + M + "practice_dummy"), "KILLED False")
    check("no Chronicle entry for the dummy", hero.command("chron kill.ordinary|practice_dummy"), "has=False")
    truth("no gold from the dummy", after["gold"] == before["gold"], (after["gold"], before["gold"]))
    stat = w.command("goto " + V + "practice_yard") + w.command("stat dummy")
    truth("the dummy is whole again after the blows: its Vitality is its maximum", re.search(r"(\d+)/(\d+)", stat) is not None and
          re.search(r"(?:HP|Vitality)\s+(\d+)\s*/\s*(\d+)", stat) is not None and
          re.search(r"(?:HP|Vitality)\s+(\d+)\s*/\s*(\d+)", stat).group(1) == re.search(r"(?:HP|Vitality)\s+(\d+)\s*/\s*(\d+)", stat).group(2), stat[:600])
    hero.command("tele " + V + "square")

    # ===== 9. loot: the rare drops =====
    probe_room = newchar("luke")
    probe_room.command("tele " + T + "lab")
    for monster, item, expect in (("goblin_elite_guard", "captains_sabre", 10), ("goblin_warrior", "goblin_war_spear", 6)):
        loot = [(m.group(1), int(m.group(2))) for m in re.finditer(r'AddLoot\("([^"]+)",\s*(\d+)\)', source(M + monster))]
        truth(f"{monster}: drops {item} at {expect} percent in the file", (O + item, expect) in loot, loot)
        hits, seen = 0, True
        for _ in range(6):
            text = probe_room.command(f"dropcount {M}{monster} {O}{item} 50", timeout=60)
            m = re.search(r"DROPS (\d+)", text)
            seen = seen and m is not None
            hits += int(m.group(1)) if m else 0
        truth(f"{monster}: {hits} of 300 corpses held a {item} ({expect} percent expected)", seen and 1 <= hits <= 300 * expect // 100 * 3, text[-200:])

    # ===== 10. the finds: once, and not twice =====
    finder = newchar("fiona", gold=0)
    finder.command("tele " + CELLARS + "hidden_cache")
    check("the Hidden Cache hides the bundle until it is searched for", finder.command("look"), "Rotted sacking", absent=("oilcloth",))
    finder.send("search")
    check("a search finds the bundle", a.quiet(finder, 6.5), "a long bundle wrapped in greased oilcloth")
    check("the Hidden Cache shows the bundle", finder.command("look"), "oilcloth")
    check("look at the bundle", finder.command("look at bundle"), "greased oilcloth")
    text = finder.command("get bundle")
    check("the first character takes the Warden's Recurve from the bundle", text, "warden's recurve")
    check("it is in the pack", finder.command("i"), "warden's recurve")
    check("the bundle now reads as taken", finder.command("look at bundle"), "folded open")
    check("the room lists the taken bundle as a sentence", finder.command("look"), "A long oilcloth wrapping lies folded open here.", absent=("folded open is here",))
    check("a second try finds nothing more", finder.command("get bundle"), "already")
    truth("only one recurve in the pack", count(finder, "warden's recurve") == 1, finder.command("i"))
    # Every obvious verb and every noun of the find's line takes it once it is found (a fresh character each time: a find is taken once). They search together.
    phrases = (("ada", "open bundle"), ("beth", "unwrap bundle"), ("cora", "untie bundle"), ("dina", "take oilcloth"), ("elsa", "get oilcloth"),
               ("faye", "untie cord"), ("gina", "pick up bundle"), ("hana", "open oilcloth"), ("iris", "take cord"), ("jana", "get long bundle"), ("kira", "unroll bundle"))
    crowd = []
    for who, phrase in phrases:
        c = newchar(who, gold=0)
        c.command("tele " + CELLARS + "hidden_cache")
        crowd.append((c, phrase))
    for c, _ in crowd:
        c.send("search")
    for c, phrase in crowd:
        a.quiet(c, 6.5)
        check(f"the bundle answers to '{phrase}'", c.command(phrase), "warden's recurve", absent=("What?",))
        c.command("quit")
    calm_bats(w)
    finder.command("tele " + CELLARS + "mushroom_cavern")
    check("the Mushroom Cavern shows the root", finder.command("look"), "pale root")
    text = finder.command("get root")
    check("the character takes a Staff of the Hollow Oak from the root", text, "staff of the Hollow Oak")
    check("a second try finds nothing more", finder.command("take root"), "already")
    truth("only one staff in the pack", count(finder, "staff of the Hollow Oak") == 1, finder.command("i"))
    second = newchar("gwen", gold=0)
    second.command("tele " + CELLARS + "hidden_cache")
    second.send("search")
    a.quiet(second, 6.5)
    check("a second character takes a recurve of their own", second.command("get bundle"), "warden's recurve")
    calm_bats(w)
    second.command("tele " + CELLARS + "mushroom_cavern")
    check("and a staff of their own", second.command("get root"), "staff of the Hollow Oak")
    calm_bats(w)
    check("the first character, back again, is still refused", finder.command("tele " + CELLARS + "mushroom_cavern") and finder.command("get root"), "already")
    wiz_text = w.command("goto " + CELLARS + "hidden_cache") + w.command("get bundle")
    check("a wizard takes nothing and leaves no mark", wiz_text, "wizards take nothing")
    finder.command("tele " + V + "square")
    finder.command("wield warden's recurve")
    s = score(finder)
    truth("the Warden's Recurve found in the cache is 13-19", s["dmg"] == (13, 19), s["text"])
    finder.command("wield staff of the hollow oak")
    s = score(finder)
    truth("the Staff of the Hollow Oak found in the cavern is 10-14 and +2 Insight", s["dmg"] == (10, 14) and attribute(s["text"], "Insight")[0] - attribute(s["text"], "Insight")[1] == 2, s["text"])

    # ===== 11. the Shrine-Blessed Mace quest, start to end =====
    quin = newchar("quin", gold=0)
    quin.command("tele " + V + "shrine")
    check("the notice at the shrine names the blessing and the errand", quin.command("read notice"), "blessing", "six gold", "offering")
    check("ask about the offering", quin.command("ask merewyn about offering"), "wildflower", "clean water", "accept offering")
    check("report before accepting: nothing to report", quin.command("report"), "nothing to report")
    check("accept something else", quin.command("accept dragon"), "Which errand")
    text = quin.command("accept offering")
    check("accept the offering", text, "Quest accepted: The Quiet Offering", "wildflower")
    check("the quest is in the quest log", quin.command("quests"), "Active quests", "The Quiet Offering")
    check("accepting twice is refused politely", quin.command("accept offering"), "already taken")
    text = quin.command("report")
    check("report with nothing: lists what is missing", text, "wildflower", "clean water", "goblin scavenger")
    # buy the things in the village, as a player would
    quin.command("gold 25")
    quin.command("tele " + V + "healer")
    buy(quin, "Elspeth", "wildflower", 2)
    buy(quin, "Elspeth", "water", 1)
    quin.command("tele " + V + "shrine")
    text = quin.command("report")
    check("report with the flower and water but no scavenger slain: still missing the proof", text, "goblin scavenger", absent=("Quest complete",))
    truth("the flower and water were not taken", count(quin, "wildflower") == 1 and count(quin, "flask of clean water") == 1, quin.command("i"))
    # a goblin scavenger is slain by damage from the character (real death code: experience and kill credit)
    quin.command("tele " + T + "lab")
    xp0 = score(quin)["xp"]
    quin.command("crit " + M + "goblin_scavenger")
    quiet(quin, 0.5)
    check("the scavenger kill is credited to the character", quin.command("killed " + M + "goblin_scavenger"), "KILLED True")
    xp1 = score(quin)["xp"]
    quin.command("tele " + V + "shrine")
    gold1 = score(quin)["gold"]
    text = quin.command("report")
    check("report with everything: the quest is complete", text, "Quest complete: The Quiet Offering", "shrine-blessed mace", "25 gold")
    s = score(quin)
    truth("the reward is 100 experience and 25 gold", s["xp"] - xp1 >= 100 and s["gold"] == gold1 + 25, (xp1, s["xp"], gold1, s["gold"]))
    truth("the flower and water were taken from the pack", count(quin, "wildflower") == 0 and count(quin, "flask of clean water") == 0, quin.command("i"))
    check("the mace is in the pack", quin.command("i"), "shrine-blessed mace")
    check("the quest is in the quest log as done", quin.command("quests"), "Completed quests", "The Quiet Offering")
    check("report again: nothing more", quin.command("report"), "done everything")
    check("accept again: already done", quin.command("accept offering"), "already finished")
    check("a thank-you in the shrine keeper's talk", quin.command("talk merewyn"), "never been so still")
    check("wield the mace", quin.command("wield shrine-blessed mace"), "You wield a shrine-blessed mace")
    s = score(quin)
    truth("the quest mace is 13-18 and +1 Spirit", s["dmg"] == (13, 18) and attribute(s["text"], "Spirit")[0] - attribute(s["text"], "Spirit")[1] == 1, s["text"])
    check("the Chronicle holds the quest, with quin as its holder", quin.command("chron quest.local|the-quiet-offering"), "has=True", "uin")
    # a second character does the quest on their own
    rose = newchar("rose", gold=30)
    rose.command("tele " + V + "shrine")
    rose.command("accept offering")
    check("a second character's report is not helped by the first one's", rose.command("report"), "wildflower")
    check("the mace is not given twice", str(count(quin, "shrine-blessed mace")), "1")

    # ===== 12. the blessing =====
    pilgrim = newchar("piers", gold=40)
    pilgrim.command("tele " + V + "shrine")
    pilgrim.command("hurt 6")
    before = score(pilgrim)
    truth("the pilgrim is hurt", before["vit"] < before["vitmax"], before)
    check("the shrine lists the blessing at six gold", pilgrim.command("browse"), "blessing", "6 gold")
    text = pilgrim.command("buy blessing")
    check("buy the blessing", text, "You buy", "6 gold")
    after = score(pilgrim)
    truth("the blessing restores the pilgrim to full for six gold", after["vit"] == after["vitmax"] and after["gold"] == before["gold"] - 6, (before, after))
    check("the blessing is not an item in the pack", pilgrim.command("i"), absent=("blessing",))

    # ===== 13. the driver log =====
    log = logged()
    problems = [l for l in log.splitlines() if "ERROR" in l or "CLASH" in l or "not loaded" in l or "Unhandled" in l]
    check("the driver log has no errors", "ok" if not problems else "\n".join(problems[:10]), "ok")


def calm_bats(w):
    """The Mushroom Cavern's spore bats are aggressive; a wizard slays them so that the finder can take what is there in peace."""
    w.command("goto " + CELLARS + "mushroom_cavern")
    for _ in range(3):
        w.command("slay bat")


def existing_exits(room_file):
    """The exits of an existing room file, as [(direction, destination room file)]."""
    return [(m.group(1), m.group(2).replace(HOLLOW, "")) for m in re.finditer(r'Exits\["(\w+)"\]\s*=\s*"([^"]+)"', source(HOLLOW + room_file))]


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-hollowexp-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib, novice=True, lit=True)  # about the expansion's content, not the cellars' dark: every room lit (tests/smoke_test.py all_lit)
    os.makedirs(os.path.join(mudlib, "domains", "hxtest"))
    h.write(mudlib, T + "probe", PROBE)
    h.write(mudlib, T + "lab", LAB)
    h.write(mudlib, T + "oathblade", OATHBLADE)
    h.write(mudlib, "/classes/hx_adept", ADEPT)
    h.write(mudlib, "/classes/hx_champion", CHAMPION)
    h.edit(mudlib, "/domains/hollow/village/square", 'Exits["south"]', f'Exits["lab"] = "{T}lab";\n        Exits["south"]')
    # The test copy of the North Road has no wandering wolf, so that walking through it is the same every time.
    h.edit(mudlib, "/domains/hollow/village/north_road", 'AddEncounter("/domains/hollow/mob/wandering_wolf", 25);', "")
    box = [None, None]
    box[0], box[1] = a.start_driver(work, mudlib)
    started = time.time()
    try:
        run(work, mudlib, box)
        w = h.Client("warden")
        w.read_until("> ")
        w.command("shutdown")
        box[0].wait(timeout=20)
    finally:
        if box[0].poll() is None:
            box[0].kill()
        box[1].close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-5000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\nwhole test {time.time() - started:.0f} s")
    print(f"{CHECKS[0]} checks, {len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
