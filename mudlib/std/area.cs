/// <summary>
/// The data of one area: its name and the region it belongs to. A file called area.cs in an area's directory
/// inherits this class and sets the two names once; every room in that directory belongs to the area, and through it to
/// the region, without repeating the names (docs/WORLD.md). `score` shows them.
/// </summary>
public abstract class Area : MudObject
{
    /// <summary>"The Village of Vincent's Hollow".</summary>
    protected string AreaName = "";
    /// <summary>"Green Vale".</summary>
    protected string RegionName = "";

    public string QueryAreaName() => AreaName;

    public string QueryRegionName() => RegionName;
}
