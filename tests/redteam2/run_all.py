#!/usr/bin/env python3
"""Run every proof of the second review in this directory, one after another (each starts its own driver on LESMUDOS_TEST_PORT), and print the
last line of each. Exit status 1 if any proof found a hole open. See REPORT.md for what each one attacks."""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SCRIPTS = ["t2_tick_exhaustion_checkpoint", "t3_tick_exhaustion_exit", "d2_disk_and_checkpoint", "w1_blame_shift", "c2_compile_freeze",
           "w2_markup_blind_stall", "t1_tickless_recursion", "l3_log_flood", "s1_visitor_mark_wrap", "n1_telnet_iac_line"]

failed = []
for name in SCRIPTS:
    run = subprocess.run([sys.executable, "-u", os.path.join(HERE, name + ".py")], capture_output=True, text=True, timeout=1800)
    lines = [l for l in (run.stdout + run.stderr).splitlines() if l.strip()]
    bad = [l for l in lines if l.startswith("FAIL")]
    print(f"{name}: {lines[-1] if lines else '(no output)'}")
    for line in bad:
        print("   ", line)
    if run.returncode != 0 or bad:
        failed.append(name)
print(f"{len(SCRIPTS) - len(failed)} of {len(SCRIPTS)} proofs pass" + (": open in " + ", ".join(failed) if failed else ""))
sys.exit(1 if failed else 0)
