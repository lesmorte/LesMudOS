// @inherit /std/room

public class HwM11 : Room
{
    public override void Create()
    {
        ShortDesc = "The Heartwood Road, the Last Great Oak";
        LongDesc = "One great oak stands alone beside the road where the wood ends, its crown spread over the road like the roof of a hall. It is older than any tree in the farmland to the north, and its trunk is wider than the cottages of the Hollow. Its limbs are propped in two places by timber baulks that someone has set, and the bark is cut with initials and rings to the height of a rider's hand. Beyond it the trees thin and the road runs out toward plain daylight.";
        Exits["south"] = "/domains/greenvale/corridor/heartwood/m10";
        Exits["north"] = "/domains/greenvale/corridor/approach/m01";
        DangerBand(7, 8);

        AddDetail("oak,tree,great oak,trunk,last oak,last great oak", "The oak has a trunk of forty paces' girth at the base, ridged and furrowed like a hill. Three limbs, each larger than most trees, leave it at the height of two men. It does not feel like the great trees behind you: it feels like the last of them, and aware of it.");
        AddDetail("initials,carvings,carving,names,letters,cut,marks", "The bark is carved from the roots to as high as an arm will reach with initials and hearts and short words. The oldest are healed over into pale seams, and the newest are white and sharp. You could {cmd}read{/} the carvings.");
        AddDetail("baulks,baulk,timber,props,prop,limbs,limb", "Two heavy timber props hold up the largest limbs. They are squared oak, pegged and bound with iron, and the iron has rusted nearly through. Someone cared enough to keep the tree standing, and some time ago stopped.");
        AddDetail("ring,rings,blaze,riverwatch,mark", "A ring cut through the bark at the height of a man's chest is the Riverwatch's last blaze on the road, and under it someone has cut a single word and a date too worn to read.");
        AddDetail("acorns,acorn,leaves,leaf,ground", "Acorns the size of a thumb lie thick under the tree, and none have sprouted. The squirrels have stripped them, and the jays have left their feathers.");
        AddDetail("road,north,country,fields,daylight,open", "Beyond the oak the road runs on under thinner trees into plain daylight, and you can hear a wagon's wheels somewhere ahead. After the Heartwood, it looks bright and small and kind.");
        AddReadable("initials,carvings,carving,names,letters,cut,marks", "Most of it is the usual thing: pairs of initials bound by a knot, \"B + E\", \"WE CAME THIS FAR\", a ship scratched by a child, a tally of nights. High on the south side, in a firm hand and deeper than the rest, someone has cut: \"THROUGH THE HEARTWOOD AND OUT AGAIN. NO ONE TOLD ME IT WOULD BE SO LONG.\"", "The bark is carved with initials and short words. You could {cmd}read{/} the carvings.");
    }
}
