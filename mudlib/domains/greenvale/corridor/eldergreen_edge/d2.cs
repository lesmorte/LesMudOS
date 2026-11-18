// @inherit /std/room

public class EeGoblinTrail : Room
{
    public override void Create()
    {
        ShortDesc = "The Goblin Scouts' Trail";
        LongDesc = "A narrow trail winds through the trees here, almost invisible, trodden to a thread by small bare feet and marked by signs a woodsman would know. Strands of knotted cord hang from low branches, and scratches in the bark show the way to someone who can read them. The trees are oak and beech as vast as towers, and their huge roots rise in knees and walls around the trail. Eastward, between two walls of ivy-hung roots, a single giant beech stands apart from its fellows, higher than anything about it. The wood is very quiet.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/d1";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/d3";
        ResetSeconds = 120;

        AddDetail("trail,path,footprints,prints,tracks", "The prints are small, bare and splay-toed, no longer than your hand. They go in single file and in both directions, and the newest overlie the old.");
        AddDetail("cords,cord,knots,strands,branches", "The cords are plaited from bark fibre, each with a number of knots in it. One knot to a wagon, two to an armed party; a goblin who passes the road counts the traffic. Each cord ends in a peg of wood cut with three notches.");
        AddDetail("scratches,bark,signs,symbols,marks,scratch", "The scratches are crude arrows and crosses, cut with a knife and inked with something black. They point towards the road, and then away from it.");
        AddDetail("roots,knees,walls,oak,trees", "The roots form a labyrinth of walls and knees, in which a small creature could hide with no trouble at all.");
        AddDetail("giant beech,great beech,great tree,beech tree,fork,ivy,east,spikes,holes,hole", "The giant beech stands a little east of the trail behind a curtain of ivy that has been combed aside, and let fall again, many times. High in its fork, forty feet up, something that looks like a platform of branches is almost hidden by leaves, and in the grey bark below the ivy are neat square holes, in a line, like the rungs of a ladder. Nobody climbs trees by accident, but somebody has climbed this one.");
        AddDetail("silence,quiet,wood", "No birds sing here. The silence is the silence of a held breath.");
        HiddenExit("east", "/domains/greenvale/corridor/eldergreen_edge/s1", "giant beech,great beech,great tree,beech tree,fork,ivy,east,spikes,holes,hole", 20,
            "You comb the ivy aside as others have done before you and set a hand to the square holes in the grey bark. They are rungs, old and still sound, and they climb to the fork. A way leads {cmd}east{/}, up into the beech.");

        AddSpawn("/domains/greenvale/corridor/mob/goblin_scout", 2);
    }
}
