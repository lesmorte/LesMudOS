# Score

`score` (and `sc`; a player's bare `stat`) leads with who the character is and where they stand in the Chronicle, with the numbers below. The layout is made by `/secure/scoresheet`; the numbers are the character body's own (`CmdScore` in `/std/adventurer`). Nothing the old score showed is dropped.

## Layout

```
╔══════════════════════════════════════════════╗     48 columns
║              Rayek the newcomer              ║     name and free title (the `title` command), centred
║             "The Cellar Delver"              ║     the Renown title the player chose; the line is left out if none
╚══════════════════════════════════════════════╝

Scout • Class Zealot • Level 5                        the calling; "Class X" only with an advanced class; "No calling yet" before one is chosen
Recognized throughout Vincent's Hollow.               one sentence for the Renown rank (not for a wizard)

Vitality   45/58
Endurance  106/108
Focus      20/24

Chronicle Standing
─────────────────────────
Renown Rank: Recognized                               (and "Infamy: N" if any)
Notable Deeds: 2
Chronicle Entries: 2

Latest Deeds
 • Cleared the Infested Cellar                        the newest three, newest first; " None yet." when there are none
 • Recovered the Overseer's Orders

Combat Summary
─────────────────────────
Damage 8-12   To hit +3   Defense 1   Armor 0
Dodge 5%   Block 0%   Critical 6%
Reaction Time 2.5 (base 2.0, a hunting bow +0.5). Your attack takes 7.0 seconds.

Attributes
─────────────────────────
Might 5   Grace 9   Vigor 5   Insight 6   Spirit 5   Resolve 6
Attribute points to place: 2 (improve ATTRIBUTE)      only when there are points
Physical Power 10   Precision 18   ...                 the derived values, wrapped
Effects: ...                                           only when there are effects

Progress
─────────────────────────
Experience 142 (158 more for level 3)   Gold 18   Carry Capacity 75 lb
Area The Forgotten Cellars   Region Greenvale
Equipment: weapon: a hunting bow          ("You have nothing equipped." when so)
You have no quests.                       (or Active / Completed quests)
You are resting: ...                      state lines, only while true
You are sneaking (rank I): ...
```

No line is wider than 80 columns: a longer line is wrapped at a space (`Wrap`), a row of items is filled in as many to a line as fit (`Columns`). Headings are bold and the name is in the title style; with colour off the layout reads the same.

## Mappings

* **Title line.** The Renown ledger already has earned, selectable titles (`/secure/renown`: `QueryActiveTitle`, set with the Renown `title` command). The quoted line is the chosen one. The name row is `Short()`: the name and the free title the `title` command in `/std/player` sets. Wizards have no Renown title line.
* **Rank sentence** (Renown tier, from the ledger's tiers):
  Unknown: "A stranger to the Vale." Local: "Known by sight to the folk of Vincent's Hollow." Recognized: "Recognized throughout Vincent's Hollow." Acclaimed: "Acclaimed across the length of Greenvale." Renowned: "Renowned on every road of Greenvale." Famous: "Famous in every tavern from the Hollow to Oakenfell." Heroic: "A hero of Hollowmere, and the songs say so." Legendary: "A living legend of Hollowmere." Mythic: "A name all Hollowmere tells its children."
* **Chronicle Entries** is the number of Chronicle entries that name the character (as participant or primary actor): `QueryEntriesFor`. **Latest Deeds** are the headlines of the newest three of them. **Notable Deeds** are those of the entries that are featured (`featured` = 1) or a server first (`first` = 1). This is the player's own score, so entries not shown on the public website may appear. `finger` and `look` are not changed.
* **Wizards** make no history: the Chronicle Standing section is the single line "Wizards make no history in the Chronicle. Wizard level N." and there is no rank sentence or title line.

## Characters

The box and the bullet are UTF-8, as is everything the driver sends; there is no ASCII switch in the game, so a client that cannot show UTF-8 shows the box characters wrongly.
