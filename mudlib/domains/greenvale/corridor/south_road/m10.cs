// @inherit /std/room

public class SrM10 : Room
{
    public override void Create()
    {
        ShortDesc = "The Stone Bridge";
        LongDesc = "The road climbs onto a bridge of old grey stone, three arches wide, that carries it high over a broad slow river. Green water moves beneath, dark and deep under the middle arch, and reeds line both banks where herons stand fishing. Wind plays along the parapet, which is worn smooth where generations of travelers have leaned to look at the water. The far bank is wooded now, dark and close, and the hedge-and-fence country of the farms is plainly behind you. A Riverwatch standard flutters from a pole at the north end.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/m09";
        Exits["north"] = "/domains/greenvale/corridor/south_road/m11";
        ResetSeconds = 120;

        AddDetail("bridge,arches,arch,stone,stonework", "Three round arches of dressed stone, mossed above the waterline and polished below. The mason who built them took trouble over every voussoir.");
        AddDetail("river,water,current,green water", "A wide green river, slow and deep, with weed streaming in it like hair. A trout turns in the shadow of the middle arch.");
        AddDetail("reeds,herons,heron,banks", "Grey herons stand knee deep in the reeds, motionless, patient as bailiffs. One lifts into the air with a creak of wings.");
        AddDetail("parapet,wall,coping", "The parapet is waist high and worn to a polish along its top edge. Carved on the coping are a hundred names and initials, some very old.");
        AddDetail("standard,flag,pole,pennant,riverwatch standard", "A green-and-grey pennant bearing a silver eye. It snaps in the wind from the river.");
        AddDetail("far bank,trees,wood,woods", "Beyond the bridge the road enters shadow under tall trees. It looks cooler there, and quieter.");
        AddReadable("plaque,stone plaque,inscription", "A slab of slate is set into the parapet at the midpoint, its letters cut deep:\n\"THE STONE BRIDGE. RAISED BY THE FREE HANDS OF THE VALE, THAT THE ROAD MIGHT KEEP THE TRAVELER, AND THE TRAVELER THE ROAD.\"\nBeneath, small and sharp: \"Ever Vigilant.\"", "A slate plaque is set into the parapet. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/south_road/npc/fisherman_cuthbert");
        AddSpawn("/domains/greenvale/corridor/south_road/npc/ranger_mildred");
    }
}
