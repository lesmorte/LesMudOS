/// <summary>Event types for the making of great items. Each is a milestone: the first of its kind is a server first.</summary>
public class EventsItem : MudObject
{
    public List<string> Definitions() => new()
    {
        @"id=item.legendary
title=First legendary item
kind=Crafting
chron=Crafting
category=500
rarity=ServerFirst
repeat_rarity=Common
first=yes
milestone=yes
listed=firsts
renown=100
renown_first=150
headline=The First Legendary Work
text=On {date}, {participants} forged {name}, the first legendary work in recorded history.",
        @"id=item.artifact
title=First artifact
kind=Crafting
chron=Crafting
category=1000
rarity=ServerFirst
repeat_rarity=Common
first=yes
milestone=yes
listed=firsts
renown=250
renown_first=250
headline=The First Artifact
text=On {date}, {participants} forged {name}, the first artifact of its kind in recorded history.",
    };
}
