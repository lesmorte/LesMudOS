/// <summary>Event types for reaching a Renown tier (Recognized and above, as the bible lists them). The Renown service raises them itself; they award no Renown.</summary>
public class EventsRenown : MudObject
{
    public List<string> Definitions() => new()
    {
        @"id=renown.recognized
title=Reached Recognized
kind=Milestone
chron=Renown
category=10
first=yes
headline={participants} Is Recognized
text=On {date}, {participants} became Recognized throughout Greenvale.",
        @"id=renown.acclaimed
title=Reached Acclaimed
kind=Milestone
chron=Renown
category=25
first=yes
headline={participants} Is Acclaimed
text=On {date}, {participants} became Acclaimed across the region.",
        @"id=renown.renowned
title=Reached Renowned
kind=Milestone
chron=Renown
category=100
first=yes
headline={participants} Is Renowned
text=On {date}, {participants} became Renowned, a name known across many regions.",
        @"id=renown.legendary
title=Reached Legendary
kind=Milestone
chron=Renown
category=500
first=yes
headline={participants} Is a Living Legend
text=On {date}, {participants} became Legendary, a part of modern history.",
        @"id=renown.mythic
title=First Mythic Renown
kind=Milestone
chron=Renown
category=2500
rarity=ServerFirst
repeat_rarity=Common
first=yes
milestone=yes
listed=firsts
headline={participants} Becomes Living Folklore
text=On {date}, {participants} reached Mythic renown, living folklore in the eyes of all Hollowmere.",
    };
}
