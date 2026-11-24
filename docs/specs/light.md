# Light and darkness (proposed 24 November 2026; not built)

Owner's ideas (24 November 2026): "a light level for rooms (default to 1) to have visibility. Items could also have + or - light and affect the current light level of a room. This makes things like lanterns and light spells have value." And: "Should also have a craftable starter torch." Every number here is a starting value for play-testing and for `tests/balance_sim.py`; nothing has been run, and section 12 lists what to measure. Where this document says "today" it means the files in the repository on 24 November 2026.

## Decisions for the owner
Each has a recommendation; the spec is written as if you accept all of them.
1. **The scale is whole numbers, and the owner's "default 1" is "lit".** A room's light is its base number plus the light of everything lit in it; 1 or more is lit, 0 is dim, -1 or less is dark. No finer scale is needed. Consequence to know about: an ordinary room (base 1) is unchanged by any light; a dim room (base 0) needs one more point to be lit; a dark room (base -1) needs two. So **a torch (+1) turns a dark room into a dim one, and only a lantern (+2), or a torch together with a Glow spell, makes it lit.** That is what gives the lantern its value. Recommendation: yes.
2. **To-hit penalties: dim -1 melee and -2 ranged; dark -3 melee and -6 ranged** (one point is 5% on the d20). Ranged is deliberately double: the archer Scout is the strongest line in the simulator and darkness is the lever against it. Spells that need no hit roll (Arcane Bolt) are not affected. Recommendation: yes; measure first (section 12).
3. **Sneaking.** Light changes a creature's chance to notice a sneaking Scout: dim -10, dark -25 (-15 for a creature that sees in the dark), and +20 if the Scout carries a light of their own. Recommendation: yes. The numbers for a level 3 Scout are in section 8.
4. **The cellars.** Lit by wall lanterns: Tavern Cellar, Collapsed Passage, Storage Chamber, Smuggler Hall, Overseer Chamber. Dim: Guard Post, Mushroom Cavern, Overseer Hall. Dark: Flooded Tunnel, Goblin Barracks, Hidden Cache. Your words were "dark from the Flooded Tunnel down"; the Mushroom Cavern is dim because of its glowing fungus, the Overseer Hall because a boss approach should be playable without a lantern, and the Overseer Chamber is lit so the boss fight is not fought blind. The Guard Post (where the key is kept) is dim: it is the gentlest first meeting with the penalties. Alternative: make the Guard Post lit. Recommendation: as written.
5. **Prices and burn times** (Hollow prices read from the shop files): Pitch Torch 5 gold, 25 minutes; Storm Lantern 30 gold, 60 minutes of oil, Flask of Lamp Oil 4 gold (half a lantern); the craftable Rag Torch costs about 1.5 gold and burns 10 minutes. A whole career in the cellars (levels 1 to 5, about an hour of play) costs 10 to 15 gold in torches or 38 gold for the lantern and two flasks. Recommendation: yes; I would not make it bite harder, because a character starts with 10 to 20 gold.
6. **A free first torch.** One Rag Torch, once per character, from a bin by the stair in the Tavern Cellar (the finds mechanic, as a keepsake), so nobody meets the dark with nothing. Recommendation: yes.
7. **A new spell, Glow, for the Mage and the Priest from level 1** (command `glow`): Focus 20 / 23 / 26 at ranks I / II / III, light +1 / +1 / +2, lasting 5 / 8 / 12 minutes. Rank II costs 20 gold at the Hedge-Scholar's Loft and the Shrine of the Quiet Hand; rank III is the Hall of Legends' usual price. It can be cast on a friend. Recommendation: yes. This means the two casters never need a torch; they are the party's lamp. That is the point of the spell, and a Mage's Focus spent on light is not spent on bolts.
8. **A burnt-out torch vanishes; a burnt-out lantern stays and is refilled.** Torches are consumed, so they are a sink, and nothing clutters the floor. Recommendation: yes.
9. **A lit light goes out when its owner leaves the game** and is saved with its remaining time (at most 60 seconds of drift after a crash). Recommendation: yes.
10. **Only the Giant Cave Toad cannot see in the dark** of all the creatures in the cellars. Every goblin, rat and spore bat is at home there. Recommendation: yes; it keeps the dark a player problem, which is what you asked for.
11. **Darkness changes `look` strongly.** In the dark you see no creatures or items and cannot loot, craft, gather, read or shop; you can still fight what attacks you and move. The room's name is shown with "(dark)", and the Mudlet mapper is sent the name and the way you came in, nothing more. Recommendation: yes.
12. **No new light in the Corridor yet**, and no night or weather (sections 14 and 15). Recommendation: yes.

## 1. What exists today
Nothing models light. Rooms have no light value and every player sees every room. The only light in the game is a toy lantern in `/domains/town/lantern.cs` (verbs `light` and `extinguish`, "it will not last long") that has no effect on anything. Some Corridor rooms describe gloom (`green_road/m10`: "even at midday a traveller here would be glad of a lantern") with no rule behind it. The Smuggler Hall's description already speaks of "rusted lantern hooks" with old tallow on them, which this spec uses.

Everything below is mudlib code; the driver is not changed.

## 2. Light as a number
The **light** of a room is an integer: `clamp(base + sum of the light of every lit source in it, -3, +3)`.
- **Base.** A room field `Light`, default **1**. Named constants for builders: `LightLit` (1), `LightDim` (0), `LightDark` (-1), `LightBlack` (-2).
- **Sources.** An object that is lit and either lies in the room, or is carried by a creature in the room (in their pack or hands), adds its light value. Everyone's counts: a Priest's Glow lights the room for everyone in it, a Warrior's torch for the whole party. An object inside a container (a corpse, a future bag) lights nothing. Values: Rag Torch +1, Pitch Torch +1, Storm Lantern +2, wall lantern or lamp +1, Glow +1 (rank III +2). A source may be negative (section 13). Each source is clamped to -3 to +3 before adding, and so is the total.
- **The state a player meets:**

| Light | State | In words |
|---|---|---|
| 1 or more | **lit** | You see as you do today. |
| 0 | **dim** | You see everything, but poorly: you fight, search and work a little worse. |
| -1 or less | **dark** | You see nothing but what you touch. |

Each room's **base** stays the same; an unlit lantern or a torch in a pack does nothing. Because only lit sources count, light changes only when something is lit, put out, burns out, is dropped, picked up, carried in or out, or a spell begins or ends (section 10).

## 3. Sources
| Source | Light | Lasts | Price | Where |
|---|---|---|---|---|
| Rag Torch | +1 | 10 minutes (600 s; warns at 30 s) | not sold; made (section 4); one free from the bin | Tavern Cellar bin; crafting |
| Pitch Torch | +1 | 25 minutes (1500 s; warns at 60 s) | 5 gold (Value 4) | Harland |
| Storm Lantern | +2 | 60 minutes of oil (3600 s) | 30 gold (Value 30) | Tobin Fettle |
| Flask of Lamp Oil | refills 30 minutes (1800 s) | consumed by `fill` | 4 gold (Value 4) | Harland and Tobin Fettle |
| Wall lantern, lamp (furniture) | +1 | never goes out | not for sale | cellar rooms, section 11 |
| Glow (spell) | +1 / +1 / +2 | 5 / 8 / 12 minutes | 20 / 23 / 26 Focus | Mage, Priest |

### Commands (in `/std/adventurer`, not on the items, so names, numbers and stacks work as everywhere)
- `light THING` (also `ignite`): lights a torch or lantern you carry. With no word it lights the only unlit source you carry. "You light the pitch torch. It flares, then settles to a steady flame." Others: "Keith lights a pitch torch." It is a quick action on the combat clock (Recovery 0.5, one second), allowed in a fight and in the dark (you find it by feel). A lantern with no oil: "The storm lantern has no oil left." A torch already lit: "It is already burning."
- `extinguish THING` (also `douse`, `snuff`, `put out`): puts a light out and keeps the time it has left. "You pinch out the pitch torch."
- `fill lantern` (also `refill`): pours one Flask of Lamp Oil into the lantern: +1800 s, up to 3600 s. "You pour the oil into the lantern, and the reservoir fills." A lantern already full: "The lantern is full." No flask: "You have no lamp oil." A flask poured into a nearly full lantern is spent in full; the lantern is simply filled.
- Lit things are shown as such: "a pitch torch (lit)", "a storm lantern (lit)". `look at torch` says roughly how long it has left, to the nearest five minutes ("about twenty minutes of burning"; "nearly out").
- There is no tinderbox: a character can always light what they carry. (A flint is a future detail.)

### Burn, warning and end
- Burn time runs only while lit, and only while the character is in the game. A torch warns once ("Your pitch torch burns low."; for a lantern "Your lantern's flame sinks; it is nearly out of oil.") and at the end: "Your torch gutters and dies." Others: "Keith's torch gutters and dies." (A lit torch lying on the floor: "The torch on the floor gutters and dies.")
- A burnt-out **torch is destroyed** ("what is left of it crumbles"). A burnt-out **lantern** stays, empty, and can be filled.
- **Water.** Carrying a lit light through ankle-deep water is safe. A lit light dropped in a room with `Wet = true` (the Flooded Tunnel) hisses out in the water: "The torch hisses out in the water." It is put out, not destroyed, and keeps the time it had left (a torch can be picked up and relit; a lantern with oil too). A room's `Wet` is a new builder field; deep water is future (section 15).
- **Dropped lit sources keep burning** and light the room; picking one up carries its light with it. A creature that picks one up (none do today) would carry it.
- **Dying.** A character's belongings go into a corpse; things in a corpse light nothing, and a light in a corpse is put out (keeping its time).

### Saving
- A torch or lantern keeps its state in the item's own properties (docs/BUILDING.md, "Quality and stacks"): `burn` (seconds of torch left) or `oil` (seconds of oil left). No property means a full one, so every shop-fresh item and every old save is right. Properties are saved with the character's carried items as they are now.
- While lit, the item writes its remaining time into the property every 60 seconds and whenever it is put out; the character's save routine (`SaveCarried` in `/std/adventurer`) asks every carried light to write its time and **put itself out** first. A lit light is never saved lit: when you leave the game your light goes out (decision 9). A crash loses at most a minute.
- Torches are not stackable (each has its own time); two fresh Pitch Torches still collapse to "a pitch torch (x2)" in `inventory`, which collapses identical name, quality and state.
- The Rag Torch and Pitch Torch are different files; neither sells for more than half its Value (4 and 0), so buying and selling cannot make gold.

## 4. The craftable starter torch: the Rag Torch
- **The recipe** (a new file `mudlib/recipes/hollow_light.cs`, class `CrLightRecipes`, one recipe):
  `Recipe("rag_torch", "Rag Torch").Needs("cloth_strip").Needs("tallow").Skill("tinkering", "simple").Makes("/domains/hollow/obj/rag_torch", 2).Description("Wind a strip of cloth tight round a stick and soak it in tallow.")`
  with no `.At(...)`: **no station**, so it can be made anywhere (a Simple recipe, like the Simple Bandage). Hmm, the description speaks of a stick, but the recipe has no stick: the description is written "Roll a strip of cloth tight, work tallow into it, and bind it round the handle of a broken tool." That keeps the story honest with two ingredients.
- **Skill.** Tinkering (the "utility items and tools" craft), difficulty Simple (work level 0), so a character with 0 Tinkering makes one and **needs no skill**; it also teaches Tinkering a little (the growth rule: work in the easy band rolls at half the rung's chance), which the Traveler's Leather Wrap will later want.
- **Materials, all bought for coppers in the Hollow.** `cloth_strip` (2 gold each at Tobin Fettle and Elspeth) and a new material, **Tallow** (`tallow`, categories "fat, fuel", Value 1, `/domains/hollow/obj/craft/tallow`, a stackable `/std/material`: "a lump of tallow"), **1 gold at Harland** (kitchen dripping) **and at Dunstan Skell** (scraped from the hides). Gathering: there is no gatherable thing in the Hollow itself (the berry bush, herb patch, wildflowers and thistle patch nodes are all in the Corridor), so nothing here is gathered; a later skinning step could return tallow from beasts (section 15).
- **Yield.** One craft makes **two** Rag Torches for 3 gold: 1.5 gold each, against 5 for a Pitch Torch. The pricing rule (docs/CRAFTING.md, section 6) holds: the output is worth 0, so no vendor loop exists. A character with 10 gold can buy three crafts' worth of materials (6 torches, an hour of light) for the price of two Pitch Torches.
- **Burn.** 10 minutes against the Pitch Torch's 25, and it warns at 30 seconds: **worse**, so the shop torch keeps its place (it burns two and a half times as long for 3.3 times the price, and is worth Value 4 in a shop's eyes; the Rag Torch is worth nothing). Per hour of light, a Rag Torch costs 9 gold and a Pitch Torch 12: the craft is cheaper in cash and worse in convenience (six torches to light and carry an hour).
- **How a new player learns it exists.** (1) The card in the Tavern Cellar (section 11), which says "cloth strip and tallow, wound tight, makes a rag torch". (2) **Harland teaches it, free**: `AddLesson("rag_torch", 0)`, so `learn rag torch` at the bar. (3) Harland talks about it: his topic on "torch, light, dark" (section 11). (4) `combine cloth strip and tallow` teaches it by experiment (it is a known recipe, so experiment finds it, as the crafting rules say). The first of these is at the stair and the second is the person who owns the cellar: Harland "tells them".
- `tests/craft_sim.py`'s pricing check and the missing-file check cover the new material, recipe and torch.

## 5. What darkness does
Everything in this section uses the room's light **as the character sees it** (their own sources count: the room is lit if their torch makes it so).

### `look`
- **Lit:** as today.
- **Dim:** the whole room as today, with one more line at the end: "It is dim here."
- **Dark:** the room's name with "(dark)" ("Flooded Tunnel (dark)") and then:
  `It is too dark to see.` plus the room's own line `DarkDesc` (what the senses give: sound, smell, cold) or, if the builder gave none, `Nothing can be made out.`
  Then the exits line: `You feel your way: the way you came in is {cmd}west{/}. There may be others; try a direction.` Creatures and items are not listed. If the character is fighting, one line for each enemy: `Something is fighting you.`
- **Exits can be guessed.** Moving works in the dark: a direction that is an exit works as always. A direction that is not one says "You grope along the wall and find only stone." and costs nothing. The character is told the way they came in, and every exit they have used from this room since they logged in is remembered for the exits line (not saved). A closed door answers with the door's own refusal, which names it. This is done in `Room.Init` by binding the ten compass words that are not exits to a handler that does nothing in a lit room and says the line in a dark one, so no `What?` tells you whether you mistyped.
- `look at THING` in the dark: "It is too dark to see that." For something in your own pack, "You feel it over, but cannot see it properly." (`inventory` and `wield`, `wear`, `eat`, `drink`, `quaff` of things you carry all still work, by feel.)
- **Arrivals.** In a dark room, other characters' arrivals, departures and deeds are told as "Someone" and creatures' as "Something" ("Something arrives.", "Something attacks you!").
- **A warning once on entering a dark room unlit:** "{yellow}It is pitch black here. You will want a light.{/}", and, if you carry an unlit light, "({cmd}light torch{/})".

### Fighting
- **Targets.** You cannot name what you cannot see: `kill goblin` in the dark says "You cannot see a goblin." The words `something` and `it` always work: they pick, among the creatures in the room, the one you are already fighting, or if you are not fighting, one chosen at random (never a person who is "no threat to you"). `aim something` and `bolt something` work the same. `consider` needs sight. A fight already begun is unaffected by a light going out: you keep your enemies.
- **To hit.** A character who cannot see in the room's light gets, on every attack roll:

| Light | Melee | Ranged (the wielded weapon is a bow) |
|---|---|---|
| lit | 0 | 0 |
| dim | -1 | -2 |
| dark | -3 | -6 |

  Ranged is deliberately double. One point is 5% on the d20 (a natural 1 and 20 stay as they are). Spells that need no hit roll (Arcane Bolt) are not affected; they are dodged as usual. Aimed Shot keeps its +3 and its multipliers; in the dark it is simply harder to land.
- **Messages.** In the dark the character reads "Something hits you for 3 damage.", "You miss something.", "Something dodges your attack." Everyone in the room who can see reads the full names; a helper (`TellRoomLit(room, fullText, darkText, ...)`) sends two versions of a line. First used for movement and combat lines.
- **Creatures' own sight.** A creature has `DarkSight` (a field of `/std/monster`, default **true**): it sees the dark as a lit room for its own to-hit and for fighting, and as a dim room for noticing a sneaker (section 8). The **only creature in the cellars with `DarkSight = false` is the Giant Cave Toad** (pale, unblinking eyes; it feels the water, not the shape): it suffers the player's own to-hit penalties (melee dim -1, dark -3), and it still engages anything that comes into its water but cannot notice a sneaker that carries no light beyond the 5% floor. Goblins (scavengers, warriors, guards, the keyholder, the Elite Guards, the Overseer), rats and spore bats all see in the dark. **A blinded creature** (no source of blindness exists yet; a future flash, smoke or spell) is treated as `DarkSight = false` in a lit room too: it fights at the dark penalties and engages nobody it was not already fighting.
- **Lighting in a fight.** `light` is a quick action and works mid-fight: the sooner the better. When the state changes, the next attack uses the new numbers.

### Other things in the dark
| Thing | Dim | Dark |
|---|---|---|
| `read`, `look at` a detail | as lit | "It is too dark to read." / "too dark to see that" |
| `search` (docs/specs/search.md) | -2 to the check | -5 to the check, except for things found by feel (a draught, a smell, a loose stone) |
| `harvest`, `skin`, `bleed` | -5 to the harvest quality score | refused: "It is too dark to see what you are doing." |
| `craft` (anything, with or without a station), `combine` | quality shift -0.25 tier | refused, the same line |
| `get` a floor item, `loot`, `get ... from corpse` | as lit | refused: "It is too dark to see what is here." (Coins and items are not seen.) |
| `browse`, `buy`, `sell`, `value`, `lessons`, `learn` | as lit | refused: "It is too dark to see the goods." (no shop is dark today) |
| `rest`, `inventory`, `quaff`, `eat`, `wield`, `wear`, `flee`, `score`, `skills` | as lit | as lit |
| `heal`, `bless`, `glow` on a person by name | as lit | allowed: you know your friends' voices (one rule for all three) |

### Aimed Shot in the dark
It is the same action, the same +3 and the same opener (x2.5 to x3.5), with the ranged penalty of the table: against a goblin scavenger in a dark room a level 3 Scout's first Aimed Shot lands 50% of the time instead of 80% (section 8). `aim something` is how it is aimed when you have not seen the target.

## 6. Reading the penalty
The penalty is applied in one place: `Living.Attack`, before the roll (`roll + QueryAttack() + action.ToHitBonus + lightPenalty >= need`), where `lightPenalty` comes from `/secure/light`: `ToHitPenalty(roomLightState, ranged, sees)`. `ranged` is "the attacker's wielded weapon is a bow", already known as `QueryWeaponCategory() == "bow"`. `sees` is true for a creature with `DarkSight` and for any creature in a lit room. It is one function, so one place to tune.

## 7. The Light spell: Glow
A new spell, `mudlib/skills/glow.cs` (class `GlowSpell`), granted by `classes/mage.cs` and `classes/priest.cs` (their `Skills` lists gain `"glow"`; existing characters get it at their next login because `RefreshClasses` reads the class files each time).

- **Command** `glow` (casting on yourself) or `glow NAME` (on a friend in the room). It is a spell with a wind-up like Blessing: Preparation 0.5, Recovery 1.0.
- **Effect.** A timed effect named `light` with a modifier `light=N`. The `/secure/light` daemon reads the sum of this modifier over the creatures in a room, clamped to 0 to 2 for each, so nothing can add more (effects can be added by any object).
- **Rank table**, in the style of docs/specs/skill-ranks.md:

| Calling | Skill | I (level 1) | II (level 5) | III (level 10) |
|---|---|---|---|---|
| Mage, Priest | Glow | +1 light, 5 minutes, 20 Focus | +1 light, 8 minutes, 23 Focus | +2 light, 12 minutes, 26 Focus |

  `MaxRank = 3`: rank III is the last (like Tracking, which stops at II). The costs follow the default step (+15% of rank I per rank), the durations and light values are in the skill's own rows (`Effect("light", 1)`, `Effect("seconds", 300)`, `RankRow(2, "seconds=480;focus=23")`, `RankRow(3, "light=2;seconds=720;focus=26")`).
- **Trainers.** Rank I comes with the calling. **Rank II** is taught by Halbert Quill (Mage) at the Hedge-Scholar's Loft and by Sister Merewyn (Priest) at the Shrine of the Quiet Hand, each with `AddRankOffer("glow", 2, 20)` (no extra requirement; 20 gold, less than the usual 40 because the spell is a convenience, not a weapon). **Rank III** is taught at the Hall of Legends by the Mage's and the Priest's hallmasters, at the usual price (150 gold).
- **Messages.** Self: "A small, steady light gathers about you." Friend: "You call a small light about Keith." / "A small light gathers about you." Room: "A small light gathers about Keith." Expiry: "Your light fades." Recasting refreshes the effect; it does not add.
- **Voice.** The Mage's version in Halbert's mouth is "a candle you carry in your head"; the Priest's in Sister Merewyn's is "the Quiet Hand's one small gift to those who walk in the dark: it asks nothing and shows you the way". One spell file; the two trainers' topics make the difference (new `AddTopic` lines).
- The effect is not saved (effects are not): logging out ends it.
- A character carrying a Glow counts as carrying a light for sneaking (section 8).

## 8. Sneaking
`/secure/sneak`'s `NoticeChance(grace, scoutLevel, creatureLevel, alert, rankAdjust)` gains a sixth parameter, `lightAdjust` (default 0), added with the rank adjustment before the final clamp of 5% to 100%:

`chance = clamp(50 - 5 x (Grace - 5) + 5 x (creature level - Scout level), 10, 90) + 30 if Alert + rank adjustment + lightAdjust`, then clamped to 5 to 100.

`lightAdjust` = **stateAdjust + 20 if the sneaker carries a lit light of their own** (a torch, a lantern or a Glow).
- `stateAdjust`, from the room's light **including the Scout's own light**, as the creature sees it:

| Room is | Creature that sees in the dark (`DarkSight`) | Creature that does not |
|---|---|---|
| lit | 0 | 0 |
| dim | -10 | -10 |
| dark | -15 | -25 |

  A creature that sees in the dark sees the dark as a poor light, not as day. A creature that does not, and a sneaker with no light, leaves the 5% floor as the chance.
- The Scout's own light is a beacon (+20) whatever the room, and it also changes the room's state (a torch in a dark room makes it dim: -10, plus 20). The terms are one function, `/secure/light.NoticeAdjust(room, sneaker, creature)`, so the sneaking code calls one line.
- Bosses still always notice. `Alert` is unchanged. The ten-second re-rolls use half the chance, as now.

### Worked numbers: a level 3 Scout, Grace 9, Sneak rank I
Base notice = 50 - 5 x 4 = 30, plus 5 per level of the creature above 3. **Goblin scavenger** (level 2, `DarkSight`): 25. **Goblin Elite Guard** (level 4, Alert, `DarkSight`): 35 + 30 = 65.

| Room (base) | Scout carries | State | Scavenger notices | Elite Guard notices |
|---|---|---|---|---|
| lit (1) | nothing | lit | 25% | 65% |
| lit (1) | a torch | lit | 45% | 85% |
| dim (0) | nothing | dim | 15% | 55% |
| dim (0) | a torch | lit | 45% | 85% |
| dark (-1) | nothing | dark | 10% | 50% |
| dark (-1) | a torch | dim | 35% | 75% |
| dark (-1) | a lantern | lit | 45% | 85% |

For the Giant Cave Toad (level 3, not Alert, no dark sight; base 30): unlit in the dark 5% (30 - 25 = 5, the floor); with a torch (the room dim, -10 and +20) 40%; with a lantern 50%.

**The intended trade, in one number: the chance that the Scout is unnoticed at the door and then lands the opening Aimed Shot** (a level 3 Scout: Attack 3 + bow 1 + Aimed Shot 3 = 7; the creature's number to beat is 10 + Armor + Defense = 12 for the scavenger (armor 1, level 2) and 16 for the Elite Guard (armor 3, level 4); the hit chance is 5 x (21 - (need - attack - penalty)), at most 95; the bow penalty is 0, -2, -6 for lit, dim, dark):

| Room (base) | Scout carries | Scavenger: unnoticed x hit = both | Elite Guard: unnoticed x hit = both |
|---|---|---|---|
| lit | nothing | 75% x 80% = **60%** | 35% x 60% = **21%** |
| lit | a torch | 55% x 80% = 44% | 15% x 60% = 9% |
| dim | nothing | 85% x 70% = **60%** | 45% x 50% = 22.5% |
| dim | a torch (room lit) | 55% x 80% = 44% | 15% x 60% = 9% |
| dark | nothing | 90% x 50% = 45% | 50% x 30% = 15% |
| dark | a torch (room dim) | 65% x 70% = 45.5% | 25% x 50% = 12.5% |
| dark | a lantern (room lit) | 55% x 80% = 44% | 15% x 60% = 9% |

(60.0 and 59.5 are written 60.) Reading it: **sneaking in the dark and seeing well enough to aim pull in opposite directions, and no row wins everywhere.** With no light the Scout goes unnoticed far more often (90% against 55%) and then shoots at half strength (50% against 80%); with a lantern they shoot at full strength and are noticed nearly as often as in a lit room. The dim rooms with no light (the Guard Post, the Mushroom Cavern, the Overseer Hall) are the Scout's sweet spot: that is by accident of the numbers, and section 12 lists it as a hypothesis to measure. A Scout who is noticed still fights; they lose only the opener.

The melee Scout (Precision Strike I, +4 to hit, attack 7 with a weapon of no bonus) has the same unnoticed column and a gentler hit column: against the scavenger 80% / 75% / 65% for lit / dim / dark, against the Elite Guard 60% / 55% / 45%.

## 9. Parties and other players
There is no party system. Light is a property of the room, so everyone present gets the benefit of everyone's light and every light's carrier counts in the sneaking numbers only for their own beacon. If a party system arrives nothing here changes. Other players are "Someone" in the dark, as creatures are "Something"; a friendly spell by name still works.

## 10. Performance and where the code lives
The daemon `/secure/light` (new, blueprint only, nothing saved) keeps the rules and the list of **lit** sources and light carriers (a handful at any time), and a cached value for each room that has been asked.
- **The level of a room** is `base + sum`, where the sum is over the registered lit sources whose room, walking up from the source (the room itself, or the creature carrying it, or nothing if it lies in a container), is that room, plus the light effects of the creatures in it. Asking costs a few comparisons, and the answer is **cached per room** and recomputed only when marked dirty.
- **A room is marked dirty (and the new level announced if its state changed) by:** a source lit, put out, burnt out, filled, or hissed out (the source calls the daemon); a Glow beginning or expiring (a callout at its end); a creature moving through `Room.MoveThrough` (both rooms); a pick-up or drop (`Pick`, `CmdDrop`); a death (the belongings go to a corpse); a login or logout; a source being created or destroyed. These are the places a lit thing moves, and they are few.
- **A safety net:** every 10 seconds, while anything is registered, the daemon refreshes the rooms that have a registered source. It heals anything the hooks missed and it is all the cost there is when nobody is lit. An unregistered world (nothing lit, nobody Glowing) does nothing at all.
- **`look` and every attack read the cached value**; neither adds up anything. The to-hit rule asks the room once per attack.
- **Trust.** Only items in the trusted trees (`/std`, `/secure`, `/domains`, `/classes`, `/skills`, `/events`) may register as sources, and only through the item's own lighting code, from the character's own command: a wizard's object under `/wiz` lights nothing. Each source and the total are clamped, so no object can make daylight or a pit.
- **Dice and the clock.** `/secure/light` has a test hook for the burn clock, settable only by trusted code for a wizard of level 10 (the pattern of `/secure/sneak`): `SetTestBurnScale(n)` runs burn time n times faster, so a test need not wait twenty-five minutes.

## 11. The cellars, room by room
Reasons are what players should be able to read in the room. A "wall lantern" is a new furniture file (`/std/wall_light`: not gettable, never goes out, +1, listed in the room as "A storm lantern hangs from a hook.").

| Room | Base | Furniture | Light | State unlit | Why / the line added |
|---|---|---|---|---|---|
| Tavern Cellar (in the Hollow) | 0 | a storm lantern on a nail by the stair | 1 | **lit** | Harland keeps his own cellar lit. New: the card and the bin. |
| Collapsed Passage | 0 | a storm lantern hung on a prop | 1 | **lit** | "Someone hung a lantern on one of the props, and the beam above it is black with soot." The first fights (rats) are in the light. |
| Storage Chamber | 0 | a storm lantern on a crate | 1 | **lit** | "A lantern burns on the nearest crate. Whoever hung it meant people to find their way." |
| Smuggler Hall | 0 | a storm lantern on the first of the rusted hooks | 1 | **lit** | "One of the old hooks has a lantern on it again, the only one; the rest wear their crusts of tallow." The last lit room: it says so. |
| Guard Post | 0 | none | 0 | **dim** | The goblins' fire pit is cold; the glow of the Smuggler Hall spills in. A gentle first dimness for the fight with the keyholder. |
| Flooded Tunnel | -1 | none | -1 | **dark** | "Distant drips echo through darkness" is already its description. First true dark, with the Giant Cave Toad. `Wet = true`. |
| Mushroom Cavern | 0 | none (the fungus is the room's base) | 0 | **dim** | "Bathe the chamber in eerie blue light". Dim by day and by night. |
| Goblin Barracks | -1 | none | -1 | **dark** | Goblins sleep in the dark; the cracked lantern in the heap is dead. |
| Hidden Cache | -1 | none | -1 | **dark** | A sealed stash, untouched for decades. Makes the search (docs/specs/search.md) need a light. |
| Overseer Hall | 0 | none | 0 | **dim** | "A rush-light gutters in the eye of a nailed skull." Dim enough to matter, light enough to fight the Elite Guards. |
| Overseer Chamber | 0 | a black iron lamp on the table | 1 | **lit** | "An iron lamp burns on the table among the maps; the Overseer reads by it." The boss fight is not blind. |

The three dark rooms' `DarkDesc` lines:
- Flooded Tunnel: "Cold water laps at your ankles. Somewhere ahead drops fall into the dark, and the sound goes a long way."
- Goblin Barracks: "The dark is thick with old straw, smoke and sour meat. The air feels crowded."
- Hidden Cache: "The air is dry and still, and smells of rust and old canvas. Your steps ring a little off close stone."

**New players meet the dark late.** The first three rooms (Tavern Cellar, Collapsed Passage, Storage Chamber) and the Smuggler Hall are lit, so the first fights (rats, scavengers) happen in full light. The first dimness is the Guard Post, once the player has learned to fight; the first dark is the Flooded Tunnel.

**A new player's first torch and who warns them.**
- **The free torch.** A new keepsake find in the Tavern Cellar (`/domains/hollow/obj/torch_bin.cs`, a `/std/find` with `ItemPath = "/domains/hollow/obj/rag_torch"` and no gold): "a bin of old torches by the stair". Every character may take one, once (the register, `/secure/finds`; the item is worth nothing).
- **The card.** A readable on the stair post (`AddReadable("card,post,notice,sign", ...)`):
  "HARLAND'S CARD, tacked to the stair post.\n  The lamps end at the Smuggler Hall. Past it is dark, and I will not hear a word about it after.\n  One torch from the bin, free, the once. More at the bar: five gold, the pitch torch, which burns longer.\n  A cloth strip and some tallow, wound tight, make a rag torch if you have the wit. Ask me and I'll show you."
- **Harland** (`/domains/hollow/npc/harland.cs`, a trainer) gains `AddLesson("rag_torch", 0)`, the stock `pitch_torch` 5, `lamp_oil` 4 and `tallow` 1, and a topic on "torch, torches, light, lantern, dark, darkness, lamp, lamps": "I hung every lamp on that wall myself, down to the Smuggler Hall, and I'll not go past it with less than a torch. There's a bin by the stair, free, the first one; take it. After that it's five gold at my bar, or tallow and cloth if you'd rather make your own. A lantern is Tobin's trade, and a good one, but you'll not want it yet."
- **Tobin Fettle** gains `storm_lantern` 30 and `lamp_oil` 4 and a topic: "A storm lantern: thirty gold, and oil is four a flask. It will see you through the dark and not go out when you wave it about, which is more than I can say for a torch."
- **Dunstan Skell** sells `tallow` at 1 gold.
- **The Guild:** Rowan's talk about the quest "Clear the Cellar", and the notice board's line for it, gain "Bring a light."
- **Halbert Quill** and **Sister Merewyn** gain the Glow topic of section 7.

## 12. Balance: hypotheses to measure (do not run yet)
Each is a guess, to be tested with `tests/balance_sim.py` once the penalties are in (the simulator learns light states: each room's base, the carried light, and the table in section 5).

1. **The lantern is the right light for the cellar.** A level 4 to 5 character with a lantern (every dark room lit) wins the Overseer run about as often as today; with torches (dark rooms dim) a few points less; with no light, clearly less (by about 8 to 15 points for the archer Scout, 4 to 8 for the others). Measure: Overseer kill rate from the cellar entrance, per calling, no light / one torch / lantern, with the standard test (level 5, starting gear, 4 potions, middle human, RestFactor 5).
2. **The archer Scout loses most without light, and the Scout in dim rooms is the strongest.** The dim rooms with nothing carried are the best spots for an unnoticed, landed opener (60% against the scavenger). Measure the archer and the melee Scout, with and without sneaking, by room.
3. **Mage and Priest are nearly unaffected** (Glow; Arcane Bolt needs no hit roll), the Warrior and Adventurer lose a little in melee, and the cost is their gold. If the Mage is far ahead of the others in the dark, the next lever is a Glow that costs more or lasts less, not the penalty.
4. **Gold.** A first pass through the cellars earns about 134 gold (docs/BALANCE_HUMAN.md) and a whole level 1 to 5 career takes about an hour of play: light costs 10 to 15 gold in Pitch Torches (3% to 10% of what is earned), or 38 gold for the lantern and two flasks. Measure how many torches or flasks a real run burns.
5. **Time.** The pace of levels 1 to 5 (62 to 69 minutes with resting) should not change by more than a few percent: light costs a quick action at the cellar door. Measure.
6. **The first dark is survivable.** A level 2 to 3 character with the free Rag Torch in the Flooded Tunnel (dim) against the Giant Cave Toad (which cannot see) wins as often as today, within 5 points. Measure.
7. **Searching for the Recurve** (docs/specs/search.md): with the cache dark, a character without a light finds it 20% a try (35% for a Scout), with a torch 35% (50%), with a lantern 45% (60%). Check the average time to find it is a few minutes.
8. **The Toad.** With its own to-hit penalty and no sight, it should be easier by a few points in the dark; confirm that it is not trivial.

What to record for each: win rate and its spread across callings, Vitality lost, minutes, gold spent on light, torches burnt, and the sneak-through rate.

## 13. Negative light: listed, not built
Items and effects may have negative light; the engine already clamps and adds them. Not built now, for later content:
- **Gloom moss** (a plant in the deep cellars or the Barrow): -1 to the room it grows in.
- **A smoke pot** (a thrown consumable): -2 for 60 seconds in the room. A Scout's tool.
- **A shadowed cloak** (equipment): -1 around the wearer; helps a Scout, at the price of the party's sight.
- **A snuffed lamp** (a trap): a creature or object that puts out the room's lantern.
- **A gutting wind or a creature of the dark** that eats light: a boss-room effect.
- **A cursed candle** that burns at -1 and cannot be put out.
Each is one source with a negative value; none changes a rule.

## 14. Outside the cellars; night and weather
- **Oakenfell and the outdoors are lit.** Every room in Vincent's Hollow, the Corridor and Oakenfell stays base 1, so nothing above ground changes. Dark candidates for later: the Corridor's dungeons (the Root Warrens, the Forgotten Barrow, the Elder Lane's far end, the green road's dip at `green_road/m10` with its gloom), where the same fields apply.
- **Night, weather and the sun are future and not specified here.** The engine would need a clock and a rule that changes the base of outdoor rooms by the hour and the weather (and it is a different level of design than a number on a room). The scale here would take it: night is an outdoor room at 0 or -1, a lantern or torch makes it lit.

## 15. Other places the light touches
- **`score`.** A line after the Vitality line when it matters: "The light here is dim." / "It is dark here." (nothing when lit), and "You carry a light: a pitch torch (about 20 minutes left)." when one of yours is lit.
- **Prompt token `{light}`** (the one table of prompt tokens in `/std/adventurer` gains it): "(dim) " in a dim room, "(dark) " in a dark one, nothing when lit. Not in the default prompt.
- **GMCP `Room.Info`** gains `"light": "lit" | "dim" | "dark"` (the state the character sees, with their own light), and a change of state sends it again (the daemon's key includes it). A dark room's `name` is the room's real name and `exits` is only the ways the character knows (the way in, and any they have used), so the mapper learns nothing the text did not; the Mudlet script draws a dim room or a dark room in a different colour (the script is untested, as the rest of the package). Nothing about creatures or items is ever sent.
- **The practice dummy** (`/domains/hollow/mob/practice_dummy`, in the Practice Yard) needs no sight and is not changed; the Yard is lit. A wizard may stand with a dummy in a dark room to see the penalties in `verbose`.
- **Shops** are lit today; a shop in a dark room refuses `browse`, `buy` and `sell` (section 5).
- **Corpses.** A corpse and everything in it is unseen in the dark and lights nothing.
- **The toy lantern** in `/domains/town` is left alone (it lights nothing); it answers only to its own name.

## 16. Files to change
| File | Change |
|---|---|
| `mudlib/secure/light.cs` (new) | The rules and the registry: `QueryLevel(room)`, `QueryState(room)`, `ToHitPenalty`, `NoticeAdjust`, `Changed(room)`, the 10-second sweep, `SetTestBurnScale`. |
| `mudlib/std/room.cs` | `Light` field (default 1), the constants, `Wet`, `DarkDesc`; `LongFor` and `Init` honour the state (dark look, grope handler, dim line); `MoveThrough` marks both rooms; `GmcpRoomFacts` carries `light`; `QueryBaseLight`; `stat` lists the light. |
| `mudlib/std/living.cs` | `Attack` applies the penalty; `QueryDarkSight`; the "something" labels in the combat lines; `TellRoomLit`. |
| `mudlib/std/monster.cs` | `DarkSight` (default true); `Noticed` gives the blind creature its rule. |
| `mudlib/std/adventurer.cs` | `light`, `extinguish`, `fill`; `SaveCarried` puts lights out and records their time; `NoticeRoll` passes `lightAdjust`; dark refusals for the commands in section 5; the prompt token; `score` line; dark exits memory; arrival messages. |
| `mudlib/std/light_source.cs`, `mudlib/std/wall_light.cs` (new) | The source item (value, burn, warning, fuel, properties) and the furniture. |
| `mudlib/secure/sneak.cs` | `NoticeChance` gains `lightAdjust`. |
| `mudlib/secure/crafting.cs` | Dark and dim rules for gathering and crafting. |
| `mudlib/secure/gmcp.cs` | `light` in `Room.Info` and in its key. |
| `mudlib/std/shopkeeper.cs`, `mudlib/std/trainer.cs` | The dark refusal. |
| `mudlib/skills/glow.cs` (new), `mudlib/classes/mage.cs`, `mudlib/classes/priest.cs` | Glow. |
| `mudlib/recipes/hollow_light.cs` (new), `mudlib/crafting/launch_materials.cs` | The Rag Torch recipe; the `tallow` material. |
| `mudlib/domains/hollow/obj/` | `rag_torch`, `pitch_torch`, `storm_lantern`, `lamp_oil`, `torch_bin` (a find), `wall_lantern`, `table_lamp` (new files); `craft/tallow` (new). |
| `mudlib/domains/hollow/` | The eleven rooms' `Light`, furniture and lines (section 11), `cellar.cs` (the card and the bin), `npc/harland.cs`, `npc/tobin.cs`, `npc/dunstan.cs`, `npc/halbert_quill.cs`, `npc/sister_merewyn.cs`, `npc/rowan.cs`, `village/guild.cs` (the board), `mob/giant_cave_toad.cs` (`DarkSight = false`). |
| `tests/lint_area.py` | Check 16: `Light` is an integer from -3 to 1 and a room below 1 has a `DarkDesc`; a room with `Wet` is not lit by an unquenchable lamp in reach (a warning); a room with a shopkeeper is not below 1 (a warning). |
| `tests/light_test.py` (new), `tests/sneak_test.py`, `tests/gmcp_test.py`, `tests/craft_sim.py`, `tests/craft_content_test.py`, `tests/hollow_expansion_test.py`, `tests/balance_sim.py` | Section 17. |
| `docs/BUILDING.md`, `docs/GAME_SYSTEMS.md`, `docs/CRAFTING.md`, `docs/MUDLET.md`, `help` | A "Light" section for builders; the rules; the recipe; the mapper colours; `help light`. |

## 17. Tests
`tests/light_test.py` (new), real characters over TCP:
- **The level.** Base and sources sum and clamp; thresholds at 1 and 0; default 1; a torch in a dark room makes it dim, a lantern lit; Glow's rank III makes two; torch and Glow together light a dark room; a lit source in a corpse lights nothing; a lit source in another room lights nothing here; an out source adds nothing.
- **Commands.** `light`, `extinguish`, `fill` with each success and refusal; a lit light is listed as lit; time left shown; quick action in a fight; works in the dark.
- **Burn.** With the burn clock sped up: the warning, the end, the torch destroyed, the lantern kept; a torch put out keeps its time; relit later it burns what remained.
- **Water.** Carried through the Flooded Tunnel stays lit; dropped there hisses out and keeps its time.
- **Saving.** Lit, quit, log in: it is out, with the time that was left; a crash (kill the driver) loses at most 60 seconds; an old save with a torch and no `burn` property has a full one.
- **Messages.** The change of state is announced to the room each way (dark to dim, dim to lit and so on).
- **Darkness.** `look` in each state; the exits guess; creatures and items hidden; `kill goblin` refused and `kill something` works; "Something" in the combat lines; every refusal of the table in section 5.
- **To-hit.** With the dice fixed (the existing probe), the exact rolls that hit at each penalty, melee and ranged; Arcane Bolt unaffected; a creature with `DarkSight` unaffected; the Giant Cave Toad penalised.
- **Sneak.** `NoticeChance` with `lightAdjust` for every row of the section 8 table, both creature kinds; the worked numbers agree with the document; a Boss still always notices.
- **Search.** The light penalty on the check once docs/specs/search.md is in (dim -2, dark -5, by feel excepted).
- **Glow.** Rank table, cost, duration, expiry, on a friend, trainers' offers at the loft and the shrine, and the Hall of Legends.
- **Crafting.** The recipe is learnt from Harland free, by the primer-less `combine`, and made with no station; two torches come of it; Tinkering rises; `craft_sim.py` pricing; Harland sells tallow and torches; Tobin the lantern and oil; Goblin Blood still cannot be stocked.
- **Free torch.** The bin gives one Rag Torch per character, once, to a character, not a wizard.
- **GMCP.** `light` in `Room.Info`, resent on a change; a dark room's exits only the known ones; no creature or item in any package.
- **Security.** A wizard-written object cannot register a source, cannot clear another's light, cannot set the burn clock, cannot give a light beyond the clamp; an effect with a huge `light` is clamped.
- **Content.** Every cellar room's light and state as in section 11 (walk each with and without a torch), each `DarkDesc`, the card, the bin, each topic.
- **Existing tests** that walk the cellars (`hollow_expansion_test.py`, `playthrough_test.py`, `story_test.py`, `aim_test.py`, `rest_test.py`, `sneak_test.py`) must either carry a light or run with the room lights at their default; the build order below makes that a switch, so they pass at each stage.

## 18. Build order
Each stage can be merged and played, and the dark is the last thing switched on.
1. **The engine, nothing visible.** `/secure/light`, `Room.Light` (default 1), the wall lantern furniture, the daemon's cache and sweep, `stat` and GMCP's `light` field. Nothing changes for a player (every room is lit).
2. **Light sources.** `/std/light_source`, the Rag and Pitch Torches, the Storm Lantern, lamp oil, `light`, `extinguish`, `fill`, burn, saving, water; Harland's and Tobin's stock; tallow and the Rag Torch recipe; the free bin and the card; Harland's lesson and topic. Players can buy and make light and it does nothing yet.
3. **Darkness.** The dark and dim rules of section 5 (look, targets, to-hit, the other commands), `DarkSight`, the Toad, the labels, the prompt token and `score`. Still no room is below 1, so nothing is dark in play; tests set a room's light.
4. **Sneak.** The `lightAdjust` term, `NoticeAdjust` and its tests.
5. **Glow.** The spell, its ranks and trainers.
6. **Switch the cellars on.** The eleven rooms' lights (section 11), the lines of text, the lint check; then run the simulator for section 12 and tune the numbers, the cellar states and the prices in their one place each before the owner plays.

## Out of scope / future
- Night, weather and the sun (section 14), and the Corridor's and Oakenfell's dark places.
- Negative-light content (section 13).
- Deep water and swimming (a torch lost in deep water), and a light that a creature can snuff.
- Tallow from skinning beasts (a "fat" yield on the beast table), and torches improved by crafting quality (a better rag torch burns longer).
- A tinderbox or flint, a hooded (dark) lantern that can be shuttered and unshuttered, and other kinds of lantern.
- Blindness effects, a blinding spell and a Flash for creatures; a Mage's Glow that harms light-shy creatures.
- Stations that give light (a cooking fire or a forge lights its room by +1).
- Party light, group commands, and a lit room that attracts wanderers.
- Light affecting archery against creatures in the next room (true sniping, docs/specs/hollow-expansion.md section 1).
- Dark sight as a calling trait or an item (a Ranger's, a goggle), and light-shy creatures.
