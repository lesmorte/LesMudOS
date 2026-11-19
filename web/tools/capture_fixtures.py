#!/usr/bin/env python3
"""Run the game's driver on a temporary mudlib, play a few commands, and copy the save files it writes
to the directory given as the first argument (the website's real-output fixtures).
Usage (from the repository root, after `dotnet build`): python3 web/tools/capture_fixtures.py OUTDIR"""
import os, sys, shutil, tempfile, time
os.environ.setdefault("LESMUDOS_TEST_PORT", "4827")
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
sys.path.insert(0, os.path.join(ROOT, "tests"))
import smoke_test as h
import chronicle_test as c

HOLLOW = "/domains/hollow/"

def do(client, line, quiet=0.7):
    client.read_until("\x00", timeout=quiet)
    client.send(line)
    text = client.read_until("> ")
    while True:
        more = client.read_until("\x00", timeout=quiet)
        if not more:
            return text
        text += more

def main():
    out = os.path.abspath(sys.argv[1])
    work = tempfile.mkdtemp(prefix="lesmudos-webfix-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    driver, log = c.start(work, mudlib)
    try:
        wiz = h.Client("Alice"); do(wiz, "promote " + c.SECRET)
        hero = h.Client("Bryn", archetype="warrior"); do(hero, "look")
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
        do(hero, "testdoor"); do(hero, "cache"); time.sleep(0.5)
        hero2 = h.Client("Cade", archetype="scout"); do(hero2, "look"); do(hero2, "testdoor"); do(hero2, "stump"); time.sleep(0.5)
        do(wiz, "goto " + HOLLOW + "village/test_door")  # the wizard walks the hidden places too: no history
        do(wiz, "goto /domains/greenvale/oakenfell/grove/great_stump")
        for cl in (hero, hero2, wiz): do(cl, "save")
        time.sleep(1.0)
        save = os.path.join(work, "save")
        shutil.rmtree(out, ignore_errors=True)
        shutil.copytree(save, out)
        for folder, _, names in os.walk(out):
            for n in names: print(os.path.relpath(os.path.join(folder, n), out))
    finally:
        driver.terminate()
        try: driver.wait(timeout=10)
        except Exception: driver.kill()
        log.close()
        shutil.rmtree(work, ignore_errors=True)

if __name__ == "__main__":
    main()
