// @inherit /std/room

/// <summary>The Shrine of the Quiet Hand: Sister Merewyn's shrine. A healer's blessing for coin, and the Shrine-Blessed Mace quest. The Priest's rank trainer belongs here later.</summary>
public class VhShrine : Room
{
    public override void Create()
    {
        ShortDesc = "The Shrine of the Quiet Hand";
        LongDesc = "A single small room of grey fieldstone, cool and almost silent. At the far end a carved stone hand rises from the wall, palm up, and a shallow basin of still water rests in its palm. Rows of little candles burn on the sills, and a few are guttering. A rush mat covers the floor, and a worn offering bowl stands on a stand by the door with a few small coins in it. A notice about the blessing hangs beside the bowl. The quiet here feels deliberate, like a held breath.";
        Exits["out"] = "/domains/hollow/village/fletchers_lane";
        Safe = true;

        AddDetail("hand,stone hand,carved hand,palm", "A hand of grey stone, as long as your arm, carved palm up with the fingers loose and a little curled. It does not ask for anything. That is, you gather, rather the point.");
        AddDetail("basin,water,still water", "A shallow basin carved into the palm of the stone hand, brimming with water so still that you can see the candle flames upside down in it. Nobody has ever seen it spill.");
        AddDetail("candles,candle,sills,sill,flames", "Dozens of little candles, white and unadorned, burn on the sills. The ones on the north side lean the same way, away from the wall, as if there were a draught that does not touch the others.");
        AddDetail("mat,rush mat,floor", "A rush mat, much patched, with a shallow hollow worn into it where people kneel.");
        AddDetail("bowl,offering bowl,stand,coins", "A worn pewter bowl on a stand by the door, a handful of small coins in it. The pilgrims who used to leave offerings at the old cellar door left their silver here instead when the cellar turned dangerous.");
        AddReadable("notice,blessing notice,board", "The notice reads:\n  \"The Quiet Hand asks nothing, and gives what it can.\n  A blessing for those who are weary or hurt: six gold in the bowl, and ask Sister Merewyn. {cmd}buy blessing{/}.\n  The Sister has a small errand for those with a little time: {cmd}ask{/} her about the offering.\"", "A small notice hangs beside the offering bowl. You could {cmd}read{/} it.");
        AddReadable("names,scratched names,north sill,sill names,scratches",
            "Under the north sill, scratched small with a pin by many hands:\n" +
            "  \"YSOLT BRANNOCK. The Spirit to sit with the sick.\"\n" +
            "  \"OLD MERRIT. His Resolve, in the bad year. He did not ask the Hand for anything.\"\n" +
            "  \"A CHILD'S NAME, and beneath it: she was brave.\"",
            "Names are scratched into the stone under the north sill. You could {cmd}read{/} them.");

        AddSpawn("/domains/hollow/npc/sister_merewyn");
    }
}
