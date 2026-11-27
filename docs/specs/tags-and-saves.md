# Tags, and saving throws for effects (proposed 27 November 2026)

Owner's words (27 November 2026). On tags: "can we make a distinction between melee and ranged weapons in the weapons themselves? Maybe add a 'tag' system that can go on weapons and armor that way if we decide to implement something like element affinity (for example) we could just add a tag, or a weapon type (blunt, slashing, etc) which could be used to fine tune the damage messages", and, of bosses, "[another tag maybe]". On saves: "Bosses [another tag maybe] and creatures higher level than the caster should get either a saving throw or bonus to the saving throw, for boss creatures it should be 50% effective or no effect based on save and higher level should be full effect or 50% effect [Above the law for bosses and 'Ignores your authority' for higher level]", and of spells that need no hit roll, "magic is immune already".

Every number is a starting value for `tests/balance_sim.py` and for play-testing; nothing here has been run in the game. "Today" means the files in the repository on 27 November 2026. Part A and Part B are one document because Part B reads the `boss` and `elite` tags that Part A creates; they can be built in two pieces (section 17). The first user of Part B is the Bureaucrat of [bureaucrat.md](bureaucrat.md).

## Decisions for the owner
Each has a recommendation and the alternative. The spec is written as if you accept every recommendation.

1. **One tag system for things and creatures.** An item, a creature and a creature's own action may each carry tags (lower-case words). Recommendation: yes, one mechanism in `/std/thing`. Alternative: separate fields per kind (`Reach` on weapons, `Rank` on creatures, ...). That is what exists now (`Category`, `Boss`, `Alert`, `DarkSight`) and it is why a rule such as "cannot make melee attacks" has nothing to ask.
2. **The vocabulary is closed and central.** A tag must be registered in one file, `/secure/tags.cs`, grouped in families; two tags of one exclusive family cannot be combined; `tests/lint_area.py` fails the build on a tag that is not registered, on a reserved tag used before its rules exist, and on a weapon without exactly one reach, one damage type and one kind. Recommendation: yes; a typo then cannot create a silent tag. Alternative: free words checked only when the game runs (a typo is then a log line, not a failure).
3. **The three creature flags stay the way to declare, and show up as tags.** `Boss = true`, `Alert = true` and `DarkSight = false` stay as they are (no file of the 44 creatures needs editing for them); the tags `boss`, `alert` and `darksight` are *derived* from them; a new `Elite = true` flag is added the same way. Every other tag is declared with `Tags.Add("...")`. Recommendation: this, because it changes nothing already written. Alternative: convert every file to `Tags.Add("boss")` and remove the flags (44 edits, no gain).
4. **Every weapon states reach, damage type and kind in its file.** If a file states none (a wizard's file, a test fixture, an old save of nothing) the runtime fills them in from the weapon's `Category` (sword: slashing, melee; bow: piercing, ranged; table in section 3). `Category` stays; it is the kind, and it stays the key the callings' weapon access uses. Recommendation: yes. Alternative: no defaults (then every untagged weapon is an error at run time).
5. **A thrown weapon counts as "at range" for the light penalty and for the ranged opening, but not for Aimed Shot or for any rule that asks for a bow.** An unarmed player or creature is `melee` and `blunt`, with no kind. No thrown weapon exists yet; the tag is registered so the first one is easy. Recommendation: yes.
6. **Damage type picks the hit wording** (section 6): three melee ladders (blunt, slashing, piercing), the existing archery ladder kept word for word, and one ladder for each natural attack (bite, claw, gore, kick, slam, lash, touch, spit, thorns). **Fists and any attack with no tags keep today's words** (scratch / hit / hit hard / smash brutally), so every test fixture and unarmed fight reads as before. Recommendation: yes. Alternative: give fists their own ladder (tap / punch / punch hard / pound); it costs about nine edits in six test files (listed in section 15).
7. **Nothing else in combat changes now.** No resistance, no element, no weakness, no damage change: those are reserved families with no rules (section 5). Recommendation: yes.
8. **Tags are not saved.** They come from the object's code, so retuning a file retunes every copy, as the callings do. A crafted or enchanted item that gains a tag later will keep it in one saved item property, `tags+`; this is designed in section 9 and not built. Recommendation: yes.
9. **Armour and weapons by calling stay as they are** (`WornBy`, `NotWornBy` lists of calling ids). They are about the character, not the item, and are not tags. Recommendation: yes.
10. **One rule for every control effect** (slow, bind, silence or interrupt, forced target, cannot-melee, stun): a **ladder of three strengths, Full, Half, None**. Every target rolls one save. A failed save leaves the effect at Full; a made save takes **one step off**. **A boss starts one step down** ("above the law"). **A target above the caster's level** gets a bonus to its save chance ("ignores your authority"). **A repeat on the same target within 60 seconds takes one more step off.** Recommendation: this, because it is the owner's two outcomes written once (ordinary or higher level: Full or Half; boss: Half or None) and it gives ordinary targets Half, not None, on a made save. Alternative: an ordinary target that makes its save takes no effect at all (the old behaviour of a slow). That is simpler to say, but then a target of equal level with 16% resistance sometimes wastes a whole spell, and the "half" outcome belongs to the above-level target alone.
11. **"Half" means half the time, everywhere.** A slow keeps its strength and lasts half as long; a bar lasts half as long; a ward lasts half as long. Recommendation: yes: one dial, and it works for every kind of effect. Alternative: half the amount for a slow (Reaction Time x1.25 instead of x1.5); it needs a second rule for every effect that has an amount and none for a duration.
12. **The caster's side is a new derived statistic, Authority**: the better of Insight and Resolve, one percentage point for each point above 5, plus 4 points for each rank of the skill above I. It lowers the target's save chance. Recommendation: yes (Insight and Resolve are the owner's words). Alternative: no caster side (only the target's resistance and level matter); then a level 10 caster with 18 points placed is no better at control than a level 5 caster with none.
13. **The level bonus is 10 points of save chance for each level the target is above the caster; elite adds 15 points; the chance is kept between 5% and 90%.** Alternative: 5 points a level (a level 8 creature against a level 5 caster would then save 24%, not 33%, in the worked table).
14. **Repeat and flood guards.** The same effect on the same target within 60 seconds of its last application is one step weaker each time (two at most), so a slow cannot be kept up for more than about 38% of the time and a restraining order for about 21%; and no target takes more than four control effects of any kind in 60 seconds. Recommendation: yes. Alternative: a rising save chance (+15 a repeat) instead of steps: it is smoother and it never reaches a fixed ceiling, so a lock stays possible with enough Focus.
15. **Nothing existing adopts the rule now.** The Warrior's Shield Bash and the creatures' slows and staggers on players keep their present Physical Resistance roll until `tests/balance_sim.py` has measured them (section 12); the Scout's notice rules never adopt it. The Bureaucrat is the first user. Recommendation: yes.
16. **The dice live in one new daemon, `/secure/control.cs`**, not in each spell. Recommendation: yes (the same shape as `/secure/sneak`).

## 1. What exists today
- **Items.** `Weapon` has a string `Category` ("dagger", "sword", "spear", "axe", "mace", "staff", "bow"), read by `Living.QueryWeaponCategory()`; `Armor` has `Category` ("light", "medium", "heavy") and a `Slot`, and `QueryCategory()` returns the slot for a shield or trinket. `Item.WornBy` and `Item.NotWornBy` are lists of calling ids. `Item` also carries saved instance state: quality, quantity and at most 16 named properties of up to 256 characters (`SetProperty`, `QueryState`, `RestoreState`).
- **Creatures.** `Monster` has the fields `Aggressive`, `Boss`, `Alert`, `Assist`, `DarkSight` (queried by `QueryAggressive`, `QueryBoss`, `QueryAlert`, `QueryDarkSight`), and the strings `ChronicleEvent` (a creature of rank reports "boss.dungeon" and the like), `ChronicleKind` and `ChronicleSubject`. There is no `Elite` flag in the game: "elite" exists as a word in the Goblin Elite Guard's name and ids and as an option of the Scout simulator (`SNEAK_ELITE`, docs/BALANCE_SCOUT.md option C).
- **Where "bow" is read** (the places that must change): `Living.BeginFight` (the opening shot: `QueryWeaponCategory() == "bow"`), `Living.Attack` twice (the light penalty argument `ranged`, and the verb ladder `shot`), `Skill.CanUse` through `WeaponCategories` (Aimed Shot adds "bow"), and `Adventurer`'s equipment access, which reads the weapon's `QueryCategory()` against the calling's `Weapons` list (that one stays; it is an access rule on the kind).
- **Where `Boss`, `Alert` and `DarkSight` are read** (the complete list): `Adventurer.SneakNotice` calls `QueryBoss` (a boss always notices a sneaking Scout), `Adventurer.NoticeRoll` calls `QueryAlert` (Alert adds 30 points to the notice chance), `/secure/light.NoticeAdjust` calls `QueryDarkSight` (the room's light term of the notice chance), and `Living.Attack` calls `QueryDarkSight` for the light penalty. **Aimed Shot's opener cap does not read `Boss` or `Alert`**: `AimedShotSkill.QueryOpenerCapPercent` takes only the level difference. `Monster.ChronicleEvent` is separate from `Boss` and stays so. `tests/balance_sim.py` keeps its own lists (`SNEAK_ALERT`, `SNEAK_BOSS`, `SNEAK_ELITE`) and `check` compares them with the mudlib.
- **Words for a blow.** `Living` has two ladders (`HitYou`/`HitThem`/`HitHow`, and `ShotYou`/`ShotThem`/`ShotHow`) chosen by whether the wielded weapon is a bow, and four tiers by the share of the target's **maximum** Vitality that the blow really took (`share = applied x 100 / max Vitality`; under 10%, under 25%, under 40%, 40% and over). Creatures' own attacks use the first ladder whatever their action is called.
- **Resistances.** `Living.QueryStat("mental_resistance")` and `("physical_resistance")` are 2% for each point above 5 of Spirit plus Resolve, and of Vigor plus Resolve, at most 75%. `RollPhysicalResistance()` is used by `AddSpeedEffect` (a made roll: "shakes off the slowing", no effect) and by `Stagger` when the stagger comes from another creature. `RollMentalResistance()` exists and **nothing calls it**. A creature's attributes default to 4 + level / 2 each, so a creature's resistance is 0% up to level 3, 4% at levels 4 and 5, 8% at 6 and 7, 12% at 8 and 9, 16% at 10 and 11, 20% at 12 and 13.
- **Who uses an effect on whom today.** Players: the Warrior's Shield Bash staggers (`Stagger`, which rolls the target's Physical Resistance). Creatures on players: Forest Spider and Woodland Spider ("Slowing Bite", `AddSpeedEffect`), Restless Spirit ("Chill of the Grave", slow), Root Creeper ("Clinging Grasp", slow), Bandit Leader ("Shield Smash", stagger). No player skill slows a creature; the wizard `speed` command uses `AddEffect` and is never resisted.
- **How spells resolve.** Arcane Bolt, Minor Heal, Blessing and Glow are skills of Kind "spell" with `IsAttack` false: the engine calls `Skill.Resolve` directly. They never enter `Living.Attack`, so they have no hit roll, no weapon, no light penalty by weapon (Arcane Bolt is only dodged: `target.RollDodge()`), and nothing that is written about a weapon's reach touches them. That is what "magic is immune already" is, in the code. It stays true under this design (section 7).

## 2. What a tag is
A **tag** is a lower-case word of 2 to 16 letters and hyphens (`[a-z][a-z-]{1,15}`) that is registered in `/secure/tags.cs`. It states a fact about a thing, never a number. It is not a style tag of the colour markup (`{cmd}`, `{red}`), which is a different thing that happens to share the word; in code the markup is "style tag" and this is "tag".

**Families.** Every registered tag belongs to one family. A family is either **exclusive** (an object may carry at most one of its tags; some families require exactly one) or **open**. A family is **active** (rules read it) or **reserved** (the words are held, the lint rejects their use, no rule reads them).

| Family | Exclusive? | Carried by | Tags | State |
|---|---|---|---|---|
| `reach` | yes; exactly one on every weapon, one on every creature | weapon, creature, creature action | `melee`, `ranged`, `thrown` | active |
| `damage` | yes; exactly one on every weapon, one on every creature | weapon, creature, creature action | `blunt`, `slashing`, `piercing` | active |
| `kind` | yes; exactly one on every weapon (equal to its `Category`), one on every creature | weapon, creature, creature action | weapons: `dagger`, `sword`, `spear`, `axe`, `mace`, `staff`, `bow`; creature-only: `pick`, `club`, `bite`, `claw`, `gore`, `kick`, `slam`, `lash`, `touch`, `spit`, `thorns` | active |
| `armour` | yes; exactly one on every armour piece (equal to `QueryCategory()`) | armour | `light`, `medium`, `heavy`, `shield`, `trinket` | active |
| `material` | yes; none or one | weapon, armour | `cloth`, `leather`, `wood`, `iron`, `steel`, `bone`, `stone` | active, informational (words only) |
| `rank` | yes; none or one | creature | `elite`, `boss` | active |
| `nature` | no | creature | `beast`, `goblin`, `human`, `undead`, `spirit`, `plant`, `construct` | active, informational (shown by `stat`, read by no rule yet) |
| `senses` | no | creature | `alert`, `darksight` | active |
| `element` | no | weapon, armour, creature, action | (held: `fire`, `frost`, `shock`, `holy`, `shadow`) | **reserved** |
| `affinity` | no | creature, armour | (held: any tag of the form `weak-ELEMENT`, `resists-ELEMENT`, `immune-ELEMENT`) | **reserved** |

Notes.
- A creature's `reach`, `damage` and `kind` describe its **basic attack**. An action may carry its own (a Warren Broodmother's Webbed Spit is `ranged`, `blunt`, `spit`); an action's tags default to the creature's.
- `thrown` is a reach for weapons made to be hurled (none exist). `ranged` and `thrown` together are "at range" (section 4).
- `kind` equals the old `Category` for every weapon. The creature-only kinds are the natural attacks and the crude arms of the goblins (`pick`, `club`).
- The pairs that have a hit-wording ladder are listed in the registry (section 6); a weapon whose (reach, damage) pair has none is a lint error until a ladder is written.
- Tags name what a thing **is**. Behaviour stays a field: `Aggressive`, `Assist`, `Peaceful`. A creature may stop being aggressive; it does not stop being a goblin.

**Implied tags.** If an object states no tag of an exclusive family that its kind requires, the runtime supplies it from this table (and the lint, which reads only files, still demands the explicit tag in every file under `/domains`):

| Kind | reach | damage | | Kind | reach | damage |
|---|---|---|---|---|---|---|
| dagger | melee | piercing | | bite | melee | piercing |
| sword | melee | slashing | | claw | melee | slashing |
| axe | melee | slashing | | gore | melee | piercing |
| spear | melee | piercing | | kick | melee | blunt |
| mace | melee | blunt | | slam | melee | blunt |
| staff | melee | blunt | | lash | melee | blunt |
| bow | ranged | piercing | | touch | melee | blunt |
| pick | melee | piercing | | spit | ranged | blunt |
| club | melee | blunt | | thorns | ranged | piercing |

## 3. The registry and how a builder adds a tag
**Where.** `/secure/tags.cs`, class `TagRegistry : MudObject`, a blueprint-only daemon holding frozen tables with copy-on-read (the shape of `RankDaemon.FrozenRankPrices`, so sandbox code cannot change them). It is a file of the core mudlib, so adding to it goes through the publishing review of docs/PUBLISHING.md like any change to `/secure`.

**The file's format** is one line per family and one per ladder pair, written so that `tests/lint_area.py` can read the vocabulary without running the game (the way `balance_sim.py check` reads numbers out of the mudlib):

```csharp
Family("reach",    Exclusive, Active,   "weapon creature action", "melee ranged thrown");
Family("damage",   Exclusive, Active,   "weapon creature action", "blunt slashing piercing");
Family("kind",     Exclusive, Active,   "weapon creature action", "dagger sword spear axe mace staff bow pick club bite claw gore kick slam lash touch spit thorns");
Family("armour",   Exclusive, Active,   "armour",                  "light medium heavy shield trinket");
Family("material", Exclusive, Active,   "weapon armour",           "cloth leather wood iron steel bone stone");
Family("rank",     Exclusive, Active,   "creature",                "elite boss");
Family("nature",   Open,      Active,   "creature",                "beast goblin human undead spirit plant construct");
Family("senses",  Open,      Active,   "creature",                "alert darksight");
Family("element",  Open,      Reserved, "weapon armour creature action", "fire frost shock holy shadow");
Family("affinity", Open,      Reserved, "creature armour",         "");   // weak-X, resists-X, immune-X for X in element
Implied("sword", "melee", "slashing");        // one line per row of the table in section 2
Ladder("melee", "blunt");  Ladder("melee", "slashing");  Ladder("melee", "piercing");  Ladder("ranged", "piercing");
```
**To add a tag:** one word on one `Family` line (and, for a new kind, one `Implied` line and a ladder, section 6), plus a sentence in this document's table. **To add a family:** one new `Family` line; if it is reserved nothing reads it. **To start using a reserved family:** change `Reserved` to `Active` in the same change that adds the rule that reads it. A rule that reads a family nobody has declared costs nothing and does nothing.

**API of the registry** (all names begin with `Query`, so they are read-only under docs/SANDBOX.md section 3 and sandbox code may call them): `QueryFamilies()`, `QueryFamilyOf(tag)` ("" if unregistered), `QueryIsRegistered(tag)`, `QueryIsActive(tag)`, `QueryIsExclusive(family)`, `QueryImplied(kind)` ("reach damage"), `QueryHasLadder(reach, damage)`, `QueryProblems(List<string> tags, string carrier)` (the list of faults in a tag set for a carrier of kind "weapon", "armour", "creature" or "action"; empty when it is valid). `/secure/master.cs` needs nothing new: `Query...` is already read-only, and the daemon changes no state.

**The lint** (`tests/lint_area.py`, a new section "tags", errors unless noted; it reads the `Family`, `Implied` and `Ladder` lines of `mudlib/secure/tags.cs`):
1. Every string in a `Tags.Add("x")`, `Tags.AddRange(...)` or a `HasTag("x")` / `QueryTagIn("f")` call (anywhere under `mudlib/std`, `secure`, `skills`, `classes`, `domains`) is a registered tag (or family).
2. A reserved tag is an error: "tag 'frost' is reserved: no rule reads it yet".
3. Two tags of one exclusive family in one file is an error.
4. Every file that inherits `/std/weapon` states exactly one `reach`, one `damage` and one `kind` tag, and the `kind` equals its `Category = "..."`; every `/std/armor` states exactly one `armour` tag equal to its `Category` (body, head) or its `Slot` (shield, trinket); every `/std/monster` states one `reach`, one `damage` and one `kind` for its basic attack (a WARN until Stage 3 is finished, then an error), and any `AddCombatAction(..., reach:, damage:, kind:)` literals are valid.
5. The (reach, damage) pair of every weapon and every creature action has a `Ladder` line, or its `kind` has its own ladder (section 6).
6. `Tags.Add("boss")`, `"alert"`, `"darksight"` and `"elite"` are errors: declare them with the flag (`Boss = true;`); the message says which.
7. A file under `/domains/town` is skipped like everything else there.

## 4. The API on Thing, Item, Living and Monster
Declared in `Create()`, like `Ids` and `WornBy`:

```csharp
protected readonly List<string> Tags = new();     // /std/thing
...
Tags.Add("sword"); Tags.Add("slashing"); Tags.Add("melee");
```
Read, all public and all beginning `Query` or `Has` (so sandbox code may read them and nothing else): in `/std/thing.cs`

| Function | Returns |
|---|---|
| `HasTag(string tag)` | true if the object carries the tag (declared, implied, derived from a flag, or, for an item, extra). One hash look-up. |
| `QueryTags()` | a sorted copy of every tag, for `stat` and for tests. |
| `QueryTagIn(string family)` | the object's one tag of an exclusive family, or "". `QueryTagIn("reach")`. |
| `QueryTagSummary()` | the short phrase for displays: "slashing sword", "piercing bow, at range", "light leather armour", "goblin, elite". "" for a thing with no tags. |

In `/std/item.cs`: `QueryTags()` adds the item's extra tags (section 9; none today). In `/std/monster.cs`: `QueryTags()` adds `boss`, `alert`, `darksight`, `elite` from the four flags, so `HasTag("boss")` is true exactly when `Boss = true`. `QueryBoss()` and `QueryAlert()` stay as thin readers (`=> HasTag("boss")`), so every caller above keeps working; new code calls `HasTag`. In `/std/living.cs`:

| Function | Returns |
|---|---|
| `QueryAttackReach()` | "melee", "ranged" or "thrown": the wielded weapon's `reach` tag; with no weapon, the creature's own; for a player with no weapon, "melee". Used where `QueryWeaponCategory() == "bow"` was. |
| `QueryAttackDamage()` | "blunt", "slashing" or "piercing": the same, from the weapon, else the creature, else "blunt". |
| `QueryAttackKind()` | the weapon's kind, else the creature's, else "" (an unarmed player). |
| `QueryAtRange()` | `QueryAttackReach()` is "ranged" or "thrown". |

An **action** (`CombatAction`) gains three optional strings, `Reach`, `Damage`, `Kind`, set by new optional parameters of `AddCombatAction(..., string reach = "", string damage = "", string kind = "")`; an empty one means "the creature's own". The engine reads `action.Reach` (else `QueryAttackReach()`) for the action being resolved. `Skill` gains `protected List<string> WeaponTags`: the wielded weapon must carry **every** one of them, with `protected string NeedsWeaponText` for the refusal (Aimed Shot keeps "Aimed Shot needs a bow in your hands."). `WeaponCategories` stays for the skills that name a kind.

**Read-only names that are genuinely read-only.** The sandbox rule (docs/SANDBOX.md section 3) is a naming rule: every public function beginning `Query`, `Is`, `Has` or `Can` is trusted not to change anything, and a restricted caller may call it. `HasTag`, `QueryTags`, `QueryTagIn`, `QueryTagSummary`, `QueryAttackReach`, `QueryAttackDamage`, `QueryAttackKind` and `QueryAtRange` change nothing: `QueryTags()` returns a copy and the set itself is built once, lazily, from the declared list and never handed out. There is **no public function that adds or removes a tag**. `Tags` is a `protected` field, so only the object's own code (its `Create()`) writes it, and a sandbox object can only write its own. A real object's tags cannot be changed by any call from outside, sandbox or not. (Anything beginning with another word, such as an `AddTag` or `SetTag`, would be refused to a restricted caller by `ValidCall`; none is written for that reason.)

**A tag on a sandbox object is untrusted data to real code.** A wizard's weapon may claim `boss`, `elite`, a ladder that does not exist or a family that is reserved. Rules, each cheap:
- `QueryTags()` always passes the set through `TagRegistry.QueryProblems` once, when it is first read; an unregistered or reserved tag, or a second tag of an exclusive family, is **dropped** (the first of the family in registry order stays) and one line is logged (`tags: <path> ...`), at most three lines per object.
- Real code never grants a benefit on a tag of an object that is not real: the control daemon, the sneak code and any later rule that reads `rank` call the reader as `RankOf(creature)`, which returns "" for an object whose `Domain` is not "". (In practice a sandbox creature can never be a real player's target, since nothing real is in sandbox space; this is a second wall, like the "trusted tree" tests that already require the object's domain to be "".)
- A real object never takes its tags from a call; only from its own program.

**Cost.** Each object holds a `List<string>` of at most about eight interned strings and a lazily built `HashSet<string>` (built on first read: most rooms' items are never asked). A weapon has three to four, armour two to three, a creature five to seven. For a whole area (44 creature files, 30 item files) that is a few kilobytes. `HasTag` is one hash probe; the engine calls `QueryAttackReach` once per blow, which reads two small sets. Nothing is saved, so nothing changes in any save file.

## 5. First uses, and what does not change
**Changes at once (small and safe):**
1. **Reach** replaces the string `"bow"` where the game means "from a distance" (section 8).
2. **Damage type** picks the hit wording (section 6), for the attacker, the target and onlookers, including creatures' natural attacks.
3. **Display** (section 10): reach and damage type in words in `look`, `appraise`, `stat` and the shop.

**Does not change, stated plainly:** no number in combat. Damage, to-hit, armour, dodge, block, critical hits, resistances, the light penalties, the opener cap and the sneak chances are exactly as today. **Resistance to a damage type, weakness, element, element affinity and any damage bonus for a type are reserved**: the `element` and `affinity` families exist so a later change is "make the family active and add the rule", and nothing in this document reads them. The `material`, `nature` and `armour` families add words and a list, not rules. A weapon's reach and damage type are not a stat and do not appear in the balance simulator's damage numbers.

## 6. The hit wording by damage type
**Rule.** `Living.Attack` picks the ladder once per blow from the action's reach, damage and kind (the action's tags if it has them, else the attacker's), by this order, and then the tier from the share of the target's maximum Vitality as now (**under 10%, under 25%, under 40%, 40% and over**; the tiers stay).
1. A creature-only natural kind has its own ladder: `bite`, `claw`, `gore`, `kick`, `slam`, `lash`, `touch`, `spit`, `thorns`.
2. Else `ranged` with `piercing`: the archery ladder, **unchanged word for word**.
3. Else `melee` or `thrown`: the ladder of the damage type (a thrown weapon reads as its damage type; no thrown ladder is written until one exists).
4. Else (no tags at all, or fists): the **plain** ladder, today's words.

**The ladders.** Each cell is the verb you read / the verb the others read (and you read when it hits you) / what follows the target. The line is built as it is today: `You {verb} {the target}{how}.`, `{Name} {verbs} you{how}.`, `{Name} {verbs} {the target}{how}.`

| Ladder | Tier 0 (under 10%) | Tier 1 (under 25%) | Tier 2 (under 40%) | Tier 3 (40% and over) |
|---|---|---|---|---|
| **plain** (fists, untagged) | scratch / scratches | hit / hits | hit / hits, " hard" | smash / smashes, " brutally" |
| **blunt** (mace, staff, club) | bruise / bruises | thump / thumps | crack / cracks, " hard" | smash / smashes, " brutally" |
| **slashing** (sword, axe) | nick / nicks | cut / cuts | slash / slashes, " deeply" | cleave / cleaves, " to the bone" |
| **piercing** (dagger, spear, pick) | prick / pricks | stab / stabs | pierce / pierces, " deeply" | run / runs, " through" |
| **archery** (ranged, piercing: kept) | graze / grazes | shoot / shoots | shoot / shoots, " squarely" | shoot / shoots, " clean through" |
| **bite** | nip / nips | bite / bites | bite / bites, " hard" | maul / mauls, " savagely" |
| **claw** | scratch / scratches | claw / claws | rake / rakes, " deeply" | rend / rends, " to ribbons" |
| **gore** | nudge / nudges | gore / gores | gore / gores, " deeply" | rip / rips, " open" |
| **kick** | scuff / scuffs | kick / kicks | kick / kicks, " hard" | stamp / stamps, " flat" |
| **slam** | bump / bumps | slam / slams | slam / slams, " hard" | crush / crushes, " brutally" |
| **lash** | flick / flicks | lash / lashes | lash / lashes, " hard" | thrash / thrashes, " brutally" |
| **touch** | brush / brushes | touch / touches | chill / chills, " deeply" | wither / withers, " utterly" |
| **spit** (ranged) | spatter / spatters | splash / splashes | splash / splashes, " thickly" | drench / drenches, " completely" |
| **thorns** (ranged) | prick / pricks | pepper / peppers | pepper / peppers, " thickly" | riddle / riddles, " with thorns" |

**Examples in the game's voice**, a Warrior with the Rusty Sword against a goblin scavenger (13 Vitality), each line the tier a blow of that share gives:
- tier 0 (1 damage, 8%): "You nick the goblin scavenger." The scavenger's own Quick Stab (`dagger`, `piercing`) at the same tier reads "The goblin scavenger pricks you."
- tier 1 (2 damage, 15%): "You cut the goblin scavenger." and, for onlookers, "Keith cuts the goblin scavenger."
- tier 2 (4 damage, 31%): "You slash the goblin scavenger deeply."
- tier 3 (9 damage, 69%): "You cleave the goblin scavenger to the bone." / "Keith cleaves the goblin scavenger to the bone."
- A mace on the Overseer (114 Vitality, 12 damage, 10%): "You thump the Goblin Overseer."; a Hunting Bow on a rat: "You shoot a large rat clean through."; a Giant Rat on the player at 45% of the player's maximum: "The giant rat mauls you savagely."; the Forest Panther: "The forest panther rends you to ribbons."; the Boar Brute at tier 3: "The boar brute rips you open."; the Warren Broodmother's Webbed Spit at tier 1: "The warren broodmother splashes you."
- In the dark the actor is "Something" as today: "Something cuts you." and "You miss something."
- A critical hit, a block and the damage note are appended exactly as now ("A critical hit!", "The goblin blocks part of the blow.", the verbose "(+9 damage)").

**Where the ladders live.** A new small daemon `/secure/blows.cs`, class `BlowWords`, with the table above as frozen data and one read-only function, `QueryWords(string ladder, int tier)` returning "you|them|how", and `QueryLadder(string kind, string reach, string damage)` returning the ladder's name by the order above. `Living.Attack` replaces its two static arrays (`HitYou`, `HitThem`, `HitHow`, `ShotYou`, `ShotThem`, `ShotHow`) with one call. Tuning a word is one line in one file. The shop and appraisal sentences of section 10 use the same daemon.

**Lint check 5 and the pair list.** `Ladder("melee", "blunt")` and the others are what the lint reads to say "no ladder for (ranged, blunt)": the first sling needs a ladder line before its file is accepted.

## 7. Melee and ranged, and "magic is immune"
- **A weapon is melee or ranged in its own file** (`reach`), no longer by being a bow. The table in section 11 gives all 21.
- **A thrown weapon** (none exists) is at range for the ranged opening and for the light penalty (it needs sight and it is not used up close) but it is not a bow: Aimed Shot, which asks for the tag `ranged`, refuses it ("Aimed Shot needs a bow in your hands.").
- **An unarmed creature** (a player with empty hands, or a creature with no weapon) uses its own tags: a player is `melee`, `blunt`, no kind (plain words); a creature uses what its file says (a rat: `melee`, `piercing`, `bite`).
- **What a "cannot make melee attacks" effect bars** (section 8 of [bureaucrat.md](bureaucrat.md) builds it): an **action of kind "attack"** whose reach is `melee`, made against the one the effect names. It does not touch actions of another kind (a battle cry, a summons, a ward: the engine's `Special` and `Resolve` paths), nor an attack at range (a bow, a Webbed Spit).
- **"Magic is immune already", confirmed from the code.** Arcane Bolt is `Kind = "spell"`, `IsAttack` false: `Living.Resolve` takes the `Kind == "skill"` branch and calls `ArcaneBoltSpell.Resolve`, which rolls only `RollDodge()` and then `ApplyDamage`. It never calls `Living.Attack`, so there is no hit roll and no reach. A rule that bars melee attacks is a filter on `Kind == "attack"` actions with reach `melee`; Arcane Bolt, Minor Heal, Blessing and Glow are not such actions and are untouched without any exception being written. The same is true of the creatures' non-attack actions today (the Overseer's Battle Cry and Summon Goblin Minion, the Bandit Leader's Call the Pack): kind `cry` and `summon` go through `Special`.
- **Skills that make a weapon attack** (Precision Strike, Double Strike, Shield Bash, Aimed Shot, the basic attack) are attacks, and their reach is the wielded weapon's. A Scout who strikes with a bow in hand is making a ranged attack; with a dagger, a melee one.

## 8. The migration: every reader, what it will call
No behaviour changes. Each row is one edit; each is checked by the tests of section 15.

| Reader (file, function) | Today | Becomes |
|---|---|---|
| `Living.BeginFight` (opening shot) | `QueryWeaponCategory() == "bow"` | `QueryAtRange()` |
| `Living.Attack`, light penalty | `ToHitPenalty(lightLevel, QueryWeaponCategory() == "bow", QueryDarkSight())` | `ToHitPenalty(lightLevel, ActionAtRange(action), QueryDarkSight())`, where `ActionAtRange` is the action's reach (else `QueryAtRange()`) |
| `Living.Attack`, wording | `shot = QueryWeaponCategory() == "bow"`, two static ladders | `BlowWords` (section 6) |
| `Skill.CanUse` and `AimedShotSkill` | `WeaponCategories.Add("bow")` | `WeaponTags.Add("ranged")` with `NeedsWeaponText = "a bow"`; refusal text unchanged |
| `Adventurer` equipment access (about line 861) | `item.QueryCategory()` against `Weapons` / `ArmorTypes` | unchanged: the kind and the armour tag equal `Category`; it is an access rule |
| `Item.WornBy`, `NotWornBy`, `QueryAllowsCalling`, `QueryCallingNote`, `QueryCallingTag`, `Shopkeeper` | lists of calling ids | **unchanged**; not tags (decision 9) |
| `Adventurer.SneakNotice` | `creature.QueryBoss()` | `creature.HasTag("boss")` (`QueryBoss()` stays as a thin reader) |
| `Adventurer.NoticeRoll` | `creature.QueryAlert()` | `creature.HasTag("alert")` |
| `/secure/light.NoticeAdjust` and `Living.Attack` | `QueryDarkSight()` | `QueryDarkSight()` stays (`Monster` returns `HasTag("darksight")`; `Living` returns false for a player) |
| `Monster.ChronicleEvent`, `ChronicleKind`, `QueryCreatureKind` (kill counters, Chronicle) | strings | unchanged; they are about history, not nature |
| `tests/balance_sim.py` `SNEAK_ALERT`, `SNEAK_BOSS`, `SNEAK_ELITE` | hand lists | `check_mudlib` reads the tags from the files (`Boss = true`, `Alert = true`, `Elite = true`) and compares |
| New | none | `Elite = true` on the Goblin Elite Guard (section 11); first reader: `/secure/control` (Part B) |

`Aggressive` and `Assist` stay behaviour fields. `QueryDarkSight()` on `Living` stays virtual.

## 9. Saving
- **Declared tags are not saved.** A weapon, armour piece or creature takes them from its program every time it is made. A saved item is a program path plus its state (`QueryState`: quality, quantity, up to 16 properties), so an item in a character's pack, in lost property or on a corpse has its tags the moment it is made again, and retuning a file retunes every copy, as with the callings. A character's save file holds no tag, and nothing in it changes.
- **Reserved design, not built: a tag gained later** (a crafted weapon with a quality tag, an enchantment that adds `frost` once the element family is active). It is **one item property, `tags+`**, a space-separated list of at most four registered tags, at most 64 characters. Rules: written only by trusted code through `SetProperty` (it already refuses anyone else: `MayChange`); `Item.PropertyAllowed` validates it against `TagRegistry.QueryProblems` (an unregistered, reserved or inactive tag is refused, and a tag of an **exclusive** family that the file already states is refused: an enchantment cannot make a blunt mace slashing; it can add to an open family, or to an exclusive one the file leaves empty, such as `material`); `RestoreState` validates again at load, so an edited save cannot smuggle a tag, and drops what is invalid. `QueryTags()` returns the declared tags plus `tags+`. Two items with different `tags+` do not merge in a stack (`CanStackWith` already compares properties). Nothing reads `tags+` in this build.
- **Creatures** are never saved. A player character carries no tags; `QueryTags()` on a player returns an empty list.

## 10. Display
- **`look at` a weapon**: one sentence after the description and quality line: "It is a sword: it cuts, and it is used up close." The words: damage `blunt` "crushes", `slashing` "cuts", `piercing` "pierces"; reach `melee` "used up close", `ranged` "used from a distance", `thrown` "made to be thrown". Armour: "It is light armour, made of leather." (weight, then the material if there is one; a shield: "It is a shield, made of wood."). A thing with no tags says nothing extra. (`Item.Long` gains the sentence; nothing is added to a stackable material.)
- **`appraise` and Magical Lore** (`Weapon.Appraisal`, `Armor.Appraisal`): "Rusty Sword: a sword (slashing, melee). Damage 9-14, to hit +0, reaction modifier 0.5, recovery 1.0. ..." where today it says "a sword"; the bow "a bow (piercing, ranged)"; armour "light armor, slot body" gains " (leather)" after the weight when there is a material. `tests/balance_sim.py` and the existing tests do not parse these lines except as listed in section 15.
- **The shop** (`/std/shopkeeper` `CmdBrowse`): the line gains the short phrase beside the calling note: `A rusty sword        10 gold  (slashing sword)`, `A hunting bow  ... (piercing bow, at range)`, `A scholar's robe  ... (light cloth armour; Mages and Priests only)`. `QueryTagSummary()` makes it, so the shop and `look` agree.
- **`stat`** (wizards): a line `  Tags: bow piercing ranged leather` on items and `Tags: beast bite piercing melee darksight` on creatures, after the existing lines (`Thing.Stat`).
- **`consider`**: unchanged. A creature's nature is not told to the player yet (a future `lore` or Appraise of a creature may).
- **GMCP is unaffected**: nothing under `mudlib/secure/gmcp.cs` reads a category or a tag. **Website**: there are no item pages (docs/WEBSITE.md lists the pages); nothing to change. If an item page is added, it reads `QueryTagSummary()` for the same words.
- **Help**: `help weapons` (or the nearest topic; check `docs/BUILDING.md` "Items") gains a paragraph; `docs/BUILDING.md` "A weapon" and "Armor" gain the tag lines; `docs/CLASS_SYSTEM.md` section 5's category table gets a pointer to this document.

## 11. The complete table of what exists, with its tags
Read from the files on 27 November 2026. "Explicit" is what the file must state after Stage 1 (section 17); every row's `kind` equals today's `Category`.

### The 21 weapons (`/domains/hollow/obj`)
| File | Category | reach | damage | kind | material (optional) |
|---|---|---|---|---|---|
| rusty_sword | sword | melee | slashing | sword | iron |
| fine_iron_longsword | sword | melee | slashing | sword | iron |
| captains_sabre | sword | melee | slashing | sword | steel |
| iron_dagger | dagger | melee | piercing | dagger | iron |
| enchanted_dagger | dagger | melee | piercing | dagger | steel |
| steel_dirk | dagger | melee | piercing | dagger | steel |
| training_spear | spear | melee | piercing | spear | wood |
| boar_spear | spear | melee | piercing | spear | wood |
| goblin_war_spear | spear | melee | piercing | spear | wood |
| woodcutter_axe | axe | melee | slashing | axe | iron |
| felling_axe | axe | melee | slashing | axe | iron |
| overseers_axe | axe | melee | slashing | axe | iron |
| novice_mace | mace | melee | blunt | mace | iron |
| flanged_mace | mace | melee | blunt | mace | iron |
| shrine_blessed_mace | mace | melee | blunt | mace | iron |
| apprentice_staff | staff | melee | blunt | staff | wood |
| hollow_oak_staff | staff | melee | blunt | staff | wood |
| ironshod_staff | staff | melee | blunt | staff | wood |
| hunting_bow | bow | ranged | piercing | bow | wood |
| yew_longbow | bow | ranged | piercing | bow | wood |
| wardens_recurve | bow | ranged | piercing | bow | wood |

(Material is written only where the description names it: the Enchanted Dagger's "dark steel", the Captain's brass-guarded sabre and the dirk's "good steel" are steel; the rest "iron" or "wood" by their descriptions. It is words only. No weapon is crafted yet, so there is no crafted weapon to tag; the crafting recipes under `mudlib/recipes` make consumables, materials and tools, and `docs/CRAFTING.md` lists no weapon recipe.)

### The 9 armour pieces
| File | Slot | armour tag (= Category) | material |
|---|---|---|---|
| scholars_robe | body | light | cloth |
| padded_jerkin | body | light | cloth |
| leather_jerkin | body | light | leather |
| commanders_vest | body | light | leather |
| guild_helm | head | light | iron |
| leather_cap | head | light | leather |
| small_shield | shield | shield | wood |
| magical_trinket | trinket | trinket | (none) |
| craft/travelers_leather_wrap | trinket | trinket | leather |

### The creatures: 44 files
Rank: `boss` and `elite` from the flags; `alert` and `darksight` from `Alert` and `DarkSight`. **Every creature has `darksight` except the Giant Cave Toad.** Basic attack = the creature's first listed action. "Other actions" lists the actions whose tags differ from the basic attack; an action not listed has the basic attack's.

Hollow (`/domains/hollow/mob`):
| File | L | rank | senses | nature | basic attack (reach, damage, kind) | other actions |
|---|---|---|---|---|---|---|
| giant_rat | 1 | | darksight | beast | melee, piercing, bite | |
| large_rat | 1 | | darksight | beast | melee, piercing, bite | |
| practice_dummy | 1 | | darksight | construct | melee, blunt, slam | |
| goblin_scavenger | 2 | | darksight | goblin | melee, piercing, dagger | |
| goblin_minion | 2 | | darksight | goblin | melee, piercing, dagger | |
| goblin_guard | 2 | | alert, darksight | goblin | melee, piercing, spear | (see conflict 1 below) |
| goblin_guard_keyholder | 2 | | alert, darksight | goblin | melee, piercing, spear | (conflict 1) |
| spore_bat | 2 | | darksight | beast | melee, piercing, bite | |
| wandering_wolf | 2 | | darksight | beast | melee, piercing, bite | |
| goblin_warrior | 3 | | darksight | goblin | melee, slashing, sword | (conflict 2) |
| giant_cave_toad | 3 | | (none: `DarkSight = false`) | beast | melee, blunt, lash | Crushing Leap: slam |
| goblin_elite_guard | 4 | **elite** (new `Elite = true`) | alert, darksight | goblin | melee, slashing, axe | Shield Bash: melee, blunt, slam |
| goblin_overseer | 5 | **boss** | darksight | goblin | melee, slashing, axe | Power Strike: the same; Battle Cry, Summon Goblin Minion: not attacks |

Corridor (`/domains/greenvale/corridor/mob`):
| File | L | rank | senses | nature | basic attack | other actions |
|---|---|---|---|---|---|---|
| field_rabbit | 1 | | darksight | beast | melee, piercing, bite | |
| roe_deer | 3 | | darksight | beast | melee, blunt, kick | |
| grey_fox | 4 | | darksight | beast | melee, piercing, bite | |
| forest_spider | 5 | | darksight | beast | melee, piercing, bite | Slowing Bite: the same |
| stray_wolf | 5 | | darksight | beast | melee, piercing, bite | |
| forest_wolf | 6 | | darksight | beast | melee, piercing, bite | Savage Bite: the same |
| bandit_lookout | 6 | | alert, darksight | human | melee, slashing, sword | |
| boar_brute | 7 | | darksight | beast | melee, piercing, gore | Trample: slam |
| goblin_raider | 7 | | darksight | goblin | melee, slashing, axe (a cleaver) | War Whoop: not an attack |
| goblin_scout | 7 | | darksight | goblin | melee, piercing, dagger | |
| tunnel_rat | 7 | | darksight | beast | melee, piercing, bite | |
| wild_boar | 6 | | darksight | beast | melee, piercing, gore | Charge: slam |
| black_bear | 8 | | darksight | beast | melee, slashing, claw (Swipe) | Maul: claw; Bellowing Roar: not an attack |
| young_black_bear | 8 | | darksight | beast | melee, slashing, claw | |
| goblin_tunneler | 8 | | darksight | goblin | melee, piercing, pick | Overhead Swing: club |
| heartwood_goblin | 8 | | alert, darksight | goblin | melee, blunt, club | (conflict 3) |
| highway_bandit | 8 | | darksight | human | melee, slashing, sword | Pommel Strike: club |
| rogue_bandit | 8 | | darksight | human | melee, slashing, sword | Pommel Strike: club |
| root_creeper | 8 | | darksight | plant | melee, blunt, lash | Clinging Grasp: lash |
| woodland_spider | 8 | | darksight | beast | melee, piercing, bite | Slowing Bite: the same |
| ancient_skeleton | 9 | | darksight | undead | melee, slashing, sword | Bone Crusher: club |
| ancient_wolf | 9 | | darksight | beast | melee, piercing, bite | Howl: not an attack |
| forest_stalker | 9 | | darksight | beast | melee, slashing, claw | Ambush Strike: claw |
| restless_spirit | 9 | | darksight | spirit | melee, blunt, touch | Chill of the Grave: touch |
| rogue_goblin_veteran | 9 | | darksight | goblin | melee, blunt, club (a flanged mace: `mace`) | Dazing Blow: mace |
| thorn_woodling | 9 | | darksight | plant | melee, blunt, lash | Thorn Barrage: **ranged**, piercing, thorns |
| bandit_leader | 10 | **boss** | darksight | human | melee, slashing, sword | Shield Smash: slam; Call the Pack: not an attack |
| barrow_guardian | 10 | **boss** | darksight | construct | melee, blunt, slam (Stone Fist) | Earthshaker: slam; Stone Skin: not an attack |
| fallen_knight | 10 | **boss** | darksight | undead | melee, slashing, sword | Crushing Blow: club; Shield of the Fallen: not an attack |
| forest_panther | 10 | **boss** | darksight | beast | melee, slashing, claw | Pouncing Leap, Raking Frenzy: claw |
| warren_broodmother | 10 | **boss** | darksight | beast | melee, piercing, bite (Venom Fang) | Webbed Spit: **ranged**, blunt, spit; Call the Brood: not an attack |

(Where a creature has the Rogue Goblin Veteran's "flanged mace" the kind is `mace`, a weapon kind a creature may carry; where a file says "club" it means the crude arms of a goblin.) **The only creatures with a ranged action today are the Warren Broodmother (Webbed Spit) and the Thorn Woodling (Thorn Barrage).** The Highway Bandit "has a crossbow slung" but has no ranged action. Only the Goblin Elite Guard becomes `elite`; the other "mini-boss" candidates are left to the owner.

**Conflicts found between files and descriptions, for the owner** (the tags follow the **action name**, which is what the code and tests know; the description is then wrong, or the action needs a rename in Stage 3):
1. The Goblin Guard and its keyholder are described with "a short-handled hammer", but their action is "Spear Jab". Tagged `spear`/`piercing` (the action's word). Alternative: rename the action "Hammer Blow" and tag `mace`/`blunt`.
2. The Goblin Warrior is described with "a chipped spear" but its action is "Slash". Tagged `sword`/`slashing`. Alternative: rename "Spear Thrust", `spear`/`piercing`.
3. The Heartwood Goblin has "a spiked club", with actions "Slash" and "Heavy Swing". Tagged `club`/`blunt` (the description); the action "Slash" reads wrong under it: rename "Club Swing".

## 12. Part B: saving throws for effects
Everything in this section is **for effects, not damage**. A blow's damage, to-hit, dodge and block are untouched.

### 12.1 What an effect is, and which resistance it uses
A **control effect** is a hostile effect that one creature puts on another and that takes away what the target may do or how fast it may do it. One rule covers all of them. The kinds, and the resistance the target rolls:

| Kind of effect | Takes away | Target's resistance | Today |
|---|---|---|---|
| **slow** | speed (Reaction Time x factor) | Physical (Vigor + Resolve) | `AddSpeedEffect` |
| **stun** | its next actions (an interrupt, or a hold) | Physical | `Stagger` |
| **bind** | moving, fleeing | Physical | none |
| **silence / interrupt** | a skill being prepared, a class of action | Mental (Spirit + Resolve) | none |
| **forced target** | whom it may attack, or that it must attack someone | Mental | none |
| **cannot-melee** (a restraining order) | melee attacks against one named creature | Mental | none |

Mental effects (will: orders, summonses, bars on conduct) use `mental_resistance`, which today nothing rolls; its first use is therefore these. Physical effects (body: pace, holding, force) use `physical_resistance`. The kind is named once in the effect's data (`"physical"` or `"mental"`), not decided by each spell.

### 12.2 The ladder
The strength of an effect is one of three: **Full** (2), **Half** (1), **None** (0). The strength is worked out as

> strength = 2 − (1 if the target is a **boss**) − (the **repeat steps**, 0 to 2) − (1 if the target **makes its save**), and not below 0.

- a failed save leaves Full, a made save takes one step: **ordinary and above-level targets: Full or Half**;
- a **boss** starts one step down: **failed save Half, made save None** ("above the law");
- a repeat takes further steps (section 12.5).

**Half means half the time**, rounded up to a whole second, minimum 1 second (decision 11). A slow keeps its factor; a bar keeps its scope; a ward keeps its strength; each lasts half as long. For an effect measured in events not time (a ward that refuses the next attack; a hold that lasts until struck three times), half is stated in the effect's own row in [bureaucrat.md](bureaucrat.md); the default is that the count stays and the time limit halves.

### 12.3 The save chance
The target rolls once, `RollPercent(S)` (a tenth of a percent), where

> **S = clamp( R + 10 × max(0, target level − caster level) + 15 if elite − Authority , 5, 90 )**, in percent.

- **R** is the target's `QueryStat("mental_resistance")` or `("physical_resistance")` by the effect's kind (0 to 75).
- **10 points for each level the target is above the caster** ("ignores your authority"). Nothing is added or subtracted for a target below the caster's level.
- **15 points if the target carries the `elite` tag** (`RankOf(target) == "elite"`).
- **Authority** is the caster's `QueryStat("authority")` plus 4 for each rank of the skill above I. `authority` is a new derived statistic in `Living.StatRaw`: `max(Above("insight"), Above("resolve"))`, one percentage point per point above 5, cut off by no cap (it only lowers S and S has its own floor); equipment may add (`StatBonus["authority"]`), and a class modifier may scale it. A creature caster has it from its attributes (4 + level / 2 each, so 0 below level 4 and 4 at level 10). `docs/ATTRIBUTES.md` section 3's table gains one row.
- The **floor of 5%** means no target is certain to be held and no caster is certain to be resisted; the **ceiling of 90%** leaves a sliver of effect on the most superior target.
- A **boss's** save chance is computed the same way; being a boss shifts the outcome, not the chance.

### 12.4 Worked table: first use, no repeat
Targets' resistances are the creature template: attribute 4 + level / 2 (whole levels), so R = 4 × max(0, level / 2 − 1)%: 0 at level 3, 4 at levels 4 and 5, 8 at 6 and 7, 12 at 8 and 9, 16 at 10 and 11, 20 at 12 and 13. The two casters are the Bureaucrat of bureaucrat.md: **level 5, Insight 14, rank I: Authority 9**; and **level 10, Insight 20, rank III: Authority 15 + 8 = 23**. The two columns "Full" and "Half" are percentages of first applications; "None" (boss only) is the rest. "Expected" is the average strength (Full = 1, Half = 0.5, None = 0).

**Caster level 5, Authority 9**

| Target level | Rel. | R | S ordinary | Ordinary: Full / Half | S elite | Elite: Full / Half | Boss: Half / None | Expected: ord. / elite / boss |
|---|---|---|---|---|---|---|---|---|
| 3 | −2 | 0 | 5 | 95 / 5 | 6 | 94 / 6 | 95 / 5 | 0.98 / 0.97 / 0.48 |
| 4 | −1 | 4 | 5 | 95 / 5 | 10 | 90 / 10 | 95 / 5 | 0.98 / 0.95 / 0.48 |
| 5 | 0 | 4 | 5 | 95 / 5 | 10 | 90 / 10 | 95 / 5 | 0.98 / 0.95 / 0.48 |
| 6 | +1 | 8 | 9 | 91 / 9 | 24 | 76 / 24 | 91 / 9 | 0.96 / 0.88 / 0.46 |
| 7 | +2 | 8 | 19 | 81 / 19 | 34 | 66 / 34 | 81 / 19 | 0.91 / 0.83 / 0.41 |
| 8 | +3 | 12 | 33 | 67 / 33 | 48 | 52 / 48 | 67 / 33 | 0.84 / 0.76 / 0.34 |

(S for level 3 is 0 + 0 − 9, below the floor, so 5. Elite adds 15: 0 + 15 − 9 = 6. Level 8: 12 + 30 − 9 = 33; elite 48.) **The Overseer** (level 5, boss) against a level 5 Bureaucrat: Half 95% of the time, None 5%: he is held for half the term and almost never for none.

**Caster level 10, Authority 23**

| Target level | Rel. | R | S ordinary | Ordinary: Full / Half | S elite | Elite: Full / Half | Boss: Half / None | Expected: ord. / elite / boss |
|---|---|---|---|---|---|---|---|---|
| 8 | −2 | 12 | 5 | 95 / 5 | 5 | 95 / 5 | 95 / 5 | 0.98 / 0.98 / 0.48 |
| 9 | −1 | 12 | 5 | 95 / 5 | 5 | 95 / 5 | 95 / 5 | 0.98 / 0.98 / 0.48 |
| 10 | 0 | 16 | 5 | 95 / 5 | 8 | 92 / 8 | 95 / 5 | 0.98 / 0.96 / 0.48 |
| 11 | +1 | 16 | 5 | 95 / 5 | 18 | 82 / 18 | 95 / 5 | 0.98 / 0.91 / 0.48 |
| 12 | +2 | 20 | 17 | 83 / 17 | 32 | 68 / 32 | 83 / 17 | 0.92 / 0.84 / 0.42 |
| 13 | +3 | 20 | 27 | 73 / 27 | 42 | 58 / 42 | 73 / 27 | 0.87 / 0.79 / 0.37 |

(Level 11: 16 + 10 − 23 = 3, floor 5.) The five level 10 bosses of the corridor are Half 95%, None 5%.

**A caster with no Authority** (a level 3 creature, or a character whose Insight and Resolve are both 5): S = R + 10 per level above, floor 5: level 3 to 5: 5 / 5 / 5; level 6: 18; level 7: 28; level 8: 42. The caster's side matters from level +1 upward: with Authority 9 a level 6 target saves 9% and with none 18%, a level 8 target 33% against 42%.

**Reading the table.** Against creatures at or near the caster's level the effect lands in full about nineteen times in twenty; the caster's investment buys the level difference (a creature three levels above is held in full two times in three instead of one in two). A boss at the caster's level is held for half the term almost always and for nothing one time in twenty: that is what the owner asked for ("50% effective or no effect"). A boss three levels above is held for half the term two times in three.

### 12.5 Repeats, and the flood guard (decision 14)
- **Repeat steps.** The control daemon keeps, per target and per effect id, the time of the last application that took hold (strength above None) and a count `n` of applications in the current window. The window is **60 seconds from the last application**. A new application with `n` = 0, 1, 2 or more takes **0, 1, 2** steps off (never more than 2): so the second use within 60 seconds is one step weaker (a failed save gives Half, a made save None; for a boss the second is None), and the third is None. An application that comes out None starts no window and counts for nothing. After 60 seconds from the last application that took hold, the count is zero again.
- **Flood guard.** A target that has had **four** control effects of any kind take hold in the last 60 seconds ignores the fifth and every later one until the oldest of the four is 60 seconds old. The record is a short list of times per target (at most 8).
- **What it gives, in numbers.** A slow of 20 seconds (Full) on an ordinary same-level target, recast the moment it ends: Full 20 s, then Half 10 s (the second within the window), then None until 60 s after the second: **30 seconds of slow in 80, 37.5%**. A 10-second restraining order: 10 + 5 s in 70, **21%**. On a boss: one Half application per 60 seconds: the slow 10 s in 60 (**17%**), the restraining order 5 s in 60 (**8%**). Nothing can be kept up.
- **What the caster sees**, one extra line after the spell's own, chosen by the cause of the lowest step (the order of precedence: boss, repeat, level, ordinary):

| Outcome | Cause | The caster reads |
|---|---|---|
| Full | none | (nothing extra: the spell's own line) |
| Half | made save, ordinary | "{X} resists. The {effect} holds for half its term." |
| Half | made save, target above your level | "{X} ignores your authority. The {effect} holds for half its term." |
| Half | boss, failed save | "{X} is above the law. The {effect} half holds." |
| None | boss, made save | "{X} is above the law, and knows it. Nothing holds." |
| Half | repeat | "{X} has been served with that already. It holds for half its term." |
| None | repeat | "{X} has had notice of that already. It will not be served again for a while." |
| None | flood guard | "{X} has had notice enough for one minute." |

  For a target that is **both** a boss and above your level the boss line is used (it is the stronger fact); the level line is kept for the made-save case. {effect} is the spell's own noun ("order", "form", "notice"). The target (if a player, section 12.7) and onlookers read nothing extra; the spell's own lines say what happened. Verbose mode adds the numbers: "(save 9%, rolled 41: failed; Full)".

### 12.6 Which things adopt it, now and later
- **Now:** nothing that exists. **The Scout's notice rules do not adopt it** (they are a notice roll, not an effect, and `/secure/sneak` keeps its own formula). No existing skill has an effect that must adopt it before something new does.
- **First user:** the Bureaucrat's seven control spells (Forms in Triplicate, Restraining Order, Cease and Desist, Red Tape, Take a Number, Summons, Due Process), each declaring its kind and effect id.
- **Later, after the simulator has measured them** (Stage 4): (a) the Warrior's **Shield Bash**, whose stagger today rolls Physical Resistance and so is nearly always applied to the Overseer; under the rule it would be Half on the Overseer and the Warrior's 81% in the standard test would fall. It adopts only if the owner takes the Warrior's loss or raises Shield Bash. (b) The **creatures' slows and staggers on players** (the two spiders, the Restless Spirit, the Root Creeper, the Bandit Leader): the caster is the creature, the target a player; the made-save step for these stays "no effect" (as today) until the corridor simulation has been rerun, which is why the daemon takes a `madeSaveSteps` argument (default 1; the legacy callers pass 2). The Restless Spirit's chill is a mind effect in spirit but a Physical one in the code; it can move to Mental when it adopts. (c) the wizard `speed` command never adopts (it uses `AddEffect`, by design).
- **A rule to keep:** a hostile effect that takes away speed, actions or choices on a creature **goes through `/secure/control`**. The lint cannot see this; the spec for any new such effect must name its kind and effect id.

### 12.7 Players as targets, wizards, PvP
- **Players are targets of creatures**, today of slows and a stagger (above). A player is never `boss` or `elite`. A player above a creature's level ("ignores your authority") gets the level bonus; a player's own `physical_resistance` and `mental_resistance` are the stats `score` shows (a level 10 Warrior who has put his 18 points into Vigor and Resolve: Vigor 7+9, Resolve 6+9: physical resistance 2 × (11 + 10) = 42%). The corridor's creatures at levels 5 to 9 are mostly **at or below** a level 8 to 10 player. Until Stage 4 (section 12.6) these use their present roll.
- **PvP does not exist**: `Living.CanFight` refuses a fight between two player characters ("Players cannot fight each other."), and a skill with `Target = "enemy"` begins a fight. So no player can put a hostile effect on another player; the rule is for creatures only. A Bureaucrat's helpful spells on friends (Stamp of Approval, Application Denied) are not control effects and roll no save: a friend never saves against a gift.
- **A creature allied to a player** (a Necromancer's skeleton, a Beastmaster's wolf, an Artificer's turret: planned, not built) cannot be attacked by its owner or other players, so no player's control spell is ever cast on one. A hostile creature's effect on an ally goes through the rule as on a player.
- **Wizards.** A wizard is a player character with a level; as a target of a creature they are as any player (and wizards are not in the Chronicle). As casters in the real game they follow the rule. **In the workshop** a wizard may do what they like to sandbox creatures: the stateful function of the daemon refuses a sandbox target or a sandbox caller (it keeps no record for them and answers "Full, no steps", so no state is created by sandbox code), and the pure chance function is callable (below).
- **The sandbox.** `/secure/control` has two kinds of function. **Pure**, named `Query...` (`QuerySaveChance(R, levelDiff, elite, authority)`, `QueryStrength(boss, repeatSteps, made, madeSaveSteps)`, `QueryHalfSeconds(seconds)`): they take plain numbers, change nothing, and are listed as sandbox-aware in `/secure/master.cs` `SandboxAware` (like `/secure/sneak`'s `NoticeChance`) so a wizard's code may compute a table. **Stateful** (`Resolve(...)`): keeps the repeat records and the flood guard; it checks `PreviousObject()` is a skill from `/skills/` (a real object: `Domain == ""`, path under `/skills/`, `/std/`, `/domains/` or `/secure/`) and that the target is a real creature; anything else is refused. No call from sandbox code can change a count.
- **A tag on a sandbox creature is untrusted data** (section 4): `RankOf` ignores the tags of an object whose domain is not "".

## 13. The control daemon: `/secure/control.cs`
A blueprint-only daemon (`ControlRules : MudObject`), nothing saved, like `/secure/sneak`. State: `Dictionary<ObjectRef, TargetRecord>` with `TargetRecord { Dictionary<string, (long lastMs, int count)> effects; List<long> applied; }`, pruned of dead objects on every access and when it passes 200 targets.

```
public int Resolve(ObjectRef caster, ObjectRef target, string effectId, string kind /*"physical"|"mental"*/, int skillRank, int madeSaveSteps = 1)
    // returns 2 (Full), 1 (Half) or 0 (None); tells the caster the line of the table in 12.5 when the result is not Full; records the application.
public int QuerySaveChance(int resistance, int levelAbove, bool elite, int authority)      // percent, 5..90
public int QueryStrength(bool boss, int repeatSteps, bool made, int madeSaveSteps)         // 0..2
public int QueryHalfSeconds(int seconds)                                                  // ceil(seconds / 2), min 1
public bool QueryWouldBeBoss(ObjectRef target)                                            // RankOf(target) == "boss", for the skill's own messages
```
`Resolve` reads `target.QueryLevel()`, `caster.QueryLevel()`, `target.QueryStat(kind + "_resistance")`, `caster.QueryStat("authority")`, `RankOf(target)`, then one `RollPercent`. `Living.RollPercent` is protected, so the daemon rolls with the driver's `Random(1000)` like `/secure/sneak.Roll`, and it has a test setter (`SetTestRolls`, level-10-wizard trusted code only) so tests fix the dice, as `/secure/sneak` does.

A skill then does, for example (the Bureaucrat's slow, in the style of `BlessingSpell.Resolve`):

```csharp
var strength = LoadObject("/secure/control").Call<int>("Resolve", user, target, "slow-forms", "physical", RankOf(user));
if (strength == 0) return;
var seconds = strength == 2 ? RankInt("seconds", rank) : LoadObject("/secure/control").Call<int>("QueryHalfSeconds", RankInt("seconds", rank));
target.Call("AddEffect", "slow-forms", "Forms in Triplicate", seconds, "", 1.5, 0);
```
`AddEffect` is used rather than `AddSpeedEffect`, which would roll the Physical Resistance a second time (the old rule inside it); the id names the source, so a slow from another source stacks multiplicatively, and `Living.QuerySpeedFactor` gains a cap of **2.0** on the product of hostile slows (nothing is over 2.0 today: the spider's 1.25 and the spirit's 1.2 give 1.5).

## 14. Edge cases
- A tag read on a destroyed object: `HasTag` is called on a handle; a dead handle answers false.
- A weapon with two reach tags: the lint rejects it; at run time `QueryProblems` drops the later one in registry order and logs.
- A creature whose `Boss = true` and `Elite = true`: lint error (one `rank`).
- A target with no `QueryStat` (an item): `Resolve` refuses non-creatures and returns Full without state.
- A caster at level 0 or a target at level 0 (a dummy): levels are clamped to at least 1.
- A target that dies after the roll but before the effect is applied: the skill checks `IsAlive` and `IsDead` first (as Blessing does); the record is made only when the effect is applied.
- A caster who is out of the room by the time the spell resolves: the spell's own checks apply; nothing is recorded.
- Two Bureaucrats on one creature: the records are per target, not per caster, so the second pays the repeat steps: nobody gets a free Full by taking turns.
- A creature that is `Peaceful` and cannot be fought: the spell is refused before the daemon is asked (`CanFight`).
- A target in the dark: unaffected; saving throws need no sight.
- Logging out and in: nothing is saved (effects are not saved); the daemon's records forget at a restart.
- `QueryStrength` with `repeatSteps` above 2 or `madeSaveSteps` above 2: clamped.

## 15. Tests
**New:** `tests/tag_test.py` (the driver's test client; fixtures in the style of `tests/aim_test.py`): registry (`QueryFamilyOf` for every word; reserved words refused; exclusive families), `HasTag` and `QueryTags` on every file of section 11 (the tables are the test data), the implied tags for an untagged weapon, a creature's derived `boss`/`alert`/`darksight`/`elite`, a sandbox object whose declared tags include an unregistered word (dropped and logged), no public function that sets a tag (a reflection-style check through `ValidCall` from a wizard's object), `QueryTagSummary` words, `look`, `appraise`, `browse`, `stat`, the wording ladders (a fixed-damage fixture at each tier for each ladder, for the actor, target and a bystander; the dark; critical and block), and unarmed and untagged = plain. `tests/control_test.py`: `QuerySaveChance` and `QueryStrength` against the tables of section 12.4 (every cell), `Resolve` with fixed dice for ordinary, elite, boss, higher-level; the half-time rounding; the repeat window (a second use at 30 s, a third, a use after 61 s); the flood guard; messages for each row of 12.5; a sandbox caller and a sandbox target refused; the legacy path (`madeSaveSteps = 2`) gives None on a made save.
**Lint:** `tests/lint_area.py` fixtures for each of the seven checks (a typo, a reserved word, two reach tags, a weapon missing damage, `Tags.Add("boss")`, a ranged blunt weapon with no ladder), run by the existing area test.
**Existing tests that assert the present wording or lines** (read with the change they need in Stage 3; those marked "plain" do not change because the attacker is unarmed or an untagged fixture):
- `tests/playfeel_test.py`: `LF_BOW` (unchanged: archery is kept), `LF_BLOW` and the regexes around the "wording" section (about lines 1427 to 1520): the `lfsword` fixture is a sword, so its words become the **slashing** ladder (nick, cut, slash deeply, cleave to the bone); `lf_blow`'s regular expressions and the tier-to-word map (`{"scratches": 0, "smashes": 3}`) change; the `lfbow` fixture is unchanged. Also line ~1209 ("You hit", "You scratch", "You smash" with a biter): the `biter` creature is a fixture with no tags: plain, unchanged.
- `tests/attribute_test.py` (lines ~490 and ~518, `You (?:scratch|hit|smash)`, `(?:scratches|hits|smashes) you`): wizards with fists against a dummy: plain, unchanged.
- `tests/class_test.py` (~674 to 818, an unarmed character and "hitter" and "overlord" fixtures): plain, unchanged.
- `tests/area_test.py` (~657, ~700, the large rat and a "gnawer"): a real `large_rat` becomes `bite`: **"You (scratch|hit|smash|miss) a large rat"** is for the player (fists: plain, unchanged); the creature's own lines (`A gnawer (attacks you|misses|scratches|hits)`) use a fixture creature: plain, unchanged; any assertion on a *real* creature's words against the player changes (the rats, wolves and goblins now say nip, bite, maul; stab, cut, slash).
- `tests/timing_test.py` (the anvil creatures, `Goblin Overseer (scratches|hits|misses|smashes) you`, `Goblin minion ...`): anvils are fixtures: plain. **The Overseer's and the minion's lines change** (slashing and piercing ladders): the regular expressions at about lines 508, 520 and 541 become `(nicks|cuts|slashes|cleaves|misses)` and `(pricks|stabs|pierces|runs|misses)`.
- `tests/light_test.py` (~573, `(?:Something|A gunner) (?:scratches|hits|smashes) you`): a fixture: plain, unchanged.
- `tests/return_test.py` (~301, the character's body "Danny" fighting a grey wolf: `Danny (hits|misses|scratches|shoots|smashes|strikes|slashes|cuts)`): the regex already accepts several verbs; if Danny's body holds a weapon it needs that weapon's new ladder (slashing: nicks, cuts, slashes, cleaves; piercing: pricks, stabs, pierces, runs); with fists it is plain and unchanged. Check which when updating.
- `tests/hollow_expansion_test.py` (weapon files' fields), `tests/item_test.py` and `tests/craft_*` do not parse the appraisal's category text; `tests/balance_sim.py check_mudlib` reads weapon and creature numbers, not wording.
- `tests/sandbox_test.py` gains the tag cases above; `tests/redteam` gets one case: a wizard's object calling any `Tags`-changing path on a real weapon (there is none; the case proves the call is refused).
- `tests/sneak_test.py` and `tests/light_test.py` are re-run unchanged: they prove the readers of section 8 still behave.

## 16. Files to change
| File | Change |
|---|---|
| `mudlib/secure/tags.cs` (new) | `TagRegistry`: the table, the read-only functions (section 3) |
| `mudlib/secure/blows.cs` (new) | `BlowWords`: the ladders (section 6) |
| `mudlib/secure/control.cs` (new) | `ControlRules` (section 13) |
| `mudlib/secure/master.cs` | `SandboxAware["/secure/control"]` gets the pure `Query...` names (they are read-only by name already; list for clarity); no other change |
| `mudlib/std/thing.cs` | `Tags`, `HasTag`, `QueryTags`, `QueryTagIn`, `QueryTagSummary`, `Stat` line |
| `mudlib/std/item.cs` | extra tags (`tags+`, reserved: a hook that returns the declared tags until built), the `Long()` sentence |
| `mudlib/std/weapon.cs`, `armor.cs` | `Appraisal` words (section 10); `Weapon` builds its implied tags from `Category` |
| `mudlib/std/living.cs` | `QueryAttackReach`, `QueryAttackDamage`, `QueryAttackKind`, `QueryAtRange`; `CombatAction.Reach/Damage/Kind`; `AddCombatAction` parameters; `BeginFight` and `Attack` (section 8); `StatRaw` "authority"; `QuerySpeedFactor` cap of 2.0 on hostile slows |
| `mudlib/std/monster.cs` | `Elite` flag, `QueryTags()` from the flags, `QueryBoss`/`QueryAlert` as thin readers |
| `mudlib/std/skill.cs` | `WeaponTags`, `NeedsWeaponText` in `CanUse` |
| `mudlib/std/adventurer.cs` | `SneakNotice` and `NoticeRoll` call `HasTag` |
| `mudlib/std/shopkeeper.cs` | `CmdBrowse` adds `QueryTagSummary()` |
| `mudlib/skills/aimed_shot.cs` | `WeaponTags.Add("ranged")` |
| 21 weapon files, 9 armour files, 44 creature files | the explicit tags of section 11 (and `Elite = true` in `goblin_elite_guard.cs`) |
| `tests/lint_area.py`, `tests/tag_test.py` (new), `tests/control_test.py` (new), the existing tests of section 15, `tests/balance_sim.py` | as listed |
| `docs/BUILDING.md` ("A weapon", "Armor", "Creatures"), `docs/ATTRIBUTES.md` (section 3: Authority), `docs/CLASS_SYSTEM.md` (section 5), `docs/GAME_SYSTEMS.md` (weapon table), `docs/SANDBOX.md` (the control daemon row in "The game's systems") | one paragraph each |

## 17. Build order
Each stage is shippable and none changes a combat number until Stage 4.
1. **Tags, no behaviour.** Registry, `Thing` API, `Item` and `Monster` readers, the flags as tags, `Elite`, the explicit tags in all 74 files, `stat`, `look`, `appraise`, `browse`, the lint and `tests/tag_test.py`. Run the whole suite unchanged. (One agent.)
2. **Reach.** The `Living` helpers, `AddCombatAction` parameters, the five edits of section 8's first four rows, `Skill.WeaponTags`, Aimed Shot. The light, opener and Aimed Shot tests pass unchanged.
3. **Wording.** `/secure/blows`, `Living.Attack`, creature actions' tags (the natural kinds), the conflicts of section 11 settled, the tests of section 15 updated.
4. **Control.** `/secure/control`, `authority`, the slow cap, `tests/control_test.py`, the simulator's model (section 18), with a throwaway test spell in the test fixtures; no player sees it. Then the Bureaucrat (bureaucrat.md), which is its first user. After the simulator has measured them: decide on Shield Bash and the creatures' effects (section 12.6).

## 18. What `tests/balance_sim.py` must model
- `check_mudlib` reads `Boss`, `Alert`, `Elite`, `DarkSight` and the tags from the files and compares them with `SNEAK_ALERT`, `SNEAK_BOSS` and `SNEAK_ELITE` (the latter becomes real).
- A **control** module with the `Resolve` formula of section 13 (the resistance by level, the level bonus, elite, Authority, the 5 to 90 clamp), the three strengths, half-time, the repeat window and the flood guard, with the creature's real level and boss status, so every table in section 12.4 is a function the simulator prints (`python3 tests/balance_sim.py control`).
- Slow as a speed factor on a creature's Reaction Time (x1.5 or the half-time term), a bar on melee actions, an interrupt of a preparing action and a forced-target effect, each as an event on the combat clock.
- The Warrior's Shield Bash under the rule (as an option, off by default) so Stage 4's decision has numbers: the 81% Warrior in the standard test against the Overseer.
- Nothing about tags changes a damage number, so the existing tables are the regression test: they must not move.

## Out of scope / future
- Elements, affinities, weaknesses and resistances by damage type (reserved families); a damage bonus for a type; any change to armour that depends on a tag.
- A thrown weapon and its ladder; a crossbow or sling and its reach and ladder (`Ladder("ranged","blunt")`).
- Saved extra tags (section 9) and the crafting or enchanting that would use them.
- A party system (the Bureaucrat's ally spells, bureaucrat.md section 12, are the first thing to need one).
- A `lore` or Appraise that tells a creature's nature to the player.
- A resistance on the creature side that is not an attribute formula (a boss's own `Resist` field); today a boss's save is the same formula as anyone's.
- The remaining control effects (fear, charm, knockdown, blind) can be added as kinds of the same rule; none is designed here. A blinded creature is, by `docs/specs/light.md`, treated as `DarkSight = false`; that stays.
