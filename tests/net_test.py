#!/usr/bin/env python3
"""Tests for the network limits of the driver (docs/specs/web-client.md section 4, docs/specs/net-limits.md if present).

Boots the driver four times on one temporary copy of the mudlib:

  1. with the loopback bridge listener on and small limits: the PROXY line (good, bad, late, oversize, split, with game bytes right
     behind it, sent to the PUBLIC port), the connection limits (per address, total, IPv6 /64, release, a flood of connects), the input
     bucket (burst, flood, steady rate, empty lines, bytes, sustained overflow) and the log lines;
  2. with a low connect rate and no bridge port: the rate limit, and nothing listens on the bridge port;
  3. with a bridge bind address that is not loopback: the listener refuses to start;
  4. with every limit at its production default: a wizard pastes 500 lines into the editor and the file arrives whole and in order.

Usage (from the repository root, after `dotnet build`):
    LESMUDOS_TEST_PORT=5100 python3 tests/net_test.py        (also uses the next port, for the bridge)
"""
import os
import re
import shutil
import socket
import subprocess
import sys
import tempfile
import time

os.environ.setdefault("LESMUDOS_TEST_PORT", "5100")
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import smoke_test as h  # noqa: E402

PORT = h.PORT
BRIDGE = PORT + 1
FULL = "The game is full. Please try again in a few minutes."
TOO_MANY = "Too many connections from your address."
TOO_FAST = "Too many connection attempts from your address."


def start(work, mudlib, n, env):
    path = os.path.join(work, f"driver{n}.log")
    log = open(path, "w")
    dll = os.path.join(h.ROOT, "src", "LesMudOS.Driver", "bin", "Debug", "net10.0", "LesMudOS.Driver.dll")
    base = {k: v for k, v in os.environ.items() if not k.startswith("LESMUDOS_") or k == "LESMUDOS_TEST_PORT"}
    proc = subprocess.Popen(["dotnet", dll, "--mudlib", mudlib, "--save", os.path.join(work, "save"), "--port", str(PORT)],
                            stdout=log, stderr=subprocess.STDOUT, env={**base, "LESMUDOS_ADMIN_SECRET": h.SECRET, **env})
    for _ in range(600):
        time.sleep(0.2)
        if "listening on port" in open(path).read() or proc.poll() is not None:
            break
    return proc, path


def stop(proc):
    if proc.poll() is None:
        proc.terminate()
        try:
            proc.wait(timeout=10)
        except subprocess.TimeoutExpired:
            proc.kill()


def logtext(path):
    return open(path).read()


def lines_with(path, needle):
    return [l for l in logtext(path).splitlines() if needle in l]


def connect(port=PORT, preface=b"", host="127.0.0.1"):
    s = socket.create_connection((host, port), timeout=5)
    s.settimeout(0.2)
    if preface:
        s.sendall(preface)
    return s


def read(s, until=None, timeout=3.0):
    """Everything received until `until` appears (bytes), the peer closes, or the timeout. Returns (data, closed)."""
    end = time.time() + timeout
    data = b""
    while time.time() < end:
        try:
            chunk = s.recv(65536)
        except socket.timeout:
            chunk = None
        except OSError:
            return data, True
        if chunk == b"":
            return data, True
        if chunk:
            data += chunk
            if until and until in data:
                return data, False
    return data, False


def proxy(ip="203.0.113.7", v6=False, dst=None):
    if v6:
        return f"PROXY TCP6 {ip} {dst or '::1'} 0 4001\r\n".encode()
    return f"PROXY TCP4 {ip} {dst or '127.0.0.1'} 0 4001\r\n".encode()


def bridge_client(name, line, **kw):
    """The shared test client, but connected to the bridge port behind a PROXY line."""
    real = socket.create_connection

    def fake(address, timeout=None, **k):
        s = real(("127.0.0.1", BRIDGE), timeout=timeout)
        s.sendall(line)
        return s

    socket.create_connection = fake
    try:
        return h.Client(name, **kw)
    finally:
        socket.create_connection = real


def settle(seconds=0.6):
    time.sleep(seconds)


def banner(s):
    data, closed = read(s, b"known?", 5)
    return b"known?" in data, data, closed


def editor_paste(work, mudlib, n=4):
    """A wizard pasting a 500 line file into the in-game editor with every limit at its production default (20 lines a second, burst 60):
    the lines over the limit are delayed, never dropped, so the file arrives whole and in order."""
    proc, log = start(work, mudlib, n, {})
    try:
        h.check("the production input limit is on in this boot", logtext(log), "input 20 lines/s (burst 60)")
        c = h.Client("scribe")
        c.read_until("> ")
        c.command("promote " + h.SECRET)
        c.command("mkdir /wiz/scribe")
        body = [f"line {i:03d} {chr(124)}{'x' * (i % 37)}" for i in range(500)]
        h.check("the editor opens", c.command("edit /wiz/scribe/paste.cs", marker=":"), "You are in the editor")
        c.command("a", marker="End with")
        t0 = time.time()
        c.sock.sendall(("\r\n".join(body) + "\r\n.\r\nx\r\n").encode())
        text = c.read_until("Left the editor", 90)
        took = time.time() - t0
        h.check("the paste ends with the file written and the editor left", text, "Wrote /wiz/scribe/paste.cs, 500 lines", "Left the editor")
        h.check("... the limit held (it took more than 15 s, the burst alone is 60 lines)", str(took > 15), "True")
        h.check("... the player was told once that input is being held", str(text.count("Input is arriving faster than the game reads it")), "1")
        path = os.path.join(mudlib, "wiz", "scribe", "paste.cs")
        got = open(path).read().splitlines() if os.path.exists(path) else []
        h.check("the file on disk is the 500 lines, complete and in order", str(got == body), "True")
        c.command("quit")
    finally:
        stop(proc)


def main():
    work = tempfile.mkdtemp(prefix="lesmudos-net-")
    mudlib = os.path.join(work, "mudlib")
    h.copy_mudlib(mudlib)
    proc = None
    try:
        # ------------------------------------------------------------------ boot 1
        proc, log = start(work, mudlib, 1, {
            "LESMUDOS_BRIDGE_PORT": str(BRIDGE), "LESMUDOS_MAX_CONNECTIONS": "12", "LESMUDOS_MAX_PER_ADDRESS": "4",
            "LESMUDOS_CONNECTS_PER_MINUTE": "0", "LESMUDOS_INPUT_OVERFLOW_SECONDS": "8", "LESMUDOS_BRIDGE_HANDSHAKE_SECONDS": "1.5",
            "LESMUDOS_INPUT_STATS": "1"})
        h.check("the driver says what it listens on", logtext(log), f"bridge listening on 127.0.0.1:{BRIDGE}", "loopback only", "network limits: 12 connections, 4 per address")

        # ---- the PROXY line: a good one, played through to the game
        c = bridge_client("pia", proxy("203.0.113.7"))
        h.check("a bridge connection with a good TCP4 line logs in", c.read_until("> "), "Welcome, Pia")
        h.check("the log shows the claimed address and (web)", "\n".join(lines_with(log, "connection from")), "connection from 203.0.113.7 (web)")
        h.check("the claimed address is not the loopback one in that line", "\n".join(l for l in lines_with(log, "connection from") if "203.0.113.7" in l), absent=("127.0.0.1",))
        c.command("quit")
        settle()
        h.check("the ended line has the claimed address, the player and (web)", "\n".join(lines_with(log, "connection ended:")), "connection ended: 203.0.113.7 (web), player Pia, lasted")

        c6 = bridge_client("pip", proxy("2001:db8::1", v6=True))
        h.check("a good TCP6 line logs in", c6.read_until("> "), "Welcome, Pip")
        h.check("the TCP6 address is logged", "\n".join(lines_with(log, "connection from")), "connection from 2001:db8::1 (web)")
        c6.command("quit")
        mapped = bridge_client("pim", proxy("::ffff:198.51.100.9", v6=True))
        h.check("an IPv4-mapped address in a TCP6 line is logged as IPv4", (mapped.read_until("> "), "\n".join(lines_with(log, "connection from")))[1], "connection from 198.51.100.9 (web)")
        mapped.command("quit")

        # ---- the game bytes right behind the line, in the same packet, and a line split over several packets
        s = connect(BRIDGE, proxy("203.0.113.20") + b"carol\r\n")
        data, _ = read(s, b"Choose a password", 6)
        h.check("game bytes sent in the same packet as the PROXY line are not lost", data.decode(errors="replace"), "Creating a new account for Carol")
        s.close()
        s = connect(BRIDGE)
        for piece in (b"PROX", b"Y TCP4 203.0.1", b"13.21 127.0", b".0.1 0 4001", b"\r", b"\n"):
            s.sendall(piece)
            time.sleep(0.1)
        ok, data, _ = banner(s)
        h.check("a line split over many packets is accepted", str(ok), "True")
        h.check("and its address is used", "\n".join(lines_with(log, "connection from")), "connection from 203.0.113.21 (web)")
        s.close()

        # ---- bad lines: closed with no reply at all and nothing created
        bad = [
            ("UNKNOWN with the fields of a real line", b"PROXY UNKNOWN 1.2.3.4 127.0.0.1 0 4001\r\n", "unsupported protocol"),
            ("UNKNOWN alone", b"PROXY UNKNOWN\r\n", "not a PROXY line"),
            ("not a PROXY line", b"GET / HTTP/1.1\r\n", "not a PROXY line"),
            ("lower case", b"proxy TCP4 1.2.3.4 127.0.0.1 0 4001\r\n", "not a PROXY line"),
            ("a bare LF", b"PROXY TCP4 1.2.3.4 127.0.0.1 0 4001\n", "no CRLF"),
            ("address that does not parse", b"PROXY TCP4 999.1.1.1 127.0.0.1 0 4001\r\n", "bad address"),
            ("IPv6 address behind TCP4", b"PROXY TCP4 2001:db8::1 127.0.0.1 0 4001\r\n", "bad address"),
            ("IPv4 address behind TCP6", b"PROXY TCP6 1.2.3.4 ::1 0 4001\r\n", "bad address"),
            ("abbreviated IPv4", b"PROXY TCP4 1.2 127.0.0.1 0 4001\r\n", "bad address"),
            ("leading zero in an address", b"PROXY TCP4 01.2.3.4 127.0.0.1 0 4001\r\n", "bad address"),
            ("a zone id", b"PROXY TCP6 fe80::1%eth0 ::1 0 4001\r\n", "bad address"),
            ("missing port", b"PROXY TCP4 1.2.3.4 127.0.0.1 0\r\n", "not a PROXY line"),
            ("port too big", b"PROXY TCP4 1.2.3.4 127.0.0.1 0 65536\r\n", "bad port"),
            ("two spaces", b"PROXY  TCP4 1.2.3.4 127.0.0.1 0 4001\r\n", "not a PROXY line"),
            ("an unspecified source", b"PROXY TCP4 0.0.0.0 127.0.0.1 0 4001\r\n", "unusable"),
            ("a multicast source", b"PROXY TCP4 224.0.0.1 127.0.0.1 0 4001\r\n", "unusable"),
            ("a control byte", b"PROXY TCP4 1.2.3.4\x01 127.0.0.1 0 4001\r\n", "bad character"),
            ("108 bytes", b"PROXY TCP4 1.2.3.4 127.0.0.1 0 " + b"1" * 76 + b"\r\n", "too long"),
        ]
        for label, line, _reason in bad:
            s = connect(BRIDGE, line)
            data, closed = read(s, None, 3)
            h.check(f"bad PROXY line ({label}): closed, nothing sent", f"closed={closed} data={data!r}", "closed=True data=b''")
            s.close()
        h.check("a bad line is logged as such, with the reason", "\n".join(lines_with(log, "bridge connection refused")), "bad PROXY line (unsupported protocol)", "bad PROXY line (not a PROXY line)")
        refused_logged = len(lines_with(log, "bridge connection refused"))
        h.check("the log lines about bad lines are rate limited (10 a minute)", str(refused_logged <= 10), "True")
        h.check("a hostile line is never copied into the log", logtext(log), absent=("HTTP/1.1", "eth0"))

        s = connect(BRIDGE, b"PROXY TCP4 1.2.3.4 127.0.0.1 0 4001 " + b"x" * 150)
        data, closed = read(s, None, 3)
        h.check("an oversize line with no end is closed", f"closed={closed} data={data!r}", "closed=True data=b''")
        s.close()
        s = connect(BRIDGE, proxy("203.0.113.22")[:-2] + b"\r")
        t0 = time.time()
        data, closed = read(s, None, 5)
        h.check("a line that never ends is closed after the handshake time", f"closed={closed} data={data!r}", "closed=True data=b''")
        h.check("... after about 1.5 seconds, not at once and not never", str(1.0 < time.time() - t0 < 4.0), "True")
        s.close()
        s = connect(BRIDGE)
        data, closed = read(s, None, 4)
        h.check("no line at all: closed after the handshake time", f"closed={closed} data={data!r}", "closed=True data=b''")
        s.close()

        # ---- the PROXY line sent to the PUBLIC port is just typing
        s = connect(PORT)
        ok, data, _ = banner(s)
        s.sendall(b"PROXY TCP4 9.9.9.9 127.0.0.1 0 4000\r\n")
        data, _ = read(s, b"Try again", 3)
        h.check("a PROXY line on the public port is refused as a name", data.decode(errors="replace"), "Names are 2 to 16 letters")
        s.sendall(b"PROXY TCP4 9.9.9.9 127.0.0.1 0 4000\r\n" * 3)
        s.close()
        settle()
        h.check("it never changes the address the log shows", logtext(log), absent=("9.9.9.9",))
        h.check("the public connection is logged with its real address", "\n".join(lines_with(log, "connection ended:"))[-300:], "connection ended: 127.0.0.1:")

        # ---- connection limits
        settle(1.0)
        socks = [connect(PORT) for _ in range(4)]
        oks = [banner(x)[0] for x in socks]
        h.check("four connections from one address are fine", str(oks), "[True, True, True, True]")
        fifth = connect(PORT)
        data, closed = read(fifth, None, 3)
        h.check("the fifth gets the one-line refusal", data.decode(errors="replace"), TOO_MANY)
        h.check("... is closed, and was never given a login prompt", f"closed={closed} {data.decode(errors='replace')}", "closed=True", absent=("known?",))
        fifth.close()
        socks[0].sendall(b"zed\r\n")
        data, _ = read(socks[0], b"Creating", 5)
        h.check("the connections already in are unaffected", data.decode(errors="replace"), "Creating a new account for Zed")
        h.check("the refusal is logged", "\n".join(lines_with(log, "connection refused")), "connection refused: too many from address: 127.0.0.1")
        socks[1].close()
        settle()
        again = connect(PORT)
        h.check("a connection closing frees a slot", str(banner(again)[0]), "True")
        again.close()
        for x in socks:
            x.close()
        settle(1.0)

        # a web connection counts under its claimed address, not under 127.0.0.1
        web = [connect(BRIDGE, proxy("203.0.113.50")) for _ in range(4)]
        h.check("four web connections from one claimed address are fine", str([banner(x)[0] for x in web]), "[True, True, True, True]")
        extra = connect(BRIDGE, proxy("203.0.113.50"))
        data, _ = read(extra, None, 3)
        h.check("the fifth from the same claimed address is refused", data.decode(errors="replace"), TOO_MANY)
        extra.close()
        h.check("... and logged with the claimed address", "\n".join(lines_with(log, "connection refused")), "too many from address: 203.0.113.50 (web)")
        pub = connect(PORT)
        h.check("the loopback address has its own count", str(banner(pub)[0]), "True")
        pub.close()
        other = connect(BRIDGE, proxy("203.0.113.51"))
        h.check("another claimed address is not affected", str(banner(other)[0]), "True")
        other.close()
        for x in web:
            x.close()
        settle(1.0)

        # IPv6 is counted by /64
        v6 = [connect(BRIDGE, proxy(f"2001:db8:1:2::{i}", v6=True)) for i in range(1, 5)]
        h.check("four addresses of one /64 are fine", str([banner(x)[0] for x in v6]), "[True, True, True, True]")
        extra = connect(BRIDGE, proxy("2001:db8:1:2:ffff:ffff:ffff:ffff", v6=True))
        data, _ = read(extra, None, 3)
        h.check("a fifth address of the same /64 is refused", data.decode(errors="replace"), TOO_MANY)
        extra.close()
        diff = connect(BRIDGE, proxy("2001:db8:1:3::1", v6=True))
        h.check("another /64 is not affected", str(banner(diff)[0]), "True")
        diff.close()
        for x in v6:
            x.close()
        settle(1.0)

        # the total cap: 12 connections, three addresses of four
        held = []
        for a in (60, 61, 62):
            held += [connect(BRIDGE, proxy(f"203.0.113.{a}")) for _ in range(4)]
        h.check("twelve connections are admitted", str(all(banner(x)[0] for x in held)), "True")
        late = connect(BRIDGE, proxy("203.0.113.63"))
        data, closed = read(late, None, 3)
        h.check("the 13th gets the game-is-full line", data.decode(errors="replace"), FULL)
        late.close()
        h.check("... and the log says full", "\n".join(lines_with(log, "connection refused")), "connection refused: full: 203.0.113.63 (web)")
        held[0].close()
        settle()
        after = connect(BRIDGE, proxy("203.0.113.63"))
        h.check("a closing connection makes room", str(banner(after)[0]), "True")
        after.close()
        for x in held:
            x.close()
        settle(1.0)

        # a flood of connect and disconnect cycles leaves nothing behind
        for _ in range(150):
            x = connect(PORT)
            x.close()
        settle(2.0)
        socks = [connect(PORT) for _ in range(4)]
        h.check("after 150 connect/disconnect cycles all four slots are free again", str([banner(x)[0] for x in socks]), "[True, True, True, True]")
        for x in socks:
            x.close()
        settle(1.0)
        flood_before = len(lines_with(log, "connection refused"))
        socks = [connect(PORT) for _ in range(4)]
        for _ in range(60):
            x = connect(PORT)
            read(x, None, 0.05)
            x.close()
        h.check("a flood of refused connections writes at most 10 log lines a minute", str(len(lines_with(log, "connection refused")) - flood_before <= 10), "True")
        for x in socks:
            x.close()
        settle(1.0)

        # ---- the input bucket (a name prompt answers every line it receives)
        def prompt_client():
            x = connect(PORT)
            banner(x)
            return x

        def count_replies(x, timeout=3.0):
            data, closed = read(x, None, timeout)
            return data.decode(errors="replace").count("Names are 2 to 16 letters"), data.decode(errors="replace"), closed

        x = prompt_client()
        x.sendall(b"x\r\n" * 50)
        n, text, _ = count_replies(x)
        h.check("50 lines in one burst all arrive", f"{n} replies", "50 replies")
        h.check("... without a warning", text, absent=("Too much input",))
        x.close()

        x = prompt_client()
        t0 = time.time()
        x.sendall(b"x\r\n" * 1000)
        n, text, _ = count_replies(x, 3)
        elapsed = time.time() - t0
        h.check("1000 lines in a burst: only about the burst (60) plus the refill are read in the first seconds", str(55 <= n <= 60 + int(20 * elapsed) + 5), "True")
        h.check("... and the player is told once that input is being held, not lost", str(text.count("Input is arriving faster than the game reads it. Nothing is lost")), "1")
        h.check("... and nothing says input is dropped", text, absent=("some of what you typed is being ignored",))
        x.close()

        # Delayed, never dropped: 150 lines at once (90 over the burst, 4.5 s at 20 a second) all arrive, in order, only slower.
        x = prompt_client()
        t0 = time.time()
        x.sendall(b"".join(b"line%d\r\n" % i for i in range(150)))
        data, closed = read(x, None, 9)
        text = data.decode(errors="replace")
        h.check("150 lines at once: every one is answered (none dropped)", str(text.count("Names are 2 to 16 letters")), "150")
        h.check("... the connection stays open", str(closed), "False")
        h.check("... and it took longer than a burst would (the limit held)", str(time.time() - t0 > 3.5), "True")
        x.close()

        x = prompt_client()
        for _ in range(40):
            x.sendall(b"x\r\n")
            time.sleep(0.1)
        n, text, _ = count_replies(x, 1)
        h.check("a steady 10 lines a second is untouched", f"{n} replies", "40 replies")
        h.check("... with no notice", text, absent=("Input is arriving faster",))
        x.close()

        x = prompt_client()
        x.sendall(b"\r\n" * 300)
        _, text, _ = count_replies(x, 2)
        h.check("empty lines count against the limit", text, "Input is arriving faster than the game reads it")
        x.close()

        x = prompt_client()
        sent = 0
        for _ in range(150):
            try:
                x.sendall(b"a" * 2000)
                sent += 2000
            except socket.timeout:
                break
        _, text, _ = count_replies(x, 2)
        h.check("a flood of bytes is limited too (300 KB against a 256 KB burst)", text, "Input is arriving faster than the game reads it")
        x.close()

        x = prompt_client()
        end = time.time() + 14
        closed = False
        text = ""
        while time.time() < end and not closed:
            try:
                x.sendall(b"x\r\n" * 50)
            except socket.timeout:
                pass
            except OSError:
                closed = True
                break
            time.sleep(0.2)
            try:
                chunk = x.recv(65536)
                if chunk == b"":
                    closed = True
                text += chunk.decode(errors="replace")
            except socket.timeout:
                pass
            except OSError:
                closed = True
        h.check("a flood that never lets up ends the connection after the overflow time (8 s here)", str(closed), "True")
        h.check("... with a goodbye", text, "Too much input. Goodbye.")
        x.close()
        settle()
        h.check("the log says so, once, without anything typed", "\n".join(lines_with(log, "input limit")), "input limit: holding input from 127.0.0.1:", "input limit: disconnecting 127.0.0.1:")
        h.check("the connection ended line says why", "\n".join(lines_with(log, "connection ended:")), "closed by the game (too much input)")
        h.check("input limit log lines are rate limited", str(len(lines_with(log, "input limit")) <= 10), "True")

        # the shared client and a pasted burst of commands (60 lines is the burst) are not hurt
        c = h.Client("burst")
        c.read_until("> ")
        c.sock.sendall(b"".join(f"sync a{i}\r\n".encode() for i in range(55)))
        text = c.read_until("[sync a54]", 10)
        h.check("55 commands pasted at once all run", str(all(f"[sync a{i}]" in text for i in range(55))), "True")
        c.command("quit")
        settle()
        h.check("the peak input rate is measured when asked", "\n".join(lines_with(log, "input stats")), "peak ", "lines/s")
        stop(proc)
        proc = None

        # ------------------------------------------------------------------ boot 2
        proc, log = start(work, mudlib, 2, {"LESMUDOS_CONNECTS_PER_MINUTE": "6", "LESMUDOS_MAX_PER_ADDRESS": "0", "LESMUDOS_MAX_CONNECTIONS": "0"})
        try:
            socket.create_connection(("127.0.0.1", BRIDGE), timeout=1).close()
            listening = True
        except OSError:
            listening = False
        h.check("without a bridge port nothing listens on it", str(listening), "False")
        h.check("the log says the bridge is off", logtext(log), "bridge off")
        results = []
        for _ in range(9):
            x = connect(PORT)
            d, _ = read(x, b"known?", 2)
            results.append("known?" in d.decode(errors="replace"))
            if not results[-1]:
                h.check("a connection past the rate gets the one-line refusal", d.decode(errors="replace"), TOO_FAST)
            x.close()
        h.check("six connections in a minute are admitted and the rest refused", str(results), "[True, True, True, True, True, True, False, False, False]")
        h.check("the refusal is logged", "\n".join(lines_with(log, "connection refused")), "connection refused: connecting too fast: 127.0.0.1")
        stop(proc)
        proc = None

        # ------------------------------------------------------------------ boot 3
        proc, log = start(work, mudlib, 3, {"LESMUDOS_BRIDGE_PORT": str(BRIDGE), "LESMUDOS_BRIDGE_BIND": "0.0.0.0"})
        h.check("a bridge bind address that is not loopback is refused", logtext(log), "bridge listener NOT started: it may listen on a loopback address only")
        try:
            socket.create_connection(("127.0.0.1", BRIDGE), timeout=1).close()
            listening = True
        except OSError:
            listening = False
        h.check("... and nothing listens", str(listening), "False")
        x = connect(PORT)
        h.check("the game itself runs normally", str(banner(x)[0]), "True")
        x.close()
        stop(proc)
        proc = None

        # ------------------------------------------------------------------ boot 4
        editor_paste(work, mudlib)
    finally:
        if proc is not None:
            stop(proc)
        if h.failures:
            for n in (1, 2, 3, 4):
                p = os.path.join(work, f"driver{n}.log")
                if os.path.exists(p):
                    print(f"--- driver{n}.log (tail)\n" + open(p).read()[-2500:])
        shutil.rmtree(work, ignore_errors=True)
    print()
    if h.failures:
        print(f"{len(h.failures)} FAILED:", *h.failures, sep="\n  ")
        return 1
    print("all net checks passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
