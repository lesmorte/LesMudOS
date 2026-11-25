#!/usr/bin/env python3
"""Tests for the login throttle (docs/specs/login-throttle.md).

One driver, the bridge listener on, and short windows (LESMUDOS_THROTTLE_*), so that the 5-minute and 15-minute rules of production take seconds.
Every "address" here is a claimed web address (a PROXY line on the bridge port), so each scenario has its own address and the driver's real
loopback address is never throttled. Checks: a lock by name, a lock by address (and IPv6 counted by /64), the known-address exception and its own
3-try limit, the same refusal for every cause, the pause on every "no", a success from a clean address resetting the count and one from an address
that had failed not doing so, doubling of the lock, the creation limit, the wizard command, that nothing typed reaches the log, that the save file
holds no raw address, and that ordinary play (three typos, then the right password) is never locked.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=5104 python3 tests/throttle_test.py      (also uses the next port, for the bridge)
"""
import os
import shutil
import socket
import subprocess
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "5104")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import net_test as n  # noqa: E402

PORT = h.PORT
BRIDGE = n.BRIDGE
GOOD = h.PASSWORD
BAD = "not-the-password-1"
REFUSED = "Too many attempts. Try again later."
NEW_LIMIT = "Too many new characters have been made from your address."
DELAY = 1.0

ENV = {
    "LESMUDOS_BRIDGE_PORT": str(BRIDGE),
    "LESMUDOS_MAX_CONNECTIONS": "0", "LESMUDOS_MAX_PER_ADDRESS": "0", "LESMUDOS_CONNECTS_PER_MINUTE": "0",
    "LESMUDOS_THROTTLE_NAME_FAILURES": "5", "LESMUDOS_THROTTLE_WINDOW_SECONDS": "60",
    "LESMUDOS_THROTTLE_NAME_LOCK_SECONDS": "10", "LESMUDOS_THROTTLE_NAME_LOCK_MAX_SECONDS": "40", "LESMUDOS_THROTTLE_RESET_SECONDS": "300",
    "LESMUDOS_THROTTLE_ADDRESS_FAILURES": "12", "LESMUDOS_THROTTLE_ADDRESS_LOCK_SECONDS": "9", "LESMUDOS_THROTTLE_ADDRESS_LOCK_MAX_SECONDS": "36",
    "LESMUDOS_THROTTLE_KNOWN_TRIES": "3", "LESMUDOS_THROTTLE_FAIL_DELAY_MS": "1000",
    "LESMUDOS_THROTTLE_NEW_PER_HOUR": "3", "LESMUDOS_THROTTLE_NEW_PER_DAY": "5",
    "LESMUDOS_THROTTLE_NEW_HOUR_SECONDS": "40", "LESMUDOS_THROTTLE_NEW_DAY_SECONDS": "3600",
}

_next = [10]


def fresh(prefix="198.51.100"):
    """A claimed address nobody has used."""
    _next[0] += 1
    return f"{prefix}.{_next[0]}"


def wait_for(s, markers, timeout=8.0):
    end = time.time() + timeout
    data = ""
    while time.time() < end:
        try:
            chunk = s.recv(65536)
        except socket.timeout:
            chunk = None
        except OSError:
            break
        if chunk == b"":
            break
        if chunk:
            data += chunk.decode(errors="replace")
            if any(m in data for m in markers):
                break
    return data


def v6(addr):
    return ":" in addr


def open_as(addr):
    return n.connect(BRIDGE, n.proxy(addr, v6=v6(addr)))


def login(name, passwords, addr):
    """Connect as `addr`, give the name, try the passwords in order. Returns (outcome, text, seconds the last answer took).
    Outcomes: refused (the refusal, before or after the name), in (reached the game), goodbye (three wrong passwords), prompt (stopped at the
    password prompt: no passwords given), new (the name is new)."""
    s = open_as(addr)
    try:
        text = wait_for(s, ["known?", REFUSED])
        if REFUSED in text:
            return "refused", text, 0.0
        s.sendall(name.encode() + b"\r\n")
        t0 = time.time()
        text = wait_for(s, ["Password: ", "Creating a new account", REFUSED, "already here"])
        took = time.time() - t0
        if REFUSED in text:
            return "refused", text, took
        if "Creating a new account" in text:
            return "new", text, took
        if not passwords:
            return "prompt", text, took
        for pw in passwords:
            s.sendall(pw.encode() + b"\r\n")
            t0 = time.time()
            text = wait_for(s, ["Wrong password. Password: ", "Goodbye", "Welcome back", "Your calling: ", REFUSED])
            took = time.time() - t0
            if "Welcome back" in text:
                s.sendall(b"quit\r\n")
                wait_for(s, ["Goodbye", "closed"], 1.5)
                return "in", text, took
            if "Goodbye" in text:
                return "goodbye", text, took
            if REFUSED in text:
                return "refused", text, took
        return "continuing", text, took
    finally:
        s.close()


def create(name, addr):
    s = open_as(addr)
    try:
        wait_for(s, ["known?", REFUSED])
        s.sendall(name.encode() + b"\r\n")
        text = wait_for(s, ["Creating a new account", NEW_LIMIT, REFUSED, "Password: "])
        if "Creating a new account" not in text:
            return "refused", text
        text = wait_for(s, ["characters): "])
        s.sendall(GOOD.encode() + b"\r\n")
        text = wait_for(s, ["again: ", NEW_LIMIT])
        s.sendall(GOOD.encode() + b"\r\n")
        text += wait_for(s, ["Your calling: ", NEW_LIMIT])
        if NEW_LIMIT in text:
            return "refused", text
        s.sendall(b"adventurer\r\n")
        text = wait_for(s, ["Welcome, "])
        if "Welcome, " not in text:
            return "failed", text
        s.sendall(b"quit\r\n")
        wait_for(s, ["Goodbye", "closed"], 1.5)
        return "created", text
    finally:
        s.close()


def wrong(name, count, addr):
    """Count wrong passwords in as few connections as it takes (three to a connection). Returns the outcomes."""
    out = []
    while count > 0:
        k = min(3, count)
        out.append(login(name, [BAD] * k, addr)[0])
        if out[-1] == "refused":
            break
        count -= k
    return out


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-throttle-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    proc = None
    try:
        proc, log = n.start(work, mudlib, 1, ENV)
        h.check("the throttle settings in force are logged", n.logtext(log), "login throttle: LESMUDOS_THROTTLE_")

        # ---- accounts: each made from its own address, which the character then knows
        owners = {}
        for name in ("Vera", "Wendy", "Rita", "Nora", "Dora", "Pat", "Quin", "Ruth", "Sam", "Wizz", "Edna", "Fay", "Ivy", "Hal", "Ian", "Jo", "Kit", "Lee", "Mo", "Ned", "Oz"):
            owners[name] = fresh()
            result, text = create(name, owners[name])
            h.check(f"account {name} made", result, "created")

        # ---- ordinary play: three typos, then the right password
        r = login("Nora", [BAD, BAD, BAD], fresh())
        h.check("three typos end the connection as they always did", r[0], "goodbye")
        r = login("Nora", [GOOD], owners["Nora"])
        h.check("... and the right password gets in afterwards", r[0], "in")
        r = login("Nora", [BAD, GOOD], fresh())
        h.check("a typo and then the right password gets in from any address", r[0], "in")
        r = login("Nora", [], fresh())
        h.check("Nora is not locked afterwards", r[0], "prompt")

        # ---- a lock by name
        x1 = fresh()
        h.check("three wrong passwords, then two more: the fifth locks the name", str(wrong("Vera", 5, x1)), "['goodbye', 'goodbye']")
        r = login("Vera", [], x1)
        h.check("the same address is refused", r[0], "refused")
        h.check("... with the plain refusal", r[1], REFUSED)
        r2 = login("Vera", [], fresh())
        h.check("another address is refused", r2[0], "refused")
        h.check("the refusal is the same words", r2[1].replace("\r", "").strip().splitlines()[-1], REFUSED)
        h.check("a locked name is logged once", str(len(n.lines_with(log, "name locked: Vera"))), "1")
        h.check("the log line says the length", "\n".join(n.lines_with(log, "name locked: Vera")), "for 10 s")

        # the known address is let through, with its own three tries
        k = owners["Vera"]
        r = login("Vera", [], k)
        h.check("an address the character has logged in from reaches the password prompt of a locked name", r[0], "prompt")
        r = login("Vera", [BAD, BAD, BAD], k)
        h.check("... and has three tries of its own", r[0], "goodbye")
        r = login("Vera", [], k)
        h.check("... after which it is refused too", r[0], "refused")

        # a success from a clean address (the owner) lifts the lock: use a fresh lock on Rita
        x2 = fresh()
        wrong("Rita", 5, x2)
        h.check("Rita is locked", login("Rita", [], fresh())[0], "refused")
        r = login("Rita", [BAD, GOOD], owners["Rita"])
        h.check("the owner, from a known address, gets in with the right password while the name is locked", r[0], "in")
        r = login("Rita", [], fresh())
        h.check("that success, from an address that had not failed, cleared the record", r[0], "prompt")
        wrong("Rita", 2, x2)
        h.check("... and the count starts again at zero", login("Rita", [], fresh())[0], "prompt")

        # a success from an address that had failed does not reset
        w = owners["Wendy"]
        wrong("Wendy", 4, w)
        r = login("Wendy", [GOOD], w)
        h.check("the owner types four wrong passwords and then the right one", r[0], "in")
        login("Wendy", [BAD], w)
        h.check("one more wrong password from that address locks the name", login("Wendy", [], fresh())[0], "refused")

        # the first success is of no use to someone who succeeds on his own other character
        x3 = fresh()
        wrong("Pat", 4, x3)
        r = login("Quin", [GOOD], x3)
        h.check("the guesser logs into his own other character from the same address", r[0], "in")
        wrong("Pat", 1, x3)
        h.check("... which did not reset what he spent on Pat: the next wrong password locks", login("Pat", [], fresh())[0], "refused")

        # ---- the lock runs out, and doubles
        d = fresh()
        wrong("Dora", 5, d)
        h.check("Dora is locked", login("Dora", [], fresh())[0], "refused")
        time.sleep(11)
        h.check("after the lock time (10 s) the name can be tried again", login("Dora", [], fresh())[0], "prompt")
        wrong("Dora", 5, d)
        h.check("five more locks it again", login("Dora", [], fresh())[0], "refused")
        time.sleep(11)
        h.check("the second lock is longer: still locked after 11 s", login("Dora", [], fresh())[0], "refused")
        time.sleep(11)
        h.check("... and over after about 20 s", login("Dora", [], fresh())[0], "prompt")

        # ---- the pause on every "no", and the same words for every cause
        a = fresh()
        r_wrong = login("Ruth", [BAD], a)
        h.check("a wrong password is answered after about a second", str(DELAY * 0.8 <= r_wrong[2] <= DELAY * 2.5), "True")
        wrong("Ruth", 5, fresh())
        r_lock = login("Ruth", [], fresh())
        h.check("a refusal for a locked name takes about as long as a wrong password", str(abs(r_lock[2] - r_wrong[2]) < 0.6), "True")

        # ---- a lock by address, with IPv6 counted by /64
        subnet = "2001:db8:77::"
        names = ["Sam", "Pat", "Quin", "Nora"]
        out = []
        for i, nm in enumerate(names):
            out.append(login(nm, [BAD, BAD, BAD], f"{subnet}{i + 1}")[0])
        h.check("twelve wrong passwords from four addresses of one /64, over four names, lock the /64", str(out), "['goodbye', 'goodbye', 'goodbye', 'goodbye']")
        existing = login("Vera", [], f"{subnet}99")
        unknown = login("Nobodyatall", [], f"{subnet}99")
        h.check("any further address of that /64 is refused whatever the name", existing[0] + " " + unknown[0], "refused refused")
        h.check("the refusal is the same bytes for a name that exists and one that does not", str(existing[1] == unknown[1]), "True")
        h.check("... and the words are the plain ones", existing[1], REFUSED)
        h.check("an address of another /64 is not affected", login("Sam", [], "2001:db8:78::1")[0], "prompt")
        h.check("the address lock is logged", "\n".join(n.lines_with(log, "address locked:")), "address locked: 2001:0db8:0077:0000::/64 for 9 s")
        time.sleep(10)
        h.check("an address lock runs out", login("Sam", [], f"{subnet}5")[0], "prompt")

        # ---- an address lock does not lock out a character's owner (the address is shared: a household, a school, a web proxy)
        def lock_address(addr, victims):
            for nm in victims:
                login(nm, [BAD, BAD, BAD], addr)

        g1 = fresh()
        h.check("Gail is made from the shared address", create("Gail", g1)[0], "created")
        lock_address(g1, ("Hal", "Ian", "Jo", "Kit"))
        h.check("the shared address is locked (a stranger's name is refused)", login("Hal", [], g1)[0], "refused")
        stranger = login("Nobodyelse", [], g1)
        h.check("... as is a name that does not exist, with the same words", stranger[0] + " " + stranger[1].replace("\r", "").strip().splitlines()[-1][-len(REFUSED):], "refused " + REFUSED)
        r = login("Gail", [], g1)
        h.check("Gail's owner, from that address, still reaches the password prompt of Gail", r[0], "prompt")
        r = login("Gail", [BAD, GOOD], g1)
        h.check("... and gets in with the right password within three tries", r[0], "in")
        r = login("Gail", [], fresh())
        h.check("an address that Gail has not used is refused only if it is itself locked: this one is not, so Gail is not locked", r[0], "prompt")
        n.settle(0.2)
        time.sleep(10)

        g2 = fresh()
        h.check("Gus is made from another shared address", create("Gus", g2)[0], "created")
        lock_address(g2, ("Lee", "Mo", "Ned", "Oz"))
        r = login("Gus", [BAD, BAD, BAD], g2)
        h.check("the owner behind a locked address has only three tries of their own", r[0], "goodbye")
        h.check("... and then that address is refused for Gus as well", login("Gus", [], g2)[0], "refused")
        time.sleep(10)

        # ---- the fifth wrong password on a name tells the owner what to do; the owner's own address still has its three tries
        a5 = fresh()
        r1 = login("Ivy", [BAD, BAD, BAD], a5)
        h.check("the third wrong password of a connection ends it as before", r1[1].replace("\r", "").strip().splitlines()[-1], "Wrong password. Goodbye.")
        h.check("... without any advice", r1[1], absent=("wait a few minutes",))
        r2 = login("Ivy", [BAD, BAD], a5)
        h.check("the fifth wrong password on the name ends the connection and tells the owner to wait and try again", r2[1], "Wrong password. Goodbye.", "If it is your character, wait a few minutes and try again.")
        h.check("... and the name is locked", login("Ivy", [], fresh())[0], "refused")
        r3 = login("Ivy", [BAD, BAD, GOOD], owners["Ivy"])
        h.check("the owner reconnecting from a known address gets three tries of their own (two typos and the right password)", r3[0], "in")

        # ---- creating characters
        z = fresh()
        results = [create(nm, z)[0] for nm in ("Zoe", "Zack", "Zara")]
        h.check("three new characters an hour are allowed", str(results), "['created', 'created', 'created']")
        r = create("Zeke", z)
        h.check("the fourth is refused", r[0], "refused")
        h.check("... with a plain sentence", r[1], NEW_LIMIT)
        h.check("another address can still create", create("Zeke", fresh())[0], "created")
        h.check("existing characters still log in from the limited address", login("Zoe", [GOOD], z)[0], "in")
        time.sleep(41)
        results = [create(nm, z)[0] for nm in ("Zita", "Zane")]
        h.check("after the hour (40 s here) two more are allowed (the day allows five)", str(results), "['created', 'created']")
        r = create("Zelda", z)
        h.check("the daily limit refuses the sixth", r[0], "refused")

        # ---- the wizard command
        wz = fresh()
        c = n.bridge_client("Wizz", n.proxy(wz))
        c.read_until("> ")
        c.command("promote " + h.SECRET)
        c.command("quit")
        n.settle()
        # a level 0 character
        c0 = n.bridge_client("Nora", n.proxy(owners["Nora"]))
        c0.read_until("> ")
        h.check("below level 10 the verb does not exist", c0.command("throttle"), "What?")
        h.check("... nor does it appear in help", c0.command("help"), absent=("throttle",))
        c0.command("quit")
        wrong("Fay", 5, fresh())
        wrong("Edna", 5, fresh())
        c = n.bridge_client("Wizz", n.proxy(wz))
        c.read_until("> ")
        text = c.command("throttle")
        h.check("a level 10 wizard lists the locks", text, "Login throttle", "Fay", "Edna", "locked for")
        h.check("... and sees how often", text, "locked 1 time(s)")
        h.check("help lists it for a level 10 wizard", c.command("help"), "throttle")
        h.check("clear with no target says how to use it", c.command("throttle clear"), "Usage: throttle")
        h.check("clear NAME lifts the lock", c.command("throttle clear fay"), "Cleared Fay")
        h.check("... at once", str(login("Fay", [], fresh())[0]), "prompt")
        h.check("clear of something not recorded says so", c.command("throttle clear Nobody"), "Nothing is recorded")
        # an address lock
        ya = "203.0.113.200"
        for nm in ("Sam", "Zoe", "Zack", "Zara"):
            login(nm, [BAD, BAD, BAD], ya)
        h.check("an address is locked", login("Sam", [], ya)[0], "refused")
        h.check("the list shows the address", c.command("throttle"), "address 203.0.113.200")
        h.check("clear ADDRESS lifts it", c.command("throttle clear 203.0.113.200"), "Cleared 203.0.113.200")
        h.check("... at once", login("Sam", [], ya)[0], "prompt")
        h.check("clear all", c.command("throttle clear all"), "Cleared everything")
        h.check("the list is then empty", c.command("throttle"), "no locks")
        c.command("quit")

        # ---- what is and is not on disk and in the log
        save = open(os.path.join(work, "save", "players", "vera.json")).read()
        h.check("a character's save holds known addresses as keyed hashes", save, "knownAddresses")
        h.check("... never the address itself", save, absent=(owners["Vera"], "198.51.100"))
        text = n.logtext(log)
        h.check("no password, right or wrong, is in the log", text, absent=(BAD, GOOD))
        h.check("the log has only a few throttle lines, not one per attempt", str(len(n.lines_with(log, "login throttle:")) < 30), "True")
        h.check("the driver is still healthy", str(proc.poll() is None), "True")
    finally:
        if proc is not None:
            n.stop(proc)
        if h.failures:
            p = os.path.join(work, "driver1.log")
            if os.path.exists(p):
                print("--- driver1.log (tail)\n" + open(p).read()[-3000:])
        shutil.rmtree(work, ignore_errors=True)
    print()
    if h.failures:
        print(f"{len(h.failures)} FAILED:", *h.failures, sep="\n  ")
        return 1
    print("all throttle checks passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
