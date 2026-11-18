// @inherit /std/room

public class MkAuctionHouse : Room
{
    public override void Create()
    {
        ShortDesc = "The Auction House";
        LongDesc = "The auction hall is a tall, empty theatre of tiered benches facing a raised block, with a carved gavel on a stand. Dust drifts in the light from clerestory windows, and the smell of beeswax and old wood hangs in the air. A bulletin board by the door lists lots from past sales, and a few clerks are tallying numbers in a side room. Between sales the place feels like a church waiting for the congregation, and the auctioneer, a florid man in a plum coat, rehearses a chant to the empty benches.";
        Exits["south"] = "/domains/greenvale/oakenfell/market/coin_street_west";
        ResetSeconds = 120;

        AddDetail("benches,tiers,seats", "Rows of padded benches climb in steep tiers, each with a numbered brass plate. Regular buyers have their names on them.");
        AddDetail("block,platform,dais,gavel,stand", "The auctioneer's block is a low wooden platform with a heavy gavel on a carved stand, its head worn glossy.");
        AddDetail("windows,clerestory,light", "High windows of clear glass throw slanting bars of light across the benches.");
        AddDetail("clerks,side room", "Through an open door you glimpse clerks bent over ledgers of lots, hammer prices and buyers' names.");
        AddDetail("lots,past sales,board", "The board lists lots from earlier auctions: a signet ring, two barrels of Elderwood oak, a racehorse, a small cannon. Prices are crossed through in red.");
        AddReadable("notice,sign,bulletin,bulletin board", "THE OAKENFELL AUCTION HOUSE\nSales are not yet open to the public.\nConsign your goods to the clerk when the books open.\nBids are binding. Reserves are honest.", "A notice by the door says something about sales. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/market/npc/auctioneer_corbin");
    }
}
