// @inherit /std/room

public class OaM02 : Room
{
    public override void Create()
    {
        ShortDesc = "The Approach Road, the First Paving";
        LongDesc = "Here the dirt road ends as though cut with a knife, and flat grey flagstones begin, laid edge to edge and sunk into the earth by a century of wheels. The ruts are worn into the stone itself, two shallow troughs polished by iron, and the sound of your own boots changes to a bright, hard tapping. Hawthorn hedges line the way, and between two old oaks to the north something pale stands on the skyline that might be cloud, and might not.";
        Exits["south"] = "/domains/greenvale/corridor/approach/m01";
        Exits["north"] = "/domains/greenvale/corridor/approach/m03";
        ResetSeconds = 120;

        AddDetail("flagstones,paving,stones,flags,road", "Each flag is as long as a man is tall, quarried, squared and laid by people who expected the road to last. They are cracked and heaved in places, and mended in places with stone of a different grey.");
        AddDetail("ruts,troughs,wheel ruts,grooves", "The grooves are exactly one wagon wide. Following them with your eye you can see how every cart that ever came this way has chosen the same two lines.");
        AddDetail("hedge,hawthorn,hedges,haws", "The hawthorn is thick and well laid, clipped on the road side and left to straggle on the other. Red haws glow among the thorns, and sparrows quarrel in the depths.");
        AddDetail("pale shape,skyline,cloud,north,horizon,something", "Between the oaks the far horizon holds a long, pale, level smudge, too straight to be a cloud and too distant to be anything else. When a breeze moves the leaves it disappears, and when they part it is there again.");
        AddDetail("oaks,oak,old oaks,trees", "Two old oaks stand either side of the road like gateposts, their lower limbs sawn away to let high wagons through. Both carry a blaze of white paint at head height.");
        AddDetail("horseshoe,shoe,iron", "A horseshoe lies in the gutter, thin as a wafer, worn through at the toe. It has been walked a long way and then lost.");

        AddEncounter("/domains/greenvale/corridor/mob/highway_bandit", 8);
    }
}
