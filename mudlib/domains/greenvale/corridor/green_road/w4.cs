// @inherit /std/room

public class GrTheWebbedGlade : Room
{
    public override void Create()
    {
        ShortDesc = "The Webbed Glade";
        LongDesc = "A hollow in the wood where the light comes through in a dim grey shaft, and every branch, trunk and bush is hung with sheets of grey web. The webs sag with leaves, dust and the husks of things, and glisten with dew. Threads cross the path at every height, and the smell is of damp and old meat. Something rustles overhead and goes still.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/w3";
        Exits["north"] = "/domains/greenvale/corridor/green_road/w5";
        Safe = false;

        AddDetail("webs,web,sheets,silk,threads,strands", "Sheets of grey silk, each as large as a bedsheet, strung between the trees. They tremble when you breathe.");
        AddDetail("husks,husk,bundles,cocoons,cocoon,remains", "Wrapped bundles hang in the webs, shrivelled and light. Some are birds, some are rabbits, and one is bigger and wears the remains of a boot.");
        AddDetail("branches,trees,trunks,canopy", "The trunks are hidden by web, which has crept over bark, roots and ground alike. The tops of the trees are lost in grey.");
        AddDetail("path,trail,ground,leaf litter", "A path of trodden leaf runs through the glade. Somebody cut the webs along it once, and they have been rebuilt, thicker than before.");
        AddDetail("dew,drops,droplets", "Dew beads on every strand. Each drop holds a tiny upside-down glade.");
        AddDetail("flowers,wildflowers,foxglove,foxgloves,herbs,herb,patch", "At the glade's edge, where the webs thin out, foxgloves and wild garlic grow in a patch, and a few herbs besides. Somebody has been picking from it, carefully, with a stick.");

        AddSpawn("/domains/greenvale/corridor/mob/woodland_spider", 2);
    }
}
