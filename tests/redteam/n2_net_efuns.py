#!/usr/bin/env python3
"""The network efuns and the login throttle against the wizard sandbox (docs/SANDBOX.md, docs/specs/login-throttle.md, net-int NOTES 2a, 2b).

  a. ConnectionAddress, AddressTag, ThrottleSetting and ThrottleLog are refused to sandbox code (a /wiz object, and a /wiz object built on
     the real /secure/login or /secure/throttle: the check is on the real path AND the domain), to real code that is not the login clone or
     the throttle blueprint, and nothing a wizard can do writes a line of its own choosing into the driver log.
  b. The throttle's functions cannot be used from sandbox code (no failure is recorded against a name or an address, no lock is lifted),
     a level 1-9 wizard cannot list or clear locks, a level 10 can; knownAddresses survive a workshop checkpoint and restore without loss or
     duplicate and are not readable or writable by sandbox code; the salt file <save>/address-secret is not reachable by cat, cp, ls or the
     file efuns at any level.

Expected: every check passes (FAIL means the hole is open)."""
import json
import os
import re
import time
import rtlib
import smoke_test as h

# This script needs the throttle ON (the shared defaults switch it off) and the bridge listener, to give each login its own address.
for _k in [k for k in os.environ if k.startswith("LESMUDOS_THROTTLE_")]:
    del os.environ[_k]
BRIDGE = h.PORT + 1
os.environ.update({"LESMUDOS_BRIDGE_PORT": str(BRIDGE), "LESMUDOS_THROTTLE_FAIL_DELAY_MS": "0", "LESMUDOS_MAX_PER_ADDRESS": "0",
                   "LESMUDOS_MAX_CONNECTIONS": "0", "LESMUDOS_CONNECTS_PER_MINUTE": "0"})
import net_test as n  # noqa: E402  (after the environment: it reads the port)
import throttle_test as t  # noqa: E402

SPY = r'''// @inherit /std/item
public class WrenSpy : Item
{
    private string Try(string label, Func<object?> f)
    {
        try { var r = f(); return label + "=OK(" + (r ?? "null") + ")"; }
        catch (Exception e) { return label + "=REFUSED"; }
    }
    public override void Create() { Name = "spy"; Ids.Add("spy"); ShortDesc = "a sandbox spy"; }
    public override void Init()
    {
        base.Init();
        AddAction("rtefun", a =>
        {
            Write("EFUN " + Try("addr", () => ConnectionAddress()) + " " + Try("tag", () => AddressTag("203.0.113.9")) + " " +
                  Try("set", () => ThrottleSetting("NameFailures")) + " " + Try("log", () => { ThrottleLog("FORGED-BY-SANDBOX"); return "logged"; }) + "\n");
            return true;
        });
        // Objects of the sandbox built on the real programs: the program path is /wiz/..., the base class is the real one.
        AddAction("rtcopy", a =>
        {
            var login = CloneObject("/wiz/wren/fakelogin");
            var thr = CloneObject("/wiz/wren/fakethrottle");
            Write("COPY login=" + (login?.Call<string>("Probe") ?? "null") + " throttle=" + (thr?.Call<string>("Probe") ?? "null") + "\n");
            return true;
        });
        // The real daemon's own functions, called from the sandbox: nothing may be recorded, lifted or learned.
        AddAction("rtreal", a =>
        {
            var th = LoadObject("/secure/throttle");
            var r = new List<string>();
            for (var i = 0; i < 7; i++) r.Add("" + th.Call("Failed", "Quin", "203.0.113.99", false));
            r.Add("tag=" + th.Call("Tag", "203.0.113.99"));
            r.Add("blocked=" + th.Call("AddressBlocked", "203.0.113.99"));
            r.Add("begin=" + th.Call("BeginPassword", "Vic", "203.0.113.5", true));
            th.Call("Succeeded", "Vic", "203.0.113.5");
            r.Add("cmd=" + th.Call("Command", "clear all"));
            th.Call("Created", "203.0.113.99");
            Write("REAL " + string.Join("|", r) + "\n");
            return true;
        });
        // The owner's body, in its own workshop: the one real object sandbox code may call freely.
        AddAction("rtnote", a =>
        {
            var me = ThisPlayer()!;
            Write("NOTE knows=" + me.Call("KnowsAddress", "aaaaaaaaaaaaaaaa") + " note=" + me.Call("NoteAddress", "bbbbbbbbbbbbbbbb") + " throttle=" + me.Call("CmdThrottle", "") + "\n");
            return true;
        });
        AddAction("rtfile", a =>
        {
            var r = new List<string>();
            foreach (var p in new[] { "/../save/address-secret", "../save/address-secret", "/address-secret", "/../save", "/players", "/../save/players/wren.json" })
            {
                try { var t = ReadFile(p); r.Add("read(" + p + ")=" + (t == null ? "null" : "TEXT" + t.Length)); } catch (Exception e) { r.Add("read(" + p + ")=REFUSED"); }
                try { var d = GetDir(p); r.Add("dir(" + p + ")=" + (d == null ? "null" : "LIST" + d.Count)); } catch (Exception e) { r.Add("dir(" + p + ")=REFUSED"); }
            }
            Write("FILE " + string.Join(" ", r) + "\n");
            return true;
        });
    }
}
'''

FAKE_LOGIN = r'''// @inherit /secure/login
public class FakeLogin : Login
{
    public string Probe()
    {
        string a;
        try { a = "addr=OK(" + (ConnectionAddress() ?? "null") + ")"; } catch (Exception e) { a = "addr=REFUSED"; }
        return a;
    }
}
'''

FAKE_THROTTLE = r'''// @inherit /secure/throttle
public class FakeThrottle : Throttle
{
    public string Probe()
    {
        var r = new List<string>();
        try { r.Add("tag=" + AddressTag("203.0.113.9")); } catch (Exception e) { r.Add("tag=REFUSED"); }
        try { r.Add("set=" + ThrottleSetting("NameFailures")); } catch (Exception e) { r.Add("set=REFUSED"); }
        try { ThrottleLog("FORGED-BY-COPY"); r.Add("log=logged"); } catch (Exception e) { r.Add("log=REFUSED"); }
        try { r.Add("delay=" + QueryDelayMs()); } catch (Exception e) { r.Add("delay=REFUSED"); }
        return string.Join(",", r);
    }
}
'''

# Real code that is neither the login clone nor the throttle blueprint (a level 10 wrote it): the master's answer is by program, not by trust.
REAL_SPY = r'''// @inherit /std/item
public class RtRealSpy : Item
{
    private string Try(string label, Func<object?> f)
    {
        try { var r = f(); return label + "=OK(" + (r ?? "null") + ")"; }
        catch (Exception e) { return label + "=REFUSED"; }
    }
    public override void Create() { Name = "rspy"; Ids.Add("rspy"); ShortDesc = "a real spy"; }
    public override void Init()
    {
        base.Init();
        AddAction("rtrspy", a =>
        {
            Write("RSPY " + Try("addr", () => ConnectionAddress()) + " " + Try("tag", () => AddressTag("203.0.113.9")) + " " +
                  Try("set", () => ThrottleSetting("NameFailures")) + " " + Try("log", () => { ThrottleLog("FORGED-BY-REAL-SPY"); return "logged"; }) + "\n");
            return true;
        });
    }
}
'''


def tags_in(g, name):
    path = os.path.join(g.work, "save", "players", name.lower() + ".json")
    if not os.path.exists(path):
        return None
    found = []

    def walk(o):
        if isinstance(o, dict):
            for k, v in o.items():
                if k == "knownAddresses":
                    found.append(v)
                else:
                    walk(v)
        elif isinstance(o, list):
            for x in o:
                walk(x)
    walk(json.load(open(path)))
    return found[0] if found else None


def main():
    g = rtlib.Game(wiz_files={"/wiz/wren/spy": SPY, "/wiz/wren/fakelogin": FAKE_LOGIN, "/wiz/wren/fakethrottle": FAKE_THROTTLE},
                   real_files={"/domains/rt/rspy": REAL_SPY})
    g.start()
    try:
        g.setup_people(extra=[("nine", 9)])
        chief, wren = g.chief, g.wren
        secret_file = os.path.join(g.work, "save", "address-secret")

        # ---- two characters made through the bridge, each from an address of its own, and one with a lock on it
        h.check("setup: Vic is made", t.create("Vic", "203.0.113.5")[0], "created")
        h.check("setup: Quin is made", t.create("Quin", "203.0.113.99")[0], "created")
        outs = t.wrong("Vic", 5, "198.51.100.77")
        listing = chief.command("throttle")
        rtlib.check("positive control: the real login does record failures (Vic is locked, the address is counted)", "name    Vic" in listing, listing)
        rtlib.check("the throttle log line names the address the bridge gave", "name locked: Vic (from 198.51.100.77)" in g.driver_log(), g.driver_log()[-600:])

        # ---- a. the efuns
        wren.command("goto /wiz/wren/lab")
        wren.command("clone /wiz/wren/spy")
        out = wren.command("rtefun")
        print("rtefun:", out.strip()[:200])
        rtlib.check("N2a: sandbox code gets all four efuns refused", "EFUN addr=REFUSED tag=REFUSED set=REFUSED log=REFUSED" in out, out)
        out = wren.command("rtcopy")
        print("rtcopy:", out.strip()[:260])
        rtlib.check("N2a: a /wiz object built on the real /secure/login is refused ConnectionAddress", "COPY login=addr=REFUSED" in out, out)
        rtlib.check("N2a: a /wiz object built on the real /secure/throttle is refused tag, setting and log, and its own setting read fails",
                    "tag=REFUSED,set=REFUSED,log=REFUSED,delay=REFUSED" in out, out)
        out = chief.command("rtrspy")
        chief.command("clone /domains/rt/rspy")
        out = chief.command("rtrspy")
        print("rtrspy:", out.strip()[:200])
        rtlib.check("N2a: real code that is neither the login clone nor the throttle blueprint is refused all four",
                    "RSPY addr=REFUSED tag=REFUSED set=REFUSED log=REFUSED" in out, out)
        log = g.driver_log()
        rtlib.check("N2a: no line of a wizard's or a spy's choosing reaches the log through ThrottleLog",
                    "FORGED" not in log, [l for l in log.splitlines() if "FORGED" in l][:3])
        rtlib.check("N2a: the log lines the throttle writes are its own (every 'login throttle:' line is one of the daemon's three shapes)",
                    all(re.match(r"\d\d:\d\d:\d\d login throttle: (name locked: [A-Za-z]+ \(from [0-9a-f.:/]+\) for \d+ s|address locked: [0-9a-f.:/]+ for \d+ s|[A-Za-z]+ cleared by [A-Za-z]+|all throttle records cleared by [A-Za-z]+|default settings.*|LESMUDOS_THROTTLE_[A-Z_]+=\d+)$", l)
                        for l in log.splitlines() if "login throttle:" in l), [l for l in log.splitlines() if "login throttle:" in l][:6])

        # ---- b. the daemon
        out = wren.command("rtreal")
        print("rtreal:", out.strip()[:260])
        rtlib.check("N2b: sandbox code cannot call the daemon's recording or deciding functions (all return nothing)",
                    "REAL " + "|".join([""] * 7) + "|tag=|blocked=|begin=|cmd=" in out.replace("\r", "") or
                    re.search(r"REAL \|*\|tag=\|blocked=\|begin=\|cmd=", out) is not None, out)
        r = t.login("Quin", [t.GOOD], "203.0.113.99")
        rtlib.check("N2b: seven failures recorded 'by the sandbox' against Quin and his address changed nothing: he logs in", r[0] == "in", r)
        listing = chief.command("throttle")
        rtlib.check("N2b: ... and the level 10 list shows no lock on Quin or on his address", "Quin" not in listing.split("Login throttle")[-1] and "203.0.113.99" not in listing, listing)
        rtlib.check("N2b: ... while Vic's real lock is still there (sandbox code did not lift or clear it)", "name    Vic" in listing, listing)

        out = wren.command("throttle")
        out2 = wren.command("throttle clear Vic")
        out3 = wren.command("throttle clear all")
        rtlib.check("N2b: a level 1 wizard has no throttle command (nothing listed, nothing cleared)",
                    all("Login throttle" not in o and "Cleared" not in o for o in (out, out2, out3)), (out, out2, out3))
        chief.command("promote wren 9")
        out = wren.command("throttle")
        out2 = wren.command("throttle clear Vic")
        out3 = wren.command("throttle clear all")
        rtlib.check("N2b: a level 9 wizard has none either", all("Login throttle" not in o and "Cleared" not in o for o in (out, out2, out3)), (out, out2, out3))
        helptext = wren.command("help")
        rtlib.check("N2b: and `help` does not offer it", "throttle" not in helptext, helptext[-300:])
        listing = chief.command("throttle")
        rtlib.check("N2b: Vic's lock survived all of that", "name    Vic" in listing, listing)
        chief.command("promote wren 1")
        out = chief.command("throttle clear Vic")
        rtlib.check("N2b: a level 10 can clear it", "Cleared Vic" in out, out)
        nine = g.others["nine"]
        out = nine.command("throttle")
        rtlib.check("N2b: a second level 9 wizard: no command", "Login throttle" not in out, out)

        # ---- b. knownAddresses and the workshop
        wren.command("quit", marker="Goodbye", timeout=10)
        w = n.bridge_client("wren", n.proxy("198.51.100.61"))
        w.read_until("> ")
        base = tags_in(g, "wren")
        rtlib.check("N2b: wren's save lists known addresses as hashes only", isinstance(base, list) and len(base) >= 1 and all(re.fullmatch(r"[0-9a-f]{16}", x) for x in base)
                    and "198.51.100.61" not in open(os.path.join(g.work, "save", "players", "wren.json")).read(), base)
        w.command("goto /wiz/wren/lab")
        w.command("clone /wiz/wren/spy")
        before = tags_in(g, "wren")
        out = w.command("rtnote")
        print("rtnote:", out.strip()[:200])
        rtlib.check("N2b: sandbox code cannot learn from or add to the known addresses of the body it runs beside",
                    "NOTE knows=" in out and "knows=True" not in out and "note=" in out, out)
        w.command("goto /domains/hollow/village/square")  # restore from the checkpoint
        w.command("quit", marker="Goodbye", timeout=10)
        after = tags_in(g, "wren")
        rtlib.check("N2b: a workshop visit (checkpoint and restore) neither loses nor duplicates the list", before == after or (before is not None and after is not None and len(after) == len(set(after)) and set(before) <= set(after)), (before, after))
        w = n.bridge_client("wren", n.proxy("198.51.100.62"))
        w.read_until("> ")
        w.command("goto /wiz/wren/lab")
        w.command("quit", marker="Goodbye", timeout=10)  # dropped in the workshop: the save holds the checkpoint
        w = n.bridge_client("wren", n.proxy("198.51.100.63"))
        w.read_until("> ")
        w.command("quit", marker="Goodbye", timeout=10)
        final = tags_in(g, "wren")
        rtlib.check("N2b: after more logins (one dropped in the workshop) the list has no duplicate and at most three entries", final is not None and len(final) == len(set(final)) <= 3, final)
        rtlib.check("N2b: ... and the newest address is the last entry (the workshop did not lose the note of a login)", final is not None and final[-1] not in (base or []), (base, final))

        # ---- b. the salt file
        rtlib.check("N2b: the salt file exists, mode 0600, outside the mudlib", os.path.exists(secret_file) and oct(os.stat(secret_file).st_mode & 0o777) == "0o600"
                    and not os.path.abspath(secret_file).startswith(os.path.abspath(g.mudlib)), oct(os.stat(secret_file).st_mode) if os.path.exists(secret_file) else "missing")
        secret = open(secret_file).read().strip()
        seen = []
        for cmd in ("cat /address-secret", "cat address-secret", "cat /../save/address-secret", "cat ../save/address-secret", "cat /wiz/../../save/address-secret",
                    "cat /players/../address-secret", "cat /%2e%2e/save/address-secret", "cp /../save/address-secret /wiz/chief/stolen", "cp /address-secret /wiz/chief/stolen",
                    "ls /../save", "ls ..", "ls /", "ls /players", "cat /players/wren.json", "cat /players/wren"):
            seen.append((cmd, chief.command(cmd)))
        rtlib.check("N2b: no command of a level 10 shows the salt (each cat of it says there is no such file)", all(secret not in o for _, o in seen)
                    and all("no such" in o.lower() or "not" in o.lower() or "cannot" in o.lower() or "may not" in o.lower() for c, o in seen if c.startswith("cat") and "address-secret" in c),
                    [(c, o[:120]) for c, o in seen if secret in o][:3] or [(c, o[:100]) for c, o in seen if c.startswith("cat") and "address-secret" in c])
        rtlib.check("N2b: cp made no copy of it", not os.path.exists(os.path.join(g.mudlib, "wiz", "chief", "stolen")) and
                    not any(secret in open(os.path.join(dp, f), errors="ignore").read() for dp, _, fs in os.walk(g.mudlib) for f in fs if os.path.getsize(os.path.join(dp, f)) < 100000), "")
        rtlib.check("N2b: no listing of the save directory or of /players shows any of the hashed saves' files by those commands",
                    all("wren.json" not in o for c, o in seen if c.startswith("ls")), [(c, o[:100]) for c, o in seen if c.startswith("ls")])
        wren_ = n.bridge_client("wren", n.proxy("198.51.100.64"))
        wren_.read_until("> ")
        wren_.command("goto /wiz/wren/lab")
        wren_.command("clone /wiz/wren/spy")
        out = wren_.command("rtfile")
        print("rtfile:", out.strip()[:400])
        rtlib.check("N2b: sandbox file efuns read nothing of the save directory, the salt or /players", "FILE" in out and "TEXT" not in out and "LIST" not in out, out)
        wren_.command("quit", marker="Goodbye", timeout=10)
        rtlib.check("N2b: the salt is still exactly what it was (nothing rewrote it)", open(secret_file).read().strip() == secret, "")
    finally:
        g.stop()
    rtlib.finish()


if __name__ == "__main__":
    main()
