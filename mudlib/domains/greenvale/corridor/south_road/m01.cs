// @inherit /std/room

public class SrM01 : Room
{
    public override void Create()
    {
        ShortDesc = "The South Road, Beyond the Hollow";
        LongDesc = "The road leaves the last cottage gardens of Vincent's Hollow and settles into open country, two ruts with a stripe of grass between them. Cow parsley and nettle crowd the verges, and a hawthorn hedge keeps pace on the right hand. Behind you the village smoke thins to a thread, and ahead the land lifts in long green swells under a very large sky. The air tastes of cut grass and, faintly, of horse.";
        Exits["south"] = "/domains/greenvale/vale_road/road";
        Exits["north"] = "/domains/greenvale/corridor/south_road/m02";
        ResetSeconds = 120;
        DangerBand(5, 6);

        AddDetail("road,ruts,track,grass", "The ruts are deep and old, filled with the dried mud of last month's rain. The grass between them is flattened by hooves but never quite dies.");
        AddDetail("hedge,hawthorn,hedgerow", "A thick hawthorn hedge, laid by someone who knew the craft. Sparrows go in and out of it like gossip.");
        AddDetail("cow parsley,nettle,nettles,verge,verges", "White lace of cow parsley stands shoulder high on the verge, with nettles hiding at its feet. Whoever has tried to pick a way through them has left a thin trodden path.");
        AddDetail("village,smoke,hollow,vincent's hollow", "South, over your shoulder, the smoke of Vincent's Hollow rises straight and grey. It looks very small from here.");
        AddDetail("sky,swells,hills", "The land swells and dips like a slow sea, green on green, with a few distant hedges drawn across it like stitches.");
        AddReadable("stone,boundary stone,marker", "The stone is knee-high and lichened, cut square on its south face:\n\"HERE ENDS THE PARISH OF VINCENT'S HOLLOW. KEEP THE ROAD.\"\nSomeone has scratched a small ring of dots beneath, a child's game or a farmer's tally.", "A leaning boundary stone stands in the grass. You could {cmd}read{/} it.");

        AddEncounter("/domains/greenvale/corridor/mob/field_rabbit", 10);
        AddEncounter("/domains/greenvale/corridor/mob/grey_fox", 5);
    }
}
