#!/usr/bin/env python3
"""The crafting economy and odds simulator (docs/CRAFTING.md, "No free gold").

Pure Python, no driver. Reads the crafting data straight from the mudlib files:

  * the numbers of the formulas (/crafting/numbers.cs and any other table that sets a Number),
  * the materials table and the beast table (Material and Beast lines in /crafting/*.cs),
  * every recipe under /recipes,
  * the base Value of each material and output file, and the quality-value multipliers of /std/item.cs,
  * every shop's AddStock line, for the prices vendors really ask.

For every recipe it reports the expected vendor value of the output (shops buy at half value, quality included) against the
cost of buying its ingredients from vendors, at several proficiency levels and for ingredients bought at every quality from
Poor to Masterwork, and FAILS if any recipe pays more than its vendor-bought ingredients cost. The pricing rule it enforces:

  * a vendor never sells a material below its value (base value x quality multiplier); unless a shop lists it, a material is
    assumed to be on sale at exactly that, which is the worst case for this check;
  * a scarce material (Goblin Blood) cannot be bought at all, so a recipe that needs it is not a vendor loop;
  * an ingredient that is itself crafted costs the lesser of its vendor price and what it costs to make from vendor goods.

It also checks that nothing but bleeding can make a scarce material: no recipe output, node yield or shop stock may be one.

The formula functions here (harvest_odds, craft_odds, gain) are the exact twins of /secure/crafting; tests/craft_test.py
compares them with the daemon's own answers.

Usage:
    python3 tests/craft_sim.py                 the real mudlib, then the mudlib with each fixture set laid over it
    python3 tests/craft_sim.py --verbose       also print the table for every recipe
    python3 tests/craft_sim.py --mudlib DIR [--overlay DIR ...]
"""
import math
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FIXTURES = os.path.join(ROOT, "tests", "fixtures", "crafting")
QUALITY = ["poor", "common", "fine", "excellent", "masterwork", "legendary"]
PROF_LEVELS = [0, 25, 50, 75, 100]


# ---------------------------------------------------------------- reading the mudlib

class Tree:
    """A mudlib with overlays: a path is looked up in the last overlay that has it, then in the mudlib."""

    def __init__(self, mudlib, overlays=()):
        self.roots = [mudlib] + list(overlays)

    def read(self, path):
        for root in reversed(self.roots):
            f = os.path.join(root, path.lstrip("/") + ".cs")
            if os.path.isfile(f):
                return open(f, encoding="utf-8").read()
        return None

    def files(self, top):
        seen = {}
        for root in self.roots:
            base = os.path.join(root, top.lstrip("/"))
            for d, _, names in os.walk(base):
                for n in names:
                    if n.endswith(".cs"):
                        rel = "/" + os.path.relpath(os.path.join(d, n), root)[:-3].replace(os.sep, "/")
                        seen[rel] = True
        return sorted(seen)


def strip_comments(src):
    return re.sub(r"//[^\n]*", "", src)


def num(text):
    return float(text.strip().rstrip("fdm"))


def parse_numbers(tree):
    numbers, difficulties = {}, {}
    for path in tree.files("/crafting"):
        src = strip_comments(tree.read(path) or "")
        for m in re.finditer(r'\bNumber\(\s*"([^"]+)"\s*,\s*(-?[\d.]+)\s*\)', src):
            numbers[m.group(1)] = [num(m.group(2))]
        for m in re.finditer(r'\bNumbers\(\s*"([^"]+)"\s*,([^)]*)\)', src):
            numbers[m.group(1)] = [num(x) for x in m.group(2).split(",") if x.strip()]
        for m in re.finditer(r'\bDifficulty\(\s*"([^"]+)"\s*,\s*(-?\d+)\s*\)', src):
            difficulties[m.group(1).lower()] = int(m.group(2))
    return numbers, difficulties


def parse_multipliers(tree):
    src = tree.read("/std/item") or ""
    m = re.search(r"QualityMultipliers\s*=\s*\{([^}]*)\}", src)
    return [num(x) for x in m.group(1).split(",")] if m else [0.5, 1.0, 1.5, 2.5, 4.0, 8.0]


def value_for(base, quality, mult):
    """Item.ValueFor: base x multiplier, rounded half away from zero, at least 1 (a base of 0 stays 0)."""
    if base <= 0:
        return 0
    v = base * mult[max(0, min(5, quality))]
    return max(1, int(math.floor(v + 0.5)))


def file_value(tree, path):
    src = tree.read(path.split("?")[0])
    if src is None:
        return None
    m = re.search(r"\bValue\s*=\s*(\d+)\s*;", strip_comments(src))
    return int(m.group(1)) if m else 0


class Mat:
    def __init__(self, mid, record, categories, scarce=False, value=None):
        self.id, self.record, self.categories, self.scarce, self.value = mid, record, categories, scarce, value


def parse_materials(tree):
    mats = {}
    for path in tree.files("/crafting"):
        src = strip_comments(tree.read(path) or "")
        for m in re.finditer(r'\bBeast\(\s*"([^"]+)"\s*,\s*"([^"]*)"\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*(?:,\s*(?:scarceBlood\s*:\s*)?(true|false))?\s*\)', src):
            kind = m.group(1).lower()
            mats[kind + "_skin"] = Mat(kind + "_skin", "/std/skin?p.kind=" + kind, ["skin", kind], False, int(m.group(3)))
            mats[kind + "_blood"] = Mat(kind + "_blood", "/std/blood?p.kind=" + kind, ["blood", kind], m.group(6) == "true", int(m.group(4)))
        for m in re.finditer(r'\bMaterial\(\s*"([^"]+)"\s*,\s*"([^"]+)"\s*(?:,\s*"([^"]*)")?\s*\)', src):
            cats = [c.strip().lower() for c in (m.group(3) or "").split(",") if c.strip()]
            mats[m.group(1)] = Mat(m.group(1), m.group(2), cats)
    for mat in mats.values():
        if mat.value is None:
            mat.value = file_value(tree, mat.record) or 0
    return mats


def parse_yields(tree):
    out = []
    for path in tree.files("/crafting"):
        src = strip_comments(tree.read(path) or "")
        for m in re.finditer(r'\bYield\(\s*"([^"]+)"\s*,\s*"([^"]+)"', src):
            out.append((path, m.group(1), m.group(2)))
    return out


class Recipe:
    def __init__(self, rid, name, source):
        self.id, self.name, self.source = rid, name, source
        self.slots = []  # (token, count): "m:x" or "c:y"
        self.station, self.prof, self.difficulty, self.output, self.count, self.discovery = "", "", "simple", "", 1, "known"


def parse_recipes(tree):
    recipes = []
    for path in tree.files("/recipes"):
        src = strip_comments(tree.read(path) or "")
        for chunk in re.finditer(r'\bRecipe\(\s*"([^"]+)"\s*,\s*"([^"]*)"\s*\)(.*?);', src, re.S):
            r = Recipe(chunk.group(1), chunk.group(2), path)
            body = chunk.group(3)
            for m in re.finditer(r'\.(Needs|NeedsAny)\(\s*"([^"]+)"\s*(?:,\s*(\d+))?\s*\)', body):
                r.slots.append((("m:" if m.group(1) == "Needs" else "c:") + m.group(2).lower(), int(m.group(3) or 1)))
            for key, attr in (("At", "station"), ("Discovery", "discovery")):
                m = re.search(r"\." + key + r'\(\s*"([^"]*)"\s*\)', body)
                if m:
                    setattr(r, attr, m.group(1).lower())
            m = re.search(r'\.Skill\(\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\)', body)
            if m:
                r.prof, r.difficulty = m.group(1), m.group(2).lower()
            m = re.search(r'\.Makes\(\s*"([^"]+)"\s*(?:,\s*(\d+))?\s*\)', body)
            if m:
                r.output, r.count = m.group(1), int(m.group(2) or 1)
            recipes.append(r)
    return recipes


def parse_shops(tree):
    """Every AddStock line: (record, price, quality, file)."""
    out = []
    for path in tree.files("/domains"):
        src = strip_comments(tree.read(path) or "")
        for m in re.finditer(r'\bAddStock\(\s*"([^"]+)"\s*,\s*(\d+)\s*(?:,\s*(\d+))?\s*\)', src):
            out.append((m.group(1), int(m.group(2)), int(m.group(3) or 1), path))
    return out


# ---------------------------------------------------------------- the formulas (twins of /secure/crafting)

DEFAULTS = {
    "harvest.base": [30.0], "harvest.variation": [15.0], "harvest.tool_quality": [-10, 0, 5, 10, 15, 20],
    "harvest.difficulty_factor": [0.5], "harvest.thresholds": [20, 50, 80, 110], "harvest.success_base": [75.0],
    "harvest.success_per_point": [0.2], "harvest.success_tool": [-10, 0, 5, 10, 15, 20], "harvest.success_difficulty": [0.4],
    "harvest.success_min": [30.0], "harvest.success_max": [98.0], "harvest.bonus_per_point": [0.15],
    "harvest.bonus_tool": [0, 0, 5, 10, 15, 20], "harvest.bonus_max": [40.0], "harvest.max_quality": [4.0],
    "growth.near_band": [10.0], "growth.easy_band": [30.0], "growth.near_gain": [10.0], "growth.easy_gain": [3.0],
    "growth.drag": [125.0], "growth.min_factor": [0.2],
    "craft.shift_per_point": [0.02], "craft.shift_min": [-1.5], "craft.shift_max": [1.0], "craft.noise": [0.75],
    "craft.noise_steps": [20.0], "craft.max_quality": [4.0], "craft.bonus_margin": [20.0], "craft.bonus_per_point": [0.5],
    "craft.bonus_max": [25.0], "craft.return_margin": [30.0], "craft.return_per_point": [0.4], "craft.return_max": [20.0],
}


class Numbers:
    def __init__(self, numbers):
        self.n = {k: [float(x) for x in v] for k, v in DEFAULTS.items()}
        self.n.update({k: [float(x) for x in v] for k, v in numbers.items()})

    def __call__(self, key, i=0):
        return self.n[key][i]


def clamp(v, lo, hi):
    return lo if v < lo else hi if v > hi else v


def harvest_tier(N, score):
    tier = 0
    for i in range(4):
        if score >= N("harvest.thresholds", i):
            tier = i + 1
    return min(tier, int(N("harvest.max_quality")))


def harvest_odds(N, prof, tool_q, work):
    """[success %, bonus %, Poor %, Common %, Fine %, Excellent %, Masterwork %], exactly as the daemon's HarvestOdds."""
    v = int(N("harvest.variation"))
    tq = max(0, min(5, tool_q))
    tiers = [0] * 5
    for a in range(v + 1):
        for b in range(v + 1):
            score = N("harvest.base") + prof + N("harvest.tool_quality", tq) - work * N("harvest.difficulty_factor") + (a + b - v)
            tiers[harvest_tier(N, score)] += 1
    total = float((v + 1) * (v + 1))
    success = clamp(N("harvest.success_base") + prof * N("harvest.success_per_point") + N("harvest.success_tool", tq) - work * N("harvest.success_difficulty"),
                    N("harvest.success_min"), N("harvest.success_max"))
    bonus = clamp(prof * N("harvest.bonus_per_point") + N("harvest.bonus_tool", tq), 0, N("harvest.bonus_max"))
    return [success, bonus] + [100.0 * t / total for t in tiers]


def craft_shift(N, prof, work):
    return clamp((prof - work) * N("craft.shift_per_point"), N("craft.shift_min"), N("craft.shift_max"))


def craft_odds(N, average, prof, work):
    """[Poor % .. Legendary %, bonus %, return %], exactly as the daemon's CraftOdds."""
    n = int(N("craft.noise_steps"))
    tiers = [0] * 6
    for a in range(n + 1):
        for b in range(n + 1):
            noise = (a + b - n) / n * N("craft.noise")
            tier = int(math.floor(average + craft_shift(N, prof, work) + noise + 0.5))
            tiers[max(0, min(int(N("craft.max_quality")), tier))] += 1
    total = float((n + 1) * (n + 1))
    bonus = clamp((prof - work - N("craft.bonus_margin")) * N("craft.bonus_per_point"), 0, N("craft.bonus_max"))
    ret = clamp((prof - work - N("craft.return_margin")) * N("craft.return_per_point"), 0, N("craft.return_max"))
    return [100.0 * t / total for t in tiers] + [bonus, ret]


def gain(N, tenths, work):
    """Tenths of a point gained for one successful piece of work, exactly as the daemon's Gain."""
    skill = tenths / 10.0
    gap = skill - work
    base = N("growth.near_gain") if gap <= N("growth.near_band") else N("growth.easy_gain") if gap <= N("growth.easy_band") else 0
    if base <= 0 or tenths >= 1000:
        return 0
    factor = max(N("growth.min_factor"), 1 - skill / N("growth.drag"))
    return max(1, int(math.floor(base * factor + 0.5)))


# ---------------------------------------------------------------- the economy

class Economy:
    def __init__(self, tree, label):
        self.tree, self.label = tree, label
        numbers, self.difficulties = parse_numbers(tree)
        if not self.difficulties:
            self.difficulties = {"simple": 0, "novice": 10, "apprentice": 25, "journeyman": 45, "expert": 65, "master": 85}
        self.N = Numbers(numbers)
        self.mult = parse_multipliers(tree)
        self.mats = parse_materials(tree)
        self.by_record = {m.record: m for m in self.mats.values()}
        self.recipes = parse_recipes(tree)
        self.shops = parse_shops(tree)
        self.yields = parse_yields(tree)
        self.errors = []
        self.notes = []
        self._making = set()
        self._cost = {}

    def material_of_record(self, record):
        if record in self.by_record:
            return self.by_record[record]
        if record.startswith("/"):
            kind = re.search(r"p\.kind=([a-z0-9_]+)", record)
            base = record.split("?")[0]
            for m in self.mats.values():
                if m.record.split("?")[0] == base and ((kind and ("p.kind=" + kind.group(1)) in m.record) or (not kind and "?" not in m.record)):
                    return m
        return self.mats.get(record)

    def vendor_price(self, mat, q):
        """The lowest price any shop asks for one at quality q, or the assumed price (the value), or None if it cannot be bought."""
        if mat.scarce:
            return None
        asked = [round_price(p, sq, self.mult) for rec, p, sq, _ in self.shops if sq == q and self.material_of_record(rec) is mat]
        floor = value_for(mat.value, q, self.mult)
        return min(asked) if asked else floor

    def output_value(self, r):
        if r.output.startswith("/"):
            m = self.material_of_record(r.output)
            return m.value if m else (file_value(self.tree, r.output) or 0)
        return self.mats[r.output].value if r.output in self.mats else 0

    def unit_cost(self, mid, q):
        """What one unit of a material at quality q costs from vendors: its price, or what it costs to make, whichever is less."""
        key = (mid, q)
        if key in self._cost:
            return self._cost[key]
        mat = self.mats.get(mid)
        best = self.vendor_price(mat, q) if mat else None
        if mid not in self._making:
            self._making.add(mid)
            for r in self.recipes:
                out = self.material_of_record(r.output) if r.output.startswith("/") else self.mats.get(r.output)
                if out is None or out.id != mid:
                    continue
                cost = self.recipe_cost(r, q)
                if cost is None:
                    continue
                for p in PROF_LEVELS:
                    odds = craft_odds(self.N, q, p, self.difficulties.get(r.difficulty, 0))
                    per = cost / (r.count + odds[6] / 100.0)
                    best = per if best is None else min(best, per)
            self._making.discard(mid)
        self._cost[key] = best
        return best

    def slot_cost(self, token, q):
        kind, name = token[:2], token[2:]
        if kind == "m:":
            return self.unit_cost(name, q)
        costs = [self.unit_cost(m.id, q) for m in self.mats.values() if name in m.categories]
        costs = [c for c in costs if c is not None]
        return min(costs) if costs else None

    def recipe_cost(self, r, q):
        total = 0.0
        for token, count in r.slots:
            c = self.slot_cost(token, q)
            if c is None:
                return None
            total += c * count
        return total

    def check(self, verbose=False):
        mult = self.mult
        lines = []
        # Goblin Blood (any scarce material) comes from nothing but bleeding.
        for r in self.recipes:
            out = self.material_of_record(r.output) if r.output.startswith("/") else self.mats.get(r.output)
            if out is not None and out.scarce:
                self.errors.append(f"recipe {r.id} makes {out.id}, which may only come from bleeding")
        for path, kind, mid in self.yields:
            if mid in self.mats and self.mats[mid].scarce:
                self.errors.append(f"{path}: node kind '{kind}' yields {mid}, which may only come from bleeding")
        for rec, price, q, path in self.shops:
            m = self.material_of_record(rec)
            if m is not None and m.scarce:
                self.errors.append(f"{path}: a shop stocks {m.id}, which may only come from bleeding")
            if m is not None and round_price(price, q, mult) < value_for(m.value, q, mult):
                self.errors.append(f"{path}: a shop sells {m.id} at {round_price(price, q, mult)}, below its value {value_for(m.value, q, mult)} (the pricing rule)")
        # Every material and every recipe output names a file that exists (the daemon does not open files when it loads).
        for m in self.mats.values():
            if self.tree.read(m.record.split("?")[0]) is None:
                self.errors.append(f"material {m.id}: its file {m.record.split('?')[0]}.cs does not exist")
        for r in self.recipes:
            if r.output.startswith("/") and self.tree.read(r.output.split("?")[0]) is None:
                self.errors.append(f"recipe {r.id}: its output {r.output}.cs does not exist")
        worst = None
        for r in self.recipes:
            work = self.difficulties.get(r.difficulty, 0)
            vout = self.output_value(r)
            table = []
            vendor_ok = True
            for q in range(0, 5):
                cost = self.recipe_cost(r, q)
                if cost is None:
                    vendor_ok = False
                    break
                first = r.slots[0][0] if r.slots else None
                for p in PROF_LEVELS:
                    odds = craft_odds(self.N, q, p, work)
                    count = r.count + odds[6] / 100.0
                    sale = 0.5 * sum(odds[t] / 100.0 * value_for(vout, t, mult) for t in range(6)) * count
                    # A shop pays half the value, rounded down per item (Shopkeeper.OfferFor); the half above is the upper bound.
                    back = (odds[7] / 100.0) * (self.slot_cost(first, q) or 0)
                    net = cost - back
                    table.append((q, p, cost, sale, net))
                    margin = sale - net
                    if worst is None or margin > worst[0]:
                        worst = (margin, r.id, q, p)
                    if sale > net + 1e-9:
                        self.errors.append(f"recipe {r.id}: ingredients bought at {QUALITY[q]} cost {net:.2f} gold (after expected savings) "
                                           f"and the output sells for {sale:.2f} at proficiency {p}: a gold loop")
            if not vendor_ok:
                self.notes.append(f"recipe {r.id}: needs a material no vendor may sell (gathered only); not a vendor loop")
                continue
            if verbose:
                lines.append(f"  {r.id} ({r.source}): output value {vout} x {r.count}, {r.difficulty} (work {work})")
                for q, p, cost, sale, net in table:
                    if q in (0, 1, 3) and p in (0, 50, 100):
                        lines.append(f"      buy {QUALITY[q]:<10} prof {p:>3}: cost {cost:6.2f}, net {net:6.2f}, sells for {sale:6.2f}")
        return lines, worst


def round_price(price, quality, mult):
    """Shopkeeper.UnitPrice: the listed price, times the quality multiplier for quality other than Common."""
    return price if quality == 1 else max(1, int(math.floor(price * mult[quality] + 0.5)))


# ---------------------------------------------------------------- the odds report

def odds_report(N):
    out = ["Harvest odds (work 6: a rabbit; 22: a level-2 wolf; 48: a level-8 bear), tool Common unless named:"]
    out.append("  prof tool       work  success bonus |   poor common   fine  excel master")
    for prof, tool, work in [(0, 1, 6), (0, 1, 22), (25, 1, 22), (50, 1, 22), (50, 3, 22), (75, 1, 48), (100, 1, 48), (100, 4, 6), (0, 0, 6)]:
        o = harvest_odds(N, prof, tool, work)
        out.append(f"  {prof:>4} {QUALITY[tool]:<10} {work:>4}  {o[0]:6.1f}% {o[1]:4.1f}% | " + " ".join(f"{x:5.1f}%" for x in o[2:]))
    out.append("Crafting odds (average ingredient quality; recipe work 10 is Novice):")
    out.append("  avg  prof work |   poor common   fine  excel master | bonus return")
    for avg, prof, work in [(1, 0, 0), (1, 0, 10), (2.5, 0, 10), (2.5, 50, 10), (1, 100, 10), (1, 0, 45), (0, 100, 0), (3, 100, 10)]:
        o = craft_odds(N, avg, prof, work)
        out.append(f"  {avg:>3} {prof:>5} {work:>4} | " + " ".join(f"{x:5.1f}%" for x in o[:5]) + f" | {o[6]:4.1f}% {o[7]:4.1f}%")
    return out


def run_one(mudlib, overlays, label, verbose):
    econ = Economy(Tree(mudlib, overlays), label)
    lines, worst = econ.check(verbose)
    print(f"== {label}: {len(econ.recipes)} recipes, {len(econ.mats)} materials, {len(econ.shops)} shop lines")
    for line in lines:
        print(line)
    for note in econ.notes:
        print("  note: " + note)
    if worst is not None:
        print(f"  closest to a loop: {worst[1]} bought at {QUALITY[worst[2]]}, proficiency {worst[3]}: sale - cost = {worst[0]:.2f} gold")
    for e in econ.errors:
        print("  FAIL: " + e)
    print(f"  {'ok' if not econ.errors else str(len(econ.errors)) + ' problem(s)'}")
    return econ


def main(argv):
    verbose = "--verbose" in argv
    if "--mudlib" in argv:
        i = argv.index("--mudlib")
        mudlib = argv[i + 1]
        overlays = [argv[j + 1] for j, a in enumerate(argv) if a == "--overlay"]
        econ = run_one(mudlib, overlays, mudlib, verbose)
        return 1 if econ.errors else 0
    mudlib = os.path.join(ROOT, "mudlib")
    failed = 0
    base = run_one(mudlib, [], "the mudlib", verbose)
    failed += len(base.errors)
    for name in ("samples", "launch"):
        d = os.path.join(FIXTURES, name)
        if os.path.isdir(d):
            failed += len(run_one(mudlib, [d], f"the mudlib with tests/fixtures/crafting/{name}", verbose).errors)
    print()
    for line in odds_report(base.N):
        print(line)
    print()
    print("craft_sim: " + ("PASS, no recipe is profitable from vendor-bought ingredients" if failed == 0 else f"FAIL, {failed} problem(s)"))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
