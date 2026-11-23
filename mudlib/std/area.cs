/// <summary>
/// The data of one area: its name and the region it belongs to. A file called area.cs in an area's directory
/// inherits this class and sets the two names once; every room in that directory belongs to the area, and through it to
/// the region, without repeating the names (docs/WORLD.md). `score` shows them.
/// </summary>
public abstract class Area : MudObject
{
    /// <summary>"The Village of Vincent's Hollow".</summary>
    protected string AreaName = "";
    /// <summary>"Greenvale".</summary>
    protected string RegionName = "";

    /// <summary>
    /// The level band of the area's rooms: ground meant for characters of level BandLow to BandHigh (0 for none). A room that calls
    /// its own DangerBand keeps that instead. Entering a band whose low end is two or more above your level warns you once.
    /// </summary>
    protected int BandLow;
    protected int BandHigh;

    /// <summary>What the ground of this area is like, for clients that draw a map (GMCP): outdoors, indoors, underground, road or water. A room can say otherwise with its own Terrain. Default outdoors.</summary>
    protected string Terrain = "";

    public string QueryTerrain() => Terrain;

    public int QueryBandLow() => BandLow;

    public int QueryBandHigh() => BandHigh;

    public string QueryAreaName() => AreaName;

    public string QueryRegionName() => RegionName;
}
