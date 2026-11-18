// @inherit /std/room

public class OaW3 : Room
{
    public override void Create()
    {
        ShortDesc = "The Bandit Camp";
        LongDesc = "A hollow in the woods has been cleared of undergrowth and ringed with sharpened stakes, and in its middle stands a litter of patched tents around a smoking fire. Barrels, stolen crates and bundles of cloth are stacked under a tarpaulin, a deer carcass hangs from a branch, and a rough lookout post of lashed poles overlooks the south track. This is where the road ends for the unwary, and the men sitting round the fire know it; they look up without hurry and reach for their weapons.";
        Exits["south"] = "/domains/greenvale/corridor/approach/w2";
        Exits["north"] = "/domains/greenvale/corridor/approach/w4";
        Exits["west"] = "/domains/greenvale/corridor/approach/d1";
        ResetSeconds = 120;

        AddDetail("tents,tent,patched tents,camp,bedrolls", "A dozen patched tents in three shades of grey, pitched round a trampled space. The largest is slightly apart and has a real door-flap, a chair outside it, and a cracked mirror hung from a pole.");
        AddDetail("fire,firepit,fire pit,cooking,pot", "A shallow pit, ringed with stones, with an iron pot bubbling on a tripod. The stew has gone grey; at its edge somebody's trophy, a Riverwatch badge, has been nailed to a post.");
        AddDetail("crates,barrels,bundles,cloth,loot,tarpaulin,plunder", "Merchants' marks are stencilled on the crates, and some have been stamped over with a rough black cross. Bolts of fine cloth, casks, a box of candles, and one very good saddle: the road's patience on show.");
        AddDetail("stakes,palisade,fence,sharpened stakes", "A ring of fire-hardened stakes, angled outward and chest-high. They are not meant to stop an army, only to make a charge awkward and a retreat loud.");
        AddDetail("lookout,lookout post,poles,post,platform", "A platform of poles lashed in a tree-fork, with a ladder, high enough to watch both the south track and the glint of the aqueduct. A horn hangs from its rail.");
        AddDetail("carcass,deer,game,branch", "A fat roe deer hangs from a branch, gutted and skinned, still dripping. The bandits eat well.");
        AddReadable("ledger,tally,tally board,board,toll book", "Scratched in charcoal on a board, in a crabbed hand:\nTHE TOLL BOOK\n Wool, 40 bales, Row 2, the 15th of each month: let go, escort.\n Salt wagons, 6, no escort: take.\n Consortium clerk with the cash chest: pays on the Rise. Not yet.\n Do not touch the Rangers' signal post. Ever.\nMonthly share to the Boss, paid in kind.", "A charcoal tally has been scratched on a board by the largest tent. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/mob/bandit_leader");
        AddSpawn("/domains/greenvale/corridor/mob/highway_bandit", 3);
    }
}
