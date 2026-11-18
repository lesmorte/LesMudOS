// @inherit /std/room

public class FdOldStoneStreet : Room
{
    public override void Create()
    {
        ShortDesc = "Old Stone Street";
        LongDesc = "Old Stone Street climbs gently north from the square between tall, narrow houses of grey stone, their upper floors leaning companionably over the roadway. The paving is the original street, great flat slabs rutted by wheels that stopped turning here generations ago, and iron lamp brackets stick out from the walls at every second door. Window boxes and brass door-knockers mark the houses of old families who do not say how old. A stone bench by a doorway has been worn to the exact shape of a great many sitters, and a narrow lane runs off to the west between the oldest houses.";
        Exits["south"] = "/domains/greenvale/oakenfell/founders/square";
        Exits["west"] = "/domains/greenvale/oakenfell/founders/elder_lane";
        Exits["north"] = "/domains/greenvale/oakenfell/founders/northgate";

        AddDetail("street,paving,slabs,stones,road,ruts", "Wide slabs of grey stone, laid before anyone thought to ask the street to be level. Wheels wore two shallow troughs down the middle long ago, and the troughs still shine in the rain.");
        AddDetail("houses,house,homes,upper floors,fronts", "Narrow, deep houses of grey stone with steep roofs and many chimneys, each a little different from the next because each was rebuilt at a different time by a different family. The oldest have stones in their lower courses that are black with age and look as though they were laid before the street was.");
        AddDetail("lamp brackets,brackets,lamps,iron brackets", "Curled iron brackets jut from the walls, each holding a glass lamp. The lamps are lit at dusk by a man with a long pole, and every bracket has a tiny oak leaf worked into its scroll.");
        AddDetail("window boxes,boxes,flowers,knockers,door-knockers,doors", "Window boxes of herbs and late flowers brighten the sober fronts, and every door has a brass knocker polished by hand. One of the knockers is a little cast acorn; another is an oak leaf.");
        AddDetail("bench,stone bench,doorway", "A stone bench set against a wall by a doorstep, its seat worn into a shallow dish by the people who have sat in it. It is evidently in constant use by someone who has opinions on the street.");
        AddDetail("lane,narrow lane,elder lane,west", "A narrow lane ducks off to the west between two of the oldest houses, so low and quiet that it looks more like an alley than a street. Its mouth is shaded by the overhang of the houses.");
        AddReadable("plaque,house plaque,brass plaque,small plaque", "A small brass plaque is bolted beside one of the doors:\n\"Here dwelt Edda Marsh, bellfounder, who cast the first bell of the town. This house was raised in the second generation after the founding and rebuilt after the Burning.\"", "A small brass plaque is bolted beside one of the doorways. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/founders/npc/jory_vance");
        AddSpawn("/domains/greenvale/oakenfell/founders/npc/widow_perrin");
    }
}
