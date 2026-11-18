// @inherit /std/room

public class RvTradeOffice : Room
{
    public override void Create()
    {
        ShortDesc = "The River Trade Office";
        LongDesc = "A long, low room with a flagged floor and a wooden counter, where the business of the River Mere is done in ink. Manifests, bills of lading and rate cards are pinned in rows on the walls, and a great brass pair of scales stands at one end of the counter beside a bell for service. A faded chart of the river hangs above the clerks' desks, its upper end curling off the wall. The room smells of paper, wax and damp wool, and the quiet scratch of pens is broken now and then by someone sighing over a figure.";
        Exits["west"] = "/domains/greenvale/oakenfell/river/quay";

        AddDetail("counter,wooden counter,bell,service bell", "A long counter of dark wood, scarred by bales and elbows. A brass bell sits on it with a card reading 'Ring once'.");
        AddDetail("manifests,manifest,bills,lading,papers,rows,walls", "Rows of manifests and bills of lading are pinned to the walls in tidy columns: cargoes, weights, ports and signatures. They read like a language, and the clerks speak it fluently.");
        AddDetail("scales,scale,brass scales,weights", "A great pair of brass scales, with a pan big enough to hold a sack of grain and a case of iron weights on a shelf beside it. They carry the stamp of the city, and a certificate says they were last tested in a year no clerk will admit to.");
        AddDetail("chart,river chart,river,map,mere", "A faded chart of the River Mere, with Oakenfell in the middle, the upper end curling off the wall. Little inked arrows mark the shoals, and a note in the margin reads 'Mind the Hag'.");
        AddReadable("rates,rate card,rate cards,card,notice,board", "A card on the wall reads:\n\"RIVER TRADE OFFICE. Freight by barge at the posted rates, by weight and distance. Bills of lading stamped while you wait. Passage for travelers: NOT YET OFFERED. Enquire later.\"", "A neatly lettered rate card hangs by the counter. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/river/npc/lenna_quill");
        AddSpawn("/domains/greenvale/oakenfell/river/npc/giles_pardew");
    }
}
