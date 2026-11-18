#!/usr/bin/env python3
"""Walk-through test of the Greenvale Transit Corridor (Legends of Hollowmere): the road from Vincent's Hollow to Oakenfell.

Starts the driver against a temporary copy of the mudlib and checks the 122 rooms (100 road rooms in five segments, the Root
Warrens and the Forgotten Barrow) against docs/areas/transit-corridor-build.md and the integration brief:

  * the map: short names and exits of the 100 road rooms, band, area and reset data, creatures allowed for each zone;
  * a wizard walks every exit of all 122 rooms both ways, looks at every detail, reads every readable, talks to every person,
    asks every topic key, browses and buys at every shop;
  * the seven hidden ways: not listed and not walkable, found by looking at their detail, then walkable both ways;
  * the Chronicle: the five secret rooms and the two dungeons report for a character and not for a wizard;
  * the 29 resource nodes, the five Riverwatch patrols (at the start of the route; walking when a player stands on it);
  * danger bands in `stat here` and the warning line for a level 5 character;
  * the two connection points, walked by a new character from the Village Square to Oakenfell's Southgate;
  * the driver log has no errors.

Expectations are derived from the files and from the build document, or typed in from the integration brief where the brief is
the source (the node placements, the patrol routes, the hidden ways). Creatures do not attack in this test: the aggression of
the corridor's creatures is switched off in the test's own copy of the mudlib, so that a walk is not a fight.

Usage (from the repository root, after `dotnet build`):
    python3 tests/corridor_test.py            (default port 4828, or LESMUDOS_TEST_PORT)
    python3 tests/corridor_test.py static     (only the checks that need no game)
"""
import os
import re
import sys
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4828")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import city_infra as ci  # noqa: E402
import city_test as ct  # noqa: E402
import smoke_test as h  # noqa: E402

R = ct.R
body, lines_of, cap = ct.body, ct.lines_of, ct.cap
COR = "/domains/greenvale/corridor/"
VALE, OAK, HOLLOW = ci.VALE, ci.OAK, ci.HOLLOW
BUILD = os.path.join(ci.ROOT, "docs", "areas", "transit-corridor-build.md")

SEGMENTS = ["south_road", "eldergreen_edge", "green_road", "heartwood", "approach"]
DUNGEONS = {"root_warrens": 12, "barrow": 10}
AREAS = {"south_road": "The South Road", "eldergreen_edge": "Eldergreen Edge", "green_road": "The Green Road", "heartwood": "Heartwood",
         "approach": "The Oakenfell Approach", "root_warrens": "The Root Warrens", "barrow": "The Forgotten Barrow"}
CITY_AREAS = {"southgate": "Oakenfell", "market": "Oakenfell", "founders": "Oakenfell", "grove": "The Hidden Grove", "river": "Oakenfell",
              "guild": "Oakenfell", "beyond": "The Roads beyond Oakenfell"}
WORDS = {w_: n for n, w_ in enumerate("zero one two three four five six seven eight nine ten eleven twelve thirteen fourteen fifteen sixteen seventeen eighteen nineteen twenty".split())}
WORDS.update({"twenty-four": 24, "twenty-five": 25, "thirty": 30})
ROOM_KEY = re.compile(r"[mwds]\d+$")

# The brief (task B): from, direction, to, detail keys that find it, search chance, chronicle event, subject, name.
HIDDEN = [
    ("south_road/d2", "west", "south_road/s1", "west wall", 20, "discover.hidden", "dry-well", "the Dry Well"),
    ("eldergreen_edge/d2", "east", "eldergreen_edge/s1", "giant beech", 20, "discover.hidden", "hunters-hide", "the Hunter's Hide"),
    ("green_road/w3", "down", "green_road/s1", "hearth", 15, "discover.hidden", "watchtower-cellar", "the Watchtower Cellar"),
    ("heartwood/d2", "east", "heartwood/s1", "dead", 15, "discover.hidden", "heartwood-cache", "the Hidden Cache"),
    ("heartwood/w2", "down", "root_warrens/entrance", "hollow", 10, "discover.secret_dungeon", "root-warrens", "The Root Warrens"),
    ("heartwood/d3", "in", "barrow/entrance", "gap", 10, "discover.secret_dungeon", "forgotten-barrow", "The Forgotten Barrow"),
    ("approach/d1", "down", "approach/s1", "sump", 15, "discover.hidden", "sealed-culvert", "the Sealed Culvert"),
]
# The brief (task C): node file -> rooms that hold one.
NODES = {
    "south_road": {"berry_bush": ["w2", "d2"], "wildflowers": ["w4", "w5"], "thistle_patch": ["m06"]},
    "eldergreen_edge": {"thistle_patch": ["m01", "w1", "m10", "w5"], "herb_patch": ["m04", "m06", "w3"]},
    "green_road": {"herb_patch": ["w1", "w2", "w4", "w5"], "wildflowers": ["w1", "w5"]},
    "heartwood": {"herb_patch": ["m10", "w1", "w3"], "wildflowers": ["m10"]},
    "approach": {"wildflowers": ["m01", "w5"], "berry_bush": ["m02", "w4", "w5"], "herb_patch": ["w1", "w5"]},
}
NODE_KINDS = {"berry_bush": ("berry bush", 1), "herb_patch": ("herb patch", 2), "wildflowers": ("wildflowers", 1), "thistle_patch": ("thistle patch", 2)}
NODE_WORD = {"berry_bush": "bush", "herb_patch": "herb", "wildflowers": "wildflowers", "thistle_patch": "thistle"}
# The brief (task D): patrol file -> (segment, first room, last room); spawned in the first room.
PATROLS = {"patrol_south_road": ("south_road", 7, 11), "patrol_eldergreen_edge": ("eldergreen_edge", 5, 11), "patrol_green_road": ("green_road", 9, 11),
           "patrol_heartwood": ("heartwood", 7, 11), "patrol_approach": ("approach", 3, 7)}
BOSS_ROOMS = ["approach/w3", "approach/d2", "root_warrens/nest", "barrow/long_gallery", "barrow/borrowed_tomb"]
DUNGEON_CREATURES = {"root_warrens": {"tunnel_rat", "goblin_tunneler", "root_creeper", "warren_broodmother"},
                     "barrow": {"restless_spirit", "ancient_skeleton", "barrow_guardian", "fallen_knight"}}


# ------------------------------------------------------------------ the build document

def parse_build():
    """({room path: {short, zone, band, exits}}, {segment dir: {zone: set of creature ids}}, {creature id: level}) from the build document."""
    rooms, allowed, levels = {}, {}, {}
    text = open(BUILD).read().splitlines()
    cur = None
    for line in text:
        m = re.match(r"### .* directory `([^`]+)`", line)
        if m:
            cur = m.group(1).rstrip("/")
            continue
        m = re.match(r"\| `([a-z_]+)` \| ([^|]+) \| (\d+) \| ([^|]+) \|$", line)
        if m and cur is None:
            levels[m.group(1)] = int(m.group(3))
    known = set(levels)
    cur = None
    for line in text:
        m = re.match(r"### .* directory `([^`]+)`", line)
        if m:
            cur = m.group(1).rstrip("/")
            continue
        if cur is None:
            continue
        m = re.match(r"Creatures: Main road \(encounters\): (.*?)\. Woodland: (.*?)\. Deep: (.*)\.$", line)
        if m:
            ids = lambda s: set(re.findall(r"[a-z]+(?:_[a-z]+)+", s)) & known  # noqa: E731
            allowed[cur] = {"main": ids(m.group(1)), "woodland": ids(m.group(2)), "deep": ids(m.group(3)), "secret": set()}
            continue
        if not line.startswith("| `"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        key = cells[0].strip("`")
        if len(cells) < 5 or not ROOM_KEY.match(key):
            continue
        exits = {}
        for part in cells[4].split(";"):
            direction, _, target = part.strip().partition(" ")
            target = target.strip()
            exits[direction] = target.strip("`") if target.startswith("`") else f"{cur}/{target}"
        lo, hi = cells[3].split("-")
        rooms[f"{cur}/{key}"] = {"short": cells[1], "zone": cells[2], "band": (int(lo), int(hi)), "exits": exits}
    return rooms, allowed, levels


def room_names():
    """Every corridor room path, from the files."""
    result = []
    for seg in SEGMENTS + list(DUNGEONS):
        folder = os.path.join(ci.MUDLIB, "domains", "greenvale", "corridor", seg)
        for name in sorted(os.listdir(folder)):
            if name.endswith(".cs") and name != "area.cs":
                result.append(COR + seg + "/" + name[:-3])
    return result


def spawns_of(src, fn):
    """[(path, number)] of the AddSpawn / AddEncounter / AddNode calls in a source."""
    pattern = re.compile(r'\b' + fn + r'\(\s*"([^"]+)"(?:\s*,\s*(\d+))?\s*\)')
    return [(m.group(1), int(m.group(2) or 1)) for m in pattern.finditer(src.text)]


def band_of(src):
    m = re.search(r"\bDangerBand\(\s*(\d+)\s*,\s*(\d+)\s*\)", src.text)
    return (int(m.group(1)), int(m.group(2))) if m else None


def zone_of(path):
    seg, room = path[len(COR):].split("/")
    return seg, ({"m": "main", "w": "woodland", "d": "deep", "s": "secret"}[room[0]] if seg in SEGMENTS else "dungeon")


# ------------------------------------------------------------------ part 1: static checks

def check_static(doc, allowed, levels, rooms, others):
    R.section("1. The map matches the build document")
    paths = room_names()
    road = [p for p in paths if p[len(COR):].split("/")[0] in SEGMENTS]
    R.ok(len(doc) == 100 and len(road) == 100, "map: the build document and the files both have 100 road rooms", f"document {len(doc)}, files {len(road)}")
    for dungeon, count in DUNGEONS.items():
        have = [p for p in paths if p.startswith(COR + dungeon + "/")]
        R.ok(len(have) == count, f"map: {dungeon} has {count} rooms", f"{len(have)}")
    R.ok(len(paths) == 122 and all(p in rooms for p in paths), "map: 122 room files, each a /std/room", f"{len(paths)} files, {len(rooms)} rooms")
    for path, want in doc.items():
        src = rooms.get(path)
        if not R.ok(src is not None, f"map: room file exists: {path}"):
            continue
        R.ok(src.short == want["short"], f"map: short name of {path}", f"document: {want['short']!r}\nfile:     {src.short!r}")
        for direction, dest in want["exits"].items():
            R.ok(src.exits.get(direction) == dest, f"map: {path} exit {direction}", f"document: {dest}\nfile:     {src.exits.get(direction)}")
        extra = sorted(set(src.exits) - set(want["exits"]))
        R.ok(not extra, f"map: {path} has no exit the document lacks", f"extra: {[(d, src.exits[d]) for d in extra]}")
        R.ok(band_of(src) == want["band"], f"bands: {path} has the {want['zone']} band {want['band']} of the document", f"file: {band_of(src)}")
    for path in paths:
        src = rooms[path]
        seg, zone = zone_of(path)
        if seg in DUNGEONS:
            want = (9, 10) if seg == "barrow" or path.endswith("/nest") else (7, 8)
            R.ok(band_of(src) == want, f"bands: {path} ({seg}) has band {want}", f"file: {band_of(src)}")
    for rel in BOSS_ROOMS:
        R.ok(int(rooms[COR + rel].get("ResetSeconds", "0") or 0) == 1800, f"reset: the boss room {rel} resets every 1800 s", f"ResetSeconds = {rooms[COR + rel].get('ResetSeconds')}")

    R.section("1b. Exits come back")
    for path in paths:
        for direction, dest in rooms[path].exits.items():
            dsrc = rooms.get(dest) or ci.Source(dest)
            R.ok(dsrc.exists, f"exits: {path} {direction} leads to a room file: {dest}")
            if dest.startswith(COR) or dest.startswith(VALE):
                # a hidden way is an exit too: the secret rooms and the dungeons are reached by it
                back = list(dsrc.exits.values()) + re.findall(r'HiddenExit\(\s*"\w+"\s*,\s*"([^"]+)"', dsrc.text)
                R.ok(path in back, f"exits: {dest} has an exit back to {path}", f"exits of {dest}: {dsrc.exits}")

    R.section("1c. Area data")
    for seg, name in AREAS.items():
        src = ci.Source(COR + seg + "/area")
        R.ok(src.exists and src.base == "/std/area", f"area: {seg}/area.cs exists and inherits /std/area")
        R.ok(re.search(r'AreaName\s*=\s*"' + re.escape(name) + '"', src.text) is not None, f"area: {seg} is '{name}'")
        R.ok(re.search(r'RegionName\s*=\s*"Greenvale"', src.text) is not None, f"area: {seg} is in the region Greenvale")
    for district, name in CITY_AREAS.items():
        src = ci.Source(OAK + district + "/area")
        R.ok(src.exists and src.base == "/std/area", f"area: oakenfell/{district}/area.cs exists and inherits /std/area")
        R.ok(re.search(r'AreaName\s*=\s*"' + re.escape(name) + '"', src.text) is not None, f"area: oakenfell/{district} is '{name}'")
        R.ok(re.search(r'RegionName\s*=\s*"Greenvale"', src.text) is not None, f"area: oakenfell/{district} is in the region Greenvale")

    R.section("1d. Creatures allowed for each zone")
    for path in paths:
        src = rooms[path]
        seg, zone = zone_of(path)
        if seg in DUNGEONS:
            ok_set = DUNGEON_CREATURES[seg]
        else:
            ok_set = allowed[COR[:-1] + "/" + seg][zone]
        for fn in ("AddSpawn", "AddEncounter"):
            for target, number in spawns_of(src, fn):
                R.ok(os.path.isfile(os.path.join(ci.MUDLIB, target.lstrip("/") + ".cs")), f"creatures: {path} {fn} names an existing file: {target}")
                if "/corridor/mob/" not in target:
                    continue
                cid = target.rsplit("/", 1)[1]
                R.ok(cid in ok_set, f"creatures: {path} ({zone}) may have {cid}", f"the build document allows here: {sorted(ok_set) or 'no creature'}")
                if zone == "main" and seg in SEGMENTS:
                    R.ok(fn == "AddEncounter", f"creatures: {path} (main road) meets {cid} only as an encounter, not as a standing spawn")
        for target, pct in [(m.group(1), int(m.group(2))) for m in re.finditer(r'AddEncounter\(\s*"([^"]+)"\s*,\s*(\d+)\s*\)', src.text)]:
            R.ok(5 <= pct <= 15, f"creatures: {path} encounter {target.rsplit('/', 1)[1]} is 5 to 15 percent", f"{pct}")
        people = [t for t, _ in spawns_of(src, "AddSpawn") if "/npc/" in t and "/patrol_" not in t]
        beasts = [t for t, _ in spawns_of(src, "AddSpawn") + spawns_of(src, "AddEncounter") if "/corridor/mob/" in t]
        R.ok(not (people and beasts), f"creatures: {path} holds people and creatures together", f"people {people}, creatures {beasts}")
    # the dungeons' own counts
    nest = sum(n for t, n in spawns_of(rooms[COR + "root_warrens/nest"], "AddSpawn") if t.endswith("warren_broodmother"))
    R.ok(nest == 1 and sum(n for p in paths if "root_warrens" in p for t, n in spawns_of(rooms[p], "AddSpawn") if t.endswith("warren_broodmother")) == 1,
         "creatures: exactly one Warren Broodmother, in the nest")
    knights = sum(n for p in paths if "/barrow/" in p for t, n in spawns_of(rooms[p], "AddSpawn") if t.endswith("fallen_knight"))
    guards = sum(n for p in paths if "/barrow/" in p for t, n in spawns_of(rooms[p], "AddSpawn") if t.endswith("barrow_guardian"))
    R.ok(knights == 1 and guards in (1, 2), "creatures: one Fallen Knight and one or two Barrow Guardians", f"knights {knights}, guardians {guards}")
    leaders = sum(n for p in paths for t, n in spawns_of(rooms[p], "AddSpawn") if t.endswith("bandit_leader"))
    R.ok(leaders == 1 and any(t.endswith("bandit_leader") for t, _ in spawns_of(rooms[COR + "approach/w3"], "AddSpawn")), "creatures: one Bandit Leader, in the Bandit Camp")

    R.section("1e. Hidden ways, nodes, patrols and discoveries, from the sources")
    for frm, direction, to, key, chance, event, subject, name in HIDDEN:
        src = rooms[COR + frm]
        m = re.search(r'HiddenExit\(\s*"' + direction + r'"\s*,\s*"' + re.escape(COR + to) + r'"\s*,\s*"([^"]*)"\s*,\s*(\d+)\s*,\s*"([^"]+)"\s*\)', src.text)
        if not R.ok(m is not None, f"hidden: {frm} has HiddenExit {direction} to {to}"):
            continue
        R.ok(int(m.group(2)) == chance, f"hidden: {frm} {direction} has search chance {chance}", f"{m.group(2)}")
        hints = [k.strip() for k in m.group(1).split(",")]
        details, _, _ = src.room_texts()
        R.ok(key in hints and key in details, f"hidden: {frm} hint includes the detail key '{key}', which exists in the room")
        R.ok(all(k in details for k in hints), f"hidden: every hint key of {frm} is a detail of the room", f"missing: {[k for k in hints if k not in details]}")
        R.ok(direction not in src.exits, f"hidden: {frm} has no ordinary exit {direction}")
        R.ok("{cmd}" + direction + "{/}" in m.group(3), f"hidden: the found line of {frm} names {{cmd}}{direction}{{/}}", m.group(3))
        far = ci.Source(COR + to)
        R.ok(frm in [p[len(COR):] for p in far.exits.values() if p.startswith(COR)] or any(p == COR + frm for p in far.exits.values()),
             f"hidden: {to} has an ordinary exit back to {frm}", f"exits of {to}: {far.exits}")
        R.ok(re.search(r'"' + event + r'"[^;]*"' + re.escape(subject) + r'"', far.text) is not None, f"chronicle: {to} reports {event} with subject '{subject}'")
        R.ok(('["name"] = "' + name + '"') in far.text, f"chronicle: {to} reports the name '{name}'")
        R.ok("override void Init()" in far.text and "ThisPlayer()" in far.text and "Interactive(who)" in far.text, f"chronicle: {to} reports from Init, as hidden_cache.cs does")
    for seg, kinds in NODES.items():
        for kind, where in kinds.items():
            for r in where:
                src = rooms[COR + f"{seg}/{r}"]
                R.ok((COR + "obj/" + kind) in [t for t, _ in spawns_of(src, "AddNode")], f"nodes: {seg}/{r} has AddNode {kind}")
    total = sum(len(spawns_of(rooms[p], "AddNode")) for p in paths)
    R.ok(total == sum(len(w) for k in NODES.values() for w in k.values()), "nodes: no room has a node the brief does not list", f"{total} AddNode calls in the files")
    for kind, (name, diff) in NODE_KINDS.items():
        src = ci.Source(COR + "obj/" + kind)
        R.ok(src.base == "/std/node", f"nodes: {kind} inherits /std/node")
        R.ok(re.search(r'Kind\s*=\s*"' + name + '"', src.text) and re.search(r"Difficulty\s*=\s*" + str(diff) + r";", src.text)
             and re.search(r"Charges\s*=\s*3;", src.text) and re.search(r"RegrowSeconds\s*=\s*600;", src.text),
             f"nodes: {kind} is a '{name}', difficulty {diff}, 3 charges, regrows in 600 s")
        R.ok("FullDesc" in src.text and "PickedDesc" in src.text, f"nodes: {kind} has a full and a picked-clean description")
        R.ok(src.text.count("class Cn") == 1, f"nodes: {kind} has a class with the prefix Cn")
    for name, (seg, first, last) in PATROLS.items():
        src = ci.Source(COR + "npc/" + name)
        route = re.findall(r'"(/domains/[^"]+)"', src.text[src.text.index("SetRoute"):])
        want = [f"{COR}{seg}/m{n:02d}" for n in range(first, last + 1)]
        R.ok(src.base == "/std/patrol" and "class Cp" in src.text, f"patrols: {name} inherits /std/patrol and has a class with the prefix Cp")
        R.ok(route == want, f"patrols: {name} walks {seg} m{first:02d} to m{last:02d}, in order", f"file: {route}")
        R.ok(re.search(r"\},\s*45\s*\)", src.text) is not None, f"patrols: {name} has a pace of 45 s and does not loop")
        R.ok("DefendPlayers = true" in src.text, f"patrols: {name} defends players")
        R.ok(src.get("ShortDesc") == "a Riverwatch patrol" and bool(src.get("LongDesc")), f"patrols: {name} is 'a Riverwatch patrol' with a description")
        R.ok(len(src.chats()) == 2, f"patrols: {name} has two chat lines", f"{len(src.chats())}")
        R.ok(2 <= len(src.topics()) <= 3, f"patrols: {name} has two or three topics", f"{len(src.topics())}")
        R.ok("Peaceful = false" not in src.text, f"patrols: {name} is peaceful")
        start = rooms[COR + f"{seg}/m{first:02d}"]
        R.ok([t for t, _ in spawns_of(start, "AddSpawn")].count(COR + "npc/" + name) == 1, f"patrols: {seg}/m{first:02d} spawns {name}")
        texts = " ".join(ci.unescape(s) for s in re.findall(ci.STR, src.text))
        R.ok("silver eye" in texts.lower() and "green" in texts.lower(), f"patrols: {name} wears the green-and-grey with the silver eye")
        topics = src.topics()
        for i, (keywords, _) in enumerate(topics):
            for raw in keywords.split(","):
                j = ci.ask_model(topics, raw.strip())
                R.ok(j == i, f"topics: {name}: 'ask patrol about {raw.strip()}' reaches its own topic", f"topic #{i + 1} [{keywords}] is shadowed by #{(j or 0) + 1}")

    R.section("1f. Canon: the sources never say what they must not")
    for path, src in sorted({**rooms, **others}.items()):
        if not path.startswith(COR):
            continue
        for literal in re.findall(ci.STR, src.text):
            t = ci.unescape(literal)
            if re.search(r"circle of ash|ash circle", t, re.I):
                R.ok(False, f"canon: the Circle of Ash is named in {path}", t[:140])
            if re.search(r"informer|informant|snitch", t, re.I):
                R.ok(False, f"canon: the informer is mentioned in {path}", t[:140])
            if "> " in t:
                R.ok(False, f"text: '> ' inside game text in {path}", t[:140])
        if src.is_creature:
            for field in ("Name", "ShortDesc"):
                R.ok("{" not in src.get(field), f"text: no colour tag in {field} of {path}", f"{field} = {src.get(field)!r}")
            R.ok(not any("{" in i for i in src.ids), f"text: no colour tag in the Ids of {path}")
    R.ok(True, "canon: scanned every string of the corridor's files")

    ct.check_shadowed_topics({p: s for p, s in others.items() if p.startswith(COR)})


# ------------------------------------------------------------------ part 2: the running game

def calm(mudlib):
    """The corridor's creatures do not attack in this test (a walk is not a fight): their aggression is off in the test's own copy."""
    folder = os.path.join(mudlib, "domains", "greenvale", "corridor", "mob")
    for name in os.listdir(folder):
        path = os.path.join(folder, name)
        text = open(path).read()
        open(path, "w").write(re.sub(r"Aggressive\s*=\s*true\s*;", "Aggressive = false;", text))


class Walker(ct.Walker):
    """The city's walker, with the corridor's differences: creatures are not people, hidden exits are listed to a wizard, beds heal."""

    def __init__(self, wiz, doc, rooms, others):
        super().__init__(wiz, doc, rooms, others)
        self.spawn_checks = 0

    def wolf(self, sess, out):
        return None

    def visit(self, path):
        super().visit(path)
        self.stat_checks(path)

    def check_listing(self, path, src, look):
        match = re.search(r"Obvious exits: (.*)", look)
        text = match.group(1).replace("\r", "") if match else ""
        hidden = re.findall(r"(\w+) \(hidden\)", text)
        text = text.replace(" (hidden)", "")
        shown = re.findall(r"(\w+)(?: \(to [^)]*\))?(?:, |$)", text)
        mine = sorted(re.findall(r'HiddenExit\(\s*"(\w+)"', src.text))
        R.ok(sorted(hidden) == mine, f"walk: a wizard sees the hidden exits of {path} marked", f"marked: {sorted(hidden)}\nfile:   {mine}")
        R.ok(sorted(shown) == sorted(list(src.exits) + mine), f"walk: the exits that {path} lists are the exits in its file", f"listed: {sorted(shown)}\nfile:   {sorted(list(src.exits) + mine)}")

    def check_details(self, path, src):
        """Every detail and readable is reachable by at least one of its keys (an object of the room may answer to one of them first)."""
        w = self.w
        details, readables, dupes = src.room_texts()
        for keys, first, second in dupes:
            R.ok(False, f"details: {path}: keys {keys} are defined twice; the later definition replaces the earlier",
                 f"earlier: [{first}]\nlater:   [{second}]")
        for fn, args in src.calls:
            if fn not in ("AddDetail", "AddReadable"):
                continue
            keys = ci.keys_of(args[0])
            if not keys:
                R.ok(False, f"details: {path}: a {fn} without a key")
                continue
            # What `look at` shows for a key is the last AddDetail that named it, else the first AddReadable's own look text.
            mine = [k for k in keys if fn == "AddReadable" or ci.norm(ci.render(details.get(k, ""))) == ci.norm(ci.render(args[1]))]
            reached, last = None, ""
            for key in mine:
                out = self.say(w, "look at " + key)
                text = body(out)
                if text and not any(text.startswith(n) for n in ct.NO_SUCH) and ci.norm(ci.render(details.get(key, ""))) in ci.norm(text):
                    reached = key
                    break
                last = text
            R.ok(reached is not None, f"details: {path}: look at '{keys[0]}' reaches the room's text" + ("" if reached in (None, keys[0]) else f" (by '{reached}')"),
                 f"none of {mine} shows it; last answer: {last[:200]!r}")
            if fn == "AddReadable":
                got, last = None, ""
                for key in [k for k in keys if readables.get(k) == args[1]]:
                    out = self.say(w, "read " + key)
                    text = body(out)
                    if text and not any(text.startswith(n) for n in ct.NO_SUCH) and ci.norm(ci.render(args[1])) in ci.norm(text):
                        got = key
                        break
                    last = text
                R.ok(got is not None, f"readables: {path}: read '{keys[0]}' shows the room's text" + ("" if got in (None, keys[0]) else f" (by '{got}')"), f"last answer: {last[:200]!r}")

    def exits(self, path):
        """Every ordinary exit both ways. The way back from a secret room is the hidden exit of the other room: hidden_ways tests it."""
        src = self.src(path)
        w = self.w
        for direction, dest in src.exits.items():
            dsrc = self.src(dest)
            self.say(w, "goto " + path, city=False)
            out = self.say(w, direction)
            if not R.ok(dsrc.short in lines_of(out), f"exits: {path} {direction} leads to '{dsrc.short}' ({dest})", f"first lines: {lines_of(out)[:2]}\n{body(out)[:200]!r}"):
                continue
            backs = [d for d, p in dsrc.exits.items() if p == path]
            if not backs:
                R.ok(re.search(r'HiddenExit\(\s*"\w+"\s*,\s*"' + re.escape(path) + '"', dsrc.text) is not None, f"exits: {dest} has a way back to {path} (ordinary or hidden)", f"exits of {dest}: {dsrc.exits}")
                continue
            typed = ct.COMPASS.get(backs[0], backs[0])
            out = self.say(w, typed)
            R.ok(src.short in lines_of(out), f"exits: '{typed}' in {dest} returns to '{src.short}' ({path})", f"first lines: {lines_of(out)[:2]}\n{body(out)[:200]!r}")

    def stat_checks(self, path):
        src = self.src(path)
        out = self.say(self.w, "stat here", city=False).replace("\r", "")
        lo, hi = None, None
        m = re.search(r"Danger band (\d+)-(\d+)", out)
        want = band_of(src)
        R.ok(m is not None and want is not None and (int(m.group(1)), int(m.group(2))) == want, f"bands: stat here in {path} shows band {want}", f"stat: {out[-300:]!r}")
        R.ok("(from the area)" not in out, f"bands: the band of {path} is the room's own, not the area's")
        reset = src.get("ResetSeconds")
        if reset:
            R.ok(f"reset every {reset}s" in out, f"reset: {path} resets every {reset} s", f"stat: {out[:200]!r}")
        for target, number in spawns_of(src, "AddSpawn") + spawns_of(src, "AddNode"):
            if "/mob/" in target:
                R.ok(os.path.isfile(os.path.join(ci.MUDLIB, target.lstrip("/") + ".cs")), f"creatures: {path} spawns {target.rsplit('/', 1)[1]}")
            alive = re.search(re.escape(f"Spawn {target} x{number}:") + r" (\d+) alive", out)
            R.ok(alive is not None and int(alive.group(1)) == number, f"spawns: {path} holds {number} of {target.rsplit('/', 1)[1]}", f"stat: {out[:500]!r}")
        for target, pct in re.findall(r'AddEncounter\(\s*"([^"]+)"\s*,\s*(\d+)', src.text):
            R.ok(f"Encounter {target} {pct}%" in out, f"spawns: {path} lets {target.rsplit('/', 1)[1]} wander in at {pct}%", f"stat: {out[:500]!r}")
        for node in [t for t, _ in spawns_of(src, "AddNode")]:
            self.node(path, node)

    def node(self, path, node):
        kind = node.rsplit("/", 1)[1]
        name, diff = NODE_KINDS[kind]
        nsrc = self.src(node)
        look = self.say(self.w, "look", city=False)
        R.ok(cap(nsrc.short) + " is here." in look.replace("\r", ""), f"nodes: {path} shows '{nsrc.short}'", f"look: {body(look)[-300:]!r}")
        out = self.say(self.w, "stat " + NODE_WORD[kind], city=False)
        R.ok(f"Node: kind '{name}', charges 3/3, regrow 600s, difficulty {diff}" in out, f"nodes: {path} {kind} is a '{name}' with 3 charges, regrow 600 s, difficulty {diff}", f"stat: {body(out)[-300:]!r}")
        full = re.search(r'FullDesc\s*=\s*"((?:[^"\\]|\\.)*)"', nsrc.text)
        shown = self.say(self.w, "look at " + NODE_WORD[kind], city=False)
        R.ok(full is not None and ci.norm(ci.unescape(full.group(1))) in ci.norm(shown), f"nodes: look at {NODE_WORD[kind]} in {path} shows the full description", f"got: {body(shown)[:200]!r}")

    def check_people(self, path, src, look):
        w = self.w
        seen = set()
        for args in src.of("AddSpawn"):
            if args[0] in seen:
                continue
            seen.add(args[0])
            person = self.src(args[0])
            if not person.exists or person.base in ("/std/monster", "/std/patrol", "/std/node"):
                continue
            if not person.is_creature:
                R.ok(cap(person.short) in look or cap(person.short) in ci.norm(look), f"things: '{person.short}' lies in {path}")
                continue
            name = person.name
            if not R.ok(person.short and cap(person.short) in look.replace("\r", ""), f"people: {name} is listed in {path}",
                        f"expected '{cap(person.short)} is here.' in:\n{body(look)[-400:]}"):
                continue
            lower = name.lower()
            R.ok(bool(person.get("LongDesc")), f"people: {name} has a description")
            out = self.say(w, "look at " + lower)
            R.ok(ci.norm(ci.render(person.get("LongDesc"))) in ci.norm(out), f"people: look at {lower} shows the description ({path})", f"got: {body(out)[:200]!r}")
            chats = person.chats()
            R.ok(chats, f"people: {name} ({args[0]}) has something to say")
            for i, line in enumerate(chats):
                out = self.say(w, "talk " + lower)
                text = ci.norm(out)
                R.ok(f'{name} says: "' in text and ci.norm(ci.render(line)) in text, f"people: talk {lower} line {i + 1} ({path})", f"expected: {line[:120]!r}\ngot: {body(out)[:240]!r}")
            topics = person.topics()
            for i, (keywords, line) in enumerate(topics):
                key = keywords.split(",")[0].strip()
                out = self.say(w, f"ask {lower} about {key}")
                text = ci.norm(out)
                shrug = "I can't tell you anything about that" in text or "shrugs" in text
                R.ok(not shrug, f"people: ask {lower} about '{key}' is not answered with the don't-know line ({path})", f"got: {body(out)[:240]!r}")
                j = ci.ask_model(topics, key)
                if j is not None and not shrug:
                    R.ok(ci.norm(ci.render(topics[j][1])) in text, f"people: ask {lower} about '{key}' gives the line the matching code predicts ({path})",
                         f"predicted topic #{j + 1}: {topics[j][1][:120]!r}\ngot: {body(out)[:240]!r}")
            if person.base == "/std/shopkeeper" and person.of("AddStock"):
                self.shop(path, person, lower)

    def shop(self, path, person, lower):
        w = self.w
        stock = self.stock(person)
        out = self.say(w, "browse")
        listed = re.findall(r"^\s+(.+?)\s{2,}(\d+) gold\s*$", out.replace("\r", ""), re.M)
        want = [(cap(item.short), str(price)) for _, item, price in stock]
        R.ok(listed == want, f"shops: browse at {person.name} ({path}) lists exactly the stock at the file's prices", f"file:   {want}\nbrowse: {listed}")
        # A price named in a topic about an item is the price of that item.
        for keywords, line in person.topics():
            for item_path, item, price in stock:
                words = {w_.lower() for w_ in re.findall(r"[A-Za-z]+", item.name)} | {i.lower() for i in item.ids}
                if words & {k.strip() for k in keywords.lower().split(",")}:
                    for number in re.findall(r"(\d+|[a-z]+(?:-[a-z]+)?) gold", ci.render(line).lower()):
                        value = int(number) if number.isdigit() else WORDS.get(number)
                        if value is not None:
                            R.ok(value == price, f"shops: {person.name}'s topic [{keywords}] names the price of {item.name} ({number}); the stock says {price}", line[:200])
        self.ensure_gold(sum(p for _, _, p in stock) + 10)
        for item_path, item, price in sorted(stock, key=lambda s: s[2]):
            if not item.exists:
                R.ok(False, f"shops: stock file exists: {item_path}")
                continue
            before = self.gold()
            word = "bed" if item_path.endswith("rest_bed") else item.name.lower()
            out = self.say(w, "buy " + word)
            if item_path.endswith("rest_bed"):
                R.ok("points you to a settle by the kitchen fire" in out, f"shops: buy bed at {person.name} ({path}) puts the buyer to bed", f"got: {body(out)[:240]!r}")
                R.ok(before - self.gold() == price, f"shops: the bed costs {price} gold", f"gold {before} -> {self.gold()}")
                continue
            R.ok(f"You buy {item.short} for {price} gold." in out, f"shops: buy {word} at {person.name} ({path})", f"got: {body(out)[:240]!r}")
            R.ok(before - self.gold() == price, f"shops: buying {word} costs {price} gold", f"gold {before} -> {self.gold()}")
            inv = self.say(w, "i")
            R.ok(item.short in inv, f"shops: the {word} arrives in the inventory", f"inventory: {body(inv)[:240]!r}")
            self.say(w, "sell " + word, city=False)  # so that a later room's details are not answered by the wizard's own purchase


def walk_exits(wk, paths):
    """A wizard walks every ordinary exit of every room, and comes back by the exit the other room has."""
    R.section("3. A wizard walks every exit of the 122 rooms both ways")
    for path in paths:
        wk.exits(path)


def hidden_ways(wk, game, probe_holder):
    R.section("6. The seven hidden ways, and what the Chronicle makes of them")
    w = wk.w
    p = ci.Session("finder", archetype="scout")
    p.cmd("look")
    w.cmd("goto " + HOLLOW + "village/square")
    w.cmd("clone /domains/citytest/probe")
    w.cmd("drop probe")
    R.ok("probe" in wk.say(p, "get probe"), "hidden: the finder picks up the probe")
    base = renown_of(p)
    # Wizards make no history: the wizard went to all seven places in the walk and the ledger knows nothing of it.
    R.ok(base == 0, "chronicle: a new character starts with no renown", f"{base}")
    gains = {}
    last_renown = 0
    for frm, direction, to, key, chance, event, subject, name in HIDDEN:
        src = ci.Source(COR + frm)
        far = ci.Source(COR + to)
        tag = f"{frm} {direction}"
        out = wk.say(p, "tele " + COR + frm)
        R.ok(src.short in lines_of(out), f"hidden: the finder is brought to {frm}", f"{lines_of(out)[:2]}")
        exits_line = re.search(r"Obvious exits: (.*)", out)
        R.ok(exits_line is not None and not re.search(r"\b" + direction + r"\b", exits_line.group(1)), f"hidden: {tag} is not listed before it is found", exits_line.group(1) if exits_line else out[-200:])
        out = wk.say(p, direction)
        R.ok("What?" in out and far.short not in out, f"hidden: {tag} is not walkable before it is found (the word is not a command)", f"got: {body(out)[:200]!r}")
        # the other detail keys: the first is enough, but the line a reader finds must be the room's
        found_line = re.search(r'HiddenExit\([^;]*?"((?:[^"\\]|\\.)*)"\s*\)\s*;', src.text, re.S)
        out = wk.say(p, "look at " + key)
        want = ci.norm(ci.render(ci.unescape(found_line.group(1)))) if found_line else ""
        R.ok(want and want in ci.norm(out), f"hidden: looking at '{key}' in {frm} finds the way, with the room's found line", f"want: {want[:160]!r}\ngot: {ci.norm(out)[-300:]!r}")
        out = wk.say(p, "look")
        listed = re.search(r"Obvious exits: (.*)", out)
        R.ok(listed is not None and re.search(r"\b" + direction + r"\b", listed.group(1)), f"hidden: {tag} is listed once it is found", listed.group(1) if listed else out[-200:])
        out = wk.say(p, direction)
        R.ok(far.short in lines_of(out), f"hidden: {tag} is walkable once found: '{far.short}'", f"{lines_of(out)[:2]}")
        time.sleep(0.4)
        got = renown_of(p)
        gain = got - last_renown
        last_renown = got
        if event == "discover.hidden":
            R.ok(gain == 30, f"chronicle: finding {far.short} reports {event} for a character (renown +30)", f"renown +{gain}")
        else:
            R.ok(gain >= 30 and gains.setdefault(event, gain) == gain, f"chronicle: entering {far.short} reports {event} for a character (renown +{gains.get(event, gain)}, the same for both dungeons)", f"renown +{gain}")
        back = next((d for d, t in far.exits.items() if t == COR + frm), None)
        out = wk.say(p, back or "?")
        R.ok(src.short in lines_of(out), f"hidden: the way back from {to} ('{back}') returns to '{src.short}'", f"{lines_of(out)[:2]}")
        # a second look finds nothing new, and is not reported twice
        wk.say(p, "look at " + key)
        out = wk.say(p, direction)
        R.ok(far.short in lines_of(out), f"hidden: {tag} still works the second time")
        wk.say(p, back or "?")
        time.sleep(0.3)
    R.ok(renown_of(p) == last_renown, "chronicle: a second visit earns nothing more (a character is credited once)", f"{renown_of(p)} after {last_renown}")
    # The Chronicle: a hidden room earns renown (checked above, +30 each); a secret dungeon is also an entry of the Chronicle itself.
    wk.say(p, "tele " + HOLLOW + "village/guild")
    about = wk.say(p, "chronicle about finder")
    R.ok("Finder appears in 2 entries" in about, "chronicle: the Chronicle holds the two dungeon discoveries as entries about the finder", body(about)[:300])
    entries = {}
    for entry_id in sorted({int(i) for i in re.findall(r"#(\d+)", about)}):
        entries[entry_id] = wk.say(p, f"chronicle {entry_id}")
    joined = " ".join(ci.norm(t) for t in entries.values())
    for frm, direction, to, key, chance, event, subject, name in HIDDEN:
        if event == "discover.secret_dungeon":
            R.ok(re.search(r"Finder discovered " + re.escape(name) + r" beneath Heartwood", joined) is not None and "Where: Heartwood" in joined,
                 f"chronicle: the entry for {name} says it lies beneath Heartwood", joined[:600])
    R.ok(all("Where:" in t for t in entries.values()), "chronicle: each entry names its place")
    nowiz = wk.say(w, "chronicle about wiz")
    R.ok("does not yet appear" in nowiz, "chronicle: the wizard appears in no entry", body(nowiz)[:200])
    p.cmd("quit")


def renown_of(sess):
    m = re.search(r"Renown: (\d+)", sess.cmd("renown"))
    return int(m.group(1)) if m else 0


def wizards_make_no_history(wk, paths):
    R.section("5. A wizard in the secret rooms and dungeons makes no history")
    w = wk.w
    for frm, direction, to, key, chance, event, subject, name in HIDDEN:
        out = w.cmd("goto " + COR + to)
        R.ok("Wizards make no history" in out, f"chronicle: a wizard in {to} is told that wizards make no history", f"{body(out)[:200]!r}")
    R.ok("Renown: 0" in w.cmd("renown"), "chronicle: the wizard has no renown")
    w.cmd("goto " + HOLLOW + "village/guild")
    about = w.cmd("chronicle about wiz")
    R.ok("does not yet appear" in about, "chronicle: the wizard appears in no entry", body(about)[:200])


def bands_warning(wk):
    R.section("7. The warning for a level 5 character")
    w = wk.w
    w.cmd("setlevel 5")
    cases = [
        # (route of room names after the first, which steps warn)
        ("eldergreen_edge", ["m03", "w1", "w2", "w3", "d1"], {"d1"}),
        ("south_road", ["m03", "w1", "w2", "w3", "d1", "d2", "d3"], set()),
        ("approach", ["m02", "m03", "w1", "w2", "w3", "d1"], {"w1"}),
        ("heartwood", ["m01", "m02", "m03", "w1"], set()),
    ]
    for seg, route, warns in cases:
        out = w.cmd("goto " + COR + f"{seg}/{route[0]}")
        here = route[0]
        # entering by goto does not warn
        R.ok("dangerous than you" not in out, f"warning: entering {seg}/{here} by goto does not warn")
        for nxt in route[1:]:
            src = ci.Source(COR + f"{seg}/{here}")
            direction = next((d for d, t in src.exits.items() if t == COR + f"{seg}/{nxt}"), None)
            if direction is None:
                R.ok(False, f"warning: {seg}/{here} has no exit to {nxt}")
                break
            out = w.cmd(direction)
            has = "this ground is more dangerous than you are ready for." in out
            lo = band_of(ci.Source(COR + f"{seg}/{nxt}"))[0]
            R.ok(has == (nxt in warns), f"warning: level 5 stepping {here} to {nxt} in {seg} (low end {lo}) is {'' if nxt in warns else 'not '}warned", f"typed {direction}: {body(out)[-200:]!r}")
            R.ok(len(re.findall("more dangerous than you are ready for", out)) <= 1, f"warning: {seg}/{nxt} gives one line at most")
            here = nxt
    # The warning appears for a main-road room too, where the road itself is above the character: heartwood main road, low end 7.
    out = w.cmd("goto " + COR + "green_road/m11")
    out = w.cmd("north")
    R.ok("this ground is more dangerous than you are ready for." in out, "warning: level 5 stepping from the Green Road (6-7) into Heartwood (7-8) is warned", body(out)[-200:])
    w.cmd("setlevel 10")


SENTINELS = ["Ansel", "Bertha", "Cyril", "Dora", "Egbert"]


def patrols_start(wk, sentinels):
    R.section("8. The patrols: at the start of the route")
    w = wk.w
    for index, (name, (seg, first, last)) in enumerate(PATROLS.items()):
        start = COR + f"{seg}/m{first:02d}"
        src = ci.Source(COR + "npc/" + name)
        # A player stands in the first room of the route (brought there with the probe): the patrol's clock starts.
        s = ci.Session(SENTINELS[index], archetype="scout")
        w.cmd("goto " + HOLLOW + "village/square")
        w.cmd("clone /domains/citytest/probe")
        w.cmd("drop probe")
        R.ok("probe" in s.cmd("get probe"), f"patrols: the player for {name} picks up the probe")
        s.cmd("tele " + start)
        s.drain(0.2)
        sentinels.append((name, seg, first, s))
        out = wk.say(w, "goto " + start)
        R.ok("A Riverwatch patrol is here." in out.replace("\r", ""), f"patrols: {name} stands in {seg}/m{first:02d}, the first room of its route", f"look: {body(out)[-300:]!r}")
        stat = wk.say(w, "stat patrol", city=False)
        R.ok(f"route {last - first + 1} rooms (there and back), at 0, pace 45s" in stat and "defends players True" in stat and "Peaceful True" in stat,
             f"patrols: {name} is peaceful, defends players and has a route of {last - first + 1} rooms at pace 45 s", f"stat: {body(stat)[-300:]!r}")
        R.ok("Level" in stat and not re.search(r"enemies [1-9]", stat), f"patrols: {name} is not fighting")
        look = wk.say(w, "look at patrol")
        R.ok(ci.norm(ci.render(src.get("LongDesc"))) in ci.norm(look), f"patrols: look at patrol ({name}) shows the description", f"got: {body(look)[:200]!r}")
        for i, line in enumerate(src.chats()):
            out = wk.say(w, "talk patrol")
            R.ok('Riverwatch Patrol says: "' in out and ci.norm(ci.render(line)) in ci.norm(out), f"patrols: talk patrol line {i + 1} ({name})", f"got: {body(out)[:240]!r}")
        topics = src.topics()
        for i, (keywords, line) in enumerate(topics):
            for raw in keywords.split(","):
                key = raw.strip()
                j = ci.ask_model(topics, key)
                out = wk.say(w, f"ask patrol about {key}", city=False)
                R.ok(j == i and ci.norm(ci.render(line)) in ci.norm(out), f"patrols: ask patrol about '{key}' reaches its own topic ({name})", f"got: {body(out)[:200]!r}")


def patrols_move(wk, sentinels, placed):
    R.section("8b. The patrols: they walk when a player stands on the route")
    w = wk.w
    remaining = 85 - (time.time() - placed)
    if remaining > 0:
        time.sleep(remaining)
    for name, seg, first, s in sentinels:
        heard = s.drain(0.5)
        R.ok("A Riverwatch patrol leaves north." in heard, f"patrols: {name} left its first room northward while a player stood on the route", f"the player heard: {body(heard)[-200:]!r}")
        found = False
        for n in range(first + 1, first + 4):
            out = wk.say(w, "goto " + COR + f"{seg}/m{n:02d}", city=False)
            if "A Riverwatch patrol is here." in out:
                found = True
                break
        R.ok(found, f"patrols: {name} is found further along its route", "")
        s.cmd("quit")


def walk_the_road(wk, doc):
    R.section("9. A new character walks from the Village Square to Oakenfell's Southgate")
    w = wk.w
    p = ci.Session("traveler", archetype="warrior")
    first = body(p.cmd("look"))
    R.ok("Village Square" in first, "road: a new character starts in the Village Square", first[:80])

    def step(direction, dest_short, label):
        out = wk.say(p, direction)
        return R.ok(dest_short in lines_of(out), f"road: {label}: '{dest_short}'", f"typed {direction}; first lines: {lines_of(out)[:3]}"), out

    gate = ci.Source(HOLLOW + "village/gate")
    road = ci.Source(HOLLOW + "village/north_road")
    step("north", gate.short, "north from the Village Square")
    step("north", road.short, "north to the North Road")
    refused = wk.say(p, "north")
    R.ok("travel pass" in refused, "road: without the pass the road north is shut", f"got: {body(refused)[:200]!r}")
    w.cmd("goto " + HOLLOW + "village/north_road")
    w.cmd("clone " + HOLLOW + "obj/travel_pass")
    w.cmd("drop pass")
    R.ok("travel pass" in wk.say(p, "get pass"), "road: the character picks up the travel pass")
    # the Hollow side of the first connection
    ok, out = step("north", ci.Source(VALE + "road").short, "the Hollow side: the North Road joins the Vale Road")
    ok, out = step("north", doc[COR + "south_road/m01"]["short"], "the Vale Road joins the first room of the South Road")
    here = COR + "south_road/m01"
    warned = out.count("more dangerous than you are ready for")
    steps = 0
    while here != COR + "approach/m11" and steps < 120:
        nxt = doc[here]["exits"]["north"]
        want = doc[nxt]["short"] if nxt in doc else None
        out = wk.say(p, "north")
        warned += out.count("more dangerous than you are ready for")
        if want is None or want not in lines_of(out):
            R.ok(False, f"road: north from {here} reaches {nxt}", f"first lines: {lines_of(out)[:3]}")
            break
        here = nxt
        steps += 1
    R.ok(here == COR + "approach/m11", "road: the main road runs north from the South Road to the Carters' Mile", f"stopped at {here} after {steps} steps")
    R.ok(steps == 54, "road: 54 steps (55 main-road rooms) from the first room of the South Road to the last room of the Approach", f"{steps}")
    # the city side of the second connection
    ok, out = step("north", ci.Source(VALE + "approach").short, "the city side: the Carters' Mile joins the Southgate Approach")
    ok, out = step("north", ci.Source(OAK + "southgate/gate").short, "the Southgate Approach joins the Southgate of Oakenfell")
    ok, out = step("south", ci.Source(VALE + "approach").short, "and back")
    R.ok(warned >= 1, "road: a level 1 character was warned that the ground is dangerous on the way", f"{warned} warnings")
    p.cmd("quit")


# ------------------------------------------------------------------ main

def main():
    only = sys.argv[1] if len(sys.argv) > 1 else ""
    doc, allowed, levels = parse_build()
    rooms, others = ci.source_files("corridor", "vale_road")
    print(f"{len(doc)} road rooms in the build document, {len([p for p in rooms if p.startswith(COR)])} corridor room files, {len(levels)} creatures in the document")
    check_static(doc, allowed, levels, rooms, others)
    if only == "static":
        R.end_section()
        print(f"\n{R.passed + R.failed} checks, {R.passed} passed, {R.failed} failed")
        return 1 if R.failed else 0

    started = time.time()
    game = ci.Game(prepare=calm)
    wk = None
    try:
        wiz = ci.Session("wiz")
        out = wiz.cmd("promote " + h.SECRET)
        R.ok("level 10 wizard" in out, "setup: the secret makes a level 10 wizard", out)
        for line in ("unwield", "drop spear", "drop ration", "drop ration", "drop dagger", "drop sword", "drop staff", "drop bow", "drop mace"):
            wiz.cmd(line)
        wk = Walker(wiz, doc, rooms, others)
        paths = [p for p in room_names()]
        # the patrols first: they walk on a clock, and the walk takes long
        sentinels = []
        patrols_start(wk, sentinels)
        placed = time.time()
        wizards_make_no_history(wk, paths)
        bands_warning(wk)
        patrols_move(wk, sentinels, placed)
        order = [p for p in paths]
        R.section("2. Every room: details, readables, people, shops, nodes, bands")
        for path in order:
            wk.visit(path)
        walk_exits(wk, order)
        hidden_ways(wk, game, None)
        walk_the_road(wk, doc)
        ct.check_texts(wk)
        R.section("10. The driver log")
        logged = game.log_text()
        errors = [l for l in logged.splitlines() if "ERROR" in l or "Exception" in l]
        R.ok(not errors, "the driver logged no errors", "\n".join(errors[:12]))
        R.end_section()
        wiz.cmd("shutdown")
    finally:
        game.close()
    print(f"\n{R.passed + R.failed} checks, {R.passed} passed, {R.failed} failed ({time.time() - started:.0f} s in the game)")
    return 1 if R.failed else 0


if __name__ == "__main__":
    sys.exit(main())
