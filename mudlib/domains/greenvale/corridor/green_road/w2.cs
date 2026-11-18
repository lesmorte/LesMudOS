// @inherit /std/room

public class GrTheRaidersFord : Room
{
    public override void Create()
    {
        ShortDesc = "The Raiders' Ford";
        LongDesc = "A brown stream crosses the path in a shallow bed of gravel and tree roots, with high banks and thickets of alder on either side. Wading it is easy, which is why raiders like it: anyone crossing is looking at their feet. Notched sticks have been jammed in the bank, and a pile of cut brush offers a place to lie. The water gurgles, and sounds from the far side are hard to place.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/w1";
        Exits["north"] = "/domains/greenvale/corridor/green_road/w3";
        Safe = false;

        AddDetail("stream,ford,brook,water,gravel,bed", "A shallow, quick stream over a gravel bed, ankle deep at the crossing and hip deep either side of it. Boot prints and small bare feet have chewed up the mud on both banks.");
        AddDetail("banks,bank,alder,alders,thicket,thickets", "Alders overhang the water, and their roots make a ladder up the bank. A man could lie flat on the top and see everyone crossing.");
        AddDetail("sticks,notched sticks,notches,marks,tally", "Short sticks, each cut with three notches, are pushed into the bank every few feet. Some are fresh. They look like markers, or a tally, and not the work of a woodsman.");
        AddDetail("brush,pile,cut brush,hide,hiding place", "A heap of cut alder, piled to make a blind. There is a hollow in it where somebody has lain, and it is still flattened.");
        AddDetail("herbs,herb,plants,patch,watercress,yarrow", "Watercress grows in the shallows and yarrow on the bank, a good patch for anyone who knows what to pick.");
        AddDetail("prints,footprints,tracks,mud", "Boot prints and bare feet, one set larger and heavier than the rest. They go across, and none come back.");

        AddSpawn("/domains/greenvale/corridor/mob/boar_brute");
    }
}
