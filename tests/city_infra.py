"""Helpers for tests/city_test.py: a C# source reader for the city's files, the build-document parser,
and the temporary game (driver, sessions) the walk runs against."""
import os
import re
import shutil
import socket
import subprocess
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4824")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402

ROOT = h.ROOT
MUDLIB = os.path.join(ROOT, "mudlib")
OAK = "/domains/greenvale/oakenfell/"
VALE = "/domains/greenvale/vale_road/"
HOLLOW = "/domains/hollow/"

# ------------------------------------------------------------------ reading the C# files

STR = r'"((?:[^"\\]|\\.)*)"'
TAGS = r"\{(?:cmd|title|b|u|/|red|green|yellow|blue|magenta|cyan|white|bred|bgreen|byellow|bblue|bmagenta|bcyan|bwhite)\}"
ESCAPES = {"n": "\n", "t": "\t", '"': '"', "\\": "\\", "'": "'", "r": "\r", "0": "\0"}


def unescape(raw):
    out, i = [], 0
    while i < len(raw):
        if raw[i] == "\\" and i + 1 < len(raw):
            out.append(ESCAPES.get(raw[i + 1], raw[i + 1]))
            i += 2
        else:
            out.append(raw[i])
            i += 1
    return "".join(out)


def render(text):
    """What a player sees of a string written with colour tags: tags removed, '{{' a single brace."""
    return re.sub(TAGS, "", text).replace("{{", "{")


def norm(text):
    """Whitespace collapsed, so that texts compare however they are wrapped."""
    return re.sub(r"\s+", " ", text).strip()


def keys_of(names):
    """The names a Room registers for a comma-separated list: trimmed, lower case, leading article dropped."""
    result = []
    for raw in names.split(","):
        name = raw.strip().lower()
        for article in ("the ", "a ", "an "):
            if name.startswith(article):
                name = name[len(article):]
                break
        if name:
            result.append(name)
    return result


CALL = re.compile(
    r'\b(?P<fn>AddDetail|AddReadable|AddTopic|AddChat|AddEmote|AddSpawn|AddStock|AddRareStock|AddEncounter|BlockExit|LockExit)\(\s*"(?P<s1>(?:[^"\\]|\\.)*)"'
    r'(?:\s*,\s*(?:"(?P<s2>(?:[^"\\]|\\.)*)"|(?P<n2>-?\d+)))?'
    r'(?:\s*,\s*(?:"(?P<s3>(?:[^"\\]|\\.)*)"|(?P<n3>-?\d+)))?')
ASSIGN = re.compile(r'\b(?P<field>ShortDesc|LongDesc|Name|Safe|LogoutRoom|ResetSeconds|Level)\s*=\s*(?:"(?P<v>(?:[^"\\]|\\.)*)"|(?P<n>[\w\.]+))\s*;')
IDS = re.compile(r'Ids\.Add\(\s*' + STR + r'\s*\)')
EXIT = re.compile(r'Exits\[\s*' + STR + r'\s*\]\s*=\s*' + STR + r'\s*;')
INHERIT = re.compile(r"//\s*@inherit\s+(\S+)")


class Source:
    """One mudlib file, read with regular expressions."""

    def __init__(self, path):
        self.path = path
        self.file = os.path.join(MUDLIB, path.lstrip("/") + ".cs")
        self.exists = os.path.isfile(self.file)
        self.text = open(self.file).read() if self.exists else ""
        # Adjacent string literals joined with '+' (a long text written over several lines) are one literal.
        self.text = re.sub(r'(?<!\\)"\s*\+\s*"', "", self.text)
        m = INHERIT.search(self.text)
        self.base = m.group(1) if m else ""
        self.fields = {}
        self.ids = [unescape(m.group(1)) for m in IDS.finditer(self.text)]
        self.exits = {}
        for m in EXIT.finditer(self.text):
            self.exits[m.group(1)] = m.group(2)
        for m in ASSIGN.finditer(self.text):
            self.fields.setdefault(m.group("field"), unescape(m.group("v")) if m.group("v") is not None else m.group("n"))
        self.calls = []
        for m in CALL.finditer(self.text):
            args = [unescape(m.group("s1"))]
            for s, n in (("s2", "n2"), ("s3", "n3")):
                if m.group(s) is not None:
                    args.append(unescape(m.group(s)))
                elif m.group(n) is not None:
                    args.append(int(m.group(n)))
            self.calls.append((m.group("fn"), args))

    def get(self, field, default=""):
        return self.fields.get(field, default)

    def of(self, fn):
        return [args for f, args in self.calls if f == fn]

    @property
    def name(self):
        return self.get("Name")

    @property
    def short(self):
        return self.get("ShortDesc")

    @property
    def is_creature(self):
        return self.base in ("/std/npc", "/std/shopkeeper", "/std/monster")

    def room_texts(self):
        """The room's details and readables as Room builds them: ({key: text for look}, {key: text for read}, duplicates).
        A duplicate is a group of keys that two AddDetail calls (or two AddReadable calls) both define: the later call replaces
        the earlier one for those keys. Each is (keys, first call's key list, later call's key list)."""
        details, readables, d_owner, r_owner, pairs = {}, {}, {}, {}, {}
        for fn, args in self.calls:
            if fn == "AddDetail":
                for k in keys_of(args[0]):
                    if k in d_owner and d_owner[k] != args[0]:
                        pairs.setdefault((d_owner[k], args[0]), []).append(k)
                    details[k] = args[1]
                    d_owner[k] = args[0]
            elif fn == "AddReadable":
                look = args[2] if len(args) > 2 else "There is writing on it. You could {cmd}read{/} it."
                for k in keys_of(args[0]):
                    if k in r_owner and r_owner[k] != args[0]:
                        pairs.setdefault((r_owner[k], args[0]), []).append(k)
                    readables[k] = args[1]
                    r_owner[k] = args[0]
                    if k not in details:
                        details[k] = look
        dupes = [(keys, first, later) for (first, later), keys in pairs.items()]
        return details, readables, dupes

    def topics(self):
        return [(a[0], a[1]) for a in self.of("AddTopic")]

    def chats(self):
        return [a[0] for a in self.of("AddChat")]


def ask_model(topics, typed):
    """Which topic answers 'ask <npc> about <typed>': exactly the matching code of /std/npc.cs (Ask). An index or None."""
    typed = typed.strip().lower()
    best, best_length, loose = None, 0, None
    for i, (keywords, _) in enumerate(topics):
        for raw in keywords.lower().split(","):
            key = raw.strip()
            if key == "":
                continue
            if key == typed:
                return i
            if key in typed and len(key) > best_length:
                best, best_length = i, len(key)
            elif loose is None and len(typed) >= 3 and typed in key:
                loose = i
    return best if best is not None else loose


# ------------------------------------------------------------------ the build document

def parse_build_document():
    """{room path: {"short": str, "exits": {dir: path}, "blocked": {dir: toward}}} from docs/areas/oakenfell-build.md.
    In an exit, '...' stands for the directory of the room described; a path that does not start with '/' is under the oakenfell directory."""
    rooms = {}
    for line in open(os.path.join(ROOT, "docs", "areas", "oakenfell-build.md")):
        if not line.startswith("| ") or line.startswith("| Path") or line.startswith("|---"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) < 3:
            continue
        path = cells[0].strip("`")
        path = path if path.startswith("/") else OAK + path
        exits = {}
        for direction, target in re.findall(r"(\w+) `([^`]+)`", cells[2]):
            target = target.strip()
            if target.startswith("..."):
                target = path.rsplit("/", 1)[0] + target[3:]
            elif not target.startswith("/"):
                target = OAK + target
            exits[direction] = target
        entry = {"short": cells[1], "exits": exits, "blocked": {}}
        if len(cells) >= 4:
            direction, _, toward = cells[3].partition(",")
            toward = re.sub(r"^\s*toward\s+(the\s+)?", "", toward.strip())
            entry["blocked"][direction.strip()] = toward
        rooms[path] = entry
    return rooms


def source_files(*subdirs):
    """Every .cs file under the given directories of domains/greenvale: ({path: Source} for rooms, {path: Source} for everything else)."""
    rooms, others = {}, {}
    for sub in subdirs:
        base = os.path.join(MUDLIB, "domains", "greenvale", sub)
        for folder, _, names in os.walk(base):
            for name in sorted(names):
                if not name.endswith(".cs"):
                    continue
                path = "/" + os.path.relpath(os.path.join(folder, name), MUDLIB)[:-3].replace(os.sep, "/")
                src = Source(path)
                (rooms if src.base == "/std/room" else others)[path] = src
    return rooms, others


def city_files():
    """The city: everything under oakenfell/ and the two Vale Road rooms that remain (the corridor has its own test)."""
    return source_files("oakenfell", "vale_road")


# ------------------------------------------------------------------ the running game

TEST_BITER = '''// @inherit /std/monster
public class CityBiter : Monster
{
    public override void Create()
    {
        Name = "Biter";
        Ids.Add("biter");
        ShortDesc = "a biter";
        LongDesc = "A creature made to wound the wizard so that a bed can be tried.";
        Level = 5; MaxVitality = 5000; DamageMin = 4; DamageMax = 6; ToHit = 30;
        XpValue = 0; Aggressive = true;
        BaseReaction = 1.5;
        AddCombatAction("bite", "Bite", preparation: 0, recovery: 0.5);
    }
}
'''
TEST_PIT = '''// @inherit /std/room
public class CityPit : Room
{
    public override void Create()
    {
        ShortDesc = "Biter Pit";
        LongDesc = "A room made for the city test.";
        ResetSeconds = 3000;
        AddSpawn("/domains/citytest/biter");
    }
}
'''


TEST_PROBE = '''// @inherit /std/item
public class CityProbe : Item
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init() { base.Init(); AddAction("tele", Tele); }
    // Carried by a character, it moves that character to any room: how the tests bring a non-wizard to a far place.
    private bool Tele(string args)
    {
        var who = ThisPlayer()!;
        MoveObject(who, LoadObject(args.Trim()));
        who.Call("Look");
        return true;
    }
}
'''


class Game:
    """A driver running on a copy of the real mudlib, with the test's own two files under /domains/citytest."""

    def __init__(self, prepare=None):
        self.work = tempfile.mkdtemp(prefix="lesmudos-city-")
        self.mudlib = os.path.join(self.work, "mudlib")
        h.copy_mudlib(self.mudlib)
        os.makedirs(os.path.join(self.mudlib, "domains", "citytest"))
        h.write(self.mudlib, "/domains/citytest/biter", TEST_BITER)
        h.write(self.mudlib, "/domains/citytest/pit", TEST_PIT)
        h.write(self.mudlib, "/domains/citytest/probe", TEST_PROBE)
        if prepare:
            prepare(self.mudlib)  # a test may change its own copy of the mudlib before the driver starts
        self.log_path = os.path.join(self.work, "driver.log")
        self.log = open(self.log_path, "w")
        dll = os.path.join(ROOT, "src", "LesMudOS.Driver", "bin", "Debug", "net10.0", "LesMudOS.Driver.dll")
        self.proc = subprocess.Popen(
            ["dotnet", dll, "--mudlib", self.mudlib, "--save", os.path.join(self.work, "save"), "--port", str(h.PORT)],
            stdout=self.log, stderr=subprocess.STDOUT, env={**os.environ, "LESMUDOS_ADMIN_SECRET": h.SECRET})
        for _ in range(200):  # 40 s: the driver boots in about 14 s, more on a busy machine
            try:
                socket.create_connection(("127.0.0.1", h.PORT), timeout=0.3).close()
                break
            except OSError:
                time.sleep(0.2)

    def log_text(self):
        self.log.flush()
        return open(self.log_path).read()

    def close(self):
        if self.proc.poll() is None:
            self.proc.kill()
            self.proc.wait(timeout=10)
        self.log.close()
        shutil.rmtree(self.work, ignore_errors=True)


class Session:
    """A logged-in character. cmd() sends a line and returns everything up to the prompt, even if the text holds '> '."""

    def __init__(self, name, archetype=None):
        self.name = name
        self.c = h.Client(name, archetype=archetype)
        self.c.sock.settimeout(0.05)
        self.log = []  # (command, output) of everything the character did, for the text rules
        self.c.read_until("> ")

    def cmd(self, line, timeout=20.0):
        c = self.c
        c.absorb()  # news that came in unasked, and the prompt that followed it, are not part of this reply
        c.send(line)
        end = time.time() + timeout
        data, c.pending = c.pending, ""
        quiet = 0
        while time.time() < end:
            try:
                chunk = c.sock.recv(65536)
                if not chunk:
                    break
                data += c.rx(chunk)
                quiet = 0
            except socket.timeout:
                # A prompt at the end, and a moment of silence after it: the reply is complete.
                if data.endswith("> "):
                    quiet += 1
                    if quiet >= 2:
                        break
        self.log.append((line, data))
        return data

    def drain(self, seconds=0.3):
        end = time.time() + seconds
        data = ""
        while time.time() < end:
            try:
                chunk = self.c.sock.recv(65536)
                if not chunk:
                    break
                data += self.c.rx(chunk)
            except socket.timeout:
                pass
        return data
