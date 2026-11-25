#!/usr/bin/env python3
"""Run every red-team proof in this directory, one after another (each starts its own driver on LESMUDOS_TEST_PORT), and print the
last line of each. Exit status 1 if any proof found a hole open. See REPORT.md for what each one attacks."""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
SCRIPTS = ["f2_exception_window", "c1_chain_phish_admin", "f3_restore_abort", "f3b_sandbox_weapon_out", "f3c_restore_routes",
           "f4_tuple_cross", "f5_cross_home", "p1_publish_review", "p3_reviewer_in_workshop", "f1_dos", "f1c_stall",
           "l1_l2_and_rules", "a1_allowlist", "t1_every_body", "r1_login_placement", "m1_low_memory"]

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
