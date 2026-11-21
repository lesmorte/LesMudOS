// @inherit /std/patrol

/// <summary>A Riverwatch patrol of two rangers, presented as one: it walks the main road of green road there and back.</summary>
public class CpGreenRoad : Patrol
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
            "/domains/greenvale/corridor/green_road/m09",
            "/domains/greenvale/corridor/green_road/m10",
            "/domains/greenvale/corridor/green_road/m11",
        }, 45);
        AddChat("Travel in company through the Dark Hollow, if you can. Alone, walk quickly and do not stop to look at anything.");
        AddChat("Three rooms of road and we are always glad to reach the Crossroads. Do not ask us why.");
        AddTopic("road,roads,way,crossroads,crossroads stone,hollow,dark hollow,travel", "The Green Road runs on beneath the canopy to the Crossroads Stone, where an old overgrown track goes off east and the road itself turns north into the Heartwood. Nobody we know takes the track. The stretch between here and the stone is short, and it is the stretch where wagons are hit.");
        AddTopic("rangers,riverwatch,ranger,badge,eye,silver eye,ever vigilant", "The silver eye on green and grey, Riverwatch, Ever Vigilant. A pair is all that can be spared for this part of the road, and we cover more of it than a pair should. If you meet us, you are safer for a while.");
        AddTopic("watch,danger,dangers,beware,raiders,goblins,boar,boars,bandits,spiders,bears,bear", "Raiders in groups, boar that will not give way, and spiders in the glades off the road. Bandits sometimes, on the lookout above. There is no one thing to watch for here: watch everything, and keep to the road, and walk with other people if there are other people.");
    }
}
