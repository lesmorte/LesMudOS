// @inherit /std/room

public class OaM03 : Room
{
    public override void Create()
    {
        ShortDesc = "The Greenvale Toll House";
        LongDesc = "A squat two-storey toll house of grey stone straddles the verge, with a painted pole barrier across the road and a green banner hanging from its gable. Wagons wait in a patient line before the pole while a clerk's window clicks open and shut, and the air is full of the smell of tar, wet rope and horse. A rutted track strikes off west between the hedgerows, away from the paving, toward a line of tall broken stonework half-hidden by the trees.";
        Exits["south"] = "/domains/greenvale/corridor/approach/m02";
        Exits["north"] = "/domains/greenvale/corridor/approach/m04";
        Exits["west"] = "/domains/greenvale/corridor/approach/w1";
        ResetSeconds = 120;

        AddDetail("toll house,house,building,gable", "The toll house is built of the same grey stone as the city's wall, with a slate roof and a chimney that smokes in all weathers. Over the door a carved oak leaf shows that it is the Assembly's.");
        AddDetail("barrier,pole,pole barrier,gate", "A long pole, painted in bands of green and white, turns on a weighted pivot. A rope runs from its tail to the window, and the toll keeper lifts it with a tug when he is satisfied.");
        AddDetail("banner,green banner,flag,oak", "The banner is green with a white oak, and below the oak, in white thread, run the words PROSPERITY THROUGH UNITY. It is clean, new, and the best-kept thing in sight.");
        AddDetail("window,clerk's window,ledger", "A shuttered hatch in the front wall, with a counter of worn oak. Behind it you can glimpse a great ledger propped open and a row of brass weights on a shelf.");
        AddDetail("wagons,line,queue,carts", "Six wagons wait, nose to tail, their drivers asleep on the boards or arguing about weights. The wheels have cut the paving here deeper than anywhere on the road.");
        AddDetail("track,west track,path,hedgerows,broken stonework", "A muddy track leaves the road beside the toll house, with a stake driven into the verge and a rag tied to it. Down the track, above the hedgerows, you can see the tops of tall stone shapes that are not any building you know.");
        AddReadable("toll board,board,tolls,rates,toll", "GREENVALE TOLL HOUSE, by order of the Assembly\nFor the upkeep of the road and the safety of its travelers.\n Handcart, barrow or pack-beast: 1 gold\n Wagon, empty: 3 gold\n Wagon, laden: 5 gold\n Herd, per ten head: 2 gold\n Riders and walkers: free\nHolders of the Greenvale Travel Pass pass without question.\nPROSPERITY THROUGH UNITY", "A painted board hangs on the wall by the window. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/approach/npc/toll_keeper");
        AddSpawn("/domains/greenvale/corridor/approach/npc/toll_guard");
    }
}
