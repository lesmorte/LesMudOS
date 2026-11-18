// @inherit /std/room

public class GrTheWatchtowerCellar : Room
{
    public override void Create()
    {
        ShortDesc = "The Watchtower Cellar";
        LongDesc = "A low, vaulted cellar of dressed stone, dry and cold, with the smell of cold iron and old paper. Racks along the walls hold rusted lanterns, a few moldering sacks and the frames of cots. A table has been dragged into the middle, and on it a lantern, a heap of papers and a ledger bound in black cloth wait as if their owner stepped away. On the end wall, at the height of a man's eyes, something has been burned deep into the stone.";
        Exits["up"] = "/domains/greenvale/corridor/green_road/w3";
        Safe = false;

        AddDetail("cellar,vault,vaulted ceiling,ceiling,walls,room", "A cellar of tight-fitting stone, built to hold a garrison's stores. The air is dry and entirely still, and nothing seems to have grown here in years.");
        AddDetail("racks,rack,lanterns,lantern,sacks,sack,cots,cot,frames", "Rows of empty racks, rusted lanterns and rotted sacks. The cots are neatly made. Nobody slept in them in the end.");
        AddDetail("table,papers,paper,heap,pile,lantern on table", "A heavy table with a few sheets of paper under a stone, and a lantern gone cold. The papers are mostly duty rosters and requisitions in a clerk's hand.");
        AddDetail("symbol,mark,sign,burn,burned,burn mark,wall,end wall,burned symbol,circle", "A circle about the size of a shield, burned into the stone with something hotter than any torch. It is broken at the top, with a short bar across the break and three dots beneath it. The lines are very fine and exactly even. It means nothing to you, but it makes your skin crawl a little.");
        AddDetail("steps,stair,stairs,trapdoor,hatch,flag,flagstone,way up", "A narrow stone stair rises to the underside of a flagstone set in the ceiling, which turns on an iron pin. Above, in the tower, it looks like every other flag in the floor.");
        AddDetail("floor,dust,scuffs,flags", "The dust on the floor is thick except in one path, from the stair to the table to the wall. That path has been walked a good deal.");
        AddReadable("ledger,log,logbook,book,garrison log,black ledger", "The last pages of the ledger, in a cramped, careful hand:\n\n...day 61. No relief. Pay is five months owing. Stores sound. Sergeant Holm has forbidden anyone the lower stair after dark and will not say why.\n\n...day 64. Two men gone from the night watch, their beds made. Holm says desertion. Their boots are by the door.\n\n...day 66. Found the mark on the cellar wall. It was not there on the day we took stock. Holm says leave it be. I have copied it, and I am sorry I did.\n\n...day 67. We go at first light. Holm will bar the door from within and go out by the roof. If any Ranger reads this: do not stay in the cellar after dusk, and do not trust the well.\n\n...day 68. I do not think we are all going to...\n\nThe entry ends there.", "A black ledger lies open on the table. You could {cmd}read{/} it.");
    }
}
