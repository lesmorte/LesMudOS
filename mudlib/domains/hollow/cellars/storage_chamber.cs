// @inherit /std/room

/// <summary>The smugglers' storeroom: rotting crates, scavengers and a healing potion for whoever clears them.</summary>
public class StorageChamber : Room
{
    public override void Create()
    {
        ShortDesc = "Storage Chamber";
        LongDesc = "Rotting crates and abandoned trade goods are stacked against damp walls. A slumped heap of grain sacks fills one corner.";
        Exits["west"] = "/domains/hollow/cellars/collapsed_passage";
        Exits["north"] = "/domains/hollow/cellars/smuggler_hall";
        Safe = false;

        AddDetail("crates,rotting crates,crate,splintered crates", "Splintered crates are stacked three high, their lids pried off and their contents long since rotted or carried away.");
        AddDetail("goods,trade goods,bales,sacks,sack,grain sacks", "Bales of cloth have gone grey with mould, and the sacks beside them hold a dark, crumbling mass that was once grain. One of them, at the bottom of the heap, is the wrong shape for grain. You could {cmd}search{/} the sacks.");
        AddDetail("walls,damp walls,wall", "Water beads on the stone and runs down in dark streaks, leaving pale crusts where it has dried.");
        AddDetail("marks,scratches,tally marks", "Someone has scratched tally marks into the back of a crate, row after row, and the last few have not been finished.");

        HideItem("honey", "/domains/hollow/obj/craft/honey", SearchEasy, "sacks,sack,grain sacks",
            "Under the rotted sacks, in a gap between the stones, someone has kept a stoppered jar of honey out of the damp. You take it.");

        AddSpawn("/domains/hollow/mob/goblin_scavenger", 2);
        AddSpawn("/domains/hollow/obj/healing_potion");
    }
}
