// @inherit /std/room

public class FbBorrowedTomb : Room
{
    public override void Create()
    {
        ShortDesc = "The Borrowed Tomb";
        LongDesc = "The great door has been drawn back just far enough to pass, and beyond it lies a tomb larger than any other, its bier dragged crosswise into a corner to clear the floor. Where it stood is a camp for one: a flat stone for a seat, a litter of rust and rag, and a ring of old soot on the paving, and against the wall a split shield. The carving on these walls is the finest in the barrow, and has been scraped at in places with something that had an edge. The cold is deeper here, and still; it has the smell of old iron.";
        Exits["south"] = "/domains/greenvale/corridor/barrow/guardian_hall";
        ResetSeconds = 600;

        AddDetail("bier,corner,rock,slab", "The bier is a block of the same green-grey stone, cut to be fixed, and torn from its footing and levered across the floor with an iron bar, the marks of which score the paving. What it was made to hold is nowhere to be seen. The room was cleared for a living man.");
        AddDetail("camp,seat,flat stone,stone,soot,ring,fire,hearth", "The seat is a block from the broken bier, placed to face the door. The soot ring is the size of a platter and long cold, with a handful of pale splinters in it that were once a stool or a chest. A man sat here a long while, and watched the door.");
        AddDetail("door,slab,bar,socket,inner face,inside,scratches", "On the inner face of the great door is a deep socket cut in the stone, at the height of a man's hip, and a bar of oak lies in two rotted halves at its foot. The oak does not belong to the barrow; it was carried in and cut to fit. The door was meant to be shut from this side, and the floor beneath it is scored in long arcs, as of something that was pushed shut, and then pushed open.");
        AddDetail("shield,device,emblem,paint,arms,leaning", "A shield leans against the wall, split down the middle and gone to red flakes. On its face the paint has all but vanished: a ghost of a pale shape, a bar, perhaps a beast, and nothing a herald could name. Whoever he served, the paint has not kept the name.");
        AddDetail("remains,rag,rags,cloth,leather,buckles,buckle,pack,purse,scatter,rust,iron", "A heap of rot and rust lies by the seat: the leather of a pack gone to black paper, a few buckles, plates of corroded harness of a make that has been out of fashion for two or three hundred years, the iron flaking to the touch. A purse lies split among them, with its coins spilled, and a cloak pin shows dull silver among the buckles.");
        AddDetail("pin,cloak pin,brooch,clasp,thorn,silver", "Among the buckles lies a cloak pin of blackened silver, the one thing here that has not rotted. It is made as a short straight stem with three barbs, like a twig of thorn. You have seen that shape cut into standing stones in the wood above.");
        AddReadable("walls,wall,carving,carvings,panels,scraped,scrape,scrapes,pattern", "The carving here is cut with a precision that outdoes every other room, interlaced lines so fine that they make a grey shimmer of the stone. In six places on the nearest wall someone has tried to scrape it away, or to dig between the strokes with a knife, and has made no impression beyond a few bright scratches. The scratches are old, and the knife has long since rusted. The pattern, here too, ends in groups of seven.", "Carving covers the walls, scratched at in places. You could {cmd}read{/} it.");
        AddSpawn("/domains/greenvale/corridor/mob/fallen_knight", 1);
        AddSpawn("/domains/greenvale/corridor/barrow/obj/tomb_coins");
    }
}
