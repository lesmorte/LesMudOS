// @inherit /std/room

public class MkConsortiumHouse : Room
{
    public override void Create()
    {
        ShortDesc = "Consortium House";
        LongDesc = "Consortium House is a hall of dark wood and pale stone, hung with wall maps of the trade roads, their routes inked in blue and red. A long gallery of desks stands at one end, where clerks and factors sit under green-shaded lamps. Samples of wool, grain, timber and glass are displayed in cases along the walls, and a bronze scale as big as a man stands in an alcove. A blue banner hangs over the main desk, and below it, a plaque bears the Consortium's motto. Visitors are received politely, and watched.";
        Exits["north"] = "/domains/greenvale/oakenfell/market/coin_street";
        ResetSeconds = 120;

        AddDetail("maps,wall maps,routes,roads", "Greenvale and its neighbors, drawn in fine ink. The trade roads are blue and the Consortium's warehouses are marked with gold. A few red lines, circled and annotated, run west toward the Elderwood.");
        AddDetail("desks,clerks,factors,lamps", "Clerks write, factors argue, runners hurry in and out. Everyone looks like they are doing arithmetic in their heads.");
        AddDetail("cases,samples,wool,grain,timber,glass", "Samples of Greenvale's wealth: a hank of fine wool, a jar of wheat, a plank of Elderwood oak and a sheet of mountain glass, all labelled with the price.");
        AddDetail("scale,bronze scale,alcove", "A huge bronze scale, its pans each large enough to hold a child. It is purely ornamental, which the Consortium would deny.");
        AddDetail("banner,blue banner,sheaf", "The Consortium's banner shows a golden scale over a sheaf of wheat on a field of blue. It hangs heavy and still.");
        AddReadable("plaque,motto,bronze plaque", "THE MERCHANT CONSORTIUM OF GREENVALE\nTrade Builds Tomorrow.", "A bronze plaque under the banner carries the Consortium's motto. You could {cmd}read{/} it.");

        AddSpawn("/domains/greenvale/oakenfell/market/npc/factor_isolde");
    }
}
