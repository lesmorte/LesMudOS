// @inherit /std/room

public class MkNorthRows : Room
{
    public override void Create()
    {
        ShortDesc = "Greenvale Market, North Rows";
        LongDesc = "The North Rows lie nearer the river, and the stalls here are older and more settled, with carved posts and painted signs that have been in families for generations. In the middle of the lane stands the Market Cross, an old stump of blackened timber banded in iron, said to be a beam salvaged from the Great Oak itself. The air smells of cloth, spices, drying herbs and woodsmoke. To the north the lane widens toward Founder's Square, to the east a bridge arches over the River Mere, and the weaponsmith's stall rings with hammerblows.";
        Exits["south"] = "/domains/greenvale/oakenfell/market/south_rows";
        Exits["north"] = "/domains/greenvale/oakenfell/founders/square";
        Exits["east"] = "/domains/greenvale/oakenfell/river/mere_bridge";
        ResetSeconds = 120;

        AddDetail("market cross,cross,beam,timber,oak beam", "The Market Cross is a squat post of charred oak taller than a man, bound in iron bands and cut with the date of the market's founding. Merchants touch it for luck, and a few place a coin in the cracks.");
        AddDetail("stalls,stall,posts,signs", "The stalls here are carved and painted, with names in gold leaf. These are the old families of the market, and they like to remind you.");
        AddDetail("spices,spice,herbs,drying herbs", "Strings of dried herbs and little cloth bags of spice hang from every awning, and the lane smells like a kitchen and a church at once.");
        AddDetail("weaponsmith's stall,weaponsmith,anvil,hammer", "A weaponsmith's stall with a half-finished blade clamped to a vice, a portable anvil and a rack with nothing on it yet. A notice says new stock is on its way.");
        AddDetail("bridge,mere,river", "To the east the lane drops toward the Mere Bridge, a stone span over the river. You can see barges poling beneath it.");
        AddDetail("herbalist,herbalist's stall,baskets", "A herbalist's stall at the lane's edge is crowded with baskets, bundles and small clay jars. A green cloth hangs over the awning, with an oak leaf worked into the weave.");
        AddReadable("plaque,market cross plaque,inscription", "MARKET CROSS\nRaised from a beam of the Great Oak,\nthe year after the fire.\nHere let honest weights be kept.\nFrom Strong Roots Grow Great Things.", "A bronze plaque is bolted to the Market Cross. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/market/npc/trader_ysolde");
        AddSpawn("/domains/greenvale/oakenfell/market/npc/smith_kessa");
        AddSpawn("/domains/greenvale/oakenfell/market/npc/herbalist_hesper");
    }
}
