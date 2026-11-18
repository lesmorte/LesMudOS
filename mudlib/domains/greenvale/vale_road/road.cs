// @inherit /std/room

/// <summary>
/// The first room beyond Vincent's Hollow, on the Vale Road to Oakenfell. The room is not safe.
/// </summary>
public class ValeRoad : Room
{
    public override void Create()
    {
        ShortDesc = "The Vale Road";
        LongDesc = "Past the last milestone of Vincent's Hollow the North Road widens into the Vale Road, a rutted cart track between hedgerows and rolling green fields. The air is cooler here, and the smoke of the village is already a smudge behind you. The road runs on north toward Oakenfell, the first city of Greenvale. A heap of cut timber and loose stone lies at the verge, where a fall has lately been cleared from the way.";
        Exits["south"] = "/domains/hollow/village/north_road";
        Safe = false;
        Exits["north"] = "/domains/greenvale/corridor/south_road/m01";

        AddDetail("road,track,cart track,vale road", "The track is churned and rutted by carts, with a strip of grass down the middle where the wheels do not reach. It runs north between the hedgerows.");
        AddDetail("hedgerows,hedgerow,hedge,hedges", "Hawthorn and bramble, thick with last year's berries, line both sides of the road.");
        AddDetail("fields,field,green fields,vale", "Green fields roll away on either side toward low hills. Somewhere out there, Oakenfell, the first city of the vale, lies waiting.");
        AddDetail("timber,stone,rubble,tangle,fallen timber", "Whole trunks and a drift of loose stone have been dragged to the verge. Someone has been at it with an axe, and the road is clear.");
    }

    // Tells the Chronicle that a player has come here. A character is credited once, so every visit may report.
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        try
        {
            LoadObject("/secure/events").Call<string>("Report", "travel.pass", who, new List<ObjectRef> { who }, Self, "vale-road",
                new Dictionary<string, string> { ["name"] = "the Vale Road" });
        }
        catch (MudRuntimeException) { }
    }
}
