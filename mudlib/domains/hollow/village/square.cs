// @inherit /std/room

public class VillageSquare : Room
{
    public override void Create()
    {
        ShortDesc = "Village Square";
        LongDesc = "The center of Vincent's Hollow bustles with life. A large stone well marks the center of the square while merchants display goods from temporary stalls. Locals gather around benches exchanging news while children dart between travelers.";
        Exits["north"] = "/domains/hollow/village/gate";
        Exits["west"] = "/domains/hollow/village/guild";
        Exits["east"] = "/domains/hollow/village/ironoak";
        Exits["south"] = "/domains/hollow/village/tavern";
        Safe = true;

        AddDetail("well,stone well", "A broad well of fitted grey stone, its rim worn smooth by seventy years of hands and buckets. The water far below smells cold and clean. A small plaque is set into the stonework.");
        AddDetail("bench,benches", "Rough plank benches, polished by generations of travelers and gossips. A few locals are leaning together over some news or other.");
        AddDetail("stalls,stall,merchants", "Temporary stalls of canvas and trestle tables, piled with tools, cloth and trinkets. Most of it is not for sale to the likes of you, but a traveling merchant seems willing.");
        AddDetail("children", "Children dart between the stalls, shrieking with laughter. Somehow none of them ever collides with a cart.");
        AddReadable("plaque,small plaque,bronze plaque", "The plaque reads:\n\"This well was dug by Vincent Holloway and his companions during the settlement's first winter.\"", "A small bronze plaque is set into the rim of the well. You could read it.");

        AddSpawn("/domains/hollow/npc/benson");
        AddSpawn("/domains/hollow/npc/traveling_merchant");
    }
}
