#!/usr/bin/env python3
"""End-to-end test of item instances (work item F1): quality, quantity and stacks.

Starts the driver against a temporary copy of the mudlib with three test-only stackable materials and a few tools
under /domains/test (reached by a door down from the Village Square of the copy), and plays real characters over TCP:
value by quality, names and plurals with quality, merging and splitting, get, drop, give, buy and sell with amounts,
the save record (and old saves of bare paths), lost property for stacks across a planned shutdown and a crash, the
corpse lock for stacks, the builder's API (create, take, count), the duplication and loss cases, and the sandbox: code
under /wiz cannot raise the quality or quantity of a real item.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4822 python3 tests/item_test.py
"""
import json
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4822")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402

SECRET = h.SECRET
TEST = "/domains/test/"
SKIN = TEST + "wolf_skin"
BERRY = TEST + "blackberry"
HERB = TEST + "healing_herb"
HOLLOW = "/domains/hollow/"

# ---------------------------------------------------------------- test content (not shipped)

MATERIALS = {
    "wolf_skin": '''// @inherit /std/item
public class TestWolfSkin : Item
{
    public override void Create()
    {
        Name = "wolf skin";
        Ids.Add("skin");
        Ids.Add("pelt");
        ShortDesc = "a wolf skin";
        LongDesc = "The grey skin of a wolf, roughly scraped.";
        Value = 4;
        Stackable = true;
    }
}
''',
    # A plural that is not name plus "s", and a stack that is eaten one at a time.
    "blackberry": '''// @inherit /std/consumable
public class TestBlackberry : Consumable
{
    public override void Create()
    {
        Name = "blackberry";
        Ids.Add("berry");
        Ids.Add("berries");
        ShortDesc = "a blackberry";
        Plural = "blackberries";
        LongDesc = "A ripe blackberry.";
        Value = 1;
        UseVerb = "eat";
        HealAmount = 1;
        Stackable = true;
    }
}
''',
    "healing_herb": '''// @inherit /std/item
public class TestHealingHerb : Item
{
    public override void Create()
    {
        Name = "healing herb";
        Ids.Add("herb");
        ShortDesc = "a healing herb";
        LongDesc = "A sprig of a bitter green herb.";
        Value = 3;
        Stackable = true;
    }
}
''',
}

TOOL = '''// @inherit /std/item
public class ItemTool : Item
{
    private const string Skin = "/domains/test/wolf_skin";
    public override void Create() { Name = "item tool"; Ids.Add("tool"); ShortDesc = "an item tool"; }
    public override void Init()
    {
        base.Init();
        foreach (var verb in new[] { "vals", "mk", "count", "splitone", "badmove", "takeof", "prop", "recs" }) AddAction(verb, Run);
    }
    private ObjectRef Where(string word) => word == "here" ? Environment(ThisPlayer()!)! : ThisPlayer()!;
    private bool Run(string args)
    {
        var who = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (QueryVerb())
        {
            case "vals":
                var line = new StringBuilder("vals");
                foreach (var b in new[] { 4, 1, 3, 0 })
                    for (var q = 0; q <= 5; q++) line.Append($" {b}/{q}={Item.ValueFor(b, q)}");
                Write(line + "\\n");
                return true;
            case "mk":
            {
                var path = w.Length > 2 ? (w[2].StartsWith("/") ? w[2] : "/domains/test/" + w[2]) : Skin;
                var into = w.Length > 3 ? Where(w[3]) : who;
                var made = CreateIn(path, int.Parse(w[0]), int.Parse(w[1]), into);
                Write("made " + (made == null ? "nothing" : made.Call<string>("Short")) + "\\n");
                return true;
            }
            case "count":
            {
                var path = w.Length > 1 ? "/domains/test/" + w[1] : Skin;
                var counts = CountIn(w.Length > 0 ? Where(w[0]) : who, path);
                Write("count" + string.Concat(counts.OrderBy(kv => kv.Key).Select(kv => $" q{kv.Key}={kv.Value}")) + " objects " + AllInventory(w.Length > 0 ? Where(w[0]) : who).Count(o => o.ProgramPath == path) + "\\n");
                return true;
            }
            case "splitone":
            {
                var stack = AllInventory(who).FirstOrDefault(o => o.ProgramPath == Skin && o.Call<int>("QueryQuantity") > 1);
                var part = stack?.Call<ObjectRef>("Split", 1);
                Write("split " + (part == null ? "refused" : part == stack ? "self" : "ok") + "\\n");
                return true;
            }
            case "badmove":
            {
                var stack = AllInventory(who).First(o => o.ProgramPath == Skin);
                var dead = CloneObject("/std/item");
                dead.Call("Vanish");
                var moved = MovePortion(stack, 1, dead);
                Write("badmove " + (moved == null ? "null" : "moved") + "\\n");
                return true;
            }
            case "takeof":
            {
                var path = w.Length > 1 ? "/domains/test/" + w[1] : Skin;
                var ok = TakeFrom(who, int.Parse(w[0]), o => o.ProgramPath == path);
                Write("takeof " + ok + "\\n");
                return true;
            }
            case "prop":
            {
                var stack = AllInventory(who).First(o => o.ProgramPath == Skin);
                var part = stack.Call<ObjectRef>("Split", 1)!;
                var ok = part.Call<bool>("SetProperty", "maker", args);
                Write("prop " + ok + " " + part.Call<string>("QueryProperty", "maker") + "\\n");
                return true;
            }
            case "recs":
                Write("recs " + string.Join(" ; ", AllInventory(who).Where(o => o.ProgramPath.StartsWith("/domains/test/") && o != Self).Select(o => RecordOf(o))) + "\\n");
                return true;
        }
        return false;
    }
}
'''

# Sandbox code: an item in a wizard's home that tries to change real items.
ROGUE = '''// @inherit /std/item
public class Rogue : Item
{
    public override void Create() { Name = "rogue"; Ids.Add("rogue"); ShortDesc = "a rogue charm"; }
    public override void Init() { base.Init(); AddAction("rogue", Run); }
    private bool Run(string args)
    {
        var who = ThisPlayer()!;
        var skin = AllInventory(who).First(o => o.ProgramPath == "/domains/test/wolf_skin");
        var r = new List<string>();
        r.Add("quality " + skin.Call<bool>("SetQuality", 5));
        r.Add("quantity " + skin.Call<bool>("SetQuantity", 999));
        r.Add("restore " + skin.Call<bool>("RestoreState", "q=5&n=999"));
        r.Add("property " + skin.Call<bool>("SetProperty", "maker", "rogue"));
        r.Add("split " + (skin.Call<ObjectRef>("Split", 1) == null ? "refused" : "done"));
        r.Add("receive " + skin.Call<bool>("ReceiveStack", 10));
        r.Add("direct " + (skin.As<Item>()?.SetQuality(5).ToString() ?? "none"));
        r.Add("create " + (CreateIn("/domains/test/wolf_skin", 50, 5, who) == null ? "refused" : "made"));
        var fresh = CloneObject("/domains/test/wolf_skin");
        r.Add("takesplit " + fresh.Call<bool>("TakeSplit", 5, 10, ""));
        fresh.Call("Vanish");
        var own = CloneObject("/wiz/wiz/rogue_mat");
        r.Add("own " + own.Call<bool>("SetQuality", 5) + " " + own.Call<bool>("SetQuantity", 7));
        MoveObject(own, who);
        Write("rogue " + string.Join(", ", r) + "\\n");
        return true;
    }
}
'''
ROGUE_MAT = '''// @inherit /std/item
public class RogueMat : Item
{
    public override void Create() { Name = "sandbox pebble"; Ids.Add("pebble"); ShortDesc = "a sandbox pebble"; Value = 1; Stackable = true; }
}
'''

SHOP = '''// @inherit /std/shopkeeper
public class TestFurrier : Shopkeeper
{
    public override void Create()
    {
        Name = "Furrier";
        Ids.Add("furrier");
        ShortDesc = "Fenna the furrier";
        AddStock("/domains/test/wolf_skin", 4);
        AddStock("/domains/test/wolf_skin", 4, 2);
        AddStock("/domains/hollow/obj/rusty_sword", 10);
    }
}
'''


def lab_room():
    return a.room("TestLab", "Item Lab", {"up": HOLLOW + "village/square"},
                  f'        AddSpawn("{TEST}furrier");\n', safe=True, reset=3000)


# ---------------------------------------------------------------- helpers

def save_of(work, name):
    with open(os.path.join(work, "save", "players", name + ".json")) as f:
        return json.load(f)


def write_save(work, name, data):
    with open(os.path.join(work, "save", "players", name + ".json"), "w") as f:
        json.dump(data, f, indent=2)


def gold(c):
    return a.score_of(c)["gold"]


def quit_(c):
    c.send("quit")
    c.read_until("Goodbye")


def login(name, check_text=None):
    c = h.Client(name)
    text = c.read_until("> ")
    return c, text


def to_lab(c):
    """From wherever a new or returning character is, walk to the lab (Square, then down)."""
    where = c.command("look")
    if "Guest Rooms" in where:
        c.command("d")
        c.command("north")
    return c.command("down")


# ---------------------------------------------------------------- the test

def main():
    work = tempfile.mkdtemp(prefix="lesmudos-items-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib, novice=True)
    os.makedirs(os.path.join(mudlib, "domains", "test"))
    for name, source in MATERIALS.items():
        h.write(mudlib, TEST + name, source)
    h.write(mudlib, TEST + "tool", TOOL)
    h.write(mudlib, TEST + "furrier", SHOP)
    h.write(mudlib, TEST + "lab", lab_room())
    h.edit(mudlib, HOLLOW + "village/square", 'Exits["south"]', f'Exits["down"] = "{TEST}lab";\n        Exits["south"]')

    box = [None, None]
    box[0], box[1] = a.start_driver(work, mudlib)
    try:
        run(work, mudlib, box)
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


def run(work, mudlib, box):
    check = h.check
    wiz, _ = login("wiz")
    check("setup: the first wizard", wiz.command("promote " + SECRET), "level 10 wizard")
    check("setup: the wizard reaches the lab", wiz.command("goto " + TEST + "lab"), "Item Lab", "Fenna the furrier is here.")
    check("setup: the tool", wiz.command("clone " + TEST + "tool"), "Cloned /domains/test/tool#")

    # ===== 4. value by quality: one table =====
    text = wiz.command("vals")
    check("value: base 4 at each quality is 2, 4, 6, 10, 16, 32", text, "4/0=2 4/1=4 4/2=6 4/3=10 4/4=16 4/5=32")
    check("value: base 1 rounds to at least 1 (Poor 0.5 is 1, Fine 1.5 is 2)", text, "1/0=1 1/1=1 1/2=2 1/3=3 1/4=4 1/5=8")
    check("value: base 3 rounds half away from zero (Fine 4.5 is 5, Excellent 7.5 is 8)", text, "3/0=2 3/1=3 3/2=5 3/3=8 3/4=12 3/5=24")
    check("value: a base value of 0 stays 0 at every quality", text, "0/0=0 0/1=0 0/2=0 0/3=0 0/4=0 0/5=0")

    # ===== 1, 2, 3, 6. creating stacks, names, merging =====
    check("api: CreateIn makes one Common skin", wiz.command("mk 1 1"), "made a wolf skin")
    check("api: CreateIn merges a second batch into the stack", wiz.command("mk 2 1"), "made 3 wolf skins")
    check("api: a different quality makes a separate stack", wiz.command("mk 2 2"), "made 2 fine wolf skins")
    check("api: one excellent skin takes 'an'", wiz.command("mk 1 3"), "made an excellent wolf skin")
    text = wiz.command("i")
    check("inventory: counts and quality prefixes; Common has none", text, "a wolf skin (x3)", "a fine wolf skin (x2)", "an excellent wolf skin", absent=("common",))
    check("api: CountIn counts by quality, one object per stack", wiz.command("count"), "count q1=3 q2=2 q3=1 objects 3")
    check("look at a stack: count and quality", wiz.command("look at fine wolf skin"), "The grey skin of a wolf", "There are 2 of them, of fine quality.")
    check("look at one: its quality", wiz.command("look at excellent skin"), "It is of excellent quality.")
    check("plural names find a stack", wiz.command("look at skins"), "The grey skin of a wolf")
    check("other names and plurals of them", wiz.command("look at pelts"), "The grey skin of a wolf")
    check("a quality word the item does not have finds nothing", wiz.command("look at poor skin"), "You see no 'poor skin' here.")
    check("typing 'common' finds the Common stack", wiz.command("look at common wolf skin"), "There are 3 of them, of common quality.")
    check("stat shows quality, quantity and unit value", wiz.command("stat fine wolf skin"), "Quality fine, quantity 2, stackable True, unit value 6")
    check("a plural override (blackberries)", wiz.command("mk 3 1 blackberry"), "made 3 blackberries")
    check("the override is typed too", wiz.command("look at blackberries"), "A ripe blackberry.", "There are 3 of them")
    check("eating from a stack uses one", wiz.command("eat blackberry"), "You eat a blackberry.")
    check("two are left", wiz.command("i"), "a blackberry (x2)")
    check("api: a non-stackable item made 3 times is 3 objects", wiz.command("mk 3 1 /domains/hollow/obj/rusty_sword"), "made a rusty sword")
    check("identical non-stackable items collapse into one line with a count", wiz.command("i"), "a rusty sword (x3)")

    # ===== 3. drop and get with amounts =====
    check("drop an amount: the stack splits", wiz.command("drop 2 wolf skin"), "You drop 2 wolf skins.")
    check("what is left in the hand", wiz.command("count"), "count q1=1 q2=2 q3=1")
    check("the room lists the dropped stack", wiz.command("look"), "A wolf skin (x2) is here.")
    check("drop by quality word", wiz.command("drop fine wolf skin"), "You drop a fine wolf skin.")
    check("drop one more Common: it merges with the stack on the floor", wiz.command("drop skin"), "You drop a wolf skin.")
    check("on the floor: one Common stack of 3, one Fine", wiz.command("count here"), "count q1=3 q2=1 objects 2")
    check("drop more than carried is refused, nothing moves", wiz.command("drop 5 fine skins"), "You have only 1.")
    check("still one fine skin carried", wiz.command("count"), "q2=1")
    check("get an amount from a stack", wiz.command("get 2 skins"), "You take 2 wolf skins.")
    check("asking for more than lies there is refused", wiz.command("get 3 skins"), "There are only 2 here.")
    check("get all of a name takes every stack of it", wiz.command("get all skins"), "You take a wolf skin.", "You take a fine wolf skin.")
    check("the floor is empty of skins", wiz.command("count here"), "objects 0")
    check("and they merged in the hand", wiz.command("count"), "count q1=3 q2=2 q3=1 objects 3")
    check("non-stackable: drop one of three swords", wiz.command("drop sword"), "You drop a rusty sword.")
    wiz.command("drop sword")
    check("non-stackable: get 2 takes two objects", wiz.command("get 2 swords"), "You take a rusty sword.\r\nYou take a rusty sword.")
    wiz.command("drop all swords")
    check("drop all of a name drops every one", wiz.command("i"), absent=("rusty sword",))
    wiz.command("drop all berries")
    text = wiz.command("get all")
    check("get all takes everything that can be taken", text, "You take a rusty sword.", "You take 2 blackberries.")
    check("get all took the three swords one by one", str(text.count("You take a rusty sword.")), "3")

    # ===== 5. give between players =====
    ann, text = login("ann")
    to_lab(ann)
    ben, _ = login("ben")
    to_lab(ben)
    a.drain(wiz)
    check("give by quality word", wiz.command("give 3 common skins to ann"), "You give 3 wolf skins to Ann.")
    a.drain(ann)
    text = ann.command("give 2 skins to ben")
    check("give: the giver's message", text, "You give 2 wolf skins to Ben.")
    check("give: the receiver's message", ben.read_until("gives you"), "Ann gives you 2 wolf skins.")
    check("give: the room's message", wiz.read_until("to Ben."), "Ann gives 2 wolf skins to Ben.")
    check("give: the receiver holds the stack", ben.command("i"), "a wolf skin (x2)")
    check("give: the giver keeps the rest", ann.command("i"), "a wolf skin")
    saved_ann = save_of(work, "ann")
    saved_ben = save_of(work, "ben")
    check("give saves the giver at once", json.dumps(saved_ann["carried"]), '"/domains/test/wolf_skin"', absent=("n=",))
    check("give saves the receiver at once, with the quantity", json.dumps(saved_ben["carried"]), '"/domains/test/wolf_skin?n=2"')
    wiz.command("give fine skin to ann")
    a.drain(ann)
    check("a fine skin given does not merge with a Common one", ann.command("i"), "a wolf skin", "a fine wolf skin")
    check("give to someone who is not here", ann.command("give skin to zed"), "There is no one called 'zed' here.")
    check("give to an NPC is refused", ann.command("give skin to furrier"), "does not take things from you", "only to other players")
    check("give to yourself", ann.command("give skin to ann"), "You already have it.")
    check("give more than you have", ann.command("give 4 skins to ben"), "You have only 2.")
    check("nothing moved", ann.command("i"), "a wolf skin", "a fine wolf skin")
    check("give needs 'to'", ann.command("give skin"), "Usage: give [NUMBER] THING to PLAYER")
    wiz.command("clone " + HOLLOW + "obj/travel_pass")
    check("the travel pass (a quest item) cannot be given", wiz.command("give travel pass to ann"), "You cannot give away a travel pass; it is too important.")
    check("give all keeps quest items back", wiz.command("give all travel passes to ann"), "You keep a travel pass; it is too important to give away.")
    check("ann did not receive it", ann.command("i"), absent=("travel pass",))
    wiz.command("drop pass")

    # ===== 3, 4. shops with amounts and quality =====
    g = gold(ann)
    check("browse: the fine stock is priced by quality (4 x 1.5 = 6)", ann.command("browse"), "A wolf skin", "A fine wolf skin", "    6 gold")
    check("buy several of a stackable item", ann.command("buy 3 wolf skins"), "You buy 3 wolf skins for 12 gold.")
    check("the purchase merges with the stack carried", ann.command("i"), "a wolf skin (x4)", "a fine wolf skin")
    check("buy a quality item by its quality word", ann.command("buy fine wolf skin"), "You buy a fine wolf skin for 6 gold.")
    check("the gold went: 12 + 6", str(gold(ann)), str(g - 18))
    check("several of a non-stackable item are refused", ann.command("buy 2 swords"), "You can buy that only one at a time.")
    check("unaffordable amounts are refused", ann.command("buy 50 skins"), "You cannot afford that. 50 cost 200 gold.")
    check("value of a stack: per item and in all", ann.command("value skins"), "would pay 2 gold each for 4 wolf skins, 8 gold in all.")
    check("value of a fine stack (6 / 2)", ann.command("value fine skins"), "would pay 3 gold each for 2 fine wolf skins, 6 gold in all.")
    g = gold(ann)
    check("sell part of a stack", ann.command("sell 3 skins"), "You sell a wolf skin (x3) to Furrier for 6 gold.")
    check("the shop pays for the quantity sold", str(gold(ann)), str(g + 6))
    check("the rest of the stack stays", ann.command("i"), "a wolf skin", "a fine wolf skin (x2)")
    check("sell more than you have", ann.command("sell 5 fine skins"), "You have only 2 that Furrier would buy.")
    check("sell a fine one: half of 6", ann.command("sell fine skin"), "You sell a fine wolf skin to Furrier for 3 gold.")
    wiz.command("mk 1 0")
    check("a poor skin is worth 2, so the shop pays 1", wiz.command("sell poor skin"), "You sell a poor wolf skin to Furrier for 1 gold.")
    check("shop help still refuses a quest item", (wiz.command("get pass"), wiz.command("sell pass"))[1], "will not buy that; it is too important.")
    wiz.command("drop pass")

    # ===== 6. the builder's API: take and count =====
    wiz.command("mk 4 1 healing_herb")
    check("api: TakeFrom refuses when there are too few, and takes nothing", wiz.command("takeof 9 healing_herb"), "takeof False")
    check("api: still 4 herbs", wiz.command("count me healing_herb"), "count q1=4")
    wiz.command("mk 2 2 healing_herb")
    check("api: TakeFrom across two stacks", wiz.command("takeof 5 healing_herb"), "takeof True")
    check("api: one herb left, of the second stack", wiz.command("count me healing_herb"), "count q2=1 objects 1")

    # ===== duplication and loss =====
    wiz.command("drop all skins")
    wiz.command("mk 3 1")
    check("split leaves two objects", (wiz.command("splitone"), wiz.command("count"))[1], "count q1=3 objects 2")
    check("a failed move after a split loses nothing and merges back", wiz.command("badmove"), "badmove null")
    check("after the failed move: still 3, still two objects (the split-off one merged back)", wiz.command("count"), "count q1=3 objects 2")
    wiz.command("save")
    check("saving while a stack is split writes both parts", json.dumps(save_of(work, "wiz")["carried"]), '"/domains/test/wolf_skin?n=2"', '"/domains/test/wolf_skin"')
    quit_(wiz)
    wiz, _ = login("wiz")
    check("merge on login: the two parts are one stack again, nothing lost or gained", wiz.command("count"), "count q1=3 objects 1")
    wiz.command("goto " + TEST + "lab")

    # properties
    check("properties: set on one split off", wiz.command("prop Ann & co=1"), "prop True Ann & co=1")
    check("a skin with a property does not merge with plain ones", wiz.command("count"), "count q1=3 objects 2")
    check("the record escapes & and =", wiz.command("recs"), "/domains/test/wolf_skin?p.maker=Ann %26 co%3D1", "/domains/test/wolf_skin?n=2")
    quit_(wiz)
    wiz, _ = login("wiz")
    check("properties survive the save", wiz.command("recs"), "/domains/test/wolf_skin?p.maker=Ann %26 co%3D1")
    check("and keep the stack apart after login", wiz.command("count"), "count q1=3 objects 2")
    wiz.command("goto " + TEST + "lab")

    # ===== the old save format =====
    olda, _ = login("olda")
    quit_(olda)
    data = save_of(work, "olda")
    check("a fresh character's save has an empty carried list", json.dumps(data["carried"]), "[]")
    data["carried"] = [SKIN, SKIN, HOLLOW + "obj/rusty_sword", TEST + "no_such_file", SKIN + "?q=9&n=-4&zz&p.=x"]
    data["carriedEquipped"] = [2]
    write_save(work, "olda", data)
    olda, text = login("olda")
    check("an old save (bare paths) loads", text, "Welcome back, Olda")
    text = olda.command("i")
    check("old records become items in the default state, merged on login", text, "a wolf skin (x3)", "a rusty sword (wielded)")
    check("a missing file is skipped, a malformed state is ignored", text, absent=("no_such", "legendary"))
    quit_(olda)
    check("saved again in the new format", json.dumps(save_of(work, "olda")["carried"]), '"/domains/test/wolf_skin?n=3"', '"/domains/hollow/obj/rusty_sword"')
    data = save_of(work, "olda")
    data["carried"] = [SKIN + "?q=2&n=2", SKIN + "?n=3&q=2", BERRY + "?n=2"]
    data["carriedEquipped"] = []
    write_save(work, "olda", data)
    olda, _ = login("olda")
    check("two saved stacks of one file and quality merge at login", olda.command("i"), "a fine wolf skin (x5)", "a blackberry (x2)")
    to_lab(olda)

    # ===== the sandbox =====
    os.makedirs(os.path.join(mudlib, "wiz", "wiz"), exist_ok=True)
    h.write(mudlib, "/wiz/wiz/rogue", ROGUE)
    h.write(mudlib, "/wiz/wiz/rogue_mat", ROGUE_MAT)
    wiz.command("drop all skins")
    wiz.command("mk 2 1")
    check("sandbox: the rogue charm is cloned", wiz.command("clone /wiz/wiz/rogue"), "Cloned /wiz/wiz/rogue#")
    text = wiz.command("rogue")
    print("      (rogue said: " + text.strip().splitlines()[0] + ")")
    check("sandbox: cannot set quality, quantity, state or properties of a real item", text, "quality False", "quantity False", "restore False", "property False")
    check("sandbox: cannot split a real stack, or feed it", text, "split refused", "receive False", "takesplit False")
    check("sandbox: a direct typed call is refused too", text, "direct ", absent=("direct True",))
    check("sandbox: CreateIn of a real item at Legendary is refused", text, "create refused")
    check("sandbox: its own sandbox items it may change", text, "own True True")
    check("sandbox: the real stack is unchanged", wiz.command("count"), "count q1=2 objects 1")
    check("sandbox: the sandbox stack exists", wiz.command("i"), "a legendary sandbox pebble (x7)")
    wiz.command("drop all pebbles")

    # ===== corpses and lost property =====
    for who in (ann, ben):
        who.command("drop all")
    a.drain(wiz)
    wiz.command("mk 3 2")
    wiz.command("mk 2 1")
    wiz.command("give 3 fine skins to ann")
    wiz.command("give 2 skins to ann")
    a.drain(ann)
    check("corpse: ann carries two stacks", ann.command("i"), "a fine wolf skin (x3)", "a wolf skin (x2)")
    wiz.command("slay ann")
    a.drain(ann, 1.0)
    text = wiz.command("look in corpse of ann")
    check("corpse: the stacks are in the corpse", text, "a fine wolf skin (x3)", "a wolf skin (x2)")
    store = open(os.path.join(work, "save", "secure", "lost_property.json")).read()
    check("corpse: the lost-property record holds quality and quantity", store, "/domains/test/wolf_skin?q=2\\u0026n=3", "/domains/test/wolf_skin?n=2")
    a.drain(ben)
    check("corpse lock: another player cannot take part of a stack", ben.command("get 2 skins from corpse of ann"), "is not yours to loot yet.")
    check("corpse lock: nor all of a name", ben.command("get all skins from corpse of ann"), "is not yours to loot yet.")
    to_lab(ann)
    check("corpse: the owner takes part of a stack", ann.command("get 2 fine skins from corpse of ann"), "You take a fine wolf skin (x2) from the corpse of Ann.")
    check("corpse: asking for more than it holds", ann.command("get 3 common skins from corpse of ann"), "There are only 2 in the corpse of Ann.")
    text = wiz.command("look in corpse of ann")
    check("corpse: the rest stays", text, "a fine wolf skin", "a wolf skin (x2)")
    store = open(os.path.join(work, "save", "secure", "lost_property.json")).read()
    check("corpse: the record follows the split", store, "/domains/test/wolf_skin?q=2\"", "/domains/test/wolf_skin?n=2", absent=("q=2\\u0026n=3",))
    quit_(ann)

    # a planned shutdown with stacks in the corpse, and a record in the old format written straight into the store
    for c in (ben, olda):
        quit_(c)
    wiz.send("shutdown")
    box[0].wait(timeout=20)
    box[1].close()
    path = os.path.join(work, "save", "secure", "lost_property.json")
    store = json.load(open(path))
    store["items"]["olda|old-format"] = [SKIN, SKIN, HOLLOW + "obj/rusty_sword"]
    store["gold"]["olda|old-format"] = 3
    with open(path, "w") as f:
        json.dump(store, f)
    box[0], box[1] = a.start_driver(work, mudlib)
    ann, text = login("ann")
    check("planned shutdown: the owner gets the rest of the corpse back", text, "Harland, the innkeeper", "You recover 3 items")
    check("planned shutdown: stacks come back with quality and quantity, merged", ann.command("i"), "a fine wolf skin (x3)", "a wolf skin (x2)")
    olda, text = login("olda")
    check("an old-format lost-property record is handed back", text, "Harland", "You recover 3 items and 3 gold.")
    check("its bare paths are default items, merged with what was carried", olda.command("i"), "a wolf skin (x2)", "a fine wolf skin (x5)", "a rusty sword")
    quit_(olda)

    # a crash straight after a gift: both save files already agree
    to_lab(ann)
    ben, _ = login("ben")
    to_lab(ben)
    a.drain(ann)
    ann.command("give 2 fine skins to ben")
    box[0].kill()
    box[0].wait(timeout=10)
    box[1].close()
    box[0], box[1] = a.start_driver(work, mudlib)
    ann, _ = login("ann")
    ben, _ = login("ben")
    check("crash after give: the giver has what is left", ann.command("i"), "a fine wolf skin", "a wolf skin (x2)")
    check("crash after give: the receiver has the gift, once", ben.command("i"), "a fine wolf skin (x2)", absent=("3 fine",))

    # crash after part of a stack was looted from a corpse
    wiz, _ = login("wiz")
    wiz.command("goto " + TEST + "lab")
    to_lab(ben)
    a.drain(ben)
    wiz.command("mk 5 3")
    wiz.command("give 5 excellent skins to ben")
    wiz.command("slay ben")
    a.drain(ben, 1.0)
    to_lab(ben)
    check("crash case: the owner takes 2 of 5 from the corpse", ben.command("get 2 excellent skins from corpse of ben"), "You take an excellent wolf skin (x2) from the corpse of Ben.")
    box[0].kill()
    box[0].wait(timeout=10)
    box[1].close()
    box[0], box[1] = a.start_driver(work, mudlib)
    ben, text = login("ben")
    # Ben was not saved after looting (as before F1, taking from a corpse does not save), so his save is from his death: the corpse's whole contents come back from the store,
    # which holds only what was still in the corpse. The 2 he took were never saved, so they are lost with the crash,
    # not duplicated.
    check("after a crash the corpse's remainder is returned", text, "Harland", "You recover 5 items")
    check("after the crash: 3 excellent from the corpse, never 5 or more", ben.command("i"), "an excellent wolf skin (x3)", "a fine wolf skin (x2)", absent=("5 excellent", "4 excellent"))
    quit_(ben)


if __name__ == "__main__":
    sys.exit(main())
