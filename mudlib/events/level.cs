/// <summary>Event types for levels. Milestone levels (10, 25, 50, 75, 100) are server firsts; the subject is the level.</summary>
public class EventsLevel : MudObject
{
    public List<string> Definitions() => new()
    {
        @"id=level.milestone
title=First to reach level {level}
kind=Milestone
chron=Renown
sub=Level
category=100
rarity=ServerFirst
repeat_rarity=Rare
first=yes
milestone=yes
listed=firsts
renown=10
renown_first=100
headline=The First to Reach Level {level}
text=On {date}, {participants} became the first to reach level {level} in all of Hollowmere.
significance=Others will follow the road they walked, but no one will walk it first again.",
    };
}
