#!/usr/bin/env python3
"""Balance simulation for Vincent's Hollow and the five archetypes (docs/CLASS_SYSTEM.md, COMBAT_TIMING.md, GAME_SYSTEMS.md).

Standalone: it does not talk to the driver. It plays the combat rules of /std/living with the numbers of the creatures,
items and class files in the mudlib, and prints tables for a solo player of each archetype:

  1. room by room: win rate, HP lost, fight length in seconds, at the level a sensible player has
  2. the Goblin Overseer against prepared level 4-5 characters
  3. a whole run through the cellars with level-ups, rests, purchases and farming, per archetype
  4. damage per second of every weapon, and a summary of how far each archetype is from the average

Rules (identical to the code; `python3 tests/balance_sim.py check` compares the numbers below with the mudlib files):
  * There are no rounds. Every combatant has a Ready Time. When it is ready and has an enemy it begins an action:
    Total Delay = (Reaction Time + action reaction modifier + action Recovery) x 2 seconds, where Reaction Time =
    base + the reaction modifiers of everything equipped. The effect happens Preparation x 2 seconds after the start.
  * The player starts the fight (as most players do): he is ready at once. A creature cannot begin its first action
    until its own Reaction Time x 2 seconds have passed. A creature summoned by the Overseer is drawn in the same way.
  * Hit rule: the defender's dodge is rolled first; then d20 + Attack against 10 + Armor + Defense, where
    Attack = round(AttackRate x Level) + to-hit bonuses and Defense = Level - 1. A natural 20 always hits, a natural 1
    always misses. Damage is uniform in the weapon's range, 5% critical (x1.5), a block (15% with a shield) halves it.
    Creatures have AttackRate 1.0 and no dodge or block.
  * Stamina and Mana come back continuously (per 10 seconds, the class's figures); the player is rested to full
    between fights. Level n needs 50 n (n-1) experience (100, 300, 600, 1000 for levels 2-5); a level adds 8 max HP and
    heals fully, in the middle of a fight too.
  * Player policies (what a sensible player does): potions at 35% HP; warrior Shield Bash on a creature that is
    preparing an action (the interrupt) or when Stamina is high; scout Precision Strike whenever it can pay; mage
    Arcane Bolt whenever it can pay; priest Minor Heal at 50% HP, Blessing at the start of a hard fight.

Usage: python3 tests/balance_sim.py [runs]      python3 tests/balance_sim.py check
"""
import heapq
import os
import random
import re
import sys
from dataclasses import dataclass

POTION_HEAL = 20
POTION_RECOVERY = 0.5
UNIT_MS = 2000
CRY_MS = 30000
BLESS_MS = 60000
# Skill numbers (the mudlib's /skills files; `check` compares them).
BASH_COST, BASH_REC, BASH_FACTOR = 15, 1.5, 0.75
STRIKE_COST, STRIKE_PREP, STRIKE_REC, STRIKE_HIT, STRIKE_CRIT = 25, 0.5, 1.0, 2, 15
BOLT_COST, BOLT_PREP, BOLT_REC, BOLT_DMG = 20, 0.5, 1.0, (6, 9)
HEAL_COST, HEAL_PREP, HEAL_REC, HEAL_AMOUNT = 25, 0.5, 1.0, 8
BLESS_COST = 25


def xp_for_level(n):
    return 50 * n * (n - 1)


@dataclass(frozen=True)
class Act:
    """An action: preparation and recovery in time units of 2 s."""
    id: str
    prep: float
    rec: float
    mult: int = 1
    mod: float = 0.0
    kind: str = "attack"   # "attack", or a special: "buff" (on itself), "summon", "mend" (heals itself)
    fx: str = ""           # on-hit effect of an attack ("slow:1.3:12", "stagger:2", "weaken:armor=-2:15") or the special's argument
    factor: float = 1.0    # damage factor (CombatAction.DamageFactor)
    hit: int = 0           # extra to-hit (CombatAction.ToHitBonus)


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
    "novice mace": ((9, 13), 0, 0.5, 1.0),
    "apprentice staff": ((7, 11), 1, 0.5, 1.0),
    "hunting bow": ((8, 12), 1, 0.5, 1.0),
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


@dataclass(frozen=True)
class Arch:
    name: str
    hp: int
    reaction: float
    rate: float
    stamina: int
    mana: int
    stamina_regen: float  # per 10 seconds
    mana_regen: float
    gold: int
    weapons: tuple        # progression, bought as gold allows
    shield: bool          # carries a shield (the warrior starts with one, others buy it)
    dodge: int = 0
    armor_bonus: int = 0
    hit_armed: int = 0
    spell_cost_pct: int = 0
    healing_pct: int = 0
    potions: int = 0      # in the starting kit


ARCHS = {a.name: a for a in [
    Arch("warrior", 40, 3.0, 1.00, 100, 20, 10, 2, 10, ("rusty sword", "woodcutter axe", "fine longsword"), True, armor_bonus=2, hit_armed=2),
    Arch("scout", 26, 2.0, 1.00, 100, 20, 8, 2, 10, ("hunting bow", "fine longsword"), False, dodge=8),
    Arch("priest", 26, 2.5, 0.97, 40, 100, 4, 4, 15, ("novice mace",), True, healing_pct=25, potions=1),
    Arch("mage", 24, 2.5, 0.96, 30, 100, 3, 8, 15, ("apprentice staff",), False, spell_cost_pct=-20, potions=1),
    Arch("adventurer", 30, 2.5, 0.98, 70, 70, 6, 5, 20, ("training spear", "woodcutter axe", "fine longsword"), True),
]}
TRINKET_HP = 5
HP_PER_LEVEL = 8
SHIELD_PRICE = 12
# Improvised Combat (adventurer) removes 0.5 of a weapon's reaction modifier.
REACTION_CUT = {"adventurer": 0.5}


def rnd(x):
    return int(x + 0.5 + 1e-9)


class Fighter:
    def __init__(self, level, hp, dmg, to_hit, armor, reaction, acts, kind=None, arch=None, shield=False, rate=1.0):
        self.level, self.hp, self.max_hp = level, hp, hp
        self.dmg, self.to_hit, self.armor = dmg, to_hit, armor
        self.reaction = reaction
        self.acts = acts
        self.kind = kind
        self.arch = arch
        self.is_player = arch is not None
        self.shield = shield
        self.rate = rate
        self.taken = 0
        self.attacks = 0
        self.summoned = False
        self.cry_until = 0
        self.bless_until = 0
        self.gen = 0
        self.ready_at = 0
        self.resolve_at = None
        self.stamina = self.mana = 0.0
        self.last_t = 0
        self.extra_hp = 0
        self.effects = {}      # timed effects by id: (until ms, {mod: value}, speed factor)
        self.paid = (0, 0)     # stamina and mana the current action cost (half comes back if it is interrupted)
        self.step = 0          # position in a creature's pattern
        self.fired = 0         # bit mask of the once-only triggers already used
        self.armor_extra = 0

    def alive(self):
        return self.hp > 0

    def add_effect(self, eid, t, seconds, mods=None, speed=1.0):
        """A timed effect; one with the same id is replaced (Living.AddEffect)."""
        self.effects[eid] = (t + seconds * 1000, dict(mods or {}), speed)

    def mod(self, key, t=0):
        return sum(e[1].get(key, 0) for e in self.effects.values() if e[0] > t)

    def speed(self, t=0):
        f = 1.0
        for e in self.effects.values():
            if e[0] > t:
                f *= e[2]
        return f

    def attack(self, t=0):
        return rnd(self.rate * self.level) + self.to_hit + (1 if t < self.bless_until else 0) + int(self.mod("tohit", t))

    def defense(self):
        return max(0, self.level - 1)

    def total_armor(self, t=0):
        return self.armor + (1 if t < self.bless_until else 0) + int(self.mod("armor", t))

    def dodge(self):
        if self.arch:
            return self.arch.dodge
        return self.kind.dodge if self.kind is not None and hasattr(self.kind, "dodge") else 0

    def block(self):
        return 15 if self.is_player and self.shield else 0

    def delay_ms(self, act, t=0):
        return round((self.reaction * self.speed(t) + act.mod + act.rec) * UNIT_MS)

    def prep_ms(self, act, t=0):
        return min(self.delay_ms(act, t), round(act.prep * UNIT_MS))

    def regen(self, t):
        if self.arch:
            dt = (t - self.last_t) / 10000.0
            self.stamina = min(self.arch.stamina, self.stamina + self.arch.stamina_regen * dt)
            self.mana = min(self.arch.mana, self.mana + self.arch.mana_regen * dt)
        self.last_t = t


def hit_chance(attack, armor, defense):
    hits = 0
    for roll in range(1, 21):
        if roll == 20 or (roll != 1 and roll + attack >= 10 + armor + defense):
            hits += 1
    return hits / 20


def player_max_hp(arch, level, extra=0):
    return arch.hp + HP_PER_LEVEL * (level - 1) + extra


def make_monster(name):
    k = CKINDS[name] if name in CKINDS else KINDS[name]
    return Fighter(k.level, k.hp, k.dmg, k.to_hit, k.armor, k.reaction, k.acts, k)


def make_player(arch, level, weapon, shield=None, extra_hp=0, full=True, armor_extra=0):
    """arch: an Arch or its name. weapon: a key of WEAPONS. shield: None means the archetype's own habit.
    armor_extra: armor from worn pieces other than the shield (vest, helm)."""
    if isinstance(arch, str):
        arch = ARCHS[arch]
    has_shield = arch.shield if shield is None else shield
    dmg, to_hit, mod, rec = WEAPONS[weapon]
    mod = max(0.0, mod - REACTION_CUT.get(arch.name, 0.0)) if mod > 0 else mod
    armor = arch.armor_bonus + (SHIELD[0] if has_shield else 0) + armor_extra
    reaction = arch.reaction + mod + (SHIELD[1] if has_shield else 0)
    to_hit += arch.hit_armed
    p = Fighter(level, player_max_hp(arch, level, extra_hp), dmg, to_hit, armor, reaction, (Act("attack", 0, rec),),
                arch=arch, shield=has_shield, rate=arch.rate)
    p.extra_hp = extra_hp
    p.stamina, p.mana = float(arch.stamina), float(arch.mana)
    p.weapon_rec = rec
    return p


def player_acts(p):
    """The actions a player of this archetype can start (Act ids matched in resolve)."""
    a = {"attack": Act("attack", 0, p.weapon_rec)}
    a["potion"] = Act("potion", 0, POTION_RECOVERY)
    a["bash"] = Act("bash", 0, BASH_REC)
    a["strike"] = Act("strike", STRIKE_PREP, STRIKE_REC)
    a["bolt"] = Act("bolt", BOLT_PREP, BOLT_REC)
    a["heal"] = Act("heal", HEAL_PREP, HEAL_REC)
    a["bless"] = Act("bless", 0.5, 1.0)
    return a


def choose(e):
    """ChooseAction of the mudlib creature files."""
    a = {x.id: x for x in e.acts}
    if e.kind is None or len(e.acts) == 1:
        return e.acts[0]
    if e.kind.name in CKINDS or hasattr(e.kind, "pattern"):
        return choose_corridor(e, a)
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


def spell_cost(p, base):
    return base * (100 + p.arch.spell_cost_pct) / 100.0


def player_choice(p, t, living, potions, hard, state):
    """Returns (act id, potions left)."""
    arch = p.arch
    p.regen(t)
    target = living[0]
    if potions > 0 and p.hp <= p.max_hp * 0.35:
        return "potion"
    if arch.name == "priest":
        if p.hp <= p.max_hp * 0.5 and p.mana >= HEAL_COST:
            return "heal"
        if hard and t >= p.bless_until and p.mana >= BLESS_COST + HEAL_COST and not state.get("blessed"):
            state["blessed"] = True
            return "bless"
    if arch.name == "warrior" and p.stamina >= BASH_COST and p.shield:
        if target.resolve_at is not None and target.resolve_at > t:
            return "bash"
        if p.stamina >= 60:
            return "bash"
    if arch.name == "scout" and p.stamina >= STRIKE_COST:
        return "strike"
    if arch.name == "mage" and p.mana >= spell_cost(p, BOLT_COST):
        return "bolt"
    return "attack"


def fight(player, enemies, potions=0, boss=False, on_kill=None, hard=None):
    """Fight until one side is dead. Mutates player and enemies. Returns a dict: alive, ms, actions, potions,
    boss_ms and lost_at_kill."""
    heap = []
    seq = 0
    if hard is None:
        hard = boss or len(enemies) >= 2

    def push(t, kind, ent, act=None, target=None):
        nonlocal seq
        seq += 1
        heapq.heappush(heap, (t, seq, kind, ent, act, target, ent.gen))

    start_hp = player.hp
    out = {"actions": 0, "boss_ms": None, "lost_at_kill": None}
    state = {}
    acts = player_acts(player)
    player.last_t = 0
    player.ready_at = 0
    push(0, "ready", player)
    for e in enemies:
        e.ready_at = round(e.reaction * UNIT_MS)
        push(e.ready_at, "ready", e)
    t = 0

    def stagger(e, now, seconds=2.0):
        e.gen += 1
        if e.resolve_at is not None and e.resolve_at > now:
            half = (e.ready_at - e.resolve_at) / 2
            e.resolve_at = None
            e.ready_at = round(now + half)
            if e.is_player:  # half the cost comes back
                e.stamina = min(e.arch.stamina, e.stamina + e.paid[0] / 2)
                e.mana = min(e.arch.mana, e.mana + e.paid[1] / 2)
        else:
            e.ready_at = max(e.ready_at, now) + round(seconds * 1000)
        push(e.ready_at, "ready", e)

    out["stagger"] = stagger
    while heap and player.hp > 0 and any(e.hp > 0 for e in enemies):
        t, _, kind, ent, act, target, gen = heapq.heappop(heap)
        if ent.hp <= 0 or gen != ent.gen:
            continue
        if kind == "ready":
            living = [e for e in enemies if e.hp > 0]
            if not living and not ent.is_player:
                continue
            if ent.is_player:
                choice = player_choice(ent, t, living, potions, hard, state)
                act = acts[choice]
                target = living[0]
                out["actions"] += 1
                ent.paid = (0, 0)
                if choice == "potion":
                    potions -= 1
                    out["potions_used"] = out.get("potions_used", 0) + 1
                    ent.hp = min(ent.max_hp, ent.hp + POTION_HEAL)
                    push(t + ent.delay_ms(act, t), "ready", ent)
                    ent.ready_at = t + ent.delay_ms(act, t)
                    continue
                if choice == "bash":
                    ent.stamina -= BASH_COST
                    ent.paid = (BASH_COST, 0)
                elif choice == "strike":
                    ent.stamina -= STRIKE_COST
                    ent.paid = (STRIKE_COST, 0)
                elif choice == "bolt":
                    ent.mana -= spell_cost(ent, BOLT_COST)
                    ent.paid = (0, spell_cost(ent, BOLT_COST))
                elif choice == "heal":
                    ent.mana -= HEAL_COST
                    ent.paid = (0, HEAL_COST)
                elif choice == "bless":
                    ent.mana -= BLESS_COST
                    ent.paid = (0, BLESS_COST)
            else:
                act = choose(ent)
                ent.taken += 1
                target = player
            delay, prep = ent.delay_ms(act, t), ent.prep_ms(act, t)
            ent.ready_at = t + delay
            push(ent.ready_at, "ready", ent)
            if prep > 0:
                ent.resolve_at = t + prep
                push(t + prep, "resolve", ent, act, target)
            else:
                ent.resolve_at = None
                resolve(ent, act, target, t, player, enemies, out, on_kill, push, boss)
        else:
            ent.resolve_at = None
            resolve(ent, act, target, t, player, enemies, out, on_kill, push, boss)
        if boss and out["boss_ms"] is None and enemies[0].hp <= 0:
            out["boss_ms"] = t
            out["lost_at_kill"] = start_hp - player.hp
    out.update(alive=player.hp > 0, ms=t, potions=potions)
    return out


def do_hit(att, defn, t, to_hit_bonus=0, crit=5, factor=1.0, mult=1, bonus=0):
    """One physical attack. Returns (damage, blocked) with damage 0 on a miss or dodge."""
    if random.randint(1, 100) <= defn.dodge():
        return 0, False
    roll = random.randint(1, 20)
    need = 10 + defn.total_armor(t) + defn.defense()
    if not (roll == 20 or (roll != 1 and roll + att.attack(t) + to_hit_bonus >= need)):
        return 0, False
    dmg = max(1, random.randint(*att.dmg) + bonus) * mult
    if factor != 1.0:
        dmg = max(1, rnd(dmg * factor))
    if random.randint(1, 100) <= crit:
        dmg = max(1, rnd(dmg * 1.5))
    blocked = random.randint(1, 100) <= defn.block()
    if blocked:
        dmg = max(1, dmg // 2)
    return dmg, blocked


def resolve(ent, act, target, t, player, enemies, out, on_kill, push, boss):
    if target.hp <= 0 and act.id not in ("heal", "bless"):
        return
    if not ent.is_player and getattr(ent.kind, "corridor", False):
        return resolve_corridor(ent, act, target, t, enemies, out, push)
    if act.id == "cry":
        ent.cry_until = t + CRY_MS
        return
    if act.id == "summon":
        m = make_monster("Goblin Minion")
        enemies.append(m)
        m.ready_at = t + round(m.reaction * UNIT_MS)
        push(m.ready_at, "ready", m)
        return
    if act.id == "heal":
        ent.hp = min(ent.max_hp, ent.hp + rnd(HEAL_AMOUNT * (100 + ent.arch.healing_pct) / 100.0))
        return
    if act.id == "bless":
        ent.bless_until = t + BLESS_MS
        return
    if act.id == "bolt":
        if target.dodge() and random.randint(1, 100) <= target.dodge():
            return
        dmg = random.randint(*BOLT_DMG)
        target.hp -= dmg
        if target.hp <= 0 and on_kill is not None:
            on_kill(player, target)
        return
    if ent.is_player:
        kw = {}
        if act.id == "strike":
            kw = dict(to_hit_bonus=STRIKE_HIT, crit=STRIKE_CRIT)
        if act.id == "bash":
            kw = dict(factor=BASH_FACTOR)
        dmg, _ = do_hit(ent, target, t, **kw)
        if dmg <= 0:
            return
        target.hp -= dmg
        if act.id == "bash" and target.hp > 0:
            out["stagger"](target, t)
        if target.hp <= 0 and on_kill is not None:
            on_kill(player, target)
        return
    bonus = 2 if t < ent.cry_until else 0
    dmg, _ = do_hit(ent, target, t, mult=act.mult, bonus=bonus)
    if dmg > 0:
        target.hp -= dmg


def room_stats(arch, creatures, level, weapon, extra_hp=0, runs=1500, potions=0, shield=None):
    wins = lost = ms_total = actions_total = 0
    for _ in range(runs):
        p = make_player(arch, level, weapon, shield, extra_hp)
        enemies = [make_monster(c) for c in creatures]
        r = fight(p, enemies, potions)
        if r["alive"]:
            wins += 1
            lost += p.max_hp - p.hp
            ms_total += r["ms"]
            actions_total += r["actions"]
    n = wins or 1
    return wins / runs, lost / n, ms_total / n / 1000.0, actions_total / n


def boss_stats(arch, level, weapon, potions, extra_hp=0, runs=1500, shield=None):
    kills = survive = lost = ms = actions = 0
    for _ in range(runs):
        p = make_player(arch, level, weapon, shield, extra_hp)
        r = fight(p, [make_monster("Goblin Overseer")], potions, boss=True)
        if r["lost_at_kill"] is not None:
            kills += 1
            lost += r["lost_at_kill"]
            ms += r["boss_ms"]
            actions += r["actions"]
        survive += r["alive"]
    n = kills or 1
    return kills / runs, survive / runs, lost / n, ms / n / 1000.0, actions / n


REQUIRED_LEVEL = {"Tavern Cellar": 1, "Collapsed Passage": 1, "Storage Chamber": 2, "Guard Post": 2,
                  "Flooded Tunnel": 3, "Mushroom Cavern": 3, "Goblin Barracks": 4, "Overseer Hall": 4}
BOSS_LEVEL = 4
PRICES = {"rusty sword": 10, "woodcutter axe": 18, "fine longsword": 60, "hunting bow": 20, "novice mace": 12,
          "apprentice staff": 10, "training spear": 8}


def full_run(arch_name, runs=500):
    """A whole solo run, Tavern Cellar to the Overseer. The player has the archetype's kit and gold, buys the next
    weapon of the archetype's progression and a shield when it can carry one, keeps up to 2 potions, rests to full
    between rooms and goes back to farm cleared rooms (respawns) whenever he is below the level a room wants. A
    death costs 10% of the gold and a retry. Returns: share that killed the Overseer within 3 attempts, share at
    the first attempt, farming fights, deaths, level histogram at the Overseer, fight seconds, share that reached
    the Overseer at level 5 or lower (always true unless farming ran away) and the farming cap hits."""
    arch = ARCHS[arch_name]
    done = first_try = 0
    farm_total = deaths_total = fight_ms_total = 0
    levels = {}
    for _ in range(runs):
        xp, gold, level = 0, arch.gold, 1
        stage, potions, trinket = 0, arch.potions, 0
        shield = arch.shield and arch_name == "warrior"
        cleared = []
        deaths = farm = 0
        failed = False
        fight_ms = 0
        buy_shield = arch.shield and arch_name != "warrior"

        def weapon():
            return arch.weapons[stage]

        def shop():
            nonlocal gold, potions, stage, shield
            if buy_shield and not shield and gold >= SHIELD_PRICE:
                gold -= SHIELD_PRICE
                shield = True
            while stage + 1 < len(arch.weapons):
                nxt = arch.weapons[stage + 1]
                cost = PRICES[nxt] - PRICES[arch.weapons[stage]] // 2
                if gold >= cost + (15 if arch_name != "mage" else 0) and (nxt != "fine longsword" or gold >= PRICES[nxt] - 5 + 15):
                    gold -= cost
                    stage += 1
                else:
                    break
            while potions < 2 and gold >= 15:
                gold -= 15
                potions += 1

        def on_kill(p, enemy):
            nonlocal xp, gold, level
            k = enemy.kind
            xp += k.xp
            if k.gold[1]:
                gold += random.randint(*k.gold)
            while level < 5 and xp >= xp_for_level(level + 1):
                level += 1
                p.level = level
                p.max_hp = player_max_hp(arch, level, trinket)
                p.hp = p.max_hp

        def attempt(creatures, hard=False):
            nonlocal gold, potions, deaths, fight_ms
            for tries in range(10):
                p = make_player(arch, level, weapon(), shield, trinket)
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
                if room_stats(arch, group, level, weapon(), trinket, runs=40, shield=shield)[0] >= 0.9:
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
                potions += 1
            if name == "Mushroom Cavern":
                gold += random.randint(30, 60)
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
            p = make_player(arch, level, weapon(), shield, trinket)
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


# ---------------------------------------------------------------- the Greenvale Transit Corridor (levels 5 to 10)
#
# 31 creatures (mudlib/domains/greenvale/corridor/mob/<id>.cs), levels 5 to 10 for all five callings with village gear,
# the pace model and the experience proposal. Run: python3 tests/balance_sim.py corridor | pace | xp  (docs/CORRIDOR_BALANCE.md)


def A(id, name, prep=0.0, rec=0.5, mult=1, mod=0.0, kind="attack", fx="", factor=1.0, hit=0):
    return Act(id, prep, rec, mult, mod, kind, fx, factor, hit)


@dataclass(frozen=True)
class CKind:
    """A corridor creature. pattern = (first actions, repeating cycle, ((HP percent, action id), ...) once each)."""
    id: str
    name: str
    level: int
    hp: int
    dmg: tuple
    to_hit: int
    armor: int
    xp: int
    reaction: float
    acts: tuple
    pattern: tuple
    gold: tuple = (0, 0)
    loot: tuple = ()          # ((item, percent), ...)
    band: tuple = (5, 6)      # player levels meant to fight it
    boss: bool = False
    dodge: int = 0
    aggressive: bool = False
    corridor: bool = True

    @property
    def tag(self):
        return self.id


def P(first=(), cycle=("hit",), at=()):
    return (tuple(first), tuple(cycle), tuple(at))


# Items a corpse may hold (existing items only) and what a perfect-knowledge player gets for them: a potion is
# drunk (worth its price), anything else is sold for half its Value.
ITEM_GOLD = {"healing_potion": 15.0, "trail_ration": 2.0, "iron_dagger": 3.0, "rusty_sword": 5.0, "training_spear": 4.0,
             "small_shield": 6.0, "woodcutter_axe": 9.0, "hunting_bow": 10.0, "novice_mace": 6.0, "bag_of_coins": 80.0,
             "enchanted_dagger": 40.0}

#   id, name, level, hp, dmg, to_hit, armor, xp, reaction, acts, pattern, gold, loot, band, boss, dodge, aggressive
CORRIDOR = [
    CKind("field_rabbit", "Field Rabbit", 1, 8, (1, 2), 0, 0, 12, 1.5, (A("bite", "Bite", 0, 0.5),), P(cycle=("bite",)),
          band=(5, 6)),
    CKind("roe_deer", "Roe Deer", 3, 22, (1, 3), 0, 0, 26, 2.5, (A("kick", "Kick", 0, 1.0),), P(cycle=("kick",)),
          band=(5, 7)),
    CKind("grey_fox", "Grey Fox", 4, 26, (2, 4), 1, 1, 65, 1.5, (A("nip", "Nip", 0, 0.5),), P(cycle=("nip",)),
          band=(5, 6), dodge=10),
    CKind("stray_wolf", "Stray Wolf", 5, 60, (6, 10), 1, 1, 120, 2.0, (A("bite", "Bite", 0, 0.5),), P(cycle=("bite",)),
          band=(5, 7)),
    CKind("forest_spider", "Forest Spider", 5, 57, (4, 8), 2, 0, 120, 1.5,
          (A("bite", "Bite", 0, 0.5), A("venom", "Slowing Bite", 0, 0.5, fx="slow:1.25:8")),
          P(cycle=("bite", "bite", "bite", "venom")), band=(5, 7), aggressive=True),
    CKind("bandit_lookout", "Bandit Lookout", 6, 48, (10, 15), 2, 2, 145, 3.0, (A("slash", "Slash", 0, 1.0),),
          P(cycle=("slash",)), gold=(3, 8), loot=(("iron_dagger", 20), ("trail_ration", 25)), band=(6, 7), aggressive=True),
    CKind("forest_wolf", "Forest Wolf", 6, 73, (3, 6), 2, 1, 165, 2.0,
          (A("bite", "Bite", 0, 0.5), A("savage", "Savage Bite", 0, 0.5, mult=2)),
          P(cycle=("bite", "bite", "bite", "savage")), band=(5, 7)),
    CKind("wild_boar", "Wild Boar", 6, 40, (11, 19), 1, 2, 160, 3.5,
          (A("gore", "Gore", 0, 1.0), A("charge", "Charge", 1.0, 1.5, mult=2)),
          P(first=("charge",), cycle=("gore",)), band=(6, 8)),
    CKind("goblin_scout", "Goblin Scout", 7, 80, (5, 8), 3, 2, 220, 2.0, (A("stab", "Quick Stab", 0, 0.5),),
          P(cycle=("stab",)), gold=(3, 9), loot=(("iron_dagger", 20), ("healing_potion", 4)), band=(7, 8), aggressive=True),
    CKind("goblin_raider", "Goblin Raider", 7, 53, (7, 11), 2, 3, 165, 3.0,
          (A("hack", "Cleaver Hack", 0, 1.0), A("whoop", "War Whoop", 0.5, 0.5, kind="buff", fx="damage=3,speed=0.85:25")),
          P(cycle=("hack",), at=((50, "whoop"),)), gold=(4, 10), loot=(("rusty_sword", 15), ("training_spear", 15), ("healing_potion", 4)),
          band=(6, 9), aggressive=True),
    CKind("boar_brute", "Boar Brute", 7, 45, (8, 14), 2, 3, 210, 4.5,
          (A("gore", "Gore", 0, 1.5), A("trample", "Trample", 1.0, 2.0, mult=2)),
          P(cycle=("gore", "gore", "trample")), band=(6, 8)),
    CKind("tunnel_rat", "Tunnel Rat", 7, 40, (3, 6), 3, 1, 160, 1.5, (A("bite", "Bite", 0, 0.5),), P(cycle=("bite",)),
          band=(7, 8), aggressive=True),
    CKind("woodland_spider", "Woodland Spider", 8, 91, (3, 5), 3, 1, 370, 1.5,
          (A("bite", "Bite", 0, 0.5), A("venom", "Slowing Bite", 0, 0.5, fx="slow:1.25:8")),
          P(cycle=("bite", "bite", "bite", "venom")), band=(7, 9), aggressive=True),
    CKind("rogue_bandit", "Rogue Bandit", 8, 60, (10, 16), 3, 3, 300, 3.0,
          (A("slash", "Slash", 0, 1.0), A("pommel", "Pommel Strike", 0, 1.5, fx="stagger:2", factor=0.5)),
          P(cycle=("slash", "slash", "pommel")), gold=(5, 12), loot=(("rusty_sword", 15), ("healing_potion", 5), ("trail_ration", 20)),
          band=(8, 9), aggressive=True),
    CKind("young_black_bear", "Young Black Bear", 8, 57, (8, 15), 2, 2, 320, 4.0,
          (A("swipe", "Swipe", 0, 1.5), A("maul", "Maul", 1.0, 2.0, mult=2)),
          P(cycle=("swipe", "swipe", "maul")), band=(7, 8)),
    CKind("black_bear", "Black Bear", 8, 54, (8, 13), 3, 3, 320, 4.5,
          (A("swipe", "Swipe", 0, 1.5), A("maul", "Maul", 1.0, 2.0, mult=2), A("roar", "Bellowing Roar", 0.5, 0.5, kind="buff", fx="damage=3,speed=0.85:30")),
          P(cycle=("swipe", "swipe", "maul"), at=((50, "roar"),)), band=(7, 9)),
    CKind("heartwood_goblin", "Goblin Warrior", 8, 56, (5, 8), 3, 4, 240, 3.0,
          (A("slash", "Slash", 0, 1.0), A("heavy", "Heavy Swing", 0.5, 1.5, mult=2)),
          P(cycle=("slash", "slash", "slash", "heavy")), gold=(5, 12), loot=(("training_spear", 15), ("iron_dagger", 15), ("healing_potion", 4)),
          band=(7, 9), aggressive=True),
    CKind("highway_bandit", "Highway Bandit", 8, 59, (6, 9), 3, 3, 190, 3.0,
          (A("slash", "Slash", 0, 1.0), A("pommel", "Pommel Strike", 0, 1.5, fx="stagger:2", factor=0.5)),
          P(cycle=("slash", "slash", "pommel")), gold=(5, 13), loot=(("rusty_sword", 15), ("healing_potion", 6), ("trail_ration", 20)),
          band=(6, 9), aggressive=True),
    CKind("thorn_woodling", "Thorn Woodling", 9, 53, (10, 16), 2, 4, 440, 4.0,
          (A("lash", "Branch Lash", 0, 1.5), A("thorns", "Thorn Barrage", 0.5, 1.5, fx="weaken:armor=-2:12")),
          P(cycle=("lash", "lash", "thorns")), band=(8, 10)),
    CKind("forest_stalker", "Forest Stalker", 9, 70, (4, 7), 4, 2, 500, 2.0,
          (A("claw", "Claw", 0, 1.0), A("ambush", "Ambush Strike", 0.5, 1.0, mult=2, hit=4)),
          P(first=("ambush",), cycle=("claw",)), band=(8, 10), dodge=15, aggressive=True),
    CKind("ancient_wolf", "Ancient Wolf", 9, 69, (8, 13), 3, 3, 550, 3.0,
          (A("bite", "Rending Bite", 0, 1.0), A("howl", "Howl", 0.5, 0.5, kind="buff", fx="damage=2,speed=0.75:25")),
          P(cycle=("bite",), at=((60, "howl"),)), band=(9, 10), aggressive=True),
    CKind("rogue_goblin_veteran", "Rogue Goblin Veteran", 9, 53, (7, 10), 4, 5, 280, 3.5,
          (A("slash", "Slash", 0, 1.0), A("daze", "Dazing Blow", 0, 1.5, fx="weaken:tohit=-2,damage=-2:12", factor=0.75)),
          P(cycle=("slash", "slash", "daze")), gold=(6, 16), loot=(("healing_potion", 7), ("woodcutter_axe", 10), ("trail_ration", 20)),
          band=(8, 10), aggressive=True),
    CKind("restless_spirit", "Restless Spirit", 9, 64, (8, 13), 3, 0, 540, 2.5,
          (A("touch", "Grave Touch", 0, 1.0), A("chill", "Chill of the Grave", 0, 1.0, fx="slow:1.2:8")),
          P(cycle=("touch", "touch", "touch", "chill")), band=(9, 10), dodge=15, aggressive=True),
    CKind("ancient_skeleton", "Ancient Skeleton", 9, 54, (8, 13), 3, 4, 450, 3.5,
          (A("blade", "Rusted Blade", 0, 1.0), A("crush", "Bone Crusher", 1.0, 1.5, mult=2)),
          P(cycle=("blade", "blade", "blade", "crush")), gold=(2, 8), band=(9, 10), aggressive=True),
    CKind("goblin_tunneler", "Goblin Tunneler", 8, 60, (5, 8), 3, 3, 240, 3.0,
          (A("pick", "Pick Strike", 0, 1.0), A("overhead", "Overhead Swing", 1.0, 1.5, mult=2)),
          P(cycle=("pick", "pick", "overhead")), gold=(4, 10), loot=(("iron_dagger", 15), ("healing_potion", 4), ("trail_ration", 20)),
          band=(7, 9), aggressive=True),
    CKind("root_creeper", "Root Creeper", 8, 58, (8, 13), 2, 3, 310, 3.5,
          (A("lash", "Root Lash", 0, 1.0), A("grasp", "Clinging Grasp", 0, 1.5, fx="slow:1.25:8")),
          P(cycle=("lash", "lash", "grasp")), band=(7, 9), aggressive=True),
    # bosses and near-bosses: two special actions each
    CKind("bandit_leader", "Bandit Leader", 10, 100, (6, 10), 4, 4, 1550, 3.5,
          (A("slash", "Sabre Slash", 0, 1.0), A("smash", "Shield Smash", 0.5, 1.5, fx="stagger:3", factor=0.75),
           A("rally", "Call the Pack", 0.5, 1.0, kind="summon", fx="bandit_lookout")),
          P(cycle=("slash", "slash", "smash"), at=((50, "rally"),)), gold=(30, 60),
          loot=(("bag_of_coins", 100), ("healing_potion", 60), ("enchanted_dagger", 10)), band=(8, 10), boss=True, aggressive=True),
    CKind("forest_panther", "Forest Panther", 10, 120, (6, 10), 5, 3, 1550, 2.0,
          (A("claw", "Claw", 0, 1.0), A("pounce", "Pouncing Leap", 0.5, 1.0, mult=2, hit=4), A("rake", "Raking Frenzy", 0, 1.0, fx="weaken:armor=-2:12")),
          P(first=("pounce",), cycle=("claw", "claw", "rake", "claw")), band=(9, 10), boss=True, dodge=15, aggressive=True),
    CKind("warren_broodmother", "Warren Broodmother", 10, 80, (6, 10), 3, 3, 1550, 4.0,
          (A("fang", "Venom Fang", 0, 1.0), A("spit", "Webbed Spit", 0.5, 1.5, fx="slow:1.25:10", factor=0.75),
           A("brood", "Call the Brood", 0.5, 1.0, kind="summon", fx="tunnel_rat")),
          P(cycle=("fang", "fang", "spit"), at=((66, "brood"), (33, "brood"))), band=(8, 10), boss=True, aggressive=True),
    CKind("barrow_guardian", "Barrow Guardian", 10, 150, (10, 15), 3, 4, 880, 4.5,
          (A("slam", "Stone Fist", 0, 1.5), A("quake", "Earthshaker", 1.0, 2.0, mult=2, fx="stagger:2"),
           A("stone", "Stone Skin", 0.5, 0.5, kind="buff", fx="armor=3:15")),
          P(cycle=("slam", "slam", "quake"), at=((50, "stone"),)), gold=(10, 24), band=(9, 10), boss=True, aggressive=True),
    CKind("fallen_knight", "Fallen Knight", 10, 155, (6, 10), 5, 5, 1550, 3.5,
          (A("cleave", "Rusted Cleave", 0, 1.0), A("crush", "Crushing Blow", 1.0, 1.5, mult=2),
           A("ward", "Shield of the Fallen", 0.5, 0.5, kind="buff", fx="armor=2,speed=0.9:15")),
          P(first=("ward",), cycle=("cleave", "cleave", "crush")), gold=(20, 40), loot=(("healing_potion", 50),), band=(9, 10), boss=True, aggressive=True),
]
CKINDS = {k.id: k for k in CORRIDOR}


def parse_fx(spec):
    """'damage=3,speed=0.85' -> ({'damage': 3.0}, 0.85)"""
    mods, speed = {}, 1.0
    for part in spec.split(","):
        if not part:
            continue
        key, value = part.split("=")
        if key == "speed":
            speed = float(value)
        else:
            mods[key] = float(value)
    return mods, speed


def choose_corridor(e, a):
    """ChooseAction of the corridor creature files: the once-only triggers by HP, then the opening actions, then the cycle."""
    first, cycle, triggers = e.kind.pattern
    for i, (percent, aid) in enumerate(triggers):
        if not e.fired & (1 << i) and e.hp * 100 <= percent * e.max_hp:
            e.fired |= 1 << i
            return a[aid]
    n = e.step
    e.step += 1
    return a[first[n] if n < len(first) else cycle[(n - len(first)) % len(cycle)]]


def resolve_corridor(ent, act, target, t, enemies, out, push):
    if act.kind == "buff":
        spec, secs = act.fx.split(":")
        mods, speed = parse_fx(spec)
        ent.add_effect(act.id, t, int(secs), mods, speed)
        return
    if act.kind == "summon":
        m = make_monster(act.fx)
        enemies.append(m)
        m.ready_at = t + round(m.reaction * UNIT_MS)
        push(m.ready_at, "ready", m)
        return
    if act.kind == "mend":
        ent.hp = min(ent.max_hp, ent.hp + int(act.fx))
        return
    dmg, _ = do_hit(ent, target, t, to_hit_bonus=act.hit, factor=act.factor, mult=act.mult, bonus=int(ent.mod("damage", t)))
    if dmg <= 0:
        return
    target.hp -= dmg
    if act.fx and target.hp > 0:
        kind, rest = act.fx.split(":", 1)
        if kind == "slow":
            factor, secs = rest.split(":")
            target.add_effect("slow-" + ent.kind.id, t, int(secs), {}, float(factor))
        elif kind == "stagger":
            out["stagger"](target, t, float(rest))
        elif kind == "weaken":
            spec, secs = rest.split(":")
            mods, speed = parse_fx(spec)
            target.add_effect("weaken-" + ent.kind.id, t, int(secs), mods, speed)


# ---- gear: what a character of each calling can have from the village (nothing beyond it exists yet)

# weapon, shield. Everybody who has killed the Overseer has the Leather Commander's Vest (armor 3) and, from the first
# quest, the Guild Initiate's Helm (1); the Hidden Cache gives the +5 HP trinket. The Overseer's axe (17-25) is for the
# callings that may wield an axe; the others buy the Fine Iron Longsword (60 gold, a rare stock) or keep to what they have.
GEAR = {"warrior": ("overseer's axe", True), "scout": ("fine longsword", False), "priest": ("novice mace", True),
        "mage": ("iron dagger", False), "adventurer": ("overseer's axe", True)}
GEAR_ARMOR = 4     # vest 3 + helm 1
GEAR_HP = TRINKET_HP
CORRIDOR_POTIONS = 5


def cplayer(arch, level):
    weapon, shield = GEAR[arch]
    return make_player(arch, level, weapon, shield, GEAR_HP, armor_extra=GEAR_ARMOR)


def cfight(arch, level, key, potions=0, runs=600, start=None, seed=None):
    """Fight creature `key` one-on-one `runs` times from full HP. start = (stamina, mana) at the start, or full.
    Returns means: win rate, seconds, net HP lost in won fights, potions drunk, total damage taken, end resources."""
    wins = 0
    sec_w = sec_l = 0.0
    lost = used = dtot = 0.0
    end_s = end_m = 0.0
    boss = CKINDS[key].boss
    for _ in range(runs):
        p = cplayer(arch, level)
        if start:
            p.stamina, p.mana = start
        e = make_monster(key)
        r = fight(p, [e], potions, boss=boss)
        u = r.get("potions_used", 0)
        if r["alive"]:
            p.regen(r["ms"])
            wins += 1
            sec_w += r["ms"] / 1000.0
            lost += p.max_hp - p.hp
            used += u
            dtot += p.max_hp - p.hp + POTION_HEAL * u
            end_s += p.stamina
            end_m += p.mana
        else:
            sec_l += r["ms"] / 1000.0
    n = wins or 1
    return dict(win=wins / runs, sec=sec_w / n, sec_loss=sec_l / max(1, runs - wins), lost=lost / n, used=used / n,
                dtot=dtot / n, end_s=end_s / n, end_m=end_m / n)


# ---- the pace model (docs/CORRIDOR_BALANCE.md section 1)
ROOM_SECONDS = 4.0          # one move: the command, the room text, the next decision
ROOMS_BETWEEN_KILLS = 3     # between one spawn and the next on a woodland or deep loop
LOOT_SECONDS = 3.0          # `loot` the corpse
POTION_SECONDS = 2.0        # `drink potion` out of a fight
RESUPPLY = 0.05             # share of the time spent buying potions in bulk
HP_REGEN_PER_SECOND = 0.1   # Adventurer.Tick: 1 HP per 10 s out of combat (3 per 10 s in a safe room)
ROOMS_TO_PREY = {5: 14, 6: 26, 7: 40, 8: 54, 9: 68, 10: 76}   # rooms from the Hollow's guest rooms to where level n prey lives
CORPSE_SECONDS = 10.0       # find and loot the corpse after a death
# A perfect-knowledge player rests in a haven (a safe room: 3 HP per 10 s, Stamina and Mana twice as fast) whenever the hunting
# ground has one within HAVEN_ROOMS rooms. Share of the time that is true, by character level (segments 1 to 3 have a haven
# beside their loops; Heartwood and the dungeons have none).
HAVEN_ROOMS = 6
HAVEN_SHARE = {5: 1.0, 6: 1.0, 7: 0.8, 8: 0.5, 9: 0.25, 10: 0.25}


def kind_gold(k):
    """Average gold a kill is worth to a player: coins, plus the loot sold (or a potion drunk)."""
    coins = (k.gold[0] + k.gold[1]) / 2.0
    return coins + sum(ITEM_GOLD[i] * pct / 100.0 for i, pct in k.loot)


def regen_hp(arch):
    return HP_REGEN_PER_SECOND * (1.5 if arch == "adventurer" else 1.0)


def pace_cell(arch, level, key, f=0.0, runs=400, haven=True):
    """One calling at one level hunting one creature, in the steady state. f is the share of the damage taken that is
    healed by bought potions (15 gold for 20 HP, drunk after the fight); the rest heals by natural regeneration. Returns the
    seconds of a whole cycle per kill (fight, loot and walk, recovery, and for the lost fights the way back from the Hollow)
    and the experience per hour. Two passes so that Stamina and Mana at the start of a fight are what the recovery gave back."""
    k = CKINDS[key]
    a = ARCHS[arch]
    start = None
    walk = ROOMS_BETWEEN_KILLS * ROOM_SECONDS + LOOT_SECONDS
    for _ in range(3):
        c = cfight(arch, level, key, 0, runs, start)
        potions = f * c["dtot"] / POTION_HEAL
        t_place = max(0.0, c["dtot"] * (1 - f)) / regen_hp(arch) + potions * POTION_SECONDS
        t_haven = 2 * HAVEN_ROOMS * ROOM_SECONDS + max(0.0, c["dtot"] * (1 - f)) / (3 * regen_hp(arch)) + potions * POTION_SECONDS
        h = HAVEN_SHARE[min(10, max(5, level))] if haven else 0.0
        t_hp = (1 - h) * t_place + h * min(t_place, t_haven)
        wait = t_hp + walk
        start = (min(a.stamina, c["end_s"] + a.stamina_regen * wait * (1 + h) / 10.0), min(a.mana, c["end_m"] + a.mana_regen * wait * (1 + h) / 10.0))
    w = c["win"]
    win_cycle = (c["sec"] + walk + t_hp) * (1 + RESUPPLY)
    loss_cycle = c["sec_loss"] + ROOMS_TO_PREY[min(10, max(5, level))] * ROOM_SECONDS + CORPSE_SECONDS
    cycle = w * win_cycle + (1 - w) * loss_cycle
    xph = 3600.0 * w * k.xp / cycle if cycle else 0.0
    return dict(win=w, fight=c["sec"], dtot=c["dtot"], lost=c["lost"], t_hp=t_hp, walk=walk, cycle=cycle, xph=xph,
                potions=potions, win_cycle=win_cycle)


def prey(level):
    """The ordinary creatures a character of this level hunts: those of its own level and one above (bosses are visits)."""
    return [k for k in CORRIDOR if k.level in (level, level + 1) and not k.boss and k.level >= 5]


def potion_share(arch, level, keys, runs=300, funded=True, haven=True):
    """The share of the damage taken in a hunting mix (the creatures in `keys`, killed in equal numbers) that the mix's own gold
    buys back in potions: the gold of all the kills is pooled, and potions are drunk wherever they save most time."""
    if not funded:
        return 0.0
    gold = sum(kind_gold(CKINDS[k]) for k in keys)
    damage = sum(pace_cell(arch, level, k, 0.0, runs, haven)["dtot"] for k in keys)
    return min(1.0, (gold / 15.0) / (damage / POTION_HEAL)) if damage else 1.0


def pace_mix(arch, level, runs=300, funded=True, f=None, haven=True):
    """Experience per hour of one calling at one level hunting the mix of prey(level) in equal numbers."""
    keys = [k.id for k in prey(level)]
    if f is None:
        f = potion_share(arch, level, keys, runs, funded, haven)
    cells = [pace_cell(arch, level, k, f, runs, haven) for k in keys]
    xp = sum(c["win"] * CKINDS[k].xp for c, k in zip(cells, keys))
    cycle = sum(c["cycle"] for c in cells)
    return dict(xph=3600.0 * xp / cycle, f=f, cycle=cycle / len(cells), win=sum(c["win"] for c in cells) / len(cells),
                fight=sum(c["fight"] for c in cells) / len(cells), best=max(c["xph"] for c in cells), worst=min(c["xph"] for c in cells), n=len(cells))


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
              "overseer's axe": "overseers_axe", "novice mace": "novice_mace", "apprentice staff": "apprentice_staff",
              "hunting bow": "hunting_bow"}
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
    check_classes(os.path.join(root, "..", ".."), problems, num)
    for p in problems:
        print("MISMATCH", p)
    print("simulation numbers match the mudlib" if not problems else f"{len(problems)} mismatch(es)")
    return not problems




def check_classes(read_root, problems, num):
    root = os.path.join(read_root, "classes")
    for name, a in ARCHS.items():
        with open(os.path.join(root, name + ".cs"), encoding="utf-8") as f:
            text = f.read()
        want = {"StartHp": a.hp, "StartReaction": a.reaction, "MaxStamina": a.stamina, "MaxMana": a.mana,
                "StaminaRegen": a.stamina_regen, "ManaRegen": a.mana_regen, "AttackRate": a.rate, "StartGold": a.gold}
        for field, value in want.items():
            have = num(text, field)
            if have != value:
                problems.append(f"class {name}: {field} is {have} in the mudlib, {value} in the simulation")


def stage_weapon(arch, level):
    idx = 0 if level <= 2 else (1 if level == 3 else len(arch.weapons) - 1)
    return arch.weapons[min(idx, len(arch.weapons) - 1)]


def main():
    if len(sys.argv) > 1 and sys.argv[1] == "check":
        sys.exit(0 if check_mudlib() else 1)
    runs = int(sys.argv[1]) if len(sys.argv) > 1 else 1500
    random.seed(20260101)
    names = list(ARCHS)
    print("=" * 118)
    print("1. Room by room, solo, rested, no potions, at the level the room asks for (REQUIRED_LEVEL), the +5 HP trinket from the")
    print("   Mushroom Cavern on. Gear by level: the archetype's weapon progression (see ARCHS), shield where it carries one.")
    print("   Each cell: win% / HP lost in won fights.")
    print("=" * 118)
    print(f"{'Room':<19}{'Lvl':>3}  " + "".join(f"{n:>16}" for n in names))
    rooms = dict(ROOMS)
    room_win = {n: [] for n in names}
    for room, creatures in ROOMS:
        level = REQUIRED_LEVEL[room]
        extra = TRINKET_HP if level >= 4 else 0
        cells = []
        for n in names:
            a = ARCHS[n]
            win, lost, secs, acts = room_stats(n, creatures, level, stage_weapon(a, level), extra, runs)
            room_win[n].append(win)
            cells.append(f"{win * 100:>4.0f}% /{lost:>5.1f}")
        print(f"{room:<19}{level:>3}  " + "".join(f"{c:>16}" for c in cells))
    print()

    print("=" * 118)
    print("2. The Goblin Overseer, solo, from full HP, best gear the archetype can buy in the village (+5 HP trinket), potions at 35% HP.")
    print("   Kill% = the Overseer dies before the player. Survive% = the player is alive at the end of the whole fight.")
    print("=" * 118)
    print(f"{'Archetype':<12}{'Lvl':>4}{'Potions':>8}{'Kill%':>7}{'Survive%':>10}{'HP lost':>9}{'Seconds':>9}{'Actions':>9}")
    boss = {}
    for n in names:
        a = ARCHS[n]
        for level in (4, 5):
            for potions in (0, 2):
                kill, survive, lost, secs, acts = boss_stats(n, level, a.weapons[-1], potions, TRINKET_HP, runs)
                boss[(n, level, potions)] = kill
                print(f"{n:<12}{level:>4}{potions:>8}{kill * 100:>6.0f}%{survive * 100:>9.0f}%{lost:>9.1f}{secs:>9.1f}{acts:>9.1f}")
    print()

    print("=" * 118)
    print(f"3. A whole run, Tavern Cellar to the Overseer ({max(100, runs // 3)} runs per archetype). The player buys gear with loot gold, keeps")
    print("   up to 2 potions, rests between rooms and farms respawns until he has the level each room wants.")
    print("=" * 118)
    print(f"{'Archetype':<12}{'Killed Overseer':>16}{'1st try':>9}{'Farm fights':>12}{'Deaths':>8}{'Seconds':>9}   Level on reaching the Overseer")
    summary = {}
    for n in names:
        done, first_try, farm, deaths, levels, secs = full_run(n, max(100, runs // 3))
        total = sum(levels.values()) or 1
        lv = ", ".join(f"L{l} {c * 100 / total:.0f}%" for l, c in sorted(levels.items()))
        print(f"{n:<12}{done * 100:>15.0f}%{first_try * 100:>8.0f}%{farm:>12.1f}{deaths:>8.2f}{secs:>9.0f}   {lv}")
        summary[n] = (done, first_try)
    print()

    print("=" * 118)
    print("4. How far is each archetype from the average? (points of Overseer kill% for a prepared level 4 and level 5 character with")
    print("   2 potions, and of first-attempt success of the whole run). Target: within about 15 points.")
    print("=" * 118)
    for label, vals in (("Overseer kill%, L4, 2 potions", {n: boss[(n, 4, 2)] for n in names}),
                        ("Overseer kill%, L5, 2 potions", {n: boss[(n, 5, 2)] for n in names}),
                        ("whole run, first attempt", {n: summary[n][1] for n in names})):
        avg = sum(vals.values()) / len(vals)
        print(f"{label:<32}average {avg * 100:>4.0f}   " + "   ".join(f"{n} {(v - avg) * 100:+.0f}" for n, v in vals.items()))
    print()

    print("=" * 118)
    print("5. Weapons: damage per second against a level 3 creature with armor 2 (defense 2) for a level 3 warrior, small shield.")
    print("=" * 118)
    print(f"{'Weapon':<18}{'Damage':>8}{'To hit':>7}{'Reaction':>9}{'Delay s':>9}{'Avg dmg':>9}{'Hit%':>6}{'DPS':>7}")
    for name in ("fists", "iron dagger", "enchanted dagger", "training spear", "rusty sword", "novice mace", "apprentice staff",
                 "hunting bow", "fine longsword", "woodcutter axe", "overseer's axe"):
        p = make_player("warrior", 3, name, True)
        (lo, hi), to_hit, mod, rec = WEAPONS[name]
        delay = p.delay_ms(p.acts[0]) / 1000.0
        avg, hit = (lo + hi) / 2, hit_chance(p.attack(), 2, 2)
        print(f"{name:<18}{lo:>4}-{hi:<3}{to_hit:>7}{p.reaction:>9.1f}{delay:>9.1f}{avg:>9.1f}{hit * 100:>6.0f}{avg * hit / delay:>7.2f}")


if __name__ == "__main__":
    main()
