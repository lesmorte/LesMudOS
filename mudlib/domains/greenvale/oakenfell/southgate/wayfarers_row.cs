// @inherit /std/room

public class SgWayfarersRow : Room
{
    public override void Create()
    {
        ShortDesc = "Wayfarer's Row";
        LongDesc = "Wayfarer's Row is a wide, cobbled street lined with the cheap boarding houses, wagon yards and shopfronts that serve everyone who has just arrived. Painted signs jostle overhead: a boot, a bed, a bowl, a crossed pair of swords. A porter sits on a trunk by the kerb, and a food seller's smoking cart perfumes the whole street with onions and baking pastry. The Southgate stables lie east, and the Free Lodge hangs its company banner to the west. The street rises gently north toward Stag Lane and the sound of the market.";
        Exits["south"] = "/domains/greenvale/oakenfell/southgate/gate";
        Exits["north"] = "/domains/greenvale/oakenfell/southgate/stag_lane";
        Exits["east"] = "/domains/greenvale/oakenfell/southgate/stables";
        Exits["west"] = "/domains/greenvale/oakenfell/southgate/free_lodge";
        ResetSeconds = 120;

        AddDetail("signs,sign,painted signs", "The signs are hand-painted and bright: a boot for the cobbler, a pillow for a boarding house, a bowl for the cookshop, two crossed swords for the Free Lodge. Several are older than the shops beneath them.");
        AddDetail("food cart,brazier,stove", "The food cart is a brazier and a griddle on iron wheels, with a canvas awning that smells of woodsmoke and hot fat. Pies are stacked on the shelf behind its owner.");
        AddDetail("boarding houses,houses,lodgings,beds", "Narrow houses three or four storeys high, with rooms let by the night or the week to anyone with coin. Their doors stand open, and their landladies lean from windows to watch the street.");
        AddDetail("cobbles,street,kerb", "The cobbles are worn into shallow troughs down the middle, where the carts run, and polished by feet at the edges. Little has changed for a hundred years but the filth.");
        AddDetail("banner,banners,company banner", "Over the Free Lodge door hangs a red-and-black banner showing a coin on a field of black. It has been mended with lighter thread in several places.");
        AddDetail("trunk,porter,luggage", "The porter's trunk is a battered sea-chest, stencilled with the mark of some long-gone carrier's company. He sits on it like a general on a hill.");

        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/street_seller");
        AddSpawn("/domains/greenvale/oakenfell/southgate/npc/porter_joss");
    }
}
