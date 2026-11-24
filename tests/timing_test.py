#!/usr/bin/env python3
"""Timing test for the combat clock (docs/COMBAT_TIMING.md section 6).

Starts the driver on a temporary copy of the mudlib, adds a small arena with controlled creatures (harmless:
they hit only on a natural 20 for 1 damage, and have 100000 Vitality, so luck cannot change the rhythm), logs players
in, and measures the REAL intervals between combat messages from the time they arrive over the network.
Every expected interval is computed from the formula

    delay = (Reaction Time + action modifier + action Recovery) x 2 seconds

and compared with a tolerance of 0.35 s (messages are stamped when they arrive; the driver itself is accurate to a
few milliseconds). A 6 s action and an 8 s one are 2 s apart, so a wrong formula cannot pass.

It checks: the call-out clock itself; an unarmed player (6 s); a slower weapon (8 s); a shield (9 s); haste (7.6 s)
and slow (10.4 s); a fast (4 s), a medium (8 s) and a slow (15 s) creature; a player and creatures with the same
numbers act at the same intervals; the first action of everyone drawn into a fight comes after their own Reaction
Time; an aggressive creature waits its Reaction Time; the queue (a potion mid-recovery happens at the next Ready Time
and displaces one attack, `stop` clears it); the boss's pattern with the Power Strike warning 2 s before the blow and
the once-only Summon; and lifetimes (a target that quits, a player who dies with something queued, fights restarted
while a call-out is pending never give two action chains).

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=4815 python3 tests/timing_test.py
"""
import os
import re
import shutil
import sys
import tempfile
import threading
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4815")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import area_test as a  # noqa: E402
import smoke_test as h  # noqa: E402

TIMING = "/domains/timing/"
HOLLOW = "/domains/hollow/"
TOL = 0.35  # seconds


# ---------------------------------------------------------------- test content

def anvil(cls, name, ident, rt, rec, aggressive=False, equip=(), actions=True, assist=True):
    equip_lines = "".join(
        f'        {{ var item = CloneObject("{HOLLOW}obj/{path}"); MoveObject(item, Self); Equip(item); }}\n' for path in equip)
    action = f'        AddCombatAction("hit", "Hit", preparation: 0, recovery: {rec});\n' if actions else ""
    return f'''// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{name}";
        Ids.Add("{ident}");
        ShortDesc = "a {name.lower()}";
        LongDesc = "An unmoving test creature.";
        Level = 1; MaxVitality = 100000; DamageMin = 1; DamageMax = 1; ToHit = -30;
        XpValue = 0; Aggressive = {str(aggressive).lower()}; Assist = {str(assist).lower()};
        BaseReaction = {rt};
{action}{equip_lines}    }}
}}
'''


CLOCK = '''// @inherit /std/item
public class Clock : Item
{
    private long started;
    public override void Create() { Name = "Clock"; Ids.Add("clock"); ShortDesc = "a clock"; }
    public override void Init() { base.Init(); AddAction("clocktest", Run); AddAction("hurt", Hurt); }
    private bool Run(string args)
    {
        started = TimeMs();
        Write("clock seconds " + (Time() - TimeMs() / 1000 <= 1 ? "agree" : "differ") + "\\n");
        foreach (var delay in new double[] { 0.05, 0.25, 0.7, 1.5 })
        {
            var d = delay;
            CallOut(() => TellObject(Environment()!, "clock " + d + " " + (TimeMs() - started) + "\\n"), d);
        }
        return true;
    }
    private bool Hurt(string args)
    {
        var room = Environment(Environment());
        var p = args.Split(' ');
        var target = room == null ? null : Present(p[0], room);
        target?.Call("TakeDamage", int.Parse(p[1]), null);
        Write(target == null ? "Nobody.\\n" : "Hurt.\\n");
        return true;
    }
}
'''

TEST_FILES = {
    "hub": a.room("TimingHub", "Timing Hub", {"up": HOLLOW + "village/square", "arena": TIMING + "arena", "other": TIMING + "other"}, safe=True),
    "arena": a.room("TimingArena", "Arena", {"hub": TIMING + "hub"}),
    "other": a.room("TimingOther", "Other Room", {"hub": TIMING + "hub"}),
    "fast": anvil("Fast", "Fast Anvil", "fast", 1.5, 0.5),
    "mid": anvil("Mid", "Mid Anvil", "mid", 3.0, 1.0),
    "slow": anvil("Slow", "Slow Anvil", "slow", 5.0, 2.5),
    "twin": anvil("Twin", "Twin Anvil", "twin", 2.5, 0.5, actions=False),  # no action declared: the plain unarmed attack, like a player
    "twin_sword": anvil("TwinSword", "Sword Anvil", "sword", 2.5, 0.5, actions=False, equip=("rusty_sword",)),
    "twin_gear": anvil("TwinGear", "Gear Anvil", "gear", 2.5, 0.5, actions=False, equip=("rusty_sword", "small_shield")),
    "brute": anvil("Brute", "Brute Anvil", "brute", 3.0, 1.0, aggressive=True),
    "clock": CLOCK,
}


# ---------------------------------------------------------------- recording client

class Rec:
    """A player connection that stamps every line with the time it arrived."""

    def __init__(self, name):
        self.name = name
        self.c = h.Client(name)
        self.c.read_until("> ")
        self.lines = []  # (arrival time, text)
        self.alive = True
        self.thread = threading.Thread(target=self.reader, daemon=True)
        self.buf = ""
        self.thread.start()
        self.healer = None

    def reader(self):
        while self.alive:
            try:
                chunk = self.c.sock.recv(65536)
            except OSError:
                continue
            except Exception:
                break
            if not chunk:
                break
            now = time.time()
            self.buf += self.c.rx(chunk).replace("\r", "")
            while "\n" in self.buf:
                line, self.buf = self.buf.split("\n", 1)
                self.lines.append((now, line))

    def send(self, line):
        self.c.send(line)

    def mark(self):
        return len(self.lines)

    def since(self, mark):
        return self.lines[mark:]

    def text(self, mark=0):
        return "\n".join(t for _, t in self.lines[mark:])

    def times(self, pattern, mark=0):
        return [t for t, line in self.lines[mark:] if re.search(pattern, line)]

    def wait_for(self, pattern, mark=0, timeout=30.0):
        """The arrival time of the first matching line after `mark`, or None."""
        end = time.time() + timeout
        while time.time() < end:
            for t, line in self.lines[mark:]:
                if re.search(pattern, line):
                    return t
            time.sleep(0.02)
        return None

    def command(self, line, wait=0.6):
        m = self.mark()
        self.send(line)
        time.sleep(wait)
        return self.text(m)

    def stop(self):
        self.alive = False


def intervals(times):
    return [round(b - a, 2) for a, b in zip(times, times[1:])]


def check_intervals(label, times, expected, tol=TOL, count=3):
    gaps = intervals(times)
    ok = len(gaps) >= count and all(abs(g - expected) <= tol for g in gaps)
    print(("PASS  " if ok else "FAIL  ") + f"{label}: {expected:.1f} s between actions")
    if not ok:
        h.failures.append(label)
        print(f"      expected {count}+ gaps of {expected} s (+-{tol}), got {gaps}")
    return ok


def check_near(label, actual, expected, tol=TOL):
    ok = actual is not None and abs(actual - expected) <= tol
    print(("PASS  " if ok else "FAIL  ") + f"{label}: {expected:.1f} s")
    if not ok:
        h.failures.append(label)
        print(f"      expected {expected} s (+-{tol}), got {actual}")
    return ok


def wait_ready(p, timeout=40):
    """Wait until stat says this character may begin an action (wizards only)."""
    end = time.time() + timeout
    while time.time() < end:
        if "Now: ready" in p.command("stat me", 0.3):
            return True
        time.sleep(0.4)
    return False


def keep_alive(p):
    """Heal the character every 1.5 s until stop_healing(p)."""
    stop = threading.Event()

    def run():
        while not stop.is_set():
            p.send("restore")
            stop.wait(1.5)
    t = threading.Thread(target=run, daemon=True)
    t.start()
    p.healer = (stop, t)


def stop_healing(p):
    if p.healer:
        p.healer[0].set()
        p.healer[1].join()
        p.healer = None


def clean_arena(wiz, hero_list=()):
    for word in ("fast", "mid", "slow", "twin", "sword", "gear", "brute", "overseer", "minion", "corpse"):
        for _ in range(3):
            if "dies." not in wiz.command("slay " + word, 0.2):
                break
    time.sleep(0.3)


# ---------------------------------------------------------------- the test

def main():
    work = tempfile.mkdtemp(prefix="lesmudos-timing-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib, novice=True)
    os.makedirs(os.path.join(mudlib, "domains", "timing"))
    for name, source in TEST_FILES.items():
        h.write(mudlib, TIMING + name, source)
    h.edit(mudlib, HOLLOW + "village/square", 'Exits["south"]', f'Exits["down"] = "{TIMING}hub";\n        Exits["south"]')
    box = [None, None]
    box[0], box[1] = a.start_driver(work, mudlib)
    try:
        wiz = run()
        box[1].flush()
        logged = open(os.path.join(work, "driver.log")).read()
        h.check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:900], "ok")
        wiz.send("shutdown")
        box[0].wait(timeout=20)
    finally:
        if box[0].poll() is None:
            box[0].kill()
        box[1].close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-4000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\n{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


def run():
    check = h.check
    wiz = Rec("wiz")
    check("secret makes the first wizard", wiz.command("promote " + h.SECRET, 1.0), "level 10 wizard")
    hero = Rec("hero")  # a wizard of level 5: it can use stat and speed
    wiz.command("promote hero 5", 0.5)
    wiz.command("goto " + TIMING + "arena", 0.5)
    hero.command("down", 0.5)
    hero.command("arena", 0.5)
    check("the hero is in the arena", hero.command("look", 0.5), "Arena")

    # ===== the clock: TimeMs and fractional call-outs =====
    wiz.command("clone " + TIMING + "clock", 0.5)
    m = wiz.mark()
    wiz.send("clocktest")
    time.sleep(2.2)
    text = wiz.text(m)
    check("TimeMs agrees with Time", text, "clock seconds agree")
    for delay in ("0.05", "0.25", "0.7", "1.5"):
        found = re.search(r"clock %s (\d+)" % re.escape(delay), text)
        elapsed = int(found.group(1)) if found else -1
        want = int(float(delay) * 1000)
        ok = found is not None and want - 3 <= elapsed <= want + 40
        check(f"a call-out of {delay} s fires after {delay} s to the millisecond (took {elapsed} ms)", "ok" if ok else str(elapsed), "ok")

    # ===== what stat, score and consider show =====
    wiz.command("goto " + TIMING + "arena", 0.3)
    wiz.command("clone " + TIMING + "mid", 0.4)
    wiz.command("clone " + TIMING + "mid", 0.4)
    text = hero.command("stat mid", 0.8)
    check("stat shows base and final Reaction Time and the action with its delay", text, "Reaction Time: base 3.0", "final 3.0",
          "Hit: preparation 0.0, recovery 1.0, modifier 0.0, delay 8.0 s", "Now: ready")
    text = hero.command("score", 0.8)
    check("score shows the Reaction Time and the delay of the basic attack", text, "Reaction Time 2.5 (base 2.5).", "Your attack takes 6.0 seconds.")
    text = hero.command("consider mid", 0.8)
    check("consider compares speed (8 s against 6 s)", text, "It strikes less often than you.")
    wiz.command("slay mid", 0.3)
    wiz.command("slay mid", 0.3)
    wiz.command("clone " + TIMING + "fast", 0.4)
    wiz.command("clone " + TIMING + "slow", 0.4)
    check("consider a fast creature", hero.command("consider fast", 0.8), "It strikes more often than you.")
    check("consider a slow creature", hero.command("consider slow", 0.8), "It strikes much less often than you can.")
    text = hero.command("stat slow", 0.8)
    check("stat shows the slow creature's numbers", text, "base 5.0", "recovery 2.5", "delay 15.0 s")
    clean_arena(wiz)

    # ===== A. an unarmed player, and four creatures drawn into the fight =====
    for kind in ("fast", "mid", "slow", "twin"):
        wiz.command("clone " + TIMING + kind, 0.3)
    wait_ready(hero)
    m = hero.mark()
    t0 = time.time()
    hero.send("kill mid")
    time.sleep(47)
    mine = hero.times(r"You (scratch|hit|smash|miss) a (mid|fast|slow|twin) anvil", m)
    check_near("the player (unarmed) strikes at once", mine[0] - t0 if mine else None, 0.0, 0.6)
    check_intervals("an unarmed player (Reaction Time 2.5 + Recovery 0.5)", mine, 6.0, count=6)
    for kind, label, rt, rec in (("fast", "fast", 1.5, 0.5), ("mid", "mid", 3.0, 1.0), ("slow", "slow", 5.0, 2.5), ("twin", "twin", 2.5, 0.5)):
        times = hero.times(r"A %s anvil (scratches|hits|misses|smashes) you" % label, m)
        delay = (rt + rec) * 2
        check_near(f"the {label} creature, drawn into the fight, waits its Reaction Time ({rt}) before the first action", times[0] - t0 if times else None, rt * 2, 0.6)
        if kind != "slow":
            check_intervals(f"the {label} creature (Reaction Time {rt} + Recovery {rec})", times, delay, count=2 if kind == "mid" else 3)
        else:
            check_intervals(f"the {label} creature (Reaction Time {rt} + Recovery {rec})", times, delay, count=2)
    twin = hero.times(r"A twin anvil (scratches|hits|misses|smashes) you", m)
    gaps_p, gaps_n = intervals(mine), intervals(twin)
    check("a player and an NPC with the same numbers act at the same rhythm", "ok" if gaps_p and gaps_n and abs(sum(gaps_p) / len(gaps_p) - sum(gaps_n) / len(gaps_n)) < 0.15 else f"{gaps_p} {gaps_n}", "ok")
    clean_arena(wiz)

    # ===== B. a slower weapon, C. plus a shield: the player and an NPC with the same gear =====
    hero.command("clone " + HOLLOW + "obj/rusty_sword", 0.4)
    hero.command("clone " + HOLLOW + "obj/small_shield", 0.4)
    hero.command("wield sword", 0.4)
    wiz.command("clone " + TIMING + "twin_sword", 0.3)
    text = hero.command("score", 0.8)
    check("score lists the weapon's modifier and the delay (8 s)", text, "Reaction Time 3.0 (base 2.5, a rusty sword +0.5).", "Your attack takes 8.0 seconds.")
    wait_ready(hero)
    m = hero.mark()
    t0 = time.time()
    hero.send("kill sword")
    time.sleep(28)
    mine = hero.times(r"You (scratch|hit|smash|miss) a sword anvil", m)
    his = hero.times(r"A sword anvil (scratches|hits|misses|smashes) you", m)
    check_intervals("a rusty sword (modifier +0.5, Recovery 1.0)", mine, 8.0)
    check_intervals("an NPC wielding the same sword", his, 8.0, count=2)
    check_near("the NPC with the sword waits Reaction Time 3.0 for its first action", his[0] - t0 if his else None, 6.0, 0.6)
    clean_arena(wiz)

    hero.command("wear shield", 0.4)
    wiz.command("clone " + TIMING + "twin_gear", 0.3)
    text = hero.command("score", 0.8)
    check("score lists the shield too (9 s)", text, "a small shield +0.5", "Reaction Time 3.5", "Your attack takes 9.0 seconds.")
    wait_ready(hero)
    m = hero.mark()
    hero.send("kill gear")
    time.sleep(31)
    mine = hero.times(r"You (scratch|hit|smash|miss) a gear anvil", m)
    his = hero.times(r"A gear anvil (scratches|hits|misses|smashes) you", m)
    check_intervals("a rusty sword and a small shield (modifiers +0.5 +0.5)", mine, 9.0)
    check_intervals("an NPC with the same sword and shield", his, 9.0, count=2)
    clean_arena(wiz)

    # ===== D. haste and slow =====
    wiz.command("clone " + TIMING + "mid", 0.3)
    hero.command("speed me 20 300", 0.5)
    check("score shows the speed factor", " ".join(hero.command("score", 0.8).split()), "speed x0.8", "Reaction Time 2.8", "Your attack takes 7.6 seconds.")
    wait_ready(hero)
    m = hero.mark()
    hero.send("kill mid")
    time.sleep(27)
    mine = hero.times(r"You (scratch|hit|smash|miss) a mid anvil", m)
    check_intervals("20% haste: Reaction Time (3.5 x 0.8 = 2.8) + Recovery 1.0", mine, 7.6)
    clean_arena(wiz)
    wiz.command("clone " + TIMING + "mid", 0.3)
    hero.command("speed me -20 300", 0.5)
    check("score shows the slow factor", " ".join(hero.command("score", 0.8).split()), "speed x1.2", "Reaction Time 4.2", "Your attack takes 10.4 seconds.")
    wait_ready(hero)
    m = hero.mark()
    hero.send("kill mid")
    time.sleep(34)
    mine = hero.times(r"You (scratch|hit|smash|miss) a mid anvil", m)
    check_intervals("20% slow: Reaction Time (3.5 x 1.2 = 4.2) + Recovery 1.0", mine, 10.4)
    clean_arena(wiz)
    check("speed 0 clears the effect", hero.command("speed me 0", 0.5) + hero.command("score", 0.8), "Reaction Time 3.5", "Your attack takes 9.0 seconds.")
    hero.command("speed me 50 2", 0.3)
    time.sleep(3)
    check("a speed effect expires by time", hero.command("score", 0.8), "Reaction Time 3.5", absent=("speed x",))
    check("speed refuses nonsense", hero.command("speed me fast", 0.5), "Usage: speed")
    check("speed is wizard level 5: a non-wizard has no such command", Rec("lowly").command("speed me 20", 0.5), "What?")
    hero.command("unwield", 0.3)
    hero.command("remove shield", 0.3)

    # ===== E. the queue: a potion mid-recovery, and stop =====
    hero.command("clone " + HOLLOW + "obj/healing_potion", 0.4)
    hero.command("clone " + HOLLOW + "obj/healing_potion", 0.4)
    wiz.command("clone " + TIMING + "mid", 0.3)
    wait_ready(hero)
    m = hero.mark()
    hero.send("kill mid")
    first = hero.wait_for(r"You (scratch|hit|smash|miss) a mid anvil", m, 3)
    time.sleep(1.0)
    q = hero.mark()
    reply = hero.command("drink potion", 0.5)
    check("a potion while recovering is queued and the player is told", reply, "You are not ready yet.", "You will drink a minor healing potion as soon as you can")
    time.sleep(14)
    drank = hero.times(r"drink the ruby-red potion", q)
    attacks = hero.times(r"You (scratch|hit|smash|miss) a mid anvil", m)
    check_near("the potion is drunk at the next Ready Time", drank[0] - first if drank and first else None, 6.0)
    check_near("the next attack is a whole potion delay later ((2.5 + 0.5) x 2)", attacks[1] - drank[0] if len(attacks) > 1 and drank else None, 6.0)
    check("the potion displaced exactly one attack (attacks at 0 and 12 s, none at 6 s; without it there would be three)", str(len(attacks)), "2")
    q = hero.mark()
    hero.command("drink potion", 0.3)
    check("a newer command replaces the queued action", hero.command("quaff potion", 0.3), "instead of what you had planned")
    check("stop clears the queue", hero.command("stop", 0.3), "You drop what you were about to do.")
    time.sleep(8)
    check("a stopped potion is not drunk", "ok" if not hero.times(r"ruby-red", q) else "drank", "ok")
    check("and is still carried", hero.command("i", 0.5), "minor healing potion")
    check("stop with nothing queued", hero.command("stop", 0.3), "You have nothing planned.")
    clean_arena(wiz)

    # ===== F. an aggressive creature waits its Reaction Time, and so does the player it attacks =====
    wait_ready(hero)
    hero.command("hub", 0.5)
    wiz.command("clone " + TIMING + "brute", 0.3)
    wiz.command("goto " + TIMING + "hub", 0.3)
    time.sleep(0.5)
    m = hero.mark()
    t0 = time.time()
    hero.send("arena")
    attacked = hero.wait_for(r"attacks you!", m, 12)
    check_near("an aggressive creature (Reaction Time 3.0) attacks 6 s after you enter", attacked - t0 if attacked else None, 6.0, 0.5)
    time.sleep(13)
    his = hero.times(r"A brute anvil (scratches|hits|misses|smashes) you", m)
    mine = hero.times(r"You (scratch|hit|smash|miss) a brute anvil", m)
    check_near("its first blow comes with the attack", his[0] - attacked if his and attacked else None, 0.0, 0.5)
    check_intervals("the ambusher then acts every 8 s (3.0 + 1.0)", his, 8.0, count=1)
    check_near("the player it attacks is drawn in: first action after his own Reaction Time (2.5 x 2 s)", mine[0] - attacked if mine and attacked else None, 5.0, 0.6)
    clean_arena(wiz)

    # ===== G. restarting a fight while a call-out is pending never gives two chains =====
    wiz.command("goto " + TIMING + "arena", 0.3)
    hero.command("goto " + TIMING + "arena", 0.5)
    wiz.command("clone " + TIMING + "mid", 0.3)
    wait_ready(hero)
    m = hero.mark()
    hero.send("kill mid")
    hero.wait_for(r"You (scratch|hit|smash|miss) a mid anvil", m, 3)
    for _ in range(3):
        hero.send("kill mid")  # spam
    time.sleep(7.5)
    hero.send("goto " + TIMING + "other")
    time.sleep(2)
    hero.send("goto " + TIMING + "arena")
    time.sleep(0.5)
    for _ in range(4):
        hero.send("kill mid")
        time.sleep(0.2)
    time.sleep(34)
    mine = hero.times(r"You (scratch|hit|smash|miss) a mid anvil", m)
    his = hero.times(r"A mid anvil (scratches|hits|misses|smashes) you", m)
    check("after leaving, returning and repeating `kill` the player never acts faster than every 6 s", "ok" if mine and min(intervals(mine)) >= 5.7 else str(intervals(mine)), "ok")
    check("and the creature never acts faster than every 8 s", "ok" if his and min(intervals(his)) >= 7.7 else str(intervals(his)), "ok")
    check("and it still acts (the chains did not die)", "ok" if len(mine) >= 6 and len(his) >= 4 else f"{len(mine)} {len(his)}", "ok")
    clean_arena(wiz)

    # ===== H. a player who dies with a potion queued does not drink afterwards =====
    wiz.command("goto " + TIMING + "arena", 0.3)
    hero.command("goto " + TIMING + "arena", 0.5)
    wiz.command("clone " + TIMING + "slow", 0.3)
    wait_ready(hero)
    m = hero.mark()
    hero.send("kill slow")
    hero.wait_for(r"You (scratch|hit|smash|miss) a slow anvil", m, 3)
    time.sleep(1.0)
    hero.command("drink potion", 0.4)
    wiz.command("slay hero", 0.4)
    q = hero.mark()
    time.sleep(9)
    after = hero.text(q)
    check("a player who died with a potion queued woke up and did not drink it", "ok" if "ruby-red" not in after and "You hit" not in after and "You miss" not in after else after, "ok")
    check("and is ready, with nothing queued", hero.command("stat me", 0.6), "queued: nothing", "Now: ready")
    check("the creature forgot him (nobody left to fight)", wiz.command("stat slow", 0.6), "enemies 0")
    clean_arena(wiz)
    hero.command("goto " + TIMING + "arena", 0.5)

    # ===== I. the Goblin Overseer's pattern =====
    wiz.command("goto " + TIMING + "arena", 0.3)
    wait_ready(hero)
    keep_alive(hero)
    m = hero.mark()
    t0 = time.time()
    wiz.command("clone " + HOLLOW + "mob/goblin_overseer", 0.5)
    cry = hero.wait_for(r"BATTLE CRY", m, 20)
    check_near("the Overseer (Reaction Time 4.0) acts 8 s after the fight is joined: Battle Cry first", cry - t0 if cry else None, 8.5, 1.2)
    time.sleep(48)
    stop_healing(hero)
    swings = hero.times(r"Goblin Overseer (scratches|hits|misses|smashes) you", m)
    windup = hero.times(r"draws his axe far back", m)
    strike = hero.times(r"crushing POWER STRIKE", m)
    check("one Battle Cry", str(hero.text(m).count("BATTLE CRY")), "1")
    check_near("Axe Slash comes 9 s after the Battle Cry ((4.0 + 0.5) x 2)", swings[0] - cry if swings and cry else None, 9.0)
    check_near("the second Axe Slash 10 s later ((4.0 + 1.0) x 2)", swings[1] - swings[0] if len(swings) > 1 else None, 10.0)
    check_near("the Power Strike is announced right after the second Slash's delay", windup[0] - swings[1] if windup and len(swings) > 1 else None, 10.0)
    check_near("the blow lands 2 s after the warning", strike[0] - windup[0] if strike and windup else None, 2.0, 0.3)
    check_near("the third attack's roll comes with the blow", swings[2] - strike[0] if len(swings) > 2 and strike else None, 0.0, 0.3)
    if windup and len(swings) > 3:
        check_near("the next Axe Slash is 12 s after the Power Strike began ((4.0 + 2.0) x 2)", swings[3] - windup[0], 12.0)
    elif windup:
        later = hero.wait_for(r"Goblin Overseer (scratches|hits|misses|smashes) you", len(hero.lines) - 1, 14)
        check_near("the next Axe Slash is 12 s after the Power Strike began ((4.0 + 2.0) x 2)", later - windup[0] if later else None, 12.0)
    clean_arena(wiz)

    # ===== J. the Summon (once) and a target that leaves during the warning =====
    wait_ready(hero)
    keep_alive(hero)
    obs = Rec("observer")
    wiz.command("promote observer 5", 0.4)
    obs.command("down", 0.4)
    obs.command("arena", 0.5)
    m = hero.mark()
    wiz.command("clone " + HOLLOW + "mob/goblin_overseer", 0.5)
    cry = hero.wait_for(r"BATTLE CRY", m, 20)
    wiz.command("goto " + TIMING + "arena", 0.2)
    wiz.command("clone " + TIMING + "clock", 0.2)
    wiz.command("hurt overseer 60", 0.3)  # below half
    summon_wind = hero.wait_for(r"draws a deep breath to call for help", m, 14)
    summoned = hero.wait_for(r"snarls an order, and a goblin minion", m, 4)
    check_near("with his Vitality at half the next action is the Summon: the call comes 1 s after the warning (0.5 x 2)", summoned - summon_wind if summoned and summon_wind else None, 1.0, 0.3)
    check_near("the Summon follows the Battle Cry's delay ((4.0 + 0.5) x 2)", summon_wind - cry if summon_wind and cry else None, 9.0)
    minion_hit = hero.wait_for(r"Goblin minion (scratches|hits|misses|smashes) you|A goblin minion (scratches|hits|misses|smashes) you", m, 9)
    check_near("the minion, drawn in, waits its Reaction Time (2.5 x 2 s) before the first blow", minion_hit - summoned if minion_hit and summoned else None, 5.0, 0.6)
    wiz.command("hurt overseer 30", 0.3)
    time.sleep(24)
    stop_healing(hero)
    check("the Summon is used once", str(hero.text(m).count("snarls an order")), "1")
    # Now a wind-up with nobody to hit: the hero leaves during a Power Strike warning.
    clean_arena(wiz)
    wiz.command("clone " + HOLLOW + "mob/goblin_overseer", 0.5)
    keep_alive(hero)
    m = hero.mark()
    om = obs.mark()
    warn = hero.wait_for(r"draws his axe far back", m, 60)
    hero.send("quit")
    stop_healing(hero)
    hero.stop()
    blow = obs.wait_for(r"finds nothing to hit", om, 6)
    check("a Power Strike aimed at a player who quit during the warning finds nothing", "ok" if blow else "no line", "ok")
    time.sleep(1)
    check("and nobody else is struck by it", "ok" if "crushing POWER STRIKE" not in obs.text(om) else "struck", "ok")
    clean_arena(wiz)
    check("the observer is still in a working game", obs.command("vitality", 0.5), "Vitality ")
    stop_healing(obs)
    return wiz


if __name__ == "__main__":
    sys.exit(main())
