#!/usr/bin/env python3
"""The Chronicle wired into play: a real character's deeds reach Renown and the ledger.

Starts its own driver on a copy of the mudlib (port from LESMUDOS_TEST_PORT, default 4826).
A wizard only sets the scene; everything recorded is done by a non-wizard character.
"""
import os, sys, shutil, tempfile, time
os.environ.setdefault("LESMUDOS_TEST_PORT", "4826")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h
import chronicle_test as c

HOLLOW = "/domains/hollow/"
failures = 0


def check(label, text, *expected, absent=()):
    global failures
    ok = all(e in text for e in expected) and not any(a in text for a in absent)
    print(("PASS  " if ok else "FAIL  ") + label)
    if not ok:
        failures += 1
        print("      expected:", expected, "absent:", absent)
        print("      got:", repr(text[-600:]))


def do(client, line, quiet=0.7):
    """Send a command and read until the game has gone quiet, so that notices which arrive
    on their own (renown gained, someone walking in) cannot put replies out of step."""
    client.read_until("\x00", timeout=quiet)
    client.send(line)
    text = client.read_until("> ")
    while True:
        more = client.read_until("\x00", timeout=quiet)
        if not more:
            return text
        text += more


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-chronplay-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    driver, log = c.start(work, mudlib)
    try:
        wiz = h.Client("Alice")
        do(wiz, "promote " + c.SECRET)
        hero = h.Client("Bryn", archetype="warrior")
        do(hero, "look")
        check("renown starts at Unknown", do(hero, "renown"), "Unknown")
        check("date names the year", do(hero, "date"), "Today is", "Year 1")
        check("score shows the renown tier", do(hero, "score"), "Renown: Unknown")
        do(hero, "west")
        check("the ledger stands in the guild", do(hero, "look"), "chronicle ledger")
        check("the ledger reads, and is empty of deeds", do(hero, "read chronicle").rsplit("\n", 1)[0], "Chronicle", absent=("Bryn",))

        # A wizard walking into a discovery makes no history.
        do(wiz, "goto " + HOLLOW + "cellars/hidden_cache")
        check("a wizard's discovery is not recorded", do(hero, "chronicle firsts"), absent=("Alice",))

        # The hero finds the Hidden Cache (brought there by the wizard: the finding is the hero's).
        do(wiz, "goto " + HOLLOW + "village/guild")
        do(hero, "east")
        # The hero reaches the far places through a door the test adds to the square.
        h.write(mudlib, "/domains/hollow/village/test_door", """// @inherit /std/room
public class TestDoor : Room
{
    public override void Create()
    {
        ShortDesc = "A Test Door";
        LongDesc = "A door for tests.";
        Exits["cache"] = "/domains/hollow/cellars/hidden_cache";
        Exits["stump"] = "/domains/greenvale/oakenfell/grove/great_stump";
        Exits["out"] = "/domains/hollow/village/square";
    }
}
""")
        h.edit(mudlib, "/domains/hollow/village/square", 'Exits["south"] = "/domains/hollow/village/tavern";',
               'Exits["south"] = "/domains/hollow/village/tavern";\n        Exits["testdoor"] = "/domains/hollow/village/test_door";')
        do(wiz, "update " + HOLLOW + "village/square")
        do(hero, "testdoor")
        check("the hero walks into the Hidden Cache", do(hero, "cache"), "Hidden Cache")
        time.sleep(0.5)
        before = do(hero, "renown")
        print("      " + before.strip().replace("\r\n", " | ")[:300])
        check("finding the cache earned renown and a title", before, "Renown: 30", "Local Hero")
        hero2 = h.Client("Cade", archetype="scout")
        do(hero2, "look")
        do(hero2, "testdoor")
        out = do(hero2, "stump")
        check("a second character walks to the Great Stump", out, "Great Stump")
        time.sleep(0.5)
        firsts = do(hero2, "renown")
        print("      " + firsts.strip().replace("\r\n", " | ")[:300])
        check("finding the Great Stump earned renown", firsts, "Renown: 40")
        do(hero2, "south"); do(hero2, "east")
        # the ledger shows both
        reader = h.Client("Dara", archetype="warrior")
        do(reader, "look")
        do(reader, "west")
        text = do(reader, "chronicle firsts")
        check("the ledger records the Great Stump as a first, by name", text, "Cade", "Stump")
        text = do(reader, "read chronicle")
        check("the ledger lists the entry", text, "The Great Stump Found")
        text += do(reader, "chronicle 1")
        check("the entry names the finder", text, "Cade")
        check("no wizard appears anywhere in the ledger", text + do(reader, "chronicle firsts"), absent=("Alice",))

        # a kill: the wizard brings a rat to the reader, who kills it
        do(wiz, "goto " + HOLLOW + "village/test_door")
        do(reader, "east"); do(reader, "testdoor")
        do(wiz, "clone " + HOLLOW + "mob/large_rat")
        reader.send("kill rat")
        out = reader.read_until("dies", timeout=120.0)
        check("the rat dies to a player", out, "dies")
        time.sleep(0.5)
        do(reader, "look")
        log.flush()
        logtext = open(os.path.join(work, "driver.log")).read()
        check("the driver logged no errors", logtext, absent=("ERROR",))
    finally:
        driver.terminate()
        try: driver.wait(timeout=10)
        except Exception: driver.kill()
        log.close()
        shutil.rmtree(work, ignore_errors=True)
    print(f"\n{failures} failure(s)")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
