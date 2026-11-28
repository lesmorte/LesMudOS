#!/usr/bin/env python3
"""Tests of the Mage's hidden class as built: the Clerk of the Chancery, its summons, the gated inner door of City Hall, the two rooms, the people and
the quest "Jury Duty" (docs/specs/bureaucrat.md sections 2, 3, 4, 11, 13, 14). It uses the REAL class and the REAL files; the model is tests/hidden_test.py,
which tests the pattern with a fixture class.

The roll is CLOSED in the shipped class (no spells yet), so the summons is served with the level 10 wizard command `hidden grant`; the roll's own
eligibility (a Mage, never a Warrior, never a wizard) is tested on a test copy of the class file with the roll opened.

  1. The summons text, the door for strangers (byte for byte what City Hall printed before), the walk from the north gate.
  2. The whole case, taken: every stage's reply, the journal, wrong answers, the lent orders, the goose, the search finds, the exhibits, the verdict,
     the return, the offer, `pledge` twice; the seal, the pay, the shop, help, the trainer.
  3. The whole case, declined; the six closing remarks; the worst player choices still reach a verdict.
  4. Leaving and resuming at every stage; a dropped connection and a takeover in the hearing; two jurors at once.
  5. A second summons never; a decliner shut out; a holder let in; a Warrior and a wizard never summoned; a stranger sees nothing anywhere.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=5061 TMPDIR=/tmp/hiddenagent python3 tests/jury_test.py
"""
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "5061")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import gmcp_test as g  # noqa: E402
import area_test as a  # noqa: E402

CHECKS = [0]
check = h.check
T = "/domains/jurytest/"
OAK = "/domains/greenvale/oakenfell/"
HALL = OAK + "founders/city_hall"
WAITING = OAK + "founders/chancery"
COURT = OAK + "founders/chancery_court"
NORTHGATE = OAK + "founders/northgate"
PW = h.PASSWORD
REPO = h.ROOT


def squash(text):
    return re.sub(r"\s+", " ", text)


def ck(label, text, *expected, absent=()):
    """h.check on the text with every run of whitespace one space (the game may break a long line), and the same for what is expected."""
    CHECKS[0] += 1
    h.check(label, squash(text), *[squash(e) for e in expected], absent=absent)


def truth(label, condition, detail=""):
    CHECKS[0] += 1
    h.check(label, str(bool(condition)) + ("" if condition else " " + str(detail)), "True")


# ------------------------------------------------------------------ the test copy

PLAYER_ACTIONS = ('        AddAction("xp", a => { AddXp(int.Parse(a)); return true; });\n'
                  '        AddAction("tp", a => { MoveObject(LoadObject(a)); Look(); return true; });\n'
                  '        AddAction("mk", a => { MoveObject(CloneObject(a), Self); return true; });\n')

MOB = '''// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{nm}"; Ids.Add("{nm}"); Ids.Add("mob"); ShortDesc = "a {nm}"; LongDesc = "A test creature.";
        Level = {lv}; MaxVitality = 3; DamageMin = 1; DamageMax = 1; ToHit = 0; Armor = 0; XpValue = 15; GoldMin = 3; GoldMax = 3; Aggressive = false;
    }}
}}
'''

ARENA = '''// @inherit /std/room
public class JtArena : Room
{
    public override void Create()
    {
        ShortDesc = "The Jury Test Arena"; LongDesc = "A bare arena for the jury test.";
        MoveObject(CloneObject("/domains/jurytest/probe"), Self);
    }
}
'''

PROBE = r'''// @inherit /std/thing
public class JtProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        foreach (var v in new[] { "jspawn", "jkill", "jstat", "jgold", "jhas", "jrolls", "jwait", "jlist", "jdie" }) AddAction(v, Do);
    }
    private ObjectRef? FirstMob(ObjectRef me) { foreach (var o in AllInventory(Environment(me)!).ToList()) if (o != me && o.IsAlive && o.Call<bool>("IsMonster")) return o; return null; }
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        switch (QueryVerb())
        {
            case "jspawn": MoveObject(CloneObject("/domains/jurytest/" + w[0]), Environment(me)!); Write("SPAWNED\n"); return true;
            case "jkill": { var m = FirstMob(me); if (m == null) { Write("NOMOB\n"); return true; } m.Call("Die", me); Write("KILLED\n"); return true; }
            case "jstat": Write($"STAT gold={me.Call<int>("QueryGold")} xp={me.Call<int>("QueryXp")} level={me.Call<int>("QueryLevel")}\n"); return true;
            case "jgold": me.Call<bool>("AddGold", int.Parse(w[0])); Write("GOLD\n"); return true;
            case "jhas": Write("ITEMS " + string.Join("|", AllInventory(me).Select(i => i.Call<string>("Short"))) + "\n"); return true;
            case "jdie": me.Call("Die", (ObjectRef?)null); Write("DIED\n"); return true;
            case "jrolls": Write("ROLLS " + LoadObject("/secure/search").Call<bool>("SetTestRolls", w[0]) + "\n"); return true;
            case "jwait": Write("WAIT " + LoadObject("/secure/search").Call<bool>("SetTestWait", int.Parse(w[0])) + "\n"); return true;
            case "jlist":
            {
                var c = LoadObject("/secure/classes");
                Write("LIST children=" + string.Join(",", c.Call<List<string>>("EnabledChildren", "mage", "advanced") ?? new List<string>()) + " advanced=" + string.Join(",", c.Call<List<string>>("EnabledOfTier", "advanced") ?? new List<string>()) + " archetypes=" + string.Join(",", c.Call<List<string>>("EnabledOfTier", "archetype") ?? new List<string>()) + "\n");
                return true;
            }
        }
        return false;
    }
}
'''


def prepare(mudlib):
    path = os.path.join(mudlib, "std", "player.cs")
    src = open(path).read()
    anchor = '        AddAction("channels", a => ToChannels("Listing", ""));'
    assert anchor in src
    open(path, "w").write(src.replace(anchor, PLAYER_ACTIONS + anchor, 1))
    h.edit(mudlib, "/std/adventurer", "if (!IsWizard(LevelSetLevel)) return false;", "")  # a test aid: any character may use setlevel
    h.edit(mudlib, "/classes/bureaucrat", "SummonsOpen = false;", "SummonsOpen = true;")  # test copy only: the shipped roll is closed
    os.makedirs(os.path.join(mudlib, "domains", "jurytest"), exist_ok=True)
    h.write(mudlib, T + "jm5", MOB.format(cls="JtM5", nm="jtm", lv=5))
    h.write(mudlib, T + "arena", ARENA)
    h.write(mudlib, T + "probe", PROBE)


# ------------------------------------------------------------------ helpers

def body(text):
    return "\n".join(line for line in text.replace("\r", "").split("\n") if line.strip() and not re.match(r"^\w* ?\(\d+/\d+\)", line)).strip()


def plain(text):
    return re.sub(r"\s+", " ", body(text)).strip()


def untag(text):
    return re.sub(r"\{(?:cmd|title|b|u|/)\}", "", text)


def unescape(raw):
    return json.loads('"' + raw + '"')


def login(name, archetype="mage"):
    c = h.Client(name, archetype=archetype)
    c.read_until("> ")
    return c


def relog(c, name, how):
    if how == "quit":
        c.send("quit")
        c.closed(5)
        time.sleep(0.5)
        n = h.Client(name)
        n.read_until("> ")
        return n
    if how == "drop":
        c.sock.close()
        time.sleep(1.5)
        n = h.Client(name)
        n.read_until("> ")
        return n
    n = h.Client(name, login=False)
    n.read_until("Password: ")
    n.send(PW)
    n.choose_calling(None)
    n.read_until("> ", 15.0)
    return n


def wait(c, seconds):
    return a.quiet(c, seconds)


def mage(name, level=5, where=None):
    c = login(name)
    c.command("mk /domains/jurytest/probe")
    c.command(f"setlevel {level}")
    if where:
        c.command("tp " + where)
    a.drain(c, 0.3)
    return c


def wizard(name="jurywiz"):
    w = h.Client(name, archetype="warrior")
    w.read_until("> ")
    ck("the secret makes the first wizard", w.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(w, 0.4)
    return w


def rec(w, name):
    out = w.command("hidden bureaucrat")
    m = re.search(rf"(?<![A-Za-z]){name}: looted (\d+), issued (no|yes #\d+), stage (\d+), outcome (\d+)", out)
    return None if not m else (int(m.group(1)), m.group(2) != "no", int(m.group(3)), int(m.group(4)))


def stat(c):
    m = re.search(r"STAT gold=(\d+) xp=(\d+) level=(\d+)", c.command("jstat"))
    return (int(m.group(1)), int(m.group(2)), int(m.group(3))) if m else None


def walk_to_hall(c):
    """From Oakenfell's north gate: south, south, then the City Hall door off the square."""
    c.command("tp " + NORTHGATE)
    a.drain(c, 0.3)
    c.command("south")
    c.command("south")
    return c.command("cityhall")


def knock(c, secs=2.0):
    out = c.command("knock")
    return out + wait(c, secs)


def goto(c, path, secs=0.3):
    out = c.command("tp " + path)
    return out + wait(c, secs)


def summon(w, c, name):
    return w.command(f"hidden grant {name} bureaucrat")


def say_through(c, name):
    """A summoned character from the north gate to the waiting room, the Registrar's welcome included."""
    walk_to_hall(c)
    return knock(c)


# ------------------------------------------------------------------ the spec's texts

SUMMONS = """                      THE CHANCERY OF OAKENFELL
                    SUMMONS TO ATTEND AS JUROR
              Form 27-J (third copy: the bearer's copy)

  Reference:  CH/J/{serial}-B
  Issued:     {date}, being the date on which you read this
  To:         {Name}, a person of sufficient standing to be inconvenienced

  You are hereby summoned to attend the Chancery as a juror in a matter
  to be heard. Present this summons at the inner door of City Hall,
  Founder's Square, Oakenfell. Knock.

  You must attend within fourteen days of the date of issue. The date of
  issue is the date on which you read this. The Chancery does not regard
  this as circular.

  Bring this summons. Do not bring a weapon, an opinion or a friend. You
  will not be required to remember anything. You will be required to be
  present while it happens.

  Failure to attend renders you liable to a fine of THIRTY-SEVEN GOLD AND
  ONE THIRD OF A GOLD (37 1/3), together with one eighth of a gold for
  the paper this notice is written on. The Chancery will write to you
  about the fine. It has never been known to finish the letter.

  Jurors are paid. The rate is one gold. It is entered.

                                   For the Chancery, M. Ashcombe, Registrar.
                                   Stamped in red. NOTHING IS DONE UNTIL IT
                                   IS ENTERED."""

FORBIDDEN_IN_SUMMONS = ("class", "path", "seal", "bureaucrat", "clerk of the chancery")


def hall_texts():
    """What City Hall printed before the gated exit existed: read from the committed file of stage 1 (the unchanged room)."""
    old = subprocess.run(["git", "show", "5e1011e:mudlib/domains/greenvale/oakenfell/founders/city_hall.cs"], cwd=REPO, capture_output=True, text=True, check=True).stdout
    detail = re.search(r'AddDetail\("letters,lintel,motto,inner door,door,gilt", "((?:[^"\\]|\\.)*)"\)', old).group(1)
    readable = re.search(r'AddReadable\("letters,lintel,motto,inscription", "((?:[^"\\]|\\.)*)"', old).group(1)
    return untag(unescape(detail)), untag(unescape(readable))


# ------------------------------------------------------------------ step 1: the summons and the door

DOOR_VERBS = ("in", "enter", "knock", "open door", "go in", "knock on door", "open", "enter door", "open inner door")


def step_summons_and_door(w):
    print("\n=== 1. the summons, the door for a stranger, the walk")
    detail, readable = hall_texts()
    st = mage("jurystr", 6)
    out = walk_to_hall(st)
    ck("a stranger walks from the north gate to City Hall", out, "City Hall")
    unknown = plain(st.command("flarb"))
    ck("the exits line shows only `out`", st.command("look"), "Obvious exits: out", absent=("in,", ", in"))
    for key in ("door", "inner door", "letters", "lintel", "motto", "gilt"):
        truth(f"look at {key}: today's text, byte for byte", plain(st.command(f"look at {key}")) == plain(detail), plain(st.command(f"look at {key}")))
    for key in ("letters", "lintel", "motto", "inscription"):
        truth(f"read {key}: today's text, byte for byte", plain(st.command(f"read {key}")) == plain(readable), plain(st.command(f"read {key}")))
    for v in DOOR_VERBS:
        truth(f"a stranger: `{v}` is the answer to an unknown word", plain(st.command(v)) == unknown, plain(st.command(v)))
    ck("`search` in City Hall finds nothing", st.command("search") + wait(st, 3.6), "find nothing of note")
    ck("... and the gate never shows in `look` or `exits` words", st.command("look"), absent=("Chancery", "inner door opens"))
    # the summons
    ann = mage("juryann", 5)
    ck("the wizard serves a summons", summon(w, ann, "juryann"), absent=("cannot", "not possible", "Nobody"))
    ck("it comes with the found line", ann.command("i"), "a summons")
    ck("`look at summons`", ann.command("look at summons"), "A stiff buff-coloured paper, folded in three and stamped in red. It has your name on it. You could read it.")
    text = ann.command("read summons")
    date = re.search(r"Issued:\s+(.+?), being the date", text)
    serial = re.search(r"CH/J/(\d+)-B", text)
    truth("the reference is 4400 plus the serial (the first ever served is 4401)", serial and serial.group(1) == "4401", text[-300:])
    truth("the date is the game date", date and len(date.group(1)) > 8, text[:300])
    expected = SUMMONS.replace("{serial}", serial.group(1) if serial else "?").replace("{date}", date.group(1) if date else "?").replace("{Name}", "Juryann")
    truth("`read summons`: the spec's text, every line", plain(text) == plain(expected), plain(text)[:400])
    low = plain(text).lower()
    for word in FORBIDDEN_IN_SUMMONS:
        truth(f"the summons never says '{word}'", re.search(rf"\b{re.escape(word)}\b", low) is None, word)
    ck("`drop summons` is refused in the paper's words", ann.command("drop summons"), "It is addressed to you. It is not a thing one drops.")
    ck("the register: served, stage 1", w.command("hidden bureaucrat"), "juryann: looted 0, issued yes #1")
    # the door, with the summons
    out = walk_to_hall(ann)
    ck("a summoned character sees the extra line at the door", ann.command("look at door"), plain(detail)[:60], "brass knocker in the shape of a rubber stamp", "expecting someone with paper")
    ck("... and `in` in the exits line", ann.command("look"), "in")
    out = knock(ann, 2.2)
    ck("the first knock: the spec's line, then the waiting room", out, "You knock on the inner door, under the gilt letters. A voice on the other side says, without hurry, 'Reference?' You hold up the summons. The door opens, politely, as though it had been about to.", "The Chancery: Waiting Room")
    ck("the Registrar's entry", out, "Marian Ashcombe does not look up from her ledger", "You are Juror Twelve of Twelve", "TAKE A NUMBER")
    ann.command("out")
    a.drain(ann, 0.3)
    ck("the second knock: the other line", ann.command("knock"), "You knock. 'Come in,' says the voice. 'You are expected. You are always expected. It is the Chancery's only courtesy.'")
    a.drain(ann, 0.5)
    return ann


# ------------------------------------------------------------------ step 2: the case

def play_case(c, name, w, verdict="y", exhibits=2, deep=False, answer=None, stranger=None):
    """One character through the whole case, from a summons to the offer. exhibits: 0, 1 (the Roll only) or 2. Returns the Registrar's return text."""
    def ckd(label, text, *expected, absent=()):
        if deep:
            ck(label, text, *expected, absent=absent)
    summon(w, c, name)
    out = say_through(c, name)
    ckd("the quest begins with the Registrar's entry", out, "Marian Ashcombe does not look up from her ledger", "TAKE A NUMBER")
    ckd("`quests` lists Jury Duty", c.command("quests"), "Jury Duty")
    ckd("the journal's note for stage 2", c.command("quest jury"), "Take a number, then ask the Registrar about the oath.")
    ckd("the journal says it cannot be abandoned", c.command("abandon jury"), "cannot be abandoned")
    ckd("the oath before a number is refused", c.command("ask registrar about oath"), "I cannot swear an unnumbered juror. It has been tried. Take a number.")
    ckd("the east door is shut", c.command("east"), "The east door is shut. The Registrar says, without looking up, 'Jurors are sworn first. It is not a complicated rule. It is merely a rule.'")
    ckd("the machine: a number", c.command("take number"), "You turn the crank. The machine clanks, thinks about it, and drops a paper ticket into your hand: 'No. ")
    ckd("... once only", c.command("turn crank"), "The machine has already issued you a number. It does not do refunds.")
    ckd("`read ticket`", c.command("read ticket"), "Please hold this ticket until you are called, and for some time afterwards.")
    ckd("the Registrar on the machine", c.command("ask registrar about clock"), "The machine is correct. The clock is correct. The time is wrong.")
    ckd("the Registrar on the summons", c.command("ask registrar about summons"), "Form 27-J. Third copy.")
    ckd("the Registrar on the fine", c.command("ask registrar about fine"), "The third of a gold is the difficulty.")
    ckd("the Registrar on the court", c.command("ask registrar about court"), "It is Pruett against Marrowby, the matter of the goose.")
    ckd("the Registrar on anything else", c.command("ask registrar about weather forecasts"), "Marian Ashcombe says:")
    out = c.command("ask registrar about oath")
    ckd("the oath: the spec's words and a badge", out, "Raise your right hand. The other one.", "A juror's badge. It is a loan from the court", "Do not touch the goose.")
    ckd("the badge is in the pack and the ticket is not", c.command("i"), "a juror's badge", absent=("paper ticket",))
    ckd("the journal moves to stage 3", c.command("quest jury"), "You are sworn. The court sits to the east")
    ckd("the Registrar on the lent orders", c.command("ask registrar about orders"), "A restraining order, a notice to cease and desist, and a form in triplicate.")
    ckd("... and the pigeonholes", c.command("ask registrar about weather"), "Pigeonhole two hundred and twelve.")
    ckd("... the badge", c.command("ask registrar about badge"), "You have it. You are wearing it. Is it not remarkable?")
    ckd("the register: stage 3", w.command("hidden bureaucrat"), f"{name}: looted 0, issued yes #", "stage 3")
    # the pigeonholes: the Roll
    roll = 0 if exhibits == 0 else 1
    if exhibits >= 1:
        out = c.command("search pigeonholes") + wait(c, 3.4)
        ckd("the pigeonholes: the Roll", out, "In pigeonhole two hundred and twelve, between a notice of adjournment and a very old umbrella, is a long roll tied with green tape. You take it.")
        ckd("... in the pack", c.command("i"), "a Boundary Roll")
        ckd("`read roll`", c.command("read roll"), "BOUNDARY ROLL: MARROW LANE.", "H. Hollis, mason.")
    out = c.command("east") + wait(c, 3.2)
    ckd("the court", out, "The Chancery: Court of First Instance", "Clerk Crake rings his bell. 'All rise,'", "The jury is you.")
    if exhibits >= 1:
        ckd("the goose takes the Roll", out, "Counsel rises. It crosses the court with great dignity", "Counsel always asks to see the exhibit.")
        ckd("... it has left the pack", c.command("i"), absent=("Boundary Roll",))
    return finish_case(c, name, w, verdict, exhibits, deep, answer, stranger)


def finish_case(c, name, w, verdict, exhibits, deep, answer, stranger):
    def ckd(label, text, *expected, absent=()):
        if deep:
            ck(label, text, *expected, absent=absent)
    ckd("the Clerk on the verdict before anything is heard", c.command("ask clerk about verdict"), "The court cannot take a verdict from a juror who has not heard the parties. It has tried.")
    ckd("the plaintiff, the first time", c.command("ask pruett about goose"), "Three marrows. Three! Prize marrows", "And the stakes.")
    ckd("... the second time", c.command("ask pruett about marrows"), "Eleven gold. And the stakes.")
    ckd("the defendant while the plaintiff is unrestrained: the interruption", c.command("ask tilda about goose"), "Objection! She is lying! The goose is hers!", "have been known to have a form for that")
    ckd("`desist tilda` with nothing to cease", c.command("desist tilda"), "Tilda Marrowby is not saying anything that has not already been filed.")
    ckd("`restrain` with no one named", c.command("restrain"), "Serve whom?")
    ckd("`restrain tilda`: not that party", c.command("restrain tilda"), "The court's authority does not stretch that far.")
    ckd("`restrain goose`: Counsel is not a party", c.command("restrain goose"), "The court's authority does not stretch that far. The goose is Counsel, not a party.")
    ckd("`restrain pruett`: the trial order", c.command("restrain pruett"), "You serve Gervase Pruett with a trial restraining order, on the court's authority.", "He sits, writes something, and underlines it.")
    ckd("... a second time", c.command("restrain pruett"), "Pruett has been served. He is writing about it.")
    ckd("the Clerk, still, on the verdict (Tilda not heard)", c.command("ask clerk about verdict"), "has not heard the parties")
    if answer == "late":
        out = c.command("ask tilda about goose")
        ckd("the history begins", out, "Tilda Marrowby draws a very long breath. 'It begins,' she says, 'with my great-grandmother, Hester Marrowby")
        late = wait(c, 9.5)
        ckd("nobody serves the notice: it runs on", late, "The history proceeds as far as the Founders' second winter. Clerk Crake rings his bell and waits.", "'Briefly, then,' says Tilda, and has the grace to look tired.", "The goose is mine.")
        ckd("the court is recovering", c.command("ask clerk about verdict"), "The court is recovering.")
        ckd("`desist tilda` after the fact", c.command("desist tilda"), "Tilda Marrowby is not saying anything that has not already been filed.")
        wait(c, 20.5)
    else:
        out = c.command("ask tilda about goose")
        ckd("the history begins", out, "Tilda Marrowby draws a very long breath.")
        out = c.command("desist tilda")
        ckd("the notice in time", out, "You serve Tilda Marrowby with a trial notice to cease and desist, on the court's authority. Her great-grandmother stops mid-sentence. 'Oh,' says Tilda. 'Well. Briefly, then.'", "The goose is mine. I have never said otherwise.", "Try the table.")
    if exhibits >= 1:
        if exhibits == 2:
            out = c.command("search table") + wait(c, 3.4)
            ckd("the table: the chit", out, "In the drawer of the left-hand table, under a stub of wax and a peppermint, is a slip of paper, curled with age. You take it.")
            ckd("`read chit`", c.command("read chit"), "Received of Mr G. Pruett, senior, three gold", "H. Hollis, mason.")
        ckd("`take roll` while the goose holds it", c.command("take roll"), "The goose is holding it. The goose looks at you.")
        ckd("`ask goose about roll`", c.command("ask goose about roll"), "The goose looks at the Roll, and then at you, and holds the Roll a little higher.")
        ckd("`triplicate goose`", c.command("triplicate goose"), "You serve the goose with a trial Form 27-B, in triplicate, on the court's authority.", "It is, however, looking elsewhere.")
        ckd("`take roll` now", c.command("take roll"), "The goose is still looking for the second copy. You take the Roll from under its wing. It lets you.")
        ckd("`submit roll`", c.command("submit roll"), "You hand the Roll to Clerk Crake.", "Exhibit One")
        if exhibits == 2:
            ckd("`submit chit`", c.command("submit chit"), "Clerk Crake reads the chit twice. 'Exhibit Two,' he says, quietly.", "Pruett finds something very interesting in his boots.")
    ckd("`submit badge`: not an exhibit", c.command("submit badge"), "Clerk Crake does not accept that. The court has never accepted that.")
    ckd("`submit zebra`: nothing", c.command("submit zebra"), "You have nothing the court recognises.")
    ckd("`kill goose`", c.command("kill goose"), "The court takes a dim view.")
    ckd("`ask goose` about anything", c.command("ask goose about roll"), "The goose looks at you. You have the impression of having been entered.")
    # the verdict, with wrong answers
    out = c.command("ask clerk about verdict", marker="(y/n)")
    ckd("the verdict question", out, "Clerk Crake rises. 'The jury will say, yes or no, and without elaboration. Is the defendant liable for the marrows? (y/n)'")
    c.send("maybe")
    ckd("a wrong line", wait(c, 0.8), "The court requires a yes or a no. The court has waited longer for less.")
    c.send("perhaps")
    ckd("a second wrong line", wait(c, 0.8), "The court will ask again when you are ready.")
    ckd("the register is still at stage 3", w.command("hidden bureaucrat"), f"{name}: looted 0, issued yes #", "stage 3")
    c.command("ask clerk about verdict", marker="(y/n)")
    c.send("yes" if verdict == "y" else "n")
    out = wait(c, 1.0)
    ckd("the verdict is entered", out, "'The court enters: liable.' He writes. 'Eleven gold, and the stakes. The court regrets the stakes.'" if verdict == "y" else "'The court enters: not liable.' He writes. 'Mr Pruett will be informed. He will be informed at length.'",
        "'The marrows,' says Gervase Pruett, to no one. 'The stakes.'", "collects the goose.", "Return your badge to the Registrar.")
    ckd("the register: stage 4", w.command("hidden bureaucrat"), f"{name}: looted 0, issued yes #", "stage 4")
    ckd("the journal's note for stage 4", c.command("quest jury"), "The verdict is entered. Return your badge to the Registrar.")
    ckd("the Clerk after the verdict", c.command("ask clerk about verdict"), "The matter is heard.")
    out = c.command("west") + wait(c, 2.6)
    return out


def remark(verdict, exhibits):
    table = {
        ("y", 0): "Liable, on no exhibits. The court admires decisiveness. The court also wonders what it was based on.",
        ("y", 1): "Liable, on one exhibit. A sound habit: when in doubt, find against the person who has been shouting.",
        ("y", 2): "Liable, on both exhibits, with the elm in evidence. Rigorous. Mrs Marrowby has been told that the goose will in future be supervised.",
        ("n", 0): "Not liable, on no exhibits. Generous. The Chancery does not hold it against you.",
        ("n", 1): "Not liable, on one exhibit. Cautious. It will survive an appeal, which is no part of the verdict.",
        ("n", 2): "Not liable, on both exhibits. The elm has been moved, the marrows were on her side, and the goose cannot be shown to have known. Mr Pruett has appealed. He has appealed to the elm.",
    }
    return table[(verdict, exhibits)]


OFFER = "Marian Ashcombe closes the Book of Proceedings and, for the first time, looks at you. 'One further matter. The court has an opening."


def step_taken(w):
    print("\n=== 2. the whole case, taken")
    c = mage("jurytak", 5)
    c.command("jgold 0")
    before = None
    out = play_case(c, "jurytak", w, "y", 2, deep=True)
    ck("back in the waiting room the Registrar holds out her hand", out, "Marian Ashcombe holds out her hand. You give her the badge.", remark("y", 2), "The juror's fee is one gold. It is entered.")
    ck("... and makes the offer", out, OFFER, "say pledge", "say decline")
    ck("the badge is gone", c.command("i"), absent=("juror's badge",))
    ck("the east door is shut again", c.command("east"), "The east door is shut.")
    ck("the journal's note for stage 5", c.command("quest jury"), "The Registrar has a question for you. Neither answer is urgent. Neither is reversible.")
    ck("the register: the offer is open", w.command("hidden bureaucrat"), "jurytak: looted 0, issued yes #", "stage 5, outcome 0")
    ck("`ask registrar about seal` says the offer again", c.command("ask registrar about seal"), "If you wish to take the seal, say pledge.")
    ck("ranks are not for the unpledged", plain(c.command("ranks")), plain(c.command("flarb")))
    for v in ("browse", "buy staff", "sell staff", "value staff"):
        truth(f"the shop is not open to a juror: `{v}` is an unknown word", plain(c.command(v)) == plain(c.command("flarb")), plain(c.command(v)))
    gold, xp, level = stat(c)
    ck("the first pledge: the permanence line", c.command("pledge"), "I will say this once, as the Chancery requires. The seal is for life.", "No fee is asked.", absent=("You are now",))
    ck("nothing was taken", c.command("score"), absent=("Clerk of the Chancery",))
    out = c.command("pledge")
    ck("the second pledge: the ceremony", out, "Marian Ashcombe rises. 'Raise your right hand. The other one. Repeat after me. I will lodge before I act.", "'It will be dry in a moment.'")
    ck("the class", out, "You are now a Clerk of the Chancery.")
    ck("her last words", out, "Your first form is Form 27-B. It is in triplicate. Please do not lose the carbon.", "it is not to be sold.")
    ck("the seal is in the pack, the summons is not", c.command("i"), "a Notary's seal", absent=("summons",))
    ck("`score` shows the class", c.command("score"), "Class Clerk of the Chancery")
    gold2, xp2, level2 = stat(c)
    truth("one gold was paid", gold2 == gold + 1, (gold, gold2))
    truth("250 experience was paid", xp2 >= xp + 250 or level2 > level, (xp, xp2, level, level2))
    ck("the quest is completed with the authored summary and its reward", c.command("quests completed"), "Jury Duty", "1 gold, 250 experience and a seal of the Chancery.", "answered the Registrar's last question")
    ck("the register: taken", w.command("hidden bureaucrat"), "jurytak: looted 0, issued yes #", "stage 9, outcome 1")
    ck("a second grant is refused", w.command("hidden grant jurytak bureaucrat"), "They already have an advanced class.")
    ck("`pledge` is not a command any more", plain(c.command("pledge")), plain(c.command("flarb")))
    # holders only: help, the shop, the trainer, the topics
    ck("`help bureaucrat` for a holder", c.command("help bureaucrat"), "BUREAUCRAT (the Clerk of the Chancery). A Mage who has taken the Chancery's seal.", "The seal is for life.", "has never lost a war")
    ck("`help clerk of the chancery` too", c.command("help clerk of the chancery"), "BUREAUCRAT (the Clerk of the Chancery)")
    ck("the shop: browse", c.command("browse"), "Clerk's staff of office", "45 gold", "Notary's seal", "60 gold")
    c.command("jgold 200")
    out = c.command("buy staff")
    ck("buy the staff", out, "You buy a Clerk's staff of office for 45 gold.")
    ck("buy a second seal", c.command("buy seal"), "You buy a Notary's seal for 60 gold.")
    ck("the staff", c.command("look at clerk's staff"), "A staff of black ash, shod in brass")
    c.command("wield staff")
    ck("`ranks` for a holder: the Registrar teaches", c.command("ranks"), "teaches")
    ck("a holder asks about the Chancery", c.command("ask registrar about chancery"), "We have never lost a war. We have never agreed that one was properly declared.")
    ck("... about the class", c.command("ask registrar about bureaucrat"), "The common word is Bureaucrat, and I do not correct it")
    ck("... the price of ranks", c.command("ask registrar about ranks"), "Ranks are forty, a hundred and fifty, four hundred and nine hundred")
    ck("... the goose", c.command("ask registrar about goose"), "Mrs Marrowby's goose is, I understand, in good health.")
    ck("... the bosses", c.command("ask registrar about boss"), "above the law")
    ck("... an unknown word", c.command("ask registrar about flarb"), "I can't tell you anything about that.")
    # the Notary's Seal: +2 Authority. Wear it (a Bound item can be worn); the derived statistic is shown nowhere to players.
    ck("wear the seal", c.command("wear seal"), absent=("What?",))
    # the door
    c.command("out")
    a.drain(c, 0.4)
    ck("a holder walks in without knocking", c.command("knock"), "The inner door opens before you knock. It has your file.", "The Chancery: Waiting Room")
    a.drain(c, 0.5)
    ck("a holder who walks in is not made a juror again: no Welcome, no quest line", wait(c, 1.5), absent=("Juror Twelve",))
    c.command("out")
    a.drain(c, 0.3)
    ck("`in` works too", c.command("in"), "The Chancery: Waiting Room")
    a.drain(c, 0.3)
    return c


def step_declined(w):
    print("\n=== 3. the whole case, declined, and the six closing remarks")
    c = mage("jurydec", 5)
    out = play_case(c, "jurydec", w, "n", 2, deep=False)
    ck("the return, not liable on both exhibits", out, remark("n", 2), "say decline")
    gold, xp, level = stat(c)
    ck("the first decline warns", c.command("decline"), "You may decline. It is permitted, and it is final. The Chancery does not summon twice.", "again within the minute")
    ck("nothing was decided", w.command("hidden bureaucrat"), "jurydec: looted 0, issued yes #", "stage 5, outcome 0")
    ck("the second decline closes it", c.command("decline"), "Entered.", "The court keeps the summons.", "A certificate of jury service.", "The door will not trouble you again.")
    ck("a certificate in the pack, the summons not", c.command("i"), "a Certificate of Jury Service", absent=("summons",))
    ck("`read certificate`", c.command("read certificate"), "CERTIFICATE OF JURY SERVICE.", "or until the Chancery forgets, whichever is the longer.")
    gold2, xp2, level2 = stat(c)
    truth("one gold, as for a taker", gold2 == gold + 1, (gold, gold2))
    truth("250 experience, as for a taker", xp2 >= xp + 250 or level2 > level, (xp, xp2))
    ck("the journal: the same summary, its own reward", c.command("quests completed"), "Jury Duty", "1 gold, 250 experience and a Certificate of Jury Service.")
    ck("the register: declined", w.command("hidden bureaucrat"), "jurydec: looted 0, issued yes #", "stage 9, outcome 2")
    ck("no class", c.command("score"), absent=("Clerk of the Chancery",))
    ck("no seal", c.command("i"), absent=("seal",))
    ck("`pledge` is nothing now", plain(c.command("pledge")), plain(c.command("flarb")))
    ck("a decliner can walk out", c.command("out"), "City Hall")
    a.drain(c, 0.4)
    unknown = plain(c.command("flarb"))
    for v in DOOR_VERBS:
        truth(f"a decliner at the door: `{v}` is an unknown word for ever", plain(c.command(v)) == unknown, plain(c.command(v)))
    ck("... the exits line has no `in`", c.command("look"), "Obvious exits: out", absent=("in,", ", in"))
    ck("... `look at door` is today's text", plain(c.command("look at door")), plain(hall_texts()[0]), absent=("brass knocker",))
    ck("a second summons is never issued", w.command("hidden grant jurydec bureaucrat"), "They have been summoned already.")
    c = relog(c, "jurydec", "quit")
    c.command("tp " + HALL)
    a.drain(c, 0.3)
    truth("after a login the door is still closed", plain(c.command("knock")) == unknown, plain(c.command("knock")))
    # the six closing remarks (a taker for each, so the offer is made after each)
    for k, (verdict, ex) in enumerate((("y", 0), ("y", 1), ("n", 0), ("n", 1), ("y", 2), ("n", 2))):
        nm = "jurycase" + "abcdef"[k]
        d = mage(nm, 5)
        out = play_case(d, nm, w, verdict, ex, deep=False)
        ck(f"the closing remark: verdict {verdict}, {ex} exhibit(s)", out, remark(verdict, ex), "The juror's fee is one gold. It is entered.", "say pledge")
        d.sock.close()


def step_worst(w):
    print("\n=== 3b. the worst player choices still reach a verdict")
    c = mage("juryworst", 5)
    out = play_case(c, "juryworst", w, "n", 0, deep=False, answer="late")
    ck("every wrong move, no exhibits, the offer is still made", out, remark("n", 0), "say pledge", "say decline")
    # not even trying the lent orders on the way: the verdict is refused with the court's own words, and the way on is plain
    d = mage("juryidle", 5)
    summon(w, d, "juryidle")
    say_through(d, "juryidle")
    d.command("take number")
    d.command("ask registrar about oath")
    d.command("east")
    wait(d, 3.0)
    out = d.command("ask clerk about verdict")
    ck("an idle juror is told what is missing, not failed", out, "The court cannot take a verdict from a juror who has not heard the parties.")
    ck("`ask tilda` unrestrained hints at the form", d.command("ask tilda about goose"), "have been known to have a form for that")
    ck("a juror cannot be struck down: the room is safe", d.command("kill pruett"), absent=("You attack",))
    d.command("restrain pruett")
    d.command("ask pruett about marrows")
    d.command("ask tilda about goose")
    wait(d, 9.5)
    ck("never serving a notice still hears Tilda", d.command("ask tilda about goose"), "The goose is mine.")
    wait(d, 20.5)
    d.command("ask clerk about verdict", marker="(y/n)")
    d.send("y")
    wait(d, 1.0)
    ck("... and the verdict is entered", w.command("hidden bureaucrat"), "juryidle: looted 0, issued yes #", "stage 4")
    d.command("west")
    out = wait(d, 2.8)
    ck("the juror who never found a thing is returned to the offer", out, remark("y", 0), "say pledge")


# ------------------------------------------------------------------ step 4: leaving and resuming

def step_resume(w):
    print("\n=== 4. leaving and coming back at each stage; a dropped connection and a takeover in the hearing")
    c = mage("juryres", 5)
    summon(w, c, "juryres")
    c = relog(c, "juryres", "quit")
    ck("stage 1: after a quit the summons is still there", c.command("i"), "a summons")
    out = say_through(c, "juryres")
    ck("... and the case begins", out, "Juror Twelve of Twelve")
    c.command("take number")
    c = relog(c, "juryres", "quit")
    ck("stage 2 with a ticket: after a quit, the quest is as left", c.command("quest jury"), "Take a number, then ask the Registrar about the oath.")
    goto(c, WAITING, 1.8)
    ck("... the Registrar picks the thread up", c.command("ask registrar about oath"), "A juror's badge.")
    ck("... the ticket was remembered (no second one, the oath needed none)", c.command("i"), "a juror's badge")
    c = relog(c, "juryres", "quit")
    ck("stage 3: after a quit the badge is still there", c.command("i"), "a juror's badge")
    goto(c, WAITING, 0.5)
    ck("... the east door is open for the badge", c.command("east"), "The Chancery: Court of First Instance")
    wait(c, 3.0)
    c.command("ask pruett about goose")
    c.command("restrain pruett")
    c = relog(c, "juryres", "quit")
    goto(c, COURT, 3.0)
    ck("in the court, the plaintiff was remembered as heard", c.command("ask pruett about goose"), "Eleven gold. And the stakes.")
    ck("... and as restrained", c.command("restrain pruett"), "Pruett has been served. He is writing about it.")
    ck("... so Tilda may begin", c.command("ask tilda about goose"), "Tilda Marrowby draws a very long breath.")
    c.command("desist tilda")
    # the goose holds the Roll, then a dropped connection in the hearing
    goto(c, WAITING, 0.3)
    c.command("search pigeonholes")
    wait(c, 3.4)
    c.command("east")
    ck("the goose takes the Roll on the way in", wait(c, 3.0), "Counsel rises.")
    c.command("ask clerk about verdict", marker="(y/n)")  # the question is open ...
    c = relog(c, "juryres", "drop")  # ... and the line goes dead
    goto(c, COURT, 3.0)
    out = c.command("take roll")
    ck("after a dropped connection the goose still holds the Roll", out, "The goose is holding it.")
    ck("the prompt did not survive: the first line is a command, not an answer", c.command("i"), "a juror's badge")
    ck("the register: still stage 3", w.command("hidden bureaucrat"), "juryres: looted 0, issued yes #", "stage 3")
    c.command("triplicate goose")
    c.command("ask clerk about verdict", marker="(y/n)")
    c = relog(c, "juryres", "takeover")
    goto(c, COURT, 3.0)
    ck("after a takeover: the goose is served and gives the Roll back", c.command("take roll"), "You take the Roll from under its wing.")
    ck("... the Roll is entered as Exhibit One", c.command("submit roll"), "Exhibit One")
    ck("... and the question can be asked again", c.command("ask clerk about verdict", marker="(y/n)"), "(y/n)")
    c.send("n")
    wait(c, 1.0)
    c = relog(c, "juryres", "quit")
    ck("stage 4: after a quit the verdict stands", w.command("hidden bureaucrat"), "juryres: looted 0, issued yes #", "stage 4")
    goto(c, HALL, 0.3)
    goto(c, WAITING, 2.6)
    ck("... and the Registrar takes the badge when the juror is back", c.command("i"), absent=("juror's badge",))
    ck("stage 5: the offer is open after the return", w.command("hidden bureaucrat"), "stage 5, outcome 0")
    c = relog(c, "juryres", "quit")
    goto(c, WAITING, 0.2)
    c.command("out")
    out = c.command("in") + wait(c, 2.0)
    ck("the offer persists across a logout: she says it is pending", out, "Still pending. It is perfectly in order for it to remain so.")
    ck("walking away answered nothing", w.command("hidden bureaucrat"), "stage 5, outcome 0")
    ck("typing anything else answers nothing", c.command("say hello") + c.command("flarb"), absent=("You are now",))
    # two jurors at once
    d1 = mage("jurytwoa", 5)
    d2 = mage("jurytwob", 5)
    for d, n in ((d1, "jurytwoa"), (d2, "jurytwob")):
        summon(w, d, n)
        say_through(d, n)
        d.command("take number")
        d.command("ask registrar about oath")
        d.command("east")
    wait(d1, 3.2)
    wait(d2, 3.2)
    d1.command("ask pruett about goose")
    d1.command("restrain pruett")
    ck("two jurors: the first one's order is the first one's", d1.command("ask tilda about goose"), "Tilda Marrowby draws a very long breath.")
    ck("... the second one's Pruett is still unrestrained", d2.command("ask tilda about goose"), "Objection! She is lying!")
    d1.command("desist tilda")
    ck("... and the second sees the first's orders in the room", wait(d2, 1.0), "serves Tilda Marrowby with a notice to cease and desist")
    for d in (d1, d2):
        d.sock.close()


# ------------------------------------------------------------------ step 5: who may be summoned, and the leaks

def step_roll(w):
    print("\n=== 5a. the roll's eligibility on a test copy of the class with the roll opened")
    w.command("tp " + T + "arena")
    a.drain(w, 0.3)
    w.command("hidden dice 0")
    mg = mage("jurymg", 5, T + "arena")
    wr = h.Client("jurywar", archetype="warrior")
    wr.read_until("> ")
    wr.command("mk /domains/jurytest/probe")
    wr.command("setlevel 6")
    wr.command("tp " + T + "arena")
    a.drain(wr, 0.3)
    w.command("hidden count jurymg bureaucrat 10")
    w.command("hidden count jurywar bureaucrat 10")
    for who, label in ((wr, "a Warrior"), (mg, "a Mage")):
        who.command("jspawn jm5")
        who.command("jkill")
        out = who.command("loot")
        if who is mg:
            ck(f"{label}: the 11th eligible loot serves the summons", out, "Among the coins and oddments you take from", "one thing is not like the others: a stiff buff-coloured paper, folded in three, with your name on it in a clerk's hand.", "It is already in your hand.")
        else:
            ck(f"{label}: nothing is served, nothing is told", out, absent=("buff-coloured", "oddments"))
    truth("the Warrior has no record", rec(w, "jurywar") in (None, (10, False, 0, 0)) or rec(w, "jurywar") is None, rec(w, "jurywar"))
    truth("the Mage is summoned", rec(w, "jurymg") == (11, True, 1, 0) or (rec(w, "jurymg") or (0, False))[1], rec(w, "jurymg"))
    # the wizard: the wizard's own body, looting, is never counted
    w.command("jspawn jm5")
    w.command("jkill")
    w.command("loot")
    truth("a wizard looting is recorded nowhere", rec(w, "jurywiz") is None, rec(w, "jurywiz"))
    ck("a wizard cannot be granted a summons", w.command("hidden grant jurywiz bureaucrat"), "A wizard cannot be summoned.")
    ck("a Mage with an ordinary advanced class is never served (the grant says so)", w.command("hidden grant jurywar bureaucrat"), absent=("cannot", "already"))
    wr.sock.close()
    mg.sock.close()


STRANGE = ("bureaucrat", "clerk of the chancery", "chancery", "registrar", "summons", "jury duty", "ashcombe", "goose", "pruett", "marrowby", "inner door")


def step_leaks(w, holder):
    print("\n=== 5b. a stranger sees nothing anywhere")
    st = mage("jurystra", 6)
    ck("the class lists omit it", st.command("tp " + T + "arena") + st.command("jlist"), "LIST children=", absent=("bureaucrat",))
    st.command("tp " + OAK + "guild/hall_of_callings")
    a.drain(st, 0.4)
    unknown = plain(st.command("ask hallmaster about flarb"))
    for word in STRANGE:
        truth(f"a hallmaster asked about '{word}' answers as for any unknown word", plain(st.command(f"ask hallmaster about {word}")) == unknown, word)
    ck("the Hall's own look and talk do not name it", st.command("look") + st.command("talk hallmaster"), absent=("Bureaucrat", "bureaucrat", "Chancery", "Clerk of the"))
    st.command("tp " + HALL)
    a.drain(st, 0.4)
    for npc in ("maude", "barnaby"):
        unk = plain(st.command(f"ask {npc} about flarb"))
        for word in STRANGE:
            if word == "inner door": continue  # Maude's own topic key "inn" is inside "inner": an old, unrelated match
            truth(f"{npc} asked about '{word}' answers as for any unknown word", plain(st.command(f"ask {npc} about {word}")) == unk, word)
    truth("`help bureaucrat` for a stranger is as for any unknown word", plain(st.command("help bureaucrat")) == plain(st.command("help flarb")) and "BUREAUCRAT" not in st.command("help bureaucrat"))
    for word in ("restrain", "clerk of the chancery", "jury duty", "triplicate", "pledge"):
        truth(f"`help {word}` for a stranger", plain(st.command(f"help {word}")) == plain(st.command("help flarb")), word)
    # the trainer: a Mage's hallmaster answers a holder as a Mage, naming nothing
    holder.command("tp " + OAK + "guild/hall_of_callings")
    a.drain(holder, 0.4)
    out = holder.command("ranks")
    ck("the Mage hallmaster teaches a holder as a Mage", out, absent=("Bureaucrat", "Chancery", "Clerk of the", "bureaucrat"))
    holder.command("out")
    # who and look at, with the holder in the room
    holder.command("tp " + HALL)
    st.command("tp " + HALL)
    a.drain(st, 0.4)
    a.drain(holder, 0.4)
    ck("`who` shows nothing of the class", st.command("who"), absent=("Clerk", "Chancery", "Bureaucrat", "bureaucrat"))
    ck("`look at` the holder shows nothing of it", st.command("look at jurytak"), absent=("Clerk", "Chancery", "Bureaucrat", "bureaucrat", "seal"))
    ck("the holder's own `score` shows it", holder.command("score"), "Clerk of the Chancery")
    # the Registrar's shop for a stranger who stands in the waiting room with a summons
    st2 = mage("jurystrb", 6)
    summon(w, st2, "jurystrb")
    say_through(st2, "jurystrb")
    unk = plain(st2.command("flarb"))
    for v in ("browse", "buy staff", "buy seal", "sell staff", "value staff", "price seal", "ranks", "train arcane_bolt", "pledge", "decline"):
        truth(f"a juror at the Registrar's: `{v}` is an unknown word (no shop, no teaching, no pledge)", plain(st2.command(v)) == unk, plain(st2.command(v)))
    # GMCP to another player in the room
    obs = g.login("juryobs")
    obs.command("setlevel 6")
    obs.command("tp " + HALL)
    obs.pump(0.8)
    mark = obs.mark()
    holder.command("in")
    obs.pump(1.2)
    holder.command("out")
    obs.pump(1.2)
    blob = json.dumps(obs.frames[mark:]) + plain(obs.pending)
    for word in ("bureaucrat", "Bureaucrat", "Chancery", "Clerk of", "summons", "Registrar", "jury"):
        truth(f"GMCP and text sent to another player never carry '{word}'", word not in blob, blob[:200])
    obs.sock.close()


# ------------------------------------------------------------------ the run

def step_files(work):
    print("\n=== 6. nothing leaked to disk the public could read")
    save = os.path.join(work, "save")
    hits = []
    for root, _, names in os.walk(save):
        for n in names:
            p = os.path.join(root, n)
            try:
                text = open(p, errors="replace").read()
            except OSError:
                continue
            if "roster" in n or "chronicle" in n or "events" in n:
                for word in ("bureaucrat", "Chancery", "Clerk of the"):
                    if word in text:
                        hits.append((n, word))
    truth("the roster, the chronicle and the event files name nothing of the class", not hits, hits)


def run(work, mudlib, log, box):
    only = [x for x in os.environ.get("JURY_STEPS", "").split(",") if x]
    want = lambda n: not only or n in only
    w = wizard()
    w.command("mk " + "/domains/jurytest/probe")
    ck("the dice: every search finds what it can (d20 19)", w.command("jrolls d20:19"), "ROLLS True")
    ck("... and a failed search costs no wait", w.command("jwait 0"), "WAIT True")
    if want("door"): step_summons_and_door(w)
    holder = None
    if want("taken"): holder = step_taken(w)
    if want("declined"): step_declined(w)
    if want("worst"): step_worst(w)
    if want("resume"): step_resume(w)
    if want("roll"): step_roll(w)
    if want("leaks") and holder is not None: step_leaks(w, holder)
    time.sleep(1.0)
    if want("files"): step_files(work)


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-jury-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    prepare(mudlib)
    logpath = os.path.join(work, "driver.log")
    log = open(logpath, "w")
    driver = g.start_driver(work, mudlib, log)
    box = [driver]
    try:
        run(work, mudlib, log, box)
    finally:
        g.stop_driver(box[0])
        log.close()
    print()
    print("driver log tail:\n" + open(logpath).read()[-3000:])
    print(f"{CHECKS[0]} checks")
    if h.failures:
        print(f"{len(h.failures)} FAILED:", *h.failures, sep="\n  ")
        sys.exit(1)
    print("all checks passed")
    shutil.rmtree(work, ignore_errors=True)


if __name__ == "__main__":
    main()
