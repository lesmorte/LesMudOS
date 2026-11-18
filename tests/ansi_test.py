#!/usr/bin/env python3
"""Tests for ANSI colour and command highlighting.

Boots the driver on a temporary copy of the mudlib with a small test area
(/domains/atest), and checks, byte for byte where it matters:

  * tags are rendered with colour on and removed with colour off
  * the `ansi` command, its aliases, its persistence and its default
  * `cat` and the editor's `p` show files exactly as stored
  * text typed by players cannot carry tags or colour (say, title, echoes)
  * `{{`, unknown tags and unclosed tags
  * shop columns line up on the visible text
  * a non-player object receives the text untouched

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4814 python3 tests/ansi_test.py
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

os.environ.setdefault("LESMUDOS_TEST_PORT", "4814")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402

ESC = "\x1b"
CMD = ESC + "[1;96m"
BOLD = ESC + "[1m"
RESET = ESC + "[0m"
A = "/domains/atest/"

FILES = {
    "hall": '''
// @inherit /std/room
public class Hall : Room
{
    public override void Create()
    {
        ShortDesc = "Test Hall";
        LongDesc = "A bare hall for testing.";
        Exits["north"] = "/domains/atest/shop";
        Exits["up"] = "/domains/atest/shop";
        AddDetail("flag", "A flag that says {cmd}wave{/} to everyone.");
        AddDetail("leak", "An {cmd}unclosed command\\nNext line is plain.");
        AddDetail("brace", "Braces: {{ and {0} and {foo} and {cmd}x{/}.");
        AddDetail("styles", "{b}bold{/} {u}under{/} {red}red{/} {bgreen}bright{/} {title}title{/}");
        AddReadable("slate", "The slate says {cmd}erase{/} and {{plain}.");
        AddSpawn("/domains/atest/echo");
        AddSpawn("/domains/atest/gizmo");
    }
}
''',
    "shop": '''
// @inherit /std/room
public class ShopRoom : Room
{
    public override void Create()
    {
        ShortDesc = "Test Shop";
        LongDesc = "A shop.";
        Exits["south"] = "/domains/atest/hall";
        AddSpawn("/domains/atest/seller");
    }
}
''',
    "seller": '''
// @inherit /std/shopkeeper
public class Seller : Shopkeeper
{
    public override void Create()
    {
        Name = "Seller";
        Ids.Add("seller");
        ShortDesc = "the seller";
        LongDesc = "A seller.";
        AddStock("/domains/atest/plain", 7);
        AddStock("/domains/atest/tagged", 12);
        AddStock("/domains/atest/longname", 123);
    }
}
''',
    "plain": '''
// @inherit /std/item
public class Plain : Item
{
    public override void Create() { Name = "plain thing"; Ids.Add("plain"); ShortDesc = "a plain thing"; }
}
''',
    # Breaks the rule "no tags in ShortDesc" on purpose, to check that columns follow the visible text.
    "tagged": '''
// @inherit /std/item
public class Tagged : Item
{
    public override void Create() { Name = "tagged thing"; Ids.Add("tagged"); ShortDesc = "a {b}tagged{/} thing"; }
}
''',
    "longname": '''
// @inherit /std/item
public class LongName : Item
{
    public override void Create() { Name = "long thing"; Ids.Add("long"); ShortDesc = "a thing with a longish name"; }
}
''',
    # A non-player that records everything it is told, and reports it on "ask echo about heard".
    "echo": '''
// @inherit /std/npc
public class Echo : Npc
{
    private readonly List<string> heard = new();
    public override void Create()
    {
        Name = "Echo";
        Ids.Add("echo");
        ShortDesc = "an echo";
        LongDesc = "An echo you can {cmd}talk{/} to.";
        AddChat("Try {cmd}wave{/} now.");
    }
    public override void CatchTell(string message) { heard.Add(message); }
    public override string? Ask(ObjectRef who, string topic)
    {
        if (topic == "clear") { heard.Clear(); return "cleared"; }
        return "HEARD[" + Esc(string.Join("|", heard)) + "]"; // escaped, so that the player sees the tags as the NPC got them
    }
}
''',
    "gizmo": '''
// @inherit /std/thing
public class Gizmo : Thing
{
    public override void Create() { Name = "gizmo"; ShortDesc = "a gizmo"; }
    public override void Init() { AddAction("flash", Flash); AddAction("partial", Partial); }
    private bool Flash(string args)
    {
        TellRoom(Environment()!, "The gizmo flashes {cmd}brightly{/}.\\n");
        return true;
    }
    private bool Partial(string args) { Write("{red}no newline here"); return true; }
}
''',
}

NOTE = "{cmd}look{/} {{ {red}x{/} {0} {foo}\n    indented {b}line{/}\n"


def start_driver(work, mudlib):
    log = open(os.path.join(work, "driver.log"), "a")
    dll = os.path.join(h.ROOT, "src", "LesMudOS.Driver", "bin", "Debug", "net10.0", "LesMudOS.Driver.dll")
    proc = subprocess.Popen(
        ["dotnet", dll, "--mudlib", mudlib, "--save", os.path.join(work, "save"), "--port", str(h.PORT)],
        stdout=log, stderr=subprocess.STDOUT, env={**os.environ, "LESMUDOS_ADMIN_SECRET": h.SECRET})
    for _ in range(100):
        try:
            socket.create_connection(("127.0.0.1", h.PORT), timeout=0.3).close()
            break
        except OSError:
            time.sleep(0.2)
    return proc, log


def strip(text):
    return re.sub(r"\x1b\[[0-9;]*m", "", text)


def help_of(c, raw=False):
    """The help text of a wizard contains '> ', so read to its last line and then to the prompt."""
    c.raw_reset()
    c.send("help")
    text = c.read_until("stop the driver")
    text += c.read_until("> ")
    return c.raw if raw else text


def wrapped(text, tag, word):
    return tag + word + RESET in text


def main():
    check = h.check
    work = tempfile.mkdtemp(prefix="lesmudos-ansi-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib, novice=True)
    os.makedirs(os.path.join(mudlib, "domains", "atest"))
    for name, source in FILES.items():
        h.write(mudlib, A + name, source)
    with open(os.path.join(mudlib, "domains", "atest", "note.txt"), "w") as f:
        f.write(NOTE)
    h.edit(mudlib, "/std/player", '"/domains/hollow/village/square"', f'"{A}hall"')
    proc, log = start_driver(work, mudlib)
    try:
        # ---- default on, rendering ----
        a = h.Client("alice")
        first = a.read_until("> ")
        check("a new character has colour on: login text is coloured", first, "Welcome, Alice", "Test Hall")
        check("room name arrives in bold", str(wrapped(a.raw, BOLD, "Test Hall")), "True")
        check("welcome line highlights help", str(wrapped(a.raw, CMD, "help")), "True")
        a.command("promote " + h.SECRET)
        b = h.Client("bob")
        b.read_until("> ")
        a.read_until("enters the game", 3)

        raw = a.raw_command("look")
        check("look: room name bold", str(wrapped(raw, BOLD, "Test Hall")), "True")
        check("look: every exit is a command", str(wrapped(raw, CMD, "north") and wrapped(raw, CMD, "up")), "True")
        check("look: exits line reads normally once stripped", a.command("look"), "Obvious exits: north, up", "Test Hall")
        raw = a.raw_command("look at flag")
        check("a detail's tagged command is wrapped in bold bright cyan", str(wrapped(raw, CMD, "wave")), "True")
        raw = a.raw_command("talk echo")
        check("an NPC line wraps its command", str(wrapped(raw, CMD, "wave")), "True")
        check("NPC line reads normally once stripped", a.command("talk echo"), 'Echo says: "Try wave now."')
        raw = a.raw_command("read slate")
        check("readable: command wrapped, {{ is a literal brace", str(wrapped(raw, CMD, "erase") and "{plain}" in raw), "True")
        raw = a.raw_command("look at styles")
        check("plain styles", str(BOLD + "bold" + RESET in raw and ESC + "[4munder" + RESET in raw and ESC + "[31mred" + RESET in raw
                                  and ESC + "[92mbright" + RESET in raw and BOLD + "title" + RESET in raw), "True")
        raw = help_of(a, raw=True)
        check("help names commands in bold bright cyan", str(wrapped(raw, CMD, "look") and wrapped(raw, CMD, "ansi") and wrapped(raw, CMD, "browse")), "True")
        check("help mentions ansi", help_of(a), "ansi [on|off]")
        raw = a.raw_command("ansi sideways")
        check("a usage message (NotifyFail) is rendered too", str(wrapped(raw, CMD, "ansi")), "True")
        check("usage text once stripped", a.command("ansi sideways"), "Usage: ansi [on|off]")

        # ---- braces, unknown tags, unclosed tags ----
        raw = a.raw_command("look at brace")
        check("{{ renders as {, unknown tags are left alone", re.sub(r"\x1b\[[0-9;]*m", "", raw), "Braces: { and {0} and {foo} and x.")
        check("only the known tag became colour", str(wrapped(raw, CMD, "x") and raw.count(ESC) == 2), "True")
        raw = a.raw_command("look at leak")
        check("an unclosed tag is reset before the line ends", str(CMD + "unclosed command" + RESET + "\r\nNext line is plain." in raw), "True")
        raw = a.raw_command("partial")
        check("an unclosed tag is reset at the end of the text", str("no newline here" + RESET in raw and ESC + "[31m" in raw), "True")

        # ---- non-player receives text untouched ----
        a.command("ask echo about clear")
        a.command("flash")
        a.command("say plain words")
        heard = a.command("ask echo about heard")
        check("a non-player is told the tags as written", heard, "HEARD[", "The gizmo flashes {cmd}brightly{/}.", "Alice says: plain words")
        check("a non-player is told no escape sequences", str(ESC in heard), "False")

        # ---- raw file output ----
        raw = a.raw_command("cat /domains/atest/note.txt")
        check("cat shows tags exactly as stored (colour on)", raw, "{cmd}look{/} {{ {red}x{/} {0} {foo}", "    indented {b}line{/}", "(2 lines)")
        check("cat sends no escape sequences", str(ESC in raw), "False")
        a.ed("edit /domains/atest/note.txt")
        raw_p = a.ed("p")
        check("editor p shows tags exactly as stored (colour on)", raw_p, "   1  {cmd}look{/} {{ {red}x{/} {0} {foo}", "   2      indented {b}line{/}")
        a.command("q")
        raw = a.raw_command("list /domains/atest")
        check("list is plain text", raw, "note.txt", "hall.cs")
        check("list sends no escape sequences", str(ESC in raw), "False")

        # ---- player-typed text ----
        out = a.raw_command("say {red}hello{/}")
        check("say: the speaker sees the text as typed", out, "You say: {red}hello{/}")
        check("say: no colour for the speaker", str(ESC + "[31m" in out or ESC in out), "False")
        heard_b = b.read_until("hello")
        check("say: the listener sees the text as typed", heard_b, "Alice says: {red}hello{/}")
        check("say: no colour for the listener", str(ESC in b.raw.split("Alice says")[-1]), "False")
        out = a.raw_command("say {{double}} {cmd}x{/}")
        check("say: doubled braces are shown as typed", out, "You say: {{double}} {cmd}x{/}")
        check("say: no sequences at all", str(ESC in out), "False")
        b.read_until("{cmd}x{/}")
        out = a.raw_command("title {b}shiny{/} {cmd}tag")
        check("title: stored text shown as typed", out, "You are now Alice {b}shiny{/} {cmd}tag.")
        out = b.raw_command("who")
        check("who: title shown as typed", out, "Alice {b}shiny{/} {cmd}tag")
        check("who: no colour from the title", str(ESC in out), "False")
        out = b.raw_command("look at alice")
        check("look at a player: title shown as typed", out, "Alice {b}shiny{/} {cmd}tag, who has visited")
        check("look at a player: no colour from the title", str(ESC in out), "False")
        out = a.raw_command("look at {red}nothing{/}")
        check("failure text echoing input shows it as typed", out, "You see no '{red}nothing{/}' here.")
        check("failure text echoing input has no colour", str(ESC in out), "False")
        out = a.raw_command("read {b}x")
        check("read: echoed input as typed", out, "You see no '{b}x' to read.")
        check("read: no colour", str(ESC in out), "False")
        a.command("north")
        out = a.raw_command("buy {red}sword{/}")
        check("shop refusal echoes the item as typed", strip(out), "does not sell '{red}sword{/}'. Try browse.")
        check("shop refusal: only the browse hint is coloured", str(wrapped(out, CMD, "browse") and ESC + "[31m" not in out), "True")
        out = a.raw_command("cd {red}nowhere")
        check("wizard path errors echo the input as typed", out, "/{red}nowhere: no such directory.")
        check("wizard path errors have no colour", str(ESC in out), "False")
        a.command("south")
        # Raw control bytes cannot be typed: the connection drops everything below 32.
        a.send("say a\x1b[31mb")
        out = a.read_until("> ")
        check("typed ESC bytes are dropped by the connection", out, "You say: a[31mb")
        check("no ESC reached the speaker", str(ESC in a.raw.split("You say: a")[-1]), "False")

        # ---- shop columns ----
        a.command("north")
        raw = a.raw_command("browse")
        plain = re.sub(r"\x1b\[[0-9;]*m", "", raw)
        lines = [l for l in plain.split("\r\n") if l.endswith(" gold")]
        ends = {len(l) for l in lines}
        check("browse lists every item", plain, "A plain thing", "A tagged thing", "A thing with a longish name")
        check("browse prices end in the same column when colour is stripped", str(len(lines) == 3 and len(ends) == 1), "True")
        check("the tag in a name was rendered", str(BOLD + "tagged" + RESET in raw), "True")

        # ---- ansi off ----
        check("ansi shows the setting", a.command("ansi"), "ANSI colour is on")
        raw = a.raw_command("ansi off")
        check("ansi off confirms", raw, "ANSI colour is now off.")
        check("ansi off: the confirmation is plain", str(ESC in raw), "False")
        check("ansi shows off", a.command("ansi"), "ANSI colour is off")
        check("ansi accepts on/off in capitals", a.command("ansi ON") + a.command("ansi Off"), "now on", "now off")
        a.command("south")
        raw = a.raw_command("look")
        check("colour off: no escape byte in a room", str(ESC in raw), "False")
        check("colour off: no tag text in a room", str("{" in raw), "False")
        check("colour off: the text is otherwise unchanged", raw, "Test Hall", "A bare hall for testing.", "Obvious exits: north, up")
        raw = a.raw_command("talk echo")
        check("colour off: NPC line has no tags and no escapes", raw, 'Echo says: "Try wave now."', absent=(ESC, "{"))
        raw = a.raw_command("look at brace")
        check("colour off: { and unknown tags kept, known tag removed", raw, "Braces: { and {0} and {foo} and x.", absent=(ESC, "{{", "{cmd}"))
        raw = a.raw_command("look at styles")
        check("colour off: styles removed", raw, "bold under red bright title", absent=(ESC, "{b}"))
        raw = help_of(a, raw=True)
        check("colour off: help has no escapes or tags", raw, "Commands: look [at THING]", absent=(ESC, "{cmd}"))
        raw = a.raw_command("cat /domains/atest/note.txt")
        check("cat shows tags exactly as stored (colour off)", raw, "{cmd}look{/} {{ {red}x{/} {0} {foo}", "    indented {b}line{/}")
        check("cat sends no escapes (colour off)", str(ESC in raw), "False")
        a.ed("edit /domains/atest/note.txt")
        raw_p = a.ed("p")
        check("editor p shows tags exactly as stored (colour off)", raw_p, "{cmd}look{/} {{ {red}x{/} {0} {foo}")
        a.command("q")
        out = a.raw_command("say {red}hello{/}")
        check("colour off: say shown as typed", out, "You say: {red}hello{/}", absent=(ESC,))
        check("colour off: the listener sees it as typed", b.read_until("hello"), "Alice says: {red}hello{/}")
        out = b.raw_command("who")
        check("colour off for alice: bob (colour on) still sees the title as typed", out, "Alice {b}shiny{/} {cmd}tag")
        a.command("ask echo about clear")
        a.command("flash")
        heard = a.command("ask echo about heard")
        check("a non-player is told the same text whatever the player's setting", heard, "The gizmo flashes {cmd}brightly{/}.")
        a.command("north")
        raw = a.raw_command("browse")
        plain = re.sub(r"\x1b\[[0-9;]*m", "", raw)
        lines = [l for l in plain.split("\r\n") if l.endswith(" gold")]
        check("browse columns line up with colour off", str(len(lines) == 3 and len({len(l) for l in lines}) == 1 and ESC not in raw and "{" not in raw), "True")
        a.command("south")

        # ---- persistence ----
        a.send("quit")
        a.read_until("Goodbye")
        a = h.Client("alice")
        text = a.read_until("> ")
        check("colour off is remembered across logout and login", str(ESC in a.raw), "False")
        check("... and the login text is complete", text, "Welcome back, Alice", "Test Hall")
        check("ansi still shows off", a.command("ansi"), "ANSI colour is off")
        with open(os.path.join(work, "save", "players", "alice.json")) as f:
            saved = json.load(f)
        check("the setting is in the save file", str(saved.get("colour")), "False")
        a.command("colour on")
        check("colour is an alias", a.raw_command("look"), "Test Hall")
        a.send("quit")
        a.read_until("Goodbye")
        a = h.Client("alice")
        a.read_until("> ")
        check("colour on is remembered across logout and login", str(BOLD + "Test Hall" + RESET in a.raw), "True")
        a.command("color off")
        check("color is an alias", a.command("ansi"), "ANSI colour is off")
        a.command("ansi on")

        # ---- a character saved before the setting existed ----
        with open(os.path.join(work, "save", "players", "oldie.json"), "w") as f:
            json.dump({"cwd": "/", "lastLogin": 0, "logins": 3, "title": "the elder"}, f)
        o = h.Client("oldie", login=False)
        o.read_until("characters): ")
        o.send("old-pass-word")
        o.read_until("again: ")
        o.send("old-pass-word")
        o.read_until("Your calling: ")  # an existing character is asked for a calling at the next login (class system)
        o.send("novice")
        o.read_until("> ")
        check("an existing character without the field gets colour on", str(BOLD + "Test Hall" + RESET in o.raw), "True")
        o.send("quit")
        o.read_until("Goodbye")

        # ---- the login prompts stay plain; the counter of messages is intact ----
        c = h.Client("carol", login=False)
        text = c.read_until("characters): ")
        check("login prompts are plain text", text, "Creating a new account for Carol", absent=(ESC,))
        c.sock.close()

        # ---- delete's message counter still works ----
        a.command("cd /domains/atest")
        a.command("copy note.txt scratch.txt")
        a.send("delete scratch.txt")
        a.read_until("(y/n) ")
        a.send("y")
        check("delete still confirms with colour on", a.read_until("> "), "Deleted /domains/atest/scratch.txt.")
        a.command("copy note.txt scratch.txt")
        a.send("delete scratch.txt")
        a.read_until("(y/n) ")
        b.command("say interrupt")
        a.read_until("interrupt", 5)
        a.send("y")
        check("delete still refuses after another message", a.read_until("> "), "Other messages arrived after the question", "was not deleted")

        # ---- clean shutdown, no driver errors ----
        a.send("shutdown")
        proc.wait(timeout=20)
        log.flush()
        logged = open(os.path.join(work, "driver.log")).read()
        check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:800], "ok")
    finally:
        if proc.poll() is None:
            proc.kill()
        log.close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-3000:])
        shutil.rmtree(work, ignore_errors=True)

    print(f"\n{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
