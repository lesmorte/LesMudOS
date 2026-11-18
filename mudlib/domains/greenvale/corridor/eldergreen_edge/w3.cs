// @inherit /std/room

public class EeDeerLawn : Room
{
    public override void Create()
    {
        ShortDesc = "The Deer Lawn";
        LongDesc = "The wood opens into a natural lawn of short, close-cropped grass, kept low by generations of grazing deer, with a scatter of ancient oaks standing about its edges like guests at a gathering. Sun pours on to the grass and the air is warm and smells of clover and crushed thyme. Here the forest feels almost kind, the deer's own garden. Hoofprints scar the soft earth near a little spring, and at the lawn's edge a line of old paths enters the trees from several directions.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/w2";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/w4";
        Exits["east"] = "/domains/greenvale/corridor/eldergreen_edge/d1";
        ResetSeconds = 120;

        AddDetail("lawn,grass,clover,thyme,meadow", "The grass is cropped as short as a bowling green, and thick with clover, daisies and creeping thyme that releases its scent underfoot.");
        AddDetail("oaks,oak,trees,edge", "The oaks stand well back from one another around the lawn, each with a wide skirt of bare earth where the deer have lain beneath them.");
        AddDetail("spring,water,pool,seep", "A small spring seeps from a mossy bank into a shallow pool trampled by hooves. The water is cold, sweet, and clear to the pebbles.");
        AddDetail("herbs,herb patch,patch,betony,sorrel,marjoram", "On the sunny bank above the spring grow thyme, betony and wild marjoram in thick patches. A herb-gatherer would find a good morning's work here.");
        AddDetail("hoofprints,prints,tracks,deer tracks", "The tracks are everywhere, the neat cloven hooves of roe deer, with now and then a fawn's, tiny and sharp. The wolves, you notice, have walked round the edge of the lawn and not across it.");
        AddDetail("paths,path,trails,game trails", "Narrow deer paths enter the trees at every point of the compass. Only one, the broader to the east, has been trodden by anything heavier than a deer.");

        AddSpawn("/domains/greenvale/corridor/mob/roe_deer", 2);
    }
}
