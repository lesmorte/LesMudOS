// @inherit /std/room

public class GvOvergrownWay : Room
{
    public override void Create()
    {
        ShortDesc = "The Overgrown Way";
        LongDesc = "A narrow way runs west between two ancient walls of dark stone, so close together that a broad man would brush both. Ivy and hazel arch overhead and make a low green roof, and every stone, wall and step is furred with moss. The shallow groove you followed in Elder Lane continues along the paving, but here thick grey roots have buckled the stones on either side of it. The noise of the city drops away behind you, and ahead, where the way brightens, there is birdsong.";
        Exits["east"] = "/domains/greenvale/oakenfell/founders/elder_lane";
        Exits["west"] = "/domains/greenvale/oakenfell/grove/hidden_grove";

        AddDetail("walls,wall,old walls,stone,stones,dark stone", "Walls of great dark stones, fitted without mortar and taller than a man can reach. They are older than the houses of Elder Lane by a long way, and they have been patched, in places, with later stone that is itself ancient.");
        AddDetail("moss,green,furred stone", "Moss covers everything in a soft green fur, thick as a hand on the north faces of the walls and thin on the steps. It is damp and cool, and it swallows the sound of your footsteps.");
        AddDetail("ivy,hazel,roof,branches,canopy,leaves", "Ivy and old hazel meet overhead and make a roof, through which the light falls green and broken. Behind you, in the east, the ivy curtain of Elder Lane hangs open on the gap you came through.");
        AddDetail("paving,groove,floor,ground,path,steps", "The same shallow groove that ran the length of Elder Lane runs on down the middle of the paving here, worn by centuries of feet, and the paving is lifted and cracked on either side of it by the roots beneath.");
        AddDetail("roots,root,grey roots,thick roots", "Thick grey roots, each as broad as a man's thigh, push up through the paving and dive down again. They run west, the way you are going, and where they cross the walls they go under them.");
        AddDetail("birdsong,birds,sound,light,brightness", "Thin and clear and very close, the birdsong grows as you walk. Where the way ends to the west the light turns from green to gold.");
        AddDetail("gap,east,elder lane,curtain,entrance", "Behind you, at the east end of the way, the ivy hangs open on a patch of grey daylight and the dark stones of Elder Lane.");
    }
}
