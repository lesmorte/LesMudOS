// @inherit /std/room

public class RvEastgate : Room
{
    public override void Create()
    {
        ShortDesc = "Eastgate";
        LongDesc = "Eastgate is a pair of stubby round towers joined by a broad arch, with a pointed portcullis raised above it and a green banner on a staff between the battlements. Beneath the arch the road passes out through the city wall, and the wagon ruts that begin here run away to the east between fields. Carters wait in a short queue for the watch to wave them on, and travelers in dusty cloaks come in from the road with a look of arrival. A watchman leans on the wall by the arch, examining every wagon with a patient and practiced eye.";
        Exits["west"] = "/domains/greenvale/oakenfell/river/mere_bridge";
        Exits["east"] = "/domains/greenvale/oakenfell/beyond/crossing_road";

        AddDetail("towers,tower,round towers,battlements,staff", "Two round towers of grey stone, with arrow slits and a ring of stone teeth along each top. The green banner flaps from a staff between them.");
        AddDetail("arch,gate,portcullis,gates", "A broad arch wide enough for two wagons abreast, with a raised iron portcullis above it, its spikes crusted with rust. The gate stands open day and night, but the watch can drop it if asked.");
        AddDetail("road,crossing road,ruts,wagon ruts,east,fields", "Beyond the arch the Crossing Road runs on east between fields, deeply rutted by the wagons that come and go from the east.");
        AddDetail("carters,carter,queue,wagons,wagon,travelers", "A short line of carters waits patiently, hats pulled down against the sun, while a pair of dusty travelers hurry in from the road looking for a drink.");
        AddReadable("notice,gate notice,sign,board", "A painted board by the arch reads:\n\"EASTGATE. Open from dawn bell to dusk bell. Carts will be inspected. The road beyond is the Crossing Road, to Watchfort Crossing; the watch cannot answer for it. Travelers are advised to travel in company.\"", "A painted board by the arch. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/river/npc/watch_eastgate");
    }
}
