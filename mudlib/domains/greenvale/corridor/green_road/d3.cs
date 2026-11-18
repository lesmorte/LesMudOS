// @inherit /std/room

public class GrTheBanditsLookout : Room
{
    public override void Create()
    {
        ShortDesc = "The Bandits' Lookout";
        LongDesc = "A rocky outcrop above the forest floor has been turned into a lookout, with a platform of lashed logs in the crooks of two great oaks and a ladder of rope hanging down. From here a watcher could see for a quarter-mile along both the trail and the road. A fire-pit lies cold at the base, ringed by log seats, and the ground is strewn with bottle-ends, dice and gnawed bones. The bandits who use it are no friends of the goblins, nor of anyone.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/d2";
        Exits["east"] = "/domains/greenvale/corridor/green_road/w5";
        Safe = false;

        AddDetail("outcrop,rock,rocks,ledge,rocky outcrop", "A shelf of grey rock raised a few feet above the surrounding forest floor. It gives a clear sight line in every direction through a gap in the trees.");
        AddDetail("platform,logs,oaks,oak,lashings,ropes,rope,ladder", "A crude platform of lashed logs in the crotch of two oaks, reached by a rope ladder. It would hold three men, but the ladder is hauled up when the platform is manned.");
        AddDetail("fire-pit,firepit,pit,fire,seats,log seats,ashes", "A ring of stones with cold ashes inside. Log seats surround it, and the ground is churned up by boots.");
        AddDetail("bottles,bottle,dice,bones,litter,rubbish", "Broken bottles, a pair of bone dice and the remains of many meals. The dice are weighted.");
        AddDetail("road,view,sightline,trail,sight line,gap", "Through a gap in the trees you can see the road below, tiny and bright, with figures walking on it. It's an easy thing, from here, to choose a likely mark.");
        AddDetail("scratches,carvings,tally,marks,tree marks", "Tally marks cut in the bark of the oak, in groups of five, in several different hands. There are a great many of them.");

        AddSpawn("/domains/greenvale/corridor/mob/rogue_bandit", 2);
    }
}
