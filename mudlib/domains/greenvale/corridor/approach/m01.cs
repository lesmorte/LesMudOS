// @inherit /std/room

public class OaM01 : Room
{
    public override void Create()
    {
        ShortDesc = "The Approach Road, the Forest's Edge";
        LongDesc = "The great trunks of the Heartwood fall behind you one by one, and the trees on either hand grow younger and farther apart until daylight lies across the road in wide bright bars. The air smells of cut grass and sap instead of moss and rot, and from somewhere ahead comes the ring of iron tyres on stone. Pale stumps show where woodcutters have been at work, and the road, after days of earth and root, has a line of edging stones set along the verge.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/m11";
        Exits["north"] = "/domains/greenvale/corridor/approach/m02";
        ResetSeconds = 120;

        AddDetail("stumps,stump,woodcutters,felled oaks", "The stumps are broad as tables and cut clean, the sawn faces still yellow. Someone fells oaks here by licence and with care: every stump has a notched tag nailed to its top.");
        AddDetail("edging stones,stones,verge,kerb", "Squared grey stones, each a stride long, bed the road's edge like the first teeth of a new mouth. They are plain work, and recently laid, though the road beneath them is ancient.");
        AddDetail("light,daylight,sunlight,sky", "After the green gloom of the old wood the sky is almost startling, a wide pale blue with larks stitched across it. You find you are walking with your head tipped back.");
        AddDetail("heartwood,forest,trees,trunks,wood", "Behind you the Heartwood stands like a dark wall, its great trunks pressed shoulder to shoulder. From here you can see how old it is, and how it ends, quite suddenly, at the line of the felling.");
        AddDetail("larks,birds,lark", "Larks climb out of the verge grass and hang singing above the road. You have not heard them since the farmland of the south.");
        AddDetail("ruts,wheel ruts,tracks", "Wheel ruts, two hands deep, run down both sides of the road, but the first flat stones of paving are beginning to show in the middle where the earth has been scraped away.");
        AddReadable("notice,riverwatch notice,sign,tree", "RIVERWATCH RANGERS, NOTICE TO TRAVELERS\nThe road to Oakenfell is watched from the Toll House northward. Beyond the verge, in the western woods, it is not.\nTravelers who meet armed men on the road are to report them at the Greenvale Toll House.\nEver Vigilant.", "A weathered notice is nailed to a tree at the edge of the verge. You could {cmd}read{/} it.");

        AddEncounter("/domains/greenvale/corridor/mob/highway_bandit", 8);
    }
}
