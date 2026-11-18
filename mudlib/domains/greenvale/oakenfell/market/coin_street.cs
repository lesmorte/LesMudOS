// @inherit /std/room

public class MkCoinStreet : Room
{
    public override void Create()
    {
        ShortDesc = "Coin Street";
        LongDesc = "Coin Street is quieter than the market, but richer. Stone-fronted storehouses, counting houses and exchanges line both sides, their shutters carved with scales and sacks. The bank stands to the north behind a pair of iron doors, and the Consortium's grand house lies opposite, its banner stirring above the door. A money-changer sits at a table under a canopy, his scales gleaming, and well-dressed clerks hurry past with sealed satchels. The street continues west toward the caravan offices and the Westgate.";
        Exits["east"] = "/domains/greenvale/oakenfell/market/south_rows";
        Exits["north"] = "/domains/greenvale/oakenfell/market/bank";
        Exits["south"] = "/domains/greenvale/oakenfell/market/consortium_house";
        Exits["west"] = "/domains/greenvale/oakenfell/market/coin_street_west";
        ResetSeconds = 120;

        AddDetail("storehouses,houses,counting houses,exchanges,shutters", "Solid buildings of grey stone with iron-bound doors and shutters carved with scales, sacks and sheaves of grain. They have the permanent, slightly smug look of those who are never short of money.");
        AddDetail("bank,iron doors,doors", "The Oakenfell Bank, a squat stone building with a pair of iron doors and a brass plaque. Two armed guards in blue livery stand at the steps.");
        AddDetail("consortium house,banner,house", "A tall house of pale stone, with a blue banner showing a scale and a sheaf. Its windows are glazed, which in this city is a statement.");
        AddDetail("canopy,table,scales,money-changer", "The money-changer sits under a striped canopy with a set of fine brass scales, a locked cash box and a neat row of weights. He looks at coins the way a hawk looks at mice.");
        AddDetail("clerks,satchels,passers-by,passersby,crowd", "Clerks in blue and grey with sealed satchels, a pair of merchants arguing over a figure in low voices, a dockworker counting coins into a purse, a veiled woman with a small locked chest, and two Free Company sellswords eyeing the bank.");
        AddDetail("cobbles,street,paving", "Coin Street is paved in squared grey stone, swept every morning, and the gutters run clean. The market's litter ends at the corner, as if by treaty.");

        AddSpawn("/domains/greenvale/oakenfell/market/npc/changer_lucan");
    }
}
