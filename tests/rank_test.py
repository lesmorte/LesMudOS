#!/usr/bin/env python3
"""Tests for skill and spell ranks (docs/specs/skill-ranks.md).

Step by step, as built:
  1. The rank engine: every ranked skill's numbers at ranks I to V against the data (the simulator's tables, which
     `balance_sim.py check` compares with the /skills files), names by rank, the level cap (rank r at level 5 x (r - 1)), the
     `skills` list (rank and the next rank's level), an old save (rank I in everything, the new fields made on the next save).
  2. Unlocks: each requirement kind met, unmet, its progress line, and permanence (an unlock recorded for good); the counters
     (kills by creature kind for every participant, killing blows by skill for the one who struck it), saved; a wizard's kill counts
     as the kill credit does.
  3. Trainers: `ranks` and `train` at the village people and the hallmasters; refusals in words (level, gold, calling, skipping,
     not taught here); gold always paid; Arcane Bolt II's ten goblins; two trainers in one room.
  4. The skills: Arcane Bolt, Minor Heal, Blessing, Shield Bash, Precision Strike at rank I and II in play; Tracking's four cases
     (open exit, closed door, an unfound hidden way at rank I and II, a found hidden way) and that it finds nothing; Double Strike's
     separate hit rolls, its damage fraction and its names and hits at ranks III and IV; Jack of All Trades through fixed dice.
  5. The simulator: `balance_sim.py check` passes and the `ranks` tables print.
And a wizard-written probe that tries every new public method and changes nothing.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=4837 python3 tests/rank_test.py
"""
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4837")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402
import balance_sim as sim  # noqa: E402

T = "/domains/ranktest/"
CT = "/domains/crafttest/obj/"
SAMPLES = os.path.join(h.ROOT, "tests", "fixtures", "crafting", "samples")
check = h.check
ROMAN = ["", "I", "II", "III", "IV", "V"]


# ---------------------------------------------------------------- test content

def room(cls, short, exits, body="", extra=""):
    ex = "".join(f'        Exits["{d}"] = "{p}";\n' for d, p in exits.items())
    return f"""// @inherit /std/room
public class {cls} : Room
{{
    public override void Create()
    {{
        ShortDesc = "{short}";
        LongDesc = "A room made for the rank test.";
        ResetSeconds = 3000;
{ex}{body}    }}
{extra}}}
"""


HALL_INIT = """    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who != null && Interactive(who) && Present("probe", who) == null) MoveObject(CloneObject("/domains/ranktest/probe"), who);
    }
"""


def creature(cls, name, ident, hp=50000, blood="", skin="", dodge=0, armor=0, reaction=6.0, short=None):
    props = ""
    if blood:
        props += f'    public string Blood {{ get; set; }} = "{blood}";\n'
    if skin:
        props += f'    public string Skin {{ get; set; }} = "{skin}";\n'
    return f"""// @inherit /std/monster
public class {cls} : Monster
{{
{props}    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); ShortDesc = "{short or 'a ' + name.lower()}"; LongDesc = "A creature made for the rank test.";
        Level = 1; MaxVitality = {hp}; DamageMin = 1; DamageMax = 1; ToHit = -40; Armor = {armor}; Dodge = {dodge}; XpValue = 0;
        Aggressive = false;
        BaseReaction = {reaction};
    }}
}}
"""


def tutor(cls, name, ident, body):
    return f"""// @inherit /std/trainer
public class {cls} : Trainer
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); Ids.Add("tutor"); ShortDesc = "{name}"; LongDesc = "A tutor made for the rank test.";
        Level = 3; MaxVitality = 30;
{body}    }}
}}
"""


# A trusted probe (it lives under /domains): what a player cannot do by command, for the test only.
PROBE = r'''// @inherit /std/thing
// @use /std/skill
public class RkProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        foreach (var v in new[] { "rkgold", "rklevel", "rkspawn", "rktele", "rkquest", "rkrenown", "rkstate", "rkvals", "rktitles", "rkcap",
                                  "rkhurt", "rkfill", "rkmod", "rkhit", "rkgive", "rkfound", "rkrolls", "rkchance", "rkprof", "rkspeed", "rkgrow", "rkslay", "rkdrop", "rkself" })
            AddAction(v, Do);
    }
    private ObjectRef? Named(string name, ObjectRef room) =>
        Users().FirstOrDefault(u => u.Call<string>("QueryName")!.ToLowerInvariant() == name) ?? Present(name, room);
    private static string Join(Dictionary<string, int>? d) => d == null ? "" : string.Join(",", d.OrderBy(k => k.Key).Select(k => k.Key + "=" + k.Value));
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var room = Environment(me)!;
        switch (QueryVerb())
        {
            case "rkgold": me.Call("AddGold", int.Parse(w[0])); Write("GOLD " + me.Call<int>("QueryGold") + "\n"); return true;
            case "rklevel":
            {
                var want = int.Parse(w[0]);
                while (me.Call<int>("QueryLevel") < want) me.Call("AddXp", me.Call<int>("QueryXpForLevel", me.Call<int>("QueryLevel") + 1) - me.Call<int>("QueryXp"));
                Write("LEVEL " + me.Call<int>("QueryLevel") + "\n"); return true;
            }
            case "rkspawn": { var n = w.Length > 1 ? int.Parse(w[1]) : 1; for (var i = 0; i < n; i++) MoveObject(CloneObject("/domains/ranktest/" + w[0]), room); Write("SPAWNED " + n + "\n"); return true; }
            case "rktele": MoveObject(me, LoadObject(args.Trim())); Write("TELE ok\n"); return true;
            case "rkquest": me.Call("SetQuest", w[0], 2, "The Test Errand"); Write("QUEST " + me.Call<int>("QueryQuest", w[0]) + "\n"); return true;
            case "rkrenown":
            {
                var r = LoadObject("/secure/events").Call<string>("Report", "quest.local", me, new List<ObjectRef> { me }, room, w[0], new Dictionary<string, string> { ["name"] = "Test" });
                Write("RENOWN " + LoadObject("/secure/renown").Call<int>("QueryRenown", me.Call<string>("QueryName")) + " " + r + "\n"); return true;
            }
            case "rkstate":
            {
                var who = w.Length > 0 ? Named(w[0], room) : me;
                if (who == null) { Write("STATE none\n"); return true; }
                Write("STATE ranks[" + Join(who.Call<Dictionary<string, int>>("QuerySkillRanks")) + "] unlocks[" + string.Join(",", who.Call<List<string>>("QueryRankUnlocks") ?? new List<string>())
                      + "] kills[" + Join(who.Call<Dictionary<string, int>>("QueryKillCounts")) + "] with[" + Join(who.Call<Dictionary<string, int>>("QueryKillsWithCounts")) + "] gold " + who.Call<int>("QueryGold") + "\n");
                return true;
            }
            case "rkvals":
            {
                var skill = LoadObject("/skills/" + w[0]);
                var parts = new List<string>();
                foreach (var key in w[1].Split(','))
                    for (var r = 1; r <= 5; r++) parts.Add($"{key}{r}={skill.Call<double>("RankValue", key, r):0.###}/{skill.Call<int>("RankInt", key, r)}");
                Write("VALS " + string.Join(" ", parts) + "\n"); return true;
            }
            case "rktitles":
            {
                var skill = LoadObject("/skills/" + w[0]);
                var parts = new List<string>();
                for (var r = 1; r <= 5; r++) parts.Add(r + ":" + skill.Call<string>("QueryTitleAt", r) + (skill.Call<bool>("QueryRankExists", r) ? "" : "(none)"));
                Write("TITLES " + string.Join("|", parts) + "\n"); return true;
            }
            case "rkcap":
            {
                var parts = new List<string>();
                for (var r = 1; r <= 6; r++) parts.Add($"r{r}=L{Skill.LevelForRank(r)}");
                foreach (var l in new[] { 1, 4, 5, 9, 10, 14, 15, 20, 100 }) parts.Add($"L{l}=r{Skill.RankCapForLevel(l)}");
                Write("CAP " + string.Join(" ", parts) + " step " + Skill.RankLevelStep + "\n"); return true;
            }
            case "rkself":
            {
                var heal = LoadObject("/skills/minor_heal");
                Write("SELF " + string.Join(",", Enumerable.Range(1, 5).Select(r => heal.Call<int>("RankInt", "heal", r) + ">" + heal.Call<int>("QuerySelfHealAt", r))) + "\n"); return true;
            }
            case "rkfill": me.Call("FillResources"); Write("FILLED\n"); return true;
            case "rkhurt": me.Call("Heal", 99999); me.Call("TakeDamage", int.Parse(w[0]), null); Write("HURT " + me.Call<int>("QueryVitality") + "/" + me.Call<int>("QueryMaxVitality") + "\n"); return true;
            case "rkmod": Write("MOD tohit " + me.Call<double>("QueryMod", "tohit") + " armor " + me.Call<double>("QueryMod", "armor") + "\n"); return true;
            case "rkhit":
            {
                // Damage a creature here in a player's name (the record that shares a kill), without killing it.
                var target = Present(w[0], room);
                var from = Named(w[1], room);
                Write("HIT " + (target == null || from == null ? "none" : target.Call<int>("ApplyDamage", int.Parse(w[2]), from).ToString()) + "\n"); return true;
            }
            case "rkgive": { var ob = CloneObject(w[0].StartsWith("/") ? w[0] : "/domains/ranktest/" + w[0]); MoveObject(ob, me); Write("GAVE " + ob.Name + "\n"); return true; }
            case "rkdrop": { var ob = Present(w[0], me); if (ob != null) ob.Call("Vanish"); Write("DROPPED " + (ob != null && !ob.IsAlive) + "\n"); return true; }
            case "rkfound": Write("FOUND " + me.Call<bool>("QueryFoundExit", w[0], w[1]) + "\n"); return true;
            case "rkrolls": Write("ROLLS " + LoadObject("/secure/crafting").Call<bool>("SetTestRolls", w[0]) + "\n"); return true;
            case "rkchance": Write("CHANCE " + LoadObject("/secure/crafting").Call<double>("RungChance", int.Parse(w[0]), int.Parse(w[1])) + "\n"); return true;
            case "rkprof":
            {
                var who = w.Length > 0 ? Named(w[0], room) : me;
                Write("PROF " + Join(who?.Call<Dictionary<string, int>>("QueryProficiencies")) + "\n"); return true;
            }
            case "rkspeed":
            {
                var who = Named(w[0], room);
                Write("SPEED " + (who == null ? "none" : LoadObject("/secure/crafting").Call<double>("LearnSpeedOf", who).ToString("0.###")) + "\n"); return true;
            }
            case "rkgrow":
            {
                var g = LoadObject("/secure/crafting").Call<List<int>>("GrowthForSpeed", int.Parse(w[0]), double.Parse(w[1]), int.Parse(w[2]), double.Parse(w[3]), double.Parse(w[4]));
                Write("GROW " + string.Join(",", g ?? new List<int>()) + "\n"); return true;
            }
            case "rkslay":
            {
                var n = 0;
                foreach (var o in AllInventory(room).ToList())
                    if (o != me && o.IsAlive && o.Call<bool>("IsMonster")) { o.Call("Die", (ObjectRef?)null); n++; }
                Write("SLAIN " + n + "\n"); return true;
            }
        }
        return false;
    }
}
'''

# A wizard's object: every new public method that could change ranks, unlocks, counters or gold, called by code under /wiz.
MEDDLER = r'''// @inherit /std/thing
public class Meddler : Thing
{
    public override void Create() { Name = "meddler"; Ids.Add("meddler"); }
    public override string? Short() => "a meddler";
    public override void Init() { AddAction("meddle", Meddle); }
    private bool Meddle(string args)
    {
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var room = Environment(ThisPlayer()!)!;
        var who = Present(w[0], room);
        if (who == null) { Write("MEDDLE none\n"); return true; }
        var gob = Present("gob", room);
        var ranks = LoadObject("/secure/ranks");
        var tries = new List<string>();
        void Try(string label, Func<object?> f)
        {
            try { tries.Add(label + "=" + (f() ?? "null")); }
            catch (MudRuntimeException e) { tries.Add(label + "!" + e.Message.Length); }
        }
        Try("RankSetSkill", () => who.Call("RankSetSkill", "double_strike", 4));
        Try("RankRecordUnlock", () => who.Call("RankRecordUnlock", "/domains/ranktest/tutor_k|double_strike|2"));
        Try("RankCountKill", () => who.Call("RankCountKill", "goblin"));
        Try("RankCountKillWith", () => who.Call("RankCountKillWith", "double_strike"));
        Try("RankPay", () => who.Call("RankPay", 1));
        Try("RankSave", () => who.Call("RankSave"));
        Try("Train", () => ranks.Call("Train", who, "double_strike", 2, 0, new List<string>()));
        Try("RecordIfMet", () => ranks.Call("RecordIfMet", who, "double_strike", 2, new List<string> { "level 1" }));
        Try("CreditKill", () => ranks.Call("CreditKill", new List<ObjectRef> { who }, who, "goblin", "double_strike"));
        Try("OfferState", () => (ranks.Call<List<string>>("OfferState", who, "/domains/ranktest/tutor_k", "double_strike", 2, new List<string>()) ?? new List<string>()).Count);
        Try("PriceFor", () => ranks.Call("PriceFor", 2));
        if (gob != null)
        {
            // A death forced by a wizard's object: the record names the victim, the blow is the meddler's.
            Try("ApplyDamage", () => gob.Call("ApplyDamage", 5, who));
            Try("FinishDamage", () => { gob.Call("ApplyDamage", 99999, who); gob.Call("FinishDamage", who); return gob.IsAlive; });
        }
        var gob2 = Present("gob", room);
        if (gob2 != null) Try("Die", () => { gob2.Call("ApplyDamage", 3, who); gob2.Call("Die", who); return gob2.IsAlive; });
        Try("SetTestRolls", () => LoadObject("/secure/crafting").Call("SetTestRolls", "growth:0"));
        // Timed effects are open to any object: they must not speed learning.
        who.Call("AddEffect", "meddle", "Meddling", 600, "learn_speed=1000", 1.0, 0);
        Try("LearnSpeedOf", () => LoadObject("/secure/crafting").Call<double>("LearnSpeedOf", who));
        Write("MEDDLE " + string.Join(" ", tries) + "\n");
        return true;
    }
}
'''

TRACK_ROOMS = {}


def test_files():
    f = {}
    f[T + "probe"] = PROBE
    f[T + "gob"] = creature("RkGob", "Test Goblin", "gob", hp=1, blood="goblin")
    f[T + "gob12"] = creature("RkGob12", "Sturdy Goblin", "gob", hp=12, blood="goblin")
    f[T + "rat"] = creature("RkRat", "Test Rat", "rat", hp=1, skin="rat", blood="rat")
    f[T + "dummy"] = creature("RkDummy", "Target", "target", hp=50000)
    f[T + "dodger"] = creature("RkDodger", "Dodger", "dodger", hp=50000, dodge=50)
    f[T + "sword"] = """// @inherit /std/weapon
public class RkSword : Weapon
{
    public override void Create()
    {
        Name = "Even Sword"; Ids.Add("evensword"); ShortDesc = "an even sword"; LongDesc = "Every blow the same.";
        Category = "sword"; DamageMin = 10; DamageMax = 10; Recovery = 1.0; Value = 4;
    }
}
"""
    f[T + "token"] = """// @inherit /std/item
public class RkToken : Item
{
    public override void Create() { Name = "Brass Token"; Ids.Add("token"); ShortDesc = "a brass token"; LongDesc = "A token for the rank test."; Value = 1; }
}
"""
    f[T + "key"] = """// @inherit /std/item
public class RkKey : Item
{
    public override void Create() { Name = "Iron Key"; Ids.Add("rkkey"); ShortDesc = "an iron key"; LongDesc = "It opens the test door."; Value = 1; }
}
"""
    # The tutors: each teaches the Adventurer's Double Strike II on level, 10 gold and one requirement kind.
    kinds = {
        "q": ("quest rk-errand", "Finish my errand first."),
        "k": ("kills goblin 2", "Two goblins, if you please."),
        "w": ("kills_with double_strike 1", "Strike one down with it first."),
        "r": ("renown 1", "Let people hear of you first."),
        "s": ("standing assembly 5", ""),
        "i": ("item /domains/ranktest/token", "Show me the token."),
        "t": ("item /domains/ranktest/token take", "Bring me a token to keep."),
        "z": ("nonsense 3", ""),
    }
    for k, (req, line) in kinds.items():
        body = f'        TeachCalling("adventurer", 2, 2);\n        AddRankOffer("double_strike", 2, 10, "{req}");\n'
        if line:
            body += f'        AddRankLine("{req}", "{line}");\n'
        f[T + f"tutor_{k}"] = tutor(f"RkTutor{k.upper()}", f"Tutor {k.upper()}", f"tutor{k}", body)
        f[T + f"room_{k}"] = room(f"RkRoom{k.upper()}", f"Tutor Room {k.upper()}", {"out": T + "hall"}, f'        AddSpawn("{T}tutor_{k}");\n')
    # Two trainers in one room, each for a different calling: each answers only its own.
    f[T + "tutor_wa"] = tutor("RkTutorWa", "Tutor Wa", "tutorwa", '        TeachCalling("warrior", 2, 3);\n')
    f[T + "tutor_ma"] = tutor("RkTutorMa", "Tutor Ma", "tutorma", '        TeachCalling("mage", 2, 3);\n')
    f[T + "pair"] = room("RkPair", "Tutor Pair", {"out": T + "hall"}, f'        AddSpawn("{T}tutor_wa");\n        AddSpawn("{T}tutor_ma");\n')
    # Tracking: an open way east (and beyond it, further east), a locked door north, two hidden ways (west never found, south found by its detail).
    f[T + "trk"] = room("RkTrk", "Track Hub", {"east": T + "trk_e", "north": T + "trk_n", "out": T + "hall"},
                        f'        LockExit("north", "rkkey", "The door is locked.");\n'
                        f'        HiddenExit("west", "{T}trk_w", "", 0);\n'
                        f'        HiddenExit("south", "{T}trk_s", "flagstone", 0);\n'
                        f'        AddDetail("flagstone", "One flagstone is loose.");\n')
    for d, cname, back, art in (("e", "Owl Prey", "west", "an"), ("n", "Door Goblin", "south", "a"), ("w", "Wall Bat", "east", "a"), ("s", "Floor Beetle", "north", "a")):
        f[T + f"trk_{d}_mob"] = creature(f"RkTrk{d.upper()}Mob", cname, cname.split()[1].lower(), short=art + " " + cname.lower())
        exits = {back: T + "trk"}
        if d == "e":
            exits["east"] = T + "trk_ee"
        f[T + f"trk_{d}"] = room(f"RkTrk{d.upper()}", f"Track {d.upper()}", exits, f'        AddSpawn("{T}trk_{d}_mob");\n')
    f[T + "trk_ee_mob"] = creature("RkTrkEeMob", "Far Owl", "owl", short="a far owl")
    f[T + "trk_ee"] = room("RkTrkEe", "Track EE", {"west": T + "trk_e"}, f'        AddSpawn("{T}trk_ee_mob");\n')
    f[T + "arena"] = room("RkArena", "Rank Arena", {"out": T + "hall"})
    f[T + "field"] = room("RkField", "Rank Field", {"out": T + "hall"}, '        AddNode("/domains/crafttest/obj/herb_patch");\n')
    exits = {"up": "/domains/hollow/village/square", "arena": T + "arena", "trk": T + "trk", "pair": T + "pair", "field": T + "field"}
    for k in kinds:
        exits["r" + k] = T + f"room_{k}"
    f[T + "hall"] = room("RkHall", "Rank Hall", exits, "", HALL_INIT)
    return f


# ---------------------------------------------------------------- helpers

def login(name, calling):
    c = h.Client(name, archetype=calling)
    c.read_until("> ")
    a.drain(c, 0.4)
    c.command("down")
    return c


def state(c, who=""):
    t = c.command("rkstate " + who)
    m = re.search(r"STATE ranks\[(.*?)\] unlocks\[(.*?)\] kills\[(.*?)\] with\[(.*?)\] gold (\d+)", t)
    if not m:
        return {"ranks": "?", "unlocks": "?", "kills": "?", "with": "?", "gold": -1, "text": t}
    return {"ranks": m.group(1), "unlocks": m.group(2), "kills": m.group(3), "with": m.group(4), "gold": int(m.group(5)), "text": t}


def to(c, where):
    c.command("rktele " + (where if where.startswith("/") else T + where))
    a.drain(c, 0.3)


def kill_now(c, target, marker="dies", timeout=30):
    """Attack and wait for the death line."""
    c.send(f"kill {target}")
    return a.fight_until(c, marker, timeout=timeout)


def skill_line(c, title):
    for line in c.command("skills").splitlines():
        if title in line:
            return line
    return ""


def blows_after_windup(text, windup, n):
    """For each wind-up line, the n blow lines that follow it (hits, misses, dodges by the player)."""
    lines = text.splitlines()
    out = []
    for i, line in enumerate(lines):
        if windup in line:
            blows = []
            for later in lines[i + 1:]:
                if re.match(r"^You (?!are|have|gain|feel)\w+", later) or "dodges your attack" in later:
                    blows.append(later)
                    if len(blows) == n:
                        break
            out.append(blows)
    return out


def repeat(c, line, count, gap):
    """Send a line count times, gap seconds apart (each queued for the next Ready Time), and return everything that arrives."""
    data = ""
    for _ in range(count):
        c.send(line)
        data += a.quiet(c, gap)
    return data + a.quiet(c, 2.0)


def use_until(c, line, marker, count, timeout=60, fill=False):
    """Send a skill `count` times, each when the last has resolved (the marker text seen again). fill: Endurance and Focus are filled first
    (a skill at 35 Endurance would otherwise run a new character dry)."""
    data = ""
    for _ in range(count):
        seen = data.count(marker)
        if fill:
            c.command("rkfill")
        c.send(line)
        end = time.time() + timeout / count + 10
        while time.time() < end and data.count(marker) <= seen:
            data += a.quiet(c, 0.3)
        data += a.quiet(c, 1.2)
    return data


# ---------------------------------------------------------------- the tests

def step1_engine(chief):
    # The numbers of every ranked skill at ranks I to V, through the one helper, against the data the simulator holds.
    for skill, t in sim.SKILL_RANKS.items():
        keys = sorted(set(t["base"]) | {k for row in t["rows"].values() for k in row})
        out = chief.command(f"rkvals {skill} {','.join(keys)}")
        got = dict(re.findall(r"(\w+?\d)=([-\d.]+/-?\d+)", out))
        bad = []
        for k in keys:
            for r in range(1, 6):
                v, i = got.get(f"{k}{r}", "?/?").split("/")
                if v == "?" or abs(float(v) - sim.rank_value(skill, k, r)) > 0.001 or int(i) != sim.rank_int(skill, k, r):
                    bad.append(f"{k}{r}: game {v}/{i}, table {sim.rank_value(skill, k, r):.3f}/{sim.rank_int(skill, k, r)}")
        check(f"{skill}: every number at ranks I to V is the rank table's", "ok" if not bad else "; ".join(bad), "ok")
    # Spot checks of the spec's worked case and the default step.
    out = chief.command("rkvals arcane_bolt min,max,focus")
    check("Arcane Bolt: I 6-9 for 20, II 8-12 for 23, III 10-14 for 26, IV by the default step 12-17 for 29", out,
          "min1=6/6", "max1=9/9", "focus1=20/20", "min2=8/8", "max2=12/12", "focus2=23/23", "min3=10/10", "max3=14/14", "focus3=26/26", "min4=11.8/12", "max4=16.7/17", "focus4=29/29")
    out = chief.command("rkvals shield_bash factor,endurance,stagger")
    check("Shield Bash: 75% / 98% / 120%, cost by the default step (15, 17, 20), the stagger never changes", out,
          "factor1=0.75/1", "factor2=0.98/1", "factor3=1.2/1", "endurance1=15/15", "endurance2=17.25/17", "endurance3=19.5/20", "stagger1=2/2", "stagger5=2/2")
    out = chief.command("rkvals precision_strike tohit,crit,endurance")
    check("Precision Strike: +4/15%, +5/20%, +6/25%, +7/30%; cost by the default step", out, "tohit1=4/4", "crit1=15/15", "tohit2=5/5", "crit2=20/20", "tohit3=6/6", "crit3=25/25", "tohit4=7/7", "crit4=30/30", "endurance2=28.75/29")
    out = chief.command("rkvals blessing hit,armor,seconds,focus")
    check("Blessing: +1/+1 .. +4/+4, 60 seconds at every rank, cost by the default step", out, "hit1=1/1", "armor2=2/2", "hit3=3/3", "armor4=4/4", "seconds4=60/60", "focus2=28.75/29")
    out = chief.command("rkvals double_strike hits,factor,endurance")
    check("Double Strike: 2 x 60%, 2 x 75%, 3 x 60%, 4 x 55%; 35 Endurance and the default step from it", out, "hits1=2/2", "factor1=0.6/1", "factor2=0.75/1", "hits3=3/3", "factor3=0.6/1", "hits4=4/4", "factor4=0.55/1", "endurance1=35/35", "endurance2=40.25/40")
    check("Minor Heal on the caster: a third of 20 / 38 / 60 / 66 / 72 rounded half up (7 / 13 / 20 / 22 / 24), before Divine Knowledge",
          chief.command("rkself"), "SELF 20>7,38>13,60>20,66>22,72>24")
    out = chief.command("rkvals tracking range,endurance")
    check("Tracking: adjacent rooms at I, two rooms away at II", out, "range1=1/1", "range2=2/2")
    # Names by rank.
    check("Double Strike's names: Triple Strike at III, Quadruple Strike at IV and after", chief.command("rktitles double_strike"),
          "1:Double Strike|2:Double Strike II|3:Triple Strike|4:Quadruple Strike|5:Quadruple Strike")
    check("Arcane Bolt's names: Arcane Bolt, Arcane Bolt II, III ...", chief.command("rktitles arcane_bolt"), "1:Arcane Bolt|2:Arcane Bolt II|3:Arcane Bolt III|4:Arcane Bolt IV")
    check("Tracking: rank II is the last", chief.command("rktitles tracking"), "2:Tracking II|3:Tracking III(none)")
    check("Magical Lore and Appraise: rank I only", chief.command("rktitles magical_lore") + chief.command("rktitles appraise"), "2:Magical Lore(none)", "2:Appraise(none)")
    check("traits have no ranks", chief.command("rktitles evasion"), "2:Evasion(none)")
    # The level cap rule, with the step one named number.
    check("the level cap: rank r needs level 5 x (r - 1)", chief.command("rkcap"),
          "r1=L1 r2=L5 r3=L10 r4=L15 r5=L20 r6=L25", "L1=r1 L4=r1 L5=r2 L9=r2 L10=r3 L14=r3 L15=r4 L20=r5 L100=r21", "step 5")
    src = open(os.path.join(h.ROOT, "mudlib", "std", "skill.cs")).read()
    check("the step is one named number", "ok" if len(re.findall(r"public const int RankLevelStep = 5;", src)) == 1 else "missing", "ok")
    for name, path in (("no skill computes its own scaling: every ranked skill reads RankInt / RankValue", "arcane_bolt"),):
        body = open(os.path.join(h.ROOT, "mudlib", "skills", path + ".cs")).read()
        check(name, "ok" if "RankInt(" in body and "6 + Random(4)" not in body else "no", "ok")


def step1_skills_list(work):
    adv = login("rkfirst", "adventurer")
    out = adv.command("skills")
    check("skills: a new Adventurer has Double Strike at rank I and is told the next rank's level", out,
          "double", "(Double Strike)", "[35 Endurance", "(rank II at level 5)", "Two quick blows, each at 60% of weapon damage")
    check("skills: a rank I only skill names no next rank", [l for l in out.splitlines() if "(Appraise)" in l][0] if "(Appraise)" in out else out, "(Appraise)", absent=("(rank II",))
    check("skills: Jack of All Trades is among the traits", out, "Jack of All Trades")
    check("help ranks explains ranks, the levels and the trainers", adv.command("help ranks"), "Rank II needs level 5", "ranks", "train", "Hall of Legends")
    check("help names ranks and train", adv.command("help", marker="Colour:"), "Ranks:", "train")
    adv.command("rklevel 5")
    check("at level 5: rank II can be learned from a trainer", skill_line(adv, "Double Strike"), "(rank II can be learned from a trainer)")
    adv.command("quit")
    # An old save: one written before ranks existed has none of the four fields.
    old = login("rkold", "adventurer")
    old.command("rklevel 5")
    old.command("save")
    old.command("quit")
    time.sleep(0.6)
    path = None
    for root_, _, files in os.walk(os.path.join(work, "save")):
        for f in files:
            if f.startswith("rkold"):
                path = os.path.join(root_, f)
    check("the save file is there", str(path is not None), "True")
    saved = json.load(open(path))
    fields = sorted(k for k in saved if k in ("skillRanks", "rankUnlocks", "killsByKind", "killsWithSkill"))
    check("a new save holds the four rank fields", str(fields), "['killsByKind', 'killsWithSkill', 'rankUnlocks', 'skillRanks']")
    for k in fields:
        del saved[k]
    json.dump(saved, open(path, "w"))
    old = login("rkold", "adventurer")
    out = old.command("skills")
    check("an old save loads with rank I in everything", out, "(Double Strike)", "[35 Endurance", absent=("Double Strike II",))
    check("an old save has no unlocks and no counts", state(old)["text"], "ranks[] unlocks[] kills[] with[]")
    to(old, "/domains/hollow/village/practice_yard")
    old.command("rkgold 100")
    out = old.command("train double")
    check("an old save can buy rank II", out, "You have learned", "Double Strike II")
    old.command("quit")
    time.sleep(0.6)
    saved = json.load(open(path))
    check("and the rank is saved in the new field", str(saved.get("skillRanks")), "{'double_strike': 2}")


def step2_unlocks(chief):
    """Each requirement kind: unmet with progress, met, recorded for good."""
    def fresh(name):
        c = login(name, "adventurer")
        c.command("rklevel 5")
        c.command("rkgold 100")
        return c

    # quest
    c = fresh("rkquest")
    to(c, "room_q")
    out = c.command("ranks")
    check("quest: ranks lists the rank, the price and the requirement as unmet", out, "Tutor Q teaches Adventurers:", "Double Strike II", "10 gold", "locked",
          "[x] Level 5: done", "[ ] Quest \"Rk errand\": not yet done", "Tutor Q says: \"Finish my errand first.\"")
    gold = state(c)["gold"]
    out = c.command("train double")
    check("quest: train refuses in words and takes no gold", out, "Not yet.", "Quest \"Rk errand\": not yet done", "No gold has been taken.")
    check("quest: the gold is untouched", str(state(c)["gold"]), str(gold))
    c.command("rkquest rk-errand")
    out = c.command("ranks")
    check("quest: met once the quest is done", out, "[x] Quest \"Rk errand\": done", "you can learn it now")
    gold = state(c)["gold"]
    out = c.command("train double")
    check("quest: train buys the rank", out, "You pay Tutor Q 10 gold.", "You have learned", "Double Strike II", "It answers to double")
    st = state(c)
    check("quest: the gold is paid and the rank and the unlock recorded", f"{gold - st['gold']} {st['ranks']} {st['unlocks']}", "10 double_strike=2 /domains/ranktest/tutor_q|double_strike|2")
    check("quest: the next ranks says you know it", c.command("ranks"), "you know it")
    c.command("quit")

    # kills by kind: every participant counts; progress text
    c = fresh("rkkills")
    to(c, "room_k")
    check("kills: progress 0 of 2", c.command("ranks"), "[ ] Goblins slain: 0 of 2", "Tutor K says: \"Two goblins, if you please.\"")
    to(c, "arena")
    c.command("rkspawn gob")
    kill_now(c, "gob")
    c.command("rkspawn rat")
    kill_now(c, "rat")
    st = state(c)
    check("kills: a goblin (Blood goblin) and a rat (Skin rat) are counted by their beast-table kind", st["kills"], "goblin=1", "rat=1")
    to(c, "room_k")
    check("kills: progress 1 of 2", c.command("ranks"), "[ ] Goblins slain: 1 of 2")
    to(c, "arena")
    c.command("rkspawn gob")
    kill_now(c, "gob")
    to(c, "room_k")
    out = c.command("ranks")
    check("kills: progress 2 of 2, met, and recorded at once", out, "[x] Goblins slain: 2 of 2", "you can learn it now")
    check("kills: the unlock is recorded by ranks", state(c)["unlocks"], "/domains/ranktest/tutor_k|double_strike|2")
    c.command("quit")
    c = login("rkkills", "adventurer")
    check("kills: the counters are saved with the character", state(c)["kills"], "goblin=2", "rat=1")
    to(c, "room_k")
    check("kills: train buys it", c.command("train double"), "You have learned", "Double Strike II")
    c.command("quit")

    # kills_with: only the killing blow; every participant counts the kill
    c = fresh("rkwith")
    m = login("rkhelper", "mage")
    to(c, "arena")
    to(m, "arena")
    for _ in range(4):   # the double's blows can both miss, and then a plain blow kills: try again
        c.command("rkspawn gob12")
        c.command("rkhit gob rkhelper 11")   # the Mage has a share of the damage; 1 Vitality is left
        c.send("double gob")
        a.fight_until(c, "dies", timeout=30)
        a.drain(m, 0.5)
        if "double_strike=1" in state(c)["with"]:
            break
    sc, sm = state(c), state(m)
    check("kills_with: the killing blow with Double Strike is counted for the one who struck it", sc["with"], "double_strike=1")
    check("kills_with: the other who shared the kill counts the kill but not the blow", f"with[{sm['with']}]", "with[]")
    check("kills: both who shared it count it", f"{sc['kills']} {sm['kills']}", "goblin=", absent=("goblin=0",))
    check("kills: the two counts are the same", sc["kills"], sm["kills"])
    # and a kill by plain attack is not a killing blow with a skill
    c.command("rkspawn gob")
    kill_now(c, "gob")
    check("kills_with: a plain attack's kill is no skill's", state(c)["with"], "double_strike=1", absent=("double_strike=2",))
    m.command("rkspawn gob12")
    m.command("rkhit gob rkhelper 11")
    m.send("bolt gob")
    a.fight_until(m, "dies", timeout=30)
    check("kills_with: a Mage's bolt kill is counted as arcane_bolt", state(m)["with"], "arcane_bolt=1")
    to(c, "room_w")
    check("kills_with: progress and met", c.command("ranks"), "[x] Killing blows with Double Strike: 1 of 1")
    check("kills_with: train buys it", c.command("train double"), "You have learned")
    c.command("quit")
    m.command("quit")

    # renown
    c = fresh("rkrenown")
    to(c, "room_r")
    check("renown: progress 0 of 1", c.command("ranks"), "[ ] Renown: 0 of 1", "Let people hear of you first.")
    check("renown: refused", c.command("train double"), "Not yet.", "Renown: 0 of 1")
    out = c.command("rkrenown rk-renown-errand")
    check("renown: the probe's quest gives 1 Renown", out, "RENOWN 1")
    check("renown: met", c.command("ranks"), "[x] Renown: 1 of 1")
    check("renown: train buys it", c.command("train double"), "You have learned")
    c.command("quit")

    # standing: accepted in data, never met, and the trainer says why
    c = fresh("rkstand")
    to(c, "room_s")
    out = c.command("ranks")
    check("standing: listed, unmet, and the trainer says nobody keeps that account yet", out,
          "[ ] Standing with the Assembly: 5 (nobody keeps that account yet)", "There is no one to vouch for that yet", "locked")
    check("standing: train refuses", c.command("train double"), "Not yet.", "nobody keeps that account yet", "No gold has been taken.")
    check("standing: no rank", state(c)["ranks"], "", absent=("double_strike",))
    c.command("quit")

    # an unknown kind fails closed
    c = fresh("rknonse")
    to(c, "room_z")
    check("an unknown requirement kind is never met", c.command("ranks") + c.command("train double"), "A requirement the game does not know", "Not yet.")
    c.command("quit")

    # item shown (not taken), and permanence: recorded at the first sight, still met when the token is gone
    c = fresh("rkitem")
    to(c, "room_i")
    check("item: unmet without it", c.command("ranks"), "[ ] To show: a brass token (you do not have it)", "Show me the token.")
    c.command("rkgive token")
    out = c.command("ranks")
    check("item: met when carried, and recorded", out, "[x] To show: a brass token (you have it)", "you can learn it now")
    check("item: the unlock is recorded", state(c)["unlocks"], "tutor_i|double_strike|2")
    c.command("rkdrop token")
    out = c.command("ranks")
    check("permanence: with the token gone the requirement stays met", out, "[x] To show: a brass token (you do not have it) (met before: it stays met)")
    check("permanence: train buys it without the token", c.command("train double"), "You have learned")
    c.command("quit")

    # item taken: needed at the purchase, and it is taken
    c = fresh("rktake")
    to(c, "room_t")
    check("item take: unmet without it", c.command("train double"), "Not yet.", "To hand over: a brass token (you do not have it)")
    c.command("rkgive token")
    out = c.command("train double")
    check("item take: with it, the rank is bought", out, "You have learned")
    check("item take: and the token is gone", c.command("inventory"), absent=("brass token",))
    c.command("quit")

    # a wizard's kill: the existing kill credit does not exclude wizards (they earn experience), so neither do the counters.
    to(chief, "arena")
    chief.command("rkspawn gob")
    chief.send("kill gob")
    a.fight_until(chief, "dies", timeout=30)
    check("a wizard's kill counts, as the kill credit does (wizards are not excluded from it)", state(chief)["kills"], "goblin=1")


def step3_trainers(chief):
    # Arcane Bolt II at the Hedge-Scholar's Loft: level 5, 40 gold, ten goblins.
    m = login("rkmage", "mage")
    to(m, "/domains/hollow/village/hedge_scholars_loft")
    out = m.command("ranks")
    check("Halbert Quill: ranks lists Arcane Bolt II with its price and three requirements", out, "Halbert Quill teaches Mages:", "Arcane Bolt II", "40 gold",
          "[ ] Level 5 (you are level 1)", "[ ] Goblins slain: 0 of 10", "Ten goblins, and then we will talk", "The third rank and beyond are taught in Oakenfell")
    check("Magical Lore has no ranks, so it is not offered", out, absent=("Magical Lore",))
    check("level refused in words, no gold taken", m.command("train bolt"), "Arcane Bolt II needs level 5. You are level 1.", "No gold has been taken.")
    m.command("rklevel 5")
    out = m.command("train bolt")
    check("goblins refused in words", out, "Not yet.", "Goblins slain: 0 of 10", "No gold has been taken.")
    to(m, "arena")
    for _ in range(10):
        m.command("rkspawn gob")
        kill_now(m, "gob")
    check("ten goblins counted", state(m)["kills"], "goblin=10")
    to(m, "/domains/hollow/village/hedge_scholars_loft")
    gold = state(m)["gold"]
    out = m.command("train bolt")
    check("gold refused in words (a Mage starts with 15)", out, "Arcane Bolt II costs 40 gold, and you have", "No gold has been taken.")
    check("but the unlock is recorded for good", state(m)["unlocks"], "/domains/hollow/npc/halbert_quill|arcane_bolt|2")
    m.command("rkgold 40")
    out = m.command("train bolt")
    check("Arcane Bolt II bought", out, "You pay Halbert Quill 40 gold.", "You have learned", "Arcane Bolt II", "It answers to bolt")
    check("the gold is paid", str(state(m)["gold"]), str(gold))
    out = m.command("skills")
    check("skills shows the rank, its numbers and cost, and the next rank's level", out, "(Arcane Bolt II)", "8-12 damage", "[23 Focus", "(rank III at level 10)")
    check("Halbert teaches no further", m.command("train bolt"), "Halbert Quill teaches Arcane Bolt no further than rank II.", "Oakenfell")
    # Wrong calling, in words.
    to(m, "/domains/hollow/village/practice_yard")
    check("a trainer of another calling refuses in words", m.command("ranks") + m.command("train bolt"),
          "Brann Odell teaches Warriors and Adventurers, and you are a Mage.")
    m.command("quit")

    # Two trainers in one room: each answers only its own calling.
    w = login("rkwarr", "warrior")
    w.command("rklevel 5")
    w.command("rkgold 200")
    to(w, "pair")
    out = w.command("ranks")
    check("two trainers in a room: the warrior is answered by the warrior's", out, "Tutor Wa teaches Warriors:", "Shield Bash II", "Shield Bash III", absent=("Tutor Ma",))
    check("a rank never hidden: III is listed, locked at level 10", out, "Shield Bash III", "[ ] Level 10 (you are level 5)")
    check("ranks cannot be skipped", w.command("rklevel 10") + w.command("train bash") + w.command("skills"), "Shield Bash II")
    out = w.command("train bash")
    check("then III", out, "Shield Bash III")
    w.command("quit")

    # The village people and the hallmasters, each for its calling.
    for calling, where, trainer, skill, word in (("priest", "shrine", "Sister Merewyn", "Minor Heal II", "heal"),
                                                 ("scout", "strung_yew", "Wenna Farrow", "Precision Strike II", "strike"),
                                                 ("warrior", "practice_yard", "Brann Odell", "Shield Bash II", "bash"),
                                                 ("adventurer", "practice_yard", "Brann Odell", "Double Strike II", "double")):
        c = login("rkv" + calling[:4], calling)
        c.command("rklevel 5")
        c.command("rkgold 40")
        to(c, "/domains/hollow/village/" + where)
        out = c.command("ranks")
        check(f"{trainer} teaches {calling}s rank II on level and gold", out, f"{trainer} teaches", skill, "40 gold", "you can learn it now")
        out = c.command(f"train {word}")
        check(f"{trainer}: {skill} bought for 40 gold", out, f"You pay {trainer} 40 gold.", skill)
        check(f"{trainer}: rank III is not taught here", c.command(f"train {word}"), "no further than rank II", "Oakenfell")
        c.command("quit")
    hall = "/domains/greenvale/oakenfell/guild/hall_of_callings"
    for calling, master, skill, word in (("mage", "Ilsabet", "Arcane Bolt III", "bolt"), ("priest", "Anselm", "Blessing III", "bless"),
                                         ("scout", "Veyla", "Precision Strike III", "strike"), ("warrior", "Gorrim", "Shield Bash III", "bash"),
                                         ("adventurer", "Jory", "Triple Strike", "double")):
        c = login("rkh" + calling[:4], calling)
        c.command("rklevel 10")
        c.command("rkgold 1000")
        to(c, hall)
        out = c.command("ranks")
        check(f"the hall: {master} answers the {calling} with the third rank and beyond", out, f"{master} teaches", skill, "150 gold",
              "400 gold", "learn ")
        out = c.command(f"train {word}")
        check(f"the hall: rank II is taught in the Hollow, not here", out, "does not teach", "The second rank is taught in Vincent's Hollow.")
        c.command("quit")


def step4_skills(chief):
    # Arcane Bolt: rank I 6-9 and rank II 8-12, scaled by Spell Power (Insight 9, Spirit 7: +10%).
    m = login("rkbolt", "mage")
    to(m, "arena")
    m.command("rkspawn dummy")
    m.command("verbose on")
    lo1 = sorted({max(1, sim.rnd(d * 1.10)) for d in range(6, 10)})
    text = use_until(m, "bolt target", "Your bolt of arcane force strikes", 4, timeout=60)
    dmg = [int(x) for x in re.findall(r"strikes .*? for (\d+) damage", text)]
    check(f"Arcane Bolt I: every bolt in {lo1}", "ok" if dmg and all(d in lo1 for d in dmg) else str(dmg), "ok")
    m.command("rkslay")
    m.command("rklevel 5")
    m.command("rkgold 40")
    m.command("rkspawn gob 10")
    for _ in range(10):
        kill_now(m, "gob")
    to(m, "/domains/hollow/village/hedge_scholars_loft")
    m.command("train bolt")
    to(m, "arena")
    m.command("rkspawn dummy")
    m.command("restore")
    lo2 = sorted({max(1, sim.rnd(d * 1.10)) for d in range(8, 13)})
    text = use_until(m, "bolt target", "Your bolt of arcane force strikes", 6, timeout=80)
    dmg = [int(x) for x in re.findall(r"strikes .*? for (\d+) damage", text)]
    check(f"Arcane Bolt II: every bolt in {lo2}", "ok" if dmg and all(d in lo2 for d in dmg) else str(dmg), "ok")
    m.command("rkslay")
    m.command("quit")

    # Minor Heal: rank I and II amounts (Spirit 9, Insight 6: Healing Power +9%, Divine Knowledge +25%).
    p = login("rkheal", "priest")
    p.command("rklevel 5")  # a level 1 Priest has 26 Vitality, too few to show a heal of 27; rank II is not bought until below
    for rank in (1, 2):
        if rank == 2:
            a.drain(p, 16)  # Minor Heal's wait is 15 seconds
            p.command("rklevel 5")
            p.command("rkgold 40")
            to(p, "/domains/hollow/village/shrine")
            p.command("train heal")
            to(p, "arena")
        want = max(1, sim.rnd(((2 * sim.rank_int("minor_heal", "heal", rank) + 3) // 6) * 1.34))  # on the caster: a third, rounded half up, then Divine Knowledge
        p.command("rkhurt 40" if rank == 1 else "rkhurt 53")
        p.send("heal")
        text = a.fight_until(p, "restoring", timeout=20) + a.quiet(p, 0.4)
        got = re.search(r"restoring (\d+) Vitality", text)
        check(f"Minor Heal {ROMAN[rank]}: restores {want} (a third of the table's {sim.rank_int('minor_heal', 'heal', rank)}, x 1.34)", got.group(1) if got else text[-200:], str(want))
        check(f"Minor Heal {ROMAN[rank]}: costs {sim.rank_int('minor_heal', 'focus', rank)} Focus", skill_line(p, "Minor Heal"), f"[{sim.rank_int('minor_heal', 'focus', rank)} Focus")
        # Blessing at the same rank (rank I here, the heal was bought at II; Blessing II is bought below).
    p.command("restore")
    to(p, "/domains/hollow/village/shrine")
    p.command("rkgold 40")
    check("Blessing II bought", p.command("train bless"), "Blessing II")
    to(p, "arena")
    p.send("bless")
    a.fight_until(p, "blessing settles", timeout=15)
    check("Blessing II: +2 to hit and +2 armor", p.command("rkmod"), "MOD tohit 2 armor 2")
    check("Blessing II: the skills line", skill_line(p, "Blessing"), "+2 to hit and +2 armor for 60 seconds", "[29 Focus")
    p.command("quit")

    # Shield Bash: the fraction of weapon damage (Even Sword 10, Might 8: 11; 75% = 8, 98% = 11; a critical x1.5).
    w = login("rkbash", "warrior")
    to(w, "arena")
    w.command("rkgive sword")
    w.command("wield evensword")
    w.command("verbose on")
    w.command("rkspawn dummy")
    for rank in (1, 2):
        if rank == 2:
            w.command("rkslay")
            w.command("rklevel 5")
            w.command("rkgold 40")
            to(w, "/domains/hollow/village/practice_yard")
            w.command("train bash")
            to(w, "arena")
            w.command("rkspawn dummy")
            w.command("restore")
        base = sim.rnd(10 * 1.06)
        normal = max(1, sim.rnd(base * sim.rank_value("shield_bash", "factor", rank)))
        allowed = {normal, max(1, sim.rnd(normal * 1.5))}
        text = repeat(w, "bash target", 5, 10.5)
        # The bash's own line is the hit just before the stagger: read the damage notes of the lines that came with a stagger.
        lines = text.splitlines()
        got = []
        for i, line in enumerate(lines):
            if "is staggered" in line or "is thrown off balance" in line:
                for back in range(i - 1, max(-1, i - 4), -1):
                    mm = re.search(r"\(\+(\d+) damage\)", lines[back])
                    if mm:
                        got.append(int(mm.group(1)))
                        break
        check(f"Shield Bash {ROMAN[rank]}: {int(sim.rank_value('shield_bash', 'factor', rank) * 100)}% of the weapon's 11 is {normal} ({sorted(allowed)} with a critical)",
              "ok" if got and all(g in allowed for g in got) else f"{got} {text[-300:]}", "ok")
    check("Shield Bash II: the skills line", skill_line(w, "Shield Bash"), "Shield Bash II", "98% of weapon damage", "[17 Endurance")
    w.command("rkslay")
    w.command("quit")

    # Precision Strike: the skills line at I and II.
    s = login("rkstrike", "scout")
    check("Precision Strike I: +4 to hit, 15%", skill_line(s, "Precision Strike"), "+4 to hit with a 15% chance", "[25 Endurance")
    s.command("rklevel 5")
    s.command("rkgold 40")
    to(s, "/domains/hollow/village/strung_yew")
    s.command("train strike")
    check("Precision Strike II: +5 to hit, 20%, cost by the default step", skill_line(s, "Precision Strike"), "Precision Strike II", "+5 to hit with a 20% chance", "[29 Endurance")
    s.command("quit")


def track(c):
    """Use Tracking and wait for its reading (it is an action on the clock)."""
    c.send("track")
    return a.fight_until(c, "You read the signs", timeout=20) + a.quiet(c, 0.6)


def step4_tracking(chief):
    s = login("rktrack", "scout")
    to(s, "trk")
    out = track(s)
    check("Tracking I: an open exit is read", out, "east", "an owl prey")
    check("Tracking I: not through the locked door", out, absent=("door goblin", "north"))
    check("Tracking I: not through an unfound hidden way", out, absent=("wall bat", "floor beetle", "west", "south"))
    check("Tracking I: no sense of anything close", out, absent=("Something is close by",))
    check("Tracking I: adjacent rooms only", out, absent=("far owl",))
    out = s.command("look at flagstone")
    check("the scout finds the hidden way south by its detail", out, "You find a hidden way")
    out = track(s)
    check("Tracking I: a found hidden way is an ordinary exit", out, "south", "a floor beetle", absent=("wall bat",))
    # Rank II.
    s.command("rklevel 5")
    s.command("rkgold 40")
    to(s, "/domains/hollow/village/strung_yew")
    check("Tracking II bought from Wenna", s.command("train track"), "Tracking II")
    check("Tracking II is the last: no rank III anywhere", s.command("train track"), "the highest rank there is")
    to(s, "trk")
    out = track(s)
    check("Tracking II: two rooms away along open ways", out, "an owl prey.", "Further on: a far owl (east)")
    check("Tracking II: one line for creatures behind a closed door or an unfound hidden way", out, "Something is close by, though you cannot tell where.")
    check("Tracking II: no direction, no names, no count for them", out, absent=("door goblin", "wall bat", "north:", "west:"))
    check("Tracking II: the line comes once", str(out.count("Something is close by")), "1")
    check("Tracking never marks a hidden way found", s.command(f"rkfound {T}trk west"), "FOUND False")
    check("and the scout still cannot walk it", s.command("west"), "What?")
    s.command("rkgive key")
    out = track(s)
    check("with the key the door is a way the scout could walk: it is read", out, "north", "a door goblin")
    to(s, "trk_e")
    check("Tracking II: nothing closed, no line", track(s), absent=("Something is close by",))
    s.command("quit")


def step4_double(chief):
    adv = login("rkdouble", "adventurer")
    to(adv, "arena")
    adv.command("rkgive sword")
    adv.command("wield evensword")
    adv.command("verbose on")
    adv.command("rkspawn dodger")
    windup = "shifts their grip for a quick pair of blows"
    text = use_until(adv, "double dodger", windup, 10, timeout=120, fill=True)
    pairs = blows_after_windup(text, windup, 2)
    full = [p for p in pairs if len(p) == 2]
    mixed = [p for p in full if any("dodges" in x for x in p) and any("dodges" not in x for x in p)]
    check("Double Strike: two blows a use", f"{len(full)} of {len(pairs)}", f"{len(pairs)} of {len(pairs)}")
    check("Double Strike: each blow has its own roll (a dodge and a blow in the same use, against a 50% dodger)", "ok" if mixed else f"no mixed pair in {full}", "ok")
    hits = [int(x) for p in full for line in p for x in re.findall(r"\(\+(\d+) damage\)", line)]
    allowed = {6, 9}  # 10 x Might 6 (+2%) = 10; 60% = 6; a critical 9
    check("Double Strike I: each blow 60% of the weapon (6, a critical 9)", "ok" if hits and all(x in allowed for x in hits) else str(hits), "ok")
    adv.command("rkslay")
    # Ranks II, III, IV: buy them, see the names and the number of blows.
    adv.command("rklevel 15")
    adv.command("rkgold 2000")
    to(adv, "/domains/hollow/village/practice_yard")
    check("Double Strike II from Brann", adv.command("train double"), "Double Strike II")
    to(adv, "/domains/greenvale/oakenfell/guild/hall_of_callings")
    out = adv.command("ranks")
    check("Jory lists Triple Strike and Quadruple Strike", out, "Triple Strike", "Quadruple Strike", "150 gold", "400 gold")
    check("Triple Strike from Jory", adv.command("train double"), "You have learned", "Triple Strike", "It answers to double")
    check("skills: Triple Strike, three blows at 60%", skill_line(adv, "double"), "(Triple Strike)", "Three quick blows, each at 60%", "[46 Endurance")
    to(adv, "arena")
    adv.command("rkspawn dodger")
    adv.command("restore")
    text = use_until(adv, "double dodger", windup, 3, timeout=60, fill=True)
    triples = [p for p in blows_after_windup(text, windup, 3) if len(p) == 3]
    check("Triple Strike: three blows a use", "ok" if triples else text[-400:], "ok")
    adv.command("rkslay")
    to(adv, "/domains/greenvale/oakenfell/guild/hall_of_callings")
    check("Quadruple Strike from Jory", adv.command("train double"), "Quadruple Strike")
    to(adv, "arena")
    adv.command("rkspawn dodger")
    adv.command("restore")
    text = use_until(adv, "double dodger", windup, 3, timeout=60, fill=True)
    quads = [p for p in blows_after_windup(text, windup, 4) if len(p) == 4]
    hits = [int(x) for p in quads for line in p for x in re.findall(r"\(\+(\d+) damage\)", line)]
    check("Quadruple Strike: four blows a use", "ok" if quads else text[-400:], "ok")
    check("Quadruple Strike: each 55% (6, a critical 9)", "ok" if hits and all(x in {6, 9} for x in hits) else str(hits), "ok")
    adv.command("rkslay")
    check("the level cap: rank V at level 20, not 15", adv.command("rklevel 15") + skill_line(adv, "double"), "(rank V at level 20)")
    adv.command("quit")


def step4_jack(chief):
    adv = login("rkjack", "adventurer")
    war = login("rkjackw", "warrior")
    to(chief, "field")
    to(adv, "field")
    to(war, "field")
    check("Jack of All Trades: an Adventurer learns 25% faster", chief.command("rkspeed rkjack"), "SPEED 1.25")
    check("and nobody else does", chief.command("rkspeed rkjackw"), "SPEED 1")
    src = open(os.path.join(h.ROOT, "mudlib", "skills", "jack_of_all_trades.cs")).read()
    check("the 25% is one named number", "ok" if "public const double LearnBonusPercent = 25;" in src else "missing", "ok")
    # The pure growth step: a roll between the rung's chance and 1.25 x it gains a point only at speed 1.25.
    chance = float(re.search(r"CHANCE ([\d.]+)", chief.command("rkchance 0 0")).group(1))
    roll = chance * 1.1
    check("the growth step at speed 1.25 gains where 1.0 does not", chief.command(f"rkgrow 0 5 0 {roll} 1.25") + chief.command(f"rkgrow 0 5 0 {roll} 1"), "GROW 1,0", "GROW 0,1")
    # Through the game with fixed dice: the growth roll fixed between the two chances, one harvest each.
    check("fixed dice set", chief.command(f"rkrolls growth:{roll:.1f}"), "ROLLS True")
    for c in (adv, war):
        c.command(f"rkgive {CT}harvest_sickle")
        c.send("harvest patch")
        c.read_any(("You harvest", "fumble", "need", "nothing"), timeout=20)
        a.quiet(c, 1.0)
    pa, pw = chief.command("rkprof rkjack"), chief.command("rkprof rkjackw")
    check("with the growth roll fixed between the chances, the Adventurer's harvest gains a point", pa, "=10")
    check("and the Warrior's does not", pw, absent=("=10",))
    chief.command("rkrolls random")
    adv.command("quit")
    war.command("quit")


def step5_sim():
    out = subprocess.run([sys.executable, os.path.join(h.ROOT, "tests", "balance_sim.py"), "check"], capture_output=True, text=True, timeout=300).stdout
    check("the simulator's numbers (rank tables included) match the mudlib", out, "simulation numbers match the mudlib")
    out = subprocess.run([sys.executable, os.path.join(h.ROOT, "tests", "balance_sim.py"), "ranks", "100", "bolt"], capture_output=True, text=True, timeout=300).stdout
    check("the simulator prints the bolt-against-staff table by rank", out, "RK5.", "Arcane Bolt at each rank", "  II  8-12")


def security(chief, mudlib):
    os.makedirs(os.path.join(mudlib, "wiz", "rkchief"), exist_ok=True)
    h.write(mudlib, "/wiz/rkchief/meddler", MEDDLER)
    vic = login("rkvictim", "adventurer")
    vic.command("rklevel 5")
    to(vic, "arena")
    to(chief, "arena")
    chief.command("rkspawn gob12")
    chief.command("rkspawn gob12")
    before = state(vic)
    check("the meddler is cloned", chief.command("clone ~/meddler"), "meddler")
    out = chief.command("meddle rkvictim")
    check("the meddler ran", out, "MEDDLE ")
    check("no setter on the character answers a wizard's object", out, "RankSetSkill=False", "RankRecordUnlock=False", "RankCountKill=False",
          "RankCountKillWith=False", "RankPay=False", absent=("RankSetSkill=True", "RankCountKill=True", "RankPay=True"))
    check("the rank daemon refuses a wizard's object", out, "Train=refused|", "RecordIfMet=False", "CreditKill=0")
    check("crafting's fixed dice refuse it too", out, "SetTestRolls=False")
    check("a timed effect does not speed learning", out, "LearnSpeedOf=1.25")
    a.drain(vic, 0.5)
    after = state(vic)
    check("the victim's ranks, unlocks and counters are as they were", f"{after['ranks']}|{after['unlocks']}|{after['kills']}|{after['with']}",
          f"{before['ranks']}|{before['unlocks']}|{before['kills']}|{before['with']}")
    check("and the gold", str(after["gold"]), str(before["gold"]))
    check("a goblin killed by a wizard's object, in the victim's name, is no kill of the victim's", after["kills"], absent=("goblin",))
    # The commands are the player's own: a trainer's `train` binds only for the player in the room, and nothing public buys.
    src = open(os.path.join(h.ROOT, "mudlib", "std", "trainer.cs")).read()
    check("the trainer's commands are private", "ok" if "private bool CmdTrain(" in src and "private bool CmdRanks(" in src else "no", "ok")
    vic.command("quit")


def run(work, mudlib):
    chief = h.Client("rkchief", archetype="adventurer")
    chief.read_until("> ")
    check("the secret makes the first wizard", chief.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(chief, 0.4)
    chief.command("down")
    # RANK_STEPS=tracking,security (say) runs only those parts, for work on one of them.
    only = [x for x in os.environ.get("RANK_STEPS", "").split(",") if x]
    for name, step in (("engine", lambda: step1_engine(chief)), ("skills_list", lambda: step1_skills_list(work)), ("unlocks", lambda: step2_unlocks(chief)),
                       ("trainers", lambda: step3_trainers(chief)), ("skills", lambda: step4_skills(chief)), ("tracking", lambda: step4_tracking(chief)),
                       ("double", lambda: step4_double(chief)), ("jack", lambda: step4_jack(chief)), ("security", lambda: security(chief, mudlib)),
                       ("sim", step5_sim)):
        if not only or name in only:
            step()
    return chief


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-rank-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    for gone in ("crafting/launch_materials.cs", "recipes/launch.cs"):
        os.remove(os.path.join(mudlib, gone))
    shutil.copytree(SAMPLES, mudlib, dirs_exist_ok=True)
    os.makedirs(os.path.join(mudlib, "domains", "ranktest"), exist_ok=True)
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
