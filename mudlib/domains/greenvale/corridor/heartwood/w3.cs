// @inherit /std/room

public class HwW3 : Room
{
    public override void Create()
    {
        ShortDesc = "The Dryad Spring";
        LongDesc = "A spring wells up in a hollow between four great roots, and its water is so clear that the stones and the pale sand at the bottom seem to float. Round it the moss is cropped short, and there are hoof-prints in the mud at the edge, and the prints of birds, and the prints of something large and soft-footed. A deer lifts its head from the far side as you come, looks at you, and goes on drinking. Everything here is quiet, and nothing is afraid. A deer path leads east into the bracken.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/w2";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/w4";
        Exits["east"] = "/domains/greenvale/corridor/heartwood/d1";

        AddDetail("spring,water,pool,basin,well", "The water rises from the sand in a slow, steady welling, and the surface is never still but never ripples. The cold of it reaches your hand before it touches. It tastes of stone and of nothing else.");
        AddDetail("roots,root,four roots,hollow", "The four roots that frame the spring grow from trees you cannot see the tops of, and they curve together like the fingers of a cupped hand. Their inner faces are worn smooth where animals have rubbed against them.");
        AddDetail("prints,tracks,mud,hoofprints,hoof-prints,edge", "The mud at the edge is crowded with tracks: deer, boar, fox, hare, the small stars of birds, a bear's broad heel and, over them all, the deep pad of a great wolf. They overlie each other without fear, as if the wolf had come to drink while the deer were here.");
        AddDetail("deer,hind,doe,animal,animals", "The deer is a hind with a notched ear. She drinks at the pool while you stand there, and when she is done she walks slowly away through the ferns and does not look back. Nothing else comes out to take her place.");
        AddDetail("moss,cropped,grass,ground", "The moss near the spring is kept as short as a lawn, by teeth. Farther out it grows long and loose, as it does everywhere else.");
        AddDetail("east,path,deer path,bracken,trail", "A deer path leads east through the bracken, a groove in the green about as wide as a beast's flank. It is the only way on that has been kept, and by animals.");
    }
}
