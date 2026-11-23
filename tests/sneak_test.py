#!/usr/bin/env python3
"""Tests for openable doors and the Scout's sneak (docs/specs/sneak.md; docs/BUILDING.md, "A room with a door").

  1. Doors (the "doors" step): the exits line and `look at door`; a closed door stops walking from both sides and says so; `open`, `close` by
     name, direction, letter and "east door", heard by both rooms; one shared state (open from one side, open from the other); a keyed door
     (unlock and lock need the key, refusals in words); flee against a closed door; a patrol stopped by a closed door and walking when it opens;
     Tracking not reading through it (and the rank II line); the room's reset shutting it for both sides, a door that does not close on reset,
     a reload of one room not desynchronising the two; the real door between the Overseer Hall and the Overseer's chamber.
  2. Probes (the "doorprobe" step): a wizard's thing and a wizard's own room cannot open, close, lock, unlock or reset anyone's door, forge the
     partner's lines, or force a reset to shut one.
  3. Sneak (the "sneak", "leave", "loud", "ambush", "spotted", "ranks" and "sneakprobe" steps): see the functions below.

Usage (after `dotnet build`):   LESMUDOS_TEST_PORT=4839 python3 tests/sneak_test.py        (SNEAK_STEPS=doors,doorprobe to run some steps)
"""
import os
import re
import shutil
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4839")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402
import area_test as a  # noqa: E402
import rank_test as rk  # noqa: E402

T = "/domains/sneaktest/"
check = h.check
VILLAGE = "/domains/hollow/village/"
HALL = "/domains/greenvale/oakenfell/guild/hall_of_callings"
CELLARS = "/domains/hollow/cellars/"

# A trusted probe (under /domains): what a player cannot do by command, for the test only.
PROBE = r'''// @inherit /std/thing
public class SnProbe : Thing
{
    public override void Create() { Name = "Probe"; Ids.Add("probe"); }
    public override string? Short() => "a probe";
    public override void Init()
    {
        foreach (var v in new[] { "sgold", "slevel", "sspawn", "stele", "sstat", "shurt", "sfill", "sslay", "sgive", "sdoor", "srolls", "sres", "sclone", "sdrop", "sspawnin", "sclear", "schance", "sskill" })
            AddAction(v, Do);
    }
    private ObjectRef? Named(string name, ObjectRef room) =>
        Users().FirstOrDefault(u => u.Call<string>("QueryName")!.ToLowerInvariant() == name) ?? Present(name, room);
    private bool Do(string args)
    {
        var me = ThisPlayer()!;
        var w = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var room = Environment(me)!;
        switch (QueryVerb())
        {
            case "sgold": me.Call("AddGold", int.Parse(w[0])); Write("GOLD " + me.Call<int>("QueryGold") + "\n"); return true;
            case "slevel":
            {
                var want = int.Parse(w[0]);
                while (me.Call<int>("QueryLevel") < want) me.Call("AddXp", me.Call<int>("QueryXpForLevel", me.Call<int>("QueryLevel") + 1) - me.Call<int>("QueryXp"));
                Write("LEVEL " + me.Call<int>("QueryLevel") + "\n"); return true;
            }
            case "sspawn": { var n = w.Length > 1 ? int.Parse(w[1]) : 1; for (var i = 0; i < n; i++) MoveObject(CloneObject("/domains/sneaktest/" + w[0]), room); Write("SPAWNED " + n + "\n"); return true; }
            case "stele": MoveObject(me, LoadObject(args.Trim())); Write("TELE ok\n"); return true;
            case "sstat":
                Write($"STAT end {me.Call<int>("QueryEndurance")}/{me.Call<int>("QueryMaxEndurance")} focus {me.Call<int>("QueryFocus")}/{me.Call<int>("QueryMaxFocus")} vit {me.Call<int>("QueryVitality")}/{me.Call<int>("QueryMaxVitality")} sneaking {me.Call<bool>("QuerySneaking")} fighting {me.Call<bool>("QueryInCombat")} grace {me.Call<int>("QueryAttribute", "grace")}\n");
                return true;
            case "shurt": me.Call("Heal", 99999); me.Call("TakeDamage", int.Parse(w[0]), null); Write("HURT " + me.Call<int>("QueryVitality") + "\n"); return true;
            case "sfill": me.Call("FillResources"); Write("FILLED\n"); return true;
            case "sslay":
            {
                var n = 0;
                foreach (var o in AllInventory(room).ToList())
                    if (o != me && o.IsAlive && o.Call<bool>("IsMonster")) { o.Call("Die", (ObjectRef?)null); n++; }
                Write("SLAIN " + n + "\n"); return true;
            }
            case "sgive": { var ob = CloneObject(w[0].StartsWith("/") ? w[0] : "/domains/sneaktest/" + w[0]); MoveObject(ob, me); Write("GAVE " + ob.Name + "\n"); return true; }
            case "sdrop": { var ob = Present(w[0], me); if (ob != null) ob.Call("Vanish"); Write("DROPPED " + (ob != null && !ob.IsAlive) + "\n"); return true; }
            case "sdoor":
            {
                // sdoor ROOM DIRECTION: what the room says of its door, and what the register says of the pair.
                var r = LoadObject(w[0]);
                var dir = w[1];
                Write("DOOR " + w[0].Substring(w[0].LastIndexOf('/') + 1) + " " + dir + " " + r.Call<string>("QueryDoorState", dir) + " name=" + r.Call<string>("QueryDoorName", dir) + "\n");
                return true;
            }
            case "srolls": Write("ROLLS " + LoadObject("/secure/sneak").Call<bool>("SetTestRolls", w[0]) + "\n"); return true;
            case "sres": me.Call("SetResources", int.Parse(w[0]), int.Parse(w[1])); Write("RES " + me.Call<int>("QueryEndurance") + "\n"); return true;
            case "sspawnin": MoveObject(CloneObject("/domains/sneaktest/" + w[1]), LoadObject(w[0])); Write("SPAWNEDIN\n"); return true;
            case "sclear": { foreach (var o in AllInventory(LoadObject(w[0])).ToList()) if (o.IsAlive && o.Call<bool>("IsMonster")) o.Call("Die", (ObjectRef?)null); Write("CLEARED\n"); return true; }
            case "schance":
            {
                var n = w.Select(int.Parse).ToArray();
                Write("CHANCE " + LoadObject("/secure/sneak").Call<int>("NoticeChance", n[0], n[1], n[2], n[3] == 1, n[4]) + "\n"); return true;
            }
            case "sskill": { var k = LoadObject("/skills/" + w[0]); Write("SKILL " + string.Join(" ", Enumerable.Range(1, int.Parse(w[2])).Select(r => k.Call<double>("RankValue", w[1], r))) + "\n"); return true; }
            case "sclone": { var r = CloneObject(w[0]); MoveObject(r, room); Write("CLONED " + r.Name + "\n"); return true; }
        }
        return false;
    }
}
'''

DOOR_ROOM = """// @inherit /std/room
public class {cls} : Room
{{
    public override void Create()
    {{
        ShortDesc = "{short}";
        LongDesc = "A room made for the door test.";
        ResetSeconds = {reset};
{exits}{body}    }}
{extra}}}
"""


def droom(cls, short, exits, body="", extra="", reset=3000):
    ex = "".join(f'        Exits["{d}"] = "{p}";\n' for d, p in exits.items())
    return DOOR_ROOM.format(cls=cls, short=short, exits=ex, body=body, extra=extra, reset=reset)


def creature(cls, name, ident, hp=50000, reaction=6.0, level=1, aggressive=False, to_hit=-40, dmg=1, flags="", short=None):
    return f"""// @inherit /std/monster
public class {cls} : Monster
{{
    public override void Create()
    {{
        Name = "{name}"; Ids.Add("{ident}"); ShortDesc = "{short or 'a ' + name.lower()}"; LongDesc = "A creature made for the sneak test.";
        Level = {level}; MaxVitality = {hp}; DamageMin = {dmg}; DamageMax = {dmg}; ToHit = {to_hit}; Armor = 0; Dodge = 0; XpValue = 0;
        Aggressive = {'true' if aggressive else 'false'};
        BaseReaction = {reaction};
{flags}    }}
}}
"""


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


KEY = """// @inherit /std/item
public class SnKey : Item
{
    public override void Create() { Name = "Test Key"; Ids.Add("sneakkey"); Ids.Add("key"); ShortDesc = "a test key"; LongDesc = "A key made for the door test."; Value = 1; }
}
"""

PATROL = """// @inherit /std/patrol
public class SnWarden : Patrol
{
    public override void Create()
    {
        Name = "Warden"; Ids.Add("warden"); ShortDesc = "a warden"; LongDesc = "A warden made for the door test.";
        Level = 1; MaxVitality = 50;
        PollSeconds = 1;
        SetRoute(new[] { "/domains/sneaktest/pat_a", "/domains/sneaktest/pat_b" }, 1, false);
    }
}
"""

# A wizard's thing and a wizard's own room (under /wiz): everything a stranger could try with doors, on the game's own doors.
DOOR_MEDDLER = r'''// @inherit /std/thing
public class SnDoorMeddler : Thing
{
    public override void Create() { Name = "meddler"; Ids.Add("meddler"); }
    public override string? Short() => "a meddler";
    public override void Init() { AddAction("meddle", Meddle); }
    private bool Meddle(string args)
    {
        var doors = LoadObject("/secure/doors");
        var tries = new List<string>();
        void Try(string label, Func<object?> f)
        {
            try { tries.Add(label + "=" + (f() ?? "null")); }
            catch (MudRuntimeException e) { tries.Add(label + "!" + e.Message.Length); }
        }
        var key = "/domains/sneaktest/door_a|/domains/sneaktest/door_b";
        var hall = "/domains/hollow/cellars/overseer_chamber|/domains/hollow/cellars/overseer_hall";
        var a = LoadObject("/domains/sneaktest/door_a");
        var b = LoadObject("/domains/sneaktest/door_b");
        Try("StateRead", () => doors.Call<int>("QueryState", key));
        Try("State", () => doors.Call<int>("State", key, true, false));
        Try("Open", () => doors.Call<bool>("Open", key));
        Try("Close", () => doors.Call<bool>("Close", key));
        Try("Lock", () => doors.Call<bool>("Lock", key));
        Try("Unlock", () => doors.Call<bool>("Unlock", key));
        Try("Reset", () => doors.Call<bool>("Reset", key, true));
        Try("OpenHall", () => doors.Call<bool>("Open", hall));
        Try("ResetHall", () => doors.Call<bool>("Reset", hall, true));
        Try("StateAfter", () => doors.Call<int>("QueryState", key));
        Try("RoomReset", () => { a.Call("Reset"); b.Call("Reset"); return "called"; });
        Try("DoorHeard", () => b.Call("DoorHeard", "/domains/sneaktest/door_a", "swings open"));
        Try("DoorHeardForged", () => b.Call("DoorHeard", "/domains/sneaktest/door_c", "is pushed shut"));
        Try("RoomState", () => a.Call<string>("QueryDoorState", "east"));
        Write("MEDDLE " + string.Join(" ", tries) + "\n");
        return true;
    }
}
'''

# A wizard's own room (under /wiz) with a door to a game room, and the same tries as a room: it is one end of its own door only.
WIZ_ROOM = r'''// @inherit /std/room
public class SnWizRoom : Room
{
    public override void Create()
    {
        ShortDesc = "Wizard Room";
        LongDesc = "A room in a wizard's home.";
        Exits["east"] = "/domains/sneaktest/door_b";
        AddDoor("east", "door");
    }
    public override void Init()
    {
        base.Init();
        if (ThisPlayer() != null) AddAction("wmeddle", WMeddle);
    }
    private bool WMeddle(string args)
    {
        var doors = LoadObject("/secure/doors");
        var tries = new List<string>();
        void Try(string label, Func<object?> f)
        {
            try { tries.Add(label + "=" + (f() ?? "null")); }
            catch (MudRuntimeException e) { tries.Add(label + "!" + e.Message.Length); }
        }
        var real = "/domains/sneaktest/door_a|/domains/sneaktest/door_b";
        var mine = "/domains/sneaktest/door_b|" + Self.ProgramPath;
        Try("OwnState", () => doors.Call<int>("State", mine, false, false));
        Try("OwnOpen", () => doors.Call<bool>("Open", mine));
        Try("OwnClose", () => doors.Call<bool>("Close", mine));
        Try("RealOpen", () => doors.Call<bool>("Open", real));
        Try("RealClose", () => doors.Call<bool>("Close", real));
        Try("RealReset", () => doors.Call<bool>("Reset", real, true));
        Try("RealState", () => doors.Call<int>("State", real, true, false));
        Try("RealRead", () => doors.Call<int>("QueryState", real));
        Write("WMEDDLE " + string.Join(" ", tries) + "\n");
        return true;
    }
}
'''


def test_files():
    f = {T + "probe": PROBE, T + "key": KEY, T + "warden": PATROL}
    f[T + "bow"] = weapon("SnBow", "Even Bow", "evenbow", "bow", 10, 10)
    f[T + "sword"] = weapon("SnSword", "Even Sword", "evensword", "sword", 10, 10, 0.0)
    f[T + "dummy"] = creature("SnDummy", "Dummy", "dummy", reaction=6.0)
    # Doors: a plain pair (open from either side), a keyed pair, a pair that resets quickly, one that does not close on reset, a patrol's pair.
    f[T + "door_a"] = droom("SnDoorA", "Door Hall A", {"east": T + "door_b", "out": T + "hall"}, 'AddDoor("east", "door", description: "A plain test door.");\n')
    f[T + "door_b"] = droom("SnDoorB", "Door Hall B", {"west": T + "door_a"}, 'AddDoor("west", "door");\n')
    f[T + "lock_a"] = droom("SnLockA", "Lock Hall A", {"east": T + "lock_b", "out": T + "hall"}, 'AddDoor("east", "iron gate", keyId: "sneakkey");\n')
    f[T + "lock_b"] = droom("SnLockB", "Lock Hall B", {"west": T + "lock_a"}, 'AddDoor("west", "iron gate", keyId: "sneakkey");\n')
    f[T + "reset_a"] = droom("SnResetA", "Reset Hall A", {"east": T + "reset_b", "out": T + "hall"}, 'AddDoor("east", "door", keyId: "sneakkey");\n', reset=6)
    f[T + "reset_b"] = droom("SnResetB", "Reset Hall B", {"west": T + "reset_a"}, 'AddDoor("west", "door", keyId: "sneakkey");\n', reset=6)
    f[T + "stay_a"] = droom("SnStayA", "Stay Hall A", {"east": T + "stay_b", "out": T + "hall"}, 'AddDoor("east", "door", closed: false, closesOnReset: false);\n', reset=6)
    f[T + "stay_b"] = droom("SnStayB", "Stay Hall B", {"west": T + "stay_a"}, 'AddDoor("west", "door", closed: false, closesOnReset: false);\n', reset=6)
    f[T + "pat_a"] = droom("SnPatA", "Patrol Hall A", {"east": T + "pat_b", "out": T + "hall"}, 'AddDoor("east", "door");\nAddSpawn("/domains/sneaktest/warden");\n')
    f[T + "pat_b"] = droom("SnPatB", "Patrol Hall B", {"west": T + "pat_a"}, 'AddDoor("west", "door");\n')
    f[T + "hall"] = droom("SnHall", "Sneak Hall", {"up": VILLAGE + "square", "door_a": T + "door_a", "lock_a": T + "lock_a", "reset_a": T + "reset_a", "stay_a": T + "stay_a", "pat_a": T + "pat_a", "lurk": T + "n_lurk", "sent": T + "n_sent", "tyrant": T + "n_tyrant", "giant": T + "n_giant", "two": T + "n_two", "empty": T + "n_empty", "morgue": T + "n_morgue", "jabroom": T + "n_jabroom", "ndoor": T + "n_ndoor"}, "",
                          rk.HALL_INIT.replace("/domains/ranktest/probe", T + "probe"))
    f.update(sneak_files())
    return f


CHEST = """// @inherit /std/find
public class SnChest : Find
{
    public override void Create()
    {
        Name = "Test Chest"; Ids.Add("chest"); ShortDesc = "a test chest"; LongDesc = "A chest made for the sneak test.";
        ItemPath = "/domains/sneaktest/sword";
    }
}
"""

WIZ_MONSTER = """// @inherit /std/monster
public class SnWizLurker : Monster
{
    public override void Create()
    {
        Name = "Wizlurker"; Ids.Add("wizlurker"); ShortDesc = "a wizard's lurker"; LongDesc = "Made by a wizard.";
        Level = 1; MaxVitality = 50000; DamageMin = 1; DamageMax = 1; ToHit = -40; Armor = 0; Dodge = 0; XpValue = 0; Aggressive = true; BaseReaction = 2.0;
    }
}
"""

WIZ_LAIR = """// @inherit /std/room
public class SnWizLair : Room
{
    public override void Create()
    {
        ShortDesc = "Wizard Lair"; LongDesc = "A wizard's room with a wizard's creature.";
        Exits["out"] = "/domains/sneaktest/hall";
        AddSpawn("/wiz/snchief/wizlurker");
    }
}
"""

SNEAK_MEDDLER = r'''// @inherit /std/thing
public class SnSneakMeddler : Thing
{
    public override void Create() { Name = "snmeddler"; Ids.Add("snmeddler"); }
    public override string? Short() => "a snmeddler";
    public override void Init() { AddAction("smeddle", Meddle); }
    private bool Meddle(string args)
    {
        var who = Users().First(u => u.Call<string>("QueryName")!.ToLowerInvariant() == args.Trim());
        var room = Environment(who)!;
        var lurker = AllInventory(room).FirstOrDefault(o => o.IsAlive && o.Call<bool>("IsMonster"));
        if (lurker == null) { Write("MEDDLE NOLURKER in " + room.ProgramPath + "\n"); return true; }
        var tries = new List<string>();
        void Try(string label, Func<object?> f)
        {
            try { tries.Add(label + "=" + (f() ?? "null")); }
            catch (MudRuntimeException e) { tries.Add(label + "!" + e.Message.Length); }
        }
        var daemon = LoadObject("/secure/sneak");
        Try("Notice", () => who.Call<int>("SneakNotice", lurker!));
        Try("Engage", () => { lurker!.Call("Engage"); return "called"; });
        Try("Departs", () => who.Call<bool>("SneakDeparts", LoadObject("/domains/sneaktest/hall")));
        Try("Disturbed", () => { who.Call("Disturbed", "open"); who.Call("Disturbed", "say"); return "called"; });
        Try("Rolls", () => daemon.Call<bool>("SetTestRolls", "notice:0.0"));
        Try("RollsRandom", () => daemon.Call<bool>("SetTestRolls", "random"));
        Try("RollsNow", () => daemon.Call<string>("QueryTestRolls"));
        Try("Sneaking", () => who.Call<bool>("QuerySneaking"));
        Try("Surprising", () => who.Call<bool>("QuerySurprising"));
        Write("MEDDLE " + string.Join(" ", tries) + "\n");
        return true;
    }
}
'''

LURKERS = (("lurker", "Lurker", 1, ""), ("sentry", "Sentry", 1, "        Alert = true;\n"), ("tyrant", "Tyrant", 1, "        Boss = true;\n"), ("giant", "Giant", 6, ""))


def sneak_files():
    f = {}
    for ident, name, level, flag in LURKERS:
        f[T + ident] = creature("Sn" + name, name, ident, level=level, aggressive=True, reaction=6.0, flags=flag)
    f[T + "jab"] = creature("SnJab", "Jab", "jab", reaction=2.0)
    f[T + "lootdummy"] = creature("SnLootDummy", "Lootdummy", "lootdummy", hp=1, flags='        AddLoot("/domains/sneaktest/sword", 100);\n')
    f[T + "chest"] = CHEST
    # One room per case: each has its own creature (cloned when the room loads) and a way back out to the hall.
    for room, spawns in (("empty", ""), ("lurk", "lurker"), ("sent", "sentry"), ("tyrant", "tyrant"), ("giant", "giant"), ("two", "lurker lurker"), ("morgue", ""), ("jabroom", "")):
        f[T + "n_" + room] = droom("SnN" + room.capitalize(), "Sneak " + room.capitalize(), {"out": T + "hall"})
    f[T + "n_ndoor"] = droom("SnNDoor", "Sneak Door", {"out": T + "hall", "east": T + "n_ndoor_b"}, 'AddDoor("east", "door");\n')
    f[T + "n_ndoor_b"] = droom("SnNDoorB", "Sneak Door Beyond", {"west": T + "n_ndoor"}, 'AddDoor("west", "door");\n')
    return f


def login(name, calling):
    c = h.Client(name, archetype=calling)
    c.read_until("> ")
    a.drain(c, 0.4)
    c.command("down")
    return c


def to(c, where):
    c.command("stele " + (where if where.startswith("/") else T + where))
    a.drain(c, 0.3)


def say(c, line, wait=0.4):
    """A command and what follows it for a moment (the other lines the room prints)."""
    return c.command(line) + a.quiet(c, wait)


def state_of(c, room, direction):
    m = re.search(r"DOOR \w+ \w+ (\w*) name=(.*)", c.command(f"sdoor {T + room if not room.startswith('/') else room} {direction}"))
    return m.group(1) if m else "?"


# ---------------------------------------------------------------- 1. doors

def step_doors(chief):
    p1 = login("sndoora", "warrior")
    p2 = login("sndoorb", "warrior")
    to(p1, "door_a")
    to(p2, "door_a")
    out = p1.command("look")
    check("the exits line shows a closed door", out, "east (closed door)")
    check("look at door: its description and its state", p1.command("look at door"), "A plain test door.", "The door to the east is closed.")
    check("walking east into a closed door is refused in words", p1.command("east"), "The door to the east is closed.", "open door")
    check("the letter does the same", p1.command("e"), "The door to the east is closed.")
    check("and the register agrees with the room", state_of(chief, "door_a", "east"), "closed")
    # The other side, and the heard lines.
    to(chief, "door_b")
    a.drain(chief, 0.3)
    check("the other room's exits line shows its half, closed", chief.command("look"), "west (closed door)")
    check("nobody walks west through a closed door either", chief.command("west"), "The door to the west is closed.")
    # Open from A by the name; B hears it, p2 (in A) hears it.
    a.drain(p2, 0.2)
    out = p1.command("open door")
    check("open door: the opener is told", out, "You open the door to the east.")
    check("the other player in the room hears it", a.quiet(p2, 0.6), "Sndoora opens the door to the east.")
    check("the room on the other side hears it", a.quiet(chief, 0.6), "The door to the west swings open.")
    check("the exits line shows it open", p1.command("look"), "east (open door)")
    check("and from the other side too: one shared state", chief.command("look"), "west (open door)")
    check("opening an open door is refused", p1.command("open door"), "already open")
    # Walk through, both ways.
    out = p1.command("east")
    check("walk through the open door", out, "Door Hall B")
    check("and back", p1.command("west"), "Door Hall A")
    # Close from the other side: A is closed too.
    a.drain(chief, 0.2)
    a.drain(p1, 0.2)
    check("close from the other side", chief.command("close west"), "You close the door to the west.")
    check("the first room hears it", a.quiet(p1, 0.6), "The door to the east is pushed shut.")
    check("the state is one: closed from A as well", p1.command("east"), "The door to the east is closed.")
    check("closing a closed door is refused", p1.command("close door"), "already closed")
    # Words: a letter, the direction, the door and the direction.
    check("open by the letter", p1.command("open e"), "You open the door to the east.")
    p1.command("close east")
    check("open by the direction", p1.command("open east"), "You open the door to the east.")
    p1.command("close door")
    check("open by the door and the direction", p1.command("open east door"), "You open the door to the east.")
    p1.command("close door")
    check("with one door the name can be left out", p1.command("open"), "You open the door to the east.")
    check("a word that names nothing here is left to others", p1.command("open chest"), absent=("You open",))
    p1.command("close door")
    # Flee against a closed door.
    p1.command("sspawn dummy")
    p1.command("kill dummy")
    out = p1.command("flee east")
    check("flee east against a closed door has no usable exit", out, "no usable exit 'east'")
    p1.command("open door")
    a.drain(p1, 0.2)
    got = ""
    for _ in range(8):
        got += p1.command("flee east") + a.quiet(p1, 0.3)
        if "Door Hall B" in got:
            break
    check("with the door open the way out is usable", got, "You flee east!", "Door Hall B")
    to(p1, "door_a")
    p1.command("sslay")
    p1.command("close door")
    p2.command("quit")
    a.drain(chief, 0.2)

    # The keyed door: refusals, the key, lock and unlock.
    to(p1, "lock_a")
    check("a keyed door starts locked", p1.command("look"), "east (locked iron gate)")
    check("walking into a locked door", p1.command("east"), "The iron gate to the east is locked.")
    check("open on a locked door", p1.command("open gate"), "The iron gate to the east is locked.")
    check("unlock without the key", p1.command("unlock gate"), "You have nothing that fits the lock of the iron gate to the east.")
    p1.command("sgive key")
    check("look at the keyed door says it has a lock", p1.command("look at gate"), "is locked", "has a lock")
    check("unlock with the key", p1.command("unlock gate"), "You unlock the iron gate to the east with a test key.")
    check("unlocking an unlocked door", p1.command("unlock gate"), "is not locked")
    check("the exits line shows it closed again", p1.command("look"), "east (closed iron gate)")
    check("lock it again", p1.command("lock gate"), "You lock the iron gate to the east with a test key.")
    p1.command("unlock gate")
    check("open it", p1.command("open iron gate"), "You open the iron gate to the east.")
    check("an open door cannot be locked", p1.command("lock gate"), "Close it first.")
    p1.command("close gate")
    to(chief, "lock_b")
    a.drain(chief, 0.2)
    check("the other side shares the lock: closed, not locked, until its key is turned", chief.command("look"), "west (closed iron gate)")
    chief.command("sgive key")
    chief.command("lock gate")
    check("locked from B, locked from A", p1.command("look"), "east (locked iron gate)")
    check("a door with no lock cannot be locked", (to(p1, "door_a"), p1.command("lock door"))[1], "has no lock")
    chief.command("sdrop key")
    p1.command("quit")


def step_door_world(chief):
    # Patrol, Tracking, reset, reload: the doors in the world.
    p = login("sndoorc", "scout")
    # --- a patrol stopped by a closed door
    to(p, "pat_a")
    out = ""
    time.sleep(5)
    st = chief.command("stele " + T + "pat_a") and chief.command("stat warden")
    m = re.search(r"steps (\d+), blocked (\d+)", st)
    check("a patrol facing a closed door does not walk: steps 0, blocked more than 0", "ok" if m and m.group(1) == "0" and int(m.group(2)) > 0 else st[-300:], "ok")
    check("the warden is still in the first room", p.command("look"), "A warden is here.")
    p.command("open door")
    time.sleep(4)
    st = chief.command("stat warden")
    m = re.search(r"steps (\d+)", st)
    check("with the door open the patrol walks", "ok" if m and int(m.group(1)) > 0 else st[-300:], "ok")
    p.command("close door")
    # --- Tracking: not through a closed door, and the rank II line
    to(p, VILLAGE + "strung_yew")
    p.command("slevel 5")
    p.command("sgold 100")
    check("Tracking II bought", p.command("train track"), "Tracking II")
    to(chief, "door_b")
    chief.command("sspawn dummy")
    to(p, "door_a")
    a.drain(p, 0.3)
    p.command("sfill")
    out = p.command("track")
    check("Tracking does not read through a closed door (no east line)", out, absent=("east", "a dummy"))
    check("Tracking II, a creature behind a closed door: the one line, no direction", out, "Something is close by, though you cannot tell where.")
    p.command("open door")
    a.drain(p, 0.5)
    time.sleep(5.5)
    out = say(p, "track", 1.0)
    check("with the door open Tracking reads through it", out, "east", "a dummy", absent=("Something is close by",))
    p.command("close door")
    chief.command("sslay")
    # --- reset: a keyed door open at the reset is shut and locked again (6-second resets), a door that does not close stays
    to(chief, "reset_b")
    to(p, "reset_a")
    p.command("sgive key")
    a.drain(chief, 0.3)
    heard_a = heard_b = ""
    opened = ""
    for _ in range(6):  # the room resets every 6 seconds and would lock it again between the two commands
        heard_a += p.command("unlock door")
        opened = p.command("open door")
        heard_a += opened
        if "You open" in opened:
            break
    check("the keyed door opens", opened, "You open the door to the east.")
    deadline = time.time() + 14
    while time.time() < deadline and ("swings shut" not in heard_a or "swings shut" not in heard_b):
        heard_a += a.quiet(p, 0.5)
        heard_b += a.quiet(chief, 0.2)
    check("the reset shuts the open door: this room is told", heard_a, "The door to the east swings shut.")
    check("and the room on the other side is told", heard_b, "The door to the west swings shut.")
    check("shut and locked again (it has a key)", p.command("look"), "east (locked door)")
    check("the other side agrees", chief.command("look"), "west (locked door)")
    to(p, "stay_a")
    check("a door that does not close on reset starts open", p.command("look"), "east (open door)")
    time.sleep(8)
    check("and is still open after a reset", p.command("look"), "east (open door)")
    # --- a reload of one room does not desynchronise the pair
    to(p, "door_a")
    p.command("open door")
    check("update one room", chief.command("update " + T + "door_b"), absent=("rror",))
    time.sleep(0.5)
    to(chief, "door_b")
    check("after a reload of B the door is still open from B", chief.command("look"), "west (open door)")
    check("and from A", p.command("look"), "east (open door)")
    chief.command("close door")
    check("a close from the reloaded room shuts A's side too", p.command("look"), "east (closed door)")
    check("the register's record for the pair", state_of(chief, "door_a", "east") + state_of(chief, "door_b", "west"), "closedclosed")
    # --- the real door between the Overseer Hall and the Overseer's chamber
    check("the Overseer Hall's east door is closed and not locked", state_of(chief, CELLARS + "overseer_hall", "east"), "closed")
    check("the chamber's west door too", state_of(chief, CELLARS + "overseer_chamber", "west"), "closed")
    p.command("quit")


# ---------------------------------------------------------------- 2. the probe

def step_doorprobe(chief, mudlib):
    os.makedirs(os.path.join(mudlib, "wiz", "snchief"), exist_ok=True)
    h.write(mudlib, "/wiz/snchief/meddler", DOOR_MEDDLER)
    h.write(mudlib, "/wiz/snchief/wroom", WIZ_ROOM)
    watcher = login("snwatch", "warrior")
    to(watcher, "door_b")
    a.drain(watcher, 0.3)
    to(chief, "door_a")
    chief.command("open door")  # open, so that a forged close shows
    a.drain(watcher, 0.3)
    chief.command("goto ~/wroom")  # CHANGED (sandbox): a wizard's code runs in their workshop
    a.drain(chief, 0.3)
    check("the meddler is cloned", chief.command("clone ~/meddler"), "meddler")
    out = chief.command("meddle")
    to(chief, "door_a")
    check("the meddler ran", out, "MEDDLE ")
    check("a wizard's thing cannot register a door", out, "State=-1")
    check("nor open, close, lock, unlock or reset one", out, "Open=False", "Close=False", "Lock=False", "Unlock=False", "Reset=False", absent=("Open=True", "Close=True", "Lock=True", "Unlock=True", "Reset=True"))
    check("nor the game's real door between the Hall and the chamber", out, "OpenHall=False", "ResetHall=False")
    check("the door is still open (the read is free)", out, "StateRead=1", "StateAfter=1", "RoomState=open")
    check("a forced Reset of the rooms does not shut it", out, "RoomReset=called", "RoomState=open")
    check("a forged line from the partner is not told", a.quiet(watcher, 0.8), absent=("swings open", "pushed shut"))
    check("the door is open in the world as before", chief.command("look"), "east (open door)")
    # A wizard's own room is one end of its own door only.
    out = chief.command("goto ~/wroom") + a.quiet(chief, 0.3)
    check("a wizard's own room loads", out, "Wizard Room")
    out = chief.command("wmeddle")
    check("a wizard's room can use its own door", out, "OwnState=0", "OwnOpen=True", "OwnClose=True")
    check("but not the game's: its tries on the pair of door_a and door_b fail", out, "RealOpen=False", "RealClose=False", "RealReset=False", "RealState=-1", "RealRead=1")
    to(chief, "door_a")
    chief.command("close door")
    watcher.command("quit")


# ---------------------------------------------------------------- 3. sneak

def rolls(chief, value):
    """Fix every notice roll (percent, 0 to 99.9): a creature notices if the roll is below its chance."""
    check(f"dice fixed at {value}", chief.command(f"srolls notice:{value}"), "ROLLS True")


def stat(c):
    out = c.command("sstat")
    m = re.search(r"end (\d+)/(\d+) .*sneaking (\w+) fighting (\w+) grace (\d+)", out)
    return {"end": int(m.group(1)), "sneaking": m.group(3) == "True", "fighting": m.group(4) == "True", "grace": int(m.group(5))} if m else {}


def chance(grace, scout, creature, alert=False, adj=0):
    c = max(10, min(90, 50 - 5 * (grace - 5) + 5 * (creature - scout)))
    return max(5, min(100, c + (30 if alert else 0) + adj))


def sneaker(name, calling="scout", level=1):
    c = login(name, calling)
    c.command("sfill")
    return c


def step_sneak(chief):
    # The formula, at its edges.
    for args, want in (("9 1 1 0 0", 30), ("5 1 1 0 0", 50), ("15 1 1 0 0", 10), ("1 1 1 0 0", 70), ("1 1 10 0 0", 90), ("9 1 1 1 0", 60),
                       ("1 1 10 1 0", 100), ("9 5 1 0 0", 10), ("9 1 6 0 0", 55), ("9 1 1 0 -10", 20), ("9 1 1 0 -20", 10),
                       ("15 1 1 0 -20", 5), ("9 1 1 1 -20", 40), ("9 10 1 0 0", 10), ("0 1 1 0 0", 75)):
        check(f"notice chance for grace/scout/creature/alert/rank {args}", chief.command("schance " + args), f"CHANCE {want}")
    check("Precision Strike's ambush factors by rank", chief.command("sskill precision_strike ambush 4"), "SKILL 1.5 1.75 2 2.25")
    check("Sneak's Endurance a room by rank", chief.command("sskill sneak room 3"), "SKILL 5 4 3")
    check("Sneak's notice change by rank", chief.command("sskill sneak notice 3"), "SKILL 0 -10 -20")

    w = sneaker("snwar", "warrior")
    check("a warrior has no sneak command", w.command("sneak"), absent=("begin to move quietly",))
    w.command("quit")

    s = sneaker("snscout")
    g = stat(s)["grace"]
    check("sneak is a skill the Scout has, in the skills list", s.command("skills"), "sneak", "Sneak", "Moves quietly")
    out = s.command("sneak")
    check("sneak turns it on and says what a room costs", out, "You begin to move quietly. Each room you enter costs 5 Endurance.")
    check("the state shows", str(stat(s)["sneaking"]), "True")
    check("score shows it with the rank and the cost", s.command("score"), "You are sneaking (rank I): each room costs 5 Endurance")
    check("the prompt token {sneak}", s.command("prompt {sneak}> ") + s.command("look"), "(sneaking) >")
    s.command("prompt off")
    check("sneak again is already on", s.command("sneak on"), "You are already sneaking.")
    check("sneak off stops it", s.command("sneak off"), "You stop sneaking.")
    check("and is not sneaking", s.command("sneak off"), "You are not sneaking.")
    check("sneak with a stray word", s.command("sneak sideways"), "Sneak on, or sneak off.")
    check("score shows no sneaking now", s.command("score"), absent=("You are sneaking",))
    check("nothing is saved: the state is false after the stop", str(stat(s)["sneaking"]), "False")

    # Entry, without sneaking: the creature attacks as it always did.
    rolls(chief, "50.0")
    to(s, "hall")
    put(s, "lurk", "lurker")
    out = say(s, "lurk", 4.0)
    check("not sneaking: the lurker attacks", out, "attacks you")
    chief_slay(s)
    s.command("sfill")

    # Entry while sneaking, at the edges of the chance (30% for this Scout against a level 1 creature).
    c30 = chance(g, 1, 1)
    for label, room, creature_alert, roll, noticed in (
            (f"plain, roll just under {c30}%", "lurk", False, f"{c30 - 1}.9", True),
            (f"plain, roll exactly {c30}%", "lurk", False, f"{c30}.0", False),
            ("alert (+30): a roll the plain one misses", "sent", True, f"{c30}.0", True),
            ("alert, roll at its chance", "sent", True, f"{chance(g, 1, 1, True)}.0", False),
            ("a level 6 creature: +25", "giant", False, f"{chance(g, 1, 6) - 1}.9", True),
            ("a level 6 creature, at its chance", "giant", False, f"{chance(g, 1, 6)}.0", False),
            ("a Boss always notices, at the highest roll", "tyrant", False, "99.9", True)):
        rolls(chief, roll)
        to(s, "hall")
        s.command("sfill")
        s.command("sneak on")
        put(s, room, {"lurk": "lurker", "sent": "sentry", "giant": "giant", "tyrant": "tyrant"}[room])
        s.command("sres 60 0")
        out = s.command(room)
        st = stat(s)
        out += a.quiet(s, 4.0)
        if noticed:
            check(f"entry, {label}: it notices and attacks", out, "has noticed you!", "attacks you")
        else:
            check(f"entry, {label}: it does not notice", out, "has not noticed you.", absent=("attacks you", "noticed you!"))
        check(f"entry, {label}: the room cost 5 Endurance (60 to 55)", str(54 <= st["end"] <= 56), "True")
        st = stat(s)
        check(f"entry, {label}: sneaking is {'over' if noticed else 'still on'}", str(st["sneaking"]), str(not noticed))
        check(f"entry, {label}: the fight is {'on' if noticed else 'not begun'}", str(st["fighting"]), str(noticed))
        chief_slay(s)
        s.command("sneak off")

    # A room with nothing aggressive in it costs the same and nothing happens.
    rolls(chief, "0.0")
    to(s, "hall")
    s.command("sfill")
    s.command("sneak on")
    s.command("sres 60 0")
    s.command("empty")
    st = stat(s)
    check("an empty room: no roll, no fight, still sneaking, 5 Endurance", str(st["sneaking"]) + str(54 <= st["end"] <= 56) + str(st["fighting"]), "TrueTrueFalse")
    s.command("quit")


def stat_max(c):
    m = re.search(r"end \d+/(\d+)", c.command("sstat"))
    return int(m.group(1))


def put(c, room, *creatures):
    """Empty a test room of creatures and put fresh ones in it (the probe's own, so no room has to reset between cases)."""
    c.command("sclear " + T + "n_" + room)
    for x in creatures:
        c.command(f"sspawnin {T}n_{room} {x}")


def chief_slay(c):
    c.command("sslay")
    a.drain(c, 0.6)


# ---------------------------------------------------------------- 4. staying and leaving

def step_leave(chief):
    s = sneaker("snleave")
    g = stat(s)["grace"]
    c30 = chance(g, 1, 1)
    rolls(chief, "50.0")
    to(s, "hall")
    s.command("sneak on")
    put(s, "lurk", "lurker")
    say(s, "lurk", 1.5)
    check("an unaware lurker: still sneaking", str(stat(s)["sneaking"]), "True")
    # Leaving: the lurker rolls at the entry chance; a notice starts the fight and the Scout stays.
    rolls(chief, f"{c30 - 1}.9")
    before = stat(s)["end"]
    out = say(s, "out", 3.0)
    check("leaving: a lurker that notices starts the fight", out, "has noticed you!", "attacks you", absent=("Sneak Hall",))
    check("the Scout stays in the room", s.command("look"), "Sneak Lurk")
    check("no Endurance was spent on a room not left", str(stat(s)["end"] >= before), "True")
    check("sneaking is over", str(stat(s)["sneaking"]), "False")
    # Spotted: sneak is refused while that creature lives and the Scout stays.
    out = s.command("sneak")
    check("sneak is refused where a creature has seen you", out, "has seen you; there is no hiding from it here.")
    s.command("sfill")
    chief_slay(s)
    out = s.command("sneak")
    check("with the spotter dead it works again", out, "You begin to move quietly.")
    s.command("sneak off")
    # A fight refuses it too.
    s.command("sspawn dummy")
    s.command("kill dummy")
    check("sneak is refused in a fight", s.command("sneak"), "You cannot slip out of sight while you are fighting.")
    chief_slay(s)

    # Leaving at a roll the lurker fails: the Scout goes and pays.
    to(s, "hall")
    s.command("sfill")
    s.command("sneak on")
    rolls(chief, "50.0")
    put(s, "lurk", "lurker")
    say(s, "lurk", 1.5)
    start = stat(s)["end"]
    out = say(s, "out", 1.5)
    check("leaving past a lurker that does not notice: out in the hall", s.command("look"), "Sneak Hall")
    check("and the room cost 5 Endurance", str(stat(s)["end"] <= start - 5 + 1), "True")
    check("still sneaking", str(stat(s)["sneaking"]), "True")

    # While staying: a re-roll every 10 s at half the chance (c30/2).
    half = c30 // 2
    rolls(chief, "50.0")
    put(s, "lurk", "lurker")
    say(s, "lurk", 1.5)
    rolls(chief, f"{half}.0")  # not below half the chance: the creature stays unaware at 10 s although the full chance would catch it
    out = say(s, "look", 12.5)
    check("a re-roll at the half chance does not notice at a roll between half and full", out, absent=("noticed you!", "attacks you"))
    check("still sneaking", str(stat(s)["sneaking"]), "True")
    rolls(chief, f"{half - 1}.9")
    out = say(s, "look", 11.5)
    check("a roll below the half chance notices at the next re-roll", out, "has noticed you!", "attacks you")
    check("sneaking is over", str(stat(s)["sneaking"]), "False")
    chief_slay(s)

    # Two creatures: each must fail the leave roll.
    to(s, "hall")
    s.command("sfill")
    rolls(chief, "50.0")
    s.command("sneak on")
    put(s, "two", "lurker", "lurker")
    out = say(s, "two", 1.5)
    check("two lurkers, both unaware", out, "has not noticed you.", absent=("attacks you",))
    # The dice are one number for every roll, so one noticing is as good as both for this check: the point is that a notice from any starts the fight.
    rolls(chief, f"{c30 - 1}.9")
    out = say(s, "out", 3.0)
    check("two lurkers: a notice from them starts the fight and the Scout stays", out, "has noticed you!", "attacks you")
    check("in the room still", s.command("look"), "Sneak Two")
    chief_slay(s)

    # Too tired to sneak a room.
    to(s, "hall")
    s.command("sfill")
    rolls(chief, "50.0")
    s.command("sneak on")
    s.command("sres 3 0")
    out = say(s, "empty", 1.0)
    check("too tired: sneaking ends and the Scout goes on openly", out, "You are too tired to keep sneaking, and go on openly.", "Sneak Empty")
    check("and is no longer sneaking", str(stat(s)["sneaking"]), "False")
    s.command("quit")


# ---------------------------------------------------------------- 5. loud and quiet

def step_loud(chief):
    s = sneaker("snloud")
    rolls(chief, "50.0")

    s.command("slevel 2")  # shout needs level 2

    def fresh(room="lurk", n=1):
        chief_slay(s)
        to(s, "hall")
        s.command("sfill")
        s.command("sneak on")
        put(s, room, *(["lurker"] * n))
        return say(s, room, 1.2)

    # kill and strike start a fight with one lurker: the second, which had not noticed, wakes and attacks too.
    for label, room, line, n in (("say", "lurk", "say hello", 1), ("shout", "lurk", "shout hello", 1), ("kill", "lurk", "kill lurker", 2),
                                 ("a skill that is not Tracking (strike)", "lurk", "strike lurker", 2), ("opening a door", "ndoor", "open door", 1)):
        fresh(room, n)
        check(f"{label}: sneaking was on", str(stat(s)["sneaking"]), "True")
        out = say(s, line, 4.0)
        check(f"{label}: it is loud, sneaking ends", out, "You are no longer sneaking.")
        check(f"{label}: and the room wakes: the lurker attacks", out, "attacks you")
        check(f"{label}: the state is off", str(stat(s)["sneaking"]), "False")
    to(s, "hall")
    s.command("ndoor")
    s.command("close door")
    s.command("out")

    # Picking up from the floor and from a corpse, and searching a chest.
    for label, setup, line in (("get from the floor", "floor", "get evensword"), ("get from a corpse", "corpse", "loot"), ("looting a chest", "chest", "get chest")):
        chief_slay(s)
        to(s, "hall")
        s.command("sfill")
        s.command("sneak off")
        room = "lurk"
        if setup == "corpse":
            # A corpse that holds something, in the morgue; then a lurker is brought in, and the Scout slips in while it is unaware.
            to(s, "n_morgue")
            s.command("sspawn lootdummy")
            for _ in range(6):
                s.command("kill lootdummy")
                if "dies" in a.quiet(s, 5.0) or not stat(s)["fighting"]:
                    break
            s.command("out")
            s.command("sspawnin " + T + "n_morgue lurker")
            room = "morgue"
        s.command("sneak on")
        if setup != "corpse": put(s, "lurk", "lurker")
        say(s, room, 1.0)
        if setup == "floor":
            s.command("sclone /domains/sneaktest/sword")
        if setup == "chest":
            s.command("sclone /domains/sneaktest/chest")
        check(f"{label}: sneaking was on in a room with a lurker", str(stat(s)["sneaking"]), "True")
        out = say(s, line, 4.0)
        check(f"{label}: it is loud", out, "You are no longer sneaking.")
        check(f"{label}: the lurker attacks", out, "attacks you")
    chief_slay(s)

    # Quiet: look, rest, tell, Tracking.
    fresh("lurk")
    out = say(s, "look", 1.0) + say(s, "rest", 1.0) + say(s, "tell snchief hello", 1.0) + say(s, "stand", 0.5)
    check("look, rest, tell and stand are quiet", out, absent=("no longer sneaking", "attacks you"))
    check("still sneaking", str(stat(s)["sneaking"]), "True")
    out = say(s, "track", 3.0)
    check("Tracking is quiet", out, absent=("no longer sneaking", "attacks you"))
    check("still sneaking after Tracking", str(stat(s)["sneaking"]), "True")
    chief_slay(s)
    s.command("quit")


# ---------------------------------------------------------------- 6. the opener

def first_action(c, line, name, limit=16.0):
    a.drain(c, 0.4)
    data = ""
    start = time.time()
    c.send(line)
    pattern = r"(?im)^(?!.*attacks you)(?:the |a )?" + name + r" .*\byou\b|you dodge (?:the |a )?" + name
    while time.time() - start < limit:
        data += a.quiet(c, 0.05)
        if re.search(pattern, data):
            return time.time() - start, data
    if os.environ.get("SNEAK_DEBUG"):
        print("      no action seen:", repr(data[-500:]))
    return None, data


def landed(text):
    return [int(x) for x in re.findall(r"\(\+(\d+) damage\)", text)]


def step_ambush(chief):
    s = sneaker("snambush")
    s.command("verbose on")  # damage numbers in the blow lines
    s.command("sgive sword")
    s.command("sgive bow")
    s.command("wield evensword")
    rolls(chief, "50.0")
    # Surprise: the target's first action waits twice as long (Jab: Reaction 2.0, 4 s; 8 s surprised).
    to(s, "n_jabroom")
    s.command("sspawn jab")
    secs, data = first_action(s, "kill jab", "jab")
    check("an ordinary fight: the target's first action comes at Reaction x 2 (4 s)", str(secs is not None and 3.4 <= secs <= 4.9), "True")
    chief_slay(s)
    s.command("out")
    to(s, "n_jabroom")
    s.command("sfill")
    s.command("sneak on")
    s.command("sspawn jab")
    secs, data = first_action(s, "kill jab", "jab")
    print("      surprised first action (seconds):", secs)
    check("begun unnoticed: the target is surprised, its first action doubles (8 s)", str(secs is not None and 7.4 <= secs <= 9.4), "True")
    chief_slay(s)
    # With a bow: the ranged opening is kept (2 s) and the surprise added.
    s.command("wield evenbow")
    to(s, "n_jabroom")
    s.command("sfill")
    s.command("sneak on")
    s.command("sspawn jab")
    secs, data = first_action(s, "kill jab", "jab")
    print("      bow, surprised first action (seconds):", secs)
    check("a bow from hiding: 4 s + 2 s ranged opening + 4 s surprise (10 s)", str(secs is not None and 9.3 <= secs <= 11.4), "True")
    chief_slay(s)

    # Damage of the opener: Precision Strike x1.5 from hiding (10 x 1.5 = 15), 10 otherwise; Aimed Shot keeps its opener (x2.5).
    s.command("wield evensword")

    def blows(prep, line, sneak, tries=8):
        got = []
        for _ in range(tries):
            to(s, "n_jabroom")
            s.command("sfill")
            if sneak:
                s.command("sneak on")
            s.command("sspawn jab")
            a.drain(s, 0.3)
            s.send(line)
            text = ""
            end = time.time() + 6
            while time.time() < end and not landed(text) and "You miss" not in text:
                text += a.quiet(s, 0.25)
            got += landed(text)
            chief_slay(s)
            s.command("out")
            if len(got) >= 3:
                break
        return got
    plain = blows("strike", "strike jab", False)
    hidden = blows("strike", "strike jab", True)
    print("      strike damage plain / from hiding:", plain, hidden)
    check("Precision Strike not from hiding: the weapon's 10 (a critical may add)", str(bool(plain) and min(plain) == 10), "True")
    check("Precision Strike as the blow that starts the fight from hiding: x1.5 (15)", str(bool(hidden) and min(hidden) == 15), "True")
    s.command("wield evenbow")
    aimed = blows("aim", "aim jab", True)
    print("      aimed shot from hiding:", aimed)
    check("Aimed Shot from hiding keeps its own opener (x2.5 = 25)", str(bool(aimed) and min(aimed) == 25), "True")
    s.command("quit")


# ---------------------------------------------------------------- 7. ranks

def step_ranks(chief):
    s = sneaker("snrank")
    to(s, VILLAGE + "strung_yew")
    check("Wenna offers Sneak II", s.command("ranks"), "Wenna Farrow teaches", "Sneak II")
    s.command("slevel 5")
    s.command("sgold 500")
    out = s.command("train sneak")
    check("Wenna teaches Sneak II", out, "You pay Wenna Farrow", "Sneak II")
    check("rank II: room cost 4", s.command("sneak"), "Each room you enter costs 4 Endurance.")
    check("score shows rank II and the notice change", s.command("score"), "You are sneaking (rank II): each room costs 4 Endurance, and creatures notice you 10 points less often")
    check("Wenna teaches no further", s.command("train sneak"), "no further than rank II")
    s.command("sneak off")
    g = stat(s)["grace"]
    c = chance(g, 5, 6, False, -10)   # a level 6 creature: the chance is not at its floor
    for roll, noticed in ((f"{c - 1}.9", True), (f"{c}.0", False)):
        rolls(chief, roll)
        to(s, "hall")
        s.command("sfill")
        s.command("sneak on")
        put(s, "giant", "giant")
        s.command("sres 60 0")
        out = s.command("giant")
        st = stat(s)
        out += a.quiet(s, 3.0)
        check(f"rank II at roll {roll} (chance {c}%): {'noticed' if noticed else 'unnoticed'}", out, "has noticed you!" if noticed else "has not noticed you.")
        check("rank II room cost 4 (60 to 56)", str(55 <= st["end"] <= 57), "True")
        chief_slay(s)
        s.command("sneak off")
    # Rank III from the Scout hallmaster.
    s.command("slevel 10")
    s.command("sgold 2000")
    to(s, HALL)
    out = s.command("ranks")
    check("Veyla offers Sneak III", out, "Veyla", "Sneak III")
    out = s.command("train sneak")
    check("Veyla teaches Sneak III", out, "Sneak III")
    check("rank III: room cost 3", s.command("sneak"), "Each room you enter costs 3 Endurance.")
    check("score shows rank III", s.command("score"), "You are sneaking (rank III): each room costs 3 Endurance, and creatures notice you 20 points less often")
    s.command("sneak off")
    g = stat(s)["grace"]
    c = chance(g, 10, 1, True, -20)   # an Alert creature: the chance is not at its floor
    for roll, noticed in ((f"{c - 1}.9", True), (f"{c}.0", False)):
        rolls(chief, roll)
        to(s, "hall")
        s.command("sfill")
        s.command("sneak on")
        put(s, "sent", "sentry")
        s.command("sres 60 0")
        out = s.command("sent")
        st = stat(s)
        out += a.quiet(s, 3.0)
        check(f"rank III at roll {roll} (chance {c}%)", out, "has noticed you!" if noticed else "has not noticed you.")
        check("rank III room cost 3 (60 to 57)", str(56 <= st["end"] <= 58), "True")
        chief_slay(s)
        s.command("sneak off")
    # Rank I Scout cannot buy from Veyla before II.
    s2 = sneaker("snranktwo")
    s2.command("slevel 10")
    s2.command("sgold 2000")
    to(s2, HALL)
    check("Veyla does not teach rank II", s2.command("train sneak"), "does not teach")
    s2.command("quit")
    s.command("quit")


# ---------------------------------------------------------------- 8. probes

def step_sneakprobe(chief, mudlib):
    h.write(mudlib, "/wiz/snchief/snmeddler", SNEAK_MEDDLER)
    h.write(mudlib, "/wiz/snchief/wizlurker", WIZ_MONSTER)
    h.write(mudlib, "/wiz/snchief/wizlair", WIZ_LAIR)
    s = sneaker("snprobe")
    rolls(chief, "50.0")
    to(s, "hall")
    s.command("sneak on")
    put(s, "lurk", "lurker")
    say(s, "lurk", 1.5)
    start = stat(s)
    h.write(mudlib, "/wiz/snchief/wroom", WIZ_ROOM)
    chief.command("goto ~/wroom")  # CHANGED (sandbox): a wizard's code runs in their workshop, and reaches the Scout from there
    check("the meddler is cloned", chief.command("clone ~/snmeddler"), "snmeddler")
    out = chief.command("smeddle snprobe")
    to(chief, "n_lurk")
    check("the meddler ran", out, "MEDDLE ")
    # CHANGED (sandbox): the calls on the Scout no longer reach it at all (nothing comes back: 0, False).
    check("a wizard's object is no creature: it cannot ask whether the Scout is noticed", out, "Notice=0")
    check("it cannot set a creature on the Scout", out, "Engage=called")
    check("nor make the Scout depart with a roll or a cost", out, "Departs=False")
    check("nor end the sneaking by Disturbed", out, "Disturbed=called", "Sneaking=True")
    check("nor fix or free the dice", out, "Rolls=False", "RollsRandom=False", "RollsNow=notice:50.0")
    check("Surprising is false outside a fight being begun", out, "Surprising=False")
    out = a.quiet(s, 3.5)
    check("no creature attacked the Scout for the meddler", out, absent=("attacks you", "noticed you"))
    after = stat(s)
    check("the Scout is still sneaking in the same room, nothing spent", str(after["sneaking"]) + str(after["end"] >= start["end"]) + str(after["fighting"]), "TrueTrueFalse")
    chief_slay(s)

    # A wizard's own creature, in a wizard's own room, is a creature that has no standing to be rolled for: it always notices.
    to(chief, "hall")
    s.command("sneak off")
    to(s, "hall")
    s.command("sfill")
    s.command("sneak on")
    # CHANGED (sandbox): a player cannot enter a wizard's workshop at all, so never meets a wizard's creature.
    out = s.command("stele /wiz/snchief/wizlair") + a.quiet(s, 0.5)
    check("a Scout cannot be brought into a wizard's lair", out, "Only wizards can enter a wizard's workshop.")
    check("and is in no fight", str(stat(s)["fighting"]), "False")
    s.command("sslay")
    s.command("quit")


def run(work, mudlib):
    chief = h.Client("snchief", archetype="adventurer")
    chief.read_until("> ")
    check("the secret makes the first wizard", chief.command("promote " + h.SECRET), "level 10 wizard")
    a.drain(chief, 0.4)
    chief.command("down")
    only = [x for x in os.environ.get("SNEAK_STEPS", "").split(",") if x]
    steps = (("doors", lambda: step_doors(chief)), ("doorworld", lambda: step_door_world(chief)), ("doorprobe", lambda: step_doorprobe(chief, mudlib)))
    steps += extra_steps(chief, mudlib)
    for name, step in steps:
        if not only or name in only:
            step()
    return chief


def extra_steps(chief, mudlib):
    return (("sneak", lambda: step_sneak(chief)), ("leave", lambda: step_leave(chief)), ("loud", lambda: step_loud(chief)),
            ("ambush", lambda: step_ambush(chief)), ("ranks", lambda: step_ranks(chief)), ("sneakprobe", lambda: step_sneakprobe(chief, mudlib)))


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-sneak-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    for gone in ("crafting/launch_materials.cs", "recipes/launch.cs"):
        if os.path.exists(os.path.join(mudlib, gone)):
            os.remove(os.path.join(mudlib, gone))
    os.makedirs(os.path.join(mudlib, "domains", "sneaktest"), exist_ok=True)
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
