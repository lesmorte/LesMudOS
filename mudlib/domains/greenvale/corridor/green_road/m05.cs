// @inherit /std/room

public class GrCorduroyCauseway : Room
{
    public override void Create()
    {
        ShortDesc = "The Green Road, the Corduroy Causeway";
        LongDesc = "Here the ground gives way to black bog, and the road becomes a causeway of split logs laid crosswise on a bed of brush, which bounces and groans under every step. The water on either side is brown and still, stained with tannin and skinned with green, and the trunks of drowned trees stand in it like a congregation. Mist lies low across the water and carries a sour smell of rot. The logs are slick with wet and rather worn.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/m04";
        Exits["north"] = "/domains/greenvale/corridor/green_road/m06";
        Safe = false;

        AddDetail("causeway,logs,log,corduroy,road,planks", "Split logs, laid side by side across the road like the ribs of a ladder, and pegged to long stringers underneath. Many have been replaced with newer, paler timber, and some are rotting where they lie.");
        AddDetail("bog,water,mire,marsh,swamp,pool,pools", "Black water with a skin of weed. Where a log has slipped, you can see the bog take hold of it and draw it down, which is a decent warning.");
        AddDetail("mist,fog,haze,vapor", "A low white mist curls above the water, thick enough to hide the edges of the causeway but not the smell.");
        AddDetail("trees,drowned trees,trunks,snags", "The dead trunks stand in the water bleached and barkless, and on a few of them there are scars at head height from other, older travelers' axes.");
        AddDetail("brush,bundles,faggots,stringers,pegs", "Underneath, the causeway is a raft of bundled brush and stakes. Somebody maintains it; fresh-cut stakes lie piled at the verge, waiting.");
        AddDetail("frogs,reeds,rushes,insects", "Reeds crowd the margins. Frogs fall silent as you pass and begin again behind you.");

        AddEncounter("/domains/greenvale/corridor/mob/goblin_raider", 8);
    }
}
