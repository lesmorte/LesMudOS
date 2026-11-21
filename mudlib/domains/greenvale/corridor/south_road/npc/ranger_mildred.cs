// @inherit /std/npc

public class SrRangerMildred : Npc
{
    public override void Create()
    {
        Name = "Ranger Mildred";
        Ids.Add("mildred");
        Ids.Add("ranger");
        Ids.Add("riverwatch ranger");
        Ids.Add("riverwatch");
        Ids.Add("woman");
        Ids.Add("ranger mildred");
        ShortDesc = "Mildred, a Riverwatch ranger";
        LongDesc = "A tall, weathered woman in a green-grey cloak, a longbow slung over her shoulder and a quiver at her hip. A silver badge in the shape of an eye is pinned at her collar, and she watches the far bank the way another person watches a fire.";
        Level = 6;
        MaxVitality = 60;

        AddChat("Ever Vigilant. That's the word. Mostly it means standing here and watching the trees.");
        AddChat("The bridge is the last easy ground. Beyond, the wood starts. Mind how you go.");
        AddChat("If you see a wolf north of the bridge, leave it be. If it sees you first, don't run.");
        AddTopic("riverwatch,rangers,ranger,eye", "The Riverwatch keeps the roads. Our headquarters is at Watchfort Crossing, but you'll find our posts all the way to Oakenfell. The next is the Riverwatch Outpost, in the Edge, a half day north.");
        AddTopic("road,patrol,patrols,patrolling", "I walk the road from the Rest to the far bank, twice a day. Further north the patrols range wider. Wolves have been bold this summer, but I've not lost a traveler yet.");
        AddTopic("wolf,wolves,stray,fox", "Strays this year, thin and hungry. The big packs are deeper in the wood. Keep to the road, and keep a light at night.");
        AddTopic("bandit,bandits,lookout,sheepfold", "There's been a watcher on the old sheepfold ridge for a fortnight. I've been up and he's been gone. I don't like it.");
        AddTopic("thorn,thorn pact,hawthorn,ring,markings", "There are hawthorn rings and rag-knots in some of the pastures. Old custom, the farmers say, or something else. I don't ask. I keep the road, and I leave the rest where it lies.");
        AddTopic("outpost,wood,eldergreen,north,forest", "Beyond the bridge the road narrows and the trees begin. By the second day it's real forest. You'll see sunlight for a while yet. Count it, and keep going.");
        AddEmote("Mildred scans the far bank and the treeline.", 8);
        AddEmote("The ranger runs a thumb along her bowstring and nods to herself.", 6);
    }
}
