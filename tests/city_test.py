#!/usr/bin/env python3
"""Walk-through test of the city of Oakenfell (Legends of Hollowmere).

Starts the driver against a temporary copy of the mudlib and walks the 68-odd rooms of the city (oakenfell/ and the two Vale Road rooms that remain; the corridor has tests/corridor_test.py): the map against
docs/areas/oakenfell-build.md, every exit both ways (as a level 10 wizard), every detail and readable, every person
(talk, every topic, topics shadowed by an earlier topic), every shop, a new character walking in from Vincent's Hollow, and
the text rules. It runs to the end whatever it finds: a content defect is a FAIL line, not a stop.

Expectations are derived from the files (regular expressions over the .cs sources) and from the build document, not typed in.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4940 python3 tests/city_test.py
"""
import os
import re
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import city_infra as ci  # noqa: E402  (also sets the default port 4824)
import smoke_test as h  # noqa: E402

OAK, VALE, HOLLOW = ci.OAK, ci.VALE, ci.HOLLOW
CORRIDOR = "/domains/greenvale/corridor/"
COMPASS = {"north": "n", "south": "s", "east": "e", "west": "w", "up": "u", "down": "d"}
TITLES = {"old", "widow", "ranger", "sergeant", "captain", "master", "mistress", "brother", "sister", "elder", "lady", "lord", "goodwife", "goodman"}
RAW_TAG = re.compile(r"\{(?:cmd|title|b|u|/|red|green|yellow|blue|magenta|cyan|white|bred|bgreen|byellow|bblue|bmagenta|bcyan|bwhite)\}")
WOLF = re.compile(r"wandering wolf|wolf wanders in", re.I)
NO_SUCH = ("You see no ", "There is nothing to read", "What?", "Get what?", "Talk to whom?", "Ask whom?")


# ------------------------------------------------------------------ reporting

class Report:
    def __init__(self):
        self.passed = 0
        self.failed = 0
        self.section_name = ""
        self.section_passed = 0
        self.section_failed = 0
        self.failures = []

    def section(self, name):
        self.end_section()
        self.section_name, self.section_passed, self.section_failed = name, 0, 0
        print(f"\n=== {name}")

    def end_section(self):
        if self.section_name:
            print(f"--- {self.section_name}: {self.section_passed} passed, {self.section_failed} failed")
        self.section_name = ""

    def ok(self, condition, label, detail=""):
        if condition:
            self.passed += 1
            self.section_passed += 1
        else:
            self.failed += 1
            self.section_failed += 1
            self.failures.append(label)
            print("FAIL  " + label)
            for line in str(detail).splitlines():
                print("        " + line)
        return condition


R = Report()


def body(out):
    """A reply without the carriage returns and without the prompt that ends it."""
    text = out.replace("\r", "")
    text = re.sub(r"(?:[A-Za-z]+ \(\d+/\d+\) \(\d+/\d+\) \(\d+/\d+\) )?> $", "", text)
    return text.strip()


def lines_of(out):
    return [l.strip() for l in body(out).splitlines()]


def cap(text):
    return text[:1].upper() + text[1:]


def short_of(out):
    return lines_of(out)[0] if lines_of(out) else ""


# ------------------------------------------------------------------ part 1: the map, from the files

def check_map(doc, rooms, others):
    R.section("1. The map matches the build document")
    R.ok(len(doc) >= 50, "the build document's tables give at least 50 rooms", f"parsed {len(doc)}")
    for path, want in doc.items():
        src = rooms.get(path)
        if not R.ok(src is not None and src.exists, f"map: room file exists: {path}"):
            continue
        R.ok(src.short == want["short"], f"map: short name of {path}", f"document: {want['short']!r}\nfile:     {src.short!r}")
        for direction, dest in want["exits"].items():
            have = src.exits.get(direction)
            R.ok(have == dest, f"map: {path} exit {direction}", f"document: {dest}\nfile:     {have}")
            R.ok(dest in rooms or os.path.isfile(os.path.join(ci.MUDLIB, dest.lstrip("/") + ".cs")), f"map: the destination of {path} {direction} is a room file: {dest}")
        extra = sorted(set(src.exits) - set(want["exits"]))
        R.ok(not extra, f"map: {path} has no exit the document lacks", f"extra exits: {[(d, src.exits[d]) for d in extra]}")
        blocked = {a[0]: a[1] for a in src.of("BlockExit")}
        for direction, toward in want["blocked"].items():
            line = blocked.get(direction)
            R.ok(line is not None, f"map: {path} blocks {direction} (toward {toward})", f"BlockExit directions in the file: {sorted(blocked)}")
            if line is not None:
                R.ok(toward.lower() in line.lower() and "not yet open" in line.lower(),
                     f"map: the blocking line of {path} {direction} names '{toward}' and says it is not yet open", f"line: {line!r}")
        extra_blocks = sorted(set(blocked) - set(want["blocked"]))
        R.ok(not extra_blocks, f"map: {path} blocks only what the document says", f"extra BlockExit: {extra_blocks}")
        R.ok(not any(d in blocked for d in src.exits), f"map: no direction of {path} is both an exit and blocked")
    # The Elder Lane has no west exit, by design.
    elder = rooms.get(OAK + "founders/elder_lane")
    if elder:
        R.ok("west" not in elder.exits, "map: Elder Lane has no west exit (the hidden way is left for integration)")
    # The room files that the document does not list.
    for path, src in sorted(rooms.items()):
        if path not in doc:
            R.ok(False, f"map: a room file the build document does not list: {path}", f"short name {src.short!r}")
    road = rooms.get(VALE + "road")
    if R.ok(road is not None, "map: the existing Vale Road room exists"):
        R.ok(road.exits.get("north") == CORRIDOR + "south_road/m01", "map: the Vale Road's north exit leads to the corridor's South Road", f"north: {road.exits.get('north')}")
    approach = rooms.get(VALE + "approach")
    if R.ok(approach is not None, "map: the Southgate Approach exists"):
        R.ok(approach.exits.get("south") == CORRIDOR + "approach/m11", "map: the Southgate Approach's south exit leads to the corridor's Carters' Mile", f"south: {approach.exits.get('south')}")
    # Every spawn and encounter names a file, and every person's file is used.
    spawned = set()
    for path, src in rooms.items():
        for args in src.of("AddSpawn"):
            spawned.add(args[0])
            R.ok(os.path.isfile(os.path.join(ci.MUDLIB, args[0].lstrip("/") + ".cs")), f"map: {path} spawns an existing file: {args[0]}")
        for args in src.of("AddEncounter"):
            R.ok(os.path.isfile(os.path.join(ci.MUDLIB, args[0].lstrip("/") + ".cs")), f"map: {path} has an encounter with an existing file: {args[0]}")
    for path, src in sorted(others.items()):
        if "/npc/" in path and src.is_creature:
            R.ok(path in spawned, f"map: every person's file is placed in a room: {path}", f"{src.name!r} is not spawned by any room")


def check_source_rules(rooms, others):
    R.section("7a. Text rules and room rules, from the sources")
    every = {**rooms, **others}
    for path, src in sorted(every.items()):
        for literal in re.findall(ci.STR, src.text):
            text = ci.unescape(literal)
            if "> " in text:
                R.ok(False, f"text: '> ' inside game text in {path}", f"in the string: {text[:140]!r}")
            if "All Hall" in text:
                R.ok(False, f"text: 'All Hall' in {path}", f"in the string: {text[:140]!r}")
        if src.is_creature or src.base in ("/std/item", "/std/consumable", "/std/weapon", "/std/armor"):
            for field in ("Name", "ShortDesc"):
                R.ok("{" not in src.get(field), f"text: no colour tag in {field} of {path}", f"{field} = {src.get(field)!r}")
            R.ok(not any("{" in i for i in src.ids), f"text: no colour tag in the Ids of {path}", f"Ids = {src.ids}")
        if src.base == "/std/monster":
            R.ok(False, f"rules: no creature in the city fights: {path} derives from Monster")
    for path, src in sorted(rooms.items()):
        safe = src.get("Safe") == "true"
        guest = path == OAK + "southgate/silver_stag_rooms"
        R.ok(safe == guest, f"rules: only the Silver Stag's guest rooms are Safe: {path}", f"Safe = {src.get('Safe') or 'unset'}")
        if guest:
            R.ok(src.get("LogoutRoom") == "true", "rules: the guest rooms are a logout room", f"LogoutRoom = {src.get('LogoutRoom') or 'unset'}")
    R.section("7b. No two people share a first name")
    given = {}
    for path, src in sorted(others.items()):
        if "/npc/" in path and src.is_creature and src.name:
            words = src.name.split()
            if words and words[0].lower() == "the":
                continue
            while len(words) > 1 and words[0].lower() in TITLES:
                words = words[1:]
            given.setdefault(words[0].lower(), []).append((src.name, path))
    for first, people in sorted(given.items()):
        R.ok(len(people) == 1, f"names: first name '{first}' is shared", "\n".join(f"{n}  ({p})" for n, p in people))
    names = {}
    for path, src in others.items():
        if "/npc/" in path and src.is_creature:
            names.setdefault(src.name.lower(), []).append(path)
    for name, paths in names.items():
        R.ok(len(paths) == 1, f"names: the name '{name}' is shared", "\n".join(paths))


def check_shadowed_topics(others):
    R.section("4a. Topic keys that an earlier topic of the same person answers instead")
    for path, src in sorted(others.items()):
        if not ("/npc/" in path and src.is_creature):
            continue
        topics = src.topics()
        for i, (keywords, _) in enumerate(topics):
            for raw in keywords.split(","):
                key = raw.strip()
                if not key:
                    continue
                j = ci.ask_model(topics, key)
                if R.ok(j == i, f"topics: {path}: 'ask {src.name.lower()} about {key}' is shadowed",
                        None if j == i else f"topic #{i + 1} [{keywords}] never answers '{key}'; "
                        + (f"topic #{j + 1} [{topics[j][0]}] does:\n{topics[j][1][:160]}" if j is not None else "nothing does")):
                    pass


# ------------------------------------------------------------------ part 2: the running game

class Walker:
    def __init__(self, wiz, doc, rooms, others):
        self.w = wiz
        self.doc, self.rooms, self.others = doc, rooms, others
        self.texts = []  # (what was typed, the reply) of everything shown in the city
        self.sources = {}

    def src(self, path):
        if path not in self.sources:
            self.sources[path] = ci.Source(path)
        return self.sources[path]

    def say(self, sess, line, city=True):
        out = sess.cmd(line)
        if city:
            self.texts.append((f"{sess.name}: {line}", out))
        self.wolf(sess, out)
        return out

    def wolf(self, sess, out):
        """A wolf wandered in: the wizard kills it (the wizard's own wolf, or the one that has found the walking character)."""
        if not WOLF.search(out) or getattr(self, "busy", False):
            return
        self.busy = True
        try:
            for _ in range(3):
                if sess is not self.w:
                    self.w.cmd("goto " + sess.name.lower())
                reply = self.w.cmd("slay wolf")
                if "wolf" not in reply.lower() or "What?" in reply:
                    break
                time.sleep(0.2)
            self.wolves = getattr(self, "wolves", 0) + 1
            sess.cmd("restore") if sess is self.w else None
        finally:
            self.busy = False

    # ---- one room ----

    def visit(self, path):
        src = self.src(path)
        w = self.w
        out = self.say(w, "goto " + path)
        R.ok(src.short in lines_of(out), f"walk: goto {path} lands in '{src.short}'", f"first lines: {lines_of(out)[:2]}")
        look = self.say(w, "look")
        self.check_listing(path, src, look)
        self.check_details(path, src)
        self.check_people(path, src, look)

    def check_listing(self, path, src, look):
        match = re.search(r"Obvious exits: (.*)", look)
        shown = re.findall(r"(\w+)(?: \(to [^)]*\))?(?:, |$)", match.group(1).replace("\r", "")) if match else []
        if not src.exits:
            R.ok(match is not None and "There are no obvious exits" in look or not shown, f"walk: {path} lists no exits")
        else:
            R.ok(sorted(shown) == sorted(src.exits), f"walk: the exits that {path} lists are the exits in its file",
                 f"listed: {sorted(shown)}\nfile:   {sorted(src.exits)}")

    def check_details(self, path, src):
        w = self.w
        details, readables, dupes = src.room_texts()
        for keys, first, second in dupes:
            R.ok(False, f"details: {path}: keys {keys} are defined twice; the later definition replaces the earlier",
                 f"earlier: [{first}]\nlater:   [{second}]")
        for fn, args in src.calls:
            if fn not in ("AddDetail", "AddReadable"):
                continue
            key = ci.keys_of(args[0])[0] if ci.keys_of(args[0]) else ""
            if not key:
                R.ok(False, f"details: {path}: a {fn} without a key")
                continue
            expected = details.get(key, "")
            out = self.say(w, "look at " + key)
            self.check_reply(f"details: {path}: look at '{key}'", out, expected, shadow=True)
            if fn == "AddReadable":
                out = self.say(w, "read " + key)
                self.check_reply(f"readables: {path}: read '{key}'", out, readables.get(key, ""), shadow=True)

    def check_reply(self, label, out, expected, shadow=False):
        text = body(out)
        bad = (not text) or any(text.startswith(n) for n in NO_SUCH)
        if not R.ok(not bad, label + (" answers" if not bad else " does not answer"), f"got: {text[:200]!r}"):
            return
        want = ci.norm(ci.render(expected))
        # Not the room's text: something else answered (an object in the room with that name).
        R.ok(want in ci.norm(text), label + " shows the room's text", f"expected: {want[:160]!r}\ngot:      {ci.norm(text)[:200]!r}")

    def check_people(self, path, src, look):
        w = self.w
        seen = set()
        for args in src.of("AddSpawn"):
            if args[0] in seen:
                continue
            seen.add(args[0])
            person = self.src(args[0])
            if not person.exists:
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
            R.ok(ci.norm(ci.render(person.get("LongDesc"))) in ci.norm(out), f"people: look at {lower} shows the description ({path})",
                 f"got: {body(out)[:200]!r}")
            chats = person.chats()
            if not R.ok(chats, f"people: {name} ({args[0]}) has something to say"):
                pass
            for i, line in enumerate(chats):
                out = self.say(w, "talk " + lower)
                text = ci.norm(out)
                R.ok(f'{name} says: "' in text and ci.norm(ci.render(line)) in text,
                     f"people: talk {lower} line {i + 1} ({path})", f"expected: {line[:120]!r}\ngot: {body(out)[:240]!r}")
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

    # ---- shops ----

    def stock(self, person):
        result = []
        for item_path, price in person.of("AddStock"):
            item = self.src(item_path)
            result.append((item_path, item, price))
        return result

    def gold(self):
        m = re.search(r"Gold (\d+)", self.w.cmd("score"))
        return int(m.group(1)) if m else -1

    def ensure_gold(self, need):
        for _ in range(8):
            if self.gold() >= need:
                return
            self.w.cmd("clone /domains/hollow/obj/bag_of_coins")
            self.w.cmd("drop coins")
            self.w.cmd("get coins")

    def shop(self, path, person, lower):
        w = self.w
        stock = self.stock(person)
        out = self.say(w, "browse")
        listed = re.findall(r"^\s+(.+?)\s{2,}(\d+) gold\s*$", out.replace("\r", ""), re.M)
        want = [(cap(item.short), str(price)) for _, item, price in stock]
        R.ok(listed == want, f"shops: browse at {person.name} ({path}) lists exactly the stock at the file's prices", f"file:   {want}\nbrowse: {listed}")
        self.ensure_gold(sum(p for _, _, p in stock) + 10)
        for item_path, item, price in sorted(stock, key=lambda s: s[2]):
            if not item.exists:
                R.ok(False, f"shops: stock file exists: {item_path}")
                continue
            if item_path.endswith("stag_bed"):
                continue
            before = self.gold()
            word = item.name.lower()
            out = self.say(w, "buy " + word)
            R.ok(f"You buy {item.short} for {price} gold." in out, f"shops: buy {word} at {person.name} ({path})", f"got: {body(out)[:240]!r}")
            after = self.gold()
            R.ok(before - after == price, f"shops: buying {word} costs {price} gold", f"gold {before} -> {after}")
            inv = self.say(w, "i")
            R.ok(item.short in inv, f"shops: the {word} arrives in the inventory", f"inventory: {body(inv)[:240]!r}")
        # Selling back is not asked for.

    def bed(self):
        R.section("5b. The Silver Stag's bed")
        w = self.w
        w.cmd("restore")
        w.cmd("goto /domains/citytest/pit")
        end = time.time() + 40
        hp = max_hp = 0
        while time.time() < end:
            time.sleep(1.0)
            m = re.search(r"HP (\d+)/(\d+)", w.cmd("hp"))
            if m:
                hp, max_hp = int(m.group(1)), int(m.group(2))
            if max_hp and hp <= max_hp - 8:
                break
        R.ok(0 < hp < max_hp, "bed: the buyer was wounded before sleeping (setup)", f"HP {hp}/{max_hp}")
        out = self.say(w, "goto " + OAK + "southgate/silver_stag")
        wounded = re.search(r"HP (\d+)/(\d+)", w.cmd("hp"))
        self.ensure_gold(30)
        before = self.gold()
        out = self.say(w, "buy bed")
        R.ok("Orla shows you upstairs" in out, "bed: buy bed gives Orla's line", f"got: {body(out)[:300]!r}")
        R.ok("The Silver Stag Inn, Guest Rooms" in lines_of(out), "bed: the buyer arrives in the guest rooms", f"got lines: {lines_of(out)[:12]}")
        hp_after = re.search(r"HP (\d+)/(\d+)", w.cmd("hp"))
        R.ok(bool(hp_after) and hp_after.group(1) == hp_after.group(2), "bed: the buyer is fully healed",
             f"before {wounded.group(0) if wounded else '?'}, after {hp_after.group(0) if hp_after else '?'}")
        R.ok(before - self.gold() == 8, "bed: the bed costs 8 gold, as browse says")
        look = self.say(w, "look")
        R.ok("The Silver Stag Inn, Guest Rooms" in lines_of(look), "bed: the buyer is in the guest rooms")
        stat = w.cmd("stat here")
        R.ok("Safe True" in stat and "logout room True" in stat, "bed: the guest rooms are Safe and a logout room", f"got: {body(stat)[:200]!r}")
        w.cmd("restore")

    # ---- exits ----

    def exits(self, path):
        src = self.src(path)
        w = self.w
        blocked = {a[0]: a[1] for a in src.of("BlockExit")}
        for direction, dest in src.exits.items():
            dsrc = self.src(dest)
            self.say(w, "goto " + path, city=False)
            out = self.say(w, direction)
            if not R.ok(dsrc.short in lines_of(out), f"exits: {path} {direction} leads to '{dsrc.short}' ({dest})", f"first lines: {lines_of(out)[:2]}\n{body(out)[:200]!r}"):
                continue
            backs = [d for d, p in dsrc.exits.items() if p == path]
            if not dest.startswith("/domains/greenvale/"):
                continue  # the Vincent's Hollow side of the road is shut to a wizard without a pass; the player walk covers it
            if dest == OAK + "founders/elder_lane" and path == OAK + "grove/overgrown_way":
                continue  # by design: the hidden way is left for integration, Elder Lane has no exit west
            if not R.ok(backs, f"exits: {dest} has a way back to {path}", f"exits of {dest}: {dsrc.exits}"):
                continue
            back = backs[0]
            typed = COMPASS.get(back, back)
            out = self.say(w, typed)
            R.ok(src.short in lines_of(out), f"exits: '{typed}' in {dest} returns to '{src.short}' ({path})", f"first lines: {lines_of(out)[:2]}\n{body(out)[:200]!r}")
        for direction, line in blocked.items():
            self.say(w, "goto " + path, city=False)
            out = self.say(w, direction)
            R.ok(ci.norm(line) in ci.norm(out), f"exits: {path} blocked {direction} gives its blocking line", f"got: {body(out)[:240]!r}")
            look = self.say(w, "look")
            R.ok(src.short in lines_of(look), f"exits: {path} blocked {direction} does not move the traveler", f"first lines: {lines_of(look)[:2]}")
            listed = re.search(r"Obvious exits: (.*)", look)
            R.ok(not listed or not re.search(r"\b" + direction + r"\b", listed.group(1)), f"exits: the blocked {direction} of {path} is not listed among the exits")


# ------------------------------------------------------------------ part 6: a new character walks in

def player_walk(wk, rooms):
    R.section("6. A new character walks from the Southgate Approach to the Hall of Callings")
    w = wk.w
    p = ci.Session("walker", archetype="warrior")
    first = body(p.cmd("look"))
    R.ok("Village Square" in first, "player: a new character starts in the Village Square", first[:80])
    # The way from the Hollow to the Approach is the corridor's (corridor_test) and the travel pass is class_test's. Here the wizard
    # lays a probe in the square and the character carries it to the Approach, as the other tests bring a character to a far room.
    w.cmd("goto " + HOLLOW + "village/square")
    w.cmd("clone /domains/citytest/probe")
    w.cmd("drop probe")
    got = wk.say(p, "get probe")
    R.ok("probe" in got, "player: the character picks up the probe", f"got: {body(got)[:200]!r}")
    out = wk.say(p, "tele " + VALE + "approach")
    R.ok(ci.Source(VALE + "approach").short in lines_of(out), "player: the character is brought to the Southgate Approach", f"first lines: {lines_of(out)[:3]}")

    def step(direction, dest_short, label):
        out = wk.say(p, direction)
        if "You are fighting" in out or WOLF.search(out):
            wk.wolf(p, out)
            out = out + wk.say(p, "look")
        return R.ok(dest_short in lines_of(out), f"player: {label}: '{dest_short}'", f"typed {direction}; first lines: {lines_of(out)[:3]}")

    chain = [OAK + "southgate/gate", OAK + "southgate/wayfarers_row", OAK + "southgate/stag_lane",
             OAK + "market/south_rows", OAK + "market/north_rows", OAK + "founders/square",
             OAK + "guild/guild_row", OAK + "guild/hall_doors", OAK + "guild/hall_of_callings"]
    here = VALE + "approach"
    for nxt in chain:
        here_src = ci.Source(here)
        direction = next((d for d, t in here_src.exits.items() if t == nxt), None)
        if direction is None:
            R.ok(False, f"player: {here} has no exit to {nxt}", f"exits: {here_src.exits}")
            break
        if not step(direction, ci.Source(nxt).short, f"{direction} to {nxt.rsplit('/', 1)[1]}"):
            break
        here = nxt
    look = wk.say(p, "look")
    hallmasters = [pp for pp in rooms[OAK + "guild/hall_of_callings"].of("AddSpawn")]
    R.ok(len(hallmasters) >= 1 and all(ci.Source(a[0]).short in look for a in hallmasters), "player: the Hall of Callings shows its people to a player",
         f"got: {body(look)[-300:]!r}")
    p.cmd("quit")


# ------------------------------------------------------------------ part 7c: what was shown

def check_texts(wk):
    R.section("7c. Text rules on everything the characters were shown")
    seen = set()
    for typed, out in wk.texts:
        text = re.sub(r"(?m)^(?:[A-Za-z]+ \(\d+/\d+\) \(\d+/\d+\) \(\d+/\d+\) )?> ?", "", body(out))  # the prompts (the default one names the character)
        # A '> ' at the start of a line is the prompt, followed by an idle action that arrived just after the reply.
        for m in re.finditer(r"(?<!\n)> ", text):
            key = ("gt", text[max(0, m.start() - 30):m.end() + 10])
            if key not in seen:
                seen.add(key)
                R.ok(False, "text: the sequence '> ' appears inside game text", f"after '{typed}': ...{text[max(0, m.start() - 80):m.end() + 20]!r}")
        for m in RAW_TAG.finditer(text):
            key = ("tag", text[max(0, m.start() - 30):m.end()])
            if key not in seen:
                seen.add(key)
                R.ok(False, "text: an unrendered colour tag reaches the player", f"after '{typed}': ...{text[max(0, m.start() - 60):m.end() + 20]!r}")
        if "All Hall" in text:
            key = ("hall", typed)
            if key not in seen:
                seen.add(key)
                R.ok(False, "text: 'All Hall' appears in output", f"after '{typed}': {text[max(0, text.index('All Hall') - 60):][:120]!r}")
    R.ok(len(wk.texts) > 500, "text: enough output was examined", f"{len(wk.texts)} replies")
    R.ok(not seen, "text: no raw tag, '> ' or 'All Hall' anywhere in the output") if not seen else None


# ------------------------------------------------------------------ main

def main():
    doc = ci.parse_build_document()
    rooms, others = ci.city_files()
    print(f"{len(doc)} rooms in the build document, {len(rooms)} room files, {len(others)} other files under domains/greenvale")
    check_map(doc, rooms, others)
    check_source_rules(rooms, others)
    check_shadowed_topics(others)

    started = time.time()
    game = ci.Game()
    wk = None
    try:
        wiz = ci.Session("wiz")
        out = wiz.cmd("promote " + h.SECRET)
        R.ok("level 10 wizard" in out, "setup: the secret makes a level 10 wizard", out)
        # The starting kit would answer to 'spear' and the like before the room's details do: walk empty-handed.
        for line in ("unwield", "drop spear", "drop ration", "drop ration", "drop dagger", "drop sword", "drop staff", "drop bow", "drop mace"):
            wiz.cmd(line)
        wk = Walker(wiz, doc, rooms, others)
        order = sorted(rooms, key=lambda p: (0 if p.startswith(VALE) else 1, p))
        R.section("2-5. Every room: details, readables, people, shops, exits")
        for path in order:
            wk.visit(path)
        for path in order:
            wk.exits(path)
        wk.bed()
        player_walk(wk, rooms)
        check_texts(wk)
        R.section("8. The driver log")
        logged = game.log_text()
        errors = [l for l in logged.splitlines() if "ERROR" in l or "Exception" in l]
        R.ok(not errors, "the driver logged no errors", "\n".join(errors[:12]))
        R.end_section()
        wiz.cmd("shutdown")
    finally:
        game.close()
    print(f"\n{R.passed + R.failed} checks, {R.passed} passed, {R.failed} failed (wolves killed: {getattr(wk, 'wolves', 0) if wk else 0}; {time.time() - started:.0f} s in the game)")
    return 1 if R.failed else 0


if __name__ == "__main__":
    sys.exit(main())
