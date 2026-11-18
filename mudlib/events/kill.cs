/// <summary>Event types for creature kills. Ordinary kills score nothing and are only counted toward aggregates.</summary>
public class EventsKill : MudObject
{
    public List<string> Definitions() => new()
    {
        @"id=kill.ordinary
title=Ordinary creature killed
kind=Victory
chron=Combat
category=0
first=no
once=no
repeat=same
renown=0
text=On {date}, {participants} defeated {name} in {area}.",
    };
}
