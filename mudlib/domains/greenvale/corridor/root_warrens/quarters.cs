// @inherit /std/room

public class RwQuarters : Room
{
    public override void Create()
    {
        ShortDesc = "The Diggers' Quarters";
        LongDesc = "This was where the goblins slept and ate, a wide low room divided by partitions of woven root and hide, most of them now fallen. Sleeping hollows are scooped into the clay floor in rows, each lined with a rotted litter of straw and fur, and the smallest are no longer than a man's forearm. A cold hearth stands against the east wall with a flue of soot running up into the roots above it. A cooking pot still sits on the hearth, collapsed into a skin of green. The room has the stillness of a house whose people have gone out and not come back.";
        Exits["west"] = "/domains/greenvale/corridor/root_warrens/junction";
        Exits["south"] = "/domains/greenvale/corridor/root_warrens/gallery";
        Exits["east"] = "/domains/greenvale/corridor/root_warrens/larder";
        ResetSeconds = 300;
        DangerBand(7, 8);

        AddDetail("partitions,partition,hide,hides,woven,screens", "The partitions are frames of split root woven with strips of hide, and the hides are old, stiff and cracked, in grey, brown and one patchy yellow. Where they have fallen they lie in curled sheets that have not rotted. A tanner or a leatherworker might yet have a use for them.");
        AddDetail("hollows,hollow,beds,bed,litter,straw,fur", "The sleeping hollows are worn smooth and each has its own shape, a wide one for a hulking digger and a narrow one beside it. The smallest, in a row at the back, are lined with soft grey fur and one has a wooden toy lying in it, a horse with one leg.");
        AddDetail("hearth,fire,flue,soot,chimney,ashes", "The hearth is a half-ring of baked clay stones with a bed of grey ash that has never been disturbed. The flue goes up into the roots and is lost. Beside the hearth, close to the floor, a gap the height of a stooping goblin leads on east into the dark of a store.");
        AddDetail("pot,pots,skin,cooking pot", "The pot is a black iron cauldron on three short legs, and what is in it has dried to a hard green crust that holds the shape of the last spoon.");
        AddDetail("floor,clay,ground", "The floor has been trodden hard and swept, and then not swept for a very long time. Small prints cross the dust everywhere, and they are the prints of rats, not of goblins.");
        AddReadable("wall,scratchings,scratches,pictures,drawings,drawing,writing,letters", "Low on the wall, at the height of a small child's reach, a nail has been used to scratch pictures and crude letters into the clay:\n\n  A row of stick figures holding hands under a round sun with rays, a sun that nobody under the ground could have seen. Beneath them, in wobbling capitals: 'ME. MUM. BIG GRIT. UNCLE NUB.'\n\n  'UNCLE NUB HEARS THINGS. MUM SAYS HE IS DAFT.'\n\n  Farther along the figures have been crossed out, one by one. Over the last of them a fat creature with a long tail and too many teeth has been drawn, scratched deep.\n\n  At the very end, pressed hard: 'WE GO UP TODAY. MUM SAYS NO LOOK BACK. I LOOKED.'", "Pictures and letters are scratched low on the wall. You could {cmd}read{/} them.");

        AddSpawn("/domains/greenvale/corridor/mob/tunnel_rat", 1);
    }
}
