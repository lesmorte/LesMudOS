// @inherit /std/room

public class SrW1 : Room
{
    public override void Create()
    {
        ShortDesc = "Old Miller's Farm, the Yard";
        LongDesc = "A farmyard of beaten earth lies between the old stone farmhouse and a great barn, with a millstone worn through for a doorstep and a well in the middle under a conical roof. Hens scratch in the dust, a brindled dog dozes in the sun, and the whole yard smells of flour, straw and warm dung. Sacks of grain are stacked by the barn door under a canvas, ready for the mill. From the hill to the north-west comes the slow sigh of the sails turning, and a footpath leads on through the orchard.";
        Exits["east"] = "/domains/greenvale/corridor/south_road/m03";
        Exits["north"] = "/domains/greenvale/corridor/south_road/w2";
        ResetSeconds = 120;

        AddDetail("farmhouse,house,thatch,door", "Stone below and thatch above, with a doorway low enough that tall men learn humility. Geraniums crowd a cracked pot on the sill.");
        AddDetail("barn,barn door,doors", "A barn the size of a chapel, its huge doors propped open on a mow of hay. Swallows nest in the rafters.");
        AddDetail("millstone,doorstep,step", "An old millstone, its grooves worn smooth, laid flat before the door. Generations have stepped over the hole where an axle once turned.");
        AddDetail("well,roof,conical roof", "A conical-roofed well in the middle of the yard. The water is cold and sweet, and a wooden bucket waits on the lip.");
        AddDetail("sacks,grain,canvas,stack", "Sacks of grain stacked three high under a canvas. Each is stamped with a tiny wheat sheaf and a name.");
        AddDetail("dog,brindled dog,hens", "The dog opens one eye at you, closes it again, and decides you are not a problem. The hens have not decided.");

        AddSpawn("/domains/greenvale/corridor/south_road/npc/miller_leofric");
        AddSpawn("/domains/greenvale/corridor/south_road/npc/farmwife_edda");
    }
}
