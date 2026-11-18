// @inherit /std/room

public class RwRootHall : Room
{
    public override void Create()
    {
        ShortDesc = "The Root-Choked Hall";
        LongDesc = "This was the goblins' great hall, and the roots of the fallen giant have taken it. They pour through the roof in pale columns as thick as barrels and spread across the floor in knotted cords, until the hall is a thicket you must climb and squeeze through. The air is cold and tastes of earth, and the whole place gives off a soft sickly gleam of white fungus on the dead wood. Some of the roots have been burned back along the south wall to leave a gap, and the black of the fire is fresh. In the middle of the floor, the cords part around a ragged hole, and a worn slope leads down into the dark.";
        Exits["east"] = "/domains/greenvale/corridor/root_warrens/gallery";
        Exits["west"] = "/domains/greenvale/corridor/root_warrens/clay_cut";
        Exits["south"] = "/domains/greenvale/corridor/root_warrens/corner";
        Exits["down"] = "/domains/greenvale/corridor/root_warrens/brood_passage";
        ResetSeconds = 300;

        AddDetail("roots,root,columns,column,cords,cord,thicket", "The roots are the colour of tallow and as smooth as skin, with a faint sheen of wet. When you look away from them you catch a movement at the edge of your sight, a slow shifting of the cords, and when you look back the cords lie as they did. They do not rot like dead wood, and they are cold to the touch.");
        AddDetail("pale roots,rootlets,root hairs,hairs,threads,white", "Among the great cords hang bundles of fine white rootlets, soft as wool and as long as a forearm. They break off clean, and they smell sharply green. An herbalist might know what to make of them.");
        AddDetail("fungus,fungi,gleam,light,glow,dead wood", "The fungus is a skin of tiny white brackets, rimming the dead root-ends like frost, and it gives off a dim glow that is just enough to see the shapes of the roots by. It is dry to the touch and crumbles easily.");
        AddDetail("burned,burned roots,burnt,fire,black,gap,south", "The roots along the south wall have been hacked and burned back to make a gap the width of two men, and the black is fresh, with charcoal still sharp-edged. Beyond the gap the air is warmer and smells of smoke and old meat. Someone has been keeping this way open, and not for the rats.");
        AddDetail("hole,slope,pit,floor,dark,ragged", "The hole is the width of a cart, with a ramp of packed earth sloping away into it, worn by constant traffic to a smooth gleaming channel down the middle. The roots have grown around its lip but nowhere across it. The air that comes up is hot and foul, and it breathes in and out in slow time.");
        AddDetail("hall,roof,ceiling,pillars,walls", "Under the roots you can make out the shape of the hall: a high vaulted roof cut from the clay, and pillars left standing between the diggings. The goblins made it to be seen, and it is hard to imagine them making anything finer.");

        AddSpawn("/domains/greenvale/corridor/mob/root_creeper", 2);
    }
}
