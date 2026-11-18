/// <summary>
/// Event types for crafting discoveries. The crafting daemon (/secure/crafting) reports one when a character finds a Rare or
/// Secret recipe by experiment (combine); the subject is the recipe id, so the first finder of each recipe is remembered.
/// Wizards make no history: the event service ignores them.
/// </summary>
public class EventsCraft : MudObject
{
    public List<string> Definitions() => new()
    {
        @"id=craft.discover_rare
title=Rare recipe discovered: {name}
kind=Discovery
chron=Crafting
category=25
rarity=Uncommon
first=yes
discovery=yes
renown=5
renown_first=25
headline={name} Rediscovered
text=On {date}, {participants} worked out how to make {name}, a recipe thought forgotten.
rumor=They say {participants} has found how to make {name}.",
        @"id=craft.discover_secret
title=Secret recipe discovered: {name}
kind=Discovery
chron=Crafting
category=100
impact=Settlement
rarity=Rare
first=yes
discovery=yes
renown=10
renown_first=50
headline=The Secret of {name}
text=On {date}, {participants} discovered by their own hand how to make {name}, a secret no trainer teaches.
rumor=They say {participants} has worked out the secret of {name}.",
    };
}
