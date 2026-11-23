"""Shared harness for the red-team scripts: a private driver on a copy of the mudlib, a level 10 (chief), an ordinary player
(pat, the victim, standing in the real village square) and a level 1 wizard (wren, the attacker). Follows tests/sandbox_test.py.

Each script prints PASS/FAIL lines; FAIL means the hole is open (the attack worked). Exit status 1 if any check failed."""
import os
import shutil
import socket
import subprocess
import sys
import tempfile
import time

REDTEAM = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(REDTEAM)
sys.path.insert(0, os.path.join(ROOT, "tests"))
os.environ.setdefault("LESMUDOS_TEST_PORT", "5041")
import smoke_test as h  # noqa: E402

SECRET = h.SECRET
failures = []


def check(label, ok, detail=""):
    print(("PASS  " if ok else "FAIL  ") + label)
    if not ok:
        failures.append(label)
        if detail:
            print("      got:", repr(detail[-900:]))


# A trusted probe (as a level 10 would write it): reports a player's state as the real game sees it.
PROBE = r'''// @inherit /std/item
public class RtProbe : Item
{
    public override void Create() { Name = "probe"; Ids.Add("probe"); ShortDesc = "a probe"; LongDesc = "It reads state."; }
    public override void Init() { base.Init(); AddAction("dump", Dump); }
    private bool Dump(string args)
    {
        var w = args.Trim().ToLower();
        var p = Users().FirstOrDefault(u => (u.Call<string>("QueryName") ?? "").ToLower() == w);
        if (p == null) { Write("DUMP " + w + " offline\n"); return true; }
        var inv = string.Join(",", AllInventory(p).Select(o => o.ProgramPath + (o.Domain != "" ? "[" + o.Domain + "]" : "[real]")));
        Write($"DUMP {w} gold={p.Call<int>("QueryGold")} xp={p.Call<int>("QueryXp")} level={p.Call<int>("QueryLevel")} quest={p.Call<int>("QueryQuest", "rt_quest")} " +
              $"workshop={p.Call<bool>("QueryInWorkshop")} leaving={p.Call<bool>("QueryLeavingWorkshop")} env={Environment(p)?.Name ?? "none"} body={p.Name} inv={inv}\n");
        return true;
    }
}
'''

RELIC = r'''// @inherit /std/item
public class RtRelic : Item
{
    public override void Create() { Name = "relic"; Ids.Add("relic"); ShortDesc = "a real relic"; LongDesc = "A relic of the real game."; Value = 5000; }
}
'''

LAB = r'''// @inherit /std/room
public class WrenLab : Room
{
    public override void Create()
    {
        ShortDesc = "Wren's Lab";
        LongDesc = "A cluttered workshop.";
        Exits["out"] = "/domains/hollow/village/square";
    }
}
'''


class Game:
    def __init__(self, wiz_files=None, real_files=None, port=None):
        if port:
            os.environ["LESMUDOS_TEST_PORT"] = str(port)
            h.PORT = int(port)
        self.work = tempfile.mkdtemp(prefix="lesmudos-redteam-")
        self.mudlib = os.path.join(self.work, "mudlib")
        h.copy_mudlib(self.mudlib)
        os.makedirs(os.path.join(self.mudlib, "domains", "rt"), exist_ok=True)
        os.makedirs(os.path.join(self.mudlib, "wiz", "wren"), exist_ok=True)
        files = {"/domains/rt/probe": PROBE, "/domains/rt/relic": RELIC, "/wiz/wren/lab": LAB}
        files.update(real_files or {})
        files.update(wiz_files or {})
        for path, src in files.items():
            os.makedirs(os.path.dirname(os.path.join(self.mudlib, path.lstrip("/"))), exist_ok=True)
            h.write(self.mudlib, path, src)
        self.proc = None
        self.log = None

    def start(self):
        self.log = open(os.path.join(self.work, "driver.log"), "a")
        dll = os.path.join(ROOT, "src", "LesMudOS.Driver", "bin", "Debug", "net10.0", "LesMudOS.Driver.dll")
        self.proc = subprocess.Popen(["dotnet", dll, "--mudlib", self.mudlib, "--save", os.path.join(self.work, "save"), "--port", str(h.PORT)],
                                     stdout=self.log, stderr=subprocess.STDOUT, env={**os.environ, "LESMUDOS_ADMIN_SECRET": SECRET})
        for _ in range(600):
            try:
                socket.create_connection(("127.0.0.1", h.PORT), timeout=0.3).close()
                return
            except OSError:
                time.sleep(0.2)
        raise RuntimeError("driver did not start")

    def setup_people(self, extra=()):
        """chief (10), pat (0, in the village square), wren (1); extra: (name, level) pairs."""
        self.chief = h.Client("chief")
        self.chief.read_until("> ")
        self.chief.command("promote " + SECRET)
        self.pat = h.Client("pat")
        self.pat.read_until("> ")
        self.wren = h.Client("wren")
        self.wren.read_until("> ")
        self.chief.command("promote wren 1")
        self.others = {}
        for name, level in extra:
            c = h.Client(name)
            c.read_until("> ")
            if level:
                self.chief.command(f"promote {name} {level}")
            self.others[name] = c
        out = self.chief.command("clone /domains/rt/probe")
        assert "Cloned" in out, out

    def dump(self, who):
        return self.chief.command("dump " + who)

    def driver_log(self):
        self.log.flush()
        return open(os.path.join(self.work, "driver.log")).read()

    def alive(self):
        return self.proc is not None and self.proc.poll() is None

    def stop(self):
        if self.proc and self.proc.poll() is None:
            self.proc.terminate()
            try:
                self.proc.wait(10)
            except subprocess.TimeoutExpired:
                self.proc.kill()
        if self.log:
            self.log.close()
        if os.environ.get("RT_KEEP") != "1":
            shutil.rmtree(self.work, ignore_errors=True)
        else:
            print("kept", self.work)


def finish():
    print(f"{len(failures)} open hole(s)" if failures else "all checks pass (holes closed)")
    sys.exit(1 if failures else 0)
