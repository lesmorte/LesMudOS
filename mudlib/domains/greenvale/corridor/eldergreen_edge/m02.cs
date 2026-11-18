// @inherit /std/room

public class EeFirstOaks : Room
{
    public override void Create()
    {
        ShortDesc = "The Edge Road, the First Oaks";
        LongDesc = "The first great oaks stand on either side of the road, so broad that two people could not link arms around the nearest, and their crowns knit overhead into a loose roof that lets the sun fall through in coins. The verge is thick with bracken and foxgloves, and the road is damp underfoot even in dry weather. Rooks argue in the high branches, and from somewhere in the shade comes the dry rattle of a woodpecker.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/m01";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/m03";
        ResetSeconds = 120;

        AddDetail("oaks,oak,trees,tree,trunks", "The nearest oak is older than the road. Its bark is cut into ridges deep enough to hide a hand, and its lowest branch is thicker than most trees on the farms.");
        AddDetail("crown,crowns,canopy,sun,sunlight,light,coins", "The leaves overhead are green gold where the sun comes through them, and the light moves on the road in slow patches as the branches stir.");
        AddDetail("bracken,foxgloves,foxglove,verge", "Bracken stands waist-high under the oaks, and foxgloves lean out of it, speckled and purple. Nothing grazes them; even the sheep know better.");
        AddDetail("rooks,rook,branches,birds", "The rooks are black and loud and entirely unafraid of you. One stares down from a bough with an acorn in its beak, and drops it near your feet.");
        AddDetail("woodpecker", "The drumming comes from higher up than you can see, a slow, deliberate knocking, as if the wood were being asked a question.");
        AddDetail("acorns,acorn,leaves,litter", "Last year's acorns lie thick on the road edge, split by the damp, and the leaf litter under the trees is dark and soft as a mattress.");

        AddEncounter("/domains/greenvale/corridor/mob/forest_wolf", 6);
        AddEncounter("/domains/greenvale/corridor/mob/forest_spider", 6);
    }
}
