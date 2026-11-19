// @inherit /std/station
public class CrCourtTanningRack : Station
{
    public override void Create()
    {
        Name = "Tanning Rack";
        Ids.Add("rack");
        Ids.Add("tanning rack");
        Ids.Add("tanning frame");
        Ids.Add("frame");
        ShortDesc = "a tanning rack";
        LongDesc = "A sturdy frame of iron-shod poles at the edge of the court, set well away from the cloth. Dark stains run down the poles, and a scraper hangs from a chain on the nearest one. Leather and hide treatment are done here.";
        StationKind = "tanning rack";
    }
}
