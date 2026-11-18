// @inherit /std/room

public class EePatrolMile : Room
{
    public override void Create()
    {
        ShortDesc = "The Edge Road, the Patrol Mile";
        LongDesc = "The road here is the best-kept stretch in the wood: brush cut back a spear's length on both sides, ruts filled, fallen limbs hauled off to the verge in tidy piles. Rangers' blazes, a small eye painted in silver-grey on a green square, mark every hundred paces, and cairns of fist-sized stones stand beside the way. The oaks are vast now, and between them the light falls golden on moss. It feels safe, and the rangers have worked hard to make it feel so.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/m09";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/m11";
        ResetSeconds = 120;
        DangerBand(5, 7);

        AddDetail("brush,verge,limbs,piles,cut", "The brush has been cut with billhooks and stacked on the verge to rot into mulch. Beyond the cut line the wood looks different: closer, darker, watching.");
        AddDetail("blazes,blaze,eye,paint,square", "On the trunk at your elbow is a small green square with a stylised eye painted on it in silver-grey. They appear every hundred paces, and each is freshly touched up.");
        AddDetail("cairns,cairn,stones,pile", "Each cairn is a stack of ten or twelve stones, set at the roadside. A ranger adds one stone as she passes; you count seven here, and the top one is still wet.");
        AddDetail("oaks,trees,moss,light", "The oaks along the Patrol Mile are among the biggest you have ever seen, trunks wider than doorways, their moss thick as velvet. Sunlight falls on them in slow gold bars.");
        AddDetail("thistle,thistles,verge thistles,flowers", "Thistles flourish in the sunny gaps the rangers have cut, purple heads nodding. The rangers leave them for the bees.");
        AddDetail("ruts,road,surface,gravel", "The road surface has been dressed with gravel carried in from the river, and drains cut across it at intervals to carry off the wet.");
        AddNode("/domains/greenvale/corridor/obj/thistle_patch");

        AddEncounter("/domains/greenvale/corridor/mob/forest_wolf", 6);
        AddEncounter("/domains/greenvale/corridor/mob/forest_spider", 5);
    }
}
