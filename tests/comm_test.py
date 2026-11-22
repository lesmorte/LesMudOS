#!/usr/bin/env python3
"""End-to-end test of the communication commands (docs/specs/communication.md): say, tell, reply, whisper, shout, channels,
ignore, the room blocks, the flood limits, text cleaning, and the security of the channel service.

Starts the driver against a temporary copy of the mudlib and plays seven characters over TCP: Warden (wizard level 10),
Mina (wizard level 1), and the ordinary players Alice, Bob, Carol and Dana (Alice stays at character level 1; the others are
raised to level 3 by a test-only command in the temporary copy of /std/player). The temporary copy also gets two rooms (one
with NoTell, one with NoShout), a public channel that can be joined and is flood limited, and a probe object written the way
a low-level wizard could write it, which tries to post to `sysop` and to act for other players.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4832 python3 tests/comm_test.py
"""
import os
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4832")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_test as a  # noqa: E402
import smoke_test as h  # noqa: E402

CHECKS = [0]


def check(label, text, *expected, absent=()):
    CHECKS[0] += 1
    h.check(label, text, *expected, absent=absent)


def flush(c):
    """Everything sent to this character so far, by a round trip (the server answers in order)."""
    return c.command("vitality")


def heard(c, text, seconds=5):
    return c.read_until(text, seconds)


def none_of(c, *texts):
    """What the character received up to now contains none of the texts."""
    return flush(c)


SQUARE = "/domains/hollow/village/square"
QUIET = '''// @inherit /std/room
public class QuietRoom : Room
{
    public override void Create()
    {
        ShortDesc = "The Quiet Room"; LongDesc = "Nothing leaves this room by tell.";
        NoTell = true;
        Exits["out"] = "/domains/hollow/village/square";
    }
}
'''
HUSH = '''// @inherit /std/room
public class HushRoom : Room
{
    public override void Create()
    {
        ShortDesc = "The Hush Room"; LongDesc = "Shouts neither leave nor enter.";
        NoShout = true;
        Exits["out"] = "/domains/hollow/village/square";
    }
}
'''
OTHER = '''// @inherit /std/room
public class OtherRoom : Room
{
    public override void Create()
    {
        ShortDesc = "The Other Room"; LongDesc = "An ordinary room.";
        Exits["out"] = "/domains/hollow/village/square";
    }
}
'''
LOUNGE = '''/// <summary>A public channel, only in the test copy: anyone may join and leave, and members may switch it off.</summary>
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
# What a low-level wizard could write: an object that tries to use the channel service and the communication service.
POSTER = '''// @inherit /std/item
public class Poster : Item
{
    public override void Create() { Name = "poster"; }
    public override void Init() { AddAction("post", Post); AddAction("probe", Probe); AddAction("forgeplayer", ForgePlayer); }

    private static string Show(object? o) => o == null ? "null" : o is string s ? (s == "" ? "empty" : s.Replace("\\n", " ")) : o.ToString() ?? "?";

    private bool Post(string args)
    {
        var c = LoadObject("/secure/channels");
        Write("send-sysop: " + Show(c.Call("Send", "sysop", "hacked by poster")) + "\\n");
        Write("send-wizard: " + Show(c.Call("Send", "wizard", "hacked wizard by poster")) + "\\n");
        Write("ismember: " + Show(c.Call("IsMember", "wizard")) + "\\n");
        Write("verbs: " + Show(c.Call<List<string>>("QueryVerbs")?.Count) + "\\n");
        Write("listing: " + Show(c.Call("Listing")) + "\\n");
        Write("switch: " + Show(c.Call("Switch", "wizard off")) + "\\n");
        return true;
    }

    private bool Probe(string args)
    {
        var m = LoadObject("/secure/comm");
        Write("say: " + Show(m.Call("DoSay", "said by poster")) + "\\n");
        Write("tell: " + Show(m.Call("DoTell", "alice told by poster")) + "\\n");
        Write("shout: " + Show(m.Call("DoShout", "shouted by poster")) + "\\n");
        var me = ThisPlayer();
        if (me != null)
        {
            Write("ignores: " + Show(me.Call("QueryIgnores", "alice")) + "\\n");
            me.Call("SetChannelOff", "wizard", true);
            me.Call("SetShoutOn", false);
            me.Call("SetLastTeller", "alice");
            Write("lastteller: " + Show(me.Call("QueryLastTeller")) + "\\n");
        }
        return true;
    }

    private bool ForgePlayer(string args)
    {
        // A fresh body is not a connected player; it cannot speak through the service either.
        var fake = CloneObject("/std/player");
        Write("fake: " + Show(LoadObject("/secure/channels").Call("Send", "sysop", "fake")) + "\\n");
        return true;
    }
}
'''
TEST_PLAYER_ADDITIONS = '''
    // test only: raise the level, vanish, become idle
    private bool vanished;
    private bool testIdle;
    public bool QueryInvisible() => vanished;
'''
TEST_IDLE = '''    public bool QueryIdle() => testIdle;
'''


def patch_player(mudlib):
    path = os.path.join(mudlib, "std", "player.cs")
    src = open(path).read()
    anchor = '        AddAction("channels", a => ToChannels("Listing", ""));'
    assert anchor in src
    actions = ('        AddAction("lv", a => { Level = int.Parse(a); return true; });\n'
               '        AddAction("vanish", a => { vanished = !vanished; Write(vanished ? "vanished\\n" : "visible\\n"); return true; });\n'
               '        AddAction("idleme", a => { testIdle = !testIdle; Write(testIdle ? "idle\\n" : "active\\n"); return true; });\n')
    src = src.replace(anchor, actions + anchor, 1)
    add = TEST_PLAYER_ADDITIONS
    real = "    public override bool QueryIdle() => "
    if real in src:
        src = src.replace(real, real + "testIdle || ", 1)   # the game's own idle timer, plus the test's switch
    else:
        add += TEST_IDLE
    marker = "    private string lastTeller = \"\";\n"
    assert marker in src
    src = src.replace(marker, marker + add, 1)
    open(path, "w").write(src)


def login(name, level=None):
    c = h.Client(name)
    c.read_until("> ")
    if level:
        c.command(f"lv {level}")
    return c


def pace(seconds):
    time.sleep(seconds)


def run(work, mudlib):
    # ---- the characters ----
    warden = h.Client("Warden")
    warden.read_until("> ")
    check("the secret makes the first wizard", warden.command("promote " + h.SECRET), "level 10 wizard")
    alice = login("Alice")
    bob = login("Bob", 3)
    carol = login("Carol", 3)
    dana = login("Dana", 3)
    mina = login("Mina")
    for c in (warden, alice, bob, carol, dana, mina):
        flush(c)
    check("Mina becomes a level 1 wizard", warden.command("promote mina 1"), "mina is now wizard level 1")
    heard(mina, "level is now 1")
    h.write(mudlib, "/wiz/mina/poster", POSTER)
    check("Mina clones her probe", mina.command("clone /wiz/mina/poster"), "Cloned /wiz/mina/poster#")
    for c in (warden, alice, bob, carol, dana, mina):
        flush(c)

    # ---- say ----
    check("say: the speaker sees You say", alice.command("say hello all"), "You say: hello all")
    check("say: the room hears it", heard(bob, "Alice says: hello all"), "Alice says: hello all")
    check("say: the whole room hears it", heard(dana, "Alice says: hello all"), "Alice says: hello all")
    check("say: the shortcut ' with a space", alice.command("' by the way"), "You say: by the way")
    check("say: the shortcut ' without a space", alice.command("'by the way"), "You say: by the way")
    check("say: shortcut heard", heard(bob, "by the way"), "Alice says: by the way")
    check("say: empty gives the usage line", alice.command("say"), "Usage: say MESSAGE")
    check("say: blanks only give the usage line", alice.command("say     "), "Usage: say MESSAGE")
    flush(alice)
    bob.command("other")
    flush(bob)
    bob.command("say anyone here?")
    check("say: another room does not hear it", flush(alice) + flush(carol), absent=("anyone here",))
    bob.command("out")
    flush(bob)
    flush(alice)

    # ---- tell, reply ----
    check("tell: the sender sees You tell", alice.command("tell bob how are you"), "You tell Bob: how are you")
    check("tell: the target sees it", heard(bob, "tells you"), "Alice tells you: how are you")
    check("tell: names are not case sensitive", alice.command("tell BOB again"), "You tell Bob: again")
    heard(bob, "again")
    check("tell: works from another room", bob.command("tell alice I am elsewhere"), "You tell Alice: I am elsewhere")
    heard(alice, "elsewhere")
    check("reply: answers the last teller", bob.command("reply thanks"), "You tell Alice: thanks")
    check("reply: the answer arrives", heard(alice, "Bob tells you: thanks"), "Bob tells you: thanks")
    check("reply: nobody to reply to", carol.command("reply hi"), "You have no one to reply to.")
    check("reply: empty gives the usage", bob.command("reply"), "Usage: reply MESSAGE")
    check("tell: no message gives the usage", alice.command("tell bob"), "Usage: tell PLAYER MESSAGE")
    check("tell: nothing gives the usage", alice.command("tell"), "Usage: tell PLAYER MESSAGE")
    check("tell: yourself is refused", alice.command("tell alice hi"), "You cannot tell yourself anything.")
    absent_text = alice.command("tell nosuchperson hello")
    check("tell: no such name", absent_text, "No one by that name is in the world.")
    carol.command("quit")
    pace(1)
    offline_text = alice.command("tell carol hello")
    check("tell: a character who is not logged in gives the same answer", offline_text, "No one by that name is in the world.")
    line = lambda t: [l for l in t.split("\r\n") if "No one by that name" in l]
    check("tell: the same answer word for word", str(line(offline_text) == line(absent_text) != []), "True")
    carol = login("Carol", 3)
    flush(carol)
    flush(alice)

    # ---- ignore ----
    check("ignore: the list is empty at first", bob.command("ignore"), "You are not ignoring anyone.")
    check("ignore: add", bob.command("ignore alice"), "You are now ignoring Alice")
    check("ignore: the list", bob.command("ignore"), "You are ignoring: Alice")
    check("ignore: yourself is refused", bob.command("ignore bob"), "You cannot ignore yourself.")
    check("ignore: twice is said", bob.command("ignore Alice"), "You are already ignoring Alice.")
    check("ignore: a tell is not delivered, and the sender is told only that", alice.command("tell bob can you hear me"), "Bob is not listening to you.", absent=("You tell",))
    check("ignore: the target heard nothing", flush(bob), absent=("can you hear me",))
    check("ignore: a whisper is not delivered either", alice.command("whisper secret to bob"), "Bob is not listening to you.")
    check("ignore: the target heard no whisper", flush(bob), absent=("secret", "whispers to you"))
    check("ignore: others still hear", alice.command("tell carol hello carol"), "You tell Carol: hello carol")
    heard(carol, "hello carol")
    check("ignore: unignore", bob.command("unignore alice"), "You no longer ignore Alice.")
    check("ignore: unignore twice", bob.command("unignore alice"), "You are not ignoring Alice.")
    check("ignore: a tell is delivered again", alice.command("tell bob now?"), "You tell Bob: now?")
    check("ignore: delivered", heard(bob, "now?"), "Alice tells you: now?")
    bob.command("ignore alice")

    # ---- whisper ----
    bob.command("unignore alice")
    flush(bob)
    flush(carol)
    check("whisper: message to name", alice.command("whisper meet me later to bob"), "You whisper to Bob: meet me later")
    check("whisper: the target sees the words", heard(bob, "whispers to you"), "Alice whispers to you: meet me later")
    check("whisper: bystanders see no words", heard(dana, "whispers something"), "Alice whispers something to Bob.", absent=("meet me later",))
    check("whisper: bystanders (second one)", heard(carol, "whispers something"), "Alice whispers something to Bob.", absent=("meet me later",))
    check("whisper: the form name message", alice.command("whisper bob the other form"), "You whisper to Bob: the other form")
    check("whisper: other form delivered", heard(bob, "the other form"), "Alice whispers to you: the other form")
    heard(dana, "whispers something")
    heard(carol, "whispers something")
    check("whisper: a message with the word to in it", alice.command("whisper I want to go home to bob"), "You whisper to Bob: I want to go home")
    heard(bob, "go home")
    check("whisper: nobody else saw the words", flush(dana), absent=("go home", "want to"))
    heard(carol, "whispers something")
    bob.command("other")
    check("whisper: the target must be in the same room", alice.command("whisper hello to bob"), "No one by that name is here.")
    check("whisper: nothing leaked to the other room", flush(bob), absent=("hello",))
    bob.command("out")
    flush(bob)
    check("whisper: no such name", alice.command("whisper hello to nobodyatall"), "No one by that name is here.")
    check("whisper: nothing given", alice.command("whisper"), "Usage: whisper")
    check("whisper: no message", alice.command("whisper to bob"), "Usage: whisper")
    check("whisper: yourself is refused", alice.command("whisper hi to alice"), "No one by that name is here.")
    flush(bob)

    # ---- shout ----
    check("shout: level 1 characters cannot, and are told when they can", alice.command("shout hello world"), "You cannot shout until you reach level 2.")
    check("shout: nobody heard it", flush(bob), absent=("hello world",))
    alice.command("lv 3")
    check("shout: the speaker sees You shout", alice.command("shout hello world"), "You shout: hello world")
    check("shout: everyone hears it", heard(bob, "shouts"), "Alice shouts: hello world")
    check("shout: even in another room", heard(dana, "shouts"), "Alice shouts: hello world")
    heard(carol, "shouts")
    heard(mina, "shouts")
    heard(warden, "shouts")
    check("shout: a second shout at once is refused", alice.command("shout again"), "before you shout again")
    check("shout: nobody heard the refused one", flush(bob), absent=("again",))
    check("shout: wizards are exempt from the interval", (mina.command("shout one"), mina.command("shout two"))[1], "You shout: two")
    heard(bob, "shouts")
    heard(alice, "Mina shouts: two")
    bob.command("ignore mina")
    mina.command("shout three")
    heard(carol, "Mina shouts: three")
    check("shout: an ignored player's shouts are not shown", flush(bob), absent=("three",))
    bob.command("unignore mina")
    check("shout: empty gives the usage", bob.command("shout"), "Usage: shout MESSAGE")
    check("shout off: stops hearing", bob.command("shout off"), "You will not hear shouts.")
    mina.command("shout while bob is deaf")
    heard(carol, "while bob is deaf")
    check("shout off: bob heard nothing", flush(bob), absent=("while bob is deaf",))
    check("shout off: bob may still shout", bob.command("shout i can still speak"), "You shout: i can still speak")
    heard(carol, "i can still speak")
    check("shout on: hears again", bob.command("shout on"), "You will hear shouts again.")
    mina.command("shout now bob hears")
    check("shout on: bob hears", heard(bob, "now bob hears"), "Mina shouts: now bob hears")
    # the setting is saved with the character
    bob.command("shout off")
    bob.command("quit")
    pace(1)
    bob = h.Client("Bob")
    bob.read_until("Password: ")
    bob.send(h.PASSWORD)
    bob.read_until("> ")
    mina.command("shout bob is back")
    heard(carol, "bob is back")
    check("shout off: saved over a new login", flush(bob), absent=("bob is back",))
    bob.command("shout on")
    heard(alice, "while", 0.2)
    flush(alice)
    flush(carol)

    # ---- the interval ends ----
    pace(10)
    check("shout: after the interval it works again", alice.command("shout once more"), "You shout: once more")
    heard(carol, "once more")

    # ---- room blocks ----
    check("NoTell: a player walks in", bob.command("quiet"), "The Quiet Room")
    flush(carol)
    check("NoTell: nothing goes out by tell", bob.command("tell carol are you there"), "Nothing you say here can be sent to anyone far away.")
    check("NoTell: carol heard nothing", flush(carol), absent=("are you there",))
    check("NoTell: nothing comes in by tell", carol.command("tell bob hello bob"), "Bob cannot be reached by tell just now.")
    check("NoTell: bob heard nothing", flush(bob), absent=("hello bob",))
    check("NoTell: a wizard is not bound when sending in", mina.command("tell bob wizard words"), "You tell Bob: wizard words")
    check("NoTell: delivered", heard(bob, "wizard words"), "Mina tells you: wizard words")
    check("NoTell: a wizard is not bound when sending out of it", (mina.command("quiet"), mina.command("tell carol from the quiet room"))[1], "You tell Carol: from the quiet room")
    check("NoTell: carol hears it", heard(carol, "from the quiet room"), "Mina tells you: from the quiet room")
    mina.command("out")
    check("NoTell: say still works in the room", bob.command("say still talking"), "You say: still talking")
    check("NoTell: whisper still works in the room", (dana.command("quiet"), dana.command("whisper psst to bob"))[1], "You whisper to Bob: psst")
    heard(bob, "psst")
    dana.command("out")
    bob.command("out")
    check("NoShout: a player walks in", bob.command("hush"), "The Hush Room")
    flush(carol)
    flush(alice)
    pace(10)
    check("NoShout: shouts do not leave", bob.command("shout is anyone out there"), "Your voice does not carry here.")
    check("NoShout: carol heard nothing", flush(carol), absent=("anyone out there",))
    check("NoShout: a shout does not enter", carol.command("shout hello hush room"), "You shout: hello hush room")
    check("NoShout: alice hears it", heard(alice, "hello hush room"), "Carol shouts: hello hush room")
    check("NoShout: bob did not", flush(bob), absent=("hello hush room",))
    check("NoShout: a wizard is not bound", mina.command("shout from a wizard"), "You shout: from a wizard")
    heard(carol, "from a wizard")
    mina.command("hush")
    check("NoShout: a wizard in the room still shouts out", mina.command("shout from the hush room"), "You shout: from the hush room")
    check("NoShout: heard outside", heard(alice, "from the hush room"), "Mina shouts: from the hush room")
    mina.command("out")
    check("NoShout: tell still works there", bob.command("tell alice via tell"), "You tell Alice: via tell")
    heard(alice, "via tell")
    bob.command("out")
    flush(bob)

    # ---- cleaning ----
    flush(alice)
    flush(carol)
    check("clean: colour tags are removed from say", alice.command("say {red}bright{/} words {cmd}x{/}"), "You say: bright words x", absent=("{red}", "{/}", "{cmd}"))
    check("clean: and for the listener", heard(bob, "words"), "Alice says: bright words x", absent=("{red}",))
    check("clean: tags in tell", alice.command("tell bob {b}bold{/} tell"), "You tell Bob: bold tell")
    check("clean: tags in tell, target", heard(bob, "bold tell"), "Alice tells you: bold tell", absent=("{b}",))
    check("clean: a tag rebuilt from two halves is removed too", alice.command("say {r{red}ed}x"), "You say: x", absent=("{red}",))
    check("clean: a lone brace is shown as typed", alice.command("say a { b } c"), "You say: a { b } c")
    check("clean: doubled braces are not tags", alice.command("say {{double}} {cmd}x{/}"), "You say: {{double}} x", absent=("{cmd}",))
    out = alice.raw_command("say {red}colour{/}")
    check("clean: no escape sequence reaches the speaker", str("\x1b" in out), "False")
    heard(bob, "colour")
    check("clean: no escape sequence reaches the listener", str("\x1b" in bob.raw.split("Alice says")[-1]), "False")
    check("clean: whisper tags", alice.command("whisper {red}shh{/} to bob"), "You whisper to Bob: shh", absent=("{red}",))
    heard(bob, "shh")
    check("clean: a message of only tags is empty", alice.command("say {red}{/}"), "Usage: say MESSAGE")
    check("clean: line separators become spaces", alice.command("say one two three"), "You say: one two three")
    check("clean: bidi and zero width characters are dropped", alice.command("say a‮b​c"), "You say: abc")
    check("clean: runs of blanks shrink", alice.command("say a      b"), "You say: a b")
    long_text = "x" * 150 + "y" * 150 + "TAIL" * 25
    out = alice.command("say " + long_text)
    check("clean: 300 characters at most (say)", out, "You say: " + "x" * 150 + "y" * 150 + "\r\n", absent=("TAIL",))
    out = alice.command("tell bob " + long_text)
    check("clean: 300 characters at most (tell)", out, "You tell Bob: " + "x" * 150 + "y" * 150 + "\r\n", absent=("TAIL",))
    heard(bob, "y" * 150)
    pace(10)
    out = alice.command("shout " + long_text)
    check("clean: 300 characters at most (shout)", out, "You shout: " + "x" * 150 + "y" * 150 + "\r\n", absent=("TAIL",))
    heard(carol, "y" * 150)
    exactly = "z" * 300
    check("clean: exactly 300 is kept whole", alice.command("say " + exactly), "You say: " + exactly + "\r\n")
    flush(bob)

    # ---- channels: wizard and sysop ----
    flush(warden)
    flush(mina)
    check("channels: a wizard lists wizard", mina.command("channels"), "Your channels:", "wizard", "on", absent=("sysop",))
    check("channels: a level 10 wizard lists both", warden.command("channels"), "wizard", "sysop")
    check("channels: a player belongs to none", alice.command("channels"), "You do not belong to any channels.")
    check("channel message by shout", warden.command("shout wizard first words"), "[wizard] Warden: first words")
    check("channel message reaches wizards", heard(mina, "[wizard]"), "[wizard] Warden: first words")
    check("channel message does not reach players", flush(alice) + flush(bob) + flush(dana), absent=("first words", "[wizard]"))
    check("channel shortcut: the channel name as a command", mina.command("wizard second words"), "[wizard] Mina: second words")
    heard(warden, "second words")
    check("channel message: no words gives the usage", mina.command("wizard"), "Usage: wizard MESSAGE")
    check("channel message: tags are stripped", mina.command("wizard {red}tagged{/}"), "[wizard] Mina: tagged")
    heard(warden, "tagged")
    check("channel messages are not subject to the shout limit", (mina.command("wizard a"), mina.command("wizard b"), mina.command("wizard c"))[2], "[wizard] Mina: c")
    check("channel: no shout interval for wizards", mina.command("shout wizard d"), "[wizard] Mina: d")
    heard(warden, "] Mina: d")
    flush(mina)

    check("sysop: a level 10 wizard posts", warden.command("sysop secret admin words"), "[sysop] Warden: secret admin words")
    check("sysop: by shout too", warden.command("shout sysop more admin words"), "[sysop] Warden: more admin words")
    check("sysop: a level 1 wizard does not hear it", flush(mina), absent=("admin words", "[sysop]"))
    check("sysop: a player does not hear it", flush(alice), absent=("admin words",))

    no_channel = mina.command("channel sysop off")
    no_such = mina.command("channel nosuchchannel off")
    check("sysop: a level 1 wizard is told there is no such channel", no_channel, "There is no channel called 'sysop'.")
    check("sysop: the answer is the same as for a channel that does not exist", str(no_channel.replace("sysop", "X") == no_such.replace("nosuchchannel", "X")), "True")
    check("sysop: join is refused the same way", mina.command("channel sysop join"), "There is no channel called 'sysop'.")
    check("sysop: status is asked the same way", mina.command("channel sysop"), "There is no channel called 'sysop'.")
    check("sysop: the name as a command does not exist", mina.command("sysop can you hear me"), "What?", absent=("[sysop]",))
    check("sysop: the same as any unknown word", mina.command("nosuchword can you hear me"), "What?")
    out = mina.command("shout sysop hello from mina")
    check("sysop: shout sysop is an ordinary shout for a non-member", out, "You shout: sysop hello from mina", absent=("[sysop]",))
    check("sysop: and the level 10 wizard hears it only as a shout", heard(warden, "Mina shouts"), "Mina shouts: sysop hello from mina")
    check("sysop: it did not reach the channel list", flush(warden), absent=("[sysop] Mina",))
    check("sysop: a player gets the same answers", alice.command("channel sysop on"), "There is no channel called 'sysop'.")
    check("wizard channel: a player is told there is no such channel", alice.command("channel wizard on"), "There is no channel called 'wizard'.")
    check("wizard channel: the name as a command does not exist for a player", alice.command("wizard hi"), "What?")
    check("wizard channel: shout wizard is an ordinary shout for a player", dana.command("shout wizard hi there"), "You shout: wizard hi there")
    heard(mina, "Dana shouts: wizard hi there")
    check("wizard channel: no [wizard] line for the wizards from a player", flush(warden), absent=("[wizard] Dana",))
    check("sysop: it cannot be switched off even by a member", warden.command("channel sysop off"), "cannot be switched off")
    check("channel: unknown name", warden.command("channel nope off"), "There is no channel called 'nope'.")
    check("channel: bad action", warden.command("channel wizard sideways"), "Usage: channel NAME")

    # switch off and on, saved
    check("channel off: the state is shown", mina.command("channel wizard"), "The wizard channel is on.")
    check("channel off: switch it off", mina.command("channel wizard off"), "You switch the wizard channel off.")
    check("channel off: the list says off", mina.command("channels"), "off")
    warden.command("wizard anyone listening")
    check("channel off: nothing is heard", flush(mina), absent=("anyone listening",))
    check("channel off: cannot speak on a channel that is off", mina.command("wizard hello"), "You have the wizard channel switched off.")
    mina.command("quit")
    pace(1)
    mina = h.Client("Mina")
    mina.read_until("Password: ")
    mina.send(h.PASSWORD)
    mina.read_until("> ")
    warden.command("wizard second try")
    check("channel off: saved over a new login", flush(mina), absent=("second try",))
    check("channel off: still off in the list", mina.command("channels"), "off")
    check("channel on: switch it on", mina.command("channel wizard on"), "You switch the wizard channel on.")
    warden.command("wizard third try")
    check("channel on: heard again", heard(mina, "third try"), "[wizard] Warden: third try")

    # ---- a public channel that can be joined ----
    check("lounge: not a member at first", alice.command("channels"), "You do not belong to any channels.")
    check("lounge: not a member, so the name as a command does not exist", alice.command("lounge hello"), "What?")
    check("lounge: status before joining", alice.command("channel lounge"), "You are not on the lounge channel.")
    check("lounge: join", alice.command("channel lounge join"), "You join the lounge channel.")
    check("lounge: join twice", alice.command("channel lounge join"), "already on the lounge channel")
    check("lounge: listed", alice.command("channels"), "lounge", "on")
    bob.command("channel lounge join")
    check("lounge: speak by name", alice.command("lounge hello lounge"), "[lounge] Alice: hello lounge")
    check("lounge: another member hears it", heard(bob, "hello lounge"), "[lounge] Alice: hello lounge")
    check("lounge: flood limit of one per 2 seconds", alice.command("lounge too fast"), "You are speaking too fast.")
    check("lounge: not delivered either", flush(alice), absent=("too fast",))
    pace(2.3)
    check("lounge: allowed again after the interval", alice.command("shout lounge later"), "[lounge] Alice: later")
    check("lounge: and again", heard(bob, "later"), "[lounge] Alice: later")
    check("lounge: someone who did not join does not", flush(carol) + flush(dana), absent=("[lounge]",))
    check("lounge: a member may switch it off", bob.command("channel lounge off"), "You switch the lounge channel off.")
    pace(2.3)
    alice.command("lounge for bob")
    check("lounge: and then hears nothing", flush(bob), absent=("for bob",))
    check("lounge: wizards are not bound by the flood limit", (carol.command("channel lounge join"), mina.command("channel lounge join"))[1], "You join the lounge channel.")
    check("lounge: a wizard may send twice at once", (mina.command("lounge one"), mina.command("lounge two"))[1], "[lounge] Mina: two")
    check("lounge: leave", alice.command("channel lounge leave"), "You leave the lounge channel.")
    check("lounge: after leaving the name is not a command", alice.command("lounge still here"), "What?")
    mina.command("lounge after alice left")
    check("lounge: and nothing arrives", flush(alice), absent=("after alice left",))
    check("lounge: an ignored sender is not heard", (carol.command("ignore mina"), mina.command("lounge ignored words"), flush(carol))[2], absent=("ignored words",))
    carol.command("unignore mina")
    check("wizard channel: join is refused", warden.command("channel wizard join"), "You cannot join the wizard channel")
    check("wizard channel: leave is refused", warden.command("channel wizard leave"), "You cannot leave the wizard channel")

    # ---- ignore is saved ----
    bob.command("ignore carol")
    bob.command("quit")
    pace(1)
    bob = h.Client("Bob")
    bob.read_until("Password: ")
    bob.send(h.PASSWORD)
    bob.read_until("> ")
    check("ignore: saved over a new login", bob.command("ignore"), "You are ignoring: Carol")
    bob.command("unignore carol")

    # ---- invisible wizards ----
    check("invisible: Mina vanishes", mina.command("vanish"), "vanished")
    check("invisible: a tell to her by a player gets the answer for a name that does not exist", alice.command("tell mina hello"), "No one by that name is in the world.")
    check("invisible: the same as for a nonexistent name", str(alice.command("tell mina hello") == alice.command("tell nobodyatall hello").replace("nobodyatall", "mina")), "True")
    check("invisible: her tell shows her as Someone", mina.command("tell alice you cannot see me"), "You tell Alice: you cannot see me")
    check("invisible: Someone tells you", heard(alice, "you cannot see me"), "Someone tells you: you cannot see me", absent=("Mina",))
    check("invisible: replying reaches her, shown as Someone", alice.command("reply who is this"), "You tell Someone: who is this")
    check("invisible: she hears the reply from Alice", heard(mina, "who is this"), "Alice tells you: who is this")
    check("invisible: a shout is from Someone", (mina.command("shout boo"), heard(dana, "boo"))[1], "Someone shouts: boo", absent=("Mina shouts",))
    check("invisible: a whisper is from Someone", (mina.command("whisper psst to dana"), heard(dana, "psst"))[1], "Someone whispers to you: psst")
    check("invisible: a level 10 wizard sees her", (mina.command("tell warden hi"), heard(warden, "hi"))[1], "Mina tells you: hi")
    check("invisible: a level 10 wizard may tell her", warden.command("tell mina yes we see you"), "You tell Mina: yes we see you")
    check("invisible: a wizard sees her say as Mina", (mina.command("say i am here"), heard(warden, "i am here"))[1], "Mina says: i am here")
    check("invisible: a player hears Someone say it", heard(alice, "i am here"), "Someone says: i am here", absent=("Mina says",))
    check("invisible: she is visible again", mina.command("vanish"), "visible")
    check("invisible: tell works again", alice.command("tell mina welcome back"), "You tell Mina: welcome back")
    heard(mina, "welcome back")

    # ---- an idle target (when the game has an idle timer) ----
    if "QueryIdle" in open(os.path.join(mudlib, "std", "player.cs")).read():
        check("idle: the target goes idle", dana.command("idleme"), "idle")
        out = alice.command("tell dana are you there")
        check("idle: the tell is delivered and the sender is told", out, "You tell Dana: are you there", "Dana is idle")
        heard(dana, "are you there")
        dana.command("idleme")

    # ---- security of the channel service ----
    flush(warden)
    flush(mina)
    out = mina.command("post")
    check("probe: a wizard's object cannot post to sysop", out, "send-sysop: null")
    check("probe: nor to wizard in her name", out, "send-wizard: null")
    check("probe: is not a member as far as the service is concerned", out, "ismember: False")
    check("probe: cannot list the channels", out, "verbs: 0", "listing: null")
    check("probe: cannot switch her channels", out, "switch: empty")
    check("probe: nothing reached the sysop channel", flush(warden), absent=("hacked", "[sysop]"))
    check("probe: nothing reached the wizard channel", flush(mina), absent=("hacked wizard",))
    out = mina.command("probe")
    check("probe: cannot say through the communication service", out, "say: empty")
    check("probe: cannot tell through it", out, "tell: empty")
    check("probe: cannot shout through it", out, "shout: empty")
    check("probe: cannot read what a player keeps", out, "ignores: False", "lastteller: empty")
    check("probe: nobody heard the forged text", flush(alice) + flush(bob) + flush(dana), absent=("by poster",))
    check("probe: Mina's wizard channel is still on", warden.command("wizard still on"), "[wizard] Warden: still on")
    check("probe: Mina hears it", heard(mina, "still on"), "[wizard] Warden: still on")
    check("probe: she can still hear shouts", (warden.command("shout wizards can shout"), heard(mina, "wizards can shout"))[1], "Warden shouts: wizards can shout")
    check("probe: a fresh player body cannot post", mina.command("forgeplayer"), "fake: null")
    check("probe: no sysop line anywhere", flush(warden), absent=("fake", "[sysop]"))
    check("probe: Mina is not told of sysop by anything", flush(mina), absent=("sysop",))
    # editing the channel files needs level 10
    check("security: a level 1 wizard cannot edit /channels", mina.command("edit /channels/sysop"), "You need wizard level 10 to change /channels.")
    check("security: nor create a channel", mina.command("mkdir /channels/x"), "You need wizard level 10 to change /channels.")
    check("security: nor delete one", mina.command("delete /channels/sysop"), "You need wizard level 10 to change /channels.")
    # a promotion is effective at the next message
    check("promotion: Mina becomes level 10", warden.command("promote mina 10"), "mina is now wizard level 10")
    heard(mina, "level is now 10")
    check("promotion: she belongs to sysop at once", mina.command("sysop reporting for duty"), "[sysop] Mina: reporting for duty")
    check("promotion: and the other members hear her", heard(warden, "reporting for duty"), "[sysop] Mina: reporting for duty")
    check("promotion: she lists both", mina.command("channels"), "wizard", "sysop")
    check("promotion: the probe still cannot post", mina.command("post"), "send-sysop: null")
    check("promotion: even though its owner belongs", flush(warden), absent=("hacked",))

    # ---- help ----
    out = alice.command("help", marker="Colour:")
    check("help lists the speaking commands", out, "Speaking:", "say", "tell", "reply", "whisper", "shout", "ignore", "channels")
    flush(alice)


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-comm-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    os.makedirs(os.path.join(mudlib, "domains", "ctest"), exist_ok=True)
    h.write(mudlib, "/domains/ctest/quiet", QUIET)
    h.write(mudlib, "/domains/ctest/hush", HUSH)
    h.write(mudlib, "/domains/ctest/other", OTHER)
    h.write(mudlib, "/channels/lounge", LOUNGE)
    h.edit(mudlib, SQUARE, 'Exits["south"] = "/domains/hollow/village/tavern";',
           'Exits["south"] = "/domains/hollow/village/tavern";\n        Exits["quiet"] = "/domains/ctest/quiet";\n'
           '        Exits["hush"] = "/domains/ctest/hush";\n        Exits["other"] = "/domains/ctest/other";')
    patch_player(mudlib)
    box = [None, None]
    box[0], box[1] = a.start_driver(work, mudlib)
    try:
        run(work, mudlib)
        box[1].flush()
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
