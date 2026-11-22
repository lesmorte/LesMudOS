/// <summary>Event types for quests: a generic local quest and story quest, and the two quests of Vincent's Hollow.</summary>
public class EventsQuest : MudObject
{
    public List<string> Definitions() => new()
    {
        @"id=quest.local
title=Local quest completed
kind=Quest
chron=Guilds
category=10
impact=Settlement
first=yes
renown=1
headline={name}
text=On {date}, {participants} completed the task {name} for the folk of {area}.",
        @"id=quest.clear_the_cellar
title=Clear the Cellar
kind=Quest
chron=Guilds
sub=Quest
category=10
impact=Settlement
first=yes
renown=1
headline=The Clearing of the Cellars
text=On {date}, {participants} answered the call of the Adventurers Guild and cleared the cellars beneath the Hollowman's Rest, a feat of Resolve.
significance=The first contract the Guild posted for the cellars was done, and the road north was opened to the one who did it.
rumor=They say {participants} cleared the goblins out of the cellars beneath the Hollowman's Rest.
tags=quest,guild,vincents-hollow",
        @"id=quest.story
title=Story quest completed
kind=Quest
chron=Guilds
category=10
impact=Regional
rarity=Uncommon
flags=storyline,regional
first=yes
renown=10
headline={name}
text=On {date}, {participants} brought the matter of {name} to its end, and the news travelled across {region}.",
        @"id=quest.overseers_orders
title=Recover the Overseer's Orders
kind=Quest
chron=Guilds
sub=Quest
category=10
impact=Regional
rarity=Uncommon
flags=storyline,regional
first=yes
renown=10
headline=The Orders from the Dark
text=On {date}, {participants} laid the Overseer's Orders before the Guildmaster of Vincent's Hollow, and the hand that had guided the goblins was seen at last.
significance=The Guild learned that the goblins of the cellars took their orders from beyond the village, and all of Greenvale had cause to listen.
rumor=They say {participants} carried the Overseer's Orders to the Guildmaster, and the Guild is not at ease.
tags=quest,story,guild,vincents-hollow",
    };
}
