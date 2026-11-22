#!/usr/bin/env python3
"""End-to-end smoke test for the LesMudOS proof of concept.

Starts the driver against a temporary copy of the mudlib, logs two players in
over TCP, and checks: movement, object-supplied verbs, call_out, heartbeat,
persistence, wizard levels and promotion, in-game file editing, live update of a room and of a base
class, compile-error handling, the sandbox, the evaluation-cost limit and the
recursion guard.

Usage (from the repository root, after `dotnet build`):
    python3 tests/smoke_test.py
"""
import json
import os
import re
import shutil
import socket
import subprocess
import sys
import tempfile
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PORT = int(os.environ.get("LESMUDOS_TEST_PORT", "4777"))
SECRET = "correct horse battery staple"
PASSWORD = "Tr0ub4dor&3"
IAC_WILL_ECHO = bytes([255, 251, 1])
IAC_WONT_ECHO = bytes([255, 252, 1])
failures = []

# Colour is on by default, so the game sends ANSI sequences. Client strips them
# (SGR sequences only: ESC [ digits and semicolons m) so that every check reads
# plain text; the raw text is kept in Client.raw for the colour checks.
DEFAULT_ARCHETYPE = "adventurer"

# The tests written before classes existed assume a bare character: 30 Vitality, 25 gold, no kit, unarmed 2-5, no
# equipment limits, nothing that changes timing. They get one as a test-only archetype in their temporary mudlib.
NOVICE_CLASS = """// @inherit /std/pclass
public class NoviceClass : PlayerClass
{
    public override void Create()
    {
        ClassName = "Novice"; Tier = "archetype"; SortOrder = 99; Title = "Novice";
        Tagline = "A bare test character."; Description = "Test only.";
        StartReaction = 2.5; VitalityBase = 12; VitalityPerLevel = 8; EnduranceBase = 55; EndurancePerLevel = 0; FocusBase = 55; FocusPerLevel = 0;
        // Attributes 5 each (no derived effect), and class modifiers that keep the bare character's regeneration exactly as before
        // the attribute system: 1 Vitality, 6 Endurance and 5 Focus per 10 s (the formulas give 0.8, 6.3 and 3.5).
        Modifiers["vitality_regen"] = 25; Modifiers["endurance_regen"] = -100.0 / 21; Modifiers["focus_regen"] = 300.0 / 7;
        AttackRate = 1.0; StartGold = 25;
        Weapons.Add("*"); ArmorTypes.AddRange(new[] { "light", "medium", "heavy" }); Shield = true;
        Traits.Add("novice_fists");
    }
}
"""
NOVICE_FISTS = """// @inherit /std/trait
public class NoviceFistsTrait : Trait
{
    public override void Create()
    {
        Title = "Novice Fists"; Summary = "Unarmed damage 2-5 (test only).";
        Mods["unarmed_min"] = 1; Mods["unarmed_max"] = 2;
    }
}
"""
ANSI = re.compile("\x1b\\[[0-9;]*m")
ANSI_TAIL = re.compile("\x1b(\\[[0-9;]*)?$")


PROMPT = re.compile(r"(?<!-)> ")
# A prompt on a line of its own: the plain "> " or the default "Name (vit/max) (endurance/max) (focus/max) > ".
PROMPT_LINE = re.compile(r"(?m)^(?:[A-Za-z]+ \(\d+/\d+\) \(\d+/\d+\) \(\d+/\d+\) )?> ?(?=\r?\n|$)")


class Client:
    def __init__(self, name, password=PASSWORD, login=True, archetype=None):
        """Connect and give the name. With login, also answer the password
        prompts: once for an existing account, twice for a new one or one
        that has no password yet, and then the choice of a calling if the
        game asks for one (a new character, or one saved before classes; the
        default is the adventurer, whose kit and unarmed damage are closest
        to the character the older tests were written for). Text read past
        the name that is not a password prompt (a refusal) is kept for the
        next read_until."""
        self.pending = ""
        self.raw = ""  # everything received since the last raw_reset(), with escape sequences
        self._carry = ""  # an escape sequence cut by the end of a chunk
        self.sock = socket.create_connection(("127.0.0.1", PORT), timeout=5)
        self.sock.settimeout(0.2)
        self.read_until("known?")
        self.send(name)
        if not login:
            return
        text = self.read_any(("Password: ", "characters): ", "Another name?"))
        if "characters): " in text:
            self.send(password)
            self.read_until("again: ")
            self.send(password)
            self.choose_calling(archetype)
        elif "Password: " in text:
            self.send(password)
            self.choose_calling(archetype)
        else:
            self.pending = text

    def choose_calling(self, archetype):
        """After the password: if the game asks for a calling, choose `archetype` (None leaves the prompt unanswered).
        Everything read on the way is kept for the next read_until."""
        archetype = DEFAULT_ARCHETYPE if archetype is None else archetype
        text = self.read_any(("Your calling: ", "> ", "Wrong password", "Goodbye", "Another name?", "Someone else"), timeout=15.0)
        if archetype is not None and "Your calling: " in text and not text.rstrip().endswith("There is no such calling. Your calling:"):
            self.send(archetype)
        self.pending = text

    def rx(self, chunk):
        """Decode a received chunk, keep it in self.raw, and return it without ANSI sequences."""
        text = self._carry + chunk.decode(errors="replace")
        tail = ANSI_TAIL.search(text)
        self._carry = text[tail.start():] if tail else ""
        text = text[:tail.start()] if tail else text
        self.raw = (self.raw + text)[-200000:]
        return ANSI.sub("", text)

    def raw_reset(self):
        self.raw = ""

    def raw_command(self, line, marker="> ", timeout=30.0):
        """Send a command and return what it printed with the escape sequences left in."""
        self.raw_reset()
        self.command(line, marker, timeout)
        return self.raw

    def read_any(self, markers, timeout=30.0):
        """Read until any of `markers` appears. Returns everything read."""
        deadline = time.time() + timeout
        data = ""
        while time.time() < deadline and not any(m in data for m in markers):
            try:
                chunk = self.sock.recv(65536)
                if not chunk:
                    break
                data += self.rx(chunk)
            except socket.timeout:
                pass
        return data

    def read_raw_until(self, marker, timeout=30.0):
        """Read raw bytes until the bytes `marker` appear. Returns everything read."""
        deadline = time.time() + timeout
        data = b""
        while time.time() < deadline and marker not in data:
            try:
                chunk = self.sock.recv(65536)
                if not chunk:
                    break
                data += chunk
            except socket.timeout:
                pass
        return data

    def closed(self, timeout=10.0):
        """True if the server closes the connection within the timeout."""
        deadline = time.time() + timeout
        while time.time() < deadline:
            try:
                if not self.sock.recv(65536):
                    return True
            except socket.timeout:
                pass
            except OSError:
                return True
        return False

    def send(self, line):
        self.sock.sendall((line + "\r\n").encode())

    @staticmethod
    def seen(marker, data):
        """Has `marker` appeared in `data`? The prompt "> " is not found inside "-> ": `stat` prints "Exit north -> /domains/..." and
        a reply cut off there leaves the real prompt behind, so that every later reply is read one command late."""
        return PROMPT.search(data) is not None if marker == "> " else marker in data

    def read_until(self, marker, timeout=30.0):
        """Read until `marker` appears. Returns everything read."""
        deadline = time.time() + timeout
        data, self.pending = self.pending, ""
        if self.seen(marker, data):
            return data + self.linger(marker)
        while time.time() < deadline:
            try:
                chunk = self.sock.recv(65536)
                if not chunk:
                    break
                data += self.rx(chunk)
                if self.seen(marker, data):
                    return data + self.linger(marker)
            except socket.timeout:
                pass
        return data

    def linger(self, marker):
        """Reading up to some text that is not the prompt: the prompt the server sends after that output may still be on its way.
        Take it in now (without the prompt line), or it would be taken for the end of the next command's reply."""
        if marker == "> ":
            return ""
        got = ""
        end = time.time() + 0.12
        while time.time() < end:
            try:
                chunk = self.sock.recv(65536)
                if not chunk:
                    break
                got += self.rx(chunk)
                if PROMPT_LINE.search(got):
                    break
            except socket.timeout:
                pass
        return PROMPT_LINE.sub("", got)

    def ed(self, line, timeout=30.0):
        """Send a line to the in-game editor and read until its ':' prompt."""
        self.send(line)
        deadline = time.time() + timeout
        data = ""
        while time.time() < deadline and not data.endswith(":"):
            try:
                data += self.rx(self.sock.recv(65536))
            except socket.timeout:
                pass
        return data

    def absorb(self):
        """Take in what has already arrived and keep it for the next read, without the prompts the server sent after output nobody
        asked for (a combat round, someone arriving): the prompt now follows unprompted output too, and a prompt left unread would be
        taken for the end of the next command's reply. Only the prompt lines go; the text stays."""
        data = ""
        quiet_since = time.time()
        while True:
            self.sock.settimeout(0.0)
            try:
                chunk = self.sock.recv(65536)
                if not chunk:
                    break
                data += self.rx(chunk)
                quiet_since = time.time()
                continue
            except (BlockingIOError, socket.timeout):
                pass
            finally:
                self.sock.settimeout(0.2)
            # Something arrived unasked: let it finish (its prompt may follow a moment later) before the next command goes out.
            if not data or time.time() - quiet_since > 0.06:
                break
            time.sleep(0.01)
        if data:
            self.pending += PROMPT_LINE.sub("", data)

    sync_default = True  # class-wide: False puts every client back to reading until the prompt (the raw mode)
    _sync_count = 0

    def command(self, line, marker="> ", timeout=30.0, sync=None):
        """Send a command and return its reply.

        Waiting for the prompt (marker "> ") cannot tell the prompt that ends a reply from one that follows unprompted output (a combat
        round, someone arriving), so by default the reply ends where the game answers a second line sent right behind the command:
        `sync TOKEN` prints "[sync TOKEN]", the token unique to this call. Everything before that line is the reply, as before it ends with the
        prompt the command itself earned. Pass sync=False (or set Client.sync_default = False) for the raw mode, which reads until the prompt:
        for the checks of the prompt itself, the login sequence and anything that asks a question. Any other marker reads until that text."""
        if marker != "> " or not (self.sync_default if sync is None else sync):
            if marker == "> ":
                self.absorb()
            self.send(line)
            return self.read_until(marker, timeout)
        self.absorb()  # only tidiness (the prompts of earlier unprompted output leave the front of the reply); the token decides where the reply ends
        Client._sync_count += 1
        token = "q%dx%d" % (os.getpid() % 10000, Client._sync_count)
        end = "[sync %s]" % token
        data, self.pending = self.pending, ""
        try:
            self.sock.sendall((line + "\r\nsync " + token + "\r\n").encode())
        except OSError:
            return data
        deadline = time.time() + timeout
        while end not in data and time.time() < deadline:
            try:
                chunk = self.sock.recv(65536)
                if not chunk:
                    return data
                data += self.rx(chunk)
            except socket.timeout:
                pass
            except OSError:
                return data
        cut = data.find(end)
        if cut < 0:
            return data
        reply, rest = data[:cut], data[cut + len(end):].lstrip("\r\n")
        # The prompt the game sends after the sync line comes straight behind it: take it in so it does not lead the next reply.
        # (A custom prompt may not look like one: then a quiet moment ends the wait, and at worst it leads the next reply.)
        stop = time.time() + 0.3
        quiet = time.time() + 0.05
        while time.time() < min(stop, quiet) and not PROMPT_LINE.match(rest):
            try:
                chunk = self.sock.recv(65536)
                if not chunk:
                    break
                rest += self.rx(chunk)
                quiet = time.time() + 0.05
            except (socket.timeout, OSError):
                pass
        prompt = PROMPT_LINE.match(rest)
        self.pending = rest[prompt.end():].lstrip("\r\n") if prompt else rest
        return reply


def check(label, text, *expected, absent=()):
    ok = all(e in text for e in expected) and not any(a in text for a in absent)
    print(("PASS  " if ok else "FAIL  ") + label)
    if not ok:
        failures.append(label)
        print("      expected:", expected, "absent:", absent)
        print("      got:", repr(text[-600:]))


def write(mudlib, path, source):
    with open(os.path.join(mudlib, path.lstrip("/") + ".cs"), "w") as f:
        f.write(source)


def edit(mudlib, path, old, new):
    file = os.path.join(mudlib, path.lstrip("/") + ".cs")
    with open(file) as f:
        source = f.read()
    assert old in source, f"{old!r} not in {path}"
    with open(file, "w") as f:
        f.write(source.replace(old, new))


def copy_mudlib(destination, novice=False):
    """Copy the mudlib for a test run, leaving out the build output beside Mudlib.csproj
    (only at the top: domains may have an obj/ directory of their own)."""
    source = os.path.join(ROOT, "mudlib")

    def skip(directory, names):
        top = os.path.abspath(directory) == os.path.abspath(source)
        return [n for n in names if (top and n in ("bin", "obj")) or n.endswith(".csproj")]

    shutil.copytree(source, destination, ignore=skip)
    if novice:
        global DEFAULT_ARCHETYPE
        DEFAULT_ARCHETYPE = "novice"
        write(destination, "/classes/novice", NOVICE_CLASS)
        write(destination, "/skills/novice_fists", NOVICE_FISTS)


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-test-")
    mudlib = os.path.join(work, "mudlib")
    copy_mudlib(mudlib)
    # New players start in Vincent's Hollow; this test is about the driver and the Town demo, so start them there.
    edit(mudlib, "/std/player", '"/domains/hollow/village/square"', '"/domains/town/square"')
    driver_dll = os.path.join(ROOT, "src", "LesMudOS.Driver", "bin", "Debug", "net10.0", "LesMudOS.Driver.dll")
    log = open(os.path.join(work, "driver.log"), "w")
    driver = subprocess.Popen(
        ["dotnet", driver_dll, "--mudlib", mudlib, "--save", os.path.join(work, "save"), "--port", str(PORT)],
        stdout=log, stderr=subprocess.STDOUT, env={**os.environ, "LESMUDOS_ADMIN_SECRET": SECRET})
    try:
        for _ in range(100):
            try:
                socket.create_connection(("127.0.0.1", PORT), timeout=0.3).close()
                break
            except OSError:
                time.sleep(0.2)

        # ---- login, rooms, objects ----
        a = Client("alice")
        text = a.read_until("> ")
        check("login shows start room", text, "Welcome, Alice", "Town Square", "A brass lantern is here.", "Obvious exits: north")

        b = Client("bob")
        b.read_until("> ")
        check("arrival is announced to the room", a.read_until("enters the game"), "Bob enters the game.")
        check("duplicate name refused", Client("alice").read_until("Another name?"), "already here")

        check("say reaches other players", (a.command("say hello"), b.read_until("hello"))[1], "Alice says: hello")
        check("unknown verb", a.command("dance"), "What?")
        check("room-supplied exit verb", a.command("north"), "The Compiled Griffin", "A ginger tavern cat is here.")
        check("departure seen by others", b.read_until("leaves north"), "Alice leaves north.")
        check("exit verb gone after leaving the room", a.command("north"), "What?")
        check("object-supplied verb", a.command("pet cat"), "purrs")
        check("notify_fail", a.command("pet dog"), "Pet what?")
        check("heartbeat", a.read_until("The cat ", timeout=15), "The cat ")
        check("abbreviated exit", a.command("s"), "Town Square", "Bob the newcomer is here.")

        # ---- inventory, call_out ----
        check("get", a.command("get lantern"), "You take a brass lantern.")
        check("verb travels with carried item", (a.command("n"), a.command("light lantern"))[1], "You light the lantern")
        check("inventory", a.command("i"), "a brass lantern (lit)")
        check("call_out fires", a.read_until("goes dark", timeout=30), "Your lantern sputters")
        a.command("s")

        # ---- wizard levels ----
        check("level 0: update does not exist", a.command("update /domains/town/square"), "What?", absent=("Recompiled",))
        check("level 0: mudstat does not exist", a.command("mudstat"), "What?", absent=("Objects",))
        check("level 0: shutdown does not exist", a.command("shutdown"), "What?")
        check("level 0: pwd, cd, list do not exist", a.command("pwd") + a.command("cd /std") + a.command("list") + a.command("ls"), "What?", absent=("/std", "secure"))
        check("level 0: cat, mkdir, edit do not exist", a.command("cat /std/room") + a.command("mkdir /x") + a.command("edit /x.cs"), "What?", absent=("Room", "Created", "editor"))
        check("level 0: copy, move, rename do not exist", a.command("copy /std/room /x") + a.command("move /std/room /x") + a.command("rename /std/room x"), "What?", absent=("Copied", "Moved", "Renamed", "Usage"))
        check("level 0: delete, rmdir do not exist", a.command("delete /std/room") + a.command("rmdir /std"), "What?", absent=("(y/n)", "Deleted", "Removed", "not empty", "Usage"))
        check("level 0: promote does not exist", a.command("promote bob 5"), "What?")
        check("level 0: help lists no wizard commands", a.command("help"), "Commands:", absent=("Wizard", "promote"))
        check("wrong secret is indistinguishable from no command", a.command("promote not-the-secret"), "What?")
        boot_text = a.command("promote " + SECRET)
        check("secret creates the first level-10 wizard", boot_text, "You are now a level 10 wizard")
        check("wizard homes: bootstrap creates the home and says so in one line", boot_text,
              "Your wizard home directory, /wiz/alice, has been created. Type cd ~ to go there")
        check("wizard homes: /wiz/alice exists on disk after the bootstrap", str(os.path.isdir(os.path.join(mudlib, "wiz", "alice"))), "True")
        check("secret refused once a level-10 wizard exists", b.command("promote " + SECRET), "What?")
        # The wizard help text itself contains "> ", so wait for its last line instead of the prompt.
        check("help now lists wizard commands", a.command("help", marker="stop the driver"), "Wizard commands at your level (10)", "shutdown")
        a.read_until("> ")
        check("promote above own level refused", a.command("promote bob 11"), "from 0 to 10")
        check("promote unknown player refused", a.command("promote carol 3"), "No player of that name is online")
        check("promote self refused", a.command("promote alice 5"), "cannot change your own level")
        check("promote to level 5", a.command("promote bob 5"), "bob is now wizard level 5")
        check("promoted player is told", b.read_until("level is now"), "Your wizard level is now 5.")
        check("level 5 can use mudstat", b.command("mudstat"), "Objects:")
        check("level 5 cannot shut down", b.command("shutdown"), "What?")
        check("level 5 cannot promote past own level", b.command("promote alice 6"), "from 0 to 5")
        check("level 5 cannot demote a superior", b.command("promote alice 0"), "below your own level")
        write(mudlib, "/domains/town/imposter", """
// @inherit /std/item
public class Imposter : Item
{
    public override void Create() { Name = "imposter"; }
    public override void Init() { AddAction("usurp", Usurp); AddAction("forge", Forge); AddAction("guess", Guess); AddAction("peek", Peek); AddAction("scribble", Scribble); AddAction("duplicate", Duplicate); AddAction("shuffle", Shuffle); AddAction("erase", Erase); AddAction("prune", Prune); }
    private bool Erase(string args) { RemoveFile("/domains/town/square.cs"); return true; }
    private bool Prune(string args) { RemoveDir("/domains/lab/ghost"); return true; }
    private bool Scribble(string args) { WriteFile("/domains/town/hack.cs", "x"); return true; }
    private bool Duplicate(string args) { CopyFile("/domains/town/square.cs", "/domains/town/hack.cs"); return true; }
    private bool Shuffle(string args) { RenameFile("/domains/town/square.cs", "/domains/town/hack.cs"); return true; }
    private bool Peek(string args) { Write(GetDir("/secure").Count.ToString()); return true; }
    private bool Usurp(string args)
    {
        Write("usurp: " + LoadObject("/secure/wizlist").Call<string>("Promote", "bob", 10) + "\\n");
        return true;
    }
    private bool Guess(string args)
    {
        Write("guess: " + LoadObject("/secure/wizlist").Call<bool>("Bootstrap", args) + " " + CheckAdminSecret(args) + "\\n");
        return true;
    }
    private bool Forge(string args) { SaveObject("/secure/wizlist"); return true; }
}
""")
        check("level 5 can clone", b.command("clone /domains/town/imposter"), "Cloned /domains/town/imposter#")
        check("other objects cannot promote on a wizard's behalf", b.command("usurp"), "usurp: Only a player can do that.")
        check("other objects cannot write the registry", b.command("forge"), "may not write /secure/wizlist")
        check("other objects cannot test the secret", b.command("guess " + SECRET), "may not check the admin secret")
        check("wizards lists levels", a.command("wizards"), "alice (10)", "bob (5)")
        check("who shows wizard level", b.command("who"), "Alice the newcomer [wizard 10]", "Bob the newcomer [wizard 5]")
        # ---- wizard file commands ----
        check("pwd starts at the mudlib root", a.command("pwd"), "/\r\n")
        check("list shows directories", a.command("list"), "/:", "domains/", "secure/", "std/")
        check("cd into a relative directory", a.command("cd domains/town"), "/domains/town")
        check("list shows files with sizes", a.command("list"), "/domains/town:", "square.cs", "lantern.cs", "imposter.cs")
        check("ls is an alias", a.command("ls"), "square.cs")
        check("cd ..", a.command("cd .."), "/domains\r\n")
        check("list with a relative argument", a.command("list town"), "/domains/town:", "cat.cs")
        check("list with an absolute argument", a.command("list /std"), "/std:", "player.cs", "room.cs")
        check("cd cannot climb out of the mudlib", a.command("cd ../../../.."), "/\r\n", absent=("etc", "tmp"))
        check("list cannot climb out of the mudlib", a.command("list ../.."), "/:", "std/", absent=("mudlib",))
        check("cd to a missing directory", a.command("cd /nowhere"), "/nowhere: no such directory")
        check("cd to a file is refused", a.command("cd /std/room.cs"), "no such directory")
        check("pwd unchanged after failed cd", a.command("pwd"), "/\r\n")
        check("hidden names are refused", a.command("list /.git"), "Illegal path")
        check("level 5 wizard can use pwd", b.command("pwd"), "/\r\n")
        check("other objects cannot list directories", b.command("peek"), "may not get-dir /secure")
        a.command("cd /domains/town")
        # CHANGED (wizard homes): cd with no argument now goes to the home, not to /. "cd /" still goes to the root.
        check("cd with no argument goes to the home", a.command("cd"), "/wiz/alice\r\n")
        check("cd / still goes to the root", a.command("cd /"), "/\r\n")
        a.command("cd /domains/town")

        # ---- mkdir, cat and the line editor ----
        a.command("cd /domains")
        check("mkdir", a.command("mkdir lab"), "Created /domains/lab/")
        check("mkdir of an existing directory", a.command("mkdir lab"), "/domains/lab already exists")
        check("mkdir needs an existing parent", a.command("mkdir /no/such"), "No such parent directory")
        check("new directory is listed", a.command("list"), "lab/", "town/")
        a.command("cd lab")
        check("edit a file in a missing directory", a.command("edit /nowhere/thing"), "no such directory")

        check("edit starts a new file", a.ed("edit widget"), "New file /domains/lab/widget.cs", "You are in the editor")
        check("editor help", a.ed("h"), "Editor commands", "write the file and leave")
        a.command("a", marker="only .")
        for line in ["// @inherit /std/item", "public class Widget : Item", "{", "    public override void Create()", "    {",
                     '        Name = "widget";', '\tLongDesc = "A widget made in-game.";', "    }", "}"]:
            a.send(line)
        check("pasted lines are stored with their indentation", (a.ed("."), a.ed("p"))[1],
              "   1  // @inherit /std/item", '   6          Name = "widget";', '   7  \tLongDesc', "   9  }")
        check("print a range", a.ed("p 2 3"), "   2  public class Widget", "   3  {", absent=("   1  ", "   4  "))
        check("bad line number", a.ed("d 99"), "No such line")
        check("leaving with unsaved changes is refused", a.ed("q"), "unsaved changes")
        check("save and leave", a.command("x"), "Wrote /domains/lab/widget.cs, 9 lines", "update /domains/lab/widget", "Left the editor")
        check("back at the command prompt", a.command("pwd"), "/domains/lab")
        check("cat shows the new file", a.command("cat widget"), "/domains/lab/widget.cs (9 lines)", "   2  public class Widget")
        check("file created in-game compiles and clones", a.command("clone widget"), "Cloned /domains/lab/widget#")
        check("object from the new file works", a.command("look at widget"), "A widget made in-game.")

        check("edit an existing file", a.ed("edit widget.cs"), "Editing /domains/lab/widget.cs, 9 lines")
        a.command("c 7", marker="only .")
        a.send('        LongDesc = "A widget edited live.";')
        check("replace a line", (a.ed("."), a.ed("p 7"))[1], "   7          LongDesc = \"A widget edited live.\";")
        a.command("i 1", marker="only .")
        a.send("// first line")
        check("insert before a line", (a.ed("."), a.ed("p 1 2"))[1], "   1  // first line", "   2  // @inherit")
        check("delete a line", a.ed("d 1"), "Deleted 1 line(s).")
        check("write keeps the editor open", a.ed("w"), "Wrote /domains/lab/widget.cs, 9 lines")
        check("leave", a.command("q"), "Left the editor")
        check("update picks up the edited file", a.command("update widget"), "Recompiled /domains/lab/widget")
        a.command("drop widget")
        a.command("clone widget")
        check("new clone runs the edited code", a.command("look at widget"), "A widget edited live.")
        a.command("drop widget")

        check("discard changes with q!", (a.ed("edit widget"), a.ed("d 1 9"), a.command("q!"), a.command("cat widget"))[3], "(9 lines)", "edited live")
        check("paths cannot leave the mudlib", a.command("cat ../../../../etc/passwd"), "/etc/passwd.cs: no such file", absent=("root:",))

        check("level 5 cannot edit /std", b.command("edit /std/room"), "need wizard level 10")
        check("level 5 cannot mkdir in /secure", b.command("mkdir /secure/x"), "need wizard level 10")
        check("level 5 can edit elsewhere", b.ed("edit /domains/lab/notes.txt"), "New file /domains/lab/notes.txt")
        b.command("a", marker="only .")
        b.send("remember the milk")
        b.ed(".")
        check("level 5 can save", b.command("x"), "Wrote /domains/lab/notes.txt, 1 lines", absent=("update",))
        check("other objects cannot write files", b.command("scribble"), "may not write-file /domains/town/hack.cs")

        # ---- copy, move and rename ----
        check("copy a file", a.command("copy widget gadget"), "Copied /domains/lab/widget.cs to /domains/lab/gadget.cs.")
        check("cat the copy", a.command("cat gadget"), "/domains/lab/gadget.cs (9 lines)", "edited live")
        a.command("mkdir sub")
        check("copy into a directory keeps the name", a.command("copy widget sub"), "Copied /domains/lab/widget.cs to /domains/lab/sub/widget.cs.")
        check("copied file is in the directory", a.command("list sub"), "/domains/lab/sub:", "widget.cs")
        check("copy refuses to overwrite", a.command("copy widget gadget"), "/domains/lab/gadget.cs already exists", absent=("Copied",))
        check("copy of a missing file", a.command("copy nothing other"), "/domains/lab/nothing.cs: no such file.")
        check("copy into a missing directory", a.command("copy widget /nowhere/widget"), "No such directory for /nowhere/widget.cs")
        check("copy onto itself", a.command("copy widget widget.cs"), "are the same file")
        # Usage messages contain "> ", so wait for the end of the line and the real prompt.
        check("copy needs two arguments", a.command("copy widget", marker="<to>\r\n> "), "Usage: copy <from> <to>")
        check("move across directories", a.command("move gadget /domains/town"), "Moved /domains/lab/gadget.cs to /domains/town/gadget.cs.")
        check("moved file is gone from the source", a.command("cat gadget"), "/domains/lab/gadget.cs: no such file.")
        check("moved file is at the destination", a.command("cat /domains/town/gadget"), "/domains/town/gadget.cs (9 lines)")
        check("move refuses to overwrite", a.command("move /domains/town/gadget sub/widget"), "/domains/lab/sub/widget.cs already exists", absent=("Moved",))
        check("rename a file", a.command("rename /domains/town/gadget gizmo"), "Renamed /domains/town/gadget.cs to /domains/town/gizmo.cs.")
        check("renamed file has the new name", a.command("cat /domains/town/gizmo") + a.command("cat /domains/town/gadget"), "/domains/town/gizmo.cs (9 lines)", "/domains/town/gadget.cs: no such file.")
        check("rename refuses a name with a slash", a.command("rename /domains/town/gizmo ../gizmo"), "cannot contain '/'", absent=("Renamed",))
        check("rename cannot climb with ..", a.command("rename /domains/town/gizmo .."), "Illegal path", absent=("Renamed",))
        check("rename needs two arguments", a.command("rename widget", marker="<newname>\r\n> "), "Usage: rename <file> <newname>")
        a.command("move /domains/town/gizmo sub")
        check("level 5 cannot copy into /std", b.command("copy /domains/lab/notes.txt /std"), "need wizard level 10", absent=("Copied",))
        check("level 5 cannot move out of /std", b.command("move /std/room /domains/lab"), "need wizard level 10", absent=("Moved",))
        check("level 5 cannot move into /secure", b.command("move /domains/lab/notes.txt /secure"), "need wizard level 10", absent=("Moved",))
        check("level 5 cannot rename in /std", b.command("rename /std/room hall"), "need wizard level 10", absent=("Renamed",))
        check("level 5 can copy from /std elsewhere", b.command("copy /std/room /domains/lab"), "Copied /std/room.cs to /domains/lab/room.cs.")
        check("level 5 can move elsewhere", b.command("move /domains/lab/room.cs /domains/lab/sub/hall.cs"), "Moved /domains/lab/room.cs to /domains/lab/sub/hall.cs.")
        check("source under /std is untouched", a.command("list /std"), "room.cs")
        check("other objects cannot copy files", b.command("duplicate"), "may not read-file /domains/town/square.cs")
        check("other objects cannot rename files", b.command("shuffle"), "may not write-file /domains/town/square.cs")

        # ---- delete and rmdir ----
        lab = os.path.join(mudlib, "domains", "lab")
        sentinel = os.path.join(work, "sentinel.txt")  # beside the mudlib, outside it
        with open(sentinel, "w") as f:
            f.write("keep me")
        a.command("copy widget doomed")
        # The question ends without a prompt, so wait for its own last words.
        check("delete asks for confirmation", a.command("delete doomed", marker="(y/n) "), "Delete /domains/lab/doomed.cs? This cannot be undone. (y/n) ")
        check("delete answered n", a.command("n"), "Not deleted.", absent=("Deleted",))
        check("file kept after n", a.command("cat doomed"), "/domains/lab/doomed.cs (9 lines)")
        a.command("delete doomed", marker="(y/n) ")
        check("any answer but y or yes keeps the file", a.command("yeah"), "Not deleted.")
        a.command("delete doomed", marker="(y/n) ")
        check("delete answered y", a.command(" Y "), "Deleted /domains/lab/doomed.cs.", absent=("still loaded",))
        check("deleted file is gone", a.command("cat doomed"), "/domains/lab/doomed.cs: no such file.")
        check("deleted file is gone from disk", str(os.path.exists(os.path.join(lab, "doomed.cs"))), "False")
        a.command("copy widget doomed")
        a.command("delete doomed.cs", marker="(y/n) ")
        check("delete answered yes", a.command("YES"), "Deleted /domains/lab/doomed.cs.")
        write(mudlib, "/domains/lab/lure", """
// @inherit /std/item
public class Lure : Item
{
    public override void Create() { Name = "lure"; }
    public override void Init() { AddAction("tempt", Tempt); }
    private bool Tempt(string args)
    {
        CallOut(() => Write("Lure: would you like a cookie? (y/n) "), 1);
        return true;
    }
}
""")
        a.command("copy widget doomed")
        a.command("clone lure")
        a.command("tempt")
        a.command("delete doomed", marker="(y/n) ")
        a.read_until("cookie? (y/n) ", timeout=10)
        check("a y meant for another object's question does not delete", a.command("y"), "Other messages arrived after the question, so /domains/lab/doomed.cs was not deleted.", absent=("Deleted",))
        check("so the file is still on disk", str(os.path.exists(os.path.join(lab, "doomed.cs"))), "True")
        a.command("delete doomed", marker="(y/n) ")
        a.command("y")
        check("delete of a missing file", a.command("delete nothing"), "/domains/lab/nothing.cs: no such file.", absent=("(y/n)",))
        check("delete of a directory is refused", a.command("delete sub"), "/domains/lab/sub is a directory; use rmdir", absent=("(y/n)",))
        check("delete of the root is refused", a.command("delete /"), "/ is a directory", absent=("(y/n)",))
        check("delete needs an argument", a.command("delete", marker="<file>\r\n> "), "Usage: delete <file>")
        write(mudlib, "/domains/lab/beacon", """
// @inherit /std/item
public class Beacon : Item
{
    public override void Create() { Name = "beacon"; }
    public override void Init() { AddAction("beckon", Beckon); }
    private bool Beckon(string args) { Write("Loaded " + LoadObject("/domains/lab/beacon").Name + ".\\n"); return true; }
}
""")
        a.command("clone beacon")
        check("load a blueprint", a.command("beckon"), "Loaded /domains/lab/beacon.")
        a.command("delete beacon", marker="(y/n) ")
        check("deleting a loaded file says it stays loaded", a.command("y"), "Deleted /domains/lab/beacon.cs.",
              "/domains/lab/beacon is still loaded and will stay until it is destructed or the driver restarts.")
        check("loaded file is gone from disk", str(os.path.exists(os.path.join(lab, "beacon.cs"))), "False")

        a.command("mkdir empty")
        check("rmdir of an empty directory", a.command("rmdir empty"), "Removed /domains/lab/empty/")
        check("removed directory is gone from disk", str(os.path.exists(os.path.join(lab, "empty"))), "False")
        check("rmdir of a missing directory", a.command("rmdir empty"), "/domains/lab/empty: no such directory.")
        check("rmdir of a non-empty directory is refused", a.command("rmdir sub"), "/domains/lab/sub is not empty", absent=("Removed",))
        check("refused directory and its contents are still on disk", " ".join(sorted(os.listdir(os.path.join(lab, "sub")))), "gizmo.cs", "hall.cs", "widget.cs")
        a.command("mkdir ghost")
        open(os.path.join(lab, "ghost", ".hidden"), "w").close()
        check("a directory with only a hidden file lists as empty", a.command("list ghost"), "(empty)")
        check("rmdir counts hidden files", a.command("rmdir ghost"), "/domains/lab/ghost is not empty (it holds hidden files)", absent=("Removed",))
        check("hidden file is still on disk", str(os.path.exists(os.path.join(lab, "ghost", ".hidden"))), "True")
        check("rmdir of a file is refused", a.command("rmdir widget.cs"), "/domains/lab/widget.cs is not a directory")
        check("widget is still on disk", str(os.path.exists(os.path.join(lab, "widget.cs"))), "True")
        a.command("mkdir here")
        a.command("cd here")
        check("rmdir of the current directory is refused", a.command("rmdir /domains/lab/here"), "You are in that directory.")
        check("rmdir . is refused", a.command("rmdir ."), "You are in that directory.")
        check("rmdir of a parent of the current directory is refused", a.command("rmdir .."), "You are inside that directory.")
        a.command("cd ..")
        check("rmdir after leaving the directory", a.command("rmdir here"), "Removed /domains/lab/here/")
        check("rmdir needs an argument", a.command("rmdir", marker="<dir>\r\n> "), "Usage: rmdir <dir>")
        check("rmdir of the root is refused", a.command("rmdir /"), "/ is the mudlib root and cannot be removed")
        a.command("cd /")
        check("rmdir .. from the root is the root", a.command("rmdir .."), "/ is the mudlib root and cannot be removed")
        a.command("cd /domains/lab")
        check("delete cannot climb out of the mudlib", a.command("delete ../../../../etc/passwd"), "/etc/passwd.cs: no such file.", absent=("(y/n)",))
        check("delete cannot reach a file beside the mudlib", a.command("delete ../../../sentinel.txt"), "/sentinel.txt: no such file.", absent=("(y/n)",))
        check("rmdir cannot reach the mudlib itself", a.command("rmdir ../../../../mudlib"), "/mudlib: no such directory.")
        check("hidden names are refused by delete", a.command("delete ghost/.hidden"), "Illegal path", absent=("(y/n)",))
        os.symlink(sentinel, os.path.join(lab, "link.txt"))
        outside = os.path.join(work, "outside")
        os.mkdir(outside)
        os.symlink(outside, os.path.join(lab, "linkdir"))
        a.command("delete link.txt", marker="(y/n) ")
        check("delete refuses a symbolic link", a.command("y"), "/domains/lab/link.txt goes through a symbolic link", absent=("Deleted",))
        check("rmdir refuses a symbolic link", a.command("rmdir linkdir"), "/domains/lab/linkdir goes through a symbolic link", absent=("Removed",))
        check("links and their targets are untouched",
              str([os.path.islink(os.path.join(lab, "link.txt")), os.path.islink(os.path.join(lab, "linkdir")), os.path.isdir(outside)]), "[True, True, True]")
        os.remove(os.path.join(lab, "link.txt"))
        os.remove(os.path.join(lab, "linkdir"))
        check("sentinel outside the mudlib still exists", open(sentinel).read(), "keep me")

        check("level 5 cannot delete under /std", b.command("delete /std/room"), "need wizard level 10", absent=("(y/n)",))
        check("level 5 cannot delete under /secure", b.command("delete /secure/master"), "need wizard level 10", absent=("(y/n)",))
        check("level 5 cannot rmdir /std or under /secure", b.command("rmdir /std") + b.command("rmdir /secure/x"), "need wizard level 10", absent=("Removed", "not empty", "no such"))
        os.mkdir(os.path.join(mudlib, "STD"))
        open(os.path.join(mudlib, "STD", "room.cs"), "w").close()
        check("level 5 cannot delete under /std in another case", b.command("delete /STD/room") + b.command("rmdir /Secure"), "need wizard level 10", absent=("(y/n)", "Removed", "no such"))
        check("so it is still on disk", str(os.path.exists(os.path.join(mudlib, "STD", "room.cs"))), "True")
        shutil.rmtree(os.path.join(mudlib, "STD"))
        check("/std/room.cs is still on disk", str(os.path.exists(os.path.join(mudlib, "std", "room.cs"))), "True")
        b.command("delete /domains/lab/sub/hall", marker="(y/n) ")
        check("level 5 can delete elsewhere", b.command("y"), "Deleted /domains/lab/sub/hall.cs.")
        b.command("delete /domains/lab/sub/gizmo", marker="(y/n) ")
        a.command("promote bob 4")
        b.read_until("level is now")
        check("level is checked again when the answer comes", b.command("y"), "You may no longer delete that file. Not deleted.")
        check("so the file is still on disk", str(os.path.exists(os.path.join(lab, "sub", "gizmo.cs"))), "True")
        # CHANGED (wizard homes): as above, a refusal naming the rule instead of "What?".
        check("level 4 cannot delete or rmdir outside the home", b.command("delete /domains/lab/sub/gizmo") + b.command("rmdir /domains/lab/ghost"),
              "You need wizard level 5 to change anything outside your own home directory", absent=("(y/n)", "not empty", "What?"))
        a.command("promote bob 5")
        b.read_until("level is now")
        check("other objects cannot delete files", b.command("erase"), "may not remove-file /domains/town/square.cs")
        check("other objects cannot remove directories", b.command("prune"), "may not remove-dir /domains/lab/ghost")
        check("their targets are still on disk", str([os.path.exists(os.path.join(mudlib, "domains", "town", "square.cs")), os.path.isdir(os.path.join(lab, "ghost"))]), "[True, True]")
        a.command("promote bob 1")
        b.read_until("level is now")
        check("level 1 can read files", b.command("cat /domains/lab/notes.txt"), "remember the milk")
        # CHANGED (wizard homes): a level-1 wizard now has the file commands (in their own home), so outside it they get
        # a refusal that names the rule instead of "What?".
        check("level 1 cannot edit or mkdir outside the home", b.command("edit /domains/lab/notes.txt") + b.command("mkdir /domains/lab/x"),
              "You need wizard level 5 to change anything outside your own home directory, /wiz/bob.", absent=("editor", "Created", "What?"))
        with open(os.path.join(mudlib, "domains", "lab", "widget.cs")) as f:
            check("file on disk matches", f.read(), "// @inherit /std/item\n", "edited live", absent=("\r",))
        a.command("cd /domains/town")

        check("demote to 0", a.command("promote bob 0"), "bob is now wizard level 0")
        b.read_until("level is now")
        check("demoted player loses commands at once", b.command("mudstat"), "What?", absent=("Objects",))
        b.command("drop imposter")

        # ---- wizard homes: /wiz/<name> ----
        # Every wizard has a home they fully control; elsewhere the old levels apply. See docs/DRIVER_API.md section 6.
        def on_disk(*parts):
            return os.path.exists(os.path.join(mudlib, *parts))

        def make_file(client, path, lines):
            """Create or extend a file with the in-game line editor, exactly as a wizard would."""
            client.ed("edit " + path)
            client.command("a", marker="only .")
            for line in lines:
                client.send(line)
            client.ed(".")
            return client.command("x")

        wiz_dir = os.path.join(mudlib, "wiz")
        check("wizard homes: /wiz is in the mudlib", str(os.path.isfile(os.path.join(wiz_dir, "README.md"))), "True")
        check("wizard homes: the home made at promotion exists and survived the demotion to 0", str(on_disk("wiz", "bob")), "True")
        mudlib_project = open(os.path.join(ROOT, "mudlib", "Mudlib.csproj")).read()
        check("wizard homes: Mudlib.csproj leaves wiz/** out of the build", mudlib_project, '<Compile Remove="wiz/**" />')

        # -- creation: at promote, and at login for a wizard whose home is gone --
        carol = Client("carol")
        carol.read_until("> ")
        a.command("promote carol 1")
        text = carol.read_until("has been created")
        check("wizard homes: promotion creates the home and tells the new wizard in one line", text, "Your wizard level is now 1.",
              "Your wizard home directory, /wiz/carol, has been created. Type cd ~ to go there")
        check("wizard homes: the home exists on disk", str(os.path.isdir(os.path.join(wiz_dir, "carol"))), "True")
        a.command("promote carol 2")
        check("wizard homes: a home that exists is not announced again", carol.command("pwd"), "level is now 2", absent=("has been created",))
        carol.send("quit")
        carol.read_until("Goodbye")
        os.rmdir(os.path.join(wiz_dir, "carol"))
        carol = Client("carol")
        text = carol.read_until("> ")
        check("wizard homes: login recreates a missing home and says so", text, "Welcome back, Carol", "Your wizard home directory, /wiz/carol, has been created.")
        check("wizard homes: the recreated home is on disk", str(os.path.isdir(os.path.join(wiz_dir, "carol"))), "True")
        carol.send("quit")
        carol.read_until("Goodbye")
        carol = Client("carol")
        check("wizard homes: a login with the home in place says nothing", carol.read_until("> "), "Welcome back, Carol", absent=("has been created",))

        # -- the owner at level 1 --
        a.command("promote bob 1")
        b.read_until("level is now 1")
        check("wizard homes: pwd and cd at the start", b.command("cd /"), "/\r\n")
        check("wizard homes: cd with no argument goes to the home", b.command("cd"), "/wiz/bob\r\n")
        b.command("cd /domains")
        check("wizard homes: cd ~ goes to the home", b.command("cd ~"), "/wiz/bob\r\n")
        check("wizard homes: pwd shows the real path", b.command("pwd"), "/wiz/bob\r\n", absent=("~",))
        check("wizard homes: cd / still goes to the root", b.command("cd /"), "/\r\n")
        check("wizard homes: level 1 mkdir in the home with ~", b.command("mkdir ~/rooms"), "Created /wiz/bob/rooms/")
        check("wizard homes: level 1 mkdir with a relative path in the home", (b.command("cd ~"), b.command("mkdir items"))[1], "Created /wiz/bob/items/")
        check("wizard homes: list ~ shows the new directories", b.command("list ~"), "/wiz/bob:", "rooms/", "items/")
        check("wizard homes: list ~/rooms", b.command("list ~/rooms"), "/wiz/bob/rooms:", "(empty)")

        text = make_file(b, "~/items/gem", [
            "// @inherit /std/item",
            "public class Gem : Item",
            "{",
            '    public override void Create() { Name = "gem"; LongDesc = "A gem that Bob made."; }',
            '    public override void Init() { AddAction("polish", Polish); }',
            '    private bool Polish(string args) { if (!Id(args)) { NotifyFail("Polish what?"); return false; } Write("The gem gleams.\\n"); return true; }',
            "}"])
        check("wizard homes: level 1 edits an item with the line editor", text, "Wrote /wiz/bob/items/gem.cs, 7 lines", "update /wiz/bob/items/gem")
        text = make_file(b, "~/rooms/hall", [
            "// @inherit /std/room",
            "public class Hall : Room",
            "{",
            "    public override void Create()",
            "    {",
            "        ShortDesc = \"Bob's Hall\";",
            "        LongDesc = \"A hall of Bob's own making.\";",
            '        Exits["out"] = "/domains/town/square";',
            '        MoveObject(CloneObject("/wiz/bob/items/gem"), Self);',
            "    }",
            "}"])
        check("wizard homes: level 1 edits a room with the line editor", text, "Wrote /wiz/bob/rooms/hall.cs, 11 lines")
        check("wizard homes: the files are on disk in the home", str([on_disk("wiz", "bob", "items", "gem.cs"), on_disk("wiz", "bob", "rooms", "hall.cs")]), "[True, True]")
        check("wizard homes: cat with ~", b.command("cat ~/rooms/hall"), "/wiz/bob/rooms/hall.cs (11 lines)", "public class Hall")

        check("wizard homes: level 1 updates their own file", b.command("update ~/rooms/hall"), "Recompiled /wiz/bob/rooms/hall")
        check("wizard homes: level 1 clones their own item", b.command("clone ~/items/gem"), "Cloned /wiz/bob/items/gem#")
        check("wizard homes: level 1 goto their own room", b.command("goto ~/rooms/hall"), "Bob's Hall", "A hall of Bob's own making.", "Gem is here.")
        check("wizard homes: the item they wrote works", b.command("polish gem"), "The gem gleams.")
        check("wizard homes: update replaces the loaded room and keeps the wizard in it", b.command("update ~/rooms/hall"), "Replaced /wiz/bob/rooms/hall; moved 1 occupant(s) back in.")
        check("wizard homes: the room they wrote has working exits", b.command("out"), "Town Square")
        b.command("goto ~/rooms/hall")
        b.command("out")

        check("wizard homes: level 1 copy within the home", b.command("copy ~/rooms/hall ~/rooms/hall2"), "Copied /wiz/bob/rooms/hall.cs to /wiz/bob/rooms/hall2.cs.")
        check("wizard homes: level 1 move within the home", b.command("move ~/rooms/hall2 ~/items"), "Moved /wiz/bob/rooms/hall2.cs to /wiz/bob/items/hall2.cs.")
        check("wizard homes: level 1 rename within the home", b.command("rename ~/items/hall2 attic"), "Renamed /wiz/bob/items/hall2.cs to /wiz/bob/items/attic.cs.")
        b.command("delete ~/items/attic", marker="(y/n) ")
        check("wizard homes: level 1 delete asks, then deletes", b.command("y"), "Deleted /wiz/bob/items/attic.cs.")
        check("wizard homes: the deleted file is gone from disk", str(on_disk("wiz", "bob", "items", "attic.cs")), "False")
        b.command("mkdir ~/rooms/tmp")
        check("wizard homes: level 1 rmdir within the home", b.command("rmdir ~/rooms/tmp"), "Removed /wiz/bob/rooms/tmp/")
        check("wizard homes: the owner can mkdir their own existing home", b.command("mkdir /wiz/bob"), "/wiz/bob already exists")

        # -- another wizard's home: read-only below level 5 --
        a.command("mkdir /wiz/alice/sub")
        make_file(a, "/wiz/alice/note", ["alice's note"])
        os.mkdir(os.path.join(wiz_dir, "alice", "keep"))  # a directory bob may not remove
        check("wizard homes: a level-1 wizard can list, cd and cat in another home", b.command("list /wiz/alice") + b.command("cat /wiz/alice/note") + b.command("cd /wiz/alice") + b.command("cd /"),
              "/wiz/alice:", "note.cs", "sub/", "alice's note", "/wiz/alice\r\n")
        other = "/wiz/alice is another wizard's home. Below wizard level 5 you may read it (list, cd, cat) but not change it."
        check("wizard homes: mkdir in another home refused", b.command("mkdir /wiz/alice/x"), other, absent=("Created",))
        check("wizard homes: edit in another home refused", b.command("edit /wiz/alice/x"), other, absent=("editor", "New file"))
        check("wizard homes: edit of an existing file in another home refused", b.command("edit /wiz/alice/note"), other, absent=("editor", "Editing"))
        check("wizard homes: copy into another home refused", b.command("copy ~/items/gem /wiz/alice"), other, absent=("Copied",))
        check("wizard homes: move out of another home refused", b.command("move /wiz/alice/note ~/items"), other, absent=("Moved",))
        check("wizard homes: move into another home refused", b.command("move ~/items/gem /wiz/alice/sub"), other, absent=("Moved",))
        check("wizard homes: rename in another home refused", b.command("rename /wiz/alice/note memo"), other, absent=("Renamed",))
        check("wizard homes: delete in another home refused", b.command("delete /wiz/alice/note"), other, absent=("(y/n)", "Deleted"))
        check("wizard homes: rmdir in another home refused", b.command("rmdir /wiz/alice/sub"), other, absent=("Removed",))
        check("wizard homes: nothing changed in the other home",
              str([on_disk("wiz", "alice", "note.cs"), on_disk("wiz", "alice", "sub"), on_disk("wiz", "alice", "x.cs"), on_disk("wiz", "alice", "x"), on_disk("wiz", "alice", "gem.cs")]),
              "[True, True, False, False, False]")
        check("wizard homes: a level-1 wizard can copy FROM another home into their own", b.command("copy /wiz/alice/note ~/items/memo"), "Copied /wiz/alice/note.cs to /wiz/bob/items/memo.cs.")
        b.command("delete ~/items/memo", marker="(y/n) ")
        b.command("y")
        check("wizard homes: clone and update of another wizard's files follow the old levels",
              b.command("clone /wiz/alice/thing") + b.command("update /wiz/alice/thing"),
              "You need wizard level 3 to clone anything outside your own home directory, /wiz/bob.",
              "You need wizard level 5 to update anything outside your own home directory, /wiz/bob.")

        # -- outside the home: the old levels --
        outside = "You need wizard level 5 to change anything outside your own home directory, /wiz/bob."
        check("wizard homes: mkdir in /domains refused", b.command("mkdir /domains/x"), outside, absent=("Created",))
        check("wizard homes: edit in /domains refused", b.command("edit /domains/town/x"), outside, absent=("editor",))
        check("wizard homes: copy into /domains refused", b.command("copy ~/items/gem /domains/town"), outside, absent=("Copied",))
        check("wizard homes: move out of /domains refused", b.command("move /domains/town/lantern ~/items"), outside, absent=("Moved",))
        check("wizard homes: rename in /domains refused", b.command("rename /domains/town/lantern lamp"), outside, absent=("Renamed",))
        check("wizard homes: delete in /domains refused", b.command("delete /domains/town/lantern"), outside, absent=("(y/n)",))
        check("wizard homes: rmdir in /domains refused", b.command("rmdir /domains/lab/sub"), outside, absent=("Removed",))
        check("wizard homes: /std still needs level 10", b.command("edit /std/room"), "You need wizard level 10 to change /secure and /std.")
        check("wizard homes: clone outside the home still needs level 3", b.command("clone /domains/town/lantern"), "You need wizard level 3 to clone anything outside your own home directory", absent=("Cloned",))
        check("wizard homes: update outside the home still needs level 5", b.command("update /domains/town/square"), "You need wizard level 5 to update anything outside your own home directory", absent=("Recompiled",))
        check("wizard homes: goto outside the home keeps its level 1 rule", b.command("goto /domains/town/square"), "Town Square")

        # -- /wiz itself and the homes themselves: level 10 --
        wiz10 = "You need wizard level 10 to change /wiz itself, a home directory (/wiz/<name>) or a file directly in /wiz. Everything under your own home, /wiz/bob, is yours."
        check("wizard homes: nobody below 10 can rmdir their own home", b.command("rmdir /wiz/bob") + b.command("rmdir ~"), wiz10, absent=("Removed",))
        check("wizard homes: nor rmdir /wiz", b.command("rmdir /wiz"), wiz10, absent=("Removed",))
        check("wizard homes: nor rename or move the home", b.command("rename /wiz/bob.cs bobby2") + b.command("rename ~ x") + b.command("move ~ /domains") + b.command("move /wiz/bob.d /domains"), wiz10, absent=("Renamed", "Moved"))
        check("wizard homes: nor move a file out to /wiz, or into it", b.command("move ~/items/gem /wiz") + b.command("copy ~/items/gem /wiz/gem") + b.command("move ~/items/gem /wiz/gem2"), wiz10, absent=("Moved", "Copied"))
        check("wizard homes: a file directly in /wiz needs level 10", b.command("edit /wiz/notes") + b.command("delete /wiz/README.md") + b.command("rename /wiz/README.md x") + b.command("mkdir /wiz/newhome"), wiz10, absent=("editor", "(y/n)", "Renamed", "Created"))
        check("wizard homes: a bobby home cannot be made by hand", b.command("mkdir /wiz/bobby"), wiz10, absent=("Created",))
        check("wizard homes: the home and README are still there", str([on_disk("wiz", "bob"), on_disk("wiz", "README.md"), on_disk("wiz", "bob", "items", "gem.cs")]), "[True, True, True]")

        # -- bob and bobby: whole segments, not prefixes --
        bobby = Client("bobby")
        bobby.read_until("> ")
        a.command("promote bobby 1")
        bobby.read_until("has been created")
        check("wizard homes: bobby gets /wiz/bobby", str(os.path.isdir(os.path.join(wiz_dir, "bobby"))), "True")
        check("wizard homes: bobby cannot change bob's home", bobby.command("mkdir /wiz/bob/x") + bobby.command("edit /wiz/bob/items/gem"), "/wiz/bob is another wizard's home.", absent=("Created", "editor"))
        check("wizard homes: bob cannot change bobby's home", b.command("mkdir /wiz/bobby/x") + b.command("edit /wiz/bobby/y") + b.command("rmdir /wiz/bobby/x"),
              "/wiz/bobby is another wizard's home.", absent=("Created", "editor", "Removed"))
        check("wizard homes: bobby cannot recreate bob's directory with the exception for a home", bobby.command("mkdir /wiz/bob"), wiz10.replace("/wiz/bob,", "/wiz/bobby,"), absent=("already exists",))
        check("wizard homes: bobby's own home works, with ~", bobby.command("mkdir ~/rooms") + bobby.command("list ~"), "Created /wiz/bobby/rooms/", "/wiz/bobby:", "rooms/")
        check("wizard homes: bobby may clone only their own files", bobby.command("clone /wiz/bob/items/gem"), "You need wizard level 3 to clone anything outside your own home directory, /wiz/bobby.")
        check("wizard homes: bob may change /wiz/bob/x but the prefix is no use to bobby", b.command("mkdir ~/x") + b.command("rmdir ~/x"), "Created /wiz/bob/x/", "Removed /wiz/bob/x/")

        # -- case tricks and path tricks --
        check("wizard homes: /WIZ/alice is still alice's home", b.command("mkdir /WIZ/alice/x") + b.command("edit /WIZ/alice/x") + b.command("delete /Wiz/Alice/note"), other, absent=("Created", "editor", "(y/n)"))
        check("wizard homes: /wiz/ALICE is still alice's home", b.command("mkdir /wiz/ALICE/x") + b.command("rmdir /wiz/ALICE/sub") + b.command("copy ~/items/gem /wiz/ALICE/sub"), other.replace("/wiz/alice", "/wiz/ALICE"), absent=("Created", "Removed", "Copied"))
        check("wizard homes: /WIZ/bobby is bobby's home", b.command("mkdir /WIZ/Bobby/x"), "/wiz/Bobby is another wizard's home.", absent=("Created",))
        check("wizard homes: /STD is still /std", b.command("edit /STD/room") + b.command("mkdir /Secure/x"), "You need wizard level 10 to change /secure and /std.", absent=("editor", "Created"))
        check("wizard homes: /WIZ itself and its homes in other case need level 10", b.command("rmdir /WIZ") + b.command("rmdir /wiz/BOB") + b.command("mkdir /wiz/Zed"), wiz10, absent=("Removed", "Created"))
        check("wizard homes: the owner's own home in other case is the owner's (the driver then says it does not exist)", b.command("mkdir /wiz/BOB/up"), "No such parent directory for /wiz/BOB/up", absent=("another wizard's home", "need wizard level"))
        check("wizard homes: ~ cannot be used to reach another wizard's home for writing", b.command("mkdir ~/../alice/x") + b.command("edit ~/../alice/x"), other, absent=("Created", "editor"))
        check("wizard homes: ~/.. is just a path, and /wiz is not writable", b.command("mkdir ~/../zed") + b.command("edit ~/../../domains/x"), wiz10, outside, absent=("Created", "editor"))
        check("wizard homes: ~name is not another wizard's home", b.command("cd ~alice"), "/~alice: no such directory.")
        check("wizard homes: ~name is only an odd relative name, outside the home", b.command("mkdir ~alice/x"), outside, absent=("Created",))
        check("wizard homes: ~ in the middle of a path is not special", b.command("mkdir /wiz/bob/~/x"), "No such parent directory", absent=("Created", "another wizard's"))
        check("wizard homes: nothing was created by any of that", str([on_disk("wiz", "alice", "x"), on_disk("wiz", "zed"), on_disk("wiz", "bobby", "x"), on_disk("wiz", "newhome")]), "[False, False, False, False]")

        # -- level 5 in another wizard's home --
        a.command("promote carol 5")
        carol.read_until("level is now 5")
        check("wizard homes: level 5 mkdir in another wizard's home", carol.command("mkdir /wiz/bob/c5"), "Created /wiz/bob/c5/")
        text = make_file(carol, "/wiz/bob/c5/f", ["carol was here"])
        check("wizard homes: level 5 edit in another wizard's home", text, "Wrote /wiz/bob/c5/f.cs, 1 lines")
        check("wizard homes: level 5 copy", carol.command("copy /wiz/bob/c5/f /wiz/bob/c5/g"), "Copied /wiz/bob/c5/f.cs to /wiz/bob/c5/g.cs.")
        check("wizard homes: level 5 move", carol.command("move /wiz/bob/c5/g /wiz/bob/items"), "Moved /wiz/bob/c5/g.cs to /wiz/bob/items/g.cs.")
        check("wizard homes: level 5 rename", carol.command("rename /wiz/bob/items/g h"), "Renamed /wiz/bob/items/g.cs to /wiz/bob/items/h.cs.")
        carol.command("delete /wiz/bob/items/h", marker="(y/n) ")
        check("wizard homes: level 5 delete", carol.command("y"), "Deleted /wiz/bob/items/h.cs.")
        carol.command("delete /wiz/bob/c5/f", marker="(y/n) ")
        carol.command("y")
        check("wizard homes: level 5 rmdir", carol.command("rmdir /wiz/bob/c5"), "Removed /wiz/bob/c5/")
        check("wizard homes: level 5 update and clone of bob's files", carol.command("update /wiz/bob/items/gem") + carol.command("clone /wiz/bob/items/gem"), "Recompiled /wiz/bob/items/gem", "Cloned /wiz/bob/items/gem#")
        check("wizard homes: level 5 still cannot touch a home itself or /wiz", carol.command("rmdir /wiz/bob") + carol.command("mkdir /wiz/zed") + carol.command("delete /wiz/README.md") + carol.command("rmdir ~"),
              "You need wizard level 10 to change /wiz itself, a home directory (/wiz/<name>) or a file directly in /wiz.", absent=("Removed", "Created", "(y/n)"))
        check("wizard homes: level 5 still cannot change /std", carol.command("mkdir /std/x"), "You need wizard level 10 to change /secure and /std.")
        check("wizard homes: level 10 may create and remove a home directory", a.command("mkdir /wiz/zed") + a.command("rmdir /wiz/zed"), "Created /wiz/zed/", "Removed /wiz/zed/")
        check("wizard homes: level 10 changes in any home", a.command("mkdir /wiz/bob/by_alice") + a.command("rmdir /wiz/bob/by_alice"), "Created /wiz/bob/by_alice/", "Removed /wiz/bob/by_alice/")

        # -- a level-1 wizard's code gets no privileges --
        wizlist_before = open(os.path.join(work, "save", "secure", "wizlist.json")).read()
        alice_before = open(os.path.join(work, "save", "players", "alice.json")).read()
        vault_file = os.path.join(work, "save", "secure", "vault.json")
        vault_before = open(vault_file).read() if os.path.exists(vault_file) else None
        make_file(b, "~/items/rogue", [
            "// @inherit /std/item",
            "public class Rogue : Item",
            "{",
            '    public override void Create() { Name = "rogue"; }',
            '    public override void Init() { AddAction("attempt", Attempt); }',
            "    private void Run(string label, Action act)",
            "    {",
            '        try { act(); Write(label + "=ALLOWED\\n"); }',
            '        catch (MudRuntimeException e) { Write(label + "=BLOCKED " + e.Message + "\\n"); }',
            "    }",
            "    private bool Attempt(string args)",
            "    {",
            '        var wiz = LoadObject("/secure/wizlist");',
            '        var vault = LoadObject("/secure/lost_property");',
            '        Run("copyfile", () => CopyFile("/domains/town/square.cs", "/wiz/bob/stolen.cs"));',
            '        Run("writefile", () => WriteFile("/wiz/bob/own.cs", "x"));',
            '        Run("readfile", () => ReadFile("/wiz/bob/items/gem.cs"));',
            '        Run("getdir", () => GetDir("/wiz/bob"));',
            '        Run("makedir", () => MakeDir("/wiz/bob/sneaky"));',
            '        Run("renamefile", () => RenameFile("/wiz/bob/items/gem.cs", "/wiz/bob/items/gem2.cs"));',
            '        Run("removefile", () => RemoveFile("/wiz/bob/items/gem.cs"));',
            '        Run("removedir", () => RemoveDir("/wiz/bob/rooms"));',
            '        Run("save-alice", () => SaveObject("/players/alice"));',
            '        Run("restore-alice", () => RestoreObject("/players/alice"));',
            '        Run("save-wizlist", () => SaveObject("/secure/wizlist"));',
            '        Run("restore-wizlist", () => RestoreObject("/secure/wizlist"));',
            '        Run("save-vault", () => SaveObject("/secure/lost_property"));',
            '        Run("secret", () => CheckAdminSecret(args));',
            '        Run("update", () => UpdateProgram("/std/room"));',
            '        Run("shutdown", () => Shutdown());',
            '        Run("destruct-player", () => Destruct(ThisPlayer()));',
            '        Write("promote=" + wiz.Call<string>("Promote", "bob", 10) + "\\n");',
            '        Write("bootstrap=" + wiz.Call<bool>("Bootstrap", args) + "\\n");',
            '        vault.Call("Record", "bob", "1", new List<string> { "/std/item" }, 5);',
            '        vault.Call("Clear", "alice", "1");',
            '        Write("claim=" + string.Join(",", vault.Call<List<string>>("Claim")) + "\\n");',
            "        return true;",
            "    }",
            "}"])
        text = b.command("clone ~/items/rogue")
        check("wizard homes: a level-1 wizard's own object compiles and clones", text, "Cloned /wiz/bob/items/rogue#")
        text = b.command("attempt " + SECRET)
        for label, reason in [("copyfile", "may not read-file /domains/town/square.cs"), ("writefile", "may not write-file /wiz/bob/own.cs"),
                              ("readfile", "may not read-file /wiz/bob/items/gem.cs"), ("getdir", "may not get-dir /wiz/bob"),
                              ("makedir", "may not make-dir /wiz/bob/sneaky"), ("renamefile", "may not write-file /wiz/bob/items/gem.cs"),
                              ("removefile", "may not remove-file /wiz/bob/items/gem.cs"), ("removedir", "may not remove-dir /wiz/bob/rooms"),
                              ("save-alice", "may not write /players/alice"), ("restore-alice", "may not read /players/alice"),
                              ("save-wizlist", "may not write /secure/wizlist"), ("restore-wizlist", "may not read /secure/wizlist"),
                              ("save-vault", "may not write /secure/lost_property"), ("secret", "may not check the admin secret"),
                              ("update", "may not update /std/room"), ("shutdown", "may not shut the driver down"), ("destruct-player", "may not destruct")]:
            check("wizard homes: code in a home cannot " + label, text, label + "=BLOCKED ", reason)
        check("wizard homes: code in a home cannot promote", text, "promote=Only a player can do that.")
        check("wizard homes: code in a home cannot use the bootstrap, even with the right secret", text, "bootstrap=False")
        check("wizard homes: code in a home cannot claim from the vault", text, "claim=0\r\n")
        check("wizard homes: nothing was allowed", text, absent=("=ALLOWED",))
        check("wizard homes: the registry file is unchanged", str(open(os.path.join(work, "save", "secure", "wizlist.json")).read() == wizlist_before), "True")
        check("wizard homes: alice's save file is unchanged", str(open(os.path.join(work, "save", "players", "alice.json")).read() == alice_before), "True")
        check("wizard homes: the vault was not written", str((open(vault_file).read() if os.path.exists(vault_file) else None) == vault_before), "True")
        check("wizard homes: bob is still level 1, the driver is still up, nothing was made by the object", a.command("wizards"), "bob (1)")
        check("wizard homes: no file was made or removed by the object", str([on_disk("wiz", "bob", "stolen.cs"), on_disk("wiz", "bob", "own.cs"), on_disk("wiz", "bob", "sneaky"), on_disk("wiz", "bob", "items", "gem.cs"), on_disk("wiz", "bob", "rooms")]), "[False, False, False, True, True]")

        # -- demotion leaves the files; re-promotion gives access back --
        check("wizard homes: demote to 0", a.command("promote bob 0"), "bob is now wizard level 0")
        b.read_until("level is now 0")
        check("wizard homes: a demoted wizard has no wizard commands", b.command("cd ~") + b.command("mkdir ~/x") + b.command("cat ~/rooms/hall") + b.command("goto ~/rooms/hall") + b.command("update ~/rooms/hall"), "What?", absent=("Created", "public class", "Hall"))
        check("wizard homes: demotion left the directory and its files", str([on_disk("wiz", "bob", "rooms", "hall.cs"), on_disk("wiz", "bob", "items", "gem.cs"), on_disk("wiz", "bob", "items", "rogue.cs")]), "[True, True, True]")
        check("wizard homes: and others can still read them", a.command("cat /wiz/bob/rooms/hall"), "public class Hall")
        check("wizard homes: a demoted player's objects stay compiled but they cannot edit them", b.command("edit /wiz/bob/rooms/hall"), "What?", absent=("editor",))
        check("wizard homes: re-promote", a.command("promote bob 1"), "bob is now wizard level 1")
        check("wizard homes: re-promotion gives the home back, without a second notice", b.read_until("level is now 1"), "Your wizard level is now 1.", absent=("has been created",))
        check("wizard homes: the old files are there again", b.command("cat ~/rooms/hall"), "/wiz/bob/rooms/hall.cs (11 lines)")
        check("wizard homes: and editable", b.command("mkdir ~/again"), "Created /wiz/bob/again/")
        b.command("rmdir ~/again")

        # -- tidy up for the sections below: everyone back to level 0, standing where they were --
        b.command("goto /domains/town/square")
        for name, who in (("bob", b), ("bobby", bobby), ("carol", carol)):
            a.command("promote " + name + " 0")
        bobby.send("quit")
        bobby.read_until("Goodbye")
        carol.send("quit")
        carol.read_until("Goodbye")
        a.command("cd /domains/town")
        b.command("drop rogue")
        b.command("look")

        # ---- live update of a room, with players standing in it ----
        edit(mudlib, "/domains/town/square", "a dry fountain", "a fountain that now runs with wine")
        text = a.command("update square")
        check("update accepts a path relative to the current directory", text, "Recompiled /domains/town/square", "Replaced /domains/town/square")
        check("new code is live", a.command("look"), "runs with wine", "Bob the newcomer is here.")
        check("bystander survived the update", b.command("look"), "runs with wine", "Alice")

        # ---- live update of a base class cascades to dependents ----
        edit(mudlib, "/std/room", '"Obvious exits: ', '"You can go: ')
        text = a.command("update /std/room")
        check("base update invalidates dependents", text, "/std/room", "/domains/town/square", "/domains/town/tavern", "/std/void")
        check("loaded objects keep old code", a.command("look"), "Obvious exits:")
        a.command("update /domains/town/square")
        check("dependents pick up the new base on update", a.command("look"), "You can go: north")

        # ---- compile error leaves the running object alone ----
        edit(mudlib, "/domains/town/square", 'ShortDesc = "Town Square";', 'ShortDesc = "Town Square" oops;')
        text = a.command("update /domains/town/square")
        check("compile error is reported with file and line", text, "Failed to compile", "square.cs(7,")
        check("world intact after compile error", a.command("look"), "runs with wine")
        edit(mudlib, "/domains/town/square", '" oops;', '";')
        check("fixed file updates again", a.command("update /domains/town/square"), "Replaced")

        # ---- sandbox ----
        write(mudlib, "/domains/town/evil", """
public class Evil : MudObject
{
    public override void Create()
    {
        var secrets = System.IO.File.ReadAllText("/etc/passwd");
        var t = GetType();
    }
}
""")
        text = a.command("clone /domains/town/evil")
        check("sandbox blocks file access and reflection", text, "sandbox", "System.IO.File", "GetType", absent=("Cloned",))

        # ---- runaway code ----
        write(mudlib, "/domains/town/spinner", """
// @inherit /std/item
public class Spinner : Item
{
    public override void Create() { Name = "spinner"; }
    public override void Init() { AddAction("spin", Spin); AddAction("dive", Dive); }
    private bool Spin(string args) { while (true) { } }
    private int Depth(int n) => Depth(n + 1) + 1;
    private bool Dive(string args) { Write(Depth(0).ToString()); return true; }
}
""")
        check("clone new source at runtime", a.command("clone /domains/town/spinner"), "Cloned /domains/town/spinner#")
        check("infinite loop is stopped", a.command("spin"), "Evaluation cost limit exceeded")
        check("infinite recursion is stopped", a.command("dive"), "Recursion too deep")
        check("driver still responsive", a.command("say still here"), "You say: still here")

        # ---- unloading of old program versions ----
        text = a.command("mudstat")
        check("mudstat", text, "Programs:", "old versions unloaded")
        print("      " + [l for l in text.splitlines() if "Programs" in l][0].strip())

        # ---- passwords ----
        def object_count():
            return [l for l in a.command("mudstat").splitlines() if l.startswith("Objects:")][0]

        def save_file(name):
            with open(os.path.join(work, "save", "players", name + ".json")) as f:
                return f.read()

        pw_g, pw_g2 = "violet-hour-42", "amber-tide-77"
        typed_passwords = [PASSWORD, pw_g, pw_g2, "different-pass", "wrong-pass-1", "wrong-pass-2", "wrong-pass-3",
                           "old-timer-pass", "hijacked-password", "not-my-password"]
        objects_before = object_count()

        g = Client("gwendolyn", login=False)
        raw = g.read_raw_until(IAC_WILL_ECHO)
        check("new name: account creation starts", raw.decode(errors="replace"), "Creating a new account for Gwendolyn", "Choose a password")
        check("IAC WILL ECHO follows the password prompt", raw, b"characters): " + IAC_WILL_ECHO)
        g.send("short")
        raw = g.read_raw_until(b"characters): " + IAC_WILL_ECHO)
        check("IAC WONT ECHO and a newline when the hidden line arrives", raw[:5], IAC_WONT_ECHO + b"\r\n")
        check("too-short password refused", raw.decode(errors="replace"), "at least 8 characters", "Choose a password")
        g.send("GWENDOLYN")
        check("password equal to the name refused", g.read_until("characters): "), "cannot be your name")
        g.send(pw_g)
        raw = g.read_raw_until(b"again: " + IAC_WILL_ECHO)
        check("confirmation prompt is between WONT ECHO and WILL ECHO", raw, IAC_WONT_ECHO + b"\r\nType it again: " + IAC_WILL_ECHO)
        g.send("different-pass")
        check("mismatched confirmation refused", g.read_until("characters): "), "do not match", "Choose a password")
        g.send(pw_g)
        g.read_until("again: ")
        g.send(pw_g)
        # Changed with the class system: after the password a new character is asked for a calling before entering the game.
        check("matching passwords create the account", g.read_until("Your calling: "), "Account created.", "choose your calling")
        g.send("adventurer")
        check("the chosen calling lets the new account into the game", g.read_until("> "), "You take up the calling of the Adventurer.", "Welcome, Gwendolyn.")
        saved = save_file("gwendolyn")
        check("new account is saved at once, with a hash", saved, '"passwordHash": "pbkdf2-sha256$210000$', absent=(pw_g,))
        g.send("quit")
        g.read_until("Goodbye")

        g = Client("gwendolyn", password=pw_g)
        check("relogin with the right password", g.read_until("> "), "Welcome back, Gwendolyn. This is visit number 2.")
        check("the password command asks for the current password", g.command("password", marker="Current password: "), "Current password: ")
        check("password command: wrong current password refused", g.command("not-my-password"), "Wrong password. Your password is unchanged.")
        g.command("password", marker="Current password: ")
        g.command(pw_g, marker="characters): ")
        check("password command: too-short new password refused", g.command("tiny"), "at least 8 characters", "unchanged")
        g.command("password", marker="Current password: ")
        g.command(pw_g, marker="characters): ")
        g.command(pw_g2, marker="again: ")
        check("password command: change succeeds", g.command(pw_g2), "Password changed.")
        check("help lists the password command", g.command("help"), "password")
        g.send("quit")
        g.read_until("Goodbye")
        check("changed password is saved", save_file("gwendolyn"), "pbkdf2-sha256$", absent=(pw_g, pw_g2))

        c = Client("gwendolyn", login=False)
        c.read_until("Password: ")
        c.send(pw_g)
        check("old password no longer works", c.read_until("Password: "), "Wrong password.")
        c.send("wrong-pass-2")
        c.read_until("Password: ")
        c.send("wrong-pass-3")
        check("third wrong password: goodbye", c.read_until("Goodbye"), "Wrong password. Goodbye.")
        check("third wrong password: disconnected", str(c.closed()), "True")
        g = Client("gwendolyn", password=pw_g2)
        check("relogin with the new password", g.read_until("> "), "Welcome back, Gwendolyn. This is visit number 3.")
        g.send("quit")
        g.read_until("Goodbye")

        c = Client("gwendolyn", login=False)
        c.read_until("Password: ")
        c.sock.close()
        c = Client("ivan", login=False)
        c.read_until("characters): ")
        c.sock.close()
        time.sleep(1)
        check("failed and abandoned logins leave no objects behind", object_count(), objects_before)
        check("abandoned new account is not saved", str(os.path.exists(os.path.join(work, "save", "players", "ivan.json"))), "False")

        with open(os.path.join(work, "save", "players", "oldtimer.json"), "w") as f:
            json.dump({"cwd": "/", "lastLogin": 0, "logins": 4, "title": "the old-timer"}, f)
        c = Client("oldtimer", login=False)
        check("account without a password must set one", c.read_until("characters): "), "Oldtimer, your account has no password", "must set one")
        c.send("old-timer-pass")
        c.read_until("again: ")
        c.send("old-timer-pass")
        # Changed with the class system: a character saved before classes is asked for a calling at the next login.
        check("password set, then asked for a calling", c.read_until("Your calling: "), "Password set.", "choose your calling")
        c.send("adventurer")
        check("calling chosen, then into the game", c.read_until("> "), "Welcome back, Oldtimer. This is visit number 5.")
        check("old save file kept its fields", c.command("who"), "Oldtimer the old-timer")
        check("old account now has a hash", save_file("oldtimer"), '"passwordHash": "pbkdf2-sha256$', '"title": "the old-timer"', absent=("old-timer-pass",))
        c.send("quit")
        c.read_until("Goodbye")
        c = Client("oldtimer", password="old-timer-pass")
        check("the password asked for is then required", c.read_until("> "), "Welcome back, Oldtimer")
        c.send("quit")
        c.read_until("Goodbye")
        b.command("look")  # Bob stands in the square: read past the arrivals and departures above

        write(mudlib, "/domains/town/pwimposter", """
// @inherit /domains/town/imposter
public class PwImposter : Imposter
{
    public override void Create() { base.Create(); Name = "thief"; }
    public override void Init() { base.Init(); AddAction("hijack", Hijack); }
    private bool Hijack(string args)
    {
        var me = ThisPlayer()!;
        var fresh = CloneObject("/std/player");
        Write("hijack: [" + me.Call<string>("BeginLogin", "Alice") + "] [" + fresh.Call<string>("BeginLogin", "Gwendolyn") + "] "
            + me.Call<bool>("CheckPassword", "x") + " " + fresh.Call<bool>("CheckPassword", "x") + " "
            + me.Call<bool>("SetPassword", "hijacked-password") + " " + fresh.Call<bool>("SetPassword", "hijacked-password") + "\\n");
        fresh.Call("SetupPlayer", "Gwendolyn");
        Write("fresh body: " + fresh.Call<string>("QueryName") + " " + Interactive(fresh) + "\\n");
        return true;
    }
}
""")
        check("clone an object that tries the login methods", a.command("clone /domains/town/pwimposter"), "Cloned /domains/town/pwimposter#")
        check("other objects cannot call the player's login methods", a.command("hijack"), "hijack: [] [] False False False False", "fresh body: thing False")
        check("the account it went for is untouched", save_file("gwendolyn"), "pbkdf2-sha256$", absent=("hijacked",))

        # ---- persistence ----
        a.command("title the tester")
        a.send("quit")
        check("quit", a.read_until("Goodbye"), "Goodbye.")
        check("quit is announced", b.read_until("leaves the game"), "Alice leaves the game.")
        # Changed from "carried items dropped on quit": items now stay with the character (saved by path).
        check("carried items do not drop on quit", b.command("look"), "Town Square", absent=("Spinner", "Alice"))
        a = Client("alice")
        check("save file restored", a.read_until("> "), "Welcome back, Alice. This is visit number 2.")
        check("carried items return at login", a.command("i"), "spinner")
        check("saved field and wizard level restored", a.command("who"), "Alice the tester [wizard 10]", "Bob the newcomer", absent=("Bob the newcomer [",))
        check("current directory restored", a.command("pwd"), "/domains/town")
        registry = open(os.path.join(work, "save", "secure", "wizlist.json")).read()
        check("registry saved to disk", registry, '"alice": 10', absent=("bob",))

        # ---- shutdown ----
        a.send("shutdown")
        driver.wait(timeout=10)
        check("clean shutdown", str(driver.returncode), "0")
        log.flush()
        logged = open(os.path.join(work, "driver.log")).read()
        check("driver log holds no password and no hash", logged, "shutdown requested", absent=tuple(typed_passwords) + ("pbkdf2",))
    finally:
        if driver.poll() is None:
            driver.kill()
        log.close()
        if failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-4000:])
        shutil.rmtree(work, ignore_errors=True)

    print(f"\n{len(failures)} failure(s)")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
