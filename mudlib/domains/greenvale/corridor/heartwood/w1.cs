// @inherit /std/room

public class HwW1 : Room
{
    public override void Create()
    {
        ShortDesc = "The Mossgate Ruins, the Inner Court";
        LongDesc = "The trail ends in an open court of dark flagstones, roofless and walled on three sides, with trees growing up through the paving as through a floor of tiles. The walls are the height of two men and thick as a cart is wide, and in the one that faces you is a row of tall narrow doorways with no doors. The stones are the same unhurried green-grey as the Fallen Arch, and a thin band of carving runs round the whole court at the height of the eye. Nothing moves here but the leaves, and there is nothing to say who left, or when.";
        Exits["west"] = "/domains/greenvale/corridor/heartwood/m03";
        Exits["north"] = "/domains/greenvale/corridor/heartwood/w2";
        ResetSeconds = 300;

        AddDetail("court,flagstones,flagstone,paving,floor,flags", "The flags are the size of a table and cut so true that the tree roots have found only the smallest crack to enter. In the middle of the court the paving has been worn by something into a long shallow dish, deepest at the centre. Rainwater lies in it and does not spill.");
        AddDetail("walls,wall,masonry,stone,stones", "The wall is built as the wall on the road was built: long blocks, no mortar, each course leaning a little inward. The top is gone in places, and the broken edge shows that the stone is solid all through, with no rubble fill.");
        AddDetail("doorways,doorway,doors,door,openings,lintels", "Seven tall doorways, narrower at the top than at the bottom, stand side by side in the north wall. None has a door or a sign of one. Beyond the first, a passage of fallen roof stones leads nowhere, and behind the others there is only wall.");
        AddDetail("band,carving,carvings,frieze,figures,pattern,border", "The carved band is a hand and a half deep and runs unbroken round the three walls. It shows a procession of long thin shapes, taller than any man, each with one arm raised, between borders of the same interlaced line that is on the arch. The faces are blank. You could {cmd}read{/} the band, though no one has read it in a long time.");
        AddDetail("trees,tree,saplings,roots,root", "Beeches and one old oak grow up through the court. The oak's roots have lifted a flag as large as a door and tilted it on its edge, and the underside is as smooth as its top.");
        AddDetail("dish,basin,hollow,water,rainwater", "The shallow dish in the paving is eleven paces across, perfectly round, and the lip of it is carved with a groove that runs once around. Whatever stood on the dish is gone.");
        AddDetail("west,trail,path,road", "The trail runs back westward between two fallen blocks. It is the only way in that has not been closed by thorn or deadfall.");
        AddReadable("band,carving,carvings,frieze,figures,pattern,border", "You follow the band round the court. The procession goes always one way, with the raised arms, and every seventh figure carries something that might be a staff or a branch. At the end, where the band should close, it does not. The last figure has no arm raised, and the border under it stops in an unfinished line. There are no letters. The carving is crisp wherever the roof once shielded it.", "A band of carving runs round the court. You could {cmd}read{/} it.");
        AddSpawn("/domains/greenvale/corridor/mob/heartwood_goblin", 2);
    }
}
