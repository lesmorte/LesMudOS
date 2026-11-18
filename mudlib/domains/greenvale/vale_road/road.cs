// @inherit /std/room

/// <summary>
/// The first room beyond Vincent's Hollow: a one-room area in Greenvale. The road runs on toward the first major city of
/// the vale, which is not built yet, so the way ahead is closed. The room is not safe.
/// </summary>
public class ValeRoad : Room
{
    public override void Create()
    {
        ShortDesc = "The Vale Road";
        LongDesc = "Past the last milestone of Vincent's Hollow the North Road widens into the Vale Road, a rutted cart track between hedgerows and rolling green fields. The air is cooler here, and the smoke of the village is already a smudge behind you. The road runs on north toward Oakenfell, the first city of Greenvale, but a tangle of fallen timber and loose stone has been dragged across it a little way ahead.";
        Exits["south"] = "/domains/hollow/village/north_road";
        Safe = false;
        BlockExit("north", "The road runs on toward Oakenfell, but fallen timber and rubble block the way ahead. It is not open yet.");

        AddDetail("road,track,cart track,vale road", "The track is churned and rutted by carts, with a strip of grass down the middle where the wheels do not reach. It runs north between the hedgerows.");
        AddDetail("hedgerows,hedgerow,hedge,hedges", "Hawthorn and bramble, thick with last year's berries, line both sides of the road.");
        AddDetail("fields,field,green fields,vale", "Green fields roll away on either side toward low hills. Somewhere out there, Oakenfell, the first city of the vale, lies waiting.");
        AddDetail("timber,stone,rubble,tangle,fallen timber", "Whole trunks and a drift of loose stone have been dragged across the road. Someone has been at it with an axe, but the work is far from finished.");
    }
}
