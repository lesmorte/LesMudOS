// @inherit /std/room

/// <summary>A broad corridor that once carried smugglers' goods, now a junction of the Forgotten Cellars.</summary>
public class SmugglerHall : Room
{
    public override void Create()
    {
        ShortDesc = "Smuggler Hall";
        LongDesc = "A broad corridor once used by smugglers. Rusted lantern hooks remain attached to the walls.";
        Exits["south"] = "/domains/hollow/cellars/storage_chamber";
        Exits["east"] = "/domains/hollow/cellars/flooded_tunnel";
        Exits["west"] = "/domains/hollow/cellars/guard_post";
        Safe = false;

        AddDetail("hooks,lantern hooks,lantern hook,rusted hooks", "Iron hooks jut from the walls at head height, each one still wearing a crust of old tallow from the lanterns that once hung there.");
        AddDetail("walls,corridor,stone walls", "The walls are cut square and smooth, the work of people who wanted their goods hidden rather than their tunnels admired.");
        AddDetail("floor,flagstones,grooves", "The floor is worn into shallow grooves by generations of heavy boots and dragged crates.");
    }
}
