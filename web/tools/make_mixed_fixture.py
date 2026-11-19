#!/usr/bin/env python3
"""Writes web/fixtures/mixed: a synthetic save directory in the exact shape the game writes (see fixtures/real),
holding public entries, hidden discoveries (one with the highest score), an unlisted event type, a wizard with the
highest Renown, and entries that name forbidden terms. For tests only: nothing in it is shown by the site as content."""
import json, os
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "fixtures", "mixed")
def entry(i, event, headline, text, ess, who, cls="Historical", cat="Renown", area="Greenvale", sig="", day=0, date="the 1st Day of Deepfrost, Year 1"):
    return {"ess": str(ess), "classname": cls, "category": cat, "subcategory": cat, "area": area, "region": "Greenvale",
            "participants": "|".join(who), "actor": who[0] if who else "", "factions": "", "headline": headline, "text": text,
            "significance": sig, "tags": "", "refs": "", "event": event, "subject": "", "first": "1", "featured": "1" if ess >= 500 else "0",
            "historicname": "", "id": str(i), "ts": str(1795059255 + i), "day": str(day), "date": date}
E = [
 entry(1, "level.milestone", "The First to Reach Level 10", "On the 3rd Day of Deepfrost, Year 1, Dara became the first to reach level 10 in all of Hollowmere.", 4200, ["Dara"], "Realm-Shaping", "Renown", "Vincent's Hollow", "Others will follow the road they walked, but no one will walk it first again.", 2, "the 3rd Day of Deepfrost, Year 1"),
 entry(2, "discover.secret_dungeon", "The Discovery of the Root Warrens", "On the 4th Day of Deepfrost, Year 1, Eda discovered the Root Warrens beneath Greenvale, revealing knowledge thought lost to time.", 99999, ["Eda"], "World-Defining", "Discovery", "Root Warrens", "", 3, "the 4th Day of Deepfrost, Year 1"),
 entry(3, "discover.hidden", "The Hidden Grove Found", "On the 4th Day of Deepfrost, Year 1, Eda found the Hidden Grove, hidden in Oakenfell.", 50000, ["Eda"], "Realm-Shaping", "Discovery", "Oakenfell", "", 3, "the 4th Day of Deepfrost, Year 1"),
 entry(4, "class.advanced", "The First Knight", "On the 5th Day of Deepfrost, Year 1, Dara chose the path of the Knight and was the first in Hollowmere to walk it.", 4020, ["Dara"], "Realm-Shaping", "Renown", "Oakenfell", "", 4, "the 5th Day of Deepfrost, Year 1"),
 entry(5, "quest.overseers_orders", "The Orders from the Dark", "On the 6th Day of Deepfrost, Year 1, Bryn laid the Overseer's Orders before the Guildmaster of Vincent's Hollow, and the hand that had guided the goblins was seen at last.", 1069, ["Bryn"], "Legendary", "Guilds", "The Village of Vincent's Hollow", "The Guild learned that the goblins took their orders from beyond the village.", 5, "the 6th Day of Deepfrost, Year 1"),
 entry(6, "renown.renowned", "Mira Is Renowned", "On the 7th Day of Deepfrost, Year 1, Mira became Renowned, a name known across many regions.", 300, ["Mira"], "Historical", "Renown", "Greenvale", "", 6, "the 7th Day of Deepfrost, Year 1"),
 entry(7, "travel.pass", "The Road Is Opened", "On the 8th Day of Deepfrost, Year 1, Dara walked out of The Village of Vincent's Hollow on the strength of a travel pass.", 260, ["Dara"], "Historical", "Exploration", "The Village of Vincent's Hollow", "", 7, "the 8th Day of Deepfrost, Year 1"),
 entry(8, "boss.world", "The Fall of the Gaunt Stag", "On the 9th Day of Deepfrost, Year 1, Mira and Dara brought down the Gaunt Stag, the first such beast ever slain, and the roads of Greenvale were safe again.", 2750, ["Mira", "Dara"], "Realm-Shaping", "Combat", "Greenvale", "", 8, "the 9th Day of Deepfrost, Year 1"),
 entry(9, "craft.discover_blood_ink", "Blood Ink Recipe Found", "On the 9th Day of Deepfrost, Year 1, Eda worked out the recipe for Blood Ink.", 9000, ["Eda"], "Legendary", "Crafting", "Oakenfell", "", 8, "the 9th Day of Deepfrost, Year 1"),
 entry(10, "mystery.unlisted", "A Quiet Marvel", "On the 10th Day of Deepfrost, Year 1, Mira witnessed something that has no name yet.", 8000, ["Mira"], "Legendary", "World Events", "Greenvale", "", 9, "the 10th Day of Deepfrost, Year 1"),
 entry(11, "boss.goblin_overseer", "The Quieting of the Cellars", "On the 10th Day of Deepfrost, Year 1, Bryn brought down the Goblin Overseer in the tunnels beneath The Forgotten Cellars.", 450, ["Bryn"], "Historical", "Combat", "The Forgotten Cellars", "", 9, "the 10th Day of Deepfrost, Year 1"),
 entry(12, "level.milestone", "The First to Reach Level 25", "On the 11th Day of Deepfrost, Year 1, Alice became the first to reach level 25 in all of Hollowmere.", 9000, ["Alice"], "World-Defining", "Renown", "Greenvale", "", 10, "the 11th Day of Deepfrost, Year 1"),
 entry(13, "aggregate.goblin_purge", "The Goblin Purge of Greenvale", "By the 12th Day of Deepfrost, Year 1, 1000 goblins had been put down across Greenvale, and the roads were safer for it.", 1125, [], "Legendary", "World Events", "Greenvale", "No single hand did it.", 11, "the 12th Day of Deepfrost, Year 1"),
 entry(14, "quest.local", "The Task at the Stump", "On the 12th Day of Deepfrost, Year 1, Dara stood by the Great Stump and finished a task for the folk of Oakenfell.", 700, ["Dara"], "Major Historical", "Guilds", "Oakenfell", "", 11, "the 12th Day of Deepfrost, Year 1"),
 entry(15, "level.milestone", "The Circle of Ash Rises", "On the 13th Day of Deepfrost, Year 1, Mira heard a name that should not be spoken.", 3000, ["Mira"], "Realm-Shaping", "Renown", "Greenvale", "", 12, "the 13th Day of Deepfrost, Year 1"),
 {k: v for k, v in entry(16, "", "No Event Recorded", "An old entry with no event type, and the Forgotten Barrow.", 6000, ["Dara"]).items() if k != "event"},
]
chron = {"contribution": {"dara": 9000, "mira": 6000}, "entries": E, "entryTotal": {}, "epochDay": 20776, "occurred": {}}
renown = {"activeTitle": {"mira": "Hero of Hollowmere"}, "discovered": {}, "done": {}, "infamy": {},
          "renown": {"alice": 99999, "wilder": 7000, "mira": 5200, "dara": 2600, "bryn": 900, "cade": 40, "eda": 20, "gone": 3000, "zed": 5},
          "titles": {"mira": ["Local Hero", "Wanderer", "Champion", "Defender of Greenvale", "Voice of the People", "Hero of Hollowmere"],
                     "dara": ["Local Hero", "Wanderer", "Champion", "Defender of Greenvale", "Voice of the People"], "bryn": ["Local Hero", "Wanderer", "Champion"]}}
wiz = {"levels": {"alice": 10, "wilder": 1}}
players = {"mira": ("warrior", "knight", 14), "dara": ("mage", "druid", 11), "bryn": ("warrior", "", 4), "cade": ("scout", "", 1), "eda": ("priest", "", 3),
           "zed": ("adventurer", "", 1), "alice": ("adventurer", "", 20), "wilder": ("mage", "", 9)}
def w(path, data):
    p = os.path.join(out, path); os.makedirs(os.path.dirname(p), exist_ok=True); json.dump(data, open(p, "w"), indent=2)
w("secure/chronicle.json", chron); w("secure/renown.json", renown); w("secure/wizlist.json", wiz)
for n, (a, adv, lvl) in players.items():
    w(f"players/{n}.json", {"advancedClass": adv, "archetype": a, "Level": lvl, "passwordHash": "pbkdf2-sha256$210000$FIXTURE$FIXTURE", "title": "the newcomer", "xp": 0, "quests": {}})
