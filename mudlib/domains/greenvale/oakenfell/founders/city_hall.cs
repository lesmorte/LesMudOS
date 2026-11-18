// @inherit /std/room

public class FdCityHall : Room
{
    public override void Create()
    {
        ShortDesc = "City Hall";
        LongDesc = "A high, plain hall of pale stone and dark beams, where footsteps carry and voices drop without anyone telling them to. A long oak counter with a brass grille divides the public from the clerks, and behind it pigeonholes full of rolled records climb to the ceiling. On the wall hangs a large painted map of the city and its five districts, and above the inner door a line of carved letters has been picked out in old gilt. The room smells of ink, sealing wax and damp wool, and the queue is never quite empty.";
        Exits["out"] = "/domains/greenvale/oakenfell/founders/square";

        AddDetail("counter,oak counter,grille,brass grille", "The counter is black with age, its edge polished by the elbows of four centuries of petitioners. The brass grille above it has a little brass hatch for passing papers, and a bell no one has ever dared to ring twice.");
        AddDetail("pigeonholes,records,rolls,rolled records,shelves,ledgers", "Thousands of pigeonholes, each with a rolled paper and a tag of brass or bone. The oldest rolls are the color of tea; the lowest holes are labeled in a script that few clerks can still read.");
        AddDetail("map,painted map,city map,districts", "A painted map of Oakenfell, the River Mere curling across its lower right like a blue ribbon. The city is drawn in five colors: the Founder's District in grey at the center, the Market District in yellow to the south, the River District in blue to the east, the Guild Quarter in red to the west, and the Southgate District in green at the very bottom. Beneath it a brass plate says that this map is out of date and the city regrets it.");
        AddDetail("letters,lintel,motto,inner door,door,gilt", "Carved over the inner door, with the gilt rubbed off the high points of each letter, are the words of the city's motto. You could {cmd}read{/} them.");
        AddReadable("letters,lintel,motto,inscription", "Carved over the inner door:\n\"FROM STRONG ROOTS GROW GREAT THINGS\"", "Carved over the inner door, the gilt rubbed away from the high points of each letter. You could {cmd}read{/} them.");
        AddDetail("queue,line,people,petitioners", "A line of petitioners shuffles toward the grille: a woman with a basket of eggs and a boundary dispute, a pair of brothers not speaking to one another, a man carrying a very large and very legal-looking scroll.");
        AddReadable("placard,hours,notice,sign", "On a placard beside the grille:\n\"CITY HALL. Open from the second bell to the sixth. Records of citizens, births, deaths and boundaries are kept within. Fees are posted. The clerks are not to be hurried.\"", "A small brass-framed placard hangs by the grille. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/founders/npc/clerk_ansel");
        AddSpawn("/domains/greenvale/oakenfell/founders/npc/clerk_maude");
    }
}
