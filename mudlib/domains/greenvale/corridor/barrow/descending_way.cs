// @inherit /std/room

public class FbDescendingWay : Room
{
    public override void Create()
    {
        ShortDesc = "The Descending Way";
        LongDesc = "A passage hardly wider than a man's outstretched arms goes down in long flights, each of seven steps and then a level landing, all cut in the one stone with no seam. The walls rise to a pointed vault so high that lamplight does not find it, and the dust on the floor lies undisturbed but for a single narrow track. On the left wall the carved figures walk down with you, one to a step. At the landing a tall doorway opens to the west, and to the east is a low opening that you might take for a fallen block.";
        Exits["south"] = "/domains/greenvale/corridor/barrow/forecourt";
        Exits["down"] = "/domains/greenvale/corridor/barrow/long_gallery";
        Exits["west"] = "/domains/greenvale/corridor/barrow/resealed_tomb";
        Exits["east"] = "/domains/greenvale/corridor/barrow/low_niche";

        AddDetail("steps,step,flights,flight,stair,landing,landings,stone", "Seven steps, a landing as long as a man, seven steps again. The treads are exactly the same height, so that you could go down them in the dark and never miss one. There is no wear in the middle of any of them.");
        AddDetail("vault,roof,ceiling,arch,darkness", "The vault is a tall, narrow point, like a cupped pair of hands held upright. Where the lamplight stops, the stone goes on.");
        AddDetail("west doorway,doorway,door,tall doorway,west", "The doorway to the west is the same shape as the one above, tall and narrow with a pointed head, and it has no door. The chamber inside is square. A pale line of fresh metal runs along something within, which catches the light.");
        AddDetail("opening,low opening,block,slab,east,gap", "A slab has slumped across all but the bottom of the eastern doorway and leans against its own jamb. It looks like rubble, and a hurried party would pass it by. Under it, a gap at the floor is wide enough to crawl through.");
        AddDetail("floor,dust,tracks,track,footprints,prints", "The pale dust on the landing is trodden along the centre in a single narrow track, and brushed level at the sides. The track goes down the stair, and it does not come back by the same line: someone swept it behind them with great patience.");
        AddReadable("band,carving,carvings,frieze,figures,figure,procession,pattern,border", "There are seven figures to a flight, one to a step, so that the procession seems to descend as you do. Each has one arm raised. On every seventh flight the figure on the landing bears the bound branch, and the cord at its middle is knotted seven times, and the knots are cut so finely that you can count them with a finger. Above them the border runs on unbroken. Nothing in it is a letter.", "The carved figures walk down the wall beside you. You could {cmd}read{/} the band.");
        AddSpawn("/domains/greenvale/corridor/mob/ancient_skeleton", 1);
    }
}
