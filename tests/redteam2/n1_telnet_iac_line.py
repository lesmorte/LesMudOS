#!/usr/bin/env python3
"""N1: an unauthenticated connection grows one input line without bound. Connection.ReadLinesAsync caps a line at MaxLineLength
(4096) for ordinary bytes, but the escaped data byte IAC IAC (255 255) is appended with `line.Add(255)` and no length check
(src/LesMudOS.Driver/Net/TelnetServer.cs, the `TelnetState.Iac when b == 255` case). Anyone who can reach the port, before logging
in, sends IAC IAC pairs with no newline and the driver's memory grows by one byte per two sent, until the process dies.
Measured: the driver's resident memory before and after 200 MB of IAC IAC (100 MB of line), with no newline sent.

Expected after a fix: memory does not grow by the amount sent."""
import os
import socket
import time
import rt2

# This proof is about the line cap alone: the byte rate limit (which delays what it reads, and would hold a flood back before the line grew) is
# switched off for this driver, so the cap is what is tested.
os.environ["LESMUDOS_INPUT_BYTES_PER_SECOND"] = "0"


def rss_mb(pid):
    for line in open(f"/proc/{pid}/status"):
        if line.startswith("VmRSS:"):
            return int(line.split()[1]) / 1024
    return 0


def main():
    g = rt2.Game()
    g.start()
    try:
        time.sleep(2)
        before = rss_mb(g.proc.pid)
        s = socket.create_connection(("127.0.0.1", rt2.rtlib.h.PORT))
        chunk = b"\xff\xff" * (1 << 20)  # 2 MB of IAC IAC: 1 MB of line
        for _ in range(100):
            s.sendall(chunk)
        time.sleep(3)
        after = rss_mb(g.proc.pid)
        print(f"driver RSS {before:.0f} MB -> {after:.0f} MB after one anonymous connection sent 200 MB of IAC IAC")
        rt2.check("N1: an unauthenticated connection cannot grow the driver's memory without bound", after - before < 50, f"+{after - before:.0f} MB")
        s.close()
    finally:
        g.stop()
    rt2.finish()


if __name__ == "__main__":
    main()
