/// <summary>The wizard channel: every wizard, level 1 and above, belongs and may speak. Membership is by rule, from the wizard level at the moment of each message; nobody joins or leaves it. Data for /secure/channels.</summary>
public class ChannelWizard : MudObject
{
    public string Definition() => @"id=wizard
title=Wizards
member_level=1
speak_level=1
can_mute=yes
can_join=no";
}
