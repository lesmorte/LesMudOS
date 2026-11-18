// @inherit /std/room

public class SrM06 : Room
{
    public override void Create()
    {
        ShortDesc = "The South Road, the Sheepwalk";
        LongDesc = "The road widens into a broad grassy walk where flocks have been driven for longer than anyone remembers, with drystone walls leaning at the edges and no hedges at all. Sheep graze in loose groups and look up at every footfall, then down again. The grass is cropped to velvet, and tufts of wool snag the wire along the walls. Thistles stand in the corners where the sheep refuse to go, and a small hut of turf and stone crouches against the wind.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/m05";
        Exits["north"] = "/domains/greenvale/corridor/south_road/m07";
        ResetSeconds = 120;

        AddDetail("sheep,flock,lambs,ewes", "Grey-faced, black-nosed sheep with a stripe of red dye across the shoulder. They stare at you with total sheepish suspicion.");
        AddDetail("walls,wall,drystone,wire", "The drystone walls are built without mortar, lichen-bearded, and topped with a single course of upright stones. A little wool clings to every sharp edge.");
        AddDetail("grass,walk,sheepwalk,road", "The turf is short and springy, and older than the hedges. It was a drove-road before it was a highway.");
        AddDetail("thistles,thistle,corners", "Purple-headed thistles stand around the margins of the walk, tall as a man's chest. The sheep eat everything else.");
        AddDetail("hut,shepherd's hut,turf hut", "A low hut walled with turf and slated with flat stone, a chimney of three stones, and a crook propped by the door. It smells of wet wool and woodsmoke.");
        AddDetail("wool,tufts", "Greasy tufts of wool on every wire. A child could knit a scarf out of the walk's verge if they had the patience.");
        AddNode("/domains/greenvale/corridor/obj/thistle_patch");

        AddSpawn("/domains/greenvale/corridor/south_road/npc/shepherd_bram");
    }
}
