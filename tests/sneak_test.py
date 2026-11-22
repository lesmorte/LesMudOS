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
        foreach (var v in new[] { "sgold", "slevel", "sspawn", "stele", "sstat", "shurt", "sfill", "sslay", "sgive", "sdoor", "srolls", "sres", "sclone", "ssneak", "sdrop" })
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
    f[T + "hall"] = droom("SnHall", "Sneak Hall", {"up": VILLAGE + "square", "door_a": T + "door_a", "lock_a": T + "lock_a", "reset_a": T + "reset_a", "stay_a": T + "stay_a", "pat_a": T + "pat_a"}, "",
                          rk.HALL_INIT.replace("/domains/ranktest/probe", T + "probe"))
    f.update(sneak_files())
    return f


def sneak_files():
    return {}


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
    check("the meddler is cloned", chief.command("clone ~/meddler"), "meddler")
    out = chief.command("meddle")
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
    return ()


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
