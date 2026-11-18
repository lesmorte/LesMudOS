// @inherit /std/room

public class HwW5 : Room
{
    public override void Create()
    {
        ShortDesc = "The Stalker's Run";
        LongDesc = "The trail narrows to a run between two ranks of trunks, scarcely wider than a man's shoulders, and goes straight for a hundred paces. The bark on both sides is scored at the height of your chest in long vertical grooves, and the ground between is trodden to a dark smooth path with no leaf upon it. Small bones lie in drifts along the edges, picked clean. It is quiet as a held breath. The trail meets the road again to the west, and another branch leads east into the deep wood.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/w4";
        Exits["west"] = "/domains/greenvale/corridor/heartwood/m07";
        Exits["east"] = "/domains/greenvale/corridor/heartwood/d3";

        AddDetail("run,path,trail,ground,floor,track", "The path is bare earth, packed so hard by many passes that it rings under a boot. It is not the trail of a herd. It is a single line, walked and walked by one thing that takes the same road at the same hour.");
        AddDetail("grooves,scratches,bark,scored,claw,claws,marks", "The grooves are in threes, a finger deep, and each set is a little longer than the one before it as the run goes on. Where it ends in the east the bark has been torn off in strips.");
        AddDetail("bones,bone,drifts,skulls,skull", "The bones are those of deer, hare and fox, and the skull of a boar with the tusks gone. They are stacked, not scattered, at the base of the trunks, as a cat lays out what it has killed. There are no tracks of scavengers anywhere.");
        AddDetail("trunks,ranks,trees,boles", "The trunks stand in two rough lines, so close that their roots overlap. The branches above have grown together into a low roof, and the run beneath it is dim.");
        AddDetail("west,road,memorial", "To the west, through the trunks, you can see the green and grey pennant at the Hunter's Memorial. It looks very far off.");
        AddDetail("east,deep wood,thicket,deep", "East, the run bends and dips into the thicker wood, and the trunks stand closer and darker.");
        AddSpawn("/domains/greenvale/corridor/mob/forest_stalker", 2);
    }
}
