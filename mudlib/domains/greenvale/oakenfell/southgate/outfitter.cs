// @inherit /std/room

public class SgOutfitter : Room
{
    public override void Create()
    {
        ShortDesc = "The Outfitter";
        LongDesc = "Shelves climb to the ceiling of this narrow shop, crammed with all a traveler could want: coils of rope, tin lanterns, canvas packs, flint and steel, oilcloth, blankets and boots. A glass-fronted cabinet near the counter holds neat rows of potion vials, and a barrel of wrapped trail rations stands by the door. The air smells of tar, wax and leather. A brass bell on the door announces each customer, and the proprietor has the unhurried manner of a person who has seen every kind of traveler in a hurry.";
        Exits["east"] = "/domains/greenvale/oakenfell/southgate/stag_lane";
        ResetSeconds = 120;

        AddDetail("shelves,shelf,goods,supplies", "The shelves hold everything an adventurer forgets until the road: needles, twine, chalk, candles, waterskins, spare bootlaces. Each is priced on a little ticket in a neat hand.");
        AddDetail("rope,ropes,coils", "Coils of hempen rope, thick as a thumb and fifty feet to a coil, hang in neat rows from a beam.");
        AddDetail("lanterns,lantern,tin lanterns", "Tin lanterns in three sizes, each with a horn window and a little brass hook.");
        AddDetail("cabinet,vials,potions,glass cabinet", "The cabinet holds rows of ruby vials, labelled in a tidy hand. The key hangs on the shopkeeper's belt.");
        AddDetail("barrel,rations,trail rations", "A barrel of paper-wrapped trail rations stands by the door, with a hand-lettered price on a slate.");
        AddDetail("bell,door,brass bell", "The brass bell hangs on a curl of iron above the door. It has a flat, cheerful note.");
        AddDetail("counter,ledger", "A worn oak counter, scarred where heavy sacks have been dropped on it for years. A ledger and a brass scale rest on it.");

        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/outfitter_perrin");
    }
}
