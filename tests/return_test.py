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


BRUTE = '''// @inherit /std/monster
public class RtBrute : Monster
{
    public override void Create()
    {
        Name = "brute"; Ids.Add("brute");
        ShortDesc = "a goblin brute"; LongDesc = "A brute made for the return test.";
        Level = 1; MaxVitality = 100000; DamageMin = 60; DamageMax = 60; ToHit = 100; BaseReaction = 1.0;
        Aggressive = true;
    }
}
'''

LINGER = 6   # seconds a dropped body stays in this test (LESMUDOS_LINGER_SECONDS); the game's own is 60


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
    h.write(mudlib, TEST + "brute", BRUTE)
    h.write(mudlib, TEST + "bruteden", a.room("RtBruteDen", "Return Brute Den", {"out": TEST + "a"}, f'        AddSpawn("{TEST}brute");\n', reset=3000))
    h.edit(mudlib, TEST + "a", 'Exits["wolf"]', 'Exits["brute"] = "/domains/rt/bruteden";\n        Exits["wolf"]')
    h.edit(mudlib, HOLLOW + "village/square", 'Exits["south"]', f'Exits["down"] = "{TEST}a";\n        Exits["south"]')
    os.environ["LESMUDOS_IDLE_MINUTES"] = "4s"
    os.environ["LESMUDOS_TIMEOUT_MINUTES"] = "14s"
    os.environ["LESMUDOS_LINGER_SECONDS"] = str(LINGER)
    proc, log = a.start_driver(work, mudlib)
    try:
        run(work)
        c = h.Client("wizz")   # the wizard of the link-dead tests (the secret makes only one)
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
        check("a drop that leaves a body says so in the log line", "\n".join(l for l in ended if "player Dave" in l), f"body lingers {LINGER} s")
        check("a quit does not", "\n".join(l for l in ended if "player Rita" in l), absent=("lingers",))
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
    if os.environ.get("RT_ONLY") == "linger":   # a quick run of the link-dead tests only
        linger_tests(work)
        return
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
    time.sleep(LINGER + 3)
    check("a dropped connection in room A: the next login is room A", where(h.Client("dave")), "Return Room A")

    walt = login("walt")
    walt.command("down")
    walt.send("wolf")
    time.sleep(0.5)
    walt.sock.close()  # dropped in the wolf den, wolf awake or not
    time.sleep(LINGER + 3)
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
    time.sleep(LINGER + 3)
    check("after the idle timeout the next login is room A", where(h.Client("ida")), "Return Room A")

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
    linger_tests(work)


def drop(c):
    c.sock.close()


def read_for(c, seconds):
    return a.quiet(c, seconds)


def linger_tests(work):
    """A dropped connection leaves the body for LINGER seconds (the game: one minute): vulnerable, fighting back, killable, and the same body when the player returns."""
    obs = login("olga")
    obs.command("down")
    # ---- out of a fight: the body stays, is marked, is deaf, goes after the window ----
    vera = login("vera")
    vera.command("down")
    a.drain(obs, 0.5)
    drop(vera)
    text = read_for(obs, 1.5)
    check("link-dead: the room is told the link dropped", text, "Vera's eyes go blank.")
    check("link-dead: the room line says so", obs.command("look"), "Vera stands here, staring at nothing.")
    check("link-dead: look at them says so", obs.command("look at vera"), "stares at nothing")
    check("link-dead: who marks them", obs.command("who"), "Vera", "(link-dead)")
    check("link-dead: a tell is not heard", obs.command("tell vera hello"), "Vera cannot hear you")
    check("link-dead: a whisper is not heard", obs.command("whisper hello to vera"), "Vera cannot hear you")
    check("link-dead: nothing can be given to them", obs.command("give sword to vera"), "stares at nothing")
    time.sleep(LINGER + 1)
    text = read_for(obs, 0.5)
    check("link-dead: the body goes after the window, and the room is told", text, "Vera fades from sight.")
    check("link-dead: and is gone from the room and from who", obs.command("look") + obs.command("who"), "Obvious exits", absent=("Vera", "link-dead"))
    back = h.Client("vera")
    check("link-dead: after the window a login is an ordinary one", back.read_until("> "), "Welcome back, Vera", absent=("shake off the daze",))
    check("link-dead: back in the room she left", back.command("look"), "Return Room A")
    back.command("quit")
    back.closed(3)
    # ---- quit is not an escape from a fight ----
    hal = login("halle")
    hal.command("down")
    hal.command("wolf")
    a.quiet(hal, 3.0)
    check("quit: refused in a fight", hal.command("quit"), "You cannot leave in the middle of a fight. Flee first.")
    check("quit: and the character is still here", hal.command("look"), "Return Wolf Den")
    for _ in range(12):
        out = hal.command("flee")
        if "You flee" in out:
            break
    hal.command("quit")
    hal.closed(3)
    # ---- in a fight: they keep hitting the body, it hits back with ordinary attacks, and goes after the window ----
    obs.command("wolf")
    dan = login("danny")
    dan.command("down")
    dan.command("wolf")
    a.quiet(dan, 1.0)
    dan.send("kill wolf")
    a.quiet(dan, 1.5)
    a.drain(obs, 0.5)
    drop(dan)
    text = read_for(obs, LINGER - 1)
    check("fight: the creature keeps attacking the body", text, "Danny")
    import re
    check("fight: the body fights back with ordinary attacks", "ok" if re.search(r"(?i)Danny (hits|misses|scratches|shoots|smashes|strikes|slashes|cuts)\b.*grey wolf|Danny \w+ a grey wolf", text) else text[-300:], "ok")
    check("fight: and uses no skill, potion or flight", text, absent=("careful aim", "flees", "drinks"))
    time.sleep(3)
    text += read_for(obs, 0.5)
    check("fight: the body goes after the window", text, "Danny fades from sight.")
    check("fight: and the wolf is left to the others", obs.command("look"), absent=("Danny",))
    obs.command("flee")
    a.quiet(obs, 1.0)
    # ---- reconnect inside the window: the same body, fight and all ----
    obs2 = obs
    obs2.command("look")
    eve = login("evelyn")
    eve.command("down")
    eve.command("wolf")
    eve.send("kill wolf")
    a.quiet(eve, 1.5)
    obs2.command("wolf") if "Wolf Den" not in obs2.command("look") else None
    drop(eve)
    time.sleep(2.0)
    again = h.Client("evelyn")
    text = again.read_until("> ")
    check("reconnect: the player is told", text, "You shake off the daze and find yourself where you stood.", "Return Wolf Den")
    check("reconnect: not a new arrival (no second arrival attack, no welcome)", text, absent=("Welcome", "enters the game"))
    check("reconnect: one body only", obs2.command("who"), "Evelyn", absent=("link-dead",))
    text = a.quiet(again, 8.0)
    check("reconnect: the fight goes on", text, "grey wolf")
    check("reconnect: and the character is whole", again.command("score"), "Evelyn")
    again.command("flee")
    again.command("quit")
    again.closed(3)
    # ---- killed while link-dead: an ordinary death, the next login wakes in the Guest Rooms ----
    gil = login("gilbert")
    gil.command("down")
    gil.command("brute")
    drop(gil)
    time.sleep(LINGER + 4)
    back = h.Client("gilbert")
    text = back.read_until("> ")
    check("death: the next login wakes in the Guest Rooms", text + back.command("look"), "Guest")
    check("death: with the penalties of a death (the corpse holds the belongings)", back.command("inventory"), absent=("Return",))
    back.command("quit")
    back.closed(3)
    # ---- a wizard's body goes at once ----
    wz = login("wizz")
    wz.command("promote " + h.SECRET)
    wz.command("down")
    a.drain(obs2, 0.5)
    obs2.command("out")
    obs2.command("up")
    obs2.command("down")
    drop(wz)
    text = read_for(obs2, 1.5)
    check("wizard: no lingering body, no blank eyes", text, absent=("eyes go blank", "staring at nothing"))
    check("wizard: gone at once from who", obs2.command("who"), absent=("Wizz",))
    obs2.command("quit")


if __name__ == "__main__":
    sys.exit(main())
