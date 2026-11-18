# Oakenfell: build map

The file names, exits and ownership for the first build of Oakenfell. It turns [oakenfell.md](oakenfell.md) (the plan) into exact paths so that several builders can work at once and their rooms connect. The city's character and history come from [../specs/oakenfell-city-design.md](../specs/oakenfell-city-design.md).

All room paths below are under `/domains/greenvale/oakenfell/` unless they start with `/`. A room's file is its path plus `.cs`.

## Rules for every builder

- Add new files only, and only in the directories you own. Do not edit any existing file.
- People go in `<your district>/npc/`, things in `<your district>/obj/`. Rooms go directly in the district directory.
- Class names must be unique across the whole mudlib: start every class name with your prefix.
- Exits use exactly the paths in the tables, including for rooms another builder is writing.
- Every exit has its return exit. The tables give both ends.
- Exit names are single words. Compass exits are abbreviated by their first letter automatically; named exits (`cityhall`, `assembly`, `out`, `up`, `down`) are typed in full.
- The city is not safe ground: do not set `Safe`, except in the inn's guest rooms.
- Citizens, shopkeepers and the watch are peaceful NPCs. No creature in the city is aggressive.
- Follow docs/BUILDING.md: the templates, the colour tag rule (`{cmd}` only around what a player can literally type, never in Name, Ids or ShortDesc), no "> " in any text, data in `Create()` only.

## Left for the integration step (do not build these)

These need systems that are still being built. Describe the place and its people, and have the people say honestly that the business is not open yet where that applies. Do not invent commands for them.

- Weapon and armor stock for the weaponsmith and armorer (equipment categories are changing). The stalls and their keepers exist; they sell nothing yet.
- The bank's `deposit`, `withdraw`, `balance` and the rentable vault. The banker exists and explains them as coming services.
- Choosing an advanced class in the Hall of Callings. The five hallmasters exist, with `talk` lines and an `ask ... about <class>` topic for each of their three classes.
- The Grand Chronicle, the Halls and the Renown records in the Chronicle Gallery. Describe the lectern and the carved walls as details; the chronicler exists.
- The hidden way from Elder Lane into the grove. Write Elder Lane with its ivy-grown wall as details and no exit west; write the Overgrown Way with its exit east to Elder Lane.
- Faction standing. Envoys and faction contacts explain their factions through `talk` and `ask`.
- The respawn point and the area and region names.

## Rooms and exits

### Vale Road (owner: builder 1, prefix `Vr`) — directory `/domains/greenvale/vale_road/`

Two rooms remain here. The Long Meadows, the River Bend and the Rise (and the Riverwatch ranger of the River Bend) were retired when the Greenvale Transit Corridor replaced them; the road between them is now the corridor's five segments (see [transit-corridor-build.md](transit-corridor-build.md)). The corridor's River Bend, the Rise and the Carters' Mile are `/domains/greenvale/corridor/approach/m07`, `m10` and `m11`.

| Path | Short name | Exits |
|---|---|---|
| `/domains/greenvale/vale_road/road` | The Vale Road | south `/domains/hollow/village/north_road`; north `/domains/greenvale/corridor/south_road/m01` |
| `/domains/greenvale/vale_road/approach` | The Southgate Approach | south `/domains/greenvale/corridor/approach/m11`; north `southgate/gate` |

### Southgate District (owner: builder 1, prefix `Sg`) — directory `southgate/`

| Path | Short name | Exits |
|---|---|---|
| `southgate/gate` | Southgate | south `/domains/greenvale/vale_road/approach`; north `southgate/wayfarers_row` |
| `southgate/wayfarers_row` | Wayfarer's Row | south `southgate/gate`; north `southgate/stag_lane`; east `southgate/stables`; west `southgate/free_lodge` |
| `southgate/stag_lane` | Stag Lane | south `southgate/wayfarers_row`; east `southgate/silver_stag`; west `southgate/outfitter`; north `market/south_rows` |
| `southgate/silver_stag` | The Silver Stag Inn | west `southgate/stag_lane`; up `southgate/silver_stag_rooms` |
| `southgate/silver_stag_rooms` | The Silver Stag Inn, Guest Rooms | down `southgate/silver_stag` |
| `southgate/stables` | Southgate Stables | west `southgate/wayfarers_row` |
| `southgate/free_lodge` | The Free Lodge | east `southgate/wayfarers_row` |
| `southgate/outfitter` | The Outfitter | east `southgate/stag_lane` |

The Silver Stag's innkeeper sells food, drink and a bed (the bed heals and moves the buyer upstairs, as Harland does in `/domains/hollow/npc/harland.cs`). The guest rooms are `Safe` and a logout room. The outfitter sells potions and rations (existing items in `/domains/hollow/obj/`). A street food seller on Wayfarer's Row sells food.

### Market District (owner: builder 1, prefix `Mk`) — directory `market/`

| Path | Short name | Exits |
|---|---|---|
| `market/south_rows` | Greenvale Market, South Rows | south `southgate/stag_lane`; north `market/north_rows`; west `market/coin_street` |
| `market/north_rows` | Greenvale Market, North Rows | south `market/south_rows`; north `founders/square`; east `river/mere_bridge` |
| `market/coin_street` | Coin Street | east `market/south_rows`; north `market/bank`; south `market/consortium_house`; west `market/coin_street_west` |
| `market/coin_street_west` | Coin Street, West End | east `market/coin_street`; north `market/auction_house`; south `market/caravan_office`; west `market/westgate` |
| `market/bank` | The Oakenfell Bank | south `market/coin_street` |
| `market/consortium_house` | Consortium House | north `market/coin_street` |
| `market/auction_house` | The Auction House | south `market/coin_street_west` |
| `market/caravan_office` | The Caravan Office | north `market/coin_street_west` |
| `market/westgate` | Westgate | east `market/coin_street_west`; west `beyond/westwood_road` |

### Founder's District (owner: builder 2, prefix `Fd`) — directory `founders/`

| Path | Short name | Exits |
|---|---|---|
| `founders/square` | Founder's Square | south `market/north_rows`; north `founders/old_stone_street`; east `river/old_river_street`; west `guild/guild_row`; cityhall `founders/city_hall`; assembly `founders/assembly_hall` |
| `founders/city_hall` | City Hall | out `founders/square` |
| `founders/assembly_hall` | Assembly Hall, the Public Gallery | out `founders/square` |
| `founders/old_stone_street` | Old Stone Street | south `founders/square`; west `founders/elder_lane`; north `founders/northgate` |
| `founders/elder_lane` | Elder Lane | east `founders/old_stone_street` |
| `founders/northgate` | Northgate | south `founders/old_stone_street`; north `beyond/northern_road` |

### The Hidden Grove (owner: builder 2, prefix `Gv`) — directory `grove/`

| Path | Short name | Exits |
|---|---|---|
| `grove/overgrown_way` | The Overgrown Way | east `founders/elder_lane`; west `grove/hidden_grove` |
| `grove/hidden_grove` | The Hidden Grove | east `grove/overgrown_way`; north `grove/great_stump` |
| `grove/great_stump` | The Great Stump | south `grove/hidden_grove` |

### River District (owner: builder 2, prefix `Rv`) — directory `river/`

| Path | Short name | Exits |
|---|---|---|
| `river/old_river_street` | Old River Street | west `founders/square`; east `river/quay`; north `river/riverwatch_office` |
| `river/riverwatch_office` | The Riverwatch Office | south `river/old_river_street` |
| `river/mere_bridge` | Mere Bridge | west `market/north_rows`; east `river/eastgate`; north `river/quay` |
| `river/quay` | The Mere Quay | west `river/old_river_street`; south `river/mere_bridge`; east `river/trade_office`; north `river/quay_north` |
| `river/quay_north` | The Mere Quay, North Landing | south `river/quay`; west `river/fish_market`; east `river/shipwright`; north `river/rivergate` |
| `river/trade_office` | The River Trade Office | west `river/quay` |
| `river/fish_market` | The Fish Market | east `river/quay_north` |
| `river/shipwright` | The Shipwright's Yard | west `river/quay_north` |
| `river/rivergate` | Rivergate | south `river/quay_north`; north `beyond/river_road` |
| `river/eastgate` | Eastgate | west `river/mere_bridge`; east `beyond/crossing_road` |

### Beyond the gates (owner: builder 2, prefix `Bg`) — directory `beyond/`

One room each. The onward direction is blocked with `BlockExit` and a line that names where the road goes and says it is not yet open to travelers.

| Path | Short name | Exits | Onward (blocked) |
|---|---|---|---|
| `beyond/westwood_road` | The Westwood Road | east `market/westgate` | west, toward the Elderwood |
| `beyond/northern_road` | The Northern Road | south `founders/northgate` | north, toward the mountains |
| `beyond/crossing_road` | The Crossing Road | west `river/eastgate` | east, toward Watchfort Crossing |
| `beyond/river_road` | The River Road | south `river/rivergate` | north, downriver |

### Guild Quarter (owner: builder 3, prefix `Gq`) — directory `guild/`

| Path | Short name | Exits |
|---|---|---|
| `guild/guild_row` | Guild Row | east `founders/square`; north `guild/hall_doors`; south `guild/athenaeum`; west `guild/crafters_court` |
| `guild/hall_doors` | The Hall of Legends, the Great Doors | south `guild/guild_row`; north `guild/hall_of_callings` |
| `guild/hall_of_callings` | The Hall of Legends, the Hall of Callings | south `guild/hall_doors`; east `guild/chronicle_gallery`; west `guild/envoys_gallery` |
| `guild/chronicle_gallery` | The Hall of Legends, the Chronicle Gallery | west `guild/hall_of_callings` |
| `guild/envoys_gallery` | The Hall of Legends, the Envoys' Gallery | east `guild/hall_of_callings` |
| `guild/athenaeum` | The Athenaeum | north `guild/guild_row` |
| `guild/crafters_court` | Crafters' Court | east `guild/guild_row`; south `guild/proving_yard` |
| `guild/proving_yard` | The Proving Yard | north `guild/crafters_court` |
