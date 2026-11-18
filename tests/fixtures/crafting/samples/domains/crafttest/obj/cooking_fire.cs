// @inherit /std/station
public class CtCookingFire : Station
{
    public override void Create()
    {
        Name = "Cooking Fire";
        Ids.Add("fire");
        ShortDesc = "a cooking fire";
        LongDesc = "A cooking fire for the crafting test.";
        StationKind = "cooking fire";
    }
}
