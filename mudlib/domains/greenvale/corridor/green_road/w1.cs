// @inherit /std/room

public class GrThePedlarsPath : Room
{
    public override void Create()
    {
        ShortDesc = "The Pedlars' Path";
        LongDesc = "A narrow path wanders off west of the camp between the trunks, worn smooth by pedlars and tinkers who prefer not to pay the camp fee. The trees crowd in on both sides and the path winds around roots and fallen limbs. Charms hang from a few low boughs: bits of tin, a feather, a twist of red wool. The sounds of the camp fade quickly behind you.";
        Exits["east"] = "/domains/greenvale/corridor/green_road/m03";
        Exits["north"] = "/domains/greenvale/corridor/green_road/w2";
        Safe = false;
        DangerBand(7, 8);

        AddDetail("path,narrow path,trail,pedlars path", "A foot-wide path, twisting and worn, with the earth packed hard. Pedlars have walked it long enough to set stones in the boggy places.");
        AddDetail("charms,charm,tin,feather,wool,boughs", "Scraps hung on twine from the low branches: a bent tin whistle, a jay feather, a bit of red wool. Pedlars hang them for luck, and one or two look much older than the rest.");
        AddDetail("roots,root,limbs,fallen limbs,logs", "A grey tangle across the path. You have to step high and watch your boots.");
        AddDetail("trees,wood,forest,trunks", "Hornbeam and beech, closely set, with ivy winding up the trunks. The path barely fits between them.");
        AddDetail("herbs,herb,plants,patch,mint,sorrel,comfrey", "Along the path grows a patch of herbs, mint and sorrel and tall comfrey with bell flowers. Pedlars gather it as they walk.");
        AddDetail("flowers,wildflowers,bluebells,campion", "A few wildflowers hold on in the thin light, pale campion and a scatter of late bluebells.");

        AddNode("/domains/greenvale/corridor/obj/herb_patch");
        AddNode("/domains/greenvale/corridor/obj/wildflowers");
    }
}
