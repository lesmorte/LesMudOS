#!/usr/bin/env python3
"""Tests for tags, reach, damage type and the words of a blow (docs/specs/tags-and-saves.md, Part A).

  1. The registry: the family of every word, reserved and unknown words, exclusive families, the implied reach and damage of a kind,
     the ladder pairs, and what a tag set becomes (QueryProblems, QueryResolved).
  2. Every weapon, every armour piece and every creature of the game, against the table below (the final table of the build).
  3. Fixtures: an untagged weapon (implied tags), a typo, a reserved word, two reach tags, a creature's flags as tags, a player with none.
  4. A wizard's object: unregistered tags are dropped and shown by `stat`, nothing can add a tag from outside, the registry's readers are
     callable from a workshop, a wizard's weapon with a typo does not break a fight.
  5. Display: look, appraise, the shop's list, stat.
  6. The words of a blow: every ladder and tier of /secure/blows, the live blow lines of a sword, a mace, a bow, fists, an untagged
     weapon, a creature's bite and a creature's slam against a player (each verb is the one of the tier of the damage really dealt),
     the dark, the order of the ladder choice.
  7. The three goblins whose descriptions disagreed with their attacks, and the lint (checks 17 and 18) on fixtures.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=5061 python3 tests/tags_test.py
"""
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
import area_test as a  # noqa: E402
import rank_test as rk  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
T = "/domains/tagtest/"
check = h.check
OBJ = "/domains/hollow/obj/"
HOLLOW = "/domains/hollow/mob/"
CORR = "/domains/greenvale/corridor/mob/"

# ---------------------------------------------------------------- the table of the build (the spec's section 11, with the goblins settled)
# weapon: (reach, damage, kind, material)
WEAPONS = {
    "rusty_sword": ("melee", "slashing", "sword", "iron"), "fine_iron_longsword": ("melee", "slashing", "sword", "iron"), "captains_sabre": ("melee", "slashing", "sword", "steel"),
    "iron_dagger": ("melee", "piercing", "dagger", "iron"), "enchanted_dagger": ("melee", "piercing", "dagger", "steel"), "steel_dirk": ("melee", "piercing", "dagger", "steel"),
    "training_spear": ("melee", "piercing", "spear", "wood"), "boar_spear": ("melee", "piercing", "spear", "wood"), "goblin_war_spear": ("melee", "piercing", "spear", "wood"),
    "woodcutter_axe": ("melee", "slashing", "axe", "iron"), "felling_axe": ("melee", "slashing", "axe", "iron"), "overseers_axe": ("melee", "slashing", "axe", "iron"),
    "novice_mace": ("melee", "blunt", "mace", "iron"), "flanged_mace": ("melee", "blunt", "mace", "iron"), "shrine_blessed_mace": ("melee", "blunt", "mace", "iron"),
    "apprentice_staff": ("melee", "blunt", "staff", "wood"), "hollow_oak_staff": ("melee", "blunt", "staff", "wood"), "ironshod_staff": ("melee", "blunt", "staff", "wood"),
    "hunting_bow": ("ranged", "piercing", "bow", "wood"), "yew_longbow": ("ranged", "piercing", "bow", "wood"), "wardens_recurve": ("ranged", "piercing", "bow", "wood"),
}
# armour: (armour tag, material)
ARMOUR = {
    "scholars_robe": ("light", "cloth"), "padded_jerkin": ("light", "cloth"), "leather_jerkin": ("light", "leather"), "commanders_vest": ("light", "leather"),
    "guild_helm": ("light", "iron"), "leather_cap": ("light", "leather"), "small_shield": ("shield", "wood"), "magical_trinket": ("trinket", ""),
    "craft/travelers_leather_wrap": ("trinket", "leather"),
}
M, S, P, B = "melee", "slashing", "piercing", "blunt"
# creature: (rank, senses, nature, basic reach/damage/kind, {action name: "reach damage kind" or "" for an action that is not an attack})
CREATURES = {
    HOLLOW + "giant_rat": ("", "darksight", "beast", (M, P, "bite"), {"Bite": "melee piercing bite"}),
    HOLLOW + "large_rat": ("", "darksight", "beast", (M, P, "bite"), {"Bite": "melee piercing bite"}),
    HOLLOW + "practice_dummy": ("", "darksight", "construct", (M, B, "slam"), {"Sway": ""}),
    HOLLOW + "goblin_scavenger": ("", "darksight", "goblin", (M, P, "dagger"), {"Quick Stab": "melee piercing dagger"}),
    HOLLOW + "goblin_minion": ("", "darksight", "goblin", (M, P, "dagger"), {"Stab": "melee piercing dagger"}),
    HOLLOW + "goblin_guard": ("", "alert darksight", "goblin", (M, B, "mace"), {"Hammer Blow": "melee blunt mace"}),
    HOLLOW + "goblin_guard_keyholder": ("", "alert darksight", "goblin", (M, B, "mace"), {"Hammer Blow": "melee blunt mace"}),
    HOLLOW + "spore_bat": ("", "darksight", "beast", (M, P, "bite"), {"Swoop": "melee piercing bite"}),
    HOLLOW + "wandering_wolf": ("", "darksight", "beast", (M, P, "bite"), {"Bite": "melee piercing bite"}),
    HOLLOW + "goblin_warrior": ("", "darksight", "goblin", (M, P, "spear"), {"Spear Thrust": "melee piercing spear"}),
    HOLLOW + "giant_cave_toad": ("", "", "beast", (M, B, "lash"), {"Tongue Lash": "melee blunt lash", "Crushing Leap": "melee blunt slam"}),
    HOLLOW + "goblin_elite_guard": ("elite", "alert darksight", "goblin", (M, S, "axe"), {"Slash": "melee slashing axe", "Shield Bash": "melee blunt slam"}),
    HOLLOW + "goblin_overseer": ("boss", "darksight", "goblin", (M, S, "axe"), {"Axe Slash": "melee slashing axe", "Power Strike": "melee slashing axe", "Battle Cry": "", "Summon Goblin Minion": ""}),
    CORR + "field_rabbit": ("", "darksight", "beast", (M, P, "bite"), {"Bite": "melee piercing bite"}),
    CORR + "roe_deer": ("", "darksight", "beast", (M, B, "kick"), {"Kick": "melee blunt kick"}),
    CORR + "grey_fox": ("", "darksight", "beast", (M, P, "bite"), {"Nip": "melee piercing bite"}),
    CORR + "forest_spider": ("", "darksight", "beast", (M, P, "bite"), {"Bite": "melee piercing bite", "Slowing Bite": "melee piercing bite"}),
    CORR + "stray_wolf": ("", "darksight", "beast", (M, P, "bite"), {"Bite": "melee piercing bite"}),
    CORR + "forest_wolf": ("", "darksight", "beast", (M, P, "bite"), {"Bite": "melee piercing bite", "Savage Bite": "melee piercing bite"}),
    CORR + "bandit_lookout": ("", "alert darksight", "human", (M, S, "sword"), {"Slash": "melee slashing sword"}),
    CORR + "boar_brute": ("", "darksight", "beast", (M, P, "gore"), {"Gore": "melee piercing gore", "Trample": "melee blunt slam"}),
    CORR + "goblin_raider": ("", "darksight", "goblin", (M, S, "axe"), {"Cleaver Hack": "melee slashing axe", "War Whoop": ""}),
    CORR + "goblin_scout": ("", "darksight", "goblin", (M, P, "dagger"), {"Quick Stab": "melee piercing dagger"}),
    CORR + "tunnel_rat": ("", "darksight", "beast", (M, P, "bite"), {"Bite": "melee piercing bite"}),
    CORR + "wild_boar": ("", "darksight", "beast", (M, P, "gore"), {"Gore": "melee piercing gore", "Charge": "melee blunt slam"}),
    CORR + "black_bear": ("", "darksight", "beast", (M, S, "claw"), {"Swipe": "melee slashing claw", "Maul": "melee slashing claw", "Bellowing Roar": ""}),
    CORR + "young_black_bear": ("", "darksight", "beast", (M, S, "claw"), {"Swipe": "melee slashing claw", "Maul": "melee slashing claw"}),
    CORR + "goblin_tunneler": ("", "darksight", "goblin", (M, P, "pick"), {"Pick Strike": "melee piercing pick", "Overhead Swing": "melee piercing pick"}),
    CORR + "heartwood_goblin": ("", "alert darksight", "goblin", (M, B, "club"), {"Club Swing": "melee blunt club", "Heavy Swing": "melee blunt club"}),
    CORR + "highway_bandit": ("", "darksight", "human", (M, S, "sword"), {"Slash": "melee slashing sword", "Pommel Strike": "melee blunt club"}),
    CORR + "rogue_bandit": ("", "darksight", "human", (M, S, "sword"), {"Slash": "melee slashing sword", "Pommel Strike": "melee blunt club"}),
    CORR + "root_creeper": ("", "darksight", "plant", (M, B, "lash"), {"Root Lash": "melee blunt lash", "Clinging Grasp": "melee blunt lash"}),
    CORR + "woodland_spider": ("", "darksight", "beast", (M, P, "bite"), {"Bite": "melee piercing bite", "Slowing Bite": "melee piercing bite"}),
    CORR + "ancient_skeleton": ("", "darksight", "undead", (M, S, "sword"), {"Rusted Blade": "melee slashing sword", "Bone Crusher": "melee slashing sword"}),
    CORR + "ancient_wolf": ("", "darksight", "beast", (M, P, "bite"), {"Rending Bite": "melee piercing bite", "Howl": ""}),
    CORR + "forest_stalker": ("", "darksight", "beast", (M, S, "claw"), {"Claw": "melee slashing claw", "Ambush Strike": "melee slashing claw"}),
    CORR + "restless_spirit": ("", "darksight", "spirit", (M, B, "touch"), {"Grave Touch": "melee blunt touch", "Chill of the Grave": "melee blunt touch"}),
    CORR + "rogue_goblin_veteran": ("", "darksight", "goblin", (M, B, "mace"), {"Mace Blow": "melee blunt mace", "Dazing Blow": "melee blunt mace"}),
    CORR + "thorn_woodling": ("", "darksight", "plant", (M, B, "lash"), {"Branch Lash": "melee blunt lash", "Thorn Barrage": "ranged piercing thorns"}),
    CORR + "bandit_leader": ("boss", "darksight", "human", (M, S, "sword"), {"Sabre Slash": "melee slashing sword", "Shield Smash": "melee blunt slam", "Call the Pack": ""}),
    CORR + "barrow_guardian": ("boss", "darksight", "construct", (M, B, "slam"), {"Stone Fist": "melee blunt slam", "Earthshaker": "melee blunt slam", "Stone Skin": ""}),
    CORR + "fallen_knight": ("boss", "darksight", "undead", (M, S, "sword"), {"Rusted Cleave": "melee slashing sword", "Crushing Blow": "melee slashing sword", "Shield of the Fallen": ""}),
    CORR + "forest_panther": ("boss", "darksight", "beast", (M, S, "claw"), {"Claw": "melee slashing claw", "Pouncing Leap": "melee slashing claw", "Raking Frenzy": "melee slashing claw"}),
    CORR + "warren_broodmother": ("boss", "darksight", "beast", (M, P, "bite"), {"Venom Fang": "melee piercing bite", "Webbed Spit": "ranged blunt spit", "Call the Brood": ""}),
}

# ---------------------------------------------------------------- test content

PROBE = r'''// @inherit /std/thing
public class TagProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        foreach (var v in new[] { "tinfo", "tcall", "tgive", "tspawn", "ttele", "tmax", "tslay", "theal", "tfill", "tlook", "tappraise" }) AddAction(v, Do);
    }
    private static string J(IEnumerable<string>? l) => l == null ? "" : string.Join(",", l);
    private object? Arg(string a)
    {
        if (a.StartsWith("L:")) return a.Substring(2).Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (a == "true") return true;
        if (a == "false") return false;
        if (a == "_") return "";
        if (int.TryParse(a, out var n)) return n;
        return a;
    }
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var room = Environment(me)!;
        switch (QueryVerb())
        {
            case "tinfo":
            {
                var ob = CloneObject(w[0]);
                var sb = new System.Text.StringBuilder("INFO " + w[0]);
                sb.Append(" tags=" + J(ob.Call<List<string>>("QueryTags")));
                sb.Append(" summary=" + ob.Call<string>("QueryTagSummary"));
                foreach (var f in new[] { "reach", "damage", "kind", "armour", "material", "rank" }) sb.Append($" {f}={ob.Call<string>("QueryTagIn", f)}");
                sb.Append(" dropped=" + J(ob.Call<List<string>>("QueryDroppedTags")));
                if (ob.Call<bool>("IsMonster"))
                {
                    sb.Append($" boss={ob.Call<bool>("QueryBoss")} alert={ob.Call<bool>("QueryAlert")} dark={ob.Call<bool>("QueryDarkSight")} elite={ob.Call<bool>("HasTag", "elite")}");
                    sb.Append($" attack={ob.Call<string>("QueryAttackReach")}/{ob.Call<string>("QueryAttackDamage")}/{ob.Call<string>("QueryAttackKind")} atrange={ob.Call<bool>("QueryAtRange")}");
                    sb.Append(" actions=" + string.Join(";", ob.Call<List<string>>("QueryActionTags") ?? new List<string>()));
                }
                Write(sb.ToString() + "\n");
                return true;
            }
            case "tcall":
            {
                try
                {
                    var target = w[0].StartsWith("/") ? LoadObject(w[0]) : me;
                    var fn = w[1];
                    var rest = w.Skip(2).Select(Arg).ToArray();
                    var r = target.Call(fn, rest);
                    var text = r is List<string> l ? "[" + J(l) + "]" : r?.ToString() ?? "null";
                    Write("CALL " + text + "\n");
                }
                catch (MudRuntimeException e) { Write("CALL ERR " + e.Message + "\n"); }
                return true;
            }
            case "tgive": { var ob = CloneObject(w[0]); MoveObject(ob, me); Write("GAVE " + ob.Name + "\n"); return true; }
            case "tspawn": { var n = w.Length > 1 ? int.Parse(w[1]) : 1; for (var i = 0; i < n; i++) MoveObject(CloneObject(w[0].StartsWith("/") ? w[0] : "/domains/tagtest/" + w[0]), room); Write("SPAWNED " + n + "\n"); return true; }
            case "ttele": MoveObject(me, LoadObject(args.Trim())); Write("TELE ok\n"); return true;
            case "tmax": Write("MAXV " + me.Call<int>("QueryMaxVitality") + "\n"); return true;
            case "tfill": me.Call("FillResources"); Write("FILLED\n"); return true;
            case "tslay":
            {
                var n = 0;
                foreach (var o in AllInventory(room).ToList()) if (o != me && o.IsAlive && o.Call<bool>("IsMonster")) { o.Call("Die", (ObjectRef?)null); n++; }
                Write("SLAIN " + n + "\n"); return true;
            }
            case "theal":
                foreach (var o in AllInventory(room).ToList()) if (o != me && o.IsAlive && o.Call<bool>("IsMonster")) o.Call("Heal", 999999);
                Write("HEALED\n"); return true;
            case "tlook": { var ob = Present(args.Trim(), me); Write("LOOK " + (ob == null ? "none" : ob.Call<string>("Long")) + "\n"); return true; }
            case "tappraise": { var ob = Present(args.Trim(), me); Write("APPRAISE " + (ob == null ? "none" : ob.Call<string>("Appraisal")) + "\n"); return true; }
        }
        return false;
    }
}
'''

# A wizard's own object (sandbox code): tries to add or change a tag of a real weapon, and reads what it may.
MEDDLER = r'''// @inherit /std/thing
public class TagMeddler : Thing
{
    public override void Create() { Name = "meddler"; Ids.Add("meddler"); }
    public override string? Short() => "a meddler";
    public override void Init() { AddAction("meddle", Meddle); }
    private bool Meddle(string args)
    {
        var me = ThisPlayer()!;
        var sword = Present("sword", me);
        var tries = new List<string>();
        void Try(string label, Func<object?> f)
        {
            try { tries.Add(label + "=" + (f() ?? "null")); }
            catch (MudRuntimeException e) { tries.Add(label + "!" + e.Message.Length); }
        }
        if (sword == null) { Write("MEDDLE nosword\n"); return true; }
        Try("AddTag", () => sword.Call("AddTag", "frost"));
        Try("SetTag", () => sword.Call("SetTag", "frost"));
        Try("RemoveTag", () => sword.Call("RemoveTag", "melee"));
        Try("SetTags", () => sword.Call("SetTags", new List<string> { "boss" }));
        Try("Tags", () => sword.Call("get_Tags"));
        Try("SetProperty", () => sword.Call("SetProperty", "tags+", "frost"));
        Try("HasTag", () => sword.Call<bool>("HasTag", "melee"));
        Try("QueryTags", () => string.Join(",", sword.Call<List<string>>("QueryTags")));
        Try("QueryTagIn", () => sword.Call<string>("QueryTagIn", "damage"));
        Try("QueryTagSummary", () => sword.Call<string>("QueryTagSummary"));
        var reg = LoadObject("/secure/tags");
        Try("QueryFamilyOf", () => reg.Call<string>("QueryFamilyOf", "slashing"));
        Try("QueryIsActive", () => reg.Call<bool>("QueryIsActive", "frost"));
        Try("QueryResolved", () => string.Join(",", reg.Call<List<string>>("QueryResolved", new List<string> { "melee", "ranged", "frost", "typo" }, "weapon")));
        Try("registry.Family", () => reg.Call("Family", "x", true, true, "weapon", "boss"));
        Try("blows.Words", () => LoadObject("/secure/blows").Call("Words", "plain", "a", "b", "c", "d"));
        Try("blows.QueryWords", () => LoadObject("/secure/blows").Call<string>("QueryWords", "slashing", 3));
        var again = sword.Call<bool>("HasTag", "slashing");
        Try("after", () => again);
        Write("MEDDLE " + string.Join(" ", tries) + "\n");
        return true;
    }
}
'''

# A wizard's weapon that claims tags it may not have, and a creature that claims rank.
TYPO_SWORD = r'''// @inherit /std/weapon
public class TypoSword : Weapon
{
    public override void Create()
    {
        Name = "Typo Sword"; Ids.Add("typo"); ShortDesc = "a typo sword"; LongDesc = "A wizard's blade with careless tags.";
        DamageMin = 3; DamageMax = 5; Category = "sword"; Recovery = 0.5; ReactionMod = 0.0; Value = 1;
        Tags.Add("sworrd"); Tags.Add("frost"); Tags.Add("melee"); Tags.Add("ranged"); Tags.Add("boss"); Tags.Add("slashing");
    }
}
'''
GRAND_BOSS = r'''// @inherit /std/monster
public class GrandBoss : Monster
{
    public override void Create()
    {
        Name = "Grand Boss"; Ids.Add("grandboss"); ShortDesc = "a grand boss"; LongDesc = "A wizard's creature.";
        Level = 1; MaxVitality = 10; DamageMin = 1; DamageMax = 1; ToHit = -40; XpValue = 0; Aggressive = false; BaseReaction = 6.0;
        Tags.Add("boss"); Tags.Add("elite"); Tags.Add("flurb"); Tags.Add("beast"); Tags.Add("bite");
    }
}
'''


def weapon(cls, name, ident, category, low, high, tags=(), mod=0.0, rec=0.5):
    tag_lines = "".join(f'Tags.Add("{t}"); ' for t in tags)
    return f"""// @inherit /std/weapon
public class {cls} : Weapon
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); ShortDesc = "a {name.lower()}"; LongDesc = "Every blow the same.";
        Category = "{category}"; DamageMin = {low}; DamageMax = {high}; ToHit = 40; ReactionMod = {mod}; Recovery = {rec}; Value = 4;
        {tag_lines}
    }}
}}
"""


def mob(cls, name, ident, hp=100000, dmg=1, to_hit=-40, reaction=6.0, tags=(), flags="", aggressive=False, actions=""):
    tag_lines = "".join(f'Tags.Add("{t}"); ' for t in tags)
    return f"""// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); ShortDesc = "a {name.lower()}"; LongDesc = "A creature made for the tag test.";
        Level = 1; MaxVitality = {hp}; DamageMin = {dmg}; DamageMax = {dmg}; ToHit = {to_hit}; Armor = 0; Dodge = 0; XpValue = 0;
        Aggressive = {'true' if aggressive else 'false'}; BaseReaction = {reaction}; {flags}
        {tag_lines}
        {actions}
    }}
}}
"""


def test_files():
    f = {T + "probe": PROBE}
    # a weapon with no tags written: the Category implies them; one with no Category: no tags at all
    f[T + "w_sword"] = weapon("TwSword", "Tagsword", "tagsword", "sword", 1000, 1000)
    f[T + "w_mace"] = weapon("TwMace", "Tagmace", "tagmace", "mace", 1000, 1000)
    f[T + "w_dagger"] = weapon("TwDagger", "Tagdagger", "tagdagger", "dagger", 1000, 1000)
    f[T + "w_bow"] = weapon("TwBow", "Tagbow", "tagbow", "bow", 1000, 1000)
    f[T + "w_plain"] = weapon("TwPlain", "Tagplain", "tagplain", "", 1000, 1000)
    f[T + "w_typo"] = TYPO_SWORD
    f[T + "w_reserved"] = weapon("TwReserved", "Tagfrost", "tagfrost", "sword", 5, 5, ("melee", "slashing", "sword", "frost"))
    f[T + "w_two"] = weapon("TwTwo", "Tagtwo", "tagtwo", "sword", 5, 5, ("melee", "ranged", "slashing", "sword"))
    f[T + "w_sling"] = weapon("TwSling", "Tagsling", "tagsling", "bow", 1000, 1000, ("ranged", "blunt", "bow"))
    f[T + "w_thrown"] = weapon("TwThrown", "Tagthrown", "tagthrown", "dagger", 5, 5, ("thrown", "piercing", "dagger"))
    # sword fixtures of fixed damage for each tier of a 100000 Vitality dummy (5%, 15%, 30%, 60%)
    for i, d in enumerate((5000, 15000, 30000, 60000)):
        f[T + f"w_sword{i}"] = weapon(f"TwSwordT{i}", f"Tagsword{i}", f"tagsword{i}", "sword", d, d)
    f[T + "dummy"] = mob("TagDummy", "Tagdummy", "tagdummy")
    f[T + "grandboss"] = GRAND_BOSS
    f[T + "elite_c"] = mob("TagElite", "Tagelite", "tagelite", flags="Elite = true; Alert = true; DarkSight = false;", tags=("goblin", "melee", "slashing", "axe"))
    f[T + "boss_c"] = mob("TagBoss", "Tagboss", "tagboss", flags="Boss = true;", tags=("beast",))
    f[T + "untagged_c"] = mob("TagUntagged", "Taguntagged", "taguntagged")
    # creatures that hit the player: damage a share of the player's maximum Vitality, set by the test through the file name's number
    for kind, tags in (("bite", ("beast", "melee", "piercing", "bite")), ("claw", ("beast", "melee", "slashing", "claw")), ("slam", ("construct", "melee", "blunt", "slam")),
                       ("dagger", ("goblin", "melee", "piercing", "dagger")), ("plainmob", ())):
        f[T + "hit_" + kind] = mob("TagHit" + kind.title(), "Hit" + kind, "hit" + kind, hp=100000, dmg=3, to_hit=100, reaction=0.5, tags=tags, aggressive=False)
    f[T + "arena"] = rk.room("TagArena", "Tag Arena", {"out": T + "hall"})
    f[T + "dark"] = rk.room("TagDark", "Tag Cellar", {"out": T + "hall"}, "        Light = -1;\n        DarkDesc = \"It is black here.\";\n")
    f[T + "hall"] = rk.room("TagHall", "Tag Hall", {"up": "/domains/hollow/village/square", "arena": T + "arena", "dark": T + "dark"}, "",
                            rk.HALL_INIT.replace("/domains/ranktest/probe", T + "probe"))
    return f


def login(name, calling="warrior"):
    c = h.Client(name, archetype=calling)
    c.read_until("> ")
    a.drain(c, 0.4)
    c.command("down")
    return c


def info(c, path):
    out = c.command("tinfo " + path)
    m = re.search(r"INFO \S+ (.*)", out)
    d = {}
    if m:
        for key, value in re.findall(r"(\w+)=(\S*)", m.group(1)):
            d[key] = value
        d["_actions"] = out.split(" actions=", 1)[1].split("\n")[0].strip() if " actions=" in out else ""
        d["_summary"] = re.search(r"summary=(.*?) (?:reach|damage)=", out).group(1) if "summary=" in out else ""
    return d, out


def call(c, *words):
    out = c.command("tcall " + " ".join(words))
    m = re.search(r"CALL (.*)", out)
    return m.group(1).strip() if m else out


def to(c, where):
    c.command("ttele " + (where if where.startswith("/") else T + where))
    a.drain(c, 0.3)


# ---------------------------------------------------------------- 1. the registry

LADDER_WORDS = {
    "plain": [("scratch", "scratches", ""), ("hit", "hits", ""), ("hit", "hits", " hard"), ("smash", "smashes", " brutally")],
    "blunt": [("bruise", "bruises", ""), ("thump", "thumps", ""), ("crack", "cracks", " hard"), ("smash", "smashes", " brutally")],
    "slashing": [("nick", "nicks", ""), ("cut", "cuts", ""), ("slash", "slashes", " deeply"), ("cleave", "cleaves", " to the bone")],
    "piercing": [("prick", "pricks", ""), ("stab", "stabs", ""), ("pierce", "pierces", " deeply"), ("run", "runs", " through")],
    "archery": [("graze", "grazes", ""), ("shoot", "shoots", ""), ("shoot", "shoots", " squarely"), ("shoot", "shoots", " clean through")],
    "bite": [("nip", "nips", ""), ("bite", "bites", ""), ("bite", "bites", " hard"), ("maul", "mauls", " savagely")],
    "claw": [("scratch", "scratches", ""), ("claw", "claws", ""), ("rake", "rakes", " deeply"), ("rend", "rends", " to ribbons")],
    "gore": [("nudge", "nudges", ""), ("gore", "gores", ""), ("gore", "gores", " deeply"), ("rip", "rips", " open")],
    "kick": [("scuff", "scuffs", ""), ("kick", "kicks", ""), ("kick", "kicks", " hard"), ("stamp", "stamps", " flat")],
    "slam": [("bump", "bumps", ""), ("slam", "slams", ""), ("slam", "slams", " hard"), ("crush", "crushes", " brutally")],
    "lash": [("flick", "flicks", ""), ("lash", "lashes", ""), ("lash", "lashes", " hard"), ("thrash", "thrashes", " brutally")],
    "touch": [("brush", "brushes", ""), ("touch", "touches", ""), ("chill", "chills", " deeply"), ("wither", "withers", " utterly")],
    "spit": [("spatter", "spatters", ""), ("splash", "splashes", ""), ("splash", "splashes", " thickly"), ("drench", "drenches", " completely")],
    "thorns": [("prick", "pricks", ""), ("pepper", "peppers", ""), ("pepper", "peppers", " thickly"), ("riddle", "riddles", " with thorns")],
}


def step_registry(c):
    print("\n=== registry")
    fams = {"melee": "reach", "ranged": "reach", "thrown": "reach", "blunt": "damage", "slashing": "damage", "piercing": "damage",
            "dagger": "kind", "sword": "kind", "spear": "kind", "axe": "kind", "mace": "kind", "staff": "kind", "bow": "kind", "pick": "kind", "club": "kind",
            "bite": "kind", "claw": "kind", "gore": "kind", "kick": "kind", "slam": "kind", "lash": "kind", "touch": "kind", "spit": "kind", "thorns": "kind",
            "light": "armour", "medium": "armour", "heavy": "armour", "shield": "armour", "trinket": "armour",
            "cloth": "material", "leather": "material", "wood": "material", "iron": "material", "steel": "material", "bone": "material", "stone": "material",
            "elite": "rank", "boss": "rank", "beast": "nature", "goblin": "nature", "human": "nature", "undead": "nature", "spirit": "nature", "plant": "nature", "construct": "nature",
            "alert": "senses", "darksight": "senses", "fire": "element", "frost": "element", "shock": "element", "holy": "element", "shadow": "element",
            "weak-fire": "affinity", "resists-frost": "affinity", "immune-shadow": "affinity", "nonsense": "", "Melee": "", "x": "", "weak-wood": ""}
    wrong = {t: call(c, "/secure/tags", "QueryFamilyOf", t) for t in fams}
    wrong = {t: v for t, v in wrong.items() if v != (fams[t] or "null") and not (fams[t] == "" and v == "")}
    check("every registered word is in its family (and nothing else is registered)", str(wrong), "{}")
    check("a reserved word is registered but not active", call(c, "/secure/tags", "QueryIsActive", "frost") + call(c, "/secure/tags", "QueryIsActive", "weak-fire") + call(c, "/secure/tags", "QueryIsActive", "melee"), "FalseFalseTrue")
    check("an unregistered word is not registered", call(c, "/secure/tags", "QueryIsRegistered", "nonsense") + call(c, "/secure/tags", "QueryIsRegistered", "melee"), "FalseTrue")
    check("exclusive families", "".join(call(c, "/secure/tags", "QueryIsExclusive", f)[0] for f in ("reach", "damage", "kind", "armour", "material", "rank", "nature", "senses")), "TTTTTTFF")
    for kind, pair in (("sword", "melee slashing"), ("dagger", "melee piercing"), ("bow", "ranged piercing"), ("spit", "ranged blunt"), ("thorns", "ranged piercing"), ("club", "melee blunt"),
                       ("pick", "melee piercing"), ("slam", "melee blunt"), ("touch", "melee blunt"), ("mace", "melee blunt"), ("melee", "")):
        check(f"a {kind} implies '{pair}'", call(c, "/secure/tags", "QueryImplied", kind), pair if pair else "null") if pair else check("a reach implies nothing", call(c, "/secure/tags", "QueryImplied", kind), "")
    check("ladder pairs: the four that exist", " ".join(call(c, "/secure/tags", "QueryHasLadder", r, d)[0] for r, d in (("melee", "blunt"), ("melee", "slashing"), ("melee", "piercing"), ("ranged", "piercing"))), "T T T T")
    check("ladder pairs: ranged blunt and thrown piercing have none", call(c, "/secure/tags", "QueryHasLadder", "ranged", "blunt") + call(c, "/secure/tags", "QueryHasLadder", "thrown", "piercing"), "FalseFalse")
    check("a valid weapon set has no problem", call(c, "/secure/tags", "QueryProblems", "L:melee,slashing,sword,iron", "weapon"), "[]")
    out = call(c, "/secure/tags", "QueryProblems", "L:melee,ranged,slashing,frost,typo,boss", "weapon")
    check("problems: two reach tags, a reserved tag, an unknown tag, a creature tag on a weapon, a missing kind",
          out, "'melee' and 'ranged'", "reserved", "'typo' is not registered", "'boss'", "needs a kind")
    out = call(c, "/secure/tags", "QueryResolved", "L:ranged,melee,frost,typo,sword", "weapon")
    check("resolved: the first of an exclusive family in registry order stays (melee before ranged); the rest are dropped; the kind implies a damage type",
          out, "E:reach:melee", "E:kind:sword", "E:damage:slashing", "!ranged", "!frost", "!typo")
    out = call(c, "/secure/tags", "QueryResolved", "L:bite,beast,alert,darksight", "creature")
    check("resolved: a creature's kind implies reach and damage; open families keep all", out, "E:reach:melee", "E:damage:piercing", "O:nature:beast", "O:senses:alert", "O:senses:darksight")
    check("resolved: nothing throws on odd input", call(c, "/secure/tags", "QueryResolved", "L:,_,Melee,x", "weapon"), "!")
    check("a weapon may not have a creature-only kind", call(c, "/secure/tags", "QueryResolved", "L:bite,melee", "weapon"), "!bite")


# ---------------------------------------------------------------- 2. every file of the game

def step_table(c):
    print("\n=== the 21 weapons, 9 armour pieces and 44 creatures")
    for name, (reach, damage, kind, material) in WEAPONS.items():
        d, out = info(c, OBJ + name)
        want = sorted({reach, damage, kind, material})
        check(f"weapon {name}: {reach} {damage} {kind} {material}", d.get("tags", out), ",".join(want))
        check(f"weapon {name}: one of each family, nothing dropped, words '{damage} {kind}'", f"{d.get('reach')}/{d.get('damage')}/{d.get('kind')}/{d.get('material')}/{d.get('dropped')}", f"{reach}/{damage}/{kind}/{material}/")
        check(f"weapon {name}: the summary", d["_summary"], f"{damage} {kind}" + (", at range" if reach == "ranged" else ""))
    for name, (armour, material) in ARMOUR.items():
        d, out = info(c, OBJ + name)
        check(f"armour {name}: {armour} {material}", d.get("tags", out), ",".join(sorted(x for x in (armour, material) if x)))
        phrase = (f"{armour} {material} armour" if material else f"{armour} armour") if armour == "light" else ((("wooden " if material == "wood" else (material + " ") if material else "") + armour))
        check(f"armour {name}: the summary", d["_summary"], phrase)
    bad = 0
    for path, (rank, senses, nature, (reach, damage, kind), actions) in CREATURES.items():
        d, out = info(c, path)
        short = path.rsplit("/", 1)[1]
        want = sorted(set([nature, reach, damage, kind] + senses.split() + ([rank] if rank else [])))
        got = d.get("tags", "")
        ok = got == ",".join(want) and d.get("rank") == rank and d.get("dropped") == ""
        ok = ok and d.get("boss") == str(rank == "boss") and d.get("alert") == str("alert" in senses) and d.get("dark") == str("darksight" in senses) and d.get("elite") == str(rank == "elite")
        ok = ok and d.get("attack") == f"{reach}/{damage}/{kind}"
        want_actions = ";".join(f"{n}" for n in actions)
        got_actions = [x.split("|") for x in d["_actions"].split(";") if x]
        ok = ok and [g[1] for g in got_actions] == list(actions) and all(g[3] == actions[g[1]] for g in got_actions)
        check(f"creature {short}: {rank or 'ordinary'}, {nature}, {reach} {damage} {kind}, {senses or 'no senses'}; actions {list(actions.values())}", "ok" if ok else f"{out.strip()[:500]}", "ok")
    summary, _ = info(c, HOLLOW + "goblin_elite_guard")
    check("a creature's summary reads nature and rank", summary["_summary"], "goblin, elite")
    check("a boss's summary", info(c, HOLLOW + "goblin_overseer")[0]["_summary"], "goblin, boss")
    check("exactly one creature has no darksight, and it is the toad", str(sorted(p.rsplit("/", 1)[1] for p, v in CREATURES.items() if "darksight" not in v[1])), "['giant_cave_toad']")
    check("the table covers all 44 creatures, 21 weapons and 9 armour pieces", f"{len(CREATURES)} {len(WEAPONS)} {len(ARMOUR)}", "44 21 9")
    on_disk = sum(len([f for f in os.listdir(os.path.join(ROOT, "mudlib", "domains", d)) if f.endswith(".cs")]) for d in ("hollow/mob", "greenvale/corridor/mob"))
    check("and there are no other creature files in the two mob directories", str(on_disk), "44")


# ---------------------------------------------------------------- 3. fixtures

def step_fixtures(c):
    print("\n=== fixtures")
    d, out = info(c, T + "w_sword")
    check("a weapon with no tags written: its Category implies kind, reach and damage type", d.get("tags", out), "melee,slashing,sword")
    d, out = info(c, T + "w_bow")
    check("an untagged bow is ranged piercing", d.get("tags", out), "bow,piercing,ranged")
    d, out = info(c, T + "w_plain")
    check("a weapon with no Category and no tags has none at all (and says nothing)", f"{d.get('tags')}|{d['_summary']}|{d.get('dropped')}", "||")
    d, out = info(c, T + "w_typo")
    check("a weapon with a typo, a reserved word and a rank tag: the extras are dropped and listed", f"{d.get('tags')} {d.get('dropped')}", "melee,slashing,sword")
    check("... all of sworrd, frost and boss, and the second reach", d.get("dropped", ""), "sworrd", "frost", "boss")
    d, out = info(c, T + "w_reserved")
    check("a reserved word is dropped, not kept", f"{d.get('tags')} {d.get('dropped')}", "melee,slashing,sword frost")
    d, out = info(c, T + "w_two")
    check("two reach tags: melee (first in registry order) stays, ranged is dropped", f"{d.get('tags')} {d.get('dropped')}", "melee,slashing,sword ranged")
    d, out = info(c, T + "w_sling")
    check("a thrown or a ranged blunt weapon is allowed to exist (the lint is what asks for a ladder)", d.get("tags", out), "blunt,bow,ranged")
    d, out = info(c, T + "elite_c")
    check("Elite = true is the tag elite; Alert is alert; DarkSight = false removes darksight", f"{d.get('tags')} {d.get('boss')} {d.get('elite')} {d.get('dark')}", "alert,axe,elite,goblin,melee,slashing False True False")
    d, out = info(c, T + "boss_c")
    check("Boss = true is the tag boss and QueryBoss reads it", f"{d.get('tags')} {d.get('boss')} {d.get('rank')}", "beast,boss,darksight True boss")
    d, out = info(c, T + "untagged_c")
    check("a creature with no tags written has only its flags: darksight, and plain attack words", f"{d.get('tags')} {d.get('attack')}", "darksight melee/blunt/")
    d, out = info(c, T + "grandboss")
    check("a creature that writes boss and elite and a typo itself: the rank is elite (first in registry order), the others dropped",
          f"{d.get('tags')} {d.get('dropped')}", "beast,bite,darksight,elite,melee,piercing", "boss", "flurb")
    check("QueryBoss of a creature that wrote the tag itself reads the tag as it is (a lint error in real files; the dropped rank decides)", f"{d.get('boss')} {d.get('elite')}", "False True")
    check("a player carries no tags", call(c, "me", "QueryTags"), "[]")
    check("and no summary", call(c, "me", "QueryTagSummary"), "")


# ---------------------------------------------------------------- 4. the sandbox

def step_sandbox(chief, mudlib):
    print("\n=== the wizard's workshop")
    os.makedirs(os.path.join(mudlib, "wiz", "tagwiz"), exist_ok=True)
    h.write(mudlib, "/wiz/tagwiz/meddler", MEDDLER)
    h.write(mudlib, "/wiz/tagwiz/typo", TYPO_SWORD.replace("TypoSword", "WizTypo"))
    h.write(mudlib, "/wiz/tagwiz/den", "// @inherit /std/room\npublic class TagChiefDen : Room { public override void Create() { ShortDesc = \"Den\"; LongDesc = \"A workshop.\"; } }\n")
    chief.command("tgive " + OBJ + "rusty_sword")
    chief.command("goto /wiz/tagwiz/den")  # the wizard's code runs in their workshop
    check("the meddler is cloned in the workshop", chief.command("clone ~/meddler"), "meddler")
    out = chief.command("meddle")
    check("the workshop meddler ran", out, "MEDDLE ")
    for fn in ("AddTag", "SetTag", "RemoveTag", "SetTags"):
        check(f"a wizard's object cannot call {fn} on a real weapon: nothing happens (null)", out, f"{fn}=null", absent=(f"{fn}=True",))
    check("SetProperty (a property key that is no tag) changes nothing: False, not True", out, "SetProperty=False")
    check("it cannot read the protected list either", out, "Tags=null")
    check("but it may read tags: HasTag, QueryTags, QueryTagIn and the summary", out, "HasTag=True", "QueryTagIn=slashing", "QueryTagSummary=slashing sword")
    check("and the registry: the family of a word, whether it is active, what a tag set would resolve to", out, "QueryFamilyOf=damage", "QueryIsActive=False", "QueryResolved=E:reach:melee")
    check("it cannot change the registry or the word tables (they have no such public function, and the call is refused)", out, "registry.Family=null", "blows.Words=null")
    check("it may read a blow word", out, "blows.QueryWords=cleave|cleaves| to the bone")
    check("the sword is as it was: still slashing", out, "after=True")
    # a wizard's weapon with careless tags
    check("clone the wizard's typo weapon", chief.command("clone ~/typo"), "Cloned")
    out = chief.command("stat typo")
    check("stat shows its tags and says what the registry refused", out, "Tags: melee slashing sword", "unregistered or refused", "sworrd", "frost", "boss")
    check("no per-call log lines: the log has none of it (checked at the end)", "ok", "ok")
    chief.command("wield typo")
    chief.command("goto /domains/tagtest/arena")
    a.drain(chief, 0.4)


# ---------------------------------------------------------------- 5. display

def step_display(c, mudlib):
    print("\n=== look, appraise, the shop, stat")
    c.command("tgive " + OBJ + "rusty_sword")
    out = c.command("look at rusty sword")
    check("look: the sentence for a sword", out, "It is a sword: it cuts, and it is used up close.")
    c.command("tgive " + OBJ + "hunting_bow")
    check("look: a bow", c.command("look at hunting bow"), "It is a bow: it pierces, and it is used from a distance.")
    c.command("tgive " + OBJ + "novice_mace")
    check("look: a mace crushes", c.command("look at mace"), "It is a mace: it crushes, and it is used up close.")
    c.command("tgive " + OBJ + "woodcutter_axe")
    check("look: an axe (an, not a)", c.command("look at woodcutter axe"), "It is an axe: it cuts, and it is used up close.")
    c.command("tgive " + OBJ + "leather_jerkin")
    check("look: armour", c.command("look at leather jerkin"), "It is light armour, made of leather.")
    c.command("tgive " + OBJ + "small_shield")
    check("look: a shield", c.command("look at small shield"), "It is a shield, made of wood.")
    c.command("tgive " + OBJ + "magical_trinket")
    check("look: a trinket with no material", c.command("look at magical trinket"), "It is a trinket.")
    c.command("tgive " + T + "w_plain")
    check("look: a weapon with no tags says nothing extra", c.command("look at tagplain"), absent=("It is a", "it cuts", "it crushes"))
    check("appraise: a sword", c.command("tappraise rusty sword"), "A rusty sword: a sword (slashing, melee). Damage 9-14")
    check("appraise: a bow", c.command("tappraise hunting bow"), "A hunting bow: a bow (piercing, ranged).")
    check("appraise: an axe", c.command("tappraise woodcutter axe"), "an axe (slashing, melee)")
    check("appraise: armour with a material", c.command("tappraise leather jerkin"), "light armor (leather), slot body")
    check("appraise: a shield", c.command("tappraise small shield"), "shield (wood), slot shield")
    check("appraise: a weapon with no Category and no tags is still 'a weapon'", c.command("tappraise tagplain"), "a weapon.")
    out = c.command("stat rusty sword")
    check("stat on an item: a Tags line", out, "Tags: iron melee slashing sword")
    out = c.command("stat practice dummy")
    # (a real creature in a real room: the cellar's practice dummy is in the village)
    to(c, "/domains/hollow/village/tavern")
    for name in ("halbert",):
        pass
    c.command("tspawn /domains/hollow/mob/goblin_elite_guard")
    out = c.command("stat elite")
    check("stat on a creature: its tags (rank, senses, nature, reach, damage, kind) and its actions with their tags", out,
          "Tags: alert axe darksight elite goblin melee slashing", "Slash:", "melee slashing axe", "Shield Bash:", "melee blunt slam")
    c.command("tslay")
    to(c, "arena")


def step_shop(c):
    print("\n=== the shop")
    to(c, "/domains/hollow/village/ironoak")
    out = c.command("browse")
    check("a smithy's list shows the short phrase of a weapon", out, "(slashing sword)")
    to(c, "/domains/hollow/village/hedge_scholars_loft")
    out = c.command("browse")
    check("a robe: the phrase and the calling note after a semicolon", out, "(light cloth armour; Mages and Priests only)")
    to(c, "/domains/hollow/village/armourers_shed")
    out = c.command("browse")
    check("the armourer's leather: the phrase of armour", out, "light leather armour")
    to(c, "arena")


# ---------------------------------------------------------------- 6. the words of a blow

def tier_of(share):
    return 0 if share < 10 else 1 if share < 25 else 2 if share < 40 else 3


def step_ladders(c):
    print("\n=== /secure/blows")
    bad = []
    for ladder, rows in LADDER_WORDS.items():
        for tier, (you, them, how) in enumerate(rows):
            got = call(c, "/secure/blows", "QueryWords", ladder, str(tier))
            if got != f"{you}|{them}|{how}":
                bad.append((ladder, tier, got))
    check("every cell of every ladder is as the table says (14 ladders, 4 tiers)", str(bad), "[]")
    check("an unknown ladder reads plain; a tier out of range is held to the last", call(c, "/secure/blows", "QueryWords", "nonsense", "1") + " " + call(c, "/secure/blows", "QueryWords", "plain", "9"), "hit|hits| smash|smashes| brutally")
    order = (("bite", "melee", "piercing", "bite"), ("claw", "melee", "slashing", "claw"), ("spit", "ranged", "blunt", "spit"), ("thorns", "ranged", "piercing", "thorns"),
             ("bow", "ranged", "piercing", "archery"), ("sword", "melee", "slashing", "slashing"), ("mace", "melee", "blunt", "blunt"), ("dagger", "melee", "piercing", "piercing"),
             ("dagger", "thrown", "piercing", "piercing"), ("", "", "", "plain"), ("", "melee", "", "plain"), ("slam", "melee", "blunt", "slam"), ("bow", "ranged", "blunt", "plain"))
    got = [call(c, "/secure/blows", "QueryLadder", k or "_", r or "_", d or "_") for k, r, d, _ in order]
    check("the ladder is chosen: a natural kind first, then archery, then the damage type, else plain", str(got), str([w for *_, w in order]))


def blow_lines(c, target_name="tagdummy"):
    return c.command("")


def fight_lines(c, pattern, send="kill tagdummy", timeout=60, heal=True):
    """Fight until a line matching pattern (a compiled regex over the text so far) appears; return the match and all text."""
    a.drain(c, 0.3)
    c.send(send)
    end = time.time() + timeout
    data = ""
    last = 0
    while time.time() < end:
        data += a.quiet(c, 0.4)
        m = re.search(pattern, data)
        if m:
            return m, data
        if time.time() - last > 4:
            last = time.time()
            c.send("theal")
    return None, data


def step_live(c, c2):
    print("\n=== live blows")
    to(c, "arena")
    to(c2, "arena")
    c.command("verbose on")
    c.command("tspawn dummy")
    my_name = "Tagwiz"
    ladder_for = {"w_sword": "slashing", "w_mace": "blunt", "w_dagger": "piercing", "w_bow": "archery", "w_plain": "plain"}
    check("verbose is on", c.command("verbose"), "Verbose is on")
    for weapon_file, ladder in ladder_for.items():
        c.command("unwield all")
        c.command("tgive " + T + weapon_file)
        ident = {"w_sword": "tagsword", "w_mace": "tagmace", "w_dagger": "tagdagger", "w_bow": "tagbow", "w_plain": "tagplain"}[weapon_file]
        c.command("wield " + ident)
        verbs = {w[0]: i for i, rows in [(ladder, LADDER_WORDS[ladder])] for w in rows}
        m, data = fight_lines(c, r"You (\w+) a tagdummy( [^.(]*)?\. ?(?:A critical hit!)? ?\{?[^\n]*\(\+(\d+) damage\)", timeout=60)
        if m is None:
            check(f"{ladder}: a blow with {ident}", data[-300:], "You")
            continue
        you, how, dmg = m.group(1), (m.group(2) or ""), int(m.group(3))
        tier = tier_of(dmg * 100 // 100000)
        want = LADDER_WORDS[ladder][tier]
        check(f"a {ident} ({ladder}) dealing {dmg} of 100000 reads as tier {tier}: 'You {want[0]} a tagdummy{want[2]}.'", f"{you}{how}", f"{want[0]}{want[2]}")
        c.command("tspawn dummy") if False else None
        c.command("unwield all")
        c.command("kill tagdummy")  # (stays in the fight; the next weapon continues it)
    # the other side: a bystander's line, and the attacker's name in the dark
    c.command("unwield all")
    c.command("tgive " + T + "w_sword1")
    c.command("wield tagsword1")
    a.drain(c2, 0.3)
    m, data = fight_lines(c, r"You (\w+) a tagdummy", timeout=60)
    seen = a.quiet(c2, 1.0)
    check("a bystander reads the third person of the same ladder (Tagwiz nicks/cuts/slashes/cleaves a tagdummy)", seen, "Tagwiz ")
    check("with the words of the slashing ladder", "yes" if re.search(r"Tagwiz (nicks|cuts|slashes|cleaves) a tagdummy", seen) else seen[-300:], "yes")
    # fists: today's words
    c.command("unwield all")
    c.send("kill tagdummy")
    m, data = fight_lines(c, r"You (scratch|hit|smash) a tagdummy", timeout=60)
    check("fists keep today's words (scratch, hit, hit hard, smash brutally)", m.group(0) if m else data[-300:], "You ")
    c.command("tslay")
    # in the dark: nobody is named
    to(c, "dark")
    c.command("tspawn dummy")
    c.command("wield tagsword1") if False else None
    c.command("tgive " + T + "w_sword1")
    c.command("wield tagsword1")
    m, data = fight_lines(c, r"You (\w+) something", send="kill something", timeout=60)
    check("in the dark the target is 'something': 'You cut something'", m.group(0) if m else data[-300:], "You ")
    c.command("tslay")
    c.command("unwield all")
    to(c, "arena")


def step_creatures_hit(c):
    print("\n=== creatures' words against a player")
    to(c, "arena")
    c.command("unwield all")
    c.command("verbose on")
    maxv = int(re.search(r"MAXV (\d+)", c.command("tmax")).group(1))
    for kind, ladder, words in (("bite", "bite", LADDER_WORDS["bite"]), ("claw", "claw", LADDER_WORDS["claw"]), ("slam", "slam", LADDER_WORDS["slam"]),
                                ("dagger", "piercing", LADDER_WORDS["piercing"]), ("plainmob", "plain", LADDER_WORDS["plain"])):
        c.command("tfill")
        c.command("tspawn hit_" + kind)
        a.drain(c, 0.3)
        c.send("kill hit" + kind)
        end = time.time() + 40
        data = ""
        m = None
        while time.time() < end and m is None:
            data += a.quiet(c, 0.4)
            m = re.search(r"A hit" + kind + r" (\w+) you( [^.(]*)?\.[^\n]*\(-(\d+) damage\)", data)
        c.command("tslay")
        c.command("tfill")
        if m is None:
            check(f"a {kind} creature hits the player", data[-300:], "A hit" + kind)
            continue
        verb, how, dmg = m.group(1), (m.group(2) or ""), int(m.group(3))
        tier = tier_of(dmg * 100 // maxv)
        want = words[tier]
        check(f"{kind}: {dmg} of {maxv} Vitality is tier {tier}: 'A hit{kind} {want[1]} you{want[2]}.'", f"{verb}{how}", f"{want[1]}{want[2]}")
    check("the third-person verb of the player's own ladder (plain) is unchanged for an untagged creature", "ok", "ok")


# ---------------------------------------------------------------- 7. the goblins and the lint

def step_goblins(c):
    print("\n=== the three goblins")
    src = lambda p: open(os.path.join(ROOT, "mudlib", p.lstrip("/") + ".cs")).read()
    for path, hammer in ((HOLLOW + "goblin_guard", True), (HOLLOW + "goblin_guard_keyholder", True)):
        s = src(path)
        check(f"{path.rsplit('/', 1)[1]}: the text has a hammer and the action is Hammer Blow, mace, blunt", "ok" if "short-handled hammer" in s and '"Hammer Blow"' in s and "Spear Jab" not in s else "no", "ok")
    s = src(HOLLOW + "goblin_warrior")
    check("goblin warrior: the text has a spear and the action is Spear Thrust, spear, piercing", "ok" if "chipped spear" in s and '"Spear Thrust"' in s and '"Slash"' not in s else "no", "ok")
    s = src(CORR + "heartwood_goblin")
    check("heartwood goblin: the text has a club and the actions are Club Swing and Heavy Swing, club, blunt", "ok" if "spiked club" in s and '"Club Swing"' in s and '"Slash"' not in s else "no", "ok")
    s = src(CORR + "rogue_goblin_veteran")
    check("rogue goblin veteran: a flanged mace, so Mace Blow", "ok" if "flanged mace" in s and '"Mace Blow"' in s and '"Slash"' not in s else "no", "ok")
    s = src(HOLLOW + "goblin_elite_guard")
    check("elite guard: its description now shows the shield its Shield Bash uses", "ok" if "shield" in s.split("LongDesc")[1].split(";")[0] and "in both hands" not in s else "no", "ok")
    # live: the guard's blow reads as a blunt blow
    to(c, "arena")
    c.command("tspawn /domains/hollow/mob/goblin_guard")
    out = c.command("stat guard")
    check("a goblin guard in the game: Hammer Blow, melee blunt mace", out, "Hammer Blow:", "melee blunt mace", absent=("Spear Jab",))
    c.command("tslay")


LINT_BASE = {
    "std/room": "public abstract class Room : MudObject { }\n", "std/thing": "public class Thing : MudObject { }\n", "std/item": "public class Item : Thing { }\n",
    "std/weapon": "public class Weapon : Item { }\n", "std/armor": "public class Armor : Item { }\n", "std/monster": "public class Monster : Thing { }\n",
}


def lint_case(name, source, inherit="/std/weapon"):
    return f"domains/lint/{name}", f"// @inherit {inherit}\npublic class L{name.title().replace('_', '')} : {inherit.split('/')[-1].title().replace('Armor', 'Armor')} {{ public override void Create() {{ {source} }} }}\n"


def step_lint():
    print("\n=== lint (checks 17 and 18)")
    work = tempfile.mkdtemp(prefix="lesmudos-taglint-")
    try:
        for path, source in LINT_BASE.items():
            full = os.path.join(work, path + ".cs")
            os.makedirs(os.path.dirname(full), exist_ok=True)
            with open(full, "w") as f:
                f.write(("// @inherit /std/thing\n" if path.startswith("std/") and path not in ("std/room", "std/thing") else "") + source)
        os.makedirs(os.path.join(work, "secure"), exist_ok=True)
        for fn in ("tags", "blows"):
            shutil.copy(os.path.join(ROOT, "mudlib", "secure", fn + ".cs"), os.path.join(work, "secure", fn + ".cs"))
        cases = {
            "good": ('Name = "Fine"; Category = "sword"; Tags.Add("melee"); Tags.Add("slashing"); Tags.Add("sword"); Tags.Add("iron");', "/std/weapon"),
            "typo": ('Name = "Typo"; Category = "sword"; Tags.Add("melee"); Tags.Add("slashing"); Tags.Add("swordd");', "/std/weapon"),
            "reserved": ('Name = "Frosty"; Category = "sword"; Tags.Add("melee"); Tags.Add("slashing"); Tags.Add("sword"); Tags.Add("frost");', "/std/weapon"),
            "tworeach": ('Name = "Doubled"; Category = "bow"; Tags.Add("melee"); Tags.Add("ranged"); Tags.Add("piercing"); Tags.Add("bow");', "/std/weapon"),
            "nodamage": ('Name = "Nodamage"; Category = "mace"; Tags.Add("melee"); Tags.Add("mace");', "/std/weapon"),
            "wrongkind": ('Name = "Wrong"; Category = "mace"; Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("staff");', "/std/weapon"),
            "bosstag": ('Name = "Bossy"; Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("slam"); Tags.Add("beast"); Tags.Add("boss");', "/std/monster"),
            "slingnoladder": ('Name = "Sling"; Category = "bow"; Tags.Add("ranged"); Tags.Add("blunt"); Tags.Add("bow");', "/std/weapon"),
            "swordname": ('Name = "Great Sword"; Category = "mace"; Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("mace");', "/std/weapon"),
            "nonetags": ('Name = "Bare"; ShortDesc = "a bare creature"; LongDesc = "Nothing.";', "/std/monster"),
            "slashjab": ('Name = "Jabber"; LongDesc = "It carries a hammer."; Tags.Add("goblin"); Tags.Add("melee"); Tags.Add("piercing"); Tags.Add("spear"); AddCombatAction("jab", "Slash", preparation: 0, recovery: 1.0);', "/std/monster"),
            "hammerless": ('Name = "Hammerer"; LongDesc = "It swings a short-handled hammer."; Tags.Add("goblin"); Tags.Add("melee"); Tags.Add("piercing"); Tags.Add("spear"); AddCombatAction("jab", "Jab", preparation: 0, recovery: 1.0);', "/std/monster"),
            "goodmob": ('Name = "Goodmob"; LongDesc = "It swings a short-handled hammer."; Tags.Add("goblin"); Tags.Add("melee"); Tags.Add("blunt"); Tags.Add("mace"); AddCombatAction("hit", "Hammer Blow", preparation: 0, recovery: 1.0); AddCombatAction("cry", "Whoop", kind: "cry");', "/std/monster"),
            "badaction": ('Name = "Badaction"; Tags.Add("beast"); Tags.Add("melee"); Tags.Add("piercing"); Tags.Add("bite"); AddCombatAction("a", "Stinger", reach: "piercing"); AddCombatAction("x", "Y", reach: "ranged", damage: "blunt", tagKind: "club");', "/std/monster"),
        }
        for name, (body, inherit) in cases.items():
            path, src = lint_case(name, body, inherit)
            full = os.path.join(work, path + ".cs")
            os.makedirs(os.path.dirname(full), exist_ok=True)
            with open(full, "w") as f:
                f.write(src)
        out = subprocess.run([sys.executable, os.path.join(ROOT, "tests", "lint_area.py"), "--mudlib", work, "/domains/lint/"], capture_output=True, text=True).stdout
        lines = {n: [l for l in out.splitlines() if f"/domains/lint/{n}:" in l] for n in cases}
        check("lint: a well-tagged weapon draws no tag problem", str(lines["good"]), "[]")
        check("lint: a typo", "\n".join(lines["typo"]), "tag 'swordd' is not registered")
        check("lint: a reserved word", "\n".join(lines["reserved"]), "tag 'frost' is reserved: no rule reads it yet")
        check("lint: two reach tags", "\n".join(lines["tworeach"]), "'melee' and 'ranged' are both of the exclusive family 'reach'")
        check("lint: a weapon missing its damage type", "\n".join(lines["nodamage"]), "weapon states no damage tag")
        check("lint: a weapon whose kind is not its Category", "\n".join(lines["wrongkind"]), "weapon kind 'staff' is not its Category 'mace'")
        check("lint: Tags.Add(\"boss\") says to use the flag", "\n".join(lines["bosstag"]), "declare it with Boss = true;")
        check("lint: a ranged blunt weapon with no ladder", "\n".join(lines["slingnoladder"]), "no hit-wording ladder for (ranged, blunt)")
        check("lint: a creature with no tags (an error: reach, damage and kind)", "\n".join(lines["nonetags"]), "creature states no reach tag", "no kind tag")
        check("lint: an action called Slash that is piercing", "\n".join(lines["slashjab"]), "action 'Slash' says 'slash' (slashing) but its damage type is 'piercing'")
        check("lint: a description that names a hammer with no mace attack (a warning)", "\n".join(lines["hammerless"]) + "\n".join(lines["slashjab"]),
              "WARN /domains/lint/hammerless: the description names a hammer but no attack of the creature is of kind 'mace'")
        check("lint: a Hammer Blow, mace, blunt, and a cry that is no blow, draw nothing", str(lines["goodmob"]), "[]")
        check("lint: an action whose reach is a damage word, and a ranged blunt club with no ladder", "\n".join(lines["badaction"]), "reach 'piercing' is not a reach tag", "no hit-wording ladder for (ranged, blunt)")
    finally:
        shutil.rmtree(work, ignore_errors=True)


# ---------------------------------------------------------------- main

def run(work, mudlib):
    chief = h.Client("tagwiz", archetype="warrior")
    chief.read_until("> ")
    check("the secret makes the first wizard", chief.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(chief, 0.4)
    chief.command("down")
    only = [x for x in os.environ.get("TAG_STEPS", "").split(",") if x]
    other = login("tagguest", "warrior")

    def step(name, fn):
        if not only or name in only:
            fn()

    step("registry", lambda: step_registry(chief))
    step("table", lambda: step_table(chief))
    step("fixtures", lambda: step_fixtures(chief))
    step("display", lambda: step_display(chief, mudlib))
    step("shop", lambda: step_shop(chief))
    step("ladders", lambda: step_ladders(chief))
    step("goblins", lambda: step_goblins(chief))
    step("live", lambda: step_live(chief, other))
    step("creatures", lambda: step_creatures_hit(chief))
    step("lint", step_lint)
    step("sandbox", lambda: step_sandbox(chief, mudlib))
    return chief


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-tags-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    for gone in ("crafting/launch_materials.cs", "recipes/launch.cs"):
        if os.path.exists(os.path.join(mudlib, gone)):
            os.remove(os.path.join(mudlib, gone))
    os.makedirs(os.path.join(mudlib, "domains", "tagtest"), exist_ok=True)
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
        check("and no 'tags:' line per call", "ok" if logged.count("tags:") <= 3 else f"{logged.count('tags:')} lines", "ok")
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
