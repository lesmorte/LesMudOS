# Crafting and gathering

The engine for the owner's crafting brief ([specs/crafting-system.md](specs/crafting-system.md)), work item C1 of [PLAN-corridor-crafting.md](PLAN-corridor-crafting.md). This document is for two readers: a player who wants to know how it works, and the builder who adds the game's real materials, tools, recipes, stations, vendors and trainers on top of it. Every number is listed with the file it lives in. The builder parts are summarised in [BUILDING.md](BUILDING.md) ("Crafting"), and the system in [GAME_SYSTEMS.md](GAME_SYSTEMS.md) section 9a.

Contents: 1 For players. 2 How it is built. 3 The numbers and the odds. 4 Data formats with copy-ready examples. 5 How a builder adds things. 6 Pricing: no free gold. 7 Security and the sandbox. 8 Tests. 9 Appendix: the ten launch recipes and sixteen materials as data. 10 What the engine cannot yet express, and later passes.

## 1. For players

**Gather.** `skin` and `bleed` take the skin and blood of a dead beast; `harvest` gathers from berry bushes, herb patches, wildflowers and thistle. Each needs its tool, carried (not wielded): a skinning knife, a bleeding kit, a harvest sickle. The best one you carry is used automatically. Each is an action on the combat clock: the result comes after 2 seconds and you can act again after your Reaction Time plus 0.5, doubled (6 seconds at 2.5). None of them can be done while fighting.

- A corpse can be skinned once and bled once, and only if the creature had a skin or blood. Bleeding needs a fresh corpse: within five minutes of the death. A creature's corpse rots away ten minutes after the death, skin or no skin. A botched cut uses the corpse's one chance ("the hide is ruined").
- For the first 30 minutes only the killer (and, for a player's corpse, the dead player) may skin or bleed it, as for looting.
- `look at corpse` tells you whether its hide can still be taken and whether it is fresh enough to bleed.
- A plant has a few charges and grows back. A fumble at a plant costs you the time but not the plant.

**Make.** `recipes` lists what you know, grouped by where it is made; `recipe NAME` shows one in full; `craft NAME [COUNT]` makes it (COUNT times in a row; `stop` ends the batch after the current one). Simple recipes can be made anywhere; others need a station in the room (herbalist table, tanning rack, workbench, cooking fire). Crafting never fails and never wastes materials: low skill gives poorer results, high skill can give a bonus output or save an ingredient.

- **Quality** follows what you put in: the average quality of the ingredients, raised or lowered by your skill against the recipe's difficulty. An Excellent healing herb and a Fine wildflower make a Fine or Excellent salve.
- **Which units are used.** When you carry several qualities of one ingredient, the lowest is used, so good materials are not spent by accident. `craft NAME best` uses the highest; `craft NAME using fine herb [and ...]` names the stacks to take first. The result line says exactly what was used.

**Learn.** Nobody starts knowing a recipe. Trainers teach them (`lessons`, `learn NAME`), books teach them (`read` the book), quests can reward them, and you can find them yourself: `combine THING and THING [and THING]`. If the mix is a recipe you do not know (at the right station, if it needs one), you learn it, are told so, and make it once. If not, nothing is used up and you are told it makes nothing. Rare recipes are hidden, Secret ones are taught by nobody; the first character to find one of those by experiment enters the Chronicle.

**Grow.** `proficiencies` shows Skinning, Bleeding, Harvesting, Cooking, Herbalism, Tanning and Tinkering, 0 to 100, in numbers and words (untrained, novice 10, apprentice 25, journeyman 45, expert 65, master 85, grandmaster 100). Each successful piece of work near your skill has a chance of raising the proficiency it uses by a whole point; easier work has half that chance, and work far beneath you teaches nothing. The chance is not shown, but it rises with each success that does not teach and starts again when a point comes, so progress is steady but never certain, and it slows as you get better. There are no profession locks.

**Use.** Crafted food and drink are eaten or drunk (`drink juice`); bandages and salves are applied (`apply salve`), on the combat clock like a potion. Better quality heals more.

Help: `help crafting`, and `help skin` (or any of the command words).

## 2. How it is built

| File | What it is |
|---|---|
| `/secure/crafting` | The daemon: loads the data, indexes recipes, runs every crafting and gathering command, teaches, runs healing over time. Blueprint only, preloaded. |
| `/crafting/*.cs` | Data tables (`/std/craft_data`): `numbers.cs` (every number), `proficiencies.cs` (proficiencies, ranks, stations, gathering), `beasts.cs` (the skin and blood table). Builders add materials and node yields here, in files of their own. |
| `/recipes/**/*.cs` | Recipe files (`/std/recipe_set`), any number of recipes each, in any sub-directory (three levels deep). |
| `/std/material` | Base of a material item: stackable; its id and categories come from the materials table. |
| `/std/skin`, `/std/blood` | The skin and the blood of every beast: one object each, the kind held as the item property `kind`. |
| `/std/tool` | A gathering tool with a `ToolType`; its quality is the item quality. |
| `/std/station` | A crafting station with a `StationKind`. |
| `/std/crafted` | A crafted consumable whose effects and their scaling with quality are data. |
| `/std/recipe_book` | A readable item that teaches recipes. |
| `/std/trainer` | An NPC that teaches recipes for gold (`lessons`, `learn`). It is also a shopkeeper (`AddStock`), so one person can teach and sell. |
| `/events/craft.cs` | The Chronicle event types `craft.discover_rare` and `craft.discover_secret`. |
| `/std/adventurer` | A self-contained block at the end: the saved proficiencies and known recipes, their guarded setters, the command verbs. |
| `/std/corpse` | Remembers the creature's kind, level, skin and blood; each part taken once. |

**Recipes are data, found by listing.** The daemon lists `/crafting` and `/recipes` (the master lets only its blueprint do that, and only there), loads each file and reads its entries. There is no list to edit. It looks again every five seconds: a new file is picked up by itself, a removed one dropped, and an edited one after `update <file>` (which replaces the loaded object). Each load is reported in the driver log: `crafting: loaded 6 recipes, 28 materials, 7 proficiencies from 5 files in 217 ms`, followed by every problem and clash.

**Why C# data files, and several recipes to a file.** Data files in C# are the house style (classes and skills are the same), and `dotnet build` checks every one of them. Compiling costs about 50 ms a file, so a thousand recipes in one file each would take a minute at boot; in files of 50 they take under two seconds. A file per proficiency or per station, or per area, is a good size. Every class name in the mudlib must be unique, because `dotnet build` compiles them into one assembly.

**Materials: one table is the truth.** A material's id and categories are one line of a table: `Material("honey", "/domains/hollow/obj/craft/honey", "sweetener")`. The item file holds only its names and value. An item is a material if and only if its program path (and, for generic items, its `kind` property) is in the table: so a wizard's look-alike herb under `/wiz` is never an ingredient, a typo in a recipe is caught at load ("unknown material"), and the clash check knows every category's members without opening a file. Any item can be made a material by a table line, crafted outputs included (Berry Juice is an ingredient of Sweet Berry Juice).

**Skins and bloods without a file per creature.** `/std/skin` and `/std/blood` are one object each. Their kind is the item property `kind` (F1's instance state), set when the daemon makes one; it is saved with the item like quality and quantity (`/std/skin?p.kind=wolf&q=2&n=3`), stacks only with the same kind (properties are part of the stack rule), and is restored at login and from the lost-property store. When the kind is set or restored the object reads the beast table and takes its name ("Wolf Skin", "a vial of wolf blood"), ids ("wolf skin", "skin of wolf", "wolf_skin") and value. Each beast line makes two materials, `<kind>_skin` and `<kind>_blood`, in the categories `skin` or `blood` and the kind itself. Adding a beast is one line. A shop can stock one by record: `AddStock("/std/skin?p.kind=rabbit", 3)` (shops and `CreateIn` accept records now).

**Corpses.** When a creature dies, `/std/corpse` reads its public string properties `Skin` and `Blood`, its level and its kind (its `ChronicleKind`, else the file name), but only if the creature itself makes the `Setup` call and its file is in a trusted tree. A corpse with a part still to take does not crumble two minutes after it is empty, as an empty one does; once its parts are taken (or the blood has gone cold and there is no skin) it does. A creature's corpse rots 600 seconds after the death whatever is still in or on it, a skin included, so a hunting ground does not fill with skinnable corpses; the number is one field on the corpse, and a creature file may set its own with a public `int CorpseSeconds`. A player's corpse keeps its hour. Only the daemon may take a part (`TakeYield`).

**Commands and the combat clock.** The player body binds the verbs and forwards them to the daemon, which checks the caller is that very body during its own command. For an action the daemon registers a job and answers "act"; the body begins it with `ActAsAction` (queued if it is still recovering). The action's "skill" is the daemon, so when it resolves the clock calls the daemon's `Resolve`, which checks everything again (the corpse may be gone, a fight may have started) and does the work. A batch (`craft x 5`) queues its next step quietly at the next Ready Time (`Living.QueueAction`); `stop`, a fight, quitting or running out of an ingredient ends it.

**One craft is all or nothing.** The outputs are made first, inside the daemon, so a broken output file costs nothing; then exactly the planned units are split off (a refused split puts back what was split and destroys the outputs); then they are destroyed, the outputs handed over, the proficiency raised, and the character saved, all in one execution. The result message is sent after the save, so a crash after the message cannot lose the craft, and a crash before it leaves the save file from before the craft: either way nothing is doubled or lost. Gathering is the same: saved before the message.

**The matcher.** Recipes are indexed by their sorted ingredient tokens (`c:berry+m:clean_water`). An offering of up to three distinct materials is matched by trying, for each material, its own id or any of its categories, and looking each combination up: at most a few dozen dictionary lookups for three materials with a few categories each, whatever the number of recipes. Order and quality are ignored. **The clash rule:** one set of materials makes one thing. A recipe with more exact (material id) ingredients beats one with categories. Two recipes that are equally exact and could take the same materials (with no more exact recipe taking them) clash, and the one loaded later (by file path, then by its place in the file) is refused and written to the driver log: `crafting: CLASH: recipe 'black_sweet' (/recipes/zz_clash) clashes with 'berry_honey' (/recipes/zz_clash): both could be made from blackberries and honey; 'black_sweet' is not loaded`. Two recipes with the same ingredients always clash. Adding a material to a category can create a clash; the log says so at the next load. A recipe whose categories have more than 5,000 combinations is checked on its exact key only (logged).

**What is checked when recipes load** (each failure logged, recipe not loaded): a valid unique id; a name; one to three kinds of ingredient (`recipe.max_ingredients`), each a known material or a category with members, none named twice, counts 1 to 99; a known station or none; a known proficiency and difficulty; a discovery level of known, discovered, rare or secret; an output that is a known material or a path in a trusted tree, never a scarce material, count 1 to 100.

## 3. The numbers and the odds

Every number is in `/crafting/numbers.cs` (the daemon has the same values built in as a fallback, and `tests/craft_sim.py` reads the file). After changing one, `update /crafting/numbers`.

### Gathering

**Work level** (how hard the work is, 0 to 100): a corpse is `beast difficulty x 5 + creature level x 1` (`difficulty.beast_scale`, `difficulty.level_weight`); a node is `node Difficulty x 5` (`difficulty.node_scale`). A level-2 wolf is 4 x 5 + 2 = 22; a rabbit 6; a level-8 bear 48; a Difficulty-1 bush 5.

**Harvest Quality Score** (the owner's formula) = `harvest.base` 30 + proficiency + tool quality modifier (`harvest.tool_quality`: Poor -10, Common 0, Fine 5, Excellent 10, Masterwork 15, Legendary 20) - work x `harvest.difficulty_factor` 0.5 + random variation (`harvest.variation` 15: two rolls of 0 to 15 less 15, so -15 to +15, most often near 0). The quality: below 20 Poor, from 20 Common, 50 Fine, 80 Excellent, 110 Masterwork (`harvest.thresholds`), never above Masterwork (`harvest.max_quality` 4): Legendary does not come from harvesting.

**Success** % = 75 + proficiency x 0.2 + tool (Poor -10, Common 0, Fine 5, Excellent 10, Masterwork 15) - work x 0.4, between 30 and 98 (`harvest.success_*`). **A bonus unit** % = proficiency x 0.15 + tool (0, 0, 5, 10, 15, 20), at most 40 (`harvest.bonus_*`).

**A failed attempt.** At a corpse it uses the one chance: a botched cut ruins the hide and spilt blood cannot be gathered again. That is the fair choice because a corpse is one creature, retrying until it worked would make the success roll meaningless, and it makes skilled bleeders worth trading with for Goblin Blood. At a plant it costs the time (one action) but not the charge: the plant is still there, and a beginner is not punished twice. Because only successes teach (below), failing at a plant cannot be used to train for free.

The exact odds (from `tests/craft_sim.py`, which the test checks against the daemon to the tenth of a percent):

| Proficiency | Tool | Work | Success | Bonus | Poor | Common | Fine | Excellent | Masterwork |
|---|---|---|---|---|---|---|---|---|---|
| 0 | Common | 6 (rabbit) | 72.6% | 0% | 14.1% | 85.9% | | | |
| 0 | Poor | 6 | 62.6% | 0% | 64.5% | 35.5% | | | |
| 0 | Common | 22 (wolf) | 66.2% | 0% | 53.1% | 46.9% | | | |
| 25 | Common | 22 | 71.2% | 3.8% | | 78.5% | 21.5% | | |
| 50 | Common | 22 | 76.2% | 7.5% | | | 94.1% | 5.9% | |
| 50 | Excellent | 22 | 86.2% | 17.5% | | | 53.1% | 46.9% | |
| 75 | Common | 48 (bear) | 70.8% | 11.2% | | | 41.0% | 59.0% | |
| 100 | Common | 48 | 75.8% | 15.0% | | | | 69.5% | 30.5% |
| 100 | Masterwork | 6 | 98.0% | 30.0% | | | | | 100% |

`bleed.fresh_seconds` 300: bleeding needs a corpse younger than this.

**Time** (`time.gather` 1.0, 0.5, 0.0: preparation, recovery, reaction modifier, in units of 2 seconds): the result after 2 s, the next action after (Reaction Time + 0.5) x 2 s, 6 s at 2.5.

### Growth

A successful piece of work (a failed one teaches nothing and changes nothing) rolls a chance at **one whole point**. The chance comes from a ladder, `growth.ladder` 20, 25, 50, 75, 100 (percent). A character starts on the first rung; a roll that misses moves them to the next rung for the next success, and a gain sends them back to the first. Each rung is used `1 + skill / growth.repeat_every` times (`repeat_every` 20, whole points) before the next: skill 0 to 19 climbs 20, 25, 50, 75, 100; skill 20 to 39 climbs 20, 20, 25, 25, 50, 50, 75, 75, 100, 100; and so on. The last rung stays until the point comes. The bands: work more than `growth.easy_band` (30) below your skill teaches nothing and does not move the ladder; work 11 to 30 below (the easy band) rolls at `growth.easy_factor` (0.5) of the rung's chance, so it is never certain, and moves the ladder; work no more than `growth.near_band` (10) below your skill, or above it, rolls at the rung's chance. Anything that later speeds learning multiplies the chance, capped at 100%. Proficiencies are kept in tenths (0 to 1000) and rise by 10.

The position on the ladder is kept per character and per proficiency, saved with the proficiency (`craftLadder` beside `craftProficiencies` on the body), set only by the crafting daemon, read only by it, and never shown: `proficiencies` shows the skill and nothing about the chance. A save with no position starts at the first rung.

Expected successful pieces of work for one point, near your skill (the easy band at half chance in brackets), and for each band of twenty points (`tests/craft_sim.py` prints the table):

| Skill | Per point | Band of 20 |
|---|---|---|
| 0 to 19 | 2.78 (4.02) | 55.5 (80.3) |
| 20 to 39 | 3.58 (5.34) | 71.6 (106.9) |
| 40 to 59 | 4.04 (6.28) | 80.8 (125.6) |
| 60 to 79 | 4.33 (6.97) | 86.5 (139.5) |
| 80 to 99 | 4.52 (7.51) | 90.3 (150.1) |
| 0 to 100 | | 384.6 (602.4) |

So from 0 to 100, always working near your skill, takes about 385 successes (the launch area's work levels, 5 to 25, stop gathering growth near 40 to 55 and Simple/Novice recipes near 30 to 40, so higher skill must come from harder work later). The tests fix the growth dice with the test rolls: `mid` and `high` always win the growth roll, `low` always loses it (the work still succeeds), `fail` fails the work.

### Crafting

**Output quality** = round(average ingredient quality, weighted by count + shift + noise), between Poor and Masterwork (`craft.max_quality` 4). Shift = (proficiency - recipe work level) x 0.02 tiers, between -1.5 and +1.0 (`craft.shift_*`). Noise is two rolls of 0 to 20, less 20, divided by 20, times 0.75 (`craft.noise`, `craft.noise_steps`): -0.75 to +0.75, most often near 0. Work levels of the difficulties (`Difficulty` lines in numbers.cs): Simple 0, Novice 10, Apprentice 25, Journeyman 45, Expert 65, Master 85. Qualities count Poor 0 to Legendary 5.

**Bonus output** % = (proficiency - work - 20) x 0.5, 0 to 25. **An ingredient returned** % = (proficiency - work - 30) x 0.4, 0 to 20 (one unit of the first ingredient, at the quality used). **Time** (`time.craft` 1.5, 0, -1.0): 3 seconds a craft, and the next can begin then.

| Average quality | Proficiency | Work | Poor | Common | Fine | Excellent | Masterwork | Bonus | Return |
|---|---|---|---|---|---|---|---|---|---|
| Common (1) | 0 | 0 (Simple) | 6.3% | 87.3% | 6.3% | | | | |
| Common | 0 | 10 (Novice) | 17.7% | 81.6% | 0.7% | | | | |
| **2.5 (Excellent herb + Fine flower)** | **0** | **10** | | | **72.8%** | **27.2%** | | | |
| 2.5 | 50 | 10 | | | | 72.8% | 27.2% | 10% | 4% |
| Common | 100 | 10 | | 6.3% | 87.3% | 6.3% | | 25% | 20% |
| Common | 0 | 45 (Journeyman) | 87.5% | 12.5% | | | | | |
| Excellent (3) | 100 | 10 | | | | 6.3% | 93.7% | 25% | 20% |

The owner's example holds: a beginner gets a Fine or Excellent salve every time; a journeyman herbalist gets Excellent or Masterwork.

### Effects

A crafted consumable's amounts are multiplied by `effect.quality_scale`: Poor 0.6, Common 1.0, Fine 1.25, Excellent 1.5, Masterwork 1.8, Legendary 2.2 (rounded, at least 1). A Common Berry Juice restoring 8 restores 10 at Fine.

## 4. Data formats

### Proficiencies, ranks, stations, gathering (`/crafting/proficiencies.cs`)

```csharp
// @inherit /std/craft_data
public class CraftProficiencies : CraftData
{
    public override void Create()
    {
        Proficiency("skinning", "Skinning", "gathering", "Taking the hides of beasts.");
        Proficiency("mining", "Mining", "gathering", "Breaking ore from the rock.");      // a new one: one line
        Rank(25, "apprentice");
        Station("herbalist table", "Herbalist Table", "herbalism");
        Gather("skin", "skinning", "skinning knife");      // the command, the proficiency it uses, the tool type it needs
    }
}
```

`kind` groups the `proficiencies` display ("gathering", "crafting"). The reserved gathering skills (Woodcutting, Mining, Fishing, Prospecting, Mushroom Gathering, Hunting, Archaeology, Relic Recovery) are each one `Proficiency` line plus, for node-based ones, one `NodeKind` line (below). Every character has every proficiency from 0.

### The beast table (`/crafting/beasts.cs`)

```csharp
Beast("wolf", "Wolf", 6, 4, 4);                         // kind, display name, skin value, blood value, difficulty (1 to 20)
Beast("goblin", "Goblin", 0, 8, 4, scarceBlood: true);  // its blood can come from nothing but bleeding
```

Shipped: rabbit, rat, toad, bat, deer, wolf, goblin, boar, bear, panther. A skin value of 0 makes a worthless skin; leave the creature's `Skin` empty instead if it has none.

### Materials (any table under `/crafting`, for example `/crafting/hollow_materials.cs`)

```csharp
Material("honey", "/domains/hollow/obj/craft/honey", "sweetener");   // id, file, categories (comma-separated, may be empty)
```

and the material's own file, a few lines of data:

```csharp
// @inherit /std/material
public class HcHoney : Material
{
    public override void Create()
    {
        Name = "Honey";
        Ids.Add("honey");
        Ids.Add("jar");
        ShortDesc = "a jar of honey";
        Plural = "jars of honey";
        LongDesc = "Thick golden honey from the village hives.";
        Value = 3;
    }
}
```

Ids: lower case letters, digits and `_`. Categories are words; a recipe ingredient `NeedsAny("berry")` takes any material in the category. The file need not inherit `/std/material` (any item path can be listed), but `/std/material` makes it stack.

### Tools

```csharp
// @inherit /std/tool
public class HcSkinningKnife : Tool
{
    public override void Create()
    {
        Name = "Skinning Knife";
        Ids.Add("knife");
        Ids.Add("skinning knife");
        ShortDesc = "a skinning knife";
        LongDesc = "A short curved blade for taking hides.";
        ToolType = "skinning knife";      // as the Gather / NodeKind lines name it
        Value = 6;
    }
}
```

One file per tool type; the five qualities are the item quality (`AddStock(path, price, quality)` sells a Fine knife; quality multiplies the price). Fifteen tools are three files.

### Node yields

```csharp
Yield("berry bush", "blackberries", 1, 2);     // node kind, material, weight among that kind's yields, units per harvest
Yield("berry bush", "blueberries", 1, 2);      // a berry bush gives one or the other, evenly
NodeKind("copper seam", "mining", "mining pick");   // only for a kind not gathered with Harvesting and a sickle
```

The node kind is the node's `Kind`, lower case. A bush that should give only blueberries gets `Kind = "blueberry bush"` and its own `Yield` line. Node `Difficulty` (an integer, default 1) x 5 is its work level.

### Recipes (`/recipes/**/*.cs`)

```csharp
// @inherit /std/recipe_set
public class HcHerbalismRecipes : RecipeSet
{
    public override void Create()
    {
        Recipe("minor_healing_salve", "Minor Healing Salve")         // id, the name players see
            .Needs("healing_herb").Needs("wildflower", 1)          // material id, count (default 1)
            .At("herbalist table")                                  // station kind; leave out for "anywhere"
            .Skill("herbalism", "novice")                           // proficiency, difficulty
            .Makes("/domains/hollow/obj/craft/minor_healing_salve") // an item path, or a material id; count (default 1)
            .Discovery("known")                                      // known (default), discovered, rare, secret
            .Description("Warmed and worked into a paste.");         // optional, shown by "recipe"
        Recipe("berry_juice", "Berry Juice")
            .NeedsAny("berry", 2).Needs("clean_water")              // a category ingredient
            .At("cooking fire").Skill("cooking", "simple")
            .Makes("berry_juice");                                   // a material id: the output is that material
    }
}
```

At most three kinds of ingredient. Discovery levels: **known** (taught by trainers and books; also learnable by experiment), **discovered** (meant to be found by experiment; a quest or book may still teach it), **rare** (hidden; found by experiment or a rare book; the first experimental discovery is a Chronicle event), **secret** (`Teach` refuses it: experiment only; Chronicle event). A recipe has no code; its output's effects are in the output's file.

### Crafted effects (`/std/crafted`)

```csharp
// @inherit /std/crafted
public class HcMinorHealingSalve : Crafted
{
    public override void Create()
    {
        Name = "Minor Healing Salve";
        Ids.Add("salve");
        ShortDesc = "a pot of minor healing salve";
        Plural = "pots of minor healing salve";
        Value = 3;
        UseVerb = "apply";            // "eat", "drink" or "apply"
        HealPerTick = 3;              // healing over time: 3 Vitality
        Ticks = 6;                    // six times
        TickSeconds = 5;              // every 5 seconds
        // RestoreVitality = 8;       Vitality at once
        // RestoreEndurance = 20;       Endurance at once
        // QualityScale = new[] { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0 };   this item's own scaling, instead of numbers.cs
    }
}
```

`RestoreVitality` is Vitality restored at once. A character has one healing over time at a time; a new one replaces the old; it shows in `score` under its name. Use is an action on the combat clock (Recovery 0.5, like a potion). Why effects live in the output's file and not in the recipe: an output made by two recipes, bought from a vendor or found as loot behaves the same, and the file is the one place its value and effect are read.

A wearable with a recorded but inert property is ordinary armor with `CarryBonus = 1` (new on `/std/armor`): appraise and `stat` show it, a later carry limit can read `QueryCarryBonus()`, nothing uses it now (owner's decision 5).

## 5. How a builder adds things

**A station.** A file inheriting `/std/station` with `StationKind` set to a kind from the `Station` lines, placed with `AddSpawn(path)`:

```csharp
// @inherit /std/station
public class HcHerbalistTable : Station
{
    public override void Create()
    {
        Name = "Herbalist Table";
        Ids.Add("table");
        ShortDesc = "a herbalist table";
        LongDesc = "A scrubbed table crowded with mortars, jars and drying racks.";
        StationKind = "herbalist table";
    }
}
```

**A trainer.** An NPC inheriting `/std/trainer`; it is a `Shopkeeper` (and so an `Npc`): `AddChat`, `AddTopic` work as usual, and `AddStock` lines make it sell (`browse`, `buy`; with no stock it is not a shop). Players in the room get `lessons` and `learn NAME`.

```csharp
// @inherit /std/trainer
public class HcHealer : Trainer
{
    public override void Create()
    {
        Name = "Mother Alis";
        Ids.Add("alis");
        ShortDesc = "Mother Alis, the healer";
        LongDesc = "...";
        AddLesson("minor_healing_salve", 10);   // recipe id, price in gold (0: free)
        AddLesson("simple_bandage", 0);
    }
}
```

Any other trusted object teaches with one call; it returns `"ok"`, `"known"` or `"refused: why"`, tells the character and saves them:

```csharp
LoadObject("/secure/crafting").Call<string>("Teach", player, "hunters_poultice");   // a quest reward in a report() handler
```

**A recipe book.**

```csharp
// @inherit /std/recipe_book
public class HcHerbalPrimer : RecipeBook
{
    public override void Create()
    {
        Name = "Herbal Primer";
        Ids.Add("primer");
        ShortDesc = "a herbal primer";
        ReadText = "Crush a healing herb with a wildflower and work it into a paste over gentle heat.";
        Teaches.Add("minor_healing_salve");
        // ConsumedOnRead = true;   a scroll that crumbles once it has taught someone
        Value = 8;
    }
}
```

`read primer` shows the text and teaches every recipe the reader does not know (Secret ones never).

**A harvestable creature.** Two public string properties on the creature, and its kind in the beast table:

```csharp
public string Skin { get; set; } = "wolf";      // "" for none
public string Blood { get; set; } = "wolf";
```

The Hollow's creatures and the corridor's have them. A new beast is one `Beast` line.

**A vendor of materials.** An ordinary shopkeeper: `AddStock("/domains/hollow/obj/craft/honey", 3)`, `AddStock("/std/skin?p.kind=rabbit", 3)`, `AddStock("/std/blood?p.kind=wolf", 4)`, `AddStock(knife, 6, 2)` for a Fine knife. Goblin Blood cannot be stocked: the line is silently ignored (the blood refuses the kind). Shops buy everything at half its value, quality included.

**A node.** As in BUILDING.md ("A resource node"), plus a `Yield` line for its kind.

**Check your work.** `python3 tests/craft_sim.py` (pricing, Goblin Blood, missing files), `python3 tests/load_check.py`, and the driver log line after boot (`crafting: loaded ...`, then problems and clashes).

## 6. Pricing: no free gold

**The rule.** A recipe must never sell to a vendor for more than its ingredients cost from vendors, at any proficiency and for ingredients bought at any quality. In practice: **the output's value times its count must be at most about 60% of the summed values of its ingredients** (times their counts), and **no vendor may sell a material below its value** (base value x quality multiplier). Shops buy at half value; the best a grandmaster can do with Poor ingredients is lift them to Common (twice the value) with a quarter more output and a fifth of an ingredient saved, which 60% covers. A recipe that needs a material no vendor sells (Goblin Blood) is not a vendor loop and is free of the rule; it is where crafting pays.

`tests/craft_sim.py` checks it exactly: for every recipe, every purchase quality from Poor to Masterwork and proficiencies 0, 25, 50, 75 and 100, the expected sale (the exact quality distribution x value x half x expected count) against the cost (the cheapest shop price if a shop lists the material, else its value, which the rule makes the lowest possible price; for a crafted ingredient, the lesser of that and its cost to make, so chains are covered), less the expected value of a saved ingredient. It fails on any profit, any shop price below value, any recipe, node yield or shop stock of a scarce material, and any material or output file that does not exist. It runs the real mudlib and each fixture set laid over it.

## 7. Security and the sandbox

- **Goblin Blood** is made by nothing but bleeding a goblin's corpse. `/std/blood` refuses the kind of a scarce beast to every caller except the crafting daemon and a player body restoring its own belongings, and a refused property fails the whole restore, so a shop, `CreateIn`, a recipe (refused at load) or a node yield (refused at load) gets nothing. Even trusted test code cannot mint it (checked).
- **Entry points check `PreviousObject()`, never an argument.** `Command` and `Resolve` only for the player body itself; `CraftSetProficiency`, `CraftSetLadder` (and `QueryCraftLadder`, which tells only the daemon), `CraftLearn`, `CraftSave`, `CraftContinue` on the body only for the daemon; `Teach` and `StartHeal` only for code in a trusted tree; `TakeYield` on a corpse only for the daemon; `TryGather` on a trusted node only for trusted code; `SetTestRolls` and `WizardSetProficiency` only for trusted code acting for a level-10 wizard.
- **Only trusted things count**: tools, stations, nodes and creatures whose files lie in `/std`, `/secure`, `/domains`, `/classes`, `/skills` or `/events`; materials only as listed in the table (whose paths must be in those trees too). Data loads only from `/crafting` and `/recipes`.
- **What was tested from `/wiz`** (a level-1 wizard's gadget, `tests/craft_test.py` section N): teaching, learning, setting a proficiency, the wizard setter, fixing the rolls, running a command or resolving an action for a player, taking a corpse's part, stripping a trusted node, minting Goblin Blood, turning a blood into Goblin Blood, minting a high-quality real herb, starting healing over time, queuing a craft, and a corpse set up from a fake creature claiming goblin blood: all refused. A sandbox knife, station and herb are ignored.
- **Not closed, and not new:** sandbox code can call `Die` on a trusted creature it clones; the corpse is real, with real loot and real parts, exactly as before crafting existed. Wizards may build and test their own stations, nodes and recipes only in so far as a trusted station cloned into their room counts; `/wiz` recipes, materials, tools and stations do not.
- **Wizards make no history**: the event service ignores them and tells them so.

## 8. Tests

- `tests/craft_test.py` (port 4829, about 4.5 minutes, 254 checks): every command and refusal, growth and its limits, harvest quality (exact odds against the simulator, 4,000-roll samples within 4.5 standard deviations, deterministic rolls), the corpse rules, node yields, the matcher and the clash rule, inheritance and the owner's example, stations, discovery by teaching, book and experiment, the Chronicle first and none for a wizard, effects, batches, quitting mid-queue, a crash right after a craft, persistence across a killed driver and a clean restart, the sandbox, shops, and 1,000 generated recipes (20 files, 300 materials) loaded and matched with timings.
- `tests/craft_sim.py`: the economy and the odds, pure Python.
- Test-only content: `tests/fixtures/crafting/samples` (six recipes, materials, tools, stations, book, trainer, nodes, creatures under `/domains/crafttest`). It reuses the launch ids, so `craft_test.py` removes the real launch files from its copy of the mudlib and `craft_sim.py` leaves them out of its samples run.
- `tests/craft_content_test.py` (port 4830) plays the real content: the notice, the stations, every supplier and teacher against the NPC files, the ten recipes at their stations, the refusals, the two experiments and the Chronicle first, gathering for oneself, and no Goblin Blood in any shop.

## 9. Appendix: the launch recipes and materials as data

These are the game's real files now: `/crafting/launch_materials.cs`, `/recipes/launch.cs` and `/domains/hollow/obj/craft/` (classes prefixed `Cr`), with the stations, tools, a recipe book and the vendors and teachers on top. `craft_sim.py` checks them on every run; the closest recipe to a loop is still 0.14 gold short. The listing below is the data as first written; two discovery levels differ in the files (Sweet Berry Juice `.Discovery("discovered")`, Blood Ink `.Discovery("rare")`) and the Herbal Bandage restores 12.

**The sixteen materials.** Six come from `/crafting/beasts.cs` as shipped: Rabbit Skin (`rabbit_skin`, 3), Wolf Skin (`wolf_skin`, 6), Deer Skin (`deer_skin`, 6), Rabbit Blood (`rabbit_blood`, 2), Wolf Blood (`wolf_blood`, 4), Goblin Blood (`goblin_blood`, 8, scarce). The other ten, and three crafted intermediates, in `/crafting/launch_materials.cs`:

```csharp
// @inherit /std/craft_data
public class HcLaunchMaterials : CraftData
{
    public override void Create()
    {
        Material("blackberries", "/domains/hollow/obj/craft/blackberries", "berry, fruit");   // value 2
        Material("blueberries", "/domains/hollow/obj/craft/blueberries", "berry, fruit");     // 2
        Material("healing_herb", "/domains/hollow/obj/craft/healing_herb", "herb");           // 3
        Material("wildflower", "/domains/hollow/obj/craft/wildflower", "flower");             // 2
        Material("thistle", "/domains/hollow/obj/craft/thistle", "plant");                    // 1
        Material("honey", "/domains/hollow/obj/craft/honey", "sweetener");                    // 3
        Material("flour", "/domains/hollow/obj/craft/flour", "grain");                        // 1
        Material("clean_water", "/domains/hollow/obj/craft/clean_water", "liquid");           // 1
        Material("cloth_strip", "/domains/hollow/obj/craft/cloth_strip", "cloth");            // 2
        Material("empty_bottle", "/domains/hollow/obj/craft/empty_bottle", "container");      // 1
        Material("berry_juice", "/domains/hollow/obj/craft/berry_juice", "juice, drink");     // 1, crafted
        Material("treated_leather", "/domains/hollow/obj/craft/treated_leather", "leather");  // 4, crafted
        Material("blood_ink", "/domains/hollow/obj/craft/blood_ink", "ink");                  // 6, crafted

        Yield("berry bush", "blackberries", 1, 2);
        Yield("berry bush", "blueberries", 1, 2);
        Yield("herb patch", "healing_herb");
        Yield("wildflowers", "wildflower", 1, 2);
        Yield("thistle patch", "thistle");
    }
}
```

**The ten recipes,** `/recipes/launch.cs` (or split by station):

```csharp
// @inherit /std/recipe_set
public class HcLaunchRecipes : RecipeSet
{
    public override void Create()
    {
        Recipe("berry_juice", "Berry Juice").Needs("blackberries").Needs("clean_water")
            .At("cooking fire").Skill("cooking", "simple").Makes("berry_juice");
        Recipe("sweet_berry_juice", "Sweet Berry Juice").Needs("berry_juice").Needs("honey")
            .At("cooking fire").Skill("cooking", "novice").Makes("/domains/hollow/obj/craft/sweet_berry_juice");
        Recipe("simple_bandage", "Simple Bandage").Needs("cloth_strip")
            .Skill("herbalism", "simple").Makes("/domains/hollow/obj/craft/simple_bandage");
        Recipe("herbal_bandage", "Herbal Bandage").Needs("cloth_strip").Needs("healing_herb")
            .At("herbalist table").Skill("herbalism", "novice").Makes("/domains/hollow/obj/craft/herbal_bandage");
        Recipe("minor_healing_salve", "Minor Healing Salve").Needs("healing_herb").Needs("wildflower")
            .At("herbalist table").Skill("herbalism", "novice").Makes("/domains/hollow/obj/craft/minor_healing_salve");
        Recipe("minor_stamina_tonic", "Minor Endurance Tonic").Needs("blueberries").Needs("honey")
            .At("herbalist table").Skill("herbalism", "novice").Makes("/domains/hollow/obj/craft/minor_stamina_tonic");
        Recipe("treated_leather", "Treated Leather").Needs("rabbit_skin").Needs("wolf_blood")
            .At("tanning rack").Skill("tanning", "novice").Makes("treated_leather");
        Recipe("travelers_leather_wrap", "Traveler's Leather Wrap").Needs("treated_leather").Needs("cloth_strip")
            .At("workbench").Skill("tinkering", "novice").Makes("/domains/hollow/obj/craft/travelers_leather_wrap");
        Recipe("blood_ink", "Blood Ink").Needs("goblin_blood").Needs("wildflower")
            .At("workbench").Skill("tinkering", "novice").Makes("blood_ink");
        Recipe("hunters_poultice", "Hunter's Poultice").Needs("healing_herb").Needs("goblin_blood").Needs("cloth_strip")
            .At("herbalist table").Skill("herbalism", "novice").Makes("/domains/hollow/obj/craft/hunters_poultice");
    }
}
```

**Outputs** (all under `/domains/hollow/obj/craft/`; full files in the fixture):

| Output | Base | Value | Effect fields |
|---|---|---|---|
| berry_juice | `/std/crafted`, drink | 1 | `RestoreVitality = 8` (minor Vitality) |
| sweet_berry_juice | `/std/crafted`, drink | 2 | `RestoreVitality = 10; RestoreEndurance = 10` |
| simple_bandage | `/std/crafted`, apply | 1 | `RestoreVitality = 6` (minor healing) |
| herbal_bandage | `/std/crafted`, apply | 3 | `RestoreVitality = 12` (moderate healing) |
| minor_healing_salve | `/std/crafted`, apply | 3 | `HealPerTick = 3; Ticks = 6; TickSeconds = 5` (18 over 30 s) |
| minor_stamina_tonic | `/std/crafted`, drink | 3 | `RestoreEndurance = 25` |
| treated_leather | `/std/material` | 4 | a component; more than a rabbit skin (3) |
| travelers_leather_wrap | `/std/armor`, trinket | 3 | `ArmorValue = 0; CarryBonus = 1` (recorded, inert) |
| blood_ink | `/std/material` | 6 | a component for scribes, scholars and rune crafters |
| hunters_poultice | `/std/crafted`, apply | 8 | `HealPerTick = 5; Ticks = 8; TickSeconds = 5` (40 over 40 s: the best healing near the Hollow) |

## 10. What the engine cannot yet express, and later passes

- **Containers.** Nothing in the brief's recipes uses the Empty Bottle, Flour or Thistle; they are materials only. A recipe cannot return a container (an empty bottle after drinking) or keep an ingredient (a mortar); a later pass could add "returns" and "tools" to recipes.
- **Tool wear and tool crafting:** tools do not wear out; there is no recipe output that is a tool of chosen quality other than by inheritance.
- **Legendary** never comes from harvesting and crafting is capped at Masterwork (`craft.max_quality`); the `item.legendary` Chronicle event is not wired.
- **Rumors, guilds and achievements** as discovery sources, and Renown beyond the event service's own award: not built. Discovery achievements are the Chronicle's firsts.
- **Effects** are vitality now, endurance now and healing over time; buffs (timed `AddEffect` mods) from food are not yet data fields. The carry bonus is inert (no carry limit).
- **Recipes with the same ingredients and different stations** count as a clash (one set of materials makes one thing). Station-aware matching is a possible extension.
- **Batch crafting** at a station that is removed mid-batch stops at the next step; fine. Crafting at a station does not reserve it; any number may work at one.
- **The 1,000-recipe test** uses materials whose files do not exist (the daemon does not open files at load; the simulator checks files).
- The daemon does not save anything of its own; who discovered what is the Chronicle's record and each character's known recipes.
