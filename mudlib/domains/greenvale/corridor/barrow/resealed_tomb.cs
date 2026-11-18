// @inherit /std/room

public class FbResealedTomb : Room
{
    public override void Create()
    {
        ShortDesc = "The Resealed Tomb";
        LongDesc = "A square chamber, no larger than a farm kitchen, holds a single great chest of green-grey stone on a low plinth, its lid a slab as thick as a man is deep. The lid has been moved: the old tight joint is broken all round, and someone has run a bead of new pale lead along the seam to close it again, as neatly as a mason would, and a drop of pale wax has hardened on the plinth beside it. The floor is clean and cold, and the dust lies even except along a narrow track from the door to the plinth. Panels of the interlaced carving cover the walls from floor to vault.";
        Exits["east"] = "/domains/greenvale/corridor/barrow/descending_way";
        ResetSeconds = 600;
        DangerBand(9, 10);

        AddDetail("chest,sarcophagus,coffin,tomb,stone chest,plinth", "The chest stands on a plinth the height of a bench. It is cut from one block, and its sides carry the same band of figures, but small, and with their arms lowered to their sides. It is longer than a man would need.");
        AddDetail("lid,seam,joint,lead,bead,new lead,reseal", "The old joint was no wider than a hair and sealed with nothing but the weight of the lid. The new seam is a bead of lead, still bright where it was smoothed with a thumb or a tool, and the pitted edge of the stone shows beside it where the lid was levered up and set back down. A mason of Oakenfell would be glad to do as well.");
        AddDetail("wax,drop,candle,drip,plinth edge", "On the edge of the plinth is a single drop of wax, pale and clean, set hard where it fell. A candle stood here while someone worked, and was taken away with them.");
        AddDetail("track,dust,floor,footprints,tracks", "The track runs from the doorway to the plinth, one pair of feet going in and the same pair coming out. Elsewhere the floor has not been touched for a very long time, and the dust is a smooth skin from wall to wall.");
        AddDetail("walls,wall,panels,panel,carvings,carving,vault", "The panels are of the same interlaced pattern as the stair, cut in groups of seven, but here the groups are closed at both ends with a small ring crossed by a bar. The work is deep and exact. You find no letter anywhere in it, nor any figure of a person.");
        AddDetail("door,doorway,east,threshold", "The doorway back to the stair is tall and narrow, and its threshold is as unworn as the rest.");
        AddSpawn("/domains/greenvale/corridor/mob/restless_spirit", 1);
    }
}
