// @inherit /std/room

public class MkCaravanOffice : Room
{
    public override void Create()
    {
        ShortDesc = "The Caravan Office";
        LongDesc = "The Caravan Office is a long, low room smelling of oiled leather, axle grease and strong tea. A great slate board covers one wall, chalked with routes, rates and departure days, and a table below it is spread with maps, ledgers and tin mugs. Through a back window you can see the wagon yard, where teams are being hitched and loads checked. Several tough-looking guards loaf by the door, and a stern woman with a bunch of keys at her belt shouts orders to a boy who runs between them.";
        Exits["north"] = "/domains/greenvale/oakenfell/market/coin_street_west";
        ResetSeconds = 120;

        AddDetail("board,slate board,routes,rates,departures", "The slate board lists destinations in chalk: Watchfort Crossing, the mountain passes, the river towns, the Elderwood road. Against several, in a different hand, is chalked: HELD.");
        AddDetail("table,maps,ledgers,mugs", "A table spread with route maps held down by tin mugs. Someone has drawn a rude face on the Elderwood.");
        AddDetail("window,wagon yard,yard,wagons", "Through the dusty window you see wagons being loaded under the eyes of foremen with slates. Each load is checked twice.");
        AddDetail("guards,caravan guards,door", "Caravan guards in boiled leather, with cudgels and crossbows, sit on a bench and play cards. They have the careful, over-casual look of men who have been paid to be ready.");
        AddDetail("keys,boy,runner", "The boy runs between the yard, the office and the Consortium House with chits. He has not stopped moving since you came in.");

        AddSpawn("/domains/greenvale/oakenfell/market/npc/caravan_master_dorrin");
    }
}
