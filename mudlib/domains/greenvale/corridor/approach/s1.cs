// @inherit /std/room

public class OaS1 : Room
{
    public override void Create()
    {
        ShortDesc = "The Sealed Culvert";
        LongDesc = "A low passage with a pointed roof runs away into darkness, its walls and floor laid with huge blocks fitted so truly that the joints are only hairlines. The air is dry, still and faintly warm, with none of the dripping chill of the cistern above, and your footsteps seem to fall into silence a stride away. The far end of this culvert was closed long ago by its own builders, with blocks of a paler stone fitted as exactly as the walls themselves. Every few paces the stone is carved with a line of small, clean marks.";
        Exits["up"] = "/domains/greenvale/corridor/approach/d1";
        ResetSeconds = 120;

        AddDetail("blocks,stone,joints,walls,masonry,floor", "The blocks are the size of coffins, laid without mortar, of a dark green-grey stone whose faces have a faint sheen. The masons who laid them were not working in any style the city knows.");
        AddDetail("channel,floor channel,groove,water channel,gutter", "A shallow channel runs down the floor's centre, as smooth as the inside of a bowl. It slopes away north, into the dark, toward the walled end.");
        AddDetail("infill,walling,wall,blocked end,rubble,north end", "The far end is filled solid with blocks of a paler, finer-grained stone, fitted so closely that no mortar shows, the same filling that closes the arch in the cistern above. Its outline is a tall pointed opening, where a great deal of something once ran through. Whoever built this place also shut it, and took care over the shutting.");
        AddDetail("vault,roof,ceiling", "The roof rises to a point along its length, closed by a keystone set every ten paces. At each keystone the marks are larger.");
        AddDetail("steps,ladder,way up,hole,gap,above", "Above you, in the roof near the entrance, is the gap you came through, a ragged square of grey light. It is about six feet up, with footholds in the wall beneath.");
        AddDetail("dust,floor dust,footprints,prints", "A fine pale dust lies over everything, and in it there are no footprints but your own.");
        AddReadable("marks,carved marks,signs,carvings,symbols,glyphs,line of marks", "The marks are cut in rows along the wall at shoulder height, small and regular. Three recur: a ring crossed by a bar; a single wavering line, like water; and a ring with a gap in it, open to the north. Between them are columns of short strokes, always in groups of seven. They are not letters of the city, nor of any tongue the Assembly keeps. The ring-and-bar is the same sign that is worn into the piers of the arch on the road.", "Rows of small clean marks are carved into the wall. You could {cmd}read{/} them, or at least try.");

        AddSpawn("/domains/greenvale/corridor/approach/obj/old_coins");   // a find: each character takes it once, for less each time
    }
}
