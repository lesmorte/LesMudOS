// @inherit /std/room

public class FdNorthgate : Room
{
    public override void Create()
    {
        ShortDesc = "Northgate";
        LongDesc = "Northgate is a squat grey gatehouse of dressed stone, its arch wide enough for two carts and its iron-bound doors folded back against the walls. Two round towers flank the arch, with arrow slits on their faces and the green banner of the city on a staff above. The street runs on under the arch and out into daylight and the long road beyond, where wagon ruts climb toward a skyline of distant mountains. A watchman stands in the arch with a spear, watching the road and the people on it with equal and unhurried attention.";
        Exits["south"] = "/domains/greenvale/oakenfell/founders/old_stone_street";
        Exits["north"] = "/domains/greenvale/oakenfell/beyond/northern_road";

        AddDetail("gatehouse,gate,arch,gates,doors,iron-bound doors", "A broad arch of dressed grey stone with iron-bound oak doors folded back against the walls. The hinges are as thick as a man's forearm; the doors have not been shut in anyone's memory except once, the old watchmen say, for a whole day, by mistake.");
        AddDetail("towers,tower,round towers,slits,arrow slits", "Two squat round towers, their faces pierced by narrow arrow slits and their tops crowned by a ring of stone teeth. A ladder in each is rumored to lead somewhere. The watch has no interest in saying where.");
        AddDetail("banner,flag,staff,green banner", "A green banner worked with a single oak hangs from a staff above the arch. It is faded by the sun and well mended.");
        AddDetail("road,northern road,ruts,mountains,skyline", "Beyond the arch the Northern Road climbs between fields and low walls, rutted by wagons, toward a blue-grey line of mountains on the horizon.");
        AddDetail("spear,watchman", "The watchman leans on a long spear and wears a green surcoat marked with the oak of Oakenfell. He looks at everyone who passes, and nobody is certain what he sees.");
        AddReadable("notice,gate notice,sign,board", "A painted board is bolted to the wall of the arch:\n\"NORTHGATE. Open from dawn bell to dusk bell. Citizens and the watch are not to be molested. Carts keep to the right. The road beyond is the Northern Road; the watch cannot answer for it.\"", "A painted board is bolted to the wall of the arch. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/founders/npc/watch_northgate");
    }
}
