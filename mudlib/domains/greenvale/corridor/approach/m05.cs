// @inherit /std/room

public class OaM05 : Room
{
    public override void Create()
    {
        ShortDesc = "The Merchant Staging Grounds";
        LongDesc = "A great trampled meadow lies beside the paving, hedged with hurdles and crowded with wagons drawn up in rows, hooded with white canvas and chalked with numbers. Oxen low in the lines, drovers shout, and a smoke of cookfires drifts across the whole camp. Caravans are forming up here for the road south, and a lean tally board stands in the middle with a crowd about it, while the Consortium's blue-and-silver banners snap above the busiest corner.";
        Exits["south"] = "/domains/greenvale/corridor/approach/m04";
        Exits["north"] = "/domains/greenvale/corridor/approach/m06";
        ResetSeconds = 120;

        AddDetail("wagons,rows,canvas,hoods,carts", "The wagons stand in numbered rows, tilts laced down, shafts propped. Their sides are chalked with marks for owner, cargo and destination, and the rows nearest the road are already hitched.");
        AddDetail("oxen,ox,beasts,teams,lines", "Great patient oxen stand in their yokes, breath smoking, chewing nothing in particular. A drover's boy walks among them with a bucket, murmuring to each by name.");
        AddDetail("banners,banner,blue banner,consortium,flag", "Blue banners worked with a silver pair of scales hang from tall poles. Beneath the scales is stitched the Merchant Consortium's motto: TRADE BUILDS TOMORROW.");
        AddDetail("cookfires,fires,smoke,camp,pots", "Small fires burn between the wagon rows, with iron pots hung over them and men squatting beside. The smell is onions, bacon and woodsmoke, which is the smell of every road in the world.");
        AddDetail("chalk,chalk marks,numbers,marks", "A wagon's side may read, in a clerk's tidy hand, 'Wool, 40 bales, Hollow' and beneath it, in a drover's scrawl, 'Slow axle.' The chalk is the only record anyone needs here.");
        AddDetail("hurdles,fence,hurdle,meadow,ground", "Woven hazel hurdles divide the meadow into lots, and the grass inside them is churned to a thick mud the colour of tea. A trough of water runs along one side.");
        AddReadable("tally board,board,departures,tally,list", "CARAVANS FORMING, by leave of the Merchant Consortium\n Row 1: salt, nails and lamp oil for the Hollow, second bell.\n Row 2: wool and cloth, south by the Heartwood road, at noon.\n Row 3: grain for the Hollow's mill, waiting on one wheel.\n Row 4: empty, returning south, passage for those who pay the master.\nRiverwatch escort rides with Row 2. Guards are signed on at the master's wagon.\nTRADE BUILDS TOMORROW", "A tall board stands in the middle of the grounds, chalked in columns. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/corridor/approach/npc/wagon_master");
        AddSpawn("/domains/greenvale/corridor/approach/npc/tally_clerk");
        AddSpawn("/domains/greenvale/corridor/approach/npc/drover_diggory");
        AddSpawn("/domains/greenvale/corridor/approach/npc/drover_lorcan");
    }
}
