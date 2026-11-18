// @inherit /std/room

public class SgStagLane : Room
{
    public override void Create()
    {
        ShortDesc = "Stag Lane";
        LongDesc = "Stag Lane runs north and south between tall, leaning houses whose upper storeys almost meet overhead. On the east side hangs the sign of the Silver Stag, a silver stag on a green board, and warm light spills from its doors even at midday. On the west, a shop with a rope-and-lantern sign advertises supplies for the road. The cobbles are smoother here and the crowd thicker, for this is where the traveling folk of the south meet the market. North, the lane opens into the bustle of the Greenvale Market.";
        Exits["south"] = "/domains/greenvale/oakenfell/southgate/wayfarers_row";
        Exits["north"] = "/domains/greenvale/oakenfell/market/south_rows";
        Exits["east"] = "/domains/greenvale/oakenfell/southgate/silver_stag";
        Exits["west"] = "/domains/greenvale/oakenfell/southgate/outfitter";
        ResetSeconds = 120;

        AddDetail("sign,stag,silver stag,board", "The sign shows a stag with silvered antlers and one forefoot raised, painted on a green board. It has been re-silvered so often that the paint stands in a ridge.");
        AddDetail("houses,upper storeys,jetties,overhang", "The old houses lean together, each storey leaning farther than the one beneath. Washing lines cross the lane from window to window, and children shout from the upper floors.");
        AddDetail("lanterns,lantern,rope", "Rope coils and tin lanterns hang from hooks outside the outfitter's, swaying a little in the draught. Customers walk under them with practiced ducks.");
        AddDetail("cobbles,lane,street", "The cobbles in Stag Lane are laid in fans, newer than the rest of the district, and a gutter runs down the middle. Someone sweeps it each morning, grumbling.");
        AddDetail("crowd,people,travelers,adventurers", "The crowd is a mix: adventurers in mismatched mail, drovers, clerks hurrying south to a customs shed, and a few well-dressed Consortium runners, all in a hurry.");
        AddDetail("noise,market,bustle", "Beyond the end of the lane, the market sounds like a distant sea: voices, wheels, a hammer, a donkey, all rolled together.");

    }
}
