/// <summary>Event types for travel.</summary>
public class EventsTravel : MudObject
{
    public List<string> Definitions() => new()
    {
        @"id=travel.pass
title=Left the village with a travel pass
kind=Milestone
chron=Exploration
category=10
first=yes
renown=1
headline=The Road Is Opened
text=On {date}, {participants} walked out of {area} on the strength of a travel pass.",
    };
}
