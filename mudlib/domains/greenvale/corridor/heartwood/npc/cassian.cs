// @inherit /std/npc

public class HwCassian : Npc
{
    public override void Create()
    {
        Name = "Cassian";
        Ids.Add("cassian");
        Ids.Add("hunter");
        Ids.Add("ranger");
        Ids.Add("riverwatch hunter");
        Ids.Add("riverwatch");
        ShortDesc = "Cassian, a Riverwatch hunter";
        LongDesc = "A weathered man of fifty in a hooded cloak the colour of bark, with a short bow across his knees and a silver eye pinned inside his collar where it will not catch the light. He has the stillness of someone who has spent a great many hours in one place, and his eyes go to the trees whenever they leave you.";
        Level = 7;
        MaxVitality = 70;

        AddChat("Quiet shift. I'm not complaining. I've learned what the other kind sounds like.");
        AddChat("Ever vigilant. In the Heartwood it means keeping your back to the cairn and your eyes on the trees.");
        AddChat("If you hear the woods go silent all at once, don't go looking. Come back here.");
        AddTopic("memorial,cairn,stone,slate,names", "We put it up for the ones who didn't come back. Edric was my first partner on this stretch. Every ranger who passes adds a stone. I add one when I come on shift and one when I go off.");
        AddTopic("riverwatch,rangers,ranger,station", "One hunter to a station, and a patrol along the road when there are enough of us. Headquarters is at Watchfort Crossing, far off. I answer to the commander at the Outpost, when I answer to anyone.");
        AddTopic("bear,bears,goblin,goblins,road", "Bears on the road in the dusk. Goblins in twos and threes, and they come by day if they're hungry. Stay on the road and keep your wits. It's the trails that cost people.");
        AddTopic("trail,east,run,stalker,stalkers,cat", "The east trail is the Stalker's Run. I blazed it myself, so that people would know which trail to leave alone. Something walks it. I've seen the scratches, never the thing, and I mean to keep it that way.");
        AddTopic("ruins,mossgate,arch,old,builders", "Older than the Riverwatch, older than Oakenfell, older than the Assembly's books. I don't ask who. There's a scholar down the road who asks, and she doesn't enjoy the answers.");
        AddTopic("stones,ritual,ritual stones,pact,thorn pact,thorn,thread", "Someone keeps the stones north of the spring. The thread changes, the ground is swept. I've never seen who. I've never wanted to. I leave the thread as I find it.");
        AddTopic("spring,deer,wolf,wolves", "The animals don't fear the spring, and I don't hunt near it. As for the wolf, if you see tracks as wide as your hand, you've seen enough.");
        AddTopic("resolve,remembered,vigil,watch,edric,dusk watch", "Edric was remembered by the whole station for his Resolve. He kept the dusk watch here for eleven years and never once left the cairn early. The last time he went out, we kept the watch for him, and we are still keeping it.");
        AddEmote("Cassian turns an arrow over in his fingers, sighting down the shaft at the trees.", 8);
        AddEmote("Cassian sets a small stone on the cairn and rests his hand on it a moment.", 4);
        AddEmote("Cassian stills and listens, then relaxes a little.", 8);
    }
}
