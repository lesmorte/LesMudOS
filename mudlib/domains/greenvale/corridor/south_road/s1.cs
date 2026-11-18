// @inherit /std/room

public class SrS1 : Room
{
    public override void Create()
    {
        ShortDesc = "The Dry Well";
        LongDesc = "A tiny round clearing opens behind the brambles, walled in by thorn and floored with soft dead leaves. In the middle stands the stone ring of a well-head, waist high, its bucket-frame long rotted away, and the shaft inside is bone dry. Beside it a farmstead's old hearthstone lies sunk in the earth, and the air smells of earth, stone and long forgotten things. Someone once rested here with his back to the well, and left something behind in a niche of the stones.";
        Exits["east"] = "/domains/greenvale/corridor/south_road/d2";
        ResetSeconds = 120;

        AddDetail("well,well-head,ring,shaft,stones", "A ring of fitted stone, mossed to the lip, over a shaft that drops away into dark. When you drop a pebble in, it takes a long time to land, and the sound is dry.");
        AddDetail("hearthstone,hearth,farmstead,foundation", "A broad flat stone worn hollow by feet, the last trace of a farmhouse that stood here before the brambles. The soil around it still shows a square of nettles where the walls were.");
        AddDetail("leaves,floor,clearing,thorn,walls", "A snug, hidden room of thorn. Nothing has been through it for years, and the quiet is complete.");
        AddDetail("niche,hollow,alcove,cache", "A niche in the stones has been closed with a flat slab, and then the slab pushed aside. Someone who knew the well was here put their treasures where only they could reach them.");
        AddDetail("satchel,leather,pouch,bag,bones", "A rotted satchel lies against the stones, its leather gone black. Whoever carried it never came back for it.");
        AddReadable("note,tablet,scrap,wax tablet,wax,paper", "Pressed in the wax of a small tablet, in a hurried hand:\n\"Hale's Acre failed the winter the well went dry. We carried what we could to the Hollow and left the rest to the thorn. I hid the purse in the well where no stranger would think to look, for the road is long and the wood is not what it was. If I do not come back for it, let it feed someone who is hungrier than I am. - M.\"", "A wax tablet lies tucked in the niche. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/south_road/obj/old_purse");   // a find: each character takes it once, for less each time
    }

    // Tells the Chronicle that a player has come here. A character is credited once, so every visit may report.
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        try
        {
            LoadObject("/secure/events").Call<string>("Report", "discover.hidden", who, new List<ObjectRef> { who }, Self, "dry-well",
                new Dictionary<string, string> { ["name"] = "the Dry Well" });
        }
        catch (MudRuntimeException) { }
    }
}
