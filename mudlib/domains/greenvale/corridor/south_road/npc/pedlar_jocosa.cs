// @inherit /std/npc

public class SrPedlarJocosa : Npc
{
    public override void Create()
    {
        Name = "Jocosa";
        Ids.Add("jocosa");
        Ids.Add("pedlar");
        Ids.Add("peddler");
        Ids.Add("woman");
        Ids.Add("trader");
        ShortDesc = "Jocosa, a pedlar";
        LongDesc = "A small, sharp-eyed woman in a bright shawl, with a pack bigger than herself leaning against the wall beside her. Needles, ribbons and tin whistles hang from straps across her chest, and she appears to be counting you.";
        Level = 3;
        MaxHp = 40;

        AddChat("Ribbons, needles, thread and gossip, and the gossip's the one I'll give away.");
        AddChat("Going north? You'll want a good cloak, good boots and better luck. I can sell you two of those.");
        AddChat("The Merchant Camp, deep in the wood, will pay twice for needles. I've done the sums.");
        AddTopic("north,wood,forest,eldergreen,road", "North of the bridge the road goes quiet. By the second day it is dark as a cellar at noon. There are camps where it widens, and the folk are decent if you share your fire.");
        AddTopic("camp,merchant camp,merchants,caravan", "Past the outpost, there's a camp where the caravans halt at night. They set guards and sell food and potions. It's the one place in the deep wood I'd call cheerful.");
        AddTopic("shrine,old shrine,pilgrims", "There's a wayside shrine, old and tended. Pilgrims leave candle-ends. I leave a ribbon. You can't be too careful.");
        AddTopic("crossroads,stone,crossroads stone,track", "Further on, a standing stone where an overgrown track leaves the road. Don't take it. I took it once. I got out.");
        AddTopic("oakenfell,city,toll", "Oakenfell has walls you can see from miles away. There's a toll house and a hamlet below it, and a good market. I sell best at the market.");
        AddTopic("ribbons,wares,goods,pack", "Ribbons, pins and a very fine tin whistle. I'm not selling today, I'm resting. I'm just looking at your purse the way a friend does.");
        AddEmote("Jocosa untangles a skein of ribbon with deft fingers.", 8);
        AddEmote("Jocosa tests a tin whistle and winces.", 6);
    }
}
