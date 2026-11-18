#!/usr/bin/env python3
"""Quick check for area builders: does every file under mudlib/domains
compile, pass the sandbox, and load in the driver?

Boots the driver on a temporary copy of the mudlib, makes a level-10 wizard,
then `goto`s every room and `clone`s everything in an npc/, mob/ or obj/
directory. Takes well under a minute.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4821 python3 tests/load_check.py [name-filter]

With a filter, only paths containing that text are loaded (for example
`cellars` or `goblin_guard`). Prints OK or FAILED for each path, with the
driver's message for failures, and exits non-zero if anything failed.
"""
import os
import shutil
import sys
import tempfile

os.environ.setdefault("LESMUDOS_TEST_PORT", "4821")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_test as a  # noqa: E402
import smoke_test as h  # noqa: E402


def domain_files(mudlib):
    """Every .cs file under mudlib/domains except the /domains/town demo, as mudlib paths without the extension."""
    found = []
    root = os.path.join(mudlib, "domains")
    for folder, _, names in os.walk(root):
        for name in sorted(names):
            if not name.endswith(".cs") or name == "area.cs":
                continue
            path = "/" + os.path.relpath(os.path.join(folder, name), mudlib).replace(os.sep, "/")[:-3]
            if not path.startswith("/domains/town/"):
                found.append(path)
    return sorted(found)


def main():
    wanted = sys.argv[1] if len(sys.argv) > 1 else ""
    work = tempfile.mkdtemp(prefix="lesmudos-load-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    proc, log = a.start_driver(work, mudlib)
    failed = []
    try:
        wiz = h.Client("checker")
        wiz.read_until("> ")
        wiz.command("promote " + h.SECRET)
        a.drain(wiz, 0.5)
        for path in domain_files(mudlib):
            if wanted not in path:
                continue
            # Rooms are walked into; everything in an npc/, mob/ or obj/ directory is cloned.
            is_room = not any(part in path for part in ("/npc/", "/mob/", "/obj/"))
            text = wiz.command(("goto " if is_room else "clone ") + path)
            a.drain(wiz, 0.3)
            bad = any(w in text for w in ("Failed", "sandbox", "***", "No such file")) or (not is_room and "Cloned" not in text)
            print(("FAILED  " if bad else "OK      ") + path)
            if bad:
                failed.append(path)
                print("        " + text.strip().replace("\r\n", "\n        ")[-1500:])
        wiz.send("shutdown")
        proc.wait(timeout=10)
    finally:
        if proc.poll() is None:
            proc.kill()
        log.close()
        errors = [l for l in open(os.path.join(work, "driver.log")).read().splitlines() if "ERROR" in l]
        shutil.rmtree(work, ignore_errors=True)
    if errors:
        print("\nDriver log errors:")
        print("\n".join(errors[:40]))
    print(f"\n{len(failed)} file(s) failed to load")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
