// @inherit /std/room

public class FdElderLane : Room
{
    public override void Create()
    {
        ShortDesc = "Elder Lane";
        LongDesc = "Elder Lane is a dead end, a short and silent lane of the oldest houses in Oakenfell, their dark lower stones laid long before the street beyond them was paved. The doors here are low and the windows small, most of them shuttered, and the noise of the city drops away until you can hear your own boots on the stone. At the far end the lane stops against a high old wall, almost wholly hidden under a thick curtain of ivy. The paving beneath your feet is worn into a shallow groove down the middle, and the groove runs straight on toward the wall.";
        Exits["east"] = "/domains/greenvale/oakenfell/founders/old_stone_street";
        // The gap behind the ivy: found by looking at it, or, less surely, by searching.
        HiddenExit("west", "/domains/greenvale/oakenfell/grove/overgrown_way", "gap,darkness,dark,shadow,hole,opening,behind the leaves,behind ivy", 20,
            "You part the thin leaves on the left and find that the darkness is a low doorway through the wall, with the worn groove running on into it. A way leads {cmd}west{/}.");

        AddDetail("lane,street,dead end,end", "A lane of old houses that stops dead against a wall. It does not look as if anyone is meant to come here, and yet the stones have been walked.");
        AddDetail("houses,house,doors,windows,shutters,stones,lower stones", "Low, deep-set houses of black-grey stone, the oldest anywhere in the city. Their doors are narrow, their windows small and shuttered, and their lower courses are rounded and softened by weather. Behind the shutters a clock ticks, a long way off.");
        AddDetail("wall,old wall,high wall,end wall", "A high wall of the same dark stone as the lowest houses, older than the lane that comes up to it. Its top is lost in ivy. The stones are huge and fitted without mortar, and they seem to belong to a different, much older city than the one that has grown around them.");
        AddDetail("ivy,leaves,vines,stems,curtain", "A thick curtain of ivy, its stems as thick as a wrist, covers the wall from top to bottom. On the left, the leaves are noticeably thinner, and the stems have been rubbed bare in places, as if by many hands over many years. Somewhere beneath them there is a gap.");
        AddDetail("gap,darkness,dark,shadow,hole,opening,behind the leaves,behind ivy", "Behind the leaves, low on the wall, there is a darkness that is not shadow. A cool air stirs out of it, smelling of moss and wet wood, and for a moment, faintly, you hear birds, which is odd in a lane with no tree in sight. Something is there, behind the wall.");
        AddDetail("paving,groove,flagstones,flagstone,floor,worn paving,ground", "The paving is worn in a long shallow groove down its center, the kind of wear that takes centuries and many feet. It runs the length of the lane, up to the foot of the wall, and then, so far as you can tell through the ivy, on under it.");
        AddDetail("birds,birdsong,sound,noise,silence", "Now that you listen, there is birdsong from somewhere beyond the wall, thin and clear, and not a single bird in sight in the lane.");
    }
}
