#!/usr/bin/env python3
"""End-to-end test of the crafting CONTENT of Vincent's Hollow and Oakenfell (work item C2).

craft_test.py tests the engine with sample content. This test plays the real game as ordinary (non-wizard) characters who
get no help with the crafting itself: they read the notice on the Hollow's square, look at the stations, buy from the village
suppliers, learn from the teachers, craft the ten launch recipes at their stations from the materials they bought, are refused
at the wrong station and without the recipe, discover Sweet Berry Juice and Blood Ink by `combine`, and skin, bleed and harvest
for themselves. What is checked against the files: every shop's stock and prices (parsed from the NPC files), every lesson and
its price, every effect (parsed from the item files and scaled by the quality the game reports).

The test's own fixtures (the only help the characters get) are: gold (a pile of coins that a level-10 warden drops, as other
tests do), test creatures and two nodes in a lab room off the square, where Goblin Blood can come from nothing but bleeding a
goblin; and a probe object that hurts a character, tires one, kills a creature, and fixes the dice (SetTestRolls) so that
quality is Common and the effects can be exact. The probe is craft_test's.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4830 python3 tests/craft_content_test.py
"""
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4830")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_test as a  # noqa: E402
import craft_sim as sim  # noqa: E402
import craft_test as ct  # noqa: E402
import smoke_test as h  # noqa: E402

T = "/domains/crafttest/"
HOLLOW = "/domains/hollow/"
V = HOLLOW + "village/"
CRAFT = HOLLOW + "obj/craft/"
OAK = "/domains/greenvale/oakenfell/"
SAMPLES = os.path.join(h.ROOT, "tests", "fixtures", "crafting", "samples", "domains", "crafttest")
MUDLIB_SRC = os.path.join(h.ROOT, "mudlib")
CHECKS = [0]
SETTLE = 3.4  # a craft takes 3 seconds; the next action may begin then
RESULT = ct.RESULT + ("nothing comes of it", "must be made", "You buy", "recover")
SCALE = {"poor": 0.6, "common": 1.0, "fine": 1.25, "excellent": 1.5, "masterwork": 1.8}


def check(label, text, *expected, absent=()):
    CHECKS[0] += 1
    h.check(label, text, *expected, absent=absent)


def truth(label, ok, detail=""):
    CHECKS[0] += 1
    h.check(label, "ok" if ok else "NOT OK " + str(detail), "ok")


# ---------------------------------------------------------------- reading the files the shops and teachers are made of

def source(mudpath):
    with open(os.path.join(MUDLIB_SRC, mudpath.lstrip("/") + ".cs"), encoding="utf-8") as f:
        return f.read()


def stock_of(npc):
    """[(record, price, quality)] from the AddStock lines of an NPC file."""
    return [(m.group(1), int(m.group(2)), int(m.group(3) or 1))
            for m in re.finditer(r'AddStock\(\s*"([^"]+)"\s*,\s*(\d+)\s*(?:,\s*(\d+))?\s*\)', source(npc))]


def lessons_of(npc):
    return [(m.group(1), int(m.group(2))) for m in re.finditer(r'AddLesson\(\s*"([^"]+)"\s*,\s*(\d+)\s*\)', source(npc))]


def field(src, name, default=0):
    m = re.search(r"\b" + name + r"\s*=\s*(\d+)\s*;", src)
    return int(m.group(1)) if m else default


def item_short(record):
    """The short description a shop lists for a stock record."""
    if record.startswith("/std/skin?p.kind="):
        return "a " + record.split("=")[1] + " skin"
    if record.startswith("/std/blood?p.kind="):
        return "a vial of " + record.split("=")[1] + " blood"
    return re.search(r'ShortDesc\s*=\s*"([^"]+)"', source(record)).group(1)


def item_word(record):
    """A word to type to buy it: the first of its Ids."""
    if record.startswith("/std/skin?p.kind="):
        return record.split("=")[1] + " skin"
    if record.startswith("/std/blood?p.kind="):
        return record.split("=")[1] + " blood"
    return re.search(r'Ids\.Add\("([^"]+)"\)', source(record)).group(1)


def scaled(amount, quality):
    return max(1, int(amount * SCALE[quality] + 0.5)) if amount > 0 else 0


def quality_of(text):
    """The quality word in 'You make a fine pot of ...' (Common has none)."""
    m = re.search(r"You make (?:an? |\d+ )?(poor|fine|excellent|masterwork)", text)
    return m.group(1) if m else "common"


# ---------------------------------------------------------------- test content written here

GOLD_PILE = """// @inherit /std/coins
public class CcGoldPile : Coins
{
    public override void Create() { Amount = 100; }
}
"""

LAB_INIT = """    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who != null && Interactive(who) && Present("probe", who) == null) MoveObject(CloneObject("/domains/crafttest/probe"), who);
    }
"""


def test_files():
    f = {}
    f[T + "probe"] = ct.PROBE
    f[T + "goldpile"] = GOLD_PILE
    f[T + "lab"] = ct.room("CcLab", "Craft Content Lab", {"out": V + "square"}, "", True, LAB_INIT)
    for who in ("ann", "bob", "dee"):  # one field each, so nobody meets another's corpse or its loot lock
        f[T + "lab_" + who] = ct.room("CcLab" + who.capitalize(), "Craft Content Field", {"out": V + "square"},
                                      f'        AddNode("{T}obj/berry_bush");\n        AddNode("{T}obj/herb_patch");\n', False)
    return f


# ---------------------------------------------------------------- helpers

def login(name):
    c = h.Client(name)
    c.read_until("> ")
    return c


def newchar(name):
    """A fresh character. A visit to the lab (and back) hands out the probe, which is only a way of moving about and hurting oneself."""
    c = login(name)
    c.command("lab")
    c.command("out")
    return c


def fund(w, c, room, piles):
    """Gold the way the other tests give it: the warden drops coins in the character's room and the character picks them up."""
    w.command("goto " + room)
    for _ in range(piles):
        w.command("clone " + T + "goldpile")
        w.command("drop coins")
    text = ""
    for _ in range(piles):  # each pile is picked up on its own
        text = c.command("get coins")
    return text


def gold(c):
    return a.score_of(c)["gold"]


def hp(c):
    return a.score_of(c)["hp"]


def hurt(c, n):
    text = c.command(f"hurt {n}")
    m = re.search(r"HP (\d+)/(\d+)", text)
    return int(m.group(1)), int(m.group(2))


def craft(c, what, *extra):
    return ct.act(c, "craft " + what, *RESULT, *extra, settle=SETTLE)


def buy(c, shop_name, noun, price, count=1):
    before = gold(c)
    text = c.command(f"buy {count} {noun}" if count > 1 else f"buy {noun}")
    truth(f"buy {count} {noun} from {shop_name}: {price * count} gold", "You buy" in text and gold(c) == before - price * count, (text[-200:], before, gold(c)))
    return text


def shop_check(c, w, npc_path, shop_name, expected_extra_absent=("goblin",)):
    """browse: the stock lines and their prices are exactly the AddStock lines of the file; then buy one of each at that price."""
    stock = stock_of(npc_path)
    text = c.command("browse")
    shown = {}
    for line in text.splitlines():
        m = re.match(r"\s*(.+?)\s+(\d+) gold\s*$", line)
        if m:
            shown[m.group(1).lower()] = int(m.group(2))
    truth(f"{shop_name}: browse lists exactly the {len(stock)} lines of the file", len(shown) == len(stock), (len(shown), len(stock), text))
    for record, price, quality in stock:
        short = item_short(record)
        check_label = f"{shop_name}: {short} costs {price}"
        truth(check_label, shown.get(short) == price, (short, shown))
    for absent in expected_extra_absent:
        check(f"{shop_name} sells no {absent}", text, absent=(absent,))
    return stock


def buy_each(c, shop_name, stock):
    """One of everything the crafting trade sells; the tavern's own food, drink and bed are checked by browse only."""
    for record, price, quality in stock:
        if record.startswith(CRAFT) or record.startswith("/std/"):
            buy(c, shop_name, item_word(record), price)


def price_in(npc, name):
    return [p for r, p, q in stock_of(npc) if r.endswith(name)][0]


def inv(w, name):
    return ct.inventory(w, name)


def have(w, name, record, quality=None):
    return ct.count_in(inv(w, name), record, quality)


# ---------------------------------------------------------------- the run

def run(work, mudlib, box):
    log_path = os.path.join(work, "driver.log")

    def logged():
        box[1].flush()
        return open(log_path).read()

    w = login("warden")
    check("secret makes the warden a level-10 wizard", w.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(w, 0.4)
    w.command("goto " + T + "lab")  # the lab hands the warden the probe
    check("the driver loaded all ten recipes and every material", logged(), "crafting: loaded 10 recipes, 33 materials")
    check("with no problems and no clashes", ct.dq(w, "problems") + ct.dq(w, "clashes"), absent=("not loaded", "unknown", "CLASH"))
    ids = "," + ct.dq(w, "ids") + ","
    for r in ("berry_juice", "sweet_berry_juice", "simple_bandage", "herbal_bandage", "minor_healing_salve", "minor_stamina_tonic",
              "treated_leather", "travelers_leather_wrap", "blood_ink", "hunters_poultice"):
        truth(f"recipe {r} is loaded", "," + r + "," in ids, ids)

    # ===== 1. a newcomer reads the notice =====
    ann = newchar("ann")  # a fresh character in the Hollow's square, starting gold 25
    text = ann.command("look")
    check("the square mentions the notice post", text, "notice post", "placard")
    text = ann.command("read notice")
    check("the notice tells a newcomer what can be done", text, "gathered", "harvest", "skin", "bleed", "combine", "craft", "help crafting")
    check("it says who teaches what and where", text, "Elspeth", "Dunstan", "Tobin", "Harland", "lessons", "learn", "browse")
    lines = [l for l in text.splitlines() if l.strip() and "THE CRAFTSFOLK" not in l] 
    truth("it is a notice of six to eight lines in all", 5 <= len(lines) <= 7, lines)
    check("read placard reads the same", ann.command("read placard"), "THE CRAFTSFOLK OF THE HOLLOW GIVE NOTICE")
    check("help crafting names the trainer commands", ann.command("help crafting"), "lessons", "learn", "combine", "never fails")
    check("help lists lessons and learn", ann.command("help", marker="Colour:"), "Crafting:", "lessons", "learn")
    a.drain(ann, 0.4)
    check("a new character knows no recipes", ann.command("recipes"), "You know no recipes yet")
    check("a new character starts with 25 gold", str(gold(ann)), "25")

    # ===== 2. the stations answer "look at" with one sensible answer =====
    ann.command("healer")
    for word in ("table", "herbalist table", "scrubbed table"):
        text = ann.command("look at " + word)
        check(f"look at {word} in the healer's cottage", text, "pale pine", "crafting station", absent=("You see no",))
        truth(f"and only one answer to {word}", text.count("crafting station") == 1, text)
    check("the cottage's other details still answer", ann.command("look at mortar"), "stone mortar")
    check("look at stove", ann.command("look at stove"), "kettle")
    ann.command("out")
    ann.command("tannery")
    for word in ("rack", "tanning rack", "frame"):
        check(f"look at {word} in the tannery", ann.command("look at " + word), "ash poles", "crafting station", absent=("You see no",))
    check("the tannery's hides still answer", ann.command("look at hides"), "Rabbit, wolf and deer skins")
    ann.command("out")
    ann.command("workshop")
    for word in ("bench", "workbench", "work bench"):
        check(f"look at {word} in the workshop", ann.command("look at " + word), "black oak", "crafting station", absent=("You see no",))
    check("the vice is still scenery", ann.command("look at vice"), "iron vice")
    ann.command("out")
    ann.command("south")
    for word in ("fire", "hearth", "fireplace", "fire place"):
        check(f"look at {word} in the tavern", ann.command("look at " + word), "fireplace", "crafting station", absent=("You see no",))
    check("the portrait is still scenery", ann.command("look at portrait"), "Vincent Holloway")
    ann.command("north")

    # ===== 3. the suppliers =====
    fund(w, ann, V + "square", 3)
    w.command("goto " + T + "lab")
    start_gold = gold(ann)
    truth("the character has 325 gold to shop with", start_gold == 325, start_gold)
    suppliers = [("elspeth", V + "healer", "healer"), ("dunstan", V + "tannery", "tannery"), ("tobin", V + "workshop", "workshop"),
                 ("harland", V + "tavern", "south")]
    sold = {}
    for npc, room, way in suppliers:
        ann.command(way)
        npc_path = HOLLOW + "npc/" + npc
        stock = shop_check(ann, w, npc_path, npc.capitalize())
        sold[npc] = stock
        buy_each(ann, npc.capitalize(), stock)
        ann.command("out" if npc != "harland" else "north")
    # What the stock covers: every material of the game but Goblin Blood.
    mats = sim.parse_materials(sim.Tree(MUDLIB_SRC))
    stocked = set(record for stock in sold.values() for record, price, quality in stock)
    fifteen = ("rabbit_skin", "wolf_skin", "deer_skin", "rabbit_blood", "wolf_blood", "blackberries", "blueberries", "healing_herb",
               "wildflower", "thistle", "honey", "flour", "clean_water", "cloth_strip", "empty_bottle")
    missing = [m for m in fifteen if mats[m].record not in stocked]
    truth("the four villagers sell all fifteen materials but Goblin Blood", not missing, missing)
    truth("and Goblin Blood is a scarce material nobody sells", mats["goblin_blood"].scarce and not any("goblin" in r for r in stocked), stocked)
    a.drain(ann, 0.3)
    # Extra purchases for the crafting below: 3 healing herbs and 4 cloth strips in all, 2 honey, 2 water, 2 blackberries.
    ann.command("healer")
    buy(ann, "Elspeth", "herb", price_in(HOLLOW + "npc/elspeth", "healing_herb"), 2)
    buy(ann, "Elspeth", "cloth", price_in(HOLLOW + "npc/elspeth", "cloth_strip"), 2)
    ann.command("out")
    ann.command("workshop")
    buy(ann, "Tobin", "cloth", price_in(HOLLOW + "npc/tobin", "cloth_strip"), 2)
    ann.command("out")
    ann.command("south")
    buy(ann, "Harland", "blackberries", 2)
    check("a shop refuses what it does not sell", ann.command("buy goblin blood"), "does not sell")
    check("nobody sells crafted outputs: Harland has no berry juice", ann.command("buy juice"), "does not sell")
    ann.command("north")

    # Oakenfell: Hesper's herbs cost a little more; Corwin sells the three tools.
    ann.command("tele " + OAK + "market/north_rows")
    hesper = stock_of(OAK + "market/npc/herbalist_hesper")
    village_price = {r: p for npc in sold for r, p, q in sold[npc]}
    shown = ann.command("browse")
    for record, price, quality in hesper:
        truth(f"Hesper sells {item_short(record)} at {price}, above the village's {village_price[record]}", price > village_price[record] and f"{price} gold" in shown, (price, shown))
    truth("Hesper sells herbs, wildflowers and thistle and nothing else", len(hesper) == 3 and sorted(item_word(r) for r, p, q in hesper) == ["flower", "herb", "thistle"], hesper)
    buy(ann, "Hesper", "herb", hesper[0][1])
    ann.command("tele " + OAK + "southgate/outfitter")
    corwin = [s for s in stock_of(OAK + "southgate/npc/outfitter_corwin") if s[0].startswith(CRAFT)]
    shown = ann.command("browse")
    truth("Corwin adds the three tools", sorted(item_word(r) for r, p, q in corwin) == ["kit", "knife", "sickle"], corwin)
    for record, price, quality in corwin:
        truth(f"Corwin's {item_short(record)} costs {price}, more than the village's {village_price[record]}", price > village_price[record] and f"{price} gold" in shown, (price, shown))
    check("Corwin still sells what he sold", shown, "minor healing potion", "trail ration", "coil of hempen rope", "tinderbox")
    buy(ann, "Corwin", "sickle", [p for r, p, q in corwin if "sickle" in r][0])

    # ===== 4. the teachers =====
    ann.command("tele " + V + "healer")
    check("Elspeth teaches in her cottage: lessons lists the four", ann.command("lessons"), "Elspeth Wortley can teach you", "Herbal Bandage", "Minor Healing Salve", "Minor Stamina Tonic", "Hunter's Poultice")
    check("her work topic says what she teaches and sells and how to ask", ann.command("ask elspeth about work"), "lessons", "learn herbal bandage", "browse")
    check("her goblin blood topic keeps the grumble and now says where it comes from",
          ann.command("ask elspeth about goblin blood"), "nobody in the Hollow can supply", "goblins", "bleeding kit", "fresh", "bleed corpse")
    check("her materials topic says what she sells", ann.command("ask elspeth about materials"), "browse", "Harland")
    text = ct.act(ann, "craft herbal bandage", *RESULT)
    check("without the recipe, no craft", text, "You do not know a recipe called 'herbal bandage'")
    check("a trainer refuses a recipe she does not teach", ann.command("learn blood ink"), "does not teach")
    check("a secret-ish one too", ann.command("learn sweet berry juice"), "does not teach")
    taught = {}
    for npc, room in (("elspeth", V + "healer"), ("dunstan", V + "tannery"), ("tobin", V + "workshop"), ("harland", V + "tavern")):
        ann.command("tele " + room)
        mine = lessons_of(HOLLOW + "npc/" + npc)
        listing = ann.command("lessons")
        for rid, price in mine:
            name = ct.dq(w, "info " + rid)
            name = re.search(r"name=([^;]*)", name).group(1)
            check(f"{npc} lists {name} at {price if price else 'free'}", listing, name, (f"{price} gold" if price else "free"))
            before = gold(ann)
            text = ann.command("learn " + name.lower())
            check(f"learn {name} from {npc}", text, f"You learn how to make {name}")
            truth(f"{name} cost {price} gold", gold(ann) == before - price, (before, gold(ann)))
            taught[rid] = price
        check(f"{npc}: learning one again", ann.command("learn " + re.search(r"name=([^;]*)", ct.dq(w, "info " + mine[0][0])).group(1).lower()), "already know")
    _, known, _ = ct.pinfo(w, "ann")
    for rid in ("berry_juice", "simple_bandage", "herbal_bandage", "minor_healing_salve", "minor_stamina_tonic", "treated_leather",
                "travelers_leather_wrap", "hunters_poultice"):
        truth(f"{rid} is known, taught", known.get(rid) == "taught", known)
    truth("exactly the eight teachable recipes are known", len(known) == 8 and "blood_ink" not in known and "sweet_berry_juice" not in known, known)
    truth("the lessons cost 57 gold in all", sum(taught.values()) == 57, taught)
    check("recipes lists them by station", ann.command("recipes"), "Anywhere", "Simple Bandage", "At a cooking fire", "Berry Juice", "At a herbalist table",
          "At a tanning rack", "Treated Leather", "At a workbench", "Traveler's Leather Wrap")

    # ===== 5. goblin blood from the lab, the only way =====
    check("rolls fixed high for gathering (Common results)", ct.dq(w, "rolls high"), "rolls=True")
    ann.command("tele " + T + "lab_ann")
    ann.command("spawn tc_goblin")
    ann.command("slaynow goblin")
    text = ct.act(ann, "bleed corpse", *RESULT, settle=ct.GATHER_SETTLE)
    check("ann bleeds a goblin with the kit she bought", text, "of goblin blood")
    truth("she now has Goblin Blood", have(w, "ann", "/std/blood?p.kind=goblin") >= 1, inv(w, "ann"))
    profs, _, _ = ct.pinfo(w, "ann")
    truth("a won growth roll (high dice) gave Bleeding one whole point, saved as 10 tenths", profs.get("bleeding") == 10, profs)
    truth("and the ladder is back at the first rung", ct.dq(w, "ladder ann bleeding") == "ladder=0", ct.dq(w, "ladder ann bleeding"))
    N = ct.numbers_now()
    truth("the shipped numbers hold the owner's ladder, repeat and easy factor",
          [N("growth.ladder", i) for i in range(5)] == [20, 25, 50, 75, 100] and N("growth.repeat_every") == 20 and N("growth.easy_factor") == 0.5, N.n.get("growth.ladder"))
    truth("and the fixed-gain numbers are gone from the file", not any(k in ct.sim.parse_numbers(ct.sim.Tree(ct.MUDLIB[0]))[0] for k in ("growth.near_gain", "growth.easy_gain", "growth.drag", "growth.min_factor")))
    check("rolls fixed to the middle for crafting", ct.dq(w, "rolls mid"), "rolls=True")

    # ===== 6. crafting the launch recipes at their stations, from bought materials =====
    results = {}

    def made(label, rid_path, text, qty=1):
        results[label] = quality_of(text)
        check(f"{label} is made", text, "You make")
        truth(f"{label}: one is in the pack", have(w, "ann", rid_path) >= qty, inv(w, "ann"))

    # wrong station / wrong place first
    ann.command("tele " + V + "tavern")
    check("a herbalist recipe at the cooking fire is refused", craft(ann, "herbal bandage"), "must be made at a herbalist table, and there is none here")
    check("a tanning recipe at the cooking fire is refused", craft(ann, "treated leather"), "must be made at a tanning rack, and there is none here")
    text = craft(ann, "berry juice")
    made("Berry Juice", CRAFT + "berry_juice", text)
    check("berry juice used the blackberries and clean water bought", text, "blackberries", "clean water")
    ann.command("north")
    check("a cooking recipe in the square is refused", craft(ann, "berry juice"), "must be made at a cooking fire")
    text = craft(ann, "simple bandage")
    made("Simple Bandage", CRAFT + "simple_bandage", text)
    truth("a Simple Bandage needs no station: made in the open square", "You make" in text, text)
    ann.command("healer")
    check("a cooking recipe at the herbalist table is refused", craft(ann, "berry juice"), "must be made at a cooking fire")
    check("a tanning recipe at the herbalist table is refused", craft(ann, "treated leather"), "must be made at a tanning rack")
    made("Herbal Bandage", CRAFT + "herbal_bandage", craft(ann, "herbal bandage"))
    made("Minor Healing Salve", CRAFT + "minor_healing_salve", craft(ann, "minor healing salve"))
    made("Minor Stamina Tonic", CRAFT + "minor_stamina_tonic", craft(ann, "minor stamina tonic"))
    made("Hunter's Poultice", CRAFT + "hunters_poultice", craft(ann, "hunters poultice"))
    ann.command("out")
    ann.command("tannery")
    check("a workbench recipe at the tanning rack is refused", craft(ann, "traveler's leather wrap"), "must be made at a workbench")
    text = craft(ann, "treated leather")
    made("Treated Leather", CRAFT + "treated_leather", text)
    ann.command("out")
    ann.command("workshop")
    check("a herbalist recipe at the workbench is refused", craft(ann, "herbal bandage"), "must be made at a herbalist table")
    text = craft(ann, "traveler's leather wrap")
    made("Traveler's Leather Wrap", CRAFT + "travelers_leather_wrap", text)
    check("the wrap used the treated leather", text, "treated leather", "cloth strip")
    truth("every Common ingredient and fixed dice gave Common results", all(q == "common" for q in results.values()), results)

    # ===== 7. the effects, when used =====
    ann.command("tele " + T + "lab_ann")
    mx = hurt(ann, 0)[1]
    D = min(40, mx - 3)
    truth("a new character has enough hit points to show the effects", D >= 20, mx)

    def effect(label, record, verb, noun, restore, stamina=0, over_time=None, q="common"):
        src = source(record)
        vit = scaled(field(src, "RestoreVitality"), q)
        sta = scaled(field(src, "RestoreStamina"), q)
        hurt(ann, D)
        ann.command("tired 0")
        text = ct.act(ann, f"{verb} {noun}", "recover", "Warmth", "can't", timeout=15)
        if vit:
            check(f"{label}: restores {vit} hit points", text, f"You recover {vit} hit points.")
            truth(f"{label}: hit points rose by {vit}", hp(ann) >= mx - D + vit, (hp(ann), mx, vit))
        if sta:
            check(f"{label}: restores {sta} Stamina", text, f"You recover {sta} Stamina.")
        return text, vit, sta

    effect("Berry Juice", CRAFT + "berry_juice", "drink", "berry juice", 8)
    a.quiet(ann, 1.2)
    effect("Simple Bandage", CRAFT + "simple_bandage", "apply", "simple bandage", 6)
    a.quiet(ann, 1.2)
    effect("Herbal Bandage", CRAFT + "herbal_bandage", "apply", "herbal bandage", 12)
    a.quiet(ann, 1.2)
    effect("Minor Stamina Tonic", CRAFT + "minor_stamina_tonic", "drink", "tonic", 0)
    a.quiet(ann, 1.2)
    # healing over time: the salve, then the poultice (a new one replaces the old; the poultice's total beats everything)
    for label, record, noun in (("Minor Healing Salve", CRAFT + "minor_healing_salve", "salve"), ("Hunter's Poultice", CRAFT + "hunters_poultice", "poultice")):
        src = source(record)
        per, ticks, secs = field(src, "HealPerTick"), field(src, "Ticks"), field(src, "TickSeconds", 5)
        hurt(ann, D)
        text = ct.act(ann, "apply " + noun, "Warmth", "can't", timeout=15)
        check(f"{label}: healing over time starts", text, f"you will recover {per} hit points every {secs} seconds, {ticks} times")
        check(f"{label} shows in score", ann.command("score"), label)
        text = a.quiet(ann, secs * 3 + 1.5)
        truth(f"{label}: three ticks arrived, {per} hit points each", text.count("soothes you") == 3 and f"(+{per} hit points)" in text, text)
        truth(f"{label}: hit points rose by {3 * per}", hp(ann) >= mx - D + 3 * per, (hp(ann), mx, per))
        truth(f"{label} heals {per * ticks} in all", per * ticks > 0)
    heal = {rid: field(source(CRAFT + rid), "HealPerTick") * field(source(CRAFT + rid), "Ticks") for rid in ("hunters_poultice", "minor_healing_salve")}
    potion = field(source(HOLLOW + "obj/healing_potion"), "HealAmount")
    instant = [field(source(CRAFT + r), "RestoreVitality") for r in ("berry_juice", "sweet_berry_juice", "simple_bandage", "herbal_bandage")]
    truth("the poultice heals more in total than any other crafted item", heal["hunters_poultice"] > max(max(instant), heal["minor_healing_salve"]), (heal, instant))
    truth("but no crafted item beats a healing potion at instant recovery (Common)", max(instant) < potion, (instant, potion))
    # the wrap is wearable
    check("the Traveler's Leather Wrap is wearable", ann.command("wear wrap"), "You put on a traveler's leather wrap")
    check("and shows as worn", ann.command("i"), "traveler's leather wrap", "worn")

    # ===== 8. the experiments: Sweet Berry Juice and Blood Ink =====
    bob = newchar("bob")
    fund(w, bob, V + "square", 2)
    w.command("goto " + T + "lab")
    bob.command("south")
    for noun, price, n in (("blackberries", 2, 3), ("water", 1, 3), ("honey", 3, 3)):
        buy(bob, "Harland", noun, price, n)
    check("the fire: combining the wrong things makes nothing", ct.act(bob, "combine water and honey", *RESULT), "nothing comes of it. Nothing is used up.")
    check("Berry Juice, a Known recipe, can be found by experiment", ct.act(bob, "combine blackberries and water", *RESULT, "discovered"), "You have discovered a recipe: Berry Juice!")
    check("Sweet Berry Juice: an obvious thing to try", ct.act(bob, "combine berry juice and honey", *RESULT, "discovered"), "You have discovered a recipe: Sweet Berry Juice!", "You make a flask of sweet berry juice")
    _, known, _ = ct.pinfo(w, "bob")
    truth("it is remembered as discovered", known.get("sweet_berry_juice") == "discovered" and known.get("berry_juice") == "discovered", known)
    check("a Discovered recipe makes no Chronicle entry", bob.command("chron craft.discover_rare|sweet_berry_juice"), "has=False")
    craft(bob, "berry juice")
    text = craft(bob, "sweet berry juice")
    check("and then it is simply crafted", text, "You make a flask of sweet berry juice")
    truth("bob holds two sweet berry juices", have(w, "bob", CRAFT + "sweet_berry_juice") == 2, inv(w, "bob"))
    q = quality_of(text)
    hurt(bob, 20)
    bob.command("tired 0")
    text = ct.act(bob, "drink sweet juice", "recover", timeout=15)
    src = source(CRAFT + "sweet_berry_juice")
    check("Sweet Berry Juice restores Vitality and Stamina", text, f"You recover {scaled(field(src, 'RestoreVitality'), 'common')} hit points.", f"You recover {scaled(field(src, 'RestoreStamina'), 'common')} Stamina.")
    bob.command("north")
    # Blood Ink
    bob.command("healer")
    check("Elspeth sells the wildflower", bob.command("buy 2 flower"), "You buy 2")
    bob.command("out")
    bob.command("tannery")
    buy(bob, "Dunstan", "kit", price_in(HOLLOW + "npc/dunstan", "bleeding_kit"))
    bob.command("out")
    bob.command("tele " + T + "lab_bob")
    ct.dq(w, "rolls high")
    for _ in range(2):
        bob.command("spawn tc_goblin")
        bob.command("slaynow goblin")
        ct.act(bob, "bleed goblin", *RESULT, settle=ct.GATHER_SETTLE)
    ct.dq(w, "rolls mid")
    truth("bob has at least two vials of Goblin Blood", have(w, "bob", "/std/blood?p.kind=goblin") >= 2, inv(w, "bob"))
    bob.command("tele " + V + "workshop")
    check("Blood Ink has not been found by anyone yet", bob.command("chron craft.discover_rare|blood_ink"), "has=False")
    bob.command("tele " + V + "tavern")
    check("the right mix at the wrong place makes nothing", ct.act(bob, "combine goblin blood and wildflower", *RESULT), "nothing comes of it. Nothing is used up.")
    check("and no recipe was learned", ct.pinfo(w, "bob")[1].get("blood_ink", "none"), "none")
    bob.command("tele " + V + "workshop")
    text = ct.act(bob, "combine goblin blood and wildflower", *RESULT, "discovered")
    check("at the workbench Blood Ink is found by experiment", text, "You have discovered a recipe: Blood Ink!", "You make a vial of blood ink")
    text = bob.command("chron craft.discover_rare|blood_ink")
    check("the first character to find Blood Ink earns a Chronicle entry", text, "has=True", "holders=bob ")
    check("naming the character and the recipe", text, "Bob worked out how to make Blood Ink")
    text = craft(bob, "blood ink")
    check("and Blood Ink can then be crafted", text, "You make a vial of blood ink")
    truth("bob holds two vials of blood ink", have(w, "bob", CRAFT + "blood_ink") == 2, inv(w, "bob"))
    check("nobody teaches it: Tobin does not", bob.command("learn blood ink"), "does not teach")
    # without the recipe: bob never learned the herbal bandage
    bob.command("tele " + V + "healer")
    check("without the recipe, no craft (bob never learned the herbal bandage)", craft(bob, "herbal bandage"), "You do not know a recipe called")

    # ===== 9. gathering for oneself: skin, bleed, harvest, then craft from what was gathered =====
    dee = newchar("dee")
    fund(w, dee, V + "square", 1)
    w.command("goto " + T + "lab")
    dee.command("tannery")
    buy(dee, "Dunstan", "knife", price_in(HOLLOW + "npc/dunstan", "skinning_knife"))
    buy(dee, "Dunstan", "kit", price_in(HOLLOW + "npc/dunstan", "bleeding_kit"))
    check("dee learns Treated Leather", dee.command("learn treated leather"), "You learn how to make Treated Leather")
    dee.command("out")
    dee.command("workshop")
    buy(dee, "Tobin", "sickle", price_in(HOLLOW + "npc/tobin", "harvest_sickle"))
    buy(dee, "Tobin", "primer", price_in(HOLLOW + "npc/tobin", "bandage_primer"))
    buy(dee, "Tobin", "cloth", price_in(HOLLOW + "npc/tobin", "cloth_strip"))
    text = dee.command("read primer")
    check("a recipe book sold by Tobin teaches the Simple Bandage", text, "TOBIN'S PRIMER ON BANDAGES", "You learn how to make Simple Bandage")
    check("reading it again teaches nothing", dee.command("read primer"), "nothing here you do not already know")
    check("the book's recipe is a recipe", dee.command("recipes"), "Simple Bandage")
    text = craft(dee, "simple bandage")
    check("and it is made with no station, here in the workshop", text, "You make a simple bandage")
    dee.command("out")
    dee.command("south")
    check("dee learns Berry Juice from Harland", dee.command("learn berry juice"), "You learn how to make Berry Juice")
    buy(dee, "Harland", "water", 1)
    dee.command("north")
    dee.command("tele " + T + "lab_dee")
    dee.command("spawn tc_rabbit")
    dee.command("slaynow rabbit")
    text = ct.act(dee, "skin rabbit", *RESULT, settle=ct.GATHER_SETTLE)
    check("dee skins a rabbit with the knife she bought", text, "rabbit skin")
    dee.command("spawn tc_wolf")
    dee.command("slaynow wolf")
    text = ct.act(dee, "bleed wolf", *RESULT, settle=ct.GATHER_SETTLE)
    check("and bleeds a wolf with the kit", text, "wolf blood")
    text = ct.act(dee, "harvest bush", *RESULT, settle=ct.GATHER_SETTLE)
    check("and harvests a berry bush with the sickle", text, "blackberries")
    text = ct.act(dee, "harvest patch", *RESULT, settle=ct.GATHER_SETTLE)
    check("and an herb patch", text, "healing herb")
    mine = inv(w, "dee")
    skins, bloods, berries = ct.count_in(mine, "/std/skin?p.kind=rabbit"), ct.count_in(mine, "/std/blood?p.kind=wolf"), ct.count_in(mine, CRAFT + "blackberries")
    truth("dee holds a rabbit skin, wolf blood and blackberries she gathered", skins >= 1 and bloods >= 1 and berries >= 1, mine)
    dee.command("tele " + V + "tannery")
    text = craft(dee, "treated leather")
    check("Treated Leather from the gathered skin and blood", text, "piece of treated leather", "rabbit skin", "wolf blood")
    truth("the gathered skin and blood were used", ct.count_in(inv(w, "dee"), "/std/skin?p.kind=rabbit") == skins - 1 and ct.count_in(inv(w, "dee"), "/std/blood?p.kind=wolf") == bloods - 1, inv(w, "dee"))
    dee.command("tele " + V + "tavern")
    text = craft(dee, "berry juice")
    check("Berry Juice from the gathered berries and bought water", text, "You make a flask of berry juice", "blackberries", "clean water")

    # ===== 10. Oakenfell's stations =====
    ann.command("tele " + OAK + "guild/crafters_court")
    for word, marker in (("rack", "iron-shod poles"), ("tanning rack", "iron-shod poles"), ("bench", "grey oak"), ("workbench", "grey oak")):
        check(f"look at {word} in the Crafters' Court", ann.command("look at " + word), marker, "crafting station", absent=("You see no",))
    check("the court's racks of cloth are still scenery", ann.command("look at racks"), "crimson", absent=("crafting station",))
    check("the court describes the workbench and rack", ann.command("look"), "workbench", "tanner's rack")
    ann.command("tele " + OAK + "southgate/silver_stag")
    for word in ("fire", "hearth", "fireplace"):
        check(f"look at {word} in the Silver Stag", ann.command("look at " + word), "wide enough to roast an ox", "crafting station", absent=("You see no",))
    check("the stag's head is still scenery", ann.command("look at stag"), "moth-bitten")
    # craft at the Oakenfell stations: Ann has the materials left from her purchases
    ann.command("tele " + OAK + "southgate/silver_stag")
    mine = inv(w, "ann")
    if ct.count_in(mine, CRAFT + "blackberries") >= 1 and ct.count_in(mine, CRAFT + "clean_water") >= 1:
        check("Berry Juice at the Silver Stag's cooking fire", craft(ann, "berry juice"), "You make a flask of berry juice")
    else:
        ann.command("tele " + V + "tavern")
        buy(ann, "Harland", "blackberries", 2)
        buy(ann, "Harland", "water", 1)
        ann.command("tele " + OAK + "southgate/silver_stag")
        check("Berry Juice at the Silver Stag's cooking fire", craft(ann, "berry juice"), "You make a flask of berry juice")
    ann.command("tele " + V + "tannery")
    buy(ann, "Dunstan", "rabbit skin", price_in(HOLLOW + "npc/dunstan", "skin?p.kind=rabbit"))
    buy(ann, "Dunstan", "wolf blood", price_in(HOLLOW + "npc/dunstan", "blood?p.kind=wolf"))
    ann.command("tele " + OAK + "guild/crafters_court")
    check("Treated Leather at the Crafters' Court tanning rack", craft(ann, "treated leather"), "You make a piece of treated leather")
    check("a Traveler's Leather Wrap at the Crafters' Court workbench", craft(ann, "traveler's leather wrap"), "You make a traveler's leather wrap")

    # ===== 11. nobody sells Goblin Blood or a crafted output =====
    scan_bad, scan_outputs, shops = [], [], 0
    outputs = {"berry_juice", "sweet_berry_juice", "simple_bandage", "herbal_bandage", "minor_healing_salve", "minor_stamina_tonic",
               "treated_leather", "travelers_leather_wrap", "blood_ink", "hunters_poultice"}
    for folder, _, names in os.walk(os.path.join(MUDLIB_SRC, "domains")):
        for n in names:
            if not n.endswith(".cs"):
                continue
            src = open(os.path.join(folder, n), encoding="utf-8").read()
            for m in re.finditer(r'Add(?:Rare)?Stock\(\s*"([^"]+)"', src):
                shops += 1
                rec = m.group(1)
                if "goblin" in rec.lower():
                    scan_bad.append((n, rec))
                if rec.startswith(CRAFT) and rec[len(CRAFT):] in outputs:
                    scan_outputs.append((n, rec))
    truth(f"no AddStock anywhere in mudlib/ names goblin blood (scanned {shops} stock lines)", shops > 40 and not scan_bad, scan_bad)
    truth("no shop stocks a crafted output (only materials, tools and the primer)", not scan_outputs, scan_outputs)
    for room in (V + "healer", V + "tannery", V + "workshop", V + "tavern", OAK + "market/north_rows", OAK + "southgate/outfitter"):
        ann.command("tele " + room)
        check(f"browse in {room.split('/')[-1]} lists no goblin blood", ann.command("browse"), absent=("goblin",))

    # ===== 12. the driver log =====
    log = logged()
    problems = [l for l in log.splitlines() if "ERROR" in l or "CLASH" in l or "not loaded" in l or "Unhandled" in l]
    check("the driver log has no errors", "ok" if not problems else "\n".join(problems[:10]), "ok")


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-craftcontent-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib, novice=True)
    # Test fixtures only: the creatures and nodes of the lab, and the lab itself. Not the sample recipes or materials.
    os.makedirs(os.path.join(mudlib, "domains", "crafttest", "mob"))
    os.makedirs(os.path.join(mudlib, "domains", "crafttest", "obj"))
    for sub, names in (("mob", ("tc_goblin", "tc_wolf", "tc_rabbit", "tc_boar")), ("obj", ("berry_bush", "herb_patch"))):
        for n in names:
            shutil.copy(os.path.join(SAMPLES, sub, n + ".cs"), os.path.join(mudlib, "domains", "crafttest", sub, n + ".cs"))
    for path, text in test_files().items():
        h.write(mudlib, path, text)
    h.edit(mudlib, "/domains/hollow/village/square", 'Exits["south"]', f'Exits["lab"] = "{T}lab";\n        Exits["south"]')
    ct.MUDLIB[0] = mudlib
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
