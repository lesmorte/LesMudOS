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
        HideClue("tally", SearchModerate, "hooks,hook,lantern hooks,crack",
            "Behind the last hook, wedged into a crack and forgotten, is a slip of greasy paper.", "slip,paper,tally,scrap",
            "A smuggler's tally, in a quick, cramped hand.\n  Casks, six. Cloth, nine bolts. Lamp-oil, a dozen flasks.\n  The long things go in the dry room, up on top and under sacking. Never on the floor. They are the Warden's, and he will ask.\n  The key stays with the post. Whoever holds the post holds the door.");
        Light = LightDim;
        AddSpawn("/domains/hollow/obj/wall_lantern");
        LongDesc += " One of the old hooks has a lantern on it again, the only one; the rest wear their crusts of tallow.";

        AddDetail("hooks,hook,lantern hooks,lantern hook,rusted hooks,crack", "Iron hooks jut from the walls at head height, each one still wearing a crust of old tallow from the lanterns that once hung there. Behind the last one a crack runs back into the stone, and something pale is wedged in it.");
        AddDetail("walls,corridor,stone walls", "The walls are cut square and smooth, the work of people who wanted their goods hidden rather than their tunnels admired.");
        AddDetail("floor,flagstones,grooves", "The floor is worn into shallow grooves by generations of heavy boots and dragged crates.");
    }
}
