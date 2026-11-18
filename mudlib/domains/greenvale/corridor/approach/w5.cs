// @inherit /std/room

public class OaW5 : Room
{
    public override void Create()
    {
        ShortDesc = "Southfarm Hamlet, the Back Fields";
        LongDesc = "Behind the hamlet's cottages lie its fields: long strips of beans, barley and kale, bounded by hedges and stone walls, with the cart-lane running down between them toward the road. Haystacks stand like beehives, a scarecrow in a patched cloak keeps a poor watch over the kale, and bramble thickets heavy with berries crowd the margins where the plough has not reached. West of the last hedge the land rises into rough woodland, and the farmers' strips stop there as if at a line drawn on a map.";
        Exits["south"] = "/domains/greenvale/corridor/approach/w4";
        Exits["east"] = "/domains/greenvale/corridor/approach/m07";
        Exits["west"] = "/domains/greenvale/corridor/approach/d3";
        ResetSeconds = 120;

        AddDetail("fields,strips,beans,barley,kale,crops", "The strips are narrow and neat, each marked by a stake with a family's colour of rag. Whoever farms the western ones does so with one eye on the tree-line.");
        AddDetail("haystacks,hay,stacks,ricks", "Conical ricks, thatched against the rain with bundled straw. One has been burrowed into by a fox, or a boy.");
        AddDetail("scarecrow,cloak,figure", "The scarecrow wears a patched cloak and a farmer's hat, and holds a flail in one stick-hand. A crow sits on its shoulder and looks smugly out over the barley.");
        AddDetail("brambles,bramble,thickets,berries,blackberries,hedgerow,wildflowers", "Blackberries hang in clusters on the hedge-banks, and between them grow poppies, cornflowers and tall yellow ragwort. Women from the hamlet pick here in the early morning.");
        AddDetail("hedge,last hedge,west,tree-line,woodland,trees", "The last hedge is a thick wall of hawthorn and blackthorn, and no gap has been cut in it. Beyond it the land rises into a gloomy tangle, with tracks that seem to lead only inward.");
        AddDetail("sheaf,last sheaf,post,corn dolly,custom", "A single sheaf of barley is tied to a post at the corner of the last field, the custom of the hamlet, and no one has taken it. 'It is for whatever is out there,' a child told a traveler once, and refused to say more.");
    }
}
