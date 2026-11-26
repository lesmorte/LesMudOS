// @inherit /std/room

/// <summary>The smugglers' storeroom: rotting crates, scavengers and a healing potion for whoever clears them.</summary>
public class StorageChamber : Room
{
    public override void Create()
    {
        ShortDesc = "Storage Chamber";
        LongDesc = "Rotting crates and abandoned trade goods are stacked against damp walls.";
        Exits["west"] = "/domains/hollow/cellars/collapsed_passage";
        Exits["north"] = "/domains/hollow/cellars/smuggler_hall";
        Safe = false;
        Light = LightDim;
        AddSpawn("/domains/hollow/obj/wall_lantern");
        LongDesc += " A lantern burns on the nearest crate. Whoever hung it meant people to find their way.";

        AddDetail("crates,rotting crates,crate,splintered crates", "Splintered crates are stacked three high, their lids pried off and their contents long since rotted or carried away.");
        AddDetail("goods,trade goods,bales,sacks,grain sacks", "Bales of cloth have gone grey with mould, and the sacks beside them hold a dark, crumbling mass that was once grain.");
        AddDetail("walls,damp walls,wall", "Water beads on the stone and runs down in dark streaks, leaving pale crusts where it has dried.");
        AddDetail("marks,scratches,tally marks", "Someone has scratched tally marks into the back of a crate, row after row, and the last few have not been finished.");

        AddSpawn("/domains/hollow/mob/goblin_scavenger", 2);
        AddSpawn("/domains/hollow/obj/healing_potion");
    }
}
