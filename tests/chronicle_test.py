#!/usr/bin/env python3
"""Test for the events / ESS / Renown / Chronicle pipeline (docs/CHRONICLE.md, docs/CHRONICLE_IMPL.md).

Runs the driver on its own copy of the mudlib. Three things are added to that copy only:
  * the lines docs/CHRONICLE_IMPL.md asks for in /secure/master (so the daemons may save and are preloaded),
  * a test harness object in /secure (tests/fixtures/chron_harness.cs), which can report events for any player,
  * a test event group /events/test.cs with event types whose ESS is exactly their category.
A level 10 wizard (alice) drives the harness; the characters who make history are ordinary players.

Usage (after `dotnet build`):  LESMUDOS_TEST_PORT=4817 python3 tests/chronicle_test.py
"""
import datetime
from fractions import Fraction
import os
import re
import shutil
import socket
import subprocess
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4817")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402

ROOT = h.ROOT
SECRET = h.SECRET


def ck(label, cond, detail=""):
    h.check(label, "ok" if cond else "FAILED " + str(detail), "ok")


def norm(text):
    return text.replace("\r", "")


def rep(c, who, event, subject="", **names):
    line = f"hrep {who} {event} {subject}" + "".join(f" {k}={v.replace(' ', '~')}" for k, v in names.items())
    text = norm(c.command(line.rstrip(), "hend"))
    m = re.search(r"report: (.*)", text)
    raw = m.group(1) if m else "NO REPORT: " + text
    out = {"raw": raw, "pre": text}
    for k, v in re.findall(r"(\w+)=(\S*)", raw):
        out[k] = v
    out["entry"] = int(out.get("entry", 0))
    out.setdefault("rumor", "0")
    return out


def q(c, daemon, method, *args):
    line = f"hq {daemon} {method} " + " ".join(str(a).replace(" ", "~") for a in args)
    text = norm(c.command(line.rstrip(), "hend"))
    m = re.search(r"hq: (.*)\nhend", text, re.S)
    return m.group(1).rstrip("\n") if m else "NO ANSWER: " + text


def drain(c, t=0.7):
    return norm(c.read_until("\x00never", t))


def day(y, m, d):
    return (datetime.date(y, m, d) - datetime.date(1970, 1, 1)).days


def ess_model(cat, imp, rar, per, part, mult, first):
    """The spec's formula, written out again here: sum, times the multipliers, round half up."""
    v = Fraction(cat + imp + part + rar + per) * Fraction(mult) * (2 if first else 1)
    return int((v + Fraction(1, 2)) // 1)


TEST_EVENTS = [(f"e{n}", n) for n in (0, 99, 100, 249, 250, 499, 500, 999, 1000, 2499, 2500, 4999, 5000)]


def test_group():
    defs = []
    for name, n in TEST_EVENTS:
        defs.append(f"id=test.{name}\ntitle=Test {n}\nkind=Victory\nchron=Combat\ncategory={n}\nfirst=no\nonce=no\nrepeat=same")
    defs.append("id=test.part\ntitle=Participation\nkind=World\ncategory=0\nfirst=no\nonce=no\nrepeat=same")
    defs.append("id=test.flags\ntitle=Flags\nkind=World\ncategory=10\nimpact=Guild\nrarity=Rare\npersistence=LongTerm\nflags=storyline,regional,world,seasonal,developer\nfirst=yes")
    defs.append("id=test.once\ntitle=Once\nkind=World\ncategory=0\nfirst=no\nrepeat=same\nrenown=7")
    defs.append("id=test.kill\ntitle=Rat\nkind=Victory\ncategory=0\nfirst=no\nonce=no\nrepeat=same")
    defs.append("id=test.purge\ntitle=Rat Plague\nkind=World\nchron=World Events\ncategory=300\nfirst=no\nonce=no\nrepeat=same\naggregate_of=test.kill\naggregate_subject=rat\naggregate_step=3\nheadline=The Rat Plague of {region}\ntext=By {date}, {count} rats had fallen in {region}.")
    for n in (1, 98, 364, 500, 1500, 2500, 5000, 14975):
        defs.append(f"id=test.a{n}\ntitle=Award {n}\nkind=World\ncategory=0\nfirst=no\nonce=no\nrepeat=same\nrenown={n}")
    body = ",\n".join('        @"' + d + '"' for d in defs)
    return ("public class EventsTest : MudObject\n{\n    public List<string> Definitions() => new()\n    {\n"
            + body + ",\n    };\n}\n")


FORGER = """
public class Forger : MudObject
{
    public override void Init() { AddAction("forge", Forge); }
    private bool Forge(string args)
    {
        var bob = Users().First(u => u.Call<string>("QueryName") == "Bob");
        var ev = LoadObject("/secure/events");
        var r1 = ev.Call<string>("Report", "quest.local", bob, new List<ObjectRef> { bob }, (ObjectRef?)null, "forged", new Dictionary<string, string>());
        var r2 = LoadObject("/secure/renown").Call<List<string>>("Award", "bob", 1000, "forged").Count;
        var r3 = LoadObject("/secure/chronicle").Call<int>("AddEntry", new Dictionary<string, string> { { "headline", "fake" } });
        var r4 = LoadObject("/secure/rumors").Call<bool>("AddRumor", "Greenvale", "fake");
        var r5 = LoadObject("/secure/chronicle").Call<bool>("MarkOccurred", "forged|", new Dictionary<string, string>());
        var r6 = ev.Call<string>("Report", "wiz.fake", bob, new List<ObjectRef> { bob }, (ObjectRef?)null, "", new Dictionary<string, string>());
        var r7 = LoadObject("/secure/renown").Call<string>("ChooseTitle", "bob", "Wanderer");
        Write("forge: " + r1 + " ## " + r2 + " ## " + r3 + " ## " + r4 + " ## " + r5 + " ## " + r6 + " ## " + r7.Trim() + "\\nhend\\n");
        return true;
    }
}
"""

MASTER_OLD1 = "if (path == Vault) return !caller.IsClone && caller.ProgramPath == Vault;"
MASTER_NEW1 = MASTER_OLD1 + '\n        if (path is "/secure/chronicle" or "/secure/renown" or "/secure/rumors" or "/secure/events") return !caller.IsClone && caller.ProgramPath == path;'
MASTER_OLD2 = "new[] { WizList, Vault,"
MASTER_NEW2 = 'new[] { WizList, Vault, "/secure/chronicle", "/secure/renown", "/secure/rumors", "/secure/events",'


def start(work, mudlib):
    dll = os.path.join(ROOT, "src", "LesMudOS.Driver", "bin", "Debug", "net10.0", "LesMudOS.Driver.dll")
    log = open(os.path.join(work, "driver.log"), "a")
    p = subprocess.Popen(["dotnet", dll, "--mudlib", mudlib, "--save", os.path.join(work, "save"), "--port", str(h.PORT)],
                         stdout=log, stderr=subprocess.STDOUT, env={**os.environ, "LESMUDOS_ADMIN_SECRET": SECRET})
    for _ in range(150):
        try:
            socket.create_connection(("127.0.0.1", h.PORT), timeout=0.3).close()
            break
        except OSError:
            time.sleep(0.2)
    return p, log


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-chron-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    shutil.copy(os.path.join(ROOT, "tests", "fixtures", "chron_harness.cs"), os.path.join(mudlib, "secure", "chron_harness.cs"))
    h.write(mudlib, "/events/test", test_group())
    os.makedirs(os.path.join(mudlib, "wiz", "alice"), exist_ok=True)
    h.write(mudlib, "/wiz/alice/forger", FORGER)
    start_day = (datetime.datetime.now(datetime.timezone.utc).date() - datetime.date(1970, 1, 1)).days
    driver, log = start(work, mudlib)
    try:
        run(work, mudlib, driver, start_day)
    finally:
        if driver.poll() is None:
            driver.kill()
        log.close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-4000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\n{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


def run(work, mudlib, driver, start_day):
    def login(name):
        c = h.Client(name)
        c.read_until("> ")
        return c
    a = login("alice")
    a.command("promote " + SECRET)
    b, c, d, e = login("bob"), login("carol"), login("dave"), login("erin")
    for cl in (a, b, c, d, e):
        cl.command("look")
    a.command("clone /secure/chron_harness")
    a.command("clone /std/chronicle_ledger")
    drain(b)

    # ---------------- persistence is on, the calendar starts ----------------
    check = h.check
    check("the Chronicle can save (master lines applied)", q(a, "chronicle", "QueryPersistent"), "True")
    check("the Renown ledger can save", q(a, "renown", "QueryPersistent"), "True")
    check("the event service can save", q(a, "events", "QueryPersistent"), "True")
    check("the rumor daemon can save", q(a, "rumors", "QueryPersistent"), "True")
    epoch = int(q(a, "chronicle", "QueryEpochDay"))
    ck("Year 1 begins on the day the Chronicle first started", abs(epoch - start_day) <= 1, (epoch, start_day))
    check("today is in Year 1", q(a, "chronicle", "DateText"), "Day of", "Year 1")
    check("the date command text", q(a, "chronicle", "DateCommand"), "Today is the ", ", Year 1.")

    # ---------------- calendar arithmetic ----------------
    def cal(epoch_day, offset):
        return q(a, "chronicle", "DateFor", f"l:{epoch_day}", f"l:{epoch_day + offset}")
    e0 = day(2027, 1, 1)
    check("day 0 is the 1st of Deepfrost", cal(e0, 0), "the 1st Day of Deepfrost, Year 1")
    check("day 29 is the 30th of Deepfrost", cal(e0, 29), "the 30th Day of Deepfrost, Year 1")
    check("day 30 starts Thawmoon", cal(e0, 30), "the 1st Day of Thawmoon, Year 1")
    check("the 17th Day of Harvest", cal(e0, 226), "the 17th Day of Harvest, Year 1")
    check("last day of the twelfth month", cal(e0, 359), "the 30th Day of Longnight, Year 1")
    check("the Turning begins", cal(e0, 360), "the 1st Day of the Turning, Year 1")
    check("the Turning has five days in an ordinary year", cal(e0, 364), "the 5th Day of the Turning, Year 1")
    check("year rollover", cal(e0, 365), "the 1st Day of Deepfrost, Year 2")
    e1 = day(2028, 1, 1)
    check("the Turning has six days in a leap year", cal(e1, 365), "the 6th Day of the Turning, Year 1")
    check("year rollover after a leap-year Turning", cal(e1, 366), "the 1st Day of Deepfrost, Year 2")
    y3 = day(2029, 1, 1) + 7 * 30 + 16
    check("Year 3 date reads like the design's example", cal(e0, y3 - e0), "the 17th Day of Harvest, Year 3")
    check("ordinals", "|".join(cal(e0, n)[4:8] for n in (1, 2, 10, 11, 12, 20, 21, 22)), "2nd |3rd |11th|12th|13th|21st|22nd|23rd".replace("|", "|"))
    check("leap year length when 29 Feb falls inside", q(a, "chronicle", "YearLength", f"l:{day(2027, 6, 1)}"), "366")
    check("29 Feb on day 365 is outside a 365-day window", q(a, "chronicle", "YearLength", f"l:{day(2027, 3, 1)}"), "365")
    check("a year starting on 29 Feb is 366 days", q(a, "chronicle", "YearLength", f"l:{day(2028, 2, 29)}"), "366")
    check("a date before the epoch clamps to day 1", cal(e0, -5), "the 1st Day of Deepfrost, Year 1")

    # ---------------- ESS: the design's table, solo player ----------------
    table = [
        ("kill.ordinary", 0, 0), ("quest.clear_the_cellar", 220, 10), ("boss.goblin_overseer", 450, 38),
        ("quest.overseers_orders", 1069, 19), ("discover.area", 10, 5), ("discover.hidden", 100, 25),
        ("discover.secret_dungeon", 600, 100), ("discover.great_stump", 750, 25), ("travel.pass", 20, 10),
        ("level.milestone", 4200, 200), ("class.advanced", 4020, 10), ("renown.recognized", 20, 10),
        ("renown.acclaimed", 50, 25), ("renown.renowned", 200, 100), ("renown.legendary", 1000, 500),
        ("aggregate.goblin_purge", 1125, 563),
    ]
    for ev, first, later in table:
        got = (q(a, "events", "PreviewEss", ev, "i:1", "b:1"), q(a, "events", "PreviewEss", ev, "i:1", "b:0"))
        check(f"ESS {ev}: first {first}, later {later}", "%s/%s" % got, f"{first}/{later}")
    # the same numbers from the formula written out in this script
    ck("formula by hand: Overseer first = (25+100+25) x 1.5 x 2", ess_model(25, 100, 25, 0, 0, Fraction(3, 2), True) == 450)
    ck("formula by hand: story quest first rounds 1068.75 up", ess_model(10, 250, 25, 0, 0, Fraction(3, 2) * Fraction(5, 4), True) == 1069)
    check("a second look at the same event scores the same (deterministic)", q(a, "events", "PreviewEss", "boss.goblin_overseer", "i:1", "b:1"), "450")
    flagged = [("all five multipliers, repeat, solo: 110 x 9.375", 1, 0, 1031), ("all five, first, solo: 260 x 18.75", 1, 1, 4875),
               ("three players, first: 270 x 18.75 = 5062.5 rounds up", 3, 1, 5063)]
    for label, players, first, want in flagged:
        check(label, q(a, "events", "PreviewEss", "test.flags", f"i:{players}", f"b:{first}"), str(want))
    for players, want in [(1, 0), (2, 10), (5, 10), (6, 25), (10, 25), (11, 50), (25, 50), (26, 100), (50, 100), (51, 250), (100, 250), (101, 500)]:
        check(f"participation score for {players} players is {want}", q(a, "events", "PreviewEss", "test.part", f"i:{players}", "b:0"), str(want))
    check("unknown event type previews as -1", q(a, "events", "PreviewEss", "nothing.here", "i:1", "b:0"), "-1")

    # ---------------- wizards make no history ----------------
    r = rep(a, "alice", "discover.hidden", "hidden-cache", name="Hidden Cache")
    check("a level 10 wizard's report is ignored", r["raw"], "ignored: wizards make no history")
    check("the wizard is told in one line", r["pre"], "Wizards make no history.")
    check("no first was recorded", q(a, "chronicle", "HasOccurred", "discover.hidden|hidden-cache"), "False")
    check("no Renown for the wizard", q(a, "renown", "QueryRenown", "alice"), "0")
    check("nothing in the Chronicle", q(a, "chronicle", "QueryEntryCount"), "0")
    check("no rumor", q(a, "rumors", "QueryCount", "Greenvale"), "0")
    a.command("promote dave 1")
    r = rep(a, "dave", "discover.hidden", "hidden-cache", name="Hidden Cache")
    check("a level 1 wizard makes no history either", r["raw"], "ignored: wizards make no history")
    check("still no first after the level 1 wizard", q(a, "chronicle", "HasOccurred", "discover.hidden|hidden-cache"), "False")
    a.command("promote dave 0")
    r = rep(a, "bob", "discover.hidden", "hidden-cache", name="Hidden Cache")
    check("an ordinary player doing it afterward still gets the first", r["raw"], "ok ess=100", "first=1")
    check("a 100 ESS first is a rumor, not an entry", f"{r['entry']} {r['rumor']}", "0 1")
    r = rep(a, "alice,carol", "discover.area", "vincents-hollow", name="Vincent's Hollow")
    check("a mixed pair records the player alone", r["raw"], "credited=carol", "first=1", "ess=10")
    check("the wizard is told even in a mixed pair", r["pre"], "Wizards make no history.")
    check("the first discoverer is the player", q(a, "chronicle", "QueryOccurred", "discover.area|vincents-hollow", "holders"), "carol")
    r = rep(a, "dave", "discover.area", "forgotten-cellars", name="The Forgotten Cellars")
    check("a character demoted to level 0 earns history normally", r["raw"], "ok ess=10", "first=1")
    check("nothing was granted retroactively for the wizard period", q(a, "renown", "QueryRenown", "dave"), "26")

    # ---------------- Vincent's Hollow ----------------
    r = rep(a, "bob", "boss.goblin_overseer", "goblin-overseer", name="Goblin Overseer")
    check("first Overseer kill: 450, a Historical entry", r["raw"], "ess=450", "class=Historical", "first=1")
    eid = r["entry"]
    ck("the entry exists", eid > 0, r)
    ent = q(a, "chronicle", "EntryText", f"i:{eid}")
    check("the entry has the named headline", ent, "The Quieting of the Cellars")
    check("the entry is dated in Year 1 and names the actor and place", ent, "Year 1", "Bob brought down the Goblin Overseer in the tunnels beneath The Village of Vincent's Hollow")
    check("the entry carries the significance text", ent, "the village slept easier")
    check("Renown: 30 (hidden cache) + 10 + 15 first + 5 Historical bonus", q(a, "renown", "QueryRenown", "bob"), "60")
    r = rep(a, "carol", "boss.goblin_overseer", "goblin-overseer", name="Goblin Overseer")
    check("a later character scores the repeat: 38, no entry", f"{r['ess']} {r['entry']} {r['first']}", "38 0 0")
    check("a later character earns the boss Renown (10) on top of 26", q(a, "renown", "QueryRenown", "carol"), "36")
    r = rep(a, "bob", "boss.goblin_overseer", "goblin-overseer", name="Goblin Overseer")
    check("the same character killing it again is not credited twice", r["raw"], "ignored: already credited")
    check("and Renown did not grow", q(a, "renown", "QueryRenown", "bob"), "60")
    r = rep(a, "bob", "quest.clear_the_cellar")
    check("first Clear the Cellar: 220 is a rumor", f"{r['ess']} {r['entry']} {r['rumor']}", "220 0 1")
    r = rep(a, "carol", "quest.clear_the_cellar")
    check("later Clear the Cellar: 10, nothing", f"{r['ess']} {r['entry']} {r['rumor']}", "10 0 0")
    r = rep(a, "bob", "quest.overseers_orders")
    check("first story quest: 1069, Legendary", r["raw"], "ess=1069", "class=Legendary")
    check("a Legendary entry is featured", q(a, "chronicle", "QueryEntry", f"i:{r['entry']}", "featured"), "1")
    check("Legendary entries get special formatting", q(a, "chronicle", "EntryText", f"i:{r['entry']}"), "=== A LEGENDARY ENTRY ===")
    r = rep(a, "carol", "quest.overseers_orders")
    check("later story quest: 19", r["raw"], "ess=19")
    r = rep(a, "bob", "discover.secret_dungeon", "forgotten-cellars", name="The Forgotten Cellars")
    check("first Forgotten Cellars: 600, Major Historical, featured", r["raw"], "ess=600", "class=MajorHistorical")
    r = rep(a, "carol", "discover.secret_dungeon", "forgotten-cellars", name="The Forgotten Cellars")
    check("finding the Cellars for oneself later is still talked about (100)", f"{r['ess']} {r['rumor']} {r['entry']}", "100 1 0")

    # the Great Stump
    r = rep(a, "bob", "discover.great_stump", "great-stump")
    check("the Great Stump first: 750 and an entry", r["raw"], "ess=750", "first=1")
    stump = r["entry"]
    ck("it made an entry", stump > 0)
    check("a recorded first, with its holder", q(a, "chronicle", "FirstsText"), "The Great Stump", "Bob")
    r = rep(a, "carol", "discover.great_stump", "great-stump")
    check("the second finder gets 25 and no entry", f"{r['ess']} {r['entry']} {r['first']}", "25 0 0")
    check("the first stays with the first finder", q(a, "chronicle", "QueryOccurred", "discover.great_stump|great-stump", "holders"), "bob")
    check("the firsts list still shows only Bob", q(a, "chronicle", "FirstsText"), "Bob", absent=("Carol",))
    r = rep(a, "bob", "discover.great_stump", "great-stump")
    check("the first cannot be recorded a second time", r["raw"], "ignored: already credited")
    check("the Stump finder: +5 +25 first +10 Major Historical bonus (176 in all)", q(a, "renown", "QueryRenown", "bob"), "176")

    # ---------------- thresholds (erin) ----------------
    expect = {0: "Routine", 99: "Routine", 100: "Noteworthy", 249: "Noteworthy", 250: "Historical", 499: "Historical",
              500: "MajorHistorical", 999: "MajorHistorical", 1000: "Legendary", 2499: "Legendary", 2500: "RealmShaping",
              4999: "RealmShaping", 5000: "WorldDefining"}
    before_entries = int(q(a, "chronicle", "QueryEntryCount"))
    entries = {}
    for n in (0, 99, 100, 249, 250, 499, 500, 999, 1000, 2499, 2500, 4999, 5000):
        r = rep(a, "erin", f"test.e{n}")
        entries[n] = r
        want_entry = n >= 250
        want_rumor = n >= 100
        check(f"ESS {n}: {expect[n]}, entry {want_entry}, rumor {want_rumor}", f"{r['raw'].split(' first')[0]} {r['entry'] > 0} {r['rumor'] == '1'}",
              f"ess={n} class={expect[n]} {want_entry} {want_rumor}")
    check("500 and above are featured, 499 is not", "".join(q(a, "chronicle", "QueryEntry", f"i:{entries[n]['entry']}", "featured") for n in (499, 500)), "01")
    check("2500 gets a generated historical name", q(a, "chronicle", "QueryEntry", f"i:{entries[2500]['entry']}", "headline"), "The Fall of Test 2500")
    check("below 2500 there is no generated name", q(a, "chronicle", "QueryEntry", f"i:{entries[2499]['entry']}", "headline"), "Test 2499")
    check("class bonus Renown: 5+5+10+10+25+25+50+50+100 = 280", q(a, "renown", "QueryRenown", "erin"), "280")
    check("280 Renown is the Recognized tier", q(a, "renown", "QueryTier", "erin"), "Recognized")
    check("a wizard-free 5000 event: Hall of Legends has erin", q(a, "chronicle", "LegendsText"), "Erin")

    # ---------------- once per character, aggregation ----------------
    r1 = rep(a, "dave", "test.once")
    r2 = rep(a, "dave", "test.once")
    check("a once-only event is credited once", r2["raw"], "ignored: already credited")
    check("and Renown 7 was given once", q(a, "renown", "QueryRenown", "dave"), str(26 + 7))
    for i in range(1, 6):
        r = rep(a, "bob", "test.kill", "rat")
    check("five rats counted", q(a, "events", "QueryCounter", "test.kill", "Greenvale", "rat"), "5")
    entries_before = int(q(a, "chronicle", "QueryEntryCount"))
    rep(a, "bob", "test.kill", "rat")
    ck("the sixth rat crosses the aggregate step: a new entry", int(q(a, "chronicle", "QueryEntryCount")) == entries_before + 1)
    last = int(q(a, "chronicle", "QueryEntryCount"))
    check("aggregate entry reads as a named event", q(a, "chronicle", "EntryText", f"i:{last}"), "The Rat Plague of Greenvale", "6 rats had fallen")
    rep(a, "alice", "test.kill", "rat")
    check("a wizard's kill is not counted", q(a, "events", "QueryCounter", "test.kill", "Greenvale", "rat"), "6")
    r = rep(a, "bob", "kill.ordinary", "goblin", name="goblin")
    check("a shipped kill is counted (and loads the aggregate group)", q(a, "events", "QueryCounter", "kill.ordinary", "Greenvale", "goblin"), "1")
    check("the catalogue has no errors", q(a, "events", "QueryCatalogueErrors"), "")

    # ---------------- Renown ladder (carol) ----------------
    def tier(n):
        return q(a, "renown", "QueryTier", n)
    base = int(q(a, "renown", "QueryRenown", "carol"))
    ck("carol starts below Acclaimed", base < 100, base)
    # bring carol to exactly 1 -> restart plan: use dave-style fresh character instead: frank not online, so use carol's number
    need = lambda target: target - int(q(a, "renown", "QueryRenown", "carol"))
    check("carol is Local with the Local Hero title", tier("carol") + q(a, "renown", "QueryTitles", "carol"), "Local", "Local Hero")
    rep(a, "carol", "test.a98")
    rep(a, "carol", "test.a1")
    rep(a, "carol", "test.a1")
    now = int(q(a, "renown", "QueryRenown", "carol"))
    ck("carol crossed 100 into Recognized", now >= 100 and tier("carol") == "Recognized", now)
    check("the Wanderer title was earned", q(a, "renown", "QueryTitles", "carol"), "Wanderer")
    drain(c)
    rep(a, "carol", "test.a364")
    check("Acclaimed: Champion title", q(a, "renown", "QueryTitles", "carol"), "Champion")
    check("NPC recognition line for Acclaimed", q(a, "renown", "RecognitionLine", "carol"), "I have heard stories about your travels.")
    check("NPC recognition for Recognized", q(a, "renown", "RecognitionLine", "erin"), "You seem familiar.")
    check("no recognition below Recognized", q(a, "renown", "RecognitionLine", "nobody"), "")
    told = drain(c)
    check("the player is told of the tier and the title", told, "Your renown grows. You are now known as", "You have earned the title:")
    rep(a, "carol", "test.a500")
    check("Renowned: Defender of Greenvale, and a Noteworthy tier event is a rumor", q(a, "renown", "QueryTitles", "carol"), "Defender of Greenvale")
    rep(a, "carol", "test.a1500")
    check("Famous: Voice of the People", q(a, "renown", "QueryTitles", "carol"), "Voice of the People")
    rep(a, "carol", "test.a2500")
    check("Famous and Heroic titles", q(a, "renown", "QueryTitles", "carol"), "Voice of the People", "Hero of Hollowmere")
    rep(a, "carol", "test.a5000")
    check("10000: Legendary, Living Legend", tier("carol") + q(a, "renown", "QueryTitles", "carol"), "Legendary", "Living Legend")
    leg = int(q(a, "chronicle", "QueryEntryCount"))
    check("the Legendary tier raised its own featured event", q(a, "chronicle", "QueryEntry", f"i:{leg}", "headline"), "Carol Is a Living Legend")
    now = int(q(a, "renown", "QueryRenown", "carol"))
    rep(a, "carol", "test.a14975")
    check("a jump past 25000 makes carol Mythic", tier("carol"), "Mythic")
    check("The Mythic title", q(a, "renown", "QueryTitles", "carol"), "The Mythic")
    check("Mythic renown is a World-Defining first", q(a, "chronicle", "QueryEntry", f"i:{int(q(a, 'chronicle', 'QueryEntryCount'))}", "ess"), "9000")
    check("Hall of Legends has the Mythic character", q(a, "chronicle", "LegendsText"), "Carol")
    check("Hall of Heroes has carol (lifetime ESS over 5000)", q(a, "chronicle", "HeroesText"), "Carol")
    check("the renown command text", q(a, "renown", "RenownText", "carol"), "Renown:", "(Mythic)", "highest tier", "Titles earned:")
    check("Infamy is not shown at zero", q(a, "renown", "RenownText", "carol"), absent=("Infamy",))
    check("score line shows the tier in words", q(a, "renown", "TierLine", "carol"), "Renown: Mythic")
    check("choosing a title", q(a, "renown", "ChooseTitle", "carol", "living~legend"), "You will be known as Living Legend.")
    check("the chosen title is remembered", q(a, "renown", "QueryActiveTitle", "carol"), "Living Legend")
    check("a title not earned is refused", q(a, "renown", "ChooseTitle", "erin", "The~Mythic"), "have not earned")
    check("title none", q(a, "renown", "ChooseTitle", "carol", "none"), "no title")
    check("TitleCommand works only from a player body", q(a, "renown", "TitleCommand", "Wanderer"), "Only a player")
    rep(a, "dave", "test.a5000")
    check("several tiers in one award grant every title", q(a, "renown", "QueryTitles", "dave"), "Local Hero", "Wanderer", "Champion", "Defender of Greenvale", "Voice of the People", "Hero of Hollowmere")

    # ---------------- milestones ----------------
    r = rep(a, "bob", "level.milestone", "10", level="10")
    check("first to level 10: 4200, Realm-Shaping", r["raw"], "ess=4200", "class=RealmShaping")
    check("it is named", q(a, "chronicle", "QueryEntry", f"i:{r['entry']}", "headline"), "The First to Reach Level 10")
    r = rep(a, "dave", "level.milestone", "10", level="10")
    check("the next to level 10 scores the Rare repeat: 200", f"{r['ess']} {r['entry']}", "200 0")
    r = rep(a, "carol", "level.milestone", "25", level="25")
    check("level 25 is its own first", r["raw"], "first=1", "ess=4200")
    r = rep(a, "bob", "class.advanced", "paladin", **{"class": "Paladin"})
    check("first advanced class: 4020", r["raw"], "ess=4020")
    check("its headline names the class", q(a, "chronicle", "QueryEntry", f"i:{r['entry']}", "headline"), "The First Paladin")
    check("level and class firsts are listed", q(a, "chronicle", "FirstsText"), "First to reach level 10", "First to reach level 25", "First Paladin")
    check("the Hall of Legends takes holders of milestone firsts", q(a, "chronicle", "LegendsText"), "Bob")

    # ---------------- forgery ----------------
    entries_now = q(a, "chronicle", "QueryEntryCount")
    bob_renown = q(a, "renown", "QueryRenown", "bob")
    rumors_now = q(a, "rumors", "QueryCount", "Greenvale")
    a.command("clone ~/forger")
    f = norm(a.command("forge", "hend"))
    check("an object in a wizard's home cannot report an event", f, "forge: refused: /wiz/alice/forger is not in a trusted tree")
    check("cannot raise Renown, write the Chronicle, a rumor or a first", f, " ## 0 ## 0 ## False ## False ## ")
    check("cannot define an event type or choose a title", f, "refused: /wiz/alice/forger", "Only trusted code")
    ck("nothing changed: entries, Renown, rumors", (q(a, "chronicle", "QueryEntryCount"), q(a, "renown", "QueryRenown", "bob"), q(a, "rumors", "QueryCount", "Greenvale")) == (entries_now, bob_renown, rumors_now))
    check("the refusal is logged", q(a, "events", "QueryRefusals"), "/wiz/alice/forger")
    check("an unknown event type from a trusted caller is refused", rep(a, "bob", "wiz.fake")["raw"], "refused: unknown event type")

    # ---------------- queries and the ledger object ----------------
    rec = norm(b.command("read chronicle", "Type 'chronicle <number>'"))
    check("read chronicle shows recent entries, featured starred", rec, "The Chronicle of Hollowmere", "*#")
    one = norm(b.command("chronicle %d" % stump))
    check("chronicle <number> shows one entry", one, "The Great Stump Found", "Named: Bob")
    check("chronicle search finds a character", norm(b.command("chronicle search bob")), "entries about 'bob'")
    check("chronicle search finds a place", norm(b.command("chronicle search cellars")), "The Quieting of the Cellars")
    check("chronicle about lists one character's entries", norm(b.command("chronicle about carol")), "Carol appears in")
    check("chronicle firsts", norm(b.command("chronicle firsts")), "Server firsts of Hollowmere")
    check("chronicle heroes", norm(b.command("chronicle heroes")), "Hall of Heroes", "Bob")
    check("chronicle legends", norm(b.command("chronicle legends")), "Hall of Legends")
    check("chronicle records", norm(b.command("chronicle records")), "World records", "Most renowned:", "Carol", "Vincent's Hollow", "First discoverers")
    check("a number out of range", norm(b.command("chronicle 9999")), "no entry numbered 9999")
    check("rumors are hearsay a gossip can draw", q(a, "rumors", "DrawRumor", "Greenvale"), "They say")
    ck("an unknown region has nothing to tell", q(a, "rumors", "DrawRumor", "Nowhere") == "")
    ck("the rumor list is capped at 50", int(q(a, "rumors", "QueryCount", "Greenvale")) <= 50)

    # ---------------- persistence across a restart ----------------
    snap = {
        "bob": q(a, "renown", "QueryRenown", "bob"), "carol": q(a, "renown", "QueryRenown", "carol"),
        "titles": q(a, "renown", "QueryTitles", "carol"), "entries": q(a, "chronicle", "QueryEntryCount"),
        "stump": q(a, "chronicle", "EntryText", f"i:{stump}"), "firsts": q(a, "chronicle", "FirstsText"),
        "counter": q(a, "events", "QueryCounter", "test.kill", "Greenvale", "rat"), "rumors": q(a, "rumors", "QueryCount", "Greenvale"),
        "epoch": q(a, "chronicle", "QueryEpochDay"), "contrib": q(a, "chronicle", "QueryContribution", "bob"),
    }
    for cl in (b, c, d, e):
        cl.send("quit")
    time.sleep(1)
    a.send("shutdown")
    driver.wait(timeout=15)
    for f_ in ("chronicle", "renown", "rumors", "events"):
        ck(f"save file for {f_} exists", os.path.exists(os.path.join(work, "save", "secure", f_ + ".json")))
    driver2, log2 = start(work, mudlib)
    try:
        a = login("alice")
        a.command("clone /secure/chron_harness")
        b, d = login("bob"), login("dave")
        check("Renown survives a restart", q(a, "renown", "QueryRenown", "bob") + "/" + q(a, "renown", "QueryRenown", "carol"), snap["bob"] + "/" + snap["carol"])
        check("titles survive", q(a, "renown", "QueryTitles", "carol"), *snap["titles"].split("\n"))
        check("entry count survives", q(a, "chronicle", "QueryEntryCount"), snap["entries"])
        check("an entry reads the same after the restart", q(a, "chronicle", "EntryText", f"i:{stump}"), snap["stump"].split("\n")[0], "Year 1")
        check("the firsts survive", q(a, "chronicle", "FirstsText"), *[l.strip() for l in snap["firsts"].split("\n")[1:3]])
        check("aggregate counters survive", q(a, "events", "QueryCounter", "test.kill", "Greenvale", "rat"), snap["counter"])
        check("rumors survive", q(a, "rumors", "QueryCount", "Greenvale"), snap["rumors"])
        check("Year 1 still begins on the same day", q(a, "chronicle", "QueryEpochDay"), snap["epoch"])
        check("contribution survives", q(a, "chronicle", "QueryContribution", "bob"), snap["contrib"])
        check("entries do not disappear without their character (carol's file is irrelevant)", q(a, "chronicle", "QueryEntriesFor", "bob"), "1")
        r = rep(a, "dave", "discover.great_stump", "great-stump")
        check("a first is not awarded again after a restart", r["raw"], "first=0", "ess=25")
        r = rep(a, "bob", "discover.great_stump", "great-stump")
        check("a character already credited stays credited after a restart", r["raw"], "ignored: already credited")
        r = rep(a, "bob", "test.e250")
        ck("new entries continue the numbering", r["entry"] == int(snap["entries"]) + 1, r)
        a.send("shutdown")
        driver2.wait(timeout=15)
    finally:
        if driver2.poll() is None:
            driver2.kill()
        log2.close()


if __name__ == "__main__":
    sys.exit(main())
