/// <summary>Event types for discoveries. The subject is the area, hidden location or dungeon, so the first finder of each one is remembered.</summary>
public class EventsDiscover : MudObject
{
    public List<string> Definitions() => new()
    {
        @"id=discover.area
title={name}
kind=Discovery
chron=Exploration
category=5
first=yes
listed=records
discovery=yes
renown=1
renown_first=25
text=On {date}, {participants} was the first to set foot in {name}.",
        @"id=discover.hidden
title=Hidden location discovered: {name}
kind=Discovery
chron=Discovery
category=25
rarity=Uncommon
first=yes
discovery=yes
renown=5
renown_first=25
headline={name} Found
text=On {date}, {participants} found {name}, hidden in {area}.
rumor=They say {participants} has found {name}, tucked away where no one had looked.",
        @"id=discover.secret_dungeon
title=Secret dungeon discovered: {name}
kind=Discovery
chron=Discovery
category=100
impact=Settlement
rarity=Rare
first=yes
discovery=yes
renown=5
renown_first=25
headline=The Discovery of {name}
text=On {date}, {participants} discovered {name} beneath {area}, revealing knowledge thought lost to time.
rumor=They say {participants} found a way down into {name}.",
        @"id=discover.great_stump
title=The Great Stump
kind=Discovery
chron=Discovery
category=25
impact=Settlement
rarity=VeryRare
first=yes
listed=firsts
discovery=yes
renown=5
renown_first=25
headline=The Great Stump Found
text=On {date}, {participants} stood before the Great Stump in the Hidden Grove, the ancient root from which Oakenfell takes its name.
significance=The city's namesake, long a rumor, was seen by living eyes for the first time.
rumor=They say {participants} found the Great Stump, the very thing Oakenfell is named for.
tags=oakenfell,hidden-grove,great-stump",
    };
}
