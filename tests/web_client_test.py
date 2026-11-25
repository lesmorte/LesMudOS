#!/usr/bin/env python3
"""End-to-end test of the browser client's bridge (docs/specs/web-client.md sections 2 and 12.1).

Starts the driver on a temporary copy of the mudlib and the website (the built LesMudOS.Web.dll) with the browser client switched on in
PLAIN mode (the bridge connects to the game's ordinary telnet port and sends no PROXY line: use Site:Play:PlainMode only for tests like this
one; the PROXY line itself is covered by the website's own tests against a fake listener). Then a headless WebSocket client (the Python
`websockets` package) does what a browser would: refused from a wrong origin, a new account through a masked password, a few commands, a
second login of the same name, a tell from a telnet player, hostile text, pings that must not count as player input, the idle timeout,
a flood of connections, and quit.

Usage (from the repository root, after `dotnet build` of src/LesMudOS.Driver and web/LesMudOS.Web, and `pip install websockets`):
    LESMUDOS_TEST_PORT=5120 LESMUDOS_WEB_TEST_PORT=5140 python3 tests/web_client_test.py
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


def start_processes(work, mudlib):
    env = {**os.environ, "LESMUDOS_ADMIN_SECRET": h.SECRET, "LESMUDOS_IDLE_MINUTES": "5s", "LESMUDOS_TIMEOUT_MINUTES": "12s"}
    driver_dll = os.path.join(h.ROOT, "src", "LesMudOS.Driver", "bin", "Debug", "net10.0", "LesMudOS.Driver.dll")
    dlog = open(os.path.join(work, "driver.log"), "w")
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
            "Site__BaseUrl": ORIGIN, "Site__SaveDirectory": "", "Site__Play__Enabled": "true", "Site__Play__PlainMode": "true",
            "Site__Play__GameBridgePort": str(GAME_PORT), "Site__Play__MaxConnections": os.environ.get("WEB_TEST_MAX_CONNECTIONS", "10"),
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
    b = await Web.open("203.0.113.10")
    await b.until("known? ")
    await b.say("webtest")
    t = await b.until("Another name? ")
    check("a second web login of the same name is refused by the game", "already here" in t, t)
    await b.ws.close()

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
    work = tempfile.mkdtemp(prefix="lesmudos-webtest-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    driver, web = start_processes(work, mudlib)
    try:
        if "--load" in sys.argv:
            asyncio.run(load(int(sys.argv[sys.argv.index("--load") + 1]), work, driver, web))
        else:
            asyncio.run(main(work, mudlib, driver, web))
    finally:
        stop(web)
        stop(driver)
        if failures:
            print(f"logs kept in {work}")
        else:
            shutil.rmtree(work, ignore_errors=True)
    print(f"\n{checks[0]} checks, {len(failures)} failures")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    run()
