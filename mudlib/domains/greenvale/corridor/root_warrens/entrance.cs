// @inherit /std/room

public class RwEntrance : Room
{
    public override void Create()
    {
        ShortDesc = "Beneath the Root Plate";
        LongDesc = "You are in the hollow the great tree tore out of the ground, where the roots hang down overhead like the ribs of a wrecked ship and the daylight comes in green and thin from above. The floor is trodden to hard bare clay by small feet, and at the back it slopes into a narrow mouth cut square into the earth, far too even to be a burrow. A slow draught breathes out of it, cool and rank, smelling of wet stone and old nests. Someone shored the mouth with split timbers a long time ago, and the timbers have been chewed. Behind you the roots make a rough stair back up to the trail.";
        Exits["up"] = "/domains/greenvale/corridor/heartwood/w2";
        Exits["down"] = "/domains/greenvale/corridor/root_warrens/root_stair";
        DangerBand(7, 8);

        AddDetail("roots,root,ribs,stair,curtain", "The roots hang in cords as thick as a man's leg, and they curve back up into the underside of the plate. Where you came down, the biggest of them have been trodden and cut into a rough stair, and the cuts are old and square. Someone made this way to be used.");
        AddDetail("mouth,opening,tunnel,entry,dark", "The mouth is the width of two men and the height of one and a half, with sides and a lintel that are plainly cut with picks. It was not dug by a fox or a badger. Beyond the first few paces it turns downward into black.");
        AddDetail("timbers,timber,props,posts,shoring,beam", "Split oak posts hold up the lintel on either side, grey with age. Their lower ends are scored with rows of small deep grooves, and one has been gnawed more than half through and now leans on its fellow for support.");
        AddDetail("draught,draft,air,smell,wind", "The draught is steady, cool and slightly damp, and it never changes direction. It smells of earth, of rot, and of the sour musk of a crowded burrow, and underneath that of something hot and living, a long way down.");
        AddDetail("floor,clay,ground,tracks,prints,feet", "The clay of the floor has been beaten smooth and hard. Small prints of hard feet and of clawed pads cross each other thickly, mostly leading in. A few are broader and flat-heeled, and those are old and half filled with dust.");
        AddDetail("daylight,light,sky,green,above", "The light that falls through the roots is the green of the forest canopy, and it ends a few paces in, as if a door had been shut on it.");
    }

    // Tells the Chronicle that a player has come here. A character is credited once, so every visit may report. The dungeon is
    // reported as lying beneath Heartwood, the forest above it.
    public override void Init()
    {
        base.Init();
        var who = ThisPlayer();
        if (who == null || !Interactive(who)) return;
        try
        {
            LoadObject("/secure/events").Call<string>("Report", "discover.secret_dungeon", who, new List<ObjectRef> { who }, Self, "root-warrens",
                new Dictionary<string, string> { ["name"] = "The Root Warrens", ["area"] = "Heartwood" });
        }
        catch (MudRuntimeException) { }
    }
}
