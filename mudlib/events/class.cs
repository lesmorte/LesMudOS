/// <summary>Event types for classes. The subject is the class, so the first of each is remembered.</summary>
public class EventsClass : MudObject
{
    public List<string> Definitions() => new()
    {
        @"id=class.advanced
title=First {class}
kind=Milestone
chron=Renown
sub=Advanced Class
category=10
rarity=ServerFirst
repeat_rarity=Common
first=yes
milestone=yes
listed=firsts
renown=10
renown_first=100
headline=The First {class}
text=On {date}, {participants} chose the path of the {class} and was the first in Hollowmere to walk it.",
        @"id=class.prestige
title=First prestige {class}
kind=Milestone
chron=Renown
sub=Prestige Class
category=10
rarity=ServerFirst
repeat_rarity=Common
first=yes
milestone=yes
listed=firsts
renown=25
renown_first=250
headline=The First {class}
text=On {date}, {participants} attained the rank of {class}, the first in Hollowmere to do so.",
    };
}
