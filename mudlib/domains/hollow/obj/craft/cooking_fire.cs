// @inherit /std/station
public class CrCookingFire : Station
{
    public override void Create()
    {
        Name = "Cooking Fire";
        Ids.Add("fire");
        Ids.Add("fireplace");
        Ids.Add("hearth");
        Ids.Add("fire place");
        Ids.Add("cooking fire");
        ShortDesc = "a cooking fire";
        LongDesc = "A wide stone fireplace with a crackling log fire. The mantel is blackened with soot and crowded with candle stubs, and an iron pot hangs ready on a hook over the flames. Food and drink are cooked here.";
        StationKind = "cooking fire";
    }
}
