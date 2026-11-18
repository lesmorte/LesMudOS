// @inherit /std/room

public class SrD2 : Room
{
    public override void Create()
    {
        ShortDesc = "The Bramble Hollow";
        LongDesc = "The ground sinks into a hollow so choked with bramble that the walls of it are a roof of thorny arches. Dead canes lie in drifts across the floor, and the living ones reach out to snag a sleeve or a cheek. Blackberries grow heavy where the sun finds a gap, but the way to them is a gauntlet. Along the western wall the brambles grow strangely, bowed in a smooth arch over something square and hidden, and a few canes there have been snapped and tied back, long ago, by a hand that wanted to pass.";
        Exits["south"] = "/domains/greenvale/corridor/south_road/d1";
        Exits["north"] = "/domains/greenvale/corridor/south_road/d3";
        ResetSeconds = 120;

        AddDetail("brambles,bramble,canes,thorns,arches", "Brambles in great arching hoops, grown over and through each other for decades. The thorns have hooked into a tatter of an old cloak.");
        AddDetail("blackberries,berries,fruit", "Big dark blackberries, so ripe they fall at a touch. Getting at them without bleeding is another matter.");
        AddDetail("hollow,floor,walls,ground", "A sunken place, as if something had been dug out of the ground and the ground had never forgotten. The air in it is still and close.");
        AddDetail("west wall,western wall,arch,square,stones,stonework", "Along the west side the brambles arch smoothly over a square shape. Between the canes you glimpse mortared stone, mossed and old.");
        AddDetail("tied canes,snapped canes,tied back,stumps", "A few thick canes on the west side have been cut and bent back with twine long since rotted into the wood. Someone once made a doorway here, and the thorns have been healing it ever since.");
        AddDetail("cloak,tatter,cloth,rag", "A rotted scrap of green wool, caught on the thorns at head height. It has been there longer than any of the brambles around it.");

        AddNode("/domains/greenvale/corridor/obj/berry_bush");
        HiddenExit("west", "/domains/greenvale/corridor/south_road/s1", "west wall,western wall,arch,square,stones,stonework,tied canes,snapped canes,tied back,stumps", 20,
            "You work along the west wall, lift the old tied canes and bend them back as the hand that tied them meant them to go. Behind the arch the brambles part on a square shape of mossed stone, and a way opens {cmd}west{/}.");

        AddSpawn("/domains/greenvale/corridor/mob/stray_wolf");
    }
}
