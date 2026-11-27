#!/usr/bin/env python3
"""End-to-end test of the browser client's bridge (docs/specs/web-client.md sections 2 and 12.1).

Two passes, each on its own driver and website:

  1. PLAIN mode (the bridge connects to the game's ordinary telnet port and sends no PROXY line: Site:Play:PlainMode, for tests only), with
     every limit of the game switched off (the shared test defaults);
  2. PROXY-LINE mode (the real thing, the first time the two halves meet): the game runs with --bridge-port on the next port and the
     production-style limits (a per-address cap, the login throttle), the website with PlainMode off, so every web player's telnet connection
     opens with a PROXY line giving the address the website was told by its proxy (here a forged X-Hollowmere-Client-Ip, as Caddy would
     set it). It checks what the game logs and counts, what the public port does with a PROXY line, what the bridge port does without one,
     GMCP, a dropped page and the link-dead resume, and that the login throttle sees the visitor's real address.

Pass 1: a headless WebSocket client (the Python
`websockets` package) does what a browser would: refused from a wrong origin, a new account through a masked password, a few commands, a
second login of the same name, a tell from a telnet player, hostile text, pings that must not count as player input, the idle timeout,
a flood of connections, and quit.

Usage (from the repository root, after `dotnet build` of src/LesMudOS.Driver and web/LesMudOS.Web, and `pip install websockets`):
    LESMUDOS_TEST_PORT=5112 LESMUDOS_WEB_TEST_PORT=5140 python3 tests/web_client_test.py      (the game uses the next port too, for the bridge)
    ... --plain-only / --proxy-only to run one pass
    python3 tests/web_client_test.py --load 20      # open N logged-out web sessions and print the processes' memory (spec section 11)
"""
import asyncio
import json
import os
import shutil
import socket
import subprocess
import sys
import tempfile
import time
import urllib.request

os.environ.setdefault("LESMUDOS_TEST_PORT", "5120")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402

try:
    import websockets
except ImportError:
    print("SKIP: the Python 'websockets' package is not installed (pip install websockets)")
    sys.exit(0)

GAME_PORT = h.PORT
WEB_PORT = int(os.environ.get("LESMUDOS_WEB_TEST_PORT", "5140"))
ORIGIN = f"http://127.0.0.1:{WEB_PORT}"
URL = f"ws://127.0.0.1:{WEB_PORT}/play/ws"
PASSWORD = "Tr0ub4dor&3 web"
failures = []
checks = [0]


def check(label, ok, detail=""):
    checks[0] += 1
    print(("ok   " if ok else "FAIL ") + label + ("" if ok else "   " + str(detail)[:300]))
    if not ok:
        failures.append(label)


class Web:
    """One page: a socket, every frame it has seen, and the text of the `out` frames since the last mark."""

    def __init__(self, ws):
        self.ws, self.frames, self.raw, self.buf = ws, [], [], ""

    @classmethod
    async def open(cls, ip="203.0.113.9", origin=ORIGIN):
        ws = await websockets.connect(URL, origin=origin, subprotocols=["hollowmere.v1"], additional_headers={"X-Hollowmere-Client-Ip": ip}, ping_interval=None, max_size=None)
        w = cls(ws)
        first = await w.frame()
        assert first["t"] == "hello" or first.get("code") == "full", first
        return w

    async def frame(self, timeout=10):
        raw = await asyncio.wait_for(self.ws.recv(), timeout)
        f = json.loads(raw)
        self.raw.append(raw)
        self.frames.append(f)
        if f["t"] == "out":
            self.buf += f["d"]
        return f

    async def until(self, needle, timeout=10):
        """Read frames until the text since the last mark contains `needle`; return it (and clear the buffer up to the end of the match)."""
        end = time.time() + timeout
        while needle not in self.buf:
            await self.frame(max(0.05, end - time.time()))
        i = self.buf.index(needle) + len(needle)
        got, self.buf = self.buf[:i], self.buf[i:]
        return got

    async def say(self, line):
        await self.ws.send(json.dumps({"t": "in", "d": line}))

    async def command(self, line, until="> "):
        await self.say(line)
        return await self.until(until)

    def gmcp(self, package):
        return [f for f in self.frames if f["t"] == "gmcp" and f["p"] == package]

    async def closed(self, timeout=10):
        """Read until the server closes; return the close code."""
        try:
            while True:
                await self.frame(timeout)
        except websockets.ConnectionClosed:
            return self.ws.close_code


BRIDGE_PORT = GAME_PORT + 1


def start_processes(work, mudlib, proxy=False):
    env = {**os.environ, "LESMUDOS_ADMIN_SECRET": h.SECRET, "LESMUDOS_IDLE_MINUTES": "5s", "LESMUDOS_TIMEOUT_MINUTES": "12s"}
    if proxy:
        env = {k: v for k, v in os.environ.items() if not k.startswith("LESMUDOS_") or k == "LESMUDOS_TEST_PORT"}
        env.update({"LESMUDOS_ADMIN_SECRET": h.SECRET, "LESMUDOS_BRIDGE_PORT": str(BRIDGE_PORT), "LESMUDOS_BRIDGE_HANDSHAKE_SECONDS": "2",
                    "LESMUDOS_MAX_PER_ADDRESS": "3", "LESMUDOS_CONNECTS_PER_MINUTE": "0", "LESMUDOS_THROTTLE_FAIL_DELAY_MS": "0",
                    "LESMUDOS_THROTTLE_ADDRESS_FAILURES": "8", "LESMUDOS_THROTTLE_NEW_PER_HOUR": "20", "LESMUDOS_THROTTLE_NEW_PER_DAY": "50"})
    driver_dll = os.path.join(h.ROOT, "src", "LesMudOS.Driver", "bin", "Debug", "net10.0", "LesMudOS.Driver.dll")
    dlog = open(os.path.join(work, "driver.log"), "w")
    if proxy:
        env["LESMUDOS_TEST_PORT"] = str(GAME_PORT)
    driver = subprocess.Popen(["dotnet", driver_dll, "--mudlib", mudlib, "--save", os.path.join(work, "save"), "--port", str(GAME_PORT)], stdout=dlog, stderr=subprocess.STDOUT, env=env)
    for _ in range(150):
        try:
            socket.create_connection(("127.0.0.1", GAME_PORT), timeout=0.3).close()
            break
        except OSError:
            time.sleep(0.2)
    else:
        raise SystemExit("the driver did not start")
    web_dir = os.path.join(h.ROOT, "web", "LesMudOS.Web")
    web_dll = os.path.join(web_dir, "bin", "Debug", "net10.0", "LesMudOS.Web.dll")
    wlog = open(os.path.join(work, "web.log"), "w")
    wenv = {**os.environ, "ASPNETCORE_URLS": f"http://127.0.0.1:{WEB_PORT}", "ASPNETCORE_ENVIRONMENT": "Production",
            "Site__BaseUrl": ORIGIN, "Site__SaveDirectory": "", "Site__Play__Enabled": "true", "Site__Play__PlainMode": "false" if proxy else "true",
            "Site__Play__GameBridgePort": str(BRIDGE_PORT if proxy else GAME_PORT), "Site__Play__MaxConnections": os.environ.get("WEB_TEST_MAX_CONNECTIONS", "10"),
            "Site__Play__KillSwitchFile": os.path.join(work, "play.off")}
    web = subprocess.Popen(["dotnet", web_dll], cwd=web_dir, stdout=wlog, stderr=subprocess.STDOUT, env=wenv)
    for _ in range(150):
        try:
            urllib.request.urlopen(f"{ORIGIN}/healthz", timeout=1).read()
            break
        except OSError:
            time.sleep(0.2)
    else:
        raise SystemExit("the website did not start; see " + os.path.join(work, "web.log"))
    return driver, web


def stop(proc):
    proc.terminate()
    try:
        proc.wait(30)
    except subprocess.TimeoutExpired:
        proc.kill()
        proc.wait()


def rss_mb(pid):
    try:
        with open(f"/proc/{pid}/status") as f:
            for line in f:
                if line.startswith("VmRSS:"):
                    return int(line.split()[1]) / 1024
    except OSError:
        pass
    return -1


async def login_new(w, name):
    await w.until("known? ")
    await w.say(name)
    await w.until("Choose a password")
    # the mask frame comes right after the prompt text
    for _ in range(20):
        if any(f["t"] == "mask" and f["on"] for f in w.frames):
            break
        await w.frame()
    await w.say(PASSWORD)
    await w.until("Type it again: ")
    await w.say(PASSWORD)
    await w.until("Your calling: ")
    await w.say("adventurer")
    await w.until("> ")


async def main(work, mudlib, driver, web):
    # ---- the doors that must stay shut ----
    for label, kwargs, status in [
        ("a wrong origin is refused (403)", {"origin": "https://evil.example"}, 403),
        ("a missing client address is refused (403)", {"headers": {}}, 403),
        ("a missing subprotocol is refused (403)", {"subprotocols": None}, 403),
    ]:
        try:
            ws = await websockets.connect(URL, origin=kwargs.get("origin", ORIGIN), subprotocols=kwargs.get("subprotocols", ["hollowmere.v1"]),
                                          additional_headers=kwargs.get("headers", {"X-Hollowmere-Client-Ip": "203.0.113.9"}), ping_interval=None)
            await ws.close()
            check(label, False, "the socket opened")
        except websockets.InvalidStatus as e:
            check(label, e.response.status_code == status, e.response.status_code)
    status = json.loads(urllib.request.urlopen(f"{ORIGIN}/play/status").read())
    check("/play/status says on and not full", status == {"v": 1, "enabled": True, "full": False}, status)
    page = urllib.request.urlopen(f"{ORIGIN}/play").read().decode()
    check("/play serves the client page", 'id="play-log"' in page and "/js/play.js" in page)

    # ---- a new account through the masked password ----
    a = await Web.open("203.0.113.9")
    await login_new(a, "Webtest")
    masks = [f["on"] for f in a.frames if f["t"] == "mask"]
    check("the password prompts were masked and unmasked (mask frames)", masks.count(True) >= 2 and masks.count(False) >= 2, masks)
    check("the password is in no frame the game sent (the game never echoes)", not any(PASSWORD in r for r in a.raw))
    check("the character arrived with GMCP: Char.Status, Room.Info and Char.Vitals frames", a.gmcp("Char.Status") and a.gmcp("Room.Info") and a.gmcp("Char.Vitals"),
          [f["p"] for f in a.frames if f["t"] == "gmcp"])
    status_msg = a.gmcp("Char.Status")[-1]["d"]
    check("Char.Status names the character", status_msg.get("name") == "Webtest", status_msg)
    room = a.gmcp("Room.Info")[-1]["d"]
    check("Room.Info has a room name and exits", isinstance(room.get("name"), str) and isinstance(room.get("exits"), dict) and room["exits"], room)
    check("colour arrives as the game's own ANSI codes, intact", any("\x1b[" in f.get("d", "") for f in a.frames if f["t"] == "out"))

    text = await a.command("look")
    check("look shows the room", room["name"] in text, text[-200:])
    check("the exits come as bold bright cyan command words", "\x1b[1;96m" in text, text[-300:])
    exit_word = sorted(room["exits"])[0]
    text = await a.command(exit_word)
    check("a move works and sends a new Room.Info", len(a.gmcp("Room.Info")) >= 3 and a.gmcp("Room.Info")[-1]["d"]["name"] != "", text[-200:])
    text = await a.command("say hello there")
    check("say is answered by the game", "hello there" in text, text)
    text = await a.command("say <script>alert(1)</script><img src=x onerror=alert(1)>")
    check("hostile text comes back as text", "<script>alert(1)</script><img src=x onerror=alert(1)>" in text, text)
    last_raw = [r for r in a.raw if "script" in r][-1]
    check("and is escaped in the frame (no raw angle bracket on the wire)", "<" not in last_raw and "\\u003C" in last_raw, last_raw[:200])

    # ---- a second login of the same name, and a tell from a telnet player ----
    # A name that is online asks for the password like any other; the right one takes the body over (docs/specs/login-throttle.md, "Taking over").
    b = await Web.open("203.0.113.10")
    await b.until("known? ")
    await b.say("webtest")
    t = await b.until("Password: ")
    check("a second web login of the same name is asked for the password (the prompt does not say who is online)", "already here" not in t, t)
    await b.say(PASSWORD)
    t = await b.until("You take over your character where it stands.")
    check("the right password takes the body over: the web player is told", "You take over your character where it stands." in t, t)
    old_text = ""
    try:
        while True:
            await a.frame(5)
    except websockets.ConnectionClosed:
        pass
    old_text = "".join(f.get("d", "") for f in a.frames if f["t"] == "out") + a.buf
    check("the old web connection was told in one line and closed", "Your character has been taken over from another connection." in old_text and a.ws.close_code is not None, old_text[-200:])
    await b.until("> ")
    a = b
    # a telnet player takes over from the web, and the web takes over from the telnet player
    tn = h.Client("webtest", password=PASSWORD)
    t = tn.read_until("> ")
    check("a telnet login takes over a web player", "You take over your character where it stands." in t, t)
    check("and the web connection is told and closed", await a.closed() is not None and "taken over from another connection" in "".join(f.get("d", "") for f in a.frames if f["t"] == "out") + a.buf)
    a = await Web.open("203.0.113.10")
    await a.until("known? ")
    await a.say("webtest")
    await a.until("Password: ")
    await a.say(PASSWORD)
    t = await a.until("You take over your character where it stands.")
    await a.until("> ")
    check("and a web login takes over a telnet player, who is told and disconnected", tn.closed(3) and a.gmcp("Room.Info") != [], t)

    friend = h.Client("friendly")
    friend.read_until("> ")
    friend.send("tell webtest hi from telnet")
    got = await a.until("hi from telnet")
    check("a tell from a telnet player arrives as text", "friendly" in got.lower() and "hi from telnet" in got, got)
    for _ in range(10):
        if any(f["t"] == "gmcp" and f["p"] == "Comm.Channel.Text" for f in a.frames):
            break
        try:
            await a.frame(1)
        except asyncio.TimeoutError:
            break
    comm = [f for f in a.frames if f["t"] == "gmcp" and f["p"] == "Comm.Channel.Text"]
    check("and as a Comm.Channel.Text frame", comm and comm[-1]["d"].get("talker") == "Friendly" and "hi from telnet" in comm[-1]["d"].get("text", ""), comm)
    friend.sock.close()

    # ---- pings are answered by the bridge and are not input ----
    for n in range(1, 101):
        await a.ws.send(json.dumps({"t": "ping", "n": n}))
    pongs = 0
    try:
        while True:
            f = await a.frame(1.5)
            pongs += f["t"] == "pong"
    except asyncio.TimeoutError:
        pass
    check("pings are answered with pongs (a flood of them is cut at 10 a second)", 8 <= pongs <= 40, pongs)
    await a.ws.send(json.dumps({"t": "size", "cols": 52, "rows": 30}))

    # ---- quit ----
    await a.say("quit")
    code = await a.closed()
    check("quit gives game_closed and a normal close (1000)", any(f["t"] == "sys" and f["code"] == "game_closed" for f in a.frames) and code == 1000, (code, a.frames[-2:]))
    time.sleep(0.5)
    check("the character was saved", os.path.exists(os.path.join(work, "save", "players", "webtest.json")))

    # ---- log in again: one masked prompt, a wrong password asks again (masked again), then in ----
    c = await Web.open("203.0.113.11")
    await c.until("known? ")
    await c.say("webtest")
    await c.until("Password: ")
    await c.say("wrong password here")
    await c.until("Wrong password. Password: ")
    await c.say(PASSWORD)
    await c.until("> ")
    check("an existing account logs in after a wrong password", True)
    masks = [f["on"] for f in c.frames if f["t"] == "mask"]
    check("every password prompt was masked again", masks.count(True) >= 2, masks)
    check("neither password appears in any frame", not any(PASSWORD in r or "wrong password here" in r for r in c.raw))

    # ---- keepalive is not input: pings every second do not stop the idle timeout (idle 5 s, disconnect 12 s) ----
    t0 = time.time()
    code = None
    try:
        while time.time() - t0 < 40:
            await c.ws.send(json.dumps({"t": "ping", "n": int(time.time())}))
            await c.frame(2) if False else None
            try:
                await asyncio.wait_for(c.frame(1), 1)
            except asyncio.TimeoutError:
                pass
    except websockets.ConnectionClosed:
        code = c.ws.close_code
        try:                      # a send fails as soon as the close is seen, with frames still unread: read them
            while True:
                await c.frame(1)
        except (websockets.ConnectionClosed, asyncio.TimeoutError):
            pass
    waited = time.time() - t0
    check("the idle player is disconnected on schedule although the page keeps pinging", code == 1000 and 10 <= waited <= 25, (code, round(waited, 1)))
    check("with the game's own words, then game_closed", any(f["t"] == "sys" and f["code"] == "game_closed" for f in c.frames))

    # ---- a flood of connections: the bridge refuses the surplus, the game stays healthy ----
    socks, fulls = [], 0
    for i in range(14):
        try:
            w = await Web.open(f"198.51.100.{i + 1}")
        except websockets.InvalidStatus:
            continue
        socks.append(w)
    for w in socks:
        if any(f["t"] == "sys" and f["code"] == "full" for f in w.frames):
            fulls += 1
        else:
            try:
                await asyncio.wait_for(w.frame(1), 1.0)
            except Exception:
                pass
            fulls += any(f["t"] == "sys" and f["code"] == "full" for f in w.frames)
    check("past the cap the page is told the game is full", fulls >= 3, fulls)
    for w in socks:
        try:
            await w.ws.close()
        except Exception:
            pass
    await asyncio.sleep(1)
    fresh = h.Client("afterflood")
    fresh.read_until("> ")
    check("the game is healthy after the flood", True)
    fresh.sock.close()

    # ---- the kill switch: no restart needed ----
    open(os.path.join(work, "play.off"), "w").close()
    await asyncio.sleep(6)
    try:
        ws = await websockets.connect(URL, origin=ORIGIN, subprotocols=["hollowmere.v1"], additional_headers={"X-Hollowmere-Client-Ip": "203.0.113.50"})
        await ws.close()
        check("the kill-switch file stops new connections", False)
    except websockets.InvalidStatus as e:
        check("the kill-switch file stops new connections", e.response.status_code == 404, e.response.status_code)
    os.unlink(os.path.join(work, "play.off"))

    log = open(os.path.join(work, "driver.log")).read()
    check("the game logged the web connections", "connection from" in log)
    wlog = open(os.path.join(work, "web.log")).read()
    check("the website logged them with the true address and no typed text", "play: open 203.0.113.9" in wlog and "hello there" not in wlog and PASSWORD not in wlog, wlog[-500:])


def game_log(work):
    return open(os.path.join(work, "driver.log")).read()


def raw_connect(port):
    s = socket.create_connection(("127.0.0.1", port), timeout=5)
    s.settimeout(0.3)
    return s


def raw_read(s, until=None, timeout=3.0):
    end = time.time() + timeout
    data = b""
    while time.time() < end:
        try:
            chunk = s.recv(65536)
        except socket.timeout:
            continue
        except OSError:
            return data, True
        if chunk == b"":
            return data, True
        data += chunk
        if until and until in data:
            return data, False
    return data, False


async def wrong_passwords(name, ip, count):
    """Open a page from `ip`, give the name and `count` wrong passwords (the game ends the connection at three). Returns the text seen."""
    w = await Web.open(ip)
    await w.until("known? ")
    await w.say(name)
    text = ""
    for _ in range(count):
        text += await w.until("Password: ", 8)
        await w.say("not the password 1")
        try:
            text += await w.until("Wrong password.", 8)
        except (asyncio.TimeoutError, websockets.ConnectionClosed):
            break
    try:
        while True:
            await w.frame(2)
    except (asyncio.TimeoutError, websockets.ConnectionClosed):
        pass
    text += w.buf
    try:
        await w.ws.close()
    except Exception:
        pass
    return text


async def main_proxy(work, mudlib, driver, web):
    A = "203.0.113.9"
    # ---- the first real character, through the website and the bridge listener ----
    a = await Web.open(A)
    await login_new(a, "Proxie")
    check("PROXY mode: a character is made through the masked password", any(f["t"] == "mask" and f["on"] for f in a.frames))
    check("PROXY mode: GMCP frames arrive (Char.Status, Room.Info, Char.Vitals)", a.gmcp("Char.Status") and a.gmcp("Room.Info") and a.gmcp("Char.Vitals"), [f["p"] for f in a.frames if f["t"] == "gmcp"])
    room = a.gmcp("Room.Info")[-1]["d"]
    text = await a.command("look")
    check("PROXY mode: look shows the room", room["name"] in text, text[-200:])
    text = await a.command(sorted(room["exits"])[0])
    check("PROXY mode: a move works", len(a.gmcp("Room.Info")) >= 3, text[-200:])
    text = await a.command("say hello through the bridge")
    check("PROXY mode: say is answered", "hello through the bridge" in text, text)
    log = game_log(work)
    check("the game's 'connection from' line has the address the website gave, with (web)", f"connection from {A} (web)" in log, log[-400:])
    check("... and never the bridge's own 127.0.0.1 as a web player's address", "127.0.0.1 (web)" not in log and ":" not in "".join(l.split("connection from ")[1].split(" (web)")[0] for l in log.splitlines() if "(web)" in l and "connection from" in l), log[-400:])
    wlog = open(os.path.join(work, "web.log")).read()
    check("the website logged the same address", f"play: open {A}" in wlog, wlog[-300:])

    # ---- the per-address cap counts the claimed address (the cap is 3 here), not the bridge's 127.0.0.1 ----
    C = "198.51.100.5"
    held = [await Web.open(C) for _ in range(3)]
    for w in held:
        await w.until("known? ")
    fourth = await Web.open(C)
    got = ""
    try:
        got = await fourth.until("Too many connections from your address.", 5)
    except Exception as e:
        got = repr(e)
    check("the fourth page from one address is refused by the game's per-address cap", "Too many connections from your address." in got, got)
    time.sleep(0.5)
    check("... and the game logged it under that address with (web)", f"connection refused: too many from address: {C} (web)" in game_log(work))
    other = await Web.open("198.51.100.6")
    await other.until("known? ")
    check("another address is not affected", True)
    await other.ws.close()
    pub = h.Client("publicguy")
    pub.read_until("> ")
    check("the public port's own 127.0.0.1 count is separate (a telnet login still works)", True)
    pub.sock.close()
    for w in held + [fourth]:
        await w.ws.close()
    await asyncio.sleep(1)

    # ---- a PROXY line typed at the PUBLIC port is just a (bad) name, and changes nothing ----
    E = "198.51.100.77"
    s = raw_connect(GAME_PORT)
    raw_read(s, b"known?", 5)
    s.sendall(f"PROXY TCP4 {E} 127.0.0.1 0 4000\r\n".encode())
    data, _ = raw_read(s, b"Try again", 4)
    check("a PROXY line at the public port is refused as a name", b"Names are 2 to 16 letters" in data, data)
    s.sendall(f"PROXY TCP4 {E} 127.0.0.1 0 4000\r\n".encode() * 3)
    time.sleep(0.5)
    held = [await Web.open(E) for _ in range(3)]
    for w in held:
        await w.until("known? ")
    check("... and did not count against the address it named (three pages from it still get in)", True)
    s.close()
    for w in held:
        await w.ws.close()
    await asyncio.sleep(1.0)
    log = game_log(work)
    check("... the log never shows the claimed address for the public connection", f"connection from {E}:" not in log and "connection from 127.0.0.1:" in log)

    # ---- the bridge port refuses a connection with no PROXY line ----
    s = raw_connect(BRIDGE_PORT)
    data, closed = raw_read(s, None, 4)
    check("the bridge port: silence is closed after the handshake time with nothing sent", closed and data == b"", (closed, data))
    s.close()
    s = raw_connect(BRIDGE_PORT)
    s.sendall(b"proxie\r\n")
    data, closed = raw_read(s, None, 4)
    check("the bridge port: a plain name instead of a PROXY line is closed at once with nothing sent", closed and data == b"", (closed, data))
    s.close()
    check("... and it is logged as a bad PROXY line", "bridge connection refused: bad PROXY line" in game_log(work))

    # ---- a dropped page leaves the body link-dead; a reconnect inside the window resumes it ----
    await a.ws.close()
    await asyncio.sleep(1.5)
    log = game_log(work)
    check("the dropped page's connection ended is logged with the web address and the player", f"connection ended: {A} (web), player Proxie" in log, log[-300:])
    r = await Web.open(A)
    await r.until("known? ")
    await r.say("proxie")
    await r.until("Password: ")
    await r.say(PASSWORD)
    text = await r.until("> ", 15)
    check("a reconnect from the web inside the window resumes the lingering body", "shake off the daze" in text, text[-300:])
    check("... with GMCP again", len(r.gmcp("Room.Info")) >= 1)
    await r.say("quit")
    await r.closed()

    # ---- the login throttle sees the visitor's real address ----
    for nm, ip in (("Victim", "203.0.113.30"), ("Yara", "203.0.113.30"), ("Zed", "203.0.113.30")):
        w = await Web.open(ip)
        await login_new(w, nm)
        await w.say("quit")
        await w.closed()
    X, Y = "203.0.113.31", "203.0.113.33"
    t1 = await wrong_passwords("Victim", X, 3)
    check("three wrong passwords through the web end the connection as before", "Goodbye" in t1, t1[-200:])
    t2 = await wrong_passwords("Victim", X, 2)
    check("the fifth locks the name and the player is told what to do", "wait a few minutes and try again" in t2, t2[-300:])
    time.sleep(0.5)
    log = game_log(work)
    check("the throttle logged the lock with the real web address", f"name locked: Victim (from {X})" in log, [l for l in log.splitlines() if "throttle" in l][-4:])
    w = await Web.open("203.0.113.34")
    await w.until("known? ")
    await w.say("victim")
    t3 = await w.until("Try again later.", 8)
    check("a name locked by five wrong passwords through the web refuses everyone else too, with the plain words", "Too many attempts. Try again later." in t3, t3[-200:])
    await w.ws.close()
    # separate counts: Y has 3 wrong passwords on another name; X has 5 so far
    ty = await wrong_passwords("Yara", Y, 3)
    check("another address's failures are counted separately", "Goodbye" in ty, ty[-200:])
    tz = await wrong_passwords("Zed", X, 3)
    time.sleep(0.5)
    log = game_log(work)
    check("the first address reaches its own count (5 + 3 = 8) and is locked", f"address locked: {X} for" in log, [l for l in log.splitlines() if "throttle" in l][-4:])
    check("... while the second address (3) is not locked", f"address locked: {Y}" not in log)
    w = await Web.open(Y)
    await login_existing(w, "Yara")
    check("the second address can still log in", True)
    await w.say("quit")
    await w.closed()
    w = await Web.open(X)
    await w.until("known? ")
    await w.say("nobodyatall")
    t4 = await w.until("Try again later.", 8)
    check("the locked address is refused whatever name it gives (an unknown name too)", "Too many attempts. Try again later." in t4, t4[-200:])
    await w.ws.close()
    log = game_log(work)
    check("no password is in the game's log", "not the password 1" not in log and PASSWORD not in log)
    check("the game logged web connections only with (web) addresses of the forged set",
          all(l.split("connection from ")[1].split(" (web)")[0] in {A, "198.51.100.5", "198.51.100.6", "198.51.100.77", "203.0.113.30", "203.0.113.31", "203.0.113.33", "203.0.113.34"}
              for l in log.splitlines() if "(web)" in l and "connection from" in l), [l for l in log.splitlines() if "(web)" in l and "connection from" in l][-6:])


async def login_existing(w, name):
    await w.until("known? ")
    await w.say(name)
    await w.until("Password: ")
    await w.say(PASSWORD)
    await w.until("> ", 15)


async def load(n, work, driver, web):
    print(f"memory with 0 sessions: game {rss_mb(driver.pid):.0f} MB, website {rss_mb(web.pid):.0f} MB")
    ws = []
    for i in range(n):
        ws.append(await Web.open(f"10.{i // 250}.{i % 250}.{i % 200 + 1}"))
    await asyncio.sleep(2)
    for w in ws:
        await w.until("known? ")
        await w.say("loaduser")
    await asyncio.sleep(2)
    print(f"memory with {n} sessions (each has given a name, so the game holds a login object and a player body for it): game {rss_mb(driver.pid):.0f} MB, website {rss_mb(web.pid):.0f} MB")


def run():
    plain = "--proxy-only" not in sys.argv
    proxy = "--plain-only" not in sys.argv
    if "--load" in sys.argv:
        work = tempfile.mkdtemp(prefix="lesmudos-webtest-")
        mudlib = os.path.join(work, "mudlib")
        h.copy_mudlib(mudlib)
        driver, web = start_processes(work, mudlib)
        try:
            asyncio.run(load(int(sys.argv[sys.argv.index("--load") + 1]), work, driver, web))
        finally:
            stop(web)
            stop(driver)
            shutil.rmtree(work, ignore_errors=True)
        return
    for mode, body, on in (("plain", main, plain), ("proxy", main_proxy, proxy)):
        if not on:
            continue
        print(f"--- pass: {mode} mode")
        work = tempfile.mkdtemp(prefix=f"lesmudos-webtest-{mode}-")
        mudlib = os.path.join(work, "mudlib")
        h.copy_mudlib(mudlib)
        driver, web = start_processes(work, mudlib, proxy=(mode == "proxy"))
        before = len(failures)
        try:
            asyncio.run(body(work, mudlib, driver, web))
        except BaseException as e:  # noqa: BLE001
            check(f"the {mode} pass ran to its end", False, repr(e))
        finally:
            stop(web)
            stop(driver)
            if len(failures) > before:
                print(f"logs kept in {work}")
            else:
                shutil.rmtree(work, ignore_errors=True)
    print(f"\n{checks[0]} checks, {len(failures)} failures")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    run()
