// @inherit /std/room

public class EeBirchStand : Room
{
    public override void Create()
    {
        ShortDesc = "The Birch Stand";
        LongDesc = "Silver birches stand in tall pale ranks here, white as a row of candles against the darker oaks beyond them, their leaves a trembling sea of green light. The ground is clean and open, floored with golden leaf and sprinkled with toadstools red as berries, and the sun comes through the leaves so freely that the whole stand seems to glow. Birds sing everywhere. On the east side of the stand the birches thin out, and the path begins to climb; to the west the road is visible through the trunks.";
        Exits["south"] = "/domains/greenvale/corridor/eldergreen_edge/w4";
        Exits["west"] = "/domains/greenvale/corridor/eldergreen_edge/m07";
        Exits["east"] = "/domains/greenvale/corridor/eldergreen_edge/d3";
        ResetSeconds = 120;

        AddDetail("birches,birch,trunks,bark,silver birches", "The birch bark peels in papery curls, creamy white and marked with black. It is the best tinder in the wood.");
        AddDetail("leaves,light,glow,sun,sunlight", "The leaves are small and tremble in every breath of wind, throwing a green shimmering light on the white trunks.");
        AddDetail("toadstools,toadstool,fungus,mushrooms,red toadstools", "The toadstools are bright red with white flecks, as pretty as a child's drawing, and as deadly. A line of them follows the birches like a trail.");
        AddDetail("birds,birdsong,song", "Finches, tits and a great many others sing in the birch crowns, a loud mixed chorus that suddenly stops, and then begins again.");
        AddDetail("thistle,thistles,herb,herbs,patch,clearing", "Where an old birch has fallen, the sun reaches the ground and thistles have claimed the hole, with a few sprigs of valerian among them.");
        AddDetail("path,east,slope,climb", "The path east rises through thinning birches towards darker trees, and the birds seem fewer that way. A scrap of grey cord is tied to a twig at the foot of it.");

        AddSpawn("/domains/greenvale/corridor/mob/forest_wolf", 1);
    }
}
