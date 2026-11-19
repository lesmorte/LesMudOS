// @inherit /std/room

public class SrM02 : Room
{
    public override void Create()
    {
        ShortDesc = "The South Road, Hedgerow Mile";
        LongDesc = "Hedges close in on both sides here, hazel and blackthorn and dog rose knotted together so thickly that the road feels like a green corridor with the roof off. Wrens scold from the tangle, and now and then a gap shows a glimpse of beans, or a hay field, or a roof far off. The ground is drier than it was, and dust lifts behind every step. A stile in the hedge on the left marks a footpath that someone uses every day.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/m01";
        Exits["north"] = "/domains/greenvale/corridor/south_road/m03";
        ResetSeconds = 120;
        DangerBand(5, 6);

        AddDetail("hedges,hedge,hazel,blackthorn,tangle", "Hazel and blackthorn, dog rose and honeysuckle, all pleached together by generations of hedgers. The old stems are as thick as a man's wrist and bent into tight green knots.");
        AddDetail("wrens,wren,birds,sparrows", "A wren is sitting on the topmost twig, enormously offended by you. It is about the size of a walnut.");
        AddDetail("rose,roses,dog rose,honeysuckle", "Pale pink dog roses and a few cream stars of honeysuckle. The scent comes and goes with the breeze.");
        AddDetail("beans,field,hay field,roof,gap", "Through a gap in the hedge you see a field of beans in blue-green rows, and far beyond it a farm roof with a lazy plume of smoke.");
        AddDetail("dust,road,ground", "The dust is pale and fine and lies in a thin skin over the packed earth. Hooves and wheel rims have written their business in it.");
        AddReadable("stile,footpath,path", "Two worn steps of oak climb over the hedge. Initials have been cut into the top rail through many summers, and one pair is fresh enough to still be yellow:\n\"B + M\"\nThe path beyond runs off across the beans toward the farm.", "A stile and footpath cross the hedge. You could {cmd}read{/} the carved rail.");

        AddEncounter("/domains/greenvale/corridor/mob/field_rabbit", 10);
        AddEncounter("/domains/greenvale/corridor/mob/grey_fox", 5);
    }
}
