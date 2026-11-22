// @inherit /std/patrol

/// <summary>A Riverwatch patrol of two rangers, presented as one: it walks the main road of eldergreen edge there and back.</summary>
public class CpEldergreenEdge : Patrol
{
    public override void Create()
    {
        Name = "Riverwatch Patrol";
        Ids.Add("patrol");
        Ids.Add("riverwatch patrol");
        Ids.Add("riverwatch");
        Ids.Add("rangers");
        Ids.Add("ranger");
        Ids.Add("pair");
        ShortDesc = "a Riverwatch patrol";
        LongDesc = "Two rangers walk together, close enough to speak without raising their voices, in cloaks of green and grey with the dust of the road on the hems. Each wears a badge of a silver eye at the collar, and each carries a longbow strung and a short blade worn thin at the edge. They watch the verges more than they watch you, and they have the unhurried gait of people who will walk this road again tomorrow.";
        Level = 7;
        MaxVitality = 140;
        DamageMin = 5;
        DamageMax = 8;
        DefendPlayers = true;
        ReturnSeconds = 300;
        SetRoute(new[]
        {
            "/domains/greenvale/corridor/eldergreen_edge/m05",
            "/domains/greenvale/corridor/eldergreen_edge/m06",
            "/domains/greenvale/corridor/eldergreen_edge/m07",
            "/domains/greenvale/corridor/eldergreen_edge/m08",
            "/domains/greenvale/corridor/eldergreen_edge/m09",
            "/domains/greenvale/corridor/eldergreen_edge/m10",
            "/domains/greenvale/corridor/eldergreen_edge/m11",
        }, 45);
        AddChat("Under the eaves is where it starts. Keep your eyes up as well as ahead.");
        AddChat("From the Waystone to the Green Gate and back again. The Outpost keeps the kettle on, if you are heading that way.");
        AddTopic("road,roads,way,narrows,green gate,gate,waystone,outpost", "The Edge Road narrows from here to the Green Gate, and the oaks come in over it. The Outpost stands midway and is the one place on the stretch where you can sleep without a hand on your knife. Beyond the Green Gate the Green Road begins, and that is another matter.");
        AddTopic("rangers,riverwatch,ranger,badge,eye,silver eye,ever vigilant,commander", "We are the Riverwatch, and the silver eye on the green and grey is ours. Ever Vigilant. The Outpost is our main post on the road, with a commander and a bounty board. Ask there. We walk the road between, and there are fewer of us on it than there used to be.");
        AddTopic("watch,danger,dangers,beware,spiders,spider,wolves,wolf,boar,boars,goblins,goblin,scouts", "Spiders in the fern banks, wolves in the thick of the afternoon, boar in the wallow east of the trail. And small bare feet where there should be none: goblin scouts count the traffic on this road, and they have been counting it for years. Stay on the road and they only count.");
    }
}
