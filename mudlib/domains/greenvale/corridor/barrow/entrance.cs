// @inherit /std/room

public class FbEntrance : Room
{
    public override void Create()
    {
        ShortDesc = "The Mouth of the Barrow";
        LongDesc = "A steep ramp of fallen blocks runs down from the gap above, and at its foot the floor becomes cut stone: a wide, shallow stair going on into the dark. The cold draught that you felt at the shrine is steady here and perfectly dry, and it moves past you toward the light without a sound. Everything the roots and the rain have not reached is clean and pale with fine dust, as if the weather had stopped at the gap. Over your head a smear of soot darkens the underside of a jutting slab. Far above, the way out is a narrow wedge of green daylight.";
        Exits["out"] = "/domains/greenvale/corridor/heartwood/d3";
        Exits["down"] = "/domains/greenvale/corridor/barrow/forecourt";

        AddDetail("gap,wedge,light,daylight,above,way out", "The gap is a slot of leaf-green light, so high and so narrow that it looks cut from paper. No sound comes down from it. The edge of the daylight ends on the ramp in a line as sharp as a knife, and below the line the stone is grey.");
        AddDetail("ramp,blocks,block,rubble,slope,fallen", "The blocks of the ramp are the green-grey stone of the shrine, tumbled where the roof of some outer chamber came down. Someone has been over them with care: the loose ones are wedged, and the worst of the edges are scuffed, as by a pack carried through a tight place.");
        AddDetail("stair,steps,step,treads,stone,floor", "The stair is cut from the rock and the treads are of one piece with the walls, without seam or joint. They are not worn. Those who came down this stair came seldom, or came all at once and never climbed it again.");
        AddDetail("dust,pale dust,powder", "The dust is as fine as flour and lies evenly on every tread. It has the dry feel of a thing that never has been wet. Down the middle of the stair it has been disturbed by boots, and at the edges brushed level again.");
        AddDetail("draught,draft,air,cold,wind", "The air flows up the stair from below, steadily, as though the place were breathing out. It smells of nothing but dry stone and cold, and it carries no damp, no rot and no smoke.");
        AddDetail("soot,smear,lintel,smudge,slab", "Just inside the gap, over your head, a slab of the old wall juts out like a lintel. On its underside is a smear of soot the width of a hand, black and soft to the finger: a lamp carried too high by someone who did not stoop. The rain has not been in to touch it.");
    }
}
