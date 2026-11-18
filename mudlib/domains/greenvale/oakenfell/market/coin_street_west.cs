// @inherit /std/room

public class MkCoinStreetWest : Room
{
    public override void Create()
    {
        ShortDesc = "Coin Street, West End";
        LongDesc = "At its western end Coin Street loses its polish and grows busy with wheels. Wagon yards, warehouses and teamsters' taverns crowd the way, and the cobbles are scored by iron tires. Barrels, crates and bales are stacked in the street, and drovers curse and shout at their teams. The auction house stands to the north, its doors closed between sales, and the caravan office to the south, its yard crowded with wagons being loaded. A short way west the street ends at the Westgate.";
        Exits["east"] = "/domains/greenvale/oakenfell/market/coin_street";
        Exits["north"] = "/domains/greenvale/oakenfell/market/auction_house";
        Exits["south"] = "/domains/greenvale/oakenfell/market/caravan_office";
        Exits["west"] = "/domains/greenvale/oakenfell/market/westgate";
        ResetSeconds = 120;

        AddDetail("wagons,wagon yards,wagon,yards", "Wagon after wagon is being loaded, patched, painted or unhitched. Each bears the Consortium's blue scale, or the plainer mark of an independent carter.");
        AddDetail("warehouses,crates,barrels,bales", "Stacks of goods from all over Greenvale: Elderwood timber, river fish in brine, wool from the hill farms, glass from the mountain towns.");
        AddDetail("drovers,teamsters,teams,oxen", "Drovers swear at their oxen in affectionate and inventive ways. The oxen take no notice.");
        AddDetail("auction house,auction,doors", "The auction house is a tall brick building with a gallery and a carved gavel over the door. The doors are shut, with a notice nailed to them.");
        AddDetail("caravan office,office,yard", "A long, low office, with a stableyard behind it and a board of destinations under the eaves.");
        AddDetail("cobbles,tires,street", "The cobbles are scored with long grooves where wheels have run for centuries, and filled with mud and straw.");

    }
}
