// @inherit /std/room

public class GrIntoTheDeepWood : Room
{
    public override void Create()
    {
        ShortDesc = "The Green Road, into the Deep Wood";
        LongDesc = "Behind you the last fence posts of the Edge Road are already lost in the trunks, and ahead the road goes under the trees as a cart goes into a barn. The oaks and beeches here are close enough to touch crowns overhead, and the light that reaches the ruts is green and thin. The air is cooler and smells of leaf mold and standing water. Somewhere off in the wood a branch cracks, and nothing follows it.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/m11";
        Exits["north"] = "/domains/greenvale/corridor/green_road/m02";
        Safe = false;

        AddDetail("road,ruts,rut,track", "Two deep ruts with a hump of moss between them. Wheels have kept the way open, but the wood has been taking back the verges a little each year.");
        AddDetail("trees,oaks,beeches,oak,beech,trunks,trunk", "Big, grey-barked trees standing shoulder to shoulder. Nothing grows beneath them but fern and pale, leggy saplings that will never see the sun.");
        AddDetail("canopy,crowns,branches,light,sky", "The branches meet far above and the sky is only a rumor of brightness. The sunlight that does fall comes down in thin green shafts and does not warm anything.");
        AddDetail("fence,fence posts,posts,gate,green gate", "Behind you, the grey stumps of the last fence line mark where the farmers gave up. Beyond them the wood has never been cleared.");
        AddDetail("ferns,fern,saplings,undergrowth", "Waist-high fern, and between the fronds something has made a narrow path that does not follow the road. It is probably only deer.");
        AddDetail("signs,tracks,prints", "Hoof prints and cart marks, many of them overlaid, all heading the same way. Travelers here have learned to go in company, and the tracks show it.");

        AddEncounter("/domains/greenvale/corridor/mob/boar_brute", 6);
    }
}
