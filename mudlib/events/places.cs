/// <summary>
/// Where things are: the trusted table that turns a room's program path into
/// the area and region a Chronicle entry names, and the list of what a
/// character must discover to be a Pathfinder in a region.
/// </summary>
public class EventPlaces : MudObject
{
    /// <summary>"path prefix|area|region". The longest matching prefix wins.</summary>
    public List<string> Places() => new()
    {
        "/domains/hollow/village/|The Village of Vincent's Hollow|Greenvale",
        "/domains/hollow/cellars/|The Forgotten Cellars|Greenvale",
        "/domains/oakenfell/|Oakenfell|Greenvale",
    };

    /// <summary>
    /// "region|closed or open|discovery keys". Pathfinder is awarded when every
    /// key of a closed region has been discovered. A region stays open while its
    /// list of areas is unfinished, so no one gets the title early.
    /// </summary>
    public List<string> Regions() => new()
    {
        "Greenvale|open|discover.area|vincents-hollow,discover.secret_dungeon|forgotten-cellars,discover.hidden|hidden-cache,discover.great_stump|great-stump",
    };
}
