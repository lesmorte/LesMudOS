// @inherit /std/patrol

/// <summary>A Riverwatch patrol of two rangers, presented as one: it walks the main road of south road there and back.</summary>
public class CpSouthRoad : Patrol
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
        MaxHp = 140;
        DamageMin = 5;
        DamageMax = 8;
        DefendPlayers = true;
        ReturnSeconds = 300;
        SetRoute(new[]
        {
            "/domains/greenvale/corridor/south_road/m07",
            "/domains/greenvale/corridor/south_road/m08",
            "/domains/greenvale/corridor/south_road/m09",
            "/domains/greenvale/corridor/south_road/m10",
            "/domains/greenvale/corridor/south_road/m11",
        }, 45);
        AddChat("Keep to the road and the wolves keep to the hedges. That is most of the arrangement.");
        AddChat("Traveler's Rest to the Far Bank and back, all day. Dull work, and we like it dull.");
        AddTopic("road,roads,way,bridge,travel,traveler's rest,travelers rest,inn", "From the inn yard to the Stone Bridge the road is as safe as any in Greenvale: farm carts, drovers, now and then a pilgrim. We walk it there and back so that it stays that way. Past the bridge the trees begin, and the road changes its mind about you.");
        AddTopic("rangers,riverwatch,ranger,badge,eye,silver eye,ever vigilant", "Riverwatch Rangers. The silver eye on a green and grey field is ours, and the word is Ever Vigilant. Our headquarters is far off at Watchfort Crossing; the nearest roof of ours is the Outpost in the Eldergreen, north of the Stone Bridge. We would be more of us if we could.");
        AddTopic("watch,danger,dangers,beware,wolves,wolf,foxes,bandits,lookouts,brambles", "Stray wolves come out of the coverts west of the road, mostly at dusk, and the brambles hold worse than blackberries. Leave the hollows alone unless you are ready to bleed for it. And if you see someone on the high ground who is not a shepherd and not watching sheep, tell the next Ranger you meet.");
    }
}
