// @inherit /std/room

public class GrTheAbandonedWatchtower : Room
{
    public override void Create()
    {
        ShortDesc = "The Abandoned Watchtower";
        LongDesc = "On the crown of a wooded knoll stands the Riverwatch tower, a square stone shell with its roof fallen in and its door hanging from one hinge. The yard around it is thick with nettle and young birch around a covered well, and a rusted signal-mast leans over the parapet. Inside it is dark, and the stairs end in rubble overhead. The ground on the west side of the knoll falls away into the forest, and a faint trail goes down there between the trees. The post was given up years ago, and the reasons given depend on who is asked.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/w2";
        Exits["north"] = "/domains/greenvale/corridor/green_road/w4";
        Exits["west"] = "/domains/greenvale/corridor/green_road/d1";
        Safe = false;

        AddDetail("tower,watchtower,walls,wall,shell,stone", "A tower of grey blocks, thirty feet high, built to the Riverwatch pattern with arrow slits and a corbelled parapet. The upper floor has fallen in and the lower floors are choked with leaf, fallen stone and bird droppings.");
        AddDetail("door,hinge,gate,doorway", "An oak door of two leaves, one gone and one hanging by its top hinge. The inside of it has been barred, and the bar is still there, snapped off, on the inner side.");
        AddDetail("mast,signal mast,signal,pole,flag,parapet", "A rusted iron mast leans out over the parapet. A scrap of grey cloth may once have been a pennant.");
        AddDetail("yard,nettles,nettle,birch,saplings,courtyard", "A yard of nettle and birch saplings. A wall once ran around it, and a few stones of the wall still stand.");
        AddDetail("well,well-head,wellhead,cover,well cover,windlass", "In the nettles of the yard is a well-head of fitted grey stone, with the stump of a windlass above it. It has been covered with a slab of oak and the slab weighted with blocks from the fallen wall, more blocks than a lid needs. No sound comes from under it, and no smell.");
        AddDetail("floor,flagstones,flags,hearth,fireplace,cellar,trapdoor,hatch,rubble", "In the corner of the ground floor a hearth has been swept clean of ash, which is odd. The flagstones before it are scuffed in a half-circle, as if something heavy had been dragged back and forth, and one flag in the middle rings hollow when kicked.");
        AddDetail("stairs,staircase,steps", "A stone stair climbs the inside wall for a short way and then ends in a heap of rubble. A rotten rope hangs from a hole above.");
        AddDetail("rumor,rumors,why,garrison,rangers,riverwatch", "Travelers say the Rangers left because the well went bad, or because the post was starved of pay, or because of something in the cellar. The Rangers, when asked, say only that the post served its purpose.");
        AddDetail("trail,faint trail,west trail,goblin trail", "A narrow trail, kept open by passing feet, leads down off the west side of the knoll. The feet are small and bare.");
        AddReadable("scratches,graffiti,wall scratches,carved words,wall", "Cut into the stone beside the door, by many hands over many years:\n\nEVER VIGILANT\nR. HOLM, 3RD WATCH\nSEND PAY OR SEND RELIEF\nDON'T GO DOWN FOR WATER\n(and the last, in a neater hand, the letters struck out)", "Words are scratched into the stone beside the door. You could {cmd}read{/} them.");
    }
}
