// @inherit /std/room

public class MkSouthRows : Room
{
    public override void Create()
    {
        ShortDesc = "Greenvale Market, South Rows";
        LongDesc = "The South Rows are the loudest part of the biggest market in Greenvale. Canvas awnings in red, yellow and blue stretch over double ranks of stalls, and the lanes between them are packed with shoppers, porters and handcarts. Stallholders bawl their wares, pie-sellers weave through the crowd, and somewhere a dog is chasing a chicken. The smell of cheese, cloth dye, hot iron and horse is thick on the air. An armorer's stall stands at the corner with a bright shield on a hook, and a lane west leads toward the money houses of Coin Street.";
        Exits["south"] = "/domains/greenvale/oakenfell/southgate/stag_lane";
        Exits["north"] = "/domains/greenvale/oakenfell/market/north_rows";
        Exits["west"] = "/domains/greenvale/oakenfell/market/coin_street";
        ResetSeconds = 120;

        AddDetail("stalls,stall,awnings,canvas", "Stalls of timber and canvas stand shoulder to shoulder, each with a painted board and a family's worth of generations behind it. The awnings are patched in every colour and stained by decades of rain.");
        AddDetail("crowd,shoppers,people", "Housewives with baskets, apprentices with errands, ragged children and sellswords all push through the lanes. Everyone is in a hurry and no one is quite sure why.");
        AddDetail("cheese,cloth,pots,wares", "You glimpse wheels of cheese, bolts of cloth, stacks of clay pots, strings of onions, coils of rope and baskets of eels. Greenvale appears to sell everything it grows or makes.");
        AddDetail("armorer's stall,armorer,shield,hook", "The armorer's stall has an empty rack and a single bright shield on a hook, polished until it dazzles. A sign says that new stock is being unloaded.");
        AddDetail("dog,chicken", "A speckled dog has cornered a very indignant hen behind a cheese stall. The hen is winning.");
        AddDetail("lane,west lane,coin street", "A narrow lane leads west toward Coin Street, where the awnings give way to stone and shuttered windows. The noise there is lower, and more expensive.");

        AddSpawn("/domains/greenvale/oakenfell/market/npc/trader_wenna");
        AddSpawn("/domains/greenvale/oakenfell/market/npc/trader_edric");
        AddSpawn("/domains/greenvale/oakenfell/market/npc/armorer_brann");
    }
}
