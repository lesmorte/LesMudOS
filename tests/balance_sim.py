#!/usr/bin/env python3
"""Balance simulation for Vincent's Hollow and the five archetypes (docs/CLASS_SYSTEM.md, COMBAT_TIMING.md, GAME_SYSTEMS.md).

Standalone: it does not talk to the driver. It plays the combat rules of /std/living with the numbers of the creatures,
items and class files in the mudlib, and prints tables for a solo player of each archetype:

  1. room by room: win rate, Vitality lost, fight length in seconds, at the level a sensible player has
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

Everything above and every table the default run prints is the PERFECT player: he acts the instant he is able, drinks at exactly 35% and
starts every fight at full strength. The human player (docs/BALANCE_HUMAN.md) is separate and off by default:
  python3 tests/balance_sim.py calibrate [runs]   the old game's evening of the owner (level 5 Scout, Hunting Bow, 4 potions): fit, ablation
  python3 tests/balance_sim.py ablate [runs]      the current game: what each human habit costs, by calling
  python3 tests/balance_sim.py human [runs]       the current game: kill% from the cellar entrance for the perfect and three human models
  python3 tests/balance_sim.py recommend [runs] [row prefix ...]   changes that would bring the middle human to about 70%
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
HEAL_COST, HEAL_PREP, HEAL_REC = 25, 0.5, 1.0
AIM_PREP, AIM_REC = 1.0, 1.0   # Aimed Shot (mudlib/skills/aimed_shot.cs)
RANGED_OPENING_MS = 2000       # std/living.cs RangedOpening: the target's first action is delayed this much when a bow starts the fight
SELF_HEAL_HALF = True   # the solo Priest heals itself, and Minor Heal gives the caster a third of what it gives another, rounded half up (the owner's decision, 22 November 2026; mudlib/skills/minor_heal.cs SelfShare)
HEAL_AMOUNT = None   # None: the Minor Heal of the rank table (SKILL_RANKS); a number overrides every rank (the old recommendations' tweaks)
DOUBLE_STRIKE = True # False: the Adventurer as before ranks (no Double Strike), for comparison tables only
BLESS_COST = 25
DOUBLE_PREP, DOUBLE_REC = 0.5, 1.0

# ---- Skill ranks (docs/specs/skill-ranks.md; mudlib/std/skill.cs RankValue; `check` compares the tables with the /skills files) ----
# Rank r needs level RANK_LEVEL_STEP x (r - 1). A rank's number is its row's, else the previous rank's plus the default step (EFFECT_STEP of
# the rank I value for an effect, COST_STEP for a cost, nothing for a fixed key). "base" is rank I. Costs: "endurance" / "focus".
RANK_LEVEL_STEP = 5
EFFECT_STEP, COST_STEP = 0.30, 0.15
RANK_FIXED = {"hits", "range", "seconds", "stagger", "wait"}
SKILL_RANKS = {
    "arcane_bolt": dict(base=dict(focus=20, min=6, max=9), rows={2: dict(min=8, max=12, focus=23), 3: dict(min=10, max=14, focus=26)}),
    # The owner's table (docs/specs/skill-ranks.md, "Owner's decisions after the build"): 20 / 38 / 60 for 25 / 32 / 40 Focus, then the default step;
    # the wait between two casts is HEAL_WAIT_S below.
    "minor_heal": dict(base=dict(focus=25, heal=20, wait=15), rows={2: dict(heal=38, focus=32), 3: dict(heal=60, focus=40)}),
    "blessing": dict(base=dict(focus=25, hit=1, armor=1, seconds=60), rows={2: dict(hit=2, armor=2), 3: dict(hit=3, armor=3), 4: dict(hit=4, armor=4)}),
    "shield_bash": dict(base=dict(endurance=15, factor=0.75, stagger=2.0), rows={2: dict(factor=0.98), 3: dict(factor=1.2)}),
    # Precision Strike's ambush factor (docs/specs/sneak.md): the damage factor of the blow that starts the fight from hiding, x1.5 / 1.75 / 2.0 / 2.25.
    "precision_strike": dict(base=dict(endurance=25, tohit=4, crit=15, ambush=1.5),
                             rows={2: dict(tohit=5, crit=20, ambush=1.75), 3: dict(tohit=6, crit=25, ambush=2.0), 4: dict(tohit=7, crit=30, ambush=2.25)}, fixed={"ambush"}),
    # Sneak (docs/specs/sneak.md): Endurance a room 5 / 4 / 3, and the change to a creature's chance to notice 0 / -10 / -20 points.
    "sneak": dict(base=dict(room=5, notice=0), rows={2: dict(room=4, notice=-10), 3: dict(room=3, notice=-20)}, max=3, fixed={"room", "notice"}),
    "double_strike": dict(base=dict(endurance=35, hits=2, factor=0.6), rows={2: dict(factor=0.75), 3: dict(hits=3, factor=0.6), 4: dict(hits=4, factor=0.55)}),
    "tracking": dict(base=dict(endurance=5, range=1), rows={2: dict(range=2)}, max=2),
    # Aimed Shot (docs/specs/hollow-expansion.md section 1): x1.75 / 2.0 / 2.25 (IV 2.5, V 2.75 by +0.25) and, as the shot that starts the fight,
    # x2.5 / 3.0 / 3.5 (IV 4.0, V 4.5 by +0.5). Costs by the default step. The to-hit bonus is +3 at every rank. Rank VI is not offered.
    "aimed_shot": dict(base=dict(endurance=30, tohit=3, factor=1.75, opener=2.5),
                       rows={2: dict(factor=2.0, opener=3.0), 3: dict(factor=2.25, opener=3.5), 4: dict(factor=2.5, opener=4.0), 5: dict(factor=2.75, opener=4.5)},
                       max=5, fixed={"tohit"}),
}
# The preparation of each ranked skill (never changed by a rank; option (c) of the Minor Heal table tries a longer one).
SKILL_PREP = {"minor_heal": 0.5}
# Which act of the simulator is which skill, and the skill each calling uses in a fight.
SKILL_OF = {"bash": "shield_bash", "strike": "precision_strike", "bolt": "arcane_bolt", "heal": "minor_heal", "bless": "blessing", "double": "double_strike",
            "aim": "aimed_shot"}
CALLING_SKILLS = {"warrior": ("shield_bash",), "scout": ("precision_strike", "tracking", "sneak", "aimed_shot"), "priest": ("minor_heal", "blessing"),
                  "mage": ("arcane_bolt",), "adventurer": ("double_strike",)}
# How ranks are held: "cap" = every skill at the highest rank the level allows (the character buys a rank when level and gold allow; at levels
# 5 and 10 the gold is in hand, see `ranks`), "none" = rank I in everything (nobody buys), or a number = that rank where the level allows it.
RANK_MODE = "cap"
RANK_IGNORE_CAP = False   # True: a rank is held whatever the level (rank II at level 4: the `ranks aim` table of the Overseer at level 4 asks for it)
# Minor Heal's wait (mudlib/skills/minor_heal.cs HealWait): seconds after a heal begins before the same caster may begin another. The game: 15.
HEAL_WAIT_S = 15
HEAL_LOCKOUT_MS = HEAL_WAIT_S * 1000


def rank_cap(level):
    return max(1, level // RANK_LEVEL_STEP + 1)


def rank_value(skill, key, rank):
    """mudlib/std/skill.cs RankValue."""
    t = SKILL_RANKS[skill]
    base = t["base"].get(key, 0)
    if key == "crit" and base < 0:
        return base
    step = 0 if key in RANK_FIXED or key in t.get("fixed", ()) else COST_STEP if key in ("endurance", "focus") else EFFECT_STEP
    value = base
    for r in range(2, rank + 1):
        row = t["rows"].get(r, {})
        value = row[key] if key in row else value + step * base
    return value


def rank_int(skill, key, rank):
    v = rank_value(skill, key, rank)
    return -rnd(-v) if v < 0 else rnd(v)   # half away from zero, as RankInt (Sneak's notice change is negative)


def default_ranks(arch_name, level, mode=None):
    mode = RANK_MODE if mode is None else mode
    out = {}
    for sk in CALLING_SKILLS.get(arch_name, ()):
        top = SKILL_RANKS[sk].get("max", 99)
        r = 1 if mode == "none" else rank_cap(level) if mode == "cap" else int(mode) if RANK_IGNORE_CAP else min(int(mode), rank_cap(level))
        out[sk] = max(1, min(top, r))
    return out


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
    "warden's recurve": ((13, 19), 1, 0.5, 1.0),   # the Hidden Cache, once per character (mudlib/domains/hollow/obj/wardens_recurve.cs)
    "yew longbow": ((11, 16), 1, 0.5, 1.0),   # sold at the Strung Yew, 60 gold (the best bow there is)
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
BOWS = ("hunting bow", "yew longbow", "warden's recurve")


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
    Arch("scout", (3, 8), 2.0, 1.00, (81, 2), (2, 1), (5, 9, 5, 6, 5, 6), 10, ("hunting bow", "fine longsword"), False, dodge=5),
    Arch("priest", (7, 7), 2.5, 0.97, (23, 1), (75, 4), (5, 5, 6, 6, 9, 5), 15, ("novice mace",), True, healing_pct=25, potions=1),
    Arch("mage", (4, 7), 2.5, 0.96, (14, 1), (71, 4), (5, 5, 5, 9, 7, 5), 15, ("apprentice staff",), False, spell_cost_pct=-20, potions=1),
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
        self.ranks = {}
        self.bless_bonus = (1, 1)   # to hit and armor of the Blessing on this fighter
        self.heal_ready = 0         # Minor Heal's wait: the next heal may not begin before this (ms in the current fight)
        self.archer = False         # the archer Scout: opens with Aimed Shot, then aims whenever it can pay
        self.weapon = ""
        self.cur_act = None         # the action in progress (a shot being aimed is spoiled by a blow)
        self.aim_opening = False    # the Aimed Shot in progress is the action that starts the fight
        self.strike_ambush = False  # the Precision Strike in progress is the blow that starts the fight from hiding
        self.n_act = 0              # actions this player has begun in the current fight (BALANCE_SCOUT.md: a creature killed by the first one)
        self.dmg_in = 0             # Vitality this player has lost to blows (not to potions) since it was reset

    def rank(self, skill):
        return self.ranks.get(skill, 1)

    def skill_int(self, skill, key):
        return rank_int(skill, key, self.rank(skill))

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
        return rnd(self.rate * self.level) + self.to_hit + (self.bless_bonus[0] if t < self.bless_until else 0) + int(self.mod("tohit", t)) + stat(self.attrs, "precision")

    def defense(self):
        return max(0, self.level - 1) + stat(self.attrs, "evasion")

    def resists(self):
        """Physical Resistance against a stagger or a slow, rolled to a tenth of a percent."""
        chance = stat(self.attrs, "physical_resistance")
        return chance > 0 and random.randrange(1000) < round(chance * 10)

    def total_armor(self, t=0):
        return self.armor + (self.bless_bonus[1] if t < self.bless_until else 0) + int(self.mod("armor", t))

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


SNEAKING = False   # True: the Scout and the archer sneak into every room they can afford (docs/specs/sneak.md); see run_approach
SNEAK_ALERT = ("Goblin Guard", "Goblin Elite Guard")   # creatures flagged Alert in the mudlib (mudlib/domains/hollow/mob)
SNEAK_BOSS = ("Goblin Overseer",)                      # flagged Boss: always notices


SNEAK_ELITE = ("Goblin Elite Guard",)   # only for the options of docs/BALANCE_SCOUT.md (option C): the game has no Elite flag

# The options of docs/BALANCE_SCOUT.md. Every one is off (the present game) unless `scout_option` switches it on. "second": None, or how the target's
# second notice roll (while an unnoticed Scout draws Aimed Shot) is made; "per_level": extra notice points per creature level above the Scout
# (option C); "elite": extra notice points for SNEAK_ELITE (option C); "cap": the share of the target's maximum Vitality an Aimed Shot opener may deal;
# "floor": the lowest base notice chance in percent (the game: 10; option F, not in the owner's list).
OPT = dict(second=None, second_plus=0, second_scale=1.0, per_level=0, elite=0, cap=None, floor=10)
SIM_REROLL = True      # a fix to match the game: a creature that has not noticed the Scout re-rolls every 10 s at half the chance while he types his command
SIM_BOW_STRIKE = True  # a fix to match the game: the ranged opening (2 s) goes to any Scout whose weapon is a bow, Precision Strike included (the melee Scout of the standard test holds the Hunting Bow)


def notice_chance(grace, scout_level, creature_level, alert, adjust, elite=False):
    """mudlib/secure/sneak.cs NoticeChance, in percent (plus the options of docs/BALANCE_SCOUT.md, all off by default)."""
    diff = creature_level - scout_level
    c = 50 - 5 * (grace - 5) + 5 * diff + (OPT["per_level"] * diff if diff > 0 else 0)
    c = max(OPT["floor"], min(90, c))
    return max(5, min(100, c + (30 if alert else 0) + (OPT["elite"] if elite else 0) + adjust))


def second_chance(entry):
    """Option B: the chance, in percent, of the target's second notice roll while the shot is drawn, or None (no second roll)."""
    if OPT["second"] is None or entry is None:
        return None
    return max(0.0, min(100.0, entry * OPT["second_scale"] + OPT["second_plus"]))


def sneak_room_roll(p, level, creatures, rng):
    """The notice rolls on entering a room. Returns (hidden, chances): hidden = no creature noticed the Scout; chances = each creature's entry chance (None: a Boss)."""
    grace = attrs_at(p.arch, level)[ATTR.index("grace")]
    adjust = p.skill_int("sneak", "notice")
    chances = [None if c in SNEAK_BOSS else notice_chance(grace, level, KINDS[c].level, c in SNEAK_ALERT, adjust, c in SNEAK_ELITE) for c in creatures]
    for ch in chances:
        if ch is None or rng.random() * 100 < ch:
            return False, chances
    return True, chances


def reroll_notices(chances, typed_ms, rng):
    """mudlib/std/adventurer.cs SneakBeat: every 10 s an unaware creature rolls again at half the chance, for as long as the Scout has not yet acted."""
    for _ in range(int(typed_ms // 10000)):
        for ch in chances:
            if ch is not None and rng.random() * 100 < ch / 2:
                return True
    return False


ARCHER = "archer"   # the archer Scout: a name for run_approach, boss_stats and make_player (a Scout with a bow that opens with Aimed Shot), not a calling


def make_player(arch, level, weapon, shield=None, extra_hp=0, full=True, armor_extra=0, ranks=None):
    archer = arch == ARCHER
    if archer:
        arch = "scout"
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
    p.archer = archer
    p.weapon = weapon
    p.ranks = dict(ranks) if ranks is not None else default_ranks(arch.name, level)
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
    a["heal"] = Act("heal", SKILL_PREP.get("minor_heal", HEAL_PREP), HEAL_REC)
    a["bless"] = Act("bless", 0.5, 1.0)
    a["double"] = Act("double", DOUBLE_PREP, DOUBLE_REC)
    a["aim"] = Act("aim", AIM_PREP, AIM_REC)
    return a


def skill_cost(p, act_id):
    """(Endurance, Focus) an act costs this player at its rank, as Living.UseSkill: the rank's whole cost, then the spell cost modifier."""
    sk = SKILL_OF.get(act_id)
    if sk is None:
        return 0, 0
    end = p.skill_int(sk, "endurance")
    foc = p.skill_int(sk, "focus")
    if foc > 0 and p.arch is not None and p.arch.spell_cost_pct:
        foc = max(1, rnd(foc * (100 + p.arch.spell_cost_pct) / 100.0))
    return end, foc


def can_pay(p, act_id, extra_mana=0):
    end, foc = skill_cost(p, act_id)
    return p.stamina >= end and p.mana >= foc + extra_mana


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


def heal_ok(p, t):
    return can_pay(p, "heal") and t >= p.heal_ready


def player_choice(p, t, living, potions, hard, state):
    """Returns (act id, potions left)."""
    arch = p.arch
    p.regen(t)
    target = living[0]
    if potions > 0 and p.hp <= p.max_hp * 0.35:
        return "potion"
    if arch.name == "priest":
        if p.hp <= p.max_hp * 0.5 and heal_ok(p, t):
            return "heal"
        if hard and t >= p.bless_until and can_pay(p, "bless", skill_cost(p, "heal")[1]) and not state.get("blessed"):
            state["blessed"] = True
            return "bless"
    if arch.name == "warrior" and can_pay(p, "bash") and p.shield:
        if target.resolve_at is not None and target.resolve_at > t:
            return "bash"
        if p.stamina >= 60:
            return "bash"
    if arch.name == "scout" and p.archer and can_pay(p, "aim"):
        return "aim"
    if arch.name == "scout" and not p.archer and can_pay(p, "strike"):
        return "strike"
    if arch.name == "mage" and can_pay(p, "bolt"):
        return "bolt"
    if arch.name == "adventurer" and DOUBLE_STRIKE and can_pay(p, "double"):
        return "double"
    return "attack"


def fight(player, enemies, potions=0, boss=False, on_kill=None, hard=None, human=None, start=None, stop_at_boss=False, opens=False, heal_carry=0, ambush=False, second=None):
    """Fight until one side is dead. Mutates player and enemies. Returns a dict: alive, ms, actions, potions,
    boss_ms and lost_at_kill.
    The perfect player (human None) decides at every Ready Time. A HumanPolicy (see "the human player" below) types its
    commands with a delay, which arrive as events of their own, and may flee (out["fled"]). start = (the player's first
    Ready Time, [each enemy's first Ready Time]) in ms, or None for the perfect model's "the player starts at once, a creature
    waits its Reaction Time x 2 s". stop_at_boss ends the fight the moment enemies[0] dies (what the human approach asks).
    opens: the player chose the fight (a creature that is not aggressive, or hypothetically): the player's first action is the one that starts it
    (Aimed Shot x opener), and a bow in the hands delays each creature's first action a further RANGED_OPENING_MS. Never set where the creatures are
    aggressive and engage on arrival (every creature of the cellars): they start those fights. heal_carry: ms of Minor Heal's wait still to run.
    ambush: the player starts the fight from hiding (a sneaking Scout nobody noticed): the first action is the opener (Aimed Shot x opener, Precision
    Strike x ambush); `start` already holds the surprised target's doubled delay and the woken creatures' own (see run_approach).
    second: (docs/BALANCE_SCOUT.md, option B) the chance in percent that the target notices an unnoticed archer while the opening Aimed Shot is drawn; if it
    does, the shot is an ordinary one and the target acts as in a normal fight start (its Reaction Time x 2 s from now)."""
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
    player.heal_ready = heal_carry
    player.cur_act = None
    if start is None:
        start = (0, [round(e.reaction * UNIT_MS) for e in enemies])
    if opens and player.weapon in BOWS:
        start = (start[0], [x + RANGED_OPENING_MS for x in start[1]])
    state["opening"] = opens or ambush
    state["ambush"] = ambush
    state["second"] = second
    player.dmg_in = 0
    player.n_act = 0
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
                ent.cur_act = act
                ent.n_act = out["actions"]
                ent.aim_opening = choice == "aim" and bool(state.get("opening"))
                ent.strike_ambush = choice == "strike" and bool(state.get("ambush"))
                if ent.aim_opening and state.get("ambush") and state.get("second") is not None:
                    out["second_rolls"] = out.get("second_rolls", 0) + 1
                    if random.random() * 100 < state["second"]:   # the target notices while the shot is drawn
                        out["second_noticed"] = out.get("second_noticed", 0) + 1
                        ent.aim_opening = False
                        tgt = living[0]
                        tgt.gen += 1
                        tgt.resolve_at = None
                        tgt.ready_at = t + round(tgt.reaction * UNIT_MS)
                        push(tgt.ready_at, "ready", tgt)
                state["opening"] = False
                state["ambush"] = False
                ent.paid = (0, 0)
                if choice == "potion":
                    potions -= 1
                    out["potions_used"] = out.get("potions_used", 0) + 1
                    ent.hp = min(ent.max_hp, ent.hp + POTION_HEAL)
                    push(t + ent.delay_ms(act, t), "ready", ent)
                    ent.ready_at = t + ent.delay_ms(act, t)
                    continue
                if choice in SKILL_OF:
                    end, foc = skill_cost(ent, choice)
                    ent.stamina -= end
                    ent.mana -= foc
                    ent.paid = (end, foc)
                    if choice == "heal" and HEAL_LOCKOUT_MS:
                        ent.heal_ready = t + HEAL_LOCKOUT_MS
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
        amount = HEAL_AMOUNT if HEAL_AMOUNT is not None else ent.skill_int("minor_heal", "heal")
        if SELF_HEAL_HALF:
            amount = (2 * amount + 3) // 6   # on the caster: a third, rounded half up, before Divine Knowledge and Healing Power
        ent.hp = min(ent.max_hp, ent.hp + max(1, rnd(amount * (100 + ent.arch.healing_pct + stat(ent.attrs, "healing_power")) / 100.0)))
        return
    if act.id == "bless":
        ent.bless_until = t + ent.skill_int("blessing", "seconds") * 1000
        ent.bless_bonus = (ent.skill_int("blessing", "hit"), ent.skill_int("blessing", "armor"))
        return
    if act.id == "bolt":
        if target.dodge() and random.randint(1, 100) <= target.dodge():
            return
        lo, hi = ent.skill_int("arcane_bolt", "min"), ent.skill_int("arcane_bolt", "max")
        dmg = max(1, rnd(random.randint(lo, max(lo, hi)) * (1 + stat(ent.attrs, "spell_power") / 100.0)))
        target.hp -= dmg
        if target.hp <= 0 and on_kill is not None:
            on_kill(player, target)
        return
    if ent.is_player and act.id == "double":
        for _ in range(max(1, ent.skill_int("double_strike", "hits"))):
            if target.hp <= 0:
                break
            dmg, _ = do_hit(ent, target, t, factor=rank_value("double_strike", "factor", ent.rank("double_strike")))
            if dmg > 0:
                target.hp -= dmg
                if target.hp <= 0 and on_kill is not None:
                    on_kill(player, target)
        return
    if ent.is_player:
        kw = {}
        if act.id == "aim":
            rk = ent.rank("aimed_shot")
            kw = dict(to_hit_bonus=ent.skill_int("aimed_shot", "tohit"), factor=rank_value("aimed_shot", "opener" if ent.aim_opening else "factor", rk))
        if act.id == "strike":
            kw = dict(to_hit_bonus=ent.skill_int("precision_strike", "tohit"), crit=ent.skill_int("precision_strike", "crit"))
            if ent.strike_ambush:
                kw["factor"] = rank_value("precision_strike", "ambush", ent.rank("precision_strike"))
                ent.strike_ambush = False
        if act.id == "bash":
            kw = dict(factor=rank_value("shield_bash", "factor", ent.rank("shield_bash")))
        dmg, _ = do_hit(ent, target, t, **kw)
        if dmg <= 0:
            return
        if OPT["cap"] and act.id == "aim" and ent.aim_opening:   # option D: the opener may not take more than a share of the target's maximum Vitality
            dmg = min(dmg, max(1, int(OPT["cap"] * target.max_hp + 1e-9)))
        target.hp -= dmg
        if act.id == "bash" and target.hp > 0:
            out["stagger"](target, t, rank_value("shield_bash", "stagger", ent.rank("shield_bash")))
        if target.hp <= 0 and on_kill is not None:
            on_kill(player, target)
        return
    bonus = 2 if t < ent.cry_until else 0
    dmg, _ = do_hit(ent, target, t, mult=act.mult, bonus=bonus)
    if dmg > 0:
        target.hp -= dmg
        target.dmg_in += dmg
        spoil(target, t, push)


def spoil(e, now, push):
    """Being hit while a shot is being aimed spoils it (Living.SpoilPreparation): the action is cancelled, nothing of its cost comes back, half
    its Recovery is still paid."""
    if e.hp <= 0 or not e.is_player or e.resolve_at is None or e.resolve_at <= now or e.cur_act is None or e.cur_act.id != "aim":
        return
    e.gen += 1
    half = (e.ready_at - e.resolve_at) // 2
    e.resolve_at = None
    e.ready_at = now + half
    e.cur_act = None
    push(e.ready_at, "ready", e)


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


def boss_stats(arch, level, weapon, potions, extra_hp=0, runs=1500, shield=None, opens=False, sneak=False):
    """The perfect player against the Overseer alone. The perfect model's player acts at once and the Overseer waits its Reaction Time x 2 s (the model's
    own assumption; the real Overseer is aggressive and starts the fight). opens: the player really chose the fight (hypothetical). sneak: a Scout who
    sneaked into the chamber: the Overseer is a Boss and always notices, so the fight is the same, but the room has cost the Scout its Endurance."""
    kills = survive = lost = ms = actions = 0
    for _ in range(runs):
        p = make_player(arch, level, weapon, shield, extra_hp)
        if sneak:
            p.stamina -= p.skill_int("sneak", "room")
        r = fight(p, [make_monster("Goblin Overseer")], potions, boss=True, opens=opens)
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
    # The `rest` command (docs/specs/rest.md; python3 tests/balance_sim.py rest). Off unless rest_cmd: then rest_to is not used.
    # PATIENCE RULE ("sit until content or bored"): after the loot, in the room just cleared, the player types `rest` and stays seated
    # until Vitality is at least rest_target of the maximum (with rest_all: until all three resources are, the "You are fully rested"
    # message), or rest_patience_s seconds have passed, whichever comes first. Then he walks on.
    rest_cmd: bool = False
    rest_factor: float = 1.0       # RestFactor: all three resources regenerate this many times faster while seated
    rest_target: float = 0.0
    rest_all: bool = False
    rest_patience_s: float = 0.0
    rest_surprise_s: float = 2.0   # RestSurprise: the extra delay of the first action of a character attacked while seated
    p_linger: float = 0.0          # chance, at each rest, that the player stays seated long after he is done (away from the keyboard,
    linger_s: tuple = (60.0, 600.0)  # chatting) for this many extra seconds (uniform): the only way a rest outlasts a cleared room's reset


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
            if p.hp <= p.max_hp * 0.5 and heal_ok(p, t):
                return "heal"
            if hard and t >= p.bless_until and can_pay(p, "bless", skill_cost(p, "heal")[1]) and not state.get("blessed"):
                state["blessed"] = True
                return "bless"
        elif a == "warrior" and p.shield and can_pay(p, "bash"):
            return "bash"
        elif a == "scout" and p.archer and can_pay(p, "aim"):
            return "aim"
        elif a == "scout" and not p.archer and can_pay(p, "strike"):
            return "strike"
        elif a == "mage" and can_pay(p, "bolt"):
            return "bolt"
        elif a == "adventurer" and DOUBLE_STRIKE and can_pay(p, "double"):
            return "double"
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
    if arch_name == ARCHER:   # a Scout with a bow: the Hunting Bow it starts with, or the best bow the Hollow sells
        return ("hunting bow", False, 0, 0) if gear == "starting" else ("yew longbow", False, TRINKET_HP, 0) if gear == "best" else gear
    a = ARCHS[arch_name]
    if gear == "starting":
        return a.weapons[0], a.shield and arch_name == "warrior", 0, 0
    if gear == "best":
        return a.weapons[-1], a.shield, TRINKET_HP, 0
    return gear


SURVIVAL = False   # True: the Adventurer's Survival Training (x1.5 out of combat for all three, mudlib/skills/survival_training.cs). Off in every
                   # table that is quoted elsewhere; `rest` prints one row with it on.


def rest(p, seconds, factor=1.0):
    """What a character regains standing about (no safe room): the out-of-combat rates of resources(); factor is RestFactor while seated."""
    _, _, _, st_rate, ma_rate, hp_rate = resources(p.arch, p.level)
    if SURVIVAL and p.arch.name == "adventurer":
        factor = factor * 1.5
    p.hp = min(p.max_hp, p.hp + hp_rate * factor * seconds / 10.0)
    p.stamina = min(p.max_stamina, p.stamina + st_rate * factor * seconds / 10.0)
    p.mana = min(p.max_mana, p.mana + ma_rate * factor * seconds / 10.0)


def seconds_to_rest(p, model):
    """How long the patience rule keeps the player seated from where he is: until Vitality (or, with rest_all, all three) reaches
    rest_target, at most rest_patience_s. Seconds."""
    _, _, _, st_rate, ma_rate, hp_rate = resources(p.arch, p.level)
    f = model.rest_factor * (1.5 if SURVIVAL and p.arch.name == "adventurer" else 1.0)
    need = max(0.0, model.rest_target * p.max_hp - p.hp) / (hp_rate * f / 10.0)
    if model.rest_all:
        if st_rate > 0:
            need = max(need, max(0.0, model.rest_target * p.max_stamina - p.stamina) / (st_rate * f / 10.0))
        if ma_rate > 0:
            need = max(need, max(0.0, model.rest_target * p.max_mana - p.mana) / (ma_rate * f / 10.0))
    return min(need, model.rest_patience_s)


# When a cleared room's creatures come back (mudlib/std/room.cs, corpse.cs; docs/BALANCE_HUMAN.md "Resting"): every cellar room resets every
# 300 s (no file changes ResetSeconds), but a reset replaces a creature only when its corpse is gone, and a corpse that holds a skin (rats, toad) lasts
# 600 s, one that holds fresh blood only (goblins, bats; the player does not bleed them) 300 s of fresh blood + 120 s empty = 420 s. The reset after that
# time (uniformly 0 to 300 s later) brings the creature back, and it is Aggressive. Smuggler Hall has no spawn at all.
CORPSE_SECONDS_BY_ROOM = {"Tavern Cellar": 600, "Collapsed Passage": 600, "Storage Chamber": 420, "Flooded Tunnel": 600, "Mushroom Cavern": 420,
                          "Goblin Barracks": 420, "Overseer Hall": 420}
RESET_SECONDS = 300


def start_times(p, enemies, delay_ms, aggro, surprise_ms=0):
    """The first Ready Time of the player and of each creature, in ms from the player's arrival in the room.
    aggro (the current game): the creatures start the fight at once and wait their Reaction Time x 2 s; the player, drawn in,
    waits his own, and a command typed after that is later still. Old game: the first creature to notice calls out after its
    Reaction Time x 2 s; a player who has typed `kill` before that starts the fight (and acts at once), one who has not is
    ambushed (the creature acts at once, the player waits his own Reaction Time)."""
    react = [round(e.reaction * UNIT_MS) for e in enemies]
    own = round(p.reaction * UNIT_MS)
    if aggro:
        return max(delay_ms, own + surprise_ms), react
    call = min(react)
    if delay_ms < call:
        return delay_ms, [delay_ms + r for r in react]
    first = react.index(call)
    return max(delay_ms, call + own), [call if i == first else call + r for i, r in enumerate(react)]


def run_approach(arch_name, level, potions, model, rng, gear="starting", aggro=True, pack_at="hall", force_open=False):
    """One run from the Tavern Cellar to the Overseer. model None is the perfect player: full Vitality, Endurance and Focus for
    every fight, no typing, the existing rules. Returns a dict: outcome ("kill", "died", "fled"), room of the end, potions used by
    room, potions left, Vitality share at the Overseer's door, and "t": seconds spent fighting, walking and looting, and resting (waiting
    at the ordinary rate for rest_to, or seated with the `rest` command); "refills": rests cut short by creatures coming back.
    force_open (hypothetical, the archer only): every fight is the player's choice, as if no creature were aggressive: the player acts as soon as his
    typed command arrives, the creatures wait their Reaction Time x 2 s and a bow in his hands delays them a further 2 s ("opens" in fight()). res["opened"]
    counts the fights the player opened: always 0 where the creatures are aggressive and engage on arrival, which is every cellar creature.
    pack_at: "hall" = the player enters the Overseer Hall with `potions` in the pack (the rooms before it use a spare supply, which
    is reported as used "on the way" but does not count against the pack: a player restocks at the merchant); "entrance" = the
    pack is `potions` from the first room on and the rooms before the Hall draw on it."""
    arch = ARCHS["scout" if arch_name == ARCHER else arch_name]
    weapon, shield, extra_hp, armor = gear_for(arch_name, gear)
    p = make_player(ARCHER if arch_name == ARCHER else arch, level, weapon, shield, extra_hp, armor_extra=armor)
    wait_carry = [0, 0.0]   # Minor Heal's wait left at the end of the last fight (ms), and the walking and resting seconds counted then
    left = potions if pack_at == "entrance" else 99
    used = {}
    t = {"fight": 0.0, "walk": 0.0, "rest": 0.0}
    res = {"outcome": "kill", "room": "Overseer Chamber", "used": used, "door": None, "t": t, "refills": 0, "opened": 0, "fights": 0,
           "sneak_spent": 0, "sneak_rooms": 0, "sneak_opened": 0, "sneak_seen": 0, "sneak_second": 0}
    sneak_on = SNEAKING and arch_name in ("scout", ARCHER)   # sneaks into every room it can afford (docs/specs/sneak.md)

    def sneak_pay():
        """The room's Endurance, if the Scout has it (else he goes in openly)."""
        cost = p.skill_int("sneak", "room")
        if p.stamina + 1e-9 < cost:
            return False
        p.stamina -= cost
        res["sneak_spent"] += cost
        res["sneak_rooms"] += 1
        return True

    def heal_left():
        """Minor Heal's wait still running when the next fight begins: what was left, less the walking and resting since."""
        gone = (t["walk"] + t["rest"] - wait_carry[1]) * 1000
        return max(0, round(wait_carry[0] - gone))

    def fought(r):
        wait_carry[0] = max(0, getattr(p, "heal_ready", 0) - r["ms"])
        wait_carry[1] = t["walk"] + t["rest"]
    moves, looted = 0, 0
    prev_room, prev_creatures, prev_ms = None, None, 0
    for room, creatures in APPROACH:
        moves += 1
        if not creatures:
            if sneak_on:
                sneak_pay()
            continue
        boss = room == "Overseer Chamber"
        if room == "Overseer Hall" and pack_at == "hall":
            left = potions
        if model is None or not model.carry_over:
            p.hp, p.stamina, p.mana = p.max_hp, float(p.max_stamina), float(p.max_mana)
            wait_carry[0] = 0
        elif room != ROOM_NAMES[0] and model.rest_cmd:
            # the `rest` command: loot and look round (ordinary rate), sit in the room just cleared (RestFactor), then walk on (ordinary rate)
            wait = looted * LOOT_SECONDS + model.pause_s
            rest(p, wait)
            t["walk"] += wait
            seated = seconds_to_rest(p, model)
            if model.p_linger and rng.random() < model.p_linger:
                seated += rng.uniform(*model.linger_s)
            safe = CORPSE_SECONDS_BY_ROOM.get(prev_room, 10 ** 9) - prev_ms / 1000.0   # earliest moment, after the fight, a creature can return
            if wait + seated > safe:   # the rest may outlast the reset: the real moment is `safe` plus a uniform 0..300 s
                back = safe + rng.random() * RESET_SECONDS
                if wait + seated > back:
                    before = max(0.0, back - wait)
                    rest(p, before, model.rest_factor)
                    t["rest"] += before
                    res["refills"] += 1
                    enemies = [make_monster(c) for c in prev_creatures]
                    policy = HumanPolicy(model, rng)
                    start = start_times(p, enemies, policy.delay(), aggro, round(model.rest_surprise_s * 1000))
                    r = fight(p, enemies, left, boss=False, human=policy, start=start, stop_at_boss=True, heal_carry=heal_left())
                    fought(r)
                    p.regen(r["ms"])
                    t["fight"] += r["ms"] / 1000.0
                    used[prev_room] = used.get(prev_room, 0) + r.get("potions_used", 0)
                    left = r["potions"]
                    if r.get("fled") or not r["alive"]:
                        res.update(outcome="fled" if r.get("fled") else "died", room=prev_room + " (back)")
                        break
                    seated = min(seconds_to_rest(p, model), max(0.0, model.rest_patience_s))   # he sits down again, by the same rule
                    rest(p, seated, model.rest_factor)
                    t["rest"] += seated
                else:
                    rest(p, seated, model.rest_factor)
                    t["rest"] += seated
            else:
                rest(p, seated, model.rest_factor)
                t["rest"] += seated
            wait = moves * ROOM_SECONDS
            rest(p, wait)
            t["walk"] += wait
        elif room != ROOM_NAMES[0]:
            wait = moves * ROOM_SECONDS + looted * LOOT_SECONDS + model.pause_s
            rest(p, wait)
            t["walk"] += wait
            if p.hp < model.rest_to * p.max_hp:
                wait = (model.rest_to * p.max_hp - p.hp) / (resources(arch, level)[5] / 10.0) + 0.01
                rest(p, wait)
                t["rest"] += wait
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
        opens = False
        hidden = False   # sneaking, and no creature of the room has noticed: the Scout opens the fight
        chances = []
        if sneak_on and sneak_pay():
            hidden, chances = sneak_room_roll(p, level, creatures, rng)
            res["sneak_seen"] += not hidden
        if model is not None:
            policy = HumanPolicy(model, rng)
            typed = policy.delay()
            start = start_times(p, enemies, typed, aggro)
            if force_open:
                opens = True
                start = (typed, [round(e.reaction * UNIT_MS) for e in enemies])
            elif not aggro and min(round(e.reaction * UNIT_MS) for e in enemies) > typed:
                opens = True   # the old game: the player typed before any creature noticed him, and starts the fight
        else:
            opens = force_open
        if hidden and SIM_REROLL and model is not None and reroll_notices(chances, typed, rng):
            hidden = False   # a creature noticed him while he was typing (every 10 s, at half the chance); the room fights as for a normal start
            res["sneak_seen"] += 1
            start = start_times(p, enemies, typed, aggro)
        second = None
        if hidden:
            # The Scout opens (docs/specs/sneak.md): he acts when his command arrives; the target, surprised, waits twice its Reaction Time x 2 s (and a
            # bow's 2 s more); the other creatures wake and wait their own. Where one creature of a room noticed him, the whole room fights at once.
            at = typed if model is not None else 0
            ranged = RANGED_OPENING_MS if (p.archer or (SIM_BOW_STRIKE and p.weapon in BOWS)) else 0
            second = second_chance(chances[0]) if p.archer else None
            start = (at, [at + 2 * round(enemies[0].reaction * UNIT_MS) + ranged] + [at + round(e.reaction * UNIT_MS) for e in enemies[1:]])
            res["sneak_opened"] += 1
        res["fights"] += 1
        res["opened"] += opens or hidden
        r = fight(p, enemies, left, boss=boss, human=policy, start=start, stop_at_boss=True, opens=opens, heal_carry=heal_left(), ambush=hidden, second=second)
        res["sneak_second"] += r.get("second_noticed", 0)
        fought(r)
        p.regen(r["ms"])
        t["fight"] += r["ms"] / 1000.0
        used[room] = used.get(room, 0) + r.get("potions_used", 0) + topped
        left = r["potions"]
        moves, looted = 0, sum(1 for c in creatures if KINDS[c].gold[1])
        prev_room, prev_creatures, prev_ms = room, creatures, r["ms"]
        if boss and r["lost_at_kill"] is not None:
            break
        if r.get("fled") or not r["alive"] or boss:
            res.update(outcome="fled" if r.get("fled") else "died", room=room)
            break
    res["left"] = left
    return res


def approach_stats(arch_name, level, potions, model, runs=1000, gear="starting", aggro=True, seed=1, pack_at="hall", force_open=False):
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
    tsum = {"fight": 0.0, "walk": 0.0, "rest": 0.0}   # seconds, over the runs that killed the Overseer
    allpots = refills = 0.0
    opened = fights = 0
    sneak = dict(spent=0.0, rooms=0.0, opened=0.0, seen=0.0, second=0.0)
    for _ in range(runs):
        r = run_approach(arch_name, level, potions, model, rng, gear, aggro, pack_at, force_open)
        opened += r["opened"]
        for k in sneak:
            sneak[k] += r["sneak_" + k]
        fights += r["fights"]
        u = r["used"]
        allpots += sum(u.values())
        refills += r["refills"]
        if r["outcome"] == "kill":
            for k in tsum:
                tsum[k] += r["t"][k]
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
                door=door / reached if reached else 0.0, runs=runs, potions=allpots / runs, refills=refills / runs,
                minutes={k: v / kills / 60.0 if kills else 0.0 for k, v in tsum.items()},
                opened=opened / runs, fights=fights / runs, sneak={k: v / runs for k, v in sneak.items()})


REQUIRED_LEVEL = {"Tavern Cellar": 1, "Collapsed Passage": 1, "Storage Chamber": 2, "Guard Post": 2,
                  "Flooded Tunnel": 3, "Mushroom Cavern": 3, "Goblin Barracks": 4, "Overseer Hall": 4}
BOSS_LEVEL = 4
PRICES = {"rusty sword": 10, "woodcutter axe": 18, "fine longsword": 60, "hunting bow": 20, "novice mace": 12,
          "apprentice staff": 10, "training spear": 8}


FULL_RUN_GOBLINS = []   # goblins killed before reaching level 5, one entry per run of full_run (for the Arcane Bolt II unlock, `ranks`)


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
        goblins = 0

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
            nonlocal xp, gold, level, goblins
            k = enemy.kind
            if "Goblin" in k.name and level < 5:
                goblins += 1
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
        FULL_RUN_GOBLINS.append(goblins)
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
    Returns means: win rate, seconds, net Vitality lost in won fights, potions drunk, total damage taken, end resources."""
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
        want = {"Level": k.level, "MaxVitality": k.hp, "DamageMin": k.dmg[0], "DamageMax": k.dmg[1], "ToHit": k.to_hit,
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
              "hunting bow": "hunting_bow", "warden's recurve": "wardens_recurve"}
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
    check_skills(os.path.join(root, "..", ".."), problems)
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
        want = {"Level": k.level, "MaxVitality": k.hp, "DamageMin": k.dmg[0], "DamageMax": k.dmg[1], "ToHit": k.to_hit,
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
        opening, cycle, trig = arr(text, "opening"), arr(text, "cycle"), arr(text, "triggerVitality")
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
    print(f"{'creature':22}{'L':>3} {'pl':>3}  " + "".join(f"{a[:3]:>5}" for a in ARCHS) + f" {'avg':>5} {'spread':>7} {'sec':>5} {'Vitality lost':>14}")
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


SKILL_FIELDS = {"focus": "FocusCost", "endurance": "EnduranceCost", "tohit": "ToHitBonus", "crit": "CritChance", "factor": "DamageFactor", "hits": "Hits"}
SKILL_DEFAULTS = {"FocusCost": 0, "EnduranceCost": 0, "ToHitBonus": 0, "CritChance": -1, "DamageFactor": 1.0, "Hits": 1}


def check_skills(read_root, problems):
    """Compare SKILL_RANKS (rank I numbers, rows, the highest rank) and the preparation and recovery of the fighting skills with mudlib/skills,
    and the rank rule's numbers with mudlib/std/skill.cs."""
    root = os.path.join(read_root, "skills")
    with open(os.path.join(read_root, "std", "skill.cs"), encoding="utf-8") as f:
        base = f.read()
    for field, value in (("RankLevelStep", RANK_LEVEL_STEP), ("EffectStep", EFFECT_STEP), ("CostStep", COST_STEP)):
        m = re.search(rf"const (?:int|double) {field} = ([\d.]+);", base)
        if m is None or float(m.group(1)) != value:
            problems.append(f"std/skill.cs: {field} is {m.group(1) if m else None}, {value} in the simulation")
    for name, t in SKILL_RANKS.items():
        with open(os.path.join(root, name + ".cs"), encoding="utf-8") as f:
            text = f.read()
        have = {}
        for key, field in SKILL_FIELDS.items():
            m = re.search(rf"\b{field} = (-?[\d.]+);", text)
            have[key] = float(m.group(1)) if m else SKILL_DEFAULTS[field]
        consts = {n: float(v) for n, v in re.findall(r"const (?:int|double) (\w+) = ([\d.]+);", text)}
        for k, v in re.findall(r'Effect\("(\w+)", (-?[\w.]+)\)', text):
            have[k] = consts[v] if v in consts else float(v)
        for key, value in t["base"].items():
            if have.get(key) != value:
                problems.append(f"skill {name}: rank I {key} is {have.get(key)} in the mudlib, {value} in the simulation")
        rows = {}
        for r, values in re.findall(r'RankRow\((\d+), "([^"]*)"', text):
            rows[int(r)] = {k: float(v) for k, v in (part.split("=") for part in values.split(";") if part)}
        want = {r: {k: float(v) for k, v in row.items()} for r, row in t["rows"].items()}
        if rows != want:
            problems.append(f"skill {name}: rank rows {rows} in the mudlib, {want} in the simulation")
        m = re.search(r"\bMaxRank = (\d+);", text)
        if (int(m.group(1)) if m else 0) != t.get("max", 0):
            problems.append(f"skill {name}: MaxRank {m.group(1) if m else 0} in the mudlib, {t.get('max', 0)} in the simulation")
    for name, prep, rec in (("arcane_bolt", BOLT_PREP, BOLT_REC), ("minor_heal", HEAL_PREP, HEAL_REC), ("shield_bash", 0, BASH_REC),
                            ("precision_strike", STRIKE_PREP, STRIKE_REC), ("double_strike", DOUBLE_PREP, DOUBLE_REC),
                            ("aimed_shot", AIM_PREP, AIM_REC)):
        with open(os.path.join(root, name + ".cs"), encoding="utf-8") as f:
            text = f.read()
        for field, value in (("Preparation", prep), ("Recovery", rec)):
            m = re.search(rf"\b{field} = ([\d.]+);", text)
            if (float(m.group(1)) if m else 0.0) != value:
                problems.append(f"skill {name}: {field} is {m.group(1) if m else None} in the mudlib, {value} in the simulation")
    with open(os.path.join(read_root, "classes", "adventurer.cs"), encoding="utf-8") as f:
        text = f.read()
    if '"double_strike"' not in text or '"jack_of_all_trades"' not in text:
        problems.append("class adventurer: Double Strike or Jack of All Trades is not granted")
    with open(os.path.join(read_root, "classes", "scout.cs"), encoding="utf-8") as f:
        text = f.read()
    if '"aimed_shot"' not in text:
        problems.append("class scout: Aimed Shot is not granted")
    with open(os.path.join(read_root, "std", "living.cs"), encoding="utf-8") as f:
        text = f.read()
    m = re.search(r"const int RangedOpening = (\d+);", text)
    if m is None or int(m.group(1)) * 1000 != RANGED_OPENING_MS:
        problems.append(f"std/living.cs: RangedOpening is {m.group(1) if m else None}, {RANGED_OPENING_MS // 1000} in the simulation")


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
def tweaked(kinds=None, summon=True, hall=None, potion_heal=None, archs=None, heal_amount=None):
    """Temporarily change creatures (name -> fields of Kind, e.g. {"Goblin Minion": {"hp": 10}}), the Overseer's Summon, the creatures
    of the Hall, the potion's strength and the callings (name -> fields of Arch, e.g. {"mage": {"vitality": (4, 7)}}), for the
    recommendations of docs/BALANCE_HUMAN.md. Nothing here is applied to the game."""
    global SUMMON, POTION_HEAL, HEAL_AMOUNT
    saved, saved_summon, saved_approach, saved_heal, saved_archs = dict(KINDS), SUMMON, list(APPROACH), POTION_HEAL, dict(ARCHS)
    saved_minor = HEAL_AMOUNT
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
        if heal_amount is not None:   # the Priest's Minor Heal
            HEAL_AMOUNT = heal_amount
        yield
    finally:
        KINDS.clear()
        KINDS.update(saved)
        SUMMON = saved_summon
        APPROACH[:] = saved_approach
        POTION_HEAL = saved_heal
        ARCHS.clear()
        ARCHS.update(saved_archs)
        HEAL_AMOUNT = saved_minor


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


def ablate_report(runs=2000):
    """The current game, level 5, 4 potions at the Hall, starting gear: what each human habit costs, by calling. The first row is the
    perfect player of the other tables; the second is the same player run through the human engine (it must agree); the third gives it
    the arrival ambush the perfect model ignores (aggressive creatures engage on arrival and the player waits his own Reaction Time x 2 s)."""
    print("=" * 118)
    print(f"A. The current game, level 5, 4 potions at the Hall, starting gear: Overseer kill% by calling as habits are added ({runs} runs a cell)")
    print("=" * 118)
    print(f"{'':<46}" + "".join(f"{c[:5]:>7}" for c in ARCHS) + f"{'mean':>6}")

    def row(label, model, aggro=True, rest=None):
        vals = [approach_stats(c, 5, 4, model, runs, "starting", aggro)["kill"] * 100 for c in ARCHS]
        print(f"{label:<46}" + "".join(f"{v:7.0f}" for v in vals) + f"{sum(vals) / 5:6.0f}", flush=True)

    row("perfect (the simulator's player)", None, False)
    row("engine, every habit off", HUMAN_ZERO, False)
    row("+ the arrival ambush (player waits own reaction)", HUMAN_ZERO, True)
    steps = [("+ Vitality, Endurance, Focus carried over", dict(carry_over=True, pause_s=3.0)),
             ("+ typing delays", dict(delay_median=1.5, delay_sigma=0.6, p_distract=0.05)),
             ("+ skips the skill 15%", dict(p_skip_skill=0.15)),
             ("+ potion at 25-35%", dict(drink_lo=0.25, drink_hi=0.35)),
             ("+ never drinks 6%", dict(p_no_drink=0.06)),
             ("+ flees when out of potions (= human, no resting)", dict(flee=True)),
             ("+ rests to 50% before each door (= human-mid)", dict(rest_to=0.5))]
    cum = HUMAN_ZERO
    for label, kw in steps:
        cum = replace(cum, **kw)
        row(label, cum, True)


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


def recommend_report(runs=3000, only=()):
    """Changes that would bring a level 5 character in starting gear with 4 potions to about 70% against the Overseer under the middle
    human, for each calling. Run through tweaked(); nothing is applied. The low and high humans get a third of the runs."""
    print("=" * 118)
    print(f"R. Level 5, 4 potions at the Hall, starting gear, attributes placed, current game: Overseer kill% by calling, middle human")
    print(f"   ({runs} runs a cell; the low and high human a third of that). Armour rows: +N armour from the planned village shop, one less for the Mage.")
    print("=" * 118)
    print(f"{'option':<62}" + "".join(f"{c[:5]:>6}" for c in ARCHS) + f"{'mean':>6}{'spread':>7}{'low':>6}{'high':>6}")
    for label, kw, armour in [("the game as it is", {}, 0)] + RECOMMENDATIONS:
        if only and not label.startswith(tuple(only)):
            continue
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


# ---- the `rest` command (docs/specs/rest.md, docs/BALANCE_HUMAN.md "Resting"): python3 tests/balance_sim.py rest [runs] [part ...]

# The patience rule of each human: after the loot, in the room just cleared, he types `rest` and stays seated until Vitality reads full (the
# sharp one: until the game says "You are fully rested", all three resources full) or until his patience runs out, whichever is first.
# The times are what a person will stare at one prompt: two minutes, one minute, twenty seconds. For the middle human they are about the time
# the old model had him wait (an average of 25 to 45 s at each door to reach 50%); p_linger is the share of rests in which he stays on his
# seat long afterwards (away from the keyboard), the only way a rest outlasts the reset of the room.
REST_HABITS = {"low": dict(rest_target=1.0, rest_all=True, rest_patience_s=120.0, p_linger=0.005),
               "mid": dict(rest_target=1.0, rest_patience_s=60.0, p_linger=0.01),
               "high": dict(rest_target=1.0, rest_patience_s=20.0, p_linger=0.02)}


def rest_human(name, factor, **over):
    """The human `name` (low, mid, high) with the rest command at RestFactor `factor`; over replaces any field."""
    return replace(HUMAN_MODELS[name], rest_cmd=True, rest_factor=float(factor), rest_to=0.0, **{**REST_HABITS[name], **over})


REST_FACTORS = (None, 3, 5, 8)      # None = no rest command: the models of docs/BALANCE_HUMAN.md, unchanged


def rest_models(factor, runs, over=None):
    """[(name, model, runs)] for the three humans at this RestFactor (None: the old ones). Low and high get half the runs."""
    out = []
    for name, n in (("low", runs // 2), ("mid", runs), ("high", runs // 2)):
        out.append((name, HUMAN_MODELS[name] if factor is None else rest_human(name, factor, **(over or {})), n))
    return out


def factor_label(f):
    return "no rest command" if f is None else f"RestFactor {f}"


def dump_json(record):
    path = os.environ.get("REST_JSON")
    if path:
        import json
        with open(path, "a") as fh:
            fh.write(json.dumps(record) + "\n")


def ends_text(r, n=2):
    ends = sorted(r["ends"].items(), key=lambda kv: -kv[1])[:n]
    return ", ".join(f"{o} {room.replace('Overseer ', '')} {v * 100:.0f}%" for (o, room), v in ends) or "-"


def rest_grid(runs=3000, factors=REST_FACTORS):
    """The required grid: kill% by calling, level, potions and gear, for the three humans, at each RestFactor. Also mean potions used, the
    minutes a successful run takes (fighting / walking and looting / resting) and where the runs end."""
    for f in factors:
        for gear in ("starting", "best"):
            print("=" * 130)
            print(f"RG. {factor_label(f)}; {gear} gear; Overseer kill% from the cellar entrance, current game. 'pots' = potions used in the whole run (the 4-potion pack at the Hall"
                  f" plus the spare supply on the way); 'min f/w/r' = mean minutes of a SUCCESSFUL run: fighting / walking and looting / resting. {runs} runs mid, {runs // 2} low and high.")
            print("=" * 130)
            for c in ARCHS:
                for lvl in (4, 5):
                    for pot in (2, 4):
                        cells, line = {}, f"{c:<11}{lvl:>2}{pot:>3}  "
                        for name, m, n in rest_models(f, runs):
                            r = approach_stats(c, lvl, pot, m, n, gear, True)
                            cells[name] = r
                            mm = r["minutes"]
                            line += f"| {name:<4}{pct(r['kill']):>4}%  {r['potions']:.1f} pots  {mm['fight']:4.1f}/{mm['walk']:3.1f}/{mm['rest']:4.1f} "
                            dump_json(dict(kind="grid", factor=f, gear=gear, calling=c, level=lvl, potions=pot, model=name, runs=n, kill=r["kill"],
                                           pots=r["potions"], before=r["before"], hall=r["hall"], chamber=r["chamber"], door=r["door"], refills=r["refills"],
                                           minutes=r["minutes"], ends={f"{o} {rm}": v for (o, rm), v in r["ends"].items()}))
                        print(line + f"| mid ends: {ends_text(cells['mid'])}", flush=True)


def rest_sens(runs=1500):
    """Sensitivity of the middle human (level 5, 4 potions, starting gear) to the assumptions that decide the answer."""
    print("=" * 130)
    print(f"RS. Level 5, 4 potions at the Hall, starting gear, middle human: kill% by calling; {runs} runs a cell.")
    print("=" * 130)
    print(f"{'':<58}" + "".join(f"{c[:5]:>7}" for c in ARCHS) + f"{'mean':>6}{'spread':>7}{'rest min':>9}{'refills':>8}")

    def row(label, model, gear="starting", n=runs):
        rs = [approach_stats(c, 5, 4, model, n, gear, True) for c in ARCHS]
        v = [r["kill"] * 100 for r in rs]
        print(f"{label:<58}" + "".join(f"{x:7.0f}" for x in v) + f"{sum(v) / 5:6.0f}{max(v) - min(v):7.0f}"
              f"{sum(r['minutes']['rest'] for r in rs) / 5:9.1f}{sum(r['refills'] for r in rs) / 5:8.2f}", flush=True)
        dump_json(dict(kind="sens", label=label, kills=v))

    row("no rest command (the middle human of BALANCE_HUMAN.md)", HUMAN_MID)
    print("-- patience (seconds seated at most), sits until Vitality is full")
    for f in (3, 5, 8):
        for pat in (20, 30, 60, 120, 240):
            row(f"RestFactor {f}, patience {pat} s", rest_human("mid", f, rest_patience_s=float(pat)))
    print("-- the target (Vitality share at which he gets up), RestFactor 5, patience 60 s")
    for tgt in (0.5, 0.65, 0.8, 0.9, 1.0):
        row(f"target {tgt:.0%}", rest_human("mid", 5, rest_target=tgt))
    print("-- sitting on after he is done (p_linger), RestFactor 5; RestSurprise 2 s")
    for pl in (0.0, 0.01, 0.05, 0.2):
        row(f"p_linger {pl:.0%} (1 to 10 minutes extra)", rest_human("mid", 5, p_linger=pl))
    print("-- RestSurprise (seconds added to the first action of the player attacked while seated), p_linger 20%, RestFactor 5")
    for sp in (0.0, 2.0, 5.0, 10.0):
        row(f"RestSurprise {sp:g} s", rest_human("mid", 5, p_linger=0.2, rest_surprise_s=sp))
    print("-- the Adventurer's Survival Training (x1.5 out of combat; not in any other table)")
    global SURVIVAL
    SURVIVAL = True
    try:
        row("RestFactor 5, Survival Training on", rest_human("mid", 5))
        row("no rest command, Survival Training on", HUMAN_MID)
    finally:
        SURVIVAL = False


ARMOUR_ASSUMED = 3   # Leather Jerkin 2 (50 gold) + Leather Cap 1 (25 gold); the Mage, who cannot wear the jerkin, has Padded Jerkin 1 (20) + Cap 1 = 2.
                     # Gold: 4 potions (60) + this (75, or 45) = 135 gold at most. A first pass through the cellars brings about 10 (Storage Chamber) + 16
                     # (Guard Post) + 29 (Barracks) + 34 (Hall) + 45 (Hidden Cache) = 134 gold and the character starts with 10 to 20; the 1000 experience
                     # of level 5 take more fights than one pass (812 experience), so about 190 to 230 gold are in hand by then: enough, with some left.


def perfect_table2(runs=3000, seed=20260101):
    """The table 2 of the simulator and of docs/ATTRIBUTES.md section 8: a level 4 character, best weapon, +5 Vitality trinket, 2 potions, the Overseer
    alone from full Vitality. Returns the kill% of the five callings."""
    saved = random.getstate()
    random.seed(seed)
    out = [boss_stats(n, 4, ARCHS[n].weapons[-1], 2, TRINKET_HP, runs)[0] * 100 for n in ARCHS]
    random.setstate(saved)
    return out


def option_row(label, factor, tweaks, armour, runs, gear_kind="starting", level=5, potions=4, models=("mid", "low", "high"), surv=False):
    """One row of the options tables: kill% by calling for the middle human (runs), and the means of the low and high human (half the runs).
    Run through tweaked(); nothing is applied. armour: points from worn armour (the Mage one less)."""
    rows = {}
    with tweaked(**tweaks):
        for name in models:
            m = HUMAN_MODELS[name] if factor is None else rest_human(name, factor)
            vals, mins = [], []
            for c in ARCHS:
                w, sh, tr, _ = gear_for(c, gear_kind)
                a = armour if c != "mage" or not armour else armour - 1
                r = approach_stats(c, level, potions, m, runs if name == "mid" else runs // 2, (w, sh, tr, a), True)
                vals.append(r["kill"] * 100)
                mins.append((r["minutes"]["fight"], r["minutes"]["walk"], r["minutes"]["rest"]))
            rows[name] = (vals, mins)
    v = rows["mid"][0]
    mm = rows["mid"][1]
    line = (f"{label:<64}" + "".join(f"{x:5.0f}" for x in v) + f"{sum(v) / 5:6.0f}{max(v) - min(v):7.0f}"
            + "".join(f"{sum(rows[n][0]) / 5:6.0f}" if n in rows else f"{'':>6}" for n in ("low", "high"))
            + f"   {sum(m[0] for m in mm) / 5:4.1f}/{sum(m[1] for m in mm) / 5:3.1f}/{sum(m[2] for m in mm) / 5:4.1f}")
    print(line, flush=True)
    dump_json(dict(kind="option", label=label, factor=factor, tweaks=str(tweaks), armour=armour, gear=gear_kind, level=level, potions=potions,
                   mid=rows["mid"][0], low=rows.get("low", ([],))[0], high=rows.get("high", ([],))[0]))
    return v


def option_head(title, runs):
    print("=" * 130)
    print(title + f" ({runs} runs a cell for the middle human, half for low and high)")
    print("=" * 130)
    print(f"{'option':<64}" + "".join(f"{c[:5]:>5}" for c in ARCHS) + f"{'mean':>6}{'spread':>7}{'low':>6}{'high':>6}   min f/w/r (mid)")


def rest_harvest(runs=1500):
    """The worst case for resting where creatures return: a player who skins and bleeds every corpse and takes its loot (the crafting game), so
    that the corpses are gone at once and the next reset (a uniform 0 to 300 s later) brings the creatures back. Middle human, level 5,
    4 potions, starting gear, RestFactor 5, by RestSurprise."""
    print("=" * 130)
    print(f"RH. The corpses are harvested at once (corpse time 10 s instead of 420 to 600 s), middle human, RestFactor 5, level 5, 4 potions, starting gear; {runs} runs a cell")
    print("=" * 130)
    print(f"{'':<46}" + "".join(f"{c[:5]:>7}" for c in ARCHS) + f"{'mean':>6}{'refills':>8}   (refills = fights per run started by creatures that came back while he sat)")
    saved = dict(CORPSE_SECONDS_BY_ROOM)
    try:
        for label, corpse, sp in (("corpses stay (the game as built)", None, 2.0), ("harvested, RestSurprise 0 s", 10, 0.0), ("harvested, RestSurprise 2 s", 10, 2.0),
                                  ("harvested, RestSurprise 5 s", 10, 5.0), ("harvested, RestSurprise 10 s", 10, 10.0)):
            if corpse is not None:
                for k in CORPSE_SECONDS_BY_ROOM:
                    CORPSE_SECONDS_BY_ROOM[k] = corpse
            rs = [approach_stats(c, 5, 4, rest_human("mid", 5, rest_surprise_s=sp), runs, "starting", True) for c in ARCHS]
            v = [r["kill"] * 100 for r in rs]
            print(f"{label:<46}" + "".join(f"{x:7.0f}" for x in v) + f"{sum(v) / 5:6.0f}{sum(r['refills'] for r in rs) / 5:8.2f}", flush=True)
            dump_json(dict(kind="harvest", label=label, kills=v, refills=[r["refills"] for r in rs]))
    finally:
        CORPSE_SECONDS_BY_ROOM.clear()
        CORPSE_SECONDS_BY_ROOM.update(saved)


_M7, _M8 = dict(mage=dict(vitality=(4, 7))), dict(mage=dict(vitality=(4, 8)))          # the Mage's Vitality a level 6 -> 7, 8
_S8, _S9 = dict(scout=dict(vitality=(3, 8))), dict(scout=dict(vitality=(3, 9)))          # the Scout's Vitality a level 7 -> 8, 9
# (label, RestFactor, tweaked() arguments, armour). None is "no rest command". Each is a candidate for the owner to choose from; none is applied.
REST_OPTIONS = [
    ("no rest command (the game as it is)", None, {}, 0),
    ("rest alone, RestFactor 3", 3, {}, 0),
    ("rest alone, RestFactor 4", 4, {}, 0),
    ("rest alone, RestFactor 5", 5, {}, 0),
    ("rest alone, RestFactor 6", 6, {}, 0),
    ("rest alone, RestFactor 8", 8, {}, 0),
    ("A  RestFactor 5, Mage 4 + 7 a level, Scout 3 + 8 a level, Priest Minor Heal 7", 5, dict(archs={**_M7, **_S8}, heal_amount=7), 0),
    ("A' = A with the Priest's Minor Heal 6", 5, dict(archs={**_M7, **_S8}, heal_amount=6), 0),
    ("A- = A without the Priest's change (heal 8)", 5, dict(archs={**_M7, **_S8}), 0),
    ("C  RestFactor 4, Mage 4 + 8, Scout 3 + 9, Priest heal 6", 4, dict(archs={**_M8, **_S9}, heal_amount=6), 0),
    ("D  RestFactor 5, Minor Healing Potion heals 25 (was 20)", 5, dict(potion_heal=25), 0),
    ("E  RestFactor 3, Mage 4 + 8, Scout 3 + 8, Priest heal 6", 3, dict(archs={**_M8, **_S8}, heal_amount=6), 0),
    ("-- the same with armour bought: Leather Jerkin 2 + Cap 1 (Mage: Padded Jerkin 1 + Cap 1)", None, {}, 0),
    ("no rest command + armour", None, {}, ARMOUR_ASSUMED),
    ("rest alone, RestFactor 3 + armour", 3, {}, ARMOUR_ASSUMED),
    ("rest alone, RestFactor 5 + armour", 5, {}, ARMOUR_ASSUMED),
    ("A + armour", 5, dict(archs={**_M7, **_S8}, heal_amount=7), ARMOUR_ASSUMED),
    ("C + armour", 4, dict(archs={**_M8, **_S9}, heal_amount=6), ARMOUR_ASSUMED),
    ("E + armour", 3, dict(archs={**_M8, **_S8}, heal_amount=6), ARMOUR_ASSUMED),
]


def rest_options(runs=3000):
    """Level 5, 4 potions at the Hall, starting gear (the calling's own weapon; armour only where the row says), attributes placed, middle human
    (runs) and low and high (half): what the rest command does alone and with the smallest changes that bring the callings together."""
    option_head("RO. Level 5, 4 potions, starting gear: Overseer kill% by calling, with the `rest` command and the changes tried", runs)
    for label, f, tweaks, armour in REST_OPTIONS:
        if label.startswith("--"):
            print(label)
            continue
        option_row(label, f, tweaks, armour, runs)


def rest_perfect(runs=3000):
    """Does the rest command (and each option) make the Overseer trivial for the perfect player? Table 2 as docs/ATTRIBUTES.md section 8 quotes
    it (level 4, best weapon, +5 Vitality trinket, 2 potions, the Overseer alone from full Vitality: resting cannot change it, he is full), and
    the same character coming from the cellar entrance: the simulator's perfect player (rested before every fight), and a player with perfect
    hands (no typing delay, never skips, drinks at 35%) who carries his resources from room to room, without the rest command and then resting by
    the middle human's patience rule."""
    print("=" * 130)
    print(f"RF. The perfect player, level 4, best gear (best weapon, shield where carried, +5 Vitality trinket), 2 potions; {runs} runs a cell")
    print("=" * 130)
    print(f"{'':<76}" + "".join(f"{c[:5]:>7}" for c in ARCHS) + f"{'mean':>6}")
    zero = replace(HUMAN_ZERO, carry_over=True, pause_s=3.0)       # perfect hands, resources carried over
    rows = [("the game as it is", None, {}), ("A  RestFactor 5, Mage 7, Scout 8, Priest heal 7", 5, dict(archs={**_M7, **_S8}, heal_amount=7)),
            ("C  RestFactor 4, Mage 8, Scout 9, Priest heal 6", 4, dict(archs={**_M8, **_S9}, heal_amount=6)),
            ("potion 35 (the earlier recommendation 2c, for comparison)", None, dict(potion_heal=35, archs=dict(mage=dict(vitality=(4, 8)))))]
    for label, f, tweaks in rows:
        with tweaked(**tweaks):
            t2 = perfect_table2(runs)
            print(f"{'table 2, ' + label:<76}" + "".join(f"{x:7.0f}" for x in t2) + f"{sum(t2) / 5:6.0f}", flush=True)
            dump_json(dict(kind="perfect", label="table 2, " + label, vals=t2))
            for sub, model in (("perfect player from the entrance (rested before every fight)", None),
                               ("perfect hands, resources carried, no rest command", zero),
                               ("perfect hands, resources carried, rests by the middle human's rule", replace(zero, rest_cmd=True, rest_factor=float(f or 5), **{**REST_HABITS["mid"], "p_linger": 0.0}))):
                vals, mins = [], []
                for c in ARCHS:
                    w, sh, tr, _ = gear_for(c, "best")
                    r = approach_stats(c, 4, 2, model, runs // 2, (w, sh, tr, 0), True)
                    vals.append(r["kill"] * 100)
                    mins.append(r["minutes"])
                tm = f"   {sum(m['fight'] for m in mins) / 5:.1f}/{sum(m['walk'] for m in mins) / 5:.1f}/{sum(m['rest'] for m in mins) / 5:.1f} min f/w/r" if model is not None else ""
                print(f"{'    cellar, ' + sub:<76}" + "".join(f"{x:7.0f}" for x in vals) + f"{sum(vals) / 5:6.0f}{tm}", flush=True)
                dump_json(dict(kind="perfect", label=f"{label}: {sub}", vals=vals))


def level_pace(arch_name, factor=1.0, runs=100, seed=11):
    """Minutes of play from level 1 to level 5 (1000 experience) for a player who goes through the cellars as full_run does (first pass in order,
    then farms the hardest cleared room he beats 9 times in 10 until the next room's level), but with time kept. Between fights he sits (or
    stands, factor 1) until all three resources are full, at `factor` times the ordinary rate: the perfect-knowledge pace, as the pace targets
    are. Walking is 2 rooms for a first visit and ROOMS_BETWEEN_KILLS for a farmed room, looting LOOT_SECONDS a goblin. A death costs the way
    back (CORPSE_SECONDS and the rooms from the village). Respawns: a room's creatures are back CORPSE_SECONDS_BY_ROOM plus 0 to 300 s after the
    kill (docs/BALANCE_HUMAN.md "Resting"); a farm fight waits for it, sitting. Returns the mean minutes at which levels 2 to 5 were reached
    (waits included), the mean minutes of fighting, walking, recovering and waiting for respawns to level 5, and the share of runs that got there."""
    arch = ARCHS[arch_name]
    saved = random.getstate()
    random.seed(seed)
    reach = {2: 0.0, 3: 0.0, 4: 0.0, 5: 0.0}
    parts = {"fight": 0.0, "walk": 0.0, "recover": 0.0, "wait": 0.0}
    done = 0
    depth = {name: i + 1 for i, (name, _) in enumerate(ROOMS)}
    for _ in range(runs):
        xp, gold, level = 0, arch.gold, 1
        stage, potions, trinket = 0, arch.potions, 0
        shield = arch.shield and arch_name == "warrior"
        buy_shield = arch.shield and arch_name != "warrior"
        clock = 0.0
        got = {}
        tt = {"fight": 0.0, "walk": 0.0, "recover": 0.0, "wait": 0.0}
        back = {}          # room -> the clock at which its creatures are back
        cleared = []
        farm = 0

        def advance(kind, seconds):
            nonlocal clock
            tt[kind] += seconds
            clock += seconds

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

        def attempt(name, creatures, hard, first):
            nonlocal gold, potions
            for tries in range(10):
                p = make_player(arch, level, arch.weapons[stage], shield, trinket)
                use = potions if hard or tries > 0 else 0
                r = fight(p, [make_monster(c) for c in creatures], use, on_kill=on_kill)
                advance("fight", r["ms"] / 1000.0)
                for lv in range(2, level + 1):
                    got.setdefault(lv, clock)
                potions = potions - use + r["potions"]
                if not r["alive"]:
                    gold = int(gold * 0.9)
                    advance("walk", (depth[name] + 4) * ROOM_SECONDS + CORPSE_SECONDS)
                    continue
                advance("walk", (2 if first else ROOMS_BETWEEN_KILLS) * ROOM_SECONDS + LOOT_SECONDS * sum(1 for c in creatures if KINDS[c].gold[1]))
                _, _, _, st_rate, ma_rate, hp_rate = resources(arch, p.level)
                f = factor * (1.5 if SURVIVAL and arch_name == "adventurer" else 1.0)
                need = max((p.max_hp - p.hp) / (hp_rate * f / 10.0),
                           (p.max_stamina - p.stamina) / (st_rate * f / 10.0) if st_rate else 0.0,
                           (p.max_mana - p.mana) / (ma_rate * f / 10.0) if ma_rate else 0.0)
                advance("recover", max(0.0, need))
                back[name] = clock + CORPSE_SECONDS_BY_ROOM.get(name, 420) + random.random() * RESET_SECONDS
                return True
            return False

        def best_cleared():
            ok = [(room, group) for room, group in cleared if room_stats(arch, group, level, arch.weapons[stage], trinket, runs=40, shield=shield)[0] >= 0.9]
            if not ok:
                return None
            ready = [x for x in ok if back.get(x[0], 0) <= clock]
            return ready[-1] if ready else min(ok, key=lambda x: back.get(x[0], 0))

        failed = False
        for name, creatures in ROOMS:
            while level < min(5, REQUIRED_LEVEL[name]) and cleared and farm < 400:
                best = best_cleared()
                if best is None:
                    break
                farm += 1
                shop()
                if back.get(best[0], 0) > clock:
                    advance("wait", back[best[0]] - clock)
                attempt(best[0], best[1], False, False)
            shop()
            if not attempt(name, creatures, name in ("Goblin Barracks", "Overseer Hall"), True):
                failed = True
                break
            cleared.append((name, creatures))
            if name == "Storage Chamber":
                potions += 1
            if name == "Mushroom Cavern":
                gold += random.randint(30, 60)
                trinket = TRINKET_HP
            if level >= 5:
                break
        while not failed and level < 5 and farm < 400:
            best = best_cleared()
            if best is None:
                break
            farm += 1
            shop()
            if back.get(best[0], 0) > clock:
                advance("wait", back[best[0]] - clock)
            attempt(best[0], best[1], False, False)
        if failed or level < 5:
            continue
        done += 1
        for lv in reach:
            reach[lv] += got.get(lv, clock)
        for k in parts:
            parts[k] += tt[k]
    random.setstate(saved)
    n = max(1, done)
    return dict(reach={lv: v / n / 60.0 for lv, v in reach.items()}, parts={k: v / n / 60.0 for k, v in parts.items()}, done=done / runs)


def rest_pace(runs=100):
    """Minutes from level 1 to level 5, by calling, without the rest command and at each RestFactor (full rest between fights)."""
    print("=" * 130)
    print(f"RP. Minutes of play to reach levels 2, 3, 4, 5 from the first step (full_run's route and farming, time kept; {runs} runs a cell). Between fights the player waits"
          f" (factor 1) or sits (RestFactor) until all three resources are full.")
    print("    'wait' = idle time until a farmed room's creatures are back (corpses last 420 to 600 s, reset every 300 s); it is the same with and without the rest command.")
    print("=" * 130)
    print(f"{'calling':<12}{'':<22}{'L2':>6}{'L3':>7}{'L4':>7}{'L5':>7}   {'to level 5: fight':>18}{'walk':>7}{'recover':>9}{'wait':>7}{'  done':>7}")
    for c in ARCHS:
        for f in (1, 3, 5, 8):
            r = level_pace(c, f, runs)
            pr, pt = r["reach"], r["parts"]
            print(f"{c:<12}{'no rest command' if f == 1 else 'RestFactor ' + str(f):<22}{pr[2]:6.1f}{pr[3]:7.1f}{pr[4]:7.1f}{pr[5]:7.1f}   {pt['fight']:18.1f}{pt['walk']:7.1f}{pt['recover']:9.1f}{pt['wait']:7.1f}{r['done'] * 100:6.0f}%", flush=True)
            dump_json(dict(kind="pace", calling=c, factor=f, reach=pr, parts=pt, done=r["done"]))


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
    ("4c  = 4b and the Priest's Minor Heal heals 5 (was 8)", dict(kinds={_G: dict(hp=32, dmg=(2, 4)), _O: dict(hp=100, dmg=(3, 6))}, summon=False, archs=MAGE_VIT, heal_amount=5), 0),
    ("5   one Elite Guard, no Summon", dict(summon=False, hall=[_G]), 0),
]


def stage_weapon(arch, level):
    idx = 0 if level <= 2 else (1 if level == 3 else len(arch.weapons) - 1)
    return arch.weapons[min(idx, len(arch.weapons) - 1)]


# ---- skill ranks (docs/specs/skill-ranks.md; python3 tests/balance_sim.py ranks [runs] [part ...])

@contextmanager
def ranked(mode=None, double=None, table=None, prep=None, lockout_s=None, ignore_cap=None, selfhalf=None):
    """Temporarily change how ranks are held (RANK_MODE), Double Strike on or off, a skill's rank table (skill -> dict(base=..., rows=...)
    merged over the current one), the Minor Heal's Preparation and the heal lockout (option c). Nothing here is applied to the game."""
    global RANK_MODE, DOUBLE_STRIKE, HEAL_LOCKOUT_MS, RANK_IGNORE_CAP, SELF_HEAL_HALF
    saved_cap, saved_half = RANK_IGNORE_CAP, SELF_HEAL_HALF
    saved_tables = {k: dict(base=dict(v["base"]), rows={r: dict(x) for r, x in v["rows"].items()}, **({"max": v["max"]} if "max" in v else {}))
                    for k, v in SKILL_RANKS.items()}
    saved = (RANK_MODE, DOUBLE_STRIKE, HEAL_LOCKOUT_MS, saved_tables, dict(SKILL_PREP))
    try:
        if mode is not None:
            RANK_MODE = mode
        if double is not None:
            DOUBLE_STRIKE = double
        for sk, t in (table or {}).items():
            SKILL_RANKS[sk]["base"].update(t.get("base", {}))
            if "rows" in t:
                SKILL_RANKS[sk]["rows"] = {r: dict(x) for r, x in t["rows"].items()}
        if ignore_cap is not None:
            RANK_IGNORE_CAP = ignore_cap
        if selfhalf is not None:
            SELF_HEAL_HALF = selfhalf
        if prep is not None:
            SKILL_PREP["minor_heal"] = prep
        if lockout_s is not None:
            HEAL_LOCKOUT_MS = round(lockout_s * 1000)
        yield
    finally:
        RANK_MODE, DOUBLE_STRIKE, HEAL_LOCKOUT_MS = saved[0], saved[1], saved[2]
        RANK_IGNORE_CAP, SELF_HEAL_HALF = saved_cap, saved_half
        SKILL_RANKS.clear()
        SKILL_RANKS.update(saved[3])
        SKILL_PREP.clear()
        SKILL_PREP.update(saved[4])


def l5_kills(callings=None, runs=3000, seed=1, force_open=False):
    """Kill% against the Overseer from the cellar entrance: level 5, starting gear, 4 potions, points placed, middle human with the rest
    command at RestFactor 5 (the owner's standing target). One number per calling."""
    m = rest_human("mid", 5)
    return {c: approach_stats(c, 5, 4, m, runs, "starting", True, seed, force_open=force_open)["kill"] * 100 for c in (callings or ARCHS)}


def band_line(label, v, note=""):
    vals = [v[c] for c in ARCHS]
    mean = sum(vals) / 5
    off = {c: v[c] - mean for c in ARCHS}
    print(f"{label:<66}" + "".join(f"{x:6.0f}" for x in vals) + f"{mean:6.1f}{max(vals) - min(vals):7.0f}   priest {off['priest']:+5.1f}, adv. {off['adventurer']:+5.1f}"
          + (f"   {note}" if note else ""), flush=True)
    dump_json(dict(kind="ranks", label=label, kills=vals))
    return vals


def band_head(title, runs):
    print("=" * 140)
    print(title + f" ({runs} runs a cell)")
    print("=" * 140)
    print(f"{'':<66}" + "".join(f"{c[:5]:>6}" for c in ARCHS) + f"{'mean':>6}{'spread':>7}   distance from the mean")


def heal_table(h, costs=(25, 32, 40), rows=None):
    """A Minor Heal table: rank I heal h, later ranks by the default step from it (or the given rows), with these Focus costs."""
    r = rows or {}
    return {"minor_heal": dict(base=dict(heal=h, focus=costs[0]),
                               rows={2: dict(focus=costs[1], **r.get(2, {})), 3: dict(focus=costs[2], **r.get(3, {}))})}


SPEC_HEAL_ROWS = {2: dict(heal=38), 3: dict(heal=60)}


def ranks_level5(runs=3000):
    band_head("RK1. Level 5 and skill ranks: Overseer kill% from the cellar entrance, starting gear, 4 potions, points placed, middle human, RestFactor 5", runs)
    with ranked(mode="none", double=False, table=heal_table(8), lockout_s=0):
        band_line("before ranks (rank I, no Double Strike, Minor Heal 8)", l5_kills(runs=runs))
    with ranked(mode="none"):
        base = l5_kills(runs=runs)
        band_line("rank I, the Adventurer with Double Strike I (2 x 60%)", base)
    with ranked(mode="cap"):
        band_line("every calling holds rank II (bought at level 5; Minor Heal as shipped)", l5_kills(runs=runs))
    for c in ARCHS:
        with ranked(mode="cap"):
            one = dict(base)
            one[c] = l5_kills([c], runs)[c]
        band_line(f"only the {c} holds rank II (the others rank I)", one)


def heal_search(rank_mode, others, runs, lo=6, hi=24, costs=(25, 32, 40), **extra):
    """The largest rank I heal whose Priest stays within 5 points of the five-calling mean (the other four given). Returns (h, priest%)."""
    best = None
    for h in range(lo, hi + 1):
        with ranked(mode=rank_mode, table=heal_table(h, costs), **{"lockout_s": 0, **extra}):
            pr = l5_kills(["priest"], runs)["priest"]
        mean = (sum(others.values()) + pr) / 5
        ok = abs(pr - mean) <= 5
        print(f"    heal {h:>2} [{'rank I' if rank_mode == 'none' else 'rank II'}]: priest {pr:5.1f}, mean {mean:5.1f}, {pr - mean:+5.1f} {'in band' if ok else ''}", flush=True)
        dump_json(dict(kind="heal_search", mode=rank_mode, heal=h, priest=pr, mean=mean))
        if ok:
            best = (h, round(pr, 1))
        elif best is not None and pr - mean > 5:
            break
    return best


def ranks_heal(runs=3000):
    """The Minor Heal options for the owner: (a) the spec's table, (b) the largest rank I heal that keeps the Priest within 5 points of the
    mean with the spec's costs, (c) a big heal paid for another way, tuned to the same band. Two columns of truth: everyone at rank I, and
    everyone at rank II (bought at level 5)."""
    others = {}
    for mode in ("none", "cap"):
        with ranked(mode=mode):
            others[mode] = l5_kills([c for c in ARCHS if c != "priest"], runs)
    band_head("RK2. Minor Heal options, level 5 Overseer as RK1. [rank I] = nobody has bought a rank; [rank II] = every calling holds rank II", runs)

    def row(label, mode, **kw):
        kw.setdefault("lockout_s", 0)   # the options of the provisional table had no wait; (c3) and the shipped row name theirs
        with ranked(mode=mode, **kw):
            pr = l5_kills(["priest"], runs)["priest"]
        return band_line(label + f"  [{'rank I' if mode == 'none' else 'rank II'}]", {**others[mode], "priest": pr})

    for mode in ("none", "cap"):
        row("(shipped) 20 / 38 / 60 for 25 / 32 / 40 Focus, no heal for 15 s after a heal", mode, table=heal_table(20, rows=SPEC_HEAL_ROWS), lockout_s=HEAL_WAIT_S)
        row("(a) spec: 20 / 38 / 60 for 25 / 32 / 40 Focus, no wait", mode, table=heal_table(20, rows=SPEC_HEAL_ROWS))
        row("    8 (today), later ranks by the default step, spec costs", mode, table=heal_table(8))
    print("  (b) search: rank I heal h, II and III by the default step from h, costs 25 / 32 / 40")
    b1 = heal_search("none", others["none"], runs)
    b2 = heal_search("cap", others["cap"], runs)
    print(f"  (b) largest heal in band: measured at rank I {b1}, measured at rank II {b2}", flush=True)
    if os.environ.get("RANKS_HEAL_C", "1") == "1":
        print("  (c) the spec's 20 / 38 / 60 heal, paid for another way:")
        for cost in ((40, 50, 62), (50, 64, 80), (60, 75, 95), (75, 95, 120)):
            for mode in ("none", "cap"):
                row(f"(c1) 20 / 38 / 60 for {cost[0]} / {cost[1]} / {cost[2]} Focus", mode, table=heal_table(20, cost, SPEC_HEAL_ROWS))
        for prep in (1.0, 2.0, 3.0):
            for mode in ("none", "cap"):
                row(f"(c2) 20 / 38 / 60, Preparation {prep:g} ({prep * 2:g} s wind-up)", mode, table=heal_table(20, rows=SPEC_HEAL_ROWS), prep=prep)
        for lock in (15, 30, 45, 60):
            for mode in ("none", "cap"):
                row(f"(c3) 20 / 38 / 60, no heal for {lock} s after a heal", mode, table=heal_table(20, rows=SPEC_HEAL_ROWS), lockout_s=lock)
    return b1, b2


def ranks_double(runs=3000):
    """Double Strike: the spec's numbers and others, Adventurer against the mean (rank I and rank II)."""
    with ranked(mode="none"):
        base1 = l5_kills([c for c in ARCHS if c != "adventurer"], runs)
    with ranked(mode="cap"):
        base2 = l5_kills([c for c in ARCHS if c != "adventurer"], runs)
    band_head("RK3. Double Strike: the Adventurer against the other callings, level 5 Overseer as RK1", runs)
    with ranked(mode="none", double=False):
        band_line("no Double Strike (Minor Heal as shipped) [rank I]", {**base1, "adventurer": l5_kills(["adventurer"], runs)["adventurer"]})
    for label, base, rows, mode in (
            ("shipped: 2 x 60%, 35 Endurance [rank I]", dict(factor=0.6, endurance=35), None, "none"),
            ("shipped: rank II 2 x 75%, 40 Endurance [rank II]", dict(factor=0.6, endurance=35), None, "cap"),
            ("(the first spec) 2 x 60%, 20 Endurance [rank I]", dict(factor=0.6, endurance=20), None, "none"),
            ("(the first spec) rank II 2 x 75%, 23 [rank II]", dict(factor=0.6, endurance=20), None, "cap"),
            ("2 x 50%, 20 Endurance [rank I]", dict(factor=0.5, endurance=20), None, "none"),
            ("2 x 45%, 20 Endurance [rank I]", dict(factor=0.45, endurance=20), None, "none"),
            ("2 x 40%, 20 Endurance [rank I]", dict(factor=0.4, endurance=20), None, "none"),
            ("2 x 60%, 35 Endurance [rank I]", dict(factor=0.6, endurance=35), None, "none"),
            ("2 x 60%, 50 Endurance [rank I]", dict(factor=0.6, endurance=50), None, "none"),
            ("2 x 50%, rank II 2 x 60% [rank II]", dict(factor=0.5, endurance=20), {2: dict(factor=0.6), 3: dict(hits=3, factor=0.5), 4: dict(hits=4, factor=0.45)}, "cap"),
            ("2 x 45%, rank II 2 x 55% [rank II]", dict(factor=0.45, endurance=20), {2: dict(factor=0.55), 3: dict(hits=3, factor=0.45), 4: dict(hits=4, factor=0.4)}, "cap")):
        t = {"double_strike": dict(base=base, **({"rows": rows} if rows else {}))}
        with ranked(mode=mode, table=t):
            a = l5_kills(["adventurer"], runs)["adventurer"]
        band_line(label, {**(base1 if mode == "none" else base2), "adventurer": a})


LEVEL10 = [k.id for k in CORRIDOR if not k.boss and k.band[0] <= 10 <= k.band[1]]
LEVEL5C = [k.id for k in CORRIDOR if not k.boss and k.band[0] <= 5 <= k.band[1]]


LEVEL10B = [k.id for k in CORRIDOR if k.boss and k.band[0] <= 10 <= k.band[1]]


def gap_at(level, keys, runs, potions=0):
    """Mean win% of each calling (perfect player, corridor gear, one creature at a time from full) over these corridor creatures."""
    random.seed(1000 + level)
    return {c: sum(cfight(c, level, k, potions, runs)["win"] for k in keys) / len(keys) * 100 for c in ARCHS}


def ranks_gap(runs=400):
    print("=" * 140)
    print("RK4. The gap between callings at levels 5 and 10 (the spec's balance requirement: not wider than today). Perfect player, corridor gear")
    print(f"    (docs/CORRIDOR_BALANCE.md), one creature at a time from full, win% averaged over the ordinary creatures whose band holds the level ({runs} fights a creature)")
    print("=" * 140)
    print(f"{'':<66}" + "".join(f"{c[:5]:>6}" for c in ARCHS) + f"{'mean':>6}{'spread':>7}")
    for level, keys, potions in ((5, LEVEL5C, 0), (10, LEVEL10, 0), (10, LEVEL10B, CORRIDOR_POTIONS), (5, ["overseer"], 2)):
        print(f"-- level {level}: {', '.join(keys)}" + (f" ({potions} potions)" if potions else "")
              + (": the Goblin Overseer alone, best Hollow gear, the +5 Vitality trinket (table 2 of the default output)" if keys == ["overseer"] else ""))
        for label, kw in (("today (rank I, no Double Strike, Minor Heal 8)", dict(mode="none", double=False, table=heal_table(8), lockout_s=0)), ("rank I, with Double Strike I", dict(mode="none")),
                          (f"every skill at the rank level {level} allows ({'II' if level == 5 else 'III'})", dict(mode="cap"))):
            with ranked(**kw):
                if keys == ["overseer"]:
                    random.seed(20260101)
                    v = {c: boss_stats(c, 5, ARCHS[c].weapons[-1], 2, TRINKET_HP, runs * 3)[0] * 100 for c in ARCHS}
                else:
                    v = gap_at(level, keys, runs, potions)
            vals = [v[c] for c in ARCHS]
            print(f"{label:<66}" + "".join(f"{x:6.0f}" for x in vals) + f"{sum(vals) / 5:6.1f}{max(vals) - min(vals):7.0f}", flush=True)
            dump_json(dict(kind="gap", level=level, label=label, wins=vals))


# The best staff there is at each level (none is sold or found beyond the Hollow): name, damage, to hit, Insight it gives.
STAFFS = {1: ("Apprentice's Staff", (7, 11), 1, 0), 5: ("Staff of the Hollow Oak", (10, 14), 1, 2),
          10: ("Staff of the Hollow Oak", (10, 14), 1, 2), 15: ("Staff of the Hollow Oak", (10, 14), 1, 2)}
# The most armour on a creature of about that level (level 1: the level 2 Goblin Guard's 2, no level 1 creature wears any; level 15: none exists, extrapolated).
ARMOURED = {1: 2, 5: 3, 10: 5, 15: 6}


def ranks_bolt():
    """The Mage's bolt at each rank against the Mage's best staff of that level, per action and per second, against an armoured and an
    unarmoured creature of that level. Expected values (no randomness): hit chance x mean damage x (1 + crit chance x 0.5); the bolt has no
    hit roll (no creature of those levels dodges but the corridor's three, ignored here)."""
    print("=" * 140)
    print("RK5. Arcane Bolt at each rank against the Mage's best staff of that level (the spec: the bolt must beat the staff per action against an armoured")
    print("    creature of the level, and not an unarmoured one by more than a small margin). Mage with points in Insight, the staff in hand.")
    print("=" * 140)
    print(f"{'level':>5} {'rank':>4} {'bolt':>6} {'Focus':>6}  {'staff':<24}{'armour':>7}{'bolt/act':>9}{'staff/act':>10}{'ratio':>7}   {'unarmoured staff/act':>21}{'ratio':>7}   {'bolt/s':>7}{'staff/s':>8}")
    a = ARCHS["mage"]
    for level, rank in ((1, 1), (5, 2), (10, 3), (15, 4)):
        name, (lo, hi), hit, ins = STAFFS[level]
        attrs = list(attrs_at(a, level))
        attrs[ATTR.index("insight")] += ins
        attrs = tuple(attrs)
        sp = stat(attrs, "spell_power")
        blo, bhi = rank_int("arcane_bolt", "min", rank), rank_int("arcane_bolt", "max", rank)
        bolt = sum(max(1, rnd(d * (1 + sp / 100.0))) for d in range(blo, bhi + 1)) / (bhi - blo + 1)
        atk = rnd(a.rate * level) + hit + stat(attrs, "precision")
        crit = 1 + (5 + stat(attrs, "crit")) / 100 * 0.5
        mean = (lo + hi) / 2 * (1 + stat(attrs, "physical_power") / 100.0) * crit
        armoured, bare = (hit_chance(atk, arm, max(0, level - 1)) * mean for arm in (ARMOURED[level], 0))
        cost = rnd(rank_int("arcane_bolt", "focus", rank) * (100 + a.spell_cost_pct) / 100.0)
        bolt_s, staff_s = bolt / ((a.reaction + BOLT_REC) * 2), armoured / ((a.reaction + 0.5 + 1.0) * 2)
        print(f"{level:>5} {['', 'I', 'II', 'III', 'IV'][rank]:>4} {blo:>2}-{bhi:<3} {cost:>6}  {name:<24}{ARMOURED[level]:>7}{bolt:9.1f}{armoured:10.1f}{bolt / armoured:7.2f}"
              f"   {bare:21.1f}{bolt / bare:7.2f}   {bolt_s:7.2f}{staff_s:8.2f}")


def ranks_focus(runs=1000):
    """The spec: a Priest's Focus runs out before a creature of the Priest's own level does, so the heal cannot make solo fights riskless. The
    Priest (corridor gear, perfect player, no potions) against a creature of its own level that never dies: how long until death, and how many
    heals a full Focus pool pays for."""
    print("=" * 140)
    print(f"RK6. The Priest's Focus against a creature of its own level that does not die ({runs} fights a cell, perfect player, corridor gear, no potions)")
    print("=" * 140)
    for level, key in ((5, "stray_wolf"), (10, "fallen_knight")):
        for mode in ("none", "cap"):
            with ranked(mode=mode):
                died = 0
                t_dead = 0.0
                for _ in range(runs):
                    p = cplayer("priest", level)
                    e = make_monster(key)
                    e.hp = e.max_hp = 10 ** 7
                    r = fight(p, [e], 0)
                    t_dead += r["ms"] / 1000.0
                    died += not r["alive"]
                rk = default_ranks("priest", level)["minor_heal"]
                heal, cost = rank_int("minor_heal", "heal", rk), rank_int("minor_heal", "focus", rk)
                p = cplayer("priest", level)
                print(f"  level {level:>2} vs {key:<14} Minor Heal {['', 'I', 'II', 'III'][rk]:<3}: {heal} Vitality (before Healing Power and Divine Knowledge) for {cost} Focus; full Focus"
                      f" {p.max_mana} = {p.max_mana // cost} heals; dead in {t_dead / runs:4.0f} s on average, {died * 100 / runs:.0f}% of fights end in the Priest's death", flush=True)
                dump_json(dict(kind="focus", level=level, mode=mode, died=died / runs, seconds=t_dead / runs))


def ranks_climb(runs=300):
    """Goblins killed before level 5 in the whole run of table 3 (for Arcane Bolt II's ten goblins)."""
    print("=" * 140)
    print(f"RK7. Goblins slain on the way to level 5 (table 3's whole run, {runs} runs a calling): does a Mage have ten for Arcane Bolt II at level 5?")
    print("=" * 140)
    for c in ARCHS:
        FULL_RUN_GOBLINS.clear()
        full_run(c, runs)
        g = sorted(FULL_RUN_GOBLINS)
        print(f"  {c:<11} median {g[len(g) // 2]:>3}, 10th percentile {g[len(g) // 10]:>3}, share with 10 or more {sum(x >= 10 for x in g) * 100 / len(g):4.0f}%", flush=True)


def aim_line(label, five, archer, note=""):
    vals = [five[c] for c in ARCHS]
    mean = sum(vals) / 5
    print(f"{label:<70}" + "".join(f"{x:6.0f}" for x in vals) + f"{archer:7.0f}{mean:7.1f}{max(vals) - min(vals):7.0f}   archer {archer - mean:+5.1f}, melee scout {five['scout'] - mean:+5.1f}"
          + (f"   {note}" if note else ""), flush=True)
    dump_json(dict(kind="aim", label=label, kills=vals, archer=archer))


def aim_head(title, runs):
    print("=" * 150)
    print(title + f" ({runs} runs a cell)")
    print("=" * 150)
    print(f"{'':<70}" + "".join(f"{c[:5]:>6}" for c in ARCHS) + f"{'archer':>7}{'mean5':>7}{'spread':>7}   distance from the mean of the five callings")


def ranks_wait(runs=3000):
    """Minor Heal's wait: the Priest against the mean of the five callings (level 5 Overseer as RK1), for waits of 0 to 90 s, rank I and rank II. The
    wait is counted in real time across fights (the walking and resting between rooms count), as the game keeps it."""
    others = {}
    for mode in ("none", "cap"):
        with ranked(mode=mode):
            others[mode] = l5_kills([c for c in ARCHS if c != "priest"], runs)
    band_head("WAIT. Minor Heal 20 / 38 / 60 for 25 / 32 / 40 Focus with a wait after each cast, level 5 Overseer as RK1", runs)
    for wait in (0, 15, 30, 45, 60, 90):
        for mode in ("none", "cap"):
            with ranked(mode=mode, lockout_s=wait):
                pr = l5_kills(["priest"], runs)["priest"]
            band_line(f"wait {wait:>2} s  [{'rank I' if mode == 'none' else 'rank II'}]" + ("  (shipped: 15)" if wait == HEAL_WAIT_S else ""), {**others[mode], "priest": pr})


def ranks_aim(runs=3000):
    """The archer Scout beside the five callings (the melee Scout is the Scout column: Precision Strike, the Hunting Bow it starts with, no Aimed
    Shot). Standard test and the perfect player against the Overseer alone. The cellar's creatures are all aggressive and engage on arrival, so the
    archer opens nothing there (`opened` below); the rows marked HYPOTHETICAL give it the opening in every fight to show what the rule is worth."""
    rows = (("rank I (nobody has bought a rank)", "none"), ("everyone holds rank II (bought at level 5)", "cap"))
    aim_head("AIM1. Standard test: level 5, starting gear, 4 potions, points placed, middle human, RestFactor 5, Overseer from the cellar entrance", runs)
    for label, mode in rows:
        with ranked(mode=mode):
            five = l5_kills(runs=runs)
            archer = l5_kills([ARCHER], runs)[ARCHER]
        aim_line(label, five, archer)
        with ranked(mode=mode):
            st = approach_stats(ARCHER, 5, 4, rest_human("mid", 5), max(300, runs // 10), "starting", True, 1)
        print(f"    the archer opened {st['opened']:.2f} of {st['fights']:.1f} fights a run on the way (every creature on the way is aggressive and starts its fight)", flush=True)
    for label, mode in rows:
        with ranked(mode=mode):
            five = l5_kills(runs=runs)
            archer = l5_kills([ARCHER], runs, force_open=True)[ARCHER]
        aim_line("HYPOTHETICAL, the archer opens every fight: " + label, five, archer)
    print()
    aim_head("AIM2. The perfect player against the Overseer alone: level 4, 2 potions, best gear the Hollow sells (Yew Longbow for the archer), +5 Vitality trinket", runs)
    for label, mode in (("rank I", "none"), ("everyone holds rank II (not buyable at level 4: asked for)", 2)):
        with ranked(mode=mode, ignore_cap=True):
            random.seed(20260101)
            five = {c: boss_stats(c, 4, ARCHS[c].weapons[-1], 2, TRINKET_HP, runs)[0] * 100 for c in ARCHS}
            archer = boss_stats(ARCHER, 4, "yew longbow", 2, TRINKET_HP, runs)[0] * 100
            hyp = boss_stats(ARCHER, 4, "yew longbow", 2, TRINKET_HP, runs, opens=True)[0] * 100
        aim_line(label + " (the Overseer is aggressive and starts the fight)", five, archer)
        aim_line("HYPOTHETICAL, the archer opens: " + label, five, hyp)


def ranks_aimfix(runs=3000):
    """What would bring the archer Scout within 5 points of the mean of the five callings (standard test, rank I and everyone at rank II)? One
    change at a time to Aimed Shot, each against the unchanged five. Nothing here is applied to the game."""
    global AIM_PREP
    mean, base = {}, {}
    for mode in ("none", "cap"):
        with ranked(mode=mode):
            five = l5_kills(runs=runs)
        mean[mode] = sum(five.values()) / 5
    print("=" * 150)
    print(f"AIMFIX. Archer Scout against the mean of the five callings, standard test ({runs} runs a cell): mean {mean['none']:.1f} at rank I, {mean['cap']:.1f} with everyone at rank II")
    print("=" * 150)
    print(f"{'Aimed Shot, one change at a time':<62}{'rank I':>8}{'vs mean':>9}{'rank II':>9}{'vs mean':>9}")
    variants = [
        ("as shipped: x1.75 / x2.0, +3 to hit, 30 Endurance, wind-up 2 s", {}, {}, 1.0),
        ("damage x1.5 at I, x1.75 at II", dict(factor=1.5), {2: dict(factor=1.75)}, 1.0),
        ("damage x1.4 at I, x1.6 at II", dict(factor=1.4), {2: dict(factor=1.6)}, 1.0),
        ("damage x1.25 at I, x1.5 at II", dict(factor=1.25), {2: dict(factor=1.5)}, 1.0),
        ("damage x1.0 (a plain shot) at both ranks", dict(factor=1.0), {2: dict(factor=1.0)}, 1.0),
        ("Endurance 40 at I (II 46)", dict(endurance=40), {}, 1.0),
        ("Endurance 50 at I (II 58)", dict(endurance=50), {}, 1.0),
        ("no bonus to hit (+0)", dict(tohit=0), {}, 1.0),
        ("wind-up 3 s (Preparation 1.5)", {}, {}, 1.5),
        ("wind-up 4 s (Preparation 2.0)", {}, {}, 2.0),
    ]
    for label, base_over, rows, prep in variants:
        got = {}
        for mode in ("none", "cap"):
            saved_prep = AIM_PREP
            AIM_PREP = prep
            try:
                table = {"aimed_shot": dict(base=base_over)}
                if rows:
                    table["aimed_shot"]["rows"] = {**{r: dict(x) for r, x in SKILL_RANKS["aimed_shot"]["rows"].items()}, **{r: {**SKILL_RANKS["aimed_shot"]["rows"].get(r, {}), **x} for r, x in rows.items()}}
                with ranked(mode=mode, table=table):
                    got[mode] = l5_kills([ARCHER], runs)[ARCHER]
            finally:
                AIM_PREP = saved_prep
        print(f"{label:<62}{got['none']:8.1f}{got['none'] - mean['none']:+9.1f}{got['cap']:9.1f}{got['cap'] - mean['cap']:+9.1f}", flush=True)
        dump_json(dict(kind="aimfix", label=label, rank1=got["none"], rank2=got["cap"], mean1=mean["none"], mean2=mean["cap"]))


def ranks_healcost(runs=3000):
    """The owner's open decision on the large heal: the Priest against the mean of the other four, standard test, for other Focus costs and smaller
    heals, with the 15 s wait. Also how many heals a level 5 Priest's full Focus pays for. Nothing here is applied to the game."""
    others = {}
    for mode in ("none", "cap"):
        with ranked(mode=mode):
            others[mode] = l5_kills([c for c in ARCHS if c != "priest"], runs)
    mana = resources(ARCHS["priest"], 5)[2]
    print("=" * 150)
    print(f"HEALCOST. Priest vs the mean of the five callings (standard test, {runs} runs a cell, wait 15 s); level 5 Priest has {mana} Focus. Rank I = nobody bought a rank, rank II = everyone holds II")
    print("=" * 150)
    print(f"{'Minor Heal (Vitality I/II/III; Focus I/II/III)':<56}{'rank I':>8}{'vs mean':>9}{'heals':>7}{'rank II':>9}{'vs mean':>9}{'heals':>7}")
    rows = [("20/38/60, 25/32/40 (as shipped)", 20, (25, 32, 40), SPEC_HEAL_ROWS), ("20/38/60, 35/45/56", 20, (35, 45, 56), SPEC_HEAL_ROWS),
            ("20/38/60, 45/58/72", 20, (45, 58, 72), SPEC_HEAL_ROWS), ("20/38/60, 55/70/88", 20, (55, 70, 88), SPEC_HEAL_ROWS),
            ("20/38/60, 70/90/112", 20, (70, 90, 112), SPEC_HEAL_ROWS), ("12/23/36, 25/32/40", 12, (25, 32, 40), {2: dict(heal=23), 3: dict(heal=36)}),
            ("15/28/45, 25/32/40", 15, (25, 32, 40), {2: dict(heal=28), 3: dict(heal=45)}), ("7/9/11 (the old small heal), 25/32/40", 7, (25, 32, 40), None)]
    for label, h, costs, hr in rows:
        out = {}
        for mode in ("none", "cap"):
            with ranked(mode=mode, table=heal_table(h, costs, hr), lockout_s=HEAL_WAIT_S):
                pr = l5_kills(["priest"], runs)["priest"]
            mean = (sum(others[mode].values()) + pr) / 5
            out[mode] = (pr, pr - mean, mana // costs[0 if mode == "none" else 1])
        print(f"{label:<56}{out['none'][0]:8.1f}{out['none'][1]:+9.1f}{out['none'][2]:7d}{out['cap'][0]:9.1f}{out['cap'][1]:+9.1f}{out['cap'][2]:7d}", flush=True)
        dump_json(dict(kind="healcost", label=label, rank1=out["none"], rank2=out["cap"]))


def ranks_selfheal(runs=3000):
    """The owner's decision of 22 November 2026: Minor Heal gives the caster half (10 / 19 / 30). The standard test for the five callings and the archer
    Scout at rank I and with everyone at rank II (the shipped game), then the self-heal that would leave the Priest within 5 points of the mean of the
    five callings (rank I value s; II and III at the spec's ratio 38/20 and 60/20, rounded). Nothing here is applied to the game."""
    rows = (("rank I (nobody has bought a rank)", "none"), ("everyone holds rank II (bought at level 5)", "cap"))
    aim_head("SELFHEAL. Standard test, Minor Heal on the caster at half (10 / 19 / 30), 15 s wait: level 5, starting gear, 4 potions, points placed, middle human, RestFactor 5, Overseer from the cellar entrance", runs)
    for label, mode in rows:
        with ranked(mode=mode):
            five = l5_kills(runs=runs)
            archer = l5_kills([ARCHER], runs)[ARCHER]
        aim_line(label, five, archer)
    with ranked(mode="none", selfhalf=False):
        five = l5_kills(runs=runs)
    aim_line("for comparison: the self-heal at full (20 / 38 / 60), rank I", five, 0)
    print()
    print("Self-heal that would be level (Priest within 5 points of the mean of the five callings, 15 s wait, Focus 25 / 32 / 40):")
    others = {}
    for mode in ("none", "cap"):
        with ranked(mode=mode):
            others[mode] = l5_kills([c for c in ARCHS if c != "priest"], runs)
    for s_value in (0, 1, 2, 3, 4, 5, 6, 7, 8, 10):
        line = f"  rank I self-heal {s_value:>2}"
        for mode in ("none", "cap"):
            r2, r3 = round(s_value * 38 / 20 + 1e-9), round(s_value * 60 / 20 + 1e-9)
            with ranked(mode=mode, selfhalf=False, table=heal_table(s_value, rows={2: dict(heal=r2), 3: dict(heal=r3)}), lockout_s=HEAL_WAIT_S):
                pr = l5_kills(["priest"], runs)["priest"]
            mean = (sum(others[mode].values()) + pr) / 5
            line += f"   [{'rank I' if mode == 'none' else 'rank II'}: II={r2} priest {pr:5.1f} ({pr - mean:+5.1f})]"
        print(line, flush=True)
        dump_json(dict(kind="selfheal", value=s_value, line=line))


@contextmanager
def sneaking(on=True):
    """Temporarily make the Scout and the archer sneak (SNEAKING). Nothing here is applied to the game."""
    global SNEAKING
    saved = SNEAKING
    SNEAKING = on
    try:
        yield
    finally:
        SNEAKING = saved


def strike_table(r1, r2, r3, r4=None):
    """Precision Strike's table with these ambush factors (the other numbers as shipped)."""
    return {"precision_strike": dict(base=dict(ambush=r1), rows={2: dict(tohit=5, crit=20, ambush=r2), 3: dict(tohit=6, crit=25, ambush=r3),
                                                                  4: dict(tohit=7, crit=30, ambush=r4 if r4 is not None else r3 + 0.25)})}


def sneak_table(room1, room2, room3, notice2=-10, notice3=-20):
    return {"sneak": dict(base=dict(room=room1, notice=0), rows={2: dict(room=room2, notice=notice2), 3: dict(room=room3, notice=notice3)})}


def sneak_sample(calling, runs, mode):
    """The standard test for one Scout (melee `scout` or the ARCHER) sneaking: kill%, fights opened per run, Endurance spent, rooms sneaked, rooms where a creature noticed."""
    m = rest_human("mid", 5)
    with ranked(mode=mode), sneaking():
        r = approach_stats(calling, 5, 4, m, runs, "starting", True, 1)
    return r


def ranks_sneak(runs=3000):
    """Sneaking measured (docs/specs/sneak.md): the standard test for the five callings and the archer, the Scouts sneaking into every room they can afford,
    at rank I and with everyone at rank II; fights opened and Endurance spent; the perfect player against the Overseer alone; then what would bring each Scout
    within 5 points of the mean of the five callings. Nothing here is applied to the game."""
    aim_head("SNEAK. Standard test, the Scouts sneak (melee Scout: Precision Strike from hiding; archer: Aimed Shot from hiding): level 5, starting gear, 4 potions, points placed, middle human, RestFactor 5, Overseer from the cellar entrance", runs)
    seen = {}
    for label, mode in (("rank I (nobody has bought a rank)", "none"), ("everyone holds rank II (bought at level 5)", "cap")):
        with ranked(mode=mode):
            plain5 = l5_kills(runs=runs)
            plain_archer = l5_kills([ARCHER], runs)[ARCHER]
        aim_line(label + ": nobody sneaks (before this change)", plain5, plain_archer)
        with ranked(mode=mode), sneaking():
            five = l5_kills(runs=runs)
            archer = l5_kills([ARCHER], runs)[ARCHER]
        aim_line(label + ": the Scouts sneak", five, archer)
        seen[mode] = (five, archer)
    print()
    print("Fights opened and Endurance spent on sneaking, per run from the cellar entrance (middle human, the whole way, killed or not):")
    for label, mode in (("rank I", "none"), ("rank II", "cap")):
        for name, calling in (("melee Scout", "scout"), ("archer", ARCHER)):
            r = sneak_sample(calling, runs, mode)
            sn = r["sneak"]
            print(f"  {label:<8}{name:<12} kill {r['kill'] * 100:5.1f}%   fights {r['fights']:4.2f}   opened by the Scout {sn['opened']:4.2f}   rooms where a creature noticed {sn['seen']:4.2f}"
                  f"   Endurance spent on rooms {sn['spent']:5.1f}", flush=True)
            dump_json(dict(kind="sneak", label=f"{label} {name}", kill=r["kill"], fights=r["fights"], **sn))
    print()
    print("The perfect player against the Overseer alone (level 5, starting gear, 4 potions; the Overseer is a Boss: it always notices, so he fights as ever but has paid for the room):")
    for label, mode in (("rank I", "none"), ("rank II", "cap")):
        for name, calling, weapon in (("melee Scout", "scout", "fine longsword"), ("archer", ARCHER, "hunting bow")):
            with ranked(mode=mode):
                plain = boss_stats(calling, 5, weapon, 4, runs=runs)
                paid = boss_stats(calling, 5, weapon, 4, runs=runs, sneak=True)
            print(f"  {label:<8}{name:<12} kill {plain[0] * 100:5.1f}% -> {paid[0] * 100:5.1f}% sneaking in (the room cost {5 if mode == 'none' else 4} Endurance; opened 0 fights, the Overseer notices)", flush=True)
    print()
    print("What would bring each Scout within 5 points of the mean of the five callings (a lever tried alone, the rest as shipped):")
    for label, mode in (("rank I", "none"), ("rank II", "cap")):
        with ranked(mode=mode):
            others = l5_kills([c for c in ARCHS if c != "scout"], runs)
        five, archer = seen[mode]
        mean = sum(five.values()) / 5
        print(f"  {label}: mean of the five {mean:.1f}; melee Scout {five['scout']:.1f} ({five['scout'] - mean:+.1f}); archer {archer:.1f} ({archer - mean:+.1f})", flush=True)
        for amb in (1.5, 2.0, 2.5, 3.0, 3.5, 4.0):
            with ranked(mode=mode, table=strike_table(amb, amb + 0.25, amb + 0.5)), sneaking():
                sc = l5_kills(["scout"], runs)["scout"]
            m5 = (sum(others.values()) + sc) / 5
            print(f"    Precision Strike ambush factor {amb:.2f} / {amb + .25:.2f} / {amb + .5:.2f}: melee Scout {sc:5.1f} ({sc - m5:+5.1f} from the mean)", flush=True)
        for rooms in ((5, 4, 3), (10, 8, 6), (15, 12, 9), (20, 16, 12)):
            with ranked(mode=mode, table=sneak_table(*rooms)), sneaking():
                ar = l5_kills([ARCHER], runs)[ARCHER]
                sc = l5_kills(["scout"], runs)["scout"]
            m5a = (sum(others.values()) + sc) / 5
            print(f"    Endurance a room {rooms[0]} / {rooms[1]} / {rooms[2]}: melee Scout {sc:5.1f} ({sc - m5a:+5.1f}), archer {ar:5.1f} ({ar - m5a:+5.1f})", flush=True)


# ---------------------------------------------------------------- the archer Scout: options and a played progression (docs/BALANCE_SCOUT.md)
#
#   python3 tests/balance_sim.py scout [std|cell|pace|hall|spread|notice|all] [runs]
#
# Nothing here is applied to the game: every option is a switch that is off by default (OPT, SIM_REROLL, SIM_BOW_STRIKE above).

SECOND_MODES = {"same": (1.0, 0), "plus15": (1.0, 15), "half": (0.5, 0)}


def aim_table(opener_delta=0.0, cost=None):
    """Aimed Shot's rank table with every opener lowered by opener_delta (the +0.5 step between ranks stays), and rank I's Endurance cost `cost` (the later
    ranks follow the default step of 15% of rank I's cost)."""
    base = dict(opener=round(2.5 + opener_delta, 4))
    if cost:
        base["endurance"] = cost
    rows = {r: dict(factor=f, opener=round(o + opener_delta, 4)) for r, f, o in ((2, 2.0, 3.0), (3, 2.25, 3.5), (4, 2.5, 4.0), (5, 2.75, 4.5))}
    return {"aimed_shot": dict(base=base, rows=rows)}


@contextmanager
def scout_option(mode=None, opener=0.0, second=None, per_level=0, elite=0, cap=None, cost=None, floor=10):
    """Switch on the options of docs/BALANCE_SCOUT.md for a block: opener = change to Aimed Shot's opener multiplier (A); second = None, "same",
    "plus15" or "half" (B); per_level, elite (C); cap = share of the target's maximum Vitality (D); cost = Aimed Shot's Endurance at rank I (E).
    mode: the rank mode of ranked()."""
    saved = dict(OPT)
    try:
        scale, plus = SECOND_MODES[second] if second else (1.0, 0)
        OPT.update(second=second, second_scale=scale, second_plus=plus, per_level=per_level, elite=elite, cap=cap, floor=floor)
        with ranked(mode=mode, table=aim_table(opener, cost) if (opener or cost) else None):
            yield
    finally:
        OPT.clear()
        OPT.update(saved)


SCOUT_OPTIONS = [
    ("baseline: the game now", {}),
    ("A1 opener x2.25", dict(opener=-0.25)),
    ("A2 opener x2.0", dict(opener=-0.5)),
    ("A3 opener x1.75", dict(opener=-0.75)),
    ("B1 second roll, entry chance", dict(second="same")),
    ("B2 second roll, entry + 15", dict(second="plus15")),
    ("B3 second roll, entry / 2", dict(second="half")),
    ("C1 +5 a level above, Elite +30", dict(per_level=5, elite=30)),
    ("C2 +10 a level above, Elite +30", dict(per_level=10, elite=30)),
    ("C3 +5 a level above only", dict(per_level=5)),
    ("C4 Elite +30 only", dict(elite=30)),
    ("D1 opener cap 75% of max Vitality", dict(cap=0.75)),
    ("D2 opener cap 60% of max Vitality", dict(cap=0.60)),
    ("E1 Aimed Shot costs 40", dict(cost=40)),
    ("E2 Aimed Shot costs 45", dict(cost=45)),
    ("E3 Aimed Shot costs 50", dict(cost=50)),
    ("F1 notice floor 20% (not in the brief)", dict(floor=20)),
    ("F2 notice floor 30% (not in the brief)", dict(floor=30)),
]
SCOUT_COMBOS = [   # the sensible combinations
    ("combo D2+B2", dict(cap=0.60, second="plus15")),
    ("combo E1+D2", dict(cost=40, cap=0.60)),
    ("combo E1+B2", dict(cost=40, second="plus15")),
    ("combo E1+D2+B2", dict(cost=40, cap=0.60, second="plus15")),
    ("combo E2+D2", dict(cost=45, cap=0.60)),
    ("combo E1+D2+F1", dict(cost=40, cap=0.60, floor=20)),
    ("combo D2+B2+F1", dict(cap=0.60, second="plus15", floor=20)),
]


def std_cells(runs, seed=1, callings=None):
    """The standard test (level 5, starting gear, 4 potions, points placed, middle human, RestFactor 5, Overseer from the cellar entrance), the Scouts sneaking.
    Returns {calling: kill%} for the five callings and the archer."""
    with sneaking():
        out = l5_kills(callings or list(ARCHS) + [ARCHER], runs, seed)
    if callings is None:
        ns = l5_kills(["scout", ARCHER], runs, seed)   # SNEAKING off: the Scouts who do not sneak
        out["scout_ns"], out["archer_ns"] = ns["scout"], ns[ARCHER]
    return out


def std_table(runs=1500, options=None, modes=(("I", "none"), ("II", "cap")), seed=1):
    print(f"Standard test, kill% against the Overseer from the cellar entrance, {runs} runs a cell, seed {seed}; the Scouts sneak. Rank I = nobody has bought a rank; rank II = everyone holds rank II.")
    print("| option | rank | warrior | scout (melee) | priest | mage | adventurer | archer | mean of five | archer - mean | scout - mean | scout, no sneak | archer, no sneak |")
    print("|---|---|---|---|---|---|---|---|---|---|---|---|---|")
    out = {}
    for label, kw in options or SCOUT_OPTIONS:
        for mname, mode in modes:
            with scout_option(mode=mode, **kw):
                v = std_cells(runs, seed)
            five = [v[c] for c in ARCHS]
            mean = sum(five) / 5
            out[(label, mname)] = v
            print(f"| {label} | {mname} | " + " | ".join(f"{x:.1f}" for x in five) + f" | {v[ARCHER]:.1f} | {mean:.1f} | {v[ARCHER] - mean:+.1f} | {v['scout'] - mean:+.1f} | {v['scout_ns']:.1f} | {v['archer_ns']:.1f} |", flush=True)
    return out


# ---- a played progression through the cellars, level 1 to level 5 ----
# The room list and creatures are APPROACH's (checked against mudlib/domains/hollow/cellars/*.cs and mob/*.cs); the Guard Post (the goblin guard and the
# keyholder, both Alert, a side branch west of the Smuggler Hall) is added, because its 64 experience is part of what a played character earns.
CELL_ROUTE = [("Tavern Cellar", ["Large Rat"] * 3), ("Collapsed Passage", ["Giant Rat"] * 2), ("Storage Chamber", ["Goblin Scavenger"] * 2),
              ("Smuggler Hall", []), ("Guard Post", ["Goblin Guard"] * 2), ("Flooded Tunnel", ["Giant Cave Toad"]),
              ("Mushroom Cavern", ["Spore Bat"] * 3), ("Goblin Barracks", ["Goblin Warrior"] * 3), ("Overseer Hall", ["Goblin Elite Guard"] * 2),
              ("Overseer Chamber", ["Goblin Overseer"])]
CELL_SPINE = {"Village": -1, "Tavern Cellar": 0, "Collapsed Passage": 1, "Storage Chamber": 2, "Smuggler Hall": 3, "Flooded Tunnel": 4, "Mushroom Cavern": 5,
              "Goblin Barracks": 6, "Overseer Hall": 7, "Overseer Chamber": 8}
CELL_GATE = {"Overseer Hall": 3, "Overseer Chamber": 4}   # the lowest level at which the played character goes through the door (the owner took the guards at level 3)
CELL_CORPSE = dict(CORPSE_SECONDS_BY_ROOM, **{"Guard Post": 420, "Overseer Chamber": 420})
START_POTIONS = 1
CELL_DEATH_SECONDS = 90.0     # wake in the Guest Rooms, walk back, loot the corpse
CELL_FLEE_SECONDS = 20.0
CELL_AVOID_SECONDS = 300.0   # after a flight or a death he leaves that room alone until he has gained a level or five minutes have passed
CELL_MAX_SECONDS = 4 * 3600.0


def cell_dist(a, b):
    """Rooms between two places of the cellars (the Guard Post hangs off the Smuggler Hall)."""
    def pos(x):
        return (CELL_SPINE["Smuggler Hall"], 1) if x == "Guard Post" else (CELL_SPINE[x], 0)
    (ia, da), (ib, db) = pos(a), pos(b)
    return abs(ia - ib) + da + db if a != b else 0


def cellar_run(calling, model, rng, sneak=True, recurve=False, goal=5):
    """One played progression: level 1, the calling's starting weapon, no armour, START_POTIONS potion, in the cellars until level `goal`. The character walks the
    route (CELL_ROUTE) fighting what is there, sits to rest after each fight (the middle human's `rest`), and when nothing is left walks out and waits for the
    resets (a room's creatures return the reset after their corpse is gone: CELL_CORPSE + a uniform 0-300 s; the Storage Chamber's potion each lap). He goes
    through the Overseer Hall's door at level 3 and the Overseer's at level 4 (CELL_GATE), and after a flight or a death does not try that room again until he
    has gained a level or CELL_AVOID_SECONDS have passed. Experience: the game's table (50 n (n-1)); a level-up heals fully, in the middle of a fight too. Death: the Guest Rooms at full
    Vitality, CELL_DEATH_SECONDS, the potions kept (the corpse is looted). The Scouts sneak into every room they can pay for. Returns a dict."""
    archer = calling == ARCHER
    arch = ARCHS["scout" if archer else calling]
    weapon0, shield, extra_hp, armor = gear_for(calling, "starting")
    sneak_on = SNEAKING and sneak and calling in ("scout", ARCHER)
    level, xp, t = 1, 0, 0.0
    p = make_player(ARCHER if archer else arch, level, weapon0, shield, 0, armor_extra=armor)
    avail = {room: 0.0 for room, c in CELL_ROUTE}
    avoid = {}
    potion_ready, left = 0.0, START_POTIONS
    st = dict(deaths=0, fled=0, potions=0, kills=0, first=0, free=0, dmg=0.0, by={}, first_fights=0, fights=0, nodmg=0, laps=0, opened=0, noticed=0, second=0, reached=None, level_at={})
    carry_ms, carry_t = 0, 0.0
    pos = "Village"
    kills_now = [0, 0, 0]

    def on_kill(pl, enemy):
        nonlocal xp, level
        k = enemy.kind
        if k.name != "Goblin Minion":
            kills_now[0] += 1
            kills_now[1] += pl.n_act == 1
            kills_now[2] += enemy.taken == 0   # killed before it ever began an action
            row = st["by"].setdefault(k.name, [0, 0, 0])
            row[0] += 1
            row[1] += pl.n_act == 1
            row[2] += enemy.taken == 0
        xp += k.xp
        while level < goal and xp >= xp_for_level(level + 1):
            level += 1
            pl.level = level
            pl.max_hp = player_max_hp(arch, level)
            pl.hp = pl.max_hp
            set_level_resources(pl, level)
            pl.ranks = default_ranks(pl.arch.name, level)
            st["level_at"][level] = None   # the time is filled in when the fight ends

    def refit():
        """The Warden's Recurve from level 3 (the Hidden Cache's keepsake): a new fighter with the same Vitality, Endurance and Focus."""
        nonlocal p
        if recurve and archer and level >= 3 and p.weapon != "warden's recurve":
            q = make_player(ARCHER, level, "warden's recurve", shield, 0, armor_extra=armor)
            q.hp, q.stamina, q.mana = min(q.max_hp, p.hp), min(q.max_stamina, p.stamina), min(q.max_mana, p.mana)
            p = q

    def sit(creatures):
        nonlocal t
        wait = sum(1 for c in creatures if KINDS[c].gold[1]) * LOOT_SECONDS + model.pause_s
        rest(p, wait)
        seated = seconds_to_rest(p, model)
        if model.p_linger and rng.random() < model.p_linger:
            seated += rng.uniform(*model.linger_s)
        rest(p, seated, model.rest_factor)
        t += wait + seated

    while level < goal and t < CELL_MAX_SECONDS:
        st["laps"] += 1
        fought, absent = False, []
        for room, creatures in CELL_ROUTE:
            if not creatures:
                continue
            if avail[room] > t:
                absent.append(avail[room])
                continue
            if level < CELL_GATE.get(room, 1) or (avoid.get(room) is not None and avoid[room][0] == level and t < avoid[room][1]):
                break
            refit()
            paid = False
            for _ in range(cell_dist(pos, room)):
                t += ROOM_SECONDS
                rest(p, ROOM_SECONDS)
                if sneak_on:
                    cost = p.skill_int("sneak", "room")
                    paid = p.stamina + 1e-9 >= cost
                    if paid:
                        p.stamina -= cost
            pos = room
            p.bless_until = p.cry_until = 0
            p.effects = {}
            enemies = [make_monster(c) for c in creatures]
            policy = HumanPolicy(model, rng)
            typed = policy.delay()
            start = start_times(p, enemies, typed, True)
            hidden, second, chances = False, None, []
            if sneak_on and paid:
                hidden, chances = sneak_room_roll(p, level, creatures, rng)
                st["noticed"] += not hidden
                if hidden and SIM_REROLL and reroll_notices(chances, typed, rng):
                    hidden = False
                    st["noticed"] += 1
            if hidden:
                ranged = RANGED_OPENING_MS if (archer or (SIM_BOW_STRIKE and p.weapon in BOWS)) else 0
                second = second_chance(chances[0]) if archer else None
                start = (typed, [typed + 2 * round(enemies[0].reaction * UNIT_MS) + ranged] + [typed + round(e.reaction * UNIT_MS) for e in enemies[1:]])
                st["opened"] += 1
            kills_now[0] = kills_now[1] = kills_now[2] = 0
            carry = max(0, round(carry_ms - (t - carry_t) * 1000))
            r = fight(p, enemies, left, boss=room == "Overseer Chamber", human=policy, start=start, stop_at_boss=True, heal_carry=carry, ambush=hidden,
                      second=second, on_kill=on_kill)
            carry_ms, carry_t = max(0, p.heal_ready - r["ms"]), t + r["ms"] / 1000.0
            p.regen(r["ms"])
            t += r["ms"] / 1000.0
            left = r["potions"]
            st["potions"] += r.get("potions_used", 0)
            st["second"] += r.get("second_noticed", 0)
            st["fights"] += 1
            st["nodmg"] += p.dmg_in == 0
            st["kills"] += kills_now[0]
            st["first"] += kills_now[1]
            st["free"] += kills_now[2]
            st["dmg"] += p.dmg_in / p.max_hp
            st["first_fights"] += kills_now[1] > 0
            for lv in st["level_at"]:
                if st["level_at"][lv] is None:
                    st["level_at"][lv] = t
            fought = True
            if not r["alive"]:
                st["deaths"] += 1
                avoid[room] = (level, t + CELL_AVOID_SECONDS)
                p.hp, p.stamina, p.mana = float(p.max_hp), float(p.max_stamina), float(p.max_mana)
                t += CELL_DEATH_SECONDS
                break
            if r.get("fled"):
                st["fled"] += 1
                avoid[room] = (level, t + CELL_AVOID_SECONDS)
                t += CELL_FLEE_SECONDS
                sit(creatures)
                break
            avail[room] = t + CELL_CORPSE.get(room, 420) + rng.random() * RESET_SECONDS
            if room == "Storage Chamber" and t >= potion_ready:
                left += 1
                potion_ready = t + rng.random() * RESET_SECONDS
            if level >= goal:
                st["reached"] = t
                break
            sit(creatures)
        if st["reached"] is not None:
            break
        back = cell_dist(pos, "Village") * ROOM_SECONDS
        t += back
        rest(p, back)
        pos = "Village"
        if not fought:   # nothing to fight: wait, seated, for the next reset
            wait = max(0.0, min(absent) - t) if absent else 60.0
            rest(p, wait, model.rest_factor)
            t += wait
    return st


def by_kind(rows):
    """{creature: (kills a run, share killed by the first action, share killed before it acted)} over the runs."""
    tot = {}
    for r in rows:
        for k, (n, f, z) in r["by"].items():
            a = tot.setdefault(k, [0, 0, 0])
            a[0] += n
            a[1] += f
            a[2] += z
    return {k: (a[0] / len(rows), a[1] / a[0] * 100, a[2] / a[0] * 100) for k, a in tot.items()}


def cell_stats(calling, runs, sneak=True, recurve=False, seed=20261124, model=None, goal=5):
    """Many cellar_runs. Means and standard errors (of the mean) of: deaths a run, flights, potions used, the share of creatures killed by the first action, the
    share of fights in which no Vitality was lost, simulated minutes to reach level `goal`, the share of runs that reached it within CELL_MAX_SECONDS."""
    model = model or rest_human("mid", 5)
    rng = random.Random(seed)
    saved = random.getstate()
    random.seed(seed * 7919 + 5)
    rows = [cellar_run(calling, model, rng, sneak, recurve, goal) for _ in range(runs)]
    random.setstate(saved)

    def mean_se(xs):
        n = len(xs)
        m = sum(xs) / n if n else 0.0
        var = sum((x - m) ** 2 for x in xs) / (n - 1) if n > 1 else 0.0
        return m, math.sqrt(var / n) if n else 0.0

    reached = [r for r in rows if r["reached"] is not None]
    kills = sum(r["kills"] for r in rows) or 1
    fights = sum(r["fights"] for r in rows) or 1
    return dict(runs=runs, deaths=mean_se([r["deaths"] for r in rows]), fled=mean_se([r["fled"] for r in rows]), potions=mean_se([r["potions"] for r in rows]),
                first=sum(r["first"] for r in rows) / kills * 100, free=sum(r["free"] for r in rows) / kills * 100, dmg=sum(r["dmg"] for r in rows) / fights * 100,
                by=by_kind(rows), first_fights=sum(r["first_fights"] for r in rows) / fights * 100, nodmg=sum(r["nodmg"] for r in rows) / fights * 100,
                minutes=mean_se([r["reached"] / 60.0 for r in reached]), reached=len(reached) / runs * 100, fights=sum(r["fights"] for r in rows) / runs,
                laps=sum(r["laps"] for r in rows) / runs, opened=sum(r["opened"] for r in rows) / fights * 100, second=sum(r["second"] for r in rows) / runs,
                died_runs=sum(1 for r in rows if r["deaths"]) / runs * 100,
                level_min={lv: mean_se([r["level_at"][lv] / 60.0 for r in rows if r["level_at"].get(lv)])[0] for lv in (2, 3, 4, 5)})


def cell_line(label, c):
    m, se = c["minutes"]
    return (f"| {label} | {c['deaths'][0]:.2f} (+-{c['deaths'][1]:.2f}) | {c['potions'][0]:.2f} | {c['first']:.0f}% | {c['free']:.0f}% | {c['nodmg']:.0f}% | {c['dmg']:.0f}% | {m:.1f} (+-{se:.1f}) | {c['reached']:.0f}% | "
            f"{c['fights']:.1f} | {c['opened']:.0f}% |")


CELL_HEAD = ("| option | deaths a run | potions used | creatures killed by the first action | creatures killed before they acted | fights with no damage taken | Vitality lost a fight | minutes to level 5 | reached level 5 | fights a run | fights opened from hiding |\n"
             "|---|---|---|---|---|---|---|---|---|---|---|")


def hall_stats(calling, level, runs, recurve=False, potions=2, seed=20261125, sneak=True):
    """The two Goblin Elite Guards alone, from the Barracks' door: a character of this level and calling at full Vitality, Endurance and Focus, `potions` potions,
    the middle human. Returns win%, the share of runs in which no Vitality was lost, the share opened from hiding, potions used."""
    archer = calling == ARCHER
    arch = ARCHS["scout" if archer else calling]
    model = rest_human("mid", 5)
    weapon0, shield, extra_hp, armor = gear_for(calling, "starting")
    sneak_on = SNEAKING and sneak and calling in ("scout", ARCHER)
    rng = random.Random(seed)
    saved = random.getstate()
    random.seed(seed * 7919 + level)
    win = nodmg = opened = used = 0
    for _ in range(runs):
        p = make_player(ARCHER if archer else arch, level, "warden's recurve" if recurve and archer else weapon0, shield, 0, armor_extra=armor)
        creatures = ["Goblin Elite Guard"] * 2
        enemies = [make_monster(c) for c in creatures]
        policy = HumanPolicy(model, rng)
        typed = policy.delay()
        start = start_times(p, enemies, typed, True)
        hidden, second, chances = False, None, []
        if sneak_on:
            cost = p.skill_int("sneak", "room")
            if p.stamina >= cost:
                p.stamina -= cost
                hidden, chances = sneak_room_roll(p, level, creatures, rng)
                if hidden and SIM_REROLL and reroll_notices(chances, typed, rng):
                    hidden = False
        if hidden:
            ranged = RANGED_OPENING_MS if (archer or (SIM_BOW_STRIKE and p.weapon in BOWS)) else 0
            second = second_chance(chances[0]) if archer else None
            start = (typed, [typed + 2 * round(enemies[0].reaction * UNIT_MS) + ranged] + [typed + round(enemies[1].reaction * UNIT_MS)])
        r = fight(p, enemies, potions, human=policy, start=start, ambush=hidden, second=second)
        win += r["alive"]
        nodmg += p.dmg_in == 0
        opened += hidden
        used += r.get("potions_used", 0)
    random.setstate(saved)
    return dict(win=win / runs * 100, nodmg=nodmg / runs * 100, opened=opened / runs * 100, potions=used / runs)


def notice_table():
    """Which cellar creatures an option touches: the chance to notice a Scout of each level (Grace with the points placed, rank I of Sneak), now and with option C."""
    kinds = [("Large Rat", 1), ("Giant Rat", 1), ("Goblin Scavenger", 2), ("Goblin Guard", 2), ("Spore Bat", 2), ("Giant Cave Toad", 3), ("Goblin Warrior", 3),
             ("Goblin Elite Guard", 4), ("Goblin Overseer", 5)]
    print("Chance, in percent, that a creature notices a sneaking Scout on entering its room (rank I of Sneak, Grace 9 + the 2 points a level placed in Grace):")
    print("| creature (level) | flags | " + " | ".join(f"Scout level {L}" for L in range(1, 6)) + " |")
    print("|---|---|" + "---|" * 5)
    for variant, kw in (("now", {}), ("C1: +5 a level above, Elite +30", dict(per_level=5, elite=30)), ("C2: +10 a level above, Elite +30", dict(per_level=10, elite=30))):
        print(f"| **{variant}** | | | | | | |")
        with scout_option(**kw):
            for name, lvl in kinds:
                flags = ("Boss" if name in SNEAK_BOSS else "Alert" if name in SNEAK_ALERT else "") + (" Elite" if name in SNEAK_ELITE and OPT["elite"] else "")
                cells = []
                for L in range(1, 6):
                    g = 9 + 2 * (L - 1)
                    cells.append("always" if name in SNEAK_BOSS else f"{notice_chance(g, L, lvl, name in SNEAK_ALERT, 0, name in SNEAK_ELITE)}")
                print(f"| {name} ({lvl}) | {flags or '-'} | " + " | ".join(cells) + " |")


def all_options():
    return SCOUT_OPTIONS + SCOUT_COMBOS


def cell_table(runs=1000, options=None, recurve=False, sneak=True):
    print(f"Archer Scout {'sneaking' if sneak else 'NOT sneaking'}, a played progression level 1 to 5 through the cellars ({runs} runs a row, seed 20261124), "
          f"{'with the Warden' + chr(39) + 's Recurve from level 3' if recurve else 'with the Hunting Bow throughout'}. Standard errors of the mean in brackets.")
    print(CELL_HEAD)
    for label, kw in options or all_options():
        with sneaking(), scout_option(mode="none", **kw):
            print(cell_line(label, cell_stats(ARCHER, runs, sneak, recurve)), flush=True)


def kind_table(runs=1000, options=None):
    """Which creatures die to the first action, and which die before they have acted at all (archer, sneaking, Hunting Bow)."""
    options = options or [o for o in all_options() if o[0][:2] in ("ba", "A3", "B2", "D1", "D2") or o[0].startswith("combo")]
    kinds = ["Large Rat", "Giant Rat", "Goblin Scavenger", "Spore Bat", "Goblin Guard", "Goblin Warrior", "Goblin Elite Guard", "Giant Cave Toad"]
    print(f"Archer sneaking, Hunting Bow, {runs} runs: of the creatures of each kind that the archer kills, the share killed by his first action / killed before they have acted at all.")
    print("| creature (Vitality) | kills a run | " + " | ".join(l for l, _ in options) + " |")
    print("|---|---|" + "---|" * len(options))
    res = []
    for label, kw in options:
        with sneaking(), scout_option(mode="none", **kw):
            res.append(cell_stats(ARCHER, runs, True)["by"])
    for k in kinds:
        print(f"| {k} ({KINDS[k].hp}) | {res[0].get(k, (0, 0, 0))[0]:.2f} | " + " | ".join(f"{r.get(k, (0, 0, 0))[1]:.0f}% / {r.get(k, (0, 0, 0))[2]:.0f}%" for r in res) + " |", flush=True)


def yardstick_table(runs=1000):
    print(f"The same progression for the other callings and the archer who does not sneak, as they are now ({runs} runs a row, seed 20261124):")
    print(CELL_HEAD)
    with sneaking(), scout_option(mode="none"):
        for label, calling, sn in (("warrior (the yardstick)", "warrior", True), ("melee Scout, sneaking", "scout", True), ("melee Scout, not sneaking", "scout", False),
                                   ("archer, not sneaking", ARCHER, False), ("priest", "priest", True), ("mage", "mage", True), ("adventurer", "adventurer", True)):
            print(cell_line(label, cell_stats(calling, runs, sn)), flush=True)


def pace_table(runs=1000):
    print(f"Pace: simulated minutes to reach each level, the cellar progression, starting kit, one potion, the game's experience table, middle human with `rest` "
          f"({runs} runs a row, seed 20261124). The Scouts sneak.")
    print("| calling | level 2 | level 3 | level 4 | level 5 | reached level 5 | deaths a run | potions used |")
    print("|---|---|---|---|---|---|---|---|")
    with sneaking(), scout_option(mode="none"):
        for label, calling, sn in (("warrior", "warrior", True), ("scout (melee, sneaking)", "scout", True), ("priest", "priest", True), ("mage", "mage", True),
                                   ("adventurer", "adventurer", True), ("archer, sneaking", ARCHER, True), ("archer, not sneaking", ARCHER, False)):
            c = cell_stats(calling, runs, sn)
            lm = c["level_min"]
            print(f"| {label} | {lm[2]:.1f} | {lm[3]:.1f} | {lm[4]:.1f} | {lm[5]:.1f} (+-{c['minutes'][1]:.1f}) | {c['reached']:.0f}% | {c['deaths'][0]:.2f} | {c['potions'][0]:.2f} |", flush=True)


def hall_table(runs=1500, options=None):
    print(f"The two Goblin Elite Guards alone from the Barracks' door, at full Vitality, 2 potions, the middle human ({runs} runs a cell, seed 20261125): win%, and in brackets the share of runs in which no Vitality was lost.")
    print("| option | archer L3 | archer L3 + Recurve | archer L4 | archer L4 + Recurve | melee Scout L3 | warrior L3 |")
    print("|---|---|---|---|---|---|---|")
    for label, kw in options or all_options():
        with sneaking(), scout_option(mode="none", **kw):
            cells = []
            for calling, level, rec in ((ARCHER, 3, False), (ARCHER, 3, True), (ARCHER, 4, False), (ARCHER, 4, True), ("scout", 3, False), ("warrior", 3, False)):
                h = hall_stats(calling, level, runs, rec)
                cells.append(f"{h['win']:.0f} ({h['nodmg']:.0f})")
            print(f"| {label} | " + " | ".join(cells) + " |", flush=True)


def spread_table(runs=1500, seeds=range(1, 9)):
    print(f"Run-to-run spread of the standard test: the baseline, {runs} runs a cell, seeds {seeds[0]} to {seeds[-1]}; rank I, the Scouts sneaking.")
    print("| seed | warrior | scout | priest | mage | adventurer | archer |")
    print("|---|---|---|---|---|---|---|")
    rows = []
    for sd in seeds:
        with scout_option(mode="none"):
            v = std_cells(runs, sd)
        rows.append(v)
        print(f"| {sd} | " + " | ".join(f"{v[c]:.1f}" for c in list(ARCHS) + [ARCHER]) + " |", flush=True)
    cols = list(ARCHS) + [ARCHER]
    mean = {c: sum(r[c] for r in rows) / len(rows) for c in cols}
    sd = {c: math.sqrt(sum((r[c] - mean[c]) ** 2 for r in rows) / (len(rows) - 1)) for c in cols}
    print("| mean | " + " | ".join(f"{mean[c]:.1f}" for c in cols) + " |")
    print("| sd of one cell | " + " | ".join(f"{sd[c]:.2f}" for c in cols) + " |")


def scout_report(parts=("std",), runs=1500):
    for part in parts:
        if part == "notice":
            notice_table()
        elif part == "std":
            std_table(runs, all_options())
        elif part == "cell":
            cell_table(max(500, runs), None, False)
            print()
            cell_table(max(500, runs), None, True)
            print()
            cell_table(max(500, runs), None, False, False)
            print()
            kind_table(max(500, runs))
        elif part == "pace":
            pace_table(max(500, runs))
            print()
            yardstick_table(max(500, runs))
        elif part == "hall":
            hall_table(runs)
        elif part == "spread":
            spread_table(runs)
        print()


def ranks_report(runs=3000, parts=()):
    parts = parts or ("level5", "heal", "double", "gap", "bolt", "focus", "climb")
    for part in parts:
        {"level5": lambda: ranks_level5(runs), "heal": lambda: ranks_heal(runs), "double": lambda: ranks_double(runs),
         "gap": lambda: ranks_gap(max(100, runs // 8)), "bolt": ranks_bolt, "focus": lambda: ranks_focus(max(200, runs // 3)),
         "climb": lambda: ranks_climb(max(100, runs // 10)), "aim": lambda: ranks_aim(runs), "wait": lambda: ranks_wait(runs), "aimfix": lambda: ranks_aimfix(runs), "healcost": lambda: ranks_healcost(runs), "selfheal": lambda: ranks_selfheal(runs), "sneak": lambda: ranks_sneak(runs)}[part]()


def main():
    if len(sys.argv) > 1 and sys.argv[1] == "check":
        sys.exit(0 if check_mudlib() else 1)
    if len(sys.argv) > 1 and sys.argv[1] == "corridor":
        corridor_table(int(sys.argv[2]) if len(sys.argv) > 2 else 400, tuple(sys.argv[3:]))
        return
    if len(sys.argv) > 1 and sys.argv[1] in ("calibrate", "human", "recommend", "ablate"):
        runs = int(sys.argv[2]) if len(sys.argv) > 2 else 3000
        if sys.argv[1] == "recommend":
            recommend_report(runs, tuple(a for a in sys.argv[3:]))
        else:
            {"calibrate": calibrate_report, "human": human_report, "ablate": ablate_report}[sys.argv[1]](runs)
        return
    if len(sys.argv) > 1 and sys.argv[1] == "rest":
        runs = int(sys.argv[2]) if len(sys.argv) > 2 and sys.argv[2].isdigit() else 3000
        parts = tuple(a for a in sys.argv[2:] if not a.isdigit()) or ("grid", "sens")
        for part in parts:
            {"grid": rest_grid, "sens": lambda r: rest_sens(r // 2), "pace": lambda r: rest_pace(max(40, r // 30)), "options": rest_options, "perfect": rest_perfect, "harvest": rest_harvest}[part](runs)
        return
    if len(sys.argv) > 1 and sys.argv[1] == "ranks":
        runs = int(sys.argv[2]) if len(sys.argv) > 2 and sys.argv[2].isdigit() else 3000
        ranks_report(runs, tuple(a for a in sys.argv[2:] if not a.isdigit()))
        return
    if len(sys.argv) > 1 and sys.argv[1] == "scout":
        runs = int(next((a for a in sys.argv[2:] if a.isdigit()), 1500))
        parts = tuple(a for a in sys.argv[2:] if not a.isdigit()) or ("notice", "std", "cell", "pace", "hall", "spread")
        scout_report(parts, runs)
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
    print("   Each cell: win% / Vitality lost in won fights.")
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
    print(f"{'Archetype':<12}{'Lvl':>4}{'Potions':>8}{'Kill%':>7}{'Survive%':>10}{'Vitality lost':>14}{'Seconds':>9}{'Actions':>9}")
    boss = {}
    for n in names:
        a = ARCHS[n]
        for level in (4, 5):
            for potions in (0, 2):
                kill, survive, lost, secs, acts = boss_stats(n, level, a.weapons[-1], potions, TRINKET_HP, runs)
                boss[(n, level, potions)] = kill
                print(f"{n:<12}{level:>4}{potions:>8}{kill * 100:>6.0f}%{survive * 100:>9.0f}%{lost:>14.1f}{secs:>9.1f}{acts:>9.1f}")
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
