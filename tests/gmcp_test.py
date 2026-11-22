#!/usr/bin/env python3
"""End-to-end test of GMCP (docs/specs/gmcp.md, section 4).

Starts the driver against a temporary copy of the mudlib and plays characters over TCP with a client that speaks telnet
option 201 (it answers IAC DO to the server's offer, reads IAC SB framing, and sends Core.* messages), beside clients that do
not: one that refuses (IAC DONT), one that never answers. Checks: the offer and the byte stream of a client without GMCP;
Room.Info on every move, with room numbers that are the same after a server restart (the driver is stopped and started again
inside the test); a hidden exit absent until that character finds it, and only for that character; a door's state; Char.Vitals
after damage, rest, a potion, a fight, and coalescing; Char.Status after a level; Comm.Channel.Text for a tell, a shout and a
channel line; Core.Supports and Core.Ping; malformed, oversized and flooding incoming subnegotiations; and a wizard's probe
object that tries to send GMCP, call the service, read what it must not and assign room numbers.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=5020 python3 tests/gmcp_test.py
"""
import json
import os
import re
import shutil
import socket
import subprocess
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "4841")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402

IAC, SB, SE, WILL, WONT, DO, DONT = 255, 250, 240, 251, 252, 253, 254
GMCP = 201
CHECKS = [0]
EXAMPLES = {}


def check(label, text, *expected, absent=()):
    CHECKS[0] += 1
    h.check(label, text, *expected, absent=absent)


def truth(label, condition, detail=""):
    CHECKS[0] += 1
    h.check(label, str(bool(condition)) + ("" if condition else " " + str(detail)), "True")


def message(package, payload=None):
    """One GMCP message as a client sends it: IAC SB 201 package SP json IAC SE, every 255 doubled."""
    body = package.encode() + (b" " + payload if isinstance(payload, bytes) else (b" " + json.dumps(payload).encode() if payload is not None else b""))
    return bytes([IAC, SB, GMCP]) + body.replace(b"\xff", b"\xff\xff") + bytes([IAC, SE])


def split_gmcp(data):
    """Take a byte stream apart: (the bytes that are not GMCP, [(package, parsed json or raw)], bytes left over, offers seen).
    The server's offer (IAC WILL 201) and every IAC SB 201 ... IAC SE message are removed; all other bytes are kept as they were."""
    out = bytearray()
    frames = []
    offers = 0
    i, n = 0, len(data)
    while i < n:
        b = data[i]
        if b != IAC:
            out.append(b)
            i += 1
            continue
        if i + 1 >= n:
            return bytes(out), frames, data[i:], offers
        c = data[i + 1]
        if c == IAC:
            out += b"\xff\xff"
            i += 2
        elif c in (WILL, WONT, DO, DONT):
            if i + 2 >= n:
                return bytes(out), frames, data[i:], offers
            if c == WILL and data[i + 2] == GMCP:
                offers += 1
            else:
                out += data[i:i + 3]
            i += 3
        elif c == SB:
            j = i + 2
            body = bytearray()
            done = False
            while j < n:
                if data[j] == IAC:
                    if j + 1 >= n:
                        break
                    if data[j + 1] == IAC:
                        body.append(IAC)
                        j += 2
                        continue
                    if data[j + 1] == SE:
                        done = True
                        j += 2
                        break
                body.append(data[j])
                j += 1
            if not done:
                return bytes(out), frames, data[i:], offers
            if body and body[0] == GMCP:
                name, _, rest = bytes(body[1:]).partition(b" ")
                try:
                    value = json.loads(rest.decode("utf-8")) if rest else None
                except ValueError:
                    value = rest
                frames.append((name.decode(), value))
            else:
                out += data[i:j]
            i = j
        else:
            out += data[i:i + 2]
            i += 2
    return bytes(out), frames, b"", offers


class GClient(h.Client):
    """The shared test client, taught telnet option 201. answer: "do" (a GMCP client), "dont" (a client without GMCP, like the shared
    one) or None (never answers the offer). supports: what a GMCP client lists in Core.Supports.Set at once (None sends none)."""

    def __init__(self, name, answer="do", supports=("Char 1", "Room 1", "Comm.Channel 1"), **kw):
        self.answer = answer
        self.supports = supports
        self.frames = []          # (package, value), in the order they arrived
        self.stream = b""         # every byte received
        self.clean = b""          # the same without the offer and the GMCP messages
        self.offers = 0
        self._g = b""
        super().__init__(name, **kw)

    def rx(self, chunk):
        self.stream += chunk
        data = self._g + chunk
        text, frames, self._g, offers = split_gmcp(data)
        self.clean += text
        self.frames.extend(frames)
        for _ in range(offers):
            self.offers += 1
            try:
                if self.answer == "do":
                    self.sock.sendall(bytes([IAC, DO, GMCP]))
                    self.sock.sendall(message("Core.Hello", {"client": "gmcp_test", "version": "1"}))
                    if self.supports is not None:
                        self.sock.sendall(message("Core.Supports.Set", list(self.supports)))
                elif self.answer == "dont":
                    self.sock.sendall(bytes([IAC, DONT, GMCP]))
            except OSError:
                pass
        return super().rx(text)

    # ---- frames ----
    def mark(self):
        return len(self.frames)

    def of(self, package, since=0):
        return [v for p, v in self.frames[since:] if p == package]

    def pump(self, seconds=0.3):
        """Take in whatever arrives for a while; the text is dropped, the GMCP messages are kept."""
        end = time.time() + seconds
        while time.time() < end:
            try:
                chunk = self.sock.recv(65536)
            except socket.timeout:
                continue
            except OSError:
                return
            if not chunk:
                return
            self.rx(chunk)

    def wait(self, package, predicate=lambda v: True, since=0, timeout=5.0):
        """The first message of the package after `since` for which the predicate holds, waiting for it; None if it never comes."""
        end = time.time() + timeout
        while True:
            for p, v in self.frames[since:]:
                if p == package and predicate(v):
                    return v
            if time.time() > end:
                return None
            self.pump(0.1)

    def quiet(self, package, since=0, seconds=0.8):
        """No message of this package arrives within a while."""
        self.pump(seconds)
        return not self.of(package, since)

    def room(self, since=0, **fields):
        return self.wait("Room.Info", lambda v: all(v.get(k) == x for k, x in fields.items()), since)

    def raw_send(self, data):
        self.sock.sendall(data)


SQUARE = "/domains/hollow/village/square"
HUB = '''// @inherit /std/room
public class GmcpHub : Room
{
    public override void Create()
    {
        ShortDesc = "The Test Hub"; LongDesc = "A hub for the GMCP test.";
        Exits["north"] = "/domains/gmcptest/hall";
        Exits["lane"] = "/domains/gmcptest/lane_end";
        Exits["east"] = "/domains/gmcptest/vault";
        AddDoor("east", "door");
        AddDetail("crack", "A thin crack in the wall.");
        HiddenExit("secret", "/domains/gmcptest/secret_room", "crack", 100);
    }
}
'''
HALL = '''// @inherit /std/room
public class GmcpHall : Room
{
    public override void Create()
    {
        ShortDesc = "The Test Hall"; LongDesc = "A hall below the hub.";
        Terrain = "underground";
        Exits["south"] = "/domains/gmcptest/hub";
    }
}
'''
LANE = '''// @inherit /std/room
public class GmcpLane : Room
{
    public override void Create()
    {
        ShortDesc = "The End of the Lane"; LongDesc = "A lane that ends.";
        Exits["out"] = "/domains/gmcptest/hub";
    }
}
'''
VAULT = '''// @inherit /std/room
public class GmcpVault : Room
{
    public override void Create()
    {
        ShortDesc = "The Test Vault"; LongDesc = "A small room behind a door.";
        Exits["west"] = "/domains/gmcptest/hub";
        AddDoor("west", "door");
    }
}
'''
SECRET = '''// @inherit /std/room
public class GmcpSecret : Room
{
    public override void Create()
    {
        ShortDesc = "The Secret Room"; LongDesc = "Found by those who looked.";
        Exits["out"] = "/domains/gmcptest/hub";
    }
}
'''
NEWROOM = '''// @inherit /std/room
public class GmcpNew : Room
{
    public override void Create()
    {
        ShortDesc = "A Room Made After the Restart"; LongDesc = "New.";
        Exits["out"] = "/domains/gmcptest/hub";
    }
}
'''
WIZROOM = '''// @inherit /std/room
public class MinaHall : Room
{
    public override void Create()
    {
        ShortDesc = "Mina's Workshop"; LongDesc = "Sawdust and half-built rooms.";
        Exits["out"] = "/domains/gmcptest/hub";
    }
}
'''
# What a low-level wizard could write: an object that tries to send GMCP, to use the service, and to read or assign what it must not.
PROBE = '''// @inherit /std/item
public class GmcpProbe : Item
{
    public override void Create() { Name = "probe"; }
    public override void Init() { AddAction("gprobe", Run); }

    private static string Show(object? o) => o == null ? "null" : o is string s ? (s == "" ? "empty" : s.Replace("\\n", " ")) : o.ToString() ?? "?";

    private bool Run(string args)
    {
        ObjectRef? t = null;
        foreach (var u in Users()) if ((u.Call<string>("QueryName") ?? "").ToLower() == args.Trim().ToLower()) t = u;
        if (t == null) { Write("probe: no such player\\n"); return true; }
        try
        {
            GmcpSend(t, "Comm.Channel.Text", new Dictionary<string, object?> { ["channel"] = "tell", ["talker"] = "Boss", ["text"] = "PROBE-FORGED" });
            Write("send: SENT\\n");
        }
        catch (MudRuntimeException e) { Write("send: refused " + e.Message + "\\n"); }
        try { GmcpSend(ThisPlayer()!, "Room.Info", new Dictionary<string, object?> { ["num"] = 1 }); Write("self: SENT\\n"); }
        catch (MudRuntimeException e) { Write("self: refused " + e.Message + "\\n"); }
        Write("active: " + Show(GmcpActive(t)) + "\\n");
        var daemon = LoadObject("/secure/gmcp");
        Write("channel: " + Show(daemon.Call("Channel", t, "tell", "Boss", "PROBE-FORGED")) + "\\n");
        Write("poll: " + Show(daemon.Call("Poll", t)) + "\\n");
        Write("look: " + Show(daemon.Call("Look", t)) + "\\n");
        Write("facts: " + Show(t.Call("GmcpFacts")) + "\\n");
        var room = Environment(t);
        Write("roomfacts: " + (room == null ? "noroom" : Show(room.Call("GmcpRoomFacts", t))) + "\\n");
        var ids = LoadObject("/secure/roomids");
        Write("assign: " + Show(ids.Call("Assign", "/wiz/mina/probe_room")) + "\\n");
        Write("query: " + Show(ids.Call("QueryNumber", "/wiz/mina/probe_room")) + "\\n");
        return true;
    }
}
'''
TEST_ACTIONS = ('        AddAction("lv", a => { Level = int.Parse(a); return true; });\n'
                '        AddAction("tp", a => { MoveObject(LoadObject(a)); Look(); return true; });\n'
                '        AddAction("hurt", a => { TakeDamage(int.Parse(a), null); return true; });\n'
                '        AddAction("xp", a => { AddXp(int.Parse(a)); return true; });\n'
                '        AddAction("gold", a => { Gold += int.Parse(a); return true; });\n'
                '        AddAction("mk", a => { MoveObject(CloneObject(a), Self); return true; });\n'
                '        AddAction("spawn", a => { MoveObject(CloneObject(a), Environment()!); return true; });\n')


def patch_player(mudlib):
    path = os.path.join(mudlib, "std", "player.cs")
    src = open(path).read()
    anchor = '        AddAction("channels", a => ToChannels("Listing", ""));'
    assert anchor in src
    open(path, "w").write(src.replace(anchor, TEST_ACTIONS + anchor, 1))


def start_driver(work, mudlib, log):
    driver_dll = os.path.join(h.ROOT, "src", "LesMudOS.Driver", "bin", "Debug", "net10.0", "LesMudOS.Driver.dll")
    driver = subprocess.Popen(
        ["dotnet", driver_dll, "--mudlib", mudlib, "--save", os.path.join(work, "save"), "--port", str(h.PORT)],
        stdout=log, stderr=subprocess.STDOUT, env={**os.environ, "LESMUDOS_ADMIN_SECRET": h.SECRET})
    for _ in range(150):
        try:
            socket.create_connection(("127.0.0.1", h.PORT), timeout=0.3).close()
            return driver
        except OSError:
            time.sleep(0.2)
    raise SystemExit("the driver did not start")


def stop_driver(driver):
    driver.terminate()
    try:
        driver.wait(30)
    except subprocess.TimeoutExpired:
        driver.kill()
        driver.wait()


def login(name, **kw):
    c = GClient(name, **kw)
    c.read_until("> ")
    c.pump(0.6)
    return c


def example(label, value):
    EXAMPLES.setdefault(label, value)


def lua_value(value):
    if isinstance(value, dict):
        return "{" + ", ".join(f"[{json.dumps(k)}] = {lua_value(v)}" for k, v in value.items()) + "}"
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, str):
        return json.dumps(value, ensure_ascii=False)
    return str(value)


def capture_village(path, moves=45):
    """Not part of the checks: with GMCP_CAPTURE=<file> a new character walks the village, depth first and back again, and the
    Room.Info messages the game sent are written as a Lua fixture for clients/mudlet/test/map_test.lua (real data, real exits)."""
    walker = login("Walker")
    first = walker.wait("Room.Info")
    visited = {first["num"]}
    blocked = set()
    stack = [first]
    taken = 0
    while stack and taken < moves:
        here = stack[-1]
        options = [(n, t) for n, t in sorted(here["exits"].items()) if t not in visited and (here["num"], n) not in blocked]
        if options:
            name, target = options[0]
            m = walker.mark()
            walker.command(name)
            taken += 1
            got = walker.wait("Room.Info", lambda v: v["num"] != here["num"], m, timeout=2.0)
            if got is None or got["num"] != target:
                blocked.add((here["num"], name))
                if got is not None and got["num"] != here["num"]:
                    stack.append(got)
                    visited.add(got["num"])
                continue
            visited.add(target)
            stack.append(got)
            continue
        stack.pop()
        if not stack:
            break
        parent = stack[-1]
        back = next((n for n, t in sorted(here["exits"].items()) if t == parent["num"]), None)
        if back is None:
            break
        m = walker.mark()
        walker.command(back)
        taken += 1
        walker.wait("Room.Info", lambda v: v["num"] == parent["num"], m, timeout=2.0)
    frames = [v for p, v in walker.frames if p == "Room.Info"]
    with open(path, "w") as f:
        f.write("-- Room.Info messages as the game sent them to a character who walked the village (tests/gmcp_test.py with GMCP_CAPTURE).\n")
        f.write("return {\n" + "".join("  " + lua_value(v) + ",\n" for v in frames) + "}\n")
    walker.send("quit")
    print(f"captured {len(frames)} Room.Info messages, {len(visited)} rooms, to {path}")


def run(work, mudlib, logpath, driver_box):
    # ================= the offer, and a client without GMCP =================
    warden = login("Warden", answer="dont")
    check("the secret makes the first wizard", warden.command("promote " + h.SECRET), "level 10 wizard")
    truth("the offer is sent once, first, to a client that has not answered", warden.offers == 1 and warden.stream.startswith(bytes([IAC, WILL, GMCP])), warden.stream[:20])
    truth("a client that answered DONT is sent no GMCP message at all", b"\xff\xfa\xc9" not in warden.stream)
    warden.command("look")
    warden.command("tp /domains/gmcptest/hub")
    warden.command("hurt 2")
    truth("a client that answered DONT got no GMCP message after play either", warden.frames == [] and b"\xff\xfa" not in warden.stream)
    truth("the only IAC bytes it was sent are the offer and the password echo codes", set(re.findall(rb"\xff[\xfb-\xfe].", warden.stream)) <= {bytes([IAC, WILL, GMCP]), bytes([IAC, WILL, 1]), bytes([IAC, WONT, 1])}, set(re.findall(rb"\xff[\xfb-\xfe].", warden.stream)))
    hank = login("Hank", answer=None)
    hank.command("look")
    truth("a client that never answers is sent no GMCP message either", hank.offers == 1 and hank.frames == [] and b"\xff\xfa" not in hank.stream)

    # ---- the same play, with and without GMCP, gives the same text ----
    # (in the test rooms only: the village's people chatter at random, and the two texts would differ by that)
    script = ["look", "north", "south", "lane", "out", "east", "open door", "close door", "say hello there", "rest", "stand", "look"]
    texts = {}
    for name, answer in (("Gina", "do"), ("Gwen", "dont")):
        c = login(name, answer=answer)
        c.command("tp /domains/gmcptest/hub")
        c.pump(0.5)
        start = len(c.clean)
        for line in script:
            c.command(line)
        c.pump(0.5)
        # The sync token differs by call, and the numbers in the prompt move with the clock (regeneration): nothing else is touched.
        texts[name] = re.sub(rb"\(\d+/\d+\)", b"(N/N)", re.sub(rb"\[sync q\w+\]", b"[sync]", c.clean[start:].replace(name.encode(), b"NAME")))
        if answer == "do":
            truth("Gina (GMCP) received GMCP messages while she played", len(c.frames) > 5, len(c.frames))
        c.send("quit")
        c.closed(5)
    first = next((i for i, (x, y) in enumerate(zip(texts["Gina"], texts["Gwen"])) if x != y), min(len(texts["Gina"]), len(texts["Gwen"])))
    truth("what a GMCP client sees as text is, byte for byte, what a client without GMCP sees", texts["Gina"] == texts["Gwen"],
          (len(texts["Gina"]), len(texts["Gwen"]), first, texts["Gina"][max(0, first - 80):first + 120], texts["Gwen"][max(0, first - 80):first + 120]))
    truth("that text is not empty", len(texts["Gina"]) > 500, len(texts["Gina"]))

    if os.environ.get("GMCP_CAPTURE"):
        capture_village(os.environ["GMCP_CAPTURE"])
        return

    # ================= a GMCP client: login =================
    alice = login("Alice")
    bob = login("Bob", answer="dont")
    carol = login("Carol")
    mina = login("Mina")
    check("Mina becomes a level 1 wizard", warden.command("promote mina 1"), "mina is now wizard level 1")
    mina.pump(0.5)
    truth("Alice's client got the offer and her Core.Hello and Core.Supports.Set were taken", alice.offers == 1)
    status = alice.wait("Char.Status")
    vitals = alice.wait("Char.Vitals")
    info = alice.wait("Room.Info")
    check("Char.Status on login", json.dumps(status, sort_keys=True), '"name": "Alice"', '"level": 1', '"gold"', '"points_to_place"', '"calling"', '"experience"', '"next_level"')
    truth("Char.Status has exactly the specified fields", status and set(status) == {"name", "calling", "level", "experience", "next_level", "gold", "points_to_place"}, status)
    truth("Char.Vitals has the specified fields and the aliases", vitals and set(vitals) == {"vitality", "maxvitality", "endurance", "maxendurance", "focus", "maxfocus", "resting", "sneaking", "fighting", "hp", "maxhp", "mp", "maxmp"}, vitals)
    truth("Char.Vitals: hp is vitality, mp is focus", vitals and vitals["hp"] == vitals["vitality"] and vitals["mp"] == vitals["focus"] and vitals["maxhp"] == vitals["maxvitality"], vitals)
    truth("Room.Info on login is the start room", info and info["name"] and info["num"] > 0 and info["environment"] in ("outdoors", "indoors", "underground", "road", "water"), info)
    truth("Room.Info has exactly the specified fields", info and set(info) == {"num", "name", "area", "environment", "exits", "doors"}, info)
    example("Char.Status", status)
    example("Char.Vitals", vitals)
    example("Room.Info (start room)", info)
    start_num = info["num"]
    start_area = info["area"]
    truth("the start room has an area", start_area and start_area != "Elsewhere", start_area)

    # ================= Room.Info on every move, stable numbers =================
    walk = {}
    for command, name in (("tp /domains/gmcptest/hub", "The Test Hub"), ("north", "The Test Hall"), ("south", "The Test Hub"), ("lane", "The End of the Lane"), ("out", "The Test Hub")):
        m = alice.mark()
        alice.command(command)
        got = alice.room(m, name=name)
        truth(f"Room.Info after {command!r} names {name}", got is not None)
        if got:
            walk.setdefault(name, got["num"])
            truth(f"{name}: the number is the same each time", walk[name] == got["num"], (walk[name], got["num"]))
            example("Room.Info (hub, door closed)", got) if name == "The Test Hub" else None
    hub = alice.room(name="The Test Hub")
    truth("hub: exits are north, lane, east (named exits under their own names), no hidden exit yet", hub and set(hub["exits"]) == {"north", "lane", "east"}, hub)
    truth("hub: environment defaults to outdoors", hub and hub["environment"] == "outdoors", hub)
    truth("hub: a door's exit is present and marked in doors", hub and hub["doors"] == {"east": "closed"}, hub)
    m = alice.mark()
    alice.command("north")
    hall = alice.room(m, name="The Test Hall")
    truth("hall: its own Terrain is sent", hall and hall["environment"] == "underground" and hall["exits"] == {"south": hub["num"]}, hall)
    truth("hub's exit number for north is the hall's number", hub["exits"]["north"] == hall["num"], (hub, hall))
    truth("the area of a room with no area file is a placeholder", hall["area"] == "Elsewhere", hall["area"])
    m = alice.mark()
    alice.command("south")
    alice.room(m, name="The Test Hub")
    m = alice.mark()
    alice.command("lane")
    lane = alice.room(m, name="The End of the Lane")
    truth("lane: the exit named lane leads to the lane room's number", hub["exits"]["lane"] == lane["num"], (hub, lane))
    alice.command("out")
    m = alice.mark()
    alice.command("look")
    again = alice.wait("Room.Info", since=m)
    truth("look sends Room.Info again even though nothing changed", again is not None and again["num"] == hub["num"])
    m = alice.mark()
    alice.command("say nothing moves")
    truth("a command that changes nothing sends no Room.Info", alice.quiet("Room.Info", m, 0.6))

    # ================= hidden exits: for that character only =================
    carol.command("tp /domains/gmcptest/hub")
    carol.pump(0.5)
    carol_hub = carol.room(name="The Test Hub")
    truth("Carol sees the hub without the secret exit", carol_hub and "secret" not in carol_hub["exits"], carol_hub)
    m = alice.mark()
    found = alice.command("search")
    truth("Alice's search finds the secret exit (text)", "hidden way" in found, found)
    after = alice.wait("Room.Info", lambda v: "secret" in v["exits"], m)
    truth("Room.Info arrives with the found exit, with no look needed", after is not None, alice.of("Room.Info", m))
    m = alice.mark()
    carol.command("look")
    carol.pump(0.5)
    carol_look = [v for v in carol.of("Room.Info") if v["name"] == "The Test Hub"][-1]
    truth("Carol still does not have the exit after Alice found it", "secret" not in carol_look["exits"], carol_look)
    truth("Carol never was sent a Room.Info with the secret exit", all("secret" not in v["exits"] for v in carol.of("Room.Info")))
    m = alice.mark()
    alice.command("secret")
    secret = alice.room(m, name="The Secret Room")
    truth("Alice can walk the exit and the number matches", secret and after["exits"]["secret"] == secret["num"], (after, secret))
    alice.command("out")
    m = carol.mark()
    carol.command("search")
    carol.wait("Room.Info", lambda v: "secret" in v["exits"], m)
    truth("when Carol finds it herself she gets it too", any("secret" in v["exits"] for v in carol.of("Room.Info", m)))

    # ================= doors =================
    m = alice.mark()
    refused = alice.command("east")
    truth("the closed door stops her", "closed" in refused, refused)
    alice.command("look")
    closed = alice.wait("Room.Info", lambda v: v["doors"].get("east") == "closed", m)
    truth("closed: the exit is present and marked closed", closed and "east" in closed["exits"] and closed["doors"] == {"east": "closed"}, closed)
    m = alice.mark()
    alice.command("open door")
    opened = alice.wait("Room.Info", lambda v: v["doors"].get("east") == "open", m)
    truth("opening the door sends Room.Info with the door open, with no look", opened is not None, alice.of("Room.Info", m))
    carol.pump(0.8)
    truth("Carol, in the same room, was told too (it is in her text as well)", any(v["doors"].get("east") == "open" for v in carol.of("Room.Info")))
    m = alice.mark()
    alice.command("east")
    vault = alice.room(m, name="The Test Vault")
    truth("the vault shows the same door open from its side", vault and vault["doors"] == {"west": "open"}, vault)
    m = alice.mark()
    alice.command("close door")
    shut = alice.wait("Room.Info", lambda v: v["doors"].get("west") == "closed", m)
    truth("closing it from the vault sends the closed state", shut is not None)
    alice.command("open door")
    alice.command("west")
    alice.command("close door")
    example("Room.Info (door)", closed)

    # ================= Char.Vitals =================
    alice.command("tp " + SQUARE)
    alice.pump(0.5)
    v0 = alice.of("Char.Vitals")[-1]
    m = alice.mark()
    alice.command("hurt 7")
    hurt = alice.wait("Char.Vitals", lambda v: v["vitality"] == v0["maxvitality"] - 7, m)
    truth("Char.Vitals after damage shows the lost Vitality", hurt is not None, alice.of("Char.Vitals", m))
    example("Char.Vitals (after damage)", hurt)
    m = alice.mark()
    alice.command("rest")
    rest = alice.wait("Char.Vitals", lambda v: v["resting"], m)
    truth("Char.Vitals after rest says resting", rest is not None)
    m = alice.mark()
    alice.command("stand")
    stand = alice.wait("Char.Vitals", lambda v: not v["resting"], m)
    truth("and not resting after stand", stand is not None)
    alice.command("mk /domains/hollow/obj/healing_potion")
    m = alice.mark()
    alice.command("drink potion")
    healed = alice.wait("Char.Vitals", lambda v: v["vitality"] > hurt["vitality"], m)
    truth("Char.Vitals after a potion shows the healing", healed is not None, alice.of("Char.Vitals", m))
    # a fight
    alice.command("tp /domains/hollow/village/practice_yard") if os.path.exists(os.path.join(mudlib, "domains/hollow/village/practice_yard.cs")) else alice.command("spawn /domains/hollow/mob/practice_dummy")
    m = alice.mark()
    alice.command("kill dummy")
    fight = alice.wait("Char.Vitals", lambda v: v["fighting"], m, timeout=8)
    truth("Char.Vitals says fighting while she fights", fight is not None, alice.of("Char.Vitals", m))
    example("Char.Vitals (fighting)", fight)
    alice.command("tp " + SQUARE)
    over = alice.wait("Char.Vitals", lambda v: not v["fighting"], m + 1, timeout=8)
    truth("and not fighting when she leaves", over is not None)
    # coalescing: ten hits in a burst are not ten messages
    alice.pump(0.5)
    base = alice.of("Char.Vitals")[-1]["vitality"]
    m = alice.mark()
    t0 = time.time()
    alice.raw_send(b"".join(b"hurt 1\r\n" for _ in range(10)))
    final = alice.wait("Char.Vitals", lambda v: v["vitality"] == base - 10, m, timeout=5)
    alice.pump(0.8)
    count = len(alice.of("Char.Vitals", m))
    truth("a burst of ten hits ends at the true Vitality", final is not None, alice.of("Char.Vitals", m))
    truth("and is coalesced into far fewer than ten messages", count <= 4, count)
    # Regeneration alone sends a message when the number changes, never faster than four a second.
    m = alice.mark()
    alice.command("hurt 3")
    t1 = time.time()
    alice.pump(6)
    per_second = len(alice.of("Char.Vitals", m)) / max(1.0, time.time() - t1)
    truth("over a few seconds of regeneration no more than four messages a second", per_second <= 4.0, per_second)

    # ================= Char.Status =================
    s0 = alice.of("Char.Status")[-1]
    m = alice.mark()
    alice.command("gold 40")
    gold = alice.wait("Char.Status", lambda v: v["gold"] == s0["gold"] + 40, m)
    truth("Char.Status after a change of gold", gold is not None, alice.of("Char.Status", m))
    m = alice.mark()
    alice.command("xp 100000")
    levelled = alice.wait("Char.Status", lambda v: v["level"] > s0["level"], m)
    truth("Char.Status after a level", levelled is not None and levelled["experience"] >= s0["experience"] + 100000 and levelled["points_to_place"] > s0["points_to_place"], levelled)
    example("Char.Status (after a level)", levelled)
    truth("the Vitality maximum went up with the level in Char.Vitals", alice.wait("Char.Vitals", lambda v: v["maxvitality"] > v0["maxvitality"], m) is not None)

    # ================= Comm.Channel.Text =================
    bob.command("lv 3")
    alice.command("lv 3")
    m = alice.mark()
    bob.command("tell alice a word from bob")
    tell = alice.wait("Comm.Channel.Text", lambda v: v["channel"] == "tell", m)
    truth("a tell arrives as Comm.Channel.Text, with talker and text as seen", tell == {"channel": "tell", "talker": "Bob", "text": "a word from bob"}, tell)
    example("Comm.Channel.Text (tell)", tell)
    m = alice.mark()
    bob.command("shout hello vale")
    shout = alice.wait("Comm.Channel.Text", lambda v: v["channel"] == "shout", m)
    truth("a shout arrives with the channel shout", shout == {"channel": "shout", "talker": "Bob", "text": "hello vale"}, shout)
    example("Comm.Channel.Text (shout)", shout)
    m = mina.mark()
    warden.command("wizard a line for the wizards")
    chan = mina.wait("Comm.Channel.Text", lambda v: v["channel"] == "wizard", m)
    truth("a channel line arrives with the channel's id", chan == {"channel": "wizard", "talker": "Warden", "text": "a line for the wizards"}, chan)
    example("Comm.Channel.Text (channel)", chan)
    m = alice.mark()
    alice.command("tell bob and back")
    bob.pump(0.3)
    truth("a tell Alice sends is not sent back to her as a received line", alice.quiet("Comm.Channel.Text", m, 0.5))
    m = alice.mark()
    bob.command("tell alice {cmd}braces{/} and {{ more")
    brace = alice.wait("Comm.Channel.Text", since=m)
    truth("the text is the cleaned text as the player sees it (colour tags removed, braces as shown)", brace is not None and brace["text"] == "braces and {{ more", brace)
    truth("and the shared text of a client without GMCP has no GMCP in it", bob.frames == [] and b"\xff\xfa" not in bob.stream)

    # ================= Core.Ping, Core.Supports =================
    m = carol.mark()
    carol.raw_send(message("Core.Ping"))
    carol.pump(0.5)
    truth("Core.Ping is answered with Core.Ping", len(carol.of("Core.Ping", m)) == 1, carol.of("Core.Ping", m))
    m = carol.mark()
    carol.raw_send(message("Core.Supports.Set", ["Char 1"]))
    time.sleep(0.2)
    carol.command("look")
    carol.pump(0.4)
    truth("a client that listed only Char is sent no Room.Info", not carol.of("Room.Info", m))
    carol.command("hurt 2")
    truth("but is sent Char.Vitals", carol.wait("Char.Vitals", since=m) is not None)
    m = carol.mark()
    carol.raw_send(message("Core.Supports.Add", ["Room 1"]))
    time.sleep(0.2)
    carol.command("look")
    truth("Core.Supports.Add brings Room.Info back", carol.wait("Room.Info", since=m) is not None)
    m = carol.mark()
    carol.raw_send(message("Core.Supports.Remove", ["Char 1"]))
    time.sleep(0.2)
    carol.command("hurt 2")
    carol.pump(0.6)
    truth("Core.Supports.Remove stops Char.Vitals", not carol.of("Char.Vitals", m))
    carol.raw_send(message("Core.Supports.Set", ["Char 1", "Room 1", "Comm.Channel 1"]))

    # ================= malformed and oversized incoming messages =================
    alice.command("tp /domains/gmcptest/hub")
    carol.command("tp /domains/gmcptest/hub")
    carol.pump(0.4)
    heard_before = len(alice.clean)
    junk = [
        bytes([IAC, SB, GMCP, IAC, SE]),                                            # empty
        message("Core.Supports.Set", b"this is not json"),                          # malformed json
        message("Core.Hello", b'{"client": '),                                      # truncated json
        message("Core.Hello", b"[" * 200),                                          # nesting
        message("Core.Supports.Set", ["x" * 5000] * 3),                             # a long list
        message("Core.Supports.Set", ["Room 1"] * 500),                             # more entries than allowed
        message("say", b"INJECTED-BY-GMCP"),                                        # something a line would run
        message("Core.Hello", {"client": "x\nsay INJECTED-BY-GMCP2"}),              # a newline in a value
        message("Char.Vitals", {"vitality": 1}),                                    # a package a client never sends
        message("Core.Hello " + "A" * 100000),                                      # oversized
        message("Core.Hello", b'{"client":"' + b"\xff\xff" + b'"}'),                # a doubled 255 inside the body
        bytes([IAC, SB, 24, 0]) + b"xterm" + bytes([IAC, SE]),                      # another option's subnegotiation
        bytes([IAC, SB, GMCP]) + b"Core.Ping" + bytes([IAC, 241]),                  # ended by another command
        bytes([IAC, DO, GMCP, IAC, WILL, GMCP, IAC, WONT, 99, IAC, 241]),           # more negotiation
    ]
    for item in junk:
        carol.raw_send(item)
    carol.raw_send(bytes([IAC, SB, GMCP]) + b"Core.Hello " + b"B" * 20000)           # started and never finished ...
    carol.raw_send(bytes([IAC, SE]))                                                 # ... and then finished: dropped whole
    alice.pump(0.5)
    reply = carol.command("sync after")
    truth("after all of it the connection still works and the next command is answered", "[sync" in reply or "sync" in reply, reply)
    alice.pump(0.3)
    truth("nothing a client sent in GMCP was run as a command or echoed to another player", b"INJECTED" not in alice.clean[heard_before:] and b"INJECTED" not in bob.clean, alice.clean[heard_before:][-300:])
    m = carol.mark()
    carol.raw_send(bytes([IAC, SB, GMCP]) + b"Core.Pi")
    time.sleep(0.15)
    carol.raw_send(b"ng" + bytes([IAC, SE]))
    carol.pump(0.6)
    truth("a message split over two packets is put together", len(carol.of("Core.Ping", m)) == 1, carol.of("Core.Ping", m))
    m = carol.mark()
    carol.raw_send(b"".join(message("Core.Ping") for _ in range(400)))
    carol.pump(1.0)
    pings = len(carol.of("Core.Ping", m))
    truth("a flood of 400 pings gets far fewer answers (rate limit), and some", 0 < pings < 100, pings)
    truth("after the flood the connection works", "sync" in carol.command("sync flood"))
    log = open(logpath, errors="replace").read()
    truth("the driver log holds none of the incoming payloads", "INJECTED" not in log and "AAAAAAAAAA" not in log and "BBBBBBBBBB" not in log and "xterm" not in log)
    truth("and at most a short line about the oversize and the rate limit", log.count("gmcp:") <= 4, log.count("gmcp:"))
    # a client that never said DO is not speaking GMCP: its messages are ignored
    m = hank.mark()
    hank.raw_send(message("Core.Ping"))
    hank.pump(0.5)
    truth("a client that did not answer DO gets no reply to Core.Ping", not hank.of("Core.Ping", m) and b"\xff\xfa" not in hank.stream)

    # ================= a wizard's probe =================
    h.write(mudlib, "/wiz/mina/probe", PROBE)
    h.write(mudlib, "/wiz/mina/hall", WIZROOM)
    check("Mina clones her probe", mina.command("clone /wiz/mina/probe"), "Cloned /wiz/mina/probe#")
    m_alice = alice.mark()
    m_carol = carol.mark()
    out = mina.command("gprobe alice")
    check("the probe cannot send GMCP to a player", out, "send: refused")
    check("nor to itself", out, "self: refused")
    check("it may ask whether a player uses GMCP (a harmless boolean)", out, "active: True")
    check("it cannot make the service send", out, "channel: null", "poll: null", "look: null")
    check("it cannot read the facts the service builds", out, "facts: null", "roomfacts: null")
    check("it cannot assign a room number and reading one gives 0", out, "assign: 0", "query: 0")
    alice.pump(1.0)
    truth("Alice received nothing from the probe", all("PROBE" not in json.dumps(v) for _, v in alice.frames[m_alice:]), alice.frames[m_alice:])
    out = mina.command("gprobe bob")
    check("for a client without GMCP the boolean is false and nothing is sent", out, "active: False", "send: refused")
    truth("Bob's stream holds no GMCP", b"\xff\xfa" not in bob.stream)

    # ================= wizard home rooms =================
    mina.command("goto /wiz/mina/hall")
    wiz = mina.wait("Room.Info", lambda v: v["name"] == "Mina's Workshop")
    truth("a wizard's home room is in the Workshop area", wiz and wiz["area"] == "Workshop of Mina", wiz)
    truth("and has a number from the separate range", wiz and wiz["num"] > 1_000_000_000, wiz)
    truth("its exit to the game names the game room's saved number", wiz and wiz["exits"].get("out") == hub["num"], wiz)
    saved = open(os.path.join(work, "save", "secure", "roomids.json")).read()
    truth("the saved table holds no wizard room and the game rooms", "/wiz/" not in saved and "gmcptest/hub" in saved, saved[:200])
    example("Room.Info (wizard home)", wiz)

    # ================= restart: stable numbers =================
    numbers = {}
    for c, nm in ((alice, "alice"),):
        for command, name in (("tp /domains/gmcptest/hall", "The Test Hall"), ("tp /domains/gmcptest/lane_end", "The End of the Lane"), ("tp /domains/gmcptest/vault", "The Test Vault"), ("tp /domains/gmcptest/hub", "The Test Hub"), ("tp " + SQUARE, None)):
            m = c.mark()
            c.command(command)
            got = c.wait("Room.Info", since=m)
            numbers[command] = (got["name"], got["num"])
    highest = max(n for _, n in numbers.values())
    for c in (alice, bob, carol, mina, warden, hank):
        try:
            c.send("quit")
        except OSError:
            pass
    time.sleep(2.0)
    stop_driver(driver_box[0])
    h.write(mudlib, "/domains/gmcptest/new_room", NEWROOM)
    driver_box[0] = start_driver(work, mudlib, driver_box[1])
    alice = login("Alice")
    after = {}
    for command in numbers:
        m = alice.mark()
        alice.command(command)
        got = alice.wait("Room.Info", since=m)
        after[command] = (got["name"], got["num"])
    truth("every room has the same number after a restart of the driver", after == numbers, (numbers, after))
    m = alice.mark()
    alice.command("tp /domains/gmcptest/new_room")
    fresh = alice.wait("Room.Info", lambda v: v["name"] == "A Room Made After the Restart", m)
    truth("a room made after the restart gets a new number, never a used one", fresh and fresh["num"] > highest and fresh["num"] not in {n for _, n in numbers.values()}, (fresh, highest))
    truth("and the hub's exit to it is not in the hub (it only names the hub)", fresh and fresh["exits"].get("out") == numbers["tp /domains/gmcptest/hub"][1], fresh)
    status = alice.wait("Char.Status")
    truth("after a restart Char.Status is sent again at login", status and status["name"] == "Alice" and status["level"] >= 2, status)

    # ================= the room numbers: the daemon itself =================
    alice.send("quit")


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-gmcp-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    patch_player(mudlib)
    for name, source in (("hub", HUB), ("hall", HALL), ("lane_end", LANE), ("vault", VAULT), ("secret_room", SECRET)):
        os.makedirs(os.path.join(mudlib, "domains", "gmcptest"), exist_ok=True)
        h.write(mudlib, f"/domains/gmcptest/{name}", source)
    os.makedirs(os.path.join(mudlib, "wiz"), exist_ok=True)
    logpath = os.path.join(work, "driver.log")
    log = open(logpath, "w")
    driver_box = [start_driver(work, mudlib, log), log]
    try:
        run(work, mudlib, logpath, driver_box)
    finally:
        stop_driver(driver_box[0])
        print("\nexamples captured from the wire:")
        for label, value in EXAMPLES.items():
            print(f"  {label}: {json.dumps(value)}")
        if os.environ.get("KEEP") != "1":
            shutil.rmtree(work, ignore_errors=True)
        else:
            print("kept", work)
    print(f"\n{CHECKS[0]} checks, {len(h.failures)} failure(s)")
    sys.exit(1 if h.failures else 0)


if __name__ == "__main__":
    main()
