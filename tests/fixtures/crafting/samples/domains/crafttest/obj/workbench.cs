// @inherit /std/station
public class CtWorkbench : Station
{
    public override void Create()
    {
        Name = "Workbench";
        Ids.Add("workbench");
        ShortDesc = "a workbench";
        LongDesc = "A workbench for the crafting test.";
        StationKind = "workbench";
    }
}
