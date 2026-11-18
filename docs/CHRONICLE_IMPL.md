# Chronicle pipeline: implementation notes

Implements the pass 1 scope of [CHRONICLE.md](CHRONICLE.md) as new files only. Gameplay code does not call it yet; the hooks are in section 6. Test: `LESMUDOS_TEST_PORT=4817 python3 tests/chronicle_test.py`.

## 1. Files added

| File | What it does |
|---|---|
| `mudlib/secure/events.cs` | The event service (`/secure/events`). The single entry point `Report`. Checks the caller, reads the catalogue, applies "wizards make no history", computes ESS, awards Renown, starts rumors, writes Chronicle entries, keeps the aggregate counters. |
| `mudlib/secure/chronicle.cs` | The Chronicle daemon. Append-only entries, first occurrences, lifetime contribution, the calendar, the Halls and records, and the text for every `chronicle` command. |
| `mudlib/secure/renown.cs` | The Renown ledger. Renown, Infamy (a number, no sources), tiers, earned titles, the chosen title, per-character "already credited" and "discovered" records, NPC recognition lines. |
| `mudlib/secure/rumors.cs` | The rumor queue: last 50 per region, saved, `DrawRumor` for gossips. |
| `mudlib/events/*.cs` | The catalogue, one file per event-id prefix (`kill`, `quest`, `boss`, `discover`, `travel`, `level`, `class`, `item`, `renown`, `aggregate`), and `places.cs` (room path to area and region; the Pathfinder lists). |
| `mudlib/std/chronicle_ledger.cs` | The readable ledger object (`read chronicle`, `chronicle ...`). A fixed object; place it in a room. |
| `tests/fixtures/chron_harness.cs` | Test harness. **Deliberately not under `mudlib/`**: it can report any event for any player, so it must never ship. The test copies it to `/secure` of its own mudlib copy. |
| `tests/chronicle_test.py` | 198 checks. |

All four daemons are blueprint only: every write checks `PreviousObject()` and a clone refuses. Only `/secure/events` (blueprint) may write the Chronicle, the ledger and the rumors. The event service accepts reports only from objects whose program is under `/std`, `/secure`, `/domains`, `/classes`, `/skills` or `/events`; anything else (every `/wiz` object) is refused and logged (`QueryRefusals`).

## 2. Public methods

**`/secure/events`**
- `string Report(string eventId, ObjectRef actor, List<ObjectRef> participants, ObjectRef room, string subject, Dictionary<string,string> names)`. `participants` may be null or omit the actor; `room` may be null (the first credited player's room); `subject` tells apart things of one type (boss id, area id, level, class) and is part of what "first" means; `names` supplies narrative words (`name`, `class`, `level`, `area`, `region`, `count`). Returns `ok ess=N class=Class first=0|1 entry=ID rumor=0|1 credited=a,b`, or `ignored: wizards make no history`, `ignored: already credited`, or `refused: reason`.
- `int PreviewEss(string eventId, int players, bool first)`: pure score, no side effects (-1 if unknown). `string ClassName(int ess)`, `string Describe(string id)`.
- `int QueryCounter(string eventId, string region, string subject)`, `List<string> QueryRefusals()`, `List<string> QueryCatalogueErrors()`, `bool ReloadCatalogue()` (trusted callers), `string Status()`, `bool QueryPersistent()`.

**`/secure/chronicle`**
- Calendar: `string DateText()`, `string DateCommand()`, `string DateFor(long epochDay, long unixDay)` (pure), `string DateOfTimestamp(long seconds)`, `int YearLength(long startDay)`, `long QueryEpochDay()`.
- Queries: `int QueryEntryCount()`, `string QueryEntry(int id, string field)`, `bool HasOccurred(string key)`, `string QueryOccurred(string key, string field)` (key is `eventId|subject`), `List<int> QueryEntriesFor(string name)`, `List<int> QueryRecent(int n)`, `long QueryContribution(string name)`, `long QueryEntryTotal(string name)`, `List<string> QueryHeroes()`, `List<string> QueryLegends()`.
- Text: `RecentText(int)`, `EntryText(int)`, `SearchText(string)`, `EntriesForText(string)`, `FirstsText()`, `HeroesText()`, `LegendsText()`, `RecordsText()`, and `ChronicleCommand(string args)` which dispatches `""`, `<number>`, `search <w>`, `about <name>`, `firsts`, `heroes`, `legends`, `records`.
- Writes (event service only): `AddEntry`, `MarkOccurred`, `AddContribution`.

**`/secure/renown`**
- `int QueryRenown(name)`, `int QueryInfamy(name)`, `string QueryTier(name)`, `string TierFor(int)`, `List<string> QueryTitles(name)`, `string QueryActiveTitle(name)`, `bool HasDone(name, key)`, `List<string> TopRenown(n)`, `List<string> NamesAtOrAbove(n)`.
- Text: `RenownText(name)`, `TierLine(name)`, `RecognitionLine(name)` (empty below Recognized), `TitleCommand(string args)` (from the calling player's body only), `ChooseTitle(string name, string args)` (trusted code).
- Writes (event service only): `Award`, `AddInfamy`, `GrantTitle`, `MarkDone`, `AddDiscovered`.

**`/secure/rumors`**: `List<string> QueryRumors(region, n)`, `string DrawRumor(region)` ("" if none; "" as region means all), `int QueryCount(region)`; `AddRumor` (event service only).

## 3. Catalogue format

`/events/<prefix>.cs` holds every event type whose id starts with `<prefix>.`. The class has one method, `List<string> Definitions()`; each string is `key=value` lines. Adding a type is adding a string (or a new prefix file). Aggregate definitions are found in `/events/aggregate.cs` and in any group already loaded.

Keys: `id title kind chron sub category impact rarity persistence flags first milestone once discovery listed renown renown_first headline text significance rumor historic_name tags repeat repeat_category repeat_impact repeat_rarity repeat_persistence aggregate_of aggregate_subject aggregate_step`.

- Impact, rarity and persistence take the spec's names (`Settlement`, `VeryRare`, `ServerFirst`, `LongTerm`...) or a number; `category` is a number.
- `flags`: `storyline regional world seasonal developer` (x1.5 x1.25 x2.0 x1.25 x2.0, multiplied together).
- `first=yes`: the first time ever earns x2.0. `listed=firsts|records`: a recorded first, shown by `chronicle firsts` / `records`. `milestone=yes`: counts as a Server First for the Hall of Legends.
- Repeat scores (used every time after the first occurrence): category and persistence stay, impact becomes Personal and rarity Common, unless `repeat_*` says otherwise; `repeat=same` keeps all the full scores (aggregates). `once=no` lets the same character be credited again (kills, aggregates); otherwise a character is credited once per `eventId|subject`.
- Templates use `{actor} {participants} {area} {region} {date} {name} {subject} {level} {class} {count}` and any key passed in `names`.

**Worked example, `boss.goblin_overseer`** (category 25, Settlement 100, Uncommon 25, Storyline, first=yes), solo. First time ever: (25 + 100 + 0 + 25 + 0) x 1.5 x 2.0 = 450, a Historical entry (headline "The Quieting of the Cellars"). Every later character: Personal and Common, so (25 + 0 + 0 + 0 + 0) x 1.5 = 37.5, rounded half up to 38, nothing recorded. Two players on the first kill: participation +10, (25+100+10+25) x 1.5 x 2 = 480.

All sixteen rows of the design's section 4 table are checked by the test.

## 4. Differences from the owner's ESS spec (and the bible)

CHRONICLE.md wins, as instructed. In addition:
1. Repeat scoring (Personal impact, Common rarity) and the per-event first rule are CHRONICLE.md additions; the spec has only a flat x2.0 first occurrence.
2. Server First rarity only on milestone types; the Great Stump is a recorded first without it.
3. The spec says only the highest category score applies; the catalogue stores one value, so that holds by construction.
4. Spec "Historical: minor Renown bonus" is the CHRONICLE.md table (+5, +10, +25, +50, +100); it also applies to Renown-tier events, which are raised inside the service.
5. Rounding is half up, in decimal arithmetic.
6. The bible's multipliers (Regional x1.5, Permanent x2.5, Storyline x2.0) and thresholds (25/100) are not used; the spec's are.
7. The spec's "Hall of Heroes review" is automatic (contribution >= 5,000 or entry total >= 10,000). Hall of Legends "Server First achievements" means firsts with `milestone=yes`, so the Stump does not qualify.
8. Per-character credit: Renown, contribution, and the entry credit go once per `eventId|subject` per character (`once`), so repeating an action cannot farm them. The spec says nothing on this.

## 5. Ambiguities and choices

- **Persistence needs master.cs.** The master lets only the wizard registry, the vault and player bodies save. Without the two lines in section 6 the daemons run but forget everything at restart (`QueryPersistent` says False). The test patches its own copy of master.cs with exactly those lines.
- Wizard as primary actor with other players: recorded for the others; the first credited player becomes the primary actor. Participation counts only the credited players.
- Leap Turning: six days when a real 29 February falls in the first 365 days of that Hollowmere year.
- Year 1 starts when the Chronicle daemon first loads. Preload it (section 6) so that means first boot.
- Renown tiers Recognized, Acclaimed, Renowned, Legendary, Mythic raise their own events (bible has none for Famous, Heroic). Mythic is a Server First milestone (4,500 x 2 = 9,000), following CHRONICLE.md section 3 over the table row.
- Area and region come from `room.QueryArea()` / `QueryRegion()` if the room has them, else `/events/places.cs` by program path, else "an unmapped corner of Hollowmere" / "Hollowmere".
- Pathfinder is implemented (`/events/places.cs`, `Regions()`), but Greenvale is marked `open` so nobody gets it before the area list is complete.
- Aggregates: counted per `eventId|region|subject` (creature kind, e.g. `goblin`, shared by all goblin variants); the first thousand in each region is that region's first (1,125, then 563).
- Infamy has no sources: `AddInfamy` exists, `renown` shows it only above zero.
- Counters and every other write are saved at once (small files).
- Level 3+ wizards can `clone` anything in `/secure`; that is why the harness is not shipped. Note `/domains` is writable by level 5 wizards under the existing rules, and `/domains` is a trusted tree here; that is a hole in the design's trust model, not in this code.

## 6. Integration list

**master.cs** (`ValidRead`/`ValidWrite` via `OwnsSaveFile`, and `Preload`):
```csharp
if (path is "/secure/chronicle" or "/secure/renown" or "/secure/rumors" or "/secure/events") return !caller.IsClone && caller.ProgramPath == path;
// Preload: add "/secure/chronicle", "/secure/renown", "/secure/rumors", "/secure/events"
```

**Hooks** (all: `LoadObject("/secure/events").Call<string>("Report", id, actor, participants, room, subject, names)`; the result may be ignored; call as often as convenient, a character is credited once):

| Where | Call |
|---|---|
| Creature death (monster.cs/living.cs), ordinary creature | `"kill.ordinary"`, subject = creature kind (`"goblin"`), participants = players who dealt damage and are present |
| Same, Goblin Overseer | `"boss.goblin_overseer"`, subject `"goblin-overseer"` |
| Same, other dungeon boss / world boss | `"boss.dungeon"` / `"boss.world"`, subject = boss id, `names["name"]` |
| Quest state set to completed (Rowan, `QueryQuest` side) | `"quest.clear_the_cellar"` / `"quest.overseers_orders"`; others `"quest.local"` / `"quest.story"` with subject = quest id, `names["name"]` |
| First entry into an area per character (room base or area entry) | `"discover.area"`, subject = area id, `names["name"]` |
| `hidden_cache.cs` entered | `"discover.hidden"`, subject `"hidden-cache"`, name "Hidden Cache" |
| First room of the Forgotten Cellars | `"discover.secret_dungeon"`, subject `"forgotten-cellars"`, name "The Forgotten Cellars" |
| Great Stump room (Oakenfell) | `"discover.great_stump"`, subject `"great-stump"` |
| gate.cs, first walk out with the pass | `"travel.pass"` |
| Level gained, at 10, 25, 50, 75, 100 | `"level.milestone"`, subject = level |
| Class framework | `"class.advanced"` / `"class.prestige"`, subject and `names["class"]` = class name |
| Great items made | `"item.legendary"` / `"item.artifact"`, `names["name"]` |

Renown tiers and aggregates are raised by the service itself.

**Commands for player.cs** (`Name` is the player's name; add each in the AddAction block):
```csharp
AddAction("renown", a => { Write(LoadObject("/secure/renown").Call<string>("RenownText", Name)); return true; });
AddAction("date",   a => { Write(LoadObject("/secure/chronicle").Call<string>("DateCommand")); return true; });
AddAction("title",  a => { if (IsWizard(1)) return false; /* keep the wizard free-text title */ Write(LoadObject("/secure/renown").Call<string>("TitleCommand", a)); return true; });
// in CmdScore, one more line:
Write(LoadObject("/secure/renown").Call<string>("TierLine", Name));
```
**NPC recognition** (npc.cs, in `talk`, before the greeting): `var line = LoadObject("/secure/renown").Call<string>("RecognitionLine", who.Call<string>("QueryName")); if (line != "") Say/Write(line)`.
**Gossips** (Mara, Harland, the Prospector; `ask <npc> about news` / `rumors`): `LoadObject("/secure/rumors").Call<string>("DrawRumor", "Greenvale")`; empty means nothing to tell.

**Ledger placement**: in `guild.cs` `Create()`: `MoveObject(CloneObject("/std/chronicle_ledger"), Self);`. The Hall of Legends room in Oakenfell does the same.
