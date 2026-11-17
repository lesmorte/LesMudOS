// @inherit /std/room

/// <summary>The first room of the Forgotten Cellars, reached through the broken wall of the tavern cellar.</summary>
public class CollapsedPassage : Room
{
    public override void Create()
    {
        ShortDesc = "Collapsed Passage";
        LongDesc = "A narrow tunnel littered with loose stone and broken support beams.";
        Exits["west"] = "/domains/hollow/village/cellar";
        Exits["east"] = "/domains/hollow/cellars/storage_chamber";
        Safe = false;

        AddDetail("stone,loose stone,rubble,rocks", "Grey stones of every size lie where the roof gave way, and some of them shift underfoot when you step on them.");
        AddDetail("beams,beam,support beams,broken beams,timber", "Split oak props lean at crooked angles, their joints crushed. Someone propped them up once and then left them to rot.");
        AddDetail("roof,ceiling,gap", "A dark gap opens overhead where the earth has sagged, and fine grit trickles down whenever you breathe too loudly.");
        AddDetail("tracks,footprints,claw marks", "Small clawed prints cross the dust in every direction, and some of them lead straight into the rubble.");

        AddSpawn("/domains/hollow/mob/giant_rat", 2);
    }
}
