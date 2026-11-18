// @inherit /std/room

public class GrTheBearTrees : Room
{
    public override void Create()
    {
        ShortDesc = "The Bear Trees";
        LongDesc = "A stand of huge beeches whose smooth grey trunks are scored to head height with long parallel gouges, each scar black with weeping sap. The ground between them is torn up and littered with beechnut husks, bones and clumps of coarse hair. A heavy animal smell hangs in the air. The path from the Wagon Halt comes in from the east, and a rougher track goes on west into the deeper wood.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/w4";
        Exits["east"] = "/domains/greenvale/corridor/green_road/m07";
        Exits["west"] = "/domains/greenvale/corridor/green_road/d3";
        Safe = false;

        AddDetail("trees,beeches,beech,trunks,bear trees", "Old beeches, smooth as columns. Every trunk is scarred at the height of a standing bear's reach, and the scars are fresh enough to weep.");
        AddDetail("scars,gouges,claw marks,claws,scratches,marks", "Sets of four parallel grooves, each as long as a forearm and deep enough to put a finger in. They are laid one over another, higher on some trees than a man could reach.");
        AddDetail("husks,beechnuts,nuts,mast,bones,bone,hair", "Beechnut husks in drifts, the clean bones of deer and a clump of coarse black hair. The hair is still oily.");
        AddDetail("ground,earth,torn earth,digging,den", "The soil is turned over in rough patches where something dug for roots. There is no den here, but the digging says it is near.");
        AddDetail("track,rough track,west,trail,paths", "A rough track leaves westward between two trunks. It is trampled rather than cut, and the smell on the air grows stronger along it.");
        AddDetail("herbs,herb,patch,flowers,wildflowers,sorrel", "A few herbs and wildflowers grow in the light gaps between the trees, and the bear seems not to have found them yet.");
        AddNode("/domains/greenvale/corridor/obj/herb_patch");
        AddNode("/domains/greenvale/corridor/obj/wildflowers");

        AddSpawn("/domains/greenvale/corridor/mob/young_black_bear");
    }
}
