// @inherit /std/patrol

/// <summary>A Riverwatch patrol of two rangers, presented as one: it walks the main road of approach there and back.</summary>
public class CpApproach : Patrol
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
            "/domains/greenvale/corridor/approach/m03",
            "/domains/greenvale/corridor/approach/m04",
            "/domains/greenvale/corridor/approach/m05",
            "/domains/greenvale/corridor/approach/m06",
            "/domains/greenvale/corridor/approach/m07",
        }, 45);
        AddChat("Toll House to the River Bend, up and down. You will be at the walls by tomorrow.");
        AddChat("The city is close, and people relax. That is when we are busiest.");
        AddTopic("road,roads,way,toll,toll house,staging,staging grounds,aqueduct,river bend,bend", "From the Toll House the road is stone, and from the Staging Grounds the wagons make it crowded. The old aqueduct stands over the road at the sixth stone, and the River Bend after it is where you first see the water. After that, the hamlet and the Rise, and then the walls.");
        AddTopic("rangers,riverwatch,ranger,badge,eye,silver eye,ever vigilant,sergeant,oakenfell", "We are Riverwatch, the silver eye on green and grey, Ever Vigilant. We keep an office in Oakenfell by the quay; the sergeant there takes reports. We are stretched thin, and the Toll House is stretched thinner. Be patient with both.");
        AddTopic("watch,danger,dangers,beware,bandits,bandit,highwaymen,goblins,goblin,veterans,quarry,camp", "Bandits keep to the Bandits' Road and the quarry track to the west, and they take what they can from the wagons they can reach. Old goblin soldiers den under the hill. Do not wander off the stone near the aqueduct, and do not take a short cut through the quarry.");
    }
}
