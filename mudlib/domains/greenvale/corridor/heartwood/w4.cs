// @inherit /std/room

public class HwW4 : Room
{
    public override void Create()
    {
        ShortDesc = "The Ritual Stones";
        LongDesc = "Seven standing stones, grey and lichened and each as tall as a man, stand in a ring in a clearing where the forest floor has been swept bare. The ground between them is cleared of leaf and the grass cut short, and a living thorn hedge has been trained around the outside, with a single gap. Twigs of thorn are bound to the stones with red thread, some old and bleached, others green. The place is empty, tended and still. There is no sound, but you have the feeling of having walked into the middle of something that has just ended.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/w3";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/w5";
        DangerBand(8, 9);

        AddDetail("stones,stone,standing stones,ring,monoliths", "The stones are rough and unworked, set in the ground without a base. Each leans a little toward the middle, and each carries a mark cut at the height of a man's chest. They are far older than the thread on them.");
        AddDetail("mark,marks,markings,thorn mark,symbol,sign,cut,carved", "The mark cut in each stone is a simple sign: a short upright stroke with three barbs, like a thorn drawn by a child. It is deeply cut and clean-edged, and the lichen has been scraped from the groove. Somebody keeps it open. You could {cmd}read{/} the tallest stone.");
        AddDetail("hedge,thorn,thorns,thorn hedge,gap", "The thorn hedge is waist-high, dense and carefully trained, with each stem bent over and tied into the last. The gap in it faces south. No path leads to the gap, and yet the ground there is bare.");
        AddDetail("thread,red thread,twigs,twig,bindings,offerings", "Each stone has a twig of thorn bound to it at the level of the heart with a loop of red thread. The oldest twigs are grey and brittle, and the newest are green and still have their leaves. They have been changed within the month.");
        AddDetail("ground,floor,grass,swept,bare,clearing", "The ground is swept, and has been swept many times, to the packed earth. In the centre is a flat stone the size of a table, bare, with a shallow cup in the middle. It is dry.");
        AddDetail("cup,flat stone,table,altar,centre,center", "The flat stone has a cup in the middle as wide as a bowl, worn smooth. There is a ring of dried moss in the cup, and a single clean feather. Nothing else.");
        AddReadable("tallest stone,tall stone,tallest,mark,marks,markings,thorn mark,symbol,sign,cut,carved", "Beneath the thorn sign on the tallest stone, in letters smaller than your thumb and scratched rather than cut, is a single line:\n\"THE FOREST REMEMBERS\"", "A thorn sign is cut into each stone. You could {cmd}read{/} the tallest stone.");
    }
}
