// @inherit /std/room

public class GvHiddenGrove : Room
{
    public override void Create()
    {
        ShortDesc = "The Hidden Grove";
        LongDesc = "The way opens into a wide green hollow ringed on every side by old walls, and the city might as well be a hundred miles off. Beech, yew and hazel stand in the still air, and birdsong replaces every sound of the streets: blackbirds, finches, a pigeon somewhere high up. Moss-covered stones, some knee-high and some taller than a child, line the beaten earth paths, and on a flat stone like a table at the center someone has left bread, ribbons and flowers. Grey roots, thick as cart-horses, run across the grove and vanish beneath the walls, and to the north the path leads on toward a low and enormous shape. In the far wall, half-swallowed by ivy, there is a small iron door.";
        Exits["east"] = "/domains/greenvale/oakenfell/grove/overgrown_way";
        Exits["north"] = "/domains/greenvale/oakenfell/grove/great_stump";

        AddDetail("birds,birdsong,blackbirds,finches,pigeon,sound", "Blackbirds sing from the hazels, finches chatter in the yews, and a wood pigeon calls somewhere in the branches. Not a single sound of carts or cries reaches you here.");
        AddDetail("trees,beech,yew,hazel,branches,canopy,sky", "Old beeches and yews, with hazel at their feet, stand well apart from one another as though they were planted or spared. Through the gaps the sky is a very small and very blue square.");
        AddDetail("stones,moss,moss-covered stones,standing stones,markers,paths,path,earth", "Ancient moss-covered stones, none of them in a straight line, border the beaten earth paths. Some are carved with letters and names worn smooth by weather, and some with nothing at all. Each one has been kept clear of weeds by somebody.");
        AddDetail("offerings,offering,bread,ribbons,flowers,gifts,flat stone,table,altar,stone", "The flat stone at the center is a slab of grey rock as broad as a table. Left on it, in no order, are heels of bread, braids of ribbon, a child's knitted bird, a carved wooden boat, a handful of copper coins and fresh and withered flowers together. None of it is yours to take, and nothing about the place suggests anyone has ever tried.");
        AddDetail("roots,root,grey roots,thick roots,walls under", "Grey roots as thick as cart-horses rise out of the earth and dive back again, crossing the paths and the grove in great arcs. Every one of them runs back to the north, to the shape in the middle of the grove, and where they meet the walls they pass underneath.");
        AddDetail("walls,wall,old walls,ring of walls,ivy", "Old dark walls, the same as in the way you came through, encircle the grove. They stand a little taller than the trees, patched, mossed, and quite without a gate, and beyond them the roofs of the city cannot be seen at all.");
        AddDetail("door,iron door,garden door,rusted door,rusty door,far wall,small door", "A small garden door of black iron set in the far wall, its hinges red with rust and half-covered in ivy. Its latch has fused into the frame and there is no keyhole. Folk say it opens onto the Guild Quarter, but it has been seized shut for as long as anyone can say.");
        AddDetail("north,shape,low shape,enormous shape,stump", "To the north the path leads on between two yews toward a low, enormous shape, grey and flat-topped, much bigger than you would believe from this distance.");

        AddSpawn("/domains/greenvale/oakenfell/grove/npc/tobiah_rue");
    }
}
