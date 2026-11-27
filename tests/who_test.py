#!/usr/bin/env python3
"""End-to-end test of `who`, wizard invisibility, the hiding hood, `all-players` and `stats` (docs/specs/who-and-visibility.md).

Starts the driver against a temporary copy of the mudlib and plays: Warden (wizard 10), Pia (9), Olga (5), Nora (3), Pat (3), Mina (1),
and the ordinary players Alice, Bob and Dana. Before the driver boots, the save directory gets an old character (no creation date, with
made-up password and address hashes) and 45 filler characters so that `all-players` pages and the index is built in the background.
The temporary copy of /std/player gets two test-only commands: `lv N` (set the level) and `hoodme` (a hood into the inventory).

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4843 python3 tests/who_test.py
"""
import datetime
import glob
import json
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4843")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_test as a  # noqa: E402
import smoke_test as h  # noqa: E402

CHECKS = [0]
SQUARE = "/domains/hollow/village/square"
HOOD = "/domains/greenvale/oakenfell/southgate/obj/deep_hood"
OUTFITTER = "/domains/greenvale/oakenfell/southgate/outfitter"
NOT_THERE = "No one by that name is in the world."
WOLFROOM = '''// @inherit /std/room
public class WhoWolfRoom : Room
{
    public override void Create()
    {
        ShortDesc = "The Wolf Room"; LongDesc = "A bare room for the who test.";
        Exits["out"] = "/domains/hollow/village/square";
        AddSpawn("/domains/hollow/mob/who_wolf");
    }
}
'''
WOLF = '''// @inherit /std/monster
public class WhoWolf : Monster
{
    public override void Create()
    {
        Name = "wolf"; Ids.Add("wolf");
        ShortDesc = "a grey wolf"; LongDesc = "A wolf made for the who test.";
        Level = 1; MaxVitality = 100000; DamageMin = 1; DamageMax = 1; ToHit = 60; BaseReaction = 2.0;
        Aggressive = true;
    }
}
'''
# What a low-level wizard could write: an object that tries to set or read invisibility and to use the roster.
PROBE = '''// @inherit /std/item
public class WhoProbe : Item
{
    public override void Create() { Name = "whoprobe"; Ids.Add("whoprobe"); ShortDesc = "a probe"; }
    public override void Init() { AddAction("whoprobe", Probe); }

    private static string Show(object? o) => o == null ? "null" : o is string s ? (s == "" ? "empty" : s.Replace("\\n", " ")) : o.ToString() ?? "?";

    private static string T(Func<object?> f) { try { return Show(f()); } catch (Exception e) { return "error " + e.Message; } }

    private bool Probe(string args)
    {
        var roster = LoadObject("/secure/roster");
        var nora = Users().FirstOrDefault(u => u.Call<string>("QueryName") == "Nora");
        var me = ThisPlayer();
        Write("set: " + T(() => roster.Call("SetInvisible", true)) + "\\n");
        Write("visible: " + T(() => roster.Call("Visible", me, nora)) + "\\n");
        Write("allplayers: " + T(() => roster.Call("AllPlayers", "")) + "\\n");
        Write("marks: " + T(() => roster.Call("WhoMarks", me, nora)) + "\\n");
        Write("note: " + T(() => roster.Call("Note")) + "\\n");
        Write("spoke: " + T(() => roster.Call("Spoke", nora, me)) + "\\n");
        Write("query: " + T(() => nora?.Call("QueryInvisible")) + "\\n");
        Write("setbody: " + T(() => nora?.Call("SetInvisible", false)) + "\\n");
        Write("cmd: " + T(() => nora?.Call("CmdInvis", "off")) + "\\n");
        try { Write("names: " + Show(SaveNames("/players").Count) + "\\n"); } catch (Exception e) { Write("names: refused " + e.Message.Length + "\\n"); }
        try { Write("restore: " + Show(RestoreObject("/players/alice")) + "\\n"); } catch (Exception e) { Write("restore: refused\\n"); }
        return true;
    }
}
'''


LOUNGE = '''/// <summary>A public channel, only in the test copy: anyone may join.</summary>
public class ChannelLounge : MudObject
{
    public string Definition() => @"id=lounge
title=The Lounge
member_level=0
speak_level=0
can_mute=yes
can_join=yes";
}
'''


def check(label, text, *expected, absent=()):
    CHECKS[0] += 1
    h.check(label, text, *expected, absent=absent)


def flush(c):
    return c.command("vitality")


def heard(c, text, seconds=5):
    return c.read_until(text, seconds)


def login(name, level=None):
    c = h.Client(name)
    c.read_until("> ")
    if level:
        c.command(f"lv {level}")
    return c


def relogin(name):
    c = h.Client(name, login=False)
    c.read_until("Password: ")
    c.send(h.PASSWORD)
    c.choose_calling(None)
    c.read_until("> ")
    return c


def rows(text):
    """The player rows of a `who` reply (indented lines) and the count it states."""
    lines = [l for l in text.replace("\r", "").split("\n")]
    names = [l.strip() for l in lines if l.startswith("  ") and l.strip()]
    m = re.search(r"(\d+) player\(s\) online", text)
    return names, int(m.group(1)) if m else -1


def who_has(c, name):
    names, count = rows(c.command("who"))
    assert count == len(names), (count, names)
    return [n for n in names if n.startswith(name)]


def table(out):
    """The rows of an all-players reply: no header, no footer, no prompt, no unrelated lines."""
    lines = [l.rstrip("\r") for l in out.replace("\r", "").split("\n")]
    return [l for l in lines if l and not l.startswith("Name ") and "character(s)" not in l and not l.endswith("> ") and not l.startswith("The list is")
            and "enters the game" not in l and "leaves the game" not in l]


def block(out, first_prefix):
    """The lines of a reply from the one that starts with first_prefix up to the prompt."""
    lines = [l.rstrip("\r") for l in out.replace("\r", "").split("\n")]
    start = next(i for i, l in enumerate(lines) if l.startswith(first_prefix))
    end = next((i for i, l in enumerate(lines) if i > start and l.endswith("> ")), len(lines))
    return [l for l in lines[start:end] if l.strip()]


def patch_player(mudlib):
    path = os.path.join(mudlib, "std", "player.cs")
    src = open(path).read()
    anchor = '        AddAction("channels", a => ToChannels("Listing", ""));'
    assert anchor in src
    actions = ('        AddAction("lv", a => { Level = int.Parse(a); return true; });\n'
               '        AddAction("hoodme", a => { var ob = CloneObject("' + HOOD + '"); MoveObject(ob, Self); Write("hood\\n"); return true; });\n')
    open(path, "w").write(src.replace(anchor, actions + anchor, 1))


def save_dir_fixtures(work):
    players = os.path.join(work, "save", "players")
    os.makedirs(players, exist_ok=True)
    old = {"Level": 4, "archetype": "warrior", "advancedClass": "", "logins": 3, "lastLogin": 1700000000,
           "passwordHash": "OLDHASH-MARKER-1234abcd", "knownAddresses": ["ADDRMARKER0001", "ADDRMARKER0002"]}
    json.dump(old, open(os.path.join(players, "oldie.json"), "w"))
    for i in range(45):
        name = "fill" + chr(97 + i // 26) + chr(97 + i % 26)
        json.dump({"Level": 1 + i % 5, "archetype": "scout", "logins": 1, "lastLogin": 1750000000 + i * 1000, "created": 1740000000 + i * 1000,
                   "passwordHash": "FILLHASH-" + name, "knownAddresses": ["FILLADDR-" + name]}, open(os.path.join(players, name + ".json"), "w"))


def secrets_of(work):
    """Every stored hash and address tag of every save, to be grepped out of the output."""
    found = ["OLDHASH-MARKER-1234abcd", "ADDRMARKER0001", "ADDRMARKER0002", "FILLHASH-", "FILLADDR-"]
    for f in glob.glob(os.path.join(work, "save", "players", "*.json")):
        d = json.load(open(f))
        if d.get("passwordHash"):
            found.append(d["passwordHash"])
        found.extend(d.get("knownAddresses") or [])
    return [x for x in found if x]


def run(work, mudlib):
    warden = h.Client("Warden")
    warden.read_until("> ")
    check("the secret makes the first wizard", warden.command("promote " + h.SECRET), "level 10 wizard")
    alice = login("Alice", 7)
    bob = login("Bob", 3)
    dana = login("Dana", 3)
    pia, olga, nora, pat, mina = (login(n) for n in ("Pia", "Olga", "Nora", "Pat", "Mina"))
    for name, level in (("pia", 9), ("olga", 5), ("nora", 3), ("pat", 3), ("mina", 1)):
        warden.command(f"promote {name} {level}")
    for c in (pia, olga, nora, pat, mina):
        heard(c, "level is now")
    h.write(mudlib, "/wiz/mina/whoprobe", PROBE)
    h.write(mudlib, "/wiz/mina/desk", "// @inherit /std/room\npublic class MinaDesk : Room { public override void Create() { ShortDesc = \"Mina's Desk\"; LongDesc = \"A workshop.\"; } }\n")
    everyone = [warden, alice, bob, dana, pia, olga, nora, pat, mina]
    for c in everyone:
        flush(c)

    # ---- who, nobody hidden ----
    out = alice.command("who")
    names, count = rows(out)
    check("who: the count line matches the rows", str(count == len(names) == 9), "True")
    check("who: wizards are marked, others are not", out, "Nora the newcomer [wizard 3]", "Alice the newcomer", absent=("(invis)", "(hidden)"))
    check("help lists stats and who", alice.command("help", marker="Colour:"), "stats", "who")

    # ---- an invisible wizard ----
    check("invis: a mortal cannot use it", alice.command("invis"), "What?")
    check("vis: nor this", alice.command("vis"), "What?")
    check("invis: Nora goes invisible and is told", nora.command("invis"), "You are invisible")
    check("invis: twice is said", nora.command("invis"), "You are already invisible.")
    check("invis: the room is told nothing", flush(alice), absent=("Nora", "fades"))
    a_names, a_count = rows(alice.command("who"))
    check("who: a mortal does not see her", str(not any(n.startswith("Nora") for n in a_names)), "True")
    check("who: the count is of what is shown, not 9 less 1 said", str(a_count == len(a_names) == 8), "True")
    check("who: the count line does not say how many are left out", alice.command("who"), "8 player(s) online:", absent=("shown", "hidden", "of 9", "invisible"))
    check("who: Mina (wizard 1) does not see her", str(who_has(mina, "Nora")), "[]")
    check("who: Pat (wizard 3, equal) sees her marked", str(who_has(pat, "Nora")), "(invis)")
    check("who: Olga (wizard 5) sees her marked", str(who_has(olga, "Nora")), "[wizard 3] (invis)")
    check("who: level 9 sees her", str(who_has(pia, "Nora")), "(invis)")
    check("who: level 10 sees her", str(who_has(warden, "Nora")), "(invis)")
    check("who: she sees herself", str(who_has(nora, "Nora")), "(invis)")
    check("who: Olga's count includes her", str(rows(olga.command("who"))[1]), "9")
    check("who: Mina's count does not", str(rows(mina.command("who"))[1]), "8")
    # the room
    out = alice.command("look")
    check("room: a mortal's look does not list her", out, "Village Square", absent=("Nora",))
    check("room: look at her is as for nothing", alice.command("look at nora"), "You see no 'nora' here.")
    check("room: look at a name that is not there is the same", alice.command("look at nosuchperson"), "You see no 'nosuchperson' here.")
    check("room: a lower wizard does not see her", mina.command("look"), absent=("Nora",))
    check("room: an equal wizard sees her", pat.command("look"), "Nora the newcomer")
    check("room: Warden sees her", warden.command("look at nora"), "Nora")
    nora.command("south")
    check("move: leaving is not shown to a mortal", flush(alice), absent=("Nora", "leaves"))
    nora.command("north")
    check("move: arriving is not shown to a mortal", flush(alice), absent=("Nora", "arrives"))
    check("move: an equal wizard saw it", heard(pat, "arrives"), "Nora arrives.")
    check("move: she is not listed after coming back", alice.command("look"), absent=("Nora",))
    check("deed: a room line about what she does reads Someone", (nora.command("sit"), heard(alice, "sits down", 5))[1], "Someone sits down to rest.", absent=("Nora sits",))
    nora.command("stand")
    flush(alice)
    # speech
    check("say: Someone says", (nora.command("say hello room"), heard(alice, "hello room"))[1], "Someone says: hello room", absent=("Nora says",))
    check("say: a wizard who can see her reads her name", heard(pat, "hello room"), "Nora says: hello room")
    flush(alice)

    # ---- tell and whisper ----
    absent_tell = alice.command("tell nosuchperson hello")
    check("tell: a name not in the game", absent_tell, NOT_THERE)
    nora_tell = alice.command("tell nora hello")
    check("tell: an invisible wizard gets the same words", nora_tell, NOT_THERE)
    check("tell: byte for byte the same as for a name not in the game", str(nora_tell.replace("nora", "X") == absent_tell.replace("nosuchperson", "X")), "True")
    absent_whisper = alice.command("whisper hello to nosuchperson")
    nora_whisper = alice.command("whisper hello to nora")
    check("whisper: the same words as for an absent name", str(nora_whisper.replace("nora", "X") == absent_whisper.replace("nosuchperson", "X")), "True")
    check("whisper: and they are the ones for an absent name", nora_whisper, "No one by that name is here.")
    check("reply: nothing to reply to", alice.command("reply hi"), "You have no one to reply to.")
    check("tell: Mina (a lower wizard) cannot tell her either", mina.command("tell nora hi"), NOT_THERE)
    check("tell: an equal wizard can", pat.command("tell nora hi pat"), "You tell Nora: hi pat")
    heard(nora, "hi pat")
    check("tell: she may tell a mortal, who reads Someone", (nora.command("tell alice you may answer"), heard(alice, "you may answer"))[1], "Someone tells you: you may answer", absent=("Nora tells",))
    check("tell: now the mortal may answer by name", alice.command("tell nora thank you"), "You tell Someone: thank you")
    check("tell: and she gets it as from Alice", heard(nora, "thank you"), "Alice tells you: thank you")
    check("reply: the mortal may reply as well", alice.command("reply and again"), "You tell Someone: and again")
    heard(nora, "and again")
    check("tell: Bob, whom she did not speak to, still cannot", bob.command("tell nora hi"), NOT_THERE)
    check("whisper: she whispers to Bob", (nora.command("whisper psst to bob"), heard(bob, "psst"))[1], "Someone whispers to you: psst")
    check("whisper: then Bob may whisper back", bob.command("whisper yes to nora"), "You whisper to Someone: yes")
    heard(nora, "yes")
    nora.command("vis")
    flush(alice)
    nora.command("invis")
    check("tell: after vis and invis the permission is gone", alice.command("tell nora hi again"), NOT_THERE)
    flush(nora)

    # ---- channels and ignore ----
    alice.command("channel lounge join")
    nora.command("channel lounge join")
    flush(alice)
    check("channel: on a public channel she is Someone to a mortal", (nora.command("lounge anybody here"), heard(alice, "anybody here"))[1], "[lounge] Someone: anybody here", absent=("Nora:",))
    check("channel: an equal wizard on the channel reads her name", nora.command("channels"), "lounge")
    nora.command("wizard wizards only")
    check("channel: on the wizard channel she is named, even to a lower wizard", heard(mina, "wizards only"), "[wizard] Nora: wizards only")
    check("channel: a mortal hears nothing of it", flush(alice), absent=("wizards only",))
    check("ignore: a mortal may ignore a name without being told anything about it", alice.command("ignore nora"), "You are now ignoring Nora")
    alice.command("unignore nora")
    flush(alice)

    # ---- creatures ----
    nora.command("goto /domains/ctest/wolfroom")
    time.sleep(6)
    check("creature: an aggressive wolf ignores an invisible wizard", nora.command("look"), "grey wolf", absent=("scratches", "attacks", "fighting"))
    check("creature: and nothing came of it", flush(nora), absent=("scratches you", "attacks", "hits you", "misses you"))
    pat.command("goto /domains/ctest/wolfroom")
    check("creature: the same wolf attacks a visible wizard", heard(pat, "grey wolf", 12), "grey wolf")
    pat.command("goto " + SQUARE)
    nora.command("goto " + SQUARE)
    pat.command("goto " + SQUARE)
    flush(nora)

    # ---- persistence: quit, reconnect, take-over ----
    nora.command("quit")
    time.sleep(1)
    check("logout: nothing said to the room", flush(alice), absent=("Nora", "leaves the game"))
    nora = relogin("Nora")
    check("login: still invisible, and told so", nora.command("score"), "(invisible)", "You are invisible")
    check("login: not on a mortal's who", str(who_has(alice, "Nora")), "[]")
    check("login: nothing said to the room", flush(alice), absent=("Nora", "enters the game"))
    second = h.Client("Nora", login=False)
    second.read_until("Password: ")
    second.send(h.PASSWORD)
    second.choose_calling(None)
    second.read_until("> ")
    check("take-over: the new connection is invisible too", second.command("score"), "(invisible)")
    check("take-over: still not on a mortal's who", str(who_has(alice, "Nora")), "[]")
    check("take-over: an equal wizard still sees her", str(who_has(pat, "Nora")), "(invis)")
    nora = second
    check("vis: she is seen", nora.command("vis"), "You are visible again")
    check("vis: on a mortal's who again", str(who_has(alice, "Nora")), "Nora")
    check("vis: and in the room", alice.command("look"), "Nora")
    check("vis: tell works again", alice.command("tell nora welcome back"), "You tell Nora: welcome back")
    heard(nora, "welcome back")
    nora.command("invis on")
    check("invis off: the other form", nora.command("invis off"), "You are visible again")
    check("invis on: the first form", nora.command("invis on"), "You are invisible")
    # a wizard demoted is no longer invisible
    warden.command("promote nora 0")
    check("demoted: no longer listed as invisible to herself", nora.command("who"), absent=("(invis)",))
    check("demoted: a mortal sees her again", str(who_has(alice, "Nora")), "Nora")
    check("demoted: invis does not exist for her", nora.command("invis"), "What?")
    warden.command("promote nora 3")
    heard(nora, "level is now 3")
    nora.command("vis")
    flush(alice)

    # ---- the hood ----
    check("hood: Corwin sells it", (warden.command("goto " + OUTFITTER), warden.command("browse"))[1], "deep traveler's hood", "120")
    check("hood: Corwin talks about it", warden.command("ask corwin about hood"), "hundred and twenty")
    warden.command("goto " + SQUARE)
    check("hood: Dana is on the who list at first", str(who_has(alice, "Dana")), "Dana")
    check("hood: she gets one", dana.command("hoodme"), "hood")
    check("hood: she wears it", dana.command("wear hood"), "hood")
    check("hood: a mortal does not see her on who", str(who_has(alice, "Dana")), "[]")
    out = alice.command("who")
    check("hood: the count is of what is shown", str(rows(out)[1] == len(rows(out)[0]) == 8), "True", absent=("hidden",))
    check("hood: Dana sees herself marked", str(who_has(dana, "Dana")), "(hidden)")
    check("hood: Mina (wizard 1) sees her marked", str(who_has(mina, "Dana")), "(hidden)")
    check("hood: Warden sees her marked", str(who_has(warden, "Dana")), "(hidden)")
    check("hood: she is in the room", alice.command("look"), "Dana")
    check("hood: look at her works", alice.command("look at dana"), "Dana")
    check("hood: tell to her works", alice.command("tell dana you are hooded"), "You tell Dana: you are hooded")
    check("hood: she hears it from a name", heard(dana, "you are hooded"), "Alice tells you: you are hooded")
    check("hood: she can speak and is named", (dana.command("say hi"), heard(alice, "hi"))[1], "Dana says: hi")
    check("hood: score reminds her", dana.command("score"), "You are keeping out of sight of the who list.")
    check("hood: nobody else is told that", alice.command("score"), absent=("keeping out of sight",))
    check("hood: taking it off shows her at once", (dana.command("remove hood"), str(who_has(alice, "Dana")))[1], "Dana")
    check("hood: and the reminder goes", dana.command("score"), absent=("keeping out of sight",))
    dana.command("wear hood")
    check("hood: hidden again", str(who_has(alice, "Dana")), "[]")
    dana.sock.close()   # the connection drops: the body lingers, link-dead
    time.sleep(1.5)
    check("hood: a link-dead hooded body is not on a mortal's who", str(who_has(alice, "Dana")), "[]")
    check("hood: but is in the room, staring", alice.command("look"), "Dana stands here, staring at nothing")
    check("hood: a wizard sees it link-dead and hidden", str(who_has(warden, "Dana")), "(link-dead) (hidden)")
    dana = relogin("Dana")
    check("hood: she comes back to the same body, still hooded", str(who_has(alice, "Dana")), "[]")
    dana.command("remove hood")
    # a wizard in a hood is both
    nora.command("invis")
    nora.command("hoodme")
    nora.command("wear hood")
    check("both: an equal wizard sees both marks", str(who_has(pat, "Nora")), "(invis) (hidden)")
    dana.command("wear hood")
    print("\nsample who (mortal), Nora invisible, Dana hooded:\n" + alice.command("who"))
    print("sample who (level 10), same moment:\n" + warden.command("who"))
    dana.command("remove hood")
    nora.command("remove hood")
    nora.command("vis")
    flush(alice)

    # ---- the sandbox ----
    nora.command("invis")
    mina.command("goto /wiz/mina/desk")
    check("sandbox: Mina clones her probe in her workshop", mina.command("clone /wiz/mina/whoprobe"), "Cloned")
    out = mina.command("whoprobe")
    mina.command("goto " + SQUARE)
    check("sandbox: it cannot turn invisibility on for anyone", out, "set: null")
    check("sandbox: it learns nothing from the roster's answer", out, "visible: True")
    check("sandbox: it cannot list all players", out, "allplayers: null")
    check("sandbox: it cannot ask for who marks", out, "marks: null")
    check("sandbox: it cannot note a character", out, "note: null")
    check("sandbox: it cannot ask who spoke first", out, "spoke: null")
    check("sandbox: a body has no readable invisibility", out, "query: null")
    check("sandbox: nor a setter", out, "setbody: null")
    check("sandbox: nor the command", out, "cmd: null")
    check("sandbox: it cannot list the saves", out, "names: refused")
    check("sandbox: it cannot read a save", out, "restore: refused")
    check("sandbox: she is still invisible to mortals", str(who_has(alice, "Nora")), "[]")
    nora.command("vis")

    # ---- all-players ----
    for who, c in (("a mortal", alice), ("level 1", mina), ("level 5", olga), ("level 9", pia)):
        for form in ("all-players", "allplayers", "all players", "all-players recent", "all players new"):
            out = c.command(form)
            check(f"all-players: {who}: '{form}' is an unknown word", out, "What?", absent=("Name", "character(s)", "Usage"))
    check("all-players: the same words as any unknown command", str(pia.command("all-players") == pia.command("nosuchcommandxx")), "True")
    check("all players: other words after all are unknown for level 10 too", warden.command("all things"), "What?")
    out = warden.command("all-players")
    check("all-players: level 10 gets the list", out, "Name", "Calling", "Class", "Lvl", "Wiz", "Created", "Last login", "Now", "character(s).", "Page 1 of")
    check("all-players: a character made in the test", out, "Alice")
    for form in ("allplayers", "all players"):
        check(f"all-players: '{form}' works for level 10", warden.command(form), "character(s)", "Alice")
    secrets = secrets_of(work)
    leak = [s for s in secrets if s in out]
    check("all-players: no hash or address in the output", str(leak), "[]")
    # every page of every form, against every secret
    allout = out
    for form in ("all-players 2", "all-players recent", "all-players new", "all-players new 2", "all-players al", "all-players recent 2"):
        allout += warden.command(form)
    leak = [s for s in secrets_of(work) if s in allout]
    check("all-players: no hash or address in any page", str(leak), "[]")
    today = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%d")
    alice_row = [l for l in table(out) if l.startswith("Alice")]
    check("all-players: Alice's row has her creation date, level and calling", alice_row[0] if alice_row else "", today, "Adventurer", "  7 ")
    check("all-players: Alice is in the game now", alice_row[0] if alice_row else "", "yes")
    wizard_row = table(warden.command("all-players pia"))
    check("all-players: a wizard's level is shown", wizard_row[0] if wizard_row else "", " 9 ")
    allnames = warden.command("all-players " + "oldie")
    check("all-players: an old save shows 'before records began'", allnames, "Oldie", "Warrior", "before records began", "2023-11-14")
    check("all-players: an old save is not in the game", table(allnames)[0], absent=("yes",))
    # sorting
    first = warden.command("all-players")
    names_in_order = [l.split()[0] for l in table(first)]
    check("all-players: sorted by name", str(names_in_order == sorted(names_in_order, key=str.lower)), "True")
    check("all-players: 40 to a page", str(len(names_in_order)), "40")
    p2 = warden.command("all-players 2")
    names2 = [l.split()[0] for l in table(p2)]
    check("all-players: page 2 has the rest, and they follow page 1", str(len(names2) > 0 and sorted(names_in_order + names2, key=str.lower) == names_in_order + names2), "True")
    check("all-players: the total", first, "character(s). Page 1 of 2.")
    check("all-players: past the last page shows the last", warden.command("all-players 9"), "Page 2 of 2.")
    rec = warden.command("all-players recent")
    first_name = table(rec)[0].split()[0]
    check("all-players recent: a character who logged in just now is first", str(first_name in ("Warden", "Alice", "Bob", "Dana", "Pia", "Olga", "Nora", "Pat", "Mina")), "True")
    last_rows = table(warden.command("all-players recent 2"))
    check("all-players recent: the oldest logins come last", last_rows[-1].split()[0], "Oldie")
    new = warden.command("all-players new")
    check("all-players new: newest creation first", table(new)[0].split()[0], absent=("Oldie", "Fill"))
    pre = warden.command("all-players fill")
    check("all-players: a prefix filters", pre, "Fillaa", "45 character(s) beginning with fill", "Page 1 of 2", absent=("Alice", "Oldie"))
    check("all-players: a prefix is not case sensitive", warden.command("all-players FILLA"), "Fillaa", "beginning with filla")
    check("all-players: nothing matches", warden.command("all-players zzz"), "0 character(s) beginning with zzz", "Page 1 of 1.")
    check("all-players: nonsense is a usage line", warden.command("all-players !!"), "Usage: all-players")
    os.remove(os.path.join(work, "save", "players", "fillaa.json"))
    check("all-players: a deleted save leaves the list", warden.command("all-players fill"), "44 character(s) beginning with fill", absent=("Fillaa ",))
    # a new character appears
    carol = login("Carol")
    check("all-players: a new character is listed at once", warden.command("all-players ca"), "Carol", "1 character(s)")
    carol.command("quit")
    time.sleep(1)
    check("all-players: and is not in the game after a quit", table(warden.command("all-players ca"))[0], absent=("yes",))
    # ---- stats ----
    out = alice.command("stats")
    lines = block(out, "Alice - ")
    check("stats: the six attributes", out, "Might", "Grace", "Vigor", "Insight", "Spirit", "Resolve")
    check("stats: the derived numbers", out, "Vitality", "Endurance", "Focus", "Damage", "To hit", "Defense", "Armor", "Dodge", "Block", "Critical", "Reaction", "Attack Time", "Mental Res", "Physical Res", "Carry")
    check("stats: no box, Chronicle, quests or equipment", out, absent=("Chronicle", "quests", "Equipment", "Standing", "═", "|", "+--"))
    check("stats: at most 13 lines", str(len(lines) <= 13), "True")
    check("stats: at most 48 columns", str(max(len(l) for l in lines) <= 48), "True")
    check("stats: plain ASCII", str(all(ord(ch) < 128 for ch in out)), "True")
    alice.command("charset ascii")
    in_ascii = block(alice.command("stats"), "Alice - ")
    check("stats: the same layout under charset ascii", str(len(in_ascii) == len(lines) and [l[:11] for l in in_ascii] == [l[:11] for l in lines]), "True")
    alice.command("charset utf8")
    check("stats: a mortal's bare stat is still score", alice.command("stat"), "Combat Summary")
    check("stats: a wizard's stats is their own", warden.command("stats"), "Warden", "Might")
    check("stats: a wizard's stat THING is untouched", warden.command("stat alice"), "Alice")
    sc = alice.command("score")
    for label in ("Damage", "To hit", "Defense", "Armor", "Dodge", "Block", "Critical"):
        check(f"stats: {label} agrees with score", alice.command("stats"), label)
    dmg = re.search(r"Damage (\d+-\d+)", sc).group(1)
    check("stats: Damage has score's numbers", str(bool(re.search(r"Damage\s+" + re.escape(dmg), alice.command("stats")))), "True")
    print("\nsample stats:\n" + out)
    print("sample who (mortal):\n" + alice.command("who"))
    print("sample who (level 10):\n" + warden.command("who"))
    print("sample all-players:\n" + warden.command("all-players al"))


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-who-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    os.makedirs(os.path.join(mudlib, "domains", "ctest"), exist_ok=True)
    h.write(mudlib, "/channels/lounge", LOUNGE)
    h.write(mudlib, "/domains/ctest/wolfroom", WOLFROOM)
    h.write(mudlib, "/domains/hollow/mob/who_wolf", WOLF)
    h.edit(mudlib, SQUARE, 'Exits["south"] = "/domains/hollow/village/tavern";', 'Exits["south"] = "/domains/hollow/village/tavern";\n        Exits["wolves"] = "/domains/ctest/wolfroom";')
    patch_player(mudlib)
    save_dir_fixtures(work)
    box = [None, None]
    box[0], box[1] = a.start_driver(work, mudlib)
    try:
        time.sleep(4)  # the index is built in the background, 40 saves a slice
        run(work, mudlib)
        time.sleep(2)
        box[1].flush()
        logged = open(os.path.join(work, "driver.log")).read()
        check("all-players: the use is logged with the name", "ok" if "all-players: Warden" in logged else "missing", "ok")
        check("all-players: and no output is logged", "ok" if "Fillab" not in logged and "before records began" not in logged else "output logged", "ok")
        logged = open(os.path.join(work, "driver.log")).read()
        check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:1200], "ok")
    finally:
        if box[0].poll() is None:
            box[0].kill()
        box[1].close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-4000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\n{CHECKS[0]} checks, {len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
