// @inherit /std/room

public class VrApproach : Room
{
    public override void Create()
    {
        ShortDesc = "The Southgate Approach";
        LongDesc = "The road widens here to a broad, paved approach below the city wall, churned by wheels and crowded with wagons, drovers and travelers queuing for the gate. Hawkers call from the verge, selling ribbons, pins and last-chance charms for the road. The wall rises above you, sixty courses of weathered stone, and the Southgate arch yawns in its foot with a warm mutter of voices and horses coming from within. Dung and dust, woodsmoke and frying onions: the city smells like itself already.";
        Exits["south"] = "/domains/greenvale/vale_road/rise";
        Exits["north"] = "/domains/greenvale/oakenfell/southgate/gate";
        ResetSeconds = 120;

        AddDetail("wall,city wall,stone", "The wall is built of big grey blocks, each as long as a man, laid in tight courses. Some are scored with old marks, masons' signs from the first builders.");
        AddDetail("arch,gate,southgate", "Ahead the Southgate arch opens in the wall, wide enough for two wagons abreast. Words are cut across its crown, but you cannot read them from here.");
        AddDetail("wagons,carts,drovers,queue", "A queue of wagons stands before the gate, loaded with sacks, barrels and crated hens. The drovers curse their oxen with the weary affection of long habit.");
        AddDetail("hawkers,ribbons,charms,verge", "The hawkers along the verge sell tin charms, coloured ribbons and little oak-leaf pins that you are assured bring luck on the road. They keep to the verge and call out as you pass.");
        AddDetail("travelers,people", "Pilgrims, farmers, peddlers and a troop of sellswords with bedrolls all wait together, glancing at one another with sidelong interest. Nobody pushes; the watch is close.");

    }
}
