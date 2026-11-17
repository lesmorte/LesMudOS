#!/usr/bin/env python3
"""Balance simulation for Vincent's Hollow (docs/GAME_SYSTEMS.md section 12).

Standalone: it does not talk to the driver. It plays the combat rules of
/std/living with the numbers of the creatures and items in the mudlib, and
prints tables for a solo player:

  1. room by room: win rate and HP lost for a player of a given level and gear
  2. the Goblin Overseer against level 4-5 players in village gear
  3. a whole run through the cellars with level-ups, rests and purchases

Rules (identical to the code):
  * One round per heartbeat (2 s). The player acts first (in the driver this is the order of the
    heartbeat list: players who logged in before the monsters became active come first), then every
    living enemy attacks. The player attacks the first living enemy of the room. A Goblin Minion
    summoned by the Overseer first attacks in the following round.
  * To hit: d20 + level + weapon to-hit >= 10 + defender armor. A natural 20
    always hits, a natural 1 always misses.
  * Damage is uniform in the range, minimum 1.
  * Level n needs 0/100/250/500/900 XP for n = 1..5; each level adds 8 max HP
    and heals fully, in the middle of a fight too (experience is paid at the kill). Base HP is 30.

Usage: python3 tests/balance_sim.py [runs]
"""
import random
import sys
from dataclasses import dataclass, field

RUNS = int(sys.argv[1]) if len(sys.argv) > 1 else 4000
XP_FOR_LEVEL = {1: 0, 2: 100, 3: 250, 4: 500, 5: 900}
POTION_HEAL = 20


@dataclass(frozen=True)
class Kind:
    name: str
    level: int
    hp: int
    dmg: tuple
    to_hit: int
    armor: int
    xp: int
    gold: tuple = (0, 0)


# The numbers of docs/GAME_SYSTEMS.md section 6 (the mudlib files must agree). These are the
# values after the first balance pass: the design's first draft (Guard 20 HP / armor 3, Warrior
# 22 HP / 3-5, Elite 38 HP, Overseer 75 HP / 4-7, rusty sword 2-5, ...) made the Goblin Barracks
# and everything after it unwinnable (see "Balance changes" in the design document).
KINDS = {k.name: k for k in [
    Kind("Large Rat", 1, 6, (1, 2), 0, 0, 8),
    Kind("Giant Rat", 1, 8, (1, 3), 0, 0, 12),
    Kind("Wandering Wolf", 2, 16, (2, 4), 1, 1, 25),
    Kind("Goblin Scavenger", 2, 10, (2, 3), 2, 1, 22, (2, 8)),
    Kind("Spore Bat", 2, 6, (1, 3), 1, 3, 20),
    Kind("Goblin Guard", 2, 16, (2, 3), 0, 2, 32, (4, 12)),
    Kind("Giant Cave Toad", 3, 30, (2, 5), 0, 0, 50),
    Kind("Goblin Warrior", 3, 16, (2, 4), 0, 2, 42, (5, 14)),
    Kind("Goblin Minion", 2, 12, (2, 4), 0, 1, 15),
    Kind("Goblin Elite Guard", 4, 22, (3, 5), 2, 3, 85, (12, 22)),
    Kind("Goblin Overseer", 5, 54, (4, 6), 2, 3, 250),
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

# weapon: (damage range, to hit); armor is the sum of worn pieces.
WEAPONS = {
    "fists": ((1, 3), 0),
    "rusty sword": ((3, 5), 0),
    "woodcutter axe": ((4, 7), 0),
    "fine longsword": ((5, 9), 0),
    "overseer's axe": ((6, 10), 0),
}
SHIELD, HELM, VEST = 1, 1, 3


def hit_chance(level, to_hit, armor):
    """Probability that d20 + level + to_hit >= 10 + armor, with natural 20 / 1 rules."""
    hits = 0
    for roll in range(1, 21):
        if roll == 20 or (roll != 1 and roll + level + to_hit >= 10 + armor):
            hits += 1
    return hits / 20


@dataclass
class Fighter:
    level: int
    hp: int
    max_hp: int
    dmg: tuple
    to_hit: int
    armor: int
    kind: Kind = None


def make_monster(name):
    k = KINDS[name]
    return Fighter(k.level, k.hp, k.hp, k.dmg, k.to_hit, k.armor, k)


def player_max_hp(level):
    return 30 + 8 * (level - 1)


def attack(att, defn, bonus=0, power=False):
    """One attack. Returns the damage dealt (0 on a miss)."""
    roll = random.randint(1, 20)
    if not (roll == 20 or (roll != 1 and roll + att.level + att.to_hit >= 10 + defn.armor)):
        return 0
    dmg = max(1, random.randint(*att.dmg))
    dmg += bonus
    if power and random.random() < 0.20:
        dmg *= 2
    return max(1, dmg)


def fight(player, enemies, potions=0, boss=False, drink_at=0.35, on_kill=None):
    """Fight until one side is dead. Mutates player and enemies. on_kill(player, enemy), if given, is called
    when the player's attack kills an enemy (the real game pays the experience then, and a level-up heals fully).
    Returns (player_alive, rounds, potions_left, boss_killed_hp_lost)."""
    rounds = 0
    # Boss state
    cried = False
    cry_left = 0
    summoned = False
    boss_dead_round = None
    hp_lost_at_boss_death = None
    start_hp = player.hp
    while player.hp > 0 and any(e.hp > 0 for e in enemies):
        rounds += 1
        # Player acts first.
        if potions > 0 and player.hp <= player.max_hp * drink_at:
            potions -= 1
            player.hp = min(player.max_hp, player.hp + POTION_HEAL)
        else:
            target = next(e for e in enemies if e.hp > 0)
            target.hp -= attack(player, target)
            if target.hp <= 0 and on_kill is not None:
                on_kill(player, target)
        if boss and boss_dead_round is None and enemies[0].hp <= 0:
            boss_dead_round = rounds
            hp_lost_at_boss_death = start_hp - player.hp
        for e in enemies[:len(enemies)]:  # a minion summoned this round first attacks next round (its heartbeat starts after the snapshot)
            if e.hp <= 0 or player.hp <= 0:
                continue
            if boss and e is enemies[0]:
                active = False
                if not summoned and e.hp * 2 <= e.max_hp:
                    summoned = True
                    enemies.append(make_monster("Goblin Minion"))
                if not cried:
                    cried = True
                    cry_left = 5
                    continue  # the Battle Cry replaces this round's attack
                if cry_left > 0:
                    cry_left -= 1
                    active = True
                player.hp -= attack(e, player, bonus=2 if active else 0, power=True)
            else:
                player.hp -= attack(e, player)
    return player.hp > 0, rounds, potions, hp_lost_at_boss_death


def make_player(level, weapon, armor, extra_hp=0):
    dmg, to_hit = WEAPONS[weapon]
    hp = player_max_hp(level) + extra_hp
    return Fighter(level, hp, hp, dmg, to_hit, armor)


def room_stats(creatures, level, weapon, armor, extra_hp=0, runs=RUNS):
    wins = lost = rounds_total = 0
    for _ in range(runs):
        p = make_player(level, weapon, armor, extra_hp)
        enemies = [make_monster(c) for c in creatures]
        alive, rounds, _, _ = fight(p, enemies)
        if alive:
            wins += 1
            lost += p.max_hp - p.hp
            rounds_total += rounds
    return wins / runs, (lost / wins if wins else float("nan")), (rounds_total / wins if wins else float("nan"))


def boss_stats(level, weapon, armor, potions, extra_hp=0, runs=RUNS):
    kills = survive = lost = 0
    for _ in range(runs):
        p = make_player(level, weapon, armor, extra_hp)
        enemies = [make_monster("Goblin Overseer")]
        alive, _, _, lost_at_kill = fight(p, enemies, potions, boss=True)
        if lost_at_kill is not None:
            kills += 1
            lost += lost_at_kill
        if alive:
            survive += 1
    return kills / runs, survive / runs, (lost / kills if kills else float("nan"))


# The level a sensible player has before taking on a room (the dungeon is advertised for 2-5).
REQUIRED_LEVEL = {"Tavern Cellar": 1, "Collapsed Passage": 1, "Storage Chamber": 2, "Guard Post": 2,
                  "Flooded Tunnel": 3, "Mushroom Cavern": 3, "Goblin Barracks": 4, "Overseer Hall": 4}
BOSS_LEVEL = 4
TRINKET_HP = 5  # the Magical Trinket from the Hidden Cache


def full_run(runs=RUNS):
    """A whole solo run, Tavern Cellar to the Overseer. The player starts with 25 gold (a rusty sword
    and a small shield), buys the best affordable weapon and up to 2 potions, rests to full between
    rooms, and goes back to farm cleared rooms (respawns) whenever he is below the level a room
    wants. A death costs 10% of the gold and a retry. Returns the share of runs that killed the
    Overseer (within 3 attempts, and at the first), the average number of farming fights and
    deaths, and the level at the Overseer."""
    done = first_try = 0
    farm_total = deaths_total = 0
    levels = {}
    for _ in range(runs):
        xp, gold, level = 0, 25 - 10 - 12, 1
        weapon, potions, trinket = "rusty sword", 0, 0
        cleared = []
        deaths = farm = 0
        failed = False

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
            """Experience and gold are paid at the kill; a level-up heals fully, even in the middle of a fight."""
            nonlocal xp, gold, level
            k = enemy.kind
            xp += k.xp
            if k.gold[1]:
                gold += random.randint(*k.gold)
            while level < 5 and xp >= XP_FOR_LEVEL[level + 1]:
                level += 1
                p.level = level
                p.max_hp = player_max_hp(level) + trinket
                p.hp = p.max_hp

        def attempt(creatures, hard=False):
            """Potions are saved for the hard rooms (and for a retry after a death)."""
            nonlocal gold, potions, deaths
            for tries in range(10):
                p = make_player(level, weapon, SHIELD, trinket)
                use = potions if hard or tries > 0 else 0
                alive, _, left, _ = fight(p, [make_monster(c) for c in creatures], use, on_kill=on_kill)
                potions = potions - use + left
                if alive:
                    return True
                deaths += 1
                gold = int(gold * 0.9)
            return False

        for name, creatures in ROOMS:
            while level < REQUIRED_LEVEL[name] and cleared and farm < 400:
                # Farm the best cleared room that is safe at this level.
                best = None
                for room, group in cleared:
                    if room_stats(group, level, weapon, SHIELD, trinket, runs=40)[0] >= 0.9:
                        best = (room, group)
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
            best = None
            for room, group in cleared:
                if room_stats(group, level, weapon, SHIELD, trinket, runs=40)[0] >= 0.9:
                    best = (room, group)
            if best is None:
                break
            farm += 1
            shop()
            attempt(best[1])
        shop()
        levels[level] = levels.get(level, 0) + 1
        for tries in range(3):
            p = make_player(level, weapon, SHIELD, trinket)
            alive, _, left, lost_at_kill = fight(p, [make_monster("Goblin Overseer")], min(potions, 2), boss=True)
            if lost_at_kill is not None:
                done += 1
                first_try += tries == 0
                break
            gold = int(gold * 0.9)
            potions = 0
            shop()
    return done / runs, first_try / runs, farm_total / runs, deaths_total / runs, levels


def main():
    random.seed(20260101)
    print("=" * 104)
    print("1. Room by room, solo, at the level a sensible player has (rested to full HP). 'Win' = the player survives.")
    print("   Gear: rusty sword, then woodcutter axe, then fine longsword; small shield; +5 HP trinket from the Mushroom")
    print("   Cavern on. No potions are used here.")
    print("=" * 104)
    header = f"{'Room':<19}{'Enemies':<22}{'Lvl':>3}  {'Weapon':<16}{'HP':>4}{'Win%':>6}{'HP lost':>9}{'Rounds':>8}"
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
        win, lost, rounds = room_stats(creatures, level, weapon, SHIELD, extra)
        hp = player_max_hp(level) + extra
        print(f"{room:<19}{label[:21]:<22}{level:>3}  {weapon:<16}{hp:>4}{win * 100:>5.0f}%{lost:>9.1f}{rounds:>8.1f}")
    print()

    print("=" * 104)
    print("2. The Goblin Overseer, solo, from full HP, in village gear only (small shield). Potions are drunk at 35% HP")
    print("   (one round each). 'Kill' = the Overseer dies before the player does (the minion may still be alive);")
    print("   'Survive' = the player is alive when the whole fight is over.")
    print("=" * 104)
    header = f"{'Lvl':>3}  {'Weapon':<16}{'Potions':>8}{'Kill%':>7}{'Survive%':>10}{'HP lost at kill':>17}"
    print(header)
    print("-" * len(header))
    for level in (4, 5):
        for weapon in ("woodcutter axe", "fine longsword"):
            for potions in (0, 2):
                kill, survive, lost = boss_stats(level, weapon, SHIELD, potions)
                best = "  <- best village gear" if weapon == "fine longsword" and potions == 2 else ""
                print(f"{level:>3}  {weapon:<16}{potions:>8}{kill * 100:>6.0f}%{survive * 100:>9.0f}%{lost:>17.1f}{best}")
    print()

    print("=" * 104)
    print(f"3. A whole run, Tavern Cellar to the Overseer ({RUNS // 4} runs). The player buys gear with loot gold, carries up to")
    print("   2 potions, rests between rooms and farms respawns until he has the level each room wants (see REQUIRED_LEVEL).")
    print("=" * 104)
    done, first_try, farm, deaths, levels = full_run(max(100, RUNS // 4))
    total = sum(levels.values()) or 1
    print(f"Killed the Overseer within 3 attempts: {done * 100:.0f}% of runs (first attempt: {first_try * 100:.0f}%).")
    print(f"Average farming fights before the Overseer: {farm:.1f}. Average deaths on the way: {deaths:.2f}.")
    print("Level on reaching the Overseer: " + ", ".join(f"L{l} {n * 100 / total:.0f}%" for l, n in sorted(levels.items())))


if __name__ == "__main__":
    main()
