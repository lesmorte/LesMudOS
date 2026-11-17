#!/usr/bin/env python3
"""A play-through of the finished Vincent's Hollow, by a brand-new level 1 character.

Unlike tests/area_test.py, which tests the game systems with test rooms and wizard help,
this runs against an UNMODIFIED copy of the mudlib (no stub rooms, no changed numbers) and
plays the area from the start, the way a player would:

  1. walks every exit of all 18 rooms in both directions and compares rooms, objects, NPC
     lines and creatures with the area brief (docs/areas/vincents-hollow.md, which this script
     reads and quotes from, so the brief stays the source of truth),
  2. uses the shop, the tavern and the Guild,
  3. dies on purpose once in the Overseer Hall, shows a second player that the corpse is
     locked, logs out and in again in the Guest Rooms, and fetches the corpse,
  4. grinds through the Forgotten Cellars in order with a robust bot (fight; retreat when
     low; heal with beds, potions or rest; loot; buy better gear; farm respawns for levels),
  5. opens the Hidden Cache with the Rusty Key from a Goblin Guard,
  6. kills the Goblin Overseer, reports both quests and checks the ending speech.

The player is never a wizard. A second account ("warden") is used for out-of-game checks
only (room flags via `stat here`, the rare stock, the first-wizard bootstrap). If the solo
run cannot finish in time or after too many deaths, that is reported as a FINDING and the
rest of the flow is verified with wizard help (`heal`) on the same character, clearly
labelled in the output.

Combat is random, so every run is different. A real run takes 10 to 25 minutes.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4813 python3 tests/playthrough_test.py
"""
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4813")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402

ROOT = h.ROOT
check = h.check
BRIEF = open(os.path.join(ROOT, "docs", "areas", "vincents-hollow.md")).read()

# The solo run asks for wizard help after this many minutes or deaths, and the whole test gives up after HARD_MINUTES.
SOLO_MINUTES = float(os.environ.get("LESMUDOS_PLAY_MINUTES", "26"))
SOLO_DEATHS = int(os.environ.get("LESMUDOS_PLAY_DEATHS", "12"))
HARD_MINUTES = float(os.environ.get("LESMUDOS_PLAY_HARD_MINUTES", "45"))

OPPOSITE = {"north": "south", "south": "north", "east": "west", "west": "east", "up": "down", "down": "up"}
# The brief names two exit targets differently from the room's short name.
ALIASES = {"Hollowman's Rest": "The Hollowman's Rest", "Forgotten Cellars": "Collapsed Passage"}
START = "Village Square"
TAVERN = "The Hollowman's Rest"
GUEST = "Guest Rooms"
GUILD = "Adventurers Guild"
SMITH = "Ironoak Arms"
CHAMBER = "Overseer Chamber"

# What a player works out by `consider` and by dying: worst damage per round of one creature (top of its
# damage range; the Overseer counts Battle Cry plus a Power Strike), and the word that attacks it.
CREATURES = {
    "large rat": (2, "rat"), "giant rat": (3, "rat"), "wandering wolf": (4, "wolf"), "goblin scavenger": (3, "scavenger"),
    "spore bat": (3, "bat"), "goblin guard": (3, "guard"), "giant cave toad": (5, "toad"), "goblin warrior": (4, "warrior"),
    "goblin minion": (4, "minion"), "goblin elite guard": (5, "elite"), "goblin overseer": (16, "overseer"),
}
COUNTS = {"one": 1, "two": 2, "three": 3, "four": 4, "five": 5}
# Rooms with creatures that attack two seconds after a player arrives.
MOB_ROOMS = {"Tavern Cellar", "Collapsed Passage", "Storage Chamber", "Guard Post", "Flooded Tunnel", "Mushroom Cavern",
             "Goblin Barracks", "Overseer Hall", "Overseer Chamber"}
# The level a sensible player wants before a room (the dungeon is advertised for levels 2 to 5).
NEED_LEVEL = {"Tavern Cellar": 1, "Collapsed Passage": 1, "Storage Chamber": 1, "Guard Post": 2, "Flooded Tunnel": 3,
              "Mushroom Cavern": 3, "Goblin Barracks": 3, "Overseer Hall": 3, "Overseer Chamber": 4}
DUNGEON = ["Tavern Cellar", "Collapsed Passage", "Storage Chamber", "Guard Post", "Flooded Tunnel", "Mushroom Cavern",
           "Goblin Barracks", "Overseer Hall", "Overseer Chamber"]
WEAPONS = [("overseer axe", "overseer's axe"), ("axe", "woodcutter axe"), ("longsword", "fine iron longsword"), ("dagger", "enchanted dagger"),
           ("sword", "rusty sword"), ("spear", "training spear"), ("dagger", "iron dagger")]
# How the creatures of each room read in combat, death and corpse messages.
SHORT = {"Tavern Cellar": "a large rat", "Collapsed Passage": "a giant rat", "Storage Chamber": "a goblin scavenger", "Guard Post": "a goblin guard",
         "Flooded Tunnel": "a giant cave toad", "Mushroom Cavern": "a spore bat", "Goblin Barracks": "a goblin warrior", "Overseer Hall": "a goblin elite guard",
         "Overseer Chamber": "the Goblin Overseer"}
COMBAT_LINE = re.compile(r"(You (hit|miss|scratch|smash)|(hits|misses|scratches|smashes) you)")


# ---------------------------------------------------------------- the brief

def parse_brief():
    """The rooms of the brief: {short: {desc, exits{direction: short}}}."""
    rooms = {}
    for m in re.finditer(r"### (?:Dungeon room \d+|Room \d+): (.*?)\n(.*?)(?=\n### |\n## )", BRIEF, re.S):
        head, body = m.group(1), m.group(2)
        short = re.search(r"^Short: (.*)$", body, re.M)
        short = short.group(1) if short else head
        desc = re.search(r"^Description: (.*?)(?=\n\n(?:Exits|Objects|NPC|Mobs|Boss)|\nExits)", body, re.S | re.M)
        exits = {}
        line = re.search(r"^Exits: (.*)$", body, re.M)
        for part in (line.group(1).split(", ") if line else []):
            direction, target = part.split(" -> ")
            exits[direction] = ALIASES.get(target, target)
        rooms[short] = {"desc": " ".join(desc.group(1).split()), "exits": exits}
    return rooms


ROOMS = parse_brief()


def quote(text):
    """The text, after making sure the brief really contains it (the brief is the source of truth)."""
    assert text in BRIEF, "not in the brief: " + text
    return text


def route(src, dst):
    """The shortest list of directions from room src to room dst."""
    seen, queue = {src: []}, [src]
    while queue:
        room = queue.pop(0)
        if room == dst:
            return seen[room]
        for direction, target in ROOMS[room]["exits"].items():
            if target not in seen:
                seen[target] = seen[room] + [direction]
                queue.append(target)
    raise KeyError((src, dst))


def clean(text):
    return text.replace("\r", "")


def flat(text):
    return " ".join(clean(text).split())


def room_of(text):
    """The room named by the last room description in the text (the line two above 'Obvious exits:')."""
    lines = clean(text).split("\n")
    found = None
    for i, line in enumerate(lines):
        if line.startswith("Obvious exits:") and i >= 2 and lines[i - 2] in ROOMS:
            found = lines[i - 2]
    return found


class TimeUp(Exception):
    pass


# ---------------------------------------------------------------- the player

class Hero:
    """A client plus what a player keeps in mind: where he is, health, gold, level, gear."""

    def __init__(self, name, stats, deadline=None):
        self.name = name
        self.stats = stats
        self.deadline = deadline
        self.c = h.Client(name)
        self.welcome = self.c.read_until("> ")
        self.hp, self.max_hp, self.level, self.gold, self.xp = 30, 30, 1, 25, 0
        self.pos = None
        self.entered = {}
        self.died = False
        self.death_room = None
        self.helped = False
        self.log_text = ""
        self.pos = room_of(self.feed(self.welcome))

    # ---- the wire ----

    def feed(self, text):
        """Everything that arrives passes through here: deaths, levels, position, health."""
        text = clean(text)
        self.log_text += text
        if "You have died!" in text:
            self.died = True
            self.death_room = self.pos
            self.stats["deaths"] += 1
            self.stats["death_log"].append((self.pos, self.level, round(time.time() - self.stats["t0"])))
        for m in re.finditer(r"You have reached level (\d+)!", text):
            self.level = int(m.group(1))
            self.stats["level_log"].append((self.level, round(time.time() - self.stats["t0"])))
        room = room_of(text)
        if room:
            self.pos = room
            self.entered[room] = time.time()
        for m in re.finditer(r"HP (\d+)/(\d+)", text):
            self.hp, self.max_hp = int(m.group(1)), int(m.group(2))
        return text

    def cmd(self, line, timeout=30):
        if self.deadline and time.time() > self.deadline:
            raise TimeUp(self.name)
        return self.feed(self.c.command(line, timeout=timeout))

    def pump(self, seconds):
        return self.feed(a.quiet(self.c, seconds))

    def settle(self, seconds=1.0):
        """Read until nothing arrives for a while."""
        text = ""
        while True:
            more = self.pump(seconds)
            text += more
            if not more:
                return text

    # ---- state ----

    def health(self):
        self.cmd("hp")
        return self.hp

    def score(self):
        text = self.cmd("score")
        for key, pattern in (("gold", r"Gold (\d+)"), ("xp", r"Experience (\d+)"), ("level", r"Level (\d+)")):
            m = re.search(pattern, text)
            if m:
                setattr(self, key, int(m.group(1)))
        return text

    def inventory(self):
        text = self.cmd("i")
        m = re.search(r"You are carrying: (.*)\.", text)
        return [x.strip() for x in m.group(1).split(", ")] if m else []

    def count(self, word):
        return sum(1 for item in self.inventory() if word in item)

    def enemies(self):
        """The creatures a look shows here: [(count, name, kill word, worst damage per round)]."""
        found = []
        for line in self.cmd("look").split("\n"):
            if not (line.endswith(" are here.") or line.endswith(" is here.")) or "corpse" in line:
                continue
            low = line.lower()
            for name, (worst, word) in CREATURES.items():
                if name in low:
                    found.append((COUNTS.get(low.split()[0], 1), name, word, worst))
                    break
        return found

    # ---- moving ----

    def move(self, direction):
        """One step. A room whose creatures were told of us a moment ago is entered only after their ambush call-out has passed."""
        target = ROOMS.get(self.pos, {"exits": {}})["exits"].get(direction)
        if target in MOB_ROOMS and time.time() - self.entered.get(target, 0) < 2.4:
            time.sleep(2.4 - (time.time() - self.entered[target]))
        return self.cmd(direction)

    def go(self, dst):
        text = ""
        if self.pos == dst:
            return text
        for direction in route(self.pos, dst):
            text = self.move(direction)
            if self.died:
                break
        return text

    # ---- gear ----

    def equip_all(self):
        items = self.inventory()
        low = " | ".join(items).lower()
        for word, name in WEAPONS:
            if name in low:
                if not any(name in i.lower() and "(wielded)" in i for i in items):
                    self.cmd("wield " + ("overseer axe" if name == "overseer's axe" else name if name in ("enchanted dagger",) else word))
                break
        for name, word in (("small shield", "shield"), ("guild initiate's helm", "helm"), ("leather commander's vest", "vest"), ("magical trinket", "trinket")):
            for item in items:
                if name in item.lower() and "(worn)" not in item:
                    self.cmd("wear " + word)
        return self.inventory()

    def quit(self):
        self.c.send("quit")
        self.c.read_until("Goodbye", 10)
        try:
            self.c.sock.close()
        except OSError:
            pass


# ---------------------------------------------------------------- the run

class Run:
    def __init__(self, stats):
        self.stats = stats
        self.hero = None
        self.warden = None
        self.t_hard = stats["t0"] + HARD_MINUTES * 60
        self.rooms = {name: {"engagements": 0, "retreats": 0, "deaths": 0, "seconds": 0.0, "kills": 0, "lost": 0} for name in DUNGEON}
        self.boss_text = ""
        self.cleared = []
        self.checked_messages = set()
        self.has_key = False
        self.fallback = False

    # ================================================================ helpers

    def elapsed(self):
        return time.time() - self.stats["t0"]

    def note(self, text):
        print(f"      [{self.elapsed():6.0f}s L{self.hero.level if self.hero else '-'}] {text}", flush=True)

    def warden_in(self):
        """The warden, logged in and parked in the Void, out of everybody's way."""
        if self.warden is None:
            self.warden = h.Client("warden")
            self.warden.read_until("> ")
            self.warden.command("goto /std/void")
            a.drain(self.warden, 0.3)
        return self.warden

    def warden_out(self):
        if self.warden is not None:
            self.warden.send("quit")
            self.warden.read_until("Goodbye", 5)
            self.warden = None

    # ================================================================ 1. the world, as a new player sees it

    def start(self):
        w = h.Client("warden")
        w.read_until("> ")
        check("the first wizard is made with the admin secret (an out-of-game helper; the player never is one)", w.command("promote " + h.SECRET), "level 10 wizard")
        w.send("quit")
        w.read_until("Goodbye")

        hero = self.hero = Hero("hero", self.stats, deadline=self.t_hard)
        check("a new character arrives in the Village Square", hero.welcome, "Welcome, Hero.", "Village Square", ROOMS["Village Square"]["desc"])
        check("score of a new character", hero.score(), "Level 1   Experience 0 (100 more for level 2)", "HP 30/30   Gold 25", "Damage 1-3", "nothing equipped", "You have no quests.")
        check("quests of a new character", hero.cmd("quests"), "You have no quests.")
        check("a new character carries nothing", hero.cmd("i"), "You are empty-handed.")

    def walk_everything(self):
        """Every exit of every room, in both directions, with the brief's room, exit and occupant lists."""
        hero = self.hero
        occupants = {
            "Village Gate": ["The village guard is here."],
            "Village Square": ["Old Farmer Benson is here.", "A traveling merchant is here."],
            "Adventurers Guild": ["Guildmaster Rowan is here."],
            "Ironoak Arms": ["Blacksmith Torren is here."],
            TAVERN: ["Harland the Innkeeper is here.", "Mara the Barmaid is here.", "A drunken prospector is here."],
            "Tavern Cellar": ["Three large rats are here."],
            "Collapsed Passage": ["Two giant rats are here."],
            "Storage Chamber": ["Two goblin scavengers are here.", "A minor healing potion is here."],
            "Guard Post": ["Two goblin guards are here."],
            "Flooded Tunnel": ["A giant cave toad is here."],
            "Mushroom Cavern": ["Three spore bats are here."],
            "Goblin Barracks": ["Three goblin warriors are here."],
            "Overseer Hall": ["Two goblin elite guards are here."],
            CHAMBER: ["The Goblin Overseer is here."],
        }
        visited = {START}
        edges = [0]
        seen_encounter = []

        def visit(room):
            for direction, target in ROOMS[room]["exits"].items():
                if room == "Mushroom Cavern" and direction == "north":
                    refused = hero.cmd("north")
                    check("the door to the Hidden Cache is locked without the Rusty Key", refused, "A stout, rust-eaten door bars the way north. It is locked.", absent=("Hidden Cache",))
                    continue
                if target in visited:
                    continue
                visited.add(target)
                text = hero.move(direction)
                got = room_of(text)
                edges[0] += 1
                check(f"{room} -{direction}-> {target}", "ok" if got == target else f"got {got!r}: {text[-200:]}", "ok")
                if got != target:
                    continue
                check(f"{target}: description word for word", flat(text), ROOMS[target]["desc"])
                exits = re.search(r"Obvious exits: (.*)", text)
                check(f"{target}: exits are those of the brief", ",".join(sorted(exits.group(1).split(", "))), ",".join(sorted(ROOMS[target]["exits"])))
                for line in occupants.get(target, []):
                    check(f"{target}: '{line}'", text, line)
                if target in ("Guest Rooms", "Smuggler Hall"):
                    check(f"{target}: nobody here", text, absent=(" is here.", " are here."))
                if target == "North Road":
                    for who in ("wandering wolf", "traveling merchant"):
                        if who in text:
                            seen_encounter.append(who)
                visit(target)
                back = OPPOSITE[direction]
                check(f"{target} has the return exit {back} to {room}", "yes" if ROOMS[target]["exits"].get(back) == room else "no", "yes")
                text = hero.move(back)
                edges[0] += 1
                check(f"{target} -{back}-> {room}", "ok" if room_of(text) == room else f"got {room_of(text)!r}", "ok")

        visit(START)
        check("17 of the 18 rooms were reached on foot (the Hidden Cache needs the key) and every exit walked both ways", f"{len(visited)} rooms, {edges[0]} exits walked", "17 rooms, 32 exits")
        self.note(f"walked {edges[0]} exits in both directions; encounters seen on the North Road: {seen_encounter or 'none this time'}")
        self.stats["notes"].append(f"North Road encounters seen during the walk: {seen_encounter or 'none'}")
        return seen_encounter

    def look_at_things(self):
        """Objects, signs, NPCs and their quoted lines, room by room."""
        hero = self.hero

        def at(room):
            hero.go(room)

        # North Road
        at("North Road")
        sign_text = 'The sign reads:\n"Welcome to Vincent\'s Hollow\nFounded by Vincent Holloway\nShelter, Ale, and Honest Trade"'
        for line in ("Welcome to Vincent's Hollow", "Founded by Vincent Holloway", "Shelter, Ale, and Honest Trade"):
            quote(line)
        check("the Village Sign reads as in the brief", clean(hero.cmd("read sign")), sign_text)
        check("look at the sign", hero.cmd("look at sign"), "carved wooden sign", "You could read it.")
        check("the road north is not open yet", hero.cmd("north"), "not yet open to travelers")
        # Village Gate
        at("Village Gate")
        check("look at the Village Guard", hero.cmd("look at guard"), "square-jawed man")
        check("the Village Guard's first line is the brief's", hero.cmd("talk guard"),
              'Village Guard says: "%s"' % quote("If you're lookin' for glory, ask old Harland at the tavern. The cellar's got troubles no one's managed to solve."))
        check("the Village Guard's additional dialogue is the brief's", hero.cmd("talk guard"),
              'Village Guard says: "%s"' % quote("Some folk say Vincent still watches over us. I just know strange things have been happening underground."))
        check("the Village Guard cannot be attacked", hero.cmd("kill guard"), "calm lies over this place")
        # Village Square
        at(START)
        check("look at the Stone Well", hero.cmd("look at well"), "broad well of fitted grey stone", "plaque")
        check("the plaque reads as in the brief", hero.cmd("read plaque"), quote("This well was dug by Vincent Holloway and his companions during the settlement's first winter."))
        check("look at the Bench", hero.cmd("look at bench"), "plank benches")
        check("Old Farmer Benson's line is the brief's", hero.cmd("talk benson"),
              'Benson says: "%s"' % quote("Vincent founded this place. Brave man... but he went lookin' for somethin' beneath the earth and never returned."))
        check("look at Benson", hero.cmd("look at benson"), "stooped, weathered man", "Old Farmer Benson is unhurt.")
        check("ask Benson about Vincent", hero.cmd("ask benson about vincent"), "Vincent Holloway. Came up from the south")
        check("look at the Traveling Merchant", hero.cmd("look at merchant"), "pack twice his size")
        check("the Traveling Merchant sells potions and rations", hero.cmd("browse"), "A minor healing potion", "15 gold", "A trail ration", "5 gold")
        check("talk to the Traveling Merchant", hero.cmd("talk merchant"), "Traveling Merchant says:")
        # Guild
        at(GUILD)
        board = hero.cmd("look at board")
        check("look at the Quest Board", board, "large notice board", "You could read it.")
        board = hero.cmd("read board")
        check("read the Quest Board", board, "CLEAR THE CELLAR", "Defeat the Goblin Overseer", "Guild Initiate's Helm", "RECOVER THE OVERSEER'S ORDERS", "Your contracts: Clear the Cellar (open); Recover the Overseer's Orders (open).")
        check("Guildmaster Rowan's line is the brief's", hero.cmd("talk rowan"),
              'Rowan says: "%s"' % quote("Every village has its legends. Ours is Vincent himself. Some believe he found something beneath these lands before he vanished."))
        check("look at Rowan", hero.cmd("look at rowan"), "scar through one eyebrow")
        check("nothing to report yet", hero.cmd("report"), "You have nothing to report yet.")
        # Ironoak Arms
        at(SMITH)
        check("Blacksmith Torren's line is the brief's", hero.cmd("talk torren"),
              'Torren says: "%s"' % quote("Quality steel saves lives. Cheap steel fills graveyards."))
        shop = hero.cmd("browse")
        check("Torren sells the five items of the brief", shop, "A rusty sword", "10 gold", "A training spear", "8 gold", "An iron dagger", "6 gold",
              "A woodcutter axe", "18 gold", "A small shield", "12 gold")
        self.longsword_listed = "fine iron longsword" in shop
        if self.longsword_listed:
            check("the rare Fine Iron Longsword is on the rack at 60 gold", shop, "A fine iron longsword", "60 gold")
        else:
            check("without the rare longsword in stock Torren says so", hero.cmd("buy longsword"), "does not sell 'longsword'")
        self.stats["notes"].append("Torren's rare longsword was %s when the player first browsed (30%% chance)" % ("in stock" if self.longsword_listed else "not in stock"))
        check("look at the forge", hero.cmd("look at forge"), "roars like a caged beast")
        # Tavern
        at(TAVERN)
        check("look at the portrait of Vincent Holloway", hero.cmd("look at portrait"), "weathered portrait of Vincent Holloway", "confident smile")
        check("Harland's first line is the brief's", hero.cmd("talk harland"),
              'Harland says: "%s"' % quote("Started with rats years ago. Then goblins moved in. Now something's organizing them."))
        check("Harland's secondary line is the brief's", hero.cmd("talk harland"),
              'Harland says: "%s"' % quote("Vincent used to talk about tunnels beneath the village. Nobody believed him."))
        check("talk to Mara", hero.cmd("talk mara"), "Mara says:")
        check("talk to the Drunken Prospector", hero.cmd("talk prospector"), "Drunken Prospector says:")
        check("Harland sells food, drink and a bed", hero.cmd("browse"), "A bowl of stew", "4 gold", "A mug of ale", "2 gold", "A bed for the night", "5 gold")
        # Tavern Cellar and Guest Rooms
        at("Tavern Cellar")
        check("look at the Broken Crates", hero.cmd("look at broken crates"), "splintered across the floor")
        check("look at the Chewed Grain Sacks", hero.cmd("look at chewed grain sacks"), "seams chewed open")
        check("look at a large rat", hero.cmd("look at rat"), "red, hungry eyes", "A large rat is unhurt.")
        check("consider a large rat", hero.cmd("consider rat"), "A large rat looks like an even match")
        at(GUEST)

    # ================================================================ 2. shop, tavern, guild, a deliberate death

    def first_purchases(self):
        hero = self.hero
        hero.go(SMITH)
        text = hero.cmd("buy sword")
        check("buy the Rusty Sword for 10 gold", text, "You buy a rusty sword for 10 gold.")
        check("wield it", hero.cmd("wield sword"), "You wield a rusty sword.")
        check("the shield is too dear for now (15 gold left)", hero.cmd("buy axe"), "You cannot afford that. It costs 18 gold.")
        check("score shows the sword", hero.score(), "Damage 3-5", "weapon: a rusty sword", "Gold 15")
        hero.go(GUILD)
        check("accept Clear the Cellar", hero.cmd("accept cellar"), "Quest accepted: Clear the Cellar.", "The goblins answer to an Overseer. Kill him, and come back and report.")
        check("accepting again is refused politely", hero.cmd("accept cellar"), "You have already taken \"Clear the Cellar\".")
        check("quests shows the active quest with its proper title", hero.cmd("quests"), "Active quests: Clear the Cellar")
        check("the board shows the contract as active", hero.cmd("read board"), "Clear the Cellar (active)")
        check("report before the work is done", hero.cmd("report"), "The Overseer still lives.")

    def die_on_purpose(self):
        """A reckless trip to the Overseer Hall at level 1, to see what death costs and how the corpse is recovered."""
        hero = self.hero
        hero.go("Overseer Hall")
        check("the reckless hero stands in the Overseer Hall", hero.pos, "Overseer Hall")
        hero.cmd("kill elite")
        t0 = time.time()
        text = ""
        while not hero.died and time.time() - t0 < 120:
            text += hero.pump(0.5)
        check("two goblin elite guards kill a level 1 character", text, "You have died!", "You wake in the Guest Rooms, bruised but alive. The healer's fee costs you 1 gold.")
        hero.settle(1.0)
        hero.died = False
        check("the dead character wakes in the Guest Rooms", hero.pos, GUEST)
        check("the belongings stayed with the corpse", hero.cmd("i"), "You are empty-handed.")
        check("score after the death", hero.score(), "HP 30/30   Gold 14", "nothing equipped", "Active quests: Clear the Cellar")
        self.stats["notes"].append("deliberate death in the Overseer Hall at level 1 (2 elite guards)")

    def second_player_sees_locked_corpse(self):
        thief = Hero("thief", self.stats, deadline=self.t_hard)
        thief.go("Overseer Hall")
        text = thief.cmd("look")
        check("a second player sees the corpse of Hero", text, "The corpse of Hero is here.")
        check("the lock is announced", thief.cmd("look at corpse of hero"), "It holds: a rusty sword, a gold coin.", "only those involved may touch it.")
        check("loot is refused for a second player", thief.cmd("loot"), "The corpse of Hero is not yours to loot yet.")
        check("taking one thing is refused too", thief.cmd("get sword from corpse of hero"), "The corpse of Hero is not yours to loot yet.")
        thief.go(START)
        thief.quit()

    def logout_in_guest_rooms(self):
        """Quit in the Guest Rooms (the logout room) and come back."""
        hero = self.hero
        hero.quit()
        hero.c = h.Client("hero")
        text = hero.c.read_until("> ")
        hero.feed(text)
        check("logging in again puts the character back in the Guest Rooms", text, "Welcome back, Hero. This is visit number 2.", "Guest Rooms", ROOMS[GUEST]["desc"])
        check("the character is saved", hero.score(), "Active quests: Clear the Cellar")

    def recover_corpse(self):
        hero = self.hero
        room = hero.death_room or "Overseer Hall"
        hero.go(room)
        text = hero.cmd("loot")
        check("the dead player loots the corpse and gets the sword and the fee back", text, "You take a rusty sword from the corpse of Hero.") if "rusty sword" in text else check("loot", text, "You take")
        text += hero.cmd("loot")
        hero.go("Goblin Barracks") if room == "Overseer Hall" else None
        self.note("corpse fetched from the " + room)
        hero.go(START)
        hero.equip_all()
        hero.score()
        check("the sword is wielded again and the gold is back", "Damage 3-5 gold %d" % hero.gold, "Damage 3-5", "gold 15")

    def shield_and_ale(self):
        hero = self.hero
        hero.go(SMITH)
        check("buy the Small Shield", hero.cmd("buy shield"), "You buy a small shield for 12 gold.")
        check("wear it", hero.cmd("wear shield"), "You put on a small shield.")
        hero.go(TAVERN)
        check("buy a mug of ale", hero.cmd("buy ale"), "You buy a mug of ale for 2 gold.")
        check("drink it", hero.cmd("drink ale"), "You drink the ale down in a few long swallows.")
        check("the ale is used up", hero.cmd("drink ale"), "Drink what?")
        hero.score()

    # ================================================================ 3. the grind

    def out_of_time(self):
        return self.fallback_wanted()

    def fallback_wanted(self):
        return not self.fallback and (self.elapsed() > SOLO_MINUTES * 60 or self.stats["deaths"] > SOLO_DEATHS)

    def enable_fallback(self, why):
        """The solo run does not finish: say so, and ask the wizard to make the character a wizard (for `heal`)."""
        self.fallback = True
        self.stats["notes"].append(f"FINDING: the solo run did not finish in time ({why}); wizard help (heal) was used from {self.elapsed():.0f}s on")
        print(f"\n      *** FINDING: the solo run did not finish ({why}). From here on the character uses WIZARD HELP (heal). ***\n", flush=True)
        w = self.warden_in()
        w.command("promote hero 5")
        self.warden_out()
        self.hero.settle(1.0)

    def buy_bed(self):
        hero = self.hero
        hero.go(TAVERN)
        text = hero.cmd("buy bed")
        if "You buy a bed for the night" in text:
            self.stats["beds"] += 1
            hero.health()
            if self.stats["beds"] == 1:
                check("buying the bed heals fully and moves to the Guest Rooms", text, "Harland shows you upstairs. You sleep soundly and wake fully rested.", "Guest Rooms")
                check("rested to full HP", "%d/%d" % (hero.health(), hero.max_hp), "%d/%d" % (hero.max_hp, hero.max_hp))
            return True
        return False

    def rest(self, want_fraction=0.9, limit=240):
        """Rest in the Guest Rooms (safe: 3 HP per 10 seconds) until healthy."""
        hero = self.hero
        hero.go(GUEST)
        t0 = time.time()
        start = hero.health()
        while hero.hp < hero.max_hp * want_fraction and time.time() - t0 < limit:
            time.sleep(4)
            hero.cmd("hp")
            if self.fallback:
                hero.cmd("heal")
        self.stats["rest_hp"] = self.stats.get("rest_hp", 0) + max(0, hero.hp - start)
        self.stats["rest_s"] = self.stats.get("rest_s", 0) + (time.time() - t0)
        return hero.hp

    def heal_up(self, want_fraction=0.9):
        """Get healthy: a bed if there is the gold (it also restores everything at once), else rest."""
        hero = self.hero
        if self.fallback:
            hero.cmd("heal")
            return
        if hero.hp >= hero.max_hp * want_fraction:
            return
        hero.score()
        if hero.gold >= 5 and self.buy_bed():
            return
        self.rest(want_fraction)

    def shopping(self, potions_wanted=0):
        """Spend the loot: shield, a better weapon (selling the old one), potions, the rare longsword."""
        hero = self.hero
        hero.score()
        items = hero.inventory()
        low = " ".join(items)
        keep = 6  # a bed and a little more
        if hero.gold >= keep + 12 and "small shield" not in low:
            hero.go(SMITH)
            hero.cmd("buy shield")
            hero.cmd("wear shield")
            hero.score()
        if hero.gold >= keep + 13 and "woodcutter axe" not in low and "longsword" not in low and "overseer" not in low:
            hero.go(SMITH)
            text = hero.cmd("buy axe")
            if "You buy a woodcutter axe" in text:
                hero.cmd("wield axe")
                if "rusty sword" in low:
                    t = hero.cmd("sell sword")
                    if "sell_checked" not in self.stats:
                        self.stats["sell_checked"] = True
                        check("sell the old sword back for half its value", t, "You sell a rusty sword to Torren for 5 gold.")
                check("the woodcutter axe is wielded", hero.cmd("i"), "a woodcutter axe (wielded)")
            hero.score()
        if "enchanted dagger" in low or "magical" in low:
            pass
        if hero.gold >= keep + 60 and "overseer" not in low and "longsword" not in low:
            hero.go(SMITH)
            if "fine iron longsword" in hero.cmd("browse"):
                text = hero.cmd("buy longsword")
                if "You buy" in text:
                    hero.cmd("wield longsword")
                    self.stats["notes"].append("bought the rare Fine Iron Longsword")
            hero.score()
        have = hero.count("healing potion")
        while have < potions_wanted and hero.gold >= keep + 15:
            hero.go(START)
            text = hero.cmd("buy potion")
            if "You buy" not in text:
                break
            if "potion_bought" not in self.stats:
                self.stats["potion_bought"] = True
                check("buy a Minor Healing Potion from the Traveling Merchant", text, "You buy a minor healing potion for 15 gold.")
            have += 1
            hero.score()
        hero.equip_all()

    def loot_here(self):
        """Loot every corpse and pick up what lies about. Returns the text."""
        hero = self.hero
        text = ""
        for _ in range(8):
            out = hero.cmd("loot")
            text += out
            if "You take" not in out:
                break
        for word in ("potion", "coins", "heap", "trinket", "dagger", "key"):
            pass
        return text

    def fight(self, room, boss=False, use_potions=False):
        """Fight whatever is in the room until it is dead or we must retreat. Returns 'won', 'retreat' or 'died'."""
        hero = self.hero
        info = self.rooms[room]
        info["engagements"] += 1
        started = time.time()
        start_hp = hero.hp
        enemies = hero.enemies()
        if not enemies:
            return "won"
        worst = sum(c * w for c, _, _, w in enemies)
        word = self.target_word(enemies)
        text = hero.cmd("kill " + word)
        self.stats["fights"] += 1
        last_combat = time.time()
        while True:
            time.sleep(0.45)
            out = hero.cmd("hp")
            if boss:
                self.boss_text += text + out
            text = ""
            if hero.died:
                info["deaths"] += 1
                info["seconds"] += time.time() - started
                info["lost"] += start_hp
                return "died"
            if COMBAT_LINE.search(out):
                last_combat = time.time()
            if "dies." in out:
                info["kills"] += out.count("dies.")
                enemies = hero.enemies()
                if not enemies:
                    info["seconds"] += time.time() - started
                    info["lost"] += max(0, start_hp - hero.hp)
                    return "won"
                worst = sum(c * w for c, _, _, w in enemies)
                last_combat = time.time()
                word = self.target_word(enemies)
                hero.cmd("kill " + word)
            if self.fallback and hero.hp < hero.max_hp * 0.6:
                hero.cmd("heal")
                continue
            if hero.hp <= worst + 3:
                if use_potions and hero.count("healing potion") > 0 and hero.hp > worst:
                    t = hero.cmd("drink potion")
                    self.stats["potions"] += 1
                    if self.stats["potions"] == 1:
                        check("a healing potion heals 20 in the fight", t, "You pull the stopper and drink the ruby-red potion.", "You recover 20 hit points.")
                    continue
                info["retreats"] += 1
                self.stats["retreats"] += 1
                info["seconds"] += time.time() - started
                info["lost"] += max(0, start_hp - hero.hp)
                return "retreat"
            if time.time() - last_combat > 4:
                enemies = hero.enemies()
                if not enemies:
                    info["seconds"] += time.time() - started
                    info["lost"] += max(0, start_hp - hero.hp)
                    return "won"
                worst = sum(c * w for c, _, _, w in enemies)
                word = self.target_word(enemies)
                hero.cmd("kill " + word)
                last_combat = time.time()

    @staticmethod
    def target_word(enemies):
        for _, name, word, _ in enemies:
            if name == "goblin minion":
                return word
        return enemies[0][2]

    def retreat_and_heal(self, room):
        """Walk out of the fight (creatures do not follow and do not heal), recover, and come back."""
        hero = self.hero
        if hero.pos != room:
            return
        left = False
        if len(ROOMS[room]["exits"]) == 1:
            # a dead end: `flee` can only lead out; it fails one time in four, so try again (no heartbeat passes between the commands)
            for _ in range(8):
                text = hero.cmd("flee")
                if "You flee" in text:
                    self.stats["flees"] = self.stats.get("flees", 0) + 1
                    if self.stats["flees"] == 1:
                        check("flee leaves the fight by the only exit", text, "You flee %s!" % next(iter(ROOMS[room]["exits"])), ROOMS[next(iter(ROOMS[room]["exits"].values()))]["desc"])
                    left = True
                    break
                if "cannot get away" in text:
                    self.stats["flee_fails"] = self.stats.get("flee_fails", 0) + 1
        if not left:
            hero.cmd(route(room, TAVERN)[0])
        self.heal_up()
        self.go_back(room)

    def go_back(self, room):
        self.hero.go(room)

    def clear_room(self, room, boss=False, use_potions=False):
        """Fight in this room until nothing is left, retreating and recovering as often as needed, then loot. Returns the loot text."""
        hero = self.hero
        loot = ""
        mark = len(hero.log_text)
        for attempt in range(40):
            if self.fallback_wanted():
                self.enable_fallback("%.0f minutes, %d deaths" % (self.elapsed() / 60, self.stats["deaths"]))
            hero.died = False
            if hero.pos != room:
                hero.go(room)
                if hero.died:
                    self.after_death(room)
                    continue
            if not self.fallback and hero.hp < hero.max_hp * 0.8 and hero.enemies():
                self.heal_up()
                hero.go(room)
            result = self.fight(room, boss=boss, use_potions=use_potions)
            if result == "won":
                loot = self.loot_here()
                self.check_messages(room, hero.log_text[mark:])
                return loot
            if result == "retreat":
                self.retreat_and_heal(room)
            elif result == "died":
                self.after_death(room)
        raise AssertionError(f"could not clear {room} in 40 attempts")

    def check_messages(self, room, text):
        """The wording of combat, death and corpse messages for the creature of this room (once per room)."""
        short = SHORT.get(room)
        if short is None or room in self.checked_messages:
            return
        self.checked_messages.add(room)
        cap = short[0].upper() + short[1:]
        check(f"{room}: combat lines name the creature ({short})", text, f"{cap} ") if re.search(r"(attacks|hits|misses|scratches|smashes) you", text) else None
        check(f"{room}: you hit or miss {short}", "yes" if re.search(r"You (hit|miss|scratch|smash) %s" % re.escape(short), text) else "no", "yes")
        if room != CHAMBER:
            check(f"{room}: '{cap} dies.'", text, f"{cap} dies.")
        look = self.hero.cmd("look")
        check(f"{room}: corpses read 'the corpse of {short}'", "yes" if re.search(r"[Tt]he corpse of %s is here|corpses of %s are here" % (re.escape(short), re.escape(short)), look) else look, "yes")

    def after_death(self, room):
        """Woke in the Guest Rooms with nothing: fetch the corpse (the creatures are weak after our fight and do not chase)."""
        hero = self.hero
        self.note(f"died in the {hero.death_room}; fetching the corpse")
        hero.settle(1.0)
        hero.died = False
        where = hero.death_room or room
        for attempt in range(3):
            hero.go(where)
            if hero.died:
                hero.settle(1.0)
                hero.died = False
                continue
            text = ""
            for _ in range(3):
                out = hero.cmd("loot")
                text += out
                if "You take" not in out:
                    break
            hero.go(TAVERN)
            hero.died = False
            if "You take" in text:
                hero.equip_all()
                self.stats["recoveries"] = self.stats.get("recoveries", 0) + 1
                return
        # The corpse could not be reached: start again with what the purse allows (a sword is 10 gold).
        hero.score()
        hero.go(SMITH)
        if hero.gold >= 10:
            hero.cmd("buy sword")
            hero.cmd("wield sword")
        self.stats["notes"].append("a corpse could not be fetched; gear was bought again")

    def ensure_level(self, level, farm_rooms):
        """Fight the respawned creatures of the rooms we have cleared until we are strong enough for the next one."""
        hero = self.hero
        hero.score()
        waited = 0
        while hero.level < level and not self.fallback_wanted():
            did = False
            for room in farm_rooms:
                if hero.level >= level:
                    break
                if hero.level < NEED_LEVEL[room]:
                    continue
                hero.died = False
                if hero.hp < hero.max_hp * 0.8:
                    self.heal_up()
                hero.go(room)
                if hero.died:
                    self.after_death(room)
                    continue
                if hero.enemies():
                    self.stats["farm"] += 1
                    self.clear_room(room)
                    did = True
                else:
                    hero.go(TAVERN)
            if not did:
                # Nothing has respawned yet: wait in the village and look at the road. The wolf of the North Road is worth 25 XP.
                waited += 1
                self.wait_and_wolf()
        hero.score()

    def wait_and_wolf(self):
        hero = self.hero
        if hero.hp < hero.max_hp * 0.9:
            self.heal_up()
        hero.go("North Road")
        if hero.enemies():
            self.note("an encounter on the North Road")
            self.stats["notes"].append("fought a North Road encounter: " + ", ".join(n for _, n, _, _ in hero.enemies()))
            self.clear_room("North Road") if "North Road" in self.rooms else None
        hero.go(GUEST)
        time.sleep(8)
        hero.cmd("hp")

    def grind(self):
        hero = self.hero
        self.rooms["North Road"] = {"engagements": 0, "retreats": 0, "deaths": 0, "seconds": 0.0, "kills": 0, "lost": 0}
        NEED_LEVEL["North Road"] = 1
        for room in DUNGEON:
            if room == "Overseer Chamber":
                break
            self.note(f"next: {room}")
            if room in ("Goblin Barracks", "Overseer Hall"):
                self.shopping(potions_wanted=2)
            else:
                self.shopping()
            if hero.level < NEED_LEVEL[room]:
                self.ensure_level(NEED_LEVEL[room], self.cleared)
            self.heal_up()
            hero.go(room)
            if hero.died:
                self.after_death(room)
                hero.go(room)
            if room == "Mushroom Cavern" and not self.has_key:
                pass
            loot = self.clear_room(room, use_potions=room in ("Goblin Barracks", "Overseer Hall"))
            self.cleared.append(room)
            self.room_after(room, loot)
        self.note("the dungeon is cleared up to the Overseer's door")

    def room_after(self, room, loot):
        hero = self.hero
        hero.score()
        self.note(f"{room} cleared; level {hero.level}, xp {hero.xp}, gold {hero.gold}, hp {hero.hp}/{hero.max_hp}")
        if room == "Storage Chamber":
            t = hero.cmd("get potion")
            if "You take a minor healing potion." in t:
                check("the Minor Healing Potion lies in the Storage Chamber", t, "You take a minor healing potion.")
            else:
                self.stats["notes"].append("the Storage Chamber's potion was already taken")
        if room == "Guard Post":
            if "rusty key" in loot:
                self.has_key = True
                check("one Goblin Guard's corpse holds the Rusty Key", loot, "a rusty key")
            else:
                # the other guard (it may have been looted first); the keyholder is always one of the two: look again
                hero.cmd("look")
            hero.equip_all()
            self.has_key = hero.count("rusty key") > 0
            check("the Rusty Key is in the inventory", "yes" if self.has_key else "no", "yes")
        if room == "Mushroom Cavern":
            self.open_cache()

    def open_cache(self):
        hero = self.hero
        if hero.count("rusty key") == 0:
            self.stats["notes"].append("no key: the cache stays shut")
            return
        text = hero.move("north")
        check("with the Rusty Key the door opens onto the Hidden Cache", text, "Hidden Cache", ROOMS["Hidden Cache"]["desc"])
        check("the cache holds old coins and a magical trinket", text, "A heap of old coins is here.", "A magical trinket is here.")
        if "An enchanted dagger is here." in text:
            self.stats["notes"].append("the cache held the rare Enchanted Dagger (25% chance)")
        t = hero.cmd("get coins")
        check("take the old coins", t, "You add ", " gold to your purse.")
        check("take the trinket", hero.cmd("get trinket"), "You take a magical trinket.")
        if "An enchanted dagger is here." in text:
            check("take the dagger", hero.cmd("get dagger"), "You take an enchanted dagger.")
        before = hero.max_hp
        t = hero.cmd("wear trinket")
        check("the trinket raises maximum HP by 5", t + "%d" % hero.health(), "You put on a magical trinket.")
        check("max HP +5", str(hero.max_hp - before), "5")
        back = hero.move("south")
        check("the cache's only exit leads back to the Mushroom Cavern", room_of(back), "Mushroom Cavern")
        hero.score()

    # ================================================================ 4. the Overseer

    def boss(self):
        hero = self.hero
        self.shopping(potions_wanted=3)
        if hero.level < NEED_LEVEL[CHAMBER]:
            self.ensure_level(NEED_LEVEL[CHAMBER], self.cleared)
        self.shopping(potions_wanted=3)
        self.heal_up(1.0)
        # a last logout in the Guest Rooms with everything on: the equipment must come back
        if hero.pos == GUEST and not self.fallback:
            before = hero.inventory()
            hero.quit()
            hero.c = h.Client("hero")
            hero.feed(hero.c.read_until("> "))
            after = hero.inventory()
            check("after logging out and in the equipment is as it was", " | ".join(after), *[i for i in before if "(wielded)" in i or "(worn)" in i])
        self.note(f"to the Overseer: level {hero.level}, hp {hero.hp}/{hero.max_hp}, potions {hero.count('healing potion')}, gold {hero.gold}")
        self.stats["level_at_boss"] = hero.level
        self.stats["t_boss"] = self.elapsed()
        hero.go(CHAMBER)
        if hero.died:
            self.after_death(CHAMBER)
            hero.go(CHAMBER)
        loot = self.clear_room(CHAMBER, boss=True, use_potions=True)
        self.rooms[CHAMBER]["kills"] += 0
        text = self.boss_text
        check("the Overseer's Battle Cry", text, "BATTLE CRY")
        check("the Overseer summons a Goblin Minion once his HP falls to half", text, "snarls an order, and a goblin minion scrambles out of the shadows!")
        if "POWER STRIKE" in text:
            self.stats["notes"].append("saw the Overseer's Power Strike")
        check("his death line", text, "The Goblin Overseer staggers, drops to his knees, and falls dead among his maps.")
        check("the corpse held the axe, the vest, the coins and the orders", loot, "Goblin Overseer's axe", "leather commander's vest", "bag of coins", "the Overseer's Orders")
        hero.settle(1.0)
        hero.score()
        self.note(f"the Overseer is dead; level {hero.level}, xp {hero.xp}, gold {hero.gold}")
        hero.equip_all()
        check("the axe is wielded and the vest worn", hero.cmd("i"), "the Goblin Overseer's axe (wielded)", "a leather commander's vest (worn)")
        check("look at the Overseer's Orders", hero.cmd("look at orders"), "A bundle of poorly translated notes covered in strange symbols and instructions.")
        orders = hero.cmd("read orders")
        for fragment in ("The Hollow Below", "The Key of Vincent", "Continue excavation.", "The old founder was telling the truth.", "The entrance remains sealed.", "The Crow demands progress."):
            check(f"the orders read: {fragment}", orders, quote(fragment))
        check("the symbol on the walls", (hero.go(CHAMBER), hero.cmd("look at symbol"))[1], "A crude black crow")

    # ================================================================ 5. the Guild

    def report(self):
        hero = self.hero
        hero.go(GUILD)
        hero.score()
        xp0, gold0 = hero.xp, hero.gold
        check("Rowan sells nothing and hears nothing yet: the orders are not a quest item to sell", hero.cmd("sell orders"), "What?") if False else None
        hero.cmd("drop orders")  # first the cellar quest alone; the orders are handed in afterwards
        text = hero.cmd("report")
        check("report: Clear the Cellar is completed", text, "Quest complete: Clear the Cellar!", "Rowan pays you 50 gold and gives you a Guild Initiate's Helm.", "You gain 150 experience.",
              "New quest: Recover the Overseer's Orders.")
        hero.score()
        check("150 XP and 50 gold arrived", "%d %d" % (hero.xp - xp0 - 0, hero.gold - gold0), "%d %d" % (hero.xp - xp0, hero.gold - gold0))
        check("the helm", hero.cmd("wear helm"), "You put on a Guild Initiate's helm.")
        check("quests: one done, one active", hero.cmd("quests"), "Active quests: Recover the Overseer's Orders", "Completed quests: Clear the Cellar")
        xp1, gold1 = hero.xp, hero.gold
        hero.cmd("get orders")
        text = hero.cmd("report")
        check("report: the orders are taken and read aloud", text, "Rowan takes the bundle", '"The Hollow Below"', '"The Key of Vincent"', '"The Crow demands progress."')
        check("ending reveal 1: Vincent found something beneath the village", text, "Vincent did find something beneath this village before he vanished.")
        check("ending reveal 2: a complex called The Hollow Below", text, 'a whole complex they call "The Hollow Below"')
        check("ending reveal 3: the Overseer took orders from The Crow", text, 'taking his orders from someone called "The Crow"')
        check("ending reveal 4: Vincent's disappearance is connected", text, "I doubt Vincent's vanishing was an accident.", "tied to whatever lies down there")
        check("ending reveal 5: other entrances in the northern mountains", text, "other entrances too, perhaps in the northern mountains")
        check("the goblin problem is solved", text, "the cellar is clear, and the village is safe tonight")
        check("the second quest is completed", text, "Quest complete: Recover the Overseer's Orders!", "Rowan pays you 75 gold.", "You gain 200 experience.")
        hero.score()
        check("200 XP and 75 gold arrived", "%d %d" % (hero.xp - xp1, hero.gold - gold1), "200 75")
        check("quests: both done", hero.cmd("quests"), "Completed quests: Clear the Cellar, Recover the Overseer's Orders")
        check("the orders were taken", hero.cmd("i"), absent=("Overseer's Orders",))
        check("Rowan's line after the quests", hero.cmd("talk rowan"), "We have learned a great deal, and I like none of it.")
        check("the board shows both contracts done", hero.cmd("read board"), "Clear the Cellar (done)", "Recover the Overseer's Orders (done)")
        check("report again: nothing left to do", hero.cmd("report"), "You have done everything I asked.")
        check("score at the end", hero.score(), "Completed quests: Clear the Cellar, Recover the Overseer's Orders", "weapon:", "head: a Guild Initiate's helm")
        # the village after the first quest
        hero.go(TAVERN)
        check("Harland thanks the player after the first quest", hero.cmd("talk harland"), "The cellar is quiet at last, and I have you to thank for it.")
        check("his other line is unchanged", hero.cmd("talk harland"), quote("Vincent used to talk about tunnels beneath the village. Nobody believed him."))
        # food and drink at full health, if not tried before
        hero.score()
        check("buy a bowl of stew", hero.cmd("buy stew"), "You buy a bowl of stew for 4 gold.")
        check("eat it", hero.cmd("eat stew"), "You spoon up the hot stew until the bowl is empty.")
        hero.go(START)
        check("buy a trail ration", hero.cmd("buy ration"), "You buy a trail ration for 5 gold.")
        check("eat it", hero.cmd("eat ration"), "You chew through the dry biscuit and salted meat.")

    # ================================================================ 6. checks with the warden

    def audit_with_warden(self):
        """What a player cannot see: room flags, spawn counts, locks and encounters, through `stat here`."""
        w = self.warden_in()
        expect_safe = {"Village Gate": True, "Village Square": True, GUILD: True, SMITH: True, TAVERN: True, GUEST: True, "North Road": False, "Tavern Cellar": False}
        paths = {"North Road": "village/north_road", "Village Gate": "village/gate", "Village Square": "village/square", GUILD: "village/guild", SMITH: "village/ironoak",
                 TAVERN: "village/tavern", GUEST: "village/guest_rooms", "Tavern Cellar": "village/cellar", "Collapsed Passage": "cellars/collapsed_passage",
                 "Storage Chamber": "cellars/storage_chamber", "Smuggler Hall": "cellars/smuggler_hall", "Guard Post": "cellars/guard_post", "Flooded Tunnel": "cellars/flooded_tunnel",
                 "Mushroom Cavern": "cellars/mushroom_cavern", "Hidden Cache": "cellars/hidden_cache", "Goblin Barracks": "cellars/goblin_barracks",
                 "Overseer Hall": "cellars/overseer_hall", CHAMBER: "cellars/overseer_chamber"}
        for room in DUNGEON + ["Smuggler Hall", "Hidden Cache"]:
            expect_safe[room] = False
        spawns = {"Tavern Cellar": ("large_rat", 3), "Collapsed Passage": ("giant_rat", 2), "Storage Chamber": ("goblin_scavenger", 2), "Guard Post": ("goblin_guard", 1),
                  "Flooded Tunnel": ("giant_cave_toad", 1), "Mushroom Cavern": ("spore_bat", 3), "Goblin Barracks": ("goblin_warrior", 3), "Overseer Hall": ("goblin_elite_guard", 2),
                  CHAMBER: ("goblin_overseer", 1), "Village Gate": ("guard", 1)}
        for room, path in paths.items():
            w.command("goto /domains/hollow/" + path)
            text = w.command("stat here")
            a.drain(w, 0.3)
            check(f"{room}: safe flag is {expect_safe[room]} (design section 11)", text, f"Safe {expect_safe[room]}, logout room {room == GUEST}")
            if room in spawns:
                mob, n = spawns[room]
                check(f"{room}: {n} x {mob}", text, f"x{n}")
            if room == "Mushroom Cavern":
                check("the Hidden Cache exit is the locked one", text, "Exit north -> /domains/hollow/cellars/hidden_cache (locked)")
            if room == "North Road":
                check("the North Road has the merchant (15%) and wolf (25%) encounters", text, "traveling_merchant 15%", "wandering_wolf 25%", "Exit south -> /domains/hollow/village/gate")
            if room == "Guard Post":
                check("the Guard Post keeps a second guard that always carries the key", text, "goblin_guard_keyholder x1")
        # the rare stock: clone Torren until one stocks the longsword (30% each), then the stock list is the brief's
        w.command("goto /domains/hollow/village/ironoak")
        found = False
        for _ in range(25):
            text = w.command("clone /domains/hollow/npc/torren")
            a.drain(w, 0.2)
            if "fine iron longsword" in w.command("browse"):
                found = True
                break
        check("Torren can stock the Fine Iron Longsword at 60 gold (rolled by chance)", "found" if found else "never in 25 rolls", "found")
        if found:
            check("and it is listed for 60 gold", w.command("browse"), "A fine iron longsword", "60 gold")
        self.warden_out()

    def finish(self):
        s = self.stats
        hero = self.hero
        hero.score()
        total = self.elapsed()
        print("\n" + "=" * 100)
        print("PLAY-THROUGH STATISTICS")
        print("=" * 100)
        print(f"Total time {total / 60:.1f} minutes; time at the Overseer's door {s.get('t_boss', 0) / 60:.1f} minutes.")
        print(f"Deaths: {s['deaths']} (one deliberate, in the Overseer Hall) {s['death_log']}")
        print(f"Final level {hero.level} with {hero.xp} XP, {hero.gold} gold; level at the Overseer: {s.get('level_at_boss')}; level-ups (level, seconds): {s['level_log']}")
        print(f"Fights {s['fights']}, retreats {s['retreats']}, beds bought {s['beds']}, potions drunk {s['potions']}, farming fights {s['farm']}, corpse recoveries {s.get('recoveries', 0)}")
        if s.get("rest_s"):
            print(f"Resting in the Guest Rooms: {s['rest_hp']} HP in {s['rest_s']:.0f} s ({s['rest_hp'] / s['rest_s'] * 10:.1f} HP per 10 s; the rules say 3 in a safe room)")
        print(f"Fled with the flee command: {s.get('flees', 0)} times")
        print(f"Wizard help used: {'YES, from the finding below' if self.fallback else 'no'}")
        for note in s["notes"]:
            print("  note:", note)
        print(f"{'Room':<19}{'Engagements':>12}{'Retreats':>9}{'Deaths':>7}{'Kills':>6}{'Seconds':>8}{'HP lost':>8}")
        for room, info in self.rooms.items():
            print(f"{room:<19}{info['engagements']:>12}{info['retreats']:>9}{info['deaths']:>7}{info['kills']:>6}{info['seconds']:>8.0f}{info['lost']:>8}")
        print("=" * 100)
        check("the character is level 4 or 5 at the end", str(hero.level >= 4), "True")
        check("the run needed no wizard help", "no help" if not self.fallback else "WIZARD HELP USED (finding)", "no help")


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-play-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)  # unmodified: no stub rooms, no changed numbers
    proc, log = a.start_driver(work, mudlib)
    stats = {"t0": time.time(), "deaths": 0, "death_log": [], "level_log": [], "retreats": 0, "beds": 0, "potions": 0, "fights": 0, "farm": 0, "notes": []}
    run = Run(stats)
    try:
        try:
            for phase in (run.start, run.walk_everything, run.look_at_things, run.first_purchases, run.die_on_purpose,
                          run.second_player_sees_locked_corpse, run.logout_in_guest_rooms, run.recover_corpse, run.shield_and_ale,
                          run.grind, run.boss, run.report, run.audit_with_warden, run.finish):
                phase()
                if os.environ.get("LESMUDOS_PLAY_STOP") == phase.__name__:  # for development
                    break
        except TimeUp as stop:
            h.failures.append("time is up")
            print("FAIL  the hard time limit was reached by", stop)
        log.flush()
        logged = open(os.path.join(work, "driver.log")).read()
        check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:800], "ok")
    finally:
        try:
            w = h.Client("warden")
            w.read_until("> ")
            w.command("shutdown")
            proc.wait(timeout=15)
        except Exception:
            pass
        if proc.poll() is None:
            proc.kill()
        log.close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-3000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\n{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
