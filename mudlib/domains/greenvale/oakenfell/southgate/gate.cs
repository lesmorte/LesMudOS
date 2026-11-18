// @inherit /std/room

public class SgGate : Room
{
    public override void Create()
    {
        ShortDesc = "Southgate";
        LongDesc = "The Southgate arch is a tunnel of dressed stone, forty feet deep, ringing with hooves and the echoes of voices. Lamps burn in iron cages along its walls even at noon, and a pair of watchmen in grey tabards stand on either side of the inner mouth, looking over everyone who passes. Above the northern end of the arch, carved deep in the keystone, is the city's motto. Beyond the arch you glimpse a noisy, crowded street, bright with signs, and smell horses, hot pies and woodsmoke.";
        Exits["south"] = "/domains/greenvale/vale_road/approach";
        Exits["north"] = "/domains/greenvale/oakenfell/southgate/wayfarers_row";
        ResetSeconds = 120;

        AddDetail("arch,tunnel,gate,southgate", "The tunnel is old, its stones black with the smoke of centuries, and its vault is hung with the iron rings and slots of portcullises no one has lowered in living memory. Lamps along the walls make a warm, even light.");
        AddDetail("watchmen,watch,tabards", "The Oakenfell watch wear grey tabards over mail, with a small green oak badge on the left breast. They are armed but easy, and every face that passes is weighed with a glance.");
        AddDetail("keystone,motto,carving,oak", "The keystone is cut with a spreading oak, and beneath it, in letters a hand deep, the city's motto: FROM STRONG ROOTS GROW GREAT THINGS. The letters are worn smooth in places by centuries of weather and by the hands of travelers who touch them for luck.");
        AddDetail("lamps,lamp,cages", "Iron cages hold oil lamps along the tunnel walls. Soot traces long black fingers up the stone above each one.");
        AddDetail("portcullis,rings,slots", "Deep grooves in the walls once held a portcullis. The rings that raised it hang rusted and unused, and the watch say the gate has not been shut in war since before their grandfathers.");
        AddDetail("hooves,traffic,echoes", "Hooves, wheels and a hundred voices chase each other up and down the vault until they blur into a single warm roar.");
        AddReadable("board,rules,notice board,notices,city rules", "THE CITY OF OAKENFELL\nBy order of the Greenvale Assembly and the Watch:\n 1. Travelers declare goods for sale or carriage at the gate, and pay the Consortium's toll.\n 2. Carts keep to the middle of the street, and riders walk their horses under the arch.\n 3. Drawn steel in a crowded street will be answered by the Watch.\n 4. Debts, once owed, are collected by the courts and not by the sword.\n 5. The Watch may be found at every gate and is to be asked before it is cursed.\nFrom Strong Roots Grow Great Things.", "A large board of city rules is bolted to the wall by the gate. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/watch_hobb");
        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/watch_dagna");
    }
}
