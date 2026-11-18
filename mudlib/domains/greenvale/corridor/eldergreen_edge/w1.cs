// @inherit /std/room

public class EeWoodcuttersRide : Room
{
    public override void Create()
    {
        ShortDesc = "The Woodcutters' Ride";
        LongDesc = "A broad ride runs through the trees here, cut wide enough for an ox-drawn sled and floored deep in bark, chips and sawdust that muffle every step. Felled oaks lie trimmed and stacked along it, waiting to be hauled out, and stumps as wide as tables stand in the cleared places with their rings bright and wet. The ring of an axe comes from further along, and the sharp clean smell of cut wood is everywhere. Thistles have colonised the sunny gaps, tall as a child.";
        Exits["west"] = "/domains/greenvale/corridor/eldergreen_edge/m03";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/w2";
        ResetSeconds = 120;

        AddDetail("ride,track,sled,drag marks,sawdust,chips,bark", "The ride is a trough of bark and chips, scored down the middle by runners. At every bend a post has been set to stop the sleds from sliding into the trees.");
        AddDetail("logs,oaks,felled oaks,timber,stack,stacks", "The felled oaks have been limbed and cut to length, each log chalked with a mark and a number. They are enormous, and each takes a team to move.");
        AddDetail("stumps,stump,rings,tree rings", "The stump nearest you has rings close as the lines of a hand, and you count to two hundred before you lose your place. Whatever stood here saw the road laid.");
        AddDetail("thistle,thistles,gaps,sun", "Thistles fill every sunny gap, thick-stemmed and spiny and full of goldfinches. A gatherer could harvest a good many without leaving the ride.");
        AddDetail("axe,sound,ring,chopping", "The axe falls in a slow regular rhythm, a pause between each stroke, as if its owner were thinking between blows.");
        AddDetail("sawpit,pit,saw", "A sawpit has been dug beside the ride, a long trench with a log laid across it. A pit-saw hangs from a branch nearby, its teeth bright.");
        AddNode("/domains/greenvale/corridor/obj/thistle_patch");

        AddSpawn("/domains/greenvale/corridor/eldergreen_edge/npc/finnian");
    }
}
