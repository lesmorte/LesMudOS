#!/usr/bin/env python3
"""Balance simulation for Vincent's Hollow on the event clock (docs/COMBAT_TIMING.md, GAME_SYSTEMS.md section 12).

Standalone: it does not talk to the driver. It plays the combat rules of /std/living with the numbers of the
creatures and items in the mudlib, and prints tables for a solo player:

  1. room by room: win rate, HP lost, fight length in seconds and in player actions
  2. the Goblin Overseer against level 4-5 players in village gear
  3. a whole run through the cellars with level-ups, rests and purchases
  4. damage per second of every weapon (the slower weapons must be worth using)

Rules (identical to the code; `python3 tests/balance_sim.py check` compares the numbers below with the mudlib files):
  * There are no rounds. Every combatant has a Ready Time. When it is ready and has an enemy it begins an action:
    Total Delay = (Reaction Time + action reaction modifier + action Recovery) x 2 seconds, where Reaction Time =
    base + the reaction modifiers of everything equipped. The effect happens Preparation x 2 seconds after the
    start; the next action begins at the Ready Time (start + Total Delay).
  * The player starts the fight (as the bot and most players do): he is ready at once. Every creature in the room
    is drawn in by someone else and cannot begin its first action until its own Reaction Time x 2 seconds have
    passed. A creature summoned by the Overseer is drawn in the same way.
  * The player attacks the first living enemy of the room. A potion is drunk at the next Ready Time when HP is at or
    below 35% (it replaces one attack; its action has Recovery 0.5).
  * To hit: d20 + level + to-hit >= 10 + defender armor. A natural 20 always hits, a natural 1 always misses.
  * Damage is uniform in the range, minimum 1. Damage multipliers and the Battle Cry bonus are applied as in
    /std/living: (roll + bonus) x multiplier.
  * Level n needs 0/100/250/500/900 XP for n = 1..5; each level adds 8 max HP and heals fully, in the middle of a
    fight too (experience is paid at the kill). Base HP is 30.

Usage: python3 tests/balance_sim.py [runs]      python3 tests/balance_sim.py check
"""
import heapq
import os
import random
import re
import sys
from dataclasses import dataclass

XP_FOR_LEVEL = {1: 0, 2: 100, 3: 250, 4: 500, 5: 900}
POTION_HEAL = 20
POTION_RECOVERY = 0.5
PLAYER_REACTION = 2.5
UNIT_MS = 2000
CRY_MS = 30000
FORGET = None  # (enemies never leave the room in the simulation)


@dataclass(frozen=True)
class Act:
    """An action: preparation and recovery in time units of 2 s."""
    id: str
    prep: float
    rec: float
    mult: int = 1
    mod: float = 0.0


@dataclass(frozen=True)
class Kind:
    name: str
    level: int
    hp: int
    dmg: tuple
    to_hit: int
    armor: int
    xp: int
    reaction: float
    acts: tuple
    gold: tuple = (0, 0)


# The numbers of docs/COMBAT_TIMING.md section 3 and docs/GAME_SYSTEMS.md section 6; the mudlib files agree
# (python3 tests/balance_sim.py check).
KINDS = {k.name: k for k in [
    Kind("Large Rat", 1, 12, (1, 2), 0, 0, 8, 2.0, (Act("bite", 0, 0.5),)),
    Kind("Giant Rat", 1, 15, (1, 3), 0, 0, 12, 2.0, (Act("bite", 0, 0.5),)),
    Kind("Wandering Wolf", 2, 26, (2, 4), 1, 1, 25, 2.0, (Act("bite", 0, 1.0),)),
    Kind("Goblin Scavenger", 2, 13, (1, 3), 2, 1, 22, 1.5, (Act("stab", 0, 0.5),), (2, 8)),
    Kind("Spore Bat", 2, 9, (1, 2), 1, 3, 20, 1.5, (Act("swoop", 0, 0.5),)),
    Kind("Goblin Guard", 2, 26, (2, 4), 0, 2, 32, 3.5, (Act("jab", 0, 1.0),), (4, 12)),
    Kind("Giant Cave Toad", 3, 66, (2, 4), 0, 0, 50, 4.5, (Act("lash", 0, 1.0), Act("leap", 1.0, 2.0, 2))),
    Kind("Goblin Warrior", 3, 28, (2, 4), 0, 2, 42, 3.0, (Act("slash", 0, 1.0),), (5, 14)),
    Kind("Goblin Minion", 2, 20, (2, 4), 0, 1, 15, 2.5, (Act("stab", 0, 0.5),)),
    Kind("Goblin Elite Guard", 4, 40, (3, 5), 2, 3, 85, 3.5, (Act("slash", 0, 1.0), Act("bash", 0.5, 1.5, 2)), (12, 22)),
    Kind("Goblin Overseer", 5, 114, (4, 7), 2, 3, 250, 4.0,
         (Act("slash", 0, 1.0), Act("power", 1.0, 2.0, 2), Act("cry", 0, 0.5), Act("summon", 0.5, 1.0))),
]}

# (room, [creatures]) in the order a player meets them.
ROOMS = [
    ("Tavern Cellar", ["Large Rat"] * 3),
    ("Collapsed Passage", ["Giant Rat"] * 2),
    ("Storage Chamber", ["Goblin Scavenger"] * 2),
    ("Guard Post", ["Goblin Guard"] * 2),
    ("Flooded Tunnel", ["Giant Cave Toad"]),
    ("Mushroom Cavern", ["Spore Bat"] * 3),
    ("Goblin Barracks", ["Goblin Warrior"] * 3),
    ("Overseer Hall", ["Goblin Elite Guard"] * 2),
]

# weapon: (damage range, to hit, reaction modifier, basic attack recovery). Fists: the player's unarmed attack.
WEAPONS = {
    "fists": ((2, 5), 0, 0.0, 0.5),
    "iron dagger": ((6, 9), 1, 0.0, 0.5),
    "enchanted dagger": ((7, 11), 2, 0.0, 0.5),
    "training spear": ((8, 12), 1, 0.5, 1.0),
    "rusty sword": ((9, 14), 0, 0.5, 1.0),
    "fine longsword": ((12, 18), 0, 1.0, 1.0),
    "woodcutter axe": ((14, 21), 0, 1.0, 1.5),
    "overseer's axe": ((17, 25), 0, 1.5, 1.5),
}
# armor pieces: (armor value, reaction modifier)
SHIELD, HELM, VEST = (1, 0.5), (1, 0.0), (3, 0.0)
TRINKET_HP = 5  # the Magical Trinket from the Hidden Cache
BASE_HP, HP_PER_LEVEL = 30, 8


def hit_chance(level, to_hit, armor):
    hits = 0
    for roll in range(1, 21):
        if roll == 20 or (roll != 1 and roll + level + to_hit >= 10 + armor):
            hits += 1
    return hits / 20


def player_max_hp(level, extra=0):
    return BASE_HP + HP_PER_LEVEL * (level - 1) + extra


class Fighter:
    def __init__(self, level, hp, dmg, to_hit, armor, reaction, acts, kind=None, is_player=False):
        self.level, self.hp, self.max_hp = level, hp, hp
        self.dmg, self.to_hit, self.armor = dmg, to_hit, armor
        self.reaction = reaction  # final Reaction Time
        self.acts = acts
        self.kind = kind
        self.is_player = is_player
        self.taken = 0  # actions chosen this fight
        self.attacks = 0
        self.summoned = False
        self.cry_until = 0

    def alive(self):
        return self.hp > 0

    def delay_ms(self, act):
        return round((self.reaction + act.mod + act.rec) * UNIT_MS)

    def prep_ms(self, act):
        return min(self.delay_ms(act), round(act.prep * UNIT_MS))


def make_monster(name):
    k = KINDS[name]
    return Fighter(k.level, k.hp, k.dmg, k.to_hit, k.armor, k.reaction, k.acts, k)


def make_player(level, weapon, armor_pieces, extra_hp=0):
    dmg, to_hit, mod, rec = WEAPONS[weapon]
    armor = sum(a for a, _ in armor_pieces)
    reaction = PLAYER_REACTION + mod + sum(m for _, m in armor_pieces)
    p = Fighter(level, player_max_hp(level, extra_hp), dmg, to_hit, armor, reaction, (Act("attack", 0, rec),), is_player=True)
    p.extra_hp = extra_hp
    return p


def roll_attack(att, defn, bonus=0, mult=1):
    """One attack. Returns the damage dealt (0 on a miss)."""
    roll = random.randint(1, 20)
    if not (roll == 20 or (roll != 1 and roll + att.level + att.to_hit >= 10 + defn.armor)):
        return 0
    dmg = max(1, random.randint(*att.dmg))
    dmg = max(1, dmg + bonus) * mult
    return dmg


def choose(e):
    """ChooseAction of the mudlib creature files."""
    a = {x.id: x for x in e.acts}
    if e.kind is None or len(e.acts) == 1:
        return e.acts[0]
    n = e.kind.name
    if n == "Giant Cave Toad":
        return a["leap"] if e.taken % 3 == 2 else a["lash"]
    if n == "Goblin Elite Guard":
        return a["bash"] if e.taken % 4 == 3 else a["slash"]
    if n == "Goblin Overseer":
        if e.taken == 0:
            return a["cry"]
        if not e.summoned and e.hp * 2 <= e.max_hp:
            e.summoned = True
            return a["summon"]
        e.attacks += 1
        return a["power"] if e.attacks % 3 == 0 else a["slash"]
    return e.acts[0]


def fight(player, enemies, potions=0, boss=False, drink_at=0.35, on_kill=None):
    """Fight until one side is dead. Mutates player and enemies. on_kill(player, enemy), if given, is called when
    the player's attack kills an enemy. Returns a dict: alive, ms (length of the fight), actions (the player's),
    potions (left), boss_ms and lost_at_kill (when the boss died)."""
    heap = []
    seq = 0

    def push(t, kind, ent, act=None, target=None):
        nonlocal seq
        seq += 1
        heapq.heappush(heap, (t, seq, kind, ent, act, target))

    start_hp = player.hp
    out = {"actions": 0, "boss_ms": None, "lost_at_kill": None}
    push(0, "ready", player)
    for e in enemies:
        push(round(e.reaction * UNIT_MS), "ready", e)
    t = 0
    while heap and player.hp > 0 and any(e.hp > 0 for e in enemies):
        t, _, kind, ent, act, target = heapq.heappop(heap)
        if ent.hp <= 0:
            continue
        if kind == "ready":
            living = [e for e in enemies if e.hp > 0]
            if not living and not ent.is_player:
                continue
            if ent.is_player:
                if potions > 0 and ent.hp <= ent.max_hp * drink_at:
                    potions -= 1
                    act = Act("potion", 0, POTION_RECOVERY)
                    ent.hp = min(ent.max_hp, ent.hp + POTION_HEAL)
                    out["actions"] += 1
                    push(t + ent.delay_ms(act), "ready", ent)
                    continue
                act = ent.acts[0]
                target = living[0]
                out["actions"] += 1
            else:
                act = choose(ent)
                ent.taken += 1
                target = player
            delay, prep = ent.delay_ms(act), ent.prep_ms(act)
            push(t + delay, "ready", ent)
            if prep > 0:
                push(t + prep, "resolve", ent, act, target)
            else:
                resolve(ent, act, target, t, player, enemies, out, on_kill, push, boss)
        else:
            resolve(ent, act, target, t, player, enemies, out, on_kill, push, boss)
        if boss and out["boss_ms"] is None and enemies[0].hp <= 0:
            out["boss_ms"] = t
            out["lost_at_kill"] = start_hp - player.hp
    out.update(alive=player.hp > 0, ms=t, potions=potions)
    return out


def resolve(ent, act, target, t, player, enemies, out, on_kill, push, boss):
    if target.hp <= 0:
        return
    if act.id == "cry":
        ent.cry_until = t + CRY_MS
        return
    if act.id == "summon":
        m = make_monster("Goblin Minion")
        enemies.append(m)
        push(t + round(m.reaction * UNIT_MS), "ready", m)
        return
    bonus = 2 if t < ent.cry_until else 0
    dmg = roll_attack(ent, target, bonus, act.mult)
    if dmg <= 0:
        return
    target.hp -= dmg
    if ent.is_player and target.hp <= 0 and on_kill is not None:
        on_kill(player, target)


def room_stats(creatures, level, weapon, armor, extra_hp=0, runs=2000, potions=0):
    wins = lost = ms_total = actions_total = 0
    for _ in range(runs):
        p = make_player(level, weapon, armor, extra_hp)
        enemies = [make_monster(c) for c in creatures]
        r = fight(p, enemies, potions)
        if r["alive"]:
            wins += 1
            lost += p.max_hp - p.hp
            ms_total += r["ms"]
            actions_total += r["actions"]
    n = wins or 1
    return wins / runs, lost / n, ms_total / n / 1000.0, actions_total / n


def boss_stats(level, weapon, armor, potions, extra_hp=0, runs=2000):
    kills = survive = lost = ms = actions = 0
    for _ in range(runs):
        p = make_player(level, weapon, armor, extra_hp)
        r = fight(p, [make_monster("Goblin Overseer")], potions, boss=True)
        if r["lost_at_kill"] is not None:
            kills += 1
            lost += r["lost_at_kill"]
            ms += r["boss_ms"]
            actions += r["actions"]
        survive += r["alive"]
    n = kills or 1
    return kills / runs, survive / runs, lost / n, ms / n / 1000.0, actions / n


# The level a sensible player has before taking on a room (the dungeon is advertised for 2-5).
REQUIRED_LEVEL = {"Tavern Cellar": 1, "Collapsed Passage": 1, "Storage Chamber": 2, "Guard Post": 2,
                  "Flooded Tunnel": 3, "Mushroom Cavern": 3, "Goblin Barracks": 4, "Overseer Hall": 4}
BOSS_LEVEL = 4


def full_run(runs=1000):
    """A whole solo run, Tavern Cellar to the Overseer. The player starts with 25 gold (a rusty sword and a small
    shield), buys the best affordable weapon and up to 2 potions, rests to full between rooms, and goes back to farm
    cleared rooms (respawns) whenever he is below the level a room wants. A death costs 10% of the gold and a retry.
    Returns the share of runs that killed the Overseer (within 3 attempts, and at the first), the average number of
    farming fights and deaths, the level at the Overseer and the average seconds spent fighting."""
    done = first_try = 0
    farm_total = deaths_total = fight_ms_total = 0
    levels = {}
    for _ in range(runs):
        xp, gold, level = 0, 25 - 10 - 12, 1
        weapon, potions, trinket = "rusty sword", 0, 0
        cleared = []
        deaths = farm = 0
        failed = False
        fight_ms = 0

        def shop():
            nonlocal gold, weapon, potions
            if weapon != "fine longsword" and gold >= 60:
                gold -= 60
                weapon = "fine longsword"
            elif weapon == "rusty sword" and gold >= 18 + 2:
                gold -= 18 - 5  # sell the sword for 5
                weapon = "woodcutter axe"
            while potions < 2 and gold >= 15:
                gold -= 15
                potions += 1

        def on_kill(p, enemy):
            nonlocal xp, gold, level
            k = enemy.kind
            xp += k.xp
            if k.gold[1]:
                gold += random.randint(*k.gold)
            while level < 5 and xp >= XP_FOR_LEVEL[level + 1]:
                level += 1
                p.level = level
                p.max_hp = player_max_hp(level, trinket)
                p.hp = p.max_hp

        def attempt(creatures, hard=False):
            nonlocal gold, potions, deaths, fight_ms
            for tries in range(10):
                p = make_player(level, weapon, [SHIELD], trinket)
                use = potions if hard or tries > 0 else 0
                r = fight(p, [make_monster(c) for c in creatures], use, on_kill=on_kill)
                fight_ms += r["ms"]
                potions = potions - use + r["potions"]
                if r["alive"]:
                    return True
                deaths += 1
                gold = int(gold * 0.9)
            return False

        def best_cleared():
            best = None
            for room, group in cleared:
                if room_stats(group, level, weapon, [SHIELD], trinket, runs=40)[0] >= 0.9:
                    best = (room, group)
            return best

        for name, creatures in ROOMS:
            while level < REQUIRED_LEVEL[name] and cleared and farm < 400:
                best = best_cleared()
                if best is None:
                    break
                farm += 1
                shop()
                attempt(best[1])
            shop()
            if not attempt(creatures, hard=name in ("Goblin Barracks", "Overseer Hall")):
                failed = True
                break
            cleared.append((name, creatures))
            if name == "Storage Chamber":
                potions += 1  # the potion lying in the room
            if name == "Mushroom Cavern":
                gold += random.randint(30, 60)  # the Hidden Cache (its key came from the Guard Post)
                trinket = TRINKET_HP
        farm_total += farm
        deaths_total += deaths
        if failed:
            continue
        while level < BOSS_LEVEL and farm < 400:
            best = best_cleared()
            if best is None:
                break
            farm += 1
            shop()
            attempt(best[1])
        shop()
        levels[level] = levels.get(level, 0) + 1
        for tries in range(3):
            p = make_player(level, weapon, [SHIELD], trinket)
            r = fight(p, [make_monster("Goblin Overseer")], min(potions, 2), boss=True)
            fight_ms += r["ms"]
            if r["lost_at_kill"] is not None:
                done += 1
                first_try += tries == 0
                break
            gold = int(gold * 0.9)
            potions = 0
            shop()
        fight_ms_total += fight_ms
    return done / runs, first_try / runs, farm_total / runs, deaths_total / runs, levels, fight_ms_total / runs / 1000.0


# ---------------------------------------------------------------- consistency with the mudlib

def check_mudlib():
    """Compare KINDS and WEAPONS with the mudlib files. Exits non-zero on any difference."""
    root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "mudlib", "domains", "hollow")

    def read(path):
        with open(os.path.join(root, path), encoding="utf-8") as f:
            return f.read()

    def num(text, field):
        m = re.search(rf"\b{field} = (-?[\d.]+);", text)
        return float(m.group(1)) if m else None

    files = {"Large Rat": "large_rat", "Giant Rat": "giant_rat", "Wandering Wolf": "wandering_wolf",
             "Goblin Scavenger": "goblin_scavenger", "Spore Bat": "spore_bat", "Goblin Guard": "goblin_guard",
             "Giant Cave Toad": "giant_cave_toad", "Goblin Warrior": "goblin_warrior", "Goblin Minion": "goblin_minion",
             "Goblin Elite Guard": "goblin_elite_guard", "Goblin Overseer": "goblin_overseer"}
    problems = []
    for name, f in files.items():
        k, text = KINDS[name], read(f"mob/{f}.cs")
        want = {"Level": k.level, "MaxHp": k.hp, "DamageMin": k.dmg[0], "DamageMax": k.dmg[1], "ToHit": k.to_hit,
                "Armor": k.armor, "XpValue": k.xp, "BaseReaction": k.reaction}
        for field, value in want.items():
            have = num(text, field)
            if (have if have is not None else 0) != value:
                problems.append(f"{name}: {field} is {have} in the mudlib, {value} in the simulation")
        acts = re.findall(r'AddCombatAction\("(\w+)", "[^"]+", preparation: ([\d.]+), recovery: ([\d.]+)(?:[^;]*?damageMultiplier: (\d+))?', text)
        got = [(a, float(p), float(r), int(m or 1)) for a, p, r, m in acts]
        have = sorted((a.id, a.prep, a.rec, a.mult) for a in k.acts)
        if sorted(got) != have:
            problems.append(f"{name}: actions {sorted(got)} in the mudlib, {have} in the simulation")
    wfiles = {"iron dagger": "iron_dagger", "enchanted dagger": "enchanted_dagger", "training spear": "training_spear",
              "rusty sword": "rusty_sword", "fine longsword": "fine_iron_longsword", "woodcutter axe": "woodcutter_axe",
              "overseer's axe": "overseers_axe"}
    for name, f in wfiles.items():
        (lo, hi), to_hit, mod, rec = WEAPONS[name]
        text = read(f"obj/{f}.cs")
        want = {"DamageMin": lo, "DamageMax": hi, "ToHit": to_hit, "ReactionMod": mod, "Recovery": rec}
        for field, value in want.items():
            have = num(text, field)
            if (have if have is not None else 0) != value:
                problems.append(f"{name}: {field} is {have} in the mudlib, {value} in the simulation")
    for name, f, (armor, mod) in (("small shield", "small_shield", SHIELD), ("guild helm", "guild_helm", HELM),
                                  ("commander's vest", "commanders_vest", VEST)):
        text = read(f"obj/{f}.cs")
        for field, value in (("ArmorValue", armor), ("ReactionMod", mod)):
            have = num(text, field)
            if (have if have is not None else 0) != value:
                problems.append(f"{name}: {field} is {have} in the mudlib, {value} in the simulation")
    text = read("obj/healing_potion.cs")
    if num(text, "HealAmount") != POTION_HEAL:
        problems.append("healing potion: HealAmount differs")
    for p in problems:
        print("MISMATCH", p)
    print("simulation numbers match the mudlib" if not problems else f"{len(problems)} mismatch(es)")
    return not problems


def main():
    if len(sys.argv) > 1 and sys.argv[1] == "check":
        sys.exit(0 if check_mudlib() else 1)
    runs = int(sys.argv[1]) if len(sys.argv) > 1 else 3000
    random.seed(20260101)
    armor = [SHIELD]
    print("=" * 112)
    print("1. Room by room, solo, at the level a sensible player has (rested to full HP, the player starts every fight).")
    print("   Gear: rusty sword, then woodcutter axe, then fine longsword; small shield; +5 HP trinket from the Mushroom")
    print("   Cavern on. No potions. 'Win' = the player survives. Seconds and actions are the averages of won fights.")
    print("=" * 112)
    header = f"{'Room':<19}{'Enemies':<22}{'Lvl':>3}  {'Weapon':<16}{'HP':>4}{'Win%':>6}{'HP lost':>9}{'Seconds':>9}{'Actions':>9}"
    print(header)
    print("-" * len(header))
    profiles = [
        ("Tavern Cellar", 1, "rusty sword", 0), ("Collapsed Passage", 1, "rusty sword", 0),
        ("Storage Chamber", 2, "rusty sword", 0), ("Guard Post", 2, "woodcutter axe", 0),
        ("Guard Post", 3, "woodcutter axe", 0), ("Flooded Tunnel", 3, "woodcutter axe", 0),
        ("Mushroom Cavern", 3, "woodcutter axe", 0), ("Goblin Barracks", 4, "fine longsword", TRINKET_HP),
        ("Goblin Barracks", 5, "fine longsword", TRINKET_HP), ("Overseer Hall", 4, "fine longsword", TRINKET_HP),
        ("Overseer Hall", 5, "fine longsword", TRINKET_HP),
    ]
    rooms = dict(ROOMS)
    for room, level, weapon, extra in profiles:
        creatures = rooms[room]
        names = {c: creatures.count(c) for c in creatures}
        label = ", ".join(f"{n}x {c.split()[-1] if n > 1 else c}" for c, n in names.items())
        win, lost, secs, acts = room_stats(creatures, level, weapon, armor, extra, runs)
        print(f"{room:<19}{label[:21]:<22}{level:>3}  {weapon:<16}{player_max_hp(level, extra):>4}{win * 100:>5.0f}%{lost:>9.1f}{secs:>9.1f}{acts:>9.1f}")
    print()
    print("   Single creatures, one at a time, level and weapon as above (actions the player needs to kill it):")
    singles = [("Large Rat", 1, "rusty sword"), ("Giant Rat", 1, "rusty sword"), ("Goblin Scavenger", 2, "rusty sword"),
               ("Goblin Guard", 2, "woodcutter axe"), ("Giant Cave Toad", 3, "woodcutter axe"), ("Spore Bat", 3, "woodcutter axe"),
               ("Goblin Warrior", 4, "fine longsword"), ("Goblin Elite Guard", 4, "fine longsword")]
    for name, level, weapon in singles:
        win, lost, secs, acts = room_stats([name], level, weapon, armor, TRINKET_HP if level >= 4 else 0, runs)
        print(f"   {name:<20} L{level} {weapon:<16} win {win * 100:>3.0f}%  HP lost {lost:>5.1f}  {secs:>5.1f} s  {acts:>4.1f} player actions")
    print()

    print("=" * 112)
    print("2. The Goblin Overseer, solo, from full HP, in village gear only (small shield). Potions are drunk at 35% HP.")
    print("   'Kill' = the Overseer dies before the player does (the minion may still be alive); 'Survive' = the player is")
    print("   alive when the whole fight is over. Seconds and actions: until the Overseer dies, averaged over the kills.")
    print("=" * 112)
    header = f"{'Lvl':>3}  {'Weapon':<16}{'Potions':>8}{'Kill%':>7}{'Survive%':>10}{'HP lost at kill':>17}{'Seconds':>9}{'Actions':>9}"
    print(header)
    print("-" * len(header))
    for level in (4, 5):
        for weapon in ("woodcutter axe", "fine longsword"):
            for potions in (0, 2):
                kill, survive, lost, secs, acts = boss_stats(level, weapon, armor, potions, runs=runs)
                best = "  <- best village gear" if weapon == "fine longsword" and potions == 2 else ""
                print(f"{level:>3}  {weapon:<16}{potions:>8}{kill * 100:>6.0f}%{survive * 100:>9.0f}%{lost:>17.1f}{secs:>9.1f}{acts:>9.1f}{best}")
    print()

    print("=" * 112)
    print(f"3. A whole run, Tavern Cellar to the Overseer ({max(100, runs // 4)} runs). The player buys gear with loot gold, carries up to")
    print("   2 potions, rests between rooms and farms respawns until he has the level each room wants (see REQUIRED_LEVEL).")
    print("=" * 112)
    done, first_try, farm, deaths, levels, secs = full_run(max(100, runs // 4))
    total = sum(levels.values()) or 1
    print(f"Killed the Overseer within 3 attempts: {done * 100:.0f}% of runs (first attempt: {first_try * 100:.0f}%).")
    print(f"Average farming fights before the Overseer: {farm:.1f}. Average deaths on the way: {deaths:.2f}. Seconds spent fighting: {secs:.0f}.")
    print("Level on reaching the Overseer: " + ", ".join(f"L{l} {n * 100 / total:.0f}%" for l, n in sorted(levels.items())))
    print()

    print("=" * 112)
    print("4. Weapons: damage per second against armor 2 for a level 3 player with a small shield (all reaction modifiers included).")
    print("=" * 112)
    print(f"{'Weapon':<18}{'Damage':>8}{'To hit':>7}{'Reaction':>9}{'Delay s':>9}{'Avg dmg':>9}{'Hit%':>6}{'DPS':>7}")
    for name in ("fists", "iron dagger", "enchanted dagger", "training spear", "rusty sword", "fine longsword", "woodcutter axe", "overseer's axe"):
        p = make_player(3, name, armor)
        (lo, hi), to_hit, mod, rec = WEAPONS[name]
        delay = p.delay_ms(p.acts[0]) / 1000.0
        avg, hit = (lo + hi) / 2, hit_chance(3, to_hit, 2)
        print(f"{name:<18}{lo:>4}-{hi:<3}{to_hit:>7}{p.reaction:>9.1f}{delay:>9.1f}{avg:>9.1f}{hit * 100:>6.0f}{avg * hit / delay:>7.2f}")


if __name__ == "__main__":
    main()
