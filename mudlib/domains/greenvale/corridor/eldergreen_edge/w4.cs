// @inherit /std/room

public class EeSpiderThicket : Room
{
    public override void Create()
    {
        ShortDesc = "The Spider Thicket";
        LongDesc = "The trees crowd here into a dense thicket of hazel, holly and blackthorn, tangled with brambles and hung everywhere with webs. Great silvered sheets of silk stretch between the branches, beaded with dew, and the light that filters through them is grey and thin. The air is still and smells of mould and something sour, and there is a faint rustle that never quite goes away. The path threads through the thicket in a narrow tunnel of leaves, and you would be wise to stoop.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/w3";
        Exits["north"] = "/domains/greenvale/corridor/eldergreen_edge/w5";
        ResetSeconds = 120;

        AddDetail("webs,web,silk,sheets,strands", "The webs are huge, tight and strong, and the nearest sheet is as thick as a bedsheet. Small bones and beetle shells hang in it, bound in silk.");
        AddDetail("thicket,hazel,holly,blackthorn,brambles,bramble", "Hazel, holly and blackthorn have woven themselves into a barrier that only the path pierces. The thorns are long and black, and catch at cloaks.");
        AddDetail("dew,drops,light", "Dew beads every strand and glitters in the weak light. It is beautiful, and you do not want to touch any of it.");
        AddDetail("bones,cocoons,husks,bundles,remains", "Several silk-wrapped bundles hang from the upper branches, some small enough to be mice and one the size of a fawn.");
        AddDetail("rustle,sound,noise,air", "The rustling comes from above and behind, from everywhere and nowhere, the sound of many small legs on dry silk.");
        AddDetail("path,tunnel,leaves", "The path is a tunnel through the thicket, so low in places that you must duck beneath the silk. Rangers have cut it back, and the webs have grown over the cuts.");

        AddSpawn("/domains/greenvale/corridor/mob/forest_spider", 2);
    }
}
