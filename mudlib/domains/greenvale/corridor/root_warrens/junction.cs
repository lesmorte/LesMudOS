// @inherit /std/room

public class RwJunction : Room
{
    public override void Create()
    {
        ShortDesc = "The Diggers' Junction";
        LongDesc = "Three tunnels meet in a round chamber with a low roof, and the roof is held up by a single post as thick as a barrel. The floor is rutted with the tracks of barrows, and a rack of broken tools stands against one wall, picks and shovels with their handles rotted to stubs. A plank has been nailed to the post at the height of a goblin's eye, scored all over with marks. It is quiet here, and strangely still. The rats seem to keep to the other tunnels, and the draught splits and goes round.";
        Exits["north"] = "/domains/greenvale/corridor/root_warrens/root_stair";
        Exits["west"] = "/domains/greenvale/corridor/root_warrens/spoil_heap";
        Exits["east"] = "/domains/greenvale/corridor/root_warrens/quarters";
        ResetSeconds = 300;

        AddDetail("post,pillar,support,prop", "The post is a whole young oak trunk, stripped and set upright. It is the only thing in the chamber that has not been gnawed, and the reason is plain: the lower half has been wrapped in strips of iron, hammered on cold and studded with nails.");
        AddReadable("plank,board,tally,marks,notches,scoring", "The scoring is goblin work, cut with a knife and filled with soot. Whoever made it counted in bundles of five, and wrote a line of blunt words above each column:\n\n  'THE DIGGING. Overseer's count, in the long dig.'\n  'FIRST MOON. West cut, forty paces. Clay. Good for pots, bad for roof. Props wanted. East cut, sixty paces. Roots. Axes dull. Hands split. Diggers eat too much.'\n  'SECOND MOON. Lost two to roof-fall in the west. One run off up the stair. Not counted.'\n  'THIRD MOON. Down-shaft reached stone that is not our stone. Square, fitted, old. Digger Nub says he hears scratching under it. Nub is a fool. Nub is not at this count.'\n  'FOURTH MOON. Rats in the spoil. Big. Killed forty. More came.'\n  'FIFTH MOON. Down-shaft left open one night. Lost eleven diggers. Lost the lamps. Overseer says close shaft. Nobody will go and close it.'\n  'SIXTH MOON. Families go up to the quarters, then up out. Tunnelers hold the Corner. Everyone else go up. Overseer has already gone up.'\n  'Count ends. Nobody left to count.'\n\nThe last line is scored over and over, until the plank is almost cut through.", "A plank nailed to the post, scored with marks. You could {cmd}read{/} it.");
        AddDetail("tunnels,tunnel,mouths,mouth,chamber", "The tunnel to the west is wide and slopes a little down, and the one to the east is lower and ends in a glow of old soot. The way you came is the stair. There is no tunnel to the south: the south wall is solid clay, with a single great root coming through it.");
        AddDetail("rack,tools,tool,picks,pick,shovels,shovel,handles", "The pick heads are hand-forged and heavy, with a short curved tooth, and every one is rusted brown. Not one is whole. The handles are bitten away to splinters, and the chewing goes right to the iron.");
        AddDetail("floor,ruts,tracks,barrows,barrow,wheels", "Two shallow ruts a barrow wide run from the west tunnel to the stair, polished and still plain. Over them, the clawed tracks of rats are thick, though not fresh.");
        AddDetail("roof,ceiling,clay", "The roof is low enough that a tall man must bend, and it is cut so flat that the pick marks run in a single direction. A great root as thick as your waist comes through the south wall and runs along the ceiling, pale and waxy, before it vanishes into the earth.");
    }
}
