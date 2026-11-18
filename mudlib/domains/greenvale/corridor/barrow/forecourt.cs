// @inherit /std/room

public class FbForecourt : Room
{
    public override void Create()
    {
        ShortDesc = "The Sunken Forecourt";
        LongDesc = "The stair ends in a square court sunk in the rock, with walls of dark green-grey stone that rise out of the reach of any light you carry. A band of carving runs round them at the height of the eye, the same interlaced line as the court in the wood, and it takes up where that one broke off. In the paving lies a shallow round dish with a groove that runs from its lip to a tall, narrow doorway in the north wall. Low on the left jamb is a small white chalk mark. The silence has weight.";
        Exits["up"] = "/domains/greenvale/corridor/barrow/entrance";
        Exits["north"] = "/domains/greenvale/corridor/barrow/descending_way";
        DangerBand(9, 10);

        AddDetail("walls,wall,stone,masonry,court,vault", "The walls are laid without mortar, in courses that lean a little inward as they rise, until they are lost in the dark. The joints are a hair wide. Nothing here has settled or cracked in all the time it has been below the earth.");
        AddDetail("dish,basin,bowl,hollow,paving,floor", "The dish is seven paces across and perfectly round, cut into the paving so that its rim is level with the floor. It is smaller than the great dish in the ruined court in the wood, and like it, it is empty and dry. The groove around its lip runs once about and then leaves it toward the doorway.");
        AddDetail("groove,channel,line", "The groove is as wide as a finger and as smooth as the inside of a cup. It runs from the dish straight across the floor to the threshold of the doorway and goes over it, into the passage beyond.");
        AddDetail("doorway,door,threshold,jamb,jambs,arch", "The doorway is tall and narrow, and its head comes to a point. The jambs are cut with the interlaced line, and the stone of the threshold is not worn at all. The passage beyond goes down.");
        AddDetail("chalk,mark,symbol,sign,broken ring,ring,dots,left jamb", "Low on the left jamb of the doorway, no larger than your hand, is a mark in white chalk: a ring broken at the top, with a short bar across the break and three dots beneath it. The lines are fine and even, and the chalk is bright. It does not belong to the stone. It sits on the carving as a label sits on a jar, and whoever drew it knelt to do so and did not smudge it.");
        AddReadable("band,carving,carvings,frieze,figures,figure,procession,pattern,border", "The band begins at the foot of the stair, as though it had come down it. The figures are the long, thin figures of the court in the wood, with the same blank faces and the same single raised arm, and they walk in sevens toward the doorway. Every seventh carries a branch, but here the branch is bare and bound with a cord at its middle. Above their heads the border that broke off unfinished in the court runs on without a flaw. There are no letters.", "A band of carved figures runs round the court. You could {cmd}read{/} it.");
    }
}
