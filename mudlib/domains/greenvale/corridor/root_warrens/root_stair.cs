// @inherit /std/room

public class RwRootStair : Room
{
    public override void Create()
    {
        ShortDesc = "The Shored Stair";
        LongDesc = "A steep stair drops from the hollow into the earth, its treads cut square into the clay and strengthened here and there with a living root left to cross the step. The walls are close enough to touch on both sides and bear the long parallel scars of picks, and every few steps a niche has been scooped out to hold a lamp. The lamps are long gone and the niches are filled with droppings. Somewhere below, claws tick on stone and stop, and tick again. The daylight fails behind you, and the air grows warmer and more crowded with each step.";
        Exits["up"] = "/domains/greenvale/corridor/root_warrens/entrance";
        Exits["south"] = "/domains/greenvale/corridor/root_warrens/junction";
        ResetSeconds = 300;
        DangerBand(7, 8);

        AddDetail("stair,steps,step,treads,tread", "The treads are the width of a short man's stride and cut with such care that the edges are still sharp. In the middle of each, a shallow dip has been worn by the passage of a great many feet.");
        AddDetail("walls,wall,clay,scars,picks,pick marks", "Pick marks run down the walls in neat slanted rows, each cut the length of a goblin's arm. At head height the clay has been scraped in a bright band, as if by fur, where something has been using this stair for years.");
        AddDetail("niches,niche,lamps,lamp,soot", "The niches are blackened above with the soot of tallow lamps. Each is half full of small hard droppings and tufts of grey fur, and one holds the half-eaten rind of something that was once a boot.");
        AddDetail("roots,root,crossing", "A few roots were left across the stair when it was cut, and the goblins smoothed their upper faces into steps. They are pale and thick and cold, and in one the rats have gnawed a gap you could put your fist through.");
        AddDetail("droppings,dung,fur,tufts", "The droppings are old and new together, and the newest are as long as a thumb. The fur is grey, coarse and plentiful, and clings to the walls in dirty drifts.");
        AddDetail("claws,sound,sounds,ticking,scratching", "The sound comes from the south, light and quick and irregular. Whatever makes it stops whenever you stand still, and begins again when you move.");

        AddSpawn("/domains/greenvale/corridor/mob/tunnel_rat", 1);
    }
}
