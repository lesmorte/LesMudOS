// @inherit /std/station
public class CtHerbalistTable : Station
{
    public override void Create()
    {
        Name = "Herbalist Table";
        Ids.Add("table");
        ShortDesc = "a herbalist table";
        LongDesc = "A herbalist table for the crafting test.";
        StationKind = "herbalist table";
    }
}
