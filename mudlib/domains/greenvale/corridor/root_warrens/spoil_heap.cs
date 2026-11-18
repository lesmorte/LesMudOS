// @inherit /std/room

public class RwSpoilHeap : Room
{
    public override void Create()
    {
        ShortDesc = "The Spoil Heap";
        LongDesc = "The tunnel opens into a long chamber that the diggers used to dump what they dug, and the dump has risen in a slope of clay and broken stone almost to the roof. A barrow lies on its side at the foot of it with its wheel rotted away. Pale fungus has colonised the heap in shelves and drifts, and the whole chamber smells of mould and earth and a sourness behind them. The heap has been burrowed through in a hundred places, and fresh earth lies spilled at the mouth of each hole. Something in the heap shifts as you step in.";
        Exits["east"] = "/domains/greenvale/corridor/root_warrens/junction";
        Exits["south"] = "/domains/greenvale/corridor/root_warrens/clay_cut";
        ResetSeconds = 300;
        DangerBand(7, 8);

        AddDetail("heap,spoil,slope,stone,stones,clay,rubble", "The heap is a dull mix of yellow clay, grey stones and rotted roots, dumped barrow by barrow over a long time. The top is crowned with a smooth crust where the fungus has taken it, and the sides are cratered with burrows.");
        AddDetail("fungus,fungi,shelves,shelf,drifts", "The fungus grows in pale shelves the colour of old candle-wax, soft and damp to the touch and faintly sweet-smelling. It is thickest where the clay is dampest. It would be worth carrying away, if one knew what it was good for.");
        AddDetail("holes,hole,burrows,burrow,earth,tracks", "Each hole is the width of a man's head and wide at the mouth, with a smear of dung inside. Fresh earth is spilled before them in tongues. They run deeper than a hand can reach, and all of them slope downward.");
        AddDetail("barrow,wheel,cart", "It is a stout wooden barrow bound with iron, lying where it was dropped. The wheel is gone and the bed has been gnawed through at one corner, as though the rats had wanted whatever was last in it.");
        AddDetail("roof,ceiling,timbers", "A line of timbers runs across the roof, each cracked, and one has been chewed so thin that it bows. Dust falls from it whenever the heap shifts.");
        AddDetail("sourness,smell,air,mould", "The smell is of old earth and mould, and under it of a close warm den. The air is thicker here, and it clings to your throat.");

        AddSpawn("/domains/greenvale/corridor/mob/tunnel_rat", 2);
    }
}
