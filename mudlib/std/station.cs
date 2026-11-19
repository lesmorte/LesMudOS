// @inherit /std/thing

/// <summary>
/// A crafting station: a herbalist table, a tanning rack, a workbench, a cooking fire (a scribing desk is reserved). It stands in
/// a room and cannot be picked up; a recipe that names a station can be made only in a room that has one of that kind. Place it
/// with AddSpawn(path) in the room's Create(). Set StationKind to one of the kinds in the crafting data ("herbalist table").
/// Only stations whose file lies in a trusted tree count. See docs/CRAFTING.md and docs/BUILDING.md.
/// </summary>
public class Station : Thing
{
    /// <summary>The station kind, as the crafting data and the recipes name it: "herbalist table", "tanning rack", "workbench", "cooking fire".</summary>
    protected string StationKind = "";

    public override bool CanGet() => false;

    public bool IsStation() => true;

    public string QueryStationKind() => StationKind;

    public override bool Id(string id) => base.Id(id) || id == StationKind || id == "station";

    public override string Long() => base.Long() + "It is a crafting station. Type {cmd}recipes{/} to see what you know how to make, and {cmd}craft{/} or {cmd}combine{/} to work here.\n";

    public override string Stat() => base.Stat() + $"  Station: {StationKind}\n";
}
