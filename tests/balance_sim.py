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
  * Attributes (docs/ATTRIBUTES.md, sections 2, 3 and 8): a player has the calling's starting attributes and places the 2
    points of every level in the calling's obvious attributes (PLACEMENT); table 2b shows a character who places none. A
    creature has 4 + level / 2 in each. Each effect counts the points above 5: weapon damage +2% a point of Might (rounded
    half away from zero, after the roll); +1 to hit for every 8 points of Grace; critical chance +0.25% a point of Grace; spell damage
    +2% a point of Insight and +1% of Spirit; spell healing +2% a point of Spirit and +1% of Insight (added to Divine
    Knowledge's 25%); Physical Resistance 2% a point of Vigor and Resolve (at most 75%) against staggers and slows.
  * Maximum HP (Vitality), Stamina (Endurance) and Mana (Focus) are class base + class per-level x level + attributes;
    Stamina and Mana come back continuously at Maximum x (6% + (Resolve + Vigor) x 0.3%) and Maximum x (1% + (Insight +
    Spirit) x 0.15%) per 10 seconds, HP out of combat at 0.3 + Vigor / 10; the player is rested to full between fights.
    Level n needs 50 n (n-1) experience (100, 300, 600, 1000 for levels 2-5); a level-up heals fully, in the middle of a
    fight too.
  * Player policies (what a sensible player does): potions at 35% HP; warrior Shield Bash on a creature that is
    preparing an action (the interrupt) or when Stamina is high; scout Precision Strike whenever it can pay; mage
    Arcane Bolt whenever it can pay; priest Minor Heal at 50% HP, Blessing at the start of a hard fight.

Usage: python3 tests/balance_sim.py [runs]      python3 tests/balance_sim.py check
"""
import heapq
import os
import random
import re
import math
import sys
from contextlib import contextmanager
from dataclasses import dataclass, replace

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
    name: str = ""         # what stat calls it


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
    vitality: tuple       # (class base, per level) -- docs/ATTRIBUTES.md section 2
    reaction: float
    rate: float
    endurance: tuple
    focus: tuple
    attrs: tuple          # starting Might, Grace, Vigor, Insight, Spirit, Resolve (section 3)
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
    Arch("warrior", (13, 8), 3.0, 1.00, (79, 2), (4, 1), (8, 5, 7, 5, 5, 6), 10, ("rusty sword", "woodcutter axe", "fine longsword"), True, armor_bonus=2, hit_armed=2),
    Arch("scout", (3, 7), 2.0, 1.00, (81, 2), (2, 1), (5, 9, 5, 6, 5, 6), 10, ("hunting bow", "fine longsword"), False, dodge=5),
    Arch("priest", (7, 7), 2.5, 0.97, (23, 1), (75, 4), (5, 5, 6, 6, 9, 5), 15, ("novice mace",), True, healing_pct=25, potions=1),
    Arch("mage", (4, 6), 2.5, 0.96, (14, 1), (71, 4), (5, 5, 5, 9, 7, 5), 15, ("apprentice staff",), False, spell_cost_pct=-20, potions=1),
    Arch("adventurer", (10, 8), 2.5, 0.98, (50, 2), (50, 2), (6, 6, 6, 6, 6, 6), 20, ("training spear", "woodcutter axe", "fine longsword"), True),
]}
TRINKET_HP = 5
ATTR = ("might", "grace", "vigor", "insight", "spirit", "resolve")


# The game before the attribute work (the simulator as of the commit before "Merge attributes"): the class numbers of
# docs/CLASS_SYSTEM.md, no attribute effects, 8 Vitality a level. Only the human-model calibration (`python3 tests/balance_sim.py
# calibrate`) switches it on; every other table is the current game. name: (Vitality at level 1, Endurance, Focus, Endurance
# and Focus per 10 s, Focus per 10 s, dodge %).
LEGACY = False
LEGACY_VALUES = {"warrior": (40, 100, 20, 10, 2, 0), "scout": (26, 100, 20, 8, 2, 8), "priest": (26, 40, 100, 4, 4, 0),
                 "mage": (24, 30, 100, 3, 8, 0), "adventurer": (30, 70, 70, 6, 5, 0)}
LEGACY_HP_PER_LEVEL = 8


def creature_attrs(level):
    """A creature's attributes when its file names none: 4 + level / 2 (whole levels). No creature file names any yet."""
    return (4 + level // 2,) * 6


def above(attrs, name):
    return max(0, attrs[ATTR.index(name)] - 5)


def stat(attrs, key):
    """Living.QueryStat for the statistics the combat rules use (no equipment bonus, no class modifier: the callings have none)."""
    if LEGACY:
        return 0
    if key == "physical_power":
        return 2 * above(attrs, "might")
    if key == "precision":
        return above(attrs, "grace") // 8
    if key == "evasion":
        return 0                     # nothing from Grace (docs/ATTRIBUTES.md section 8)
    if key == "crit":
        return 0.25 * above(attrs, "grace")
    if key == "spell_power":
        return 2 * above(attrs, "insight") + above(attrs, "spirit")
    if key == "healing_power":
        return 2 * above(attrs, "spirit") + above(attrs, "insight")
    if key == "physical_resistance":
        return min(75, 2 * (above(attrs, "vigor") + above(attrs, "resolve")))
    raise KeyError(key)


# Where a sensible player puts the 2 points of every level (docs/ATTRIBUTES.md section 8): the calling's obvious attributes,
# in turn. PLACE_POINTS False is a character who places none (table 2b).
PLACEMENT = {"warrior": ("might", "vigor"), "scout": ("grace",), "priest": ("spirit", "vigor"), "mage": ("insight",),
             "adventurer": ("might", "vigor")}
PLACE_POINTS = True


def attrs_at(arch, level):
    """The calling's starting attributes plus 2 points a level placed by PLACEMENT (when PLACE_POINTS)."""
    values = list(arch.attrs)
    if PLACE_POINTS:
        order = PLACEMENT[arch.name]
        for k in range(2 * (level - 1)):
            values[ATTR.index(order[k % len(order)])] += 1
    return tuple(values)


def resources(arch, level):
    """(max HP, max Stamina, max Mana, Stamina per 10 s, Mana per 10 s, HP per 10 s out of combat) by docs/ATTRIBUTES.md
    sections 2 and 8."""
    if LEGACY:
        hp0, end, foc, end_rate, foc_rate, _ = LEGACY_VALUES[arch.name]
        return hp0 + LEGACY_HP_PER_LEVEL * (level - 1), end, foc, end_rate, foc_rate, 1.0
    m, g, v, i, s, r = attrs_at(arch, level)
    hp = arch.vitality[0] + arch.vitality[1] * level + 2 * v
    end = arch.endurance[0] + arch.endurance[1] * level + 2 * r + v
    foc = arch.focus[0] + arch.focus[1] * level + 2 * i + s
    return hp, end, foc, end * (0.06 + (r + v) * 0.003), foc * (0.01 + (i + s) * 0.0015), 0.3 + v / 10
SHIELD_PRICE = 12
# Improvised Combat (adventurer) removes 0.5 of a weapon's reaction modifier.
REACTION_CUT = {"adventurer": 0.5}


def rnd(x):
    return int(x + 0.5 + 1e-9)


class Fighter:
    def __init__(self, level, hp, dmg, to_hit, armor, reaction, acts, kind=None, arch=None, shield=False, rate=1.0):
        self.level, self.hp, self.max_hp = level, hp, hp
        self.attrs = attrs_at(arch, level) if arch is not None else creature_attrs(level)
        self.max_stamina = self.max_mana = 0
        self.stamina_regen = self.mana_regen = 0.0
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
        return rnd(self.rate * self.level) + self.to_hit + (1 if t < self.bless_until else 0) + int(self.mod("tohit", t)) + stat(self.attrs, "precision")

    def defense(self):
        return max(0, self.level - 1) + stat(self.attrs, "evasion")

    def resists(self):
        """Physical Resistance against a stagger or a slow, rolled to a tenth of a percent."""
        chance = stat(self.attrs, "physical_resistance")
        return chance > 0 and random.randrange(1000) < round(chance * 10)

    def total_armor(self, t=0):
        return self.armor + (1 if t < self.bless_until else 0) + int(self.mod("armor", t))

    def dodge(self):
        if self.arch:
            return LEGACY_VALUES[self.arch.name][5] if LEGACY else self.arch.dodge
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
            self.stamina = min(self.max_stamina, self.stamina + self.stamina_regen * dt)
            self.mana = min(self.max_mana, self.mana + self.mana_regen * dt)
        self.last_t = t


def hit_chance(attack, armor, defense):
    hits = 0
    for roll in range(1, 21):
        if roll == 20 or (roll != 1 and roll + attack >= 10 + armor + defense):
            hits += 1
    return hits / 20


def player_max_hp(arch, level, extra=0):
    return resources(arch, level)[0] + extra


def set_level_resources(p, level):
    """A player's resource maxima and rates at a level (they grow with it)."""
    _, p.max_stamina, p.max_mana, p.stamina_regen, p.mana_regen, _ = resources(p.arch, level)
    p.attrs = attrs_at(p.arch, level)


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
    set_level_resources(p, level)
    p.stamina, p.mana = float(p.max_stamina), float(p.max_mana)
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


SUMMON = True   # False takes the Overseer's Summon away (only the recommendations of docs/BALANCE_HUMAN.md try that)


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
        if SUMMON and not e.summoned and e.hp * 2 <= e.max_hp:
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


def fight(player, enemies, potions=0, boss=False, on_kill=None, hard=None, human=None, start=None, stop_at_boss=False):
    """Fight until one side is dead. Mutates player and enemies. Returns a dict: alive, ms, actions, potions,
    boss_ms and lost_at_kill.
    The perfect player (human None) decides at every Ready Time. A HumanPolicy (see "the human player" below) types its
    commands with a delay, which arrive as events of their own, and may flee (out["fled"]). start = (the player's first
    Ready Time, [each enemy's first Ready Time]) in ms, or None for the perfect model's "the player starts at once, a creature
    waits its Reaction Time x 2 s". stop_at_boss ends the fight the moment enemies[0] dies (what the human approach asks)."""
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
    if start is None:
        start = (0, [round(e.reaction * UNIT_MS) for e in enemies])
    player.ready_at = start[0]
    push(start[0], "ready", player)
    for e, first in zip(enemies, start[1]):
        e.ready_at = first
        push(e.ready_at, "ready", e)
    t = 0
    if human is not None:
        human.begin(player)

    def stagger(e, now, seconds=2.0):
        if e.resists():
            return
        e.gen += 1
        if e.resolve_at is not None and e.resolve_at > now:
            half = (e.ready_at - e.resolve_at) / 2
            e.resolve_at = None
            e.ready_at = round(now + half)
            if e.is_player:  # half the cost comes back
                e.stamina = min(e.max_stamina, e.stamina + e.paid[0] / 2)
                e.mana = min(e.max_mana, e.mana + e.paid[1] / 2)
        else:
            e.ready_at = max(e.ready_at, now) + round(seconds * 1000)
        push(e.ready_at, "ready", e)

    out["stagger"] = stagger
    while heap and player.hp > 0 and any(e.hp > 0 for e in enemies):
        if human is not None:
            human.observe(t, player, potions, push)
        t, _, kind, ent, act, target, gen = heapq.heappop(heap)
        if kind == "cmd":              # the human's typed command arrives: it waits in the queue for the next Ready Time
            human.arrive(act)
            continue
        if kind == "flee":
            if human.flee_attempt():
                out["fled"] = True
                break
            continue
        if ent.hp <= 0 or gen != ent.gen:
            continue
        if kind == "ready":
            living = [e for e in enemies if e.hp > 0]
            if not living and not ent.is_player:
                continue
            if ent.is_player:
                if human is not None:
                    choice = human.choose(ent, t, living, potions, hard, state, push)
                else:
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
            if stop_at_boss:
                break
    out.update(alive=player.hp > 0, ms=t, potions=potions)
    return out


def do_hit(att, defn, t, to_hit_bonus=0, crit=5, factor=1.0, mult=1, bonus=0):
    """One physical attack. Returns (damage, blocked) with damage 0 on a miss or dodge. crit is the action's own chance (5 for
    a basic attack); Critical Chance from Grace is added here, as Living.QueryCritFor does."""
    if random.randint(1, 100) <= defn.dodge():
        return 0, False
    roll = random.randint(1, 20)
    need = 10 + defn.total_armor(t) + defn.defense()
    if not (roll == 20 or (roll != 1 and roll + att.attack(t) + to_hit_bonus >= need)):
        return 0, False
    dmg = max(1, rnd(max(1, random.randint(*att.dmg) + bonus) * (1 + stat(att.attrs, "physical_power") / 100.0))) * mult
    if factor != 1.0:
        dmg = max(1, rnd(dmg * factor))
    if random.randrange(1000) < round(min(100, crit + stat(att.attrs, "crit")) * 10):
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
        ent.hp = min(ent.max_hp, ent.hp + rnd(HEAL_AMOUNT * (100 + ent.arch.healing_pct + stat(ent.attrs, "healing_power")) / 100.0))
        return
    if act.id == "bless":
        ent.bless_until = t + BLESS_MS
        return
    if act.id == "bolt":
        if target.dodge() and random.randint(1, 100) <= target.dodge():
            return
        dmg = max(1, rnd(random.randint(*BOLT_DMG) * (1 + stat(ent.attrs, "spell_power") / 100.0)))
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


# ---------------------------------------------------------------- the human player (docs/BALANCE_HUMAN.md)
#
# Everything above plays perfectly: it acts the instant it is able, uses its best skill every time, drinks at exactly 35% and
# starts every fight at full Vitality. A person does none of that. HumanModel names every way in which the human differs, each as
# one documented number; HumanPolicy plays a fight with them (inside fight()); run_approach() plays the whole way from the
# cellar to the Overseer with the resources carried from fight to fight. Nothing here touches the random stream of the perfect
# model: a policy draws from its own random.Random.

FLEE_SUCCESS = 0.75     # the game: `flee` fails one time in four (docs/GAME_SYSTEMS.md section 11, CmdFlee)
STORAGE_POTION = False  # the Storage Chamber's potion (restocked by reset) is not counted in the potions a run starts with


@dataclass(frozen=True)
class HumanModel:
    """One version of the human player. Times in seconds. Every field is a number a person could measure."""
    name: str
    # Typing. A command is typed `delay` seconds after the thing that prompted it (the start of the previous action, the hit that
    # lowered Vitality, the arrival in the room), then waits in the queue for the next Ready Time (docs/COMBAT_TIMING.md).
    delay_median: float = 1.5      # the median of the typing delay: see the text, read, decide, type
    delay_sigma: float = 0.6       # spread of the delay (log-normal: 0.6 gives 1.5 s median, 3.7 s at the 90th percentile)
    p_distract: float = 0.05       # chance that a command is also held up by a distraction (a chat, a second look at the map) ...
    distract_s: tuple = (6.0, 20.0)  # ... of this many extra seconds (uniform)
    # Skills.
    p_skip_skill: float = 0.15     # chance, in each action slot, that the player types no skill and lets the auto-attack run
    # Potions. Drawn afresh for every fight.
    drink_lo: float = 0.25         # the player reaches for a potion when Vitality falls to this share of the maximum ...
    drink_hi: float = 0.35         # ... somewhere in this range (perfect play: always 0.35)
    p_no_drink: float = 0.06       # chance that in a fight the player never types the potion nor the flee (frozen, fascinated, away)
    # Fleeing. When no potion is left and Vitality is at or below this share (drawn for each fight in the range), the player
    # types `flee`. It succeeds 3 times in 4 (FLEE_SUCCESS) and a failure is retried after another typing delay.
    flee_lo: float = 0.25
    flee_hi: float = 0.35
    flee: bool = True
    # Resources between fights.
    carry_over: bool = True        # Vitality, Endurance and Focus carry over, with only the rest the room sequence allows
    pause_s: float = 3.0           # seconds spent reading the room and looting between the last blow and the next door
    rest_to: float = 0.0           # before the next door the player waits (out-of-combat regeneration only) until Vitality is at least
                                   # this share of the maximum; 0 = never waits, the strict "only the rest the room sequence allows"
    topup_below: float = 0.0       # before the next door, drink potions (out of combat, one at a time) while Vitality is below this
                                   # share; 0 = never. Counted as used in the room the player is about to enter


# Three versions of the human, calibrated in docs/BALANCE_HUMAN.md (low = a sharp player, high = a clumsy one).
HUMAN_LOW = HumanModel("human-low", delay_median=1.0, delay_sigma=0.5, p_distract=0.03, p_skip_skill=0.10, drink_lo=0.28,
                       drink_hi=0.35, p_no_drink=0.03, rest_to=0.7)
HUMAN_MID = HumanModel("human-mid", rest_to=0.5)
HUMAN_HIGH = HumanModel("human-high", delay_median=2.0, delay_sigma=0.7, p_distract=0.08, p_skip_skill=0.25, drink_lo=0.20,
                        drink_hi=0.32, p_no_drink=0.10, rest_to=0.0)
# The perfect player's habits, run through the human engine: the zero point of the ablation table.
HUMAN_ZERO = HumanModel("zero-delay", delay_median=0.0, delay_sigma=0.0, p_distract=0.0, p_skip_skill=0.0, drink_lo=0.35,
                        drink_hi=0.35, p_no_drink=0.0, flee=False, carry_over=False, pause_s=0.0)
HUMAN_MODELS = {"low": HUMAN_LOW, "mid": HUMAN_MID, "high": HUMAN_HIGH}


class HumanPolicy:
    """A human at the keyboard for one fight. fight() calls begin, observe (before every event), arrive (a typed command reaches
    the queue), choose (the player's Ready Time) and flee_attempt."""

    def __init__(self, model, rng):
        self.m, self.rng = model, rng

    def delay(self):
        """One typing delay in ms."""
        m = self.m
        if m.delay_median <= 0:
            return 0
        d = m.delay_median * math.exp(m.delay_sigma * self.rng.gauss(0, 1))
        if self.rng.random() < m.p_distract:
            d += self.rng.uniform(*m.distract_s)
        return round(min(d, 60.0) * 1000)

    def begin(self, player):
        m, rng = self.m, self.rng
        self.queued = "skill" if rng.random() >= m.p_skip_skill else None   # the first command may already be a skill
        self.thr = rng.uniform(m.drink_lo, m.drink_hi)
        self.flee_thr = rng.uniform(m.flee_lo, m.flee_hi)
        self.frozen = rng.random() < m.p_no_drink
        self.skill_pending = self.potion_pending = self.flee_pending = False
        self.token = 0

    def observe(self, t, p, potions, push):
        """The player reads the screen after every event: Vitality low and a potion left, or low and none left."""
        if p.hp <= 0 or self.frozen:
            return
        frac = p.hp / p.max_hp
        if potions > 0:
            if frac <= self.thr and not self.potion_pending:
                self.potion_pending = True
                self.token += 1                      # a skill about to be typed is dropped: the potion comes first
                push(t + self.delay(), "cmd", p, "potion")
        elif self.m.flee and frac <= self.flee_thr and not self.flee_pending:
            self.flee_pending = True
            push(t + self.delay(), "flee", p)

    def arrive(self, cmd):
        if cmd == "potion":
            self.queued = "potion"
        else:
            self.skill_pending = False
            if cmd[1] == self.token and self.queued != "potion":
                self.queued = "skill"

    def flee_attempt(self):
        self.flee_pending = False
        return self.rng.random() < FLEE_SUCCESS

    def skill(self, p, t, living, hard, state):
        """The calling's skill, as typed; the game refuses it (and the slot goes to the auto-attack) if it cannot be paid."""
        a = p.arch.name
        if a == "priest":
            if p.hp <= p.max_hp * 0.5 and p.mana >= HEAL_COST:
                return "heal"
            if hard and t >= p.bless_until and p.mana >= BLESS_COST + HEAL_COST and not state.get("blessed"):
                state["blessed"] = True
                return "bless"
        elif a == "warrior" and p.shield and p.stamina >= BASH_COST:
            return "bash"
        elif a == "scout" and p.stamina >= STRIKE_COST:
            return "strike"
        elif a == "mage" and p.mana >= spell_cost(p, BOLT_COST):
            return "bolt"
        return "attack"

    def choose(self, p, t, living, potions, hard, state, push):
        p.regen(t)
        cmd, self.queued = self.queued, None
        choice = "attack"
        if cmd == "potion":
            self.potion_pending = False
            if potions > 0:
                choice = "potion"
        elif cmd == "skill":
            choice = self.skill(p, t, living, hard, state)
        if not (self.potion_pending or self.skill_pending or self.queued) and self.rng.random() >= self.m.p_skip_skill:
            self.skill_pending = True
            push(t + self.delay(), "cmd", p, ("skill", self.token))
        return choice


# The cellars from the village to the Overseer: (room, creatures, aggressive). Smuggler Hall is empty; the Guard Post is a side branch
# (the keyholder, for the Hidden Cache) and is not on the way. Every creature on the way is Aggressive in the mudlib.
APPROACH = [("Tavern Cellar", ["Large Rat"] * 3), ("Collapsed Passage", ["Giant Rat"] * 2),
            ("Storage Chamber", ["Goblin Scavenger"] * 2), ("Smuggler Hall", []), ("Flooded Tunnel", ["Giant Cave Toad"]),
            ("Mushroom Cavern", ["Spore Bat"] * 3), ("Goblin Barracks", ["Goblin Warrior"] * 3),
            ("Overseer Hall", ["Goblin Elite Guard"] * 2), ("Overseer Chamber", ["Goblin Overseer"])]
ROOM_NAMES = [r for r, c in APPROACH if c]


def gear_for(arch_name, gear):
    """(weapon, shield, extra Vitality, armour from worn pieces). gear: "starting" (the calling's own kit: no armour, no trinket),
    "best" (the best weapon and the shield the Hollow sells, with the trinket from the Hidden Cache; no body armour is sold), or a
    tuple (weapon, shield, trinket, armour) as given, which is how the table of recommendations tries armour."""
    a = ARCHS[arch_name]
    if gear == "starting":
        return a.weapons[0], a.shield and arch_name == "warrior", 0, 0
    if gear == "best":
        return a.weapons[-1], a.shield, TRINKET_HP, 0
    return gear


def rest(p, seconds):
    """What a character regains standing about (no safe room): the out-of-combat rates of resources()."""
    _, _, _, st_rate, ma_rate, hp_rate = resources(p.arch, p.level)
    p.hp = min(p.max_hp, p.hp + hp_rate * seconds / 10.0)
    p.stamina = min(p.max_stamina, p.stamina + st_rate * seconds / 10.0)
    p.mana = min(p.max_mana, p.mana + ma_rate * seconds / 10.0)


def start_times(p, enemies, delay_ms, aggro):
    """The first Ready Time of the player and of each creature, in ms from the player's arrival in the room.
    aggro (the current game): the creatures start the fight at once and wait their Reaction Time x 2 s; the player, drawn in,
    waits his own, and a command typed after that is later still. Old game: the first creature to notice calls out after its
    Reaction Time x 2 s; a player who has typed `kill` before that starts the fight (and acts at once), one who has not is
    ambushed (the creature acts at once, the player waits his own Reaction Time)."""
    react = [round(e.reaction * UNIT_MS) for e in enemies]
    own = round(p.reaction * UNIT_MS)
    if aggro:
        return max(delay_ms, own), react
    call = min(react)
    if delay_ms < call:
        return delay_ms, [delay_ms + r for r in react]
    first = react.index(call)
    return max(delay_ms, call + own), [call if i == first else call + r for i, r in enumerate(react)]


def run_approach(arch_name, level, potions, model, rng, gear="starting", aggro=True, pack_at="hall"):
    """One run from the Tavern Cellar to the Overseer. model None is the perfect player: full Vitality, Endurance and Focus for
    every fight, no typing, the existing rules. Returns a dict: outcome ("kill", "died", "fled"), room of the end, potions used by
    room, potions left, Vitality share at the Overseer's door.
    pack_at: "hall" = the player enters the Overseer Hall with `potions` in the pack (the rooms before it use a spare supply, which
    is reported as used "on the way" but does not count against the pack: a player restocks at the merchant); "entrance" = the
    pack is `potions` from the first room on and the rooms before the Hall draw on it."""
    arch = ARCHS[arch_name]
    weapon, shield, extra_hp, armor = gear_for(arch_name, gear)
    p = make_player(arch, level, weapon, shield, extra_hp, armor_extra=armor)
    left = potions if pack_at == "entrance" else 99
    used = {}
    res = {"outcome": "kill", "room": "Overseer Chamber", "used": used, "door": None}
    moves, looted = 0, 0
    for room, creatures in APPROACH:
        moves += 1
        if not creatures:
            continue
        boss = room == "Overseer Chamber"
        if room == "Overseer Hall" and pack_at == "hall":
            left = potions
        if model is None or not model.carry_over:
            p.hp, p.stamina, p.mana = p.max_hp, float(p.max_stamina), float(p.max_mana)
        elif room != ROOM_NAMES[0]:
            rest(p, moves * ROOM_SECONDS + looted * LOOT_SECONDS + model.pause_s)
            if p.hp < model.rest_to * p.max_hp:
                rest(p, (model.rest_to * p.max_hp - p.hp) / (resources(arch, level)[5] / 10.0) + 0.01)
        p.bless_until = p.cry_until = 0
        p.effects = {}
        topped = 0
        if model is not None and model.carry_over and model.topup_below:
            while left > 0 and p.hp < model.topup_below * p.max_hp:
                p.hp = min(p.max_hp, p.hp + POTION_HEAL)
                left -= 1
                topped += 1
        if boss:
            res["door"] = p.hp / p.max_hp
        enemies = [make_monster(c) for c in creatures]
        policy = start = None
        if model is not None:
            policy = HumanPolicy(model, rng)
            start = start_times(p, enemies, policy.delay(), aggro)
        r = fight(p, enemies, left, boss=boss, human=policy, start=start, stop_at_boss=True)
        p.regen(r["ms"])
        used[room] = r.get("potions_used", 0) + topped
        left = r["potions"]
        moves, looted = 0, sum(1 for c in creatures if KINDS[c].gold[1])
        if boss and r["lost_at_kill"] is not None:
            break
        if r.get("fled") or not r["alive"] or boss:
            res.update(outcome="fled" if r.get("fled") else "died", room=room)
            break
    res["left"] = left
    return res


def approach_stats(arch_name, level, potions, model, runs=1000, gear="starting", aggro=True, seed=1, pack_at="hall"):
    """Many runs of run_approach. Returns kill share, outcomes by room, potions used before the Hall / in the Hall / at the
    Overseer, the share that reached the Overseer, the kill share of those that did, the share that used three or more potions
    on the guards, and the mean Vitality share at the Overseer's door."""
    rng = random.Random(seed)
    saved = random.getstate()
    random.seed(seed * 7919 + level)
    kills = reached = hall3 = 0
    ends = {}
    before = hall = chamber = 0.0
    door = 0.0
    hall_hist = {}
    for _ in range(runs):
        r = run_approach(arch_name, level, potions, model, rng, gear, aggro, pack_at)
        u = r["used"]
        before += sum(v for k, v in u.items() if k not in ("Overseer Hall", "Overseer Chamber"))
        hall += u.get("Overseer Hall", 0)
        chamber += u.get("Overseer Chamber", 0) + 0
        hall_hist[u.get("Overseer Hall", 0)] = hall_hist.get(u.get("Overseer Hall", 0), 0) + 1
        hall3 += u.get("Overseer Hall", 0) >= 3
        if r["door"] is not None:
            reached += 1
            door += r["door"]
        if r["outcome"] == "kill":
            kills += 1
        else:
            key = (r["outcome"], r["room"])
            ends[key] = ends.get(key, 0) + 1
    random.setstate(saved)
    return dict(kill=kills / runs, reached=reached / runs, kill_if_reached=kills / reached if reached else 0.0, before=before / runs,
                hall=hall / runs, chamber=chamber / runs, hall3=hall3 / runs, hall_hist=hall_hist, ends={k: v / runs for k, v in ends.items()},
                door=door / reached if reached else 0.0, runs=runs)


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
                set_level_resources(p, level)

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
    return Act(id, prep, rec, mult, mod, kind, fx, factor, hit, name)


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
    CKind("stray_wolf", "Stray Wolf", 5, 60, (6, 10), 1, 1, 140, 2.0, (A("bite", "Bite", 0, 0.5),), P(cycle=("bite",)),
          band=(5, 7)),
    CKind("forest_spider", "Forest Spider", 5, 57, (4, 8), 2, 0, 135, 1.5,
          (A("bite", "Bite", 0, 0.5), A("venom", "Slowing Bite", 0, 0.5, fx="slow:1.25:8")),
          P(cycle=("bite", "bite", "bite", "venom")), band=(5, 7), aggressive=True),
    CKind("bandit_lookout", "Bandit Lookout", 6, 48, (10, 15), 2, 2, 145, 3.0, (A("slash", "Slash", 0, 1.0),),
          P(cycle=("slash",)), gold=(3, 8), loot=(("iron_dagger", 20), ("trail_ration", 25)), band=(6, 7), aggressive=True),
    CKind("forest_wolf", "Forest Wolf", 6, 73, (3, 6), 2, 1, 185, 2.0,
          (A("bite", "Bite", 0, 0.5), A("savage", "Savage Bite", 0, 0.5, mult=2)),
          P(cycle=("bite", "bite", "bite", "savage")), band=(5, 7)),
    CKind("wild_boar", "Wild Boar", 6, 40, (11, 19), 1, 2, 145, 3.5,
          (A("gore", "Gore", 0, 1.0), A("charge", "Charge", 1.0, 1.5, mult=2)),
          P(first=("charge",), cycle=("gore",)), band=(6, 8)),
    CKind("goblin_scout", "Goblin Scout", 7, 80, (5, 8), 3, 2, 270, 2.0, (A("stab", "Quick Stab", 0, 0.5),),
          P(cycle=("stab",)), gold=(3, 9), loot=(("iron_dagger", 20), ("healing_potion", 4)), band=(7, 8), aggressive=True),
    CKind("goblin_raider", "Goblin Raider", 7, 53, (7, 11), 2, 3, 170, 3.0,
          (A("hack", "Cleaver Hack", 0, 1.0), A("whoop", "War Whoop", 0.5, 0.5, kind="buff", fx="damage=3,speed=0.85:25")),
          P(cycle=("hack",), at=((50, "whoop"),)), gold=(4, 10), loot=(("rusty_sword", 15), ("training_spear", 15), ("healing_potion", 4)),
          band=(6, 9), aggressive=True),
    CKind("boar_brute", "Boar Brute", 7, 45, (8, 14), 2, 3, 180, 4.5,
          (A("gore", "Gore", 0, 1.5), A("trample", "Trample", 1.0, 2.0, mult=2)),
          P(cycle=("gore", "gore", "trample")), band=(6, 8)),
    CKind("tunnel_rat", "Tunnel Rat", 7, 40, (3, 6), 3, 1, 165, 1.5, (A("bite", "Bite", 0, 0.5),), P(cycle=("bite",)),
          band=(7, 8), aggressive=True),
    CKind("woodland_spider", "Woodland Spider", 8, 91, (3, 5), 3, 1, 420, 1.5,
          (A("bite", "Bite", 0, 0.5), A("venom", "Slowing Bite", 0, 0.5, fx="slow:1.25:8")),
          P(cycle=("bite", "bite", "bite", "venom")), band=(7, 9), aggressive=True),
    CKind("rogue_bandit", "Rogue Bandit", 8, 60, (10, 16), 3, 3, 300, 3.0,
          (A("slash", "Slash", 0, 1.0), A("pommel", "Pommel Strike", 0, 1.5, fx="stagger:2", factor=0.5)),
          P(cycle=("slash", "slash", "pommel")), gold=(5, 12), loot=(("rusty_sword", 15), ("healing_potion", 5), ("trail_ration", 20)),
          band=(8, 9), aggressive=True),
    CKind("young_black_bear", "Young Black Bear", 8, 57, (8, 15), 2, 2, 300, 4.0,
          (A("swipe", "Swipe", 0, 1.5), A("maul", "Maul", 1.0, 2.0, mult=2)),
          P(cycle=("swipe", "swipe", "maul")), band=(7, 8)),
    CKind("black_bear", "Black Bear", 8, 54, (8, 13), 3, 3, 280, 4.5,
          (A("swipe", "Swipe", 0, 1.5), A("maul", "Maul", 1.0, 2.0, mult=2), A("roar", "Bellowing Roar", 0.5, 0.5, kind="buff", fx="damage=3,speed=0.85:30")),
          P(cycle=("swipe", "swipe", "maul"), at=((50, "roar"),)), band=(7, 9)),
    CKind("heartwood_goblin", "Goblin Warrior", 8, 56, (5, 8), 3, 4, 230, 3.0,
          (A("slash", "Slash", 0, 1.0), A("heavy", "Heavy Swing", 0.5, 1.5, mult=2)),
          P(cycle=("slash", "slash", "slash", "heavy")), gold=(5, 12), loot=(("training_spear", 15), ("iron_dagger", 15), ("healing_potion", 4)),
          band=(7, 9), aggressive=True),
    CKind("highway_bandit", "Highway Bandit", 8, 59, (6, 9), 3, 3, 195, 3.0,
          (A("slash", "Slash", 0, 1.0), A("pommel", "Pommel Strike", 0, 1.5, fx="stagger:2", factor=0.5)),
          P(cycle=("slash", "slash", "pommel")), gold=(5, 13), loot=(("rusty_sword", 15), ("healing_potion", 6), ("trail_ration", 20)),
          band=(6, 9), aggressive=True),
    CKind("thorn_woodling", "Thorn Woodling", 9, 53, (10, 16), 2, 4, 410, 4.0,
          (A("lash", "Branch Lash", 0, 1.5), A("thorns", "Thorn Barrage", 0.5, 1.5, fx="weaken:armor=-2:12")),
          P(cycle=("lash", "lash", "thorns")), band=(8, 10)),
    CKind("forest_stalker", "Forest Stalker", 9, 70, (4, 7), 4, 2, 550, 2.0,
          (A("claw", "Claw", 0, 1.0), A("ambush", "Ambush Strike", 0.5, 1.0, mult=2, hit=4)),
          P(first=("ambush",), cycle=("claw",)), band=(8, 10), dodge=15, aggressive=True),
    CKind("ancient_wolf", "Ancient Wolf", 9, 69, (8, 13), 3, 3, 550, 3.0,
          (A("bite", "Rending Bite", 0, 1.0), A("howl", "Howl", 0.5, 0.5, kind="buff", fx="damage=2,speed=0.75:25")),
          P(cycle=("bite",), at=((60, "howl"),)), band=(9, 10), aggressive=True),
    CKind("rogue_goblin_veteran", "Rogue Goblin Veteran", 9, 53, (7, 10), 4, 5, 280, 3.5,
          (A("slash", "Slash", 0, 1.0), A("daze", "Dazing Blow", 0, 1.5, fx="weaken:tohit=-2,damage=-2:12", factor=0.75)),
          P(cycle=("slash", "slash", "daze")), gold=(6, 16), loot=(("healing_potion", 7), ("woodcutter_axe", 10), ("trail_ration", 20)),
          band=(8, 10), aggressive=True),
    CKind("restless_spirit", "Restless Spirit", 9, 64, (8, 13), 3, 0, 520, 2.5,
          (A("touch", "Grave Touch", 0, 1.0), A("chill", "Chill of the Grave", 0, 1.0, fx="slow:1.2:8")),
          P(cycle=("touch", "touch", "touch", "chill")), band=(9, 10), dodge=15, aggressive=True),
    CKind("ancient_skeleton", "Ancient Skeleton", 9, 54, (8, 13), 3, 4, 410, 3.5,
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
        if kind == "slow" and not target.resists():
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
# Adventurer.Tick: 0.3 + Vigor / 10 HP per 10 s out of combat (three times that in a safe room); see resources().
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
    """HP regained per second out of combat (Survival Training: x1.5 for the adventurer)."""
    return resources(ARCHS[arch], 1)[5] / 10.0 * (1.5 if arch == "adventurer" else 1.0)


def regen_hp_at(arch, level):
    return resources(ARCHS[arch], level)[5] / 10.0 * (1.5 if arch == "adventurer" else 1.0)


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
        t_place = max(0.0, c["dtot"] * (1 - f)) / regen_hp_at(arch, level) + potions * POTION_SECONDS
        t_haven = 2 * HAVEN_ROOMS * ROOM_SECONDS + max(0.0, c["dtot"] * (1 - f)) / (3 * regen_hp_at(arch, level)) + potions * POTION_SECONDS
        h = HAVEN_SHARE[min(10, max(5, level))] if haven else 0.0
        t_hp = (1 - h) * t_place + h * min(t_place, t_haven)
        wait = t_hp + walk
        _, mst, mma, rst, rma, _ = resources(a, level)
        start = (min(mst, c["end_s"] + rst * wait * (1 + h) / 10.0), min(mma, c["end_m"] + rma * wait * (1 + h) / 10.0))
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
    check_corridor(problems)
    for p in problems:
        print("MISMATCH", p)
    print("simulation numbers match the mudlib" if not problems else f"{len(problems)} mismatch(es)")
    return not problems



def check_corridor(problems):
    """Compare the 31 corridor creatures in CORRIDOR with their files in domains/greenvale/corridor/mob."""
    root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "mudlib", "domains", "greenvale", "corridor", "mob")

    def num(text, field):
        m = re.search(rf"\b{field} = (-?[\d.]+);", text)
        return float(m.group(1)) if m else 0.0

    def arr(text, name):
        m = re.search(r"\[\] " + name + r" = (?:new \w+\[0\]|\{([^}]*)\});", text)
        if not m or m.group(1) is None:
            return []
        return [x.strip().strip('"') for x in m.group(1).split(",") if x.strip()]

    for k in CORRIDOR:
        path = os.path.join(root, k.id + ".cs")
        if not os.path.exists(path):
            problems.append(f"{k.id}: file missing")
            continue
        text = open(path, encoding="utf-8").read()
        want = {"Level": k.level, "MaxHp": k.hp, "DamageMin": k.dmg[0], "DamageMax": k.dmg[1], "ToHit": k.to_hit,
                "Armor": k.armor, "XpValue": k.xp, "BaseReaction": k.reaction, "Dodge": k.dodge,
                "GoldMin": k.gold[0], "GoldMax": k.gold[1]}
        for field, value in want.items():
            if num(text, field) != value:
                problems.append(f"{k.id}: {field} is {num(text, field)} in the file, {value} in the simulation")
        if ("Aggressive = true;" in text) != k.aggressive:
            problems.append(f"{k.id}: Aggressive differs")
        got = {}
        for m in re.finditer(r'AddCombatAction\("(\w+)", "([^"]+)", preparation: ([\d.]+), recovery: ([\d.]+)([^;]*?)\);', text):
            rest = m.group(5)
            mult = re.search(r"damageMultiplier: (\d+)", rest)
            kind = re.search(r'kind: "(\w+)"', rest)
            got[m.group(1)] = [m.group(2), float(m.group(3)), float(m.group(4)), int(mult.group(1)) if mult else 1,
                               kind.group(1) if kind else "attack", 1.0, 0]
        for m in re.finditer(r'FindAction\("(\w+)"\)!\.(DamageFactor|ToHitBonus) = (-?[\d.]+);', text):
            if m.group(1) in got:
                got[m.group(1)][5 if m.group(2) == "DamageFactor" else 6] = float(m.group(3))
        have = {a.id: [a.name, a.prep, a.rec, a.mult, a.kind, a.factor, a.hit] for a in k.acts}
        if got != have:
            problems.append(f"{k.id}: actions {got} in the file, {have} in the simulation")
        opening, cycle, trig = arr(text, "opening"), arr(text, "cycle"), arr(text, "triggerHp")
        tact = arr(text, "triggerAction")
        first, cyc, at = k.pattern
        if not cycle and not opening and len(k.acts) == 1:
            cycle = list(cyc)   # a creature with one attack has no pattern arrays
        if opening != list(first) or cycle != list(cyc) or [(int(a), b) for a, b in zip(trig, tact)] != [(a, b) for a, b in at]:
            problems.append(f"{k.id}: pattern {opening}/{cycle}/{list(zip(trig, tact))} in the file, {k.pattern} in the simulation")
        # the on-hit effects of the special attacks
        fa, fk, fv, fs, fm = (arr(text, n) for n in ("fxAction", "fxKind", "fxValue", "fxSeconds", "fxMods"))
        fgot = sorted((a, kind, float(v), int(sec), mods) for a, kind, v, sec, mods in zip(fa, fk, fv, fs, fm))
        fwant = []
        for a in k.acts:
            if a.kind != "attack" or not a.fx:
                continue
            parts = a.fx.split(":")
            if parts[0] == "slow":
                fwant.append((a.id, "slow", float(parts[1]), int(parts[2]), ""))
            elif parts[0] == "stagger":
                fwant.append((a.id, "stagger", float(parts[1]), 0, ""))
            else:
                fwant.append((a.id, "weaken", 0.0, int(parts[2]), parts[1].replace(",", ";")))
        if fgot != sorted(fwant):
            problems.append(f"{k.id}: on-hit effects {fgot} in the file, {sorted(fwant)} in the simulation")
        lgot = sorted((i, int(c)) for i, c in re.findall(r'AddLoot\("/domains/\w+/obj/(\w+)", (\d+)\)', text))
        if lgot != sorted((i, c) for i, c in k.loot):
            problems.append(f"{k.id}: loot {lgot} in the file, {sorted(k.loot)} in the simulation")

def corridor_table(runs=400, only=()):
    """Win rate (percent) of each calling against each corridor creature at the low, middle and high level of its band, with the
    gap of the furthest calling from the five-calling average. Bosses are fought with CORRIDOR_POTIONS potions."""
    random.seed(11)
    print(f"{'creature':22}{'L':>3} {'pl':>3}  " + "".join(f"{a[:3]:>5}" for a in ARCHS) + f" {'avg':>5} {'spread':>7} {'sec':>5} {'hp lost':>8}")
    worst = 0.0
    for k in CORRIDOR:
        if only and k.id not in only:
            continue
        lo, hi = k.band
        for lv in sorted({lo, hi, (lo + hi) // 2}):
            ws, ts, ls = [], [], []
            for c in ARCHS:
                r = cfight(c, lv, k.id, CORRIDOR_POTIONS if k.boss else 0, runs)
                ws.append(r["win"] * 100)
                ts.append(r["sec"])
                ls.append(r["lost"])
            avg = sum(ws) / len(ws)
            spread = max(abs(w - avg) for w in ws)
            if not k.boss:
                worst = max(worst, spread)
            print(f"{k.id:22}{k.level:>3} {lv:>3}  " + "".join(f"{w:5.0f}" for w in ws)
                  + f" {avg:5.0f} {spread:7.0f} {sum(ts) / 5:5.0f} {sum(ls) / 5:8.0f}")
    print(f"largest gap to the average on an ordinary creature: {worst:.0f} points")

PACE_HOURS = {5: 2.0, 6: 2.5, 7: 3.0, 8: 3.5, 9: 4.0}   # the target hours to leave level L, for a perfect-knowledge player
TAIL_P = 3.0                                              # exponent of the proposed continuation beyond level 10


def xp_to_reach(n, table, p=TAIL_P):
    """The proposed experience needed to reach level n (modifier 1.0): the old formula to 5, the table 6..10, a power law after."""
    if n <= 5:
        return 0 if n <= 1 else 50 * n * (n - 1)
    if n <= 10:
        return table[n]
    return int(round(table[10] * (n / 10.0) ** p))


def pace_report(runs=250):
    """Experience per hour by calling and level, the proposed experience table and what it implies. About four minutes."""
    random.seed(31)
    callings = list(ARCHS)
    variants = (("perfect (own gold buys potions, rests in a haven)", {}), ("no haven", {"haven": False}),
                ("no potions", {"funded": False}), ("neither", {"funded": False, "haven": False}))
    res = {}
    for name, kw in variants:
        print(f"experience per hour, {name}")
        print("level " + "".join(f"{c[:6]:>9}" for c in callings) + "   harmonic mean")
        for lv in range(5, 10):
            row = [pace_mix(c, lv, runs, **kw)["xph"] for c in callings]
            res[(name, lv)] = row
            print(f"{lv:>5} " + "".join(f"{x:9.0f}" for x in row) + f"{len(row) / sum(1 / x for x in row):12.0f}")
    perfect = variants[0][0]
    hm = {lv: 5 / sum(1 / x for x in res[(perfect, lv)]) for lv in range(5, 10)}
    table = {5: 1000}
    for lv in range(5, 10):
        table[lv + 1] = int(round((table[lv] + PACE_HOURS[lv] * hm[lv]) / 100.0) * 100)
    print("\nproposed experience to reach level:", {k: v for k, v in table.items() if k >= 6})
    print("hours per level and calling at the perfect pace (and cumulative from level 5):")
    tot = [0.0] * 5
    for lv in range(5, 10):
        hrs = [(table[lv + 1] - table[lv]) / x for x in res[(perfect, lv)]]
        tot = [a + b for a, b in zip(tot, hrs)]
        print(f"  {lv}->{lv + 1} " + "".join(f"{h:7.2f}" for h in hrs) + f"   mean {sum(hrs) / 5:5.2f}   cumulative " + "".join(f"{t:7.2f}" for t in tot))
    # the experience per hour beyond 9: a power law fitted to the harmonic means
    q = math.log(hm[9] / hm[5]) / math.log(9 / 5)
    print(f"\nexperience per hour fitted: {hm[9]:.0f} x (L/9)^{q:.2f}; continuation exponent p={TAIL_P}")
    for p in (2.5, 3.0, TAIL_P):
        out = []
        hours = 15.0
        for n in range(10, 100):
            hours += (xp_to_reach(n + 1, table, p) - xp_to_reach(n, table, p)) / (hm[9] * ((n / 9.0) ** q))
            if n + 1 in (11, 20, 50, 100):
                out.append(f"level {n + 1}: T={xp_to_reach(n + 1, table, p):,} xp, {hours:,.0f} h")
        print(f"  p={p}: " + "; ".join(out))


def check_classes(read_root, problems, num):
    root = os.path.join(read_root, "classes")
    for name, a in ARCHS.items():
        with open(os.path.join(root, name + ".cs"), encoding="utf-8") as f:
            text = f.read()
        want = {"VitalityBase": a.vitality[0], "VitalityPerLevel": a.vitality[1], "EnduranceBase": a.endurance[0],
                "EndurancePerLevel": a.endurance[1], "FocusBase": a.focus[0], "FocusPerLevel": a.focus[1],
                "StartReaction": a.reaction, "AttackRate": a.rate, "StartGold": a.gold}
        for attr, value in zip(ATTR, a.attrs):
            m = re.search(rf'\["{attr}"\] = (\d+)', text)
            if m is None or int(m.group(1)) != value:
                problems.append(f"class {name}: start {attr} is {m.group(1) if m else None} in the mudlib, {value} in the simulation")
        for field, value in want.items():
            have = num(text, field)
            if have != value:
                problems.append(f"class {name}: {field} is {have} in the mudlib, {value} in the simulation")


# ---- the human tables (python3 tests/balance_sim.py calibrate | human | recommend [runs])


@contextmanager
def tweaked(kinds=None, summon=True, hall=None, potion_heal=None, archs=None):
    """Temporarily change creatures (name -> fields of Kind, e.g. {"Goblin Minion": {"hp": 10}}), the Overseer's Summon, the creatures
    of the Hall, the potion's strength and the callings (name -> fields of Arch, e.g. {"mage": {"vitality": (4, 7)}}), for the
    recommendations of docs/BALANCE_HUMAN.md. Nothing here is applied to the game."""
    global SUMMON, POTION_HEAL
    saved, saved_summon, saved_approach, saved_heal, saved_archs = dict(KINDS), SUMMON, list(APPROACH), POTION_HEAL, dict(ARCHS)
    try:
        for name, fields in (kinds or {}).items():
            KINDS[name] = replace(KINDS[name], **fields)
        SUMMON = summon
        if hall is not None:   # the creatures of the Overseer Hall
            APPROACH[[r for r, c in APPROACH].index("Overseer Hall")] = ("Overseer Hall", list(hall))
        if potion_heal is not None:
            POTION_HEAL = potion_heal
        for name, fields in (archs or {}).items():
            ARCHS[name] = replace(ARCHS[name], **fields)
        yield
    finally:
        KINDS.clear()
        KINDS.update(saved)
        SUMMON = saved_summon
        APPROACH[:] = saved_approach
        POTION_HEAL = saved_heal
        ARCHS.clear()
        ARCHS.update(saved_archs)


def pct(x):
    return f"{x * 100:3.0f}"


def calibrate_report(runs=3000):
    """The owner's evening in the old game: a level 5 Scout, Hunting Bow, no armour, 4 potions at the Hall. Prints the perfect model,
    the engine with every human habit switched off, the three humans, the ablation and the sensitivity of the fit to resting."""
    global LEGACY
    LEGACY = True
    try:
        print("=" * 118)
        print("CALIBRATION: the old game (before attributes), level 5 Scout, Hunting Bow, no armour, no trinket, 4 potions at the Overseer Hall.")
        print(f"Target (the owner's evening): about 3 potions spent on the two Elite Guards, then the Overseer is lost or fled more often than not.")
        print("=" * 118)
        random.seed(5)
        k = boss_stats("scout", 5, "hunting bow", 4, 0, runs)
        print(f"Perfect model, the Overseer alone, full Vitality, 4 potions (the old claim): kill {k[0] * 100:.0f}%")
        print()
        head = f"{'model':<26}{'kill%':>6}{'fled%':>6}{'died%':>6}  {'guards: potions mean':>21}{'0':>5}{'1':>5}{'2':>5}{'3':>5}{'4':>5}{'':>3}{'door Vit%':>10}{'Overseer pots':>14}"
        print(head)

        def row(label, model, **kw):
            r = approach_stats("scout", 5, 4, model, runs, "starting", False, **kw)
            h, n = r["hall_hist"], r["runs"]
            fled = sum(v for (o, room), v in r["ends"].items() if o == "fled")
            died = sum(v for (o, room), v in r["ends"].items() if o == "died")
            print(f"{label:<26}{pct(r['kill']):>6}{pct(fled):>6}{pct(died):>6}  {r['hall']:>21.2f}" + "".join(f"{pct(h.get(i, 0) / n):>5}" for i in range(5))
                  + f"{'':>3}{pct(r['door']):>10}{r['chamber']:>14.2f}")
            return r

        row("perfect (no typing)", None)
        row("engine, all habits off", HUMAN_ZERO)
        for m in (HUMAN_LOW, HUMAN_MID, HUMAN_HIGH):
            row(m.name, m)
        print()
        print("Ablation: from the engine with every habit off, add one human habit at a time (alone), then all in order (cumulative).")
        base = HUMAN_ZERO
        steps = [("carry Vitality/Endurance/Focus", dict(carry_over=True, pause_s=3.0)),
                 ("typing delays", dict(delay_median=1.5, delay_sigma=0.6, p_distract=0.05)),
                 ("skip the skill 15%", dict(p_skip_skill=0.15)),
                 ("potion at 25-35%", dict(drink_lo=0.25, drink_hi=0.35)),
                 ("never drinks 6%", dict(p_no_drink=0.06)),
                 ("flees when out of potions", dict(flee=True)),
                 ("rests to 50% between doors", dict(carry_over=True, pause_s=3.0, rest_to=0.5))]
        print(head)
        for label, kw in steps:
            row("only: " + label, replace(base, **kw))
        cum = base
        for label, kw in steps[:-1]:
            cum = replace(cum, **kw)
            row("all up to: " + label[:14], cum)
        print()
        print("Sensitivity of the fit to resting (the middle human with rest_to varied):")
        print(head)
        for rt in (0.0, 0.3, 0.5, 0.6, 0.7, 0.8):
            row(f"mid, rest to {rt:.0%}", replace(HUMAN_MID, rest_to=rt))
        print()
        print("Sensitivity to where the 4 potions are carried (mid): in the pack from the first room instead of from the Hall's door:")
        print(head)
        row("mid, 4 from the entrance", HUMAN_MID, pack_at="entrance")
    finally:
        LEGACY = False


def human_report(runs=3000):
    """The current game: kill percentage against the Overseer, by gear, calling, level and potions, for the perfect model and the three
    humans, then the potions used and where the runs end."""
    models = (("perfect", None), ("low", HUMAN_LOW), ("mid", HUMAN_MID), ("high", HUMAN_HIGH))
    for gear, label in (("starting", "starting gear (the calling's own weapon, no armour, no trinket)"),
                        ("best", "best gear the Hollow sells at that level (best weapon, shield where the calling carries one, +5 Vitality trinket; no armour is sold)")):
        print("=" * 118)
        print(f"H1. Overseer kill% from the cellar entrance, the current game (attributes placed, aggressive creatures engage on arrival), {label}.")
        print(f"    Potions in the pack at the Overseer Hall's door; {runs} runs a cell. Perfect = the simulator's own player (acts at once, rested before every fight).")
        print("=" * 118)
        print(f"{'calling':<12}{'lvl':>4}{'potions':>8}" + "".join(f"{n:>9}" for n, _ in models) + f"{'':>4}low/mid/high: guards pots, Overseer pots, reached%")
        for c in ARCHS:
            for lvl in (4, 5):
                for pot in (2, 4):
                    rs = [approach_stats(c, lvl, pot, m, runs, gear, True) for _, m in models]
                    extra = "  ".join(f"{r['hall']:.1f}/{r['chamber']:.1f}/{pct(r['reached'])}" for r in rs[1:])
                    print(f"{c:<12}{lvl:>4}{pot:>8}" + "".join(f"{pct(r['kill']):>9}" for r in rs) + f"{'':>4}{extra}")
        print()
        print(f"H2. Where the runs end and what the potions go on, level 5, 4 potions, {gear} gear (percent of runs; 'died'/'fled' in the room named).")
        for name, m in models:
            print(f"  {name}:")
            for c in ARCHS:
                r = approach_stats(c, 5, 4, m, runs, gear, True)
                ends = sorted(r["ends"].items(), key=lambda kv: -kv[1])[:4]
                where = ", ".join(f"{o} in {room} {v * 100:.0f}%" for (o, room), v in ends) or "-"
                print(f"    {c:<11} kill {pct(r['kill'])}%  potions on the way {r['before']:.2f}, guards {r['hall']:.2f}, Overseer {r['chamber']:.2f};  Vitality at the door {pct(r['door'])}%;  {where}")
        print()


def recommend_report(runs=3000):
    """Changes that would bring a level 5 character in starting gear with 4 potions to about 70% against the Overseer under the middle
    human, for each calling. Run through tweaked(); nothing is applied. The low and high humans get a third of the runs."""
    print("=" * 118)
    print(f"R. Level 5, 4 potions at the Hall, starting gear, attributes placed, current game: Overseer kill% by calling, middle human")
    print(f"   ({runs} runs a cell; the low and high human a third of that). Armour rows: +N armour from the planned village shop, one less for the Mage.")
    print("=" * 118)
    print(f"{'option':<62}" + "".join(f"{c[:5]:>6}" for c in ARCHS) + f"{'mean':>6}{'spread':>7}{'low':>6}{'high':>6}")
    for label, kw, armour in [("the game as it is", {}, 0)] + RECOMMENDATIONS:
        rows = []
        with tweaked(**kw):
            for model, n in ((HUMAN_MID, runs), (HUMAN_LOW, runs // 3), (HUMAN_HIGH, runs // 3)):
                vals = []
                for c in ARCHS:
                    w, sh, tr, _ = gear_for(c, "starting")
                    a = armour if c != "mage" or not armour else armour - 1
                    vals.append(approach_stats(c, 5, 4, model, n, (w, sh, tr, a), True)["kill"] * 100)
                rows.append(vals)
        v = rows[0]
        print(f"{label:<62}" + "".join(f"{x:6.0f}" for x in v) + f"{sum(v) / 5:6.0f}{max(v) - min(v):7.0f}{sum(rows[1]) / 5:6.0f}{sum(rows[2]) / 5:6.0f}", flush=True)


_G, _O, _M = "Goblin Elite Guard", "Goblin Overseer", "Goblin Minion"
MAGE_VIT = {"mage": dict(vitality=(4, 8))}     # the Mage's Vitality a level 6 -> 8
# (label, tweaked() arguments, armour). Each is a candidate for the owner to choose from; none is applied.
RECOMMENDATIONS = [
    ("1a  the Overseer's Summon taken away", dict(summon=False), 0),
    ("1b  Elite Guards 40 -> 32 Vitality, damage 3-5 -> 2-4", dict(kinds={_G: dict(hp=32, dmg=(2, 4))}), 0),
    ("1c  one Elite Guard in the Hall instead of two", dict(hall=[_G]), 0),
    ("2a  Minor Healing Potion heals 30 (was 20)", dict(potion_heal=30), 0),
    ("2b  Minor Healing Potion heals 35", dict(potion_heal=35), 0),
    ("2c  = 2b and the Mage's Vitality 4 + 8 a level (was 6)", dict(potion_heal=35, archs=MAGE_VIT), 0),
    ("3a  armour sold: Leather Jerkin 2 + Leather Cap 1", {}, 3),
    ("3b  = 3a and Overseer 100 Vitality, damage 3-6", dict(kinds={_O: dict(hp=100, dmg=(3, 6))}), 3),
    ("3c  = 3b and the Mage's Vitality 4 + 8 a level", dict(kinds={_O: dict(hp=100, dmg=(3, 6))}, archs=MAGE_VIT), 3),
    ("4a  Guards 32 / 2-4, Overseer 100 / 3-6, no Summon", dict(kinds={_G: dict(hp=32, dmg=(2, 4)), _O: dict(hp=100, dmg=(3, 6))}, summon=False), 0),
    ("4b  = 4a and the Mage's Vitality 4 + 8 a level", dict(kinds={_G: dict(hp=32, dmg=(2, 4)), _O: dict(hp=100, dmg=(3, 6))}, summon=False, archs=MAGE_VIT), 0),
    ("5   one Elite Guard, no Summon", dict(summon=False, hall=[_G]), 0),
]


def stage_weapon(arch, level):
    idx = 0 if level <= 2 else (1 if level == 3 else len(arch.weapons) - 1)
    return arch.weapons[min(idx, len(arch.weapons) - 1)]


def main():
    if len(sys.argv) > 1 and sys.argv[1] == "check":
        sys.exit(0 if check_mudlib() else 1)
    if len(sys.argv) > 1 and sys.argv[1] == "corridor":
        corridor_table(int(sys.argv[2]) if len(sys.argv) > 2 else 400, tuple(sys.argv[3:]))
        return
    if len(sys.argv) > 1 and sys.argv[1] in ("calibrate", "human", "recommend"):
        runs = int(sys.argv[2]) if len(sys.argv) > 2 else 3000
        {"calibrate": calibrate_report, "human": human_report, "recommend": recommend_report}[sys.argv[1]](runs)
        return
    if len(sys.argv) > 1 and sys.argv[1] == "pace":
        pace_report(int(sys.argv[2]) if len(sys.argv) > 2 else 250)
        return
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
    print("2b. The same, for a character who has placed none of its attribute points (2 potions):")
    global PLACE_POINTS
    PLACE_POINTS = False
    unspent = []
    for n in names:
        a = ARCHS[n]
        for level in (4, 5):
            kill = boss_stats(n, level, a.weapons[-1], 2, TRINKET_HP, runs)[0]
            unspent.append(f"{n} L{level} {kill * 100:.0f}%")
    PLACE_POINTS = True
    print("   " + "   ".join(unspent))
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
