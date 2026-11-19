// @inherit /std/station
public class CrTanningRack : Station
{
    public override void Create()
    {
        Name = "Tanning Rack";
        Ids.Add("rack");
        Ids.Add("tanning rack");
        Ids.Add("tanning frame");
        Ids.Add("frame");
        Ids.Add("poles");
        ShortDesc = "a tanning rack";
        LongDesc = "A tall rack of lashed ash poles, taller than a man, built to hold a hide taut while it is scraped and treated. The poles are dark and slick from years of handling. Leather and hide treatment are done here, and nowhere else in the Hollow.";
        StationKind = "tanning rack";
    }
}
