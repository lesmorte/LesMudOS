#!/usr/bin/env python3
"""Tests for the generic binding effects (docs/specs/bureaucrat.md section 7.3, 7.5): bars, melee restraints, wards, interrupts, a movement
bar, the save-penalty mark, and the three bosses' summons that are spent only when they happen.

Nothing in the game applies these yet, so a TEST-ONLY fixture skill (/domains/efxtest/fxskill, a real object in a trusted tree, as a class's skill
will be) serves the effects through /secure/control.Resolve and a probe object gives each player verbs to drive it.

  1. Bars: an action id barred for a time (a creature takes another action, then the bar ends and it comes back); the bar through Resolve (Full, Half).
  2. A melee restraint and what a creature does instead: a declared ranged action; another enemy; a wait with a line and never longer than the bar
     and never losing its aggression; the lapse when the one it is barred from strikes in melee; a player under such a bar is told.
  3. Wards: the next attacks are refused (a charge each, whichever of charges and time runs out first); a spell that needs no roll is not refused.
  4. Interrupts: a creature's wind-up and a player's skill are cancelled with no resistance roll, nothing refunded, the action barred.
  5. A movement bar (flee, exits).
  6. The save-penalty mark: lowers the next effect's save chance by the caster on that target, once.
  7. The three bosses: a barred summons is not spent and comes back when the bar ends (the Overseer, the Bandit Leader, the Warren Broodmother).
  8. Guards: another object cannot use a ward's charge or lift a restraint.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=5071 python3 tests/effects_test.py
"""
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "5071")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402
import rank_test as rk  # noqa: E402

T = "/domains/efxtest/"
check = h.check

FXSKILL = r'''// @inherit /std/skill
public class FxSkill : Skill
{
    public override void Create() { Title = "Fixture"; Command = "fxfixture"; Kind = "skill"; Summary = "A test-only skill."; Target = "none"; }

    private int Term(ObjectRef control, int strength, int seconds) => strength == 2 ? seconds : strength == 1 ? control.Call<int>("QueryHalfSeconds", seconds) : 0;

    /// <summary>Bar an action id for a time, through the rule.</summary>
    public string Bar(ObjectRef caster, ObjectRef target, string id, int seconds, string kind)
    {
        var control = LoadObject("/secure/control");
        var s = control.Call<int>("Resolve", caster, target, "bar-" + id, kind, 1, 1, "notice");
        var t = Term(control, s, seconds);
        var ok = t > 0 && target.Call<bool>("BarAction", id, t);
        return $"strength={s} secs={t} ok={ok}";
    }

    /// <summary>The target may not melee the caster for a time, through the rule.</summary>
    public string Restrain(ObjectRef caster, ObjectRef target, int seconds)
    {
        var control = LoadObject("/secure/control");
        var s = control.Call<int>("Resolve", caster, target, "restrain", "mental", 1, 1, "order");
        var t = Term(control, s, seconds);
        var ok = t > 0 && target.Call<bool>("RestrainMelee", caster, t, "");
        return $"strength={s} secs={t} ok={ok}";
    }

    public string Ward(ObjectRef bearer, int attacks, int seconds) => $"ok={bearer.Call<bool>("GrantWard", attacks, seconds, "")}";

    public string Interrupt(ObjectRef caster, ObjectRef target, int push, int bar)
    {
        var control = LoadObject("/secure/control");
        var s = control.Call<int>("Resolve", caster, target, "interrupt", "mental", 1, 1, "notice");
        if (s == 0) return "strength=0 ok=False";
        var ok = target.Call<bool>("InterruptPreparation", caster, s == 2 ? push : push / 2.0, s == 2 ? bar : control.Call<int>("QueryHalfSeconds", bar));
        return $"strength={s} ok={ok}";
    }

    public string Hold(ObjectRef target, int seconds) => $"ok={target.Call<bool>("BarMovement", seconds, "")}";

    public string Penalty(ObjectRef caster, ObjectRef target, int points, int seconds) =>
        $"ok={LoadObject("/secure/control").Call<bool>("AddSavePenalty", caster, target, points, seconds)}";

    public string Plain(ObjectRef caster, ObjectRef target, string effect, string kind) =>
        $"strength={LoadObject("/secure/control").Call<int>("Resolve", caster, target, effect, kind, 1, 1, "order")}";
}
'''

PROBE = r'''// @inherit /std/thing
public class FxProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init() { AddAction("fx", Do); }
    private ObjectRef? where;
    private ObjectRef? Find(ObjectRef me, string id) => id == "me" ? me : Present(id, where ?? Environment(me)!);
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        where = null;
        if (w.Length > 0 && w[0].StartsWith("@")) { where = LoadObject("/domains/efxtest/" + w[0].Substring(1)); w = w.Skip(1).ToArray(); } // work in another room, without going there
        var skill = LoadObject("/domains/efxtest/fxskill");
        var control = LoadObject("/secure/control");
        var room = where ?? Environment(me)!;
        ObjectRef? A(int i) => w.Length > i ? Find(me, w[i]) : null;
        int N(int i) => int.Parse(w[i]);
        switch (w[0])
        {
            case "spawn":
            {
                var path = w[1].StartsWith("/") ? w[1] : "/domains/efxtest/" + w[1];
                var n = w.Length > 2 ? N(2) : 1;
                for (var i = 0; i < n; i++) MoveObject(CloneObject(path), room);
                Write("FX spawned\n"); return true;
            }
            case "slay": { foreach (var o in AllInventory(room).ToList()) if (o != me && o.IsAlive && o.Call<bool>("IsMonster")) o.Call("Die", (ObjectRef?)null); Write("FX slain\n"); return true; }
            case "fight": Write("FX fight " + A(1)!.Call<bool>("StartFight", A(2)!) + "\n"); return true;
            case "hurt": A(1)!.Call("ApplyDamage", N(2), (ObjectRef?)null); Write("FX hurt\n"); return true;
            case "bar": Write("FX " + skill.Call<string>("Bar", A(1)!, A(2)!, w[3], N(4), w.Length > 5 ? w[5] : "mental") + "\n"); return true;
            case "restrain": Write("FX " + skill.Call<string>("Restrain", A(1)!, A(2)!, N(3)) + "\n"); return true;
            case "ward": Write("FX " + skill.Call<string>("Ward", A(1)!, N(2), N(3)) + "\n"); return true;
            case "interrupt": Write("FX " + skill.Call<string>("Interrupt", A(1)!, A(2)!, N(3), N(4)) + "\n"); return true;
            case "hold": Write("FX " + skill.Call<string>("Hold", A(1)!, N(2)) + "\n"); return true;
            case "pen": Write("FX " + skill.Call<string>("Penalty", A(1)!, A(2)!, N(3), N(4)) + "\n"); return true;
            case "res": Write("FX " + skill.Call<string>("Plain", A(1)!, A(2)!, w[3], w[4]) + "\n"); return true;
            case "dice": Write("FX dice " + control.Call<bool>("SetTestRolls", w[1]) + "\n"); return true;
            case "clock": Write("FX clock " + control.Call<bool>("SetTestClock", N(1)) + "\n"); return true;
            case "forget": Write("FX forgot " + control.Call<bool>("ForgetAllRecords") + "\n"); return true;
            case "tele": MoveObject(me, LoadObject(w[1])); Write("FX tele\n"); return true;
            case "count": Write("FX count " + AllInventory(room).Count(o => o != me && o.IsAlive && o.Call<bool>("IsMonster") && o.Call<bool>("Id", w[1])) + "\n"); return true;
            case "state":
            {
                var o = A(1)!;
                var who = w.Length > 3 ? A(3) : null;
                Write($"FX state vit={o.Call<int>("QueryVitality")} foc={o.Call<int>("QueryFocus")} barleft={(w.Length > 2 ? o.Call<int>("QueryBarLeft", w[2]) : 0)} " +
                      $"restr={(who != null && o.Call<bool>("QueryRestrainedFrom", who))} ward={o.Call<int>("QueryWard")} move={o.Call<int>("QueryMovementBarred")} prep={o.Call<string>("QueryPreparing")} " +
                      $"fighting={o.Call<bool>("QueryInCombat")} pen={(who != null ? control.Call<int>("QuerySavePenalty", who, o) : -1)}\n");
                return true;
            }
            case "misuse":
            {
                // another object may not spend a ward's charge, lift a restraint, set a bar, or interrupt (the probe is real code, but not the attacker)
                var t = A(1)!; var other = A(2)!;
                Write($"FX misuse consume={t.Call<bool>("ConsumeWard", other)} lapse={t.Call<bool>("LapseRestraintFrom", other)}\n");
                return true;
            }
        }
        return false;
    }
}
'''


def creature(cls, ident, body="", level=1, hp=100000, reaction=0.5, flags="", dmg=1, tohit=40, actions="", choose=""):
    return f"""// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{ident.title()}"; Ids.Add("{ident}"); ShortDesc = "{ident.title()}"; LongDesc = "A creature made for the effects test.";
        Level = {level}; MaxVitality = {hp}; DamageMin = {dmg}; DamageMax = {dmg}; ToHit = {tohit}; Armor = 0; Dodge = 0; XpValue = 0;
        Aggressive = false; BaseReaction = {reaction}; {flags}
{actions}
    }}
{choose}{body}}}
"""


def files():
    f = {T + "probe": PROBE, T + "fxskill": FXSKILL}
    # a plain melee creature: a marker line for every swing
    f[T + "gruntox"] = creature("EfxGruntox", "gruntox", actions='        AddCombatAction("swing", "Swing", preparation: 0, recovery: 0.5, strike: "SWINGS wildly!");')
    # a creature that never attacks (an enemy for the others to turn on, and a caster that never strikes in melee)
    f[T + "passive"] = creature("EfxPassive", "passive", dmg=0, tohit=-40, actions='        AddCombatAction("idle", "Idle", preparation: 0, recovery: 0.5, kind: "idle");')
    # a melee creature with a declared ranged action it never chooses by itself
    f[T + "spitter"] = creature("EfxSpitter", "spitter", actions='''        AddCombatAction("claw", "Claw", preparation: 0, recovery: 0.5, strike: "CLAWS!", reach: "melee", damage: "slashing", tagKind: "claw");
        AddCombatAction("spit", "Spit", preparation: 0, recovery: 0.5, strike: "SPITS a glob!", reach: "ranged", damage: "blunt", tagKind: "spit");''')
    # a creature with a slow wind-up it always chooses, and a quick jab
    f[T + "winder"] = creature("EfxWinder", "winder", actions='''        AddCombatAction("slam", "Slam", preparation: 1.5, recovery: 0.5, windUp: "gathers itself", strike: "SLAMS down!", reach: "melee", damage: "blunt", tagKind: "slam");
        AddCombatAction("jab", "Jab", preparation: 0, recovery: 0.5, strike: "JABS out!", reach: "melee", damage: "piercing", tagKind: "jab");''')
    # a level 8 creature, for the save-penalty cells (resistance 12, 70 points above a level 1 caster)
    f[T + "lv8"] = creature("EfxLv8", "lv8", level=8, reaction=6.0, tohit=-40)
    f[T + "arena"] = rk.room("EfxArena", "Effects Arena", {"out": T + "hall"})
    f[T + "hall"] = rk.room("EfxHall", "Effects Hall", {"up": "/domains/hollow/village/square", "arena": T + "arena", "arena2": T + "arena2", "arena3": T + "arena3", "arena4": T + "arena4"}, "",
                            rk.HALL_INIT.replace("/domains/ranktest/probe", T + "probe"))
    for n in (2, 3, 4):
        f[T + f"arena{n}"] = rk.room(f"EfxArena{n}", f"Effects Arena {n}", {"out": T + "hall"})
    return f


def collect(clients, seconds):
    """Read everything the clients receive for this long; returns one text per client."""
    if not isinstance(clients, (list, tuple)):
        clients = [clients]
    texts = [""] * len(clients)
    end = time.time() + seconds
    while time.time() < end:
        for i, c in enumerate(clients):
            try:
                chunk = c.sock.recv(65536)
                if chunk:
                    texts[i] += c.rx(chunk)
            except Exception:
                pass
    return texts


def fx(c, line, at=""):
    out = c.command("fx " + (f"@{at} " if at else "") + line)
    m = re.search(r"FX (.*)", out)
    return m.group(1).strip() if m else out


def state(c, who, bar="-", frm="-", at=""):
    out = fx(c, f"state {who} {bar} {frm}" if frm != "-" else f"state {who} {bar}", at)
    m = re.search(r"vit=(-?\d+) foc=(-?\d+) barleft=(\d+) restr=(\w+) ward=(\d+) move=(\d+) prep=(.*?) fighting=(\w+) pen=(-?\d+)", out)
    if not m:
        return {"raw": out}
    return {"vit": int(m.group(1)), "foc": int(m.group(2)), "bar": int(m.group(3)), "restr": m.group(4) == "True", "ward": int(m.group(5)),
            "move": int(m.group(6)), "prep": m.group(7), "fighting": m.group(8) == "True", "pen": int(m.group(9))}


def how_many(c, ident, at=""):
    for _ in range(4):
        out = fx(c, f"count {ident}", at)
        m = re.search(r"count (\d+)", out)
        if m:
            return int(m.group(1))
        time.sleep(0.5)
    return -1


def pause(c, seconds):
    """Wait without going idle (an idle character is put to bed at the inn)."""
    end = time.time() + seconds
    while time.time() < end:
        c.command("score")
        time.sleep(min(4.0, max(0.0, end - time.time())))


def count(text, needle):
    return len(re.findall(needle, text))


def clear(w, war):
    """Slay every creature in the arena and make the fighters whole."""
    fx(war, "slay")
    time.sleep(0.4)
    a.drain(war, 0.4)


# ---------------------------------------------------------------- 1. bars (through the rule) and the creature's other action

def step_bars(w, war):
    print("\n=== a barred action: the creature takes another, and it comes back")
    clear(w, war)
    fx(w, "dice save:99.0")
    fx(w, "forget")
    fx(war, "spawn winder")
    war.command("kill winder")
    got = a.quiet(war, 0.5)
    got += collect(war, 6)[0]
    check("a winder with no bar winds up and slams: 'gathers itself' then 'SLAMS down!'", got, "gathers itself", "SLAMS down!")
    # find the next wind-up and cut it
    text = ""
    end = time.time() + 12
    while time.time() < end and "gathers itself" not in text:
        text += collect(war, 0.3)[0]
    out = fx(war, "interrupt winder winder 2 8")
    check("an interrupt through the rule at Full (a failed save) takes hold", out, "strength=2", "ok=True")
    after = collect(war, 6)[0]
    check("the interrupted slam never lands, and the bar sends the winder to its jab", after, "JABS out!", absent=("SLAMS down!",))
    s = state(war, "winder", "slam")
    check("the bar on 'slam' is running (about 8 seconds, less the time gone)", str(1 <= s.get("bar", 0) <= 8), "True")
    later = collect(war, 6)[0]
    check("when the bar ends the slam comes back: the wind-up is seen again", later, "gathers itself")
    check("it is still in the fight: it never lost its aggression", str(state(war, "winder")["fighting"]), "True")
    # half: a made save halves the bar; a kind bar on the creature
    clear(w, war)
    fx(w, "dice save:0.0")
    fx(w, "forget")
    fx(war, "spawn winder")
    out = fx(war, "bar winder winder jab 8")
    check("a made save at Half bars for half the time (8 becomes 4); the bar is on the action id", out, "strength=1", "secs=4", "ok=True")
    s = state(war, "winder", "jab")
    check("the half bar is on", str(1 <= s["bar"] <= 4), "True")
    fx(w, "dice save:99.0")
    out = fx(war, "bar winder winder summon 5")
    check("a bar by kind ('summon': a call for help) is set the same way", out, "ok=True")
    check("and QueryActionBarred / QueryBarLeft see it by kind", str(state(war, "winder", "summon")["bar"] >= 1), "True")
    time.sleep(5.2)
    check("a bar ends by itself", str(state(war, "winder", "summon")["bar"]), "0")
    clear(w, war)


# ---------------------------------------------------------------- 2. the melee restraint

def step_restraint(w, war, mage):
    print("\n=== a melee restraint, and what a creature does instead")
    fx(w, "dice save:99.0")
    clear(w, war)
    fx(w, "forget")
    # (i) a declared ranged action: the spitter claws; under the order it spits at the one it may not claw
    fx(war, "spawn spitter")
    fx(war, "spawn passive")
    fx(war, "fight spitter passive")
    got = collect(war, 4)[0]
    check("before the order the spitter claws", got, "CLAWS!", absent=("SPITS",))
    out = fx(war, "restrain passive spitter 10")
    check("a restraint through the rule (a failed save: Full, 10 seconds)", out, "strength=2", "secs=10", "ok=True")
    check("the creature reports itself restrained from the caster", str(state(war, "spitter", "-", "passive")["restr"]), "True")
    got = collect(war, 6)[0]
    check("restrained, it uses its declared ranged action instead: it spits and does not claw", got, "SPITS a glob!", absent=("CLAWS!",))
    clear(w, war)
    # (ii) another enemy it is hostile to, else (iii) wait
    fx(w, "forget")
    fx(war, "spawn gruntox")
    fx(war, "spawn passive")
    fx(war, "fight gruntox passive")
    war.command("kill gruntox")
    got = collect(war, 5)[0]
    check("before the order a gruntox with two enemies swings at the first (the passive one): no blow at the player", str(count(got, r"[Gg]runtox \w+ you\b") <= 1), "True")
    out = fx(war, "restrain passive gruntox 12")
    check("the order against the first enemy: Full", out, "strength=2", "ok=True")
    got = collect(war, 8)[0]
    swings = count(got, "SWINGS wildly!")
    on_me = count(got, r"[Gg]runtox (hits|misses|\w+) you\b")
    check("it turns on the other enemy in the room: it goes on swinging, and the blows come at the player", str(swings >= 2 and on_me >= 2), "True")
    clear(w, war)
    # (iii) nobody else: it waits, with a line, and comes back after
    fx(w, "forget")
    fx(war, "spawn gruntox")
    fx(war, "spawn passive")
    fx(war, "fight gruntox passive")
    got = collect(war, 3)[0]
    check("before the order it swings at the one enemy it has", got, "SWINGS wildly!")
    out = fx(war, "restrain passive gruntox 7")
    t0 = time.time()
    got = collect(war, 6)[0]
    check("with no other enemy it waits: no swings for the length of the order", got, absent=("SWINGS wildly!",))
    check("and the room reads that it holds back (once, not every wait)", str(1 <= count(got, "holds back") <= 2), "True")
    check("it is still fighting: it never lost its enemy", str(state(war, "gruntox")["fighting"]), "True")
    got = collect(war, 6)[0]
    check("it was never idle longer than the order: within one Reaction Time (1 s) of its end the swings resume", got, "SWINGS wildly!")
    check("its aggression afterwards is whole: it goes on swinging", str(count(collect(war, 4)[0], "SWINGS wildly!") >= 2), "True")
    clear(w, war)
    # the lapse: the one it is barred from strikes it in melee (the warrior's own blows)
    fx(w, "forget")
    fx(war, "spawn gruntox")
    war.command("kill gruntox")
    out = fx(war, "restrain me gruntox 30")
    check("an order from the player against the creature takes hold", out, "strength=2", "ok=True")
    got = collect(war, 13)[0]
    check("the player's own melee blow lifts it at once, and says so", got, "Your hold on", "lapses", "you struck it in person")
    check("it is gone: the creature is no longer restrained from the player", str(state(war, "gruntox", "-", "me")["restr"]), "False")
    check("and the creature swings again", str(count(got, "SWINGS wildly!") >= 1), "True")
    clear(w, war)
    # a player under such an order (the caster, the passive creature, never strikes the player, so the order does not lapse)
    fx(w, "forget")
    fx(mage, "spawn passive")
    mage.command("kill passive")
    before = state(mage, "passive")["vit"]
    out = fx(mage, "restrain passive me 10")
    check("an order against a player (the passive creature is its caster) takes hold", out, "strength=2", "ok=True")
    got = collect(mage, 8)[0]
    check("the player reads a clear message: it may not strike it in melee, and for how long", got, "You may not strike Passive in melee for")
    check("and the message is not repeated every round (at most one in six seconds)", str(count(got, "You may not strike") <= 2), "True")
    check("no blow lands while it holds", str(state(mage, "passive")["vit"] == before), "True")
    got = collect(mage, 30)[0]  # several of the player's swings: one may miss
    after = state(mage, "passive")["vit"]
    check("when it ends the player's blows land again", str(after < before), "True")
    clear(w, mage)
    fx(w, "forget")


# ---------------------------------------------------------------- 3. wards

def step_ward(w, war, mage):
    print("\n=== wards")
    fx(w, "dice save:99.0")
    clear(w, war)
    fx(war, "spawn gruntox")
    war.command("kill gruntox")
    out = fx(war, "ward me 2 30")
    check("a ward of two attacks takes hold", out, "ok=True")
    check("the bearer has two charges", str(state(war, "me")["ward"]), "2")
    got = collect(war, 9)[0]
    check("the first two attacks on the bearer are refused, and the bearer reads it", str(count(got, "attack on you is refused")), "2")
    check("and the room-line carries the refusal to the attacker's side only through the same lines: none after two", str(state(war, "me")["ward"]), "0")
    check("later blows are rolled as always (hits or misses are read again)", str(count(got, r"[Gg]runtox \w+ you\b") >= 3), "True")
    clear(w, war)
    # time runs out first
    fx(war, "spawn passive")
    fx(war, "ward me 5 2")
    time.sleep(3)
    check("a ward with attacks left lapses when its time runs out", str(state(war, "me")["ward"]), "0")
    # the ward on a creature: a melee blow is refused, a spell that needs no roll is not
    clear(w, war)
    fx(war, "spawn passive")
    out = fx(war, "ward passive 3 30")
    got = war.command("kill passive") + collect(war, 5)[0]
    check("the player's melee blows on a warded creature are refused: 'is warded: your attack is refused'", got, "is warded: your attack is refused")
    used = 3 - state(war, "passive")["ward"]
    check("each refusal used one charge", str(used >= 1), "True")
    fx(mage, "ward passive 5 30")
    c0 = state(mage, "passive")["ward"]
    mage.command("bolt passive")
    got = collect(mage, 3)[0]
    c1 = state(mage, "passive")["ward"]
    check("Arcane Bolt needs no hit roll: it is not an attack that a ward refuses (it lands)", got, "arcane force strikes")
    check("... and the bolt used no charge", str(c1 >= c0 - 1 and "is refused" not in got), "True")
    clear(w, war)
    clear(w, mage)
    # the guard: nobody but the attacker can use a charge or lift a restraint
    fx(war, "spawn passive")
    fx(war, "ward passive 3 30")
    out = fx(war, "misuse passive me")
    check("another object cannot spend a ward's charge or lift a restraint for someone else", out, "consume=False", "lapse=False")
    check("the ward is whole", str(state(war, "passive")["ward"]), "3")
    clear(w, war)


# ---------------------------------------------------------------- 4. interrupting a player's skill

def step_interrupt(w, war, mage):
    print("\n=== interrupting a player's skill")
    fx(w, "dice save:99.0")
    clear(w, mage)
    fx(w, "forget")
    fx(mage, "spawn passive")
    fx(mage, "spawn gruntox")
    # the player casts; the interrupt arrives inside the wind-up (Arcane Bolt prepares for 1 second)
    mage.command("kill gruntox")
    time.sleep(1.0)
    v0 = state(mage, "gruntox")["vit"]
    f_before = state(mage, "me")["foc"]
    mage.command("bolt gruntox")  # queued: it begins at the player's next Ready Time, replacing the swing
    end = time.time() + 14
    s0 = state(mage, "me")
    while time.time() < end and state(mage, "me")["prep"] == "":
        s0 = state(mage, "me")
        time.sleep(0.05)
    got = ""
    if state(mage, "me")["prep"] != "":
        s0 = state(mage, "me")
        mage.send("fx interrupt gruntox me 2 8")
    got = collect(mage, 2.5)[0]
    check("the player reads that the skill was stopped mid-motion", got, "stops your arcane bolt mid-motion")
    check("the interrupt through the rule took hold at Full", got, "FX strength=2 ok=True")
    s1 = state(mage, "me")
    check("what was spent is not refunded: the 20 Focus the bolt cost is gone (a stagger would give half back)", str(f_before - s1["foc"] >= 14), "True")
    check("nobody was hurt by the bolt: it never resolved", str(state(mage, "gruntox")["vit"] >= v0 - 0), "True")
    s = state(mage, "me", "arcane_bolt")
    check("the interrupted skill is barred for a time", str(1 <= s["bar"] <= 8), "True")
    out = mage.command("bolt gruntox")
    check("and the player is refused at the command, with the time left", out, "You cannot use Arcane Bolt for")
    time.sleep(8.2)
    check("when the bar ends the skill works again", mage.command("bolt gruntox"), absent=("You cannot use Arcane Bolt",))
    collect(mage, 2)
    clear(w, mage)
    # a half interrupt: the cancel still happens, and the bar is half
    fx(w, "dice save:0.0")
    fx(w, "forget")
    fx(mage, "spawn winder")
    mage.command("kill winder")
    text = ""
    end = time.time() + 12
    while time.time() < end and "gathers itself" not in text:
        text += collect(mage, 0.25)[0]
    out = fx(mage, "interrupt me winder 2 8")
    check("a made save at Half still cancels the wind-up (it is instant: there is nothing of it to halve)", out, "strength=1", "ok=True")
    check("the bar is half (4 seconds, less the time gone)", str(1 <= state(mage, "winder", "slam")["bar"] <= 4), "True")
    fx(w, "dice save:99.0")
    clear(w, mage)
    fx(mage, "spawn passive")
    out = fx(mage, "interrupt me passive 2 8")
    check("an interrupt of a creature that is preparing nothing does nothing (the skill refuses nothing it did not stop)", out, "strength=2", "ok=False")


# ---------------------------------------------------------------- 5. the movement bar

def step_movement(w, war):
    print("\n=== the movement bar")
    clear(w, war)
    out = fx(war, "hold me 6")
    check("a movement bar takes hold", out, "ok=True")
    got = war.command("out")
    check("an exit refuses the one held, with its line", got, "You are held fast and cannot leave.")
    check("and the character did not move", war.command("look"), "Effects Arena")
    fx(war, "spawn gruntox")
    war.command("kill gruntox")
    got = war.command("flee")
    check("flee is refused the same way, and costs no attempt", got, "You are held fast and cannot leave.", absent=("You flee",))
    clear(w, war)
    time.sleep(6.3)
    got = war.command("out")
    check("when it ends the exit works", got, "Effects Hall")
    war.command("arena")
    a.drain(war, 0.3)


# ---------------------------------------------------------------- 6. the save-penalty mark

def step_penalty(w, war, mage):
    print("\n=== the save-penalty mark")
    clear(w, war)
    fx(war, "spawn lv8")
    fx(mage, "res me lv8 warmup physical")  # nothing: the warm-up
    fx(w, "forget")
    fx(w, "dice save:70.0")
    out = fx(war, "res me lv8 plain1 physical")
    check("baseline: a level 1 caster against a level 8 creature (82% to save) with a roll of 70: a made save, Half", out, "strength=1")
    fx(w, "forget")
    out = fx(war, "pen me lv8 20 30")
    check("a mark of 20 is accepted", out, "ok=True")
    s = state(war, "lv8", "-", "me")
    check("the mark can be read (read-only)", str(s["pen"]), "20")
    out = fx(war, "res me lv8 plain2 physical")
    check("with the mark the chance is 62%: the same roll of 70 now fails: Full", out, "strength=2")
    check("the mark is used up by that one effect", str(state(war, "lv8", "-", "me")["pen"]), "0")
    fx(w, "forget")
    out = fx(war, "res me lv8 plain3 physical")
    check("the next effect has the old chance again: Half", out, "strength=1")
    # only the same caster consumes it; time ends it; a mark does not count against the flood guard
    fx(w, "forget")
    fx(war, "pen me lv8 20 30")
    fx(mage, "res me lv8 other physical")
    check("another caster's effect does not use the mark", str(state(war, "lv8", "-", "me")["pen"]), "20")
    fx(w, "clock 31")
    check("a mark lapses with its time", str(state(war, "lv8", "-", "me")["pen"]), "0")
    fx(w, "clock 0")
    fx(w, "forget")
    fx(war, "pen me lv8 20 30")
    fx(war, "pen me lv8 5 30")
    check("a new mark replaces the old one", str(state(war, "lv8", "-", "me")["pen"]), "5")
    fx(war, "pen me lv8 0 30")
    check("0 points clears it", str(state(war, "lv8", "-", "me")["pen"]), "0")
    fx(w, "forget")
    fx(w, "dice save:99.0")
    clear(w, war)


# ---------------------------------------------------------------- 7. the three bosses

BOSSES = [("arena2", "/domains/hollow/mob/goblin_overseer", "overseer", "minion", "goblin minion"),
          ("arena3", "/domains/greenvale/corridor/mob/bandit_leader", "leader", "lookout", "lookout"),
          ("arena4", "/domains/greenvale/corridor/mob/warren_broodmother", "broodmother", "rat", "rat")]


def step_bosses(w, war):
    print("\n=== a barred summons is not spent (the Overseer, the Bandit Leader, the Warren Broodmother)")
    fx(w, "dice save:99.0")
    fx(w, "forget")
    clear(w, war)
    # the rooms are worked from outside: a boss is aggressive and would take the player into the fight
    for room, path, ident, minion, _ in BOSSES:
        fx(war, f"spawn {path}", room)
        fx(war, "spawn passive", room)
        fx(war, f"fight {ident} passive", room)
        # at half his Vitality (the leader's and the brood's triggers are at 50%): damage to a little more than half
        mx = state(war, ident, at=room)["vit"]
        fx(war, f"hurt {ident} {mx // 2 + 1}", room)
        out = fx(war, f"bar {ident} {ident} summon 40", room)
        check(f"{ident}: a boss fails its save one step down: the bar on the kind 'summon' is Half (40 s becomes 20 s)", out, "strength=1", "secs=20")
    pause(war, 10)
    for room, path, ident, minion, _ in BOSSES:
        n = how_many(war, minion, room)
        s = state(war, ident, "summon", at=room)
        check(f"{ident}: under the bar nothing was called (no {minion} in the room), and the summons is still there to come", str(n), "0")
        check(f"{ident}: the bar is still running and the boss is still fighting", str(s.get("bar", 0) > 0 and s.get("fighting", False)), "True")
    pause(war, 40)  # the bar ends at 20 s; the boss's own actions are 7 to 10 seconds apart
    for room, path, ident, minion, _ in BOSSES:
        n = how_many(war, minion, room)
        check(f"{ident}: when the bar ends the summons comes back and happens ({minion} in the room)", str(n >= 1), "True")
        fx(war, "slay", room)


def run(work, mudlib):
    w = h.Client("efxwiz", archetype="warrior")
    w.read_until("> ")
    check("the secret makes the first wizard", w.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(w, 0.4)
    w.command("down")
    war = h.Client("efxwar", archetype="warrior")
    war.read_until("> ")
    a.drain(war, 0.4)
    war.command("down")
    mage = h.Client("efxmage", archetype="mage")
    mage.read_until("> ")
    a.drain(mage, 0.4)
    mage.command("down")
    for c in (war, mage):
        c.command("fx tele " + T + "arena")
        a.drain(c, 0.3)
    w.command("fx tele " + T + "hall")
    only = [x for x in os.environ.get("EFX_STEPS", "").split(",") if x]

    def step(name, fn):
        if not only or name in only:
            fn()

    step("bars", lambda: step_bars(w, war))
    step("restraint", lambda: step_restraint(w, war, mage))
    step("ward", lambda: step_ward(w, war, mage))
    step("interrupt", lambda: step_interrupt(w, war, mage))
    step("movement", lambda: step_movement(w, war))
    step("penalty", lambda: step_penalty(w, war, mage))
    step("bosses", lambda: step_bosses(w, war))
    w.command("fx dice random")
    return w


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-effects-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    for gone in ("crafting/launch_materials.cs", "recipes/launch.cs"):
        if os.path.exists(os.path.join(mudlib, gone)):
            os.remove(os.path.join(mudlib, gone))
    os.makedirs(os.path.join(mudlib, "domains", "efxtest"), exist_ok=True)
    for path, source in files().items():
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
            print(open(os.path.join(work, "driver.log")).read()[-3000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\nwhole test {time.time() - started:.0f} s")
    print(f"{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
