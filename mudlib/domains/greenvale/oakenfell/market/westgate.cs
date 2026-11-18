// @inherit /std/room

public class MkWestgate : Room
{
    public override void Create()
    {
        ShortDesc = "Westgate";
        LongDesc = "Westgate is a squat gatehouse of weathered stone, its great oaken doors standing open to a road that runs away into the hazy west. Watchmen in grey tabards check the carts that come and go, and a queue of heavy wagons waits for the toll. The road beyond is lined with oaks and, far off, the darker line of the Elderwood stands against the sky. A sign beside the gate warns that travel beyond is at the traveler's own risk, and the watch looks unusually grim.";
        Exits["east"] = "/domains/greenvale/oakenfell/market/coin_street_west";
        Exits["west"] = "/domains/greenvale/oakenfell/beyond/westwood_road";
        ResetSeconds = 120;

        AddDetail("gatehouse,gate,doors,oaken doors", "The gatehouse is lower and older than Southgate's arch, and its doors, forty feet of black oak, are studded with iron. They are open, but the hinges are oiled and ready.");
        AddDetail("watchmen,watch,tabards", "The watch here wear their tabards over mail and carry crossbows as well as spears. They look at every wagon twice.");
        AddDetail("road,west,westwood road,elderwood", "The road runs west among scattered farms and stands of oak toward a line of darker trees. That is the Elderwood, and the road passes into its shadow.");
        AddDetail("queue,wagons", "Heavy wagons laden with timber, wool and barrels wait for the toll-keeper. Several are fresh from the Elderwood, with scorch marks on their canvas.");
        AddDetail("toll,toll-keeper,tollbooth", "The toll-keeper sits in a small booth with a cash box, his face a study in indifference.");
        AddReadable("sign,warning,warning sign", "WESTGATE\nThe western road is held by the Watch and the Riverwatch.\nTravelers beyond this point go at their own risk.\nCarts to leave by the Westgate must be registered at the Caravan Office.", "A large sign beside the gate carries a warning. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/market/npc/watch_hugh");
    }
}
