// @inherit /std/room

public class FbLongGallery : Room
{
    public override void Create()
    {
        ShortDesc = "The Long Gallery";
        LongDesc = "The stair gives out into a gallery so long that your light shows neither end of it: paving, a pointed vault, and then more dark. Carving runs along both walls, and the tall figures walk in two files toward the north, their raised arms beginning, one by one, to come down. Two doorways face one another halfway along, east and west, and between them and along the walls stand low benches of the same stone that nobody ever sat on. The air is so dry that your own breath is the only sound.";
        Exits["up"] = "/domains/greenvale/corridor/barrow/descending_way";
        Exits["north"] = "/domains/greenvale/corridor/barrow/guardian_hall";
        Exits["east"] = "/domains/greenvale/corridor/barrow/bowl_tomb";
        Exits["west"] = "/domains/greenvale/corridor/barrow/inscribed_tomb";

        AddDetail("gallery,paving,floor,flags,slabs", "The paving is laid in long and short slabs that repeat every seventh course, as on the road in the wood, and it is not worn into a trough as that is. Nothing walked here in numbers. The slabs are so true that a dropped coin would not roll.");
        AddDetail("vault,roof,ceiling,arch,dark", "The vault is lost in the dark, and the walls seem to lean toward one another as they rise. Here and there a faint sheen on the stone shows where the lamplight has found a facet, and then gone.");
        AddDetail("benches,bench,seats,ledges", "The benches are single blocks, knee-high, set in the foot of the wall at equal spaces. Their tops are flat and clean and bear no mark of a body or a bundle. They are too narrow to sit on.");
        AddDetail("doorways,doorway,doors,east,west,east doorway,west doorway", "The two doorways are tall and narrow, with pointed heads, and they face each other exactly across the width of the gallery. The east one leads to a chamber with a bier in it. The west one has a panel of carving beside it, and a dark drift on the floor before the threshold.");
        AddReadable("band,carving,carvings,frieze,figures,figure,procession,pattern,border,files,arms", "Forty-two figures walk north along the two walls, and you count them, because the count is so plainly meant. In each group of seven one figure has let its raised arm fall, and the group after it has two, and so on, until at the northern end of the gallery there are no raised arms at all, and the figures walk with their hands open at their sides. Their faces are blank to the last. The border above them runs on, not yet closed.", "Two files of carved figures walk north along the walls. You could {cmd}read{/} the band.");
        AddReadable("slip,scrap,paper,note,scrap of paper,fold", "A scrap of paper lies in the dust at the foot of the west wall, where it must have fallen from a sleeve. It is written in a quick, small, modern hand, in pencil:\n\n\"Gallery 42, hall 7. The count is as it should be. Seven bowls east. Rubbing done, second sheet spoiled, third sheet clean. Far door: do not go through it without the others. You were right about the cold.\"\n\nThere is no name.", "A scrap of paper lies in the dust at the foot of the wall. You could {cmd}read{/} it.");
        AddSpawn("/domains/greenvale/corridor/mob/restless_spirit", 1);
    }
}
