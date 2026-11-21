// @inherit /std/npc

public class SrDroverIves : Npc
{
    public override void Create()
    {
        Name = "Ives";
        Ids.Add("ives");
        Ids.Add("drover");
        Ids.Add("man");
        Ids.Add("cattle man");
        ShortDesc = "Ives, a drover";
        LongDesc = "A broad, sun-browned man with a drover's long coat, a stick as tall as himself, and the habit of looking at every animal in the yard and estimating its weight. He sits on the bench under the ash with a bowl of stew and a mug beside him.";
        Level = 3;
        MaxVitality = 40;

        AddChat("Sit, sit. I've been on the road a week and I'd talk to a post if it nodded.");
        AddChat("Cattle to the Hollow, empty home. The easy way round. It's all the rest I'd not do again.");
        AddChat("Another bowl of Aldith's stew and I might forgive the road entirely.");
        AddTopic("road,north,oakenfell,journey", "From Oakenfell it's two days, a day and a half if the wagon is empty. Bridge, then trees, then more trees. The road's good, but narrow. Keep to it.");
        AddTopic("bridge,stone bridge,river", "The Stone Bridge is the last easy place. After it, the Rangers' outpost is a half day's walk into the wood, and I always sleep better once I've seen their fire.");
        AddTopic("wolves,wolf,bandits,danger", "Wolves more than men, for now. I lost a steer to one three days north. Beyond the outpost they're bigger. And goblins, they say, though I never saw one.");
        AddTopic("outpost,riverwatch,rangers,ranger", "The Riverwatch outpost has a stockade, a commander and a good bench by the fire. They'll sell you nothing but advice, which is generally free and generally good.");
        AddTopic("forester,cabin,hunter,hunters,camp", "There's a forester's cabin where the trees begin. Hunters camp further on. Plenty of folk in the wood, if you know where to look.");
        AddTopic("cattle,drove,beasts", "Forty head to the Hollow, and none lost. Forty. I have never been prouder of anything in my life.");
        AddEmote("Ives tears a heel of bread and mops his bowl.", 8);
        AddEmote("Ives stretches his shoulders with a creak and a sigh.", 6);
    }
}
