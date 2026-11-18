// @inherit /std/room

public class SrM05 : Room
{
    public override void Create()
    {
        ShortDesc = "The South Road, below Windmill Hill";
        LongDesc = "The road bends to skirt the foot of a long green hill, and the windmill on its crown now looms close, its sails sweeping a wide pale arc with a slow, creaking rhythm you feel in your chest. Gorse blazes yellow on the slope, and a footpath winds up through it toward the mill, worn to bare earth by boots. A stone trough beside the road is fed by a pipe from the hill, and the water is cold and clean. A cart track climbs the hill beside the footpath and a fingerpost stands at the foot of it.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/m04";
        Exits["north"] = "/domains/greenvale/corridor/south_road/m06";
        ResetSeconds = 120;

        AddDetail("hill,slope,windmill hill,crown", "A steep green hill with a flat crown, kept short by sheep. The mill sits on top like a hen on an egg.");
        AddDetail("windmill,mill,sails", "The sails are as long as a cart and a half, canvas stretched on lattices of wood. Their creak comes down the hill with each turn.");
        AddDetail("gorse,whin", "Gorse in full yellow, smelling of coconut and sun. It is armed like a small army, and nothing grazes in it.");
        AddDetail("trough,pipe,water,spring", "A stone trough fed by a lead pipe, the water so cold it makes your teeth ache. The stone is worn into a smooth dip where thousands of hands have cupped it.");
        AddDetail("tracks,cart tracks,footpath,path", "The footpath climbs the hill in tight zigzags, and the cart track beside it is wide enough for a wain. Both go to the mill, and a good deal of the neighborhood's grain with them.");
        AddReadable("signpost,post,fingerpost", "A fingerpost with two arms stands at the foot of the hill. One points up the slope and reads \"MILL\". The other points up the road and reads:\n\"TRAVELER'S REST. HOT FOOD. BEDS.\"\nSomeone has carved a smaller arrow under the second arm and written \"stew\".", "A weathered fingerpost stands at the foot of the hill. You could {cmd}read{/} it.");

        AddEncounter("/domains/greenvale/corridor/mob/grey_fox", 8);
        AddEncounter("/domains/greenvale/corridor/mob/field_rabbit", 6);
    }
}
