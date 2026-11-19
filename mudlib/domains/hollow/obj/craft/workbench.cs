// @inherit /std/station
public class CrWorkbench : Station
{
    public override void Create()
    {
        Name = "Workbench";
        Ids.Add("bench");
        Ids.Add("work bench");
        Ids.Add("heavy workbench");
        Ids.Add("black oak");
        ShortDesc = "a workbench";
        LongDesc = "A heavy bench of black oak, scarred with a lifetime of saw cuts, burn marks and one clear handprint in dried glue. A great iron vice is bolted to one end. Utility items and tools are made here.";
        StationKind = "workbench";
    }
}
