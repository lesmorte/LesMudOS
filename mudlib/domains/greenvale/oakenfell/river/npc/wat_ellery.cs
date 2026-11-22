// @inherit /std/npc

public class RvWatEllery : Npc
{
    public override void Create()
    {
        Name = "Wat Ellery";
        Ids.Add("wat");
        Ids.Add("ellery");
        Ids.Add("angler");
        Ids.Add("fisherman");
        ShortDesc = "Wat Ellery, an angler";
        LongDesc = "A gaunt man with a long rod propped on the parapet and a wide straw hat tipped over his eyes. A wicker basket sits beside him, containing a very small perch and a sandwich.";
        Level = 1;
        MaxVitality = 15;

        AddChat("Sh. You will frighten them.");
        AddChat("Thirty years I have fished off this bridge. The river and I have an understanding. It does not give me fish, and I do not leave.");
        AddTopic("fish,fishing,bite,bait,perch", "Perch, roach, now and then a pike. The big ones lie under the third arch, and none of them have ever liked me.");
        AddTopic("river,mere,river mere,water", "The Mere is wide and slow and kind to its friends. It comes down from the south and runs out under the north wall. I have stood on this bridge and watched it for most of my life, and I would say I have learned about half of it.");
        AddTopic("bridge,arches,stone", "Five arches, built long before my time. The bridge is older than the river, say some. That is nonsense, but I have never found anyone who could say why.");
        AddEmote("Wat Ellery reels in a line, finds the bait gone, and baits the hook again without a word.", 6);
        AddEmote("Wat Ellery lifts his hat, studies the water for a long time, and puts it back.", 5);
    }
}
