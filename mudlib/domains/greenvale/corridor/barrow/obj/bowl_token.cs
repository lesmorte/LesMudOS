// @inherit /std/find

/// <summary>A token in one of the seven bowls: each character may take one, ever (a keepsake worth 15), with no end.</summary>
public class FbBowlToken : Find
{
    public override void Create()
    {
        Name = "Bowl Token";
        Ids.Add("token");
        Ids.Add("grave token");
        Ids.Add("disc");
        Ids.Add("stone disc");
        ShortDesc = "a flat stone token in one of the bowls";
        LongDesc = "A disc of green-grey stone no bigger than a coin, ground perfectly flat, lies in the crust of one of the bowls. One face is cut with a ring crossed by a bar; the other is blank. It is laid there as if for whoever comes.";
        TakenShort = "a bowl with a disturbed crust where a token lay";
        TakenLong = "The crust of one of the bowls is broken where you lifted a flat stone token out of it. The other bowls hold only dust.";
        EmptyShort = "a bowl with a disturbed crust";
        EmptyLong = "The crust of one of the bowls is broken, as if something had been lifted out of it. Nothing is left in it.";
        ItemLine = "You lift %item% out of the crust of the bowl. It is very cold to the hand.";
        ItemPath = "/domains/greenvale/corridor/barrow/obj/grave_token";
    }
}
