// @inherit /std/room

public class SrW5 : Room
{
    public override void Create()
    {
        ShortDesc = "The Hay Meadow";
        LongDesc = "A broad hay meadow slopes gently toward the road, golden and uncut, with wildflowers lying thick between the stalks. A farm track runs east across it to the yard of the inn, whose chimney smoke is a pleasant sight, and a narrow sheep-path leads away west into rougher ground. Grasshoppers click in the stems, and the grass whispers around your knees. The tall hedge on the far side is darker than the rest, with a gap in it that leads uphill to a ruin.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/w4";
        Exits["east"] = "/domains/greenvale/corridor/south_road/m07";
        Exits["west"] = "/domains/greenvale/corridor/south_road/d3";
        ResetSeconds = 120;
        DangerBand(5, 6);

        AddDetail("hay,meadow,grass,stalks", "Meadow hay, seeding and heavy, bowed over with its own weight. It smells of summer and sweetness.");
        AddDetail("wildflowers,flowers,poppies,cornflowers,daisies", "Poppies, cornflowers, yellow rattle and ox-eye daisies, all tangled together. A few honey bees crawl heavily among them.");
        AddDetail("grasshoppers,grasshopper,insects", "A grasshopper clicks from every third stem. Step and they stop; stand still and they start again.");
        AddDetail("track,farm track,east,chimney,inn", "The track runs east to the yard of Traveler's Rest. You can smell the kitchen from here.");
        AddDetail("path,sheep-path,sheep path,west,gap,hedge,ruin", "The sheep-path climbs from the gap in the hedge toward a broken line of stones on the ridge. The shepherds say the sheep go that way only to be fetched back.");
        AddNode("/domains/greenvale/corridor/obj/wildflowers");

        AddSpawn("/domains/greenvale/corridor/mob/field_rabbit", 2);
        AddSpawn("/domains/greenvale/corridor/mob/roe_deer");
        AddSpawn("/domains/greenvale/corridor/mob/stray_wolf");
    }
}
