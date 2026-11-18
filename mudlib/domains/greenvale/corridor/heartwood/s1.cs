// @inherit /std/room

public class HwS1 : Room
{
    public override void Create()
    {
        ShortDesc = "The Hidden Cache";
        LongDesc = "A cleft among the old stones, walled by the thorn on the one side and by the base of a very old wall on the other, so low that you must stoop to enter. The air is dry and bitter and smells of burning. The floor is dusted with black flakes, and in the corner stands a stack of books and papers that were set alight and not allowed to finish: their edges are charred to lace, and a satchel of scorched leather lies on top. Someone came here with a purpose, and then took pains that the purpose should not be known.";
        Exits["west"] = "/domains/greenvale/corridor/heartwood/d2";
        ResetSeconds = 600;

        AddDetail("cleft,cache,hollow,stones,wall,thorn,roof", "The cleft is no wider than a table, roofed with a slab and a tangle of thorn, so that you would pass it a hundred times without knowing. The stones of the wall at its back are of the dark green-grey and are the same as the ruins on the road.");
        AddDetail("flakes,black,char,charcoal,floor,dust,cinders,cinder,soot", "The black flakes on the floor are the remains of pages, crumbling at the touch and leaving a smear on the fingers. The cinders in the corner have been raked into a heap and then not lit.");
        AddDetail("books,book,papers,paper,stack,journals,journal,notebooks,notes", "The stack is of five or six bound journals and a bundle of loose sheets, charred to a crust at the edges and soaked since by damp, so that the fire stopped and was not put out. Most of the pages have fused into a block. A few, near the middle, can still be read in part: you could {cmd}read first journal{/}, {cmd}read second journal{/} or {cmd}read third journal{/}.");
        AddDetail("satchel,bag,leather,scorched satchel", "The satchel is a plain leather bag with a buckled flap, scorched black on one side and whole on the other. It is empty except for a few flakes, and it has no mark, no name and no maker's stamp. The wearer was careful.");
        AddDetail("markings,marks,symbol,symbols,scratches", "There is nothing on the walls. No sign, no initial. The only mark in the cleft is on the floor, where the cinders were scraped into a heap by a boot and then spread out again.");
        AddDetail("thorn", "On this side the thorn has grown through the roof of the cleft and has been cut back from the entrance, neatly, and recently enough that the cut ends are still pale.");
        AddReadable("first journal,first,journal one,first book,1", "The paper is burned away on every side. What remains reads:\n\n\"...third day in the court. The lowest course is as the old report said, and no one at the road-station has asked what I am cutting for. I have given out that I draw for the Athenaeum. [burned] ...should anyone come again I will say the same. Keep the sketches in the other book, and this one on me...\"", "A journal, charred at the edges. You could {cmd}read{/} the first journal.");
        AddReadable("second journal,second,journal two,second book,2", "Half the page is gone. What remains reads:\n\n\"[burned] ...nothing under the arch, nor in the court, nor beneath the spring-stone. It was not set down where they said. If it was moved, it was moved before any of us. [burned] ...hollow trunks big enough to hide a cart; I will go through the thorn on the east side tomorrow. Burn the maps when they are copied. No one is to know that we were ever here...\"", "A journal, charred at the edges. You could {cmd}read{/} the second journal.");
        AddReadable("third journal,third,journal three,third book,3", "The last book was thrown on the heap, and the fire reached only the cover. The final page reads:\n\n\"...the shrine in the deep wood is shut from within or fallen. I do not like what the cold air smells of. [burned] ...if it lies below, it is for those who come after me, and they will have better tools. Leave nothing. This book goes into the fire with the rest of--\"\n\nThe line stops there, in the middle of a stroke.", "A journal, charred at the edges. You could {cmd}read{/} the third journal.");
        AddSpawn("/domains/greenvale/corridor/heartwood/obj/scorched_satchel");
    }
}
