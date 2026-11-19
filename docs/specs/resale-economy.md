# Resale economy (FUTURE FEATURE: planned, not scheduled)

Status: the owner asked for a plan on 19 November 2026 and set it aside as a future feature. Nothing here is built.

## Today
A merchant pays half an item's value and the item is destroyed. Stock is a fixed list with unlimited supply. Nothing a player sells is ever offered to another player.

## Owner's decisions
- Merchants put what they buy on a second-hand shelf and sell it on.
- At most 20 second-hand items per merchant.
- A merchant buys and resells only what that merchant deals in.
- Shelves are saved across restarts.
- Nobody buys Goblin Blood. It passes only from player to player.

## Design

### What a merchant deals in
- Each merchant gets a `DealsIn` list of categories (weapon, armor, skin, blood, herb, food, potion, tool, crafted, trinket, misc) and may add or exclude single items by path. An item's category comes from what it inherits (`/std/weapon`, `/std/skin`, ...) or, for materials, from the materials table.
- A merchant refuses anything outside the list in the merchant's own words, and `price` says so. A general store (Torren) may deal in most things; specialists stay narrow.
- This is a real change for players: today most merchants buy anything of value. The first task is a table of every merchant and what they deal in, for the owner to approve.
- A material may be marked `NoMerchant` in the materials table. Goblin Blood is the first: no merchant buys it, values it or shelves it.

### The shelf
- Selling moves the item (the real object, with its quality, charges and properties) into a store object behind the merchant instead of destroying it. Stackable goods merge into one shelf entry.
- 20 entries per merchant. When full, the oldest entry is dropped to make room (destroyed), so the shelf turns over. A stack counts as one entry, capped at a named number of units so one seller cannot bury a merchant in rat skins; units beyond the cap are bought and destroyed as today.
- `browse` lists the merchant's regular goods first, then "Second-hand:" with name, quality, count and price. `buy` works on both; when a name matches both, regular stock wins unless the player says `buy second-hand <item>` or uses a number from the list.
- Quest items stay unsellable. Items bound to a character, and anything a wizard created that is flagged not for players, are never shelved.

### Prices
- The merchant pays half of value, as today. The shelf price is full value: twice what was paid, so selling and buying back always loses money. Both factors are per-merchant numbers, which is where `haggle`, faction standing and regional prices would later attach.
- The seller is not paid again when the item is resold.

### Saving
- The shelf is saved in the item-record form characters already use (`path?q=&n=&p.x=`), by one daemon, `/secure/resale`, keyed by merchant. It reloads at start-up and when a merchant is reset. A record whose file no longer loads is dropped and logged.
- Only that daemon may write the save. A merchant's shelf can be changed only by that merchant's own sell and buy code: a wizard's object must not be able to add to, price or empty a shelf, and a wizard-made item sold to a shop must not become a way to hand goods to players (wizards' sales are destroyed as today and never shelved).

### Player to player
- `give` already moves items and gold between players and stays the only way Goblin Blood changes hands. A safe two-sided `trade` command (both sides see the offer and confirm) is a separate, later item.

## Risks to settle when this is scheduled
- **Rare items in circulation.** Anything sold becomes buyable by anyone with gold, including find-only and boss drops. Decide per item whether it may be shelved.
- **Laundering.** A high-level character can pass gear to a new one through a quiet merchant at a known price. The full-value price limits it; the level requirements on gear do the rest.
- **Duplication.** Moving real objects across save and restart is where duplication bugs live: the sale, the shelf write and the payment must succeed or fail together, and the test must kill the server between steps.
- **Shelf griefing.** Filling a specialist's 20 slots with junk to push out good items. Mitigations: the stack cap, dropping the lowest-value entry instead of the oldest, or a short per-seller limit.
- **Economy sinks.** Selling to merchants stops being pure gold creation only if players buy the goods back; watch gold per hour in the simulator before and after.

## Build outline
1. Opus: the merchant table, `DealsIn`, the shelf, saving and the transaction rules, with an adversarial test (wizard objects, crash between steps, full shelf).
2. Sonnet: per-merchant data for every shop in the Hollow, the corridor and Oakenfell; `browse` and `price` text; docs and help.
3. Opus review: exploit pass on duplication and laundering before it goes live.
