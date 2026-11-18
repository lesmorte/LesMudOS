// @inherit /std/room

public class GrTheSpiderRavine : Room
{
    public override void Create()
    {
        ShortDesc = "The Spider Ravine";
        LongDesc = "A deep cleft in the hillside where the land falls away on both sides in sheer, slimy rock, and the trail runs along the bottom beside a black stream. Webs stretch from wall to wall, thick as rope and white as bone, and the whole ravine seems to breathe with a faint tremble. Fallen leaves and bones lie in drifts. High above, the strip of sky is the color of dishwater.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/d1";
        Exits["north"] = "/domains/greenvale/corridor/green_road/d3";
        Safe = false;

        AddDetail("ravine,cleft,gully,walls,wall,rock,rocks", "A sheer-sided cleft forty feet deep and twenty wide, full of cold air. The rock is black, wet and furred with lichen.");
        AddDetail("stream,water,brook,rill", "A black stream runs along the floor over smooth stones, silent and cold. Its edges are stringy with web.");
        AddDetail("webs,web,silk,ropes,strands,threads", "Thick cables of silk run from wall to wall, bound with fine threads and set at every height. They are strong enough to bear a man's weight, and some already do.");
        AddDetail("bones,bone,leaves,drifts,remains,carcasses", "A drift of old bones, bleached and cleaned: deer, a wolf, a number of smaller things, and a human jaw. Spiders do not leave much.");
        AddDetail("sky,strip,gap,light", "A narrow strip of cloudy sky far overhead. The light in the ravine is feeble, dim and greenish.");
        AddDetail("lichen,moss,ledges,ledge,crevices", "Pale lichen grows on the ledges, and in the crevices there are white egg sacs the size of fists.");

        AddSpawn("/domains/greenvale/corridor/mob/woodland_spider", 2);
    }
}
