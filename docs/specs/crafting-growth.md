# Proficiency growth by chance, and corpse decay

Owner's decisions from play (November 2026). Two changes.

## 1. Proficiency grows by a rising chance, not a fixed amount
Today every successful piece of work near your skill adds a full point (10 tenths), which the owner found too fast and too predictable. New rule, for every gathering and crafting proficiency:

- A success no longer adds a fixed amount. It rolls a chance to gain **one whole point**. If the roll fails, the chance for the next success is the next rung of a ladder; when a point is gained the ladder starts again. Owner's ladder: 20%, 25%, 50%, 75%, 100%.
- "Restarting and lengthening as you get more levels": each rung is used (1 + skill / 20, whole numbers) times before moving to the next. Skill 0-19: 20, 25, 50, 75, 100. Skill 20-39: 20, 20, 25, 25, 50, 50, 75, 75, 100, 100. And so on. The expected number of successes for a point is about 2.8, 3.6, 4.0, 4.4 and 4.7 in the five bands, roughly 390 successes from 0 to 100.
- The existing bands stay: work more than `growth.easy_band` below your skill teaches nothing and does not move the ladder; work in the easy band rolls at half the rung's chance (so never certain) and moves the ladder; work in the near band rolls at the rung's chance.
- The fixed-gain numbers (`growth.near_gain`, `growth.easy_gain`, `growth.drag`, `growth.min_factor`) go. New numbers in `/crafting/numbers.cs`: `growth.ladder` (the list), `growth.repeat_every` (20), `growth.easy_factor` (0.5).
- The position on the ladder is kept per character and per proficiency, saved with the proficiency. It is never shown: `proficiencies` shows the skill as today and nothing about the chance.
- A failed piece of work teaches nothing and does not move the ladder, as today.
- Anything that later speeds learning (the Adventurer's Jack of All Trades, +25%) multiplies the rung's chance, capped at 100%.
- `tests/craft_sim.py` and the proficiency checks in `tests/craft_test.py` and `tests/craft_content_test.py` follow the new rule (fix the dice through the existing probe where a test needs an exact outcome). The simulator reports successes needed per band.

## 2. Creature corpses decay in ten minutes
A corpse with a skin left on it lasts an hour today, so a hunting ground fills with skinnable corpses. New rule:

- A **creature's** corpse rots 600 seconds after the death, whatever is still in or on it (loot, a skin). The number is one field on the corpse, and a creature file may set its own. The empty-corpse timer (120 s) and crumbling at once when the last thing is taken are unchanged. Bleeding stays limited to the first 300 seconds.
- A **player's** corpse keeps its hour, because it holds that player's belongings.
- When a corpse rots with things still in it, the things are destroyed with it, as today.
