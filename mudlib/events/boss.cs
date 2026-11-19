/// <summary>Event types for creatures of rank: a dungeon boss in general, the Goblin Overseer, and the first kill of a world boss.</summary>
public class EventsBoss : MudObject
{
    public List<string> Definitions() => new()
    {
        @"id=boss.dungeon
title=Dungeon boss killed
kind=Victory
chron=Combat
category=25
impact=Settlement
rarity=Uncommon
first=yes
renown=10
headline={name} Falls
text=On {date}, {name} was defeated by {participants}, ending a period of danger in {area}.",
        @"id=boss.goblin_overseer
title=Goblin Overseer killed
kind=Victory
chron=Combat
sub=Dungeon Boss
category=25
impact=Settlement
rarity=Uncommon
flags=storyline
first=yes
renown=10
renown_first=15
headline=The Quieting of the Cellars
text=On {date}, {participants} brought down the Goblin Overseer in the tunnels beneath Vincent's Hollow.
significance=The goblins that had plagued the Hollowman's Rest were left leaderless, and the village slept easier.
rumor=They say {participants} brought down the Goblin Overseer in the tunnels below the village.
tags=goblin,overseer,boss,vincents-hollow",
        @"id=boss.world
title=World boss killed
kind=Victory
chron=Combat
sub=World Boss
category=250
impact=Regional
rarity=ServerFirst
repeat_rarity=Common
first=yes
milestone=yes
listed=firsts
renown=50
renown_first=250
headline=The Fall of {name}
text=On {date}, {participants} brought down {name}, the first such beast ever slain, and the roads of {region} were safe again.",
    };
}
