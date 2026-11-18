// @inherit /std/npc

public class SrShepherdBram : Npc
{
    public override void Create()
    {
        Name = "Bram";
        Ids.Add("bram");
        Ids.Add("shepherd");
        Ids.Add("man");
        Ids.Add("old shepherd");
        ShortDesc = "Bram, a shepherd";
        LongDesc = "A lean, weathered man leaning on a crook, his coat patched with sheepskin and his boots mended with twine. A black-and-white dog sits at his heel and watches the sheep with an air of long-suffering responsibility.";
        Level = 2;
        MaxHp = 30;

        AddChat("Easy there, don't spook the ewes. They're thick, but they're mine.");
        AddChat("Fine grazing, the walk. It has always been grazing, since before the road.");
        AddChat("Don't mind the dog. She thinks you're a sheep until you prove otherwise.");
        AddTopic("sheep,flock,ewes,lambs,wool", "Two hundred ewes, a ram called Alderman, and a lamb I lost last week. A grey wolf took it at dusk off the west slope.");
        AddTopic("wolf,wolves,fox,lamb", "A stray, thin and bold, down from the wood. I've seen its tracks by the Fox Covert, west of the mill. I'll not send the dog after it. It would not come back.");
        AddTopic("dog,sheepdog,collie", "Bess. Eleven years old and still better at the job than I am.");
        AddTopic("road,north,bridge,wood,forest", "I've been to the bridge twice in my life and to the wood once. It was dark and the trees watched. I'm a hill man; give me an open sky.");
        AddTopic("sheepfold,old fold,ridge,west", "The old fold on the ridge, past the hay meadow? My grandfather's flock lived there. We left it a long time ago. Something about the place; the dog won't go in, and a dog knows.");
        AddTopic("thistle,thistles", "Sheep won't touch them. I pull the worst and leave the rest. Stalks make a fair broom if you've the patience.");
        AddEmote("Bram whistles twice and the dog circles the flock.", 8);
        AddEmote("Bram leans on his crook and studies the sky.", 6);
    }
}
