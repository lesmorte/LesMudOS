// @inherit /std/room

public class HwM02 : Room
{
    public override void Create()
    {
        ShortDesc = "The Fallen Arch";
        LongDesc = "Two tall piers of dark stone stand on either side of the road, and between them lies what was once their arch, broken into heavy wedge-shaped blocks sunk in moss. The stone is a green-grey that no wall in the region shares, and the blocks are fitted so closely that you cannot slide a knife blade into the joints that remain. The road runs through the gap as though the arch had never been there. Cold comes off the stone even at noon.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/m01";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/m03";
        DangerBand(7, 8);

        AddDetail("piers,pier,pillars,pillar,uprights", "Each pier stands about three times the height of a man and is much too narrow for its height: a cart could pass between them, but the stone suggests a door for something taller and thinner than any cart driver. Their faces are smooth, without mortar, and the top of each is cut to a slope where the arch began.");
        AddDetail("arch,blocks,block,wedges,wedge,stones,rubble,fallen arch,voussoirs", "Eleven blocks lie where they fell, and a twelfth stands cracked on the east pier. They are cut to a pointed curve, narrow at the crown and sharp at the peak, unlike the round arches of Oakenfell. The undersides are smooth. Moss has grown over everything else, and has grown thick enough to bury a man's arm.");
        AddDetail("joints,joint,masonry,fitting,fit,stone", "No mortar was used. Each stone has been shaped to its neighbour, and where the joints are open, they are narrower than a coin. Whoever built it did not guess the weight of the arch; they knew it. The stone is not local. There is no quarry within a week's walk that gives this green-grey.");
        AddDetail("carving,carvings,band,pattern,lines,interlaced", "A band of carving runs round each pier at the height of a man's chest. It is a pattern of interlaced lines, with a long loop, a short one and a cross-stroke, repeated and varied. You could {cmd}read{/} it, though it may not be writing.");
        AddDetail("moss,green,lichen", "The moss lies on the north faces in a deep soft fur, and on the south in a thin crust. It has eaten the sharp edges of the carving on the lower blocks, but not those on the piers above its reach.");
        AddDetail("road,gap,ground,way", "The road goes through the gap between the piers without turning. Its dirt is the same as before, which suggests that the road, when it came here, found a thing already standing and went through it.");
        AddDetail("cold,air,chill", "The air is a few degrees colder between the piers than on either side of them. The chill does not come from the shade.");
        AddReadable("carving,carvings,band,pattern,lines,interlaced", "You trace the carved band with a finger. The strokes are cut deep and clean, and they repeat in groups of seven before a stroke changes, and then begin again. It looks like writing, or like a border, and nothing about it matches the marks on any stone, stall or coin that you know. Near the base of the east pier the pattern breaks off, and the last few groups are worn flat by weather.", "A band of interlaced carving runs round each pier. You could {cmd}read{/} it, though it may not be writing.");
        AddEncounter("/domains/greenvale/corridor/mob/heartwood_goblin", 6);
    }
}
