// @inherit /std/room

public class OaW1 : Room
{
    public override void Create()
    {
        ShortDesc = "The Old Aqueduct, the Broken Span";
        LongDesc = "The track ends beneath the ruin of a great arcade of stone, where a span of the Old Aqueduct has fallen and lies in tumbled blocks across a bed of bracken. The two ends of the break still reach into the air, a long way over your head, like the stumps of an enormous broken bridge. Ferns and elder grow in the fallen stone, jackdaws wheel and shout above, and the whole place is silent in the way of things that have been silent for a very long time.";
        Exits["east"] = "/domains/greenvale/corridor/approach/m03";
        Exits["north"] = "/domains/greenvale/corridor/approach/w2";
        ResetSeconds = 120;
        DangerBand(8, 9);

        AddDetail("span,broken span,break,ends,arcade", "Two great stumps of masonry reach out from the standing piers and stop in empty air. Looking along the line, you can count seven arches before the ruin ends, and trace where the rest used to march.");
        AddDetail("blocks,fallen blocks,stone,rubble,tumble", "The fallen blocks lie where they fell, each as big as a cart, half sunk in the earth, dark green-grey under their fur of moss. None of them has a chisel mark, and the faces are as smooth as river stones.");
        AddDetail("channel,conduit,top,water course,trough", "Along the top of the standing arches you can see the channel the water ran in, a deep stone trough open to the sky, full now of fern and rain. It runs north-east and out of sight, toward the city, and then, seemingly, nowhere.");
        AddDetail("ferns,fern,bracken,herbs,plants,elder", "Hart's-tongue fern and elder fill the gaps, and where the soil is deep, clumps of herbs have taken hold, feverfew and yarrow and wild thyme. Someone has trodden a path through them, and not so very long ago.");
        AddDetail("circle,pale stones,stones,seven stones,ring", "Around the foot of one standing pier lie seven pale stones, laid in an exact ring. No ivy touches them, and the grass is cropped short around them, as if by something that respects the circle. If they mean anything, nobody here can tell you what.");
        AddDetail("jackdaws,birds,jackdaw", "Jackdaws live in the high joints of the ruin, and a loud colony it is. They watch you with pale eyes, the way a guard watches a stranger.");

        AddNode("/domains/greenvale/corridor/obj/herb_patch");
    }
}
