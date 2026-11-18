# Faction Reputation: design

How the owner's Faction Reputation chapter ([specs/design-bible.md](specs/design-bible.md)) becomes a system in LesMudOS. This is the design for the first pass; nothing here is built yet. It sits beside [CHRONICLE.md](CHRONICLE.md) (Renown and the Chronicle), [CLASS_SYSTEM.md](CLASS_SYSTEM.md) and [areas/oakenfell.md](areas/oakenfell.md).

Renown says how widely a character is known. Reputation says what one faction thinks of them. They are separate numbers kept by separate daemons, and a deed can move both.

## 1. Scope of pass 1

In:

- A standing with each of the seven factions of Greenvale, saved with the character's name.
- The owner's nine tiers and point ranges.
- Gains and losses reported by trusted game code, with rival and ally spillover, diminishing repeats and donations.
- Discovery of factions (a faction is Unknown until met).
- What a player sees: the `standing` command, NPC remarks, exact numbers from faction contacts and ledgers.
- Hooks that other systems read: shop discounts, item requirements, dialogue and exits gated by tier, faction titles.
- First content: the envoys in the Envoys' Gallery, the recruiter in the Free Lodge, the herbalist in the North Rows, the Riverwatch sergeant, and reputation on the two existing quests.

Later passes: faction storage, trainers, mounts, companions, housing, faction campaigns, leadership, world events, and reputation loss for crimes (theft, smuggling, murder) that need systems which do not exist yet.

## 2. Factions

Defined as data in `/factions/<id>.cs`, one file each, read by the registry `/secure/factions`. `/factions` is a trusted directory (level 10 to change), like `/events` and `/classes`.

| Id | Name | Motto | Starts |
|---|---|---|---|
| `assembly` | The Greenvale Assembly | Prosperity Through Unity | Known, Neutral 0 (+100 for a character who starts in Vincent's Hollow) |
| `hall` | The Hall of Legends | Greatness Is Chosen | Known, Neutral 0 |
| `consortium` | The Merchant Consortium | Trade Builds Tomorrow | Known, Neutral 0 |
| `riverwatch` | The Riverwatch Rangers | Ever Vigilant | Known, Neutral 0 |
| `thornpact` | The Thorn Pact | The Forest Remembers | Unknown |
| `freecompanies` | The Free Companies | Coin Before Crown | Unknown |
| `ash` | The Circle of Ash | Knowledge Demands Sacrifice | Unknown, hidden |

A faction file holds: id, name, short name, motto, headquarters, one-paragraph description, whether it is hidden, its relations (section 5), its tier titles (section 9), and the remark lines its people use for each tier (section 7).

**Hidden** applies to the Circle of Ash only. A hidden faction does not appear in `standing`, in any list, or in any NPC's words until the character has discovered it; faction contacts of other factions deny knowing of it, as the envoys already do.

## 3. Tiers

The owner's scale, unchanged:

| Tier | Points |
|---|---|
| Unknown | not yet discovered |
| Hostile | −1000 to −501 |
| Suspicious | −500 to −1 |
| Neutral | 0 to 999 |
| Friendly | 1,000 to 2,999 |
| Trusted | 3,000 to 5,999 |
| Honored | 6,000 to 9,999 |
| Exalted | 10,000 to 19,999 |
| Legendary | 20,000 and above |

Points are clamped to −1000 below. There is no cap above; Legendary is a tier, not a ceiling.

The bible has a second, shorter list of tiers in the Greenvale chapter that stops at Exalted. The longer list in the reputation chapter is the one used.

### Discovery

A character discovers a faction the first time any of these happens:

- they gain or lose standing with it;
- they `talk` to or `ask` a declared member of it (an NPC with `Faction` set, section 8);
- game code calls `Discover` for them (a room, a book, a quest step).

On discovery the character is told once, in one line: `You have learned of the Thorn Pact.` Standing starts at 0 unless the cause was a loss.

Unknown is never returned to. A character at −1000 is Hostile, not Unknown.

## 4. Gaining and losing standing

One entry point, on `/secure/factions`:

```
Adjust(ObjectRef who, string faction, int amount, string reason, string key)
```

- **Caller check.** As the event service does it: accepted only from programs in the trusted trees, never from `/wiz`. Wizards of any level gain and lose no standing ("Wizards make no history" applies here too), and are told so in one line the first time in a session.
- **reason** is a short phrase used in the message and the log: `"for delivering the Consortium's ledgers"`.
- **key** identifies the source for the repeat rule below: `quest.clear_the_cellar`, `bounty.wolves`, `donate.gold`. Null means one-off and never diminished.

The return value says what happened (`"ok +100 friendly"`, `"ignored: wizard"`, `"refused: ..."`), as `Report` does.

### Sizes

Guide values, so that builders award consistently. They follow the owner's examples.

| Deed | Standing |
|---|---|
| Errand, delivery, small favour | +25 |
| Ordinary quest for the faction | +50 to +100 |
| Defeating a named enemy of the faction | +150 |
| A quest chain's end; a regional story arc | +500 |
| Repeatable task (bounty, patrol, escort, contract) | +10 to +25 |
| A discovery the faction values | +50 to +200 |
| World event defended | +250 to +1,000 |
| Attacking a member | −100 |
| Killing a member | −500, and at least down to Suspicious |
| Failing or abandoning the faction's contract | −50 to −150 |
| Helping a declared enemy against it | −100 to −500 |

### Repeats

A keyed gain is worth less the more often the same character repeats it within one game day (24 real hours, counted from the first):

| Times today | Worth |
|---|---|
| 1st to 3rd | 100% |
| 4th to 6th | 50% |
| 7th onward | 10%, at least 1 |

A quest that can only be completed once never meets this rule. Losses are never diminished.

### Donations

`donate <amount> gold to <faction contact>` at a contact who accepts donations (a flag on the NPC). 10 gold is 1 point. Donations follow a steeper daily rule per faction: the first 50 points in a day at full value, the next 50 at half, nothing beyond. That is 100 points a day at most for 1,500 gold, so gold alone takes a character from Neutral to Friendly in ten days and no further in reasonable time; the higher tiers have to be earned. Donations cannot raise a character above the top of Friendly (2,999). Goods and relics as donations wait for a later pass.

### Messages

Every change tells the character, without numbers:

- `The Riverwatch Rangers think better of you for clearing the wolves from the road.`
- `The Thorn Pact thinks less of you.` (spillover, no reason given)
- On crossing a tier: `You are now Friendly with the Riverwatch Rangers.` A loss across a tier: `The Merchant Consortium now regards you as Suspicious.`

Small spillover of fewer than 10 points gives no message, so that ordinary play is not a stream of faction lines; it shows in `standing` only as a changed word when a tier is crossed.

## 5. Relations between factions

A gain with one faction spills onto others. Each faction file lists its relations as a percentage of any **gain of 50 or more**; smaller gains do not spill. Losses do not spill in pass 1 (hurting the Consortium does not make the Thorn Pact like you; that has to be earned from the Pact).

| Gaining with | Allies (gain) | Soft rivals (loss) | Rivals (loss) |
|---|---|---|---|
| Assembly | Consortium +10%, Riverwatch +10%, Hall +5% | Free Companies −10% | Thorn Pact −25% |
| Hall of Legends | | | |
| Consortium | Assembly +10%, Riverwatch +5% | Free Companies −10% | Thorn Pact −25% |
| Riverwatch | Assembly +10%, Hall +5% | | Thorn Pact −25% |
| Thorn Pact | | Assembly −10%, Riverwatch −10% | Consortium −25% |
| Free Companies | | Assembly −10%, Consortium −10% | |
| Circle of Ash | | | |

Notes:

- The Riverwatch row is the owner's own example: +100 Riverwatch, −25 Thorn Pact. The soft figure is the owner's too: +100 Assembly, −10 Free Companies.
- The Hall of Legends is neutral ground. Nothing spills to or from it except small goodwill from the Assembly and Riverwatch; its only rivals are "anyone seeking to monopolize power", which is story, not arithmetic.
- The Circle of Ash has no known relations. Its file leaves them empty until the owner's story says otherwise.
- Spillover never discovers a faction. A character who has not met the Thorn Pact loses nothing with it; the loss starts counting once they have. This keeps the Pact and the Circle from being revealed by a number moving.
- Spillover is applied once. A spilled change does not spill again.
- Spillover losses cannot take a character below Neutral 0 by themselves. Becoming Suspicious or Hostile takes a direct act against the faction. Otherwise a devoted Ranger would find the Pact hostile without ever having met it in anger, and "a Knight may become a Thorn Pact ally" would be false in practice.
- A single award may override spillover (`AdjustOnly`) when the story calls for it, for example a quest that reconciles two factions.

## 6. The standing ledger

`/secure/factions` keeps, for each character name: a map of faction id to points for discovered factions, the set of discovered factions, the day's repeat and donation counters, and the chosen faction title. It saves as the vault and wizlist do and needs its line in the master object's save rules and preload list.

Standings are kept by character name, outside the player object, so that they survive a body being rebuilt and can be read while the character is offline (for monuments and the Chronicle).

Public queries: `Points(name, faction)` (0 if undiscovered), `Tier(name, faction)` (0 Unknown to 8 Legendary), `TierName`, `Knows(name, faction)`, `AtLeast(name, faction, tierName)`, `StandingText(name, exact)`, `RemarkLine(name, faction)`.

### Renown and the Chronicle

- Reaching **Honored**, **Exalted** and **Legendary** with a faction reports an event to the event service (`faction.honored`, `faction.exalted`, `faction.legendary`, subject = faction id). Legendary is a Chronicle entry and, the first time anyone reaches it with that faction, a recorded first. These three catalogue entries are added to `/events/faction.cs`.
- Nothing flows the other way: Renown does not change standing. A faction contact may still remark on a character's Renown.

## 7. What the player sees

### `standing`

Lists discovered factions with the tier as a word, in the order of the table in section 2:

```
Your standing in Greenvale:
  The Greenvale Assembly      Neutral
  The Hall of Legends         Neutral
  The Merchant Consortium     Friendly
  The Riverwatch Rangers      Trusted
```

No numbers. A hidden or undiscovered faction is not listed. `reputation` and `factions` are the same command. `standing <faction>` adds the faction's motto and one line about where it is found.

### Exact numbers

As the owner specifies, exact numbers come from people and books, not from the command:

- **Faction contacts.** `ask <contact> about standing` gives the exact figure for the contact's own faction, and how far the next tier is: `"The Rangers hold you at 3,240. Another 2,760 and you will be Honored among us."` The four people in the Envoys' Gallery are contacts; so are the Free Lodge recruiter and the Riverwatch duty sergeant. The Hall's clerk answers for the Hall and, being the keeper of the ledgers, for every faction the character has discovered except hidden ones.
- **Reputation ledgers.** A readable object (`/std/reputation_ledger`) placed in a faction's house shows the reader's exact standing with that faction.
- Guild memberships and lore skills, also named by the owner, are later passes.

### NPC remarks

Any NPC with `Faction` set greets a character according to tier, the first time they `talk` in a visit, with a line from the faction file. The owner's examples set the tone:

| Tier | Riverwatch example |
|---|---|
| Hostile | "You have nerve, showing your face to the Riverwatch." |
| Suspicious | "The Rangers have not forgotten what you did." |
| Neutral | (no remark) |
| Friendly | "The Riverwatch knows your name." |
| Trusted | "Riverwatch speaks highly of your efforts." |
| Honored | "An honor. The roads are safer for you." |
| Exalted | "The Captain asks after you by name." |
| Legendary | "They tell recruits about you at Watchfort Crossing." |

Each faction has its own eight lines in its own voice. An NPC may override them.

## 8. Hooks for builders

All of these are small additions to existing standard objects.

- **Membership.** `Faction = "riverwatch";` on any living thing. Used for remarks, discovery, and reputation loss when attacked: attacking a member costs −100 once per fight, and killing one costs −500 and drops the killer to the top of Suspicious if they were above it. City watch count as Assembly. Peaceful NPCs cannot be attacked at all today; this matters when they can be, and for faction creatures in the wild (a Thorn Pact extremist, a Free Companies deserter) which are members without being peaceful. A creature can be marked `FactionOutcast = true` so that killing it costs nothing with its own faction.
- **Enemies.** `EnemyOf("consortium", 10, "bounty.bandits")` on a creature: killing it awards that standing through the normal rules, keyed for the repeat rule.
- **Shops.** A shopkeeper with `Faction` set gives the owner's "minor discounts": 5% at Friendly, 10% at Trusted, 15% at Honored and above; adds 25% at Suspicious; and refuses to trade at Hostile. A stock line may require a tier: `AddStock(path, price, "trusted")`. Below it the item is listed as `(Trusted with the Riverwatch Rangers)` and cannot be bought.
- **Items.** `RequireStanding("riverwatch", "trusted")` on a weapon, armor or other item: it cannot be wielded, worn or used below that tier. The owner's examples: Riverwatch Longbow (Trusted), Assembly Guard Shield (Honored), Merchant Caravan Ring (Friendly), Circle of Ash Grimoire (Exalted).
- **Dialogue.** `AddTopic(keys, text, "riverwatch", "friendly")`: the topic exists only at or above that tier, so contacts open up as standing rises (the owner's "faction secrets" at Exalted are topics and rooms).
- **Exits.** `StandingExit("north", "riverwatch", "trusted", "The sentry bars the way. \"Rangers' business beyond.\"")` on a room.
- **Quests.** Quest givers award standing through `Adjust` when a quest is completed, and may require a tier before offering one.

Sandbox code under `/wiz` can read standing (so a wizard can build and test faction content) but cannot change it; `Adjust` refuses untrusted callers, and published content gains the right when it is approved into `/domains`.

## 9. Titles

Each faction grants one title at Honored, one at Exalted and one at Legendary. They join the earned titles the Renown ledger already keeps and are chosen with the same `title` command; a title is lost if standing falls below its tier.

| Faction | Honored | Exalted | Legendary |
|---|---|---|---|
| Assembly | Friend of Oakenfell | Warden of Greenvale | Voice of the Assembly |
| Hall of Legends | Companion of the Hall | Exemplar of the Hall | Legend of the Hall |
| Consortium | Merchant Protector | Factor of the Consortium | Master of Coin |
| Riverwatch | Riverwatch Scout | Riverwatch Warden | Captain of the Watch |
| Thorn Pact | Friend of the Forest | Thornwarden | Champion of the Thorn Pact |
| Free Companies | Blade for Hire | Company Captain | Free Marshal |
| Circle of Ash | Seeker of Ash | Adept of the Circle | Keeper of Ancient Ash |

Friend of Oakenfell, Warden of Greenvale, Voice of the Assembly, Merchant Protector, Riverwatch Scout, Champion of the Thorn Pact and Keeper of Ancient Ash are from the owner's bible. The rest were proposed to fill the table and the owner has accepted them.

## 10. Classes

No class is tied to a faction, as the owner requires. The natural leanings (Knights and the Assembly, Rangers and Riverwatch, Druids and the Thorn Pact, Necromancers and the Circle) appear only as:

- one extra line of greeting from that faction's people to a character of that class; and
- for a character who chooses that advanced class, a single gift of +100 standing with that faction if they have discovered it, with no spillover.

Nothing is ever refused to a class on faction grounds, or the reverse.

## 11. First content

Enough to make the system visible from the first hour in Oakenfell:

1. **Existing quests.** Clearing the cellar: +50 Assembly. The Overseer's Orders (the goblin boss): +150 Assembly and +50 Riverwatch, given as two direct awards without spillover.
2. **Vincent's Hollow.** The guard is Assembly; the traveling merchant is Consortium. This matches the owner's starting relationships: strong Assembly, moderate Consortium, minor Riverwatch.
3. **Contacts.** The four in the Envoys' Gallery, the recruiter in the Free Lodge (meeting her discovers the Free Companies) and the Riverwatch duty sergeant get `Faction`, the `standing` topic and, for the envoys and the recruiter, donations.
4. **The herbalist.** Asking the North Rows herbalist about the forest or the Pact discovers the Thorn Pact. She is not a contact and gives no numbers.
5. **Repeatable work.** One bounty each from three contacts, using creatures that exist: wolves on the Vale Road for the Riverwatch (+15, keyed), a Consortium delivery between the Caravan Office and the River Trade Office (+25, keyed), a notice-board errand for the Assembly (+25, keyed).
6. **Shops.** The outfitter is Consortium (discounts apply). The Fish Market stays unaligned.
7. **A first reputation item.** The Merchant Caravan Ring at the Consortium factor, requiring Friendly, as the owner's example.
8. **The Circle of Ash.** Nothing. The visiting scholar in the Athenaeum is not marked as a member and discovers nothing. The Circle's first discovery belongs to the owner's story.

## 12. Build order and tests

1. `/secure/factions`, the seven faction files, tiers, discovery, `Adjust` with caller check, repeats, spillover, save and restore.
2. `standing`, messages, remark lines, contacts and the ledger object.
3. Hooks: membership and attack penalties, enemies, shops, items, topics, exits.
4. Titles and the three events to the Chronicle.
5. First content.

`tests/faction_test.py` (port 4818): tier boundaries on both sides of every threshold; discovery by each route and the one-line message; spillover amounts for every row of the relations table; spillover neither discovering a faction nor pushing below Neutral; the repeat table across a day boundary; donation limits and the Friendly cap; wizards unaffected; untrusted caller refused; hidden faction absent from `standing` and from the Hall clerk; discounts, surcharges and refusal at a faction shop; an item refused below its tier and usable at it; a title granted and withdrawn; the three tier events reaching the Chronicle; persistence across a restart.

## 13. The owner's decisions (18 November 2026)

1. **Spillover floor.** Spillover stops at Neutral 0, as in section 5. Only a direct act against a faction makes it Suspicious or Hostile.
2. **Donations.** The proposed rule stands (10 gold a point, 100 points a day for each faction, nothing past the top of Friendly), and every figure in it is configuration, not code: gold per point, the daily full-value and half-value bands, and the tier cap are read from `/factions/config.cs`, so they can be changed without touching the system. The repeat table in section 4 and the spillover threshold of 50 live in the same file.
3. **Starting standing.** A character who starts in Vincent's Hollow begins with one bump with the Greenvale Assembly: +100, given once at character creation, with no spillover and no message beyond the ordinary one. The amount and the faction are set on the starting location, so a later starting location (Oakenfell may become one) can give a different bump or none. Everything else starts at Neutral 0.
4. **Unknown factions.** The Thorn Pact and the Free Companies start Unknown and are discovered by meeting them. The Circle of Ash is hidden.
5. **Titles.** The table in section 9 is accepted as written.
