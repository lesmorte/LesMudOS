// @inherit /std/room

public class HwW2 : Room
{
    public override void Create()
    {
        ShortDesc = "The Uprooted Giant";
        LongDesc = "A great tree has gone over here, and lies where it fell with its crown wrecked among the trunks it brought down. Its root plate stands on end, twice the height of a man and wider than a barn door, a wall of dark earth and interlocked roots hung with stones and dead fern. Where the plate has torn out of the ground a deep hollow gapes beneath it, shaded and wet. The trunk beyond is as long as a street, and it is clothed in moss from end to end. The trail runs on to the north, along its flank.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/w1";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/w3";

        AddDetail("root plate,plate,roots,root,wall,earth", "The plate is a disc of earth and tangled roots torn upright, taller than a house, with a skin of fern and moss across the top. Stones as big as your head are still bedded in the soil between the roots, as though the tree had held them close. The bottom of the plate meets the ground in a ragged line of dark.");
        AddDetail("hollow,hole,pit,gap,dark,beneath,under,below", "Under the lower edge of the root plate the ground drops into a dark hollow, deeper than a man is tall. The earth there is damp and smells of rot and cold stone, and a draught moves out of it, slow and steady. The roots hang across the mouth like a curtain. You could climb down among them, if you looked closely at how the roots make a stair.");
        AddDetail("draught,draft,air,smell,wind", "The air that comes up from under the plate is cooler than the air around you and does not stop. It carries a smell of wet earth and of something rank, like a fox-earth multiplied.");
        AddDetail("tracks,prints,trodden,trampled,ground", "The ground at the mouth of the hollow is trodden bare in a path from the trail to the dark, worn by small hard feet in a great number. The tracks are mostly old, and mostly lead in.");
        AddDetail("trunk,tree,giant,bark", "The trunk lies along the trail for sixty paces, with its limbs snapped under it. It was an oak, or had been. Heart-rot has hollowed it, and the inside is full of pale beetle-gnawed wood.");
        AddDetail("moss,fern,ferns,stones,stone","The moss on the trunk is deep and green, and ferns have rooted along its upper side in a long, even line. Among the roots of the plate dead fern hangs in brown hanks, and the stones in the soil are rounded and wet.");

        HiddenExit("down", "/domains/greenvale/corridor/root_warrens/entrance", "hollow,hole,pit,gap,dark,beneath,under,below,draught,draft,air,smell,wind,tracks,prints,trodden,trampled", 10,
            "You lift the curtain of roots at the lip of the hollow and find, as the draught has been telling you, that they have been cut into a stair, square and old. A way leads {cmd}down{/} among them, into the cold.");
        AddSpawn("/domains/greenvale/corridor/mob/black_bear", 1);
    }
}
