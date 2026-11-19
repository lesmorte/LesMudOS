// @inherit /std/room

public class OaM04 : Room
{
    public override void Create()
    {
        ShortDesc = "The Approach Road, Milestone Row";
        LongDesc = "The road is fully paved now, and a line of grey milestones marches along the verge, one at every furlong, each carved with a number and the city's oak. Wagons pass in both directions with their wheels ringing, and drivers lift a hand to one another without turning their heads. Beyond the hedges the fields are tilled, with stooks standing in rows, and to the north, plainly now, a long grey line lies across the land that you can only call a wall.";
        Exits["south"] = "/domains/greenvale/corridor/approach/m03";
        Exits["north"] = "/domains/greenvale/corridor/approach/m05";
        ResetSeconds = 120;
        DangerBand(6, 8);

        AddDetail("milestones,stones,milestone,numbers", "The stones stand knee-high and leaning slightly, as milestones do, each with its numeral cut deep and filled with black paint. Travelers have rubbed the oak on the nearest to a shine.");
        AddDetail("wall,grey line,city wall,horizon,line", "It lies low and level across the north, grey against the paler sky, and it does not end at either side of your view. You are still miles away, and it is already the largest made thing you have ever seen.");
        AddDetail("fields,stooks,stook,hedges,farmland", "The field beyond the hedge has been reaped, and the stooks stand in neat ranks like a congregation. Rooks walk between them, in no hurry.");
        AddDetail("wagons,traffic,drivers,carts", "Both directions are busy: big six-ox wagons creaking south, lighter carts rattling north with crates of hens and baskets of apples. The drivers know each other by their teams.");
        AddDetail("oak,oak carving,carving", "The oak on each stone is cut the same, with a spreading crown and a thick bole, and below it a row of roots running out to the edges. Roots, the motto says, and the masons clearly took it to heart.");
        AddDetail("rooks,birds,crows", "A dozen rooks pick over the stubble, rising in one black sheet whenever a wagon passes and settling again with an air of complaint.");
        AddReadable("milestone,nearest milestone,stone,distance", "OAKENFELL, 5 MILES\nVINCENT'S HOLLOW, 36 MILES\nFrom Strong Roots Grow Great Things", "The nearest milestone has letters cut into its face. You could {cmd}read{/} it.");

        AddEncounter("/domains/greenvale/corridor/mob/highway_bandit", 6);
    }
}
