// @inherit /std/room

public class OaW4 : Room
{
    public override void Create()
    {
        ShortDesc = "The Quarry Track";
        LongDesc = "A heavy-wheeled track of ruts and rubble climbs through the trees toward a pale scar in the hillside, where stone for the city was once cut. Half-dressed blocks litter the verge, abandoned when the wagons stopped coming, and the green spoil heaps of the old workings are overgrown with bramble and briar. The ruts are deep, the stones are loose, and from the quarry itself comes the clink of something that is not a mason's hammer.";
        Exits["south"] = "/domains/greenvale/corridor/approach/w3";
        Exits["north"] = "/domains/greenvale/corridor/approach/w5";
        ResetSeconds = 120;

        AddDetail("quarry,pale scar,scar,hillside,quarry face,workings", "The quarry face is a pale grey cliff, sheer and scored with wedge-marks, its floor a shallow pool of green water. The stone cut here made the courses of the city wall.");
        AddDetail("blocks,half-dressed blocks,stones,rubble,dressed stone", "Half-squared blocks sit where the wagons left them, their faces chiselled smooth on one side and rough on the others. A mason's mark, the oak, is cut small in each corner.");
        AddDetail("ruts,track,wheels,wagon ruts", "The ruts are the deepest on the road, worn by decades of overloaded wagons. Rainwater has made small canals of them.");
        AddDetail("spoil,spoil heaps,heaps,bramble,briar,brambles,berries", "Bramble has taken the spoil heaps and made them its own; the berries are fat and black, if you can reach them past the thorns. Wasps work the ripest.");
        AddDetail("pool,green water,water,quarry pool", "A pool of green, still water lies at the foot of the face, deep and cold. Its surface is dead calm, and a little silt cloud rises now and then from the bottom.");
        AddDetail("hammer,sound,clink,clinking,noise", "Somewhere in the cutting a hard, patient clink answers every few moments, as if someone were sharpening a blade on stone. It stops when you stop to listen.");

        AddSpawn("/domains/greenvale/corridor/mob/rogue_goblin_veteran");
    }
}
