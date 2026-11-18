// @inherit /std/room

public class SrD1 : Room
{
    public override void Create()
    {
        ShortDesc = "The Fox Covert";
        LongDesc = "The ground drops steeply from the hill into a dense covert of blackthorn and hazel, so thick that the light comes down the colour of weak tea. The earth is pocked with burrows and trodden runs, and the smell of fox is strong and sharp, over something older and wilder. Bones lie scattered at the entrance of an earth, small bones and a few large ones, cleaned and white. Branches rattle, and somewhere behind you a twig breaks, and you wonder whether it was you.";
        Exits["east"] = "/domains/greenvale/corridor/south_road/w3";
        Exits["north"] = "/domains/greenvale/corridor/south_road/d2";
        ResetSeconds = 120;

        AddDetail("blackthorn,hazel,thicket,covert,branches", "Blackthorn with thorns as long as a finger, and hazel bent in the wind. Every path through it is low and sidelong.");
        AddDetail("burrows,earth,runs,holes", "A network of holes and runs. Some are narrow as a cat; one, larger than the rest, is scored deep by claws.");
        AddDetail("bones,bone,skull,remains", "Rabbit bones, the skull of a sheep, and a few much larger ones that have been gnawed and cracked for the marrow. The cleaning is expert.");
        AddDetail("smell,scent,fox", "The sharp musk of fox, and under it something heavier. The fox did not make all of these tracks.");
        AddDetail("tracks,paw prints,prints,wolf tracks", "Among the fox prints there are others, bigger and splayed, with the claw marks showing. They go both ways.");

        AddSpawn("/domains/greenvale/corridor/mob/stray_wolf");
    }
}
