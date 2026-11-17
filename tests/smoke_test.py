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


class Client:
    def __init__(self, name, password=PASSWORD, login=True):
        """Connect and give the name. With login, also answer the password
        prompts: once for an existing account, twice for a new one or one
        that has no password yet. Text read past the name that is not a
        password prompt (a refusal) is kept for the next read_until."""
        self.pending = ""
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
        elif "Password: " in text:
            self.send(password)
        else:
            self.pending = text

    def read_any(self, markers, timeout=30.0):
        """Read until any of `markers` appears. Returns everything read."""
        deadline = time.time() + timeout
        data = ""
        while time.time() < deadline and not any(m in data for m in markers):
            try:
                chunk = self.sock.recv(65536)
                if not chunk:
                    break
                data += chunk.decode(errors="replace")
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

    def read_until(self, marker, timeout=30.0):
        """Read until `marker` appears. Returns everything read."""
        deadline = time.time() + timeout
        data, self.pending = self.pending, ""
        if marker in data:
            return data
        while time.time() < deadline:
            try:
                chunk = self.sock.recv(65536)
                if not chunk:
                    break
                data += chunk.decode(errors="replace")
                if marker in data:
                    return data
            except socket.timeout:
                pass
        return data

    def ed(self, line, timeout=30.0):
        """Send a line to the in-game editor and read until its ':' prompt."""
        self.send(line)
        deadline = time.time() + timeout
        data = ""
        while time.time() < deadline and not data.endswith(":"):
            try:
                data += self.sock.recv(65536).decode(errors="replace")
            except socket.timeout:
                pass
        return data

    def command(self, line, marker="> ", timeout=30.0):
        self.send(line)
        return self.read_until(marker, timeout)


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


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-test-")
    mudlib = os.path.join(work, "mudlib")
    shutil.copytree(os.path.join(ROOT, "mudlib"), mudlib, ignore=shutil.ignore_patterns("bin", "obj", "*.csproj"))
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
        check("secret creates the first level-10 wizard", a.command("promote " + SECRET), "You are now a level 10 wizard")
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
        check("cd with no argument returns to the root", a.command("cd"), "/\r\n")
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
        check("level 4 cannot delete or rmdir", b.command("delete /domains/lab/sub/gizmo") + b.command("rmdir /domains/lab/ghost"), "What?", absent=("(y/n)", "not empty"))
        a.command("promote bob 5")
        b.read_until("level is now")
        check("other objects cannot delete files", b.command("erase"), "may not remove-file /domains/town/square.cs")
        check("other objects cannot remove directories", b.command("prune"), "may not remove-dir /domains/lab/ghost")
        check("their targets are still on disk", str([os.path.exists(os.path.join(mudlib, "domains", "town", "square.cs")), os.path.isdir(os.path.join(lab, "ghost"))]), "[True, True]")
        a.command("promote bob 1")
        b.read_until("level is now")
        check("level 1 can read files", b.command("cat /domains/lab/notes.txt"), "remember the milk")
        check("level 1 cannot edit or mkdir", b.command("edit /domains/lab/notes.txt") + b.command("mkdir /domains/lab/x"), "What?", absent=("editor", "Created"))
        with open(os.path.join(mudlib, "domains", "lab", "widget.cs")) as f:
            check("file on disk matches", f.read(), "// @inherit /std/item\n", "edited live", absent=("\r",))
        a.command("cd /domains/town")

        check("demote to 0", a.command("promote bob 0"), "bob is now wizard level 0")
        b.read_until("level is now")
        check("demoted player loses commands at once", b.command("mudstat"), "What?", absent=("Objects",))
        b.command("drop imposter")

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
        check("matching passwords create the account", g.read_until("> "), "Account created.", "Welcome, Gwendolyn.")
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
        check("password set, then into the game", c.read_until("> "), "Password set.", "Welcome back, Oldtimer. This is visit number 5.")
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
        check("carried items dropped on quit", b.command("look"), "Spinner is here.", absent=("Alice",))
        a = Client("alice")
        check("save file restored", a.read_until("> "), "Welcome back, Alice. This is visit number 2.")
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
