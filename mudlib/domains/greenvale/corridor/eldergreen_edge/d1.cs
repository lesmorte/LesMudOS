// @inherit /std/room

public class EeBoarWallow : Room
{
    public override void Create()
    {
        ShortDesc = "The Boar Wallow";
        LongDesc = "A broad hollow in the wood has been churned into a stinking black bog by generations of wallowing boar, the mud still wet and steaming in the shade. Every tree trunk about it is rubbed smooth and scored with tusks to a height of three feet, and the ground is rooted up in great ploughed furrows. The smell is rank and musky, and from the thickets around comes a grunting and snuffling that stops when you stop. The trees here are enormous, and the sun barely reaches the ground.";
        Exits["west"] = "/domains/greenvale/corridor/eldergreen_edge/w3";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/d2";
        ResetSeconds = 120;
        DangerBand(7, 8);

        AddDetail("wallow,mud,bog,hollow,pool", "The mud is black, glossy and ankle-deep, with a skin of green scum over it. Great hollows where the boars have lain are still wet, and the shape of a sow with piglets is pressed in one.");
        AddDetail("trees,trunks,scores,tusk marks,tusks,gouges", "The gouges in the bark run from knee to waist height, and the sap has bled and hardened amber in the deepest. A tusker rubbed here lately.");
        AddDetail("furrows,roots,rooting,ground,earth", "The ground between the trees looks ploughed. Acorns, grubs and truffles have been turned up and eaten, and whole saplings uprooted.");
        AddDetail("tracks,prints,hoofprints", "The boar prints are wide as a hand, deeply cut, and some are fresh; among them are smaller ones, a sow's litter running between.");
        AddDetail("thicket,thickets,grunting,sound", "The grunting comes from the thicket on the north side, rhythmic and unconcerned, and then breaks off with a snort.");
        AddDetail("bristles,hair,bristle", "Coarse black bristles are caught on the bark in clumps, stiff as wire. A fistful would make a decent brush.");

        AddSpawn("/domains/greenvale/corridor/mob/wild_boar", 2);
    }
}
