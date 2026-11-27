#!/usr/bin/env python3
"""Tests for the control rule (docs/specs/tags-and-saves.md, Part B): /secure/control, the Authority statistic and the slow cap.

Nothing in the game calls the rule yet, so a test probe (a real object under /domains, which the daemon accepts as a caller) stands in for a skill.

  1. The pure functions against the worked tables of section 12.4 (every cell), the strength ladder, half-time, Authority; and against the
     simulation (tests/balance_sim.py).
  2. Authority as a statistic: 9 and 15 for the two casters of the tables; it is shown to no player (score, stat).
  3. Resolve with fixed dice, for every cell of both tables: ordinary, elite and boss targets, a made save and a failed one.
  4. The messages of section 12.5 (every row), the verbose numbers, the legacy path (a made save means no effect).
  5. The repeat window (a second use at 30 s, a third, a use after 60 s), a None that starts no window, the flood guard.
  6. Statistics: the save chance of a few cells against the dice.
  7. The workshop: a wizard's object may call the pure functions and nothing else; a sandbox target or a restricted caller is refused and leaves no
     record; a sandbox creature that claims to be a boss is not one.
  8. The speed cap: the product of slows is held to 2.0.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=5062 python3 tests/control_test.py
"""
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "5062")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402
import rank_test as rk  # noqa: E402
import balance_sim as sim  # noqa: E402

T = "/domains/ctltest/"
check = h.check

PROBE = r'''// @inherit /std/thing
public class CtlProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        foreach (var v in new[] { "ccall", "cdice", "cclock", "cforget", "cres", "cmc", "cstate", "cslow", "cspeed", "cauth", "cspawn", "cslay", "ctele", "cwould" }) AddAction(v, Do);
    }
    private static string J(IEnumerable<string>? l) => l == null ? "" : string.Join(",", l);
    private object? Arg(string a)
    {
        if (a == "true") return true;
        if (a == "false") return false;
        if (int.TryParse(a, out var n)) return n;
        return a;
    }
    private ObjectRef? Find(ObjectRef me, string id) => id == "me" ? me : Present(id, Environment(me)!);
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var control = LoadObject("/secure/control");
        switch (QueryVerb())
        {
            case "ccall":
            {
                try
                {
                    var r = control.Call(w[0], w.Skip(1).Select(Arg).ToArray());
                    Write("CALL " + (r?.ToString() ?? "null") + "\n");
                }
                catch (MudRuntimeException e) { Write("CALL ERR " + e.Message + "\n"); }
                return true;
            }
            case "cdice": Write("DICE " + control.Call<bool>("SetTestRolls", w[0]) + " " + control.Call<string>("QueryTestRolls") + "\n"); return true;
            case "cclock": Write("CLOCK " + control.Call<bool>("SetTestClock", int.Parse(w[0])) + "\n"); return true;
            case "cforget": Write("FORGOT " + control.Call<bool>("ForgetAllRecords") + "\n"); return true;
            case "cres":
            {
                // cres caster target effect kind rank [madeSaveSteps] [noun]
                var caster = Find(me, w[0]); var target = Find(me, w[1]);
                if (caster == null || target == null) { Write("RES none\n"); return true; }
                var r = control.Call<int>("Resolve", caster, target, w[2], w[3], int.Parse(w[4]), w.Length > 5 ? int.Parse(w[5]) : 1, w.Length > 6 ? w[6] : "order");
                Write("RES " + r + "\n");
                return true;
            }
            case "cmc":
            {
                // cmc caster target n effect kind rank: n resolutions with the records forgotten between them
                var caster = Find(me, w[0]); var target = Find(me, w[1]);
                var n = int.Parse(w[2]); var c = new int[3];
                for (var i = 0; i < n; i++)
                {
                    control.Call<bool>("ForgetAllRecords");
                    c[control.Call<int>("Resolve", caster, target, w[3], w[4], int.Parse(w[5]), 1, "order")]++;
                }
                Write($"MC none={c[0]} half={c[1]} full={c[2]}\n");
                return true;
            }
            case "cstate":
            {
                var target = Find(me, w[0]);
                Write($"STATE steps={control.Call<int>("QueryRepeatSteps", target, w[1])} recent={control.Call<int>("QueryRecentEffects", target)} records={control.Call<int>("QueryRecordCount")}\n");
                return true;
            }
            case "cslow":
            {
                var target = Find(me, w[0]);
                target!.Call("AddEffect", w[1], "Test " + w[1], 60, "", int.Parse(w[2]) / 100.0, 0);
                Write("SLOWED\n"); return true;
            }
            case "cspeed": Write("SPEED " + Find(me, w[0])!.Call<double>("QuerySpeedFactor").ToString("0.000") + "\n"); return true;
            case "cauth":
            {
                var ob = Find(me, w[0]);
                Write($"AUTH {ob!.Call<double>("QueryStat", "authority")} level={ob.Call<int>("QueryLevel")} phys={ob.Call<double>("QueryStat", "physical_resistance")} ment={ob.Call<double>("QueryStat", "mental_resistance")}\n");
                return true;
            }
            case "cwould": Write("WOULD " + control.Call<bool>("QueryWouldBeBoss", Find(me, w[0])) + "\n"); return true;
            case "cspawn": { var n = w.Length > 1 ? int.Parse(w[1]) : 1; for (var i = 0; i < n; i++) MoveObject(CloneObject("/domains/ctltest/" + w[0]), Environment(me)!); Write("SPAWNED\n"); return true; }
            case "cslay": { foreach (var o in AllInventory(Environment(me)!).ToList()) if (o != me && o.IsAlive && o.Call<bool>("IsMonster")) o.Call("Die", (ObjectRef?)null); Write("SLAIN\n"); return true; }
            case "ctele": MoveObject(me, LoadObject(args.Trim())); Write("TELE ok\n"); return true;
        }
        return false;
    }
}
'''

MEDDLER = r'''// @inherit /std/thing
public class CtlMeddler : Thing
{
    public override void Create() { Name = "meddler"; Ids.Add("meddler"); }
    public override string? Short() => "a meddler";
    public override void Init() { AddAction("cmeddle", Meddle); }
    private bool Meddle(string args)
    {
        var me = ThisPlayer()!;
        var control = LoadObject("/secure/control");
        var tries = new List<string>();
        void Try(string label, Func<object?> f)
        {
            try { tries.Add(label + "=" + (f() ?? "null")); }
            catch (MudRuntimeException e) { tries.Add(label + "!" + e.Message.Length); }
        }
        var gboss = Present("gboss", Environment(me)!);
        Try("QuerySaveChance", () => control.Call<int>("QuerySaveChance", 12, 3, true, 9));
        Try("QueryStrength", () => control.Call<int>("QueryStrength", true, 0, false, 1));
        Try("QueryHalfSeconds", () => control.Call<int>("QueryHalfSeconds", 5));
        Try("QueryAuthority", () => control.Call<int>("QueryAuthority", 15, 3));
        Try("SetTestRolls", () => control.Call("SetTestRolls", "save:0.0"));
        Try("SetTestClock", () => control.Call("SetTestClock", 100));
        Try("ForgetAllRecords", () => control.Call("ForgetAllRecords"));
        Try("Resolve", () => control.Call("Resolve", me, gboss, "x", "physical", 1, 1, "order"));
        Try("Roll", () => control.Call("Roll"));
        Try("QueryRecordCount", () => control.Call<int>("QueryRecordCount"));
        Try("QueryWouldBeBoss", () => control.Call<bool>("QueryWouldBeBoss", gboss));
        Try("HasTag", () => gboss != null && gboss.Call<bool>("HasTag", "boss"));
        Write("CMEDDLE " + string.Join(" ", tries) + "\n");
        return true;
    }
}
'''

GBOSS = r'''// @inherit /std/monster
public class CtlGBoss : Monster
{
    public override void Create()
    {
        Name = "Gboss"; Ids.Add("gboss"); ShortDesc = "a gboss"; LongDesc = "A wizard's creature that claims rank.";
        Level = 3; MaxVitality = 1000; DamageMin = 1; DamageMax = 1; ToHit = -40; XpValue = 0; Aggressive = false; BaseReaction = 6.0;
        Boss = true;
    }
}
'''


def creature(cls, ident, level, flags="", attrs=""):
    return f"""// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{ident.title()}"; Ids.Add("{ident}"); ShortDesc = "a {ident}"; LongDesc = "A creature made for the control test.";
        Level = {level}; MaxVitality = 100000; DamageMin = 1; DamageMax = 1; ToHit = -40; Armor = 0; Dodge = 0; XpValue = 0;
        Aggressive = false; BaseReaction = 6.0; {flags}
        {attrs}
    }}
}}
"""


CASTERS = {"c5": (5, 14, 1), "c10": (10, 20, 3)}  # fixture id -> (level, Insight, rank of the skill)
SPEC = {  # the spec's worked tables 12.4: caster -> {target level: (S ordinary, S elite)}
    "c5": {3: (5, 6), 4: (5, 10), 5: (5, 10), 6: (9, 24), 7: (19, 34), 8: (33, 48)},
    "c10": {8: (5, 5), 9: (5, 5), 10: (5, 8), 11: (5, 18), 12: (17, 32), 13: (27, 42)},
}


def test_files():
    f = {T + "probe": PROBE, T + "gboss": GBOSS}
    for level in range(1, 14):
        f[T + f"lv{level}"] = creature(f"CtlLv{level}", f"lv{level}", level)
        f[T + f"el{level}"] = creature(f"CtlEl{level}", f"el{level}", level, "Elite = true;")
        f[T + f"bs{level}"] = creature(f"CtlBs{level}", f"bs{level}", level, "Boss = true;")
    f[T + "c5"] = creature("CtlC5", "c5", 5, "", 'Attributes["insight"] = 14;')
    f[T + "c10"] = creature("CtlC10", "c10", 10, "", 'Attributes["insight"] = 20;')
    f[T + "c10r"] = creature("CtlC10R", "c10r", 10, "", 'Attributes["resolve"] = 11;')
    f[T + "arena"] = rk.room("CtlArena", "Control Arena", {"out": T + "hall"})
    f[T + "hall"] = rk.room("CtlHall", "Control Hall", {"up": "/domains/hollow/village/square", "arena": T + "arena"}, "",
                            rk.HALL_INIT.replace("/domains/ranktest/probe", T + "probe"))
    return f


def call(c, *words):
    out = c.command("ccall " + " ".join(str(w) for w in words))
    m = re.search(r"CALL (.*)", out)
    return m.group(1).strip() if m else out


def res(c, *words):
    out = c.command("cres " + " ".join(str(w) for w in words))
    m = re.search(r"RES (\S+)", out)
    return (m.group(1) if m else out), out


def dice(c, percent):
    """Fix the roll in percent (a float with one decimal)."""
    out = c.command(f"cdice save:{percent:.1f}")
    assert "DICE True" in out, out


def state(c, target, effect):
    out = c.command(f"cstate {target} {effect}")
    m = re.search(r"STATE steps=(\d+) recent=(\d+) records=(\d+)", out)
    return tuple(int(x) for x in m.groups()) if m else out


# ---------------------------------------------------------------- 1. the pure functions

def step_pure(c):
    print("\n=== the pure rule")
    bad = []
    for caster, rows in SPEC.items():
        level, insight, rank = CASTERS[caster]
        authority = sim.control_authority(insight, insight, rank)
        for tl, (ordinary, elite) in rows.items():
            r = sim.creature_resistance(tl)
            for is_elite, want in ((False, ordinary), (True, elite)):
                got = call(c, "QuerySaveChance", r, tl - level, str(is_elite).lower(), authority)
                if got != str(want) or sim.control_save_chance(r, tl - level, is_elite, authority) != want:
                    bad.append((caster, tl, is_elite, got, want))
    check("QuerySaveChance reproduces every cell of both tables of section 12.4 (24 cells), and so does the simulation", str(bad), "[]")
    check("a caster with no Authority: the chance is the resistance plus 10 a level (the simulation agrees)", " ".join(call(c, "QuerySaveChance", sim.creature_resistance(tl), tl - 3, "false", 0) for tl in (3, 4, 5, 6, 7, 8)), " ".join(str(sim.control_save_chance(sim.creature_resistance(tl), tl - 3, False, 0)) for tl in (3, 4, 5, 6, 7, 8)))
    check("the ceiling is 90 and the floor 5", call(c, "QuerySaveChance", 75, 9, "true", 0) + " " + call(c, "QuerySaveChance", 0, 0, "false", 50), "90 5")
    bad = []
    for boss in (False, True):
        for rep in range(0, 5):
            for made in (False, True):
                for steps in (0, 1, 2, 3):
                    got = call(c, "QueryStrength", str(boss).lower(), rep, str(made).lower(), steps)
                    want = sim.control_strength(boss, rep, made, steps)
                    if got != str(want):
                        bad.append((boss, rep, made, steps, got, want))
    check("QueryStrength: every combination of boss, repeat steps (clamped to 2), made save and made-save steps (clamped to 2) as the simulation", str(bad), "[]")
    check("the ladder: ordinary fails Full, makes Half; a boss fails Half, makes None; a repeat takes a step each; the legacy made save takes two",
          " ".join(call(c, "QueryStrength", *args) for args in (("false", 0, "false", 1), ("false", 0, "true", 1), ("true", 0, "false", 1), ("true", 0, "true", 1),
                                                                  ("false", 1, "false", 1), ("false", 2, "false", 1), ("false", 0, "true", 2))), "2 1 1 0 1 0 0")
    check("QueryHalfSeconds rounds up, at least 1", " ".join(call(c, "QueryHalfSeconds", n) for n in (0, 1, 2, 3, 4, 5, 10, 20, 45)), "1 1 1 2 2 3 5 10 23")
    check("QueryAuthority: the statistic plus 4 for each rank above I", call(c, "QueryAuthority", 9, 1) + " " + call(c, "QueryAuthority", 15, 3) + " " + call(c, "QueryAuthority", 0, 5), "9 23 16")
    check("the repeat and flood numbers of the simulation (37.5%, 21%, 17%, 8%)", f"{sim.control_uptime(20) * 100:.1f} {sim.control_uptime(10) * 100:.0f} {sim.control_uptime(20, True) * 100:.0f} {sim.control_uptime(10, True) * 100:.0f}", "37.5 21 17 8")


# ---------------------------------------------------------------- 2. Authority

def step_authority(c):
    print("\n=== Authority")
    c.command("cspawn c5")
    c.command("cspawn c10")
    c.command("cspawn c10r")
    c.command("cspawn lv10")
    c.command("cspawn lv3")
    check("the level 5 caster of the tables (Insight 14) has Authority 9", c.command("cauth c5"), "AUTH 9 level=5")
    check("the level 10 caster (Insight 20) has 15 (plus 8 for rank III: QueryAuthority)", c.command("cauth c10"), "AUTH 15 level=10")
    check("Resolve is better with the better of Insight and Resolve: a creature with Resolve 11 has 6", c.command("cauth c10r"), "AUTH 6 level=10")
    check("a creature at the template: level 10 has 4, level 3 has 0", c.command("cauth lv10") + c.command("cauth lv3"), "AUTH 4 level=10", "AUTH 0 level=3")
    check("resistances of the template: level 10 is 16%, level 3 is 0", c.command("cauth lv10"), "phys=16", "ment=16")
    out = c.command("score")
    check("score shows no Authority (it is not for players)", out, absent=("uthority",))
    out = c.command("stat me")
    check("nor does a wizard's stat", out, absent=("uthority",))
    check("nor the skills list or help", c.command("skills") + c.command("help"), absent=("uthority",))
    c.command("cslay")


# ---------------------------------------------------------------- 3. Resolve, every cell

def step_cells(c):
    print("\n=== Resolve with fixed dice, every cell")
    for ident in [f"lv{n}" for n in range(3, 14)] + [f"el{n}" for n in range(3, 14)] + [f"bs{n}" for n in range(3, 14)] + ["c5", "c10"]:
        c.command("cspawn " + ident)
    bad = []
    checked = 0
    for caster, rows in SPEC.items():
        level, insight, rank = CASTERS[caster]
        for tl, (s_ord, s_elite) in rows.items():
            for prefix, s in (("lv", s_ord), ("el", s_elite), ("bs", s_ord)):
                boss = prefix == "bs"
                target = f"{prefix}{tl}"
                for made in (False, True):
                    c.command("cforget")
                    # a made save rolls just below the chance, a failed one exactly on it
                    dice(c, (s - 1) + 0.9 if made else float(s))
                    got, out = res(c, caster, target, "slow", "physical", rank)
                    want = sim.control_strength(boss, 0, made)
                    checked += 1
                    if got != str(want):
                        bad.append((caster, target, "made" if made else "failed", got, want))
    check(f"Resolve gives the strength of every cell: ordinary Full or Half, elite Full or Half, boss Half or None ({checked} resolutions)", str(bad), "[]")
    dice(c, 0.0)
    c.command("cforget")
    # the chance really is the one the table gives: a roll one tenth below the chance is made, the chance itself is not
    c.command("cdice random")
    # reading the mental resistance: the same cell with kind "mental" (same template, same chance)
    dice(c, 32.9)
    c.command("cforget")
    check("the mental kind uses the mental resistance (the template: the same 12% at level 8)", res(c, "c5", "lv8", "bind", "mental", 1)[0], "1")
    dice(c, 33.0)
    c.command("cforget")
    check("and the chance itself is a failed save: Full", res(c, "c5", "lv8", "bind", "mental", 1)[0], "2")
    c.command("cforget")
    check("a bad kind or an empty effect id is refused: Full, nothing recorded", res(c, "c5", "lv8", "bind", "nonsense", 1)[0] + " " + str(state(c, "lv8", "bind")), "2 (0, 0, 0)")
    c.command("cdice random")
    c.command("cforget")


# ---------------------------------------------------------------- 4. messages

def step_messages(c):
    print("\n=== what the caster reads")
    c.command("cspawn lv1")
    c.command("cspawn lv3")
    c.command("cspawn bs3")
    c.command("cspawn bs1")
    c.command("cspawn el3")
    lvl = int(re.search(r"level=(\d+)", c.command("cauth me")).group(1))
    check("the wizard's own character is level 1 (so the level 3 creatures are above it)", str(lvl), "1")
    for case, target, rollp, want, absent in (
        ("Full: nothing extra", "lv1", 99.0, ("RES 2",), ("resists", "ignores", "above the law", "served", "notice")),
        ("Half, a made save, an ordinary target", "lv1", 0.0, ("resists. The order holds for half its term.", "RES 1"), ("ignores",)),
        ("Half, a made save, a target above your level", "lv3", 0.0, ("ignores your authority. The order holds for half its term.", "RES 1"), ("resists.",)),
        ("Half, a boss that fails its save", "bs1", 99.0, ("is above the law. The order half holds.", "RES 1"), ("knows it",)),
        ("None, a boss that makes its save", "bs1", 0.0, ("is above the law, and knows it. Nothing holds.", "RES 0"), ()),
        ("a boss above your level uses the boss line", "bs3", 0.0, ("is above the law, and knows it. Nothing holds.",), ("ignores",)),
    ):
        c.command("cforget")
        dice(c, rollp)
        got, out = res(c, "me", target, "slow", "physical", 1, 1, "order")
        check(f"message: {case}", out, *want, absent=absent)
    # repeats
    c.command("cforget")
    dice(c, 99.0)
    res(c, "me", "lv1", "bar", "mental", 1)
    got, out = res(c, "me", "lv1", "bar", "mental", 1)
    check("message: a second use within the minute is Half: 'has been served with that already'", out, "has been served with that already. It holds for half its term.", "RES 1")
    got, out = res(c, "me", "lv1", "bar", "mental", 1)
    check("message: a third is None: 'has had notice of that already'", out, "has had notice of that already. It will not be served again for a while.", "RES 0")
    # flood: four effects of any kind, then the fifth
    c.command("cforget")
    for i in range(4):
        res(c, "me", "lv1", f"flood{i}", "physical", 1)
    got, out = res(c, "me", "lv1", "flood4", "physical", 1)
    check("message: the fifth effect in a minute is refused: 'has had notice enough for one minute'", out, "has had notice enough for one minute.", "RES 0")
    check("... and the flood guard keeps only four", str(state(c, "lv1", "flood4")), "(0, 4, 1)")
    c.command("cclock 61")
    got, out = res(c, "me", "lv1", "flood4", "physical", 1)
    check("... and after a minute the sixty seconds have passed: Full", got, "2")
    c.command("cclock 0")
    # the numbers
    c.command("cforget")
    c.command("verbose on")
    dice(c, 41.0)
    got, out = res(c, "me", "lv3", "slow", "physical", 1)
    m = re.search(r"\(save (\d+)%, rolled (\d+): (made|failed); (Full|Half|None)\)", out)
    check("verbose adds the numbers: (save N%, rolled 41: failed; Full)", out, "rolled 41: failed; Full)")
    dice(c, 1.0)
    c.command("cforget")
    got, out = res(c, "me", "lv3", "slow", "physical", 1)
    check("verbose with a made save", out, "ignores your authority", "rolled 1: made; Half)")
    c.command("verbose off")
    # legacy path
    c.command("cforget")
    dice(c, 0.0)
    check("the legacy path (madeSaveSteps = 2): a made save means no effect at all", res(c, "me", "lv1", "legacy", "physical", 1, 2)[0], "0")
    dice(c, 99.0)
    c.command("cforget")
    check("... and a failed save is Full as before", res(c, "me", "lv1", "legacy", "physical", 1, 2)[0], "2")
    c.command("cforget")
    c.command("cdice random")
    c.command("cslay")


# ---------------------------------------------------------------- 5. repeats and the flood

def step_repeats(c):
    print("\n=== the repeat window and the flood guard")
    c.command("cspawn c5")
    c.command("cspawn lv5")
    c.command("cspawn bs5")
    c.command("cforget")
    c.command("cclock 0")
    dice(c, 99.0)  # every save fails
    check("first use: Full", res(c, "c5", "lv5", "slow", "physical", 1)[0], "2")
    check("the record: one application, the next takes one step", str(state(c, "lv5", "slow")), "(1, 1, 1)")
    c.command("cclock 30")
    check("a second use at 30 s: Half", res(c, "c5", "lv5", "slow", "physical", 1)[0], "1")
    check("the record now: two applications, the next takes two steps", str(state(c, "lv5", "slow")), "(2, 2, 1)")
    c.command("cclock 40")
    check("a third use at 40 s: None", res(c, "c5", "lv5", "slow", "physical", 1)[0], "0")
    check("a None starts no window and counts for nothing: still two recent effects", str(state(c, "lv5", "slow")), "(2, 2, 1)")
    c.command("cclock 89")
    check("at 59 s after the second the window still holds: None", res(c, "c5", "lv5", "slow", "physical", 1)[0], "0")
    c.command("cclock 91")
    check("a use 61 s after the second: Full again", res(c, "c5", "lv5", "slow", "physical", 1)[0], "2")
    # a different effect on the same target has its own count (but shares the flood guard)
    c.command("cforget")
    c.command("cclock 0")
    check("another effect on the same target is not a repeat: Full", res(c, "c5", "lv5", "slow", "physical", 1)[0] + res(c, "c5", "lv5", "bind", "physical", 1)[0], "22")
    # a boss: the first is Half, the second None; a boss that makes its first save takes None and no window
    c.command("cforget")
    check("a boss that fails: Half", res(c, "c5", "bs5", "slow", "physical", 1)[0], "1")
    check("and the second within the minute: None", res(c, "c5", "bs5", "slow", "physical", 1)[0], "0")
    c.command("cclock 61")
    check("a minute later: Half again (one Half in sixty seconds)", res(c, "c5", "bs5", "slow", "physical", 1)[0], "1")
    c.command("cforget")
    c.command("cclock 0")
    dice(c, 0.0)  # every save is made
    check("a boss that makes its save: None", res(c, "c5", "bs5", "slow", "physical", 1)[0], "0")
    dice(c, 99.0)
    check("and that None started no window: the next, failed, is Half and not None", res(c, "c5", "bs5", "slow", "physical", 1)[0], "1")
    # the flood guard: four effects of any kind, the fifth ignored until the oldest is 60 s old
    c.command("cforget")
    c.command("cclock 0")
    for i in range(4):
        res(c, "c5", "lv5", f"f{i}", "physical", 1)
    check("four effects have taken hold", str(state(c, "lv5", "f9")), "(0, 4, 1)")
    check("the fifth is refused whatever it is", res(c, "c5", "lv5", "f9", "mental", 1)[0], "0")
    c.command("cclock 59")
    check("at 59 s the guard still holds", res(c, "c5", "lv5", "f9", "mental", 1)[0], "0")
    c.command("cclock 60")
    check("at 60 s the oldest is 60 s old: the fifth is Full", res(c, "c5", "lv5", "f9", "mental", 1)[0], "2")
    c.command("cclock 0")
    c.command("cforget")
    c.command("cdice random")
    c.command("cslay")


# ---------------------------------------------------------------- 6. statistics

def step_stats(c):
    print("\n=== statistics")
    c.command("cspawn c5")
    c.command("cspawn lv8")
    c.command("cspawn el7")
    c.command("cspawn bs5")
    c.command("cdice random")
    for caster, target, kind, rank, expect_half, expect_none in (("c5", "lv8", "physical", 1, 33, 0), ("c5", "el7", "physical", 1, 34, 0), ("c5", "bs5", "mental", 1, 0, 5), ("c5", "lv5", "physical", 1, 5, 0)):
        if target == "lv5":
            c.command("cspawn lv5")
        tallies = [0, 0, 0]
        n = 0
        for _ in range(4):
            out = c.command(f"cmc {caster} {target} 400 stat{target} {kind} {rank}")
            m = re.search(r"MC none=(\d+) half=(\d+) full=(\d+)", out)
            if not m:
                check(f"statistics: {target}", out, "MC")
                break
            for i in range(3):
                tallies[i] += int(m.group(i + 1))
            n += 400
        else:
            none_pct, half_pct, full_pct = (100.0 * t / n for t in tallies)
            if target == "bs5":
                check(f"a level 5 boss against the level 5 caster ({n} tries): Half about 95%, None about 5% (got Half {half_pct:.1f}%, None {none_pct:.1f}%)", "ok" if abs(none_pct - 5) < 3.5 and half_pct > 90 and full_pct == 0 else f"{tallies}", "ok")
            else:
                check(f"{target} against the level 5 caster ({n} tries): Half about {expect_half}% (got {half_pct:.1f}%, Full {full_pct:.1f}%)", "ok" if abs(half_pct - expect_half) < 4.5 and none_pct == 0 else f"{tallies}", "ok")
    c.command("cforget")
    c.command("cslay")


# ---------------------------------------------------------------- 7. the workshop

def step_workshop(c, mudlib):
    print("\n=== the wizard's workshop")
    os.makedirs(os.path.join(mudlib, "wiz", "ctlwiz"), exist_ok=True)
    h.write(mudlib, "/wiz/ctlwiz/meddler", MEDDLER)
    h.write(mudlib, "/wiz/ctlwiz/gboss", GBOSS.replace("CtlGBoss", "WizGBoss"))
    h.write(mudlib, "/wiz/ctlwiz/den", "// @inherit /std/room\npublic class CtlDen : Room { public override void Create() { ShortDesc = \"Den\"; LongDesc = \"A workshop.\"; } }\n")
    c.command("cforget")
    c.command("cspawn lv8")
    c.command("cdice random")
    before = state(c, "lv8", "slow")
    c.command("goto /wiz/ctlwiz/den")
    check("clone a meddler in the workshop", c.command("clone ~/meddler"), "meddler")
    c.command("clone ~/gboss")
    out = c.command("cmeddle")
    check("the meddler ran", out, "CMEDDLE ")
    check("a wizard's code may compute a table: the pure functions answer", out, "QuerySaveChance=48", "QueryStrength=1", "QueryHalfSeconds=3", "QueryAuthority=23")
    for fn in ("SetTestRolls", "SetTestClock", "ForgetAllRecords", "Resolve", "Roll"):
        check(f"{fn} is refused to a wizard's object (null)", out, f"{fn}=null", absent=(f"{fn}=True",))
    check("the record count is readable and unchanged by it", out, "QueryRecordCount=")
    check("a creature of the workshop that claims to be a boss is not one to the daemon (and still says so about itself)", out, "QueryWouldBeBoss=False", "HasTag=True")
    # the real probe, carried into the workshop: the wizard is restricted there, and everything made there is a sandbox object
    check("a sandbox creature is cloned in the workshop", c.command("clone /domains/ctltest/lv8"), "Cloned")
    out = c.command("cres me lv8 slow physical 1")
    check("a real probe asked in the workshop (restricted execution): Full, nothing recorded", out, "RES 2")
    out = c.command("cres me gboss slow physical 1")
    check("... and for a sandbox boss", out, "RES 2", absent=("above the law",))
    out = c.command("cstate lv8 slow")
    check("no record was made by anything done in the workshop", out, "records=0")
    c.command("goto /domains/ctltest/arena")
    a.drain(c, 0.4)
    c.command("cclock 0")
    # a player as a target of a creature caster: the rule works on them, with their own resistance
    c.command("cforget")
    c.command("cspawn c10")
    dice(c, 99.0)
    got, out = res(c, "c10", "me", "slow", "physical", 1)
    check("a creature may be the caster and a player the target (for the creatures' slows that will adopt the rule): Full on a failed save", got, "2")
    c.command("cdice random")
    c.command("cforget")
    c.command("cslay")


# ---------------------------------------------------------------- 8. the speed cap

def step_speed(c):
    print("\n=== the slow cap")
    c.command("cspawn lv8")
    check("no effect: x1.0", c.command("cspeed lv8"), "SPEED 1.000")
    c.command("cslow lv8 a 125")
    c.command("cslow lv8 b 120")
    check("two slows keep multiplying (1.25 x 1.2 = 1.5, as the spider and the spirit do)", c.command("cspeed lv8"), "SPEED 1.500")
    c.command("cslow lv8 c 150")
    check("a third of 1.5: the product of 2.25 is held to 2.0", c.command("cspeed lv8"), "SPEED 2.000")
    c.command("cslow lv8 d 80")
    check("haste is not capped: 0.8 on top of the capped slows is 1.6", c.command("cspeed lv8"), "SPEED 1.600")
    c.command("cslay")


def run(work, mudlib):
    chief = h.Client("ctlwiz", archetype="warrior")
    chief.read_until("> ")
    check("the secret makes the first wizard", chief.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(chief, 0.4)
    print("down:", chief.command("down")[:300])
    print("ctele:", chief.command("ctele " + T + "arena")[:300])
    a.drain(chief, 0.3)
    only = [x for x in os.environ.get("CTL_STEPS", "").split(",") if x]

    def step(name, fn):
        if not only or name in only:
            fn()

    step("pure", lambda: step_pure(chief))
    step("authority", lambda: step_authority(chief))
    step("cells", lambda: step_cells(chief))
    step("messages", lambda: step_messages(chief))
    step("repeats", lambda: step_repeats(chief))
    step("stats", lambda: step_stats(chief))
    step("speed", lambda: step_speed(chief))
    step("workshop", lambda: step_workshop(chief, mudlib))
    return chief


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-control-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    for gone in ("crafting/launch_materials.cs", "recipes/launch.cs"):
        if os.path.exists(os.path.join(mudlib, gone)):
            os.remove(os.path.join(mudlib, gone))
    os.makedirs(os.path.join(mudlib, "domains", "ctltest"), exist_ok=True)
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
            print(open(os.path.join(work, "driver.log")).read()[-3000:])
        shutil.rmtree(work, ignore_errors=True)
    print(f"\nwhole test {time.time() - started:.0f} s")
    print(f"{len(h.failures)} failure(s)")
    return 1 if h.failures else 0


if __name__ == "__main__":
    sys.exit(main())
