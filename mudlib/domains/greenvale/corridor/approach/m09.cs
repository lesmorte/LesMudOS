// @inherit /std/room

public class OaM09 : Room
{
    public override void Create()
    {
        ShortDesc = "Southfarm Hamlet, Mill Lane";
        LongDesc = "The road runs on past the last cottages and a stone mill stands beside it, its great wheel turning slowly in a leat of dark, quick water. Sacks of flour are piled by the door, white dust lies on everything like a fall of early snow, and the whole lane thrums with the deep, steady knock of the millstones. Hens scratch in the verge, an orchard wall runs beside the lane, and to the north the road begins at last to tilt upward toward a ridge you cannot see over.";
        Exits["south"] = "/domains/greenvale/corridor/approach/m08";
        Exits["north"] = "/domains/greenvale/corridor/approach/m10";
        ResetSeconds = 120;

        AddDetail("mill,stone mill,watermill,millstones", "A tall, narrow mill of grey stone with a slate roof and a hoist beam jutting from the loft. Inside, through the open lower door, you can see the stones grinding, and hear them, more than anything else.");
        AddDetail("wheel,mill wheel,waterwheel,leat,water", "The wheel is oak, black with water, and turns with a long wet sigh at each stroke. The leat that feeds it runs fast and cold from a stream above, and eventually joins the Mere.");
        AddDetail("sacks,flour,flour sacks,dust", "The sacks are stencilled with the mill's mark, a wheatsheaf over a wheel, and stacked in tidy ranks for the carter. Dust whitens the sleeve of anyone who leans on them.");
        AddDetail("hens,chickens,verge,chicken", "A dozen speckled hens work through the verge and pay no attention to wheels, hooves or people. One has made a nest in the wheelwright's discarded barrel.");
        AddDetail("orchard,orchard wall,wall,apples,apple trees", "A low wall of stacked field stone encloses an orchard of old apple trees, heavy with fruit and wasps. The windfalls ferment sweetly in the grass.");
        AddDetail("ridge,north,slope,hill,tilt", "The lane runs uphill beyond the mill, climbing between hedges to a skyline that is just a line of grass and sky. Something about the quality of the light above it suggests there is more beyond.");
        AddReadable("mill sign,sign,board,mill board", "SOUTHFARM MILL, flour ground to the Oakenfell weight\nCarters loading for the city: leave your tally with the miller's wife.\nNo dogs in the mill, no children in the wheel.", "A painted sign hangs over the mill door. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/approach/npc/carter");
    }
}
