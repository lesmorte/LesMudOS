// @inherit /std/room

public class GrTheDarkHollow : Room
{
    public override void Create()
    {
        ShortDesc = "The Green Road, the Dark Hollow";
        LongDesc = "The road dips into a hollow where the trees close over it entirely and the only light is a grey half-dark that never changes with the hour. Water seeps from the banks and stands in the ruts. The air is cold, still and heavy with the smell of wet rot, and sounds from the wood come strangely, near and far at once. On the high banks on both sides, the roots of the trees hang out over the road like fingers, and a fallen oak lies across one verge with its roots in the air.";
        Exits["south"] = "/domains/greenvale/corridor/green_road/m09";
        Exits["north"] = "/domains/greenvale/corridor/green_road/m11";
        Safe = false;

        AddDetail("hollow,dip,dark hollow,bowl,dell", "A steep-sided dip in the land, where the road runs between banks the height of a man. A highwayman could not have built a better place.");
        AddDetail("banks,bank,roots,root,earth", "Exposed earth and a mat of roots, grey with age. Gaps between them are dark and dry, and one or two look like they were dug.");
        AddDetail("oak,fallen oak,tree,trunk,fallen tree", "A huge oak, down across the verge, lying with its root plate still holding a ton of earth. The trunk is rotten at the heart and full of beetles.");
        AddDetail("water,seep,ruts,puddle,puddles", "Cold seeping water. The ruts are half full and ringed with the prints of small bare feet.");
        AddDetail("prints,footprints,bare feet,feet", "A tangle of small, three-toed prints pressed into the mud, going both ways. Whatever made them stood about a long while, and they are fresh.");
        AddDetail("dark,gloom,shadows,light", "The gloom is almost complete at the bottom of the dip. Even at midday a traveller here would be glad of a lantern.");

        AddEncounter("/domains/greenvale/corridor/mob/goblin_raider", 12);
    }
}
