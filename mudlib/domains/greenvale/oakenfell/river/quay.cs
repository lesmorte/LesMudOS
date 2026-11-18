// @inherit /std/room

public class RvQuay : Room
{
    public override void Create()
    {
        ShortDesc = "The Mere Quay";
        LongDesc = "The Mere Quay is a long timber-and-stone landing where the River Mere laps against iron rings and green weed, and barges ride three deep at the bollards. Dockhands trundle barrels and bales up planks, a crane creaks round on its post, and the harbormaster's chalked board stands at the head of the steps. The air tastes of river-water, tar, rope and the sweet smell of wet timber. A low stone building with a brass-bound door and a painted scale on its sign stands to the east, and the quay runs on to the north toward the fish market and the water gate.";
        Exits["west"] = "/domains/greenvale/oakenfell/river/old_river_street";
        Exits["south"] = "/domains/greenvale/oakenfell/river/mere_bridge";
        Exits["east"] = "/domains/greenvale/oakenfell/river/trade_office";
        Exits["north"] = "/domains/greenvale/oakenfell/river/quay_north";

        AddDetail("quay,landing,planks,planking,timber,stone,floor", "The quay is built of black oak planking on stone piers, scored by chains and dented by dropped barrels. Rain-darkened boards give a little underfoot, and the river laps below them.");
        AddDetail("river,mere,river mere,water,weed,rings,iron rings,bollards,bollard", "The River Mere is wide and brown here, moving slow. Iron rings and stout bollards line the quay's edge, rubbed bright by ropes, and the green weed on the stone shows the level of the high water.");
        AddDetail("barges,barge,boats,boat,ships", "Flat-bottomed river barges, riding low with cargo and painted with the colors of their owners, lie three deep at the quay. Their crews sleep in tiny cabins at the stern and wake at dawn to curse the same cargo.");
        AddDetail("crane,post,hoist,cranes", "A timber crane with a long jib and a drum of chain stands on a swivel post. Two men on the treadwheel walk slowly nowhere, and the crane lifts a barrel from a barge to the quay.");
        AddDetail("dockhands,dockhand,barrels,bales,crates,cargo", "Barrels of salt fish, bales of wool and crates of crockery go up the planks on shoulders and handcarts. Everything is marked in chalk or paint with a number and the name of its owner.");
        AddDetail("building,trade office,brass-bound door,door,sign,scale", "A low stone building to the east, its brass-bound door standing open and its sign painted with a pair of scales over a barge. This is the River Trade Office.");
        AddReadable("board,chalked board,harbormaster's board,harbour board,harbor board,arrivals", "The harbormaster's board reads, in a cramped chalk hand:\n\"ARRIVED: Heron (barley, Corvane), Good Intent (wool), two lighters (stone).\nDUE: Marigold (salt), Three Sisters (hides).\nPIERS 1-3 FULL. Do not tie to the crane post.\nPASSAGE: none today.\"", "A large chalked board stands at the head of the steps. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/river/npc/corm_teague");
        AddSpawn("/domains/greenvale/oakenfell/river/npc/brit_halloran");
        AddSpawn("/domains/greenvale/oakenfell/river/npc/harbormaster_isolde");
    }
}
