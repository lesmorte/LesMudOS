// @inherit /std/room

public class HwD3 : Room
{
    public override void Create()
    {
        ShortDesc = "The Collapsed Shrine";
        LongDesc = "A small stone shrine has fallen in on itself at the end of the trail, and the broken blocks lie piled where the roof came down. Two walls still stand to the height of a man, built of the dark green-grey stone, and between them lies a heavy lintel, cracked in two and tilted against the rubble. The trail ends here: it was never meant to go further. Behind the fallen lintel is a gap between two leaning slabs, black and narrow, from which a cold air comes steadily. Moss and fern grow over the rest, and the silence is thick.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/d2";
        Exits["west"] = "/domains/greenvale/corridor/heartwood/w5";

        AddDetail("shrine,ruin,ruins,building,walls,wall", "The shrine was square, and small, no bigger than a cottage room. Two walls stand, and the roof has come in between them. The stone of the walls is the same green-grey as the arch on the road, cut and fitted in the same way, and there is no mark on the outside at all.");
        AddDetail("lintel,beam,cracked,crack,stone", "The lintel is a single stone as long as a wagon, broken across its middle and tilted. Under the moss on its upper face are the last few groups of a carved line, in the interlaced pattern of the arch.");
        AddDetail("gap,slabs,slab,leaning,behind,black,dark,opening,crack,entrance", "Behind the lintel two slabs lean together and leave a gap about the width of a man's shoulders. It slopes down into darkness. The cold that comes out of it smells of dry stone and old air, and the moss has not grown on the lip. It is not a place that you would see from the main trail.");
        AddDetail("rubble,blocks,block,pile,roof", "The blocks of the roof lie heaped, some on their sides, some on their faces. One has been cut into a shallow bowl and another into a low step. All of them are very heavy, and none has been moved in a long time.");
        AddDetail("trail,path,end,south,west", "The trail that brought you here is narrow and does not look used. It was cut by animals, then widened by one or two with a blade, then lost to the thorn again. West it returns toward the Stalker's Run.");
        AddDetail("moss,fern,ferns,silence,quiet", "The moss on the rubble is thick and unbroken, except along the lip of the gap, where it has been rubbed away. There is no sound at all, not even the rustle of leaves.");
        AddSpawn("/domains/greenvale/corridor/mob/forest_stalker", 1);
        AddSpawn("/domains/greenvale/corridor/mob/thorn_woodling", 1);
    }
}
