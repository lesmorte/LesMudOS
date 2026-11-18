// @inherit /std/room

public class OaM08 : Room
{
    public override void Create()
    {
        ShortDesc = "Southfarm Hamlet, the Green";
        LongDesc = "A village green lies open beside the road, with a duck pond at one end, a stone well under a little thatched roof, and a ring of whitewashed cottages whose chimneys all smoke at once. Geese patrol the grass, washing flaps on a line, and the smell of baking bread drifts from a cottage with a table of loaves set out beneath its window. After the long, wary miles of road, the quiet here feels like an unwritten rule: nobody draws a blade on Southfarm Green.";
        Exits["south"] = "/domains/greenvale/corridor/approach/m07";
        Exits["north"] = "/domains/greenvale/corridor/approach/m09";
        Safe = true;
        ResetSeconds = 120;

        AddDetail("green,grass,village green,common", "The grass is cropped short by geese and sheep in turn, and rubbed bare in a ring where the village children have been playing for generations. A stone in the middle marks the spot where the hamlet's moots are held.");
        AddDetail("pond,duck pond,ducks,water", "Six ducks and one very dignified drake cruise the pond, which is the colour of weak tea and edged with irises. A child's wooden boat lies stranded among the reeds.");
        AddDetail("well,thatched well,roof,bucket", "A stone-lipped well with a windlass and a pitched thatch roof, the bucket chain bright with use. A tin cup hangs from a nail for any traveler to drink from.");
        AddDetail("cottages,houses,chimneys,whitewash", "The cottages are low and whitewashed, with deep thatch and brightly painted doors, each with a vegetable patch at its side. They have the look of places where the same family has lived since the roads were mud.");
        AddDetail("children,child,boat,playing,games", "A knot of children play a game involving a ball, a stick and rules they are revising as they go. They stop to stare at your weapons, and then resume with greater noise.");
        AddDetail("geese,goose,washing,line", "A gander the size of a dog guards the path to the pond and hisses at the ground in general. The washing on the line, white and blue, flaps like signal flags.");
        AddDetail("bread,loaves,table,window,baking", "The table under the window holds loaves, rounds of cheese under muslin, and a tray of baked apples glistening with honey. A small hand-lettered board leans against it.");
        AddReadable("board,hand-lettered board,prices,little board", "SOUTHFARM, bread, cheese and baked apples. Honest weight. Leave the plate.", "A small board leans against the loaves. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/approach/npc/farm_wife");
        AddSpawn("/domains/greenvale/corridor/approach/npc/old_farmer");
    }
}
