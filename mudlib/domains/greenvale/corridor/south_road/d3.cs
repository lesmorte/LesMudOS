// @inherit /std/room

public class SrD3 : Room
{
    public override void Create()
    {
        ShortDesc = "The Old Sheepfold";
        LongDesc = "A broken sheepfold crowns the ridge, its drystone walls fallen into heaps and its gate a pair of rotten posts. Beside it the roof of a shepherd's hut lies in on the floor. Nothing grows inside the ring but nettles and pale grass, and the view from the wall runs north across the whole country to the black line of the Eldergreen Wood. Someone has been here lately: the ashes of a small fire lie in the lee of the hut, a tally of notches runs along a fallen lintel, and a boot-print is pressed deep in the mud. Above the gate hangs a knot of hawthorn twigs that nobody put there for the sheep.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/d2";
        Exits["east"] = "/domains/greenvale/corridor/south_road/w5";
        ResetSeconds = 120;

        AddDetail("fold,sheepfold,walls,wall,stones,ring", "The walls of the fold stand knee high where they stand at all. They were built to keep sheep in and wolves out, and they have done neither for a long time.");
        AddDetail("hut,roof,shepherd's hut,ruin", "The roof timbers of the hut are rotted through and lie in a wedge on the floor. A rusted iron crook is still hooked on a nail.");
        AddDetail("ashes,fire,embers,cinders", "Grey ashes with a scorched core, the remains of a very small fire made by someone who did not want smoke. The ash is still dry.");
        AddDetail("notches,tally,lintel,boot-print,boot print,print,mud", "A row of notches cut in the lintel with a knife, one for each day, and a boot-print pressed into the mud beside it. Whoever it was kept watch on the road for a good many days.");
        AddDetail("view,north,forest,road,country", "From here you can see all the way to the Stone Bridge, and the dark bank beyond it. It is a good place to watch the road without being seen.");
        AddDetail("twigs,hawthorn,knot,gate,posts", "A tight knot of hawthorn twigs, bound with a strip of wool and hung from the gate post. It is neither a farmer's mark nor a child's toy, and the shepherds say they have not touched it.");

        AddSpawn("/domains/greenvale/corridor/mob/bandit_lookout");
        AddSpawn("/domains/greenvale/corridor/mob/stray_wolf");
    }
}
