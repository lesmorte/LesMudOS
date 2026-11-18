/// <summary>Aggregate events: they happen when a counter of routine events crosses a multiple of its step, and no one in particular caused them.</summary>
public class EventsAggregate : MudObject
{
    public List<string> Definitions() => new()
    {
        @"id=aggregate.goblin_purge
title=The Goblin Purge of {region}
kind=World
chron=World Events
sub=Aggregate
category=100
impact=Regional
rarity=Rare
flags=regional
first=yes
once=no
repeat=same
aggregate_of=kill.ordinary
aggregate_subject=goblin
aggregate_step=1000
headline=The Goblin Purge of {region}
text=By {date}, {count} goblins had been put down across {region}, and the roads were safer for it.
significance=No single hand did it. Every adventurer who ever drew a blade against the goblins of {region} had a share.
rumor=They say that {count} goblins have fallen in {region} now, and the roads are the safer for it.",
    };
}
