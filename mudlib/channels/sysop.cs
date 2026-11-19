/// <summary>The sysop channel: level 10 wizards only, by rule from the wizard level at the moment of each message. Members cannot switch it off, join or leave. Data for /secure/channels.</summary>
public class ChannelSysop : MudObject
{
    public string Definition() => @"id=sysop
title=System operators
member_level=10
speak_level=10
can_mute=no
can_join=no";
}
