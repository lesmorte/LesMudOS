// @inherit /std/room

public class EeNarrows : Room
{
    public override void Create()
    {
        ShortDesc = "The Edge Road, the Narrows";
        LongDesc = "The road is squeezed here between a steep, root-tangled bank on one side and a drop to a wooded gully on the other, until it is no wider than a single wagon. Great trunks stand so close to the verge that their lower branches brush your shoulders. Rangers have cut notches in the nearest trees at the height of a rider's head, and the cuts are fresh. Sound carries oddly in the narrow place, and every footstep seems to come back from the trees a moment late.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/m07";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/m09";
        ResetSeconds = 120;

        AddDetail("bank,roots,root,tangle", "The bank on the west is a wall of earth and roots, with oak roots as thick as a man's thigh knotted into it like cables.");
        AddDetail("gully,drop,slope,ravine", "The gully on the east is a green well of ferns and fallen branches, far enough down that its stream is only a sound. A careless carter could lose a wheel over the edge.");
        AddDetail("trunks,trees,branches,oaks", "The trunks press so close that the road is a corridor, and the light is dimmer here, though the sun still finds the ground.");
        AddDetail("notches,notch,cuts,marks,rangers marks", "The notches are cut at rider-height in a regular pattern: one for the day, a longer cut for the week. A Ranger patrol keeps tally of how many times it has passed, and when it passed last.");
        AddDetail("echo,echoes,sound,footsteps", "The sound of your steps comes back from the trees slightly altered, as though someone were pacing you.");
        AddDetail("wheel marks,ruts,scrape,scrapes,road", "Scrapes of axle hubs show white on the bank at the height of a cart. Teamsters hate this stretch, and have left their marks on it.");

        AddEncounter("/domains/greenvale/corridor/mob/forest_wolf", 12);
        AddEncounter("/domains/greenvale/corridor/mob/forest_spider", 8);
    }
}
