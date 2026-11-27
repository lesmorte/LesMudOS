#!/usr/bin/env python3
"""Logging in again as a character who is online (docs/specs/login-throttle.md, "Taking over"): the name prompt does not say who is online,
the right password takes the body over, the old connection is told and closed, nothing else changes.

Cases (the numbers are the owner's list): 1 a body with a live connection; 3 a connection that never answers (half open) and the slot it gives
back; 4 two takeovers at once, and a takeover of a body made while the first connection sat at the password prompt; 5 a wizard, a wizard in
the workshop (sandbox input handler dropped), the editor, a y/n prompt, a hidden-input prompt; 6 the idle warning; 7 a name being created;
8 who, the log lines, GMCP, prompt and charset. (2, the link-dead body, is tests/return_test.py.)

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=5060 python3 tests/takeover_test.py
"""
import os
import re
import shutil
import socket
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "5060")
os.environ["LESMUDOS_MAX_PER_ADDRESS"] = "12"       # the slot test fills it
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402
import gmcp_test as g  # noqa: E402

TEST = "/domains/tk/"
HOLLOW = "/domains/hollow/"
check = h.check
PW = h.PASSWORD
NOTICE = "Your character has been taken over from another connection."
TAKE = "You take over your character where it stands."

WOLF = '''// @inherit /std/monster
public class TkWolf : Monster
{
    public override void Create()
    {
        Name = "wolf"; Ids.Add("wolf");
        ShortDesc = "a grey wolf"; LongDesc = "A wolf made for the takeover test.";
        Level = 1; MaxVitality = 100000; DamageMin = 1; DamageMax = 1; ToHit = 60; BaseReaction = 2.0;
        Aggressive = true;
    }
}
'''

WAND = '''// @inherit /std/item
public class TkWand : Item
{
    public override void Create() { Name = "wand"; Ids.Add("wand"); ShortDesc = "a takeover wand"; }
    public override void Init()
    {
        base.Init();
        AddAction("grab", a => { InputTo(line => Write("CAUGHT " + line + "\\n")); Write("Grabbing.\\n"); return true; });
    }
}
'''


def login(name):
    c = h.Client(name)
    c.read_until("> ")
    return c


def at_password(name):
    """A connection that has given a known name and is at the password prompt."""
    c = h.Client(name, login=False)
    text = c.read_until("Password: ")
    return c, text


def finish(c, timeout=15.0):
    """Give the password to a connection at the prompt; the text up to the next prompt."""
    c.send(PW)
    c.choose_calling(None)
    return c.read_until("> ", timeout)


def vitality(text):
    m = re.findall(r"\((\d+)/(\d+)\) \(\d+/\d+\) \(\d+/\d+\) > ", text)
    return (int(m[-1][0]), int(m[-1][1])) if m else None


def ends(c, seconds=4.0):
    """(what arrived, whether the server closed the connection) within a while."""
    end = time.time() + seconds
    data = ""
    while time.time() < end:
        try:
            chunk = c.sock.recv(65536)
        except socket.timeout:
            continue
        except OSError:
            return data, True
        if not chunk:
            return data, True
        data += c.rx(chunk)
    return data, False


def gone(c, seconds=3.0):
    return c.closed(seconds)


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-takeover-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    os.makedirs(os.path.join(mudlib, "domains", "tk"))
    h.write(mudlib, TEST + "a", a.room("TkA", "Takeover Room A", {"up": HOLLOW + "village/square", "wolf": TEST + "den"}, reset=3000))
    h.write(mudlib, TEST + "den", a.room("TkDen", "Takeover Wolf Den", {"out": TEST + "a"}, f'        AddSpawn("{TEST}wolf");\n', reset=3000))
    h.write(mudlib, TEST + "wolf", WOLF)
    h.edit(mudlib, HOLLOW + "village/square", 'Exits["south"]', f'Exits["down"] = "{TEST}a";\n        Exits["south"]')
    os.makedirs(os.path.join(mudlib, "wiz", "wren"), exist_ok=True)
    os.makedirs(os.path.join(mudlib, "wiz", "boss"), exist_ok=True)
    h.write(mudlib, "/wiz/wren/lab", a.room("WrenLab", "Wren's Lab", {"out": TEST + "a"}))
    h.write(mudlib, "/wiz/wren/wand", WAND)
    os.environ["LESMUDOS_IDLE_MINUTES"] = "8s"
    os.environ["LESMUDOS_TIMEOUT_MINUTES"] = "30s"
    os.environ["LESMUDOS_LINGER_SECONDS"] = "4"
    proc, log = a.start_driver(work, mudlib)
    try:
        run(work)
        c = h.Client("boss")
        c.read_until("> ")
        c.command("shutdown")
        proc.wait(timeout=15)
        log.flush()
        logged = open(os.path.join(work, "driver.log")).read()
        ended = [l for l in logged.splitlines() if "connection ended:" in l]
        taken = [l for l in ended if "closed by the game (taken over)" in l]
        check("the log: a taken-over connection ends 'closed by the game (taken over)' with the player's name", "\n".join(taken), "player Alice", "player Wren", "player Gina")
        check("the log: and does not say a body lingers", "\n".join(taken), absent=("lingers",))
        check("the log: each takeover has its own line, with both addresses", "\n".join(l for l in logged.splitlines() if "takeover:" in l), "replaced by the one from 127.0.0.1")
        check("the log: no password in it", logged, absent=(PW,))
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
    # ---------------------------------------------------------------- 1: a body with a live connection
    olga = login("olga")
    olga.command("down")
    old = login("alice")
    old.command("down")
    a.drain(olga, 0.5)
    carried = old.command("inventory")

    # the name prompt is the same for a name that is online and a name that is not
    quinn = login("quinn")
    quinn.command("quit")
    quinn.closed(3)
    t0 = time.time()
    on, online_text = at_password("alice")
    t_on = time.time() - t0
    t0 = time.time()
    off, offline_text = at_password("quinn")
    t_off = time.time() - t0
    check("the name prompt: an online name and an offline one get the same words", "same" if online_text == offline_text else f"{online_text!r} != {offline_text!r}", "same")
    check("the name prompt: and the prompt is the password prompt, not 'already here'", online_text, "Password: ", absent=("already here", "Another name"))
    check("the name prompt: the same speed (no pause for one and not the other)", "ok" if abs(t_on - t_off) < 1.0 else f"{t_on:.2f} s against {t_off:.2f} s", "ok")
    off.sock.close()

    # a wrong password against an online character counts like any other and tells the character nothing
    on.send("not the password")
    check("wrong password on an online name: the same answer as for any name", on.read_until("Password: "), "Wrong password.")
    check("wrong password on an online name: the connected player is not told", a.quiet(old, 1.5), absent=("taken over", "failed", "Someone", "log in"))
    check("wrong password on an online name: and is not disturbed", old.command("look"), "Takeover Room A")
    on.sock.close()

    # the right password
    new = h.Client("alice")
    text = new.read_until("> ")
    check("takeover: the new connection is told", text, TAKE, "Takeover Room A")
    check("takeover: it is not a new arrival", text, absent=("Welcome", "enters the game", "already here", "Another name"))
    check("takeover: the old connection is told, in one line", a.quiet(old, 2.0), NOTICE)
    check("takeover: and is closed", str(old.closed(3)), "True")
    check("takeover: the room is told nothing", a.quiet(olga, 1.5), absent=("Alice", "enters", "eyes", "fades", "disconnects"))
    who = olga.command("who")
    check("takeover: who lists the character once", str(len(re.findall(r"\bAlice\b", who))), "1")
    check("takeover: nothing marks her link-dead", who, absent=("link-dead",))
    check("takeover: the state is as it was (what she carries)", "same" if new.command("inventory") == carried else "different", "same")
    check("takeover: the new connection plays", (new.command("say hello there"), a.quiet(olga, 1.0))[1], "Alice says: hello there")

    # ---------------------------------------------------------------- the fight goes on; Vitality as it was
    new.command("wolf")
    new.send("kill wolf")
    a.quiet(new, 5.0)
    swap = h.Client("alice")
    text = swap.read_until("> ")
    check("fight: taken over in the middle of it", text, TAKE, "Takeover Wolf Den")
    check("fight: the old connection is closed", str(new.closed(3)), "True")
    more = a.quiet(swap, 8.0)
    check("fight: it goes on (the wolf still attacks, the character still strikes back)", text + more, "grey wolf")
    v = vitality(text + more)
    check("fight: Vitality is not made whole by the takeover", "ok" if v and v[0] < v[1] else f"{v}", "ok")
    swap.command("flee")
    swap.command("quit")
    swap.closed(3)

    # ---------------------------------------------------------------- a preparation in progress continues
    pru = login("pru")
    c2, _ = at_password("pru")
    pru.send("search")
    c2.send(PW)
    c2.choose_calling(None)
    text = c2.read_until("find nothing of note", 8.0)
    check("preparation: a search begun on the old connection finishes for the new one", text, TAKE, "You search carefully but find nothing of note")
    check("preparation: the old connection was closed", str(gone(pru)), "True")
    c2.command("quit")
    c2.closed(3)

    # ---------------------------------------------------------------- hidden-input prompt (a mortal changing her password)
    pam = login("pam")
    pam.send("password")
    pam.read_until("Current password: ")
    swap = h.Client("pam")
    text = swap.read_until("> ")
    check("password prompt: the new connection gets a clean command prompt", text, TAKE, "question that was waiting for your answer was cancelled")
    check("password prompt: its first line is a command, not an answer", swap.command(PW), "What?")
    check("password prompt: the character is not locked out afterwards", swap.command("look"), "Village Square")
    swap.command("quit")
    swap.closed(3)

    # ---------------------------------------------------------------- prompt and charset apply to the new connection
    ron = login("ron")
    ron.command("prompt TKO> ")
    ron.command("charset ascii")
    swap = h.Client("ron")
    text = swap.read_until("> ")
    check("prompt: the prompt setting is the character's, so the new connection has it", text, TAKE, "TKO> ")
    check("charset: so is the charset", swap.command("charset"), "plain ASCII")
    swap.command("quit")
    swap.closed(3)

    # ---------------------------------------------------------------- GMCP: the new connection gets the whole login burst
    gina = g.GClient("gina")
    gina.read_until("> ")
    gina.pump(1.0)
    check("gmcp: the first connection had its burst", str(sorted({p for p, v in gina.frames if p in ("Char.Status", "Room.Info", "Char.Vitals", "Char.Quests.List")})), "Char.Quests.List", "Char.Status", "Char.Vitals", "Room.Info")
    swap = g.GClient("gina")
    text = swap.read_until("> ")
    swap.pump(1.5)
    got = {p for p, v in swap.frames}
    check("gmcp: the connection that took over gets Char.Status, Room.Info, Char.Vitals and Char.Quests.List", str(sorted(got)), "Char.Quests.List", "Char.Status", "Char.Vitals", "Room.Info")
    check("gmcp: and the text", text, TAKE)
    check("gmcp: the old one was closed", str(gina.closed(3)), "True")
    swap.command("quit")
    swap.closed(3)

    # ---------------------------------------------------------------- 7: a name being created now
    p = h.Client("newbie", login=False)
    check("creating: the first connection is asked to choose a password", p.read_until("characters): "), "Creating a new account for Newbie")
    q = h.Client("newbie", login=False)
    check("creating: a second connection with that name is told it is being set up (unchanged)", q.read_until("Another name?"), "Someone else is setting up Newbie")
    p.send(PW)
    p.read_until("again: ")
    p.send(PW)
    p.choose_calling("adventurer")
    p.read_until("> ")
    q.send("Newbie")
    check("creating: once the character exists the name asks for the password like any other", q.read_until("Password: "), "Password: ", absent=("already here",))
    q.send(PW)
    q.choose_calling(None)
    check("creating: and the password takes it over", q.read_until("> "), TAKE)
    check("creating: the creating connection was closed", str(p.closed(3)), "True")
    q.command("quit")
    q.closed(3)

    # ---------------------------------------------------------------- 4: two at once; one made while the first sat at the password prompt
    olga.sock.close()
    olga = login("otto")
    olga.command("down")
    rae = login("rae")
    r1, _ = at_password("rae")
    r2, _ = at_password("rae")
    r1.send(PW)
    r2.send(PW)
    clients = [("old", rae), ("first", r1), ("second", r2)]
    seen = [(n, ends(c)) for n, c in clients]
    survivors = [n for n, (text, closed) in seen if not closed]
    check("race: of three connections for one name exactly one is left", str(len(survivors)), "1")
    keeper = dict(clients)[survivors[0]] if survivors else None
    if keeper is not None:
        check("race: and it is the one in play (one body)", keeper.command("who"), "Rae")
        check("race: who lists her once", str(len(re.findall(r"\bRae\b", olga.command("who")))), "1")
        keeper.command("quit")
        keeper.closed(3)

    sam = login("sam")
    sam.command("quit")
    sam.closed(3)
    x, _ = at_password("sam")           # at the prompt, a body not yet made
    y = login("sam")                      # logs in fully meanwhile: this is the body
    text = finish(x)
    check("race: a connection waiting at the prompt takes over the body another one made meanwhile", text, TAKE)
    check("race: the other connection is closed", str(gone(y)), "True")
    check("race: one body", str(len(re.findall(r"\bSam\b", olga.command("who")))), "1")
    x.command("quit")
    x.closed(3)

    # ---------------------------------------------------------------- 5: wizards
    boss = login("boss")
    check("wizard: boss becomes level 10", boss.command("promote " + h.SECRET), "level 10 wizard")
    wren = login("wren")
    boss.command("promote wren 1")
    wren.command("down")
    check("wizard: Wren walks into the workshop", wren.command("goto /wiz/wren/lab"), "step into the workshop")
    wren.command("clone /wiz/wren/wand")
    check("wizard: and holds a sandbox wand", wren.command("i"), "wand")
    wren.send("grab")
    check("wizard: whose code takes the next line she types", wren.read_until("Grabbing."), "Grabbing.")
    swap = h.Client("wren")
    text = swap.read_until("> ")
    check("workshop: the wizard in the workshop is taken over like anyone", text, TAKE, "Wren's Lab")
    check("workshop: the old connection is told and closed", a.quiet(wren, 1.5), NOTICE)
    check("workshop: the first line on the new connection goes to the game, not to the wand's handler", swap.command("say first line"), "You say", absent=("CAUGHT"))
    check("workshop: she is still in the workshop with her checkpoint", swap.command("score"), "[in the workshop: nothing here is saved]")
    swap.send("grab")
    swap.read_until("Grabbing.")
    swap2 = h.Client("wren")
    text = swap2.read_until("> ")
    check("workshop: taken over while the wand is waiting for a line", text, TAKE, "Wren's Lab", absent=("CAUGHT",))
    check("workshop: the wand's handler is gone", swap2.command("look"), "Wren's Lab", absent=("CAUGHT",))
    check("workshop: and the earlier connection is closed", str(swap.closed(3)), "True")
    got = swap2.command("out")
    check("workshop: leaving the workshop still works on the new connection", got, "Takeover Room A")
    check("workshop: and the connection is still hers afterwards", swap2.command("look"), "Takeover Room A")
    swap2.command("quit")
    swap2.closed(3)

    # a wizard in the editor, and at a y/n prompt
    boss.send("edit /wiz/boss/notes")
    boss.read_until(":")
    boss.send("a")
    boss.send("unsaved line")
    time.sleep(0.7)
    swap = h.Client("boss")
    text = swap.read_until("> ")
    check("editor: taken over in the editor", text, TAKE, "The editor was closed with the old connection")
    check("editor: the old connection is closed", str(boss.closed(3)), "True")
    check("editor: the first line is a command, not editor input", swap.command("pwd"), absent=("Unknown editor command", "unsaved"))
    check("editor: nothing was written", swap.command("cat /wiz/boss/notes"), "no such file")
    h.write(os.path.join(work, "mudlib"), "/wiz/boss/doomed", "// not code\n")
    swap.send("delete /wiz/boss/doomed")
    swap.read_until("(y/n)")
    again = h.Client("boss")
    text = again.read_until("> ")
    check("y/n: taken over at a (y/n) prompt", text, TAKE, "question that was waiting for your answer was cancelled")
    check("y/n: the first line, y, is a command and answers nothing", again.command("y"), "What?")
    check("y/n: the file is still there", again.command("cat /wiz/boss/doomed"), "not code")
    swap.closed(3)
    boss = again

    # ---------------------------------------------------------------- 3 and the slots: a connection that never answers
    for c in (olga,):
        c.command("quit")
        c.closed(3)
    time.sleep(1.0)
    dead = login("hana")                  # 1 connection; from here it is never read until the end: half open
    fillers = []
    for _ in range(9):
        s = socket.create_connection(("127.0.0.1", h.PORT), timeout=5)
        fillers.append(s)
    boss_conn_open = 1                    # boss still holds one
    b, _ = at_password("hana")            # the 12th
    s = socket.create_connection(("127.0.0.1", h.PORT), timeout=5)
    s.settimeout(3.0)
    try:
        refused = s.recv(200).decode(errors="replace")
    except OSError:
        refused = ""
    s.close()
    check("slots: at the limit a new connection is refused", refused, "Too many connections")
    text = finish(b)
    check("slots: the takeover works with the old connection silent", text, TAKE)
    time.sleep(0.5)
    s = socket.create_connection(("127.0.0.1", h.PORT), timeout=5)
    s.settimeout(3.0)
    try:
        admitted = s.recv(200).decode(errors="replace")
    except OSError:
        admitted = ""
    s.close()
    check("slots: the dead connection's slot is given back at once (a new connection is welcome)", admitted, absent=("Too many",))
    check("slots: and the silent connection was told and closed", dead.read_until(NOTICE, 3.0), NOTICE)
    check("slots: closed", str(dead.closed(3)), "True")
    for s in fillers:
        s.close()
    b.command("quit")
    b.closed(3)
    boss.command("quit")
    boss.closed(3)
    time.sleep(1.0)

    # ---------------------------------------------------------------- 6: the idle warning (last: the idle times are short)
    ida = login("ida")
    text = ida.read_until("You have been idle for a while", 25.0)
    check("idle: the old connection reached the warning", text, "You have been idle for a while")
    swap = h.Client("ida")
    text = swap.read_until("> ")
    check("idle: taken over at the warning", text, TAKE)
    time.sleep(16.0)                      # past the moment the old connection would have been cut (30 s of silence)
    check("idle: the takeover counts as activity: the new connection is still here after the old one's deadline", swap.command("look"), "Village Square")
    swap.command("quit")
    swap.closed(3)


if __name__ == "__main__":
    sys.exit(main())
