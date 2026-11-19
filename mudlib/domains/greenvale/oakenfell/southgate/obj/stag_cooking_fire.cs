// @inherit /std/station
public class CrStagCookingFire : Station
{
    public override void Create()
    {
        Name = "Cooking Fire";
        Ids.Add("fire");
        Ids.Add("fireplace");
        Ids.Add("hearth");
        Ids.Add("cooking fire");
        ShortDesc = "a cooking fire";
        LongDesc = "The hearth is wide enough to roast an ox, and on feast nights they do. A spit, a trivet and a row of black iron pots wait at its edge, and a silver-grey stag's head hangs above the mantel. Food and drink are cooked here.";
        StationKind = "cooking fire";
    }
}
