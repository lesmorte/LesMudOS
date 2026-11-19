// @inherit /std/station
public class CrCourtWorkbench : Station
{
    public override void Create()
    {
        Name = "Workbench";
        Ids.Add("bench");
        Ids.Add("work bench");
        Ids.Add("workbench");
        Ids.Add("guild bench");
        ShortDesc = "a workbench";
        LongDesc = "A long bench of grey oak under the guild awning, its top inlaid with the brass marks of a dozen trades and worn pale where hands have lain. The Joined Guilds keep it free for any crafter who asks. Utility items and tools are made here.";
        StationKind = "workbench";
    }
}
