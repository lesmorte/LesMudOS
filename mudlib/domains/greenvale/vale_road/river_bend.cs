// @inherit /std/room

public class VrRiverBend : Room
{
    public override void Create()
    {
        ShortDesc = "Vale Road, the River Bend";
        LongDesc = "The road swings east to follow a broad, slow river, brown over gravel and silver where the sun finds it. This is the River Mere, and a plank-and-stone footbridge crosses a tributary beside a ford marked with painted poles. Reeds crowd the bank, and herons stand in the shallows as if on watch. A small signal post of lashed timber stands on the rise above the water, and a Riverwatch ranger keeps to the road near it, eyes moving from the river to the hills and back again.";
        Exits["south"] = "/domains/greenvale/vale_road/meadows";
        Exits["north"] = "/domains/greenvale/vale_road/rise";
        ResetSeconds = 120;

        AddDetail("river,mere,river mere,water", "The River Mere runs wide and unhurried here, with a clean gravel bed showing through the shallows. Far out a flat barge is poled slowly downstream, its load hidden under tarpaulins.");
        AddDetail("reeds,bank,riverbank", "Tall reeds, brown at the tips, whisper in the wind. Between them the mud is stitched with the prints of herons and, closer to the road, hobnailed boots.");
        AddDetail("heron,herons,birds", "Two grey herons stand motionless in the shallows, so still that they might be carved. One flicks into the air as you watch, and settles again farther down.");
        AddDetail("footbridge,bridge,ford,poles", "A little bridge of planks on stone piers crosses the tributary, with a shallow ford beside it for carts. Painted poles show the depth, and the highest mark, well above your head, is dated to a flood decades ago.");
        AddDetail("signal post,post,timber,beacon", "A tall post of lashed timbers with a cresting of iron for a brazier. Rangers light it at night to show the road is watched, and by day a bundle of coloured pennants says whether it is quiet.");
        AddDetail("pennants,flags", "Three pennants hang limp from the post: green, white and a grey-blue. By the ranger's easy stance, green means all is well.");

        AddSpawn("/domains/greenvale/vale_road/npc/ranger");
    }
}
