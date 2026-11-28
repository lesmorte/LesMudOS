// @inherit /std/room
public class OwnRoom : Room
{
    public override void Create() { ShortDesc = "Own"; LongDesc = "The Keeper of Leaks keeps its leaky secrets; the leak hunt goes on and Seep Away is cast here."; Exits["out"] = "/domains/leak/town/hall"; ReportEvent("class.advanced", ""); }
}
