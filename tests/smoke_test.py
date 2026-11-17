#!/usr/bin/env python3
"""End-to-end smoke test for the LesMudOS proof of concept.

Starts the driver against a temporary copy of the mudlib, logs two players in
over TCP, and checks: movement, object-supplied verbs, call_out, heartbeat,
persistence, wizard levels and promotion, live update of a room and of a base
class, compile-error handling, the sandbox, the evaluation-cost limit and the
recursion guard.

Usage (from the repository root, after `dotnet build`):
    python3 tests/smoke_test.py
"""
import os
import shutil
import socket
import subprocess
import sys
import tempfile
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PORT = 4777
SECRET = "correct horse battery staple"
failures = []


class Client:
    def __init__(self, name):
        self.sock = socket.create_connection(("127.0.0.1", PORT), timeout=5)
        self.sock.settimeout(0.2)
        self.read_until("known?")
        self.send(name)

    def send(self, line):
        self.sock.sendall((line + "\r\n").encode())

    def read_until(self, marker, timeout=30.0):
        """Read until `marker` appears. Returns everything read."""
        deadline = time.time() + timeout
        data = ""
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
    public override void Init() { AddAction("usurp", Usurp); AddAction("forge", Forge); AddAction("guess", Guess); AddAction("peek", Peek); }
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
        check("other objects cannot list directories", b.command("peek"), "may not list /secure")
        a.command("cd /domains/town")
        check("cd with no argument returns to the root", a.command("cd"), "/\r\n")
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
