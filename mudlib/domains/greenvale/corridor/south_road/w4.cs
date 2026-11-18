// @inherit /std/room

public class SrW4 : Room
{
    public override void Create()
    {
        ShortDesc = "The Brook Pasture";
        LongDesc = "A low pasture lies along the brook, rich and green and bright with buttercups, where a few red cattle graze up to their knees in clover. Willows crowd the water, meadowsweet foams along its banks, and the air is thick with the scent of crushed mint. In the middle of the grass a ring of young hawthorns stands around a patch of bare ground, and the cattle graze up to the ring and no further. The brook murmurs over stones, and a heron lifts from the shallows at your step.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/w3";
        Exits["north"] = "/domains/greenvale/corridor/south_road/w5";
        ResetSeconds = 120;

        AddDetail("pasture,grass,clover,buttercups", "Lush pasture, wet underfoot, golden with buttercups. Wild strawberries glint red in the sunniest bank.");
        AddDetail("cattle,cows,cow", "Red cattle with white faces, chewing in the shade of a willow. They study you with one soft eye.");
        AddDetail("brook,stream,willows,meadowsweet,mint", "The brook is the same one that crosses the road, here running slow and silty. Meadowsweet and wild mint grow down to the waterline and smell wonderful underfoot.");
        AddDetail("hawthorns,hawthorn,ring,saplings,trees", "Seven young hawthorns stand in a neat ring, too regular for chance. Their bark is unmarked, and there is not a bird's nest in any of them.");
        AddDetail("bare ground,patch,ground,wool,rags,ribbons", "In the middle of the ring is a circle of bare, bitten-down earth, though nothing has been grazed there. Faded strips of wool and cloth are knotted to the lowest branch, wind-whitened and old.");
        AddDetail("wildflowers,flowers,bank", "Ox-eye daisies, red clover and purple vetch, a wild bank of colour. Bees work it from sunrise.");
        AddNode("/domains/greenvale/corridor/obj/wildflowers");

        AddSpawn("/domains/greenvale/corridor/mob/roe_deer");
        AddSpawn("/domains/greenvale/corridor/mob/grey_fox");
    }
}
