#!/usr/bin/env python3
"""Where a login begins (docs/specs/play-feel.md, "Where a login begins"): a character who leaves the game by quit, by a dropped
connection or by the idle timeout wakes in the room they left, through the ordinary arrival (an aggressive creature there engages);
a new character, a save with no room, a room that does not load, the void and a room under /wiz all fall back to the Village Square
without a message.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4851 python3 tests/return_test.py
"""
import glob
import json
import os
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4851")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402

TEST = "/domains/rt/"
HOLLOW = "/domains/hollow/"
check = h.check

WOLF = '''// @inherit /std/monster
public class RtWolf : Monster
{
    public override void Create()
    {
        Name = "wolf"; Ids.Add("wolf");
        ShortDesc = "a grey wolf"; LongDesc = "A wolf made for the return test.";
        Level = 1; MaxVitality = 100000; DamageMin = 1; DamageMax = 1; ToHit = 30; BaseReaction = 3.0;
        Aggressive = true;
    }
}
'''


def login(name):
    c = h.Client(name)
    c.read_until("> ")
    return c


def where(c):
    """The title of the room the character is in (the first line of look), once the login text is read."""
    c.read_until("> ")
    return c.command("look").lstrip("\r\n").split("\n")[0].strip()


def saved_file(work, name):
    for f in glob.glob(os.path.join(work, "save", "**", "*.json"), recursive=True):
        if os.path.basename(f).lower() == name + ".json":
            return f
    return None


def set_room(work, name, value):
    f = saved_file(work, name)
    if f is None:
        return False
    data = json.load(open(f))
    holder = data
    # the saved fields may sit at the top or in a nested object: set every logoutRoom key found
    def walk(o):
        n = 0
        if isinstance(o, dict):
            for k in list(o):
                if k == "logoutRoom":
                    o[k] = value
                    n += 1
                else:
                    n += walk(o[k])
        elif isinstance(o, list):
            for x in o:
                n += walk(x)
        return n
    if walk(holder) == 0:
        return False
    json.dump(data, open(f, "w"))
    return True


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-return-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    os.makedirs(os.path.join(mudlib, "domains", "rt"))
    h.write(mudlib, TEST + "a", a.room("RtA", "Return Room A", {"up": HOLLOW + "village/square", "wolf": TEST + "den"}, reset=3000))
    h.write(mudlib, TEST + "den", a.room("RtDen", "Return Wolf Den", {"out": TEST + "a"}, f'        AddSpawn("{TEST}wolf");\n', reset=3000))
    h.write(mudlib, TEST + "wolf", WOLF)
    h.edit(mudlib, HOLLOW + "village/square", 'Exits["south"]', f'Exits["down"] = "{TEST}a";\n        Exits["south"]')
    os.makedirs(os.path.join(mudlib, "wiz", "wren"), exist_ok=True)
    os.makedirs(os.path.join(mudlib, "wiz", "boss"), exist_ok=True)
    h.write(mudlib, "/wiz/wren/lab", a.room("WrenLab", "Wren's Lab", {"out": TEST + "a"}))
    h.write(mudlib, "/wiz/boss/hall", a.room("BossHall", "Boss's Hall", {"out": TEST + "a"}))
    os.environ["LESMUDOS_IDLE_MINUTES"] = "4s"
    os.environ["LESMUDOS_TIMEOUT_MINUTES"] = "14s"
    proc, log = a.start_driver(work, mudlib)
    try:
        run(work)
        c = h.Client("boss")  # the level 10 made in run()
        c.read_until("> ")
        c.command("shutdown")
        proc.wait(timeout=15)
        log.flush()
        logged = open(os.path.join(work, "driver.log")).read()
        ended = [l for l in logged.splitlines() if "connection ended:" in l]
        check("the driver logs one line for every ended connection", str(len(ended) >= 15), "True")
        check("a dropped socket is logged as the client closing, with the player's name", "\n".join(l for l in ended if "player Dave" in l), "client closed", "lasted", "since last input")
        check("a quit is logged as closed by the game", "\n".join(l for l in ended if "player Rita" in l), "closed by the game")
        check("the idle timeout is logged as closed by the game", "\n".join(l for l in ended if "player Ida" in l), "closed by the game")
        check("the log line carries the remote address", ended[0], "127.0.0.1:")
    finally:
        if proc.poll() is None:
            proc.kill()
        log.close()
        if h.failures:
            print(open(os.path.join(work, "driver.log")).read()[-3000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\n{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


def run(work):
    rita = login("rita")
    check("a new character starts in the Village Square", rita.command("look"), "Village Square")
    check("down to room A", rita.command("down"), "Return Room A")
    rita.command("quit")
    rita.closed(3)
    again = h.Client("rita")
    text = again.read_until("> ")
    check("quit in room A: the next login is room A", where(again), "Return Room A")
    check("and no error shown", text, absent=("rror", "***"))
    again.command("up")
    check("up from there is the square", again.command("look"), "Village Square")
    again.command("quit")
    again.closed(3)
    check("a character who quit in the square wakes in the square", where(h.Client("rita")), "Village Square")

    dave = login("dave")
    dave.command("down")
    dave.sock.close()  # a dropped connection
    time.sleep(2.0)
    check("a dropped connection in room A: the next login is room A", where(h.Client("dave")), "Return Room A")

    walt = login("walt")
    walt.command("down")
    walt.send("wolf")
    time.sleep(0.5)
    walt.sock.close()  # dropped in the wolf den, wolf awake or not
    time.sleep(2.0)
    back = h.Client("walt")
    text = back.read_until("> ")
    check("a dropped connection in the wolf den: the next login is the den", text, "Return Wolf Den")
    text += a.quiet(back, 10.0)
    check("and the wolf engages as for anyone walking in", text, "attacks you")
    back.command("quit")
    back.closed(3)

    ida = login("ida")
    ida.command("down")
    check("the idle timeout disconnects", str(ida.closed(30)), "True")
    check("after the idle timeout the next login is room A", where(h.Client("ida")), "Return Room A")

    # A wizard's own home is a workshop (docs/SANDBOX.md): it is entered only through the master's move, which takes the checkpoint
    # with the room the wizard came from. Quitting or dropping in it wakes them in that room, never in sandbox space.
    boss = login("boss")
    boss.command("promote " + h.SECRET)
    wren = login("wren")
    boss.command("promote wren 1")
    wren.command("down")
    check("a wizard enters their own home from room A", wren.command("goto /wiz/wren/lab"), "step into the workshop")
    wren.command("quit")
    wren.closed(3)
    saved = open(saved_file(work, "wren")).read()
    check("quit in the own home: the save names no room under /wiz", saved, absent=("/wiz/",))
    check("quit in the own home: the next login is room A, the room the workshop was entered from", where(h.Client("wren")), "Return Room A")
    wren = h.Client("wren")
    wren.read_until("> ")
    wren.command("goto /wiz/wren/lab")
    wren.sock.close()
    time.sleep(2.0)
    check("a dropped connection in the own home: the next login is room A", where(h.Client("wren")), "Return Room A")
    for value, label in (("/wiz/wren/lab", "the wizard's own home"), ("/wiz/boss/hall", "another wizard's home"), ("/WIZ/wren/lab", "the own home, spelled otherwise")):
        c = h.Client("wren")
        c.read_until("> ")
        c.command("quit")
        c.closed(3)
        time.sleep(0.5)
        check(f"{label} as the saved room: the save could be edited", str(set_room(work, "wren", value)), "True")
        c = h.Client("wren")
        text = c.read_until("> ")
        check(f"{label} as the saved room: login never places a wizard in sandbox space", where(c), "Village Square", absent=("rror",))
        check(f"{label} as the saved room: no workshop entered at login", text, absent=("step into the workshop",))
    boss.command("quit")

    for name, value, label in (("fay", TEST + "gone", "a saved room that does not load"), ("gus", "/std/void", "the void"),
                                ("hal", "/wiz/someone/room", "a room under /wiz"), ("ivy", "", "no saved room")):
        c = login(name)
        c.command("down")
        c.command("quit")
        c.closed(3)
        time.sleep(0.5)
        check(f"{label}: the save could be edited", str(set_room(work, name, value)), "True")
        c = h.Client(name)
        text = c.read_until("> ")
        check(f"{label}: the next login is the Village Square, with no error", where(c), "Village Square", absent=("rror",))
        check(f"{label}: and no error was shown", text, absent=("rror", "***"))


if __name__ == "__main__":
    sys.exit(main())
