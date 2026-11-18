// @inherit /std/room

public class FbLowNiche : Room
{
    public override void Create()
    {
        ShortDesc = "The Low Niche";
        LongDesc = "You must stoop, almost kneel, to come through the gap beneath the slab, and the cell inside is hardly bigger than a cupboard. It is walled with a single course of small blocks and floored with the pale dust, and the cold here is the same as on the stair, but nearer. A shelf at the back is cut in the shape of a shallow hand, and it was made for something other than a body. On the shelf lies a curl of black ash, delicate as a feather, kept in the shape of a folded sheet.";
        Exits["west"] = "/domains/greenvale/corridor/barrow/descending_way";
        DangerBand(9, 10);

        AddDetail("ash,page,sheet,curl,burned page,black", "A single page was burned here, on the shelf, and left to burn out. The ash has kept the fold of the sheet, and in it the ghost of ruled lines and a column of small marks in a hand that is not the hand of the carvers. Not a word survives. A breath would take it, and so the draught from the stair has not been let in: the slab is as it was left.");
        AddDetail("shelf,hand,hollow,niche,cell", "The shelf is cut to the shape of an open hand, fingers spread, the size of a platter. The stone beneath the ash is not even blackened. The page burned on something that was afterward carried away.");
        AddDetail("blocks,course,walls,wall,small blocks", "The blocks are no bigger than loaves and are laid as exactly as the great walls outside, and with the same hair-wide joints. They are bare. This cell is the one place in the barrow where nothing is carved.");
        AddDetail("slab,gap,doorway,entrance,west,opening", "The slab that half-blocks the entrance rests on its own jamb and is quite sound. A hurried party would have gone by and never known the cell was here.");
        AddDetail("dust,floor,knee marks,marks,knees", "In the dust before the shelf are two shallow, round prints where someone knelt, and beside them the straight mark of a sleeve. The dust at the edges has been brushed smooth again.");
    }
}
