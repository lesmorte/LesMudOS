# F1 item instances: working notes

Branch work/items, worktree /home/claude/wt-items. Spec: docs/PLAN-corridor-crafting.md sections 3 (Items F1) and 8.

## Done
- /std/item: quality (0-5, default Common), quantity, properties (<=16, <=256 chars); Stackable, Plural; quality table
  (QualityNames, QualityMultipliers, ValueFor) in one place; QueryValue = unit value by quality; DescribeCount/Short with
  quality prefix and counts; Id answers plurals and "<quality> <name>"; Settle (merge), Split, ReceiveStack/TakeSplit
  (same-program protocol), UseOne; QueryState/RestoreState (record text). Setters guarded: trusted trees may change
  any item, /wiz code only /wiz items (PreviousObject).
- /std/thing: builder API + command helpers: ParseAmount, Matching, MovePortion, RecordOf, RecordQuantity, CloneRecord,
  CreateIn, TakeFrom, CountIn, QuantityOf, Describe.
- adventurer: save records (path?state), restore via CloneRecord (old bare paths load as default), lost-property claim
  via records; get/drop with N/all; give command (saves giver then receiver: Player.SaveAfterTrade).
- corpse: records to lost property; Take with amounts; Hand via MovePortion; spill via MovePortion.
- shopkeeper: buy N of stackables (OnBuyStack), sell N/all at half quality value per unit, value per unit, stock quality.
- consumable: uses one of a stack. weapon/armor appraisal uses QueryValue.

- tests/item_test.py: 125 checks, passing (port 4913).
- docs: GAME_SYSTEMS.md section 7 (quality, stacks, record), 8 (shops), 9 (save, give), corpses; BUILDING.md
  "Quality and stacks" with a compiled template (/domains/example/pelt) and the helper table.
- Passing after the main change: smoke, area, class, ansi, timing, chronicle, load_check.

- Final rerun after the last fixes: build 0/0; smoke 372, area 303, class 140, ansi 82, timing 74, chronicle 198,
  item 125 checks, all 0 failures; load_check 0 failed. Every script passed on its first run.

## Next
- Nothing open for F1. Waiting on the lead: the room.cs plural ("are here") for stacks; whether looting should save.

## Decisions
- Record format: "path" or "path?q=2&n=5&p.key=value" (%XX escapes for % & = ?). Kept in the same List<string> fields,
  so no driver change and old saves load unchanged.
- Merging is explicit (Settle) at every standard move point; raw MoveObject (room spawns, monster loot) does not merge.
  This also keeps room spawn bookkeeping (room.cs, not mine) from being fooled by merged-away handles.
- Stacks merge only if file, quality AND properties match (properties would otherwise be lost).
- Plain "get skin" takes one; "get skins"/"get all skins" with all takes all. Asking for more than there is refuses.
- "all" never includes equipped items; naming an equipped item still works as before.
- Quest items cannot be given (travel pass, orders). They can still be dropped (as before).
- Shops: buying several only for stackable stock; price per unit x count; sell price = (unit value / 2) x count.
- Default plural = short description without article + "s" (Name is often Title Case).
- Shop stock entries carry their own quality; the entry being bought is remembered during OnBuy (two entries may sell one file).
- Rooms derive from MudObject, not Thing, so they do not get the helpers (documented).
- Taking from a corpse still does not save the taker (existing area_test asserts the looted sword is lost on a crash).
- Known: room.cs lists "3 wolf skins is here." (room.cs not mine; it needs "are" when QueryQuantity > 1).
