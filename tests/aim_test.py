#!/usr/bin/env python3
"""Tests for the large Minor Heal and its wait, Double Strike's cost, the ranged opening and Aimed Shot
(docs/specs/skill-ranks.md "Owner's decisions after the build"; docs/specs/hollow-expansion.md section 1).

  1. Minor Heal: the amounts at ranks I to III, the refusal during the wait (with the seconds left, before any Focus is spent), the wait
     shown by `skills`, per caster and not per target, another Priest unaffected, over after 15 seconds, gone on logout.
  2. Double Strike: the Endurance it really costs at ranks I, II and III.
  3. The ranged opening: a creature's first action with and without a bow in the player's hands, once per fight (a second creature
     taken on mid-fight gets no delay), and not when the creature starts the fight.
  4. Aimed Shot: damage as the shot that starts the fight and mid-fight at ranks I and II, the spoiled shot (the Endurance stays spent),
     the refusals (no bow, no target, a target fighting someone else), training at Wenna Farrow (rank II) and the hallmaster (rank III),
     Wenna's words, and an existing Scout's save gaining the skill.
  5. A wizard-written probe: no object can start a fight for a player, claim the ranged opening, spoil a shot, set or clear a wait.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=4838 python3 tests/aim_test.py
"""
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4838")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402
import rank_test as rk  # noqa: E402
import balance_sim as sim  # noqa: E402

T = "/domains/aimtest/"
check = h.check
VILLAGE = "/domains/hollow/village/"
HALL = "/domains/greenvale/oakenfell/guild/hall_of_callings"

PROBE = r'''// @inherit /std/thing
public class AimProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        foreach (var v in new[] { "agold", "alevel", "aspawn", "atele", "astat", "ahurt", "afill", "aslay", "agive", "apower" })
            AddAction(v, Do);
    }
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var room = Environment(me)!;
        switch (QueryVerb())
        {
            case "agold": me.Call("AddGold", int.Parse(w[0])); Write("GOLD " + me.Call<int>("QueryGold") + "\n"); return true;
            case "alevel":
            {
                var want = int.Parse(w[0]);
                while (me.Call<int>("QueryLevel") < want) me.Call("AddXp", me.Call<int>("QueryXpForLevel", me.Call<int>("QueryLevel") + 1) - me.Call<int>("QueryXp"));
                Write("LEVEL " + me.Call<int>("QueryLevel") + "\n"); return true;
            }
            case "aspawn": { var n = w.Length > 1 ? int.Parse(w[1]) : 1; for (var i = 0; i < n; i++) MoveObject(CloneObject("/domains/aimtest/" + w[0]), room); Write("SPAWNED " + n + "\n"); return true; }
            case "atele": MoveObject(me, LoadObject(args.Trim())); Write("TELE ok\n"); return true;
            case "astat":
                Write($"STAT end {me.Call<int>("QueryEndurance")}/{me.Call<int>("QueryMaxEndurance")} focus {me.Call<int>("QueryFocus")}/{me.Call<int>("QueryMaxFocus")} vit {me.Call<int>("QueryVitality")}/{me.Call<int>("QueryMaxVitality")} wait {me.Call<int>("QuerySkillWaitLeft", "minor_heal")}\n");
                return true;
            case "ahurt": me.Call("Heal", 99999); me.Call("TakeDamage", int.Parse(w[0]), null); Write("HURT " + me.Call<int>("QueryVitality") + "\n"); return true;
            case "afill": me.Call("FillResources"); Write("FILLED\n"); return true;
            case "aslay":
            {
                var n = 0;
                foreach (var o in AllInventory(room).ToList())
                    if (o != me && o.IsAlive && o.Call<bool>("IsMonster")) { o.Call("Die", (ObjectRef?)null); n++; }
                Write("SLAIN " + n + "\n"); return true;
            }
            case "agive": { var ob = CloneObject("/domains/aimtest/" + w[0]); MoveObject(ob, me); Write("GAVE " + ob.Name + "\n"); return true; }
            case "apower": Write("POWER " + me.Call<double>("QueryStat", "physical_power") + "\n"); return true;
        }
        return false;
    }
}
'''

# A wizard's object (under /wiz): everything a stranger could try with the new entry points, on a victim player and a creature.
MEDDLER = r'''// @inherit /std/thing
public class AimMeddler : Thing
{
    public override void Create() { Name = "meddler"; Ids.Add("meddler"); }
    public override string? Short() => "a meddler";
    public override void Init() { AddAction("meddle", Meddle); }
    private bool Meddle(string args)
    {
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var room = Environment(ThisPlayer()!)!;
        var who = Present(w[0], room);
        var mob = Present("dummy", room);
        if (who == null || mob == null) { Write("MEDDLE none\n"); return true; }
        var tries = new List<string>();
        void Try(string label, Func<object?> f)
        {
            try { tries.Add(label + "=" + (f() ?? "null")); }
            catch (MudRuntimeException e) { tries.Add(label + "!" + e.Message.Length); }
        }
        Try("StartFight", () => who.Call("StartFight", mob));
        Try("OpenFight", () => who.Call("OpenFight", mob));
        Try("BeginFight", () => who.Call("BeginFight", mob, false));
        Try("AddEnemy", () => who.Call("AddEnemy", mob));
        Try("QueryOpeningShot", () => who.Call<bool>("QueryOpeningShot"));
        Try("SpoilPreparation", () => who.Call("SpoilPreparation", mob));
        Try("SetSkillWait", () => who.Call("SetSkillWait", "minor_heal", 0));
        Try("ClearSkillWait", () => who.Call("ClearSkillWait", "minor_heal"));
        Try("UseSkill", () => who.Call("UseSkill", LoadObject("/skills/minor_heal"), who, false));
        Try("QuerySkillWaitLeft", () => who.Call<int>("QuerySkillWaitLeft", "minor_heal"));
        Try("fighting", () => who.Call<bool>("QueryFightingWith", mob) || mob.Call<bool>("QueryFightingWith", who));
        // The creature starts a fight with the player by itself (its own StartFight): a creature's own choice, and with no bow in its hands.
        Write("MEDDLE " + string.Join(" ", tries) + "\n");
        return true;
    }
}
'''


def weapon(cls, name, ident, category, low, high, mod=0.5):
    return f"""// @inherit /std/weapon
public class {cls} : Weapon
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); ShortDesc = "a {name.lower()}"; LongDesc = "Every blow the same.";
        Category = "{category}"; DamageMin = {low}; DamageMax = {high}; ToHit = 0; ReactionMod = {mod}; Recovery = 1.0; Value = 4;
    }}
}}
"""


def capmob(cls, level, vit):
    """A creature of a given level and Vitality for the opener's cap (docs/specs/skill-ranks.md, Aimed Shot)."""
    return f"""// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "Capmob"; Ids.Add("capmob"); ShortDesc = "capmob"; LongDesc = "A creature made for the cap test.";
        Level = {level}; MaxVitality = {vit}; DamageMin = 1; DamageMax = 1; ToHit = -40; Armor = 0; Dodge = 0; XpValue = 0;
        Aggressive = false; BaseReaction = 6.0;
    }}
}}
"""


def mob(cls, name, ident, reaction, to_hit=-40, aggressive=False, dmg=1):
    return f"""// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); ShortDesc = "{name.lower()}"; LongDesc = "A creature made for the aim test.";
        Level = 1; MaxVitality = 50000; DamageMin = {dmg}; DamageMax = {dmg}; ToHit = {to_hit}; Armor = 0; Dodge = 0; XpValue = 0;
        Aggressive = {'true' if aggressive else 'false'};
        BaseReaction = {reaction};
    }}
}}
"""


def test_files():
    f = {T + "probe": PROBE}
    f[T + "bow"] = weapon("AimBow", "Even Bow", "evenbow", "bow", 10, 10)
    f[T + "sword"] = weapon("AimSword", "Even Sword", "evensword", "sword", 10, 10, 0.0)
    f[T + "dummy"] = mob("AimDummy", "Dummy", "dummy", 6.0)
    for name, level, vit in (("cap_d0", 1, 40), ("cap_dm1", 2, 40), ("cap_dm2", 3, 40), ("cap_dm4", 5, 40), ("cap_d2", 1, 20), ("cap_d3", 1, 20), ("cap_mid", 3, 50)):
        f[T + name] = capmob("Aim" + name.title().replace("_", ""), level, vit)
    f[T + "alpha"] = mob("AimAlpha", "Alpha", "alpha", 2.0)
    f[T + "beta"] = mob("AimBeta", "Beta", "beta", 2.0)
    f[T + "aggro"] = mob("AimAggro", "Raider", "raider", 2.0, aggressive=True)
    f[T + "hitter"] = mob("AimHitter", "Hitter", "hitter", 0.5, to_hit=100)
    f[T + "arena"] = rk.room("AimArena", "Aim Arena", {"out": T + "hall"})
    f[T + "yard"] = rk.room("AimYard", "Aim Yard", {"out": T + "hall"})
    f[T + "hall"] = rk.room("AimHall", "Aim Hall", {"up": VILLAGE + "square", "arena": T + "arena", "yard": T + "yard"}, "", rk.HALL_INIT.replace("/domains/ranktest/probe", T + "probe"))
    return f


def login(name, calling):
    c = h.Client(name, archetype=calling)
    c.read_until("> ")
    a.drain(c, 0.4)
    c.command("down")
    return c


def to(c, where):
    c.command("atele " + (where if where.startswith("/") else T + where))
    a.drain(c, 0.3)


def stat(c):
    t = c.command("astat")
    m = re.search(r"STAT end (\d+)/(\d+) focus (\d+)/(\d+) vit (\d+)/(\d+) wait (\d+)", t)
    if not m:
        return dict(end=-1, max_end=-1, focus=-1, max_focus=-1, vit=-1, max_vit=-1, wait=-1)
    n = [int(x) for x in m.groups()]
    return dict(end=n[0], max_end=n[1], focus=n[2], max_focus=n[3], vit=n[4], max_vit=n[5], wait=n[6])


def skill_line(c, title):
    for line in c.command("skills").splitlines():
        if title in line:
            return line
    return ""


def acts(name):
    """A pattern for a line that shows this creature acted against the player: a miss, a hit or the player's dodge (a natural 20 always hits)."""
    return r"(?im)^(?!.*attacks you)(?:" + name + r" .*\byou\b|you dodge " + name + r")"


def timed(c, line, marker, limit=14.0):
    """Send a line and return the seconds until the pattern `marker` first appears (None if it does not within the limit), and everything read."""
    a.drain(c, 0.4)
    data = ""
    start = time.time()
    c.send(line)
    while time.time() - start < limit:
        data += a.quiet(c, 0.05)
        if re.search(marker, data):
            return time.time() - start, data
    return None, data


def damages(text):
    return [int(x) for x in re.findall(r"\(\+(\d+) damage\)", text)]


# ---------------------------------------------------------------- 1. Minor Heal

def wait_over(p):
    deadline = time.time() + 30
    while stat(p)["wait"] > 0 and time.time() < deadline:
        time.sleep(0.5)


def step_heal(chief):
    pa = login("aimpria", "priest")
    pb = login("aimprib", "priest")
    for p in (pa, pb):
        p.command("alevel 5")
        p.command("agold 500")
        to(p, "arena")
    # The owner's decision of 22 November 2026: on another 20 / 38 / 60, on the caster half of that, rounded half up (10 / 19 / 30), and the
    # half comes BEFORE Divine Knowledge and Healing Power (Spirit 9 and Insight 6: +9% and +25% = x1.34).
    # Rank I on the caster: 10 x 1.34 = 13 (not 20 x 1.34 = 27 halved to 14), and 25 Focus.
    pa.command("ahurt 40")
    s0 = stat(pa)
    pa.send("heal")
    text = a.fight_until(pa, "restoring", timeout=20) + a.quiet(pa, 0.4)
    got = re.search(r"restoring (\d+) Vitality", text)
    check("Minor Heal I on the caster restores 9 (a third of 20 = 7, x 1.34)", got.group(1) if got else text[-200:], "9")
    check("the message to the caster says the smaller amount plainly", text, "The light comes slowly to your own hurts, restoring 9 Vitality.")
    check("and the room is told it as before", a.quiet(pb, 0.3), "heals themselves.")
    s1 = stat(pa)
    check("Minor Heal I costs 25 Focus (the cost is not halved)", str(s0["focus"] - s1["focus"] in (24, 25, 26)), "True")
    check("the wait is on: 15 seconds or a little less are left", str(1 <= s1["wait"] <= 15), "True")
    # The refusal: before any Focus is spent, with the time left.
    pa.command("ahurt 40")
    f0 = stat(pa)["focus"]
    out = pa.command("heal")
    f1 = stat(pa)["focus"]
    m = re.search(r"You cannot cast Minor Heal again so soon: (\d+) seconds? left", out)
    check("a second Minor Heal during the wait is refused with the seconds left", "ok" if m and 1 <= int(m.group(1)) <= 15 else out, "ok")
    check("and no Focus is spent by the refusal", str(f1 >= f0), "True")
    check("and nothing is cast", a.quiet(pa, 2.5), absent=("restoring",))
    check("skills shows the wait: 15 seconds, and the seconds left", skill_line(pa, "Minor Heal"), "You cannot cast it again for 15 seconds", "you may cast it again in ")
    check("skills says what it gives another and what it gives you, and why", skill_line(pa, "Minor Heal"), "Restores 20 Vitality to someone in the room, or 7 to you",
          "gives freely to the one who reaches for another and sparingly to the one who reaches for himself")
    # Per caster, not per target: the same wait for a heal on someone else.
    out = pa.command("heal aimprib")
    check("the wait is the caster's: no heal on another either", out, "You cannot cast Minor Heal again so soon")
    # Another Priest is unaffected.
    pb.command("ahurt 40")
    pb.send("heal")
    text = a.fight_until(pb, "restoring", timeout=20)
    check("another Priest casts at once, the wait is not shared (on itself: 9)", text, "restoring 9 Vitality")
    # Over after 15 seconds (counted from the cast, the time already waited included).
    time.sleep(max(0.0, 16.0 - 8.0))
    wait_over(pa)
    check("the wait ends: nothing left", str(stat(pa)["wait"]), "0")
    # On another: the full 20 x 1.34 = 27; the target is told too.
    pb.command("afill")
    pb.command("ahurt 40")
    a.quiet(pb, 0.3)
    pa.command("ahurt 40")
    pa.send("heal aimprib")
    text = a.fight_until(pa, "restoring", timeout=20) + a.quiet(pa, 0.4)
    check("Minor Heal I on another restores the full 27 (20 x 1.34)", text, "Warm light flows from you to Aimprib, restoring 27 Vitality.")
    check("the one healed is told the same amount", a.quiet(pb, 0.5), "heals you for 27 Vitality")
    wait_over(pa)
    pa.command("ahurt 40")
    pa.send("heal")
    text = a.fight_until(pa, "restoring", timeout=20)
    check("after the wait a second Minor Heal is cast", text, "restoring 9 Vitality")
    # Rank II: on another 38 x 1.34 = 51 (32 Focus), on the caster 19 x 1.34 = 25 (51 halved would give 26).
    to(pa, VILLAGE + "shrine")
    check("Minor Heal II bought", pa.command("train heal"), "Minor Heal II")
    to(pa, "arena")
    wait_over(pa)
    pa.command("afill")
    pa.command("ahurt 53")
    s0 = stat(pa)
    pa.send("heal")
    text = a.fight_until(pa, "restoring", timeout=20) + a.quiet(pa, 0.4)
    got = re.search(r"restoring (\d+) Vitality", text)
    check("Minor Heal II on the caster restores 17 (a third of 38 = 13, x 1.34)", got.group(1) if got else text[-200:], "17")
    check("Minor Heal II costs 32 Focus", str(s0["focus"] - stat(pa)["focus"] in (31, 32, 33)), "True")
    check("skills: Minor Heal II in numbers", skill_line(pa, "Minor Heal"), "Minor Heal II", "Restores 38 Vitality", "or 13 to you", "[32 Focus")
    wait_over(pa)
    pb.command("afill")
    pb.command("ahurt 53")
    a.quiet(pb, 0.3)
    pa.send("heal aimprib")
    text = a.fight_until(pa, "restoring", timeout=20) + a.quiet(pa, 0.4)
    check("Minor Heal II on another restores the full 51 (38 x 1.34)", text, "restoring 51 Vitality")
    pa.command("alevel 10")
    to(pa, HALL)
    check("Minor Heal III bought from Anselm", pa.command("train heal"), "Minor Heal III")
    to(pa, "arena")
    wait_over(pa)
    pa.command("afill")
    pa.command("ahurt 85")
    s0 = stat(pa)
    pa.send("heal")
    text = a.fight_until(pa, "restoring", timeout=20) + a.quiet(pa, 0.4)
    got = re.search(r"restoring (\d+) Vitality", text)
    check("Minor Heal III on the caster restores 27 (a third of 60 = 20, x 1.34)", got.group(1) if got else text[-200:], "27")
    check("Minor Heal III costs 40 Focus", str(s0["focus"] - stat(pa)["focus"] in (39, 40, 41)), "True")
    check("skills: Minor Heal III names its wait too", skill_line(pa, "Minor Heal"), "Minor Heal III", "Restores 60 Vitality", "or 20 to you", "cast it again for 15 seconds")
    wait_over(pa)
    pb.command("alevel 10")
    pb.command("afill")
    pb.command("ahurt 85")
    a.quiet(pb, 0.3)
    pa.send("heal aimprib")
    text = a.fight_until(pa, "restoring", timeout=20) + a.quiet(pa, 0.4)
    check("Minor Heal III on another restores the full 80 (60 x 1.34)", text, "restoring 80 Vitality")
    # Logout ends the wait: it is not saved.
    pa.command("ahurt 40")
    pa.command("quit")
    time.sleep(1.0)
    pa = login("aimpria", "priest")
    pa.command("ahurt 40")
    pa.send("heal")
    text = a.fight_until(pa, "restoring", timeout=20)
    check("the wait is gone after a logout (it is not saved)", text, "restoring")
    pa.command("quit")
    pb.command("quit")


# ---------------------------------------------------------------- 2. Double Strike

def step_double(chief):
    adv = login("aimadv", "adventurer")
    adv.command("agive sword")
    adv.command("wield evensword")
    to(adv, "arena")
    adv.command("aspawn dummy")
    adv.command("alevel 15")
    adv.command("agold 3000")
    for rank, cost in ((1, 35), (2, 40), (3, 46)):
        if rank == 2:
            to(adv, VILLAGE + "practice_yard")
            check("Double Strike II bought", adv.command("train double"), "Double Strike II")
        if rank == 3:
            to(adv, HALL)
            check("Triple Strike bought", adv.command("train double"), "Triple Strike")
        to(adv, "arena")
        adv.command("aslay")
        adv.command("aspawn dummy")
        adv.command("afill")
        s0 = stat(adv)
        adv.send("double dummy")
        a.quiet(adv, 0.6)
        s1 = stat(adv)
        spent = s0["end"] - s1["end"]
        check(f"Double Strike rank {rank} spends {cost} Endurance", str(cost - 2 <= spent <= cost), "True")
        check(f"Double Strike rank {rank}: the skills line says [{cost} Endurance", (skill_line(adv, "Double Strike") or skill_line(adv, "Triple Strike")), f"[{cost} Endurance")
        adv.command("stop")
        adv.command("aslay")
        a.drain(adv, 8.0)
    adv.command("quit")


# ---------------------------------------------------------------- 3. the ranged opening

def first_miss(c, line, name="alpha"):
    return timed(c, line, acts(name))


def step_opening(chief):
    s = login("aimopen", "scout")
    s.command("agive bow")
    s.command("agive sword")
    to(s, "arena")
    deltas = {}
    debug = {}
    for label, weapon_id in (("a sword", "evensword"), ("a bow", "evenbow")):
        s.command("wield " + weapon_id)
        s.command("aspawn alpha")
        secs, data = first_miss(s, "kill alpha")
        deltas[label] = secs
        debug[label] = data
        s.command("aslay")
        a.drain(s, 1.0)
        to(s, "yard")
        to(s, "arena")
    print("      first actions (seconds):", deltas)
    if os.environ.get("AIM_DEBUG"):
        print("      ", repr(debug["a bow"][-400:]))
    check("the creature's first action comes at its Reaction Time x 2 s (4 s) with a sword in the player's hands",
          str(deltas["a sword"] is not None and 3.4 <= deltas["a sword"] <= 4.7), "True")
    check("with a bow in the player's hands it comes 2 seconds later (6 s)", str(deltas["a bow"] is not None and 5.4 <= deltas["a bow"] <= 6.7), "True")
    if deltas["a sword"] and deltas["a bow"]:
        check("the difference is RangedOpening, 2 seconds", str(1.5 <= deltas["a bow"] - deltas["a sword"] <= 2.5), "True")
    # Once per fight: a second creature taken on while the fight is on gets no delay.
    s.command("wield evenbow")
    s.command("aspawn alpha")
    s.command("aspawn beta")
    s.send("kill alpha")
    a.quiet(s, 0.5)
    secs, data = timed(s, "kill beta", acts("beta"), limit=14.0)
    s.command("aslay")
    a.drain(s, 1.0)
    check("a second creature taken on mid-fight gets no opening delay: the fight was already begun",
          str(secs is not None and secs <= 4.7), "True")
    # Not when the creature starts the fight: an aggressive creature engages on arrival (here, as it is cloned into the room where the
    # player stands with a bow in hand) and waits only its own Reaction Time x 2 = 4 s.
    s.command("aslay")
    a.drain(s, 1.0)
    secs, data = timed(s, "aspawn aggro", acts("raider"))
    print("      aggressive creature's first action (seconds):", secs)
    check("an aggressive creature's fight is its own: no opening delay for a bow in the player's hands (4 s, not 6)",
          str(secs is not None and 3.4 <= secs <= 4.9), "True")
    s.command("aslay")
    s.command("quit")


# ---------------------------------------------------------------- 4. Aimed Shot

WINDUP = "draws a long breath and takes aim"


def shots(c, line, target, count, expect_opener):
    """Aim `count` times; for each, a fresh dummy (an opener) or the fight already on (mid-fight). Returns the damage of each shot that landed."""
    out = []
    for _ in range(count):
        c.command("afill")
        if expect_opener:
            c.command("aslay")
            c.command("aspawn dummy")
            a.drain(c, 0.3)
            c.send(line)
        else:
            c.send(line)
        text = ""
        end = time.time() + 14
        while time.time() < end and "(+" not in text.split(WINDUP, 1)[-1] and "You miss" not in text.split(WINDUP, 1)[-1]:
            text += a.quiet(c, 0.25)
        d = damages(text.split(WINDUP, 1)[-1]) if WINDUP in text else []
        if d:
            out.append(d[0])
        if os.environ.get("AIM_DEBUG"):
            print("      shot text:", repr(text[-500:]))
        if not expect_opener:
            a.quiet(c, 7.5)
    return out


def step_aim(chief):
    s = login("aimscout", "scout")
    s.command("agive bow")
    s.command("agive sword")
    s.command("wield evenbow")
    s.command("verbose on")
    to(s, "arena")
    check("a Scout has Aimed Shot from level 1, with its numbers", skill_line(s, "(Aimed Shot)"), "aim", "(Aimed Shot)", "[30 Endurance", "wind-up 2.0 s",
          "+3 to hit", "1.75 times the bow's damage", "2.5 times as the shot that starts the fight", "(rank II at level 5)")
    check("the Precision Strike line is as it was", skill_line(s, "Precision Strike"), "+4 to hit with a 15% chance", "[25 Endurance")
    # Rank I numbers: even bow 10 (Might 5 at level 1: no bonus). Mid-fight 1.75 -> 18 (a critical 27); the opener 2.5 -> 25 (a critical 38).
    s.command("aspawn dummy")
    s.send("kill dummy")
    a.quiet(s, 1.0)
    mid = shots(s, "aim dummy", "dummy", 3, False)
    check("Aimed Shot I, mid-fight: x1.75 of 10 is 18 (27 with a critical)", "ok" if mid and all(d in (18, 27) for d in mid) else str(mid), "ok")
    s.command("stop")
    s.command("aslay")
    a.drain(s, 1.0)
    opener = shots(s, "aim dummy", "dummy", 5, True)
    check("Aimed Shot I, as the shot that starts the fight: x2.5 of 10 is 25 (38 with a critical)", "ok" if opener and all(d in (25, 38) for d in opener) else str(opener), "ok")
    s.command("aslay")
    # Refusals.
    s.command("aspawn dummy")
    s.command("afill")
    s.command("wield evensword")
    e0 = stat(s)["end"]
    out = s.command("aim dummy")
    check("no bow: a plain refusal", out, "Aimed Shot needs a bow in your hands.")
    check("no bow: no fight begun and no Endurance spent", out + str(stat(s)["end"] >= e0), "True", absent=("You attack",))
    s.command("wield evenbow")
    check("no target: a plain refusal", s.command("aim"), "You are not fighting anyone: name a target, as in 'aim rat'.")
    check("no such target: a plain refusal", s.command("aim nobody"), "You see no 'nobody' here.")
    other = login("aimother", "warrior")
    to(other, "arena")
    other.send("kill dummy")
    a.quiet(other, 1.0)
    e0 = stat(s)["end"]
    out = s.command("aim dummy")
    check("a target already fighting someone else: a plain refusal", out, "is busy fighting someone else; you cannot take aim at it.")
    check("and no fight begun, no Endurance spent", out + str(stat(s)["end"] >= e0), "True", absent=("You attack",))
    other.command("quit")
    s.command("aslay")
    a.drain(s, 1.0)
    # The spoiled shot: a creature that always hits, taken on first with the bow; then a shot aimed while its blows keep landing.
    s.command("aspawn hitter")
    s.send("kill hitter")
    a.quiet(s, 1.0)
    spoiled = 0
    spent_ok = False
    for _ in range(4):
        s.command("ahurt 0")
        s.command("afill")
        s0 = stat(s)
        s.send("aim hitter")
        text = ""
        end = time.time() + 16
        while time.time() < end and "spoiled" not in text and "(+" not in text.split(WINDUP, 1)[-1]:
            text += a.quiet(s, 0.2)
        if "spoiled" in text:
            spoiled += 1
            s1 = stat(s)
            spent_ok = spent_ok or (s0["end"] - s1["end"] >= 26)
            break
        a.quiet(s, 6.0)
    check("a blow that lands while the shot is aimed spoils it", str(spoiled >= 1), "True")
    check("the Endurance of a spoiled shot stays spent (no refund)", str(spent_ok), "True")
    s.command("aslay")
    # The spoil does not apply to the opener: the creature's first action waits Reaction x 2 + 2 s, longer than the 2 s of aiming.
    a.drain(s, 9.0)
    s.command("aspawn hitter")
    s.command("afill")
    s.send("aim hitter")
    text = ""
    end = time.time() + 10
    while time.time() < end and "(+" not in text.split(WINDUP, 1)[-1] and "You miss" not in text.split(WINDUP, 1)[-1]:
        text += a.quiet(s, 0.25)
    check("the opening shot lands before the creature can act (2 s of aiming, 3 s before it may act)", text, WINDUP, absent=("spoiled",))
    s.command("aslay")
    # Training: Wenna Farrow, rank II at level 5 and 40 gold.
    to(s, VILLAGE + "strung_yew")
    out = s.command("ranks")
    check("Wenna lists Aimed Shot II with level 5 and 40 gold", out, "Wenna Farrow teaches", "Aimed Shot II", "40 gold", "Level 5 (you are level 1)")
    out = s.command("train aim")
    check("refused by level, in words, no gold taken", out, "Aimed Shot II needs level 5", "No gold has been taken")
    s.command("alevel 5")
    s.command("agold 40")
    out = s.command("train aim")
    check("Aimed Shot II bought on level and gold alone", out, "You pay Wenna Farrow 40 gold.", "Aimed Shot II", "It answers to aim")
    check("the skills line at rank II", skill_line(s, "(Aimed Shot II)"), "[35 Endurance", "2 times the bow's damage", "3 times as the shot that starts the fight", "(rank III at level 10)")
    check("Wenna no longer says the lesson is not ready", s.command("ask wenna about aimed shot"), "aimed shot", absent=("not ready", "Ask me again soon"))
    check("Wenna says what the opener and the spoiling are", s.command("ask wenna about aim"), "two and a half times", "spoils it")
    check("Wenna on the opening: only for the one who chooses the fight", s.command("ask wenna about opening"), "two seconds more", "chooses the fight")
    # Rank II numbers: x2 mid-fight (20, critical 30), x3 as the opener (30, critical 45); Might 5 at level 5: Physical Power +2%: still 10.
    to(s, "arena")
    s.command("wield evenbow")
    s.command("aspawn dummy")
    s.send("kill dummy")
    a.quiet(s, 1.0)
    mid = shots(s, "aim dummy", "dummy", 3, False)
    check("Aimed Shot II, mid-fight: x2.0 of 10 is 20 (30 with a critical)", "ok" if mid and all(d in (20, 30) for d in mid) else str(mid), "ok")
    s.command("aslay")
    a.drain(s, 1.0)
    opener = shots(s, "aim dummy", "dummy", 5, True)
    check("Aimed Shot II, as the shot that starts the fight: x3.0 of 10 is 30 (45 with a critical)", "ok" if opener and all(d in (30, 45) for d in opener) else str(opener), "ok")
    s.command("aslay")
    # Rank III from the scouts' hallmaster at level 10, then the numbers of every rank through the one helper.
    s.command("alevel 10")
    s.command("agold 500")
    to(s, HALL)
    out = s.command("ranks")
    check("Veyla lists Aimed Shot III", out, "Veyla teaches", "Aimed Shot III", "150 gold")
    check("Aimed Shot III bought", s.command("train aim"), "Aimed Shot III")
    check("the skills line at rank III", skill_line(s, "(Aimed Shot III)"), "[39 Endurance", "2.25 times the bow's damage", "3.5 times as the shot that starts the fight", "(rank IV at level 15)")
    check("Veyla's words mention the aimed shot", s.command("ask veyla about ranks"), "aimed shot")
    s.command("quit")
    # Numbers by rank, the helper's, through the data the simulator holds.
    t = sim.SKILL_RANKS["aimed_shot"]
    want = {r: (sim.rank_value("aimed_shot", "factor", r), sim.rank_value("aimed_shot", "opener", r)) for r in range(1, 6)}
    check("the simulator's table: x1.75/2.5, 2.0/3.0, 2.25/3.5, 2.5/4.0, 2.75/4.5", str(want),
          "(1.75, 2.5)", "(2.0, 3.0)", "(2.25, 3.5)", "(2.5, 4.0)", "(2.75, 4.5)")
    old = login("aimold", "scout")
    old.command("alevel 5")
    old.command("save")
    old.command("quit")
    time.sleep(0.6)
    old = login("aimold", "scout")
    check("an existing Scout's save has Aimed Shot on login (the skill is the class's, not saved)", skill_line(old, "(Aimed Shot)"), "(Aimed Shot)", "aim")
    old.command("quit")


CAPLINE = "twists away at the last moment"


def first_landed(c, spawn, line, tries=5):
    """A fresh creature `spawn`, one command `line`; the damage of the first shot that lands and the whole text. Retries after a miss."""
    text = ""
    for _ in range(tries):
        c.command("afill")
        c.command("aslay")
        c.command("aspawn " + spawn)
        a.drain(c, 0.3)
        c.send(line)
        text = ""
        end = time.time() + 12
        while time.time() < end and "(+" not in text and "You miss" not in text and "dodges" not in text:
            text += a.quiet(c, 0.25)
        d = damages(text)
        if d:
            return d[0], text + a.quiet(c, 0.6)
    return None, text


def step_cap(chief):
    """The Aimed Shot opener's cap (docs/specs/skill-ranks.md, owner's decision 24 Nov 2026): 60% + 15% x d of maximum Vitality, floor 30%, none from d = +3."""
    s = login("aimcap", "scout")
    s.command("agive bow")
    s.command("agive sword")
    s.command("wield evenbow")
    s.command("verbose on")
    to(s, "arena")
    check("the simulator's G2 is the one the game has: 60% at d = 0, 45% at -1, 30% from -2, 90% at +2, none at +3",
          str([None if x is None else round(x, 4) for x in (sim.g_share(sim.G2, d) for d in (0, -1, -2, -4, 2, 3))]), "[0.6, 0.45, 0.3, 0.3, 0.9, None]")
    for spawn, d, cap in (("cap_d0", 0, 24), ("cap_dm1", -1, 18), ("cap_dm2", -2, 12), ("cap_dm4", -4, 12)):
        dmg, text = first_landed(s, spawn, "aim capmob")
        check(f"opener at d = {d}: capped to {cap} of 40", str(dmg), str(cap))
        check(f"opener at d = {d}: the player is told why", text, CAPLINE)
        s.command("aslay")
    # An ordinary Aimed Shot (a fight already begun) is not capped: a level 3 creature with 50 Vitality (cap would be 15) takes the full x1.75 (18, or 27).
    s.command("afill")
    s.command("aspawn cap_mid")
    s.send("kill capmob")
    a.quiet(s, 1.0)
    mid = []
    for _ in range(3):
        mid = shots(s, "aim capmob", "capmob", 1, False)
        if mid:
            break
    text = ""
    check("an ordinary aimed shot at d = -2 is not capped (not 12, no excuse line)", "ok" if mid and mid[0] in (18, 27) else str(mid), "ok")
    check("and says nothing about twisting", text, absent=(CAPLINE,))
    s.command("stop")
    s.command("aslay")
    # Precision Strike's ambush from hiding is not capped (x1.5 of 10 is 15, 23 with a critical; a cap at the floor would be 12).
    s.command("wield evensword")
    got = None
    for _ in range(5):
        s.command("afill")
        s.command("aslay")
        s.command("sneak on")
        s.command("aspawn cap_dm4")
        a.drain(s, 0.3)
        s.send("strike capmob")
        text = ""
        end = time.time() + 8
        while time.time() < end and "(+" not in text and "You miss" not in text:
            text += a.quiet(s, 0.25)
        if damages(text):
            got = damages(text)[0]
            break
    check("Precision Strike's ambush at d = -4 is not capped", "ok" if got in (15, 23) else str(got), "ok")
    check("and says nothing about twisting", text, absent=(CAPLINE,))
    s.command("aslay")
    s.command("wield evenbow")
    # d = +2 at level 3 (90% of 20 = 18), d = +3 at level 4: no cap, the shot takes the whole 20.
    s.command("alevel 3")
    dmg, text = first_landed(s, "cap_d2", "aim capmob")
    check("opener at d = +2: capped to 18 of 20", str(dmg), "18")
    check("opener at d = +2: told", text, CAPLINE)
    s.command("aslay")
    s.command("alevel 4")
    dmg, text = first_landed(s, "cap_d3", "aim capmob")
    check("opener at d = +3: no cap, the whole 20", str(dmg), "20")
    check("opener at d = +3: no excuse line", text, absent=(CAPLINE,))
    check("skills says the opener is limited", skill_line(s, "(Aimed Shot)"), "never takes more than part of the target's full Vitality")
    s.command("quit")


# ---------------------------------------------------------------- 5. the probe

def step_security(chief, mudlib):
    os.makedirs(os.path.join(mudlib, "wiz", "aimchief"), exist_ok=True)
    h.write(mudlib, "/wiz/aimchief/meddler", MEDDLER)
    vic = login("aimvictim", "scout")
    vic.command("agive bow")
    vic.command("wield evenbow")
    to(vic, "arena")
    to(chief, "arena")
    chief.command("aspawn dummy")
    check("the meddler is cloned", chief.command("clone ~/meddler"), "meddler")
    out = chief.command("meddle aimvictim")
    check("the meddler ran", out, "MEDDLE ")
    check("no object can start a fight for a player", out, "StartFight=False", absent=("StartFight=True",))
    check("nor claim the opening or set or clear a wait: the methods are not reachable (null, not an answer)", out, "OpenFight=null", "BeginFight=null", "SetSkillWait=null", "ClearSkillWait=null", "UseSkill=null",
          absent=("OpenFight=True", "OpenFight=False", "BeginFight=True", "BeginFight=False", "UseSkill=True", "UseSkill=False"))
    check("an enemy cannot be added for a player", out, "fighting=False")
    check("the opening shot reads false to anyone", out, "QueryOpeningShot=False")
    check("a blow cannot be forged on a player: spoiling needs the attacker's own attack", out, "SpoilPreparation=False")
    check("the wait can be read (it is not secret), and is zero", out, "QuerySkillWaitLeft=0")
    s = stat(vic)
    check("the victim spent nothing", str(s["end"] == s["max_end"]), "True")
    check("the victim is in no fight", vic.command("score"), absent=("You are fighting",))
    vic.command("quit")


def run(work, mudlib):
    chief = h.Client("aimchief", archetype="adventurer")
    chief.read_until("> ")
    check("the secret makes the first wizard", chief.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(chief, 0.4)
    chief.command("down")
    only = [x for x in os.environ.get("AIM_STEPS", "").split(",") if x]
    for name, step in (("heal", lambda: step_heal(chief)), ("double", lambda: step_double(chief)), ("opening", lambda: step_opening(chief)),
                       ("aim", lambda: step_aim(chief)), ("cap", lambda: step_cap(chief)), ("security", lambda: step_security(chief, mudlib))):
        if not only or name in only:
            step()
    return chief


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-aim-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    for gone in ("crafting/launch_materials.cs", "recipes/launch.cs"):
        if os.path.exists(os.path.join(mudlib, gone)):
            os.remove(os.path.join(mudlib, gone))
    os.makedirs(os.path.join(mudlib, "domains", "aimtest"), exist_ok=True)
    for path, source in test_files().items():
        h.write(mudlib, path, source)
    h.edit(mudlib, "/domains/hollow/village/square", 'Exits["south"]', f'Exits["down"] = "{T}hall";\n        Exits["south"]')
    proc, log = a.start_driver(work, mudlib)
    started = time.time()
    try:
        chief = run(work, mudlib)
        chief.command("shutdown")
        time.sleep(1.5)
        log.flush()
        logged = open(os.path.join(work, "driver.log")).read()
        check("the driver logged no errors", "ok" if "ERROR" not in logged else logged[logged.index("ERROR"):][:800], "ok")
    finally:
        proc.kill()
        log.close()
        if h.failures:
            print("\n--- driver log ---")
            print(open(os.path.join(work, "driver.log")).read()[-4000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\nwhole test {time.time() - started:.0f} s")
    print(f"{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
