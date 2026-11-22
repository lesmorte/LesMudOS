# Vincent's Hollow expansion: archery, weapon ladders, armour, a bigger village

**Built so far (22 November 2026): section 1, archery (the ranged opening and Aimed Shot, with ranks IV and V by the continuation docs/CLASS_SYSTEM.md section 14 states).**

Owner's requests (19 November 2026): "A scout should be able to play as a sniper archer type. Maybe a bow skill." "The starting village needs to be bigger; there should be advanced versions of each starting weapon, some loot, some at shops." Every number here is a starting value for `tests/balance_sim.py`; none is tested.

## Why
- The only bow in the game is the starting Hunting Bow (8-12). A Scout's upgrade is the Fine Iron Longsword, so the archer stops being an archer at level 3. The Priest's mace and the Mage's staff have no upgrade at all.
- No body armour is sold anywhere in the Hollow. The only armour before the corridor is the quest helm and the Overseer's vest. The owner's level 5 Scout reached the Overseer with no armour and the starting bow.
- The village is twelve rooms with one general merchant.

## 1. Archery
- **Bows are ranged weapons.** When a character wielding a bow starts a fight, the target has to close the distance: its first action is delayed by a further 2 seconds on top of its Reaction Time. This applies to every calling that may wield a bow, once per fight, and not when the creature starts the fight.
- **Aimed Shot (`aim`)**, a new Scout skill from level 1, bow only: 30 Endurance, Preparation 1.0, Recovery 1.0, +3 to hit, damage x1.75; as the action that starts the fight, damage x2.5. Being hit while preparing it spoils the shot (the Endurance is spent). Ranks follow `docs/specs/skill-ranks.md`: II at level 5 (x2.0 / x3.0), III at level 10 (x2.25 / x3.5).
- **Precision Strike** stays as it is and works with any weapon, so the Scout has a melee line and an archery line. The Evasion and Tracking features are unchanged.
- **Ammunition:** none. Arrows are not tracked.
- **Shooting into the next room** (true sniping) is left for later: it needs line of sight, a rule for creatures that come after you, and protection for players in safe rooms.
- The simulator gets an archer Scout and a melee Scout as separate lines; the two must come out within a few points of each other and of the other callings.

## 2. Weapon ladders for levels 1 to 5
Three steps for every weapon line: the starter (in every shop, as now), an improved one sold in the village (affordable around level 3), and a fine one that is found, not bought. Reaction modifiers and to-hit are set so that the lines are equal in damage per second at each step; the simulator's weapon table (section 4 of its output) is the check.

| Line | Starter (shop) | Improved (shop) | Fine (found) |
|---|---|---|---|
| Sword | Rusty Sword 9-14, 10 gold | Fine Iron Longsword 12-18, 60 gold (exists) | Captain's Sabre 14-20: Goblin Elite Guard drop, rare |
| Axe | Woodcutter Axe 14-21, 18 gold (exists; slow) | Felling Axe 15-23, 65 gold | The Goblin Overseer's Axe 17-25 (exists) |
| Bow | Hunting Bow 8-12, 20 gold | Yew Longbow 11-16, 60 gold | Warden's Recurve 13-19: the Hidden Cache |
| Mace | Novice's Mace 9-13, 12 gold | Flanged Mace 12-17, 55 gold | Shrine-Blessed Mace 13-18, +1 Spirit: a new quest from the shrine |
| Staff | Apprentice's Staff 7-11, 10 gold | Ironshod Staff 9-13, +1 Insight, 55 gold | Staff of the Hollow Oak 10-14, +2 Insight: Mushroom Cavern, hidden |
| Spear | Training Spear 8-12, 8 gold | Boar Spear 11-16, 50 gold | Goblin War-Spear 13-18: Goblin Barracks drop, rare |
| Dagger | Iron Dagger 6-9, 6 gold | Steel Dirk 8-12, 45 gold | Enchanted Dagger 7-11 (exists; its enchantment is its value) |

- Attribute bonuses on weapons use the equipment hook the attribute build provides. The caster weapons carry attributes because a caster's staff damage hardly matters.
- "Rare" drops use the existing loot percentages; a find uses the finds mechanic (once per character) where the item should not be farmable.
- Quality (Poor to Masterwork) applies to all of these as it does to every item.

## 3. Armour
Sold in the village, light enough for every calling that may wear it:

| Piece | Armour | Price | Who may wear |
|---|---|---|---|
| Padded Jerkin | 1 | 20 gold | all |
| Leather Jerkin | 2 | 50 gold | all but Mage |
| Leather Cap | 1 | 25 gold | all (the quest helm is the same value, so the quest still saves the gold) |
| Scholar's Robe | 0, +1 Insight | 40 gold | Mage, Priest |

The Leather Commander's Vest (3) stays the best body armour before the corridor. The simulator's Overseer table is rerun with "what a level 4 character can afford" as the gear assumption, replacing today's assumption of no body armour.

### Armour by calling (owner's decision, 21 November 2026)
The engine restricts armour only by weight (light, medium, heavy), so "all but Mage" and "Mage, Priest" in the table above are words in a description today. They become rules:
- An armour file may name the callings that can wear it (`WornBy`, a list of calling ids) or the callings that cannot (`NotWornBy`). With neither, the weight category decides, as now. An advanced class counts as the calling it grew from.
- `wear` refuses in words that say who the piece is for. The item's description line and a shop's `browse` show the restriction, so nobody buys what they cannot wear; `buy` warns before taking the gold.
- A piece already worn when a rule arrives (old saves) stays worn until taken off.
- Applied now: Leather Jerkin not worn by Mage; Scholar's Robe worn by Mage and Priest only. The same fields are available to weapons (a file may restrict a weapon the same way), but no weapon is changed in this step.
- Tests: each calling against each restricted piece, the shop warning, an advanced class inheriting, an old save with a now-forbidden piece worn, and that a wizard-written object cannot dress a player in a forbidden piece through a public method.
- Built together with the `rest` command, after the rename merges (both touch `std/adventurer.cs`).

## 4. A bigger village
Six new rooms off the square and the north road, each with a reason to exist:

| Room | What is there |
|---|---|
| Fletcher's Lane | A short street joining the square to the new shops |
| The Strung Yew (bowyer) | Bows; later the Scout's rank trainer; the Aimed Shot is taught and talked about here |
| The Armourer's Shed (beside the Iron Oak) | The armour above; the smith keeps weapons |
| The Shrine of the Quiet Hand | The Priest's rank trainer; the Shrine-Blessed Mace quest; a healer's blessing for coin |
| The Hedge-Scholar's Loft | The Mage's rank trainer (Arcane Bolt II is taught here, not at the guild); the Ironshod Staff and the robe |
| The Practice Yard | A training dummy (no experience, shows your damage: useful with `verbose`), archery butts, the Warrior's and Adventurer's rank trainer |

- The general merchant keeps the starters; the specialists sell the improved step. This is also the first use of "what a merchant deals in" from `docs/specs/resale-economy.md`.
- New people get names from a list given to the builder (no reuse of existing names), topics, and a rumour or two each; nothing may name the Circle of Ash.
- The Hollow's notice board and the guild's talk point players at the new shops by level 2.

## 5. Order of work
After the attribute build and the rename (the items carry attribute bonuses and Endurance costs). Together with skill ranks, since the rank trainers live in the new rooms:
1. Sonnet: weapons, armour, loot placements, the six rooms and their people.
2. Sonnet: Aimed Shot and the ranged opening rule (engine change in the fight start), with tests.
3. Balance re-check with the human-like player model, archer and melee Scouts as separate lines.

## 6. Owner's decisions (19 November 2026)
1. Ammunition is not tracked.
2. Rank trainers live in the new rooms. Arcane Bolt II moves from the Adventurers Guild to the Hedge-Scholar's Loft (this overrides the guild in `docs/specs/skill-ranks.md`).
3. Improved weapons need gold only, no level.
