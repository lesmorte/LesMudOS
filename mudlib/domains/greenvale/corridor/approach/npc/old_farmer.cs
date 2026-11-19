// @inherit /std/npc

public class OaOldFarmer : Npc
{
    public override void Create()
    {
        Name = "Florian Thatch";
        Ids.Add("florian");
        Ids.Add("thatch");
        Ids.Add("florian thatch");
        Ids.Add("old farmer");
        Ids.Add("farmer");
        Ids.Add("old man");
        ShortDesc = "Florian Thatch, an old farmer";
        LongDesc = "A very old man seated on a bench by the well, with a stick across his knees, a hat full of holes and hands like tree-roots. He has the contented look of someone who has done his share of the work and is now supervising.";
        Level = 1;
        MaxHp = 20;

        AddChat("Sit yourself down. Everyone goes by in a hurry, and no one sits.");
        AddChat("I've sat on this bench for sixty years. The road has gone by me the whole time.");
        AddChat("Wait until you see the city. Wait until you see it. That's all I say.");
        AddTopic("aqueduct,arches,old arches,water", "My grandmother used to say the water went somewhere else, before the city. That it ran to a place that isn't there now. The arches go from nowhere to nowhere, and she said that was the point.");
        AddTopic("oakenfell,city,rise,walls", "I walked up the Rise at fifteen and I've never been so frightened in my life, nor so glad. You'll see.");
        AddTopic("southfarm,hamlet,farm,farms,fields", "We've farmed here as long as there's been a Southfarm. The soil is good, the neighbours are decent, and the city eats everything we grow.");
        AddTopic("sheaf,last sheaf,custom,tradition,field", "The last sheaf stays on the post. That's what we do. Why? Because my father did, and his before him.");
        AddTopic("goblins,goblin,den,veterans,bandits,hedge,west", "Past the last hedge, the land's not ours. We leave it alone, and it leaves us alone, mostly.");
        AddTopic("children,child,kids", "Noisy. They'll be noisier still when they're grown. That's how you know they're well.");
        AddEmote("Florian Thatch dozes, wakes, and pretends he was thinking.", 8);
        AddEmote("The old farmer taps his stick on the ground in time with a tune only he hears.", 6);
    }
}
