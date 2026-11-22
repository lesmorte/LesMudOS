// @inherit /std/patrol

/// <summary>A Riverwatch patrol of two rangers, presented as one: it walks the main road of heartwood there and back.</summary>
public class CpHeartwood : Patrol
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
            "/domains/greenvale/corridor/heartwood/m07",
            "/domains/greenvale/corridor/heartwood/m08",
            "/domains/greenvale/corridor/heartwood/m09",
            "/domains/greenvale/corridor/heartwood/m10",
            "/domains/greenvale/corridor/heartwood/m11",
        }, 45);
        AddChat("The trees here were old before the road was. We try to remember that we are the guests.");
        AddChat("Memorial to the Last Great Oak and back. When the trees thin, you are nearly through.");
        AddTopic("road,roads,way,paved,memorial,oak,last great oak,hunters memorial,hunter's memorial", "The old paved stretch is older than the Rangers and older than the road. North of the Memorial the forest begins to thin, and the Last Great Oak marks where Heartwood ends and the Approach begins. Keep to the stone and you will find it.");
        AddTopic("rangers,riverwatch,ranger,badge,eye,silver eye,ever vigilant,hunter,hunters", "We are Riverwatch, with the silver eye on green and grey, Ever Vigilant. There is a hunter's station in this wood, one of ours, and the Memorial is for those who did not come back from it. It is a long list for so short a road.");
        AddTopic("watch,danger,dangers,beware,bears,bear,goblins,goblin,stalkers,stalker,woodlings,wolves,wolf,thorn", "Bears, goblin warriors out of the ruins, stalkers that you hear only after they have chosen you, and a thing or two in the thorn that walks when it should not. The big wolf keeps to its dell. Do not leave the road to see the ruins, and leave any grove you find alone.");
    }
}
